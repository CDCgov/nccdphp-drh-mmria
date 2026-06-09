using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.HealthDiagnostics.DAL;

namespace mmria.common.SharedLibraries.HealthDiagnostics.Manager;

public sealed class HealthDiagnosticsManager
{
    private readonly HealthDiagnosticsDAL _dal;

    public HealthDiagnosticsManager(HealthDiagnosticsDAL dal)
    {
        _dal = dal;
    }

    public async Task<bool> IsMmrdsHealthyAsync(DBConfigurationDetail dbConfig)
    {
        return await _dal.UrlEndpointExistsAsync(dbConfig, "mmrds");
    }
}
