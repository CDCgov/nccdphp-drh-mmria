using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.cvs;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.CVS.DAL;

public sealed class CVSDAL
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly HttpClient _externalHttpClient;

    public CVSDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
        var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
        _externalHttpClient = httpClientFactory.CreateClient("external");
    }

    public async Task<string> PostExternalAsync(string base_url, object body)
    {
        var body_text = JsonSerializer.Serialize(body);
        var content = new StringContent(body_text, Encoding.UTF8, "application/json");
        var response = await _externalHttpClient.PostAsync(base_url, content);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PostInternalAsync(string base_url, object body, DBConfigurationDetail db_config)
    {
        var body_text = JsonSerializer.Serialize(body);
        return await _httpClient.ExecuteAsync("POST", base_url, body_text, db_config.user_name, db_config.user_value);
    }

    public async Task<case_view_response> GetCaseViewByRecordIdAsync(string recordId, DBConfigurationDetail db_config)
    {
        string request = db_config.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=30000&descending=true");
        string response = await _httpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(response);
        var result = new case_view_response { offset = case_view_response.offset, total_rows = case_view_response.total_rows };
        result.rows = case_view_response.rows.FindAll(cvi => cvi.value.record_id.Equals(recordId, System.StringComparison.OrdinalIgnoreCase));
        result.total_rows = result.rows.Count;
        return result;
    }

    public async Task<ExpandoObject> GetCaseAsync(string caseId, DBConfigurationDetail db_config)
    {
        string request = db_config.Get_Prefix_DB_Url($"mmrds/{caseId}");
        string response = await _httpClient.ExecuteAsync("GET", request, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
    }
}
