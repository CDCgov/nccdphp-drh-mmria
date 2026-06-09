using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Jurisdiction.DAL;

public sealed class JurisdictionDAL
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public JurisdictionDAL(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<jurisdiction_tree> GetJurisdictionTreeAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");
        string response = await _couchDbHttpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<jurisdiction_tree>(response);
    }

    public async Task<document_put_response> SaveJurisdictionTreeAsync(jurisdiction_tree jurisdictionTree, DBConfigurationDetail dbConfig)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        string requestBody = JsonConvert.SerializeObject(jurisdictionTree, settings);
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");
        string response = await _couchDbHttpClient.ExecuteAsync("PUT", requestUrl, requestBody, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
