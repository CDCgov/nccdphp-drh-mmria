using System;
using System.Collections.Generic;
using Microsoft.CSharp;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Linq;
using mmria.common.model.couchdb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

using  mmria.server.extension;    

namespace mmria.server;

//[Authorize(Roles  = "jurisdiction_admin")]
//[AllowAnonymous] 
[Route("api/[controller]")]
public sealed class sessionDBController: ControllerBase 
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    public sessionDBController 
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


    [HttpGet]
    public async System.Threading.Tasks.Task<IEnumerable<session_response>> Get() 
    { 
        try
        {
            string auth_session_value = this.Request.Cookies["AuthSession"];
            return await _sessionManager.GetCouchDbSessionAsync(auth_session_value, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }


    //[Authorize(Roles  = "abstractor")]
    [HttpPut]
    [HttpPost]
    public async System.Threading.Tasks.Task<IEnumerable<login_response>> Post
    (
        [FromBody] Post_Request_Struct post_request_struct 
    ) 
    {
        

        try
        {
            return await _sessionManager.LoginToCouchDbSessionAsync(db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    }
}



