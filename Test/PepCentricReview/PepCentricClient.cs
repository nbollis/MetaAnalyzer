

using Google.Protobuf.WellKnownTypes;
using PuppeteerSharp;
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

    public async Task<SubmissionResponse> SubmitJobAsync(List<string> peptides, string jobType = "peptide", string enzyme = "enzymatic", string email = "", string jobName = "")
    {
        string query = string.Join(',', peptides);
        var url = $"{_baseUrl}/submit?email={email}&jobName={jobName}&jobType={jobType}&query={Uri.EscapeDataString(query)}&enzyme={enzyme}";
        return await GetJsonAsync<SubmissionResponse>(url);
    }

    public async Task<SubmissionResponse> SubmitJobAsync(string query, string jobType = "peptide", string enzyme = "enzymatic", string email = "", string jobName = "")
    {
        var url = $"{_baseUrl}/submit?email={email}&jobName={jobName}&jobType={jobType}&query={Uri.EscapeDataString(query)}&enzyme={enzyme}";
        return await GetJsonAsync<SubmissionResponse>(url);
    }

    public async Task<JobStatusResponse> GetJobStatusAsync(SubmissionResponse submissionResponse)
        => await GetJobStatusAsync(submissionResponse.JobIdentifier);

    public async Task<JobStatusResponse> GetJobStatusAsync(string jobId)
    {
        var url = $"{_baseUrl}/jobstatus?jobID={jobId}";
        return await GetJsonAsync<JobStatusResponse>(url);
    }

    public async Task<PepCentricBaseResponse> GetResultAsync(PepCentricBaseResponse submissionResponse)
        => await GetResultAsync(submissionResponse.JobIdentifier);
    public async Task<PepCentricBaseResponse> GetResultAsync(string jobId)
    {
        var url = $"{_baseUrl}/result?jobID={jobId}";
        return await GetJsonAsync<PepCentricBaseResponse>(url);
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

    // Gets all Responses of a peptide from a given protein
    public async Task<PepCentricShowSequenceResponse> GetSequenceAsync(string jobId, string proteinId, double q = 0.01, double p = 0.01, double e = 0.01, double offset = 0)
    {
        var url = $"{_baseUrl}/showSequence?jobID={jobId}&proteinID={proteinId}&q={q}&p={p}&e={e}";
        _ = await WaitUntilJobComplete(jobId);

        var response = await GetJsonAsync<PepCentricShowSequenceResponse>(url);
        response.JobIdentifier = jobId;
        return response;
    }

    // gets all modified forms of a peptide from a given protein and peptide. 
    public async Task<PepCentricShowPeptideResponse> GetPeptideAsync(string jobId, string proteinId, int sequenceId, double p = 0.01, double e = 0.01)
    {
        var url = $"{_baseUrl}/showPeptide?jobID={jobId}&proteinID={proteinId}&sequenceID={sequenceId}&p={p}&e={e}";
        _ = await WaitUntilJobComplete(jobId);

        var response = await GetJsonAsync<PepCentricShowPeptideResponse>(url);
        response.JobIdentifier = jobId;
        return response;
    }

    // Gets all response from peptide for a given protein then all modified peptides. 
    public async Task<List<PepCentricShowPeptideResponse>> GetPeptidesFromSequence(string jobId, string proteinId, double q = 0.01, double p = 0.01, double e = 0.01, double offset = 0)
    {
        PepCentricShowSequenceResponse response = await GetSequenceAsync(jobId, proteinId, q, p, e, offset);
        response.JobIdentifier = jobId;

        var peptideResponses = new List<PepCentricShowPeptideResponse>();
        foreach (var sequence in response.Results)
        {
            var peptideResponse = await GetPeptideAsync(jobId, proteinId, sequence.ID, p, e);
            peptideResponse.JobIdentifier = jobId;
            peptideResponses.Add(peptideResponse);
        }
        return peptideResponses;
    }

    public async Task<List<PepCentricPeptide>> GetPeptidesFromFullSequences(List<string> peptides, string jobType = "peptide", string enzyme = "enzymatic", string email = "", string jobName = "")
    {
        var submission = await SubmitJobAsync(peptides, jobType, enzyme, email, jobName);
        var finished = await WaitUntilJobComplete(submission.JobIdentifier);

        var url = $"{_baseUrl}/result?jobID={submission.JobIdentifier}";
        var allResults = await GetJsonAsync<PepCentricProteinResponse>(url);

        var allPeptides = new List<PepCentricPeptide>();
        foreach (var result in allResults.Results)
        {
            var sequences = await GetSequenceAsync(submission.JobIdentifier, result.ID.ToString());
            foreach (var sequence in sequences.Results)
            {
                var peps = await GetPeptideAsync(submission.JobIdentifier, result.ID.ToString(), sequence.ID);
                allPeptides.AddRange(peps.Results);
            }
        }


        return allPeptides; 
    }





    #region Base API Calls - Not used Yet

    //public async Task<string> ShowSequenceAsync(string jobId, string proteinId, string q, string p, string e)
    //{
    //    return await _client.GetStringAsync($"{_baseUrl}/showSequence?jobID={jobId}&proteinID={proteinId}&q={q}&p={p}&e={e}");
    //}

    //public async Task<string> ShowPeptideAsync(string jobId, string proteinId, string sequenceId, string p, string e)
    //{
    //    return await _client.GetStringAsync($"{_baseUrl}/showPeptide?jobID={jobId}&proteinID={proteinId}&sequenceID={sequenceId}&p={p}&e={e}");
    //}

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

        return result!;
    }
}


