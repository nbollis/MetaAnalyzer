using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Analyzer.Util.TypeConverters;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Easy.Common.Extensions;
using Readers;

namespace Analyzer.FileTypes.External;

public class MsPathFinderTCrossTabResultFile : ResultFile<MsPathFinderTCrossTabResultRecord>, IResultFile
{
    public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
    public override Software Software { get; set; }
    public List<MsPathFinderTCrossTabResultRecord> TargetResults => Results.Where(p => !p.IsDecoy).ToList();
    public List<MsPathFinderTCrossTabResultRecord> FilteredTargetResults =>
        Results.Where(p => p is { IsDecoy: false, BestEValue: <= 0.01 }).ToList();
    public MsPathFinderTCrossTabResultFile(string filePath) : base(filePath, Software.Unspecified) { }
    public MsPathFinderTCrossTabResultFile() : base() { }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), MsPathFinderTCrossTabResultRecord.CsvConfiguration);
        var headers = File.ReadLines(FilePath).First().Split('\t');
        var spectraCount = headers.Where(p => p.Contains("_SpectraCount"))
            .ToDictionary(p => p, p => headers.IndexOf(p));
        var abundance = headers.Where(p => p.Contains("_Abundance"))
            .ToDictionary(p => p, p => headers.IndexOf(p));
        var preIndex = headers.IndexOf("Pre");
        var results = new List<MsPathFinderTCrossTabResultRecord>();

        bool readHeader = false;
        while (csv.Read())
        {
            if (readHeader == false)
            {
                csv.ReadHeader();
                readHeader = true;
                continue;
            }
            if (csv.Parser.Record[preIndex].IsNullOrEmpty())
                continue;


            var record = csv.GetRecord<MsPathFinderTCrossTabResultRecord>();
            if (record is null) 
                continue;
            foreach (var kvp in spectraCount)
            {
                var file = kvp.Key.Replace("_SpectraCount", "");
                record.FileToPsmCount[file] = csv.GetField<int>(kvp.Value);
            }

            foreach (var kvp in abundance)
            {
                var file = kvp.Key.Replace("_Abundance", "");
                record.AbundanceByFile[file] = csv.GetField<double>(kvp.Value);
            }

            results.Add(record);
        }

        Results = results;
    }

    public override void WriteResults(string outputPath)
    {
        // TODO: Write this out based upon the stored dictionaries
        throw new NotImplementedException();
    }
}


public class MsPathFinderTCrossTabResultRecord
{

    public static CsvConfiguration CsvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = "\t",
        HasHeaderRecord = true,
        MissingFieldFound = null,
        HeaderValidated = null,
        BadDataFound = null
    };

    [Name("MonoMass")]
    public double MonoMass { get; set; }

    [Name("MinElutionTime")]
    public double MinElutionTime { get; set; }

    [Name("MaxElutionTime")]
    public double MaxElutionTime { get; set; }

    [NotMapped]
    public Dictionary<string, double> AbundanceByFile { get; set; }

    [Name("Sequence")]
    public string BaseSequence { get; set; }

    [Name("Post")]
    public char NextAminoAcid { get; set; }

    [TypeConverter(typeof(CommaDelimitedToIntegerArrayTypeConverter))]
    public int[] Modifications { get; set; }

    [Name("ProteinName")]
    public string ProteinAccessionName { get; set; }

    [NotMapped]
    private string _proteinAccession;

    [NotMapped]
    public string ProteinAccession => _proteinAccession ??= ProteinAccessionName.Split('|')[1].Trim();

    [NotMapped]
    private string _proteinName;
    [NotMapped]
    public string ProteinName => _proteinName ??= ProteinAccessionName.Split('|')[2].Trim();

    [NotMapped]
    private bool? isDecoy;

    [NotMapped]
    public bool IsDecoy => isDecoy ??= ProteinAccessionName.StartsWith("XXX");

    [Name("ProteinDesc")]
    public string Description { get; set; }

    public int ProteinLength { get; set; }

    [Name("Start")]
    public int StartResidue { get; set; }

    [Name("End")]
    public int EndResidue { get; set; }

    [Name("BestEValue")]
    public double BestEValue { get; set; }

    [NotMapped]
    public Dictionary<string, int> FileToPsmCount { get; set; }

    public MsPathFinderTCrossTabResultRecord()
    {
        AbundanceByFile = new Dictionary<string, double>();
        FileToPsmCount = new Dictionary<string, int>();
    }
}