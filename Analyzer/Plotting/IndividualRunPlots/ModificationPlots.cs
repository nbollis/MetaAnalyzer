using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plotly.NET;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;

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
            string resultTypeLabel = isTopDown ? resultType == ResultType.Psm ? "PrSM" : "Proteoform" :
                resultType == ResultType.Psm ? "PSM" : "Peptide";

            foreach (var bulkResult in fileResults.Where(p => p is MetaMorpheusResult))
            {
                var result = (MetaMorpheusResult)bulkResult;
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
                var grouped = results.GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer)
                    .GroupBy(m => m.Count())
                    .ToDictionary(p => p.Key, p => p.SelectMany(m => m));

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
                    .WithTitle($"{cellLine.CellLine} 1% {resultType} Modification Distribution")
                    .WithSize(1200, 800)
                    .WithXAxis(LinearAxis.init<string, string, string, string, string, string>(TickAngle: 45))
                    .WithLayout(PlotlyBase.DefaultLayout);
                var outName = $"{FileIdentifiers.ModificationDistributionFigure}_{resultTypeLabel}_{cellLine.CellLine}";
                if (filterByCondition)
                    chart.SaveInCellLineAndMann11Directories(cellLine, outName, 800, 600);
                else
                    chart.SaveInCellLineOnly(cellLine, outName, 800, 600);
            }
        }
    }
}
