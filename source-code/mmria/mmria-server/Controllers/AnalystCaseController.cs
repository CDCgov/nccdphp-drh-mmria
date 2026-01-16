using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  

namespace mmria.server.Controllers;

[Authorize(Roles  = "data_analyst")]
[Route("analyst-case")]
public sealed class AnalystCaseController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public AnalystCaseController
	(
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }
    public IActionResult Index(string r = "da")
    {

        TempData["metadata_version"] = configuration.GetString("metadata_version",host_prefix);
        TempData["ui_role_mode"] = r;
         
        return View();
    }
}
