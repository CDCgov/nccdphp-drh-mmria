# Trivy Remediation Log

## Scan: 31002 — MMRIA Services @ 1a75c40a (2026-07-25)

- **Commit:** `1a75c40a3c8e85a9b2ada53141dca2cd3409687c`
- **Service:** `MMRIA Services`
- **Scan ID:** `31002`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
- **Scan date:** 2026-07-25

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| HIGH | 18 | 0 | 0 | 13 | 5 | 13 |
| CRITICAL | 0 | 0 | 0 | 0 | 0 | 0 |

- Rebuild remediation added in `nccdphp-drh-mmria-services/mmria.services/Dockerfile` to update `libacl`, `curl-minimal`, `libcurl-minimal`, `tar`, and `dotnet-host` during image build so the next image picks up the latest available RHEL 9.8 errata.
- `⏳ EVIDENCE WOULD UPGRADE:` the curl/libcurl residual findings could be upgraded only if a rebuilt pod proves the RPMs are gone or upgraded beyond the scanned versions.

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `mmria-services:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-11352` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | `Program.cs` configures `SocketsHttpHandler` for outbound HTTP clients and repo inspection found no curl HTTP/3 or QUIC usage in service code; RPM remains installed until rebuild proves otherwise. |
| `mmria-services:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-11586` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The vulnerable path needs curl WebSocket client traffic; the service host uses .NET `SocketsHttpHandler` and repository inspection found no curl WebSocket usage. |
| `mmria-services:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-12064` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The vulnerable path requires explicit curl CLI use with schemeless SFTP/SCP URLs; repository inspection found no runtime curl CLI invocation. |
| `mmria-services:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-8286` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The STARTTLS connection-reuse flaw is tied to curl client protocol upgrades; the service host calls HTTPS endpoints through .NET HTTP handlers instead of curl STARTTLS flows. |
| `mmria-services:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-8925` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The GSASL double-free requires curl SASL integration; repository inspection found no GSASL or curl SASL usage in the service host. |
| `mmria-services:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-9547` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The vulnerable path requires curl/libcurl SCP or SFTP with SSH key callbacks; repository inspection found no such transfer flow in the service code. |
| `mmria-services:latest (redhat 9.8)` | `dotnet-host` | `CVE-2024-38081` | HIGH | end_of_life | `10.0.10-1.el9_8` | _none published in finding_ | Not applicable / false positive | The scanned artifact is a Linux RHEL 9.8 container image, while the reported vulnerability is a Windows-oriented .NET elevation-of-privilege issue and the service does not ship a Windows host stack. |
| `mmria-services:latest (redhat 9.8)` | `dotnet-host` | `CVE-2025-26682` | HIGH | end_of_life | `10.0.10-1.el9_8` | _none published in finding_ | Residual risk – required, not reachable under current controls | The service is an authenticated ASP.NET host: `Program.cs` registers `BasicAuthentication`, and controllers are decorated with `[Authorize(AuthenticationSchemes = "BasicAuthentication")]`; the Dockerfile now also requests the latest available `dotnet-host` errata at build time. |
| `mmria-services:latest (redhat 9.8)` | `dotnet-host` | `CVE-2025-59144` | HIGH | end_of_life | `10.0.10-1.el9_8` | _none published in finding_ | Not applicable / false positive | OSV scopes this CVE to the npm `debug` package compromise, and the service project has no `package.json` or `node_modules` tree in the runtime source path. |
| `mmria-services:latest (redhat 9.8)` | `dotnet-host` | `CVE-2026-48779` | HIGH | under_investigation | `10.0.10-1.el9_8` | _none published in finding_ | Not applicable / false positive | OSV scopes this CVE to the Node.js `ws` package, and the service project has no Node dependency manifest in the runtime source path. |
| `mmria-services:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-11352` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The libcurl HTTP/3 QUIC receive path is not exercised by the .NET `SocketsHttpHandler`-based service code, but the RPM remains present until rebuild/rescan evidence exists. |
| `mmria-services:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-11586` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The vulnerable libcurl WebSocket path is absent from the service codebase, which uses .NET HTTP clients rather than libcurl WebSocket APIs. |
| `mmria-services:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-12064` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The schemeless SFTP/SCP invocation pattern is a curl tool behavior and does not appear in the .NET service project. |
| `mmria-services:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-8286` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The STARTTLS-specific reuse flaw is not reachable from the service's .NET HTTP client configuration or authenticated controller surface. |
| `mmria-services:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-8925` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The GSASL precondition is absent from the service host, which does not configure GSASL or libcurl authentication callbacks. |
| `mmria-services:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-9547` | HIGH | affected | `7.76.1-40.el9` | _none published in finding_ | Residual risk – required, not reachable under current controls | The service code does not implement SCP/SFTP or `CURLOPT_SSH_KEYFUNCTION`-style behavior required to reach this libcurl issue. |
| `mmria-services:latest (redhat 9.8)` | `tar` | `CVE-2026-59873` | HIGH | affected | `2:1.34-11.el9` | _none published in finding_ | Not applicable / false positive | OSV scopes this CVE to npm `node-tar`, not the GNU `tar` RPM installed in the Red Hat runtime image. |
| `mmria-services:latest (redhat 9.8)` | `tar` | `CVE-2026-59874` | HIGH | affected | `2:1.34-11.el9` | _none published in finding_ | Not applicable / false positive | OSV scopes this CVE to npm `node-tar` `tar.replace` logic, not the GNU `tar` RPM installed in the Red Hat runtime image. |

## HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| `curl-minimal` | `CVE-2026-11352` | Residual risk – required, not reachable under current controls | HTTP/3 QUIC client precondition is absent from the .NET service host; rebuild now requests latest RPM errata. |
| `curl-minimal` | `CVE-2026-11586` | Residual risk – required, not reachable under current controls | WebSocket curl client precondition is absent from the service host. |
| `curl-minimal` | `CVE-2026-12064` | Residual risk – required, not reachable under current controls | Schemeless SFTP/SCP curl CLI precondition is absent from the repository. |
| `curl-minimal` | `CVE-2026-8286` | Residual risk – required, not reachable under current controls | STARTTLS curl client precondition is absent from the repository. |
| `curl-minimal` | `CVE-2026-8925` | Residual risk – required, not reachable under current controls | GSASL curl client precondition is absent from the repository. |
| `curl-minimal` | `CVE-2026-9547` | Residual risk – required, not reachable under current controls | SCP/SFTP curl SSH-key callback precondition is absent from the repository. |
| `dotnet-host` | `CVE-2024-38081` | Not applicable / false positive | The finding describes a Windows-oriented .NET EoP path, but the target is a Linux RHEL 9.8 container image. |
| `dotnet-host` | `CVE-2025-26682` | Residual risk – required, not reachable under current controls | Controllers require `BasicAuthentication` and rebuild now requests latest `dotnet-host` errata. |
| `dotnet-host` | `CVE-2025-59144` | Not applicable / false positive | OSV identifies npm `debug`, not `dotnet-host`. |
| `dotnet-host` | `CVE-2026-48779` | Not applicable / false positive | OSV identifies npm `ws`, not `dotnet-host`. |
| `libcurl-minimal` | `CVE-2026-11352` | Residual risk – required, not reachable under current controls | The service uses .NET HTTP handlers rather than libcurl HTTP/3 flows. |
| `libcurl-minimal` | `CVE-2026-11586` | Residual risk – required, not reachable under current controls | The service does not use libcurl WebSocket APIs. |
| `libcurl-minimal` | `CVE-2026-12064` | Residual risk – required, not reachable under current controls | The service does not invoke curl tool SFTP/SCP flows. |
| `libcurl-minimal` | `CVE-2026-8286` | Residual risk – required, not reachable under current controls | STARTTLS libcurl client behavior is absent from the repository. |
| `libcurl-minimal` | `CVE-2026-8925` | Residual risk – required, not reachable under current controls | GSASL libcurl client behavior is absent from the repository. |
| `libcurl-minimal` | `CVE-2026-9547` | Residual risk – required, not reachable under current controls | SCP/SFTP libcurl SSH callback behavior is absent from the repository. |
| `tar` | `CVE-2026-59873` | Not applicable / false positive | OSV identifies npm `node-tar`, not GNU tar. |
| `tar` | `CVE-2026-59874` | Not applicable / false positive | OSV identifies npm `node-tar`, not GNU tar. |

## Verification

- `dotnet build -c Release` succeeded for `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/nccdphp-drh-mmria-services/mmria.services`.
- Repo-static inspection found:
  - `Program.cs` registers `BasicAuthentication` and configures `SocketsHttpHandler` for outbound clients.
  - Service controllers are decorated with `[Authorize(AuthenticationSchemes = "BasicAuthentication")]`.
  - The service project has no `package.json` or `node_modules` tree under `nccdphp-drh-mmria-services/mmria.services`.
- Limitations:
  - No image rebuild, `oc rsh`, or Trivy rescan was available in this session.
  - The Dockerfile remediation requires a rebuild to prove upgraded RPM versions.
- Reviewer verification commands:
  - `podman build -f nccdphp-drh-mmria-services/mmria.services/Dockerfile -t mmria-services:trivy-remediation .`
  - `trivy image --severity HIGH,CRITICAL --ignore-unfixed=false mmria-services:trivy-remediation`
  - `oc rsh <pod> rpm -q curl-minimal libcurl-minimal tar dotnet-host`
  - `oc rsh <pod> find /app -maxdepth 8 \\( -name package.json -o -name node_modules \\) -print`

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** `curl-minimal` remains installed in the Red Hat runtime image, but the vulnerable HTTP/3 QUIC client path is not exercised by the MMRIA Services host because outbound calls are configured through .NET `SocketsHttpHandler` instead of curl or libcurl HTTP/3 features.

NVD describes CVE-2026-11352 as a curl HTTP/3 QUIC receive-path denial of service. In this repository, `nccdphp-drh-mmria-services/mmria.services/Program.cs` configures outbound clients with `SocketsHttpHandler`, and repo inspection found no curl CLI, HTTP/3, or QUIC usage in the service host. The Dockerfile now requests the latest available `curl-minimal` errata during rebuild, but until a rebuilt image proves the RPM version changed or disappeared, this remains residual risk rather than fixed.

Reviewer verification: `podman build -f nccdphp-drh-mmria-services/mmria.services/Dockerfile -t mmria-services:trivy-remediation . && trivy image --severity HIGH,CRITICAL mmria-services:trivy-remediation` or `oc rsh <pod> rpm -q curl-minimal && oc rsh <pod> grep -R \"HTTP3\\|http3\\|QUIC\\|quic\" /app/ /opt/app-root/src 2>/dev/null`.

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The MMRIA Services host does not use curl WebSocket client features, so the memory-growth path described in CVE-2026-11586 is not reachable from the repository's current code paths even though the `curl-minimal` RPM is still present in the base image.

The finding targets curl WebSocket PING handling. The service host's outbound HTTP stack is created with .NET `SocketsHttpHandler`, and repository inspection found no curl WebSocket usage in `mmria.services` or `mmria.common`. Because the RPM remains present until a rebuilt image proves an updated package set, this stays a residual-risk verdict.

Reviewer verification: `oc rsh <pod> rpm -q curl-minimal && oc rsh <pod> grep -R \"WebSocket\\|websocket\" /app/ /opt/app-root/src 2>/dev/null`.

### curl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2026-12064 requires an operator-invoked curl CLI flow with schemeless SFTP or SCP URLs, and the MMRIA Services repository does not invoke curl CLI for runtime data movement.

The vulnerable precondition is a specific curl command-line usage pattern, not generic HTTP traffic. Repository inspection found no runtime `curl` invocation in the service code, and the service performs network calls through .NET HTTP handlers instead. Because the `curl-minimal` RPM remains part of the runtime image until rebuild evidence exists, the finding remains residual risk.

Reviewer verification: `oc rsh <pod> rpm -q curl-minimal && oc rsh <pod> grep -R \"\\bcurl\\b\\|sftp://\\|scp://\" /app/ /opt/app-root/src 2>/dev/null`.

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The STARTTLS connection-reuse flaw in CVE-2026-8286 is not reachable from the MMRIA Services host because the service uses .NET HTTP clients against HTTPS endpoints rather than curl-managed STARTTLS protocol upgrades.

This repository does not configure SMTP, IMAP, POP3, FTP, or other STARTTLS-upgrade flows in the service host. `Program.cs` registers .NET HTTP clients with `SocketsHttpHandler`, and controllers expose authenticated HTTP APIs only. Until a rebuilt image proves the package is upgraded or absent, the installed `curl-minimal` RPM keeps this as residual risk.

Reviewer verification: `oc rsh <pod> rpm -q curl-minimal && oc rsh <pod> grep -R \"STARTTLS\\|smtp\\|imap\\|pop3\\|ftp\" /app/ /opt/app-root/src 2>/dev/null`.

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2026-8925 requires curl GSASL-based SASL handling, and the MMRIA Services host does not configure GSASL or any curl SASL callback path in repository code.

The service host uses application-level BasicAuthentication for inbound APIs and .NET HTTP clients for outbound traffic. Repository inspection found no GSASL configuration or curl SASL usage in the service runtime source. The RPM remains installed until rebuild verification exists, so the correct verdict stays residual risk.

Reviewer verification: `oc rsh <pod> rpm -q curl-minimal && oc rsh <pod> rpm -q gsasl libgsasl 2>&1`.

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2026-9547 depends on curl SCP or SFTP transfers using SSH host-key callbacks, and no such transfer implementation exists in the MMRIA Services repository.

Repository inspection found no SCP, SFTP, or SSH-key callback usage in the service host. The application exposes authenticated HTTP APIs and uses .NET HTTP handlers for outbound traffic. Because `curl-minimal` is still present until the updated Dockerfile is rebuilt and rescanned, the finding remains residual risk.

Reviewer verification: `oc rsh <pod> rpm -q curl-minimal && oc rsh <pod> grep -R \"SFTP\\|sftp\\|SCP\\|scp\\|SSH_KEYFUNCTION\" /app/ /opt/app-root/src 2>/dev/null`.

### dotnet-host / CVE-2024-38081

**Verdict:** Not applicable / false positive

**Summary:** The scanned target is a Linux RHEL 9.8 container image for `mmria.services`, while CVE-2024-38081 is reported as a Windows-oriented .NET elevation-of-privilege issue and does not map to a Windows runtime surface in this container.

The affected artifact in this repository is `dotnet-host` packaged into a Red Hat Linux image, not a Windows host or desktop runtime. The service Dockerfile targets `trusted-images/dotnet-100-aspnet` on RHEL 9.8 and the repository contains only Linux container build instructions for this service. Because the vulnerable platform precondition is absent from the scanned artifact, this finding is not applicable.

Reviewer verification: `oc rsh <pod> uname -a && oc rsh <pod> cat /etc/os-release && oc rsh <pod> rpm -q dotnet-host`.

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2025-26682 is an ASP.NET resource-exhaustion issue, but the MMRIA Services host exposes authenticated controllers only and the Dockerfile now requests the latest available `dotnet-host` errata during rebuild.

`Program.cs` registers `BasicAuthentication`, and service controllers are decorated with `[Authorize(AuthenticationSchemes = "BasicAuthentication")]`, which limits the reachable HTTP surface to authenticated callers. The service still depends on `dotnet-host`, so without rebuild and rescan evidence showing an upgraded RPM, the correct current verdict remains residual risk rather than fixed.

Reviewer verification: `oc rsh <pod> rpm -q dotnet-host && oc rsh <pod> grep -R \"Authorize(AuthenticationSchemes = \\\"BasicAuthentication\\\")\" /app/ /opt/app-root/src 2>/dev/null && trivy image --severity HIGH,CRITICAL mmria-services:trivy-remediation`.

### dotnet-host / CVE-2025-59144

**Verdict:** Not applicable / false positive

**Summary:** OSV scopes CVE-2025-59144 to the npm `debug` package compromise, and the MMRIA Services runtime source path contains no Node.js dependency manifest or `node_modules` directory that would carry that artifact.

This finding is misattributed to the `dotnet-host` RPM. The service project is a .NET Web SDK application, and repository inspection under `nccdphp-drh-mmria-services/mmria.services` found no `package.json`; the only `node_modules` match in that path is the ignore rule inside `.dockerignore`. Because the vulnerable npm package is absent from the scanned source tree, this finding is not applicable.

Reviewer verification: `oc rsh <pod> rpm -q dotnet-host && oc rsh <pod> find /app -maxdepth 8 \\( -name package.json -o -name node_modules \\) -print`.

### dotnet-host / CVE-2026-48779

**Verdict:** Not applicable / false positive

**Summary:** OSV scopes CVE-2026-48779 to the Node.js `ws` package, and the MMRIA Services runtime source path does not contain a Node dependency manifest that would bring `ws` into the final service image.

This is a misattribution to `dotnet-host`, not a real vulnerable .NET host package path. The service project is a .NET-only runtime host, and repository inspection found no `package.json` or `node_modules` tree under `nccdphp-drh-mmria-services/mmria.services`. Because the vulnerable `ws` package is absent from the relevant source path, this finding is not applicable.

Reviewer verification: `oc rsh <pod> rpm -q dotnet-host && oc rsh <pod> find /app -maxdepth 8 \\( -name package.json -o -name node_modules \\) -print`.

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The libcurl HTTP/3 QUIC denial-of-service path in CVE-2026-11352 is not exercised by MMRIA Services because outbound network clients are configured with .NET `SocketsHttpHandler`, not libcurl HTTP/3 features, even though the RPM is still present in the base image.

The repository's service host registers outbound HTTP clients through `SocketsHttpHandler`, and repository inspection found no HTTP/3 or QUIC usage in service code. The Dockerfile now requests updated `libcurl-minimal` errata during rebuild, but until a rebuild proves the version changed, the installed RPM keeps this as residual risk.

Reviewer verification: `oc rsh <pod> rpm -q libcurl-minimal && oc rsh <pod> grep -R \"HTTP3\\|http3\\|QUIC\\|quic\" /app/ /opt/app-root/src 2>/dev/null`.

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2026-11586 requires libcurl WebSocket client use, and the MMRIA Services host does not implement that behavior in repository code even though `libcurl-minimal` remains installed in the base image.

The service's outbound network behavior is configured in `Program.cs` through `SocketsHttpHandler`, and repository inspection found no WebSocket or libcurl WebSocket configuration in the service host. Without rebuild and rescan evidence showing the RPM is upgraded or gone, the correct current verdict remains residual risk.

Reviewer verification: `oc rsh <pod> rpm -q libcurl-minimal && oc rsh <pod> grep -R \"WebSocket\\|websocket\" /app/ /opt/app-root/src 2>/dev/null`.

### libcurl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2026-12064 is tied to curl tool invocation with schemeless SFTP or SCP URLs, and that invocation pattern does not exist in the MMRIA Services codebase that uses .NET HTTP clients instead.

The service repository does not invoke curl CLI for runtime work, and repo inspection found no SFTP or SCP URL usage in the service host. Because `libcurl-minimal` remains installed until rebuild evidence shows otherwise, the finding remains residual risk rather than fixed or not applicable.

Reviewer verification: `oc rsh <pod> rpm -q libcurl-minimal && oc rsh <pod> grep -R \"sftp://\\|scp://\\|\\bcurl\\b\" /app/ /opt/app-root/src 2>/dev/null`.

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The STARTTLS-specific libcurl reuse flaw in CVE-2026-8286 is not reachable from the service host because the repository configures authenticated HTTP APIs and .NET HTTP clients, not STARTTLS-upgrade protocol flows.

There is no SMTP, IMAP, POP3, or FTP STARTTLS configuration in the service host source path. `Program.cs` wires .NET HTTP clients only, and repo inspection found no STARTTLS markers in the service runtime source. Until rebuild evidence proves the RPM was upgraded or removed, the installed package keeps this as residual risk.

Reviewer verification: `oc rsh <pod> rpm -q libcurl-minimal && oc rsh <pod> grep -R \"STARTTLS\\|smtp\\|imap\\|pop3\\|ftp\" /app/ /opt/app-root/src 2>/dev/null`.

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The GSASL-specific libcurl double-free path in CVE-2026-8925 is not configured anywhere in the MMRIA Services repository, which uses BasicAuthentication for inbound APIs and .NET HTTP handlers for outbound traffic.

Repository inspection found no GSASL configuration, no curl SASL callbacks, and no application code that routes outbound traffic through libcurl authentication features. Because the `libcurl-minimal` RPM remains present until rebuild and rescan confirm an upgraded version, this finding remains residual risk.

Reviewer verification: `oc rsh <pod> rpm -q libcurl-minimal && oc rsh <pod> rpm -q gsasl libgsasl 2>&1`.

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2026-9547 requires libcurl SCP or SFTP transfers using SSH host-key callbacks, and no such callback-driven SSH transfer path exists in the MMRIA Services repository.

The service host exposes authenticated HTTP APIs and uses .NET HTTP clients for outbound requests. Repository inspection found no SCP, SFTP, or SSH key callback usage in the service runtime source, but because the RPM remains installed until rebuild evidence exists, the correct verdict remains residual risk.

Reviewer verification: `oc rsh <pod> rpm -q libcurl-minimal && oc rsh <pod> grep -R \"SFTP\\|sftp\\|SCP\\|scp\\|SSH_KEYFUNCTION\" /app/ /opt/app-root/src 2>/dev/null`.

### tar / CVE-2026-59873

**Verdict:** Not applicable / false positive

**Summary:** OSV scopes CVE-2026-59873 to the Node.js `node-tar` package, not the GNU `tar` RPM present in the Red Hat runtime image used by MMRIA Services.

The finding description and OSV record both identify a Node.js archive library path (`node-tar`) rather than GNU tar. The service repository has no Node package manifest in `nccdphp-drh-mmria-services/mmria.services`, so there is no repo-visible path that would ship `node-tar` with this service. Because the vulnerability maps to a different ecosystem and artifact than the installed RPM, this finding is not applicable.

Reviewer verification: `oc rsh <pod> rpm -q tar && oc rsh <pod> find /app -maxdepth 8 \\( -name package.json -o -name node_modules \\) -print`.

### tar / CVE-2026-59874

**Verdict:** Not applicable / false positive

**Summary:** OSV scopes CVE-2026-59874 to the Node.js `node-tar` `tar.replace` implementation, not the GNU `tar` RPM present in the Red Hat runtime image used by MMRIA Services.

The vulnerability description names a Node.js library API, while the scanned package is the GNU tar RPM supplied by the base image. The service repository has no Node package manifest in the runtime source path, so there is no repo-visible route that would add `node-tar` to the service artifact. Because the affected component and ecosystem do not match the installed RPM, this finding is not applicable.

Reviewer verification: `oc rsh <pod> rpm -q tar && oc rsh <pod> find /app -maxdepth 8 \\( -name package.json -o -name node_modules \\) -print`.
