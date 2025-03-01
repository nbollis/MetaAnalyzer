namespace ResultAnalyzerUtil.CommandLine;

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