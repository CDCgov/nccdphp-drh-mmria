# .NET 10 Compatibility Analysis — MMRIA

**Story:** 22.1 — Compatibility Analysis and Risk Assessment
**Analyst:** Winston (Architect)
**Date:** 2026-07-15
**Status:** Complete — no unresolved package or image blockers (one image item requires EcPaaS team confirmation before Story 22.2 begins; see §3)

---

## Scope

All 11 `.csproj` files across both repos (all currently targeting `net9.0`), two production Dockerfiles referencing `dotnet-90`/`dotnet-90-runtime`, and the legacy `.s2i/dockerfile`.

| Project | Repo | Kind |
|---|---|---|
| `mmria-server` | nccdphp-drh-mmria | ASP.NET Core web server |
| `mmria.common` | nccdphp-drh-mmria | Class library |
| `mmria.services` | nccdphp-drh-mmria | ASP.NET Core worker service |
| `mmria-server.tests` | nccdphp-drh-mmria-utilities | NUnit test project |
| `mmria-case-generator` | nccdphp-drh-mmria-utilities | Console utility |
| `mmria-ije-generator` | nccdphp-drh-mmria-utilities | Console utility |
| `mmria-tools` | nccdphp-drh-mmria-utilities | Class library |
| `mmria-tenant-database-counts` | nccdphp-drh-mmria-utilities | Console utility |
| `data-migration` | nccdphp-drh-mmria-utilities | Console utility |
| `Replication` | nccdphp-drh-mmria-utilities | Console utility |
| `strongly-typed-case` | nccdphp-drh-mmria-utilities | Console utility |

---

## 1. Breaking-Change Audit

### 1.1 ASP.NET Core 10 Breaking Changes

| Change | Category | Impact on MMRIA | Severity |
|---|---|---|---|
| `IActionContextAccessor` and `ActionContextAccessor` are obsolete | Source incompatible | **Not used** — grep confirms no references in active code | None |
| `WebHostBuilder`, `IWebHost`, `WebHost` are obsolete | Source incompatible | **Not active** — only in `MMRIA_Window_Service.cs` which is explicitly excluded from the build via `DefaultItemExcludes` in `mmria-server.csproj`. Active code uses `WebApplication.CreateBuilder()`. | None |
| Cookie login redirects disabled for known API endpoints | Behavioral | API endpoints that previously triggered a cookie-based redirect may see changed behavior. Needs smoke-test on login flow. | Low |
| Razor runtime compilation is obsolete | Source incompatible | **Not used** — no `AddRazorRuntimeCompilation` calls found | None |
| `WithOpenApi` extension method deprecated | Source incompatible | **Not used** — no OpenAPI wiring found | None |
| `IPNetwork` and `ForwardedHeadersOptions.KnownNetworks` are obsolete | Source incompatible | Needs check if `ForwardedHeadersOptions` is configured. If not using it, no impact. | Low |
| `IncludeOpenAPIAnalyzers` and MVC API analyzers deprecated | Source incompatible | Not referenced in any csproj | None |
| `Microsoft.Extensions.ApiDescription.Client` package deprecated | Source incompatible | Not referenced in any csproj | None |

### 1.2 Core .NET 10 Breaking Changes

| Change | Category | Impact on MMRIA | Severity |
|---|---|---|---|
| `BackgroundService.ExecuteAsync` runs as a full `Task` | Behavioral | Akka.NET hosted services extend `BackgroundService`. The behavior change is that unhandled exceptions propagate differently. Akka.Hosting 1.5.x handles this pattern but warrants a smoke test of actor system startup. | Low |
| `ProviderAliasAttribute` moved to `Microsoft.Extensions.Logging.Abstractions` | Source incompatible | No direct use of `ProviderAliasAttribute` found. Indirect via Serilog packages — covered by upgrading `Serilog.Extensions.Logging` to 10.0.0. | Low |
| `System.Text.Json` checks for duplicate property names | Behavioral | MMRIA uses Newtonsoft.Json throughout; `System.Text.Json` is not used directly. No impact. | None |
| C# 14 overload resolution change with `Span<T>` parameters | Behavioral | Could affect method calls that are ambiguous between span and non-span overloads. Compiler will surface these as errors at compile time; treat as a build-time concern, not a hidden runtime risk. | Low |
| API obsoletions (general) | Source incompatible | `SYSLIB0014` is suppressed in `mmria-server.csproj`. Codebase grep confirms no direct `WebRequest`/`HttpWebRequest`/`WebClient` usage — the suppression is likely preemptive or from a transitive dependency. In .NET 10 these APIs remain available (deprecated, not removed). Suppression can be retained. | None |
| `Null values preserved in configuration` (Extensions) | Behavioral | A key under `IConfiguration` that was previously silently dropped may now be preserved as `null`. Verify that startup config loading does not break on explicit `null` values. | Low |

### 1.3 Docker / Container Breaking Changes

| Change | Impact |
|---|---|
| Default .NET images use Ubuntu (Containers) | The EcPaaS images are Red Hat UBI-based, not the Microsoft default images. No impact from this change. |

### 1.4 SDK / MSBuild Breaking Changes

| Change | Impact on MMRIA | Severity |
|---|---|---|
| `PackageReference` without a version raises an error | None — all package references in scope have explicit versions | None |
| `NU1510` raised for direct references pruned by NuGet | Could affect test project if transitive packages are incorrectly assumed to be direct. Treat as a build-time verification item. | Low |
| `PrunePackageReference` privatizes prunable references | Same as above — surfaced at build time, not hidden. | Low |

---

## 2. NuGet Package Compatibility Table

### Legend
- ✅ **Compatible** — current version targets a TFM compatible with net10.0 (`net8.0+` or `netstandard2.0`); upgrade optional
- ⚠️ **Upgrade recommended** — compatible today but version is misaligned with the .NET release train; upgrade before or during Story 22.2
- ❌ **Must update** — version will not work correctly on net10.0; blocks the upgrade
- 🚫 **Deprecated** — package is formally deprecated on NuGet; runtime-compatible but a migration is needed

### 2.1 mmria-server

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `Serilog` | 4.2.0 | 4.2.0 | netstandard2.0 | ✅ No action | — |
| `Serilog.Extensions.Logging` | 9.0.0 | **10.0.0** | net8.0+ | ⚠️ Align to release train | → `10.0.0` |
| `Serilog.Sinks.Console` | 6.0.0 | 6.0.0 | netstandard2.0 | ✅ No action | — |
| `Serilog.Sinks.File` | 6.0.0 | 6.0.0 | netstandard2.0 | ✅ No action | — |
| `Akka` | 1.5.52 | 1.5.70 | net6.0, netstandard2.0 | ✅ Compatible | Upgrade to 1.5.70 optional |
| `Akka.Quartz.Actor` | 1.5.13 | **1.5.59** | netstandard2.0 | ✅ Compatible | Upgrade to 1.5.59 optional |
| `Akka.DependencyInjection` | 1.5.52 | 1.5.70 | net6.0 | ✅ Compatible | Upgrade optional |
| `Akka.Cluster` | 1.5.52 | 1.5.70 | net6.0 | ✅ Compatible | Upgrade optional |
| `Akka.Hosting` | 1.5.51.1 | **1.5.70** | net6.0 | ✅ Compatible | Upgrade optional |
| `Akka.Management` | 1.5.50 | 1.5.70 | net6.0 | ✅ Compatible | Upgrade optional |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 9.0.0 | **10.0.10** | **net10.0 only** | ❌ **Must update** | → `10.0.10` |
| `Microsoft.CodeAnalysis.CSharp` | 4.12.0 | **5.6.0** | net8.0, netstandard2.0 | ✅ Compatible | Upgrade optional |
| `Quartz` | 3.13.1 | **3.18.2** | net8.0, netstandard2.0 | ✅ Compatible | Upgrade optional |
| `SharpZipLib` | 1.4.2 | 1.4.2 | netstandard2.0 | ✅ No action | — |
| `System.Text.Encoding.CodePages` | 9.0.0 | **10.0.10** | net8.0+ | ⚠️ Align to release train | → `10.0.10` |
| `NJsonSchema` | 11.0.2 | **11.6.1** | net8.0, netstandard2.0 | ✅ Compatible | Upgrade optional |
| `NJsonSchema.CodeGeneration.CSharp` | 11.0.2 | **11.6.1** | net8.0 | ✅ Compatible | Upgrade optional |
| `FastExcel` | 3.0.13 | 3.0.13 | netstandard2.0 | ✅ No action | — |
| `TinyCsvParser` | 2.7.1 | 2.7.1 | netstandard2.0 | ✅ No action | — |

> **Note:** `<TargetFrameworks>net9.0</TargetFrameworks>` in `mmria-server.csproj` uses the plural element with a single value. This is syntactically valid but should be normalized to `<TargetFramework>net10.0</TargetFramework>` (singular) in Story 22.2 since there is no multi-targeting.

### 2.2 mmria.common

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `Microsoft.Extensions.Http` | 9.0.0 | **10.0.10** | net8.0, netstandard2.0 | ⚠️ Align to release train | → `10.0.10` |
| `Newtonsoft.Json` | 13.0.3 | 13.0.3 | netstandard2.0 | ✅ No action | — |

### 2.3 mmria.services

> ⚠️ **This project carries two deprecated Akka DI packages** (`Akka.DI.Core` and `Akka.DI.Extensions.DependencyInjection`) that are not present in `mmria-server`. Both packages are `netstandard2.0` and will load under .NET 10, but `Akka.DI.Core` is formally deprecated by the Akka.NET team in favour of `Akka.DependencyInjection`. This creates risk of silent incompatibilities as the Akka.NET API evolves past 1.4.x. **Migrating to `Akka.DependencyInjection` is deferred to a separate story** — it is not a hard blocker for the TFM upgrade, but the risk is Medium.

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `Akka` | 1.5.52 | 1.5.70 | net6.0, netstandard2.0 | ✅ Compatible | Upgrade optional |
| `Akka.Quartz.Actor` | 1.5.13 | 1.5.59 | netstandard2.0 | ✅ Compatible | Upgrade optional |
| `Akka.DI.Core` | 1.4.51 | 1.4.51 (final) | netstandard2.0 | 🚫 **Deprecated** — no new releases | Migrate to `Akka.DependencyInjection` (separate story; not a hard blocker for TFM upgrade) |
| `Akka.DI.Extensions.DependencyInjection` | 1.4.22 | 1.4.22 (final) | net5.0, netstandard2.0 | 🚫 **Unmaintained** (last update Jan 2021) | Migrate alongside `Akka.DI.Core` |
| `FastExcel` | 3.0.13 | 3.0.13 | netstandard2.0 | ✅ No action | — |
| `SharpZipLib` | 1.4.2 | 1.4.2 | netstandard2.0 | ✅ No action | — |

### 2.4 mmria-server.tests

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `NUnit` | 4.1.0 | **4.6.1** | net6.0, net4.6.2 | ✅ Compatible | Upgrade to 4.6.1 optional |
| `NUnit3TestAdapter` | 4.5.0 | 4.6.x | net8.0 | ✅ Compatible | Upgrade optional |
| `Microsoft.NET.Test.Sdk` | 17.10.0 | **18.8.1** | net8.0, netstandard2.0 | ✅ Compatible | Upgrade to 18.8.1 optional |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align to release train | → `10.0.10` |

### 2.5 mmria-tools

> ⚠️ `Microsoft.AspNetCore.Mvc.NewtonsoftJson` version 3.1.1 targets `netcoreapp3.1`. While `netstandard2.0` compatibility may allow it to load, the internal types were compiled against ASP.NET Core 3.1 APIs that no longer match the .NET 10 runtime. This is the highest-risk outdated package across all projects.

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `TinyCsvParser` | 2.5.1 | 2.7.1 | netstandard2.0 | ✅ Compatible | Upgrade to 2.7.1 optional (aligns with version in `mmria-server`) |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | **3.1.1** | **10.0.10** | netcoreapp3.1 | ❌ **Must update** | → `10.0.10` (or remove if only using `Newtonsoft.Json` directly; evaluate during Story 22.2) |
| `Microsoft.Extensions.Configuration` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.Configuration.Binder` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.Http` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |

### 2.6 data-migration

> ⚠️ This project has an accumulation of mismatched older packages. `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 7.0.0 targets `net7.0` and shares the same type-incompatibility risk as the 3.1.1 version in mmria-tools. `Akka` 1.4.46 is two major release trains behind and should be upgraded to 1.5.x. `System.Data.OleDb` 6.0.0 targets older frameworks — needs validation.

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `CsvHelper` | 27.1.1 | 33.x | netstandard2.0 | ✅ Compatible | No change required unless API changes affect usage |
| `LumenWorksCsvReader` | 4.0.0 | 4.0.0 | net4.5, netstandard2.0 | ✅ Compatible (netstandard2.0) | No action required |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | **7.0.0** | **10.0.10** | net7.0 | ❌ **Must update** | → `10.0.10` (or remove if only using `Newtonsoft.Json` directly; evaluate during Story 22.2) |
| `Akka` | **1.4.46** | 1.5.70 | netstandard2.0 | ⚠️ Old release train | → `1.5.70` (upgrade within Story 22.2 or as separate cleanup) |
| `System.Data.OleDb` | **6.0.0** | 8.0.0 | net6.0, net4.6.2 | ⚠️ Old; Linux-incompatible | Upgrade to 8.0.0; note: OleDb is Windows-only and will throw on Linux/container builds |
| `Microsoft.Extensions.Configuration` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.Configuration.Binder` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |
| `Microsoft.Extensions.Http` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |

### 2.7 mmria-case-generator

| Package | Current | Latest Available | TFM Declared | Status | Recommended Action |
|---|---|---|---|---|---|
| `Bogus` | 35.6.1 | 35.6.1 | netstandard2.0 | ✅ No action | — |
| `Microsoft.Extensions.Http` | 9.0.0 | 10.0.10 | net8.0+ | ⚠️ Align | → `10.0.10` |

### 2.8 Replication, mmria-ije-generator, strongly-typed-case, mmria-tenant-database-counts

These projects carry no explicit third-party `PackageReference` entries beyond project references to `mmria.common` and/or `mmria-tools`. Their upgrade impact is limited to the TFM change and picking up the upgraded dependencies transitively.

---

## 3. Docker Image Availability (EcPaaS)

### 3.1 Current State

Both Dockerfiles (`mmria-server/Dockerfile` and `mmria.services/Dockerfile`) use EcPaaS trusted images:

| Image | Current Tag/Digest Used |
|---|---|
| Build: `dotnet-90` | `9.0-1784003632@sha256:fbff3be85e...` (mmria-server) |
| Runtime: `dotnet-90-runtime` | `9.0-1783975593@sha256:1b7266ecedff...` (mmria-server) |
| Build: `dotnet-90` | `9.0-1779943927@sha256:94f9d689...` (mmria.services) |
| Runtime: `dotnet-90-runtime` | `9.0-1779914985@sha256:5802a377...` (mmria.services) |

### 3.2 Required Images for .NET 10

| Image Required | Known Available? |
|---|---|
| `dotnet-100` (SDK/build image) | **Unknown — must confirm with EcPaaS platform team** |
| `dotnet-100-runtime` (runtime image) | **Unknown — must confirm with EcPaaS platform team** |

### 3.3 Status and Gate Condition

> ⚠️ **Gate condition for Story 22.2:** The `dotnet-100` and `dotnet-100-runtime` images **must exist in the EcPaaS trusted-image registry** before the Dockerfile changes in Story 22.2 are executed. If the images are absent, the CI/CD build will fail at the `FROM` line.

**Recommended path forward (before Story 22.2 begins):**
1. Contact the EcPaaS platform team and ask: "Are `dotnet-100` and `dotnet-100-runtime` UBI images available in the trusted-image registry?"
2. If available: obtain the tag/digest format (e.g., `10.0-XXXXXXXXXX@sha256:...`) for use in Story 22.2.
3. If not yet available: obtain an estimated availability date. As an interim, `mcr.microsoft.com/dotnet/aspnet:10.0` can be used with a security waiver for dev/test only; production deployment must wait for the EcPaaS trusted image.

**This is a potential blocker.** Story 22.2 cannot complete the Dockerfile changes until these images are confirmed available in the registry.

### 3.4 Legacy `.s2i/dockerfile` Assessment

The `.s2i/dockerfile` still references `dotnet-80` which is two major versions behind. It has been superseded by the Dockerfiles above and is not used in active CI/CD pipelines. **Recommendation: retire this file in Story 22.2.** Delete it or replace with a comment-only file noting it is decommissioned.

---

## 4. Suppressed-Warning Review

`mmria-server.csproj` suppresses the following compiler warnings via `<NoWarn>`:

| Warning | Description | .NET 10 Behavior | Recommendation |
|---|---|---|---|
| `SYSLIB0014` | `WebRequest`/`HttpWebRequest`/`WebClient` obsolescence | Remains a warning in .NET 10; does **not** escalate to error. Codebase grep confirms no direct usage of these APIs — suppression is preemptive or from a transitive dependency. | **Retain suppression.** |
| `CS8632` | Nullable annotation (#nullable not enabled) used in non-nullable context | Remains a warning; does not escalate in .NET 10. | Retain suppression. |
| `CS0414` | Field assigned but never used | Remains a warning. | Retain suppression. |
| `CS0649` | Field never assigned | Remains a warning. | Retain suppression. |
| `CS0169` | Field never used | Remains a warning. | Retain suppression. |
| `CS0219` | Variable assigned but never read | Remains a warning. | Retain suppression. |
| `CS0168` | Variable declared but never used | Remains a warning. | Retain suppression. |

**No suppressed warning escalates to a compilation error in .NET 10.**

---

## 5. Test Suite Review

**Project:** `mmria-server.tests`

| Concern | Assessment |
|---|---|
| **NUnit 4.1.0** | NUnit 4.x is fully compatible with .NET 10 (targets `net6.0+`). No API breaking changes between 4.1 and 4.6.1 that affect this test suite. Upgrade to 4.6.1 is safe and optional. |
| **NUnit3TestAdapter 4.5.0** | Compatible with .NET 10. Upgrade to latest optional. |
| **Microsoft.NET.Test.Sdk 17.10.0** | Compatible with .NET 10 (targets `net8.0+`). Upgrade to 18.8.1 recommended to use the latest test runner features. |
| **Test project references** | The test project references `mmria-server`, `mmria.common`, `mmria.services`, and `mmria-tools` via `<ProjectReference>`. All will be updated together — no isolated compatibility concern. |
| **`mmria-server.tests.runsettings`** | No framework-specific settings expected to change. Verify TFM in runsettings file if it contains an explicit `TargetFrameworkVersion` element. |
| **NUnit 4 vs NUnit 3 migration notes** | If any test uses `Assert.IsTrue`, `Assert.AreEqual`, or other classic NUnit 3 assertion styles, be aware NUnit 4 introduced async-awareness changes. NUnit 4.1.0 was already installed so this gap was already crossed — no new NUnit 4 migration issues apply from upgrading to 4.6.1. |

---

## 6. Story 22.2 Task Checklist

The following is the derived remediation checklist for Story 22.2 execution.

### 6.1 Pre-Conditions (Must Complete Before Story 22.2 Begins)

- [ ] **EcPaaS image confirmation**: Confirm with EcPaaS platform team that `dotnet-100` and `dotnet-100-runtime` trusted images exist in the registry and obtain their tag/digest strings.

### 6.2 TFM Changes (All 11 Projects)

- [ ] `mmria-server.csproj`: `<TargetFrameworks>net9.0</TargetFrameworks>` → `<TargetFramework>net10.0</TargetFramework>` (also normalize plural → singular)
- [ ] `mmria.common.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `mmria.services.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `mmria-server.tests.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `mmria-case-generator.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `mmria-ije-generator.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `mmria-tools.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `mmria-tenant-database-counts.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `migrate.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `replicate.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`
- [ ] `strongcase.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`

### 6.3 Must-Update Packages (Hard Requirement — Will Fail Without These)

- [ ] **mmria-server**: `Microsoft.AspNetCore.Mvc.NewtonsoftJson` `9.0.0` → `10.0.10`
- [ ] **mmria-server**: `Serilog.Extensions.Logging` `9.0.0` → `10.0.0`
- [ ] **mmria-server**: `System.Text.Encoding.CodePages` `9.0.0` → `10.0.10`
- [ ] **mmria-tools**: `Microsoft.AspNetCore.Mvc.NewtonsoftJson` `3.1.1` → `10.0.10` (or remove the reference if it is only pulling in `Newtonsoft.Json` — evaluate at the call sites during Story 22.2)
- [ ] **data-migration**: `Microsoft.AspNetCore.Mvc.NewtonsoftJson` `7.0.0` → `10.0.10` (or remove — same evaluation as above)

### 6.4 Recommended Upgrades (Align with .NET 10 Release Train)

- [ ] **mmria.common**: `Microsoft.Extensions.Http` `9.0.0` → `10.0.10`
- [ ] **mmria-tools**: all `Microsoft.Extensions.*` `9.0.0` → `10.0.10`
- [ ] **data-migration**: all `Microsoft.Extensions.*` `9.0.0` → `10.0.10`
- [ ] **data-migration**: `Akka` `1.4.46` → `1.5.70` (aligns with mmria-server's Akka version)
- [ ] **data-migration**: `System.Data.OleDb` `6.0.0` → `8.0.0` (note: Windows-only, confirm not invoked in Linux container builds)
- [ ] **mmria-case-generator**: `Microsoft.Extensions.Http` `9.0.0` → `10.0.10`
- [ ] **mmria-server.tests**: `Microsoft.Extensions.Configuration.Json` `9.0.0` → `10.0.10`
- [ ] **mmria-server.tests**: `Microsoft.NET.Test.Sdk` `17.10.0` → `18.8.1`

### 6.5 Dockerfile Changes

- [ ] **mmria-server/Dockerfile**: Replace `dotnet-90` build image reference with `dotnet-100` tag/digest (obtained in §6.1)
- [ ] **mmria-server/Dockerfile**: Replace `dotnet-90-runtime` runtime image reference with `dotnet-100-runtime` tag/digest
- [ ] **mmria-server/Dockerfile**: Update explicit `-f net9.0` flags in `dotnet build` and `dotnet publish` to `-f net10.0`
- [ ] **mmria.services/Dockerfile**: Replace `dotnet-90` build image reference with `dotnet-100` tag/digest
- [ ] **mmria.services/Dockerfile**: Replace `dotnet-90-runtime` runtime image reference with `dotnet-100-runtime` tag/digest
- [ ] **mmria.services/Dockerfile**: Update explicit `-f net9.0` flags to `-f net10.0`
- [ ] **`.s2i/dockerfile`**: **Retire this file** (it references the long-EOL `dotnet-80` image and is superseded by the Dockerfiles above)

### 6.6 Behavioral Change Smoke Tests (Post-Upgrade Validation)

- [ ] Verify actor system startup and Akka.NET hosted service lifecycle (covers `BackgroundService` behavior change — §1.2)
- [ ] Run `mmria-server.tests` test suite against net10.0 build to confirm no NUnit 4 assertion changes surfaced
- [ ] Smoke-test cookie-based login flow (covers ASP.NET Core 10 cookie redirect behavioral change — §1.1)
- [ ] Verify server startup configuration loading does not fail on any newly-preserved `null` IConfiguration values (covers `Null values preserved in configuration` — §1.2)
- [ ] Confirm `ForwardedHeaders` middleware (if configured) still routes correctly

### 6.7 Deferred Items (Out of Scope for Story 22.2)

- [ ] **mmria.services**: Migrate `Akka.DI.Core` + `Akka.DI.Extensions.DependencyInjection` to `Akka.DependencyInjection` — this is a medium-risk deprecated package issue but does not block the TFM upgrade. Schedule as a separate cleanup story.
- [ ] **mmria-tools**: Evaluate whether `Microsoft.AspNetCore.Mvc.NewtonsoftJson` is actually needed (console app), and if so, whether `Newtonsoft.Json` alone suffices.
- [ ] **mmria-server**: Consider upgrading the full Akka.NET suite (Akka, Akka.Hosting, Akka.Cluster, Akka.Management, Akka.Quartz.Actor) from 1.5.52 to 1.5.70 for bug fixes, though not strictly required for .NET 10 compatibility.

---

## Appendix A — Full Package Inventory

| Package | Version | Project(s) | TFM Compatibility | Must Update? |
|---|---|---|---|---|
| `Akka` | 1.5.52 | mmria-server, mmria.services | net6.0+ ✅ | No |
| `Akka` | 1.4.46 | data-migration | netstandard2.0 ✅ | Recommended upgrade to 1.5.70 |
| `Akka.Cluster` | 1.5.52 | mmria-server | net6.0+ ✅ | No |
| `Akka.DependencyInjection` | 1.5.52 | mmria-server | net6.0+ ✅ | No |
| `Akka.DI.Core` | 1.4.51 | mmria.services | netstandard2.0 ✅ (deprecated 🚫) | No (runtime ok; migrate separately) |
| `Akka.DI.Extensions.DependencyInjection` | 1.4.22 | mmria.services | net5.0, netstandard2.0 ✅ (unmaintained 🚫) | No (runtime ok; migrate separately) |
| `Akka.Hosting` | 1.5.51.1 | mmria-server | net6.0+ ✅ | No |
| `Akka.Management` | 1.5.50 | mmria-server | net6.0+ ✅ | No |
| `Akka.Quartz.Actor` | 1.5.13 | mmria-server, mmria.services | netstandard2.0 ✅ | No |
| `Bogus` | 35.6.1 | mmria-case-generator | netstandard2.0 ✅ | No |
| `CsvHelper` | 27.1.1 | data-migration | netstandard2.0 ✅ | No |
| `FastExcel` | 3.0.13 | mmria-server, mmria.services | netstandard2.0 ✅ | No |
| `LumenWorksCsvReader` | 4.0.0 | data-migration | netstandard2.0 ✅ | No |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 9.0.0 | mmria-server | net9.0 ❌ | **Yes → 10.0.10** |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 7.0.0 | data-migration | net7.0 ❌ | **Yes → 10.0.10** |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 3.1.1 | mmria-tools | netcoreapp3.1 ❌ | **Yes → 10.0.10** |
| `Microsoft.CodeAnalysis.CSharp` | 4.12.0 | mmria-server | net8.0+ ✅ | No |
| `Microsoft.Extensions.Configuration` | 9.0.0 | mmria-tools, data-migration | net8.0+ ✅ | Recommended upgrade |
| `Microsoft.Extensions.Configuration.Binder` | 9.0.0 | mmria-tools, data-migration | net8.0+ ✅ | Recommended upgrade |
| `Microsoft.Extensions.Configuration.Json` | 9.0.0 | mmria-tools, data-migration, mmria-server.tests | net8.0+ ✅ | Recommended upgrade |
| `Microsoft.Extensions.DependencyInjection` | 9.0.0 | mmria-tools, data-migration | net8.0+ ✅ | Recommended upgrade |
| `Microsoft.Extensions.Http` | 9.0.0 | mmria.common, mmria-tools, mmria-case-generator | net8.0+ ✅ | Recommended upgrade → 10.0.10 |
| `Microsoft.NET.Test.Sdk` | 17.10.0 | mmria-server.tests | net8.0+ ✅ | Recommended upgrade → 18.8.1 |
| `Newtonsoft.Json` | 13.0.3 | mmria.common | netstandard2.0 ✅ | No |
| `NJsonSchema` | 11.0.2 | mmria-server | net8.0+ ✅ | No |
| `NJsonSchema.CodeGeneration.CSharp` | 11.0.2 | mmria-server | net8.0+ ✅ | No |
| `NUnit` | 4.1.0 | mmria-server.tests | net6.0+ ✅ | No |
| `NUnit3TestAdapter` | 4.5.0 | mmria-server.tests | net8.0+ ✅ | No |
| `Quartz` | 3.13.1 | mmria-server | net8.0+ ✅ | No |
| `Serilog` | 4.2.0 | mmria-server | netstandard2.0 ✅ | No |
| `Serilog.Extensions.Logging` | 9.0.0 | mmria-server | net8.0+ ✅ | **Yes → 10.0.0** (alignment) |
| `Serilog.Sinks.Console` | 6.0.0 | mmria-server | netstandard2.0 ✅ | No |
| `Serilog.Sinks.File` | 6.0.0 | mmria-server | netstandard2.0 ✅ | No |
| `SharpZipLib` | 1.4.2 | mmria-server, mmria.services | netstandard2.0 ✅ | No |
| `System.Data.OleDb` | 6.0.0 | data-migration | net6.0 ✅ | Recommended upgrade → 8.0.0 |
| `System.Text.Encoding.CodePages` | 9.0.0 | mmria-server | net8.0+ ✅ | **Yes → 10.0.10** (alignment) |
| `TinyCsvParser` | 2.5.1 | mmria-tools | netstandard2.0 ✅ | No |
| `TinyCsvParser` | 2.7.1 | mmria-server | netstandard2.0 ✅ | No |
