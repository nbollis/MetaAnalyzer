﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
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
        public static string GetAxisLabel(this ProteinCountPlotTypes type) => type switch
        {
            ProteinCountPlotTypes.SequenceCoverage => "Sequence Coverage",
            ProteinCountPlotTypes.BaseSequenceCount => "Unique Peptides per Protein",
            ProteinCountPlotTypes.FullSequenceCount => "Unique Peptidoforms per Protein",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static GenericChart.GenericChart GetProteinCountPlotsStacked(this List<ProteinCountingRecord> records,
            ProteinCountPlotTypes resultType)
        {
            var chart = Chart.Grid(new[]
                {
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.BoxPlot),
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.ViolinPlot),
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.Histogram),
                    GetProteinCountPlot(records, resultType, DistributionPlotTypes.KernelDensity),
                }, 2, 2)
                .WithSize(1000, 1000)
                .WithTitle($"Distribution of {resultType.GetAxisLabel()}");

            return chart;
        }

        public static GenericChart.GenericChart GetProteinCountPlotsStacked(this List<ProteinCountingRecord> records,
            DistributionPlotTypes plotType)
        {
            var chart = Chart.Grid(new[]
                {
                    GetProteinCountPlot(records, ProteinCountPlotTypes.SequenceCoverage, plotType)
                        .WithYAxis(LinearAxis.init<string, string, string, string, string, string>
                                (Title: Title.init(ProteinCountPlotTypes.SequenceCoverage.GetAxisLabel())),
                            StyleParam.SubPlotId.NewYAxis(1))
                        .WithXAxis(LinearAxis.init<string, string, string, string, string, string>
                                (Title: Title.init("Condition")),
                            StyleParam.SubPlotId.NewXAxis(1)),
                    GetProteinCountPlot(records, ProteinCountPlotTypes.BaseSequenceCount, plotType)
                        .WithYAxis(LinearAxis.init<string, string, string, string, string, string>
                                (Title: Title.init(ProteinCountPlotTypes.BaseSequenceCount.GetAxisLabel())),
                            StyleParam.SubPlotId.NewYAxis(2))
                        .WithXAxis(LinearAxis.init<string, string, string, string, string, string>
                                (Title: Title.init("Condition")),
                            StyleParam.SubPlotId.NewXAxis(2)),
                    GetProteinCountPlot(records, ProteinCountPlotTypes.FullSequenceCount, plotType)
                        .WithYAxis(LinearAxis.init<string, string, string, string, string, string>
                                (Title: Title.init(ProteinCountPlotTypes.FullSequenceCount.GetAxisLabel())),
                            StyleParam.SubPlotId.NewYAxis(3))
                        .WithXAxis(LinearAxis.init<string, string, string, string, string, string>
                                (Title: Title.init("Condition")),
                            StyleParam.SubPlotId.NewXAxis(3)),
                }, 3, 1, 
                    YAxes: new Optional<StyleParam.LinearAxisId[]>(new StyleParam.LinearAxisId[]
                {
                    StyleParam.LinearAxisId.NewY(1),
                    StyleParam.LinearAxisId.NewY(2),
                    StyleParam.LinearAxisId.NewY(3),
                }, true), 
                    XAxes: new Optional<StyleParam.LinearAxisId[]>(new StyleParam.LinearAxisId[]
                {
                    StyleParam.LinearAxisId.NewX(1),
                    StyleParam.LinearAxisId.NewX(2),
                    StyleParam.LinearAxisId.NewX(3),
                }, true))
                .WithSize(600, 1200)
                .WithTitle($"Distribution of Protein Counting Metrics");

            return chart;
        }

        public static GenericChart.GenericChart GetProteinCountPlot(this List<ProteinCountingRecord> records,
            ProteinCountPlotTypes resultType, DistributionPlotTypes plotType)
        {
            string xTitle = "Condition";
            string yTitle = resultType.GetAxisLabel();
            List<GenericChart.GenericChart> toCombine = new();

            foreach (var record in records
                         .Where(p => p is { UniqueFullSequences: > 1, UniqueBaseSequences: > 1 })
                         .GroupBy(p => p.Condition.ConvertConditionName()))
            {
                var condition = record.Key;
                var data = record.GetValues(resultType).ToList();


                switch (plotType)
                {
                    case DistributionPlotTypes.ViolinPlot:
                        toCombine.Add(GenericPlots.ViolinPlot(data, condition));
                        break;

                    case DistributionPlotTypes.Histogram:
                        toCombine.Add(GenericPlots.Histogram(data, condition, xTitle, yTitle));
                        break;

                    case DistributionPlotTypes.BoxPlot:
                        toCombine.Add(GenericPlots.BoxPlot(data, condition, xTitle, yTitle, false));
                        break;

                    case DistributionPlotTypes.KernelDensity:
                        toCombine.Add(GenericPlots.KernelDensityPlot(data, condition, xTitle, yTitle, 0.1));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plotType), plotType, null);
                }
            }

            var finalPlot = Chart.Combine(toCombine)
                .WithTitle($"Distribution of {yTitle} per Protein")
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600);
            return finalPlot;

        }
    }
}