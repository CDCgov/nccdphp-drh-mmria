using System.Threading.Tasks;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.Report;

/// <summary>
/// Repository interface for application-layer read operations against the report database.
/// ReportDAL is the sole implementation. A SQL migration requires only a new implementation
/// of this interface — no caller changes needed.
/// Write and rebuild operations (DROP DB, CREATE DB, bulk PUT, _index creation, design document PUT)
/// are infrastructure concerns handled by sync/rebuild actors and are intentionally excluded.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// GET report/_all_docs?include_docs=true
    /// </summary>
    Task<string> GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET report/_design/interactive_aggregate_report/_view/indicator_id?key="indicatorId"
    /// </summary>
    Task<string> GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET report/_design/data_summary_view_report/_view/year_of_death?skip=N&amp;limit=N
    /// </summary>
    Task<string> GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST report/_find with the provided Mango selector JSON body.
    /// </summary>
    Task<string> FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig);
}
