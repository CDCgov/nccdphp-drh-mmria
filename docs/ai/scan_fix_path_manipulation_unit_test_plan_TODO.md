# Scan3 Path-Manipulation Unit Test Confirmation Plan

- Status: Draft
- Created: 2026-05-14
- Scope: Focused unit-test and guard-test validation before the broader path-storage refactor.
- Source scan folder: `docs/ai/local/scans3`

## Summary

This document captures the focused test-first validation step for the Scan3 path-manipulation remediation effort. The immediate goal is to prove that the proposed approach can make Fortify-visible path manipulation drop, instead of only moving findings to new line numbers or relying on false-positive tags.

This is not the full remediation implementation. It is the confirmation layer to build and run before taking the larger storage and path abstraction refactor across `mmria-server` and `mmria.services`.

## Test Focus

- Add shared path-safety unit tests for generated artifact names, trusted roots, exact-match directory lookup, traversal rejection, rooted-path rejection, invalid filename rejection, and reparse-point rejection.
- Add server-focused tests around the current high-noise flows: backup download proxying, CVS cached PDF lookup, STEVE/PDF file read/delete, and malicious filename inputs.
- Add services-focused tests around backup file listing/download, export queue physical storage names, export delete, CSV/XLSX writer output, backup document output, and compression manifests.
- Include static guard tests that fail if Scan3-targeted code reintroduces direct `Path.Combine` with request, CouchDB, queue, or external filename values.

## Acceptance Criteria

- Focused tests pass before any broad rollout.
- New tests prove public download names stay stable while internal physical names can be generated.
- Malicious inputs such as `../x`, rooted paths, nested paths, Windows device names, trailing-dot names, and separator variants are rejected or return not found.
- Tests cover legacy fallback behavior for existing artifacts without new storage metadata.
- After the prototype, rerun Fortify or a focused scan to confirm findings drop instead of merely moving lines.

## Proposed Test Buckets

### Shared Path Safety

- Generated artifact names are created from trusted inputs only, such as constants, timestamps, counters, and GUIDs.
- Trusted directory roots must be absolute and normalized.
- Exact-match lookup succeeds only when the requested display name matches an enumerated file or directory name.
- Traversal attempts, rooted paths, nested paths, Windows device names, trailing dots/spaces, control characters, and separator variants fail validation.
- Existing reparse points and reparse-point ancestors are rejected before file reads, writes, deletes, and directory deletes.

### mmria-server Flows

- Backup download proxying returns the same public download filename without writing a request-provided filename to local disk.
- CVS cached PDF lookup reads only an enumerated exact-match file for the current export/cache root.
- STEVE MMRIA, STEVE PRAMS, and PDF Central read/delete actions return the expected file for valid queue entries and reject malicious filename values.
- Generated internal names remain independent from user-controlled or externally supplied filenames.

### mmria.services Flows

- Backup file listing and download enumerate from the configured backup root and do not combine route parameters into paths.
- Export queue writes and reads generated physical storage names while preserving `file_name` as the public download name.
- Export delete removes only the enumerated/generated artifact associated with the queue item.
- CSV/XLSX writers write to generated physical paths and preserve manifest/display metadata.
- Backup document output uses generated file names and a manifest for CouchDB document IDs and attachment names.
- Compression writes safe archive entries and includes manifest data needed to map internal names back to logical names.

### Static Guard Tests

- Guard tests scan the Scan3-targeted files and fail when direct filesystem path construction is reintroduced with tainted values.
- Guard tests cover route parameters, request bodies, CouchDB document fields, export queue fields, attachment names, external filenames, and service response filenames.
- Guard tests allow constant-only and generated-name-only path construction where the generated-name source is verified by unit tests.

## Assumptions

- This is a pre-refactor confirmation step, not the full implementation.
- The document lives under `docs/ai/local/` because it is local working guidance.
- Public routes, response shapes, and displayed download filenames remain unchanged.
- Internal physical names and artifact layout may change for new files if legacy lookup remains available for existing artifacts.
- Fortify reduction is the target success signal; false-positive documentation remains supporting evidence only.
