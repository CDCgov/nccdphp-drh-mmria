using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server;

[Route("api/[controller]")]
public sealed class queueController: ControllerBase
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public queueController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.data.api.Set_Queue_Response> Post(mmria.common.data.api.Set_Queue_Request set_queue_request)
    { 
        mmria.common.data.api.Set_Queue_Response result = new mmria.common.data.api.Set_Queue_Response();

        mmria.common.data.api.Queue_Item queue_item = new mmria.common.data.api.Queue_Item ();
        queue_item.queue_id = System.Guid.NewGuid ().ToString ();
        queue_item.case_list = set_queue_request.case_list;

        string queue_url = db_config.url + "/queue/"  + queue_item.queue_id;

        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(queue_item);

        System.Net.WebRequest request = System.Net.WebRequest.Create(new System.Uri(queue_url));
        request.Method = "PUT";
        request.ContentType = "application/json";
        request.ContentLength = object_string.Length;
        request.PreAuthenticate = false;

        if(!string.IsNullOrWhiteSpace(set_queue_request.security_token))
        {
            request.Headers.Add("Cookie", "AuthSession=" + set_queue_request.security_token);
            request.Headers.Add("X-CouchDB-WWW-Authenticate", set_queue_request.security_token);
        }
        else if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
        {
            string auth_session_value = this.Request.Cookies["AuthSession"];
            request.Headers.Add("Cookie", "AuthSession=" + auth_session_value);
            request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
        }

        mmria.common.model.couchdb.document_put_response put_response = null;

        using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(request.GetRequestStream()))
        {
            try
            {
                streamWriter.Write(object_string);
                streamWriter.Flush();
                streamWriter.Close();


                System.Net.WebResponse response = await request.GetResponseAsync();
                System.IO.Stream dataStream = response.GetResponseStream ();
                System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
                string responseFromServer = reader.ReadToEnd ();

                put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.WriteLine (ex);
                result.Ok = false;
                result.message = ex.ToString ();
            }
        }

        //if(put_response.


        return result;
    }
}


