
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using mmria.server.model;


namespace VitalsImport_FileUpload.Controllers;

[Authorize(Roles = "abstractor, jurisdiction_admin")]
[Route("vital-import-history")]
public sealed class vital_import_history_abstractorController : Controller
{
    private readonly ILogger<vitalsController> _logger;

    public vital_import_history_abstractorController(ILogger<vitalsController> logger)
    {
        _logger = logger;
    }

    
    public IActionResult Index()
    {
        var model = new FileUploadModel();
        return View(model);
    }

}

