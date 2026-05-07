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

        return BuildDefaultRuleDocument(metadataVersion, metadata, userName);
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
        var document = new CaseValidationRuleDocument
        {
            _id = CaseValidationDAL.CreateDocumentId(metadataVersion),
            metadata_version = metadataVersion,
            date_created = now,
            created_by = userName,
            date_last_updated = now,
            last_updated_by = userName
        };

        var fields = FlattenMetadata(metadata);
        foreach (var field in fields.Where(f => f.is_scalar))
        {
            var rule = CreateFieldRule(field);
            if (rule != null)
            {
                document.field_rules.Add(rule);
            }
        }

        document.form_status_rules.AddRange(CreateFormStatusRules(fields));
        document.connected_field_rules.AddRange(CreateConnectedFieldRules(fields));
        NormalizeRuleDocumentMetadata(document);

        return document;
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
        string metadataVersion = null)
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
        EvaluateFieldRules(caseData, rules, fieldMap, result);
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

        if (metadata == null)
        {
            NormalizeRuleDocumentMetadata(document);
            return;
        }

        var defaults = BuildDefaultRuleDocument(metadataVersion ?? document.metadata_version, metadata);
        MergeMissingRules(document.field_rules, defaults.field_rules, r => r.id);
        MergeMissingRules(document.connected_field_rules, defaults.connected_field_rules, r => r.id);
        MergeMissingRules(document.form_status_rules, defaults.form_status_rules, r => r.id);
        NormalizeRuleDocumentMetadata(document);
    }

    private static void MergeMissingRules<T>(List<T> target, IEnumerable<T> defaults, Func<T, string> idSelector)
    {
        var existing = new HashSet<string>(target.Select(idSelector).Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
        foreach (var item in defaults)
        {
            var id = idSelector(item);
            if (!string.IsNullOrWhiteSpace(id) && existing.Add(id))
            {
                target.Add(item);
            }
        }
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

    private static CaseValidationFieldRule CreateFieldRule(CaseValidationFlattenedField field)
    {
        var rule = new CaseValidationFieldRule
        {
            id = $"field:{field.field_path}",
            form_path = field.form_path,
            form_prompt = field.form_prompt,
            field_path = field.field_path,
            metadata_path = field.metadata_path,
            prompt = field.prompt,
            subject = field.subject,
            data_type = field.data_type,
            field_type = field.type,
            editable = field.can_quick_edit,
            source = "metadata",
            rationale = "Generated from field metadata constraints and lookup values.",
            message = $"{field.prompt} is outside the expected range or accepted values."
        };

        var hasRule = false;
        if (double.TryParse(field.min_value, NumberStyles.Any, CultureInfo.InvariantCulture, out var min))
        {
            rule.min_value = min;
            hasRule = true;
        }

        if (double.TryParse(field.max_value, NumberStyles.Any, CultureInfo.InvariantCulture, out var max))
        {
            rule.max_value = max;
            hasRule = true;
        }

        if (int.TryParse(field.max_length, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxLength))
        {
            rule.max_length = maxLength;
            hasRule = true;
        }

        if (!string.IsNullOrWhiteSpace(field.regex_pattern))
        {
            rule.regex_pattern = field.regex_pattern;
            hasRule = true;
        }

        if (field.values.Count > 0)
        {
            rule.allowed_values = field.values.Select(v => v.value).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            hasRule = rule.allowed_values.Count > 0 || hasRule;
        }

        var seeded = GetSeededNumericRange(field);
        if (seeded != null)
        {
            rule.min_value ??= seeded.Value.Min;
            rule.max_value ??= seeded.Value.Max;
            rule.message = seeded.Value.Message;
            rule.validation_level = seeded.Value.Level;
            rule.source = seeded.Value.Source;
            rule.rationale = seeded.Value.Rationale;
            rule.unit = seeded.Value.Unit;
            rule.review_status = seeded.Value.ReviewStatus;
            hasRule = true;
        }

        if (!hasRule)
        {
            return null;
        }

        rule.rule_type = rule.allowed_values.Count > 0 ? "range-list" : "range";
        return rule;
    }

    private static (double Min, double Max, string Message, string Level, string Source, string Rationale, string Unit, string ReviewStatus)? GetSeededNumericRange(CaseValidationFlattenedField field)
    {
        if (!string.Equals(field.type, "number", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var subject = NormalizeSubject($"{field.field_path} {field.prompt} {field.subject}");
        if (subject.Contains("temperature"))
        {
            return (80, 115, $"{field.prompt} is expected to be a plausible human temperature in Fahrenheit.", "intrinsic-logic", "logical-seed", "Human body temperature values outside 80-115 F are extremely unlikely and usually indicate abstraction or unit errors.", "F", "review-pending");
        }

        if (subject.Contains("oxygen saturation"))
        {
            return (0, 100, $"{field.prompt} must be between 0 and 100 percent.", "intrinsic-logic", "logical-seed", "Oxygen saturation is a percentage and cannot be below 0 or above 100.", "%", "review-pending");
        }

        if (subject.Contains("systolic bp") || subject.Contains("systolic blood pressure") || subject.Contains("systolic"))
        {
            return (40, 300, $"{field.prompt} is expected to be a plausible systolic blood pressure.", "intrinsic-logic", "logical-seed", "Systolic blood pressure outside 40-300 mmHg is extremely unlikely for a clinical vital sign.", "mmHg", "review-pending");
        }

        if (subject.Contains("diastolic bp") || subject.Contains("diastolic blood pressure") || subject.Contains("diastolic"))
        {
            return (20, 200, $"{field.prompt} is expected to be a plausible diastolic blood pressure.", "intrinsic-logic", "logical-seed", "Diastolic blood pressure outside 20-200 mmHg is extremely unlikely for a clinical vital sign.", "mmHg", "review-pending");
        }

        if (subject.Contains("heart rate") || subject.Contains("pulse"))
        {
            return (20, 250, $"{field.prompt} is expected to be a plausible heart rate.", "intrinsic-logic", "logical-seed", "Adult clinical heart rates outside 20-250 beats per minute are extremely unlikely.", "beats per minute", "review-pending");
        }

        if (subject.Contains("respiration") || subject.Contains("respiratory rate"))
        {
            return (4, 80, $"{field.prompt} is expected to be a plausible respiratory rate.", "intrinsic-logic", "logical-seed", "Respiratory rates outside 4-80 breaths per minute are extremely unlikely for a clinical vital sign.", "breaths per minute", "review-pending");
        }

        if (subject.Contains("blood sugar") || subject.Contains("glucose"))
        {
            return (10, 1000, $"{field.prompt} is expected to be a plausible blood glucose value.", "intrinsic-logic", "logical-seed", "Blood glucose values outside 10-1000 are extremely unlikely and should be reviewed for unit or abstraction errors.", "mg/dL", "review-pending");
        }

        if (subject.Contains("apgar"))
        {
            return (0, 10, $"{field.prompt} must be between 0 and 10.", "intrinsic-logic", "logical-seed", "Apgar scores are scored from 0 to 10.", "score", "review-pending");
        }

        if (subject.Contains("bmi"))
        {
            return (10, 80, $"{field.prompt} is expected to be a plausible BMI.", "intrinsic-logic", "logical-seed", "BMI outside 10-80 is extremely unlikely and should be reviewed.", "kg/m2", "review-pending");
        }

        if (subject.Contains("height feet") || subject.EndsWith("feet", StringComparison.Ordinal))
        {
            return (3, 8, $"{field.prompt} is expected to be between 3 and 8 feet.", "intrinsic-logic", "logical-seed", "Adult height feet values outside 3-8 usually indicate abstraction errors.", "feet", "review-pending");
        }

        if (subject.Contains("height inches") || subject.EndsWith("inches", StringComparison.Ordinal))
        {
            return (0, 11, $"{field.prompt} is expected to be between 0 and 11 inches.", "intrinsic-logic", "logical-seed", "Height inch remainders should be 0-11.", "inches", "review-pending");
        }

        if ((subject.Contains("weight") || subject.Contains("pre pregnancy weight")) &&
            !subject.Contains("birth weight") &&
            !subject.Contains("fetal weight") &&
            !subject.Contains("weight gain") &&
            !subject.Contains("unit of measurement") &&
            !subject.Contains(" uom"))
        {
            return (50, 700, $"{field.prompt} is expected to be a plausible adult weight in pounds.", "intrinsic-logic", "logical-seed", "Adult weights outside 50-700 pounds are extremely unlikely and should be reviewed.", "pounds", "review-pending");
        }

        if (subject.Contains("gestational age weeks") || subject.Contains("gestational weeks") || subject.EndsWith("gestational age", StringComparison.Ordinal))
        {
            return (0, 45, $"{field.prompt} is expected to be between 0 and 45 weeks.", "intrinsic-logic", "logical-seed", "Gestational age in weeks outside 0-45 is unlikely for MMRIA abstraction.", "weeks", "review-pending");
        }

        if (subject.Contains("gestational age days") || subject.Contains("gestational days"))
        {
            return (0, 6, $"{field.prompt} is expected to be between 0 and 6 days.", "connected-logic", "logical-seed", "Day remainders paired with gestational weeks should be 0-6.", "days", "review-pending");
        }

        if ((subject.Contains("age at death") && !subject.Contains("family medical history")) ||
            subject.Contains("maternal age") ||
            subject.Contains("mother age"))
        {
            return (10, 60, $"{field.prompt} is expected to be between 10 and 60 years.", "intrinsic-logic", "logical-seed", "MMRIA maternal age values outside 10-60 are unusual enough to review.", "years", "review-pending");
        }

        if (subject.Contains("gravida") || subject.Contains("parity"))
        {
            return (0, 25, $"{field.prompt} is expected to be between 0 and 25.", "intrinsic-logic", "logical-seed", "Pregnancy history counts outside 0-25 are very unusual and should be reviewed.", "count", "review-pending");
        }

        if (subject.Contains("birth weight") && subject.Contains("ounces"))
        {
            return (0, 15, $"{field.prompt} is expected to be between 0 and 15 ounces.", "intrinsic-logic", "logical-seed", "Birth weight ounce remainders should be 0-15.", "ounces", "review-pending");
        }

        if (subject.Contains("birth weight") || subject.Contains("fetal weight"))
        {
            return (0, 7000, $"{field.prompt} is expected to be between 0 and 7000 grams.", "intrinsic-logic", "logical-seed", "Birth or fetal weight gram values outside 0-7000 should be reviewed for units or abstraction errors.", "grams", "review-pending");
        }

        return null;
    }

    private static IEnumerable<CaseValidationFormStatusRule> CreateFormStatusRules(List<CaseValidationFlattenedField> fields)
    {
        var forms = fields
            .Where(f => string.Equals(f.field_path, f.form_path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var statusFields = fields
            .Where(f => NormalizeSubject($"{f.subject} {f.prompt}").Contains("form status"))
            .Where(f => f.values.Any(v => IsFormStatusDisplay(v.display)))
            .ToList();

        var usedStatusFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var form in forms)
        {
            var status = statusFields
                .Where(s => !usedStatusFields.Contains(s.field_path))
                .OrderByDescending(s => SubjectSimilarity(form.form_prompt, s.prompt))
                .ThenByDescending(s => SubjectSimilarity(form.form_prompt, s.subject))
                .FirstOrDefault(s => SubjectSimilarity(form.form_prompt, s.prompt) >= 0.65 || SubjectSimilarity(form.form_prompt, s.subject) >= 0.65);

            if (status == null)
            {
                continue;
            }

            usedStatusFields.Add(status.field_path);
            yield return new CaseValidationFormStatusRule
            {
                id = $"form-status:{form.form_path}",
                form_path = form.form_path,
                form_prompt = form.form_prompt,
                status_field_path = status.field_path,
                status_field_prompt = status.prompt,
                message = $"{form.form_prompt} status does not match the data currently present in the form."
            };
        }
    }

    private static IEnumerable<CaseValidationConnectedFieldRule> CreateConnectedFieldRules(List<CaseValidationFlattenedField> fields)
    {
        var byPath = fields.ToDictionary(f => f.field_path, StringComparer.OrdinalIgnoreCase);
        var result = new List<CaseValidationConnectedFieldRule>();

        if (byPath.TryGetValue("death_certificate/demographics/date_of_birth", out var dob) &&
            byPath.TryGetValue("home_record/date_of_death", out var dod))
        {
            result.Add(new CaseValidationConnectedFieldRule
            {
                id = "connected:death-certificate-birth-date-before-death-date",
                rule_type = "date_less_than_or_equal",
                form_path = dob.form_path,
                form_prompt = dob.form_prompt,
                field_path = dob.field_path,
                related_field_path = dod.field_path,
                metadata_path = dob.metadata_path,
                prompt = dob.prompt,
                related_prompt = dod.prompt,
                subject = "date of birth and date of death",
                comparison = "less_than_or_equal",
                validation_level = "timeline-logic",
                source = "logical-seed",
                rationale = "A birth date cannot occur after the date of death.",
                review_status = "review-pending",
                message = "Date of birth should not be later than date of death."
            });
        }

        AddDateBeforeDeathRule(result, byPath, "death_certificate/injury_associated_information/date_of_injury", "Date of injury should not be later than date of death.");
        AddDateBeforeDeathRule(result, byPath, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery", "Date of delivery should not be later than date of death.");
        AddDateBeforeDeathRule(result, byPath, "birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "Date of delivery should not be later than date of death.");

        AddConnectedRule(result, byPath, "connected:er-arrival-before-admission",
            "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival",
            "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission",
            "datetime_less_than_or_equal",
            "ER/hospital arrival and admission date/time",
            "Hospital/ER arrival should occur on or before hospital admission within the same visit.",
            "Date of arrival should be on or before date of hospital admission.",
            requireSameContainer: true,
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:er-admission-before-discharge",
            "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission",
            "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge",
            "datetime_less_than_or_equal",
            "ER/hospital admission and discharge date/time",
            "Hospital admission should occur on or before hospital discharge within the same visit.",
            "Date of hospital admission should be on or before date of hospital discharge.",
            requireSameContainer: true,
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:autopsy-after-death",
            "autopsy_report/reporter_characteristics/date_of_autopsy",
            "home_record/date_of_death",
            "date_greater_than_or_equal",
            "autopsy date and date of death",
            "An autopsy date should not occur before the date of death.",
            "Date of autopsy should be on or after date of death.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:informant-interview-after-death",
            "informant_interviews/date_of_interview",
            "home_record/date_of_death",
            "date_greater_than_or_equal",
            "informant interview date and date of death",
            "Informant interviews are expected to occur after the death date.",
            "Date of interview should be on or after date of death.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:committee-review-after-death",
            "committee_review/date_of_review",
            "home_record/date_of_death",
            "date_greater_than_or_equal",
            "committee review date and date of death",
            "Committee review is expected to occur after the death date.",
            "Review date should be on or after date of death.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:case-locked-after-death",
            "home_record/case_status/case_locked_date",
            "home_record/date_of_death",
            "date_greater_than_or_equal",
            "case locked date and date of death",
            "Case lock date is expected to occur after the death date.",
            "Case locked date should be on or after date of death.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:abstraction-begin-before-complete",
            "home_record/case_status/abstraction_begin_date",
            "home_record/case_status/abstraction_complete_date",
            "date_less_than_or_equal",
            "abstraction begin and complete dates",
            "Abstraction should begin on or before it is marked complete.",
            "Abstraction begin date should be on or before abstraction complete date.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:parent-first-prenatal-before-last-prenatal",
            "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit",
            "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit",
            "date_less_than_or_equal",
            "parent-section first and last prenatal care visit dates",
            "The first prenatal care visit should not occur after the last prenatal care visit.",
            "Date of first prenatal care visit should be on or before date of last prenatal care visit.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:prenatal-first-prenatal-before-last-prenatal",
            "prenatal/current_pregnancy/date_of_1st_prenatal_visit",
            "prenatal/current_pregnancy/date_of_last_prenatal_visit",
            "date_less_than_or_equal",
            "prenatal first and last prenatal visit dates",
            "The first prenatal visit should not occur after the last prenatal visit.",
            "Date of first prenatal visit should be on or before date of last prenatal visit.",
            validationLevel: "timeline");

        AddConnectedRule(result, byPath, "connected:infant-delivery-date-matches-parent-delivery-date",
            "birth_certificate_infant_fetal_section/record_identification/date_of_delivery",
            "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery",
            "date_equal",
            "parent and infant/fetal delivery dates",
            "The parent-section and infant/fetal-section delivery dates should match when both are available.",
            "Infant/fetal delivery date should match parent-section delivery date.",
            validationLevel: "impossibility");

        foreach (var field in fields.Where(IsClinicalEventDateField))
        {
            if (!byPath.ContainsKey("home_record/date_of_death"))
            {
                continue;
            }

            result.Add(new CaseValidationConnectedFieldRule
            {
                id = $"connected:event-before-death:{field.field_path}",
                rule_type = "date_less_than_or_equal",
                form_path = field.form_path,
                form_prompt = field.form_prompt,
                field_path = field.field_path,
                related_field_path = "home_record/date_of_death",
                metadata_path = field.metadata_path,
                prompt = field.prompt,
                related_prompt = "Date of Death",
                subject = "clinical event date and date of death",
                comparison = "less_than_or_equal",
                validation_level = "timeline-logic",
                source = "logical-seed",
                rationale = "Clinical visit, transport, and vital sign event dates should not occur after the death date.",
                review_status = "review-pending",
                message = $"{field.prompt} should not be later than date of death."
            });
        }

        foreach (var systolic in fields.Where(IsSystolicBloodPressureField))
        {
            var diastolicPath = ResolveSiblingPath(systolic.field_path, "bp_systolic", "bp_diastolic")
                                ?? ResolveSiblingPath(systolic.field_path, "systolic_bp", "diastolic_bp")
                                ?? ResolveSiblingPath(systolic.field_path, "systolic_bp", "diastolic")
                                ?? ResolveSiblingPath(systolic.field_path, "systolic", "diastolic");
            if (diastolicPath == null || !byPath.TryGetValue(diastolicPath, out var diastolic))
            {
                continue;
            }

            result.Add(new CaseValidationConnectedFieldRule
            {
                id = $"connected:blood-pressure:{systolic.field_path}",
                rule_type = "numeric_greater_than_or_equal",
                form_path = systolic.form_path,
                form_prompt = systolic.form_prompt,
                field_path = systolic.field_path,
                related_field_path = diastolic.field_path,
                metadata_path = systolic.metadata_path,
                prompt = systolic.prompt,
                related_prompt = diastolic.prompt,
                subject = "systolic and diastolic blood pressure",
                comparison = "greater_than_or_equal",
                validation_level = "connected-logic",
                source = "logical-seed",
                rationale = "Systolic blood pressure should be greater than or equal to diastolic blood pressure.",
                review_status = "review-pending",
                require_same_container = true,
                message = "Systolic blood pressure should be greater than or equal to diastolic blood pressure."
            });
        }

        result.AddRange(CreateOtherSpecifyRules(fields, byPath));

        foreach (var weekField in fields.Where(IsGestationalWeekField))
        {
            var daysPath = weekField.field_path.Substring(0, weekField.field_path.Length - "weeks".Length) + "days";
            if (!byPath.TryGetValue(daysPath, out var daysField))
            {
                continue;
            }

            result.Add(new CaseValidationConnectedFieldRule
            {
                id = $"connected:gestational-days:{daysField.field_path}",
                rule_type = "numeric_max",
                form_path = daysField.form_path,
                form_prompt = daysField.form_prompt,
                field_path = daysField.field_path,
                related_field_path = weekField.field_path,
                metadata_path = daysField.metadata_path,
                prompt = daysField.prompt,
                related_prompt = weekField.prompt,
                subject = "gestational age days",
                comparison = "less_than_or_equal",
                max_difference = 6,
                validation_level = "connected-logic",
                source = "logical-seed",
                rationale = "Gestational age day remainders paired with weeks should be 0-6.",
                review_status = "review-pending",
                message = "Gestational age days is expected to be between 0 and 6 when weeks are also captured."
            });
        }

        return result
            .GroupBy(r => r.id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First());
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
        string validationLevel = "impossibility")
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
            rule_type = ruleType,
            form_path = field?.form_path ?? FirstPathSegment(fieldPath),
            form_prompt = field?.form_prompt ?? PromptFromPath(FirstPathSegment(fieldPath)),
            field_path = fieldPath,
            related_field_path = relatedFieldPath,
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
        CaseValidationEvaluationResult result)
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

                AddCheckAndFinding(result, new CaseValidationFinding
                {
                    id = $"{rule.id}:{result.checks.Count}",
                    rule_id = rule.id,
                    category = "range",
                    severity = isFinding ? rule.severity : "ok",
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
                var finding = TryFindConnectedRuleIssue(rule, value, related, out var expected);

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
    }

    private static IEnumerable<ConnectedRuleTokenPair> GetConnectedRuleTokenPairs(JObject caseData, CaseValidationConnectedFieldRule rule)
    {
        if (rule.require_same_container)
        {
            var sharedPath = GetSharedContainerPath(rule.field_path, rule.related_field_path);
            if (!string.IsNullOrWhiteSpace(sharedPath))
            {
                var containers = GetTokensByPath(caseData, sharedPath).ToList();
                if (containers.Count == 0)
                {
                    return new[] { new ConnectedRuleTokenPair() };
                }

                var valueSuffix = RemovePathPrefix(rule.field_path, sharedPath);
                var relatedSuffix = RemovePathPrefix(rule.related_field_path, sharedPath);
                return containers.SelectMany(container =>
                    BuildIndexedTokenPairs(
                        GetTokensByRelativePath(container, valueSuffix),
                        GetTokensByRelativePath(container, relatedSuffix)));
            }
        }

        return BuildIndexedTokenPairs(
            GetTokensByPath(caseData, rule.field_path),
            GetTokensByPath(caseData, rule.related_field_path));
    }

    private static IEnumerable<ConnectedRuleTokenPair> BuildIndexedTokenPairs(IEnumerable<JToken> values, IEnumerable<JToken> relatedValues)
    {
        var valueList = values?.ToList() ?? new List<JToken>();
        var relatedList = relatedValues?.ToList() ?? new List<JToken>();

        if (valueList.Count == 0)
        {
            valueList.Add(null);
        }

        if (relatedList.Count == 0)
        {
            relatedList.Add(null);
        }

        var count = Math.Max(valueList.Count, relatedList.Count);
        for (var i = 0; i < count; i++)
        {
            yield return new ConnectedRuleTokenPair
            {
                Value = valueList.Count == 1 ? valueList[0] : valueList[Math.Min(i, valueList.Count - 1)],
                Related = relatedList.Count == 1 ? relatedList[0] : relatedList[Math.Min(i, relatedList.Count - 1)]
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
        var left = SplitPath(leftPath);
        var right = SplitPath(rightPath);
        var count = 0;

        while (count < left.Length &&
               count < right.Length &&
               string.Equals(left[count], right[count], StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        return count == 0 ? string.Empty : string.Join("/", left.Take(count));
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

        return rule.comparison;
    }

    private static bool TryFindConnectedRuleIssue(CaseValidationConnectedFieldRule rule, JToken value, JToken related, out string expected)
    {
        expected = BuildConnectedRuleExpectedText(rule);
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

        var trimmed = text.Trim();
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
