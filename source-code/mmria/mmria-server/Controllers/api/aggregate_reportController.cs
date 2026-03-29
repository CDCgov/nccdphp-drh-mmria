using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

using mmria.common;
using mmria.common.Manager;
using mmria.common.Model.AggregateReport;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class aggregate_reportController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly AggregateReportManager _aggregateReportManager;

    string host_prefix = null;

    public aggregate_reportController  
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        AggregateReportManager aggregateReportManager
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _aggregateReportManager = aggregateReportManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IList<c_report_object>> Get()
    {
        System.Console.WriteLine ("Recieved message.");
        return await _aggregateReportManager.GetReportsAsync(db_config);
    } 
}
