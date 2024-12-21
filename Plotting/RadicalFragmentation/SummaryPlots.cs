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
            if (missedMonos != 0)
                sb.Append($" {missedMonos} Missed Mono");
            if (tolerance != 10)
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

        #region Result Writing

        public static void WriteCumulativeFragmentsNeededChart(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, bool outlinedMarkers = false, int? width = null, int? height = null)
        {
            width ??= StandardWidth;
            height ??= StandardHeight;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            var chart = summaryRecords.GetCumulativeFragmentsNeededChart(type, ambigLevel, tolerance, missedMono, outlinedMarkers);
            var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_CumulativeFragmentsNeeded");
            chart.SavePNG(outPath, null, width, height);
        }

        public static void WriteMinFragmentsNeededHist(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, int? width = null, int? height = null)
        {
            width ??= StandardWidth;
            height ??= StandardHeight;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            var chart = summaryRecords.MinFragmentsNeededHist(type, out int maxVal, ambigLevel, tolerance, missedMono);
            var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_MinFragmentsNeededHist");
            chart.SavePNG(outPath, null, width, height);
        }

        public static void WriteHybridFragmentNeededChart(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int missedMono = 1, int? width = null, int? height = null)
        {
            width ??= StandardWidth;
            height ??= StandardHeight;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            var chart = summaryRecords.GetHybridFragmentNeededChart(type, ambigLevel, tolerance, missedMono);
            var outPath = Path.Combine(outDir, $"{GetLabel(type, missedMono, tolerance)}_{GetAmbigLabel(ambigLevel)}_HybridFragmentNeeded");
            chart.SavePNG(outPath, null, width, height);
        }

        public static void WriteMissedMonoCumulativeChart(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, int? width = null, int? height = null)
        {
            width ??= StandardWidth;
            height ??= StandardHeight;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            var chart = summaryRecords.GetMissedMonoCumulativeChart(type, ambigLevel, tolerance);
            var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, tolerance)}_{GetAmbigLabel(ambigLevel)}_MissedMonoCumulative");
            chart.SavePNG(outPath, null, width, height);
        }

        public static void WriteMissedMonoHistogram(this List<FragmentsNeededSummary> summaryRecords, string outDir, string type, int ambigLevel = 1, double tolerance = 10, bool doLog = true, int? width = null, int? height = null)
        {
            width ??= StandardWidth;
            height ??= StandardHeight;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            var chart = summaryRecords.GetMissedMonoFragmentsNeededHist(type, ambigLevel, tolerance);

            string variableLabel = doLog ? "Log" : "Absolute";
            var outPath = Path.Combine(outDir, $"{GetLabel(type, 0, tolerance)}_{GetAmbigLabel(ambigLevel)}_MinFragmentsNeededByMissedMono_{variableLabel}Hist");
            chart.SavePNG(outPath, null, width, height);
        }

        #endregion

        #region Plot Generation

        public static GenericChart.GenericChart MinFragmentsNeededHist(this List<FragmentsNeededSummary> summaryRecords, string type, out int maxVal, int ambigLevel = 1, double tolerance = 10, int missedMono = 1)
        {
            maxVal = 0;
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in summaryRecords
                         .Where(p => p.AmbiguityLevel == ambigLevel && p.PpmTolerance == tolerance && p.MissedMonoisotopics == missedMono && p.FragmentationType == type)
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
                    var name =  $"{modCount} mods ({tolerance})";

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

                    var color = RadicalFragmentationPlotHelpers.ModToColorSetDict[modCount][index];
                    var chart = Chart.Column<int, string, string>(y, x,
                        Name: $"{modCount} mods ({tolerance})", MarkerColor: color);
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