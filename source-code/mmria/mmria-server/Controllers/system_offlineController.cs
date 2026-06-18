using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.server.util;

namespace mmria.server.Controllers;

[Route("system-offline/{action=Index}")]
public sealed class system_offlineController : Controller
{
    private readonly mmria.common.couchdb.ConfigurationSet ConfigDB;
    private readonly mmria.common.couchdb.OverridableConfiguration configuration;
    private readonly string host_prefix;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public system_offlineController(
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        ConfigDB = tenantRuntime.RequireConfigurationSet();
        _couchDbHttpClient = couchDbHttpClient;
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
    [HttpPost]
    public async Task<IActionResult> SaveConfig()
    {
        var result = new mmria.common.model.couchdb.document_put_response { ok = false };

        try
        {
            var request = await JsonRequestBodyReader.ReadAsync<mmria.common.metadata.SystemOfflineConfig>(Request);

            // Sanitize: discard any client-supplied _rev and data_type.
            // mmria-services will resolve the real revision server-side.
            var sanitized = new mmria.common.metadata.SystemOfflineConfig
            {
                _rev = null,
                warn_date = request?.warn_date,
                warn_message = request?.warn_message,
                offline_date = request?.offline_date,
                offline_modal_message = request?.offline_modal_message,
                offline_page_message = request?.offline_page_message
            };

            var servicesBaseUrl = GetServicesBaseUrl();
            var postUrl = $"{servicesBaseUrl}/api/systemOffline/SaveSystemOfflineConfig";

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(sanitized, settings);

            var requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                VitalServiceKey = ConfigDB.name_value["vital_service_key"]
            };

            var responseBody = await _couchDbHttpClient.ExecuteAsync(
                "POST", postUrl, json, "application/json", requestOptions);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseBody)
                ?? result;
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
        var status = new
        {
            config.warn_date,
            config.offline_date,
            config.warn_message,
            config.offline_modal_message,
            config.offline_page_message
        };
        return EscapedJsonResultFactory.Create(status);
    }

    private async Task<mmria.common.metadata.SystemOfflineConfig> LoadConfigFromServicesAsync()
    {
        var result = new mmria.common.metadata.SystemOfflineConfig();

        try
        {
            var servicesBaseUrl = GetServicesBaseUrl();
            var getUrl = $"{servicesBaseUrl}/api/systemOffline/GetSystemOfflineConfig";

            var requestOptions = new mmria.common.getset.CouchDbRequestOptions
            {
                VitalServiceKey = ConfigDB.name_value["vital_service_key"]
            };

            var responseBody = await _couchDbHttpClient.ExecuteAsync(
                "GET", getUrl, null, "application/json", requestOptions);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(responseBody)
                ?? new mmria.common.metadata.SystemOfflineConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"system_offlineController.LoadConfigFromServicesAsync error: {ex}");
        }

        return result;
    }

    private string GetServicesBaseUrl()
    {
        return configuration.GetString("vitals_url", host_prefix)
            .Replace("/api/Message/IJESet", string.Empty);
    }
}
