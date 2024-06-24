namespace TaskLayer
{
    public abstract class BaseResultAnalyzerTaskParameters
    {
        public string InputDirectoryPath { get; }
        public bool Override { get; set; }
        public bool RunOnAll { get; set; }

        protected BaseResultAnalyzerTaskParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll)
        {
            InputDirectoryPath = inputDirectoryPath;
            Override = overrideFiles;
            RunOnAll = runOnAll;
        }
    }
}
