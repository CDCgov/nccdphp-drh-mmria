# Story 45.4 — Retire Remaining Live-DB Tests + Playwright E2E Coverage Plan

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.4
**Status:** ready-for-dev
**Date added:** 2026-08-21
**Depends on:** Stories 45.1 (catalog), 45.2 (folder layout), 45.3 (wave 1 conversion) must all be `done`
**Blocks:** none — Epic 45 closeout after 45.4 lands and 45.5 either lands or is deferred
**Source:** 2026-08-21 analyst session with Mary. Nick's direction: "Remove entirely and document possible Playwright/E2E tests to create."

---

## User Story

As a developer / test steward,
I want the residual live-DB tests deleted from `mmria-server.tests`, every scenario worth preserving captured in a Playwright/E2E coverage plan, and the live-DB support infrastructure (`DatabaseTestHelper`, `TestEnvironment`, `PopulateCdcTestEnvironment`) removed —
So that `mmria-server.tests` is finally a pure C# unit + contract test project with no infra dependency, and the mmria team has a clear roadmap for building the missing E2E coverage.

---

## Acceptance Criteria

**AC-1 — Story 45.3 handoff list is the authoritative input**
Given Story 45.3's Completion Notes list of residual `LiveDb` fixtures
When Story 45.4 kicks off
Then that list is copied into `docs/ai/local/mmria-server-tests-catalog.md` under a new "Story 45.4 disposition" column with per-file value:

- `delete` — default. Fixture is retired; scenario is captured in the E2E plan (AC-5).
- `quarantine-explicit` — scenario is genuinely valuable but not expressible in E2E today; move to `Tests/Quarantine/` with `[Explicit]` and Story 45.2's TODO pattern. Rationale must be recorded in the catalog `Notes`.

`Wave1 Disposition = defer-to-45.5` rows (i.e. `CaseTests.cs`) are excluded from Story 45.4 — Story 45.5 owns them.

**AC-2 — Every `delete` file is removed**
Given every catalog row with `Story 45.4 disposition = delete`
When Story 45.4 is complete
Then the file has been removed via `git rm` and the corresponding rows in the E2E coverage plan (AC-5) exist. `Tests/LiveDb/` contains only files still classified as `keep-live-semantic` (if AC-3 chose the quarantine route for them) — see AC-3.

**AC-3 — `Tests/LiveDb/` is emptied**
Given the empty-`Tests/LiveDb/`-folder target
When Story 45.4 is complete
Then `Tests/LiveDb/` contains zero files. Any file that would otherwise remain is either deleted (AC-2) or moved to `Tests/Quarantine/` with `[Explicit]` (AC-1 `quarantine-explicit`). The empty folder is deleted from the working tree.

**AC-4 — Live-DB support infrastructure removed**
Given the test-side helpers whose only remaining consumers were live-DB fixtures
When Story 45.4 is complete
Then these files are deleted:

- `mmria-server.tests/DatabaseTestHelper.cs`
- `mmria-server.tests/Helpers/TestEnvironment.cs`
- `mmria-server.tests/Helpers/PopulateCdcTestEnvironment.cs`

Additionally: `mmria-server.tests/Helpers/AccountTestHelper.cs`, `mmria-server.tests/Helpers/MiscHelpers.cs`, and `mmria-server.tests/Helpers/RepositoryPathResolver.cs` are audited — any file with zero remaining consumers after the deletions and `CaseTests.cs` disposition (per Story 45.5 or a plan-B for 45.5) is also deleted. Any file with surviving consumers is kept and referenced in the AC-6 catalog update.

`TestConfigurationLoader.cs` and `TestCredentialSettings.cs`: keep if any surviving `Mocked` fixture still uses them for non-secret constants; delete otherwise.

**AC-5 — E2E coverage plan document created**
Given every scenario deleted per AC-2
When Story 45.4 is complete
Then `nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md` exists with one row per deleted `[Test]` method, containing:

| Column | Content |
|---|---|
| `Source Fixture` | Pre-delete file name (e.g. `AccountTests.cs`) |
| `Source [Test]` | Test method name |
| `Behavior Under Test` | One-sentence description of the behavior the test verified |
| `Proposed Playwright Test File` | Path under `nccdphp-drh-mmria-utilities/e2e/tests/` (e.g. `account/account-authentication.spec.ts`) |
| `Proposed Test Name` | Descriptive title for the Playwright test |
| `Required Tenant` | `tenant1`, `tenant4`, `cdc`, etc. |
| `Required User Role` | `abstractor`, `cdc_admin`, `jurisdiction_admin`, etc. |
| `Required Seed Data` | Case IDs, user names, or "none — creates its own" |
| `Priority` | `High` \| `Medium` \| `Low` — based on the regression risk of the behavior |

Priority guidance:

- `High` — behavior with observed production incidents, security-sensitive paths (auth, session, authorization), or PRD-mandated features.
- `Medium` — happy-path CRUD and workflow that has a UI entry point.
- `Low` — edge cases with no user-facing impact.

**AC-6 — Catalog closeout**
Given `docs/ai/local/mmria-server-tests-catalog.md`
When Story 45.4 is complete
Then the catalog:

- Reflects the final state — deleted files are marked as such (row retained for history with `Destination = deleted (see e2e-coverage-plan.md row <N>)`, or removed entirely if the catalog is now the source of truth for *surviving* fixtures only).
- Has a closing summary section: pre-epic file/test counts, post-epic file/test counts, total deletions, total quarantined, total converted, total E2E-planned scenarios.

**AC-7 — csproj simplified**
Given `mmria-server.tests/mmria-server.tests.csproj`
When Story 45.4 is complete
Then:

- Any `ProjectReference` whose only consumers were deleted live-DB fixtures is removed. Realistically that means auditing whether `mmria-server` and `mmria.services` cross-repo references are still needed by surviving `Mocked` / `Unit` / `Contract` fixtures. Keep the reference if any surviving file uses it; remove if not.
- Corresponding `Aliases="global,server"` / `Aliases="global,services"` clauses are removed if the underlying reference is removed.
- `mmria.common` reference is kept regardless — all surviving fixtures need it.
- `mmria-tools` reference is kept if any surviving fixture depends on generator code (`mmria.common.Testing.CaseGeneration.*`); removed otherwise.

**AC-8 — `appsettings.local.example.json` trimmed**
Given `mmria-server.tests/appsettings.local.example.json`
When Story 45.4 is complete
Then every field that only existed to support live-DB tests is removed. Kept fields are only those still read by surviving `Mocked` / `Unit` fixtures. `appsettings.local.json` is git-ignored — do not attempt to touch developer copies; document the trimming in the AI context per AC-10.

**AC-9 — Fresh-clone green run (final)**
Given a fresh clone of `nccdphp-drh-mmria-utilities` with no `appsettings.local.json`, no CouchDB pods, no environment variables
When the developer runs `dotnet test mmria-server.tests.csproj` (no filter, using the Story 45.2 default filter)
Then every executed test passes. Zero `Failed`. Zero unexpected `Inconclusive`. The green run count matches the surviving test count minus the `Quarantine` and `LiveDb` (should be zero) categories.

**AC-10 — Documentation and epic header refresh**
Given `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`
When Story 45.4 is complete
Then:

- The "cross-repo test dependencies" bullet is updated to reflect the trimmed `ProjectReference` set (AC-7).
- A "Live-DB retirement" bullet points to `docs/ai/e2e-coverage-plan.md`.
- The "Purpose" section is updated to describe the post-epic reality: no live-DB tests, pure unit + mocked + contract project, E2E coverage owned by `nccdphp-drh-mmria-utilities/e2e/`.
- The three VS Code tasks from Story 45.2 are refreshed if any behavior changed (`test-livedb` may now be a no-op — either remove it or document that it exists only for archaeology).

Additionally: `story-index.md` epic-45 header note is updated if the actual retirement/deletion counts materially differ from the epic-scoping estimate (e.g. if fewer files converted than expected in 45.3 and more had to be quarantined here, note that).

**AC-11 — Playwright test files are NOT written in this story**
Given the E2E coverage plan (AC-5)
When Story 45.4 is complete
Then no `*.spec.ts` files are added under `nccdphp-drh-mmria-utilities/e2e/tests/`. The plan is the deliverable. Execution of the plan is a separate future epic — not scoped here, not committed to a timeline in this story.

---

## Dev Notes — Implementation

### Deletion order (safety)

Do the deletions in this order to avoid intermediate broken builds:

1. Delete `Tests/LiveDb/*.cs` files first (or move to `Tests/Quarantine/` if `quarantine-explicit`).
2. `dotnet build` — expect compile errors only in files that reference the now-orphaned helpers.
3. Delete the orphaned helpers (`DatabaseTestHelper`, `TestEnvironment`, `PopulateCdcTestEnvironment`, ...).
4. `dotnet build` — expect clean build.
5. Update csproj (AC-7). Rebuild. Confirm no accidental removal broke a surviving fixture.
6. Update `appsettings.local.example.json` (AC-8).
7. Full test run per AC-9.

### E2E plan — where the file lives, what section headers

`nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md` structure:

```markdown
# mmria E2E Coverage Plan — Live-DB Test Retirement Follow-up

**Source:** Epic 45 Story 45.4 (2026-08-21). Documents the Playwright coverage that should replace the retired live-DB tests in mmria-server.tests.

## Summary

- Total retired scenarios: <N>
- Priority distribution: High <n_h>, Medium <n_m>, Low <n_l>
- Existing Playwright coverage overlap: ...

## Coverage rows

| # | Source Fixture | Source [Test] | Behavior | Playwright File | Playwright Test Name | Tenant | Role | Seed Data | Priority |
|---|---|---|---|---|---|---|---|---|---|
| 1 | AccountTests.cs | Scenario_A_Authenticate | Session auth over CouchDB with valid creds | account/authentication.spec.ts | logs in with valid creds | tenant1 | abstractor | seeded user | High |
| ... |
```

### E2E overlap audit

Before authoring each row, check the existing Playwright tests under `nccdphp-drh-mmria-utilities/e2e/tests/`. If a scenario is already covered end-to-end there, note it in a `Notes` column (add if useful) and reduce priority. The plan should not double up coverage that already exists.

### Files known to be candidates for `keep-live-semantic` → `quarantine-explicit`

Per the 2026-08-21 scoping session, these are plausible candidates for AC-1's `quarantine-explicit` disposition — but Story 45.3's handoff list is authoritative:

- Any test that exercises real CouchDB view execution ordering.
- Any test that verifies actual `_rev` conflict handling with a real CouchDB (not a mocked 409).
- Any test that exercises `Process_Central_Pull_list` / CDC integration through a live cluster.

Default disposition is `delete` — only quarantine if a specific reason applies and is recorded.

### csproj audit

`mmria-server` cross-repo reference (`ProjectReference` with `Aliases="global,server"`) is likely needed by:

- `AuthenticationSessionTimeoutTests` (uses `mmria.server.util.SessionTimeoutHelper`)
- `DbRebuildTests` (uses `mmria.server.util.StartupRebuildTenantGate` etc.)
- `SecurityScanBatch4Tests` (uses server-side auth internals)

`mmria.services` cross-repo reference (`Aliases="global,services"`) is likely needed by:

- `PopulateCDCInstanceTests` (`extern alias services;` at top of file) — if this test is deleted, the reference may be removable
- `BatchItemProcessorTests`
- Any tests that touch `mmria.services.backup.Backup`

Confirm per-file at deletion time. Do not remove references speculatively.

### Non-goals

- **No Playwright test authoring.** Plan only.
- **No new source-project code.**
- **No new mocked fixtures.** Story 45.3 handled the conversion wave.
- **No `CaseTests.cs` work.** Story 45.5, or a permanent defer.
- **No PRD, no planning-artifact doc.**

### Sequencing

Depends on Stories 45.1, 45.2, 45.3. Blocks nothing — Epic 45 closeout after 45.4 lands and 45.5 either lands or is formally deferred.

---

## Tasks / Subtasks

- [ ] Kickoff
  - [ ] Copy Story 45.3 handoff list into the catalog as "Story 45.4 disposition" column (AC-1)
  - [ ] For each row, confirm `delete` vs `quarantine-explicit` — default is `delete`
- [ ] Deletion pass (AC-2, AC-3)
  - [ ] `git rm` every `delete` row's file
  - [ ] Move every `quarantine-explicit` row into `Tests/Quarantine/` with Story 45.2 attributes
  - [ ] Delete the empty `Tests/LiveDb/` folder
- [ ] Helper cleanup (AC-4)
  - [ ] `git rm DatabaseTestHelper.cs`, `Helpers/TestEnvironment.cs`, `Helpers/PopulateCdcTestEnvironment.cs`
  - [ ] Audit `AccountTestHelper.cs`, `MiscHelpers.cs`, `RepositoryPathResolver.cs`, `TestConfigurationLoader.cs`, `TestCredentialSettings.cs` — delete any with zero surviving consumers
- [ ] Build verification
  - [ ] `dotnet build mmria-server.tests.csproj` — expect clean build
- [ ] csproj audit (AC-7)
  - [ ] Audit `mmria-server` `ProjectReference` — keep or drop based on surviving consumers
  - [ ] Audit `mmria.services` `ProjectReference` — keep or drop
  - [ ] Audit `mmria-tools` `ProjectReference` — keep or drop
  - [ ] Rebuild after each change
- [ ] `appsettings.local.example.json` trim (AC-8)
  - [ ] Remove fields that no surviving `Mocked` / `Unit` fixture reads
- [ ] E2E coverage plan (AC-5)
  - [ ] Create `nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md`
  - [ ] One row per deleted `[Test]` method
  - [ ] Assign priority per the AC-5 guidance
  - [ ] Note overlaps with existing Playwright coverage under `nccdphp-drh-mmria-utilities/e2e/tests/`
- [ ] Catalog closeout (AC-6)
  - [ ] Update `docs/ai/local/mmria-server-tests-catalog.md` — post-epic summary section
- [ ] Documentation refresh (AC-10)
  - [ ] Update `mmria-server-tests_AI_CONTEXT.md` per AC-10
  - [ ] Update `story-index.md` epic-45 header if counts materially shifted
- [ ] Final verification (AC-9)
  - [ ] Fresh clone (or clean `git stash` + `.local.json` remove)
  - [ ] `dotnet test mmria-server.tests.csproj` — expect zero failures, zero unexpected inconclusives
  - [ ] Confirm zero `LiveDb`-categorized tests exist

---

## Dev Agent Record

### Completion Notes

_To be filled by the dev agent. Include final counts: deleted files, deleted `[Test]` methods, quarantined files, E2E rows drafted._

### Change Log

_To be filled by the dev agent._
