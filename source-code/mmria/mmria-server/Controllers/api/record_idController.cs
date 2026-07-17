using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class record_idController: ControllerBase
{ 
    private readonly mmria.common.SharedLibraries.Case.ICaseRepository _caseRepository;

    public record Record_Id_Response
    {
        public bool ok { get; init;}
        public bool is_unique { get; init;}
    }
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public record_idController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.SharedLibraries.Case.ICaseRepository caseRepository
    )
    {
        _caseRepository = caseRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async Task<Record_Id_Response> Get(string record_id)
    {
        var result = new Record_Id_Response(){ ok = true, is_unique = false };
        try
        {        
            var is_found = await _caseRepository.RecordIdExistsAsync(record_id, db_config);

            result = new Record_Id_Response(){ ok = true, is_unique = !is_found };

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 
    
} 


