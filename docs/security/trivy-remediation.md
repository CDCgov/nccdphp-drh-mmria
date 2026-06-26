# Trivy Remediation Log

This file is the system of record for all Trivy vulnerability remediation actions taken in this repository.
Entries are appended each scan cycle and carry forward prior verdicts where applicable.

---

## Scan: 2026-06-26 — Service 45 — Commit 6e95d0f9

- **Scan ID:** 30345
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)`
- **Severity counts:** Critical: 0 | High: 14

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 5 | 0 | 9 | 0 | 9 |
| **TOTAL** | **14** | **5** | **0** | **9** | **0** | **9** |

### Findings inventory

| # | Target | Package | Version | CVE | Severity | Verdict |
|---|---|---|---|---|---|---|
| 1 | mmria-s2i:latest (redhat 8.10) | aspnetcore-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45591 | HIGH | Fixed |
| 2 | mmria-s2i:latest (redhat 8.10) | aspnetcore-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45736 | HIGH | Residual risk – required, not reachable under current controls |
| 3 | mmria-s2i:latest (redhat 8.10) | aspnetcore-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-48779 | HIGH | Residual risk – required, not reachable under current controls |
| 4 | mmria-s2i:latest (redhat 8.10) | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-10732 | HIGH | Residual risk – required, not reachable under current controls |
| 5 | mmria-s2i:latest (redhat 8.10) | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-45591 | HIGH | Fixed |
| 6 | mmria-s2i:latest (redhat 8.10) | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-45736 | HIGH | Residual risk – required, not reachable under current controls |
| 7 | mmria-s2i:latest (redhat 8.10) | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-48779 | HIGH | Residual risk – required, not reachable under current controls |
| 8 | mmria-s2i:latest (redhat 8.10) | dotnet-hostfxr-9.0 | 9.0.16-1.el8_10 | CVE-2026-45591 | HIGH | Fixed |
| 9 | mmria-s2i:latest (redhat 8.10) | dotnet-hostfxr-9.0 | 9.0.16-1.el8_10 | CVE-2026-45736 | HIGH | Residual risk – required, not reachable under current controls |
| 10 | mmria-s2i:latest (redhat 8.10) | dotnet-hostfxr-9.0 | 9.0.16-1.el8_10 | CVE-2026-48779 | HIGH | Residual risk – required, not reachable under current controls |
| 11 | mmria-s2i:latest (redhat 8.10) | dotnet-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45591 | HIGH | Fixed |
| 12 | mmria-s2i:latest (redhat 8.10) | dotnet-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45736 | HIGH | Residual risk – required, not reachable under current controls |
| 13 | mmria-s2i:latest (redhat 8.10) | dotnet-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-48779 | HIGH | Residual risk – required, not reachable under current controls |
| 14 | mmria-s2i:latest (redhat 8.10) | openssl-libs | 1:1.1.1k-15.el8_6 | CVE-2026-45447 | HIGH | Fixed |

### Residual-risk findings — upgrade candidates

⏳ **EVIDENCE WOULD UPGRADE** — rows 2, 3, 6, 7, 9, 10, 12, 13 (CVE-2026-45736 / CVE-2026-48779 on all four .NET packages):
NVD confirms both CVEs describe the Node.js `ws` npm package, not .NET/ASP.NET packages. If the running image does not bundle Node.js or the `ws` npm module, all eight findings are false positives. Developer can verify with:
```bash
oc rsh <mmria-pod> sh -c "rpm -q nodejs; find /usr /opt /app -name 'ws' -path '*/node_modules/ws/package.json' 2>/dev/null; echo done"
```
If output shows nodejs is not installed and no ws package.json is found, upgrade verdict to **Not applicable / false positive**.

⏳ **EVIDENCE WOULD UPGRADE** — row 4 (CVE-2026-10732 on dotnet-host):
NVD (Snyk CNA) confirms this CVE describes the npm `decompress` package, not `dotnet-host`. If the running image does not bundle Node.js or the `decompress` npm module, this finding is a false positive. Developer can verify with:
```bash
oc rsh <mmria-pod> sh -c "rpm -q nodejs; find /usr /opt /app -name 'decompress' -path '*/node_modules/decompress/package.json' 2>/dev/null; echo done"
```
If output shows nodejs is not installed and no decompress package.json is found, upgrade verdict to **Not applicable / false positive**.

---

## SWA Exception Justifications

### CVE-2026-45591 — aspnetcore-runtime-9.0 | dotnet-host | dotnet-hostfxr-9.0 | dotnet-runtime-9.0

**Verdict: Fixed**

A fix is available for all affected packages (aspnetcore-runtime-9.0 → 9.0.17-1.el8_10; dotnet-host → 10.0.9-1.el8_10; dotnet-hostfxr-9.0 → 9.0.17-1.el8_10; dotnet-runtime-9.0 → 9.0.17-1.el8_10). NVD describes uncontrolled resource consumption in ASP.NET Core allowing unauthenticated remote denial of service (Microsoft MSRC advisory). The vulnerability is resolved by the package updates applied via `dnf update -y` in the Dockerfile runtime stages and `.s2i/bin/assemble` script added in this PR (files: `source-code/mmria/mmria-server/Dockerfile`, `source-code/mmria/mmria-server/Dockerfile.pmss`, `nccdphp-drh-mmria-services/mmria.services/Dockerfile`, `.s2i/bin/assemble`). A rebuild and rescan is required to confirm removal; verification is a handoff to CI/CD.

---

### CVE-2026-45447 — openssl-libs

**Verdict: Fixed**

A fix is available (openssl-libs → 1:1.1.1k-16.el8_6). NVD describes a use-after-free in OpenSSL's PKCS7_verify() triggered when a PKCS#7 or S/MIME signed message contains an empty digestAlgorithms SET; the BIO passed by the caller is freed prematurely, with the caller's subsequent BIO_free() causing heap corruption or potentially remote code execution. The CMS API path (used by most modern applications) is not affected per the NVD description. The fix is applied via `dnf update -y openssl-libs` in the Dockerfile runtime stages and `.s2i/bin/assemble` added in this PR. A rebuild and rescan is required to confirm removal; verification is a handoff to CI/CD.

---

### CVE-2026-45736 — aspnetcore-runtime-9.0 | dotnet-host | dotnet-hostfxr-9.0 | dotnet-runtime-9.0

**Verdict: Residual risk – required, not reachable under current controls**

No fix is available for this CVE in any of the four affected .NET packages (`fixedIn` is empty, status `affected`). NVD confirms the vulnerability is in the Node.js `ws` npm package (WebSocket, CWE-908: Use of Uninitialized Resource) — specifically the `websocket.close()` implementation disclosing uninitialized memory when a TypedArray is passed as the reason argument. The CVE has been associated with the .NET runtime RPM packages by the scanner. The .NET runtime packages are required for MMRIA application operation and cannot be removed. No vendor-supplied patch exists in the RHEL 8.10 package feed as of the scan date. Residual risk is accepted pending availability of a vendor fix or confirmation that Node.js/ws is absent from the runtime image. Exploit requires the running application to call `websocket.close()` with a TypedArray argument, which is a Node.js WebSocket code path not present in .NET-managed code.

---

### CVE-2026-48779 — aspnetcore-runtime-9.0 | dotnet-host | dotnet-hostfxr-9.0 | dotnet-runtime-9.0

**Verdict: Residual risk – required, not reachable under current controls**

No fix is available for this CVE in any of the four affected .NET packages (`fixedIn` is empty, status `affected`). NVD confirms the vulnerability is in the Node.js `ws` npm package (CWE-400: Uncontrolled Resource Consumption, CWE-770: Allocation of Resources Without Limits or Throttling) — an attacker can send high-volume small fragments to force excessive memory allocation leading to OOM process termination. The CVE has been associated with the .NET runtime RPM packages by the scanner. The .NET runtime packages are required for MMRIA application operation and cannot be removed. No vendor-supplied patch exists in the RHEL 8.10 package feed as of the scan date. Residual risk is accepted pending availability of a vendor fix or confirmation that Node.js/ws is absent from the runtime image. The exploit path requires the application to use the Node.js `ws` WebSocket library, which is not a .NET application code path.

---

### CVE-2026-10732 — dotnet-host

**Verdict: Residual risk – required, not reachable under current controls**

No fix is available for this CVE in `dotnet-host` (`fixedIn` is empty, status `affected`). NVD (Snyk CNA) confirms the vulnerability is in the npm `decompress` package (CWE-29: Path Traversal) — a Zip Slip vulnerability where extracting a specially crafted ZIP archive can write files through a symlink to arbitrary locations outside the extraction directory, potentially enabling remote code execution. The CVE has been associated with the `dotnet-host` RPM package by the scanner. The `dotnet-host` package is required for MMRIA application operation and cannot be removed. No vendor-supplied patch exists in the RHEL 8.10 package feed as of the scan date. Residual risk is accepted pending availability of a vendor fix or confirmation that the npm `decompress` package is absent from the runtime image. The exploit requires an attacker to supply a malicious ZIP archive to the `decompress` npm module, which is not a .NET host code path.
