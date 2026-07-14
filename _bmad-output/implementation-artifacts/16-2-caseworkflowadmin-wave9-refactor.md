# Story 16.2 — CaseWorkflowAdmin Wave 9 Refactor

**Epic:** 16 — Controller Pattern Remediation
**Story ID:** 16.2
**Status:** done
**Date added:** 2026-07-14
**Depends on:** none (independent of 16.1)
**Source requirements:** project-context.md §2.2; controller_sharedlibraries_migration_matrix.md Wave 9

---

## User Story

As a developer maintaining the case administration workflow,
I want `clear_case_status.cs` and `recover_deleted_case.cs` to delegate their CouchDB work through a `CaseWorkflowAdmin` Manager and DAL,
So that these controllers follow the SharedLibraries pattern and the audit-write code added by Epic 7 is in the correct layer.

---

## Acceptance Criteria

**AC-1 — clear_case_status CouchDB calls extracted**
Given `Controllers/clear_case_status.cs` calls `_couchDbHttpClient.ExecuteAsync(...)` directly at multiple points (case view query in `FindRecord`, case GET + case PUT + audit PUT in `ClearCaseStatus`)
When this story is complete
Then all four CouchDB calls — including the audit-write added by Story 7.2 — have been moved into `SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs`; the controller delegates via `SharedLibraries/CaseWorkflowAdmin/Manager/CaseWorkflowAdminManager.cs`

**AC-2 — recover_deleted_case CouchDB calls extracted**
Given `Controllers/recover_deleted_case.cs` calls `_couchDbHttpClient.ExecuteAsync(...)` directly at multiple points (deleted-case audit view, audit doc GET, revisions GET, revision-specific GET, case PUT, audit DELETE, audit PUT) in `FindRecord` and `UpdateDeletedCase`
When this story is complete
Then all seven CouchDB calls have been moved into `CaseWorkflowAdminDAL`; the controller delegates via `CaseWorkflowAdminManager`

**AC-3 — First-pass rules followed exactly**
Given the migration matrix rates both controllers as Wave 9 `planned` with High risk
When the refactor is implemented
Then per project-context.md §2.2: tenant resolution (`host_prefix`, `configuration`, `db_config`, `_dbConfigSet`), `AuthorizedWorkflowScopeHelper` calls (which need `User`), `User.Identities` resolution, `View()`, `[Bind(...)]`, and `TempData` all stay in the controller; no outer `try/catch` blocks are added in Manager or DAL methods

**AC-4 — URL building uses Get_Prefix_DB_Url**
Given `ConfigurationSet.detail_list` is accessed in the controllers
When the Manager or DAL builds a database URL
Then `dbConfig.Get_Prefix_DB_Url(path)` is used — never a hand-assembled `$"{url}/{prefix}..."` pattern; `detail_list` is always accessed via `TryGetValue` — never the direct indexer

**AC-5 — Epic 7 audit writes move to Manager**
Given `Change_Stack` audit writes were added by Story 7.2 directly into the controller bodies of `ClearCaseStatus` and `UpdateDeletedCase`
When this refactor is complete
Then those audit writes have been moved into `CaseWorkflowAdminManager` alongside the rest of the business logic

**AC-6 — No external-facing changes**
Given `clear_case_statusController` MVC view actions and `recover_deleted_caseController` MVC view actions
When the refactor is complete
Then route paths, action signatures, HTTP method attributes, view names, `ViewBag` keys, and response shapes are identical to pre-refactor

**AC-7 — Build succeeds**
Given the refactor is complete
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with exit code 0

---

## Dev Notes — Implementation

### Overview of Changes

Five files across two projects:

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs` | **CREATE** — all CouchDB calls for both controllers |
| `mmria.common/SharedLibraries/CaseWorkflowAdmin/Manager/CaseWorkflowAdminManager.cs` | **CREATE** — business logic and audit-write orchestration |
| `mmria-server/Controllers/clear_case_status.cs` | **UPDATE** — remove `_couchDbHttpClient`; inject manager; delegate 4 calls |
| `mmria-server/Controllers/recover_deleted_case.cs` | **UPDATE** — remove `_couchDbHttpClient`; inject manager; delegate 7 calls |
| `mmria-server/Program.cs` | **UPDATE** — add 2 `AddScoped` lines |

---

### Current State — CouchDB calls inventory

#### clear_case_status.cs

**`FindRecord()` — 1 call:**
```csharp
// CouchDB call #1 — case view query
string request_string = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true";
responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
// → Deserializes to mmria.common.model.couchdb.case_view_response
```

**`ClearCaseStatus()` — 3 calls:**
```csharp
// CouchDB call #2 — fetch case document
string request_string = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}mmrds/{model._id}";
responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
// → Deserializes to System.Dynamic.ExpandoObject; business logic mutates it

// CouchDB call #3 — PUT updated case document
string put_request_string = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}mmrds/{model._id}";
responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", put_request_string, object_string, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
// → Deserializes to document_put_response; if ok, write audit

// CouchDB call #4 — audit write (Story 7.2)
await _couchDbHttpClient.ExecuteAsync("PUT",
    $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}audit/{auditEntry._id}",
    auditString, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
```

#### recover_deleted_case.cs

**`FindRecord()` — 1 call:**
```csharp
// CouchDB call #1 — deleted-cases audit view
string request_string = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true";
responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
```

**`UpdateDeletedCase()` — 6 calls:**
```csharp
// CouchDB call #2 — fetch audit document
string audit_url = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}audit/{Model._id}";
var audit_response = await _couchDbHttpClient.ExecuteAsync("GET", audit_url, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
// → Deserializes to Change_Stack

// CouchDB call #3 — get case revisions (for current _rev)
string get_revs_url = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}mmrds/{audit_object.case_id}?revs=true&open_revs=all";
var get_revs_curl_response = await _couchDbHttpClient.ExecuteAsync("GET", get_revs_url, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
// → String-parsed to extract current _rev

// CouchDB call #4 — get case at deleted revision
string get_case_url = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}mmrds/{audit_object.case_id}?rev={audit_object.delete_rev}";
var get_case_response = await _couchDbHttpClient.ExecuteAsync("GET", get_case_url, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
// → Deserializes to ExpandoObject; _rev removed, dates updated

// CouchDB call #5 — PUT restored case
string put_case_url = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}mmrds/{audit_object.case_id}";
var put_case_response = await _couchDbHttpClient.ExecuteAsync("PUT", put_case_url, put_case_object_string, effectiveDbConfig.user_name, effectiveDbConfig.user_value);

// CouchDB call #6 — DELETE audit tombstone (only if PUT succeeded)
string delete_audit_url = $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}audit/{Model._id}?rev={audit_object._rev}";
var delete_response = await _couchDbHttpClient.ExecuteAsync("DELETE", delete_audit_url, null, effectiveDbConfig.user_name, effectiveDbConfig.user_value);

// CouchDB call #7 — audit write (Story 7.2)
await _couchDbHttpClient.ExecuteAsync("PUT",
    $"{effectiveDbConfig.url}/{effectiveDbConfig.prefix}audit/{auditEntry._id}",
    auditString, effectiveDbConfig.user_name, effectiveDbConfig.user_value);
```

---

### Step 1 — Create CaseWorkflowAdminDAL

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseWorkflowAdmin/DAL/CaseWorkflowAdminDAL.cs`

> All URL building uses `dbConfig.Get_Prefix_DB_Url(path)` — never hand-assembled strings.
> No outer try/catch — callers (Manager or controller) own error surfacing.

```csharp
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;

namespace mmria.common.SharedLibraries.CaseWorkflowAdmin.DAL;

public class CaseWorkflowAdminDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public CaseWorkflowAdminDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    // ── clear_case_status: FindRecord ────────────────────────────────────

    public async Task<case_view_response> GetCasesByDateAsync(DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=25000&descending=true");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(response)
            ?? new case_view_response();
    }

    // ── clear_case_status: ClearCaseStatus ───────────────────────────────

    public async Task<System.Dynamic.ExpandoObject> GetCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(response);
    }

    public async Task<document_put_response> UpdateCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId, string caseJson)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
        var response = await _couchDbHttpClient.ExecuteAsync("PUT", url, caseJson, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(response)
            ?? new document_put_response { ok = false };
    }

    public async Task WriteAuditEntryAsync(DBConfigurationDetail dbConfig, Change_Stack auditEntry)
    {
        var settings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(auditEntry, settings);
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditEntry._id}");
        await _couchDbHttpClient.ExecuteAsync("PUT", url, json, dbConfig.user_name, dbConfig.user_value);
    }

    // ── recover_deleted_case: FindRecord ─────────────────────────────────

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)
    {
        var url = dbConfig.Get_Prefix_DB_Url("audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<get_sortable_view_reponse_header<Audit_Detail_View>>(response)
            ?? new get_sortable_view_reponse_header<Audit_Detail_View>();
    }

    // ── recover_deleted_case: UpdateDeletedCase ───────────────────────────

    public async Task<Change_Stack> GetAuditDocumentAsync(DBConfigurationDetail dbConfig, string auditId)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditId}");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Change_Stack>(response);
    }

    public async Task<string> GetCaseRevisionsRawAsync(DBConfigurationDetail dbConfig, string caseId)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?revs=true&open_revs=all");
        return await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
    }

    public async Task<System.Dynamic.ExpandoObject> GetCaseAtRevisionAsync(DBConfigurationDetail dbConfig, string caseId, string revision)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}?rev={revision}");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(response);
    }

    public async Task<document_put_response> RestoreCaseDocumentAsync(DBConfigurationDetail dbConfig, string caseId, string caseJson)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"mmrds/{caseId}");
        var response = await _couchDbHttpClient.ExecuteAsync("PUT", url, caseJson, dbConfig.user_name, dbConfig.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(response)
            ?? new document_put_response { ok = false };
    }

    public async Task DeleteAuditDocumentAsync(DBConfigurationDetail dbConfig, string auditId, string rev)
    {
        var url = dbConfig.Get_Prefix_DB_Url($"audit/{auditId}?rev={rev}");
        await _couchDbHttpClient.ExecuteAsync("DELETE", url, null, dbConfig.user_name, dbConfig.user_value);
    }
}
```

---

### Step 2 — Create CaseWorkflowAdminManager

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseWorkflowAdmin/Manager/CaseWorkflowAdminManager.cs`

Business logic that currently lives in the two controllers, including the Epic 7 audit writes. No outer `try/catch` — controller retains its existing `catch` blocks.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.CaseWorkflowAdmin.DAL;

namespace mmria.common.SharedLibraries.CaseWorkflowAdmin.Manager;

/// <summary>
/// Manager for case workflow admin operations (clear status, recover deleted).
/// Contains business logic moved from clear_case_status.cs and recover_deleted_case.cs.
/// Audit writes (Story 7.2) live here — not in controllers.
/// NO outer try/catch — controllers own error surfacing.
/// </summary>
public class CaseWorkflowAdminManager
{
    private readonly CaseWorkflowAdminDAL _dal;

    public CaseWorkflowAdminManager(CaseWorkflowAdminDAL dal)
    {
        _dal = dal;
    }

    // ── clear_case_status ────────────────────────────────────────────────

    public async Task<case_view_response> GetCasesByDateAsync(DBConfigurationDetail dbConfig)
        => await _dal.GetCasesByDateAsync(dbConfig);

    /// <summary>
    /// Fetches the case document, clears case_status.overall_case_status to 9999,
    /// updates last_updated_by / date_last_updated, PUTs the case back, and on success
    /// writes the Change_Stack audit entry. Returns (ok, oldCaseStatus).
    /// </summary>
    public async Task<(bool ok, string oldCaseStatus, string errorMessage)> ClearCaseStatusAsync(
        DBConfigurationDetail dbConfig,
        string caseId,
        string userName)
    {
        var caseDoc = await _dal.GetCaseDocumentAsync(dbConfig, caseId);
        var dictionary = caseDoc as IDictionary<string, object>;
        if (dictionary == null)
            return (false, "", "Case document could not be parsed.");

        if (!dictionary.TryGetValue("home_record", out var homeRecordObj) || homeRecordObj is not IDictionary<string, object> home_record)
            return (false, "", "home_record not found in case document.");

        if (!home_record.TryGetValue("case_status", out var caseStatusObj) || caseStatusObj is not IDictionary<string, object> case_status)
            return (false, "", "case_status not found in home_record.");

        var oldCaseStatus = case_status.TryGetValue("overall_case_status", out var existing) ? existing?.ToString() ?? "" : "";
        case_status["overall_case_status"] = 9999;
        case_status["case_locked_date"] = "";
        dictionary["last_updated_by"] = userName;
        dictionary["date_last_updated"] = DateTime.Now;

        var settings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        var caseJson = Newtonsoft.Json.JsonConvert.SerializeObject(caseDoc, settings);

        var putResult = await _dal.UpdateCaseDocumentAsync(dbConfig, caseId, caseJson);
        if (!putResult.ok)
            return (false, oldCaseStatus, "PUT case document returned ok=false.");

        var auditEntry = new Change_Stack
        {
            _id = Guid.NewGuid().ToString(),
            case_id = caseId,
            user_name = userName,
            note = "admin change, case unlocked, case status cleared",
            old_value = oldCaseStatus,
            new_value = "",
            date_created = DateTime.UtcNow,
            doc_type = "Change_Stack"
        };
        try
        {
            await _dal.WriteAuditEntryAsync(dbConfig, auditEntry);
        }
        catch (Exception auditEx)
        {
            Console.WriteLine($"CaseWorkflowAdminManager.ClearCaseStatusAsync audit write failed: {auditEx.Message}");
        }

        return (true, oldCaseStatus, null);
    }

    // ── recover_deleted_case ─────────────────────────────────────────────

    public async Task<get_sortable_view_reponse_header<Audit_Detail_View>> GetDeletedCasesViewAsync(DBConfigurationDetail dbConfig)
        => await _dal.GetDeletedCasesViewAsync(dbConfig);

    /// <summary>
    /// Fetches audit doc, resolves current _rev via open_revs, fetches the deleted
    /// revision, PUTs the case back, DELETEs the tombstone, and writes audit entry.
    /// Returns (ok, errorMessage). Controller retains its outer try/catch.
    /// </summary>
    public async Task<(bool ok, string errorMessage)> RecoverDeletedCaseAsync(
        DBConfigurationDetail dbConfig,
        string auditId,
        string userName)
    {
        var auditDoc = await _dal.GetAuditDocumentAsync(dbConfig, auditId);

        var revisionsRaw = await _dal.GetCaseRevisionsRawAsync(dbConfig, auditDoc.case_id);
        var startIndex = revisionsRaw.IndexOf("_rev");
        var endIndex = revisionsRaw.IndexOf(",", startIndex);
        var currentRev = revisionsRaw.Substring(startIndex, endIndex - startIndex)
            .Replace("\"", "").Replace("_rev:", "");

        var caseDoc = await _dal.GetCaseAtRevisionAsync(dbConfig, auditDoc.case_id, auditDoc.delete_rev);
        var resultDictionary = caseDoc as IDictionary<string, object>;
        if (resultDictionary == null)
            return (false, "Case document at deleted revision could not be parsed.");

        if (resultDictionary.ContainsKey("_rev"))
            resultDictionary.Remove("_rev");

        resultDictionary["date_last_updated"] = DateTime.Now;
        resultDictionary["last_updated_by"] = userName;

        var settings = new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore };
        var caseJson = Newtonsoft.Json.JsonConvert.SerializeObject(caseDoc, settings);

        var putResult = await _dal.RestoreCaseDocumentAsync(dbConfig, auditDoc.case_id, caseJson);
        if (!putResult.ok)
            return (false, "PUT restore case returned ok=false.");

        await _dal.DeleteAuditDocumentAsync(dbConfig, auditId, auditDoc._rev);

        var auditEntry = new Change_Stack
        {
            _id = Guid.NewGuid().ToString(),
            case_id = auditDoc.case_id,
            user_name = userName,
            note = "admin change, case recovered",
            date_created = DateTime.UtcNow,
            doc_type = "Change_Stack"
        };
        try
        {
            await _dal.WriteAuditEntryAsync(dbConfig, auditEntry);
        }
        catch (Exception auditEx)
        {
            Console.WriteLine($"CaseWorkflowAdminManager.RecoverDeletedCaseAsync audit write failed: {auditEx.Message}");
        }

        return (true, null);
    }
}
```

> ⚠️ **Note on `currentRev`:** The revision-parsing logic (`IndexOf("_rev")`, string `.Substring`, `.Replace`) is moved verbatim from the controller into the Manager. It is fragile string-parsing of CouchDB's multipart response. Do not refactor or improve it — preserve it exactly to avoid regressions.

---

### Step 3 — Update clear_case_status.cs

**File:** `source-code/mmria/mmria-server/Controllers/clear_case_status.cs`

Three changes:
1. Replace `_couchDbHttpClient` field with `_manager` field; update constructor
2. Rewrite `FindRecord()` to call `_manager.GetCasesByDateAsync(effectiveDbConfig)`
3. Rewrite `ClearCaseStatus()` to call `_manager.ClearCaseStatusAsync(effectiveDbConfig, model._id, userName)`

**New field + constructor (replace existing):**
```csharp
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using mmria.server.extension;
using mmria.server.util;
using mmria.common.SharedLibraries.CaseWorkflowAdmin.Manager;

namespace mmria.server.Controllers;

[Authorize(Roles = "cdc_admin,jurisdiction_admin")]
public sealed class clear_case_statusController : Controller
{
    private readonly IAuthorizationService _authorizationService;
    private readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly CaseWorkflowAdminManager _manager;
    private readonly System.Collections.Generic.Dictionary<string, string> CaseStatusToDisplay;

    public clear_case_statusController(
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        CaseWorkflowAdminManager manager)
    {
        _dbConfigSet = tenantRuntime.RequireConfigurationSet();
        _manager = manager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
        if (_dbConfigSet.detail_list.ContainsKey("vital_import"))
            _dbConfigSet.detail_list.Remove("vital_import");

        CaseStatusToDisplay = new Dictionary<string, string>
        {
            ["9999"] = "(blank)",
            ["1"]    = "Abstracting (Incomplete)",
            ["2"]    = "Abstraction Complete",
            ["3"]    = "Ready for Review",
            ["4"]    = "Review Complete and Decision Entered",
            ["5"]    = "Out of Scope and Death Certificate Entered",
            ["6"]    = "False Positive and Death Certificate Entered",
            ["0"]    = "Vitals Import"
        };
    }
```

**Rewritten FindRecord() — replace the body after model/effectiveDbConfig setup:**
```csharp
    [HttpPost]
    public async Task<IActionResult> FindRecord(
        [Bind(
            nameof(mmria.server.model.casestatus.CaseStatusRequest.StateDatabase) + "," +
            nameof(mmria.server.model.casestatus.CaseStatusRequest.RecordId))]
        mmria.server.model.casestatus.CaseStatusRequest Model)
    {
        Model ??= new mmria.server.model.casestatus.CaseStatusRequest();
        var model = new mmria.server.model.casestatus.CaseStatusRequestResponse();
        model.SearchText = Model.RecordId;
        TempData["SearchText"] = model.SearchText;
        try
        {
            var isCdcAdmin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
            var effectiveRole = isCdcAdmin ? "cdc_admin" : "jurisdiction_admin";
            var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
            var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, Model.StateDatabase, host_prefix, db_config, _dbConfigSet);
            model.is_cdc_admin = isCdcAdmin;

            var caseViewResponse = await _manager.GetCasesByDateAsync(effectiveDbConfig);
            var lockedStatusList = new List<int> { 4, 5, 6 };

            foreach (var item in caseViewResponse.rows)
            {
                try
                {
                    if (item.value.record_id != null &&
                        !string.IsNullOrWhiteSpace(Model.RecordId) &&
                        (item.value.record_id.IndexOf(Model.RecordId, StringComparison.OrdinalIgnoreCase) > -1 ||
                         Model.RecordId.IndexOf(item.value.record_id, StringComparison.OrdinalIgnoreCase) > -1))
                    {
                        model.CaseStatusDetail.Add(new mmria.server.model.casestatus.CaseStatusDetail
                        {
                            _id                = item.id,
                            RecordId           = item.value?.record_id,
                            FirstName          = item.value?.first_name,
                            LastName           = item.value?.last_name,
                            MiddleName         = item.value?.middle_name,
                            DateOfDeath        = $"{item.value?.date_of_death_month}/{item.value.date_of_death_year}",
                            StateOfDeath       = item.value?.host_state,
                            AgencyCaseId       = item.value?.agency_case_id,
                            LocalFileNumber    = item.value?.local_file_number,
                            StateFileNumber    = item.value?.state_file_number,
                            LastUpdatedBy      = item.value?.last_updated_by,
                            DateLastUpdated    = item.value?.date_last_updated,
                            CaseStatus         = item.value.case_status,
                            CaseStatusDisplay  = (item.value.case_status != null && CaseStatusToDisplay.ContainsKey(item.value.case_status.ToString()))
                                                    ? CaseStatusToDisplay[item.value.case_status.ToString()]
                                                    : "(blank)",
                            StateDatabase      = effectiveStateDatabase,
                            is_cdc_admin       = isCdcAdmin,
                            Role               = effectiveRole
                        });
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }
        catch (Exception ex) { Console.WriteLine(ex); }

        return View(model);
    }
```

**Rewritten ClearCaseStatus() — replace the body after model setup:**
```csharp
    [HttpPost]
    public async Task<IActionResult> ClearCaseStatus(
        [Bind( /* same Bind attributes as before — unchanged */ )]
        mmria.server.model.casestatus.CaseStatusDetail Model)
    {
        var model = Model ?? new mmria.server.model.casestatus.CaseStatusDetail();
        var isCdcAdmin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
        var effectiveRole = isCdcAdmin ? "cdc_admin" : "jurisdiction_admin";
        var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, model.StateDatabase, host_prefix, _dbConfigSet);
        var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, model.StateDatabase, host_prefix, db_config, _dbConfigSet);
        model.is_cdc_admin = isCdcAdmin;
        model.Role = effectiveRole;
        model.StateDatabase = effectiveStateDatabase;

        try
        {
            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
                userName = User.Identities.First(u => u.IsAuthenticated && u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;

            var (ok, oldCaseStatus, errorMessage) = await _manager.ClearCaseStatusAsync(effectiveDbConfig, model._id, userName);

            if (ok)
            {
                model.CaseStatusDisplay = "(blank)";
                model.LastUpdatedBy = userName;
                model.DateLastUpdated = DateTime.Now;
            }
            else
            {
                model.CaseStatusDisplay = errorMessage ?? "Problem Setting Status to (blank)";
            }
        }
        catch (Exception ex)
        {
            model.CaseStatusDisplay = ex.ToString();
        }

        return View(model);
    }
```

> ⚠️ `ConfirmClearCaseStatusRequest` is unchanged — it does no CouchDB work.

---

### Step 4 — Update recover_deleted_case.cs

**File:** `source-code/mmria/mmria-server/Controllers/recover_deleted_case.cs`

Same pattern as clear_case_status: replace `_couchDbHttpClient` with `_manager`, rewrite `FindRecord()` and `UpdateDeletedCase()`.

**New field + constructor:**
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using mmria.server.extension;
using mmria.server.util;
using mmria.common.SharedLibraries.CaseWorkflowAdmin.Manager;

namespace mmria.server.Controllers;

[Authorize(Roles = "installation_admin,cdc_admin")]
[Route("recover-deleted-case/{action=Index}")]
public sealed class recover_deleted_caseController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;
    private readonly CaseWorkflowAdminManager _manager;

    public recover_deleted_caseController(
        IHttpContextAccessor httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        CaseWorkflowAdminManager manager)
    {
        _manager = manager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
        _dbConfigSet = tenantRuntime.RequireConfigurationSet();
        if (_dbConfigSet.detail_list.ContainsKey("vital_import"))
            _dbConfigSet.detail_list.Remove("vital_import");
    }
```

**Rewritten FindRecord():**
```csharp
    [HttpPost]
    public async Task<IActionResult> FindRecord(
        [Bind(
            nameof(mmria.server.model.recover_deleted.Request.StateDatabase) + "," +
            nameof(mmria.server.model.recover_deleted.Request.RecordId))]
        mmria.server.model.recover_deleted.Request Model)
    {
        Model ??= new mmria.server.model.recover_deleted.Request();
        var model = new mmria.server.model.recover_deleted.RequestResponse();
        model.SearchText = Model.RecordId;
        try
        {
            var isCdcAdmin = AuthorizedWorkflowScopeHelper.IsCdcAdmin(User);
            var effectiveStateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
            var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, Model.StateDatabase, host_prefix, db_config, _dbConfigSet);
            model.is_cdc_admin = isCdcAdmin;

            var auditViewResponse = await _manager.GetDeletedCasesViewAsync(effectiveDbConfig);

            foreach (var item in auditViewResponse.rows)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Model.RecordId) ||
                        (item.value.record_id != null &&
                        (item.value.record_id.IndexOf(Model.RecordId, StringComparison.OrdinalIgnoreCase) > -1 ||
                         Model.RecordId.IndexOf(item.value.record_id, StringComparison.OrdinalIgnoreCase) > -1)))
                    {
                        item.value._id = item.id;
                        item.value.StateDatabase = effectiveStateDatabase;
                        model.Detail.Add(item.value);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }
        catch (Exception ex) { Console.WriteLine(ex); }

        return View(model);
    }
```

**Rewritten UpdateDeletedCase():**
```csharp
    [HttpPost]
    public async Task<IActionResult> UpdateDeletedCase(
        [Bind(
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View._id) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.record_id) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.first_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.last_name) + "," +
            nameof(mmria.common.model.couchdb.audit.Audit_Detail_View.StateDatabase))]
        mmria.common.model.couchdb.audit.Audit_Detail_View Model)
    {
        Model ??= new mmria.common.model.couchdb.audit.Audit_Detail_View();
        Model.StateDatabase = AuthorizedWorkflowScopeHelper.ResolveAuthorizedStateDatabase(User, Model.StateDatabase, host_prefix, _dbConfigSet);
        var result = new UpdateDeletedCaseResult { detail = Model, is_problem_deleting = false };

        try
        {
            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
                userName = User.Identities.First(u => u.IsAuthenticated && u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                    .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;

            var effectiveDbConfig = AuthorizedWorkflowScopeHelper.ResolveAuthorizedDbConfig(User, Model.StateDatabase, host_prefix, db_config, _dbConfigSet);

            var (ok, errorMessage) = await _manager.RecoverDeletedCaseAsync(effectiveDbConfig, Model._id, userName);
            if (!ok)
            {
                result.is_problem_deleting = true;
                result.problem_description = errorMessage;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            result.is_problem_deleting = true;
            result.problem_description = ex.Message;
        }

        return View(result);
    }
```

> ⚠️ `ConfirmRecoverRequest` is unchanged — no CouchDB work.
> ⚠️ `tombstone_struct` struct in recover_deleted_case.cs can be **deleted** — it is populated in the controller but never used after the move.

---

### Step 5 — Update Program.cs DI Registration

**File:** `source-code/mmria/mmria-server/Program.cs`

After the existing `SystemOffline` registrations (or after `CaseValidationManager` if 16.1 is not yet merged), add:

```csharp
            builder.Services.AddScoped<mmria.common.SharedLibraries.CaseWorkflowAdmin.DAL.CaseWorkflowAdminDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.CaseWorkflowAdmin.Manager.CaseWorkflowAdminManager>();
```

---

### Architecture Guardrails

Per `project-context.md §2.2` — enforced in this story:
- **No outer `try/catch` in Manager or DAL.** The audit write is wrapped in a non-propagating `catch` (same as the original controller code) — this is intentional.
- **Tenant resolution stays in controller.** `host_prefix`, `db_config`, `_dbConfigSet`, `AuthorizedWorkflowScopeHelper` calls all remain in the controllers.
- **URL building:** `dbConfig.Get_Prefix_DB_Url(path)` — replaces all hand-assembled `$"{url}/{prefix}..."` strings from the original controllers.
- **`detail_list` access:** The constructors use `ContainsKey` (not `TryGetValue`) for the `vital_import` removal — this is pre-existing defensive code on the controller side and is acceptable.
- **No route or view changes.** `[Authorize(Roles = ...)]`, `[Route(...)]`, `[HttpPost]`, `[Bind(...)]`, view names, `TempData` keys — all unchanged.

### Verification

```
dotnet build source-code/mmria/mmria-server/mmria-server.csproj
```
Expected: exit code 0, no errors.

---

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.5 (Winston — bmad-agent-architect)

### Debug Log References

### Completion Notes List

### File List
