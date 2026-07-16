## Scan: mmria s2i @ 558f4a87 — 2026-07-16

**Commit:** 558f4a87b4caf890a1df7af98b19d943a69b75d1  
**Branch:** development  
**SSC Application Version:** 10291  
**Findings:** 2 unique (1 Critical, 1 High)

---

## Finding 1 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:25

**SSC Issue ID:** 2235651  
**SSC Issue ID:** 2235652  
**Severity:** Critical  
**Rule GUID:** E89E1BB0-0F7E-4A15-A1F2-DF5F5F851E6C

**Verdict: Already remediated**

### Evidence

Taint path Fortify traces:

1. **Source:** User-controlled data enters an ASP.NET controller action (e.g., `caseController.cs:111`, `vitalsController.cs:115`).
2. **Propagation:** Tainted `object value` is passed to `EscapedJsonResultFactory.Create(value)` at `EscapedJsonResultFactory.cs:22`.
3. **Sink:** `Content = Serialize(value)` at `EscapedJsonResultFactory.cs:25` sets the HTTP response body.

**Sanitizer at sink — `StringEscapeHandling.EscapeHtml` (Json.NET):**

`EscapedJsonResultFactory.cs:15–19` declares serializer settings with HTML escaping:

```csharp
private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
{
    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    StringEscapeHandling = StringEscapeHandling.EscapeHtml,
    TypeNameHandling = TypeNameHandling.None
};
```

`EscapedJsonResultFactory.cs:33–38` applies the same setting to the `JsonTextWriter`:

```csharp
using var jsonWriter = new JsonTextWriter(stringWriter)
{
    CloseOutput = false,
    Formatting = Formatting.None,
    StringEscapeHandling = StringEscapeHandling.EscapeHtml
};
```

`StringEscapeHandling.EscapeHtml` (Json.NET 13.x) Unicode-escapes `<` → `\u003c`, `>` → `\u003e`, `&` → `\u0026`, `'` → `\u0027`, `"` → `\u0022`, preventing script injection even if response content were rendered in an HTML context.

**Response headers applied by `SecureEscapedJsonResult.ExecuteResultAsync` (`EscapedJsonResultFactory.cs:47–51`):**

- `Content-Type: application/json; charset=utf-8` — instructs browsers to treat the response as JSON, not HTML.
- `X-Content-Type-Options: nosniff` — prevents MIME-type sniffing that could cause browsers to render JSON as HTML.

The class name `EscapedJsonResultFactory` and inner class `SecureEscapedJsonResult` confirm this utility was purpose-built to provide XSS-safe JSON responses. The HTML-escaping serializer settings are active at both the `JsonSerializerSettings` level and the `JsonTextWriter` level, which together ensure every string value in the output is escaped.

### Verdict rationale

The taint path is real: user-controlled data can flow from controller actions through `Serialize(value)` into the HTTP response body. However, the fix is demonstrably present in the current codebase: `StringEscapeHandling.EscapeHtml` is configured at both the serializer and writer levels, and the response is served with `Content-Type: application/json; charset=utf-8` and `X-Content-Type-Options: nosniff`. These controls together eliminate the XSS exploit precondition (that the browser render tainted output as HTML). Fortify does not model Json.NET's `StringEscapeHandling.EscapeHtml` as an XSS sanitizer, producing a stale result. All three SSC Issue IDs (2235651, 2235652) correspond to data-flow traces through the same fixed serialization path.

---

## Finding 2 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**SSC Issue ID:** 2223458  
**Severity:** High  
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99

**Verdict: Not applicable / false positive**

### Evidence

Taint path Fortify traces:

1. **Source:** User-controlled input arrives as the `childDirectoryName` parameter in `EnsureContainedDirectoryExists` (`ContainedPathHelper.cs:144`).
2. **Propagation:** `childDirectoryName` flows into `ResolveContainedDirectoryPath` (`ContainedPathHelper.cs:126`), which calls `ValidateContainedName(childDirectoryName, ...)` (`ContainedPathHelper.cs:129`) and then `Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName))` (`ContainedPathHelper.cs:130`).
3. **Sink:** `Directory.CreateDirectory(safePath)` at `ContainedPathHelper.cs:148`.

**Allow-list sanitizer — `ValidateContainedName` (`ContainedPathHelper.cs:208–252`) neutralizes the taint before any file-system operation:**

Character allow-list (`ContainedPathHelper.cs:233–239`):
```csharp
foreach (var character in trimmedValue)
{
    if (!char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.')
    {
        throw new ArgumentException("Path segment contains unsupported characters.", paramName);
    }
}
```
This permits only `[A-Za-z0-9\-_.]`. Any character outside this set raises `ArgumentException` before the value reaches `Directory.CreateDirectory`.

Additional checks in `ValidateContainedName`:
- `ContainedPathHelper.cs:216–219`: rejects the relative path traversal operators `.` and `..`.
- `ContainedPathHelper.cs:226–230`: rejects rooted paths and any value containing `Path.DirectorySeparatorChar` or `Path.AltDirectorySeparatorChar`, blocking directory separator injection.
- `ContainedPathHelper.cs:241–244`: rejects `Path.GetInvalidFileNameChars()`.
- `ContainedPathHelper.cs:246–249`: rejects Windows reserved device names (`CON`, `PRN`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`).

**Defense-in-depth — `EnsureContainedPath` (`ContainedPathHelper.cs:254–260`):**
```csharp
if (!resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
}
```
Even if a bypass of the character allow-list were somehow possible, this check verifies the resolved absolute path begins with the trusted base directory.

**Reparse-point guard — `ThrowIfExistingPathOrAncestorIsReparsePoint` (`ContainedPathHelper.cs:262–268`):**
Traverses the entire ancestor chain of `safePath` and throws if any existing component is a symlink or junction, preventing symlink-based path escapes.

**Callers pass sanitized or hardcoded names:**
- `cvsAPIController.cs:77–79`: passes the hardcoded string literal `"csv"` — not user-controlled at all.
- `backup_managerController.cs:270–271` and `backup_managerController.cs:291`: pre-validates `folder` with `ValidateContainedName` before passing `safeFolderName` to `EnsureContainedDirectoryExists`, so the input is validated twice.

Search confirming no other callers pass unvalidated user input directly:
```
grep -rn "EnsureContainedDirectoryExists" source-code/mmria/
  source-code/mmria/mmria-server/Controllers/api/cvsAPIController.cs:77   (hardcoded "csv")
  source-code/mmria/mmria-server/Controllers/backup_managerController.cs:291  (pre-validated safeFolderName)
  source-code/mmria/mmria-server/util/ContainedPathHelper.cs:144          (definition)
```

### SWA Summary

Path Manipulation finding at `ContainedPathHelper.cs:148` is not exploitable. The `childDirectoryName` parameter is run through `ValidateContainedName` before any file-system operation is attempted. `ValidateContainedName` enforces a strict character allow-list of `[A-Za-z0-9\-_.]`, explicitly rejects `.`, `..`, path separators, rooted paths, and Windows device names. At both known call sites the input is either the hardcoded literal `"csv"` or a value already pre-validated with `ValidateContainedName`. A second containment check (`EnsureContainedPath`) and a reparse-point guard provide defense-in-depth. No path traversal outside the configured base directory is reachable.

### Verdict rationale

Fortify traces user-controlled input to `Directory.CreateDirectory(safePath)` at line 148, which would normally constitute a path manipulation vulnerability. However, the taint is fully neutralized by `ValidateContainedName`'s strict character allow-list (`[A-Za-z0-9\-_.]`) applied inside `ResolveContainedDirectoryPath` before the value reaches `Directory.CreateDirectory`. Fortify's data-flow engine does not model this allow-list as a sanitizer, producing a false positive. The containment check and reparse-point guard are additional layers that would independently block any theoretical bypass. The finding is not applicable to this codebase.
