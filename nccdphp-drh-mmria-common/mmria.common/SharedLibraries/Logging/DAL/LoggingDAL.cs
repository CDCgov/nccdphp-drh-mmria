using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Logging.DAL;

public class LoggingDAL : ILoggingRepository
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public LoggingDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<dynamic> GetLoggingModulesAsync(DBConfigurationDetail dbConfig)
    {
        string url = dbConfig.Get_Prefix_DB_Url("logging/_design/sortable/_view/by-offline-session");
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<dynamic>(response);
    }

    public async Task<string> GetFilteredLoggingAsync(string filterOrViewPath, DBConfigurationDetail dbConfig)
    {
        string url = dbConfig.Get_Prefix_DB_Url($"logging/{filterOrViewPath}");
        return await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<document_put_response> PostLoggingDocumentAsync(string documentJson, DBConfigurationDetail dbConfig)
    {
        string url = dbConfig.Get_Prefix_DB_Url("logging");
        string response = await _couchDbHttpClient.ExecuteAsync("POST", url, documentJson, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
