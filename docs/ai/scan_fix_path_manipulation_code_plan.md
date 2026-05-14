# Eliminate Scan3 Path-Manipulation Findings

## Summary

- `mmria-server`: `80` Fortify SCA path findings, all reported at `ContainedPathHelper.cs`; WebInspect has no path-manipulation rows. The real flows are backup downloads, CVS PDF cache, STEVE/PDF downloads, and STEVE actor file creation.
- `mmria-services`: `48` Fortify path/base-path findings across backup controller, export queue download/delete, export writers, CSV/XLSX writing, backup document files, and compression.
- Goal is no longer “prove false positive”; it is to remove scanner-visible tainted filesystem usage. Preserve public routes, response shapes, and download names, but allow new internal artifact names/layouts to be generated and manifest-backed.

## Key Changes

- Add shared filesystem primitives under `mmria.common/SharedLibraries/Security/FileSystem`:
  - `TrustedDirectoryRoot` from configured absolute roots only.
  - `ExistingFileHandle` / `ExistingDirectoryHandle` created only by directory enumeration plus exact-name match.
  - `GeneratedArtifactName` created only from constants, UTC timestamp, counter/GUID, and allowlist labels.
  - No API that accepts `(basePath, userOrDbString)` and returns a path.
- Replace server-side temp file hops:
  - `backupManagerController` proxies service downloads directly to a `FileContentResult`/stream result; it no longer writes route-provided names to local disk.
  - CVS, STEVE, and PDF download/delete actions resolve requested files by exact match against enumerated handles, not by combining request strings into paths.
  - STEVE actor uses generated workspace, generated downloaded-file names, and a manifest for original STEVE filenames.
- Replace services-side path construction:
  - `backupController` file/subfolder endpoints use trusted-root enumeration lookup.
  - Export queue processing writes `storage_directory_name` and `storage_file_name` to `export_queue` docs while keeping `file_name` as the user-facing download name.
  - Exporters and `WriteCSV` write generated physical CSV/XLSX names plus `artifact-manifest.json`; zip entries use generated safe names, with display/original names in the manifest.
  - Backup document export uses generated file names and `backup-manifest.json` for CouchDB IDs and attachments.
  - Remove `CleanPath` from active export/delete paths.

## Interfaces And Compatibility

- Add nullable fields to `export_queue_item`: `storage_file_name`, `storage_directory_name`.
- Keep all existing HTTP routes/action signatures and primary JSON/download behavior.
- Download headers continue to use the intended display filename after header-safe normalization.
- Legacy files without storage fields remain accessible/deletable through enumeration-based exact-name fallback.
- New internal zip/export/backup contents may use generated filenames; manifests preserve original logical names.

## Test Plan

- Add unit tests for trusted root validation, traversal rejection, generated-name creation, enumeration lookup, reparse-point rejection, and zip-entry safety.
- Add server tests for backup proxy downloads, CVS cache manifest behavior, legacy CVS fallback, and STEVE/PDF read/delete lookup with malicious filenames returning not found.
- Add services tests for backup file/subfolder endpoints, export queue storage fields, legacy export fallback, export delete, generated CSV/XLSX writes, compression manifests, and backup document manifests.
- Add static guard tests banning `CleanPath.execute` and direct `Path.Combine` with request/queue/CouchDB/external filename fields in the scan3-targeted files.
- Verify with `dotnet build source-code/mmria/mmria-server/mmria-server.csproj`, `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`, and focused `mmria-server.tests` security tests.

## Assumptions

- Security acceptance requires Fortify findings to drop, not only be tagged false positive.
- Internal artifact naming/layout changes are acceptable for new files if UI, routes, download names, and legacy artifact access remain stable.
- No current restore workflow depends on backup filenames; manifests become the durable mapping for future restore tooling.
