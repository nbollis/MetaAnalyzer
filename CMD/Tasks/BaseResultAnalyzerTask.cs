using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Plotting.Util;
using CMD.TaskParameters;
using CMD.Util;

namespace CMD.Tasks
{
    public abstract class BaseResultAnalyzerTask
    {
        public static event EventHandler<StringEventArgs> LogHandler;
        public static event EventHandler<StringEventArgs> WarnHandler;
        public abstract CommandLineTasks MyTask { get; }
        protected abstract BaseResultAnalyzerTaskParameters Parameters { get; }

        protected BaseResultAnalyzerTask() { }

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
            LogHandler?.Invoke(this, new StringEventArgs(added+message));
        }

        protected static void Warn(string v, int nestLayer = 1)
        {
            string added = string.Join("", Enumerable.Repeat("\t", nestLayer));
            WarnHandler?.Invoke(null, new StringEventArgs($"{added}Error (Nonfatal): {v}"));
        }
    }
}
