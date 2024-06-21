namespace TaskLayer
{
    public abstract class BaseResultAnalyzerTaskParameters
    {
        public string InputDirectoryPath { get; }
        public bool Override { get; set; }

        protected BaseResultAnalyzerTaskParameters(string inputDirectoryPath, bool overrideFiles)
        {
            InputDirectoryPath = inputDirectoryPath;
            Override = overrideFiles;
        }
    }
}
