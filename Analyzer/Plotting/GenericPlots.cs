using Analyzer.FileTypes.Internal;
using Analyzer.Util;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.TraceObjects;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Omics.SpectrumMatch;
using Analyzer.Plotting.Util;

namespace Analyzer.Plotting
{


    public static class GenericPlots
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
                    MarkerColor: result.First().Condition.ConvertConditionToColor())).ToList();

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

        public static GenericChart.GenericChart SpectralAngleChimeraComparisonViolinPlot(double[] chimeraAngles,
            double[] nonChimeraAngles, string identifier = "", bool isTopDown = false)
        {
            var chimeraLabels = Enumerable.Repeat("Chimeras", chimeraAngles.Length).ToArray();
            var nonChimeraLabels = Enumerable.Repeat("No Chimeras", nonChimeraAngles.Length).ToArray();
            string resultType = Labels.GetSpectrumMatchLabel(isTopDown);
            var violin = Chart.Combine(new[]
                {
                    // chimeras
                    Chart.Violin<string, double, string> (chimeraLabels,chimeraAngles, null, MarkerColor: "Chimeras".ConvertConditionToColor(),
                        MeanLine: MeanLine.init(true,  "Chimeras".ConvertConditionToColor()), ShowLegend: false), 
                    // not chimeras
                    Chart.Violin<string, double, string> (nonChimeraLabels,nonChimeraAngles, null, MarkerColor:  "No Chimeras".ConvertConditionToColor(),
                        MeanLine: MeanLine.init(true,  "No Chimeras".ConvertConditionToColor()), ShowLegend: false)

                })
                .WithTitle($"{identifier} Spectral Angle Distribution (1% {resultType})")
                .WithYAxisStyle(Title.init("Spectral Angle"))
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600);
            return violin;
        }

        public static GenericChart.GenericChart BulkResultBarChart(List<BulkResultCountComparison> results,
            bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

            List<GenericChart.GenericChart> charts = new();
            foreach (var condition in results.Select(p => p.Condition).ConvertConditionNames().Distinct())
            {
                var conditionSpecificResults = results.Where(p => p.Condition.ConvertConditionName() == condition).ToList();

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
                    MarkerColor: condition.ConvertConditionToColor()));
            }

            return Chart.Combine(charts).WithTitle($"1% FDR {Labels.GetLabel(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
        }


        internal static GenericChart.GenericChart KernelDensityPlot(List<double> values, string title,
            string xTitle = "", string yTitle = "", double bandwidth = 0.2, Kernels kernel = Kernels.Gaussian)
        {
            List<(double, double)> data = new List<(double, double)>();

            foreach (var sample in values.DistinctBy(p => p.Round(3)))
            {
                var pdf = kernel switch
                {
                    Kernels.Gaussian => KernelDensity.EstimateGaussian(sample, bandwidth, values),
                    Kernels.Epanechnikov => KernelDensity.EstimateEpanechnikov(sample, bandwidth, values),
                    Kernels.Triangular => KernelDensity.EstimateTriangular(sample, bandwidth, values),
                    Kernels.Uniform => KernelDensity.EstimateUniform(sample, bandwidth, values),
                    _ => throw new ArgumentOutOfRangeException(nameof(kernel), kernel, null)
                };
                data.Add((sample, pdf));
            }

            var chart =
                Chart.Line<double, double, string>(data.Select(p => p.Item1), data.Select(p => p.Item2), Name: title,
                        LineColor: title.ConvertConditionToColor())
                    .WithSize(400, 400)
                    .WithXAxisStyle(Title.init(xTitle)/*, new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-15, 15))*/)
                    .WithYAxisStyle(Title.init(yTitle))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            return chart;
        }

        internal static GenericChart.GenericChart Histogram2D<T>(List<T> xValues, List<double> yValues, string title,
            string xTitle = "", string yTitle = "", bool normalizeColumns = false) where T : IConvertible
        {
            var zValues = default(Plotly.NET.CSharp.Optional<IEnumerable<IEnumerable<double>>>);
            if (normalizeColumns)
            {
                if (xValues.Count != yValues.Count)
                    goto NoNorm;

                // combine values and keys
                var combined = new (T, double)[xValues.Count];
                for (int i = 0; i < xValues.Count; i++)
                    combined[i] = (xValues[i], yValues[i]);

                // group by keys and adjust values to be a percentage of total in group
                zValues = new Plotly.NET.CSharp.Optional<IEnumerable<IEnumerable<double>>>(combined.GroupBy(p => p.Item1)
                    .Select(group =>
                        group.Select(p => Math.Sign(p.Item2) * (Math.Abs(p.Item2) / group.Max(m => Math.Abs(m.Item2))))), true);

            }


        NoNorm:
            var chart = Chart.Histogram2DContour<T, double, double>(xValues, yValues, Z: zValues, YBins: Bins.init(null, null, 0.1)
                    /*HistNorm: StyleParam.HistNorm.Percent*//*, HistFunc: StyleParam.HistFunc.Avg*/)
                //var chart = Chart.BoxPlot<T, double, string>(xValues, yValues, Name: title, MarkerColor: title.ConvertConditionToColor()
                //BoxWidth: 4, MeanLine: MeanLine.init(true, title.ConvertConditionToColor()), Points: StyleParam.BoxPoints.False)
                .WithSize(400, 400)
                .WithTitle(title)
                .WithXAxisStyle(Title.init(xTitle))
                .WithYAxisStyle(Title.init(yTitle))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithSize(800, 800);
            return chart;
        }

        internal static GenericChart.GenericChart ModificationDistribution(List<string> fullSequences, string title,
            string xTitle = "", string yTitle = "", bool displayCarbamimidoMethyl = false, bool displayRelative = true)
        {
            var modDict = new Dictionary<string, double>();
            foreach (var mod in fullSequences.SelectMany(p =>
                         SpectrumMatchFromTsv.ParseModifications(p).SelectMany(m => m.Value)
                             .Select(mod => mod.Split(":")[1])))
            {
                if (!modDict.TryAdd(mod, 1))
                {
                    modDict[mod]++;
                }
            }

            if (!displayCarbamimidoMethyl)
            {
                modDict.Remove("Carbamidomethyl on C");
                modDict.Remove("Carbamidomethyl on U");
            }

            if (displayRelative)
            {
                var modCount = modDict.Sum(p => p.Value);
                foreach (var keyValuePair in modDict)
                {
                    modDict[keyValuePair.Key] = keyValuePair.Value / modCount * 100.0;
                }
            }

            // remove anything where the mod is less than 1% of total modifications
            modDict = modDict.Where(p => p.Value > 1)
                .ToDictionary(p => p.Key, p => p.Value);

            var chart = Chart.Column<double, string, string>(modDict.Values, modDict.Keys, title, MarkerColor: title.ConvertConditionToColor())
                .WithSize(400, 400)
                .WithTitle(title)
                .WithXAxisStyle(Title.init(xTitle))
                .WithYAxisStyle(Title.init(yTitle))
                .WithLayout(PlotlyBase.DefaultLayout);
            return chart;
        }

        internal static GenericChart.GenericChart GetBulkResultsDifferentFilteringPlot_ColumnChartByTask(
            this List<BulkResultCountComparisonMultipleFilteringTypes> results, ResultType resultType = ResultType.Psm,
            FilteringType filteringType = FilteringType.PEPQValue)
        {

            var condition = results.First().DatasetName;
            var values = new int[results.Count()];
            var keys = results.Select(p => p.Condition).ToArray();
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

            var chart = Chart.Column<int, string, string>(values, keys, Name: condition, MarkerColor: condition.ConvertConditionToColor());
            return chart;
        }

    }

}
