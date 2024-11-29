namespace RadicalFragmentation;

public interface ICommandLineParameters
{
    string ProgramName { get; }
    string OutputDirectory { get; }
    VerbosityType Verbosity { get; }
    void ValidateCommandLineSettings();
}