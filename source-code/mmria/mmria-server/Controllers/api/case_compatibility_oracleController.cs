#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.metadata;
using mmria.server.util;
using Newtonsoft.Json;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class case_compatibility_oracleController : ControllerBase
{
    private const string DefaultMetadataVersion = "25.08.14";

    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly string _hostPrefix;

    public case_compatibility_oracleController(
        RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _configuration = tenantRuntime.RequireConfiguration();
        _dbConfig = tenantRuntime.RequireDbConfig();
        _couchDbHttpClient = couchDbHttpClient;
        _hostPrefix = tenantRuntime.EffectiveHostPrefix;
    }

    [Authorize(Roles = "abstractor, data_analyst")]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get(string case_id, string metadata_version = DefaultMetadataVersion)
    {
        if (!IsEnabled())
        {
            return NotFound(new
            {
                error = "case_compatibility_oracle_disabled",
                message = "Set MMRIA_ENABLE_CASE_COMPATIBILITY_ORACLE=1 to enable the case compatibility oracle."
            });
        }

        if (string.IsNullOrWhiteSpace(case_id))
        {
            return BadRequest(new { error = "case_id_required" });
        }

        var effectiveMetadataVersion = string.IsNullOrWhiteSpace(metadata_version)
            ? DefaultMetadataVersion
            : metadata_version.Trim();

        if (!IsSafeMetadataVersion(effectiveMetadataVersion))
        {
            return BadRequest(new { error = "invalid_metadata_version" });
        }

        try
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var rawCaseJson = await FetchCaseJsonAsync(case_id.Trim());
            var metadata = await FetchMetadataAsync(effectiveMetadataVersion);
            var canonicalCase = CaseCompatibilityOracleCanonicalizer.Canonicalize(rawCaseJson, metadata);

            return Content(canonicalCase.ToString(Formatting.None), "application/json");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new
            {
                error = "case_compatibility_oracle_failed",
                message = ex.Message
            });
        }
    }

    private async Task<string> FetchCaseJsonAsync(string caseId)
    {
        var requestUrl = _dbConfig.Get_Prefix_DB_Url($"mmrds/{Uri.EscapeDataString(caseId)}");
        return await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            _dbConfig.user_name,
            _dbConfig.user_value,
            "application/json");
    }

    private async Task<app> FetchMetadataAsync(string metadataVersion)
    {
        var requestUrl = $"{_dbConfig.url}/metadata/version_specification-{Uri.EscapeDataString(metadataVersion)}/metadata";
        var response = await _couchDbHttpClient.ExecuteAsync(
            "GET",
            requestUrl,
            null,
            _dbConfig.user_name,
            _dbConfig.user_value,
            "application/json");

        return JsonConvert.DeserializeObject<app>(response)
            ?? throw new InvalidOperationException($"Metadata version {metadataVersion} returned an empty metadata document.");
    }

    private bool IsEnabled()
    {
        var envValue = Environment.GetEnvironmentVariable("MMRIA_ENABLE_CASE_COMPATIBILITY_ORACLE");
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return IsTruthy(envValue);
        }

        var configBoolean = TryGetConfigurationBoolean("enable_case_compatibility_oracle");
        if (configBoolean.HasValue)
        {
            return configBoolean.Value;
        }

        var configString = TryGetConfigurationString("enable_case_compatibility_oracle");
        return !string.IsNullOrWhiteSpace(configString) && IsTruthy(configString);
    }

    private bool? TryGetConfigurationBoolean(string key)
    {
        try
        {
            return _configuration.GetBoolean(key, _hostPrefix);
        }
        catch
        {
            return null;
        }
    }

    private string TryGetConfigurationString(string key)
    {
        try
        {
            return _configuration.GetString(key, _hostPrefix);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsTruthy(string value)
    {
        return value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeMetadataVersion(string metadataVersion)
    {
        return metadataVersion.Length <= 64 &&
               metadataVersion.All(character =>
                   char.IsLetterOrDigit(character) ||
                   character == '.' ||
                   character == '-' ||
                   character == '_');
    }
}
#endif
