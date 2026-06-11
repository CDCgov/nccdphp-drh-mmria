# Trivy Remediation Log

## Scan: 30001 @ 1e38e65d — C:0 H:2

- **Commit:** `1e38e65d5a1d08fd8755a2bd4d86af04c0b54a0f`
- **Service:** `45`
- **Scan ID:** `30001`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-06-11

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| -------- | -------- | ----- | -------------------- | -------- | -------------- | --------- |
| Critical | 0        | 0     | 0                    | 0        | 0              | 0         |
| High     | 2        | 0     | 0                    | 2        | 0              | 2         |

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
| ------ | ------- | ------------- | -------- | ------ | --------- | ------------- | ------- | -------- |
| mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-10732 | HIGH | under_investigation | 10.0.8-1.el8_10 | none | Residual risk – fix deferred | No fix available from vendor; status is under_investigation. NVD CVSS 4.0 AV:N/AC:L/AT:P/PR:N/UI:P requires attacker-supplied ZIP and user interaction. The mmria application does not expose a ZIP extraction endpoint and runs as UID 1001 inside an OpenShift ARO cluster. Base image update to import a patched dotnet-host package is tracked as follow-up. |
| mmria-s2i:latest (redhat 8.10) | openssl-libs | CVE-2026-45447 | HIGH | affected | 1:1.1.1k-15.el8_6 | none | Residual risk – fix deferred | No fix available from vendor; status is affected with empty fixedIn. NVD CWE-416 use-after-free in OpenSSL PKCS7_verify() is triggered only when an application calls PKCS7_verify() directly on a crafted S/MIME or PKCS#7 message with an empty digestAlgorithms SET. The mmria application is an ASP.NET Core server communicating with CouchDB over HTTP/HTTPS; it does not call OpenSSL PKCS#7 APIs directly. Container runs as UID 1001 in OpenShift ARO with network policies limiting external access. Base image update to pick up a patched openssl-libs package is tracked as follow-up. |

## HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| ------- | ------------- | ------- | -------- |
| dotnet-host@10.0.8-1.el8_10 | CVE-2026-10732 | Residual risk – fix deferred | No vendor fix; under_investigation status; Zip Slip exploit requires attacker-supplied ZIP processed by the vulnerable decompress JS library bundled within the dotnet-host SDK tooling — not reachable via the mmria ASP.NET Core application's runtime path |
| openssl-libs@1:1.1.1k-15.el8_6 | CVE-2026-45447 | Residual risk – fix deferred | No vendor fix; affected status; use-after-free requires direct PKCS7_verify() call with crafted empty digestAlgorithms ASN.1 SET — mmria app does not use OpenSSL PKCS#7 APIs; CMS APIs (unaffected per NVD) are the standard path for modern .NET TLS |

### dotnet-host / CVE-2026-10732

- **Finding:** target `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)`; package `dotnet-host`; CVE `CVE-2026-10732`; severity HIGH; status `under_investigation`; installed `10.0.8-1.el8_10`; fixed version none
- **Remediation attempted:** The `dotnet-host` package is an OS-level RPM installed in the RHEL 8.10 base image (`dotnet-80` trusted image). No updated RPM is available from the RHEL 8 channel at this time (fixedIn is empty). Removing the package would break the .NET runtime entrypoint.
- **Why not fixed here:** No vendor-provided fixed version exists. Status is `under_investigation`, meaning Red Hat has not yet published a patched RPM for RHEL 8. A repository-only fix is not possible; the fix must come from the upstream trusted-images base image update.
- **Usage/reachability:** The CVE-2026-10732 root vulnerability is in the npm `decompress` JavaScript package (SNYK-JS-DECOMPRESS-16415209), attributed by Trivy to the `dotnet-host` RPM because the .NET SDK tooling bundles Node.js-based tooling or similar components. The mmria application is an ASP.NET Core server (`dotnet mmria-server.dll`) — it does not expose a ZIP extraction API, does not invoke the JavaScript `decompress` package, and the bundled component (if present) is not accessible at runtime without deliberate shell access to the container.
- **Exploit preconditions:** NVD CVSS 4.0 vector `CVSS:4.0/AV:N/AC:L/AT:P/PR:N/UI:P/VC:L/VI:H/VA:L/SC:N/SI:N/SA:N` (source: Snyk). The `AT:P` (Attack Requirements: Present) component requires that the application is actually performing ZIP extraction using the vulnerable decompress library on a ZIP file the attacker can influence. The `UI:P` (User Interaction: Passive) component requires user interaction. The mmria application does not perform ZIP extraction via this library at runtime, so the precondition AT:P is not met.
- **Compensating controls:** Container runs as UID 1001 (non-root) inside OpenShift ARO; `oc rsh` and pod exec access is restricted to platform admins; no ZIP extraction endpoint is exposed by the mmria application; the application image is deployed in a private OpenShift cluster with network policies.
- **Reviewer verification:**
  ```bash
  # Confirm dotnet-host version in the running pod
  oc rsh -n mmria deployment/mmria-server -- rpm -q dotnet-host

  # Confirm no ZIP extraction tooling is callable from the app entrypoint
  oc rsh -n mmria deployment/mmria-server -- sh -c "command -v decompress 2>/dev/null || echo 'PASS: decompress not on PATH'"

  # Confirm non-root UID
  oc rsh -n mmria deployment/mmria-server -- id -u
  ```
- **Follow-up:** Monitor Red Hat CVE database at `https://access.redhat.com/security/cve/CVE-2026-10732` for a patched `dotnet-host` RPM. When available, rebuild the `mmria-s2i` image from an updated trusted base image and rescan with:
  ```bash
  trivy image --severity HIGH,CRITICAL default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest
  ```

---

### openssl-libs / CVE-2026-45447

- **Finding:** target `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)`; package `openssl-libs`; CVE `CVE-2026-45447`; severity HIGH; status `affected`; installed `1:1.1.1k-15.el8_6`; fixed version none
- **Remediation attempted:** `openssl-libs` is a core OS RPM in the RHEL 8.10 base image. No updated RPM is available from the RHEL 8 channel at this time (fixedIn is empty). Removing the package is not possible because it is a foundational dependency of the OS and the .NET runtime TLS stack.
- **Why not fixed here:** No vendor-provided fixed version exists. Status is `affected` with an empty fixedIn field, meaning Red Hat has not yet published a patched RPM. A repository-only fix is not possible; remediation requires the upstream trusted base image to be rebuilt with a patched `openssl-libs` RPM.
- **Usage/reachability:** Per NVD (https://nvd.nist.gov/vuln/detail/CVE-2026-45447): "Applications that process PKCS#7 or S/MIME signed messages using OpenSSL PKCS#7 APIs may be affected. Applications using the CMS APIs for this processing are not affected." The mmria application is an ASP.NET Core server that communicates with CouchDB over HTTP/HTTPS and serves a web UI. It does not process PKCS#7 or S/MIME signed messages and does not call `PKCS7_verify()` directly. The .NET TLS stack uses OpenSSL via its own abstraction layer (SslStream → CMS APIs), not the legacy PKCS#7 APIs. NVD also states: "The FIPS modules in 4.0, 3.6, 3.5, 3.4, and 3.0 are not affected by this issue, as the affected code is outside the OpenSSL FIPS module boundary."
- **Exploit preconditions:** The vulnerability (CWE-416, use-after-free) is triggered when `PKCS7_verify()` is called on a crafted PKCS#7 or S/MIME message where the `SignedData digestAlgorithms` field is present as an empty ASN.1 SET. This requires: (1) the application calls OpenSSL's `PKCS7_verify()` directly, and (2) the attacker can supply a specially crafted message to that call. The mmria application does not expose any endpoint that parses PKCS#7 or S/MIME content, so neither precondition is met in this deployment.
- **Compensating controls:** Container runs as UID 1001 (non-root) inside OpenShift ARO; no inbound S/MIME or PKCS#7 message processing endpoint is exposed; network access to the cluster is restricted; `oc rsh` and pod exec access is restricted to platform admins.
- **Reviewer verification:**
  ```bash
  # Confirm openssl-libs version in the running pod
  oc rsh -n mmria deployment/mmria-server -- rpm -q openssl-libs

  # Confirm PKCS#7 processing tools are not present
  oc rsh -n mmria deployment/mmria-server -- sh -c "command -v openssl 2>/dev/null && openssl pkcs7 --help 2>&1 | head -3 || echo 'PASS: openssl CLI not on PATH or pkcs7 subcommand not accessible'"

  # Confirm non-root UID
  oc rsh -n mmria deployment/mmria-server -- id -u
  ```
- **Follow-up:** Monitor Red Hat CVE database at `https://access.redhat.com/security/cve/CVE-2026-45447` for a patched `openssl-libs` RPM. When available, rebuild the `mmria-s2i` image from an updated trusted base image and rescan with:
  ```bash
  trivy image --severity HIGH,CRITICAL default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest
  ```

## Verification

- No Dockerfile or application code changes were made for this scan; both findings are OS-level RPMs in the RHEL 8.10 base image with no vendor fix available.
- Before totals: C:0 H:2
- After totals: C:0 H:2 (both remain as residual risk — no fix available from vendor)
- Rescan command (run after base image is updated in trusted-images):
  ```bash
  trivy image --severity HIGH,CRITICAL default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest
  ```

## SWA Exception Justifications

### dotnet-host / CVE-2026-10732

- **CVE:** CVE-2026-10732
- **Package:** dotnet-host@10.0.8-1.el8_10
- **Severity:** HIGH
- **Status:** affected (no fix available) — under_investigation by Red Hat
- **SSC Issue ID:** not yet assigned
- **Verdict:** Residual risk – required, not reachable under current controls

The `dotnet-host` RPM version 10.0.8-1.el8_10 is installed in the RHEL 8.10-based `mmria-s2i` image. CVE-2026-10732 is a Zip Slip vulnerability (CWE-29) in the npm `decompress` JavaScript library, attributed to the dotnet-host package in the Trivy scan. No vendor-provided fix exists (fixedIn is empty; Red Hat status is under_investigation). The mmria application is an ASP.NET Core server that does not perform ZIP extraction via the JavaScript `decompress` library — the vulnerable code path is bundled within the .NET SDK tooling and is not invoked by the application's runtime entrypoint (`dotnet mmria-server.dll`). The NVD CVSS 4.0 vector `CVSS:4.0/AV:N/AC:L/AT:P/PR:N/UI:P` from Snyk lists Attack Requirements as Present (AT:P), meaning exploitation requires the application to be actively extracting a ZIP supplied by the attacker — a precondition not met in this deployment. The container runs as UID 1001 (non-root) inside an OpenShift ARO cluster with restricted `oc rsh` access and no exposed ZIP extraction endpoint. The package cannot be removed without breaking the .NET runtime. Remediation is deferred pending a patched dotnet-host RPM from Red Hat; the trusted-images base image should be updated when the fix is available.

### openssl-libs / CVE-2026-45447

- **CVE:** CVE-2026-45447
- **Package:** openssl-libs@1:1.1.1k-15.el8_6
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **SSC Issue ID:** not yet assigned
- **Verdict:** Residual risk – required, not reachable under current controls

The `openssl-libs` RPM version 1:1.1.1k-15.el8_6 is installed in the RHEL 8.10-based `mmria-s2i` image. CVE-2026-45447 is a use-after-free vulnerability (CWE-416) in OpenSSL's `PKCS7_verify()` function, triggered when a PKCS#7 or S/MIME signed message contains an empty `digestAlgorithms` ASN.1 SET. No vendor-provided fix exists (fixedIn is empty; Red Hat status is affected). Per NVD (https://nvd.nist.gov/vuln/detail/CVE-2026-45447): "Applications that process PKCS#7 or S/MIME signed messages using OpenSSL PKCS#7 APIs may be affected. Applications using the CMS APIs for this processing are not affected." The mmria application is an ASP.NET Core server that communicates with CouchDB over HTTP/HTTPS and serves a web UI; it does not process PKCS#7 or S/MIME signed messages and does not call `PKCS7_verify()` directly. The .NET TLS stack uses OpenSSL via CMS APIs, not the legacy PKCS#7 APIs. The package is a foundational OS dependency and cannot be removed. The container runs as UID 1001 (non-root) inside an OpenShift ARO cluster with restricted pod exec access and no PKCS#7 message processing endpoint. Remediation is deferred pending a patched openssl-libs RPM from Red Hat; the trusted-images base image should be updated when the fix is available.
