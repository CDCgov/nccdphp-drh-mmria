using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.MMRIARebuild.Model.SummaryReport;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.DataSummary.DAL;

public sealed class DataSummaryDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public DataSummaryDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<get_sortable_view_reponse_header<FrequencySummaryDocument>> GetYearOfDeathSummaryAsync(
        int skip,
        int take,
        DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}report/_design/data_summary_view_report/_view/year_of_death?skip={skip}&limit={take}";
        string response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            dbConfig.user_name,
            dbConfig.user_value);

        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<FrequencySummaryDocument>>(response)
            ?? new get_sortable_view_reponse_header<FrequencySummaryDocument>();
    }
}
