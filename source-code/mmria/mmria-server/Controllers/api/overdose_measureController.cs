using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
using mmria.common.SharedLibraries.OverdoseReport.Manager;
using mmria.common.SharedLibraries.OverdoseReport.Model;

namespace mmria.server;

[Route("api/overdose-measures")]
public sealed class overdose_measureController: ControllerBase
{ 
    private readonly OverdoseReportManager _overdoseReportManager;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public overdose_measureController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        OverdoseReportManager overdoseReportManager
    )
    {
        _overdoseReportManager = overdoseReportManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [AllowAnonymous] 
    [HttpGet]
    public async Task<OverdoseMeasureResult> Get()
    {
        OverdoseMeasureResult result = new OverdoseMeasureResult();
        
        try
        {
            result = await _overdoseReportManager.GetOverdoseMeasuresAsync(db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }
} 


