using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.metadata;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.SystemOffline.DAL;

public sealed class SystemOfflineDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public SystemOfflineDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<SystemOfflineConfig> LoadConfigAsync(
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
    {
        var url = $"{servicesBaseUrl}/api/systemOffline/GetSystemOfflineConfig";
        var responseBody = await _couchDbHttpClient.ExecuteAsync(
            "GET", url, null, "application/json", requestOptions);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<SystemOfflineConfig>(responseBody)
            ?? new SystemOfflineConfig();
    }

    public async Task<document_put_response> SaveConfigAsync(
        SystemOfflineConfig config,
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
    {
        var url = $"{servicesBaseUrl}/api/systemOffline/SaveSystemOfflineConfig";
        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, settings);
        var responseBody = await _couchDbHttpClient.ExecuteAsync(
            "POST", url, json, "application/json", requestOptions);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(responseBody)
            ?? new document_put_response { ok = false };
    }
}
