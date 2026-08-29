using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Microsoft.AspNetCore.Authorization;
using mmria.server.model;


namespace VitalsImport_FileUpload.Controllers;


[Authorize(Roles = "vital_importer_state")]
[Route("vitals-state/{action=Index}")]
public sealed class vitals_stateController : Controller
{
    private readonly ILogger<vitalsController> _logger;
    private readonly IConfiguration _appConfiguration;

    public vitals_stateController(ILogger<vitalsController> logger, IConfiguration appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
    }

    private void PopulateVitalsUploadViewData()
    {
        TempData["vitals_import_additional_tenants"] = _appConfiguration["mmria_settings:vitals_import_additional_tenants"] ?? string.Empty;
    }

    
    public IActionResult Index()
    {
        PopulateVitalsUploadViewData();
        var model = new FileUploadModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult FileUpload()
    {
        PopulateVitalsUploadViewData();
        var model = new FileUploadModel();
        return View(model);
    }

}

