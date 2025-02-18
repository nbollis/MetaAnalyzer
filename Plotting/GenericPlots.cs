using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET;
using Plotly.NET.TraceObjects;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Omics.SpectrumMatch;
using Plotting.Util;
using Microsoft.FSharp.Core;
using ResultAnalyzerUtil;

namespace Plotting
{
    public static class GenericPlots
    {
        public static GenericChart.GenericChart KernelDensityPlot(List<double> values, string title,
            string xTitle = "", string yTitle = "", double bandwidth = 0.2, Kernels kernel = Kernels.Gaussian, Color? color = null)
        {
            List<(double, double)> data = new List<(double, double)>();

            foreach (var sample in values.DistinctBy(p => p.Round(3)).OrderBy(p => p))
            {
                var pdf = kernel switch
                {
                    Kernels.Gaussian => KernelDensity.EstimateGaussian(sample, bandwidth, values),
                    Kernels.Epanechnikov => KernelDensity.EstimateEpanechnikov(sample, bandwidth, values),
                    Kernels.Triangular => KernelDensity.EstimateTriangular(sample, bandwidth, values),
                    Kernels.Uniform => KernelDensity.EstimateUniform(sample, bandwidth, values),
                    _ => throw new ArgumentOutOfRangeException(nameof(kernel), kernel, null)
                };
                data.Add((sample, pdf));
            }

            color ??= title.ConvertConditionToColor();
            var chart =
                Chart.Line<double, double, string>(data.Select(p => p.Item1), data.Select(p => p.Item2), Name: title,
                        LineColor: color)
                    .WithSize(400, 400)
                    .WithTitle(title, TitleFont: Font.init(Size: PlotlyBase.TitleSize))
                    .WithXAxisStyle(Title.init(xTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize))/*, new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(-15, 15))*/)
                    .WithYAxisStyle(Title.init(yTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            return chart;
        }

        public static GenericChart.GenericChart Histogram(List<double> values, string title, string xTitle = "",
            string yTitle = "", bool normalize = false, (double, double)? minMax = null)
        {
            var chart = Chart.Histogram<double, double, string>(values,  Name: title, MarkerColor: title.ConvertConditionToColor(),
                                   HistNorm: normalize ? StyleParam.HistNorm.Percent : StyleParam.HistNorm.None)
                .WithSize(400, 400)
                .WithTitle(title)
                .WithYAxisStyle(Title.init(yTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            if (minMax is not null)
                return chart.WithXAxisStyle(Title.init(xTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)),
                    new FSharpOption<Tuple<IConvertible, IConvertible>>(
                        new Tuple<IConvertible, IConvertible>(minMax.Value.Item1, minMax.Value.Item2)));
            else
                return chart.WithXAxisStyle(Title.init(xTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)));
        }

        public static GenericChart.GenericChart ViolinPlot(List<double> values, string label)
        {
            var labels = Enumerable.Repeat(label, values.Count).ToArray();
            var violin = Chart.Violin<string, double, string> (labels, values,
                    label, MarkerColor: label.ConvertConditionToColor(), 
                        MeanLine: MeanLine.init(true, label.ConvertConditionToColor()), ShowLegend: false)
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600);
            return violin;
        }

        public static GenericChart.GenericChart BoxPlot(List<double> values, string title, string xTitle = "",
            string yTitle = "", bool showOutliers = true)
        {
            var chart = Chart.BoxPlot<string, double, string>(X: Enumerable.Repeat(title, values.Count()).ToArray(),
                    Y: values, Name: title, MarkerColor: title.ConvertConditionToColor(), Jitter: 0.1,
                                   BoxPoints: showOutliers ? StyleParam.BoxPoints.Outliers : StyleParam.BoxPoints.False
                                  /* Orientation: StyleParam.Orientation.Vertical*/)
                .WithSize(400, 400)
                .WithTitle(title, TitleFont: Font.init(Size: PlotlyBase.TitleSize))
                .WithXAxisStyle(Title.init(xTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxisStyle(Title.init(yTitle, Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            return chart;
        }

        public static GenericChart.GenericChart Histogram2D<T>(List<T> xValues, List<double> yValues, string title,
            string xTitle = "", string yTitle = "", bool normalizeColumns = false) where T : IConvertible
        {
            var zValues = default(Plotly.NET.CSharp.Optional<IEnumerable<IEnumerable<double>>>);
            if (normalizeColumns)
            {
                if (xValues.Count != yValues.Count)
                    goto NoNorm;

                // combine values and keys
                var combined = new (T, double)[xValues.Count];
                for (int i = 0; i < xValues.Count; i++)
                    combined[i] = (xValues[i], yValues[i]);

                // group by keys and adjust values to be a percentage of total in group
                zValues = new Plotly.NET.CSharp.Optional<IEnumerable<IEnumerable<double>>>(combined.GroupBy(p => p.Item1)
                    .Select(group =>
                        group.Select(p => Math.Sign(p.Item2) * (Math.Abs(p.Item2) / group.Max(m => Math.Abs(m.Item2))))), true);

            }


            NoNorm:
            var chart = Chart.Histogram2DContour<T, double, double>(xValues, yValues, Z: zValues, YBins: Bins.init(null, null, 0.1)
                    /*HistNorm: StyleParam.HistNorm.Percent*//*, HistFunc: StyleParam.HistFunc.Avg*/)
                //var chart = Chart.BoxPlot<T, double, string>(xValues, yValues, Name: title, MarkerColor: title.ConvertConditionToColor()
                //BoxWidth: 4, MeanLine: MeanLine.init(true, title.ConvertConditionToColor()), Points: StyleParam.BoxPoints.False)
                .WithSize(400, 400)
                .WithTitle(title)
                .WithXAxisStyle(Title.init(xTitle))
                .WithYAxisStyle(Title.init(yTitle))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend)
                .WithSize(800, 800);
            return chart;
        }

        public static GenericChart.GenericChart ModificationDistribution(List<string> fullSequences, string title,
            string xTitle = "", string yTitle = "", bool displayCarbamimidoMethyl = false, bool displayRelative = true)
        {
            var modDict = new Dictionary<string, double>();
            foreach (var mod in fullSequences.SelectMany(p =>
                         Omics.SpectrumMatch.SpectrumMatchFromTsv.ParseModifications(p).SelectMany(m => m.Value)
                             .Select(mod => System.Text.RegularExpressions.Regex.Replace(mod, @".*?:", "").Trim()
                                 .Replace("Accetyl", "Acetyl"))))
            {
                if (!displayCarbamimidoMethyl && mod.StartsWith("Carbamidometh"))
                    continue;

                if (!modDict.TryAdd(mod, 1))
                {
                    modDict[mod]++;
                }
            }

            if (displayRelative)
            {
                var modCount = modDict.Sum(p => p.Value);
                foreach (var keyValuePair in modDict)
                {
                    modDict[keyValuePair.Key] = keyValuePair.Value / modCount * 100.0;
                }
            }


            if (false)
            {
                foreach (var keyValuePair in modDict.Where(p => p.Key.Contains("Deamida") || p.Key.Contains("Hydrox")))
                {
                    modDict.Remove(keyValuePair.Key);
                }
            }

            // remove anything where the mod is less than 1 % of total modifications
            modDict = modDict.Where(p => p.Value > 1)
                .ToDictionary(p => p.Key, p => p.Value);

            var chart = Chart.Column<double, string, string>(modDict.Values, modDict.Keys, title, MarkerColor: title.ConvertConditionToColor())
                .WithSize(900, 600)
                .WithTitle(title)
                .WithXAxisStyle(Title.init(xTitle))
                .WithYAxisStyle(Title.init(yTitle))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegend);
            return chart;
        }
    }
}
