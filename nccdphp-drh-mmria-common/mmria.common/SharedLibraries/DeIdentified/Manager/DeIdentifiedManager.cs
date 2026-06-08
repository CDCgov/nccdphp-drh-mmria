using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.DeIdentified.DAL;

namespace mmria.common.SharedLibraries.DeIdentified.Manager;

public sealed class DeIdentifiedManager
{
    private readonly DeIdentifiedDAL _dal;

    public DeIdentifiedManager(DeIdentifiedDAL dal)
    {
        _dal = dal;
    }

    public async Task<ExpandoObject> GetDeIdentifiedCaseAsync(string caseId, DBConfigurationDetail dbConfig)
    {
        return await _dal.GetDeIdentifiedCaseAsync(caseId, dbConfig);
    }
}
