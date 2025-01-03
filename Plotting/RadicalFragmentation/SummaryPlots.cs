using Plotly.NET;
using MathNet.Numerics;
using Plotly.NET.LayoutObjects;
using RadicalFragmentation.Processing;
using RadicalFragmentation;
using System.Globalization;
using System.Text;
using Microsoft.FSharp.Core;
using UsefulProteomicsDatabases;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET.ImageExport;
using Plotting.Util;
using ResultAnalyzerUtil;
using System.Security.Cryptography;
using static Plotly.NET.StyleParam.LinearAxisId;
using MzLibUtil;

namespace Plotting.RadicalFragmentation
{
    public static class SummaryPlots
    {
        public static int StandardWidth = RadicalFragmentationPlotExtensions.StandardWidth;
        public static int StandardHeight = RadicalFragmentationPlotExtensions.StandardHeight;

        public static string GetLabel(string type, int missedMonos, double tolerance)
        {
            var sb = new StringBuilder(16);
            sb.Append(type);
            if (missedMonos is not (0 or -1))
                sb.Append($" {missedMonos} Missed Mono");
            if (tolerance is not (10 or -1))
                sb.Append($" {tolerance} ppm");
            return sb.ToString();
        }

        public static string GetAmbigLabel(int ambigLevel)
        {
            return ambigLevel switch
            {
                1 => "Proteoform",
                2 => "Protein",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        #region Plot Writing

        public static void WriteCumulativeFragmentsNeededLine(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, bool outlinedMarkers = false, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetCumulativeFragmentsNeededChart(type, ambigLevel, tolerance, missedMono, outlinedMarkers);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_CumulativeFragmentsNeeded");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteCumulativeFragmentsNeededLine: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteFragmentsNeededHistogram(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.MinFragmentsNeededHist(type, out int maxVal, ambigLevel, tolerance, missedMono);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_MinFragmentsNeededHist");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteFragmentsNeededHistogram: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteHybridFragmentNeeded(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetHybridFragmentNeededChart(type, ambigLevel, tolerance, missedMono);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_HybridFragmentNeeded");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteHybridFragmentNeeded: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteUniqueFragmentPlot(this List<FragmentHistogramRecord> summaryRecords, string outDir, string type, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetUniqueFragmentHist(type);
                var outPath = Path.Combine(outDir, $"{type}_UniqueFragmentsHistogram");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteUniqueFragmentPlot: {ex.Message}\nType: {type}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WritePrecursorCompetitionPlot(this List<PrecursorCompetitionSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetPrecursorCompetitionHistogram(type, ambigLevel, tolerance, missedMono);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_PrecursorCompetition");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WritePrecusorCompetitonPlot: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteMissedMonoCumulativeLine(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetMissedMonoCumulativeChart(type, ambigLevel, tolerance);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, tolerance)}_{GetAmbigLabel(ambigLevel)}_MissedMonoCumulative");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteMissedMonoCumulativeLine: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteMissedMonoFragmentsNeededHistogram(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, bool doLog = true, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetMissedMonoFragmentsNeededHist(type, ambigLevel, tolerance);

                string variableLabel = doLog ? "" : "Absolute";
                var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, tolerance)}_{GetAmbigLabel(ambigLevel)}_MinFragmentsNeededByMissedMono_{variableLabel}Hist");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteMissedMonoFragmentsNeededHistogram: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteMissedMonoPrecursorCompetitionPlot(this List<PrecursorCompetitionSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetPrecursorCompetitionHistogram(type, ambigLevel, tolerance, -1);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, tolerance)}_{GetAmbigLabel(ambigLevel)}_MissedMonoPrecursorCompetition");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteMissedMonoPrecursorCompetitionPlot: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, Tolerance: {tolerance}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteToleranceCumulativeLine(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, int missedMono = 0, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetToleranceCumulativeChart(type, ambigLevel, missedMono);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, 10)}_{GetAmbigLabel(ambigLevel)}_ToleranceCumulative");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteToleranceCumulativeLine: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteToleranceFragmentsNeededHistogram(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, int missedMono = 0, bool doLog = true, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetToleranceFragmentsNeededHist(type, ambigLevel, missedMono, doLog);
                string variableLabel = doLog ? "" : "Absolute";
                var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, 10)}_{GetAmbigLabel(ambigLevel)}_MinFragmentsNeededByTolerance_{variableLabel}Hist");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteToleranceFragmentsNeededHistogram: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void WriteTolerancePrecursorCompetitionPlot(this List<PrecursorCompetitionSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, int missedMono = 0, int? width = null, int? height = null)
        {
            try
            {
                width ??= StandardWidth;
                height ??= StandardHeight;
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                var chart = summaryRecords.GetPrecursorCompetitionHistogram(type, ambigLevel, -1, missedMono);
                var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, 10)}_{GetAmbigLabel(ambigLevel)}_TolerancePrecursorCompetition");
                chart.SavePNG(outPath, null, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WriteTolerancePrecursorCompetitionPlot: {ex.Message}\nType: {type}, AmbigLevel: {ambigLevel}, MissedMono: {missedMono}\n{ex.StackTrace}");
                System.Diagnostics.Debugger.Break();
            }
        }

        #endregion

        #region Plot Generation

        public static GenericChart.GenericChart MinFragmentsNeededHist(this List<FragmentsNeededSummary> summaryRecords, string type, out int maxVal, int ambigLevel = 1, double tolerance = 10, int missedMono = 1)
        {
            maxVal = 0;
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in summaryRecords
                         .Where(p => 
                         p.AmbiguityLevel == ambigLevel 
                         && p.PpmTolerance == tolerance 
                         && p.MissedMonoisotopics == missedMono
                         && p.FragmentationType == type)
                         .GroupBy(p => p.NumberOfMods))
            {
                var color = RadicalFragmentationPlotHelpers.ModAndMissedMonoToColorDict[modGroup.Key][missedMono];
                var temp = modGroup.GroupBy(p => p.FragmentsNeeded)
                    .OrderBy(p => p.Key)
                    .Select(p => (p.Key, p.First().Count)).ToArray();

                var x = Enumerable.Range(-1, temp.Max(p => p.Key) + 2).Select(p => p.ToString()).ToArray();
                var y = temp.Select(p => p.Item2).ToArray();
                for (int i = 0; i < x.Length; i++)
                {
                    x[i] = x[i] switch
                    {
                        "-1" => "No ID",
                        "0" => "Precursor Only",
                        _ => x[i]
                    };
                }

                var localMax = y.Max();
                if (localMax > maxVal)
                    maxVal = localMax;


                var chart = Chart.Column<int, string, string>(y, x,
                    Name: $"{modGroup.Key} mods", MarkerColor: color);
                toCombine.Add(chart);
            }
            var combined = Chart.Combine(toCombine)
                .WithTitle(
                    $"{GetLabel(type, missedMono, tolerance)}: Fragments Needed to Distinguish from other {GetAmbigLabel(ambigLevel)}s (Ambiguity Level {ambigLevel})")
                .WithXAxisStyle(Title.init("Fragments Needed"))
                .WithYAxisStyle(Title.init($"Log(Count of {GetAmbigLabel(ambigLevel)}s)"))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight);

            return combined;
        }

        public static GenericChart.GenericChart GetCumulativeFragmentsNeededChart(this List<FragmentsNeededSummary> summaryRecords, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, bool outlinedMarkers = false)
        {
            int maxToDifferentiate = summaryRecords.Max(p => p.FragmentsNeeded);

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in summaryRecords
                         .Where(p => p.AmbiguityLevel == ambigLevel && p.PpmTolerance == tolerance && p.MissedMonoisotopics == missedMono && p.FragmentationType == type)
                         .GroupBy(p => p.NumberOfMods))
            {
                double total = modGroup.Sum(p => p.Count);
                var color = RadicalFragmentationPlotHelpers.ModToColorDict[modGroup.Key];
                var toSubtract = modGroup.FirstOrDefault(p => p.FragmentsNeeded == -1)!.Count;

                var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();

                var yVal = xInteger.Select(fragNeed => (modGroup.Where(p => p.FragmentsNeeded <= fragNeed).Sum(m => m.Count) - toSubtract) / total * 100).ToArray();
                var xVal = xInteger.Select(p => p.ToString()).ToArray();
                xVal[0] = "No ID";
                xVal[1] = "Precursor Only";
                var chart = Chart.Spline<string, double, string>(xVal, yVal, true, 0.0,
                    Name: $"{modGroup.Key} mods", MultiText: yVal.Select(p => $"{p.Round(2)}%").ToArray(), MarkerColor: color);
                if (outlinedMarkers)
                    chart = chart.WithMarkerStyle(Color: color, Size: 8, Outline: Line.init(Color: Color.fromKeyword(ColorKeyword.Black), Width: 0.5),
                        Symbol: StyleParam.MarkerSymbol.NewModified(StyleParam.MarkerSymbol.Circle, StyleParam.SymbolStyle.Dot));
                toCombine.Add(chart);
            }


            var combined = Chart.Combine(toCombine)
                .WithTitle(
                    $"{GetLabel(type, missedMono, tolerance)}: {GetAmbigLabel(ambigLevel)}s Identified by Number of Fragments")
                .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Tick0: 0, DTick: 1))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight)
                .WithYAxisStyle(Title.init($"Percent of {GetAmbigLabel(ambigLevel)}s Identified"),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)));

            return combined;
        }

        public static GenericChart.GenericChart GetHybridFragmentNeededChart(this List<FragmentsNeededSummary> summaryRecords, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1)
        {
            var combined = Chart.Combine(new[]
                {
                    summaryRecords.MinFragmentsNeededHist(type, out int maxVal, ambigLevel, tolerance, missedMono).WithAxisAnchor(Y: 1),
                    summaryRecords.GetCumulativeFragmentsNeededChart(type, ambigLevel, tolerance, missedMono, true)
                        .WithAxisAnchor(Y: 2)
                })
                .WithTitle($"{GetLabel(type, missedMono, tolerance)}: Fragments to distinguish from other {GetAmbigLabel(ambigLevel)}")
                .WithYAxisStyle(Title.init("Log Count"),
                    Side: StyleParam.Side.Left,
                    Id: StyleParam.SubPlotId.NewYAxis(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, Math.Log10(maxVal))))
                .WithYAxisStyle(Title.init("Percent Identified"),
                    Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(2),
                    Overlaying: StyleParam.LinearAxisId.NewY(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)))
                  .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight);
            return combined;
        }

        public static GenericChart.GenericChart GetHybridFragmentNeededChart(GenericChart.GenericChart linePlot, GenericChart.GenericChart histogram, string label, int maxVal, int ambigLevel)
        {
            var combined = Chart.Combine(new[]
                {
                    histogram.WithAxisAnchor(Y: 1),
                    linePlot.WithAxisAnchor(Y: 2)
                })
                .WithTitle($"{label}: Fragments to distinguish from other Proteoforms")
                .WithYAxisStyle(Title.init("Log Count"),
                    Side: StyleParam.Side.Left,
                    Id: StyleParam.SubPlotId.NewYAxis(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, Math.Log10(maxVal))))
                .WithYAxisStyle(Title.init("Percent Identified"),
                    Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(2),
                    Overlaying: StyleParam.LinearAxisId.NewY(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight);
            return combined;
        }

        public static GenericChart.GenericChart GetUniqueFragmentHist(this List<FragmentHistogramRecord> summaryRecords, 
            string type)
        {
            List<GenericChart.GenericChart> toCombine = new();

            foreach (var modGroup in summaryRecords
                         .Where(p => p.AmbiguityLevel == 1)
                         .GroupBy(p => p.NumberOfMods)
                         .OrderBy(p => p.Key))
            {
                var color = RadicalFragmentationPlotHelpers.ModToColorDict[modGroup.Key];
                var x = modGroup.Select(p => p.FragmentCount);
                var y = modGroup.Select(p => p.ProteinCount);
                //var chart = Chart.Spline<int, int, string>(x, y, true, 2, $"{modGroup.Key} mods", MarkerColor: color);

                var chart = Chart.Histogram<int, int, string>(x.ToArray(), y.ToArray(), ShowLegend: true, 
                    Name: $"{modGroup.Key} mods", MarkerColor: color,
                 HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Sum);
                toCombine.Add(chart);
            }

            var combined = Chart.Combine(toCombine)
                .WithXAxisStyle(Title.init("Fragment Count"))
                .WithYAxisStyle(Title.init($"Count of {GetAmbigLabel(1)}s"))
                .WithTitle($"{GetLabel(type, 0, 10)}: Unique Fragments per {GetAmbigLabel(1)} ")
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight);

            return combined;
        }

        public static GenericChart.GenericChart GetPrecursorCompetitionHistogram(this List<PrecursorCompetitionSummary> summaryRecords, string type,
            int ambigLevel = 1, double tolerance = -1, int missedMono = -1)
        {
            if (tolerance == -1 && missedMono == -1)
                throw new ArgumentException("Cannot do both");

            var toUse = summaryRecords.Where(p =>
                p.AmbiguityLevel == ambigLevel && p.FragmentationType == type
                                               && (missedMono == -1 || p.MissedMonoisotopics == missedMono)
                                               && (tolerance == -1 || p.PpmTolerance == tolerance))
                .ToList();

            var modIndexDict = toUse.Select(p => p.NumberOfMods).Distinct()
                .ToDictionary(p => p, p => 0);
            List<GenericChart.GenericChart> toCombine = new();
            List<GenericChart.GenericChart> toCombine2 = new();
            foreach (var modGroup in toUse
                .GroupBy(p => (p.MissedMonoisotopics, p.PpmTolerance, p.NumberOfMods)))
            {
                int mods = modGroup.Key.NumberOfMods;
                var color = RadicalFragmentationPlotHelpers.ModToColorSetDict[mods][modIndexDict[mods]];
                modIndexDict[mods]++;

                var temp = modGroup.OrderBy(p => p.PrecursorsInGroup)
                    .ToList();

                List<double> x = new();
                List<double> y = new();

                foreach (var item in temp)
                {
                    if (item.Count == 0)
                        continue;
                    x.Add(item.PrecursorsInGroup + 1);
                    y.Add(item.Count);
                }

                // Apply rolling average with 5% of all values with a floor and ceiling of 3 and 50
                int toRoll = (int)(y.Count * 0.05);
                int min = 5;
                int max = 50;

                if (toRoll > max)
                    toRoll = max;
                else if (toRoll < min)
                    toRoll = min;

                bool useWeighting = false;
                int leaveAlone = 2;
                List<double> yRolled = new(y.Count);
                for (int i = 0; i < y.Count; i++)
                {
                    if (i < leaveAlone)
                    {
                        yRolled.Add(y[i]);
                        continue;
                    }

                    double sum = 0;
                    double weightSum = 0;
                    for (int j = -toRoll; j <= toRoll; j++)
                    {
                        int index = i + j;
                        if (index >= 0 && index < y.Count)
                        {
                            double weight = useWeighting ? 1.0 / (Math.Abs(j) + 1) : 1.0; // Closer values are weighted more if useWeighting is true
                            sum += y[index] * weight;
                            weightSum += weight;
                        }
                    }
                    yRolled.Add(sum / weightSum);
                }

                y = yRolled;

                string name = $"{mods} mods";
                if (missedMono == -1)
                    name += $" with {modGroup.Key.MissedMonoisotopics} Missed Mono";
                if (tolerance == -1)
                    name += $" at {modGroup.Key.PpmTolerance} ppm";

                var chart = Chart.Scatter<double, double, string>(x, y, StyleParam.Mode.Lines,
                    Name: name, MarkerColor: color, ShowLegend: false,
                    Line: Line.init(Width: 1, Color: color, Smoothing: 0.5, Shape: StyleParam.Shape.Spline));
                var chart2 = Chart.Scatter<double, double, string>(x, y, StyleParam.Mode.Lines,
                    Name: name, MarkerColor: color, 
                    Line: Line.init(Width: 1, Color: color, Smoothing: 0.5, Shape: StyleParam.Shape.Spline));

                toCombine.Add(chart);
                toCombine2.Add(chart2);
            }

            double yMax = toUse.Max(p => p.Count) + 1;
            double cutoff = toUse
                .Where(p => p is { PrecursorsInGroup: > 2, Count: > 0 })
                .OrderBy(p => p.PrecursorsInGroup)
                .Select(p => p.Count)
                .Average(p => p) *2;

            var combined = Chart.Combine(toCombine)
                .WithAxisAnchor(X: 1, Y: 1)
                .WithLegend(false)
                .WithXAxisStyle(Title.init("Precursors in group"))
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log, Tick0: 0))
                .WithYAxisStyle(Title.init($"Count of {GetAmbigLabel(ambigLevel)}"), Id: StyleParam.SubPlotId.NewYAxis(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(cutoff, yMax)));

            // Create the second plot with y ranging from 0 to averageCount * 5
            var secondPlot = Chart.Combine(toCombine2)
                .WithAxisAnchor(X: 2, Y: 2)
                .WithLegend(true)
                .WithLayout(PlotlyBase.JustLegend)
                .WithXAxisStyle(Title.init("Precursors in group"))
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log, Tick0: 0))
                .WithYAxisStyle(Title.init($"Count of {GetAmbigLabel(ambigLevel)}"), Id: StyleParam.SubPlotId.NewYAxis(2),
                MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, cutoff)));
        
            // Combine both plots vertically
            string title = $"{GetLabel(type, missedMono, tolerance)}: {GetAmbigLabel(ambigLevel)} Precursor Competition ";
            if (missedMono == -1)
                title += "by Missed Monos";
            else if (tolerance == -1)
                title += "by Tolerance";

            var finalCombined = Chart.Grid(new[] { combined, secondPlot }, 2, 1, Pattern: StyleParam.LayoutGridPattern.Independent, YGap: 0.05,
                    XSide: StyleParam.LayoutGridXSide.Bottom)
                .WithTitle(title)
                .WithSize(StandardWidth, StandardHeight);

            return finalCombined;
        }

        // missed mono
        public static GenericChart.GenericChart GetMissedMonoCumulativeChart(this List<FragmentsNeededSummary> summaryRecords, string type, int ambigLevel = 1, double tolerance = 10)
        {
            int maxToDifferentiate = summaryRecords.Max(m => m.FragmentsNeeded);
            var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in summaryRecords
                .Where(p => p.AmbiguityLevel == ambigLevel && p.PpmTolerance == tolerance && p.FragmentationType == type)
                .GroupBy(p => p.NumberOfMods))
            {
                int modCount = modGroup.Key;
                foreach (var result in modGroup.GroupBy(p => p.MissedMonoisotopics))
                {
                    int missedMonos = result.Key;
                    double total = result.Sum(p => p.Count);
                    var toSubtract = result.FirstOrDefault(p => p.FragmentsNeeded == -1)!.Count;

                    var yVal = xInteger.Select(fragNeed => (result.Where(p => p.FragmentsNeeded <= fragNeed).Sum(m => m.Count) - toSubtract) / total * 100).ToArray();
                    var xVal = xInteger.Select(p => p.ToString()).ToArray();
                    xVal[0] = "No ID";
                    xVal[1] = "Precursor Only";


                    // missed mono plot differences
                    var color = RadicalFragmentationPlotHelpers.ModAndMissedMonoToColorDict[modCount][missedMonos];
                    var lineDash = RadicalFragmentationPlotHelpers.IntegerToLineDict[missedMonos];
                    var name = missedMonos == 0 ? $"{modCount} mods" : $"{modCount} mods with {missedMonos} Missed Mono";

                    var chart = Chart.Spline<string, double, string>(xVal, yVal, true, 0.0, LineDash: lineDash,
                        Name: name, MultiText: yVal.Select(p => $"{p.Round(2)}%").ToArray(), MarkerColor: color);
                    toCombine.Add(chart);
                }
            }

            var combined = Chart.Combine(toCombine)
                .WithTitle($"{GetLabel(type, 0, tolerance)}: {GetAmbigLabel(ambigLevel)}s Identified by Number of Fragments")
                .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Tick0: 0, DTick: 1))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight)
                .WithYAxisStyle(Title.init($"Percent of {GetAmbigLabel(ambigLevel)}s Identified"), MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)));

            return combined;
        }

        public static GenericChart.GenericChart GetMissedMonoFragmentsNeededHist(this List<FragmentsNeededSummary> summaryRecords, string type, int ambigLevel = 1, double tolerance = 10, bool doLog = true)
        {
            int maxToDifferentiate = summaryRecords.Max(m => m.FragmentsNeeded);
            var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();
            List<GenericChart.GenericChart> toCombine = new();

            foreach (var result in summaryRecords
                .Where(p => p.AmbiguityLevel == ambigLevel && p.PpmTolerance == tolerance && p.FragmentationType == type)
                .GroupBy(p => p.NumberOfMods))
            {
                var innerGroups = result.GroupBy(p => p.MissedMonoisotopics);
                foreach (var innerGroup in innerGroups)
                {
                    var temp = innerGroup.GroupBy(p => p.FragmentsNeeded)
                        .OrderBy(p => p.Key)
                        .Select(p => (p.Key, p.First().Count)).ToArray();

                    var x = Enumerable.Range(-1, temp.Max(p => p.Key) + 2).Select(p => p.ToString()).ToArray();
                    var y = temp.Select(p => p.Item2).ToArray();
                    for (int i = 0; i < x.Length; i++)
                    {
                        x[i] = x[i] switch
                        {
                            "-1" => "No ID",
                            "0" => "Precursor Only",
                            _ => x[i]
                        };
                    }

                    var color = RadicalFragmentationPlotHelpers.ModAndMissedMonoToColorDict[result.Key][innerGroup.Key];
                    var chart = Chart.Column<int, string, string>(y, x,
                        Name: $"{result.Key} mods ({innerGroup.Key})", MarkerColor: color);
                    toCombine.Add(chart);
                }
            }
            var combined = Chart.Combine(toCombine)
                .WithTitle($"{GetLabel(type, 0, tolerance)}: {GetAmbigLabel(ambigLevel)}s Identified by Number of Fragments (Missed Monos)")
                .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight);

            if (doLog)
            {
                combined = combined.WithYAxisStyle(Title.init($"Log(Count of {GetAmbigLabel(ambigLevel)}s)"))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log));
            }
            else
            {
                combined = combined.WithYAxisStyle(Title.init($"Count of {GetAmbigLabel(ambigLevel)}s"));
            }
            return combined;
        }

        // tolerance
        public static GenericChart.GenericChart GetToleranceCumulativeChart(this List<FragmentsNeededSummary> summaryRecords, string type, int ambigLevel = 1, int missedMono = 0)
        {
            int maxToDifferentiate = summaryRecords.Max(m => m.FragmentsNeeded);
            var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in summaryRecords
                .Where(p => p.AmbiguityLevel == ambigLevel && p.MissedMonoisotopics == missedMono && p.FragmentationType == type)
                .GroupBy(p => p.NumberOfMods))
            {
                int modCount = modGroup.Key;
                var ugh = modGroup.GroupBy(p => p.PpmTolerance).ToList();
                for (int i = 0; i < ugh.Count; i++)
                {
                    var result = ugh[i];
                    int tolerance = (int)result.Key;
                    double total = result.Sum(p => p.Count);
                    var toSubtract = result.FirstOrDefault(p => p.FragmentsNeeded == -1)!.Count;

                    var yVal = xInteger.Select(fragNeed => (result.Where(p => p.FragmentsNeeded <= fragNeed).Sum(m => m.Count) - toSubtract) / total * 100).ToArray();
                    var xVal = xInteger.Select(p => p.ToString()).ToArray();
                    xVal[0] = "No ID";
                    xVal[1] = "Precursor Only";


                    // missed mono plot differences
                    var color = RadicalFragmentationPlotHelpers.ModToColorSetDict[modCount][i];
                    var lineDash = RadicalFragmentationPlotHelpers.IntegerToLineDict[tolerance];
                    var name = tolerance == 100 
                        ? $"{modCount} mods\u00a0({tolerance})"
                        : $"{modCount} mods ({tolerance})";

                    var chart = Chart.Spline<string, double, string>(xVal, yVal, true, 0.0, LineDash: lineDash,
                        Name: name, MultiText: yVal.Select(p => $"{p.Round(2)}%").ToArray(), MarkerColor: color);
                    toCombine.Add(chart);
                }
            }

            var combined = Chart.Combine(toCombine)
                .WithTitle($"{GetLabel(type, 0, 10)}: {GetAmbigLabel(ambigLevel)}s Identified by Number of Fragments (Ppm Tolerance)")
                .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Tick0: 0, DTick: 1))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight)
                .WithYAxisStyle(Title.init($"Percent of {GetAmbigLabel(ambigLevel)}s Identified"), MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)));

            return combined;
        }

        public static GenericChart.GenericChart GetToleranceFragmentsNeededHist(this List<FragmentsNeededSummary> summaryRecords, string type, int ambigLevel = 1, int missedMono = 0, bool doLog = true)
        {
            int maxToDifferentiate = summaryRecords.Max(m => m.FragmentsNeeded);
            var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();
            List<GenericChart.GenericChart> toCombine = new();

            foreach (var result in summaryRecords
                .Where(p => p.AmbiguityLevel == ambigLevel && p.MissedMonoisotopics == missedMono && p.FragmentationType == type)
                .GroupBy(p => p.NumberOfMods))
            {
                var modCount = result.Key;
                var innerGroups = result.GroupBy(p => p.PpmTolerance).ToList() ;
                for (var index = 0; index < innerGroups.Count; index++)
                {
                    var innerGroup = innerGroups[index];
                    var tolerance = (int)innerGroup.Key;
                    var temp = innerGroup.GroupBy(p => p.FragmentsNeeded)
                        .OrderBy(p => p.Key)
                        .Select(p => (p.Key, p.First().Count)).ToArray();

                    var x = Enumerable.Range(-1, temp.Max(p => p.Key) + 2).Select(p => p.ToString()).ToArray();
                    var y = temp.Select(p => p.Item2).ToArray();
                    for (int i = 0; i < x.Length; i++)
                    {
                        x[i] = x[i] switch
                        {
                            "-1" => "No ID",
                            "0" => "Precursor Only",
                            _ => x[i]
                        };
                    }
                    var name = tolerance == 100
                        ? $"{modCount} mods\u00a0({tolerance})"
                        : $"{modCount} mods ({tolerance})";

                    var color = RadicalFragmentationPlotHelpers.ModToColorSetDict[modCount][index];
                    var chart = Chart.Column<int, string, string>(y, x,
                        Name: name, MarkerColor: color);
                    toCombine.Add(chart);
                }
            }
            var combined = Chart.Combine(toCombine)
                .WithTitle($"{GetLabel(type, 0, 10)}: {GetAmbigLabel(ambigLevel)}s Identified by Number of Fragments (Ppm Tolerance)")
                .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                .WithLayout(PlotlyBase.JustLegend)
                .WithSize(StandardWidth, StandardHeight);

            if (doLog)
            {
                combined = combined.WithYAxisStyle(Title.init($"Log(Count of {GetAmbigLabel(ambigLevel)}s)"))
                    .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log));
            }
            else
            {
                combined = combined.WithYAxisStyle(Title.init($"Count of {GetAmbigLabel(ambigLevel)}s"));
            }
            return combined;
        }
        #endregion
    }
}