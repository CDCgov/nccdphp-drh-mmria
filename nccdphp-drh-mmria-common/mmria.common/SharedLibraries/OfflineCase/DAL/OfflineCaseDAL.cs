using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.OfflineCase.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.OfflineCase.DAL;

public class OfflineCaseDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public OfflineCaseDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, DBConfigurationDetail dbConfig)
    {
        string documentId = Guid.NewGuid().ToString();

        var doc = new
        {
            _id = documentId,
            offline_ids = request.offline_ids,
            offline_key = request.offline_key,
            offline_state = 0,
            case_documents = new List<DocumentChange>(),
            created_by = userName,
            created_by_tab_id = request.tab_id,
            date_created = DateTime.UtcNow,
            last_updated_by = userName,
            date_last_updated = DateTime.UtcNow
        };

        string objectString = JsonConvert.SerializeObject(doc, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}offline_cases/{documentId}";

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}offline_cases/{id}";

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<OfflineCaseResponse>(response);
        return result;
    }

    public async Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig)
    {
        // Use sortable view by-created-by
        string requestUrl = dbConfig.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/by-created-by");

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");

        var offline_case_documents = Newtonsoft.Json.JsonConvert.DeserializeObject<OfflineCaseListResponse>(response);


        return offline_case_documents;
    }

    public async Task<string> GetActiveSessionIdForUserInAnotherTabAsync(string userId, string currentTabId, DBConfigurationDetail dbConfig)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(currentTabId))
        {
            return null;
        }

        string requestUrl = dbConfig.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/by-created-by");

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var parsed = JObject.Parse(response);
        var rows = parsed["rows"] as JArray;
        if (rows == null)
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (!string.Equals(row["key"]?.ToString(), userId, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = row["value"] as JObject;
            if (value == null)
            {
                continue;
            }

            var offlineState = value["offline_state"]?.Value<int?>();
            if (offlineState != 0 && offlineState != 1)
            {
                continue;
            }

            var createdByTabId = value["created_by_tab_id"]?.ToString();
            if (!string.IsNullOrWhiteSpace(createdByTabId) &&
                !string.Equals(createdByTabId, currentTabId, System.StringComparison.Ordinal))
            {
                return row["id"]?.ToString() ?? value["_id"]?.ToString();
            }
        }

        return null;
    }

    public async Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(DBConfigurationDetail dbConfig)
    {
        // Use sortable view by-created-by
        string requestUrl = dbConfig.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/by-created-by");

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var couchResponse = JsonConvert.DeserializeObject<dynamic>(response);

        var rows = new List<OfflineCaseItem>();
        foreach (var row in couchResponse.rows)
        {
            var doc = JsonConvert.DeserializeObject<OfflineCaseResponse>(row.value.ToString());
            if (doc.offline_state == 0 || doc.offline_state == 1)
            {
                rows.Add(new OfflineCaseItem
                {
                    id = row.id,
                    key = row.key,
                    value = doc
                });
            }
        }

        return new OfflineCaseListResponse(0, rows, rows.Count);
    }

    public async Task<document_put_response> UpdateOfflineCaseAsync(string id, OfflineCaseResponse updatedDoc, DBConfigurationDetail dbConfig)
    {
        string objectString = JsonConvert.SerializeObject(updatedDoc, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}offline_cases/{id}";

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<document_put_response> DeleteOfflineCaseAsync(string id, string rev, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}offline_cases/{id}?rev={rev}";

        string response = await _couchDbHttpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }
}
