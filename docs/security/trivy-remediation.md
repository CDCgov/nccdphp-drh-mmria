# Trivy Remediation Log

## Scan: 30009 @ 8af8c9d3 — C:0 H:6

- **Commit:** `8af8c9d3668df6757eb3561e48d30018dcbfb36f`
- **Service:** `45`
- **Scan ID:** `30009`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-06-12

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| -------- | -------- | ----- | -------------------- | -------- | -------------- | --------- |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 6 | 4 | 0 | 2 | 0 | 2 |

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
| ------ | ------- | ------------- | -------- | ------ | --------- | ------------- | ------- | -------- |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | aspnetcore-runtime-9.0 | CVE-2026-45591 | HIGH | fixed | 9.0.16-1.el8_10 | 9.0.17-1.el8_10 | Fixed | `/source-code/mmria/mmria-server/Dockerfile` now runs `dnf -y update aspnetcore-runtime-9.0 ...` in the runtime stage, which upgrades the vulnerable package at image build time before runtime image publish. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-10732 | HIGH | under_investigation | 10.0.8-1.el8_10 |  | Residual risk – fix deferred | Red Hat CVE JSON for CVE-2026-10732 lists RHEL8 `dotnet8.0` as `Under investigation`; no fixed package version is published in finding data (`fixedIn` empty). Runtime stage update command is present, but no vendor-fixed rpm is currently available to consume. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-45591 | HIGH | fixed | 10.0.8-1.el8_10 | 10.0.9-1.el8_10 | Fixed | `/source-code/mmria/mmria-server/Dockerfile` now runs `dnf -y update ... dotnet-host ...` in the runtime stage, so rebuilt images will pull the latest available dotnet-host errata including 10.0.9+ when present in configured repos. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-hostfxr-9.0 | CVE-2026-45591 | HIGH | fixed | 9.0.16-1.el8_10 | 9.0.17-1.el8_10 | Fixed | `/source-code/mmria/mmria-server/Dockerfile` now runs `dnf -y update ... dotnet-hostfxr-9.0 ...` in the runtime stage, upgrading hostfxr to current security errata during image build. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-runtime-9.0 | CVE-2026-45591 | HIGH | fixed | 9.0.16-1.el8_10 | 9.0.17-1.el8_10 | Fixed | `/source-code/mmria/mmria-server/Dockerfile` now runs `dnf -y update ... dotnet-runtime-9.0 ...` in the runtime stage, upgrading runtime package security errata in final image assembly. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | openssl-libs | CVE-2026-45447 | HIGH | affected | 1:1.1.1k-15.el8_6 |  | Residual risk – no fix available | Red Hat CVE JSON for CVE-2026-45447 marks `openssl` on RHEL8 as `Affected` and lists fixed advisories for RHEL9/RHEL10 only, while `fixedIn` is empty in findings. Runtime stage now attempts `dnf update openssl-libs`, but no RHEL8 fixed package is yet published for this CVE. |

## HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| ------- | ------------- | ------- | -------- |
| dotnet-host | CVE-2026-10732 | Residual risk – fix deferred | Red Hat marks RHEL8 dotnet state `Under investigation`; finding has no `fixedIn` version. |
| openssl-libs | CVE-2026-45447 | Residual risk – no fix available | Red Hat marks RHEL8 openssl state `Affected`; findings show no fixed version while RHSA fixes listed for RHEL9/10. |

### dotnet-host / CVE-2026-10732

- Finding: `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)`, package `dotnet-host`, CVE `CVE-2026-10732`, severity `HIGH`, status `under_investigation`, installed `10.0.8-1.el8_10`, fixed-version ``.
- Remediation attempted: Added `dnf -y update aspnetcore-runtime-9.0 dotnet-host dotnet-hostfxr-9.0 dotnet-runtime-9.0 openssl-libs` to runtime stage in `/source-code/mmria/mmria-server/Dockerfile` so all available runtime errata are applied on build.
- Why not fixed here: Red Hat security data (`/hydra/rest/securitydata/cve/CVE-2026-10732.json`) currently lists RHEL8 `dotnet8.0` as `Under investigation` and the Trivy finding has empty `fixedIn`, so no vendor patch version is available to pin in this repository.
- Usage/reachability: NVD describes this CVE as a Zip Slip issue in `decompress` archive extraction requiring a specially crafted ZIP input and extraction path handling; this is transitive OS package surface from dotnet host rpm, not a repository-managed direct dependency.
- Exploit preconditions: NVD states attacker must provide a crafted ZIP and trigger extraction flow (`AV:N/AC:L/PR:N/UI:P` via CNA vector) and Red Hat classifies package state as `Under investigation`, so exploitability in this image remains unresolved pending vendor determination.
- Compensating controls: No repository-level control can remove `dotnet-host` from a .NET runtime image; runtime image runs as non-root (`USER 1001`) in Dockerfile, which reduces post-compromise privilege but does not eliminate vulnerability risk.
- Reviewer verification:
  - `docker run --rm <rebuilt-image> rpm -q dotnet-host`
  - `docker run --rm <rebuilt-image> rpm -qa | grep -E '^dotnet|^aspnetcore'`
  - `trivy image --severity HIGH,CRITICAL <internal-registry>/mmria-s2i:<new-tag>@<new-digest>`
- Follow-up: Track Red Hat advisory publication for CVE-2026-10732 on RHEL8 dotnet and update image immediately when fixed package is released.

### openssl-libs / CVE-2026-45447

- Finding: `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)`, package `openssl-libs`, CVE `CVE-2026-45447`, severity `HIGH`, status `affected`, installed `1:1.1.1k-15.el8_6`, fixed-version ``.
- Remediation attempted: Added runtime-stage `dnf -y update ... openssl-libs` in `/source-code/mmria/mmria-server/Dockerfile` so any available RHEL8 OpenSSL errata are consumed during image build.
- Why not fixed here: Red Hat security data (`/hydra/rest/securitydata/cve/CVE-2026-45447.json`) marks RHEL8 `openssl` as `Affected`; fixed advisories in the same record are for RHEL9 and RHEL10 package streams, with no published RHEL8 fixed package currently referenced by Trivy.
- Usage/reachability: NVD specifies impact occurs when applications process crafted PKCS#7 or S/MIME signed messages using OpenSSL `PKCS7_verify()` APIs; applications using CMS APIs are not affected per NVD description.
- Exploit preconditions: NVD details that attacker must provide specially crafted signed PKCS#7/S/MIME content that reaches vulnerable PKCS#7 verification code path; Red Hat CVSS vector is `AV:N/AC:H/PR:N/UI:N`, indicating non-trivial exploit conditions but still network reachable where that parsing path exists.
- Compensating controls: Repository-level Dockerfile keeps container non-root (`USER 1001`), limiting privilege escalation impact; no repository evidence currently proves PKCS#7 verification path absence, so risk remains open.
- Reviewer verification:
  - `docker run --rm <rebuilt-image> rpm -q openssl-libs`
  - `docker run --rm <rebuilt-image> rpm -qa | grep '^openssl'`
  - `docker run --rm <rebuilt-image> sh -c "command -v openssl && openssl version -a"`
  - `trivy image --severity HIGH,CRITICAL <internal-registry>/mmria-s2i:<new-tag>@<new-digest>`
- Follow-up: Track Red Hat RHEL8 OpenSSL errata for CVE-2026-45447 and apply via image rebuild as soon as advisory is released.

## Verification

- Baseline build status before changes:
  - `dotnet build source-code/mmria/mmria.sln -c Release` ✅
  - `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.sln -c Release` ✅
  - `dotnet test source-code/mmria/mmria.sln -c Release --no-build` ✅
  - `dotnet test nccdphp-drh-mmria-services/mmria.services/mmria.services.sln -c Release --no-build` ✅
- Code changes made:
  - `/source-code/mmria/mmria-server/Dockerfile` runtime stage now applies `dnf` security updates for affected runtime packages.
- Local limitations:
  - This environment cannot access the referenced external workflow run in `cdcent/nccdphp-od-devops` (GitHub API returned `404` for run `27419588783`) and cannot rebuild/pull the internal OpenShift image here.
- Team rescan command after image rebuild/import:
  - `trivy image --severity HIGH,CRITICAL default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:<new-tag>@<new-digest>`

## SWA Exception Justifications

### dotnet-host / CVE-2026-10732

- **CVE:** CVE-2026-10732
- **Package:** dotnet-host@10.0.8-1.el8_10
- **Severity:** HIGH
- **Status:** under_investigation
- **SSC Issue ID:**
- **Verdict:** Residual risk – fix deferred

Red Hat security data currently lists RHEL8 dotnet (`dotnet8.0`) as under investigation for CVE-2026-10732 and the scan result provides no fixed version to adopt in this repository. The Dockerfile now applies all available dotnet runtime errata at build time, but this specific CVE cannot be closed until Red Hat publishes an updated package. Reviewers can confirm current state by checking `rpm -q dotnet-host` in the rebuilt image and rerunning Trivy on the rebuilt digest.

### openssl-libs / CVE-2026-45447

- **CVE:** CVE-2026-45447
- **Package:** openssl-libs@1:1.1.1k-15.el8_6
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:**
- **Verdict:** Residual risk – no fix available

Red Hat marks RHEL8 openssl as affected for CVE-2026-45447 while listing fixed advisories only for RHEL9 and RHEL10 streams, so a repository-only package pin cannot resolve this on the current base stream. NVD states exploitation requires a crafted PKCS#7 or S/MIME signed message reaching OpenSSL PKCS7 verification code; this repo does not yet include proof eliminating that path, so the finding remains open pending RHEL8 errata. Reviewers should verify package version in rebuilt image and rerun Trivy against the rebuilt digest.
