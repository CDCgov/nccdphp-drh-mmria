# Story 16.1 — Establish SystemOffline SharedLibraries Feature

**Epic:** 16 — Controller Pattern Remediation
**Story ID:** 16.1
**Status:** done
**Date added:** 2026-07-14
**Depends on:** none
**Source requirements:** project-context.md §2.2; controller_sharedlibraries_migration_matrix.md Wave 9

---

## User Story

As a developer working on the system offline feature,
I want the system offline business logic and service HTTP access to live in a SharedLibraries Manager and DAL,
So that `system_offlineController` contains only routing, authorization, and response shaping — no direct service calls.

---

## Acceptance Criteria

**AC-1 — CouchDB/service calls extracted from controller**
Given the current `system_offlineController` calls `_couchDbHttpClient.ExecuteAsync(...)` directly in `SaveConfig()` and in the private `LoadConfigFromServicesAsync()` helper
When this story is complete
Then both of those calls have been moved into `SharedLibraries/SystemOffline/DAL/SystemOfflineDAL.cs`; the controller delegates through `SharedLibraries/SystemOffline/Manager/SystemOfflineManager.cs`

**AC-2 — SystemOfflineMessageFormatter deleted, logic moved to Manager**
Given `mmria.server.util.SystemOfflineMessageFormatter` currently lives in server-only utility code
When this story is complete
Then the message-substitution logic has been moved into `SystemOfflineManager`; the `mmria.server.util.SystemOfflineMessageFormatter` class is deleted

**AC-3 — DI registration follows established pattern**
Given `SystemOfflineManager` and `SystemOfflineDAL` are created
When registered in the server DI container
Then they follow the same `AddScoped` pattern as other SharedLibraries features (e.g., `ManageUsersManager`, `CVSManager`) in `Program.cs`

**AC-4 — No external-facing changes**
Given `system_offlineController`'s public actions (`Index`, `GetConfig`, `GetJurisdictions`, `GetStatus`, `SaveConfig`) and the `/api/system-offline/status` route
When the refactor is complete
Then route paths, action signatures, HTTP method attributes, auth attributes (`[Authorize(Roles = ...)]`), and response JSON shapes are byte-for-byte identical to pre-refactor — no client-side changes required

**AC-5 — Tenant resolution stays in controller**
Given the controller still needs tenant resolution (`host_prefix`, `configuration`, `ConfigDB`)
When the story is implemented
Then tenant resolution stays in the controller per project-context.md §2.2 first-pass rule — it is not moved into `SystemOfflineManager`

**AC-6 — GetJurisdictions key-list filtering stays in controller**
Given `GetJurisdictions()` filters `ConfigDB.detail_list.Keys` in the controller
When the refactor is complete
Then that key-list filtering stays in the controller — it is lightweight config-reading, not a service call, and moving it would violate the first-pass rule

**AC-7 — Build succeeds**
Given the refactor is complete
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with exit code 0

---

## Dev Notes — Implementation

### Overview of changes

Six files across two projects (`mmria-server` and `mmria.common`):

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/SystemOffline/DAL/SystemOfflineDAL.cs` | **CREATE** — two HTTP service call methods |
| `mmria.common/SharedLibraries/SystemOffline/Manager/SystemOfflineManager.cs` | **CREATE** — delegates to DAL; owns message-substitution logic |
| `mmria-server/Controllers/system_offlineController.cs` | **UPDATE** — remove `_couchDbHttpClient` field; inject `SystemOfflineManager`; delegate both service calls |
| `mmria-server/util/SystemOfflineMessageFormatter.cs` | **DELETE** |
| `mmria-server/Program.cs` | **UPDATE** — add two `AddScoped` lines |
| `mmria.common/mmria.common.csproj` _(or equivalent)_ | **VERIFY** — `SystemOffline/` subfolder is automatically included; no manual file registration needed |

---

### Step 1 — Create SystemOfflineDAL

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/SystemOffline/DAL/SystemOfflineDAL.cs`

This is a _service-call DAL_ — it calls `mmria.services` HTTP endpoints (not CouchDB directly). The pattern is identical to how other DALs call external services via `CouchDbHttpClient.ExecuteAsync`.

```csharp
using System;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.metadata;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.SystemOffline.DAL;

public class SystemOfflineDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public SystemOfflineDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<SystemOfflineConfig> LoadConfigAsync(
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
    {
        var url = $"{servicesBaseUrl}/api/systemOffline/GetSystemOfflineConfig";
        var responseBody = await _couchDbHttpClient.ExecuteAsync(
            "GET", url, null, "application/json", requestOptions);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<SystemOfflineConfig>(responseBody)
            ?? new SystemOfflineConfig();
    }

    public async Task<document_put_response> SaveConfigAsync(
        SystemOfflineConfig config,
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
    {
        var url = $"{servicesBaseUrl}/api/systemOffline/SaveSystemOfflineConfig";
        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, settings);
        var responseBody = await _couchDbHttpClient.ExecuteAsync(
            "POST", url, json, "application/json", requestOptions);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(responseBody)
            ?? new document_put_response { ok = false };
    }
}
```

---

### Step 2 — Create SystemOfflineManager

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/SystemOffline/Manager/SystemOfflineManager.cs`

Owns two duties: delegate service calls through the DAL, and own message-substitution logic (moved from `SystemOfflineMessageFormatter`).

```csharp
using System;
using System.Globalization;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.metadata;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.SystemOffline.DAL;

namespace mmria.common.SharedLibraries.SystemOffline.Manager;

/// <summary>
/// Manager for system offline feature.
/// Delegates CouchDB/service calls to SystemOfflineDAL.
/// Owns message-substitution logic (previously in mmria.server.util.SystemOfflineMessageFormatter).
/// NO outer try/catch — the controller owns error surfacing.
/// </summary>
public class SystemOfflineManager
{
    private readonly SystemOfflineDAL _dal;

    public SystemOfflineManager(SystemOfflineDAL dal)
    {
        _dal = dal;
    }

    public Task<SystemOfflineConfig> LoadConfigAsync(
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
        => _dal.LoadConfigAsync(servicesBaseUrl, requestOptions);

    public Task<document_put_response> SaveConfigAsync(
        SystemOfflineConfig config,
        string servicesBaseUrl,
        CouchDbRequestOptions requestOptions)
        => _dal.SaveConfigAsync(config, servicesBaseUrl, requestOptions);

    /// <summary>
    /// Substitutes template tokens in a system-offline message string.
    /// Moved from mmria.server.util.SystemOfflineMessageFormatter — logic is unchanged.
    /// Tokens: {{warn_date}}, {{offline_date}}, {{outage_duration}}, {{estimated_restoration}}
    /// </summary>
    public string SubstituteMessage(string message, string warnDateUtc, string offlineDateUtc, int restorationHours = 2)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var warnDate    = ParseUtc(warnDateUtc);
        var offlineDate = ParseUtc(offlineDateUtc);

        message = message.Replace("{{warn_date}}",
            warnDate.HasValue ? FormatLocal(warnDate.Value) : "(not set)");
        message = message.Replace("{{offline_date}}",
            offlineDate.HasValue ? FormatLocal(offlineDate.Value) : "(not set)");
        message = message.Replace("{{outage_duration}}",
            FormatSpan(TimeSpan.FromHours(restorationHours)));
        message = message.Replace("{{estimated_restoration}}",
            offlineDate.HasValue ? FormatLocal(offlineDate.Value.AddHours(restorationHours)) : "(not set)");

        return message;
    }

    private static DateTime? ParseUtc(string utcStr)
    {
        if (string.IsNullOrWhiteSpace(utcStr)) return null;
        if (!DateTime.TryParse(utcStr, null, DateTimeStyles.RoundtripKind, out var dt)) return null;
        return dt;
    }

    private static string FormatLocal(DateTime utcDt)
        => utcDt.ToLocalTime().ToString("MMMM d, yyyy 'at' h:mm tt");

    private static string FormatSpan(TimeSpan span)
    {
        var totalMinutes = Math.Abs(span.TotalMinutes);
        if (totalMinutes < 60)
        {
            var m = (int)Math.Round(totalMinutes);
            return m == 1 ? "1 minute" : $"{m} minutes";
        }
        var totalHours = Math.Abs(span.TotalHours);
        if (totalHours < 24)
        {
            var h = (int)Math.Round(totalHours);
            return h == 1 ? "1 hour" : $"{h} hours";
        }
        var days = (int)Math.Round(totalHours / 24);
        return days == 1 ? "1 day" : $"{days} days";
    }
}
```

---

### Step 3 — Update system_offlineController.cs

**File:** `source-code/mmria/mmria-server/Controllers/system_offlineController.cs`

Three changes:
1. Remove `_couchDbHttpClient` field and its `using mmria.server.util;` import
2. Inject `SystemOfflineManager` via constructor
3. Rewrite `LoadConfigFromServicesAsync()` to delegate; rewrite `SaveConfig()` to delegate; update `GetStatus()` to call `_manager.SubstituteMessage(...)` instead of `SystemOfflineMessageFormatter.Substitute(...)`

**New constructor and field section (replace existing):**

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mmria.common.getset;
using mmria.common.SharedLibraries.SystemOffline.Manager;

namespace mmria.server.Controllers;

[Route("system-offline/{action=Index}")]
public sealed class system_offlineController : Controller
{
    private readonly mmria.common.couchdb.ConfigurationSet ConfigDB;
    private readonly mmria.common.couchdb.OverridableConfiguration configuration;
    private readonly string host_prefix;
    private readonly SystemOfflineManager _manager;

    public system_offlineController(
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        SystemOfflineManager manager)
    {
        ConfigDB = tenantRuntime.RequireConfigurationSet();
        _manager = manager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
    }
```

**Rewritten SaveConfig() action:**

```csharp
    [Authorize(Roles = "installation_admin")]
    [HttpPost]
    public async Task<IActionResult> SaveConfig()
    {
        var result = new mmria.common.model.couchdb.document_put_response { ok = false };

        try
        {
            var request = await JsonRequestBodyReader.ReadAsync<mmria.common.metadata.SystemOfflineConfig>(Request);

            var selectedJurisdictions = request?.selected_jurisdictions ?? new System.Collections.Generic.List<string>();

            // Sanitize: discard any client-supplied _rev and data_type.
            var sanitized = new mmria.common.metadata.SystemOfflineConfig
            {
                _rev = null,
                warn_date = request?.warn_date,
                warn_message = request?.warn_message,
                offline_date = request?.offline_date,
                offline_modal_message = request?.offline_modal_message,
                offline_page_message = request?.offline_page_message,
                apply_to_all_jurisdictions = request?.apply_to_all_jurisdictions ?? true,
                selected_jurisdictions = selectedJurisdictions,
                restoration_hours = request?.restoration_hours ?? 2,
                auto_logout_minutes = request?.auto_logout_minutes ?? 5
            };

            var requestOptions = new CouchDbRequestOptions
            {
                VitalServiceKey = ConfigDB.name_value["vital_service_key"]
            };

            result = await _manager.SaveConfigAsync(sanitized, GetServicesBaseUrl(), requestOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"system_offlineController.SaveConfig error: {ex}");
        }

        return EscapedJsonResultFactory.Create(result);
    }
```

**Rewritten LoadConfigFromServicesAsync() helper:**

```csharp
    private async Task<mmria.common.metadata.SystemOfflineConfig> LoadConfigFromServicesAsync()
    {
        try
        {
            var requestOptions = new CouchDbRequestOptions
            {
                VitalServiceKey = ConfigDB.name_value["vital_service_key"]
            };
            return await _manager.LoadConfigAsync(GetServicesBaseUrl(), requestOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"system_offlineController.LoadConfigFromServicesAsync error: {ex}");
            return new mmria.common.metadata.SystemOfflineConfig();
        }
    }
```

**Rewritten GetStatus() — replace `SystemOfflineMessageFormatter.Substitute(...)` calls:**

```csharp
    [Authorize]
    [HttpGet]
    [Route("~/api/system-offline/status")]
    public async Task<IActionResult> GetStatus()
    {
        var config = await LoadConfigFromServicesAsync();

        if (!config.apply_to_all_jurisdictions)
        {
            var selected = config.selected_jurisdictions ?? new System.Collections.Generic.List<string>();
            var isSelected = selected.Contains(host_prefix, StringComparer.OrdinalIgnoreCase);
            if (!isSelected)
            {
                return EscapedJsonResultFactory.Create(new
                {
                    warn_date = (string)null,
                    offline_date = (string)null,
                    warn_message = (string)null,
                    offline_modal_message = (string)null,
                    offline_page_message = (string)null
                });
            }
        }

        var status = new
        {
            config.warn_date,
            config.offline_date,
            config.auto_logout_minutes,
            warn_message          = _manager.SubstituteMessage(config.warn_message,          config.warn_date, config.offline_date, config.restoration_hours),
            offline_modal_message = _manager.SubstituteMessage(config.offline_modal_message, config.warn_date, config.offline_date, config.restoration_hours),
            offline_page_message  = _manager.SubstituteMessage(config.offline_page_message,  config.warn_date, config.offline_date, config.restoration_hours)
        };
        return EscapedJsonResultFactory.Create(status);
    }
```

> ⚠️ `GetConfig()` and `GetJurisdictions()` are unchanged. `GetServicesBaseUrl()` is unchanged.

---

### Step 4 — Update Program.cs DI Registration

**File:** `source-code/mmria/mmria-server/Program.cs`

After the existing `builder.Services.AddScoped<mmria.common.SharedLibraries.CaseValidation.Manager.CaseValidationManager>();` line (currently the last `AddScoped` feature registration), add:

```csharp
            builder.Services.AddScoped<mmria.common.SharedLibraries.SystemOffline.DAL.SystemOfflineDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.SystemOffline.Manager.SystemOfflineManager>();
```

---

### Step 5 — Delete SystemOfflineMessageFormatter.cs

**File:** `source-code/mmria/mmria-server/util/SystemOfflineMessageFormatter.cs`

Delete this file. No other files in `mmria-server` reference it after the controller is updated in Step 3.

**Before deleting:** run a grep to confirm zero remaining references:
```
grep -r "SystemOfflineMessageFormatter" source-code/
```
Expected result: zero matches after the controller update.

---

### Architecture Guardrails

Per `project-context.md §2.2`:
- **No outer `try/catch` in Manager or DAL** — error surfacing stays in the controller. The `LoadConfigFromServicesAsync()` wrapper on the controller retains its `try/catch`.
- **Tenant resolution stays in the controller.** `host_prefix`, `configuration`, and `ConfigDB` are NOT injected into Manager. They are resolved in the controller and passed as resolved values (`servicesBaseUrl`, `requestOptions`).
- **No route or response shape changes.** AC-4 is a hard constraint — the client JS must not require any changes.
- **`GetJurisdictions()` key filtering stays in the controller.** It is lightweight in-memory config reading, not a service call.

### Reference Pattern

DI registration at `Program.cs` ~line 327:
```csharp
builder.Services.AddScoped<mmria.common.SharedLibraries.SystemOffline.DAL.SystemOfflineDAL>();
builder.Services.AddScoped<mmria.common.SharedLibraries.SystemOffline.Manager.SystemOfflineManager>();
```
Pattern matches: `ManageUsersDAL`/`ManageUsersManager`, `CVSDAL`/`CVSManager`, `CaseValidationDAL`/`CaseValidationManager`.

### Verification

```
dotnet build source-code/mmria/mmria-server/mmria-server.csproj
```
Expected: exit code 0, no errors.

---

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6 (GitHub Copilot — bmad-agent-architect mode)

### Debug Log References

### Completion Notes List

- Story spec assumed no references to `SystemOfflineMessageFormatter` outside `system_offlineController.cs`. `AccountController.cs` also called the static class (lines 112, 136) — injected `SystemOfflineManager` there as well and replaced static calls. This was the minimal scope expansion needed to satisfy AC-2 (file deleted) without breaking the build.
- Debug DLL lock prevented `dotnet build` in Debug config (server was running under debug adapter); confirmed clean compile via Release config build (`Build succeeded`).

### File List

- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/SystemOffline/DAL/SystemOfflineDAL.cs` — CREATED
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/SystemOffline/Manager/SystemOfflineManager.cs` — CREATED
- `source-code/mmria/mmria-server/Controllers/system_offlineController.cs` — UPDATED
- `source-code/mmria/mmria-server/Controllers/AccountController.cs` — UPDATED (inject SystemOfflineManager; replace two static Substitute calls)
- `source-code/mmria/mmria-server/Program.cs` — UPDATED (added two AddScoped lines)
- `source-code/mmria/mmria-server/util/SystemOfflineMessageFormatter.cs` — DELETED
