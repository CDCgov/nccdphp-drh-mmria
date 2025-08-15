using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "form_designer")]
[Route("version-manager")]
public sealed class version_managerController : Controller
{
    public version_managerController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }


    [Route("migrate")]
    public IActionResult Migrate()
    {
        return View("migrate");
    }
}
