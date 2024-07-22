namespace TaskLayer.ChimeraAnalysis;

public class InternalMetaMorpheusAnalysisParameters : BaseResultAnalyzerTaskParameters
{
    public string DatabasePath { get; set; }
    public string SpectraFileDirectory { get; set; }
    public string OutputDirectory { get; set; }

    public InternalMetaMorpheusAnalysisParameters(string inputDirectoryPath, string outputDirectory,
        string spectraFileDir, string dbPath, bool overrideFiles = false,
        bool runOnAll = true, int maxDegreesOfParallelism = 2)

        : base(inputDirectoryPath, overrideFiles, runOnAll, maxDegreesOfParallelism)
    {
        OutputDirectory = outputDirectory;
        SpectraFileDirectory = spectraFileDir;
        DatabasePath = dbPath;
    }
}