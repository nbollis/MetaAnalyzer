using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Test.PepCentricReview;

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

public class SubmissionResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonIgnore]
    public string JobIdentifier => ExtractUuid(Status);

    [JsonIgnore]
    public string Query => ExtractField("query", Status);

    [JsonIgnore]
    public string JobType => ExtractField("jobType", Status);

    [JsonIgnore]
    public string Enzyme => ExtractField("enzyme", Status);

    private static string ExtractUuid(string text)
    {
        var match = Regex.Match(text, @"UUID = ([0-9a-fA-F\-]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ExtractField(string fieldName, string text)
    {
        var match = Regex.Match(text, @$"{fieldName} = ([^,]+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}
