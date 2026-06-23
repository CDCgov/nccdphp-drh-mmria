# Trivy Remediation Log

## Scan 30296 (service 45, commit 994338851d890a51f4cef68decc176638e7095dc)

### Triage inventory (HIGH/CRITICAL)

| Target | Package | CVE | Installed | Fixed In | Verdict | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `aspnetcore-runtime-9.0` | `CVE-2026-45591` | `9.0.16-1.el8_10` | `9.0.17-1.el8_10` | Pending — base image update | `source-code/mmria/mmria-server/Dockerfile` now pins newer `dotnet-90` and `dotnet-90-runtime` base digests to pull updated runtime RPMs during image rebuild. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-45591` | `10.0.8-1.el8_10` | `10.0.9-1.el8_10` | Pending — base image update | `source-code/mmria/mmria-server/Dockerfile` base image tags/digests were updated to newer Red Hat .NET 9 images so the host/runtime packages are replaced on rebuild. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-hostfxr-9.0` | `CVE-2026-45591` | `9.0.16-1.el8_10` | `9.0.17-1.el8_10` | Pending — base image update | `source-code/mmria/mmria-server/Dockerfile` now uses newer pinned runtime image digest, which is the remediation path for OS/base-image package findings. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-runtime-9.0` | `CVE-2026-45591` | `9.0.16-1.el8_10` | `9.0.17-1.el8_10` | Pending — base image update | Base image pin updated in `source-code/mmria/mmria-server/Dockerfile`; this finding is expected to clear after rebuild/rescan against the new image digest. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `openssl-libs` | `CVE-2026-45447` | `1:1.1.1k-15.el8_6` | `1:1.1.1k-16.el8_6` | Pending — base image update | Runtime base image digest update in `source-code/mmria/mmria-server/Dockerfile` is the direct remediation path for this OS package CVE. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `aspnetcore-runtime-9.0` | `CVE-2026-45736` | `9.0.16-1.el8_10` | *(none)* | Not applicable / false positive | OSV identifies `CVE-2026-45736` as the Node.js `ws` library vulnerability (`ws.close` typed-array reason handling), but this repo has no npm/yarn manifests (`**/package.json`, `**/package-lock.json`, `**/yarn.lock` all absent) and this image is built from .NET RPM/runtime layers. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-45736` | `10.0.8-1.el8_10` | *(none)* | Not applicable / false positive | `CVE-2026-45736` is a `ws` npm package issue (OSV advisory), not a `dotnet-host` package vulnerability; repository dependency evidence and Docker build path show only .NET restore/publish steps with no Node package install path. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-hostfxr-9.0` | `CVE-2026-45736` | `9.0.16-1.el8_10` | *(none)* | Not applicable / false positive | NVD/OSV scope this CVE to the JavaScript `ws` library behavior, while `dotnet-hostfxr-9.0` is a .NET host resolver RPM; no Node dependency lockfile exists in this repository to satisfy the vulnerable precondition. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-runtime-9.0` | `CVE-2026-45736` | `9.0.16-1.el8_10` | *(none)* | Not applicable / false positive | The CVE description requires vulnerable `ws` code path usage, but this image build (`source-code/mmria/mmria-server/Dockerfile`) performs `dotnet restore/build/publish` only and does not install or execute Node modules. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10)` | `dotnet-host` | `CVE-2026-10732` | `10.0.8-1.el8_10` | *(none)* | Not applicable / false positive | NVD scopes `CVE-2026-10732` to the npm package `decompress` Zip Slip path; `dotnet-host` is not that component and this repository has no npm dependency manifests to include `decompress` in the produced runtime image. |

### Verification commands (live checks for platform team)

```bash
# Rebuild service 45 image from updated Dockerfile and confirm resulting image reference
oc -n mmria start-build mmria-s2i --follow
oc -n mmria get istag mmria-s2i:latest -o jsonpath='{.image.dockerImageReference}{"\n"}'

# Verify installed runtime packages in the rebuilt image/pod
oc -n mmria rsh deploy/mmria-s2i rpm -qa | egrep 'aspnetcore-runtime-9.0|dotnet-host|dotnet-hostfxr-9.0|dotnet-runtime-9.0|openssl-libs'

# Validate Node package preconditions for CVE-2026-45736 / CVE-2026-10732 are absent
oc -n mmria rsh deploy/mmria-s2i sh -c 'command -v node || true; command -v npm || true'
oc -n mmria rsh deploy/mmria-s2i sh -c 'find /app -maxdepth 6 -type d \\( -name ws -o -name decompress \\) 2>/dev/null | head'

# Rescan rebuilt image
trivy image --severity HIGH,CRITICAL --ignore-unfixed=false "$(oc -n mmria get istag mmria-s2i:latest -o jsonpath='{.image.dockerImageReference}')"
```

## SWA Exception Justifications

### CVE-2026-45736
Verdict: Not applicable / false positive

OSV records CVE-2026-45736 as a vulnerability in the Node.js `ws` package (`websocket.close()` with a `TypedArray` reason). This repository does not contain npm/yarn dependency manifests (`package.json`, `package-lock.json`, `yarn.lock` are absent in repository-wide search), and the affected image build path in `source-code/mmria/mmria-server/Dockerfile` uses only `dotnet restore/build/publish` stages. The finding is therefore a package/CPE mismatch against .NET RPMs (`aspnetcore-runtime-9.0`, `dotnet-host`, `dotnet-hostfxr-9.0`, `dotnet-runtime-9.0`) rather than evidence of vulnerable `ws` package presence in the shipped runtime.

### CVE-2026-10732
Verdict: Not applicable / false positive

NVD describes CVE-2026-10732 as a Zip Slip vulnerability in the npm package `decompress` during crafted archive extraction. The Trivy finding maps this CVE to `dotnet-host`, but `dotnet-host` is a .NET host RPM and not the `decompress` JavaScript package identified by NVD. Repository-wide dependency evidence shows no npm lock/manifests in this codebase, and the container build flow for service 45 is .NET-only, so the vulnerable npm package precondition is not met in the produced artifact.
