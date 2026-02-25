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

            var toggleResult = await _caseManager.ToggleOfflineStatusAsync(
                caseId,
                request?.direction,
                User,
                db_config
            );

            if (toggleResult.IsSuccessful)
            {
                // Dispatch sync message
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                    toggleResult.CaseId,
                    toggleResult.SerializedCase,
                    "PUT",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);

                return Ok(new { success = true, is_offline = toggleResult.IsOffline, message = toggleResult.Message });
            }
            else
            {
                return toggleResult.StatusCode switch
                {
                    400 => BadRequest(new { success = false, message = toggleResult.ErrorMessage }),
                    404 => NotFound(new { success = false, message = toggleResult.ErrorMessage }),
                    500 => StatusCode(500, new { success = false, message = toggleResult.ErrorMessage }),
                    _ => BadRequest(new { success = false, message = toggleResult.ErrorMessage })
                };
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
