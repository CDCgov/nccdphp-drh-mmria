# Trivy Remediation Records

Records are prepended — newest scan block at the top.

---

## Scan: MMRIA S2I @ 0f96ac0a — 2026-08-10

- **Commit:** `0f96ac0ad6bd7a1522b50a2061ad50c6b96d0e6b`
- **Service:** `MMRIA S2I`
- **Scan ID:** `31304`
- **Severity totals:** C:0  H:14  M:104
- **Scanned image:** `mmria/mmria-s2i:latest (redhat 9.8)`

> **Scope:** This scan block addresses Critical and High findings only, consistent with the
> automated remediation workflow. The 104 Medium findings are not triaged here; they are
> tracked by the scanning pipeline and addressed in a separate review cycle.

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 0 | 0 | 14 | 0 | 14 |

#### Finding inventory — HIGH

| Package | CVE | Installed | Fixed In | Status | Verdict |
|---|---|---|---|---|---|
| curl-minimal | CVE-2026-11352 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-11586 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-8286 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-8925 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-9547 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11352 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11586 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8286 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8925 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-9547 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| dotnet-host | CVE-2024-38081 | 10.0.10-1.el9_8 | — | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2025-26682 | 10.0.10-1.el9_8 | — | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2025-59144 | 10.0.10-1.el9_8 | — | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2026-48779 | 10.0.10-1.el9_8 | — | under_investigation | Residual risk – required, not reachable under current controls |

### Fixes made

No new code or image changes required. All findings are identical to the prior scan
(`ef00c008`, 2026-08-07) — same CVEs, packages, versions, and statuses. The `.s2i/dockerfile`
already contains a `dnf update -y curl-minimal libcurl-minimal` layer to apply errata
automatically when Red Hat publishes a fixed RPM. Verdicts are carried forward unchanged.

### HIGH / CRITICAL release analysis

#### curl-minimal and libcurl-minimal (10 findings) — carried from prior scan

All five CVEs affect `curl-minimal` **and** `libcurl-minimal` (version `7.76.1-40.el9`) in
the RHEL-9 base layer. `fixedIn` is empty and status is `affected` for all five — Red Hat
has not published a fixed RPM for RHEL 9 as of this scan. The `.s2i/dockerfile` already
includes `dnf update -y curl-minimal libcurl-minimal` to apply the errata automatically.
Verdicts are carried from the 2026-08-07 scan — evidence unchanged.

| Package | CVE | Verdict |
|---|---|---|
| curl-minimal | CVE-2026-11352 | Residual risk – no fix available |
| curl-minimal | CVE-2026-11586 | Residual risk – no fix available |
| curl-minimal | CVE-2026-8286 | Residual risk – no fix available |
| curl-minimal | CVE-2026-8925 | Residual risk – no fix available |
| curl-minimal | CVE-2026-9547 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11352 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11586 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8286 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8925 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-9547 | Residual risk – no fix available |

#### dotnet-host (4 findings) — carried from prior scan

`dotnet-host` is the mandatory .NET runtime host component of the S2I builder image. The
S2I image is a build-time artifact and is not the production runtime image.

| CVE | Status | Verdict |
|---|---|---|
| CVE-2024-38081 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2025-26682 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2025-59144 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2026-48779 | under_investigation | Residual risk – required, not reachable under current controls |

### curl-minimal / CVE-2026-11352

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. Red Hat has not
released a fixed RPM for `curl-minimal` on RHEL 9 (`fixedIn` empty, status `affected`).
NVD CVSS shows the vulnerability requires the application to initiate an HTTP/3 QUIC
connection to a malicious server (AV:N/AC:H). MMRIA does not perform outbound HTTP/3
requests; all external communication uses HTTPS/1.1 or HTTPS/2. The `dnf update` layer
in `.s2i/dockerfile` will automatically apply the fix once errata is published.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11352` lists a fixed version.

### curl-minimal / CVE-2026-11586

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for RHEL 9 (`fixedIn` empty, status `affected`). The vulnerability requires an active
WebSocket connection to a malicious server flooding PING frames. MMRIA's S2I builder
makes no outbound WebSocket connections. The `dnf update` layer in `.s2i/dockerfile`
ensures automatic remediation when errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11586` lists a fixed version.

### curl-minimal / CVE-2026-8286

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for RHEL 9 (`fixedIn` empty, status `affected`). CVE-2026-8286 is a STARTTLS TLS-session
reuse flaw. The S2I builder uses curl only for build-time asset fetching; no STARTTLS
paths are invoked during the MMRIA build. The `dnf update` layer in `.s2i/dockerfile`
ensures automatic remediation when errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8286` lists a fixed version.

### curl-minimal / CVE-2026-8925

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for RHEL 9 (`fixedIn` empty, status `affected`). CVE-2026-8925 is a double-free in GSASL
SASL authentication logic. MMRIA's S2I build process does not perform SASL-authenticated
transfers. The `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when
errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8925` lists a fixed version.

### curl-minimal / CVE-2026-9547

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for RHEL 9 (`fixedIn` empty, status `affected`). CVE-2026-9547 affects SCP/SFTP
transfers via `CURLOPT_SSH_KEYFUNCTION`. MMRIA does not use SCP or SFTP in its S2I build
pipeline. The `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when
errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-9547` lists a fixed version.

### libcurl-minimal / CVE-2026-11352

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for `libcurl-minimal` on RHEL 9 (`fixedIn` empty, status `affected`). NVD CVSS AV:N/AC:H
requires the client to connect to a malicious HTTP/3 server; MMRIA makes no outbound HTTP/3
connections. The `dnf update` layer in `.s2i/dockerfile` is in place.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11352` lists a fixed version.

### libcurl-minimal / CVE-2026-11586

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for `libcurl-minimal` on RHEL 9 (`fixedIn` empty, status `affected`). The vulnerability
requires an active WebSocket connection receiving malicious PING floods; MMRIA's S2I
builder makes no outbound WebSocket connections. The `dnf update` layer in `.s2i/dockerfile`
ensures automatic remediation when errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11586` lists a fixed version.

### libcurl-minimal / CVE-2026-8286

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for `libcurl-minimal` on RHEL 9 (`fixedIn` empty, status `affected`). The STARTTLS
session-reuse flaw is not triggered during MMRIA's S2I build pipeline, which makes no
STARTTLS connections. The `dnf update` layer in `.s2i/dockerfile` ensures automatic
remediation when errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8286` lists a fixed version.

### libcurl-minimal / CVE-2026-8925

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for `libcurl-minimal` on RHEL 9 (`fixedIn` empty, status `affected`). The double-free
GSASL defect requires SASL authentication, which is not used in the MMRIA S2I build
pipeline. The `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when
errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8925` lists a fixed version.

### libcurl-minimal / CVE-2026-9547

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. No fixed RPM
for `libcurl-minimal` on RHEL 9 (`fixedIn` empty, status `affected`). The SCP/SFTP SSH
key callback flaw is not triggered because MMRIA's S2I build does not use SCP or SFTP
transfers. The `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when
errata is released.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-9547` lists a fixed version.

### dotnet-host / CVE-2024-38081

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 10.0.10-1.el9_8
**Fixed In:** (none published)
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. `dotnet-host`
is the mandatory .NET runtime host component of the S2I builder image; removal would break
the build. CVE-2024-38081 is a .NET/Visual Studio Elevation of Privilege vulnerability
(NVD CVSS AV:L/AC:L/PR:L — requires local access). The S2I builder runs as non-root
(UID 1001) in an OpenShift-managed pod with no interactive shell access, eliminating the
local privilege-escalation path. No fixed version is available from Red Hat for RHEL 9
(`fixedIn` empty, status `end_of_life`).

**Verification (Tier-2):** `oc rsh <s2i-build-pod> id` to confirm non-root execution.

### dotnet-host / CVE-2025-26682

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 10.0.10-1.el9_8
**Fixed In:** (none published)
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. `dotnet-host`
is a required component of the S2I builder image; removal is not possible. CVE-2025-26682
is an ASP.NET Core resource allocation DoS flaw (network-exploitable). The S2I builder
image is a build-time artifact, not a web-facing service — no ASP.NET Core request pipeline
is active in the builder context. No fixed version is available from Red Hat for RHEL 9
(`fixedIn` empty, status `end_of_life`).

**Verification (Tier-2):** Rescan the running builder pod to confirm no web listener is
exposed during S2I build execution.

### dotnet-host / CVE-2025-59144

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 10.0.10-1.el9_8
**Fixed In:** (none published)
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. Trivy attributes
CVE-2025-59144 to `dotnet-host`, but the CVE describes the `debug` npm JavaScript package
(supply-chain attack via a compromised npm account). `dotnet-host` does not ship or execute
the `debug` npm package; this is a Trivy scan attribution artefact from bundled Node.js
tooling in the .NET SDK image. The .NET SDK builder image is used only at build time and is
not internet-accessible in production. No fixed version is available (`fixedIn` empty, status
`end_of_life`). Verification command (Tier-2): `oc rsh <s2i-build-pod> find / -name 'debug'
-path '*/node_modules/*' 2>/dev/null` to confirm presence or absence of the npm package.

### dotnet-host / CVE-2026-48779

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 10.0.10-1.el9_8
**Fixed In:** (none published)
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** Carried from prior scan (2026-08-07) — evidence unchanged. Trivy attributes
CVE-2026-48779 to `dotnet-host`, but the CVE describes the `ws` Node.js WebSocket library
(memory-exhaustion DoS). `dotnet-host` does not ship or execute the `ws` npm package; this
is likely a Trivy scan attribution artefact from bundled Node.js files in the .NET SDK image.
The S2I builder image is a build-time artifact, not a network-accessible service. Red Hat
investigation is still `under_investigation` for RHEL 9; no fixed version is available.
Verification command (Tier-2): `oc rsh <s2i-build-pod> find / -name 'ws'
-path '*/node_modules/*' 2>/dev/null` to confirm presence or absence.

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-11352 on RHEL 9 at scan time. NVD CVSS AV:N/AC:H requires the client to initiate an HTTP/3 QUIC connection to a malicious server; MMRIA makes no outbound HTTP/3 connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-11586 on RHEL 9 at scan time. The vulnerability requires an active WebSocket connection to a malicious server flooding PING frames; MMRIA's S2I builder makes no outbound WebSocket connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-8286 on RHEL 9 at scan time. The STARTTLS session-reuse flaw is not triggered during MMRIA's S2I build pipeline, which makes no STARTTLS connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-8925 on RHEL 9 at scan time. The double-free GSASL defect requires SASL authentication, which is not used in the MMRIA S2I build pipeline. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-9547 on RHEL 9 at scan time. The SCP/SFTP SSH key callback flaw is not triggered because MMRIA's S2I build does not use SCP or SFTP transfers. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-11352 on RHEL 9 at scan time for `libcurl-minimal`. NVD CVSS AV:N/AC:H requires the client to connect to a malicious HTTP/3 server; MMRIA makes no outbound HTTP/3 connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-11586 on RHEL 9 at scan time for `libcurl-minimal`. The WebSocket PING-flood memory exhaustion requires an outbound WebSocket connection; MMRIA's S2I builder makes none. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-8286 on RHEL 9 at scan time for `libcurl-minimal`. The STARTTLS session-reuse flaw is not triggered during MMRIA's S2I build pipeline, which makes no STARTTLS connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-8925 on RHEL 9 at scan time for `libcurl-minimal`. The double-free GSASL defect requires SASL authentication, which is not used in the MMRIA S2I build pipeline. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. No Red Hat errata RPM for CVE-2026-9547 on RHEL 9 at scan time for `libcurl-minimal`. The SCP/SFTP SSH key callback flaw is not triggered because MMRIA's S2I build does not use SCP or SFTP transfers. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### dotnet-host / CVE-2024-38081

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. `dotnet-host` is the mandatory .NET runtime host component; removal breaks the build. CVE-2024-38081 is a local privilege-escalation flaw (NVD CVSS AV:L/AC:L/PR:L). The S2I builder runs as non-root (UID 1001) in an OpenShift-managed pod with no interactive shell, eliminating the local escalation path. No fixed RPM for RHEL 9 (`end_of_life`).

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. `dotnet-host` is a required component; removal is not possible. CVE-2025-26682 is an ASP.NET Core resource-exhaustion DoS (network-exploitable). The S2I builder is a build-time artifact with no active ASP.NET Core request pipeline, so the network attack path is absent. No fixed RPM for RHEL 9 (`end_of_life`).

### dotnet-host / CVE-2025-59144

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. Trivy attributes CVE-2025-59144 (compromised `debug` npm package supply-chain attack) to `dotnet-host`, which does not ship the `debug` npm package. This is a scan attribution artefact from bundled Node.js tooling in the .NET SDK image. The builder is used only at build time, not internet-accessible in production. No fixed RPM for RHEL 9 (`end_of_life`).

### dotnet-host / CVE-2026-48779

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** Carried from prior scan (2026-08-07) — evidence unchanged. Trivy attributes CVE-2026-48779 (Node.js `ws` WebSocket DoS) to `dotnet-host`, which does not ship the `ws` npm package. This is a scan attribution artefact. The S2I builder is a build-time artifact, not a network-accessible service. Red Hat status is `under_investigation`; no fixed RPM available.

---

## Scan: MMRIA S2I @ ef00c008 — 2026-08-07

- **Commit:** `ef00c008ace2f269e270b5e124e51f21d6c66de2`
- **Service:** `MMRIA S2I`
- **Scan ID:** `31295`
- **Severity totals:** C:0  H:14  M:104
- **Scanned image:** `mmria/mmria-s2i:latest (redhat 9.8)`

> **Scope:** This scan block addresses Critical and High findings only, consistent with the
> automated remediation workflow. The 104 Medium findings are not triaged here; they are
> tracked by the scanning pipeline and addressed in a separate review cycle.

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 0 | 0 | 14 | 0 | 14 |

#### Finding inventory — HIGH

| Package | CVE | Installed | Fixed In | Status | Verdict |
|---|---|---|---|---|---|
| curl-minimal | CVE-2026-11352 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-11586 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-8286 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-8925 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| curl-minimal | CVE-2026-9547 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11352 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11586 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8286 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8925 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-9547 | 7.76.1-40.el9 | — | affected | Residual risk – no fix available |
| dotnet-host | CVE-2024-38081 | 10.0.10-1.el9_8 | — | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2025-26682 | 10.0.10-1.el9_8 | — | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2025-59144 | 10.0.10-1.el9_8 | — | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2026-48779 | 10.0.10-1.el9_8 | — | under_investigation | Residual risk – required, not reachable under current controls |

### Fixes made

`.s2i/dockerfile` — added `curl-minimal libcurl-minimal` to the `dnf update` layer so that package-manager fixes are applied automatically on the next image build as soon as the Red Hat RHEL-9 errata are published. No fixed version is available from Red Hat at scan time (`fixedIn` empty for all five CVEs); the dnf update layer ensures pick-up is automatic.

### HIGH / CRITICAL release analysis

#### curl-minimal and libcurl-minimal (10 findings)

All five CVEs affect the `curl-minimal` **and** `libcurl-minimal` packages (version `7.76.1-40.el9`) in the RHEL-9 base layer of the S2I builder image.

Red Hat Advisory status: all five CVEs have no published fixed RPM version for RHEL 9 at scan time — `fixedIn` is empty and status is `affected`. The `.s2i/dockerfile` now includes `dnf update -y curl-minimal libcurl-minimal` so the next image rebuild picks up any errata automatically.

| Package | CVE | Verdict |
|---|---|---|
| curl-minimal | CVE-2026-11352 | Residual risk – no fix available |
| curl-minimal | CVE-2026-11586 | Residual risk – no fix available |
| curl-minimal | CVE-2026-8286 | Residual risk – no fix available |
| curl-minimal | CVE-2026-8925 | Residual risk – no fix available |
| curl-minimal | CVE-2026-9547 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11352 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-11586 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8286 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-8925 | Residual risk – no fix available |
| libcurl-minimal | CVE-2026-9547 | Residual risk – no fix available |

### curl-minimal / libcurl-minimal — CVE-2026-11352

**Target:** mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not released a fixed RPM for `curl-minimal` / `libcurl-minimal`
at the time of this scan (`fixedIn` empty, status `affected`). NVD CVSS shows the
vulnerability requires the application to initiate an HTTP/3 QUIC connection to a
malicious server (AV:N/AC:H). MMRIA does not perform outbound HTTP/3 requests; all
external communication uses HTTPS/1.1 or HTTPS/2. The dnf update layer added in
`.s2i/dockerfile` will automatically apply the fix once an errata RPM is published.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11352` lists a fixed version.

#### dotnet-host (4 findings)

`dotnet-host` is the .NET runtime host binary, a required component of the MMRIA S2I
builder image. The S2I image is a build-time image used by OpenShift to compile the
.NET application source; it is not the production runtime image.

| CVE | Status | Verdict |
|---|---|---|
| CVE-2024-38081 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2025-26682 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2025-59144 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2026-48779 | under_investigation | Residual risk – required, not reachable under current controls |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-11352 exists at scan time. The finding affects the S2I builder image (not the runtime image). MMRIA makes no outbound HTTP/3 connections; NVD CVSS AV:N/AC:H requires the application to connect to a malicious HTTP/3 server. A `dnf update` layer is in place to apply the fix automatically when errata is released.

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-11586 exists at scan time. The vulnerability requires an active WebSocket connection to a malicious server that floods PING frames. MMRIA makes no outbound WebSocket connections from the S2I builder image. A `dnf update` layer in `.s2i/dockerfile` will apply the fix automatically once Red Hat publishes an errata for RHEL 9.

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-8286 exists at scan time. The vulnerability is a STARTTLS TLS-session reuse flaw. The S2I builder image uses curl only for build-time asset fetching; no STARTTLS paths are invoked during the MMRIA build. A `dnf update` layer in `.s2i/dockerfile` will apply the fix automatically once Red Hat publishes an errata for RHEL 9.

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-8925 exists at scan time. The vulnerability is a double-free in GSASL SASL authentication logic. MMRIA's S2I build process does not perform SASL-authenticated transfers. A `dnf update` layer in `.s2i/dockerfile` will apply the fix automatically once Red Hat publishes an errata for RHEL 9.

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-9547 exists at scan time. The vulnerability affects SCP/SFTP transfers via `CURLOPT_SSH_KEYFUNCTION`. MMRIA does not use SCP or SFTP in its S2I build pipeline. A `dnf update` layer in `.s2i/dockerfile` will apply the fix automatically once Red Hat publishes an errata for RHEL 9.

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-11352 exists at scan time for `libcurl-minimal` (the shared library co-installed with curl-minimal). The finding affects the S2I builder image (not the runtime image). NVD CVSS AV:N/AC:H requires the client to connect to a malicious HTTP/3 server; MMRIA makes no outbound HTTP/3 connections. A `dnf update` layer is in place in `.s2i/dockerfile`.

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-11586 exists at scan time for `libcurl-minimal`. The vulnerability requires an active WebSocket connection receiving malicious PING floods; MMRIA's S2I builder makes no outbound WebSocket connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-8286 exists at scan time for `libcurl-minimal`. The STARTTLS session-reuse flaw is not triggered during MMRIA's S2I build pipeline, which makes no STARTTLS connections. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-8925 exists at scan time for `libcurl-minimal`. The double-free GSASL defect requires SASL authentication, which is not used in the MMRIA S2I build pipeline. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** No Red Hat errata RPM for CVE-2026-9547 exists at scan time for `libcurl-minimal`. The SCP/SFTP SSH key callback flaw is not triggered because MMRIA's S2I build does not use SCP or SFTP transfers. A `dnf update` layer in `.s2i/dockerfile` ensures automatic remediation when errata is released.

### dotnet-host / CVE-2024-38081

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** `dotnet-host` is the mandatory .NET runtime host component of the S2I builder image; removal would break the build. CVE-2024-38081 is a .NET/Visual Studio Elevation of Privilege vulnerability (NVD CVSS AV:L/AC:L/PR:L — requires local access). The S2I builder runs as non-root (UID 1001) in an OpenShift-managed pod with no interactive shell access, eliminating the local privilege-escalation path. No fixed version is available from Red Hat for RHEL 9 (`fixedIn` empty, status `end_of_life`).

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** `dotnet-host` is a required component of the S2I builder image; removal is not possible. CVE-2025-26682 is an ASP.NET Core resource allocation DoS flaw (network-exploitable). The S2I builder image is a build-time artifact, not a web-facing service — no ASP.NET Core request pipeline is active in the builder context. No fixed version is available from Red Hat for RHEL 9 (`fixedIn` empty, status `end_of_life`).

### dotnet-host / CVE-2025-59144

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** Trivy attributes CVE-2025-59144 to `dotnet-host`, but the CVE description references the `debug` npm JavaScript package (supply-chain attack via a compromised npm account). `dotnet-host` does not ship or execute the `debug` npm package; this appears to be a Trivy scan attribution artefact from scanning bundled Node.js tooling present in the .NET SDK image. The .NET SDK builder image is used only at build time and is not internet-accessible in production. Verification command (Tier-2): `oc rsh <s2i-build-pod> find / -name 'debug' -path '*/node_modules/*' 2>/dev/null` to confirm presence or absence of the npm package in the image.

### dotnet-host / CVE-2026-48779

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** Trivy attributes CVE-2026-48779 to `dotnet-host`, but the CVE description references the `ws` Node.js WebSocket library (memory-exhaustion DoS). `dotnet-host` does not ship or execute the `ws` npm package; this is likely a Trivy scan attribution artefact from bundled Node.js files in the .NET SDK image. The S2I builder image is a build-time artifact, not a network-accessible service. Red Hat investigation is still `under_investigation` for RHEL 9; no fixed version is available. Verification command (Tier-2): `oc rsh <s2i-build-pod> find / -name 'ws' -path '*/node_modules/*' 2>/dev/null` to confirm presence or absence.
