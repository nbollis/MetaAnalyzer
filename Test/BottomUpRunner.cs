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
        public static void WeekendRunner()
        {

            string dir = @"B:\Users\Nic\Chimeras\InternalMMAnalysis\Mann_11cell_lines";
            string toReplace = "Mann_11cell_lines_";
            foreach (var cellLineDir in Directory.GetDirectories(dir))
            {
                if (cellLineDir.Contains("Generate"))
                    continue;

                var cellLine = Path.GetFileNameWithoutExtension(cellLineDir);
                foreach (var runDir in Directory.GetDirectories(cellLineDir))
                {
                    if (runDir.Contains("Figure"))
                        continue;
                    var files = Directory.GetFiles(runDir, "*.csv");
                    foreach (var file in files)
                    {
                        if (Path.GetFileNameWithoutExtension(file).StartsWith(toReplace))
                        {
                            string newName = file.Replace(toReplace, $"{cellLine}_");
                            File.Move(file, newName);
                        }
                            
                    }
                }
            }
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
