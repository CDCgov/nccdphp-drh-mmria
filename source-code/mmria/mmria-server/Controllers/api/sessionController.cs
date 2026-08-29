using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using mmria.common.model.couchdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using mmria.common.utils;

using  mmria.server.extension;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class sessionController: ControllerBase
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    public sessionController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager
    )
    {
        _sessionManager = sessionManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Route("list")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.session>> Get
    (
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,
        bool descending = false
    ) 
    {
        try
        {
            return await _sessionManager.GetSessionListAsync(skip, take, sort, search_key, descending, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    } 


    [HttpGet]
    public  async System.Threading.Tasks.Task<IEnumerable<session_response>> Get() 
    { 
        try
        {
            return await _sessionManager.GetSessionDatabaseAsync(db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }

    [HttpPut]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post()
    {
        var postRequest = await mmria.server.util.JsonRequestBodyReader.ReadAsync<session>(Request);

        var sanitizedSession = await CreateSanitizedSessionAsync(postRequest);
        if (sanitizedSession == null)
        {
            return null;
        }

        try
        {
            var result = await _sessionManager.PostSessionDocumentAsync(sanitizedSession, User, db_config);
            if (result == null || !result.ok)
            {
                var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(postRequest?._rev, sanitizedSession._rev);
                Console.WriteLine(
                    $"Session save failed for {sanitizedSession._id}: rev={revisionHandling}; response={result?.error_description}");
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

    private async System.Threading.Tasks.Task<session> CreateSanitizedSessionAsync(session request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        var sessionId = request._id.Trim();
        session existingSession = null;

        try
        {
            existingSession = await _sessionManager.GetSessionDocumentAsync(sessionId, db_config);
        }
        catch
        {
            // Missing sessions are treated as creates.
        }

        var userName = GetCurrentUserName();
        var remoteIp = HttpContext?.Connection?.RemoteIpAddress?.ToString();

        var sanitizedSession = existingSession ?? new session();
        sanitizedSession._id = sessionId;
        sanitizedSession._rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(request._rev, existingSession?._rev);
        sanitizedSession.data_type = "session";
        sanitizedSession.date_created =
            existingSession?.date_created ??
            (request.date_created == default ? DateTime.UtcNow : request.date_created);
        sanitizedSession.date_last_updated = DateTime.UtcNow;
        sanitizedSession.date_expired = request.date_expired;
        sanitizedSession.is_active = request.is_active;
        sanitizedSession.user_id = !string.IsNullOrWhiteSpace(existingSession?.user_id) ? existingSession.user_id : userName;
        sanitizedSession.ip = !string.IsNullOrWhiteSpace(remoteIp) ? remoteIp : existingSession?.ip;
        sanitizedSession.session_event_id = !string.IsNullOrWhiteSpace(existingSession?.session_event_id)
            ? existingSession.session_event_id
            : request.session_event_id;
        sanitizedSession.data = CloneSessionData(request.data, existingSession?.data);

        return sanitizedSession;
    }

    private static Dictionary<string, string> CloneSessionData(
        Dictionary<string, string> requestData,
        Dictionary<string, string> existingData)
    {
        var source = requestData ?? existingData;
        var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (source == null)
        {
            return result;
        }

        foreach (var kvp in source)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private string GetCurrentUserName()
    {
        if (User?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            return User.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name)
                .Value;
        }

        return null;
    }
}

public struct Post_Request_Struct
{
    public string name;
    public string value;
}


