using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  
namespace mmria.pmss.server;

[Route("api/[controller]")]
public sealed class checkcodeController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public checkcodeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }

    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<string> Get()
    {
        System.Console.WriteLine ("Recieved message.");
        string result = null;

        try
        {
            //"2016-06-12T13:49:24.759Z"
            string request_string = db_config.url + $"/metadata/2016-06-12T13:49:24.759Z/mmria-check-code.js";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "GET";
            request.PreAuthenticate = false;

            /*
            if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
            {
                string auth_session_value = this.Request.Cookies["AuthSession"];
                request.Headers.Add("Cookie", "AuthSession=" + auth_session_value);
                request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
            }
            */

            System.Net.WebResponse response = (System.Net.HttpWebResponse) await request.GetResponseAsync();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = await reader.ReadToEndAsync ();

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }


    public class PutCheckCodeRequest
    {
        public PutCheckCodeRequest(){}

        public string data { get; set; }
    }

    [Authorize(Roles  = "form_designer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Put
    (
        [FromBody] PutCheckCodeRequest CheckCodeRequest
    ) 
    { 
        //string check_code_json;
        string check_code_json = CheckCodeRequest.data;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

            try
            {
                /*

                System.IO.Stream dataStream0 = this.Request.Body;
                // Open the stream using a StreamReader for easy access.
                //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);
                // Read the content.
                check_code_json = await reader0.ReadToEndAsync ();
                */

                string metadata_url = db_config.url + "/metadata/2016-06-12T13:49:24.759Z/mmria-check-code.js";

                var put_curl = new mmria.server.cURL("PUT", null, metadata_url, check_code_json,db_config.user_name, db_config.user_value, "text/*");

                var revision = await get_revision(db_config.url + "/metadata/2016-06-12T13:49:24.759Z");

                if (!string.IsNullOrWhiteSpace(revision))
                {

                    //System.Text.RegularExpressions.Regex rgx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9 -]");
                    //string If_Match = rgx.Replace(this.Request.Headers["If-Match"], "");
                        
                    put_curl.AddHeader("If-Match",  revision);
                }

                string responseFromServer = await put_curl.executeAsync();

                Console.Write("checkCodeController.Put");
                Console.Write(responseFromServer);

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

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

    private async System.Threading.Tasks.Task<string> get_revision(string p_document_url)
    {

        string result = null;

        var document_curl = new mmria.server.cURL("GET", null, p_document_url, null,db_config.user_name, db_config.user_value);
        string temp_document_json = null;

        try
        {
            
            temp_document_json = await document_curl.executeAsync();
            var request_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(temp_document_json);
            IDictionary<string, object> updater = request_result as IDictionary<string, object>;
            if(updater != null && updater.ContainsKey("_rev"))
            {
                result = updater ["_rev"].ToString ();
            }
        }
        catch(Exception ex) 
        {
            if (!(ex.Message.IndexOf ("(404) Object Not Found") > -1)) 
            {
                //System.Console.WriteLine ("c_sync_document.get_revision");
                //System.Console.WriteLine (ex);
            }
        }

        return result;
    }

} 


