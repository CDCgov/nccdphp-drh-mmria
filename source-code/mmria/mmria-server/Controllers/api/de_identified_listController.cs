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
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public de_identified_listController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get(string id) 
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

            var customHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
            {
                string auth_session_value = this.Request.Cookies["AuthSession"];
                customHeaders.Add("Cookie", "AuthSession=" + auth_session_value);
                customHeaders.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
            }

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, null, null, "application/json", customHeaders);

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

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                metadata_url,
                document_json,
                db_config.user_name,
                db_config.user_value,
                "text/*"
            );

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

} 


