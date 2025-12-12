using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles  = "jurisdiction_admin")]
[Route("manage-case-check-outs")]
public sealed class manage_case_check_outsController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
 
    public manage_case_check_outsController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    public IActionResult Index()
    {

        TempData["metadata_version"] = configuration.GetString("metadata_version", host_prefix);
        TempData["is_offline_mode_enabled"] = configuration.GetBoolean("is_offline_mode_enabled", host_prefix);
        TempData["offline_mode_max_new_cases"] = configuration.GetInteger("offline_mode_max_new_cases", host_prefix);
        TempData["offline_mode_max_existing_cases"] = configuration.GetInteger("offline_mode_max_existing_cases", host_prefix);
        ViewBag.is_offline_mode_enabled = configuration.GetBoolean("is_offline_mode_enabled", host_prefix);

        return View();
    }
}
