using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.DeIdentified.DAL;

public sealed class DeIdentifiedDAL : IDeIdentifiedRepository
{
    private readonly CouchDbHttpClient _httpClient;

    public DeIdentifiedDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetRevisionAsync(string id, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"de_id/{id}");
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        if (response.Contains("\"error\":\"not_found\""))
            return null;
        var doc = JObject.Parse(response);
        return doc.Value<string>("_rev");
    }

    public async Task<document_put_response> UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"de_id/{id}");
        string response = await _httpClient.ExecuteAsync("PUT", requestUrl, doc.ToString(Formatting.None), dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<document_put_response> DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"de_id/{id}?rev={rev}");
        string response = await _httpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<IEnumerable<document_put_response>> BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(new { docs });
        string requestUrl = dbConfig.Get_Prefix_DB_Url("de_id/_bulk_docs");
        string response = await _httpClient.ExecuteAsync("POST", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<List<document_put_response>>(response) ?? new List<document_put_response>();
    }

    public async Task DropAndResetAsync(DBConfigurationDetail dbConfig)
    {
        string dbUrl = dbConfig.Get_Prefix_DB_Url("de_id");
        await _httpClient.ExecuteAsync("DELETE", dbUrl, null, dbConfig.user_name, dbConfig.user_value);
        await _httpClient.ExecuteAsync("PUT", dbUrl, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"de_id/_design/{designName}");
        await _httpClient.ExecuteAsync("PUT", requestUrl, designDocJson, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("de_id/_index");
        await _httpClient.ExecuteAsync("POST", requestUrl, indexJson, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task WaitForIndexReadyAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("de_id/_design/sortable/_view/by_date_created?limit=1&update=true");
        await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<IDictionary<string, string>> GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(new { keys = ids });
        string requestUrl = dbConfig.Get_Prefix_DB_Url("de_id/_all_docs?include_docs=false");
        string response = await _httpClient.ExecuteAsync("POST", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        var result = new Dictionary<string, string>();
        var jObj = JObject.Parse(response);
        if (jObj["rows"] is not JArray rows)
            return result;
        foreach (var row in rows)
        {
            string? rowId = row["id"]?.Value<string>();
            string? rev = row["value"]?["rev"]?.Value<string>();
            if (rowId != null && rev != null)
                result[rowId] = rev;
        }
        return result;
    }
}
