using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.MetadataVersion;
using mmria.services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public async Task<IActionResult> SaveSystemOfflineConfig()
    {
        var result = new mmria.common.model.couchdb.document_put_response { ok = false };

        try
        {
            var request = await ReadSaveSystemOfflineConfigRequestAsync();
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

    private async Task<SaveSystemOfflineConfigRequest> ReadSaveSystemOfflineConfigRequestAsync()
    {
        if (Request?.Body == null)
        {
            return new SaveSystemOfflineConfigRequest();
        }

        if (Request.Body.CanSeek)
        {
            Request.Body.Position = 0;
        }

        using var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();

        if (Request.Body.CanSeek)
        {
            Request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new SaveSystemOfflineConfigRequest();
        }

        try
        {
            var json = JObject.Parse(body);
            return new SaveSystemOfflineConfigRequest
            {
                warn_date = ReadString(json, "warn_date"),
                warn_message = ReadString(json, "warn_message"),
                offline_date = ReadString(json, "offline_date"),
                offline_modal_message = ReadString(json, "offline_modal_message"),
                offline_page_message = ReadString(json, "offline_page_message"),
                apply_to_all_jurisdictions = ReadValue(json, "apply_to_all_jurisdictions", true),
                selected_jurisdictions = ReadStringList(json, "selected_jurisdictions"),
                restoration_hours = ReadValue(json, "restoration_hours", 2),
                auto_logout_minutes = ReadValue(json, "auto_logout_minutes", 5)
            };
        }
        catch (JsonException)
        {
            return new SaveSystemOfflineConfigRequest();
        }
    }

    private static string ReadString(JObject json, string propertyName)
    {
        return TryGetProperty(json, propertyName, out var token)
            ? token.Type == JTokenType.Null ? null : token.Value<string>()
            : null;
    }

    private static List<string> ReadStringList(JObject json, string propertyName)
    {
        if (!TryGetProperty(json, propertyName, out var token) || token.Type != JTokenType.Array)
        {
            return new List<string>();
        }

        return token.Values<string>()
            .Where(value => value != null)
            .ToList();
    }

    private static T ReadValue<T>(JObject json, string propertyName, T defaultValue)
    {
        if (!TryGetProperty(json, propertyName, out var token) || token.Type == JTokenType.Null)
        {
            return defaultValue;
        }

        try
        {
            return token.ToObject<T>();
        }
        catch (JsonException)
        {
            return defaultValue;
        }
    }

    private static bool TryGetProperty(JObject json, string propertyName, out JToken token)
    {
        return json.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out token);
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
