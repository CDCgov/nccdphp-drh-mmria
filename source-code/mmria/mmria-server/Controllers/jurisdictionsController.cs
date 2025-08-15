using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles = "installation_admin,jurisdiction_admin")]
public sealed class jurisdictionsController : Controller
{
    public async Task<IActionResult> Index()
    {
        return View();
    }

}
