# Validation Engine V4.1 — AI Context

- **Status:** Foundation sprint (V4.1)
- **Scope:** Vitals field validation only; 6 fields; hard validation on active input, warnings on historical data
- **Last updated:** 2026-06-16 (OI-PRD-4 resolved)
- **Related docs:** [V4.1 PRD](../planning-artifacts/prds/prd-mmria-2026-06-12/prd.md), [Story Index](../implementation-artifacts/story-index.md), [POC Branch](https://github.com/NCCDPHP/nccdphp-drh-mmria/tree/v4.1-case-data-validation-mode)

---

## Architecture Overview

The V4.1 validation engine replaces the Story 2.1/2.2 config-document approach with a **POC-derived, context-aware evaluation system** that supports hard validation for new data entry and soft warnings for historical data without retroactive clearing.

### Core Model: One Rule, Two Contexts

Each vitals rule is stored once with `severity: "hard"`. The evaluation context determines effective enforcement:

| Context | When | Severity Effective | Behavior | API Call |
|---|---|---|---|---|
| **active-input** | User editing a field, blur event fires | `hard` (unchanged) | Field cleared + modal shown | `EvaluateCase(caseData, rules, "active-input")` |
| **historical** | Case loaded, scanning stored values | `hard` → `warning` (downgraded) | Surfaced in panel, never clears | `EvaluateCase(caseData, rules, "historical")` |

**Implementation:** `CaseValidationManager.EvaluateCase()` takes a `contextFlag` parameter. If `contextFlag == "historical"`, the engine walks `rule.severity` and downgrades: `hard → warning`. The caller never sees `hard` in historical context.

---

## Seeded Vitals Ranges (V4.1)

These six fields are seeded at server startup via `CaseValidationManager.BuildDefaultRuleDocument()` calling `GetSeededNumericRange()`. All are sourced from the POC and are marked `review_status: "reviewed"` (not `review_status: "review-pending"`).

| Field | field_path pattern | min_value | max_value | unit | rationale | severity |
|---|---|---|---|---|---|---|
| **Temperature** | `*/temperature` | 80 | 115 | °F | Human plausibility (clinical vital signs rarely <80 or >115°F). | hard |
| **Heart Rate** | `*/heart_rate` | 20 | 250 | bpm | Clinical plausibility (pulse unlikely <20 or >250 bpm). | hard |
| **Respiration Rate** | `*/respiration_rate` | 4 | 80 | breaths/min | Clinical plausibility (respiratory rate 4-80 breaths/min). | hard |
| **Systolic BP** | `*/systolic*` or `systolic_bp` | 40 | 300 | mmHg | Clinical plausibility (systolic 40-300 mmHg). | hard |
| **Diastolic BP** | `*/diastolic*` or `diastolic_bp` | 20 | 200 | mmHg | Clinical plausibility (diastolic 20-200 mmHg). | hard |
| **Oxygen Saturation** | `*/oxygen_saturation` | 0 | 100 | % | Percentage definition (0-100% by definition). | hard |

**Stored Location:** `case-validation-rules-{metadata_version}` CouchDB document in the metadata database (NOT the general config document). One document per metadata version; separate from application configuration.

**Startup Pattern:**
1. `CaseManager` or `CaseController` startup hook calls `CaseValidationManager.GetOrCreateRuleDocumentAsync(metadataVersion, metadata, dbConfig, userName)`
2. If document exists: load it, ensure shape, merge any missing rules from defaults
3. If document does not exist: call `BuildDefaultRuleDocument()` which auto-seeds the six vitals + form_status rules (form_status rules are generated but disabled in V4.1)
4. Seed document cached in memory for the lifetime of the app instance

---

## Client-Side Integration Points

### Three `mmria_vitals_is_out_of_range()` Functions — Must Stay Synchronized

Each of these files contains an identical function that must be updated identically to read from the new data structure:

**Before (reads from old global):**
```javascript
function mmria_vitals_is_out_of_range(fieldName, value) {
    const config = window.mmria_vital_sign_range[fieldName];
    if (!config) return false;
    return value < config.min || value > config.max;
}
```

**After (reads from new rules structure):**
```javascript
function mmria_vitals_is_out_of_range(fieldName, value, evaluationContext = "active-input") {
    // Locate the rule by field_path (converted from fieldName)
    const rule = (window.mmria_validation_rules?.rules || [])
        .find(r => r.field_path === fieldName || r.field_path.endsWith('/' + fieldName));
    
    if (!rule) return false;
    
    // Apply context: historical downgrades hard to warning
    let effectiveSeverity = rule.severity;
    if (evaluationContext === "historical" && rule.severity === "hard") {
        effectiveSeverity = "warning";
    }
    
    // Check range (only hard and warning trigger a violation)
    if (effectiveSeverity !== "hard" && effectiveSeverity !== "warning") return false;
    
    if (rule.min_value !== undefined && value < rule.min_value) return true;
    if (rule.max_value !== undefined && value > rule.max_value) return true;
    return false;
}
```

**Locations to update (MUST BE IDENTICAL):**
1. [source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js](source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js#L265-L272)
2. [source-code/mmria/mmria-server/wwwroot/scripts/print-version/print_version_renderer.js](source-code/mmria/mmria-server/wwwroot/scripts/print-version/print_version_renderer.js#L1084-L1091)
3. [source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js](source-code/mmria/mmria-server/wwwroot/scripts/pdf-version/index.js#L707-L714)

**Critical:** If these drift (different implementations), the print/PDF exclusion will not match the blur validation, and data will be cleared on edit but not on print. Test all three paths during Story 4.0 implementation.

---

## Server-Side Integration

### API Endpoint (New)

Return the seeded vitals rules to the client at case page load. Pattern:

```csharp
// Pseudo-code for endpoint design
[HttpGet("/api/case-validation/rules/current")]
public async Task<IActionResult> GetCurrentRules()
{
    var metadataVersion = _metadata.version; // or from request
    var rules = await _caseValidationManager.GetOrCreateRuleDocumentAsync(
        metadataVersion, 
        _metadata, 
        _dbConfig, 
        User);
    
    return Ok(new {
        rules = rules.field_rules,  // Only field_rules in V4.1
        metadata_version = rules.metadata_version,
        seeded_version = rules.seeded_version
    });
}
```

### View Integration

In [Views/Case/Index.cshtml](source-code/mmria/mmria-server/Views/Case/Index.cshtml):

**Before:**
```html
<script>
    var vital_sign_range_config = @Html.Raw(TempData["vital_sign_range_config"] ?? "null");
    window.mmria_vital_sign_range = vital_sign_range_config;
</script>
```

**After:**
```html
<script>
    // Populate from API or controller action
    window.mmria_validation_rules = @Html.Raw(ViewData["validation_rules"] ?? "null");
</script>
```

Pass `validation_rules` from `CaseController` action (either via `ViewData` or as inline JSON from an API call).

---

## Callsite Cascade

Once `window.mmria_vital_sign_range` is replaced with `window.mmria_validation_rules` AND the three `mmria_vitals_is_out_of_range()` functions are updated, these callsites automatically benefit (no additional changes needed):

- Graph exclusion in `chart.js` (lines 362-404, 888, 979, 1025)
- Table exclusion in `chart.js` (line 1296)
- Print exclusion in `print_version_renderer.js` (lines 769, 887, 932)
- PDF exclusion in `pdf-version/index.js` (lines 2272, 2746, 2769, 2786)

All of these call `mmria_vitals_is_out_of_range()` and will automatically read from the new structure once that function is updated.

---

## Deletion Checklist

**Story 4.0 removes:**
1. **Entire file:** [source-code/mmria/mmria-server/util/VitalSignRangeHelper.cs](source-code/mmria/mmria-server/util/VitalSignRangeHelper.cs) — no longer used; vitals ranges now come from `CaseValidationManager` seeded rules
2. **Lines in CaseController.cs** (approx lines 67-68): Remove the two lines that load vitals range config from `VitalSignRangeHelper` and serialize to `TempData`

---

## Evaluation Context Flag — Implementation Notes

When the dev agent implements `CaseValidationManager.EvaluateCase()` with a context flag, the pattern should be:

```csharp
public CaseValidationEvaluationResult EvaluateCase(
    JObject caseData,
    app metadata,
    CaseValidationRuleDocument rules,
    string metadataVersion = null,
    string evaluationContext = "active-input")  // NEW PARAMETER
{
    // ... existing setup ...
    
    foreach (var rule in rules.field_rules)
    {
        var effectiveSeverity = rule.severity;
        if (evaluationContext == "historical" && rule.severity == "hard")
        {
            effectiveSeverity = "warning";  // Downgrade in historical context
        }
        
        // Evaluate rule with effectiveSeverity
        var violations = EvaluateFieldRule(caseData, rule, effectiveSeverity);
        result.findings.AddRange(violations);
    }
    
    return result;
}
```

**Callers:**
- **Active-input blur validation:** `EvaluateCase(caseData, rules, metadataVersion, "active-input")`
- **Case load historical scan (Story 5.1):** `EvaluateCase(caseData, rules, metadataVersion, "historical")`

---

## Test Vectors

**Story 4.0 acceptance tests should verify:**
1. ✅ Seeded rules document is created at startup if missing
2. ✅ Seeded rules are cached in memory (no per-case CouchDB fetch)
3. ✅ API endpoint returns rules; `window.mmria_validation_rules` is set on page load
4. ✅ Three `mmria_vitals_is_out_of_range()` functions return identical results when given the same input
5. ✅ Graph exclusion in `chart.js` filters correctly
6. ✅ Print exclusion in `print_version_renderer.js` filters correctly
7. ✅ PDF exclusion in `pdf-version/index.js` filters correctly
8. ✅ `VitalSignRangeHelper.cs` is deleted and `CaseController.cs` TempData lines are removed with no compilation errors

**Story 4.1 will verify:**
1. ✅ Hard violations block print/PDF/View with a modal (no proceed button)
2. ✅ Soft violations show acknowledgment modal (proceed requires explicit confirm)
3. ✅ Historical data surfaces as warnings (triggered at case load time)

**Story 5.1 will verify:**
1. ✅ Panel shows errors (red) and warnings (amber) in separate sections
2. ✅ Warning rows display stored value in `"Value [X] outside [min]–[max]"` format
3. ✅ Load-time historical scan populates the warnings section

---

## Known Constraints

- **No multi-tenant override in V4.1:** Rules are global per metadata version. Jurisdiction-specific overrides are V4.2+.
- **No form-completeness rules in V4.1:** POC includes form-status rules; they are generated but disabled (not seeded, or seeded with `enabled: false`). V4.2 scope.
- **No connected-field rules in V4.1:** POC includes timeline/connected rules; not ported. V4.2 scope.
- **Seeded version is not incremented dynamically:** If seeding logic changes, `seeded_version` in code must be bumped manually and startup re-seeding checks this field.

---

## References

- **POC branch:** `v4.1-case-data-validation-mode` (commit baseline to be recorded in Story 4.0)
- **CaseValidationManager source:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation/Manager/CaseValidationManager.cs`
- **POC seeding logic:** `GetSeededNumericRange()` method (copy vitals-only, discard other categories)
- **Callsite reference:** 29 locations in 9 files (see story-index.md or conversation context)
