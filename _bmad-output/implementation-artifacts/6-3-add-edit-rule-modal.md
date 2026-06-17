# Story 6.3: Add/Edit Rule Modal with Cascading Metadata Dropdowns

Status: draft

## Story

As a form designer,
I want to add new validation rules and edit existing ones from the Case Validation Rule Manager,
So that I can build and maintain the rule set without directly editing CouchDB documents.

## Context and Scope

This story adds Create and Edit capabilities to the admin UI delivered in Story 6.2. It introduces:

1. **A new API endpoint** that returns the flattened metadata field list from the database (not the repo), organized by form — to power the cascading form/field dropdowns.
2. **An "Add Rule" button** in the admin UI header that opens a modal dialog.
3. **A cascading dropdown** in the modal: select Form → Field list filters to that form.
4. **A rule creation/edit form** in the modal: after selecting a form and field, show all editable rule properties relevant to the selected rule type.
5. **An "Edit" button** on each existing rule row that opens the modal pre-populated with that rule's current values.
6. **Save** writes the updated document back via the existing `PUT /api/case-validation/rules/{metadata_version}` endpoint.

All three rule types are supported: `field_rules`, `connected_field_rules`, and `form_status_rules`.

### Dependencies

- **Story 6.1** must be complete — auto-generation is removed; the rule document starts empty and is built entirely through this UI.
- **Story 6.2** must be complete — the admin page, `case-validation.js`, and all API routes must exist.

### Metadata Source

The form and field dropdown data comes from the database metadata document (fetched server-side via `MetadataVersionManager`), not from hardcoded or repo-side metadata. This ensures dropdown options always reflect the current deployed metadata version.

The server uses the existing `CaseValidationManager.FlattenMetadata(app metadata)` method to build the field list. The new API endpoint calls this and serializes the result for the client.

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

**AC #1: Metadata fields endpoint returns data**
When `GET /api/case-validation/metadata/fields` is called by a `form_designer`,
Then it returns a JSON array of flattened metadata fields from the current DB metadata version, including at minimum: `field_path`, `form_path`, `form_prompt`, `prompt`, `type`, `is_scalar`.

**AC #2: Form dropdown populated from DB metadata**
When the Add Rule modal opens,
Then the Form dropdown is populated from the metadata fields API — showing the distinct `form_prompt` labels in metadata order, matching the list shown in the screenshot (Home Record, Death Certificate, Birth/Fetal Death Certificate, etc.).

**AC #3: Field dropdown filters on form selection**
When the user selects a form in the Form dropdown,
Then the Field dropdown updates to show only the scalar fields belonging to that form — not fields from other forms.

**AC #4: Field auto-populates read-only metadata**
When the user selects a field in the Field dropdown,
Then the `form_path`, `field_path`, `field_type`, and `metadata_path` display fields are auto-populated from the API response and are read-only (not editable).

**AC #5: Add mode — empty modal**
When the user clicks "Add Rule",
Then the modal opens with rule type defaulting to "Field Rule", all dropdowns at their placeholder, and all text/number fields empty.

**AC #6: Edit mode — pre-populated modal**
When the user clicks "Edit" on an existing rule row,
Then the modal opens pre-populated with that rule's current values in all fields, including the correct form and field dropdown selections.

**AC #7: Save creates a new rule**
Given the user fills in form, field, severity, message, and at least one rule-specific field,
When the user clicks Save,
Then a new rule is appended to the appropriate rule list (`field_rules`, `connected_field_rules`, or `form_status_rules`) in the rule document, the document is saved via `PUT /api/case-validation/rules/{metadata_version}`, and the rule list re-renders showing the new rule.

**AC #8: Save updates an existing rule**
Given the user edits an existing rule and changes its message and severity,
When the user clicks Save,
Then the rule document is updated with the new values (matched by `id`), saved, and the rule list re-renders showing the updated values.

**AC #9: Duplicate ID prevention**
When a new rule is added with the same `field_path` as an existing rule,
Then the generated `id` gets a counter suffix (`:2`, `:3`, etc.) so no two rules share the same `id`.

**AC #10: Save validation**
When the user clicks Save with no form selected (Add mode) or no message/rationale filled,
Then the modal shows an inline validation message and does not call the save API.

**AC #11: Section 508**
The modal meets Section 508 requirements: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` pointing to the header, focus goes to the first interactive element on open, Escape key dismisses.

**AC #12: Build succeeds**
When `dotnet build` is run on `mmria-server.csproj`,
Then the build completes with 0 errors and 0 warnings introduced by this change.

---

## Tasks / Subtasks

### Phase 1 — New API endpoint in `case_validationController.cs`

- [ ] **Add `GetMetadataFields` action**:
  ```csharp
  [Authorize(Roles = "form_designer")]
  [HttpGet("metadata/fields")]
  [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
  public async Task<IActionResult> GetMetadataFields()
  ```
  - Calls `GetCurrentMetadataAsync(GetCurrentMetadataVersion())`
  - Calls `_caseValidationManager.FlattenMetadata(metadata)`
  - Returns `Ok(fields)` as JSON
  - Wraps in try-catch returning 500 on error

### Phase 2 — "Add Rule" button in admin UI

- [ ] **Add button** in the admin UI header/actions area in `case-validation.js` (or in `Index.cshtml` — follow the pattern of existing buttons in the admin UI):
  ```html
  <button type="button" class="btn btn-primary" onclick="caseValidationMetadataOpenAddModal()">Add Rule</button>
  ```

- [ ] **Add "Edit" button** to each rendered rule row. In the rule row render function, append:
  ```html
  <button type="button" class="btn btn-sm btn-outline-secondary" onclick="caseValidationMetadataOpenEditModal('{rule_id}', '{rule_category}')">Edit</button>
  ```

### Phase 3 — Modal HTML

- [ ] **Define modal HTML** (add to the admin view or render dynamically in JS):
  - Container: `<div id="case_validation_add_edit_modal" role="dialog" aria-modal="true" aria-labelledby="case_validation_modal_title" ...>`
  - Purple header: `<div id="case_validation_modal_title">Add Validation Rule</div>`
  - Body sections:
    - Rule type radio/tabs
    - Form dropdown (`<select id="cv_modal_form">`)
    - Field dropdown (`<select id="cv_modal_field">` — for field/connected types)
    - Related field dropdown (`<select id="cv_modal_related_field">` — connected type only)
    - Read-only display: form_path, field_path, field_type, metadata_path
    - Common fields: enabled, severity, validation_level, confidence, review_status, source, message, rationale, explanation, admin_notes
    - Field-rule-specific: rule_type selector, conditional number/text inputs for min_value/max_value/max_length/regex/allowed_values
    - Connected-field-rule-specific: comparison, max_difference, require_same_container, trigger_values
    - Form-status-rule-specific: expected_status
  - Footer: Save button, Cancel button, `<div id="cv_modal_status">` for inline messages

### Phase 4 — Modal JavaScript

- [ ] **`caseValidationMetadataOpenAddModal()`**: Clear all modal fields, set title to "Add Validation Rule", set mode to 'add', load metadata fields if not cached, open modal.

- [ ] **`caseValidationMetadataOpenEditModal(ruleId, ruleCategory)`**: Find rule by id and category from loaded rules state, populate modal fields with rule values, set title to "Edit Validation Rule", set mode to 'edit', open modal.

- [ ] **`caseValidationMetadataLoadMetadataFields()`**: Fetch from `GET /api/case-validation/metadata/fields`. Cache result in `case_validation_state.metadata_fields`. On success, populate Form dropdown with distinct form_prompt values (maintain metadata order).

- [ ] **Form dropdown `change` handler**: Filter Field dropdown options to only fields where `form_path` matches selected form. Clear field selection.

- [ ] **Field dropdown `change` handler**: Auto-populate the read-only metadata display fields (form_path, field_path, field_type, metadata_path) from the cached metadata_fields array.

- [ ] **Rule type change handler**: Show/hide the field-type-specific input groups based on selected rule type. Hide "Field" dropdown row when rule type is "Form Status Rule". Hide "Related Field" row unless rule type is "Connected Field Rule".

- [ ] **Rule type sub-type change handler** (field rule only): Show/hide min_value/max_value vs max_length vs regex vs allowed_values inputs based on selected `rule_type` value.

- [ ] **`caseValidationMetadataSaveRule()`**:
  1. Validate required fields — show error in `#cv_modal_status` and return if invalid.
  2. Build rule object from modal fields based on rule type.
  3. Generate `id` for new rules; use existing `id` for edits.
  4. Clone the loaded rules document from `case_validation_state.rules`.
  5. For add: append to the appropriate list. For edit: find and replace by `id`.
  6. Call `PUT /api/case-validation/rules/{metadata_version}` with the updated document.
  7. On success: close modal, reload rules via `GET /api/case-validation/rules/current`, re-render rule list.
  8. On error: show error in `#cv_modal_status`, leave modal open.

- [ ] **`caseValidationMetadataGenerateRuleId(ruleType, fieldPath, relatedFieldPath, formPath, existingIds)`**: Generate id based on rule type, deduplicate with counter suffix if needed.

- [ ] **Focus management**: On modal open, move focus to the rule type selector. On close, return focus to the triggering button. Escape key closes modal.

### Phase 5 — Build and smoke test

- [ ] Run `dotnet build mmria-server.csproj` — 0 errors.
- [ ] Navigate to `/case_validation_metadata` as `form_designer`.
- [ ] Click "Add Rule" — modal opens, Form dropdown populated with all forms from DB metadata.
- [ ] Select "ER Visits and Hospitalizations" — Field dropdown filters to ER Visits fields only.
- [ ] Select "Temperature" — metadata fields auto-populate: `field_path = er_visit_and_hospital_medical_records/vital_signs/temperature`, `field_type = number`.
- [ ] Set severity = "warning", message = "Test rule", min_value = 80, max_value = 115.
- [ ] Click Save — rule appears in the list.
- [ ] Click Edit on the saved rule — modal reopens with pre-populated values.
- [ ] Change severity to "hard" and save — list shows updated severity.
