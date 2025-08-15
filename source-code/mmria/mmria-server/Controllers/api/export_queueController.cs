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

    public export_queueController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _actorSystem = actorSystem;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }


    [HttpGet]
    public async System.Threading.Tasks.Task<IEnumerable<export_queue_item>> Get() 
    { 
        List<export_queue_item> result = new List<export_queue_item>();

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
        }


        try
        {
            string request_string = db_config.Get_Prefix_DB_Url($"export_queue/_all_docs?include_docs=true");
            var export_queue_curl = new cURL ("GET", null, request_string, null, db_config.user_name, db_config.user_value);

            string responseFromServer = await export_queue_curl.executeAsync();

            IDictionary<string,object> response_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer) as IDictionary<string,object>; 


            IList<object> enumerable_rows = null;


            if(response_result != null && response_result.ContainsKey("rows"))
            {
                enumerable_rows = response_result["rows"] as IList<object>;
            }


            if(enumerable_rows != null)
            foreach(IDictionary<string,object> enumerable_item in enumerable_rows)
            {

                IDictionary<string,object> doc_item = enumerable_item["doc"] as IDictionary<string,object>;

                if(doc_item == null)
                {
                    continue;
                }

                export_queue_item item = new export_queue_item();
                try
                {
                    item._id = doc_item ["_id"].ToString ();
                    item._rev = doc_item ["_rev"].ToString ();
                    item._deleted = doc_item .ContainsKey("_deleted") ? doc_item["_deleted"] as bool?: null;
                    item.date_created = doc_item["date_created"] as DateTime?;
                    item.created_by = doc_item.ContainsKey("created_by") ? doc_item["created_by"] as string: null;
                    item.date_last_updated = doc_item["date_last_updated"] as DateTime?;
                    item.last_updated_by = doc_item.ContainsKey("last_updated_by")? doc_item["last_updated_by"] as string : null;
                    item.file_name = doc_item["file_name"] != null? doc_item["file_name"].ToString() : null;
                    item.export_type = doc_item["export_type"] != null? doc_item["export_type"].ToString() : null;
                    item.status = doc_item["status"] != null? doc_item["status"].ToString() : null;
                
                    if(userName.ToLowerInvariant() == item.created_by.ToLowerInvariant())
                    {
                        result.Add(item);
                    }
                    
                }
                catch(Exception)
                {
                    // do nothing for now
                }
            
            }

            return result;
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
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (queue_item, settings); 

            string export_queue_request_url = db_config.Get_Prefix_DB_Url("export_queue/" + queue_item._id);

            var export_queue_curl = new cURL ("PUT", null, export_queue_request_url, object_string, db_config.user_name, db_config.user_value);

            string responseFromServer = await export_queue_curl.executeAsync();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
        
            if
            (
                result.ok && 
                (
                    queue_item.status.StartsWith("In Queue...", StringComparison.OrdinalIgnoreCase) ||
                    queue_item.status.StartsWith ("Deleted", StringComparison.OrdinalIgnoreCase)
                )
            )
            {

                var juris_user_name = User.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value; 

                mmria.server.model.actor.ScheduleInfoMessage new_scheduleInfo = new mmria.server.model.actor.ScheduleInfoMessage
                (
                    configuration.GetString("cron_schedule", host_prefix),
                    db_config.url,
                    db_config.prefix,
                    db_config.user_name,
                    db_config.user_value,
                    configuration.GetString("export_directory", host_prefix),
                    juris_user_name,
                    configuration.GetString("metadata_version", host_prefix),
                    configuration.GetString("cdc_instance_pull_list", host_prefix)

                );

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.quartz.Process_Export_Queue>(db_config)).Tell(new_scheduleInfo);
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

} 


