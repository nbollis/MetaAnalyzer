using Analyzer.Plotting.IndividualRunPlots;
using Analyzer.SearchType;

namespace TaskLayer.ChimeraAnalysis;

public class SingleRunCzeRescoreTask : BaseResultAnalyzerTask
{
    public override MyTask MyTask => MyTask.SingleRunCzeRescore;
    public override SingleRunAnalysisParameters Parameters { get; }

    public SingleRunCzeRescoreTask(SingleRunAnalysisParameters parameters)
    {
        Parameters = parameters;
    }

    protected override void RunSpecific()
    {
        MetaMorpheusResult run;
        switch (Parameters.RunResult)
        {
            case MetaMorpheusResult mm:
                run = mm;
                break;
            case null:
                run = new MetaMorpheusResult(Parameters.SingleRunResultsDirectoryPath);
                break;
            default:
                return;
        }

        run.Override = Parameters.Override;
        _ = run.RetentionTimePredictionFile;
        run.PlotCzePredictions();
        run.PlotCzeDeltaKernelPDF();
    }
}
