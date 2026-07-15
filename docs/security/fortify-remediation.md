## Scan: 2026-07-15 — Fortify mmria s2i @ c8fab9fa (SSC 10291)

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148
**SSC Issue ID:** 2223458
**Severity:** High
**Verdict:** Already remediated

### Evidence
- Taint source: the `childDirectoryName` parameter of `EnsureContainedDirectoryExists(...)` is caller-controlled (e.g. from a route value in `backup_managerController`).
- Propagation: `EnsureContainedDirectoryExists(...)` calls `ResolveContainedDirectoryPath(...)` which calls `ValidateContainedName(childDirectoryName, ...)` at `source-code/mmria/mmria-server/util/ContainedPathHelper.cs:129`. `ValidateContainedName(...)` rejects any character not in `[A-Za-z0-9\-_.]`, rejects `.`/`..`, rejects rooted paths, and rejects Windows reserved device names (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:208-251`).
- Sink: `Directory.CreateDirectory(safePath)` at line 148 only executes after `ResolveContainedDirectoryPath(...)` returns a validated, fully-qualified path that `EnsureContainedPath(...)` has confirmed still starts with the trusted base directory (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:130-131`).
- Guard at sink: `ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, ...)` at line 147 additionally blocks symlink/junction escapes before `Directory.CreateDirectory` is called (`source-code/mmria/mmria-server/util/ContainedPathHelper.cs:147`).
- The allow-list character filter in `ValidateContainedName(...)` is the specific guard that satisfies CWE-22 (Improper Limitation of a Pathname) — the tainted value is reduced to a single safe path segment before it is combined with any file-system path. Carried from prior scan (5c69b518) — code fix unchanged.

### SWA Summary
`Directory.CreateDirectory(...)` at the tainted sink is guarded by an allow-list character filter (`ValidateContainedName`), a path-escape check (`EnsureContainedPath`), and a reparse-point check. The caller-controlled directory name can no longer contain traversal syntax, alternate separators, or shell metacharacters. Fortify detected a stale result from before these controls were introduced.

### Verdict rationale
The fix is demonstrably present in the current codebase. `ValidateContainedName(...)` enforces a positive allow-list before the tainted value reaches the `Directory.CreateDirectory(...)` sink; the path is further verified to remain inside the trusted root. The taint path identified by Fortify is therefore fully neutralized by in-repo guards that were committed prior to this scan.

## Finding 2 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:25
**SSC Issue ID:** 2235651
**SSC Issue ID:** 2235652
**Severity:** Critical
**Verdict:** Already remediated

### Evidence
- Taint source: the `value` parameter of `EscapedJsonResultFactory.Create(object value)` at line 22 is caller-controlled (controllers pass arbitrary model objects including user-supplied data).
- Propagation: `Create(value)` calls `Serialize(value)` and assigns the result to the `Content` property of `SecureEscapedJsonResult` at line 25 (`source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22-28`).
- Sink: `Content` is returned to the HTTP response through `ContentResult.ExecuteResultAsync(...)`, which writes the content string to the response body.
- Sanitizer at source (serialization): `Serialize(value)` uses `JsonTextWriter` with `StringEscapeHandling = StringEscapeHandling.EscapeHtml` (line 37) and `JsonSerializer.Create(HtmlEscapingSerializerSettings)` whose settings also set `StringEscapeHandling.EscapeHtml` (line 18). This encodes `<`, `>`, `&`, `'`, and `"` inside every JSON string value before the content reaches the response (`source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:15-43`).
- Sanitizer at response: `SecureEscapedJsonResult.ExecuteResultAsync(...)` at line 47-51 sets `X-Content-Type-Options: nosniff` on every response, preventing browser content-sniffing from reinterpreting the JSON payload as HTML.
- These two controls together satisfy the CWE-79 (XSS: Reflected) remediation requirements: HTML-significant characters are encoded at the serialization step so no reflected value can inject executable HTML, and the `nosniff` header forecloses the content-sniffing vector. Carried from prior scan (5c69b518, Finding 3 at line 22) — SSC IDs differ (new data-flow traces) but the code fix is unchanged.

### SWA Summary
`EscapedJsonResultFactory.Create(...)` HTML-escapes all string content through Newtonsoft.Json's `StringEscapeHandling.EscapeHtml` during serialization and forces `X-Content-Type-Options: nosniff` on every response via `SecureEscapedJsonResult`. Reflected user-controlled values cannot inject executable HTML, and the browser cannot content-sniff the JSON response as HTML. Fortify detected new data-flow traces for the same already-fixed sink.

### Verdict rationale
The fix is demonstrably present in the current codebase. HTML encoding at serialization (line 37) and the `nosniff` header (line 49) were both in place before this scan. The two new SSC issue IDs represent additional data-flow traces Fortify enumerated for the same `(ruleGuid, path, line)` sink, not new vulnerabilities. The taint path is fully neutralized by the in-repo controls.

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
