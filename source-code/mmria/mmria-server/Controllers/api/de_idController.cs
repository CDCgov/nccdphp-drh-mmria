using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.DeIdentified.Manager;
namespace mmria.server;

[Authorize(Roles  = "committee_member")]
[Route("api/[controller]")]
public sealed class de_idController: ControllerBase 
{     
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly DeIdentifiedManager _deIdentifiedManager;

    public de_idController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        DeIdentifiedManager deIdentifiedManager
    )
    {
        _deIdentifiedManager = deIdentifiedManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    public async Task<System.Dynamic.ExpandoObject> Get(string case_id = null) 
    { 
        try
        {
            return await _deIdentifiedManager.GetDeIdentifiedCaseAsync(case_id, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    } 

} 


