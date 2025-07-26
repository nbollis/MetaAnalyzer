using Analyzer.Plotting.Util;
using Analyzer.SearchType;
using ResultAnalyzerUtil.CommandLine;

namespace TaskLayer
{
    public abstract class BaseResultAnalyzerTask : TaskManager
    {
        public static event EventHandler<StringEventArgs> LogHandler;
        public static event EventHandler<StringEventArgs> WarnHandler;
        public abstract MyTask MyTask { get; }
        public string Condition { get; set; }
        public abstract BaseResultAnalyzerTaskParameters Parameters { get; }

        public async Task Run()
        {
            Log($"Running Task {MyTask}: {Condition}", 0);
            
            await Task.Run(RunSpecific);
            
            Log($"Finished Running {MyTask}: {Condition}", 0);
            
            Console.WriteLine();
        }

        protected abstract void RunSpecific();


        protected static void Log(string message, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            LogHandler?.Invoke(null, new StringEventArgs(added + message));
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

    }
}
