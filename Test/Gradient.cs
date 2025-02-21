using GradientDevelopment;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotting.GradientDevelopment;
using Readers;
using ResultAnalyzerUtil;
using static Plotly.NET.StyleParam.DrawingStyle;
using Transcriptomics;
using SpectrumMatchTsvReader = GradientDevelopment.Temporary.SpectrumMatchTsvReader;
using System.Diagnostics;

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

        public static string GradientDevelopmentParsedInfo => Path.Combine(GradientDevelopmentDirectory, $"{FileIdentifiers.ExtractedGradientInformation}.tsv");


        [Test]
        public static void NewStruct()
        {
            var experiments = StoredInformation.ExperimentalBatches[ExperimentalGroup.DifferentialMethylFluc];
            var batch = new ExperimentalBatch("Differential Methyl Fluc", experiments.First().ParentDirectory, experiments);

            var info = batch.ExtractedInformationFile;


        }













        [Test]
        public static void CollectRunData()
        {
            var outPath = GradientDevelopmentParsedInfo;

            var results = new List<ExtractedInformation>();
            foreach (var runInformation in StoredInformation.ExperimentalBatches.SelectMany(p => p.Value))
            {
                var info = runInformation.GetExtractedRunInformation();
                if (info.FivePercentIds.Any())
                    results.Add(info);
            }
            //results.Add(StoredInformation.RunInformationList[0].GetExtractedRunInformation());

            ExtractedInformationFile resultFile;
            if (File.Exists(outPath))
            {
                Debugger.Break(); // CAREFUL: you may duplicate data doing it this way. 
                resultFile = new ExtractedInformationFile(outPath);
                resultFile.Results.AddRange(results);
            }
            else
            {
                resultFile = new ExtractedInformationFile(outPath) { Results = results };
            }

            resultFile.WriteResults(outPath);
            PlotOne();
        }

        [Test]
        public static void PlotOne()
        {
            var outPath = GradientDevelopmentParsedInfo;
            var results = new ExtractedInformationFile(outPath).Results;
            //var topFdPath = @"B:\Users\Nic\RNA\FLuc\GradientDevelopment\TopFD";
            var topFdPath = @"B:\Users\Nic\RNA\FLuc\250220_FlucDifferentialMethylations\TopFD";
            //results.UpdateTimesToDisplay();

            foreach (var run in results.TakeLast(3))
            {
                var path = Path.Combine(GradientDevelopmentFigureDirectory,
                    $"{FileIdentifiers.GradientFigure}_{run.DataFileName}_{run.GradientName}");
                var plot2 = run.GetPlotHist(topFdPath, 30);
                if (plot2 is null)
                    continue;

                //plot2.Show();
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
