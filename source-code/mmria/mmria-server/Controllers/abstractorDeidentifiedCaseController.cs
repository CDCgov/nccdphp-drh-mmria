using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.MetadataVersion;
using  mmria.server.extension; 
using mmria.server.util;
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
    private readonly IMetadataRepository _metadataRepository;

    public abstractorDeidentifiedCaseController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        IMetadataRepository metadataRepository
    )
    {
        _metadataRepository = metadataRepository;
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
        TempData["omb_expiration_date"] = configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";
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
            string responseFromServer = await _metadataRepository.GetDuplicateMultiFormListAsync(db_config);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<DuplicateMultiformResult>(responseFromServer);

        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
        }


        return EscapedJsonResultFactory.Create(result);
    }

}
