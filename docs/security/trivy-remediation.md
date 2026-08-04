## Scan: MMRIA S2I @ 8d320931b4a37e4a2dad436ded62b2a508f3a4d7 (2026-08-04)

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| High | 14 | 10 | 4 | 0 | 0 | 4 |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |

### Fixes made

| File | Package/image | CVEs | Before | After | Notes |
| --- | --- | --- | --- | --- | --- |
| `.s2i/dockerfile` | `curl-minimal`, `libcurl-minimal` | CVE-2026-11352, CVE-2026-11586, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547 | installed from `trusted-images/dotnet-100:9.8-1784594615` | removed with `dnf remove -y curl-minimal libcurl-minimal` | `.s2i/bin/assemble` and `.s2i/bin/run` do not execute `curl`; removing the packages shrinks the S2I attack surface without changing the build flow. |

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| curl-minimal | CVE-2026-11352 | Fixed | `.s2i/dockerfile` removes `curl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| curl-minimal | CVE-2026-11586 | Fixed | `.s2i/dockerfile` removes `curl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| curl-minimal | CVE-2026-8286 | Fixed | `.s2i/dockerfile` removes `curl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| curl-minimal | CVE-2026-8925 | Fixed | `.s2i/dockerfile` removes `curl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| curl-minimal | CVE-2026-9547 | Fixed | `.s2i/dockerfile` removes `curl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| libcurl-minimal | CVE-2026-11352 | Fixed | `.s2i/dockerfile` removes `libcurl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| libcurl-minimal | CVE-2026-11586 | Fixed | `.s2i/dockerfile` removes `libcurl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| libcurl-minimal | CVE-2026-8286 | Fixed | `.s2i/dockerfile` removes `libcurl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| libcurl-minimal | CVE-2026-8925 | Fixed | `.s2i/dockerfile` removes `libcurl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| libcurl-minimal | CVE-2026-9547 | Fixed | `.s2i/dockerfile` removes `libcurl-minimal`; `.s2i/bin/assemble` contains only a commented curl line and `.s2i/bin/run` invokes `dotnet`, so the package is not required by repo-tracked S2I logic. |
| dotnet-host | CVE-2024-38081 | Pending — base image update | The finding is reported against the inherited `trusted-images/dotnet-100:9.8-1784594615` S2I SDK base. This repo does not build or vendor `dotnet-host`; the only repo-controlled action is the pinned `FROM` reference in `.s2i/dockerfile`. Updating to a newer approved `trusted-images/dotnet-100` digest is required to replace the vulnerable RPM, then the image must be rebuilt and rescanned. |
| dotnet-host | CVE-2025-26682 | Pending — base image update | The finding is reported against the inherited `trusted-images/dotnet-100:9.8-1784594615` S2I SDK base. This repo does not build or vendor `dotnet-host`; the only repo-controlled action is the pinned `FROM` reference in `.s2i/dockerfile`. Updating to a newer approved `trusted-images/dotnet-100` digest is required to replace the vulnerable RPM, then the image must be rebuilt and rescanned. |
| dotnet-host | CVE-2025-59144 | Pending — base image update | The finding is reported against the inherited `trusted-images/dotnet-100:9.8-1784594615` S2I SDK base. This repo does not build or vendor `dotnet-host`; the only repo-controlled action is the pinned `FROM` reference in `.s2i/dockerfile`. Updating to a newer approved `trusted-images/dotnet-100` digest is required to replace the vulnerable RPM, then the image must be rebuilt and rescanned. |
| dotnet-host | CVE-2026-48779 | Pending — base image update | The finding is reported against the inherited `trusted-images/dotnet-100:9.8-1784594615` S2I SDK base. This repo does not build or vendor `dotnet-host`; the only repo-controlled action is the pinned `FROM` reference in `.s2i/dockerfile`. Updating to a newer approved `trusted-images/dotnet-100` digest is required to replace the vulnerable RPM, then the image must be rebuilt and rescanned. |

### Finding: dotnet-host / CVE-2024-38081
- **Verdict:** Pending — base image update
- **Why:** OSV describes CVE-2024-38081 as a .NET elevation-of-privilege issue. The vulnerable `dotnet-host` RPM is inherited from the internal `trusted-images/dotnet-100:9.8-1784594615` image pinned in `.s2i/dockerfile`; this repository does not produce that RPM itself.
- **Verification:** Rebuild the S2I image after the trusted image team publishes a newer `dotnet-100` digest and verify `rpm -q dotnet-host` no longer reports `10.0.10-1.el9_8`.
- **Command:** `oc rsh <mmria-s2i-pod> rpm -q dotnet-host`

### Finding: dotnet-host / CVE-2025-26682
- **Verdict:** Pending — base image update
- **Why:** OSV describes CVE-2025-26682 as an ASP.NET Core resource-exhaustion denial of service. Trivy attributes it to the inherited `dotnet-host` RPM in `trusted-images/dotnet-100:9.8-1784594615`, and this repo cannot independently patch that base RPM without a newer approved parent image digest.
- **Verification:** Rebuild the S2I image after the trusted image team publishes a newer `dotnet-100` digest and verify `rpm -q dotnet-host` no longer reports `10.0.10-1.el9_8`.
- **Command:** `oc rsh <mmria-s2i-pod> rpm -q dotnet-host`

### Finding: dotnet-host / CVE-2025-59144
- **Verdict:** Pending — base image update
- **Why:** Trivy reports CVE-2025-59144 on the inherited `dotnet-host` package within `trusted-images/dotnet-100:9.8-1784594615`. The S2I customization in this repo only layers shell steps on top of that base and cannot remediate a vulnerable base RPM until the parent image digest is updated.
- **Verification:** Rebuild the S2I image after the trusted image team publishes a newer `dotnet-100` digest and verify `rpm -q dotnet-host` no longer reports `10.0.10-1.el9_8`.
- **Command:** `oc rsh <mmria-s2i-pod> rpm -q dotnet-host`

### Finding: dotnet-host / CVE-2026-48779
- **Verdict:** Pending — base image update
- **Why:** OSV describes CVE-2026-48779 as a `ws` memory exhaustion issue. Trivy maps it to the inherited `dotnet-host` package in the pinned `trusted-images/dotnet-100:9.8-1784594615` base, so this repo can only remediate it by consuming a parent image digest where the vulnerable component mapping is gone.
- **Verification:** Rebuild the S2I image after the trusted image team publishes a newer `dotnet-100` digest and verify `rpm -q dotnet-host` no longer reports `10.0.10-1.el9_8`.
- **Command:** `oc rsh <mmria-s2i-pod> rpm -q dotnet-host`

## SWA Exception Justifications

### dotnet-host / CVE-2024-38081
**Verdict:** Pending — base image update
**Summary:** The vulnerable `dotnet-host` RPM is inherited from the pinned internal `trusted-images/dotnet-100:9.8-1784594615` S2I base image rather than produced in this repository, so remediation requires consuming an updated approved parent image digest and rescanning the rebuilt S2I image.
**Justification:** `.s2i/dockerfile` pins `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1784594615@sha256:c71106c2ad6009ee6238dc383f8bf4afe8539df2e4868ca0e1d9e1a724fe1d02` as the S2I base image, and the finding reports `dotnet-host 10.0.10-1.el9_8` from that inherited layer. OSV identifies CVE-2024-38081 as a .NET elevation-of-privilege issue; because this repository does not build or vendor the `dotnet-host` RPM, the smallest safe remediation available here is to record the required base-image update and hand off rebuild verification with `oc rsh <mmria-s2i-pod> rpm -q dotnet-host` plus a post-rebuild Trivy rescan.

### dotnet-host / CVE-2025-26682
**Verdict:** Pending — base image update
**Summary:** The vulnerable `dotnet-host` RPM is inherited from the pinned internal `trusted-images/dotnet-100:9.8-1784594615` S2I base image rather than produced in this repository, so remediation requires consuming an updated approved parent image digest and rescanning the rebuilt S2I image.
**Justification:** `.s2i/dockerfile` pins `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1784594615@sha256:c71106c2ad6009ee6238dc383f8bf4afe8539df2e4868ca0e1d9e1a724fe1d02` as the S2I base image, and the finding reports `dotnet-host 10.0.10-1.el9_8` from that inherited layer. OSV describes CVE-2025-26682 as an ASP.NET Core resource-exhaustion denial of service; because this repository does not build or vendor the `dotnet-host` RPM, the actionable repository-side step is to move to a newer approved parent digest and verify the rebuilt image with `oc rsh <mmria-s2i-pod> rpm -q dotnet-host` plus a Trivy rescan.

### dotnet-host / CVE-2025-59144
**Verdict:** Pending — base image update
**Summary:** The vulnerable `dotnet-host` RPM is inherited from the pinned internal `trusted-images/dotnet-100:9.8-1784594615` S2I base image rather than produced in this repository, so remediation requires consuming an updated approved parent image digest and rescanning the rebuilt S2I image.
**Justification:** `.s2i/dockerfile` pins `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1784594615@sha256:c71106c2ad6009ee6238dc383f8bf4afe8539df2e4868ca0e1d9e1a724fe1d02` as the S2I base image, and the finding reports `dotnet-host 10.0.10-1.el9_8` from that inherited layer. Trivy associates CVE-2025-59144 with that base-provided package; this repository only customizes the S2I layer and cannot replace the vulnerable RPM until a newer approved `trusted-images/dotnet-100` digest is available, after which the rebuilt image must be verified with `oc rsh <mmria-s2i-pod> rpm -q dotnet-host` and rescanned.

### dotnet-host / CVE-2026-48779
**Verdict:** Pending — base image update
**Summary:** The vulnerable `dotnet-host` RPM is inherited from the pinned internal `trusted-images/dotnet-100:9.8-1784594615` S2I base image rather than produced in this repository, so remediation requires consuming an updated approved parent image digest and rescanning the rebuilt S2I image.
**Justification:** `.s2i/dockerfile` pins `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1784594615@sha256:c71106c2ad6009ee6238dc383f8bf4afe8539df2e4868ca0e1d9e1a724fe1d02` as the S2I base image, and the finding reports `dotnet-host 10.0.10-1.el9_8` from that inherited layer. OSV describes CVE-2026-48779 as a `ws` memory exhaustion issue; because this repository does not build or vendor the inherited `dotnet-host` RPM, the only safe repository-side disposition is to require a parent image update and then verify the rebuilt image with `oc rsh <mmria-s2i-pod> rpm -q dotnet-host` plus a Trivy rescan.
