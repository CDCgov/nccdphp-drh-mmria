## Scan: 2026-07-15 — Fortify mmria s2i @ 2a26dc0d (SSC 10291)

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148
**SSC Issue ID:** 2223458
**Severity:** High
**Verdict:** Already remediated

### Evidence
- SSC Issue ID 2223458 appeared in the prior scan (Scan: 2026-07-15 — mmria s2i @ 5c69b518) with verdict **Fixed**; the same fix is still present in the current codebase at the same file and line.
- `EnsureContainedDirectoryExists(...)` calls `ResolveContainedDirectoryPath(...)`, which in turn calls `ValidateContainedName(...)` before combining paths (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:144-150`). The sink `Directory.CreateDirectory(safePath)` at line 148 only receives a path that has passed allow-list validation.
- `ValidateContainedName(...)` rejects any character outside letters, digits, dash, underscore, or dot; rejects `.` and `..`; rejects rooted paths and separator characters; and rejects reserved Windows device names (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:208-252`).
- `EnsureContainedPath(...)` verifies the resolved combined path still starts with the normalized base directory after `Path.GetFullPath(...)` resolution, preventing any traversal that might survive character filtering (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:254-260`).
- Carried from prior scan — evidence unchanged; code fix verified in-place at current HEAD.

### SWA Summary
Contained directory creation at `Directory.CreateDirectory(safePath)` only receives paths that have passed allow-list validation in `ValidateContainedName(...)` and a post-resolution boundary check in `EnsureContainedPath(...)`. No traversal characters or separator sequences can reach the sink. The fix introduced in scan 5c69b518 remains in the current codebase.

### Verdict rationale
The taint path from caller-controlled input to `Directory.CreateDirectory(...)` is fully constrained by allow-list validation and a post-resolution containment check already present in the codebase. Fortify reported the same SSC Issue ID (2223458) in the previous scan where the fix was verified; this is a stale Fortify result against code that already contains the remediation.

## Finding 2 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:25
**SSC Issue ID:** 2235651
**SSC Issue ID:** 2235652
**Severity:** Critical
**Verdict:** Already remediated

### Evidence
- `EscapedJsonResultFactory.Serialize(...)` serializes the response object using Newtonsoft.Json with `StringEscapeHandling.EscapeHtml`, which encodes `<`, `>`, `&`, `'`, and `"` as Unicode escape sequences inside every JSON string value before the content reaches the HTTP response (`source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:15-43`).
- `EscapedJsonResultFactory.Create(...)` wraps the serialized payload in a `SecureEscapedJsonResult` whose `ExecuteResultAsync(...)` injects `X-Content-Type-Options: nosniff` before the response is written, preventing browsers from content-sniffing the escaped JSON as HTML (`source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22-53`).
- Line 25 (`Content = Serialize(value)`) is the assignment inside `Create(...)` that stores the HTML-escaped JSON string; the tainted value has already been encoded by the time it is set as the `Content` property and before `ExecuteResultAsync(...)` writes it to the wire.
- A prior XSS finding at this factory (SSC Issue ID 2225918, line 22) was verdicted **Fixed** in scan 5c69b518 using the same sanitizer pair; the two new data-flow traces (IDs 2235651, 2235652) reach the same sink with the same encoding in place.
- Carried from prior scan — evidence unchanged; code fix verified in-place at current HEAD.

### SWA Summary
`EscapedJsonResultFactory` HTML-escapes all JSON string content via `StringEscapeHandling.EscapeHtml` and marks every response `X-Content-Type-Options: nosniff`. Both the payload-encoding and content-sniffing preconditions for reflected XSS at this sink are eliminated by controls already present in the current codebase. These new data-flow traces (SSC IDs 2235651, 2235652) are stale results that reach the same already-protected sink as the previously-fixed finding (SSC 2225918).

### Verdict rationale
The two new data-flow traces follow different entry-point paths to the same `Content = Serialize(value)` sink at line 25. `Serialize(...)` unconditionally applies `StringEscapeHandling.EscapeHtml`, making any HTML or script payload inserted into the JSON string non-executable in a browser context. The `X-Content-Type-Options: nosniff` header additionally prevents content-type coercion. The encoding and header controls verified in the previous scan are still present at HEAD; these findings represent stale Fortify detections of an already-remediated code path.

---

## Scan: 2026-07-15 — Fortify mmria s2i @ 5c69b518 (SSC 10291)

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148
**SSC Issue ID:** 2223458
**Severity:** High
**Verdict:** Fixed

### Evidence
- `ContainedPathHelper.EnsureContainedDirectoryExists(...)` creates directories only after `ResolveContainedDirectoryPath(...)` normalizes the trusted root, validates the child name, resolves the combined path, and verifies the resolved path still starts with the trusted base directory (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:126-149`).
- `ContainedPathHelper.ValidateContainedName(...)` now rejects any character outside letters, digits, dash, underscore, or dot before the path is combined or created, which constrains caller-controlled input to a single safe path segment (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:208-248`).
- `backup_managerController.GetSubFolderFile(...)` passes route input through `ValidateContainedName(...)` before calling `EnsureContainedDirectoryExists(...)`, so Fortify's source path is now bounded both at the controller and inside the helper (`source-code/mmria/mmria-server/Controllers/backup_managerController.cs:266-295`).

### Changes made
- Added an explicit positive allow-list in `ValidateContainedName(...)` so contained path segments cannot contain traversal syntax, alternate path delimiters, or shell metacharacters even on platforms where `Path.GetInvalidFileNameChars()` is permissive.

### SWA Summary
Contained directory creation now rejects any requested file or folder name containing characters outside letters, digits, dash, underscore, or dot before combining it with the trusted base directory. The helper still verifies the resolved full path remains inside the configured root and still blocks reparse points before `Directory.CreateDirectory(...)`.

### Verdict rationale
The finding is fixed in code because the sink at `Directory.CreateDirectory(...)` no longer accepts arbitrary caller-controlled path text. Inputs are reduced to a single allow-listed path segment, resolved beneath a trusted fully-qualified root, and rejected if the final path escapes that root or traverses a reparse point.

## Finding 2 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45
**SSC Issue ID:** 2221778
**Severity:** High
**Verdict:** Fixed

### Evidence
- `CreateBearerAuthenticationHeaderValue(...)` trims and validates the bearer token with `ValidateHeaderValue(...)`, then enforces the bearer-token character allow-list before building the outbound header (`source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:32-45`).
- `ValidateHeaderValue(...)` rejects empty values, overlong values, non-visible ASCII, and any value changed by `CouchDbHttpClient.SanitizeHeader(...)`, which strips control characters such as CR/LF used for header injection (`source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:48-73`, `nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs:674-691`).
- `AccountController.OIDC.cs` and `SteveAPI_Instance.cs` set the `Authorization` header only through `CreateBearerAuthenticationHeaderValue(...)`, so tainted access tokens now flow through the helper's parser-backed validation before they reach `HttpRequestHeaders.Authorization` (`source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs:229`, `source-code/mmria/mmria-server/model/actor/SteveAPI_Instance.cs:330`).

### Changes made
- Replaced direct construction of `AuthenticationHeaderValue` with `AuthenticationHeaderValue.TryParse(...)` and exact scheme/parameter checks so the final `Authorization` header must round-trip through the framework parser after sanitization.

### SWA Summary
Authorization header values are now sanitized, regex-validated, and then round-tripped through `AuthenticationHeaderValue.TryParse(...)` before the helper returns a `Bearer` header. Tokens containing control characters, unsupported punctuation, or parser-visible ambiguities are rejected before any outbound request is sent.

### Verdict rationale
The finding is fixed in code because user-controlled token data can no longer be injected directly into the header sink. The helper now requires the final serialized header value to parse as a valid `Bearer` authorization header whose parameter exactly matches the sanitized token.

## Finding 3 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22
**SSC Issue ID:** 2225918
**Severity:** Critical
**Verdict:** Fixed

### Evidence
- `EscapedJsonResultFactory.Serialize(...)` uses Newtonsoft.Json with `StringEscapeHandling.EscapeHtml`, which escapes HTML-significant characters inside JSON string values before they are returned to the client (`source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:12-40`).
- `EscapedJsonResultFactory.Create(...)` now returns a custom `ContentResult` that always emits `application/json; charset=utf-8` and adds `X-Content-Type-Options: nosniff`, preventing browsers from content-sniffing the escaped JSON as HTML (`source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:8-54`).
- Controllers such as `system_offlineController`, `vitalsController`, and `caseController` use this factory for JSON responses, so the fix applies uniformly to the reflected-response sinks Fortify traced (`source-code/mmria/mmria-server/Controllers/system_offlineController.cs:42-133`, `source-code/mmria/mmria-server/Controllers/vitalsController.cs:111-133`, `source-code/mmria/mmria-server/Controllers/api/caseController.cs:111-132`).

### Changes made
- Added a secure `ContentResult` subclass that sets `X-Content-Type-Options: nosniff` before writing the already HTML-escaped JSON payload returned by the factory.

### SWA Summary
JSON responses produced by `EscapedJsonResultFactory` continue to HTML-escape all string content and now also send `X-Content-Type-Options: nosniff`. This prevents reflected JSON data from being interpreted as executable HTML while preserving the existing JSON response contract used by the legacy controllers.

### Verdict rationale
The finding is fixed in code because untrusted values are HTML-escaped during JSON serialization and the response is explicitly forced to stay JSON at the browser boundary. The response helper therefore closes both the payload-encoding and content-sniffing avenues required to exploit reflected XSS at this sink.
