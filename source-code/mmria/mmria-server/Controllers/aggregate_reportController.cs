using System;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]

public sealed class aggregate_reportController : Controller
{
    public aggregate_reportController()
    {

    }
    
    [Route("aggregate-report")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("aggregate-report/pdf")]
    public IActionResult pdf()
    {
        return View();
    }
}
