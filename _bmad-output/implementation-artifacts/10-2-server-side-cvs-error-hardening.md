---
baseline_commit: cb40e16bdf2867eb10a552897456c446bbde041f
---

# Story 10.2 — Server-Side CVS Error Hardening

**Epic:** 10 — CVS PDF Export Tool Reliability
**Story ID:** 10.2
**Status:** done
**Date added:** 2026-07-06

---

## User Story

As a case reviewer generating a CVS PDF,
When the external CVS service fails for any reason,
I want the server to return a structured, descriptive result instead of an unhandled exception,
So that the client can display a meaningful message and react appropriately.

---

## Acceptance Criteria

**AC-1 — Network failure returns structured unavailable result**
Given the external CVS service is unreachable
When `CVSManager.GetDashboardAsync` calls `PostExternalForResponseAsync`
Then an `HttpRequestException` or `TaskCanceledException` is caught
And `CVSFileStatusResult` is returned with `file_status = "unavailable"` and `message = "The CVS service did not respond."`

**AC-2 — HTTP error status codes return structured results**
Given the CVS service returns a non-2xx HTTP response
When the status code is checked against `IsTransientHttpStatus` (408, 429, 500, 502, 503, 504)
Then `file_status = "unavailable"` is returned for transient codes
And `file_status = "error"` is returned for all other non-2xx codes
And `message` includes the HTTP status code: `"The CVS service returned HTTP {code}."`

**AC-3 — Empty response body returns structured unavailable result**
Given the CVS service returns a 2xx response with an empty or whitespace body
When the body is checked with `string.IsNullOrWhiteSpace`
Then `file_status = "unavailable"` and `message = "The CVS service returned an empty response."` are returned

**AC-4 — JSON parse failure is classified by body content**
Given the CVS service returns a body that cannot be parsed as JSON
When `JsonSerializer.Deserialize<ExpandoObject>` throws `JsonException`
Then if the body matches `IsGeneratingResponse`, `file_status = "generating"` and `message = "The CVS service is preparing the PDF."` are returned
And if the body matches `LooksLikeUnavailableResponse`, `file_status = "unavailable"` and `message = "The CVS service is unavailable."` are returned
And otherwise `file_status = "error"` and `message = "The CVS service returned an unexpected response."` are returned

**AC-5 — Base64 decode failure returns structured error**
Given the CVS service returns a response flagged `isBase64Encoded: true` but the body is not valid Base64
When `Convert.FromBase64String` throws `FormatException`
Then `file_status = "error"` and `message = "The CVS service returned an invalid PDF response."` are returned

**AC-6 — `message` field is propagated to the API response**
Given `CVSFileStatusResult` now carries a `message` string property
When `cvsAPIController` maps the `dashboardResult` to `file_status_result`
Then `file_status_result.message = dashboardResult.message` is set
And the `message` field is serialized in the JSON response to the client

**AC-7 — Request duration is logged as structured telemetry**
Given a CVS dashboard request completes (any outcome)
When `cvsAPIController` logs the result
Then a structured log entry is written: `"CVS dashboard request completed. status={Status} duration_ms={DurationMs}"`
And `Status` is the `file_status` string and `DurationMs` is the elapsed milliseconds from a `Stopwatch` started before the manager call

**AC-8 — `CVSExternalResponse` carries status code, body, and success flag**
Given `CVSDAL.PostExternalForResponseAsync` returns
When the HTTP response is mapped
Then a `CVSExternalResponse` record is returned containing `StatusCode` (int), `Body` (string), and `IsSuccess` (bool matching `response.IsSuccessStatusCode`)
And the `HttpResponseMessage` is disposed correctly (`using var response = ...`)

---

## Dev Notes — Root Cause and Fix

### Root Cause

`CVSManager.GetDashboardAsync` called `_dal.PostExternalAsync` (which returned a raw string) and immediately called `JsonSerializer.Deserialize` on it. No exception handling existed for:
- Network failures
- Non-2xx HTTP responses
- Empty response bodies
- JSON parse errors
- Base64 decode errors on the PDF payload

Any failure in the chain would propagate an unhandled exception to the controller.

### New Type: `CVSExternalResponse`

Added to `CVSDAL.cs`:
```csharp
public sealed class CVSExternalResponse
{
    public int StatusCode { get; init; }
    public string Body { get; init; }
    public bool IsSuccess { get; init; }
}
```

`PostExternalForResponseAsync` returns this type. The existing `PostExternalAsync` (returning raw string) is kept for any remaining callers.

### New property on `CVSFileStatusResult`

Added to `CVSModels.cs`:
```csharp
public string message { get; set; }
```

### Helper methods on `CVSManager`

```csharp
private static bool IsTransientHttpStatus(int statusCode) =>
    statusCode == 408 || statusCode == 429 || statusCode == 500 ||
    statusCode == 502 || statusCode == 503 || statusCode == 504;

private static bool IsGeneratingResponse(string body) =>
    body != null && (
        body.Contains("PDF ", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("PDF creation has been initiated", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("retry API call", StringComparison.OrdinalIgnoreCase));

private static bool LooksLikeUnavailableResponse(string body) =>
    !string.IsNullOrWhiteSpace(body) && (
        body.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("service unavailable", StringComparison.OrdinalIgnoreCase));

private static string GetResponseValueAsString(object value) { ... }
```

### `cvsAPIController` changes

- `ILogger<cvsAPIController>` injected and stored as `_logger`
- `Stopwatch dashboardStopwatch` started before `GetDashboardAsync` call
- `file_status_result.message = dashboardResult.message` mapped
- After `break`: `_logger.LogInformation("CVS dashboard request completed. status={Status} duration_ms={DurationMs}", file_status_result.file_status ?? "unknown", dashboardStopwatch.ElapsedMilliseconds)`

### Files Changed

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/DAL/CVSDAL.cs` | Add `CVSExternalResponse` record; add `PostExternalForResponseAsync` method returning it; dispose response with `using` |
| `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/Manager/CVSManager.cs` | Replace `PostExternalAsync` call with `PostExternalForResponseAsync`; add all try/catch layers; add `IsTransientHttpStatus`, `IsGeneratingResponse`, `LooksLikeUnavailableResponse`, `GetResponseValueAsString` helpers |
| `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/Model/CVSModels.cs` | Add `message` property to `CVSFileStatusResult` |
| `source-code/mmria/mmria-server/Controllers/api/cvsAPIController.cs` | Inject `ILogger`; add `Stopwatch`; map `message`; add structured log entry |

### Sequencing

Story 10.2 is independent of 10.3 and 10.4 — server and client changes do not conflict. Can be verified independently by inspecting the API response JSON for `message` and checking server logs.
