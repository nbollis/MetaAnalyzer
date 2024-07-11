using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.SearchType;
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
            var mmRun = new MetaMorpheusResult(path);
            var parameters = new SingleRunAnalysisParameters(path, false, true, mmRun);
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
                var parameters = new CellLineAnalysisParameters(cellLine.DirectoryPath, true, true, cellLine);
                var task = new CellLineRetentionTimeCalibrationTask(parameters);
                task.Run();
            }
        }



     

        [Test]
        public static void RunSpectrumSummaryTask()
        {
            var path = TopDownJurkatFDRRunPath;
            var mmRun = new MetaMorpheusResult(path);
            var parameters = new SingleRunAnalysisParameters(path, false, true, mmRun);
            var task = new SingleRunChimericSpectrumSummaryTask(parameters);
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
