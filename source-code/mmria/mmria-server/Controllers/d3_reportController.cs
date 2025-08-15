using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
[Route("d3-report")]
public sealed class d3_reportController : Controller
{
    public d3_reportController(IAuthorizationService authorizationService)
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
