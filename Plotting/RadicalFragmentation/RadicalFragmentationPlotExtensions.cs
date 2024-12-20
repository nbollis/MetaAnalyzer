using Plotly.NET;
using MathNet.Numerics;
using Plotly.NET.LayoutObjects;
using RadicalFragmentation.Processing;
using RadicalFragmentation;
using System.Globalization;
using Microsoft.FSharp.Core;
using UsefulProteomicsDatabases;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET.ImageExport;
using Plotting.Util;
using ResultAnalyzerUtil;

namespace Plotting.RadicalFragmentation
{
    public static class RadicalFragmentationPlotExtensions
    {
        #region Plot Base

        private static Dictionary<string, Color> ConditionToColorDictionary = new()
        {
            {"0", Color.fromKeyword(ColorKeyword.RoyalBlue) },
            {"1", Color.fromKeyword(ColorKeyword.IndianRed) },
            {"2", Color.fromKeyword(ColorKeyword.MediumSpringGreen) },
            {"3", Color.fromKeyword(ColorKeyword.Orchid) },
            {"4", Color.fromKeyword(ColorKeyword.Gold) },
            {"5", Color.fromKeyword(ColorKeyword.Orange) },
        };

        private static Dictionary<int, (Color, Color)> MissedMonoHybridDict = new()
        {
            {0, (Color.fromKeyword(ColorKeyword.RoyalBlue), Color.fromKeyword(ColorKeyword.MediumBlue)) },
            {1, (Color.fromKeyword(ColorKeyword.IndianRed), Color.fromKeyword(ColorKeyword.Red)) },
            {2, (Color.fromKeyword(ColorKeyword.MediumSpringGreen), Color.fromKeyword(ColorKeyword.Green)) },
            {3, (Color.fromKeyword(ColorKeyword.Orchid), Color.fromKeyword(ColorKeyword.Indigo)) },
            {4, (Color.fromKeyword(ColorKeyword.Gold), Color.fromKeyword(ColorKeyword.DarkGoldenRod)) },
            {5, (Color.fromKeyword(ColorKeyword.Orange), Color.fromKeyword(ColorKeyword.SandyBrown)) },
        };

        private static Dictionary<string, string> ConditionNameConversionDictionary = new()
        {

        };

        private static Color ConvertConditionToColor(this string condition)
        {
            if (ConditionToColorDictionary.TryGetValue(condition, out var color))
                return color;
            else if (ConditionToColorDictionary.TryGetValue(condition.Trim(), out color))
                return color;
            else
            {
                if (ConditionNameConversionDictionary.ContainsValue(condition))
                {
                    var key = ConditionNameConversionDictionary.FirstOrDefault(x => x.Value == condition).Key;
                    if (key is null)
                        return Color.fromKeyword(ColorKeyword.Black);
                    if (ConditionToColorDictionary.TryGetValue(key, out color))
                        return color;
                }
                else
                {
                    ConditionToColorDictionary.Add(condition, PlottingTranslators.ColorQueue.Dequeue());
                    return ConditionToColorDictionary[condition];
                }
            }

            return Color.fromKeyword(ColorKeyword.Black);
        }

        #endregion

        public static void SaveToFigureDirectory(this RadicalFragmentationExplorer explorer,
            GenericChart.GenericChart chart, string outName, int? width = null, int? height = null)
        {
            if (!Directory.Exists(explorer.FigureDirectory))
                Directory.CreateDirectory(explorer.FigureDirectory);

            var outpath = Path.Combine(explorer.FigureDirectory, outName);
            chart.SavePNG(outpath, null, width, height);
        }

        public static void CreateAminoAcidFrequencyFigure(this RadicalFragmentationExplorer explorer, bool higherResolution = false)
        {
            var modifications = GlobalVariables.AllModsKnown;
            var proteins = ProteinDbLoader.LoadProteinXML(explorer.DatabasePath, true, DecoyType.None,
                modifications, false, new List<string>(), out var um);

            var sequences = proteins.Select(p => p.BaseSequence)
                .Distinct()
                .ToList();
            Dictionary<char, Dictionary<double, int>> aminoAcidCounts = sequences
                .SelectMany(p => p)
                .Distinct()
                .OrderBy(p => p)
                .ToDictionary(p => p, p => new Dictionary<double, int>());
            foreach (var baseSequence in sequences)
            {

                for (int i = 0; i < baseSequence.Length; i++)
                {
                    double loc = i == 0 ? 0 : Math.Round(i / (double)baseSequence.Length, higherResolution ? 2 : 1);
                    var toAdjust = aminoAcidCounts[baseSequence[i]];
                    if (!toAdjust.TryAdd(loc, 1))
                        toAdjust[loc] += 1;
                }
            }

            aminoAcidCounts = aminoAcidCounts
                .Where(p => p.Key != 'U' && p.Key != 'X' && (!higherResolution || p.Key != 'M'))
                .OrderBy(p => p.Value.Sum(m => m.Value))
                .ToDictionary(p => p.Key, p => p.Value);
            var aas = aminoAcidCounts.Select(p => p.Key).ToArray();
            var yTotals = aas.Select(p => (p, aminoAcidCounts[p].Sum(m => m.Value)))
                .ToDictionary(p => p.p, p => (double)p.Item2);
            var x = aminoAcidCounts.SelectMany(p => p.Value.Keys).Distinct()
                .OrderBy(p => p).ToArray();


            var z = aas.Select(aa => x.Select(loc =>
            {
                if (aminoAcidCounts[aa].TryGetValue(loc, out var count))
                    return (count / yTotals[aa] * 100.0).Round(1);
                return 0.0;
            }).ToArray()).ToArray();

            int places = yTotals.Max(p => p.Value.ToString(CultureInfo.InvariantCulture).Length);
            var y = aas.Select(aa =>
            {
                var yTotal = ((int)yTotals[aa]).ToString().Length;
                var spacesToAdd = places - yTotal + 1;
                return $"{aa}:{new string(' ', spacesToAdd)}{yTotals[aa].Round(0)}";
            }).ToArray();

            var annotationText = z.Select(p => p.Select(m => $"{m}%").ToArray()).ToArray();
            var distinctMap =
                Chart.Heatmap<double, double, string, string>(z, X: x, Y: y, ShowLegend: true, Name: "Percent By Location")
                    .WithXAxisStyle(Title.init("N-Term -> C-Term"))
                    .WithYAxisStyle(Title.init("Amino Acid: Total in Database"))
                    .WithZAxisStyle(Title.init("Percent By Location"))
                    .WithTitle($"{explorer.Species} Amino Acid Frequency By Location");
            string outType = higherResolution ? "_HighRes" : "";
            var outName = $"{explorer.Species}_AminoAcidFrequencyByLocation{outType}";
            explorer.SaveToFigureDirectory(distinctMap, outName, 1000, 1000);
        }

        public static void CreatePlots(this List<RadicalFragmentationExplorer> explorers)
        {
            var typeCollection = explorers.Select(p => p.AnalysisType).Distinct().ToArray();
            if (typeCollection.Count() > 1)
                throw new ArgumentException("All explorers must be of the same type");
            var type = typeCollection.First();

            foreach (var speciesGroup in explorers.GroupBy(p => p.Species)
                         .ToDictionary(p => p.Key, p => p.ToList()))
            {
                string outName;
                var hist = speciesGroup.Value.SelectMany(p => p.FragmentHistogramFile).ToList();
                var frag = speciesGroup.Value.SelectMany(p => p.MinFragmentNeededFile.Results).ToList();

                var trueMax = 0;
                if (speciesGroup.Value.Any(p => p.AmbiguityLevel == 1))
                    GeneratePlotsOnOneAmbigLevel(speciesGroup, type, frag, hist, 1);
                
                if (speciesGroup.Value.Any(p => p.AmbiguityLevel == 2))
                    GeneratePlotsOnOneAmbigLevel(speciesGroup, type, frag, hist, 2);
            }
        }

        private static void GeneratePlotsOnOneAmbigLevel(KeyValuePair<string, List<RadicalFragmentationExplorer>> speciesGroup, string type, 
            List<FragmentsToDistinguishRecord> records, List<FragmentHistogramRecord> hist, int ambigLevel)
        {
            string typeText = ambigLevel == 1
                ? "Proteoform"
                : "Protein";


            var uniqueFragmentsPlot = hist.GetProteinByUniqueFragmentsLine(ambigLevel, speciesGroup.Key);

            string outName = $"{type}_UniqueFragmentMasses_{speciesGroup.Key}_{typeText}Level";
            speciesGroup.Value.First().SaveToFigureDirectory(uniqueFragmentsPlot, outName, 1000, 600);


            var fragmentsNeededHist = records.GetFragmentsNeededHistogram(out int maxVal, ambigLevel, speciesGroup.Key)
                .WithAxisAnchor(Y: 1);

            outName = $"{type}_FragmentsNeeded_{speciesGroup.Key}_{typeText}Level";
            speciesGroup.Value.First().SaveToFigureDirectory(fragmentsNeededHist, outName, 1000, 600);


            var cumulativeFragmentsHist = records.GetCumulativeFragmentsNeededChart(ambigLevel, speciesGroup.Key)
                .WithAxisAnchor(Y: 2);

            outName = $"{type}_CumulativeFragmentsNeeded_{speciesGroup.Key}_{typeText}Level";
            speciesGroup.Value.First().SaveToFigureDirectory(cumulativeFragmentsHist, outName, 1000, 600);


            var combined = Chart.Combine(new[] {  fragmentsNeededHist, cumulativeFragmentsHist })
                .WithTitle($"{speciesGroup.Value.First().AnalysisLabel} Fragments to distinguish from other Proteoforms")
                .WithYAxisStyle(Title.init("Log Count"), 
                    Side: StyleParam.Side.Left,
                    Id: StyleParam.SubPlotId.NewYAxis(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, maxVal)))
                .WithYAxisStyle(Title.init("Percent Identified"), 
                    Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(2),
                    Overlaying: StyleParam.LinearAxisId.NewY(1),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);

            outName = $"{type}_CombinedFragmentsNeeded_{speciesGroup.Key}_{typeText}Level";
            speciesGroup.Value.First().SaveToFigureDirectory(combined, outName, 1000, 600);
        }


        public static void CreateMissedMonoCombinedCumulativeFragCountPlot(this List<RadicalFragmentationExplorer> explorers)
        {
            var typeCollection = explorers.Select(p => p.AnalysisType).Distinct().ToArray();
            if (typeCollection.Count() > 1)
                throw new ArgumentException("All explorers must be of the same type");
            var labelCollecion = explorers.Select(p => p.AnalysisLabel).Distinct().ToArray();
            if (labelCollecion.Count() == 1)
                throw new ArgumentException("Exploreres must have different labels e.g. missed mono vs not missed mono");
            var speciesCollection = explorers.Select(p => p.Species).Distinct().ToArray();
            if (speciesCollection.Count() > 1)
                throw new ArgumentException("All explorers must be of the same species");


            string speciesGroup = speciesCollection.First();    
            var toProcess = explorers.GroupBy(p => (p.AnalysisType, p.AmbiguityLevel))
                .ToDictionary(p => p.Key,
                    p => p.ToList());


            foreach (var analysisTypeSet in toProcess)
            {
                string type = analysisTypeSet.Key.AnalysisType;
                var ambigLevel = analysisTypeSet.Key.AmbiguityLevel;

                int maxToDifferentiate = analysisTypeSet.Value
                    .Max(p => p.MinFragmentNeededFile
                        .Max(m => m.FragmentCountNeededToDifferentiate));
                var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();

                List<GenericChart.GenericChart> toCombine = new();
                foreach (var modGroup in analysisTypeSet.Value
                             .GroupBy(p => p.NumberOfMods))
                {
                    int modCount = modGroup.Key;
                    foreach (var result in modGroup)
                    {
                        var records = result.MinFragmentNeededFile.Results;

                        double total = records.Count();
                        var toSubtract = records.Count(p => p.FragmentCountNeededToDifferentiate == -1);

                        var yVal = xInteger.Select(p => (records.Count(m => m.FragmentCountNeededToDifferentiate <= p) - toSubtract) / total * 100)
                            .ToArray();
                        var xVal = xInteger.Select(p => p.ToString()).ToArray();
                        xVal[0] = "No ID";
                        xVal[1] = "Precursor Only";


                        // missed mono plot differences
                        var missedMonos = result.MissedMonoIsotopics;
                        var colorOptions = MissedMonoHybridDict[modCount];
                        var color = missedMonos == 0 ? colorOptions.Item1 : colorOptions.Item2;
                        var lineDash = missedMonos == 0 ? StyleParam.DrawingStyle.Solid : StyleParam.DrawingStyle.Dash;
                        var name = missedMonos == 0 ? $"{modCount} mods" : $"{modCount} mods with Missed Mono";

                        var chart = Chart.Spline<string, double, string>(xVal, yVal, true, 0.0, LineDash: lineDash,
                            Name: name, MultiText: yVal.Select(p => $"{p.Round(2)}%").ToArray(), MarkerColor: color);
                        toCombine.Add(chart);
                    }
                }

                string typeText = ambigLevel == 1
                    ? "Proteoform"
                    : "Protein";

                var combined = Chart.Combine(toCombine)
                    .WithTitle(
                        $"{speciesGroup}: {typeText}s Identified by Number of Fragments")
                    .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                    .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Tick0: 0, DTick: 1))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                    .WithYAxisStyle(Title.init($"Percent of {typeText}s Identified"), MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0, 100)));

                var outName = $"{type}_HybridMono_CumulativeFragmentsNeeded_{speciesGroup}_{typeText}Level";
                analysisTypeSet.Value.First().SaveToFigureDirectory(combined, outName, 1000, 600);
            }
        }



        public static GenericChart.GenericChart GetProteinByUniqueFragmentsLine(this List<FragmentHistogramRecord> records,
            int ambiguityLevel = 1, string species = "")
        {
            List<GenericChart.GenericChart> toCombine = new();

            foreach (var modGroup in records
                         .Where(p => p.AmbiguityLevel == ambiguityLevel)
                         .GroupBy(p => p.NumberOfMods)
                         .OrderBy(p => p.Key))
            {
                var color = modGroup.Key.ToString().ConvertConditionToColor();
                var x = modGroup.Select(p => p.FragmentCount);
                var y = modGroup.Select(p => p.ProteinCount);
                var chart = Chart.Spline<int, int, string>(x, y, true, 2, $"{modGroup.Key} mods", MarkerColor: color);

                //var chart = Chart.Histogram<int, int, string>(x.ToArray(), y.ToArray(), ShowLegend: true, Name: $"{modGroup.Key} mods", 
                   // HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Sum);
                toCombine.Add(chart);
            }

            string typeText = ambiguityLevel == 1
                ? "Proteoform"
                : "Protein";
            var combined = Chart.Combine(toCombine)
                .WithTitle($"{species} Fragments per {typeText} (Ambiguity Level {ambiguityLevel})")
                .WithXAxisStyle(Title.init("Fragment Count"))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithYAxisStyle(Title.init($"Count of {typeText}s"));

            return combined;
        }

        public static GenericChart.GenericChart GetFragmentsNeededHistogram(this List<FragmentsToDistinguishRecord> records, out int maxVal,
            int ambiguityLevel = 1, string species = "")
        {
            maxVal = 0;
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in records
                         .Where(p => p.AmbiguityLevel == ambiguityLevel)
                         .GroupBy(p => p.NumberOfMods))
            {
                var color = modGroup.Key.ToString().ConvertConditionToColor();
                var temp = modGroup.GroupBy(p => p.FragmentCountNeededToDifferentiate)
                    .OrderBy(p => p.Key)
                    .Select(p => (p.Key, p.Count())).ToArray();

                var x = Enumerable.Range(-1, temp.Max(p => p.Key)+2).Select(p => p.ToString()).ToArray();
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

            string typeText = ambiguityLevel == 1
                ? "Proteoform"
                : "Protein";
            var combined = Chart.Combine(toCombine)
                .WithTitle(
                    $"{species} Fragments Needed to Distinguish from other {typeText}s (Ambiguity Level {ambiguityLevel})")
                .WithXAxisStyle(Title.init("Fragments Needed"))
                .WithYAxisStyle(Title.init($"Log(Count of {typeText}s)"))
                .WithYAxis(LinearAxis.init<int, int, int, int, int, int>(AxisType: StyleParam.AxisType.Log))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            
            return combined;
        }

        public static GenericChart.GenericChart GetCumulativeFragmentsNeededChart(
            this List<FragmentsToDistinguishRecord> records,
            int ambiguityLevel = 1, string species = "")
        {
            int maxToDifferentiate = records.Max(p => p.FragmentCountNeededToDifferentiate);

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var modGroup in records
                         .Where(p => p.AmbiguityLevel == ambiguityLevel)
                         .GroupBy(p => p.NumberOfMods))
            {
                double total = modGroup.Count();
                var color = modGroup.Key.ToString().ConvertConditionToColor();
                var toSubtract = modGroup.Count(p => p.FragmentCountNeededToDifferentiate == -1);

                var xInteger = Enumerable.Range(-1, maxToDifferentiate + 2).ToList();

                var yVal = xInteger.Select(p => (modGroup.Count(m => m.FragmentCountNeededToDifferentiate <= p) - toSubtract) / total * 100)
                    .ToArray();
                var xVal = xInteger.Select(p => p.ToString()).ToArray();
                xVal[0] = "No ID";
                xVal[1] = "Precursor Only";
                var chart = Chart.Spline<string, double, string>(xVal, yVal, true, 0.0,
                    Name: $"{modGroup.Key} mods", MultiText: yVal.Select(p => $"{p.Round(2)}%").ToArray(), MarkerColor: color);
                toCombine.Add(chart);
            }

            string typeText = ambiguityLevel == 1
                ? "Proteoform"
                : "Protein";
            var combined = Chart.Combine(toCombine)
                .WithTitle(
                    $"{species}: {typeText}s Identified by Number of Fragments")
                .WithXAxisStyle(Title.init($"Fragment Ions Required"))
                .WithXAxis(LinearAxis.init<int, int, int, int, int, int>(Tick0: 0, DTick: 1))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithYAxisStyle(Title.init($"Percent of {typeText}s Identified"), 
                MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new(0,100)));
            return combined;
        }
    }
}
