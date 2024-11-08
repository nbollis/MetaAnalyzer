using System.Security.Cryptography;
using GradientDevelopment;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.FSharp.Core;
using Plotly.NET.CSharp;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using Chart = Plotly.NET.CSharp.Chart;
using Plotting.Util;

namespace Plotting.GradientDevelopment
{
    public static class GradientDevelopmentPlotExtensions
    {
        public static GenericChart.GenericChart GetPlotHist(this ExtractedInformation run)
        {
            int numBins = ((int)run.Tic.Max(p => p.Item1) + 1) * 10;

            var allIdCount = Chart.Histogram<double, double, string>(
                    run.Ids.Select(p => p.Item1).ToArray(),
                    run.Ids.Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.MediumTurquoise),
                    Name: "All OSMs",
                    Opacity: 0.7,
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Count,
                    NBinsX: numBins / 4)
                .WithAxisAnchor(Y: 1);

            var idCount = Chart.Histogram<double, double, string>(
                    run.FivePercentIds.Select(p => p.Item1).ToArray(),
                    run.FivePercentIds.Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.DarkGreen),
                    Name: "5% OSMs",
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Count,
                    NBinsX: numBins / 4)
                .WithAxisAnchor(Y: 1);

            var tic = Chart.Histogram<double, double, string>(
                    run.Tic.Select(p => p.Item1).ToArray(),
                    run.Tic.Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.Black),
                    Name: "TIC", 
                    Opacity: 0.5,
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Avg,
                    NBinsX: numBins)
                .WithAxisAnchor(Y: 3);

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
                .WithAxisAnchor(Y: 4);

            
            var min = run.MinRtToDisplay ?? 0;
            var max = run.MaxRtToDisplay ?? run.Tic.Last().Item1;
            var mid = (min + max) / 2;

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

            var toReturn = Chart.Combine(new[] { tic, idCount, allIdCount, gradient })
                .WithXAxisStyle(Title.init($"Retention Time"),
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.15, 1)),
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(min, max)))
                .WithYAxisStyle(Title.init("OSM Count (30s bin)"), Side: StyleParam.Side.Left, Position: .05,
                    Id: StyleParam.SubPlotId.NewYAxis(1),
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.05, 1)))
                .WithYAxisStyle(Title.init("TIC"), Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(3), Overlaying: StyleParam.LinearAxisId.NewY(1),
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.05, 1)))
                .WithYAxisStyle(Title.init($"%{run.MobilePhaseB}"), Side: StyleParam.Side.Left, 
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, 100)),
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.05, 1)),
                    Id: StyleParam.SubPlotId.NewYAxis(4), Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithAnnotations(annotations)
                .WithSize(1200, 700)
                .WithTitle($"{run.MobilePhaseB} - {run.DataFileName}");


            string temp = $"MS2 Scans: {run.Ms2ScansCollected}  ---  Precursors Fragmented: {run.PrecursorsFragmented}";
            return toReturn;
        }

    }
}
