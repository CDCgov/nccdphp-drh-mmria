using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles  = "cdc_admin")]
public sealed class update_maiden_nameController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;


    private System.Collections.Generic.Dictionary<string, string> MaidenNameToDisplay;
    public update_maiden_nameController
    (
        mmria.common.couchdb.ConfigurationSet DbConfigurationSet,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {

        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);

        _dbConfigSet = DbConfigurationSet;

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


    public async Task<IActionResult> FindRecord(mmria.server.model.maiden_name.MaidenNameRequest Model)
    {
        var model = new mmria.server.model.maiden_name.MaidenNameRequestResponse();
        model.SearchText = Model.RecordId;
        TempData["MaidenNameSearchRecordId"] = model.SearchText;
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

                        StateDatabase = Model.StateDatabase,

                        CaseStatus = item.value.case_status,

                        Role = Model.Role
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

    public IActionResult ConfirmUpdateMaidenNameRequest(mmria.server.model.maiden_name.MaidenNameDetail Model)
    {
        var model = Model;
        string server_url = db_config.url;
        string user_name = db_config.user_name;
        string user_value = db_config.user_value;
        string prefix = "";

        if(Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
        {
            var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
            server_url = db_info.url;
            prefix = db_info.prefix;
            user_name = db_info.user_name;
            user_value = db_info.user_value;
        }
        return View(model);
    }

    
    public async Task<IActionResult> UpdateMaidenName(mmria.server.model.maiden_name.MaidenNameDetail Model)
    {
        var model = Model;
        try
        {
            var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient);

            var updateResult = await caseManager.UpdateMaidenNameAsync(
                Model._id,
                Model.Role,
                Model.StateDatabase,
                Model.MaidenNameReplacement,
                User,
                db_config,
                _dbConfigSet
            );

            Model.MaidenName = updateResult.MaidenName;
            Model.LastUpdatedBy = updateResult.LastUpdatedBy;
            Model.DateLastUpdated = updateResult.DateLastUpdated;
            model.StatusText = updateResult.StatusText;
            
        }
        catch(Exception ex)
        {
            model.StatusText = ex.ToString();
        }

        return View(model);
    }

    public async Task<HashSet<string>> GetExistingRecordIds(string p_server_url, string user_name,  string user_value, string p_prefix = "")
    {
        var caseManager = new mmria.common.SharedLibraries.Case.Manager.CaseManager(_couchDbHttpClient);

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
