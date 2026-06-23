# Trivy remediation log

## 2026-06-23 — Scan 30299 — Service 45 — Commit 3141fa8685885293b49e6719df4924d8378f7dfd

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| High | 10 | 5 | 0 | 5 | 0 | 5 |

⏳ EVIDENCE WOULD UPGRADE

- `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | aspnetcore-runtime-9.0 | CVE-2026-45736` — Pod-level file inspection proving no bundled `ws` package or proving the flagged package path is absent would upgrade this from residual risk to not applicable.
- `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-10732` — Pod-level file inspection proving no bundled `decompress` package or proving the flagged package path is absent would upgrade this from residual risk to not applicable.
- `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-45736` — Pod-level file inspection proving no bundled `ws` package or proving the flagged package path is absent would upgrade this from residual risk to not applicable.
- `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-hostfxr-9.0 | CVE-2026-45736` — Pod-level file inspection proving no bundled `ws` package or proving the flagged package path is absent would upgrade this from residual risk to not applicable.
- `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-runtime-9.0 | CVE-2026-45736` — Pod-level file inspection proving no bundled `ws` package or proving the flagged package path is absent would upgrade this from residual risk to not applicable.

### Fixes made

| File | Change | Findings |
| --- | --- | --- |
| `source-code/mmria/mmria-server/Dockerfile` | Added a runtime-stage package-manager upgrade for `aspnetcore-runtime-9.0`, `dotnet-host`, `dotnet-hostfxr-9.0`, `dotnet-runtime-9.0`, and `openssl-libs`, with `microdnf`/`dnf` fallback and cache cleanup, so the final image pulls patched RPMs during image build. | `CVE-2026-45591` on `aspnetcore-runtime-9.0`, `dotnet-host`, `dotnet-hostfxr-9.0`, and `dotnet-runtime-9.0`; `CVE-2026-45447` on `openssl-libs` |

Validation notes:

- `dotnet build /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria.sln` succeeded after the Dockerfile change.
- `dotnet test /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/source-code/mmria/mmria.sln --no-build` completed successfully after the Dockerfile change.
- A full image rebuild/rescan could not be run from this environment because the pinned base image comes from the internal OpenShift registry `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov`, which is not reachable from this sandbox.

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| `aspnetcore-runtime-9.0` | `CVE-2026-45591` | Fixed | The runtime stage now upgrades `aspnetcore-runtime-9.0` during image build before app startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). The finding listed `fixedIn` `9.0.17-1.el8_10`; this change is designed to pull that or a newer patched RPM from the configured Red Hat repositories at build time. |
| `dotnet-host` | `CVE-2026-45591` | Fixed | The runtime stage now upgrades `dotnet-host` during image build before app startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). The finding listed `fixedIn` `10.0.9-1.el8_10`; this change is designed to pull that or a newer patched RPM from the configured Red Hat repositories at build time. |
| `dotnet-hostfxr-9.0` | `CVE-2026-45591` | Fixed | The runtime stage now upgrades `dotnet-hostfxr-9.0` during image build before app startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). The finding listed `fixedIn` `9.0.17-1.el8_10`; this change is designed to pull that or a newer patched RPM from the configured Red Hat repositories at build time. |
| `dotnet-runtime-9.0` | `CVE-2026-45591` | Fixed | The runtime stage now upgrades `dotnet-runtime-9.0` during image build before app startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). The finding listed `fixedIn` `9.0.17-1.el8_10`; this change is designed to pull that or a newer patched RPM from the configured Red Hat repositories at build time. |
| `openssl-libs` | `CVE-2026-45447` | Fixed | The runtime stage now upgrades `openssl-libs` during image build before app startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). NVD describes the issue as a PKCS#7/S/MIME signature-verification use-after-free in OpenSSL; upgrading the RPM in the final image is the direct remediation path. |
| `aspnetcore-runtime-9.0` | `CVE-2026-45736` | Residual risk – required, not reachable under current controls | OSV states this issue requires a Node.js `ws` consumer that calls `websocket.close()` with a `TypedArray` reason. The scanned target starts `dotnet mmria-server.dll`, not a Node process, and the final image only copies published .NET output plus database scripts (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 also found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so there is no application-managed Node/WebSocket dependency to invoke this path. Live pod access is still required to prove the flagged `ws` artifact is absent from the runtime filesystem, so the finding remains residual risk pending that verification. |
| `dotnet-host` | `CVE-2026-10732` | Residual risk – required, not reachable under current controls | NVD says this issue requires the JavaScript `decompress` package to extract a crafted ZIP with a symlink-first, duplicate-path sequence. The scanned target is an ASP.NET Core process launched with `ENTRYPOINT ["dotnet", "mmria-server.dll"]` and run as non-root `USER 1001`, not a Node archive-extraction workflow (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so the application code in this repo does not introduce a reachable `decompress` execution path. Live pod access is still required to prove the flagged `decompress` artifact is absent from the runtime filesystem, so the finding remains residual risk pending that verification. |
| `dotnet-host` | `CVE-2026-45736` | Residual risk – required, not reachable under current controls | OSV states this issue requires a Node.js `ws` consumer that calls `websocket.close()` with a `TypedArray` reason. The scanned target starts `dotnet mmria-server.dll`, not a Node process, and the final image only copies published .NET output plus database scripts (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 also found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so there is no application-managed Node/WebSocket dependency to invoke this path. Live pod access is still required to prove the flagged `ws` artifact is absent from the runtime filesystem, so the finding remains residual risk pending that verification. |
| `dotnet-hostfxr-9.0` | `CVE-2026-45736` | Residual risk – required, not reachable under current controls | OSV states this issue requires a Node.js `ws` consumer that calls `websocket.close()` with a `TypedArray` reason. The scanned target starts `dotnet mmria-server.dll`, not a Node process, and the final image only copies published .NET output plus database scripts (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 also found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so there is no application-managed Node/WebSocket dependency to invoke this path. Live pod access is still required to prove the flagged `ws` artifact is absent from the runtime filesystem, so the finding remains residual risk pending that verification. |
| `dotnet-runtime-9.0` | `CVE-2026-45736` | Residual risk – required, not reachable under current controls | OSV states this issue requires a Node.js `ws` consumer that calls `websocket.close()` with a `TypedArray` reason. The scanned target starts `dotnet mmria-server.dll`, not a Node process, and the final image only copies published .NET output plus database scripts (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 also found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so there is no application-managed Node/WebSocket dependency to invoke this path. Live pod access is still required to prove the flagged `ws` artifact is absent from the runtime filesystem, so the finding remains residual risk pending that verification. |

### Verification handoff

Run the following against a rebuilt image or running pod for `mmria-s2i`:

```bash
oc rsh <mmria-pod> rpm -q aspnetcore-runtime-9.0 dotnet-host dotnet-hostfxr-9.0 dotnet-runtime-9.0 openssl-libs
oc rsh <mmria-pod> find /usr/share/dotnet -path '*node_modules/ws*' -o -path '*node_modules/decompress*'
oc rsh <mmria-pod> sh -lc 'command -v node || true'
trivy image default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest
```

### Pod verification output

Pending developer-run verification.

## SWA Exception Justifications

### `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | aspnetcore-runtime-9.0 | CVE-2026-45736`

Verdict: `Residual risk – required, not reachable under current controls`

OSV describes `CVE-2026-45736` as an uninitialized-memory disclosure in the Node.js `ws` library when `websocket.close()` is called with a `TypedArray` reason. In this repository, the scanned target is built as an ASP.NET Core container whose final image starts `dotnet mmria-server.dll`, copies only the published .NET output and database scripts into the runtime stage, and drops back to `USER 1001` before startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so the application code here does not define a Node-managed `ws` dependency or a Node entrypoint that could drive the vulnerable `websocket.close()` path. Because I do not have pod access to prove the flagged `ws` files are absent from the final runtime filesystem, the finding remains Residual risk – required, not reachable under current controls until the live checks below are run.

Verification command:

```bash
oc rsh <mmria-pod> find /usr/share/dotnet -path '*node_modules/ws*'
```

### `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-10732`

Verdict: `Residual risk – required, not reachable under current controls`

NVD describes `CVE-2026-10732` as an arbitrary-file-write condition in the JavaScript `decompress` package that requires processing a crafted ZIP archive with a symlink-first duplicate path sequence. In this repository, the scanned target is built as an ASP.NET Core container whose final image starts `dotnet mmria-server.dll`, copies only the published .NET output and database scripts into the runtime stage, and runs as `USER 1001` before startup (`source-code/mmria/mmria-server/Dockerfile:24-56`). Repo-static verification on 2026-06-23 found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so the application code here does not define a Node-managed `decompress` dependency or a Node archive-extraction workflow that could reach this path. Because I do not have pod access to prove the flagged `decompress` files are absent from the final runtime filesystem, the finding remains Residual risk – required, not reachable under current controls until the live checks below are run.

Verification command:

```bash
oc rsh <mmria-pod> find /usr/share/dotnet -path '*node_modules/decompress*'
```

### `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-host | CVE-2026-45736`

Verdict: `Residual risk – required, not reachable under current controls`

OSV describes `CVE-2026-45736` as an uninitialized-memory disclosure in the Node.js `ws` library when `websocket.close()` is called with a `TypedArray` reason. In this repository, the scanned target is built as an ASP.NET Core container whose final image starts `dotnet mmria-server.dll`, copies only the published .NET output and database scripts into the runtime stage, and drops back to `USER 1001` before startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so the application code here does not define a Node-managed `ws` dependency or a Node entrypoint that could drive the vulnerable `websocket.close()` path. Because I do not have pod access to prove the flagged `ws` files are absent from the final runtime filesystem, the finding remains Residual risk – required, not reachable under current controls until the live checks below are run.

Verification command:

```bash
oc rsh <mmria-pod> find /usr/share/dotnet -path '*node_modules/ws*'
```

### `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-hostfxr-9.0 | CVE-2026-45736`

Verdict: `Residual risk – required, not reachable under current controls`

OSV describes `CVE-2026-45736` as an uninitialized-memory disclosure in the Node.js `ws` library when `websocket.close()` is called with a `TypedArray` reason. In this repository, the scanned target is built as an ASP.NET Core container whose final image starts `dotnet mmria-server.dll`, copies only the published .NET output and database scripts into the runtime stage, and drops back to `USER 1001` before startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so the application code here does not define a Node-managed `ws` dependency or a Node entrypoint that could drive the vulnerable `websocket.close()` path. Because I do not have pod access to prove the flagged `ws` files are absent from the final runtime filesystem, the finding remains Residual risk – required, not reachable under current controls until the live checks below are run.

Verification command:

```bash
oc rsh <mmria-pod> find /usr/share/dotnet -path '*node_modules/ws*'
```

### `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 8.10) | dotnet-runtime-9.0 | CVE-2026-45736`

Verdict: `Residual risk – required, not reachable under current controls`

OSV describes `CVE-2026-45736` as an uninitialized-memory disclosure in the Node.js `ws` library when `websocket.close()` is called with a `TypedArray` reason. In this repository, the scanned target is built as an ASP.NET Core container whose final image starts `dotnet mmria-server.dll`, copies only the published .NET output and database scripts into the runtime stage, and drops back to `USER 1001` before startup (`source-code/mmria/mmria-server/Dockerfile:27-56`). Repo-static verification on 2026-06-23 found no `package.json`, `package-lock.json`, or `node_modules` entries under the repository root, so the application code here does not define a Node-managed `ws` dependency or a Node entrypoint that could drive the vulnerable `websocket.close()` path. Because I do not have pod access to prove the flagged `ws` files are absent from the final runtime filesystem, the finding remains Residual risk – required, not reachable under current controls until the live checks below are run.

Verification command:

```bash
oc rsh <mmria-pod> find /usr/share/dotnet -path '*node_modules/ws*'
```
