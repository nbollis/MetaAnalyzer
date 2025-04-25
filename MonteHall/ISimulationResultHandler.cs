namespace MonteCarlo;

public interface ISimulationResultHandler
{
    string OutputDirectory { get; }
    void HandleResult(SimulationResult result);
}

public class SimulationResult
{
    public List<double> AllScores { get; init; }
}

public class SimulationResultHandler : ISimulationResultHandler
{
    public string OutputDirectory { get; private set; }
    public SimulationResultHandler(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
    }
    public void HandleResult(SimulationResult result)
    {
        // Handle the result, e.g., save it to a file or process it further
        var filePath = Path.Combine(OutputDirectory, "simulation_result.txt");
        File.WriteAllLines(filePath, result.AllScores.Select(score => score.ToString()));
    }
}


