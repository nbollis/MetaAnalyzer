using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Readers;

namespace Analyzer.FileTypes.External;

public class ProteomeDiscovererProteinRecord : IEquatable<ProteomeDiscovererProteinRecord>
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

    public bool Equals(ProteomeDiscovererPsmRecord psm)
    {
        if (psm.ProteinAccessions != Accession)
            return false;
        return true;
    }

    [Name("Accession")]
    public string Accession { get; set; }

    [Name("Description")]
    public string Description { get; set; }

    [Name("# of Isoforms")]
    public int IsoformCount { get; set; }

    [Name("# of Isoforms with Characterized Proteoforms")]
    public int IsoformsWithCharacterizedProteoforms { get; set; }

    [Name("# of Proteoforms")]
    public int ProteoformCount { get; set; }

    [Name("Proteoform Characterization Confidence")]
    public string ProteoformCharacterizationConfidence { get; set; }

    [Name("Protein FDR Confidence: Combined")]
    public string ProteinFdrConfidenceCombined { get; set; }

    [Name("# Characterized Proteoforms")]
    public int CharacterizedProteoformCount { get; set; }

    [Name("# Peptides")]
    public int NumberOfPeptides { get; set; }

    [Name("# Unique Peptides")]
    public int NumberOfUniquePeptides { get; set; }

    [Name("# Protein Groups")]
    public int ProteinGroupCount { get; set; }

    [Name("# of PrSMs", "# PSMs")]
    public int PsmCount { get; set; }

    [Name("Q-value", "Exp. q-value: Combined")]
    public double QValue { get; set; }

    [Name("Sum PEP Score")]
    public double SumPepScore { get; set; }

    [Name("Checked")]
    public bool Checked { get; set; }

    [Name("Master")]
    public string Master { get; set; }

    [Name("Sequence")]
    public string Sequence { get; set; }

    [Name("Coverage [%]")]
    public double Coverage { get; set; }

    [Name("# AAs")]
    public int AminoAcidCount { get; set; }

    [Name("MW [kDa]")]
    public double MolecularWeightkDa { get; set; }

    [Name("calc. pI")]
    public double CalculatedpI { get; set; }

    [Name("Score CHIMERY: CHIMERYS")]
    public double ChimerysScore { get; set; }

    public bool Equals(ProteomeDiscovererProteinRecord? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Accession == other.Accession;
     }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ProteomeDiscovererProteinRecord)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Accession);
    }
}

public class ProteomeDiscovererProteinFile : ResultFile<ProteomeDiscovererProteinRecord>, IResultFile
{
    public ProteomeDiscovererProteinFile(string filePath) : base(filePath)
    {
    }

    public ProteomeDiscovererProteinFile()
    {
    }
    public override SupportedFileType FileType => SupportedFileType.Tsv_FlashDeconv;
    public override Software Software { get; set; }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), ProteomeDiscovererProteinRecord.CsvConfiguration);
        Results = csv.GetRecords<ProteomeDiscovererProteinRecord>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        using var csv = new CsvWriter(new StreamWriter(File.Create(outputPath)), ProteomeDiscovererProteinRecord.CsvConfiguration);

        csv.WriteHeader<ProteomeDiscovererProteinRecord>();
        foreach (var result in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(result);
        }
    }
}
