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
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;  

namespace mmria.server;
	
[Route("api/[controller]")]
public sealed class caseRevisionController: ControllerBase 
{ 
    private ActorSystem _actorSystem;

	//IHttpContextAccessor _accessor;


    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.AuditRecovery.Manager.AuditRecoveryManager _auditRecoveryManager;

    private readonly IAuthorizationService _authorizationService;
    //private readonly IDocumentRepository _documentRepository;

    public caseRevisionController
    (
        IHttpContextAccessor httpContextAccessor,
        ActorSystem actorSystem, 
        IAuthorizationService authorizationService, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.SharedLibraries.AuditRecovery.Manager.AuditRecoveryManager auditRecoveryManager
    )
    {
        _actorSystem = actorSystem;
        _authorizationService = authorizationService;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _auditRecoveryManager = auditRecoveryManager;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }
    
    [Authorize(Roles  = "installation_admin")]
    [HttpGet]
    public async Task<System.Dynamic.ExpandoObject> Get(string jurisdiction_id, string case_id, string revision_id) 
    { 
        try
        {
            var config = configuration.GetDBConfig(jurisdiction_id);

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


