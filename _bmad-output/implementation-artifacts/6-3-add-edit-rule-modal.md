# Story 6.3: Per-Rule Save via Inline Detail Panel

Status: done

## Story

As a form designer,
I want to save individual validation rules directly from the inline detail panel,
So that I can build and maintain the rule set without a publish step or separate modal.

## Context and Scope

This story replaces the originally planned Add/Edit modal with a simpler inline approach:

1. **Remove "Publish Rules" button** — rules are managed in dev and pushed to production via deployment; a bulk-publish UI step is not needed.
2. **Add "Save Rule" button** to the inline detail panel — clicking it persists the currently selected rule (create if new, update if existing) via the existing `PUT /api/case-validation/rules/{metadata_version}` endpoint.
3. **No new API endpoint or modal** — the existing panel and PUT endpoint are sufficient.
4. **1:1 field → rule** for `field_rules` — already enforced by the existing row-building logic; every metadata field has a row with default rule values until explicitly saved and enabled.

The left-panel field list is already database-driven (loaded from the live DB metadata document). The "Enabled" count reflects rules that have been explicitly enabled in the rules document. `_rev` is kept current in memory after each save so sequential saves do not produce CouchDB conflict errors.

### Dependencies

- **Story 6.1** must be complete.
- **Story 6.2** must be complete — the admin page and all API routes must exist.

---

## Server-Side Changes

### New API Endpoint: `GET /api/case-validation/metadata/fields`

Add to `case_validationController.cs`:

```csharp
[Authorize(Roles = "form_designer")]
[HttpGet("metadata/fields")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public async Task<IActionResult> GetMetadataFields()
```

- Fetches current metadata from DB via `MetadataVersionManager`.
- Calls `_caseValidationManager.FlattenMetadata(metadata)` to get the flattened field list.
- Returns the result as JSON — an array of `CaseValidationFlattenedField` objects, each with at minimum: `field_path`, `form_path`, `form_prompt`, `prompt`, `type` (field_type), `metadata_path`, `is_scalar`, `is_multi`.
- Returns 500 with `{ ok: false, message }` on error.

**Response shape:**
```json
[
  {
    "field_path": "er_visit_and_hospital_medical_records/vital_signs/temperature",
    "form_path": "er_visit_and_hospital_medical_records",
    "form_prompt": "ER Visits and Hospitalizations",
    "prompt": "Temperature",
    "type": "number",
    "metadata_path": "g_metadata.children[19].children[10].children[1]",
    "is_scalar": true,
    "is_multi": true
  },
  ...
]
```

---

## Client-Side Changes (`case-validation.js` and/or inline in `Index.cshtml`)

### Add Rule Button

- Add an **"Add Rule"** button to the admin UI header/actions area (alongside the existing filter and action controls).
- Clicking it opens the Add/Edit Rule modal in "Add" mode with all fields cleared.

### Edit Button per Rule Row

- Add an **"Edit"** button (or pencil icon button) to each rule row in the rule list (field_rules, connected_field_rules, form_status_rules).
- Clicking it opens the Add/Edit Rule modal in "Edit" mode, pre-populated with the clicked rule's current values.

### Add/Edit Rule Modal

The modal follows the existing site modal pattern (purple header, white body).

**Header:** "Add Validation Rule" (Add mode) or "Edit Validation Rule" (Edit mode).

**Rule Type Selection (Step 1):**
A tab or radio selector at the top lets the user choose the rule type before selecting the form field:
- **Field Rule** — validates a single field's value (range, allowed values, regex, max length)
- **Connected Field Rule** — validates a relationship between two fields
- **Form Status Rule** — validates that form status matches data completeness

Rule type selection is shown first and controls which fields appear below.

**Form / Field Selection (Step 2 — for Field Rule and Connected Field Rule):**

*Form dropdown:*
- Labeled "Form"
- Options are the distinct `form_prompt` values from the metadata fields API response, in metadata order (de-duplicated).
- First option is a placeholder: `(Select form)`.
- On change: update the Field dropdown to show only fields belonging to the selected `form_path`.

*Field dropdown (primary):*
- Labeled "Field"
- Initially empty or showing `(Select field)`.
- Filtered to the selected form — each option's label is the field's `prompt`, value is the field's `field_path`.
- Only scalar fields (`is_scalar: true`) are shown.
- On selection: auto-populate read-only display fields: Form Path, Field Path, Field Type, Metadata Path.

*Related Field dropdown (Connected Field Rule only):*
- Labeled "Related Field"
- Same population logic as Field dropdown (all forms, all scalar fields).
- Appears only when rule type is "Connected Field Rule".

*Form selection (Form Status Rule):*
- Only the Form dropdown appears (no Field dropdown) — the rule applies to the entire form.

**Editable Rule Fields (Step 3):**

All fields appear below the dropdowns. Fields shown depend on rule type.

**Fields common to all rule types:**

| Field | Input type | Default |
|---|---|---|
| Enabled | Toggle / checkbox | checked (true) |
| Severity | Select: `warning`, `hard` | `warning` |
| Validation Level | Select: `metadata`, `impossibility`, `plausibility`, `timeline`, `conditional`, `form-completeness` | `plausibility` |
| Confidence | Select: `high`, `medium`, `low` | `medium` |
| Review Status | Select: `generated`, `review-pending`, `reviewed`, `rejected` | `review-pending` |
| Source | Text input | `admin` |
| Message | Textarea | empty |
| Rationale | Textarea | empty |
| Explanation | Textarea | empty |
| Admin Notes | Textarea | empty |

**Fields for Field Rule only:**

| Field | Input type | Notes |
|---|---|---|
| Rule Type | Select: `range`, `allowed-values`, `regex`, `max-length` | `range` |
| Min Value | Number input | Shown when rule_type = `range` |
| Max Value | Number input | Shown when rule_type = `range` |
| Unit | Text input | e.g. `°F`, `bpm` |
| Max Length | Number input | Shown when rule_type = `max-length` |
| Regex Pattern | Text input | Shown when rule_type = `regex` |
| Allowed Values | Tag/comma-separated input | Shown when rule_type = `allowed-values` |

**Fields for Connected Field Rule only:**

| Field | Input type | Notes |
|---|---|---|
| Comparison | Select: `date-before`, `date-after`, `date-equal`, `value-equal`, `value-not-equal`, `max-difference`, `required-when` | |
| Max Difference | Number input | Shown when comparison = `max-difference` |
| Require Same Container | Checkbox | |
| Trigger Values | Tag/comma-separated input | Values on the primary field that activate the rule |

**Fields for Form Status Rule only:**

| Field | Input type | Notes |
|---|---|---|
| Expected Status | Select: `complete`, `not-started`, `in-progress` | |

**Save / Cancel:**
- **Save** button: validates required fields (form/field selected, at least one of message or rationale filled). On success, calls `PUT /api/case-validation/rules/{metadata_version}` with the full updated document and re-renders the rule list. Shows inline success/error status.
- **Cancel** button: dismisses modal without saving.

**ID generation for new rules:**
When creating a new rule, generate the `id` field as:
```
field:{field_path}   (for field_rules)
connected:{field_path}:{related_field_path}   (for connected_field_rules)
form-status:{form_path}   (for form_status_rules)
```
If a rule with the generated id already exists, append a counter suffix: `field:{field_path}:2`, etc.

---

## Acceptance Criteria

**AC #1: "Publish Rules" button removed**
The "Publish Rules" button does not appear in the page header.

**AC #2: "Save Rule" button appears in detail panel**
When a rule row is selected, the detail panel shows a "Save Rule" button at the bottom.

**AC #3: Save persists new rule**
Given a rule row in default/generated state (not yet explicitly saved),
When the user configures values and clicks "Save Rule",
Then the rule is written to the rules document via `PUT /api/case-validation/rules/{metadata_version}` and the Enabled summary count updates accordingly.

**AC #4: Save updates existing rule**
Given a rule that was previously saved,
When the user changes a field (e.g. Severity) and clicks "Save Rule",
Then the rules document is updated with the new value and "Saved." appears in the `#cv_detail_save_status` span.

**AC #5: `_rev` stays current**
After each successful Save Rule, `caseValidationMetadataRules._rev` is updated from the API response so subsequent saves do not produce CouchDB conflict errors.

**AC #6: Save error shown inline**
When the PUT call fails (network error or CouchDB conflict),
Then an error message appears in `#cv_detail_save_status` and the page does not crash.

**AC #7: Build succeeds**
`dotnet build mmria-server.csproj` completes with 0 errors introduced by this change.

---

## Tasks / Subtasks

### Phase 1 — Remove "Publish Rules" button

- [x] Remove the `<button>` for "Publish Rules" from `.case-validation-admin-actions` in `Index.cshtml`.
- [x] Remove the `caseValidationMetadataSave()` async function from the `<script>` block.

### Phase 2 — Add "Save Rule" to detail panel

- [x] Append Save Rule button + `#cv_detail_save_status` span to the `detail.innerHTML` string at the end of `caseValidationMetadataRenderDetail()`.
- [x] Add `caseValidationMetadataSaveRule()` function.

### Phase 3 — Build and smoke test

- [ ] Run `dotnet build mmria-server.csproj` — 0 errors.
- [ ] Navigate to `/case_validation_metadata` as `form_designer`.
- [ ] Select any rule — "Save Rule" button appears at bottom of detail panel; "Publish Rules" button is gone.
- [ ] Toggle Enabled on a rule, click Save Rule — "Saved." appears; Enabled summary count updates.
- [ ] Select another rule, save it — no CouchDB conflict (rev was updated correctly).
