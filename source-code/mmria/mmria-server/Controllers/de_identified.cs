using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "committee_member")]
[Route("de-identified")]
public sealed class de_identifiedController : Controller
{
    public de_identifiedController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
