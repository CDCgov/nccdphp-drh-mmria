#if !IS_PMSS_ENHANCED
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
using mmria.common.Manager.InteractiveReport;
using mmria.common.Model.InteractiveReport;

namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/measure-indicator/{indicator_id}")]
public sealed class interactive_report_viewController: ControllerBase 
{  

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly InteractiveReportManager _interactiveReportManager;

    public interactive_report_viewController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        InteractiveReportManager interactiveReportManager
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _couchDbHttpClient = couchDbHttpClient;
        _interactiveReportManager = interactiveReportManager;
    }
    public async Task<IList<report_measure_value_struct>> Get(string indicator_id)
    {
        var jurisdiction_hashset = mmria.common.SharedLibraries.Other.authorization.get_current_jurisdiction_id_set_for(db_config, User);
        var jurisdictionAccessList = jurisdiction_hashset.Select(j => 
            new mmria.common.Model.InteractiveReport.JurisdictionAccessInfo
            {
                JurisdictionId = j.jurisdiction_id,
                ResourceRight = (int)j.ResourceRight
            }).ToList();
        return await _interactiveReportManager.Get(indicator_id, db_config, jurisdictionAccessList);
    }
} 
#endif
