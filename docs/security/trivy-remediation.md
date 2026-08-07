# Trivy Remediation Records

Records are prepended — newest scan block at the top.

---

## Scan: MMRIA S2I @ 0f96ac0a — 2026-08-07

- **Commit:** `0f96ac0ad6bd7a1522b50a2061ad50c6b96d0e6b`
- **Service:** `MMRIA S2I`
- **Scan ID:** `31297`
- **Severity totals:** C:0  H:14  M:104
- **Scanned image:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`

> **Scope:** This scan block addresses Critical and High findings only, consistent with the
> automated remediation workflow. The 104 Medium findings are not triaged here; they are
> tracked by the scanning pipeline and addressed in a separate review cycle.
>
> **Carry-forward note:** Scan 31297 reports the same 14 High findings, package versions,
> and package statuses already documented for scan 31295. Evidence is carried forward where
> unchanged and corrected below where stronger Red Hat / NVD evidence supports a tighter
> verdict.

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 0 | 0 | 11 | 3 | 11 |

- `⏳ EVIDENCE WOULD UPGRADE — dotnet-host / CVE-2026-48779`: `oc rsh <mmria-s2i-pod> sh -lc "find / -path '*/node_modules/ws*' 2>/dev/null"` proving the `ws` package is absent from the image would upgrade this finding to `Not applicable / false positive`.

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
| dotnet-host | CVE-2024-38081 | 10.0.10-1.el9_8 | — | end_of_life | Not applicable / false positive |
| dotnet-host | CVE-2025-26682 | 10.0.10-1.el9_8 | — | end_of_life | Not applicable / false positive |
| dotnet-host | CVE-2025-59144 | 10.0.10-1.el9_8 | — | end_of_life | Not applicable / false positive |
| dotnet-host | CVE-2026-48779 | 10.0.10-1.el9_8 | — | under_investigation | Residual risk – required, not reachable under current controls |

### Fixes made

- `docs/security/trivy-remediation.md` — prepended the scan 31297 record for commit `0f96ac0a`, carrying forward unchanged curl/libcurl evidence from scan 31295 and tightening the `dotnet-host` verdicts where Red Hat and NVD product-scope evidence now proves false positives.
- `.s2i/dockerfile` — no new file change required in this run. The repository already contains `dnf update -y libacl curl-minimal libcurl-minimal`, which is the only available repo-side mitigation while Red Hat has not published fixed RHEL 9 curl RPMs.

### HIGH / CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | Residual risk – no fix available | Red Hat has no fixed RHEL 9 RPM yet; NVD says exploitation requires a malicious HTTP/3 server; repo build scripts do not enable or invoke HTTP/3. |
| curl-minimal | CVE-2026-11586 | Residual risk – no fix available | Red Hat has no fixed RHEL 9 RPM yet; NVD requires WebSocket PING flooding; repo build scripts make no WebSocket client connections. |
| curl-minimal | CVE-2026-8286 | Residual risk – no fix available | Red Hat has no fixed RHEL 9 RPM yet; NVD requires STARTTLS upgrade reuse; repo build scripts use HTTPS rather than STARTTLS protocols. |
| curl-minimal | CVE-2026-8925 | Residual risk – no fix available | Red Hat has no fixed RHEL 9 RPM yet; NVD requires GSASL/SASL transfer paths; repo build scripts do not perform SASL-authenticated transfers. |
| curl-minimal | CVE-2026-9547 | Residual risk – no fix available | Red Hat has no fixed RHEL 9 RPM yet; NVD requires SCP/SFTP with `CURLOPT_SSH_KEYFUNCTION`; repo build scripts do not use SCP or SFTP. |
| libcurl-minimal | CVE-2026-11352 | Residual risk – no fix available | Same package version and same HTTP/3 precondition as `curl-minimal`; carried from prior scan with unchanged evidence. |
| libcurl-minimal | CVE-2026-11586 | Residual risk – no fix available | Same package version and same WebSocket precondition as `curl-minimal`; carried from prior scan with unchanged evidence. |
| libcurl-minimal | CVE-2026-8286 | Residual risk – no fix available | Same package version and same STARTTLS precondition as `curl-minimal`; carried from prior scan with unchanged evidence. |
| libcurl-minimal | CVE-2026-8925 | Residual risk – no fix available | Same package version and same GSASL/SASL precondition as `curl-minimal`; carried from prior scan with unchanged evidence. |
| libcurl-minimal | CVE-2026-9547 | Residual risk – no fix available | Same package version and same SCP/SFTP precondition as `curl-minimal`; carried from prior scan with unchanged evidence. |
| dotnet-host | CVE-2024-38081 | Not applicable / false positive | NVD affected products stop at .NET 6.0 / Visual Studio 2022; Red Hat describes Visual Studio Installer / NuGet lockfile hijacking; finding is mapped to `dotnet-host` 10.0.10 on RHEL 9. |
| dotnet-host | CVE-2025-26682 | Not applicable / false positive | Red Hat states the issue is exploitable only with HTTP/3 enabled and that RHEL-shipped .NET packages do not support HTTP/3. |
| dotnet-host | CVE-2025-59144 | Not applicable / false positive | NVD says only browser-bundle use of malicious `debug` 4.4.2 is affected; server, local, and CLI environments are not affected. |
| dotnet-host | CVE-2026-48779 | Residual risk – required, not reachable under current controls | NVD and Trivy describe a Node.js `ws` library issue, not a native `dotnet-host` bug; repo contains no Node package manifests, and normal `.s2i/bin/run` execution starts `dotnet`, not Node. Live image inspection is still required to prove the library is absent. |

### curl-minimal / CVE-2026-11352

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD describes this as a QUIC / HTTP/3 client denial of service. The repo's active S2I build path in `.s2i/bin/assemble` restores and publishes the .NET project and does not invoke HTTP/3-specific client code. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `curl-minimal` on RHEL 9.

### curl-minimal / CVE-2026-11586

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD says exploitation requires a client WebSocket connection that receives malicious PING floods. The repo's S2I scripts do not open WebSocket client connections; active build steps use `dotnet restore` / `dotnet publish` and optional npm tooling over HTTPS. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `curl-minimal` on RHEL 9.

### curl-minimal / CVE-2026-8286

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD describes a STARTTLS connection-reuse flaw. The repo's S2I build scripts use HTTPS package feeds and do not perform STARTTLS upgrades for SMTP, IMAP, FTP, or similar protocols. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `curl-minimal` on RHEL 9.

### curl-minimal / CVE-2026-8925

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD describes a double-free in curl's GSASL cleanup path. The repo's S2I build scripts do not perform SASL-authenticated transfers, so the vulnerable code path is not exercised by the documented build flow. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `curl-minimal` on RHEL 9.

### curl-minimal / CVE-2026-9547

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD says exploitation requires SCP or SFTP transfers that use `CURLOPT_SSH_KEYFUNCTION`. The repo's S2I build scripts do not use SCP or SFTP. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `curl-minimal` on RHEL 9.

### libcurl-minimal / CVE-2026-11352

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD describes this as a QUIC / HTTP/3 client denial of service. The repo's active S2I build path in `.s2i/bin/assemble` restores and publishes the .NET project and does not invoke HTTP/3-specific client code. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `libcurl-minimal` on RHEL 9.

### libcurl-minimal / CVE-2026-11586

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD says exploitation requires a client WebSocket connection that receives malicious PING floods. The repo's S2I scripts do not open WebSocket client connections; active build steps use `dotnet restore` / `dotnet publish` and optional npm tooling over HTTPS. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `libcurl-minimal` on RHEL 9.

### libcurl-minimal / CVE-2026-8286

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD describes a STARTTLS connection-reuse flaw. The repo's S2I build scripts use HTTPS package feeds and do not perform STARTTLS upgrades for SMTP, IMAP, FTP, or similar protocols. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `libcurl-minimal` on RHEL 9.

### libcurl-minimal / CVE-2026-8925

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD describes a double-free in curl's GSASL cleanup path. The repo's S2I build scripts do not perform SASL-authenticated transfers, so the vulnerable code path is not exercised by the documented build flow. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `libcurl-minimal` on RHEL 9.

### libcurl-minimal / CVE-2026-9547

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 7.76.1-40.el9
**Fixed In:** (none published)
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` and the finding JSON still reports `fixedIn` as empty with status `affected`. NVD says exploitation requires SCP or SFTP transfers that use `CURLOPT_SSH_KEYFUNCTION`. The repo's S2I build scripts do not use SCP or SFTP. Carried from prior scan — evidence unchanged.

**Verification:** Rebuild and rescan after Red Hat publishes a fixed RPM for `libcurl-minimal` on RHEL 9.

### dotnet-host / CVE-2026-48779

**Target:** default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)
**Installed version:** 10.0.10-1.el9_8
**Fixed In:** (none published)
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** NVD and Trivy describe CVE-2026-48779 as a memory-exhaustion denial of service in the Node.js `ws` package rather than a native `dotnet-host` defect. Repo search found no `package.json`, lockfile, or checked-in `node_modules` directory, and `.s2i/bin/run` executes `dotnet` in normal mode rather than a Node process. The image could still contain `ws` in bundled base-image tooling, so live image inspection is required before the finding can be upgraded to `Not applicable / false positive`.

**Verification:** Run `oc rsh <mmria-s2i-pod> sh -lc "find / -path '*/node_modules/ws*' 2>/dev/null"`. If no `ws` package is present in the image filesystem, upgrade this finding to `Not applicable / false positive`; otherwise keep the current residual-risk verdict.

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` (`fixedIn` remains empty in the findings JSON). NVD describes a QUIC / HTTP/3 denial of service, and the repo's active `.s2i/bin/assemble` build flow does not invoke HTTP/3-specific client code. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` (`fixedIn` remains empty in the findings JSON). NVD says exploitation requires a client WebSocket connection that receives malicious PING floods, while the repo's S2I build path uses `dotnet` / optional npm package restores over HTTPS rather than WebSocket traffic. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` (`fixedIn` remains empty in the findings JSON). NVD describes a STARTTLS connection-reuse flaw, but the repo's S2I build path uses HTTPS package feeds and does not perform STARTTLS upgrades for SMTP, IMAP, FTP, or similar protocols. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` (`fixedIn` remains empty in the findings JSON). NVD describes a GSASL cleanup double-free that requires SASL-authenticated transfer paths, while the repo's documented S2I build flow does not perform SASL-authenticated transfers. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `curl-minimal` (`fixedIn` remains empty in the findings JSON). NVD says exploitation requires SCP or SFTP transfers that use `CURLOPT_SSH_KEYFUNCTION`, and the repo's S2I build scripts do not use SCP or SFTP. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` (`fixedIn` remains empty in the findings JSON). NVD describes a QUIC / HTTP/3 denial of service, and the repo's active `.s2i/bin/assemble` build flow does not invoke HTTP/3-specific client code. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` (`fixedIn` remains empty in the findings JSON). NVD says exploitation requires a client WebSocket connection that receives malicious PING floods, while the repo's S2I build path uses `dotnet` / optional npm package restores over HTTPS rather than WebSocket traffic. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` (`fixedIn` remains empty in the findings JSON). NVD describes a STARTTLS connection-reuse flaw, but the repo's S2I build path uses HTTPS package feeds and does not perform STARTTLS upgrades for SMTP, IMAP, FTP, or similar protocols. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` (`fixedIn` remains empty in the findings JSON). NVD describes a GSASL cleanup double-free that requires SASL-authenticated transfer paths, while the repo's documented S2I build flow does not perform SASL-authenticated transfers. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat has not published a fixed RHEL 9 RPM for `libcurl-minimal` (`fixedIn` remains empty in the findings JSON). NVD says exploitation requires SCP or SFTP transfers that use `CURLOPT_SSH_KEYFUNCTION`, and the repo's S2I build scripts do not use SCP or SFTP. The existing `.s2i/dockerfile` update layer will pick up any future Red Hat errata automatically. Carried from prior scan — evidence unchanged.

### dotnet-host / CVE-2024-38081

**Verdict:** Not applicable / false positive
**Summary:** Trivy attributes CVE-2024-38081 to `dotnet-host` `10.0.10-1.el9_8`, but NVD lists affected products as Microsoft Visual Studio 2022 and .NET 6.0 before 6.0.32; it does not list .NET 10. Red Hat's CVE page describes this issue as Visual Studio Installer / NuGet lockfile hijacking and shows RHEL 9 `dotnet6.0` as not affected. Because this image packages `dotnet-host` 10.x on RHEL 9 rather than Visual Studio or .NET 6.0, this finding is treated as a false positive.

### dotnet-host / CVE-2025-26682

**Verdict:** Not applicable / false positive
**Summary:** Red Hat's CVE statement says CVE-2025-26682 can only be exploited when HTTP/3 is enabled and that the .NET packages shipped in Red Hat Enterprise Linux do not support HTTP/3, so Red Hat products are not affected. Repo search found no HTTP/3 enablement in `.s2i/` or the Kestrel configuration in `Program.cs`, which only configures connection limits and timeouts. This finding is therefore treated as a false positive for the RHEL-shipped `dotnet-host` package in `mmria-s2i`.

### dotnet-host / CVE-2025-59144

**Verdict:** Not applicable / false positive
**Summary:** NVD says CVE-2025-59144 affects the malicious `debug` npm `4.4.2` package only in browser contexts and that local environments, server environments, and command-line applications are not affected. This repo has no checked-in Node package manifests or lockfiles, `.s2i/environment` sets only `DOTNET_STARTUP_PROJECT`, and `.s2i/bin/assemble` runs npm only when `DOTNET_NPM_TOOLS` is explicitly set. The finding against `dotnet-host` is therefore treated as a false positive for this repository image context.

### dotnet-host / CVE-2026-48779

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** NVD and Trivy describe CVE-2026-48779 as a memory-exhaustion denial of service in the Node.js `ws` package rather than a native `dotnet-host` defect. Repo search found no `package.json`, lockfile, or checked-in `node_modules`, and `.s2i/bin/run` executes `dotnet` in normal mode rather than a Node process. Live image inspection is still required to prove the `ws` package is absent from bundled base-image tooling, so this finding remains residual risk until `oc rsh <mmria-s2i-pod> sh -lc "find / -path '*/node_modules/ws*' 2>/dev/null"` confirms absence.

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
