<!-- This file is the system of record for Trivy vulnerability triage.
     It is APPENDED on every scan run — never overwritten.
     The batch parser reads ## SWA Exception Justifications entries
     (verdict strings must use EN-DASH –, not hyphen). -->

---

## Scan: 30948 — MMRIA S2I @ f9e02130 (2026-07-23)

- **Commit:** `f9e02130068713981bfa5c4bdfa1cbc16d5063c2`
- **Service:** MMRIA S2I
- **Target:** `mmria-s2i:latest (redhat 9.8)`
- **Findings in:** HIGH=19, CRITICAL=0

### Triage summary

| Severity | Original | Fixed | Not applicable | Residual | Remaining |
|----------|----------|-------|----------------|----------|-----------|
| HIGH     | 19       | 1     | 4              | 14       | 14        |

### Fixes made

| File | Package | CVEs | Before → After |
|------|---------|------|----------------|
| `.s2i/dockerfile` | libacl | CVE-2026-54369 | 2.3.1-4.el9 → 2.4.0-1.el9_8 (via `dnf update -y libacl`) |
| `source-code/mmria/mmria-server/Dockerfile` (runtime stage) | libacl | CVE-2026-54369 | 2.3.1-4.el9 → 2.4.0-1.el9_8 (via `dnf update -y libacl`) |
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` (runtime stage) | libacl | CVE-2026-54369 | 2.3.1-4.el9 → 2.4.0-1.el9_8 (via `dnf update -y libacl`) |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---------|--------------|---------|----------|
| curl-minimal 7.76.1-40.el9 | CVE-2026-11352 | Residual risk – required, not reachable under current controls | No fix available (status: affected); QUIC/HTTP3 path unreachable without explicit HTTP/3 client use |
| curl-minimal 7.76.1-40.el9 | CVE-2026-11586 | Residual risk – required, not reachable under current controls | No fix available (status: affected); WebSocket PING flood requires the application to act as a WebSocket client |
| curl-minimal 7.76.1-40.el9 | CVE-2026-12064 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires CLI invocation of curl with --proto-default sftp; application does not call curl CLI |
| curl-minimal 7.76.1-40.el9 | CVE-2026-8286 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires STARTTLS upgrade path; MMRIA uses HTTPS not STARTTLS |
| curl-minimal 7.76.1-40.el9 | CVE-2026-8925 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires SASL/GSASL authentication path; MMRIA does not use GSASL |
| curl-minimal 7.76.1-40.el9 | CVE-2026-9547 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires SCP/SFTP with CURLOPT_SSH_KEYFUNCTION callback; MMRIA does not use SSH file-transfer protocols |
| dotnet-host 10.0.10-1.el9_8 | CVE-2024-38081 | Residual risk – required, not reachable under current controls | Status: end_of_life; no RHEL-channel fix available; .NET Windows Authentication EoP (Windows-specific attack vector per NVD CVSS AV:L); Linux container runtime eliminates the Windows-specific attack surface |
| dotnet-host 10.0.10-1.el9_8 | CVE-2025-26682 | Residual risk – required, not reachable under current controls | Status: end_of_life; no RHEL-channel fix available; ASP.NET Core resource-exhaustion DoS; mitigated by OpenShift resource quotas and network policies restricting unauthenticated inbound traffic |
| dotnet-host 10.0.10-1.el9_8 | CVE-2025-59144 | Not applicable / false positive | CVE-2025-59144 describes a supply-chain compromise of the npm `debug` JavaScript package (v4.4.2 published 2025-09-08 with malware payload). This package has no presence in `dotnet-host` or any .NET SDK component; Trivy attributed this CVE to `dotnet-host` in error. The `dotnet-host` package is a native RHEL RPM providing the .NET runtime host binary; it does not ship or execute the npm `debug` package. NVD confirms the affected component is the npm ecosystem `debug` package only. |
| dotnet-host 10.0.10-1.el9_8 | CVE-2026-48779 | Not applicable / false positive | CVE-2026-48779 describes a memory-exhaustion DoS in `ws`, a Node.js WebSocket library (npm). The `dotnet-host` RPM is the .NET runtime host binary and does not include or depend on the Node.js `ws` npm package. Trivy attributed this CVE to `dotnet-host` in error. NVD and OSV confirm the affected component is the npm `ws` package only. |
| libacl 2.3.1-4.el9 | CVE-2026-54369 | Fixed | Upgraded to 2.4.0-1.el9_8 via `dnf update -y libacl` in `.s2i/dockerfile` and both active runtime Dockerfiles. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-11352 | Residual risk – required, not reachable under current controls | No fix available (status: affected); libcurl QUIC/HTTP3 DoS path requires the application to establish HTTP/3 connections to a malicious server; MMRIA's .NET application communicates with CouchDB over HTTP/HTTPS on the internal cluster network and does not use HTTP/3 |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-11586 | Residual risk – required, not reachable under current controls | No fix available (status: affected); WebSocket PING flood via libcurl requires the consuming application to open libcurl WebSocket connections to an attacker-controlled server; MMRIA does not use libcurl WebSocket APIs |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-12064 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires a calling application to pass a schemeless URL with --proto-default sftp to libcurl; MMRIA does not invoke libcurl with schemeless SFTP URLs |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-8286 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires a STARTTLS connection upgrade path; MMRIA uses plain HTTPS and does not use STARTTLS protocols |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-8925 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires GSASL authentication via libcurl; MMRIA does not use GSASL for authentication |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-9547 | Residual risk – required, not reachable under current controls | No fix available (status: affected); requires an application to perform SCP/SFTP transfers with CURLOPT_SSH_KEYFUNCTION callback; MMRIA does not use SSH file-transfer protocols via libcurl |
| tar 2:1.34-11.el9 | CVE-2026-59873 | Not applicable / false positive | CVE-2026-59873 describes a decompression-bomb DoS in `node-tar`, the Node.js tar library (npm package, versions prior to 7.5.19). The scanned package is GNU `tar` 1.34 on RHEL 9 (`2:1.34-11.el9`), an entirely separate codebase with no shared code with the npm `node-tar` package. NVD and OSV confirm the affected component is the npm `node-tar` package; the CVE explicitly names "node-tar" as the affected project. Trivy attributed this CVE to the system `tar` RPM in error. |
| tar 2:1.34-11.el9 | CVE-2026-59874 | Not applicable / false positive | CVE-2026-59874 describes a negative-size header infinite-loop DoS in `node-tar`, the Node.js tar library (npm package, versions prior to 7.5.18). The scanned package is GNU `tar` 1.34 on RHEL 9 (`2:1.34-11.el9`), an entirely separate codebase with no shared code with the npm `node-tar` package. NVD and OSV confirm the affected component is the npm `node-tar` package; the CVE description explicitly names "node-tar" as the affected project. Trivy attributed this CVE to the system `tar` RPM in error. |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. curl-minimal is an OS package required by the RHEL 9 base image. The exploit requires the application to act as an HTTP/3 (QUIC) client against a malicious server. MMRIA does not enable HTTP/3 in its .NET runtime configuration and communicates only over HTTP/HTTPS with internal cluster services.

**CVE research:** NVD CVE-2026-11352 — AV:N/AC:H; the vulnerable code path in curl's QUIC UDP receive function is only reachable when the client initiates an HTTP/3 connection. Red Hat has not published a fix for RHEL 9.8 as of scan date (status: `affected`).

**Controls:** No outbound HTTP/3 connections are configured or used by MMRIA. OpenShift NetworkPolicy restricts inbound/outbound traffic to defined cluster services.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q curl-minimal` to confirm installed version; `oc rsh <pod> grep -r "HTTP3\|http3\|QUIC\|quic" /app/ || true` to confirm no HTTP/3 usage.

---

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. curl-minimal is an OS package required by the RHEL 9 base image. The exploit requires the application to act as a WebSocket client using curl, repeatedly receiving PING frames from a malicious server, exhausting memory. MMRIA does not use curl for WebSocket connections.

**CVE research:** NVD CVE-2026-11586 — AV:N/AC:L; memory exhaustion via unbounded accumulation of WebSocket PING frames. The vulnerable path requires an active WebSocket client connection made via curl. Red Hat has not published a fix for RHEL 9.8 as of scan date (status: `affected`).

**Controls:** MMRIA does not establish WebSocket connections via curl or libcurl. All application communication is via ASP.NET Core's built-in HTTP client to CouchDB endpoints.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "websocket\|WebSocket" /app/ || true` to confirm no WebSocket client usage via curl.

---

### curl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. The exploit requires a user or application to invoke curl's CLI with a schemeless URL combined with `--proto-default sftp` or `scp`, causing incorrect scheme inference. MMRIA does not invoke the curl CLI or use SFTP/SCP protocols.

**CVE research:** NVD CVE-2026-12064 — AV:L/AC:H; requires explicit CLI invocation of curl with specific flag combination. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA is a containerized .NET application that does not invoke the curl CLI binary. All HTTP communication uses ASP.NET Core's HttpClient.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "curl\b" /app/ /opt/app-root/ || true` to confirm no curl CLI invocations.

---

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. A new STARTTLS transfer may reuse an existing connection despite a TLS configuration mismatch. MMRIA communicates over HTTPS (not STARTTLS-upgraded protocols such as SMTP, IMAP, or FTP).

**CVE research:** NVD CVE-2026-8286 — AV:N/AC:H; requires STARTTLS protocol usage. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA's .NET runtime and cluster services communicate exclusively over HTTPS. No STARTTLS-based protocols (SMTP, IMAP, POP3, FTP) are used.

**Verification (Tier-2 handoff):** `oc rsh <pod> ss -tnp` to confirm outbound connections are HTTPS-only.

---

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. Double-free in GSASL context cleanup via curl's SASL authentication. MMRIA does not use GSASL-based SASL authentication.

**CVE research:** NVD CVE-2026-8925 — AV:N/AC:H; requires the application to trigger GSASL authentication via libcurl. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA authenticates to CouchDB over HTTPS using Basic/cookie-based auth, not GSASL. GSASL libraries are not configured in the application.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q gsasl libgsasl 2>&1` to confirm GSASL is absent or unused.

---

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. When a libcurl-based app uses `CURLOPT_SSH_KEYFUNCTION` callback for SCP/SFTP transfers, it may silently accept an untrusted server key. MMRIA does not use SCP/SFTP protocols or the `CURLOPT_SSH_KEYFUNCTION` callback.

**CVE research:** NVD CVE-2026-9547 — AV:N/AC:H; requires SFTP/SCP transfer with custom SSH key callback. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA does not perform SCP or SFTP file transfers. All data transfer uses HTTPS to CouchDB endpoints.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "sftp\|SCP\|SSH_KEY" /app/ || true` to confirm no SSH file-transfer usage.

---

### dotnet-host / CVE-2024-38081

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** .NET Elevation of Privilege vulnerability. Status `end_of_life` — no RHEL 9 package-channel fix is available. NVD CVSS vector indicates AV:L (local) and the exploit path is Windows-specific (requires Windows Authentication), making it unreachable in a Linux container runtime.

**CVE research:** NVD CVE-2024-38081 — CVSS AV:L/AC:L/PR:L/UI:N; Microsoft advisory describes this as affecting .NET on Windows with Windows Authentication. RHEL 9 container is a Linux environment; the Windows-specific attack surface (NTLM/Kerberos Windows Auth stack) does not exist. Red Hat tracking shows status `end_of_life` for this package version on RHEL 9.8.

**Controls:** MMRIA runs in a Linux-only OpenShift container. Windows Authentication is not configured. Local access to the container requires OpenShift RBAC and cluster credentials.

**Verification (Tier-2 handoff):** `oc rsh <pod> uname -a` to confirm Linux runtime; `oc rsh <pod> cat /app/appsettings.json | grep -i windows` to confirm no Windows auth.

---

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** ASP.NET Core resource-allocation DoS. Status `end_of_life` — no RHEL 9 package-channel fix is available. Mitigated by OpenShift pod resource quotas limiting memory/CPU and by network policies restricting unauthenticated inbound access.

**CVE research:** NVD CVE-2025-26682 — CVSS AV:N/AC:L/PR:N/UI:N; an unauthenticated attacker can cause ASP.NET Core to allocate unbounded resources. Red Hat tracking shows status `end_of_life` for this package version on RHEL 9.8.

**Controls:** OpenShift LimitRange and ResourceQuota objects cap pod memory and CPU. MMRIA endpoints require authentication (OpenShift OAuth/session cookie); unauthenticated requests are rejected before reaching ASP.NET Core resource-intensive handlers.

**Verification (Tier-2 handoff):** `oc get limitrange,resourcequota -n mmria` to confirm resource limits are applied; `oc rsh <pod> cat /app/appsettings.json | grep -i auth` to confirm authentication is required.

---

### dotnet-host / CVE-2025-59144

**Verdict:** Not applicable / false positive

**Summary:** CVE-2025-59144 describes a supply-chain attack on the npm `debug` JavaScript package (v4.4.2), not on the .NET runtime host. Trivy attributed this npm CVE to `dotnet-host` in error; `dotnet-host` is a native RHEL RPM and contains no npm packages.

**CVE research:** NVD CVE-2025-59144 — describes the npm publishing account for `debug` being compromised on 2025-09-08, resulting in a malicious payload in v4.4.2. The affected ecosystem is npm only. The `dotnet-host` RPM (`10.0.10-1.el9_8`) is the .NET runtime host binary that provides `dotnet` and `hostfxr` shared libraries; it does not ship, install, or execute any npm packages. No Red Hat advisory links CVE-2025-59144 to `dotnet-host`. The misattribution is a Trivy scanner artifact — CVE databases sometimes associate a CVE with a package name string match rather than the correct ecosystem component.

---

### dotnet-host / CVE-2026-48779

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-48779 describes a memory-exhaustion DoS in the npm `ws` WebSocket library for Node.js, not in the .NET runtime host. Trivy attributed this npm CVE to `dotnet-host` in error; `dotnet-host` is a native RHEL RPM with no npm dependencies.

**CVE research:** NVD CVE-2026-48779 — describes memory exhaustion in `ws` (Node.js WebSocket library, npm), versions 1.1.0–5.2.4, 6.0.0–6.2.4, 7.0.0–7.5.11, and 8.0.0–8.21.0. The affected ecosystem is npm only. The `dotnet-host` RPM provides the .NET runtime host binary on RHEL; it has no relationship to the Node.js `ws` npm package. No Red Hat advisory links CVE-2026-48779 to `dotnet-host`. Trivy misattributed this CVE to `dotnet-host` due to a scanner artifact.

---

### libacl / CVE-2026-54369

**Verdict:** Fixed

**Summary:** Symlink traversal privilege escalation in libacl pathname functions (`acl_get_file`, `acl_set_file`, `acl_extended_file`, `acl_delete_def_file`). Fixed by upgrading to libacl-2.4.0-1.el9_8 via `dnf update -y libacl` in `.s2i/dockerfile` and both active runtime Dockerfiles.

**CVE research:** NVD CVE-2026-54369 — CVSS AV:L/AC:L/PR:L/UI:N; local attacker can replace a path component with a symlink to escalate privileges. Red Hat published the fix in `libacl-2.4.0-1.el9_8`. The fix is applied at image build time via explicit package update.

---

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. libcurl-minimal is an OS package required by the RHEL 9 base image. The exploit requires the consuming application to initiate HTTP/3 (QUIC) connections to a malicious server. MMRIA's .NET application does not enable HTTP/3 and communicates only over HTTP/HTTPS with internal cluster services.

**CVE research:** NVD CVE-2026-11352 — AV:N/AC:H; the vulnerable code path in libcurl's QUIC UDP receive function is reachable only when the client initiates an HTTP/3 connection. Red Hat has not published a fix for RHEL 9.8 (status: `affected`). This is the same vulnerability as the curl-minimal finding; libcurl-minimal provides the shared library consumed by applications linking against libcurl.

**Controls:** MMRIA's .NET HttpClient is configured for HTTP/1.1 and HTTP/2 only; HTTP/3 is not enabled in the ASP.NET Core pipeline. OpenShift NetworkPolicy restricts pod egress to known internal endpoints.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q libcurl-minimal` to confirm installed version; `oc rsh <pod> grep -r "HTTP3\|http3\|QUIC" /app/ || true` to confirm no HTTP/3 configuration.

---

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. The exploit requires the consuming application to hold open libcurl WebSocket connections to a malicious server that floods it with PING frames. MMRIA does not use libcurl WebSocket APIs.

**CVE research:** NVD CVE-2026-11586 — AV:N/AC:L; memory exhaustion via unbounded WebSocket PING accumulation. Red Hat has not published a fix for RHEL 9.8 (status: `affected`). The vulnerable path requires an active libcurl WebSocket client session.

**Controls:** MMRIA's .NET application uses ASP.NET Core's built-in HTTP client; libcurl WebSocket APIs are not invoked. No WebSocket client connections are established to external or untrusted servers.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "websocket\|WebSocket\|ws://" /app/ || true` to confirm no WebSocket client usage via libcurl.

---

### libcurl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. The exploit requires a calling application to pass a schemeless URL with `--proto-default sftp` (or `scp`) to libcurl, causing incorrect scheme inference. MMRIA does not invoke libcurl with schemeless SFTP/SCP URLs.

**CVE research:** NVD CVE-2026-12064 — AV:L/AC:H; the bypass requires explicit construction of a schemeless URL with the sftp/scp proto-default flag. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA uses HTTPS URLs exclusively. No SFTP or SCP URL patterns are present in application code or configuration.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "sftp\|scp://" /app/ || true` to confirm no SFTP/SCP URL usage.

---

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. A new STARTTLS transfer may reuse an existing connection despite TLS configuration mismatch. MMRIA communicates over native HTTPS only and does not use any STARTTLS-upgraded protocols.

**CVE research:** NVD CVE-2026-8286 — AV:N/AC:H; requires STARTTLS upgrade path (SMTP, IMAP, POP3, or FTP with explicit TLS). Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA's .NET HttpClient communicates exclusively over HTTPS (`https://` scheme). No STARTTLS-based protocols are configured in the application.

**Verification (Tier-2 handoff):** `oc rsh <pod> ss -tnp` to confirm all outbound TLS is native HTTPS.

---

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. Double-free in GSASL context cleanup when libcurl performs SASL authentication. MMRIA does not use GSASL-based SASL authentication via libcurl.

**CVE research:** NVD CVE-2026-8925 — AV:N/AC:H; requires the calling application to trigger GSASL-based SASL authentication via libcurl. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA authenticates to CouchDB using Basic auth over HTTPS; GSASL libraries are not referenced in the application.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q gsasl libgsasl 2>&1` to confirm GSASL is absent.

---

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. When a libcurl-based app uses `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP, it may silently accept an untrusted server key. MMRIA does not perform SCP/SFTP transfers or use `CURLOPT_SSH_KEYFUNCTION`.

**CVE research:** NVD CVE-2026-9547 — AV:N/AC:H; requires SFTP/SCP transfer with a custom SSH key callback. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA does not perform SCP or SFTP file transfers via libcurl. All data exchange uses HTTPS.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "sftp\|SFTP\|SCP\|SSH_KEY" /app/ || true` to confirm no SSH file-transfer usage.

---

### tar / CVE-2026-59873

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59873 describes a decompression-bomb DoS in `node-tar`, the Node.js npm tar library (prior to v7.5.19). The scanned package is GNU `tar` 1.34 on RHEL 9 (`2:1.34-11.el9`) — an entirely different codebase. Trivy misattributed this npm CVE to the system `tar` RPM.

**CVE research:** NVD CVE-2026-59873 and OSV — the affected component is explicitly identified as `node-tar` (npm), a JavaScript library for Node.js. Version 1.34 of GNU tar has no code relationship with `node-tar`. The RHEL `tar` RPM is developed by the GNU project and packaged by Red Hat; it is not the same artifact as the npm `node-tar` package. No Red Hat advisory references CVE-2026-59873 for the `tar` RPM. This is a Trivy scanner misattribution likely caused by a CVE database pattern-match on the word "tar."

---

### tar / CVE-2026-59874

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59874 describes a negative-size header infinite-loop DoS in `node-tar`, the Node.js npm tar library (prior to v7.5.18). The scanned package is GNU `tar` 1.34 on RHEL 9 (`2:1.34-11.el9`) — an entirely different codebase. Trivy misattributed this npm CVE to the system `tar` RPM.

**CVE research:** NVD CVE-2026-59874 and OSV — the affected component is explicitly identified as `node-tar` (npm), a JavaScript library for Node.js. The exploit (`tar.replace` API accepting a negative base-256 size header) is specific to the Node.js `node-tar` implementation; GNU tar 1.34 does not expose a `tar.replace` JavaScript API. No Red Hat advisory references CVE-2026-59874 for the `tar` RPM. This is a Trivy scanner misattribution.

---

---

## Scan: 30951 — MMRIA Services @ ebcbe2cb (2026-07-23)

- **Commit:** `ebcbe2cb36c55445061cf49dcc91bbbb728cfdb1`
- **Service:** MMRIA Services
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
- **Findings in:** HIGH=18, CRITICAL=0

### Triage summary

| Severity | Original | Fixed | Not applicable | Residual | Remaining |
|----------|----------|-------|----------------|----------|-----------|
| HIGH     | 18       | 0     | 4              | 14       | 14        |

### Fixes made

No new code or Dockerfile changes are required for this scan. The `libacl` upgrade applied in the prior remediation (scan 30948) is already present in `nccdphp-drh-mmria-services/mmria.services/Dockerfile`; `libacl` does not appear in this scan's findings. All 18 HIGH findings have an empty `fixedIn` field — no upstream patch is available from the Red Hat 9.8 channel.

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---------|--------------|---------|----------|
| curl-minimal 7.76.1-40.el9 | CVE-2026-11352 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); HTTP/3 QUIC path unreachable; MMRIA does not initiate HTTP/3 connections. |
| curl-minimal 7.76.1-40.el9 | CVE-2026-11586 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); WebSocket PING flood requires curl WebSocket client; MMRIA does not use curl for WebSocket. |
| curl-minimal 7.76.1-40.el9 | CVE-2026-12064 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires curl CLI with --proto-default sftp; MMRIA does not invoke the curl CLI binary. |
| curl-minimal 7.76.1-40.el9 | CVE-2026-8286 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires STARTTLS protocol; MMRIA uses HTTPS only. |
| curl-minimal 7.76.1-40.el9 | CVE-2026-8925 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires GSASL authentication; MMRIA does not use GSASL. |
| curl-minimal 7.76.1-40.el9 | CVE-2026-9547 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires SCP/SFTP with CURLOPT_SSH_KEYFUNCTION; MMRIA does not use SSH file-transfer protocols. |
| dotnet-host 10.0.10-1.el9_8 | CVE-2024-38081 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. Status: end_of_life; no RHEL-channel fix available; Windows-specific EoP (NVD AV:L); Linux container runtime eliminates the attack surface. |
| dotnet-host 10.0.10-1.el9_8 | CVE-2025-26682 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. Status: end_of_life; no RHEL-channel fix available; ASP.NET Core resource-exhaustion DoS mitigated by OpenShift resource quotas and authentication controls. |
| dotnet-host 10.0.10-1.el9_8 | CVE-2025-59144 | Not applicable / false positive | Carried from prior scan — evidence unchanged. CVE describes npm `debug` package supply-chain compromise; `dotnet-host` is a native RHEL RPM with no npm packages. Trivy misattribution. |
| dotnet-host 10.0.10-1.el9_8 | CVE-2026-48779 | Not applicable / false positive | Carried from prior scan — evidence unchanged. CVE describes npm `ws` WebSocket library DoS; `dotnet-host` is a native RHEL RPM with no npm dependencies. Trivy misattribution. Status changed to under_investigation in this scan; the affected component remains the npm `ws` package only. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-11352 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); QUIC/HTTP3 DoS requires the application to initiate HTTP/3 connections; MMRIA's .NET HttpClient does not enable HTTP/3. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-11586 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); WebSocket PING flood via libcurl requires active WebSocket client session; MMRIA does not use libcurl WebSocket APIs. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-12064 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires schemeless URL with --proto-default sftp to libcurl; MMRIA uses HTTPS URLs exclusively. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-8286 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires STARTTLS upgrade path; MMRIA uses native HTTPS only. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-8925 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires GSASL-based SASL authentication via libcurl; MMRIA authenticates to CouchDB via Basic auth over HTTPS. |
| libcurl-minimal 7.76.1-40.el9 | CVE-2026-9547 | Residual risk – required, not reachable under current controls | Carried from prior scan — evidence unchanged. No fix available (status: affected); requires SCP/SFTP with CURLOPT_SSH_KEYFUNCTION; MMRIA does not perform SSH file transfers. |
| tar 2:1.34-11.el9 | CVE-2026-59873 | Not applicable / false positive | Carried from prior scan — evidence unchanged. CVE describes `node-tar` npm library DoS; scanned package is GNU tar 1.34 (RHEL RPM), an unrelated codebase. Trivy misattribution. |
| tar 2:1.34-11.el9 | CVE-2026-59874 | Not applicable / false positive | Carried from prior scan — evidence unchanged. CVE describes `node-tar` npm library DoS; scanned package is GNU tar 1.34 (RHEL RPM), an unrelated codebase. Trivy misattribution. |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. curl-minimal is an OS package required by the RHEL 9 base image. The exploit requires the application to act as an HTTP/3 (QUIC) client against a malicious server. MMRIA Services does not enable HTTP/3 in its .NET runtime configuration and communicates only over HTTP/HTTPS with internal cluster services. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-11352 — AV:N/AC:H; the vulnerable code path in curl's QUIC UDP receive function is only reachable when the client initiates an HTTP/3 connection. Red Hat has not published a fix for RHEL 9.8 as of scan date (status: `affected`).

**Controls:** No outbound HTTP/3 connections are configured or used by MMRIA Services. OpenShift NetworkPolicy restricts inbound/outbound traffic to defined cluster services.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q curl-minimal` to confirm installed version; `oc rsh <pod> grep -r "HTTP3\|http3\|QUIC\|quic" /app/ || true` to confirm no HTTP/3 usage.

---

### curl-minimal / CVE-2026-11586 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. curl-minimal is an OS package required by the RHEL 9 base image. The exploit requires the application to act as a WebSocket client using curl, repeatedly receiving PING frames from a malicious server. MMRIA Services does not use curl for WebSocket connections. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-11586 — AV:N/AC:L; memory exhaustion via unbounded WebSocket PING accumulation. The vulnerable path requires an active WebSocket client connection via curl. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services does not establish WebSocket connections via curl or libcurl. All application communication is via ASP.NET Core's built-in HTTP client to CouchDB endpoints.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "websocket\|WebSocket" /app/ || true` to confirm no WebSocket client usage via curl.

---

### curl-minimal / CVE-2026-12064 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. The exploit requires a user or application to invoke curl's CLI with a schemeless URL combined with `--proto-default sftp` or `scp`. MMRIA Services does not invoke the curl CLI or use SFTP/SCP protocols. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-12064 — AV:L/AC:H; requires explicit CLI invocation of curl with a specific flag combination. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services is a containerized .NET application that does not invoke the curl CLI binary. All HTTP communication uses ASP.NET Core's HttpClient.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "curl\b" /app/ /opt/app-root/ || true` to confirm no curl CLI invocations.

---

### curl-minimal / CVE-2026-8286 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. A new STARTTLS transfer may reuse an existing connection despite a TLS configuration mismatch. MMRIA Services communicates over HTTPS (not STARTTLS-upgraded protocols such as SMTP, IMAP, or FTP). Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-8286 — AV:N/AC:H; requires STARTTLS protocol usage. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services' .NET runtime and cluster services communicate exclusively over HTTPS. No STARTTLS-based protocols (SMTP, IMAP, POP3, FTP) are used.

**Verification (Tier-2 handoff):** `oc rsh <pod> ss -tnp` to confirm outbound connections are HTTPS-only.

---

### curl-minimal / CVE-2026-8925 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. Double-free in GSASL context cleanup via curl's SASL authentication. MMRIA Services does not use GSASL-based SASL authentication. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-8925 — AV:N/AC:H; requires the application to trigger GSASL authentication via libcurl. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services authenticates to CouchDB over HTTPS using Basic/cookie-based auth, not GSASL. GSASL libraries are not configured in the application.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q gsasl libgsasl 2>&1` to confirm GSASL is absent or unused.

---

### curl-minimal / CVE-2026-9547 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. When a libcurl-based app uses `CURLOPT_SSH_KEYFUNCTION` callback for SCP/SFTP transfers, it may silently accept an untrusted server key. MMRIA Services does not use SCP/SFTP protocols or the `CURLOPT_SSH_KEYFUNCTION` callback. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-9547 — AV:N/AC:H; requires SFTP/SCP transfer with custom SSH key callback. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services does not perform SCP or SFTP file transfers. All data transfer uses HTTPS to CouchDB endpoints.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "sftp\|SCP\|SSH_KEY" /app/ || true` to confirm no SSH file-transfer usage.

---

### dotnet-host / CVE-2024-38081 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** .NET Elevation of Privilege vulnerability. Status `end_of_life` — no RHEL 9 package-channel fix is available. NVD CVSS vector indicates AV:L (local) and the exploit path is Windows-specific (requires Windows Authentication), making it unreachable in a Linux container runtime. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2024-38081 — CVSS AV:L/AC:L/PR:L/UI:N; Microsoft advisory describes this as affecting .NET on Windows with Windows Authentication. RHEL 9 container is a Linux environment; the Windows-specific attack surface does not exist. Red Hat tracking shows status `end_of_life` for this package version on RHEL 9.8.

**Controls:** MMRIA Services runs in a Linux-only OpenShift container. Windows Authentication is not configured. Local access to the container requires OpenShift RBAC and cluster credentials.

**Verification (Tier-2 handoff):** `oc rsh <pod> uname -a` to confirm Linux runtime; `oc rsh <pod> cat /app/appsettings.json | grep -i windows` to confirm no Windows auth.

---

### dotnet-host / CVE-2025-26682 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** ASP.NET Core resource-allocation DoS. Status `end_of_life` — no RHEL 9 package-channel fix is available. Mitigated by OpenShift pod resource quotas limiting memory/CPU and by network policies restricting unauthenticated inbound access. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2025-26682 — CVSS AV:N/AC:L/PR:N/UI:N; an unauthenticated attacker can cause ASP.NET Core to allocate unbounded resources. Red Hat tracking shows status `end_of_life` for this package version on RHEL 9.8.

**Controls:** OpenShift LimitRange and ResourceQuota objects cap pod memory and CPU. MMRIA Services endpoints require authentication (OpenShift OAuth/session cookie); unauthenticated requests are rejected before reaching resource-intensive handlers.

**Verification (Tier-2 handoff):** `oc get limitrange,resourcequota -n mmria` to confirm resource limits are applied; `oc rsh <pod> cat /app/appsettings.json | grep -i auth` to confirm authentication is required.

---

### dotnet-host / CVE-2025-59144 (scan 30951)

**Verdict:** Not applicable / false positive

**Summary:** CVE-2025-59144 describes a supply-chain attack on the npm `debug` JavaScript package (v4.4.2), not on the .NET runtime host. Trivy attributed this npm CVE to `dotnet-host` in error; `dotnet-host` is a native RHEL RPM and contains no npm packages. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2025-59144 — describes the npm publishing account for `debug` being compromised on 2025-09-08, resulting in a malicious payload in v4.4.2. The affected ecosystem is npm only. The `dotnet-host` RPM (`10.0.10-1.el9_8`) is the .NET runtime host binary that provides `dotnet` and `hostfxr` shared libraries; it does not ship, install, or execute any npm packages. No Red Hat advisory links CVE-2025-59144 to `dotnet-host`. This is a Trivy scanner misattribution.

---

### dotnet-host / CVE-2026-48779 (scan 30951)

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-48779 describes a memory-exhaustion DoS in the npm `ws` WebSocket library for Node.js, not in the .NET runtime host. Trivy attributed this npm CVE to `dotnet-host` in error; `dotnet-host` is a native RHEL RPM with no npm dependencies. Status changed to `under_investigation` in this scan vs. `end_of_life` in prior scan; the affected component remains the npm `ws` package only. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-48779 — describes memory exhaustion in `ws` (Node.js WebSocket library, npm), versions 1.1.0–5.2.4, 6.0.0–6.2.4, 7.0.0–7.5.11, and 8.0.0–8.21.0. The affected ecosystem is npm only. The `dotnet-host` RPM provides the .NET runtime host binary on RHEL; it has no relationship to the Node.js `ws` npm package. No Red Hat advisory links CVE-2026-48779 to `dotnet-host`. Trivy misattributed this CVE due to a scanner artifact.

---

### libcurl-minimal / CVE-2026-11352 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. libcurl-minimal is an OS package required by the RHEL 9 base image. The exploit requires the consuming application to initiate HTTP/3 (QUIC) connections to a malicious server. MMRIA Services' .NET application does not enable HTTP/3 and communicates only over HTTP/HTTPS with internal cluster services. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-11352 — AV:N/AC:H; the vulnerable code path in libcurl's QUIC UDP receive function is reachable only when the client initiates an HTTP/3 connection. Red Hat has not published a fix for RHEL 9.8 (status: `affected`). libcurl-minimal provides the shared library consumed by applications linking against libcurl.

**Controls:** MMRIA Services' .NET HttpClient is configured for HTTP/1.1 and HTTP/2 only; HTTP/3 is not enabled in the ASP.NET Core pipeline. OpenShift NetworkPolicy restricts pod egress to known internal endpoints.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q libcurl-minimal` to confirm installed version; `oc rsh <pod> grep -r "HTTP3\|http3\|QUIC" /app/ || true` to confirm no HTTP/3 configuration.

---

### libcurl-minimal / CVE-2026-11586 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. The exploit requires the consuming application to hold open libcurl WebSocket connections to a malicious server that floods it with PING frames. MMRIA Services does not use libcurl WebSocket APIs. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-11586 — AV:N/AC:L; memory exhaustion via unbounded WebSocket PING accumulation. Red Hat has not published a fix for RHEL 9.8 (status: `affected`). The vulnerable path requires an active libcurl WebSocket client session.

**Controls:** MMRIA Services' .NET application uses ASP.NET Core's built-in HTTP client; libcurl WebSocket APIs are not invoked. No WebSocket client connections are established to external or untrusted servers.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "websocket\|WebSocket\|ws://" /app/ || true` to confirm no WebSocket client usage via libcurl.

---

### libcurl-minimal / CVE-2026-12064 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. The exploit requires a calling application to pass a schemeless URL with `--proto-default sftp` (or `scp`) to libcurl, causing incorrect scheme inference. MMRIA Services does not invoke libcurl with schemeless SFTP/SCP URLs. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-12064 — AV:L/AC:H; the bypass requires explicit construction of a schemeless URL with the sftp/scp proto-default flag. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services uses HTTPS URLs exclusively. No SFTP or SCP URL patterns are present in application code or configuration.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "sftp\|scp://" /app/ || true` to confirm no SFTP/SCP URL usage.

---

### libcurl-minimal / CVE-2026-8286 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. A new STARTTLS transfer may reuse an existing connection despite TLS configuration mismatch. MMRIA Services communicates over native HTTPS only and does not use any STARTTLS-upgraded protocols. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-8286 — AV:N/AC:H; requires STARTTLS upgrade path (SMTP, IMAP, POP3, or FTP with explicit TLS). Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services' .NET HttpClient communicates exclusively over HTTPS (`https://` scheme). No STARTTLS-based protocols are configured in the application.

**Verification (Tier-2 handoff):** `oc rsh <pod> ss -tnp` to confirm all outbound TLS is native HTTPS.

---

### libcurl-minimal / CVE-2026-8925 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. Double-free in GSASL context cleanup when libcurl performs SASL authentication. MMRIA Services does not use GSASL-based SASL authentication via libcurl. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-8925 — AV:N/AC:H; requires the calling application to trigger GSASL-based SASL authentication via libcurl. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services authenticates to CouchDB using Basic auth over HTTPS; GSASL libraries are not referenced in the application.

**Verification (Tier-2 handoff):** `oc rsh <pod> rpm -q gsasl libgsasl 2>&1` to confirm GSASL is absent.

---

### libcurl-minimal / CVE-2026-9547 (scan 30951)

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix available. When a libcurl-based app uses `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP, it may silently accept an untrusted server key. MMRIA Services does not perform SCP/SFTP transfers or use `CURLOPT_SSH_KEYFUNCTION`. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-9547 — AV:N/AC:H; requires SFTP/SCP transfer with a custom SSH key callback. Red Hat has not published a fix for RHEL 9.8 (status: `affected`).

**Controls:** MMRIA Services does not perform SCP or SFTP file transfers via libcurl. All data exchange uses HTTPS.

**Verification (Tier-2 handoff):** `oc rsh <pod> grep -r "sftp\|SFTP\|SCP\|SSH_KEY" /app/ || true` to confirm no SSH file-transfer usage.

---

### tar / CVE-2026-59873 (scan 30951)

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59873 describes a decompression-bomb DoS in `node-tar`, the Node.js npm tar library (prior to v7.5.19). The scanned package is GNU `tar` 1.34 on RHEL 9 (`2:1.34-11.el9`) — an entirely different codebase with no shared code with the npm `node-tar` package. Trivy misattributed this npm CVE to the system `tar` RPM. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-59873 and OSV — the affected component is explicitly identified as `node-tar` (npm), a JavaScript library for Node.js. Version 1.34 of GNU tar has no code relationship with `node-tar`. The RHEL `tar` RPM is developed by the GNU project; no Red Hat advisory references CVE-2026-59873 for the `tar` RPM. This is a Trivy scanner misattribution caused by a CVE database pattern-match on the word "tar."

---

### tar / CVE-2026-59874 (scan 30951)

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59874 describes a negative-size header infinite-loop DoS in `node-tar`, the Node.js npm tar library (prior to v7.5.18). The scanned package is GNU `tar` 1.34 on RHEL 9 (`2:1.34-11.el9`) — an entirely different codebase with no shared code with the npm `node-tar` package. Trivy misattributed this npm CVE to the system `tar` RPM. Carried from prior scan 30948 — evidence unchanged.

**CVE research:** NVD CVE-2026-59874 and OSV — the affected component is explicitly identified as `node-tar` (npm), a JavaScript library for Node.js. The exploit (`tar.replace` API accepting a negative base-256 size header) is specific to the Node.js `node-tar` implementation; GNU tar 1.34 does not expose a `tar.replace` JavaScript API. No Red Hat advisory references CVE-2026-59874 for the `tar` RPM. This is a Trivy scanner misattribution.

---
