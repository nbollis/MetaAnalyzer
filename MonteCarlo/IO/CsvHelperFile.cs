using CsvHelper;
using CsvHelper.Configuration;
using Readers;

namespace MonteCarlo;

public class CsvHelperFile<T> : ResultFile<T>, IResultFile where T : IRecord
{
    public static CsvConfiguration CsvConfiguration { get; } = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        HasHeaderRecord = true,
        TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
        MissingFieldFound = null,
    };

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }

    public CsvHelperFile(string filePath) : base(filePath)
    {
        FilePath = filePath;
    }

    public override void LoadResults()
    {
        var csv = new CsvReader(new StreamReader(FilePath), CsvConfiguration);
        Results = csv.GetRecords<T>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), CsvConfiguration);

        csv.WriteHeader<T>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }
}