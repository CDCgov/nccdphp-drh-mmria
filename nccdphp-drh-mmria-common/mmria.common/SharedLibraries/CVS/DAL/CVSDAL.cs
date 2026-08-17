using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.cvs;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Case;

namespace mmria.common.SharedLibraries.CVS.DAL;

public sealed record CVSExternalResponse
{
    public int StatusCode { get; init; }
    public string Body { get; init; }
    public bool IsSuccess { get; init; }
}

public sealed class CVSDAL
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly HttpClient _externalHttpClient;
    private readonly ICaseRepository _caseRepository;

    public CVSDAL(CouchDbHttpClient httpClient, ICaseRepository caseRepository)
    {
        _httpClient = httpClient;
        var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
        _externalHttpClient = httpClientFactory.CreateClient("external");
        _caseRepository = caseRepository;
    }

    public async Task<string> PostExternalAsync(string base_url, object body)
    {
        var requestUri = ValidateCvsServiceUri(base_url);
        var body_text = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(body_text, Encoding.UTF8, "application/json");
        var response = await _externalHttpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<CVSExternalResponse> PostExternalForResponseAsync(string base_url, object body)
    {
        var requestUri = ValidateCvsServiceUri(base_url);
        var body_text = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(body_text, Encoding.UTF8, "application/json");
        using var response = await _externalHttpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        return new CVSExternalResponse
        {
            StatusCode = (int)response.StatusCode,
            Body = responseBody,
            IsSuccess = response.IsSuccessStatusCode
        };
    }

    public async Task<string> PostInternalAsync(string base_url, object body, DBConfigurationDetail db_config)
    {
        var requestUri = ValidateCvsServiceUri(base_url);
        var body_text = JsonSerializer.Serialize(body);
        return await _httpClient.ExecuteAsync("POST", requestUri.AbsoluteUri, body_text, db_config.user_name, db_config.user_value);
    }

    public async Task<case_view_response> GetCaseViewByRecordIdAsync(string recordId, DBConfigurationDetail db_config)
    {
        // CVSDAL uses limit=30000 (vs CaseDAL default 25000) to ensure all cases are fetched before filtering by record_id.
        string response = await _caseRepository.GetCasesByDateLastUpdatedViewJsonAsync(db_config, 30000);
        var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(response);
        var result = new case_view_response { offset = case_view_response.offset, total_rows = case_view_response.total_rows };
        result.rows = case_view_response.rows.FindAll(cvi => cvi.value.record_id.Equals(recordId, System.StringComparison.OrdinalIgnoreCase));
        result.total_rows = result.rows.Count;
        return result;
    }

    public async Task<ExpandoObject> GetCaseAsync(string caseId, DBConfigurationDetail db_config)
    {
        string response = await _caseRepository.GetCaseDocumentJsonAsync(caseId, db_config);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
    }

    private static Uri ValidateCvsServiceUri(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new System.ArgumentException("CVS service URL is required.", nameof(baseUrl));
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedUri))
        {
            throw new System.ArgumentException("CVS service URL must be an absolute URI.", nameof(baseUrl));
        }

        if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new System.ArgumentException("CVS service URL must use HTTP or HTTPS.", nameof(baseUrl));
        }

        if (!string.IsNullOrWhiteSpace(parsedUri.UserInfo) || !string.IsNullOrWhiteSpace(parsedUri.Fragment))
        {
            throw new System.ArgumentException("CVS service URL must not contain user info or fragments.", nameof(baseUrl));
        }

        return new UriBuilder(parsedUri)
        {
            Fragment = string.Empty
        }.Uri;
    }
}
