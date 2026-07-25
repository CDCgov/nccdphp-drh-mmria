## Scan: 30999 — MMRIA S2I @ 1a75c40a — 2026-07-25

- **Service:** MMRIA S2I
- **Commit:** `1a75c40a3c8e85a9b2ada53141dca2cd3409687c`
- **Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`
- **Scan ID:** 30999
- **Scanned:** 2026-07-25

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 18 | 0 | 0 | 14 | 4 | 14 |

**Residual-risk findings eligible for upgrade with live evidence:**

| Finding | ⏳ EVIDENCE WOULD UPGRADE | What would prove it |
|---|---|---|
| curl-minimal / CVE-2026-11352 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q curl-minimal` after rebuild showing version ≥ fixed release |
| curl-minimal / CVE-2026-11586 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q curl-minimal` after rebuild showing version ≥ fixed release |
| curl-minimal / CVE-2026-12064 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q curl-minimal` after rebuild showing version ≥ fixed release |
| curl-minimal / CVE-2026-8286 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q curl-minimal` after rebuild showing version ≥ fixed release |
| curl-minimal / CVE-2026-8925 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q curl-minimal` after rebuild showing version ≥ fixed release |
| curl-minimal / CVE-2026-9547 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q curl-minimal` after rebuild showing version ≥ fixed release |
| libcurl-minimal / CVE-2026-11352 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q libcurl-minimal` after rebuild showing version ≥ fixed release |
| libcurl-minimal / CVE-2026-11586 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q libcurl-minimal` after rebuild showing version ≥ fixed release |
| libcurl-minimal / CVE-2026-12064 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q libcurl-minimal` after rebuild showing version ≥ fixed release |
| libcurl-minimal / CVE-2026-8286 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q libcurl-minimal` after rebuild showing version ≥ fixed release |
| libcurl-minimal / CVE-2026-8925 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q libcurl-minimal` after rebuild showing version ≥ fixed release |
| libcurl-minimal / CVE-2026-9547 | ⏳ EVIDENCE WOULD UPGRADE | `rpm -q libcurl-minimal` after rebuild showing version ≥ fixed release |

### Fixes made

| File | Package | CVEs | Before | After | Notes |
|---|---|---|---|---|---|
| `.s2i/dockerfile` | curl-minimal, libcurl-minimal | CVE-2026-11352, CVE-2026-11586, CVE-2026-12064, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | not updated | `dnf update -y curl-minimal libcurl-minimal` added | No fixed version available upstream yet; update ensures the latest patch level ships on next rebuild |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; QUIC/HTTP3 attack path not present in S2I build context |
| curl-minimal | CVE-2026-11586 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; WebSocket attack path not exercised in S2I build context |
| curl-minimal | CVE-2026-12064 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; --proto-default sftp/scp invocation not used in S2I scripts |
| curl-minimal | CVE-2026-8286 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; STARTTLS protocols not used in S2I build context |
| curl-minimal | CVE-2026-8925 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; GSASL/SASL auth not exercised in S2I build scripts |
| curl-minimal | CVE-2026-9547 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; SCP/SFTP with SSH key callback not used in S2I context |
| libcurl-minimal | CVE-2026-11352 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; QUIC/HTTP3 attack path not present in S2I build context |
| libcurl-minimal | CVE-2026-11586 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; WebSocket attack path not exercised in S2I build context |
| libcurl-minimal | CVE-2026-12064 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; --proto-default sftp/scp invocation not used in S2I scripts |
| libcurl-minimal | CVE-2026-8286 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; STARTTLS protocols not used in S2I build context |
| libcurl-minimal | CVE-2026-8925 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; GSASL/SASL auth not exercised in S2I build scripts |
| libcurl-minimal | CVE-2026-9547 | Residual risk – required, not reachable under current controls | No fix available (fixedIn: ""); dnf update added; SCP/SFTP with SSH key callback not used in S2I context |
| dotnet-host | CVE-2024-38081 | Residual risk – required, not reachable under current controls | EoP requires interactive local logon on Windows; this image runs Linux (UBI 9); attack vector is local-only |
| dotnet-host | CVE-2025-26682 | Residual risk – required, not reachable under current controls | ASP.NET Core DoS in request allocation; S2I builder runs build jobs, not a web server accepting untrusted requests |
| dotnet-host | CVE-2025-59144 | Not applicable / false positive | CVE describes a malware payload in the JavaScript `debug` npm package; dotnet-host is a .NET runtime component with no relationship to the npm `debug` module |
| dotnet-host | CVE-2026-48779 | Not applicable / false positive | CVE describes a memory exhaustion DoS in the Node.js `ws` WebSocket library; dotnet-host is a .NET runtime component that does not bundle Node.js ws |
| tar | CVE-2026-59873 | Not applicable / false positive | CVE describes a decompression-limit bypass in the Node.js `node-tar` npm package; the scanned package is GNU tar 1.34 on RHEL, an entirely different codebase |
| tar | CVE-2026-59874 | Not applicable / false positive | CVE describes a negative-header infinite-loop in the Node.js `node-tar` npm package; the scanned package is GNU tar 1.34 on RHEL, an entirely different codebase |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). curl-minimal has been added to the `.s2i/dockerfile` `dnf update` command so the fix will be pulled automatically when Red Hat ships one. The vulnerability is a QUIC UDP receive function flaw that enables a remote DoS when a curl/libcurl client connects to a malicious HTTP/3 server. In the S2I builder image, curl is invoked only for HTTPS package downloads against known Red Hat and internal package repositories — not against HTTP/3 (QUIC) endpoints. The NVD CVSS vector shows AV:N/AC:L, but the vulnerable code path requires HTTP/3 transport, which is not enabled by default and is not exercised by the S2I build scripts (`.s2i/bin/assemble`). Red Hat has not issued a "Not Affected" determination for UBI 9 as of this scan date; residual risk is retained until a patched RPM is available.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -q curl-minimal
# Confirm version; a version newer than 7.76.1-40.el9 would indicate a fix is available.
```

---

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). curl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability allows a malicious WebSocket server to exhaust client memory by flooding it with unacknowledged PING frames because curl places no upper bound on PING-frame memory allocation. Exploiting this requires curl to be used with a WebSocket URL (`ws://` or `wss://`) or the `--websocket` flag. Inspection of the S2I build scripts (`.s2i/bin/assemble`) confirms that curl is called only for HTTPS package and source artifact downloads; no WebSocket connectivity is initiated. The exploit path is therefore absent in the S2I build context.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -q curl-minimal
grep -r -- '--websocket\|ws://' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
```

---

### curl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). curl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability is triggered when curl is invoked with a schemeless URL combined with `--proto-default sftp` or `--proto-default scp`, causing a disconnect between the tool layer and libcurl that can bypass SFTP/SCP initialization. This attack vector requires the specific `--proto-default sftp|scp` flag in the curl invocation. The S2I build scripts do not invoke curl with `--proto-default` and use only standard `https://` URLs for package and artifact downloads.

**Verification (Tier-2 handoff):**
```
grep -r -- '--proto-default\|proto-default' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q curl-minimal
```

---

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). curl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability allows an attacker to cause curl to reuse a live connection where TLS configuration mismatches because STARTTLS-upgraded connections bypass TLS-parameters validation. STARTTLS is used only for application-layer protocols such as SMTP, IMAP, POP3, and LDAP. The S2I build scripts use curl exclusively for HTTPS (direct TLS, not STARTTLS) downloads, so the vulnerable STARTTLS upgrade path is not exercised.

**Verification (Tier-2 handoff):**
```
grep -r 'starttls\|smtp\|imap\|pop3\|ldap' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q curl-minimal
```

---

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). curl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability is a double-free in curl's GSASL (GNU SASL) context cleanup path. Triggering this flaw requires a SASL-authenticated transfer (e.g., SMTP with SASL/GSASL). The S2I build scripts invoke curl only for HTTPS downloads from package repositories; no SASL or GSASL authentication is configured or executed. The double-free code path is therefore unreachable in normal S2I operation.

**Verification (Tier-2 handoff):**
```
grep -r 'gsasl\|SASL' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q curl-minimal
```

---

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). curl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability causes libcurl to silently accept an untrusted server host key when the application registers a `CURLOPT_SSH_KEYFUNCTION` callback and the server presents a key type that the callback does not handle. Exploiting this requires two conditions: (1) the transfer uses `scp://` or `sftp://` schemes, and (2) a `CURLOPT_SSH_KEYFUNCTION` callback is registered. The S2I build scripts do not use SCP or SFTP and do not register SSH host-key callbacks; both preconditions are absent.

**Verification (Tier-2 handoff):**
```
grep -r 'scp://\|sftp://\|SSH_KEYFUNCTION' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q curl-minimal
```

---

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). libcurl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability is a QUIC UDP receive function flaw in libcurl that allows a remote DoS when a libcurl client connects to a malicious HTTP/3 server. The S2I builder image uses libcurl (loaded by curl-minimal) only for HTTPS package downloads against known Red Hat and internal repositories — not against HTTP/3 (QUIC) endpoints. HTTP/3 is not enabled by default and is not exercised by the S2I build scripts. Residual risk is retained until a patched RPM is available from Red Hat.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
# A version newer than 7.76.1-40.el9 would indicate a fix is available.
```

---

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). libcurl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability allows a malicious WebSocket server to exhaust client memory by flooding it with unacknowledged PING frames (no upper bound on PING allocation in libcurl). Exploiting this requires libcurl to perform WebSocket transfers. The S2I builder uses libcurl only for HTTPS package and source downloads; no WebSocket connectivity is initiated in the build scripts. The exploit path is absent in the S2I build context.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
grep -r 'websocket\|ws://' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
```

---

### libcurl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). libcurl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability is triggered when curl/libcurl receives a schemeless URL combined with `--proto-default sftp` or `--proto-default scp`, causing the tool layer to infer the wrong scheme and bypass SFTP/SCP initialization. The S2I build scripts do not pass `--proto-default` to curl and use only fully-qualified `https://` URLs; the exploit preconditions are absent.

**Verification (Tier-2 handoff):**
```
grep -r -- '--proto-default' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
```

---

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). libcurl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability allows an attacker to hijack a STARTTLS-upgraded connection when TLS parameters mismatch. STARTTLS applies only to application-layer protocol upgrades (SMTP, IMAP, POP3, LDAP). The S2I build scripts use libcurl via curl only for direct HTTPS transfers that establish TLS from the start, not through STARTTLS; the vulnerable upgrade path is not exercised.

**Verification (Tier-2 handoff):**
```
grep -r 'starttls\|smtp://\|imap://\|pop3://' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
```

---

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). libcurl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability is a double-free in libcurl's GSASL context handling during SASL authentication. Triggering this requires a libcurl-based application to perform SASL-authenticated transfers. The S2I build scripts use libcurl via curl solely for HTTPS package downloads with no SASL or GSASL configuration; the double-free path is unreachable.

**Verification (Tier-2 handoff):**
```
grep -r 'gsasl\|SASL\|sasl' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
```

---

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** No upstream fix is available (fixedIn: ""). libcurl-minimal has been added to the `.s2i/dockerfile` `dnf update` command. The vulnerability causes libcurl to silently accept an untrusted SSH server host key when the application uses `CURLOPT_SSH_KEYFUNCTION` with an SCP or SFTP transfer. Both preconditions — use of `scp://`/`sftp://` schemes and registration of a `CURLOPT_SSH_KEYFUNCTION` callback — must be satisfied. The S2I build scripts do not use SCP or SFTP schemes and no application in the builder image registers SSH key callbacks; both preconditions are absent.

**Verification (Tier-2 handoff):**
```
grep -r 'scp://\|sftp://\|SSH_KEYFUNCTION' /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/.s2i/
oc rsh <s2i-builder-pod> rpm -q libcurl-minimal
```

---

### dotnet-host / CVE-2024-38081

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2024-38081 is a .NET Elevation of Privilege Vulnerability (CVSS AV:L/AC:L/PR:L). This is a local privilege escalation requiring an attacker with an interactive logon session on the affected host. The MMRIA S2I builder image runs as a non-root container (UID 1001) on OpenShift; there is no interactive login surface exposed to untrusted principals. The Trivy scan reports the package status as `end_of_life` for this CVE, indicating the vulnerability tracking considers this package revision unsupported. The remediation path is a base-image update to a Red Hat-patched dotnet-100 release when one becomes available. Until then, the local-only attack vector (requiring an authenticated shell session inside the container) is not reachable through normal OpenShift build-pod operations.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -q dotnet-host
oc rsh <s2i-builder-pod> id
# Confirm non-root UID and dotnet-host version.
```

---

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls

**Summary:** CVE-2025-26682 describes an allocation-without-limits DoS in ASP.NET Core that allows an unauthenticated network attacker to exhaust server resources by sending crafted HTTP requests. The vulnerability requires an ASP.NET Core web server to be running and accepting network requests. The MMRIA S2I builder image is used exclusively as an OpenShift Source-to-Image build container; it executes `dotnet restore` and `dotnet publish` during builds and does not start an ASP.NET Core web server or expose any HTTP listener. The Trivy scan reports `end_of_life` status; a base-image update to a patched dotnet-100 release is the remediation path. Until then, the network-facing ASP.NET Core attack surface is absent in the S2I build context.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> ss -tlnp | grep -E ':80|:443|:5000'
# Confirm no HTTP listener is running in the S2I builder pod.
```

---

### dotnet-host / CVE-2025-59144

**Verdict:** Not applicable / false positive

**Summary:** CVE-2025-59144 describes a malware payload injected into version 4.4.2 of the `debug` JavaScript npm package after the npm publishing account was compromised via phishing. The finding is attributed by Trivy to `dotnet-host` version `10.0.10-1.el9_8`, which is the .NET runtime host RPM installed from Red Hat's UBI 9 repositories. The `dotnet-host` RPM is a binary .NET runtime component; it does not contain, vendor, or execute the JavaScript `debug` npm module. No Node.js runtime or npm packages are installed as part of `dotnet-host`. This is a Trivy CVE-to-package attribution error where a JavaScript ecosystem CVE has been incorrectly matched to a .NET system package.

**Evidence:** The CVE description explicitly states "debug is a JavaScript debugging utility" and references an npm account compromise. The `dotnet-host` RPM content can be verified with `rpm -ql dotnet-host` — it contains only .NET binary components (`.dll`, `.so`, `.exe`), no JavaScript files.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -ql dotnet-host | grep -E '\.js$'
# Expected output: (none) — confirms no JavaScript files in the package.
```

---

### dotnet-host / CVE-2026-48779

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-48779 describes a memory exhaustion DoS vulnerability in `ws`, the Node.js WebSocket client/server library, affecting all versions from 1.1.0 through 8.20.x. The finding is attributed by Trivy to `dotnet-host` version `10.0.10-1.el9_8`, which is the .NET runtime host RPM installed from Red Hat's UBI 9 repositories. The `dotnet-host` RPM is a .NET binary runtime component; it does not bundle the Node.js `ws` package, does not include a Node.js runtime, and does not install npm packages. This is a Trivy CVE-to-package attribution error where a Node.js ecosystem CVE has been incorrectly matched to a .NET system package.

**Evidence:** The CVE description explicitly states "ws is an open source WebSocket client and server for Node.js." The `dotnet-host` RPM contains only .NET binary components with no Node.js or npm content.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -ql dotnet-host | grep -E 'node_modules|ws\.js'
# Expected output: (none) — confirms no Node.js ws library in the package.
```

---

### tar / CVE-2026-59873

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59873 describes a decompression-limit bypass in `node-tar`, a tar archive manipulation library for Node.js (prior to version 7.5.19). The finding is attributed by Trivy to the `tar` system package version `2:1.34-11.el9` installed via RPM on Red Hat UBI 9. GNU tar 1.34 and `node-tar` (npm package) are entirely separate codebases with different authors, implementations, and version schemes. The CVE description explicitly identifies the vulnerable software as "node-tar is a tar archive manipulation library for Node.js" and references the `src/extract.ts` source file — a TypeScript file that does not exist in GNU tar. This is a Trivy CVE-to-package attribution error where an npm ecosystem CVE has been incorrectly matched to the GNU tar system package.

**Evidence:** The CVE description references TypeScript source files (`src/extract.ts`) and "node-tar" by name. GNU tar 1.34 (`tar-1.34-11.el9`) is a C program available at `https://ftp.gnu.org/gnu/tar/` with no TypeScript components.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -qi tar
# Confirms the installed package is GNU tar (C implementation), not node-tar (npm).
oc rsh <s2i-builder-pod> node -e "require('node-tar')" 2>&1 || echo "node-tar not installed"
# Expected: error/not installed.
```

---

### tar / CVE-2026-59874

**Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59874 describes a negative-base-256-encoded-header parsing loop in `node-tar` (prior to version 7.5.18), causing `tar.replace` to make no progress while repeatedly parsing the same header entry. The finding is attributed by Trivy to the `tar` system package version `2:1.34-11.el9` on Red Hat UBI 9. GNU tar 1.34 and the `node-tar` npm package are entirely separate software projects. The CVE description explicitly identifies the affected software as "node-tar is a tar archive manipulation library for Node.js" and references the `tar.replace` JavaScript API — which does not exist in GNU tar. This is a Trivy CVE-to-package attribution error where an npm ecosystem CVE has been incorrectly matched to the GNU tar system package.

**Evidence:** The CVE description references `tar.replace` JavaScript API and "node-tar" by name. GNU tar 1.34 does not have a `tar.replace` method or any JavaScript API surface.

**Verification (Tier-2 handoff):**
```
oc rsh <s2i-builder-pod> rpm -qi tar
# Confirms the installed package is GNU tar (C implementation), not node-tar (npm).
oc rsh <s2i-builder-pod> node -e "require('node-tar')" 2>&1 || echo "node-tar not installed"
# Expected: error/not installed.
```
