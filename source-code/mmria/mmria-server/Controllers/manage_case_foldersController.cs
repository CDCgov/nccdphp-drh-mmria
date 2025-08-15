using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using  mmria.server.extension;

namespace mmria.server.Controllers;
    

public sealed class manage_case_foldersController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public manage_case_foldersController
    ( 
        IHttpContextAccessor httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration p_configuration
    )
    {
         configuration = p_configuration;

        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }

    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    [Route("/manage-case-folders")]
    public IActionResult Index()
    {
        return View();
    }

}
