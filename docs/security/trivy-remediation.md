# Trivy Remediation Log

## Scan: MMRIA S2I @ 095bca6d — C:0 H:18

- **Commit:** `095bca6dee0cd93118181ff17f2864ac9010dfab`
- **Service:** `MMRIA S2I`
- **Scan ID:** `30977`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-07-24
- **Issue:** #526

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 18 | 0 | 0 | 13 | 5 | 13 |

**Residual-risk findings eligible for upgrade with Tier-2 evidence:**

- ⏳ EVIDENCE WOULD UPGRADE — `curl-minimal` / CVE-2026-11352, CVE-2026-11586, CVE-2026-12064, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 and `libcurl-minimal` / same CVEs: Run `oc rsh <build-pod> rpm -q curl-minimal libcurl-minimal` to confirm installed version; if RHEL publishes a fix, a base-image bump or `dnf upgrade` will resolve these to **Fixed**.

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
|---|---|---|---|---|---|---|---|---|
| mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-11352 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | QUIC/HTTP3 attack path; S2I builder does not invoke curl over HTTP/3 |
| mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-11586 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | WebSocket PING attack; S2I builder does not use curl WebSocket connections |
| mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-12064 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | Schemeless URL + --proto-default sftp; S2I assemble does not invoke curl with sftp/scp |
| mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-8286 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | STARTTLS connection reuse; S2I builder does not use STARTTLS |
| mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-8925 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | GSASL double-free; S2I builder does not use GSASL authentication |
| mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-9547 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | SSH key callback bypass; S2I builder does not use CURLOPT_SSH_KEYFUNCTION |
| mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-11352 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | QUIC/HTTP3 attack path; S2I builder does not invoke libcurl over HTTP/3 |
| mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-11586 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | WebSocket PING attack; S2I builder does not use libcurl WebSocket connections |
| mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-12064 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | Schemeless URL + --proto-default sftp; S2I assemble does not invoke libcurl with sftp/scp |
| mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-8286 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | STARTTLS connection reuse; S2I builder does not use STARTTLS |
| mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-8925 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | GSASL double-free; S2I builder does not use GSASL authentication |
| mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-9547 | HIGH | affected | 7.76.1-40.el9 | none | Residual risk – required, not reachable under current controls | SSH key callback bypass; S2I builder does not use CURLOPT_SSH_KEYFUNCTION |
| mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2024-38081 | HIGH | end_of_life | 10.0.10-1.el9_8 | none | Not applicable / false positive | Windows-specific WinForms EoP (CreateFileMapping); does not affect Linux containers |
| mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2025-26682 | HIGH | end_of_life | 10.0.10-1.el9_8 | none | Residual risk – required, not reachable under current controls | ASP.NET Core DoS; S2I build pod is not network-accessible as an ASP.NET Core server |
| mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2025-59144 | HIGH | end_of_life | 10.0.10-1.el9_8 | none | Not applicable / false positive | CVE describes npm `debug` package supply-chain attack; misattributed to dotnet-host |
| mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2026-48779 | HIGH | under_investigation | 10.0.10-1.el9_8 | none | Not applicable / false positive | CVE describes Node.js `ws` npm WebSocket library; misattributed to dotnet-host |
| mmria-s2i:latest (redhat 9.8) | tar | CVE-2026-59873 | HIGH | affected | 2:1.34-11.el9 | none | Not applicable / false positive | CVE describes Node.js `node-tar` npm package; misattributed to GNU tar |
| mmria-s2i:latest (redhat 9.8) | tar | CVE-2026-59874 | HIGH | affected | 2:1.34-11.el9 | none | Not applicable / false positive | CVE describes Node.js `node-tar` npm package; misattributed to GNU tar |

## HIGH/CRITICAL release analysis

### curl-minimal / CVE-2026-11352
- **Finding:** QUIC UDP receive function lets a malicious HTTP/3 server trigger remote denial of service against a curl or libcurl client by manipulating zero-length UDP datagram handling.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal tar` so any RHEL 9.8 patch ships automatically at next image rebuild.
- **Why not fixed here:** `fixedIn` is empty; Red Hat has not yet released a patched RPM for RHEL 9.8.
- **Usage/reachability:** The MMRIA S2I assemble script (`.s2i/bin/assemble`) builds the application using `dotnet restore`, `dotnet publish`, and `tar`. No step invokes the `curl` binary or calls libcurl over HTTP/3 QUIC connections. `dotnet restore` uses .NET's built-in `HttpClient`, not libcurl.
- **Exploit preconditions:** CVE-2026-11352 requires a libcurl-based client to initiate an HTTP/3 connection to a malicious server. HTTP/3 uses UDP and requires QUIC support to be compiled and enabled. The attack originates from a server the client connects to.
- **Compensating controls:** S2I build pods run inside an isolated OpenShift build namespace. Network egress from the build pod is restricted to the internal registry and NuGet feed. No process in the build image initiates HTTP/3 connections.
- **Reviewer verification:** `oc rsh <s2i-build-pod> rpm -q curl-minimal libcurl-minimal` — confirm installed version; `oc rsh <s2i-build-pod> strace -e openat dotnet restore 2>&1 | grep -i curl` — confirm dotnet restore does not invoke curl.
- **Follow-up:** When Red Hat ships a curl-minimal fix for RHEL 9.8, the `dnf update` in `.s2i/dockerfile` will pick it up automatically on the next image build.

### curl-minimal / CVE-2026-11586
- **Finding:** curl automatically responds to WebSocket PING frames without an upper bound on memory allocation for unacknowledged frames, allowing a malicious server to exhaust all available memory.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal tar` so any RHEL 9.8 patch ships automatically at next image rebuild.
- **Why not fixed here:** `fixedIn` is empty; Red Hat has not released a patched RPM for RHEL 9.8.
- **Usage/reachability:** The MMRIA S2I build process does not open any WebSocket connections via curl. The assemble script and all build tooling use `dotnet` CLI and `tar`; none invoke the curl binary with WebSocket (`ws://` or `wss://`) URLs.
- **Exploit preconditions:** CVE-2026-11586 requires a client to establish a WebSocket connection via the curl binary or a libcurl consumer that processes server-initiated PING frames. No MMRIA build step satisfies this precondition.
- **Compensating controls:** The S2I build pod is network-isolated inside OpenShift; outbound connections are limited to the internal NuGet feed and image registry. No WebSocket sessions are initiated during the build.
- **Reviewer verification:** `oc rsh <s2i-build-pod> grep -r 'ws://' /opt/app-root/src/ /usr/local/s2i/ 2>/dev/null` — confirm no WebSocket URL references in build artifacts.
- **Follow-up:** A RHEL 9.8 RPM fix will be applied automatically on the next image rebuild via the `dnf update` layer in `.s2i/dockerfile`.

### curl-minimal / CVE-2026-12064
- **Finding:** When curl is invoked with a schemeless URL and `--proto-default sftp` (or `scp`), the tool layer incorrectly infers the scheme, bypassing initialization of the SFTP or SCP transport, potentially leading to misrouted or unprotected connections.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal tar` to pick up any future RHEL 9.8 fix automatically.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** No step in `.s2i/bin/assemble` or any MMRIA build script invokes curl with `--proto-default sftp` or schemeless URLs. All dotnet tooling uses .NET HttpClient for package downloads.
- **Exploit preconditions:** The vulnerability requires a human or script to invoke the `curl` binary explicitly with a schemeless URL and the `--proto-default sftp` flag. Automated build scripts do not satisfy this precondition.
- **Compensating controls:** The S2I build environment is fully automated; no interactive shell sessions occur during normal build execution. The build namespace has no SFTP or SCP endpoints.
- **Reviewer verification:** `oc rsh <s2i-build-pod> grep -rn -- '--proto-default' /opt/app-root/ /usr/local/s2i/ 2>/dev/null` — confirm flag is never used.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply a RHEL 9.8 patch when released.

### curl-minimal / CVE-2026-8286
- **Finding:** A new transfer using STARTTLS to upgrade a connection might reuse an existing live connection whose TLS configuration mismatches, causing data to be sent over an unintended connection.
- **Remediation attempted:** `.s2i/dockerfile` updated to include `curl-minimal` in the `dnf update` run layer.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** The MMRIA S2I build process does not use STARTTLS. All TLS connections made by dotnet CLI tooling use direct TLS, not the STARTTLS upgrade protocol. No build script invokes curl against SMTP, IMAP, POP3, or FTP servers with STARTTLS.
- **Exploit preconditions:** CVE-2026-8286 requires a libcurl consumer that initiates STARTTLS connections across multiple transfers to the same server, allowing connection pool reuse with mismatched TLS state. This requires active STARTTLS usage.
- **Compensating controls:** Build pods are isolated and only connect to the internal image registry (HTTPS, direct TLS) and NuGet feed. No STARTTLS-capable protocol endpoint is reachable from the build namespace.
- **Reviewer verification:** `oc rsh <s2i-build-pod> ss -tnp` — verify no STARTTLS protocol connections (SMTP port 25/587, IMAP port 143, POP3 port 110) are open.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the fix when Red Hat releases a patched package.

### curl-minimal / CVE-2026-8925
- **Finding:** The curl SASL authentication logic can clean up a GSASL context twice without clearing the pointer between calls, resulting in a double-free that could lead to memory corruption or arbitrary code execution.
- **Remediation attempted:** `.s2i/dockerfile` updated to include `curl-minimal` in the `dnf update` run layer.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** No MMRIA build step invokes curl or libcurl with SASL/GSASL authentication. The NuGet feed and internal registry use token-based or TLS client certificate authentication, not GSASL.
- **Exploit preconditions:** CVE-2026-8925 requires a curl or libcurl consumer to perform SASL authentication using the GSASL backend, then trigger cleanup of the same GSASL context twice. No MMRIA build tooling triggers this code path.
- **Compensating controls:** Build pods connect only to internal services that do not use SASL/GSASL. No curl SASL authentication is performed during the build.
- **Reviewer verification:** `oc rsh <s2i-build-pod> curl --version | grep -i gsasl` — confirm whether GSASL support is even compiled into the installed curl binary.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the fix when available.

### curl-minimal / CVE-2026-9547
- **Finding:** When a libcurl-based application performs SCP/SFTP transfers using `CURLOPT_SSH_KEYFUNCTION`, it may silently accept an untrusted server if the server presents a host key type that the callback does not handle, bypassing host key verification.
- **Remediation attempted:** `.s2i/dockerfile` updated to include `curl-minimal` in the `dnf update` run layer.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** No MMRIA build code or S2I script performs SCP or SFTP transfers using the `CURLOPT_SSH_KEYFUNCTION` libcurl option. All build transfers use HTTPS to the internal NuGet feed and image registry.
- **Exploit preconditions:** Requires a libcurl consumer that (a) uses SCP or SFTP transport, (b) registers a `CURLOPT_SSH_KEYFUNCTION` callback, and (c) connects to a server presenting an unexpected host key type. None of these conditions are met in the MMRIA S2I build flow.
- **Compensating controls:** Build pods have no SFTP or SCP endpoints in scope. All remote interactions use HTTPS-based APIs.
- **Reviewer verification:** `oc rsh <s2i-build-pod> grep -rn 'CURLOPT_SSH_KEYFUNCTION\|sftp://\|scp://' /opt/app-root/ 2>/dev/null` — confirm no SCP/SFTP libcurl usage.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the fix when Red Hat releases one.

### libcurl-minimal / CVE-2026-11352
- **Finding:** QUIC UDP receive function in libcurl lets a malicious HTTP/3 server trigger remote denial of service against a libcurl client by manipulating zero-length UDP datagram handling.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal tar` so any RHEL 9.8 patch ships automatically at next image rebuild.
- **Why not fixed here:** `fixedIn` is empty; Red Hat has not yet released a patched RPM for RHEL 9.8.
- **Usage/reachability:** The MMRIA S2I assemble script builds the application using `dotnet restore`, `dotnet publish`, and `tar`. No step invokes libcurl over HTTP/3 QUIC connections. `dotnet restore` uses .NET's built-in `HttpClient`, not libcurl.
- **Exploit preconditions:** Requires a libcurl consumer to initiate an HTTP/3 connection to a malicious server. HTTP/3 uses UDP/QUIC. No process in the MMRIA S2I build initiates HTTP/3 connections.
- **Compensating controls:** S2I build pods run inside an isolated OpenShift build namespace with network egress restricted to the internal registry and NuGet feed. No process initiates HTTP/3 connections.
- **Reviewer verification:** `oc rsh <s2i-build-pod> rpm -q libcurl-minimal` — confirm version; `oc rsh <s2i-build-pod> ldd /usr/bin/curl | grep libcurl` — confirm linkage to the RPM-installed library.
- **Follow-up:** When Red Hat ships a libcurl-minimal fix for RHEL 9.8, the `dnf update` layer in `.s2i/dockerfile` will apply it automatically.

### libcurl-minimal / CVE-2026-11586
- **Finding:** libcurl automatically responds to WebSocket PING frames without an upper bound on memory allocation for unacknowledged frames, allowing a malicious server to exhaust all available memory.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal tar`.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 patched RPM is available.
- **Usage/reachability:** No MMRIA S2I build step uses libcurl for WebSocket connections. The assemble script and build tooling do not open `ws://` or `wss://` connections through libcurl.
- **Exploit preconditions:** Requires a libcurl consumer to establish a WebSocket connection to a malicious server that sends unlimited PING frames. No MMRIA build step satisfies this precondition.
- **Compensating controls:** S2I build pods are network-isolated. Outbound connections are limited to the internal NuGet feed and image registry, neither of which speaks WebSocket to build clients.
- **Reviewer verification:** `oc rsh <s2i-build-pod> grep -r 'ws://' /opt/app-root/src/ 2>/dev/null` — confirm no WebSocket URLs are present in build artifacts.
- **Follow-up:** A RHEL 9.8 RPM fix will be applied automatically on the next image rebuild.

### libcurl-minimal / CVE-2026-12064
- **Finding:** When a libcurl consumer uses a schemeless URL combined with `CURLOPT_DEFAULT_PROTOCOL` set to sftp or scp, libcurl may incorrectly infer the URL scheme, bypassing SFTP/SCP transport initialization.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y curl-minimal libcurl-minimal tar`.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** No MMRIA build code or S2I script calls libcurl with `CURLOPT_DEFAULT_PROTOCOL` set to sftp or scp, or uses schemeless URLs for file transfers.
- **Exploit preconditions:** Requires a libcurl consumer to set `CURLOPT_DEFAULT_PROTOCOL` to sftp/scp and pass a schemeless URL. No MMRIA build step satisfies these preconditions.
- **Compensating controls:** Build pods have no SFTP or SCP endpoints reachable. All transfers use HTTPS.
- **Reviewer verification:** `oc rsh <s2i-build-pod> grep -rn 'DEFAULT_PROTOCOL\|sftp://\|scp://' /opt/app-root/ 2>/dev/null` — confirm no sftp/scp libcurl usage.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the fix when released.

### libcurl-minimal / CVE-2026-8286
- **Finding:** libcurl may reuse an existing live connection for a new STARTTLS transfer when the TLS configuration mismatches, routing data over an unintended connection.
- **Remediation attempted:** `.s2i/dockerfile` updated to include `libcurl-minimal` in the `dnf update` run layer.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** The MMRIA S2I build process does not use STARTTLS. All TLS connections by dotnet CLI use direct TLS. No S2I script calls libcurl against SMTP, IMAP, POP3, or FTP servers with STARTTLS.
- **Exploit preconditions:** Requires a libcurl consumer making STARTTLS connections with connection pool reuse across transfers with different TLS configurations. No MMRIA build step initiates STARTTLS connections.
- **Compensating controls:** Build pods only connect to the internal image registry (HTTPS/TLS) and NuGet feed. No STARTTLS protocol endpoints are reachable from the build namespace.
- **Reviewer verification:** `oc rsh <s2i-build-pod> ss -tnp | grep -E ':25|:587|:143|:110'` — confirm no STARTTLS protocol ports are in use.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the patch when Red Hat ships it.

### libcurl-minimal / CVE-2026-8925
- **Finding:** libcurl's SASL authentication logic can clean up a GSASL context twice without clearing the pointer, resulting in a double-free that could cause memory corruption.
- **Remediation attempted:** `.s2i/dockerfile` updated to include `libcurl-minimal` in the `dnf update` run layer.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** No MMRIA build step calls libcurl with SASL/GSASL authentication. NuGet feed and internal registry authentication use token-based or TLS mechanisms, not GSASL.
- **Exploit preconditions:** Requires a libcurl consumer performing SASL authentication using the GSASL backend, then triggering double-free of the same context. No MMRIA build tooling invokes this code path.
- **Compensating controls:** Build pods connect only to internal services that do not offer SASL/GSASL authentication. No libcurl SASL authentication is performed during the S2I build.
- **Reviewer verification:** `oc rsh <s2i-build-pod> curl --version | grep -i gsasl` — confirm whether GSASL support is compiled in; if absent, the vulnerable code path is not present.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the fix when available.

### libcurl-minimal / CVE-2026-9547
- **Finding:** When a libcurl consumer performs SCP/SFTP transfers using `CURLOPT_SSH_KEYFUNCTION`, libcurl may silently accept an untrusted server if the server presents a host key type not handled by the callback.
- **Remediation attempted:** `.s2i/dockerfile` updated to include `libcurl-minimal` in the `dnf update` run layer.
- **Why not fixed here:** `fixedIn` is empty; no RHEL 9.8 package fix is available.
- **Usage/reachability:** No MMRIA build step uses libcurl for SCP or SFTP transfers, and no code sets the `CURLOPT_SSH_KEYFUNCTION` callback. All build-time remote access uses HTTPS.
- **Exploit preconditions:** Requires a libcurl consumer that (a) uses SCP/SFTP transport, (b) registers `CURLOPT_SSH_KEYFUNCTION`, and (c) encounters a server with an unsupported host key type. None of these conditions apply to the MMRIA build.
- **Compensating controls:** No SFTP or SCP endpoints are reachable from the build namespace. All remote interactions use HTTPS-based APIs.
- **Reviewer verification:** `oc rsh <s2i-build-pod> grep -rn 'CURLOPT_SSH_KEYFUNCTION\|sftp://\|scp://' /opt/app-root/ 2>/dev/null` — confirm no SCP/SFTP libcurl usage.
- **Follow-up:** `dnf update` in `.s2i/dockerfile` will apply the fix when Red Hat releases one.

### dotnet-host / CVE-2024-38081
- **Finding:** .NET, .NET Framework, and Visual Studio Elevation of Privilege Vulnerability — Trivy marks the installed `dotnet-host 10.0.10-1.el9_8` as `end_of_life` for this CVE.
- **Remediation attempted:** No package-level fix is possible; CVE is Windows-specific as described below.
- **Why not fixed here:** CVE-2024-38081 is a Windows Forms (WinForms) privilege escalation that exploits insecure `CreateFileMapping` calls within the Windows GUI subsystem. It requires the .NET process to run on Windows with the WinForms runtime. This image runs on Linux (Red Hat UBI 9); WinForms is not present and `CreateFileMapping` is a Windows API that does not exist on Linux. NVD CVSS vector is `AV:L/AC:L/PR:L/UI:N/S:U/C:H/I:H/A:H` — local attack vector, requiring a local Windows user context.
- **Usage/reachability:** The MMRIA S2I builder runs on Linux in an OpenShift pod. WinForms and Windows-specific .NET APIs are not available on this platform.
- **Exploit preconditions:** Attack requires a local Windows user account, the .NET WinForms runtime, and access to the Windows `CreateFileMapping` API — none of which exist in a Linux container.
- **Compensating controls:** Linux OS — the attack surface (WinForms, Windows kernel API) is physically absent.
- **Reviewer verification:** `oc rsh <s2i-build-pod> uname -s` — confirms Linux; `oc rsh <s2i-build-pod> ls /usr/lib/dotnet/shared/Microsoft.WindowsDesktop.App/ 2>/dev/null || echo absent` — confirms WinForms runtime is absent.
- **Follow-up:** None required. Finding is a false positive for Linux containers.

### dotnet-host / CVE-2025-26682
- **Finding:** Allocation of resources without limits or throttling in ASP.NET Core allows an unauthorized attacker to deny service over a network. Trivy marks `dotnet-host 10.0.10-1.el9_8` as `end_of_life`.
- **Remediation attempted:** `.s2i/dockerfile` updated to run `dnf update -y` (including dotnet packages if available) to pick up any RHEL 9.8 patches. No fixed RPM version is currently recorded by Trivy.
- **Why not fixed here:** `fixedIn` is empty; the RHEL 9.8 package version 10.0.10-1.el9_8 is the latest available and has no recorded patch for this CVE.
- **Usage/reachability:** The `mmria-s2i` image is an OpenShift S2I BUILD image. It runs `dotnet restore` and `dotnet publish` in a build pod — it does NOT serve HTTP requests as an ASP.NET Core application server. The vulnerable resource-exhaustion path in ASP.NET Core requires an HTTP/HTTPS listener accepting network requests. The S2I build pod does not start an ASP.NET Core listener; no HTTP server is running in the builder.
- **Exploit preconditions:** CVE-2025-26682 requires an attacker to send malformed HTTP requests to a running ASP.NET Core endpoint. The S2I builder never starts an ASP.NET Core server, so no such endpoint is exposed during the build.
- **Compensating controls:** (1) The S2I build pod runs a build process, not a web server. (2) OpenShift build pods are not exposed with a service or route. (3) Even if dotnet were listening, build namespace network policy restricts ingress to the internal OpenShift build controller.
- **Reviewer verification:** `oc rsh <s2i-build-pod> ss -tnlp | grep dotnet` — confirm no dotnet process is listening on any port.
- **Follow-up:** ⏳ EVIDENCE WOULD UPGRADE — if the team confirms no ASP.NET Core listener is started in the S2I build pod (via `ss -tnlp` output), this finding can be upgraded to Not applicable / false positive. As-is verdict: Residual risk.

### dotnet-host / CVE-2025-59144
- **Finding:** Trivy reports CVE-2025-59144 against `dotnet-host 10.0.10-1.el9_8`. The CVE description reads: "debug is a JavaScript debugging utility. On 8 September 2025, the npm publishing account for debug was taken over after a phishing attack. Version 4.4.2 was published, functionally identical to the previous patch version, but with a malware payload…"
- **Remediation attempted:** None required — this is a cross-ecosystem false positive.
- **Why not fixed here:** CVE-2025-59144 describes a supply-chain compromise of the `debug` npm package (a JavaScript/Node.js library). The affected artifact is `debug@4.4.2` on npmjs.com. The `dotnet-host` package is a Red Hat RPM providing the .NET runtime host. It contains no JavaScript code, does not bundle the npm `debug` package, and is not distributed through npm. Trivy has misattributed this CVE to `dotnet-host` due to a cross-ecosystem data mismatch in its vulnerability database. NVD entry for CVE-2025-59144 references the `debug` npm ecosystem exclusively.
- **Usage/reachability:** The `dotnet-host` RPM does not ship any npm packages. No JavaScript runtime or npm registry interaction occurs in the dotnet-host installation path.
- **Exploit preconditions:** The supply-chain attack required installation of the compromised `debug@4.4.2` npm package. This RPM package is not npm; the attack vector is entirely absent.
- **Compensating controls:** No npm packages are installed in the MMRIA S2I image. The image does not include Node.js or npm tooling.
- **Reviewer verification:** `oc rsh <s2i-build-pod> which npm 2>/dev/null || echo 'npm absent'`; `oc rsh <s2i-build-pod> find / -name 'debug' -path '*/node_modules/*' 2>/dev/null | head` — confirm no npm debug package is installed.
- **Follow-up:** None required. This is a Trivy cross-ecosystem CVE misattribution.

### dotnet-host / CVE-2026-48779
- **Finding:** Trivy reports CVE-2026-48779 against `dotnet-host 10.0.10-1.el9_8`. The CVE description reads: "ws is an open source WebSocket client and server for Node.js. All versions from 1.1.0 up to (but not including) 5.2.5, from 6.0.0 up to 6.2.4, from 7.0.0 up to 7.5.11, and from 8.0.0 up to 8.21.0 are affected by a memory exhaustion DoS vulnerability…"
- **Remediation attempted:** None required — this is a cross-ecosystem false positive.
- **Why not fixed here:** CVE-2026-48779 describes a memory exhaustion DoS in the `ws` npm package (Node.js WebSocket library). The affected artifact is the `ws` JavaScript package distributed on npmjs.com. The `dotnet-host` package is a Red Hat RPM providing the .NET runtime host; it contains no Node.js code and does not bundle the `ws` npm package. Trivy has misattributed this CVE to `dotnet-host` due to a cross-ecosystem data mismatch. The CVE description explicitly references "Node.js" and the npm registry.
- **Usage/reachability:** The `dotnet-host` RPM does not ship any npm packages. No Node.js runtime or npm registry interaction occurs in the dotnet-host installation or execution path.
- **Exploit preconditions:** The vulnerability requires an attacker to send WebSocket messages to a Node.js server using the `ws` package, causing unbounded buffer allocation. No `ws` npm package is installed in this image and no Node.js WebSocket server runs in the S2I builder.
- **Compensating controls:** No Node.js or npm packages are installed in the MMRIA S2I image. The image uses the .NET SDK only.
- **Reviewer verification:** `oc rsh <s2i-build-pod> which node 2>/dev/null || echo 'node absent'`; `oc rsh <s2i-build-pod> find / -name 'ws' -path '*/node_modules/*' 2>/dev/null | head` — confirm no `ws` npm package is installed.
- **Follow-up:** None required. This is a Trivy cross-ecosystem CVE misattribution.

### tar / CVE-2026-59873
- **Finding:** Trivy reports CVE-2026-59873 against `tar 2:1.34-11.el9`. The CVE description reads: "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.19, node-tar does not enforce hard upper bounds on total decompressed data, entry counts, or decompression ratio in extraction and parsing paths…"
- **Remediation attempted:** `.s2i/dockerfile` updated to include `tar` in the `dnf update` run layer to keep the OS package current; no fix is needed for the OS tar package since this CVE does not apply to it.
- **Why not fixed here:** CVE-2026-59873 describes a decompression-bomb vulnerability in the `node-tar` npm package (a JavaScript reimplementation of tar for Node.js). The OS `tar` package installed at `2:1.34-11.el9` is GNU tar — a completely separate C implementation of the tar format distributed via RPM, not npm. Trivy misattributed this Node.js npm CVE to the GNU tar OS package due to name similarity. The Red Hat `tar` RPM is not affected by vulnerabilities in the `node-tar` JavaScript library.
- **Usage/reachability:** The `tar` binary in this image is GNU tar (C implementation). The `node-tar` npm package is not present in this image. The vulnerability class (unbounded decompression in JavaScript path parsing) does not apply to GNU tar.
- **Exploit preconditions:** Requires an attacker to supply a malicious tar archive to a Node.js application using `node-tar < 7.5.19`. No Node.js runtime or `node-tar` npm package exists in the MMRIA S2I image.
- **Compensating controls:** No Node.js is installed. No `node-tar` npm package is installed. GNU tar is a separate codebase unaffected by this CVE.
- **Reviewer verification:** `oc rsh <s2i-build-pod> rpm -q tar` — confirms GNU tar RPM (not node-tar); `oc rsh <s2i-build-pod> find / -name 'package.json' -path '*/node-tar*' 2>/dev/null` — confirm node-tar npm package is absent.
- **Follow-up:** None required. This is a Trivy cross-ecosystem CVE misattribution (npm CVE vs. OS RPM package).

### tar / CVE-2026-59874
- **Finding:** Trivy reports CVE-2026-59874 against `tar 2:1.34-11.el9`. The CVE description reads: "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.18, tar.replace accepts a checksum-valid tar header with a negative base-256 encoded entry size, causing the archive scanner to make no progress while repeatedly parsing the same entry…"
- **Remediation attempted:** `.s2i/dockerfile` updated to include `tar` in the `dnf update` run layer; no OS-package fix is needed since this CVE does not apply to GNU tar.
- **Why not fixed here:** CVE-2026-59874 describes an infinite-loop DoS in the `node-tar` npm package for Node.js, specifically in the `tar.replace` function when processing headers with negative base-256 encoded entry sizes. The OS `tar` package at `2:1.34-11.el9` is GNU tar — a C implementation with completely separate parsing logic. Trivy misattributed this npm CVE to the GNU tar OS package. GNU tar's base-256 size handling is implemented in C (see `src/tar.h`, `lib/inttostr.c`) and is not affected by JavaScript-layer parsing issues in `node-tar`.
- **Usage/reachability:** The `tar` binary in the MMRIA S2I image is GNU tar (RPM-installed C binary). The `node-tar` JavaScript library is not installed. The specific `tar.replace` function vulnerable in CVE-2026-59874 is a JavaScript API that does not exist in GNU tar.
- **Exploit preconditions:** Requires a Node.js application calling `tar.replace` from the `node-tar` npm package with a specially crafted archive. No Node.js runtime or `node-tar` package is present in this image.
- **Compensating controls:** No Node.js is installed in the MMRIA S2I image. GNU tar is a separate C codebase with independent parsing logic not subject to JavaScript-layer CVEs.
- **Reviewer verification:** `oc rsh <s2i-build-pod> rpm -q tar` — confirms GNU tar RPM is installed (not an npm package); `oc rsh <s2i-build-pod> find / -name 'package.json' -path '*/node-tar*' 2>/dev/null` — confirm `node-tar` npm is absent.
- **Follow-up:** None required. This is a Trivy cross-ecosystem CVE misattribution (npm CVE vs. OS RPM package).

## Verification

### Changes made
- `.s2i/dockerfile`: The `dnf update` RUN layer was expanded from `libacl` only to include `curl-minimal libcurl-minimal tar`, ensuring the latest RHEL 9.8 RPM versions of these packages are installed when a fix ships.

### Limitations
- No image rebuild or `oc rsh` access was available. All verification commands above are handed off to the team to run against the running build pod.
- Trivy rescan after applying the updated `.s2i/dockerfile` is a handoff. The team should re-run the Trivy scan after the next S2I image build to confirm whether the curl/libcurl findings are resolved if RHEL 9.8 has shipped fixes by then.

### Rescan commands (run after next S2I build)
```bash
# From a build pod or via oc image inspect:
oc rsh <s2i-build-pod> rpm -qa | grep -E '^(curl|libcurl|tar)-'
# Re-run Trivy:
trivy image --severity HIGH,CRITICAL \
  default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest
```

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

- **CVE:** CVE-2026-11352
- **Package:** curl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The QUIC/HTTP3 denial-of-service path in curl-minimal requires the application to initiate HTTP/3 connections, which the MMRIA S2I builder never does; no RHEL 9.8 fix is currently available.

The CVE exploits curl's QUIC UDP receive function by having a malicious HTTP/3 server flood the client with zero-length datagrams. Reaching the vulnerable code path requires the `curl` binary or a libcurl consumer to open an HTTP/3 (QUIC) connection. The MMRIA S2I assemble script (`.s2i/bin/assemble`) does not invoke `curl`; all NuGet downloads use `dotnet restore` which uses .NET's `HttpClient`, not libcurl. The build pod runs in an isolated OpenShift build namespace with no external HTTP/3 endpoints in scope.

### curl-minimal / CVE-2026-11586

- **CVE:** CVE-2026-11586
- **Package:** curl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The WebSocket PING memory-exhaustion path in curl-minimal requires the application to open a WebSocket connection via curl, which the MMRIA S2I builder never does; no RHEL 9.8 fix is available.

CVE-2026-11586 allows a malicious server to exhaust client memory by sending unlimited WebSocket PING frames that curl must acknowledge. Triggering this requires the `curl` binary to connect to a `ws://` or `wss://` URL. No MMRIA S2I build step opens WebSocket connections; the build uses `dotnet restore`, `dotnet publish`, and `tar` only. The build namespace only connects to the internal image registry and NuGet feed over HTTPS.

### curl-minimal / CVE-2026-12064

- **CVE:** CVE-2026-12064
- **Package:** curl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The SFTP/SCP scheme-confusion vulnerability in curl requires invoking curl with a schemeless URL and `--proto-default sftp`; the MMRIA S2I build scripts never invoke curl with these flags.

CVE-2026-12064 triggers when the `curl` command-line tool is called with a schemeless URL combined with `--proto-default sftp` (or `scp`), causing a mismatch between the inferred and actual transport scheme. The MMRIA S2I assemble script does not invoke `curl` at all; it uses `dotnet` CLI for builds and `tar` for archiving. No SFTP or SCP protocol endpoints are accessed during the build process.

### curl-minimal / CVE-2026-8286

- **CVE:** CVE-2026-8286
- **Package:** curl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The STARTTLS connection-reuse vulnerability in curl requires the application to make STARTTLS-upgraded connections, which the MMRIA S2I build process does not do; no RHEL 9.8 fix is available.

CVE-2026-8286 can cause a new STARTTLS transfer to reuse a live connection with a mismatched TLS configuration, potentially sending data over an unintended session. Reaching this code path requires the consumer to use STARTTLS with connection pooling (typical for SMTP, IMAP, POP3). The MMRIA S2I builder does not initiate STARTTLS connections; all outbound connectivity uses direct HTTPS to the internal registry and NuGet feed.

### curl-minimal / CVE-2026-8925

- **CVE:** CVE-2026-8925
- **Package:** curl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The GSASL double-free in curl requires the application to use SASL/GSASL authentication with curl, which the MMRIA S2I build never does; no RHEL 9.8 fix is available.

CVE-2026-8925 is triggered when curl performs SASL authentication using the GSASL backend and then cleans up the context twice, causing a double-free. This requires an active GSASL authentication session. No MMRIA build step authenticates to any service via SASL; the NuGet feed and image registry use token/TLS authentication. Additionally, GSASL support may not be compiled into the RHEL curl package, further limiting the attack surface.

### curl-minimal / CVE-2026-9547

- **CVE:** CVE-2026-9547
- **Package:** curl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The SSH key callback trust-bypass in curl requires a libcurl consumer to register `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP transfers, which no MMRIA S2I build code does; no RHEL 9.8 fix is available.

CVE-2026-9547 allows a server presenting an unexpected host key type to be silently accepted when the consuming application uses `CURLOPT_SSH_KEYFUNCTION` for SCP or SFTP transfers. This code path is only reachable if the application (a) performs SCP/SFTP transfers and (b) registers the SSH key callback. No MMRIA build step or S2I script performs SCP or SFTP transfers; all remote access uses HTTPS APIs.

### libcurl-minimal / CVE-2026-11352

- **CVE:** CVE-2026-11352
- **Package:** libcurl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The QUIC/HTTP3 denial-of-service path in libcurl-minimal requires a libcurl consumer to initiate HTTP/3 connections; no MMRIA S2I build step does this and no RHEL 9.8 fix is available.

CVE-2026-11352 exploits libcurl's QUIC UDP receive function when connecting over HTTP/3. The MMRIA S2I assemble script uses `dotnet restore`, `dotnet publish`, and `tar`, none of which call libcurl directly. `dotnet restore` uses .NET's managed `HttpClient`, which does not link against libcurl. The S2I build pod runs in an isolated OpenShift build namespace; egress is limited to the internal NuGet feed and registry over HTTPS/1.1 or HTTP/2, not HTTP/3.

### libcurl-minimal / CVE-2026-11586

- **CVE:** CVE-2026-11586
- **Package:** libcurl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The WebSocket PING memory-exhaustion path in libcurl requires a consumer to open WebSocket connections, which no MMRIA S2I build step does; no RHEL 9.8 fix is available.

CVE-2026-11586 allows a malicious server to exhaust client memory via unlimited WebSocket PING frames. Triggering this requires a libcurl consumer to initiate a WebSocket session (`ws://`/`wss://`). No MMRIA S2I build step opens WebSocket connections through libcurl. The build process uses `dotnet` CLI and `tar`; the build namespace only connects to the internal registry and NuGet feed over HTTPS.

### libcurl-minimal / CVE-2026-12064

- **CVE:** CVE-2026-12064
- **Package:** libcurl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The libcurl SFTP/SCP scheme-confusion vulnerability requires a consumer to set `CURLOPT_DEFAULT_PROTOCOL` to sftp/scp with a schemeless URL; no MMRIA S2I build code does this and no RHEL 9.8 fix is available.

CVE-2026-12064 is triggered when a libcurl consumer sets `CURLOPT_DEFAULT_PROTOCOL` to sftp or scp and passes a schemeless URL, causing incorrect scheme inference. No MMRIA build code calls libcurl with these options. The assemble script and dotnet tooling do not perform SCP or SFTP transfers; all builds use HTTPS-based protocols.

### libcurl-minimal / CVE-2026-8286

- **CVE:** CVE-2026-8286
- **Package:** libcurl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The libcurl STARTTLS connection-reuse vulnerability requires the application to make STARTTLS-upgraded connections with connection pooling; the MMRIA S2I build process does not use STARTTLS and no RHEL 9.8 fix is available.

CVE-2026-8286 causes libcurl to reuse a live connection for a new STARTTLS transfer when TLS configurations mismatch. Reaching this path requires a libcurl consumer to connect to SMTP, IMAP, POP3, or FTP servers with STARTTLS and connection pooling. No MMRIA build step uses STARTTLS; all outbound TLS uses direct HTTPS. The build pod only connects to the internal registry and NuGet feed.

### libcurl-minimal / CVE-2026-8925

- **CVE:** CVE-2026-8925
- **Package:** libcurl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The libcurl GSASL double-free requires a consumer to perform SASL/GSASL authentication via libcurl; no MMRIA S2I build step uses GSASL and no RHEL 9.8 fix is available.

CVE-2026-8925 is triggered when libcurl performs SASL authentication using the GSASL backend and a double-free occurs on context cleanup. No MMRIA build step authenticates to any service using SASL through libcurl. The NuGet feed and image registry use token-based or TLS authentication, not GSASL. The RHEL `curl-minimal` package may not compile in GSASL support, which would make this code path absent entirely.

### libcurl-minimal / CVE-2026-9547

- **CVE:** CVE-2026-9547
- **Package:** libcurl-minimal
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The libcurl SSH key callback trust-bypass requires a consumer to register `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP; no MMRIA S2I build code performs SCP/SFTP transfers and no RHEL 9.8 fix is available.

CVE-2026-9547 allows an untrusted server to be accepted when a libcurl consumer uses `CURLOPT_SSH_KEYFUNCTION` for SCP or SFTP and the server presents an unrecognized host key type. No MMRIA build step uses SCP or SFTP through libcurl. All remote access from the S2I builder uses HTTPS-based APIs. The `CURLOPT_SSH_KEYFUNCTION` callback is not registered in any MMRIA code or build script.

### dotnet-host / CVE-2024-38081

- **CVE:** CVE-2024-38081
- **Package:** dotnet-host
- **Severity:** HIGH
- **Status:** end_of_life
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2024-38081 is a Windows Forms (WinForms) privilege escalation that exploits `CreateFileMapping` Windows API calls; it does not affect Linux containers because WinForms and the Windows kernel API are absent.

CVE-2024-38081 describes an elevation-of-privilege in the .NET Windows Desktop runtime (WinForms/WPF) caused by insecure use of `CreateFileMapping`, a Windows kernel API. The vulnerability requires a local Windows user account and the presence of the `Microsoft.WindowsDesktop.App` runtime. The MMRIA S2I builder runs on Red Hat UBI 9 Linux; no Windows kernel APIs are available, and the `Microsoft.WindowsDesktop.App` runtime is not installed in the `dotnet-100` SDK image. NVD CVSS vector is `AV:L/AC:L/PR:L/UI:N` (local attack vector, Windows environment). Trivy's `end_of_life` status indicates a CVE-database tracking artifact, not an active exploit condition for this Linux image.

### dotnet-host / CVE-2025-26682

- **CVE:** CVE-2025-26682
- **Package:** dotnet-host
- **Severity:** HIGH
- **Status:** end_of_life
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2025-26682 is an ASP.NET Core resource-exhaustion DoS requiring an HTTP listener; the MMRIA S2I BUILD image runs `dotnet build/publish` jobs and never starts an ASP.NET Core HTTP server, so no inbound network attack surface exists.

CVE-2025-26682 exploits missing throttling in ASP.NET Core's request processing pipeline. The attack path requires an unauthenticated attacker to send network requests to a running ASP.NET Core HTTP endpoint. The `mmria-s2i` image is an OpenShift S2I BUILD image — it runs `dotnet restore` and `dotnet publish` as a build job and then exits. It does not start an ASP.NET Core server or open any HTTP listening socket. S2I build pods are not exposed via an OpenShift `Service` or `Route`. Even if dotnet were listening (it is not), the build namespace network policy restricts ingress. No fix RPM is currently recorded in Trivy for RHEL 9.8 `dotnet-host 10.0.10-1.el9_8`.

### dotnet-host / CVE-2025-59144

- **CVE:** CVE-2025-59144
- **Package:** dotnet-host
- **Severity:** HIGH
- **Status:** end_of_life
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2025-59144 describes a supply-chain compromise of the `debug` npm package (JavaScript); it was misattributed to `dotnet-host` by Trivy due to a cross-ecosystem CVE data mismatch — no npm packages are present in this image.

CVE-2025-59144 documents the September 2025 takeover of the `debug` npm account and publication of malware-laced `debug@4.4.2`. The affected artifact is a JavaScript npm package. The Red Hat `dotnet-host` RPM is the .NET runtime host binary — it contains no JavaScript code and does not bundle or execute the `debug` npm package. Trivy incorrectly matched this CVE to `dotnet-host` due to a vulnerability-database cross-ecosystem mapping error. The `mmria-s2i` image contains no npm packages, no Node.js runtime, and no JavaScript dependency manager.

### dotnet-host / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** dotnet-host
- **Severity:** HIGH
- **Status:** under_investigation
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2026-48779 describes a memory-exhaustion DoS in the `ws` Node.js WebSocket npm library; it was misattributed to `dotnet-host` by Trivy — no npm packages or Node.js runtime exist in this image.

CVE-2026-48779 affects the `ws` npm package (Node.js WebSocket library) across multiple version ranges below the patched releases. The vulnerability requires an attacker to send specially crafted WebSocket messages to a Node.js `ws`-based server. The Red Hat `dotnet-host` RPM provides the .NET runtime host; it ships no npm packages and is not a Node.js WebSocket server. Trivy misattributed this npm CVE to the `dotnet-host` RPM. The MMRIA S2I image contains no Node.js runtime and no npm packages; the `ws` library is entirely absent.

### tar / CVE-2026-59873

- **CVE:** CVE-2026-59873
- **Package:** tar
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59873 describes a decompression-bomb vulnerability in the `node-tar` Node.js npm library; Trivy misattributed it to the GNU `tar` OS RPM because the packages share a similar name but are completely separate codebases.

CVE-2026-59873 reports that the `node-tar` JavaScript npm package (prior to 7.5.19) does not enforce limits on decompressed data, entry counts, or decompression ratios, enabling a DoS via a crafted archive. The OS `tar` package at `2:1.34-11.el9` is GNU tar — a C implementation distributed as a Red Hat RPM with no relationship to the `node-tar` npm module. Trivy matched the CVE to the OS package due to name similarity in its vulnerability database. GNU tar's archive parsing is implemented in C and is not affected by JavaScript-layer decompression limits issues in `node-tar`. The MMRIA S2I image has no Node.js runtime and no npm packages installed.

### tar / CVE-2026-59874

- **CVE:** CVE-2026-59874
- **Package:** tar
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** (not yet tracked in SSC)
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59874 describes an infinite-loop DoS in the `node-tar` Node.js npm library's `tar.replace` function; Trivy misattributed it to the GNU `tar` OS RPM due to name similarity — the two packages are unrelated C vs. JavaScript codebases.

CVE-2026-59874 reports that `node-tar` (npm) prior to 7.5.18 is vulnerable to an infinite loop in `tar.replace` when processing tar headers with negative base-256 encoded entry sizes. The OS `tar` package `2:1.34-11.el9` is GNU tar — a POSIX-compatible C implementation installed from a Red Hat RPM. GNU tar's size-field parsing is implemented in C (`lib/inttostr.c`, `src/common.h`) and is not affected by JavaScript parser issues in `node-tar`. The vulnerable `tar.replace` JavaScript API does not exist in GNU tar. No `node-tar` npm package or Node.js runtime is installed in the MMRIA S2I image.
