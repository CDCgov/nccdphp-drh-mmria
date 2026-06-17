using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.server.extension;

namespace mmria.server;

[Authorize(Roles = "form_designer")]
public sealed class case_validation_metadataController : Controller
{
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _hostPrefix;

    public case_validation_metadataController(mmria.server.util.RequestTenantRuntime tenantRuntime)
    {
        _configuration = tenantRuntime.RequireConfiguration();
        _hostPrefix = tenantRuntime.EffectiveHostPrefix;
    }

    public IActionResult Index()
    {
        TempData["metadata_version"] = _configuration.GetString("metadata_version", _hostPrefix);
        ViewBag.Title = "Case Validation Metadata";
        ViewBag.BreadCrumbs = true;
        ViewBag.Sidebar = true;
        return View();
    }
}
