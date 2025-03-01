using System.Text;
using TaskLayer.CMD;

namespace TaskLayer.CMD;

public class MetaMorpheusGptmdSearchCmdProcess : MetaMorpheusCmdProcess
{
    public bool IsLibraryCreator { get; }

    /// <summary>
    /// For use in the creation of the internal MM comparison searches
    /// </summary>
    /// <param name="spectraPaths"></param>
    /// <param name="dbPath"></param>
    /// <param name="gptmd"></param>
    /// <param name="search"></param>
    /// <param name="outputPath"></param>
    /// <param name="summaryText"></param>
    /// <param name="weight"></param>
    /// <param name="workingDir"></param>
    /// <param name="quickName"></param>
    /// <param name="programExe"></param>
    public MetaMorpheusGptmdSearchCmdProcess(string[] spectraPaths, string dbPath, string gptmd, string search, string outputPath,
        string summaryText, double weight, string workingDir, string? quickName = null, string programExe = "CMD.exe")
        : base(spectraPaths, dbPath, [gptmd, search], outputPath, summaryText, weight, workingDir, quickName, programExe)
    {
        IsLibraryCreator = search.Contains("Build");
    }

    public override bool IsCompleted()
    {
        var spectraFiles = SpectraPaths.Length;
        if (!HasStarted()) return false;

        if (Directory.GetFiles(OutputDirectory, "*.psmtsv", SearchOption.AllDirectories).Length <
            spectraFiles + 3) return false;

        // build library tasks also need to wait on the msp being written out
        if (!IsLibraryCreator) return true;
        var filePath = Directory.GetFiles(OutputDirectory, "*.msp", SearchOption.AllDirectories).FirstOrDefault();
        return filePath != null;
    }
}

public abstract class MetaMorpheusCmdProcess(
    string[] spectraPaths,
    string dbPath,
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
            sb.Append($" -o {OutputDirectory}");
            sb.Append($" -d {DatabasePath}");
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
    public string DatabasePath { get; } = dbPath;
    public string[] TasksTomls { get; } = taskTomlPaths;

    public override bool HasStarted()
    {
        return Directory.Exists(OutputDirectory);
    }
}

public class MetaMorpheusCalibrationCmdProcess(string[] spectraPaths, string dbPath, 
    string calibTomlPath, string outputPath, string summaryText, double weight,
    string workingDir, string? quickName = null, string programExe = "CMD.exe") 
    : MetaMorpheusCmdProcess(spectraPaths, dbPath, [calibTomlPath], outputPath, summaryText, weight, workingDir, quickName, programExe)
{
    public override bool IsCompleted()
    {
        if (!Directory.Exists(OutputDirectory))
            return false;

        var files = Directory.GetFiles(OutputDirectory, "*.mzML", SearchOption.AllDirectories);
        if (files.Length != SpectraPaths.Length)
            return false;

        var calibFiles = files.Count(p => p.Contains("-calib"));
        return calibFiles == SpectraPaths.Length;
    }
}

public class MetaMorpheusAveragingCmdProcess(string[] spectraPaths, string dbPath, 
    string averagingTomlPath, string outputPath, string summaryText, double weight, 
    string workingDir, string? quickName = null, string programExe = "CMD.exe") 
    : MetaMorpheusCmdProcess(spectraPaths, dbPath, [averagingTomlPath], outputPath, summaryText, weight, workingDir, quickName, programExe)
{
    public override bool IsCompleted()
    {
        if (!Directory.Exists(OutputDirectory))
            return false;

        var files = Directory.GetFiles(OutputDirectory, "*.mzML", SearchOption.AllDirectories);
        if (files.Length != SpectraPaths.Length)
            return false;

        var averagedFiles = files.Count(p => p.Contains("-averaged"));
        return averagedFiles == SpectraPaths.Length;
    }
}