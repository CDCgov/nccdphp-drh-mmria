using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Session.DAL;

public class SessionDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public SessionDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<document_put_response> CreateSessionAsync(Session_Message session, DBConfigurationDetail dbConfig)
    {
        string objectString = JsonConvert.SerializeObject(session, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}session/{session._id}";

        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, objectString, dbConfig.user_name, dbConfig.user_value, "application/json");
        var result = JsonConvert.DeserializeObject<document_put_response>(response);
        return result;
    }
}
