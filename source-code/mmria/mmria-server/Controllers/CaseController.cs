using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.server.util;
namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
public sealed class CaseController : Controller
{
    public class DuplicateMultiformResult
    {
        
        public string _id {get;set;}
        public System.Collections.Generic.HashSet<string> field_list{ get; set;}
    }

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public CaseController
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
    }
        
    public IActionResult Index()
    {

        TempData["metadata_version"] = configuration.GetString("metadata_version", host_prefix);
        TempData["is_offline_mode_enabled"] = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        TempData["offline_mode_max_new_cases"] = configuration.GetInteger("offline_mode_max_new_cases", host_prefix) ?? 3;
        TempData["offline_mode_max_existing_cases"] = configuration.GetInteger("offline_mode_max_existing_cases", host_prefix) ?? 3;
        TempData["is_offline_logging_enabled"] = configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        TempData["offline_logging_max_logs"] = configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;
        TempData["case_edit_inactivity_lock_minutes"] = configuration.GetInteger("case_edit_inactivity_lock_minutes", host_prefix) ?? 120;
        TempData["case_edit_inactivity_warning_minutes_before_lock"] = configuration.GetInteger("case_edit_inactivity_warning_minutes_before_lock", host_prefix) ?? 110;
        TempData["case_edit_auto_save_freq"] = configuration.GetInteger("case_edit_auto_save_freq", host_prefix) ?? 2;
        ViewBag.is_offline_mode_enabled = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        ViewBag.is_offline_logging_enabled = configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        ViewBag.offline_logging_max_logs = configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetDuplicateMultiFormList()
    {
        var result = new DuplicateMultiformResult();

        try
        {
            string request_string = $"{db_config.url}/metadata/duplicate-multiform-list";

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<DuplicateMultiformResult>(responseFromServer);

        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
        }


        return EscapedJsonResultFactory.Create(result);
    }

}
