#if IS_PMSS_ENHANCED
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using mmria.common.model;
using Akka.Actor;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  
namespace mmria.pmss.server;

[Authorize]
[Route("api/[controller]")]
public sealed class pmss_csv_importController: ControllerBase 
{ 
    ActorSystem actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public pmss_csv_importController
    (
        ActorSystem _actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        actorSystem = _actorSystem;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
    
    [Authorize(Roles  = "abstractor,jurisdiction_admin,data_analyst,vital_importer")]
    [HttpGet]
    public async Task<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>> Get(string case_id) 
    { 
        mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch> result = null;

        try
        {
            common.couchdb.DBConfigurationDetail config = configuration.GetDBConfig("vital_import");
            
            string url = $"{db_config.url}/vital_import/_all_docs?include_docs=true";


            var user_curl = new mmria.server.cURL("GET", null, url, null, db_config.user_name, db_config.user_value);

            var responseFromServer = await user_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>>(responseFromServer);

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }


        return result;
    }

    [Authorize(Roles  = "vital_importer")]
    [HttpDelete]
    public async Task<bool> Delete() 
    { 
        var  result = true;

        return result;
        /*
        var  batch_list = new List<mmria.common.ije.Batch>();

        string url = $"{db_config.url}/vital_import/_all_docs?include_docs=true";
        var document_curl = new mmria.getset.cURL ("GET", null, url, null, db_config.user_name, db_config.user_value);
        try
        {
            var responseFromServer = await document_curl.executeAsync();
            var alldocs = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>>(responseFromServer);

            foreach(var item in alldocs.rows)
            {
                batch_list.Add(item.doc);
            }
            
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        foreach(var item in batch_list)
        {
            var message = new mmria.common.ije.BatchRemoveDataMessage()
            {
                id = item.id,
                date_of_removal = DateTime.Now
            };

            var bsr = actorSystem.ActorSelection("user/batch-supervisor");
            bsr.Tell(message);
        }
        return result;
        */
    }

    [Authorize(Roles  = "vital_importer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.ije.NewIJESet_MessageResponse> Post([FromBody] mmria.common.ije.NewIJESet_MessageDTO ijeset) 
    { 
        var processor = actorSystem.ActorSelection("user/batch-supervisor");

        var NewIJESet_Message = new mmria.common.ije.NewIJESet_Message()
        {
            batch_id = System.Guid.NewGuid().ToString(),
            mor = ijeset.mor,
            nat = ijeset.nat,
            fet = ijeset.fet,
            mor_file_name = ijeset.mor_file_name,
            nat_file_name = ijeset.nat_file_name,
            fet_file_name = ijeset.fet_file_name
        };

        var result = new mmria.common.ije.NewIJESet_MessageResponse()
        {
            batch_id = NewIJESet_Message.batch_id,
            ok = true
        };

        //processor.Tell(NewIJESet_Message);

        var response = await processor.Ask(NewIJESet_Message) as string;


        return result;


/*        
        string object_string = null;
        mmria.pmss.server.model.NewIJESet_MessageResponse result = new ();

        try
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(ijeset, settings);

                //var localUrl = "https://localhost:44331/api/Message/IJESet";
                //var message_curl = new mmria.pmss.server.cURL("POST", null, localUrl, message);
                //var messge_curl_result = await message_curl.executeAsync();

            string user_db_url = configuration.GetString("vitals_url",host_prefix);

            var user_curl = new cURL("PUT", null, user_db_url, object_string);
            user_curl.AddHeader("vital-service-key", configuration.GetString("vital_service_key",host_prefix));
            var responseFromServer = await user_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.pmss.server.model.NewIJESet_MessageResponse>(responseFromServer);

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
            result.detail = ex.Message;
            
        }

        return result;
        */
    } 

} 


#endif