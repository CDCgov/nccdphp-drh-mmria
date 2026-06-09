using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.SharedLibraries.OverdoseReport.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.OverdoseReport.DAL;

public sealed class OverdoseReportDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public OverdoseReportDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<OverdoseMeasureResult> FindOverdoseMeasuresAsync(string selectorJson, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}report/_find";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "POST",
            requestUrl,
            selectorJson,
            dbConfig.user_name,
            dbConfig.user_value);

        return JsonConvert.DeserializeObject<OverdoseMeasureResult>(response) ?? new OverdoseMeasureResult();
    }
}
