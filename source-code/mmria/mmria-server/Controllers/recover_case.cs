using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "installation_admin")]
[Route("recover-case")]
public sealed class recover_caseController : Controller
{
    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public recover_caseController
    (
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        ConfigDB = tenantRuntime.RequireConfigurationSet();
    }
    public IActionResult Index()
    {
        return View(ConfigDB);
    }
}
