using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting.AggregatePlots;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET.ImageExport;
using Plotting.Util;
using ResultAnalyzerUtil;
using System.Diagnostics;
using UsefulProteomicsDatabases;

namespace Test.ChimeraPaper
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
        public static void PaperInternalMMParsing()
        {
            string path = @"B:\Users\Nic\Chimeras\InternalMMAnalysis\TopDown_Jurkat\Jurkat";
            var groupedRuns = Directory.GetDirectories(path)
                .Select(p => new MetaMorpheusResult(p))
                .GroupBy(p => p.Condition.ConvertConditionName())
                .ToDictionary(p => p.Key, p => p.ToList());



        }




        [Test]
        public static void RunAllParsing()
        {
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine.Skip(2))
                {
                    result.Override = true;
                    //result.CountChimericPsms();
                    //result.GetBulkResultCountComparisonFile();
                    //result.GetIndividualFileComparison();
                    if (result is IChimeraBreakdownCompatible cb){
                        cb.GetChimeraBreakdownFile();
                        cb.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                        try
                        {
                            cb.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
                        } catch { // Ignore
                        }
                    }
                    //if (result is IChimeraPeptideCounter pc)
                    //    pc.CountChimericPeptides();
                    //if (result is MetaMorpheusResult mm)
                    //{
                    //    //mm.PlotPepFeaturesScatterGrid();
                    //    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                    //    //mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, false);
                    //    //mm.PlotTargetDecoyCurves(ResultType.Psm, TargetDecoyCurveMode.Score, true);
                    //    //mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, false);
                    //    //mm.PlotTargetDecoyCurves(ResultType.Peptide, TargetDecoyCurveMode.Score, true);
                    //}
                    result.Override = false;
                }

                //cellLine.Override = true;
                //cellLine.GetIndividualFileComparison();
                //cellLine.GetBulkResultCountComparisonFile();
                //cellLine.CountChimericPsms();
                //cellLine.CountChimericPeptides();
                //cellLine.Override = false;

                //cellLine.PlotIndividualFileResults();
                //cellLine.PlotCellLineSpectralSimilarity();
                cellLine.PlotCellLineChimeraBreakdown();
                cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
                //cellLine.PlotModificationDistribution(ResultType.Psm, false);
                //cellLine.PlotModificationDistribution(ResultType.Peptide, false);

                cellLine.Dispose();
            }

            AllResults.Override = true;
            AllResults.IndividualFileComparison();
            AllResults.GetBulkResultCountComparisonFile();
            AllResults.CountChimericPsms();
            AllResults.CountChimericPeptides();

        }

        [Test]
        public static void GenerateAllFigures()
        {
            foreach (CellLineResults cellLine in AllResults)
            {
                //foreach (var individualResult in cellLine)
                //{
                //    if (individualResult is not MetaMorpheusResult mm) continue;
                //    //mm.PlotPepFeaturesScatterGrid();
                //    //mm.ExportCombinedChimeraTargetDecoyExploration(mm.FigureDirectory, mm.Condition);
                //}

                //cellLine.PlotIndividualFileResults();
                //cellLine.PlotCellLineSpectralSimilarity();
                //cellLine.PlotCellLineChimeraBreakdown();
                //cellLine.PlotCellLineChimeraBreakdown_TargetDecoy();
            }

            AllResults.PlotInternalMMComparison();
            //AllResults.PlotBulkResultComparisons();
            //AllResults.PlotStackedIndividualFileComparison();
            //AllResults.PlotBulkResultChimeraBreakDown();
            //AllResults.PlotBulkResultChimeraBreakDown_TargetDecoy();
        }

        [Test]
        public static void GenerateSpecificFigures()
        {
            var a549 = BottomUpRunner.AllResults.First();
            a549.PlotModificationDistribution();
            a549.PlotModificationDistribution(ResultType.Peptide);
            var jurkat = AllResults.Skip(1).First();
            jurkat.PlotModificationDistribution();
            jurkat.PlotModificationDistribution(ResultType.Peptide);
            //a549.PlotAccuracyByModificationType();
            //a549.PlotChronologerDeltaKernelPDF();
        }

        [Test]
        public static void TopDownComparison()
        {
            List<MetaMorpheusResult> metaMorpheusResults = [];
            List<MsPathFinderTResults> msPathFinderResults = [];
            List<ProteomeDiscovererResult> proteomeDiscovererResults = [];

            // load relevant data only
            foreach (var cellLine in AllResults.Skip(1))
            {
                var cellLineLabel = cellLine.ToString();
                foreach (var result in cellLine)
                {
                    switch (result)
                    {
                        case MetaMorpheusResult mm when (mm.Condition == "MetaMorpheus" && cellLineLabel == "Jurkat") || (mm.Condition == "MetaMorpheus" && cellLineLabel == "Ecoli"):
                            metaMorpheusResults.Add(mm);
                            break;
                        case MsPathFinderTResults mspt when mspt.Condition == "MsPathFinderTWithMods_15Rep2_Final":
                            msPathFinderResults.Add(mspt);
                            break;
                        case ProteomeDiscovererResult { Condition: "ProsightPdChimeras_Rep2_15_10ppm" } pd when cellLineLabel == "Jurkat":
                        case ProteomeDiscovererResult { Condition: "ProsightPDChimeras_15" } when cellLineLabel == "Ecoli":
                            proteomeDiscovererResults.Add(result as ProteomeDiscovererResult);
                            break;
                    }
                }
            }

            // reduce to only rep2
            metaMorpheusResults.First().IndividualFileResults = metaMorpheusResults.First().IndividualFileResults.Where(p => p.FileName.Contains("rep2")).ToList();
            msPathFinderResults.First().IndividualFileResults = msPathFinderResults.First().IndividualFileResults.Where(p => p.RawFilePath.Contains("rep2")).ToList();

            List<SingleRunResults> allResults = metaMorpheusResults.Concat(msPathFinderResults.Cast<SingleRunResults>()).Concat(proteomeDiscovererResults).ToList();
            string outDir = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Figures";
            var chimeraCountingPath = Path.Combine(outDir, "ChimeraCountingResults.csv");

            var file = new ChimeraCountingFile(chimeraCountingPath);
            file.Results.GetChimeraCountingPlot(true);



            // parse
            ChimeraCountingFile? chimeraCountingResults = null!;
            foreach (var result in allResults)
            {
                string software = result switch
                {
                    MetaMorpheusResult => "MetaMorpheus",
                    MsPathFinderTResults => "MsPathFinderT",
                    ProteomeDiscovererResult => "ProteomeDiscoverer",
                    _ => throw new ArgumentOutOfRangeException()
                };

                // TODO: remove this
                if (software.StartsWith("M"))
                    continue;

                result.Override = true;
                //var chimericPsms = result.CountChimericPsms();
                //if (chimeraCountingResults == null)
                //{
                //    chimeraCountingResults = chimericPsms;
                //}
                //else
                //{
                //    chimeraCountingResults.Results.AddRange(chimericPsms.Results);
                //}

                var proforma = result.ToPsmProformaFile();
                result.Override = false;



                var proformaPath = Path.Combine(outDir, $"{software}_JurkatRep2_Proforma.tsv");
                proforma.WriteResults(proformaPath);
            }

            chimeraCountingResults.WriteResults(chimeraCountingPath);

        }

            [Test]
        public static void MsPathTDatasetInfoGenerator()
        {

            //var path =@"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\SearchResults\MsPathFinderTWithMods_15Rep2_Final";
            var path = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Ecoli\SearchResults\MsPathFinderTWithMods_15_Final";
            var result = new MsPathFinderTResults(path);
            result.GetIndividualFileComparison();
            result.ToPsmProformaFile();

            foreach (MsPathFinderTResults mspt in AllResults.SelectMany(p => p.Results)
                .Where(p => p is MsPathFinderTResults mspt && mspt.IndividualFileResults.Count is 20 or 43 or 10))
            {
                mspt.CreateDatasetInfoFile();
            }
        }


        [Test]
        public static void DetermineChimericResultsScanPercentForPaper()
        {
            // get the single result selector result file from each cell line
            var data =
                AllResults.SelectMany(p => p.Where(m => p.GetSingleResultSelector().Contains(m.Condition))).ToList();
            data.AddRange(BottomUpRunner.AllResults.SelectMany(p => p.Where(m => p.GetSingleResultSelector().Contains(m.Condition))).ToList());

            var results = data.ToDictionary(p => (p.Condition, p.DatasetName),
                        p => ((MetaMorpheusResult)p).AllPsms
                            .Where(p => p is { DecoyContamTarget: "T", PEP_QValue: <= 0.01 })
                            .ToChimeraGroupedDictionary());


            var percentResults = results.ToDictionary(p => p.Key,
                p => p.Value
                    .OrderBy(n => n.Key)
                    .ToDictionary(
                    m => m.Key,
                    m => m.Value.Count / (double)p.Value.Sum(p => p.Value.Count) * 100));

            var singles = percentResults.Select(p => (p.Key.Item1, p.Key.Item2, p.Value[1])).ToList();

            var topDown = singles.Take(AllResults.Count());
            var bottomUp = singles.Skip(AllResults.Count());

            var topDownAveragePercent = topDown.Average(p => p.Item3);
            var bottomUpAveragePercent = bottomUp.Average(p => p.Item3);
        }





        [Test]
        public static void RunProteinCountingAndProForma()
        {
            List<string> errors = new();
            foreach (var cellLine in AllResults)
            {
                foreach (var result in cellLine)
                {
                    try
                    {
                        result.CountProteins();
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Counting: {result.Condition} {result.DatasetName} {e.Message}");
                    }

                    try
                    {
                        result.ToPsmProformaFile();
                    }
                    catch (Exception e)
                    {
                        errors.Add($"ProForma: {result.Condition} {result.DatasetName} {e.Message}");
                    }
                }
            }

            errors.ForEach(Console.WriteLine);
        }



        [Test]
        public static void Renamer()
        {
            string dirPath = @"B:\RawSpectraFiles\Mann_11cell_lines";
            var directories = Directory.GetDirectories(dirPath);
            var calibAvgDirs = directories.Where(p => p.Contains("ged_107")).ToList();
            var cellLineDirs = directories.Except(calibAvgDirs).ToList();

            Dictionary<string, List<string>> fileDictionary = new();
            foreach (var replicateDir in calibAvgDirs)
            {
                int rep = int.Parse(replicateDir.Last().ToString());
                var toLook = Directory.GetDirectories(replicateDir).First(p => p.Contains("Task1-C"));

                var files = Directory.GetFiles(toLook).Where(p => !p.EndsWith(".txt"));

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var converted = fileName.ConvertFileName();

                    var splits = converted.Split('_');
                    var name = splits[0];
                    var innerRep = int.Parse(splits[1]);
                    var fileNumber = int.Parse(splits[2]);

                    if (rep != innerRep)
                        Debugger.Break();

                    if (fileDictionary.ContainsKey(name))
                    {
                        fileDictionary[name].Add(file);
                    }
                    else
                    {
                        fileDictionary.Add(name, [file]);
                    }
                }
            }

            // Find the folder called CalibratedAveraged and rename it to 106_CalibratedAveraged
            foreach (var cellLineDir in cellLineDirs)
            {
                var destinationDir = Path.Combine(cellLineDir, "106_Calibrated");
                if (Directory.Exists(destinationDir) && Directory.GetFiles(destinationDir).Length == 36)
                    continue;


                var calibDirs = Directory.GetDirectories(cellLineDir).Where(p => p.Contains("Calibra", StringComparison.InvariantCultureIgnoreCase) && !p.Contains("Averaged", StringComparison.InvariantCultureIgnoreCase))
                    .Where(p => p != destinationDir)
                    .ToList();
                if (calibDirs.Count > 1)
                    Debugger.Break();
                else if (calibDirs.Count == 1)
                {
                    var calibDir = calibDirs.First();
                    Directory.Move(calibDir, destinationDir);
                }


                var cellLineName = Path.GetFileNameWithoutExtension(cellLineDir);
                var calibPath = Path.Combine(cellLineDir, "Calibrated");
                Directory.CreateDirectory(calibPath);

                var files = fileDictionary[cellLineName];

                foreach (var file in files)
                {
                    var originalDir = Path.GetDirectoryName(file);
                    var newLocation = file.Replace(originalDir!, calibPath);

                    File.Move(file, newLocation);
                }
            }
        }


        [Test]
        public static void MsPathFinderTAnalysis()
        {
            string dirpath = @"D:\Projects\Chimeras\MsPTVal";

            var pathfinder = new MsPathFinderTResults(dirpath);
            pathfinder.Override = true;
            var file = pathfinder.GetChimeraBreakdownFile();
            pathfinder.Override = false;

            var plot = file.Results.GetChimeraBreakdownStackedColumn_Scaled(ResultType.Psm, true);
            var outPath = Path.Combine(dirpath, "Figures", "ChimeraBreakdown_Psms_Scaled");
            plot.SavePNG(outPath, null, 1200, 1200);

            plot = file.Results.GetChimeraBreakDownStackedArea(ResultType.Psm, true, out int width);
            outPath = Path.Combine(dirpath, "Figures", "ChimeraBreakdown_Psms_Area");
            plot.SavePNG(outPath, null, 1200, 1200);

            plot = file.Results.GetChimeraBreakDownStackedColumn(ResultType.Psm, true, out width);
            outPath = Path.Combine(dirpath, "Figures", "ChimeraBreakdown_Psms");
            plot.SavePNG(outPath, null, 1200, 1200);


            plot = file.Results.GetChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide, true);
            outPath = Path.Combine(dirpath, "Figures", "ChimeraBreakdown_Peptides_Scaled");
            plot.SavePNG(outPath, null, 1200, 1200);

            plot = file.Results.GetChimeraBreakDownStackedArea(ResultType.Peptide, true, out width);
            outPath = Path.Combine(dirpath, "Figures", "ChimeraBreakdown_Peptides_Area");
            plot.SavePNG(outPath, null, 1200, 1200);

            plot = file.Results.GetChimeraBreakDownStackedColumn(ResultType.Peptide, true, out width);
            outPath = Path.Combine(dirpath, "Figures", "ChimeraBreakdown_Peptides");
            plot.SavePNG(outPath, null, 1200, 1200);
        }
    }
}
