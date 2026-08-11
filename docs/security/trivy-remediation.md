# Trivy Remediation Records

Records are prepended — newest scan block at the top.

---

## Scan: MMRIA Services @ 26b70afb — 2026-08-11

- **Commit:** `26b70afb1110cc18d8c5168c5c4e8b7951b3900c`
- **Service:** `MMRIA Services`
- **Scan ID:** `31320`
- **Severity totals:** C:0  H:14  M:103
- **Scanned image:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`

> **Scope:** This scan block addresses Critical and High findings only, consistent with the
> automated remediation workflow. The 103 Medium findings are not triaged here; they are
> tracked by the scanning pipeline and addressed in a separate review cycle.

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 0 | 10 | 4 | 0 | 14 |

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

`nccdphp-drh-mmria-services/mmria.services/Dockerfile` — extended the runtime-stage
`dnf update` layer to include `curl-minimal` and `libcurl-minimal` alongside the existing
`libacl` update. This ensures that errata RPMs for all five curl CVEs are automatically
applied on the next image rebuild once Red Hat publishes fixed packages for RHEL 9.
No fixed RPM version is available from Red Hat at scan time (`fixedIn` empty for all five
CVEs); the update layer guarantees automatic pick-up.

### HIGH / CRITICAL release analysis

#### curl-minimal and libcurl-minimal (10 findings)

All five CVEs affect `curl-minimal` and `libcurl-minimal` at version `7.76.1-40.el9` in
the RHEL-9 base layer of the services runtime image. Red Hat has not published a fixed RPM
for any of these CVEs at scan time — `fixedIn` is empty and status is `affected`. The
runtime-stage `dnf update` layer now includes both packages so the next image rebuild
automatically applies any errata as they become available.

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

#### dotnet-host (4 findings)

`dotnet-host` is the .NET runtime host binary, required by the MMRIA Services container
to execute the published .NET application. It cannot be removed without breaking the
service. The installed version `10.0.10-1.el9_8` is flagged `end_of_life` or
`under_investigation` by Trivy; no fixed RPM is available from Red Hat for RHEL 9 at
scan time.

| CVE | Status | Verdict |
|---|---|---|
| CVE-2024-38081 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2025-26682 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2025-59144 | end_of_life | Residual risk – required, not reachable under current controls |
| CVE-2026-48779 | under_investigation | Residual risk – required, not reachable under current controls |

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use HTTP/3 QUIC connections; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** curl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `curl-minimal` for RHEL 9 at scan
time (`fixedIn` empty, status `affected`). NVD CVSS for CVE-2026-11352 requires a client
to connect to a malicious HTTP/3 QUIC server (AV:N/AC:H). MMRIA Services does not initiate
any outbound HTTP/3 connections; all service communication uses HTTPS/1.1 or HTTPS/2 to
known internal endpoints. The `dnf update` layer in the Dockerfile runtime stage will
automatically apply the errata RPM once published by Red Hat.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11352` lists a fixed RHEL-9 version.

---

### curl-minimal / CVE-2026-11586

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use WebSocket connections as a client; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** curl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `curl-minimal` for RHEL 9 at scan
time (`fixedIn` empty, status `affected`). CVE-2026-11586 describes a memory exhaustion
attack via WebSocket PING frames that requires a WebSocket client connection to a malicious
server. MMRIA Services does not act as a WebSocket client via libcurl; the application
exposes HTTP REST endpoints and does not initiate WebSocket client sessions. The runtime
`dnf update` layer will automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11586` lists a fixed RHEL-9 version.

---

### curl-minimal / CVE-2026-8286

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use STARTTLS-upgraded connections; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** curl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `curl-minimal` for RHEL 9 at scan
time (`fixedIn` empty, status `affected`). CVE-2026-8286 describes incorrect TLS session
reuse when upgrading a connection with STARTTLS. MMRIA Services communicates with internal
services over already-encrypted HTTPS endpoints and does not use STARTTLS protocol
upgrades. The runtime `dnf update` layer will automatically apply the fix once Red Hat
publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8286` lists a fixed RHEL-9 version.

---

### curl-minimal / CVE-2026-8925

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use SASL/GSASL authentication; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** curl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `curl-minimal` for RHEL 9 at scan
time (`fixedIn` empty, status `affected`). CVE-2026-8925 is a double-free in curl's SASL
GSASL context cleanup path; exploitation requires an application to use SASL GSASL
authentication through libcurl. MMRIA Services does not configure or invoke SASL/GSASL
authentication in any of its service calls. The runtime `dnf update` layer will
automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8925` lists a fixed RHEL-9 version.

---

### curl-minimal / CVE-2026-9547

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use SCP/SFTP with SSH key callbacks; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** curl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `curl-minimal` for RHEL 9 at scan
time (`fixedIn` empty, status `affected`). CVE-2026-9547 requires a libcurl application
to use SCP:// or SFTP:// with a `CURLOPT_SSH_KEYFUNCTION` callback. MMRIA Services does
not perform SCP or SFTP transfers; all data exchange uses HTTPS REST APIs. The runtime
`dnf update` layer will automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-9547` lists a fixed RHEL-9 version.

---

### libcurl-minimal / CVE-2026-11352

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use HTTP/3 QUIC connections; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** libcurl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `libcurl-minimal` for RHEL 9 at
scan time (`fixedIn` empty, status `affected`). NVD CVSS for CVE-2026-11352 requires a
client to connect to a malicious HTTP/3 QUIC server (AV:N/AC:H). MMRIA Services does not
initiate any outbound HTTP/3 connections; all service communication uses HTTPS/1.1 or
HTTPS/2 to known internal endpoints. The `dnf update` layer in the Dockerfile runtime
stage will automatically apply the errata RPM once published by Red Hat.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11352` lists a fixed RHEL-9 version for libcurl-minimal.

---

### libcurl-minimal / CVE-2026-11586

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not initiate WebSocket client connections via libcurl; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** libcurl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `libcurl-minimal` for RHEL 9 at
scan time (`fixedIn` empty, status `affected`). CVE-2026-11586 requires a WebSocket client
connection to a malicious server in order to exhaust memory via unbounded PING frame
accumulation. MMRIA Services does not act as a WebSocket client via libcurl — it provides
HTTP REST endpoints and does not initiate WebSocket sessions. The runtime `dnf update`
layer will automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-11586` lists a fixed RHEL-9 version for libcurl-minimal.

---

### libcurl-minimal / CVE-2026-8286

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use STARTTLS upgrades; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** libcurl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `libcurl-minimal` for RHEL 9 at
scan time (`fixedIn` empty, status `affected`). CVE-2026-8286 exploits incorrect session
reuse when using STARTTLS to upgrade a connection to TLS. MMRIA Services communicates with
internal endpoints over pre-established HTTPS and does not use STARTTLS protocol upgrades.
The runtime `dnf update` layer will automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8286` lists a fixed RHEL-9 version for libcurl-minimal.

---

### libcurl-minimal / CVE-2026-8925

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use SASL GSASL authentication via libcurl; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** libcurl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `libcurl-minimal` for RHEL 9 at
scan time (`fixedIn` empty, status `affected`). CVE-2026-8925 is a double-free in libcurl's
SASL GSASL cleanup path triggered when an application uses SASL GSASL authentication.
MMRIA Services does not configure SASL or GSASL authentication in any libcurl call path.
The runtime `dnf update` layer will automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-8925` lists a fixed RHEL-9 version for libcurl-minimal.

---

### libcurl-minimal / CVE-2026-9547

**Summary:** No Red Hat RHEL-9 fix available; MMRIA Services does not use SCP/SFTP with SSH key callbacks; dnf update layer will auto-apply errata on next rebuild.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** libcurl-minimal 7.76.1-40.el9
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RPM for `libcurl-minimal` for RHEL 9 at
scan time (`fixedIn` empty, status `affected`). CVE-2026-9547 requires a libcurl client to
make SCP:// or SFTP:// transfers with a `CURLOPT_SSH_KEYFUNCTION` callback — conditions not
present in MMRIA Services, which exclusively uses HTTPS REST APIs. The runtime `dnf update`
layer will automatically apply the fix once Red Hat publishes it.

**Verification:** Rescan after next image rebuild once
`access.redhat.com/security/cve/CVE-2026-9547` lists a fixed RHEL-9 version for libcurl-minimal.

---

### dotnet-host / CVE-2024-38081

**Summary:** dotnet-host is required at runtime; no RHEL-9 fix available; privilege escalation precondition (interactive desktop session) absent in containerized OpenShift environment.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** dotnet-host 10.0.10-1.el9_8
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** `dotnet-host` is the .NET runtime host binary required for MMRIA Services to
execute. It cannot be removed without breaking the service. Trivy reports status
`end_of_life` with no published fixed RPM for RHEL 9 (`fixedIn` empty). NVD for
CVE-2024-38081 describes an Elevation of Privilege vulnerability in .NET, .NET Framework,
and Visual Studio with CVSS vector AV:L (local access required). Microsoft's advisory
(CVE-2024-38081) notes the flaw is in Windows Forms UI components — a Windows-specific
GUI path that does not exist in a Linux container or in the MMRIA Services ASP.NET Core
web service. The container runs non-root (UID 1001) in OpenShift with no local interactive
session, eliminating the privilege-escalation precondition.

**Verification:** Confirm non-root container identity: `oc rsh <pod> id`. Rescan after base
image update when Red Hat publishes a patched dotnet-host RPM for RHEL 9.

---

### dotnet-host / CVE-2025-26682

**Summary:** dotnet-host is required at runtime; no RHEL-9 fix available; ASP.NET Core DoS requires reaching the HTTP request path — network controls constrain external exposure.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** dotnet-host 10.0.10-1.el9_8
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** `dotnet-host` is required for MMRIA Services execution. Trivy reports status
`end_of_life` with `fixedIn` empty for RHEL 9. CVE-2025-26682 is an allocation-without-
limits DoS in ASP.NET Core triggered by an unauthorized attacker over the network (CVSS
AV:N). MMRIA Services is an internal API not directly exposed to the internet; network-level
controls (OpenShift route + namespace policies) limit inbound connections to authorized
internal consumers, reducing the likelihood of unauthenticated exploitation. No code fix is
possible without an updated dotnet-host RPM from Red Hat.

**Verification:** Confirm OpenShift network policy restricts ingress to authorized sources.
Rescan after base image update when Red Hat publishes a patched dotnet-host RPM for RHEL 9.

---

### dotnet-host / CVE-2025-59144

**Summary:** dotnet-host is required at runtime; CVE-2025-59144 describes a malicious npm package (debug 4.4.2) unrelated to the dotnet-host RPM in the container image; finding is a Trivy mis-attribution.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** dotnet-host 10.0.10-1.el9_8
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** CVE-2025-59144 describes a supply-chain compromise of the `debug` npm package
(v4.4.2 published with a malware payload after a phishing takeover of the npm account).
This CVE concerns a JavaScript npm package, not the `dotnet-host` RPM. Trivy has associated
the CVE with the `dotnet-host` package in RHEL 9 — this appears to be an erroneous
attribution. The MMRIA Services container does not install or run Node.js or the `debug` npm
package; it is a pure ASP.NET Core service. The `dotnet-host` RPM in the image is the
standard Red Hat-distributed .NET runtime and does not contain the malicious JavaScript code
described in CVE-2025-59144. No remediation action is possible on the dotnet-host RPM for
this CVE; the finding represents residual risk from the scanner's attribution.

**Verification:** Confirm no `node_modules` or `debug` npm package in the container:
`oc rsh <pod> find / -name "package.json" -path "*/debug/*" 2>/dev/null`. Rescan after Red
Hat updates its advisory database to clarify the dotnet-host attribution.

---

### dotnet-host / CVE-2026-48779

**Summary:** dotnet-host is required at runtime; CVE-2026-48779 describes a ws (Node.js WebSocket) memory exhaustion issue unrelated to the dotnet-host RPM; finding is a Trivy mis-attribution.

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Package:** dotnet-host 10.0.10-1.el9_8
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** CVE-2026-48779 describes a memory exhaustion DoS in the `ws` Node.js
WebSocket library (versions 1.1.0 through 8.x). This CVE concerns a Node.js npm package,
not the `dotnet-host` RPM. Trivy has associated the CVE with the `dotnet-host` package in
RHEL 9 at status `under_investigation` — this appears to be an erroneous attribution. The
MMRIA Services container is a pure ASP.NET Core service with no Node.js runtime or `ws` npm
package installed; `dotnet-host` is the Red Hat-distributed .NET host and does not contain
the Node.js `ws` library code. No remediation action is possible on the dotnet-host RPM for
this CVE.

**Verification:** Confirm no Node.js or `ws` npm package in the container:
`oc rsh <pod> node --version 2>/dev/null || echo "node not present"`. Rescan after Red Hat
updates its advisory database to clarify the dotnet-host attribution.

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
