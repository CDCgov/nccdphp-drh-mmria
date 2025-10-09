#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

using mmria.server.extension;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class OfflineCaseController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public OfflineCaseController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
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
                offline_ids = request.OfflineIds,
                offline_key = request.OfflineKey,
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
                        offline_ids = request.OfflineIds,
                        offline_key = request.OfflineKey,
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

    [Authorize(Roles = "abstractor, data_analyst")]
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
                offline_state = offlineCaseDoc.offline_state,
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
    /// Lightweight connectivity check endpoint for determining online/offline status.
    /// This endpoint requires no database calls and returns immediately.
    /// </summary>
    [HttpGet("connectivity-check")]
    [AllowAnonymous] // Allow anonymous access since this is just a connectivity check
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
}

// Request model for the offline case data
public class OfflineCaseRequest
{
    public List<string> OfflineIds { get; set; } = new List<string>();
    public string OfflineKey { get; set; } = string.Empty;
}

// Request model for saving offline cases with documents
public class SaveOfflineCasesRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public List<DocumentChange> CaseDocuments { get; set; } = new List<DocumentChange>();
}

// Request model for syncing offline changes
public class OfflineSyncRequest
{
    public string OfflineSessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public List<DocumentChange> DocumentChanges { get; set; } = new List<DocumentChange>();
}

// Model for individual document changes
public class DocumentChange
{
    public string DocumentId { get; set; } = string.Empty;
    public dynamic OriginalDocument { get; set; }
    public dynamic ModifiedDocument { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

// Enhanced model for document changes with complete original document
public class EnhancedDocumentChange
{
    public string DocumentId { get; set; } = string.Empty;
    public dynamic OriginalDocument { get; set; } // Complete original document from database
    public dynamic ModifiedDocument { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string OriginalRevision { get; set; } = string.Empty; // Track original revision
}
#endif
