using Plotly.NET.LayoutObjects;
using Plotly.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Plotting.Util
{
    public static class PlotlyBase
    {
        public static int DefaultHeight = 600;
        public static Layout DefaultLayout => Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

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

        public static Layout DefaultLayoutWithLegendTransparentBackground => Layout.init<string>(
            PaperBGColor: Color.fromARGB(0, 0, 0, 0),
            PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            ShowLegend: true,
            Legend: Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
                VerticalAlign: StyleParam.VerticalAlign.Bottom,
                XAnchor: StyleParam.XAnchorPosition.Center,
                YAnchor: StyleParam.YAnchorPosition.Top
            ));
    }
}
