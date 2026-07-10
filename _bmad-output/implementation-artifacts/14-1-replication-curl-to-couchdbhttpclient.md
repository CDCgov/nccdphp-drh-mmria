# Story 14.1 — Replication: Replace cURL with CouchDbHttpClient

**Epic:** 14 — HTTP Client Modernization (Replication)
**Story ID:** 14.1
**Status:** not-started
**Date added:** 2026-07-08
**PRD ref:** FR-16

---

## User Story

As a developer maintaining the Replication tool,
I want all HTTP calls to go through `mmria.common.getset.CouchDbHttpClient` instead of the local `cURL` class,
So that the Replication project uses the same tested, DI-managed HTTP layer as mmria-server, mmria-services, and the data-migration tool, and `cURL.cs` can be deleted.

---

## Acceptance Criteria

**AC-1 — `mmria.common` project reference added to `replicate.csproj`**
Given the refactored `replicate.csproj`
When the developer opens it
Then it contains a `ProjectReference` to `mmria.common.csproj`
And `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Http` packages are present

**AC-2 — `ServiceProvider` built in `Program.cs` and `CouchDbHttpClient` resolved**
Given the refactored `Program.cs`
When it runs
Then a `ServiceCollection` is constructed, `AddHttpClient()` and `CouchDbHttpClient` singleton are registered, and a `ServiceProvider` is built before any HTTP call is made
And the resolved `CouchDbHttpClient` instance is passed to `Utils`, `ConfigurationManager` (a.k.a. `OverridableConfiguration`), and `Role_Replication` constructors

**AC-3 — `CouchDbHttpClient` replaces every `new cURL(...)` call in `OverridableConfiguration.cs`**
Given the refactored `OverridableConfiguration.cs` (`ConfigurationManager`)
When the developer searches for `new cURL`
Then no matches exist
And all two CouchDB calls use `await _couchDbHttpClient.ExecuteAsync(method, url, payload, userName, password)` with the same method, URL, payload, and credentials as before

**AC-4 — `CouchDbHttpClient` replaces every `new cURL(...)` call in `utils.cs`**
Given the refactored `utils.cs` (`Utils`)
When the developer searches for `new cURL`
Then no matches exist
And the `get_revision` CouchDB GET call uses `await _couchDbHttpClient.ExecuteAsync("GET", url, null, config_timer_user_name, config_timer_value)`

**AC-5 — `CouchDbHttpClient` replaces all `new cURL(...)` calls in `Program.cs` (CouchDB-credentialed)**
Given the refactored `Program.cs`
When the developer searches for `new cURL`
Then no CouchDB-credentialed cURL calls remain
The following call patterns are fully replaced (non-exhaustive list of types):
- Replication POSTs (to `/_replicate`) with `env_username`/`env_password` or `config_timer_user_name`/`config_timer_value`
- User GET/PUT operations in `_users` databases
- Config document GET/PUT operations (`config_db_get_curl`, `config_db_put_curl`)
- Design document PUT and index POST operations
- `clear_history_curl` DELETE calls
- `delete_report_curl` DELETE calls

**AC-6 — All unauthenticated external API calls (`null, null` credentials) migrated to `CouchDbHttpClient`**
Given the refactored `Program.cs`
When the developer searches for `new cURL`
Then no matches remain — including:
- `image_tag_release_curl`, `image_tag_commit_curl`, `image_push_curl` (image tag lookups)
- `redeploy_curl` calls
- `trivy_curl`, `twistlock_scan_curl` calls
- `scale_to_zero_curl`, `scale_to_one_curl` calls
- `pause_rollout_curl`, `resume_rollout_curl` calls
- `environment_update_curl` calls
All replaced with `await _couchDbHttpClient.ExecuteAsync("GET", url, null, null, null)`

**AC-7 — `cURL.cs` deleted from the project**
Given all call sites have been migrated
When the developer lists the project files
Then `Replication/cURL.cs` does not exist

**AC-8 — Project builds without errors after migration**
Given all changes are applied
When `dotnet build` is run against `replicate.csproj`
Then the build exits with code 0 and zero errors

**AC-9 — No behavior change**
Given the migrated project connects to the same URLs with the same credentials and payloads
When a replication run executes
Then it behaves identically to before — same replication POSTs, same user synchronization, same design document seeding
And no replication logic, jurisdiction list processing, or environment configuration is modified

---

## Dev Notes — Implementation Guide

### Changes to `replicate.csproj`

Add project reference:
```xml
<ItemGroup>
  <ProjectReference Include="C:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-common\mmria.common\mmria.common.csproj" />
</ItemGroup>
```

Add packages:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
```

### DI wiring in `Program.cs` `Main()`

Add before the main run logic (after config and credential variables are assigned):

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddHttpClient();
services.AddSingleton<mmria.common.getset.CouchDbHttpClient>();
var serviceProvider = services.BuildServiceProvider();
var couchDbHttpClient = serviceProvider.GetRequiredService<mmria.common.getset.CouchDbHttpClient>();
```

### Call-site replacement pattern

```csharp
// BEFORE:
var replication_curl = new cURL("POST", null, replication_url, replicate_struct_string, env_username, env_password);
await replication_curl.executeAsync();

// AFTER:
await couchDbHttpClient.ExecuteAsync("POST", replication_url, replicate_struct_string, env_username, env_password);

// BEFORE (unauthenticated):
var image_tag_release_curl = new cURL("GET", null, image_tag_release_url, null, null, null);
var image_tag_release_result = await image_tag_release_curl.executeAsync();

// AFTER:
var image_tag_release_result = await couchDbHttpClient.ExecuteAsync("GET", image_tag_release_url, null, null, null);
```

Note: `cURL(method, headers, url, payload, username, password)` → `ExecuteAsync(method, url, payload, userName, password)`. The `headers` parameter (always `null`) is dropped; positional order of `url` and `payload` is preserved.

### Classes that need `CouchDbHttpClient` constructor parameter

| File | Class | Current credential source |
|------|-------|--------------------------|
| `OverridableConfiguration.cs` | `ConfigurationManager` | `IConfiguration` object passed to constructor |
| `utils.cs` | `Utils` | `config_timer_user_name`, `config_timer_value` constructor params |
| `Role_Replication.cs` | `Role_Replication` | `config_timer_user_name`, `config_timer_value` constructor params (currently unused — method body is commented out) |

```csharp
// Example — Utils.cs:
public class Utils
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    string config_timer_user_name;
    string config_timer_value;

    public Utils(
        string p_config_timer_user_name,
        string p_config_timer_value,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        config_timer_user_name = p_config_timer_user_name;
        config_timer_value = p_config_timer_value;
        _couchDbHttpClient = couchDbHttpClient;
    }
}
```

Update every instantiation site in `Program.cs` to pass the resolved instance.

### Note on `Role_Replication.cs`

The `Role_Replication` class constructor accepts `config_timer_user_name`/`config_timer_value` but the `Execute()` method body is entirely commented out (the active `cURL` call on line 39 is inside a commented block). Add the `CouchDbHttpClient` parameter for consistency and to prepare the class for future activation, but no active call-site replacement is required.

### Scale of change in `Program.cs`

`Program.cs` is large (~2900+ lines). The pattern is uniform — every `new cURL(...)` followed by `.executeAsync()` becomes a single `await couchDbHttpClient.ExecuteAsync(...)`. Work top-to-bottom by method/block. Use a global find (`new cURL`) to track completion. The credential arguments (`null, null` for external; env-specific strings for CouchDB) remain unchanged.

### Namespace import to add

```csharp
using mmria.common.getset;
```
