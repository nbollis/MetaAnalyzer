using System.Text.Json.Serialization;

namespace Test.PepCentricReview;

public class PepCentricBaseResponse
{
    [JsonIgnore]
    public string JobIdentifier { get; set; }

    [JsonPropertyName("searchtime")]
    public int SearchTime { get; set; }

    [JsonPropertyName("servetime")]
    public int ServeTime { get; set; }

    [JsonPropertyName("assembletime")]
    public int AssembleTime { get; set; }

    [JsonPropertyName("indexcount")]
    public int IndexCount { get; set; }

    [JsonPropertyName("indexes")]
    public string IndexesRaw { get; set; }

    [JsonIgnore]
    public List<string> Indexes => ParseList(IndexesRaw);

    [JsonPropertyName("candidates")]
    public int Candidates { get; set; }

    [JsonPropertyName("digesttime")]
    public int DigestTime { get; set; }

    [JsonPropertyName("requests")]
    public int Requests { get; set; }

    [JsonPropertyName("sequences")]
    public int Sequences { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    private static List<string> ParseList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        return raw.Trim('[', ']').Split(',', System.StringSplitOptions.TrimEntries).ToList();
    }
}

public class PepCentricShowSequenceResponse : PepCentricBaseResponse
{
    [JsonPropertyName("protein")]
    public string Protein { get; set; }

    [JsonPropertyName("results")]
    public List<PepCentricSequence> Results { get; set; }
}

public class PepCentricShowPeptideResponse : PepCentricBaseResponse
{
    [JsonPropertyName("sequence")]
    public string BaseSequence { get; set; }

    [JsonPropertyName("results")]
    public List<PepCentricPeptide> Results { get; set; }
}