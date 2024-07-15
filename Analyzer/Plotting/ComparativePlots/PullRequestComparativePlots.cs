using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using MathNet.Numerics;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
using Chart = Plotly.NET.CSharp.Chart;
using Color = Plotly.NET.Color;
using Font = Plotly.NET.Font;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;

namespace Analyzer.Util
{
    public static partial class FileIdentifiers
    {
        public static string ComparativeResultFilteringFigure => "AllResults_ComparingPRs";
        public static string IndividualFileComparativeResultFilteringFigure => "AllResults_ComparingPRs_IndividualFile";
        public static string ComparativeFileResults_TargetDecoyAbsolute => "AllResults_ComparingPRs_TargetDecoy_Absolute";
        public static string ComparativeFileResults_TargetDecoyRelative => "AllResults_ComparingPRs_TargetDecoy_Relative";
        public static string ComparativeTopDownResults => "AllResults_TopDownSummary.png";
    }
}
namespace Analyzer.Plotting.ComparativePlots
{
    public static class PullRequestComparativePlots
    {
        public static void PlotBulkResultsDifferentFilteringTypePlotsForPullRequests(this AllResults allResults, bool individualFiles = false, bool targetDecoyStratified = false)
        {
            var chart = individualFiles
                ? allResults.IndividualFileResultCountingMultipleFilteringTypesFile.Results
                    .GetBulkResultsDifferentFilteringTypePlotsForPullRequests(individualFiles)
                : allResults.BulkResultCountComparisonMultipleFilteringTypesFile.Results
                    .GetBulkResultsDifferentFilteringTypePlotsForPullRequests(individualFiles);

            //GenericChartExtensions.Show(chart);

            var outName = individualFiles
                ? $"{DateTime.Now:yyMMdd}_{FileIdentifiers.IndividualFileComparativeResultFilteringFigure}"
                : $"{DateTime.Now:yyMMdd}_{FileIdentifiers.ComparativeResultFilteringFigure}";
            int height = individualFiles ? 3000 : 2200;
            int width = individualFiles ? 3000 : 2200;
            chart.SaveInAllResultsOnly(allResults, outName, width, height);
        }

        internal static GenericChart.GenericChart GetBulkResultsDifferentFilteringTypePlotsForPullRequests(this List<BulkResultCountComparisonMultipleFilteringTypes> results,
            bool individualFiles = false)
        {
            var chartsToGrid = new List<GenericChart.GenericChart>();
            var types = Enum.GetValues<ResultType>();
            var filters = Enum.GetValues<FilteringType>();
            for (var i = 0; i < filters.Length; i++)
            {
                var filterType = filters[i];
                for (var j = 0; j < types.Length; j++)
                {
                    var resultType = types[j];
                    var yAxisTitle = j == 0 ? filterType.ToString() : "";
                    var xAxisTitle = i == filters.Length - 1 ? resultType.ToString() : "";

                    // Generate a single plot in the 3x4 grid
                    var chartsToCombine = new List<GenericChart.GenericChart>();
                    foreach (var prRun in results.GroupBy(p => p.DatasetName))
                    {
                        var prRunChart = prRun.ToList()
                            .GetBulkResultsDifferentFilteringPlot(resultType, filterType, individualFiles)
                            .WithXAxisStyle(Title.init($"{resultType}", Side: StyleParam.Side.Top))
                            .WithLegend(false);
                        chartsToCombine.Add(prRunChart);
                        // GenericChartExtensions.Show(prRunChart);
                    }

                    var chart = Chart.Combine(chartsToCombine)
                        .WithXAxisStyle(Title.init(xAxisTitle, Side: StyleParam.Side.Top, Font: Font.init(null, 36)))
                        .WithYAxisStyle(Title.init(yAxisTitle, Font: Font.init(null, 36)))
                        .WithAxisAnchor(i, j)
                        .WithLayout(PlotlyBase.DefaultLayout)
                        .WithLegend(false);
                    chartsToGrid.Add(chart);
                    //GenericChartExtensions.Show(chart);
                }
            }

            var grid = Chart.Grid(chartsToGrid, filters.Length, types.Length)
                .WithSize(2400, 2400)
                .WithLegend(true);
            return grid;
        }

        public static void PlotBulkResultsDifferentFilteringTypePlotsForPullRequests_TargetDecoy(
            this AllResults allResults, bool absolute = true)
        {
            var chart = allResults.BulkResultCountComparisonMultipleFilteringTypesFile.Results
                .GetBulkResultsDifferentFilteringTypePlotsForPullRequests_TargetDecoy(absolute);

            //GenericChartExtensions.Show(chart);

            var outName = absolute
                ? $"{DateTime.Now:yyMMdd}_{FileIdentifiers.ComparativeFileResults_TargetDecoyAbsolute}"
                : $"{DateTime.Now:yyMMdd}_{FileIdentifiers.ComparativeFileResults_TargetDecoyRelative}";
            int height = 800 * allResults.Select(p => p.CellLine).Distinct().Count();
            int width = 2400; 
            chart.SaveInAllResultsOnly(allResults, outName, width, height);
        }

        public static GenericChart.GenericChart GetBulkResultsDifferentFilteringTypePlotsForPullRequests_TargetDecoy(
            this List<BulkResultCountComparisonMultipleFilteringTypes> results, bool absolute = true)
        {
            var chartsToGrid = new List<GenericChart.GenericChart>();
            var types = Enum.GetValues<ResultType>().SkipLast(1).ToArray();
            var filters = Enum.GetValues<FilteringType>().SkipLast(1).ToArray();
            int rows = results.Select(p => p.DatasetName).Distinct().Count();
            int columns = filters.Length * types.Length;
            int width = 450 * columns;
            int height = 650 * rows;

            int yLocation = 1;
            foreach (var prGroup in results.GroupBy(p => p.DatasetName))
            {
                for (var i = 0; i < filters.Length; i++)
                {
                    var filterType = filters[i];
                    for (var j = 0; j < types.Length; j++)
                    {
                        var resultType = types[j];
                        int xLocation = (2 * i + 1) + j;

                        string yAxisTitle = $"{resultType} {filterType}";
                        if (i == 0 && j == 0)
                            yAxisTitle = $"{prGroup.Key} - {resultType} {filterType}";

                        // Generate a single plot in the 6xn grid where n is the number of pull requests compared
                        var singlePlot = prGroup.ToList()
                            .GetBulkResultsDifferentFilteringPlotWIthDecoys(resultType, filterType, absolute)
                            .WithYAxisStyle(Title.init(yAxisTitle, Font: Font.init(null, 18)))
                            .WithAxisAnchor(xLocation, yLocation)
                            .WithLegend(false);
                        
                        chartsToGrid.Add(singlePlot);
                        //GenericChartExtensions.Show(singlePlot);
                    }
                }
                yLocation++;
            }

            
            var grid = Chart.Grid(chartsToGrid, rows, columns)
                .WithSize(width, height)
                .WithLegend(true);

            //GenericChartExtensions.Show(grid);
            return grid;
        }

        public static void PlotTopDownSummary(this AllResults allResults)
        {
            List<GenericChart.GenericChart> psmAbsoluteCharts = new();
            List<GenericChart.GenericChart> proteoformAbsoluteCharts = new();
            List<GenericChart.GenericChart> psmRelativeCharts = new();
            List<GenericChart.GenericChart> proteoformRelativeCharts = new();
            foreach (var searchSet in allResults.SelectMany(p =>
                             p.Where(run => run.Condition.Contains("TopDown") && !run.Condition.Contains("Modern"))
                                 .Cast<MetaMorpheusResult>())
                         .GroupBy(p => p.DatasetName)
                         .ToDictionary(p => p.Key,
                             p => p.Select(m => m.BulkResultCountComparisonMultipleFilteringTypesFile.First()).ToArray()))
            {
                var psmTargetValues = searchSet.Value.Select(p => p.PsmCount_PepQValue).ToArray();
                var proteoformTargetValues = searchSet.Value.Select(p => p.ProteoformCount_PepQValue).ToArray();
                var keys = searchSet.Value.Select(p => p.Condition).ToArray();

                psmAbsoluteCharts.Add(Chart.Column<int, string, int>(psmTargetValues, keys,
                    Name: searchSet.Key, MarkerColor: searchSet.Key.ConvertConditionToColor(),
                    MultiText: psmTargetValues
                ));

                proteoformAbsoluteCharts.Add(Chart.Column<int, string, int>(proteoformTargetValues, keys,
                    Name: searchSet.Key, MarkerColor: searchSet.Key.ConvertConditionToColor(),
                    MultiText: proteoformTargetValues
                ));

                var psmRelativeTargetValues = searchSet.Value.Select(p =>
                        (p.PsmCount_PepQValue / (double)(p.PsmCount_PepQValue + p.PsmCountDecoys_PepQValue) * 100).Round(2)).ToArray();
                var psmRelativeDecoyValues = searchSet.Value.Select(p =>
                        (p.PsmCountDecoys_PepQValue / (double)(p.PsmCount_PepQValue + p.PsmCountDecoys_PepQValue) * 100).Round(2)).ToArray();
                var proteoformRelativeTargetValues = searchSet.Value.Select(p =>
                        (p.ProteoformCount_PepQValue / (double)(p.ProteoformCount_PepQValue + p.ProteoformCountDecoys_PepQValue) * 100).Round(2)).ToArray();
                var proteoformRelativeDecoyValues = searchSet.Value.Select(p =>
                        (p.ProteoformCountDecoys_PepQValue / (double)(p.ProteoformCount_PepQValue + p.ProteoformCountDecoys_PepQValue) * 100).Round(2)).ToArray();
                var relativeKeys = searchSet.Value.Select(p => $"{p.Condition}: {searchSet.Key}").ToArray();

                psmRelativeCharts.Add(Chart.Combine(new []
                {
                    Chart.StackedColumn<double, string, string>(psmRelativeTargetValues, relativeKeys, 
                        MarkerColor: searchSet.Key.ConvertConditionToColor(), Name: $"{searchSet.Key} Targets",
                        MultiText: psmRelativeTargetValues.Select(p => $"{p}%").ToArray()),
                    Chart.StackedColumn<double, string, string>(psmRelativeDecoyValues, relativeKeys, 
                        MarkerColor: Color.fromKeyword(ColorKeyword.Red), Name: $"{searchSet.Key} Decoys",
                        MultiText: psmRelativeDecoyValues.Select(p => $"{p}%").ToArray())
                }));

                proteoformRelativeCharts.Add(Chart.Combine(new[]
                {
                    Chart.StackedColumn<double, string, string>(proteoformRelativeTargetValues, relativeKeys, 
                        MarkerColor: searchSet.Key.ConvertConditionToColor(), Name: $"{searchSet.Key} Targets",
                        MultiText: proteoformRelativeTargetValues.Select(p => $"{p}%").ToArray()),
                    Chart.StackedColumn<double, string, string>(proteoformRelativeDecoyValues, relativeKeys, 
                        MarkerColor: Color.fromKeyword(ColorKeyword.Red), Name: $"{searchSet.Key} Decoys",
                        MultiText: proteoformRelativeDecoyValues.Select(p => $"{p}%").ToArray())
                }));
            }

            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var tempPsmPath = System.IO.Path.Combine(dir, "tempPsm");
            var tempProteoformPath = System.IO.Path.Combine(dir, "tempProteoform");
            var tempRelpsmpath = System.IO.Path.Combine(dir, "tempRelPsm");
            var tempRelProteoformPath = System.IO.Path.Combine(dir, "tempRelProteoform");
            int width = 1000;
            int height = 700;


            var psmAbsoluteChart = Chart.Combine(psmAbsoluteCharts)
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithLegend(false)
                .WithYAxisStyle(Title.init("PSM Count", Side: StyleParam.Side.Left))
                .WithXAxisStyle(Title.init("Task Ran", Side: StyleParam.Side.Bottom));
            psmAbsoluteChart.SavePNG(tempPsmPath, null, width, height);

            var proteoformAbsoluteChart = Chart.Combine(proteoformAbsoluteCharts)
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithLegend(true)
                .WithYAxisStyle(Title.init("Proteoform Count", Side: StyleParam.Side.Left))
                .WithXAxisStyle(Title.init("Task Ran", Side: StyleParam.Side.Bottom));
            proteoformAbsoluteChart.SavePNG(tempProteoformPath, null, width, height+300);

            var psmRelativeChart = Chart.Combine(psmRelativeCharts)
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithLegend(false)
                .WithYAxisStyle(Title.init("PSM Target Decoy Ratio", Side: StyleParam.Side.Left))
                .WithXAxisStyle(Title.init("Task Ran", Side: StyleParam.Side.Bottom))
                .WithXAxis(LinearAxis.init<string, string, string, string, string, string>(false));
            psmRelativeChart.SavePNG(tempRelpsmpath, null, width, height);

            var proteoformRelativeChart = Chart.Combine(proteoformRelativeCharts)
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithLegend(false)
                .WithYAxisStyle(Title.init("Proteoform Target Decoy Ratio", Side: StyleParam.Side.Left))
                .WithXAxisStyle(Title.init("Task Ran", Side: StyleParam.Side.Bottom))
                .WithXAxis(LinearAxis.init<string, string, string, string, string, string>(false));
            proteoformRelativeChart.SavePNG(tempRelProteoformPath, null, width, height);

            tempPsmPath += ".png";
            tempProteoformPath += ".png";
            tempRelpsmpath += ".png";
            tempRelProteoformPath += ".png";

            List<Bitmap> bitMaps = new()
            {
                new Bitmap(tempRelpsmpath),
                new Bitmap(tempPsmPath),
                new Bitmap(tempRelProteoformPath),
                new Bitmap(tempProteoformPath)
            };

            var temp = StackBitmapsVertically(bitMaps.ToArray());
            temp.Save(System.IO.Path.Combine(allResults.GetChimeraPaperFigureDirectory(), FileIdentifiers.ComparativeTopDownResults), ImageFormat.Png);
            bitMaps.ForEach(p => p.Dispose());


            File.Delete(tempPsmPath);
            File.Delete(tempProteoformPath);
            File.Delete(tempRelpsmpath);
            File.Delete(tempRelProteoformPath);
        }

        public static Bitmap Combine(params Bitmap[] sources)
        {
            List<int> imageHeights = new List<int>();
            List<int> imageWidths = new List<int>();
            foreach (Bitmap img in sources)
            {
                imageHeights.Add(img.Height);
                imageWidths.Add(img.Width);
            }
            Bitmap result = new Bitmap(imageWidths.Max(), imageHeights.Max());
            using (Graphics g = Graphics.FromImage(result))
            {
                foreach (Bitmap img in sources)
                    g.DrawImage(img, Point.Empty);
            }
            return result;
        }

        public static Bitmap StackBitmapsVertically(params Bitmap[] sources)
        {
            List<int> imageHeights = new List<int>();
            List<int> imageWidths = new List<int>();
            foreach (Bitmap img in sources)
            {
                imageHeights.Add(img.Height + (imageHeights.Any() ? imageHeights.Last() : 0));
                imageWidths.Add(img.Width);
            }
            Bitmap result = new Bitmap(imageWidths.Max(), imageHeights.Max());
            using (Graphics g = Graphics.FromImage(result))
            {
                for (var index = 0; index < sources.Length; index++)
                {
                    var img = sources[index];
                    var y = index == 0 ? 0 : imageHeights[index - 1];
                    g.DrawImage(img, new Point(0, y));
                }
            }
            return result;
        }
    }
}
