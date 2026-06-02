using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.server.util;
using mmria.common.SharedLibraries.AuditRecovery.Manager;
namespace mmria.server.Controllers;

[Authorize(Roles  = "installation_admin,cdc_admin")]
[Route("recover-deleted-case/{action=Index}")]
public sealed class recover_deleted_caseController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;
    private readonly AuditRecoveryManager _auditRecoveryManager;


    public recover_deleted_caseController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        AuditRecoveryManager auditRecoveryManager
    )
    {
        _auditRecoveryManager = auditRecoveryManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _dbConfigSet = tenantRuntime.RequireConfigurationSet();

        if(_dbConfigSet.detail_list.ContainsKey("vital_import"))
        {
            _dbConfigSet.detail_list.Remove("vital_import");
        }
    }

    public IActionResult Index()
    {
        return View(_dbConfigSet);
    }

    [HttpPost]
    public async Task<IActionResult> FindRecord(
        [Bind(
            nameof(mmria.server.model.recover_deleted.Request.StateDatabase) + "," +
            nameof(mmria.server.model.recover_deleted.Request.RecordId))]
        mmria.server.model.recover_deleted.Request Model)
    {
        Model ??= new mmria.server.model.recover_deleted.Request();
        var model = new mmria.server.model.recover_deleted.RequestResponse();
        model.SearchText = Model.RecordId;
        try
        {
            var isCdcAdmin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
            var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
            var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, Model.StateDatabase, host_prefix, db_config, _dbConfigSet);
            model.is_cdc_admin = isCdcAdmin;

            var deletedCases = await _auditRecoveryManager.FindDeletedCasesAsync(Model.RecordId, effectiveStateDatabase, effectiveDbConfig);
            foreach(var item in deletedCases)
            {
                try
                {
                    model.Detail.Add(item);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }


        return View(model);
    }
    [HttpPost]
    public IActionResult ConfirmRecoverRequest(
        [Bind(
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View._id) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.record_id) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.first_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.last_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.user_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.date_created) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.StateDatabase))]
        mmria.common.model.couchdb.audit.Audit_Detail_View Model)
    {
        var model = Model ?? new mmria.common.model.couchdb.audit.Audit_Detail_View();
        model.StateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, model.StateDatabase, host_prefix, _dbConfigSet);

    
        return View(model);
    }

    public sealed class UpdateDeletedCaseResult
    {
        public UpdateDeletedCaseResult(){}
        public mmria.common.model.couchdb.audit.Audit_Detail_View detail { get; set; }
        public bool is_problem_deleting { get; set; }
        public string problem_description { get; set; }
    }
    [HttpPost]
    public async Task<IActionResult> UpdateDeletedCase(
        [Bind(
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View._id) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.record_id) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.first_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.last_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.StateDatabase))]
        mmria.common.model.couchdb.audit.Audit_Detail_View Model)
    {
        Model ??= new mmria.common.model.couchdb.audit.Audit_Detail_View();
        Model.StateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
        var result = new UpdateDeletedCaseResult()
        {
            detail = Model,
            is_problem_deleting = false
        };

        try
        {
            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }


            var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, Model.StateDatabase, host_prefix, db_config, _dbConfigSet);
            var restoreResult = await _auditRecoveryManager.RestoreDeletedCaseAsync(Model._id, userName, effectiveDbConfig);
            result.is_problem_deleting = restoreResult.IsProblemDeleting;
            result.problem_description = restoreResult.ProblemDescription;

            
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            result.is_problem_deleting = true;
            result.problem_description = ex.Message;
        }

        return View(result);
    }

}
