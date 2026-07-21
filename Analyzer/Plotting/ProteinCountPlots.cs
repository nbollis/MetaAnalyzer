using Analyzer.FileTypes.Internal;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Plotting;
using Plotting.Util;
using ResultAnalyzerUtil;
using Chart = Plotly.NET.CSharp.Chart;

namespace Analyzer.Plotting
{
    public static class ProteinCountPlots
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
        public static string GetAxisLabel(this ProteinCountPlotTypes type, bool isTopDown) => type switch
        {
            ProteinCountPlotTypes.SequenceCoverage => "Sequence Coverage",
            ProteinCountPlotTypes.BaseSequenceCount => $"Unique {Labels.GetLabel(isTopDown, ResultType.Psm)} per Protein",
            ProteinCountPlotTypes.FullSequenceCount => $"Unique {Labels.GetDifferentFormLabel(isTopDown)}s per Protein",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static GenericChart.GenericChart GetProteinCountPlotsGrid(this List<ProteinCountingRecord> records,
            ProteinCountPlotTypes resultType, bool isTopDown)
        {
            var chart = Chart.Grid(new[]
                {
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.BoxPlot, isTopDown),
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.ViolinPlot, isTopDown),
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.Histogram, isTopDown),
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.KernelDensity, isTopDown),
                }, 2, 2)
                .WithSize(1000, 1000)
                .WithTitle($"Distribution of {resultType.GetAxisLabel(isTopDown)}");

            return chart;
        }

        public static GenericChart.GenericChart GetProteinCountPlot(this List<ProteinCountingRecord> records,
            ProteinCountPlotTypes resultType, DistributionPlotTypes plotType, bool isTopDown, bool logY = false)
        {
            string xTitle = "" /*"Condition"*/;
            string yTitle = resultType.GetAxisLabel(isTopDown);
            string title = yTitle;
            if (logY)
                yTitle = $"Log10 {yTitle}";

            List<GenericChart.GenericChart> toCombine = new();

            foreach (var record in records
                         .Where(p => p is { UniqueFullSequences: > 1, UniqueBaseSequences: > 1 })
                         .GroupBy(p => p.Condition.ConvertConditionName())
                         .OrderBy(p => p.Key))
            {
                var condition = record.Key;
                var data = record.GetValues(resultType).ToList();

                int max = (int)(data.Max() + (data.Max() * 0.1));
                int min = -10;
                if (logY)
                {
                    max = (int)(Math.Log10(data.Max()) + (Math.Log10(data.Max()) * 0.1));
                    min = 0;
                    data = data.Select(Math.Log10).ToList();
                }
                switch (plotType)
                {
                    case DistributionPlotTypes.ViolinPlot:
                        toCombine.Add(GenericPlots.ViolinPlot(data, condition, xTitle, yTitle)
                            .WithYAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(min, max)));
                        break;

                    case DistributionPlotTypes.Histogram:
                        toCombine.Add(GenericPlots.Histogram(data, condition, xTitle, yTitle)
                            .WithXAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(0, max)));
                        break;

                    case DistributionPlotTypes.BoxPlot:
                        toCombine.Add(GenericPlots.BoxPlot(data, condition, xTitle, yTitle, false)
                            .WithYAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(min, max))
                            .WithLegend(false));
                        break;

                    case DistributionPlotTypes.KernelDensity:
                        toCombine.Add(GenericPlots.KernelDensityPlot(data, condition, xTitle, yTitle, 0.5)
                            .WithXAxisStyle<int, int, string>(MinMax: new Tuple<int, int>(0, max)));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plotType), plotType, null);
                }
            }

            var finalPlot = Chart.Combine(toCombine)
                .WithTitle($"{title} (1% {Labels.GetLabel(isTopDown, ResultType.Psm)})")
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            return finalPlot;
        }

    }
}
