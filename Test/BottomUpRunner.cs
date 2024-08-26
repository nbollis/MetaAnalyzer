using System.Diagnostics;
using System.Text;
using Analyzer;
using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Calibrator;
using CMD;
using Readers;
using TaskLayer.ChimeraAnalysis;
using UsefulProteomicsDatabases;

namespace Test
{
    internal class BottomUpRunner
    {
        internal static string DirectoryPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis";
        internal static bool RunOnAll = true;
        internal static bool Override = false;

        internal static AllResults? _allResults;
        internal static AllResults AllResults => _allResults ??= new AllResults(DirectoryPath, Directory.GetDirectories(DirectoryPath)
                   .Where(p => !p.Contains("Figures") && !p.Contains("Prosight") && RunOnAll || p.Contains("Hela"))
                   .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

        [OneTimeSetUp]
        public static void OneTimeSetup() { Loaders.LoadElements(); }


        [Test]
        public static void RunAllParsing()
        {
            // Got to K562
            foreach (CellLineResults cellLine in AllResults.Skip(9))
            {
                foreach (var result in cellLine)
                {
                    //if (result is MetaMorpheusResult)
                    //    continue;
                    //result.Override = true;
                    result.GetIndividualFileComparison();
                    result.GetBulkResultCountComparisonFile();
                    result.CountChimericPsms();

                    if (result is IChimeraBreakdownCompatible cb)
                        cb.GetChimeraBreakdownFile();
                    if (result is IChimeraPeptideCounter pc)
                        pc.CountChimericPeptides();
                    if (result is MetaMorpheusResult mm)
                    {
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                        mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                    }

                    //result.Override = false;
                }

                try
                {
                    cellLine.PlotModificationDistribution(ResultType.Psm, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                try
                {
                    cellLine.PlotModificationDistribution(ResultType.Peptide, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                cellLine.GetMaximumChimeraEstimationFile();
                cellLine.GetChimeraBreakdownFile();

                cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;


                cellLine.PlotModificationDistribution(ResultType.Psm, false);
                cellLine.PlotModificationDistribution(ResultType.Peptide, false);
                cellLine.Dispose();
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
                //cellLine.PlotIndividualFileResults(ResultType.Psm);
                //cellLine.PlotIndividualFileResults(ResultType.Peptide);
                //cellLine.PlotIndividualFileResults(ResultType.Protein);
                //cellLine.PlotCellLineRetentionTimePredictions();
                //cellLine.PlotCellLineSpectralSimilarity();
                    //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                //cellLine.PlotChronologerDeltaKernelPDF();
                //cellLine.PlotChronologerVsPercentHi();
                foreach (var individualResult in cellLine
                             .Where(p => cellLine.GetSingleResultSelector().Contains(p.Condition)))
                {
                    if (individualResult is not MetaMorpheusResult mm) continue;
              
                    mm.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                    mm.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);

                    //mm.PlotPepFeaturesScatterGrid();
                    //mm.PlotTargetDecoyCurves();
                    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                }
                //cellLine.Dispose(); 
                cellLine.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
                cellLine.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            }

            //AllResults.PlotInternalMMComparison();
            //AllResults.PlotBulkResultComparisons();
            //AllResults.PlotStackedIndividualFileComparison();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotStackedSpectralSimilarity();
            //AllResults.PlotAggregatedSpectralSimilarity();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            //AllResults.PlotChronologerVsPercentHi();
            //AllResults.PlotBulkChronologerDeltaPlotKernalPDF();
            //AllResults.PlotGridChronologerDeltaPlotKernalPDF();

            AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            TopDownRunner.AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            TopDownRunner.AllResults.PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            TopDownRunner.AllResults.First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            TopDownRunner.AllResults.First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
            TopDownRunner.AllResults.Skip(1).First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Psm);
            TopDownRunner.AllResults.Skip(1).First().PlotChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide);
        }

        [Test]
        public static void RunProformaShit_SumbissionDirectory()
        {
            string resultDirectoryPath = @"D:\Projects\Chimeras\SumbissionDirectory\Mann_11cell_analysis";
            var allResults = new AllResults(resultDirectoryPath, Directory.GetDirectories(resultDirectoryPath)
                           .Where(p => !p.Contains("Figures") && !p.Contains("Prosight") && !p.Contains("Proforma"))
                           .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

            foreach (var cellLine in allResults)
            {
                foreach (var result in cellLine)
                {
                    if (result is MsFraggerResult msf)
                        msf.ToPsmProformaFile();
                }
            }

            var bigResultPath = @"D:\Projects\Chimeras\SumbissionDirectory\Mann_11cell_analysis\ProformaSummary";

            var temp = allResults.SelectMany(p => p.Results)
                .GroupBy(p => p.Condition).ToDictionary(p => p.Key, p => p.ToList());

            foreach (var condition in temp)
            {
                var proforomaFileName = Path.Combine(bigResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProformaFile);
                var records = new List<ProformaRecord>();
                foreach (var result in condition.Value)
                {
                    if (result is MsFraggerResult msf)
                        records.AddRange(msf.ToPsmProformaFile().Results);
                }

                var newFile = new ProformaFile(proforomaFileName)
                {
                    Results = records
                };

                newFile.WriteResults(proforomaFileName);
            }
        }


    }
}
