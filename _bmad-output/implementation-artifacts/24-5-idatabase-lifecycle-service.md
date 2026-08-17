# Story 24.5 — Extract `IDatabaseLifecycleService` over `c_db_setup.cs`

**Epic:** 24 — Infrastructure Sync and Database Lifecycle Consolidation (SQL Migration Foundation)
**Story ID:** 24.5
**Status:** done
**Date added:** 2026-07-16
**Depends on:** 24.1
**Source requirements:** epics.md §Epic 24 Story 24.5; project-context.md §2.2

---

## User Story

As a developer,
I want `c_db_setup.cs` to be registered behind an `IDatabaseLifecycleService` interface,
So that the entire system startup database initialization path has a clean SQL migration seam and a SQL implementation can substitute it with schema-migration tooling without touching `Program.cs`.

---

## Acceptance Criteria

**AC-1 — `IDatabaseLifecycleService` defined with startup method signatures**
Given `c_db_setup.cs` in `mmria-server/util/` is currently called directly from `Program.cs` startup code
When this story is complete
Then `IDatabaseLifecycleService` is defined in `mmria-server/` with async method signature(s) matching exactly what `Program.cs` calls on the `c_db_setup` instance at startup; the interface does not expose internal CouchDB helper methods or methods never called from `Program.cs`

**AC-2 — `c_db_setup` implements `IDatabaseLifecycleService`**
Given the interface is defined
When `c_db_setup.cs` is updated
Then `c_db_setup` declares `IDatabaseLifecycleService` in its type signature; no other changes are made to the class — the internal `CouchDbHttpClient.ExecuteAsync` calls, PMSS conditional branches, and MMRIARebuildManager orchestration are completely unchanged

**AC-3 — `Program.cs` resolves via interface**
Given `IDatabaseLifecycleService` is defined and `c_db_setup` implements it
When DI registration is updated
Then `services.AddScoped<IDatabaseLifecycleService, c_db_setup>()` (or `AddSingleton` if that matches the current lifetime of `c_db_setup`) is present in `Program.cs`; `Program.cs` references `IDatabaseLifecycleService` only — the concrete `c_db_setup` type does not appear outside of the DI registration

**AC-4 — PMSS conditional compilation paths unchanged**
Given `c_db_setup.cs` has `#if IS_PMSS_ENHANCED` conditional paths
When the interface is extracted
Then the `IDatabaseLifecycleService` method signatures are identical regardless of compile-time flag; `c_db_setup` continues to branch internally on `IS_PMSS_ENHANCED` exactly as before; no interface member is conditional

**AC-5 — Interface scoped to startup path only**
Given `IDatabaseLifecycleService` is the SQL migration seam for startup database initialization
When the interface is defined
Then it covers only the public methods `Program.cs` calls — it does NOT include design-doc, index, or per-database lifecycle methods; those are on `IDeIdentifiedRepository` and `IReportRepository` (Story 24.2); the scope boundary is documented with a comment in the interface file

**AC-6 — Build passes with identical startup behavior**
Given the build after this story
When verified
Then `mmria-server` builds with zero errors; startup database initialization behavior is identical to pre-change; all existing multi-tenant startup tests (if any) pass

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/IDatabaseLifecycleService.cs` | **CREATE** — interface; confirm exact location with project conventions |
| `source-code/mmria/mmria-server/util/c_db_setup.cs` | **UPDATE** — add `: IDatabaseLifecycleService` to class declaration; no other changes |
| `source-code/mmria/mmria-server/Program.cs` | **UPDATE** — replace direct `c_db_setup` instantiation/call with DI registration + resolution via `IDatabaseLifecycleService` |

**Steps:**
1. Identify every method `Program.cs` calls on `c_db_setup` — these become the interface methods.
2. Define `IDatabaseLifecycleService` with those signatures only.
3. Add `: IDatabaseLifecycleService` to `c_db_setup`'s class declaration.
4. Register and resolve via the interface in `Program.cs`.

**Design notes:**
- This story is deliberately minimal — interface extraction only, zero internal changes to `c_db_setup`. The intent is to establish the SQL migration seam without touching the sensitive startup logic.
- `c_db_setup.cs` is called once at startup per tenant initialization. Confirm the lifetime (scoped vs. singleton vs. transient) in use before choosing the DI lifetime for the registration.
- SQL migration note: a future SQL implementation of `IDatabaseLifecycleService` would trigger EF Core migrations (or equivalent schema tooling), create SQL roles/permissions, and insert seed data. It would NOT call CouchDB at all.
- Do NOT add `IDatabaseLifecycleService` as a dependency to sync/rebuild files. Design-doc and index operations in sync files are owned by `IDeIdentifiedRepository` / `IReportRepository` (Story 24.2), not this service.

---

## Sequencing

Depends on 24.1. Standalone — no other Epic 24 stories depend on it. Can proceed in parallel with 24.2, 24.3, 24.4 once 24.1 is complete. This is the lowest-risk story in Epic 24 — purely additive, zero behavioral change.
