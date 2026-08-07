<!-- Trivy remediation records — newest scan block at the top. Do not overwrite; prepend. -->

## Scan: 31293 — MMRIA Services @ ef00c008 — 2026-08-07

**Service:** MMRIA Services
**Commit:** ef00c008ace2f269e270b5e124e51f21d6c66de2
**Image target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-services:latest (redhat 9.8)`
**Findings sent for remediation:** Critical: 0 | High: 14 | Medium: 104

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| HIGH | 14 | 10 | 0 | 4 | 0 | 4 |

### Fixes made

| File | Package | CVEs | Before | After | Notes |
|---|---|---|---|---|---|
| `nccdphp-drh-mmria-services/mmria.services/Dockerfile` | `curl-minimal`, `libcurl-minimal` | CVE-2026-11352, CVE-2026-11586, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | 7.76.1-40.el9 | latest patched via `dnf update` | Added to runtime-stage package update |

---

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

**Summary:** curl-minimal QUIC/HTTP3 UDP receive DoS — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-11352 describes a remote denial-of-service in curl's QUIC UDP receive path where a malicious HTTP/3 server can exploit how zero-length UDP datagrams are counted toward the per-connection limit. NVD CVSS vector reflects network-accessible attack (AV:N). The Dockerfile runtime stage now includes `dnf update -y curl-minimal libcurl-minimal`, which pulls the latest available patched RPM from the Red Hat UBI9 repository at image build time. After this update, the installed version of curl-minimal will be at or above any published fix.

---

### curl-minimal / CVE-2026-11586

**Summary:** curl-minimal WebSocket PING unbounded memory allocation DoS — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-11586 describes a remote memory-exhaustion denial-of-service where a malicious server floods a curl client with rapid WebSocket PING frames, which curl acknowledges without an upper memory bound. The Dockerfile runtime stage now includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM. The installed curl-minimal version will be at or above any published fix after the image is rebuilt.

---

### curl-minimal / CVE-2026-8286

**Summary:** curl-minimal STARTTLS connection-reuse TLS mismatch — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-8286 describes a TLS session reuse flaw where a new STARTTLS transfer can incorrectly reuse an existing live connection whose TLS configuration mismatches. This can allow a man-in-the-middle or credential exposure scenario. The Dockerfile runtime stage now includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM from the Red Hat UBI9 repository at image rebuild time.

---

### curl-minimal / CVE-2026-8925

**Summary:** curl-minimal GSASL double-free memory corruption — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-8925 describes a double-free memory corruption in curl's SASL/GSASL authentication path where the GSASL context is freed twice without clearing the pointer. This can lead to heap corruption and potentially arbitrary code execution in a SASL-authenticated transfer scenario. The Dockerfile runtime stage now includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM.

---

### curl-minimal / CVE-2026-9547

**Summary:** curl-minimal SSH host-key type bypass via CURLOPT_SSH_KEYFUNCTION — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-9547 describes a silent acceptance of an untrusted SSH server host key when `CURLOPT_SSH_KEYFUNCTION` is used and the server presents a key type not handled by the callback. This applies to SCP:// and SFTP:// transfers. The Dockerfile runtime stage now includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM.

---

### libcurl-minimal / CVE-2026-11352

**Summary:** libcurl-minimal QUIC/HTTP3 UDP receive DoS — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-11352 in the `libcurl-minimal` package is the same underlying vulnerability as in `curl-minimal` (shared library). A malicious HTTP/3 server can exploit how zero-length UDP datagrams are counted in the QUIC receive path to trigger a remote DoS. The Dockerfile runtime stage includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched libcurl-minimal RPM from the Red Hat UBI9 repository at image build time.

---

### libcurl-minimal / CVE-2026-11586

**Summary:** libcurl-minimal WebSocket PING unbounded memory DoS — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-11586 in `libcurl-minimal` describes the same WebSocket PING memory-exhaustion DoS as in the curl-minimal package (shared library). A malicious server can exhaust all available client memory. The Dockerfile runtime stage includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM.

---

### libcurl-minimal / CVE-2026-8286

**Summary:** libcurl-minimal STARTTLS connection-reuse TLS mismatch — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-8286 in `libcurl-minimal` is the same STARTTLS connection-reuse TLS mismatch flaw as in the curl-minimal package (shared library). The Dockerfile runtime stage includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM.

---

### libcurl-minimal / CVE-2026-8925

**Summary:** libcurl-minimal GSASL double-free memory corruption — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-8925 in `libcurl-minimal` is the same GSASL double-free memory corruption as in the curl-minimal package (shared library). The Dockerfile runtime stage includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM.

---

### libcurl-minimal / CVE-2026-9547

**Summary:** libcurl-minimal SSH host-key type bypass — fixed by runtime-stage dnf update.

**Verdict:** Fixed

CVE-2026-9547 in `libcurl-minimal` is the same SSH host-key bypass as in the curl-minimal package (shared library). The Dockerfile runtime stage includes `dnf update -y curl-minimal libcurl-minimal`, pulling the latest patched RPM.

---

### dotnet-host / CVE-2024-38081

**Summary:** dotnet-host .NET Elevation of Privilege — residual risk, no fix available in trusted image registry.

**Verdict:** Residual risk – required, not reachable under current controls

CVE-2024-38081 is a .NET / Visual Studio elevation-of-privilege vulnerability. Trivy reports `dotnet-host` version 10.0.10-1.el9_8 as `end_of_life` status with no `fixedIn` version. The dotnet-host package is part of the trusted base image `dotnet-100-aspnet:9.8-1785990857` pinned from the internal OpenShift trusted image registry; this image cannot be independently updated outside the trusted-images pipeline. A base-image update from the trusted-images team is required to pick up a patched dotnet-host RPM. The application runs as UID 1001 (non-root) per Dockerfile `USER 1001`, which constrains local privilege-escalation paths. No interactive shell or developer tooling is present in the runtime image. Remediation requires a new trusted base image publication.

⏳ EVIDENCE WOULD UPGRADE — A rebuild + Trivy rescan against an updated trusted base image (`dotnet-100-aspnet` newer than 9.8-1785990857) would confirm dotnet-host is patched. Run `oc rsh <pod> rpm -q dotnet-host` to verify the installed version at runtime.

---

### dotnet-host / CVE-2025-26682

**Summary:** dotnet-host ASP.NET Core network DoS — residual risk, no fix available in trusted image registry.

**Verdict:** Residual risk – required, not reachable under current controls

CVE-2025-26682 is an ASP.NET Core resource-exhaustion denial-of-service vulnerability where an unauthenticated attacker can deny service over a network. Trivy reports `dotnet-host` 10.0.10-1.el9_8 as `end_of_life` with no `fixedIn` version. The dotnet-host package is part of the trusted base image pinned from the internal OpenShift trusted image registry; independent package-level update is not available outside the trusted-images pipeline. The service is deployed inside OpenShift with network policy and ingress controls; direct exposure to unauthenticated external actors depends on route configuration which is a deployment-level control. Remediation requires a new trusted base image publication.

⏳ EVIDENCE WOULD UPGRADE — A rebuild + rescan against an updated `dotnet-100-aspnet` trusted base image would confirm dotnet-host is patched. Run `oc rsh <pod> rpm -q dotnet-host` to verify installed version.

---

### dotnet-host / CVE-2025-59144

**Summary:** dotnet-host misattributed finding (JavaScript debug package compromise) — residual risk pending trusted image update.

**Verdict:** Residual risk – required, not reachable under current controls

CVE-2025-59144 describes a supply-chain compromise of the npm `debug` JavaScript package (v4.4.2 contained malware). Trivy attributes this to `dotnet-host` 10.0.10-1.el9_8 with `end_of_life` status, likely due to Red Hat CVE database cross-attribution in the RPM advisory metadata. The MMRIA Services image is a .NET runtime container; it does not include a Node.js runtime, npm toolchain, or the `debug` JavaScript package. However, because the package is reported via the RPM database (not a direct npm manifest scan), this cannot be dismissed as a static false positive without a runtime rescan. The dotnet-host package originates from the trusted base image; update requires the trusted-images pipeline.

⏳ EVIDENCE WOULD UPGRADE — Run `oc rsh <pod> rpm -q nodejs npm` and `find / -name "debug" -path "*/node_modules/*" 2>/dev/null` to confirm no Node.js or npm `debug` package is present; if absent, this can be reclassified as Not applicable / false positive.

---

### dotnet-host / CVE-2026-48779

**Summary:** dotnet-host misattributed finding (Node.js ws WebSocket DoS) — residual risk pending trusted image update.

**Verdict:** Residual risk – required, not reachable under current controls

CVE-2026-48779 describes a memory-exhaustion denial-of-service in the Node.js `ws` WebSocket library (versions up to 8.21.0). Trivy attributes this to `dotnet-host` 10.0.10-1.el9_8 with `under_investigation` status, likely via Red Hat RPM advisory cross-attribution. The MMRIA Services image is a .NET runtime container and does not include a Node.js runtime or the `ws` npm package. However, because the finding is attributed via the RPM database, it cannot be statically dismissed without a runtime check. The dotnet-host package originates from the trusted base image; an independent dnf update of dotnet-host outside the trusted-images pipeline is not viable.

⏳ EVIDENCE WOULD UPGRADE — Run `oc rsh <pod> rpm -q nodejs` and `find / -name "ws" -path "*/node_modules/*" 2>/dev/null` to confirm no Node.js or `ws` package is present; if absent, this can be reclassified as Not applicable / false positive.
