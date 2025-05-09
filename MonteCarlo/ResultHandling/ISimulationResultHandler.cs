namespace MonteCarlo;

public interface ISimulationResultHandler
{
    public string ConditionIdentifier { get; set; }
    string SummaryText { get; set; }
    string OutputDirectory { get; }
    void HandleResult(SimulationResult result, int iteration);
    void HandleBestScoreRecord(IndividualScoreRecord scoreRecord);
    void DoPostProcessing();
}


