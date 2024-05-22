using Analyzer.FileTypes.Internal;
using Plotly.NET.ImageExport;
using Readers;
using Analyzer.Plotting;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using UsefulProteomicsDatabases;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test
{
    internal class TopDownRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis";
        internal static bool RunOnAll = true;
        internal static bool Override = false;
        private static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
            .Where(p => !p.Contains("Figures") && RunOnAll || p.Contains("Jurkat"))
            .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

        [OneTimeSetUp]
        public static void OneTimeSetup() { Loaders.LoadElements(); }


        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    result.GetIndividualFileComparison();
                    result.CountChimericPsms();
                    if (result is MsPathFinderTResults)
                        result.Override = true;
                    result.GetBulkResultCountComparisonFile();
                    if (result is MetaMorpheusResult mm)
                    {
                        // mm.GetChimeraBreakdownFile();
                        mm.CountChimericPeptides();
                    }
                    result.Override = false;
                }

                cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;

                cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            AllResults.Override = true;
            AllResults.IndividualFileComparison();
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();


            AllResults.PlotBulkResultComparisons();
            AllResults.PlotStackedIndividualFileComparison();
        }

        [Test]
        public static void GenerateAllFigures()
        {
            foreach (CellLineResults cellLine in AllResults)
            {
                foreach (var individualResult in cellLine)
                {
                    if (individualResult is not MetaMorpheusResult mm) continue;
                    //mm.ExportPepFeaturesPlots();
                    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                }

                //cellLine.PlotIndividualFileResults();
                //cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            AllResults.PlotInternalMMComparison();
            AllResults.PlotBulkResultComparisons();
            AllResults.PlotStackedIndividualFileComparison();
            AllResults.PlotBulkResultChimeraBreakDown();
            AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
        }

        [Test]
        public static void GenerateSpecificFigures()
        {
            foreach (CellLineResults cellLine in AllResults.Skip(1))
            {
                //cellLine.PlotIndividualFileResults();
                //cellLine.PlotCellLineSpectralSimilarity();
                //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                foreach (var individualResult in cellLine)
                {
                    if (individualResult is not MetaMorpheusResult {Condition: "MetaMorpheus_NewPEP_NoNormNoMult"} mm ) continue;
                    mm.ExportPepFeaturesPlots();
                    mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                    mm.PlotTargetDecoyCurves();
                }
            }

            //AllResults.PlotInternalMMComparison();
            //AllResults.PlotBulkResultComparisons();
            //AllResults.PlotStackedIndividualFileComparison();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
        }


        [Test]
        public static void MsPathTDatasetInfoGenerator()
        {
            foreach (MsPathFinderTResults mspt in AllResults.SelectMany(p => p.Results)
                .Where(p => p is MsPathFinderTResults mspt && mspt.IndividualFileResults.Count is 20 or 43 or 10))
            {
                mspt.CreateDatasetInfoFile();
            }
        }


        [Test]
        public static void GenerateTargetDecoyByChimeraGroupPlots()
        {

            Dictionary<string, string> conditionsAndOutHeader = new()
            {
                { "MetaMorpheus", "Original" },
                { "MetaMorpheus_ChimeraInPEP_Negative", "PEP Negative" },
                { "MetaMorpheus_ChimeraInPEP_Positive", "PEP Positive" },
                { "MetaMorpheus_DeconScoreAndPeaksInPEP", "Decon Score And Peaks" },
                { "MetaMorpheus_OneMissedMonoIsotopic", "OneMissedMono" },
                { "MetaMorpheus_FullPEPChimeraIncorporation", "FullPEPIncorporation" },
            };
            //var selectedCondition = conditionsAndOutHeader.First();
            //var condition = selectedCondition.Key;

            //var results = AllResults
            //    .First(p => p.CellLine == "Jurkat")
            //    .First(p => p.Condition == condition) as MetaMorpheusResult;

            //string outDir = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MetaMorpheus\Figures";
            //results.ExportCombinedChimeraTargetDecoyExploration(outDir, selectedCondition);




            var selectedCondition = conditionsAndOutHeader.Skip(5).First();
            var condition = selectedCondition.Key;
            var results = AllResults
                .First(p => p.CellLine == "Jurkat")
                .First(p => p.Condition == condition) as MetaMorpheusResult;

            var outDir = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MetaMorpheus\Figures";
            results.ExportCombinedChimeraTargetDecoyExploration(outDir, selectedCondition.Value);
        }


    
   
    }
}
