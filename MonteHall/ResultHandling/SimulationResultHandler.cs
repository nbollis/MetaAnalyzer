namespace MonteCarlo;

public class SimulationResultHandler : ISimulationResultHandler
{
    public string OutputDirectory { get; private set; }
    public Dictionary<int, List<double>> ScoresByIteration { get; }

    public SimulationResultHandler(string outputDirectory)
    {
        ScoresByIteration = new();
        OutputDirectory = outputDirectory;
    }

    public void HandleResult(SimulationResult result, int iteration)
    {
        ScoresByIteration.Add(iteration, result.AllScores);
    }
}


