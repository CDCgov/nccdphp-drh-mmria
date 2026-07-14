using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.case_version.v260615;
using mmria.common.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Case.DAL;

public class CaseDAL : ICaseRepository
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public CaseDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = CaseJsonSerialization.DeserializeMmriaCase(response);
        return result;
    }

    public async Task<string> GetCaseDocumentJsonAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<document_put_response> UpdateCaseAsync(string caseId, mmria_case caseDoc, DBConfigurationDetail dbConfig)
    {
        string objectString = CaseJsonSerialization.SerializeMmriaCase(caseDoc);
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<string> PutCaseDocumentJsonAsync(string caseId, string caseDocumentJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");

        return await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            requestUrl,
            caseDocumentJson,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> DeleteCaseAsync(string caseId, string revision, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={revision}");

        return await _couchDbHttpClient.ExecuteAsync(
            "DELETE",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCaseAtRevisionAsync(string caseId, string revision, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={revision}");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCaseRevisionsAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?revs_info=true");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCaseRevisionsRawAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?revs=true&open_revs=all");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCasesByDateLastUpdatedViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCasesByDateCreatedViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCasesByJurisdictionIdViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_jurisdiction_id?skip=0&take=100000");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCasesByLastNameViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_last_name?skip=0&limit=100000");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCasesByPmssNumberViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_pmss_number?skip=0&take=250000");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCaseRecordIdListViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/record_id_list");

        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetSoftLockedCaseIdForUserInAnotherTabAsync(string userName, string currentTabId, DBConfigurationDetail dbConfig)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(currentTabId))
        {
            return null;
        }

        var selector = new JObject
        {
            ["offline_by"] = userName,
            ["is_offline"] = true,
            ["offline_lock_type"] = 1,
            ["offline_by_tab_id"] = new JObject
            {
                ["$exists"] = true,
                ["$ne"] = currentTabId
            }
        };

        var requestBody = new JObject
        {
            ["selector"] = selector,
            ["fields"] = new JArray("_id"),
            ["limit"] = 1
        };

        var response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            dbConfig.Get_Prefix_DB_Url("mmrds/_find"),
            requestBody.ToString(Formatting.None),
            dbConfig.user_name,
            dbConfig.user_value,
            "application/json"
        );

        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var parsed = JObject.Parse(response);
        var docs = parsed["docs"] as JArray;
        return docs?.FirstOrDefault()?["_id"]?.ToString();
    }

    public async Task<bool> RecordIdExistsAsync(string recordId, DBConfigurationDetail dbInfo)
    {
        if (string.IsNullOrWhiteSpace(recordId) || dbInfo == null)
        {
            return false;
        }

        try
        {
            var selectorPayload = new
            {
                selector = new
                {
                    record_id = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["$eq"] = recordId
                    }
                },
                fields = new[] { "_id" },
                limit = 1
            };

            string payload = JsonConvert.SerializeObject(selectorPayload);
            string requestUrl = dbInfo.Get_Prefix_DB_Url("mmrds/_find");

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "POST",
                requestUrl,
                payload,
                dbInfo.user_name,
                dbInfo.user_value,
                "application/json");

            if (string.IsNullOrEmpty(responseFromServer))
            {
                return false;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(responseFromServer);
            if (doc.RootElement.TryGetProperty("docs", out var docsElement) &&
                docsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                return docsElement.GetArrayLength() > 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RecordIdExistsAsync error for record_id={recordId}: {ex.Message}");
            return true;
        }

        return false;
    }

    public async Task<(int StatusCode, string Body)> GetCaseDocumentWithStatusAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
        var response = await _couchDbHttpClient.ExecuteForResponseAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value);
        return (response.StatusCode, response.Body);
    }
}
