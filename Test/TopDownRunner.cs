using System.Security.Cryptography;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Plotly.NET.ImageExport;
using Readers;
using Analyzer.Plotting;
using Analyzer.SearchType;
using Analyzer.Util;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal.Commands;
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
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath);

        [OneTimeSetUp]
        public static void OneTimeSetup() { Loaders.LoadElements(); }

      

        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    //result.Override = true;
                    result.CountChimericPsms();
                    result.GetBulkResultCountComparisonFile();
                    result.GetIndividualFileComparison();
                    if (result is IChimeraBreakdownCompatible cb)
                        cb.GetChimeraBreakdownFile();
                    if (result is IChimeraPeptideCounter pc)
                        pc.CountChimericPeptides();
                    if (result is MetaMorpheusResult mm)
                    {
                        //mm.ExportPepFeaturesPlots();
                        //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                    }
                    result.Override = false;
                }

                //cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;

                cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();

                cellLine.Dispose();
            }

            //AllResults.Override = true;
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
            foreach (CellLineResults cellLine in AllResults.SkipLast(1))
            {
                foreach (var individualResult in cellLine)
                {
                    if (individualResult is not MetaMorpheusResult mm) continue;
                    //mm.ExportPepFeaturesPlots();
                    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                }

                cellLine.PlotIndividualFileResults();
                cellLine.PlotCellLineSpectralSimilarity();
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



        /// <summary>
        /// Overnight I will:
        /// Rerun all individual file results for MM due to the unique by base sequence issue
        /// Replot all individual file results
        ///
        /// Run get maximum chimera estimation file
        /// rerun every plot with new selectors
        /// </summary>
        [Test]
        public static void OvernightRunner()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine.Where(p => true.ChimeraBreakdownSelector(cellLine.CellLine).Contains(p.Condition)))
                {
                    (result as MetaMorpheusResult)?.GetChimeraBreakdownFile();
                }
                // These require the masses and charges
                cellLine.PlotChimeraBreakdownByMassAndCharge();
                cellLine.Dispose();
            }

            foreach (var cellLine in BottomUpRunner.AllResults)
            {
                cellLine.PlotChronologerDeltaPlotBoxAndWhisker();
                cellLine.PlotChronologerDeltaRangePlot();

                foreach (var result in cellLine.Where(p => false.ChimeraBreakdownSelector(cellLine.CellLine).Contains(p.Condition)))
                {
                    (result as MetaMorpheusResult)?.GetChimeraBreakdownFile();
                }
                // These require the masses and charges
                cellLine.PlotChimeraBreakdownByMassAndCharge();


                cellLine.GetMaximumChimeraEstimationFile();
                //cellLine.Override = true;
                //cellLine.GetMaximumChimeraEstimationFile(false);
                //cellLine.Override = false;
                //cellLine.PlotAverageRetentionTimeShiftPlotKernelPdf(false);
                //cellLine.PlotAverageRetentionTimeShiftPlotHistogram(false);
                //cellLine.PlotAllRetentionTimeShiftPlots(false);
                cellLine.Dispose();
            }
            BottomUpRunner.AllResults.PlotBulkChronologerDeltaPlotKernalPDF();
            BottomUpRunner.AllResults.PlotGridChronologerDeltaPlotKernalPDF();

            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                    if (result is IChimeraBreakdownCompatible cbc)
                        cbc?.GetChimeraBreakdownFile();
                cellLine.Dispose();
            }

            foreach (var cellLine in BottomUpRunner.AllResults)
            {
                foreach (var result in cellLine)
                    if (result is IChimeraBreakdownCompatible cbc)
                        cbc?.GetChimeraBreakdownFile();
                cellLine.Dispose();
            }
        }
    


    

        [Test]
        public static void IsabellaData()
        {
            string path = @"B:\Users\AlexanderS_Bison\240515_DataFromITW";
            var results = (from dirpath in Directory.GetDirectories(path)
                    where !dirpath.Contains("Fig")
                    where dirpath.Contains("Ecoli")
                    select new MetaMorpheusResult(dirpath))
                .Cast<BulkResult>()
                .ToList();


            var cellLine = new CellLineResults(path, results);
            cellLine.Override = true;
            cellLine.GetBulkResultCountComparisonFile();
            cellLine.GetIndividualFileComparison();
            cellLine.GetBulkResultCountComparisonMultipleFilteringTypesFile();
            cellLine.PlotIndividualFileResults(ResultType.Psm, null, false);
            cellLine.PlotIndividualFileResults(ResultType.Peptide, null, false);
            cellLine.PlotIndividualFileResults(ResultType.Protein, null, false);
        }


    }
}
