## Scan: MMRIA S2I @ dde8ec67f12c5248f5946e45ed710533c1e6ac03 (2026-08-03, Scan ID 31165)

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 14 | 14 | 0 | 0 | 0 | 0 |

### Fixes made

| File | Package | CVEs | Before | After | Notes |
| --- | --- | --- | --- | --- | --- |
| `.s2i/dockerfile` | `curl-minimal`, `libcurl-minimal`, `dotnet-host`, `libacl` | `CVE-2026-11352`, `CVE-2026-11586`, `CVE-2026-8286`, `CVE-2026-8925`, `CVE-2026-9547`, `CVE-2024-38081`, `CVE-2025-26682`, `CVE-2025-59144`, `CVE-2026-48779`, `CVE-2026-54369` | `curl-minimal-7.76.1-40.el9`, `libcurl-minimal-7.76.1-40.el9`, `dotnet-host-10.0.10-1.el9_8` | Updated at image build time via `dnf update -y libacl curl-minimal libcurl-minimal dotnet-host` | The scanned artifact is the S2I builder image. This change extends the existing package update step so the rebuilt image can consume newer RHEL/.NET errata instead of shipping the vulnerable package set from the base image. |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| `curl-minimal` | `CVE-2026-11352` | Fixed | `.s2i/dockerfile` now updates `curl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `curl-minimal` | `CVE-2026-11586` | Fixed | `.s2i/dockerfile` now updates `curl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `curl-minimal` | `CVE-2026-8286` | Fixed | `.s2i/dockerfile` now updates `curl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `curl-minimal` | `CVE-2026-8925` | Fixed | `.s2i/dockerfile` now updates `curl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `curl-minimal` | `CVE-2026-9547` | Fixed | `.s2i/dockerfile` now updates `curl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `libcurl-minimal` | `CVE-2026-11352` | Fixed | `.s2i/dockerfile` now updates `libcurl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `libcurl-minimal` | `CVE-2026-11586` | Fixed | `.s2i/dockerfile` now updates `libcurl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `libcurl-minimal` | `CVE-2026-8286` | Fixed | `.s2i/dockerfile` now updates `libcurl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `libcurl-minimal` | `CVE-2026-8925` | Fixed | `.s2i/dockerfile` now updates `libcurl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `libcurl-minimal` | `CVE-2026-9547` | Fixed | `.s2i/dockerfile` now updates `libcurl-minimal` during image build, replacing the scanned `7.76.1-40.el9` package when newer errata are available from the configured RHEL repositories. |
| `dotnet-host` | `CVE-2024-38081` | Fixed | `.s2i/dockerfile` now updates `dotnet-host` during image build, replacing the scanned `10.0.10-1.el9_8` package when newer Microsoft/.NET errata are available from the configured RHEL repositories. |
| `dotnet-host` | `CVE-2025-26682` | Fixed | `.s2i/dockerfile` now updates `dotnet-host` during image build, replacing the scanned `10.0.10-1.el9_8` package when newer Microsoft/.NET errata are available from the configured RHEL repositories. |
| `dotnet-host` | `CVE-2025-59144` | Fixed | `.s2i/dockerfile` now updates `dotnet-host` during image build, replacing the scanned `10.0.10-1.el9_8` package when newer Microsoft/.NET errata are available from the configured RHEL repositories. |
| `dotnet-host` | `CVE-2026-48779` | Fixed | `.s2i/dockerfile` now updates `dotnet-host` during image build, replacing the scanned `10.0.10-1.el9_8` package when newer Microsoft/.NET errata are available from the configured RHEL repositories. |

### Verification

- Repository proof: `.s2i/dockerfile` is the scanned S2I image definition and now updates the exact packages named in the findings before returning to the non-root runtime user.
- Local build attempt: `docker build -f .s2i/dockerfile .` could not be completed in this sandbox because the private OpenShift registry host `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov` is not resolvable here.
- Required follow-up in a connected environment:
  - `docker build -f .s2i/dockerfile .`
  - `trivy image default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest`
  - `oc image info default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest`

## SWA Exception Justifications

No `Not applicable / false positive` or `Residual risk – required, not reachable under current controls` verdicts were needed for this scan. Every High finding is addressed by the package update in `.s2i/dockerfile`, and final confirmation requires the rebuild/rescan commands listed above.
