# AI Context Pack (Single File)
This document is the **source of truth** for Copilot/AI-assisted changes in this repo.

See also: [Account Login + Session Auth Context](./account_login_session_auth_context.md)
See also: [Authentication, Session, and Timeout Context](./authentication_session_timeout.md)
See also: [Populate CDC Instance and De-identification Context](./populate_cdc_deidentification_context.md)
See also: [User Reported Issues Context](./user_reported_issues_context.md)
See also: [Security Scan Guidance: Sensitive Data on Heap](./security_scan_sensitive_data_heap_guidance.md)
See also: [Controller -> SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md)

## Hard constraints (read first)
- **Preserve routes**: Any change to controllers, APIs, or server-side code must **not** change route names, route templates, `[Route]` attributes, HTTP method attributes, or conventional routing behavior **unless explicitly asked**. This is to preserve app functionality.- **Never change controller action signatures or return types without explicit discussion**: Do not modify the method signature, parameters, or return type of any existing controller action without first discussing it with the user. This includes changing action return types (e.g. `JsonResult` → `IActionResult`) and HTTP status codes returned.
- **Never change view models / response shapes without explicit discussion**: The shape of any object returned by a controller action (view model, DTO, JSON response) is part of the **front-end contract**. Changing property names, types, nesting, or removing/adding fields will break JavaScript consumers at runtime with no compile-time warning. **Do not alter response shapes without explicit approval.**- **Refactor-only default**: When refactoring, **minimize enhancements**. Do not add new features, change behavior, alter outputs, or “improve” UX/performance beyond what is necessary to achieve the refactor goal—unless explicitly asked. Prefer small, mechanical moves that preserve existing behavior.
- **SharedLibraries-first**: Prefer implementing new/changed server-side logic in `SharedLibraries/` rather than directly in the MVC app.
- **SharedLibraries location priority**: `mmria.common/SharedLibraries/` is the **preferred** location for all new SharedLibraries code. `mmria-server/SharedLibraries/` is **deprecated** — do not add new feature folders there. Migrate existing mmria-server SharedLibraries code to `mmria.common/SharedLibraries/` when touching those files.
- **MMRIA Services placement**: Place all `mmria.services` code in `mmria.common/sharedlibraries/mmriaservices` (`mmria.common/SharedLibraries/MMRIAServices`).
- **MMRIA Services helpers placement**: Place `mmria.services` helper methods in the same helper file under `mmria.common/sharedlibraries/mmriaservices` (`mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs`).
- **No nested try-catches in Managers or DAL**: When moving code from a controller into a Manager or DAL, do **not** wrap the method body in an outer `try/catch`. Exceptions must propagate unhandled out of the Manager/DAL so the calling controller's `try/catch` can return a meaningful error response to the client. Inner catches are only acceptable for truly isolated sub-operations that must not abort the whole method (e.g., a best-effort audit write). Never add a catch that swallows an exception and returns a default/null/empty-list value when the operation has actually failed.
- **Enforced feature-based layering (Option A)**: Organize shared server-side code by **feature**, and within each feature use **/Model**, **/Manager**, **/DAL**:
  - `/Model`: shared data contracts used by Manager and/or DAL
  - `/Manager`: business logic + orchestration
  - `/DAL`: *all* CouchDB/data access calls (and nothing else)
- **No CouchDB calls in Controllers** (new or modified code): Controllers call Managers. Managers call DAL.
- **Jurisdiction isolation**: All data operations must be **jurisdiction-scoped** and must **never** cross jurisdictions unless explicitly designed and approved.

---

## Overview
This repository contains a **.NET 9 MVC** application ("MMRIA Server") with controllers and Razor views, plus an extended service ("MMRIA Sir Services") that handles additional calls.

The system is primarily single-tenant in deployment, but logically multi-tenant: tenants are called **jurisdictions**. Each jurisdiction has its **own CouchDB database set** (including a configuration DB and additional DBs per jurisdiction). The app also uses **Akka.NET** in parts of the server-side system and serves significant front-end JavaScript from `wwwroot/`.

### High-level components
- **MMRIA Server (.NET 9 MVC)**
  - Controllers + Razor Views
  - Should call shared **Managers** (preferred), not CouchDB directly
  - Uses/hosts Akka.NET actor system for background processing
  - Runs Quartz.NET scheduled jobs (pulse every minute, midnight sync jobs)
  - Serves static assets from `wwwroot/`
- **MMRIA Services**
  - Standalone .NET service for background processing (vitals import, backups, batch operations)
  - Hosts Akka.NET actor system and Quartz.NET scheduler
  - Runs independently of the web application
  - See [MMRIA Services & MMRIA-Server Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md) for complete job schedules and actor details
- **Offline Mode**
  - Client-side offline functionality with encryption and service worker caching
  - Enables users to work without internet connectivity
  - See [Offline Mode Documentation](./offline_mode.md) for complete technical details
- **CouchDB**
  - Jurisdiction-scoped DB topology and naming rules
  - Mango queries and/or design-doc views may be used

---

## Architecture (enforced)
### SharedLibraries priority
All new or modified server-side code should be prioritized into the `SharedLibraries/` folder whenever feasible. Controllers should remain thin wrappers that call into shared feature `/Manager` code.

For controller migration planning and current wave/order guidance, see [Controller -> SharedLibraries Migration Matrix](./controller_sharedlibraries_migration_matrix.md).

> **DEPRECATION NOTICE**: `mmria-server/SharedLibraries/` is deprecated. All new feature SharedLibraries belong in `mmria.common/SharedLibraries/<FeatureName>/`. When modifying any file under `mmria-server/SharedLibraries/`, migrate it to `mmria.common/SharedLibraries/` as part of that work.

### No nested try-catches
When extracting controller logic into a Manager or DAL:
- ❌ **Do NOT** add an outer `try/catch` around the entire method body.
- ✅ Let exceptions propagate naturally to the controller so it can return a proper HTTP error response.
- ✅ Inner catches are only allowed for **isolated, non-critical sub-operations** (e.g., best-effort audit writes) where failure of that sub-operation should not abort the whole request — and even then the catch must log the error, never silently swallow it.
- ❌ Never return `null`, `false`, or an empty collection from a catch block as a way of hiding a failure from the caller.

## Enforced structure: Feature-based /Model, /Manager, /DAL
All new or modified server-side code should be prioritized into `SharedLibraries/` and organized by **feature**, with `/Model`, `/Manager`, and `/DAL` under each feature folder.

**CRITICAL: NO generic Model/Manager/DAL folders at SharedLibraries root level. ALL code must be feature-scoped.**

### Required layout
Location: `SharedLibraries/<FeatureName>/`

Each feature contains:
- `Model/`    (contracts used by Manager/DAL)
- `Manager/`  (business logic + orchestration)
- `DAL/`      (all CouchDB/data access for the feature)

Examples:
- `SharedLibraries/OfflineCase/Model/`
- `SharedLibraries/OfflineCase/Manager/`
- `SharedLibraries/OfflineCase/DAL/`
- `SharedLibraries/Case/Model/`
- `SharedLibraries/Case/Manager/`
- `SharedLibraries/Case/DAL/`

### Layer rules (strict)
- Controllers call **feature Managers**.
- Managers contain all business logic and orchestrate DAL calls.
- DAL contains **all CouchDB calls** and nothing else.
- Models used by a feature’s Manager or DAL must live under that feature’s `Model/`.
- **No CouchDB calls in Controllers** (new or modified code).

### Cross-feature reuse rules
- ❌ Do not call another feature's `DAL/` directly.
- ❌ Do NOT create generic `SharedLibraries/Model/`, `SharedLibraries/Manager/`, or `SharedLibraries/DAL/` folders.
- ✅ Shared utilities and truly cross-cutting models go in `SharedLibraries/Common/`:
  - `SharedLibraries/Common/Model/` (shared contracts)
  - `SharedLibraries/Common/` (shared helpers/utilities)
- If Feature A needs something from Feature B, prefer exposing it via Feature B’s **Manager** (or a shared service), not by reaching into Feature B’s DAL.

### Migration policy
- When modifying legacy code, move it into the appropriate feature folder.
- If the “feature home” is unclear, choose the best fit and document the rationale in the PR description.

### Controllers (MVC)
- Controllers must not call CouchDB directly.
- Controllers must not contain business logic.
- Controllers call feature Managers.
- Controllers must preserve routing contract unless explicitly asked to change it.
- **Do NOT change action method signatures** (parameters, return types, HTTP attributes) without explicit user approval.
- **Do NOT change the shape of any view model or JSON response** returned by a controller action. JavaScript on the front end depends on exact property names, types, and structure — a shape change causes silent runtime errors in JS that are difficult to debug.
- Pass `CancellationToken` through.

### Akka.NET rules
**CRITICAL: No blocking calls in actors**

Blocking async operations in actors causes deadlocks and `System.NotSupportedException: There is no active ActorContext`.

**Required patterns:**
- ✅ Use `ReceiveAsync<T>` for async message handlers
- ✅ Use direct `await` for all async operations
- ✅ Use async + `PipeTo` patterns for complex flows
- ✅ Prefer Tell + correlation over Ask on hot paths
- ✅ If actors need data, call Managers/DAL via DI

**Strictly forbidden:**
- ❌ **NEVER** use `.Result` in actors
- ❌ **NEVER** use `.Wait()` in actors
- ❌ **NEVER** use `.GetAwaiter().GetResult()` in actors
- ❌ Do NOT use blocking I/O

**Example:**
```csharp
ReceiveAsync<MessageType>(async message =>
{
    var result = await _service.DoSomethingAsync();
    Sender.Tell(new ResponseMessage { Data = result });
});

// Constructor async init (fire-and-forget): InitializeStateAsync();
private async void InitializeStateAsync() { var data = await _service.GetDataAsync(); }
```

### MMRIA Server ↔ MMRIA Sir Services
- Use `IHttpClientFactory` for HTTP calls.
- Use explicit request/response DTOs.
- Propagate correlation ID + jurisdiction explicitly.

### Migration policy (incremental)
- No new direct CouchDB calls in controllers.
- If you touch a controller action, move Couch logic behind feature Manager + DAL.
- Refactor endpoint-by-endpoint (strangler fig), not big bang.

## Routing contract (do not break)
- Routes are part of the contract.
- Do not rename controller routes, action routes, route templates, or routing attributes unless explicitly requested.
- Refactors must preserve routing behavior to avoid breaking application functionality.

---

## Tenancy (Jurisdictions)
### Terminology
- **Jurisdiction**: tenant-like isolation boundary.
- **Jurisdiction Context**: resolved identity + routing info needed to safely access the correct DBs.

### Non-negotiable rules
1. All data operations MUST be jurisdiction-scoped.
2. Never hardcode DB names in controllers/managers/DAL/actors.
3. Resolve jurisdiction once per request; pass context down.
4. No cross-jurisdiction access unless explicitly designed and approved.

### Jurisdiction resolution (TODO)
Document how jurisdiction is derived:
- Route value? Subdomain? Header? Auth claim? Session?
- Source precedence
- Validation rules
- Error handling when missing/invalid

### Multi-tenant database architecture
**Each jurisdiction has its own CouchDB server**, not just prefixed databases on a shared server.

**Configuration:**
- `multi_tenant_jurisdictions`: Comma-separated list of tenant identifiers (e.g., "tenant1,tenant2,tenant3,cdc")
- `multi_tenant_shared_config_id_template_couchdb_url`: URL template with `{replace}` token
  - Example: `http://{replace}-couchdb.local:6984`
  - Resolved: `http://tenant1-couchdb.local:6984`, `http://cdc-couchdb.local:6984`

**DBConfigurationDetail structure:**
```csharp
public sealed class DBConfigurationDetail
{
    public string prefix { get; set; }      // Database name prefix (typically "" in multi-tenant mode)
    public string url { get; set; }          // Base CouchDB server URL (e.g., "http://tenant1-couchdb.local:6984")
    public string user_name { get; set; }
    public string user_value { get; set; }
    
    public string Get_Prefix_DB_Url(string p_database_name)
    {
        return $"{url}/{prefix}{p_database_name}";
    }
}
```

**CRITICAL: `url` contains ONLY the base CouchDB server URL, NOT the database name.**

### DB topology + naming
Each jurisdiction has:
- `mmrds` - Primary case database
- `de_id` - De-identified cases
- `report` - Aggregate reporting data
- `configuration` - Jurisdiction-specific configuration
- Additional: `users`, `audit`, `session`, `logging`, `jurisdiction`, `offline_cases`, `vital_import`

**URL construction pattern (CORRECT):**
```csharp
// Using helper method (RECOMMENDED)
string url = db_config.Get_Prefix_DB_Url("report");

// Manual construction (if needed)
string url = db_config.url + $"/{db_config.prefix}de_id";

// WRONG - hardcoded server/port
string url = "http://localhost:5984/report";  // ❌ NEVER DO THIS
```

### Getting DBConfigurationDetail
**Use `MultiTenantConfigHelper.GetDBConfigForTenant()`** to obtain the correct database configuration:

```csharp
// In controllers (injected dependencies)
db_config = MultiTenantConfigHelper.GetDBConfigForTenant(
    _dbConfigSets,        // List<ConfigurationSet> from DI
    _configuration,       // OverridableConfiguration fallback
    host_prefix          // Current tenant identifier
);
```

**Configuration flow:**
1. `appsettings.json` → `multi_tenant_shared_config_id_template_couchdb_url`
2. `Program.cs` → Token replacement: `couchDbTemplateUrl.Replace("{replace}", tenant)`
3. `ConfigurationSet` → Loaded per tenant at startup
4. `MultiTenantConfigHelper` → Returns tenant-specific `DBConfigurationDetail`

**CRITICAL:** Both `mmria-server` and `mmria-services` must use the same multi-tenant configuration mechanism.

### Passing jurisdiction through the system
- MVC obtains context via middleware/filter or `IJurisdictionAccessor`
- Managers accept context explicitly OR read it from accessor
- Cross-service calls include jurisdiction (header/param) and validate on receiver

### Testing requirements
- Unit tests for DB naming rules
- Integration tests that ensure no cross-jurisdiction access

### Test project build note
- In this environment, `source-code/mmria/mmria-server.tests/mmria-server.tests.csproj` can fail during parallel MSBuild project-reference evaluation with a generic `Build FAILED` and `0 Error(s)` result, even when source code is valid.
- When building or testing that project from the CLI, prefer single-process execution:
  - `dotnet build source-code\mmria\mmria-server.tests\mmria-server.tests.csproj --no-restore -m:1`
  - `dotnet test source-code\mmria\mmria-server.tests\mmria-server.tests.csproj --no-restore -m:1`
- Treat this as an environment/MSBuild issue first, not automatically as a source compile failure in `CaseTests.cs` or other test files.

---

## Security Best Practices

**Critical rules:**
- ❌ No SSNs/PII in string variables (heap inspection risk). Use inline: `if (set.Contains(item.Substring(start, 9).Trim()))`
- ❌ No `System.Random` for IDs/tokens/keys. Use: `RandomNumberGenerator.GetInt32(min, max)`
- ❌ No untrusted input in file paths. Use: `Path.GetFileName(userInput)` to sanitize
- ✅ Remove PII from logs/errors (log line numbers, not values)

```csharp
// BAD: var ssn = item.Substring(start, 9); if (set.Contains(ssn)) { log(ssn); }
// GOOD: if (set.Contains(item.Substring(start, 9).Trim())) { log($"Line {n}"); }

// BAD: var id = new Random().Next(1000, 9999);
// GOOD: var id = RandomNumberGenerator.GetInt32(1000, 10000);

// BAD: var path = Path.Combine(baseDir, userFileName);
// GOOD: var path = Path.Combine(baseDir, Path.GetFileName(userFileName));
```

---

## CouchDB guidance
### Golden rules
- No direct CouchDB calls from controllers (new/modified code).
- Use DAL for all CouchDB access.
- Avoid N sequential calls in loops; batch where possible.
- Prefer `_bulk_docs` for multiple writes.
- Avoid accidental unindexed scans; ensure indexes exist for frequent Mango queries.

### Connection management
- Centralize CouchDB client configuration (auth, base URL, serialization).
- Reuse configured clients; do not create ad-hoc clients throughout code.
- Centralize retry/backoff and timeout policy.

### Error handling & logging
**MANDATORY: No empty catch blocks**
- ❌ **NEVER** use empty catch blocks: `catch (Exception) { }`
- ✅ **ALWAYS** log CouchDB errors with context
- ✅ Use `throwOnError: true` for critical operations (design docs, database creation)

**CouchDbHttpClient features (as of implementation):**
- Validates JSON syntax before sending PUT/POST requests
- Parses CouchDB error responses: `{"error":"bad_request", "reason":"invalid_json"}`
- Logs all HTTP errors to console
- Optional exception throwing via `throwOnError` parameter

```csharp
// BAD: Silent failure
try { await _couch.ExecuteAsync("PUT", url, json, user, pass); } catch { }

// GOOD: Log errors, optionally throw for critical ops
try
{
    await _couchDbHttpClient.ExecuteAsync("PUT", url, json, user, pass, throwOnError: true);
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
    throw;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"CouchDB error: {ex.Message}");
    // Handle or rethrow
}
```

**Normalize CouchDB errors into meaningful outcomes:**
- missing doc → NotFound (if applicable)
- conflict → 409 with actionable message
- forbidden/unauthorized → 401/403
- timeout/unavailable → 503 (with retry policy as appropriate)

**Log fields (recommended):**
- jurisdictionId, dbName, operation, docId (if applicable), latency, status/exception, correlationId

### cURL is DEPRECATED - Use CouchDbHttpClient
**Do NOT use `cURL` class in new/modified code.**

```csharp
// OLD: var curl = new cURL("GET", null, url, null, user, pass); string r = curl.execute();
// NEW: string r = await _couchDbHttpClient.ExecuteAsync("GET", url, null, user, pass);
```

**Rules:**
- ✅ Use `CouchDbHttpClient` via DI; ✅ `async Task<T>` methods; ✅ direct `await`
- ❌ No `cURL` class; ❌ No `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`

**Why:** IHttpClientFactory prevents socket exhaustion, proper pooling, better testability.

### Design document deployment
**Design documents must be valid JSON** - `CouchDbHttpClient` validates syntax before upload.

**Common JSON issues:**
- Extra closing braces: `}}}` instead of `}}`
- Missing commas between properties
- Unescaped quotes in JavaScript strings

**View function null safety:**
CouchDB design doc views must check for null/undefined before accessing nested properties:

```javascript
// BAD: Crashes if doc.home_record is null
emit(doc.home_record.first_name.toLowerCase(), {...});

// GOOD: Null-safe
if (doc.home_record && doc.home_record.first_name) {
    emit(doc.home_record.first_name.toLowerCase(), {...});
}
```

**Deployment pattern:**
```csharp
string current_directory = AppContext.BaseDirectory;
if (!Directory.Exists(Path.Combine(current_directory, "database-scripts")))
{
    current_directory = Directory.GetCurrentDirectory();
}

using var sr = new StreamReader(Path.Combine(current_directory, "database-scripts/case_design_sortable.json"));
string designDocJson = await sr.ReadToEndAsync();

// Use throwOnError: true for design docs (critical operation)
await _couchDbHttpClient.ExecuteAsync(
    "PUT",
    db_config.url + $"/{db_config.prefix}de_id/_design/sortable",
    designDocJson,
    db_config.user_name,
    db_config.user_value,
    throwOnError: true  // Fail fast if JSON is invalid or upload fails
);
```

**Timing consideration:** After creating a new database, CouchDB may need brief initialization time before accepting design documents. If uploads fail immediately after database creation, consider adding `await Task.Delay(100)` before design doc upload.

---

## Patterns (what to generate)
### Controller pattern
- Get jurisdiction context (or use accessor)
- Validate input
- Call feature Manager
- Return IActionResult / view result
- **Do not change routes unless explicitly asked**
- **Refactor-only default**: preserve behavior unless explicitly asked to change it

### Manager pattern
- Public use-case entry points
- Orchestrate DAL calls
- Apply business rules and validation
- Return DTOs/models suitable for controllers/views

### DAL pattern
- Provide domain-meaningful methods (avoid “god query runner”)
- Encapsulate:
  - DB selection by jurisdiction
  - query definitions (Mango/view) + indexes
  - bulk operations
  - mapping to/from Models

### JavaScript in wwwroot
- Keep feature modules organized
- Avoid duplicate utility functions
- Prefer consistent naming and structure:
  - `wwwroot/js/shared/`
  - `wwwroot/js/features/<feature>/`
  - `wwwroot/js/pages/<page>/`

---

## Checklist (Copilot must follow)
### Non-negotiables
- ✅ Preserve existing route names/templates and routing behavior unless explicitly requested.
- ✅ Refactor-only default: minimize enhancements; preserve existing behavior unless explicitly asked to change it.
- ✅ Prefer changes in `SharedLibraries/`.
- ✅ **New SharedLibraries code goes in `mmria.common/SharedLibraries/<FeatureName>/`** — this is the canonical location.
- ❌ **Do NOT add new feature folders to `mmria-server/SharedLibraries/`** — that location is deprecated. Migrate to `mmria.common` when touching those files.
- ✅ Organize new/modified SharedLibraries code under `SharedLibraries/<FeatureName>/` (e.g., `OfflineCase`, `Case`, `Session`).
- ❌ Do NOT create generic `SharedLibraries/Model/`, `SharedLibraries/Manager/`, or `SharedLibraries/DAL/` folders.
- ✅ Each feature must have `Model/`, `Manager/`, and `DAL/` folders within the feature folder.
- ✅ Models used by Manager/DAL go in that feature's `/Model`.
- ✅ Business logic goes in that feature's `/Manager`.
- ✅ CouchDB calls go only in that feature's `/DAL`.
- ✅ Resolve jurisdiction once; pass context down.
- ❌ No new CouchDB calls in controllers.
- ❌ Do not hardcode DB names.
- ❌ Do not cross jurisdictions.
- ❌ Do not call another feature's `DAL/` directly.
- ❌ **No nested try-catches in Managers or DAL.** Do not wrap a Manager/DAL method body in an outer `try/catch`. Exceptions must reach the controller so it can return a proper HTTP error. Inner catches are only allowed for isolated best-effort sub-operations (e.g., audit writes) and must always log — never silently return `null`, `false`, or empty collections on failure.

### CouchDB efficiency
- ✅ Minimize round-trips; batch reads/writes where possible.
- ✅ Prefer `_bulk_docs` for multiple writes.
- ✅ Avoid unindexed queries/scans.
- ❌ Don’t do N sequential Couch calls in loops.

### Akka.NET safety
- ✅ Use `ReceiveAsync<T>` for async message handlers.
- ✅ Use async + `PipeTo`.
- ✅ Direct `await` for all async operations in actors.
- ❌ **NEVER** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in actors.
- ❌ Avoid Ask in hot paths unless justified.

### Security
- ✅ No sensitive data (SSN, PII) stored in string variables.
- ✅ Use `System.Security.Cryptography.RandomNumberGenerator` for secure random values.
- ✅ Sanitize file paths with `Path.GetFileName()` before using external input.
- ✅ Remove PII from error messages and logs.
- ❌ Do NOT use `System.Random` for IDs, tokens, or keys.
- ❌ Do NOT use untrusted input directly in file paths.
- ❌ Do NOT use `cURL` class (deprecated).

### Cross-service calls
- ✅ Use `IHttpClientFactory`.
- ✅ Propagate jurisdiction + correlation IDs.

---


## Performance
- Correct jurisdiction isolation
- Minimize CouchDB round-trips; batch operations
- Stable actor concurrency (no mailbox runaway)
- Track: DB calls/latency, query indexing, actor mailbox depth, render time

### Case View Endpoint Optimizations (Feb 2026)
**Context**: `/api/case_view` endpoint experienced slow performance in cloud vs local environments with 600-1000 cases.

**Server-side optimizations implemented in `CaseViewSearch.cs`**:

1. **Parallel HTTP requests** (lines 988-1004): Fetch main case view query and pinned cases in parallel instead of sequentially to reduce round-trip latency in cloud environments
   ```csharp
   Task<string> mainQueryTask = _couchDbHttpClient.ExecuteAsync(...);
   Task<mmria.common.model.couchdb.pinned_case_set> pinnedCasesTask = null;
   if (is_case_identified_data && is_include_pinned_cases)
       pinnedCasesTask = GetPinnedCaseSet();
   ```

2. **Skip unnecessary predicate creation** (lines 1179-1198): When all filters are "all" and no search key, skip creating 20+ predicate functions to reduce overhead
   ```csharp
   bool has_filters = !string.IsNullOrWhiteSpace(search_key) || 
                     !case_status.Equals("all", ...) || ...;
   if (!has_filters) return;  // Skip predicate creation
   ```

3. **Single-pass filtering** (lines 1045-1063, 1100-1116): Use single foreach loop instead of multiple `.Where().ToList()` LINQ chains to avoid multiple iterations and intermediate allocations
   ```csharp
   // OLD: var data = rows.Where(...).ToList(); 
   //      var offline = data.Where(...).ToList();
   //      var online = data.Where(...).ToList();
   // NEW: foreach loop with inline predicate checks
   ```

**Client-side optimizations** (Feb 2026):

4. **Eliminate redundant pinned cases HTTP call**: Extended `case_view_response` to include `pinned_case_set` property, removing separate sequential `/api/pinned_cases` call
   - Modified `case_view_response.cs` to include `pinned_case_set` property
   - Populated in `CaseViewSearch.cs` when `include_pinned_cases=true`
   - Client in `index.js` now uses `case_view_response.pinned_case_set` instead of separate API call
   
   **Impact**: Eliminates 1 HTTP round-trip, reducing cloud latency by ~30-50ms for typical queries

5. **Parallel case view and offline session calls** (index.js lines 1790-1810): Use `Promise.all()` to fetch `/api/case_view` and `/api/OfflineCase/active-user-session` in parallel when offline mode is enabled
   ```javascript
   // Start both HTTP calls in parallel
   let offlineSessionPromise = null;
   if(is_offline_mode_enabled==true) {
       offlineSessionPromise = fetch(`/api/OfflineCase/active-user-session`, ...);
   }
   
   // Wait for both to complete
   const [case_view_response] = await Promise.all(
       [$.ajax({ url: case_view_url }), offlineSessionPromise].filter(p => p !== null)
   );
   ```
   
   **Impact**: Eliminates sequential wait for offline session check (~30-50ms in cloud)

**Expected Combined Impact**: Reduces total cloud latency by ~90-150ms for common queries with offline mode enabled in typical 600-1000 case environments:
- Pinned cases: ~30-50ms saved
- Offline session parallel fetch: ~30-50ms saved  
- Predicate optimization: ~10-30ms saved (CPU-bound, variable)
- Single-pass filtering: ~10-20ms saved (reduces GC pressure)

---

## External Service Integration

MMRIA integrates with external APIs for geocoding and social determinant data:
- **CVS (Community Vital Signs)**: 49 fields providing social determinant metrics - See [CVS_Community_Vital_Signs_Context.md](./CVS_Community_Vital_Signs_Context.md)
- **TAMU Geocoding**: Geocode fields at 10 locations providing coordinates and census data - See [TAMU_Geocoding_Context.md](./TAMU_Geocoding_Context.md)

For test data generation, see the [Case Generator documentation](../../nccdphp-drh-mmria-utilities/mmria-case-generator/docs/AI_CONTEXT.md).

For strongly-typed case model generation and adding new properties, see [strongly_typed_case_generator.md](./strongly_typed_case_generator.md).

---

## Data Summary Report Feature

The Data Summary Report provides frequency analysis and statistical summaries of MMRIA case data via the `/view-data-summary` route.

**Key Documentation**: See [data_summary_report.md](./data_summary_report.md) for complete architecture, implementation details, and historical context.

**Quick Reference**:
- **Freq Documents**: Generated via `c_generate_frequency_summary_report.cs` on every case save
- **CouchDB View**: `data_summary_view_report/_view/year_of_death` in `{prefix}report` database
- **API**: `/api/data-summary/{skip}` returns paginated freq documents (100 per page)
- **Frontend**: `view-data-summary/index.js` filters records client-side with date range and jurisdiction controls
- **Sync**: Automatic background sync via Akka.NET actors, manual via `/api/sync` (installation_admin only)

---

## Aggregate Report Feature

The Aggregate Report provides jurisdiction-level summary statistics on maternal mortality cases for analytical dashboards.

**Key Documentation**: See [aggregate_report.md](./aggregate_report.md) for complete architecture, API contract, frontend integration, and testing guidelines.

**Quick Reference**:
- **API Endpoint**: `GET /api/aggregate_report` returns `IList<c_report_object>`
- **Business Logic**: `mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs`
- **Data Model**: `mmria.common/SharedLibraries/AggregateReport/Model/c_report_object.cs`
- **Data Source**: CouchDB `report` database, filtered by jurisdiction
- **Frontend Route**: `/aggregate-report` MVC view (renders D3/C3 charts)
- **Architecture**: Follows feature-based layering (Manager orchestrates DAL calls; controller is thin wrapper)

---

## SAMS Authentication Integration

**SAMS (Secure Access Management System)** is CDC's enterprise authentication system used for external-facing applications.

### Configuration
- **Enable SAMS**: Set `sams:is_enabled` to `true` in configuration
- **SAMS URL**: Configure via `sams:logout_url` for logout redirects
- When enabled, SAMS handles all authentication via OAuth/OIDC flow with external redirect

### Key Implementation Details

**Server-Side Authentication Flow**:
- `use_sams` flag determined from `_configuration.GetBoolean("sams:is_enabled", host_prefix)` in AccountController constructor
- When SAMS enabled, Login action redirects to SignIn action, which initiates SAMS OAuth flow
- SAMS middleware intercepts unauthenticated requests and issues 302 redirects to external SAMS login page

**Client-Side Considerations**:
- JavaScript `fetch()` **does not** require `credentials: 'include'` for same-origin requests (cookies sent automatically)
- However, `fetch()` **cannot follow cross-origin redirects** due to CORS policy
- SAMS redirects to external domain (e.g., `apigw-stg.cdc.gov`) which triggers CORS errors in JavaScript

**Offline Mode Integration** (Feb 2026):
- **Problem**: Hardcoded client-side redirects to `/account/login` bypassed SAMS detection after offline→online transitions
- **Solution**: Created `/account/auto-login` endpoint that detects SAMS configuration server-side
- **Pattern**: All client-side navigation requiring authentication should use `/account/auto-login` instead of assuming `/account/login`

**Auto-Login Endpoint** (`AccountController.cs`):
```csharp
[AllowAnonymous]
[HttpGet("auto-login")]
public IActionResult AutoLogin(string returnUrl = null)
{
    // Detects SAMS configuration and routes appropriately
    if (use_sams.HasValue && use_sams.Value)
    {
        return RedirectToAction("SignIn", new { returnUrl });
    }
    return RedirectToAction("Login", new { returnUrl });
}
```

**Usage**:
- Offline-to-online transitions: `window.location.href = '/account/auto-login'`
- Any client-side code needing to trigger login: redirect to `/account/auto-login`
- Preserves returnUrl for post-authentication redirects

**Key Learning**: When integrating external authentication systems like SAMS:
1. Never hardcode authentication endpoints in client code
2. Use server-side detection to abstract authentication provider
3. Server controls routing decisions based on configuration
4. Avoids client-side config synchronization issues
5. Works correctly regardless of SAMS enabled/disabled state

---

## Client-Side Rendering: Case Summary Page Lifecycle

**Critical Pattern for AI Models**: The Case Summary page (`case/index.js` + `app.mmria.js`) uses a 5-phase asynchronous render cycle. State mutations (e.g., clearing error flags, toggling localStorage) **MUST be deferred to post-render callbacks**, not executed during render. Coupling state mutations to rendering causes race conditions where DOM updates are replaced by secondary render passes before user sees the content.

### The 5-Phase Render Cycle

#### PHASE 1: Data Loading (`get_case_set` in case/index.js)
- **Trigger**: `setTimeout()` or `window.onhashchange` handler
- **HTTP calls**: 
  - `/api/case_view` + query params (case data)
  - `/api/OfflineCase/active-user-session` (only if offline mode enabled) → parallelized
- **Result**: g_ui data structures populated with fresh data
- **Timing**: Network latency 50-200ms typical; cloud 150-300ms

#### PHASE 2: Render (page_render + app_render)
- **Entry**: `page_render(g_metadata, default_object, g_ui, ...)` called from case/index.js line 1933
- **Pattern**: 
  - `page_render()` routes to appropriate section render based on `p_data.type`
  - `app_render()` handles type='app' (case summary view)
  - **Read state flags AT START** of function (e.g., `localStorage.getItem('is_go_offline_error')`)
  - Render HTML conditionally based on flags → collected in `p_result` array
  - **Collect post-render callbacks as STRINGS** in `p_post_html_render` array
    - Example: `p_post_html_render.push("$('#search_text_box').on('change', ...);");`
- **Output**: Two arrays returned
  1. `p_result`: HTML strings to be joined
  2. `p_post_html_render`: Function calls/jQuery code as strings

#### PHASE 3: DOM Update
- **Execution**: Line 1935 in case/index.js
  ```javascript
  document.getElementById('form_content_id').innerHTML = page_render(...).join('');
  ```
- **Effect**: Browser renders HTML but lifecycle incomplete
- **Critical**: State mutations have NOT yet executed

#### PHASE 4: Post-Render Callbacks (eval execution)
- **Execution**: Lines 1942-1950 in case/index.js
  ```javascript
  if (post_html_call_back.length > 0) {
      const codeToEval = post_html_call_back.join('\n');
      eval(codeToEval);  // Execute collected callbacks
  }
  ```
- **Callbacks run here**: 
  - jQuery event bindings: `$('#search_text_box').on('change', ...)`
  - State mutations: `localStorage.setItem('flag', 'false')`
  - **This is where error flags MUST be cleared** (after first render)
- **Critical rule**: State-clearing operations collected in Phase 2 now execute

#### PHASE 5: Secondary Render (conditional)
- **Trigger**: `window.onhashchange` or manual `page_render()` call
- **Pattern**: Reads state flags (now cleared by Phase 4) → conditional renders skip
- **Result**: Previous render remains visible (no replacement)

### Error Message Visibility Pattern (Example: Go Offline Error)

**Problem**: Error flag cleared in render function (Phase 2) before DOM update (Phase 3), causing secondary renders to skip the message.

**Solution**: Defer flag clearing to post-render callback (Phase 4):

```javascript
// WRONG: Flag cleared too early
if (isGoOfflineError === 'true') {
    p_result.push(`<div>Error message</div>`);
    localStorage.setItem('is_go_offline_error', 'false');  // ❌ Clears DURING render
}

// CORRECT: Flag cleared after DOM update
if (isGoOfflineError === 'true') {
    p_result.push(`<div>Error message</div>`);
    // Push state mutation to post-render callback
    p_post_html_render.push("localStorage.setItem('is_go_offline_error', 'false');");
}
```

**Data Flow**:
1. **Flag set**: `offline-transition-manager.js:705` → `EnableGoOfflineErrorState()` → `localStorage.setItem('is_go_offline_error', 'true')`
2. **Phase 2**: `app_render` reads 'true' → renders HTML message
3. **Phase 3**: Message appears in DOM
4. **Phase 4**: Callback executes → clears flag to 'false'
5. **Phase 5** (if occurs): Reads 'false' → skips rendering → previous message persists
6. **Result**: User sees error message until navigation or explicit clear

### When to Use p_post_html_render

**DO use for**:
- ✅ State mutations (localStorage, global variables)
- ✅ Event handler binding (jQuery `.on()`, `.click()`)
- ✅ DOM manipulation that depends on newly-rendered elements
- ✅ Async operations initiated after first render

**DON'T use for**:
- ❌ HTML generation (belongs in `p_result`)
- ❌ Conditional rendering logic (read flags in Phase 2)
- ❌ Data transformations

### Key Files & Entry Points

| Role | File | Key Function | Lines |
|------|------|--------------|-------|
| Orchestrator | `case/index.js` | `get_case_set(p_call_back)` | 1700-2000 |
| Orchestrator | `case/index.js` | `window_on_hash_change(e)` | 2036-2100 |
| Render router | `page_renderer.js` | `page_render()` | Main render entry |
| App renderer | `app.mmria.js` | `app_render()` | 286-1561 |

### Hash Change Handler: Manual Triggers Must Be Eliminated

**CRITICAL FIX (Feb 2026)**: Removed manual `window.onhashchange()` calls from `load_and_set_data()` and offline path in `get_case_set()`.

**Problem**: After page load, the code manually triggered `window.onhashchange({ isTrusted: true, newURL: window.location.href })` immediately after `await get_case_set()`. This caused a second render to fire **before the browser painted the first render**, resulting in the error message being overwritten.

**Timeline of the Bug**:
1. `get_case_set()` completes rendering (Phase 1-3)
2. Post-render callbacks execute in Phase 4 (flag cleared to 'false')
3. ❌ Manual `window.onhashchange()` fires **synchronously** during Phase 4
4. ❌ `window_on_hash_change` calls `g_render()` → Phase 5 second render
5. ❌ Second render reads cleared flag, skips error message
6. ❌ Browser finally paints, showing second render (error gone)

**Root Cause**: The manual call at line 1705 in `load_and_set_data()` was unnecessary on initial page load. The `window_on_hash_change` handler handles all actual user navigation automatically. On page load, `g_data` is `null` and only the summary view exists—no actual hash change needed processing.

**Solution**: Removed the manual triggers:
- ❌ Deleted: `window.onhashchange({ isTrusted: true, newURL: window.location.href });` from `load_and_set_data()` (line ~1705)
- ❌ Deleted: `setTimeout(() => { window.onhashchange(...) })` from offline path in `get_case_set()` (line ~1715)

**Result**:
- ✅ Only one render occurs during page load
- ✅ Error messages persist visible on screen
- ✅ `window_on_hash_change` still fires for actual user navigation (browser's native hash change)
- ✅ All business logic in `window_on_hash_change` preserved (case switching, saving, cleanup)

**Files Modified**:
- `wwwroot/scripts/case/index.js` lines 1618-1625, 1758-1769 (removed manual triggers)
- Cleaned up debug logging that was added during investigation

**Testing (must verify)**:
- ✓ Offline error message now persists until user navigates or dismisses
- ✓ Page load with `/#/0/form` hash loads first case correctly
- ✓ Page load with `/#/summary` shows case list summary
- ✓ Case-to-case navigation still works
- ✓ Navigate back to summary still works
- ✓ Browser back/forward buttons work correctly
- ✓ Direct URL entry (e.g., `index.html#/2/form_name`) loads correct case

### Implications for AI Models

When modifying client-side rendering code:
1. **Always check**: Are state mutations in `p_post_html_render` or in function body?
2. **Flag mutations**: MUST be pushed as strings to `p_post_html_render`, never inline
3. **Error messages**: Decouple message rendering from flag clearing (separate phases)
4. **Race conditions**: Secondary renders replace DOM—defer state changes to prevent visibility loss
5. **Hash change triggers**: NEVER manually call `window.onhashchange()` after page setup. Let browser's native hash change events trigger it.
6. **Testing**: Verify error messages persist through multiple render cycles and navigation works correctly

---

## Copilot prompt template
"Follow AI_CONTEXT.md: Preserve routes. Feature-based SharedLibraries/<Feature>/{Model,Manager,DAL}. New SharedLibraries go in mmria.common/SharedLibraries/ — mmria-server/SharedLibraries/ is deprecated. No nested try-catches in Managers/DAL: let exceptions propagate to the controller so it can return proper HTTP errors; inner catches only for isolated best-effort sub-ops and must log. Multi-tenant: separate CouchDB servers per jurisdiction, use db_config.url + /prefix + dbname. Use CouchDbHttpClient (not cURL) with throwOnError for critical ops. No empty catch blocks. ReceiveAsync+await in actors (never .Result/.Wait()). Security: no PII in strings, use RandomNumberGenerator, sanitize paths with GetFileName(). Jurisdiction-scoped data access. Client-side rendering: State mutations MUST be in p_post_html_render callbacks, not during render phase."
