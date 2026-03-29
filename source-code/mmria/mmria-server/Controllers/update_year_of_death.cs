using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

using mmria.server.extension;
namespace mmria.server.Controllers;

[Authorize(Roles = "cdc_admin,jurisdiction_admin")]
public sealed class update_year_of_deathController : Controller
{
  mmria.common.couchdb.OverridableConfiguration configuration;
  mmria.common.couchdb.DBConfigurationDetail db_config;
  string host_prefix = null;
  private readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;
  private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;


  private System.Collections.Generic.Dictionary<string, string> YearOfDeathToDisplay;
  public update_year_of_deathController
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

    if (_dbConfigSet.detail_list.ContainsKey("vital_import"))
    {
      _dbConfigSet.detail_list.Remove("vital_import");
    }

    YearOfDeathToDisplay = new System.Collections.Generic.Dictionary<string, string>();
    YearOfDeathToDisplay["9999"] = "(blank)";
    YearOfDeathToDisplay["1"] = "Abstracting (Incomplete)";
    YearOfDeathToDisplay["2"] = "Abstraction Complete";
    YearOfDeathToDisplay["3"] = "Ready for Review";
    YearOfDeathToDisplay["4"] = "Review Complete and Decision Entered";
    YearOfDeathToDisplay["5"] = "Out of Scope and Death Certificate Entered";
    YearOfDeathToDisplay["6"] = "False Positive and Death Certificate Entered";
    YearOfDeathToDisplay["0"] = "Vitals Import";
  }
  public IActionResult Index()
  {
    return View(_dbConfigSet);
  }


  public async Task<IActionResult> FindRecord(mmria.server.model.year_of_death.YearOfDeathRequest Model)
  {
    var model = new mmria.server.model.year_of_death.YearOfDeathRequestResponse();
    model.SearchText = Model.RecordId;
    TempData["YearOfDeathSearchRecordId"] = model.SearchText;
    try
    {
      var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient);

      var items = await caseManager.FindYearOfDeathRecordsAsync(
      Model.RecordId,
      Model.Role,
      Model.StateDatabase,
      db_config,
      _dbConfigSet
      );

      foreach (var item in items)
      {
      var x = new mmria.server.model.year_of_death.YearOfDeathDetail()
      {
          _id = item.id,
          RecordId = item.value?.record_id,
          AgencyCaseId = item.value?.agency_case_id,
          LocalFileNumber = item.value?.local_file_number,
          StateFileNumber = item.value?.state_file_number,
          FirstName = item.value?.first_name,
          LastName = item.value?.last_name,
          MiddleName = item.value?.middle_name,
          DateOfDeath = $"{item.value?.date_of_death_month}/{item.value?.date_of_death_day}/{item.value?.date_of_death_year}",
          StateOfDeath = item.value?.host_state,
          LastUpdatedBy = item.value?.last_updated_by,
          DateLastUpdated = item.value?.date_last_updated,
          YearOfDeath = item.value.date_of_death_year,
        StateDatabase = Model.StateDatabase,
          CaseStatus = item.value.case_status,
        Role = Model.Role
      };

      model.YearOfDeathDetail.Add(x);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex);
    }


    return View(model);
  }

  public async Task<IActionResult> ConfirmUpdateYearOfDeathRequest(mmria.server.model.year_of_death.YearOfDeathDetail Model)
  {
    var model = Model;

    var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient);
    Model.RecordIdReplacement = await caseManager.GetRecordIdReplacementForYearOfDeathAsync(
      Model.Role,
      Model.StateDatabase,
      Model.RecordId,
      Model.YearOfDeathReplacement,
      _dbConfigSet
    );

    return View(model);
  }

  public async Task<IActionResult> UpdateYearOfDeath(mmria.server.model.year_of_death.YearOfDeathDetail Model)
  {
    var model = Model;
    try
    {
      var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient);

      // Best-effort: tab id is generated client-side per browser tab and posted
      // with the confirmation form. Used to enforce same-user/different-tab locks.
      var tabId = HttpContext?.Request?.Form["tab_id"].FirstOrDefault();
      if (string.IsNullOrWhiteSpace(tabId))
      {
        tabId = HttpContext?.Request?.Query["tab_id"].FirstOrDefault();
      }

      var updateResult = await caseManager.UpdateYearOfDeathAsync(
        Model._id,
        Model.Role,
        Model.StateDatabase,
        Model.YearOfDeathReplacement,
        Model.RecordIdReplacement,
        Model.DateOfDeath,
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

            if (Model.Role != null && Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
            {
              var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
              caseJson = await dal.GetCaseDocumentJsonAsync(Model._id, db_info);
            }
            else
            {
              caseJson = await dal.GetCaseDocumentJsonAsync(Model._id, db_config);
            }

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
        Model.LastUpdatedBy = updateResult.LastUpdatedBy;
        Model.DateLastUpdated = updateResult.DateLastUpdated;
        Model.DateOfDeath = updateResult.DateOfDeath;
      }

      model.StatusText = updateResult?.StatusText;
    }
    catch (Exception ex)
    {
      model.StatusText = ex.ToString();
    }
    return View(model);
  }

}
