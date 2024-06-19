using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;

namespace Analyzer.Util
{
    public static partial class FileIdentifiers
    {
        public static string ComparativeResultFilteringFigure => "AllResults_ComparingPRs";
        public static string IndividualFileComparativeResultFilteringFigure => "AllResults_ComparingPRs_IndividualFile";
        public static string ComparativeFileResults_TargetDecoyAbsolute => "AllResults_ComparingPRs_TargetDecoy_Absolute";
        public static string ComparativeFileResults_TargetDecoyRelative => "AllResults_ComparingPRs_TargetDecoy_Relative";
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
            int height = 650 * allResults.Select(p => p.DatasetName).Distinct().Count();
            int width = 450 * 6; 
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

    }
}
