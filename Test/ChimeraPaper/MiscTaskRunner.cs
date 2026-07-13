using System.Collections.Specialized;
using System.Drawing.Text;
using System.Globalization;
using Analyzer.FileTypes.Internal;
using Analyzer.Interfaces;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Calibrator;
using CsvHelper;
using CsvHelper.Configuration;
using MassSpectrometry;
using Plotly.NET;
using Plotly.NET.ImageExport;
using Plotting;
using Plotting.Util;
using Readers;
using ResultAnalyzerUtil;
using RetentionTimePrediction;
using TaskLayer.ChimeraAnalysis;
using UsefulProteomicsDatabases;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.ChimeraPaper
{
    internal class MiscTaskRunner
    {
        public static string Man11FDRRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_Mann11_Ind";

        public static string Man11AllResultsPath => BottomUpRunner.DirectoryPath;
        public static string TopDownDirectoryPath => TopDownRunner.DirectoryPath;

        public static string TopDownJurkatFDRRunPath =>
            @"B:\Users\Nic\Chimeras\FdrAnalysis\UseProvidedLibraryOnAllFiles_JurkatTD";

        [Test]
        public static void DirCleanerUpper()
        {
            var dirpath = @"D:\Projects\Chimeras\SumbissionDirectory";


            //foreach (var dir in Directory.GetFiles(dirpath, "*MetaDraw*", SearchOption.AllDirectories))
            //{
            //    Directory.Delete(dir, true);
            //}


            var initialFiles = Directory.GetFiles(dirpath, "*", SearchOption.AllDirectories);

            foreach (var file in initialFiles)
            {
                string newPath;
                if (file.EndsWith(".mzrt.csv"))
                {
                    newPath = file.Replace(".mzrt.csv", "_mzrt.csv");
                }
                else if (file.EndsWith(".feature"))
                {
                    newPath = file.Replace(".feature", "_feature.tsv");
                }
                else if (file.EndsWith(".msalign"))
                {
                    newPath = file.Replace(".msalign", "_msalign.tsv");
                }
                else
                {
                   continue;
                }

                if (!File.Exists(newPath))
                {
                    File.Move(file, newPath);
                }
            }


            var files = Directory.GetFiles(dirpath, "*", SearchOption.AllDirectories)
                .GroupBy(Path.GetExtension)
                .ToDictionary(p => p.Key, p => (p.ToList(), p.Select(Path.GetFileNameWithoutExtension).ToList(), p.Select(Path.GetFileNameWithoutExtension).Distinct().ToList()));

            Dictionary<string, skip> deleteDict = files
                .ToDictionary(p => p.Key, p => new skip(p.Key));
            //deleteDict[".xml"].EndsWith.AddRange(["_feature"]);
            deleteDict[".txt"].StartsWith.AddRange(["log_2024-", "glyco_masses_list"]);
            deleteDict[".txt"].EndsWith.AddRange(["Prose"]);
            deleteDict[".xml"].EndsWith.AddRange(["_feature"]);


            List<string> extensionsToDelete = new() { ".db", ".png", ".zip", ".tab" };
            List<string> directoriesToDeleteStartsWith = new() { ".toppic_", ".msalign_index", ".bin" };


            List<string> removedPaths = new();
            foreach (var fileGroup in files)
            {
                var paths = fileGroup.Value.Item1;
                var names = fileGroup.Value.Item2;
                var distinctNames = fileGroup.Value.Item3;

                if (directoriesToDeleteStartsWith.Any(p => fileGroup.Key!.StartsWith(p)))
                {
                    removedPaths.AddRange(paths);
                    continue;
                }

                if (extensionsToDelete.Any(p => p == fileGroup.Key))
                {
                    removedPaths.AddRange(paths);
                    continue;
                }

                var theSkipEntry = deleteDict[fileGroup.Key!];

                var toRemove = new List<int>();
                for (int i = 0; i < names.Count; i++)
                {
                    if (theSkipEntry.EndsWith.Any(s => names[i].EndsWith(s)))
                        toRemove.Add(i);

                    if (theSkipEntry.StartsWith.Any(s => names[i].StartsWith(s)))
                        toRemove.Add(i);
                }

                for (int i = toRemove.Count - 1; i >= 0; i--)
                {
                    int indexToRemove = toRemove[i];
                    removedPaths.Add(paths[indexToRemove]);
                    paths.RemoveAt(indexToRemove);
                    names.RemoveAt(indexToRemove);
                }
            }

            var removeDict = removedPaths
                .GroupBy(Path.GetExtension)
                .ToDictionary(p => p.Key, p => (p.ToList(), p.Select(Path.GetFileNameWithoutExtension).ToList(), p.Select(Path.GetFileNameWithoutExtension).Distinct().ToList()));

            foreach (var path in removedPaths)
            {
                File.Delete(path);
            }

        }

        public class skip(string exten)
        {
            public string Extension { get; set; } = exten;
            public List<string> EndsWith { get; set; } = new();
            public List<string> StartsWith { get; set; } = new();
        }



        [Test]
        public static void SpectrumSimilarityTaskRunner()
        {
            string path = Man11FDRRunPath;
            var parameters =
                new SingleRunAnalysisParameters(path, false, true, new MetaMorpheusResult(path));
            var task = new SingleRunSpectralAngleComparisonTask(parameters);
            task.Run();
        }

        [Test]
        public static void GetPaperNumbers()
        {
            // user internal comparison to get paper numbers
            List<MetaMorpheusResult> GetBottomUpResults()
            {
                string buBasePath = InternalMetaMorpheusAnalysisTask.Mann11OutputDirectory;
                var results = new List<MetaMorpheusResult>();

                foreach (var searchPath in Directory.GetDirectories(buBasePath, $"*WithChimeras_{InternalMetaMorpheusAnalysisTask.Version}_ChimericLibrary*", SearchOption.AllDirectories)
                    .Where(p => !p.Contains("Figure"))
                    .Where(p => !p.Contains("Processed"))
                    .Where(p => !p.Contains("Generate")))
                {
                    var result = new MetaMorpheusResult(searchPath);
                    results.Add(result);
                }

                return results;
            }

            List<MetaMorpheusResult> GetTopDownResults()
            {
                string tdBasePath = InternalMetaMorpheusAnalysisTask.JurkatTopDownOutputDirectory;
                var results = new List<MetaMorpheusResult>();
                foreach (var searchPath in Directory.GetDirectories(tdBasePath, $"*WithChimeras_{InternalMetaMorpheusAnalysisTask.Version}_ChimericLibrary*", SearchOption.AllDirectories)
                    .Where(p => !p.Contains("Figure"))
                    .Where(p => !p.Contains("Processed"))
                    .Where(p => !p.Contains("Generate")))
                {
                    var result = new MetaMorpheusResult(searchPath);
                    results.Add(result);
                }
                return results;
            }

            List<MetaMorpheusResult> buResults = GetBottomUpResults();
            List<MetaMorpheusResult> tdResults = GetTopDownResults();

            var byPsm = buResults.CalculateMetrics(ResultType.Psm);
            var byPeptide = buResults.CalculateMetrics(ResultType.Peptide);

            var tdByPsm = tdResults.CalculateMetrics(ResultType.Psm);
            var tdByPeptide = tdResults.CalculateMetrics(ResultType.Peptide);

            List<PaperNumbers> allnumbrs = [byPsm, byPeptide, tdByPsm, tdByPeptide];

            string outpath = @"B:\Users\Nic\Chimeras\InternalMMAnalysis\parsednums.csv";

            using var csv = new CsvWriter(new StreamWriter(outpath), new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteHeader<PaperNumbers>();
            foreach (var result in allnumbrs)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }





        [Test]
        public static void ChimericSpectrumSummaryTask()
        {
            string path = Man11FDRRunPath;
            //string path = TopDownJurkatFDRRunPath;
            var mmRun = new MetaMorpheusResult(path);
            var parameters = new SingleRunAnalysisParameters(path, false, false, mmRun);
            var task = new SingleRunChimericSpectrumSummaryTask(parameters);

            task.Run();
        }

        [Test]
        public static void RunRetentionTimeAdjustmentTask()
        {
            var allResults = new AllResults(Man11AllResultsPath);
            // figures found at B:\Users\Nic\Chimeras\Mann_11cell_analysis\A549\Figures
            foreach (var cellLine in allResults)
            {
                foreach (var singleRunResult in cellLine)
                {
                    if (singleRunResult is not IRetentionTimePredictionAnalysis)
                        continue;
                    if (!cellLine.GetSingleResultSelector().Contains(singleRunResult.Condition))
                        continue;

                    var parameters = new SingleRunAnalysisParameters(singleRunResult.DirectoryPath,
                        false, true, singleRunResult);
                    var task = new SingleRunRetentionTimeCalibrationTask(parameters);
                    task.Run();
                }
            }
        }

        [Test]
        public static void RunJenkinsLikeParserOnSingleRun()
        {
            string path = @"B:\Users\Nic\PEPTesting\240715_NB_NewPep_FracInt+Term+Intern_2377";

            var name = Path.GetFileName(path);
            var runDirectories = path.GetDirectories();

            var allMMResults = new List<SingleRunResults>();


            var semiSpecificDir = runDirectories.FirstOrDefault(p => p.Contains("Semispecific"));
            if (semiSpecificDir is not null)
                allMMResults.Add(new MetaMorpheusResult(semiSpecificDir, name, "Semi-Specific"));

            var nonspecificDir = runDirectories.FirstOrDefault(p => p.Contains("Nonspecific"));
            if (nonspecificDir is not null)
                allMMResults.Add(new MetaMorpheusResult(nonspecificDir, name, "Non-Specific"));

            var modernDir = runDirectories.FirstOrDefault(p => p.Contains("Modern") && !p.Contains("Open"));
            if (modernDir is not null)
                allMMResults.Add(new MetaMorpheusResult(modernDir, name, "Modern"));


            var classicDir = runDirectories.FirstOrDefault(p => p.Contains("Classic"));
            if (classicDir is not null)
            {
                var classicInitialDir = classicDir.GetDirectories().First(p => p.Contains("Task1"));
                allMMResults.Add(new MetaMorpheusResult(classicInitialDir, name, "Classic - Initial"));
                var classicPostCalibDir = classicDir.GetDirectories().First(p => p.Contains("Task3"));
                allMMResults.Add(new MetaMorpheusResult(classicPostCalibDir, name, "Classic - Post Calibration"));
                var classicPostGptmdDir = classicDir.GetDirectories().First(p => p.Contains("Task5"));
                allMMResults.Add(new MetaMorpheusResult(classicPostGptmdDir, name, "Classic - Post GPTMD"));
            }



            var topDownDir = runDirectories.FirstOrDefault(p => p.Contains("TopDown"));
            if (topDownDir is not null)
            {
                var tdInitialDir = topDownDir.GetDirectories().First(p => p.Contains("Task1"));
                allMMResults.Add(new MetaMorpheusResult(tdInitialDir, name, "TopDown - Initial"));
                var tdPostCalibDir = topDownDir.GetDirectories().First(p => p.Contains("Task3"));
                allMMResults.Add(new MetaMorpheusResult(tdPostCalibDir, name, "TopDown - Post Calibration"));
                var tdPostAveragingDir = topDownDir.GetDirectories().First(p => p.Contains("Task5"));
                allMMResults.Add(new MetaMorpheusResult(tdPostAveragingDir, name, "TopDown - Post Averaging"));
                var tdPostGPTMDDir = topDownDir.GetDirectories().First(p => p.Contains("Task7"));
                allMMResults.Add(new MetaMorpheusResult(tdPostGPTMDDir, name, "TopDown - Post GPTMD"));
            }

            var bottomupOpenModernDir = runDirectories.FirstOrDefault(p => p.Contains("BottomUpOpenModern"));
            if (bottomupOpenModernDir is not null)
                allMMResults.Add(new MetaMorpheusResult(bottomupOpenModernDir, name, "BottomUp OpenModern"));


            var topDownOpenModernDir = runDirectories.FirstOrDefault(p => p.Contains("TopDownOpenModern"));
            if (topDownOpenModernDir is not null)
                allMMResults.Add(new MetaMorpheusResult(topDownOpenModernDir, name, "TopDown OpenModern"));


            var run = new CellLineResults(path, allMMResults);

            foreach (var result in run)
            {
                var mm = (MetaMorpheusResult)result;
                mm.PlotPepFeaturesScatterGrid();
            }
        }

        [Test]
        public static void Overnighter()
        {
            InternalMetaMorpheusAnalysisTask.Version = "105";
            var path = BottomUpRunner.DirectoryPath;
            var dataDirectoryPath = InternalMetaMorpheusAnalysisTask.Mann11DataFileDirectory;
            var outputDir = InternalMetaMorpheusAnalysisTask.Mann11OutputDirectory;
            var dbPath = InternalMetaMorpheusAnalysisTask.UniprotHumanProteomeAndReviewedXml;

            var parameters = new InternalMetaMorpheusAnalysisParameters(path, outputDir, dataDirectoryPath, dbPath, @"C:\Program Files\MetaMorpheus");
            var task = new InternalMetaMorpheusAnalysisTask(parameters);
            task.Run().Wait();



            InternalMetaMorpheusAnalysisTask.Version = "106";
            path = TopDownRunner.DirectoryPath;
            dataDirectoryPath = InternalMetaMorpheusAnalysisTask.JurkatTopDownDataFileDirectory;
            outputDir = InternalMetaMorpheusAnalysisTask.JurkatTopDownOutputDirectory;
            dbPath = InternalMetaMorpheusAnalysisTask.UniprotHumanProteomeAndReviewedXml;

            parameters = new InternalMetaMorpheusAnalysisParameters(path, outputDir, dataDirectoryPath, dbPath, @"C:\Program Files\MetaMorpheus");
            task = new InternalMetaMorpheusAnalysisTask(parameters);
            task.Run().Wait();
        }


        [Test]
        public static void RunInternalMMComparison()
        {
            InternalMetaMorpheusAnalysisTask.Version = "114";
            var path = BottomUpRunner.DirectoryPath;
            var dataDirectoryPath = InternalMetaMorpheusAnalysisTask.Mann11DataFileDirectory;
            var outputDir = InternalMetaMorpheusAnalysisTask.Mann11OutputDirectory;
            var dbPath = InternalMetaMorpheusAnalysisTask.UniprotHumanProteomeAndReviewedXml;

            var parameters = new InternalMetaMorpheusAnalysisParameters(path, outputDir, dataDirectoryPath, dbPath, @"C:\Program Files\MetaMorpheus");
            var task = new InternalMetaMorpheusAnalysisTask(parameters);
            task.Run().Wait();
        }

        [Test]
        public static void RunInternalMMComparison_TopDown()
        {
            InternalMetaMorpheusAnalysisTask.Version = "114";
            var path = TopDownRunner.DirectoryPath;
            var dataDirectoryPath = InternalMetaMorpheusAnalysisTask.JurkatTopDownDataFileDirectory;
            var outputDir = InternalMetaMorpheusAnalysisTask.JurkatTopDownOutputDirectory;
            var dbPath = InternalMetaMorpheusAnalysisTask.UniprotHumanProteomeAndReviewedXml;

            var parameters = new InternalMetaMorpheusAnalysisParameters(path, outputDir, dataDirectoryPath, dbPath, @"C:\Program Files\MetaMorpheus");
            var task = new InternalMetaMorpheusAnalysisTask(parameters);
            task.Run().Wait();
        }

        [Test]
        public static void RunExternalComparison()
        {
            var path = BottomUpRunner.DirectoryPath;
            var dataDirectoryPath = InternalMetaMorpheusAnalysisTask.Mann11DataFileDirectory;
            var outputDir = ExternalComparisonTask.Mann11OutputDirectory;
            var dbPath = ExternalComparisonTask.UniprotHumanProteomeAndReviewedFasta;

            var parameters = new ExternalComparisonParameters(path, outputDir, dataDirectoryPath, dbPath, @"C:\Program Files\MetaMorpheus");
            var task = new ExternalComparisonTask(parameters);
            task.Run().Wait();
        }

        #region On the fly tests


        [Test]
        public static void IsolationWidthStudy()
        {
            string path = @"R:\Nic\IsolationWindowScaling\SearchResults\TD_AllTasks_1";
            string figureDir = @"R:\Nic\IsolationWindowScaling\Figures";

            bool isTopDown = !path.Contains("BU");
            string titleLeader = isTopDown ? "Top-Down" : "Bottom-Up";
            var mm = new MetaMorpheusResult(path, "Yeast", "IsolationWidth");
            var indFile = mm.GetIndividualFileComparison() ?? throw new NullReferenceException();
            int widthSq = 600;
            int heightSq = 600;

            var outName = $"{titleLeader}_{Path.GetFileNameWithoutExtension(path).Split('_')[1]}";


            var barChart = indFile.GetFileDelimitedPlotsForIsolationWidthStudy(ResultType.Psm, isTopDown, $"{titleLeader}");
            var outpath = Path.Combine(figureDir, $"{outName}_Psm");
            barChart.SavePNG(outpath, null, widthSq + 1 / 3 * widthSq, heightSq);

            var pepBarChart = indFile.GetFileDelimitedPlotsForIsolationWidthStudy(ResultType.Peptide, isTopDown, $"{titleLeader}");
            var pepOutpath = Path.Combine(figureDir, $"{outName}_Peptide");
            pepBarChart.SavePNG(pepOutpath, null, widthSq + 1 / 3 * widthSq, heightSq);

            var protBarChart = indFile.GetFileDelimitedPlotsForIsolationWidthStudy(ResultType.Protein, isTopDown, $"{titleLeader}");
            var protOutpath = Path.Combine(figureDir, $"{outName}_Protein");
            protBarChart.SavePNG(protOutpath, null, widthSq + 1 / 3 * widthSq, heightSq);

            //var breakdown = mm.GetChimeraBreakdownFile();

            //foreach (var fileGroupedBreakdown in breakdown.GroupBy(p => p.FileName))
            //{
            //    // Stacked Column
            //    if (true)
            //    {
            //        var plot = fileGroupedBreakdown.ToList().GetChimeraBreakdownStackedColumn_Scaled(ResultType.Psm)
            //            .WithSize(600, 600);
            //        var outPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_PSM_{fileGroupedBreakdown.Key}.png");
            //        plot.SavePNG(outPath, null, widthSq, heightSq);

            //        var plot2 = fileGroupedBreakdown.ToList().GetChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide)
            //            .WithSize(600, 600);
            //        var outPath2 = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_Peptide_{fileGroupedBreakdown.Key}.png");
            //        plot2.SavePNG(outPath2, null, widthSq, heightSq);
            //    }

            //    // Target Decoy
            //    if (true)
            //    {
            //        var psmTD = fileGroupedBreakdown.ToList()
            //            .GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, false, true, out int width);
            //        var psmTDPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_TD_Absolute_PSM_{fileGroupedBreakdown.Key}.png");
            //        psmTD.SavePNG(psmTDPath, null, widthSq, heightSq);

            //        var pepTD = fileGroupedBreakdown.ToList()
            //            .GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, false, true, out width);
            //        var pepTDPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_TD_Absolute_Peptide_{fileGroupedBreakdown.Key}.png");
            //        pepTD.SavePNG(pepTDPath, null, widthSq, heightSq);

            //        var psmTDNorm = fileGroupedBreakdown.ToList()
            //            .GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Psm, true, false, out width);
            //        var psmTDNormPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_TD_Normalized_PSM_{fileGroupedBreakdown.Key}.png");
            //        psmTDNorm.SavePNG(psmTDNormPath, null, widthSq, heightSq);

            //        var pepTDNorm = fileGroupedBreakdown.ToList()
            //            .GetChimeraBreakDownStackedColumn_TargetDecoy(ResultType.Peptide, true, false, out width);
            //        var pepTDNormPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_TD_Normalized_Peptide_{fileGroupedBreakdown.Key}.png");
            //        pepTDNorm.SavePNG(pepTDNormPath, null, widthSq, heightSq);
            //    }

            //    // Mass and Charge
            //    if (true)
            //    {
            //        var psmMassCharge = fileGroupedBreakdown.ToList()
            //            .GetChimeraBreakdownByMassAndCharge(ResultType.Psm, false);
            //        var psmMassPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_MassBreakdown_PSM_{fileGroupedBreakdown.Key}.png");
            //        psmMassCharge.Mass.SavePNG(psmMassPath, null, widthSq, heightSq);
            //        var psmChargePath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_ChargeBreakdown_PSM_{fileGroupedBreakdown.Key}.png");
            //        psmMassCharge.Charge.SavePNG(psmChargePath, null, widthSq, heightSq);

            //        var pepMassCharge = fileGroupedBreakdown.ToList()
            //            .GetChimeraBreakdownByMassAndCharge(ResultType.Peptide, false);
            //        var pepMassPath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_MassBreakdown_Peptide_{fileGroupedBreakdown.Key}.png");
            //        pepMassCharge.Mass.SavePNG(pepMassPath, null, widthSq, heightSq);
            //        var pepChargePath = Path.Combine(mm.FigureDirectory, $"ChimeraBreakdown_ChargeBreakdown_Peptide_{fileGroupedBreakdown.Key}.png");
            //        pepMassCharge.Charge.SavePNG(pepChargePath, null, widthSq, heightSq);
            //    }
            //}
        }


        //[Test]
        //public static void RunChronologer()
        //{
        //    var estimator = new ChronologerRetentionTimePredictor();
        //    var allResults = new AllResults(Man11AllResultsPath);
        //    // figures found at B:\Users\Nic\Chimeras\Mann_11cell_analysis\A549\Figures
        //    foreach (var cellLine in allResults)
        //    {
        //        var mm = (MetaMorpheusResult)cellLine.First(p => p is MetaMorpheusResult);
        //        var peptides = mm.AllPeptides;
        //        var results = peptides
        //            .Where(p => p is { PEP_QValue: <= 0.01, DecoyContamTarget: "T" }
        //                        && !p.FullSequence.Contains("Metal")
        //                        && !p.BaseSeq.Contains('U'))
        //            .Select(p => (p.FullSequence, p.RetentionTime, estimator.PredictRetentionTime(p.BaseSeq, p.FullSequence))).ToArray();

        //        var notDone = results.Where(p => p.Item3 is null).ToArray();
        //    }
        //}


        [Test]
        public static void PepChecker()
        {
            var version = 106;
            bool isTopDown = false;
            var label = isTopDown ? "TopDown" : "BottomUp";
            var path = @"B:\Users\Nic\Chimeras\InternalMMAnalysis\Mann_11cell_lines\A549\MetaMorpheusWithChimeras_106_ChimericLibrary_Rep3";

            var outDir = @"D:\Projects\Code Maintenance\105106PEPQuestions";
            var mmResult = new MetaMorpheusResult(path);


            var peptidePeps = mmResult.AllPeptides.Select(p => p.PEP).ToList();
            var peptideHist = GenericPlots.Histogram(peptidePeps, $"{version} {label} Peptide Histogram", "PEP", "Count");
            string outName = $"Histogram_{version}_{label}_Peptides";
            peptideHist.SaveJPG(Path.Combine(outDir, outName), null, 600, 400);


            var psmPeps = mmResult.AllPsms.Select(p => p.PEP).ToList();
            var psmHist = GenericPlots.Histogram(psmPeps, $"{version} {label} Psm Histogram", "PEP", "Count");
            outName = $"Histogram_{version}_{label}_Psms";
            psmHist.SaveJPG(Path.Combine(outDir, outName), null, 600, 400);


            var peptideKde = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(mmResult.AllPeptides.Where(p => !p.IsDecoy())
                    .Select(p => p.PEP).ToList(), "Targets", "PEP", "Density"),
                GenericPlots.KernelDensityPlot(mmResult.AllPeptides.Where(p => p.IsDecoy())
                    .Select(p => p.PEP).ToList(), "Decoys", "PEP", "Density"),
            }).WithTitle($"{version} {label} Peptides");
            outName = $"KDE_{version}_{label}_Peptides";
            peptideKde.SaveJPG(Path.Combine(outDir, outName), null, 600, 400);

            var psmKde = Chart.Combine(new[]
            {
                GenericPlots.KernelDensityPlot(mmResult.AllPsms.Where(p => !p.IsDecoy())
                    .Select(p => p.PEP).ToList(), "Targets", "PEP", "Density"),
                GenericPlots.KernelDensityPlot(mmResult.AllPsms.Where(p => p.IsDecoy())
                    .Select(p => p.PEP).ToList(), "Decoys", "PEP", "Density"),
            }).WithTitle($"{version} {label} Psms");
            outName = $"KDE_{version}_{label}_Psms";
            psmKde.SaveJPG(Path.Combine(outDir, outName), null, 600, 400);

        }




        #endregion


        [Test]
        public static void ChimerysRearanger()
        {
            string buBasePath = BottomUpRunner.DirectoryPath;
            var destinationDir = ExternalComparisonTask.Mann11OutputDirectory;

            foreach (var cellLineDir in Directory.GetDirectories(buBasePath)
                .Where(p => !p.Contains("Figure"))
                .Where(p => !p.Contains("Processed"))
                .Where(p => !p.Contains("Prosight")))
            {
                var dirName = Path.GetFileName(cellLineDir);
                var searchResultDir = Directory.GetDirectories(cellLineDir).First(p => p.Contains("SearchRes"));
                var chimerysDir = Directory.GetDirectories(searchResultDir).First(p => p.Contains("Chimerys2"));
                var chimerysFiles = Directory.GetFiles(chimerysDir).ToList();

                var groupedFiles = chimerysFiles.GroupBy(p => Path.GetFileName(p).Split('_')[0])
                    .ToDictionary(p => p.Key, p => p.ToList());

                foreach (var chimerysGroup in groupedFiles)
                {
                    var finalDestName = $"Chimerys_{chimerysGroup.Key}";
                    var finalDestDir = Path.Combine(destinationDir, dirName, finalDestName);
                    var localDestDir = Path.Combine(searchResultDir, finalDestName);

                    if (!Directory.Exists(finalDestDir))
                        Directory.CreateDirectory(finalDestDir);

                    foreach (var file in chimerysGroup.Value)
                    {
                        var finalDestPath = Path.Combine(finalDestDir, Path.GetFileName(file))
                            .Replace("__", "_");
                        if (!File.Exists(finalDestPath))
                            File.Copy(file, finalDestPath);

                        var originalSubbedPath = file.Replace("__", "_");
                        if (!File.Exists(originalSubbedPath))
                            File.Move(file, originalSubbedPath);
                    }
                    var dummyFile = Path.Combine(chimerysDir, "dummyReport.tdReport");
                    if (!File.Exists(dummyFile))
                        File.Create(dummyFile);
                }
            }
        }



        [Test]
        public static void EdwinTest()
        {
            var dbPath = @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";

            var proteins = ProteinDbLoader.LoadProteinXML(dbPath, true, DecoyType.Reverse, GlobalVariables.AllModsKnown, false, null, out _);
        }

        [Test]
        public static void ShortreedTest()
        {
            string path =
                @"C:\Users\Nic\Downloads\AllPSMs_FormattedForPercolator\AllPSMs_FormattedForPercolator_ReducedWithLabel.txt";

            using var sr = new StreamReader(path);
            _ = sr.ReadLine();
            _ = sr.ReadLine();
            List<HyperScoreMetric> results = new();
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                var split = line.Split('\t');


                bool isDecoy = split.First().Trim().Contains("-1");
                for (int i = 1; i < split.Length; i++)
                {
                    var score = double.Parse(split[i]);
                    if (score == 0.0)
                        continue;

                    var length = i;
                    results.Add(new HyperScoreMetric { Length = length, Score = score, IsDecoy = isDecoy });
                }
            }

            var targetViolin = Chart.Violin<int, double, string>(results.Where(p => !p.IsDecoy).Select(p => p.Length).ToArray(),
                                   results.Where(p => !p.IsDecoy).Select(p => p.Score).ToArray(), "Length vs HyperScore");

            var decoyViolin = Chart.Violin<int, double, string>(
                results.Where(p => p.IsDecoy).Select(p => p.Length).ToArray(),
                results.Where(p => p.IsDecoy).Select(p => p.Score).ToArray(), "Length vs HyperScore");

            var combinedViolin = Chart.Combine(new[] { targetViolin, decoyViolin })
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600)
                .WithXAxisStyle(Title.init("Peptide Length"))
                .WithYAxisStyle(Title.init("HyperScore"))
                .WithTitle("Peptide Length Vs Hyperscore - Target");
            combinedViolin.Show();


            var violin = Chart.Violin<int, double, string>(results.Select(p => p.Length).ToArray(),
                    results.Select(p => p.Score).ToArray(), "Length vs HyperScore")
                .WithLayout(PlotlyBase.DefaultLayout)
                .WithSize(1000, 600)
                .WithXAxisStyle(Title.init("Peptide Length"))
                .WithYAxisStyle(Title.init("HyperScore"))
                .WithTitle("Peptide Length Vs Hyperscore");

            violin.Show();


        }


        [Test]
        public static void MoveStuff()
        {
            var inDir = @"R:\Nic\ML_FDR_Data";
            var outDir = @"C:\Users\Nic\OneDrive - UW-Madison\MachineLearningFDR\Results\PaperDataRuns";

            var researchDriveDirs = Directory.GetDirectories(inDir)
                .ToList();

            foreach (var researchDir in researchDriveDirs)
            {
                var dirName = Path.GetFileName(researchDir);
                if (dirName is null)
                    continue;

                var oneDriveDir = Path.Combine(outDir, dirName);
                if (!Directory.Exists(oneDriveDir))
                    Directory.CreateDirectory(oneDriveDir);

                var runDirectories = Directory.GetDirectories(researchDir);
                foreach (var runDir in runDirectories)
                {
                    var runName = Path.GetFileName(runDir);
                    if (runName is null)
                        continue;
                    var oneDriveRunDir = Path.Combine(oneDriveDir, runName);
                    if (!Directory.Exists(oneDriveRunDir))
                        Directory.CreateDirectory(oneDriveRunDir);

                    var files = Directory.GetFiles(runDir);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        if (fileName is null)
                            continue;

                        var destPath = Path.Combine(oneDriveRunDir, fileName);
                        if (destPath.Contains("PRCurve"))
                            destPath = destPath.Replace("ModelPrediction", "PRCurve").Replace("_PRCurve", "");

                        if (destPath.Contains("Predict"))
                            continue;

                        if (!File.Exists(destPath))
                            File.Copy(file, destPath);
                    }
                }
            }

        }


        [Test]
        public static void SnipMzMl()
        {
            string origDataFile = @"B:\Users\Nic\RNA\Standards\BurkeStandards\20250612_RNA-Mix_10V.raw";
            int startScan = 2763;
            int endScan = 2765;
            FilteringParams filter = new FilteringParams(200, 0.01, 1, null, false, false, true);
            var reader = MsDataFileReader.GetDataFile(origDataFile);
            reader.LoadAllStaticData(filter, 1);

            var scans = reader.GetAllScansList();
            var scansToKeep = scans.Where(x => x.OneBasedScanNumber >= startScan && x.OneBasedScanNumber <= endScan).ToList();

            List<(int oneBasedScanNumber, int? oneBasedPrecursorScanNumber)> scanNumbers = new List<(int oneBasedScanNumber, int? oneBasedPrecursorScanNumber)>();
            foreach (var scan in scansToKeep)
            {
                if (scan.OneBasedPrecursorScanNumber.HasValue)
                {
                    scanNumbers.Add((scan.OneBasedScanNumber, scan.OneBasedPrecursorScanNumber.Value));
                }
                else
                {
                    scanNumbers.Add((scan.OneBasedScanNumber, null));
                }
            }

            Dictionary<int, int> scanNumberMap = new Dictionary<int, int>();

            foreach (var scanNumber in scanNumbers)
            {
                if (!scanNumberMap.ContainsKey(scanNumber.oneBasedScanNumber) && (scanNumber.oneBasedScanNumber - startScan + 1) > 0)
                {
                    scanNumberMap.Add(scanNumber.oneBasedScanNumber, scanNumber.oneBasedScanNumber - startScan + 1);
                }
                if (scanNumber.oneBasedPrecursorScanNumber.HasValue && !scanNumberMap.ContainsKey(scanNumber.oneBasedPrecursorScanNumber.Value) && (scanNumber.oneBasedPrecursorScanNumber.Value - startScan + 1) > 0)
                {
                    scanNumberMap.Add(scanNumber.oneBasedPrecursorScanNumber.Value, scanNumber.oneBasedPrecursorScanNumber.Value - startScan + 1);
                }
            }
            List<MsDataScan> scansForTheNewFile = new List<MsDataScan>();


            foreach (var scanNumber in scanNumbers)
            {
                MsDataScan scan = scansToKeep.First(x => x.OneBasedScanNumber == scanNumber.oneBasedScanNumber);

                int? newOneBasedPrecursorScanNumber = null;
                if (scan.OneBasedPrecursorScanNumber.HasValue && scanNumberMap.ContainsKey(scan.OneBasedPrecursorScanNumber.Value))
                {
                    newOneBasedPrecursorScanNumber = scanNumberMap[scan.OneBasedPrecursorScanNumber.Value];
                }
                MsDataScan newDataScan = new MsDataScan(
                    scan.MassSpectrum,
                    scanNumberMap[scan.OneBasedScanNumber],
                    scan.MsnOrder,
                    scan.IsCentroid,
                    scan.Polarity,
                    scan.RetentionTime,
                    scan.ScanWindowRange,
                    scan.ScanFilter,
                    scan.MzAnalyzer,
                    scan.TotalIonCurrent,
                    scan.InjectionTime,
                    scan.NoiseData,
                    scan.NativeId.Replace(scan.OneBasedScanNumber.ToString(), scanNumberMap[scan.OneBasedScanNumber].ToString()),
                    scan.SelectedIonMZ,
                    scan.SelectedIonChargeStateGuess,
                    scan.SelectedIonIntensity,
                    scan.IsolationMz,
                    scan.IsolationWidth,
                    scan.DissociationType,
                    newOneBasedPrecursorScanNumber,
                    scan.SelectedIonMonoisotopicGuessMz,
                    scan.HcdEnergy
                );
                scansForTheNewFile.Add(newDataScan);
            }

            string outPath = origDataFile.Replace(".raw", "_snip.mzML").ToString();

            SourceFile sourceFile = new SourceFile(reader.SourceFile.NativeIdFormat,
                reader.SourceFile.MassSpectrometerFileFormat, reader.SourceFile.CheckSum, reader.SourceFile.FileChecksumType, reader.SourceFile.Uri, reader.SourceFile.Id, reader.SourceFile.FileName);


            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(new GenericMsDataFile(scansForTheNewFile.ToArray(), sourceFile), outPath, false);

            Assert.IsTrue(false);
        }
        public record HyperScoreMetric
        {
            public bool IsDecoy { get; init; }
            public int Length { get; init; }
            public double Score { get; init; }
        }






    }
}
