using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.TraceObjects;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.FSharp.Core;
using Plotly.NET.LayoutObjects;

namespace RadicalFragmentation;

internal static class GenericPlots
{
    public static Layout DefaultLayoutWithLegend => Layout.init<string>(
        //PaperBGColor: Color.fromARGB(0, 0,0,0),
        //PlotBGColor: Color.fromARGB(0, 0, 0, 0),
        PaperBGColor: Color.fromKeyword(ColorKeyword.White),
        PlotBGColor: Color.fromKeyword(ColorKeyword.White),
        ShowLegend: true,
        Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
            VerticalAlign: StyleParam.VerticalAlign.Bottom,
            XAnchor: StyleParam.XAnchorPosition.Center,
            YAnchor: StyleParam.YAnchorPosition.Top
        ));

    public static Layout DefaultLayoutNoLegend => Layout.init<string>(
        //PaperBGColor: Color.fromARGB(0, 0,0,0),
        //PlotBGColor: Color.fromARGB(0, 0, 0, 0),
        PaperBGColor: Color.fromKeyword(ColorKeyword.White),
        PlotBGColor: Color.fromKeyword(ColorKeyword.White),
        ShowLegend: false);


    internal static GenericChart.GenericChart KernelDensityPlot(List<double> values, string title,
        string xTitle = "", string yTitle = "", double bandwidth = 0.2)
    {
        List<(double, double)> data = new List<(double, double)>();

        foreach (var sample in values.DistinctBy(p => p.Round(3)).OrderBy(p => p))
        {
            var pdf = KernelDensity.EstimateGaussian(sample, bandwidth, values);
        }

        var chart =
            Chart.Line<double, double, string>(data.Select(p => p.Item1), data.Select(p => p.Item2), Name: title,
                    LineColor: title.ConvertConditionToColor())
                .WithSize(400, 400)
                .WithXAxisStyle(Title.init(xTitle)/*, new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-15, 15))*/)
                .WithYAxisStyle(Title.init(yTitle))
                .WithLayout(DefaultLayoutWithLegend);
        return chart;
    }

    public static GenericChart.GenericChart Histogram(List<double> values, string title, string xTitle = "",
        string yTitle = "", bool normalize = false, (double, double)? minMax = null)
    {
        var chart = Chart.Histogram<double, double, string>(values, Name: title, MarkerColor: title.ConvertConditionToColor(),
                HistNorm: normalize ? StyleParam.HistNorm.Percent : StyleParam.HistNorm.None)
            .WithSize(400, 400)
            .WithTitle(title)
            .WithYAxisStyle(Title.init(yTitle))
            .WithLayout(DefaultLayoutWithLegend);
        if (minMax is not null)
            return chart.WithXAxisStyle(Title.init(xTitle),
                new FSharpOption<Tuple<IConvertible, IConvertible>>(
                    new Tuple<IConvertible, IConvertible>(minMax.Value.Item1, minMax.Value.Item2)));
        else
            return chart.WithXAxisStyle(Title.init(xTitle));
    }
}