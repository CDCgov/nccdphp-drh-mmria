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
public sealed class export_list_managerController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public export_list_managerController
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
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get() 
    { 
        try
        {
            string request_string = $"{db_config.url}/metadata/export-standard-list";

            var curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value,"text/*");

            string responseFromServer = await curl.executeAsync ();


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

        try
        {

            System.IO.Stream dataStream0 = this.Request.Body;
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

            string document_json = await reader0.ReadToEndAsync ();

            string metadata_url = $"{db_config.url}/metadata/export-standard-list";

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


