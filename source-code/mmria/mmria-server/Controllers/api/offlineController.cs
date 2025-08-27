using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Dynamic;
using mmria.common;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using mmria.server.extension;
using Newtonsoft.Json;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class offlineController : ControllerBase
{
    ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    private readonly IAuthorizationService _authorizationService;

    public offlineController(
        IHttpContextAccessor httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration p_configuration,
        ActorSystem actorSystem,
        IAuthorizationService authorizationService
    )
    {
        configuration = p_configuration;
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;

        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Offline controller is working!", timestamp = DateTime.Now });
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost("mark-offline/{case_id}")]
    public async Task<IActionResult> MarkCaseOffline(string case_id)
    {
        try
        {
            // Get the existing case
            string request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
            var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();

            var case_data = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
            IDictionary<string, object> case_dict = case_data as IDictionary<string, object>;

            if (case_dict == null)
            {
                return NotFound("Case not found");
            }

            // Check authorization
            if (!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(
                db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, case_data))
            {
                return Unauthorized();
            }

            // Add offline flag
            case_dict["is_offline"] = true;
            case_dict["offline_marked_date"] = DateTime.UtcNow;
            case_dict["offline_marked_by"] = User.Identity.Name;

            // Save the updated case
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            string updated_case_string = JsonConvert.SerializeObject(case_data, settings);

            string save_url = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
            var save_curl = new cURL("PUT", null, save_url, updated_case_string, db_config.user_name, db_config.user_value);
            string save_response = await save_curl.executeAsync();

            var result = JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost("mark-online/{case_id}")]
    public async Task<IActionResult> MarkCaseOnline(string case_id)
    {
        try
        {
            // Validate case_id parameter
            if (string.IsNullOrEmpty(case_id))
            {
                Console.WriteLine("Error: case_id parameter is null or empty");
                return BadRequest("Case ID is required");
            }

            Console.WriteLine($"Marking case online: {case_id}");

            // Get the existing case
            string request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
            var case_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();

            var case_data = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
            IDictionary<string, object> case_dict = case_data as IDictionary<string, object>;

            if (case_dict == null)
            {
                Console.WriteLine($"Error: Case not found for ID: {case_id}");
                return NotFound("Case not found");
            }

            // Check authorization
            if (!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(
                db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, case_data))
            {
                return Unauthorized();
            }

            // Remove offline flag
            case_dict["is_offline"] = false;
            case_dict["offline_unmarked_date"] = DateTime.UtcNow;
            case_dict["offline_unmarked_by"] = User.Identity.Name;

            // Save the updated case
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            string updated_case_string = JsonConvert.SerializeObject(case_data, settings);

            string save_url = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
            var save_curl = new cURL("PUT", null, save_url, updated_case_string, db_config.user_name, db_config.user_value);
            string save_response = await save_curl.executeAsync();

            var result = JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MarkCaseOnline for case_id: {case_id}");
            Console.WriteLine($"Exception: {ex}");
            return StatusCode(500, $"Error marking case online: {ex.Message}");
        }
    }

    [Authorize(Roles = "abstractor")]
    [HttpGet("offline-cases")]
    public async Task<IActionResult> GetOfflineCases()
    {
        try
        {
            // Use a more efficient approach - get all case IDs first, then bulk fetch documents
            var is_identified_case = true;
            var cvs = new mmria.server.utils.CaseViewSearch
            (
                db_config, 
                User,
                is_identified_case,
                false // include_pinned_cases
            );

            var all_cases_result = await cvs.execute
            (
                System.Threading.CancellationToken.None,
                0, // skip
                250000, // take
                "by_date_created", // sort
                "", // search_key
                true, // descending
                "all", // case_status
                "all", // field_selection
                "all", // pregnancy_relatedness
                "all", // date_of_death_range
                "all", // date_of_review_range
                false // offline_only - get all cases first
            );

            var offline_cases = new List<dynamic>();

            if (all_cases_result != null && all_cases_result.rows != null && all_cases_result.rows.Count > 0)
            {
                // Create a dictionary for quick lookup of case view items by ID
                var case_lookup = all_cases_result.rows.ToDictionary(r => r.id, r => r);
                
                // Get all documents to check offline status
                string all_docs_url = db_config.Get_Prefix_DB_Url("mmrds/_all_docs?include_docs=true");
                var all_docs_curl = new cURL("GET", null, all_docs_url, null, db_config.user_name, db_config.user_value);
                
                string all_docs_response = await all_docs_curl.executeAsync();
                var all_docs_result = JsonConvert.DeserializeObject<dynamic>(all_docs_response);

                if (all_docs_result?.rows != null)
                {
                    foreach (var row in all_docs_result.rows)
                    {
                        try
                        {
                            if (row?.doc != null)
                            {
                                var doc = row.doc;
                                string case_id = doc.id ?? doc._id;
                                
                                // Check if case is marked as offline AND marked by current user
                                if (doc.is_offline == true && 
                                    doc.offline_marked_by != null &&
                                    doc.offline_marked_by.ToString() == User.Identity.Name &&
                                    case_lookup.ContainsKey(case_id))
                                {
                                    // Return the full case document instead of just case view item
                                    offline_cases.Add(doc);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing document row: {ex.Message}");
                        }
                    }
                }
            }

            // Return the offline cases as an array of full case documents
            return Ok(offline_cases);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost("sync-offline-changes")]
    public async Task<IActionResult> SyncOfflineChanges([FromBody] List<dynamic> offline_changes)
    {
        try
        {
            var sync_results = new List<dynamic>();

            foreach (var change in offline_changes)
            {
                try
                {
                    var case_id = change._id?.ToString();
                    if (string.IsNullOrEmpty(case_id))
                        continue;

                    // Check authorization
                    if (!mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(
                        db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, change))
                    {
                        sync_results.Add(new { case_id = case_id, status = "unauthorized", error = "Not authorized to update this case" });
                        continue;
                    }

                    // Add sync metadata
                    IDictionary<string, object> change_dict = change as IDictionary<string, object>;
                    change_dict["last_sync_date"] = DateTime.UtcNow;
                    change_dict["last_synced_by"] = User.Identity.Name;

                    // Save the change
                    var settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    string change_string = JsonConvert.SerializeObject(change, settings);

                    string save_url = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
                    var save_curl = new cURL("PUT", null, save_url, change_string, db_config.user_name, db_config.user_value);
                    string save_response = await save_curl.executeAsync();

                    var result = JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response);

                    sync_results.Add(new { case_id = case_id, status = result.ok ? "success" : "error", result = result });
                }
                catch (Exception ex)
                {
                    sync_results.Add(new { case_id = change._id?.ToString(), status = "error", error = ex.Message });
                }
            }

            return Ok(sync_results);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "abstractor")]
    [HttpGet("pending-sync-count")]
    public IActionResult GetPendingSyncCount()
    {
        try
        {
            // Get count from local storage (this would typically be handled client-side)
            // For now, return a placeholder
            return Ok(new { count = 0 });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, ex.Message);
        }
    }
}
