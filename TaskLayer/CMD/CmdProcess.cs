namespace TaskLayer.CMD;

public abstract class CmdProcess
{
    public abstract string Prompt { get; }
    public string SummaryText { get; init; }
    public string QuickName { get; }
    public double Weight { get; }
    public string WorkingDirectory { get; init; }
    public string ProgramExe { get; init; }
    public TaskCompletionSource<string> CompletionSource { get; } = new TaskCompletionSource<string>();
    public TaskCompletionSource<string>? Dependency { get; private set; }

    protected CmdProcess(string summaryText, double weight, string workingDir, string? quickName = null,
        string programExe = "CMD.exe")
    {
        SummaryText = summaryText;
        Weight = weight;
        WorkingDirectory = workingDir;
        QuickName = quickName ?? summaryText;
        ProgramExe = programExe;
    }

    public void DependsOn(CmdProcess other)
    {
        Dependency = other.CompletionSource;
    }

    public abstract bool HasStarted();

    // If completion source is needed, be sure to set that before returning true
    public abstract bool IsCompleted();

}