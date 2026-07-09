# Story 13.1 — data-migration: Replace cURL with CouchDbHttpClient

**Epic:** 13 — HTTP Client Modernization
**Story ID:** 13.1
**Status:** done
**Date added:** 2026-07-08
**PRD ref:** FR-15

---

## User Story

As a developer maintaining the data-migration tool,
I want all CouchDB and external HTTP calls to go through `mmria.common.getset.CouchDbHttpClient` instead of the local `cURL` class,
So that the data-migration project uses the same tested, DI-managed HTTP layer as mmria-server and mmria-services, and `cURL.cs` can be deleted.

---

## Acceptance Criteria

**AC-1 — DI packages added and `ServiceProvider` built in `Program.cs`**
Given the refactored `migrate.csproj`
When the project is opened
Then it references `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Http`
And `Program.cs` constructs a `ServiceCollection`, calls `services.AddHttpClient()`, registers `CouchDbHttpClient` as a singleton, builds a `ServiceProvider`, and resolves `CouchDbHttpClient` before the main run begins

**AC-2 — `CouchDbHttpClient` replaces every `new cURL(...)` call in `SaveRecord.cs`**
Given the refactored `SaveRecord.cs`
When the developer searches for `new cURL`
Then no matches exist
And all CouchDB calls use `await _couchDbHttpClient.ExecuteAsync(method, url, payload, userName, password)` with the same method, URL, payload, and credentials as before
And the one synchronous `document_curl.execute()` call on line 235 is converted to `await _couchDbHttpClient.ExecuteAsync(...)`, with the containing method made `async`

**AC-3 — `CouchDbHttpClient` replaces every `new cURL(...)` call in `db_backup/db_backup.cs`**
Given the refactored `db_backup.cs`
When the developer searches for `new cURL`
Then no matches exist
And all CouchDB calls use `await _couchDbHttpClient.ExecuteAsync(...)`

**AC-4 — `CouchDbHttpClient` replaces every `new cURL(...)` call in all `migration-set/` files**
Given the refactored migration-set classes
When the developer searches for `new cURL` across the following files
Then no matches exist in any of them:
- `committee_review_pregnancy_relatedness.cs`
- `CVS_Migration.cs` (CouchDB calls)
- `editable_list.cs`
- `Fix_American_Indian_Recode.cs`
- `GA-One-Time.cs`
- `Manual-Migration.cs`
- `MMRDS_CS_Narrative_Migration.cs`
- `Process_Migrate_Charactor_to_Numeric.cs`
- `SubstanceMigration.cs`
- `v2.10-Migration.cs`

**AC-5 — `CouchDbHttpClient` replaces every `new cURL(...)` call in `mmrds-importer/mmria_server_api_client.cs`**
Given the refactored `mmria_server_api_client.cs`
When the developer searches for `new cURL`
Then no matches exist
And all calls use `await _couchDbHttpClient.ExecuteAsync(...)`

**AC-6 — External CVS service calls migrated to `CouchDbHttpClient` with null credentials**
Given the refactored `common/CVS.cs` and `migration-set/CVS_Migration.cs`
When the developer searches for `new cURL`
Then no matches exist
And the unauthenticated POST calls to the CVS base URL use `await _couchDbHttpClient.ExecuteAsync("POST", base_url, body_text, null, null)`

**AC-7 — `cURL.cs` deleted from the project**
Given all call sites have been migrated
When the developer lists the project files
Then `data-migration/cURL.cs` does not exist

**AC-8 — Project builds without errors after migration**
Given all changes are applied
When `dotnet build` is run against `migrate.csproj`
Then the build exits with code 0 and zero errors

**AC-9 — No behavior change**
Given the migrated project connects to the same CouchDB URL with the same credentials and payloads
When a migration run executes in report-only mode (`IsReportOnlyMode = true`)
Then the output log is identical in content and structure to the pre-migration output
And no migration logic, JSON serialization, `has_been_done_set` skip mechanism, or report-only mode guard is modified

---

## Dev Notes — Implementation Guide

### Packages to add to `migrate.csproj`

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
```

### DI wiring in `Program.cs` `Main()`

Add before the main run logic (after config is loaded):

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
// BEFORE (async):
var curl = new cURL("GET", null, url, null, user_name, password);
string result = await curl.executeAsync();

// AFTER:
string result = await couchDbHttpClient.ExecuteAsync("GET", url, null, user_name, password);

// BEFORE (sync — SaveRecord.cs line ~235):
var document_curl = new cURL("PUT", null, put_url, object_string, user_name, user_value);
string responseFromServer = document_curl.execute();

// AFTER (make containing method async):
string responseFromServer = await couchDbHttpClient.ExecuteAsync("PUT", put_url, object_string, user_name, user_value);
```

Note: `cURL(method, headers, url, payload, username, password)` → `ExecuteAsync(method, url, payload, userName, password)`. The `headers` parameter (always `null`) is dropped; the positional order of `url` and `payload` is preserved.

### Threading `CouchDbHttpClient` into migration classes

Each class that currently builds a `cURL` object receives `CouchDbHttpClient` as a new constructor parameter:

```csharp
// Example — SubstanceMigration.cs:
public sealed class SubstanceMigration
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public SubstanceMigration(
        string db_server_url,
        ...
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        ...
        _couchDbHttpClient = couchDbHttpClient;
    }
}
```

Update every instantiation site in `Program.cs` to pass the resolved instance.

### Files that need constructor changes

The following classes currently own `cURL` construction and need a `CouchDbHttpClient` parameter added:

| File | Class |
|------|-------|
| `SaveRecord.cs` | `SaveRecord` |
| `db_backup/db_backup.cs` | `Backup` |
| `migration-set/committee_review_pregnancy_relatedness.cs` | `committee_review_pregnancy_relatedness` |
| `migration-set/CVS_Migration.cs` | `CVS_Migration` |
| `migration-set/editable_list.cs` | `editable_list` |
| `migration-set/Fix_American_Indian_Recode.cs` | `Fix_American_Indian_Recode` |
| `migration-set/GA-One-Time.cs` | `GA_One_Time` (or similar) |
| `migration-set/Manual-Migration.cs` | `Manual_Migration` |
| `migration-set/MMRDS_CS_Narrative_Migration.cs` | `MMRDS_CS_Narrative_Migration` |
| `migration-set/Process_Migrate_Charactor_to_Numeric.cs` | `Process_Migrate_Charactor_to_Numeric` |
| `migration-set/SubstanceMigration.cs` | `SubstanceMigration` |
| `migration-set/v2.10-Migration.cs` | `v2_10_Migration` (or similar) |
| `mmrds-importer/mmria_server_api_client.cs` | `mmria_server_api_client` |
| `common/CVS.cs` | `CVS` (or the class making the calls) |

### Sync call to fix

`SaveRecord.cs` line ~235 has a synchronous `document_curl.execute()` call. The containing method must be made `async Task<...>` and the call replaced with `await _couchDbHttpClient.ExecuteAsync(...)`. Check the call chain from `Program.cs` to ensure the calling method is also awaited correctly.

### Namespace import to add

```csharp
using mmria.common.getset;
```
