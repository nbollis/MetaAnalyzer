using System.Diagnostics;
using System.Security.Cryptography;
using GradientDevelopment;
using GradientDevelopment.Temporary;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.FSharp.Core;
using MzLibUtil;
using Plotly.NET.CSharp;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.CSharp.GenericChartExtensions;
using Plotting.Util;
using ResultAnalyzerUtil;
using MathNet.Numerics.Distributions;
using Plotly.NET.ImageExport;

namespace Plotting.GradientDevelopment
{
    public static class GradientDevelopmentPlotExtensions
    {
        public static void SavePlotHists(this ExperimentalBatch batch, int featuresToMap = 30)
        {
            var outDir = batch.ProcessedResultsDirectory;
            foreach (var run in batch)
            {
                var extractedInfo = batch.ExtractedInformationFile.First(p => p.DataFileName == run.DataFileName);
                var plot = GetPlotHist(extractedInfo, run.Ms1FeatureFile, featuresToMap);
                var path = Path.Combine(outDir,
                            $"{FileIdentifiers.GradientFigure}_{run.DataFileName}_{run.Gradient.Name}");

                plot.SavePNG(path, null, 1200, 700);
            }
        }


        public static GenericChart.GenericChart GetPlotHist(this ExtractedInformation run, Ms1FeatureFile? featureFile = null,
            int featuresToMap = 5)
        {
            int multiplier = 10;
            int osmDivisor = 4;
            var min = run.MinRtToDisplay ?? 0;
            var max = run.MaxRtToDisplay ?? run.Tic.Last().Item1;
            var mid = (min + max) / 2;
            int numBins = ((int)run.Tic
                .Where(p => p.Item1 > min && p.Item1 < max)
                .Max(p => p.Item1) + 1) * multiplier;
            var ticMax = run.Tic
                .Where(p => p.Item1 > min && p.Item1 < max)
                .GroupBy(p => (p.Item1 / multiplier * osmDivisor).Round(1))
                .Max(p => p.Average(m => m.Item2));
            

            var allIdCount = Chart.Histogram<double, double, string>(
                    run.Ids.Where(p => p.Item1 > min && p.Item1 < max).Select(p => p.Item1).ToArray(),
                    run.Ids.Where(p => p.Item1 > min && p.Item1 < max).Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.MediumTurquoise),
                    Name: "All OSMs",
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Count,
                    NBinsX: numBins / osmDivisor)
                .WithAxisAnchor(Y: 1);

            var idCount = Chart.Histogram<double, double, string>(
                    run.FivePercentIds.Where(p => p.Item1 > min && p.Item1 < max).Select(p => p.Item1).ToArray(),
                    run.FivePercentIds.Where(p => p.Item1 > min && p.Item1 < max).Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.DarkGreen),
                    Name: "5% OSMs",
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Count,
                    NBinsX: numBins / osmDivisor)
                .WithAxisAnchor(Y: 1);

            var toPlot = run.Tic.Where(p => p.Item1 >= min && p.Item1 <= max).ToArray();
            var tic = Chart.Histogram<double, double, string>(
                    toPlot.Where(p => p.Item1 > min && p.Item1 < max).Select(p => p.Item1).ToArray(),
                    toPlot.Where(p => p.Item1 > min && p.Item1 < max).Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.Black),
                    Name: "TIC",
                    Opacity: 0.8,
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Avg,
                    NBinsX: numBins)
                .WithAxisAnchor(Y: 2)
                .WithXAxisStyle(Title.init($"Retention Time"),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(min, max)));


            var markers = run.Gradient.Select(p => $"{p.Item2}%").ToArray();
            for (int i = 1; i < markers.Length; i++)
                if (markers[i] == markers[i - 1])
                    markers[i] = "";
         
            var gradient = Chart.Line<double, double, string>(
                run.Gradient.Select(p => p.Item1).ToArray(),
                run.Gradient.Select(p => p.Item2).ToArray(),
                MarkerColor: run.MobilePhaseB.ConvertConditionToColor(),
                ShowMarkers: true,
                MarkerSymbol: new Optional<StyleParam.MarkerSymbol>(StyleParam.MarkerSymbol.StarDiamond, true),
                MultiText: markers,
                TextPosition: new Optional<StyleParam.TextPosition>(StyleParam.TextPosition.TopCenter, true),
                LineWidth: 2,
                LineDash: new Optional<StyleParam.DrawingStyle>(StyleParam.DrawingStyle.LongDashDot, true),
                Name: $"%{run.MobilePhaseB}")
                .WithAxisAnchor(Y: 3);

            
            var annotations = new List<Annotation>()
            {
                Annotation.init<double, double, double, double, double, double, double, double, double, string>
                (mid, 1, YRef: "paper", 
                    YAnchor: new FSharpOption<StyleParam.YAnchorPosition>(StyleParam.YAnchorPosition.Top),
                    Align: new FSharpOption<StyleParam.AnnotationAlignment>(StyleParam.AnnotationAlignment.Center),
                    VAlign: new FSharpOption<StyleParam.VerticalAlign>(StyleParam.VerticalAlign.Top),
                    ArrowColor: Color.fromARGB(0, 0,0,0),
                    BGColor: Color.fromKeyword(ColorKeyword.White),
                    BorderColor: Color.fromKeyword(ColorKeyword.DarkSlateGray),
                    Text: $"OSMs: {run.OSMCount}   ---   Oligos: {run.OligoCount}<br>" +
                          $"MS2 Scans: {run.Ms2ScansCollected}  ---  Precursors Fragmented: {run.PrecursorsFragmented}"),
            };

            var curves = new List<GenericChart.GenericChart>();
            if (featureFile is not null)
            {
                var osms = StoredInformation.RunInformationList.First(p => p.DataFileName == run.DataFileName)
                    .OsmFromTsv;

                var consensus = ResultFileConsensus.GetConsensusIds(osms, featuresToMap);
                var tolerance = new AbsoluteTolerance(20);
                foreach (var consensusRecord in consensus)
                {
                   
                    if (!consensusRecord.SpectralMatchesByFileName.TryGetValue(run.DataFileName, out var matches))
                        continue;

                    var filteredMatches = matches.Where(p => p.QValue <= 0.4).ToArray();
                    if (!filteredMatches.Any())
                        continue;

                    var fullSequence = filteredMatches.First().FullSequence;
                    var weightedAvgRt = filteredMatches.WeightedAverage(p => p.RetentionTime!.Value,
                        p => 1 - p.QValue);
                    var avgCharge = Math.Abs(filteredMatches.WeightedAverage(p => p.PrecursorCharge,
                        p => 1 - p.QValue));

                    var fullFiltered = featureFile.Where(p =>
                            p.RetentionTimeBegin != p.RetentionTimeApex && p.RetentionTimeEnd != p.RetentionTimeApex
                            && p.RetentionTimeBegin >= min && p.RetentionTimeEnd <= max
                            && p.RetentionTimeBegin - 30 <= weightedAvgRt && p.RetentionTimeEnd + 30 >= weightedAvgRt
                            && p.ChargeStateMin - 1 <= avgCharge && p.ChargeStateMax + 1 >= avgCharge
                            && tolerance.Within(p.Mass, double.Parse(matches.First().MonoisotopicMass)))
                        .OrderByDescending(p => p.ChargeStateMax - p.ChargeStateMin)
                        .ThenBy(p => Math.Abs(p.Mass - double.Parse(matches.First().MonoisotopicMass)))
                        .ToList();

                    if (fullFiltered.Count == 0)
                        continue;

                    var toUse = fullFiltered.First();
                    // construct a gaussian curve from RtEnd to RtStart centered on RtApex apex height being apex Intensity and area under the curve being Intensity
                    double mean = toUse.RetentionTimeApex;
                    double stdDev = (toUse.RetentionTimeEnd - toUse.RetentionTimeBegin) / 6; // 99.7% of data within 3 std devs
                    var gaussian = new Normal(mean, stdDev);

                    List<double> rtValues = new();
                    List<double> intensityValues = new();
                    for (double rt = toUse.RetentionTimeBegin; rt <= toUse.RetentionTimeEnd; rt += 0.01)
                    {
                        var density = gaussian.Density(rt) * toUse.Intensity;
                        if (density <= 10000)
                            continue;

                        rtValues.Add(rt);
                        intensityValues.Add(density);
                    }

                    // Add the gaussian curve to the plot
                    var gaussianCurve = Chart.Line<double, double, string>(
                        rtValues.ToArray(),
                        intensityValues.ToArray(),
                        MarkerColor: fullSequence.ConvertConditionToColor(),
                        ShowLegend: false,
                        LineWidth: 2)
                        .WithAxisAnchor(Y: 2);

                    //var annotation =
                    //    Annotation.init<double, double, double, double, double, double, double, double, double, string>
                    //    (mean, toUse.Intensity, YRef: "y3",
                    //        Text: $"{baseSeq}",
                    //        Align: new FSharpOption<StyleParam.AnnotationAlignment>(StyleParam.AnnotationAlignment.Left),
                    //        //VAlign: new FSharpOption<StyleParam.VerticalAlign>(StyleParam.VerticalAlign.Top),
                    //        TextAngle: 300,
                    //        ArrowColor: fullSequence.ConvertConditionToColor());

                    curves.Add(gaussianCurve);
                }
            }

            PlotIt:
            var toCombine = new List<GenericChart.GenericChart> { tic, idCount, allIdCount, gradient };
            toCombine.AddRange(curves);
            var toReturn = Chart.Combine(toCombine)
                .WithXAxisStyle(Title.init($"Retention Time"),
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.15, 1)),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(min, max)))
                .WithYAxisStyle<double, double, string>($"OSM Count ({(int)(60.0 / (multiplier / (double)osmDivisor))}s bin)", 
                    Side: StyleParam.Side.Left, 
                    Id: StyleParam.SubPlotId.NewYAxis(1)
                    )
                .WithYAxisStyle<double, double, string>("TIC",
                    Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(2),
                    MinMax: new Tuple<double, double>(0, ticMax),
                    Overlaying: StyleParam.LinearAxisId.NewY(1)
                )
                .WithYAxisStyle<double, double, string>($"%{run.MobilePhaseB}", 
                    Side: StyleParam.Side.Left, 
                    Position: .05,
                    Overlaying: StyleParam.LinearAxisId.NewY(1),
                    Id: StyleParam.SubPlotId.NewYAxis(3),
                    MinMax: new Tuple<double, double>(0, 100)
                    )
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithAnnotations(annotations)
                .WithSize(1000, 600)
                .WithTitle($"{run.MobilePhaseB} - {run.DataFileName}");

            return toReturn;
        }

      

        public static double WeightedAverage(this IEnumerable<SpectrumMatchFromTsv> source, Func<SpectrumMatchFromTsv, double> valueSelector, Func<SpectrumMatchFromTsv, double> weightSelector)
        {
            double weightedValueSum = source.Sum(x => valueSelector(x) * weightSelector(x));
            double weightSum = source.Sum(weightSelector);
            return weightedValueSum / weightSum;
        }
    }
}
