using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "form_designer,cdc_analyst")]
[Route("manage-substance-lists")]
public sealed class manageSubstanceListsController : Controller
{
    public manageSubstanceListsController()
    {

    }
    public IActionResult Index()
    {
        return View();
    }
}
