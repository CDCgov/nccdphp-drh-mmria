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
using mmria.common.SharedLibraries.AuditRecovery.Manager;

namespace mmria.server;

	
[Route("api/[controller]")]
public sealed class caseRevisionListController: ControllerBase 
{ 
    private readonly mmria.server.util.RequestTenantRuntime _tenantRuntime;
    private readonly mmria.server.util.TenantCatalog _tenantCatalog;
    private readonly AuditRecoveryManager _auditRecoveryManager;

    public caseRevisionListController
    (
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.server.util.TenantCatalog tenantCatalog,
        AuditRecoveryManager auditRecoveryManager
    )
    {
        _tenantRuntime = tenantRuntime;
        _tenantCatalog = tenantCatalog;
        _auditRecoveryManager = auditRecoveryManager;
    }
    
    [Authorize(Roles  = "installation_admin")]
    [HttpGet]
    public async Task<All_Revs> Get(string jurisdiction_id, string case_id) 
    { 
        try
        {
            _ = _tenantRuntime;
            var config = _tenantCatalog.TryResolveDbConfig(jurisdiction_id);
            if (config == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(case_id))
            {
                return await _auditRecoveryManager.GetAllCaseRevisionsAsync(case_id, config);
            } 

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 




} 


