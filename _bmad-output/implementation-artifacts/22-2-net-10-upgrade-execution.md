# Story 22.2 — .NET 10 Upgrade Execution

**Epic:** 22 — .NET 10 Upgrade
**Story ID:** 22.2
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** 22.1 complete with no unresolved blockers
**Source requirements:** epics.md §Epic 22 Story 22.2

---

## ⚠️ Pre-condition

**This story must not be started until Story 22.1 is `done` and `docs/ai/dotnet10-compatibility-analysis.md` shows no unresolved blocker items.** Any package with no .NET 10 support path must be resolved before this story begins.

---

## User Story

As a developer,
I want all mmria projects running on .NET 10,
So that the codebase is on the current LTS release with continued Microsoft support and access to .NET 10 platform improvements.

---

## Acceptance Criteria

**AC-1 — .NET 10 SDK installed on dev machine**
Given the developer machine does not yet have the .NET 10 SDK installed
When the developer runs the upgrade
Then the .NET 10 SDK is installed via `winget install Microsoft.DotNet.SDK.10` (or the equivalent official installer) and `dotnet --list-sdks` confirms the `10.x` SDK is present alongside the existing 9.x SDK

**AC-2 — All project target frameworks updated**
Given all eleven in-scope `.csproj` files currently declare `<TargetFramework>net9.0</TargetFramework>` (or `<TargetFrameworks>net9.0</TargetFrameworks>` for mmria-server)
When the developer updates them
Then every in-scope `.csproj` declares `net10.0` and `dotnet build` succeeds with no new errors for each project

**AC-3 — Version-locked Microsoft packages updated**
Given the version-locked Microsoft packages:

| Package | Current | Action |
|---|---|---|
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 9.0.0 | Update to 10.0.x |
| `Microsoft.Extensions.Http` | 9.0.0 | Update to 10.0.x |
| `System.Text.Encoding.CodePages` | 9.0.0 | Update to 10.0.x (or confirm in-box) |
| `Serilog.Extensions.Logging` | 9.0.0 | Update to 10.0.x-aligned stable |

When the developer updates packages
Then each is updated to its .NET 10-aligned version and the projects restore without errors

**AC-4 — Additional packages updated per findings report**
Given the compatibility analysis report's per-package recommended versions (Story 22.1 output)
When remaining packages require version bumps (e.g., Akka, Quartz, NJsonSchema as identified in Story 22.1)
Then each is updated to the version specified in the report and the affected projects build and restore cleanly

**AC-5 — `mmria-server` Dockerfile updated**
Given `source-code/mmria/mmria-server/Dockerfile` build stage references:
```
FROM .../trusted-images/dotnet-90:9.0-<tag>@sha256:<digest> AS build
```
and the runtime stage references:
```
FROM .../trusted-images/dotnet-90-runtime:9.0-<tag>@sha256:<digest> AS runtime
```
and `-f net9.0` flags in `dotnet build` and `dotnet publish`

When the developer updates the Dockerfile
Then both `FROM` lines reference the .NET 10 trusted images (`dotnet-100` / `dotnet-100-runtime`) with the correct tag and digest from Story 22.1, and both `-f net9.0` flags are updated to `-f net10.0`

**AC-6 — `mmria.services` Dockerfile updated**
Given `nccdphp-drh-mmria-services/mmria.services/Dockerfile` contains the same `dotnet-90` / `dotnet-90-runtime` image references and `-f net9.0` flags
When the developer updates it
Then both `FROM` lines and both `-f` flags are updated identically to the server Dockerfile

**AC-7 — `.s2i/dockerfile` assessed**
Given `.s2i/dockerfile` currently references `dotnet-80` and is largely commented out
When the developer reviews it per Story 22.1 findings
Then either (a) it is updated to reference `dotnet-100` if the file is still used, or (b) a comment is added to the top of the file documenting that it is retired and not used in the active build pipeline

**AC-8 — Full build and test gate passes**
Given all changes are applied
When the developer runs:
- `dotnet build` on `mmria-server.csproj` (Release, `net10.0`)
- `dotnet build` on `mmria.services.csproj` (Release, `net10.0`)
- `dotnet build` on `mmria.common.csproj`
- `dotnet test` on `mmria-server.tests.csproj`

Then all builds succeed and all tests pass with no new failures (pre-existing failures, if any, are noted but do not block this story)

**AC-9 — `.vscode/tasks.json` checked for hardcoded framework flags**
Given the VS Code task definitions may reference `-f net9.0` in build arguments
When the developer searches task definitions
Then any hardcoded `-f net9.0` flags in `.vscode/tasks.json` are updated to `net10.0`

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/mmria-server.csproj` | `net9.0` → `net10.0`; update version-locked packages |
| `nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj` | `net9.0` → `net10.0`; update `Microsoft.Extensions.Http` |
| `nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` | `net9.0` → `net10.0` |
| `mmria-server.tests/mmria-server.tests.csproj` | `net9.0` → `net10.0` |
| `mmria-case-generator/mmria-case-generator.csproj` | `net9.0` → `net10.0` |
| `strongly-typed-case/strongcase.csproj` | `net9.0` → `net10.0` |
| `data-migration/migrate.csproj` | `net9.0` → `net10.0` |
| `Replication/replicate.csproj` | `net9.0` → `net10.0` |
| `mmria-ije-generator/mmria-ije-generator.csproj` | `net9.0` → `net10.0` |
| `mmria-tools/mmria-tools.csproj` | `net9.0` → `net10.0` |
| `mmria-tenant-database-counts/mmria-tenant-database-counts.csproj` | `net9.0` → `net10.0` |
| `source-code/mmria/mmria-server/Dockerfile` | `dotnet-90` → `dotnet-100`; `dotnet-90-runtime` → `dotnet-100-runtime`; `-f net9.0` → `-f net10.0` |
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` | Same as above |
| `.s2i/dockerfile` | Update or document as retired (per Story 22.1 recommendation) |
| `.vscode/tasks.json` | Check for any hardcoded `-f net9.0` flags |

---

## Sequencing

Must not start until Story 22.1 is complete and clean. The two stories are strictly sequential.
