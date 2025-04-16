

using Google.Protobuf.WellKnownTypes;
using System.Formats.Asn1;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Test.PepCentricReview;

public class PepCentricClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public PepCentricClient(string serverAddress = "35.171.101.18", int port = 50001)
    {
        _baseUrl = $"http://{serverAddress}:{port}";
        _client = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<PepCentricResponse> SubmitJobAsync(List<string> peptides, string jobType = "peptide", string enzyme = "enzymatic", string email = "", string jobName = "")
    {
        string query = string.Join(',', peptides);
        var url = $"{_baseUrl}/submit?email={email}&jobName={jobName}&jobType={jobType}&query={Uri.EscapeDataString(query)}&enzyme={enzyme}";
        return await GetJsonAsync<PepCentricResponse>(url);
    }

    public async Task<PepCentricResponse> SubmitJobAsync(string query, string jobType = "peptide", string enzyme = "enzymatic", string email = "", string jobName = "")
    {
        var url = $"{_baseUrl}/submit?email={email}&jobName={jobName}&jobType={jobType}&query={Uri.EscapeDataString(query)}&enzyme={enzyme}";
        return await GetJsonAsync<PepCentricResponse>(url);
    }

    public async Task<JobStatusResponse> GetJobStatusAsync(PepCentricResponse submissionResponse)
        => await GetJobStatusAsync(submissionResponse.JobIdentifier);

    public async Task<JobStatusResponse> GetJobStatusAsync(string jobId)
    {
        var url = $"{_baseUrl}/jobstatus?jobID={jobId}";
        return await GetJsonAsync<JobStatusResponse>(url);
    }

    public async Task<PepCentricResponse> GetResultAsync(PepCentricResponse submissionResponse) 
        => await GetResultAsync(submissionResponse.JobIdentifier);
    public async Task<PepCentricResponse> GetResultAsync(string jobId)
    {
        var url = $"{_baseUrl}/result?jobID={jobId}";
        return await GetJsonAsync<PepCentricResponse>(url);
    }

    public async Task<PepCentricResponse> GetCompletedResultAsync(PepCentricResponse submissionResponse)
        => await GetCompletedResultAsync(submissionResponse.JobIdentifier);
    public async Task<PepCentricResponse> GetCompletedResultAsync(string jobId)
    {
        var url = $"{_baseUrl}/result?jobID={jobId}";

        JobStatusResponse status;
        do
        {
            Task.Delay(2000); // wait 2s
            status = await GetJobStatusAsync(jobId);
        }
        while (!status.IsComplete);

        // Get final results
        return await GetResultAsync(jobId);
    }

    public async Task<JobStatusResponse> WaitUntilJobComplete(string jobId)
    {
        var url = $"{_baseUrl}/jobstatus?jobID={jobId}";
        JobStatusResponse status;
        do
        {
            await Task.Delay(2000); // wait 2s
            status = await GetJsonAsync<JobStatusResponse>(url);
        }
        while (!status.IsComplete);
        return status;
    }


    #region Base API Calls - Not used Yet

    public async Task<string> ShowSequenceAsync(string jobId, string proteinId, string q, string p, string e)
    {
        return await _client.GetStringAsync($"{_baseUrl}/showSequence?jobID={jobId}&proteinID={proteinId}&q={q}&p={p}&e={e}");
    }

    public async Task<string> ShowPeptideAsync(string jobId, string proteinId, string sequenceId, string p, string e)
    {
        return await _client.GetStringAsync($"{_baseUrl}/showPeptide?jobID={jobId}&proteinID={proteinId}&sequenceID={sequenceId}&p={p}&e={e}");
    }

    public async Task<string> ShowSpectrumAsync(string jobId, string proteinId, string sequenceId, string peptideId, string psmCap, string p, string e)
    {
        return await _client.GetStringAsync($"{_baseUrl}/showSpectrum?jobID={jobId}&proteinID={proteinId}&sequenceID={sequenceId}&peptideID={peptideId}&psmCap={psmCap}&p={p}&e={e}");
    }

    public async Task<string> CountSpectrumAsync(string jobId, string proteinId, string sequenceId, string peptideId, string p, string e)
    {
        return await _client.GetStringAsync($"{_baseUrl}/countSpectrum?jobID={jobId}&proteinID={proteinId}&sequenceID={sequenceId}&peptideID={peptideId}&p={p}&e={e}");
    }

    public async Task<string> SearchResultsAsync(string runId, string scanNum)
    {
        return await _client.GetStringAsync($"{_baseUrl}/searchresults?runID={runId}&scanNum={scanNum}");
    }

    public async Task<string> DumpAsync(string jobId, string q, string p, string e)
    {
        return await _client.GetStringAsync($"{_baseUrl}/dump?jobID={jobId}&q={q}&p={p}&e={e}");
    }

    #endregion

    private async Task<T> GetJsonAsync<T>(string url)
    {
        var response = await _client.GetStringAsync(url);
        var result = JsonSerializer.Deserialize<T>(response, _jsonOptions);

        // set the identifier if we can
        if (result is PepCentricResponse pepCentricResponse)
        {
            var uuid = ExtractUuid(pepCentricResponse.Status);
            pepCentricResponse.JobIdentifier = uuid;
        }

        return result!;
    }

    private string ExtractUuid(string text)
    {
        var match = Regex.Match(text, "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }
}


