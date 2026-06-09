using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace mmria.common.SharedLibraries.DeIdentified.DAL;

public sealed class DeIdentifiedDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public DeIdentifiedDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<ExpandoObject> GetDeIdentifiedCaseAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        string requestUrl = string.IsNullOrWhiteSpace(caseId)
            ? dbConfig.Get_Prefix_DB_Url("de_id/_all_docs?include_docs=true")
            : dbConfig.Get_Prefix_DB_Url($"de_id/{caseId}");

        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value);

        return JsonConvert.DeserializeObject<ExpandoObject>(response, new ExpandoObjectConverter());
    }
}
