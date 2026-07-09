# Story 12.1.1 — Fix Data Migration Project Reference

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.1.1
**Status:** done
**Date added:** 2026-07-08
**Blocker for:** Story 12.2

---

## Background

Story 12.1 refactored `Program.cs` and the appsettings configuration files. The build was not verified after the refactor. A pre-existing broken `ProjectReference` in `migrate.csproj` — pointing to a relative path that does not exist — causes all 401 compile errors. This is not a code logic issue; all referenced types and namespaces are present in the correct `mmria.common` library. The reference simply targets the wrong location.

---

## User Story

As a developer working on the data-migration tool,
When I run `dotnet build` on `migrate.csproj`,
I want the project to compile successfully,
So that I can develop and run Story 12.2 (Vitals Retrospective Type Correction Migration).

---

## Acceptance Criteria

**AC-1 — Build succeeds**
Given `migrate.csproj` references the correct absolute path to `mmria.common`
When `dotnet build c:\repos\nccdphp-drh-mmria-utilities\data-migration\migrate.csproj` is run
Then the build completes with 0 errors
And the `MSB9008` warning about a missing project is gone

**AC-2 — Project reference matches the Replication project pattern**
Given the corrected `migrate.csproj`
When a developer reads the `<ProjectReference>` element
Then it uses the absolute path `C:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-common\mmria.common\mmria.common.csproj`
— consistent with the pattern in `Replication/replicate.csproj`

**AC-3 — No source-code changes required**
Given the reference fix is the sole root cause of all build errors
When the reference is corrected
Then no changes to any `.cs` file are needed to achieve a clean build

---

## Root Cause Analysis

### Broken Reference (current state)

**`data-migration/migrate.csproj`:**
```xml
<ItemGroup>
  <ProjectReference Include="..\mmria.common\mmria.common.csproj" />
</ItemGroup>
```

Resolves to: `c:\repos\nccdphp-drh-mmria-utilities\mmria.common\mmria.common.csproj`
**Status: directory does not exist → MSB9008 warning → 401 cascading CS0234/CS0246 errors**

### Correct Reference (target state)

**`Replication/replicate.csproj` (working):**
```xml
<ItemGroup>
  <ProjectReference Include="C:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-common\mmria.common\mmria.common.csproj"/>
</ItemGroup>
```

### Namespace Verification

All namespaces used by migration source files are confirmed to exist in the correct `mmria.common`:

| Namespace Used | Exists in mmria.common | Location |
|---|---|---|
| `mmria.common.metadata` | ✅ | `nccdphp-drh-mmria-common/mmria.common/metadata/` |
| `mmria.common.couchdb` | ✅ | `nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs` |
| `mmria.common.model.couchdb` | ✅ | `nccdphp-drh-mmria-common/mmria.common/couchdb/*.cs` |
| `mmria.common.cvs` | ✅ | `nccdphp-drh-mmria-common/mmria.common/cvs/` |
| `Metadata_Node` (in `mmria.common.metadata`) | ✅ | `nccdphp-drh-mmria-common/mmria.common/metadata/Metadata_Node.cs` |

---

## Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-utilities/data-migration/migrate.csproj` | Replace the relative `ProjectReference` with the absolute path matching the Replication project |

### Exact Change

**Old:**
```xml
<ProjectReference Include="..\mmria.common\mmria.common.csproj" />
```

**New:**
```xml
<ProjectReference Include="C:\repos\nccdphp-drh-mmria\nccdphp-drh-mmria-common\mmria.common\mmria.common.csproj" />
```

---

## Dev Verification Steps

1. Apply the `migrate.csproj` change above
2. Run: `dotnet build c:\repos\nccdphp-drh-mmria-utilities\data-migration\migrate.csproj`
3. Confirm: `Build succeeded` with 0 errors and no `MSB9008` warning
4. Mark story `done` — Story 12.2 is now unblocked
