using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using mmria.server.extension;

namespace mmria.server.Controllers;

[Authorize(Roles  = "committee_member")]
[Route("de-identified")]
public sealed class de_identifiedController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    string host_prefix = null;

    public de_identifiedController
    (
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();
    }

    public IActionResult Index()
    {
        TempData["omb_expiration_date"] = configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";
        return View();
    }
}
