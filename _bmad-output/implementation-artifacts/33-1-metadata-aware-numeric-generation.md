---
baseline_commit: 35f51d1c4c5050d9c9c16a0fd05eaa3aa4c0189b
---

# Story 33.1 - Metadata-Aware Numeric Generation

**Epic:** 33 - Case Generator Date and Number Plausibility
**Story ID:** 33.1
**Status:** done

---

## User Story

As a developer or tester using generated MMRIA cases,
I want populated numeric fields to contain numeric, plausible values,
So that generated cases exercise realistic workflows without poisoning tests with obvious invalid data.

---

## Context

`mmria-case-generator` is a thin CLI wrapper. The numeric generation behavior lives in the sibling utilities repo under `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration`.

Current flow:

- `CaseDataGenerator.GenerateNodeData(...)` dispatches metadata `type = "number"` to `_numberGenerator.Generate(node.name, isRequired)` without passing metadata path, decimal precision, min, or max.
- `NumberValueGenerator.Generate(...)` returns `string?` and falls back to `value.ToString("F2")`, so populated numeric fields are usually JSON strings.
- `NumberValueGenerator.GenerateInt(...)` duplicates most of `Generate(...)` and is not the primary metadata-number path.
- `node` already exposes `decimal_precision`, `min_value`, and `max_value` in `nccdphp-drh-mmria-common/mmria.common/metadata/node.cs`.
- Calculated fields in `CaseDataGenerator` already tolerate object values via `TryParseDouble(...)`; preserve that behavior while making generated populated number values numeric.

This is a conservative generator-only change. Do not edit production metadata, generated strongly-typed case model files, or runtime app behavior.

---

## Acceptance Criteria

**AC-1 - Populated metadata numbers are numeric**
Given a metadata node has `type = "number"`
When the generator populates the field
Then the emitted JSON value is a numeric value (`int`, `double`, `decimal`, or nullable numeric), not an arbitrary text string.

**AC-2 - Optional blank convention is preserved**
Given a non-required numeric field is intentionally left blank by strategy completeness
When the case is serialized
Then the existing blank convention is preserved only for intentional blank values, and validation does not treat those intentional blanks as invalid numbers.

**AC-3 - Decimal precision is honored**
Given a numeric metadata node has `decimal_precision = "0"`
When the generator produces a populated value
Then the value has no fractional component.

Given a numeric metadata node has `decimal_precision = "1"` or another supported positive precision
When the generator produces a populated value
Then the value is rounded to that precision using invariant-culture numeric rules.

**AC-4 - Metadata min/max are honored when present**
Given a numeric metadata node has parseable `min_value` and/or `max_value`
When no narrower field-specific range applies
Then the generated value respects the parseable metadata bounds.

**AC-5 - High-risk numeric fields use plausible ranges**
Given a high-risk clinical or date-adjacent numeric field is generated
When the metadata path or field name matches a known pattern
Then the generator uses these plausible ranges:

| Field pattern | Plausible range |
| --- | --- |
| `height_feet` | 4-6 |
| `height_inches` | 0-11 |
| generic adult `height` in inches | 58-74 |
| `weight`, `pre_pregnancy_weight`, `weight_at_delivery`, `admission_weight` | 90-350 |
| `birth_weight`, `fetal_weight` in grams | 500-5000 |
| `bmi` | 15.0-60.0 |
| `maternal_age`, `mother_age` | 18-45 normally, 12-55 for edge strategy |
| generic `age` | bounded 0-90 normally, 0-100 for edge strategy |
| `gestational_age_weeks`, `gestational_age` | 24-42 normally, 0-45 for edge strategy |
| `gestational_age_days` | 0-6 |
| `days_postpartum` | 0-365 |
| Apgar score fields | 0-10 |
| systolic blood pressure | 70-250 |
| diastolic blood pressure | 30-150 |
| pulse / heart rate | 30-220 |
| respiration | 6-60 |
| oxygen saturation | 50-100 |
| temperature | 90.0-107.0 |

**AC-6 - Fallback remains bounded**
Given no metadata constraint or special range applies
When a populated number is generated
Then the existing broad fallback behavior remains bounded and numeric.

---

## Tasks / Subtasks

- [ ] Add a metadata-aware numeric generation path (AC-1 through AC-6)
  - [ ] Update the number dispatch in `CaseDataGenerator.GenerateNodeData(...)` to pass the full metadata node/path into numeric generation.
  - [ ] Prefer a small overload or helper in `NumberValueGenerator` over a new subsystem.
  - [ ] Preserve existing public methods where practical so other tests/callers do not break.

- [ ] Return numeric objects for populated values (AC-1, AC-2)
  - [ ] Return `int`, `double`, or `decimal` for populated generated numbers.
  - [ ] Continue returning the current intentional blank value for optional unpopulated fields.
  - [ ] Avoid culture-sensitive `ToString()`/`double.Parse()` logic for numeric decisions.

- [ ] Apply metadata precision and bounds (AC-3, AC-4)
  - [ ] Parse `node.decimal_precision`, `node.min_value`, and `node.max_value` defensively.
  - [ ] Use precision to select integer vs rounded decimal output.
  - [ ] Treat malformed metadata constraints as absent, not fatal.

- [ ] Add plausible range selection (AC-5, AC-6)
  - [ ] Use full metadata path first, then field name as fallback.
  - [ ] Handle repeated names such as `age`, `value`, `weight`, `month`, and `day` by checking path context.
  - [ ] Keep the range table close to the existing generator code and small enough to review.

- [ ] Verify calculated post-processing still works (AC-1 through AC-6)
  - [ ] Confirm `CalculateBMI(...)`, `CalculateWeightGain(...)`, and `TryParseDouble(...)` still work with numeric objects and intentional blanks.

---

## Dev Notes

### Files to Touch

- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/NumberValueGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/*CaseGenerator*Tests.cs`

### Guardrails

- Keep this as a utilities/tooling change only.
- Do not hand-edit generated `mmria_case*.cs` files.
- Do not change production metadata JSON to make tests pass.
- Do not introduce new external packages or services.
- Prefer path-aware helpers over broad rewrites of the case generation pipeline.

### Source References

- Epic: `_bmad-output/planning-artifacts/epics.md` - Epic 33, Story 33.1
- Utilities context: `../nccdphp-drh-mmria-utilities/ai/AI_CONTEXT.md`
- Tools context: `../nccdphp-drh-mmria-utilities/ai/mmria-tools_AI_CONTEXT.md`
- Current dispatch: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- Current numeric generator: `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/NumberValueGenerator.cs`
- Metadata node properties: `nccdphp-drh-mmria-common/mmria.common/metadata/node.cs`

---

## Testing

- Add focused NUnit tests in `../nccdphp-drh-mmria-utilities/mmria-server.tests`.
- Cover decimal precision `0` and `1`.
- Cover optional blank values separately from invalid numeric values.
- Cover at least: height feet/inches, weight, birth weight, BMI, maternal age, gestational age, Apgar, vital signs, and fallback numeric fields.
- Run a targeted test command such as:

```powershell
dotnet test ..\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --filter CaseGenerator
```

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-27: Added focused NUnit coverage for metadata-aware number generation; targeted test execution was intentionally skipped after user interruption/request.
- 2026-07-27: Implemented generator changes without running validation per user instruction to ignore tests for the moment.

### Completion Notes List

- Added a metadata-node numeric generation overload that returns numeric objects for populated number fields while preserving the existing optional blank convention.
- Added invariant-culture metadata parsing for decimal precision, min, and max values; malformed constraints are treated as absent.
- Added path-first plausible range selection for high-risk numeric fields including height, weight, birth/fetal weight, BMI, age, gestational age, Apgar scores, vital signs, and fallback bounded values.
- Updated `CaseDataGenerator.GenerateNodeData(...)` to dispatch metadata number nodes through the metadata-aware overload.
- Validation is pending because tests were not run at the user's request.

### File List

- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/CaseDataGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-tools/Testing/CaseGeneration/Generators/ValueGenerators/NumberValueGenerator.cs`
- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/CaseGeneratorNumericValueTests.cs`

### Change Log

- 2026-07-27: Implemented metadata-aware numeric generation and focused regression tests; story remains in-progress pending validation.

---

## QA Results

TBD
