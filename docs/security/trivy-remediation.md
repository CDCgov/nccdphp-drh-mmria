# Trivy Remediation Records

Records are prepended — newest scan block at the top.

---

## Scan: MMRIA Services @ 0f96ac0a — 2026-08-10

- **Commit:** `0f96ac0ad6bd7a1522b50a2061ad50c6b96d0e6b`
- **Service:** `MMRIA Services`
- **Scan ID:** `31307`
- **Severity totals:** C:0  H:14  M:104
- **Scanned image:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`

> **Scope:** This scan block addresses Critical and High findings only, consistent with the
> automated remediation workflow. The 104 Medium findings are not triaged here; they are
> tracked by the scanning pipeline and addressed in a separate review cycle.

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 0 | 0 | 14 | 0 | 14 |

- ⏳ **EVIDENCE WOULD UPGRADE** `dotnet-host / CVE-2025-59144` — run `oc rsh <mmria-services-pod> find /app /usr/share/dotnet -path '*/node_modules/debug*' 2>/dev/null`; if the runtime image has no `debug` package at all, this verdict can upgrade to `Not applicable / false positive`.
- ⏳ **EVIDENCE WOULD UPGRADE** `dotnet-host / CVE-2026-48779` — run `oc rsh <mmria-services-pod> find /app /usr/share/dotnet -path '*/node_modules/ws*' 2>/dev/null`; if the runtime image has no `ws` package at all, this verdict can upgrade to `Not applicable / false positive`.

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

`nccdphp-drh-mmria-services/mmria.services/Dockerfile` — expanded the runtime package-update layer from `libacl` only to `libacl curl-minimal libcurl-minimal dotnet-host`, with the existing `microdnf` fallback preserved. This ensures the MMRIA Services runtime image pulls any published Red Hat errata for the scanned packages during the next rebuild instead of waiting for a future Dockerfile change.

### HIGH / CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| curl-minimal | CVE-2026-11352 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no HTTP/3 or QUIC usage in `mmria.services`; Dockerfile now updates `curl-minimal` on rebuild. |
| curl-minimal | CVE-2026-11586 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no WebSocket usage in `mmria.services`; Dockerfile now updates `curl-minimal` on rebuild. |
| curl-minimal | CVE-2026-8286 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no STARTTLS usage in `mmria.services`; Dockerfile now updates `curl-minimal` on rebuild. |
| curl-minimal | CVE-2026-8925 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no GSASL usage in `mmria.services`; Dockerfile now updates `curl-minimal` on rebuild. |
| curl-minimal | CVE-2026-9547 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no SCP, SFTP, or `CURLOPT_SSH_KEYFUNCTION` usage in `mmria.services`; Dockerfile now updates `curl-minimal` on rebuild. |
| libcurl-minimal | CVE-2026-11352 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no HTTP/3 or QUIC usage in `mmria.services`; Dockerfile now updates `libcurl-minimal` on rebuild. |
| libcurl-minimal | CVE-2026-11586 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no WebSocket usage in `mmria.services`; Dockerfile now updates `libcurl-minimal` on rebuild. |
| libcurl-minimal | CVE-2026-8286 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no STARTTLS usage in `mmria.services`; Dockerfile now updates `libcurl-minimal` on rebuild. |
| libcurl-minimal | CVE-2026-8925 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no GSASL usage in `mmria.services`; Dockerfile now updates `libcurl-minimal` on rebuild. |
| libcurl-minimal | CVE-2026-9547 | Residual risk – no fix available | Red Hat lists the affected component and no fixed RPM is published at scan time; repo search found no SCP, SFTP, or `CURLOPT_SSH_KEYFUNCTION` usage in `mmria.services`; Dockerfile now updates `libcurl-minimal` on rebuild. |
| dotnet-host | CVE-2024-38081 | Residual risk – required, not reachable under current controls | `dotnet-host` is required for the ASP.NET runtime image; the Dockerfile runs the final image as UID 1001, which removes the local privileged execution path described by the EoP advisory. |
| dotnet-host | CVE-2025-26682 | Residual risk – required, not reachable under current controls | `dotnet-host` is required for the ASP.NET runtime image; the service host requires BasicAuthentication on controller endpoints, so the unauthenticated path described by the advisory is constrained by the existing auth gate. |
| dotnet-host | CVE-2025-59144 | Residual risk – required, not reachable under current controls | OSV describes this CVE as the compromised npm `debug` package, not the .NET host; the services source tree has no Node package manifests, so the Trivy attribution needs live-image verification before it can be upgraded to false positive. |
| dotnet-host | CVE-2026-48779 | Residual risk – required, not reachable under current controls | OSV describes this CVE as the npm `ws` package, not the .NET host; the services source tree has no Node package manifests, so the Trivy attribution needs live-image verification before it can be upgraded to false positive. |

### curl-minimal / CVE-2026-11352

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability description requires the client to connect to a malicious HTTP/3 server. `Program.cs` configures outbound traffic through `.NET` `SocketsHttpHandler`, and a repo search for `HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL` in `nccdphp-drh-mmria-services/mmria.services` returned no matches. The Dockerfile now updates `curl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### curl-minimal / CVE-2026-11586

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. OSV/NVD describe the issue as a malicious WebSocket peer flooding PING frames. The services source tree has no WebSocket references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `curl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### curl-minimal / CVE-2026-8286

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability requires a STARTTLS upgrade path to reuse the wrong live connection. The services source tree has no STARTTLS references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `curl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### curl-minimal / CVE-2026-8925

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability requires the GSASL authentication path inside curl. The services source tree has no GSASL references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `curl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### curl-minimal / CVE-2026-9547

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability requires `SCP://` or `SFTP://` transfers together with `CURLOPT_SSH_KEYFUNCTION`. The services source tree has no `SCP`, `SFTP`, or `CURLOPT_SSH_KEYFUNCTION` references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `curl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### libcurl-minimal / CVE-2026-11352

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability description requires the client to connect to a malicious HTTP/3 server. `Program.cs` configures outbound traffic through `.NET` `SocketsHttpHandler`, and a repo search for `HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL` in `nccdphp-drh-mmria-services/mmria.services` returned no matches. The Dockerfile now updates `libcurl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### libcurl-minimal / CVE-2026-11586

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. OSV/NVD describe the issue as a malicious WebSocket peer flooding PING frames. The services source tree has no WebSocket references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### libcurl-minimal / CVE-2026-8286

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability requires a STARTTLS upgrade path to reuse the wrong live connection. The services source tree has no STARTTLS references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### libcurl-minimal / CVE-2026-8925

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability requires the GSASL authentication path inside curl. The services source tree has no GSASL references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### libcurl-minimal / CVE-2026-9547

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `7.76.1-40.el9`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – no fix available

**Evidence:** Red Hat lists products that include the affected curl component, and the scan still reports `fixedIn` empty with status `affected`. The vulnerability requires `SCP://` or `SFTP://` transfers together with `CURLOPT_SSH_KEYFUNCTION`. The services source tree has no `SCP`, `SFTP`, or `CURLOPT_SSH_KEYFUNCTION` references in `nccdphp-drh-mmria-services/mmria.services`, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image picks up the first published Red Hat errata automatically.

**Verification:** Repo-static check already run: `rg -n "HTTP/3|QUIC|WebSocket|STARTTLS|SFTP|SCP|CURLOPT_SSH_KEYFUNCTION|GSASL" nccdphp-drh-mmria-services/mmria.services` → `No matches found.` Rebuild the image and rescan once Red Hat publishes a fixed curl errata RPM.

### dotnet-host / CVE-2024-38081

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `10.0.10-1.el9_8`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** Red Hat lists products that include the affected .NET host component, and the scan still reports `fixedIn` empty with status `end_of_life`. The advisory is an elevation-of-privilege issue that requires local execution. The runtime Dockerfile sets `USER 1001`, so the container does not run the service as root. `dotnet-host` is required by the ASP.NET runtime image and cannot be removed without breaking startup.

**Verification:** Repo-static proof is present in `nccdphp-drh-mmria-services/mmria.services/Dockerfile` (`USER 1001`). Tier-2 follow-up command: `oc rsh <mmria-services-pod> id -u` and confirm the output is `1001`.

### dotnet-host / CVE-2025-26682

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `10.0.10-1.el9_8`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** Red Hat lists products that include the affected .NET host component, and the scan still reports `fixedIn` empty with status `end_of_life`. The CVE describes an unauthorized network DoS in ASP.NET Core. In this repository, `Program.cs` registers `BasicAuthentication`, and controller entrypoints in `nccdphp-drh-mmria-services/mmria.services/Controllers` are annotated with `[Authorize]` and `[Authorize(AuthenticationSchemes = "BasicAuthentication")]`, so normal request execution is behind the existing auth gate. `dotnet-host` is required by the ASP.NET runtime image and cannot be removed without breaking startup.

**Verification:** Repo-static proof is present in `Program.cs` and the controller classes. Tier-2 follow-up command: `curl -i http://<mmria-services-route>/api/Message/_health` and confirm the endpoint returns `401 Unauthorized` without credentials.

### dotnet-host / CVE-2025-59144

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `10.0.10-1.el9_8`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** OSV identifies CVE-2025-59144 as the compromised npm `debug` package rather than a native `dotnet-host` defect. The services Dockerfile copies only `/app/publish` into the runtime stage, and `find nccdphp-drh-mmria-services/mmria.services \( -name package.json -o -name package-lock.json -o -name yarn.lock -o -name pnpm-lock.yaml -o -type d -name node_modules \) -print` returned no paths in the services source tree. Without live image inspection, the Trivy attribution cannot be upgraded to false positive yet, so the finding stays residual.

**Verification:** Repo-static check already run: `find nccdphp-drh-mmria-services/mmria.services \( -name package.json -o -name package-lock.json -o -name yarn.lock -o -name pnpm-lock.yaml -o -type d -name node_modules \) -print` → `(no output)`. Tier-2 follow-up command: `oc rsh <mmria-services-pod> find /app /usr/share/dotnet -path '*/node_modules/debug*' 2>/dev/null`.

### dotnet-host / CVE-2026-48779

**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`  
**Installed version:** `10.0.10-1.el9_8`  
**Fixed In:** `(none published)`  
**Verdict:** Residual risk – required, not reachable under current controls

**Evidence:** OSV identifies CVE-2026-48779 as the npm `ws` package rather than a native `dotnet-host` defect. The services Dockerfile copies only `/app/publish` into the runtime stage, and `find nccdphp-drh-mmria-services/mmria.services \( -name package.json -o -name package-lock.json -o -name yarn.lock -o -name pnpm-lock.yaml -o -type d -name node_modules \) -print` returned no paths in the services source tree. Without live image inspection, the Trivy attribution cannot be upgraded to false positive yet, so the finding stays residual.

**Verification:** Repo-static check already run: `find nccdphp-drh-mmria-services/mmria.services \( -name package.json -o -name package-lock.json -o -name yarn.lock -o -name pnpm-lock.yaml -o -type d -name node_modules \) -print` → `(no output)`. Tier-2 follow-up command: `oc rsh <mmria-services-pod> find /app /usr/share/dotnet -path '*/node_modules/ws*' 2>/dev/null`.

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time. The MMRIA Services source tree has no HTTP/3 or QUIC references, and the Dockerfile now updates `curl-minimal` during rebuild so the image will pick up the first published errata automatically.

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time. The MMRIA Services source tree has no WebSocket references, and the Dockerfile now updates `curl-minimal` during rebuild so the image will pick up the first published errata automatically.

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time. The MMRIA Services source tree has no STARTTLS references, and the Dockerfile now updates `curl-minimal` during rebuild so the image will pick up the first published errata automatically.

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time. The MMRIA Services source tree has no GSASL references, and the Dockerfile now updates `curl-minimal` during rebuild so the image will pick up the first published errata automatically.

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time. The MMRIA Services source tree has no SCP, SFTP, or `CURLOPT_SSH_KEYFUNCTION` references, and the Dockerfile now updates `curl-minimal` during rebuild so the image will pick up the first published errata automatically.

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time for `libcurl-minimal`. The MMRIA Services source tree has no HTTP/3 or QUIC references, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image will pick up the first published errata automatically.

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time for `libcurl-minimal`. The MMRIA Services source tree has no WebSocket references, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image will pick up the first published errata automatically.

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time for `libcurl-minimal`. The MMRIA Services source tree has no STARTTLS references, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image will pick up the first published errata automatically.

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time for `libcurl-minimal`. The MMRIA Services source tree has no GSASL references, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image will pick up the first published errata automatically.

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – no fix available
**Summary:** Red Hat lists the affected curl component, and the scan still reports no published fixed RPM for RHEL 9 at scan time for `libcurl-minimal`. The MMRIA Services source tree has no SCP, SFTP, or `CURLOPT_SSH_KEYFUNCTION` references, and the Dockerfile now updates `libcurl-minimal` during rebuild so the image will pick up the first published errata automatically.

### dotnet-host / CVE-2024-38081

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** `dotnet-host` is required by the ASP.NET runtime image and cannot be removed without breaking startup. The final runtime image runs as `USER 1001`, which removes the local privileged execution path described by the elevation-of-privilege advisory while Red Hat still reports no published fixed RPM for this target.

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** `dotnet-host` is required by the ASP.NET runtime image and cannot be removed without breaking startup. `Program.cs` registers `BasicAuthentication`, and the MMRIA Services controllers are annotated with `[Authorize(AuthenticationSchemes = "BasicAuthentication")]`, so normal request execution is behind the existing authentication gate while Red Hat still reports no published fixed RPM for this target.

### dotnet-host / CVE-2025-59144

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** OSV identifies CVE-2025-59144 as the compromised npm `debug` package rather than a native `dotnet-host` defect. The services Dockerfile copies only `/app/publish` into the runtime stage, and the services source tree contains no Node package manifests, so this stays residual until a live `oc rsh` check proves the runtime image also lacks the `debug` package.

### dotnet-host / CVE-2026-48779

**Verdict:** Residual risk – required, not reachable under current controls
**Summary:** OSV identifies CVE-2026-48779 as the npm `ws` package rather than a native `dotnet-host` defect. The services Dockerfile copies only `/app/publish` into the runtime stage, and the services source tree contains no Node package manifests, so this stays residual until a live `oc rsh` check proves the runtime image also lacks the `ws` package.

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
