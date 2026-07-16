using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;

namespace mmria.common.SharedLibraries.Report.DAL;

public sealed class ReportDAL : IReportRepository
{
    private readonly CouchDbHttpClient _httpClient;

    public ReportDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true");
        return await _httpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url($"report/_design/interactive_aggregate_report/_view/indicator_id?key=\"{indicatorId}\"");
        return await _httpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url($"report/_design/data_summary_view_report/_view/year_of_death?skip={skip}&limit={take}");
        return await _httpClient.ExecuteAsync("GET", requestString, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<string> FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig)
    {
        string requestString = dbConfig.Get_Prefix_DB_Url("report/_find");
        return await _httpClient.ExecuteAsync("POST", requestString, selectorJson, dbConfig.user_name, dbConfig.user_value);
    }
}
