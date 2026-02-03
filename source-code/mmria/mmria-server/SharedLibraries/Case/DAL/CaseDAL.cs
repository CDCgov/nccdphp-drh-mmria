using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.server;
using mmria.case_version.v251014;
using Newtonsoft.Json;

namespace mmria.server.SharedLibraries.DAL;

public class CaseDAL
{
    private readonly OverridableConfiguration _configuration;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public CaseDAL(OverridableConfiguration configuration, mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _configuration = configuration;
        _couchDbHttpClient = couchDbHttpClient;
    }

    private DBConfigurationDetail GetDbConfig(string jurisdictionId)
    {
        return _configuration.GetDBConfig(jurisdictionId);
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, string jurisdictionId)
    {
        var dbConfig = GetDbConfig(jurisdictionId);
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<mmria_case>(response);
        return result;
    }

    public async Task<document_put_response> UpdateCaseAsync(string caseId, mmria_case caseDoc, string jurisdictionId)
    {
        var dbConfig = GetDbConfig(jurisdictionId);
        string objectString = JsonConvert.SerializeObject(caseDoc, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }
}