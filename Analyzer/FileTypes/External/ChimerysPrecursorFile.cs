﻿using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Readers;


namespace Analyzer.FileTypes.External;
public class ChimerysPrecursorFile : ResultFile<ChimerysPrecursor>, IResultFile
{
    public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Encoding = Encoding.UTF8,
        HasHeaderRecord = true,
        Delimiter = "\t",
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim
    };

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; } = Software.Unspecified;

    public ChimerysPrecursorFile() : base() { Software = Software.Unspecified; }
    public ChimerysPrecursorFile(string path) : base(path, Software.Unspecified) { }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), CsvConfiguration);
        Results = csv.GetRecords<ChimerysPrecursor>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), CsvConfiguration);

        csv.WriteHeader<ChimerysPrecursor>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }
}