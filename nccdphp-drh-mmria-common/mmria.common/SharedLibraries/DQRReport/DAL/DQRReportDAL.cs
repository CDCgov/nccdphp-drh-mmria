using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.SharedLibraries.DQRReport.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.DQRReport.DAL;

public sealed class DQRReportDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public DQRReportDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<DQRReportResult> FindDqrDetailsAsync(string selectorJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}report/_find";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            requestUrl,
            selectorJson,
            dbConfig.user_name,
            dbConfig.user_value);

        return JsonConvert.DeserializeObject<DQRReportResult>(response) ?? new DQRReportResult();
    }
}
