# Refactor Risk Review Context

- Status: Active
- Scope: Repo-wide refactor regression hotspots across startup/bootstrap, multi-tenant config, case serialization, offline sync, and rebuild/runtime setup.
- When to use: Read this before broad refactors touching SharedLibraries migration seams, multi-tenant runtime wiring, offline sync, or typed case persistence.
- Last verified: 2026-03-29
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Authentication, Session, and Timeout Context](./authentication_session_timeout.md), [Offline Mode Documentation](./offline_mode.md), [Multi-Tenant Rebuild Process](./multi_tenant_rebuild_process.md), [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md)

## What is current today

- The current refactor-risk cluster is concentrated in tenant-aware configuration resolution, startup/bootstrap wiring, and multi-tenant rebuild/runtime seams.
- This file is an active risk map for the current implementation.
- Use feature-specific docs for detailed behavior; use this doc to understand the highest-leverage regression seams before broad changes.

## Recently resolved since initial review

### Typed-case serialization contract is now centralized

- Current truth: typed `mmria_case` reads and writes now use one shared helper instead of ad hoc serializer settings per path.
- What changed:
  - typed case deserialization now routes through a shared compatibility-first helper
  - typed case serialization now routes through the same helper and preserves the canonical scalar date/time JSON contract
  - controller JSON settings still align with the same `TimeOnly` and `DateOnly` converter behavior used by the helper
- Residual watchpoints:
  - keep regression coverage in place because legacy case compatibility still depends on the shared fallback path
  - avoid introducing new typed `mmria_case` JSON call sites that bypass the shared helper
  - raw `JObject` and dictionary mutation paths still exist, but they are intentionally untyped and should not be treated as the typed-case contract

Primary code locations:

- [Shared typed-case serializer/deserializer helper](../../nccdphp-drh-mmria-common/mmria.common/utils/CaseJsonSerialization.cs)
- [Main typed case manager read/write paths](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs)
- [Typed case DAL read/write paths](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs)
- [Controller JSON converter registration](../../source-code/mmria/mmria-server/Program.cs)
- [Focused typed-case serialization regression coverage](../../source-code/mmria/mmria-server.tests/Tests/CaseSerializationContractTests.cs)

### Offline sync now inherits the shared typed-case contract

- Current truth: offline sync still goes through `CaseDAL`, but `CaseDAL` now uses the same centralized typed-case serializer/deserializer as the main case-manager path.
- What changed:
  - the prior mismatch between online typed case handling and offline typed case handling has been removed
  - legacy typed case compatibility and canonical writeback now flow through the same DAL contract used by offline sync
- Residual watchpoints:
  - offline sync is no longer an incompatible path, but it is still contract-sensitive because it depends on `CaseDAL` staying aligned with the shared helper
  - changes to typed models, metadata generation, or date/time converters should continue to be validated against offline reconciliation
  - raw offline lock/update flows that mutate untyped JSON still need separate review when touched

Primary code locations:

- [Offline sync manager loop](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs)
- [Typed case DAL read/write path](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs)
- [Shared typed-case serializer/deserializer helper](../../nccdphp-drh-mmria-common/mmria.common/utils/CaseJsonSerialization.cs)
- [Focused typed-case serialization regression coverage](../../source-code/mmria/mmria-server.tests/Tests/CaseSerializationContractTests.cs)

## Highest-risk findings

### `CustomAuthHandler` sliding timeout refresh uses fallback config

- Current truth: `CustomAuthHandler` resolves tenant-specific configuration up front, but the sliding timeout refresh still reads `session_idle_timeout_minutes` from the injected fallback `_configuration` object.
- Risk: tenant-specific timeout behavior can diverge between login/session creation and sliding refresh.
- Likely regressions:
  - multi-tenant deployments can use the wrong timeout during steady-state authenticated traffic
  - tenants with explicit overrides can behave differently during login vs later API navigation
  - debugging session-expiration issues becomes misleading because the read path is partly tenant-aware and partly fallback-based

Primary code locations:

- [Tenant-aware resolution in `HandleAuthenticateAsync()`](../../source-code/mmria/mmria-server/CustomAuthHandler.cs)
- [Sliding timeout refresh still using fallback config](../../source-code/mmria/mmria-server/CustomAuthHandler.cs)
- [Active timeout guidance doc](./authentication_session_timeout.md)

### `Program.cs` still exposes first-tenant fallback singletons and builds a second service provider

- Current truth: startup still registers `overridableConfigSets[0]` and `dbConfigSets[0]` as singleton fallbacks, and it builds a second actor-specific `ServiceProvider`.
- Risk: broad tenancy refactors can silently keep working in single-tenant or first-tenant scenarios while remaining wrong for later tenants.
- Likely regressions:
  - wrong-tenant configuration can leak into code that uses the fallback singleton instead of `MultiTenantConfigHelper`
  - singleton lifetime assumptions can diverge between the main DI graph and the actor DI graph
  - startup rebuild or actor-created services can behave differently from request-scoped web paths

Primary code locations:

- [First-tenant singleton registrations](../../source-code/mmria/mmria-server/Program.cs)
- [Actor-specific service collection and second service provider](../../source-code/mmria/mmria-server/Program.cs)
- [Runtime rebuild context](../../source-code/mmria/mmria-server/util/MultiTenantSetupService.cs)
- [Current rebuild behavior doc](./multi_tenant_rebuild_process.md)

### `MultiTenantConfigurationLoader` startup path fails open

- Current truth: the startup bulk-load methods call private helpers that create a fresh `CouchDbHttpClient`, swallow load failures, and return empty config objects instead of failing fast.
- Risk: startup can continue with unusable tenant config and surface the failure later as null lookups, missing tenant config, or wrong fallback behavior.
- Likely regressions:
  - config or network errors can look like downstream feature bugs instead of startup/configuration failures
  - startup and runtime tenant-load behavior are now different in important ways
  - tests that only validate happy-path config loading can miss the real failure mode

Primary code locations:

- [Startup bulk-load entry points](../../nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs)
- [Fail-open `GetOverridableConfigurationAsync(...)`](../../nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs)
- [Fail-open `GetConfigurationSetAsync(...)`](../../nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs)
- [Runtime tenant-load path that uses the stricter `TryGet...` helpers](../../source-code/mmria/mmria-server/util/MultiTenantSetupService.cs)

## Durable guardrails

- Keep `mmria_case` serializer settings centralized for all typed case reads and writes. Do not let different typed paths invent their own JSON contract.
- Do not use injected fallback configuration when tenant-resolved configuration is already available in the current method.
- Do not add new `overridableConfigSets[0]` or `dbConfigSets[0]` shortcuts in multi-tenant mode.
- Prefer fail-fast startup configuration loading over swallowing errors into empty config objects.
- Treat offline sync as contract-sensitive whenever typed case models, date/time converters, or metadata generation change, even though it now shares the centralized contract.

## Verification gaps

- Clean builds completed for `mmria.common`, `mmria-server`, `mmria.services`, and `mmria-server.tests`.
- Focused `CaseSerializationContractTests` passed, covering scalar time values, legacy array-shaped time values, malformed string tolerance, and canonical typed writeback.
- Repo search confirmed that typed `mmria_case` reads and writes now route through `CaseJsonSerialization`, with no remaining direct typed `JsonConvert.DeserializeObject<mmria_case>(...)` or ad hoc typed `SerializeObject(...)` call sites outside the shared helper.
- Remaining limit: this verification validates the typed-case contract path specifically, not the entire test suite or every raw JSON mutation path.

## How to use this doc

- Use this file to frame repo-wide refactor risk before broad changes.
- Use feature-specific docs for implementation detail after you identify the hotspot.
- If one of these risks is resolved, update this file in place instead of creating a second competing active note.
- If a future change contradicts this file, verify the live code path and then update the doc pack so the active guidance stays aligned with the implementation.
