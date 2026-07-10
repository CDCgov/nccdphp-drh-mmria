using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.case_version.v260615;
using mmria.common.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Case.DAL;

public class CaseDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public CaseDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = CaseJsonSerialization.DeserializeMmriaCase(response);
        return result;
    }

    public async Task<string> GetCaseDocumentJsonAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

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
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }

    public async Task<string> PutCaseDocumentJsonAsync(string caseId, string caseDocumentJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        return await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            requestUrl,
            caseDocumentJson,
            dbConfig.user_name,
            dbConfig.user_value
        );
    }

    public async Task<string> GetCasesByDateLastUpdatedViewJsonAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true";

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
        string requestUrl;
        if (string.IsNullOrWhiteSpace(dbConfig.prefix))
        {
            requestUrl = $"{dbConfig.url}/mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";
        }
        else
        {
            requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";
        }

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
}
