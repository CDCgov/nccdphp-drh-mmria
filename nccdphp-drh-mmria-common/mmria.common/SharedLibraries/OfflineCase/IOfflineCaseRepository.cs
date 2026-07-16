using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.OfflineCase.Model;

namespace mmria.common.SharedLibraries.OfflineCase;

/// <summary>
/// Repository interface for all offline_cases database operations.
/// OfflineCaseDAL is the sole implementation. A SQL migration requires
/// only a new implementation of this interface — no caller changes needed.
/// </summary>
public interface IOfflineCaseRepository
{
    Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, DBConfigurationDetail dbConfig);
    Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, DBConfigurationDetail dbConfig);
    Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig);
    Task<OfflineCaseListResponse> TryGetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig);
    Task<string> GetActiveSessionIdForUserInAnotherTabAsync(string userId, string currentTabId, DBConfigurationDetail dbConfig);
    Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(DBConfigurationDetail dbConfig);
    Task<document_put_response> UpdateOfflineCaseAsync(string id, OfflineCaseResponse updatedDoc, DBConfigurationDetail dbConfig);
    Task<document_put_response> DeleteOfflineCaseAsync(string id, string rev, DBConfigurationDetail dbConfig);
    Task<LightweightOfflineCaseListResponse> GetAllLightweightOfflineCasesAsync(DBConfigurationDetail dbConfig);
}
