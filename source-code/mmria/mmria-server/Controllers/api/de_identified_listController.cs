using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Route("api/[controller]")]
public sealed class de_identified_listController: ControllerBase 
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public de_identified_listController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpGet]
    public System.Dynamic.ExpandoObject Get(string id) 
    { 
        try
        {

            string list_id = null;

            if(!string.IsNullOrWhiteSpace(id) && id.ToLower() == "export")
            {
                list_id = "de-identified-export-list";
            }
            else
            {
                list_id = "de-identified-list";
            }

            string request_string = $"{db_config.url}/metadata/{list_id}";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));

            request.PreAuthenticate = false;


            if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
            {
                string auth_session_value = this.Request.Cookies["AuthSession"];
                request.Headers.Add("Cookie", "AuthSession=" + auth_session_value);
                request.Headers.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
            }

            System.Net.WebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            string responseFromServer = reader.ReadToEnd ();

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

            return result;
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    } 

    [Authorize(Roles = "form_designer, cdc_admin")]
    [Route("{id?}")]
    [HttpPost]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post(string id) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        string list_id = null;

        if(!string.IsNullOrWhiteSpace(id) && id.ToLower() == "export")
        {
            list_id = "de-identified-export-list";
        }
        else
        {
            list_id = "de-identified-list";
        }

        try
        {

            System.IO.Stream dataStream0 = this.Request.Body;
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

            string document_json = await reader0.ReadToEndAsync ();

            string metadata_url = $"{db_config.url}/metadata/{list_id}";

            var de_identified_curl = new cURL("PUT", null, metadata_url, document_json, db_config.user_name, db_config.user_value,"text/*");

            string responseFromServer = await de_identified_curl.executeAsync ();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

} 


