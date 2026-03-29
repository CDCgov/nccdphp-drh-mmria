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
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public queueController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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

        var requestOptions = new mmria.common.getset.CouchDbRequestOptions();
        if(!string.IsNullOrWhiteSpace(set_queue_request.security_token))
        {
            requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                AuthSessionValue = set_queue_request.security_token
            };
        }
        else if (!string.IsNullOrWhiteSpace(this.Request.Cookies["AuthSession"]))
        {
            requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                AuthSessionValue = this.Request.Cookies["AuthSession"]
            };
        }

        mmria.common.model.couchdb.document_put_response put_response = null;

        try
        {
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", queue_url, object_string, "application/json", requestOptions);
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


