using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Dynamic;
using mmria.common;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

public sealed class IsDuplicateCaseRequest
{
    public IsDuplicateCaseRequest(){}

    public string FirstName {get;set;}
    public string LastName {get;set;}

    public int MonthOfDeath {get;set;}
    public int DayOfDeath {get;set;}
    public int YearOfDeath {get;set;}
    public string StateOfDeath {get;set;}

}

[Authorize(Roles  = "abstractor")]
[Route("api/[controller]")]
public sealed class isDuplicateCaseController: ControllerBase 
{ 
    ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public isDuplicateCaseController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _actorSystem = actorSystem;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _couchDbHttpClient = couchDbHttpClient;
    }
    
    
    [HttpPost]
    public async Task<bool> Post([FromBody] IsDuplicateCaseRequest DuplicateCaseRequest) 
    { 

        var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
            db_config,
            User,
            true,
            false,
            _couchDbHttpClient
        );

        return await caseViewManager.IsDuplicateCaseAsync(
            DuplicateCaseRequest.FirstName,
            DuplicateCaseRequest.LastName,
            DuplicateCaseRequest.MonthOfDeath,
            DuplicateCaseRequest.DayOfDeath,
            DuplicateCaseRequest.YearOfDeath,
            DuplicateCaseRequest.StateOfDeath
        );
    } 
} 


