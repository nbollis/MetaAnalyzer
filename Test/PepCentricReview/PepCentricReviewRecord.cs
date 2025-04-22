using System.Globalization;
using System.Text;
using Chemistry;
using CsvHelper;
using CsvHelper.Configuration;
using Omics.Modifications;
using Readers;
using Proteomics.ProteolyticDigestion;
using ResultAnalyzerUtil;
using CsvHelper.Configuration.Attributes;
using Easy.Common.Extensions;

namespace Test.PepCentricReview;

public enum IdClassification
{
    Fasta,
    Xml,
    Gptmd, 
    Unknown,
}

public class PepCentricReviewRecord : IEquatable<PepCentricPeptide>
{
    public string BaseSequence { get; set; }
    public string FullSequence { get; set; }
    public string FullSequenceWithMassShifts { get; set; }
    public int BaseSequenceCount { get; set; }
    public int FullSequenceCount { get; set; }
    public string Accession { get; set; }

    public IdClassification IdClassification { get; set; }

    [Optional] public int PsmCount { get; set; }
    [Optional] public int OrganCount { get; set; }
    [Optional] public int DiseaseCount { get; set; }
    [Optional] public int DatasetCount { get; set; }


    public void SetValuesFromPepCentric(PepCentricPeptide pep)
    {
        PsmCount = pep.PsmCount;
        OrganCount = pep.Organs.Count;
        DiseaseCount = pep.Diseases.Count;
        DatasetCount = pep.DatasetCount;
    }



    public static string GetFullSequenceWithMassShift(string fullSequence, int decimals = 4)
    {
        var withSetMods = new PeptideWithSetModifications(fullSequence, GlobalVariables.AllModsKnownDictionary);
        var subsequence = new StringBuilder();

        // modification on peptide N-terminus
        if (withSetMods.AllModsOneIsNterminus.TryGetValue(1, out Modification? mod))
        {
            subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(decimals)}]");
        }

        for (int r = 0; r < withSetMods.Length; r++)
        {
            subsequence.Append(withSetMods[r]);

            // modification on this residue
            if (withSetMods.AllModsOneIsNterminus.TryGetValue(r + 2, out mod))
            {
                if (mod.MonoisotopicMass > 0)
                {
                    subsequence.Append($"[+{mod.MonoisotopicMass.RoundedDouble(decimals)}]");
                }
                else
                {
                    subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(decimals)}]");
                }
            }
        }

        // modification on peptide C-terminus
        if (withSetMods.AllModsOneIsNterminus.TryGetValue(withSetMods.Length + 2, out mod))
        {
            if (mod.MonoisotopicMass > 0)
            {
                subsequence.Append($"[+{mod.MonoisotopicMass.RoundedDouble(decimals)}]");
            }
            else
            {
                subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(decimals)}]");
            }
        }
        return subsequence.ToString();
    }

    public bool Equals(PepCentricPeptide? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (other.FullSequenceWithMassShifts != FullSequenceWithMassShifts) return false;

        return true;
    }
}

public class PepCentricReviewFile : ResultFile<PepCentricReviewRecord>, IResultFile
{
    CsvConfiguration Configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        Delimiter = "\t",
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
    };
    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }
    public PepCentricReviewFile(string filePath) : base(filePath, Software.Unspecified) { }
    public PepCentricReviewFile() : base() { }

    public override void LoadResults()
    {
        var csv = new CsvReader(new StreamReader(FilePath), Configuration);
        Results = csv.GetRecords<PepCentricReviewRecord>().ToList();
    }

    public override void WriteResults(string outputPath)
    {
        if (!outputPath.EndsWith(".tsv"))
            outputPath += ".tsv";

        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, Configuration);
        csv.WriteRecords(Results);
    }
}