# Multi-Tenant Rebuild Process

- Status: Active
- Scope: Current `mmria-server` startup rebuild flow, manual tenant rebuild flow, and startup summary behavior in multi-tenant mode.
- When to use: Read this before changing startup rebuild behavior, tenant load/rebuild UI behavior, or the startup summary API.
- Last verified: 2026-04-13
- Related docs: [AI Context Index](./AI_CONTEXT.md), [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md)

This document describes the current rebuild behavior after the rebuild system was simplified to a single legacy implementation.

## Current behavior

### `Load` and `Rebuild` are different
- `Load` adds a tenant's configuration into the running process and makes that tenant available to the current pod.
- `Rebuild` starts rebuild work.
- `Rebuild` auto-loads the tenant first if it is not already loaded.
- Manual rebuilds always start fresh. There is no resume mode.

Relevant code:
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - `LoadTenantAsync(...)`
  - `RebuildTenantAsync(...)`

### Startup and manual rebuilds are legacy-only
- Startup and manual rebuilds always use the legacy rebuild executor.
- `mmria-server` now only decides when to queue rebuilds and what startup context to send.
- `mmria.services` owns the actual rebuild execution through the shared rebuild manager and worker.
- There is no active `bulk` or `compatibility` mode anymore.

Relevant code:
- `source-code/mmria/mmria-server/util/c_db_setup.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/MMRIARebuildManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/MMRIARebuildWorker.cs`

### Only one tenant rebuild runs at a time
- Startup rebuild execution is guarded by a single global semaphore.
- One tenant gets the rebuild slot.
- Other startup or manual rebuilds wait until that tenant releases the slot.
- Manual rebuilds do not interrupt an active startup rebuild.

Relevant code:
- `source-code/mmria/mmria-server/util/c_document_sync_all.cs`
  - `s_startup_rebuild_gate`
  - `executeAsync()`
- `source-code/mmria/mmria-server/util/TenantRebuildCoordinator.cs`

### Startup rebuild queueing is process-start configuration
- Startup rebuild queueing is gated by `multi_tenant_db_rebuild`.
- When queueing is enabled, startup rebuild tenants come from `multi_tenant_jurisdictions_rebuild`, with fallback to `multi_tenant_jurisdictions`.
- Startup queue requests now carry `configured_tenants` and `summary_host_prefix` so `mmria.services` does not infer startup scope from its own appsettings.
- Those values are read when the pod starts.
- Updating a ConfigMap or environment variables does not change the current pod's startup rebuild behavior.
- A pod restart is required for startup rebuild to pick up a changed enablement flag or tenant list.

Relevant code:
- `source-code/mmria/mmria-server/Program.cs`
- `source-code/mmria/mmria-server/util/c_db_setup.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Model/MMRIARebuildRequest.cs`

### Supported rebuild tuning keys
- Rebuild tuning is owned by `mmria.services`, not `mmria-server`.
- `startup_rebuild_page_size`
- `startup_rebuild_max_concurrent_tenants`
- `startup_rebuild_batch_delay_ms`
- `startup_rebuild_bulk_write_retry_count`
- `startup_rebuild_bulk_write_retry_delay_ms`
- `startup_rebuild_progress_persist_every_batches`

Keys that are no longer used:
- `startup_rebuild_mode`
- `startup_rebuild_max_parallelism`
- `startup_rebuild_bulk_doc_chunk_size`
- `startup_rebuild_resumed_page_size`

Relevant code:
- `nccdphp-drh-mmria-services/mmria.services/appsettings.json`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/MMRIARebuildManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/MMRIARebuildWorker.cs`

### Runtime tenant resolution is explicit
- `mmria-server` startup now resolves tenant runtime state through `RootRuntimeSettings`, `TenantCatalog`, and `RequestTenantRuntime`.
- The request-scoped compatibility bridge for `ConfigurationSet`, `OverridableConfiguration`, and `DBConfigurationDetail` has been removed from `Program.cs`.
- Request controllers now resolve current-tenant state through `RequestTenantRuntime` and use `TenantCatalog` only for explicit cross-tenant lookups.

Relevant code:
- `source-code/mmria/mmria-server/Program.cs`
- `source-code/mmria/mmria-server/util/RootRuntimeSettings.cs`
- `source-code/mmria/mmria-server/util/TenantCatalog.cs`
- `source-code/mmria/mmria-server/util/RequestTenantRuntime.cs`

## Manual rebuild behavior

### Clicking `Rebuild` for an unloaded tenant is enough
- If a tenant is not loaded yet, `Rebuild` loads it first.
- The tenant is registered with the rebuild coordinator as `queued`.
- Rebuild runs in the background when the slot becomes available.
- `/MultiTenantSetup` manual rebuild requests now send the page's shared summary context so queued, running, and completed state flows back into the same summary table the page polls.
- Manual rebuild callers outside `/MultiTenantSetup` that do not send summary context still fall back to tenant-local summary persistence.

### `Load` is still useful when you want runtime availability without rebuild
- `Load` lets the current pod know about the tenant.
- It does not start rebuild work by itself.

Relevant code:
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`

## Startup summary behavior

### The persisted summary document lives in `db_rebuild/startup-run-summary`
- Startup queue requests provide both `configured_tenants` and `summary_host_prefix`.
- `summary_host_prefix` resolves from `multi_tenant_re_build_src` when present, otherwise from the first configured startup rebuild tenant.
- During rebuild execution, the running tenant updates its summary state in that shared summary location.
- `/MultiTenantSetup` manual rebuilds also update that shared summary location because they now send the current summary host plus the visible tenant set with each manual request.
- Summary persistence happens at the configured cadence, not on every batch.

Relevant code:
- `source-code/mmria/mmria-server/util/c_db_setup.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/MMRIARebuildWorker.cs`

### Persisted summary pruning and API summary reads are different
- When rebuild code persists `startup-run-summary`, it normalizes `tenant_statuses` to the startup request's configured tenant list.
- Tenants not in the configured startup rebuild tenant list are pruned from the persisted startup summary during those writes.
- When the summary API reads data, it merges the persisted startup tenants with currently loaded tenants before returning the response.
- Result:
  - the persisted document is startup-config oriented
  - the API response is runtime oriented

Relevant code:
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - `GetStartupRunSummaryAsync(...)`
  - `CreateStartupRunSummaryDocument(...)`
  - `UpdateSummaryTotals(...)`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIARebuild/Manager/MMRIARebuildWorker.cs`

### Active summary reads prefer cache
- If rebuild reservations are active and an in-memory startup summary cache is available, the summary API returns that cached snapshot first.
- If there is no active cached snapshot, the API falls back to reading `db_rebuild/startup-run-summary`.

Relevant code:
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - `GetStartupRunSummaryAsync(...)`

## Operator guidance

### Add a tenant to the current running pod
1. Use `Rebuild` if you want the tenant loaded and rebuilt.
2. Use `Load` only if you want the tenant available in the current runtime without starting rebuild work.
3. Expect rebuild to queue behind any active tenant rebuild.

### Add a tenant to future startup rebuilds
1. Ensure `multi_tenant_db_rebuild` is `true`.
2. Add the tenant to `multi_tenant_jurisdictions` or `multi_tenant_jurisdictions_rebuild`, depending on whether you want fallback-to-all or an explicit rebuild list.
3. Restart or respin the pod.
4. The new pod will include that tenant in startup rebuild if queueing is enabled.

## Current expectations to preserve
- Keep `Load` and `Rebuild` as separate actions.
- Keep manual rebuilds from interrupting active rebuild execution.
- Keep the single rebuild slot unless there is an explicit design change.
- Keep startup rebuild on the legacy executor unless there is an explicit replacement design.
