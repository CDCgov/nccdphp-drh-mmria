using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
[Route("data-quality-report")]
    public sealed class data_quality_reportController : Controller
{
    public data_quality_reportController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
