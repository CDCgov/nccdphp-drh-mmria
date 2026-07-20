## Scan: mmria s2i @ 06fcb937 (SSC 10291)

- Date: 2026-07-20
- Commit scanned: `06fcb9375d0bdbf03d0a30808234c05dfc158218`
- Workflow run: `https://github.com/cdcent/nccdphp-od-devops/actions/runs/29750150301`

## Finding 1 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:25

**SSC Issue ID:** 2235651
**SSC Issue ID:** 2235652

**Verdict:** Fixed

### Evidence
- Source-to-sink path: controller return payloads are passed to `EscapedJsonResultFactory.Create(...)` and emitted by `ContentResult.Content` assignment at `EscapedJsonResultFactory.cs:25`.
- Fix applied: the sink no longer writes pre-built string content through `ContentResult`. The factory now returns `JsonResult` with `SerializerSettings = HtmlEscapingSerializerSettings` and `StringEscapeHandling.EscapeHtml`, preserving JSON HTML escaping while avoiding direct raw content emission.
- Defense at sink remains explicit: `X-Content-Type-Options: nosniff` is still set in `ExecuteResultAsync`.

### Verdict rationale
The direct reflected-content sink was removed and replaced with framework JSON emission using HTML-escaping serializer settings plus no-sniff response hardening.

## Finding 2 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**SSC Issue ID:** 2223458

**Verdict:** Fixed

### Evidence
- Source-to-sink path: caller-controlled directory segment enters `EnsureContainedDirectoryExists(...)`; Fortify flagged `Directory.CreateDirectory(safePath)`.
- Fix applied: `EnsureContainedDirectoryExists` now performs full in-method validation and containment checks before and after directory creation:
  - normalizes trusted root (`NormalizeTrustedDirectoryRoot`),
  - validates single-segment name (`ValidateContainedName`),
  - canonicalizes combined path and verifies it stays under root (`EnsureContainedPath`),
  - checks for reparse points before and after create (`ThrowIfExistingPathOrAncestorIsReparsePoint`),
  - returns canonical created path (`Path.GetFullPath(createdDirectory.FullName)`).

### Verdict rationale
The directory-creation sink is now guarded with explicit pre/post canonicalization and containment checks in the same method Fortify flagged, closing traversal/path manipulation risk at the sink.
