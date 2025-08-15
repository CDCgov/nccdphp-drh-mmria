using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
public sealed class overdose_reportController : Controller
{
    public overdose_reportController()
    {

    }
    
    [Route("overdose-data-summary")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("overdose-data-summary/pdf")]
    public IActionResult pdf()
    {
        return View();
    }
}
