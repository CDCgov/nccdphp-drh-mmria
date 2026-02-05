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
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public SessionDAL(OverridableConfiguration configuration, mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _configuration = configuration;
        _couchDbHttpClient = couchDbHttpClient;
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

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }
}