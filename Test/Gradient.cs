using GradientDevelopment;
using Plotly.NET.ImageExport;
using Plotting.GradientDevelopment;
using ResultAnalyzerUtil;

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
                var plot2 = run.GetPlotHist();

                //GenericChartExtensions.Show(plot2);
                plot2.SavePNG(path, null, 1200, 700);
            } 
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
