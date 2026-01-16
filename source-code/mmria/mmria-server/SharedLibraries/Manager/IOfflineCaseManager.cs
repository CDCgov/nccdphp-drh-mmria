using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.model.couchdb;
using mmria.server.SharedLibraries.Model.OfflineCase;

namespace mmria.server.SharedLibraries.Manager;

public interface IOfflineCaseManager
{
    Task<CacheVersionResponse> GetCacheVersionAsync();
    Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, string jurisdictionId, CancellationToken cancellationToken);
    Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, string jurisdictionId, CancellationToken cancellationToken);
    Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, string jurisdictionId, CancellationToken cancellationToken);
    Task<OfflineCaseResponse> GetActiveUserSessionAsync(string userId, string jurisdictionId, CancellationToken cancellationToken);
    Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(string jurisdictionId, CancellationToken cancellationToken);
    Task<LightweightOfflineCaseResponse> GetLightweightStatusOnlyAsync(string userId, string jurisdictionId, CancellationToken cancellationToken);
    Task<document_put_response> DeleteOfflineCaseAsync(string id, string jurisdictionId, CancellationToken cancellationToken);
    Task<document_put_response> UpdateCasesAsync(SaveOfflineCasesRequest request, string userName, string jurisdictionId, CancellationToken cancellationToken);
    Task<document_put_response> UpdateSyncStatusAsync(DocumentChangeSyncStatusRequest request, string jurisdictionId, CancellationToken cancellationToken);
    Task<document_put_response> UpdateOfflineStateAsync(UpdateOfflineStateRequest request, string jurisdictionId, CancellationToken cancellationToken);
    Task<string> CreateOfflineAuthTokenAsync(string userName, string jurisdictionId, CancellationToken cancellationToken);
    Task<object> SyncOfflineChangesAsync(string id, string userName, ClaimsPrincipal user, string jurisdictionId, CancellationToken cancellationToken);
}