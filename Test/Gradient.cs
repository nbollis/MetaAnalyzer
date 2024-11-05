using Analyzer.Plotting.Util;
using Analyzer.Util;
using GradientDevelopment;
using Microsoft.FSharp.Core;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.GenericChartExtensions;

namespace Test
{

    internal class Gradient
    {

        static string GradientDevelopmentDirectory = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment";
        static string GradientDevelopmentFigureDirectory = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment\Figures";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (!Directory.Exists(GradientDevelopmentFigureDirectory))
                Directory.CreateDirectory(GradientDevelopmentFigureDirectory);
        }

        [Test]
        public static void FuckThatRunThat()
        {
            var outPath = Path.Combine(GradientDevelopmentDirectory, $"{FileIdentifiers.ExtractedGradientInformation}.tsv");

            var results = new List<ExtractedInformation>();
            foreach (var runInformation in StoredInformation.RunInformationList)
            {
                var info = runInformation.GetExtractedRunInformation();
                if (info.FivePercentIds.Any())
                    results.Add(info);
            }
            //results.Add(StoredInformation.RunInformationList[0].GetExtractedRunInformation());

            var resultFile = new ExtractedInformationFile(outPath) { Results = results };
            resultFile.WriteResults(outPath);
            PlotOne();
        }

        [Test]
        public static void PlotOne()
        {
            var outPath = Path.Combine(GradientDevelopmentDirectory, $"{FileIdentifiers.ExtractedGradientInformation}.tsv");

            foreach (var run in new ExtractedInformationFile(outPath).Results)
            {
                var path = Path.Combine(GradientDevelopmentFigureDirectory,
                    $"{FileIdentifiers.GradientFigure}_{run.DataFileName}_{run.GradientName}");
                var plot2 = GetPlotHist(run);

                //GenericChartExtensions.Show(plot2);
                plot2.SavePNG(path, null, 1200, 700);
            } 
        }

        public static GenericChart.GenericChart GetPlotHist(ExtractedInformation run)
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

            var gradient = Chart.Line<double, double, string>(
                    run.Gradient.Select(p => p.Item1).ToArray(),
                    run.Gradient.Select(p => p.Item2).ToArray(),
                    MarkerColor: run.MobilePhaseB.ConvertConditionToColor(),
                    LineWidth:2,
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
                    MinMax: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0,100)),
                    Id: StyleParam.SubPlotId.NewYAxis(4), Overlaying: StyleParam.LinearAxisId.NewY(1))
                .WithXAxisStyle(Title.init("Retention Time"), 
                    Domain: new FSharpOption<Tuple<IConvertible, IConvertible>>(new Tuple<IConvertible, IConvertible>(0.15, 1)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithSize(1200, 700)
                .WithTitle($"{run.MobilePhaseB} - {run.DataFileName}");

            return toReturn;
        }
    }
















    class FuckThat
    {
        public static bool SheLetMeHitIt;

        static FunFact RunThat()
        {
            return Buzzfeed.HitemWithIt();
        }

        static Cum? HitIt()
        {
            if (SheLetMeHitIt)
                return Cum.Fast;
            else
                return Cum.No;
        }
    }


    class Buzzfeed
    {
        internal static FunFact HitemWithIt() => new FunFact();
    }

    class FunFact {}


    enum Cum
    {
        Fast,
        No,
    }
}
