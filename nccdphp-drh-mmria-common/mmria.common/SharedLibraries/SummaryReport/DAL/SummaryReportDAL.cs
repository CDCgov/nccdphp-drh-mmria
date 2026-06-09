using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.SummaryReport.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.SummaryReport.DAL;

public sealed class SummaryReportDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public SummaryReportDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<get_response_header<user>> GetUsersAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/_users/_all_docs?include_docs=true&skip=1";
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        return JsonConvert.DeserializeObject<get_response_header<user>>(response) ?? new get_response_header<user>();
    }

    public async Task<case_view_response> GetCaseJurisdictionViewAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_jurisdiction_id?skip=0&take=100000";
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        return JsonConvert.DeserializeObject<case_view_response>(response) ?? new case_view_response();
    }

    public async Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionsAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}jurisdiction/_design/sortable/_view/by_date_created?skip=0&limit=20000";
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<user_role_jurisdiction>>(response)
            ?? new get_sortable_view_reponse_header<user_role_jurisdiction>();
    }

    public async Task<view_response<SessionSummaryDocument>> GetRecentSessionsAsync(DBConfigurationDetail dbConfig, int limit)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/_design/session_sortable/_view/by_date_created?descending=true&limit={limit}";
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        return JsonConvert.DeserializeObject<view_response<SessionSummaryDocument>>(response) ?? new view_response<SessionSummaryDocument>();
    }
}
