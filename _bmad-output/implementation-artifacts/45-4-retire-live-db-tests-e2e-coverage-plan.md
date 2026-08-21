---
baseline_commit: 7c2aa93a240b0d7921645b83de6c793a8ba02b02
baseline_commit_mmria: f8fba4014565b1ed939355912ad44b4dafc280fa
---

# Story 45.4 — Retire Remaining Live-DB Tests + Playwright E2E Coverage Plan

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.4
**Status:** review
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

- [x] Kickoff
  - [x] Copy Story 45.3 handoff list into the catalog as "Story 45.4 disposition" column (AC-1)
  - [x] For each row, confirm `delete` vs `quarantine-explicit` — default is `delete`
- [x] Deletion pass (AC-2, AC-3)
  - [x] `git rm` every `delete` row's file
  - [x] Move every `quarantine-explicit` row into `Tests/Quarantine/` with Story 45.2 attributes _(no `quarantine-explicit` rows chosen — all 4 residual quarantined LiveDb-tier fixtures went to `delete`)_
  - [x] Delete the empty `Tests/LiveDb/` folder
- [x] Helper cleanup (AC-4)
  - [x] `git rm DatabaseTestHelper.cs`, `Helpers/TestEnvironment.cs`, `Helpers/PopulateCdcTestEnvironment.cs`
  - [x] Audit `AccountTestHelper.cs`, `MiscHelpers.cs`, `RepositoryPathResolver.cs`, `TestConfigurationLoader.cs`, `TestCredentialSettings.cs` — delete any with zero surviving consumers (deleted `AccountTestHelper.cs`, `MiscHelpers.cs`; retained `RepositoryPathResolver.cs`, `TestConfigurationLoader.cs`, `TestCredentialSettings.cs`)
- [x] Build verification
  - [x] `dotnet build mmria-server.tests.csproj` — expect clean build (0 errors, 4 warnings all pre-existing)
- [x] csproj audit (AC-7)
  - [x] Audit `mmria-server` `ProjectReference` — keep (still needed by `AuthenticationSessionTimeoutTests`, `TenantRuntimeBridgeTests`, `SecurityScanBatch4Tests`)
  - [x] Audit `mmria.services` `ProjectReference` — keep (still needed by `VitalImportCaseWriterTests`)
  - [x] Audit `mmria-tools` `ProjectReference` — keep (still needed by `CaseGenerator*Tests` fixtures)
  - [x] Rebuild after each change (single build after helper deletions; no reference removals)
- [x] `appsettings.local.example.json` trim (AC-8)
  - [x] Remove fields that no surviving `Mocked` / `Unit` fixture reads (33-key → 10-key `mmria_settings`; see catalog closeout for full delta)
- [x] E2E coverage plan (AC-5)
  - [x] Create `nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md`
  - [x] One row per deleted `[Test]` method (29 method rows + 10 `[TestCase]` rows collapsed into 2 parameterized specs)
  - [x] Assign priority per the AC-5 guidance
  - [x] Note overlaps with existing Playwright coverage under `nccdphp-drh-mmria-utilities/e2e/tests/`
- [x] Catalog closeout (AC-6)
  - [x] Update `docs/ai/local/mmria-server-tests-catalog.md` — post-epic summary section (Story 45.4 dispositions + Post-Story-45.4 state + Epic 45 closeout summary tables)
- [x] Documentation refresh (AC-10)
  - [x] Update `mmria-server-tests_AI_CONTEXT.md` per AC-10 (Purpose section rewritten, Live-DB Retirement section added, Tier Layout / Quarantine Mechanics / VS Code Tasks sections refreshed)
  - [x] Update `story-index.md` epic-45 header if counts materially shifted (no header detail to refresh; 45.4 bullet removed as story flips to `review`)
- [x] Final verification (AC-9)
  - [x] Fresh clone (or clean `git stash` + `.local.json` remove) — simulated by stashing `appsettings.local.json`
  - [x] `dotnet test mmria-server.tests.csproj` — expect zero failures, zero unexpected inconclusives → **168 passed / 0 failed / 0 skipped**
  - [x] Confirm zero `LiveDb`-categorized tests exist — grep for `[Category("LiveDb")]` returns no active-tier hits

---

## Dev Agent Record

### Completion Notes

**Wave 2 retirement outcome (2026-08-21):** Story 45.3 handoff was authoritative — zero residual `Tests/LiveDb/` fixtures at kickoff. Story 45.4 therefore focused on the 4 previously-`LiveDb`-tier fixtures Story 45.2 had quarantined for drift (`AccountTests`, `IJEImportTests`, `PopulateCDCInstanceTests`, `UserTests`) plus the helper infrastructure that only served them. All 4 fixtures + 5 helpers were `git rm`'d in a single pass. Fresh-clone green run confirmed: **168 passed / 0 failed / 0 skipped** (identical to Story 45.3's post-Wave-1 baseline — no active-tier coverage was lost). The `Tests/LiveDb/` folder was removed from disk.

**Per-AC compliance:**

- **AC-1** — Story 45.3 handoff table (empty residual `LiveDb`) applied to catalog. Story 45.4's disposition column populated in the new `Story 45.4 dispositions` catalog section: all 4 rows (2, 25, 34, 38) chose `delete`. Zero rows chose `quarantine-explicit` — the drift-repair effort for each fixture exceeded the cost of authoring the Playwright equivalent per the e2e coverage plan. `CaseTests.cs` (row 14) excluded per AC-1 last paragraph (defer-to-45.5).
- **AC-2** — All 4 `delete` rows removed via `git rm` (rename detection irrelevant; these are deletions, not moves). Every retired `[Test]` method has a corresponding row in the e2e coverage plan (AC-5). See File List below for the exact set.
- **AC-3** — `Tests/LiveDb/` folder removed from working tree. No files remain in it (was already 0 after Story 45.3). No `quarantine-explicit` route taken, so no residual `Tests/LiveDb/` content is possible.
- **AC-4** — `DatabaseTestHelper.cs`, `Helpers/TestEnvironment.cs`, `Helpers/PopulateCdcTestEnvironment.cs` deleted per the AC-4 explicit list. Additionally, `Helpers/AccountTestHelper.cs` and `Helpers/MiscHelpers.cs` were audited and deleted because their only non-Quarantine consumers were the 4 fixtures being retired; the residual consumers (deferred `CaseTests.cs`, quarantined with `<Compile Remove>`) are latent and do not affect the compiled build. `Helpers/RepositoryPathResolver.cs`, `TestConfigurationLoader.cs`, and `TestCredentialSettings.cs` retained — all three have surviving Unit/Mocked-tier consumers (documented in catalog closeout).
- **AC-5** — E2E coverage plan created at [nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md](../../../nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md). 29 rows for deleted `[Test]` methods + 2 parameterized rows covering 10 embedded `[TestCase]` entries. All 9 required columns populated. 4 rows explicitly flagged as "reauthor as Unit / Mocked" rather than Playwright (see `PopulateCDCInstanceTests` rows P.5, P.8, P.9, P.10 in the plan) — those scenarios are pure predicate / loader-clamp / mocked-HTTP-retry logic that Playwright would be the wrong tool for.
- **AC-6** — Catalog closeout section added at `nccdphp-drh-mmria-utilities/docs/ai/local/mmria-server-tests-catalog.md` with (a) Story 45.4 dispositions table, (b) Helpers retired/retained tables, (c) csproj + appsettings changes summary, (d) Post-Story-45.4 state counts, (e) Epic 45 closeout summary table (pre-epic → post-epic file/test counts, deletions, quarantines, conversions, E2E plans).
- **AC-7** — All three cross-repo `ProjectReference` entries audited and retained. `mmria-server` needed by `AuthenticationSessionTimeoutTests` (Mocked, active) + `TenantRuntimeBridgeTests` (Unit, in Quarantine but still lists as a nominal consumer — see caveat below) + `SecurityScanBatch4Tests` (Mocked, in Quarantine); `mmria.services` needed by `VitalImportCaseWriterTests` (Unit, active); `mmria-tools` needed by every `CaseGenerator*Tests` fixture (active Mocked + Unit). No `Aliases` clauses removed. `mmria.common` retained (universal). Caveat: `TenantRuntimeBridgeTests` and `SecurityScanBatch4Tests` are currently quarantined for drift — if the underlying drift is repaired without re-adding the seams they exercised, the `mmria-server` ProjectReference could become removable in a future story. This is not a Story 45.4 concern.
- **AC-8** — `appsettings.local.example.json` `mmria_settings` block trimmed from 33 keys to 10 (retained: `is_environment_based`, `timer_user_name/password/value`, `multi_tenant_jurisdictions/shared_config_id/template_couchdb_url`, `target_test_tenant`, `case_lock_minutes`). All CDC-instance, PowerBI, geocode, vitals, log-directory, rebuild, IJE-sampling, and metadata-version keys removed. `test_credentials:*` fully retained (drives `TestConfigurationLoaderTests` sensitive-settings assertions). `test_paths:app_repo_root` retained (drives `RepositoryPathResolver`).
- **AC-9** — Fresh-clone green run confirmed: `dotnet build && dotnet test --no-build` after stashing `appsettings.local.json` → **168 passed / 0 failed / 0 skipped**. Identical to the Story 45.3 post-Wave-1 baseline — no active-tier coverage lost. Zero `LiveDb`-categorized tests exist in the assembly (folder removed, no fixture carries `[Category("LiveDb")]`).
- **AC-10** — `mmria-server-tests_AI_CONTEXT.md` refreshed: (a) Purpose section rewritten to reflect the post-epic reality (pure C# unit + mocked project with E2E coverage delegated to Playwright); (b) new "Live-DB Retirement (Epic 45)" section summarizing Wave 1 + Wave 2 + `CaseTests` deferral; (c) Tier Layout `Tests/LiveDb/` bullet marked as removed with a pointer to the E2E plan; (d) VS Code Tasks section notes `test-livedb` is vestigial; (e) Quarantine Mechanics XML snippet refreshed (no longer references deleted helpers). `story-index.md` epic-45 header note has no detail to refresh; the 45.4 bullet was removed as the story flips to `review`.
- **AC-11** — Zero `*.spec.ts` files added under `nccdphp-drh-mmria-utilities/e2e/tests/`. The plan is the deliverable; execution is a future epic per AC-11.

**Fixtures retired (delete disposition) — 4 files, 29 `[Test]` methods + 10 `[TestCase]` rows:**

| File | `[Test]` methods | `[TestCase]` rows |
|------|------------------:|-------------------:|
| `Tests/Quarantine/AccountTests.cs` | 7 | 0 |
| `Tests/Quarantine/IJEImportTests.cs` | 9 | 10 (across 2 tests) |
| `Tests/Quarantine/PopulateCDCInstanceTests.cs` | 10 | 0 |
| `Tests/Quarantine/UserTests.cs` | 3 | 0 |
| **Total** | **29** | **10** |

All 29 `[Test]` methods appear as rows in the E2E coverage plan (25 Playwright-planned, 4 flagged "reauthor as Unit / Mocked" instead).

**Helpers retired (delete disposition) — 5 files:**

- `DatabaseTestHelper.cs` (285 lines)
- `Helpers/TestEnvironment.cs` (192 lines)
- `Helpers/PopulateCdcTestEnvironment.cs` (86 lines)
- `Helpers/AccountTestHelper.cs` (92 lines)
- `Helpers/MiscHelpers.cs` (25 lines)

**Epic 45 status after Story 45.4:**

- Story 45.1: `done` (catalog)
- Story 45.2: `review` (tier enforcement)
- Story 45.3: `review` (Wave 1 conversion)
- Story 45.4: `review` (this story — Wave 2 retirement + E2E plan)
- Story 45.5: `ready-for-dev` (optional — `CaseTests.cs` shed)
- Epic 45 itself remains `in-progress` until Story 45.5 lands or is formally deferred.

### Change Log

| Date | Change |
|---|---|
| 2026-08-21 | Story 45.4 complete: retired 4 quarantined `LiveDb`-tier fixtures + 5 support helpers; removed `Tests/LiveDb/` folder; trimmed `appsettings.local.example.json` `mmria_settings` from 33 keys to 10; created [nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md](../../../nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md) with 29 planned Playwright rows + 10 `[TestCase]` rows collapsed into 2 parameterized specs; refreshed catalog closeout + AI context. Zero source-project changes. Fresh-clone green: 168 passed / 0 failed / 0 skipped (`Category=Unit\|Category=Mocked`). |

### File List

**Modified:**

- `nccdphp-drh-mmria/_bmad-output/implementation-artifacts/45-4-retire-live-db-tests-e2e-coverage-plan.md` (this story)
- `nccdphp-drh-mmria/_bmad-output/implementation-artifacts/sprint-status.yaml`
- `nccdphp-drh-mmria/_bmad-output/implementation-artifacts/story-index.md`
- `nccdphp-drh-mmria-utilities/docs/ai/local/mmria-server-tests-catalog.md`
- `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/mmria-server.tests.csproj`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/appsettings.local.example.json`

**Created:**

- `nccdphp-drh-mmria-utilities/docs/ai/e2e-coverage-plan.md` (E2E coverage plan — AC-5 deliverable)

**Deleted (git rm):**

- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/AccountTests.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/IJEImportTests.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/PopulateCDCInstanceTests.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/UserTests.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/DatabaseTestHelper.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Helpers/TestEnvironment.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Helpers/PopulateCdcTestEnvironment.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Helpers/AccountTestHelper.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Helpers/MiscHelpers.cs`
- `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/LiveDb/` (empty directory removed from disk)
