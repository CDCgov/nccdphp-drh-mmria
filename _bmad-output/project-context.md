---
project_name: mmria
user_name: Nick
date: 2026-07-14
sections_completed: [technology_stack, architecture, critical_rules, audit_logging, js_client, offline_mode, cross_repo]
---

# Project Context for AI Agents

_Critical rules and patterns for AI agents working in the mmria codebase. Read before planning or implementing any change. Focus on unobvious constraints that are easy to get wrong._

---

## 0. Start Here — Context Pack Routing

| If you are working on... | Read first |
|---|---|
| Anything — start here | This file, then `docs/ai/AI_CONTEXT.md` |
| Login, session, timeout, SAMS | `docs/ai/authentication_session_timeout.md` |
| Offline mode, service worker, sync | `docs/ai/offline_mode.md` |
| Controller → SharedLibraries refactor | `docs/ai/controller_sharedlibraries_migration_matrix.md` |
| Background jobs, Akka actors, Quartz | `docs/ai/MMRIA_Background_Jobs_Documentation.md` |
| Case summary rendering, pinned cases | `docs/ai/case_summary_rendering_context.md` |
| Playwright / E2E tests | `docs/ai/case_view_edit_playwright_testing_context.md` |
| Aggregate or data summary reports | `docs/ai/aggregate_report.md` or `docs/ai/data_summary_report.md` |
| Multi-tenant rebuild | `docs/ai/multi_tenant_rebuild_process.md` |
| Utilities repo (tests, generators, replication) | `../nccdphp-drh-mmria-utilities/ai/AI_CONTEXT.md` |

---

## 1. Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Web host | .NET MVC — `mmria-server` | .NET 9 |
| Shared libraries | `mmria.common` — `SharedLibraries/<Feature>/Manager+DAL` | .NET 9 |
| Background services | `mmria.services` — Akka.NET + Quartz | Akka 1.5.52, Quartz 3.13.1 |
| Database | CouchDB | Per-jurisdiction tenant databases |
| Client | Vanilla JavaScript — `wwwroot/scripts` | No build step, no bundler |
| Actor framework | Akka.NET + Akka.Hosting | 1.5.52 |
| JSON | Newtonsoft.Json (server), `System.Text.Json` (spot use) | Mixed — check context |
| Serialization | `CaseJsonSerialization` helper wraps strongly-typed case models | Use this, not raw `JsonConvert`, for case documents |

---

## 2. Non-Negotiable Architecture Rules

These override everything else. Never break these without an explicit, written request.

### 2.1 Routes and controller signatures
- **Never change** route paths, controller action signatures, HTTP method attributes (`[HttpGet]`, `[HttpPost]`, etc.), or response shapes unless explicitly requested.
- Do not add or remove `[Bind(...)]` attributes without explicit discussion.

### 2.2 SharedLibraries pattern
- New business logic belongs in `mmria.common/SharedLibraries/<Feature>/Manager/<FeatureManager>.cs`.
- New CouchDB access belongs in `mmria.common/SharedLibraries/<Feature>/DAL/<FeatureDAL>.cs`.
- **Do not add outer `try/catch` blocks in Manager or DAL methods** — the controller owns error surfacing.
- `HttpContext`, `User`, `View()`, `Json()`, `File()`, cookies, headers, actor dispatch, and `TempData` **stay in controllers**.
- On a first migration pass, keep tenant resolution (`host_prefix`, `configuration`, `db_config`, `ConfigurationSet`) in the controller, not the Manager.

### 2.3 Tenant resolution
- Always resolve config via `tenantRuntime.RequireConfiguration()` → `configuration.GetString(key, host_prefix)`.
- `DBConfigurationDetail` carries `url`, `prefix`, `user_name`, `user_value`. Use `db_info.Get_Prefix_DB_Url(path)` to build CouchDB URLs.
- `ConfigurationSet.detail_list` is a `Dictionary<string, DBConfigurationDetail>`. Always use `TryGetValue` — never index directly — because the key may be absent.

### 2.4 Case narrative HTML (FR-1 override)
- **The generated HTML structure stored in `g_data.case_narrative.case_opening_overview` must not be altered.** The PDF and print renderers are tightly coupled to it.
- If security sanitization is needed on the save path, strip executable attributes only (`onclick`, `javascript:` hrefs). Never strip structural tags (`<br>`, `<u>`, `<hr>`, `<font>`).

---

## 3. CouchDB Access Patterns

### 3.1 Null safety on ExpandoObject / Dictionary reads
- `certificate_identification["dmaiden"]` **throws `KeyNotFoundException`** if the key is absent. Always use `TryGetValue`.
- Pattern: `var val = dict.TryGetValue("key", out var v) ? v?.ToString() ?? "" : "";`

### 3.2 String comparisons on metadata types
- Use `string.Equals(x, "VALUE", StringComparison.OrdinalIgnoreCase)` — **never** `.ToUpper() == "VALUE"` — because the value may be null and `.ToUpper()` will throw.

### 3.3 Record ID format
- Record IDs are typically `STATE-YEAR-NNNN` (3 hyphen-separated segments). **Guard `array[2]` with a length check** because server data may not follow that format: `if (array.Length < 3) return recordId;`

### 3.4 CouchDbHttpClient.ExecuteAsync
- Default overload signature: `(method, url, payload, userName, password, contentType, ...)` — `contentType` defaults to `"application/json"`.
- `throwOnError` defaults to `false`. Non-2xx responses return the body as a string rather than throwing.
- Use `dbInfo.Get_Prefix_DB_Url("mmrds/{id}")` to build tenant-prefixed URLs.

---

## 4. Audit Logging (Change_Stack) — Critical

Every admin mutation to a case document must write a `Change_Stack` audit record to the `audit/` CouchDB database.

### 4.1 Required fields on `Change_Stack_Item`
All six fields below are **required**. Missing any causes NullReferenceException in the audit log view:

```csharp
new Change_Stack_Item
{
    user_name        = userName,
    date_created     = DateTime.UtcNow,       // required — view sorts by this
    prompt           = "Human-readable label",
    object_path      = "g_data.path.to.field", // JS object path
    metadata_path    = "/path/to/field",        // slash-prefixed metadata path
    dictionary_path  = "/path/to/field",        // same as metadata_path usually
    metadata_type    = "string",                // field type: "string", "datetime", etc.
    old_value        = previousValue ?? "",
    new_value        = newValue ?? "",
    doc_type         = "Change_Stack_Item"
}
```

### 4.2 Required fields on `Change_Stack`
```csharp
new Change_Stack
{
    _id              = Guid.NewGuid().ToString(),
    case_id          = caseId,
    user_name        = userName,
    note             = "admin change, <description>",
    date_created     = DateTime.UtcNow,
    doc_type         = "Change_Stack",
    items            = new List<Change_Stack_Item> { ... }
}
```

### 4.3 Write pattern
```csharp
JsonSerializerSettings auditSettings = new() { NullValueHandling = NullValueHandling.Ignore };
var auditJson = JsonConvert.SerializeObject(changeStack, auditSettings);
string auditUrl = dbConfig.Get_Prefix_DB_Url($"audit/{changeStack._id}");
try
{
    string auditResponse = await _couchDbHttpClient.ExecuteAsync(
        "PUT", auditUrl, auditJson, dbConfig.user_name, dbConfig.user_value);
    var auditResult = JsonConvert.DeserializeObject<document_put_response>(auditResponse);
    if (auditResult == null || !auditResult.ok)
        Console.WriteLine($"Audit save failed for case {caseId}, audit {changeStack._id}");
}
catch (Exception ex)
{
    Console.WriteLine($"Audit save threw for case {caseId}, audit {changeStack._id}: {ex.Message}");
}
```

### 4.4 Admin functions that must audit
The following admin operations each require a `Change_Stack` entry on success:
- Year of death update (`CaseManager.UpdateYearOfDeathAsync`)
- Maiden name update (`CaseManager.UpdateMaidenNameAsync`)
- Force-release case lock (`CaseManager.ForceReleaseCaseLockAsync`)
- Remove offline lock (`CaseManager.ForceRemoveOfflineLockAsync`)

### 4.5 Audit view guards
`AuditRecoveryManager.DebounceDateTimeField` iterates `Change_Stack_Item.metadata_type`. Use `string.Equals` not `.ToUpper()`. The razor view `Views/_audit/Index.cshtml` renders `change.dictionary_path?.Replace(...)` — use the null-conditional because the field may be absent on older records.

---

## 5. Client-Side JavaScript Rules

- **No build step.** All JS is vanilla, loaded directly by Razor views via `<script src="...">`. Do not introduce npm dependencies, module bundlers, or TypeScript in `wwwroot/`.
- `wwwroot/scripts/case/index.js` is the primary case page entry point (~4500+ lines). Make surgical changes; do not restructure.
- The form renderer pipeline: `index.js → page_renderer.js → chart.js` dispatches by metadata type. `p_post_html_render` callbacks fire after DOM insertion via `eval(post_html_call_back.join('\n'))`.
- `change_stack_items` are built client-side per field change and posted with `Save_Case_Request`. Each item needs `metadata_type`, `dictionary_path`, `prompt`, `old_value`, `new_value`, and `date_created`.

---

## 6. Offline Mode — Key Constraints

- Cases are encrypted client-side with AES-256-GCM + PBKDF2. The crypto key lives only in service-worker memory.
- `offline-integrity-validator.js` is the integrity gate — add new health checks there, not inline.
- When an admin removes an offline lock (`ForceRemoveOfflineLockAsync`), clear all seven fields: `is_offline`, `offline_date`, `offline_by`, `offline_lock_type`, `offline_by_tab_id`, `date_last_checked_out`, `last_checked_out_by`, `checked_out_by_tab_id`.
- The `is_offline` field may be stored as a boolean or as the string `"true"`. Always parse both forms when reading.

---

## 7. Multi-Tenant Specifics

- Each jurisdiction runs its own CouchDB instance. Tenant is resolved from the incoming hostname via `tenantRuntime.EffectiveHostPrefix`.
- Database names follow `{prefix}_mmrds`, `{prefix}_audit`, etc. Use `db_info.Get_Prefix_DB_Url(path)` — never hand-assemble the URL.
- `_dbConfigSet.detail_list["vital_import"]` is stripped in some controller constructors. Never assume `detail_list` contains any specific key.
- `ResolveAuthorizedStateDatabase` returns `hostPrefix` for `jurisdiction_admin` and validates `requestedStateDatabase` exists in `detail_list` for `cdc_admin`.

---

## 8. Cross-Repo Boundary (nccdphp-drh-mmria-utilities)

- Test projects (`mmria-server.tests`) live in the utilities repo but test code in the main repo. Build paths use `${workspaceFolder}/../nccdphp-drh-mmria-utilities/...`.
- Strongly-typed case models are **generated** by `strongly-typed-case` in the utilities repo — do not hand-edit generated files in the main repo.
- Operational utilities (`Replication`, `data-migration`, `mmria-ije-generator`) are never deployed as part of the app — they are admin/DevOps tooling.
- For utilities-repo AI context: `../nccdphp-drh-mmria-utilities/ai/AI_CONTEXT.md`.

---

## 9. Common Failure Modes (Lessons from Implementation)

| Symptom | Root cause | Fix |
|---|---|---|
| NullReferenceException in audit log view | `metadata_type` null on `Change_Stack_Item` | Populate all 6 required fields |
| NullReferenceException in `Index.cshtml` | `dictionary_path` null | Use `?.Replace(...)` in Razor |
| KeyNotFoundException on ExpandoObject | Direct indexer `dict["key"]` on optional field | Use `TryGetValue` |
| KeyNotFoundException on `detail_list` | Direct indexer on `DBConfigurationDetail` dictionary | Use `TryGetValue` |
| IndexOutOfRangeException on record ID split | Assumes 3 segments; server data may differ | Guard with `array.Length < 3` |
| 500 on server, works locally | Unhandled exception in action with no try/catch | Add try/catch; local dev exception page hides it |
| String comparison NullReferenceException | `.ToUpper() == "X"` on nullable string | Use `string.Equals(..., OrdinalIgnoreCase)` |
| Audit loop infinite | `RecordIdExistsAsync` always returns true on CouchDB error | Error handler returns `true`; fix the Mango query |
