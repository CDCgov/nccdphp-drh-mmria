# Story 45.1 — `mmria-server.tests` Inventory & Classification Catalog

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.1
**Status:** ready-for-dev
**Date added:** 2026-08-21
**Depends on:** none — discovery only, no test bodies touched
**Source:** 2026-08-21 analyst session with Mary. Epic entry in `story-index.md`.

---

## User Story

As a developer / test steward,
I want a single authoritative catalog of every fixture in `mmria-server.tests` — line count, test count, dependency tier, current pass/fail/inconclusive outcome, drift risk, and proposed post-epic destination —
So that Stories 45.2, 45.3, and 45.4 have an agreed-upon inventory and I can trust the state of the suite instead of guessing.

---

## Acceptance Criteria

**AC-1 — Catalog document created**
Given every `*.cs` file under `nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/`
When the developer completes the catalog
Then `nccdphp-drh-mmria-utilities/docs/ai/local/mmria-server-tests-catalog.md` exists with a table containing one row per test file.

**AC-2 — Required columns per row**
Each row records, at minimum:

| Column | Content |
|---|---|
| `#` | Sequential row number (1..N) — referenced from `[Explicit]` messages in Story 45.2 |
| `File` | File name (relative to `Tests/`) |
| `Lines` | Line count of the file |
| `Tests` | Count of `[Test]` + `[TestCase]` methods |
| `Current Category` | Existing `[Category("...")]` values (blank if none) |
| `Tier` | `Unit` \| `Mocked` \| `LiveDb` (see Dev Notes for classification rules) |
| `Outcome` | `Passed` \| `Failed` \| `Inconclusive` \| `NotRun` \| `CompileError` |
| `Drift Risk` | `green` \| `at-risk` \| `broken` |
| `Destination` | `Tests/Unit/` \| `Tests/Mocked/` \| `Tests/LiveDb/` \| `Tests/Quarantine/` \| `delete + E2E replacement` |
| `Extern Alias` | `services` \| `server` \| `both` \| `—` |
| `mmria-tools?` | `yes` \| `no` — references the shared generator library |
| `Cross-repo symbols OK?` | `yes` \| `no (list broken symbols)` |
| `Notes` | Short free-text: reason for tier, seam needed to mock, why quarantined, etc. |

**AC-3 — Full-suite run captured**
Given credentials populated in `mmria-server.tests/appsettings.local.json` and the multi-tenant CouchDB pods running (via the `launch-multi-tenant-dbs-only` task from the main repo)
When the developer runs the full test suite (`dotnet test` with no filter)
Then per-test outcome is captured in the catalog rows (AC-2 `Outcome` column) and the raw `trx` file is saved to `mmria-server.tests/test-output/results/` and referenced by name in the catalog header.

**AC-4 — Tier classification rules applied consistently**
Tier assignment follows these rules (see Dev Notes for evidence patterns):

- `Unit` — no HTTP, no CouchDB. Uses in-memory helpers, hand-rolled fakes (e.g. `FakeCaseRepository`), or `OverridableConfiguration` built from dictionaries.
- `Mocked` — instantiates `CouchDbHttpClient` but wires it to a `RecordingHttpMessageHandler`, `FixedHttpClientFactory`, or equivalent test-owned handler that intercepts all outbound HTTP.
- `LiveDb` — uses `TestEnvironment.BootstrapAsync`, `PopulateCdcTestEnvironment.BootstrapAsync`, `DatabaseTestHelper` (in `IsCouchDbAccessibleAsync` / `TestDatabaseExistsAsync` mode), or otherwise calls `CouchDbHttpClient.ExecuteAsync` against a URL that resolves to a real CouchDB pod.

If a fixture is a legitimate mix (e.g. mostly `Unit` with one live-DB scenario), classify by the dominant tier and note the exception in the `Notes` column.

**AC-5 — Drift risk assigned**
`Drift Risk` is assigned as:

- `green` — file compiles, all its `[Test]` methods pass on the AC-3 run (or `Inconclusive` for the accepted reason of infra unavailability when the run was intentionally infra-less).
- `at-risk` — file compiles and runs but exhibits one or more of: (a) `Assert.Inconclusive` on a code path that infra should have supported, (b) `[Ignore]` / `[Explicit]` attributes, (c) references stale symbol names (renamed classes, deleted methods that resolve only via `dynamic`), (d) exercises a route/endpoint that has been deprecated.
- `broken` — file fails to compile, throws unexpectedly, or fails on a green infra. Also: any fixture that hits an app code path that no longer exists.

**AC-6 — Broken fixtures route to `Quarantine`**
Every row with `Drift Risk = broken` has `Destination = Tests/Quarantine/`. Story 45.2 will use this column to decide moves.

**AC-7 — Cross-repo dependency audit**
Given each fixture uses cross-repo `ProjectReference` symbols from `mmria-server`, `mmria.common`, `mmria.services`, or `mmria-tools`
When the catalog is completed
Then each row records: (a) whether it uses the `services` or `server` extern alias, (b) whether it references `mmria-tools` generator code (`mmria.common.Testing.CaseGeneration.*` counts as `mmria-tools`), (c) whether every referenced cross-repo symbol still exists in its source project on the current baseline commit.

**AC-8 — Summary section**
Given the row table
When the catalog is complete
Then the catalog also contains a summary section at the top with:

- Total file count
- Total `[Test]` method count
- Per-tier subtotal (Unit / Mocked / LiveDb / broken)
- Per-destination subtotal
- List of any test-side helpers slated for deletion in Story 45.4 (`DatabaseTestHelper`, `TestEnvironment`, `PopulateCdcTestEnvironment`, etc. — anything only used by `LiveDb` fixtures)

**AC-9 — Referenced from AI_CONTEXT**
Given `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md`
When Story 45.1 is complete
Then that file has a "Test Catalog" bullet linking to `docs/ai/local/mmria-server-tests-catalog.md` and a one-sentence note explaining that Stories 45.2–45.4 consume the catalog as their scope input.

**AC-10 — Zero test-file modifications**
Given this is a discovery-only story
When Story 45.1 is complete
Then `git diff nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/` shows zero changes. Only the catalog file, the `AI_CONTEXT` link update, and optionally a `test-output/results/*.trx` file are added.

---

## Dev Notes — Implementation

### Output files

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-utilities/docs/ai/local/mmria-server-tests-catalog.md` | **New file.** Full catalog per AC-1..AC-8. |
| `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md` | Add a "Test Catalog" reference bullet (AC-9). |
| `nccdphp-drh-mmria-utilities/mmria-server.tests/test-output/results/*.trx` | Optional. The AC-3 run output. |

### Files to inspect (baseline)

As of the 2026-08-21 scoping session, `mmria-server.tests/Tests/` contains **38 `*.cs` fixtures** with **~412 `[Test]` methods** across **~19,900 lines**. The full baseline list is included below (from the scoping session — verify current state at story start). Confirmed .disabled files were re-enabled prior to this story; there are no `.disabled` fixtures on the current baseline.

<details>
<summary>Baseline file list (2026-08-21, 38 files)</summary>

`AccountDalTests.cs`, `AccountTests.cs`, `AggregateReportTests.cs`, `AuthCookieContractTests.cs`, `AuthenticationSessionTimeoutTests.cs`, `BatchItemProcessorTests.cs`, `CaseCompatibilityOracleCanonicalizerTests.cs`, `CaseGeneratorConfigTests.cs`, `CaseGeneratorNumericValueTests.cs`, `CaseGeneratorPlausibilityTests.cs`, `CaseGeneratorWriterTests.cs`, `CaseRecordIdGeneratorTests.cs`, `CaseSerializationContractTests.cs`, `CaseTests.cs`, `ColdBackupTests.cs`, `ConfigurationTests.cs`, `CvsPdfGenerationTests.cs`, `DbRebuildTests.cs`, `ExportQueueDownloadTests.cs`, `FunctionalIntegrationTests.cs`, `GenerateUniqueRecordIdAsyncTests.cs`, `GetRecordIdReplacementForYearOfDeathAsyncTests.cs`, `HotBackupTests.cs`, `IJEImportTests.cs`, `IjeMessageControllerDuplicateTests.cs`, `JsonRequestBodyContractTests.cs`, `LegacyTenantRebuildTests.cs`, `MemoryLeakTests.cs`, `MultiTenantConfigurationLoaderTests.cs`, `OverdoseReportTests.cs`, `PerformanceFixesTests.cs`, `PopulateCDCInstanceTests.cs`, `RevisionOwnershipContractTests.cs`, `SecurityScanBatch4Tests.cs`, `TenantRuntimeBridgeTests.cs`, `UserTests.cs`, `ValidateRecordIdAndPersistAsyncTests.cs`, `VitalImportCaseWriterTests.cs`

If Story 43.1's `FRACEMappingRuleTests.cs` has landed in `main` before this story starts, add it to the catalog (Tier: `Unit`, calls only `MMRIAServicesHelper` helpers).
</details>

### Tier classification — how to read the code

Use these evidence patterns:

**Tier = `Unit`** if the fixture does not construct `CouchDbHttpClient` at all, or constructs one only via a fake / non-HTTP factory. Signals:
- `new FakeCaseRepository(...)` or similar hand-rolled interface implementations.
- `OverridableConfiguration` built from an in-memory `Dictionary<string, string>` or `IConfiguration`.
- No `using mmria.common.couchdb;` in a way that instantiates a live client.

**Tier = `Mocked`** if the fixture constructs `CouchDbHttpClient` but every outbound HTTP call is intercepted by a test-owned handler. Signals:
- `new HttpClient(new RecordingHttpMessageHandler(...))` or a matching pattern (`FixedHttpClientFactory`, custom `HttpMessageHandler` subclass in the same file).
- `new CouchDbHttpClient(new FixedHttpClientFactory(client))` — the client's `HttpClient` was built on a test handler.
- No call to `TestEnvironment.BootstrapAsync`, `PopulateCdcTestEnvironment.BootstrapAsync`, or `DatabaseTestHelper.IsCouchDbAccessibleAsync`.

**Tier = `LiveDb`** if any of:
- `TestEnvironment.BootstrapAsync(...)` — the fixture calls `Assert.Inconclusive` if CouchDB is unreachable.
- `PopulateCdcTestEnvironment.BootstrapAsync(...)`.
- `new DatabaseTestHelper(...)` where the helper's constructor is used in URL-resolving mode (i.e. not just for `ConfigurationLoader` access).
- Any URL like `http://tenant<n>-couchdb.local:6984` or `http://cdc-couchdb.local:6984` that is passed to `CouchDbHttpClient.ExecuteAsync` without a mock handler in scope.
- `HttpClient` construction via `SimpleHttpClientFactory` (the production factory) without a test handler.

### Cross-repo symbol audit

For AC-7, run:

```powershell
dotnet build c:\repos\nccdphp-drh-mmria-utilities\mmria-server.tests\mmria-server.tests.csproj --nologo -v minimal 2>&1 | Select-String -Pattern "error CS"
```

Any `CS0246` / `CS0117` / `CS1061` / `CS0234` at compile time indicates a broken cross-repo symbol reference. Attribute each error to its owning test file and record in the `Cross-repo symbols OK?` column.

### Drift risk — heuristics beyond the compile check

Sources of `at-risk` classification worth recording:

- Fixture calls a controller route that no longer exists on the current mmria-server.
- Fixture asserts against a JSON shape / field name that has been renamed in the current metadata.
- Fixture instantiates a class (`XxxDAL`, `XxxManager`) whose constructor signature has changed — the test might compile via a base overload but not exercise the intended path.
- Fixture depends on a specific seeded document ID that no longer exists in the reference tenant.

### Test-side helpers to inventory

Additionally list every file in `mmria-server.tests/Helpers/` and `mmria-server.tests/*.cs` (the top-level helpers) with a one-line note about which tier(s) currently consume it. Files to include at minimum:

- `DatabaseTestHelper.cs`
- `TestConfigurationLoader.cs`
- `TestCredentialSettings.cs`
- `Helpers/AccountTestHelper.cs`
- `Helpers/MiscHelpers.cs`
- `Helpers/PopulateCdcTestEnvironment.cs`
- `Helpers/RepositoryPathResolver.cs`
- `Helpers/TestEnvironment.cs`

This inventory feeds AC-8's "helpers slated for deletion" list and Story 45.4's cleanup scope.

### Non-goals

- **No test code changes.** No file moves, no attribute additions, no test-body edits. Story 45.2 does that.
- **No decisions about which tests to delete or convert.** The catalog is descriptive. Stories 45.3 and 45.4 make those calls.
- **No PRD, no planning-artifact doc.** The catalog is the only new artifact.
- **No source-project (mmria.common / mmria-server / mmria.services) changes.**

### Sequencing

Independent — can start immediately. Blocks 45.2, 45.3, 45.4, and 45.5.

---

## Tasks / Subtasks

- [ ] Read source once (baseline)
  - [ ] Confirm the file list matches the epic-header snapshot (38 files, +1 if `FRACEMappingRuleTests.cs` from Story 43.1 has landed)
  - [ ] Confirm neither `CvsPdfGenerationTests.cs.disabled` nor `LegacyTenantRebuildTests.cs.disabled` exists on the working tree
- [ ] Compile-time cross-repo audit (AC-7)
  - [ ] `dotnet build mmria-server.tests.csproj` on a clean workspace; capture any `error CS*` output per file
- [ ] Run the full suite (AC-3)
  - [ ] Ensure `appsettings.local.json` is populated with sensitive credentials
  - [ ] Start the multi-tenant CouchDB pods (`launch-multi-tenant-dbs-only`)
  - [ ] `dotnet test mmria-server.tests.csproj --logger "trx;LogFileName=45-1-baseline.trx"`
  - [ ] Save the trx file to `test-output/results/` (this is already the configured `--results-directory` — verify no extra flag needed)
- [ ] Per-file inventory (AC-1, AC-2, AC-4, AC-5)
  - [ ] For each `Tests/*.cs`, populate all AC-2 columns
  - [ ] Classify Tier per the Dev Notes rules
  - [ ] Assign Drift Risk per the AC-5 taxonomy
  - [ ] Set Destination per AC-6 (broken → Quarantine)
- [ ] Per-file destination for green LiveDb fixtures
  - [ ] For each `LiveDb` fixture with `Drift Risk = green` or `at-risk`, provisionally set `Destination = Tests/LiveDb/` — Stories 45.3 and 45.4 will refine this per-file into `Tests/Mocked/`, `delete + E2E replacement`, or `Tests/Quarantine/`
- [ ] Helpers inventory (AC-8)
  - [ ] Add a "Test-side helpers" table listing every file in `Helpers/` and the top-level test-project helpers, with tier-consumer notes
- [ ] Summary section (AC-8)
  - [ ] Totals: files, tests, per-tier, per-destination
  - [ ] "Helpers slated for deletion in Story 45.4" bullet list
- [ ] Cross-repo AI context (AC-9)
  - [ ] Add a "Test Catalog" bullet in `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md` linking to the catalog
- [ ] Verify (AC-10)
  - [ ] `git diff nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/` shows zero changes
  - [ ] `git status` shows only the catalog file, the AI_CONTEXT update, and optionally the `*.trx`

---

## Dev Agent Record

### Completion Notes

_To be filled by the dev agent._

### Change Log

_To be filled by the dev agent._
