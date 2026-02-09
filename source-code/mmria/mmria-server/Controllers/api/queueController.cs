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
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public queueController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
        _couchDbHttpClient = couchDbHttpClient;
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

        var customHeaders = new Dictionary<string, string>();
        if(!string.IsNullOrWhiteSpace(set_queue_request.security_token))
        {
            customHeaders.Add("Cookie", "AuthSession=" + set_queue_request.security_token);
            customHeaders.Add("X-CouchDB-WWW-Authenticate", set_queue_request.security_token);
        }
        else if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
        {
            string auth_session_value = this.Request.Cookies["AuthSession"];
            customHeaders.Add("Cookie", "AuthSession=" + auth_session_value);
            customHeaders.Add("X-CouchDB-WWW-Authenticate", auth_session_value);
        }

        mmria.common.model.couchdb.document_put_response put_response = null;

        try
        {
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", queue_url, object_string, null, null, "application/json", customHeaders);
            put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
            result.Ok = false;
            result.message = ex.ToString ();
        }

        //if(put_response.


        return result;
    }
}


