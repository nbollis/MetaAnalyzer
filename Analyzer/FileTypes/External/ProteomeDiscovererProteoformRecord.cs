using Analyzer.SearchType;
using Analyzer.Util.TypeConverters;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.External;

public class ProteomeDiscovererProteoformRecord : IEquatable<ProteomeDiscovererProteoformRecord>
{
    public static CsvConfiguration CsvConfiguration => new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
    {
        Delimiter = "\t",
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        BadDataFound = null,
        MissingFieldFound = null,
        HeaderValidated = null,
    };

    // PSM Equivalence
    public bool Equals(ProteomeDiscovererPsmRecord psm)
    {
        if (psm.BaseSequence != Sequence)
            return false;
        if (psm.ProteinAccessions != ProteinAccessions)
            return false;
        if (Modifications.Length != psm.Modifications.Length)
            return false;
        return Modifications.All(mod => psm.Modifications.Contains(mod));
    }

    

    [Name("Proteoform Characterization Confidence")]
    public string ProteoformCharacterizationConfidence { get; set; }

    [Name("Protein Description")]
    public string ProteinDescription { get; set; }

    [Name("Confidence")]
    public string Confidence { get; set; }

    [Name("Sequence")]
    public string Sequence { get; set; }

    [Name("# PrSMs", "# PSMs")]
    public int PrsmCount { get; set; }

    [Name("# Protein Groups")]
    public int ProteinGroupCount { get; set; }

    [Name("# Proteins")]
    public int ProteinCount { get; set; }

    [Name("Protein Accessions")]
    public string ProteinAccessions { get; set; }

    [Name("m/z [Da] (by Search Engine): CHIMERYS")]
    public double Mz { get; set; }

    [Name("Theo. Mass [Da]", "Theo. MH+ [Da]")]
    public double TheoreticalMass { get; set; }

    [Name("Best PrSM C-Score")]
    public double BestPrsmCScore { get; set; }

    [Name("Average PrSM Detected Neutral Mass")]
    public double AveragePrsmDetectedNeutralMass { get; set; }

    [Name("Q-value", "q-Value")]
    public double QValue { get; set; }

    [Name("PEP")]
    public double PEP { get; set; }

    [Name("SVM Score (by Search Engine): CHIMERYS")]
    public double SVMScore { get; set; }

    [Name("Modifications")]
    [TypeConverter(typeof(ProteomeDiscovererPSMModToProteomeDiscovererModificationArrayConverter))]
    public ProteomeDiscovererMod[] Modifications { get; set; }

    [Name("Proforma")]
    public string Proforma { get; set; }

    [Name("% Residue Cleavages")]
    public double PercentResidueCleavages { get; set; }

    [Name("Checked")]
    public bool Checked { get; set; }

    [Name("# Missed Cleavages")]
    [Optional]
    public int MissedCleavages { get; set; }

    public bool Equals(ProteomeDiscovererProteoformRecord? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Sequence != other.Sequence)
            return false;
        if (ProteinAccessions != other.ProteinAccessions)
            return false;
        if (Modifications.Length != other.Modifications.Length)
            return false;
        return Modifications.All(mod => other.Modifications.Contains(mod));
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ProteomeDiscovererProteoformRecord)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Sequence, ProteinAccessions, Modifications);
    }
}

public class ProteomeDiscovererProteoformFile : ResultFile<ProteomeDiscovererProteoformRecord>, IResultFile
{
    private List<ProteomeDiscovererProteoformRecord>? _filteredResults;
    public List<ProteomeDiscovererProteoformRecord> FilteredResults => 
        _filteredResults ??= Results.Where(p => p.PEP <= 0.01).ToList();

    public ProteomeDiscovererProteoformFile(string filePath) : base(filePath)
    {
    }

    public ProteomeDiscovererProteoformFile()
    {
    }
    public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
    public override Software Software { get; set; }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), ProteomeDiscovererProteoformRecord.CsvConfiguration);
        Results = csv.GetRecords<ProteomeDiscovererProteoformRecord>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ProteomeDiscovererProteoformRecord.CsvConfiguration);

        csv.WriteHeader<ProteomeDiscovererProteoformRecord>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }
}