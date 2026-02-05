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
            string responseFromServer = null;

            if (Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
            {
                var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
                string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true";
                responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_info.user_name, db_info.user_value);

            }

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

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
                    /*
                    &&
                    (
                        item.value.case_status.HasValue &&
                        Locked_status_list.IndexOf(item.value.case_status.Value) > -1
                    )*/

                    )
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
            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }
            string responseFromServer = null;
            if(Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
            {
        
                var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
                string request_string = $"{db_info.url}/{db_info.prefix}mmrds/{Model._id}";
                responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_info.user_name, db_info.user_value);
            }
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
            
            //death_certificate/certificate_identification/dmaiden
            var dictionary = case_response as IDictionary<string,object>;
            if(dictionary != null)
            {
                var death_certificate = dictionary["death_certificate"] as IDictionary<string,object>;
                if(death_certificate != null)
                {
                    var certificate_identification = death_certificate["certificate_identification"] as IDictionary<string, object>;
                    if(certificate_identification != null)
                    {
                        // date_of_death["year"] = model.YearOfDeathReplacement.ToString();
                        //Model.MaidenName = Model.MaidenName.Replace(Model.MaidenName.ToString(), Model.MaidenNameReplacement);
                        dictionary["last_updated_by"] = userName;
                        dictionary["date_last_updated"] = DateTime.Now;
                        certificate_identification["dmaiden"] = Model.MaidenNameReplacement;

                        Model.MaidenName = Model.MaidenNameReplacement;
                        Model.LastUpdatedBy = userName;
                        Model.DateLastUpdated = (DateTime) dictionary["date_last_updated"];
                        // Model.DateOfDeath = Model.DateOfDeath.Replace(Model.YearOfDeath.ToString(), Model.YearOfDeathReplacement.ToString());

                        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_response, settings);

                        string put_request_string = "";

                        if(Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
                        {
                            var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
                            put_request_string = $"{db_info.url}/{db_info.prefix}mmrds/{Model._id}";
                        }
                        else
                        {
                            put_request_string = $"{db_config.url}/{db_config.prefix}mmrds/{Model._id}";
                        }

                        var document_put_response = new mmria.common.model.couchdb.document_put_response();
                        try
                        {
                            responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", put_request_string, object_string,
                                Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase) ? _dbConfigSet.detail_list[Model.StateDatabase].user_name : db_config.user_name,
                                Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase) ? _dbConfigSet.detail_list[Model.StateDatabase].user_value : db_config.user_value);
                            document_put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                        }
                        catch(Exception ex)
                        {
                            model.StatusText = $"Problem Setting Status to (blank)\n{ex}";
                        }

                        if(document_put_response.ok)
                        {
                            model.StatusText = "(blank)";
                        }
                        else
                        {
                            model.StatusText = "Problem Setting Status to (blank)";
                        }

                    }
                    else
                    {
                        model.StatusText = "Problem Setting Status to (blank)";
                    }   
                }
                else
                {
                    model.StatusText = "Problem Setting Status to (blank)";
                }
            }
            else
            {
                model.StatusText = "Problem Setting Status to (blank)";
            }
            
        }
        catch(Exception ex)
        {
            model.StatusText = ex.ToString();
        }

        return View(model);
    }

    public async Task<HashSet<string>> GetExistingRecordIds(string p_server_url, string user_name,  string user_value, string p_prefix = "")
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            string request_string;

            if(string.IsNullOrWhiteSpace(p_prefix))
            {
                request_string = $"{p_server_url}/mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";
            }
            else
            {
                request_string = $"{p_server_url}/{p_prefix}mmrds/_design/sortable/_view/by_date_created?skip=0&take=25000";
            }
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, user_name, user_value);

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                result.Add(cvi.value.record_id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

}
