## Scan: ccc7fe50 — 2026-07-10

**Commit:** ccc7fe50950e432ff6a195acfb5425ec2c4961ae  
**Branch:** development  
**SSC application version:** 10291  
**Scan date:** 2026-07-10  
**Findings:** C:1 H:2 M:0 (3 unique, 3 in SSC)

### Triage summary

| Category | File:Line | Severity | SSC Issue IDs | Verdict | Evidence |
|---|---|---|---|---|---|
| Path Manipulation | source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148 | High | 2223458 | Not applicable / false positive | `ValidateContainedName` allowlist + `EnsureContainedPath` containment assertion + `ThrowIfExistingPathOrAncestorIsReparsePoint` reparse-point guard fully neutralize any user-supplied path segment before `Directory.CreateDirectory` is called |
| Header Manipulation | source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45 | High | 2221778 | Not applicable / false positive | Strict allowlist regex `^[A-Za-z0-9._~+/=-]{1,4096}$` validated after visible-ASCII check and `SanitizeHeader` call; CRLF injection categorically impossible with this character set |
| Cross-Site Scripting: Reflected | source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22 | Critical | 2225918 | Not applicable / false positive | `StringEscapeHandling.EscapeHtml` applied at both `JsonSerializerSettings` and `JsonTextWriter` levels; all HTML-significant characters (`<`, `>`, `&`, `'`, `"`) are Unicode-escaped before content is written to the HTTP response with `Content-Type: application/json` |

---

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**Severity:** High  
**SSC Issue ID:** 2223458  
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99

**Verdict:** Not applicable / false positive

### Evidence

**Taint source:** Caller-supplied `childDirectoryName` parameter propagated to `EnsureContainedDirectoryExists` (e.g., `cvsAPIController.cs:77`, `backup_managerController.cs:291`).

**Taint sink:** `Directory.CreateDirectory(safePath)` at `ContainedPathHelper.cs:148`.

**Taint path with guards:**

1. **Source:** `childDirectoryName` (any string) enters `EnsureContainedDirectoryExists` (line 144).

2. **Guard 1 — `ValidateContainedName` (lines 208–244):** Applied inside `ResolveContainedDirectoryPath` at line 129. This method throws `ArgumentException` for:
   - Null / empty / whitespace input
   - Relative operators `"."` or `".."`
   - Path-rooted strings
   - Strings containing `Path.DirectorySeparatorChar` or `Path.AltDirectorySeparatorChar`
   - Any character in `Path.GetInvalidFileNameChars()`
   - Windows reserved device names (`CON`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`, etc.)
   - Trailing dot or space on Windows
   
   Only a single, clean filename-safe segment passes.

3. **Guard 2 — `EnsureContainedPath` (lines 246–252):** After `Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName))` at line 130, this asserts `resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase)`. Any path that somehow escaped the root despite the above guards is rejected here.

4. **Guard 3 — `ThrowIfExistingPathOrAncestorIsReparsePoint` (lines 254–260):** Called at line 147 before `Directory.CreateDirectory`. Walks every existing ancestor segment and throws if any component has `FileAttributes.ReparsePoint` set, preventing symlink/junction traversal after a safe path is constructed.

5. **Sink:** `Directory.CreateDirectory(safePath)` at line 148 receives a fully validated, base-contained, non-reparse-point path.

**Specific precondition from Fortify Taxonomy / CWE-22 (Path Traversal):** Path traversal requires that user input containing `../`, absolute roots, or symlinks can reach the filesystem operation without being rejected. All three are categorically blocked by the guards above:
- `../` → blocked by `ValidateContainedName` (contains `DirectorySeparatorChar` and `..` checks)
- Absolute root → blocked by `ValidateContainedName` (`Path.IsPathRooted`) and `EnsureContainedPath`
- Symlinks → blocked by `ThrowIfExistingPathOrAncestorIsReparsePoint`

No exploitable path traversal vector exists at this sink.

### SWA Summary

Path manipulation finding at `ContainedPathHelper.cs:148` is a false positive. The `Directory.CreateDirectory` call operates on a path produced by `ResolveContainedDirectoryPath`, which enforces three independent guards: (1) `ValidateContainedName` allowlists only single path segments with no separators or traversal operators; (2) `EnsureContainedPath` asserts the resolved path is still inside the configured base directory after `Path.GetFullPath`; (3) `ThrowIfExistingPathOrAncestorIsReparsePoint` prevents symlink-based traversal. No user-controlled string can reach `Directory.CreateDirectory` without passing all three guards.

---

## Finding 2 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**Severity:** High  
**SSC Issue ID:** 2221778  
**Rule GUID:** 0858F67A-D592-4E5F-8C3A-514CB484E1CB

**Verdict:** Not applicable / false positive

### Evidence

**Taint source:** `access_token` from an OIDC authentication response passed to `CreateBearerAuthenticationHeaderValue(access_token, ...)` at `AccountController.OIDC.cs:226`. Also `bearerToken` at `SteveAPI_Instance.cs:330`.

**Taint sink:** `new AuthenticationHeaderValue("Bearer", sanitizedToken)` at `OutboundRequestSecurityHelper.cs:45`.

**Taint path with guards:**

1. **Source:** `bearerToken` (OIDC `access_token`) enters `CreateBearerAuthenticationHeaderValue` (line 32).

2. **Guard 1 — null/empty check (line 34):** `string.IsNullOrWhiteSpace(bearerToken)` throws if absent.

3. **Guard 2 — `ValidateHeaderValue` (lines 48–73):** Called at line 39.
   - Trims the value.
   - Enforces a maximum length of 4096 characters.
   - Checks `IsVisibleAsciiHeaderCharacter` for every character (line 61): character must be in range `[0x20, 0x7E]`. This categorically rejects CR (`\r`, 0x0D) and LF (`\n`, 0x0A), the two characters required for HTTP header injection.
   - Calls `CouchDbHttpClient.SanitizeHeader` (`CouchDbHttpClient.cs:674`) which retains only `ch == 9 || (ch >= 32 && ch != 127)`, and verifies the sanitized result is identical to the trimmed input (line 67). Any CR/LF would cause `SanitizeHeader` to drop them, making the sanitized value differ from the trimmed input and throwing.

4. **Guard 3 — `BearerTokenPattern` allowlist (line 40):** `^[A-Za-z0-9._~+/=-]{1,4096}$` is a strict allowlist. Any character not in this set (including CR, LF, colon, space, semicolon) causes the match to fail and throws `ArgumentException`.

**Specific precondition from Fortify Taxonomy / CWE-113 (HTTP Response Splitting):** Header injection requires CRLF (`\r\n`) or `\n` in user-supplied data reaching an HTTP header setter. The visible-ASCII check at guard 2 and the allowlist regex at guard 3 each independently and categorically block CR and LF. There is no exploitable path to header injection at this sink.

### SWA Summary

Header manipulation finding at `OutboundRequestSecurityHelper.cs:45` is a false positive. Before `AuthenticationHeaderValue("Bearer", sanitizedToken)` is constructed, the bearer token is validated through three independent guards: (1) a visible-ASCII character check (`[0x20, 0x7E]`) that categorically rejects CR and LF; (2) `CouchDbHttpClient.SanitizeHeader` with an equality check confirming no characters were removed; (3) a strict allowlist regex `^[A-Za-z0-9._~+/=-]{1,4096}$` that permits only alphanumeric characters and the safe symbols `._~+/=-`. CR/LF injection is categorically impossible.

---

## Finding 3 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22

**Severity:** Critical  
**SSC Issue ID:** 2225918  
**Rule GUID:** E89E1BB0-0F7E-4A15-A1F2-DF5F5F851E6C

**Verdict:** Not applicable / false positive

### Evidence

**Taint source:** User-influenced data retrieved from CouchDB (e.g., `result` from `vitalsController.cs:111,133`, deserialized from server response).

**Taint sink:** `Content = Serialize(value)` at `EscapedJsonResultFactory.cs:22`, setting the HTTP response body.

**Taint path with guards:**

1. **Source:** `value` (object graph that may contain user-controlled string fields) enters `EscapedJsonResultFactory.Create(value)` (line 19) and then `Serialize(value)` (line 22).

2. **Guard 1 — `HtmlEscapingSerializerSettings` (lines 12–17):** `JsonSerializerSettings` with `StringEscapeHandling = StringEscapeHandling.EscapeHtml` set at the class level. This causes Json.NET to Unicode-escape the HTML-significant characters `<`, `>`, `&`, `'`, and `"` as `\u003C`/`\u003E`/`\u0026`/`\u0027`/`\u0022` respectively.

3. **Guard 2 — `JsonTextWriter.StringEscapeHandling` (line 33):** Set to `StringEscapeHandling.EscapeHtml` a second time at the writer level, ensuring the escaping is applied even if the writer is reused in a different context. Both layers must independently escape the output.

4. **Guard 3 — Content-Type header (line 23):** `ContentType = "application/json; charset=utf-8"` instructs browsers to parse the response as JSON, not HTML. Combined with the Unicode-escaped characters, no modern browser will interpret the response as HTML.

**Specific precondition from Fortify Taxonomy / CWE-79 (XSS: Reflected):** Reflected XSS requires that user-controlled data is rendered as HTML in the browser. Two independent controls prevent this:
- `StringEscapeHandling.EscapeHtml` Unicode-escapes every HTML special character, so even if a browser incorrectly interprets the body as HTML, all markup is neutralized.
- `Content-Type: application/json` prevents browsers from rendering the body as HTML under same-origin security policies.

The class name `EscapedJsonResultFactory` reflects its purpose: producing HTML-safe JSON responses. No exploitable XSS vector exists at this sink.

### SWA Summary

Cross-site scripting finding at `EscapedJsonResultFactory.cs:22` is a false positive. The `Serialize` method applies `StringEscapeHandling.EscapeHtml` at both the `JsonSerializerSettings` level and the `JsonTextWriter` level, ensuring all HTML-significant characters (`<`, `>`, `&`, `'`, `"`) are Unicode-escaped in the serialized output. The resulting `ContentResult` carries `Content-Type: application/json; charset=utf-8`, which prevents browser HTML rendering. There is no exploitable XSS path from user-controlled data through this factory to the browser.
