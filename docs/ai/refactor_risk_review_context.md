# Refactor Risk Review Context

- Status: Active
- Scope: Repo-wide refactor regression hotspots across startup/bootstrap, multi-tenant config, case serialization, offline sync, and rebuild/runtime setup.
- When to use: Read this before broad refactors touching SharedLibraries migration seams, multi-tenant runtime wiring, offline sync, or typed case persistence.
- Last verified: 2026-03-29
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Authentication, Session, and Timeout Context](./authentication_session_timeout.md), [Offline Mode Documentation](./offline_mode.md), [Multi-Tenant Rebuild Process](./multi_tenant_rebuild_process.md), [Controller to SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md)

## What is current today

- The current refactor-risk cluster is now concentrated in multi-tenant rebuild/runtime seams and keeping new request-path work aligned with the final tenant-runtime pattern.
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

### Sliding session timeout refresh now uses the shared tenant-aware resolver

- Current truth: password login, SAMS session creation, and `CustomAuthHandler` sliding refresh now resolve `session_idle_timeout_minutes` through one shared helper instead of mixing tenant-aware reads with fallback-only refresh logic.
- What changed:
  - auth timeout resolution is centralized in a shared server-side helper
  - the sliding refresh path now honors tenant overrides and only falls back to shared config or the code default when needed
  - the nullable OIDC session-timeout read was removed in favor of the same shared resolver
- Residual watchpoints:
  - keep timeout resolution centralized; do not reintroduce ad hoc auth-path reads for `session_idle_timeout_minutes`
  - future auth/session work should validate both login-time lifetime assignment and sliding refresh behavior together
  - older auth helpers that do not define the active session lifetime should still be reviewed carefully if they are revived or expanded

Primary code locations:

- [Shared session timeout resolver](../../source-code/mmria/mmria-server/util/SessionTimeoutHelper.cs)
- [Sliding refresh in `CustomAuthHandler`](../../source-code/mmria/mmria-server/CustomAuthHandler.cs)
- [Password login timeout resolution](../../source-code/mmria/mmria-server/Controllers/AccountController.cs)
- [SAMS session creation timeout resolution](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs)
- [Focused auth timeout regression coverage](../../source-code/mmria/mmria-server.tests/Tests/AuthenticationSessionTimeoutTests.cs)

### `mmria-server` startup now uses explicit tenant runtime services and one DI graph

- Current truth: `mmria-server` startup now resolves tenant runtime state through `RootRuntimeSettings`, `TenantCatalog`, and `RequestTenantRuntime`, and it no longer registers first-tenant singleton fallbacks or builds a second startup service provider.
- What changed:
  - startup now registers the tenant-catalog/runtime abstractions explicitly
  - the old first-tenant singleton shortcuts were removed
  - `MMRIARebuildManager` is explicitly factory-registered to avoid constructor-selection ambiguity
  - the former request-scoped compatibility bridge was removed from `Program.cs` after the request-layer migration finished
- Residual watchpoints:
  - keep new request-path code on `RequestTenantRuntime` and `TenantCatalog`; do not reintroduce direct config injection
  - do not reintroduce ad hoc startup containers or first-tenant singleton shortcuts
  - keep explicit DI registration for `MMRIARebuildManager` while both constructor shapes still exist in `mmria.common`

Primary code locations:

- [Explicit tenant runtime abstractions](../../source-code/mmria/mmria-server/util/RootRuntimeSettings.cs)
- [Tenant catalog resolution](../../source-code/mmria/mmria-server/util/TenantCatalog.cs)
- [Request-scoped tenant runtime](../../source-code/mmria/mmria-server/util/RequestTenantRuntime.cs)
- [Main `mmria-server` startup wiring](../../source-code/mmria/mmria-server/Program.cs)
- [Startup and DI source guards](../../source-code/mmria/mmria-server.tests/Tests/TenantRuntimeBridgeTests.cs)

### Request-layer compatibility bridge has been removed

- Current truth: request controllers now resolve tenant state through `RequestTenantRuntime`, and explicit cross-tenant request lookups use `TenantCatalog`.
- What changed:
  - the scoped compatibility registrations for `ConfigurationSet`, `OverridableConfiguration`, and `DBConfigurationDetail` were removed from `Program.cs`
  - migrated request controllers no longer use `MultiTenantConfigHelper` or raw config-list injection
  - cross-tenant request paths such as revision, audit-recovery, and vitals flows now resolve other tenants through `TenantCatalog`
- Residual watchpoints:
  - do not reintroduce direct config/list injection in new request controllers
  - keep `TenantCatalog` limited to explicit cross-tenant request paths and startup/background resolution
  - the old helper file is now inert and should not be treated as an active implementation surface

Primary code locations:

- [Request-scoped tenant runtime](../../source-code/mmria/mmria-server/util/RequestTenantRuntime.cs)
- [Tenant catalog resolution](../../source-code/mmria/mmria-server/util/TenantCatalog.cs)
- [Main `mmria-server` startup wiring without bridge registrations](../../source-code/mmria/mmria-server/Program.cs)
- [Current request controllers](../../source-code/mmria/mmria-server/Controllers)
- [Request-runtime source guards](../../source-code/mmria/mmria-server.tests/Tests/TenantRuntimeBridgeTests.cs)

### Startup configuration loading now fails fast in both apps

- Current truth: startup configuration loading for `mmria-server` and `mmria.services` now uses strict loader entry points that throw when required tenant/shared/single-tenant config cannot be loaded.
- What changed:
  - `MultiTenantConfigurationLoader` now has strict startup-only load methods for required overridable-config and configuration-set loading
  - `mmria-server` startup now uses those strict methods instead of the older fail-open bulk loaders
  - `mmria.services` now loads its required single-tenant `ConfigurationSet` through the same strict loader and no longer swallows failures into `new ConfigurationSet()`
  - `mmria.services` now initializes actors from the main app provider instead of a second ad hoc container
- Residual watchpoints:
  - keep runtime unknown-tenant handling separate from startup-required config loading
  - if future startup code adds new required config, wire it through the strict startup path rather than the old permissive loaders
  - keep `mmria.services` single-tenant unless there is an explicit design change

Primary code locations:

- [Strict startup loader entry points](../../nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs)
- [Fail-fast `mmria-server` startup loads](../../source-code/mmria/mmria-server/Program.cs)
- [Single-provider, fail-fast `mmria.services` startup](../../nccdphp-drh-mmria-services/mmria.services/Program.cs)
- [Startup loader regression coverage](../../source-code/mmria/mmria-server.tests/Tests/MultiTenantConfigurationLoaderTests.cs)

## Highest-risk findings

### No active high-severity request-time tenant-resolution finding remains from this review

- Current truth: the original bridge-removal and helper-removal objective is now complete in `mmria-server`.
- Ongoing risk is future drift, not an active known incompatibility.
- The main guardrail is simple: new request-path code should use `RequestTenantRuntime`, and only add `TenantCatalog` when a route intentionally resolves another tenant.

## Durable guardrails

- Keep `mmria_case` serializer settings centralized for all typed case reads and writes. Do not let different typed paths invent their own JSON contract.
- Do not use injected fallback configuration when tenant-resolved configuration is already available in the current method.
- Keep `session_idle_timeout_minutes` resolution centralized for active auth/session lifetime paths.
- Do not add new `overridableConfigSets[0]` or `dbConfigSets[0]` shortcuts in multi-tenant mode.
- Prefer fail-fast startup configuration loading over swallowing errors into empty config objects.
- Prefer `RequestTenantRuntime` and `TenantCatalog` for new request-path code instead of direct config/list injection.
- Treat offline sync as contract-sensitive whenever typed case models, date/time converters, or metadata generation change, even though it now shares the centralized contract.

## Verification gaps

- Clean isolated builds completed for `mmria.common`, `mmria-server`, and `mmria.services`.
- Focused `CaseSerializationContractTests` passed, covering scalar time values, legacy array-shaped time values, malformed string tolerance, and canonical typed writeback.
- Focused `AuthenticationSessionTimeoutTests` passed, covering the shared timeout resolver, sliding refresh behavior, and auth-controller timeout wiring guards.
- Repo search confirmed that typed `mmria_case` reads and writes now route through `CaseJsonSerialization`, with no remaining direct typed `JsonConvert.DeserializeObject<mmria_case>(...)` or ad hoc typed `SerializeObject(...)` call sites outside the shared helper.
- Startup source guards were updated to lock in the one-provider startup shape and strict startup loader usage.
- The request-layer compatibility bridge was removed from `Program.cs`, and request-runtime source guards now check for the absence of helper-based tenant resolution and raw config-list injection across controllers and views.
- Remaining limit: the `mmria-server.tests` project build is still flaky in this workspace during cross-project test builds, so the new startup guard tests were updated in source but not fully re-executed in this pass.

## How to use this doc

- Use this file to frame repo-wide refactor risk before broad changes.
- Use feature-specific docs for implementation detail after you identify the hotspot.
- If one of these risks is resolved, update this file in place instead of creating a second competing active note.
- If a future change contradicts this file, verify the live code path and then update the doc pack so the active guidance stays aligned with the implementation.
