using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.case_version.v260120;
using Newtonsoft.Json;

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
        var result = JsonConvert.DeserializeObject<mmria_case>(response);
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
        string objectString = JsonConvert.SerializeObject(caseDoc, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
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
}
