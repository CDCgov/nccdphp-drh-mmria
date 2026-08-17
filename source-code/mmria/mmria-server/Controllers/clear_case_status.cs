using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using mmria.server.extension;
using mmria.server.util;
using mmria.common.SharedLibraries.CaseWorkflowAdmin.Manager;

namespace mmria.server.Controllers;

[Authorize(Roles = "cdc_admin,jurisdiction_admin")]
public sealed class clear_case_statusController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly CaseWorkflowAdminManager _manager;

    private readonly System.Collections.Generic.Dictionary<string, string> CaseStatusToDisplay;
    public clear_case_statusController
    (
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        CaseWorkflowAdminManager manager
    )
    {
        _dbConfigSet = tenantRuntime.RequireConfigurationSet();
        _manager = manager;

        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        if (_dbConfigSet.detail_list.ContainsKey("vital_import"))
        {
            _dbConfigSet.detail_list.Remove("vital_import");
        }

        CaseStatusToDisplay = new System.Collections.Generic.Dictionary<string, string>();
        CaseStatusToDisplay["9999"] = "(blank)";
        CaseStatusToDisplay["1"] = "Abstracting (Incomplete)";
        CaseStatusToDisplay["2"] = "Abstraction Complete";
        CaseStatusToDisplay["3"] = "Ready for Review";
        CaseStatusToDisplay["4"] = "Review Complete and Decision Entered";
        CaseStatusToDisplay["5"] = "Out of Scope and Death Certificate Entered";
        CaseStatusToDisplay["6"] = "False Positive and Death Certificate Entered";
        CaseStatusToDisplay["0"] = "Vitals Import";
    }
    public IActionResult Index()
    {
        return View(_dbConfigSet);
    }

    [HttpPost]
    public async Task<IActionResult> FindRecord(
        [Bind(
            nameof(mmria.server.model.casestatus.CaseStatusRequest.StateDatabase) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusRequest.RecordId))]
        mmria.server.model.casestatus.CaseStatusRequest Model)
    {
        Model ??= new mmria.server.model.casestatus.CaseStatusRequest();
        var model = new mmria.server.model.casestatus.CaseStatusRequestResponse();
        model.SearchText = Model.RecordId;
        TempData["SearchText"] = model.SearchText;
        try
        {
            var isCdcAdmin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
            var effectiveRole = isCdcAdmin ? "cdc_admin" : "jurisdiction_admin";
            var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
            var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, Model.StateDatabase, host_prefix, db_config, _dbConfigSet);
            model.is_cdc_admin = isCdcAdmin;

            var case_view_response = await _manager.GetCasesByDateAsync(effectiveDbConfig);

            var Locked_status_list = new List<int>() { 4, 5, 6 };
            foreach (var item in case_view_response.rows)
            {
                try
                {
                    if
                    (
                        item.value.record_id != null &&
                        !string.IsNullOrWhiteSpace(Model.RecordId) &&
                        (
                            item.value.record_id.IndexOf(Model.RecordId, System.StringComparison.OrdinalIgnoreCase) > -1 ||
                            Model.RecordId.IndexOf(item.value.record_id, System.StringComparison.OrdinalIgnoreCase) > -1
                        )
                    )
                    {
                        var x = new mmria.server.model.casestatus.CaseStatusDetail()
                        {
                            _id = item.id,
                            RecordId = item.value?.record_id,
                            FirstName = item.value?.first_name,
                            LastName = item.value?.last_name,
                            MiddleName = item.value?.middle_name,
                            DateOfDeath = $"{item.value?.date_of_death_month}/{item.value.date_of_death_year}",
                            StateOfDeath = item.value?.host_state,
                            AgencyCaseId = item.value?.agency_case_id,
                            LocalFileNumber = item.value?.local_file_number,
                            StateFileNumber = item.value?.state_file_number,

                            LastUpdatedBy = item.value?.last_updated_by,

                            DateLastUpdated = item.value?.date_last_updated,

                            CaseStatus = item.value.case_status,

                            CaseStatusDisplay = (item.value.case_status != null && CaseStatusToDisplay.ContainsKey(item.value.case_status.ToString())) ? CaseStatusToDisplay[item.value.case_status.ToString()] : "(blank)",

                            StateDatabase = effectiveStateDatabase,
                            is_cdc_admin = isCdcAdmin,
                            Role = effectiveRole
                        };

                        model.CaseStatusDetail.Add(x);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }


        return View(model);
    }
    [HttpPost]
    public IActionResult ConfirmClearCaseStatusRequest(
        [Bind(
            nameof(mmria.server.model.casestatus.CaseStatusDetail._id) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.RecordId) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.FirstName) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.LastName) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.MiddleName) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.DateOfDeath) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.StateOfDeath) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.LastUpdatedBy) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.DateLastUpdated) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.CaseStatus) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.CaseStatusDisplay) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.StateDatabase))]
        mmria.server.model.casestatus.CaseStatusDetail Model)
    {
        var model = Model ?? new mmria.server.model.casestatus.CaseStatusDetail();
        model.is_cdc_admin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
        model.Role = model.is_cdc_admin ? "cdc_admin" : "jurisdiction_admin";
        model.StateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, model.StateDatabase, host_prefix, _dbConfigSet);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ClearCaseStatus(
        [Bind(
            nameof(mmria.server.model.casestatus.CaseStatusDetail._id) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.RecordId) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.FirstName) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.LastName) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.MiddleName) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.DateOfDeath) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.StateOfDeath) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.LastUpdatedBy) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.DateLastUpdated) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.CaseStatus) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.CaseStatusDisplay) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusDetail.StateDatabase))]
        mmria.server.model.casestatus.CaseStatusDetail Model)
    {
        var model = Model ?? new mmria.server.model.casestatus.CaseStatusDetail();
        var isCdcAdmin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
        var effectiveRole = isCdcAdmin ? "cdc_admin" : "jurisdiction_admin";
        var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, model.StateDatabase, host_prefix, _dbConfigSet);
        var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, model.StateDatabase, host_prefix, db_config, _dbConfigSet);
        model.is_cdc_admin = isCdcAdmin;
        model.Role = effectiveRole;
        model.StateDatabase = effectiveStateDatabase;

        try
        {

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }


            var (ok, oldCaseStatus, errorMessage) = await _manager.ClearCaseStatusAsync(effectiveDbConfig, model._id, userName);

            if (ok)
            {
                model.CaseStatusDisplay = "(blank)";
                model.LastUpdatedBy = userName;
                model.DateLastUpdated = DateTime.Now;
            }
            else
            {
                model.CaseStatusDisplay = errorMessage ?? "Problem Setting Status to (blank)";
            }
            
        }
        catch(Exception ex)
        {
            model.CaseStatusDisplay = ex.ToString();
        }

        return View(model);
    }

}
