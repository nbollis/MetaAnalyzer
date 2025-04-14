using System.Text;

namespace ResultAnalyzerUtil.CommandLine;

public abstract class MetaMorpheusCmdProcess(
    string[] spectraPaths,
    string[] dbPaths,
    string[] taskTomlPaths,
    string outputPath,
    string summaryText,
    double weight,
    string workingDir,
    string? quickName = null,
    string programExe = "CMD.exe")
    : CmdProcess(summaryText, weight, workingDir, quickName, programExe)
{
    public override string Prompt
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($" -t {string.Join(" ", TasksTomls)}");
            sb.Append($" -s {string.Join(" ", SpectraPaths)}");
            sb.Append($" -s {string.Join(" ", DatabasePaths)}");
            sb.Append($" -o {OutputDirectory}");
            if (Dependency != null)
            {
                sb.Append($" {Dependency.Task.Result}");
            }

            var promptstring = sb.ToString();
            var start = OutputDirectory.Substring(0, 3);
            promptstring = promptstring.Replace(@"B:\", start);

            return promptstring;
        }
    }
    public string OutputDirectory { get; } = outputPath;
    public string[] SpectraPaths { get; } = spectraPaths;
    public string[] DatabasePaths { get; } = dbPaths;
    public string[] TasksTomls { get; } = taskTomlPaths;

    public override bool HasStarted()
    {
        return Directory.Exists(OutputDirectory);
    }
}