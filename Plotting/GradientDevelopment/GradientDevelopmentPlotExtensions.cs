using GradientDevelopment;
using Microsoft.FSharp.Core;
using Plotly.NET.CSharp;
using Plotly.NET;
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
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Avg,
                    NBinsX: numBins / 4)
                .WithAxisAnchor(Y: 1);

            var idCount = Chart.Histogram<double, double, string>(
                    run.FivePercentIds.Select(p => p.Item1).ToArray(),
                    run.FivePercentIds.Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.DarkGreen),
                    Name: "5% OSMs",
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Avg,
                    NBinsX: numBins / 4)
                .WithAxisAnchor(Y: 1);

            var tic = Chart.Histogram<double, double, string>(
                    run.Tic.Select(p => p.Item1).ToArray(),
                    run.Tic.Select(p => p.Item2).ToArray(),
                    MarkerColor: Color.fromKeyword(ColorKeyword.Blue),
                    Name: "TIC",
                    HistNorm: StyleParam.HistNorm.None, HistFunc: StyleParam.HistFunc.Avg,
                    NBinsX: numBins)
                .WithAxisAnchor(Y: 3);

            var markers = run.Gradient.Select(p => $"{p.Item2}%").ToArray();
            var gradient = Chart.Line<double, double, string>(
                run.Gradient.Select(p => p.Item1).ToArray(),
                run.Gradient.Select(p => p.Item2).ToArray(),
                MarkerColor: run.MobilePhaseB.ConvertConditionToColor(),
                MultiText: markers,
                    LineWidth: 2,
                    LineDash: new Optional<StyleParam.DrawingStyle>(StyleParam.DrawingStyle.LongDashDot, true),
                    Name: $"%{run.MobilePhaseB}")
                .WithAxisAnchor(Y: 4);

            var toReturn = Chart.Combine(new[] { tic, idCount, allIdCount, gradient })
                .WithYAxisStyle(Title.init("OSM Count"), Side: StyleParam.Side.Left, Position: .05,
                    Id: StyleParam.SubPlotId.NewYAxis(1))
                //.WithYAxisStyle(Title.init("5% OSMs"), Side: StyleParam.Side.Left, Position: .1,
                //    Id: StyleParam.SubPlotId.NewYAxis(2), Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithYAxisStyle(Title.init("TIC"), Side: StyleParam.Side.Right,
                    Id: StyleParam.SubPlotId.NewYAxis(3), Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithYAxisStyle(Title.init($"%{run.MobilePhaseB}"), Side: StyleParam.Side.Left,
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0, 100)),
                    Id: StyleParam.SubPlotId.NewYAxis(4), Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithXAxisStyle(Title.init("Retention Time"),
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.15, 1)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithSize(1200, 700)
                .WithTitle($"{run.MobilePhaseB} - {run.DataFileName}");

            return toReturn;
        }

    }
}
