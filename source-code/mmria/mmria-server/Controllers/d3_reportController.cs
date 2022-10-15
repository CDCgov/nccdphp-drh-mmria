using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace mmria.server.Controllers
{
    [Authorize(Roles  = "abstractor,data_analyst")]
    [Route("d3-report")]
    //[Authorize(Policy = "Over21Only")]
    //[Authorize(Policy = "BuildingEntry")]
    //https://docs.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased?view=aspnetcore-2.1&tabs=aspnetcore2x
    public sealed class d3_reportController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        //private readonly IDocumentRepository _documentRepository;

        public d3_reportController(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
            //_documentRepository = documentRepository;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}