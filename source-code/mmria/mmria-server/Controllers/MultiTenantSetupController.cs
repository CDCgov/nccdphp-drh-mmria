using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.server.extension;
using mmria.server.util;

namespace mmria.server.Controllers;

[Authorize(Roles = "installation_admin")]
public sealed class MultiTenantSetupController : Controller
{
    private readonly MultiTenantSetupService _multiTenantSetupService;

    public MultiTenantSetupController(MultiTenantSetupService multiTenantSetupService)
    {
        _multiTenantSetupService = multiTenantSetupService;
    }

    [HttpGet("/MultiTenantSetup")]
    public IActionResult Index()
    {
        string currentHostPrefix = HttpContext?.Request?.Host.GetPrefix();
        var model = _multiTenantSetupService.BuildPageModel(currentHostPrefix);
        return View(model);
    }

    [HttpGet("/api/MultiTenantSetup/load")]
    public async Task<IActionResult> Load([FromQuery] string tenant)
    {
        string resolvedTenant = ResolveTenant(tenant);
        var result = await _multiTenantSetupService.LoadTenantAsync(resolvedTenant);
        return BuildApiResponse(result);
    }

    [HttpPost("/api/MultiTenantSetup/rebuild")]
    public async Task<IActionResult> Rebuild([FromQuery] string tenant, [FromQuery] string mode = "fresh")
    {
        string resolvedTenant = ResolveTenant(tenant);
        var result = await _multiTenantSetupService.RebuildTenantAsync(resolvedTenant, mode);
        return BuildApiResponse(result);
    }

    [HttpGet("/api/MultiTenantSetup/summary")]
    public async Task<IActionResult> Summary()
    {
        string currentHostPrefix = HttpContext?.Request?.Host.GetPrefix();
        var summary = await _multiTenantSetupService.GetStartupRunSummaryAsync(currentHostPrefix);
        return Ok(summary);
    }

    private string ResolveTenant(string tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            return tenant.Trim();
        }

        return HttpContext?.Request?.Host.GetPrefix();
    }

    private IActionResult BuildApiResponse(MultiTenantSetupResult result)
    {
        return result.status_code switch
        {
            StatusCodes.Status200OK => Ok(result),
            StatusCodes.Status202Accepted => Accepted(result),
            StatusCodes.Status404NotFound => NotFound(result),
            StatusCodes.Status400BadRequest => BadRequest(result),
            StatusCodes.Status409Conflict => Conflict(result),
            _ => StatusCode(result.status_code, result)
        };
    }
}
