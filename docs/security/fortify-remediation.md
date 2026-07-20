## Scan: e752bc03dfc428990041ab21328f888dc6c96d1d — 2026-07-20

- **Commit:** `e752bc03dfc428990041ab21328f888dc6c96d1d` on `development`
- **SSC application version:** 12317
- **Findings processed:** 2 High, 0 Critical, 0 Medium

---

## Finding 1 — Path Manipulation at nccdphp-drh-mmria-services/mmria.services/Actors/ExportQueue/Process_Export_Queue.cs:537

**SSC Issue ID:** 2236373

**Severity:** High  
**Category:** Path Manipulation  
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99

**Verdict:** Fixed

### Taint path

| Step | File:Line | Code |
|------|-----------|------|
| Source | `Process_Export_Queue.cs:529` | `var validated_file_name = PathSanitizer.ValidatePathSegment(item_to_process.file_name, …)` — `item_to_process.file_name` originates from a CouchDB document retrieved by the export queue; Fortify treats database-sourced strings as tainted. |
| Propagation | `Process_Export_Queue.cs:530` | `string item_directory_name = System.IO.Path.GetFileNameWithoutExtension(validated_file_name);` — `GetFileNameWithoutExtension` does not constitute a sanitizer recognized by Fortify. |
| Propagation | `Process_Export_Queue.cs:531` | `string export_directory = System.IO.Path.Combine(scheduleInfoMessage.export_directory, item_directory_name);` — plain `Path.Combine` does not canonicalize the path; a crafted `item_directory_name` (e.g. containing `..`) could escape the base directory. |
| Sink | `Process_Export_Queue.cs:537` | `System.IO.Directory.Delete(export_directory, true);` — file-system destructive operation on unverified path. |

### Fix applied

Replaced `System.IO.Path.Combine(scheduleInfoMessage.export_directory, item_directory_name)` at line 531 with `PathSanitizer.ResolveContainedPath(scheduleInfoMessage.export_directory, item_directory_name, nameof(item_directory_name))`.

`ResolveContainedPath` (added to `PathSanitizer.cs`):
1. Calls `ValidatePathSegment` on the segment (rejects traversal sequences, separators, rooted paths, and illegal characters).
2. Resolves both the base directory and the combined path to their canonical absolute forms via `Path.GetFullPath`.
3. Appends a directory separator to the normalized root before comparing, preventing sibling-directory prefix confusion.
4. Asserts the resolved combined path starts with the normalized root, throwing `ArgumentException` if it escapes the base directory.

This is the standard canonicalization + containment check (CWE-22 / OWASP Path Traversal defense) that satisfies Fortify's path-manipulation rule.

---

## Finding 2 — Path Manipulation at nccdphp-drh-mmria-services/mmria.services/Actors/ExportQueue/Process_Export_Queue.cs:552

**SSC Issue ID:** 2236374

**Severity:** High  
**Category:** Path Manipulation  
**Rule GUID:** 06C49ABE-9D01-4036-A3CB-E4D14DFE6D99

**Verdict:** Fixed

### Taint path

| Step | File:Line | Code |
|------|-----------|------|
| Source | `Process_Export_Queue.cs:529` | `var validated_file_name = PathSanitizer.ValidatePathSegment(item_to_process.file_name, …)` — `item_to_process.file_name` originates from a CouchDB document retrieved by the export queue; Fortify treats database-sourced strings as tainted. |
| Propagation | `Process_Export_Queue.cs:546` | `string file_path = System.IO.Path.Combine(scheduleInfoMessage.export_directory, validated_file_name);` — plain `Path.Combine` does not canonicalize; a crafted `validated_file_name` (or a tainted `export_directory`) could escape the base directory in Fortify's model. |
| Sink | `Process_Export_Queue.cs:552` | `System.IO.File.Delete(file_path);` — file-system destructive operation on unverified path. |

### Fix applied

Replaced `System.IO.Path.Combine(scheduleInfoMessage.export_directory, validated_file_name)` at line 546 with `PathSanitizer.ResolveContainedPath(scheduleInfoMessage.export_directory, validated_file_name, nameof(validated_file_name))`.

`ResolveContainedPath` applies the same canonicalization + containment guard described in Finding 1. The segment (`validated_file_name`) is a single filename already cleared by `ValidatePathSegment`; `ResolveContainedPath` additionally verifies via `Path.GetFullPath` that no symlink or OS-level path resolution can redirect outside the base directory before the `File.Delete` call executes.

---
