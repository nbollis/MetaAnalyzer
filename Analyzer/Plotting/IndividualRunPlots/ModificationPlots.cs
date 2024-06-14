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
