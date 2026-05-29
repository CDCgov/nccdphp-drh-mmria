using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.DataSummary.DAL;
using mmria.common.SharedLibraries.MMRIARebuild.Model.SummaryReport;

namespace mmria.common.SharedLibraries.DataSummary.Manager;

public sealed class DataSummaryManager
{
    private const int DefaultTake = 100;
    private readonly DataSummaryDAL _dal;

    public DataSummaryManager(DataSummaryDAL dal)
    {
        _dal = dal;
    }

    public async Task<get_sortable_view_reponse_header<FrequencySummaryDocument>> GetYearOfDeathSummaryAsync(
        string skip,
        DBConfigurationDetail dbConfig)
    {
        int.TryParse(skip, out int skipNumber);
        return await _dal.GetYearOfDeathSummaryAsync(skipNumber, DefaultTake, dbConfig);
    }
}
