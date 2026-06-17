using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mmria.case_version.v260120;
using mmria.common.couchdb;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Case.Manager;
using mmria.common.SharedLibraries.CaseValidation.DAL;
using mmria.common.SharedLibraries.CaseValidation.Model;
using mmria.common.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.CaseValidation.Manager;

public sealed class CaseValidationManager
{
    private static readonly HashSet<string> ScalarTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "textarea",
        "number",
        "list",
        "date",
        "datetime",
        "time",
        "boolean",
        "jurisdiction",
        "hidden"
    };

    private static readonly HashSet<string> MeaninglessValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "",
        "9999",
        "9998",
        "8888",
        "7777",
        "6666",
        "9999.0",
        "9998.0",
        "8888.0",
        "7777.0",
        "6666.0",
        "(select value)",
        "select value"
    };

    private static readonly Dictionary<string, string> ValidationLevelMeanings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["metadata"] = "Checks the value against metadata shape, type, list, length, regex, or required semantics.",
        ["impossibility"] = "Checks for logically contradictory values that should not be possible together.",
        ["plausibility"] = "Checks for values that may be possible but are unlikely enough to need review.",
        ["timeline"] = "Checks whether dates and events are in the expected chronological order.",
        ["conditional"] = "Checks dependent fields against selected answers, such as Other/specify or yes/no grids.",
        ["form-completeness"] = "Checks whether form status matches meaningful data present in the form."
    };

    private readonly CaseValidationDAL _dal;
    private readonly CaseManager _caseManager;

    public CaseValidationManager(CaseValidationDAL dal, CaseManager caseManager)
    {
        _dal = dal;
        _caseManager = caseManager;
    }

    public async Task<CaseValidationRuleDocument> GetOrCreateRuleDocumentAsync(
        string metadataVersion,
        app metadata,
        DBConfigurationDetail dbConfig,
        string userName)
    {
        var document = await _dal.GetRuleDocumentAsync(metadataVersion, dbConfig);
        if (document != null)
        {
            EnsureRuleDocumentShape(document, metadataVersion, metadata);
            return document;
        }

        return new CaseValidationRuleDocument
        {
            _id = CaseValidationDAL.CreateDocumentId(metadataVersion),
            metadata_version = metadataVersion
        };
    }

    public async Task<document_put_response> SaveRuleDocumentAsync(
        string metadataVersion,
        CaseValidationRuleDocument document,
        DBConfigurationDetail dbConfig,
        string userName)
    {
        document ??= new CaseValidationRuleDocument();
        document._id = CaseValidationDAL.CreateDocumentId(metadataVersion);
        document.metadata_version = metadataVersion;
        document.data_type = "case-validation-rules";
        document.date_created ??= DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        document.created_by ??= userName;
        document.date_last_updated = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        document.last_updated_by = userName;
        NormalizeRuleDocumentMetadata(document);
        MarkReviewedRules(document, userName);

        var existing = await _dal.GetRuleDocumentAsync(metadataVersion, dbConfig);
        if (existing != null && string.IsNullOrWhiteSpace(document._rev))
        {
            document._rev = existing._rev;
        }

        return await _dal.SaveRuleDocumentAsync(document, dbConfig);
    }

    public CaseValidationRuleDocument BuildDefaultRuleDocument(string metadataVersion, app metadata, string userName = null)
    {
        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        return new CaseValidationRuleDocument
        {
            _id = CaseValidationDAL.CreateDocumentId(metadataVersion),
            metadata_version = metadataVersion,
            date_created = now,
            created_by = userName
        };
    }

    public List<CaseValidationFlattenedField> FlattenMetadata(app metadata)
    {
        var result = new List<CaseValidationFlattenedField>();
        if (metadata?.children == null)
        {
            return result;
        }

        var lookup = BuildLookupDictionary(metadata);
        for (var i = 0; i < metadata.children.Length; i++)
        {
            var child = metadata.children[i];
            if (!IsNodeType(child, "form"))
            {
                continue;
            }

            var formPath = child.name;
            FlattenNode(
                child,
                formPath,
                child.prompt,
                formPath,
                $"g_metadata.children[{i}]",
                $"g_data.{child.name}",
                "/" + child.name,
                IsMulti(child),
                false,
                new List<string> { child.prompt },
                lookup,
                result);
        }

        return result;
    }

    public CaseValidationEvaluationResult EvaluateCase(
        JObject caseData,
        app metadata,
        CaseValidationRuleDocument rules,
        string metadataVersion = null,
        string evaluationContext = null)
    {
        var fields = FlattenMetadata(metadata);
        rules ??= BuildDefaultRuleDocument(metadataVersion ?? metadata?.version ?? metadata?._id, metadata);
        EnsureRuleDocumentShape(rules, metadataVersion ?? rules.metadata_version, metadata);

        var result = new CaseValidationEvaluationResult
        {
            metadata_version = rules.metadata_version,
            fields = fields
        };

        if (caseData == null || rules.enabled == false)
        {
            return result;
        }

        var fieldMap = fields
            .GroupBy(f => f.field_path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        EvaluateFormStatus(caseData, rules, result);
        EvaluateFieldRules(caseData, rules, fieldMap, result, evaluationContext);
        EvaluateConnectedFieldRules(caseData, rules, fieldMap, result);

        return result;
    }

    public CaseValidationRuleSummary BuildRuleSummary(CaseValidationRuleDocument rules)
    {
        NormalizeRuleDocumentMetadata(rules);

        var summary = new CaseValidationRuleSummary
        {
            metadata_version = rules?.metadata_version,
            validation_level_meanings = ValidationLevelMeanings.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase)
        };

        if (rules == null)
        {
            return summary;
        }

        foreach (var rule in rules.field_rules ?? new List<CaseValidationFieldRule>())
        {
            AddRuleSummaryRow(summary, rule.enabled, rule.category, rule.validation_level, rule.confidence, rule.review_status, rule.source, rule.last_changed_reason);
        }

        foreach (var rule in rules.connected_field_rules ?? new List<CaseValidationConnectedFieldRule>())
        {
            AddRuleSummaryRow(summary, rule.enabled, rule.category, rule.validation_level, rule.confidence, rule.review_status, rule.source, rule.last_changed_reason);
        }

        foreach (var rule in rules.form_status_rules ?? new List<CaseValidationFormStatusRule>())
        {
            AddRuleSummaryRow(summary, rule.enabled, rule.category, rule.validation_level, rule.confidence, rule.review_status, rule.source, rule.last_changed_reason);
        }

        return summary;
    }

    public async Task<CaseValidationRulePreviewResult> PreviewRulesAsync(
        CaseValidationRulePreviewRequest request,
        app metadata,
        string metadataVersion,
        DBConfigurationDetail dbConfig,
        ClaimsPrincipal user)
    {
        var rules = request?.rules ?? await GetOrCreateRuleDocumentAsync(metadataVersion, metadata, dbConfig, GetUserName(user));
        EnsureRuleDocumentShape(rules, metadataVersion, metadata);

        var result = new CaseValidationRulePreviewResult
        {
            ok = true,
            metadata_version = rules.metadata_version,
            rule_summary = BuildRuleSummary(rules)
        };

        JObject caseData = request?.case_data;
        if (caseData == null && !string.IsNullOrWhiteSpace(request?.case_id))
        {
            var caseDocument = await _caseManager.GetCaseAsync(request.case_id, dbConfig, user);
            if (caseDocument == null)
            {
                result.ok = false;
                result.error_description = "Case was not found or is not readable by the current user.";
                return result;
            }

            caseData = JObject.Parse(JsonConvert.SerializeObject(caseDocument, CaseValidationDAL.CreateSerializerSettings()));
        }

        if (caseData == null)
        {
            result.message = "No case data was supplied. The preview includes rule inventory counts only.";
            return result;
        }

        var evaluation = EvaluateCase(caseData, metadata, rules, metadataVersion);
        result.check_count = evaluation.checks.Count;
        result.finding_count = evaluation.findings.Count;
        result.findings_by_validation_level = CountFindingsBy(evaluation.findings, f => f.validation_level);
        result.findings_by_confidence = CountFindingsBy(evaluation.findings, f => f.confidence);
        result.findings_by_category = CountFindingsBy(evaluation.findings, f => f.category);
        result.findings_by_severity = CountFindingsBy(evaluation.findings, f => f.severity);
        result.sample_findings = evaluation.findings
            .Take(Math.Clamp(request?.max_findings ?? 25, 1, 100))
            .ToList();
        return result;
    }

    public async Task<CaseValidationFieldUpdateResult> SaveSingleFieldAsync(
        CaseValidationFieldUpdateRequest request,
        app metadata,
        DBConfigurationDetail dbConfig,
        ClaimsPrincipal user,
        OverridableConfiguration configuration,
        string hostPrefix)
    {
        var result = new CaseValidationFieldUpdateResult { ok = false };
        if (request == null || string.IsNullOrWhiteSpace(request.case_id) || string.IsNullOrWhiteSpace(request.field_path))
        {
            result.error_description = "A case id and field path are required.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.tab_id))
        {
            result.error_description = "An active edit tab is required before quick editing validation fields.";
            return result;
        }

        var fields = FlattenMetadata(metadata);
        var field = fields.FirstOrDefault(f => string.Equals(f.field_path, request.field_path, StringComparison.OrdinalIgnoreCase));
        if (field == null)
        {
            result.error_description = "The requested field is not part of the current metadata version.";
            return result;
        }

        if (!field.can_quick_edit)
        {
            result.error_description = "This field type is not supported for quick edit.";
            return result;
        }

        var userName = GetUserName(user);
        var caseData = await _caseManager.GetCaseAsync(request.case_id, dbConfig, user);
        if (caseData == null)
        {
            result.error_description = "Case was not found or is not readable by the current user.";
            return result;
        }

        if (!IsCheckedOutByCurrentUser(caseData, userName))
        {
            result.error_description = "The case must be checked out by the current user before validation quick edit.";
            return result;
        }

        caseData.checked_out_by_tab_id = request.tab_id;

        var oldValue = GetFieldValueAsString(caseData, field, request.form_index, request.grid_index);
        if (!TryApplyScalarFieldValue(caseData, field, request.value, request.form_index, request.grid_index, out var newValue, out var error))
        {
            result.error_description = error;
            return result;
        }

        var changeStack = new Change_Stack
        {
            _id = Guid.NewGuid().ToString(),
            case_id = caseData._id,
            case_rev = caseData._rev,
            date_created = DateTime.UtcNow,
            user_name = userName,
            note = "Validation quick edit",
            items = new List<Change_Stack_Item>
            {
                new()
                {
                    _id = caseData._id,
                    _rev = caseData._rev,
                    object_path = BuildObjectPath(field.field_path),
                    metadata_path = field.metadata_path,
                    old_value = oldValue,
                    new_value = newValue,
                    dictionary_path = "/" + field.field_path,
                    metadata_type = field.type,
                    prompt = field.prompt,
                    date_created = DateTime.UtcNow,
                    user_name = userName,
                    form_index = request.form_index,
                    grid_index = request.grid_index
                }
            }
        };

        var saveResult = await _caseManager.SaveCaseAsync(
            caseData,
            changeStack,
            dbConfig,
            user,
            configuration,
            hostPrefix);

        result.ok = saveResult.Response?.ok == true;
        result.id = saveResult.Response?.id;
        result.rev = saveResult.Response?.rev;
        result.error_description = saveResult.Response?.error_description;
        result.serialized_case = saveResult.SerializedCase;
        return result;
    }

    private static void AddRuleSummaryRow(
        CaseValidationRuleSummary summary,
        bool enabled,
        string category,
        string validationLevel,
        string confidence,
        string reviewStatus,
        string source,
        string lastChangedReason)
    {
        summary.total_rules++;
        if (enabled)
        {
            summary.enabled_rules++;
        }

        if (string.Equals(reviewStatus, "review-pending", StringComparison.OrdinalIgnoreCase))
        {
            summary.review_pending_rules++;
        }

        if (!string.IsNullOrWhiteSpace(lastChangedReason))
        {
            summary.changed_since_publish_rules++;
        }

        Increment(summary.by_category, category);
        Increment(summary.by_validation_level, validationLevel);
        Increment(summary.by_confidence, confidence);
        Increment(summary.by_review_status, reviewStatus);
        Increment(summary.by_source, source);
    }

    private static Dictionary<string, int> CountFindingsBy(IEnumerable<CaseValidationFinding> findings, Func<CaseValidationFinding, string> selector)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in findings.Select(selector))
        {
            Increment(result, value);
        }

        return result;
    }

    private static void Increment(Dictionary<string, int> dictionary, string key)
    {
        key = string.IsNullOrWhiteSpace(key) ? "unspecified" : key;
        dictionary[key] = dictionary.TryGetValue(key, out var count) ? count + 1 : 1;
    }

    private void EnsureRuleDocumentShape(CaseValidationRuleDocument document, string metadataVersion, app metadata)
    {
        document._id ??= CaseValidationDAL.CreateDocumentId(metadataVersion);
        document.metadata_version ??= metadataVersion;
        document.data_type ??= "case-validation-rules";
        document.field_rules ??= new List<CaseValidationFieldRule>();
        document.connected_field_rules ??= new List<CaseValidationConnectedFieldRule>();
        document.form_status_rules ??= new List<CaseValidationFormStatusRule>();
        NormalizeRuleDocumentMetadata(document);
    }

    private static void NormalizeRuleDocumentMetadata(CaseValidationRuleDocument document)
    {
        if (document == null)
        {
            return;
        }

        document.field_rules ??= new List<CaseValidationFieldRule>();
        document.connected_field_rules ??= new List<CaseValidationConnectedFieldRule>();
        document.form_status_rules ??= new List<CaseValidationFormStatusRule>();

        foreach (var rule in document.field_rules)
        {
            rule.category = string.IsNullOrWhiteSpace(rule.category) ? "range" : rule.category;
            rule.severity = string.IsNullOrWhiteSpace(rule.severity) ? "warning" : rule.severity;
            rule.review_status = NormalizeReviewStatus(rule.review_status);
            rule.validation_level = NormalizeValidationLevel(rule.validation_level, rule.category, rule.source);
            rule.source = NormalizeSource(rule.source, rule.validation_level);
            rule.confidence = NormalizeConfidence(rule.confidence, rule.validation_level);
            rule.rationale ??= DefaultRationale(rule.validation_level);
            rule.bands ??= new List<CaseValidationRuleBand>();
            EnsureDefaultBand(rule.bands, rule.validation_level, rule.min_value, rule.max_value, rule.message);
            rule.explanation = ExplainRule(rule);
        }

        foreach (var rule in document.connected_field_rules)
        {
            rule.category = string.IsNullOrWhiteSpace(rule.category) ? "connected-field" : rule.category;
            rule.severity = string.IsNullOrWhiteSpace(rule.severity) ? "warning" : rule.severity;
            rule.review_status = NormalizeReviewStatus(rule.review_status);
            rule.validation_level = NormalizeValidationLevel(rule.validation_level, rule.category, rule.source);
            rule.source = NormalizeSource(rule.source, rule.validation_level);
            rule.confidence = NormalizeConfidence(rule.confidence, rule.validation_level);
            rule.rationale ??= DefaultRationale(rule.validation_level);
            rule.bands ??= new List<CaseValidationRuleBand>();
            rule.trigger_values ??= new List<string>();
            rule.trigger_displays ??= new List<string>();
            rule.related_field_paths ??= new List<string>();
            rule.parameters ??= new JObject();
            if (rule.max_difference.HasValue)
            {
                EnsureDefaultBand(rule.bands, rule.validation_level, null, rule.max_difference, rule.message);
            }

            rule.explanation = ExplainRule(rule);
        }

        foreach (var rule in document.form_status_rules)
        {
            rule.category = string.IsNullOrWhiteSpace(rule.category) ? "form-status" : rule.category;
            rule.severity = string.IsNullOrWhiteSpace(rule.severity) ? "warning" : rule.severity;
            rule.review_status = NormalizeReviewStatus(rule.review_status);
            rule.validation_level = NormalizeValidationLevel(rule.validation_level, rule.category, rule.source);
            rule.source = NormalizeSource(rule.source, rule.validation_level);
            rule.confidence = NormalizeConfidence(rule.confidence, rule.validation_level);
            rule.rationale ??= "Form status should reflect whether the form has meaningful abstracted data.";
            rule.bands ??= new List<CaseValidationRuleBand>();
            rule.explanation = ExplainRule(rule);
        }
    }

    private static void MarkReviewedRules(CaseValidationRuleDocument document, string userName)
    {
        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        foreach (var rule in document.field_rules.Where(r => string.Equals(r.review_status, "reviewed", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(rule.reviewed_by))
            {
                rule.reviewed_by = userName;
            }

            if (string.IsNullOrWhiteSpace(rule.reviewed_at))
            {
                rule.reviewed_at = now;
            }
        }

        foreach (var rule in document.connected_field_rules.Where(r => string.Equals(r.review_status, "reviewed", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(rule.reviewed_by))
            {
                rule.reviewed_by = userName;
            }

            if (string.IsNullOrWhiteSpace(rule.reviewed_at))
            {
                rule.reviewed_at = now;
            }
        }

        foreach (var rule in document.form_status_rules.Where(r => string.Equals(r.review_status, "reviewed", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(rule.reviewed_by))
            {
                rule.reviewed_by = userName;
            }

            if (string.IsNullOrWhiteSpace(rule.reviewed_at))
            {
                rule.reviewed_at = now;
            }
        }
    }

    private static string NormalizeValidationLevel(string validationLevel, string category, string source)
    {
        var normalized = NormalizeSubject(validationLevel).Replace(" ", "-", StringComparison.Ordinal);
        return normalized switch
        {
            "metadata" => "metadata",
            "impossibility" => "impossibility",
            "plausibility" or "intrinsic" or "intrinsic-logic" => "plausibility",
            "timeline" or "timeline-logic" => "timeline",
            "conditional" or "conditional-logic" => "conditional",
            "form-completeness" or "completeness" => "form-completeness",
            "connected" or "connected-logic" => "impossibility",
            _ when string.Equals(category, "form-status", StringComparison.OrdinalIgnoreCase) => "form-completeness",
            _ when string.Equals(category, "connected-field", StringComparison.OrdinalIgnoreCase) => "impossibility",
            _ when string.Equals(source, "logical-seed", StringComparison.OrdinalIgnoreCase) => "plausibility",
            _ => "metadata"
        };
    }

    private static string NormalizeConfidence(string confidence, string validationLevel)
    {
        var normalized = NormalizeSubject(confidence).Replace(" ", "-", StringComparison.Ordinal);
        return normalized switch
        {
            "high" => "high",
            "medium" => "medium",
            "low" => "low",
            _ when string.Equals(validationLevel, "metadata", StringComparison.OrdinalIgnoreCase) => "high",
            _ when string.Equals(validationLevel, "impossibility", StringComparison.OrdinalIgnoreCase) => "high",
            _ when string.Equals(validationLevel, "timeline", StringComparison.OrdinalIgnoreCase) => "high",
            _ => "medium"
        };
    }

    private static string NormalizeReviewStatus(string reviewStatus)
    {
        var normalized = NormalizeSubject(reviewStatus).Replace(" ", "-", StringComparison.Ordinal);
        return normalized switch
        {
            "generated" => "generated",
            "review-pending" => "review-pending",
            "reviewed" => "reviewed",
            "retired" => "retired",
            _ => "generated"
        };
    }

    private static string NormalizeSource(string source, string validationLevel)
    {
        var normalized = NormalizeSubject(source).Replace(" ", "-", StringComparison.Ordinal);
        return normalized switch
        {
            "metadata" => "metadata",
            "seed-catalog" or "logical-seed" => "seed-catalog",
            "admin" or "metadata-editor" => "admin",
            "imported-standard" => "imported-standard",
            _ when string.Equals(validationLevel, "metadata", StringComparison.OrdinalIgnoreCase) => "metadata",
            _ => "seed-catalog"
        };
    }

    private static string DefaultRationale(string validationLevel)
    {
        return ValidationLevelMeanings.TryGetValue(validationLevel ?? string.Empty, out var meaning)
            ? meaning
            : "Generated from validation metadata.";
    }

    private static void EnsureDefaultBand(List<CaseValidationRuleBand> bands, string validationLevel, double? minValue, double? maxValue, string message)
    {
        if ((!minValue.HasValue && !maxValue.HasValue) || bands.Count > 0)
        {
            return;
        }

        var bandName = validationLevel switch
        {
            "plausibility" => "plausible-warning",
            "impossibility" => "impossible-warning",
            _ => "normal"
        };

        bands.Add(new CaseValidationRuleBand
        {
            name = bandName,
            label = bandName switch
            {
                "plausible-warning" => "Warn outside plausible range",
                "impossible-warning" => "Warn outside possible range",
                _ => "Expected metadata range"
            },
            min_value = minValue,
            max_value = maxValue,
            message = message
        });
    }

    private static string ExplainRule(CaseValidationFieldRule rule)
    {
        var field = string.IsNullOrWhiteSpace(rule.prompt) ? rule.field_path : rule.prompt;
        var expected = BuildFieldRuleExpectedText(rule);
        if (string.IsNullOrWhiteSpace(expected))
        {
            expected = "the field's configured validation metadata";
        }

        return $"{field} is checked as {rule.validation_level} validation with {rule.confidence} confidence; expected {expected}.";
    }

    private static string ExplainRule(CaseValidationConnectedFieldRule rule)
    {
        var field = string.IsNullOrWhiteSpace(rule.prompt) ? rule.field_path : rule.prompt;
        var related = string.IsNullOrWhiteSpace(rule.related_prompt) ? rule.related_field_path : rule.related_prompt;
        var expected = BuildConnectedRuleExpectedText(rule);
        if (string.IsNullOrWhiteSpace(expected))
        {
            expected = $"{field} should be consistent with {related}";
        }

        return $"{field} is compared with {related} as {rule.validation_level} validation with {rule.confidence} confidence; expected {expected}.";
    }

    private static string ExplainRule(CaseValidationFormStatusRule rule)
    {
        var form = string.IsNullOrWhiteSpace(rule.form_prompt) ? rule.form_path : rule.form_prompt;
        return $"{form} status is checked as form-completeness validation with {rule.confidence} confidence; data-present forms should be in progress or completed, and completed forms should have at least {rule.completed_min_meaningful_fields} meaningful fields.";
    }

    private static Dictionary<string, value_node[]> BuildLookupDictionary(app metadata)
    {
        var result = new Dictionary<string, value_node[]>(StringComparer.OrdinalIgnoreCase);
        if (metadata?.lookup == null)
        {
            return result;
        }

        foreach (var item in metadata.lookup.Where(i => !string.IsNullOrWhiteSpace(i?.name)))
        {
            result[$"lookup/{item.name}"] = item.values ?? Array.Empty<value_node>();
        }

        return result;
    }

    private static value_node[] ResolveValues(node current, Dictionary<string, value_node[]> lookup)
    {
        if (current?.values?.Length > 0)
        {
            return current.values;
        }

        if (!string.IsNullOrWhiteSpace(current?.path_reference) &&
            lookup != null &&
            lookup.TryGetValue(current.path_reference, out var values))
        {
            return values;
        }

        return current?.values;
    }

    private static void FlattenNode(
        node current,
        string formPath,
        string formPrompt,
        string fieldPath,
        string metadataPath,
        string objectPath,
        string dictionaryPath,
        bool isMultiform,
        bool isGrid,
        List<string> ancestry,
        Dictionary<string, value_node[]> lookup,
        List<CaseValidationFlattenedField> result)
    {
        var type = current.type ?? string.Empty;
        var isGridNode = IsNodeType(current, "grid");
        var currentIsGrid = isGrid || isGridNode;
        var isScalar = ScalarTypes.Contains(type);
        var values = ResolveValues(current, lookup);
        var field = new CaseValidationFlattenedField
        {
            form_path = formPath,
            form_prompt = formPrompt,
            field_path = fieldPath,
            metadata_path = metadataPath,
            dictionary_path = dictionaryPath,
            object_path = objectPath,
            prompt = current.prompt,
            name = current.name,
            type = current.type,
            data_type = current.data_type ?? current.list_item_data_type,
            cardinality = current.cardinality,
            subject = BuildSubject(current, ancestry),
            path_reference = current.path_reference,
            is_multiform = isMultiform,
            is_grid = currentIsGrid,
            is_scalar = isScalar,
            can_quick_edit = isScalar && !currentIsGrid && !IsMultiSelect(current) && current.is_read_only != true && current.is_hidden != true,
            is_required = current.is_required == true,
            is_read_only = current.is_read_only == true,
            is_hidden = current.is_hidden == true,
            min_value = current.min_value,
            max_value = current.max_value,
            max_length = current.max_length,
            regex_pattern = current.regex_pattern,
            validation_description = current.validation_description,
            tags = current.tags ?? Array.Empty<string>(),
            ancestry = ancestry.Where(a => !string.IsNullOrWhiteSpace(a)).ToList()
        };

        if (values != null)
        {
            field.values = values
                .Select(v => new CaseValidationListValue { value = v.value, display = v.display ?? v.description ?? v.value })
                .ToList();
        }

        result.Add(field);

        if (current.children == null)
        {
            return;
        }

        for (var i = 0; i < current.children.Length; i++)
        {
            var child = current.children[i];
            var nextPath = $"{fieldPath}/{child.name}";
            var nextMetadataPath = $"{metadataPath}.children[{i}]";
            var nextAncestry = new List<string>(ancestry);
            if (!string.IsNullOrWhiteSpace(current.prompt) && !IsNodeType(current, "form"))
            {
                nextAncestry.Add(current.prompt);
            }

            FlattenNode(
                child,
                formPath,
                formPrompt,
                nextPath,
                nextMetadataPath,
                $"{objectPath}.{child.name}",
                $"{dictionaryPath}/{child.name}",
                isMultiform || IsMulti(current),
                currentIsGrid,
                nextAncestry,
                lookup,
                result);
        }
    }

    private static void AddFutureCandidateReviewRules(
        List<CaseValidationConnectedFieldRule> result,
        Dictionary<string, CaseValidationFlattenedField> byPath)
    {
        const string disabledReason = "Generated from future-candidate review findings; enable after preview impact review.";

        AddConnectedRule(result, byPath, "connected:er-internal-transfer-within-admission-window",
            "er_visit_and_hospital_medical_records/internal_transfers/date_and_time",
            "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission",
            "date_within_window",
            "ER internal transfer date/time and hospital stay window",
            "Internal transfers should occur within the admission and discharge window for the same ER/hospital visit.",
            "Internal transfer date/time should fall within the hospital admission and discharge window.",
            requireSameContainer: true,
            validationLevel: "timeline",
            relatedFieldPaths: new[]
            {
                "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission",
                "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge"
            },
            parameters: new JObject
            {
                ["start_field_path"] = "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission",
                ["end_field_path"] = "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge"
            });

        foreach (var eventPath in new[]
        {
            "other_medical_office_visits/vital_signs/date_and_time",
            "other_medical_office_visits/laboratory_tests/date_and_time",
            "other_medical_office_visits/diagnostic_imaging_and_other_technology/date_and_time",
            "other_medical_office_visits/referrals_and_consultations/date",
            "other_medical_office_visits/medications/date_and_time"
        })
        {
            AddConnectedRule(result, byPath, $"connected:other-medical-visit-before-event:{eventPath}",
                eventPath,
                "other_medical_office_visits/visit/date_of_medical_office_visit",
                "date_greater_than_or_equal",
                "other medical visit date and same-visit event date",
                "Same-visit vital signs, laboratory, diagnostic imaging, referral, and medication dates should not occur before the medical office visit date.",
                "Event date should be on or after the medical office visit date.",
                requireSameContainer: true,
                validationLevel: "timeline");
        }

        AddConnectedRule(result, byPath, "connected:transport-vital-sign-date-matches-transport-date",
            "medical_transport/transport_vital_signs/date_and_time",
            "medical_transport/date_of_transport",
            "date_equal",
            "transport vital sign date and transport date",
            "Transport vital signs should be recorded on the date of transport.",
            "Transport vital sign date should match date of transport.",
            requireSameContainer: true,
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:abstraction-complete-before-committee-review",
            "home_record/case_status/abstraction_complete_date",
            "committee_review/date_of_review",
            "date_less_than_or_equal",
            "abstraction complete date and committee review date",
            "Abstraction should be complete on or before committee review when both dates are available.",
            "Abstraction complete date should be on or before committee review date.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:abstraction-complete-before-case-locked",
            "home_record/case_status/abstraction_complete_date",
            "home_record/case_status/case_locked_date",
            "date_less_than_or_equal",
            "abstraction complete date and case locked date",
            "Abstraction should be complete on or before the case is locked.",
            "Abstraction complete date should be on or before case locked date.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:parent-mother-birth-date-before-death-date",
            "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth",
            "home_record/date_of_death",
            "date_less_than_or_equal",
            "maternal date of birth and date of death",
            "A maternal birth date cannot occur after the date of death.",
            "Mother date of birth should be on or before date of death.",
            validationLevel: "timeline");

        foreach (var pair in new[]
        {
            ("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses", "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit", "parent-lmp-before-first-prenatal"),
            ("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit", "parent-lmp-before-last-prenatal"),
            ("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery", "parent-lmp-before-delivery"),
            ("prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_1st_prenatal_visit", "prenatal-lmp-before-first-prenatal"),
            ("prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_1st_ultrasound", "prenatal-lmp-before-first-ultrasound"),
            ("prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_last_prenatal_visit", "prenatal-lmp-before-last-prenatal"),
            ("prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/estimated_date_of_confinement", "prenatal-lmp-before-edd"),
            ("prenatal/current_pregnancy/date_of_last_normal_menses", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery", "prenatal-lmp-before-delivery")
        })
        {
            AddConnectedRule(result, byPath, $"connected:{pair.Item3}",
                pair.Item1,
                pair.Item2,
                "date_less_than_or_equal",
                "last normal menses and pregnancy chronology",
                "Date of last normal menses should occur on or before later pregnancy dates.",
                "Date of last normal menses should be on or before the related pregnancy date.",
                validationLevel: "timeline");
        }

        AddConnectedRule(result, byPath, "connected:apgar-10-requires-apgar-5",
            "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_10",
            "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_5",
            "requires_related_when_present",
            "Apgar 10-minute score and 5-minute score",
            "A 10-minute Apgar score should not be entered without a 5-minute Apgar score unless reviewed as an exception.",
            "Apgar 5-minute score should be entered when Apgar 10-minute score is entered.",
            requireSameContainer: true,
            validationLevel: "conditional");

        AddDisabledCalculatedRules(result, byPath, disabledReason);
    }

    private static void AddDisabledCalculatedRules(
        List<CaseValidationConnectedFieldRule> result,
        Dictionary<string, CaseValidationFlattenedField> byPath,
        string disabledReason)
    {
        AddConnectedRule(result, byPath, "connected:death-certificate-age-aligns-with-dob-dod",
            "death_certificate/demographics/age",
            "death_certificate/demographics/date_of_birth",
            "age_matches_dates",
            "death certificate age and death chronology",
            "Age should align with date of birth and date of death when full dates are available.",
            "Age should align with date of birth and date of death.",
            validationLevel: "plausibility",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { "death_certificate/demographics/date_of_birth", "home_record/date_of_death" },
            parameters: new JObject { ["event_date_field_path"] = "home_record/date_of_death", ["tolerance_years"] = 1 });

        AddConnectedRule(result, byPath, "connected:death-certificate-age-on-certificate-aligns-with-dob-dod",
            "death_certificate/demographics/age_on_death_certificate",
            "death_certificate/demographics/date_of_birth",
            "age_matches_dates",
            "age on death certificate and death chronology",
            "Age on death certificate should align with date of birth and date of death when full dates are available.",
            "Age on death certificate should align with date of birth and date of death.",
            validationLevel: "plausibility",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { "death_certificate/demographics/date_of_birth", "home_record/date_of_death" },
            parameters: new JObject { ["event_date_field_path"] = "home_record/date_of_death", ["tolerance_years"] = 1 });

        foreach (var ageRule in new[]
        {
            ("connected:parent-mother-age-aligns-with-dob-delivery", "birth_fetal_death_certificate_parent/demographic_of_mother/age", "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth"),
            ("connected:parent-father-age-aligns-with-dob-delivery", "birth_fetal_death_certificate_parent/demographic_of_father/age", "birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth")
        })
        {
            AddConnectedRule(result, byPath, ageRule.Item1,
                ageRule.Item2,
                ageRule.Item3,
                "age_matches_dates",
                "parent age and delivery chronology",
                "Parent age should align with date of birth and delivery date when enough date components exist.",
                "Parent age should align with date of birth and delivery date.",
                validationLevel: "plausibility",
                enabled: false,
                disabledReason: disabledReason,
                relatedFieldPaths: new[] { ageRule.Item3, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery" },
                parameters: new JObject { ["event_date_field_path"] = "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery", ["tolerance_years"] = 1 });
        }

        AddConnectedRule(result, byPath, "connected:parent-previous-live-births-equals-now-living-plus-dead",
            "birth_fetal_death_certificate_parent/pregnancy_history/number_of_previous_live_births",
            "birth_fetal_death_certificate_parent/pregnancy_history/now_living",
            "numeric_sum_equals",
            "previous live births, now living, and now dead",
            "Previous live births should equal now living plus now dead when all three values are present.",
            "Previous live births should equal now living plus now dead.",
            validationLevel: "plausibility",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { "birth_fetal_death_certificate_parent/pregnancy_history/now_living", "birth_fetal_death_certificate_parent/pregnancy_history/now_dead" },
            parameters: new JObject { ["comparison"] = "equals_sum", ["tolerance"] = 0 });

        AddConnectedRule(result, byPath, "connected:prenatal-gravida-broadly-consistent-with-para-abortions",
            "prenatal/pregnancy_history/gravida",
            "prenatal/pregnancy_history/para",
            "numeric_sum_equals",
            "gravida, para, and abortions",
            "Gravida should be greater than or equal to para plus abortions when all values are present.",
            "Gravida should be greater than or equal to para plus abortions.",
            validationLevel: "plausibility",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { "prenatal/pregnancy_history/para", "prenatal/pregnancy_history/abortions" },
            parameters: new JObject { ["comparison"] = "greater_or_equal_sum", ["tolerance"] = 0 });

        foreach (var weightRule in new[]
        {
            ("connected:parent-weight-gain-matches-prepregnancy-delivery-weight", "birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain", "birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery", "birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight"),
            ("connected:prenatal-weight-gain-matches-prepregnancy-last-visit-weight", "prenatal/current_pregnancy/weight_gain", "prenatal/current_pregnancy/weight_at_last_visit", "prenatal/current_pregnancy/pre_pregnancy_weight")
        })
        {
            AddConnectedRule(result, byPath, weightRule.Item1,
                weightRule.Item2,
                weightRule.Item3,
                "numeric_difference_matches",
                "weight gain and pregnancy weights",
                "Weight gain should be consistent with later pregnancy weight minus pre-pregnancy weight.",
                "Weight gain should match later pregnancy weight minus pre-pregnancy weight.",
                validationLevel: "plausibility",
                enabled: false,
                disabledReason: disabledReason,
                relatedFieldPaths: new[] { weightRule.Item3, weightRule.Item4 },
                parameters: new JObject { ["minuend_field_path"] = weightRule.Item3, ["subtrahend_field_path"] = weightRule.Item4, ["tolerance"] = 2 });
        }

        foreach (var bmiRule in new[]
        {
            ("connected:parent-bmi-matches-height-weight", "birth_fetal_death_certificate_parent/maternal_biometrics/bmi", "birth_fetal_death_certificate_parent/maternal_biometrics/height_feet", "birth_fetal_death_certificate_parent/maternal_biometrics/height_inches", "birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight"),
            ("connected:prenatal-bmi-matches-height-weight", "prenatal/current_pregnancy/bmi", "prenatal/current_pregnancy/height/feet", "prenatal/current_pregnancy/height/inches", "prenatal/current_pregnancy/pre_pregnancy_weight"),
            ("connected:er-bmi-matches-height-weight", "er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi", "er_visit_and_hospital_medical_records/maternal_biometrics/height/feet", "er_visit_and_hospital_medical_records/maternal_biometrics/height/inches", "er_visit_and_hospital_medical_records/maternal_biometrics/admission_weight"),
            ("connected:autopsy-bmi-matches-height-weight", "autopsy_report/biometrics/mother/bmi", "autopsy_report/biometrics/mother/height/feet", "autopsy_report/biometrics/mother/height/inches", "autopsy_report/biometrics/mother/weight")
        })
        {
            AddConnectedRule(result, byPath, bmiRule.Item1,
                bmiRule.Item2,
                bmiRule.Item3,
                "bmi_matches_height_weight",
                "BMI, height, and weight",
                "BMI should be consistent with height and weight within a broad tolerance.",
                "BMI should match height and weight within tolerance.",
                requireSameContainer: bmiRule.Item1.Contains(":er-", StringComparison.Ordinal),
                validationLevel: "plausibility",
                enabled: false,
                disabledReason: disabledReason,
                relatedFieldPaths: new[] { bmiRule.Item3, bmiRule.Item4, bmiRule.Item5 },
                parameters: new JObject { ["height_feet_field_path"] = bmiRule.Item3, ["height_inches_field_path"] = bmiRule.Item4, ["weight_field_path"] = bmiRule.Item5, ["tolerance"] = 1 });
        }

        foreach (var unitRule in new[]
        {
            ("connected:infant-birth-weight-unit-aware-range", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/grams_or_pounds", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/unit_of_measurement", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/ounces"),
            ("connected:autopsy-fetal-weight-unit-aware-range", "autopsy_report/biometrics/fetus/fetal_weight", "autopsy_report/biometrics/fetus/fetal_weight_uom", "autopsy_report/biometrics/fetus/fetal_weight_ounce_value"),
            ("connected:prenatal-history-birth-weight-unit-aware-range", "prenatal/pregnancy_history/details_grid/birth_weight", "prenatal/pregnancy_history/details_grid/birth_weight_uom", "prenatal/pregnancy_history/details_grid/birth_weight_oz")
        })
        {
            AddConnectedRule(result, byPath, unitRule.Item1,
                unitRule.Item2,
                unitRule.Item3,
                "unit_numeric_range",
                "birth/fetal weight and unit",
                "Birth/fetal weight values should be interpreted with their unit field.",
                "Birth/fetal weight should be plausible for the selected unit, and ounce remainder should be 0-15.",
                requireSameContainer: true,
                validationLevel: "plausibility",
                enabled: false,
                disabledReason: disabledReason,
                relatedFieldPaths: new[] { unitRule.Item3, unitRule.Item4 },
                parameters: new JObject { ["unit_field_path"] = unitRule.Item3, ["ounce_field_path"] = unitRule.Item4, ["default_min"] = 0, ["default_max"] = 7000, ["ounce_min"] = 0, ["ounce_max"] = 15 });
        }

        foreach (var gestationRule in new[]
        {
            ("connected:prenatal-first-visit-gestational-age-matches-lmp", "prenatal/current_pregnancy/date_of_1st_prenatal_visit", "prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days"),
            ("connected:prenatal-first-ultrasound-gestational-age-matches-lmp", "prenatal/current_pregnancy/date_of_1st_ultrasound", "prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound", "prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound_days"),
            ("connected:prenatal-last-visit-gestational-age-matches-lmp", "prenatal/current_pregnancy/date_of_last_prenatal_visit", "prenatal/current_pregnancy/date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit", "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days"),
            ("connected:er-arrival-gestational-age-matches-lmp", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival", "prenatal/current_pregnancy/date_of_last_normal_menses", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days"),
            ("connected:transport-gestational-age-matches-lmp", "medical_transport/date_of_transport", "prenatal/current_pregnancy/date_of_last_normal_menses", "medical_transport/date_of_transport/gestational_age_weeks", "medical_transport/date_of_transport/gestational_age_days")
        })
        {
            AddConnectedRule(result, byPath, gestationRule.Item1,
                gestationRule.Item2,
                gestationRule.Item3,
                "gestational_age_matches_anchor",
                "event date, pregnancy anchor date, and gestational age",
                "Gestational age weeks/days should be consistent with event date and pregnancy anchor date when enough dates are present.",
                "Gestational age should align with the event date and pregnancy anchor date within tolerance.",
                requireSameContainer: gestationRule.Item1.Contains(":er-", StringComparison.Ordinal) || gestationRule.Item1.Contains(":transport-", StringComparison.Ordinal),
                validationLevel: "plausibility",
                enabled: false,
                disabledReason: disabledReason,
                relatedFieldPaths: new[] { gestationRule.Item3, gestationRule.Item4, gestationRule.Item5 },
                parameters: new JObject { ["anchor_date_field_path"] = gestationRule.Item3, ["weeks_field_path"] = gestationRule.Item4, ["days_field_path"] = gestationRule.Item5, ["tolerance_days"] = 14 });
        }

        AddConnectedRule(result, byPath, "connected:prenatal-total-visits-requires-visit-dates",
            "prenatal/current_pregnancy/total_number_of_visits",
            "prenatal/current_pregnancy/date_of_1st_prenatal_visit",
            "requires_related_when_present",
            "total prenatal visits and visit dates",
            "Total number of visits should be consistent with first/last prenatal visit dates.",
            "A positive total number of visits should have a first or last prenatal visit date.",
            validationLevel: "conditional",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { "prenatal/current_pregnancy/date_of_1st_prenatal_visit", "prenatal/current_pregnancy/date_of_last_prenatal_visit" },
            parameters: new JObject { ["only_positive"] = true, ["require_any_related"] = true });

        AddConnectedRule(result, byPath, "connected:multiple-gestation-plurality-birth-order-agree",
            "birth_certificate_infant_fetal_section/birth_order",
            "birth_fetal_death_certificate_parent/prenatal_care/plurality",
            "dependent_grid_consistency",
            "multiple gestation, plurality, and birth order",
            "Multiple gestation, plurality, and birth order should agree when present.",
            "Birth order should agree with plurality and multiple gestation values.",
            requireSameContainer: false,
            validationLevel: "plausibility",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { "birth_fetal_death_certificate_parent/prenatal_care/plurality", "birth_certificate_infant_fetal_section/is_multiple_gestation" },
            parameters: new JObject { ["mode"] = "multiple_gestation_birth_order" });

        AddDependentGridRule(result, byPath, disabledReason, "connected:autopsy-toxicology-performed-matches-toxicology-grid",
            "autopsy_report/was_toxicology_performed", "autopsy_report/toxicology");
        AddDependentGridRule(result, byPath, disabledReason, "connected:social-referrals-source-matches-referral-grid",
            "social_and_environmental_profile/were_there_referrals_for_social_services", "social_and_environmental_profile/social_and_medical_referrals");
        AddDependentGridRule(result, byPath, disabledReason, "connected:prenatal-medical-referrals-source-matches-referral-grid",
            "prenatal/current_pregnancy/were_there_medical_referrals", "prenatal/medical_referrals");
        AddDependentGridRule(result, byPath, disabledReason, "connected:prenatal-hospitalizations-source-matches-hospitalization-grid",
            "prenatal/current_pregnancy/were_there_pre_delivery_hospitalizations", "prenatal/pre_delivery_hospitalizations");
    }

    private static void AddDependentGridRule(
        List<CaseValidationConnectedFieldRule> result,
        Dictionary<string, CaseValidationFlattenedField> byPath,
        string disabledReason,
        string id,
        string sourcePath,
        string gridPath)
    {
        AddConnectedRule(result, byPath, id,
            sourcePath,
            gridPath,
            "dependent_grid_consistency",
            "source answer and dependent details",
            "Yes/no source fields should be consistent with dependent detail rows.",
            "Source answer should be consistent with dependent detail rows.",
            validationLevel: "conditional",
            enabled: false,
            disabledReason: disabledReason,
            relatedFieldPaths: new[] { gridPath },
            parameters: new JObject { ["mode"] = "grid_requires_yes", ["grid_path"] = gridPath, ["yes_values"] = new JArray("1", "yes", "true") });
    }

    private static void AddConnectedRule(
        List<CaseValidationConnectedFieldRule> rules,
        Dictionary<string, CaseValidationFlattenedField> byPath,
        string id,
        string fieldPath,
        string relatedFieldPath,
        string ruleType,
        string subject,
        string rationale,
        string message,
        bool requireSameContainer = false,
        string validationLevel = "impossibility",
        bool enabled = true,
        string disabledReason = null,
        IEnumerable<string> relatedFieldPaths = null,
        JObject parameters = null)
    {
        byPath.TryGetValue(fieldPath, out var field);
        byPath.TryGetValue(relatedFieldPath, out var relatedField);
        if (field == null && relatedField == null)
        {
            return;
        }

        rules.Add(new CaseValidationConnectedFieldRule
        {
            id = id,
            enabled = enabled,
            rule_type = ruleType,
            form_path = field?.form_path ?? FirstPathSegment(fieldPath),
            form_prompt = field?.form_prompt ?? PromptFromPath(FirstPathSegment(fieldPath)),
            field_path = fieldPath,
            related_field_path = relatedFieldPath,
            related_field_paths = relatedFieldPaths?.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>(),
            metadata_path = field?.metadata_path,
            prompt = field?.prompt ?? PromptFromPath(fieldPath),
            related_prompt = relatedField?.prompt ?? PromptFromPath(relatedFieldPath),
            subject = subject,
            comparison = ruleType,
            validation_level = validationLevel,
            source = "logical-seed",
            rationale = rationale,
            review_status = "review-pending",
            require_same_container = requireSameContainer,
            parameters = parameters ?? new JObject(),
            disabled_reason = disabledReason,
            message = message
        });
    }

    private static IEnumerable<CaseValidationConnectedFieldRule> CreateOtherSpecifyRules(
        List<CaseValidationFlattenedField> fields,
        Dictionary<string, CaseValidationFlattenedField> byPath)
    {
        var specifyFields = fields
            .Where(IsOtherSpecifyTextField)
            .ToList();

        foreach (var field in fields.Where(IsOtherListField))
        {
            var triggerValues = field.values
                .Where(IsOtherListValue)
                .Select(v => v.value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var triggerDisplays = field.values
                .Where(IsOtherListValue)
                .Select(v => v.display ?? v.value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (triggerValues.Count == 0 && triggerDisplays.Count == 0)
            {
                continue;
            }

            var specify = FindNearestSpecifyField(field, specifyFields);
            if (specify == null || !byPath.ContainsKey(specify.field_path))
            {
                continue;
            }

            yield return new CaseValidationConnectedFieldRule
            {
                id = $"connected:other-specify:{field.field_path}",
                rule_type = "conditional_other_requires_specify",
                form_path = field.form_path,
                form_prompt = field.form_prompt,
                field_path = field.field_path,
                related_field_path = specify.field_path,
                metadata_path = field.metadata_path,
                prompt = field.prompt,
                related_prompt = specify.prompt,
                subject = "other selection and specify text",
                comparison = "other_requires_specify",
                validation_level = "conditional",
                source = "logical-seed",
                rationale = "When a list value of Other is selected, the matching specify field should explain the selected value.",
                review_status = "review-pending",
                require_same_container = true,
                trigger_values = triggerValues,
                trigger_displays = triggerDisplays,
                message = $"{specify.prompt} should be entered when {field.prompt} is Other."
            };

            yield return new CaseValidationConnectedFieldRule
            {
                id = $"connected:other-specify-reverse:{specify.field_path}",
                enabled = false,
                rule_type = "dependent_grid_consistency",
                form_path = specify.form_path,
                form_prompt = specify.form_prompt,
                field_path = specify.field_path,
                related_field_path = field.field_path,
                related_field_paths = new List<string> { field.field_path },
                metadata_path = specify.metadata_path,
                prompt = specify.prompt,
                related_prompt = field.prompt,
                subject = "specify text and other selection",
                comparison = "specify_blank_unless_trigger",
                validation_level = "conditional",
                source = "logical-seed",
                rationale = "Specify fields may contain stale or contradictory text when the matching list field does not select Other.",
                review_status = "review-pending",
                require_same_container = true,
                trigger_values = triggerValues,
                trigger_displays = triggerDisplays,
                parameters = new JObject { ["mode"] = "specify_blank_unless_trigger" },
                disabled_reason = "Generated from future-candidate review findings; enable after preview impact review.",
                message = $"{specify.prompt} should be blank or reviewed when {field.prompt} is not Other."
            };
        }
    }

    private static bool IsOtherListField(CaseValidationFlattenedField field)
    {
        return field != null &&
               string.Equals(field.type, "list", StringComparison.OrdinalIgnoreCase) &&
               field.values.Any(IsOtherListValue);
    }

    private static bool IsOtherListValue(CaseValidationListValue value)
    {
        var normalized = NormalizeSubject($"{value?.value} {value?.display}");
        return ContainsNormalizedWord(normalized, "other");
    }

    private static bool IsOtherSpecifyTextField(CaseValidationFlattenedField field)
    {
        if (field == null ||
            (!string.Equals(field.type, "string", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(field.type, "textarea", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var normalized = NormalizeSubject($"{field.field_path} {field.prompt} {field.subject}");
        return ContainsNormalizedWord(normalized, "other") && ContainsNormalizedWord(normalized, "specify");
    }

    private static CaseValidationFlattenedField FindNearestSpecifyField(
        CaseValidationFlattenedField field,
        List<CaseValidationFlattenedField> candidates)
    {
        return candidates
            .Where(candidate => string.Equals(candidate.form_path, field.form_path, StringComparison.OrdinalIgnoreCase))
            .Select(candidate => new
            {
                Field = candidate,
                SharedLength = GetSharedPathLength(field.field_path, candidate.field_path),
                SameParent = string.Equals(ParentPath(field.field_path), ParentPath(candidate.field_path), StringComparison.OrdinalIgnoreCase),
                Similarity = Math.Max(
                    SubjectSimilarity(field.prompt, candidate.prompt),
                    SubjectSimilarity(field.subject, candidate.subject))
            })
            .Where(item => item.SameParent || item.SharedLength >= 2 || item.Similarity >= 0.45)
            .OrderByDescending(item => item.SameParent)
            .ThenByDescending(item => item.SharedLength)
            .ThenByDescending(item => item.Similarity)
            .Select(item => item.Field)
            .FirstOrDefault();
    }

    private static bool ContainsNormalizedWord(string normalizedText, string word)
    {
        if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return Regex.IsMatch(normalizedText, $@"(^|\s){Regex.Escape(word)}(\s|$)", RegexOptions.IgnoreCase);
    }

    private static int GetSharedPathLength(string leftPath, string rightPath)
    {
        var left = SplitPath(leftPath);
        var right = SplitPath(rightPath);
        var count = 0;
        while (count < left.Length &&
               count < right.Length &&
               string.Equals(left[count], right[count], StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        return count;
    }

    private static string ParentPath(string path)
    {
        var parts = SplitPath(path);
        return parts.Length <= 1 ? string.Empty : string.Join("/", parts.Take(parts.Length - 1));
    }

    private static string FirstPathSegment(string path)
    {
        return SplitPath(path).FirstOrDefault() ?? string.Empty;
    }

    private static string PromptFromPath(string path)
    {
        var leaf = SplitPath(path).LastOrDefault() ?? path ?? string.Empty;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(leaf.Replace("_", " ", StringComparison.Ordinal).Replace("-", " ", StringComparison.Ordinal));
    }

    private static void AddDateBeforeDeathRule(
        List<CaseValidationConnectedFieldRule> rules,
        Dictionary<string, CaseValidationFlattenedField> byPath,
        string fieldPath,
        string message)
    {
        if (!byPath.TryGetValue(fieldPath, out var field) ||
            !byPath.TryGetValue("home_record/date_of_death", out var dateOfDeath))
        {
            return;
        }

        rules.Add(new CaseValidationConnectedFieldRule
        {
            id = $"connected:before-death:{fieldPath}",
            rule_type = "date_less_than_or_equal",
            form_path = field.form_path,
            form_prompt = field.form_prompt,
            field_path = field.field_path,
            related_field_path = dateOfDeath.field_path,
            metadata_path = field.metadata_path,
            prompt = field.prompt,
            related_prompt = dateOfDeath.prompt,
            subject = "event date and date of death",
            comparison = "less_than_or_equal",
            validation_level = "timeline-logic",
            source = "logical-seed",
            rationale = "This event is expected to occur on or before the death date.",
            review_status = "review-pending",
            message = message
        });
    }

    private static bool IsClinicalEventDateField(CaseValidationFlattenedField field)
    {
        if (field == null || string.Equals(field.field_path, "home_record/date_of_death", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = field.field_path ?? string.Empty;
        var subject = NormalizeSubject($"{field.field_path} {field.prompt} {field.subject}");
        var isDateType = string.Equals(field.type, "date", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(field.type, "datetime", StringComparison.OrdinalIgnoreCase) ||
                         path.EndsWith("/date_and_time", StringComparison.OrdinalIgnoreCase);
        if (!isDateType)
        {
            return false;
        }

        var isClinicalEvent = subject.Contains("vital signs") ||
                              subject.Contains("transport vital signs") ||
                              subject.Contains("er visits") ||
                              subject.Contains("hospital") ||
                              subject.Contains("medical office visits") ||
                              subject.Contains("medical transport");

        var allowedAfterDeath = subject.Contains("autopsy") ||
                                subject.Contains("committee") ||
                                subject.Contains("case status") ||
                                subject.Contains("abstraction") ||
                                subject.Contains("locked") ||
                                subject.Contains("review");

        return isClinicalEvent && !allowedAfterDeath;
    }

    private static bool IsSystolicBloodPressureField(CaseValidationFlattenedField field)
    {
        if (!IsNumericField(field))
        {
            return false;
        }

        var subject = NormalizeSubject($"{field.field_path} {field.prompt} {field.subject}");
        return subject.Contains("systolic bp") ||
               subject.Contains("systolic blood pressure") ||
               subject.EndsWith("systolic", StringComparison.Ordinal);
    }

    private static string ResolveSiblingPath(string fieldPath, string currentName, string siblingName)
    {
        if (string.IsNullOrWhiteSpace(fieldPath) ||
            !fieldPath.EndsWith(currentName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fieldPath.Substring(0, fieldPath.Length - currentName.Length) + siblingName;
    }

    private static bool IsGestationalWeekField(CaseValidationFlattenedField field)
    {
        if (!IsNumericField(field))
        {
            return false;
        }

        var normalizedPath = NormalizeSubject(field.field_path);
        var normalizedSubject = NormalizeSubject($"{field.prompt} {field.subject}");
        return normalizedPath.EndsWith("weeks", StringComparison.Ordinal) &&
               (normalizedPath.Contains("gestational age") ||
                normalizedPath.Contains("gestational weeks") ||
                normalizedSubject.Contains("gestational age") ||
                normalizedSubject.Contains("ga weeks"));
    }

    private static void EvaluateFormStatus(JObject caseData, CaseValidationRuleDocument rules, CaseValidationEvaluationResult result)
    {
        foreach (var rule in rules.form_status_rules.Where(r => r.enabled))
        {
            var status = TokenToString(GetTokensByPath(caseData, rule.status_field_path).FirstOrDefault());
            var meaningfulCount = CountMeaningfulData(GetTokenByPath(caseData, rule.form_path), $"{rule.form_path}/", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rule.status_field_path });
            var statusKind = NormalizeFormStatus(status);
            var expected = "Status should match the meaningful data present in the form.";
            var message = $"{rule.form_prompt} status matches the current form data.";
            var isFinding = false;

            if (meaningfulCount >= rule.data_present_min_meaningful_fields &&
                (statusKind == "not-started" || statusKind == "not-applicable" || statusKind == "not-available"))
            {
                isFinding = true;
                expected = "In Progress or Completed when data are present";
                message = rule.message;
            }

            if (statusKind == "completed" && meaningfulCount < rule.completed_min_meaningful_fields)
            {
                isFinding = true;
                expected = $"At least {rule.completed_min_meaningful_fields} meaningful fields";
                message = $"{rule.form_prompt} is marked Completed but has little meaningful data.";
            }

            AddCheckAndFinding(result, new CaseValidationFinding
            {
                id = $"{rule.id}:form-status:{result.checks.Count}",
                rule_id = rule.id,
                category = "form-status",
                severity = isFinding ? rule.severity : "ok",
                form_path = rule.form_path,
                form_prompt = rule.form_prompt,
                field_path = rule.status_field_path,
                prompt = rule.status_field_prompt,
                subject = "form status",
                value = status,
                expected = expected,
                message = message,
                is_finding = isFinding,
                validation_level = rule.validation_level,
                confidence = rule.confidence,
                review_status = rule.review_status,
                source = rule.source,
                rationale = rule.rationale,
                admin_notes = rule.admin_notes,
                explanation = rule.explanation,
                can_quick_edit = true
            });
        }
    }

    private static void EvaluateFieldRules(
        JObject caseData,
        CaseValidationRuleDocument rules,
        Dictionary<string, CaseValidationFlattenedField> fieldMap,
        CaseValidationEvaluationResult result,
        string evaluationContext = null)
    {
        foreach (var rule in rules.field_rules.Where(r => r.enabled))
        {
            fieldMap.TryGetValue(rule.field_path, out var field);
            var values = GetTokensByPath(caseData, rule.field_path).ToList();
            if (values.Count == 0)
            {
                values.Add(null);
            }

            foreach (var token in values)
            {
                var valueText = TokenToString(token);
                var expected = string.Empty;
                string message = null;
                var isFinding = !IsMeaninglessToken(token) && TryFindFieldRuleIssue(rule, token, out expected, out message);
                if (!isFinding)
                {
                    expected = BuildFieldRuleExpectedText(rule);
                    message = rule.rationale ?? rule.message;
                }

                // Apply evaluation context: downgrade hard rules to warning in historical context
                var effectiveSeverity = isFinding ? rule.severity : "ok";
                if (isFinding && evaluationContext == "historical" && rule.severity == "hard")
                {
                    effectiveSeverity = "warning";
                }

                AddCheckAndFinding(result, new CaseValidationFinding
                {
                    id = $"{rule.id}:{result.checks.Count}",
                    rule_id = rule.id,
                    category = "range",
                    severity = effectiveSeverity,
                    form_path = rule.form_path,
                    form_prompt = rule.form_prompt,
                    field_path = rule.field_path,
                    metadata_path = rule.metadata_path,
                    prompt = rule.prompt,
                    subject = rule.subject,
                    value = valueText,
                    expected = expected,
                    message = string.IsNullOrWhiteSpace(message) ? rule.message : message,
                    is_finding = isFinding,
                    validation_level = rule.validation_level,
                    confidence = rule.confidence,
                    review_status = rule.review_status,
                    source = rule.source,
                    rationale = rule.rationale,
                    admin_notes = rule.admin_notes,
                    explanation = rule.explanation,
                    can_quick_edit = field?.can_quick_edit == true
                });
            }
        }
    }

    private static void EvaluateConnectedFieldRules(
        JObject caseData,
        CaseValidationRuleDocument rules,
        Dictionary<string, CaseValidationFlattenedField> fieldMap,
        CaseValidationEvaluationResult result)
    {
        foreach (var rule in rules.connected_field_rules.Where(r => r.enabled))
        {
            foreach (var pair in GetConnectedRuleTokenPairs(caseData, rule))
            {
                var value = pair.Value;
                var related = pair.Related;
                var finding = TryFindConnectedRuleIssue(rule, pair, out var expected);

                fieldMap.TryGetValue(rule.field_path, out var field);
                AddCheckAndFinding(result, new CaseValidationFinding
                {
                    id = $"{rule.id}:{result.checks.Count}",
                    rule_id = rule.id,
                    category = "connected-field",
                    severity = finding ? rule.severity : "ok",
                    form_path = rule.form_path,
                    form_prompt = rule.form_prompt,
                    field_path = rule.field_path,
                    related_field_path = rule.related_field_path,
                    metadata_path = rule.metadata_path,
                    prompt = rule.prompt,
                    related_prompt = rule.related_prompt,
                    subject = rule.subject,
                    value = TokenToString(value),
                    expected = string.IsNullOrWhiteSpace(expected) ? BuildConnectedRuleExpectedText(rule) : expected,
                    message = finding ? rule.message : rule.rationale ?? rule.message,
                    is_finding = finding,
                    validation_level = rule.validation_level,
                    confidence = rule.confidence,
                    review_status = rule.review_status,
                    source = rule.source,
                    rationale = rule.rationale,
                    admin_notes = rule.admin_notes,
                    explanation = rule.explanation,
                    can_quick_edit = field?.can_quick_edit == true
                });
            }
        }
    }

    private sealed class ConnectedRuleTokenPair
    {
        public JToken Value { get; init; }
        public JToken Related { get; init; }
        public Dictionary<string, JToken> RelatedValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<ConnectedRuleTokenPair> GetConnectedRuleTokenPairs(JObject caseData, CaseValidationConnectedFieldRule rule)
    {
        var relatedPaths = GetRuleRelatedPaths(rule).ToList();
        if (rule.require_same_container)
        {
            var sharedPath = GetSharedContainerPath(new[] { rule.field_path }.Concat(relatedPaths));
            if (!string.IsNullOrWhiteSpace(sharedPath))
            {
                var containers = GetTokensByPath(caseData, sharedPath).ToList();
                if (containers.Count == 0)
                {
                    return new[] { new ConnectedRuleTokenPair() };
                }

                var valueSuffix = RemovePathPrefix(rule.field_path, sharedPath);
                return containers.SelectMany(container =>
                    BuildIndexedTokenPairs(
                        GetTokensByRelativePath(container, valueSuffix),
                        relatedPaths.ToDictionary(path => path, path => GetTokensByRelativePath(container, RemovePathPrefix(path, sharedPath)), StringComparer.OrdinalIgnoreCase)));
            }
        }

        return BuildIndexedTokenPairs(
            GetTokensByPath(caseData, rule.field_path),
            relatedPaths.ToDictionary(path => path, path => GetTokensByPath(caseData, path), StringComparer.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetRuleRelatedPaths(CaseValidationConnectedFieldRule rule)
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(rule.related_field_path))
        {
            result.Add(rule.related_field_path);
        }

        if (rule.related_field_paths != null)
        {
            result.AddRange(rule.related_field_paths.Where(path => !string.IsNullOrWhiteSpace(path)));
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<ConnectedRuleTokenPair> BuildIndexedTokenPairs(IEnumerable<JToken> values, Dictionary<string, IEnumerable<JToken>> relatedValuesByPath)
    {
        var valueList = values?.ToList() ?? new List<JToken>();
        var relatedLists = (relatedValuesByPath ?? new Dictionary<string, IEnumerable<JToken>>(StringComparer.OrdinalIgnoreCase))
            .ToDictionary(
                item => item.Key,
                item =>
                {
                    var list = item.Value?.ToList() ?? new List<JToken>();
                    if (list.Count == 0)
                    {
                        list.Add(null);
                    }

                    return list;
                },
                StringComparer.OrdinalIgnoreCase);

        if (valueList.Count == 0)
        {
            valueList.Add(null);
        }

        var count = Math.Max(valueList.Count, relatedLists.Values.Select(list => list.Count).DefaultIfEmpty(1).Max());
        for (var i = 0; i < count; i++)
        {
            var relatedValues = relatedLists.ToDictionary(
                item => item.Key,
                item => item.Value.Count == 1 ? item.Value[0] : item.Value[Math.Min(i, item.Value.Count - 1)],
                StringComparer.OrdinalIgnoreCase);

            yield return new ConnectedRuleTokenPair
            {
                Value = valueList.Count == 1 ? valueList[0] : valueList[Math.Min(i, valueList.Count - 1)],
                Related = relatedValues.TryGetValue(relatedLists.Keys.FirstOrDefault() ?? string.Empty, out var related) ? related : null,
                RelatedValues = relatedValues
            };
        }
    }

    private static IEnumerable<JToken> GetTokensByRelativePath(JToken container, string relativePath)
    {
        if (container == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            yield return container;
            yield break;
        }

        foreach (var token in GetTokensByPath(container, relativePath))
        {
            yield return token;
        }
    }

    private static string GetSharedContainerPath(string leftPath, string rightPath)
    {
        return GetSharedContainerPath(new[] { leftPath, rightPath });
    }

    private static string GetSharedContainerPath(IEnumerable<string> paths)
    {
        var splitPaths = paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(SplitPath).Where(path => path.Length > 0).ToList();
        if (splitPaths.Count == 0)
        {
            return string.Empty;
        }

        var count = 0;
        while (splitPaths.All(path => count < path.Length && string.Equals(path[count], splitPaths[0][count], StringComparison.OrdinalIgnoreCase)))
        {
            count++;
        }

        return count == 0 ? string.Empty : string.Join("/", splitPaths[0].Take(count));
    }

    private static string RemovePathPrefix(string path, string prefix)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return path;
        }

        if (string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
            ? path.Substring(prefix.Length + 1)
            : path;
    }

    private static string[] SplitPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? Array.Empty<string>()
            : path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private static void AddCheckAndFinding(CaseValidationEvaluationResult result, CaseValidationFinding check)
    {
        result.checks.Add(check);
        if (check.is_finding)
        {
            result.findings.Add(check);
        }
    }

    private static string BuildFieldRuleExpectedText(CaseValidationFieldRule rule)
    {
        var parts = new List<string>();
        if (rule.min_value.HasValue)
        {
            parts.Add($">= {rule.min_value.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (rule.max_value.HasValue)
        {
            parts.Add($"<= {rule.max_value.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (rule.max_length.HasValue)
        {
            parts.Add($"length <= {rule.max_length.Value}");
        }

        if (!string.IsNullOrWhiteSpace(rule.regex_pattern))
        {
            parts.Add($"pattern {rule.regex_pattern}");
        }

        if (rule.allowed_values?.Count > 0)
        {
            parts.Add("accepted list value");
        }

        return string.Join(", ", parts);
    }

    private static string BuildConnectedRuleExpectedText(CaseValidationConnectedFieldRule rule)
    {
        if (string.Equals(rule.rule_type, "numeric_less_than_or_equal", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} <= {rule.related_prompt}";
        }

        if (string.Equals(rule.rule_type, "numeric_greater_than_or_equal", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} >= {rule.related_prompt}";
        }

        if (string.Equals(rule.rule_type, "numeric_max", StringComparison.OrdinalIgnoreCase) && rule.max_difference.HasValue)
        {
            return $"Value <= {rule.max_difference.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        if (string.Equals(rule.rule_type, "date_less_than_or_equal", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} on or before {rule.related_prompt}";
        }

        if (string.Equals(rule.rule_type, "datetime_less_than_or_equal", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} on or before {rule.related_prompt}";
        }

        if (string.Equals(rule.rule_type, "date_greater_than_or_equal", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} on or after {rule.related_prompt}";
        }

        if (string.Equals(rule.rule_type, "date_equal", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} matches {rule.related_prompt}";
        }

        if (string.Equals(rule.rule_type, "conditional_other_requires_specify", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.related_prompt} is entered when Other is selected";
        }

        if (string.Equals(rule.rule_type, "date_within_window", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} falls within the configured date window";
        }

        if (string.Equals(rule.rule_type, "requires_related_when_present", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.related_prompt} is present when {rule.prompt} is present";
        }

        if (string.Equals(rule.rule_type, "numeric_sum_equals", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} is consistent with the configured related numeric fields";
        }

        if (string.Equals(rule.rule_type, "numeric_difference_matches", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} matches the configured numeric difference";
        }

        if (string.Equals(rule.rule_type, "age_matches_dates", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} matches the configured birth and event dates";
        }

        if (string.Equals(rule.rule_type, "bmi_matches_height_weight", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} matches height and weight within tolerance";
        }

        if (string.Equals(rule.rule_type, "unit_numeric_range", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} is plausible for the selected unit";
        }

        if (string.Equals(rule.rule_type, "gestational_age_matches_anchor", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} aligns with pregnancy anchor date and gestational age";
        }

        if (string.Equals(rule.rule_type, "dependent_grid_consistency", StringComparison.OrdinalIgnoreCase))
        {
            return $"{rule.prompt} is consistent with dependent detail fields";
        }

        return rule.comparison;
    }

    private static bool TryFindConnectedRuleIssue(CaseValidationConnectedFieldRule rule, ConnectedRuleTokenPair pair, out string expected)
    {
        expected = BuildConnectedRuleExpectedText(rule);
        var value = pair.Value;
        var related = pair.Related;

        if (TryFindCompoundConnectedRuleIssue(rule, pair, out expected))
        {
            return true;
        }

        if (IsCompoundConnectedRule(rule))
        {
            return false;
        }

        if (IsMeaninglessToken(value) || IsMeaninglessToken(related))
        {
            return false;
        }

        if (string.Equals(rule.rule_type, "numeric_less_than_or_equal", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDouble(value, out var lessThanLeft) &&
            TryTokenDouble(related, out var lessThanRight))
        {
            return lessThanLeft > lessThanRight;
        }

        if (string.Equals(rule.rule_type, "numeric_greater_than_or_equal", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDouble(value, out var greaterThanLeft) &&
            TryTokenDouble(related, out var greaterThanRight))
        {
            return greaterThanLeft < greaterThanRight;
        }

        if (string.Equals(rule.rule_type, "numeric_max", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDouble(value, out var numericValue) &&
            rule.max_difference.HasValue)
        {
            return numericValue > rule.max_difference.Value;
        }

        if (string.Equals(rule.rule_type, "date_less_than_or_equal", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDate(value, out var leftDate) &&
            TryTokenDate(related, out var rightDate))
        {
            return leftDate > rightDate;
        }

        if (string.Equals(rule.rule_type, "datetime_less_than_or_equal", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDateTime(value, out var leftDateTime) &&
            TryTokenDateTime(related, out var rightDateTime))
        {
            return leftDateTime.Date > rightDateTime.Date ||
                   (leftDateTime.Date == rightDateTime.Date &&
                    leftDateTime.Time.HasValue &&
                    rightDateTime.Time.HasValue &&
                    leftDateTime.Time.Value > rightDateTime.Time.Value);
        }

        if (string.Equals(rule.rule_type, "date_greater_than_or_equal", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDate(value, out var laterDate) &&
            TryTokenDate(related, out var earlierDate))
        {
            return laterDate < earlierDate;
        }

        if (string.Equals(rule.rule_type, "date_equal", StringComparison.OrdinalIgnoreCase) &&
            TryTokenDate(value, out var firstDate) &&
            TryTokenDate(related, out var secondDate))
        {
            return firstDate != secondDate;
        }

        if (string.Equals(rule.rule_type, "conditional_other_requires_specify", StringComparison.OrdinalIgnoreCase))
        {
            return TokenHasTriggerValue(value, rule) && IsMeaninglessToken(related);
        }

        return false;
    }

    private static bool IsCompoundConnectedRule(CaseValidationConnectedFieldRule rule)
    {
        return rule.rule_type is "date_within_window" or
            "requires_related_when_present" or
            "numeric_sum_equals" or
            "numeric_difference_matches" or
            "age_matches_dates" or
            "bmi_matches_height_weight" or
            "unit_numeric_range" or
            "gestational_age_matches_anchor" or
            "dependent_grid_consistency";
    }

    private static bool TryFindCompoundConnectedRuleIssue(CaseValidationConnectedFieldRule rule, ConnectedRuleTokenPair pair, out string expected)
    {
        expected = BuildConnectedRuleExpectedText(rule);
        if (string.Equals(rule.rule_type, "date_within_window", StringComparison.OrdinalIgnoreCase))
        {
            var start = GetRelatedToken(pair, ParameterText(rule, "start_field_path") ?? rule.related_field_path);
            var end = GetRelatedToken(pair, ParameterText(rule, "end_field_path"));
            if (!TryTokenDateTime(pair.Value, out var valueDateTime))
            {
                return false;
            }

            var hasStart = TryTokenDateTime(start, out var startDateTime);
            var hasEnd = TryTokenDateTime(end, out var endDateTime);
            if (hasStart && CompareDateTimeParts(valueDateTime, startDateTime) < 0)
            {
                expected = $"{rule.prompt} on or after start of window";
                return true;
            }

            if (hasEnd && CompareDateTimeParts(valueDateTime, endDateTime) > 0)
            {
                expected = $"{rule.prompt} on or before end of window";
                return true;
            }

            return false;
        }

        if (string.Equals(rule.rule_type, "requires_related_when_present", StringComparison.OrdinalIgnoreCase))
        {
            if (IsMeaninglessToken(pair.Value))
            {
                return false;
            }

            if (ParameterBool(rule, "only_positive") && (!TryTokenDouble(pair.Value, out var numericValue) || numericValue <= 0))
            {
                return false;
            }

            var relatedTokens = GetRuleRelatedPaths(rule).Select(path => GetRelatedToken(pair, path)).ToList();
            if (ParameterBool(rule, "require_any_related"))
            {
                return relatedTokens.All(IsMeaninglessToken);
            }

            return relatedTokens.Count > 0 && IsMeaninglessToken(relatedTokens[0]);
        }

        if (string.Equals(rule.rule_type, "numeric_sum_equals", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryTokenDouble(pair.Value, out var value))
            {
                return false;
            }

            var relatedNumbers = GetRuleRelatedPaths(rule)
                .Select(path => GetRelatedToken(pair, path))
                .Select(token => TryTokenDouble(token, out var number) ? (double?)number : null)
                .ToList();
            if (relatedNumbers.Count == 0 || relatedNumbers.Any(number => !number.HasValue))
            {
                return false;
            }

            var sum = relatedNumbers.Sum(number => number.Value);
            var tolerance = ParameterDouble(rule, "tolerance", 0);
            var comparison = ParameterText(rule, "comparison") ?? "equals_sum";
            if (string.Equals(comparison, "greater_or_equal_sum", StringComparison.OrdinalIgnoreCase))
            {
                expected = $"{rule.prompt} >= related sum {sum.ToString(CultureInfo.InvariantCulture)}";
                return value + tolerance < sum;
            }

            expected = $"{rule.prompt} = related sum {sum.ToString(CultureInfo.InvariantCulture)}";
            return Math.Abs(value - sum) > tolerance;
        }

        if (string.Equals(rule.rule_type, "numeric_difference_matches", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryTokenDouble(pair.Value, out var value) ||
                !TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "minuend_field_path") ?? rule.related_field_path), out var minuend) ||
                !TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "subtrahend_field_path")), out var subtrahend))
            {
                return false;
            }

            var expectedValue = minuend - subtrahend;
            var tolerance = ParameterDouble(rule, "tolerance", 0);
            expected = $"{rule.prompt} within {tolerance.ToString(CultureInfo.InvariantCulture)} of {expectedValue.ToString(CultureInfo.InvariantCulture)}";
            return Math.Abs(value - expectedValue) > tolerance;
        }

        if (string.Equals(rule.rule_type, "age_matches_dates", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryTokenDouble(pair.Value, out var age) ||
                !TryTokenDate(GetRelatedToken(pair, rule.related_field_path), out var birthDate) ||
                !TryTokenDate(GetRelatedToken(pair, ParameterText(rule, "event_date_field_path")), out var eventDate))
            {
                return false;
            }

            var expectedAge = eventDate.Year - birthDate.Year;
            if (eventDate < birthDate.AddYears(expectedAge))
            {
                expectedAge--;
            }

            var tolerance = ParameterDouble(rule, "tolerance_years", 1);
            expected = $"{rule.prompt} within {tolerance.ToString(CultureInfo.InvariantCulture)} year(s) of {expectedAge}";
            return Math.Abs(age - expectedAge) > tolerance;
        }

        if (string.Equals(rule.rule_type, "bmi_matches_height_weight", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryTokenDouble(pair.Value, out var bmi) ||
                !TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "height_feet_field_path")), out var feet) ||
                !TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "height_inches_field_path")), out var inches) ||
                !TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "weight_field_path")), out var weight))
            {
                return false;
            }

            var totalInches = feet * 12 + inches;
            if (totalInches <= 0 || weight <= 0)
            {
                return false;
            }

            var expectedBmi = weight * 703 / (totalInches * totalInches);
            var tolerance = ParameterDouble(rule, "tolerance", 1);
            expected = $"{rule.prompt} within {tolerance.ToString(CultureInfo.InvariantCulture)} of {expectedBmi.ToString("0.0", CultureInfo.InvariantCulture)}";
            return Math.Abs(bmi - expectedBmi) > tolerance;
        }

        if (string.Equals(rule.rule_type, "unit_numeric_range", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryTokenDouble(pair.Value, out var value))
            {
                return false;
            }

            var min = ParameterDouble(rule, "default_min", double.MinValue);
            var max = ParameterDouble(rule, "default_max", double.MaxValue);
            if (value < min || value > max)
            {
                expected = $"{rule.prompt} between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)} for selected unit";
                return true;
            }

            var ouncePath = ParameterText(rule, "ounce_field_path");
            if (!string.IsNullOrWhiteSpace(ouncePath) && TryTokenDouble(GetRelatedToken(pair, ouncePath), out var ounces))
            {
                var ounceMin = ParameterDouble(rule, "ounce_min", 0);
                var ounceMax = ParameterDouble(rule, "ounce_max", 15);
                if (ounces < ounceMin || ounces > ounceMax)
                {
                    expected = $"Ounce remainder between {ounceMin.ToString(CultureInfo.InvariantCulture)} and {ounceMax.ToString(CultureInfo.InvariantCulture)}";
                    return true;
                }
            }

            return false;
        }

        if (string.Equals(rule.rule_type, "gestational_age_matches_anchor", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryTokenDate(pair.Value, out var eventDate) ||
                !TryTokenDate(GetRelatedToken(pair, ParameterText(rule, "anchor_date_field_path")), out var anchorDate) ||
                !TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "weeks_field_path")), out var weeks))
            {
                return false;
            }

            TryTokenDouble(GetRelatedToken(pair, ParameterText(rule, "days_field_path")), out var days);
            var actualDays = eventDate.DayNumber - anchorDate.DayNumber;
            if (actualDays < 0)
            {
                return false;
            }

            var statedDays = (int)Math.Round(weeks * 7 + days);
            var tolerance = ParameterDouble(rule, "tolerance_days", 14);
            expected = $"Gestational age within {tolerance.ToString(CultureInfo.InvariantCulture)} day(s) of date-derived age";
            return Math.Abs(actualDays - statedDays) > tolerance;
        }

        if (string.Equals(rule.rule_type, "dependent_grid_consistency", StringComparison.OrdinalIgnoreCase))
        {
            var mode = ParameterText(rule, "mode") ?? string.Empty;
            if (string.Equals(mode, "specify_blank_unless_trigger", StringComparison.OrdinalIgnoreCase))
            {
                return !IsMeaninglessToken(pair.Value) && !TokenHasTriggerValue(pair.Related, rule);
            }

            if (string.Equals(mode, "grid_requires_yes", StringComparison.OrdinalIgnoreCase))
            {
                var grid = GetRelatedToken(pair, ParameterText(rule, "grid_path") ?? rule.related_field_path);
                var hasGridData = CountMeaningfulData(grid, string.Empty, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) > 0;
                return hasGridData && !MatchesTrigger(TokenToString(pair.Value), ParameterStringList(rule, "yes_values"));
            }

            if (string.Equals(mode, "multiple_gestation_birth_order", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryTokenDouble(pair.Value, out var birthOrder) || birthOrder <= 1)
                {
                    return false;
                }

                var plurality = GetRelatedToken(pair, "birth_fetal_death_certificate_parent/prenatal_care/plurality");
                var multiple = GetRelatedToken(pair, "birth_certificate_infant_fetal_section/is_multiple_gestation");
                var pluralityOk = TryTokenDouble(plurality, out var pluralityNumber) && pluralityNumber > 1;
                var multipleOk = MatchesTrigger(TokenToString(multiple), new[] { "1", "yes", "true" });
                expected = "Plurality or multiple gestation should indicate more than one fetus when birth order is greater than 1";
                return !pluralityOk && !multipleOk;
            }
        }

        return false;
    }

    private static int CompareDateTimeParts((DateOnly Date, TimeOnly? Time) left, (DateOnly Date, TimeOnly? Time) right)
    {
        var dateCompare = left.Date.CompareTo(right.Date);
        if (dateCompare != 0)
        {
            return dateCompare;
        }

        if (left.Time.HasValue && right.Time.HasValue)
        {
            return left.Time.Value.CompareTo(right.Time.Value);
        }

        return 0;
    }

    private static JToken GetRelatedToken(ConnectedRuleTokenPair pair, string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && pair.RelatedValues.TryGetValue(path, out var token))
        {
            return token;
        }

        return pair.Related;
    }

    private static string ParameterText(CaseValidationConnectedFieldRule rule, string name)
    {
        return rule.parameters?[name]?.Type == JTokenType.String
            ? rule.parameters[name].Value<string>()
            : rule.parameters?[name]?.ToString(Formatting.None).Trim('"');
    }

    private static bool ParameterBool(CaseValidationConnectedFieldRule rule, string name)
    {
        var token = rule.parameters?[name];
        if (token == null)
        {
            return false;
        }

        return token.Type == JTokenType.Boolean
            ? token.Value<bool>()
            : bool.TryParse(TokenToString(token), out var value) && value;
    }

    private static double ParameterDouble(CaseValidationConnectedFieldRule rule, string name, double defaultValue)
    {
        var token = rule.parameters?[name];
        return token != null && double.TryParse(TokenToString(token), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    private static IEnumerable<string> ParameterStringList(CaseValidationConnectedFieldRule rule, string name)
    {
        var token = rule.parameters?[name];
        if (token == null)
        {
            return Enumerable.Empty<string>();
        }

        return token.Type == JTokenType.Array
            ? token.Values<string>().Where(value => !string.IsNullOrWhiteSpace(value))
            : new[] { TokenToString(token) }.Where(value => !string.IsNullOrWhiteSpace(value));
    }

    private static bool TryFindFieldRuleIssue(CaseValidationFieldRule rule, JToken token, out string expected, out string message)
    {
        expected = string.Empty;
        message = null;
        var text = TokenToString(token);
        if (rule.allowed_values?.Count > 0 && !rule.allowed_values.Contains(text, StringComparer.OrdinalIgnoreCase))
        {
            expected = "One of the accepted list values";
            message = $"{rule.prompt} is not an accepted list value.";
            return true;
        }

        if ((rule.min_value.HasValue || rule.max_value.HasValue) && TryTokenDouble(token, out var numericValue))
        {
            if (rule.min_value.HasValue && numericValue < rule.min_value.Value)
            {
                expected = $">= {rule.min_value.Value.ToString(CultureInfo.InvariantCulture)}";
                return true;
            }

            if (rule.max_value.HasValue && numericValue > rule.max_value.Value)
            {
                expected = $"<= {rule.max_value.Value.ToString(CultureInfo.InvariantCulture)}";
                return true;
            }
        }

        if (rule.max_length.HasValue && text.Length > rule.max_length.Value)
        {
            expected = $"Length <= {rule.max_length.Value}";
            return true;
        }

        if (!string.IsNullOrWhiteSpace(rule.regex_pattern))
        {
            try
            {
                if (!Regex.IsMatch(text, rule.regex_pattern))
                {
                    expected = $"Pattern {rule.regex_pattern}";
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return false;
    }

    public int CountMeaningfulData(JToken token)
    {
        return CountMeaningfulData(token, string.Empty, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static int CountMeaningfulData(JToken token, string pathPrefix, HashSet<string> ignoredPaths)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return 0;
        }

        if (token.Type == JTokenType.Object)
        {
            var count = 0;
            foreach (var property in token.Children<JProperty>())
            {
                var path = string.IsNullOrWhiteSpace(pathPrefix) ? property.Name : $"{pathPrefix.TrimEnd('/')}/{property.Name}";
                if (ignoredPaths.Contains(path))
                {
                    continue;
                }

                count += CountMeaningfulData(property.Value, path, ignoredPaths);
            }

            return count;
        }

        if (token.Type == JTokenType.Array)
        {
            return token.Children().Sum(child => CountMeaningfulData(child, pathPrefix, ignoredPaths));
        }

        return IsMeaninglessToken(token) ? 0 : 1;
    }

    private static bool TryApplyScalarFieldValue(
        mmria_case caseData,
        CaseValidationFlattenedField field,
        JToken value,
        int? formIndex,
        int? gridIndex,
        out string newValue,
        out string error)
    {
        newValue = TokenToString(value);
        error = null;
        try
        {
            var type = (field.type ?? string.Empty).ToLowerInvariant();
            var dataType = (field.data_type ?? string.Empty).ToLowerInvariant();
            switch (type)
            {
                case "number":
                    if (!TryConvertNullableDouble(value, out var doubleValue))
                    {
                        error = "Value must be numeric.";
                        return false;
                    }

                    return ApplyField(caseData, field, formIndex, gridIndex, "double", doubleValue, out error);

                case "boolean":
                    if (!TryConvertNullableBoolean(value, out var boolValue))
                    {
                        error = "Value must be true or false.";
                        return false;
                    }

                    return ApplyField(caseData, field, formIndex, gridIndex, "boolean", boolValue, out error);

                case "date":
                    if (!TryConvertNullableDateOnly(value, out var dateOnlyValue))
                    {
                        error = "Value must be a valid date.";
                        return false;
                    }

                    newValue = dateOnlyValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    return ApplyField(caseData, field, formIndex, gridIndex, "date", dateOnlyValue, out error);

                case "datetime":
                    if (!TryConvertNullableDateTime(value, out var dateTimeValue))
                    {
                        error = "Value must be a valid date and time.";
                        return false;
                    }

                    newValue = dateTimeValue?.ToString("o", CultureInfo.InvariantCulture);
                    return ApplyField(caseData, field, formIndex, gridIndex, "datetime", dateTimeValue, out error);

                case "time":
                    if (!TryConvertNullableTimeOnly(value, out var timeOnlyValue))
                    {
                        error = "Value must be a valid time.";
                        return false;
                    }

                    newValue = timeOnlyValue?.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                    return ApplyField(caseData, field, formIndex, gridIndex, "time", timeOnlyValue, out error);

                case "list":
                    if (string.Equals(dataType, "number", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dataType, "double", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TryConvertNullableDouble(value, out var listNumberValue))
                        {
                            error = "Value must be numeric.";
                            return false;
                        }

                        return ApplyField(caseData, field, formIndex, gridIndex, "double", listNumberValue, out error);
                    }

                    return ApplyField(caseData, field, formIndex, gridIndex, "string", newValue, out error);

                default:
                    return ApplyField(caseData, field, formIndex, gridIndex, "string", newValue, out error);
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool ApplyField(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, string setterType, object value, out string error)
    {
        error = null;
        var path = field.field_path;
        var success = setterType switch
        {
            "double" => ApplyDouble(caseData, field, formIndex, gridIndex, (double?)value),
            "boolean" => ApplyBoolean(caseData, field, formIndex, gridIndex, (bool?)value),
            "date" => ApplyDate(caseData, field, formIndex, gridIndex, (DateOnly?)value),
            "datetime" => ApplyDateTime(caseData, field, formIndex, gridIndex, (DateTime?)value),
            "time" => ApplyTime(caseData, field, formIndex, gridIndex, (TimeOnly?)value),
            _ => ApplyString(caseData, field, formIndex, gridIndex, value?.ToString())
        };

        if (!success)
        {
            error = $"The generated case setter rejected path {path}.";
        }

        return success;
    }

    private static string GetFieldValueAsString(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        var type = (field.type ?? string.Empty).ToLowerInvariant();
        var dataType = (field.data_type ?? string.Empty).ToLowerInvariant();
        object value = null;
        if (type == "number" || (type == "list" && (dataType == "number" || dataType == "double")))
        {
            value = GetDouble(caseData, field, formIndex, gridIndex);
        }
        else if (type == "boolean")
        {
            value = GetBoolean(caseData, field, formIndex, gridIndex);
        }
        else if (type == "date")
        {
            value = GetDate(caseData, field, formIndex, gridIndex);
        }
        else if (type == "datetime")
        {
            value = GetDateTime(caseData, field, formIndex, gridIndex);
        }
        else if (type == "time")
        {
            value = GetTime(caseData, field, formIndex, gridIndex);
        }
        else
        {
            value = GetString(caseData, field, formIndex, gridIndex);
        }

        return value switch
        {
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            _ => value?.ToString()
        };
    }

    private static bool ApplyString(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, string value)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue)
        {
            return caseData.SetMG_String(field.field_path, formIndex.Value, gridIndex.Value, value);
        }

        if (field.is_multiform && formIndex.HasValue)
        {
            return caseData.SetM_String(field.field_path, formIndex.Value, value);
        }

        if (field.is_grid && gridIndex.HasValue)
        {
            return caseData.SetSG_String(field.field_path, gridIndex.Value, value);
        }

        return caseData.SetS_String(field.field_path, value);
    }

    private static bool ApplyDouble(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, double? value)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue)
        {
            return caseData.SetMG_Double(field.field_path, formIndex.Value, gridIndex.Value, value);
        }

        if (field.is_multiform && formIndex.HasValue)
        {
            return caseData.SetM_Double(field.field_path, formIndex.Value, value);
        }

        if (field.is_grid && gridIndex.HasValue)
        {
            return caseData.SetSG_Double(field.field_path, gridIndex.Value, value);
        }

        return caseData.SetS_Double(field.field_path, value);
    }

    private static bool ApplyBoolean(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, bool? value)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue)
        {
            return caseData.SetMG_Boolean(field.field_path, formIndex.Value, gridIndex.Value, value);
        }

        if (field.is_multiform && formIndex.HasValue)
        {
            return caseData.SetM_Boolean(field.field_path, formIndex.Value, value);
        }

        if (field.is_grid && gridIndex.HasValue)
        {
            return caseData.SetSG_Boolean(field.field_path, gridIndex.Value, value);
        }

        return caseData.SetS_Boolean(field.field_path, value);
    }

    private static bool ApplyDate(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, DateOnly? value)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue)
        {
            return caseData.SetMG_Date_Only(field.field_path, formIndex.Value, gridIndex.Value, value);
        }

        if (field.is_multiform && formIndex.HasValue)
        {
            return caseData.SetM_Date_Only(field.field_path, formIndex.Value, value);
        }

        if (field.is_grid && gridIndex.HasValue)
        {
            return caseData.SetSG_Date_Only(field.field_path, gridIndex.Value, value);
        }

        return caseData.SetS_Date_Only(field.field_path, value);
    }

    private static bool ApplyDateTime(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, DateTime? value)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue)
        {
            return caseData.SetMG_Datetime(field.field_path, formIndex.Value, gridIndex.Value, value);
        }

        if (field.is_multiform && formIndex.HasValue)
        {
            return caseData.SetM_Datetime(field.field_path, formIndex.Value, value);
        }

        if (field.is_grid && gridIndex.HasValue)
        {
            return caseData.SetSG_Datetime(field.field_path, gridIndex.Value, value);
        }

        return caseData.SetS_Datetime(field.field_path, value);
    }

    private static bool ApplyTime(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex, TimeOnly? value)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue)
        {
            return caseData.SetMG_Time_Only(field.field_path, formIndex.Value, gridIndex.Value, value);
        }

        if (field.is_multiform && formIndex.HasValue)
        {
            return caseData.SetM_Time_Only(field.field_path, formIndex.Value, value);
        }

        if (field.is_grid && gridIndex.HasValue)
        {
            return caseData.SetSG_Time_Only(field.field_path, gridIndex.Value, value);
        }

        return caseData.SetS_Time_Only(field.field_path, value);
    }

    private static string GetString(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue) return caseData.GetSG_String(field.field_path, formIndex.Value, gridIndex.Value);
        if (field.is_multiform && formIndex.HasValue) return caseData.GetM_String(field.field_path, formIndex.Value);
        if (field.is_grid && gridIndex.HasValue) return caseData.GetSG_String(field.field_path, gridIndex.Value);
        return caseData.GetS_String(field.field_path);
    }

    private static double? GetDouble(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue) return caseData.GetSG_Double(field.field_path, formIndex.Value, gridIndex.Value);
        if (field.is_multiform && formIndex.HasValue) return caseData.GetM_Double(field.field_path, formIndex.Value);
        if (field.is_grid && gridIndex.HasValue) return caseData.GetSG_Double(field.field_path, gridIndex.Value);
        return caseData.GetS_Double(field.field_path);
    }

    private static bool? GetBoolean(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue) return caseData.GetSG_Boolean(field.field_path, formIndex.Value, gridIndex.Value);
        if (field.is_multiform && formIndex.HasValue) return caseData.GetM_Boolean(field.field_path, formIndex.Value);
        if (field.is_grid && gridIndex.HasValue) return caseData.GetSG_Boolean(field.field_path, gridIndex.Value);
        return caseData.GetS_Boolean(field.field_path);
    }

    private static DateOnly? GetDate(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue) return caseData.GetSG_Date_Only(field.field_path, formIndex.Value, gridIndex.Value);
        if (field.is_multiform && formIndex.HasValue) return caseData.GetM_Date_Only(field.field_path, formIndex.Value);
        if (field.is_grid && gridIndex.HasValue) return caseData.GetSG_Date_Only(field.field_path, gridIndex.Value);
        return caseData.GetS_Date_Only(field.field_path);
    }

    private static DateTime? GetDateTime(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue) return caseData.GetSG_Datetime(field.field_path, formIndex.Value, gridIndex.Value);
        if (field.is_multiform && formIndex.HasValue) return caseData.GetM_Datetime(field.field_path, formIndex.Value);
        if (field.is_grid && gridIndex.HasValue) return caseData.GetSG_Datetime(field.field_path, gridIndex.Value);
        return caseData.GetS_Datetime(field.field_path);
    }

    private static TimeOnly? GetTime(mmria_case caseData, CaseValidationFlattenedField field, int? formIndex, int? gridIndex)
    {
        if (field.is_multiform && field.is_grid && formIndex.HasValue && gridIndex.HasValue) return caseData.GetSG_Time_Only(field.field_path, formIndex.Value, gridIndex.Value);
        if (field.is_multiform && formIndex.HasValue) return caseData.GetM_Time_Only(field.field_path, formIndex.Value);
        if (field.is_grid && gridIndex.HasValue) return caseData.GetSG_Time_Only(field.field_path, gridIndex.Value);
        return caseData.GetS_Time_Only(field.field_path);
    }

    private static bool TryConvertNullableDouble(JToken token, out double? value)
    {
        value = null;
        if (IsMeaninglessToken(token)) return true;
        if (double.TryParse(TokenToString(token), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryConvertNullableBoolean(JToken token, out bool? value)
    {
        value = null;
        if (IsMeaninglessToken(token)) return true;
        if (bool.TryParse(TokenToString(token), out var parsed))
        {
            value = parsed;
            return true;
        }

        if (double.TryParse(TokenToString(token), NumberStyles.Any, CultureInfo.InvariantCulture, out var numeric))
        {
            value = numeric != 0;
            return true;
        }

        return false;
    }

    private static bool TryConvertNullableDateOnly(JToken token, out DateOnly? value)
    {
        value = null;
        if (IsMeaninglessToken(token)) return true;
        if (DateOnly.TryParse(TokenToString(token), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryConvertNullableDateTime(JToken token, out DateTime? value)
    {
        value = null;
        if (IsMeaninglessToken(token)) return true;
        if (DateTime.TryParse(TokenToString(token), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryConvertNullableTimeOnly(JToken token, out TimeOnly? value)
    {
        value = null;
        if (IsMeaninglessToken(token)) return true;
        if (TimeOnly.TryParse(TokenToString(token), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool IsCheckedOutByCurrentUser(mmria_case caseData, string userName)
    {
        return !string.IsNullOrWhiteSpace(userName) &&
               !string.IsNullOrWhiteSpace(caseData.last_checked_out_by) &&
               string.Equals(caseData.last_checked_out_by, userName, StringComparison.OrdinalIgnoreCase) &&
               caseData.date_last_checked_out.HasValue;
    }

    private static JToken GetTokenByPath(JToken root, string path)
    {
        return GetTokensByPath(root, path).FirstOrDefault();
    }

    private static IEnumerable<JToken> GetTokensByPath(JToken root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        foreach (var token in GetTokensByParts(root, path.Split('/', StringSplitOptions.RemoveEmptyEntries), 0))
        {
            yield return token;
        }
    }

    private static IEnumerable<JToken> GetTokensByParts(JToken token, string[] parts, int index)
    {
        if (token == null)
        {
            yield break;
        }

        if (index >= parts.Length)
        {
            yield return token;
            yield break;
        }

        if (token.Type == JTokenType.Array)
        {
            foreach (var child in token.Children())
            {
                foreach (var found in GetTokensByParts(child, parts, index))
                {
                    yield return found;
                }
            }

            yield break;
        }

        var next = token[parts[index]];
        foreach (var found in GetTokensByParts(next, parts, index + 1))
        {
            yield return found;
        }
    }

    private static bool TryTokenDouble(JToken token, out double value)
    {
        return double.TryParse(TokenToString(token), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryTokenDate(JToken token, out DateOnly value)
    {
        value = default;
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return false;
        }

        if (token.Type == JTokenType.Object)
        {
            var monthText = TokenToString(token["month"]);
            var dayText = TokenToString(token["day"]);
            var yearText = TokenToString(token["year"]);
            if (MeaninglessValues.Contains(monthText ?? string.Empty) ||
                MeaninglessValues.Contains(dayText ?? string.Empty) ||
                MeaninglessValues.Contains(yearText ?? string.Empty))
            {
                return false;
            }

            if (int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var month) &&
                int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) &&
                int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) &&
                year >= 1 &&
                month is >= 1 and <= 12 &&
                day is >= 1 and <= 31)
            {
                try
                {
                    value = new DateOnly(year, month, day);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }
            }
        }

        var text = TokenToString(token);
        if (string.IsNullOrWhiteSpace(text) || MeaninglessValues.Contains(text.Trim()))
        {
            return false;
        }

        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return true;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            value = DateOnly.FromDateTime(dateTime);
            return true;
        }

        return false;
    }

    private static bool TryTokenDateTime(JToken token, out (DateOnly Date, TimeOnly? Time) value)
    {
        value = default;
        if (!TryTokenDate(token, out var date))
        {
            return false;
        }

        TimeOnly? time = null;
        if (token?.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                if (property.Name.Contains("time", StringComparison.OrdinalIgnoreCase) &&
                    TryTokenTime(property.Value, out var parsedTime))
                {
                    time = parsedTime;
                    break;
                }
            }
        }
        else
        {
            var text = TokenToString(token);
            if (!string.IsNullOrWhiteSpace(text) &&
                DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDateTime))
            {
                time = TimeOnly.FromDateTime(parsedDateTime);
            }
        }

        value = (date, time);
        return true;
    }

    private static bool TryTokenTime(JToken token, out TimeOnly value)
    {
        value = default;
        var text = TokenToString(token);
        if (string.IsNullOrWhiteSpace(text) || MeaninglessValues.Contains(text.Trim()))
        {
            return false;
        }

        if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return true;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            value = TimeOnly.FromDateTime(dateTime);
            return true;
        }

        return false;
    }

    private static bool TokenHasTriggerValue(JToken token, CaseValidationConnectedFieldRule rule)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return false;
        }

        if (token.Type == JTokenType.Array)
        {
            return token.Children().Any(child => TokenHasTriggerValue(child, rule));
        }

        var text = TokenToString(token);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return MatchesTrigger(text, rule.trigger_values) || MatchesTrigger(text, rule.trigger_displays);
    }

    private static bool MatchesTrigger(string text, IEnumerable<string> triggers)
    {
        if (triggers == null)
        {
            return false;
        }

        var trimmed = text?.Trim() ?? string.Empty;
        foreach (var trigger in triggers.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            var candidate = trigger.Trim();
            if (string.Equals(trimmed, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var valueNumber) &&
                double.TryParse(candidate, NumberStyles.Any, CultureInfo.InvariantCulture, out var triggerNumber) &&
                Math.Abs(valueNumber - triggerNumber) < 0.000001)
            {
                return true;
            }
        }

        return false;
    }

    private static string TokenToString(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return null;
        }

        return token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Formatting.None).Trim('"');
    }

    private static bool IsMeaninglessToken(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return true;
        }

        if (token.Type == JTokenType.Array && !token.HasValues)
        {
            return true;
        }

        if (token.Type == JTokenType.Object && !token.HasValues)
        {
            return true;
        }

        var value = TokenToString(token);
        return value == null || MeaninglessValues.Contains(value.Trim());
    }

    private static string NormalizeFormStatus(string status)
    {
        return status?.Trim() switch
        {
            "0" => "not-started",
            "1" => "in-progress",
            "2" => "completed",
            "3" => "not-available",
            "4" => "not-applicable",
            _ => NormalizeSubject(status ?? string.Empty).Replace(" ", "-", StringComparison.Ordinal)
        };
    }

    private static bool IsFormStatusDisplay(string display)
    {
        var normalized = NormalizeSubject(display);
        return normalized is "not started" or "in progress" or "completed" or "not available" or "not applicable";
    }

    private static bool IsNodeType(node current, string type)
    {
        return string.Equals(current?.type, type, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMulti(node current)
    {
        return current?.cardinality == "*" || current?.cardinality == "+";
    }

    private static bool IsMultiSelect(node current)
    {
        return current?.is_multiselect == true;
    }

    private static bool IsNumericField(CaseValidationFlattenedField field)
    {
        return string.Equals(field.type, "number", StringComparison.OrdinalIgnoreCase) ||
               (string.Equals(field.type, "list", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(field.data_type, "number", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(field.data_type, "double", StringComparison.OrdinalIgnoreCase)));
    }

    private static string BuildSubject(node current, IEnumerable<string> ancestry)
    {
        var parts = ancestry.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        if (!string.IsNullOrWhiteSpace(current.prompt))
        {
            parts.Add(current.prompt);
        }

        if (current.tags != null)
        {
            parts.AddRange(current.tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        return string.Join(" / ", parts);
    }

    private static string NormalizeSubject(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }

    private static double SubjectSimilarity(string a, string b)
    {
        var left = NormalizeSubject(a).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var right = NormalizeSubject(b).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var intersection = left.Count(term => right.Contains(term));
        return intersection / (double)Math.Max(left.Count, right.Count);
    }

    private static string BuildObjectPath(string fieldPath)
    {
        return "g_data." + fieldPath.Replace("/", ".", StringComparison.Ordinal);
    }

    private static string GetUserName(ClaimsPrincipal user)
    {
        if (user?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            return user.Identities.First(
                u => u.IsAuthenticated &&
                     u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name)?.Value;
        }

        return null;
    }
}

