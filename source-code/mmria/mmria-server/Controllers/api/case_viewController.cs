#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.functional;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/[controller]")]
public sealed class case_viewController: ControllerBase 
{  

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;

    public case_viewController  (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);

    }

    [HttpGet]
    public async Task<mmria.common.model.couchdb.case_view_response> Get
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,
        bool descending = false,
        string case_status = "all",
        string field_selection = "all",
        string pregnancy_relatedness ="all",
        string date_of_death_range = "all",
        string date_of_review_range = "all",
        bool include_pinned_cases = false

    ) 
    {
        /*
        System.Console.WriteLine("case_viewController.Get");
        System.Console.WriteLine($"host_prefix = {host_prefix}");
        System.Console.WriteLine($"db_config.url = {db_config.url}");
        System.Console.WriteLine($"db_config.prefix = {db_config.prefix}");
        */
        
        var is_identefied_case = true;
        var cvs = new mmria.server.utils.CaseViewSearch
        (
            db_config, 
            User,
            is_identefied_case,
            include_pinned_cases
        );

        var result = await cvs.execute
        (
            cancellationToken,
            skip,
            take,
            sort,
            search_key,
            descending,
            case_status,
            field_selection,
            pregnancy_relatedness,
            date_of_death_range,
            date_of_review_range
        );


        return result;
    }



    [HttpGet("record-id-list")]
    public async Task<System.Collections.Generic.List<string>> GetRecordIdList(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var case_view_curl = new cURL(
                "GET",
                null,
                db_config.url + $"/{db_config.prefix}mmrds/_design/sortable/_view/record_id_list",
                null,
                db_config.user_name,
                db_config.user_value
            );

            var case_view_response = await case_view_curl.executeAsync();
            var case_view_result = System.Text.Json.JsonSerializer.Deserialize<mmria.common.model.couchdb.case_view_response>(case_view_response);

            var result = new System.Collections.Generic.List<string>();

            foreach (var item in case_view_result.rows)
            {
                result.Add(item.value.record_id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return new System.Collections.Generic.List<string>();
    }

    [HttpPost("toggle-offline/{caseId}")]
    public async Task<IActionResult> ToggleOfflineStatus(string caseId, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"ToggleOfflineStatus called for caseId: {caseId}");
            
            // Get the current case document
            var case_curl = new cURL(
                "GET", 
                null, 
                db_config.url + $"/{db_config.prefix}mmrds/" + caseId,
                null, 
                db_config.user_name, 
                db_config.user_value
            );

            var case_response = await case_curl.executeAsync();
            Console.WriteLine($"Case response length: {case_response?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(case_response))
            {
                return NotFound(new { success = false, message = "Case not found" });
            }

            // Check if the response indicates an error
            if (case_response.Contains("\"error\""))
            {
                Console.WriteLine($"CouchDB error in response: {case_response}");
                return BadRequest(new { success = false, message = "Error retrieving case from database", details = case_response });
            }

            // Use Newtonsoft.Json for better compatibility with existing code
            var case_document = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(case_response);
            
            if (case_document == null)
            {
                return BadRequest(new { success = false, message = "Invalid case document format" });
            }

            Console.WriteLine($"Case document loaded successfully, has {case_document.Count} properties");

            // Ensure we have the _id and _rev fields
            if (!case_document.ContainsKey("_id"))
            {
                case_document["_id"] = caseId;
            }

            if (!case_document.ContainsKey("_rev"))
            {
                Console.WriteLine("Warning: Document missing _rev field");
                return BadRequest(new { success = false, message = "Document missing revision information" });
            }

            Console.WriteLine($"Document revision: {case_document["_rev"]}");

            // Toggle offline state
            bool currentOfflineState = false;
            if (case_document.ContainsKey("is_offline") && case_document["is_offline"] != null)
            {
                if (case_document["is_offline"] is bool boolValue)
                {
                    currentOfflineState = boolValue;
                }
                else if (case_document["is_offline"] is string stringValue)
                {
                    bool.TryParse(stringValue, out currentOfflineState);
                }
                // Handle Newtonsoft.Json.Linq.JValue case
                else if (case_document["is_offline"].ToString().ToLowerInvariant() == "true")
                {
                    currentOfflineState = true;
                }
            }

            Console.WriteLine($"Current offline state: {currentOfflineState}");

            // Set new offline state
            bool newOfflineState = !currentOfflineState;
            case_document["is_offline"] = newOfflineState;
            case_document["offline_date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            case_document["offline_by"] = User.Identity?.Name ?? "system";

            // Update last_updated fields
            case_document["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            case_document["last_updated_by"] = User.Identity?.Name ?? "system";

            Console.WriteLine($"New offline state: {newOfflineState}");

            // Save the updated document
            var json_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_document);
            Console.WriteLine($"Serialized document length: {json_string.Length}");
            
            var save_curl = new cURL(
                "PUT", 
                null, 
                db_config.url + $"/{db_config.prefix}mmrds/" + caseId,
                json_string, 
                db_config.user_name, 
                db_config.user_value
            );

            var save_response = await save_curl.executeAsync();
            Console.WriteLine($"Save response: {save_response}");
            
            if (string.IsNullOrEmpty(save_response))
            {
                return StatusCode(500, new { success = false, message = "Empty response from database" });
            }

            var save_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response);

            if (save_result != null && save_result.ok)
            {
                Console.WriteLine($"Document updated successfully. New revision: {save_result.rev}");
                return Ok(new { success = true, is_offline = newOfflineState, message = $"Case {(newOfflineState ? "marked for offline use" : "removed from offline use")}" });
            }
            else
            {
                Console.WriteLine($"Save failed - save_result.ok: {save_result?.ok}, error: {save_result?.error_description}");
                var errorMsg = save_result?.error_description ?? "Unknown error";
                return BadRequest(new { success = false, message = "Failed to update case offline status", error = errorMsg, details = save_response });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ToggleOfflineStatus: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = "Internal server error while toggling offline status", error = ex.Message });
        }
    }

    [HttpGet("offline-documents")]
    public async Task<mmria.common.model.couchdb.case_view_response> GetOfflineDocuments
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        bool descending = false
    )
    {
        try
        {
            Console.WriteLine($"GetOfflineDocuments called by user: {User.Identity?.Name}");
            
            var current_user = User.Identity?.Name;
            if (string.IsNullOrEmpty(current_user))
            {
                Console.WriteLine("User identity not found");
                return new mmria.common.model.couchdb.case_view_response();
            }

            // For debugging, let's get all documents and filter to see what we have
            // Using a larger limit to ensure we get all potential offline documents
            var large_limit = 10000; // Get many documents to ensure we don't miss any
            
            var sort_view = sort switch
            {
                "by_date_created" => "by_date_created",
                "by_date_last_updated" => "by_date_last_updated",
                "by_last_name" => "by_last_name",
                "by_first_name" => "by_first_name",
                "by_middle_name" => "by_middle_name",
                "by_year_of_death" => "by_year_of_death",
                "by_month_of_death" => "by_month_of_death",
                "by_committee_review_date" => "by_committee_review_date",
                "by_created_by" => "by_created_by",
                "by_last_updated_by" => "by_last_updated_by",
                "by_state_of_death" => "by_state_of_death",
                "by_record_id" => "by_record_id",
                _ => "by_date_created"
            };

            // Get all documents to see what's available (force view update)
            var descending_text = descending ? "&descending=true" : "";
            var request_string = db_config.Get_Prefix_DB_Url($"mmrds/_design/sortable/_view/{sort_view}?skip=0&limit={large_limit}{descending_text}&update=true");

            Console.WriteLine($"Executing CouchDB query: {request_string}");

            var case_view_curl = new cURL(
                "GET",
                null,
                request_string,
                null,
                db_config.user_name,
                db_config.user_value
            );

            var case_view_response = await case_view_curl.executeAsync();
            
            if (string.IsNullOrEmpty(case_view_response))
            {
                Console.WriteLine("Empty response from CouchDB");
                return new mmria.common.model.couchdb.case_view_response();
            }

            Console.WriteLine($"CouchDB response length: {case_view_response.Length}");
            Console.WriteLine($"First 500 chars of response: {case_view_response.Substring(0, Math.Min(500, case_view_response.Length))}");

            // Use Newtonsoft.Json for consistency with ToggleOfflineStatus method
            var case_view_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(case_view_response);
            
            if (case_view_result?.rows == null)
            {
                Console.WriteLine("No rows found in CouchDB response");
                return new mmria.common.model.couchdb.case_view_response();
            }

            // Filter to only include documents that are offline and created by the current user
            Console.WriteLine($"Total rows retrieved from CouchDB: {case_view_result.rows.Count}");
            
            var all_by_user = case_view_result.rows.Where(row => 
                row?.value != null && 
                string.Equals(row.value.offline_by, current_user, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            Console.WriteLine($"Documents created by current user ({current_user}): {all_by_user.Count}");
            
            var offline_by_user = all_by_user.Where(row => 
            {
                var is_offline = false;
                
                // Handle different possible types for is_offline field
                if (row.value.is_offline.HasValue)
                {
                    is_offline = row.value.is_offline.Value;
                }
                else
                {
                    // Check if the raw JSON might have this as a different type
                    Console.WriteLine($"Document {row.value.record_id}: is_offline is null, checking raw data...");
                }
                
                Console.WriteLine($"Document {row.value.record_id}: is_offline={is_offline}, created_by={row.value.created_by}, offline_by={row.value.offline_by}, offline_date={row.value.offline_date}");
                return is_offline;
            }).ToList();

            Console.WriteLine($"Offline documents created by current user: {offline_by_user.Count}");

            var filtered_rows = offline_by_user.Skip(skip).Take(take).ToList();

            Console.WriteLine($"Final filtered results (after skip/take): {filtered_rows.Count}");

            // Create a new response with filtered results
            var result = new mmria.common.model.couchdb.case_view_response
            {
                total_rows = offline_by_user.Count, // Total offline documents for this user
                offset = skip,
                rows = filtered_rows
            };

            Console.WriteLine($"Returning {filtered_rows.Count} documents out of {offline_by_user.Count} total offline documents");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in GetOfflineDocuments: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new mmria.common.model.couchdb.case_view_response();
        }
    }

    [HttpGet("check-document/{caseId}")]
    public async Task<IActionResult> CheckDocument(string caseId, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"CheckDocument called for caseId: {caseId}");
            
            // Get the current case document directly
            var case_curl = new cURL(
                "GET", 
                null, 
                db_config.url + $"/{db_config.prefix}mmrds/" + caseId,
                null, 
                db_config.user_name, 
                db_config.user_value
            );

            var case_response = await case_curl.executeAsync();
            
            if (string.IsNullOrEmpty(case_response))
            {
                return NotFound(new { success = false, message = "Case not found" });
            }

            // Parse as raw JSON to see exactly what's stored
            var case_document = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(case_response);
            
            return Ok(new { 
                case_id = caseId,
                has_is_offline_field = case_document.ContainsKey("is_offline"),
                is_offline_value = case_document.ContainsKey("is_offline") ? case_document["is_offline"] : null,
                is_offline_type = case_document.ContainsKey("is_offline") ? case_document["is_offline"]?.GetType()?.Name : null,
                has_offline_by = case_document.ContainsKey("offline_by"),
                offline_by = case_document.ContainsKey("offline_by") ? case_document["offline_by"] : null,
                has_offline_date = case_document.ContainsKey("offline_date"),
                offline_date = case_document.ContainsKey("offline_date") ? case_document["offline_date"] : null,
                created_by = case_document.ContainsKey("created_by") ? case_document["created_by"] : null,
                record_id = case_document.ContainsKey("record_id") ? case_document["record_id"] : null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("debug-offline")]
    public async Task<IActionResult> DebugOfflineDocuments(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var current_user = User.Identity?.Name;
            Console.WriteLine($"Debug: Current user is: '{current_user}'");
            
            // Get a few recent documents to examine their structure
            var request_string = db_config.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?limit=50&descending=true");
            
            var case_view_curl = new cURL(
                "GET",
                null,
                request_string,
                null,
                db_config.user_name,
                db_config.user_value
            );

            var case_view_response = await case_view_curl.executeAsync();
            var case_view_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(case_view_response);
            
            var debug_info = new List<object>();
            
            foreach (var row in case_view_result.rows.Take(10)) // Just look at first 10
            {
                debug_info.Add(new {
                    id = row.id,
                    record_id = row.value?.record_id,
                    created_by = row.value?.created_by,
                    is_offline = row.value?.is_offline,
                    is_offline_type = row.value?.is_offline?.GetType()?.Name,
                    created_by_matches = string.Equals(row.value?.created_by, current_user, StringComparison.OrdinalIgnoreCase)
                });
            }
            
            return Ok(new { 
                current_user = current_user,
                debug_info = debug_info,
                total_documents_checked = debug_info.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    public async Task<HashSet<string>> GetExistingRecordIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000");

            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                result.Add(cvi.value.record_id);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

} 

#endif