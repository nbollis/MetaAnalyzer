namespace ResultAnalyzerUtil.CommandLine;

public class MetaMorpheusCalibrationCmdProcess(string[] spectraPaths, string dbPath,
    string calibTomlPath, string outputPath, string summaryText, double weight,
    string workingDir, string? quickName = null)
    : MetaMorpheusCmdProcess(spectraPaths, dbPath, [calibTomlPath], outputPath, summaryText, weight, workingDir, quickName)
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