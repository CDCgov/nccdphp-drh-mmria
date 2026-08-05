# Trivy Remediation Log

## Scan: 31248 @ 583a5ff6 — C:0 H:14

- **Commit:** `583a5ff6eaf36606f4f643bf5c5e2d971dbd79db`
- **Service:** `MMRIA S2I`
- **Scan ID:** `31248`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-08-05

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| High | 14 | 10 | 0 | 4 | 0 | 4 |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
|---|---|---|---|---|---|---|---|---|
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-11352 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable curl client package is no longer kept in the customized S2I image. `.s2i/bin/assemble` and `.s2i/bin/run` contain no active curl invocations, so removing curl does not remove a referenced runtime dependency from this repo. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-11586 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable curl client package is no longer kept in the customized S2I image. `.s2i/bin/assemble` and `.s2i/bin/run` contain no active curl invocations, so removing curl does not remove a referenced runtime dependency from this repo. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-8286 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable curl client package is no longer kept in the customized S2I image. `.s2i/bin/assemble` and `.s2i/bin/run` contain no active curl invocations, so removing curl does not remove a referenced runtime dependency from this repo. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-8925 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable curl client package is no longer kept in the customized S2I image. `.s2i/bin/assemble` and `.s2i/bin/run` contain no active curl invocations, so removing curl does not remove a referenced runtime dependency from this repo. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | curl-minimal | CVE-2026-9547 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable curl client package is no longer kept in the customized S2I image. `.s2i/bin/assemble` and `.s2i/bin/run` contain no active curl invocations, so removing curl does not remove a referenced runtime dependency from this repo. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-11352 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable libcurl runtime library is no longer kept in the customized S2I image. The checked-in S2I scripts use shell utilities and `dotnet`; they do not call curl or link repo code against the system libcurl package. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-11586 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable libcurl runtime library is no longer kept in the customized S2I image. The checked-in S2I scripts use shell utilities and `dotnet`; they do not call curl or link repo code against the system libcurl package. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-8286 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable libcurl runtime library is no longer kept in the customized S2I image. The checked-in S2I scripts use shell utilities and `dotnet`; they do not call curl or link repo code against the system libcurl package. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-8925 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable libcurl runtime library is no longer kept in the customized S2I image. The checked-in S2I scripts use shell utilities and `dotnet`; they do not call curl or link repo code against the system libcurl package. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | libcurl-minimal | CVE-2026-9547 | HIGH | affected | 7.76.1-40.el9 | none published in findings | Fixed | `.s2i/dockerfile` now runs `dnf remove -y curl-minimal libcurl-minimal` after the existing `libacl` update, so the vulnerable libcurl runtime library is no longer kept in the customized S2I image. The checked-in S2I scripts use shell utilities and `dotnet`; they do not call curl or link repo code against the system libcurl package. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2024-38081 | HIGH | end_of_life | 10.0.10-1.el9_8 | none published in findings | Residual risk – required, not reachable under current controls | The finding is on `dotnet-host` inside the Red Hat .NET 10 S2I base image selected by `.s2i/dockerfile`, not on an application NuGet dependency in this repository. Repo evidence can update the derived image layer but cannot replace the platform-provided `dotnet-host` package independently. NVD/OSV classify this as a .NET elevation-of-privilege issue, so confirmation requires a rebuilt image or live package inspection after the trusted base image is refreshed. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2025-26682 | HIGH | end_of_life | 10.0.10-1.el9_8 | none published in findings | Residual risk – required, not reachable under current controls | The finding is on `dotnet-host` inside the Red Hat .NET 10 S2I base image selected by `.s2i/dockerfile`, not on an application NuGet dependency in this repository. OSV describes this issue as ASP.NET Core resource exhaustion over the network. The customized S2I image itself is only a build image extension, but the installed `dotnet-host` package remains part of the trusted base image and cannot be surgically replaced here without a platform image refresh or image rebuild verification. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2025-59144 | HIGH | end_of_life | 10.0.10-1.el9_8 | none published in findings | Residual risk – required, not reachable under current controls | The scanner attributed the finding to `dotnet-host`, but NVD and Red Hat describe CVE-2025-59144 as the malicious `debug` npm package in browser-bundled JavaScript, not a .NET runtime defect. Repo evidence shows the S2I customization does not add browser bundling steps and the app build comments out npm work in `.s2i/bin/assemble`, but proving the flagged package is absent from the final image still requires a live package/file-system inspection of the built S2I image. |
| default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8) | dotnet-host | CVE-2026-48779 | HIGH | under_investigation | 10.0.10-1.el9_8 | none published in findings | Residual risk – required, not reachable under current controls | The scanner attributed the finding to `dotnet-host`, but NVD/OSV describe CVE-2026-48779 as a `ws` Node.js WebSocket package memory exhaustion issue, not a .NET runtime defect. Repo evidence shows the customized S2I image is for .NET build/assemble flow and the checked-in `.s2i/bin/assemble` has its npm section commented out, but proving that no vulnerable `ws` package is present in the built image requires live image or pod inspection that is outside repo-only access. |

## HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| dotnet-host | CVE-2024-38081 | Residual risk – required, not reachable under current controls | Base-image `dotnet-host` package finding; repo can document and hand off verification but cannot independently replace the trusted image package. |
| dotnet-host | CVE-2025-26682 | Residual risk – required, not reachable under current controls | Base-image `dotnet-host` package finding; OSV describes ASP.NET Core resource exhaustion, but the S2I customization does not control the packaged host version. |
| dotnet-host | CVE-2025-59144 | Residual risk – required, not reachable under current controls | CVE scope is browser-bundled `debug` malware rather than a .NET host defect, but proving absence from the produced image needs live inspection. |
| dotnet-host | CVE-2026-48779 | Residual risk – required, not reachable under current controls | CVE scope is Node `ws` package memory exhaustion rather than a .NET host defect, but proving absence from the produced image needs live inspection. |

### dotnet-host / CVE-2024-38081
- Finding: target `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`, package `dotnet-host`, CVE `CVE-2024-38081`, severity `HIGH`, status `end_of_life`, installed `10.0.10-1.el9_8`, fixed version `none published in findings`
- Remediation attempted: remediated removable curl packages in `.s2i/dockerfile`; no repo-only package substitution exists for the trusted-image `dotnet-host` base package.
- Why not fixed here: the vulnerable component is delivered by the trusted `dotnet-100` S2I base image referenced in `.s2i/dockerfile`; this repo cannot publish a replacement base package independently.
- Usage/reachability: the checked-in `.s2i` customization only adjusts the base image layer and provides assemble/run scripts; it does not vendor a separate .NET host runtime.
- Exploit preconditions: NVD/OSV describe an elevation-of-privilege issue in .NET/.NET Framework/Visual Studio, which requires the vulnerable runtime implementation to remain installed in the rebuilt image.
- Compensating controls: the customized image continues to run as non-root (`USER 1001`) after build customization, reducing immediate privilege available inside the container, but verifying package replacement requires a rebuilt image or pod package listing.
- Reviewer verification: `oc rsh <mmria-s2i-pod> rpm -q dotnet-host && oc rsh <mmria-s2i-pod> id -u` and `trivy image default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest --severity HIGH,CRITICAL`
- Follow-up: refresh the trusted `dotnet-100` base image digest when Red Hat ships an updated `dotnet-host` package and rescan the rebuilt S2I image.

### dotnet-host / CVE-2025-26682
- Finding: target `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`, package `dotnet-host`, CVE `CVE-2025-26682`, severity `HIGH`, status `end_of_life`, installed `10.0.10-1.el9_8`, fixed version `none published in findings`
- Remediation attempted: remediated removable curl packages in `.s2i/dockerfile`; no repo-only package substitution exists for the trusted-image `dotnet-host` base package.
- Why not fixed here: the vulnerable component is delivered by the trusted `dotnet-100` S2I base image referenced in `.s2i/dockerfile`; this repo cannot publish a replacement base package independently.
- Usage/reachability: `.s2i/bin/assemble` builds the application and `.s2i/bin/run` launches the produced .NET entrypoint, so the image does need the platform `dotnet` host to exist.
- Exploit preconditions: OSV describes unauthorized network-triggered resource exhaustion in ASP.NET Core, meaning exploitability depends on the exact runtime bits that remain installed in the rebuilt base image.
- Compensating controls: the issue is confined to the platform-provided host package in the trusted base image; validating whether a newer trusted image or package erratum clears it requires live `rpm -q` and image rescan evidence.
- Reviewer verification: `oc rsh <mmria-s2i-pod> rpm -q dotnet-host` and `trivy image default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest --severity HIGH,CRITICAL`
- Follow-up: refresh the trusted `dotnet-100` base image digest when Red Hat ships an updated `dotnet-host` package and rescan the rebuilt S2I image.

### dotnet-host / CVE-2025-59144
- Finding: target `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`, package `dotnet-host`, CVE `CVE-2025-59144`, severity `HIGH`, status `end_of_life`, installed `10.0.10-1.el9_8`, fixed version `none published in findings`
- Remediation attempted: remediated removable curl packages in `.s2i/dockerfile`; inspected `.s2i/bin/assemble`, which has npm install/build lines commented out instead of active.
- Why not fixed here: the repository does not package `debug` into the S2I customization itself, but proving the scanned image does not contain the flagged browser-side artifact requires live file-system or package inspection of the built image.
- Usage/reachability: NVD/Red Hat describe this CVE as malicious browser-targeted `debug` 4.4.2 JavaScript content. The customized S2I image is a .NET builder image extension and the checked-in assemble script does not execute npm bundling.
- Exploit preconditions: the vulnerable path requires the malicious `debug` browser bundle content to be present and then served or executed in a browser-oriented build pipeline.
- Compensating controls: repo evidence narrows the likely issue to scanner attribution noise on the base image, but the required proof is a live check showing that no matching `debug` package or browser bundle is present in the final image.
- Reviewer verification: `oc rsh <mmria-s2i-pod> find /opt/app-root -path '*node_modules/debug*' -o -name 'debug*.js' | head` and `oc rsh <mmria-s2i-pod> rpm -q dotnet-host`
- Follow-up: if live inspection shows no `debug` payload in the image, upgrade this verdict to `Not applicable / false positive`; otherwise refresh the trusted base image and rescan.

### dotnet-host / CVE-2026-48779
- Finding: target `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`, package `dotnet-host`, CVE `CVE-2026-48779`, severity `HIGH`, status `under_investigation`, installed `10.0.10-1.el9_8`, fixed version `none published in findings`
- Remediation attempted: remediated removable curl packages in `.s2i/dockerfile`; inspected `.s2i/bin/assemble`, which has npm install/build lines commented out instead of active.
- Why not fixed here: the repository does not package the Node `ws` library into the S2I customization itself, but proving the scanned image does not contain the flagged dependency requires live file-system inspection of the built image.
- Usage/reachability: NVD/OSV describe this CVE as a Node.js `ws` WebSocket package issue. The customized S2I image is a .NET builder image extension and the checked-in assemble script does not execute npm dependency installation.
- Exploit preconditions: the vulnerable path requires the `ws` package to be present in a Node environment that accepts attacker-controlled fragmented WebSocket traffic.
- Compensating controls: repo evidence narrows the likely issue to scanner attribution noise on the base image, but the required proof is a live check showing that no matching `ws` dependency is present in the final image.
- Reviewer verification: `oc rsh <mmria-s2i-pod> find /opt/app-root -path '*node_modules/ws*' | head` and `oc rsh <mmria-s2i-pod> rpm -q dotnet-host`
- Follow-up: if live inspection shows no `ws` dependency in the image, upgrade this verdict to `Not applicable / false positive`; otherwise refresh the trusted base image and rescan.

## Verification

- Repo change: `.s2i/dockerfile` now removes `curl-minimal` and `libcurl-minimal` after the existing `libacl` package update.
- Static reachability review: `.s2i/bin/assemble` and `.s2i/bin/run` contain no active curl usage; npm build steps are commented out in `.s2i/bin/assemble`.
- Validation run: `dotnet build source-code/mmria/mmria.sln --nologo` completed successfully in this repo after the Dockerfile change.
- Validation run: `dotnet test source-code/mmria/mmria.sln --nologo` completed restore without surfacing code-test failures; no compiled source files were modified for this remediation.
- Limitations: pod inspection, rebuilt-image package verification, and Trivy rescans are handed off because this environment cannot rebuild the OpenShift image or `oc rsh` into a running pod.
- Suggested rescan after image rebuild: `trivy image default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest --severity HIGH,CRITICAL`

## SWA Exception Justifications

### dotnet-host / CVE-2024-38081

- **CVE:** CVE-2024-38081
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** affected
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The remaining finding is tied to the trusted `dotnet-host` package in the Red Hat S2I base image, which this repository cannot replace independently. The custom layer keeps the image non-root after customization and hands off rebuilt-image verification for the platform package.

This repository only customizes the trusted `dotnet-100` S2I image in `.s2i/dockerfile`; it does not vendor or publish a separate `.NET` host package. NVD and OSV classify CVE-2024-38081 as a .NET elevation-of-privilege issue, so the relevant precondition is whether the vulnerable `dotnet-host` package remains installed in the rebuilt image. The customized image switches back to `USER 1001` after package changes, which limits available privileges inside the container, but the authoritative proof still requires live `rpm -q dotnet-host`, `id -u`, and a post-build Trivy rescan.

### dotnet-host / CVE-2025-26682

- **CVE:** CVE-2025-26682
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** affected
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The remaining finding is tied to the trusted `dotnet-host` package in the Red Hat S2I base image, which this repository cannot replace independently. The repo can only document and hand off rebuilt-image verification until the trusted base image is refreshed.

This repository only customizes the trusted `dotnet-100` S2I image in `.s2i/dockerfile`; it does not vendor or publish a separate ASP.NET Core runtime package. OSV describes CVE-2025-26682 as unauthorized network-triggered resource exhaustion in ASP.NET Core, so exploitability depends on the exact runtime bits that remain installed in the rebuilt image. The checked-in `.s2i` content shows a build-image extension rather than an application-specific runtime fork, so the required proof is live `rpm -q dotnet-host` output plus a rebuilt-image Trivy rescan after the trusted base image digest is refreshed.

### dotnet-host / CVE-2025-59144

- **CVE:** CVE-2025-59144
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** affected
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** Scanner output attributed this finding to `dotnet-host`, but the CVE scope is malicious browser-bundled `debug` JavaScript rather than a .NET runtime defect. Repo inspection narrows this to likely base-image attribution noise, but live image inspection is still required before calling it not applicable.

NVD and Red Hat describe CVE-2025-59144 as the compromised `debug` 4.4.2 npm package with malware that affects browser bundle contexts, not ordinary local or server-side runtime usage. The checked-in `.s2i/bin/assemble` script has its npm install and npm build steps commented out, so this repository does not currently inject browser-bundle tooling into the S2I customization path. However, repo-only evidence cannot prove the scanned image lacks all matching `debug` artifacts from the inherited base image, so the safest current verdict is residual risk with handoff commands to inspect `/opt/app-root` and rescan the rebuilt image.

### dotnet-host / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** affected
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** Scanner output attributed this finding to `dotnet-host`, but the CVE scope is the Node.js `ws` package rather than a .NET runtime defect. Repo inspection narrows this to likely base-image attribution noise, but live image inspection is still required before calling it not applicable.

NVD and OSV describe CVE-2026-48779 as a memory exhaustion issue in the Node.js `ws` package that requires the vulnerable dependency to be present in an environment accepting fragmented WebSocket traffic. The checked-in `.s2i/bin/assemble` script has its npm install and npm build steps commented out, so this repository does not currently inject Node dependency installation into the S2I customization path. However, repo-only evidence cannot prove the scanned image lacks all matching `ws` artifacts from the inherited base image, so the safest current verdict is residual risk with handoff commands to inspect `/opt/app-root` and rescan the rebuilt image.
