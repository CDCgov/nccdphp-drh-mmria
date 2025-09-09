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
    public async Task<IActionResult> SaveOfflineCases(string id, [FromBody] List<object> caseDocuments)
    {
        try
        {
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
                case_documents = caseDocuments, // Add the case documents array
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
                    documentCount = caseDocuments?.Count ?? 0,
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
    public async Task<IActionResult> SyncOfflineChanges(string id, [FromBody] OfflineSyncRequest request)
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

                // Enhance the document changes with complete original documents from the database
                var enhancedChanges = new List<object>();
                var validationErrors = new List<string>();

                foreach (var documentChange in request.DocumentChanges)
                {
                    try
                    {
                        // Get the current complete document from the main case database
                        string getCaseUrl = $"{db_config.url}/{db_config.prefix}mmrds/{documentChange.DocumentId}";
                        var getCaseCurl = new cURL("GET", null, getCaseUrl, null, db_config.user_name, db_config.user_value);
                        
                        string currentCaseResponse = await getCaseCurl.executeAsync();
                        var currentCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(currentCaseResponse);

                        if (currentCaseDoc == null || currentCaseDoc._id == null)
                        {
                            validationErrors.Add($"Case document {documentChange.DocumentId} not found in database");
                            continue;
                        }

                        // Create enhanced change record with complete original document
                        var enhancedChange = new
                        {
                            DocumentId = documentChange.DocumentId,
                            OriginalDocument = currentCaseDoc, // Store complete original document
                            ModifiedDocument = documentChange.ModifiedDocument,
                            Timestamp = documentChange.Timestamp,
                            ChangeDescription = documentChange.ChangeDescription,
                            UserId = documentChange.UserId,
                            SessionId = documentChange.SessionId,
                            OriginalRevision = currentCaseDoc._rev?.ToString() // Track original revision
                        };

                        enhancedChanges.Add(enhancedChange);
                    }
                    catch (Exception ex)
                    {
                        validationErrors.Add($"Error validating document {documentChange.DocumentId}: {ex.Message}");
                        Console.WriteLine($"Error validating document {documentChange.DocumentId}: {ex}");
                    }
                }

                // If there were validation errors, return them
                if (validationErrors.Count > 0)
                {
                    return BadRequest(new 
                    { 
                        error = "Some documents could not be validated", 
                        validationErrors = validationErrors,
                        validChanges = enhancedChanges.Count,
                        totalChanges = request.DocumentChanges?.Count ?? 0
                    });
                }

                var updatedOfflineDocument = new
                {
                    _id = id,
                    _rev = offlineCaseDoc._rev?.ToString(),
                    offline_ids = offlineCaseDoc.offline_ids,
                    offline_key = offlineCaseDoc.offline_key?.ToString(),
                    offline_state = 1, // Keep as pending sync
                    pending_changes = enhancedChanges, // Store the enhanced changes with complete original documents
                    changes_received_timestamp = DateTime.UtcNow,
                    created_by = offlineCaseDoc.created_by?.ToString(),
                    date_created = offlineCaseDoc.date_created,
                    last_updated_by = userName,
                    date_last_updated = DateTime.UtcNow
                };

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string updatedOfflineDocString = Newtonsoft.Json.JsonConvert.SerializeObject(updatedOfflineDocument, settings);

                string putOfflineUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
                var putOfflineCurl = new cURL("PUT", null, putOfflineUrl, updatedOfflineDocString, db_config.user_name, db_config.user_value);

                string saveResponse = await putOfflineCurl.executeAsync();
                var saveResult = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(saveResponse);

                if (saveResult.ok)
                {
                    return Ok(new
                    {
                        message = "Offline changes saved successfully. Use apply-changes endpoint to sync to main database.",
                        offlineSessionId = id,
                        pendingChanges = enhancedChanges.Count,
                        validationErrors = validationErrors.Count > 0 ? validationErrors : null,
                        revision = saveResult.rev,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new { error = "Failed to save offline changes", details = saveResult.error_description });
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

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpPost("apply-changes/{id}")]
    public async Task<IActionResult> ApplyOfflineChanges(string id)
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

            // Get the offline case document with pending changes
            string getOfflineUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
            var getOfflineCurl = new cURL("GET", null, getOfflineUrl, null, db_config.user_name, db_config.user_value);
            
            string offlineDocResponse = await getOfflineCurl.executeAsync();
            var offlineCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(offlineDocResponse);

            if (offlineCaseDoc == null || offlineCaseDoc._id == null)
            {
                return NotFound(new { error = "Offline case document not found", id = id });
            }

            if (offlineCaseDoc.pending_changes == null)
            {
                return BadRequest(new { error = "No pending changes found to apply", id = id });
            }

            // Convert pending_changes back to enhanced DocumentChange objects
            var pendingChanges = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EnhancedDocumentChange>>(
                offlineCaseDoc.pending_changes.ToString());

            // Process each document change and apply to mmrds database
            var processedChanges = new List<object>();
            var errors = new List<string>();

            foreach (var documentChange in pendingChanges)
            {
                try
                {
                    // Get the current document from the main case database
                    string getCaseUrl = $"{db_config.url}/{db_config.prefix}mmrds/{documentChange.DocumentId}";
                    var getCaseCurl = new cURL("GET", null, getCaseUrl, null, db_config.user_name, db_config.user_value);
                    
                    string currentCaseResponse = await getCaseCurl.executeAsync();
                    var currentCaseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(currentCaseResponse);

                    if (currentCaseDoc == null || currentCaseDoc._id == null)
                    {
                        errors.Add($"Case document {documentChange.DocumentId} not found in database");
                        continue;
                    }

                    // Prepare the modified document for saving
                    var modifiedDoc = documentChange.ModifiedDocument;
                    
                    // Ensure we have the latest _rev from the database to avoid conflicts
                    if (modifiedDoc._rev == null || modifiedDoc._rev != currentCaseDoc._rev)
                    {
                        modifiedDoc._rev = currentCaseDoc._rev;
                    }

                    // Update audit fields
                    modifiedDoc.last_updated_by = userName;
                    modifiedDoc.date_last_updated = DateTime.UtcNow;

                    // Serialize the modified document
                    Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                    settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    string modifiedDocString = Newtonsoft.Json.JsonConvert.SerializeObject(modifiedDoc, settings);

                    // Save the modified document back to the main case database
                    string putCaseUrl = $"{db_config.url}/{db_config.prefix}mmrds/{documentChange.DocumentId}";
                    var putCaseCurl = new cURL("PUT", null, putCaseUrl, modifiedDocString, db_config.user_name, db_config.user_value);

                    string saveResponse = await putCaseCurl.executeAsync();
                    var saveResult = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(saveResponse);

                    if (saveResult.ok)
                    {
                        processedChanges.Add(new
                        {
                            documentId = documentChange.DocumentId,
                            status = "success",
                            newRevision = saveResult.rev,
                            timestamp = documentChange.Timestamp,
                            changeDescription = documentChange.ChangeDescription
                        });
                    }
                    else
                    {
                        errors.Add($"Failed to save document {documentChange.DocumentId}: {saveResult.error_description}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing document {documentChange.DocumentId}: {ex.Message}");
                    Console.WriteLine($"Error processing offline change for document {documentChange.DocumentId}: {ex}");
                }
            }

            // Update the offline case document to mark as synced
            try
            {
                var updatedOfflineDocument = new
                {
                    _id = id,
                    _rev = offlineCaseDoc._rev?.ToString(),
                    offline_ids = offlineCaseDoc.offline_ids,
                    offline_key = offlineCaseDoc.offline_key?.ToString(),
                    offline_state = 2, // Mark as synced
                    pending_changes = (object)null, // Clear pending changes
                    applied_changes = processedChanges, // Store what was applied
                    sync_timestamp = DateTime.UtcNow,
                    sync_errors = errors,
                    changes_received_timestamp = offlineCaseDoc.changes_received_timestamp,
                    created_by = offlineCaseDoc.created_by?.ToString(),
                    date_created = offlineCaseDoc.date_created,
                    last_updated_by = userName,
                    date_last_updated = DateTime.UtcNow
                };

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string updatedOfflineDocString = Newtonsoft.Json.JsonConvert.SerializeObject(updatedOfflineDocument, settings);

                string putOfflineUrl = $"{db_config.url}/{db_config.prefix}offline_cases/{id}";
                var putOfflineCurl = new cURL("PUT", null, putOfflineUrl, updatedOfflineDocString, db_config.user_name, db_config.user_value);

                await putOfflineCurl.executeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating offline case document after sync: {ex}");
                // Don't fail the entire operation if we can't update the offline doc
            }

            return Ok(new
            {
                message = "Offline changes applied to main database successfully",
                offlineSessionId = id,
                appliedChanges = processedChanges.Count,
                totalChanges = pendingChanges?.Count ?? 0,
                errors = errors.Count > 0 ? errors : null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { error = "Internal server error during apply changes", details = ex.Message });
        }
    }
}

// Request model for the offline case data
public class OfflineCaseRequest
{
    public List<string> OfflineIds { get; set; } = new List<string>();
    public string OfflineKey { get; set; } = string.Empty;
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
