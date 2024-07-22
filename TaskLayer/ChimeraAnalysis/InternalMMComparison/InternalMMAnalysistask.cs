using Analyzer.Plotting.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

        // Semaphore to control the weight of running processes
        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        public static string Version => "105";

        public override MyTask MyTask => MyTask.InternalMetaMorpheusAnalysis;
        public override InternalMetaMorpheusAnalysisParameters Parameters { get; }

        public InternalMetaMorpheusAnalysisTask(InternalMetaMorpheusAnalysisParameters parameters)
        {
            Parameters = parameters;
            MetaMorpheusLocation = parameters.MetaMorpheusPath;
            CurrentWeight = 0;
        }

        protected override void RunSpecific() => RunSpecificAsync(Parameters);



        private static void RunSpecificAsync(InternalMetaMorpheusAnalysisParameters parameters)
        {
            if (!Directory.Exists(parameters.OutputDirectory))
                Directory.CreateDirectory(parameters.OutputDirectory);

            var processes = BuildProcesses(parameters);
            RunProcesses(processes).Wait();
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

            for (int i = 1; i < replicateDict.Count + 1; i++)
            {
                var specToRun = replicateDict.Where(p => p.Key != i)
                    .SelectMany(m => m.Value).ToArray();
                string descriptor = i switch
                {
                    1 => "2+3",
                    2 => "1+3",
                    3 => "1+2",
                    _ => throw new NotImplementedException()
                };

                // Generate Library Processes
                string chimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateChimericLibrary_{Version}_{descriptor}");
                string gptmd = GetGptmdPath(true, isTopDown);
                string search = GetSearchPath(true, isTopDown, true);
                var chimLibPrcess = new CmdProcess(specToRun, parameters.DatabasePath, gptmd, search, chimericOutPath,
                    $"Generating Chimeric Library for {descriptor} in {dataset}", 0.5, MetaMorpheusLocation);
                toReturn.Add(chimLibPrcess);

                string nonChimericOutPath = Path.Combine(parameters.OutputDirectory,
                    $"GenerateNonChimericLibrary_{Version}_{descriptor}");
                gptmd = GetGptmdPath(false, isTopDown);
                search = GetSearchPath(false, isTopDown, true);
                var nonChimLibProcess = new CmdProcess(specToRun, parameters.DatabasePath, gptmd, search,
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
                        $"MetaMorpheusWithChimeras_{Version}_ChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    var individualProcess = new CmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithChimOutPath,
                        $"Searching with Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation);
                    individualProcess.DependsOn(chimLibPrcess);
                    toReturn.Add(individualProcess);

                    // Search Chimerically with NonChimeric Lib
                    string chimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"MetaMorpheusWithChimeras_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(true, isTopDown);
                    search = GetSearchPath(true, isTopDown, false);
                    individualProcess = new CmdProcess(spec, parameters.DatabasePath, gptmd, search,
                        chimWithNonChimOutPath,
                        $"Searching Chimeric with Non-Chimeric Library for Replicate {i} in {cellLine.CellLine}", 0.25,
                        MetaMorpheusLocation);
                    individualProcess.DependsOn(nonChimLibProcess);
                    toReturn.Add(individualProcess);

                    // Search NonChimerically with NonChimeric Lib
                    string nonChimWithNonChimOutPath = Path.Combine(cellLineDir,
                        $"MetaMorpheusNoChimeras_{Version}_NonChimericLibrary_Rep{i}");
                    gptmd = GetGptmdPath(false, isTopDown);
                    search = GetSearchPath(false, isTopDown, false);
                    individualProcess = new CmdProcess(spec, parameters.DatabasePath, gptmd, search,
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


        private static double CurrentWeight;
        static async Task RunProcesses(List<CmdProcess> processes)
        {
            List<Task> runningTasks = new List<Task>();

            foreach (var process in processes)
            {
                var task = RunProcess(process);
                runningTasks.Add(task);

                // Wait for any task to complete if current weight exceeds the limit
                while (CurrentWeight >= 1)
                {
                    var completedTask = await Task.WhenAny(runningTasks);
                    runningTasks.Remove(completedTask);
                }
            }

            // Wait for all remaining tasks to complete
            await Task.WhenAll(runningTasks);
        }

        static async Task RunProcess(CmdProcess process)
        {
            string dependencyResult = null;

            if (process.Dependency != null)
            {
                dependencyResult = await process.Dependency.Task;
            }

            // Manages the ability to run on multiple computers at once
            // if already finished, return
            // if still running, wait until finish and return
            if (process.HasStarted())
            {
                if (process.IsCompleted())
                    Console.WriteLine($"Previous Completion Detected: {process.SummaryText}");
                else
                    Console.WriteLine($"Has Started Elsewhere: {process.SummaryText}");

                while (!process.IsCompleted())
                {
                    await Task.Delay(1000); // Adjust delay as needed
                }

                return;
            }

            while (true)
            {
                await semaphore.WaitAsync();

                if (CurrentWeight + process.Weight <= 1)
                {
                    CurrentWeight += process.Weight;
                    semaphore.Release();
                    break;
                }

                semaphore.Release();
                await Task.Delay(100000); // Adjust delay as needed
            }

            try
            {
                // Simulate running the command and generate a file path
                Console.WriteLine($"Starting process: {process.SummaryText}");

                // Example Runner
                if (false)
                {
                    string filePath = $"{process}_output.txt";

                    // Use the dependency result if it exists
                    if (!string.IsNullOrEmpty(dependencyResult))
                    {
                        Console.WriteLine($"Using dependency result: {dependencyResult} in {process}");
                    }

                    // Set the result of the completion source to the file path
                    process.CompletionSource.SetResult(filePath);
                }
                else
                {
                    // Use the dependency result if it exists
                    if (!string.IsNullOrEmpty(dependencyResult))
                    {
                        Console.WriteLine($"Using dependency result: {dependencyResult} in {process.SummaryText}");
                    }

                    var proc = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = process.ProgramExe,
                            Arguments = process.Prompt,
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            WorkingDirectory = process.WorkingDirectory, 
                            ErrorDialog = true, 
                        }
                    };
                    proc.Start();
                    await proc.WaitForExitAsync();

                    // if build library task, set completion results to the library path
                    if (process.SearchTask.Contains("Build"))
                    {
                        var filePath = Directory.GetFiles(process.OutputDirectory, "*.msp", SearchOption.AllDirectories).FirstOrDefault();
                        if (filePath != null)
                            process.CompletionSource.SetResult(filePath);
                    }
                }

                Console.WriteLine($"Completed process: {process.SummaryText}");
            }
            finally
            {
                await semaphore.WaitAsync();
                CurrentWeight -= process.Weight;
                semaphore.Release();
            }
        }
    }

}
