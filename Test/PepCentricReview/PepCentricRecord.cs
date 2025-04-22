using System.Text.Json.Serialization;
namespace Test.PepCentricReview;

public abstract class PepCentricRecord
{
    [JsonPropertyName("organs")]
    [JsonConverter(typeof(SemicolonDelimitedStringToHashSetConverter))]
    public HashSet<string> Organs { get; set; }

    [JsonPropertyName("dataset_count")]
    public int DatasetCount { get; set; }

    [JsonIgnore]
    public abstract int ID { get; set; }

    [JsonPropertyName("diseases")]
    [JsonConverter(typeof(SemicolonDelimitedStringToHashSetConverter))]
    public HashSet<string> Diseases { get; set; }

    [JsonPropertyName("best_psm")]
    public string BestPsm { get; set; }

    [JsonPropertyName("evalue")]
    [JsonConverter(typeof(DoubleToStringConverter))]
    public double EValue { get; set; }

    [JsonPropertyName("pvalue")]
    [JsonConverter(typeof(DoubleToStringConverter))]
    public double PValue { get; set; }

    [JsonPropertyName("datasets")]
    [JsonConverter(typeof(SemicolonDelimitedStringToHashSetConverter))]
    public HashSet<string> Datasets { get; set; }

    [JsonPropertyName("psms")]
    public int PsmCount { get; set; }

    [JsonPropertyName("run_count")]
    public int RunCount { get; set; }
}

public class PepCentricSequence : PepCentricRecord
{
    [JsonPropertyName("QValue")]
    [JsonConverter(typeof(DoubleToStringConverter))]
    public double QValue { get; set; }

    [JsonPropertyName("sequence")]
    public string BaseSequence { get; set; }

    [JsonPropertyName("sequence_id")]
    public override int ID { get; set; }
}

public class PepCentricPeptide : PepCentricRecord
{
    [JsonPropertyName("peptide")]
    public string FullSequenceWithMassShifts { get; set; }

    [JsonPropertyName("peptide_id")]
    public override int ID { get; set; }
}

public class PepCentricProtein : PepCentricRecord
{
    [JsonPropertyName("protein_id")]
    public override int ID { get; set; }

    [JsonPropertyName("best_sequence")]
    public string BestBaseSequence { get; set; }
}