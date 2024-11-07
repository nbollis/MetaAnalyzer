using System.Diagnostics;
using Analyzer;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;
using Analyzer.Util;
using Easy.Common.Extensions;
using Plotting.Util;
using ResultAnalyzerUtil;
using TaskLayer.CMD;

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

        protected override void RunSpecific() => RunSpecificAsync(Parameters).Wait();

        private static async Task RunSpecificAsync(ExternalComparisonParameters parameters)
        {
            var processes = BuildProcesses(parameters);
            await RunProcesses(processes);


            var isTopDown = !parameters.InputDirectoryPath.Contains("Mann");
            Dictionary<string, List<string>> cellLineDict = new();
            foreach (var cellLineDirectory in Directory.GetDirectories(parameters.OutputDirectory)
                         .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
            {
                cellLineDict.Add(cellLineDirectory, new());
                foreach (var runDirectory in Directory.GetDirectories(cellLineDirectory).Where(p => !p.Contains("Figure") && p.Contains(Version)))
                    cellLineDict[cellLineDirectory].Add(runDirectory);
            }

            // Run MM Task basic processing 
            int degreesOfParallelism = (int)(MaxWeight / 0.25);
            Parallel.ForEach(cellLineDict.SelectMany(p => p.Value),
                new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(degreesOfParallelism, 1) },
                singleRunPath =>
                {
                    var mmResult = new MetaMorpheusResult(singleRunPath);
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
                    if (sw.Elapsed.Minutes < 1 && !parameters.Override)
                        return;

                    mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                    mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
                });

            Log($"Running Chimeric Spectrum Summaries", 0);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);
                Log($"Processing Cell Line {cellLine}", 1);
                List<CmdProcess> summaryTasks = new();
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    var summaryParams =
                        new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
                    var summaryTask = new SingleRunChimericSpectrumSummaryTask(summaryParams);
                    summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Chimeric Spectrum Summary", 0.5,
                        singleRunPath));
                }

                try
                {
                    await RunProcesses(summaryTasks);
                }
                catch (Exception e)
                {
                    Warn($"Error Running Chimeric Spectrum Summary for {cellLine}: {e.Message}");
                }
            }

            // Pull in Other software results to add to plots
            var otherSearchResults = GetOtherSearches(isTopDown);
            foreach (var runGroup in otherSearchResults.GroupBy(p => p.Condition.ConvertConditionName()))
            {
                cellLineDict.Add(runGroup.Key, new());
                foreach (var run in runGroup)
                    cellLineDict[runGroup.Key].Add(run.DirectoryPath);
            }

            // TODO: Comparative bar graphs

            // Run Protein Information
            Log("Creating Proforma Files", 1);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    SingleRunResults result;
                    if (singleRunPath.Contains("MetaMorpheus"))
                        result = new MetaMorpheusResult(singleRunPath);
                    else if (singleRunPath.Contains("Fragger"))
                        result = new MsFraggerResult(singleRunPath);
                    else if (singleRunPath.Contains("Chimerys") || singleRunPath.Contains("ProsightPD"))
                        result = new ProteomeDiscovererResult(singleRunPath);
                    else if (singleRunPath.Contains("MsPathFinder"))
                        result = new MsPathFinderTResults(singleRunPath);
                    else
                    {
                        Debugger.Break();
                        throw new NotImplementedException();
                    }
                    result.ToPsmProformaFile();
                    result.Dispose();
                }
            }

            var proformaGroups = cellLineDict.SelectMany(p => p.Value)
                .Where(p => p.Contains("MetaMorpheus"))
                .Select(p => new MetaMorpheusResult(p))
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

            Log("Creating Protein Counting Files", 1);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    SingleRunResults result;
                    if (singleRunPath.Contains("MetaMorpheus"))
                        result = new MetaMorpheusResult(singleRunPath);
                    else if (singleRunPath.Contains("Fragger"))
                        result = new MsFraggerResult(singleRunPath);
                    else if (singleRunPath.Contains("Chimerys") || singleRunPath.Contains("ProsightPD"))
                        result = new ProteomeDiscovererResult(singleRunPath);
                    else if (singleRunPath.Contains("MsPathFinder"))
                        result = new MsPathFinderTResults(singleRunPath);
                    else
                    {
                        Debugger.Break();
                        throw new NotImplementedException();
                    }
                    result.CountProteins();
                    result.Dispose();
                }
            }

            var proteinGroups = cellLineDict.SelectMany(p => p.Value)
                .Where(p => p.Contains("MetaMorpheus"))
                .Select(p => new MetaMorpheusResult(p))
                .GroupBy(p => p.Condition.ConvertConditionName())
                .ToDictionary(p => p.Key, p => p.ToList());
            foreach (var condition in proteinGroups)
            {
                var proforomaFileName = Path.Combine(proformaResultPath, condition.Key + "_PSM_" + FileIdentifiers.ProteinCountingFile);
                var records = new List<ProteinCountingRecord>();
                foreach (var result in condition.Value)
                    records.AddRange(result.CountProteins().Results);
                var newFile = new ProteinCountingFile(proforomaFileName)
                {
                    Results = records
                };

                newFile.WriteResults(proforomaFileName);
            }
            // plot in R


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
                var chimLibPrcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search, chimericOutPath,
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
                    var individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
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

        static List<SingleRunResults> GetOtherSearches(bool isTopDown)
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
                        .Select(datasetDirectory => new CellLineResults(datasetDirectory)).ToList())
                    .SelectMany(cellLine => cellLine.Results.Where(result => result.Condition.Contains("DDA+") 
                                                                             && result is MsFraggerResult).Cast<MsFraggerResult>())
                    .ToList();
                var fraggerReviewedDbNoPhospho = allResults
                    .Where(p => p.Condition.ConvertConditionName().Contains("NoPhospho"))
                    .ToList();
                var fraggerReviewdWithPhospho = allResults
                    .Where(p => !p.Condition.ConvertConditionName().Contains("NoPhospho") && p.Condition != "ReviewdDatabase_MsFraggerDDA+")
                    .ToList();

                allOtherResults.AddRange(fraggerReviewedDbNoPhospho);


                string chimerysPath = @"B:\Users\Nic\Chimeras\Chimerys\Chimerys";
                var chimerys = new ProteomeDiscovererResult(chimerysPath);

                allOtherResults.Add(chimerys);
            }

            return allOtherResults;
        }

        #endregion

        #region Result File Creation

        private static string BulkResultComparisonFilePath { get; set; }
        private static BulkResultCountComparisonFile? _bulkResultCountComparisonFile;

        internal static BulkResultCountComparisonFile GetResultCountFile(List<SingleRunResults> mmResults)
        {
            Debugger.Break();
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
                // Combine all 3 replicates into one for MM, just use as is for all else
                foreach (var cellLineGroup in conditionGroup.GroupBy(p => p.DatasetName))
                {
                    string cellLine = cellLineGroup.Key;

                    if (cellLineGroup.Count() != 3 && !isTopDown)
                        Debugger.Break();
                    if (cellLineGroup.Count() != 2 && isTopDown)
                        Debugger.Break();

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
                            psmCount = group.Sum(p =>
                                p.AllPsms.Count(psm => !psm.IsDecoy() && psm.PEP_QValue <= 0.01));
                            allPsmCount = group.Sum(p => p.AllPsms.Count(psm => !psm.IsDecoy()));

                            peptideCount = group.SelectMany(p =>
                                    p.AllPeptides.Where(peptide => !peptide.IsDecoy() && peptide.PEP_QValue <= 0.01))
                                .DistinctBy(peptide => peptide.FullSequence)
                                .Count();
                            allPeptideCount = group.SelectMany(p => p.AllPeptides.Where(peptide => !peptide.IsDecoy()))
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

                            proteinGroupCount = accessions.Distinct().Count();
                            allProteinGroupCount = accessions.Distinct().Count();

                            break;
                        case MsFraggerResult:
                            var group2 = cellLineGroup.Cast<MsFraggerResult>().ToArray();
                            var psms = group2.SelectMany(m => m.IndividualFileResults)
                                .SelectMany(p => p.PsmFile).ToList();
                            psmCount = psms.Count(m => m.PassesConfidenceFilter);
                            allPsmCount = psms.Count;

                            var peptides = group2.SelectMany(m => m.IndividualFileResults)
                                .SelectMany(p => p.PeptideFile).ToList();
                            peptideCount = peptides.Count(p => p.Probability >= 0.99);

                            var proteins = group2.SelectMany(m => m.IndividualFileResults)
                                .SelectMany(p => p.ProteinFile).ToList();
                            proteinGroupCount = proteins.GroupBy(p => p.Accession).Count();
                            allProteinGroupCount = proteins.Where(p => p.ProteinProbability >= 0.99)
                                .GroupBy(p => p.Accession).Count();
                            break;
                        case ProteomeDiscovererResult:
                            var group3 = cellLineGroup.Cast<ProteomeDiscovererResult>().ToArray();

                            break;
                        case MsPathFinderTResults:

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


        #region Plotting

        // TODO:

        #endregion
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
