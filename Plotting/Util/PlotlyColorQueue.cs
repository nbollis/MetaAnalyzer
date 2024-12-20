using System.Drawing;

namespace Plotting;

public sealed class PlotlyColorQueue : ColorQueue<Plotly.NET.Color>
{
    public PlotlyColorQueue(int setCapacity = 5) : base(setCapacity) { }

    protected override Plotly.NET.Color ConvertFromColor(Color color) => Plotly.NET.Color.fromRGB(color.R, color.G, color.B);

    protected override List<Plotly.NET.Color> ConvertListFromColor(List<Color> colorSet) => colorSet.Select(ConvertFromColor).ToList();
}