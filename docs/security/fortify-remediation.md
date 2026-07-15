## Scan: mmria s2i @ 1c266b5e — 2026-07-15

- **Commit:** `1c266b5ed2adae4ca2f6a47f80879fea42b710d6` on `development`
- **SSC application version:** 10291
- **Findings processed:** 3 (1 Critical, 2 High)

---

## Finding 1 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:22

**Severity:** Critical  
**Rule GUID:** E89E1BB0-0F7E-4A15-A1F2-DF5F5F851E6C  
**SSC Issue ID:** 2225918  
**Verdict:** Not applicable / false positive

### Evidence

**Taint source → sink trace:**

External user-supplied data flows into application logic and is ultimately passed to
`EscapedJsonResultFactory.Create(object value)` or `EscapedJsonResultFactory.Serialize(object value)`.

**Sink — `EscapedJsonResultFactory.cs:22`:**
```csharp
// Lines 19-25 (EscapedJsonResultFactory.cs)
public static ContentResult Create(object value) =>
    new()
    {
        Content = Serialize(value),   // ← Fortify sink (line 22)
        ContentType = JsonContentType,
        StatusCode = 200
    };
```

**Guard 1 — HTML-escaping serializer settings (lines 12-17):**
```csharp
private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
{
    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    StringEscapeHandling = StringEscapeHandling.EscapeHtml,   // ← escapes < > & ' "
    TypeNameHandling = TypeNameHandling.None
};
```

**Guard 2 — HTML-escaping on the JsonTextWriter itself (lines 30-35):**
```csharp
using var jsonWriter = new JsonTextWriter(stringWriter)
{
    CloseOutput = false,
    Formatting = Formatting.None,
    StringEscapeHandling = StringEscapeHandling.EscapeHtml   // ← second layer
};
```

**Guard 3 — `application/json` Content-Type (line 10 + line 23):**
```csharp
private const string JsonContentType = "application/json; charset=utf-8";
// ...
ContentType = JsonContentType,
```

**Why the taint path is not exploitable:**

`StringEscapeHandling.EscapeHtml` in Newtonsoft.Json encodes `<` → `\u003c`,
`>` → `\u003e`, `&` → `\u0026`, `'` → `\u0027`, `"` → `\u0022` — the complete set of
characters needed for HTML injection. These escapes are applied at two independent layers
(serializer settings _and_ the JsonTextWriter instance), so even if one were bypassed the
other would catch it. Additionally, the response is served with `Content-Type:
application/json; charset=utf-8`, which causes browsers to parse the body as JSON data
rather than HTML, preventing script execution even if the escaping were somehow absent.
The combination of double-layer HTML encoding plus a non-HTML Content-Type fully neutralises
reflected XSS for this code path. Fortify does not model the Content-Type + dual escaping
combination and emits a false positive.

**CWE-79 precondition not met:** CWE-79 (XSS) requires that user-controlled data be
reflected into an HTML rendering context without encoding. The `application/json` response
type is not an HTML rendering context, and the mandatory HTML-escape encoding prevents
injection even in contexts where JSON is embedded in HTML pages.

### SWA Summary

Cross-Site Scripting: Reflected finding at `EscapedJsonResultFactory.cs:22` is a false
positive. The serializer uses `StringEscapeHandling.EscapeHtml` at two independent layers
(serializer settings and JsonTextWriter instance), encoding `<`, `>`, `&`, `'`, `"` as
Unicode escape sequences. The response is served with `Content-Type: application/json`
which is not an HTML rendering context. No user-controlled data can reach the browser as
unescaped HTML through this code path.

---

## Finding 2 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**Severity:** High  
**Rule GUID:** 0858F67A-D592-4E5F-8C3A-514CB484E1CB  
**SSC Issue ID:** 2221778  
**Verdict:** Not applicable / false positive

### Evidence

**Taint source → sink trace:**

External user-supplied `bearerToken` parameter enters
`CreateBearerAuthenticationHeaderValue(string bearerToken, string paramName)`.

**Step 1 — input validation, length check, visible-ASCII allow-list (lines 34-43):**
```csharp
// Lines 34-43 (OutboundRequestSecurityHelper.cs)
if (string.IsNullOrWhiteSpace(bearerToken))
{
    throw new ArgumentException("****** is required.", paramName);
}

var sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096);
if (!BearerTokenPattern.IsMatch(sanitizedToken))
{
    throw new ArgumentException("****** contains unexpected characters.", paramName);
}
```

**`ValidateHeaderValue` internals (lines 50-73):**
```csharp
// Lines 55-70 (OutboundRequestSecurityHelper.cs)
var trimmedValue = value.Trim();
if (trimmedValue.Length > maxLength) { throw ...; }

if (trimmedValue.Any(character => !IsVisibleAsciiHeaderCharacter(character)))
{
    throw new ArgumentException("Header value contains unsupported characters.", paramName);
}

var sanitizedValue = mmria.common.getset.CouchDbHttpClient.SanitizeHeader(trimmedValue)?.Trim();
if (string.IsNullOrWhiteSpace(sanitizedValue) || !string.Equals(trimmedValue, sanitizedValue, StringComparison.Ordinal))
{
    throw new ArgumentException("Header value contains invalid characters.", paramName);
}
```

`IsVisibleAsciiHeaderCharacter` (line 75-76) accepts only `char >= 0x20 && char <= 0x7E`,
which explicitly excludes `\r` (0x0D) and `\n` (0x0A) — the characters required for
CRLF header injection.

**`CouchDbHttpClient.SanitizeHeader` (common/getset/CouchDbHttpClient.cs:674-691):**
```csharp
foreach (var ch in headerString)
{
    if ((ch == 9 || ch >= 32) && ch != 127)
    {
        sb.Append(ch);
    }
}
```
This strips any character outside `[TAB, 0x20-0x7E]`. The change-detection guard at line 67
then throws if the sanitized form differs from the input — meaning any input containing
disallowed characters is always rejected, never silently passed through.

**Step 2 — regex allow-list (line 40):**
```csharp
private static readonly Regex BearerTokenPattern =
    new("^[A-Za-z0-9._~+/=-]{1,4096}$", ...);
```
Only alphanumeric and `._~+/=-` are accepted. `\r`, `\n`, `:`; `,`, space, and all other
CRLF/folding characters are excluded.

**Sink (line 45):**
```csharp
return new AuthenticationHeaderValue("Bearer", sanitizedToken);
```

**Why the taint path is not exploitable:**

Header Manipulation (CWE-113) requires injection of `\r\n` sequences into a header value
to split the response and inject additional headers. The `IsVisibleAsciiHeaderCharacter`
check at line 61 unconditionally rejects `\r` (0x0D) and `\n` (0x0A) before the token
reaches `sanitizedToken`. Any input containing these control characters causes an
`ArgumentException` to be thrown at line 63, well before reaching the sink at line 45.
The BearerTokenPattern regex at line 40 provides a second, independent block on any
non-ASCII or control character. No CRLF sequence can reach `AuthenticationHeaderValue`.

**CWE-113 precondition not met:** CWE-113 requires unsanitized newline characters in
header values. Both `IsVisibleAsciiHeaderCharacter` and `BearerTokenPattern` reject
`\r`/`\n` unconditionally — the precondition is structurally absent.

### SWA Summary

Header Manipulation finding at `OutboundRequestSecurityHelper.cs:45` is a false positive.
The bearer token is validated through two independent guards before reaching the sink:
(1) `IsVisibleAsciiHeaderCharacter` rejects characters below 0x20, which unconditionally
blocks `\r` (0x0D) and `\n` (0x0A) required for CRLF injection; (2) `BearerTokenPattern`
regex `^[A-Za-z0-9._~+/=-]{1,4096}$` allows only safe alphanumeric characters. Any input
containing injection-capable control characters is rejected with `ArgumentException` before
the `AuthenticationHeaderValue` constructor is called.

---

## Finding 3 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**Severity:** High  
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99  
**SSC Issue ID:** 2223458  
**Verdict:** Not applicable / false positive

### Evidence

**Taint source → sink trace:**

External user-supplied `childDirectoryName` parameter enters
`EnsureContainedDirectoryExists(string trustedBaseDirectory, string childDirectoryName)`.

**Step 1 — resolve and validate path (lines 146-147):**
```csharp
// Lines 144-149 (ContainedPathHelper.cs)
public static string EnsureContainedDirectoryExists(string trustedBaseDirectory, string childDirectoryName)
{
    var safePath = ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName);
    ThrowIfExistingPathOrAncestorIsReparsePoint(safePath, nameof(childDirectoryName));
    Directory.CreateDirectory(safePath);   // ← Fortify sink (line 148)
    return safePath;
}
```

**`ResolveContainedDirectoryPath` internals (lines 126-133):**
```csharp
public static string ResolveContainedDirectoryPath(string trustedBaseDirectory, string childDirectoryName)
{
    var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, ...);
    var safeDirectoryName = ValidateContainedName(childDirectoryName, ...); // ← guard
    var combinedPath = Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName));
    EnsureContainedPath(normalizedRoot, combinedPath, ...);  // ← containment check
    return combinedPath;
}
```

**`ValidateContainedName` guard (lines 208-244):**
```csharp
// Rejects: null/empty/whitespace, "." and "..", paths ending with "." or " " on Windows,
// rooted paths, any occurrence of DirectorySeparatorChar or AltDirectorySeparatorChar,
// chars in Path.GetInvalidFileNameChars(), and reserved Windows device names (CON, NUL, etc.)
if (trimmedValue is "." or "..") { throw ...; }
if (Path.IsPathRooted(trimmedValue) ||
    trimmedValue.Contains(Path.DirectorySeparatorChar) ||
    trimmedValue.Contains(Path.AltDirectorySeparatorChar)) { throw ...; }
if (trimmedValue.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { throw ...; }
if (IsReservedWindowsDeviceName(trimmedValue)) { throw ...; }
```

**`EnsureContainedPath` guard (lines 246-252):**
```csharp
private static void EnsureContainedPath(string trustedBaseDirectory, string resolvedPath, string paramName)
{
    if (!resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
    }
}
```

**`ThrowIfExistingPathOrAncestorIsReparsePoint` guard (lines 254-260):**
Walks the full path chain and throws if any existing component is a reparse point (symlink),
preventing symlink-escape attacks that might survive the path containment check.

**Why the taint path is not exploitable:**

Path Manipulation (CWE-22 / path traversal) requires that user-supplied data inject `../`
sequences or absolute path roots to escape the intended directory. The guard chain at
`ValidateContainedName` unconditionally throws if `childDirectoryName` contains `./`,
`..\`, `/../`, a directory separator, an absolute root, or invalid filename characters.
After validation, `Path.GetFullPath(Path.Combine(...))` resolves the path and
`EnsureContainedPath` verifies it starts with the trusted root — a defence-in-depth check
that catches any edge case in path canonicalization. A third layer (`ThrowIfExistingPathOrAncestorIsReparsePoint`)
blocks reparse-point/symlink escapes that could otherwise bypass the string-comparison
check. No user-controlled traversal sequence can survive all three guards to reach
`Directory.CreateDirectory` at line 148.

**CWE-22 precondition not met:** CWE-22 requires user-supplied path separators or
traversal tokens to reach the file-system call. All such characters are blocked by
`ValidateContainedName` before `Path.Combine` is called, and the final resolved path is
independently verified against the trusted base directory. The precondition is
structurally absent.

### SWA Summary

Path Manipulation finding at `ContainedPathHelper.cs:148` is a false positive. The
user-supplied path segment is processed through a three-layer guard chain before reaching
`Directory.CreateDirectory`: (1) `ValidateContainedName` rejects path separators, traversal
operators (`.`, `..`), absolute roots, invalid filename characters, and reserved Windows
device names; (2) `EnsureContainedPath` verifies the fully resolved path starts with the
trusted base directory after `Path.GetFullPath`; (3) `ThrowIfExistingPathOrAncestorIsReparsePoint`
blocks symlink escape attacks. No traversal sequence can survive all three layers to reach
the file-system sink.
