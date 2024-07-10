using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.SearchType;
using Calibrator;
using Microsoft.ML.Calibrators;
using TaskLayer.ChimeraAnalysis;

namespace Test
{
    internal class MiscTaskRunner
    {
        public static string Man11FDRRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_Mann11_Ind";

        public static string Man11AllResultsPath => BottomUpRunner.DirectoryPath;
        public static string TopDownDirectoryPath => TopDownRunner.DirectoryPath;

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
            foreach (var cellLine in allResults)
            {
                var parameters = new CellLineAnalysisParameters(cellLine.DirectoryPath, false, true, cellLine);
                var task = new CellLineRetentionTimeCalibrationTask(parameters);
                task.Run();
            }
        }
    }
}
