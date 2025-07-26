using Analyzer.FileTypes.Internal;
using Plotly.NET;
using Plotting.Util;

namespace Analyzer.Plotting.ComparativePlots
{
    public static class ChimeraCountingPlots
    {

        public static GenericChart.GenericChart GetChimeraCountingPlot(this List<ChimeraCountingResult> results, bool isTopDown = false)
        {
            
            List<GenericChart.GenericChart> toCombine = new List<GenericChart.GenericChart>();
            foreach (var softwareGroup in results.GroupBy(p => p.Software))
            {
                var toPlot = softwareGroup.ToList();

                var software = softwareGroup.Key;
                var xArray = toPlot.Select(p => p.IdsPerSpectra).ToArray();
                var yArray = toPlot.Select(p => p.OnePercentIdCount).ToArray();

                var plot = Chart2D.Chart.Column<int, int, string, int, int>(yArray, xArray, Name: software.ConvertConditionName(), MarkerColor: software.ConvertConditionToColor())
                    .WithXAxisStyle(Title.init($"1% {Labels.GetSpectrumMatchLabel(isTopDown)} Spectra", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                    .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                    .WithSize(800, 800);
                toCombine.Add(plot);
            }

            var chart = Chart.Combine(toCombine.ToArray())
                .WithTitle($"1% {Labels.GetSpectrumMatchLabel(isTopDown)} per Spectra", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithSize(1400, 800);
            chart.Show();
            return chart;
        }
    }
}
