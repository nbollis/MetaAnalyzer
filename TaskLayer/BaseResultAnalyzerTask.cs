using Analyzer.Plotting.Util;
using Analyzer.SearchType;

namespace TaskLayer
{
    public abstract class BaseResultAnalyzerTask
    {
        public static event EventHandler<StringEventArgs> LogHandler;
        public static event EventHandler<StringEventArgs> WarnHandler;
        public abstract MyTask MyTask { get; }
        public string Condition { get; set; }
        public abstract BaseResultAnalyzerTaskParameters Parameters { get; }

        public void Run()
        {
            Log($"Running Task {MyTask}: {Condition}", 0);
            RunSpecific();
            Log($"Finished Running {MyTask}: {Condition}", 0);
            Console.WriteLine();
        }

        protected abstract void RunSpecific();


        protected void Log(string message, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            LogHandler?.Invoke(this, new StringEventArgs(added + message));
        }

        protected static void Warn(string v, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            WarnHandler?.Invoke(null, new StringEventArgs($"{added}Error (Nonfatal): {v}"));
        }

        public static AllResults BuildChimeraPaperResultsObjects(string inputPath, bool runOnAll)
        {
            var allResults = new AllResults(inputPath);
            if (runOnAll)
                return allResults;

            var cellLines = new List<CellLineResults>();
            foreach (var cellLine in allResults)
            {
                var selector = cellLine.GetAllSelectors();

                var runResults = new List<SingleRunResults>();
                foreach (var singleRunResult in cellLine)
                {
                    if (selector.Contains(singleRunResult.Condition))
                        runResults.Add(singleRunResult);
                }
                cellLines.Add(new CellLineResults(cellLine.DirectoryPath, runResults));
            }

            allResults = new AllResults(inputPath, cellLines);
            return allResults;
        }

        protected AllResults BuildChimeraPaperResultsObjects() =>
            BuildChimeraPaperResultsObjects(Parameters.InputDirectoryPath, Parameters.RunOnAll);


        #region CMD

        public static double TotalWeight = 0;
        protected void RunCmdProcess(string prompt, string workingDir, string programExe = "CMD.exe")
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = programExe,
                    Arguments = prompt,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDir
                }
            };
            process.Start();
            process.WaitForExit();
        }

        
        protected async Task RunCmdProcess(CmdProcess cmdProcess)
        {
            Log(cmdProcess.SummaryText);
            RunCmdProcess(cmdProcess.Prompt, cmdProcess.WorkingDirectory);
        }

        #endregion

    }
}
