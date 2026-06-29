# Trivy Remediation Log

## Scan: 45 @ e5009f9b — C:0 H:14

- **Commit:** `e5009f9b79f61b736ae6e39facf8272c56817f4c`
- **Service:** `45`
- **Scan ID:** `30382`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-06-29

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| High | 14 | 5 | 0 | 9 | 0 | 9 |

`⏳ EVIDENCE WOULD UPGRADE` candidates: the nine residual findings below could be upgraded if a live container check proves the Node.js `ws` or `decompress` packages are absent from the final runtime filesystem.

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `aspnetcore-runtime-9.0` | `CVE-2026-45591` | HIGH | fixed | `9.0.16-1.el8_10` | `9.0.17-1.el8_10` | Fixed | `/source-code/mmria/mmria-server/Dockerfile` updates the final runtime stage packages with `microdnf update`, including `aspnetcore-runtime-9.0`; rebuild and rescan are still required to verify the new layer contents. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `aspnetcore-runtime-9.0` | `CVE-2026-45736` | HIGH | affected | `9.0.16-1.el8_10` |  | Residual risk – required, not reachable under current controls | The repository has no `package.json` lockfile or Node dependency declaration for `ws`, and the final container entrypoint is `dotnet mmria-server.dll`; a live filesystem check is still required to prove the `ws` package is absent from the runtime image. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `aspnetcore-runtime-9.0` | `CVE-2026-48779` | HIGH | affected | `9.0.16-1.el8_10` |  | Residual risk – required, not reachable under current controls | OSV and NVD describe a `ws` WebSocket fragmentation DoS in Node.js; the repository does not ship a Node service and the final image starts only the .NET app, but a live image check is still required to prove the vulnerable `ws` implementation is not present on disk. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-10732` | HIGH | affected | `10.0.8-1.el8_10` |  | Residual risk – required, not reachable under current controls | NVD describes a Node.js `decompress` zip-slip flaw. The repository contains no Node dependency manifests for `decompress`, and the Dockerfile does not install Node tooling into the app layer, but a live filesystem check is still required to prove the vulnerable package is absent from the runtime image. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-45591` | HIGH | fixed | `10.0.8-1.el8_10` | `10.0.9-1.el8_10` | Fixed | `/source-code/mmria/mmria-server/Dockerfile` updates the final runtime stage packages with `microdnf update`, including `dotnet-host`; rebuild and rescan are still required to verify the new layer contents. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-45736` | HIGH | affected | `10.0.8-1.el8_10` |  | Residual risk – required, not reachable under current controls | The finding maps to the Node.js `ws` package rather than to an application code path in this repository. Repo-static evidence shows no Node dependency manifest and no Node entrypoint, but a live image inspection is still required to prove the `ws` package is absent from the shipped runtime filesystem. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-48779` | HIGH | affected | `10.0.8-1.el8_10` |  | Residual risk – required, not reachable under current controls | OSV describes peer-triggered memory exhaustion in the Node.js `ws` library. The repository has no Node package manifests and the final container starts `dotnet mmria-server.dll`, but a live image inspection is still required to prove the vulnerable `ws` package is not present in the final runtime layer. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-hostfxr-9.0` | `CVE-2026-45591` | HIGH | fixed | `9.0.16-1.el8_10` | `9.0.17-1.el8_10` | Fixed | `/source-code/mmria/mmria-server/Dockerfile` updates the final runtime stage packages with `microdnf update`, including `dotnet-hostfxr-9.0`; rebuild and rescan are still required to verify the new layer contents. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-hostfxr-9.0` | `CVE-2026-45736` | HIGH | affected | `9.0.16-1.el8_10` |  | Residual risk – required, not reachable under current controls | NVD and OSV tie this CVE to `websocket.close()` in the Node.js `ws` package. Repo-static evidence shows no Node dependency declaration and no WebSocket server bootstrap in the shipped app, but a live image inspection is still required to prove the `ws` package is absent from the final runtime filesystem. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-hostfxr-9.0` | `CVE-2026-48779` | HIGH | affected | `9.0.16-1.el8_10` |  | Residual risk – required, not reachable under current controls | This CVE requires a Node.js `ws` peer path. The repository does not define Node package manifests or a Node process entrypoint, and the final image runs only the .NET server, but a live image inspection is still required to prove the `ws` package is not present in the runtime layer. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-runtime-9.0` | `CVE-2026-45591` | HIGH | fixed | `9.0.16-1.el8_10` | `9.0.17-1.el8_10` | Fixed | `/source-code/mmria/mmria-server/Dockerfile` updates the final runtime stage packages with `microdnf update`, including `dotnet-runtime-9.0`; rebuild and rescan are still required to verify the new layer contents. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-runtime-9.0` | `CVE-2026-45736` | HIGH | affected | `9.0.16-1.el8_10` |  | Residual risk – required, not reachable under current controls | The finding maps to the Node.js `ws` package. Repo-static evidence shows no Node dependency manifests, no `UseWebSockets` or `ClientWebSocket` usage, and a .NET-only container entrypoint, but a live image inspection is still required to prove the vulnerable `ws` package is absent from the final runtime filesystem. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-runtime-9.0` | `CVE-2026-48779` | HIGH | affected | `9.0.16-1.el8_10` |  | Residual risk – required, not reachable under current controls | OSV identifies a Node.js `ws` message-fragmentation DoS. The repository is .NET-only at runtime and does not declare Node package manifests, but a live image inspection is still required to prove the vulnerable `ws` package is absent from the shipped runtime filesystem. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `openssl-libs` | `CVE-2026-45447` | HIGH | fixed | `1:1.1.1k-15.el8_6` | `1:1.1.1k-16.el8_6` | Fixed | `/source-code/mmria/mmria-server/Dockerfile` updates the final runtime stage packages with `microdnf update`, including `openssl-libs`; rebuild and rescan are still required to verify the new layer contents. |

## HIGH/CRITICAL release analysis

### aspnetcore-runtime-9.0 / CVE-2026-45736
- Finding: Trivy reports `CVE-2026-45736` against `aspnetcore-runtime-9.0@9.0.16-1.el8_10`.
- Remediation attempted: Updated the runtime layer packages that have published fixes; this CVE has no fixed RHEL package version in the findings payload.
- Why not fixed here: NVD and OSV describe a vulnerability in Node.js `ws` `websocket.close()` handling, not in repository application code.
- Usage/reachability: The repository has no `package.json` manifests and no Node process entrypoint; the final image runs `dotnet mmria-server.dll`.
- Exploit preconditions: A vulnerable `ws` package would need to exist in the final runtime filesystem and be invoked by a Node.js WebSocket path.
- Compensating controls: Repo-static evidence narrows the path to the base runtime contents rather than to application code, but live image inspection is still required to prove package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: If the live check shows no `ws` package in the runtime image, upgrade this finding to Not applicable / false positive; otherwise keep the residual verdict until the base image removes or patches it.

### aspnetcore-runtime-9.0 / CVE-2026-48779
- Finding: Trivy reports `CVE-2026-48779` against `aspnetcore-runtime-9.0@9.0.16-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: OSV and NVD describe a Node.js `ws` fragmentation DoS, and repo-static inspection cannot prove whether the package exists in the vendor base image.
- Usage/reachability: The repository does not ship a Node service and the final image entrypoint starts only the .NET app.
- Exploit preconditions: A vulnerable `ws` package would need to be present and reachable through a running Node.js WebSocket endpoint.
- Compensating controls: The repository does not define a Node runtime entrypoint or dependency manifest for `ws`, but that is insufficient to claim package absence without a live image check.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

### dotnet-host / CVE-2026-10732
- Finding: Trivy reports `CVE-2026-10732` against `dotnet-host@10.0.8-1.el8_10`.
- Remediation attempted: Updated the runtime packages that have published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: NVD describes an archive extraction flaw in the Node.js `decompress` package, not in repository application code.
- Usage/reachability: The repository has no `package.json` manifest and no Dockerfile step that installs Node tooling into the application layer.
- Exploit preconditions: A vulnerable `decompress` package would need to be present in the final runtime filesystem and invoked on attacker-controlled ZIP content.
- Compensating controls: Repo-static evidence limits the suspected path to the vendor runtime image rather than to code in this repository, but live image inspection is still required to prove the package is absent.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls decompress --all 2>/dev/null || true; find / -path \"*node_modules/decompress/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `decompress` is absent from the runtime filesystem.

### dotnet-host / CVE-2026-45736
- Finding: Trivy reports `CVE-2026-45736` against `dotnet-host@10.0.8-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: The vulnerability is in Node.js `ws`, and repo-static inspection cannot prove the vendor runtime image does not contain that package.
- Usage/reachability: The shipped container runs `dotnet mmria-server.dll`, and the repository defines no Node dependency manifest for `ws`.
- Exploit preconditions: A vulnerable `ws` package would need to exist in the final runtime filesystem and be reachable through a running Node.js WebSocket path.
- Compensating controls: The repository does not define a Node runtime entrypoint or Node package manifest, but a live image check is still required to prove package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

### dotnet-host / CVE-2026-48779
- Finding: Trivy reports `CVE-2026-48779` against `dotnet-host@10.0.8-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: OSV describes a Node.js `ws` peer-fragmentation DoS, and repo-static inspection cannot prove the vendor runtime image does not contain that package.
- Usage/reachability: The shipped container starts only the .NET app and the repository does not declare a Node package manifest.
- Exploit preconditions: A vulnerable `ws` package would need to exist in the final runtime filesystem and be reachable through a running Node.js WebSocket endpoint.
- Compensating controls: Repo-static evidence points away from application code, but a live image check is still required before claiming package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

### dotnet-hostfxr-9.0 / CVE-2026-45736
- Finding: Trivy reports `CVE-2026-45736` against `dotnet-hostfxr-9.0@9.0.16-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: NVD and OSV tie the issue to Node.js `ws`, and repo-static inspection cannot prove the vendor runtime image does not contain that package.
- Usage/reachability: The repository has no Node dependency manifest and the final container starts only the .NET server.
- Exploit preconditions: A vulnerable `ws` package would need to be present in the final runtime filesystem and reachable through a Node.js WebSocket path.
- Compensating controls: The repository does not ship a Node entrypoint or declared `ws` dependency, but live image inspection is still required before claiming package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

### dotnet-hostfxr-9.0 / CVE-2026-48779
- Finding: Trivy reports `CVE-2026-48779` against `dotnet-hostfxr-9.0@9.0.16-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: OSV describes a Node.js `ws` fragmentation DoS, and repo-static inspection cannot prove the vendor runtime image does not contain that package.
- Usage/reachability: The repository has no Node dependency manifest and the final container starts only the .NET server.
- Exploit preconditions: A vulnerable `ws` package would need to be present in the final runtime filesystem and reachable through a Node.js WebSocket path.
- Compensating controls: The repository does not ship a Node entrypoint or declared `ws` dependency, but live image inspection is still required before claiming package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

### dotnet-runtime-9.0 / CVE-2026-45736
- Finding: Trivy reports `CVE-2026-45736` against `dotnet-runtime-9.0@9.0.16-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: The vulnerability is in Node.js `ws`, and repo-static inspection cannot prove the vendor runtime image does not contain that package.
- Usage/reachability: Repo search found no `package.json`, `UseWebSockets`, or `ClientWebSocket` usage, and the final image entrypoint starts only the .NET app.
- Exploit preconditions: A vulnerable `ws` package would need to exist in the final runtime filesystem and be reachable through a running Node.js WebSocket path.
- Compensating controls: Repo-static evidence narrows the path to the vendor runtime image rather than to repository code, but a live image check is still required before claiming package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

### dotnet-runtime-9.0 / CVE-2026-48779
- Finding: Trivy reports `CVE-2026-48779` against `dotnet-runtime-9.0@9.0.16-1.el8_10`.
- Remediation attempted: Updated runtime packages with published fixes; this CVE still has no fixed RHEL package version in the findings payload.
- Why not fixed here: OSV describes a Node.js `ws` fragmentation DoS, and repo-static inspection cannot prove the vendor runtime image does not contain that package.
- Usage/reachability: Repo search found no `package.json`, `UseWebSockets`, or `ClientWebSocket` usage, and the final image entrypoint starts only the .NET app.
- Exploit preconditions: A vulnerable `ws` package would need to exist in the final runtime filesystem and be reachable through a running Node.js WebSocket endpoint.
- Compensating controls: Repo-static evidence narrows the path to the vendor runtime image rather than to repository code, but a live image check is still required before claiming package absence.
- Reviewer verification: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path \"*node_modules/ws/package.json\" 2>/dev/null | head'`
- Follow-up: Upgrade to Not applicable / false positive only if live image inspection proves `ws` is absent from the runtime filesystem.

## Verification

**Completed in this session**
- Verified baseline repository health before edits: `dotnet build source-code/mmria/mmria.sln`, `dotnet test --no-build source-code/mmria/mmria.sln`, `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.sln`, and `dotnet test --no-build nccdphp-drh-mmria-services/mmria.services/mmria.services.sln` all passed.
- Verified repo-static reachability evidence for the residual findings: no `package.json` or `package-lock.json` files were found in the repository, and repo search found no `UseWebSockets` or `ClientWebSocket` usage in application code.

**Handoff commands**
- Rebuild the runtime image: `docker build -f /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria`
- Rescan the rebuilt image with Trivy and confirm the fixed findings drop out.
- Validate residual findings directly in a running container:
  - `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'rpm -q aspnetcore-runtime-9.0 dotnet-host dotnet-hostfxr-9.0 dotnet-runtime-9.0 openssl-libs'`
  - `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws decompress --all 2>/dev/null || true'`
  - `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'find / -path \"*node_modules/ws/package.json\" -o -path \"*node_modules/decompress/package.json\" 2>/dev/null | head -20'`

**Limitations**
- I could not rebuild or rescan the OpenShift image from this environment, so fixed findings remain documented as code-level remediations pending rebuild plus Trivy confirmation.

## SWA Exception Justifications

### aspnetcore-runtime-9.0 / CVE-2026-45736

- **CVE:** CVE-2026-45736
- **Package:** aspnetcore-runtime-9.0@9.0.16-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository ships a .NET runtime entrypoint and no Node dependency manifests, so this Node.js `ws` finding is not reachable from repository-defined runtime code without additional vendor-image contents.

NVD and OSV describe CVE-2026-45736 as an uninitialized-memory disclosure in the Node.js `ws` package when `websocket.close()` receives a `TypedArray` reason. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no Node dependency declaration for `ws`, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That limits reachability to vendor-image contents outside repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### aspnetcore-runtime-9.0 / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** aspnetcore-runtime-9.0@9.0.16-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository runs only the .NET server and does not declare Node dependencies, so this Node.js `ws` fragmentation DoS is not reachable from repository-defined runtime code without additional vendor-image contents.

OSV and NVD describe CVE-2026-48779 as a memory-exhaustion DoS in the Node.js `ws` package that requires a vulnerable WebSocket implementation to be present and handling peer traffic. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no declared `ws` dependency, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That narrows the finding to possible vendor-image contents rather than repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### dotnet-host / CVE-2026-10732

- **CVE:** CVE-2026-10732
- **Package:** dotnet-host@10.0.8-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository does not declare or install the Node.js `decompress` package, so this zip-slip finding is not reachable from repository-defined runtime code without additional vendor-image contents.

NVD describes CVE-2026-10732 as an arbitrary-file-write flaw in the Node.js `decompress` package during ZIP extraction. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no Node dependency declaration for `decompress`, and no Dockerfile step that installs Node tooling into the application layer in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That narrows the finding to possible vendor-image contents rather than repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls decompress --all 2>/dev/null || true; find / -path "*node_modules/decompress/package.json" 2>/dev/null | head'`.

### dotnet-host / CVE-2026-45736

- **CVE:** CVE-2026-45736
- **Package:** dotnet-host@10.0.8-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository ships a .NET runtime entrypoint and no Node dependency manifests, so this Node.js `ws` finding is not reachable from repository-defined runtime code without additional vendor-image contents.

NVD and OSV describe CVE-2026-45736 as an uninitialized-memory disclosure in the Node.js `ws` package when `websocket.close()` receives a `TypedArray` reason. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no Node dependency declaration for `ws`, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That limits reachability to vendor-image contents outside repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### dotnet-host / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** dotnet-host@10.0.8-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository runs only the .NET server and does not declare Node dependencies, so this Node.js `ws` fragmentation DoS is not reachable from repository-defined runtime code without additional vendor-image contents.

OSV and NVD describe CVE-2026-48779 as a memory-exhaustion DoS in the Node.js `ws` package that requires a vulnerable WebSocket implementation to be present and handling peer traffic. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no declared `ws` dependency, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That narrows the finding to possible vendor-image contents rather than repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### dotnet-hostfxr-9.0 / CVE-2026-45736

- **CVE:** CVE-2026-45736
- **Package:** dotnet-hostfxr-9.0@9.0.16-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository ships a .NET runtime entrypoint and no Node dependency manifests, so this Node.js `ws` finding is not reachable from repository-defined runtime code without additional vendor-image contents.

NVD and OSV describe CVE-2026-45736 as an uninitialized-memory disclosure in the Node.js `ws` package when `websocket.close()` receives a `TypedArray` reason. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no Node dependency declaration for `ws`, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That limits reachability to vendor-image contents outside repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### dotnet-hostfxr-9.0 / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** dotnet-hostfxr-9.0@9.0.16-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository runs only the .NET server and does not declare Node dependencies, so this Node.js `ws` fragmentation DoS is not reachable from repository-defined runtime code without additional vendor-image contents.

OSV and NVD describe CVE-2026-48779 as a memory-exhaustion DoS in the Node.js `ws` package that requires a vulnerable WebSocket implementation to be present and handling peer traffic. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no declared `ws` dependency, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That narrows the finding to possible vendor-image contents rather than repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### dotnet-runtime-9.0 / CVE-2026-45736

- **CVE:** CVE-2026-45736
- **Package:** dotnet-runtime-9.0@9.0.16-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository ships a .NET runtime entrypoint and no Node dependency manifests, so this Node.js `ws` finding is not reachable from repository-defined runtime code without additional vendor-image contents.

NVD and OSV describe CVE-2026-45736 as an uninitialized-memory disclosure in the Node.js `ws` package when `websocket.close()` receives a `TypedArray` reason. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no Node dependency declaration for `ws`, no `UseWebSockets` or `ClientWebSocket` usage in repository search results, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That limits reachability to vendor-image contents outside repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.

### dotnet-runtime-9.0 / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** dotnet-runtime-9.0@9.0.16-1.el8_10
- **Severity:** HIGH
- **Status:** affected
- **SSC Issue ID:** 
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** The repository runs only the .NET server and does not declare Node dependencies, so this Node.js `ws` fragmentation DoS is not reachable from repository-defined runtime code without additional vendor-image contents.

OSV and NVD describe CVE-2026-48779 as a memory-exhaustion DoS in the Node.js `ws` package that requires a vulnerable WebSocket implementation to be present and handling peer traffic. Repo-static evidence shows no `package.json` or `package-lock.json` files in this repository, no declared `ws` dependency, no `UseWebSockets` or `ClientWebSocket` usage in repository search results, and a final container entrypoint of `dotnet mmria-server.dll` in `/home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria-server/Dockerfile`. That narrows the finding to possible vendor-image contents rather than repository code, but I cannot prove package absence without a live container check. Reviewer verification command: `oc rsh -n <namespace> deployment/<deployment> -- sh -lc 'which node || true; npm ls ws --all 2>/dev/null || true; find / -path "*node_modules/ws/package.json" 2>/dev/null | head'`.
