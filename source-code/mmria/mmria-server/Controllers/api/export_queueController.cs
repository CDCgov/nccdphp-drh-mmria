using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Dynamic;
using mmria.common.model;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/[controller]")]
public sealed class export_queueController: ControllerBase
{ 
    ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager _exportQueueManager;

    public export_queueController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager exportQueueManager
    )
    {
        _actorSystem = actorSystem;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
        _couchDbHttpClient = couchDbHttpClient;
        _exportQueueManager = exportQueueManager;
    }


    [HttpGet]
    public async System.Threading.Tasks.Task<IEnumerable<export_queue_item>> Get() 
    { 
        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
        }

        try
        {
            var result = await _exportQueueManager.GetQueueItemsForUserAsync(userName, db_config);
            return result.Select(MapToServerModel).ToList();
        }
        catch(Exception)
        {
            //Console.WriteLine (ex);
        } 

        return null;
    } 



    // POST api/values 
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post([FromBody] export_queue_item queue_item) 
    { 
        //bool valid_login = false;
        //mmria.common.data.api.Set_Queue_Request queue_request = null;

        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
        
        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
        }

        var is_match = System.Text.RegularExpressions.Regex.IsMatch
        (
            queue_item._id, 
            @"^\d\d\d\d-\d\d-\d\dT\d\d-\d\d-\d\d.\d\d\dZ.zip$"
        );

        

        if(
            ! is_match  ||
            queue_item == null
        )
        {

            return result;
        }


        if(string.IsNullOrWhiteSpace(queue_item.created_by))
        {
            queue_item.created_by = userName;
        } 

        
        queue_item.last_updated_by = userName;

        //if(queue_request.case_list.Length == 1)
        try
        {
            var sharedItem = MapToSharedModel(queue_item);
            result = await _exportQueueManager.SaveQueueItemAsync(sharedItem, userName, db_config);
        
            if(_exportQueueManager.ShouldTriggerService(sharedItem, result))
            {
                var juris_user_name = User.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value; 

                // Call mmria.services to process export queue
                try
                {                    
                    await _exportQueueManager.TriggerExportQueueServiceAsync(
                        sharedItem,
                        juris_user_name,
                        host_prefix,
                        configuration.GetString("vitals_url", host_prefix),
                        configuration.GetString("vital_service_key", host_prefix)
                    );
                    System.Console.WriteLine($"Export queue processing delegated to mmria.services: {queue_item._id}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error calling mmria.services for export queue: {ex.Message}");
                    // Don't fail the request - export will remain in queue and can be retried
                }
            }
            else // if (!result.ok) 
            {

            }

        }
        catch(Exception) 
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            //Console.WriteLine (ex);
        }

        return result;

    } 

    private static mmria.common.SharedLibraries.ExportQueue.Model.ExportQueueItem MapToSharedModel(export_queue_item item)
    {
        return new mmria.common.SharedLibraries.ExportQueue.Model.ExportQueueItem
        {
            _id = item._id,
            _rev = item._rev,
            data_type = item.data_type,
            _deleted = item._deleted,
            date_created = item.date_created,
            created_by = item.created_by,
            date_last_updated = item.date_last_updated,
            last_updated_by = item.last_updated_by,
            file_name = item.file_name,
            export_type = item.export_type,
            status = item.status,
            all_or_core = item.all_or_core,
            grantee_name = item.grantee_name,
            is_encrypted = item.is_encrypted,
            zip_key = item.zip_key,
            de_identified_selection_type = item.de_identified_selection_type,
            de_identified_field_set = item.de_identified_field_set,
            case_filter_type = item.case_filter_type,
            case_file_type = item.case_file_type,
            case_set = item.case_set,
            ExportType = (mmria.common.SharedLibraries.ExportQueue.Model.ExportQueueItem.ExportTypeEnum)item.ExportType,
            field_set = item.field_set,
            pregnancy_relatedness = item.pregnancy_relatedness,
            include_blank_date_of_reviews = item.include_blank_date_of_reviews,
            include_blank_date_of_deaths = item.include_blank_date_of_deaths,
            date_of_review_begin = item.date_of_review_begin,
            date_of_review_end = item.date_of_review_end,
            date_of_death_begin = item.date_of_death_begin,
            date_of_death_end = item.date_of_death_end
        };
    }

    private static export_queue_item MapToServerModel(mmria.common.SharedLibraries.ExportQueue.Model.ExportQueueItem item)
    {
        return new export_queue_item
        {
            _id = item._id,
            _rev = item._rev,
            data_type = item.data_type,
            _deleted = item._deleted,
            date_created = item.date_created,
            created_by = item.created_by,
            date_last_updated = item.date_last_updated,
            last_updated_by = item.last_updated_by,
            file_name = item.file_name,
            export_type = item.export_type,
            status = item.status,
            all_or_core = item.all_or_core,
            grantee_name = item.grantee_name,
            is_encrypted = item.is_encrypted,
            zip_key = item.zip_key,
            de_identified_selection_type = item.de_identified_selection_type,
            de_identified_field_set = item.de_identified_field_set,
            case_filter_type = item.case_filter_type,
            case_file_type = item.case_file_type,
            case_set = item.case_set,
            ExportType = (export_queue_item.ExportTypeEnum)item.ExportType,
            field_set = item.field_set,
            pregnancy_relatedness = item.pregnancy_relatedness,
            include_blank_date_of_reviews = item.include_blank_date_of_reviews,
            include_blank_date_of_deaths = item.include_blank_date_of_deaths,
            date_of_review_begin = item.date_of_review_begin,
            date_of_review_end = item.date_of_review_end,
            date_of_death_begin = item.date_of_death_begin,
            date_of_death_end = item.date_of_death_end
        };
    }

} 


