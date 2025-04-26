namespace MonteCarlo;

public interface ISimulationResultHandler
{
    string SummaryText { get; set; }
    string OutputDirectory { get; }
    void HandleResult(SimulationResult result, int iteration);
    void DoPostProcessing();
}


