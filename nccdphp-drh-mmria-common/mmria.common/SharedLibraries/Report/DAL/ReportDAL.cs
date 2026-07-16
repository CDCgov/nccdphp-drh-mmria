using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Report.DAL;

public sealed class ReportDAL : IReportRepository
{
    private readonly CouchDbHttpClient _httpClient;

    public ReportDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ── Read operations (Story 23.6) ─────────────────────────────────────────

    public async Task<string> GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true");
        return await _httpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url($"report/_design/interactive_aggregate_report/_view/indicator_id?key=\"{indicatorId}\"");
        return await _httpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url($"report/_design/data_summary_view_report/_view/year_of_death?skip={skip}&limit={take}");
        return await _httpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url("report/_find");
        return await _httpClient.ExecuteAsync("POST", requestString, selectorJson, dbConfig.user_name, dbConfig.user_value);
    }

    // ── Write and lifecycle operations (Story 24.2) ──────────────────────────

    public async Task<string?> GetRevisionAsync(string id, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"report/{id}");
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        if (response.Contains("\"error\":\"not_found\""))
            return null;
        var doc = JObject.Parse(response);
        return doc.Value<string>("_rev");
    }

    public async Task<document_put_response> UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"report/{id}");
        string response = await _httpClient.ExecuteAsync("PUT", requestUrl, doc.ToString(Formatting.None), dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<document_put_response> DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"report/{id}?rev={rev}");
        string response = await _httpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public async Task<IEnumerable<document_put_response>> BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(new { docs });
        string requestUrl = dbConfig.Get_Prefix_DB_Url("report/_bulk_docs");
        string response = await _httpClient.ExecuteAsync("POST", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<List<document_put_response>>(response) ?? new List<document_put_response>();
    }

    public async Task DropAndResetWithSystemDocPreservationAsync(DBConfigurationDetail dbConfig)
    {
        // 1. Fetch system/config documents to preserve across the DROP+CREATE cycle.
        const string preserveSelectorJson = "{\"selector\":{\"type\":{\"$in\":[\"system\",\"config\"]}},\"limit\":10000}";
        string findResponse = await _httpClient.ExecuteAsync(
            "POST",
            dbConfig.Get_Prefix_DB_Url("report/_find"),
            preserveSelectorJson,
            dbConfig.user_name,
            dbConfig.user_value);

        var findResult = JObject.Parse(findResponse);
        var preservedDocs = findResult["docs"] as JArray ?? new JArray();

        // Strip _rev before re-insertion (documents are new in the recreated database).
        foreach (var doc in preservedDocs)
        {
            (doc as JObject)?.Remove("_rev");
        }

        // 2. DROP the report database.
        string dbUrl = dbConfig.Get_Prefix_DB_Url("report");
        await _httpClient.ExecuteAsync("DELETE", dbUrl, null, dbConfig.user_name, dbConfig.user_value);

        // 3. Recreate the report database empty.
        await _httpClient.ExecuteAsync("PUT", dbUrl, null, dbConfig.user_name, dbConfig.user_value);

        // 4. Re-insert preserved documents if any were found.
        if (preservedDocs.Count > 0)
        {
            string bulkPayload = JsonConvert.SerializeObject(new { docs = preservedDocs });
            await _httpClient.ExecuteAsync(
                "POST",
                dbConfig.Get_Prefix_DB_Url("report/_bulk_docs"),
                bulkPayload,
                dbConfig.user_name,
                dbConfig.user_value);
        }
    }

    public async Task EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"report/_design/{designName}");
        await _httpClient.ExecuteAsync("PUT", requestUrl, designDocJson, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("report/_index");
        await _httpClient.ExecuteAsync("POST", requestUrl, indexJson, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task WaitForIndexReadyAsync(DBConfigurationDetail dbConfig)
    {
        // Minimal _find to force CouchDB to warm the Mango index before proceeding.
        const string barrierSelector = "{\"selector\":{},\"limit\":1}";
        await _httpClient.ExecuteAsync(
            "POST",
            dbConfig.Get_Prefix_DB_Url("report/_find"),
            barrierSelector,
            dbConfig.user_name,
            dbConfig.user_value);
    }

    public async Task<IDictionary<string, string>> GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(new { keys = ids });
        string requestUrl = dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=false");
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
