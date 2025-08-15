using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers;

[Authorize(Roles  = "installation_admin")]
[Route("recover-case")]
public sealed class recover_caseController : Controller
{
    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public recover_caseController
    (
        mmria.common.couchdb.ConfigurationSet p_config_db
    )
    {
        ConfigDB = p_config_db;
    }
    public IActionResult Index()
    {
        return View(ConfigDB);
    }
}
