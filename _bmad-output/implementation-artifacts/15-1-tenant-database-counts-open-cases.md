# Story 15.1 — Tenant Database Counts: Open Cases Column

**Epic:** 15 — Admin Monitoring Enhancements
**Story ID:** 15.1
**Status:** not-started
**Date added:** 2026-07-09
**Depends on:** none
**Source requirements:** FR-20.1–FR-20.5

---

## User Story

As an installation administrator viewing the Tenant Database Counts page,
I want to see how many cases are currently open for editing per tenant — and which of those look like orphaned checkouts —
So that I can identify edit-lock issues and investigate abnormal activity without needing a separate database query.

---

## Acceptance Criteria

**AC-1 — Active open case count appears per tenant row**
Given the Tenant Database Counts page loads successfully
Then each tenant row in the Counts by Entry table has an "Open Cases" column
And the cell shows the count of MMRDS documents where `checked_out_by_tab_id` is present AND `date_last_updated` is within the past 10 minutes (UTC) at query time
When that count is zero the cell displays `0`

**AC-2 — Possibly-stale count shown in amber when non-zero**
Given a tenant has one or more MMRDS documents where `checked_out_by_tab_id` is present AND `date_last_updated` is more than 10 minutes ago
Then the stale count is shown in amber parentheses after the active count (e.g. `2 (1)`)
When only stale cases exist (active = 0, stale > 0), the cell displays `0 (1)`
When both are zero the cell displays `0` with no parenthetical

**AC-3 — Open Cases summary tile added**
Given the page loads successfully
Then a fifth summary tile labeled "Open Cases" is present in the header row
And the tile shows the system-wide total of active open cases
And when the total possibly-stale count is non-zero, it is shown on a second line in amber text (e.g. `3 possibly stale`)
When both totals are zero the tile shows `0` with no sub-label

**AC-4 — Open case query failure displays `-` and does not affect status**
Given the Mango query for a tenant's MMRDS database fails (timeout, network error, 404)
Then the Open Cases cell for that row displays `-`
And the tenant's `status` field (ok / partial_error / error) is unchanged — open-case errors do not affect it
And the error message is available in `open_case_error` on the response model for diagnostic logging

**AC-5 — 10-minute threshold is a fixed constant**
The active/stale boundary is exactly 10 minutes. It is not read from CouchDB configuration and is not configurable at runtime.

---

## Dev Notes — Implementation

### Overview of changes

Five files across two projects:

| File | Change |
|------|--------|
| `mmria.common/.../Model/TenantDatabaseCountsResponse.cs` | Add 4 fields to models |
| `mmria.common/.../DAL/MMRIAServicesDAL.cs` | Add `GetOpenCaseCountsAsync` (Mango query) |
| `mmria.common/.../Manager/MMRIAServicesManager.cs` | Add `TryGetOpenCaseCountsAsync`, wire into `BuildTenantDatabaseCountEntryAsync` |
| `mmria-server/Controllers/TenantDatabaseCountsController.cs` | Add totals to `TenantDatabaseCountsPageModel` |
| `mmria-server/Views/TenantDatabaseCounts/Index.cshtml` | Add 5th tile + Open Cases column |

---

### Step 1 — Model (`TenantDatabaseCountsResponse.cs`)

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Model/TenantDatabaseCountsResponse.cs`

Add to `TenantDatabaseCountsResponse`:
```csharp
public int total_open_case_count_active { get; set; }
public int total_open_case_count_stale { get; set; }
```

Add to `TenantDatabaseCountEntryResponse`:
```csharp
public int? open_case_count_active { get; set; }
public int? open_case_count_stale { get; set; }
public string open_case_error { get; set; }
```

---

### Step 2 — DAL (`MMRIAServicesDAL.cs`)

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs`

Add a new public method after `GetDesignDocumentCountAsync`. The method POSTs a Mango `_find` query and returns the matched document stubs:

```csharp
public async Task<List<(string id, DateTime? dateLastUpdated)>> GetOpenCaseStubsAsync(
    string databaseUrl,
    string userName,
    string userValue,
    int timeoutSeconds = 20)
{
    string requestUrl = $"{databaseUrl}/_find";
    string requestBody = """
        {
          "selector": { "checked_out_by_tab_id": { "$exists": true } },
          "fields": ["_id", "date_last_updated"],
          "limit": 1000
        }
        """;

    string response = await _couchDbHttpClient.ExecuteAsync(
        "POST",
        requestUrl,
        requestBody,
        userName,
        userValue,
        timeoutSeconds: timeoutSeconds,
        throwOnError: true);

    var result = new List<(string id, DateTime? dateLastUpdated)>();
    var payload = Newtonsoft.Json.Linq.JObject.Parse(response);
    var docs = payload["docs"] as Newtonsoft.Json.Linq.JArray;
    if (docs == null) return result;

    foreach (var doc in docs)
    {
        var id = doc.Value<string>("_id");
        DateTime? dateLastUpdated = null;
        var rawDate = doc["date_last_updated"];
        if (rawDate != null && rawDate.Type != Newtonsoft.Json.Linq.JTokenType.Null)
        {
            if (DateTime.TryParse(rawDate.ToString(), null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                dateLastUpdated = parsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : parsed.ToUniversalTime();
            }
        }
        result.Add((id, dateLastUpdated));
    }

    return result;
}
```

**Note on `throwOnError: true`:** Matches the same flag used in `GetDesignDocumentCountAsync`. The `TryGet*` wrapper in the manager handles the exception and converts it to an error string.

---

### Step 3 — Manager (`MMRIAServicesManager.cs`)

**File:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs`

#### 3a — Add `TryGetOpenCaseCountsAsync` helper

Add alongside `TryGetDesignDocumentCountAsync` (line ~1265):

```csharp
private const int OpenCaseActiveThresholdMinutes = 10;

private async Task<(int? active, int? stale, string error)> TryGetOpenCaseCountsAsync(
    string databaseUrl,
    mmria.common.couchdb.DBConfigurationDetail dbInfo,
    int timeoutSeconds)
{
    try
    {
        var stubs = await _mmriaServicesDal.GetOpenCaseStubsAsync(
            databaseUrl,
            dbInfo.user_name,
            dbInfo.user_value,
            timeoutSeconds);

        var cutoff = DateTime.UtcNow.AddMinutes(-OpenCaseActiveThresholdMinutes);
        int active = 0;
        int stale = 0;

        foreach (var (_, dateLastUpdated) in stubs)
        {
            if (dateLastUpdated.HasValue && dateLastUpdated.Value >= cutoff)
                active++;
            else
                stale++;
        }

        return (active, stale, null);
    }
    catch (Exception ex)
    {
        return (null, null, $"Open case query failed: {ex.Message}");
    }
}
```

#### 3b — Wire into `BuildTenantDatabaseCountEntryAsync`

In `BuildTenantDatabaseCountEntryAsync` (line ~1176), add the new task alongside the existing six tasks:

```csharp
// existing tasks (unchanged):
var mmrdsTask = TryGetDatabaseCountAsync(...);
var deIdTask = TryGetDatabaseCountAsync(...);
var reportTask = TryGetDatabaseCountAsync(...);
var mmrdsDesignDocCountTask = TryGetDesignDocumentCountAsync(...);
var deIdDesignDocCountTask = TryGetDesignDocumentCountAsync(...);
var reportDesignDocCountTask = TryGetDesignDocumentCountAsync(...);

// new task:
var openCaseTask = TryGetOpenCaseCountsAsync(
    dbInfo.Get_Prefix_DB_Url("mmrds"), dbInfo, perDatabaseTimeoutSeconds);

await Task.WhenAll(mmrdsTask, deIdTask, reportTask,
    mmrdsDesignDocCountTask, deIdDesignDocCountTask, reportDesignDocCountTask,
    openCaseTask);
```

After awaiting, unpack and assign:
```csharp
var (openActive, openStale, openError) = await openCaseTask;
entry.open_case_count_active = openActive;
entry.open_case_count_stale = openStale;
entry.open_case_error = openError;
```

#### 3c — Compute response-level totals in `BuildTenantDatabaseCountsResponseAsync`

After `response.entries = results...ToList();` and the existing `ok_entry_count`/`partial_error_entry_count`/`error_entry_count` calculations, add:

```csharp
response.total_open_case_count_active = response.entries
    .Sum(item => item.open_case_count_active ?? 0);
response.total_open_case_count_stale = response.entries
    .Sum(item => item.open_case_count_stale ?? 0);
```

---

### Step 4 — Controller (`TenantDatabaseCountsController.cs`)

**File:** `source-code/mmria/mmria-server/Controllers/TenantDatabaseCountsController.cs`

Add two properties to `TenantDatabaseCountsPageModel`:
```csharp
public int TotalOpenCaseCountActive { get; set; }
public int TotalOpenCaseCountStale { get; set; }
```

In `Index()`, after `model.DeIdMismatchCount = ...` and `model.EntriesWithErrorsCount = ...`, add:
```csharp
model.TotalOpenCaseCountActive = counts?.total_open_case_count_active ?? 0;
model.TotalOpenCaseCountStale = counts?.total_open_case_count_stale ?? 0;
```

---

### Step 5 — View (`Index.cshtml`)

**File:** `source-code/mmria/mmria-server/Views/TenantDatabaseCounts/Index.cshtml`

#### 5a — Add CSS for stale amber color

In the `<style>` block alongside the other `.tenant-counts-*` rules:
```css
.tenant-counts-open-stale {
    color: #8a5a00;
    font-weight: 600;
}
```

#### 5b — Add helper function in the `@{ }` block

Alongside `FormatCount`, `FormatRatio`, etc.:
```csharp
string FormatOpenCases(
    mmria.common.SharedLibraries.MMRIAServices.Model.TenantDatabaseCountEntryResponse entry)
{
    if (entry.open_case_count_active == null && entry.open_case_count_stale == null)
        return "-";
    int active = entry.open_case_count_active ?? 0;
    int stale = entry.open_case_count_stale ?? 0;
    if (stale == 0) return active.ToString();
    return $"{active}"; // stale rendered separately in the view for amber styling
}
```

(The stale count is rendered separately in the Razor table cell so it can carry the amber CSS class.)

#### 5c — 5th summary tile

Add after the De-ID Mismatch tile (`col-md-3` block), changing the row from four `col-md-3` to five entries. Change each existing tile to `col-md-2` OR keep `col-md-3` in a separate row. The simplest approach: add a second row for the new tile. Place it directly after the existing 4-tile row with its own `<div class="row mb-4">`:

```html
<div class="row mb-2">
    <div class="col-md-3 mb-3">
        <div class="card-container-light col-md-12 tenant-counts-summary-card">
            <div class="header">
                <h2 class="h3 mb-0">Open Cases</h2>
            </div>
            <div class="card-content">
                <span class="metric-value">@Model.TotalOpenCaseCountActive active</span>
                @if (Model.TotalOpenCaseCountStale > 0)
                {
                    <span class="metric-label tenant-counts-open-stale">
                        @Model.TotalOpenCaseCountStale possibly stale
                    </span>
                }
                else if (Model.TotalOpenCaseCountActive == 0)
                {
                    <span class="metric-label">0</span>
                }
            </div>
        </div>
    </div>
</div>
```

> **Layout note:** Alternatively, change all five tiles to `col-md-2` in a single row. Either approach is acceptable — match whatever looks best with the existing card CSS. The `card-container-light` component naturally stretches to fill the column.

#### 5d — Open Cases table column

Add to `<thead>`:
```html
<th scope="col">Open Cases</th>
```

Add to each `<tr>` in `<tbody>` (alongside the other `<td>` cells):
```html
<td>
    @{
        var active = entry.open_case_count_active;
        var stale = entry.open_case_count_stale;
    }
    @if (active == null && stale == null)
    {
        <span>-</span>
    }
    else
    {
        <span>@(active ?? 0)</span>
        @if ((stale ?? 0) > 0)
        {
            <span class="tenant-counts-open-stale"> (@stale)</span>
        }
    }
</td>
```

---

### Notes for implementation

**`Get_Prefix_DB_Url`:** This is the same helper used for "mmrds", "de_id", "report" — e.g. `dbInfo.Get_Prefix_DB_Url("mmrds")` returns the full CouchDB URL for the tenant's MMRDS database. The Mango `_find` endpoint is at `{mmrds_db_url}/_find`.

**`_couchDbHttpClient.ExecuteAsync` with `"POST"`:** Matches existing DAL call patterns. The third argument is the JSON request body string.

**DateTime parsing safety:** `date_last_updated` is stored as ISO 8601 in CouchDB (e.g. `"2026-07-09T09:24:00.000Z"`). If the field is absent or malformed for a document, treat it as stale (count it in the stale bucket). This is already handled by the `else stale++` branch in `TryGetOpenCaseCountsAsync` (when `dateLastUpdated` is null).

**No changes to `status` field logic:** The existing `errorCount` switch (`mmrds_error`, `de_id_error`, `report_error`) is unchanged. `open_case_error` is deliberately excluded from that calculation per AC-4.

**`mmria.common` is consumed by both mmria-server and the standalone `mmria-tenant-database-counts` utility.** The model and manager changes automatically apply to both consumers — no changes needed to `mmria-tenant-database-counts/Program.cs`.

---

## Tasks

- [ ] Add 3 fields to `TenantDatabaseCountEntryResponse` and 2 fields to `TenantDatabaseCountsResponse`
- [ ] Add `GetOpenCaseStubsAsync` to `MMRIAServicesDAL`
- [ ] Add `TryGetOpenCaseCountsAsync` helper and constant to `MMRIAServicesManager`
- [ ] Wire `openCaseTask` into `BuildTenantDatabaseCountEntryAsync`; assign fields; compute response-level totals in `BuildTenantDatabaseCountsResponseAsync`
- [ ] Add `TotalOpenCaseCountActive` / `TotalOpenCaseCountStale` to `TenantDatabaseCountsPageModel`; populate in `Index()`
- [ ] Add amber CSS class, `FormatOpenCases` helper, 5th summary tile, and Open Cases table column to `Index.cshtml`
- [ ] Build `mmria.common` — confirm 0 errors
- [ ] Build `mmria-server` — confirm 0 errors
- [ ] Manual smoke test: load `/tenant-database-counts`; confirm tile and column render; confirm `-` renders when a tenant DB is unreachable
