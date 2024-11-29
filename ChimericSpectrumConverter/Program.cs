using System.Diagnostics;
using CMD;
using CommandLine;
using CommandLine.Text;
using ResultAnalyzerUtil.CommandLine;
using TaskLayer;

namespace ChimericSpectrumConverter;

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

    public static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
    {
        int errorCode = 0;

        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.Copyright = "";
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);

        helpText.MaximumDisplayWidth = 300;

        helpText.AddPostOptionsLine("Example usage (Windows): ");
        helpText.AddPostOptionsLine("CMD.exe -d C:\\ExampleDatabase.fasta -s C:\\ExampleSpectra.mzML -t C:\\ExampleTask.toml");
        helpText.AddPostOptionsLine(Environment.NewLine);

        helpText.AddPostOptionsLine("Example usage (Linux): ");
        helpText.AddPostOptionsLine("dotnet CMD.dll -d home/mydata/ExampleDatabase.fasta -s home/mydata/ExampleSpectra.mzML -t home/mydata/ExampleTask.toml");
        helpText.AddPostOptionsLine(Environment.NewLine);

        Console.WriteLine(helpText);

        if (errs.Any(x => x.Tag != ErrorType.HelpRequestedError))
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

            CrashHandler(e, errorCode);
            return errorCode;
        }

        BaseResultAnalyzerTask.LogHandler += LogHandler;
        BaseResultAnalyzerTask.WarnHandler += WarnHandler;

        TaskCollectionRunner runner;
        try
        {
            runner = new(new List<BaseResultAnalyzerTask>()
            {
                new ChimericSpectrumConverterTask(new ChimericSpectrumConverterParameters(null, CommandLineArguments.OutputFolder, CommandLineArguments.Spectra, CommandLineArguments.MaxCharge, CommandLineArguments.OverrideFiles, true, CommandLineArguments.MaxDegreeOfParallelism))
            });
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
            runner.Run();
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

    private static void LogHandler(object? sender, StringEventArgs e)
    {
        Console.WriteLine($"{DateTime.Now:T}: {e.Message}");
    }

    private static void WarnHandler(object? sender, StringEventArgs e)
    {
        Console.WriteLine($"{DateTime.Now:T}: {e.Message}");
    }

    private static void CrashHandler(Exception e, int errorCode)
    {
        string path = Path.Combine(CommandLineArguments.OutputFolder, "ErrorLog.txt");
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