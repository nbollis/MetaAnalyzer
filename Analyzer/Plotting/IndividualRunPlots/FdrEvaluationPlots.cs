using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Proteomics.PSM;
using Chart = Plotly.NET.CSharp.Chart;

namespace Analyzer.Plotting.IndividualRunPlots
{
    public static class FdrEvaluationPlots
    {
        /// <summary>
        /// Plots the target decoy curve(s) for the dataset, if result type or mode is left null, it will perform all
        /// </summary>
        /// <param name="results"></param>
        /// <param name="resultType"></param>
        /// <param name="mode"></param>
        public static void PlotTargetDecoyCurves(this MetaMorpheusResult results,
            ResultType? resultType = null, TargetDecoyCurveMode? mode = null)
        {
            List<(ResultType, TargetDecoyCurveMode)> plotsToRun = new List<(ResultType, TargetDecoyCurveMode)>();


            if (mode != null) // mode is selected
            {
                if (resultType != null)
                    plotsToRun.Add((resultType.Value, mode.Value));
                else
                {
                    plotsToRun.Add((ResultType.Psm, mode.Value));
                    plotsToRun.Add((ResultType.Peptide, mode.Value));
                }
            }
            else // mode is not selected
            {
                if (resultType != null)
                    plotsToRun.AddRange(Enum.GetValues<TargetDecoyCurveMode>().Select(m => (resultType.Value, m)));
                else
                {
                    plotsToRun.AddRange(Enum.GetValues<TargetDecoyCurveMode>().Select(m => (ResultType.Psm, m)));
                    plotsToRun.AddRange(Enum.GetValues<TargetDecoyCurveMode>().Select(m => (ResultType.Peptide, m)));
                }
            }

            foreach (var plotParams in plotsToRun)
            {
                var plot = results.CreateTargetDecoyCurve(plotParams.Item1, plotParams.Item2);
                string outPath = Path.Combine(results.FigureDirectory,
                                       $"{FileIdentifiers.TargetDecoyCurve}_{results.DatasetName}_{results.Condition}_{Labels.GetLabel(results.IsTopDown, plotParams.Item1)}_{plotParams.Item2}");
                plot.SavePNG(outPath, null, 600, 400);
            }
        }

        internal static GenericChart.GenericChart CreateTargetDecoyCurve(this MetaMorpheusResult results, ResultType resultType, TargetDecoyCurveMode mode)
        {
            var allResults = resultType switch
            {
                ResultType.Psm => results.AllPsms,
                ResultType.Peptide => results.AllPeptides,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };

            IEnumerable<IGrouping<double, PsmFromTsv>>? binnedResults;
            switch (mode)
            {
                case TargetDecoyCurveMode.Score:
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.Score));
                    break;
                case TargetDecoyCurveMode.QValue: // from 0 to 1 in 100 bins
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.QValue * 100));
                    break;
                case TargetDecoyCurveMode.PepQValue: // from 0 to 1 in 100 bins
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.PEP_QValue * 100));
                    break;
                case TargetDecoyCurveMode.Pep: // from 0 to 1 in 100 bins
                    binnedResults = allResults.GroupBy(p => Math.Floor(p.PEP * 100));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            var resultDict = binnedResults.OrderBy(p => p.Key).ToDictionary(p => p.Key,
                p => (p.Count(sm => sm.IsDecoy()), p.Count(sm => !sm.IsDecoy())));
            var xValues = mode == TargetDecoyCurveMode.Score ? resultDict.Keys.ToArray() : resultDict.Keys.Select(p => p / 1000.0).ToArray();
            var targetValues = mode == TargetDecoyCurveMode.Score
                ? resultDict.Values.Select(p => p.Item2).ToArray()
                : resultDict.Values.Select(p => p.Item2 / 100).ToArray();
            var decoyValues = mode == TargetDecoyCurveMode.Score
                ? resultDict.Values.Select(p => p.Item1).ToArray()
                : resultDict.Values.Select(p => p.Item1 / 100).ToArray();

            var targetDecoyChart = Chart.Combine(new[]
                {
                    Chart.Spline<double, int, string>(xValues, targetValues, Name: "Targets"),
                    Chart.Spline<double, int, string>(xValues, decoyValues, Name: "Decoys"),
                })
                .WithTitle($"{results.DatasetName} {results.Condition} {Labels.GetLabel(results.IsTopDown, resultType)} by {mode}")
                .WithSize(600, 400)
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);


            return targetDecoyChart;
        }
    }
}
