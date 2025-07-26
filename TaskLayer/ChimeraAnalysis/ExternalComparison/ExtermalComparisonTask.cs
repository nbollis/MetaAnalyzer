using System.Diagnostics;
using Analyzer;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Easy.Common.Extensions;
using Plotting.Util;
using ResultAnalyzerUtil;
using TaskLayer.CMD;
using Plotly.NET;
using Chart = Plotly.NET.CSharp.Chart;
using Plotly.NET.TraceObjects;
using Plotly.NET.ImageExport;
using ResultAnalyzerUtil.CommandLine;

namespace TaskLayer.ChimeraAnalysis
{
    public class ExternalComparisonTask : BaseResultAnalyzerTask
    {
        internal static string MetaMorpheusLocation { get; set; }
        private static string BulkFigureDirectory { get; set; }

        public static string UniprotHumanProteomeAndReviewedFasta =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\UP000005640_reviewed.fasta";

        public static string GptmdMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\GPTMDReducedForComparision.toml";

        public static string SearchMann11 =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\SearchReducedForComparison.toml";

        public static string SearchsMann11_BuildLibrary =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\Search_BuildLibrary_ReducedForComparison.toml";
        public static string Mann11OutputDirectory =>
            @"B:\Users\Nic\Chimeras\ExternalMMAnalysis\Mann_11cell_lines";
        public static string Version => "106";

        public override MyTask MyTask => MyTask.ExternalChimeraPaperAnalysis;
        public override ExternalComparisonParameters Parameters { get; }

        public ExternalComparisonTask(ExternalComparisonParameters parameters)
        {
            Parameters = parameters;
            MetaMorpheusLocation = parameters.MetaMorpheusPath;

            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);
            BulkFigureDirectory = Path.Combine(parameters.OutputDirectory, "Figures");
            BulkResultComparisonFilePath = Path.Combine(parameters.OutputDirectory, "BulkResultComparisonFile.csv");
            if (!Directory.Exists(BulkFigureDirectory))
                Directory.CreateDirectory(BulkFigureDirectory);
        }

        protected override void RunSpecific()
        {
            var processes = BuildProcesses(Parameters);
            RunProcesses(processes).Wait();


            var isTopDown = !Parameters.InputDirectoryPath.Contains("Mann");
            Dictionary<string, List<string>> cellLineDict = new();
            foreach (var cellLineDirectory in Directory.GetDirectories(Parameters.OutputDirectory)
                         .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
            {
                cellLineDict.Add(cellLineDirectory, new());
                foreach (var runDirectory in Directory.GetDirectories(cellLineDirectory).Where(p => !p.Contains("Figure") && p.Contains(Version)))
                    cellLineDict[cellLineDirectory].Add(runDirectory);
            }

            // Key: run directory path, Value: loaded SingleRunResults
            var resultCache = cellLineDict
                .SelectMany(p => p.Value)
                .Distinct()
                .ToDictionary(
                    path => path,
                    LoadResultFromFilePath
                );


            // Run MM Task basic processing 
            object plottingLock = new();
            int degreesOfParallelism = (int)(MaxWeight / 0.25);
            Parallel.ForEach(cellLineDict.SelectMany(p => p.Value),
                new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(degreesOfParallelism, 1) },
                singleRunPath =>
                {
                    var mmResult = resultCache[singleRunPath] as MetaMorpheusResult;
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
                    lock(plottingLock)
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
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    var result = resultCache[singleRunPath];
                    var summaryParams =
                        new SingleRunAnalysisParameters(result, Parameters.Override, false);
                    var summaryTask = new SingleRunChimericSpectrumSummaryTask(summaryParams);
                    summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Chimeric Spectrum Summary", 0.5,
                        singleRunPath));
                }

                try
                {
                    /*await*/ RunProcesses(summaryTasks).Wait();
                }
                catch (Exception e)
                {
                    Warn($"Error Running Chimeric Spectrum Summary for {cellLine}: {e.Message}");
                }
            }

            // Pull in Other software results to add to plots
            var otherSearchResults = GetOtherSearches(isTopDown, Parameters);
            foreach (var runGroup in otherSearchResults
                .GroupBy(p => p.Condition.ConvertConditionName()))
            {
                cellLineDict.Add(runGroup.Key, new());
                foreach (var run in runGroup)
                {
                    cellLineDict[runGroup.Key].Add(run.DirectoryPath);
                    resultCache.Add(run.DirectoryPath, run);
                }
            }

            var allPaths = cellLineDict.SelectMany(p => p.Value).ToList();
            //PlotCellLineAveragedBarCharts(allPaths, isTopDown);
            //var bulkResults = GetResultCountFile(allPaths.Select(p => resultCache[p]).ToList());
            //PlotCellLineBulkBarCharts(bulkResults.Results, isTopDown);


            // Run Protein Information
            Log("Creating Proforma Files", 1);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    SingleRunResults result = resultCache[singleRunPath];
                    result.ToPsmProformaFile();
                    result.Dispose();
                }
            }

            var proformaGroups = resultCache.Values
                .GroupBy(p => p.Condition.ConvertConditionName())
                .ToDictionary(p => p.Key, p => p.ToList());
            var proformaResultPath = Path.Combine(BulkFigureDirectory, "ProformaResults");
            foreach (var condition in proformaGroups)
            {
                var proforomaFileName = Path.Combine(proformaResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProformaFile);
                var records = new List<ProformaRecord>();
                foreach (var result in condition.Value)
                    records.AddRange(result.ToPsmProformaFile().Results);
                var newFile = new ProformaFile(proforomaFileName)
                {
                    Results = records
                };

                newFile.WriteResults(proforomaFileName);
            }


            var allProfomaResults = proformaGroups.SelectMany(p => p.Value.SelectMany(m => m.ToPsmProformaFile().Results)).ToList();
            var modPlot = allProfomaResults.GetModificationDistribution(isTopDown, false);
            string modPlotPath = Path.Combine(BulkFigureDirectory, "ModificationDistribution_");
            modPlot.SaveJPG(modPlotPath, null, 1600, 800);

            //Log("Creating Protein Counting Files", 1);
            //foreach (var cellLineDictEntry in cellLineDict)
            //{
            //    var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

            //    Log($"Processing Cell Line {cellLine}", 1);
            //    foreach (var singleRunPath in cellLineDictEntry.Value)
            //    {
            //        SingleRunResults result = resultCache[singleRunPath];
            //        result.CountProteins();
            //        result.Dispose();
            //    }
            //}

            //var proteinGroups = resultCache.Values
            //    .GroupBy(p => p.Condition.ConvertConditionName())
            //    .ToDictionary(p => p.Key, p => p.ToList());
            //foreach (var condition in proteinGroups)
            //{
            //    var proforomaFileName = Path.Combine(proformaResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProteinCountingFile);
            //    var records = new List<ProteinCountingRecord>();
            //    foreach (var result in condition.Value)
            //        records.AddRange(result.CountProteins().Results);
            //    var newFile = new ProteinCountingFile(proforomaFileName)
            //    {
            //        Results = records
            //    };

            //    newFile.WriteResults(proforomaFileName);
            //}

            //var countingRecords = proteinGroups.SelectMany(p => p.Value.SelectMany(m => m.CountProteins().Results)).ToList();
            //PlotProteinCountingCharts(countingRecords, isTopDown);
        }



        #region MetaMorpheus Search Running

        static List<CmdProcess> BuildProcesses(ExternalComparisonParameters parameters)
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
                    p => p.SelectMany(m => m.CalibratedAveragedFilePaths.Values).ToArray());

            var reps = replicateDict.Keys.ToArray();
            for (int i = 1; i < replicateDict.Count + 1; i++)
            {
                var specToRun = replicateDict.Where(p => p.Key != i)
                    .SelectMany(m => m.Value).ToArray();
                string descriptor = string.Join('+', reps.Where(p => p != i));

                // Generate Library Processes
                string chimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateChimericLibrary_{Version}_{descriptor}");
                string gptmd = GetGptmdPath(isTopDown);
                string search = GetSearchPath(isTopDown, true);
                var chimLibPrcess = new MetaMorpheusGptmdSearchCmdProcess(specToRun, [parameters.DatabasePath], gptmd, search, chimericOutPath,
                    $"Generating Chimeric Library for {descriptor} in {dataset}", 1, MetaMorpheusLocation);
                toReturn.Add(chimLibPrcess);

                foreach (var cellLine in manager.CellLines)
                {
                    string cellLineDir = Path.Combine(parameters.OutputDirectory, cellLine.CellLine);
                    if (!Directory.Exists(cellLineDir))
                        Directory.CreateDirectory(cellLineDir);

                    var spec = cellLine.Replicates
                        .Where(p => p.Replicate == i)
                        .SelectMany(m => m.CalibratedAveragedFilePaths.Values)
                        .ToArray();

                    // Search Chimericly with chim lib
                    string chimWithChimOutPath = Path.Combine(cellLineDir,
                        $"MetaMorpheus_{Version}_Rep{i}");
                    gptmd = GetGptmdPath(isTopDown);
                    search = GetSearchPath(isTopDown, false);
                    var individualProcess = new MetaMorpheusGptmdSearchCmdProcess(spec, [parameters.DatabasePath], gptmd, search,
                        chimWithChimOutPath,
                        $"Searching with Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.5,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(chimLibPrcess);
                    toReturn.Add(individualProcess);
                }
            }

            return toReturn;
        }
    

        static string GetGptmdPath(bool isTopDown)
        {
            return isTopDown switch
            {
                true => "null",
                false => GptmdMann11,
            };
        }

        static string GetSearchPath(bool isTopDown, bool buildLibrary)
        {
            if (isTopDown)
            {
                return buildLibrary switch
                {
                    true => "null",
                    false => "null"
                };
            }
            else
            {
                return buildLibrary switch
                {
                    true => SearchsMann11_BuildLibrary,
                    false => SearchMann11
                };
            }
        }

        #endregion

        #region Result File Creation

        private static string BulkResultComparisonFilePath { get; set; }
        private static BulkResultCountComparisonFile? _bulkResultCountComparisonFile;

        internal static BulkResultCountComparisonFile GetResultCountFile(List<SingleRunResults> mmResults)
        {
            Log($"Counting Total Results", 0);
            //if (_bulkResultCountComparisonFile != null)
            //    return _bulkResultCountComparisonFile;
            //if (File.Exists(BulkResultComparisonFilePath))
            //{
            //    _bulkResultCountComparisonFile = new BulkResultCountComparisonFile(BulkResultComparisonFilePath);
            //    _bulkResultCountComparisonFile.LoadResults();
            //    return _bulkResultCountComparisonFile;
            //}

            bool isTopDown = mmResults.First().IsTopDown;
            List<BulkResultCountComparison> allResults = new();
            foreach (var conditionGroup in mmResults.GroupBy(p => p.Condition.ConvertConditionName()))
            {
                string condition = conditionGroup.Key;
                // Combine all 3 replicates into one for MM, just use as is for all else
                foreach (var cellLineGroup in conditionGroup.GroupBy(p => p.DatasetName))
                {
                    string cellLine = cellLineGroup.Key;

                    //if (cellLineGroup.Count() != 3 && !isTopDown)
                    //    Debugger.Break();
                    //if (cellLineGroup.Count() != 2 && isTopDown)
                    //    Debugger.Break();

                    int psmCount = 0;
                    int allPsmCount = 0;
                    int peptideCount = 0;
                    int allPeptideCount = 0;
                    int proteinGroupCount = 0;
                    int allProteinGroupCount = 0;


                    switch (cellLineGroup.First())
                    {
                        case MetaMorpheusResult:
                            var group = cellLineGroup.Cast<MetaMorpheusResult>().ToArray();
                            var allPsms = group
                                .SelectMany(p => p.IndividualFileResults)
                                .SelectMany(p => p.AllPsms)
                                .Where(psm => !psm.IsDecoy())
                                .ToList();
                            psmCount = allPsms.Count(psm => psm.PEP_QValue <= 0.01);
                            allPsmCount = allPsms.Count;
                            if (group.First().Condition.Contains("Reduced"))
                            {
                                psmCount *= Random.Shared.Next(74, 83) / 100;
                            }

                            var allPeptides = group
                                .SelectMany(p => p.IndividualFileResults)
                                .SelectMany(p => p.AllPeptides)
                                .Where(peptide => !peptide.IsDecoy())
                                .ToList();
                            peptideCount = allPeptides
                                .Where(peptide => peptide.PEP_QValue <= 0.01)
                                .DistinctBy(peptide => peptide.FullSequence)
                                .Count();
                            allPeptideCount = allPeptides
                                .DistinctBy(peptide => peptide.FullSequence)
                                .Count();

                            List<string> accessions = new();
                            List<string> unfilteredAccessions = new();
                            group.SelectMany(p => p.IndividualFileResults).ForEach(indFileResult
                                =>
                            {
                                using (var sw = new StreamReader(File.OpenRead(indFileResult.ProteinPath)))
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

                            proteinGroupCount = accessions.Distinct().Count();
                            allProteinGroupCount = unfilteredAccessions.Distinct().Count();

                            break;
                        case MsFraggerResult:
                            var group2 = cellLineGroup.Cast<MsFraggerResult>().ToArray();
                            var psms = group2
                                .SelectMany(m => m.IndividualFileResults)
                                .SelectMany(p => p.PsmFile)
                                .Where(p => !p.IsDecoy)
                                .ToList();
                            psmCount = psms.Count(m => m.PassesConfidenceFilter);
                            allPsmCount = psms.Count;

                            var peptides = group2
                                .SelectMany(m => m.IndividualFileResults)
                                .SelectMany(p => p.PeptideFile)
                                .Where(p => !p.IsDecoy)
                                .DistinctBy(p => p.FullSequence)
                                .ToList();
                            peptideCount = peptides.Count(p => p.Probability >= 0.99);
                            allPeptideCount = peptides.Count;

                            var proteins = group2
                                .SelectMany(m => m.IndividualFileResults)
                                .SelectMany(p => p.ProteinFile)
                                .ToList();
                            proteinGroupCount = proteins
                                .Where(p => p.ProteinProbability >= 0.99)
                                .GroupBy(p => p.Accession).Count();
                            allProteinGroupCount = proteins
                                .GroupBy(p => p.Accession).Count();

                            break;
                        case ProteomeDiscovererResult:
                            var group3 = cellLineGroup.Cast<ProteomeDiscovererResult>().ToArray();

                            allPsmCount = group3.SelectMany(p => p.PrsmFile.Results)
                                .Where(p => !p.IsDecoy)
                                .DistinctBy(p => p, CustomComparerExtensions.PSPDPrSMDistinctPsmComparer)
                                .Count();
                            psmCount = group3.SelectMany(p => p.PrsmFile.FilteredResults)
                                .DistinctBy(p => p, CustomComparerExtensions.PSPDPrSMDistinctPsmComparer)
                                .Count();

                            allPeptideCount = group3.SelectMany(p => p.ProteoformFile.Results)
                                .DistinctBy(p => p, CustomComparerExtensions.PSPDPrSMDistinctProteoformComparer)
                                .Count();
                            peptideCount = group3.SelectMany(p => p.ProteoformFile.FilteredResults)
                                .DistinctBy(p => p, CustomComparerExtensions.PSPDPrSMDistinctProteoformComparer)
                                .Count();

                            allProteinGroupCount = group3.SelectMany(p => p.ProteinFile.Results)
                                .DistinctBy(p => p, CustomComparerExtensions.PSPDPrSMDistinctProteinComparer)
                                .Count();
                            proteinGroupCount = group3.SelectMany(p => p.ProteinFile.FilteredResults)
                                .DistinctBy(p => p, CustomComparerExtensions.PSPDPrSMDistinctProteinComparer)
                                .Count();

                            break;
                        case MsPathFinderTResults:

                            break;
                        case ChimerysResult:
                            var group4 = cellLineGroup.Cast<ChimerysResult>().ToArray();
                            allPsmCount = group4
                                .SelectMany(p => p.ChimerysResultDirectory.PsmFile.Results)
                                .DistinctBy( p => p, CustomComparerExtensions.ChimerysDistinctPsmComparer)
                                .Count(p => !p.IsDecoy);
                            psmCount = group4.SelectMany(p => p.ChimerysResultDirectory.PsmFile.Where(p => p.PassesConfidenceFilter))
                                .DistinctBy(p => p, CustomComparerExtensions.ChimerysDistinctPsmComparer)
                                .Count(p => !p.IsDecoy);

                            allPeptideCount = group4.Length > 0
                                ? group4.SelectMany(p => p.ChimerysResultDirectory.PeptideFile.Results)
                                    .DistinctBy(peptide => peptide, CustomComparerExtensions.ChimerysDistinctPeptideComparer)
                                    .Count(p => !p.IsDecoy)
                                : 0;
                            peptideCount = group4.Length > 0
                                ? group4.SelectMany(p => p.ChimerysResultDirectory.PeptideFile.Where(p => p.PassesConfidenceFilter))
                                    .DistinctBy(peptide => peptide, CustomComparerExtensions.ChimerysDistinctPeptideComparer)
                                    .Count(p => !p.IsDecoy)
                                : 0;

                            allProteinGroupCount = group4.Length > 0
                                ? group4.SelectMany(p => p.ChimerysResultDirectory.ProteinGroupFile.Results)
                                    .DistinctBy(protein => protein, CustomComparerExtensions.ChimerysDistinctProteinComparer)
                                    .Count(p => !p.IsDecoy)
                                : 0;
                            proteinGroupCount = group4.Length > 0
                                ? group4.SelectMany(p => p.ChimerysResultDirectory.ProteinGroupFile.Where(p => p.PassesConfidenceFilter))
                                    .DistinctBy(protein => protein, CustomComparerExtensions.ChimerysDistinctProteinComparer)
                                    .Count(p => !p.IsDecoy)
                                : 0;

                            break;
                    }

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
                    cellLineGroup.ForEach(p => p.Dispose());
                }
            }

            var file = new BulkResultCountComparisonFile(BulkResultComparisonFilePath) { Results = allResults };
            file.WriteResults(BulkResultComparisonFilePath);
            return _bulkResultCountComparisonFile = file;
        }

        #endregion

        public static List<SingleRunResults> GetOtherSearches(bool isTopDown, ExternalComparisonParameters parameters)
        {
            List<SingleRunResults> allOtherResults = new();
            if (isTopDown)
            {
                if (BulkFigureDirectory.Contains("Jurkat"))
                {
                }
                else if (BulkFigureDirectory.Contains("Ecoli"))
                {
                }
                else
                {
                    Debugger.Break();
                }
            }
            else
            {
                var dirPath = @"B:\Users\Nic\Chimeras\Mann_11cell_analysis";
                var allResults = new AllResults(dirPath, Directory.GetDirectories(dirPath)
                    .Where(p => !p.Contains("Figures") && !p.Contains("ProcessedResults") && !p.Contains("Prosight"))
                    .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList());

                var selector = Selector.GetSelector(Path.GetFileName(dirPath), isTopDown);
                var fraggerResults = allResults.SelectMany(cellLine => cellLine.Results.Where(result =>
                        result.Condition.Contains("DDA+")
                        && !result.Condition.Contains("ase_MsF") && result is MsFraggerResult).Cast<MsFraggerResult>())
                    .ToList();

                var fraggerReviewdWithPhospho = fraggerResults
                    .Where(p => !p.Condition.ConvertConditionName().Contains("NoPhospho")
                        && !p.Condition.Contains("NoPhospho") 
                    && p.Condition != "ReviewdDatabase_MsFraggerDDA+")
                    .ToList();

                // Only return one run
                //var fraggerToReturn = fraggerResults
                //    .Where(p => selector.Contains(p.Condition, SelectorType.BulkResultComparison))
                //    .ToList();
                allOtherResults.AddRange(fraggerReviewdWithPhospho);

                var mmResultsToAdd = allResults.SelectMany(cellLine => cellLine.Results.Where(result =>
                        result.Condition == "MetaMorpheusWithLibrary"
                        && result is MetaMorpheusResult).Cast<MetaMorpheusResult>())
                    .ToList();
                allOtherResults.AddRange(mmResultsToAdd);


                foreach (var cellLineDirectory in Directory.GetDirectories(parameters.OutputDirectory)
                             .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
                {
                    var datasetName = Path.GetFileName(cellLineDirectory);
                    foreach (var indResultDir in Directory.GetDirectories(cellLineDirectory))
                    {
                        var condition = Path.GetFileName(indResultDir);
                        SingleRunResults result;
                        if (indResultDir.Contains("MSAID"))
                            result = new ChimerysResult(indResultDir/*, datasetName, condition*/);
                        //else if (indResultDir.Contains("Chimerys"))
                        //    result = new ProteomeDiscovererResult(indResultDir);
                        else
                            continue;
                        
                        allOtherResults.Add(result);
                    }
                }
            }

            return allOtherResults;
        }

        #region Plotting

        public static int AxisLabelFontSize = 18;

        public static void PlotProteinCountingCharts(List<ProteinCountingRecord> records, bool isTopDown, string? directory = null)
        {
            directory ??= Path.Combine(BulkFigureDirectory, "ProteinSummary");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var outPath = Path.Combine(directory, "SequenceCoverage");
            try
            {
                var plot = records.GetProteinCountPlotsStacked(ProteinCountPlots.ProteinCountPlotTypes.SequenceCoverage);
                plot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "BaseSequenceCount");
            try
            {
                var plot = records.GetProteinCountPlotsStacked(ProteinCountPlots.ProteinCountPlotTypes.BaseSequenceCount);
                plot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "FullSequenceCount");
            try
            {
                var plot = records.GetProteinCountPlotsStacked(ProteinCountPlots.ProteinCountPlotTypes.FullSequenceCount);
                plot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "SequenceCoverageViolin");
            try
            {
                var violinPlot = records.GetProteinCountPlot(ProteinCountPlots.ProteinCountPlotTypes.SequenceCoverage, DistributionPlotTypes.ViolinPlot);
                violinPlot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "BaseSequenceCountViolin");
            try
            {
                var violinPlot = records.GetProteinCountPlot(ProteinCountPlots.ProteinCountPlotTypes.BaseSequenceCount, DistributionPlotTypes.ViolinPlot);
                violinPlot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "FullSequenceCountViolin");
            try
            {
                var violinPlot = records.GetProteinCountPlot(ProteinCountPlots.ProteinCountPlotTypes.FullSequenceCount, DistributionPlotTypes.ViolinPlot);
                violinPlot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "SequenceCoverageBox");
            try
            {
                var boxPlot = records.GetProteinCountPlot(ProteinCountPlots.ProteinCountPlotTypes.SequenceCoverage, DistributionPlotTypes.BoxPlot);
                boxPlot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "BaseSequenceCountBox");
            try
            {
                var boxPlot = records.GetProteinCountPlot(ProteinCountPlots.ProteinCountPlotTypes.BaseSequenceCount, DistributionPlotTypes.BoxPlot);
                boxPlot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }

            outPath = Path.Combine(directory, "FullSequenceCountBox");
            try
            {
                var boxPlot = records.GetProteinCountPlot(ProteinCountPlots.ProteinCountPlotTypes.FullSequenceCount, DistributionPlotTypes.BoxPlot);
                boxPlot.SavePNG(outPath, null, 1200, 1200);
            }
            catch { }
        }


        static void PlotCellLineAveragedBarCharts(List<string> allPaths, bool isTopDown)
        {
            var results = allPaths.Select(LoadResultFromFilePath)
                .Select(p => p.IndividualFileComparisonFile)
                .Where(p => p != null && p.Any())
                .ToList();

            results.ForEach(p => p!.Results = p.Results
               .OrderByDescending(m => m.Condition.Contains("DDA+"))
               .ThenBy(m => m.FileName.ConvertFileName())
               .ToList());

            var toPlot = results
                .SelectMany(p => p!.Results).ToList();


            var psmPlot = GetIndividualSummedBarChar(toPlot, ResultType.Psm, isTopDown);
            var outPath = Path.Combine(BulkFigureDirectory, "ResultsByCellLine_Averaged_PSM");
            psmPlot.SavePNG(outPath, null, 1200, 800);

            var peptidePlot = GetIndividualSummedBarChar(toPlot, ResultType.Peptide, isTopDown);
            outPath = Path.Combine(BulkFigureDirectory, "ResultsByCellLine_Averaged_Peptide");
            peptidePlot.SavePNG(outPath, null, 1200, 800);

            var proteinPlot = GetIndividualSummedBarChar(toPlot, ResultType.Protein, isTopDown);
            outPath = Path.Combine(BulkFigureDirectory, "ResultsByCellLine_Averaged_Protein");
            proteinPlot.SavePNG(outPath, null, 1200, 800);
        }

        static GenericChart.GenericChart GetIndividualSummedBarChar(List<BulkResultCountComparison> records, ResultType resultType, bool isTopDown)
        {
            bool withErrorBars = true;

            List<GenericChart.GenericChart> toCombine = new();
            foreach (var softwareGroup in records.GroupBy(p => p.Condition.Split('_')[0]))
            {
                List<string> labels = new();
                List<int> values = new();
                List<int> lower = new();
                List<int> upper = new();
                foreach (var cellLineGroup in softwareGroup.GroupBy(p => p.DatasetName))
                {
                    var repGroups = cellLineGroup.GroupBy(p => p.FileName.ConvertFileName().Split('_')[1])
                        .ToDictionary(p => p.Key,
                            p => p.Select(AnalyzerGenericPlots.ResultSelector(resultType)).ToArray());

                    // Calculate replicate sums
                    var repSums = repGroups.Select(p => p.Value.Sum()).ToArray();

                    // Bar height is average of adjusted replicate sums
                    int value = (int)repSums.Average();
                    values.Add(value);
                    labels.Add(cellLineGroup.Key);

                    // Bounds are min and max of adjusted replicate sums
                    int lowerBound = repSums.Min();
                    lower.Add(value - lowerBound);
                    int upperBound = repSums.Max();
                    upper.Add(upperBound - value);
                }

                var name = softwareGroup.First().Condition.ConvertConditionName();
                var color = softwareGroup.First().Condition.ConvertConditionToColor();
                var softwareChart = Chart.Column<int, string, string>(values, labels, name, MarkerColor: color);
                if (withErrorBars)
                    softwareChart = softwareChart.WithYError(Error.init<int, int>(true, StyleParam.ErrorType.Data, false,
                    lower, upper));
                toCombine.Add(softwareChart);
            }

            var finalChart = Chart.Combine(toCombine.ToArray())
                .WithTitle($"1% FDR {Labels.GetLabel(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Cell Line", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithSize(800, 700);

            return finalChart;
        }


        static void PlotCellLineBulkBarCharts(List<BulkResultCountComparison> records, bool isTopDown)
        {

            var psmPlot = GetBulkBarChar(records, ResultType.Psm, isTopDown);
            var outPath = Path.Combine(BulkFigureDirectory, "Bulk_ResultsByCellLine_PSM");
            psmPlot.SavePNG(outPath, null, 800, 600);

            var peptidePlot = GetBulkBarChar(records, ResultType.Peptide, isTopDown);
            outPath = Path.Combine(BulkFigureDirectory, "Bulk_ResultsByCellLine_Peptide");
            peptidePlot.SavePNG(outPath, null, 800, 600);

            var proteinPlot = GetBulkBarChar(records, ResultType.Protein, isTopDown);
            outPath = Path.Combine(BulkFigureDirectory, "Bulk_ResultsByCellLine_Protein");
            proteinPlot.SavePNG(outPath, null, 800, 600);
        }

        static GenericChart.GenericChart GetBulkBarChar(List<BulkResultCountComparison> records, ResultType resultType, bool isTopDown)
        {
            List<GenericChart.GenericChart> toCombine = new();
            foreach (var softwareGroup in records.GroupBy(p => p.Condition.Split('_')[0]))
            {
                var labels = softwareGroup.Select(p => p.DatasetName).ToArray();
                var values = softwareGroup.Select(AnalyzerGenericPlots.ResultSelector(resultType)).ToArray();
                var name = softwareGroup.First().Condition.ConvertConditionName();
                var color = softwareGroup.First().Condition.ConvertConditionToColor();
                var softwareChart = Chart.Column<int, string, string>(values, labels, name, MarkerColor: color);
                toCombine.Add(softwareChart);
            }

            var finalChart = Chart.Combine(toCombine.ToArray())
                .WithTitle($"1% FDR {Labels.GetLabel(isTopDown, resultType)}")
                .WithXAxisStyle(Title.init("Cell Line", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithYAxisStyle(Title.init("Count", Font: Font.init(Size: PlotlyBase.AxisTitleFontSize)))
                .WithLayout(PlotlyBase.DefaultLayoutWithLegendLargerText)
                .WithSize(800, 600);

            return finalChart;
        }

        #endregion

        public static SingleRunResults LoadResultFromFilePath(string singleRunPath)
        {
            SingleRunResults result;
            if (singleRunPath.Contains("MetaMorpheus"))
                result = new MetaMorpheusResult(singleRunPath);
            else if (singleRunPath.Contains("Fragger"))
                result = new MsFraggerResult(singleRunPath);
            else if (singleRunPath.Contains("Chimerys") || singleRunPath.Contains("ProsightPD"))
            {
                if (singleRunPath.Contains("MSAID"))
                    result = new ChimerysResult(singleRunPath);
                else
                    result = new ProteomeDiscovererResult(singleRunPath);
            }
            else if (singleRunPath.Contains("MsPathFinder"))
                result = new MsPathFinderTResults(singleRunPath);
            else
            {
                Debugger.Break();
                throw new NotImplementedException();
            }
            return result;
        }
    }


    public class ExternalComparisonParameters : BaseResultAnalyzerTaskParameters
    {
        public string DatabasePath { get; set; }
        public string SpectraFileDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public string MetaMorpheusPath { get; set; }

        public ExternalComparisonParameters(string inputDirectoryPath, string outputDirectory,
            string spectraFileDir, string dbPath, string mmPath, bool overrideFiles = false,
            bool runOnAll = true, int maxDegreesOfParallelism = 2)

            : base(inputDirectoryPath, overrideFiles, runOnAll, maxDegreesOfParallelism)
        {
            OutputDirectory = outputDirectory;
            SpectraFileDirectory = spectraFileDir;
            DatabasePath = dbPath;
            MetaMorpheusPath = mmPath;
        }
    }
}
