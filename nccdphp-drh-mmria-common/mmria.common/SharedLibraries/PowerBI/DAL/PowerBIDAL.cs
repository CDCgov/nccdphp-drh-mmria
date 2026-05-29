using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.SharedLibraries.PowerBI.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.PowerBI.DAL;

public sealed class PowerBIDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public PowerBIDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<PowerBIMeasureResult> FindPowerBIMeasuresAsync(string selectorJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}report/_find";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            requestUrl,
            selectorJson,
            dbConfig.user_name,
            dbConfig.user_value);

        return JsonConvert.DeserializeObject<PowerBIMeasureResult>(response) ?? new PowerBIMeasureResult();
    }
}
