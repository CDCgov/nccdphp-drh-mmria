using System;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.metadata;
using mmria.common.SharedLibraries.CaseValidation.Model;
using mmria.server.extension;
using mmria.server.util;
using Newtonsoft.Json;

namespace mmria.server;

[Route("api/case-validation")]
public sealed class case_validationController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly string _hostPrefix;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    private readonly mmria.common.SharedLibraries.CaseValidation.Manager.CaseValidationManager _caseValidationManager;

    public case_validationController(
        RequestTenantRuntime tenantRuntime,
        ActorSystem actorSystem,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager metadataVersionManager,
        mmria.common.SharedLibraries.CaseValidation.Manager.CaseValidationManager caseValidationManager)
    {
        _configuration = tenantRuntime.RequireConfiguration();
        _dbConfig = tenantRuntime.RequireDbConfig();
        _hostPrefix = tenantRuntime.EffectiveHostPrefix;
        _actorSystem = actorSystem;
        _couchDbHttpClient = couchDbHttpClient;
        _metadataVersionManager = metadataVersionManager;
        _caseValidationManager = caseValidationManager;
    }

    [Authorize(Roles = "abstractor, data_analyst, form_designer")]
    [HttpGet("rules/current")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CaseValidationRuleDocument>> GetCurrentRules()
    {
        try
        {
            var metadataVersion = GetCurrentMetadataVersion();
            var metadata = await GetCurrentMetadataAsync(metadataVersion);
            var rules = await _caseValidationManager.GetOrCreateRuleDocumentAsync(
                metadataVersion,
                metadata,
                _dbConfig,
                GetCurrentUserName());

            return Ok(rules);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, message = ex.Message });
        }
    }

    [Authorize(Roles = "form_designer")]
    [HttpGet("rules/current/summary")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CaseValidationRuleSummary>> GetCurrentRulesSummary()
    {
        try
        {
            var metadataVersion = GetCurrentMetadataVersion();
            var metadata = await GetCurrentMetadataAsync(metadataVersion);
            var rules = await _caseValidationManager.GetOrCreateRuleDocumentAsync(
                metadataVersion,
                metadata,
                _dbConfig,
                GetCurrentUserName());

            return Ok(_caseValidationManager.BuildRuleSummary(rules));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, message = ex.Message });
        }
    }

    [Authorize(Roles = "form_designer")]
    [HttpGet("rules/current/export")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ExportCurrentRules()
    {
        try
        {
            var metadataVersion = GetCurrentMetadataVersion();
            var metadata = await GetCurrentMetadataAsync(metadataVersion);
            var rules = await _caseValidationManager.GetOrCreateRuleDocumentAsync(
                metadataVersion,
                metadata,
                _dbConfig,
                GetCurrentUserName());

            var fileName = $"case-validation-rules-{metadataVersion}.json";
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            return Content(JsonConvert.SerializeObject(rules, Formatting.Indented), "application/json");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, message = ex.Message });
        }
    }

    [Authorize(Roles = "form_designer")]
    [HttpPut("rules/{metadata_version}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SaveRules(string metadata_version)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(metadata_version))
            {
                return BadRequest(new { ok = false, message = "metadata_version is required." });
            }

            var request = await JsonRequestBodyReader.ReadAsync<CaseValidationRuleDocument>(Request);
            var response = await _caseValidationManager.SaveRuleDocumentAsync(
                metadata_version,
                request,
                _dbConfig,
                GetCurrentUserName());

            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, message = ex.Message });
        }
    }

    [Authorize(Roles = "form_designer")]
    [HttpPost("rules/preview")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PreviewRules()
    {
        try
        {
            var request = await JsonRequestBodyReader.ReadAsync<CaseValidationRulePreviewRequest>(Request);
            var metadataVersion = GetCurrentMetadataVersion();
            var metadata = await GetCurrentMetadataAsync(metadataVersion);
            var result = await _caseValidationManager.PreviewRulesAsync(
                request,
                metadata,
                metadataVersion,
                _dbConfig,
                User);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, error_description = ex.Message });
        }
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost("field")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SaveField()
    {
        try
        {
            var request = await JsonRequestBodyReader.ReadAsync<CaseValidationFieldUpdateRequest>(Request);
            var metadataVersion = GetCurrentMetadataVersion();
            var metadata = await GetCurrentMetadataAsync(metadataVersion);
            var result = await _caseValidationManager.SaveSingleFieldAsync(
                request,
                metadata,
                _dbConfig,
                User,
                _configuration,
                _hostPrefix);

            if (result.ok && !string.IsNullOrWhiteSpace(result.id) && !string.IsNullOrWhiteSpace(result.serialized_case))
            {
                var syncDocumentMessage = new mmria.server.model.actor.Sync_Document_Message(
                    result.id,
                    result.serialized_case,
                    "PUT",
                    metadataVersion);

                _actorSystem.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>(_dbConfig, _couchDbHttpClient, _configuration, _hostPrefix)).Tell(syncDocumentMessage);
            }

            return Ok(new
            {
                ok = result.ok,
                id = result.id,
                rev = result.rev,
                error_description = result.error_description
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { ok = false, error_description = ex.Message });
        }
    }

    private string GetCurrentMetadataVersion()
    {
        return _configuration.GetString("metadata_version", _hostPrefix);
    }

    private async Task<app> GetCurrentMetadataAsync(string metadataVersion)
    {
        var metadataJson = await _metadataVersionManager.GetVersionDocumentAsync(metadataVersion, "metadata", _dbConfig);
        return JsonConvert.DeserializeObject<app>(metadataJson);
    }

    private string GetCurrentUserName()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return User.Identity.Name;
        }

        return null;
    }
}
