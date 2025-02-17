using Plotly.NET.LayoutObjects;
using Plotly.NET;

namespace Plotting.Util
{
    public static class PlotlyBase
    {
        public static readonly int TitleSize = 24;
        public static readonly int AxisTitleFontSize = 18;

        public static int DefaultHeight = 600;
        public static Layout DefaultLayout =>
            Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

        public static Legend DefaultLegend => Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
            VerticalAlign: StyleParam.VerticalAlign.Bottom,
            XAnchor: StyleParam.XAnchorPosition.Center,
            YAnchor: StyleParam.YAnchorPosition.Top
        );
        public static Legend DefaultLegend16 => Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
            VerticalAlign: StyleParam.VerticalAlign.Bottom,
            XAnchor: StyleParam.XAnchorPosition.Center,
            YAnchor: StyleParam.YAnchorPosition.Top,
            Font: Font.init(null, 16, null)
        ); 
        public static Legend DefaultLegend24 => Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
            VerticalAlign: StyleParam.VerticalAlign.Bottom,
            XAnchor: StyleParam.XAnchorPosition.Center,
            YAnchor: StyleParam.YAnchorPosition.Top,
            Font: Font.init(null, 16, null)
        );

        public static Layout JustLegend => Layout.init<string>(
            //PaperBGColor: Color.fromKeyword(ColorKeyword.White), 
            //PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            Legend: DefaultLegend   
            );


        public static Layout DefaultLayoutWithLegend => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: DefaultLegend,
            Font: Font.init(null, 12, null));
        public static Layout DefaultLayoutWithLegendLargeText => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: DefaultLegend16,
            Font: Font.init(null, 16, null));

        public static Layout DefaultLayoutWithLegendLargerText => Layout.init<string>(
            ShowLegend: true,
            Legend: DefaultLegend24, 
            Font: Font.init(null, 24, null));
        public static Layout DefaultLayoutNoLegend => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: false);

        public static Layout DefaultLayoutWithLegendTransparentBackground => Layout.init<string>(
            PaperBGColor: Color.fromARGB(0, 0, 0, 0),
            PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            ShowLegend: true,
            Legend: DefaultLegend);
        public static Layout DefaultLayoutNoLegendTransparentBackground => Layout.init<string>(
            PaperBGColor: Color.fromARGB(0, 0, 0, 0),
            PlotBGColor: Color.fromARGB(0, 0, 0, 0),
            ShowLegend: false
            );
    }
}
