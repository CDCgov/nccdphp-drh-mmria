---
baseline_commit: d0066f0662fe7be5270fe0cc8b168f227be85cab
---

# Story 33.4 - Generator Regression Coverage for Date and Number Fields

**Epic:** 33 - Case Generator Date and Number Plausibility
**Story ID:** 33.4
**Status:** review

---

## User Story

As a maintainer,
I want focused tests around date and number generation,
So that future generator changes do not reintroduce invalid dates, non-numeric numbers, or shallow validation.

---

## Context

Existing tests cover useful adjacent behavior but not the plausibility contract for Epic 33:

- `CaseGeneratorConfigTests` covers ER vital-sign count CSV parsing.
- `CaseGeneratorWriterTests` covers CouchDB bulk writer behavior.
- `CaseTests.Scenario_A_CaseGenerator` is broad and CouchDB-backed; it is not a focused plausibility regression suite.

The new tests should live in `../nccdphp-drh-mmria-utilities/mmria-server.tests` and consume generator logic from `../nccdphp-drh-mmria-utilities/mmria-tools`, consistent with the utilities repo AI context. Prefer minimal in-memory or fake-HTTP metadata over live CouchDB.

This story can be implemented alongside 33.1, 33.2, and 33.3, but it is the closeout proof for the epic.

---

## Acceptance Criteria

**AC-1 - Focused generator plausibility tests exist**
Given the generator runs with test metadata
When tests include simple number fields, grouped date fields, grids, and multiforms
Then tests assert populated numeric fields are numeric and populated date fields are parseable.

**AC-2 - Decimal precision tests exist**
Given test metadata includes `decimal_precision = "0"` and `decimal_precision = "1"`
When numeric values are generated
Then tests assert generated values honor the requested precision.

**AC-3 - Plausible range tests exist**
Given test metadata includes `height_feet`, `height_inches`, gestational age, Apgar, vital-sign, and BMI-like fields
When numeric values are generated
Then tests assert values fall within the agreed plausible ranges from Story 33.1.

**AC-4 - Date group tests exist**
Given test metadata includes month/day/year groups and month/year-only groups
When date groups are generated
Then tests assert full date groups are valid calendar dates and month/year-only groups do not contain a generated `day` key.

**AC-5 - Timeline tests exist**
Given generated cases include date of death plus related DOB, prenatal, admission, and discharge dates
When the test inspects the generated case
Then the tested date relationships satisfy Story 33.2.

**AC-6 - Recursive validation tests exist**
Given intentionally invalid nested values are passed to the validator
When validation runs
Then tests prove recursive validation catches the invalid values and reports full metadata paths.

**AC-7 - Validation gate tests exist**
Given `ValidateBeforeSave = true`
When validation errors are present
Then tests prove output writers are not called.

**AC-8 - Tests are not CouchDB-dependent**
Given the focused plausibility suite runs locally
When no live CouchDB instance is available
Then the new tests still run and either use minimal metadata objects or a fake HTTP response for metadata fetches.

---

## Tasks / Subtasks

- [x] Add focused test file(s) for Epic 33 (AC-1 through AC-8)
  - [x] Prefer a new `CaseGeneratorPlausibilityTests.cs` or similarly named file in `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests`.
  - [x] Keep tests targeted and readable; avoid expanding `Scenario_A_CaseGenerator`.

- [x] Build minimal metadata fixtures (AC-1, AC-4, AC-5, AC-8)
  - [x] Include one normal form with simple date and number fields.
  - [x] Include a date group with month/day/year.
  - [x] Include a month/year-only date group.
  - [x] Include a grid with date and number columns.
  - [x] Include a multiform with nested date/number fields.
  - [x] Use fake HTTP metadata responses if exercising `MetadataManager.FetchMetadataAsync(...)`.

- [x] Cover numeric behavior (AC-1, AC-2, AC-3)
  - [x] Assert populated number values are numeric objects.
  - [x] Assert decimal precision rules.
  - [x] Assert plausible ranges for high-risk fields.
  - [x] Assert optional blanks are accepted separately from invalid populated values.

- [x] Cover date behavior (AC-1, AC-4, AC-5)
  - [x] Assert generated date strings parse.
  - [x] Assert grouped date components construct valid calendar dates.
  - [x] Assert month/year-only groups do not receive an extra `day`.
  - [x] Assert representative timeline relationships.

- [x] Cover recursive validation and gating (AC-6, AC-7)
  - [x] Create invalid nested number/date/time values and assert errors include full paths.
  - [x] Prove validation errors block JSON/CouchDB output when validation is enabled.
  - [x] Prove validation-disabled behavior remains permissive if covered in Story 33.3 tests.

- [x] Document the test command (AC-8)
  - [x] Include the targeted command in completion notes.

---

## Dev Notes

### Files to Touch

- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorPlausibilityTests.cs` or equivalent
- Existing generator tests only if sharing small helpers is cleaner:
  - `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorConfigTests.cs`
  - `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorWriterTests.cs`
- Generator files from Stories 33.1, 33.2, and 33.3 only as needed to make tests pass.

### Guardrails

- Do not require live CouchDB for the new focused tests.
- Do not use production metadata as the only assertion source; minimal test metadata should make failures obvious.
- Keep broad scenario tests separate from plausibility tests.
- Do not add brittle tests that assert exact random values unless the specific generator path is deterministic and seeded.

### Source References

- Epic: `_bmad-output/planning-artifacts/epics.md` - Epic 33, Story 33.4
- Tests context: `../nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`
- Tools context: `../nccdphp-drh-mmria-utilities/ai/mmria-tools_AI_CONTEXT.md`
- Current tests:
  - `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorConfigTests.cs`
  - `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorWriterTests.cs`
  - `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseTests.cs`

---

## Testing

Run targeted generator tests:

```powershell
dotnet test ..\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --filter CaseGenerator
```

If a narrower fixture name is added, also run it directly, for example:

```powershell
dotnet test ..\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --filter CaseGeneratorPlausibility
```

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-27: Added focused `CaseGeneratorPlausibilityTests` coverage in the utilities test project.
- 2026-07-27: Attempted targeted `dotnet test ... --filter CaseGeneratorPlausibility --no-restore -v minimal`; run was interrupted, and follow-up user instruction was to not worry about unit tests.
- 2026-07-27: Per user instruction, no further unit-test/build validation was run because the test build is known to fail for another reason.
- 2026-07-28: Investigated generator validation failure from 500-case run; root causes were time-only strings being treated as dates during timeline alignment and generated strings exceeding metadata `max_length`.
- 2026-07-28: `dotnet build C:\repos\nccdphp-drh-mmria-utilities\mmria-tools\mmria-tools.csproj --no-restore -v minimal` passed with 0 warnings and 0 errors after sandbox escalation for sibling-repo build writes.
- 2026-07-28: One-case non-unit smoke run passed: `dotnet run --project C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\mmria-case-generator.csproj -- --settings C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\appsettings.local.json --case-count 1 --save-to-couchdb false --validate-before-save true --output-directory C:\repos\nccdphp-drh-mmria\artifacts\generator-smoke-verify --er-visit-vital-signs-counts-csv 1`; result reported `Validation: 100.0% valid`.
- 2026-07-28: Full 500-case non-unit smoke run passed with validation enabled and CouchDB disabled: `dotnet run --no-build --project C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\mmria-case-generator.csproj -- --settings C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\appsettings.local.json --case-count 500 --save-to-couchdb false --validate-before-save true --output-directory C:\repos\nccdphp-drh-mmria\artifacts\generator-smoke-500 --er-visit-vital-signs-counts-csv 1`; result reported `Validation: 100.0% valid`.
- 2026-07-28: Fixed generated `type: "time"` values to use canonical `HH:mm:ss` strings; fresh 500-case smoke run plus metadata-aware JSON scan confirmed all populated metadata time fields use `HH:mm:ss`.
- 2026-07-28: Investigated local case-list ordering after generated CouchDB load; local `by_date_last_updated` view showed generated UTC `Z` timestamps sorting ahead of later app-edited local-offset timestamps.
- 2026-07-28: `dotnet build C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\mmria-case-generator.csproj --no-restore -v minimal` passed with 0 warnings and 0 errors after the timestamp-format fix.
- 2026-07-28: Three-case non-unit smoke run passed: `dotnet run --no-build --project C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\mmria-case-generator.csproj -- --settings C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\appsettings.local.json --case-count 3 --save-to-couchdb false --validate-before-save true --output-directory C:\repos\nccdphp-drh-mmria\artifacts\generator-local-offset-smoke --er-visit-vital-signs-counts-csv 1`; result reported `Validation: 100.0% valid`.
- 2026-07-28: Investigated ER visit selection drift; live metadata sorts ER visits by arrival year/month/day, while generated ER arrival dates could be reverse-sorted or inconsistent with admission/discharge dates.
- 2026-07-28: Added generator-side ER timeline alignment and metadata sort-order persistence so generated multiform/grid arrays are stored in the same order the UI will apply.
- 2026-07-28: Three-case ER sort smoke run passed with validation enabled and CouchDB disabled; generated ER visits were ascending by arrival date, first displayed ER retained the configured 100 vital-sign rows, and vital-sign `date_and_time` rows were ascending.
- 2026-07-28: Full 500-case non-unit ER sort smoke run passed: `dotnet run --no-build --project C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\mmria-case-generator.csproj -- --settings C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\appsettings.local.json --case-count 500 --save-to-couchdb false --validate-before-save true --output-directory C:\repos\nccdphp-drh-mmria\artifacts\generator-er-sort-500 --er-visit-vital-signs-counts-csv 100`; result reported `Validation: 100.0% valid`.
- 2026-07-28: Metadata-aware scan over the 500 ER sort smoke files confirmed all ER visit arrival dates are ascending, admission is not before arrival, discharge is not before admission, and the configured 100 vital signs remain on the first displayed ER visit.
- 2026-07-28: Adjusted the ER vital-sign override to apply the configured vital-sign row count to every generated ER visit instead of only the first generated visit, so second/subsequent ER visits can be tested.
- 2026-07-28: Full 500-case all-ER-vitals smoke run passed: `dotnet run --no-build --project C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\mmria-case-generator.csproj -- --settings C:\repos\nccdphp-drh-mmria-utilities\mmria-case-generator\appsettings.local.json --case-count 500 --save-to-couchdb false --validate-before-save true --output-directory C:\repos\nccdphp-drh-mmria\artifacts\generator-er-all-vitals-500 --er-visit-vital-signs-counts-csv 100`; result reported `Validation: 100.0% valid`.
- 2026-07-28: Scan over the 500 all-ER-vitals files confirmed all 1,249 generated ER visits had 100 vital-sign rows, sorted vital-sign datetimes, sorted ER arrival dates, admission not before arrival, and discharge not before admission.

### Completion Notes List

- Added focused generator plausibility regression coverage using minimal metadata fixtures and fake HTTP metadata fetches instead of live CouchDB.
- Covered populated numeric object output, decimal precision `0` and `1`, high-risk numeric ranges, generated date string parsing, full date-group validity, and month/year-only groups without invented `day` keys.
- Covered representative timeline relationships for date of death, date of birth, prenatal dates, estimated delivery, admission, and discharge dates.
- Covered recursive validator behavior for invalid nested number/date/time values with full metadata paths and optional blank number/date values accepted separately.
- Covered validation gating so `ValidateBeforeSave = true` blocks JSON/CouchDB output on validation errors, and `ValidateBeforeSave = false` remains permissive.
- Targeted command documented: `dotnet test ..\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --filter CaseGeneratorPlausibility`.
- Added regression assertions for generated string `max_length` and time fields nested inside admission/discharge date groups.
- Fixed generator string dispatch to use metadata-aware string generation and truncate generated strings to metadata `max_length`.
- Fixed timeline date detection so time-only strings such as `HH:mm` are not treated as date candidates and rewritten during admission/discharge alignment.
- Fixed time generation to emit canonical `HH:mm:ss` strings instead of permissive-but-noncanonical `HH:mm`.
- Fixed generated top-level `date_created` and `date_last_updated` strings to use the app-compatible local offset format so CouchDB string sorting remains consistent with app save timestamps.
- Fixed generated ER visit timelines so arrival, admission, discharge, and vital-sign datetimes remain coherent and already match the UI metadata sort order.
- Added a final metadata sort-order pass for generated multiform and grid arrays so generated documents are persisted in the same order the UI applies.
- Applied the configured ER vital-sign row count to every generated ER visit instead of clearing vital signs from subsequent visits.

### File List

- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorPlausibilityTests.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/DateValueGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/StringValueGenerator.cs`
- `_bmad-output/implementation-artifacts/33-4-generator-regression-coverage.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-07-27: Added focused Epic 33 generator plausibility regression coverage; marked story ready for review with unit-test execution intentionally skipped per user instruction.
- 2026-07-28: Addressed generator validation fallout for string max lengths and time-only timeline alignment; verified with `mmria-tools` build plus one-case and 500-case generator smoke runs.
- 2026-07-28: Canonicalized generated time strings to `HH:mm:ss` and verified 500 generated cases against metadata time fields.
- 2026-07-28: Aligned generated case header timestamps with app save timestamp format to avoid generated UTC `Z` rows sorting ahead of newer local edits in CouchDB string views.
- 2026-07-28: Aligned generated ER/list ordering with metadata sort rules to prevent generated records from changing displayed position when the UI applies `sort_order`.
- 2026-07-28: Populated configured vital-sign rows on every generated ER visit so second and later ER visit screens remain testable.

---

## QA Results

TBD
