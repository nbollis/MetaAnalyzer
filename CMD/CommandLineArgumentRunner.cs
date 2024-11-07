﻿using System.Diagnostics;
using Plotting.Util;

namespace CMD
{
    public static class CommandLineArgumentRunner
    {
        internal static List<ChimeraPaperDataManager> Mann11InternalComparison;

        static CommandLineArgumentRunner()
        {
            Mann11InternalComparison = new();
            foreach (var runDirectory in Directory.GetDirectories(ImportantPaths.Mann11AllRunPath)
                         .Where(p => !p.Contains("Figures") && !p.Contains("Prosight")))
            {
                var cellLineId = Path.GetFileNameWithoutExtension(runDirectory);
                var dataDirPath = Path.Combine(ImportantPaths.Mann11DataFilePath, cellLineId);
                Mann11InternalComparison.Add(new("Mann_11cell_analysis", cellLineId, false, runDirectory, dataDirPath));
            }
        }

        public static void GenerateLibraryFiles()
        {
                        string workingDir = ImportantPaths.MetaMorpheusLocation;
            string programExe = "CMD.exe";

            var prompts = Mann11InternalComparison.First().GenerateLibraryTasksBottomUp().ToList();
            
            RunAllProcesses(prompts, workingDir, programExe);
        }
        

        public static void RunMann11InternalComparisonInParallel()
        {
            string workingDir = ImportantPaths.MetaMorpheusLocation;
            string programExe = "CMD.exe";
            

            List<string> prompts = new();
            foreach (var cellLine in Mann11InternalComparison)
            {
                var results = cellLine.InternalComparisonCommandPrompts().ToArray();
                var nonChim = results[0];
                var chim = results[1];
                var resultPath = Path.Combine(cellLine.DirectoryPath, "SearchResults", "MetaMorpheus_105_NoChimeras");
                if (!Directory.Exists(resultPath))
                    prompts.Add(nonChim);
                else if (Directory.GetFiles(resultPath, "*.psmtsv", SearchOption.AllDirectories).Length < 14)
                    prompts.Add(nonChim);

                var chimResultPath = Path.Combine(cellLine.DirectoryPath, "SearchResults", "MetaMorpheus_105_WithChimeras");
                //if (!Directory.Exists(chimResultPath))
                //    prompts.Add(chim);
                //else if (Directory.GetFiles(chimResultPath, "*.psmtsv", SearchOption.AllDirectories).Length < 14)
                //    prompts.Add(chim);
            }
            RunAllProcesses(prompts, workingDir, programExe);
        }

        public static void RunAllProcesses(List<string> cmdPrompts, string workingDir, string programExe)
        {
            List<string> outputsList = new List<string>();
            int maxThreads = 1;
            int threadsToUse = 20 / maxThreads;
            int[] threads = Enumerable.Range(0, maxThreads).ToArray();
            Parallel.ForEach(threads, (i) =>
            {
                for (; i < cmdPrompts.Count; i += maxThreads)
                {
                    var prompt = cmdPrompts[i];

                    try
                    {
                        var proc = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = programExe,
                                Arguments = prompt,
                                UseShellExecute = true,
                                CreateNoWindow = false,
                                WorkingDirectory = workingDir
                            }
                        };
                        proc.Start();
                        proc.WaitForExit();
                        lock (outputsList)
                            outputsList.Add(cmdPrompts + ":  " + "Success");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        lock (outputsList)
                            outputsList.Add(cmdPrompts + ":  " + e.Message);
                    }
                }
            });
        }
    }

    internal class ChimeraPaperDataManager
    {
        public string Dataset { get; set; }
        public string CellLine { get; set; }
        public bool IsTopDown { get; set; }
        public string DirectoryPath { get; set; }
        public string DataDirectoryPath { get; set; }

        public Dictionary<string, string> RawFilePaths { get; set; }
        public Dictionary<string, string> CentroidedFilePaths { get; set; }
        //public Dictionary<string, string> CalibratedFilePaths { get; set; }
        public Dictionary<string, string> CalibratedAveragedFilePaths { get; set; }

        public ChimeraPaperDataManager(string dataset, string cellLine, bool isTopDown, string directoryPath,
            string dataDirectoryPath)
        {
            Dataset = dataset;
            CellLine = cellLine;
            IsTopDown = isTopDown;
            DirectoryPath = directoryPath;
            DataDirectoryPath = dataDirectoryPath;

            RawFilePaths = new Dictionary<string, string>();
            CentroidedFilePaths = new Dictionary<string, string>();
            //CalibratedFilePaths = new Dictionary<string, string>();
            CalibratedAveragedFilePaths = new Dictionary<string, string>();

            var rawDir = dataDirectoryPath;
            var centroidedDir = Path.Combine(dataDirectoryPath, "Centroided");
            //var calibratedDir = Path.Combine(dataDirectoryPath, "Calibrated");
            var calibratedAveragedDir = Path.Combine(dataDirectoryPath, "CalibratedAveraged");

            foreach (var rawFile in Directory.GetFiles(rawDir, "*.mzML"))
            {
                var fileName = Path.GetFileNameWithoutExtension(rawFile);
                RawFilePaths.Add(fileName.ConvertFileName(), rawFile);
            }

            foreach (var centroidedFile in Directory.GetFiles(centroidedDir, "*.mzML"))
            {
                var fileName = Path.GetFileNameWithoutExtension(centroidedFile);
                CentroidedFilePaths.Add(fileName.ConvertFileName(), centroidedFile);
            }

            //foreach (var calibratedFile in Directory.GetFiles(calibratedDir, "*.mzML"))
            //{
            //    var fileName = Path.GetFileNameWithoutExtension(calibratedFile);
            //    CalibratedFilePaths.Add(fileName.ConvertFileName(), calibratedFile);
            //}

            foreach (var calibratedAveragedFile in Directory.GetFiles(calibratedAveragedDir, "*.mzML"))
            {
                var fileName = Path.GetFileNameWithoutExtension(calibratedAveragedFile);
                CalibratedAveragedFilePaths.Add(fileName.ConvertFileName(), calibratedAveragedFile);
            }
        }

        public IEnumerable<string> GenerateLibraryTasksBottomUp()
        {
            var files = CalibratedAveragedFilePaths.GroupBy(p => int.Parse(p.Key.Split('_')[1]))
                .ToDictionary(p => p.Key, p => p.ToDictionary(m => m.Key, m => m.Value));

            var oneTwo = new List<string>();
            var oneThree = new List<string>();
            var twoThree = new List<string>();

            oneTwo.AddRange(files[1].Values);
            oneTwo.AddRange(files[2].Values);

            oneThree.AddRange(files[1].Values);
            oneThree.AddRange(files[3].Values);

            twoThree.AddRange(files[2].Values);
            twoThree.AddRange(files[3].Values);

            var dirpath = @"B:\Users\Nic\Chimeras\FdrAnalysis";
            var nonChimericOutFolderOneTwo = Path.Combine(dirpath, "GenerateNonChimericLibrary_Reps1+2");
            var nonChimericOutFolderOneThree = Path.Combine(dirpath, "GenerateNonChimericLibrary_Reps1+3");
            var nonChimericOutFolderTwoThree = Path.Combine(dirpath, "GenerateNonChimericLibrary_Reps2+3");

            var chimericOutFolderOneTwo = Path.Combine(dirpath, "GenerateChimericLibrary_Reps1+2");
            var chimericOutFolderOneThree = Path.Combine(dirpath, "GenerateChimericLibrary_Reps1+3");
            var chimericOutFolderTwoThree = Path.Combine(dirpath, "GenerateChimericLibrary_Reps2+3");

    
            //var nonChimericLineOneTwo = $" -t {ImportantPaths.GptmdNoChimerasMann11} {ImportantPaths.SearchNoChimerasMann11_BuildLibrary}" +
            //                          $" -s {string.Join(" ", oneTwo)} -o {nonChimericOutFolderOneTwo} " +
            //                          $"-d {ImportantPaths.UniprotHumanProteomeAndReviewedXml}";
            //yield return nonChimericLineOneTwo;

            var nonChimericLineOneThree = $" -t {ImportantPaths.GptmdNoChimerasMann11} {ImportantPaths.SearchNoChimerasMann11_BuildLibrary}" +
                                      $" -s {string.Join(" ", oneThree)} -o {nonChimericOutFolderOneThree} " +
                                      $"-d {ImportantPaths.UniprotHumanProteomeAndReviewedXml}";
            yield return nonChimericLineOneThree;


            var nonChimericLineTwoThree = $" -t {ImportantPaths.GptmdNoChimerasMann11} {ImportantPaths.SearchNoChimerasMann11_BuildLibrary}" +
                                      $" -s {string.Join(" ", twoThree)} -o {nonChimericOutFolderTwoThree} " +
                                      $"-d {ImportantPaths.UniprotHumanProteomeAndReviewedXml}";
            yield return nonChimericLineTwoThree;

            //var chimericLineOneTwo = $" -t {ImportantPaths.GptmdWithChimerasMann11} {ImportantPaths.SearchWithChimerasMann11_BuildLibrary}" +
            //                          $" -s {string.Join(" ", oneTwo)} -o {chimericOutFolderOneTwo} " +
            //                          $"-d {ImportantPaths.UniprotHumanProteomeAndReviewedXml}";
            //yield return chimericLineOneTwo;

            var chimericLineOneThree = $" -t {ImportantPaths.GptmdWithChimerasMann11} {ImportantPaths.SearchWithChimerasMann11_BuildLibrary}" +
                                      $" -s {string.Join(" ", oneThree)} -o {chimericOutFolderOneThree} " +
                                      $"-d {ImportantPaths.UniprotHumanProteomeAndReviewedXml}";
            yield return chimericLineOneThree;

            var chimericLineTwoThree = $" -t {ImportantPaths.GptmdWithChimerasMann11} {ImportantPaths.SearchWithChimerasMann11_BuildLibrary}" +
                                      $" -s {string.Join(" ", twoThree)} -o {chimericOutFolderTwoThree} " +
                                      $"-d {ImportantPaths.UniprotHumanProteomeAndReviewedXml}";
            yield return chimericLineTwoThree;

        }

        public IEnumerable<string> InternalComparisonCommandPrompts()
        {
            var files = CalibratedAveragedFilePaths.Where(p => p.Key.StartsWith($"{CellLine}_3_"))
                .Select(p => p.Value)
                .ToArray();

            if (files.Length != 6)
                Debugger.Break();

            var nonChimericOutputFolder = Path.Combine(DirectoryPath, "SearchResults", "MetaMorpheus_105_NoChimeras");
            var chimericOutputFolder = Path.Combine(DirectoryPath, "SearchResults", "MetaMorpheus_105_WithChimeras");
            var gptmd_NoChimerasPath = IsTopDown  ? ImportantPaths.GptmdNoChimerasJurkatTd    : ImportantPaths.GptmdNoChimerasMann11;
            var gptmd_ChimerasPath = IsTopDown    ? ImportantPaths.GptmdWithChimerasJurkatTd  : ImportantPaths.GptmdWithChimerasMann11;
            var search_NoChimerasPath = IsTopDown ? ImportantPaths.SearchNoChimerasJurkatTd   : ImportantPaths.SearchNoChimerasMann11;
            var search_ChimerasPath = IsTopDown   ? ImportantPaths.SearchWithChimerasJurkatTd : ImportantPaths.SearchWithChimerasMann11;
            var nonChimericLibPath = IsTopDown    ? ImportantPaths.NonChimericLibraryJurkatTd : ImportantPaths.NonChimericLibraryMann11;
            var chimericLibPath = IsTopDown       ? ImportantPaths.ChimericLibraryJurkatTd    : ImportantPaths.ChimericLibraryMann11;
            var dbPath = ImportantPaths.UniprotHumanProteomeAndReviewedXml;

            var nonChimericLine =
                $" -t {gptmd_NoChimerasPath} {search_NoChimerasPath} -s {string.Join(" ", files)} -o {nonChimericOutputFolder} -d {dbPath} {nonChimericLibPath}";
            yield return nonChimericLine;

            var chimericLine =
                $" -t {gptmd_ChimerasPath} {search_ChimerasPath} -s {string.Join(" ", files)} -o {chimericOutputFolder} -d {dbPath} {chimericLibPath}";
            yield return chimericLine;
        }
    }
}
