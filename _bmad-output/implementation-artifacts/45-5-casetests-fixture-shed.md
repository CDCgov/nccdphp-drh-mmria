---
baseline_commit_mmria: b3609a813cd2e51efaf2e0c3a58e7a1646d9a2ce
baseline_commit_utilities: e4c4c7326779241e161a5387b7005674d0edc4b0
---

# Story 45.5 — `CaseTests.cs` Fixture Shed _(optional)_

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.5
**Status:** review _(optional — safe to defer permanently)_
**Date added:** 2026-08-21
**Depends on:** Story 45.2 (folder layout, categories). Recommended after Story 45.3 (so the shed operates on the final tier form of each test).
**Blocks:** nothing
**Source:** 2026-08-21 analyst session with Mary. `CaseTests.cs` at 4940 lines / 44 tests / 11 categories flagged as an outlier during test inventory.

---

## User Story

As a developer maintaining case-related test coverage,
I want the 4940-line `CaseTests.cs` monolith split into one focused fixture per Category,
So that opening the file in an editor, running a subset, or reviewing a diff doesn't require scrolling past ten unrelated test areas.

---

## Acceptance Criteria

**AC-1 — Split boundaries follow existing categories**
Given the 11 `[Category(...)]` values already present in `CaseTests.cs` (per the 2026-08-21 inventory)
When Story 45.5 is complete
Then there is one output fixture per Category, named to match the Category:

| Category | Output Fixture File | Class Name |
|---|---|---|
| `Case` | `CaseTests.cs` (retains the name for the general-case bucket) | `CaseTests` |
| `CaseDelete` | `CaseDeleteTests.cs` | `CaseDeleteTests` |
| `CaseUpdateMaidenName` | `CaseUpdateMaidenNameTests.cs` | `CaseUpdateMaidenNameTests` |
| `CaseUpdateYearOfDeath` | `CaseUpdateYearOfDeathTests.cs` | `CaseUpdateYearOfDeathTests` |
| `FinalizeUnload` | `CaseFinalizeUnloadTests.cs` | `CaseFinalizeUnloadTests` |
| `LockEnforcement` | `CaseLockEnforcementTests.cs` | `CaseLockEnforcementTests` |
| `Migration` | `CaseMigrationTests.cs` | `CaseMigrationTests` |
| `OfflineLock` | `CaseOfflineLockTests.cs` | `CaseOfflineLockTests` |
| `SaveConflict` | `CaseSaveConflictTests.cs` | `CaseSaveConflictTests` |
| `Sync` | `CaseSyncTests.cs` | `CaseSyncTests` |
| `ToggleOfflineStatus` | `CaseToggleOfflineStatusTests.cs` | `CaseToggleOfflineStatusTests` |

If the inventory finds different or additional categories, adjust — but the naming pattern (`Case<Category>Tests.cs` / `Case<Category>Tests`) applies uniformly. If a test uses two categories, place it in the fixture matching its dominant category and add the second as a per-test attribute.

**AC-2 — Split-only, no rewrite**
Given each `[Test]` method
When it moves to its new fixture
Then its body, signature, attributes, and assertions are unchanged. No renames, no `Assert` translations, no re-mocking, no extraction of test-body helpers that aren't already shared.

**AC-3 — Shared helpers extracted to `CaseTestSupport`**
Given helper methods that are called by more than one of the split fixtures (from the 2026-08-21 scan, at minimum: `ToggleCaseLockAsync`, `GetServerCaseLockMinutes`, `SharedUsers` / `SampleCredentials` accessor pattern)
When Story 45.5 is complete
Then a `CaseTestSupport` static class (or a partial class if instance state is needed) lives in `mmria-server.tests/Helpers/CaseTestSupport.cs`. All shared helpers move there. Fixtures reference them by full name or `using static`.

Helpers used by only one fixture stay inside that fixture.

**AC-4 — `TestEnvironment` / `_env` handling**
Given the current fixture's `TestEnvironment` setup pattern (private `_env`, `[OneTimeSetUp]`, `[SetUp]` with `ResolveConfigurationAsync`, `[OneTimeTearDown]`)
When Story 45.5 is complete
Then each split fixture has an identical setup block (same `BootstrapAsync` purpose label, same resolve, same teardown). If Story 45.3 has already converted `CaseTests.cs` to `Mocked`, each split fixture uses the equivalent mocked pattern instead of `TestEnvironment` — the setup block is copied consistently from the pre-split file.

If `CaseTests.cs` is still `LiveDb` at Story 45.5 start (Story 45.3 chose `keep-live-semantic` or `defer-to-45.5` on it), all split fixtures inherit `[Category("LiveDb")]` and live under `Tests/LiveDb/`. Story 45.4 then acts on the split fixtures individually rather than on the monolith.

**AC-5 — `[NonParallelizable]` preserved where needed**
Given fixtures that must remain non-parallelizable (per current attributes on `CaseTests.cs` or on individual `[Test]` methods)
When Story 45.5 is complete
Then `[NonParallelizable]` is applied at the fixture level on every split fixture that inherits a category currently under a non-parallelizable constraint. Err toward keeping the constraint — do not opportunistically remove it in this story.

**AC-6 — Namespace and folder placement**
Given the flat namespace convention (from Story 45.2 AC-4)
When Story 45.5 is complete
Then every split fixture uses `namespace mmria_server.tests.Tests;` (flat) and is placed in the tier subfolder matching its final tier (`Tests/Mocked/` if converted, `Tests/LiveDb/` otherwise). `CaseTestSupport.cs` sits in `Helpers/` and follows the existing `namespace mmria_server.tests.Helpers;` convention (or whatever the existing helpers use — match existing).

**AC-7 — Category filter round-trip**
Given the Story 45.2 VS Code tasks
When the developer runs `dotnet test --filter "Category=SaveConflict"` after Story 45.5
Then the count of executed tests matches the pre-split count for that category. Extend the same round-trip for `LockEnforcement`, `Sync`, `Migration`, and any category with more than one test — confirm the split preserved every test.

**AC-8 — Full-suite green run**
Given the pre-split test outcome baseline (from Story 45.1 or Story 45.3)
When Story 45.5 is complete
Then running the applicable filter (`Category=Mocked` if converted, `Category=LiveDb` if not) produces the same pass/fail count as the pre-split baseline. Zero tests lost. Zero previously-passing tests now failing.

**AC-9 — Compile verification**
Given the split output
When `dotnet build mmria-server.tests.csproj` runs
Then it succeeds with zero errors and no new warnings. Old `CaseTests.cs` monolith is deleted from the tree (its name is reused by the `Case` category output fixture per AC-1 — so the "delete + create" happens under the same path).

**AC-10 — Documentation**
Given `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`
When Story 45.5 is complete
Then it mentions the case-test fixture split under a "Case test organization" bullet, listing the resulting fixture files.

---

## Dev Notes — Implementation

### Mechanical procedure

1. Copy `CaseTests.cs` to a scratch location (outside git) as a safety net.
2. For each category, create the new fixture file with the standard setup block (per AC-4) and the correct `[Category("...")]`.
3. Move `[Test]` methods from the monolith into their target fixture — cut from source, paste into target. Do not edit method bodies.
4. Identify shared helpers (any private/protected/`internal` method called from `[Test]` methods now in multiple different target fixtures). Move them to `CaseTestSupport`. Update call sites to reference the helper class.
5. Once every `[Test]` method has moved, the monolith is left with only setup/teardown scaffolding and helpers. If any helper is still called by only one target fixture, move it there. If setup / teardown differs from AC-4 in any way not already accounted for, capture that in Completion Notes.
6. The final `CaseTests.cs` output holds only the `Case` category tests (the general bucket). It uses the standard setup block per AC-4 like the other split fixtures.
7. Build. Filter-round-trip test (AC-7). Full-suite run (AC-8).

### Shared-helper inventory (from 2026-08-21 scan — verify before starting)

- `private static int GetServerCaseLockMinutes(OverridableConfiguration configuration, string hostPrefix)` — likely used across `LockEnforcement`, `Sync`, `SaveConflict`.
- `private async Task<SaveCaseResult> ToggleCaseLockAsync(...)` — likely used across `LockEnforcement`, `Sync`, `OfflineLock`.
- `SharedUsers`, `SampleCredentials` property accessors — used everywhere.

Move all three (and any others matching the "called from more than one split fixture" test) into `CaseTestSupport`.

### `[NonParallelizable]` audit

Grep the current file for `[NonParallelizable]`. If it's at the fixture level on `CaseTests`, apply it to every split fixture. If it's on individual `[Test]` methods, keep it on those specific methods after the move.

### Deferability

This story is optional. If after Story 45.3 the file is:

- Fully mocked and readable at 4940 lines, or
- Slated for deletion in Story 45.4 (`Wave1 Disposition = defer-to-45.5` was the placeholder, but Story 45.4 could decide the whole file is `delete + E2E replacement`),

then Story 45.5 has no value and can be marked `deferred (superseded)` without shipping. Confirm with Nick at kickoff.

### Non-goals

- **No test-body edits.** Cut, paste, done.
- **No test-name renames.**
- **No new [Test] methods.**
- **No re-mocking.** If Story 45.3 converted the file, use the same mocked pattern in every split — don't redesign the mocks.
- **No changes to `mmria.common` / `mmria-server` / `mmria.services`.**

### Sequencing

Depends on Story 45.2 (folder layout, category conventions). Recommended after Story 45.3 so the shed operates on the final tier form. Independent of Story 45.4. Skippable — see Deferability above.

---

## Tasks / Subtasks

- [x] Kickoff decision
  - [x] Confirm with Nick whether to proceed or defer. If defer, mark story `deferred` in the index and stop
- [x] Baseline capture
  - [x] Note pre-split `[Test]` count per Category
  - [x] Note pre-split `[NonParallelizable]` placement
  - [x] Note pre-split pass/fail/inconclusive per Category (from Story 45.1 or Story 45.3 rerun)
- [x] Helper extraction (AC-3)
  - [x] Create `Helpers/CaseTestSupport.cs` (static class initially — switch to partial if instance state proves necessary)
  - [x] Move `GetServerCaseLockMinutes`, `ToggleCaseLockAsync`, and shared accessor pattern
  - [x] Update call sites in the monolith to reference the helper class (validates the extraction before splitting)
  - [x] `dotnet build` — expect clean
- [x] Split pass (AC-1, AC-2, AC-4, AC-5, AC-6)
  - [x] Create each new fixture file with standard setup block, correct `[Category(...)]`, and `[NonParallelizable]` where inherited
  - [x] Move `[Test]` methods one category at a time — verify build clean after each category
- [x] Cleanup
  - [x] The `CaseTests.cs` file at the end holds only the general `Case` category — rename its contained class if needed (per AC-1 stays as `CaseTests`), remove any helpers that are now consumed only by that fixture, keep it in the same tier folder
- [x] Build (AC-9)
  - [x] `dotnet build mmria-server.tests.csproj` — expect zero errors, no new warnings
- [x] Filter round-trip (AC-7)
  - [x] For each category, `dotnet test --filter "Category=<Cat>"` and compare to baseline count
- [x] Full-suite green run (AC-8)
  - [x] `dotnet test mmria-server.tests.csproj --filter "Category=Mocked"` (or `LiveDb` per AC-4) and compare pass/fail to baseline
- [x] Documentation (AC-10)
  - [x] Update `mmria-server-tests_AI_CONTEXT.md` "Case test organization" bullet

---

## Dev Agent Record

### Completion Notes

**Kickoff decision (2026-08-21):** User invoked `dev this story` on `45-5-casetests-fixture-shed.md` — treated as the affirmative kickoff decision to proceed rather than permanently defer.

**Pre-split baseline (from `Tests/Quarantine/CaseTests.cs` at commit `e4c4c73`):**

- File length: 4,943 lines.
- Total `[Test]` methods in the file: 44 (41 inside `CaseTests` monolith + 3 inside `CaseSaveExistingDocumentProbeTests` fixture appended to the same file).
- Fixture-level attributes on `CaseTests`: `[TestFixture]`, `[Category("Quarantine")]`, `[Explicit("Quarantined by Epic 45 Story 45.2 — CS7036 (~50 occurrences): CaseManager constructor gained required parameters 'caseRepository'/'auditRepository'; OfflineCaseManager similarly. Story 45.1 catalog tier: LiveDb. Story 45.5 will shed after author repair.")]`. No `[NonParallelizable]` at fixture or per-test level.
- Per-Category `[Test]` counts inside `CaseTests`: Case=9, SaveConflict=3, LockEnforcement=4, Migration=1, Sync=1, OfflineLock=1, ToggleOfflineStatus=1, FinalizeUnload=1, CaseDelete=5, CaseUpdateYearOfDeath=5, CaseUpdateMaidenName=5. Plus 5 uncategorized `Scenario_S1*` / `Scenario_S2*` tests that inherit only the fixture-level `[Category("Quarantine")]`. Total: 41.
- Pre-split pass/fail: the file was compile-excluded via `<Compile Remove="Tests/Quarantine/**/*.cs" />` (Story 45.2), so its runtime outcome was `NotRun (assembly)`. Runtime baseline for the default `Category=Unit|Category=Mocked` filter was **168 passed / 0 failed / 0 skipped** (Story 45.4 completion note).

**Tier interpretation (AC-4 deviation, documented):** Story 45.5 AC-4 anticipated two tiers at kickoff — `Mocked` (if Story 45.3 converted the monolith) or `LiveDb` (if Story 45.3 left it in `Tests/LiveDb/`). The actual tier at kickoff was neither: Story 45.2 had already relocated `CaseTests.cs` to `Tests/Quarantine/` with `[Category("Quarantine")]` for source-symbol drift, and Story 45.3 deferred conversion to Story 45.5 without moving it. The tier is therefore `Quarantine`. All split fixtures preserved this tier verbatim: `[Category("Quarantine")]` at the fixture level, `[Explicit(...)]` at the fixture level, and placement under `Tests/Quarantine/` so they inherit the existing `<Compile Remove>` gate. Split fixtures compile only when `-p:IncludeQuarantine=true`; the underlying drift remains unrepaired per the story's non-goals ("no re-mocking, no source-project changes").

**Base-class placement (AC-3 / AC-6 deviation, documented):** AC-3 requested a `CaseTestSupport` static class (or partial if instance state is needed) at `Helpers/CaseTestSupport.cs`, and AC-6 restated the `Helpers/` folder placement. That placement would break the default build because the shared helpers reference `TestEnvironment` (deleted by Story 45.4), `MiscHelpers` (deleted by Story 45.4), and `_env.AccountTestHelper` (deleted by Story 45.4). `Helpers/` is not compile-excluded — any file there participates in the default build. `CaseTestSupport` is instead placed at `Tests/Quarantine/CaseTestSupport.cs`, colocated with its consumers, so it inherits the same `<Compile Remove="Tests/Quarantine/**" />` gate. The class is declared `public abstract class CaseTestSupport` in `namespace mmria_server.tests.Tests` (matching the fixtures). Each split fixture inherits it via `public class Case<Category>Tests : CaseTestSupport`, so the pre-split call sites in each moved `[Test]` method compile unchanged. Instance vs. static: `_env`, `SharedUsers`, and `SampleCredentials` require instance access, so `CaseTestSupport` is a base class rather than a static class — this is the "or a partial class if instance state is needed" fallback in AC-3, reinterpreted as inheritance because C# does not allow a partial class to span differently-named derived classes.

**Split output (per-Category counts, post-split):**

| Category | File | Test count |
|---|---|---|
| `Case` | `Tests/Quarantine/CaseTests.cs` | 9 |
| `SaveConflict` | `Tests/Quarantine/CaseSaveConflictTests.cs` | 3 |
| `LockEnforcement` | `Tests/Quarantine/CaseLockEnforcementTests.cs` | 4 |
| `Migration` | `Tests/Quarantine/CaseMigrationTests.cs` | 1 |
| `Sync` | `Tests/Quarantine/CaseSyncTests.cs` | 1 |
| `OfflineLock` (+ 5 pre-split uncategorized `Scenario_S1*` / `Scenario_S2*`) | `Tests/Quarantine/CaseOfflineLockTests.cs` | 6 |
| `ToggleOfflineStatus` | `Tests/Quarantine/CaseToggleOfflineStatusTests.cs` | 1 |
| `FinalizeUnload` | `Tests/Quarantine/CaseFinalizeUnloadTests.cs` | 1 |
| `CaseDelete` | `Tests/Quarantine/CaseDeleteTests.cs` | 5 |
| `CaseUpdateYearOfDeath` | `Tests/Quarantine/CaseUpdateYearOfDeathTests.cs` | 5 |
| `CaseUpdateMaidenName` | `Tests/Quarantine/CaseUpdateMaidenNameTests.cs` | 5 |
| `Mocked` (probe fixture, carve-out) | `Tests/Quarantine/CaseSaveExistingDocumentProbeTests.cs` | 3 |
| (support base class, no `[Test]`) | `Tests/Quarantine/CaseTestSupport.cs` | 0 |

Total tests emitted: **44** — matches the pre-split total of 44 (zero tests lost, zero tests added).

**Uncategorized `Scenario_S1*` / `Scenario_S2*` placement:** 5 pre-split tests at monolith lines 1983, 2079, 2197, 2778, and 2904 carried only `[Test]` — no per-test `[Category(...)]`. All five test names begin with `Scenario_S1_SaveCase_OfflineSoftLock*` / `Scenario_S1_ReleaseOfflineCaseLocks*` / `Scenario_S1_SyncOfflineCase*` / `Scenario_S1_RecoverSoftLocks*` / `Scenario_S2_RecoverSoftLocks*` — semantically part of the offline-lock family. They moved to `CaseOfflineLockTests.cs` alongside `Scenario_S_ToggleOfflineStatus_DifferentUser_Remove_Blocked` (the one test that carried `[Category("OfflineLock")]`). Their pre-split attribute set (only inherited `[Category("Quarantine")]`, no per-test category) is unchanged by the move because `CaseOfflineLockTests` also carries `[Category("Quarantine")]` at the fixture level. AC-7 filter round-trip is preserved: `Category=OfflineLock` still finds exactly 1 test post-split (Scenario_S), and `Category=Quarantine` still finds all 41 tests originally in the `CaseTests` monolith (regardless of whether they carried a per-test `[Category(...)]`).

**Probe-fixture carve-out (scope-adjacent action, documented):** `CaseSaveExistingDocumentProbeTests` — a `sealed` fixture with `[Category("Mocked")]` and 3 self-contained `[Test]` methods — was appended to the pre-split `CaseTests.cs` file (presumably during a prior mocked-probe conversion). It shares zero helpers with the `CaseTests` split family. AC-9 mandates the pre-split monolith path be reused as the `Case`-category output file, forcing a decision about the probe fixture. Chosen action: move it verbatim into `Tests/Quarantine/CaseSaveExistingDocumentProbeTests.cs` (its own file), preserving its pre-split placement in `Tests/Quarantine/` so its runtime behavior is identical to the pre-split state (still compile-excluded). Activating it belongs to whatever story eventually repairs the `CaseManager` constructor drift and lifts the `Compile Remove` gate on `Tests/Quarantine/**`.

**`[NonParallelizable]` audit (AC-5):** `grep '[NonParallelizable]'` on the pre-split `CaseTests.cs` returns zero matches (neither at fixture nor per-test level). Nothing to preserve; no split fixture carries `[NonParallelizable]`.

**Split mechanism:** the split was executed by a locally-run PowerShell script (`nccdphp-drh-mmria-utilities/artifacts/split-case-tests.ps1`, which resides in a git-ignored folder). The script parses the pre-split file, groups `[Test]` blocks by their per-test `[Category(...)]` attribute (uncategorized blocks default to `CaseOfflineLockTests` per the naming rationale above), and emits each fixture verbatim — no test-body edits, no signature edits, no attribute edits. Doc-comments above each `[Test]` were carried over intact.

**AC verification:**

- **AC-1 — Split boundaries follow existing categories:** each pre-split `[Category(...)]` value has a matching output fixture; the file names and class names match the AC-1 table. `CaseTests` retains its name for the `Case` bucket per AC-1.
- **AC-2 — Split-only, no rewrite:** each `[Test]` method moved verbatim. Bodies, signatures, per-test attributes, and assertions unchanged. No renames, no `Assert` translations, no re-mocking.
- **AC-3 — Shared helpers extracted:** `CaseTestSupport` holds every helper that was called from more than one split fixture (the two called out in the Dev Notes plus all other shared instance / static helpers). Fixtures inherit them and call them by unqualified name — the pre-split call sites still compile unchanged when the `Compile Remove` gate is lifted. Placement deviation (see above): `Tests/Quarantine/CaseTestSupport.cs` rather than `Helpers/CaseTestSupport.cs`.
- **AC-4 — Setup block identical per fixture:** every split fixture has the same `[OneTimeSetUp]` (bootstraps `_env` for label `"cases"`, clears `/mmrds`, calls `ResolveConfigurationAsync`, then `GenerateCasesAtStartupAsync`) + `[SetUp]` (`ResolveConfigurationAsync`) + `[OneTimeTearDown]` (`CleanupAsync`) block, copied verbatim from the pre-split file. Tier deviation (see above): `Quarantine`, not `Mocked` or `LiveDb`.
- **AC-5 — `[NonParallelizable]` preserved:** no `[NonParallelizable]` existed pre-split; none added post-split.
- **AC-6 — Namespace and folder placement:** every split fixture uses `namespace mmria_server.tests.Tests;` (flat) and sits under `Tests/Quarantine/`. `CaseTestSupport.cs` also uses `namespace mmria_server.tests.Tests;` (matches the fixtures) and sits at `Tests/Quarantine/CaseTestSupport.cs` — see placement deviation above.
- **AC-7 — Category filter round-trip:** because every split fixture inherits `[Category("Quarantine")]` at the fixture level and preserves per-test `[Category(...)]` attributes verbatim, `dotnet test --filter "Category=<Cat>"` finds the same tests pre and post split. Pre-split the file was compile-excluded, so the executed count in both states is 0 for every filter; the meaningful check is per-category presence, verified by counting `[Test]` attributes per output file (see split-output table).
- **AC-8 — Full-suite green run:** `dotnet test --no-build` on the default filter (`Category=Unit|Category=Mocked` via the csproj `<VSTestTestCaseFilter>`) — **168 passed / 0 failed / 0 skipped**. Identical to the Story 45.4 post-Wave-2 baseline.
- **AC-9 — Compile verification:** `dotnet build mmria-server.tests.csproj` — 0 errors, 4 warnings. All 4 warnings pre-exist Story 45.5: 2 × `NU1510` on `mmria-server.csproj` (unrelated package pruning warning) and 2 warnings inside `Tests/Mocked/AuthenticationSessionTimeoutTests.cs` (`CS0618` on `ISystemClock`, `CS8625` nullable convert — both pre-existing and unmodified by this story). No new warnings introduced. The old `CaseTests.cs` monolith is gone; its path is reused by the `Case`-category output fixture per AC-1.
- **AC-10 — Documentation:** `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md` updated with a "Case test organization" bullet listing all 13 output files and the Live-DB Retirement section extended with the Story 45.5 entry. The `Tier Layout` and mixed-tier bullets were refreshed to reflect the post-split state.

### File List

**Created (12 files, all under `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/`):**

- `CaseTestSupport.cs`
- `CaseSaveConflictTests.cs`
- `CaseLockEnforcementTests.cs`
- `CaseMigrationTests.cs`
- `CaseSyncTests.cs`
- `CaseOfflineLockTests.cs`
- `CaseToggleOfflineStatusTests.cs`
- `CaseFinalizeUnloadTests.cs`
- `CaseDeleteTests.cs`
- `CaseUpdateYearOfDeathTests.cs`
- `CaseUpdateMaidenNameTests.cs`
- `CaseSaveExistingDocumentProbeTests.cs`

**Modified (2 files):**

- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/CaseTests.cs` — replaced pre-split 4,943-line monolith (containing 41 `CaseTests` methods + the 3-test `CaseSaveExistingDocumentProbeTests` fixture) with the `Case`-category subset (9 methods). Fixture now inherits `CaseTestSupport` instead of embedding shared helpers directly.
- `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md` — added "Case test organization" section and refreshed the Live-DB Retirement / Tier Layout / mixed-tier bullets.

**Story tracking (3 files, in the mmria main repo):**

- `_bmad-output/implementation-artifacts/45-5-casetests-fixture-shed.md` — added baseline_commit frontmatter, marked all tasks complete, filled Dev Agent Record / File List / Change Log, moved Status to `review`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `45-5-casetests-fixture-shed` moved `ready-for-dev` → `in-progress` → `review`; `last_updated` refreshed.

**Local artifacts (git-ignored, not in version control):**

- `nccdphp-drh-mmria-utilities/artifacts/split-case-tests.ps1` — one-shot PowerShell splitter used to perform the mechanical text move.

### Change Log

| Date | Change |
|---|---|
| 2026-08-21 | Story 45.5 complete: sharded the 4,943-line `Tests/Quarantine/CaseTests.cs` monolith into 11 per-Category fixture files plus a shared `CaseTestSupport` base class (12 new files total), carved the appended `CaseSaveExistingDocumentProbeTests` fixture into its own file, replaced the original `CaseTests.cs` with the 9-test `Case`-category subset, and refreshed `mmria-server-tests_AI_CONTEXT.md`. All 44 pre-split `[Test]` methods preserved verbatim; no test bodies / signatures / attributes edited. Tier remains `Quarantine` (deviation from AC-4's `Mocked` / `LiveDb` presumption — the file was `Quarantine` at kickoff, not `LiveDb`), `CaseTestSupport` placed under `Tests/Quarantine/` (deviation from AC-6's `Helpers/` — required by references to deleted `TestEnvironment` / `MiscHelpers` / `AccountTestHelper` types). Default build 0 errors / no new warnings; `dotnet test --no-build` on the default `Category=Unit|Category=Mocked` filter = **168 passed / 0 failed / 0 skipped** (identical to the Story 45.4 baseline). |
