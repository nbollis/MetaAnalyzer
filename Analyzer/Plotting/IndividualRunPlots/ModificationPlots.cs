using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
using Plotly.NET;
using Plotting.Util;
using ResultAnalyzerUtil;
using Chart = Plotly.NET.CSharp.Chart;
using Plotting;

namespace Analyzer.Plotting.IndividualRunPlots
{
    public static class ModificationPlots
    {
        public static void PlotModificationDistribution(this CellLineResults cellLine,
            ResultType resultType = ResultType.Psm, bool filterByCondition = true)
        {
            bool isTopDown = cellLine.First().IsTopDown;
            var fileResults = (filterByCondition
                    ? cellLine.Select(p => p)
                        .Where(p => isTopDown.GetSingleResultSelector(cellLine.CellLine).Contains(p.Condition))
                    : cellLine.Select(p => p))
                .OrderBy(p => p.Condition.ConvertConditionName())
                .ToList();
            string resultTypeLabel = Labels.GetLabel(isTopDown, resultType);

            foreach (var bulkResult in fileResults.Where(p => p is MetaMorpheusResult))
            {
                var result = (MetaMorpheusResult)bulkResult;
                var chart = result.GetModificationDistribution(resultType, cellLine.CellLine, isTopDown);

                var outName = $"{FileIdentifiers.ModificationDistributionFigure}_{resultTypeLabel}_{cellLine.CellLine}";
                if (filterByCondition)
                    chart.SaveInCellLineAndMann11Directories(cellLine, outName, 800, 600);
                else
                    chart.SaveInCellLineOnly(cellLine, outName, 800, 600);
            }
        }

        /// <summary>
        ///
        /// assumes ISpectralMatch full sequences are in MetaMorpheus style
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="conditionLabel"></param>
        /// <returns></returns>
        public static GenericChart.GenericChart PlotModificationDistribution(this List<ProteinCountingRecord> matches, ResultType resultType = ResultType.Psm, bool isTopDown = false)
        {

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var conditionGroup in matches
                         .Where(p => p is { UniqueFullSequences: > 1, UniqueBaseSequences: > 1 })
                         .GroupBy(p => p.Condition.ConvertConditionName()))
            {
                string condition = conditionGroup.Key;
                var fullSequences = conditionGroup.SelectMany(p => p.FullSequences);
                if (resultType is ResultType.Peptide)
                    fullSequences = fullSequences.GroupBy(p => p).Select(p => p.First());
                

                var plot = GenericPlots.ModificationDistribution(fullSequences.ToList(), condition, "Modification", "Count", false, false);
                toCombine.Add(plot);
            }

            var finalPlot = Chart.Combine(toCombine)
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)}  Modification Distribution")
                .WithSize(900, 600)
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

            return finalPlot;
        }

        public static GenericChart.GenericChart GetModificationDistribution(this List<SingleRunResults> runResults,
            ResultType resultType = ResultType.Psm, bool isTopDown = false)
        {
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var groupResult in runResults.GroupBy(p => p.Condition.ConvertConditionName()))
            {
                List<string> fullSequences = new();
                foreach (var runResult in groupResult)
                    switch (runResult)
                    {
                        case ProteomeDiscovererResult proteomeDiscovererResult:
                            if (resultType is ResultType.Psm)
                            {
                                var sequences = proteomeDiscovererResult.PrsmFile
                                    .Where(p => p.PassesConfidenceFilter)
                                    .Select(p => p.FullSequence).ToList();
                                fullSequences.AddRange(sequences);
                            }
                            else
                            {
                                var sequences = proteomeDiscovererResult.PrsmFile
                                    .Where(p => p.PassesConfidenceFilter)
                                    .GroupBy(p => p.FullSequence)
                                    .Select(p => p.First().FullSequence)
                                    .ToList();
                                fullSequences.AddRange(sequences);
                            }
                            break;

                        case MetaMorpheusResult metaMorpheusResult:
                            if (resultType is ResultType.Psm)
                            {
                                var sequences = metaMorpheusResult.IndividualFileResults
                                    .SelectMany(p => p.FilteredPsms)
                                    .Select(p => p.FullSequence)
                                    .ToList();
                                fullSequences.AddRange(sequences);
                            }
                            else
                            {
                                var sequences = metaMorpheusResult.IndividualFileResults
                                    .SelectMany(p => p.AllPeptides)
                                    .Where(pep => pep.PassesConfidenceFilter())
                                    .GroupBy(p => p.FullSequence)
                                    .Select(p => p.First().FullSequence)
                                    .ToList();
                                fullSequences.AddRange(sequences);
                            }
                            break;

                        case MsFraggerResult msFraggerResult:
                            if (resultType is ResultType.Psm)
                            {
                                var sequences = msFraggerResult.IndividualFileResults
                                    .SelectMany(file => file.PsmFile)
                                    .Where(p => p.PassesConfidenceFilter)
                                    .Select(p => p.FullSequence)
                                    .ToList();
                                fullSequences.AddRange(sequences);
                            }
                            else
                            {
                                var sequences = msFraggerResult.IndividualFileResults
                                    .SelectMany(file => file.PsmFile)
                                    .Where(p => p.PassesConfidenceFilter)
                                    .GroupBy(p => p.FullSequence)
                                    .Select(p => p.First().FullSequence)
                                    .ToList();
                                fullSequences.AddRange(sequences);
                            }

                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(runResult));
                    }
                

                if (resultType is ResultType.Peptide)
                    fullSequences = fullSequences.GroupBy(p => p).Select(p => p.First()).ToList();
                var plot = GenericPlots.ModificationDistribution(fullSequences, groupResult.Key, "Modification",
                    "Count", false, false);
                toCombine.Add(plot);
            }
            
            var combined = Chart.Combine(toCombine)
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Modification Distribution")
                .WithSize(900, 600)
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            return combined;
        }

        public static GenericChart.GenericChart GetModificationDistribution(this MetaMorpheusResult result, ResultType resultType = ResultType.Psm, string cellLine = "", bool isTopDown = true)
        {
            string resultTypeLabel = Labels.GetLabel(isTopDown, resultType);
            List<PsmFromTsv> results = resultType switch
            {
                ResultType.Psm => result.AllPsms.Where(p => p is
                        { DecoyContamTarget: "T", PEP_QValue: <= 0.01, AmbiguityLevel: "1" })
                    .ToList(),
                ResultType.Peptide => result.AllPsms.Where(p => p is
                        { DecoyContamTarget: "T", PEP_QValue: <= 0.01, AmbiguityLevel: "1" })
                    .GroupBy(p => p.FullSequence)
                    .Select(p => p.First())
                    .ToList(),
                _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null)
            };


            var grouped = results.ToChimeraGroupedDictionary();
            var nonChimeric = grouped[1]
                .Select(p => p.FullSequence)
                .ToList();
            var chimeric = grouped.Where(p => p.Key != 1)
                .SelectMany(p => p.Value)
                .Select(p => p.FullSequence)
                .ToList();

            var chart = Chart.Combine(new[]
                {
                    GenericPlots.ModificationDistribution(nonChimeric, "Non-Chimeric", "Modification", "Percent"),
                    GenericPlots.ModificationDistribution(chimeric, "Chimeric", "Modification", "Percent"),
                })
                .WithTitle($"{cellLine} 1% {resultTypeLabel} Modification Distribution")
                .WithSize(1200, 800)
                .WithXAxis(LinearAxis.init<string, string, string, string, string, string>(TickAngle: 45))
                .WithLayout(PlotlyBase.DefaultLayout);
            return chart;
        }
    
        public static GenericChart.GenericChart GetModificationDistribution(this List<ProformaRecord> records, bool isTopDown, bool displayRelative = false, bool displayCarbamidoMethyl = false)
        {
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var conditionGroup in records.GroupBy(p => p.Condition.ConvertConditionName()))
            {
                var fullSequences = conditionGroup.Select(p => p.FullSequence);
                var plot = GenericPlots.ModificationDistribution(fullSequences.ToList(), conditionGroup.Key, "Modification", "Count", displayCarbamidoMethyl, displayRelative);
                toCombine.Add(plot);
            }

            var finalPlot = Chart.Combine(toCombine)
                .WithTitle($"1% {Labels.GetLabel(isTopDown, ResultType.Psm)}  Modification Distribution")
                .WithSize(900, 600)
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

            return finalPlot;
        }
    }
}
