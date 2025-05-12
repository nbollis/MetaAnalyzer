using MathNet.Numerics;
using System.Collections.Concurrent;

namespace MonteCarlo;

public class SimulationResultHandler : ISimulationResultHandler
{
    private ConcurrentBag<IndividualScoreRecord> individualScoreRecords = new();
    public string ConditionIdentifier { get; set; }
    public string OutputDirectory { get; private set; }
    public string SummaryText { get; set; } = string.Empty;
    public Dictionary<int, List<double>> ScoresByIteration { get; }
    public CsvHelperFile<HistogramRecord> HistogramFile { get; set; }
    public CsvHelperFile<AllScoreRecord> ScoreFile { get; set; }
    public CsvHelperFile<IndividualScoreRecord> BestScoresFile { get; set; }

    public SimulationResultHandler(string outputDirectory, string? conditionIdentifier = null)
    {
        ScoresByIteration = new();
        ConditionIdentifier = conditionIdentifier ?? string.Empty;
        OutputDirectory = outputDirectory;

        string summaryPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.AllSimulationScores}"); 
        string histogramPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.SimulationResultHistogram}");
        string bestScoresPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.BestScoringPeptides}");
        ScoreFile = new CsvHelperFile<AllScoreRecord>(summaryPath);
        HistogramFile = new CsvHelperFile<HistogramRecord>(histogramPath);
        BestScoresFile = new CsvHelperFile<IndividualScoreRecord>(bestScoresPath);
    }

    public void HandleResult(SimulationResult result, int iteration)
    {
        ScoresByIteration.Add(iteration, result.AllScores);
    }

    public bool SimulationComplete()
    {
        var summaryPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.SimulationSummary}");
        if (!File.Exists(summaryPath))
            return false;

        if (!File.Exists(ScoreFile.FilePath))
            return false;

        if (!File.Exists(HistogramFile.FilePath))
            return false;
        return true;
    }

    public void HandleBestScoreRecord(IndividualScoreRecord scoreRecord)
    {
        individualScoreRecords.Add(scoreRecord);
    }

    public void DoPostProcessing()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        GatherAllScoreResults();
        DoHistogramProcessing();
        DoBestScoringPostProcessing();
    }

    public void WriteAllResults()
    {
        ScoreFile.WriteResults(ScoreFile.FilePath);
        HistogramFile.WriteResults(HistogramFile.FilePath);
        BestScoresFile.WriteResults(BestScoresFile.FilePath);

        // Write summary text to a file
        string summaryPath = Path.Combine(OutputDirectory, $"{ConditionIdentifier}_{FileIdentifiers.SimulationSummary}");
        using (var writer = new StreamWriter(summaryPath))
        {
            writer.WriteLine(SummaryText);
        }
    }

    private void GatherAllScoreResults()
    {
        
        // Flatten ScoresByIteration into a list of ScoreRecords
        var scoreRecords = ScoresByIteration
            .SelectMany(kvp => kvp.Value.Select(score => new AllScoreRecord(ConditionIdentifier, kvp.Key, score)))
            .ToList();

        // Write the score records to a CSV file
        ScoreFile = new CsvHelperFile<AllScoreRecord>(ScoreFile.FilePath)
        {
            Results = scoreRecords
        };
    }

    private void DoHistogramProcessing()
    {
        

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

        HistogramFile = new CsvHelperFile<HistogramRecord>(HistogramFile.FilePath)
        {
            Results = histogramRecords
        };
    }

    public void DoBestScoringPostProcessing()
    {
        // Write the best scoring peptides to a CSV file
        BestScoresFile = new CsvHelperFile<IndividualScoreRecord>(BestScoresFile.FilePath)
        {
            Results = individualScoreRecords.ToList()
        };

        // Clear the individual score records for the next iteration
        individualScoreRecords = new ConcurrentBag<IndividualScoreRecord>();
    }
}


