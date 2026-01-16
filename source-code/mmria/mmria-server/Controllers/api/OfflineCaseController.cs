#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Security.Claims;
using mmria.server.extension;
using mmria.server.SharedLibraries.Manager;
using mmria.server.SharedLibraries.Model.OfflineCase;
using mmria.common.model.couchdb;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class OfflineCaseController: ControllerBase
{ 
    private readonly IOfflineCaseManager _manager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string host_prefix = null;

    public OfflineCaseController
    (
        IHttpContextAccessor httpContextAccessor,
        IOfflineCaseManager manager
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _manager = manager;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
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
    public async Task<document_put_response> Post([FromBody] OfflineCaseRequest request)
    {
        try
        {
            string userName = GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return new document_put_response { ok = false, error_description = "Unable to determine user" };
            }
            return await _manager.CreateOfflineCaseAsync(request, userName, host_prefix);
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
            var result = await _manager.GetUserOfflineCasesAsync(userId, host_prefix);
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

            var result = await _manager.GetOfflineCaseAsync(id, host_prefix);
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

            var sessionStatus = await _manager.GetActiveUserSessionAsync(current_user, host_prefix);
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
            var result = await _manager.GetAllActiveSessionsAsync(host_prefix);
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

            var result = await _manager.GetLightweightStatusOnlyAsync(current_user, host_prefix);
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
            return await _manager.DeleteOfflineCaseAsync(documentId, host_prefix);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new document_put_response { ok = false, error_description = ex.Message };
        }
    }

    [Authorize(Roles = "offline_mode")]
    [HttpPost("update-cases/{id}")]
    public async Task<IActionResult> SaveOfflineCases(string id, [FromBody] SaveOfflineCasesRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(id) || request == null || request.CaseDocuments == null)
            {
                return BadRequest(new { error = "Invalid request" });
            }

            string userName = GetUserName();
            request.OfflineSessionId = id;
            var result = await _manager.UpdateCasesAsync(request, userName, host_prefix);
            if (result.ok)
            {
                return Ok(new {
                    message = "Case documents saved successfully",
                    offlineCaseId = id,
                    documentCount = request.CaseDocuments.Count,
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
            var result = await _manager.SyncOfflineChangesAsync(id, userName, User, host_prefix);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error during sync", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates the sync status of a specific document change within an offline session.
    /// This allows tracking which documents have been synced, abandoned, or errored.
    /// </summary>
    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("update-sync-status")]
    public async Task<IActionResult> UpdateDocumentSyncStatus([FromBody] DocumentChangeSyncStatusRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OfflineSessionId) || string.IsNullOrWhiteSpace(request._id))
            {
                return BadRequest(new { error = "Invalid request" });
            }

            var result = await _manager.UpdateSyncStatusAsync(request, host_prefix);
            if (result.ok)
            {
                string statusDescription = request.SyncState switch
                {
                    0 => "not synced",
                    1 => "synced",
                    2 => "abandoned",
                    3 => "error",
                    _ => "unknown"
                };
                return Ok(new {
                    message = "Document sync status updated successfully",
                    offlineSessionId = request.OfflineSessionId,
                    documentId = request._id,
                    syncState = request.SyncState,
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
    public async Task<IActionResult> UpdateOfflineState([FromBody] UpdateOfflineStateRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OfflineSessionId))
            {
                return BadRequest(new { error = "Invalid request" });
            }

            var result = await _manager.UpdateOfflineStateAsync(request, host_prefix);
            if (result.ok)
            {
                string stateDescription = request.OfflineState switch
                {
                    0 => "initial/not started",
                    1 => "in progress",
                    2 => "completed",
                    3 => "error/failed",
                    _ => "unknown"
                };
                return Ok(new {
                    message = "Offline state updated successfully",
                    offlineSessionId = request.OfflineSessionId,
                    offlineState = request.OfflineState,
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

            var sessionId = await _manager.CreateOfflineAuthTokenAsync(userName, host_prefix);
            Response.Cookies.Append("sid", sessionId, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.AddMinutes(24 * 7 * 60), SameSite = SameSiteMode.Strict });

            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error creating offline token", details = ex.Message });
        }
    }

}
#endif