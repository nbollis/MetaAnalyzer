using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Plotly.NET;

namespace Analyzer.Plotting
{
    public static class PlotWorkshop
    {
        public enum ProteinCountPlotTypes
        {
            SequenceCoverage,
            BaseSequenceCount,
            FullSequenceCount,
        }

        public static IEnumerable<double> GetValues(this IEnumerable<ProteinCountingRecord> records,
            ProteinCountPlotTypes type)
        {
            return type switch
            {
                ProteinCountPlotTypes.SequenceCoverage => records.Select(p => p.SequenceCoverage),
                ProteinCountPlotTypes.BaseSequenceCount => records.Select(p => (double)p.UniqueBaseSequences),
                ProteinCountPlotTypes.FullSequenceCount => records.Select(p => (double)p.UniqueFullSequences),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        public static string GetAxisLabel(this ProteinCountPlotTypes type) => type switch
        {
            ProteinCountPlotTypes.SequenceCoverage => "Sequence Coverage",
            ProteinCountPlotTypes.BaseSequenceCount => "Unique Peptides per Protein ",
            ProteinCountPlotTypes.FullSequenceCount => "Unique Peptidoforms per Protein",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static void GetProteinCountPlot(this List<ProteinCountingRecord> records,
            ProteinCountPlotTypes resultType, DistributionPlotTypes plotType)
        {
            string xTitle = "Condition";
            string yTitle = resultType.GetAxisLabel();
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var record in records.GroupBy(p => p.Condition))
            {
                var condition = record.Key;
                var data = record.GetValues(resultType).ToList();
                switch (plotType)
                {
                    case DistributionPlotTypes.ViolinPlot:
                        toCombine.Add(GenericPlots.ViolinPlot(data, condition));
                        break;

                    case DistributionPlotTypes.Histogram:
                        toCombine.Add(GenericPlots.Histogram(data, "", xTitle, yTitle));
                        break;

                    case DistributionPlotTypes.BoxPlot:
                        toCombine.Add(GenericPlots.BoxPlot(data, "", xTitle, yTitle));
                        break;

                    case DistributionPlotTypes.KernelDensity:
                        toCombine.Add(GenericPlots.KernelDensityPlot(data, "", xTitle, yTitle));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plotType), plotType, null);
                }
            }

            var finalPlot = Chart.Combine(toCombine)
                .WithTitle($"Distribution of {yTitle} per Protein")
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600);
            finalPlot.Show();
            
        }
    }
}
