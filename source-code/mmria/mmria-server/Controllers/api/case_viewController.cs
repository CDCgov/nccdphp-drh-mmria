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