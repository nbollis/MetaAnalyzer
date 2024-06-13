using System.Data;
using System.Text;
using Analyzer.Interfaces;
using Analyzer.Plotting;
using Analyzer.SearchType;
using Analyzer.Util;
using Proteomics;
using Readers;
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
            foreach (CellLineResults cellLine in AllResults)
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

                    //result.Override = false;
                }


                cellLine.GetMaximumChimeraEstimationFile();
                cellLine.GetChimeraBreakdownFile();

                cellLine.Override = true;
                cellLine.GetIndividualFileComparison();
                cellLine.GetBulkResultCountComparisonFile();
                cellLine.CountChimericPsms();
                cellLine.CountChimericPeptides();
                cellLine.Override = false;
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

            //AllResults.PlotInternalMMComparison();
            //AllResults.PlotBulkResultComparisons();
            //AllResults.PlotStackedIndividualFileComparison();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotStackedSpectralSimilarity();
            //AllResults.PlotAggregatedSpectralSimilarity();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
            //AllResults.PlotChronologerVsPercentHi();
            AllResults.PlotBulkChronologerDeltaPlotKernalPDF();
            AllResults.PlotGridChronologerDeltaPlotKernalPDF();

            foreach (CellLineResults cellLine in AllResults)
            {
                //cellLine.PlotIndividualFileResults(ResultType.Psm);
                //cellLine.PlotIndividualFileResults(ResultType.Peptide);
                //cellLine.PlotIndividualFileResults(ResultType.Protein);
                //cellLine.PlotCellLineRetentionTimePredictions();
                //cellLine.PlotCellLineSpectralSimilarity();
                //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                cellLine.PlotChronologerDeltaKernelPDF();
                cellLine.PlotChronologerVsPercentHi();
                foreach (var individualResult in cellLine)
                {
                    //if (individualResult is not MetaMorpheusResult mm) continue;
                    //mm.ExportPepFeaturesPlots();
                    //mm.PlotTargetDecoyCurves();
                    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                }
                cellLine.Dispose(); 
            }
        }

 

        [Test]
        public static void RunSpecificCellLine()
        {
            string cellLineString = "A549";
            //var cellLine = AllResults.First(p => p.CellLine == cellLineString);
            var cellLine = new CellLineResults(Path.Combine(DirectoryPath, "A549"));
            
            AllResults.PlotBulkChronologerDeltaPlotKernalPDF();
            //cellLine.PlotIndividualFileResults();
            //cellLine.PlotIndividualFileResults(ResultType.Psm);
            //cellLine.PlotIndividualFileResults(ResultType.Protein);
            //cellLine.PlotCellLineChimeraBreakdown();
            //cellLine.PlotCellLineRetentionTimePredictions();
            //cellLine.GetChronologerDeltaPlotKernelPDF();
            //cellLine.GetMaximumChimeraEstimationFile();
            //cellLine.GetChronologerDeltaHistogram();
            //cellLine.PlotCellLineSpectralSimilarity();
            //cellLine.PlotCellLineChimeraBreakdown();
            //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
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

    }
}
