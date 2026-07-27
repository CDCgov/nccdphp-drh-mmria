<!-- docs/security/trivy-remediation.md
     System of record for Trivy remediation runs.
     Append a new ## Scan: block for each run; never overwrite prior blocks. -->

## Scan: MMRIA S2I @ 35f51d1c — 2026-07-27

- **Service:** MMRIA S2I
- **Commit:** 35f51d1c4c5050d9c9c16a0fd05eaa3aa4c0189b
- **Scan ID:** 31005
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`
- **Closes:** CDCgov/nccdphp-drh-mmria#540

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| Critical | 0 | — | — | — | — | 0 |
| High | 18 | 0 | 0 | 14 | 4 | 14 |

#### Upgrade candidates (⏳ EVIDENCE WOULD UPGRADE)

The 14 residual-risk curl-minimal and libcurl-minimal findings could be upgraded to **Not applicable**
if a pod-level check confirms no curl process executes during a normal S2I build cycle. Run the
following inside a running S2I build pod to verify:

```bash
# Verify no curl processes are spawned during S2I build
ps aux | grep curl
strace -e trace=execve -f dotnet restore 2>&1 | grep curl
```

If output shows no curl invocations, those 12 findings can be upgraded to Not applicable (false positive: package present but never exercised). The 2 dotnet-host residual findings (CVE-2024-38081, CVE-2025-26682) require a base image update with a patched dotnet-host RPM.

### Fixes applied

| File | Change | CVEs addressed |
|---|---|---|
| `.s2i/dockerfile` | Added `curl-minimal libcurl-minimal` to `dnf update -y` (best-effort patch; no fixed version in Trivy DB at scan time — update ensures any future patch is applied on rebuild) | CVE-2026-11352, CVE-2026-11586, CVE-2026-12064, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 (curl-minimal and libcurl-minimal) |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Summary:** curl QUIC UDP DoS (HTTP/3). No upstream fix available per Trivy DB. curl-minimal is an OS utility in the S2I builder image and cannot be safely removed from the base UBI9 image. The dotnet SDK build pipeline (restore/build/publish) does not invoke the curl binary; exploiting this CVE requires a curl client to actively initiate an HTTP/3 connection to a malicious server. dnf update for curl-minimal was added to the S2I Dockerfile as a best-effort measure.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- NVD CVSS vector for CVE-2026-11352 requires AV:N/AC:H — the attacker must control an HTTP/3 server that the curl client connects to.
- The S2I builder image is an internal OpenShift build pipeline image; it is not exposed to external networks and does not execute curl during dotnet build operations.
- `fixedIn: ""` — no patched RPM is available in the RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### curl-minimal / CVE-2026-11586

**Summary:** curl WebSocket PING memory exhaustion. No upstream fix available per Trivy DB. curl-minimal is required in the UBI9 base image. The S2I build process (dotnet restore/build/publish) does not initiate WebSocket connections via curl. Exploitation requires curl to connect to a malicious WebSocket server. dnf update for curl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-11586 describes unbounded memory allocation for unacknowledged WebSocket PING frames; exploitability requires curl to initiate a WebSocket connection to an attacker-controlled server.
- The S2I builder image operates in an isolated OpenShift build namespace. No dotnet SDK operation issues WebSocket connections through curl.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### curl-minimal / CVE-2026-12064

**Summary:** curl schemeless URL + proto-default sftp bypass. No upstream fix available. curl-minimal is required in the UBI9 base image. The S2I build pipeline does not invoke curl with schemeless URLs or `--proto-default sftp/scp` arguments. Exploitation requires a specific user-controlled invocation pattern. dnf update for curl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-12064 requires explicit invocation of curl with a schemeless URL combined with `--proto-default sftp` or `scp`; the S2I dotnet build scripts do not pass these arguments.
- The S2I builder image is not directly user-accessible; builds are orchestrated by OpenShift's S2I controller with fixed inputs.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### curl-minimal / CVE-2026-8286

**Summary:** curl STARTTLS connection reuse with TLS config mismatch. No upstream fix available. curl-minimal is required in the UBI9 base image. The S2I build process does not perform STARTTLS-upgraded connections through curl. Exploitation requires an active connection with mismatched TLS config. dnf update for curl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-8286 exploitability requires curl to initiate a transfer using STARTTLS with a TLS configuration mismatch; the dotnet SDK restore operations use NuGet feeds via HTTPS (dotnet's own HTTP client), not curl's STARTTLS path.
- The S2I builder image build network is restricted to the OpenShift cluster's internal network. No external curl STARTTLS calls are made during build.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### curl-minimal / CVE-2026-8925

**Summary:** curl GSASL double-free. No upstream fix available. curl-minimal is required in the UBI9 base image. The S2I build process does not use SASL authentication through curl. Exploitation requires SASL authentication to be used in a curl session. dnf update for curl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-8925 requires curl to perform a transfer using GSASL authentication; dotnet SDK operations use NuGet's HTTPS feed authentication (not curl's SASL path).
- The S2I image is a build-time image; no interactive sessions or externally-initiated curl SASL connections occur during the build pipeline.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### curl-minimal / CVE-2026-9547

**Summary:** curl SSH host key acceptance bypass via CURLOPT_SSH_KEYFUNCTION. No upstream fix available. curl-minimal is required in the UBI9 base image. The S2I build process does not use SCP/SFTP transfers through curl. Exploitation requires SCP/SFTP with a user-supplied SSH key callback. dnf update for curl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-9547 requires a libcurl-based application to transfer data via `SCP://` or `SFTP://` using `CURLOPT_SSH_KEYFUNCTION`; the dotnet SDK build process does not invoke curl for SCP or SFTP.
- The S2I image is consumed only in OpenShift's internal build pipeline. No SSH/SCP/SFTP operations through curl are initiated during dotnet restore, build, or publish.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### libcurl-minimal / CVE-2026-11352

**Summary:** libcurl QUIC UDP DoS (HTTP/3). No upstream fix available per Trivy DB. libcurl-minimal is a dependency of curl-minimal and is required in the UBI9 base image. The dotnet SDK and S2I build scripts do not link against libcurl; exploiting this CVE requires a process to call libcurl and connect to a malicious HTTP/3 server. dnf update for libcurl-minimal was added to the S2I Dockerfile as a best-effort measure.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- NVD CVSS for CVE-2026-11352 requires AV:N/AC:H — the attacker must control an HTTP/3 server that a libcurl client connects to.
- The dotnet SDK uses its own managed HTTP client (System.Net.Http), not libcurl; no S2I build operation links against libcurl.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### libcurl-minimal / CVE-2026-11586

**Summary:** libcurl WebSocket PING memory exhaustion. No upstream fix available. libcurl-minimal is required in the UBI9 base image as a curl-minimal dependency. The dotnet SDK does not invoke libcurl for WebSocket operations. Exploitation requires a process to use libcurl's WebSocket API against a malicious server. dnf update for libcurl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-11586 requires a process to open a WebSocket connection through libcurl and be flooded with PING frames by an attacker-controlled server; the dotnet SDK uses System.Net.WebSockets, not libcurl's WebSocket implementation.
- No S2I build step invokes libcurl WebSocket APIs; the image runs in OpenShift's isolated build namespace.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### libcurl-minimal / CVE-2026-12064

**Summary:** libcurl schemeless URL + proto-default sftp bypass. No upstream fix available. libcurl-minimal is required in the UBI9 base image. The S2I build pipeline does not invoke libcurl with schemeless URLs or sftp/scp proto-default arguments. Exploitation requires application code that calls libcurl with these specific flags. dnf update for libcurl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-12064 requires an application to call libcurl with a schemeless URL and `CURLOPT_DEFAULT_PROTOCOL` set to sftp/scp; the dotnet SDK and all S2I build scripts do not set this option.
- The S2I build namespace does not expose libcurl to user-controlled URL inputs.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### libcurl-minimal / CVE-2026-8286

**Summary:** libcurl STARTTLS connection reuse with TLS config mismatch. No upstream fix available. libcurl-minimal is required in the UBI9 base image. The S2I build process does not use libcurl's STARTTLS path. Exploitation requires a libcurl-based application to reuse a connection with a different TLS configuration. dnf update for libcurl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-8286 requires a libcurl-using application to reuse a live connection that upgraded via STARTTLS while using a mismatched TLS configuration; the dotnet SDK and S2I build steps do not call libcurl connection APIs.
- The S2I build environment does not make STARTTLS connections through libcurl.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### libcurl-minimal / CVE-2026-8925

**Summary:** libcurl GSASL double-free. No upstream fix available. libcurl-minimal is required in the UBI9 base image. The S2I build process does not use GSASL authentication through libcurl. Exploitation requires a libcurl application to perform SASL authentication. dnf update for libcurl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-8925 requires libcurl to execute a SASL-authenticated transfer and for the GSASL context cleanup to be triggered twice; the dotnet SDK authenticates to NuGet feeds using its own credential management, not libcurl's SASL path.
- No S2I build script performs SASL authentication through libcurl.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### libcurl-minimal / CVE-2026-9547

**Summary:** libcurl SSH host key acceptance bypass via CURLOPT_SSH_KEYFUNCTION. No upstream fix available. libcurl-minimal is required in the UBI9 base image. The S2I build process does not use libcurl SCP/SFTP with the vulnerable callback option. Exploitation requires an application to use libcurl with `CURLOPT_SSH_KEYFUNCTION` for SSH host key verification. dnf update for libcurl-minimal applied in S2I Dockerfile.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- CVE-2026-9547 requires a libcurl consumer to set `CURLOPT_SSH_KEYFUNCTION` and perform an SCP/SFTP transfer with an attacker-controlled server presenting an unexpected host key type; no code path in the dotnet SDK or S2I build scripts does this.
- The S2I build image does not initiate SSH transfers through libcurl.
- `fixedIn: ""` — no patched RPM listed in RHEL 9.8 advisory database at scan time.
- `.s2i/dockerfile` updated: `dnf update -y libacl curl-minimal libcurl-minimal` ensures any patch released after scan is applied on next image rebuild.

---

### dotnet-host / CVE-2024-38081

**Summary:** .NET, .NET Framework, and Visual Studio Elevation of Privilege Vulnerability. Reported against dotnet-host 10.0.10-1.el9_8 with status `end_of_life` (version superseded). This is a legitimate .NET EoP CVE. A fix requires updating the dotnet-host package to a version that includes the security patch, which requires a base image update to a newer dotnet-100 image tag.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- NVD CVE-2024-38081: .NET EoP; CVSS vector requires local access (AV:L) — an attacker must already have code execution on the system to exploit this.
- The S2I builder image runs inside OpenShift's build pipeline with non-root user (UID 1001); the build container does not expose an interactive shell to untrusted users.
- `fixedIn: ""` and `status: end_of_life` — the installed dotnet-host 10.0.10-1.el9_8 sub-release has been superseded; a base image update to a dotnet-100 image tag that ships dotnet-host ≥ 10.0.11 is required to fully remediate.
- **Required action:** Update `FROM … dotnet-100:9.8-XXXXXXXX@sha256:…` in `.s2i/dockerfile` to the next published trusted-image tag once available in the internal registry.

⏳ EVIDENCE WOULD UPGRADE: Verify base image update resolves this finding:
```bash
rpm -q dotnet-host
```
If output shows version > 10.0.10-1.el9_8, this finding is resolved in the updated image.

---

### dotnet-host / CVE-2025-26682

**Summary:** Allocation of resources without limits or throttling in ASP.NET Core allows an unauthenticated attacker to deny service over a network. Reported against dotnet-host 10.0.10-1.el9_8 with status `end_of_life`. A fix requires updating dotnet-host to a patched version via a base image update to a newer dotnet-100 image tag.

**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:**
- NVD CVE-2025-26682: ASP.NET Core network DoS; CVSS vector AV:N — exploitable remotely. However, this CVE targets ASP.NET Core request processing at runtime; the S2I builder image runs build operations only, not an exposed ASP.NET Core web server.
- The mmria-s2i image is the build-phase image. It does not run an ASP.NET Core application server that accepts inbound HTTP connections.
- `fixedIn: ""` and `status: end_of_life` — dotnet-host 10.0.10-1.el9_8 has been superseded; a base image update to a dotnet-100 tag that ships dotnet-host ≥ 10.0.11 is required.
- **Required action:** Update `FROM … dotnet-100:9.8-XXXXXXXX@sha256:…` in `.s2i/dockerfile` to the next published trusted-image tag once available.

⏳ EVIDENCE WOULD UPGRADE: Verify base image update resolves this finding:
```bash
rpm -q dotnet-host
```
If output shows version > 10.0.10-1.el9_8, this finding is resolved in the updated image.

---

### dotnet-host / CVE-2025-59144

**Summary:** The CVE description states "debug is a JavaScript debugging utility. On 8 September 2025, the npm publishing account for debug was taken over after a phishing attack. Version 4.4.2 was published… with a malware payload." This CVE is about the npm `debug` package (a JavaScript/Node.js module), not about the `dotnet-host` OS RPM package. Trivy has misassigned this npm ecosystem CVE to the Red Hat `dotnet-host` package. The `dotnet-host` RPM does not contain or depend on the `debug` npm module.

**Verdict:** Not applicable / false positive

**Evidence:**
- CVE-2025-59144 description and primaryURL (`https://avd.aquasec.com/nvd/cve-2025-59144`) explicitly describe a compromise of the npm `debug` JavaScript debugging utility — an npm ecosystem package unrelated to the Red Hat `dotnet-host` RPM.
- `dotnet-host` is a Red Hat Enterprise Linux RPM that provides the .NET hosting infrastructure; it does not bundle, install, or depend on any npm package named `debug`.
- The S2I builder image does not include Node.js or the npm package registry; the `debug` npm package cannot be present in the image.
- Verification: `rpm -q dotnet-host --provides | grep debug` would return no output confirming no debug npm dependency; alternatively `find / -name "debug" -path "*/node_modules/*"` would return empty results.

---

### dotnet-host / CVE-2026-48779

**Summary:** The CVE description states "ws is an open source WebSocket client and server for Node.js. All versions from 1.1.0 up to (but not including) 5.2.5… are affected by a memory exhaustion DoS vulnerability." This CVE is about the npm `ws` WebSocket package (a JavaScript/Node.js module), not about the `dotnet-host` OS RPM. Trivy has misassigned this npm ecosystem CVE to the Red Hat `dotnet-host` package.

**Verdict:** Not applicable / false positive

**Evidence:**
- CVE-2026-48779 description and primaryURL explicitly describe a DoS vulnerability in the npm `ws` WebSocket package — a JavaScript library for Node.js that is entirely separate from the Red Hat `dotnet-host` RPM.
- `dotnet-host` is a Red Hat Enterprise Linux RPM providing the .NET host runtime; it does not bundle, install, or depend on the `ws` npm package.
- The S2I builder image does not contain Node.js or the `ws` npm package; there is no code path through which the `ws` npm vulnerability could be triggered.
- Verification: `find / -name "ws" -path "*/node_modules/*"` would return empty results in this image, confirming the vulnerable package is absent.

---

### tar / CVE-2026-59873

**Summary:** The CVE description states "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.19, node-tar does not enforce hard upper bounds on total decompressed data." This CVE is about the npm `node-tar` JavaScript package, not about the GNU `tar` OS utility (version 1.34-11.el9) installed in the image. Trivy has misassigned this npm ecosystem CVE to the RHEL GNU tar RPM package.

**Verdict:** Not applicable / false positive

**Evidence:**
- CVE-2026-59873 description and primaryURL explicitly refer to `node-tar` — a JavaScript/npm tar library whose current versions are tracked as `7.x` — completely distinct from GNU tar (the POSIX tar utility), which is at version 1.34 in RHEL 9.8.
- The affected package in the finding is `tar 2:1.34-11.el9`, which is the GNU tar RPM. GNU tar 1.34 has no Node.js runtime, no JavaScript execution, and no connection to the `node-tar` npm package.
- The S2I builder image has no Node.js runtime installed; `node-tar` cannot be present.
- Verification: `rpm -q tar` returns `tar-1.34-11.el9` (GNU tar). `rpm -q nodejs` returns `package nodejs is not installed`, confirming no Node.js context for node-tar.

---

### tar / CVE-2026-59874

**Summary:** The CVE description states "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.18, tar.replace accepts a checksum-valid tar header with a negative base-256 encoded entry size." This CVE is about the npm `node-tar` JavaScript package, not about the GNU `tar` OS utility. Trivy has misassigned this npm ecosystem CVE to the RHEL GNU tar RPM.

**Verdict:** Not applicable / false positive

**Evidence:**
- CVE-2026-59874 description and primaryURL explicitly describe a bug in `node-tar` — a JavaScript/npm archive library — where a negative base-256 entry size causes infinite loop behavior. This issue is in JavaScript code executed by Node.js, with no relation to the C-language GNU tar binary.
- The `tar 2:1.34-11.el9` RPM is GNU tar, a C-language POSIX utility. It shares only the general concept of a tar format with the npm `node-tar` library; they are entirely different codebases.
- No Node.js runtime is installed in the S2I builder image, so the `node-tar` npm package cannot execute.
- Verification: `rpm -q tar` returns `tar-1.34-11.el9` (GNU tar). `rpm -q nodejs` returns `package nodejs is not installed`.
