using Analyzer.Plotting;
using Analyzer.ResultType;

namespace Test
{
    internal class BottomUpRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis";
        internal static bool RunOnAll = true;
        internal static bool Override = false;

        internal static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
                   .Where(p => !p.Contains("Figures") && RunOnAll || p.Contains("Hela"))
                   .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

        [Test]
        public static void RunAllParsing()
        {
            foreach (CellLineResults cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    if (result is not MetaMorpheusResult)
                        continue;
                    result.Override = true;
                    result.IndividualFileComparison();
                    result.GetBulkResultCountComparisonFile();
                    result.CountChimericPsms();
                    if (result is MetaMorpheusResult mm)
                    {
                        mm.CountChimericPeptides();
                        mm.GetChimeraBreakdownFile();
                    }
                    result.Override = false;
                }

                cellLine.Override = true;
                cellLine.IndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.GetChimeraBreakdownFile();
                cellLine.Override = false;
            }

            AllResults.Override = true;
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.IndividualFileComparison();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();
            AllResults.GetChimeraBreakdownFile();
            AllResults.Override = false;
        }


        [Test]
        public static void PlotAllFigures()
        {
            foreach (CellLineResults cellLine in AllResults)
            {
                cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineRetentionTimePredictions();
                //cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            AllResults.PlotInternalMMComparison();
            AllResults.PlotBulkResultComparison();
            AllResults.PlotStackedIndividualFileComparison();
            AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotStackedSpectralSimilarity();
            //AllResults.PlotAggregatedSpectralSimilarity();
            AllResults.PlotBulkResultChimeraBreakDown();
            AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
        }

        [Test]
        public static void RunBulkParsing()
        {
            var bottomUpResults = AllResults;
            var topDownResults = TopDownRunner.AllResults;

            foreach (var cellLine in topDownResults)
            {
                foreach (var result in cellLine)
                {
                    if (result is MetaMorpheusResult { Condition: "MetaMorpheus" } mm)
                    {
                        //mm.Override = true;
                        //mm.GetChimeraBreakdownFile();
                    }
                    else if (result is MsPathFinderTResults mspt)
                    {
                        //mspt.CountChimericPsms();
                        //mspt.IndividualFileComparison();
                        //mspt.GetBulkResultCountComparisonFile();
                    }
                    else if (result is ProsightPDResult pspd)
                    {
                        pspd.CountChimericPsms();
                        pspd.IndividualFileComparison();
                        pspd.Override = true;
                        pspd.GetBulkResultCountComparisonFile();
                    }

                    result.Override = false;
                }

                cellLine.PlotIndividualFileResults();
            }

            topDownResults.PlotStackedIndividualFileComparison();
            topDownResults.PlotBulkResultComparison();

            foreach (var cellLine in bottomUpResults)
            {
                foreach (var result in cellLine)
                {
                    result.Override = true;
                    if (result is MetaMorpheusResult { Condition: "MetaMorpheusWithLibrary" } mm)
                    {
                        mm.GetChimeraBreakdownFile();
                    }
                    result.Override = false;

                }

                cellLine.Override = true;
                cellLine.GetChimeraBreakdownFile();
                cellLine.Override = false;

                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            bottomUpResults.Override = true;
            bottomUpResults.GetChimeraBreakdownFile();
            bottomUpResults.Override = false;
            bottomUpResults.PlotBulkResultChimeraBreakDown();
            bottomUpResults.PlotBulkResultChimeraBreakDown_TargetDecoy();

        }

    }
}
