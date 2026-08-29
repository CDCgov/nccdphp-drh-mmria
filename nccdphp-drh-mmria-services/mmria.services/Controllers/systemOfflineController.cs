using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.MetadataVersion;
using mmria.services.Models;

namespace mmria.services.vitalsimport.Controllers;

[Authorize(AuthenticationSchemes = "BasicAuthentication")]
[Route("api/[controller]/[action]")]
[ApiController]
public sealed class systemOfflineController : Controller
{
    private readonly mmria.common.couchdb.ConfigurationSet ConfigDB;
    private readonly IMetadataRepository _metadataRepository;

    public systemOfflineController(
        mmria.common.couchdb.ConfigurationSet configDB,
        IMetadataRepository metadataRepository)
    {
        ConfigDB = configDB;
        _metadataRepository = metadataRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemOfflineConfig()
    {
        var result = new mmria.common.metadata.SystemOfflineConfig();

        try
        {
            var cdcConfig = GetCdcConfig();
            result = await _metadataRepository.GetSystemOfflineConfigAsync(cdcConfig)
                ?? new mmria.common.metadata.SystemOfflineConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"systemOfflineController.GetSystemOfflineConfig error: {ex}");
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSystemOfflineConfig(
        [FromBody] SaveSystemOfflineConfigRequest request)
    {
        var result = new mmria.common.model.couchdb.document_put_response { ok = false };

        try
        {
            var cdcConfig = GetCdcConfig();

            // Read current revision from the server-side document.
            mmria.common.metadata.SystemOfflineConfig existing = null;
            try
            {
                existing = await _metadataRepository.GetSystemOfflineConfigAsync(cdcConfig);
            }
            catch (Exception)
            {
                // Treat as non-existent; PUT will create a new document.
            }

            // Sanitize: always use server-owned revision; client-supplied _rev is
            // not accepted (excluded from SaveSystemOfflineConfigRequest DTO).
            var payload = new mmria.common.metadata.SystemOfflineConfig
            {
                _rev = existing?._rev,
                warn_date = request?.warn_date,
                warn_message = request?.warn_message,
                offline_date = request?.offline_date,
                offline_modal_message = request?.offline_modal_message,
                offline_page_message = request?.offline_page_message,
                apply_to_all_jurisdictions = request?.apply_to_all_jurisdictions ?? true,
                selected_jurisdictions = request?.selected_jurisdictions ?? new System.Collections.Generic.List<string>(),
                restoration_hours = request?.restoration_hours ?? 2,
                auto_logout_minutes = request?.auto_logout_minutes ?? 5
            };

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload, settings);

            result = await _metadataRepository.SaveSystemOfflineConfigAsync(json, cdcConfig) ?? result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"systemOfflineController.SaveSystemOfflineConfig error: {ex}");
        }

        return Ok(result);
    }

    private mmria.common.couchdb.DBConfigurationDetail GetCdcConfig()
    {
        if (ConfigDB.detail_list.ContainsKey("cdc"))
            return ConfigDB.detail_list["cdc"];

        if (ConfigDB.detail_list.ContainsKey("cdcqa"))
            return ConfigDB.detail_list["cdcqa"];

        throw new InvalidOperationException("No CDC instance found in configuration (expected key 'cdc' or 'cdcqa').");
    }
}
