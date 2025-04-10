﻿using CsvHelper;
using CsvHelper.Configuration;
using Readers;

namespace RadicalFragmentation;

public class PrecursorCompetitionSummary
{
    public static CsvConfiguration CsvConfiguration = new(System.Globalization.CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        HasHeaderRecord = true,
    };

    // Identifiers
    public string FragmentationType { get; set; }
    public string Species { get; set; }
    public int AmbiguityLevel { get; set; }
    public int MissedMonoisotopics { get; set; }
    public int NumberOfMods { get; set; }
    public double PpmTolerance { get; set; }

    // Results
    public int Count { get; set; }
    public int PrecursorsInGroup { get; set; }
}

public class PrecursorCompetitionFile : ResultFile<PrecursorCompetitionSummary>, IResultFile
{
    public PrecursorCompetitionFile() : base() { }
    public PrecursorCompetitionFile(string filePath) : base(filePath) { }
    public override void LoadResults()
    {
        using (var csv = new CsvReader(new StreamReader(FilePath), PrecursorCompetitionSummary.CsvConfiguration))
        {
            Results = csv.GetRecords<PrecursorCompetitionSummary>().ToList();
        }
    }

    public override void WriteResults(string outputPath)
    {
        var csv = new CsvWriter(new StreamWriter(outputPath), PrecursorCompetitionSummary.CsvConfiguration);

        csv.WriteHeader<PrecursorCompetitionSummary>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }

        csv.Dispose();
    }

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }
}