using System.Diagnostics;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;
using Analyzer.Util;
using Plotly.NET;
using Plotly.NET.ImageExport;
using TaskLayer.CMD;
using Analyzer.FileTypes.Internal;
using Easy.Common.Extensions;
using Analyzer;
using Plotting;
using Plotting.Util;
using ResultAnalyzerUtil;
using Proteomics.PSM;

namespace TaskLayer.ChimeraAnalysis
{
    public class InternalMetaMorpheusAnalysisTask : BaseResultAnalyzerTask
    {
        #region FilePaths

        // Paths for setup
        internal static string MetaMorpheusLocation { get; set; }

        public static string UniprotHumanProteomeAndReviewedFasta =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";

        public static string UniprotHumanProteomeAndReviewedXml =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";

        public static string Mann11DataFileDirectory =>
            @"B:\RawSpectraFiles\Mann_11cell_lines";

        public static string Mann11OutputDirectory =>
            @"B:\Users\Nic\Chimeras\InternalMMAnalysis\Mann_11cell_lines";

        public static string JurkatTopDownDataFileDirectory =>
            @"B:\RawSpectraFiles\JurkatTopDown";

        public static string JurkatTopDownOutputDirectory =>
            @"B:\Users\Nic\Chimeras\InternalMMAnalysis\TopDown_Jurkat";

        // Bottom Up Tomls
        public static string GptmdNoChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMD_NoChimeras.toml";

        public static string GptmdWithChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMD_WithChimeras.toml";

        public static string SearchNoChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_NoChimeras.toml";

        public static string SearchWithChimerasMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_WithChimeras.toml";

        public static string SearchNoChimerasMann11_BuildLibrary =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_NoChimeras_BuildLibrary.toml";

        public static string SearchWithChimerasMann11_BuildLibrary =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_WithChimeras_BuildLibrary.toml";


        // Top-Down Tomls
        public static string Search_BuildChimericLibraryJurkatTd =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\Search_WithChimeras_BuildLibrary.toml";

        public static string Search_BuildNonChimericLibraryJurkatTd =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\Search_NoChimeras_BuildLibrary.toml";

        public static string GptmdNoChimerasJurkatTd =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\GPTMD_NoChimeras.toml";

        public static string GptmdWithChimerasJurkatTd =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\GPTMD_WithChimeras.toml";

        public static string SearchNoChimerasJurkatTd =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\Search_NoChimeras.toml";

        public static string SearchWithChimerasJurkatTd =>
            @"B:\Users\Nic\Chimeras\TopDown_Analysis\Jurkat\Search_WithChimeras.toml";


        #endregion

        public static string Version { get; set; } = "105";
        private static string NonChimericDescriptor => "MetaMorpheusNoChimeras";
        private static string ChimericDescriptor => "MetaMorpheusWithChimeras";
        private static string BulkFigureDirectory { get; set; }

        public override MyTask MyTask => MyTask.InternalMetaMorpheusAnalysis;
        public override InternalMetaMorpheusAnalysisParameters Parameters { get; }

        public InternalMetaMorpheusAnalysisTask(InternalMetaMorpheusAnalysisParameters parameters)
        {
            Parameters = parameters;
            MetaMorpheusLocation = parameters.MetaMorpheusPath;

            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);
            BulkFigureDirectory = Path.Combine(parameters.OutputDirectory, "Figures");
            BulkResultComparisonFilePath = Path.Combine(parameters.OutputDirectory, $"{Version}_BulkResultComparisonFile.csv");
            if (!Directory.Exists(BulkFigureDirectory))
                Directory.CreateDirectory(BulkFigureDirectory);
        }

        protected override void RunSpecific()
        {
            var processes = BuildProcesses(Parameters);
            RunProcesses(processes).Wait();

            // Parse Results Together
            Dictionary<string, List<MetaMorpheusResult>> cellLineDict = new();
            foreach (var cellLineDirectory in Directory.GetDirectories(Parameters.OutputDirectory)
                         .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
            {
                // Filter here if needed. 


                cellLineDict.Add(cellLineDirectory, new());
                foreach (var runDirectory in Directory.GetDirectories(cellLineDirectory).Where(p => !p.Contains("Figure") && p.Contains(Version)))
                {
                    //if (runDirectory.Contains("WithChimeras") && runDirectory.Contains("NonChimericLib"))
                    //    continue;
                    cellLineDict[cellLineDirectory].Add(new MetaMorpheusResult(runDirectory));
                }
            }
            cellLineDict = cellLineDict.SelectMany(p => p.Value)
                .GroupBy(p => p.Condition.ConvertConditionName())
                .ToDictionary(p => p.Key, p => p.ToList());


            bool isTopDown = cellLineDict.First().Value.First().IsTopDown;

            // Run MM Task basic processing 
            object plottingLock = new();
            int degreesOfParallelism = (int)(MaxWeight / 0.25);
            Parallel.ForEach(cellLineDict.SelectMany(p => p.Value),
                new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(degreesOfParallelism, 1) },
                mmResult =>
                {
                    Log($"Processing {mmResult.DatasetName} {mmResult.Condition}", 1);

                    Log($"Tabulating Result Counts: {mmResult.DatasetName} {mmResult.Condition}", 2);
                    _ = mmResult.GetIndividualFileComparison();
                    _ = mmResult.GetBulkResultCountComparisonFile();

                    Log($"Counting Chimeric Psms/Peptides: {mmResult.DatasetName} {mmResult.Condition}", 2);
                    mmResult.CountChimericPsms();
                    mmResult.CountChimericPeptides();

                    Log($"Running Chimera Breakdown Analysis: {mmResult.DatasetName} {mmResult.Condition}", 2);
                    var sw = Stopwatch.StartNew();
                    _ = mmResult.GetChimeraBreakdownFile();
                    sw.Stop();

                    // if it takes less than one minute to get the breakdown, and we are not overriding, do not plot
                    if (sw.Elapsed.Minutes < 1 && !Parameters.Override)
                        return;
                    lock (plottingLock)
                    {
                        mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                        mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
                    }
                });

            Log($"Running Chimeric Spectrum Summaries", 0);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);
                Log($"Processing Cell Line {cellLine}", 1);
                List<CmdProcess> summaryTasks = new();
                foreach (var mmResult in cellLineDictEntry.Value)
                {
                    var summaryParams =
                        new SingleRunAnalysisParameters(mmResult.DirectoryPath, Parameters.Override, false, mmResult);
                    var summaryTask = new SingleRunChimericSpectrumSummaryTask(summaryParams);
                    summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Chimeric Spectrum Summary", 0.5,
                        mmResult.DirectoryPath));
                }

                try
                {
                    RunProcesses(summaryTasks).Wait();
                }
                catch (Exception e)
                {
                    Warn($"Error Running Chimeric Spectrum Summary for {cellLine}: {e.Message}");
                }
            }

            //if (!isTopDown)
            //{
            //    Log($"Running Retention Time Plots", 0);
            //    foreach (var cellLineDictEntry in cellLineDict)
            //    {
            //        var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //        List<CmdProcess> summaryTasks = new();
            //        Log($"Processing Cell Line {cellLine}", 1);
            //        foreach (var mmResult in cellLineDictEntry.Value)
            //        {
            //            if (mmResult.ProcessedResultsDirectory.Contains(NonChimericDescriptor))
            //                continue;

            //            foreach (var distribPlotTypes in Enum.GetValues<DistributionPlotTypes>())
            //            {
            //                var summaryParams =
            //                    new SingleRunAnalysisParameters(mmResult.ProcessedResultsDirectory, parameters.Override, false, mmResult, distribPlotTypes);
            //                var summaryTask = new SingleRunChimeraRetentionTimeDistribution(summaryParams);
            //                summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Retention Time Plots", 0.25,
            //                    mmResult.ProcessedResultsDirectory));
            //            }
            //        }

            //        try
            //        {
            //            await RunProcesses(summaryTasks);
            //        }
            //        catch (Exception e)
            //        {
            //            Warn($"Error Running Retention Time Plots for {cellLine}: {e.Message}");
            //        }
            //    }
            //}

            Log($"Running Spectral Angle Comparisons", 0);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                List<CmdProcess> summaryTasks = new();
                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var mmResult in cellLineDictEntry.Value)
                {
                    if (mmResult.DirectoryPath.Contains(NonChimericDescriptor))
                        continue;
                    foreach (var distribPlotTypes in Enum.GetValues<DistributionPlotTypes>())
                    {
                        var summaryParams =
                            new SingleRunAnalysisParameters(mmResult.DirectoryPath, Parameters.Override, false, mmResult, distribPlotTypes);
                        var summaryTask = new SingleRunSpectralAngleComparisonTask(summaryParams);
                        summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Spectral Angle Comparisons", 0.25,
                            mmResult.DirectoryPath));
                    }
                }

                try
                {
                    RunProcesses(summaryTasks).Wait();
                }
                catch (Exception e)
                {
                    Warn($"Error Running Spectral Angle Comparisons for {cellLine}: {e.Message}");
                }
            }
            PlotSpectralAnglePlots(cellLineDict);


            Log($"Plotting Target Decoy Curves", 0);
            PlotFdrPlots(cellLineDict);


            //TODO: Change retention time alignment to operate on the grouped runs
            //Log($"Running Retention Time Alignment", 0);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    List<CmdProcess> summaryTasks = new();
            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var singleRunPath in cellLineDictEntry.Value)
            //    {
            //        if (singleRunPath.Contains(NonChimericDescriptor))
            //            continue;
            //        var summaryParams =
            //            new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
            //        var summaryTask = new SingleRunRetentionTimeCalibrationTask(summaryParams);
            //        summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Retention Time Alignment", 0.25,
            //            singleRunPath));
            //    }

            //    try
            //    {
            //        await RunProcesses(summaryTasks);
            //    }
            //    catch (Exception e)
            //    {
            //        Warn($"Error Running Retention Time Alignment for {cellLine}: {e.Message}");
            //    }
            //}

            //Log("Creating Proforma Files", 1);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var mmResult in cellLineDictEntry.Value)
            //    {
            //        if (mmResult.ProcessedResultsDirectory.Contains(NonChimericDescriptor))
            //            continue;

            //        mmResult.ToPsmProformaFile();
            //    }
            //}

            var proformaResultPath = Path.Combine(BulkFigureDirectory, "ProformaResults");
            //foreach (var condition in cellLineDict)
            //{
            //    var proforomaFileName = Path.Combine(proformaResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProformaFile);
            //    if (File.Exists(proforomaFileName) && !parameters.Override)
            //        continue;

            //    var records = new List<ProformaRecord>();
            //    foreach (var result in condition.Value)
            //        records.AddRange(result.ToPsmProformaFile().Results);
            //    var newFile = new ProformaFile(proforomaFileName)
            //    {
            //        Results = records
            //    };

            //    newFile.WriteResults(proforomaFileName);
            //}

            //Log("Creating Protein Counting Files", 1);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var mmResult in cellLineDictEntry.Value)
            //    {
            //        if (mmResult.ProcessedResultsDirectory.Contains(NonChimericDescriptor))
            //            continue;

            //        mmResult.CountProteins();
            //    }
            //}

            //foreach (var condition in cellLineDict)
            //{
            //    var proteinCountFileName = Path.Combine(proformaResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProteinCountingFile);

            //    if (File.Exists(proteinCountFileName) && !Parameters.Override)
            //        continue;

            //    var records = new List<ProteinCountingRecord>();
            //    foreach (var result in condition.Value)
            //        records.AddRange(result.CountProteins().Results);
            //    var newFile = new ProteinCountingFile(proteinCountFileName)
            //    {
            //        Results = records
            //    };

            //    newFile.WriteResults(proteinCountFileName);
            //}

            //var countingRecords = cellLineDict.SelectMany(p => p.Value.SelectMany(m => m.CountProteins().Results)).ToList();
            //ExternalComparisonTask.PlotProteinCountingCharts(countingRecords, isTopDown, BulkFigureDirectory);

            var resultsForInternalComparison = cellLineDict
                .SelectMany(p => p.Value.Take(1).ToList())
                .Where(p => !p.DirectoryPath.Contains($"{ChimericDescriptor}_{Version}_NonChimericLibrary"))
                .ToList();

            GetResultCountFile(resultsForInternalComparison);
            Log($"Plotting Bulk Internal Comparison", 0);
            PlotCellLineBarCharts(resultsForInternalComparison);

            resultsForInternalComparison = resultsForInternalComparison
                .Where(p => p.Condition.Contains($"{ChimericDescriptor}_{Version}_ChimericLibrary"))
                .ToList();

            PlotChimeraBreakdownBarChart(resultsForInternalComparison);
            PlotPossibleFeatures(resultsForInternalComparison);
            PlotFractionalIntensityPlots(resultsForInternalComparison);
        }

        #region MetaMorpheus search running

        static List<CmdProcess> BuildProcesses(InternalMetaMorpheusAnalysisParameters parameters)
        {
            List<CmdProcess> toReturn = new();
            var directoryPath = parameters.InputDirectoryPath;
            var dataDirectoryPath = parameters.SpectraFileDirectory;
            var dataset = Path.GetFileNameWithoutExtension(directoryPath);
            var isTopDown = !directoryPath.Contains("Mann");
            var manager = new DatasetFileManager(dataset, isTopDown, directoryPath, dataDirectoryPath);

            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);

            // Generate Libraries
            var replicateDict = manager.CellLines.SelectMany(p => p.Replicates)
                .GroupBy(p => p.Replicate)
                .ToDictionary(p => p.Key,
                    p =>
                        p.Select(m => (m.CalibratedAveragedFilePaths.Values, m.CalibratedAveragedSetPrecursorFilePaths.Values)).ToArray());

            var reps = replicateDict.Keys.ToArray();
            for (int i = 1; i < replicateDict.Count + 1; i++)
            {
                var allSpectralPaths = replicateDict.Where(p => p.Key != i)
                    .SelectMany(m => m.Value).ToArray();
                string descriptor = string.Join('+', reps.Where(p => p != i));

                // Generate Library Processes
                string chimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateChimericLibrary_{Version}_{descriptor}");
                string gptmd = GetGptmdPath(true, isTopDown);
                string search = GetSearchPath(true, isTopDown, true);
                string[] specToRun = allSpectralPaths.SelectMany(p => p.Item1).ToArray();
                var chimLibPrcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search, chimericOutPath,
                    $"Generating Chimeric Library for {descriptor} in {dataset}", 1, MetaMorpheusLocation);
                toReturn.Add(chimLibPrcess);

                string nonChimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateNonChimericLibrary_{Version}_{descriptor}");
                gptmd = GetGptmdPath(false, isTopDown);
                search = GetSearchPath(false, isTopDown, true);
                specToRun = isTopDown
                    ? allSpectralPaths.SelectMany(p => p.Item2).ToArray()
                    : allSpectralPaths.SelectMany(p => p.Item1).ToArray();
                var nonChimLibProcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search,
                    nonChimericOutPath,
                    $"Generating Non-Chimeric Library for {descriptor} in {dataset}", 1, MetaMorpheusLocation);
                toReturn.Add(nonChimLibProcess);

                foreach (var cellLine in manager.CellLines)
                {
                    string cellLineDir = Path.Combine(parameters.OutputDirectory, cellLine.CellLine);
                    if (!Directory.Exists(cellLineDir))
                        Directory.CreateDirectory(cellLineDir);

                    var allSpec = cellLine.Replicates
                        .Where(p => p.Replicate == i)
                        .Select(m => (m.CalibratedAveragedFilePaths.Values, m.CalibratedAveragedSetPrecursorFilePaths.Values))
                        .ToArray();

                    // Search Chimericly with chim lib
                    string chimWithChimOutPath = Path.Combine(cellLineDir,
                        $"{ChimericDescriptor}_{Version}_ChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    string[] spec = allSpec.SelectMany(p => p.Item1).ToArray();
                    var individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithChimOutPath,
                        $"Searching with Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.5,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(chimLibPrcess);
                    toReturn.Add(individualProcess);

                    // Search Chimerically with NonChimeric Lib
                    string chimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"{ChimericDescriptor}_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    spec = allSpec.SelectMany(p => p.Item1).ToArray();
                    individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithNonChimOutPath,
                        $"Searching Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.5,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);

                    // Search NonChimerically with NonChimeric Lib
                    string nonChimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"{NonChimericDescriptor}_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(false, isTopDown);
                    search = GetSearchPath(false, isTopDown, false);
                    spec = isTopDown
                        ? allSpec.SelectMany(p => p.Item2).ToArray()
                        : allSpec.SelectMany(p => p.Item1).ToArray();
                    individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        nonChimWithNonChimOutPath,
                        $"Searching Non-Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.5,
                        MetaMorpheusLocation);
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);
                }
            }

            return toReturn.OrderByDescending(p => p.Weight).ToList();
        }

        static string GetGptmdPath(bool isChimeric, bool isTopDown)
        {
            return isTopDown switch
            {
                true => isChimeric ? GptmdWithChimerasJurkatTd : GptmdNoChimerasJurkatTd,
                false => isChimeric ? GptmdWithChimerasMann11 : GptmdNoChimerasMann11
            };
        }

        static string GetSearchPath(bool isChimeric, bool isTopDown, bool buildLibrary)
        {
            if (isTopDown)
            {
                return buildLibrary switch
                {
                    true => isChimeric ? Search_BuildChimericLibraryJurkatTd : Search_BuildNonChimericLibraryJurkatTd,
                    false => isChimeric ? SearchWithChimerasJurkatTd : SearchNoChimerasJurkatTd
                };
            }
            else
            {
                return buildLibrary switch
                {
                    true => isChimeric ? SearchWithChimerasMann11_BuildLibrary : SearchNoChimerasMann11_BuildLibrary,
                    false => isChimeric ? SearchWithChimerasMann11 : SearchNoChimerasMann11
                };
            }
        }

        #endregion

        #region Result File Creation

        private static string BulkResultComparisonFilePath { get; set; }
        private static BulkResultCountComparisonFile? _bulkResultCountComparisonFile;

        internal static BulkResultCountComparisonFile GetResultCountFile(List<MetaMorpheusResult> mmResults)
        {
            Log($"Counting Total Results", 0);
            if (_bulkResultCountComparisonFile != null)
                return _bulkResultCountComparisonFile;
            if (File.Exists(BulkResultComparisonFilePath))
            {
                _bulkResultCountComparisonFile = new BulkResultCountComparisonFile(BulkResultComparisonFilePath);
                _bulkResultCountComparisonFile.LoadResults();
                return _bulkResultCountComparisonFile;
            }

            bool isTopDown = mmResults.First().IsTopDown;
            List<BulkResultCountComparison> allResults = new();
            foreach (var conditionGroup in mmResults.GroupBy(p => p.Condition.ConvertConditionName()))
            {
                string condition = conditionGroup.Key;
                foreach (var cellLineGroup in conditionGroup.GroupBy(p => p.DatasetName))
                {
                    string cellLine = cellLineGroup.Key;

                    if (cellLineGroup.Count() != 3 && !isTopDown)
                        Debugger.Break();
                    if (cellLineGroup.Count() != 2 && isTopDown)
                        Debugger.Break();

                    int psmCount = cellLineGroup.Sum(p =>
                        p.AllPsms.Count(psm => !psm.IsDecoy() && psm.PEP_QValue <= 0.01));
                    int allPsmCount = cellLineGroup.Sum(p => p.AllPsms.Count(psm => !psm.IsDecoy()));

                    int peptideCount = cellLineGroup.SelectMany(p =>
                            p.AllPeptides.Where(peptide => !peptide.IsDecoy() && peptide.PEP_QValue <= 0.01))
                        .DistinctBy(peptide => peptide.FullSequence)
                        .Count();
                    int allPeptideCount = cellLineGroup.SelectMany(p => p.AllPeptides.Where(peptide => !peptide.IsDecoy()))
                        .DistinctBy(peptide => peptide.FullSequence)
                        .Count();

                    List<string> accessions = new();
                    List<string> unfilteredAccessions = new();
                    cellLineGroup.ForEach(group =>
                    {
                        using (var sw = new StreamReader(File.OpenRead(group.ProteinPath)))
                        {
                            var header = sw.ReadLine();
                            var headerSplit = header.Split('\t');
                            var qValueIndex = Array.IndexOf(headerSplit, "Protein QValue");
                            var decoyContamTargetIndex = Array.IndexOf(headerSplit, "Protein Decoy/Contaminant/Target");
                            var accessionIndex = Array.IndexOf(headerSplit, "Protein Accession");

                            while (!sw.EndOfStream)
                            {
                                var line = sw.ReadLine();
                                var values = line.Split('\t');

                                if (values[decoyContamTargetIndex].Contains('D'))
                                    continue;

                                var internalAccessions = values[accessionIndex].Split('|')
                                    .Select(p => p.Trim()).ToArray();
                                unfilteredAccessions.AddRange(internalAccessions);

                                if (double.Parse(values[qValueIndex]) > 0.01)
                                    continue;


                                accessions.AddRange(internalAccessions);
                            }
                        }
                    });

                    var proteinGroupCount = accessions.Distinct().Count();
                    var allProteinGroupCount = unfilteredAccessions.Distinct().Count();

                    var result = new BulkResultCountComparison()
                    {
                        DatasetName = cellLine,
                        Condition = condition,
                        FileName = "All 3 Replicates",
                        OnePercentPeptideCount = peptideCount,
                        OnePercentPsmCount = psmCount,
                        OnePercentProteinGroupCount = proteinGroupCount,
                        PsmCount = allPsmCount,
                        PeptideCount = allPeptideCount,
                        ProteinGroupCount = allProteinGroupCount
                    };

                    allResults.Add(result);
                    //cellLineGroup.ForEach(p => p.Dispose());
                }
            }

            var file = new BulkResultCountComparisonFile(BulkResultComparisonFilePath) { Results = allResults };
            file.WriteResults(BulkResultComparisonFilePath);
            return _bulkResultCountComparisonFile = file;
        }

        #endregion

        #region Plotting


        static void PlotCellLineBarCharts(List<MetaMorpheusResult> results)
        {
            Log($"Cell line bar charts", 1);
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = GetResultCountFile(results)
                .ToList();

            var labels = results.Select(p => p.DatasetName).Distinct().ConvertConditionNames().ToList();
            List<GenericChart.GenericChart> psmCharts = new();
            List<GenericChart.GenericChart> peptideCharts = new();
            List<GenericChart.GenericChart> proteinCharts = new();
            foreach (var condition in results.Select(p => p.Condition).ConvertConditionNames().Distinct())
            {
                var conditionSpecificResults = resultsToPlot
                    .Where(p => p.Condition.ConvertConditionName() == condition)
                    .ToList();

                //(string CellLine, int OnePercentPsmCount, int OnePercentPeptideCount, int OnePercentProteinGroupCount)[] extractedCounts = conditionSpecificResults
                //    .GroupBy(p => p.DatasetName)
                //    .Select(p => (p.Key, p.Sum(m => m.OnePercentPsmCount),
                //            p.Sum(m => m.OnePercentPeptideCount), p.Sum(m => m.OnePercentProteinGroupCount)))
                //        .ToArray();


                psmCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(m => m.OnePercentPsmCount), labels, null, condition,
                    MarkerColor: condition.ConvertConditionToColor(),
                    MultiText: conditionSpecificResults.Select(m => m.OnePercentPsmCount).Select(p => p.ToString()).ToArray()));

                peptideCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(m => m.OnePercentPeptideCount), labels, null, condition,
                    MarkerColor: condition.ConvertConditionToColor(),
                    MultiText: conditionSpecificResults.Select(m => m.OnePercentPeptideCount).Select(p => p.ToString()).ToArray()));

                proteinCharts.Add(Chart2D.Chart.Column<int, string, string, int, int>(
                    conditionSpecificResults.Select(m => m.OnePercentProteinGroupCount), labels, null, condition,
                    MarkerColor: condition.ConvertConditionToColor(),
                    MultiText: conditionSpecificResults.Select(m => m.OnePercentProteinGroupCount).Select(p => p.ToString()).ToArray()));
            }

            var psmPlot = Chart.Combine(psmCharts)
                .WithTitle($"1% FDR {Labels.GetLabel(isTopDown, ResultType.Psm)}", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithXAxisStyle(Title.init("Cell Line", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            //var psmPlot = GenericPlots.BulkResultBarChart(resultsToPlot, isTopDown, ResultType.Psm);
            var psmOutName = Path.Combine(BulkFigureDirectory, $"{Version}_InternalComparison_Psm");
            psmPlot.SaveJPG(psmOutName, null, 1000, 800);

            var peptidePlot = Chart.Combine(peptideCharts)
                .WithTitle($"1% FDR {Labels.GetLabel(isTopDown, ResultType.Peptide)}", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithXAxisStyle(Title.init("Cell Line", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            //var peptidePlot = GenericPlots.BulkResultBarChart(resultsToPlot, isTopDown, ResultType.Peptide);
            var peptideOutName = Path.Combine(BulkFigureDirectory, $"{Version}_InternalComparison_Peptide");
            peptidePlot.SaveJPG(peptideOutName, null, 1000, 800);

            var proteinPlot = Chart.Combine(proteinCharts)
                .WithTitle($"1% FDR {Labels.GetLabel(isTopDown, ResultType.Protein)}", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithXAxisStyle(Title.init("Cell Line", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            //var proteinPlot = GenericPlots.BulkResultBarChart(resultsToPlot, isTopDown, ResultType.Protein);
            var proteinOutName = Path.Combine(BulkFigureDirectory, $"{Version}_InternalComparison_Protein");
            proteinPlot.SaveJPG(proteinOutName, null, 1000, 800);
        }

        static void PlotChimeraBreakdownBarChart(List<MetaMorpheusResult> results)
        {
            Log($"Chimera Breakdown", 1);
            string tempTitleLeader = "E.Coli ";
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = results.SelectMany(p => p.ChimeraBreakdownFile)
                .ToList();

            var psmPlot = resultsToPlot.GetChimeraBreakdownStackedColumn_Scaled(ResultType.Psm, isTopDown)
                .WithTitle($"{tempTitleLeader}Chimera Spectra Composition (1% {Labels.GetSpectrumMatchLabel(isTopDown)})", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize));
            //var psmOutPath = Path.Combine(BulkFigureDirectory, $"{Version}_ChimeraBreakdown_Absolute_Psm");
            var psmOutPath = Path.Combine(BulkFigureDirectory, $"{Version}_ChimeraBreakdown_Psm");
            psmPlot.SaveJPG(psmOutPath, null, 1000, 800);

            var peptidePlot = resultsToPlot.GetChimeraBreakdownStackedColumn_Scaled(ResultType.Peptide, isTopDown)
                .WithTitle($"{tempTitleLeader}Chimera Spectra Composition (1% {Labels.GetPeptideLabel(isTopDown)})", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize));
            //var peptideOutPath = Path.Combine(BulkFigureDirectory, $"{Version}_ChimeraBreakdown_Absolute_Peptide");
            var peptideOutPath = Path.Combine(BulkFigureDirectory, $"{Version}_ChimeraBreakdown_Peptide");
            peptidePlot.SaveJPG(peptideOutPath, null, 1000, 800);
        }

        static void PlotPossibleFeatures(List<MetaMorpheusResult> results)
        {
            Log($"Possible Features", 1);
            var noIdString = "No ID";
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = results.SelectMany(p => p.ChimericSpectrumSummaryFile)
                .Where(p =>
                    p.PEP_QValue <= 0.01
                    && p.PossibleFeatureCount != 0
                    && p.Type != noIdString
                    && p is not { PrecursorCharge: 1, PossibleFeatureCount: 1 })
                .ToList();

            var resultType = ResultType.Psm;
            var records = resultsToPlot.Where(p => p.Type == resultType.ToString()).ToList();
            var chimeric = records.Where(p => p.IsChimeric).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric).ToList();


            (double, double)? minMax = isTopDown ? (0.0, 50.0) : (0.0, 15.0);
            var chimericHist = GenericPlots.Histogram(chimeric.Select(p =>
                (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            var nonChimericHist = GenericPlots.Histogram(nonChimeric.Select(p =>
                (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            var hist = Chart.Combine(new List<GenericChart.GenericChart> { chimericHist, nonChimericHist })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Features Per Isolation Window", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            var outname = $"{Version}_SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            hist.SaveJPG(Path.Combine(BulkFigureDirectory, outname), null, 800, 600);





            resultType = ResultType.Peptide;
            records = resultsToPlot.Where(p => p.Type == resultType.ToString()).ToList();
            chimeric = records.Where(p => p.IsChimeric).ToList();
            nonChimeric = records.Where(p => !p.IsChimeric).ToList();

            minMax = null;

            chimericHist = GenericPlots.Histogram(chimeric.Select(p =>
                    (double)p.PossibleFeatureCount).ToList(), "Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            nonChimericHist = GenericPlots.Histogram(nonChimeric.Select(p =>
                    (double)p.PossibleFeatureCount).ToList(), "Non-Chimeric ID", "Features per Isolation Window", "Number of Spectra", false, minMax);
            hist = Chart.Combine(new List<GenericChart.GenericChart> { chimericHist, nonChimericHist })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Features Per Isolation Window", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            outname = $"{Version}_SpectrumSummary_FeatureCount_{Labels.GetLabel(isTopDown, resultType)}_Histogram";
            hist.SaveJPG(Path.Combine(BulkFigureDirectory, outname), null, 800, 600);
        }

        static void PlotFractionalIntensityPlots(List<MetaMorpheusResult> results)
        {
            Log($"Fractional Intensity", 1);
            bool isTopDown = results.First().IsTopDown;
            var resultsToPlot = results.SelectMany(p => p.ChimericSpectrumSummaryFile)
                .Where(p =>
                    p.PEP_QValue <= 0.01
                    && p.Type != "No ID")
                .ToList();

            GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, true, true, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, true, false, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, true, true, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, true, false, isTopDown);

            //GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, false, false, isTopDown);
            GenerateFractionalIntensityPlots(ResultType.Psm, resultsToPlot, false, true, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, false, false, isTopDown);
            //GenerateFractionalIntensityPlots(ResultType.Peptide, resultsToPlot, false, true, isTopDown);
        }

        static void GenerateFractionalIntensityPlots(ResultType resultType,
            List<ChimericSpectrumSummary> summaryRecords, bool isPrecursor, bool sumPrecursor, bool isTopDown = false)
        {
            var records = summaryRecords.Where(p => p.Type == resultType.ToString()).ToList();
            var chimeric = records.Where(p => p.IsChimeric).ToList();
            var nonChimeric = records.Where(p => !p.IsChimeric).ToList();

            var chimericFractionalIntensity = (sumPrecursor
                ? isPrecursor
                    ? chimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : chimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : chimeric.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList()).Select(p => p * 100 / 1).ToList();
            var nonChimericFractionalIntensity = (sumPrecursor
                ? isPrecursor
                    ? nonChimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.PrecursorFractionalIntensity).Sum())
                        .Values.ToList()
                    : nonChimeric.GroupBy(p => p,
                            new CustomComparer<ChimericSpectrumSummary>(result => result.Ms2ScanNumber,
                                result => result.FileName))
                        .ToDictionary(p => p.Key, p => p.Select(b => b.FragmentFractionalIntensity).Sum())
                        .Values.ToList()
                : nonChimeric.Select(p => isPrecursor ? p.PrecursorFractionalIntensity : p.FragmentFractionalIntensity)
                    .ToList()).Select(p => p * 100 / 1).ToList();

            (double, double) minMax = isTopDown switch
            {
                true when sumPrecursor && isPrecursor => (0.0, 100.0),
                true when sumPrecursor && !isPrecursor => (0.0, 80.0),
                true when !sumPrecursor && isPrecursor => (0.0, 30.0),
                true when !sumPrecursor && !isPrecursor => (0.0, 25.0),

                false when sumPrecursor && isPrecursor => (0.0, 100.0),
                false when sumPrecursor && !isPrecursor => (0.0, 60.0),
                false when !sumPrecursor && isPrecursor => (0.0, 100.0),
                false when !sumPrecursor && !isPrecursor => (0.0, 60.0),

                _ => (0.0, 100.0)
            };


            var label = /*isPrecursor ? sumPrecursor ?*/ "Percent of Signal Identified" /*: "Precursor ID Fractional Intensity" : "Fragment Fractional Intensity"*/;
            var titleEnd = sumPrecursor
                ? isPrecursor ? "Per Isolation Window" : "Per MS2"
                : "Per ID";
            var outPrecursor = isPrecursor ? "Precursor" : "Fragment";
            var outType = sumPrecursor ? "Summed" : "Independent";

            var chimericHist = GenericPlots.Histogram(chimericFractionalIntensity, "Chimeric ID", label, "Number of Spectra", false, minMax);
            var nonChimericHist = GenericPlots.Histogram(nonChimericFractionalIntensity, "Non-Chimeric ID", label, "Number of Spectra", false, minMax);
            var hist = Chart.Combine(new List<GenericChart.GenericChart> { chimericHist, nonChimericHist })
                .WithTitle($"1% {Labels.GetLabel(isTopDown, resultType)} Identified {outPrecursor} Intensity {titleEnd}", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                .WithYAxisStyle(Title.init("Number of Spectra", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)), Side: StyleParam.Side.Left,
                    Id: StyleParam.SubPlotId.NewYAxis(1))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
            var outName = $"SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_Histogram";

            outName =
                $"{Version}_SpectrumSummary_{outPrecursor}FractionalIntensity_{outType}_{Labels.GetLabel(isTopDown, resultType)}_Combined";
            hist.SavePNG(Path.Combine(BulkFigureDirectory, outName), null, 800, 600);
        }

        static void PlotFdrPlots(Dictionary<string, List<MetaMorpheusResult>> allResults)
        {
            foreach (var conditionGroup in allResults)
            {
                if (conditionGroup.Key.Contains(NonChimericDescriptor))
                    continue;
                if (conditionGroup.Key.Contains("Non-chimeric")) continue;
                if (conditionGroup.Key.Contains("No Chimeras")) continue;

                bool isTopDown = conditionGroup.Value.First().IsTopDown;
                var figDir = Path.Combine(BulkFigureDirectory, conditionGroup.Key);
                if (!Directory.Exists(figDir))
                    Directory.CreateDirectory(figDir);

                var name = "";
                var condition = conditionGroup.Key;
                var psms = conditionGroup.Value.SelectMany(mm => mm.AllPsms).ToList();


                var chart = psms.GetTargetDecoyCurve(ResultType.Psm, TargetDecoyCurveMode.Score, name, condition, isTopDown);
                var outName = $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Psm)}_{FileIdentifiers.TargetDecoyCurve}";
                var outPath = Path.Combine(figDir, outName);
                chart.SaveJPG(outPath, null, 1000, 800);

                var chimStratChart = psms.GetTargetDecoyCurveChimeraStratified(ResultType.Psm, TargetDecoyCurveMode.Score, name, condition, isTopDown);
                outName = $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Psm)}_{FileIdentifiers.TargetDecoyCurve}_ChimeraStratified";
                outPath = Path.Combine(figDir, outName);
                chimStratChart.SaveJPG(outPath, null, 1000, 800);
            }
        }

        static void PlotSpectralAnglePlots(Dictionary<string, List<MetaMorpheusResult>> allResults)
        {
            foreach (var conditionGroup in allResults)
            {
                if (conditionGroup.Key.Contains("Non-chimeric")) continue;
                if (conditionGroup.Key.Contains("No Chimeras")) continue;

                bool isTopDown = conditionGroup.Value.First().IsTopDown;
                var figDir = Path.Combine(BulkFigureDirectory, conditionGroup.Key);
                if (!Directory.Exists(figDir))
                    Directory.CreateDirectory(figDir);

                var psmChimericAngles = new List<double>();
                var psmNonChimericAngles = new List<double>();
                var peptideChimericAngles = new List<double>();
                var peptideNonChimericAngles = new List<double>();

                foreach (var run in conditionGroup.Value)
                {
                    // Peptides
                    foreach (var chimeraGroup in run.AllPeptides.Where(p => p.PassesConfidenceFilter())
                                 .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
                    {
                        var filtered = chimeraGroup
                            .Where(p => !p.SpectralAngle.Equals(-1.0) && !p.SpectralAngle.Equals(double.NaN) &&
                                        !p.SpectralAngle.Equals(null)).ToArray();

                        if (!filtered.Any()) continue;
                        if (chimeraGroup.Count() == 1)
                        {
                            var first = filtered.First();
                            if (first.SpectralAngle.HasValue)
                                peptideNonChimericAngles.Add(first.SpectralAngle!.Value);
                        }
                        else
                        {
                            peptideChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                        }
                    }

                    // Psms
                    foreach (var chimeraGroup in run.AllPsms.Where(p => p.PassesConfidenceFilter())
                                 .GroupBy(p => p, CustomComparer<PsmFromTsv>.ChimeraComparer))
                    {
                        var filtered = chimeraGroup.Where(p =>
                            !p.SpectralAngle.Equals(-1.0) && !p.SpectralAngle.Equals(double.NaN) &&
                            !p.SpectralAngle.Equals(null)).ToArray();

                        if (!filtered.Any()) continue;
                        if (chimeraGroup.Count() == 1)
                        {
                            psmNonChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                        }
                        else
                        {
                            psmChimericAngles.AddRange(filtered.Select(p => p.SpectralAngle!.Value));
                        }
                    }
                }

                //var peptidePlot = AnalyzerGenericPlots.SpectralAngleChimeraComparisonViolinPlot(
                //        peptideChimericAngles.ToArray(), peptideNonChimericAngles.ToArray(),
                //        "", isTopDown, ResultType.Peptide)
                //    .WithTitle(
                //        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Peptide)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                //    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                //var outName =
                //    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_ViolinPlot";
                //var outPath = Path.Combine(figDir, outName);
                //peptidePlot.SaveJPG(outPath, null, 1000, 800);

                var peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.BoxPlot(peptideChimericAngles, "Chimeric", "", "Spectral Angle"),
                        GenericPlots.BoxPlot(peptideNonChimericAngles, "Non-Chimeric", "", "Spectral Angle")
                    })
                    .WithTitle(
                        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Peptide)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                var outName =
                    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_BoxPlot";
                var outPath = Path.Combine(figDir, outName);
                peptidePlot.SaveJPG(outPath, null, 1000, 800);

                //peptidePlot = Chart.Combine(new[]
                //    {
                //        GenericPlots.KernelDensityPlot(peptideChimericAngles, "Chimeric", "Spectral Angle", "Density",
                //            0.02),
                //        GenericPlots.KernelDensityPlot(peptideNonChimericAngles, "Non-Chimeric", "Spectral Angle",
                //            "Density", 0.02)
                //    })
                //    .WithTitle(
                //        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Peptide)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                //    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                //outName =
                //    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_KernelDensity";
                //outPath = Path.Combine(figDir, outName);
                //peptidePlot.SaveJPG(outPath, null, 1000, 800);

                peptidePlot = Chart.Combine(new[]
                    {
                        GenericPlots.Histogram(peptideChimericAngles, "Chimeric", "Spectral Angle", "Count"),
                        GenericPlots.Histogram(peptideNonChimericAngles, "Non-Chimeric", "Spectral Angle", "Count")
                    })
                    .WithTitle(
                        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Peptide)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                outName =
                    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Peptide)}_{FileIdentifiers.SpectralAngleFigure}_Histogram";
                outPath = Path.Combine(figDir, outName);
                peptidePlot.SaveJPG(outPath, null, 1000, 800);


                //var psmPlot = AnalyzerGenericPlots.SpectralAngleChimeraComparisonViolinPlot(psmChimericAngles.ToArray(),
                //        psmNonChimericAngles.ToArray(),
                //        "", isTopDown, ResultType.Psm)
                //    .WithTitle(
                //        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Psm)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                //    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                //outName =
                //    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_ViolinPlot";
                //outPath = Path.Combine(figDir, outName);
                //peptidePlot.SaveJPG(outPath, null, 1000, 800);

                var psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.BoxPlot(psmChimericAngles, "Chimeric", "", "Spectral Angle", false),
                        GenericPlots.BoxPlot(psmNonChimericAngles, "Non-Chimeric", "", "Spectral Angle", false)
                    })
                    .WithTitle(
                        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Psm)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                outName =
                    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_BoxPlot";
                outPath = Path.Combine(figDir, outName);
                psmPlot.SaveJPG(outPath, null, 1000, 800);

                //psmPlot = Chart.Combine(new[]
                //    {
                //        GenericPlots.KernelDensityPlot(psmChimericAngles, "Chimeric", "Spectral Angle", "Density",
                //            0.02),
                //        GenericPlots.KernelDensityPlot(psmNonChimericAngles, "Non-Chimeric", "Spectral Angle",
                //            "Density", 0.02)
                //    })
                //    .WithTitle(
                //        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Psm)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                //    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                //outName =
                //    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_KernelDensity";
                //outPath = Path.Combine(figDir, outName);
                //psmPlot.SaveJPG(outPath, null, 1000, 800);

                psmPlot = Chart.Combine(new[]
                    {
                        GenericPlots.Histogram(peptideChimericAngles, "Chimeric", "Spectral Angle", "Count"),
                        GenericPlots.Histogram(psmNonChimericAngles, "Non-Chimeric", "Spectral Angle", "Count")
                    })
                    .WithTitle(
                        $"MetaMorpheus 1% {Labels.GetLabel(isTopDown, ResultType.Psm)} Spectral Angle Distribution", Plotly.NET.Font.init(Size: PlotlyBase.TitleSize))
                    .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText);
                outName =
                    $"FdrAnalysis_{Labels.GetLabel(isTopDown, ResultType.Psm)}_{FileIdentifiers.SpectralAngleFigure}_Histogram";
                outPath = Path.Combine(figDir, outName);
                psmPlot.SaveJPG(outPath, null, 1000, 800);
            }
        }
        #endregion
    }
}
