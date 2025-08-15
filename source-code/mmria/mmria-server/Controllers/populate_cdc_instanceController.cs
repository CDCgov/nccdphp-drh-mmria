using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using mmria.server.model;


namespace CDCAdmin.Controllers;

[Authorize(Roles = "cdc_admin")]
[Route("populate-cdc-instance")]
public sealed class populate_cdc_instanceController : Controller
{
    private readonly ILogger<populate_cdc_instanceController> _logger;

    public populate_cdc_instanceController(ILogger<populate_cdc_instanceController> logger)
    {
        _logger = logger;
    }

    
    public IActionResult Index()
    {
        var model = new FileUploadModel();
        return View(model);
    }

}

