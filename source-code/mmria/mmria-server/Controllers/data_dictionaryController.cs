using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[AllowAnonymous] 


public sealed class data_dictionaryController : Controller
{
    public data_dictionaryController()
    {
;
    }

    [Route("data-dictionary")]
    public IActionResult Index()
    {
        return View();
    }

    
    [Route("data-dictionary/diff")]
    public IActionResult diff()
    {
        return View();
    }
}
