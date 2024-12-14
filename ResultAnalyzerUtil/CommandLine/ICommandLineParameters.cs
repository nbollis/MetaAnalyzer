namespace ResultAnalyzerUtil.CommandLine;

public interface ICommandLineParameters
{
    string ProgramName { get; }
    string OutputDirectory { get; }
    VerbosityType Verbosity { get; }
    int MaxThreads { get; }
    void ValidateCommandLineSettings();
}