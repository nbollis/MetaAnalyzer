using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Calibrator;
using Microsoft.ML.Calibrators;
using RetentionTimePrediction;
using TaskLayer.ChimeraAnalysis;

namespace Test
{
    internal class MiscTaskRunner
    {
        public static string Man11FDRRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_Mann11_Ind";

        public static string Man11AllResultsPath => BottomUpRunner.DirectoryPath;
        public static string TopDownDirectoryPath => TopDownRunner.DirectoryPath;

        public static string TopDownJurkatFDRRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_JurkatTD";

        [Test]
        public static void SpectrumSimilarityTaskRunner()
        {
            string path = Man11FDRRunPath;
            var parameters =
                new SingleRunAnalysisParameters(path, false, true, new MetaMorpheusResult(path));
            var task = new SingleRunSpectralAngleComparisonTask(parameters);
            task.Run();
        }

        [Test]
        public static void ChimericSpectrumSummaryTask()
        {
            string path = Man11FDRRunPath;
            //string path = TopDownJurkatFDRRunPath;
            var mmRun = new MetaMorpheusResult(path);
            var parameters = new SingleRunAnalysisParameters(path, false, false, mmRun);
            var task = new SingleRunChimericSpectrumSummaryTask(parameters);

            task.Run();
        }

        [Test]
        public static void RunRetentionTimeAdjustmentTask()
        {
            var allResults = new AllResults(Man11AllResultsPath);
            // figures found at B:\Users\Nic\Chimeras\Mann_11cell_analysis\A549\Figures
            foreach (var cellLine in allResults)
            {
                foreach (var singleRunResult in cellLine)
                {
                    if (singleRunResult is not IRetentionTimePredictionAnalysis)
                        continue;
                    if (!cellLine.GetSingleResultSelector().Contains(singleRunResult.Condition))
                        continue;

                    var parameters = new SingleRunAnalysisParameters(singleRunResult.DirectoryPath,
                        false, true, singleRunResult);
                    var task = new SingleRunRetentionTimeCalibrationTask(parameters);
                    task.Run();
                }
            }
        }

        [Test]
        public static void RunJenkinsLikeParserOnSingleRun()
        {
            string path = @"B:\Users\Nic\PEPTesting\240715_NB_NewPep_FracInt+Term+Intern_2377";

            var name = Path.GetFileName(path);
            var runDirectories = path.GetDirectories();

            var allMMResults = new List<SingleRunResults>();


            var semiSpecificDir = runDirectories.FirstOrDefault(p => p.Contains("Semispecific"));
            if (semiSpecificDir is not null)
                allMMResults.Add(new MetaMorpheusResult(semiSpecificDir, name, "Semi-Specific"));

            var nonspecificDir = runDirectories.FirstOrDefault(p => p.Contains("Nonspecific"));
            if (nonspecificDir is not null)
                allMMResults.Add(new MetaMorpheusResult(nonspecificDir, name, "Non-Specific"));

            var modernDir = runDirectories.FirstOrDefault(p => p.Contains("Modern") && !p.Contains("Open"));
            if (modernDir is not null)
                allMMResults.Add(new MetaMorpheusResult(modernDir, name, "Modern"));


            var classicDir = runDirectories.FirstOrDefault(p => p.Contains("Classic"));
            if (classicDir is not null)
            {
                var classicInitialDir = classicDir.GetDirectories().First(p => p.Contains("Task1"));
                allMMResults.Add(new MetaMorpheusResult(classicInitialDir, name, "Classic - Initial"));
                var classicPostCalibDir = classicDir.GetDirectories().First(p => p.Contains("Task3"));
                allMMResults.Add(new MetaMorpheusResult(classicPostCalibDir, name, "Classic - Post Calibration"));
                var classicPostGptmdDir = classicDir.GetDirectories().First(p => p.Contains("Task5"));
                allMMResults.Add(new MetaMorpheusResult(classicPostGptmdDir, name, "Classic - Post GPTMD"));
            }



            var topDownDir = runDirectories.FirstOrDefault(p => p.Contains("TopDown"));
            if (topDownDir is not null)
            {
                var tdInitialDir = topDownDir.GetDirectories().First(p => p.Contains("Task1"));
                allMMResults.Add(new MetaMorpheusResult(tdInitialDir, name, "TopDown - Initial"));
                var tdPostCalibDir = topDownDir.GetDirectories().First(p => p.Contains("Task3"));
                allMMResults.Add(new MetaMorpheusResult(tdPostCalibDir, name, "TopDown - Post Calibration"));
                var tdPostAveragingDir = topDownDir.GetDirectories().First(p => p.Contains("Task5"));
                allMMResults.Add(new MetaMorpheusResult(tdPostAveragingDir, name, "TopDown - Post Averaging"));
                var tdPostGPTMDDir = topDownDir.GetDirectories().First(p => p.Contains("Task7"));
                allMMResults.Add(new MetaMorpheusResult(tdPostGPTMDDir, name, "TopDown - Post GPTMD"));
            }

            var bottomupOpenModernDir = runDirectories.FirstOrDefault(p => p.Contains("BottomUpOpenModern"));
            if (bottomupOpenModernDir is not null)
                allMMResults.Add(new MetaMorpheusResult(bottomupOpenModernDir, name, "BottomUp OpenModern"));


            var topDownOpenModernDir = runDirectories.FirstOrDefault(p => p.Contains("TopDownOpenModern"));
            if (topDownOpenModernDir is not null)
                allMMResults.Add(new MetaMorpheusResult(topDownOpenModernDir, name, "TopDown OpenModern"));


            var run = new CellLineResults(path, allMMResults);

            foreach (var result in run)
            {
                var mm = (MetaMorpheusResult)result;
                mm.PlotPepFeaturesScatterGrid();
            }
        }


        [Test]
        public static void RunInternalMMComparison()
        {
            var path = BottomUpRunner.DirectoryPath;
            var dataDirectoryPath = InternalMetaMorpheusAnalysisTask.Mann11DataFileDirectory;
            var outputDir = InternalMetaMorpheusAnalysisTask.Mann11OutputDirectory;
            var dbPath = InternalMetaMorpheusAnalysisTask.UniprotHumanProteomeAndReviewedXml;

            var parameters = new InternalMetaMorpheusAnalysisParameters(path, outputDir, dataDirectoryPath, dbPath, @"C:\Program Files\MetaMorpheus");
            var task = new InternalMetaMorpheusAnalysisTask(parameters);
            task.Run();
        }

        #region On the fly tests

        [Test]
        public static void RunChronologer()
        {
            var allResults = new AllResults(Man11AllResultsPath);
            // figures found at B:\Users\Nic\Chimeras\Mann_11cell_analysis\A549\Figures
            foreach (var cellLine in allResults)
            {
                var mm = (MetaMorpheusResult)cellLine.First(p => p is MetaMorpheusResult);
                var peptides = mm.AllPeptides;
                var results = peptides
                    .Where(p => p is {PEP_QValue: <= 0.01, DecoyContamTarget: "T"} 
                                && !p.FullSequence.Contains("Metal")
                                && !p.BaseSeq.Contains('U'))
                    .Select(p => (p.FullSequence, p.RetentionTime, ChronologerEstimator.PredictRetentionTime(p.BaseSeq, p.FullSequence))).ToArray();

                var notDone = results.Where(p => p.Item3 is null).ToArray();
            }
        }

        #endregion
    }
}
