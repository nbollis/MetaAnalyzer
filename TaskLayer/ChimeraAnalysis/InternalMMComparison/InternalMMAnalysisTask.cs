using Analyzer.Plotting.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Interfaces;
using Analyzer.Plotting.ComparativePlots;
using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;
using Analyzer.Util;
using pepXML.Generated;
using Plotly.NET;
using TaskLayer.CMD;

namespace TaskLayer.ChimeraAnalysis
{
    public class InternalMetaMorpheusAnalysisTask : BaseResultAnalyzerTask
    {

        #region FilePaths

        // Paths for setup
        internal static string MetaMorpheusLocation { get; set; }


        public static string UniprotHumanProteomeAndReviewedXml =>
            @"B:\Users\Nic\Chimeras\Mann_11cell_analysis\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";

        public static string Mann11DataFileDirectory =>
            @"B:\RawSpectraFiles\Mann_11cell_lines";

        public static string Mann11OutputDirectory =>
            @"B:\Users\Nic\Chimeras\InternalMMAnalysis\Mann_11cell_lines";


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
            @"";

        public static string Search_BuildNonChimericLibraryJurkatTd =>
            @"";

        public static string GptmdNoChimerasJurkatTd =>
            @"";

        public static string GptmdWithChimerasJurkatTd =>
            @"";

        public static string SearchNoChimerasJurkatTd =>
            @"";

        public static string SearchWithChimerasJurkatTd =>
            @"";


        #endregion

        public static string Version => "105";
        private static string NonChimericDescriptor => "MetaMorpheusNoChimeras";
        private static string ChimericDescriptor => "MetaMorpheusWithChimeras";

        public override MyTask MyTask => MyTask.InternalMetaMorpheusAnalysis;
        public override InternalMetaMorpheusAnalysisParameters Parameters { get; }

        public InternalMetaMorpheusAnalysisTask(InternalMetaMorpheusAnalysisParameters parameters)
        {
            Parameters = parameters;
            MetaMorpheusLocation = parameters.MetaMorpheusPath;
        }

        protected override void RunSpecific() => RunSpecificAsync(Parameters);

        private static void RunSpecificAsync(InternalMetaMorpheusAnalysisParameters parameters)
        {
            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);

            var processes = BuildProcesses(parameters);
            RunProcesses(processes).Wait();

            // Parse Results Together
            Dictionary<string, List<string>> cellLineDict = new();
            foreach (var cellLineDirectory in Directory.GetDirectories(parameters.OutputDirectory)
                         .Where(p => !p.Contains("Generate") && !p.Contains("Figure")))
            {
                cellLineDict.Add(cellLineDirectory, new());
                foreach (var runDirectory in Directory.GetDirectories(cellLineDirectory)
                             .Where(p => !p.Contains("Figure")))
                {
                    //if (runDirectory.Contains(ChimericDescriptor) && runDirectory.Contains("NonChimericLibrary"))
                    //    continue;

                    cellLineDict[cellLineDirectory].Add(runDirectory);
                }
            }

            // Run MM Task basic processing 
            int degreesOfParallelism = (int)(MaxWeight / 0.25);
            Parallel.ForEach(cellLineDict.SelectMany(p =>  p.Value),
                new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(degreesOfParallelism, 1) },
                singleRunPath =>
                {
                    var mmResult = new MetaMorpheusResult(singleRunPath);
                    Log($"Processing {mmResult.DatasetName} {mmResult.Condition}", 1);

                    Log($"Tabulating Result Counts", 2);
                    _ = mmResult.GetIndividualFileComparison();
                    _ = mmResult.GetBulkResultCountComparisonFile();

                    Log($"Counting Chimeric Psms/Peptides", 2);
                    mmResult.CountChimericPsms();
                    mmResult.CountChimericPeptides();

                    Log($"Running Chimera Breakdown Analysis", 2);
                    var sw = Stopwatch.StartNew();
                    _ = mmResult.GetChimeraBreakdownFile();
                    sw.Stop();

                    // if it takes less than one minute to get the breakdown, and we are not overriding, do not plot
                    if (sw.Elapsed.Minutes < 1 && !parameters.Override)
                        return;

                    mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Psm);
                    mmResult.PlotChimeraBreakDownStackedColumn_Scaled(ResultType.Peptide);
                });

            // Chimeric Spectrum Summary -> Creates Fractional Intensity Plots
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
                    RunProcesses(summaryTasks).Wait();
                }
                catch (Exception e)
                {
                    Warn($"Error Running Chimeric Spectrum Summary for {cellLine}: {e.Message}");
                }
            }


            Log($"Running Spectral Angle Comparisons", 0);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                List<CmdProcess> summaryTasks = new();
                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    if (singleRunPath.Contains(NonChimericDescriptor))
                        continue;
                    var summaryParams =
                        new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
                    var summaryTask = new SingleRunSpectralAngleComparisonTask(summaryParams);
                    summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Spectral Angle Comparisons", 0.25,
                        singleRunPath));
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

            Log($"Running Retention Time Plots", 0);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                List<CmdProcess> summaryTasks = new();
                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    if (singleRunPath.Contains(NonChimericDescriptor))
                        continue;

                    var summaryParams =
                        new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
                    var summaryTask = new SingleRunChimeraRetentionTimeDistribution(summaryParams);
                    summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Retention Time Plots", 0.25,
                        singleRunPath));
                }

                try
                {
                    RunProcesses(summaryTasks).Wait();
                }
                catch (Exception e)
                {
                    Warn($"Error Running Retention Time Plots for {cellLine}: {e.Message}");
                }
            }

            Log($"Running Retention Time Alignment", 0);
            foreach (var cellLineDictEntry in cellLineDict)
            {
                var cellLine = Path.GetFileNameWithoutExtension(cellLineDictEntry.Key);

                List<CmdProcess> summaryTasks = new();
                Log($"Processing Cell Line {cellLine}", 1);
                foreach (var singleRunPath in cellLineDictEntry.Value)
                {
                    if (singleRunPath.Contains(NonChimericDescriptor))
                        continue;
                    var summaryParams =
                        new SingleRunAnalysisParameters(singleRunPath, parameters.Override, false);
                    var summaryTask = new SingleRunChimeraRetentionTimeDistribution(summaryParams);
                    summaryTasks.Add(new ResultAnalyzerTaskToCmdProcessAdaptor(summaryTask, "Retention Time Alignment", 0.25,
                        singleRunPath));
                }

                try
                {
                    RunProcesses(summaryTasks).Wait();
                }
                catch (Exception e)
                {
                    Warn($"Error Running Retention Time Alignment for {cellLine}: {e.Message}");
                }
            }


            // Plot the bulk comparisons
        }

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
                string gptmd = GetGptmdPath(true, isTopDown);
                string search = GetSearchPath(true, isTopDown, true);
                var chimLibPrcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search, chimericOutPath,
                    $"Generating Chimeric Library for {descriptor} in {dataset}", 0.5, MetaMorpheusLocation);
                toReturn.Add(chimLibPrcess);

                string nonChimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateNonChimericLibrary_{Version}_{descriptor}");
                gptmd = GetGptmdPath(false, isTopDown);
                search = GetSearchPath(false, isTopDown, true);
                var nonChimLibProcess = new InternalMetaMorpheusCmdProcess(specToRun, parameters.DatabasePath, gptmd, search,
                    nonChimericOutPath,
                    $"Generating Non-Chimeric Library for {descriptor} in {dataset}", 0.5, MetaMorpheusLocation);
                toReturn.Add(nonChimLibProcess);

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
                        $"{ChimericDescriptor}_{Version}_ChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    var individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithChimOutPath,
                        $"Searching with Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(chimLibPrcess);
                    toReturn.Add(individualProcess);

                    // Search Chimerically with NonChimeric Lib
                    string chimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"{ChimericDescriptor}_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithNonChimOutPath,
                        $"Searching Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation, $"{cellLine.CellLine} rep {i}");
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);

                    // Search NonChimerically with NonChimeric Lib
                    string nonChimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"{NonChimericDescriptor}_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(false, isTopDown);
                    search = GetSearchPath(false, isTopDown, false);
                    individualProcess = new InternalMetaMorpheusCmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        nonChimWithNonChimOutPath,
                        $"Searching Non-Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation);
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);
                }

            }

            return toReturn;
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

    }

}
