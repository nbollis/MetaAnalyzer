using Analyzer.Plotting.Util;
using Analyzer.SearchType;

namespace TaskLayer.ChimeraAnalysis;

public class SingleRunAnalysisParameters : BaseResultAnalyzerTaskParameters
{
    public DistributionPlotTypes PlotType { get; set; }
    public SingleRunResults RunResult { get; init; }
    public SingleRunAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll, 
        SingleRunResults runResult, DistributionPlotTypes distributionPlotType = DistributionPlotTypes.ViolinPlot) 
        : base(inputDirectoryPath, overrideFiles, runOnAll)
    {
        RunResult = runResult;
        PlotType = distributionPlotType;
    }
}

public class CellLineAnalysisParameters : BaseResultAnalyzerTaskParameters
{
    public CellLineResults CellLine { get; init; }
    public CellLineAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll, 
        CellLineResults cellLine) 
        : base(inputDirectoryPath, overrideFiles, runOnAll)
    {
        CellLine = cellLine;
    }
}

public class AllResultsAnalysisParameters : BaseResultAnalyzerTaskParameters
{
    public AllResults AllResults { get; init; }
    public AllResultsAnalysisParameters(string inputDirectoryPath, bool overrideFiles, bool runOnAll,
        AllResults allResults) : base(inputDirectoryPath, overrideFiles, runOnAll)
    {
        AllResults = allResults;
    }
}