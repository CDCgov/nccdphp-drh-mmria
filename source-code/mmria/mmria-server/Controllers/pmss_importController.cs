#if IS_PMSS_ENHANCED
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using mmria.pmss.server.model;

using mmria.server.model;
namespace mmria.server.Controllers;

[Authorize(Roles = "vital_importer")]
[Route("pmss-import/{action=Index}")]
public sealed class pmss_importController : Controller
{
    private readonly ILogger<pmss_importController> _logger;

    public pmss_importController(ILogger<pmss_importController> logger)
    {
        _logger = logger;
    }

    //[HttpGet]
    public IActionResult Index()
    {
        var model = new FileUploadModel();
        return View(model);
    }

    //[HttpGet]
    public IActionResult FileUpload()
    {
        var model = new FileUploadModel();
        return View(model);
    }

}

#endif