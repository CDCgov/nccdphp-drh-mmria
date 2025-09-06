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
}

// Request model for the offline case data
public class OfflineCaseRequest
{
    public List<string> OfflineIds { get; set; } = new List<string>();
    public string OfflineKey { get; set; } = string.Empty;
}
#endif
