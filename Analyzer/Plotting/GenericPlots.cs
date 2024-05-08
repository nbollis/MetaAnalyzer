using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Util;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;

namespace Analyzer.Plotting
{
    

    public static class GenericPlots
    {
        #region Labels

        public static string SpectralMatchLabel(bool isTopDown) => isTopDown ? "PrSMs" : "PSMs";
        public static string ResultLabel(bool isTopDown) => isTopDown ? "Proteoforms" : "Peptides";

        public static string Label(bool isTopDown, ResultType resultType) => resultType switch
        {
            ResultType.Psm => SpectralMatchLabel(isTopDown),
            ResultType.Peptide => ResultLabel(isTopDown),
            ResultType.Protein => "Proteins",
            _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
        };

        #endregion

        #region Defaults

        #region Plotly Things

        public static int DefaultHeight = 600;
        public static Layout DefaultLayout => Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

        public static Layout DefaultLayoutWithLegend => Layout.init<string>(
            //PaperBGColor: Color.fromARGB(0, 0,0,0),
            //PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                VerticalAlign: StyleParam.VerticalAlign.Bottom,
                XAnchor: StyleParam.XAnchorPosition.Center,
                YAnchor: StyleParam.YAnchorPosition.Top
            ));

        public static Layout DefaultLayoutWithLegendTransparentBackground => Layout.init<string>(
            PaperBGColor: Color.fromARGB(0, 0, 0, 0),
            PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            ShowLegend: true,
            Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                VerticalAlign: StyleParam.VerticalAlign.Bottom,
                XAnchor: StyleParam.XAnchorPosition.Center,
                YAnchor: StyleParam.YAnchorPosition.Top
            ));

        #endregion


        #endregion

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

            width = 50 * labels.Count + 10 * results.Count;
            height = DefaultHeight;
            var chart = Chart.Combine(charts)
                .WithTitle($"{title} 1% FDR {Label(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("File"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend)
                .WithSize(width, height);
            return chart;
        }


        public static GenericChart.GenericChart SpectralAngleChimeraComparisonViolinPlot(double[] chimeraAngles,
            double[] nonChimeraAngles, string identifier = "", bool isTopDown = false)
        {
            var chimeraLabels = Enumerable.Repeat("Chimeras", chimeraAngles.Length).ToArray();
            var nonChimeraLabels = Enumerable.Repeat("No Chimeras", nonChimeraAngles.Length).ToArray();
            string resultType = ResultLabel(isTopDown);
            var violin = Chart.Combine(new[]
                {
                    // chimeras
                    Chart.Violin<string, double, string> (chimeraLabels,chimeraAngles, null, MarkerColor:  PlottingTranslators.ConditionToColorDictionary["Chimeras"],
                        MeanLine: MeanLine.init(true,  "Chimeras".ConvertConditionToColor()), ShowLegend: false), 
                    // not chimeras
                    Chart.Violin<string, double, string> (nonChimeraLabels,nonChimeraAngles, null, MarkerColor:  PlottingTranslators.ConditionToColorDictionary["No Chimeras"],
                        MeanLine: MeanLine.init(true,  "No Chimeras".ConvertConditionToColor()), ShowLegend: false)

                })
                .WithTitle($"{identifier} Spectral Angle Distribution (1% {resultType})")
                .WithYAxisStyle(Title.init("Spectral Angle"))
                .WithLayout(DefaultLayout)
                .WithSize(1000, 600);
            return violin;
        }

        public static GenericChart.GenericChart BulkResultBarChart(List<BulkResultCountComparison> results,
            bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();

            List<GenericChart.GenericChart> charts = new();
            foreach (var condition in results.Select(p => p.Condition).Distinct())
            {
                var conditionSpecificResults = results.Where(p => p.Condition == condition).ToList();

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

            return Chart.Combine(charts).WithTitle($"1% FDR {Label(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Cell Line"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(DefaultLayoutWithLegend);
        }




    }

}
