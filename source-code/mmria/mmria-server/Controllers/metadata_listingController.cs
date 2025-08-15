using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[AllowAnonymous] 
[Route("metadata-listing")]

public sealed class metadata_listingController : Controller
{
    public metadata_listingController()
    {
        
    }
    public IActionResult Index()
    {
        return View();
    }
}
