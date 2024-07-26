using System.Diagnostics;
using System.Text;
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
        public static void GenerateCommandLinePrompts()
        {

            CommandLineArgumentRunner.GenerateLibraryFiles();

            CommandLineArgumentRunner.RunMann11InternalComparisonInParallel();



            
        }



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
            //AllResults.PlotChimeraBreakdownHybridFigure(ResultType.Psm);
            //AllResults.PlotChimeraBreakdownHybridFigure(ResultType.Peptide);
            //TopDownRunner.AllResults.First().PlotChimeraBreakdownHybridFigure(ResultType.Psm);
            //TopDownRunner.AllResults.First().PlotChimeraBreakdownHybridFigure(ResultType.Peptide);
            //TopDownRunner.AllResults.Skip(1).First().PlotChimeraBreakdownHybridFigure(ResultType.Psm);
            //TopDownRunner.AllResults.Skip(1).First().PlotChimeraBreakdownHybridFigure(ResultType.Peptide);
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
                        mm.PlotChimeraBreakDownHybridFigure(ResultType.Peptide);
                    //mm.PlotPepFeaturesScatterGrid();
                    //mm.PlotTargetDecoyCurves();
                    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                }
                cellLine.Dispose(); 
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
        }

 

        [Test]
        public static void WeekendRunner()
        {

            //foreach (var cellLine in AllResults)
            //{
            //    var cellLineParams = new CellLineAnalysisParameters(cellLine.DirectoryPath, false, false, cellLine);
            //    SingleRunRetentionTimeCalibrationTask task = new(cellLineParams);
            //    task.Run();
            //}


            List<string> errors = new();
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine.Where(p => cellLine.GetAllSelectors().Contains(p.Condition)))
                {
                    result.Override = true;
                    //try
                    //{
                    //    result.GetIndividualFileComparison();
                    //    result.CountChimericPsms();
                    //    if (result is IChimeraPeptideCounter pc)
                    //        pc.CountChimericPeptides();
                    //}
                    //catch (Exception e)
                    //{
                    //    errors.Add($"Individual file comparison failed for {cellLine.CellLine}:{result.Condition} with error {e.Message}");
                    //}


                    //try
                    //{
                    //    if (result is IChimeraBreakdownCompatible cbc)
                    //    {
                    //        cbc.GetChimeraBreakdownFile();
                    //    }
                    //}
                    //catch (Exception e)
                    //{
                    //    errors.Add($"Chimera breakdown failed for {cellLine.CellLine}:{result.Condition} with error {e.Message}");
                    //}

                    try
                    {
                        if (result is MetaMorpheusResult mm && cellLine.GetSingleResultSelector().Contains(mm.Condition))
                        {
                            mm.GetChimericSpectrumSummaryFile();
                        }
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Chimeric spectrum summary failed for {cellLine.CellLine}:{result.Condition} with error {e.Message}");
                    }

                    result.Override = false;
                }

                //cellLine.Override = true;
                //cellLine.GetIndividualFileComparison();
                //cellLine.CountChimericPsms();
                //cellLine.CountChimericPeptides();
                //try
                //{
                //    cellLine.GetChimeraBreakdownFile();
                //}
                //catch (Exception e)
                //{
                //    errors.Add($"Chimera breakdown failed for {cellLine.CellLine} with error {e.Message}");
                //}

                //cellLine.Override = false;
                //cellLine.PlotIndividualFileResults(ResultType.Psm);
                //cellLine.PlotIndividualFileResults(ResultType.Peptide);
                //cellLine.PlotIndividualFileResults(ResultType.Protein);
                //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();

                cellLine.Dispose();
            }

            //AllResults.Override = true;
            //AllResults.GetBulkResultCountComparisonFile();
            //AllResults.IndividualFileComparison();
            //AllResults.CountChimericPsms();
            //AllResults.CountChimericPeptides();
            //AllResults.GetChimeraBreakdownFile();
            //AllResults.Override = false;



            foreach (var cellLine in TopDownRunner.AllResults)
            {
                foreach (var result in cellLine.Where(p => cellLine.GetAllSelectors().Contains(p.Condition)))
                {
                    result.Override = true;
                    //try
                    //{
                    //    result.GetIndividualFileComparison();
                    //    result.CountChimericPsms();
                    //    if (result is IChimeraPeptideCounter pc)
                    //        pc.CountChimericPeptides();
                    //}
                    //catch (Exception e)
                    //{
                    //    errors.Add($"Individual file comparison failed for {cellLine.CellLine}:{result.Condition} with error {e.Message}");
                    //}


                    //try
                    //{
                    //    if (result is IChimeraBreakdownCompatible cbc)
                    //    {
                    //        cbc.GetChimeraBreakdownFile();
                    //    }
                    //}
                    //catch (Exception e)
                    //{
                    //    errors.Add($"Chimera breakdown failed for {cellLine.CellLine}:{result.Condition} with error {e.Message}");
                    //}

                    try
                    {
                        if (result is MetaMorpheusResult mm && cellLine.GetSingleResultSelector().Contains(mm.Condition))
                        {
                            mm.GetChimericSpectrumSummaryFile();
                        }
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Chimeric spectrum summary failed for {cellLine.CellLine}:{result.Condition} with error {e.Message}");
                    }

                    result.Override = false;
                }

                //cellLine.Override = true;
                //cellLine.GetIndividualFileComparison();
                //cellLine.CountChimericPsms();
                //cellLine.CountChimericPeptides();
                //try
                //{
                //    cellLine.GetChimeraBreakdownFile();
                //}
                //catch (Exception e)
                //{
                //    errors.Add($"Chimera breakdown failed for {cellLine.CellLine} with error {e.Message}");
                //}

                //cellLine.Override = false;
                //cellLine.PlotIndividualFileResults(ResultType.Psm);
                //cellLine.PlotIndividualFileResults(ResultType.Peptide);
                //cellLine.PlotIndividualFileResults(ResultType.Protein);
                //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            //TopDownRunner.AllResults.Override = true;
            //TopDownRunner.AllResults.GetBulkResultCountComparisonFile();
            //TopDownRunner.AllResults.IndividualFileComparison();
            //TopDownRunner.AllResults.CountChimericPsms();
            //TopDownRunner.AllResults.CountChimericPeptides();
            //TopDownRunner.AllResults.GetChimeraBreakdownFile();
            //TopDownRunner.AllResults.Override = false;
        }

        [Test]
        public static void TESTNAME()
        {
            var cellLine = AllResults.First();
            var frag = (MsFraggerResult)cellLine.First(p => p.Condition.Contains("ReviewdDatabaseNoPhospho_MsFraggerDDA+"));
            //frag.CreateRetentionTimePredictionFile();
            frag.PlotChronologerDeltaKernelPDF();

        }

        [Test]
        public static void GeneParserForBurke()
        {
            string geneOfInterest = "C11orf68";
            int psmCount = 0;
            int onePercentPsmCount = 0;
            int peptideCount = 0;
            int onePercentPeptideCount = 0;
            int onePercentProteinCount = 0;
            int proteinCount = 0;
            List<string> filesFoundIn = new List<string>();
            foreach (var cellLine in AllResults)
            {
                var mmResults = cellLine.First(p => p.Condition == "MetaMorpheusWithLibrary") as MetaMorpheusResult;

                foreach (var psm in SpectrumMatchTsvReader.ReadPsmTsv(mmResults.PsmPath, out _))
                    if (psm.GeneName.Contains(geneOfInterest) || psm.Description.Contains(geneOfInterest, StringComparison.InvariantCultureIgnoreCase))
                    {
                        psmCount++;
                        filesFoundIn.Add(psm.FileNameWithoutExtension);
                        if (psm.QValue <= 0.01)
                            onePercentPsmCount++;
                    }

                foreach (var peptide in SpectrumMatchTsvReader.ReadPsmTsv(mmResults.PeptidePath, out _))
                    if (peptide.GeneName.Contains(geneOfInterest) || peptide.Description.Contains(geneOfInterest, StringComparison.InvariantCultureIgnoreCase))
                    {
                        peptideCount++;
                        filesFoundIn.Add(peptide.FileNameWithoutExtension);
                        if (peptide.QValue <= 0.01)
                            onePercentPeptideCount++;
                    }
                using (var sw = new StreamReader(File.OpenRead(mmResults.ProteinPath)))
                {
                    var header = sw.ReadLine();
                    var headerSplit = header.Split('\t');
                    var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                    var geneIndex = Array.IndexOf(headerSplit, "Gene");


                    while (!sw.EndOfStream)
                    {
                        var line = sw.ReadLine();
                        var values = line.Split('\t');
                        if (values[geneIndex].Contains(geneOfInterest, StringComparison.InvariantCultureIgnoreCase))
                        {
                            proteinCount++;
                            if (double.Parse(values[qValueIndex]) <= 0.01)
                                onePercentProteinCount++;
                        }
                    }
                }


            }   
            filesFoundIn = filesFoundIn.Distinct().ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"Gene {geneOfInterest} was found in:");
            sb.AppendLine($"{psmCount} Psms");
            sb.AppendLine($"{onePercentPsmCount} Psms with qValue <= 0.01");
            sb.AppendLine($"{peptideCount} Peptides");
            sb.AppendLine($"{onePercentPeptideCount} Peptides with qValue <= 0.01");
            sb.AppendLine($"{proteinCount} Proteins");
            sb.AppendLine($"{onePercentProteinCount} Proteins with qValue <= 0.01");
            sb.AppendLine($"In the following files:");
            sb.AppendLine(string.Join('\n', filesFoundIn));
            var result = sb.ToString();
        }



        [Test]
        public static void ChimerysResultSplitter()
        {
            
        }

    }
}
