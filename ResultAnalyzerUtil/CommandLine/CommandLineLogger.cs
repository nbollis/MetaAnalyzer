using System.Text.RegularExpressions;

namespace ResultAnalyzerUtil.CommandLine
{
    public static class CommandLineLogger
    {

        public static System.CodeDom.Compiler.IndentedTextWriter MyWriter
            = new System.CodeDom.Compiler.IndentedTextWriter(Console.Out, "\t");

        public static ICommandLineParameters? CommandLineParameters { get; private set; } = null!;
        public static Dictionary<string, ProgressBar> ProgressBars;


        public static void Initialize(ICommandLineParameters commandLineParameters)
        {
            CommandLineParameters = commandLineParameters;
            MyWriter.Indent = 0;
            ProgressBars = new Dictionary<string, ProgressBar>();
        }

        private static void WriteMultiLineIndented(string toWrite)
        {
            string[] tokens = Regex.Split(toWrite, @"\r?\n|\r");
            foreach (var str in tokens)
            {
                Console.Write($"{DateTime.Now:T}\t");
                MyWriter.WriteLine($"{str}");
            }
        }

        public static void WarnHandler(object? sender, StringEventArgs e)
        {
            if (CommandLineParameters?.Verbosity != VerbosityType.None)
            {
                WriteMultiLineIndented("WARN: " + e.Message);
            }
        }

        public static void LogHandler(object? sender, StringEventArgs e)
        {
            if (CommandLineParameters?.Verbosity == VerbosityType.Normal)
            {
                WriteMultiLineIndented(e.Message);
            }
        }

        public static void FinishedWritingFileHandler(object? sender, SingleFileEventArgs e)
        {
            if (CommandLineParameters?.Verbosity == VerbosityType.Normal)
            {
                MyWriter.Indent++;
                WriteMultiLineIndented("Finished writing file: " + Path.GetFileNameWithoutExtension(e.WrittenFile));
                MyWriter.Indent--;
            }
        }

        public static void StartingSubProcessHandler(object? sender, SubProcessEventArgs e)
        {
            if (CommandLineParameters?.Verbosity == VerbosityType.Normal)
            {
                WriteMultiLineIndented("Started: " + e.SubProcessIdentifier);
                MyWriter.Indent++;
            }
        }

        public static void FinishedSubProcessHandler(object? sender, SubProcessEventArgs e)
        {
            if (CommandLineParameters?.Verbosity != VerbosityType.None)
            {
                MyWriter.Indent--;
                WriteMultiLineIndented("Finished:" + e.SubProcessIdentifier);
            }
        }

        public static void ReportProgressHandler(object? sender, ProgressBarEventArgs e)
        {
            if (CommandLineParameters?.Verbosity == VerbosityType.Normal)
            {
                if (!ProgressBars.ContainsKey(e.ProgressBarName))
                {
                    ProgressBars[e.ProgressBarName] = new ProgressBar(e.ProgressBarName);
                }
                ProgressBars[e.ProgressBarName].Report(e.Progress);

                if (Math.Abs(e.Progress - 1) < 0.0000000001)
                {
                    ProgressBars[e.ProgressBarName].Dispose();
                    ProgressBars.Remove(e.ProgressBarName);
                }
            }
        }
    }
}
