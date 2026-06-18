using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.utils;

namespace mmria.services.vitalsimport.Controllers;

[Authorize(AuthenticationSchemes = "BasicAuthentication")]
[Route("api/[controller]/[action]")]
[ApiController]
public sealed class systemOfflineController : Controller
{
    private readonly mmria.common.couchdb.ConfigurationSet ConfigDB;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public systemOfflineController(
        mmria.common.couchdb.ConfigurationSet configDB,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        ConfigDB = configDB;
        _couchDbHttpClient = couchDbHttpClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemOfflineConfig()
    {
        var result = new mmria.common.metadata.SystemOfflineConfig();

        try
        {
            var cdcConfig = GetCdcConfig();
            var url = GetCdcMetadataDocUrl(cdcConfig);

            var response = await _couchDbHttpClient.ExecuteForResponseAsync(
                "GET",
                url,
                payload: null,
                userName: cdcConfig.user_name,
                password: cdcConfig.user_value);

            if (response.StatusCode == 404)
                return Ok(result);

            if (response.StatusCode < 200 || response.StatusCode >= 300)
                return Ok(result);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(response.Body)
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
        [FromBody] mmria.common.metadata.SystemOfflineConfig request)
    {
        var result = new mmria.common.model.couchdb.document_put_response { ok = false };

        try
        {
            var cdcConfig = GetCdcConfig();
            var url = GetCdcMetadataDocUrl(cdcConfig);

            // Read current revision from the server-side document.
            mmria.common.metadata.SystemOfflineConfig existing = null;
            try
            {
                var getResponse = await _couchDbHttpClient.ExecuteForResponseAsync(
                    "GET",
                    url,
                    payload: null,
                    userName: cdcConfig.user_name,
                    password: cdcConfig.user_value);

                if (getResponse.StatusCode >= 200 && getResponse.StatusCode < 300)
                {
                    existing = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(getResponse.Body);
                }
            }
            catch (Exception)
            {
                // Treat as non-existent; PUT will create a new document.
            }

            // Sanitize: always use server-owned revision; never trust client-supplied _rev.
            var payload = new mmria.common.metadata.SystemOfflineConfig
            {
                _rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(request?._rev, existing?._rev),
                warn_date = request?.warn_date,
                warn_message = request?.warn_message,
                offline_date = request?.offline_date,
                offline_modal_message = request?.offline_modal_message,
                offline_page_message = request?.offline_page_message
            };

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload, settings);

            var responseBody = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                url,
                json,
                cdcConfig.user_name,
                cdcConfig.user_value);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseBody)
                ?? result;
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

    private static string GetCdcMetadataDocUrl(mmria.common.couchdb.DBConfigurationDetail cdcConfig)
    {
        return $"{cdcConfig.url}/{cdcConfig.prefix}metadata/system-offline-config";
    }
}
