namespace MonteCarlo;

public interface ISimulationResultHandler
{
    string OutputDirectory { get; }
    void HandleResult(SimulationResult result);
}


