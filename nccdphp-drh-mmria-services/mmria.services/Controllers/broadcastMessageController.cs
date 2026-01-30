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

    public broadcastMessageController
    (
        mmria.common.couchdb.ConfigurationSet _ConfigDB
    )
    {
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

        request._rev = null;

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
            
            task_list.Add(UpdateBroadcastMessge(prefix, config.Value, request));
        }

        await Task.WhenAll(task_list);

        return Ok(result);
    }

    async System.Threading.Tasks.Task UpdateBroadcastMessge
    (
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail,
        mmria.common.metadata.BroadcastMessageList request
    ) 
    { 
        string url = $"{p_config_detail.url}/{p_config_detail.prefix}metadata/broadcast-message-list";
        string revision = null;
        try
        {
            revision = await get_revision(url, p_config_detail);
        }
        catch(System.Exception)
        {
            //System.Console.WriteLine($"mmria.services.broadcastMessageController.UpdateBroadcastMessage error\n{url}");
        }

        try
        {
            if(!string.IsNullOrWhiteSpace(revision))
            {
                request._rev = revision;
            }

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(request, settings);

            var curl = new mmria.getset.cURL("PUT", null, url, object_string, null, null);
        
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(await curl.executeAsync());
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

    async System.Threading.Tasks.Task<string> get_revision
    (
        string p_document_url,
        mmria.common.couchdb.DBConfigurationDetail config
    )
    {
        string result = null;

        var document_curl = new mmria.getset.cURL("GET", null, p_document_url, null, config.user_name, config.user_value);
        string temp_document_json = null;

        try
        {
            
            temp_document_json = await document_curl.executeAsync();
            var request_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.BroadcastMessageList>(temp_document_json);
            result = request_result._rev;
        }
        catch(Exception ex) 
        {
            if (!(ex.Message.IndexOf ("404") > -1)) 
            {
                //System.Console.WriteLine ("c_sync_document.get_revision");
                //System.Console.WriteLine (ex);
            }
        }

        return result;
    }
}
