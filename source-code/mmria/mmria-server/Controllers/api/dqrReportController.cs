using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.functional;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.DQRReport.Manager;
using mmria.common.SharedLibraries.DQRReport.Model;
namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/dqr-detail/{quarter_string}")]
public sealed class dqrReportController: ControllerBase 
{  
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly DQRReportManager _dqrReportManager;

    public dqrReportController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        DQRReportManager dqrReportManager
    )
    {
        _dqrReportManager = dqrReportManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }
    public async Task<DQRReportResult> Get(string quarter_string)
    {
        var result = new DQRReportResult();
        
        try
        {
            result = await _dqrReportManager.GetDqrDetailsAsync(quarter_string, db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }
} 


