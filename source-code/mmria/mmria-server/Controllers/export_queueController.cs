using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("export-queue")]
  
public sealed class export_queueController : Controller
{
    public export_queueController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
