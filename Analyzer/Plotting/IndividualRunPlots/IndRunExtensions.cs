using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.Util;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.TraceObjects;

namespace Analyzer.Plotting
{
    public static class IndRunExtensions
    {
        public static GenericChart.GenericChart GetFileDelimitedPlotsForIsolationWidthStudy(this BulkResultCountComparisonFile file,
            ResultType resultType, bool isTopDown, string title)
        {
            List<GenericChart.GenericChart> charts = new List<GenericChart.GenericChart>();

            foreach (var result in file
                         .OrderBy(p => p.FileName.ConvertFileName())
                         .GroupBy(p => p.FileName.ConvertFileName().Split('_')[0]))
            {
                var temp = result.Select(p => p.FileName.ConvertFileName()).ToArray();
                var indChart = Chart.Column<int, string, string>(
                    result.Select(GenericPlots.ResultSelector(resultType)),
                    new Optional<IEnumerable<string>>(result.Select(p => p.FileName.ConvertFileName()), true), // Fix the error here
                    MarkerColor: result.First().FileName.ConvertFileName().ConvertConditionToColor()
                );
                charts.Add(indChart);
            }

            var chart = Chart.Combine(charts)
                .WithTitle($"{title} 1% FDR {Labels.GetLabel(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Isolation Width"))
                .WithYAxisStyle(Title.init("Count"))
                .WithLayout(PlotlyBase.DefaultLayoutNoLegend)
                .WithSize(600, PlotlyBase.DefaultHeight);
            return chart;
        }
    }
}
