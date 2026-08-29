using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst,committee_member")]
[Route("community-vital-signs")]
public sealed class CvsController : Controller
{
    public sealed class CVS_View_Model
    {
        public CVS_View_Model(){}

        public string lat {get;set;}
        public string lon {get;set;}
        public string year{get;set;}
        public string id {get;set;}

    }
    private readonly IAuthorizationService _authorizationService;
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public CvsController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public IActionResult Index
    (
        string lat = null,
        string lon = null,
        string year = null,
        string id = null
    )
    {

        var model = new CVS_View_Model()
        {
            lat = lat,
            lon = lon,
            year = year,
            id = id
        };

        TempData["CVS_MAX_ATTEMPTS"] = configuration.GetInteger("CVS_MAX_ATTEMPTS", host_prefix) ?? 10;
        TempData["CVS_RETRY_DELAY_SECONDS"] = configuration.GetInteger("CVS_RETRY_DELAY_SECONDS", host_prefix) ?? 60;

        return View(model);
    }


}
