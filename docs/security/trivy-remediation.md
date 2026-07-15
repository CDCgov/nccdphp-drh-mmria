# Trivy remediation log

## Scan: 30746 | Service: MMRIA S2I | Commit: 1c266b5ed2adae4ca2f6a47f80879fea42b710d6

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| High | 24 | 0 | 14 | 10 | 0 | 24 |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |

⏳ EVIDENCE WOULD UPGRADE
- `aspnetcore-runtime-9.0` (`CVE-2026-45736`, `CVE-2026-48779`): `oc rsh` proof that no Node.js runtime / ws package path exists in the image would support Not applicable.
- `dotnet-host` (`CVE-2026-10732`, `CVE-2026-12143`, `CVE-2026-45736`, `CVE-2026-48779`): `oc rsh` package-file and runtime checks proving vulnerable npm library paths are absent would support Not applicable.
- `dotnet-hostfxr-9.0` and `dotnet-runtime-9.0` (`CVE-2026-45736`, `CVE-2026-48779`): `oc rsh` proof that Node/websocket npm surface is absent would support Not applicable.

### HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
|---|---|---|---|
| aspnetcore-runtime-9.0 | CVE-2026-45736 | Residual risk – required, not reachable under current controls | Trivy maps this to Node.js `ws`; repository has no `package.json`, and runtime image is non-root (`USER 1001`). |
| aspnetcore-runtime-9.0 | CVE-2026-48779 | Residual risk – required, not reachable under current controls | Trivy maps this to Node.js `ws`; repository has no `package.json`, and runtime image is non-root (`USER 1001`). |
| curl | CVE-2026-11352 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| curl | CVE-2026-11586 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| curl | CVE-2026-12064 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| curl | CVE-2026-8286 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| curl | CVE-2026-8925 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| curl | CVE-2026-9547 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| dotnet-host | CVE-2026-10732 | Residual risk – required, not reachable under current controls | Trivy description references npm `decompress`; repository has no npm manifest and container runs non-root. |
| dotnet-host | CVE-2026-12143 | Residual risk – required, not reachable under current controls | Trivy description references npm `form-data`; repository has no npm manifest and container runs non-root. |
| dotnet-host | CVE-2026-45736 | Residual risk – required, not reachable under current controls | Trivy description references npm `ws`; repository has no npm manifest and container runs non-root. |
| dotnet-host | CVE-2026-48779 | Residual risk – required, not reachable under current controls | Trivy description references npm `ws`; repository has no npm manifest and container runs non-root. |
| dotnet-hostfxr-9.0 | CVE-2026-45736 | Residual risk – required, not reachable under current controls | Trivy description references npm `ws`; repository has no npm manifest and container runs non-root. |
| dotnet-hostfxr-9.0 | CVE-2026-48779 | Residual risk – required, not reachable under current controls | Trivy description references npm `ws`; repository has no npm manifest and container runs non-root. |
| dotnet-runtime-9.0 | CVE-2026-45736 | Residual risk – required, not reachable under current controls | Trivy description references npm `ws`; repository has no npm manifest and container runs non-root. |
| dotnet-runtime-9.0 | CVE-2026-48779 | Residual risk – required, not reachable under current controls | Trivy description references npm `ws`; repository has no npm manifest and container runs non-root. |
| glib2 | CVE-2026-58016 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libacl | CVE-2026-54369 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libcurl | CVE-2026-11352 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libcurl | CVE-2026-11586 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libcurl | CVE-2026-12064 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libcurl | CVE-2026-8286 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libcurl | CVE-2026-8925 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |
| libcurl | CVE-2026-9547 | Pending — base image update | OS RPM from Red Hat 8.10 base image; no package-level fix applied in-repo for this scan. |

### Verification commands (handoff)

```bash
# Verify image package inventory and versions
oc -n mmria rsh deploy/mmria-s2i rpm -qa | egrep 'aspnetcore-runtime|dotnet-host|dotnet-hostfxr|dotnet-runtime|curl|libcurl|glib2|libacl'

# Verify Node runtime and npm package path absence
oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; find / -maxdepth 5 -type d -name node_modules 2>/dev/null | head'

# Re-scan after image refresh
trivy image default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest
```

## SWA Exception Justifications

### aspnetcore-runtime-9.0 / CVE-2026-45736
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The image package remains present through the base image, but exploit preconditions depend on a vulnerable Node.js websocket flow. Repository search found no `package.json`, and the runtime stage explicitly runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql aspnetcore-runtime-9.0 | head'`

### aspnetcore-runtime-9.0 / CVE-2026-48779
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The image package remains present through the base image, but exploit preconditions depend on a vulnerable Node.js websocket flow. Repository search found no `package.json`, and the runtime stage explicitly runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql aspnetcore-runtime-9.0 | head'`

### dotnet-host / CVE-2026-10732
**Summary:** Trivy maps this finding to npm package `decompress`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable archive handling in npm `decompress`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-host | head'`

### dotnet-host / CVE-2026-12143
**Summary:** Trivy maps this finding to npm package `form-data`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable multipart handling in npm `form-data`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-host | head'`

### dotnet-host / CVE-2026-45736
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable websocket handling in npm `ws`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-host | head'`

### dotnet-host / CVE-2026-48779
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable websocket handling in npm `ws`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-host | head'`

### dotnet-hostfxr-9.0 / CVE-2026-45736
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable websocket handling in npm `ws`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-hostfxr-9.0 | head'`

### dotnet-hostfxr-9.0 / CVE-2026-48779
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable websocket handling in npm `ws`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-hostfxr-9.0 | head'`

### dotnet-runtime-9.0 / CVE-2026-45736
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable websocket handling in npm `ws`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-runtime-9.0 | head'`

### dotnet-runtime-9.0 / CVE-2026-48779
**Summary:** Trivy maps this finding to npm package `ws`; repository evidence shows no Node.js/npm application surface and runtime is non-root.
**Verdict:** Residual risk – required, not reachable under current controls
**Justification:** The package is supplied by the base image and no in-repo package replacement is available in this pass. The exploit path requires vulnerable websocket handling in npm `ws`; repository search found no `package.json`, and runtime runs as UID 1001 (`source-code/mmria/mmria-server/Dockerfile:48`).
**Verification:** `oc -n mmria rsh deploy/mmria-s2i sh -lc 'command -v node || true; rpm -ql dotnet-runtime-9.0 | head'`
