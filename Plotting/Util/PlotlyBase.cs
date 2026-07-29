using Plotly.NET.LayoutObjects;
using Plotly.NET;
using Plotly.NET.ImageExport;

namespace Plotting.Util
{
    public static class PlotlyBase
    {
        public static readonly int TitleSize = 32;
        public static readonly int AxisTitleFontSize = 28;

        public static readonly int DefaultExportWidth = 1600;
        public static readonly int DefaultExportHeight = 1200;
        public static readonly int DefaultSquareExportSize = 1400;
        public static readonly int DefaultWideExportWidth = 1800;
        public static readonly int DefaultWideExportHeight = 1200;

        public static int DefaultHeight = 600;
        public static Layout DefaultLayout =>
            Layout.init<string>(PaperBGColor: Color.fromKeyword(ColorKeyword.White), PlotBGColor: Color.fromKeyword(ColorKeyword.White));

        public static void ExportFigure(GenericChart.GenericChart chart, string outPath, int? width = null, int? height = null)
        {
            int exportWidth = width ?? DefaultExportWidth;
            int exportHeight = height ?? DefaultExportHeight;

            try
            {
                chart.SavePNG(outPath, ExportEngine.PuppeteerSharp, exportWidth, exportHeight);
            }
            catch
            {
                try
                {
                    chart.SaveSVG(outPath, ExportEngine.PuppeteerSharp, exportWidth, exportHeight);
                }
                catch
                {
                    try
                    {
                        chart.SaveJPG(outPath, ExportEngine.PuppeteerSharp, exportWidth, exportHeight);
                    }
                    catch
                    {
                        Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(chart, outPath, false);
                    }
                }
            }
        }

        public static Legend DefaultLegend => Legend.init(X: 0.5, Y: -0.1, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
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
        public static Legend DefaultLegend20 => Legend.init(X: 0.5, Y: -0.2, Orientation: StyleParam.Orientation.Horizontal, EntryWidth: 0,
            VerticalAlign: StyleParam.VerticalAlign.Bottom,
            XAnchor: StyleParam.XAnchorPosition.Center,
            YAnchor: StyleParam.YAnchorPosition.Top,
            Font: Font.init(null, 20, null)
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
            Legend: DefaultLegend16,
            Font: Font.init(null, 12, null));
        public static Layout DefaultLayoutWithLegendLargeText => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: DefaultLegend16,
            Font: Font.init(null, 16, null));

        public static Layout DefaultLayoutWithLegendLargerText => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: DefaultLegend20,
            Font: Font.init(null, 18, null));

        public static Layout DefaultLayoutWithLegendLargererText => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: true,
            Legend: DefaultLegend20,
            Font: Font.init(null, 24, null));
        public static Layout DefaultLayoutNoLegend => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: false);

        public static Layout DefaultLayoutNoLegendLargererText => Layout.init<string>(
            PaperBGColor: Color.fromKeyword(ColorKeyword.White),
            PlotBGColor: Color.fromKeyword(ColorKeyword.White),
            ShowLegend: false,
            Font: Font.init(null, 24, null));

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
