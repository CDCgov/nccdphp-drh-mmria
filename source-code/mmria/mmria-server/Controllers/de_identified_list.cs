using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "form_designer, cdc_admin")]
[Route("de-identified-list")]
public sealed class de_identified_listController : Controller
{
    public de_identified_listController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
