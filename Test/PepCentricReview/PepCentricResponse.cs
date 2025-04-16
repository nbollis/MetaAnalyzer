using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Test.PepCentricReview;

public class PepCentricResponse
{
    [JsonIgnore]
    public string JobIdentifier { get; set; }

    [JsonPropertyName("searchtime")]
    public int SearchTime { get; set; }

    [JsonPropertyName("servetime")]
    public int ServeTime { get; set; }

    [JsonPropertyName("candidates")]
    public int Candidates { get; set; }

    [JsonPropertyName("assembletime")]
    public int AssembleTime { get; set; }

    [JsonPropertyName("indexcount")]
    public int IndexCount { get; set; }

    [JsonPropertyName("indexes")]
    public string IndexesRaw { get; set; }

    [JsonIgnore]
    public List<string> Indexes => ParseList(IndexesRaw);

    [JsonPropertyName("digesttime")]
    public int DigestTime { get; set; }

    [JsonPropertyName("requests")]
    public int Requests { get; set; }

    [JsonPropertyName("sequences")]
    public int Sequences { get; set; }

    [JsonPropertyName("results")]
    public List<PepCentricResult> Results { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    private static List<string> ParseList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        return raw.Trim('[', ']').Split(',', System.StringSplitOptions.TrimEntries).ToList();
    }
}

public class JobStatusResponse
{
    [JsonPropertyName("serveTime")]
    public int ServeTime { get; set; }

    [JsonPropertyName("job_complete")]
    public string JobComplete { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonIgnore]
    public bool IsComplete => JobComplete == "1";
}

public class PepCentricResult
{
    [JsonPropertyName("organs")]
    public string Organs { get; set; }

    [JsonPropertyName("protein_id")]
    public int ProteinId { get; set; }

    [JsonPropertyName("dataset_count")]
    public int DatasetCount { get; set; }

    [JsonPropertyName("protein")]
    public string Protein { get; set; }

    [JsonPropertyName("diseases")]
    public string Diseases { get; set; }

    [JsonPropertyName("evalue")]
    public string EValue { get; set; }

    [JsonPropertyName("pvalue")]
    public string PValue { get; set; }

    [JsonPropertyName("best_sequence")]
    public string BestSequence { get; set; }

    [JsonPropertyName("datasets")]
    public string Datasets { get; set; }

    [JsonPropertyName("sequences")]
    public int Sequences { get; set; }

    [JsonPropertyName("run_count")]
    public int RunCount { get; set; }
}
