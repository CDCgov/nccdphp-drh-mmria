using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.ExportQueue.DAL;

public sealed class ExportQueueDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public ExportQueueDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExpandoObject> GetAllQueueDocumentsAsync(DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("export_queue/_all_docs?include_docs=true");
        string response = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(response);
    }

    public async Task<T> GetQueueDocumentAsync<T>(string id, DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("export_queue/" + id);
        string response = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(response);
    }

    public async Task<document_put_response> SaveQueueDocumentAsync(string id, string document_content, DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("export_queue/" + id);
        string response = await _httpClient.ExecuteAsync("PUT", request_string, document_content, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<string> TriggerExportQueueServiceAsync(
        string service_url,
        string request_json,
        string vitalServiceKey)
    {
        return await _httpClient.ExecuteAsync(
            "POST",
            service_url,
            request_json,
            "application/json",
            new CouchDbRequestOptions
            {
                VitalServiceKey = vitalServiceKey
            });
    }
}
