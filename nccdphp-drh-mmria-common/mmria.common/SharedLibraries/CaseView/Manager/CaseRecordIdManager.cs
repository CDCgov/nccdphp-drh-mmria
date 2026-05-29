using System;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.CaseView.DAL;

namespace mmria.common.SharedLibraries.CaseView.Manager;

public sealed class CaseRecordIdManager
{
    private const int DefaultLookupLimit = 25000;
    private readonly CaseViewDAL _dal;

    public CaseRecordIdManager(CaseViewDAL dal)
    {
        _dal = dal;
    }

    public async Task<bool> IsRecordIdUniqueAsync(string recordId, DBConfigurationDetail dbConfig)
    {
        var caseViewResponse = await _dal.GetCaseViewByDateCreatedAsync(0, DefaultLookupLimit, dbConfig);
        if (caseViewResponse?.rows == null)
        {
            return true;
        }

        foreach (var item in caseViewResponse.rows)
        {
            if (!string.IsNullOrWhiteSpace(item?.value?.record_id) &&
                item.value.record_id.Trim().Equals(recordId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
