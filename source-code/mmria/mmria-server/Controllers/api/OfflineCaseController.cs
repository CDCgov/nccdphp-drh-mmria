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
using Akka.Actor;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class OfflineCaseController: ControllerBase
{ 
        ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public OfflineCaseController
    (
        IHttpContextAccessor httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration _configuration,
        ActorSystem actorSystem
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
        _actorSystem = actorSystem;
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] OfflineCaseRequest request
    ) 
    { 
        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            string userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            // Create document ID: userid-randomguid
            string documentId = $"{userName}-{Guid.NewGuid()}";

            // Create the document to store
            var offlineDocument = new
            {
                _id = documentId,
                offline_ids = request.offline_ids,
                offline_key = request.offline_key,                
                offline_state = 0,
                created_by = userName,
                date_created = DateTime.UtcNow,
                last_updated_by = userName,
                date_last_updated = DateTime.UtcNow
            };

            // Serialize to JSON
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(offlineDocument, settings);

            // Check if document exists first (for updates)
            string checkUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{documentId}";
            var checkCurl = new cURL("GET", null, checkUrl, null, db_config.user_name, db_config.user_value);
            
            try
            {
                string existingDoc = await checkCurl.executeAsync();
                var existingObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(existingDoc);
                
                // If document exists, preserve the _rev for update
                if (existingObject != null && existingObject._rev != null)
                {
                    var updateDocument = new
                    {
                        _id = documentId,
                        _rev = existingObject._rev.ToString(),
                        offline_ids = request.offline_ids,
                        offline_key = request.offline_key,
                        created_by = existingObject.created_by?.ToString() ?? userName,
                        date_created = existingObject.date_created ?? DateTime.UtcNow,
                        last_updated_by = userName,
                        date_last_updated = DateTime.UtcNow
                    };
                    
                    object_string = Newtonsoft.Json.JsonConvert.SerializeObject(updateDocument, settings);
                }
            }
            catch (Exception)
            {
                // Document doesn't exist, proceed with creation
            }

            // PUT the document to the offline_cases database
            string putUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{documentId}";
            var putCurl = new cURL("PUT", null, putUrl, object_string, db_config.user_name, db_config.user_value);

            string responseFromServer = await putCurl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
        }
            
        return result;
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        try
        {
            // Get all documents for the user by querying with startkey/endkey
            string requestString = $"{db_config.url}/{db_config.prefix}offline_cases/_all_docs?include_docs=true&startkey=\"{userId}-\"&endkey=\"{userId}-\\ufff0\"";
            
            var curl = new cURL("GET", null, requestString, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await curl.executeAsync();

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseFromServer);
            
            return Ok(result);
        }
        catch(Exception ex) 
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
            // Validate input
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "Document ID is required" });
            }

            // Get the specific offline case document
            string requestString = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
            
            var curl = new cURL("GET", null, requestString, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await curl.executeAsync();

            // Check if document was found
            if (string.IsNullOrWhiteSpace(responseFromServer))
            {
                return NotFound(new { error = "Offline case document not found", documentId = id });
            }

            // Deserialize to strongly typed response
            var offlineCaseDocument = Newtonsoft.Json.JsonConvert.DeserializeObject<OfflineCaseResponse>(responseFromServer);
            
            if (offlineCaseDocument == null || string.IsNullOrWhiteSpace(offlineCaseDocument._id))
            {
                return NotFound(new { error = "Offline case document not found", documentId = id });
            }

            return Ok(offlineCaseDocument);
        }
        catch(Exception ex) 
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
            Console.WriteLine($"GetOfflineDocuments called by user: {User.Identity?.Name}");
            
            var current_user = User.Identity?.Name;
            if (string.IsNullOrEmpty(current_user))
            {
                Console.WriteLine("User identity not found");
                return null;
            }
            
            string request_string = db_config.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/by-created-by");

            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();


            // Deserialize to strongly typed response
            var offline_case_documents = Newtonsoft.Json.JsonConvert.DeserializeObject<OfflineCaseListResponse>(responseFromServer);

            var all_by_user = offline_case_documents.rows.Where(row => 
                row?.value.created_by != null && 
                string.Equals(row.value.created_by, current_user, StringComparison.OrdinalIgnoreCase)
                && (row.value.offline_state == 0 || row.value.offline_state == 1)
            ).ToList();

            if(all_by_user.Count == 0)
            {
                return Ok(new { error = "no active sessions" });
            }

            return Ok(all_by_user.First().value);
        }
        catch(Exception ex) 
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
            Console.WriteLine($"GetAllActiveSessions called by user: {User.Identity?.Name}");
            
            string request_string = db_config.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/by-created-by");

            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            // Deserialize to strongly typed response
            var offline_case_documents = Newtonsoft.Json.JsonConvert.DeserializeObject<OfflineCaseListResponse>(responseFromServer);

            var all_active = offline_case_documents.rows.Where(row => 
                row?.value != null && 
                (row.value.offline_state == 0 || row.value.offline_state == 1)
            ).Select(row => row.value).ToList();

            if(all_active.Count == 0)
            {
                return Ok(new { error = "no active sessions" });
            }

            return Ok(all_active);
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpDelete("{documentId}")]
    public async Task<mmria.common.model.couchdb.document_put_response> Delete(string documentId)
    {
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            // First get the document to obtain the _rev
            string getUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{documentId}";
            var getCurl = new cURL("GET", null, getUrl, null, db_config.user_name, db_config.user_value);
            
            string docResponse = await getCurl.executeAsync();
            var existingDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(docResponse);

            if (existingDoc != null && existingDoc._rev != null)
            {
                // Delete the document
                string deleteUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{documentId}?rev={existingDoc._rev}";
                var deleteCurl = new cURL("DELETE", null, deleteUrl, null, db_config.user_name, db_config.user_value);
                
                string responseFromServer = await deleteCurl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            else
            {
                result.ok = false;
                result.error_description = "Document not found";
            }
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
        }
            
        return result;
    }

    [Authorize(Roles = "offline_mode")]
    [HttpPost("update-cases/{id}")]
    public async Task<IActionResult> SaveOfflineCases(string id, [FromBody] SaveOfflineCasesRequest request)
    {
        try
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Invalid or missing offline case ID" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Request body is null or invalid" });
            }

            if (request.CaseDocuments == null)
            {
                return BadRequest(new { error = "CaseDocuments array is null" });
            }

            Console.WriteLine($"SaveOfflineCases called with ID: {id}, CaseDocuments count: {request.CaseDocuments.Count}");

            // Fetch the offline case document from the database
            string getUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
            var getCurl = new cURL("GET", null, getUrl, null, db_config.user_name, db_config.user_value);
            
            string docResponse = await getCurl.executeAsync();
            var offlineCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(docResponse);

            if (offlineCaseDoc == null || offlineCaseDoc._id == null)
            {
                return NotFound(new { error = "Offline case document not found", id = id });
            }

            // Get current user for audit trail
            string userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            // Create updated document with case documents added
            var updatedDocument = new
            {
                _id = id,
                _rev = offlineCaseDoc._rev?.ToString(),
                offline_ids = offlineCaseDoc.offline_ids,
                offline_key = offlineCaseDoc.offline_key?.ToString(),
                offline_state = 1,
                case_documents = request.CaseDocuments, // Add the case documents array
                created_by = offlineCaseDoc.created_by?.ToString(),
                date_created = offlineCaseDoc.date_created,
                last_updated_by = userName,
                date_last_updated = DateTime.UtcNow
            };

            // Serialize and save the updated document
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string updatedDocString = Newtonsoft.Json.JsonConvert.SerializeObject(updatedDocument, settings);

            // PUT the updated document back to the database
            string putUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
            var putCurl = new cURL("PUT", null, putUrl, updatedDocString, db_config.user_name, db_config.user_value);

            string responseFromServer = await putCurl.executeAsync();
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

            if (result.ok)
            {
                return Ok(new { 
                    message = "Case documents saved successfully", 
                    offlineCaseId = id,
                    documentCount = request.CaseDocuments?.Count ?? 0,
                    revision = result.rev
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to save case documents", details = result.error_description });
            }
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("sync-changes/{id}")]
    public async Task<IActionResult> SyncOfflineChanges(string id)
    {
        try
        {
            // Get current user for audit trail
            string userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            // Save the offline changes to the offline_cases database first
            try
            {
                string getOfflineUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
                var getOfflineCurl = new cURL("GET", null, getOfflineUrl, null, db_config.user_name, db_config.user_value);
                
                string offlineDocResponse = await getOfflineCurl.executeAsync();
                var offlineCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(offlineDocResponse);

                if (offlineCaseDoc == null || offlineCaseDoc._id == null)
                {
                    return NotFound(new { error = "Offline case document not found", id = id });
                }

                // Get the case documents from the offline document
                var caseDocuments = offlineCaseDoc.case_documents as Newtonsoft.Json.Linq.JArray;
                if (caseDocuments == null)
                {
                    return BadRequest(new { error = "No case documents found in offline session", id = id });
                }

                // Enhance the document changes with complete original documents from the database
                var enhancedChanges = new List<object>();
                var validationErrors = new List<string>();

                foreach (var docChangeToken in caseDocuments)
                {
                    try
                    {
                        var docChange = docChangeToken as Newtonsoft.Json.Linq.JObject;
                        if (docChange == null) continue;

                        var caseId = docChange["_id"]?.ToString();
                        if (string.IsNullOrWhiteSpace(caseId)) continue;

                        // Get the current document from the mmrds database to merge with changes
                        string getCurrentDocUrl = $"{db_config.url}/{db_config.prefix}mmrds/{caseId}";
                        var getCurrentDocCurl = new cURL("GET", null, getCurrentDocUrl, null, db_config.user_name, db_config.user_value);
                        
                        string currentDocResponse = await getCurrentDocCurl.executeAsync();
                        var currentDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(currentDocResponse);
                        
                        if (currentDoc == null)
                        {
                            validationErrors.Add($"Could not retrieve current document for case ID: {caseId}");
                            continue;
                        }

                        // Convert current document to JObject for easier manipulation
                        var currentDocJson = Newtonsoft.Json.JsonConvert.SerializeObject(currentDoc);
                        var currentDocObject = Newtonsoft.Json.Linq.JObject.Parse(currentDocJson);

                        // Apply the changes from offline document to the current document
                        foreach (var property in docChange.Properties())
                        {
                            if (property.Name != "_id") // Don't overwrite the ID
                            {
                                currentDocObject[property.Name] = property.Value;
                            }
                        }

                        // Update audit fields
                        currentDocObject["last_updated_by"] = userName;
                        currentDocObject["date_last_updated"] = DateTime.UtcNow.ToString("o");

                        // Validate jurisdiction authorization
                        var jurisdictionId = currentDocObject["home_record"]?["jurisdiction_id"]?.ToString();
                        if (!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, jurisdictionId))
                        {
                            validationErrors.Add($"Unauthorized to save case {caseId} in jurisdiction {jurisdictionId}");
                            continue;
                        }

                        // Serialize the merged document for saving
                        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                        string mergedDocString = Newtonsoft.Json.JsonConvert.SerializeObject(currentDocObject, settings);

                        // Save the merged document to the mmrds database
                        string saveUrl = $"{db_config.url}/{db_config.prefix}mmrds/{caseId}";
                        var saveCurl = new cURL("PUT", null, saveUrl, mergedDocString, db_config.user_name, db_config.user_value);

                        string saveResponse = await saveCurl.executeAsync();
                        var saveResult = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(saveResponse);

                        if (saveResult.ok)
                        {
                            enhancedChanges.Add(new { 
                                caseId = caseId, 
                                status = "saved",
                                revision = saveResult.rev
                            });
                            Console.WriteLine($"Successfully saved case {caseId} to mmrds database");
                        }
                        else
                        {
                            validationErrors.Add($"Failed to save case {caseId}: {saveResult.error_description}");
                            Console.WriteLine($"Failed to save case {caseId}: {saveResult.error_description}");
                        }
                    }
                    catch (Exception docEx)
                    {
                        Console.WriteLine($"Error processing document change: {docEx}");
                        validationErrors.Add($"Error processing document: {docEx.Message}");
                    }
                }

                // Return the results of the sync operation
                if (validationErrors.Any())
                {
                    return BadRequest(new { 
                        error = "Some documents failed to sync", 
                        validationErrors = validationErrors,
                        successfulSaves = enhancedChanges.Count,
                        failedSaves = validationErrors.Count
                    });
                }
                else
                {
                    return Ok(new { 
                        message = "All offline changes synced successfully to mmrds database", 
                        syncedDocuments = enhancedChanges,
                        totalSynced = enhancedChanges.Count
                    });
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving offline changes: {ex}");
                return StatusCode(500, new { error = "Internal server error saving offline changes", details = ex.Message });
            }
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
            // Validate input parameters
            if (request == null)
            {
                return BadRequest(new { error = "Request body is null or invalid" });
            }

            if (string.IsNullOrWhiteSpace(request.OfflineSessionId))
            {
                return BadRequest(new { error = "OfflineSessionId is required" });
            }

            if (string.IsNullOrWhiteSpace(request._id))
            {
                return BadRequest(new { error = "Document ID (_id) is required" });
            }

            // Get current user for audit trail
            string userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            // Fetch the offline case document from the database
            string getUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{request.OfflineSessionId}";
            var getCurl = new cURL("GET", null, getUrl, null, db_config.user_name, db_config.user_value);
            
            string docResponse = await getCurl.executeAsync();
            var offlineCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(docResponse);

            if (offlineCaseDoc == null || offlineCaseDoc._id == null)
            {
                return NotFound(new { error = "Offline case document not found", offlineSessionId = request.OfflineSessionId });
            }

            // Get the case documents array
            var caseDocuments = offlineCaseDoc.case_documents as Newtonsoft.Json.Linq.JArray;
            if (caseDocuments == null)
            {
                return BadRequest(new { error = "No case documents found in offline session", offlineSessionId = request.OfflineSessionId });
            }

            // Find and update the specific document's sync status
            bool documentFound = false;
            foreach (var docToken in caseDocuments)
            {
                var doc = docToken as Newtonsoft.Json.Linq.JObject;
                if (doc != null && doc["DocumentId"]?.ToString() == request._id)
                {
                    doc["SyncState"] = request.SyncState;
                    documentFound = true;
                    break;
                }
            }

            if (!documentFound)
            {
                return NotFound(new { error = "Document not found in offline session", documentId = request._id, offlineSessionId = request.OfflineSessionId });
            }

            // Create updated document
            var updatedDocument = new
            {
                _id = request.OfflineSessionId,
                _rev = offlineCaseDoc._rev?.ToString(),
                offline_ids = offlineCaseDoc.offline_ids,
                offline_key = offlineCaseDoc.offline_key?.ToString(),
                offline_state = offlineCaseDoc.offline_state,
                case_documents = caseDocuments,
                created_by = offlineCaseDoc.created_by?.ToString(),
                date_created = offlineCaseDoc.date_created,
                last_updated_by = userName,
                date_last_updated = DateTime.UtcNow
            };

            // Serialize and save the updated document
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string updatedDocString = Newtonsoft.Json.JsonConvert.SerializeObject(updatedDocument, settings);

            // PUT the updated document back to the database
            string putUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{request.OfflineSessionId}";
            var putCurl = new cURL("PUT", null, putUrl, updatedDocString, db_config.user_name, db_config.user_value);

            string responseFromServer = await putCurl.executeAsync();
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

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
        catch(Exception ex) 
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
    //[Authorize(Roles = "offline_mode")]
    public IActionResult ConnectivityCheck()
    {
        try
        {
            // This is a lightweight endpoint that doesn't require database access
            // It simply returns a success response to indicate the server is reachable
            return Ok(new
            {
                status = "online",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                message = "Server is reachable"
            });
        }
        catch (Exception ex)
        {
            // Even if there's an exception, we want to return a response
            // since the fact that we're executing this code means the server is running
            return Ok(new
            {
                status = "online",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                message = "Server is reachable",
                note = "Exception occurred but server is still accessible"
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
            // Validate input parameters
            if (request == null)
            {
                return BadRequest(new { error = "Request body is null or invalid" });
            }

            if (string.IsNullOrWhiteSpace(request.OfflineSessionId))
            {
                return BadRequest(new { error = "OfflineSessionId is required" });
            }

            // Get current user for audit trail
            string userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            // Fetch the offline case document from the database
            string getUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{request.OfflineSessionId}";
            var getCurl = new cURL("GET", null, getUrl, null, db_config.user_name, db_config.user_value);
            
            string docResponse = await getCurl.executeAsync();
            var offlineCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(docResponse);

            if (offlineCaseDoc == null || offlineCaseDoc._id == null)
            {
                return NotFound(new { error = "Offline case document not found", offlineSessionId = request.OfflineSessionId });
            }

            // Create updated document with new offline state
            var updatedDocument = new
            {
                _id = request.OfflineSessionId,
                _rev = offlineCaseDoc._rev?.ToString(),
                offline_ids = offlineCaseDoc.offline_ids,
                offline_key = offlineCaseDoc.offline_key?.ToString(),
                offline_state = request.OfflineState,
                case_documents = offlineCaseDoc.case_documents,
                created_by = offlineCaseDoc.created_by?.ToString(),
                date_created = offlineCaseDoc.date_created,
                last_updated_by = userName,
                date_last_updated = DateTime.UtcNow
            };

            // Serialize and save the updated document
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string updatedDocString = Newtonsoft.Json.JsonConvert.SerializeObject(updatedDocument, settings);

            // PUT the updated document back to the database
            string putUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{request.OfflineSessionId}";
            var putCurl = new cURL("PUT", null, putUrl, updatedDocString, db_config.user_name, db_config.user_value);

            string responseFromServer = await putCurl.executeAsync();
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

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
                    updatedBy = userName,
                    updatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update offline state", details = result.error_description });
            }
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current API cache version for offline mode.
    /// This endpoint provides the single source of truth for cache versioning,
    /// preventing hardcoded version strings from becoming out of sync.
    /// </summary>
    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet("cache-version")]
    public IActionResult GetCacheVersion()
    {
        try
        {
            // Single source of truth for cache versioning - update these constants to change version
            const string VERSION = "v40";
            const string STABILITY = "stable";
            
            // Computed values - no need to update these manually
            var cacheVersion = $"mmria-api-{VERSION}-{STABILITY}";
            var baseVersion = $"{VERSION}-{STABILITY}";

            return Ok(new
            {
                cacheVersion = cacheVersion,
                baseVersion = baseVersion,
                version = VERSION,
                stability = STABILITY,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Failed to get cache version", details = ex.Message });
        }
    }

      [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("create-offline-auth-token")]
    public async Task<IActionResult> CreateOfflineAuthToken()
    {
        try
        {
            // Get current user for audit trail
            string userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }
            int expire_minutes = 24 * 7 * 60;

            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new { error = "Unable to determine current user" });
            }

            List<string> role_list = new List<string>();
            role_list.Add("offline_mode");

            // Create minimal claims for offline mode - only username and offline role
            const string Issuer = "https://contoso.com";
            var claims = new List<System.Security.Claims.Claim>();
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userName, System.Security.Claims.ClaimValueTypes.String, Issuer));
            
            // ONLY add the offline role - remove all other roles for security
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "offline_mode", System.Security.Claims.ClaimValueTypes.String, Issuer));
            
            // Set extended expiration for offline work (7 days)
            var extendedExpiry = DateTime.UtcNow.AddMinutes(expire_minutes);
            claims.Add(new System.Security.Claims.Claim("exp", new DateTimeOffset(extendedExpiry).ToUnixTimeSeconds().ToString(), System.Security.Claims.ClaimValueTypes.Integer64, Issuer));
            
            var userIdentity = new ClaimsIdentity("SuperSecureLogin");
            userIdentity.AddClaims(claims);
            var userPrincipal = new ClaimsPrincipal(userIdentity);

            this.HttpContext.User = userPrincipal;
            System.Threading.Thread.CurrentPrincipal = userPrincipal;

            // Create a simple token response (for now, just return the claims info)
            // In a full JWT implementation, you would create an actual JWT token here
            var tokenResponse = new
            {
                user_name = userName,
                roles = role_list,
                expires_at = extendedExpiry.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                expires_unix = new DateTimeOffset(extendedExpiry).ToUnixTimeSeconds(),
                token_type = "offline_bearer",
                message = "Offline authentication token created successfully. User must re-authenticate when going back online."
            };


var Session_Event_Message = new mmria.server.model.actor.Session_Event_Message
                (
                    DateTime.Now,
                    userName,
                    "1.1.1.1",//this.GetRequestIP(),
                   mmria.server.model.actor.Session_Event_Message.Session_Event_Message_Action_Enum.successful_login
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Record_Session_Event>(db_config)).Tell(Session_Event_Message);

                var Session_Message_id = Guid.NewGuid().ToString();
                var session_data = new System.Collections.Generic.Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                var session_expiration_datetime = DateTime.Now.AddMinutes(expire_minutes);
                var Session_Message = new mmria.server.model.actor.Session_Message
                (
                    Session_Message_id, //_id = 
                    null, //_rev = 
                    DateTime.Now, //date_created = 
                    DateTime.Now, //date_last_updated = 
                    session_expiration_datetime, //date_expired = 

                    true, //is_active = 
                    userName, //user_id = 
                    "1.1.1.1",//this.GetRequestIP(),
                    Session_Event_Message._id, // session_event_id = 
                    role_list,
                    session_data
                );

                var config_couchdb_url = db_config.url;
                var config_timer_user_name = db_config.user_name;
                var config_timer_password = db_config.user_value;



                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(Session_Message, settings);

                string request_string = db_config.url + "/_session";
                request_string = config_couchdb_url + $"/{db_config.prefix}session/{Session_Message._id}";

                mmria.server.cURL document_curl = new mmria.server.cURL("PUT", null, request_string, object_string, config_timer_user_name, config_timer_password);

                try
                {
                    string responseFromServer = document_curl.execute();
                    var put_session_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                    if (put_session_result.ok)
                    {
                        _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Post_Session>(db_config)).Tell(Session_Message);
                        Response.Cookies.Append("sid", Session_Message._id, new CookieOptions { HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                        //Response.Cookies.Append("aid", Session_Message._id, new CookieOptions{ HttpOnly = false });
                        //Response.Cookies.Append("expires_at", unix_time.ToString(), new CookieOptions{ HttpOnly = true });

                        /*
                            Response.Cookies.Append("sid", Session_Message._id, new CookieOptions{ HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                            Response.Cookies.Append("expires_at", unix_time.ToString(), new CookieOptions{ HttpOnly = true, Expires = session_expiration_datetime, SameSite = SameSiteMode.Strict });
                        */

                        //return RedirectToAction("Index", "HOME");
                        //return RedirectToAction("Index", "HOME");
                        //return RedirectToAction("Index", "HOME");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }


            Console.WriteLine($"Created offline token for user {userName}, expires {extendedExpiry}");
            return Ok(new
            {
                status = "success"                
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error creating offline token", details = ex.Message });
        }
    }

}

// Request model for the offline case data
public class OfflineCaseRequest
{
    public List<string> offline_ids { get; set; } = new List<string>();
    public string offline_key { get; set; } = string.Empty;
    public string device_id { get; set; } = string.Empty;
    public string browser_id { get; set; } = string.Empty;    
}

// Request model for saving offline cases with documents
public class SaveOfflineCasesRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public List<DocumentChange> CaseDocuments { get; set; } = new List<DocumentChange>();
}


// Model for individual document changes
public class DocumentChange
{
    public string DocumentId { get; set; } = string.Empty;
    public mmria.case_version.v251014.mmria_case OriginalDocument { get; set; }
    public mmria.case_version.v251014.mmria_case ModifiedDocument { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public int SyncState { get; set; } = 0; // 0 = not synced, 1 = synced, 2 = abandoned, 3 = error
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public List<mmria.common.model.couchdb.Change_Stack_Item> ChangeStackItems { get; set; } = new List<mmria.common.model.couchdb.Change_Stack_Item>();
}

// Response model for offline case document
public class OfflineCaseResponse
{
    public string _id { get; set; } = string.Empty;
    public string _rev { get; set; } = string.Empty;
    public List<string> offline_ids { get; set; } = new List<string>();
    public string offline_key { get; set; } = string.Empty;    
    public int offline_state { get; set; } = 0;
    public List<DocumentChange> case_documents { get; set; } = new List<DocumentChange>();
    public string created_by { get; set; } = string.Empty;
    public DateTime date_created { get; set; }
    public string last_updated_by { get; set; } = string.Empty;
    public DateTime date_last_updated { get; set; }
}

public class DocumentChangeSyncStatusRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public string _id { get; set; } = string.Empty;//case document ID
    public int SyncState { get; set; } = 0; // 0 = not synced, 1 = synced, 2 = processed, 3 = abandoned, 4 = error
}

public class UpdateOfflineStateRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public int OfflineState { get; set; } = 0; // 0 = initial/not started, 1 = in progress, 2 = completed, 3 = error/failed
}

#endif
public sealed class OfflineCaseItem
{
    public OfflineCaseItem(){}

    public string id { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public string key { get; set; } //": "16e458537602f5ef2a710089dffd9453",
    public OfflineCaseResponse value {  get; set; }

}

public sealed class OfflineCaseListResponse
{
    public OfflineCaseListResponse () 
    {
        this.rows = new System.Collections.Generic.List<OfflineCaseItem> ();
    }

    public OfflineCaseListResponse 
    (
        int p_offset,
        System.Collections.Generic.List<OfflineCaseItem> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }


    public int offset { get; set; } //": 0,
    public System.Collections.Generic.List<OfflineCaseItem> rows { get; set; }
    public int total_rows { get; set; } 
}