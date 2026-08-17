# Story 18.4 — Define `IConfigurationRepository` and Create `SystemConfigDAL`

**Epic:** 18 — `_users` and `configuration` Consolidation (SQL Migration Foundation)
**Story ID:** 18.4
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** 18.1 (catalog confirms operation set); no dependency on 18.2 or 18.3
**Source requirements:** epics.md §Epic 18 Story 18.4; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IConfigurationRepository` interface over all `configuration` database CRUD,
So that the files currently accessing the configuration database directly can be replaced with interface calls and a SQL migration requires changing only `SystemConfigDAL`.

---

## Acceptance Criteria

**AC-1 — SystemConfig SharedLibraries feature created**
Given no existing `SystemConfig` SharedLibraries feature exists in `mmria.common`
When this story creates one
Then the following structure exists:
```
mmria.common/SharedLibraries/SystemConfig/
  IConfigurationRepository.cs
  DAL/
    SystemConfigDAL.cs
```
`SystemConfigDAL` implements `IConfigurationRepository` and contains all in-scope `configuration` database operations

**AC-2 — SystemConfigDAL contains all in-scope operations**
Given the configuration operations identified in the catalog:
- GET `configuration/{configId}` (returning `ConfigurationSet`)
- GET `configuration/{configId}` (returning raw JSON string)
- PUT `configuration/{configId}` (updating shared config document)
When this story is complete
Then `SystemConfigDAL` has async methods for all three; URL construction uses `$"{dbConfig.url}/configuration/{configId}"` (configuration is not tenant-prefixed — never use `Get_Prefix_DB_Url`)

**AC-3 — _config.cs calls replaced**
Given `_config.cs` lines 89, 120, and 171 — all using `$"{db_config.url}/configuration/{shared_config_id}"`:
- Line 89: GET `OverridableConfiguration` document
- Line 120: GET `OverridableConfiguration` document (second read)
- Line 171: PUT `OverridableConfiguration` document
When this story is complete
Then all three are replaced with the corresponding `IConfigurationRepository` methods; `IConfigurationRepository` is injected into `_configController` via constructor injection

**AC-4 — MMRIAServicesDAL configuration calls replaced**
Given `MMRIAServicesDAL.cs` configuration methods:
- Line 234: `GetConfigurationDocumentJson(string couchDbUrl, string configId)` — raw URL overload
- Line 241: URL `$"{couchDbUrl}/configuration/{configId}"`
- Line 249: `GetConfigurationDocumentAsync(string couchDbUrl, string configId)` — async, raw URL overload
- Line 256: URL `$"{couchDbUrl.TrimEnd('/')}/configuration/{Uri.EscapeDataString(configId)}"`
- Line 269: `GetConfigurationDocumentAsync(DBConfigurationDetail dbConfig, string configId)` — async, typed overload
- Line 279: URL `$"{dbConfig.url.TrimEnd('/')}/configuration/{Uri.EscapeDataString(configId)}"`
When this story is complete
Then these 3 methods (6 lines) are replaced with corresponding `IConfigurationRepository` calls; `IConfigurationRepository` is injected into `MMRIAServicesDAL` via constructor injection

**AC-5 — MultiTenantConfigurationLoader marked out of scope**
Given `MultiTenantConfigurationLoader.cs` contains 6 hits to the `configuration` database at startup
When evaluated
Then it is explicitly marked out of scope in the catalog (startup bootstrap infrastructure, addressed in Story 18.5); no changes are made to `MultiTenantConfigurationLoader` in this story

**AC-6 — DI registration added**
Given `IConfigurationRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `services.AddScoped<IConfigurationRepository, SystemConfigDAL>()` is present; `MMRIAServicesDAL` and `_configController` DI registrations are updated to satisfy the new dependency

**AC-7 — Build succeeds**
Given the changes are complete
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with exit code 0

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/SystemConfig/IConfigurationRepository.cs` | **CREATE** — interface |
| `mmria.common/SharedLibraries/SystemConfig/DAL/SystemConfigDAL.cs` | **CREATE** — implementation |
| `mmria-server/Controllers/_config.cs` | **UPDATE** — inject `IConfigurationRepository`; replace 3 calls |
| `mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs` | **UPDATE** — inject `IConfigurationRepository`; replace 3 methods |
| `mmria-server/Program.cs` | **UPDATE** — register `IConfigurationRepository` as `SystemConfigDAL` |

---

### configuration database URL construction rule

The `configuration` database is a single shared database — it is **never** tenant-prefixed:

```csharp
// Correct
string url = $"{dbConfig.url}/configuration/{configId}";

// Wrong — configuration is not prefixed
// string url = dbConfig.Get_Prefix_DB_Url($"configuration/{configId}");
```

---

### _config.cs call sites (verified 2026-07-14)

All three lines in `_config.cs` use the same pattern:
```csharp
// Lines 89, 120, 171:
string request_string = $"{db_config.url}/configuration/{shared_config_id}";
```

- Line 89: GET — reads an `OverridableConfiguration` document
- Line 120: GET — second read of same document (likely for re-check or different path)
- Line 171: PUT — updates the `OverridableConfiguration` document

Map these to:
- Lines 89, 120 → `IConfigurationRepository.GetConfigurationAsync(shared_config_id, dbConfig)` returning `OverridableConfiguration` or JSON string
- Line 171 → `IConfigurationRepository.PutConfigurationAsync(shared_config_id, json, dbConfig)`

---

### MMRIAServicesDAL configuration methods (verified 2026-07-14)

`MMRIAServicesDAL` has three configuration methods to replace:

| Method | Lines | Overload type | Replace with |
|--------|-------|--------------|-------------|
| `GetConfigurationDocumentJson` | 234, 241 | `(string couchDbUrl, string configId)` — raw URL | `IConfigurationRepository.GetConfigurationJsonAsync(configId, ...)` — see note |
| `GetConfigurationDocumentAsync` | 249, 256 | `(string couchDbUrl, string configId)` — returns `ConfigurationSet` | `IConfigurationRepository.GetConfigurationSetAsync(configId, ...)` |
| `GetConfigurationDocumentAsync` | 269, 279 | `(DBConfigurationDetail dbConfig, string configId)` — returns `ConfigurationSet` | same method, typed overload |

> **Note on raw URL overload:** `GetConfigurationDocumentJson` takes a raw `couchDbUrl` string rather than a `DBConfigurationDetail`. If callers of this method do not have a `DBConfigurationDetail`, `IConfigurationRepository` may need an overload that accepts a raw URL. Read the callers of `GetConfigurationDocumentJson` before finalizing the interface signature.

---

### IConfigurationRepository interface template

```csharp
namespace mmria.common.SharedLibraries.SystemConfig;

public interface IConfigurationRepository
{
    Task<string?> GetConfigurationJsonAsync(string configId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<mmria.common.couchdb.ConfigurationSet?> GetConfigurationSetAsync(string configId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<string?> PutConfigurationAsync(string configId, string configJson, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    // Add any additional methods discovered in the catalog
}
```

Verify `ConfigurationSet` namespace against existing `MMRIAServicesDAL` usages.
