using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using ResultAnalyzerUtil;
using Chart = Plotly.NET.CSharp.Chart;

namespace Analyzer.Plotting.AggregatePlots
{
    public static class SummaryPlots
    {
        public static void PlotJenkinsLikeRunSummary(this CellLineResults cellLine)
        {
            var plot = cellLine.GetJenkinsLikeRunSummary()
                .WithSize(1600, 2400);
            plot.SaveInAllResultsOnly(cellLine, $"{FileIdentifiers.PepTestingSummaryFigure}_{cellLine.CellLine}", 1600, 2400);
        }

        internal static GenericChart.GenericChart GetJenkinsLikeRunSummary(this CellLineResults cellLine)
        {
            var topDownInitial = (MetaMorpheusResult)cellLine.First(p => p.Condition.Contains("TopDown - Initial"));
            var topDownFinal = (MetaMorpheusResult)cellLine.First(p => p.Condition.Contains("TopDown - Post GPTMD"));
            var bottomUpInitial = (MetaMorpheusResult)cellLine.First(p => p.Condition.Contains("Classic - Initial"));
            var bottomUpFinal = (MetaMorpheusResult)cellLine.First(p => p.Condition.Contains("Classic - Post GPTMD"));

            var tdPsmPep =
                cellLine.BulkResultCountComparisonMultipleFilteringTypesFile.Results
                    .GetBulkResultsDifferentFilteringPlotWIthDecoys(ResultType.Psm, FilteringType.PEPQValue, false)
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.47)),
                        StyleParam.SubPlotId.NewXAxis(1))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.67, 0.98)),
                        StyleParam.SubPlotId.NewYAxis(1));
            var tdPepPep =
                cellLine.BulkResultCountComparisonMultipleFilteringTypesFile.Results
                    .GetBulkResultsDifferentFilteringPlotWIthDecoys(ResultType.Peptide, FilteringType.PEPQValue, false)
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.52, 1.00)),
                        StyleParam.SubPlotId.NewXAxis(2))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.67, 0.98)),
                        StyleParam.SubPlotId.NewYAxis(2));

            var topDownInitialCbPsm = topDownInitial.ChimeraBreakdownFile.Results.GetChimeraBreakDownStackedColumn(ResultType.Psm, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.23)),
                    StyleParam.SubPlotId.NewXAxis(3))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.49, 0.65)),
                    StyleParam.SubPlotId.NewYAxis(3));

            var topDownInitialCbPep = topDownInitial.ChimeraBreakdownFile.Results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.23)),
                    StyleParam.SubPlotId.NewXAxis(4))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.32, 0.48)),
                    StyleParam.SubPlotId.NewYAxis(4));

            var topDownInitialPep = topDownInitial.GetPepFeaturesScatterGrid()
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.27, 0.49)),
                    StyleParam.SubPlotId.NewXAxis(5))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.32, 0.65)),
                    StyleParam.SubPlotId.NewYAxis(5));

            var topDownFinalCbPsm = topDownFinal.ChimeraBreakdownFile.Results
                .GetChimeraBreakDownStackedColumn(ResultType.Psm, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.51, 0.73)),
                    StyleParam.SubPlotId.NewXAxis(6))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.49, 0.65)),
                    StyleParam.SubPlotId.NewYAxis(6));

            var topDownFinalCbPep = topDownFinal.ChimeraBreakdownFile.Results
                .GetChimeraBreakDownStackedColumn(ResultType.Peptide, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.51, 0.73)),
                    StyleParam.SubPlotId.NewXAxis(7))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.32, 0.48)),
                    StyleParam.SubPlotId.NewYAxis(7));

            var topDownFinalPep = topDownFinal.GetPepFeaturesScatterGrid()
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.77, 0.98)),
                    StyleParam.SubPlotId.NewXAxis(8))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.32, 0.65)),
                    StyleParam.SubPlotId.NewYAxis(8));

            var bottomUpCBPsm = bottomUpInitial.ChimeraBreakdownFile.Results
                .GetChimeraBreakDownStackedColumn(ResultType.Psm, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.23)),
                    StyleParam.SubPlotId.NewXAxis(9))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.175, 0.31)),
                    StyleParam.SubPlotId.NewYAxis(9));

            var bottomUpCbPep = bottomUpInitial.ChimeraBreakdownFile.Results
                .GetChimeraBreakDownStackedColumn(ResultType.Peptide, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.00, 0.23)),
                    StyleParam.SubPlotId.NewXAxis(10))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.0, 0.155)),
                    StyleParam.SubPlotId.NewYAxis(10));

            var bottomUpInitialPep = bottomUpInitial.GetPepFeaturesScatterGrid()
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.27, 0.48)),
                    StyleParam.SubPlotId.NewXAxis(11))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.0, 0.32)),
                    StyleParam.SubPlotId.NewYAxis(11));

            var bottomUpFinalCbPsm = bottomUpFinal.ChimeraBreakdownFile.Results
                .GetChimeraBreakDownStackedColumn(ResultType.Psm, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.51, 0.73)),
                    StyleParam.SubPlotId.NewXAxis(12))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.165, 0.31)),
                    StyleParam.SubPlotId.NewYAxis(12));

            var bottomUpFinalCbPep = bottomUpFinal.ChimeraBreakdownFile.Results
                .GetChimeraBreakDownStackedColumn(ResultType.Peptide, true, out _)
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.51, 0.73)),
                    StyleParam.SubPlotId.NewXAxis(13))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.0, 0.155)),
                    StyleParam.SubPlotId.NewYAxis(13));

            var bottomUpFinalPep = bottomUpFinal.GetPepFeaturesScatterGrid()
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.77, 0.98)),
                    StyleParam.SubPlotId.NewXAxis(14))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(Domain: StyleParam.Range.NewMinMax(0.0, 0.32)),
                    StyleParam.SubPlotId.NewYAxis(14));

            

            var grid = Chart.Grid(new[]
                {
                    tdPsmPep, tdPepPep, topDownInitialCbPsm, topDownInitialCbPep, topDownInitialPep, topDownFinalCbPsm,
                    topDownFinalCbPep, topDownFinalPep, bottomUpCBPsm, bottomUpCbPep, bottomUpInitialPep,
                    bottomUpFinalCbPsm, bottomUpFinalCbPep, bottomUpFinalPep
                }, 8, 10, YGap: 50)
                .WithTitle($"{cellLine.CellLine} Result Summary");
            return grid;
        }
    }
}
