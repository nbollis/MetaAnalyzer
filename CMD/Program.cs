using System;
using Analyzer.Plotting.Util;
using CMD;
using CMD.TaskParameters;
using CMD.Tasks;
using CMD.Util;
using CommandLine;
using CommandLine.Text;
using Plotly.NET;

namespace MyApp
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

                return errorCode;
            }

            BaseResultAnalyzerTask.LogHandler += LogHandler;
            BaseResultAnalyzerTask.WarnHandler += WarnHandler;


            // construct tasks
            TaskCollectionRunner runner;
            try
            {
                List<BaseResultAnalyzerTask> allTasks = new();
                foreach (var taskType in CommandLineArguments.Tasks)
                {
                    BaseResultAnalyzerTask task;
                    switch (taskType)
                    {
                        case CommandLineTasks.JenkinsLikeRunParser:
                            var parameters = new JenkinsLikeRunParserTaskParameters(CommandLineArguments.InputDirectory,
                                CommandLineArguments.OverrideFiles);
                            task = new JenkinsLikeRunParserTask(parameters);
                            break;
                    
                        default:
                            throw new ArgumentOutOfRangeException();
                    }


                    allTasks.Add(task);
                }

                runner = new(allTasks);
            }
            catch (Exception e)
            {
                Console.WriteLine("Result Analyzer encountered the following error:" + Environment.NewLine + e.Message);
                errorCode = 3;

                return errorCode;
            }

            // run tasks
            try
            {
                runner.Run();
            }
            catch (Exception e)
            {
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                }

                var message = "Run failed, Exception: " + e.Message;
                Console.WriteLine("Result Analyzer encountered the following error:" + message);
                errorCode = 4;
                return errorCode;
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
    }
}