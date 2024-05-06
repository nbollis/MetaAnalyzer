using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;

namespace Test
{
    internal class PEPSplom
    {
        internal static string TestPath = @"B:\Users\Nic\Chimeras\PEPTesting\AllPSMs_FormattedForPercolator.tab";

        [Test]
        public static void TestSplom()
        {
            var pepAnalysis = new PepAnalysisForPercolatorFile(TestPath);
            pepAnalysis.LoadResults();
            var allResults = pepAnalysis.Results;

            var pepEvaluationPlot = new PepEvaluationPlot(allResults);
            pepEvaluationPlot.GenerateChart();
        }
    }
}
