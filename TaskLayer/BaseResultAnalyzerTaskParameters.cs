namespace TaskLayer
{
    public abstract class BaseResultAnalyzerTaskParameters
    {
        public string InputDirectoryPath { get; }
        public bool Override { get; set; }
        public bool RunOnAll { get; set; }
        public int MaxDegreesOfParallelism { get; set; } = 2;

        protected BaseResultAnalyzerTaskParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll)
        {
            InputDirectoryPath = inputDirectoryPath;
            Override = overrideFiles;
            RunOnAll = runOnAll;
        }
    }
}
