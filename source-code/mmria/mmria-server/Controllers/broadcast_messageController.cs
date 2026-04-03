using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.IO;
using Akka.Actor;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
using mmria.server.util;
namespace mmria.server.Controllers;


[Route("broadcast-message/{action=Index}")]
public sealed class broadcast_messageController : Controller
{

    private readonly IConfiguration _configuration;
    mmria.common.couchdb.ConfigurationSet ConfigDB;
   
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public broadcast_messageController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        ConfigDB = tenantRuntime.RequireConfigurationSet();
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Authorize]
    public IActionResult Index()
    {
        return View();
    }


    [Authorize]
    [HttpGet]
    public async Task<mmria.common.metadata.BroadcastMessageList> GetBroadcastMessageList()
    {
        return await LoadBroadcastMessageListAsync();
    }

    [Authorize(Roles  = "cdc_admin")]
    [HttpPost]
    public async Task<JsonResult> SaveBroadcastMessageDraft
    (
        [FromBody] mmria.common.metadata.BroadcastMessageList request
    )
    {
        var result = new mmria.common.model.couchdb.document_put_response();

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        var existingRequest = await LoadBroadcastMessageListAsync();
        var sanitizedRequest = CreateSanitizedBroadcastMessageList(request, existingRequest, userName);
        result = await save_request(sanitizedRequest);

        return EscapedJsonResultFactory.Create(result);
    }

    [Authorize(Roles  = "cdc_admin")]
    [HttpPost]
    public async Task<JsonResult> UnpublishBroadcastMessage
    (
        [FromBody] mmria.common.metadata.BroadcastMessageList request
    )
    {
        var result = new mmria.common.model.couchdb.document_put_response();

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        var existingRequest = await LoadBroadcastMessageListAsync();
        var sanitizedRequest = CreateSanitizedBroadcastMessageList(request, existingRequest, userName);
        result = await save_request(sanitizedRequest, true);

        return EscapedJsonResultFactory.Create(result);
    }

    [Authorize(Roles  = "cdc_admin")]
    [HttpPost]
    public async Task<JsonResult> PublishBroadcastMessage
    (
        [FromBody] mmria.common.metadata.BroadcastMessageList request
    )
    {
        var result = new mmria.common.model.couchdb.document_put_response();

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        var existingRequest = await LoadBroadcastMessageListAsync();
        var sanitizedRequest = CreateSanitizedBroadcastMessageList(request, existingRequest, userName);
        result = await save_request(sanitizedRequest, true);



        return EscapedJsonResultFactory.Create(result);
    }

    async Task<mmria.common.model.couchdb.document_put_response> save_request(mmria.common.metadata.BroadcastMessageList request, bool send_replication = false)
    {
        var result = new mmria.common.model.couchdb.document_put_response();

        string url = $"{db_config.url}/metadata/broadcast-message-list";
        
        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(request, settings);

        try
        {
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(await _couchDbHttpClient.ExecuteAsync("PUT", url, object_string, null, null));
        }
        catch(Exception ex)
        {

            Console.WriteLine(ex);
        }

        if(send_replication)         
        await replicate(object_string);

        return result;
    }

    async Task replicate(string object_json)
    {
        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");

        var base_url = $"{config_url}/api/broadcastMessage/ReplicateMessage";

        var requestOptions = new mmria.common.getset.CouchDbRequestOptions
        {
            VitalServiceKey = ConfigDB.name_value["vital_service_key"]
        };

        try
        {
            var responseContent = await _couchDbHttpClient.ExecuteAsync("POST", base_url, object_json, "application/json", requestOptions);

           var response = System.Text.Json.JsonSerializer.Deserialize<mmria.common.model.couchdb.document_put_response>(responseContent);
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
        }
    }

    private async Task<mmria.common.metadata.BroadcastMessageList> LoadBroadcastMessageListAsync()
    {
        var result = new mmria.common.metadata.BroadcastMessageList();
        string url = $"{db_config.url}/metadata/broadcast-message-list";
        
        try
        {
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.BroadcastMessageList>(
                await _couchDbHttpClient.ExecuteAsync("GET", url, null, null, null))
                ?? new mmria.common.metadata.BroadcastMessageList();
        }
        catch(System.Net.WebException ex)
        {
            if(ex.Message.IndexOf("404") > -1)
            {
                result.created_by = "system";
                result.date_created = DateTime.UtcNow;

                result.last_updated_by = "system";
                result.date_last_updated = DateTime.UtcNow;
            }
            else
            {
             Console.WriteLine(ex);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    private static mmria.common.metadata.BroadcastMessageList CreateSanitizedBroadcastMessageList(
        mmria.common.metadata.BroadcastMessageList request,
        mmria.common.metadata.BroadcastMessageList existing,
        string userName)
    {
        existing ??= new mmria.common.metadata.BroadcastMessageList();
        request ??= new mmria.common.metadata.BroadcastMessageList();

        return new mmria.common.metadata.BroadcastMessageList
        {
            _rev = string.IsNullOrWhiteSpace(existing._rev) ? request._rev : existing._rev,
            date_created = existing.date_created ?? request.date_created ?? DateTime.UtcNow,
            created_by = !string.IsNullOrWhiteSpace(existing.created_by) ? existing.created_by : (request.created_by ?? userName),
            date_last_updated = DateTime.UtcNow,
            last_updated_by = userName,
            data_type = string.IsNullOrWhiteSpace(existing.data_type) ? request.data_type : existing.data_type,
            message_one = CreateSanitizedBroadcastMessage(request.message_one, existing.message_one),
            message_two = CreateSanitizedBroadcastMessage(request.message_two, existing.message_two)
        };
    }

    private static mmria.common.metadata.BroadcastMessage CreateSanitizedBroadcastMessage(
        mmria.common.metadata.BroadcastMessage request,
        mmria.common.metadata.BroadcastMessage existing)
    {
        request ??= new mmria.common.metadata.BroadcastMessage();
        existing ??= new mmria.common.metadata.BroadcastMessage();

        return new mmria.common.metadata.BroadcastMessage
        {
            draft = CreateSanitizedBroadcastMessageItem(request.draft, existing.draft),
            published = CreateSanitizedBroadcastMessageItem(request.published, existing.published),
            publish_status = request.publish_status
        };
    }

    private static mmria.common.metadata.BroadcastMessageItem CreateSanitizedBroadcastMessageItem(
        mmria.common.metadata.BroadcastMessageItem request,
        mmria.common.metadata.BroadcastMessageItem existing)
    {
        request ??= new mmria.common.metadata.BroadcastMessageItem();
        existing ??= new mmria.common.metadata.BroadcastMessageItem();

        return new mmria.common.metadata.BroadcastMessageItem
        {
            title = request.title ?? existing.title ?? string.Empty,
            body = request.body ?? existing.body ?? string.Empty,
            type = request.type ?? existing.type ?? "information"
        };
    }
    
}
