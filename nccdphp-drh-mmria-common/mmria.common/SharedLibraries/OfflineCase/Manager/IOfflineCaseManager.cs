using System.Security.Claims;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.OfflineCase.Model;

namespace mmria.common.SharedLibraries.OfflineCase.Manager;

public interface IOfflineCaseManager
{
    Task<CacheVersionResponse> GetCacheVersionAsync();
    Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, DBConfigurationDetail dbConfig);
    Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, DBConfigurationDetail dbConfig);
    Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig);
    Task<OfflineSessionStatus> GetActiveUserSessionAsync(string userId, DBConfigurationDetail dbConfig);
    Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(DBConfigurationDetail dbConfig);
    Task<LightweightOfflineCaseResponse> GetLightweightStatusOnlyAsync(string userId, DBConfigurationDetail dbConfig);
    Task<document_put_response> DeleteOfflineCaseAsync(string id, DBConfigurationDetail dbConfig);
    Task<document_put_response> UpdateCasesAsync(SaveOfflineCasesRequest request, string userName, DBConfigurationDetail dbConfig);
    Task<document_put_response> UpdateSyncStatusAsync(DocumentChangeSyncStatusRequest request, DBConfigurationDetail dbConfig);
    Task<document_put_response> UpdateOfflineStateAsync(UpdateOfflineStateRequest request, DBConfigurationDetail dbConfig);
    Task<string> CreateOfflineAuthTokenAsync(string userName, DBConfigurationDetail dbConfig);
    Task<object> SyncOfflineChangesAsync(string id, string userName, ClaimsPrincipal user, DBConfigurationDetail dbConfig);
    Task<bool> ShouldRedirectToCaseSummaryAsync(string userName, DBConfigurationDetail dbConfig);
}
