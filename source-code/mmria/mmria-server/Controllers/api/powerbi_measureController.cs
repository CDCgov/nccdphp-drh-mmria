using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.PowerBI.Manager;
using mmria.common.SharedLibraries.PowerBI.Model;

namespace mmria.server;

[Route("api/powerbi-measures/{indicator_id?}")]
public sealed class powerbi_measureController: ControllerBase
{ 
    private readonly PowerBIManager _powerBIManager;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public powerbi_measureController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        PowerBIManager powerBIManager
    )
    {
        _powerBIManager = powerBIManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }


    [AllowAnonymous] 
    [HttpGet]
    public async Task<PowerBIMeasureResult> Get(string indicator_id)
    {
        PowerBIMeasureResult result = new PowerBIMeasureResult();
        
        try
        {
            result = await _powerBIManager.GetPowerBIMeasuresAsync(indicator_id, db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }
} 


