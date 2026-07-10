# Story 12.3 — Case Rev Endpoint

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.3
**Status:** done
**Date added:** 2026-07-08
**Depends on:** None (consumed by Story 12.4 stale-tab UX)
**Source requirements:** FR-18.1–FR-18.2

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

**AC-4 — Response latency**
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
        string url = $"{db_config.url}/{Uri.EscapeDataString(case_id)}";
        var headResponse = await _couchDbHttpClient.ExecuteForResponseAsync(
            "HEAD",
            url,
            null,
            "application/json",
            new mmria.common.getset.CouchDbRequestOptions
            {
                UserName = db_config.user_name,
                Password = db_config.user_value,
                SuppressErrorLogging = true
            });

        if (headResponse.StatusCode == 404)
            return NotFound();

        var headRev = NormalizeCouchDbRevisionHeader(headResponse.GetFirstHeaderValue("ETag"));
        if (headResponse.StatusCode >= 200 && headResponse.StatusCode < 300 && !string.IsNullOrWhiteSpace(headRev))
        {
            var headResult = new { _id = case_id, _rev = headRev };

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";

            return EscapedJsonResultFactory.Create(headResult);
        }

        // Fallback to GET only when CouchDB does not provide an ETag revision.
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

`NormalizeCouchDbRevisionHeader()` trims the surrounding quotes from CouchDB's `ETag` value.

Do not attach offline-status headers or call mmria-services from this endpoint. Offline timing remains owned by `/api/system-offline/status` so the case rev check stays lightweight.

### Route collision check

The existing `[HttpGet]` in `caseController.cs` handles `GET api/case/{case_id}` with parameter `string case_id`. The new route `[HttpGet("{case_id}/rev")]` adds a path segment, so there is no collision. Verify by running the build after adding the action.

### `EscapedJsonResultFactory`

This factory is already used throughout the controller (`source-code/mmria/mmria-server/`). It handles JSON escaping and content-type headers consistently. Use it for the response — do not use `Json()` or `Ok()` directly.

### Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Controllers/api/caseController.cs` | Add `GetRev(string case_id)` action with `[HttpGet("{case_id}/rev")]` returning only `{ "_id": "...", "_rev": "..." }` |

### Testing

**Integration test approach:** The project uses test files under `nccdphp-drh-mmria-utilities/mmria-server.tests/`. Look for existing tests that call API endpoints with an HTTP client. If the pattern uses `WebApplicationFactory<Program>` or similar, add:
- `GET /api/case/{known_id}/rev` returns 200 with `_id` and `_rev` matching the document
- `GET /api/case/nonexistent-id/rev` returns 404
- `GET /api/case/{id}/rev` response contains only `_id` and `_rev`, with no offline-status header dependency

If integration tests are not practical, document the manual verification steps in the completion notes.

### Important Notes

- The CouchDB GET on a non-existent document returns `{"error":"not_found","reason":"missing"}` — do not assume a specific HTTP status code from `CouchDbHttpClient.ExecuteAsync`; check the response body.
- `Uri.EscapeDataString(case_id)` is used defensively — case IDs in MMRIA should be UUID-format and safe, but escaping prevents path injection.
- `/api/system-offline/status` owns offline-date polling. Do not add a services call to `/api/case/{id}/rev`.

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

- Added `GetRev(string case_id)` action at `[HttpGet("{case_id}/rev")]` in `caseController.cs`. No route collision with the existing parameterless `[HttpGet]`.
- Uses CouchDB `HEAD` and the `ETag` revision for the normal lightweight path, with a GET/JObject fallback only if the revision header is unavailable. The response remains `{ "_id": "...", "_rev": "..." }` via `mmria.server.util.EscapedJsonResultFactory.Create`.
- Follow-up 2026-07-10: removed the `X-Offline-Date` services call from the rev endpoint so it returns only `{ "_id": "...", "_rev": "..." }` and meets the lightweight latency target.
- `EscapedJsonResultFactory` is referenced with its full namespace (`mmria.server.util.EscapedJsonResultFactory`) since `caseController.cs` does not have a `using mmria.server.util;` directive.
- Build verified: zero C# compile errors. Current build passes with pre-existing warnings unrelated to the rev endpoint change.

### Manual Verification Steps

1. Start the server and authenticate as an abstractor or data_analyst.
2. `GET /api/case/{known-case-id}/rev` → expect `200` with `{ "_id": "...", "_rev": "..." }` only.
3. `GET /api/case/nonexistent-id/rev` → expect `404`.
4. Unauthenticated `GET /api/case/{id}/rev` → expect `401`.
5. Verify the response does not include `X-Offline-Date`; offline status is provided by `/api/system-offline/status`.

### Change Log

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Controllers/api/caseController.cs` | Added `GetRev` action (`[HttpGet("{case_id}/rev")]`) returning only `_id` and `_rev`; removed the offline-date helper from this endpoint |
