# Strongly Typed Case Generator Workflow

- Status: Active
- Scope: Workflow for updating metadata-driven case models and syncing generated output back into this repository.
- When to use: Read this before changing generated case-model files or adding metadata-backed properties to the strongly typed model.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Offline Mode Documentation](./offline_mode.md), [Historical Notes](./local/archive/)

## External dependency

This workflow depends on a sibling utility repository that may not exist in the current workspace.

- External dependency: `nccdphp-drh-mmria-utilities/strongly-typed-case`
- External dependency: `nccdphp-drh-mmria-utilities/mmria-case-generator`
- External dependency: `nccdphp-drh-mmria-utilities/mmria-ije-generator`
- External dependency: `nccdphp-drh-mmria-utilities/mmria-tools`

Do not assume those repos are present unless you verify them locally.

`mmria-tools` is now the shared library home for moved generator and test-tooling code. The first migration wave keeps the existing generator namespaces stable while moving the implementation boundary into the utilities repo.

## What this workflow controls

The strongly typed case generator produces case-model classes from metadata. The generated output is then copied into `mmria-server` versioned case-model folders.

The broader utilities-repo tooling boundary now includes:

- `strongly-typed-case` for metadata-driven C# model generation
- `mmria-case-generator` for synthetic case generation
- `mmria-ije-generator` for synthetic IJE file generation
- `mmria-tools` for shared generator and test-tooling code used by those utilities and the moved test project

Within this repo, the generated destination is under:

- `source-code/mmria/mmria-server/case-version/mmria/{version}/`

## Stable rules

- Update metadata first. Do not hand-edit generated classes as the primary change path.
- Regenerate after metadata changes instead of patching generated output manually.
- Keep the generator workflow and the server-side versioned output in sync.
- Treat generator changes and metadata changes as a coordinated change set.

## Typical workflow

1. Update the metadata definition for the property or shape change.
2. Run the strongly typed case generator in the external utility repo.
3. Copy the generated `.cs` output into the target `case-version/mmria/{version}` folder in this repo.
4. Build and test the affected server project.

## Example placeholder workflow

The exact local paths vary by machine, so prefer placeholders rather than hard-coded machine-specific local paths.

```powershell
cd <mmria-utilities-root>\strongly-typed-case
dotnet run -- <metadata-version-or-config>

Copy-Item .\output\*.cs `
  <mmria-root>\source-code\mmria\mmria-server\case-version\mmria\<version>\

cd <mmria-root>\source-code\mmria\mmria-server
dotnet build .\mmria-server.csproj
```

## When adding a new property

- Confirm the property belongs in metadata first.
- Regenerate the strongly typed output after the metadata change.
- Check downstream code that reads or writes the property.
- If offline mode, CDC population, or case generation depends on the field, review those feature docs as well.

## Common pitfalls

- Editing generated output first and forgetting to update metadata.
- Copying generator output into the wrong metadata version folder.
- Treating a utility-repo path as guaranteed to exist in every workspace.
- Forgetting to rebuild after copying generated files.

## Cross-feature reminders

- For offline-lock or session-related fields, also review [Offline Mode Documentation](./offline_mode.md).
- For test-data generation changes, verify whether the case generator or external enrichment docs need updates too.



