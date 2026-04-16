using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using mmria.services.vitalsimport.Actors.VitalsImport;
using mmria.services.vitalsimport.Messages;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace mmria.services.vitalsimport.Controllers;

[Authorize]
[Route("api/[controller]/[action]")]
[ApiController]
public sealed class broadcastMessageController : Controller
{
     private mmria.common.couchdb.ConfigurationSet ConfigDB;
     private mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public broadcastMessageController
    (
        mmria.common.couchdb.ConfigurationSet _ConfigDB,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        ConfigDB = _ConfigDB;
        _couchDbHttpClient = couchDbHttpClient;
        ConfigDB = _ConfigDB;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> ReplicateMessage
    (
        [FromBody] mmria.common.metadata.BroadcastMessageList request
    )
    {
        var result = new mmria.common.model.couchdb.document_put_response()
        {
            ok = true
        };

        var task_list = new List<Task>();
        var exclusion_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Discard any client-supplied audit/identity fields. These must never be
        // trusted from the request body; they are populated server-side per tenant
        // in UpdateBroadcastMessge from the authenticated principal and the
        // existing document (for created_by/date_created on update).
        request._rev = null;
        request.created_by = null;
        request.date_created = null;
        request.last_updated_by = null;
        request.date_last_updated = null;

        var authenticated_user = User?.Identity?.Name;
        if(string.IsNullOrWhiteSpace(authenticated_user))
        {
            authenticated_user = "system";
        }

        if(ConfigDB.name_value.ContainsKey("exclude_from_broadcast_list"))
        {
            var array = ConfigDB.name_value["exclude_from_broadcast_list"].Split(",");
            foreach(var item in array)
            {
                if(!string.IsNullOrWhiteSpace(item))
                    exclusion_set.Add(item.Trim());
            }
        }

        var current_date = System.DateTime.Now;

        foreach(var config in ConfigDB.detail_list)
        {
            var prefix = config.Key.ToUpper();

            if(prefix == "VITAL_IMPORT") continue;

            if(exclusion_set.Contains(prefix)) continue;
            
            task_list.Add(UpdateBroadcastMessge(prefix, config.Value, request, authenticated_user, current_date));
        }

        await Task.WhenAll(task_list);

        return Ok(result);
    }

    async System.Threading.Tasks.Task UpdateBroadcastMessge
    (
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail,
        mmria.common.metadata.BroadcastMessageList request,
        string authenticated_user,
        System.DateTime current_date
    ) 
    { 
        string url = $"{p_config_detail.url}/{p_config_detail.prefix}metadata/broadcast-message-list";
        mmria.common.metadata.BroadcastMessageList existing = null;
        try
        {
            existing = await get_existing_document(url, p_config_detail);
        }
        catch(System.Exception)
        {
            //System.Console.WriteLine($"mmria.services.broadcastMessageController.UpdateBroadcastMessage error\n{url}");
        }

        try
        {
            // Per-tenant copy so audit fields from one tenant's existing doc do
            // not leak into other tenants' PUT payloads.
            var payload = new mmria.common.metadata.BroadcastMessageList
            {
                message_one = request.message_one,
                message_two = request.message_two,
                last_updated_by = authenticated_user,
                date_last_updated = current_date,
                created_by = existing?.created_by ?? authenticated_user,
                date_created = existing?.date_created ?? current_date,
                _rev = existing?._rev
            };

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(payload, settings);

            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(await _couchDbHttpClient.ExecuteAsync("PUT", url, object_string, null, null));
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        catch(System.Exception)
        {

        }
    }

    async System.Threading.Tasks.Task<mmria.common.metadata.BroadcastMessageList> get_existing_document
    (
        string p_document_url,
        mmria.common.couchdb.DBConfigurationDetail config
    )
    {
        try
        {
            var temp_document_json = await _couchDbHttpClient.ExecuteAsync("GET", p_document_url, null, config.user_name, config.user_value);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.BroadcastMessageList>(temp_document_json);
        }
        catch(Exception ex)
        {
            if (!(ex.Message.IndexOf ("404") > -1))
            {
                //System.Console.WriteLine ("c_sync_document.get_existing_document");
                //System.Console.WriteLine (ex);
            }
        }

        return null;
    }
}
