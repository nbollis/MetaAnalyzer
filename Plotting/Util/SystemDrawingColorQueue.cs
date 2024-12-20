using System.Drawing;

namespace Plotting;

public sealed class SystemDrawingColorQueue : ColorQueue<Color>
{
    public SystemDrawingColorQueue(int setCapacity = 5) : base(setCapacity) { }

    protected override Color ConvertFromColor(Color color) => color;

    protected override List<Color> ConvertListFromColor(List<Color> colorSet) => colorSet;
}