namespace ResultAnalyzerUtil.CommandLine;

public class MetaMorpheusGptmdSearchCmdProcess : MetaMorpheusCmdProcess
{
    public bool IsLibraryCreator { get; }

    /// <summary>
    /// For use in the creation of the internal MM comparison searches
    /// </summary>
    public MetaMorpheusGptmdSearchCmdProcess(string[] spectraPaths, string[] dbPaths, string gptmd, string search, string outputPath,
        string summaryText, double weight, string workingDir, string? quickName = null, string programExe = "CMD.exe")
        : base(spectraPaths, dbPaths, [gptmd, search], outputPath, summaryText, weight, workingDir, quickName, programExe)
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
        if (!IsLibraryCreator)
        {
            CompletionSource.SetResult(OutputDirectory);
            return true;
        }
        var filePath = Directory.GetFiles(OutputDirectory, "*.msp", SearchOption.AllDirectories).FirstOrDefault();

        if (filePath == null) 
            return false;

        CompletionSource.SetResult(filePath);
        return true;
    }
}