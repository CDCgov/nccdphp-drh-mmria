# Story 22.1 — .NET 10 Compatibility Analysis and Risk Assessment

**Epic:** 22 — .NET 10 Upgrade
**Story ID:** 22.1
**Status:** ready-for-dev
**Date added:** 2026-07-15
**Depends on:** none — discovery only
**Source requirements:** epics.md §Epic 22 Story 22.1

---

## User Story

As a developer,
I want a documented analysis of all compatibility risks before upgrading to .NET 10,
So that the upgrade execution story has a clear, evidence-based remediation plan and no surprises block CI/CD.

---

## Acceptance Criteria

**AC-1 — Breaking-change audit**
Given the Microsoft .NET 10 breaking-changes documentation
When the developer reviews it against the mmria codebase
Then a written findings report is produced listing every breaking change that applies (or is suspected to apply) to this codebase, its severity (High / Medium / Low / None), and the affected file(s)

**AC-2 — NuGet package compatibility table**
Given the key third-party NuGet packages used across all in-scope projects:

| Package | Current Version | Risk Notes |
|---|---|---|
| `Akka` / `Akka.Hosting` / `Akka.Cluster` / `Akka.DependencyInjection` | 1.5.52 | Check NuGet for .NET 10 TFM support |
| `Akka.Quartz.Actor` | 1.5.13 | Transitively depends on Quartz 3.x; verify compatibility |
| `Akka.DI.Core` / `Akka.DI.Extensions.DependencyInjection` | 1.4.51 / 1.4.22 | Older release train; may not declare `net10.0` support |
| `Quartz` | 3.13.1 | Check for .NET 10 support |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 9.0.0 | Must be updated to 10.0.x |
| `Microsoft.Extensions.Http` | 9.0.0 | Must be updated to 10.0.x |
| `Serilog.Extensions.Logging` | 9.0.0 | Check for 10.0.x release |
| `System.Text.Encoding.CodePages` | 9.0.0 | Likely in-box for .NET 10; confirm |
| `Microsoft.CodeAnalysis.CSharp` | 4.12.0 | Verify .NET 10 compiler support |
| `NJsonSchema` / `NJsonSchema.CodeGeneration.CSharp` | 11.0.2 | Check for compatibility |
| `FastExcel` | 3.0.13 | Low risk (no framework coupling) |
| `SharpZipLib` | 1.4.2 | Low risk |
| `TinyCsvParser` | 2.7.1 | Low risk |
| `Newtonsoft.Json` | 13.0.3 | Low risk (framework-agnostic) |

When the developer checks each package on NuGet.org for .NET 10 TFM listings, open issues, and release notes
Then the findings report records the latest compatible version for each package (or "no upgrade needed" if the current version is compatible) and flags any packages with no .NET 10 support path as blockers

**AC-3 — EcPaaS trusted-image availability**
Given the EcPaaS trusted-image registry currently has `dotnet-90` and `dotnet-90-runtime` images
When the developer contacts the EcPaaS platform team or inspects the registry
Then the findings report records whether `dotnet-100` and `dotnet-100-runtime` images exist in the registry, their tag/digest format, and (if absent) the estimated availability timeline and any interim workaround (e.g., use `mcr.microsoft.com/dotnet/aspnet:10.0` with a waiver)

**AC-4 — Suppressed-warning review**
Given the suppressed compiler warnings in `mmria-server.csproj` (`SYSLIB0014`, `CS8632`, `CS0414`, `CS0649`, `CS0169`, `CS0219`, `CS0168`)
When the developer reviews them against .NET 10 release notes
Then the report notes whether any suppressed warning escalates to an error in .NET 10 and recommends the remediation action (fix the call site or retain the suppression)

**AC-5 — Test suite review**
Given the test suite in `mmria-server.tests`
When the developer reviews the test project's dependencies and test patterns
Then the report notes any test-framework or assertion-library changes needed for .NET 10

**AC-6 — Findings report committed**
Given the full analysis is complete
When the developer commits the output
Then a markdown findings report exists at `docs/ai/dotnet10-compatibility-analysis.md` covering:
1. Breaking-change audit results
2. Per-package compatibility status table with recommended versions
3. Docker image availability status and path forward
4. Suppressed-warning review
5. Recommended Story 22.2 task checklist derived from the above findings

---

## Dev Notes — Projects in Scope

**nccdphp-drh-mmria repo:**
- `source-code/mmria/mmria-server/mmria-server.csproj` — currently `net9.0`
- `nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj` — currently `net9.0`
- `nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj` — currently `net9.0`

**nccdphp-drh-mmria-utilities repo:**
- `mmria-server.tests/mmria-server.tests.csproj`
- `mmria-case-generator/mmria-case-generator.csproj`
- `strongly-typed-case/strongcase.csproj`
- `data-migration/migrate.csproj`
- `Replication/replicate.csproj`
- `mmria-ije-generator/mmria-ije-generator.csproj`
- `mmria-tools/mmria-tools.csproj`
- `mmria-tenant-database-counts/mmria-tenant-database-counts.csproj`

**Dockerfiles in scope:**
- `source-code/mmria/mmria-server/Dockerfile` — build: `dotnet-90`, runtime: `dotnet-90-runtime`
- `nccdphp-drh-mmria-services/mmria.services/Dockerfile` — same images
- `.s2i/dockerfile` — legacy, currently references `dotnet-80`; assess whether to update or retire

---

## Sequencing

Discovery only. Must be complete with no unresolved blockers before Story 22.2 begins. The two stories must not run in parallel.
