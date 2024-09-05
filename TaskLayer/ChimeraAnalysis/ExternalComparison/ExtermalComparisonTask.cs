using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.FileTypes.Internal;
using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using Analyzer.Util;
using Easy.Common.Extensions;
using Readers;
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
        public static string Version => "105";

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

            Dictionary<string, List<string>> cellLineDict = new();
            foreach (var cellLineDirectory in Directory.GetDirectories(parameters.OutputDirectory)
                         .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
            {
                cellLineDict.Add(cellLineDirectory, new());
                foreach (var runDirectory in Directory.GetDirectories(cellLineDirectory).Where(p => !p.Contains("Figure") && p.Contains(Version)))
                    cellLineDict[cellLineDirectory].Add(runDirectory);
            }
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
                    $"Generating Chimeric Library for {descriptor} in {dataset}", 0.5, MetaMorpheusLocation);
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
                        $"Searching with Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
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
                true => GptmdMann11,
                false => "null"
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

        internal static BulkResultCountComparisonFile GetResultCountFile(List<MetaMorpheusResult> mmResults)
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
