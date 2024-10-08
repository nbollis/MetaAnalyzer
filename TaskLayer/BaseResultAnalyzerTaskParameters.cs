﻿namespace TaskLayer
{
    public class BaseResultAnalyzerTaskParameters
    {
        public string InputDirectoryPath { get; }
        public bool Override { get; set; }
        public bool RunOnAll { get; set; }
        public int MaxDegreesOfParallelism { get; set; }

        public BaseResultAnalyzerTaskParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll, int maxDegreesOfParallelism = 2)
        {
            InputDirectoryPath = inputDirectoryPath;
            Override = overrideFiles;
            RunOnAll = runOnAll;
            MaxDegreesOfParallelism = maxDegreesOfParallelism;
        }
    }

   

}
