# Trivy Remediation Log

This file is the system of record for all Trivy CVE verdicts for this repository.
New scan batches are **appended** — do not overwrite existing entries.

---

## Scan batch — 2026-06-30 | Scan ID 30439 | Service 45 | mmria-s2i:latest

- **Commit scanned:** `e5009f9b79f61b736ae6e39facf8272c56817f4c`
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)`
- **Severity counts (original):** Critical 0 · High 16
- **Dockerfiles updated:** `source-code/mmria/mmria-server/Dockerfile`, `nccdphp-drh-mmria-services/mmria.services/Dockerfile`

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 16 | 5 | 0 | 6 | 5 | 0 |

**Fix details:** CVE-2026-45591 appears on four packages (`aspnetcore-runtime-9.0`, `dotnet-host`, `dotnet-hostfxr-9.0`, `dotnet-runtime-9.0`) and CVE-2026-45447 appears on `openssl-libs` — all five instances are resolved by adding `dnf update` for those packages in the runtime stage of both Dockerfiles.

**Residual-risk items:** CVE-2026-54369 (`libacl`) and CVE-2026-54371 (`libattr`) have no fix available from Red Hat at this time; both require local attacker access to exploit.

**Not-applicable items:** CVE-2026-45736, CVE-2026-48779 (both affecting four .NET packages), and CVE-2026-10732 (affecting `dotnet-host`) describe Node.js npm module vulnerabilities (`ws`, `decompress`) that are not present in RHEL RPM .NET runtime packages.

### Finding inventory

| # | Package | Version | CVE | Severity | fixedIn | Status | Verdict |
|---|---|---|---|---|---|---|---|
| 1 | aspnetcore-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45591 | HIGH | 9.0.17-1.el8_10 | fixed | **Fixed** |
| 2 | aspnetcore-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45736 | HIGH | — | affected | **Not applicable / false positive** |
| 3 | aspnetcore-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-48779 | HIGH | — | affected | **Not applicable / false positive** |
| 4 | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-10732 | HIGH | — | affected | **Not applicable / false positive** |
| 5 | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-45591 | HIGH | 10.0.9-1.el8_10 | fixed | **Fixed** |
| 6 | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-45736 | HIGH | — | affected | **Not applicable / false positive** |
| 7 | dotnet-host | 10.0.8-1.el8_10 | CVE-2026-48779 | HIGH | — | affected | **Not applicable / false positive** |
| 8 | dotnet-hostfxr-9.0 | 9.0.16-1.el8_10 | CVE-2026-45591 | HIGH | 9.0.17-1.el8_10 | fixed | **Fixed** |
| 9 | dotnet-hostfxr-9.0 | 9.0.16-1.el8_10 | CVE-2026-45736 | HIGH | — | affected | **Not applicable / false positive** |
| 10 | dotnet-hostfxr-9.0 | 9.0.16-1.el8_10 | CVE-2026-48779 | HIGH | — | affected | **Not applicable / false positive** |
| 11 | dotnet-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45591 | HIGH | 9.0.17-1.el8_10 | fixed | **Fixed** |
| 12 | dotnet-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-45736 | HIGH | — | affected | **Not applicable / false positive** |
| 13 | dotnet-runtime-9.0 | 9.0.16-1.el8_10 | CVE-2026-48779 | HIGH | — | affected | **Not applicable / false positive** |
| 14 | libacl | 2.2.53-3.el8 | CVE-2026-54369 | HIGH | — | affected | **Residual risk – required, not reachable under current controls** |
| 15 | libattr | 2.4.48-3.el8 | CVE-2026-54371 | HIGH | — | affected | **Residual risk – required, not reachable under current controls** |
| 16 | openssl-libs | 1:1.1.1k-15.el8_6 | CVE-2026-45447 | HIGH | 1:1.1.1k-16.el8_6 | fixed | **Fixed** |

---

## SWA Exception Justifications

### CVE-2026-45736 — Not applicable / false positive

**Affected packages (this scan):** `aspnetcore-runtime-9.0 9.0.16-1.el8_10`, `dotnet-host 10.0.8-1.el8_10`, `dotnet-hostfxr-9.0 9.0.16-1.el8_10`, `dotnet-runtime-9.0 9.0.16-1.el8_10`

**CVE description (verbatim from findings):** "ws is an open source WebSocket client and server for Node.js. Prior to 8.20.1, the websocket.close() implementation is vulnerable to uninitialized memory disclosure when a TypedArray is passed as the reason argument."

**Evidence:** The CVE explicitly describes the `ws` npm module, a Node.js WebSocket library. The packages flagged in this scan are RHEL 8.10 RPMs for the .NET runtime (`aspnetcore-runtime-9.0`, `dotnet-host`, `dotnet-hostfxr-9.0`, `dotnet-runtime-9.0`) published by Red Hat under their .NET support program. These RPMs contain the Microsoft .NET CLR runtime and ASP.NET Core framework — they do not bundle, ship, or link against the Node.js `ws` npm package. The runtime Dockerfiles (`source-code/mmria/mmria-server/Dockerfile` runtime stage, `nccdphp-drh-mmria-services/mmria.services/Dockerfile` runtime stage) install no Node.js, npm, or JavaScript runtimes; the only base is `dotnet-90-runtime`. Verification command (Tier-2 handoff): `oc rsh <pod> rpm -ql aspnetcore-runtime-9.0 | grep -i 'node\|ws\|websocket'` — expected: no output. This is a Trivy advisory-database false attribution of a Node.js npm CVE to RHEL .NET RPM packages.

---

### CVE-2026-48779 — Not applicable / false positive

**Affected packages (this scan):** `aspnetcore-runtime-9.0 9.0.16-1.el8_10`, `dotnet-host 10.0.8-1.el8_10`, `dotnet-hostfxr-9.0 9.0.16-1.el8_10`, `dotnet-runtime-9.0 9.0.16-1.el8_10`

**CVE description (verbatim from findings):** "ws is an open source WebSocket client and server for Node.js. All versions from 1.1.0 up to (but not including) 5.2.5, from 6.0.0 up to 6.2.4, from 7.0.0 up to 7.5.11, and from 8.0.0 up to 8.21.0 are affected by a memory exhaustion DoS vulnerability."

**Evidence:** The CVE explicitly describes the `ws` npm module for Node.js (a memory exhaustion DoS in its message parsing). The packages flagged are RHEL 8.10 RPMs (`aspnetcore-runtime-9.0`, `dotnet-host`, `dotnet-hostfxr-9.0`, `dotnet-runtime-9.0`) published by Red Hat for the .NET CLR runtime. These RPMs implement the .NET managed runtime, class libraries, and ASP.NET Core hosting infrastructure. They do not contain the Node.js `ws` npm module or any Node.js JavaScript engine. The runtime Dockerfiles for this application install no Node.js or npm tooling; the final image layer contains only .NET runtime binaries. Verification command (Tier-2 handoff): `oc rsh <pod> rpm -ql dotnet-runtime-9.0 | grep -i 'node\|ws\|websocket'` — expected: no output. This is a Trivy advisory-database false attribution of a Node.js npm CVE to RHEL .NET RPM packages.

---

### CVE-2026-10732 — Not applicable / false positive

**Affected packages (this scan):** `dotnet-host 10.0.8-1.el8_10`

**CVE description (verbatim from findings):** "All versions of the package decompress are vulnerable to Arbitrary File Write via Archive Extraction (Zip Slip) when extracting a ZIP archive containing two entries with the same path — the first being a symlink to an arbitrary target and the second…"

**Evidence:** The CVE explicitly describes the `decompress` npm package, a Node.js archive-extraction library. The package flagged is `dotnet-host 10.0.8-1.el8_10`, a RHEL 8.10 RPM published by Red Hat that provides the .NET global host binary (`dotnet`). This RPM implements the native .NET entry-point host and does not bundle, vendor, or ship the `decompress` npm module or any Node.js tooling. The runtime Dockerfiles install no npm packages; the final image layer contains only .NET runtime binaries. Verification command (Tier-2 handoff): `oc rsh <pod> rpm -ql dotnet-host | grep -i 'decompress\|node\|npm'` — expected: no output. This is a Trivy advisory-database false attribution of a Node.js npm CVE to a RHEL .NET RPM package.

---

### CVE-2026-54369 — Residual risk – required, not reachable under current controls

**Affected package (this scan):** `libacl 2.2.53-3.el8`

**CVE description:** "acl before version 2.4.0 contains a symlink traversal vulnerability in the libacl pathname-based functions acl_get_file(), acl_set_file(), acl_extended_file(), and acl_delete_def_file() that allows local attackers to escalate privileges by replacing a pathname component with a symbolic link during directory hierarchy traversal."

**Status / fix availability:** No fix available in `el8` package stream at scan date; `fixedIn` is empty.

**Evidence and controls:** The exploit requires a **local attacker** with interactive shell access to the container (`AV:L` per CVSS network vector). The `libacl` package is a core POSIX ACL library required by numerous OS utilities and cannot be removed without breaking fundamental filesystem operations. The application container runs as unprivileged UID 1001 (non-root) under OpenShift, which enforces `restricted` or equivalent SCCs preventing privilege escalation. No shell access to pods is granted to end-users or application users. Compensating controls preventing exploit path: (a) no interactive shell access for application users, (b) non-root UID 1001 reduces privilege escalation surface, (c) OpenShift SCC blocks privileged operations. Verification command (Tier-2 handoff): `oc rsh <pod> id` — expected: `uid=1001` confirming non-root. Risk is accepted as residual pending a Red Hat fix in the RHEL 8 package stream.

---

### CVE-2026-54371 — Residual risk – required, not reachable under current controls

**Affected package (this scan):** `libattr 2.4.48-3.el8`

**CVE description:** "attr before version 2.6.0 contains a symlink traversal vulnerability in the getfattr and setfattr utilities that allows local attackers to escalate privileges by replacing a pathname component with a symbolic link during directory hierarchy traversal."

**Status / fix availability:** No fix available in `el8` package stream at scan date; `fixedIn` is empty.

**Evidence and controls:** The exploit requires a **local attacker** with interactive access to the container (`AV:L`). The `libattr` package provides extended-attribute support and is a mandatory OS dependency that cannot be removed without breaking core filesystem and ACL operations. The application container runs as unprivileged UID 1001 under OpenShift, which enforces SCCs preventing privilege escalation. No interactive shell access is provided to application users or end-users of the mmria application. Compensating controls: (a) container runs as non-root UID 1001, (b) OpenShift SCC blocks privileged filesystem operations, (c) no shell access granted to application users. Verification command (Tier-2 handoff): `oc rsh <pod> id` — expected: `uid=1001`. Risk is accepted as residual pending a Red Hat fix in the RHEL 8 package stream.
