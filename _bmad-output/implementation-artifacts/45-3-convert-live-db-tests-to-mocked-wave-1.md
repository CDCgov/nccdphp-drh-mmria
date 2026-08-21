# Story 45.3 — Convert Live-DB Tests to Mocked (Wave 1: High-Value, Low-Risk)

**Epic:** 45 — `mmria-server.tests` Reliability Uplift & Live-DB Retirement (2026-08-21)
**Story ID:** 45.3
**Status:** ready-for-dev
**Date added:** 2026-08-21
**Depends on:** Stories 45.1 (catalog) and 45.2 (folder layout, categories) must be `done`
**Blocks:** Story 45.4 (retirement of the residual live-DB set)
**Source:** 2026-08-21 analyst session with Mary. Direction: remove live-DB entirely — 45.3 is Wave 1 of that removal.

---

## User Story

As a developer maintaining `mmria-server.tests`,
I want every live-DB test whose intent can be preserved by canned HTTP responses converted to the mocked handler pattern,
So that Wave 1 of the live-DB → mocked migration ships and the residual set (for Story 45.4) is small and unambiguous.

---

## Acceptance Criteria

**AC-1 — Wave 1 scope column added to catalog**
Given the Story 45.1 catalog, refreshed after Story 45.2's folder moves
When Story 45.3 kicks off
Then a new column `Wave1 Disposition` is added with values:

- `convert` — mockable in this wave; test intent expressible as verb + URL + payload assertions against a canned response.
- `keep-live-semantic` — test genuinely exercises CouchDB view execution, `_rev` conflict semantics, replication, or Mango `_find` result ordering. Do not convert. Story 45.4 decides its fate.
- `quarantine` — conversion would require asserting weaker guarantees than the original test. Skip conversion, quarantine per Story 45.2 pattern, note the semantic loss in `Notes`.
- `defer-to-45.5` — for `CaseTests.cs` only — the split under 45.5 handles this file.

**AC-2 — Every `convert` file is converted**
Given every catalog row with `Wave1 Disposition = convert`
When Story 45.3 is complete
Then that fixture:

- Has been moved from `Tests/LiveDb/` to `Tests/Mocked/` via `git mv`.
- Has its fixture-level `[Category("LiveDb")]` replaced with `[Category("Mocked")]`.
- No longer references `TestEnvironment.BootstrapAsync`, `PopulateCdcTestEnvironment.BootstrapAsync`, or `DatabaseTestHelper` in URL-resolving mode. (`TestConfigurationLoader` may still be used, but only for non-secret constants; see AC-6.)
- All outbound HTTP goes through a test-owned handler (`RecordingHttpMessageHandler`, `FixedHttpClientFactory`, or an inline `HttpMessageHandler` subclass).
- Every previous `[Test]` method has a same-named counterpart (or better, same method) that asserts semantically equivalent behavior.

**AC-3 — Reuse the existing mock pattern**
Given fixtures already in `Tests/Mocked/` prior to this story (e.g. `AccountDalTests`, `ColdBackupTests`, `HotBackupTests`, `SecurityScanBatch4Tests`, `RevisionOwnershipContractTests`, `VitalImportCaseWriterTests`, `GenerateUniqueRecordIdAsyncTests`)
When any converted fixture needs a mocked HTTP handler
Then it reuses the same helper types (`RecordingHttpMessageHandler`, `FixedHttpClientFactory`, or their siblings) — do not introduce a new equivalent. If a needed pattern doesn't already exist (e.g. multi-response ordered queues), extract the smallest possible helper into `Helpers/` and reuse it across multiple converted files, don't inline a bespoke handler per file.

**AC-4 — Fresh-clone green run**
Given a fresh clone with no `appsettings.local.json`, no environment variables, and no CouchDB pods running
When the developer runs `dotnet test mmria-server.tests.csproj --filter "Category=Unit|Category=Mocked"`
Then every test in the converted set passes. Zero `Failed`. Zero unexpected `Inconclusive`.

**AC-5 — Semantic equivalence, not weaker**
Given each converted `[Test]` method
When compared to its pre-conversion form
Then the assertions are semantically the same or stronger. If preserving equivalence would require simulating CouchDB behavior beyond simple canned responses (e.g. rev-token bumping, view collation, `_bulk_docs` per-row error handling), do not convert — apply `Wave1 Disposition = quarantine` and note the semantic loss in the catalog `Notes` column, then quarantine per the Story 45.2 pattern.

**AC-6 — No new dependence on shared credentials**
Given every converted fixture
When it runs on a fresh clone
Then it does not require any field of `TestCredentialSettings` to be populated. Any strings previously read from `SharedUsers` / `SampleCredentials` are either replaced with literal constants (e.g. `"test-user"`, `"test-password"`) or the fixture is refactored to derive them from the mock's canned response. Fixtures may still `LoadConfiguredCredentials` if the credentials feed the *request* being asserted, but not if they are only needed because the real CouchDB is contacted.

**AC-7 — `keep-live-semantic` and `quarantine` files unmoved and unmodified**
Given catalog rows with `Wave1 Disposition ∈ { keep-live-semantic, quarantine, defer-to-45.5 }`
When Story 45.3 is complete
Then those files remain in their Story 45.2 locations with their Story 45.2 attributes. `quarantine` files receive the Story 45.2 quarantine treatment (if not already applied) — do not delete them here.

**AC-8 — `CaseTests.cs` is deferred**
Given `CaseTests.cs`
When Story 45.3 is complete
Then it has not been converted or split by this story. Its `Wave1 Disposition = defer-to-45.5`. If its Tier is `LiveDb`, it remains in `Tests/LiveDb/`.

**AC-9 — Zero source-project changes**
Given `mmria-server`, `mmria.common`, `mmria.services`
When Story 45.3 is complete
Then no production code is modified. If a conversion needs a source-side seam (a new interface, a virtual method, a DI parameter), do not add it — apply `Wave1 Disposition = quarantine`, note the missing seam in the catalog `Notes` (e.g. `needs seam: CaseManager.SaveCaseAsync accepts ICaseRepository`), and log the seam as a future story input in the epic header.

**AC-10 — Catalog and AI context refreshed**
Given the Story 45.1 catalog
When Story 45.3 is complete
Then per-row `Wave1 Disposition`, updated `Tier`, and updated `Destination` are reflected. `nccdphp-drh-mmria-utilities/ai/mmria-server-tests_AI_CONTEXT.md` is updated with the post-wave-1 count of `LiveDb` fixtures and a one-line pointer to Story 45.4 for the residual set.

**AC-11 — Story 45.4 handoff summary**
Given the residual `Tests/LiveDb/` set after Wave 1
When Story 45.3 is complete
Then the story's Completion Notes list every remaining `LiveDb` fixture with its `Wave1 Disposition` (`keep-live-semantic` or `defer-to-45.5`). This list is Story 45.4's authoritative input.

---

## Dev Notes — Implementation

### Conversion pattern (reference)

The proven mock pattern is already in the codebase. Reuse it verbatim:

```csharp
var handler = new RecordingHttpMessageHandler(async request =>
{
    string url = request.RequestUri!.ToString();

    if (url.EndsWith("/_all_docs", StringComparison.OrdinalIgnoreCase))
    {
        return CreateJsonResponse(@"{ ""rows"": [ ... ] }");
    }

    if (url.EndsWith("/mmrds/case-123", StringComparison.OrdinalIgnoreCase))
    {
        return CreateJsonResponse(@"{ ""_id"": ""case-123"", ""_rev"": ""1-a"" }");
    }

    throw new InvalidOperationException($"Unexpected request URL: {url}");
});

var client = new HttpClient(handler);
var couchDbClient = new CouchDbHttpClient(new FixedHttpClientFactory(client));
```

Study these files for full working examples before starting conversion:

- `Tests/Mocked/AccountDalTests.cs` — simple session-endpoint assertions.
- `Tests/Mocked/ColdBackupTests.cs` — URL-branching handler with per-doc responses.
- `Tests/Mocked/HotBackupTests.cs` — multi-phase interaction.
- `Tests/Mocked/RevisionOwnershipContractTests.cs` — `_rev` assertion patterns (useful reference for what is / isn't reproducible without a real DB).

### Wave 1 disposition decision tree

For each `LiveDb` fixture, ask in order:

1. Does the fixture assert against CouchDB view collation ordering, `_rev` conflict resolution, `_bulk_docs` partial-failure semantics, or replication behavior? → `keep-live-semantic`.
2. Does the fixture require multiple sequential requests where the *response body of request N+1 depends on the request-N body being persisted* (i.e. not just replayable)? → likely `keep-live-semantic`, unless the sequence is short and can be modeled as a stateful handler that mutates a dictionary. Prefer `keep-live-semantic` if in doubt.
3. Does the fixture only assert HTTP verb + URL + request payload against a canned response? → `convert`.
4. Would converting require a source-side change (new interface, virtual method, DI parameter)? → `quarantine` (do not modify source per AC-9).
5. Is the fixture `CaseTests.cs`? → `defer-to-45.5` unconditionally.

### Files known to be `Mocked` already (do not re-convert)

Per the 2026-08-21 scoping session — these fixtures already follow the mocked pattern and will be in `Tests/Mocked/` after Story 45.2 finishes:

- `AccountDalTests.cs`
- `ColdBackupTests.cs`
- `HotBackupTests.cs`
- `SecurityScanBatch4Tests.cs`
- `RevisionOwnershipContractTests.cs`
- `VitalImportCaseWriterTests.cs`
- `GenerateUniqueRecordIdAsyncTests.cs`
- Likely: `JsonRequestBodyContractTests.cs`, `AuthCookieContractTests.cs`, `CaseSerializationContractTests.cs`, `ValidateRecordIdAndPersistAsyncTests.cs`

Confirm each against the catalog before touching. If any of these is actually `LiveDb`, treat it as an in-scope conversion.

### Files likely to convert in Wave 1 (candidates — subject to catalog audit)

Based on the 2026-08-21 scoping session, these fixtures use `TestEnvironment.BootstrapAsync` but their scenarios read like verb+URL+payload assertions. Confirm each against the catalog before conversion:

- `ConfigurationTests.cs` — reads multi-tenant configuration; response shape is known and canned-friendly.
- `AccountTests.cs`, `UserTests.cs` — account lifecycle assertions; mockable.
- `AggregateReportTests.cs`, `OverdoseReportTests.cs` — report shape assertions.
- `IjeMessageControllerDuplicateTests.cs`, `IJEImportTests.cs` — import path assertions where the CouchDB persistence is a side-effect the test can assert via the mock's request-log instead of via a follow-up GET.
- `ExportQueueDownloadTests.cs` — request-shape assertions.
- `MultiTenantConfigurationLoaderTests.cs` — config-load assertions.
- `TenantRuntimeBridgeTests.cs` — bridge-shape assertions.

Files likely to fall into `keep-live-semantic` or `quarantine`:

- `DbRebuildTests.cs` — rebuild coordinator scenarios; some pure (`Scenario_B_BlankStartupRebuildSubset_FallsBackToConfiguredTenants` is already `Unit`-shaped), others exercise real actor + DB flow.
- `PopulateCDCInstanceTests.cs` — CDC populate flow; multi-request state may push it into `keep-live-semantic`.
- `FunctionalIntegrationTests.cs` — the name says "integration" — likely `keep-live-semantic`.
- `LegacyTenantRebuildTests.cs` — complex rebuild flow.
- `CaseTests.cs` — `defer-to-45.5` unconditionally.

The above lists are **hints, not decisions**. Trust the catalog + decision tree.

### Preserving `TestConfigurationLoader` use

`TestConfigurationLoader` reads from `appsettings.local.example.json` (checked in) and `appsettings.local.json` (git-ignored). If a converted fixture only needs constants from the *example* file (which is committed), leave the loader call in place — it reads without requiring `appsettings.local.json` to exist. If the fixture uses `HasResolvedSensitiveSettings()` gating, remove the gate — a `Mocked` fixture should never call `Assert.Inconclusive` on missing credentials.

### Common gotchas

- `FixedHttpClientFactory` returns the same `HttpClient` for every call — if a converted fixture spins up multiple `CouchDbHttpClient` instances, each still shares the underlying handler. Assertions on request-log ordering must account for this.
- `RecordingHttpMessageHandler` should throw on unmatched URLs — silent fallthrough hides drift when the fixture is later maintained.
- Some `LiveDb` fixtures call `Assert.Inconclusive(...)` in `[SetUp]` if infra is missing. After conversion, that `Assert.Inconclusive` path is dead — remove it entirely rather than leaving the branch.

### Non-goals

- **No source-project changes.** If a seam is missing, quarantine, do not add.
- **No `CaseTests.cs` work.** Story 45.5.
- **No deletion of the residual `LiveDb` set.** Story 45.4.
- **No new mock-support helpers beyond what's already in the codebase**, except a single small reusable one if the pattern needs it (AC-3).
- **No test-body semantic rewrites.** Assertions may shift from response-based to request-log-based (that's the point of the mock), but the behavior under test does not change.

### Sequencing

Depends on Stories 45.1 and 45.2. Blocks Story 45.4. May run in parallel with Story 45.5 (they touch disjoint files — 45.5 only touches `CaseTests.cs` and its split children).

---

## Tasks / Subtasks

- [ ] Catalog refresh
  - [ ] Add `Wave1 Disposition` column to `docs/ai/local/mmria-server-tests-catalog.md`
  - [ ] Populate every row per the AC-1 vocabulary and the Dev Notes decision tree
  - [ ] Set `CaseTests.cs` to `defer-to-45.5` unconditionally
- [ ] Study existing mocked fixtures (~10 minutes)
  - [ ] Confirm the `RecordingHttpMessageHandler` + `FixedHttpClientFactory` pattern
- [ ] Conversion pass (per `convert` row in the catalog)
  - [ ] Replace `TestEnvironment.BootstrapAsync` / `DatabaseTestHelper` / `PopulateCdcTestEnvironment` calls with a per-fixture `RecordingHttpMessageHandler`
  - [ ] Remove `Assert.Inconclusive` credential / infra gates
  - [ ] Replace `SharedUsers` / `SampleCredentials` reads with literal constants unless they feed request assertions
  - [ ] `git mv` file to `Tests/Mocked/`
  - [ ] Change fixture-level `[Category("LiveDb")]` → `[Category("Mocked")]`
  - [ ] Run the fixture in isolation (`dotnet test --filter "FullyQualifiedName~<fixture>"`) — expect green
- [ ] Quarantine pass (per `quarantine` row)
  - [ ] Apply Story 45.2 quarantine pattern (`[Explicit]` + `TODO`) if not already applied
  - [ ] Update catalog `Notes` with why conversion was infeasible (semantic loss / missing seam)
- [ ] Fresh-clone green run (AC-4)
  - [ ] Stash / remove `appsettings.local.json`, stop CouchDB pods
  - [ ] `dotnet test mmria-server.tests.csproj --filter "Category=Unit|Category=Mocked"` — expect zero failures
- [ ] Documentation (AC-10, AC-11)
  - [ ] Refresh catalog `Tier` and `Destination` columns for converted rows
  - [ ] Update `mmria-server-tests_AI_CONTEXT.md` with post-wave-1 `LiveDb` count and pointer to 45.4
  - [ ] Populate the Story 45.4 handoff list in this story's Completion Notes below

---

## Dev Agent Record

### Completion Notes

_To be filled by the dev agent. Must include the Story 45.4 handoff list (AC-11): every remaining `LiveDb` fixture with its `Wave1 Disposition` (`keep-live-semantic` or `defer-to-45.5`)._

### Change Log

_To be filled by the dev agent._
