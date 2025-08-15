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
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;


    private System.Collections.Generic.Dictionary<string, string> MaidenNameToDisplay;
    public update_maiden_nameController
    (
        mmria.common.couchdb.ConfigurationSet DbConfigurationSet,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {

        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);

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
        try
        {
            string responseFromServer  = null;

            if(Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
            {
                var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
                string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true";
                var case_view_curl = new cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
                responseFromServer = await case_view_curl.executeAsync();

            }

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            var Locked_status_list = new List<int>(){4,5,6};
            foreach(var item in case_view_response.rows)
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
                var case_view_curl = new cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
                responseFromServer = await case_view_curl.executeAsync();
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

                        cURL document_curl = null;

                        if(Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
                        {
                            var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
                            string request_string = $"{db_info.url}/{db_info.prefix}mmrds/{Model._id}";
                            document_curl = new cURL ("PUT", null, request_string, object_string, db_info.user_name, db_info.user_value);
                        }
                        else
                        {
                            string request_string = $"{db_config.url}/{db_config.prefix}mmrds/{Model._id}";
                            document_curl = new cURL ("PUT", null, request_string, object_string, db_config.user_name, db_config.user_value);
                        }

                        var document_put_response = new mmria.common.model.couchdb.document_put_response();
                        try
                        {
                            responseFromServer = await document_curl.executeAsync();
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

    public HashSet<string> GetExistingRecordIds(string p_server_url, string user_name,  string user_value, string p_prefix = "")
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
            var case_view_curl = new mmria.getset.cURL("GET", null, request_string, null, user_name, user_value);
            string responseFromServer = case_view_curl.execute();

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
