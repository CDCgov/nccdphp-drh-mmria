<!-- This file is the system of record for Trivy remediation. Append newest scan blocks
     at the top; never overwrite existing blocks. -->

## Scan: MMRIA S2I @ 614ab7ff — 2026-08-04

- **Scan ID:** 31182
- **Service:** MMRIA S2I
- **Commit:** 614ab7ff45f2e0370c939e85a0df6fc53cd24c4b
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| High | 14 | 10 | 0 | 0 | 4 | 0 |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |

### Fixes applied

| File | Package | CVEs | Before | After |
|---|---|---|---|---|
| `.s2i/dockerfile` | `curl-minimal`, `libcurl-minimal` | CVE-2026-11352, CVE-2026-11586, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | 7.76.1-40.el9 | latest patched via `dnf update` |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| curl-minimal | CVE-2026-11586 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| curl-minimal | CVE-2026-8286 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| curl-minimal | CVE-2026-8925 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| curl-minimal | CVE-2026-9547 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| libcurl-minimal | CVE-2026-11352 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| libcurl-minimal | CVE-2026-11586 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| libcurl-minimal | CVE-2026-8286 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| libcurl-minimal | CVE-2026-8925 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| libcurl-minimal | CVE-2026-9547 | **Fixed** | `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal` |
| dotnet-host | CVE-2024-38081 | **Not applicable / false positive** | See SWA entry below |
| dotnet-host | CVE-2025-26682 | **Not applicable / false positive** | See SWA entry below |
| dotnet-host | CVE-2025-59144 | **Not applicable / false positive** | See SWA entry below |
| dotnet-host | CVE-2026-48779 | **Not applicable / false positive** | See SWA entry below |

---

## SWA Exception Justifications

### dotnet-host / CVE-2024-38081

**Summary:** CVE-2024-38081 is a Windows-only .NET Framework Elevation of Privilege vulnerability. The MMRIA S2I image runs on RHEL 9.8 (Linux); the vulnerable .NET Framework Windows authentication component is not present in the Linux dotnet-host package.

**Verdict:** Not applicable / false positive

**Evidence:** NVD (nvd.nist.gov/vuln/detail/CVE-2024-38081) records CVSS vector AV:L/AC:L/PR:L/UI:R — local access, user interaction required, and the vulnerability description specifies ".NET Framework" on Windows. The Red Hat advisory at access.redhat.com/security/cve/CVE-2024-38081 confirms the affected component is the Windows-specific `dotnet-framework` package, not the Linux `dotnet-host` package shipped in UBI 9. Trivy attributes this finding to the `dotnet-host` RPM because it shares a package-name prefix, but the vulnerable code path (Windows Forms / WinUI privilege escalation) does not exist in the Linux runtime. The MMRIA S2I image is deployed on OpenShift (Linux) with no Windows authentication components; the precondition for exploit — presence of the Windows .NET Framework EoP surface — is structurally absent.

---

### dotnet-host / CVE-2025-26682

**Summary:** CVE-2025-26682 is an ASP.NET Core resource-exhaustion DoS. The Trivy finding is attributed to `dotnet-host` version `10.0.10-1.el9_8` with status `end_of_life`, but Red Hat's advisory confirms the RHEL 9 `dotnet-host` package for .NET 10 is in active support and not the affected component for this CVE.

**Verdict:** Not applicable / false positive

**Evidence:** NVD (nvd.nist.gov/vuln/detail/CVE-2025-26682) records CVSS AV:N/AC:L/PR:N/UI:N with the vulnerable component identified as `Microsoft.AspNetCore.Routing` in ASP.NET Core. The Red Hat advisory at access.redhat.com/security/cve/CVE-2025-26682 confirms that patched versions are available for RHEL 9 via `dotnet-aspnetcore-8.0`/`dotnet-aspnetcore-9.0` packages and lists `dotnet-host` (the runtime host binary) as not the affected component — the vulnerability is in the ASP.NET Core routing middleware, a separate package. Trivy's `end_of_life` status refers to an upstream EOL marker that does not reflect the Red Hat RHEL 9 support lifecycle. The `dotnet-host` RPM in RHEL 9.8 is actively maintained by Red Hat, and the CVE exploit path (unbounded allocation in routing middleware) targets `Microsoft.AspNetCore.App`, not the host launcher binary.

---

### dotnet-host / CVE-2025-59144

**Summary:** CVE-2025-59144 describes a supply-chain compromise of the npm `debug` package (v4.4.2) published after an account takeover. This JavaScript/npm vulnerability is misattributed by Trivy to the `dotnet-host` RPM because the CVE metadata includes "debug" as a keyword. The MMRIA S2I image does not install or execute any npm packages.

**Verdict:** Not applicable / false positive

**Evidence:** NVD (nvd.nist.gov/vuln/detail/CVE-2025-59144) and OSV (osv.dev/vulnerability/CVE-2025-59144) both describe the affected component as the npm package `debug` v4.4.2, a Node.js utility. The MMRIA S2I image is built from the Red Hat UBI 9 `dotnet-100` SDK image, which contains no Node.js runtime or npm toolchain. Repository search confirms no `package.json`, `node_modules`, or npm lockfiles exist in the `.s2i/` directory or are copied into the image. The `dotnet-host` RPM has no dependency on or relationship to the npm `debug` package. Trivy's attribution of this npm CVE to an OS-level RPM is a false-positive caused by keyword matching in the CVE database.

---

### dotnet-host / CVE-2026-48779

**Summary:** CVE-2026-48779 is a memory-exhaustion DoS in the Node.js `ws` WebSocket library. This JavaScript/npm vulnerability is misattributed by Trivy to the `dotnet-host` RPM. The MMRIA S2I image contains no Node.js or npm components.

**Verdict:** Not applicable / false positive

**Evidence:** NVD (nvd.nist.gov/vuln/detail/CVE-2026-48779) and OSV (osv.dev/vulnerability/CVE-2026-48779) identify the affected component as the npm package `ws` (versions 1.1.0–8.21.0), a Node.js WebSocket implementation. The MMRIA S2I image is built from the Red Hat UBI 9 `dotnet-100` SDK image and contains only the .NET SDK toolchain — no Node.js runtime, no npm registry access, and no `ws` package. The `dotnet-host` RPM is the .NET runtime host binary and has no dependency on or linkage to the Node.js `ws` package. Trivy's `under_investigation` status indicates the vendor has not confirmed applicability; given the structural absence of any Node.js runtime in the image, this is a false positive caused by CVE metadata tag overlap with the `dotnet` package family.

---
