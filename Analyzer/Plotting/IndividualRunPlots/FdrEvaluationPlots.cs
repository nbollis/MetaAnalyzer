using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Microsoft.FSharp.Core;
using Plotly.NET;
using Proteomics.PSM;
using Chart = Plotly.NET.CSharp.Chart;


namespace Analyzer.Util
{
    public static partial class FileIdentifiers
    {
        public static string SpectralAngleFigure => "SpectralAngleComparison";
        public static string PepGridChartFigure => "PepFeatureAnalysis";
        public static string TargetDecoyCurve => "TargetDecoyCurve";
    }
}

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
        /// <param name="chimeraStratified"></param>
        public static void PlotTargetDecoyCurves(this MetaMorpheusResult results,
            ResultType? resultType = null, TargetDecoyCurveMode? mode = null, bool chimeraStratified = false)
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

            string tail = chimeraStratified ? "_ChimeraStratified" : "";

            foreach (var plotParams in plotsToRun)
            {
                var plot = results.GetTargetDecoyCurve(plotParams.Item1, plotParams.Item2, chimeraStratified);
                var outName = $"{FileIdentifiers.TargetDecoyCurve}_{results.DatasetName}_{results.Condition}_{Labels.GetLabel(results.IsTopDown, plotParams.Item1)}_{plotParams.Item2}{tail}";
                plot.SaveInRunResultOnly(results, outName, 800, 800);
            }
        }

        public static GenericChart.GenericChart GetTargetDecoyCurve(this MetaMorpheusResult results, ResultType resultType, TargetDecoyCurveMode mode, bool chimeraStratified = false)
        {
            var allResults = resultType switch
            {
                ResultType.Psm => results.AllPsms,
                ResultType.Peptide => results.AllPeptides,
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };
            return chimeraStratified
                ? allResults.GetTargetDecoyCurveChimeraStratified(resultType, mode, results.DatasetName, results.Condition, results.IsTopDown) 
                : allResults.GetTargetDecoyCurve(resultType, mode, results.DatasetName, results.Condition, results.IsTopDown);
        }

        public static GenericChart.GenericChart GetTargetDecoyCurve(this List<PsmFromTsv> allResults, ResultType resultType, TargetDecoyCurveMode mode, string datasetName, 
            string condition, bool isTopDown = true)
        {
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
                .WithTitle($"{datasetName} {condition} {Labels.GetLabel(isTopDown, resultType)} by {mode}")
                .WithSize(600, 400)
                .WithXAxisStyle(Title.init($"MetaMorpheus {mode}"), new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, xValues.Max())))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

            return targetDecoyChart;
        }

        public static GenericChart.GenericChart GetTargetDecoyCurveChimeraStratified(this List<PsmFromTsv> allResults,
            ResultType resultType, TargetDecoyCurveMode mode, string datasetName, string condition, bool isTopDown = true)
        {
            IEnumerable<IGrouping<double, PsmFromTsv>>? binnedChimericResults;
            IEnumerable<IGrouping<double, PsmFromTsv>>? binnedNonchimericResults;

            var chimeraGroupedDictionary = allResults.ToChimeraGroupedDictionary();
            var chimeric = chimeraGroupedDictionary.Where(p => p.Key != 1).SelectMany(p => p.Value).ToList();
            var nonChimeric = chimeraGroupedDictionary[1];

            switch (mode)
            {
                case TargetDecoyCurveMode.Score:
                    binnedChimericResults = chimeric.GroupBy(p => Math.Floor(p.Score));
                    binnedNonchimericResults = nonChimeric.GroupBy(p => Math.Floor(p.Score));
                    break;
                case TargetDecoyCurveMode.QValue: // from 0 to 1 in 100 bins
                    binnedChimericResults = chimeric.GroupBy(p => Math.Floor(p.QValue * 100));
                    binnedNonchimericResults = nonChimeric.GroupBy(p => Math.Floor(p.QValue * 100));
                    break;
                case TargetDecoyCurveMode.PepQValue: // from 0 to 1 in 100 bins
                    binnedChimericResults = chimeric.GroupBy(p => Math.Floor(p.PEP_QValue * 100));
                    binnedNonchimericResults = nonChimeric.GroupBy(p => Math.Floor(p.PEP_QValue * 100));
                    break;
                case TargetDecoyCurveMode.Pep: // from 0 to 1 in 100 bins
                    binnedChimericResults = chimeric.GroupBy(p => Math.Floor(p.PEP * 100));
                    binnedNonchimericResults = nonChimeric.GroupBy(p => Math.Floor(p.PEP * 100));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }


            var chimericResultsDict = binnedChimericResults.OrderBy(p => p.Key).ToDictionary(p => p.Key,
                p => (p.Count(sm => sm.IsDecoy()), p.Count(sm => !sm.IsDecoy())));
            var chimericXValues = mode == TargetDecoyCurveMode.Score ? chimericResultsDict.Keys.ToArray() : chimericResultsDict.Keys.Select(p => p / 1000.0).ToArray();
            var chimericTargetValues = mode == TargetDecoyCurveMode.Score
                ? chimericResultsDict.Values.Select(p => p.Item2).ToArray()
                : chimericResultsDict.Values.Select(p => p.Item2 / 100).ToArray();
            var chimericDecoyValues = mode == TargetDecoyCurveMode.Score
                ? chimericResultsDict.Values.Select(p => p.Item1).ToArray()
                : chimericResultsDict.Values.Select(p => p.Item1 / 100).ToArray();

            var nonChimericResultsDict = binnedNonchimericResults.OrderBy(p => p.Key).ToDictionary(p => p.Key,
                               p => (p.Count(sm => sm.IsDecoy()), p.Count(sm => !sm.IsDecoy())));
            var nonChimericXValues = mode == TargetDecoyCurveMode.Score ? nonChimericResultsDict.Keys.ToArray() : nonChimericResultsDict.Keys.Select(p => p / 1000.0).ToArray();
            var nonChimericTargetValues = mode == TargetDecoyCurveMode.Score
                ? nonChimericResultsDict.Values.Select(p => p.Item2).ToArray()
                : nonChimericResultsDict.Values.Select(p => p.Item2 / 100).ToArray();
            var nonChimericDecoyValues = mode == TargetDecoyCurveMode.Score 
                ? nonChimericResultsDict.Values.Select(p => p.Item1).ToArray() 
                : nonChimericResultsDict.Values.Select(p => p.Item1 / 100).ToArray();

            var targetDecoyChart = Chart.Combine(new[]
                {
                    Chart.Spline<double, int, string>(chimericXValues, chimericTargetValues, Name: "Chimeric Targets", LineColor: "Chimeric".ConvertConditionToColor()),
                    Chart.Spline<double, int, string>(chimericXValues, chimericDecoyValues, Name: "Chimeric Decoys", LineColor: "Non-Chimeric".ConvertConditionToColor()),
                    Chart.Spline<double, int, string>(nonChimericXValues, nonChimericTargetValues, Name: "Non-Chimeric Targets", LineColor: Color.fromKeyword(ColorKeyword.Green)),
                    Chart.Spline<double, int, string>(nonChimericXValues, nonChimericDecoyValues, Name: "Non-Chimeric Decoys", LineColor: Color.fromKeyword(ColorKeyword.YellowGreen)),
                })
                .WithTitle($"{datasetName} {condition} {Labels.GetLabel(isTopDown, resultType)} by {mode}")
                .WithSize(600, 400)
                .WithXAxisStyle(Title.init($"MetaMorpheus {mode}"), new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, chimericXValues.Concat(nonChimericXValues).Max())))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

            return targetDecoyChart;
        }

        /// <summary>
        /// Exports a grid of scatter plots of the ratio of targets/total results for each PEP training feature
        /// </summary>
        /// <param name="results"></param>
        /// <param name="condition"></param>
        public static void PlotPepFeaturesScatterGrid(this MetaMorpheusResult results, string? condition = null)
        {
            string exportPath = $"{FileIdentifiers.PepGridChartFigure}_{results.DatasetName}_{condition ?? results.Condition}";
            var plot = results.GetPepFeaturesScatterGrid(condition);
            plot.SaveInRunResultOnly(results, exportPath, 800, 800);
        }

        internal static GenericChart.GenericChart GetPepFeaturesScatterGrid(this MetaMorpheusResult results,
            string? condition = null)
        {
            string pepForPercolatorPath = Directory.GetFiles(results.DirectoryPath, "*.tab", SearchOption.AllDirectories).First();
            var plot = new PepEvaluationPlot(pepForPercolatorPath).PepChart;
            return plot;
        }

        
    }
}
