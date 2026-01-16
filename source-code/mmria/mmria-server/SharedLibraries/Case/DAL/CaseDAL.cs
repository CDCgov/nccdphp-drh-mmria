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

    public CaseDAL(OverridableConfiguration configuration)
    {
        _configuration = configuration;
    }

    private DBConfigurationDetail GetDbConfig(string jurisdictionId)
    {
        return _configuration.GetDBConfig(jurisdictionId);
    }

    public async Task<mmria_case> GetCaseAsync(string caseId, string jurisdictionId)
    {
        var dbConfig = GetDbConfig(jurisdictionId);
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        var curl = new cURL("GET", null, requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        string response = await curl.executeAsync();
        var result = JsonConvert.DeserializeObject<mmria_case>(response);
        return result;
    }

    public async Task<document_put_response> UpdateCaseAsync(string caseId, mmria_case caseDoc, string jurisdictionId)
    {
        var dbConfig = GetDbConfig(jurisdictionId);
        string objectString = JsonConvert.SerializeObject(caseDoc, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";

        var curl = new cURL("PUT", null, requestUrl, objectString, dbConfig.user_name, dbConfig.user_value);
        string response = await curl.executeAsync();
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }
}