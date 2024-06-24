using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskLayer.ChimeraAnalysis
{
    public class ChimeraPaperAnalysisParameters : BaseResultAnalyzerTaskParameters
    {
        public bool RunOnAll { get; set; }
        public bool RunChimeraBreakdown { get; set; }
        public bool RunFdrAnalysis { get; set; }
        public bool RunResultCounting { get; set; }
        public bool CountChimericResults { get; set; }

        public ChimeraPaperAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runChimeraBreakdown, bool runOnAll,
            bool runFdrAnalysis, bool runResultCounting, bool countChimericResults) : base(inputDirectoryPath, overrideFiles)
        {
            RunChimeraBreakdown = runChimeraBreakdown;
            RunOnAll = runOnAll;
            RunFdrAnalysis = runFdrAnalysis;
            RunResultCounting = runResultCounting;
            CountChimericResults = countChimericResults;
        }
    }
}
