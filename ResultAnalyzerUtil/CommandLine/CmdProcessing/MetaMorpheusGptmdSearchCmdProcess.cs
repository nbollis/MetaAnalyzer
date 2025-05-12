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
        if (!HasStarted()) 
            return false;

        // Library creator will not have individual result files. We only need to find the .msp
        if (IsLibraryCreator)
        {
            var filePath = Directory.GetFiles(OutputDirectory, "*.msp", SearchOption.AllDirectories).FirstOrDefault();
            if (filePath == null) 
                return false;

            CompletionSource.SetResult(filePath);
            return true;
        }

        // A non-library creator will have the IndividualResultFiles Directory all populated. 
        int psmFileCount = Directory.GetFiles(OutputDirectory, "*.psmtsv", SearchOption.AllDirectories).Length;
        if (!IsLibraryCreator && spectraFiles * 2 + 2 <= psmFileCount) // * 2 for individual PSM and Peptides. +2 for global psm, pep
        {
            CompletionSource.SetResult(OutputDirectory);
            return true;
        }

        return false; 
    }
}