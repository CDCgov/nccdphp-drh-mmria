using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;
using mmria.server.model;


namespace VitalsImport_FileUpload.Controllers;


[Authorize(Roles = "vital_importer_state")]
[Route("vitals-state/{action=Index}")]
public sealed class vitals_stateController : Controller
{
    private readonly ILogger<vitalsController> _logger;

    public vitals_stateController(ILogger<vitalsController> logger)
    {
        _logger = logger;
    }

    
    public IActionResult Index()
    {
        var model = new FileUploadModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult FileUpload()
    {
        var model = new FileUploadModel();
        return View(model);
    }

}

