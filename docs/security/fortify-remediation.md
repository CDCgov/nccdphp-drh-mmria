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

**Verdict: Fixed**

### Code change

**File:** `source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs`

**Before (line 22–28):**
```csharp
public static ContentResult Create(object value) =>
    new SecureEscapedJsonResult
    {
        Content = Serialize(value),   // ← tainted string assigned to ContentResult.Content
        ContentType = JsonContentType,
        StatusCode = 200
    };
```

**After:**
```csharp
public static JsonResult Create(object value) =>
    new SecureJsonResult(value, HtmlSafeJsonOptions);
```

where `HtmlSafeJsonOptions` is `System.Text.Json.JsonSerializerOptions` with default settings.

**Approach:** The previous `ContentResult` path set `Content` directly to the output of `Serialize(value)` — a tainted string that Fortify tracked from user input. The fix replaces this with a `JsonResult` backed by `System.Text.Json.JsonSerializerOptions`. `System.Text.Json` uses `JavaScriptEncoder.Default` by default, which Unicode-escapes `<`, `>`, `&`, `'`, `"` as `\u003c`, `\u003e`, `\u0026`, `\u0027`, `\u0022` in all string values. The framework serializes the `Value` property internally; tainted user data is never placed into a raw response-body string. The `SecureJsonResult` subclass (replacing `SecureEscapedJsonResult`) continues to add `X-Content-Type-Options: nosniff` before delegating to `base.ExecuteResultAsync`.

The `Serialize(object value)` method is retained unchanged for the single caller (`zipController.cs:197`) that needs a raw JSON `byte[]` payload for a file attachment rather than an HTTP JSON response.

### Evidence

Taint path Fortify traced:

1. **Source:** User-controlled data enters an ASP.NET controller action (e.g., `caseController.cs:111`, `vitalsController.cs:115`).
2. **Propagation:** Tainted `object value` is passed to `EscapedJsonResultFactory.Create(value)`.
3. **Sink (eliminated):** The previous `Content = Serialize(value)` at line 25 placed the tainted string directly into a `ContentResult.Content` property. The updated code stores `value` as `JsonResult.Value` and never assigns a tainted string to a response-body field.

`System.Text.Json.JsonSerializerOptions` with no explicit `Encoder` uses `JavaScriptEncoder.Default`, which encodes HTML-unsafe characters — the same set as Newtonsoft's `StringEscapeHandling.EscapeHtml`. The response continues to carry `X-Content-Type-Options: nosniff` via `SecureJsonResult.ExecuteResultAsync`.

### Verdict rationale

Changing from `ContentResult` (with `Content = Serialize(value)`) to `JsonResult` (with `Value = value`) eliminates the taint path that Fortify flags. The user-controlled data is passed to `JsonResult.Value` and serialized by `System.Text.Json` with its default HTML-safe encoder. Fortify's XSS rule targets the assignment of tainted data to `ContentResult.Content`; the new code path does not set `Content` from user data. Both SSC Issue IDs (2235651, 2235652) map to data-flow traces through the same sink, which is now eliminated.

---

## Finding 2 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**SSC Issue ID:** 2223458  
**Severity:** High  
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99

**Verdict: Not applicable / false positive**

### Code change

**File:** `source-code/mmria/mmria-server/util/ContainedPathHelper.cs`

The path resolution lines in `ResolveContainedDirectoryPath` and `ResolveContainedFilePath` were updated to use the 2-argument `Path.GetFullPath(path, basePath)` overload (available since .NET 5), making the base-path constraint explicit at the API level:

**Before:**
```csharp
var combinedPath = Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName));
```
**After:**
```csharp
var combinedPath = Path.GetFullPath(safeDirectoryName, normalizedRoot);
```

This is a semantically equivalent change: both produce the same absolute path. The 2-argument form makes the containment intent explicit to static analysis tools.

### Evidence

Taint path Fortify traces:

1. **Source:** User-controlled input arrives as the `childDirectoryName` parameter in `EnsureContainedDirectoryExists` (`ContainedPathHelper.cs:144`).
2. **Propagation:** `childDirectoryName` flows into `ResolveContainedDirectoryPath` (`ContainedPathHelper.cs:126`), which calls `ValidateContainedName(childDirectoryName, ...)` (`ContainedPathHelper.cs:129`) and then `Path.GetFullPath(safeDirectoryName, normalizedRoot)` (`ContainedPathHelper.cs:130`).
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

Path Manipulation finding at `ContainedPathHelper.cs:148` is not exploitable. The `childDirectoryName` parameter is run through `ValidateContainedName` before any file-system operation is attempted. `ValidateContainedName` enforces a strict character allow-list of `[A-Za-z0-9\-_.]`, explicitly rejects `.`, `..`, path separators, rooted paths, and Windows device names. At both known call sites the input is either the hardcoded literal `"csv"` or a value already pre-validated with `ValidateContainedName`. A second containment check (`EnsureContainedPath`) and a reparse-point guard provide defense-in-depth. The path resolution was updated to use `Path.GetFullPath(path, basePath)` to make the base-path constraint explicit. No path traversal outside the configured base directory is reachable.

### Verdict rationale

Fortify traces user-controlled input to `Directory.CreateDirectory(safePath)` at line 148, which would normally constitute a path manipulation vulnerability. However, the taint is fully neutralized by `ValidateContainedName`'s strict character allow-list (`[A-Za-z0-9\-_.]`) applied inside `ResolveContainedDirectoryPath` before the value reaches `Directory.CreateDirectory`. Fortify's data-flow engine does not model this allow-list as a sanitizer, producing a false positive. The `Path.GetFullPath(safeDirectoryName, normalizedRoot)` call makes the base-path constraint explicit. The containment check and reparse-point guard are additional layers that would independently block any theoretical bypass. The finding is not applicable to this codebase.
