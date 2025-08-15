using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "form_designer,power_bi_manager")]
[Route("power-bi-user")]
public sealed class power_bi_userController : Controller
{
    public power_bi_userController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
