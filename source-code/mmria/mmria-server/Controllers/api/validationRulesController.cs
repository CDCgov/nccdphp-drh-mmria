using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.CaseValidation.Manager;
using mmria.common.SharedLibraries.CaseValidation.Model;
using mmria.common.SharedLibraries.MetadataVersion.Manager;
using mmria.server.extension;
using mmria.server.util;

namespace mmria.server.Controllers.api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ValidationRulesController : Controller
{
    private readonly CaseValidationManager _caseValidationManager;
    private readonly MetadataVersionManager _metadataVersionManager;
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly string _hostPrefix;

    public ValidationRulesController(
        CaseValidationManager caseValidationManager,
        MetadataVersionManager metadataVersionManager,
        IHttpContextAccessor httpContextAccessor,
        RequestTenantRuntime tenantRuntime)
    {
        _caseValidationManager = caseValidationManager;
        _metadataVersionManager = metadataVersionManager;
        _hostPrefix = tenantRuntime.EffectiveHostPrefix;
        _configuration = tenantRuntime.RequireConfiguration();
        _dbConfig = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        try
        {
            var metadataVersion = _configuration.GetString("metadata_version", _hostPrefix);
            if (string.IsNullOrWhiteSpace(metadataVersion))
            {
                return BadRequest(new { error = "Metadata version not configured" });
            }

            var metadata = await _metadataVersionManager.GetAppMetadataAsync(metadataVersion, _dbConfig);
            if (metadata == null)
            {
                return BadRequest(new { error = "Metadata not found for version" });
            }

            // Get or create the validation rules document
            var ruleDocument = await _caseValidationManager.GetOrCreateRuleDocumentAsync(
                metadataVersion,
                metadata,
                _dbConfig,
                "api-request");

            if (ruleDocument == null)
            {
                return NotFound(new { error = "Validation rules document not found or could not be created" });
            }

            // Convert field_rules to a dictionary keyed by field_path — only numeric range rules
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
                        rationale = rule.rationale,
                        explanation = rule.explanation,
                        enabled = rule.enabled,
                        category = rule.category
                    },
                    StringComparer.OrdinalIgnoreCase);

            return Ok(fieldRulesByPath);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error in ValidationRulesController.Index: {ex}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}
