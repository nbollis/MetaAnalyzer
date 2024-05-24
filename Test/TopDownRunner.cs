using System.Security.Cryptography;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Plotly.NET.ImageExport;
using Readers;
using Analyzer.Plotting;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Proteomics.PSM;
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
            foreach (var cellLine in AllResults.Skip(1))
            {
                foreach (var result in cellLine)
                {
                    //if (result is MsPathFinderTResults)
                    //    result.Override = true;
                    result.CountChimericPsms();
                    result.GetBulkResultCountComparisonFile();
                    result.GetIndividualFileComparison();
                    if (result is IChimeraBreakdownCompatible cb)
                    {
                        if (result is ProteomeDiscovererResult /*or MsPathFinderTResults*/ && result.Condition.Contains("15"))
                            result.Override = true;
                        cb.GetChimeraBreakdownFile();
                        result.Override = false;
                    }
                    if (result is IChimeraPeptideCounter pc)
                        pc.CountChimericPeptides();
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
            AllResults.PlotBulkResultChimeraBreakDown();
            AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
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

            AllResults.PlotBulkResultComparisons();
            //foreach (CellLineResults cellLine in AllResults.Skip(1))
            //{
            //    //cellLine.PlotIndividualFileResults();
            //    //cellLine.PlotCellLineSpectralSimilarity();
            //    //cellLine.PlotCellLineChimeraBreakdown();
            //    //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            //    foreach (var individualResult in cellLine)
            //    {
            //        if (individualResult is not MetaMorpheusResult {Condition: "MetaMorpheus_NewPEP_NoNormNoMult"} mm ) continue;
            //        mm.ExportPepFeaturesPlots();
            //        mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
            //        mm.PlotTargetDecoyCurves();
            //    }
            //}

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
        public static void ugh()
        {
            string path =
                @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MetaMorpheus\Task4-SearchTask\AllPSMs.psmtsv";
            var psms = SpectrumMatchTsvReader.ReadPsmTsv(path, out _);
            var count = psms.Count;
            var distinctCount = psms.DistinctBy(p => p,
                CustomComparer<PsmFromTsv>.MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer).Count();
            var filteredCount = psms.Count(p => p.PEP_QValue <= 0.01);
            var distinctFilteredCount = psms.Where(p => p.PEP_QValue <= 0.01).DistinctBy(p => p,
                CustomComparer<PsmFromTsv>.MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer).Count();

            var grouped = psms.GroupBy(p => p,
                    CustomComparer<PsmFromTsv>.MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer)
                .OrderByDescending(p => p.Count());

            string path2 =
                @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MetaMorpheus_Rep2_WithLibrary_NewPEP_NoNorm\Task4-SearchTask\AllPSMs.psmtsv";
            var psms2 = SpectrumMatchTsvReader.ReadPsmTsv(path2, out _);
            var count2 = psms2.Count;
            var distinctCount2 = psms2.DistinctBy(p => p,
                CustomComparer<PsmFromTsv>.MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer).Count();
            var filteredCount2 = psms2.Count(p => p.PEP_QValue <= 0.01);
            var distinctFilteredCount2 = psms2.Where(p => p.PEP_QValue <= 0.01).DistinctBy(p => p,
                CustomComparer<PsmFromTsv>.MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer).Count();

            var grouped2 = psms2.GroupBy(p => p,
                    CustomComparer<PsmFromTsv>.MetaMorpheusDuplicatedPsmFromDifferentPrecursorPeaksComparer)
                .OrderByDescending(p => p.Count());



        }




    }
}
