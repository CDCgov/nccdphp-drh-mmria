# Story 18.5 — Extract `IConfigurationBootstrapLoader` over `MultiTenantConfigurationLoader`

**Epic:** 18 — `_users` and `configuration` Consolidation (SQL Migration Foundation)
**Story ID:** 18.5
**Status:** done
**Date added:** 2026-07-14
**Depends on:** none — fully independent of Stories 18.1–18.4
**Source requirements:** epics.md §Epic 18 Story 18.5; project-context.md §2.2

---

## User Story

As a developer,
I want `MultiTenantConfigurationLoader` to be registered behind an interface in DI,
So that the startup tenant-registry and shared-config loading path can be swapped for a SQL implementation without editing `Program.cs`.

---

## Acceptance Criteria

**AC-1 — IConfigurationBootstrapLoader interface created**
Given `MultiTenantConfigurationLoader` is a concrete class with no interface
When this story is complete
Then `IConfigurationBootstrapLoader` is defined in `mmria.common/couchdb/configuration/IConfigurationBootstrapLoader.cs` with async method signatures matching the public surface of `MultiTenantConfigurationLoader`:
- `LoadOverridableConfigurationsAsync(...)` (line 113)
- `LoadRequiredOverridableConfigurationsAsync(...)` (line 162)
- `LoadConfigurationSetsAsync(...)` (line 213)
- `LoadRequiredConfigurationSetsAsync(...)` (line 259)
- `LoadTenantOverridableConfigurationAsync(...)` (line 303)
- `LoadTenantConfigurationSetAsync(...)` (line 325)

**AC-2 — MultiTenantConfigurationLoader implements the interface**
Given the interface is defined
When `MultiTenantConfigurationLoader` is updated
Then it declares `public class MultiTenantConfigurationLoader : IConfigurationBootstrapLoader`; all public method signatures are unchanged; no behavior changes

**AC-3 — Program.cs uses the interface**
Given `Program.cs` currently calls `new MultiTenantConfigurationLoader(appSettingsConfig)` directly
When this story is complete
Then `IConfigurationBootstrapLoader` is registered in DI and `Program.cs` resolves it through the interface rather than instantiating the concrete class directly

**AC-4 — Internal CouchDB URL construction stays in the concrete class**
Given `MultiTenantConfigurationLoader` constructs `configuration/` URLs internally
When this story is complete
Then the URL construction remains in the concrete class — `IConfigurationBootstrapLoader` exposes only the loading contract, not HTTP or URL mechanics

**AC-5 — TestConfigurationLoader evaluated**
Given `TestConfigurationLoader` in the utilities repo may also use `MultiTenantConfigurationLoader`
When evaluated
Then it is updated to depend on `IConfigurationBootstrapLoader` if the DI context is available; if it instantiates the concrete class for test isolation, that is acceptable and noted with a comment

**AC-6 — Build succeeds**
Given the changes are complete
When `dotnet build` runs for both `mmria-server` and `mmria.common`
Then both build with zero errors; existing `MultiTenantConfigurationLoaderTests` pass without modification

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/couchdb/configuration/IConfigurationBootstrapLoader.cs` | **CREATE** — interface |
| `mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs` | **UPDATE** — add `: IConfigurationBootstrapLoader` declaration |
| `mmria-server/Program.cs` | **UPDATE** — register `IConfigurationBootstrapLoader` in DI; update usage to go through interface |
| `mmria-server.tests` or utilities test project | **CHECK** — evaluate `TestConfigurationLoader` usage |

---

### MultiTenantConfigurationLoader public methods (verified 2026-07-14)

All 6 public async methods to include in the interface:

| Line | Method Signature |
|------|-----------------|
| 113 | `LoadOverridableConfigurationsAsync(...)` → `Task<List<OverridableConfiguration>>` |
| 162 | `LoadRequiredOverridableConfigurationsAsync(...)` → `Task<List<OverridableConfiguration>>` |
| 213 | `LoadConfigurationSetsAsync(...)` → `Task<List<ConfigurationSet>>` |
| 259 | `LoadRequiredConfigurationSetsAsync(...)` → `Task<List<ConfigurationSet>>` |
| 303 | `LoadTenantOverridableConfigurationAsync(...)` → `Task<OverridableConfiguration?>` |
| 325 | `LoadTenantConfigurationSetAsync(...)` → `Task<ConfigurationSet?>` |

Read each method signature in the file to get the exact parameter types before defining the interface.

---

### Program.cs registration pattern

```csharp
// Before (direct instantiation):
var configLoader = new MultiTenantConfigurationLoader(appSettingsConfig);

// After (DI registration + resolve):
services.AddSingleton<IConfigurationBootstrapLoader>(
    sp => new MultiTenantConfigurationLoader(appSettingsConfig));

// Then resolve at startup:
var configLoader = app.Services.GetRequiredService<IConfigurationBootstrapLoader>();
```

Or, if `Program.cs` runs the loading before the DI container is built (common in .NET startup bootstrapping), a simpler approach is:

```csharp
// Keep the instantiation for startup loading but expose via interface type:
IConfigurationBootstrapLoader configLoader = new MultiTenantConfigurationLoader(appSettingsConfig);
```

Read the actual `Program.cs` startup sequence to determine which pattern fits. The key requirement is that the variable is typed as `IConfigurationBootstrapLoader`, not `MultiTenantConfigurationLoader`.

---

### Why this is independent of Stories 18.1–18.4

`MultiTenantConfigurationLoader` reads the `configuration` database at startup to build the in-memory tenant map — this is fundamentally different from runtime CRUD. `IConfigurationBootstrapLoader` is a startup infrastructure interface, not an application repository interface. Stories 18.4 and 18.5 establish two distinct seams:

- **`IConfigurationRepository`** — runtime reads/writes of configuration documents from running controllers and services
- **`IConfigurationBootstrapLoader`** — one-time startup loading of the entire tenant configuration set

A future SQL migration can swap each independently.
