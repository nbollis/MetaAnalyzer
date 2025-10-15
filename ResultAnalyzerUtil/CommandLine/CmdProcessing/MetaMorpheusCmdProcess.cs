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
            sb.Append("-t ");
            foreach (var taskToml in TasksTomls)
                sb.Append($"\"{taskToml}\" ");

            sb.Append("-s ");
            foreach (var spectra in SpectraPaths)
                sb.Append($"\"{spectra}\" ");

            sb.Append("-d ");
            foreach (var db in DatabasePaths)
                sb.Append($"\"{db}\" ");

            sb.Append($"-o {OutputDirectory} ");
            if (Dependency != null)
            {
                sb.Append($" {Dependency.Task.Result}");
            }

            var promptstring = sb.ToString();

            // Rare: Computer specific path switches. 
            var userDir = Path.GetFullPath(System.Reflection.Assembly.GetEntryAssembly()?.Location);
            if (userDir.Contains("Artemis"))
                promptstring = promptstring.Replace(@"B:\", @"Z:\");

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