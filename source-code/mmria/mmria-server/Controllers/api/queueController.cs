using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;

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
    public async System.Threading.Tasks.Task<mmria.common.data.api.Set_Queue_Response> Post()
    { 
        var set_queue_request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<mmria.common.data.api.Set_Queue_Request>(Request);
        mmria.common.data.api.Set_Queue_Response result = new mmria.common.data.api.Set_Queue_Response();
        var safeRequest = CreateSanitizedQueueRequest(set_queue_request);

        if (safeRequest == null)
        {
            result.Ok = false;
            result.message = "Invalid queue request.";
            return result;
        }

        mmria.common.data.api.Queue_Item queue_item = new mmria.common.data.api.Queue_Item ();
        queue_item.queue_id = System.Guid.NewGuid ().ToString ();
        queue_item.action = safeRequest.action;
        queue_item.case_list = safeRequest.case_list;

        string queue_url = db_config.url + "/queue/"  + queue_item.queue_id;

        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(queue_item);

        var requestOptions = new mmria.common.getset.CouchDbRequestOptions();
        if(!string.IsNullOrWhiteSpace(safeRequest.security_token))
        {
            requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                AuthSessionValue = safeRequest.security_token
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
            result.Ok = put_response?.ok == true;
            result.Queue_Id = queue_item.queue_id;
            result.message = put_response?.error_description;
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

    private static mmria.common.data.api.Set_Queue_Request CreateSanitizedQueueRequest(mmria.common.data.api.Set_Queue_Request request)
    {
        if (request == null)
        {
            return null;
        }

        return new mmria.common.data.api.Set_Queue_Request
        {
            security_token = string.IsNullOrWhiteSpace(request.security_token) ? null : request.security_token.Trim(),
            action = string.IsNullOrWhiteSpace(request.action) ? null : request.action.Trim(),
            case_list = CloneCaseList(request.case_list)
        };
    }

    private static ExpandoObject[] CloneCaseList(ExpandoObject[] requestCaseList)
    {
        if (requestCaseList == null)
        {
            return Array.Empty<ExpandoObject>();
        }

        return requestCaseList
            .Where(item => item != null)
            .Select(CloneExpandoObject)
            .ToArray();
    }

    private static ExpandoObject CloneExpandoObject(ExpandoObject source)
    {
        var clone = new ExpandoObject();
        var cloneDictionary = (IDictionary<string, object>)clone;

        if (source is IDictionary<string, object> sourceDictionary)
        {
            foreach (var kvp in sourceDictionary)
            {
                cloneDictionary[kvp.Key] = kvp.Value;
            }
        }

        return clone;
    }
}


