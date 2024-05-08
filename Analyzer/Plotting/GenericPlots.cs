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
            _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
        };

        #endregion

        #region Defaults

        #region Plotly Things

        public static int DefaultHeight = 600;
        public static Layout DefaultLayout => Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

        private static Layout DefaultLayoutWithLegend => Layout.init<string>(
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

        private static Layout DefaultLayoutWithLegendTransparentBackground => Layout.init<string>(
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


        public static GenericChart.GenericChart IndividualFileResultBarChart(List<BulkResultCountComparisonFile> results, 
            out int width, out int height, string title = "", bool isTopDown = false, ResultType resultType = ResultType.Psm)
        {
            results.ForEach(p => p.Results = p.Results.OrderBy(m => m.FileName.ConvertFileName()).ToList());
            var labels = results.SelectMany(p => p.Results.Select(m => m.FileName))
                .ConvertFileNames().Distinct().ToList();
            Func<BulkResultCountComparison, int> selector = resultType switch
            {
                ResultType.Psm => m => m.OnePercentPsmCount,
                ResultType.Peptide => m => m.OnePercentPeptideCount,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };

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
                Chart2D.Chart.Column<int, string, string, int, int>(result.Select(selector), labels, null,
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

   


  



    }



    public static class BulkResultPlots
    {

    }
}
