using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "jurisdiction_admin")]
[Route("manage-case-check-outs")]
public sealed class manage_case_check_outsController : Controller
{
    public manage_case_check_outsController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
