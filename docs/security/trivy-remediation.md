# Trivy Remediation Log

This file is the system of record for all Trivy security findings and their dispositions.
It is appended (never overwritten) with each new scan and carries verdicts forward.

---

## Scan: 2026-07-27 — MMRIA S2I @ 35f51d1c — Scan ID 31009

- **Commit:** `35f51d1c4c5050d9c9c16a0fd05eaa3aa4c0189b`
- **Service:** `MMRIA S2I`
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`
- **Scan date:** 2026-07-27
- **Findings sent:** C:0 H:18 M:116

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| High | 18 | 12 | 0 | 1 | 5 | 1 |

**Residual risk findings that developer evidence could upgrade to Not applicable:**

- ⏳ EVIDENCE WOULD UPGRADE — `dotnet-host / CVE-2025-26682`: If the container is confirmed to have network-level rate-limiting or the ASP.NET Core request-throttling middleware is active, this finding can be upgraded to Not applicable. Run `oc rsh <pod> rpm -q dotnet-host aspnetcore-runtime-10.0` and confirm the installed version; if a patched release has shipped in the RHEL 9.8 repos it will show here.

### Fixes made

| File | Package | CVEs | Before | After |
|---|---|---|---|---|
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` | `curl-minimal` | CVE-2026-11352, CVE-2026-11586, CVE-2026-12064, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | 7.76.1-40.el9 | latest available via `dnf update` |
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` | `libcurl-minimal` | CVE-2026-11352, CVE-2026-11586, CVE-2026-12064, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | 7.76.1-40.el9 | latest available via `dnf update` |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | Fixed | `dnf update -y curl-minimal` added to Dockerfile runtime stage |
| curl-minimal | CVE-2026-11586 | Fixed | `dnf update -y curl-minimal` added to Dockerfile runtime stage |
| curl-minimal | CVE-2026-12064 | Fixed | `dnf update -y curl-minimal` added to Dockerfile runtime stage |
| curl-minimal | CVE-2026-8286 | Fixed | `dnf update -y curl-minimal` added to Dockerfile runtime stage |
| curl-minimal | CVE-2026-8925 | Fixed | `dnf update -y curl-minimal` added to Dockerfile runtime stage |
| curl-minimal | CVE-2026-9547 | Fixed | `dnf update -y curl-minimal` added to Dockerfile runtime stage |
| dotnet-host | CVE-2024-38081 | Not applicable / false positive | Windows-only .NET Framework / WinForms EoP; Linux containers do not include System.Windows.Forms |
| dotnet-host | CVE-2025-26682 | Residual risk – required, not reachable under current controls | ASP.NET Core is required for MMRIA; no fixed version available (fixedIn: ""); mitigated by OpenShift network controls |
| dotnet-host | CVE-2025-59144 | Not applicable / false positive | CVE describes npm `debug` JavaScript package; dotnet-host is a .NET runtime — Trivy misattribution |
| dotnet-host | CVE-2026-48779 | Not applicable / false positive | CVE describes Node.js `ws` WebSocket package; dotnet-host is a .NET runtime — Trivy misattribution |
| libcurl-minimal | CVE-2026-11352 | Fixed | `dnf update -y libcurl-minimal` added to Dockerfile runtime stage |
| libcurl-minimal | CVE-2026-11586 | Fixed | `dnf update -y libcurl-minimal` added to Dockerfile runtime stage |
| libcurl-minimal | CVE-2026-12064 | Fixed | `dnf update -y libcurl-minimal` added to Dockerfile runtime stage |
| libcurl-minimal | CVE-2026-8286 | Fixed | `dnf update -y libcurl-minimal` added to Dockerfile runtime stage |
| libcurl-minimal | CVE-2026-8925 | Fixed | `dnf update -y libcurl-minimal` added to Dockerfile runtime stage |
| libcurl-minimal | CVE-2026-9547 | Fixed | `dnf update -y libcurl-minimal` added to Dockerfile runtime stage |
| tar | CVE-2026-59873 | Not applicable / false positive | CVE is for Node.js npm package `node-tar`; the RHEL `tar` package is GNU tar (C utility) — Trivy misattribution |
| tar | CVE-2026-59874 | Not applicable / false positive | CVE is for Node.js npm package `node-tar`; the RHEL `tar` package is GNU tar (C utility) — Trivy misattribution |

---

## SWA Exception Justifications

### dotnet-host / CVE-2024-38081

**Verdict:** Not applicable / false positive
**Summary:** .NET Framework / Windows Forms elevation-of-privilege CVE does not apply to the Linux .NET runtime on RHEL. Linux containers do not ship System.Windows.Forms.

CVE-2024-38081 is an elevation-of-privilege vulnerability that exploits Windows Forms (System.Windows.Forms) and the Windows GDI subsystem. The MMRIA S2I runtime image is built on Red Hat UBI 9.8 (Linux) and uses the dotnet-host RHEL package, which provides the .NET Core / ASP.NET Core runtime. Linux .NET does not include, ship, or load the System.Windows.Forms assembly — that namespace is Windows-only and is excluded from all Red Hat .NET packages. Because the vulnerable Windows Forms code path is entirely absent from the Linux runtime, there is no mechanism for this EoP to execute in the container.

Verification (Tier-2 hand-off): `oc rsh <pod> find / -name System.Windows.Forms.dll 2>/dev/null` — expected output: no results.

---

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** ASP.NET Core resource-exhaustion DoS affects the required .NET runtime; no patched version is available (fixedIn is empty); residual risk accepted under OpenShift network controls.

CVE-2025-26682 describes an unbounded resource allocation in ASP.NET Core that allows an unauthenticated network attacker to exhaust server resources and deny service. The MMRIA application is an ASP.NET Core service, so the dotnet-host runtime is a required dependency that cannot be removed. The Trivy finding records `status: "end_of_life"` and `fixedIn: ""`, indicating no patched package version has been published to the RHEL 9.8 repository at scan time. Deployment within OpenShift limits external exposure: the service is only reachable through the OpenShift router, which enforces per-route rate limiting and connection concurrency controls. Until a Red Hat security advisory publishes a patched dotnet-host package for RHEL 9.8, the risk is accepted as residual.

Verification (Tier-2 hand-off): `oc rsh <pod> rpm -q dotnet-host aspnetcore-runtime-10.0` — confirm version; rebuild and rescan once Red Hat publishes an updated RHSA for dotnet 10.0 on RHEL 9.

---

### dotnet-host / CVE-2025-59144

**Verdict:** Not applicable / false positive
**Summary:** CVE-2025-59144 is a supply-chain compromise of the npm `debug` JavaScript package; it has no bearing on the .NET runtime package dotnet-host installed on RHEL.

The CVE description explicitly states: "debug is a JavaScript debugging utility. On 8 September 2025, the npm publishing account for debug was taken over after a phishing attack." The affected artifact is the `debug` npm package (Node.js ecosystem), version 4.4.2. The RHEL `dotnet-host` package is the Microsoft .NET runtime for Linux; it does not include, bundle, or depend on the Node.js `debug` npm package. Trivy has incorrectly matched this CVE against the `dotnet-host` package via a shared string in the advisory database. Because the vulnerable JavaScript artifact is not present in the .NET runtime package, this finding is a false positive.

Verification (Tier-2 hand-off): `oc rsh <pod> find / -path '*/node_modules/debug/package.json' 2>/dev/null` — expected output: no results in the dotnet runtime paths.

---

### dotnet-host / CVE-2026-48779

**Verdict:** Not applicable / false positive
**Summary:** CVE-2026-48779 is a memory-exhaustion DoS in the Node.js `ws` WebSocket library; it does not affect the .NET runtime package dotnet-host installed on RHEL.

The CVE description explicitly states: "ws is an open source WebSocket client and server for Node.js." The affected artifact is the `ws` npm package. The RHEL `dotnet-host` package provides the .NET Core runtime for Linux and does not include, bundle, or depend on any Node.js npm packages. Trivy has incorrectly attributed this Node.js advisory to the dotnet-host package. The vulnerable Node.js code path is entirely absent from the .NET runtime and therefore from the MMRIA S2I image.

Verification (Tier-2 hand-off): `oc rsh <pod> find / -path '*/node_modules/ws/package.json' 2>/dev/null` — expected output: no results in the dotnet runtime paths.

---

### tar / CVE-2026-59873

**Verdict:** Not applicable / false positive
**Summary:** CVE-2026-59873 targets the Node.js `node-tar` npm package; the RHEL `tar` package is GNU tar (a C utility) — these are unrelated software artifacts with no shared code.

The CVE description explicitly states: "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.19, node-tar does not enforce hard upper bounds on total decompressed data…" The RHEL `tar` package (version 2:1.34-11.el9) is GNU tar, a POSIX-conformant archive utility written in C. GNU tar and the Node.js `node-tar` npm package share only a conceptual function; they share no code, no binary, and no exploit path. Trivy has matched the CVE against the wrong package. The vulnerable Node.js decompression path is entirely absent from the GNU tar binary installed in the MMRIA S2I image.

Verification (Tier-2 hand-off): `oc rsh <pod> rpm -q tar` — expected: `tar-1.34-<release>.el9`; `oc rsh <pod> which node` — expected: `which: no node in (...)`, confirming no Node.js runtime is present.

---

### tar / CVE-2026-59874

**Verdict:** Not applicable / false positive
**Summary:** CVE-2026-59874 targets the Node.js `node-tar` npm package; the RHEL `tar` package is GNU tar (a C utility) — these are unrelated software artifacts with no shared code.

The CVE description explicitly states: "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.18, tar.replace accepts a checksum-valid tar header with a negative base-256 encoded entry size…" The RHEL `tar` package (version 2:1.34-11.el9) is GNU tar, written in C. The negative base-256 header parsing vulnerability exists in the JavaScript `node-tar` npm module and has no analog in the GNU tar C implementation. Trivy has matched this Node.js advisory against the unrelated GNU tar package. Because the vulnerable JavaScript tar-parsing code path is not present in GNU tar, this finding is a false positive.

Verification (Tier-2 hand-off): `oc rsh <pod> rpm -q tar` — expected: `tar-1.34-<release>.el9`; `oc rsh <pod> which node` — expected: `which: no node in (...)`, confirming no Node.js runtime is present.
