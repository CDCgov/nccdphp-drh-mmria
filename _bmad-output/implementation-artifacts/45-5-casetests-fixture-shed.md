# Story 45.5 — `CaseTests.cs` Fixture Shed _(optional)_

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.5
**Status:** ready-for-dev _(optional — safe to defer permanently)_
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

- [ ] Kickoff decision
  - [ ] Confirm with Nick whether to proceed or defer. If defer, mark story `deferred` in the index and stop
- [ ] Baseline capture
  - [ ] Note pre-split `[Test]` count per Category
  - [ ] Note pre-split `[NonParallelizable]` placement
  - [ ] Note pre-split pass/fail/inconclusive per Category (from Story 45.1 or Story 45.3 rerun)
- [ ] Helper extraction (AC-3)
  - [ ] Create `Helpers/CaseTestSupport.cs` (static class initially — switch to partial if instance state proves necessary)
  - [ ] Move `GetServerCaseLockMinutes`, `ToggleCaseLockAsync`, and shared accessor pattern
  - [ ] Update call sites in the monolith to reference the helper class (validates the extraction before splitting)
  - [ ] `dotnet build` — expect clean
- [ ] Split pass (AC-1, AC-2, AC-4, AC-5, AC-6)
  - [ ] Create each new fixture file with standard setup block, correct `[Category(...)]`, and `[NonParallelizable]` where inherited
  - [ ] Move `[Test]` methods one category at a time — verify build clean after each category
- [ ] Cleanup
  - [ ] The `CaseTests.cs` file at the end holds only the general `Case` category — rename its contained class if needed (per AC-1 stays as `CaseTests`), remove any helpers that are now consumed only by that fixture, keep it in the same tier folder
- [ ] Build (AC-9)
  - [ ] `dotnet build mmria-server.tests.csproj` — expect zero errors, no new warnings
- [ ] Filter round-trip (AC-7)
  - [ ] For each category, `dotnet test --filter "Category=<Cat>"` and compare to baseline count
- [ ] Full-suite green run (AC-8)
  - [ ] `dotnet test mmria-server.tests.csproj --filter "Category=Mocked"` (or `LiveDb` per AC-4) and compare pass/fail to baseline
- [ ] Documentation (AC-10)
  - [ ] Update `mmria-server-tests_AI_CONTEXT.md` "Case test organization" bullet

---

## Dev Agent Record

### Completion Notes

_To be filled by the dev agent. Include per-Category test counts pre/post split._

### Change Log

_To be filled by the dev agent._
