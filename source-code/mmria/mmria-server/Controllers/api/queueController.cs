using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.Queue.Manager;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class queueController: ControllerBase
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly QueueManager _queueManager;
    public queueController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        QueueManager queueManager
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _queueManager = queueManager;
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

        return await _queueManager.SaveQueueItemAsync(
            safeRequest,
            this.Request.Cookies["AuthSession"],
            db_config);
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


