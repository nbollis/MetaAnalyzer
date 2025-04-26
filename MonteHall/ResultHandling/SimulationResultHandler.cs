using MathNet.Numerics;

namespace MonteCarlo;

public class SimulationResultHandler : ISimulationResultHandler
{
    public string ConditionIdentifier { get; set; }
    public string OutputDirectory { get; private set; }
    public string SummaryText { get; set; } = string.Empty;
    public Dictionary<int, List<double>> ScoresByIteration { get; }
    public CsvHelperFile<HistogramRecord> HistogramFile { get; set; }
    public CsvHelperFile<AllScoreRecord> ScoreFile { get; set; }

    public SimulationResultHandler(string outputDirectory, string? conditionIdentifier = null)
    {
        ScoresByIteration = new();
        ConditionIdentifier = conditionIdentifier ?? string.Empty;
        OutputDirectory = outputDirectory;
    }

    public void HandleResult(SimulationResult result, int iteration)
    {
        ScoresByIteration.Add(iteration, result.AllScores);
    }

    public void DoPostProcessing()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        GatherAllScoreResults();
        DoHistogramProcessing();
    }

    public void WriteAllResults()
    {
        ScoreFile.WriteResults(ScoreFile.FilePath);
        HistogramFile.WriteResults(HistogramFile.FilePath);

        // Write summary text to a file
        string summaryPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.SimulationSummary}");
        using (var writer = new StreamWriter(summaryPath))
        {
            writer.WriteLine(SummaryText);
        }
    }

    private void GatherAllScoreResults()
    {
        string summaryPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.AllSimulationScores}");

        // Flatten ScoresByIteration into a list of ScoreRecords
        var scoreRecords = ScoresByIteration
            .SelectMany(kvp => kvp.Value.Select(score => new AllScoreRecord(ConditionIdentifier, kvp.Key, score)))
            .ToList();

        // Write the score records to a CSV file
        ScoreFile = new CsvHelperFile<AllScoreRecord>(summaryPath)
        {
            Results = scoreRecords
        };
    }

    private void DoHistogramProcessing()
    {
        string histogramPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.SimulationResultHistogram}");

        // Aggregate scores across iterations into a histogram
        SortedDictionary<double, int> histogram = new();
        foreach (var scores in ScoresByIteration.Values)
        {
            foreach (var score in scores.Select(p => p.Round(3)))
            {
                if (histogram.ContainsKey(score))
                {
                    histogram[score]++;
                }
                else
                {
                    histogram[score] = 1;
                }
            }
        }

        // Convert histogram data to HistogramRecord array
        var histogramRecords = histogram.Select(kvp => new HistogramRecord
        {
            Condition = ConditionIdentifier,
            Score = kvp.Key,
            Count = kvp.Value
        }).ToList();

        HistogramFile = new CsvHelperFile<HistogramRecord>(histogramPath)
        {
            Results = histogramRecords
        };
    }
}


