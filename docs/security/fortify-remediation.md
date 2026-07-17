## Scan: mmria s2i @ 45204c84 — 2026-07-17

- **Commit:** `45204c84df00dfa1204f8ca1f06f0c9a43c91eb4` on `development`
- **SSC application version:** 10291
- **Findings processed:** 2 unique (1 Critical, 1 High)

---

## Finding 1 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:25

- **Severity:** Critical
- **Category:** Cross-Site Scripting: Reflected
- **Rule GUID:** `E89E1BB0-0F7E-4A15-A1F2-DF5F5F851E6C`
- **SSC Issue ID:** 2235651
- **SSC Issue ID:** 2235652

**Verdict:** Already remediated

### Evidence

**Taint path:**

User-controlled data (e.g., case records, user attributes) enters controller action methods such as `system_offlineController`, `_usersController`, `manage_usersController`, and `caseController`, and is then passed to `EscapedJsonResultFactory.Create(value)`. Inside `Create`, line 25 assigns the result of `Serialize(value)` to the `Content` property of an `ActionResult`:

```csharp
// EscapedJsonResultFactory.cs:22-28
public static ContentResult Create(object value) =>
    new SecureEscapedJsonResult
    {
        Content = Serialize(value),      // line 25 — flagged sink
        ContentType = JsonContentType,
        StatusCode = 200
    };
```

**Sanitizer at sink:** `Serialize` applies `StringEscapeHandling.EscapeHtml` on the `JsonTextWriter` (line 37) and in the `JsonSerializerSettings` instance used to create the serializer (line 18):

```csharp
// EscapedJsonResultFactory.cs:15-20
private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
{
    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    StringEscapeHandling = StringEscapeHandling.EscapeHtml,   // line 18
    TypeNameHandling = TypeNameHandling.None
};

// EscapedJsonResultFactory.cs:30-43
public static string Serialize(object value)
{
    using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
    using var jsonWriter = new JsonTextWriter(stringWriter)
    {
        CloseOutput = false,
        Formatting = Formatting.None,
        StringEscapeHandling = StringEscapeHandling.EscapeHtml   // line 37
    };

    JsonSerializer.Create(HtmlEscapingSerializerSettings).Serialize(jsonWriter, value);
    jsonWriter.Flush();
    return stringWriter.ToString();
}
```

`StringEscapeHandling.EscapeHtml` in Newtonsoft.Json unconditionally encodes `<` → `\u003c`, `>` → `\u003e`, `&` → `\u0026`, `'` → `\u0027`, and `"` → `\u0022` in all serialized string values. This encoding is applied before the content is written to the HTTP response, eliminating any HTML/script injection from the output.

Additionally, the `SecureEscapedJsonResult.ExecuteResultAsync` method sets `X-Content-Type-Options: nosniff` on every response (line 49), preventing MIME-type sniffing that could lead to XSS from misidentified content.

**Fortify Taxonomy / CWE precondition satisfied:** Fortify CWE-79 (Cross-Site Scripting) requires that user-controlled data reach an HTML output sink without encoding. Here, the encoding is applied unconditionally by the serializer before the content is placed into the HTTP response body. The taint path terminates at the HTML-escape sanitizer inside `Serialize`; the encoded output reaching `Content` is not exploitable.

### SWA Summary

`EscapedJsonResultFactory.Create` at line 25 serializes data with `StringEscapeHandling.EscapeHtml` (Newtonsoft.Json), encoding `<`, `>`, `&`, `'`, `"` as unicode escapes before the content reaches the HTTP response. The class was introduced specifically to prevent reflected XSS in JSON API responses. No code change is required; Fortify did not recognize the Newtonsoft.Json HTML-escape serializer configuration as a sanitizer.

---

## Finding 2 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

- **Severity:** High
- **Category:** Path Manipulation
- **Rule GUID:** `06C49ABE-9D01-4036-A3CB-E4D14DFE6D99`
- **SSC Issue ID:** 2223458

**Verdict:** Already remediated

### Evidence

**Taint path:**

The flagged sink is `Directory.CreateDirectory(safePath)` at line 148 inside `EnsureContainedDirectoryExists`. The `childDirectoryName` parameter can originate from external input (e.g., `cvsAPIController` constructor, which passes a configuration-derived value). The full call chain before the sink is:

```
EnsureContainedDirectoryExists(trustedBaseDirectory, childDirectoryName)   [line 143]
  → ResolveContainedDirectoryPath(trustedBaseDirectory, childDirectoryName)  [line 146 → 126]
      → NormalizeTrustedDirectoryRoot(trustedBaseDirectory)                  [line 128]
      → ValidateContainedName(childDirectoryName)                            [line 129]
      → Path.GetFullPath(Path.Combine(normalizedRoot, safeDirectoryName))    [line 130]
      → EnsureContainedPath(normalizedRoot, combinedPath)                    [line 131]
  → ThrowIfExistingPathOrAncestorIsReparsePoint(safePath)                   [line 147]
  → Directory.CreateDirectory(safePath)                                      [line 148 — flagged sink]
```

**Sanitizer chain at source:**

1. **`ValidateContainedName` (lines 208–252)** — character-level allow-list guard applied before any path combination:
   - Rejects null, empty, or whitespace values (line 210).
   - Rejects `.` and `..` (line 216).
   - Rejects values ending in `.` or ` ` on Windows (line 221).
   - Rejects rooted paths or values containing `Path.DirectorySeparatorChar` / `Path.AltDirectorySeparatorChar` (lines 226–231), blocking directory separators entirely.
   - Enforces allow-list: only letters, digits, `-`, `_`, `.` are accepted — all other characters throw (lines 233–239).
   - Rejects any character in `Path.GetInvalidFileNameChars()` (lines 241–244).
   - Rejects Windows reserved device names (CON, PRN, AUX, NUL, COM1–9, LPT1–9) (lines 246–249).

2. **`EnsureContainedPath` (lines 254–260)** — after `Path.GetFullPath` canonicalizes the combined path, this guard confirms the result starts with the trusted base directory (case-insensitive), throwing if any path-traversal still escaped the allow-list.

3. **`ThrowIfExistingPathOrAncestorIsReparsePoint` (lines 262–268)** — walks every existing path segment from the root to the target and throws if any is a reparse point (symlink/junction), blocking symlink-escape attacks.

The `safePath` value reaching `Directory.CreateDirectory(safePath)` at line 148 is the fully-canonicalized, allow-list-validated, base-directory-confined, reparse-point-checked path. No path traversal is possible.

**Fortify Taxonomy / CWE precondition satisfied:** Fortify CWE-22 (Path Traversal) requires that user input influence a file-system path in a way that escapes an intended directory. The three-layer sanitization chain (`ValidateContainedName` → `EnsureContainedPath` → `ThrowIfExistingPathOrAncestorIsReparsePoint`) eliminates all traversal vectors before the path reaches the filesystem API. Fortify does not recognize this custom sanitization chain as equivalent to a known sanitizer annotation.

### SWA Summary

`Directory.CreateDirectory(safePath)` at line 148 receives a path that has passed through `ValidateContainedName` (allow-list: letters, digits, `-`, `_`, `.` only; rejects separators and relative operators), `Path.GetFullPath` canonicalization, `EnsureContainedPath` base-directory confinement check, and a reparse-point guard. No path traversal is possible through this call. The `ContainedPathHelper` utility class was introduced specifically to contain all file-system operations to a trusted base directory. No code change is required; Fortify did not recognize the custom sanitization chain as a known sanitizer.
