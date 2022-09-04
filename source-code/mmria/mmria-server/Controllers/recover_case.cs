using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers
{
    [Authorize(Roles  = "installation_admin")]
    [Route("recover-case")]
    //[Authorize(Policy = "Over21Only")]
    //[Authorize(Policy = "BuildingEntry")]
    //https://docs.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased?view=aspnetcore-2.1&tabs=aspnetcore2x
    public class recover_caseController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        mmria.common.couchdb.ConfigurationSet ConfigDB;

        public recover_caseController
        (
            IAuthorizationService authorizationService, 
            mmria.common.couchdb.ConfigurationSet p_config_db
        )
        {
            _authorizationService = authorizationService;
            ConfigDB = p_config_db;
        }
        public IActionResult Index()
        {
            return View(ConfigDB);
        }
    }
}