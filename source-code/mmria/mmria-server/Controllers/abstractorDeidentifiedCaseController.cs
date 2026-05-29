using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.server.util;
using mmria.common.SharedLibraries.MetadataVersion.Manager;
namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
public sealed class abstractorDeidentifiedCaseController : Controller
{
    public class DuplicateMultiformResult
    {
        
        public string _id {get;set;}
        public System.Collections.Generic.HashSet<string> field_list{ get; set;}
    }

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly MetadataVersionManager _metadataVersionManager;

    public abstractorDeidentifiedCaseController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        MetadataVersionManager metadataVersionManager
    )
    {
        _metadataVersionManager = metadataVersionManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }
        
    public IActionResult Index()
    {
        var configuredLockMinutes = configuration.GetInteger("case_edit_inactivity_lock_minutes", host_prefix) ?? 120;
        var configuredWarningMinutes = configuration.GetInteger("case_edit_inactivity_warning_minutes_before_lock", host_prefix) ?? 110;
        var sessionIdleTimeoutMinutes = SessionTimeoutHelper.GetSessionIdleTimeoutMinutes(
            configuration,
            configuration,
            host_prefix);
        var effectiveInactivityConfig = CaseEditInactivityConfigHelper.GetEffectiveMinutes(
            configuredLockMinutes,
            configuredWarningMinutes,
            sessionIdleTimeoutMinutes);

        TempData["metadata_version"] = configuration.GetString("metadata_version", host_prefix);
        TempData["case_edit_inactivity_lock_minutes"] = effectiveInactivityConfig.LockMinutes;
        TempData["case_edit_inactivity_warning_minutes_before_lock"] = effectiveInactivityConfig.WarningMinutes;
        TempData["case_edit_auto_save_freq"] = configuration.GetInteger("case_edit_auto_save_freq", host_prefix) ?? 2;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDuplicateMultiFormList()
    {
        var result = new DuplicateMultiformResult();

        try
        {
            string responseFromServer = await _metadataVersionManager.GetDuplicateMultiformListJsonAsync(db_config);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<DuplicateMultiformResult>(responseFromServer);

        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
        }


        return EscapedJsonResultFactory.Create(result);
    }

}
