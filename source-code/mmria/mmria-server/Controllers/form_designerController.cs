using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Route("form-designer")]
public sealed class form_designerController : Controller
{
    [Authorize(Roles = "form_designer")]
    public IActionResult Index()
    {
        return View();
    }
}
