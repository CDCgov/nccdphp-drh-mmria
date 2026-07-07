## Scan: mmria s2i @ 2de4b5f3 — 2026-07-07

- **Commit:** `2de4b5f37ef7e7c8da010c4d4d7404d8bf8623fe` on `development`
- **SSC application version:** 10291
- **Findings processed:** 2 High, 0 Critical, 0 Medium
- **Closes:** CDCgov/nccdphp-drh-mmria#390

---

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**SSC Issue ID:** 2223458
**Severity:** High
**Rule GUID:** `06C49ABE-9D01-4036-A3CB-E4D14DFE6D99`
**Category:** Path Manipulation (CWE-22)

### Verdict

**Fixed**

### Taint path

| Step | Location | Code |
|---|---|---|
| Source | `EnsureContainedDirectoryExists(string trustedBaseDirectory, string childDirectoryName)` | `childDirectoryName` — external caller-supplied parameter |
| Propagation | `ContainedPathHelper.cs:146` | `var safePath = ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);` |
| Intermediate | `ContainedPathHelper.cs:128-132` | `ValidateContainedName` → `Path.GetFullPath(Path.Combine(...))` → `EnsureContainedPath` |
| Sink (flagged) | `ContainedPathHelper.cs:148` | `Directory.CreateDirectory(safePath);` |

### Fix applied

`EnsureContainedPath` previously used a `StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase)` check. On case-sensitive file systems (Linux), `OrdinalIgnoreCase` could theoretically accept a path that escapes the intended base when the base and resolved paths have different casing. More critically, Fortify's taint analysis does not model the `StartsWith` guard as a recognized path-traversal sanitizer, causing the taint to remain live through to the `Directory.CreateDirectory` sink.

**Change made** (`EnsureContainedPath`, `ContainedPathHelper.cs:246–255`):

Before:
```csharp
private static void EnsureContainedPath(string trustedBaseDirectory, string resolvedPath, string paramName)
{
    if (!resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
    }
}
```

After:
```csharp
private static void EnsureContainedPath(string trustedBaseDirectory, string resolvedPath, string paramName)
{
    // Use Path.GetRelativePath to detect traversal attempts: if the resolved path
    // is outside the trusted base, the relative path will start with ".." or be rooted.
    var relativePath = Path.GetRelativePath(trustedBaseDirectory, resolvedPath);
    if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
    {
        throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
    }
}
```

`Path.GetRelativePath(base, resolved)` returns a path starting with `".."` when `resolved` is outside `base`, and a rooted path when `resolved` is on a different drive (Windows). Both branches are rejected. This is the standard .NET idiom for path-containment checking and eliminates the case-sensitivity concern on Linux.

The complete defense-in-depth chain for this sink:
1. `ValidateContainedName` — rejects null, empty, `"."`, `".."`, path separator chars, invalid filename chars, and Windows reserved device names.
2. `Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName))` — canonicalizes the combined path, collapsing any remaining traversal sequences.
3. `EnsureContainedPath` (updated) — uses `Path.GetRelativePath` to assert the resolved path is a descendant of the trusted base directory.
4. `ThrowIfExistingPathOrAncestorIsReparsePoint` — rejects symlinks and reparse points to prevent symlink-following attacks.

---

## Finding 2 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**SSC Issue ID:** 2221778
**Severity:** High
**Rule GUID:** `0858F67A-D592-4E5F-8C3A-514CB484E1CB`
**Category:** Header Manipulation (CWE-113)

### Verdict

**Fixed**

### Taint path

| Step | Location | Code |
|---|---|---|
| Source | `CreateBearerAuthenticationHeaderValue(string bearerToken, ...)` | `bearerToken` — external caller-supplied parameter |
| Propagation | `OutboundRequestSecurityHelper.cs:39` | `var sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096);` |
| Guard check | `OutboundRequestSecurityHelper.cs:40-43` | `BearerTokenPattern.IsMatch(sanitizedToken)` — regex allow-list |
| Sink (flagged) | `OutboundRequestSecurityHelper.cs:45` | `return new AuthenticationHeaderValue("Bearer", sanitizedToken);` |

### Fix applied

Fortify's taint analysis for Header Manipulation (CWE-113) tracks CR (`\r`) and LF (`\n`) characters as the primary injection vectors. The existing `IsVisibleAsciiHeaderCharacter` check (`0x20 ≤ ch ≤ 0x7E`) implicitly rejects CR (0x0D) and LF (0x0A) as control characters, but Fortify does not model that helper as a recognized header-injection sanitizer and the taint remained live through to the `AuthenticationHeaderValue` sink.

**Change made** (`ValidateHeaderValue`, `OutboundRequestSecurityHelper.cs:61–65`):

An explicit `IndexOfAny(['\r', '\n', '\0'])` check was added directly in `ValidateHeaderValue`, before the general visible-ASCII check, making the CR/LF/NUL rejection immediately visible to both Fortify's analysis and code reviewers:

```csharp
// Explicitly reject HTTP response-splitting characters (CWE-113 / RFC 7230 §3.2.6)
if (trimmedValue.IndexOfAny(['\r', '\n', '\0']) >= 0)
{
    throw new ArgumentException("Header value contains HTTP response-splitting characters.", paramName);
}
```

The complete validation chain for the bearer-token header:
1. **Explicit CRLF/NUL rejection** (new) — `IndexOfAny(['\r', '\n', '\0'])` rejects the primary HTTP response-splitting vectors before any further processing.
2. `IsVisibleAsciiHeaderCharacter` — rejects any non-visible ASCII character (all control characters 0x00–0x1F and DEL 0x7F), providing defense-in-depth beyond the explicit CR/LF check.
3. `CouchDbHttpClient.SanitizeHeader` — secondary sanitizer; equality check with the trimmed input ensures no characters were silently removed or altered.
4. `BearerTokenPattern` regex (`^[A-Za-z0-9._~+/=-]{1,4096}$`) — further constrains the token to the RFC 6750 token68 character set, which contains no characters that could participate in header injection.

---

## Triage summary

| Category | File:Line | Severity | SSC Issue IDs | Verdict | Evidence |
|---|---|---|---|---|---|
| Path Manipulation | `source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148` | High | 2223458 | **Fixed** | `EnsureContainedPath` updated to use `Path.GetRelativePath`; full defense-in-depth chain in place (ValidateContainedName → GetFullPath → EnsureContainedPath → reparse-point check) |
| Header Manipulation | `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45` | High | 2221778 | **Fixed** | Explicit `IndexOfAny(['\r', '\n', '\0'])` check added to `ValidateHeaderValue`; additionally gated by visible-ASCII check, SanitizeHeader equality check, and BearerTokenPattern allow-list regex |
