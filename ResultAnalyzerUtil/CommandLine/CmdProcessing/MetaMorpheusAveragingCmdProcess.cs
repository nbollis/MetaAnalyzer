namespace ResultAnalyzerUtil.CommandLine;

public class MetaMorpheusAveragingCmdProcess(string[] spectraPaths, string[] dbPaths,
    string averagingTomlPath, string outputPath, string summaryText, double weight,
    string workingDir, string? quickName = null)
    : MetaMorpheusCmdProcess(spectraPaths, dbPaths, [averagingTomlPath], outputPath, summaryText, weight, workingDir, quickName)
{
    public override bool IsCompleted()
    {
        if (!Directory.Exists(OutputDirectory))
            return false;

        var files = Directory.GetFiles(OutputDirectory, "*.mzML", SearchOption.AllDirectories);
        if (files.Length != SpectraPaths.Length)
            return false;

        var averagedFiles = files.Count(p => p.Contains("-averaged"));
        if (averagedFiles != SpectraPaths.Length) 
            return false;

        CompletionSource.SetResult(string.Join(' ', files));
        return true;
    }
}