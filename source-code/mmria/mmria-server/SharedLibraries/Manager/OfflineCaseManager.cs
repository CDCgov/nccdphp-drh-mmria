using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using mmria.case_version.v251014;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.server;
using mmria.server.model.actor;
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
    private readonly ActorSystem _actorSystem;
    private readonly OverridableConfiguration _configuration;

    public OfflineCaseManager(
        OfflineCaseDAL offlineCaseDal,
        CaseDAL caseDal,
        SessionDAL sessionDal,
        ActorSystem actorSystem,
        OverridableConfiguration configuration)
    {
        _offlineCaseDal = offlineCaseDal;
        _caseDal = caseDal;
        _sessionDal = sessionDal;
        _actorSystem = actorSystem;
        _configuration = configuration;
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

    public async Task<document_put_response> CreateOfflineCaseAsync(OfflineCaseRequest request, string userName, string jurisdictionId, CancellationToken cancellationToken)
    {
        return await _offlineCaseDal.CreateOfflineCaseAsync(request, userName, jurisdictionId, cancellationToken);
    }

    public async Task<OfflineCaseResponse> GetOfflineCaseAsync(string id, string jurisdictionId, CancellationToken cancellationToken)
    {
        return await _offlineCaseDal.GetOfflineCaseAsync(id, jurisdictionId, cancellationToken);
    }

    public async Task<OfflineCaseListResponse> GetUserOfflineCasesAsync(string userId, string jurisdictionId, CancellationToken cancellationToken)
    {
        return await _offlineCaseDal.GetUserOfflineCasesAsync(userId, jurisdictionId, cancellationToken);
    }

    public async Task<OfflineCaseResponse> GetActiveUserSessionAsync(string userId, string jurisdictionId, CancellationToken cancellationToken)
    {
        var cases = await _offlineCaseDal.GetUserOfflineCasesAsync(userId, jurisdictionId, cancellationToken);
        // Assuming the latest or active one
        var active = cases.rows.OrderByDescending(r => r.value.date_created).FirstOrDefault(r => r.value.offline_state == 0 || r.value.offline_state == 1);
        return active?.value;
    }

    public async Task<OfflineCaseListResponse> GetAllActiveSessionsAsync(string jurisdictionId, CancellationToken cancellationToken)
    {
        return await _offlineCaseDal.GetAllActiveSessionsAsync(jurisdictionId, cancellationToken);
    }

    public async Task<LightweightOfflineCaseResponse> GetLightweightStatusOnlyAsync(string userId, string jurisdictionId, CancellationToken cancellationToken)
    {
        var session = await GetActiveUserSessionAsync(userId, jurisdictionId, cancellationToken);
        if (session == null) return null;

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

    public async Task<document_put_response> DeleteOfflineCaseAsync(string id, string jurisdictionId, CancellationToken cancellationToken)
    {
        var doc = await _offlineCaseDal.GetOfflineCaseAsync(id, jurisdictionId, cancellationToken);
        return await _offlineCaseDal.DeleteOfflineCaseAsync(id, doc._rev, jurisdictionId, cancellationToken);
    }

    public async Task<document_put_response> UpdateCasesAsync(SaveOfflineCasesRequest request, string userName, string jurisdictionId, CancellationToken cancellationToken)
    {
        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, jurisdictionId, cancellationToken);
        if (session.offline_state != 0)
        {
            throw new InvalidOperationException("Session is not in initial state");
        }

        session.case_documents = request.CaseDocuments;
        session.offline_state = 1; // in progress
        session.last_updated_by = userName;
        session.date_last_updated = DateTime.UtcNow;

        return await _offlineCaseDal.UpdateOfflineCaseAsync(request.OfflineSessionId, session, jurisdictionId, cancellationToken);
    }

    public async Task<document_put_response> UpdateSyncStatusAsync(DocumentChangeSyncStatusRequest request, string jurisdictionId, CancellationToken cancellationToken)
    {
        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, jurisdictionId, cancellationToken);
        var docChange = session.case_documents.FirstOrDefault(d => d.DocumentId == request._id);
        if (docChange != null)
        {
            docChange.SyncState = request.SyncState;
        }
        return await _offlineCaseDal.UpdateOfflineCaseAsync(request.OfflineSessionId, session, jurisdictionId, cancellationToken);
    }

    public async Task<document_put_response> UpdateOfflineStateAsync(UpdateOfflineStateRequest request, string jurisdictionId, CancellationToken cancellationToken)
    {
        var session = await _offlineCaseDal.GetOfflineCaseAsync(request.OfflineSessionId, jurisdictionId, cancellationToken);
        session.offline_state = request.OfflineState;
        return await _offlineCaseDal.UpdateOfflineCaseAsync(request.OfflineSessionId, session, jurisdictionId, cancellationToken);
    }

    public async Task<string> CreateOfflineAuthTokenAsync(string userName, string jurisdictionId, CancellationToken cancellationToken)
    {
        // TODO: Implement full token creation
        return Guid.NewGuid().ToString();
    }

    public async Task<object> SyncOfflineChangesAsync(string id, string userName, ClaimsPrincipal user, string jurisdictionId, CancellationToken cancellationToken)
    {
        var offlineCase = await _offlineCaseDal.GetOfflineCaseAsync(id, jurisdictionId, cancellationToken);
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

                var currentDoc = await _caseDal.GetCaseAsync(caseId, jurisdictionId, cancellationToken);
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
                if (!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(_configuration.GetDBConfig(jurisdictionId), user, mmria.server.utils.ResourceRightEnum.WriteCase, caseJurisdiction))
                {
                    validationErrors.Add($"Unauthorized to save case {caseId} in jurisdiction {caseJurisdiction}");
                    continue;
                }

                var saveResult = await _caseDal.UpdateCaseAsync(caseId, updatedDoc, jurisdictionId, cancellationToken);
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
}