using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.SearchType;

namespace TaskLayer.ChimeraAnalysis
{
    public class ChimeraPaperAnalysisParameters : BaseResultAnalyzerTaskParameters
    {
        public bool RunOnAll { get; set; }
        public bool RunChimeraBreakdown { get; set; }
        public bool RunFdrAnalysis { get; set; }
        public bool RunResultCounting { get; set; }
        public bool CountChimericResults { get; set; }
        public bool RunModificationAnalysis { get; set; }

        public ChimeraPaperAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runChimeraBreakdown, bool runOnAll,
            bool runFdrAnalysis, bool runResultCounting, bool countChimericResults, bool runModificationAnalysis) : base(inputDirectoryPath, overrideFiles, runOnAll)
        {
            RunChimeraBreakdown = runChimeraBreakdown;
            RunOnAll = runOnAll;
            RunFdrAnalysis = runFdrAnalysis;
            RunResultCounting = runResultCounting;
            CountChimericResults = countChimericResults;
            RunModificationAnalysis = runModificationAnalysis;
        }
    }

    public class SingleRunAnalysisParameters : BaseResultAnalyzerTaskParameters
    {
        public SingleRunResults RunResult { get; init; }
        public SingleRunAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll, SingleRunResults runResult) : base(inputDirectoryPath, overrideFiles, runOnAll)
        {
            RunResult = runResult;
        }
    }
}
