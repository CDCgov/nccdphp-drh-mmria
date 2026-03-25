# Multi-Tenant Rebuild Process

- Status: Active
- Scope: Startup rebuild behavior, runtime tenant load and rebuild behavior, and summary-document expectations in multi-tenant mode.
- When to use: Read this before changing tenant load or rebuild behavior in the running application.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md)
This document captures the current multi-tenant rebuild behavior in `mmria-server`, especially when adding a tenant while the server is already running.

## Scope
- Multi-tenant `mmria-server` startup rebuild behavior
- Manual tenant load/rebuild behavior from `MultiTenantSetup`
- Runtime vs startup configuration differences
- Current queueing and summary behavior

## Key rules

### 1. `Load` and `Rebuild` are different actions
- `Load` adds a tenant's configuration into the running process and sets that tenant to `pending`.
- `Rebuild` is the action that actually starts or queues rebuild work.
- `Rebuild` will auto-load the tenant first if it is not already loaded.
- Rebuilds always start fresh. The old startup checkpoint and manual resume behavior has been removed.

Code:
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - `LoadTenantAsync(...)`
  - `RebuildTenantAsync(...)`

## Startup rebuild behavior

### 2. Startup only uses the tenant list known at process start
- Startup rebuild tenants come from `multi_tenant_jurisdictions`.
- That value is read from the running process configuration.
- In OpenShift, updating the ConfigMap does not update the environment variables inside an already-running pod.
- Therefore, changing the ConfigMap alone does not change the current startup rebuild set until the pod is restarted.

Code:
- `source-code/mmria/mmria-server/Program.cs`
- `source-code/mmria/mmria-server/util/c_document_sync_all.cs`
  - `get_configured_tenants()`

### 3. Startup rebuilds are serialized
- Startup rebuild execution is guarded by a single global semaphore.
- Only one tenant rebuild acquires the startup rebuild slot at a time.
- Other startup rebuilds wait for the slot.
- `startup_rebuild_mode` currently supports `bulk`, `compatibility`, and `legacy`.
- `legacy` uses the older page-and-per-document write shape derived from the February 8, 2026 implementation, while still flowing through the current startup gate and summary tracking.

Code:
- `source-code/mmria/mmria-server/util/c_document_sync_all.cs`
  - `s_startup_rebuild_gate`
  - `executeAsync()`

## Manual tenant add/rebuild during an active startup pass

### 4. Clicking `Rebuild` for a new tenant is enough
- If `tenant5` is not loaded yet, clicking `Rebuild` will call `LoadTenantAsync(...)` first.
- The tenant is then registered with the rebuild coordinator and marked `queued`.
- The rebuild runs in the background.

Code:
- `source-code/mmria/mmria-server/util/MultiTenantSetupService.cs`
  - `RebuildTenantAsync(...)`

### 5. Manual rebuild should not interrupt existing rebuilds
- Manual rebuilds and startup rebuilds both flow through `c_document_sync_all.executeAsync()`.
- The same startup rebuild gate is used when the actual document rebuild begins.
- Result: a newly queued tenant should wait its turn instead of interrupting currently running rebuilds.

Practical effect:
- If `tenant1` is running and `tenant2`, `tenant3`, `tenant4`, and `cdc` are already queued from startup, a manual `tenant5` rebuild should queue behind them.

## Summary document behavior

### 6. Startup summary pruning is startup-config based
- The startup summary document is `db_rebuild/startup-run-summary`.
- During summary sync, the code normalizes `configured_tenants` from the running process configuration.
- Tenants not in that configured tenant list are pruned from the configured startup set during startup summary updates.
- The current rebuilding tenant is still written into `tenant_statuses`, even if it is not in `configured_tenants`.

Important implication:
- A manually rebuilt tenant that is not part of the current pod's `multi_tenant_jurisdictions` can still run.
- But it may not be counted in the startup summary totals until the pod is restarted with updated environment variables.

Code:
- `source-code/mmria/mmria-server/util/c_document_sync_all.cs`
  - `sync_startup_run_summary_async(...)`
  - `update_run_summary_totals(...)`

## Recommended operator process

### Scenario: pod is already running and startup rebuild is in progress
If you need to add `tenant5` without restarting the pod:

1. Update the OpenShift ConfigMap if you want future pods/startups to include `tenant5`.
2. Do not expect that change to affect the current running pod.
3. In the current running app, click `Rebuild` for `tenant5`.
4. Expect `tenant5` to auto-load, then queue, then run when the rebuild slot reaches it.
5. Expect startup-summary totals/configured tenant counts to continue reflecting the tenant list known by the current pod.

### Scenario: you want `tenant5` to be part of normal startup behavior
1. Add `tenant5` to `multi_tenant_jurisdictions`.
2. Restart or respin the pod.
3. On the new process, startup rebuild will treat `tenant5` as part of the configured startup set.

## Current expectations to preserve
- Do not auto-start rebuild work from `Load` unless explicitly requested.
- Do not let manual tenant addition interrupt active rebuild execution.
- Only prune startup summary tenants on startup-summary sync, not on summary reads.
- Treat ConfigMap changes as future-process configuration unless the pod is restarted.



