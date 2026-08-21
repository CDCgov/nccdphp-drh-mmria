# Story 43.1 — Vitals Import: Fix Father's Race Principal Tribe Mapping (FRACE16 + FRACE17)

**Epic:** 43 — Vitals Import Father's Race Principal Tribe Fix (v4.2)
**Story ID:** 43.1
**Status:** done
**Date added:** 2026-08-20
**Source:** BUG 119513 — Rel 4.2, P-High, reported by Susana (MMRIA\ITDM 25-26 - Option Yr 4)
**PRD:** FR-13.1 – FR-13.5 in `_bmad-output/planning-artifacts/prds/prd-mmria-2026-08-06/prd.md`

---

## User Story

As a vital importer,
When I upload an IJE vitals file (NAT or FET) containing values in `FRACE16` and/or `FRACE17`,
I want the "Specify Principal Tribe" field on Father's Race (`bfdcpdofr_p_tribe`) to be populated with the correct pipe-delimited combination of the two source values,
So that the imported case reflects the abstractor's intended father's-race principal tribe entry.

---

## Acceptance Criteria

**AC-1 — NAT import: both FRACE16 and FRACE17 populated → pipe-joined**
Given a NAT IJE file record with `FRACE16 = "Cherokee"` and `FRACE17 = "Navajo"`,
When the vitals import processes the record,
Then the resulting CouchDB case document has
`birth_fetal_death_certificate_parent.demographic_of_father.race.principle_tribe = "Cherokee|Navajo"`.

**AC-2 — NAT import: only FRACE16 populated → FRACE16 verbatim**
Given a NAT record with `FRACE16 = "Cherokee"` and `FRACE17` blank,
When the import runs,
Then `principle_tribe = "Cherokee"`.

**AC-3 — NAT import: only FRACE17 populated → FRACE17 verbatim**
Given a NAT record with `FRACE16` blank and `FRACE17 = "Navajo"`,
When the import runs,
Then `principle_tribe = "Navajo"`.
_This is the regression case — under current buggy behavior the field is empty._

**AC-4 — NAT import: both blank → field left empty**
Given a NAT record where both `FRACE16` and `FRACE17` are blank/whitespace,
When the import runs,
Then `principle_tribe` is empty (unchanged from its default state on the case document).

**AC-5 — FET import: identical behavior to AC-1 through AC-4**
The same four cases (AC-1 through AC-4) are exercised on the FET (fetal death) import path and produce the same results at the same MMRIA path (the `bfdc_parent` path is shared between NAT and FET father's-race storage).

**AC-6 — Verbatim string transfer**
Source values from `FRACE16` and `FRACE17` are transferred verbatim (subject only to the trim performed during IJE line parsing). No case-normalization, character mapping, or dictionary lookup is applied. The MMRIA field is stored as a JSON string.

**AC-7 — No collateral changes to sibling FRACE mappings**
The `FRACE18_19`, `FRACE20_21`, and `FRACE22_23` call sites and their helper rules are unchanged. Regression sanity check: an IJE record that populates `FRACE18` and `FRACE19` (e.g., "Chinese" and "Vietnamese") still produces the expected pipe-joined value at `bfdcpdofr_o_asian` in both NAT and FET paths.

**AC-8 — Unit test coverage for the four-case matrix**
Automated unit tests are added covering the four value combinations of `FRACE16` / `FRACE17` on both `FRACE16_17_NAT_Rule` and `FRACE16_17_FET_Rule` helper methods. Tests use realistic string values (e.g., "Cherokee", "Navajo") — not just `"a"`/`"b"` — and assert exact expected output.

**AC-9 — Build clean**
`dotnet build` succeeds for both `mmria.services` and `mmria.common` with zero errors and no new warnings.

---

## Dev Notes — Root Cause and Fix

### Root Cause

Two call sites in `BatchItemProcessingService.cs` pass `field_set["FRACE16"]` **twice** to the pipe-join helper, instead of passing `field_set["FRACE17"]` as the second argument. This is a copy/paste defect — the adjacent `FRACE18_19`, `FRACE20_21`, and `FRACE22_23` calls on the same block already follow the correct `(N, N+1)` pattern.

**NAT path — `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` line ~1682:**
```csharp
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE16_17"], FRACE16_17_NAT_Rule(field_set["FRACE16"], field_set["FRACE16"]), new_case);
//                                                                                                    ^^^^^^^^^^^^^^^^^^^^^^ should be field_set["FRACE17"]
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE18_19"], FRACE18_19_NAT_Rule(field_set["FRACE18"], field_set["FRACE19"]), new_case);   // correct
```

**FET path — `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` line ~2024:**
```csharp
gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FRACE16_17"], FRACE16_17_FET_Rule(field_set["FRACE16"], field_set["FRACE16"]), new_case);
//                                                                                                    ^^^^^^^^^^^^^^^^^^^^^^ should be field_set["FRACE17"]
gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FRACE18_19"], FRACE18_19_FET_Rule(field_set["FRACE18"], field_set["FRACE19"]), new_case);   // correct
```

### Helper Rule Contract (No Change Required)

The helper methods in `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs` already implement the pipe-join specification correctly. They just never receive `FRACE17`.

- `FRACE16_17_NAT_Rule(string value16, string value17)` — line ~3775
- `FRACE16_17_FET_Rule(string value16, string value17)` — line ~8168

Both follow the pattern:
```csharp
if (both non-blank)      value = $"{value16}|{value17}";
else if (value16 set)    value = value16;
else                     value = value17;   // yields "" when both blank
```

No edits to the helpers are required. **Do not** attempt to "fix" or refactor them as part of this story — the defect is entirely at the two call sites.

### MMRIA Target Path

Both call sites route through:
```
Parent_{NAT|FET}_IJE_to_MMRIA_Path["FRACE16_17"]
  = "birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe"
```
This path corresponds to metadata field `bfdcpdofr_p_tribe` (SASS export name) — see `source-code/mmria/mmria-server/database-scripts/metadata.json` line ~9531.

### Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` | Line ~1682: replace second `field_set["FRACE16"]` with `field_set["FRACE17"]` in the NAT `FRACE16_17_NAT_Rule` call. Line ~2024: same fix for the FET `FRACE16_17_FET_Rule` call. |
| `nccdphp-drh-mmria-utilities/mmria-server.tests/**` **or** `nccdphp-drh-mmria-services/mmria.services.tests/**` | Add unit tests for the four-case matrix on both helper methods (see AC-8). Pick whichever test project already covers `MMRIAServicesHelper` rule methods; if neither does, add tests to `mmria-server.tests` following the `Category=IJE` convention. |

### Test Approach

Two options for AC-8:

- **Option A — Rule-helper unit tests (preferred, lowest cost).** Call `FRACE16_17_NAT_Rule` and `FRACE16_17_FET_Rule` directly with the four value combinations. Fast, deterministic, no fixtures required.
- **Option B — End-to-end via `BatchItemProcessingService`.** Exercise the mapping through the batch processor with a stub IJE record. Higher-fidelity but requires more scaffolding; only pursue if rule-helper tests already exist elsewhere in the same project and the same pattern applies here.

Option A is sufficient for AC-8. Option B is optional and does not need to happen in this story if the scaffolding does not already exist.

### Non-Goals

- **Retrospective data correction is out of scope.** Cases already imported with the wrong mapping remain wrong until a separate remediation story is authorized (tracked as OI-v42-4 in the PRD; potential Story 43.2).
- **No changes to mother's race (`MRACE*`) mappings.**
- **No changes to `FRACE18_19` / `FRACE20_21` / `FRACE22_23` call sites or helpers.** Those are already correct.
- **No UI or metadata changes.** The metadata already exposes `principle_tribe` correctly; only the import path is broken.

### Sequencing

Independent of all other v4.2 epics. Can be worked immediately.

---

## Tasks / Subtasks

- [ ] Fix NAT call site (AC-1..AC-4)
  - [ ] `BatchItemProcessingService.cs` line ~1682: change second argument of `FRACE16_17_NAT_Rule` from `field_set["FRACE16"]` to `field_set["FRACE17"]`
- [ ] Fix FET call site (AC-5)
  - [ ] `BatchItemProcessingService.cs` line ~2024: change second argument of `FRACE16_17_FET_Rule` from `field_set["FRACE16"]` to `field_set["FRACE17"]`
- [ ] Add unit test coverage (AC-8)
  - [ ] Four-case matrix on `FRACE16_17_NAT_Rule`: (both set, only 16, only 17, both blank)
  - [ ] Four-case matrix on `FRACE16_17_FET_Rule`: (both set, only 16, only 17, both blank)
  - [ ] Use realistic string values (e.g., "Cherokee", "Navajo") and assert exact expected output
- [ ] Regression sanity check (AC-7)
  - [ ] Add or confirm a unit test exists for `FRACE18_19_NAT_Rule` with `(FRACE18="Chinese", FRACE19="Vietnamese")` producing `"Chinese|Vietnamese"`
- [ ] Build (AC-9)
  - [ ] `dotnet build` on `mmria.services.csproj` — zero errors, no new warnings
  - [ ] `dotnet build` on `mmria.common.csproj` — zero errors, no new warnings
- [ ] Run test suite
  - [ ] Run whichever test project the new tests were added to and confirm all pass
- [ ] Manual smoke test (optional — recommended if a sample IJE file is available)
  - [ ] Import a NAT file with `FRACE16` and `FRACE17` both populated; confirm `principle_tribe` shows `"{FRACE16}|{FRACE17}"` on the case
  - [ ] Import a FET file with only `FRACE17` populated; confirm `principle_tribe` shows the `FRACE17` value (previously empty)

---

## Dev Agent Record

### Completion Notes

- **Root-cause fix (AC-1 – AC-5):** Two single-character copy/paste corrections in `BatchItemProcessingService.cs` — the second argument of the `FRACE16_17_NAT_Rule` call (line ~1692) and the `FRACE16_17_FET_Rule` call (line ~2034) both passed `field_set["FRACE16"]` twice. Changed the second argument at each site to `field_set["FRACE17"]`. The `FRACE16_17_NAT_Rule` / `FRACE16_17_FET_Rule` helper methods in `MMRIAServicesHelper.cs` were verified correct and unchanged (AC-6).
- **AC-7 (no collateral damage):** Verified by inspection that the adjacent `FRACE18_19`, `FRACE20_21`, and `FRACE22_23` calls in both NAT and FET paths already used the correct `(FRACE_n, FRACE_n+1)` argument pattern. Added a regression assertion in the unit tests that exercises `FRACE18_19_NAT_Rule` against realistic values (`"Chinese"`, `"Vietnamese"`) to lock the pattern.
- **AC-8 (test coverage):** Added `mmria-server.tests/Tests/FRACEMappingRuleTests.cs` — nine `[Test]` methods, `[Category("IJE")]`. Covers the four-case matrix (both populated, only 16, only 17, both blank) on both the NAT and FET helpers, plus the AC-7 sanity assertion on `FRACE18_19_NAT_Rule`. Tests reference `mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper` via the existing `mmria.common` project reference — no new project references, no new using aliases, no external DI. Skill applied per user direction: tests written but not executed in this session.
- **AC-9 (build):** `mmria-server.csproj` build task ran to completion (exit 0). `mmria.services` compilation clean. `mmria-server.tests.csproj` MSBuild step reported no `error CS` output but the file-copy phase for `mmria.common.dll` was blocked by an attached debugger holding the DLL (PID 30852, "Visual Studio Debug Adapter for .NET"); the compile itself succeeded.
- **Retrospective correction (OI-v42-4):** Out of scope for 43.1 as specified. No retrospective migration was authored. Any previously imported case with only `FRACE17` populated will remain incorrect until a follow-up story is authorized.

### Change Log

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` | NAT path (~line 1692): second arg to `FRACE16_17_NAT_Rule` changed from `field_set["FRACE16"]` to `field_set["FRACE17"]`. FET path (~line 2034): same correction on `FRACE16_17_FET_Rule`. |
| `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/FRACEMappingRuleTests.cs` | **New file.** Nine `[Test]` methods covering the four-case matrix on both `FRACE16_17_NAT_Rule` and `FRACE16_17_FET_Rule`, plus an AC-7 sanity assertion on `FRACE18_19_NAT_Rule`. All under `[Category("IJE")]`. |
