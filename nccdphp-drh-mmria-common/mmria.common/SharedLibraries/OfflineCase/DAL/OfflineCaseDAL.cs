using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.OfflineCase.Model;
using mmria.common.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.OfflineCase.DAL;

public class OfflineCaseDAL : IOfflineCaseRepository
{
    private const string OfflineCasesByCreatedByViewPath = "offline_cases/_design/sortable/_view/by-created-by";
    private const string LightweightStatusOnlyViewPath = "offline_cases/_design/sortable/_view/lightweight-status-only";
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private static readonly JsonSerializerSettings CaseAwareSerializerSettings = CaseJsonSerialization.CreateNewtonsoftSerializerSettings();
    private static readonly JsonSerializerSettings CaseAwareSerializerSettingsIgnoreNulls = CaseJsonSerialization.CreateNewtonsoftSerializerSettings(ignoreNulls: true);

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

        string objectString = JsonConvert.SerializeObject(doc, CaseAwareSerializerSettingsIgnoreNulls);
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"offline_cases/{documentId}");

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"offline_cases/{id}");

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<OfflineCaseResponse>(response, CaseAwareSerializerSettings);
        return result;
    }

    public async Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = BuildOfflineCasesByCreatedByUserUrl(userId, dbConfig);

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");

        var offline_case_documents = JsonConvert.DeserializeObject<OfflineCaseListResponse>(response, CaseAwareSerializerSettings);

        return offline_case_documents;
    }

    public async Task<OfflineCaseListResponse> TryGetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return CreateEmptyOfflineCaseListResponse();
        }

        string requestUrl = BuildOfflineCasesByCreatedByUserUrl(userId, dbConfig);
        var response = await _couchDbHttpClient.ExecuteForResponseAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value,
            "application/json");

        if (response.StatusCode != (int)HttpStatusCode.OK)
        {
            LogOfflineSessionLookupFailure(
                dbConfig,
                requestUrl,
                response.StatusCode,
                response.Body,
                "Scoped offline session lookup returned a non-success status.");
            return CreateEmptyOfflineCaseListResponse();
        }

        if (string.IsNullOrWhiteSpace(response.Body))
        {
            LogOfflineSessionLookupFailure(
                dbConfig,
                requestUrl,
                response.StatusCode,
                response.Body,
                "Scoped offline session lookup returned an empty body.");
            return CreateEmptyOfflineCaseListResponse();
        }

        try
        {
            var offlineCaseDocuments = JsonConvert.DeserializeObject<OfflineCaseListResponse>(response.Body, CaseAwareSerializerSettings);
            return offlineCaseDocuments ?? CreateEmptyOfflineCaseListResponse();
        }
        catch (JsonException ex)
        {
            LogOfflineSessionLookupFailure(
                dbConfig,
                requestUrl,
                response.StatusCode,
                response.Body,
                $"Scoped offline session lookup returned invalid JSON: {ex.Message}");
            return CreateEmptyOfflineCaseListResponse();
        }
    }

    public async Task<string> GetActiveSessionIdForUserInAnotherTabAsync(string userId, string currentTabId, DBConfigurationDetail dbConfig)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(currentTabId))
        {
            return null;
        }

        string requestUrl = BuildOfflineCasesByCreatedByUserUrl(userId, dbConfig);

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
        string requestUrl = dbConfig.Get_Prefix_DB_Url(OfflineCasesByCreatedByViewPath);

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var couchResponse = JsonConvert.DeserializeObject<OfflineCaseListResponse>(response, CaseAwareSerializerSettings);
        var rows = couchResponse?.rows?
            .Where(row => row?.value != null && (row.value.offline_state == 0 || row.value.offline_state == 1))
            .ToList() ?? new List<OfflineCaseItem>();

        return new OfflineCaseListResponse(0, rows, rows.Count);
    }

    public async Task<document_put_response> UpdateOfflineCaseAsync(string id, OfflineCaseResponse updatedDoc, DBConfigurationDetail dbConfig)
    {
        string objectString = JsonConvert.SerializeObject(updatedDoc, CaseAwareSerializerSettingsIgnoreNulls);
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"offline_cases/{id}");

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<document_put_response> DeleteOfflineCaseAsync(string id, string rev, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"offline_cases/{id}?rev={rev}");

        string response = await _couchDbHttpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<LightweightOfflineCaseListResponse> GetAllLightweightOfflineCasesAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url(LightweightStatusOnlyViewPath);
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        return JsonConvert.DeserializeObject<LightweightOfflineCaseListResponse>(response) ?? new LightweightOfflineCaseListResponse();
    }

    private static string BuildOfflineCasesByCreatedByUserUrl(string userId, DBConfigurationDetail dbConfig)
    {
        var encodedUserId = Uri.EscapeDataString($"\"{userId}\"");
        return dbConfig.Get_Prefix_DB_Url($"{OfflineCasesByCreatedByViewPath}?startkey={encodedUserId}&endkey={encodedUserId}");
    }

    private static OfflineCaseListResponse CreateEmptyOfflineCaseListResponse()
    {
        return new OfflineCaseListResponse(0, new List<OfflineCaseItem>(), 0);
    }

    private static void LogOfflineSessionLookupFailure(
        DBConfigurationDetail dbConfig,
        string requestUrl,
        int statusCode,
        string responseBody,
        string message)
    {
        var prefix = dbConfig?.prefix ?? "(null)";
        var tenantUrl = dbConfig?.url ?? "(null)";
        var snippet = string.IsNullOrWhiteSpace(responseBody)
            ? "(empty)"
            : responseBody.Substring(0, Math.Min(responseBody.Length, 300));

        Console.WriteLine(
            $"[OfflineCaseDAL] {message} tenant_prefix={prefix} tenant_url={tenantUrl} request_url={requestUrl} status_code={statusCode} body_snippet={snippet}");
    }
}
