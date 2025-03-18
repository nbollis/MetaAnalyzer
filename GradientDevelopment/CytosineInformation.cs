using CsvHelper;
using CsvHelper.Configuration;
using Readers;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace GradientDevelopment;

/// <summary>
/// Class representing the differential methylation patterns of a single LC/MS run
/// </summary>
public class CytosineInformation
{
    public CytosineInformation(string dataFileName, double fdrCutoff, int totalTargetCytosines, int totalDecoyCytosines,
        int methylatedTargetCystosines, int methylatedDecoyCystosines, int unmethylatedTargetCytosines,
        int unmethylatedDecoyCytosines, double targetMethylPercent, double decoyMethylPercent,
        double targetMethylPercentGreaterThanOne, double decoyMethylPercentGreaterThanOne)
    {
        DataFileName = dataFileName;
        FdrCutoff = fdrCutoff;
        TotalTargetCytosines = totalTargetCytosines;
        TotalDecoyCytosines = totalDecoyCytosines;
        MethylatedTargetCystosines = methylatedTargetCystosines;
        MethylatedDecoyCystosines = methylatedDecoyCystosines;
        UnmethylatedTargetCytosines = unmethylatedTargetCytosines;
        UnmethylatedDecoyCytosines = unmethylatedDecoyCytosines;
        TargetMethylPercent = targetMethylPercent;
        DecoyMethylPercent = decoyMethylPercent;
        TargetMethylPercentGreaterThanOne = targetMethylPercentGreaterThanOne;
        DecoyMethylPercentGreaterThanOne = decoyMethylPercentGreaterThanOne;

        ExpectedMethylPercent = -1;
        try
        {
            var splits = dataFileName.Split('_');
            var relevantSplit = splits.FirstOrDefault(p => p.Contains("Met"));
            if (relevantSplit is null) return;

            var percentString = relevantSplit.Split('M')[0];
            var converted = percentString.Replace('-', '.');
            if (double.TryParse(converted, out var percent))
            {
                ExpectedMethylPercent = percent;
            }
        }
        catch
        {  // do nothing
        }
    }

    public CytosineInformation() { }

    public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        HasHeaderRecord = true,
        HeaderValidated = null,
        MissingFieldFound = null, 
    };

    [Name("DataFileName")]
    public string DataFileName { get; set; }

    [Name("Fdr Cutoff")]
    [Optional] [Default(0.05)]
    public double FdrCutoff { get; set; }

    [Name("Expected Percent")]
    [Optional] [Default(-1)]
    public double ExpectedMethylPercent { get; set; }

    [Name("TotalTargetCytosines")]
    public int TotalTargetCytosines { get; set; }

    [Name("TotalDecoyCytosines")]
    public int TotalDecoyCytosines { get; set; }

    [Name("MethylatedTargetCystosines")]
    public int MethylatedTargetCystosines { get; set; }

    [Name("MethylatedDecoyCystosines")]
    public int MethylatedDecoyCystosines { get; set; }

    [Name("UnmethylatedTargetCytosines")]
    public int UnmethylatedTargetCytosines { get; set; }

    [Name("UnmethylatedDecoyCytosines")]
    public int UnmethylatedDecoyCytosines { get; set; }

    [Name("TargetMethylPercent")]
    public double TargetMethylPercent { get; set; }

    [Name("DecoyMethylPercent")]
    public double DecoyMethylPercent { get; set; }

    [Name("TargetMethylPercentGreaterThanOne")]
    public double TargetMethylPercentGreaterThanOne { get; set; }

    [Name("DecoyMethylPercentGreaterThanOne")]
    public double DecoyMethylPercentGreaterThanOne { get; set; }
}

public class CytosineInformationFile : ResultFile<CytosineInformation>, IResultFile
{
    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), CytosineInformation.CsvConfiguration);
        Results = csv.GetRecords<CytosineInformation>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        using (var csv = new CsvWriter(new StreamWriter(outputPath), CytosineInformation.CsvConfiguration))
        {
            csv.WriteHeader<CytosineInformation>();
            foreach (var result in Results)
            {
                csv.NextRecord();
                csv.WriteRecord(result);
            }
        }
    }

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }

    public CytosineInformationFile(string filePath, List<CytosineInformation>? cytosineInformation = null) : base(filePath)
    {
        if (cytosineInformation is not null)
            Results = cytosineInformation;
    }
}