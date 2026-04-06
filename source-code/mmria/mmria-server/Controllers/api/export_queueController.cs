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
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager _exportQueueManager;

    public export_queueController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager exportQueueManager
    )
    {
        _actorSystem = actorSystem;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post()
    {
        //bool valid_login = false;
        //mmria.common.data.api.Set_Queue_Request queue_request = null;
        var queue_item = await mmria.server.util.JsonRequestBodyReader.ReadAsync<export_queue_item>(Request);

        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
        
        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
        }

        var safeQueueItem = await CreateSanitizedQueueItemAsync(queue_item, userName);
        if (safeQueueItem == null)
        {
            return result;
        }

        var is_match = System.Text.RegularExpressions.Regex.IsMatch
        (
            safeQueueItem._id, 
            @"^\d\d\d\d-\d\d-\d\dT\d\d-\d\d-\d\d.\d\d\dZ.zip$"
        );

        

        if(
            ! is_match  ||
            safeQueueItem == null
        )
        {

            return result;
        }

        //if(queue_request.case_list.Length == 1)
        try
        {
            var sharedItem = MapToSharedModel(safeQueueItem);
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
                    System.Console.WriteLine($"Export queue processing delegated to mmria.services: {safeQueueItem._id}");
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

    private async System.Threading.Tasks.Task<export_queue_item> CreateSanitizedQueueItemAsync(export_queue_item request, string userName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        mmria.common.SharedLibraries.ExportQueue.Model.ExportQueueItem existingItem = null;
        try
        {
            existingItem = await _exportQueueManager.GetQueueItemAsync(request._id.Trim(), db_config);
        }
        catch
        {
            // Missing queue items are treated as creates.
        }

        var safeQueueItem = existingItem != null ? MapToServerModel(existingItem) : new export_queue_item();
        safeQueueItem._id = request._id.Trim();
        safeQueueItem._rev = !string.IsNullOrWhiteSpace(request._rev) ? request._rev : existingItem?._rev;
        safeQueueItem.data_type = "export";
        safeQueueItem._deleted = request._deleted;
        safeQueueItem.date_created = existingItem?.date_created ?? DateTime.UtcNow;
        safeQueueItem.created_by = !string.IsNullOrWhiteSpace(existingItem?.created_by) ? existingItem.created_by : userName;
        safeQueueItem.date_last_updated = DateTime.UtcNow;
        safeQueueItem.last_updated_by = userName;
        safeQueueItem.file_name = NormalizeOptionalString(request.file_name) ?? safeQueueItem.file_name ?? safeQueueItem._id;
        safeQueueItem.export_type = NormalizeOptionalString(request.export_type) ?? safeQueueItem.export_type;
        safeQueueItem.status = NormalizeOptionalString(request.status) ?? safeQueueItem.status;
        safeQueueItem.all_or_core = NormalizeOptionalString(request.all_or_core);
        safeQueueItem.grantee_name = NormalizeOptionalString(request.grantee_name);
        safeQueueItem.is_encrypted = NormalizeOptionalString(request.is_encrypted);
        safeQueueItem.zip_key = NormalizeOptionalString(request.zip_key);
        safeQueueItem.de_identified_selection_type = NormalizeOptionalString(request.de_identified_selection_type);
        safeQueueItem.de_identified_field_set = request.de_identified_field_set != null
            ? CloneTrimmedStringArray(request.de_identified_field_set)
            : safeQueueItem.de_identified_field_set;
        safeQueueItem.case_filter_type = NormalizeOptionalString(request.case_filter_type);
        safeQueueItem.case_file_type = NormalizeOptionalString(request.case_file_type);
        safeQueueItem.case_set = request.case_set != null ? CloneTrimmedStringArray(request.case_set) : safeQueueItem.case_set;
        safeQueueItem.ExportType = request.ExportType;
        safeQueueItem.field_set = request.field_set != null ? CloneTrimmedStringArray(request.field_set) : safeQueueItem.field_set;
        safeQueueItem.pregnancy_relatedness = request.pregnancy_relatedness != null
            ? (int[])request.pregnancy_relatedness.Clone()
            : safeQueueItem.pregnancy_relatedness;
        safeQueueItem.include_blank_date_of_reviews = request.include_blank_date_of_reviews;
        safeQueueItem.include_blank_date_of_deaths = request.include_blank_date_of_deaths;
        safeQueueItem.date_of_review_begin = request.date_of_review_begin;
        safeQueueItem.date_of_review_end = request.date_of_review_end;
        safeQueueItem.date_of_death_begin = request.date_of_death_begin;
        safeQueueItem.date_of_death_end = request.date_of_death_end;

        return safeQueueItem;
    }

    private static string NormalizeOptionalString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string[] CloneTrimmedStringArray(string[] source)
    {
        return source?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToArray();
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


