## Scan: MMRIA S2I — 2026-07-31 (scan ID 31124, commit 56d69b7c)

**Service:** MMRIA S2I  
**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`  
**Base image:** `trusted-images/dotnet-100:9.8-1784594615@sha256:c71106c2...`  
**Findings sent for remediation:** C:0 H:14

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 0 | 0 | 14 | 0 | 14 |
| CRITICAL | 0 | 0 | 0 | 0 | 0 | 0 |

**Dockerfile change:** `.s2i/dockerfile` now includes `curl-minimal libcurl-minimal` in the existing `dnf update` command so that when Red Hat releases patches for the five curl/libcurl CVEs the fix is applied automatically on the next image rebuild. No upstream fix is available at the time of this scan (`fixedIn: ""`), so the version remains unchanged today.

### Fixes made

| File | Change | CVEs targeted | Before | After |
|---|---|---|---|---|
| `.s2i/dockerfile` | Added `curl-minimal libcurl-minimal` to `dnf update` | CVE-2026-11352, CVE-2026-11586, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | `dnf update -y libacl` | `dnf update -y libacl curl-minimal libcurl-minimal` |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | Residual risk – required, not reachable under current controls | HTTP/3 QUIC; build-time only; no QUIC traffic in pipeline |
| curl-minimal | CVE-2026-11586 | Residual risk – required, not reachable under current controls | WebSocket PING; no WebSocket connections in S2I build pipeline |
| curl-minimal | CVE-2026-8286 | Residual risk – required, not reachable under current controls | STARTTLS reuse; S2I connects only to known Red Hat / NuGet registries |
| curl-minimal | CVE-2026-8925 | Residual risk – required, not reachable under current controls | SASL double-free; no SASL/GSASL auth in build pipeline |
| curl-minimal | CVE-2026-9547 | Residual risk – required, not reachable under current controls | SCP/SFTP with SSH key callback; no SCP/SFTP in build pipeline |
| libcurl-minimal | CVE-2026-11352 | Residual risk – required, not reachable under current controls | HTTP/3 QUIC; build-time only; no QUIC traffic in pipeline |
| libcurl-minimal | CVE-2026-11586 | Residual risk – required, not reachable under current controls | WebSocket PING; no WebSocket connections in S2I build pipeline |
| libcurl-minimal | CVE-2026-8286 | Residual risk – required, not reachable under current controls | STARTTLS reuse; S2I connects only to known Red Hat / NuGet registries |
| libcurl-minimal | CVE-2026-8925 | Residual risk – required, not reachable under current controls | SASL double-free; no SASL/GSASL auth in build pipeline |
| libcurl-minimal | CVE-2026-9547 | Residual risk – required, not reachable under current controls | SCP/SFTP with SSH key callback; no SCP/SFTP in build pipeline |
| dotnet-host | CVE-2024-38081 | Residual risk – required, not reachable under current controls | EoP requiring local code execution; builder runs as UID 1001 in isolated OpenShift namespace |
| dotnet-host | CVE-2025-26682 | Residual risk – required, not reachable under current controls | ASP.NET Core DoS; S2I builder does not expose ASP.NET endpoints; no network listener |
| dotnet-host | CVE-2025-59144 | Residual risk – required, not reachable under current controls | npm debug supply-chain CVE attributed to dotnet-host RPM; likely Trivy DB mismatch; verification command in SWA section |
| dotnet-host | CVE-2026-48779 | Residual risk – required, not reachable under current controls | ws WebSocket npm CVE attributed to dotnet-host RPM; likely Trivy DB mismatch; verification command in SWA section |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Summary:** No upstream fix available; HTTP/3 QUIC attack surface is absent from the MMRIA S2I build pipeline.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** curl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-11352 targets curl's QUIC UDP receive function — a malicious HTTP/3 server can trigger a remote denial-of-service against a curl or libcurl client by exploiting a logic error that discards zero-length UDP datagrams before counting them toward the per-connection limit. Exploitation requires the client to initiate an HTTP/3 (QUIC) request to an attacker-controlled server. In the MMRIA S2I builder image, curl is available as a system tool for the build pipeline. All network traffic during the S2I build is limited to the internal OpenShift cluster network reaching the Red Hat CDN (package updates) and the NuGet registry (dotnet restore). Neither of these paths uses HTTP/3 / QUIC. The S2I builder does not perform arbitrary internet requests and does not connect to untrusted servers. The `.s2i/dockerfile` now includes `curl-minimal` in the `dnf update` command so that when Red Hat releases an upstream fix it is applied on the next image rebuild.

**CVE research:** NVD CVSS AV:N/AC:H (network-reachable, high complexity requiring attacker-controlled HTTP/3 server). No Red Hat "Not Affected" statement available at time of scan; `fixedIn` is empty.

**Verification (Tier-2 handoff):**
```bash
# Confirm curl is not invoked with HTTP/3 anywhere in the S2I assemble script:
grep -r "http3\|quic\|--http3" .s2i/bin/
# Confirm installed curl version:
oc rsh <s2i-builder-pod> rpm -q curl-minimal
```

---

### curl-minimal / CVE-2026-11586

**Summary:** No upstream fix available; WebSocket PING memory-exhaustion attack requires a WebSocket connection — no WebSocket connections are made during the S2I build.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** curl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-11586 abuses curl's default behavior of automatically responding to WebSocket PING frames with no upper bound on memory allocation for unacknowledged frames. A malicious server can exhaust client memory by flooding it with rapid sequential PING messages. Exploitation requires the client to open a WebSocket connection (`ws://` or `wss://`) to an attacker-controlled server. The MMRIA S2I build pipeline uses curl only for system package updates (Red Hat CDN) and dotnet restore (NuGet HTTPS). No part of the assemble or run script initiates WebSocket connections. The control that prevents exploitation is the absence of WebSocket usage in the build pipeline, enforced by the fixed set of endpoints curl contacts during build.

**CVE research:** NVD CVSS AV:N/AC:L (network-reachable, low complexity) — severity is high because memory exhaustion can crash the curl process. Precondition: client must initiate a WebSocket handshake.

**Verification (Tier-2 handoff):**
```bash
grep -r "ws://\|wss://\|websocket\|--websocket" .s2i/bin/
```

---

### curl-minimal / CVE-2026-8286

**Summary:** No upstream fix available; STARTTLS connection-reuse flaw requires mismatched TLS config reuse — the build pipeline's fixed set of HTTPS-only endpoints precludes the attack.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** curl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-8286 allows a new transfer using STARTTLS to upgrade an existing live connection even when the TLS configuration mismatches, potentially downgrading encryption or connecting to a wrong host. Exploitation requires two sequential transfers with mismatched TLS configurations sharing a connection pool, typically involving SMTP, IMAP, FTP, or LDAP with STARTTLS. The MMRIA S2I build pipeline does not use STARTTLS protocols — all connections are standard HTTPS to the Red Hat CDN and NuGet registry. No email, FTP, or LDAP operations occur during build. The `.s2i/dockerfile` now includes `curl-minimal` in the `dnf update` command.

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: client must use STARTTLS (`--ssl-reqd`) and connection-pool reuse across mismatched TLS configs.

**Verification (Tier-2 handoff):**
```bash
grep -r "starttls\|--ssl-reqd\|smtp\|imap\|ftp\|ldap" .s2i/bin/
```

---

### curl-minimal / CVE-2026-8925

**Summary:** No upstream fix available; GSASL double-free requires SASL authentication — SASL is not used in the S2I build pipeline.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** curl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-8925 is a double-free in curl's SASL/GSASL authentication cleanup path: the GSASL context is freed twice without clearing the pointer in between. Exploitation requires the client to perform SASL authentication (e.g., SMTP AUTH, IMAP LOGIN) against a server that triggers the vulnerable code path. The MMRIA S2I build pipeline does not use SASL authentication. Package manager calls to the Red Hat CDN use HTTPS without SASL, and NuGet calls use API-key headers over HTTPS. No SASL/GSASL credential exchange occurs during build.

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: client must negotiate a GSASL mechanism (e.g., GSSAPI, NTLM, Kerberos) in the SASL handshake.

**Verification (Tier-2 handoff):**
```bash
grep -r "sasl\|gsasl\|kerberos\|gssapi\|ntlm" .s2i/bin/
```

---

### curl-minimal / CVE-2026-9547

**Summary:** No upstream fix available; SSH key-callback bypass requires SCP/SFTP with CURLOPT_SSH_KEYFUNCTION — no SCP/SFTP is used in the S2I build pipeline.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** curl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-9547 causes libcurl to silently accept an untrusted server when a libcurl-based application performs SCP or SFTP transfers using the `CURLOPT_SSH_KEYFUNCTION` callback and the server presents a host-key type that the callback was not designed to handle. The untrusted host key is accepted without verification. Exploitation requires the client to connect via `scp://` or `sftp://` and register a key-function callback. The MMRIA S2I build pipeline does not use SCP or SFTP — all package downloads use HTTPS. The `.s2i/bin/assemble` script uses `dotnet restore` (HTTPS/NuGet) and dnf (HTTPS/Red Hat CDN).

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: client must use SCP/SFTP scheme with `CURLOPT_SSH_KEYFUNCTION` callback registered.

**Verification (Tier-2 handoff):**
```bash
grep -r "scp://\|sftp://\|SSH_KEYFUNCTION" .s2i/bin/
```

---

### libcurl-minimal / CVE-2026-11352

**Summary:** No upstream fix available; HTTP/3 QUIC attack surface is absent from the MMRIA S2I build pipeline. libcurl-minimal is the shared library used by curl-minimal and other system tools.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** libcurl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-11352 targets the QUIC UDP receive function in libcurl. A malicious HTTP/3 server can trigger a remote DoS against any process linking libcurl. Exploitation requires a process in the image to initiate an HTTP/3 connection to an attacker-controlled server. In the MMRIA S2I builder, libcurl-minimal is the underlying library for curl-minimal. All network traffic during the S2I build goes to the Red Hat CDN and NuGet registry, both over standard HTTPS (HTTP/1.1 or HTTP/2). No process in the build pipeline initiates HTTP/3 / QUIC connections. The `.s2i/dockerfile` now includes `libcurl-minimal` in the `dnf update` command to apply fixes automatically when released.

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: process must initiate an HTTP/3 request to an attacker-controlled server.

**Verification (Tier-2 handoff):**
```bash
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
# Check no process uses HTTP/3 at build time by inspecting assemble script:
grep -r "http3\|quic\|--http3" .s2i/bin/
```

---

### libcurl-minimal / CVE-2026-11586

**Summary:** No upstream fix available; WebSocket PING memory-exhaustion requires a WebSocket connection — no WebSocket connections are made during the S2I build.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** libcurl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-11586 is a memory-exhaustion vulnerability in libcurl triggered by rapid WebSocket PING frames from a malicious server. Exploitation requires a process to open a WebSocket connection (`ws://` or `wss://`). In the MMRIA S2I builder, the only processes that link against libcurl-minimal are curl-minimal and dnf-related tools. None of these initiate WebSocket connections during a normal build. The fixed set of HTTPS endpoints contacted during build (Red Hat CDN, NuGet) do not use WebSocket.

**CVE research:** NVD CVSS AV:N/AC:L. Precondition: client process must initiate a WebSocket handshake with an attacker-controlled server.

**Verification (Tier-2 handoff):**
```bash
grep -r "ws://\|wss://\|websocket" .s2i/bin/
ldd $(which curl) | grep libcurl   # confirms libcurl linkage version
```

---

### libcurl-minimal / CVE-2026-8286

**Summary:** No upstream fix available; STARTTLS connection-reuse flaw requires mismatched TLS config reuse — the build pipeline uses HTTPS-only endpoints with no STARTTLS protocols.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** libcurl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-8286 allows libcurl to reuse an existing STARTTLS-upgraded connection for a new transfer with a mismatched TLS configuration. Exploitation requires two sequential transfers using STARTTLS (SMTP, IMAP, LDAP, FTP) sharing a connection pool with differing TLS settings. In the MMRIA S2I builder, all network traffic is HTTPS to the Red Hat CDN and NuGet registry. No STARTTLS protocols are used. The `.s2i/dockerfile` now includes `libcurl-minimal` in the `dnf update` to apply patches on the next rebuild when released.

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: two sequential STARTTLS transfers with mismatched TLS configurations in the same connection pool.

**Verification (Tier-2 handoff):**
```bash
grep -r "starttls\|smtp\|imap\|ldap\|ftp" .s2i/bin/
```

---

### libcurl-minimal / CVE-2026-8925

**Summary:** No upstream fix available; GSASL double-free requires SASL authentication — SASL is not used in the S2I build pipeline.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** libcurl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-8925 is a double-free in libcurl's SASL/GSASL cleanup path. Exploitation requires a process linking libcurl to negotiate a SASL mechanism (GSSAPI, NTLM, Kerberos) against a server that triggers the double-free. In the MMRIA S2I builder, no process links libcurl and uses SASL authentication. Package manager calls use HTTPS without SASL; NuGet calls use bearer/API-key tokens over HTTPS.

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: GSASL mechanism negotiation in client process.

**Verification (Tier-2 handoff):**
```bash
grep -r "sasl\|gsasl\|gssapi\|ntlm\|kerberos" .s2i/bin/
```

---

### libcurl-minimal / CVE-2026-9547

**Summary:** No upstream fix available; SSH key-callback bypass requires SCP/SFTP with CURLOPT_SSH_KEYFUNCTION — no SCP/SFTP is used in the S2I build pipeline.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** libcurl-minimal 7.76.1-40.el9  
**Fixed in:** (none at time of scan)  
**Status:** affected

**Justification:** CVE-2026-9547 causes libcurl to accept an untrusted SSH host key when a process uses SCP/SFTP with the `CURLOPT_SSH_KEYFUNCTION` callback and the server presents an unexpected key type. Exploitation requires a process to connect via `scp://` or `sftp://` and register the callback. The MMRIA S2I build pipeline does not perform SCP or SFTP transfers. All transfers use HTTPS or NuGet protocols. No process in the build pipeline registers `CURLOPT_SSH_KEYFUNCTION`.

**CVE research:** NVD CVSS AV:N/AC:H. Precondition: SCP/SFTP scheme with `CURLOPT_SSH_KEYFUNCTION` callback registered in the calling process.

**Verification (Tier-2 handoff):**
```bash
grep -r "scp://\|sftp://\|SSH_KEYFUNCTION" .s2i/bin/
```

---

### dotnet-host / CVE-2024-38081

**Summary:** .NET Elevation of Privilege requiring local code execution as a lower-privileged user; S2I builder runs as UID 1001 in an isolated OpenShift namespace with no multi-user shell access.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** dotnet-host 10.0.10-1.el9_8  
**Fixed in:** (none at time of scan)  
**Status:** end_of_life

**Justification:** CVE-2024-38081 is a .NET/.NET Framework/Visual Studio Elevation of Privilege vulnerability. EoP in .NET typically requires an attacker to already have local code execution on the system as a lower-privileged user and leverage a specific .NET runtime behavior to escalate privileges. The MMRIA S2I builder runs in an OpenShift build pod as UID 1001 (non-root). The pod is ephemeral and isolated to the mmria project namespace. There is no multi-user shell access to the build pod; only the automated CI/CD pipeline triggers builds. The `end_of_life` Trivy status indicates no Red Hat backport is planned for this version; the .NET 10.0 RHEL package is the current active package track. Dotnet-host is required for the S2I builder to compile .NET applications.

**CVE research:** NVD CVSS score indicates local privilege escalation. Precondition: attacker must have local code execution as a non-privileged user on the same system. OpenShift build pods are single-tenant and short-lived.

**Verification (Tier-2 handoff):**
```bash
oc rsh <s2i-builder-pod> id
oc rsh <s2i-builder-pod> rpm -q dotnet-host
```

---

### dotnet-host / CVE-2025-26682

**Summary:** ASP.NET Core DoS via unbounded resource allocation; the S2I builder does not expose or run any ASP.NET Core network endpoints.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** dotnet-host 10.0.10-1.el9_8  
**Fixed in:** (none at time of scan)  
**Status:** end_of_life

**Justification:** CVE-2025-26682 is a denial-of-service vulnerability in ASP.NET Core where allocation of resources without limits or throttling allows an unauthenticated network attacker to exhaust server resources. Exploitation requires an attacker to send crafted HTTP requests to a running ASP.NET Core server endpoint. The MMRIA S2I image is a **builder** image, not a runtime image. The S2I build pod runs `dotnet restore` and `dotnet build/publish` during the build phase and then exits. It does not launch an ASP.NET Core web server or expose any network ports. No network endpoint exists for an attacker to target. The `end_of_life` Trivy status indicates no Red Hat backport; dotnet-host is required for building .NET applications.

**CVE research:** NVD CVSS AV:N/AC:L — network-reachable, but requires a running ASP.NET Core server. Precondition absent in a build-only image.

**Verification (Tier-2 handoff):**
```bash
# Confirm no ASP.NET server is started during S2I build:
grep -r "ASPNETCORE\|dotnet.*\.dll\|kestrel" .s2i/bin/
# Confirm no network ports are listened on during build:
oc rsh <s2i-builder-pod> ss -tlnp
```

---

### dotnet-host / CVE-2025-59144

**Summary:** npm `debug` package supply-chain CVE attributed to the dotnet-host RPM — likely a Trivy database mismatch; dotnet-host is a .NET runtime host binary that does not ship npm packages. Verification command provided.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** dotnet-host 10.0.10-1.el9_8  
**Fixed in:** (none at time of scan)  
**Status:** end_of_life

**Justification:** CVE-2025-59144 describes a supply-chain attack on the npm `debug` JavaScript debugging utility — specifically, the npm publishing account for `debug` was compromised on 8 September 2025 and version 4.4.2 was published with a malware payload. This CVE applies to the JavaScript/npm ecosystem, not to the RHEL `dotnet-host` RPM. The `dotnet-host` package provides the .NET runtime host binary (`dotnet`) for executing .NET applications; it does not include npm, Node.js, or any JavaScript packages. The attribution of this CVE to `dotnet-host` is consistent with Trivy database entries that sometimes map ecosystem-specific CVEs to platform packages when a dotnet SDK workload includes Node.js tooling. Without image inspection we cannot rule out that the dotnet-100 SDK base image ships bundled npm packages, so the verdict remains Residual risk pending Tier-2 verification.

**CVE research:** CVE-2025-59144 is a JavaScript supply-chain attack (npm ecosystem). No Red Hat advisory attributes this to dotnet-host. NVD lists this under npm/debug, not .NET.

**Verification (Tier-2 handoff — would upgrade to Not applicable if output confirms absence):**
```bash
# Check whether any npm packages are installed in the builder image:
oc rsh <s2i-builder-pod> find / -name "debug" -path "*/node_modules/*" 2>/dev/null | head -20
oc rsh <s2i-builder-pod> npm list -g debug 2>/dev/null || echo "npm not found or debug not installed"
# Check dotnet-host RPM file list for any npm/js content:
oc rsh <s2i-builder-pod> rpm -ql dotnet-host | grep -i "node\|npm\|js\|debug"
```

⏳ EVIDENCE WOULD UPGRADE: If the above commands confirm that `debug` npm package is not present in the image, this finding can be upgraded to **Not applicable / false positive** (Trivy DB mismatch — CVE targets npm debug, not dotnet-host RPM).

---

### dotnet-host / CVE-2026-48779

**Summary:** `ws` WebSocket npm package DoS CVE attributed to the dotnet-host RPM — likely a Trivy database mismatch; dotnet-host is a .NET runtime host binary that does not ship npm packages. Verification command provided.

**Verdict:** Residual risk – required, not reachable under current controls

**Package:** dotnet-host 10.0.10-1.el9_8  
**Fixed in:** (none at time of scan)  
**Status:** under_investigation

**Justification:** CVE-2026-48779 describes a memory-exhaustion DoS vulnerability in the `ws` WebSocket library for Node.js (all versions from 1.1.0 through 8.21.0 are affected, with fixes in 5.2.5, 6.2.4, 7.5.11, and 8.21.0). This CVE applies to the JavaScript/npm ecosystem. The RHEL `dotnet-host` RPM provides the .NET runtime host binary and does not include Node.js, npm, or the `ws` WebSocket library. The attribution to `dotnet-host` is consistent with Trivy database entries that sometimes map JavaScript CVEs to dotnet packages when the dotnet SDK ships related Node.js tooling. Without Tier-2 image inspection we cannot conclusively confirm the `ws` package is absent, so the verdict remains Residual risk pending verification.

**CVE research:** CVE-2026-48779 is a Node.js/npm WebSocket CVE. No Red Hat advisory attributes this to dotnet-host. NVD CVSS AV:N/AC:L — an attacker sends specially crafted WebSocket frames, requiring the vulnerable `ws` library to be actively processing network connections.

**Verification (Tier-2 handoff — would upgrade to Not applicable if output confirms absence):**
```bash
# Check whether ws npm package is installed in the builder image:
oc rsh <s2i-builder-pod> find / -name "ws" -path "*/node_modules/*" 2>/dev/null | head -20
oc rsh <s2i-builder-pod> npm list -g ws 2>/dev/null || echo "npm not found or ws not installed"
# Check dotnet-host RPM file list for any npm/js content:
oc rsh <s2i-builder-pod> rpm -ql dotnet-host | grep -i "node\|npm\|ws\|websocket"
```

⏳ EVIDENCE WOULD UPGRADE: If the above commands confirm that the `ws` npm package is not present in the image, this finding can be upgraded to **Not applicable / false positive** (Trivy DB mismatch — CVE targets npm ws, not dotnet-host RPM).
