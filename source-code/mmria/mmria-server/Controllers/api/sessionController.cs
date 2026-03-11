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

using  mmria.server.extension;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class sessionController: ControllerBase
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.Session.Manager.SessionManager _sessionManager;
    public sessionController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.SharedLibraries.Session.Manager.SessionManager sessionManager
    )
    {
        _sessionManager = sessionManager;
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
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
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] session Post_Request

    ) 
    { 

        try
        {
            await _sessionManager.PostSessionDocumentAsync(Post_Request, User, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }
}

public struct Post_Request_Struct
{
    public string name;
    public string value;
}


