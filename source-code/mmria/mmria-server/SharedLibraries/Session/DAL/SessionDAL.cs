using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.server;
using mmria.server.model.actor;
using Newtonsoft.Json;

namespace mmria.server.SharedLibraries.DAL;

public class SessionDAL
{
    private readonly OverridableConfiguration _configuration;

    public SessionDAL(OverridableConfiguration configuration)
    {
        _configuration = configuration;
    }

    private DBConfigurationDetail GetDbConfig(string jurisdictionId)
    {
        return _configuration.GetDBConfig(jurisdictionId);
    }

    public async Task<document_put_response> CreateSessionAsync(Session_Message session, string jurisdictionId)
    {
        var dbConfig = GetDbConfig(jurisdictionId);
        string objectString = JsonConvert.SerializeObject(session, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/{session._id}";

        var curl = new cURL("PUT", null, requestUrl, objectString, dbConfig.user_name, dbConfig.user_value);
        string response = await curl.executeAsync();
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }
}