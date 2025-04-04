﻿using GradientDevelopment;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotting.GradientDevelopment;
using Readers;
using ResultAnalyzerUtil;
using static Plotly.NET.StyleParam.DrawingStyle;
using Transcriptomics;
using SpectrumMatchTsvReader = GradientDevelopment.Temporary.SpectrumMatchTsvReader;
using System.Diagnostics;
using Analyzer.SearchType;

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
        public static void GetTheInformationYouWant()
        {
            var batch = StoredInformation.ExperimentalBatches[ExperimentalGroup.DifferentialMethylFlucRound3];

            var cys = batch.CytosineInformationFile;
            var fdr = batch.CytosineInformationByFdrFile;
            var info = batch.ExtractedInformationFile;
            batch.SavePlotHists();


            //foreach (var groupIdentifier in Enum.GetValues<ExperimentalGroup>().Where(p => p.ToString().Contains("Round3_")))
            //{
            //    var batch2 = StoredInformation.ExperimentalBatches[groupIdentifier];
            //    var cys2 = batch2.CytosineInformationFile;
            //    var fdr2 = batch2.CytosineInformationByFdrFile;
            //    var info2 = batch2.ExtractedInformationFile;
            //    batch2.SavePlotHists();
            //}
        }


        [Test]
        public static void GetTheInformationYouWant2()
        {

            var results = new List<CytosineInformation>();
            foreach (var groupIdentifier in Enum.GetValues<ExperimentalGroup>().Where(p => p.ToString().Contains("Round3_")))
            {
                var batch2 = StoredInformation.ExperimentalBatches[groupIdentifier];
                results.AddRange(batch2.CytosineInformationByFdrFile);
            }

            var file = new CytosineInformationFile(@"B:\Users\Nic\RNA\FLuc\250317_FlucDifferentialMethylations\ProcessedResults\Round3SearchedAlone_CytosineMethylDataByFdr.csv", results);
            file.WriteResults(file.FilePath);
        
        }













        [Test]
        public static void CollectRunData()
        {
            var outPath = GradientDevelopmentParsedInfo;

            var results = new List<ExtractedInformation>();
            foreach (var runInformation in StoredInformation.RunInformationList)
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
            //PlotOne();
        }

        [Test]
        public static void PlotOne()
        {
            string path = @"D:\Projects\Chimeras\UniqueIonsRequired\FirstTest";
            var mmResult = new MetaMorpheusResult(path, "Jurkat", "DiffCutoffs");

            mmResult.GetIndividualFileComparison();


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
