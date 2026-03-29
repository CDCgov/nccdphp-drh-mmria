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
using mmria.common.model.couchdb.recover_doc;

using  mmria.server.extension;  

namespace mmria.server;
	
[Route("api/[controller]")]
public sealed class caseRevisionController: ControllerBase 
{ 
    private ActorSystem _actorSystem;

	//IHttpContextAccessor _accessor;


    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.AuditRecovery.Manager.AuditRecoveryManager _auditRecoveryManager;
    private readonly mmria.server.util.RequestTenantRuntime _tenantRuntime;
    private readonly mmria.server.util.TenantCatalog _tenantCatalog;

    private readonly IAuthorizationService _authorizationService;
    //private readonly IDocumentRepository _documentRepository;

    public caseRevisionController
    (
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.server.util.TenantCatalog tenantCatalog,
        ActorSystem actorSystem, 
        IAuthorizationService authorizationService, 
        mmria.common.SharedLibraries.AuditRecovery.Manager.AuditRecoveryManager auditRecoveryManager
    )
    {
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;
        _auditRecoveryManager = auditRecoveryManager;
        _tenantRuntime = tenantRuntime;
        _tenantCatalog = tenantCatalog;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
        host_prefix = tenantRuntime.EffectiveHostPrefix;
    }
    
    [Authorize(Roles  = "installation_admin")]
    [HttpGet]
    public async Task<System.Dynamic.ExpandoObject> Get(string jurisdiction_id, string case_id, string revision_id) 
    { 
        try
        {
            _ = _tenantRuntime;
            var config = _tenantCatalog.TryResolveDbConfig(jurisdiction_id);
            if (config == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                return await _auditRecoveryManager.GetCaseRevisionAsync(case_id, revision_id, config);

            } 

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 


    [Authorize(Roles  = "installation_admin")]
    [HttpPost]
    public async Task<mmria.common.model.couchdb.document_put_response> Post
    (
        string jurisdiction_id, 
        string case_id, 
        string revision_id
    ) 
    { 

        return null;
    } 

    
} 


