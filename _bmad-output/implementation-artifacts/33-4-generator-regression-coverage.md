# Story 33.4 - Generator Regression Coverage for Date and Number Fields

**Epic:** 33 - Case Generator Date and Number Plausibility
**Story ID:** 33.4
**Status:** ready-for-dev

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

- [ ] Add focused test file(s) for Epic 33 (AC-1 through AC-8)
  - [ ] Prefer a new `CaseGeneratorPlausibilityTests.cs` or similarly named file in `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests`.
  - [ ] Keep tests targeted and readable; avoid expanding `Scenario_A_CaseGenerator`.

- [ ] Build minimal metadata fixtures (AC-1, AC-4, AC-5, AC-8)
  - [ ] Include one normal form with simple date and number fields.
  - [ ] Include a date group with month/day/year.
  - [ ] Include a month/year-only date group.
  - [ ] Include a grid with date and number columns.
  - [ ] Include a multiform with nested date/number fields.
  - [ ] Use fake HTTP metadata responses if exercising `MetadataManager.FetchMetadataAsync(...)`.

- [ ] Cover numeric behavior (AC-1, AC-2, AC-3)
  - [ ] Assert populated number values are numeric objects.
  - [ ] Assert decimal precision rules.
  - [ ] Assert plausible ranges for high-risk fields.
  - [ ] Assert optional blanks are accepted separately from invalid populated values.

- [ ] Cover date behavior (AC-1, AC-4, AC-5)
  - [ ] Assert generated date strings parse.
  - [ ] Assert grouped date components construct valid calendar dates.
  - [ ] Assert month/year-only groups do not receive an extra `day`.
  - [ ] Assert representative timeline relationships.

- [ ] Cover recursive validation and gating (AC-6, AC-7)
  - [ ] Create invalid nested number/date/time values and assert errors include full paths.
  - [ ] Prove validation errors block JSON/CouchDB output when validation is enabled.
  - [ ] Prove validation-disabled behavior remains permissive if covered in Story 33.3 tests.

- [ ] Document the test command (AC-8)
  - [ ] Include the targeted command in completion notes.

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

TBD

### Debug Log References

### Completion Notes List

### File List

---

## QA Results

TBD
