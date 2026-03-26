using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.MMRIARebuild.Manager;
using mmria.common.SharedLibraries.MMRIARebuild.Model;

namespace mmria.services.vitalsimport.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class MMRIARebuildController : ControllerBase
{
    private readonly MMRIARebuildManager _mmriaRebuildManager;

    public MMRIARebuildController(MMRIARebuildManager mmriaRebuildManager)
    {
        _mmriaRebuildManager = mmriaRebuildManager;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> Post([FromBody] MMRIARebuildRequest request)
    {
        var result = await _mmriaRebuildManager.EnqueueInProcessRebuildAsync(request);

        return result.status_code switch
        {
            StatusCodes.Status202Accepted => Accepted(result),
            StatusCodes.Status409Conflict => Conflict(result),
            StatusCodes.Status400BadRequest => BadRequest(result),
            StatusCodes.Status404NotFound => NotFound(result),
            _ => StatusCode(result.status_code, result)
        };
    }
}
