using Analyzer.Plotting.Util;
using Analyzer.SearchType;

namespace TaskLayer
{
    public abstract class BaseResultAnalyzerTask
    {
        public static event EventHandler<StringEventArgs> LogHandler;
        public static event EventHandler<StringEventArgs> WarnHandler;
        public abstract MyTask MyTask { get; }
        public abstract BaseResultAnalyzerTaskParameters Parameters { get; }

        public void Run()
        {
            Log($"Running Task {MyTask}", 0);
            RunSpecific();
            Log($"Finished Running {MyTask}", 0);
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

        protected AllResults BuildChimeraPaperResultsObjects()
        {
            var allResults = new AllResults(Parameters.InputDirectoryPath);
            if (Parameters.RunOnAll)
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

            allResults = new AllResults(Parameters.InputDirectoryPath, cellLines);
            return allResults;
        }
    }
}
