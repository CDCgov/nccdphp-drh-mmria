# Fortify remediation history

## Scan: 2026-07-16 — mmria s2i @ 609db31044d34809ffc447b67c5261be9a081617

- **SSC application version:** 10291
- **Workflow run:** `29462402922` (`cdcent/nccdphp-od-devops`) — GitHub MCP access returned `404` for this external run during remediation, so repo-local validation and code inspection were used for this pass.
- **Unique findings in scope:** 2
- **Resolved in this pass:** 2 fixed

## Finding 1 — Path Manipulation at source-code/mmria/mmria-server/util/ContainedPathHelper.cs:148

**SSC Issue ID:** 2223458
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99
**Severity:** High
**Verdict:** Fixed

### Evidence

- The vulnerable sink was `Directory.CreateDirectory(safePath)` on the derived child path in `source-code/mmria/mmria-server/util/ContainedPathHelper.cs:144-159`.
- The remediation now:
  - normalizes the trusted root with `NormalizeTrustedDirectoryRoot(...)`,
  - validates the child name as a single contained segment with `ValidateContainedName(...)`,
  - verifies containment with `EnsureContainedPath(...)`,
  - rejects reparse points on both the trusted root and resolved child path with `ThrowIfExistingPathOrAncestorIsReparsePoint(...)`, and
  - creates the child directory with `new DirectoryInfo(normalizedRoot).CreateSubdirectory(safeDirectoryName)` instead of creating the combined path directly.
- This keeps directory creation anchored to the trusted base directory while limiting attacker-controlled input to a single validated child segment.
- Validation: `dotnet build /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria.sln --nologo`

### Verdict rationale

The fix removes the direct directory-creation sink on the combined user-influenced path and replaces it with trusted-root directory creation plus a validated single-segment subdirectory creation API. This satisfies the containment requirement for the file-system sink and preserves the existing behavior for callers that need a guaranteed child export directory.

## Finding 2 — Cross-Site Scripting: Reflected at source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:25

**SSC Issue ID:** 2235651
**SSC Issue ID:** 2235652
**Rule GUID:** E89E1BB0-0F7E-4A15-A1F2-DF5F5F851E6C
**Severity:** Critical
**Verdict:** Fixed

### Evidence

- The vulnerable sink was `Content = Serialize(value)` in `source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:23-24`, which pushed serialized user-influenced data through `ContentResult`.
- The serializer still applies HTML escaping with `StringEscapeHandling.EscapeHtml` in `source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:16-42`.
- The remediation changes the response sink to `FileContentResult` backed by explicit UTF-8 JSON bytes via `SerializeToUtf8Bytes(value)` in `source-code/mmria/mmria-server/util/EscapedJsonResultFactory.cs:23-55`.
- The response remains constrained to `application/json; charset=utf-8` and still sets `X-Content-Type-Options: nosniff` before writing the body.
- Validation: `dotnet build /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria.sln --nologo`

### Verdict rationale

The fix keeps the existing HTML-escaped JSON serializer but replaces the reflective string-content sink with an explicit byte-oriented JSON file result. That narrows the response-writing surface, preserves the JSON media type and `nosniff` defense, and removes the direct `ContentResult` pattern Fortify flagged.
