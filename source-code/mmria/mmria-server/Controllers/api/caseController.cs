#if !IS_PMSS_ENHANCED
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

using  mmria.server.extension;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace mmria.server;


[Route("api/[controller]")]
public sealed class caseController: ControllerBase 
{ 
    ActorSystem _actorSystem;	

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.SharedLibraries.Manager.CaseManager _caseManager;

    private readonly IAuthorizationService _authorizationService;
    //private readonly IDocumentRepository _documentRepository;

    public caseController
    ( 
        IHttpContextAccessor httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration p_configuration, 
        ActorSystem actorSystem, 
        IAuthorizationService authorizationService,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.server.SharedLibraries.Manager.CaseManager caseManager
    )
    {
         configuration = p_configuration;
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _couchDbHttpClient = couchDbHttpClient;
        _caseManager = caseManager;

        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(
            _overridableConfigSets,
            p_configuration,
            host_prefix
        );
        
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(
            _dbConfigSets,
            p_configuration,
            host_prefix
        );
    }
    

    [Authorize(Roles  = "abstractor, data_analyst")]
    [HttpGet]
    //public async Task<System.Dynamic.ExpandoObject> Get(string case_id) 
    public async Task<mmria.case_version.v260120.mmria_case> Get(string case_id) 
    { 
        try
        {
            return await _caseManager.GetCaseAsync(case_id, db_config, User);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 


    public sealed class Save_Case_Request
    {
        public mmria.common.model.couchdb.Change_Stack Change_Stack {get;set;} = new();

        public mmria.case_version.v260120.mmria_case Case_Data {get;set;}
        public Save_Case_Request()
        {

        }
    }


    [Authorize(Roles  = "abstractor")]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] Save_Case_Request save_case_request
    ) 
    { 
        try
        {
            var saveResult = await _caseManager.SaveCaseAsync(
                save_case_request.Case_Data,
                save_case_request.Change_Stack,
                db_config,
                User,
                configuration,
                host_prefix
            );

            // Dispatch sync message if save was successful
            if (saveResult.Response.ok && !string.IsNullOrWhiteSpace(saveResult.CaseId))
            {
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                    saveResult.CaseId,
                    saveResult.SerializedCase,
                    "PUT",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
            }

            return saveResult.Response;
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
            return new mmria.common.model.couchdb.document_put_response 
            { 
                error_description = ex.Message 
            };
        }
    }

    public sealed class SetOfflineStatusRequest
    {
        public string direction { get; set; } // "add" or "remove"
    }

    [HttpPost("toggle-offline/{caseId}")]
    public async Task<IActionResult> ToggleOfflineStatus(string caseId, [FromBody] SetOfflineStatusRequest request, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"ToggleOfflineStatus called for caseId: {caseId}, direction: {request?.direction}");
            
            // Validate direction parameter
            if (request == null || string.IsNullOrWhiteSpace(request.direction))
            {
                return BadRequest(new { success = false, message = "Direction parameter is required. Must be 'add' or 'remove'." });
            }

            var direction = request.direction.ToLowerInvariant();
            if (direction != "add" && direction != "remove")
            {
                return BadRequest(new { success = false, message = "Invalid direction parameter. Must be 'add' or 'remove'." });
            }

            bool targetOfflineState = direction == "add";
            Console.WriteLine($"Target offline state: {targetOfflineState}");
            
            // Get the current case document
            var case_response = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                db_config.url + $"/{db_config.prefix}mmrds/" + caseId,
                null,
                db_config.user_name,
                db_config.user_value
            );
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

            // Validate that we're not already in the target state
            if (currentOfflineState == targetOfflineState)
            {
                string message = targetOfflineState 
                    ? "Case is already marked for offline use" 
                    : "Case is already marked as online";
                Console.WriteLine($"State validation failed: {message}");
                return BadRequest(new { 
                    success = false, 
                    message = message,
                    is_offline = currentOfflineState,
                    already_in_state = true 
                });
            }

            // Set new offline state (use targetOfflineState instead of toggling)
            bool newOfflineState = targetOfflineState;
            case_document["is_offline"] = newOfflineState;
            
            if (newOfflineState)
            {
                // Adding to offline list (soft lock = 1)
                case_document["offline_date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                case_document["offline_by"] = User.Identity?.Name ?? "system";
                case_document["offline_lock_type"] = 1; // Soft lock
            }
            else
            {
                // Removing from offline list - clear all offline fields
                case_document["offline_date"] = null;
                case_document["offline_by"] = null;
                case_document["offline_lock_type"] = null;
            }

            // Update last_updated fields
            case_document["date_last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            case_document["last_updated_by"] = User.Identity?.Name ?? "system";

            Console.WriteLine($"New offline state: {newOfflineState}");

            // Save the updated document
            var json_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_document);
            Console.WriteLine($"Serialized document length: {json_string.Length}");
            
            var save_response = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                db_config.url + $"/{db_config.prefix}mmrds/" + caseId,
                json_string,
                db_config.user_name,
                db_config.user_value
            );
            Console.WriteLine($"Save response: {save_response}");
            
            if (string.IsNullOrEmpty(save_response))
            {
                return StatusCode(500, new { success = false, message = "Empty response from database" });
            }

            var save_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response);

            if (save_result != null && save_result.ok)
            {
                      var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
            (
                caseId,
                json_string,
                "PUT",
                configuration.GetString("metadata_version", host_prefix)
            );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
            
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

    [Authorize(Roles = "abstractor")]
    [HttpDelete]
    public async Task<System.Dynamic.ExpandoObject> Delete(string case_id = null, string rev = null) 
    { 
        try
        {

            var mmria_record_id = "";
            var first_name = "";
            var last_name = "";

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }

            string request_string = null;
            //mmria.server.utils.c_sync_document sync_document = null;

            if (!string.IsNullOrWhiteSpace (case_id) && !string.IsNullOrWhiteSpace (rev)) 
            {
                request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}?rev={rev}");
            }
            else 
            {
                return null;
            }

            string document_json = null;
            try 
            {
                document_json = await _couchDbHttpClient.ExecuteAsync(
                    "GET",
                    db_config.Get_Prefix_DB_Url($"mmrds/{case_id}"),
                    null,
                    db_config.user_name,
                    db_config.user_value
                );
                var check_docuement_curl_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_json);
                IDictionary<string, object> result_dictionary = check_docuement_curl_result as IDictionary<string, object>;
                
                if
                (
                    result_dictionary != null && 
                    !mmria.server.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.server.utils.ResourceRightEnum.WriteCase, check_docuement_curl_result)
                )
                {
                    Console.Write($"unauthorized DELETE {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                    return null;
                }
                
                if (result_dictionary.ContainsKey("_rev")) 
                {
                    request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}?rev={result_dictionary["_rev"]}");
                }

                if 
                (
                    result_dictionary.ContainsKey("home_record") &&
                    result_dictionary["home_record"] is IDictionary<string,object> home_record
                ) 
                {
                    if(home_record.ContainsKey("record_id"))
                    mmria_record_id = home_record["record_id"].ToString();

                    if(home_record.ContainsKey("first_name"))
                    first_name = home_record["first_name"].ToString();

                    if(home_record.ContainsKey("last_name"))
                    last_name = home_record["last_name"].ToString();
                }
            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Delete\n{ex}");
            }

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "DELETE",
                request_string,
                null,
                db_config.user_name,
                db_config.user_value
            );
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

            var audit_data = new mmria.common.model.couchdb.Change_Stack()
            {
                _id = System.Guid.NewGuid().ToString(),
                case_id = case_id,
                case_rev = rev,

                record_id = mmria_record_id,
                is_delete = true,
                delete_rev = rev,

                user_name = userName,
                first_name = first_name,
                last_name = last_name,

                note = "deleted case",

                metadata_version = configuration.GetString("metadata_version", host_prefix),
                date_created = DateTime.UtcNow,
            };

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
 

            var audit_string = Newtonsoft.Json.JsonConvert.SerializeObject(audit_data, settings);

            string audit_url = db_config.Get_Prefix_DB_Url($"audit/{audit_data._id}");

            try
            {
                string save_delete_audit_response = await _couchDbHttpClient.ExecuteAsync(
                    "PUT",
                    audit_url,
                    audit_string,
                    db_config.user_name,
                    db_config.user_value
                );
                var audit_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_delete_audit_response);
            }
            catch(Exception ex)
            {
                Console.Write("problem saving audit\n{0}", ex);

            }



            if(! string.IsNullOrWhiteSpace(document_json))
            {
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
                (
                    case_id,
                    document_json,
                    "DELETE",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
          
            }
            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }



} 


#endif
