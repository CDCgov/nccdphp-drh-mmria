using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Linq;

using  mmria.server.extension; 
using mmria.server.util;
using mmria.common.SharedLibraries.CaseValidation.Manager;
using mmria.common.SharedLibraries.MetadataVersion.Manager;
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
    private readonly CaseValidationManager _caseValidationManager;
    private readonly MetadataVersionManager _metadataVersionManager;

    public CaseController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        CaseValidationManager caseValidationManager,
        MetadataVersionManager metadataVersionManager
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _caseValidationManager = caseValidationManager;
        _metadataVersionManager = metadataVersionManager;
        
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        
        configuration = tenantRuntime.RequireConfiguration();
        
        db_config = tenantRuntime.RequireDbConfig();
    }
        
    public async Task<IActionResult> Index()
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
        TempData["is_offline_mode_enabled"] = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        TempData["offline_mode_max_new_cases"] = configuration.GetInteger("offline_mode_max_new_cases", host_prefix) ?? 3;
        TempData["offline_mode_max_existing_cases"] = configuration.GetInteger("offline_mode_max_existing_cases", host_prefix) ?? 3;
        TempData["is_offline_logging_enabled"] = configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        TempData["offline_logging_max_logs"] = configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;
        TempData["case_edit_inactivity_lock_minutes"] = effectiveInactivityConfig.LockMinutes;
        TempData["case_edit_inactivity_warning_minutes_before_lock"] = effectiveInactivityConfig.WarningMinutes;
        TempData["case_edit_auto_save_freq"] = configuration.GetInteger("case_edit_auto_save_freq", host_prefix) ?? 2;

        // Get validation rules (keyed by field_path)
        try
        {
            var metadataVersion = configuration.GetString("metadata_version", host_prefix);
            if (!string.IsNullOrEmpty(metadataVersion))
            {
                var metadata = await _metadataVersionManager.GetAppMetadataAsync(metadataVersion, db_config);
                if (metadata != null)
                {
                    var ruleDocument = await _caseValidationManager.GetOrCreateRuleDocumentAsync(
                        metadataVersion,
                        metadata,
                        db_config,
                        User?.Identity?.Name ?? "system");
                    
                    if (ruleDocument?.field_rules != null && ruleDocument.field_rules.Count > 0)
                    {
                        var fieldRulesByPath = ruleDocument.field_rules
                            .Where(rule => rule.enabled == true && rule.min_value.HasValue && rule.max_value.HasValue)
                            .ToDictionary(
                                rule => rule.field_path,
                                rule => new
                                {
                                    id = rule.id,
                                    field_path = rule.field_path,
                                    min_value = rule.min_value,
                                    max_value = rule.max_value,
                                    severity = rule.severity,
                                    review_status = rule.review_status,
                                    validation_level = rule.validation_level,
                                    confidence = rule.confidence,
                                    source = rule.source,
                                    unit = rule.unit,
                                    message = rule.message,
                                    enabled = rule.enabled,
                                    category = rule.category
                                },
                                StringComparer.OrdinalIgnoreCase);
                        
                        TempData["validation_rules"] = JsonSerializer.Serialize(fieldRulesByPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting validation rules: {ex}");
            // Fail gracefully - leave validation_rules unset
        }

        TempData["omb_expiration_date"] = configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";

        ViewBag.is_offline_mode_enabled = configuration.GetBoolean("is_offline_mode_enabled", host_prefix) ?? false;
        ViewBag.is_offline_logging_enabled = configuration.GetBoolean("is_offline_logging_enabled", host_prefix) ?? false;
        ViewBag.offline_logging_max_logs = configuration.GetInteger("offline_logging_max_logs", host_prefix) ?? 10000;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDuplicateMultiFormList()
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
