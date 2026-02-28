using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using mmria.case_version.v260120;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session.Model;
using mmria.common.SharedLibraries.Session.Manager;
using mmria.server;
using mmria.server.SharedLibraries.DAL;
using mmria.server.SharedLibraries.Model.OfflineCase;
using mmria.server.extension;
using Newtonsoft.Json;

namespace mmria.server.SharedLibraries.Manager;

public class OfflineCaseManager : IOfflineCaseManager
{
    private readonly OfflineCaseDAL _offlineCaseDal;
    private readonly CaseDAL _caseDal;
    private readonly SessionDAL _sessionDal;
    private readonly SessionManager _sessionManager;
    private readonly OverridableConfiguration _configuration;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public OfflineCaseManager(
        OfflineCaseDAL offlineCaseDal,
        CaseDAL caseDal,
        SessionDAL sessionDal,
        SessionManager sessionManager,
        OverridableConfiguration configuration,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _offlineCaseDal = offlineCaseDal;
        _caseDal = caseDal;
        _sessionDal = sessionDal;
        _sessionManager = sessionManager;
        _configuration = configuration;
        _couchDbHttpClient = couchDbHttpClient;
    }

    public Task<CacheVersionResponse> GetCacheVersionAsync()
    {
        const string VERSION = "v114";
        const string STABILITY = "stable";
        var cacheVersion = $"mmria-api-{VERSION}-{STABILITY}";
        var baseVersion = $"{VERSION}-{STABILITY}";

        var response = new CacheVersionResponse
        {
            cacheVersion = cacheVersion,
            baseVersion = baseVersion,
            version = VERSION,
            stability = STABILITY,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        return Task.FromResult(response);
    }

    public async Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, DBConfigurationDetail dbConfig)
    {
        var result = await _offlineCaseDal.CreateOfflineCaseAsync(request, userName, dbConfig);
        
        // Upgrade all cases in offline_ids from soft lock (type 1) to hard lock (type 2)
        if (result.ok && request.offline_ids != null && request.offline_ids.Count > 0)
        {
            await UpgradeCaseToHardLockAsync(request.offline_ids, dbConfig);
        }
        
        return result;
    }
    
    private async Task UpgradeCaseToHardLockAsync(List<string> caseIds, DBConfigurationDetail dbConfig)
    {
        foreach (var caseId in caseIds)
        {
            try
            {
                // Fetch the case document
                var caseUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";
                var caseResponse = await _couchDbHttpClient.ExecuteAsync("GET", caseUrl, null, dbConfig.user_name, dbConfig.user_value);
                
                if (string.IsNullOrEmpty(caseResponse)) continue;
                
                var caseDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(caseResponse);
                if (caseDocument == null) continue;
                
                // Only upgrade if currently soft lock (type 1)
                if (caseDocument.ContainsKey("offline_lock_type") && caseDocument["offline_lock_type"]?.ToString() == "1")
                {
                    caseDocument["offline_lock_type"] = 2; // Upgrade to hard lock
                    caseDocument["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    
                    var json_string = Newtonsoft.Json.JsonConvert.SerializeObject(caseDocument);
                    await _couchDbHttpClient.ExecuteAsync("PUT", caseUrl, json_string, dbConfig.user_name, dbConfig.user_value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upgrading case {caseId} to hard lock: {ex.Message}");
                // Continue with other cases even if one fails
            }
        }
    }

    public async Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, DBConfigurationDetail dbConfig)
    {
        return await _offlineCaseDal.GetOfflineCaseAsync(id, dbConfig);
    }

    public async Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, DBConfigurationDetail dbConfig)
    {
        return await _offlineCaseDal.GetUserOfflineCasesAsync(userId, dbConfig);
    }

    public async Task<OfflineSessionStatus> GetActiveUserSessionAsync(string userId, DBConfigurationDetail dbConfig)
    {
        var cases = await _offlineCaseDal.GetUserOfflineCasesAsync(userId, dbConfig);
        
        // Filter for active states (0 or 1)
        var activeSessions = cases.rows.Where(r => 
            r?.value != null && r.key == userId &&
            (r.value.offline_state == 0 || r.value.offline_state == 1)
        ).ToList();

        if (activeSessions.Count == 0)
        {
            return new OfflineSessionStatus
            {
                HasActiveSession = false,
                OfflineState = null,
                SessionData = null
            };
        }

        // Return the first active session found (ordered by most recent)
        var firstSession = activeSessions.OrderByDescending(r => r.value.date_created).First().value;
        return new OfflineSessionStatus
        {
            HasActiveSession = true,
            OfflineState = firstSession.offline_state,
            SessionData = firstSession
        };
    }

    public async Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(DBConfigurationDetail dbConfig)
    {
        return await _offlineCaseDal.GetAllActiveSessionsAsync(dbConfig);
    }

    public async Task<LightweightOfflineCaseResponse> GetLightweightStatusOnlyAsync(string userId, DBConfigurationDetail dbConfig)
    {
        var sessionStatus = await GetActiveUserSessionAsync(userId, dbConfig);
        if (!sessionStatus.HasActiveSession || sessionStatus.SessionData == null) return null;

        var session = sessionStatus.SessionData;
        return new LightweightOfflineCaseResponse
        {
            _id = session._id,
            _rev = session._rev,
            offline_ids = session.offline_ids,
            offline_key = session.offline_key,
            offline_state = session.offline_state,
            case_documents = session.case_documents.Select(d => new LightweightDocumentChange
            {
                DocumentId = d.DocumentId,
                Timestamp = d.Timestamp,
                ChangeDescription = d.ChangeDescription,
                SyncState = d.SyncState,
                UserId = d.UserId,
                SessionId = d.SessionId
            }).ToList(),
            created_by = session.created_by,
            date_created = session.date_created,
            last_updated_by = session.last_updated_by,
            date_last_updated = session.date_last_updated
        };
    }

    public async Task<document_put_response> DeleteOfflineCaseAsync(string id, DBConfigurationDetail dbConfig)
    {
        var doc = await _offlineCaseDal.GetOfflineCaseAsync(id, dbConfig);
        return await _offlineCaseDal.DeleteOfflineCaseAsync(id, doc._rev, dbConfig);
    }

    public async Task<document_put_response> UpdateCasesAsync(SaveOfflineCasesRequest request, string userName, DBConfigurationDetail dbConfig)
    {
        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, dbConfig);
        if (session.offline_state != 0)
        {
            throw new InvalidOperationException("Session is not in initial state");
        }

        session.case_documents = request.CaseDocuments;
        session.offline_state = 1; // in progress
        session.last_updated_by = userName;
        session.date_last_updated = DateTime.UtcNow;

        return await _offlineCaseDal.UpdateOfflineCaseAsync(request.OfflineSessionId, session, dbConfig);
    }

    public async Task<document_put_response> UpdateSyncStatusAsync(DocumentChangeSyncStatusRequest request, DBConfigurationDetail dbConfig)
    {
        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, dbConfig);
        var docChange = session.case_documents.FirstOrDefault(d => d.DocumentId == request._id);
        if (docChange != null)
        {
            docChange.SyncState = request.SyncState;
        }
        return await _offlineCaseDal.UpdateOfflineCaseAsync(request.OfflineSessionId, session, dbConfig);
    }

    public async Task<document_put_response> UpdateOfflineStateAsync(UpdateOfflineStateRequest request, DBConfigurationDetail dbConfig)
    {
        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, dbConfig);
        session.offline_state = request.OfflineState;
        return await _offlineCaseDal.UpdateOfflineCaseAsync(request.OfflineSessionId, session, dbConfig);
    }

    public async Task<string> CreateOfflineAuthTokenAsync(string userName, DBConfigurationDetail dbConfig)
    {
        int expireMinutes = 24 * 7 * 60; // 7 days

        // Create role list with ONLY offline_mode
        var roleList = new List<string> { "offline_mode" };

        // Create session event message
        var sessionEventMessage = new Session_Event_Message(
            DateTime.Now,
            userName,
            "1.1.1.1", // IP placeholder for offline mode
            Session_Event_Message.Session_Event_Message_Action_Enum.successful_login
        );

        // Record the session event via actor
         _sessionManager.RecordSessionEvent(sessionEventMessage, dbConfig);

        // Create new session with offline_mode role
        var sessionId = Guid.NewGuid().ToString();
        var sessionData = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var sessionExpirationDateTime = DateTime.Now.AddMinutes(expireMinutes);

        var sessionMessage = new Session_Message(
            sessionId,
            null, // _rev
            DateTime.Now, // date_created
            DateTime.Now, // date_last_updated
            sessionExpirationDateTime, // date_expired
            true, // is_active
            userName,
            "1.1.1.1", // IP placeholder
            sessionEventMessage._id, // session_event_id
            roleList,
            sessionData
        );

        // Save session to database
        await _sessionDal.CreateSessionAsync(sessionMessage, dbConfig);

        // Post session to actor system
        _sessionManager.PostSessionAsync(sessionMessage, dbConfig);

        return sessionId;
    }

    public async Task<object> SyncOfflineChangesAsync(string id, string userName, ClaimsPrincipal user, DBConfigurationDetail dbConfig)
    {
        var offlineCase = await _offlineCaseDal.GetOfflineCaseAsync(id, dbConfig);
        if (offlineCase == null || offlineCase.case_documents == null)
        {
            throw new ArgumentException("Offline case not found or no case documents");
        }

        var enhancedChanges = new List<object>();
        var validationErrors = new List<string>();

        foreach (var docChange in offlineCase.case_documents)
        {
            try
            {
                var caseId = docChange.DocumentId;
                if (string.IsNullOrWhiteSpace(caseId)) continue;

                var currentDoc = await _caseDal.GetCaseAsync(caseId, dbConfig);
                if (currentDoc == null)
                {
                    validationErrors.Add($"Could not retrieve current document for case ID: {caseId}");
                    continue;
                }

                // Merge changes: use ModifiedDocument as the updated case
                var updatedDoc = docChange.ModifiedDocument;
                if (updatedDoc == null)
                {
                    validationErrors.Add($"No modified document for case ID: {caseId}");
                    continue;
                }

                // Update audit fields
                updatedDoc.last_updated_by = userName;
                updatedDoc.date_last_updated = DateTime.UtcNow;

                // Validate jurisdiction
                var caseJurisdiction = currentDoc.home_record?.jurisdiction_id;
                if (!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase, caseJurisdiction))
                {
                    validationErrors.Add($"Unauthorized to save case {caseId} in jurisdiction {caseJurisdiction}");
                    continue;
                }

                var saveResult = await _caseDal.UpdateCaseAsync(caseId, updatedDoc, dbConfig);
                if (saveResult.ok)
                {
                    enhancedChanges.Add(new { 
                        caseId = caseId, 
                        status = "saved",
                        revision = saveResult.rev
                    });
                }
                else
                {
                    validationErrors.Add($"Failed to save case {caseId}: {saveResult.error_description}");
                }
            }
            catch (Exception docEx)
            {
                validationErrors.Add($"Error processing document {docChange.DocumentId}: {docEx.Message}");
            }
        }

        if (validationErrors.Any())
        {
            return new { 
                error = "Some documents failed to sync", 
                validationErrors = validationErrors,
                successfulSaves = enhancedChanges.Count,
                failedSaves = validationErrors.Count
            };
        }
        else
        {
            return new { 
                message = "All offline changes synced successfully to mmrds database", 
                syncedDocuments = enhancedChanges,
                totalSynced = enhancedChanges.Count
            };
        }
    }
    public async Task<bool> ShouldRedirectToCaseSummaryAsync(string userName, DBConfigurationDetail dbConfig)
    {
        var sessionStatus = await GetActiveUserSessionAsync(userName, dbConfig);
        return sessionStatus.HasActiveSession;
    }}