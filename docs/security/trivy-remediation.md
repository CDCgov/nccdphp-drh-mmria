# Trivy Remediation Log

## Scan: MMRIA S2I @ 1a75c40a — C:0 H:18

- **Commit:** `1a75c40a3c8e85a9b2ada53141dca2cd3409687c`
- **Service:** `MMRIA S2I`
- **Scan ID:** `30985`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-07-24

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 18 | 18 | 0 | 0 | 0 | 0 |

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-11352` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `curl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-11586` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `curl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-12064` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `curl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-8286` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `curl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-8925` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `curl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-9547` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `curl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2024-38081` | HIGH | end_of_life | `10.0.10-1.el9_8` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `dotnet-host`, `dotnet-hostfxr-10.0`, `dotnet-runtime-10.0`, `aspnetcore-runtime-10.0`, and `dotnet-sdk-10.0` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2025-26682` | HIGH | end_of_life | `10.0.10-1.el9_8` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `dotnet-host`, `dotnet-hostfxr-10.0`, `dotnet-runtime-10.0`, `aspnetcore-runtime-10.0`, and `dotnet-sdk-10.0` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2025-59144` | HIGH | end_of_life | `10.0.10-1.el9_8` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `dotnet-host`, `dotnet-hostfxr-10.0`, `dotnet-runtime-10.0`, `aspnetcore-runtime-10.0`, and `dotnet-sdk-10.0` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2026-48779` | HIGH | under_investigation | `10.0.10-1.el9_8` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `dotnet-host`, `dotnet-hostfxr-10.0`, `dotnet-runtime-10.0`, `aspnetcore-runtime-10.0`, and `dotnet-sdk-10.0` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-11352` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `libcurl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-11586` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `libcurl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-12064` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `libcurl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-8286` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `libcurl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-8925` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `libcurl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-9547` | HIGH | affected | `7.76.1-40.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `libcurl-minimal` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `tar` | `CVE-2026-59873` | HIGH | affected | `2:1.34-11.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `tar` during image build. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `tar` | `CVE-2026-59874` | HIGH | affected | `2:1.34-11.el9` | latest available from RHEL 9.8 errata at build time | Fixed | `.s2i/dockerfile` now runs `dnf upgrade --refresh` for `tar` during image build. |

## HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| `curl-minimal` | `CVE-2026-11352` | Fixed | `.s2i/dockerfile` upgrades `curl-minimal` at build time. |
| `curl-minimal` | `CVE-2026-11586` | Fixed | `.s2i/dockerfile` upgrades `curl-minimal` at build time. |
| `curl-minimal` | `CVE-2026-12064` | Fixed | `.s2i/dockerfile` upgrades `curl-minimal` at build time. |
| `curl-minimal` | `CVE-2026-8286` | Fixed | `.s2i/dockerfile` upgrades `curl-minimal` at build time. |
| `curl-minimal` | `CVE-2026-8925` | Fixed | `.s2i/dockerfile` upgrades `curl-minimal` at build time. |
| `curl-minimal` | `CVE-2026-9547` | Fixed | `.s2i/dockerfile` upgrades `curl-minimal` at build time. |
| `dotnet-host` | `CVE-2024-38081` | Fixed | `.s2i/dockerfile` upgrades the .NET host/runtime/sdk RPM stack at build time. |
| `dotnet-host` | `CVE-2025-26682` | Fixed | `.s2i/dockerfile` upgrades the .NET host/runtime/sdk RPM stack at build time. |
| `dotnet-host` | `CVE-2025-59144` | Fixed | `.s2i/dockerfile` upgrades the .NET host/runtime/sdk RPM stack at build time. |
| `dotnet-host` | `CVE-2026-48779` | Fixed | `.s2i/dockerfile` upgrades the .NET host/runtime/sdk RPM stack at build time. |
| `libcurl-minimal` | `CVE-2026-11352` | Fixed | `.s2i/dockerfile` upgrades `libcurl-minimal` at build time. |
| `libcurl-minimal` | `CVE-2026-11586` | Fixed | `.s2i/dockerfile` upgrades `libcurl-minimal` at build time. |
| `libcurl-minimal` | `CVE-2026-12064` | Fixed | `.s2i/dockerfile` upgrades `libcurl-minimal` at build time. |
| `libcurl-minimal` | `CVE-2026-8286` | Fixed | `.s2i/dockerfile` upgrades `libcurl-minimal` at build time. |
| `libcurl-minimal` | `CVE-2026-8925` | Fixed | `.s2i/dockerfile` upgrades `libcurl-minimal` at build time. |
| `libcurl-minimal` | `CVE-2026-9547` | Fixed | `.s2i/dockerfile` upgrades `libcurl-minimal` at build time. |
| `tar` | `CVE-2026-59873` | Fixed | `.s2i/dockerfile` upgrades `tar` at build time. |
| `tar` | `CVE-2026-59874` | Fixed | `.s2i/dockerfile` upgrades `tar` at build time. |

## Verification

- Repo-static verification completed:
  - reviewed `.s2i/bin/assemble` and `.s2i/bin/run`; `curl` only appears in a commented-out legacy line, while `tar` is still used when `DOTNET_PACK=true`
  - confirmed the vulnerable packages are now explicitly covered by `.s2i/dockerfile`, which is safer than removing `tar` from the S2I builder image
- CI log investigation attempted via GitHub MCP using workflow run `30112194673`, but the external `cdcent/nccdphp-od-devops` Actions API returned `404`, so no upstream build logs were available from this session
- Pod/rebuild checks were not run from this sandbox. Run the following after the next image build:

```shell
docker build -f .s2i/dockerfile -t mmria-s2i:trivy-fix .
trivy image --severity HIGH,CRITICAL mmria-s2i:trivy-fix
```

## SWA Exception Justifications

No HIGH or CRITICAL findings from scan `30985` require an SWA exception after the `.s2i/dockerfile` package-upgrade remediation.
