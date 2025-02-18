using Analyzer.FileTypes.Internal;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using MathNet.Numerics;
using Plotting.Util;
using ResultAnalyzerUtil;
using Plotly.NET.TraceObjects;

namespace Analyzer.Plotting
{
    public static class AnalyzerGenericPlots
    {
        public static Func<BulkResultCountComparison, int> ResultSelector(ResultType resultType)
        {
            return resultType switch
            {
                ResultType.Psm => m => m.OnePercentPsmCount,
                ResultType.Peptide => m => m.OnePercentPeptideCount,
                ResultType.Protein => m => m.OnePercentProteinGroupCount,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };
        }

        public static GenericChart.GenericChart SpectralAngleChimeraComparisonViolinPlot(double[] chimeraAngles,
            double[] nonChimeraAngles, string identifier = "", bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            var chimeraLabels = Enumerable.Repeat("Chimeric", chimeraAngles.Length).ToArray();
            var nonChimeraLabels = Enumerable.Repeat("Non-Chimeric", nonChimeraAngles.Length).ToArray();
            string label = Labels.GetLabel(isTopDown, resultType);
            var violin = Chart.Combine(new[]
                {
                    // chimeras
                    Chart.Violin<string, double, string> (chimeraLabels,chimeraAngles, null, MarkerColor: "Chimeras".ConvertConditionToColor(),
                        MeanLine: MeanLine.init(true,  "Chimeras".ConvertConditionToColor()), ShowLegend: false), 
                    // not chimeras
                    Chart.Violin<string, double, string> (nonChimeraLabels,nonChimeraAngles, null, MarkerColor:  "No Chimeras".ConvertConditionToColor(),
                        MeanLine: MeanLine.init(true,  "No Chimeras".ConvertConditionToColor()), ShowLegend: false)

                })
                .WithTitle($"{identifier} Spectral Angle Distribution (1% {label})", TitleFont: Font.init(Size: PlotlyBase.TitleSize))
                .WithYAxisStyle(Title.init("Spectral Angle", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600);
            return violin;
        }

        public static GenericChart.GenericChart IndividualFileResultBarChart(List<BulkResultCountComparisonFile> results,
            out int width, out int height, string title = "", bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            results.ForEach(p => p.Results = p.Results.OrderBy(m => m.FileName.ConvertFileName()).ToList());
            var labels = results.SelectMany(p => p.Results.Select(m => m.FileName))
                .ConvertFileNames().Distinct().ToList();


            // if results exist for one dataset but not the other, ensure they are plotted in the correct order
            foreach (var individualFile in results)
            {
                if (individualFile.Results.Count != labels.Count)
                {
                    var allResults = new List<BulkResultCountComparison>();
                    foreach (var file in labels)
                    {
                        if (individualFile.Any(p => p.FileName.ConvertFileName() == file))
                            allResults.Add(individualFile.First(p => p.FileName.ConvertFileName() == file));
                        else
                            allResults.Add(new BulkResultCountComparison()
                            {
                                FileName = file,
                                Condition = individualFile.First().Condition,
                                OnePercentPsmCount = 0,
                                OnePercentPeptideCount = 0,
                                OnePercentProteinGroupCount = 0
                            });
                    }

                    individualFile.Results = allResults;
                }
            }

            List<GenericChart.GenericChart> charts = results.Select(result =>
                Chart2D.Chart.Column<int, string, string, int, int>(result.Select(ResultSelector(resultType)), labels, null,
                    result.Results.First().Condition.ConvertConditionName(),
                    MarkerColor: result.First().Condition.ConvertConditionToColor(), 
                    MultiText: result.Select(ResultSelector(resultType)).Select(p => p.ToString()).ToArray())).ToList();

            width = Math.Max(50 * labels.Count + 10 * results.Count, 800);
            height = PlotlyBase.DefaultHeight;
            var chart = Chart.Combine(charts)
                .WithTitle($"{title} 1% FDR {Labels.GetLabel(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("File"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithSize(width, height);
            return chart;
        }

       
        public static GenericChart.GenericChart BulkResultBarChart(List<BulkResultCountComparison> results,
            bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

            List<GenericChart.GenericChart> charts = new();
            foreach (var condition in results.Select(p => p.Condition).ConvertConditionNames().Distinct())
            {
                var conditionSpecificResults = results
                    .Where(p => p.Condition.ConvertConditionName() == condition)
                    .ToList();

                // if results exist for one dataset but not the other, ensure they are plotted in the correct order
                if (conditionSpecificResults.Count != labels.Count)
                {
                    var newResults = new List<BulkResultCountComparison>();
                    foreach (var dataset in labels)
                    {
                        if (conditionSpecificResults.Any(p => p.DatasetName == dataset))
                            newResults.Add(conditionSpecificResults.First(p => p.DatasetName == dataset));
                        else
                            newResults.Add(new BulkResultCountComparison()
                            {
                                DatasetName = dataset,
                                OnePercentPsmCount = 0,
                                OnePercentPeptideCount = 0,
                                OnePercentProteinGroupCount = 0
                            });
                    }
                    conditionSpecificResults = newResults;
                }

                var conditionToWrite = condition.ConvertConditionName();
                charts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(ResultSelector(resultType)), labels, null, conditionToWrite,
                    MarkerColor: condition.ConvertConditionToColor(), MultiText: conditionSpecificResults.Select(ResultSelector(resultType)).Select(p => p.ToString()).ToArray()));
            }

            return Chart.Combine(charts).WithTitle($"1% FDR {Labels.GetLabel(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
        }


        internal static GenericChart.GenericChart GetBulkResultsDifferentFilteringPlot(
            this List<BulkResultCountComparisonMultipleFilteringTypes> results, ResultType resultType = ResultType.Psm,
            FilteringType filteringType = FilteringType.PEPQValue, bool individualFiles = false)
        {

            var condition = results.First().DatasetName;
            var values = new int[results.Count()];
            var keys = individualFiles
                ? results.Select(p => p.Condition + " " + p.FileName.ConvertFileName()).ToArray()
                : results.Select(p => p.Condition).ToArray();
            values = resultType switch
            {
                ResultType.Psm => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => p.PsmCount_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => p.PsmCount_QValue).ToArray(),
                    FilteringType.None => results.Select(p => p.PsmCount).ToArray(),
                    FilteringType.ResultsText => results.Select(p => p.PsmCount_ResultFile).ToArray(),
                    _ => values
                },
                ResultType.Peptide => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => p.ProteoformCount_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => p.ProteoformCount_QValue).ToArray(),
                    FilteringType.None => results.Select(p => p.ProteoformCount).ToArray(),
                    FilteringType.ResultsText => results.Select(p => p.ProteinGroupCount_ResultFile).ToArray(),
                    _ => values
                },
                ResultType.Protein => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => p.ProteinGroupCount_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => p.ProteinGroupCount_QValue).ToArray(),
                    FilteringType.None => results.Select(p => p.ProteinGroupCount).ToArray(),
                    FilteringType.ResultsText => results.Select(p => p.ProteinGroupCount_ResultFile).ToArray(),
                    _ => values
                },
                _ => values
            };

         

            var chart = Chart.Column<int, string, string>(values, keys, Name: condition,
                MarkerColor: condition.ConvertConditionToColor(), MultiText: values.Select(p => p.ToString()).ToArray());
            return chart;
        }


        internal static GenericChart.GenericChart GetBulkResultsDifferentFilteringPlotWIthDecoys(
           this List<BulkResultCountComparisonMultipleFilteringTypes> results, ResultType resultType = ResultType.Psm,
           FilteringType filteringType = FilteringType.PEPQValue, bool absolute = true)
        {

            var condition = results.First().DatasetName;
            var values = new double[results.Count()];
            var keys = results.Select(p => p.Condition).ToArray();
            values = resultType switch
            {
                ResultType.Psm => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => (double)p.PsmCount_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => (double)p.PsmCount_QValue).ToArray(),
                    FilteringType.None => results.Select(p => (double)p.PsmCount).ToArray(),
                    FilteringType.ResultsText => results.Select(p => (double)p.PsmCount_ResultFile).ToArray(),
                    _ => values
                },
                ResultType.Peptide => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => (double)p.ProteoformCount_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => (double)p.ProteoformCount_QValue).ToArray(),
                    FilteringType.None => results.Select(p => (double)p.ProteoformCount).ToArray(),
                    FilteringType.ResultsText => results.Select(p => (double)p.ProteinGroupCount_ResultFile).ToArray(),
                    _ => values
                },
                ResultType.Protein => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => (double)p.ProteinGroupCount_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => (double)p.ProteinGroupCount_QValue).ToArray(),
                    FilteringType.None => results.Select(p => (double)p.ProteinGroupCount).ToArray(),
                    FilteringType.ResultsText => results.Select(p => (double)p.ProteinGroupCount_ResultFile).ToArray(),
                    _ => values
                },
                _ => values
            };

            var decoyValues = new double[results.Count];
            decoyValues = resultType switch
            {
                ResultType.Psm => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => (double)p.PsmCountDecoys_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => (double)p.PsmCountDecoys_QValue).ToArray(),
                    FilteringType.None => results.Select(p => (double)p.PsmCountDecoys).ToArray(),
                    FilteringType.ResultsText => results.Select(p => 0.0).ToArray(),
                    _ => decoyValues
                },
                ResultType.Peptide => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => (double)p.ProteoformCountDecoys_PepQValue).ToArray(),
                    FilteringType.QValue => results.Select(p => (double)p.ProteoformCountDecoys_QValue).ToArray(),
                    FilteringType.None => results.Select(p => (double)p.ProteoformCountDecoys).ToArray(),
                    FilteringType.ResultsText => results.Select(p => 0.0).ToArray(),
                    _ => decoyValues
                },
                ResultType.Protein => filteringType switch
                {
                    FilteringType.PEPQValue => results.Select(p => 0.0).ToArray(),
                    FilteringType.QValue => results.Select(p => (double)p.ProteinGroupCountDecoys_QValue).ToArray(),
                    FilteringType.None => results.Select(p => (double)p.ProteinGroupCountDecoys).ToArray(),
                    FilteringType.ResultsText => results.Select(p => 0.0).ToArray(),
                    _ => decoyValues
                },
                _ => decoyValues
            };

            if (!absolute)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var total = values[i] + decoyValues[i];
                    values[i] = (values[i] / total * 100).Round(2);
                    decoyValues[i] = (decoyValues[i] / total * 100).Round(2);
                }
            }

            var chart = Chart.StackedColumn<double, string, string>(values, keys, Name: condition,
                MarkerColor: condition.ConvertConditionToColor(), MultiText: values.Select(p => p.ToString()).ToArray());
            var decoyChart = Chart.StackedColumn<double, string, string>(decoyValues, keys, Name: condition,
                MarkerColor: Color.fromKeyword(ColorKeyword.Red), MultiText: decoyValues.Select(p => p.ToString()).ToArray());
            var combined = Chart.Combine(new[] { chart, decoyChart });
                //.WithXAxisStyle(Title.init(), Side: StyleParam.Side.Bottom, Id: StyleParam.SubPlotId.NewXAxis(1));
            return combined;
        }
    }

}
