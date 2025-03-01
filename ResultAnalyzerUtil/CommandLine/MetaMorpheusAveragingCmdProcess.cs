namespace ResultAnalyzerUtil.CommandLine;

public class MetaMorpheusAveragingCmdProcess(string[] spectraPaths, string dbPath, 
    string averagingTomlPath, string outputPath, string summaryText, double weight, 
    string workingDir, string? quickName = null) 
    : MetaMorpheusCmdProcess(spectraPaths, dbPath, [averagingTomlPath], outputPath, summaryText, weight, workingDir, quickName)
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