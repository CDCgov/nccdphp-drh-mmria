#if IS_PMSS_ENHANCED
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor, data_analyst, committee_member, vro")]
public sealed class CaseVROController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public CaseVROController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }
        
    public IActionResult Index()
    {

        TempData["metadata_version"] = configuration.GetString("metadata_version", host_prefix);
        TempData["omb_expiration_date"] = configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";
        return View();
    }

}
#endif
