## Scan: 2026-07-17 — mmria services @ 45204c84

- **Commit:** `45204c84df00dfa1204f8ca1f06f0c9a43c91eb4` on `development`
- **SSC application version:** 12317
- **Findings JSON source:** GitHub issue CDCgov/nccdphp-drh-mmria#477

### Triage summary

| Category | File:Line | Severity | SSC Issue IDs | Verdict | Evidence |
|---|---|---|---|---|---|
| Mass Assignment: Request Parameters Bound via Input Formatter | `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46` | Medium | 2221777 | **Fixed** | Introduced `SaveSystemOfflineConfigRequest` DTO; `[FromBody]` binding point now uses a type that only exposes client-settable fields. Server-managed fields (`_id`, `_rev`, `data_type`) are excluded from the DTO, so they cannot be supplied by clients. |

---

## Finding 1 — Mass Assignment: Request Parameters Bound via Input Formatter at nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46

**SSC Issue ID:** 2221777

**ruleGuid:** `7AA165DE-F8D9-471F-A1E4-562BB552146D`

**Severity:** Medium

### Verdict

**Fixed**

### Taint path

**Source (line 46 — before fix):**
```csharp
// systemOfflineController.cs:46-48 (original)
[HttpPost]
public async Task<IActionResult> SaveSystemOfflineConfig(
    [FromBody] mmria.common.metadata.SystemOfflineConfig request)
```

Fortify's `[FromBody]` input-formatter rule triggers when a complex domain model is bound directly from the HTTP request body. `SystemOfflineConfig` exposes `_id` (init-only), `_rev` (settable), `data_type` (init-only), and all user-facing fields as settable properties. Even though `_id` and `data_type` carry init-only defaults and the downstream mapping was explicit, Fortify correctly identifies the binding point as a mass-assignment surface because the model type itself accepts `_rev` and all payload fields via deserialization.

**Propagation step:**
```csharp
// systemOfflineController.cs:70 (original) — client _rev leaks into the CouchDB payload
_rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(request?._rev, existing?._rev),
```
`request._rev` was propagated to the helper; `ResolveServerOwnedRevision` prefers `existing._rev` but falls back to the incoming value when no valid server revision is present (e.g. first creation), making a controlled `_rev` injection possible in that edge case.

### Fix applied

**New file:** `nccdphp-drh-mmria-services/mmria.services/Models/SaveSystemOfflineConfigRequest.cs`

A dedicated request DTO was introduced that contains only the nine client-settable fields. Server-managed fields (`_id`, `_rev`, `data_type`) are entirely absent from the DTO — they cannot be deserialized from the request body.

```csharp
// SaveSystemOfflineConfigRequest.cs (new)
public sealed class SaveSystemOfflineConfigRequest
{
    public string warn_date { get; set; }
    public string warn_message { get; set; }
    public string offline_date { get; set; }
    public string offline_modal_message { get; set; }
    public string offline_page_message { get; set; }
    public bool apply_to_all_jurisdictions { get; set; } = true;
    public List<string> selected_jurisdictions { get; set; } = new();
    public int restoration_hours { get; set; } = 2;
    public int auto_logout_minutes { get; set; } = 5;
}
```

**Updated binding point** in `systemOfflineController.cs:46–48`:
```csharp
// After fix
[HttpPost]
public async Task<IActionResult> SaveSystemOfflineConfig(
    [FromBody] SaveSystemOfflineConfigRequest request)
```

**Updated revision assignment** (line 70, after fix) — `_rev` is now always sourced exclusively from the server-side existing document; no client path exists:
```csharp
_rev = existing?._rev,
```

The `CouchDbRevisionHelper` import and call were removed from the controller because `_rev` is no longer derived from any client-supplied value.

### Verdict rationale

The mass-assignment surface is eliminated at the binding point. The DTO type accepted by `[FromBody]` does not contain `_id`, `_rev`, or `data_type`, so the ASP.NET Core input formatter cannot populate those fields regardless of what the client sends. The `_rev` assignment is now unconditionally server-owned, closing the edge-case fallback path identified above.
