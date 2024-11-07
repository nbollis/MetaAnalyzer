using Analyzer.Plotting.Util;
using TaskLayer;

namespace CMD
{
    public class TaskCollectionRunner
    {
        public List<BaseResultAnalyzerTask> AllTaskList { get; }

        public TaskCollectionRunner(List<BaseResultAnalyzerTask> allTasks)
        {
            AllTaskList = allTasks;
        }

        #region CMD

        public static event EventHandler<StringEventArgs>? CrashHandler;
        protected static void ReportCrash(string message, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            CrashHandler?.Invoke(null, new StringEventArgs($"{added}Error (Fatal): {message}"));
        }

        #endregion



        public void Run()
        {
            foreach (var task in AllTaskList)
            {
                try
                {
                    task.Run().Wait();
                }
                catch (Exception e)
                {
                    ReportCrash($"{task.MyTask} failed with message {e.Message} at\n{e.StackTrace}");
                }
            }
        }
    }
}
