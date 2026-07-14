# Fortify remediation history

## Scan: 2026-07-14 — mmria s2i @ 09bed04f

- Commit: `09bed04f7a12f07e73b37ded36163be15f218828`
- SSC application version: `10291`
- Workflow run: `29366749439`

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**SSC Issue ID:** 2223458
**Verdict:** Fixed

### Evidence

- `source-code/mmria/mmria-server/util/ContainedPathHelper.cs` now validates `childDirectoryName` as a single contained path segment, resolves the final path under the normalized trusted root, rejects reparse points, and creates the directory through `new DirectoryInfo(normalizedRoot).CreateSubdirectory(safeDirectoryName)`.
- The updated sink no longer passes a combined user-influenced path string into `Directory.CreateDirectory`; it creates only a validated child segment beneath the trusted base directory.

### Verdict rationale

- The fix keeps directory creation anchored to the trusted root and preserves the existing containment and reparse-point checks, eliminating the path-manipulation sink Fortify reported.

## Finding 2 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**SSC Issue ID:** 2221778
**Verdict:** Fixed

### Evidence

- `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs` still constrains the bearer token with `ValidateHeaderValue(...)` and the `BearerTokenPattern` allow-list.
- The helper now converts the token into an `AuthenticationHeaderValue` only through `AuthenticationHeaderValue.TryParse(...)`, then verifies the parsed scheme and parameter match the sanitized bearer token exactly before returning it.

### Verdict rationale

- The Authorization header is now built only after framework parsing confirms a valid bearer header structure, preventing header-manipulation characters from reaching the outbound request sink.

## Finding 3 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22

**SSC Issue ID:** 2225918
**Verdict:** Fixed

### Evidence

- `source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs` continues to serialize payloads with `StringEscapeHandling.EscapeHtml`.
- The factory now returns a `FileContentResult` containing UTF-8 encoded JSON bytes with the `application/json; charset=utf-8` content type instead of writing raw string content through `ContentResult`.

### Verdict rationale

- Returning escaped JSON as a typed file response keeps the payload in a JSON-only response path and removes the reflected-string HTML sink Fortify reported.
