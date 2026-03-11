using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.ManageUsers.Manager;

using  mmria.server.extension;  

namespace mmria.server;

[Route("api/[controller]")]
public sealed class user_role_jurisdiction_viewController: ControllerBase
{


    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;


    IHttpContextAccessor httpContextAccessor;
    string host_prefix = null;
    private readonly ManageUsersManager _manageUsersManager;

    public user_role_jurisdiction_viewController
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
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }

    [HttpGet]
    [Route("my-roles")]
    public async Task<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>> my_roles()
    {
        return await _manageUsersManager.GetMyRolesAsync(httpContextAccessor.HttpContext.User, db_config);
    } 


    // GET api/values 
    [HttpGet]
    public async Task<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>> Get
    (
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,
        bool descending = false
    ) 
    {
        /*
            * 
            * http://localhost:5984/de_id/_design/sortable/_view/conflicts
            * 

by_date_created
by_created_by
by_date_last_updated
by_last_updated_by
by_role_name
by_user_id
by_parent_id
by_jurisdiction_id
by_is_active
by_effective_start_date
by_effective_end_date


date_created
created_by
date_last_updated
last_updated_by
role_name
user_id
parent_id
jurisdiction_id
is_active
effective_start_date
effective_end_date

*/
        return await _manageUsersManager.GetUserRoleJurisdictionViewAsync(skip, take, sort, search_key, descending, httpContextAccessor.HttpContext.User, db_config);
    } 

} 


