using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.CaseView.DAL;

public sealed class CaseViewDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public CaseViewDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<case_view_response> GetCaseViewResponseAsync(string request_string, DBConfigurationDetail db_config)
    {
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<case_view_response>(responseFromServer);
    }

    public async Task<case_view_response> GetCaseViewByDateCreatedAsync(int skip, int take, DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url($"mmrds/_design/sortable/_view/by_date_created?skip={skip}&take={take}");
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<case_view_response>(responseFromServer);
    }

    public async Task<pinned_case_set> GetPinnedCaseSetAsync(string request_string, DBConfigurationDetail db_config)
    {
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<pinned_case_set>(responseFromServer);
    }

    public async Task<document_put_response> SavePinnedCaseSetAsync(string request_string, string document_content, DBConfigurationDetail db_config)
    {
        string responseFromServer = await _httpClient.ExecuteAsync("PUT", request_string, document_content, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
    }

    public async Task<ExpandoObject> GetCaseDocumentAsync(string case_id, DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<ExpandoObject>(responseFromServer);
    }
}
