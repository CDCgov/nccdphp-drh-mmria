using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mmria.case_version.v260120;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session.Model;
using mmria.common.SharedLibraries.Session.Manager;
using mmria.common.SharedLibraries.OfflineCase.DAL;
using mmria.common.SharedLibraries.Case.DAL;
using mmria.common.SharedLibraries.Session.DAL;
using mmria.common.SharedLibraries.OfflineCase.Model;
using mmria.common.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.OfflineCase.Manager;

public class OfflineCaseManager : IOfflineCaseManager
{
    private readonly OfflineCaseDAL _offlineCaseDal;
    private readonly CaseDAL _caseDal;
    private readonly SessionDAL _sessionDal;
    private readonly SessionManager _sessionManager;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public OfflineCaseManager(
        OfflineCaseDAL offlineCaseDal,
        CaseDAL caseDal,
        SessionDAL sessionDal,
        SessionManager sessionManager,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _offlineCaseDal = offlineCaseDal;
        _caseDal = caseDal;
        _sessionDal = sessionDal;
        _sessionManager = sessionManager;
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
        if (request == null)
        {
            return new document_put_response { ok = false, error_description = "Offline session request is required." };
        }

        if (string.IsNullOrWhiteSpace(request.tab_id))
        {
            return new document_put_response { ok = false, error_description = "tab_id is required to enter offline mode." };
        }

        var conflictingSoftLockCaseId = await _caseDal.GetSoftLockedCaseIdForUserInAnotherTabAsync(userName, request.tab_id, dbConfig);
        if (!string.IsNullOrWhiteSpace(conflictingSoftLockCaseId))
        {
            return new document_put_response { ok = false, error_description = "Cannot go into offline mode with cases added in another browser tab. Please try this tab from the original tab." };
        }

        var conflictingSessionId = await _offlineCaseDal.GetActiveSessionIdForUserInAnotherTabAsync(userName, request.tab_id, dbConfig);
        if (!string.IsNullOrWhiteSpace(conflictingSessionId))
        {
            return new document_put_response { ok = false, error_description = "Cannot go into offline mode with cases added in another browser tab. Please try this tab from the original tab." };
        }

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
        var cases = await _offlineCaseDal.TryGetUserOfflineCasesAsync(userId, dbConfig);
        
        // Filter for active states (0 or 1)
        var activeSessions = (cases?.rows ?? Enumerable.Empty<OfflineCaseItem>()).Where(r => 
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

    public async Task<document_put_response> ReleaseOfflineCaseLocksAsync(ReleaseOfflineCaseLocksRequest request, string userName, DBConfigurationDetail dbConfig)
    {
        if (request == null)
        {
            return new document_put_response { ok = false, error_description = "Release offline case locks request is required." };
        }

        if (string.IsNullOrWhiteSpace(request.OfflineSessionId))
        {
            return new document_put_response { ok = false, error_description = "OfflineSessionId is required." };
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return new document_put_response { ok = false, error_description = "Unable to determine user." };
        }

        var caseIds = request.CaseIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (caseIds.Count == 0)
        {
            return new document_put_response { ok = true };
        }

        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, dbConfig);
        if (session == null || string.IsNullOrWhiteSpace(session._id))
        {
            return new document_put_response { ok = false, error_description = "Offline session not found." };
        }

        if (!string.Equals(session.created_by, userName, System.StringComparison.OrdinalIgnoreCase))
        {
            return new document_put_response { ok = false, error_description = "Offline session belongs to another user." };
        }

        if (session.offline_ids == null)
        {
            return new document_put_response { ok = false, error_description = "Offline session does not contain case ids." };
        }

        var sessionCaseIds = new HashSet<string>(session.offline_ids.Where(x => !string.IsNullOrWhiteSpace(x)), System.StringComparer.OrdinalIgnoreCase);
        var missingCaseId = caseIds.FirstOrDefault(caseId => !sessionCaseIds.Contains(caseId));
        if (!string.IsNullOrWhiteSpace(missingCaseId))
        {
            return new document_put_response { ok = false, error_description = $"Case {missingCaseId} is not part of the offline session." };
        }

        document_put_response lastResponse = new document_put_response { ok = true };

        foreach (var caseId in caseIds)
        {
            var caseUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";
            var caseJson = await _couchDbHttpClient.ExecuteAsync("GET", caseUrl, null, dbConfig.user_name, dbConfig.user_value);
            var caseDocument = JObject.Parse(caseJson);

            var isOffline = caseDocument.Value<bool?>("is_offline") == true ||
                string.Equals(caseDocument["is_offline"]?.ToString(), "true", System.StringComparison.OrdinalIgnoreCase);
            if (!isOffline)
            {
                continue;
            }

            var offlineBy = caseDocument.Value<string>("offline_by");
            if (!string.IsNullOrWhiteSpace(offlineBy) &&
                !string.Equals(offlineBy, userName, System.StringComparison.OrdinalIgnoreCase))
            {
                return new document_put_response { ok = false, error_description = $"Case {caseId} is offline locked by {offlineBy}." };
            }

            caseDocument["is_offline"] = false;
            caseDocument.Remove("offline_date");
            caseDocument.Remove("offline_by");
            caseDocument.Remove("offline_lock_type");
            caseDocument.Remove("offline_by_tab_id");
            caseDocument["date_last_updated"] = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            caseDocument["last_updated_by"] = userName;

            var saveResponse = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                caseUrl,
                caseDocument.ToString(Formatting.None),
                dbConfig.user_name,
                dbConfig.user_value);

            lastResponse = JsonConvert.DeserializeObject<document_put_response>(saveResponse) ?? new document_put_response { ok = false, error_description = "Failed to update case." };
            if (!lastResponse.ok)
            {
                return lastResponse;
            }
        }

        return lastResponse;
    }

    public async Task<document_put_response> RecoverSoftLocksAsync(RecoverSoftLocksRequest request, string userName, DBConfigurationDetail dbConfig)
    {
        if (request == null)
        {
            return new document_put_response { ok = false, error_description = "Recover soft locks request is required." };
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return new document_put_response { ok = false, error_description = "Unable to determine user." };
        }

        if (string.IsNullOrWhiteSpace(request.tab_id))
        {
            return new document_put_response { ok = false, error_description = "tab_id is required." };
        }

        var caseIds = request.CaseIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (caseIds.Count == 0)
        {
            return new document_put_response { ok = false, error_description = "At least one case id is required." };
        }

        OfflineCaseResponse session = null;
        if (!string.IsNullOrWhiteSpace(request.OfflineSessionId))
        {
            session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, dbConfig);
            if (session == null || string.IsNullOrWhiteSpace(session._id))
            {
                return new document_put_response { ok = false, error_description = "Offline session not found." };
            }

            if (!string.Equals(session.created_by, userName, StringComparison.OrdinalIgnoreCase))
            {
                return new document_put_response { ok = false, error_description = "Offline session belongs to another user." };
            }

            var sessionCaseIds = new HashSet<string>(
                session.offline_ids?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var missingCaseId = caseIds.FirstOrDefault(caseId => !sessionCaseIds.Contains(caseId));
            if (!string.IsNullOrWhiteSpace(missingCaseId))
            {
                return new document_put_response { ok = false, error_description = $"Case {missingCaseId} is not part of the offline session." };
            }
        }

        document_put_response lastResponse = new document_put_response { ok = true };

        foreach (var caseId in caseIds)
        {
            var caseUrl = $"{dbConfig.url}/{dbConfig.prefix}mmrds/{caseId}";
            var caseJson = await _couchDbHttpClient.ExecuteAsync("GET", caseUrl, null, dbConfig.user_name, dbConfig.user_value);

            if (string.IsNullOrWhiteSpace(caseJson))
            {
                return new document_put_response { ok = false, error_description = $"Unable to load case {caseId}." };
            }

            var caseDocument = JObject.Parse(caseJson);
            if (caseDocument["error"] != null)
            {
                return new document_put_response { ok = false, error_description = $"Unable to load case {caseId}." };
            }

            var offlineBy = caseDocument.Value<string>("offline_by");
            if (!string.IsNullOrWhiteSpace(offlineBy) &&
                !string.Equals(offlineBy, userName, StringComparison.OrdinalIgnoreCase))
            {
                return new document_put_response { ok = false, error_description = $"Case {caseId} is offline locked by {offlineBy}." };
            }

            caseDocument["is_offline"] = true;
            caseDocument["offline_date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            caseDocument["offline_by"] = userName;
            caseDocument["offline_lock_type"] = 1;
            caseDocument["offline_by_tab_id"] = request.tab_id;
            caseDocument["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            caseDocument["last_updated_by"] = userName;

            var saveResponse = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                caseUrl,
                caseDocument.ToString(Formatting.None),
                dbConfig.user_name,
                dbConfig.user_value);

            lastResponse = JsonConvert.DeserializeObject<document_put_response>(saveResponse) ??
                new document_put_response { ok = false, error_description = $"Failed to update case {caseId}." };

            if (!lastResponse.ok)
            {
                return lastResponse;
            }
        }

        if (session != null)
        {
            session.offline_state = 3;
            session.last_updated_by = userName;
            session.date_last_updated = DateTime.UtcNow;

            lastResponse = await _offlineCaseDal.UpdateOfflineCaseAsync(session._id, session, dbConfig);
            if (!lastResponse.ok)
            {
                return lastResponse;
            }
        }

        return lastResponse;
    }

    public async Task<mmria.common.SharedLibraries.Case.Manager.SaveCaseResult> SyncOfflineCaseAsync(SyncOfflineCaseRequest request, string userName, ClaimsPrincipal user, DBConfigurationDetail dbConfig, OverridableConfiguration configuration, string hostPrefix)
    {
        if (request == null)
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = "Sync offline case request is required." }
            };
        }

        if (string.IsNullOrWhiteSpace(request.OfflineSessionId))
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = "OfflineSessionId is required." }
            };
        }

        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = "CaseId is required." }
            };
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = "Unable to determine user." }
            };
        }

        var offlineCase = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, dbConfig);
        if (offlineCase == null || string.IsNullOrWhiteSpace(offlineCase._id))
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = "Offline session not found." }
            };
        }

        if (!string.Equals(offlineCase.created_by, userName, StringComparison.OrdinalIgnoreCase))
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = "Offline session belongs to another user." }
            };
        }

        var docChange = offlineCase.case_documents?.FirstOrDefault(d =>
            string.Equals(d.DocumentId, request.CaseId, StringComparison.OrdinalIgnoreCase));
        if (docChange?.ModifiedDocument == null)
        {
            return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
            {
                Response = new document_put_response { ok = false, error_description = $"No modified document found for case {request.CaseId}." }
            };
        }

        var modifiedDocument = docChange.ModifiedDocument;
        var sessionCaseIds = new HashSet<string>(
            offlineCase.offline_ids?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        JObject currentCaseDocument = null;
        string currentCaseJson = null;
        bool caseExistsInDatabase = false;

        try
        {
            currentCaseJson = await _caseDal.GetCaseDocumentJsonAsync(request.CaseId, dbConfig);
            currentCaseDocument = JObject.Parse(currentCaseJson);
            caseExistsInDatabase = currentCaseDocument["error"] == null;
        }
        catch
        {
            caseExistsInDatabase = false;
            currentCaseDocument = null;
            currentCaseJson = null;
        }

        var isExistingCaseInSession = sessionCaseIds.Contains(request.CaseId);
        if (!isExistingCaseInSession)
        {
            if (caseExistsInDatabase)
            {
                return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
                {
                    Response = new document_put_response { ok = false, error_description = $"Case {request.CaseId} is not part of the offline session." }
                };
            }

            if (!mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(
                    dbConfig,
                    user,
                    mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase,
                    modifiedDocument.home_record?.jurisdiction_id,
                    _couchDbHttpClient))
            {
                return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
                {
                    Response = new document_put_response { ok = false, error_description = $"Unauthorized to save case {request.CaseId} in jurisdiction {modifiedDocument.home_record?.jurisdiction_id}" }
                };
            }
        }
        else
        {
            var currentOfflineBy = currentCaseDocument.Value<string>("offline_by");
            var currentIsOffline = currentCaseDocument.Value<bool?>("is_offline") == true ||
                string.Equals(currentCaseDocument["is_offline"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

            if (!currentIsOffline)
            {
                return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
                {
                    Response = new document_put_response { ok = false, error_description = "Case is not currently in offline mode." }
                };
            }

            if (!string.Equals(currentOfflineBy, userName, StringComparison.OrdinalIgnoreCase))
            {
                return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
                {
                    Response = new document_put_response { ok = false, error_description = $"Case {request.CaseId} is offline locked by {currentOfflineBy}." }
                };
            }

            var currentCase = CaseJsonSerialization.DeserializeMmriaCase(currentCaseJson);
            var caseJurisdiction = currentCase?.home_record?.jurisdiction_id;
            if (!mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(
                    dbConfig,
                    user,
                    mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase,
                    caseJurisdiction,
                    _couchDbHttpClient))
            {
                return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
                {
                    Response = new document_put_response { ok = false, error_description = $"Unauthorized to save case {request.CaseId} in jurisdiction {caseJurisdiction}" }
                };
            }

            var currentRevision = currentCaseDocument.Value<string>("_rev");
            if (!string.Equals(modifiedDocument._rev, currentRevision, StringComparison.Ordinal))
            {
                return new mmria.common.SharedLibraries.Case.Manager.SaveCaseResult
                {
                    Response = new document_put_response { ok = false, error_description = "(409) Conflict: Case revision has changed while offline processing." }
                };
            }
        }

        modifiedDocument.is_offline = "false";
        modifiedDocument.offline_date = null;
        modifiedDocument.offline_by = null;
        modifiedDocument.offline_lock_type = null;
        modifiedDocument.last_updated_by = userName;
        modifiedDocument.date_last_updated = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(modifiedDocument.home_record?.record_id) &&
            modifiedDocument.home_record.record_id.EndsWith("-offline", StringComparison.OrdinalIgnoreCase))
        {
            modifiedDocument.home_record.record_id = modifiedDocument.home_record.record_id[..^"-offline".Length];
        }

        List<mmria.common.model.couchdb.Change_Stack_Item> changeStackItems;
        if (docChange.ChangeStackItems != null && docChange.ChangeStackItems.Count > 0)
        {
            changeStackItems = docChange.ChangeStackItems;
        }
        else
        {
            changeStackItems = new List<mmria.common.model.couchdb.Change_Stack_Item>
            {
                new()
                {
                    _id = modifiedDocument._id,
                    _rev = modifiedDocument._rev,
                    object_path = "offline_document_sync",
                    metadata_path = "/offline_sync",
                    old_value = "offline_changes",
                    new_value = "synced_to_server",
                    dictionary_path = "/offline_sync",
                    metadata_type = "offline_sync",
                    prompt = "Offline Document Sync",
                    date_created = DateTime.UtcNow,
                    user_name = userName
                }
            };
        }

        var changeStack = new Change_Stack
        {
            _id = Guid.NewGuid().ToString(),
            case_id = modifiedDocument._id,
            case_rev = modifiedDocument._rev,
            date_created = DateTime.UtcNow,
            user_name = userName,
            items = changeStackItems,
            note = $"Offline sync: Document modified offline and synced from session {request.OfflineSessionId}"
        };

        var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient);
        var saveResult = await caseManager.SaveCaseAsync(
            modifiedDocument,
            changeStack,
            dbConfig,
            user,
            configuration,
            hostPrefix,
            bypassOfflineTabOwnershipCheck: true);

        if (!saveResult.Response.ok)
        {
            return saveResult;
        }

        if (currentCaseDocument != null &&
            !string.IsNullOrWhiteSpace(currentCaseDocument.Value<string>("offline_by_tab_id")))
        {
            var savedCaseJson = await _caseDal.GetCaseDocumentJsonAsync(request.CaseId, dbConfig);
            var savedCaseDocument = JObject.Parse(savedCaseJson);
            savedCaseDocument.Remove("offline_by_tab_id");

            var cleanupResponseJson = await _caseDal.PutCaseDocumentJsonAsync(
                request.CaseId,
                savedCaseDocument.ToString(Formatting.None),
                dbConfig);

            var cleanupResponse = JsonConvert.DeserializeObject<document_put_response>(cleanupResponseJson) ??
                new document_put_response { ok = false, error_description = "Failed to clear offline_by_tab_id." };

            saveResult.Response = cleanupResponse;
            if (!cleanupResponse.ok)
            {
                return saveResult;
            }

            saveResult.SerializedCase = savedCaseDocument.ToString(Formatting.None);
        }

        return saveResult;
    }

    public async Task<string> CreateOfflineAuthTokenAsync(string userName, DBConfigurationDetail dbConfig)
    {
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
        var sessionExpirationDateTime = OfflineAuthSessionDefaults.GetExpirationDateTime(DateTime.Now);

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
                if (!mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(dbConfig, user, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase, caseJurisdiction, _couchDbHttpClient))
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
    }
}
