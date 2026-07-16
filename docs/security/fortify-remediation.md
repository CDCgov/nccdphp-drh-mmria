## Scan: mmria services @ 558f4a87 — 2026-07-16

- **Commit:** `558f4a87b4caf890a1df7af98b19d943a69b75d1` on `development`
- **SSC application version:** 12317
- **Severity totals:** C:0 H:0 M:1
- **Findings in this block:** 1

---

## Finding 1 — Mass Assignment: Request Parameters Bound via Input Formatter at nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46

**SSC Issue ID:** 2221777

**Verdict:** Fixed

### Taint path

| Step | File:Line | Code |
|------|-----------|------|
| Source | `systemOfflineController.cs:46–48` | `[FromBody] mmria.common.metadata.SystemOfflineConfig request` — ASP.NET input formatter binds all settable properties of the domain model, including server-owned `_id` and `data_type` |
| Sink | `systemOfflineController.cs:68–80` | `new mmria.common.metadata.SystemOfflineConfig { ... }` — payload assembled from the bound request object |

### Fix applied

A dedicated request DTO `mmria.services.Models.SaveSystemOfflineConfigRequest` was introduced in
`nccdphp-drh-mmria-services/mmria.services/Models/SaveSystemOfflineConfigRequest.cs`. This DTO
exposes only the nine client-settable fields (`warn_date`, `warn_message`, `offline_date`,
`offline_modal_message`, `offline_page_message`, `apply_to_all_jurisdictions`,
`selected_jurisdictions`, `restoration_hours`, `auto_logout_minutes`). Server-owned fields
(`_id`, `_rev`, `data_type`) are absent from the DTO and therefore cannot be bound by the input
formatter, eliminating the mass assignment vector at the source.

The `[FromBody]` parameter type was changed from `mmria.common.metadata.SystemOfflineConfig` to
`mmria.services.Models.SaveSystemOfflineConfigRequest`. Because `_rev` is no longer in the DTO,
the `CouchDbRevisionHelper.ResolveServerOwnedRevision` call was updated to pass `null` as the
incoming revision, ensuring the server-owned revision is always used.

### Evidence

```csharp
// BEFORE (systemOfflineController.cs:46-48)
[HttpPost]
public async Task<IActionResult> SaveSystemOfflineConfig(
    [FromBody] mmria.common.metadata.SystemOfflineConfig request)

// AFTER (systemOfflineController.cs:46-48)
[HttpPost]
public async Task<IActionResult> SaveSystemOfflineConfig(
    [FromBody] SaveSystemOfflineConfigRequest request)
```

New file `Models/SaveSystemOfflineConfigRequest.cs` (excerpt):
```csharp
/// <summary>
/// Request DTO for the SaveSystemOfflineConfig endpoint.
/// Only contains fields that a client is permitted to supply; server-owned
/// fields (_id, _rev, data_type) are intentionally excluded to prevent
/// mass assignment (CWE-915).
/// </summary>
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
