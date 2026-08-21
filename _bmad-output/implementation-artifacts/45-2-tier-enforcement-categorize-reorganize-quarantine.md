# Story 45.2 — Tier Enforcement: Categorize, Reorganize, Quarantine Out-of-Sync Tests

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.2
**Status:** ready-for-dev
**Date added:** 2026-08-21
**Depends on:** Story 45.1 (`docs/ai/local/mmria-server-tests-catalog.md`) must be `done`
**Blocks:** Stories 45.3, 45.4, 45.5
**Source:** 2026-08-21 analyst session with Mary. Open items OI-e45-1 and OI-e45-2 resolve at kickoff.

---

## User Story

As a developer running `mmria-server.tests`,
I want every fixture tagged with a category, sorted into a tier folder, and every drifted test quarantined behind `[Explicit]` —
So that `dotnet test` runs green in a fresh clone with no CouchDB running, and infra-dependent or broken tests only run when I explicitly opt in.

---

## Acceptance Criteria

**AC-1 — Category vocabulary confirmed (OI-e45-2)**
Given the Story 45.1 catalog
When the developer kicks off Story 45.2
Then the final `[Category(...)]` vocabulary is confirmed with Nick. Draft: `Unit`, `Mocked`, `LiveDb`, `Quarantine`. Optional: `Slow`, `Contract`. Any category with a zero row count in the catalog is dropped. The confirmed vocabulary is recorded at the top of the Change Log below.

**AC-2 — Quarantine bucket decision (OI-e45-1)**
Given the count of `broken` rows in the Story 45.1 catalog
When the developer kicks off Story 45.2
Then a decision is captured — either (a) `Tests/Quarantine/` folder within `mmria-server.tests` (default; `[Explicit]` gates prevent accidental execution) or (b) a separate `mmria-server.tests.quarantine` csproj (only if the broken-count is large enough to justify a separate project). Decision goes in the Change Log. Rest of the ACs below assume option (a) — swap folder references to project references if (b) is chosen.

**AC-3 — Fixture-level category attributes**
Given every `[TestFixture]` in `mmria-server.tests/Tests/`
When Story 45.2 is complete
Then each fixture has a fixture-level `[Category("...")]` attribute matching its tier per the catalog. Per-test `[Category]` overrides are added only where a fixture legitimately mixes tiers (documented in the catalog `Notes` column).

**AC-4 — Folder reorganization**
Given the catalog `Destination` column
When Story 45.2 is complete
Then files are moved into these subfolders (created if absent):

- `Tests/Unit/`
- `Tests/Mocked/`
- `Tests/LiveDb/`
- `Tests/Quarantine/`

The `mmria_server.tests.Tests` namespace remains **flat** — do not change `namespace mmria_server.tests.Tests;` to `namespace mmria_server.tests.Tests.Unit;` etc. Namespace-flat preserves any `InternalsVisibleTo` scoping and every existing `using` in cross-file test helpers.

**AC-5 — Broken fixtures quarantined**
Given every catalog row with `Drift Risk = broken`
When Story 45.2 is complete
Then the fixture file has been moved to `Tests/Quarantine/` and has, at fixture level:

- `[Category("Quarantine")]`
- `[Explicit("Quarantined by Epic 45 Story 45.2 — see docs/ai/local/mmria-server-tests-catalog.md row <N>. Drift: <one-line symptom>.")]`

Where `<N>` is the row number in the catalog and `<one-line symptom>` is a short human-readable reason (e.g. `"references deleted CaseManager.LegacyGetCaseAsync"`, `"asserts against renamed route /api/case/legacy-list"`).

A `// TODO(epic-45): ...` single-line comment above the class references the same catalog row.

**AC-6 — `runsettings` defaults exclude `LiveDb` and `Quarantine`**
Given `mmria-server.tests.runsettings`
When Story 45.2 is complete
Then a default `TestCaseFilter` (or NUnit-appropriate equivalent) excludes both `Category=LiveDb` and `Category=Quarantine` on a bare `dotnet test`. Both filters can be overridden by an explicit `--filter` argument or by the VS Code tasks defined in AC-7.

**AC-7 — Three VS Code tasks added**
Given `mmria-server.tests/.vscode/tasks.json` (or the utilities-repo top-level `.vscode/tasks.json` — pick whichever already owns the existing test-run tasks)
When Story 45.2 is complete
Then three tasks exist:

| Task label | Command |
|---|---|
| `test-unit-mocked` | `dotnet test mmria-server.tests.csproj --filter "Category=Unit\|Category=Mocked"` — the default green run |
| `test-livedb` | `dotnet test mmria-server.tests.csproj --filter "Category=LiveDb"` — opt-in, requires the multi-tenant CouchDB pods |
| `test-all-including-quarantine` | `dotnet test mmria-server.tests.csproj` (no filter) — for triage / drift audits |

Each task has a `group: test` entry so it appears in the VS Code task-run menu.

**AC-8 — Default `dotnet test` is green on a fresh clone**
Given a fresh clone of `nccdphp-drh-mmria-utilities` with no `appsettings.local.json`, no CouchDB pods running, no environment variables set
When the developer runs `dotnet test mmria-server.tests.csproj` (no filter)
Then every executed test either passes or is skipped by the AC-6 filter. Zero `Failed`. Zero unexpected `Inconclusive`.

If a `Mocked` or `Unit` fixture requires `appsettings.local.json` fields that don't have safe defaults (e.g. it hard-codes user-name/password strings expected to be non-empty), fix the fixture to use safe defaults **or** re-classify to `LiveDb`. Prefer the fix — the fixture is not truly `Mocked` if it can't run without local credentials.

**AC-9 — Zero test-body changes**
Given every fixture file
When Story 45.2 is complete
Then the `git diff` for each file consists solely of: (a) folder-move rename, (b) added `[Category(...)]` line, (c) for quarantined files only, added `[Explicit(...)]` and `// TODO(epic-45)` comment. No changes to `[Test]` method bodies, no changes to imports/usings, no new fields, no new helpers.

**AC-10 — Documentation refresh**
Given `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`
When Story 45.2 is complete
Then it is updated with:

- New folder layout (`Tests/Unit/`, `Tests/Mocked/`, `Tests/LiveDb/`, `Tests/Quarantine/`)
- Confirmed category vocabulary (from AC-1)
- Description of the three VS Code tasks and when to use each
- Note that `dotnet test` default excludes `LiveDb` and `Quarantine` (AC-6)

**AC-11 — Cross-reference sanity**
Given `InternalsVisibleTo` targets, other test projects, and any AI-context docs that reference these fixture files by path
When Story 45.2 is complete
Then no cross-file reference breaks. The assembly name (`mmria-server.tests`) is unchanged. Nothing in the main mmria repo depends on a specific file path under `Tests/`.

---

## Dev Notes — Implementation

### Move mechanics

Use `git mv` (not raw `mv` / `Move-Item`) so history is preserved on the rename. Example:

```powershell
cd c:\repos\nccdphp-drh-mmria-utilities\mmria-server.tests
mkdir Tests\Unit, Tests\Mocked, Tests\LiveDb, Tests\Quarantine -ErrorAction SilentlyContinue
git mv Tests\GetRecordIdReplacementForYearOfDeathAsyncTests.cs Tests\Unit\
# ... one per file, driven by the catalog Destination column
```

Do the moves in a single commit per tier to keep review focused.

### `runsettings` filter syntax

NUnit under `Microsoft.NET.Test.Sdk` accepts a default filter via `<RunConfiguration><TestCaseFilter>` in the runsettings file:

```xml
<RunSettings>
  <RunConfiguration>
    <TestCaseFilter>TestCategory!=LiveDb&amp;TestCategory!=Quarantine</TestCaseFilter>
  </RunConfiguration>
</RunSettings>
```

Verify the filter shape against the existing `mmria-server.tests.runsettings` — the current file was created before this epic and may use a different top-level element. Prefer editing the existing file over replacing it.

If NUnit-adapter version 4.5 rejects the `TestCategory` axis in `RunConfiguration`, fall back to setting the same filter in each VS Code task's command arguments and document the fallback in the AI context (AC-10). The default green run must still work — do not ship a default that runs live-DB tests.

### `[Explicit]` template for quarantined fixtures

```csharp
// TODO(epic-45): Quarantined — see docs/ai/local/mmria-server-tests-catalog.md row <N>.
[TestFixture]
[Category("Quarantine")]
[Explicit("Quarantined by Epic 45 Story 45.2 — see docs/ai/local/mmria-server-tests-catalog.md row <N>. Drift: <one-line symptom>.")]
public class XxxTests
{
    // existing body unchanged
}
```

`[Explicit]` in NUnit means the test only runs when explicitly named (by filter or by user click in Test Explorer) — it does not run in default suite executions even when its `Category` matches. This is the desired safety net.

### Namespace stays flat — rationale

`mmria-server.tests` uses `AssemblyName=mmria-server.tests` and the source projects use `InternalsVisibleTo("mmria-server.tests")`. As long as the assembly name doesn't change, `InternalsVisibleTo` keeps working regardless of folder layout. Namespace changes would break every cross-file `using` inside the test project. Leave namespaces alone.

### VS Code tasks — location

`.vscode/tasks.json` in a multi-root workspace may live at either the workspace root or under each repo. The existing `mmria-server.tests.tests` tasks (per the workspace `task` inventory) appear to live under the utilities repo's `.vscode/tasks.json`. Confirm at story kickoff; add the three new tasks to whichever file already carries `run-mmria-server-tests-no-build` / `build-mmria-server-tests`. Do not create a duplicate `tasks.json`.

### Files to change

| File | Change |
|------|--------|
| `mmria-server.tests/Tests/*.cs` | Fixture-level `[Category(...)]` on every file. `[Explicit(...)]` on quarantined files. Moved into tier subfolders via `git mv`. |
| `mmria-server.tests/mmria-server.tests.runsettings` | Default `TestCaseFilter` excluding `LiveDb` and `Quarantine` (AC-6). |
| `.vscode/tasks.json` (utilities repo) | Three new tasks per AC-7. |
| `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md` | Documentation refresh per AC-10. |
| `nccdphp-drh-mmria-utilities/docs/ai/local/mmria-server-tests-catalog.md` | Update `Current Category` column post-move if the catalog is re-verified; also record any Story 45.2 kickoff decisions in the catalog header (OI-e45-1, OI-e45-2 outcomes). |

### Non-goals

- **No conversion of live-DB tests to mocked.** Story 45.3.
- **No deletion of live-DB tests or helpers.** Story 45.4.
- **No fixture splitting.** `CaseTests.cs` stays as one file. Story 45.5.
- **No test-body edits, no import changes, no new fields.**
- **No source-project (mmria.common / mmria-server / mmria.services) changes.**

### Sequencing

Depends on Story 45.1. Blocks 45.3, 45.4, and 45.5.

---

## Tasks / Subtasks

- [ ] Kickoff decisions
  - [ ] Confirm category vocabulary with Nick (OI-e45-2) — record in Change Log below
  - [ ] Confirm Quarantine bucket layout with Nick (OI-e45-1) — record in Change Log below
- [ ] Prep
  - [ ] Load the Story 45.1 catalog and confirm `Destination` column is populated for every row
  - [ ] Create `Tests/Unit`, `Tests/Mocked`, `Tests/LiveDb`, `Tests/Quarantine` subfolders (unless the OI-e45-1 decision creates a separate project)
- [ ] Attribute pass (AC-3, AC-5)
  - [ ] Add fixture-level `[Category(...)]` on every `[TestFixture]` per catalog `Tier`
  - [ ] On every `broken` fixture: add `[Category("Quarantine")]`, `[Explicit(...)]`, and a `// TODO(epic-45)` comment referencing the catalog row
- [ ] Move pass (AC-4)
  - [ ] `git mv` each fixture into its `Destination` subfolder
  - [ ] Commit per tier (four commits: unit, mocked, livedb, quarantine) for review clarity
- [ ] `runsettings` update (AC-6)
  - [ ] Confirm the current `runsettings` schema and add / update the default filter
  - [ ] Verify `dotnet test` on a fresh clone (no local config, no pods) runs green
- [ ] VS Code tasks (AC-7)
  - [ ] Confirm which `.vscode/tasks.json` owns the existing test tasks
  - [ ] Add `test-unit-mocked`, `test-livedb`, `test-all-including-quarantine`
- [ ] Green-run verification (AC-8)
  - [ ] Fresh clone (or `git stash` + remove `appsettings.local.json`) and stop CouchDB pods
  - [ ] Run `dotnet test mmria-server.tests.csproj` — expect zero failures, zero unexpected inconclusives
  - [ ] If any `Unit` or `Mocked` fixture requires local infra to run, fix it or reclassify to `LiveDb` before shipping
- [ ] Documentation (AC-10)
  - [ ] Update `mmria-server-tests_AI_CONTEXT.md` — folder layout, categories, tasks, default filter behavior
  - [ ] Update the Story 45.1 catalog with post-move `Current Category` values and the OI decisions in the header
- [ ] Cross-reference audit (AC-11)
  - [ ] `grep -r "mmria-server.tests/Tests/"` across both repos and the E2E folder — confirm no hard-coded fixture paths
  - [ ] Confirm assembly name `mmria-server.tests` is unchanged in the csproj

---

## Dev Agent Record

### Completion Notes

_To be filled by the dev agent. Include here: the OI-e45-1 and OI-e45-2 kickoff decisions verbatim._

### Change Log

_To be filled by the dev agent._
