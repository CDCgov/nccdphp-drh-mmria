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
namespace mmria.server;
	
[Route("api/[controller]")]
public sealed class vital_importController: ControllerBase 
{ 


    private ActorSystem _actorSystem;


    private readonly IAuthorizationService _authorizationService;
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.VitalImport.Manager.VitalImportManager _vitalImportManager;
    public vital_importController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.VitalImport.Manager.VitalImportManager vitalImportManager
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _vitalImportManager = vitalImportManager;
        _actorSystem = actorSystem;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }

    private bool is_authorized()
    {
        var result = false;

        var vital_service_key = configuration.GetString("vital_service_key", host_prefix);
        
        if
        (
            (
                !this.Request.Headers.ContainsKey("vitals_service_key") ||
                string.IsNullOrWhiteSpace(vital_service_key)
            ) &&
            this.Request.Headers["vitals_service_key"] != vital_service_key
        )
        {
            result = false;
        }
        else
        {
            result = true;
        }

        return result;
    }
    
    [AllowAnonymous]
    [HttpGet("view")]
    public async Task<mmria.common.model.couchdb.case_view_response> GetCaseView
    (

        string search_key

    )
    {
        if ( !is_authorized() )
        {
            return null;
        }

        try
        {
            return await _vitalImportManager.GetCaseViewAsync(search_key, db_config);
            
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        }


        return null;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<System.Dynamic.ExpandoObject> Get(string case_id) 
    { 
        if ( !is_authorized() )
        {
            return null;
        }

        try
        {
            return await _vitalImportManager.GetCaseAsync(case_id, User, db_config);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 




    [AllowAnonymous]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] System.Dynamic.ExpandoObject case_post_request
    ) 
    { 

        if ( !is_authorized() )
        {
            return null;
        }

        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();


        try
        {

            var saveResult = await _vitalImportManager.SaveCaseAsync(case_post_request, User, db_config);
            result = saveResult.Response;

            var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
            (
                saveResult.Id,
                saveResult.SerializedDocument,
                "PUT",
                configuration.GetString("metadata_version", host_prefix)
            );

            _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
    
            /*
            var case_sync_actor = _actorSystem.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
            case_sync_actor.Tell(Sync_Document_Message);
            */
            if (!result.ok)
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;

    } 

    [AllowAnonymous]
    [HttpDelete]
    public async Task<System.Dynamic.ExpandoObject> Delete(string case_id = null, string rev = null) 
    { 

        if ( !is_authorized() )
        {
            return null;
        }

        try
        {

            var deleteResult = await _vitalImportManager.DeleteCaseAsync(case_id, rev, User, db_config);
            if(deleteResult == null)
            {
                return null;
            }

            if(! string.IsNullOrWhiteSpace(deleteResult.DocumentJson))
            {
                var Sync_Document_Message = new mmria.server.model.actor.Sync_Document_Message
                (
                    deleteResult.CaseId,
                    deleteResult.DocumentJson,
                    "DELETE",
                    configuration.GetString("metadata_version", host_prefix)
                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(db_config, _couchDbHttpClient, configuration, host_prefix)).Tell(Sync_Document_Message);
                /*
                var case_sync_actor = _actorSystem.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
                case_sync_actor.Tell(Sync_Document_Message);
                */

            }
            return deleteResult.Response;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

} 


#endif
