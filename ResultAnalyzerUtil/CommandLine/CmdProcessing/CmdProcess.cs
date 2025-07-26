namespace ResultAnalyzerUtil.CommandLine;

public abstract class CmdProcess
{
    public abstract string Prompt { get; }
    public string SummaryText { get; init; }
    public string QuickName { get; }
    public double Weight { get; }
    public string WorkingDirectory { get; init; }
    public string ProgramExe { get; init; }
    public TaskCompletionSource<string> CompletionSource { get; } = new();
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
        if (other == this || IsCircularDependency(other))
            throw new InvalidOperationException("Circular dependency detected.");

        Dependency = other.CompletionSource;
    }

    public abstract bool HasStarted();

    // If completion source is needed, be sure to set that before returning true
    public abstract bool IsCompleted();


    // for adaptors, not a good pattern, but here we are. 
    public bool IsCmdTask { get; protected set; } = true;
    public virtual async Task RunTask()
    {
        var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ProgramExe,
                Arguments = Prompt,
                UseShellExecute = true,
                CreateNoWindow = false,
                WorkingDirectory = WorkingDirectory,
                ErrorDialog = true,
            }
        };
        proc.Start();
        await proc.WaitForExitAsync();
    }

    private bool IsCircularDependency(CmdProcess other)
    {
        var current = other;
        while (current != null)
        {
            if (current == this)
                return true;

            current = current.Dependency?.Task.AsyncState as CmdProcess;
        }
        return false;
    }
}