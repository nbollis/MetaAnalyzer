using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD.TaskParameters
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
