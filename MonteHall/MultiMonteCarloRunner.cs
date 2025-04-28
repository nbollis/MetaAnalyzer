using MonteCarlo.IO;

namespace MonteCarlo;

public class MultiMonteCarloRunner
{
    public string OutputDirectory { get; }
    public List<MonteCarloParameters> ParametersToRun { get; }
    public MultiMonteCarloRunner(List<MonteCarloParameters> parametersToRun, string outputDirectory)
    {
        if (parametersToRun == null || parametersToRun.Count == 0)
        {
            throw new ArgumentException("Parameters to run cannot be null or empty.");
        }

        foreach (var parameters in parametersToRun)
        {
            if (string.IsNullOrEmpty(parameters.OutputDirectory))
            {
                throw new ArgumentException("Output directory must be specified for each set of parameters.");
            }
            if (string.IsNullOrEmpty(parameters.ConditionIdentifier))
            {
                throw new ArgumentException("Condition identifier must be specified for each set of parameters.");
            }
        }

        ParametersToRun = parametersToRun;
        OutputDirectory = outputDirectory;

        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory);
    }

    public void RunAll()
    {
        Logger.Log("Running Monte Carlo simulations...");
        List<SimulationResultHandler> resultHandlers = new();
        foreach (var parameters in ParametersToRun)
        {
            Logger.Log($"Running Monte Carlo simulation with parameters: {parameters.ConditionIdentifier}", 1);
            var runner = new MonteCarloRunner(parameters);
            var results = runner.Run();
            resultHandlers.Add(results);
            Logger.Log($"Simulation completed for {parameters.ConditionIdentifier}.", 1);
        }

        var combinedHistogram = new CsvHelperFile<HistogramRecord>(Path.Combine(OutputDirectory, FileIdentifiers.SimulationResultHistogram))
        {
            Results = resultHandlers.SelectMany(rh => rh.HistogramFile.Results).ToList()
        };
        var combinedScore = new CsvHelperFile<AllScoreRecord>(Path.Combine(OutputDirectory, FileIdentifiers.AllSimulationScores))
        {
            Results = resultHandlers.SelectMany(rh => rh.ScoreFile.Results).ToList()
        };
        combinedHistogram.WriteResults(combinedHistogram.FilePath);
        combinedScore.WriteResults(combinedScore.FilePath);
    }
}


