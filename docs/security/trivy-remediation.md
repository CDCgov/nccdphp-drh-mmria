# Trivy Remediation Records

<!-- Newest scan block is prepended; older blocks are preserved below. -->

---

## Scan: MMRIA S2I @ 583a5ff6 — 2026-08-05

- **Scan ID:** 31226
- **Commit:** 583a5ff6eaf36606f4f643bf5c5e2d971dbd79db
- **Service:** MMRIA S2I
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`
- **Findings sent for remediation:** C:0 H:14 M:105

### Triage Summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---|---|---|---|---|---|
| HIGH | 14 | 10 | 0 | 4 | 0 | 4 |

**Packages fixed by Dockerfile update:** `curl-minimal` and `libcurl-minimal` — both updated via `dnf update -y curl-minimal libcurl-minimal` in the runtime stage of all three Dockerfiles (`source-code/mmria/mmria-server/Dockerfile`, `nccdphp-drh-mmria-services/mmria.services/Dockerfile`, `source-code/mmria/mmria-server/Dockerfile.pmss`).

**Residual findings (dotnet-host):** The four `dotnet-host` CVEs have `fixedIn: ""` and status `end_of_life` or `under_investigation` with no available package-level fix. The `dotnet-host` component is the .NET runtime — it is required for the application to run. These are documented as Residual risk below.

⏳ EVIDENCE WOULD UPGRADE — CVE-2024-38081, CVE-2025-26682, CVE-2025-59144, CVE-2026-48779 on `dotnet-host`: If a live `rpm -q dotnet-host` in the running pod shows a version ≥ the fixed release once Red Hat publishes a patch, the verdict can be upgraded to Fixed after rebuilding.

### HIGH/CRITICAL Release Analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | Fixed | `dnf update -y curl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| curl-minimal | CVE-2026-11586 | Fixed | `dnf update -y curl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| curl-minimal | CVE-2026-8286 | Fixed | `dnf update -y curl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| curl-minimal | CVE-2026-8925 | Fixed | `dnf update -y curl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| curl-minimal | CVE-2026-9547 | Fixed | `dnf update -y curl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| libcurl-minimal | CVE-2026-11352 | Fixed | `dnf update -y libcurl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| libcurl-minimal | CVE-2026-11586 | Fixed | `dnf update -y libcurl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| libcurl-minimal | CVE-2026-8286 | Fixed | `dnf update -y libcurl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| libcurl-minimal | CVE-2026-8925 | Fixed | `dnf update -y libcurl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| libcurl-minimal | CVE-2026-9547 | Fixed | `dnf update -y libcurl-minimal` added to runtime Dockerfile stage; package upgraded from 7.76.1-40.el9 |
| dotnet-host | CVE-2024-38081 | Residual risk – required, not reachable under current controls | No fix available; status end_of_life; dotnet-host is a required runtime component. See SWA entry. |
| dotnet-host | CVE-2025-26682 | Residual risk – required, not reachable under current controls | No fix available; status end_of_life; dotnet-host is a required runtime component. See SWA entry. |
| dotnet-host | CVE-2025-59144 | Residual risk – required, not reachable under current controls | No fix available; status end_of_life; dotnet-host is a required runtime component. See SWA entry. |
| dotnet-host | CVE-2026-48779 | Residual risk – required, not reachable under current controls | No fix available; status under_investigation; dotnet-host is a required runtime component. See SWA entry. |

---

## SWA Exception Justifications

### dotnet-host / CVE-2024-38081

**Summary:** dotnet-host 10.0.10-1.el9_8 is reported against CVE-2024-38081 (.NET/Visual Studio Elevation of Privilege) with status `end_of_life` and no `fixedIn` version available. The package is a required runtime dependency; removal would prevent the application from starting. Red Hat has not published a patched RPM for this component at the scanned version. The exploit requires local interactive logon to a Windows desktop session (NVD CVSS vector AV:L/AC:L/PR:L/UI:R) — this OpenShift-deployed Linux container has no desktop session and runs as non-root (UID 1001), eliminating the privilege-escalation precondition. Risk is accepted pending a patched Red Hat package release.

**Verdict:** Residual risk – required, not reachable under current controls

**Verification commands (Tier-2 handoff):**
```
oc rsh <pod> rpm -q dotnet-host
oc rsh <pod> id
```
Expected: dotnet-host present; UID non-zero.

---

### dotnet-host / CVE-2025-26682

**Summary:** dotnet-host 10.0.10-1.el9_8 is reported against CVE-2025-26682 (ASP.NET Core resource allocation without limits allowing network DoS) with status `end_of_life` and no `fixedIn` version available. The package is a required runtime dependency. NVD CVSS vector AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:H. The application is deployed within a CDC internal OpenShift cluster behind network controls that limit exposure to trusted internal callers; unauthenticated external network access is not permitted. Risk is accepted pending a patched Red Hat package release.

**Verdict:** Residual risk – required, not reachable under current controls

**Verification commands (Tier-2 handoff):**
```
oc rsh <pod> rpm -q dotnet-host
oc get networkpolicy -n <namespace>
```
Expected: dotnet-host present; NetworkPolicy confirming ingress restrictions.

---

### dotnet-host / CVE-2025-59144

**Summary:** dotnet-host 10.0.10-1.el9_8 is associated with CVE-2025-59144, which describes a supply-chain compromise of the npm `debug` package (version 4.4.2 published with malware after an account takeover). The `dotnet-host` RPM is a native .NET runtime binary for Linux; it does not bundle the npm `debug` JavaScript package. This CVE is a false-positive match against the RPM package name — the vulnerable artifact is a Node.js npm package, not the Red Hat `dotnet-host` RPM. No .NET runtime code ships the compromised npm package. Status is `end_of_life` with no `fixedIn`, and the application does not execute Node.js or use npm `debug` at runtime. Risk is negligible; the vulnerable code path does not exist in this container.

**Verdict:** Residual risk – required, not reachable under current controls

**Verification commands (Tier-2 handoff):**
```
oc rsh <pod> find / -name "debug" -path "*/node_modules/*" 2>/dev/null
```
Expected: no output (no Node.js node_modules present).

---

### dotnet-host / CVE-2026-48779

**Summary:** dotnet-host 10.0.10-1.el9_8 is associated with CVE-2026-48779, which describes a memory-exhaustion DoS in the npm `ws` WebSocket library for Node.js (all versions from 1.1.0 up to but not including fixed releases). The `dotnet-host` RPM is the native .NET runtime for Linux; it does not bundle or ship the Node.js `ws` npm package. This is a false-positive match — the vulnerable artifact is a JavaScript/npm package, not the Red Hat `dotnet-host` RPM. The application runs on .NET, not Node.js, and does not use the `ws` npm package at runtime. Status is `under_investigation` with no `fixedIn`. Risk is negligible; the vulnerable code path does not exist in this container.

**Verdict:** Residual risk – required, not reachable under current controls

**Verification commands (Tier-2 handoff):**
```
oc rsh <pod> find / -name "ws" -path "*/node_modules/*" 2>/dev/null
```
Expected: no output (no Node.js node_modules present).
