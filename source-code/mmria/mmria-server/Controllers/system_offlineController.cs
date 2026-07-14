using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.SystemOffline.Manager;
using mmria.server.util;

namespace mmria.server.Controllers;

[Route("system-offline/{action=Index}")]
public sealed class system_offlineController : Controller
{
    private readonly mmria.common.couchdb.ConfigurationSet ConfigDB;
    private readonly mmria.common.couchdb.OverridableConfiguration configuration;
    private readonly string host_prefix;
    private readonly SystemOfflineManager _manager;

    public system_offlineController(
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        SystemOfflineManager manager)
    {
        ConfigDB = tenantRuntime.RequireConfigurationSet();
        _manager = manager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
    }

    [Authorize(Roles = "installation_admin")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "installation_admin")]
    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var config = await LoadConfigFromServicesAsync();
        return EscapedJsonResultFactory.Create(config);
    }

    [Authorize(Roles = "installation_admin")]
    [HttpGet]
    public IActionResult GetJurisdictions()
    {
        var jurisdictions = ConfigDB.detail_list.Keys
            .Where(k => !string.Equals(k, "vital_import", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return EscapedJsonResultFactory.Create(jurisdictions);
    }

    [Authorize(Roles = "installation_admin")]
    [HttpPost]
    public async Task<IActionResult> SaveConfig()
    {
        var result = new mmria.common.model.couchdb.document_put_response { ok = false };

        try
        {
            var request = await JsonRequestBodyReader.ReadAsync<mmria.common.metadata.SystemOfflineConfig>(Request);

            var selectedJurisdictions = request?.selected_jurisdictions ?? new System.Collections.Generic.List<string>();

            // Sanitize: discard any client-supplied _rev and data_type.
            var sanitized = new mmria.common.metadata.SystemOfflineConfig
            {
                _rev = null,
                warn_date = request?.warn_date,
                warn_message = request?.warn_message,
                offline_date = request?.offline_date,
                offline_modal_message = request?.offline_modal_message,
                offline_page_message = request?.offline_page_message,
                apply_to_all_jurisdictions = request?.apply_to_all_jurisdictions ?? true,
                selected_jurisdictions = selectedJurisdictions,
                restoration_hours = request?.restoration_hours ?? 2,
                auto_logout_minutes = request?.auto_logout_minutes ?? 5
            };

            var requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                VitalServiceKey = ConfigDB.name_value["vital_service_key"]
            };

            result = await _manager.SaveConfigAsync(sanitized, GetServicesBaseUrl(), requestOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"system_offlineController.SaveConfig error: {ex}");
        }

        return EscapedJsonResultFactory.Create(result);
    }

    [Authorize]
    [HttpGet]
    [Route("~/api/system-offline/status")]
    public async Task<IActionResult> GetStatus()
    {
        var config = await LoadConfigFromServicesAsync();

        // If scoped to specific jurisdictions, check whether this tenant is included.
        if (!config.apply_to_all_jurisdictions)
        {
            var selected = config.selected_jurisdictions ?? new System.Collections.Generic.List<string>();
            var isSelected = selected.Contains(host_prefix, StringComparer.OrdinalIgnoreCase);
            if (!isSelected)
            {
                // Return empty status — this tenant is not in the offline window.
                return EscapedJsonResultFactory.Create(new
                {
                    warn_date = (string)null,
                    offline_date = (string)null,
                    warn_message = (string)null,
                    offline_modal_message = (string)null,
                    offline_page_message = (string)null
                });
            }
        }

        var status = new
        {
            config.warn_date,
            config.offline_date,
            config.auto_logout_minutes,
            warn_message          = _manager.SubstituteMessage(config.warn_message,          config.warn_date, config.offline_date, config.restoration_hours),
            offline_modal_message = _manager.SubstituteMessage(config.offline_modal_message, config.warn_date, config.offline_date, config.restoration_hours),
            offline_page_message  = _manager.SubstituteMessage(config.offline_page_message,  config.warn_date, config.offline_date, config.restoration_hours)
        };
        return EscapedJsonResultFactory.Create(status);
    }

    private async Task<mmria.common.metadata.SystemOfflineConfig> LoadConfigFromServicesAsync()
    {
        try
        {
            var requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                VitalServiceKey = ConfigDB.name_value["vital_service_key"]
            };
            return await _manager.LoadConfigAsync(GetServicesBaseUrl(), requestOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"system_offlineController.LoadConfigFromServicesAsync error: {ex}");
            return new mmria.common.metadata.SystemOfflineConfig();
        }
    }

    private string GetServicesBaseUrl()
    {
        return configuration.GetString("vitals_url", host_prefix)
            .Replace("/api/Message/IJESet", string.Empty);
    }
}
