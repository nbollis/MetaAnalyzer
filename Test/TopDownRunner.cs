using ResultAnalyzer.Plotting;
using ResultAnalyzer.ResultType;

namespace Test
{
    internal class TopDownRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis";
        internal static bool RunOnAll = false;
        internal static bool Override = false;
        private static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
            .Where(p => !p.Contains("Figures") && RunOnAll || p.Contains("Jurkat"))
            .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    if (result is MsPathFinderTResults)
                        result.Override = true;
                    result.IndividualFileComparison();
                    result.GetBulkResultCountComparisonFile();
                    result.CountChimericPsms();
                    if (result is MetaMorpheusResult mm)
                        mm.CountChimericPeptides();
                    result.Override = false;
                }

                cellLine.Override = true;
                cellLine.IndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;
            }

            AllResults.Override = true;
            AllResults.IndividualFileComparison();
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();
        }
    
        [Test]
        public static void PlotAllFigures()
        {
            foreach (var cellLine in AllResults)
            {
                //cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            AllResults.PlotBulkResultChimeraBreakDown();
            AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            AllResults.PlotInternalMMComparison();
            AllResults.PlotBulkResultComparison();
            AllResults.PlotStackedIndividualFileComparison();
        }
    }
}
