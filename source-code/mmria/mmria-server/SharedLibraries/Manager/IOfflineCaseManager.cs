using System.Security.Claims;
using System.Threading.Tasks;
using mmria.common.model.couchdb;
using mmria.server.SharedLibraries.Model.OfflineCase;

namespace mmria.server.SharedLibraries.Manager;

public interface IOfflineCaseManager
{
    Task<CacheVersionResponse> GetCacheVersionAsync();
    Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, string jurisdictionId);
    Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, string jurisdictionId);
    Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, string jurisdictionId);
    Task<OfflineCaseResponse> GetActiveUserSessionAsync(string userId, string jurisdictionId);
    Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(string jurisdictionId);
    Task<LightweightOfflineCaseResponse> GetLightweightStatusOnlyAsync(string userId, string jurisdictionId);
    Task<document_put_response> DeleteOfflineCaseAsync(string id, string jurisdictionId);
    Task<document_put_response> UpdateCasesAsync(SaveOfflineCasesRequest request, string userName, string jurisdictionId);
    Task<document_put_response> UpdateSyncStatusAsync(DocumentChangeSyncStatusRequest request, string jurisdictionId);
    Task<document_put_response> UpdateOfflineStateAsync(UpdateOfflineStateRequest request, string jurisdictionId);
    Task<string> CreateOfflineAuthTokenAsync(string userName, string jurisdictionId);
    Task<object> SyncOfflineChangesAsync(string id, string userName, ClaimsPrincipal user, string jurisdictionId);
}