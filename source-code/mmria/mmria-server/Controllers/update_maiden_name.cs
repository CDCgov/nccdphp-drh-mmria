using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.server.util;
namespace mmria.server.Controllers;

[Authorize(Roles  = "cdc_admin")]
public sealed class update_maiden_nameController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;


    private System.Collections.Generic.Dictionary<string, string> MaidenNameToDisplay;
    public update_maiden_nameController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {

        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _dbConfigSet = tenantRuntime.RequireConfigurationSet();

        if(_dbConfigSet.detail_list.ContainsKey("vital_import"))
        {
            _dbConfigSet.detail_list.Remove("vital_import");
        }

        MaidenNameToDisplay = new System.Collections.Generic.Dictionary<string, string>();
        MaidenNameToDisplay["9999"] = "(blank)";
        MaidenNameToDisplay["1"] = "Abstracting (Incomplete)";	
        MaidenNameToDisplay["2"] = "Abstraction Complete";
        MaidenNameToDisplay["3"] = "Ready for Review";
        MaidenNameToDisplay["4"] = "Review Complete and Decision Entered";
        MaidenNameToDisplay["5"] = "Out of Scope and Death Certificate Entered";
        MaidenNameToDisplay["6"] = "False Positive and Death Certificate Entered";
        MaidenNameToDisplay["0"] = "Vitals Import";
    }
    public IActionResult Index()
    {
        return View(_dbConfigSet);
    }

    [HttpPost]
    public async Task<IActionResult> FindRecord(
        [Bind(
            nameof(mmria.server.model.maiden_name.MaidenNameRequest.StateDatabase) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameRequest.RecordId))]
        mmria.server.model.maiden_name.MaidenNameRequest Model)
    {
        Model ??= new mmria.server.model.maiden_name.MaidenNameRequest();
        var model = new mmria.server.model.maiden_name.MaidenNameRequestResponse();
        model.SearchText = Model.RecordId;
        TempData["MaidenNameSearchRecordId"] = model.SearchText;
        try
        {
            var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
            var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient, new mmria.common.SharedLibraries.Case.DAL.CaseDAL(_couchDbHttpClient));

            var items = await caseManager.FindYearOfDeathRecordsAsync(
                Model.RecordId,
                "cdc_admin",
                effectiveStateDatabase,
                db_config,
                _dbConfigSet
            );

            foreach (var item in items)
            {
                try
                {
                    var x = new mmria.server.model.maiden_name.MaidenNameDetail()
                    {
                        _id = item.id,
                        RecordId = item.value?.record_id,
                        FirstName = item.value?.first_name,
                        LastName = item.value?.last_name,
                        MiddleName = item.value?.middle_name,
                        MaidenName = item.value?.maiden_name,
                        AgencyCaseId = item.value?.agency_case_id,
                        LocalFileNumber = item.value?.local_file_number,
                        StateFileNumber = item.value?.state_file_number,
                        // DateOfDeath = $"{item.value?.date_of_death_month}/{item.value.date_of_death_year}",
                        // StateOfDeath = item.value?.host_state,

                        LastUpdatedBy = item.value?.last_updated_by,

                        DateLastUpdated = item.value?.date_last_updated,

                        // YearOfDeath = item.value.date_of_death_year,

                        StateDatabase = effectiveStateDatabase,

                        CaseStatus = item.value.case_status,

                        Role = "cdc_admin"
                    };

                    model.MaidenNameDetail.Add(x);
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
    public IActionResult ConfirmUpdateMaidenNameRequest(
        [Bind(
            nameof(mmria.server.model.maiden_name.MaidenNameDetail._id) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.RecordId) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.FirstName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.LastName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.MiddleName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.LastUpdatedBy) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.DateLastUpdated) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.MaidenName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.CaseStatus) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.CaseStatusDisplay) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.StateDatabase))]
        mmria.server.model.maiden_name.MaidenNameDetail Model)
    {
        var model = Model ?? new mmria.server.model.maiden_name.MaidenNameDetail();
        model.Role = "cdc_admin";
        model.StateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, model.StateDatabase, host_prefix, _dbConfigSet);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMaidenName(
        [Bind(
            nameof(mmria.server.model.maiden_name.MaidenNameDetail._id) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.RecordId) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.FirstName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.LastName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.MiddleName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.LastUpdatedBy) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.DateLastUpdated) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.MaidenName) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.MaidenNameReplacement) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.CaseStatus) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.CaseStatusDisplay) + "," +
            nameof(mmria.server.model.maiden_name.MaidenNameDetail.StateDatabase))]
        mmria.server.model.maiden_name.MaidenNameDetail Model)
    {
        var model = Model ?? new mmria.server.model.maiden_name.MaidenNameDetail();
        var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, model.StateDatabase, host_prefix, _dbConfigSet);
        model.Role = "cdc_admin";
        model.StateDatabase = effectiveStateDatabase;
        try
        {
            var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient, new mmria.common.SharedLibraries.Case.DAL.CaseDAL(_couchDbHttpClient));

            // Best-effort: tab id is generated client-side per browser tab and posted
            // with the confirmation form. Used to enforce same-user/different-tab locks.
            var tabId = HttpContext?.Request?.Form["tab_id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tabId))
            {
                tabId = HttpContext?.Request?.Query["tab_id"].FirstOrDefault();
            }

            var updateResult = await caseManager.UpdateMaidenNameAsync(
                model._id,
                "cdc_admin",
                effectiveStateDatabase,
                model.MaidenNameReplacement,
                User,
                db_config,
                _dbConfigSet,
                configuration,
                host_prefix,
                currentTabId: tabId
            );

            // If the manager reports a conflict, show a clear message.
            // Note: 409 can be lock-related or offline-related.
            if (updateResult != null && updateResult.StatusCode == 409)
            {
                if (!string.IsNullOrWhiteSpace(updateResult.StatusText) &&
                    updateResult.StatusText.IndexOf("offline", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    model.StatusText = updateResult.StatusText;
                }
                else
                {
                    string lockedBy = null;
                    try
                    {
                        var dal = new mmria.common.SharedLibraries.Case.DAL.CaseDAL(_couchDbHttpClient);
                        string caseJson;

                        var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, effectiveStateDatabase, host_prefix, db_config, _dbConfigSet);
                        caseJson = await dal.GetCaseDocumentJsonAsync(model._id, effectiveDbConfig);

                        var doc = Newtonsoft.Json.Linq.JObject.Parse(caseJson);
                        lockedBy = doc.Value<string>("last_checked_out_by");
                    }
                    catch
                    {
                        // best-effort
                    }

                    if (string.IsNullOrWhiteSpace(lockedBy))
                    {
                        lockedBy = "another user";
                    }

                    model.StatusText = $"The case is currently locked by {lockedBy}. The case cannot be updated.";
                }

                return View(model);
            }

            // Only overwrite display fields on success.
            if (updateResult != null && updateResult.IsSuccessful)
            {
                model.MaidenName = updateResult.MaidenName;
                model.LastUpdatedBy = updateResult.LastUpdatedBy;
                model.DateLastUpdated = updateResult.DateLastUpdated;
            }

            model.StatusText = updateResult?.StatusText;
            
        }
        catch(Exception ex)
        {
            model.StatusText = ex.ToString();
        }

        return View(model);
    }

    public async Task<HashSet<string>> GetExistingRecordIds(string p_server_url, string user_name,  string user_value, string p_prefix = "")
    {
        var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient, new mmria.common.SharedLibraries.Case.DAL.CaseDAL(_couchDbHttpClient));

        var dbInfo = new mmria.common.couchdb.DBConfigurationDetail
        {
            url = p_server_url,
            prefix = p_prefix,
            user_name = user_name,
            user_value = user_value
        };

        return await caseManager.GetExistingRecordIdsAsync(dbInfo);
    }

}
