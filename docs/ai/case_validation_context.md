# Case Validation Context

- Status: Active
- Scope: Metadata-driven case validation rules, validation tab rendering, and single-field quick edits.
- Last verified: 2026-05-06
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Case View/Edit Playwright Testing Context](./case_view_edit_playwright_testing_context.md), [Case Summary Rendering Context](./case_summary_rendering_context.md), [Case Validation Rule Seed](./case_validation_rule_seed.md), [Case Validation Field Logic Catalog](./case_validation_field_logic_catalog.md)

## Architecture

Case validation is warning-only in V1. It does not block save, form status changes, review completion, or case navigation.

Rules are version-scoped metadata documents in the metadata CouchDB database. They are separate from the core form metadata document so validation can evolve without changing the metadata schema consumed by the case renderer.

Rule document id:

```text
case-validation-rules-{metadata_version}
```

Server-side reusable logic lives under:

```text
nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseValidation
```

The folder follows the standard SharedLibraries split:

- `Model`: rule document, flattened metadata field, finding, and single-field update DTOs.
- `DAL`: CouchDB reads/writes for case validation rule documents.
- `Manager`: metadata flattening, default rule seed creation, rule evaluation helpers, and safe one-field patching through `CaseManager.SaveCaseAsync`.

Thin server routes live in:

```text
source-code/mmria/mmria-server/Controllers/api/case_validationController.cs
```

Current routes:

- `GET /api/case-validation/rules/current`
- `GET /api/case-validation/rules/current/summary`
- `GET /api/case-validation/rules/current/export`
- `PUT /api/case-validation/rules/{metadata_version}`
- `POST /api/case-validation/rules/preview`
- `POST /api/case-validation/field`

## Rule Governance

Validation is treated as reviewed metadata, not fixed clinical code. Field, connected-field, and form-status rules carry the same transparency fields:

- `validation_level`: `metadata`, `impossibility`, `plausibility`, `timeline`, `conditional`, or `form-completeness`
- `confidence`: `high`, `medium`, or `low`
- `review_status`: `generated`, `review-pending`, `reviewed`, or `retired`
- `source`: `metadata`, `seed-catalog`, `admin`, or `imported-standard`
- `rationale`, `admin_notes`, `reviewed_by`, `reviewed_at`, and `last_changed_reason`
- optional editable `bands` such as `normal`, `plausible-warning`, and `impossible-warning`

Runtime defaults normalize older labels such as `intrinsic-logic`, `connected-logic`, `timeline-logic`, and `logical-seed` into the current governance vocabulary. Generated defaults are merged into existing documents only when missing, so reviewed admin edits are not overwritten by later seed generation.

Validation level meanings:

- Metadata: checks the value against metadata shape, type, list, length, regex, or required semantics.
- Impossibility: checks for logically contradictory values that should not be possible together.
- Plausibility: checks for values that may be possible but are unlikely enough to need review.
- Timeline: checks whether dates and events are in the expected chronological order.
- Conditional: checks dependent fields against selected answers, such as Other/specify or yes/no grids.
- Form completeness: checks whether form status matches meaningful data present in the form.

## Metadata Flattening

Validation is driven from the current release version:

- `/api/version/release-version`
- `/api/version/{version}/metadata`

Flattened fields retain:

- source form path and prompt
- field path and metadata path
- prompt, type, data type, cardinality, tags, and ancestry
- lookup-resolved list values, regex, min/max, max length, required/read-only/hidden flags, and validation description
- whether the field is scalar, multiform, grid-backed, hidden, read-only, or quick-edit eligible

Form status mapping must use normalized prompts and subject ancestry. Do not couple status rules to code names like `committe_review_worksheet`, because the status fields and top-level forms do not always share names.

## Rule Categories

V1 rules are all warnings.

Form status:

- Data present but status is Not Started, Not Available, or Not Applicable.
- Completed with too little meaningful data.

Ranges:

- Metadata `min_value`, `max_value`, `max_length`, `regex_pattern`, and list values.
- Reviewed seed ranges from `case_validation_rule_seed.md`.
- Broad logical plausibility checks from `case_validation_field_logic_catalog.md`, including vital signs, Apgar scores, gestational age, birth/fetal weight, BMI, height, weight, gravida, and age.

Connected fields:

- Date, order, age, status, and related numeric consistency checks.
- Related fields are stored in validation rule metadata, not hard-coded into the case renderer.
- Timeline checks compare clinical event dates such as vital sign date/time, delivery date, injury date, and date of birth against the Home Record date of death while allowing administrative/autopsy dates after death.

## Case Tab Behavior

The virtual case route is:

```text
#/{caseIndex}/case_validation
```

The case navigation select includes `Case Validation` beside form options. Rendering is integrated with the existing `page_render(...)` flow and display switching in `case/index.js`.

The validation tab shows grouped vertical results and filters:

- Findings
- All Fields
- Form Status
- Ranges
- Connected Fields

`Open Field` stores focus context in `sessionStorage`, navigates to the source form, then `case_validation_apply_pending_focus()` highlights and focuses the rendered field after the page render cycle completes.

`Quick Edit` is only enabled when:

- the field is a supported scalar,
- the field is not grid-backed, hidden, read-only, or multiselect,
- `g_data_is_checked_out === true`.

The server still enforces current-user checkout and tab ownership by saving through `CaseManager.SaveCaseAsync`.

## Metadata Editor

Form designers can use:

```text
/case_validation_metadata
```

The editor is a rule-management dashboard. It loads the current metadata and rule document, then shows a unified list of scalar field, connected-field, and form-status rules with filters for form, category, validation level, confidence, severity, review status, source, and enabled state.

The rule detail panel shows the plain-language explanation, field labels, connected fields, thresholds, message, rationale, admin notes, metadata ancestry, and editable bands. Form designers can edit enabled state, severity, confidence, review status, thresholds/bands, warning message, rationale, admin notes, and last-change reason.

`Preview Impact` posts the edited draft document to `/api/case-validation/rules/preview`. It can run against an optional case id or pasted sample case JSON and returns warning counts by validation level, confidence, category, and severity before publishing. Saves publish the version-scoped validation rule document through `PUT /api/case-validation/rules/{metadata_version}`.

## Safety Notes

- Keep validation warning-only unless a future plan explicitly adds blocking behavior.
- Do not mutate core form metadata for validation rules.
- Do not bypass `CaseManager.SaveCaseAsync` for quick edits; audit, sync, lock, jurisdiction, and revision behavior belong there.
- Do not enable quick edit for grids or complex/multiselect controls without a separate design.
- PMSS behavior should remain guarded by metadata. Do not add PMSS-specific assumptions unless matching PMSS validation rules are added.
