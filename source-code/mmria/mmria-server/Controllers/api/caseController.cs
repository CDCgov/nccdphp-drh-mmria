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
    private readonly mmria.common.SharedLibraries.Case.Manager.CaseManager _caseManager;

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
        mmria.common.SharedLibraries.Case.Manager.CaseManager caseManager
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


    public sealed class Release_Lock_Request
    {
        public string case_id { get; set; }
        public string tab_id { get; set; }
    }


    [Authorize(Roles = "abstractor")]
    [HttpPost("release-lock")]
    public async Task<IActionResult> ReleaseLock([FromBody] Release_Lock_Request request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.case_id))
        {
            return BadRequest(new { message = "case_id is required" });
        }

        var releaseResult = await _caseManager.ReleaseCaseLockAsync(request.case_id, request.tab_id, db_config, User);

        if (!releaseResult.IsSuccessful)
        {
            return StatusCode(releaseResult.StatusCode, new { message = releaseResult.Message });
        }

        if (!string.IsNullOrWhiteSpace(releaseResult.CaseId) && !string.IsNullOrWhiteSpace(releaseResult.SerializedCase))
        {
            var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message(
                releaseResult.CaseId,
                releaseResult.SerializedCase,
                "PUT",
                configuration.GetString("metadata_version", host_prefix)
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
        }

        return Ok(new { ok = true });
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
            var deleteResult = await _caseManager.DeleteCaseAsync(case_id, rev, User, db_config);

            if (deleteResult.IsSuccessful)
            {
                // Dispatch sync message
                if (!string.IsNullOrWhiteSpace(deleteResult.DocumentJson))
                {
                    var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
                    (
                        deleteResult.CaseId,
                        deleteResult.DocumentJson,
                        "DELETE",
                        configuration.GetString("metadata_version", host_prefix)
                    );

                    _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
                }

                return deleteResult.Result;
            }
            else
            {
                return null;
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }



} 


#endif
