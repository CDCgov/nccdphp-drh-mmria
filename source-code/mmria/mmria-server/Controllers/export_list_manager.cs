using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "form_designer, cdc_admin")]
[Route("export-list-manager")]
public sealed class export_list_managerController : Controller
{
    public export_list_managerController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
