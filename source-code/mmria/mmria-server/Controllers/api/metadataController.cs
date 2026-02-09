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
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public metadataController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(
            _overridableConfigSets,
            _configuration,
            host_prefix
        );
        
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(
            _dbConfigSets,
            _configuration,
            host_prefix
        );
    }
    
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get()
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;
        System.Dynamic.ExpandoObject json_result = null;
        try
        {

            //"2016-06-12T13:49:24.759Z"
            string request_string = $"{db_config.url}/metadata/2016-06-12T13:49:24.759Z";

            result = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                null,
                null
            );

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
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get(string id)
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;
        System.Dynamic.ExpandoObject json_result = null;
        try
        {
            string request_string =  $"{db_config.url}/metadata/{id}";

            result = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                null,
                null
            );

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

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", metadata_url, object_string, db_config.user_name, db_config.user_value);

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
    public async System.Threading.Tasks.Task<string> GetCheckCode()
    {
        //System.Console.WriteLine ("Recieved message.");
        string result = null;

        try
        {
            string request_string = $"{db_config.url}/metadata/2016-06-12T13:49:24.759Z/mmria-check-code.js";

            result = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                null,
                null
            );

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

            var revision = await get_revision(db_config.url + "/metadata/2016-06-12T13:49:24.759Z");
            string responseFromServer;
            if (!string.IsNullOrWhiteSpace(revision))
            {
                responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", metadata_url, check_code_json, db_config.user_name, db_config.user_value, "text/*", new Dictionary<string, string> { { "If-Match", revision } });
            }
            else
            {
                responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", metadata_url, check_code_json, db_config.user_name, db_config.user_value, "text/*");
            }

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

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", metadata_url, json_string, db_config.user_name, db_config.user_value);

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

        string temp_document_json = null;

        try
        {
            
            temp_document_json = await _couchDbHttpClient.ExecuteAsync("GET", p_document_url, null, db_config.user_name, db_config.user_value);
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


