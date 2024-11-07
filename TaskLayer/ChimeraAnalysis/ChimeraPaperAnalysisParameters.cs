namespace TaskLayer.ChimeraAnalysis
{
    public class ChimeraPaperAnalysisParameters : BaseResultAnalyzerTaskParameters
    {
        public bool RunChimeraBreakdown { get; set; }
        public bool RunFdrAnalysis { get; set; }
        public bool RunResultCounting { get; set; }
        public bool CountChimericResults { get; set; }
        public bool RunModificationAnalysis { get; set; }

        public ChimeraPaperAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runChimeraBreakdown, bool runOnAll,
            bool runFdrAnalysis, bool runResultCounting, bool countChimericResults, bool runModificationAnalysis) : base(inputDirectoryPath, overrideFiles, runOnAll)
        {
            RunChimeraBreakdown = runChimeraBreakdown;
            RunFdrAnalysis = runFdrAnalysis;
            RunResultCounting = runResultCounting;
            CountChimericResults = countChimericResults;
            RunModificationAnalysis = runModificationAnalysis;
        }
    }
}
