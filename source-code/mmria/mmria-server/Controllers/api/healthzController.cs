using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  
using mmria.common.SharedLibraries.HealthDiagnostics.Manager;

namespace mmria.server.Controllers;
    
[Route("api/[controller]")]
[AllowAnonymous] 
public sealed class healthzController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly HealthDiagnosticsManager _healthDiagnosticsManager;
    
    public healthzController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        HealthDiagnosticsManager healthDiagnosticsManager
    )
    {
        _healthDiagnosticsManager = healthDiagnosticsManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!await _healthDiagnosticsManager.IsMmrdsHealthyAsync(db_config)) 
        {
            return StatusCode(500); 
        }
        else
        {
            return Ok(); 
        }
    }
}
