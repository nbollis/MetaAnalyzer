using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.SearchType;
using TaskLayer.ChimeraAnalysis;

namespace Test
{
    internal class MiscTaskRunner
    {
        public static string Man11FDRRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_Mann11_Ind";

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
    }
}
