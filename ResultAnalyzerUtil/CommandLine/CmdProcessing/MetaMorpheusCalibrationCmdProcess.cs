namespace ResultAnalyzerUtil.CommandLine;

public class MetaMorpheusCalibrationCmdProcess(string[] spectraPaths, string[] dbPaths,
    string calibTomlPath, string outputPath, string summaryText, double weight,
    string workingDir, string? quickName = null)
    : MetaMorpheusCmdProcess(spectraPaths, dbPaths, [calibTomlPath], outputPath, summaryText, weight, workingDir, quickName)
{
    public override bool IsCompleted()
    {
        if (!Directory.Exists(OutputDirectory))
            return false;

        var files = Directory.GetFiles(OutputDirectory, "*.mzML", SearchOption.AllDirectories);
        if (files.Length != SpectraPaths.Length)
            return false;

        CompletionSource.SetResult(string.Join(' ', files));
        return files.Length == SpectraPaths.Length;
    }
}