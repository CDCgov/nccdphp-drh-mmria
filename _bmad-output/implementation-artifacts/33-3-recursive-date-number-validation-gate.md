---
baseline_commit: d0066f0662fe7be5270fe0cc8b168f227be85cab
---

# Story 33.3 - Recursive Date and Number Validation Gate

**Epic:** 33 - Case Generator Date and Number Plausibility
**Story ID:** 33.3
**Status:** review

---

## User Story

As a developer running the case generator with `ValidateBeforeSave = true`,
I want invalid generated date and number values to stop the run before output,
So that bad generated cases are caught at the generator boundary instead of being written to JSON files or CouchDB.

---

## Context

The existing validation path reports some problems but does not yet work as a true output gate:

- `CaseGeneratorService.GenerateCasesAsync(...)` validates when `ValidateBeforeSave` is true, then still writes JSON files and saves to CouchDB.
- `CaseGeneratorService.CollectNodes(...)` keys metadata by bare `node.name`, which collides for repeated names like `age`, `month`, `year`, `value`, and `date_of_birth`.
- `CaseValidator.ValidateCase(...)` delegates to `MetadataConstraintValidator.ValidateCase(...)`.
- `MetadataConstraintValidator.ValidateCase(...)` iterates only the top-level case dictionary, so nested forms, groups, grids, and multiform instances are not recursively checked by full metadata path.
- `MetadataConstraintValidator.ValidateNumber(...)` parses strings today, but range validation is a placeholder even though `node` exposes `min_value` and `max_value`.
- `MetadataConstraintValidator.ValidateDate(...)` uses general `DateTime.TryParse(...)`; time-only fields and grouped date components need more explicit handling.

This story makes validation a real guardrail while preserving permissive generation when validation is disabled.

---

## Acceptance Criteria

**AC-1 - Validation walks the generated case recursively**
Given a generated case contains nested forms, groups, grids, or multiform instances
When validation runs
Then every generated value is validated recursively using the full metadata path from `MetadataManager.NodeDictionary`.

**AC-2 - Number parse failures are errors**
Given a field has metadata `type = "number"`
When a populated value is not a JSON number and cannot be parsed as a number under invariant-culture rules
Then validation records an error with the full path and case number.

**AC-3 - Intentional optional blanks are allowed**
Given a non-required date or number field is intentionally blank
When validation runs
Then validation does not record a parse error for that blank.

**AC-4 - Date/time parse failures are errors**
Given a field has metadata `type = "date"`, `type = "datetime"`, or `type = "time"`
When a populated value cannot be parsed into the expected type
Then validation records an error with the full path and case number.

**AC-5 - Date group component failures are errors**
Given a group has date components
When the populated month/day/year combination is impossible, such as February 30, or partial in an unsupported way
Then validation records an error with the full path and component values.

**AC-6 - Validation errors block output when enabled**
Given `ValidateBeforeSave = true` and validation errors exist
When `CaseGeneratorService.GenerateCasesAsync(...)` completes validation
Then the result is unsuccessful, includes the validation report, and skips JSON and CouchDB output.

**AC-7 - Validation-disabled behavior remains permissive**
Given `ValidateBeforeSave = false`
When generated data would contain validation errors
Then existing permissive output behavior is preserved, and validation is not silently implied.

**AC-8 - CLI summary is useful**
Given validation blocks output
When the CLI reports the generation result
Then the summary includes enough error context to identify the case number and first failing paths.

---

## Tasks / Subtasks

- [x] Replace bare-name metadata lookup with path-aware validation input (AC-1)
  - [x] Prefer reusing `MetadataManager.NodeDictionary` instead of rebuilding a lossy dictionary.
  - [x] If `CollectNodes(...)` remains, key it by full path, not `node.name`.
  - [x] Preserve repeated field names without collisions.

- [x] Implement recursive case traversal (AC-1, AC-5)
  - [x] Traverse dictionaries for forms and groups.
  - [x] Traverse `List<Dictionary<string, object?>>` and compatible enumerable shapes for multiforms and grids.
  - [x] Build full metadata paths as traversal descends.
  - [x] Include row/instance context in error messages where helpful while still reporting the metadata path.

- [x] Tighten number/date/time validation (AC-2, AC-3, AC-4, AC-5)
  - [x] Use invariant-culture numeric parsing for string fallbacks.
  - [x] Treat populated unparseable values as errors.
  - [x] Treat intentional optional blanks as allowed.
  - [x] Validate time values as time values, not arbitrary dates.
  - [x] Validate date groups by constructing a real date from components.

- [x] Gate outputs in `CaseGeneratorService` (AC-6, AC-7, AC-8)
  - [x] After validation, check `ValidationReport.InvalidCases` or equivalent error count before writing.
  - [x] On errors, set `GenerationResult.Success = false`, set a concise `ErrorMessage`, keep `GeneratedCases` and `ValidationReport`, and return before `JsonCaseWriter` or `CouchDbWriter`.
  - [x] Preserve existing behavior when `ValidateBeforeSave` is false.

- [x] Improve result/report messaging (AC-8)
  - [x] Ensure `GenerationResult.GetSummary()` remains useful for CLI output.
  - [x] Include first failing case/path summaries without dumping excessive data.

---

## Dev Notes

### Files to Touch

- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Services/CaseGeneratorService.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Validators/CaseValidator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Utilities/MetadataConstraintValidator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/MetadataManager.cs` only if a small accessor/helper is needed
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Models/GenerationResult.cs` only if summary/error reporting needs a small extension
- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/*CaseGenerator*Tests.cs`

### Guardrails

- Do not make validation run implicitly when `ValidateBeforeSave` is false.
- Do not require live CouchDB for validator unit tests.
- Do not collapse full paths to bare names.
- Do not block output on warnings; block only validation errors.
- Do not treat intentional optional blanks as parse failures.

### Source References

- Epic: `_bmad-output/planning-artifacts/epics.md` - Epic 33, Story 33.3
- Service gate: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Services/CaseGeneratorService.cs`
- Orchestrator: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Validators/CaseValidator.cs`
- Constraint validator: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Utilities/MetadataConstraintValidator.cs`
- Path dictionary: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/MetadataManager.cs`

---

## Testing

- Add unit tests that pass nested invalid values directly to the validator.
- Include invalid nested values in:
  - a normal group,
  - a grid row,
  - a multiform instance,
  - a grouped date.
- Assert full metadata paths appear in errors.
- Add a service-level test proving writers are skipped when `ValidateBeforeSave = true` and validation errors exist.
- Run a targeted test command such as:

```powershell
dotnet test ..\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --filter CaseGenerator
```

---

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- `dotnet build ..\nccdphp-drh-mmria-utilities\mmria-tools\mmria-tools.csproj --no-restore -v minimal` - passed with 0 warnings and 0 errors.
- Unit-test additions and test execution were skipped per user request because the test build has a known issue.

### Completion Notes List

- Replaced lossy bare-name validation setup with `MetadataManager.NodeDictionary` path-aware metadata.
- Added recursive validation over nested form/group dictionaries, multiform/grid row lists, and compatible dictionary/enumerable shapes.
- Tightened number parsing to invariant culture, added min/max range checks, allowed optional blanks, and made populated invalid number/date/datetime/time values produce validation errors.
- Added date-group validation for month/day/year completeness and impossible dates.
- Added the validation output gate so validation errors return an unsuccessful `GenerationResult` before JSON or CouchDB output.
- Improved failure summaries with first case/path errors for CLI-friendly context.
- Unit tests were intentionally not added for this story at user request.

### File List

- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Services/CaseGeneratorService.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Validators/CaseValidator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Utilities/MetadataConstraintValidator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Models/GenerationResult.cs`
- `_bmad-output/implementation-artifacts/33-3-recursive-date-number-validation-gate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-07-27: Implemented recursive path-aware metadata validation gate; skipped unit tests per user request.

---

## QA Results

TBD
