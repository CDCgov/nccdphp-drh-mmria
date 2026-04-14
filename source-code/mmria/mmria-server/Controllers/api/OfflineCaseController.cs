#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Security.Claims;
using Akka.Actor;
using mmria.server.extension;
using mmria.common.SharedLibraries.OfflineCase.Manager;
using mmria.common.SharedLibraries.OfflineCase.Model;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.utils;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class OfflineCaseController: ControllerBase
{ 
    private readonly IOfflineCaseManager _manager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OverridableConfiguration _configuration;
    private readonly ActorSystem _actorSystem;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly DBConfigurationDetail db_config;
    private string host_prefix = null;

    public OfflineCaseController
    (
        IHttpContextAccessor httpContextAccessor,
        IOfflineCaseManager manager,
        ActorSystem actorSystem,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _manager = manager;
        _actorSystem = actorSystem;
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        _configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
    }

    private string GetUserName()
    {
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            return User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }
        return null;
    }


    /// <summary>
    /// Gets the current API cache version for offline mode.
    /// This endpoint provides the single source of truth for cache versioning,
    /// preventing hardcoded version strings from becoming out of sync.
    /// </summary>
    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("cache-version")]
    public async Task<IActionResult> GetCacheVersion()
    {
        try
        {
            var response = await _manager.GetCacheVersionAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Failed to get cache version", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost]
    public async Task<document_put_response> Post()
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<OfflineCaseRequest>(Request);
            string userName = GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return new document_put_response { ok = false, error_description = "Unable to determine user" };
            }

            var sanitizedRequest = CreateSanitizedOfflineCaseRequest(request);
            return await _manager.CreateOfflineCaseAsync(sanitizedRequest, userName, db_config);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new document_put_response { ok = false, error_description = ex.Message };
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        try
        {
            var result = await _manager.GetUserOfflineCasesAsync(userId, db_config);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500);
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("by-session/{id}")]
    public async Task<IActionResult> GetOfflineCaseDocument(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "Document ID is required" });
            }

            var result = await _manager.GetOfflineCaseAsync(id, db_config);
            if (result == null)
            {
                return NotFound(new { error = "Offline case document not found", documentId = id });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("active-user-session")]
    public async Task<IActionResult> GetActiveSession()
    {
        try
        {
            var current_user = GetUserName();
            if (string.IsNullOrEmpty(current_user))
            {
                return BadRequest(new { error = "User not found" });
            }

            var sessionStatus = await _manager.GetActiveUserSessionAsync(current_user, db_config);
            if (!sessionStatus.HasActiveSession)
            {
                return Ok(new { error = "no active sessions" });
            }
            // Return the full OfflineCaseResponse to maintain API compatibility
            return Ok(sessionStatus.SessionData);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("all-active-sessions")]
    public async Task<IActionResult> GetAllActiveSessions()
    {
        try
        {
            var result = await _manager.GetAllActiveSessionsAsync(db_config);
            if (result.rows.Count == 0)
            {
                return Ok(new { error = "no active sessions" });
            }
            return Ok(result.rows.Select(r => r.value).ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("lightweight-status-only")]
    public async Task<IActionResult> GetActiveSessionLight()
    {
        try
        {
            var current_user = GetUserName();
            if (string.IsNullOrEmpty(current_user))
            {
                return BadRequest(new { error = "User not found" });
            }

            var result = await _manager.GetLightweightStatusOnlyAsync(current_user, db_config);
            if (result == null)
            {
                return Ok(new { error = "no active sessions" });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }


    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpDelete("{documentId}")]
    public async Task<document_put_response> Delete(string documentId)
    {
        try
        {
            return await _manager.DeleteOfflineCaseAsync(documentId, db_config);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new document_put_response { ok = false, error_description = ex.Message };
        }
    }

    [Authorize(Roles = "offline_mode")]
    [HttpPost("update-cases/{id}")]
    public async Task<IActionResult> SaveOfflineCases(string id)
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<SaveOfflineCasesRequest>(Request);
            if (string.IsNullOrEmpty(id) || request == null || request.CaseDocuments == null)
            {
                return BadRequest(new { error = "Invalid request" });
            }

            string userName = GetUserName();
            var sanitizedRequest = CreateSanitizedSaveOfflineCasesRequest(request, id, userName);
            var result = await _manager.UpdateCasesAsync(sanitizedRequest, userName, db_config);
            if (result.ok)
            {
                return Ok(new {
                    message = "Case documents saved successfully",
                    offlineCaseId = id,
                    documentCount = sanitizedRequest.CaseDocuments.Count,
                    revision = result.rev,
                    offline_state = 1,
                    shouldSetProcessOffline = true
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to save case documents", details = result.error_description });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("sync-changes/{id}")]
    public async Task<IActionResult> SyncOfflineChanges(string id)
    {
        try
        {
            string userName = GetUserName();
            var result = await _manager.SyncOfflineChangesAsync(id, userName, User, db_config);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error during sync", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("sync-case")]
    public async Task<document_put_response> SyncOfflineCase()
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<SyncOfflineCaseRequest>(Request);
            string userName = GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return new document_put_response { ok = false, error_description = "Unable to determine user" };
            }

            var sanitizedRequest = CreateSanitizedSyncOfflineCaseRequest(request);
            var saveResult = await _manager.SyncOfflineCaseAsync(sanitizedRequest, userName, User, db_config, _configuration, host_prefix);

            if (saveResult.Response.ok && !string.IsNullOrWhiteSpace(saveResult.CaseId))
            {
                var syncDocumentMessage = new mmria.server.model.actor.Sync_Document_Message(
                    saveResult.CaseId,
                    saveResult.SerializedCase,
                    "PUT",
                    _configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, _configuration, host_prefix)).Tell(syncDocumentMessage);
            }

            return saveResult.Response;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new document_put_response { ok = false, error_description = ex.Message };
        }
    }

    /// <summary>
    /// Updates the sync status of a specific document change within an offline session.
    /// This allows tracking which documents have been synced, abandoned, or errored.
    /// </summary>
    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("update-sync-status")]
    public async Task<IActionResult> UpdateDocumentSyncStatus()
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<DocumentChangeSyncStatusRequest>(Request);
            var sanitizedRequest = CreateSanitizedDocumentChangeSyncStatusRequest(request);
            if (request == null || string.IsNullOrWhiteSpace(sanitizedRequest.OfflineSessionId) || string.IsNullOrWhiteSpace(sanitizedRequest._id))
            {
                return BadRequest(new { error = "Invalid request" });
            }

            var result = await _manager.UpdateSyncStatusAsync(sanitizedRequest, db_config);
            if (result.ok)
            {
                string statusDescription = sanitizedRequest.SyncState switch
                {
                    0 => "not synced",
                    1 => "synced",
                    2 => "abandoned",
                    3 => "error",
                    _ => "unknown"
                };
                return Ok(new {
                    message = "Document sync status updated successfully",
                    offlineSessionId = sanitizedRequest.OfflineSessionId,
                    documentId = sanitizedRequest._id,
                    syncState = sanitizedRequest.SyncState,
                    syncStatusDescription = statusDescription,
                    revision = result.rev
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update document sync status", details = result.error_description });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Lightweight connectivity check endpoint for determining online/offline status.
    /// This endpoint requires no database calls and returns immediately.
    /// </summary>
    [HttpGet("connectivity-check")]
    [AllowAnonymous] // Allow anonymous access since this is just a connectivity check
    public IActionResult ConnectivityCheck()
    {
        try
        {
            return Ok(new
            {
                status = "online",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                message = "Server is reachable"
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                status = "online",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                message = "Server is reachable despite error"
            });
        }
    }

    /// <summary>
    /// Updates the offline state for a specific offline session.
    /// This allows tracking the progress of offline operations.
    /// </summary>
    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("update-offline-state")]
    public async Task<IActionResult> UpdateOfflineState()
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<UpdateOfflineStateRequest>(Request);
            var sanitizedRequest = CreateSanitizedUpdateOfflineStateRequest(request);
            if (request == null || string.IsNullOrWhiteSpace(sanitizedRequest.OfflineSessionId))
            {
                return BadRequest(new { error = "Invalid request" });
            }

            var result = await _manager.UpdateOfflineStateAsync(sanitizedRequest, db_config);
            if (result.ok)
            {
                string stateDescription = sanitizedRequest.OfflineState switch
                {
                    0 => "initial/not started",
                    1 => "in progress",
                    2 => "completed",
                    3 => "error/failed",
                    _ => "unknown"
                };
                return Ok(new {
                    message = "Offline state updated successfully",
                    offlineSessionId = sanitizedRequest.OfflineSessionId,
                    offlineState = sanitizedRequest.OfflineState,
                    stateDescription = stateDescription,
                    revision = result.rev,
                    updatedBy = GetUserName(),
                    updatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update offline state", details = result.error_description });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("release-case-locks")]
    public async Task<IActionResult> ReleaseCaseLocks()
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<ReleaseOfflineCaseLocksRequest>(Request);
            string userName = GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new { error = "Unable to determine user" });
            }

            var sanitizedRequest = CreateSanitizedReleaseOfflineCaseLocksRequest(request);
            var result = await _manager.ReleaseOfflineCaseLocksAsync(sanitizedRequest, userName, db_config);
            if (result.ok)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("recover-softlocks")]
    public async Task<IActionResult> RecoverSoftLocks()
    {
        try
        {
            var request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<RecoverSoftLocksRequest>(Request);
            string userName = GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new { error = "Unable to determine user" });
            }

            var sanitizedRequest = CreateSanitizedRecoverSoftLocksRequest(request);
            var result = await _manager.RecoverSoftLocksAsync(sanitizedRequest, userName, db_config);
            if (result.ok)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }


    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("create-offline-auth-token")]
    public async Task<IActionResult> CreateOfflineAuthToken()
    {
        try
        {
            string userName = GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new { error = "Unable to determine current user" });
            }

            var sessionId = await _manager.CreateOfflineAuthTokenAsync(userName, db_config);
            mmria.server.util.AppSessionCookieHelper.AppendSessionIdCookie(
                Response,
                sessionId,
                DateTime.Now.AddMinutes(24 * 30 * 60),
                Request.IsHttps);

            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error creating offline token", details = ex.Message });
        }
    }

    private static OfflineCaseRequest CreateSanitizedOfflineCaseRequest(OfflineCaseRequest request)
    {
        return new OfflineCaseRequest
        {
            offline_ids = SanitizeIdentifierList(request?.offline_ids),
            offline_key = SanitizeSingleLineText(request?.offline_key, 1024),
            device_id = SanitizeSingleLineText(request?.device_id, 256),
            browser_id = SanitizeSingleLineText(request?.browser_id, 256),
            tab_id = SanitizeSingleLineText(request?.tab_id, 256)
        };
    }

    private static SaveOfflineCasesRequest CreateSanitizedSaveOfflineCasesRequest(
        SaveOfflineCasesRequest request,
        string offlineSessionId,
        string userName)
    {
        return new SaveOfflineCasesRequest
        {
            OfflineSessionId = SanitizeSingleLineText(offlineSessionId, 256),
            CaseDocuments = request?.CaseDocuments?
                .Where(change => change != null)
                .Select(change => CreateSanitizedDocumentChange(change, offlineSessionId, userName))
                .ToList() ?? new List<DocumentChange>()
        };
    }

    private static SyncOfflineCaseRequest CreateSanitizedSyncOfflineCaseRequest(SyncOfflineCaseRequest request)
    {
        return new SyncOfflineCaseRequest
        {
            OfflineSessionId = SanitizeSingleLineText(request?.OfflineSessionId, 256),
            CaseId = SanitizeSingleLineText(request?.CaseId, 256)
        };
    }

    private static DocumentChangeSyncStatusRequest CreateSanitizedDocumentChangeSyncStatusRequest(DocumentChangeSyncStatusRequest request)
    {
        return new DocumentChangeSyncStatusRequest
        {
            OfflineSessionId = SanitizeSingleLineText(request?.OfflineSessionId, 256),
            _id = SanitizeSingleLineText(request?._id, 256),
            SyncState = NormalizeSyncState(request?.SyncState ?? 0)
        };
    }

    private static UpdateOfflineStateRequest CreateSanitizedUpdateOfflineStateRequest(UpdateOfflineStateRequest request)
    {
        return new UpdateOfflineStateRequest
        {
            OfflineSessionId = SanitizeSingleLineText(request?.OfflineSessionId, 256),
            OfflineState = NormalizeOfflineState(request?.OfflineState ?? 0)
        };
    }

    private static ReleaseOfflineCaseLocksRequest CreateSanitizedReleaseOfflineCaseLocksRequest(ReleaseOfflineCaseLocksRequest request)
    {
        return new ReleaseOfflineCaseLocksRequest
        {
            OfflineSessionId = SanitizeSingleLineText(request?.OfflineSessionId, 256),
            CaseIds = SanitizeIdentifierList(request?.CaseIds)
        };
    }

    private static RecoverSoftLocksRequest CreateSanitizedRecoverSoftLocksRequest(RecoverSoftLocksRequest request)
    {
        return new RecoverSoftLocksRequest
        {
            OfflineSessionId = SanitizeSingleLineText(request?.OfflineSessionId, 256),
            tab_id = SanitizeSingleLineText(request?.tab_id, 256),
            CaseIds = SanitizeIdentifierList(request?.CaseIds)
        };
    }

    private static DocumentChange CreateSanitizedDocumentChange(DocumentChange request, string offlineSessionId, string userName)
    {
        return new DocumentChange
        {
            DocumentId = SanitizeSingleLineText(request?.DocumentId, 256),
            OriginalDocument = request?.OriginalDocument,
            ModifiedDocument = request?.ModifiedDocument,
            Timestamp = SanitizeSingleLineText(request?.Timestamp, 128),
            ChangeDescription = SanitizeMultilineText(request?.ChangeDescription, 2048),
            SyncState = NormalizeSyncState(request?.SyncState ?? 0),
            UserId = SanitizeSingleLineText(userName, 256),
            SessionId = SanitizeSingleLineText(offlineSessionId, 256),
            ChangeStackItems = request?.ChangeStackItems?
                .Where(item => item != null)
                .Select(item => CreateSanitizedChangeStackItem(item, userName))
                .ToList() ?? new List<mmria.common.model.couchdb.Change_Stack_Item>()
        };
    }

    private static mmria.common.model.couchdb.Change_Stack_Item CreateSanitizedChangeStackItem(
        mmria.common.model.couchdb.Change_Stack_Item request,
        string userName)
    {
        return new mmria.common.model.couchdb.Change_Stack_Item
        {
            _id = SanitizeSingleLineText(request?._id, 256),
            _rev = CouchDbRevisionHelper.NormalizeIncomingRevision(request?._rev),
            user_name = SanitizeSingleLineText(userName, 256),
            temp_index = request?.temp_index,
            date_created = request?.date_created,
            object_path = SanitizeSingleLineText(request?.object_path, 512),
            metadata_path = SanitizeSingleLineText(request?.metadata_path, 512),
            old_value = request?.old_value,
            new_value = request?.new_value,
            dictionary_path = SanitizeSingleLineText(request?.dictionary_path, 512),
            form_index = request?.form_index,
            grid_index = request?.grid_index,
            prompt = SanitizeMultilineText(request?.prompt, 512),
            metadata_type = SanitizeSingleLineText(request?.metadata_type, 128),
            doc_type = "Change_Stack_Item"
        };
    }

    private static List<string> SanitizeIdentifierList(IEnumerable<string> values)
    {
        if (values == null)
        {
            return new List<string>();
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var sanitized = SanitizeSingleLineText(value, 256);
            if (string.IsNullOrWhiteSpace(sanitized) || !seen.Add(sanitized))
            {
                continue;
            }

            result.Add(sanitized);
        }

        return result;
    }

    private static int NormalizeSyncState(int value) =>
        value < 0 ? 0 : value > 6 ? 6 : value;

    private static int NormalizeOfflineState(int value) =>
        value < 0 ? 0 : value > 3 ? 3 : value;

    private static string SanitizeSingleLineText(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }

    private static string SanitizeMultilineText(string value, int maxLength = 2048)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => character == '\r' || character == '\n' || !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }

}
#endif
