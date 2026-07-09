# Story 12.3 — Case Rev Endpoint

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.3
**Status:** not-started
**Date added:** 2026-07-08
**Depends on:** Story 8.1 (system-offline-config and `/api/system-offline/status` — needed for `X-Offline-Date` header)
**Source requirements:** FR-18.1–FR-18.3

---

## User Story

As the client-side case polling module,
I want a lightweight endpoint that returns only the current `_rev` of a case document,
So that I can detect whether the open case has been modified without fetching the full document on every poll cycle.

---

## Acceptance Criteria

**AC-1 — Endpoint returns `_rev` for an existing document**
Given an authenticated request: `GET /api/case/{id}/rev`
When the document exists in CouchDB
Then the response status is `200`
And the body is `{ "_id": "<id>", "_rev": "<current_rev>" }`
And only these two fields are returned (not the full case document)

**AC-2 — Endpoint returns 404 for a missing document**
Given an authenticated request: `GET /api/case/{id}/rev`
When the document does not exist in CouchDB (CouchDB returns 404)
Then the mmria-server response status is `404`

**AC-3 — Authentication required**
Given an unauthenticated request: `GET /api/case/{id}/rev`
When the request is received
Then the response status is `401`
And no CouchDB call is made

**AC-4 — `X-Offline-Date` header included when offline_date is configured**
Given the system offline config has a non-empty `offline_date`
When `GET /api/case/{id}/rev` returns 200
Then the response includes header `X-Offline-Date: <offline_date value as-stored (ISO 8601)>`

**AC-5 — `X-Offline-Date` header is absent when offline_date is not configured**
Given the system offline config has a null or empty `offline_date`
When `GET /api/case/{id}/rev` returns 200
Then the response does NOT include the `X-Offline-Date` header

**AC-6 — Response latency**
Given a request to `GET /api/case/{id}/rev` on a local network
When the endpoint proxies to CouchDB and returns
Then the round-trip latency is under 200 ms

---

## Dev Notes — Implementation

### File to modify: `source-code/mmria/mmria-server/Controllers/api/caseController.cs`

This is the existing `api/caseController.cs` at route `[Route("api/[controller]")]`. It already has:
- Constructor injecting `tenantRuntime`, `actorSystem`, `authorizationService`, `couchDbHttpClient`, `caseManager`
- `[Authorize(Roles = "abstractor, data_analyst")]` on the GET action
- `db_config.url` as the CouchDB base URL, `db_config.user_name`/`db_config.user_value` as credentials

**Add a new action `GetRev`:**

```csharp
[Authorize(Roles = "abstractor, data_analyst")]
[HttpGet("{case_id}/rev")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public async Task<IActionResult> GetRev(string case_id)
{
    try
    {
        // Fetch only enough to get _id and _rev — use a fields-limited GET or HEAD+GET
        // CouchDB does not support HEAD with _rev in the body, so do a GET
        // but only return _id and _rev to the client
        string url = $"{db_config.url}/{Uri.EscapeDataString(case_id)}";
        string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
            "GET", url, null, db_config.user_name, db_config.user_value);

        // Check for CouchDB 404 ({"error":"not_found",...})
        if (responseFromServer.Contains("\"not_found\""))
            return NotFound();

        // Deserialize just enough to extract _id and _rev
        var doc = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer)
            as IDictionary<string, object>;

        if (doc == null)
            return NotFound();

        var result = new { _id = doc["_id"]?.ToString(), _rev = doc["_rev"]?.ToString() };

        // Attach X-Offline-Date header if offline_date is configured
        var offlineDate = await GetOfflineDateAsync();
        if (!string.IsNullOrWhiteSpace(offlineDate))
            Response.Headers["X-Offline-Date"] = offlineDate;

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";

        return EscapedJsonResultFactory.Create(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return StatusCode(500);
    }
}
```

**Add `GetOfflineDateAsync()` private helper:**

```csharp
private async Task<string> GetOfflineDateAsync()
{
    try
    {
        // Read offline_date from mmria-services — same pattern used by system_offlineController.LoadConfigFromServicesAsync()
        // Check system_offlineController.cs for the vitals_url pattern:
        // string vitals_url = configuration.GetString("vitals_url", host_prefix);
        // string vital_service_key = configuration.GetString("vital_service_key", host_prefix);
        // GET {vitals_url}/api/systemOffline/GetSystemOfflineConfig
        var vitals_url = configuration.GetString("vitals_url", host_prefix);
        var vital_service_key = configuration.GetString("vital_service_key", host_prefix);
        if (string.IsNullOrWhiteSpace(vitals_url)) return null;

        string url = $"{vitals_url}/api/systemOffline/GetSystemOfflineConfig";
        string response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, "services", vital_service_key);
        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.SystemOfflineConfig>(response);
        return config?.offline_date;
    }
    catch
    {
        return null; // header is optional — never fail the main request for it
    }
}
```

**Study `system_offlineController.cs` before implementing** — specifically `LoadConfigFromServicesAsync()` (around line 148) to copy the exact vitals_url / vital_service_key access pattern. The `configuration.GetString()` calls and credential format must match.

**Alternative for `X-Offline-Date`:** If the system offline config is already cached in-memory on the server (check if there is a singleton or IMemoryCache storing it), read from cache instead of making a services call on every rev request. The performance target is <200 ms so a services round-trip on every 45s poll is acceptable, but cache is preferable.

### Route collision check

The existing `[HttpGet]` in `caseController.cs` handles `GET api/case/{case_id}` with parameter `string case_id`. The new route `[HttpGet("{case_id}/rev")]` adds a path segment, so there is no collision. Verify by running the build after adding the action.

### `EscapedJsonResultFactory`

This factory is already used throughout the controller (`source-code/mmria/mmria-server/`). It handles JSON escaping and content-type headers consistently. Use it for the response — do not use `Json()` or `Ok()` directly.

### Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Controllers/api/caseController.cs` | Add `GetRev(string case_id)` action with `[HttpGet("{case_id}/rev")]`; add `GetOfflineDateAsync()` private helper |

### Testing

**Integration test approach:** The project uses test files under `nccdphp-drh-mmria-utilities/mmria-server.tests/`. Look for existing tests that call API endpoints with an HTTP client. If the pattern uses `WebApplicationFactory<Program>` or similar, add:
- `GET /api/case/{known_id}/rev` returns 200 with `_id` and `_rev` matching the document
- `GET /api/case/nonexistent-id/rev` returns 404
- `GET /api/case/{id}/rev` with `X-Offline-Date` present when offline config has a date

If integration tests are not practical, document the manual verification steps in the completion notes.

### Important Notes

- The CouchDB GET on a non-existent document returns `{"error":"not_found","reason":"missing"}` — do not assume a specific HTTP status code from `CouchDbHttpClient.ExecuteAsync`; check the response body.
- `Uri.EscapeDataString(case_id)` is used defensively — case IDs in MMRIA should be UUID-format and safe, but escaping prevents path injection.
- The `X-Offline-Date` header is a hint to the client, not enforced by this endpoint. If the services call fails, the header is omitted and the main response proceeds normally.

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

### Change Log

| File | Change |
|------|--------|
| | |
