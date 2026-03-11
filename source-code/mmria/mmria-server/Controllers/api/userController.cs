using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using mmria.common.model;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.ManageUsers.Manager;

using  mmria.server.extension;
namespace mmria.server;


[Route("api/[controller]")]
public sealed class userController: ControllerBase 
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;

    IHttpContextAccessor httpContextAccessor;
    string host_prefix = null;
    private readonly ManageUsersManager _manageUsersManager;

    public userController
	(
        IHttpContextAccessor p_httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        ManageUsersManager manageUsersManager
    )
    {
        _manageUsersManager = manageUsersManager;
        httpContextAccessor = p_httpContextAccessor; 
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
    
    [Authorize(Roles  = "abstractor,data_analyst")]
    [Route("my-user")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.user> GetMyUser() 
    { 
        try
        {
            return await _manageUsersManager.GetMyUserAsync(httpContextAccessor.HttpContext.User, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
        
    } 

    //public IEnumerable<mmria.common.model.couchdb.user_alldocs_response> Get() 
    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user>> Get(int skip = 1, int take = 9000) 
    { 

        try
        {
            return await _manageUsersManager.GetUsersAsync(skip, take, httpContextAccessor.HttpContext.User, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
        //return new mmria.common.model.couchdb.user[] { default(mmria.common.model.couchdb.user), default(mmria.common.model.couchdb.user) }; 
    } 

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.user> Get(string id) 
    { 
        mmria.common.model.couchdb.user result = null;
        try
        {
            result = await _manageUsersManager.GetUserAsync(id, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return result; 
    }


    [Authorize(Roles = "jurisdiction_admin,installation_admin")]
    [Route("check-user/{id}")]
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> CheckUser(string id)
    {
        bool exists = await _manageUsersManager.CheckUserAsync(id, db_config);
        if (exists)
        {
            return Ok();
        }
        return NotFound();
    }

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post([FromBody] mmria.common.model.couchdb.user user) 
    { 
        return await _manageUsersManager.SaveUserAsync(user, db_config);
    } 

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpDelete]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Delete(string user_id = null, string rev = null) 
    { 
        try
        {
            if (string.IsNullOrWhiteSpace(user_id) || string.IsNullOrWhiteSpace(rev)) 
            {
                return null;
            }

            // Authorization check must remain in controller (server-only dependency)
            try 
            {
                var user = await _manageUsersManager.GetUserAsync(user_id, db_config);
                if(!await _manageUsersManager.IsAuthorizedToDeleteUserAsync(User, user, db_config))
                {
                    return null;
                }
            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Delete\n{ex}");
            }

            return await _manageUsersManager.DeleteUserAsync(user_id, rev, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

} 


