using System.Runtime.CompilerServices;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
using Plotly.NET;
using Chart = Plotly.NET.CSharp.Chart;

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
        public static GenericChart.GenericChart PlotModificationDistribution(this List<ProteinCountingRecord> matches)
        {

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var conditionGroup in matches
                         .Where(p => p is { UniqueFullSequences: > 1, UniqueBaseSequences: > 1 })
                         .GroupBy(p => p.Condition.ConvertConditionName()))
            {
                string condition = conditionGroup.Key;
                var fullSequences = conditionGroup.SelectMany(p => p.FullSequences)
                    .Distinct()
                    .ToList();

                var plot = GenericPlots.ModificationDistribution(fullSequences, condition, "Modification", "Percent", false);
                toCombine.Add(plot);
            }

            var finalPlot = Chart.Combine(toCombine);
            finalPlot.Show();
            
            return null;
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
    }
}
