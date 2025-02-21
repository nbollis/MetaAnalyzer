using CsvHelper;
using CsvHelper.Configuration;
using Readers;
using System.Globalization;

namespace GradientDevelopment;

public class CytosineInformation(
    string dataFileName,
    int totalTargetCytosines,
    int totalDecoyCytosines,
    int methylatedTargetCystosines,
    int methylatedDecoyCystosines,
    int unmethylatedTargetCytosines,
    int unmethylatedDecoyCytosines)
{
    public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        HasHeaderRecord = true,
    };

    public string DataFileName { get; set; } = dataFileName;
    public int TotalTargetCytosines { get; set; } = totalTargetCytosines;
    public int TotalDecoyCytosines { get; set; } = totalDecoyCytosines;

    public int MethylatedTargetCystosines { get; set; } = methylatedTargetCystosines;
    public int MethylatedDecoyCystosines { get; set; } = methylatedDecoyCystosines;

    public int UnmethylatedTargetCytosines { get; set; } = unmethylatedTargetCytosines;
    public int UnmethylatedDecoyCytosines { get; set; } = unmethylatedDecoyCytosines;
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