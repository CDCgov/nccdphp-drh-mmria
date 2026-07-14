using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Case;

namespace mmria.common.SharedLibraries.VitalImport.DAL;

public sealed class VitalImportDAL
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly ICaseRepository _caseRepository;

    public VitalImportDAL(CouchDbHttpClient httpClient, ICaseRepository caseRepository)
    {
        _httpClient = httpClient;
        _caseRepository = caseRepository;
    }

    public async Task<case_view_response> GetCaseViewAsync(string request_string, DBConfigurationDetail db_config)
    {
        string response = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(response);
    }

    public async Task<ExpandoObject> GetCaseAsync(string case_id, DBConfigurationDetail db_config)
    {
        string response = await _caseRepository.GetCaseDocumentJsonAsync(case_id, db_config);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
    }

    public async Task<document_put_response> PutCaseAsync(string id, string document_content, DBConfigurationDetail db_config)
    {
        string response = await _caseRepository.PutCaseDocumentJsonAsync(id, document_content, db_config);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<ExpandoObject> DeleteCaseAsync(string request_string, DBConfigurationDetail db_config)
    {
        string response = await _httpClient.ExecuteAsync("DELETE", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
    }

    public async Task<alldocs_response<mmria.common.ije.Batch>> GetBatchSetAsync(DBConfigurationDetail db_config)
    {
        string url = $"{db_config.url}/vital_import/_all_docs?include_docs=true";
        string response = await _httpClient.ExecuteAsync("GET", url, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<alldocs_response<mmria.common.ije.Batch>>(response);
    }
}
