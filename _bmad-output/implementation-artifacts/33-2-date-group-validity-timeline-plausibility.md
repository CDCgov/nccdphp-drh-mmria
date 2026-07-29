---
baseline_commit: d0066f0662fe7be5270fe0cc8b168f227be85cab
---

# Story 33.2 - Date Group Validity and Timeline Plausibility

**Epic:** 33 - Case Generator Date and Number Plausibility
**Story ID:** 33.2
**Status:** review

---

## User Story

As a developer or tester using generated MMRIA cases,
I want date values to be valid and mostly chronological,
So that generated data supports workflow and reporting tests without impossible timelines.

---

## Context

The current date path is already safer than random strings because values come from `DateTime`, but it has component and timeline gaps:

- `DateValueGenerator.GenerateDate(...)` creates dates from field-name heuristics, mostly independent of the case timeline.
- `CaseDataGenerator.IsDateGroup(...)` says a date group has month/day/year children, but currently returns true when only `month` and `year` are present.
- `GenerateDateComponents(...)` always emits `month`, `day`, and `year`, even if the metadata group only defines month/year.
- `GenerateDateComponentValue(...)` can generate standalone `month`, `day`, and `year` values from different random dates when components are handled independently.
- `PostProcessHomeRecord(...)` sets `home_record/date_of_death` and the record ID year.
- `PostProcessDateOfBirth(...)` aligns maternal DOB, death-certificate DOB, and ages to date of death.

This story should tighten validity and timeline plausibility without building a full clinical scenario engine.

---

## Acceptance Criteria

**AC-1 - Complete date groups are valid**
Given a metadata group contains `month`, `day`, and `year` children
When the generator populates the group
Then the combined components always form a valid calendar date, or all populated components use the accepted blank sentinel convention.

**AC-2 - Month/year groups do not gain extra fields**
Given a date group contains only `month` and `year`
When the generator populates the group
Then only metadata-defined components are emitted; the generator does not add a `day` component that is absent from metadata.

**AC-3 - Standalone date components do not drift**
Given `month`, `day`, and `year` components are generated as part of the same date concept
When they are populated
Then they come from the same date anchor rather than separate random dates.

**AC-4 - Date of death anchors core case dates**
Given `home_record/date_of_death` is generated
When related fields are generated or post-processed
Then maternal date of birth, death certificate date of birth, and record ID year remain consistent with date of death.

**AC-5 - Pregnancy-related dates are plausible**
Given pregnancy-related date groups are present
When `date_of_last_normal_menses`, prenatal visit dates, delivery-related dates, and estimated confinement dates are generated
Then they are internally plausible relative to the case timeline:

- LMP precedes prenatal visits and delivery-related dates.
- Prenatal visit dates do not occur after date of death.
- Gestational age fields align with nearby date groups when both are populated.
- Missing metadata fields are ignored rather than invented.

**AC-6 - Admission/discharge ordering is plausible**
Given ER or hospital admission and discharge date groups are present in a generated record
When both fields are populated
Then admission is not after discharge, and both dates remain near the pregnancy/death timeline.

**AC-7 - Generic dates avoid accidental futures**
Given generic `date`, `datetime`, and `time` metadata fields are generated
When the strategy is not explicitly edge-case focused
Then future dates are avoided unless the field semantics require a future projection.

**AC-8 - Edge dates are intentional and valid**
Given the edge strategy is used
When edge dates are emitted
Then edge values remain valid calendar dates and are constrained to intentional edge cases documented in tests.

---

## Tasks / Subtasks

- [x] Fix date group component generation (AC-1, AC-2, AC-3)
  - [x] Update date-group detection so month/year groups are recognized without assuming a `day` child exists.
  - [x] Generate components only for child names present in metadata.
  - [x] Use one `DateTime`/`DateOnly` anchor per date group.
  - [x] Preserve the existing blank sentinel convention when a date group is intentionally blank.

- [x] Make date generation path-aware (AC-4 through AC-8)
  - [x] Pass metadata path/context to date generation where needed.
  - [x] Prefer a small helper over a new scenario engine.
  - [x] Keep existing post-process methods and strengthen them only where needed.

- [x] Anchor core case timeline (AC-4, AC-5)
  - [x] Continue generating `home_record/date_of_death` in the past.
  - [x] Preserve record ID year alignment with date of death.
  - [x] Preserve maternal DOB/death-certificate DOB synchronization.
  - [x] Add limited pregnancy-date alignment for LMP, prenatal visits, delivery-related dates, and estimated confinement dates when those paths exist.

- [x] Order admission/discharge pairs (AC-6)
  - [x] Apply to grid rows and multiform instances without changing row counts.
  - [x] If only one side of a pair is populated, do not invent the missing side unless metadata generation already would.

- [x] Keep time/datetime parseable (AC-7, AC-8)
  - [x] Preserve `HH:mm` for time values.
  - [x] Preserve parseable datetime output.
  - [x] Ensure no invalid or partial datetime strings are emitted.

---

## Dev Notes

### Files to Touch

- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/DateValueGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/*CaseGenerator*Tests.cs`

### Guardrails

- Do not create a comprehensive clinical scenario engine.
- Do not add fields that are not present in metadata.
- Do not change generated model files or production metadata.
- Keep the case generator metadata-driven.
- Keep optional blanks intentional; do not turn every date into populated data just to simplify validation.

### Source References

- Epic: `_bmad-output/planning-artifacts/epics.md` - Epic 33, Story 33.2
- Date generator: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/DateValueGenerator.cs`
- Date group helpers: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- Utilities context: `../nccdphp-drh-mmria-utilities/ai/mmria-tools_AI_CONTEXT.md`

---

## Testing

- Add focused tests using minimal metadata objects; do not require live CouchDB.
- Test month/day/year groups across leap-year and month-length boundaries.
- Test month/year groups assert no `day` key is emitted.
- Test generated prenatal, admission/discharge, DOB, and DOD relationships when fields are present.
- Test edge strategy produces valid calendar dates.
- Run a targeted test command such as:

```powershell
dotnet test ..\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --filter CaseGenerator
```

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-27: User requested finishing implementation excluding tests.
- 2026-07-27: `dotnet build ..\nccdphp-drh-mmria-utilities\mmria-tools\mmria-tools.csproj --no-restore -v minimal` passed after sandbox escalation for utilities build artifact writes.

### Completion Notes List

- Implemented metadata-defined date group component generation so month/year groups no longer receive invented `day` fields.
- Added shared date anchors for complete date groups and standalone month/day/year components under the same parent path.
- Added path-aware date/datetime generation and prevented optional datetime blanks from becoming year-0001 timestamps.
- Strengthened date-of-death, maternal DOB, death-certificate DOB, pregnancy timeline, gestational age, and admission/discharge ordering post-processing without adding row counts or missing pair sides.
- Tests were not added or run per user instruction to finish this story excluding tests.

### File List

- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/DateValueGenerator.cs`
- `_bmad-output/implementation-artifacts/33-2-date-group-validity-timeline-plausibility.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-07-27: Implemented date group validity and timeline plausibility updates; marked ready for review without tests per user instruction.

---

## QA Results

TBD
