using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace TaskLayer;

public class CmdProcess
{
    internal string Prompt 
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($" -t {GptmdTask} {SearchTask}");
            sb.Append($" -s {string.Join(" ", SpectraPaths)}");
            sb.Append($" -o {OutputDirectory}");
            sb.Append($" -d {DatabasePath}");
            if (Dependency != null)
                sb.Append($" {Dependency.Task.Result}");
            sb.Append(" -v minimal");
            return sb.ToString();
        }
    }
    internal string SummaryText { get; init; }
    public double Weight { get; }
    public string OutputDirectory { get; }
    public string[] SpectraPaths { get; }
    public string DatabasePath { get; }
    public string GptmdTask { get; }
    public string SearchTask { get; }


    internal string WorkingDirectory { get; init; }
    internal string ProgramExe { get; init; }
    public TaskCompletionSource<string> CompletionSource { get; } = new TaskCompletionSource<string>();
    public TaskCompletionSource<string>? Dependency { get; private set; }

    
    public CmdProcess(string[] spectraPaths, string dbPath, string gptmd, string search, string outputPath, string summaryText, double weight, string workingDir,
        string programExe = "CMD.exe")
    {
        SpectraPaths = spectraPaths;
        DatabasePath = dbPath;
        GptmdTask = gptmd;
        SearchTask = search;
        WorkingDirectory = outputPath;
        SummaryText = summaryText;
        Weight = weight;
        OutputDirectory = outputPath;

        WorkingDirectory = workingDir;
        ProgramExe = programExe;
    }



    public void DependsOn(CmdProcess other)
    {
        Dependency = other.CompletionSource;
    }

    public bool IsCompleted()
    {
        var spectraFiles = Prompt.Split('-').First(p => p.StartsWith('s'))
            .Split(' ').Count(p => p.Contains(".mzML", StringComparison.InvariantCultureIgnoreCase));
        if (HasStarted())
            if (Directory.GetFiles(OutputDirectory, "*.psmtsv", SearchOption.AllDirectories).Length >= spectraFiles + 2)
                return true;
        return false;
    }

    public bool HasStarted()
    {
        return Directory.Exists(OutputDirectory);
    }



}