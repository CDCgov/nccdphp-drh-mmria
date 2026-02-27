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
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly AggregateReportManager _aggregateReportManager;

    string host_prefix = null;

    public aggregate_reportController  
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        AggregateReportManager aggregateReportManager
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _couchDbHttpClient = couchDbHttpClient;
        _aggregateReportManager = aggregateReportManager;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IList<c_report_object>> Get()
    {
        System.Console.WriteLine ("Recieved message.");
        return await _aggregateReportManager.GetReportsAsync(db_config);
    } 
}
