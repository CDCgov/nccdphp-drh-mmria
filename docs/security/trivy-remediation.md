# Trivy Remediation Log

## Scan: 2026-07-24 | Service: MMRIA Services | Scan ID: 30983 | Commit: 1a75c40a3c8e85a9b2ada53141dca2cd3409687c

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 18 | 18 | 0 | 0 | 0 | 0 |

### Fixes made

- File: `nccdphp-drh-mmria-services/mmria.services/Dockerfile`
- Change: runtime stage package update expanded from `libacl` only to `libacl curl-minimal libcurl-minimal tar dotnet-host`
- CVEs addressed in scan scope: `CVE-2026-11352`, `CVE-2026-11586`, `CVE-2026-12064`, `CVE-2026-8286`, `CVE-2026-8925`, `CVE-2026-9547`, `CVE-2026-59873`, `CVE-2026-59874`, `CVE-2024-38081`, `CVE-2025-26682`, `CVE-2025-59144`, `CVE-2026-48779`
- Evidence: runtime image now performs package refresh for all affected OS packages during build.

### HIGH/CRITICAL finding inventory

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| curl-minimal | CVE-2026-11352 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `curl-minimal`. |
| curl-minimal | CVE-2026-11586 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `curl-minimal`. |
| curl-minimal | CVE-2026-12064 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `curl-minimal`. |
| curl-minimal | CVE-2026-8286 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `curl-minimal`. |
| curl-minimal | CVE-2026-8925 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `curl-minimal`. |
| curl-minimal | CVE-2026-9547 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `curl-minimal`. |
| libcurl-minimal | CVE-2026-11352 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `libcurl-minimal`. |
| libcurl-minimal | CVE-2026-11586 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `libcurl-minimal`. |
| libcurl-minimal | CVE-2026-12064 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `libcurl-minimal`. |
| libcurl-minimal | CVE-2026-8286 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `libcurl-minimal`. |
| libcurl-minimal | CVE-2026-8925 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `libcurl-minimal`. |
| libcurl-minimal | CVE-2026-9547 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `libcurl-minimal`. |
| dotnet-host | CVE-2024-38081 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `dotnet-host`. |
| dotnet-host | CVE-2025-26682 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `dotnet-host`. |
| dotnet-host | CVE-2025-59144 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `dotnet-host`. |
| dotnet-host | CVE-2026-48779 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `dotnet-host`. |
| tar | CVE-2026-59873 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `tar`. |
| tar | CVE-2026-59874 | Fixed | Dockerfile runtime `dnf/microdnf update` now includes `tar`. |

### Verification / handoff commands

```bash
# Rebuild service image with updated Dockerfile
podman build -f nccdphp-drh-mmria-services/mmria.services/Dockerfile -t mmria-services:trivy-30983 .

# Rescan rebuilt image
trivy image --severity HIGH,CRITICAL mmria-services:trivy-30983
```

## SWA Exception Justifications

No `Not applicable / false positive` or `Residual risk – required, not reachable under current controls` exceptions were used for this scan.
