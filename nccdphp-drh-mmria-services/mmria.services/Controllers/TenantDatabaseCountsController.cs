using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.MMRIAServices.Manager;

namespace mmria.services.vitalsimport.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class TenantDatabaseCountsController : ControllerBase
{
    private readonly MMRIAServicesManager _mmriaServicesManager;

    public TenantDatabaseCountsController(MMRIAServicesManager mmriaServicesManager)
    {
        _mmriaServicesManager = mmriaServicesManager;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> Get()
    {
        try
        {
            var result = await _mmriaServicesManager.GetTenantDatabaseCountsAsync(
                mmria.services.vitalsimport.Program.DbConfigSet,
                maxConcurrentEntries: 4,
                perDatabaseTimeoutSeconds: 20);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TenantDatabaseCountsController.Get failed: {ex}");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    error = "Failed to load tenant database counts from central configuration.",
                    reason = ex.Message
                });
        }
    }
}
