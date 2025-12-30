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
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public AnalystCaseController
	(
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
    public IActionResult Index(string r = "da")
    {

        TempData["metadata_version"] = configuration.GetString("metadata_version",host_prefix);
        TempData["ui_role_mode"] = r;
     
   
        TempData["is_offline_mode_enabled"] = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        TempData["offline_mode_max_new_cases"] = configuration.GetInteger("offline_mode_max_new_cases", host_prefix) ?? 3;
        TempData["offline_mode_max_existing_cases"] = configuration.GetInteger("offline_mode_max_existing_cases", host_prefix) ?? 3;
        ViewBag.is_offline_mode_enabled = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
     
        
        return View();
    }
}
