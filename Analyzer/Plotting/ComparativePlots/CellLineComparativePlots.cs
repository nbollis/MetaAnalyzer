using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;

namespace Analyzer.Plotting.ComparativePlots
{
    public static class CellLineComparativePlots
    {
        public static void PlotIndividualFileResults(this CellLineResults cellLine, ResultType? resultType = null,
            string? outputDirectory = null, bool filterByCondition = true)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            resultType ??= isTopDown ? ResultType.Psm : ResultType.Peptide;


            string outPath = $"{FileIdentifiers.IndividualFileComparisonFigure}_{resultType}_{cellLine.CellLine}";
            var chart = cellLine.GetIndividualFileResultsBarChart(out int width, out int height, resultType.Value, filterByCondition);
            chart.SaveInCellLineOnly(cellLine, outPath, width, height);
        }

        public static GenericChart.GenericChart GetIndividualFileResultsBarChart(this CellLineResults cellLine, out int width,
            out int height, ResultType resultType = ResultType.Psm, bool filterByCondition = true)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            var filteredResults = (filterByCondition ? cellLine.Select(p => p.IndividualFileComparisonFile)
                        .Where(p => p != null && p.Any() && isTopDown.GetIndividualFileComparisonSelector(cellLine.CellLine).Contains(p.First().Condition))
                    : cellLine.Select(p => p.IndividualFileComparisonFile));
            var fileResults = filteredResults
                .Where(p => p is not null)
                .OrderBy(b => b.First().Condition.ConvertConditionName())
                .ToList();

            return GenericPlots.IndividualFileResultBarChart(fileResults, out width, out height, cellLine.CellLine,
                isTopDown, resultType);
        }

        public static GenericChart.GenericChart GetIndividualFileResultsBarChart(this List<BulkResultCountComparisonFile> fileResults, out int width,
            out int height, ResultType resultType = ResultType.Psm,  string cellLine = "", bool isTopDown = false)

        {
            return GenericPlots.IndividualFileResultBarChart(fileResults, out width, out height, cellLine, isTopDown, resultType);
        }


    }
}
