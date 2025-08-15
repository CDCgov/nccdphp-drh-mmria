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
namespace mmria.server.Controllers;


[Route("broadcast-message/{action=Index}")]
public sealed class broadcast_messageController : Controller
{

    private readonly IConfiguration _configuration;
    mmria.common.couchdb.ConfigurationSet ConfigDB;
   
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public broadcast_messageController
    (
        mmria.common.couchdb.ConfigurationSet p_config_db,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        ConfigDB = p_config_db;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
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
        var result = new mmria.common.metadata.BroadcastMessageList();


        string url = $"{db_config.url}/metadata/broadcast-message-list";
        
        cURL curl = new cURL("GET", null, url, null, null, null);
        try
        {
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.BroadcastMessageList>(await curl.executeAsync());
        }
        catch(System.Net.WebException ex)
        {
            if(ex.Message.IndexOf("404") > -1)
            {
                // do nothing
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

        request.last_updated_by = userName;
        request.date_last_updated = DateTime.UtcNow;

        result = await save_request(request);

        return Json(result);
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

        request.last_updated_by = userName;
        request.date_last_updated = DateTime.UtcNow;

        result = await save_request(request, true);

        return Json(result);
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

        request.last_updated_by = userName;
        request.date_last_updated = DateTime.UtcNow;

        result = await save_request(request, true);



        return Json(result);
    }

    async Task<mmria.common.model.couchdb.document_put_response> save_request(mmria.common.metadata.BroadcastMessageList request, bool send_replication = false)
    {
        var result = new mmria.common.model.couchdb.document_put_response();

        string url = $"{db_config.url}/metadata/broadcast-message-list";
        
        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(request, settings);


        cURL curl = new cURL("PUT", null, url, object_string, null, null);
       

        try
        {
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(await curl.executeAsync());
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


        var curl = new mmria.server.cURL("POST", null, base_url, object_json);
        curl.AddHeader("vital-service-key", ConfigDB.name_value["vital_service_key"]);

        try
        {
            var responseContent = await curl.executeAsync();

           var response = System.Text.Json.JsonSerializer.Deserialize<mmria.common.model.couchdb.document_put_response>(responseContent);
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
        }
    }
    
}