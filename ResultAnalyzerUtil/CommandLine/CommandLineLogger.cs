using ResultAnalyzerUtil.CommandLine;
using System.Text.RegularExpressions;

namespace RadicalFragmentation
{
    public static class CommandLineLogger
    {

        public static System.CodeDom.Compiler.IndentedTextWriter MyWriter
            = new System.CodeDom.Compiler.IndentedTextWriter(Console.Out, "\t");

        public static ICommandLineParameters? CommandLineParameters { get; private set; } = null!;

        public static void Initialize(ICommandLineParameters commandLineParameters)
        {
            CommandLineParameters = commandLineParameters;
            MyWriter.Indent = 0;
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
                WriteMultiLineIndented("Log: " + e.Message);
            }
        }

        public static void FinishedWritingFileHandler(object? sender, SingleFileEventArgs e)
        {
            if (CommandLineParameters?.Verbosity == VerbosityType.Normal)
            {
                WriteMultiLineIndented("Finished writing file: " + Path.GetFileNameWithoutExtension(e.WrittenFile));
            }
        }

        public static void StartingSubProcessHandler(object? sender, SubProcessEventArgs e)
        {
            if (CommandLineParameters?.Verbosity == VerbosityType.Normal)
            {
                WriteMultiLineIndented(e.SubProcessIdentifier);
                MyWriter.Indent++;
            }
        }

        public static void FinishedSubProcessHandler(object? sender, SubProcessEventArgs e)
        {
            if (CommandLineParameters?.Verbosity != VerbosityType.None)
            {
                MyWriter.Indent--;
                WriteMultiLineIndented(e.SubProcessIdentifier);
            }
        }
    }
}
