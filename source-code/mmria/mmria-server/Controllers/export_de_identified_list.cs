using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "form_designer, cdc_admin")]
[Route("export-de-identified-list")]
public sealed class export_de_identified_listController : Controller
{
    public export_de_identified_listController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
