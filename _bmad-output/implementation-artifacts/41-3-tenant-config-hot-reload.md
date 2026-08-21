# Story 41.3: Tenant Configuration Hot-Reload

Status: backlog

> **Origin:** Follow-up surfaced by Story 41.2 (per-tenant auth implementation). See [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 #1 and §8. Not blocking 41.1/41.2 delivery but resolves the "restart required to flip `sams:is_enabled`" operational constraint documented at that gate.

## Story

As a system administrator,
I want changes to a tenant's `configuration-master` CouchDB document to take effect on the next request without requiring an mmria-server restart,
so that operators can flip per-tenant flags (SAMS on/off, offline mode, feature toggles) with the same low-friction lifecycle as CouchDB document edits.

## Acceptance Criteria

1. When a tenant's `configuration-master` document is updated in CouchDB (via admin UI, db-redeploy script, or direct `PUT`), the running mmria-server picks up the change and applies it to subsequent requests for that tenant. No restart required.
2. The reload is scoped: only the affected tenant's `OverridableConfiguration` is refreshed. Other tenants' cached configurations remain untouched.
3. The reload is bounded: either a change-feed listener (`_changes?feed=continuous&filter=…`) or a short-interval poll with `_rev` comparison. Latency between CouchDB write and effective on the server: ≤ 30 seconds is acceptable; ≤ 5 seconds is preferred.
4. Failure modes are safe: if the listener disconnects or CouchDB is unreachable, the server continues serving with the last-known-good configuration and logs a warning. No 5xx storm.
5. Startup path is unchanged: `LoadRequiredOverridableConfigurationsAsync` still runs once at startup and populates the initial catalog before the change-feed listener starts.
6. `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` — zero errors.
7. Integration verification: flip `boolean_keys.shared.sams:is_enabled` on tenant1 while the server is running; issue a request within 30 seconds and confirm the new value takes effect without restart.
8. Runbook and [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 #1 are updated to remove the "restart required" note.

## Tasks / Subtasks

- [ ] Design decision: change-feed listener vs periodic poll
  - [ ] Trace how other CouchDB documents (e.g., case documents, metadata) are refreshed on the server today. Match that pattern if one exists.
  - [ ] If no existing pattern: prefer continuous change feed for latency; document the trade-off in the implementation artifact.
- [ ] Wire the reload mechanism into `TenantCatalog`
  - [ ] `TenantCatalog.UpsertOverridableConfiguration` ([TenantCatalog.cs L130-L165](../../source-code/mmria/mmria-server/util/TenantCatalog.cs)) already exists — determine whether it can be reused by the listener or whether a lighter-touch swap-in is safer.
  - [ ] Ensure the reload is thread-safe. `TenantCatalog` is registered as a singleton; concurrent request threads read from it. Prefer `ImmutableDictionary` swap or a `ReaderWriterLockSlim` boundary.
  - [ ] Do NOT swap `RequestTenantRuntime` instances mid-request — each request captures its configuration reference at construction; a swap after construction must not affect the in-flight request.
- [ ] Failure & recovery
  - [ ] Retry the listener with exponential backoff on transient CouchDB failures.
  - [ ] Log-and-continue on config document deserialization failures — never let a bad edit take down the tenant.
- [ ] Test coverage
  - [ ] Unit test the swap logic in isolation.
  - [ ] Integration test: bring up the local multi-tenant environment, flip `sams:is_enabled`, verify the next request sees the new value.
- [ ] Documentation cleanup
  - [ ] Update [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 #1 and §8 to remove the restart requirement.
  - [ ] Update the operations runbook.

## Dev Notes

**Baseline evidence** for the current startup-only behavior lives in [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 #1 (post-41.2 update). `TenantCatalog.UpsertOverridableConfiguration` exists but has no production callers today; wiring it to a change feed is the smallest-diff path.

**Scope boundary:** this story is ONLY about `configuration-master` documents. Case documents, metadata, and other CouchDB-backed data have their own refresh lifecycles — do NOT touch those.

**Non-goal:** admin UI for editing tenant config. This story assumes edits arrive via the existing admin UI or db-redeploy script; it just makes those edits take effect faster.

**Related:** none currently. If a broader "operational hot-reload" epic emerges, this may be folded in.

**Depends on:** none. 41.2 is done and this is optional follow-up work.
