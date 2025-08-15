using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class metadataController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public metadataController
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
    public System.Dynamic.ExpandoObject Get()
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;
        System.Dynamic.ExpandoObject json_result = null;
        try
        {

            //"2016-06-12T13:49:24.759Z"
            string request_string = $"{db_config.url}/metadata/2016-06-12T13:49:24.759Z";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));

            request.PreAuthenticate = false;

            System.Net.WebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = reader.ReadToEnd ();

            json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(result, new  Newtonsoft.Json.Converters.ExpandoObjectConverter());

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return json_result;
    }


    [AllowAnonymous] 
    [Route("{id}")]
    [HttpGet]
    public System.Dynamic.ExpandoObject Get(string id)
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;
        System.Dynamic.ExpandoObject json_result = null;
        try
        {
            string request_string =  $"{db_config.url}/metadata/{id}";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));

            request.PreAuthenticate = false;

            System.Net.WebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = reader.ReadToEnd ();

            json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(result, new  Newtonsoft.Json.Converters.ExpandoObjectConverter());

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return json_result;
    }


    [Authorize(Policy = "form_designer")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.app metadata
    ) 
    { 
        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(metadata, settings);

            string metadata_url = $"{db_config.url}/metadata/"  + metadata._id;

            var metadata_curl = new cURL("PUT", null, metadata_url, object_string, db_config.user_name, db_config.user_value);


            string responseFromServer = await metadata_curl.executeAsync();

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



    [HttpGet("GetCheckCode")]
    public string GetCheckCode()
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;

        try
        {
            string request_string = $"{db_config.url}/metadata/2016-06-12T13:49:24.759Z/mmria-check-code.js";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
                request.Method = "GET";
                request.PreAuthenticate = false;


            System.Net.WebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream dataStream = response.GetResponseStream ();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = reader.ReadToEnd ();

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }


    [Authorize(Roles  = "form_designer")]
    [HttpPost("PutCheckCode")]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> PutCheckCode
    (
        
    ) 
    { 
        string check_code_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

            System.IO.Stream dataStream0 = this.Request.Body;
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

            check_code_json = await reader0.ReadToEndAsync ();

            string metadata_url = $"{db_config.url}/metadata/2016-06-12T13:49:24.759Z/mmria-check-code.js";

            var metadata_curl = new cURL("PUT", null, metadata_url, check_code_json, db_config.user_name, db_config.user_value, "text/*");


            var revision = await get_revision(db_config.url + "/metadata/2016-06-12T13:49:24.759Z");
            if (!string.IsNullOrWhiteSpace(revision))
            {
                metadata_curl.AddHeader("If-Match",  revision);
            }

            string responseFromServer = await metadata_curl.executeAsync();

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

    [Authorize(Roles  = "form_designer")]
    [Route("{id}")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.Version_Specification p_version_specification
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        if
        (
            p_version_specification.data_type == null ||
            p_version_specification.data_type != "version-specification" || 
            p_version_specification._id == "2016-06-12T13:49:24.759Z" ||
            p_version_specification._id == "de-identified-list"

        )
        {
            return null;
        }


        try
        {

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings{
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    MissingMemberHandling =  Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            string json_string = Newtonsoft.Json.JsonConvert.SerializeObject(p_version_specification, settings);
            string metadata_url = $"{db_config.url}/metadata/{p_version_specification._id}";

            var metadata_curl = new cURL("PUT", null, metadata_url, json_string, db_config.user_name, db_config.user_value);

            string responseFromServer = await metadata_curl.executeAsync();

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

        var document_curl = new cURL("GET", null, p_document_url, null, db_config.user_name, db_config.user_value);
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


