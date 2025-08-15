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


  private System.Collections.Generic.Dictionary<string, string> YearOfDeathToDisplay;
  public update_year_of_deathController
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
    try
    {
      string responseFromServer = null;

      if (Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
      {
        var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
        string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true";
        var case_view_curl = new cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
        responseFromServer = await case_view_curl.executeAsync();

      }
      else
      {
        string request_string = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true";
        var case_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
        responseFromServer = await case_view_curl.executeAsync();
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
              DateOfDeath = $"{item.value?.date_of_death_month}/{item.value.date_of_death_year}",
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

      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex);
    }


    return View(model);
  }

  public IActionResult ConfirmUpdateYearOfDeathRequest(mmria.server.model.year_of_death.YearOfDeathDetail Model)
  {
    var model = Model;
    string server_url = db_config.url;
    string user_name = db_config.user_name;
    string user_value = db_config.user_value;
    string prefix = "";
    common.couchdb.DBConfigurationDetail db_info = null;

    if (Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
    {
      db_info = _dbConfigSet.detail_list[Model.StateDatabase];
    }
    else if (Model.Role.Equals("jurisdiction_admin", StringComparison.OrdinalIgnoreCase))
    {
      db_info = _dbConfigSet.detail_list[Model.StateDatabase];
    }
    server_url = db_info.url;
    prefix = db_info.prefix;
    user_name = db_info.user_name;
    user_value = db_info.user_value;
    HashSet<string> ExistingRecordIds = GetExistingRecordIds(server_url, user_name, user_value, prefix);
    var array = Model.RecordId.Split('-');
    string record_id = $"{array[0]}-{Model.YearOfDeathReplacement}-{array[2]}";
    System.Console.WriteLine($"ExistingRecordIds.Count{ExistingRecordIds.Count}");
    while (ExistingRecordIds.Contains(record_id))
    {
      record_id = $"{array[0]}-{Model.YearOfDeathReplacement}-{GenerateRandomFourDigits().ToString()}";
    };
    Model.RecordIdReplacement = record_id;
    return View(model);
  }

  public async Task<IActionResult> UpdateYearOfDeath(mmria.server.model.year_of_death.YearOfDeathDetail Model)
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
      if (Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
      {
        var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
        string request_string = $"{db_info.url}/{db_info.prefix}mmrds/{Model._id}";
        var case_view_curl = new cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
        responseFromServer = await case_view_curl.executeAsync();
      }
      else
      {
        string request_string = $"{db_config.url}/{db_config.prefix}mmrds/{Model._id}";
        var case_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
        responseFromServer = await case_view_curl.executeAsync();
      }

        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = { 
                new mmria.server.utils.TimeOnlyJsonConverter(), 
                new mmria.server.utils.DateOnlyJsonConverter() 
            }

            // HH:MM
        };
      // var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
      var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.case_version.v250616.mmria_case>(responseFromServer, settings);

      if (model.YearOfDeathReplacement.HasValue)
        case_response.home_record.date_of_death.year = model.YearOfDeathReplacement.Value;

      if (!string.IsNullOrWhiteSpace(model.RecordIdReplacement))
        case_response.home_record.record_id = model.RecordIdReplacement;

      case_response.last_updated_by = userName;
      case_response.date_last_updated = DateTime.Now;

      Model.LastUpdatedBy = userName;
      Model.DateLastUpdated = case_response.date_last_updated;

      List<string> date_of_death_sections = Model.DateOfDeath.Length > 0
        ? new List<string>(Model.DateOfDeath.Split("/"))
        : new List<string>();

      if (date_of_death_sections.Count == 3)
        date_of_death_sections[2] = Model.YearOfDeathReplacement.Value.ToString();
      else if (date_of_death_sections.Count == 2)
        date_of_death_sections[1] = Model.YearOfDeathReplacement.Value.ToString();
      else
        date_of_death_sections.Add(Model.YearOfDeathReplacement.Value.ToString());

      Model.DateOfDeath = String.Join("/", date_of_death_sections);

      settings = new Newtonsoft.Json.JsonSerializerSettings();
      settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
      var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_response, settings);

      cURL document_curl = null;

      if (Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
      {
        var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
        string request_string = $"{db_info.url}/{db_info.prefix}mmrds/{Model._id}";
        document_curl = new cURL("PUT", null, request_string, object_string, db_info.user_name, db_info.user_value);
      }
      else
      {
        string request_string = $"{db_config.url}/{db_config.prefix}mmrds/{Model._id}";
        document_curl = new cURL("PUT", null, request_string, object_string, db_config.user_name, db_config.user_value);
      }
      var document_put_response = new mmria.common.model.couchdb.document_put_response();
      try
      {
        responseFromServer = await document_curl.executeAsync();
        document_put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
      }
      catch (Exception ex)
      {
        model.StatusText = $"Problem Setting Status to (blank)\n{ex}";
      }
      if (document_put_response.ok)
      {
        model.StatusText = "(blank)";
      }
      else
      {
        model.StatusText = "Problem Setting Status to (blank)";
      }
    }
    catch (Exception ex)
    {
      model.StatusText = ex.ToString();
    }
    return View(model);
  }

  public HashSet<string> GetExistingRecordIds(string p_server_url, string user_name, string user_value, string p_prefix = "")
  {
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    try
    {
      string request_string;
      if (string.IsNullOrWhiteSpace(p_prefix))
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

  int my_count = -1;
  private int GenerateRandomFourDigits()
  {
    int _min = 1000;
    int _max = 9999;
    Random _rdm = new Random(System.DateTime.Now.Millisecond + my_count);
    my_count++;
    return _rdm.Next(_min, _max);

  }

}
