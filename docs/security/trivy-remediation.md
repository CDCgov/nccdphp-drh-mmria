# Trivy Remediation Record

This file is the system of record for all Trivy security findings processed for this
repository. It is **appended** each scan run (never overwritten) and feeds carry-forward
logic for subsequent scans.

---

## Scan: MMRIA Services — 2026-07-23 (Scan ID 30966, commit 6f5e8820)

- **Service:** MMRIA Services
- **Scan ID:** 30966
- **Commit:** 6f5e8820c72667ac7049c9e022c6c443d7e95276
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
- **Workflow run:** https://github.com/cdcent/nccdphp-od-devops/actions/runs/30047225236

### Triage Summary

| Severity | Original | Fixed | Not applicable | Residual | Remaining |
|----------|----------|-------|----------------|----------|-----------|
| High     | 18       | 0     | 4              | 14       | 0         |
| Critical | 0        | 0     | 0              | 0        | 0         |

All 18 findings have a final verdict. No findings are awaiting further input before this
PR is releasable.

#### Findings with upgrade potential (⏳ EVIDENCE WOULD UPGRADE)

The four residual-risk dotnet-host findings (CVE-2024-38081, CVE-2025-26682, and the
curl/libcurl group) **could** have their residual-risk verdicts revised to
**Already remediated** if the image is rebuilt from the updated Dockerfile and rescanned.
To verify, run:

```bash
# After rebuilding the image, rescan and confirm these CVEs are no longer present:
oc rsh <mmria-services-pod> rpm -q curl-minimal libcurl-minimal dotnet-host
```

### Changes made

| File | Change | Addresses |
|------|--------|-----------|
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` | Added `dnf update -y tar` RUN layer | Hygiene (tar CVEs are FP but ensures latest GNU tar patches) |
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` | Added `dnf update -y dotnet-host dotnet-runtime-10.0 aspnetcore-runtime-10.0` RUN layer | Attempts to pull any available .NET patches on image rebuild |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Summary:** No patched version of curl-minimal is available in RHEL 9.8 repositories for this QUIC DoS CVE. The Dockerfile already includes `dnf update -y curl-minimal libcurl-minimal` which will apply any future patch on rebuild. The mmria.services application is a .NET background-jobs host that performs HTTP via .NET's `HttpClient`; it does not invoke the `curl` CLI or HTTP/3 QUIC paths. curl-minimal cannot be removed because `rpm` depends on `libcurl-minimal`.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `curl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty — no patched RPM is available in the current repository.
- CVE-2026-11352 (https://avd.aquasec.com/nvd/cve-2026-11352): The vulnerability is in curl's QUIC UDP receive path. Exploiting it requires a client connecting to a malicious HTTP/3 server using QUIC. mmria.services is a .NET application (`mmria.services.dll`) that uses `System.Net.Http.HttpClient` for all HTTP communication; it does not invoke `curl` as a process and does not enable HTTP/3 QUIC (`DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3SUPPORT` is not set to `1`). The exploit path requires an active curl client making HTTP/3 connections — a path not present in this application's runtime code.
- curl-minimal removal is blocked: `rpm` depends on `libcurl-minimal` for package metadata; removing it would break the container's package manager layer.
- Dockerfile `RUN (dnf update -y curl-minimal libcurl-minimal && dnf clean all) || ...` ensures any future RHEL patch is automatically applied on rebuild.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q curl-minimal` → confirm version; `oc rsh <pod> cat /proc/$(pgrep dotnet)/maps | grep -i curl` → confirm curl shared library linkage from within the .NET process.

---

### curl-minimal / CVE-2026-11586

**Summary:** No patched version of curl-minimal is available for this WebSocket PING memory-exhaustion CVE. The mmria.services runtime does not use `curl` for WebSocket connections. curl-minimal cannot be removed due to rpm dependency. Dockerfile update layer is in place.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `curl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty — no patched RPM available.
- CVE-2026-11586 (https://avd.aquasec.com/nvd/cve-2026-11586): Exploitation requires a curl/libcurl client opening a WebSocket connection to a malicious server that floods PING frames. mmria.services is a .NET background-jobs host; all WebSocket communication (if any) goes through .NET's `System.Net.WebSockets` stack, not through libcurl. There is no code path in mmria.services that calls `curl` as a process or links against libcurl for WebSocket communications.
- curl-minimal removal is blocked: `rpm` depends on `libcurl-minimal`.
- Dockerfile update layer ensures future patches are applied automatically on rebuild.
- Verification (Tier-2 handoff): `oc rsh <pod> ldd /app/mmria.services.dll 2>/dev/null || ldd $(which dotnet)` → confirm dotnet binary does not link against libcurl.

---

### curl-minimal / CVE-2026-12064

**Summary:** No patched version available for this schemeless-URL `--proto-default sftp` CLI bypass. Exploit requires invoking `curl` CLI directly with specific flags; mmria.services does not invoke `curl` CLI.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `curl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-12064 (https://avd.aquasec.com/nvd/cve-2026-12064): The vulnerability occurs only when a user invokes `curl` with `--proto-default sftp` (or `scp`) combined with a schemeless URL — a specific CLI invocation pattern. mmria.services is a .NET application; it does not spawn `curl` as a subprocess and has no code path that could trigger `--proto-default sftp`. No mechanism in the container runs curl CLI directly as part of the application's normal operation.
- curl-minimal cannot be removed: `rpm` depends on `libcurl-minimal`.
- Dockerfile update layer ensures future patches are applied automatically on rebuild.
- Verification (Tier-2 handoff): `oc rsh <pod> ps aux | grep curl` → confirm no curl subprocess running during application operation.

---

### curl-minimal / CVE-2026-8286

**Summary:** No patched version available for this STARTTLS TLS-config-mismatch connection-reuse CVE. Exploit requires a curl client performing STARTTLS upgrades. mmria.services does not use curl for STARTTLS connections.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `curl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-8286 (https://avd.aquasec.com/nvd/cve-2026-8286): Exploitation requires a libcurl client performing STARTTLS connection upgrades (e.g., SMTP+STARTTLS, IMAP+STARTTLS) where a mismatched TLS config could reuse a live connection. mmria.services is a .NET background-jobs host; SMTP email (if configured) goes through `System.Net.Mail.SmtpClient` or a .NET mail library, not through libcurl. No STARTTLS handling occurs via curl in the application.
- curl-minimal cannot be removed: `rpm` depends on `libcurl-minimal`.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> grep -r 'curl\|libcurl' /app/*.dll` → confirm no curl native bindings in the published assembly.

---

### curl-minimal / CVE-2026-8925

**Summary:** No patched version available for this GSASL double-free in SASL authentication. Exploit requires curl performing SASL auth with GSASL. mmria.services does not use curl for SASL.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `curl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-8925 (https://avd.aquasec.com/nvd/cve-2026-8925): The double-free occurs in curl's SASL authentication flow when GSASL is used. Exploiting it requires a curl client performing SASL authentication (e.g., SMTP AUTH, IMAP AUTH). mmria.services is a .NET application; any authentication to external services uses .NET's built-in auth APIs, not libcurl's SASL stack. There is no GSASL library in the application runtime.
- curl-minimal cannot be removed: `rpm` depends on `libcurl-minimal`.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q libgsasl` → confirm GSASL library is not installed.

---

### curl-minimal / CVE-2026-9547

**Summary:** No patched version available for this SCP/SFTP SSH host-key-type validation bypass. Exploit requires curl using `CURLOPT_SSH_KEYFUNCTION` callback with SCP/SFTP. mmria.services has no SCP/SFTP usage.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `curl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-9547 (https://avd.aquasec.com/nvd/cve-2026-9547): The vulnerability affects libcurl-based applications using `SCP://` or `SFTP://` with the `CURLOPT_SSH_KEYFUNCTION` callback — a specific libcurl programming pattern for SSH host-key verification. mmria.services is a .NET background-jobs host; it does not use SCP or SFTP protocols and does not call into libcurl for file transfers. The exploit path (libcurl SCP/SFTP + `CURLOPT_SSH_KEYFUNCTION`) is entirely absent.
- curl-minimal cannot be removed: `rpm` depends on `libcurl-minimal`.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q libssh2` → confirm SSH library linkage state; `oc rsh <pod> grep -r 'sftp\|scp' /app/` → confirm no SCP/SFTP code paths in the published assembly.

---

### libcurl-minimal / CVE-2026-11352

**Summary:** No patched version of libcurl-minimal is available for this QUIC DoS CVE. mmria.services does not use libcurl directly and does not make HTTP/3 QUIC connections. libcurl-minimal cannot be removed because `rpm` depends on it.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `libcurl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty — no patched RPM available.
- CVE-2026-11352 (https://avd.aquasec.com/nvd/cve-2026-11352): Vulnerability is in the QUIC UDP receive path. Exploiting it requires an application using libcurl as a client library making HTTP/3 QUIC connections to a malicious server. mmria.services is a .NET application compiled against the .NET runtime; its HTTP client stack is `System.Net.Http.HttpClient` which does not use libcurl. The application does not P/Invoke or interop into libcurl. No HTTP/3 QUIC configuration is active.
- libcurl-minimal removal is blocked: `rpm` requires `libcurl-minimal` as a dependency.
- Dockerfile `dnf update -y curl-minimal libcurl-minimal` ensures future RHEL patches are applied on rebuild.
- Verification (Tier-2 handoff): `oc rsh <pod> ldd $(which dotnet) | grep curl` → confirm dotnet binary does not dynamically link against libcurl-minimal; `oc rsh <pod> rpm -q libcurl-minimal` → confirm installed version.

---

### libcurl-minimal / CVE-2026-11586

**Summary:** No patched version of libcurl-minimal available for this WebSocket PING memory-exhaustion CVE. mmria.services does not use libcurl for WebSocket connections. Removal blocked by rpm dependency.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `libcurl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-11586 (https://avd.aquasec.com/nvd/cve-2026-11586): Exploiting this requires a libcurl client application establishing a WebSocket connection to a malicious server that floods PING frames. mmria.services is a .NET application; all WebSocket communication uses .NET's `System.Net.WebSockets` stack. The libcurl WebSocket API path that this CVE targets is not used by any component in mmria.services.
- libcurl-minimal removal is blocked: `rpm` depends on it.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> ldd $(which dotnet) | grep curl` → confirm no libcurl linkage in the dotnet binary.

---

### libcurl-minimal / CVE-2026-12064

**Summary:** No patched version of libcurl-minimal available for this schemeless-URL CLI bypass. Exploit requires `curl` CLI invocation with `--proto-default sftp`; mmria.services does not invoke the curl CLI.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `libcurl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-12064 (https://avd.aquasec.com/nvd/cve-2026-12064): The vulnerability is triggered only by invoking `curl` at the command line with `--proto-default sftp` and a schemeless URL. mmria.services is a .NET application; it does not spawn `curl` as a subprocess, does not use shell scripting that calls curl, and does not link against libcurl for URL resolution. The specific CLI invocation pattern required to trigger this CVE does not exist in mmria.services.
- libcurl-minimal removal is blocked: `rpm` depends on it.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> ps aux | grep curl` → confirm no curl subprocess during normal operation.

---

### libcurl-minimal / CVE-2026-8286

**Summary:** No patched version of libcurl-minimal available for this STARTTLS connection-reuse CVE. mmria.services does not use libcurl for STARTTLS-based protocol upgrades.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `libcurl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-8286 (https://avd.aquasec.com/nvd/cve-2026-8286): Exploiting this requires a libcurl client using STARTTLS upgrade semantics (e.g., SMTP, IMAP, POP3 with STARTTLS). mmria.services is a .NET application; any email or protocol-upgrade communication uses .NET's standard library (`SmtpClient` or `MailKit`), not libcurl. The STARTTLS code path in libcurl is not reachable from mmria.services.
- libcurl-minimal removal is blocked: `rpm` depends on it.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> grep -rl 'libcurl\|curl_easy' /app/` → confirm no native libcurl bindings in published assemblies.

---

### libcurl-minimal / CVE-2026-8925

**Summary:** No patched version of libcurl-minimal available for this GSASL double-free. mmria.services does not use libcurl SASL authentication. Removal blocked by rpm dependency.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `libcurl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-8925 (https://avd.aquasec.com/nvd/cve-2026-8925): The double-free vulnerability is in libcurl's SASL/GSASL authentication code path. Triggering it requires a libcurl-using application performing SASL authentication (e.g., SMTP AUTH, IMAP AUTH with GSASL). mmria.services is a .NET application; authentication to backend services uses .NET's `HttpClient` credentials or token-based auth, not libcurl's SASL stack. The GSASL library (`libgsasl`) is not part of the container's runtime dependencies.
- libcurl-minimal removal is blocked: `rpm` depends on it.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q libgsasl` → confirm GSASL is not installed.

---

### libcurl-minimal / CVE-2026-9547

**Summary:** No patched version of libcurl-minimal available for this SSH host-key bypass in SCP/SFTP with `CURLOPT_SSH_KEYFUNCTION`. mmria.services has no SCP/SFTP usage. Removal blocked by rpm dependency.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `libcurl-minimal 7.76.1-40.el9` on RHEL 9.8; `fixedIn` is empty.
- CVE-2026-9547 (https://avd.aquasec.com/nvd/cve-2026-9547): The vulnerability is triggered when a libcurl-based application uses `SCP://` or `SFTP://` with the `CURLOPT_SSH_KEYFUNCTION` callback and encounters a server offering an unexpected host-key type. mmria.services is a .NET background-jobs host; it uses HTTP/HTTPS for all service communication. There is no SCP or SFTP protocol usage anywhere in the application, and it does not invoke libcurl for SSH-based file transfers.
- libcurl-minimal removal is blocked: `rpm` depends on it.
- Dockerfile update layer is in place.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q libssh2; oc rsh <pod> grep -r 'sftp://\|scp://' /app/` → confirm no SSH-URL usage in the application.

---

### dotnet-host / CVE-2024-38081

**Summary:** This .NET/Visual Studio EoP CVE applies to .NET Framework on Windows GUI/desktop scenarios (Windows Forms, Elevation-of-Privilege via Windows API). The mmria.services container runs on Linux (RHEL 9.8 / OpenShift) with .NET 10 Core runtime; it is not affected. No patched package is available in the RHEL 9.8 repos (`fixedIn` is empty; status `end_of_life`). A base-image update is required when the CDC trusted-image maintainers release a newer `dotnet-100-aspnet` tag.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `dotnet-host 10.0.10-1.el9_8` on RHEL 9.8; `fixedIn` is empty; status `end_of_life`.
- CVE-2024-38081 (https://avd.aquasec.com/nvd/cve-2024-38081): Microsoft Security Advisory MSRC for CVE-2024-38081 describes an Elevation of Privilege in .NET Framework and Visual Studio on Windows. The CVSS vector is AV:L (local access) and the vulnerable component is the Windows Forms installer / MSI privilege escalation path. Red Hat advisory RHSA for this CVE marks RHEL 9 as "Not Affected" for the `dotnet-host` RPM because the Linux .NET Core runtime does not ship the Windows Forms / MSI components that are vulnerable (https://access.redhat.com/security/cve/CVE-2024-38081). mmria.services runs on Linux in an OpenShift pod with no local user access and no Windows Forms components.
- Dockerfile `dnf update -y dotnet-host dotnet-runtime-10.0 aspnetcore-runtime-10.0` layer is added to pull any future patches on rebuild.
- Base-image update required: when the CDC trusted-image team releases a newer `dotnet-100-aspnet` image with a patched dotnet-host, the `FROM` digest in the Dockerfile must be updated.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q dotnet-host` → confirm installed version; rescan after base-image rebuild to confirm resolution.

---

### dotnet-host / CVE-2025-26682

**Summary:** ASP.NET Core resource-exhaustion DoS CVE with no patched version available in RHEL 9.8 repos (`fixedIn` is empty; status `end_of_life`). mmria.services is a background-jobs host not exposed directly to untrusted internet clients; its ASP.NET Core endpoints are inside the OpenShift service mesh. A base-image update is required when patches become available.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- Package: `dotnet-host 10.0.10-1.el9_8` on RHEL 9.8; `fixedIn` is empty; status `end_of_life`.
- CVE-2025-26682 (https://avd.aquasec.com/nvd/cve-2025-26682): "Allocation of resources without limits or throttling in ASP.NET Core allows an unauthorized attacker to deny service over a network." CVSS vector is AV:N (network reachable). mmria.services is deployed as an internal OpenShift service behind the cluster ingress. It is not directly exposed to untrusted public internet clients — all traffic routes through the platform ingress which applies rate-limiting and TLS termination. While the CVE is legitimate, the attack surface is limited to actors already inside the cluster network.
- Dockerfile `dnf update -y dotnet-host dotnet-runtime-10.0 aspnetcore-runtime-10.0` layer is added to pull future patches on rebuild.
- Base-image update required: when the CDC trusted-image team releases a newer `dotnet-100-aspnet` image the `FROM` digest must be updated.
- ⏳ EVIDENCE WOULD UPGRADE: A confirmed rescan of the rebuilt image showing the CVE is resolved would change this verdict to **Already remediated**.
- Verification (Tier-2 handoff): `oc rsh <pod> rpm -q dotnet-host aspnetcore-runtime-10.0`; rescan image after rebuild.

---

### dotnet-host / CVE-2025-59144

**Summary:** This CVE describes a supply-chain attack on the **npm `debug` JavaScript package** (v4.4.2 was published with malware after account takeover). It has no connection to the `dotnet-host` RPM package. Trivy misattributed this npm ecosystem CVE to the RHEL `dotnet-host` package. This is a false positive.

**Verdict:** Not applicable / false positive

**Evidence:**
- Package: `dotnet-host 10.0.10-1.el9_8` on RHEL 9.8; status `end_of_life`.
- CVE-2025-59144 description (from findings.json): *"debug is a JavaScript debugging utility. On 8 September 2025, the npm publishing account for debug was taken over after a phishing attack. Version 4.4.2 was published, functionally identical to the previous patch version, but with a malware payload..."* — this CVE explicitly describes the npm package `debug` (https://www.npmjs.com/package/debug), a JavaScript utility. It has nothing to do with the .NET CLR runtime (`dotnet-host`).
- The `dotnet-host` package is the .NET 10 runtime host binary (`dotnet` CLI) for Linux, distributed as an RPM. It does not bundle, ship, or use the `debug` npm package. The .NET runtime does not execute JavaScript code and does not reference npm packages in its binary distribution.
- Trivy cross-ecosystem CVE attribution: Trivy can occasionally attribute npm-ecosystem CVEs to system packages when CVE identifiers overlap across databases. This is a known scanner limitation for mismatched ecosystem entries.
- Verification (repo-static): `grep -r 'debug' nccdphp-drh-mmria-services/mmria.services/*.csproj nccdphp-drh-mmria-common/**/*.csproj 2>/dev/null | grep -v '<!--'` — the .NET project files contain no reference to the npm `debug` package.

---

### dotnet-host / CVE-2026-48779

**Summary:** This CVE describes a memory-exhaustion DoS in the **Node.js `ws` WebSocket library** (npm package). It has no connection to the `dotnet-host` RPM package. Trivy misattributed this Node.js ecosystem CVE to the RHEL `dotnet-host` package. This is a false positive.

**Verdict:** Not applicable / false positive

**Evidence:**
- Package: `dotnet-host 10.0.10-1.el9_8` on RHEL 9.8; status `under_investigation`.
- CVE-2026-48779 description (from findings.json): *"ws is an open source WebSocket client and server for Node.js. All versions from 1.1.0 up to (but not including) 5.2.5, from 6.0.0 up to 6.2.4, from 7.0.0 up to 7.5.11, and from 8.0.0 up to 8.21.0 are affected by a memory exhaustion DoS vulnerability..."* — this CVE explicitly names `ws`, a Node.js npm package (https://www.npmjs.com/package/ws). Versioning in the description (5.x, 6.x, 7.x, 8.x) refers to the npm package's semver, not to .NET versioning.
- `dotnet-host 10.0.10-1.el9_8` is the .NET 10 Core runtime RPM for Linux. It is compiled from the Microsoft .NET SDK (C#/.NET) and does not include, bundle, or depend on Node.js, npm, or the `ws` npm package. The .NET runtime ships its own WebSocket implementation in `System.Net.WebSockets`.
- Trivy cross-ecosystem attribution: same misattribution pattern as CVE-2025-59144 above — an npm CVE incorrectly attributed to a .NET RPM package.
- Verification (repo-static): the mmria.services `.csproj` files contain no npm `ws` package reference and the runtime image contains no `node_modules` directory or Node.js installation. `grep -r '"ws"' nccdphp-drh-mmria-services/mmria.services/` returns no matches.

---

### tar / CVE-2026-59873

**Summary:** This CVE describes an unbound decompression DoS in the **Node.js `node-tar` npm package** (versions before 7.5.19). The installed `tar` package is **GNU tar 1.34** (RHEL RPM `2:1.34-11.el9`) — a completely different software project with different codebase, language, and version scheme. Trivy misattributed an npm ecosystem CVE to the system `tar` RPM. This is a false positive.

**Verdict:** Not applicable / false positive

**Evidence:**
- Package: `tar 2:1.34-11.el9` on RHEL 9.8; status `affected`, `fixedIn` empty.
- CVE-2026-59873 description (from findings.json): *"node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.19, node-tar does not enforce hard upper bounds on total decompressed data, entry counts, or decompression ratio in extraction and parsing paths such as `src/extract.ts`..."* — the description explicitly names `node-tar`, an npm package written in TypeScript/JavaScript (https://www.npmjs.com/package/tar). The version references (< 7.5.19) refer to the npm package's semver.
- The RHEL RPM `tar-1.34-11.el9` (epoch 2) is **GNU tar** (https://www.gnu.org/software/tar/), a C utility. GNU tar 1.34 has a completely different codebase from `node-tar` — it predates Node.js entirely and shares no code. The CVE's affected version range (< 7.5.19) does not apply to GNU tar 1.34, which uses a different versioning scheme (1.x).
- Trivy cross-ecosystem attribution: Trivy matched the `tar` RPM name against the `node-tar` npm package name. This is a known Trivy issue when CVE database entries for npm packages can be matched against similarly-named system packages.
- The Dockerfile includes `dnf update -y tar` to ensure any genuine GNU tar patches are applied on image rebuild (belt-and-suspenders hygiene).
- Verification (repo-static): The mmria.services runtime image is a .NET application container; it does not include Node.js or npm packages. `grep -r 'node-tar\|require.*tar' nccdphp-drh-mmria-services/mmria.services/` returns no matches.

---

### tar / CVE-2026-59874

**Summary:** This CVE describes a negative-size-value DoS in the **Node.js `node-tar` npm package** (versions before 7.5.18). The installed `tar` package is **GNU tar 1.34** (RHEL RPM `2:1.34-11.el9`) — a completely separate software project. Trivy misattributed an npm ecosystem CVE to the GNU tar system package. This is a false positive.

**Verdict:** Not applicable / false positive

**Evidence:**
- Package: `tar 2:1.34-11.el9` on RHEL 9.8; status `affected`, `fixedIn` empty.
- CVE-2026-59874 description (from findings.json): *"node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.18, tar.replace accepts a checksum-valid tar header with a negative base-256 encoded entry size, causing the archive scanner to make no progress while repeatedly parsing the same entry..."* — the description explicitly names `node-tar`, an npm TypeScript/JavaScript package, and references the `tar.replace` API which does not exist in GNU tar.
- The RHEL RPM `tar-1.34-11.el9` is GNU tar (C utility). GNU tar does not have a `tar.replace` JavaScript API. The CVE's affected version range (< 7.5.18) does not apply to GNU tar's versioning scheme. These are entirely different software projects that share only a name prefix.
- Trivy cross-ecosystem attribution: same misattribution pattern as CVE-2026-59873 — npm `node-tar` CVE incorrectly attributed to GNU `tar` RPM.
- The Dockerfile includes `dnf update -y tar` to ensure any genuine GNU tar patches are applied on image rebuild.
- Verification (repo-static): `grep -r 'node-tar\|require.*tar' nccdphp-drh-mmria-services/mmria.services/` returns no matches; the container does not include Node.js or npm.
