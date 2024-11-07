using CommandLine;
using CommandLine.Text;
using RadicalFragmentation.Processing;
using System.Diagnostics;

namespace RadicalFragmentation
{
    internal class Program
    {
        private static CommandLineArguments CommandLineArguments;
        public static int Main(string[] args)
        {
            // an error code of 0 is returned if the program ran successfully.
            // otherwise, an error code of >0 is returned.
            // this makes it easier to determine via scripts when the program fails.
            int errorCode = 0;

            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<CommandLineArguments>(args);

            parserResult
                .WithParsed<CommandLineArguments>(options => errorCode = Run(options))
                .WithNotParsed(errs => errorCode = DisplayHelp(parserResult, errs));

            return errorCode;
        }

        public static int Run(CommandLineArguments settings)
        {
            int errorCode = 0;

            // validate command line settings
            try
            {
                settings.ValidateCommandLineSettings();
                CommandLineArguments = settings;
            }
            catch (Exception e)
            {
                Console.WriteLine("Result Analyzer encountered the following error:" + Environment.NewLine + e.Message);
                errorCode = 2;

                CrashHandler(e, errorCode);
                return errorCode;
            }

            // construct runner
            RadicalFragmentationExplorer explorer;
            try
            {
                string dbPath = CommandLineArguments.DatabasePath;
                int mods = CommandLineArguments.ModsToConsider;
                string label = CommandLineArguments.Label ?? "Human";
                int ambigLevel = CommandLineArguments.AmbiguityLevel;
                string outDir = CommandLineArguments.OutputDirectory!;

                switch (CommandLineArguments.FragmentExplorerType)
                {
                    case FragmentExplorerType.Tryptophan:
                        explorer = new TryptophanFragmentationExplorer(dbPath, mods, label, ambigLevel, outDir);
                        break;
                    case FragmentExplorerType.Cysteine:
                        explorer = new CysteineFragmentationExplorer(dbPath, mods, label, ambigLevel, int.MaxValue, outDir);
                        break;
                    case FragmentExplorerType.ETD:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine("Result Analyzer encountered the following error:" + Environment.NewLine + e.Message);
                errorCode = 3;

                CrashHandler(e, errorCode);
                return errorCode;
            }

            // run tasks
            try
            {
                explorer.CreateIndexedFile();
                explorer.CreateFragmentHistogramFile();
                explorer.FindNumberOfFragmentsNeededToDifferentiate();
            }
            catch (Exception e)
            {
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                }

                Debugger.Break();

                var message = "Run failed, Exception: " + e.Message;
                Console.WriteLine("Result Analyzer encountered the following error:" + message);
                errorCode = 4;
                CrashHandler(e, errorCode);
                return errorCode;
            }

            return errorCode;
        }

        public static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
        {
            Console.WriteLine("Welcome to Result Analyzer");
            int errorCode = 0;

            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Copyright = "";
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);

            if (errors.Any(x => x.Tag != ErrorType.HelpRequestedError))
            {
                errorCode = 1;
            }

            return errorCode;
        }

        private static void LogHandler(object? sender, StringEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now:T}: {e.Message}");
        }

        private static void WarnHandler(object? sender, StringEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now:T}: {e.Message}");
        }

        private static void CrashHandler(object? sender, StringEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now:T}: {e.Message}");
        }

        private static void CrashHandler(Exception e, int errorCode)
        {
            string path = Path.Combine(CommandLineArguments.OutputDirectory, "ErrorLog.txt");
            using (var sw = new StreamWriter(File.Create(path)))
            {
                sw.WriteLine($"Exception was thrown with error code {errorCode}");
                sw.WriteLine();
                sw.WriteLine(e.Message);
                sw.WriteLine();
                sw.WriteLine("Stack Trace: ");
                sw.WriteLine();
                sw.WriteLine(e.StackTrace);
            }
        }
    }
}
