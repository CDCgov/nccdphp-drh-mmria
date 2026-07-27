# Story 33.2 - Date Group Validity and Timeline Plausibility

**Epic:** 33 - Case Generator Date and Number Plausibility
**Story ID:** 33.2
**Status:** ready-for-dev

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

- [ ] Fix date group component generation (AC-1, AC-2, AC-3)
  - [ ] Update date-group detection so month/year groups are recognized without assuming a `day` child exists.
  - [ ] Generate components only for child names present in metadata.
  - [ ] Use one `DateTime`/`DateOnly` anchor per date group.
  - [ ] Preserve the existing blank sentinel convention when a date group is intentionally blank.

- [ ] Make date generation path-aware (AC-4 through AC-8)
  - [ ] Pass metadata path/context to date generation where needed.
  - [ ] Prefer a small helper over a new scenario engine.
  - [ ] Keep existing post-process methods and strengthen them only where needed.

- [ ] Anchor core case timeline (AC-4, AC-5)
  - [ ] Continue generating `home_record/date_of_death` in the past.
  - [ ] Preserve record ID year alignment with date of death.
  - [ ] Preserve maternal DOB/death-certificate DOB synchronization.
  - [ ] Add limited pregnancy-date alignment for LMP, prenatal visits, delivery-related dates, and estimated confinement dates when those paths exist.

- [ ] Order admission/discharge pairs (AC-6)
  - [ ] Apply to grid rows and multiform instances without changing row counts.
  - [ ] If only one side of a pair is populated, do not invent the missing side unless metadata generation already would.

- [ ] Keep time/datetime parseable (AC-7, AC-8)
  - [ ] Preserve `HH:mm` for time values.
  - [ ] Preserve parseable datetime output.
  - [ ] Ensure no invalid or partial datetime strings are emitted.

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

TBD

### Debug Log References

### Completion Notes List

### File List

---

## QA Results

TBD
