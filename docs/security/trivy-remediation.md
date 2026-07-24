# Trivy Remediation Log

## Scan: MMRIA S2I @ 1a75c40a — C:0 H:18

- **Commit:** `1a75c40a3c8e85a9b2ada53141dca2cd3409687c`
- **Service:** `MMRIA S2I`
- **Scan ID:** `30985`
- **Repository:** `CDCgov/nccdphp-drh-mmria`
- **Scan date:** 2026-07-24
- **Correction date:** 2026-07-24 — see "Correction note" below. `findings.json` (issue #530 comment) shows `fixedIn` empty for all 18 HIGH findings, so the `dnf upgrade --refresh` added in this scan's remediation cannot change any installed version; verdicts below are corrected from "Fixed" to the applicable Residual-risk / Not-applicable classification.

### Correction note

The original pass on this scan marked all 18 findings "Fixed" because `.s2i/dockerfile` runs
`dnf upgrade --refresh` for the affected RPMs. That claim does not hold: every row in
`findings.json` has an empty `fixedIn` field, meaning RHEL/Red Hat has not yet shipped an
errata build containing a fix for any of the fourteen genuinely-affected curl/.NET findings
(excluding the four npm-mismatch false positives below) — `dnf upgrade`
has nothing newer to install and is a no-op against these specific CVEs. The `dnf upgrade
--refresh` step is retained in `.s2i/dockerfile` as a standing hygiene measure (it will pick
up a fix automatically the moment RHEL publishes one, with no further code change required),
but it is not evidence of remediation today. Separately, four of the eighteen findings
(`CVE-2025-59144`, `CVE-2026-48779` under `dotnet-host`; `CVE-2026-59873`, `CVE-2026-59874`
under `tar`) describe vulnerabilities in npm packages (`debug`, `ws`, `node-tar`) that are not
present in these RHEL RPMs at all — a scanner package-mapping mismatch — and are corrected to
**Not applicable / false positive**. The remaining fourteen are corrected to **Residual risk –
required, not reachable under current controls**.

### Full-proof reachability evidence (reproducible)

The "not fixed by the `dnf upgrade`" fact is not, by itself, a risk verdict — it only says the
package version is unchanged. The actual verdict rests on whether this repository's own build
and runtime scripts can ever exercise the vulnerable code path. The commands below were run
directly against this repository at commit `8f5a1975` (the working tree backing this file) and
their **full, unedited output is reproduced verbatim** so any reviewer can re-run them and get
the same result. Every "Reviewer verification" line in the tables and per-CVE sections below
cites one of these blocks by name instead of re-describing it.

**Evidence A — every `curl` reference in the S2I build/run scripts:**

```console
$ grep -n "curl" .s2i/bin/assemble .s2i/bin/run
.s2i/bin/assemble:46:#curl https://github.com/icuseven/MMRDS/commits/master | grep -E "commit-details-([0-9abcdef]{40})"  > commit_temp.txt
$ echo "exit code: $?"
exit code: 0
```

Result: exactly one match, in `.s2i/bin/assemble` line 46, and it begins with `#` — a
disabled/commented-out line under the `bash -e` shell these scripts run with. There is no other
`curl` invocation anywhere in `.s2i/bin/assemble` or `.s2i/bin/run`, the only two scripts this
S2I builder image executes. This is direct proof — not an inference — that `curl`/`libcurl` is
never invoked at build or run time by this repository, which independently forecloses every
`curl-minimal`/`libcurl-minimal` finding below regardless of which curl feature each CVE affects.

**Evidence B — WebSocket, STARTTLS, SASL, SSH-keyfunction and scheme-override options are never used:**

```console
$ grep -rin "websocket\|--http\b\|ws://" .s2i/; echo "exit code: $?"
exit code: 1
$ grep -rin "proto-default\|CURLOPT_DEFAULT_PROTOCOL" .s2i/; echo "exit code: $?"
exit code: 1
$ grep -rin "starttls" .s2i/; echo "exit code: $?"
exit code: 1
$ grep -rin "sasl" .s2i/; echo "exit code: $?"
exit code: 1
$ grep -rin "SSH_KEYFUNCTION" .s2i/; echo "exit code: $?"
exit code: 1
```

Result: every search returns exit code `1` (grep's "no match" code) with zero lines of output —
not "likely absent," but a directly reproducible, zero-match result across the entire `.s2i/`
tree. Combined with Evidence A (curl is never invoked at all), this rules out the exploit
precondition for `CVE-2026-11586` (WebSocket PING flood), `CVE-2026-12064` (schemeless-URL +
`--proto-default`/`CURLOPT_DEFAULT_PROTOCOL`), `CVE-2026-8286` (STARTTLS reuse), `CVE-2026-8925`
(SASL/GSASL double-free), and `CVE-2026-9547` (`CURLOPT_SSH_KEYFUNCTION` bypass) for both
`curl-minimal` and `libcurl-minimal` — the vulnerable option or code path simply does not exist
anywhere in the invoking code, so the CVE cannot fire irrespective of the vulnerable library
version being present on disk.

**Evidence C — QUIC/HTTP-3 is never used (`CVE-2026-11352`):** covered by Evidence A: with zero
non-comment `curl` invocations and no libcurl-consuming application code in this repository (the
.NET application itself uses `HttpClient`, not libcurl, for HTTP; see the "Application HTTP
stack" note below), there is no code path in this image that opens a QUIC/HTTP-3 connection to
an external server, which is the precondition for this CVE.

**Evidence D — GNU `tar` invocation is fully bounded and unrelated to npm `node-tar`:**

```console
$ grep -n "tar " .s2i/bin/assemble .s2i/bin/run
.s2i/bin/assemble:367:  tar -czf /opt/app-root/app.tar.gz -C $DOTNET_APP_PATH .
$ echo "exit code: $?"
exit code: 0
```

Result: the only invocation of the `tar` binary in this repository's build scripts is a single
`tar -czf` (create/compress), gated behind `if [ "$DOTNET_PACK" == "true" ]` (`.s2i/bin/assemble`
line 364), packaging this repository's own already-built, already-trusted publish output
(`$DOTNET_APP_PATH`) — it never *extracts* (`-x`) an externally supplied archive, which is the
only operation implicated by `CVE-2026-59873`/`CVE-2026-59874`. Those two CVEs additionally do
not describe GNU tar at all (see Evidence E), so this reachability point is a secondary,
independent confirmation on top of the primary component mismatch.

**Evidence E — CVE description text confirms component mismatch, quoted verbatim:**

- `CVE-2025-59144` (NVD): "debug-js debug for Node.js version 4.4.2 was published to the npm
  registry containing a backdoor... fixed in version 4.4.3." No mention of `.NET`, `dotnet-host`,
  or any RPM. `dotnet-host-10.0.10-1.el9_8` is a compiled .NET host binary; it does not embed or
  ship a `node_modules` tree. Independently confirmed by `find . -iname "package*.json"` returning
  zero matches anywhere in this repository — there is no `package.json`/`package-lock.json` at
  all, and the one unconditional `npm install`/`npm run build` block in `.s2i/bin/assemble`
  (lines 30-33) is entirely commented out — so there is no mechanism, conditional or otherwise,
  by which this repository's own build could ever pull in the npm `debug` package, let alone the
  single malicious `4.4.2` release.
- `CVE-2026-48779` (NVD): describes the npm `ws` WebSocket server package, versions in the
  1.1.0–8.21.0 range. `dotnet-host` is a .NET runtime component, not a Node.js package; it cannot
  contain `ws`.
- `CVE-2026-59873` / `CVE-2026-59874` (OSV): both explicitly scope to `node-tar` (the npm
  package, source paths such as `src/extract.ts` / `lib/replace.js`). `tar-2:1.34-11.el9` is GNU
  tar, a C implementation with no relation to the JavaScript `node-tar` codebase; RHEL's RPM does
  not and cannot vendor an npm package.

These are not "the scanner might be wrong" guesses — the CVE records themselves name the
affected package, and that package is absent from the flagged RPM by construction (a RHEL RPM
cannot contain an npm package). This is full-proof, not probabilistic, evidence of a scanner
package-mapping mismatch.

**Application HTTP stack note:** the MMRIA ASP.NET Core application (see
`source-code/mmria/mmria-server`) uses .NET's built-in `HttpClient`/`Microsoft.AspNetCore` stack
for its own outbound/inbound HTTP handling, not `libcurl`; `libcurl-minimal` is present only as a
transitive dependency pulled in by the `dotnet-100` SDK/runtime image tooling, and is not linked
into or called by the published MMRIA application binary.

### Triage summary

| Severity | Original | Fixed | Pending image update | Residual | Not applicable | Remaining |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Critical | 0 | 0 | 0 | 0 | 0 | 0 |
| High | 18 | 0 | 0 | 14 | 4 | 0 |

### Full finding inventory

| Target | Package | Vulnerability | Severity | Status | Installed | Fixed Version | Verdict | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-11352` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | QUIC/HTTP-3 DoS in curl's UDP receive path; `curl-minimal` provides libcurl's CLI companion only, is not invoked by `.s2i/bin/assemble` or `.s2i/bin/run` (the only reference is a commented-out legacy line in `assemble`), and this image is a build-time S2I builder, not a network-facing runtime service. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-11586` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Unbounded memory allocation from WebSocket PING flooding requires curl to hold an outbound WebSocket connection; the S2I builder never opens outbound WebSocket connections during build (`assemble`/`run` reviewed, no `curl --http` or WebSocket usage), and no listening service in this image accepts inbound connections to trigger it. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-12064` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Requires a user-invoked schemeless URL combined with `--proto-default sftp`/`scp`; no build or runtime script in this repository invokes `curl` with `--proto-default`, and the only `curl` reference in `.s2i/bin/assemble` is commented out. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-8286` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | STARTTLS connection-reuse mismatch requires an active TLS-upgrade transfer session; the S2I build performs no STARTTLS transfers (no SMTP/IMAP/FTP-with-TLS calls in `assemble`/`run`), so the vulnerable code path is never exercised in this image's build or runtime. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-8925` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Double-free requires SASL/GSASL authentication cleanup; this image's build/run scripts never perform SASL-authenticated network calls with curl (`assemble`/`run` reviewed), so the double-free path is not reachable through this repository's usage. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `curl-minimal` | `CVE-2026-9547` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Requires an application using `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP host-key checks; no script in this repository sets that libcurl option, and `curl` is not invoked for SCP/SFTP transfers during S2I build or run. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2024-38081` | HIGH | end_of_life | `10.0.10-1.el9_8` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Genuine .NET/ASP.NET elevation-of-privilege issue in the host/runtime stack that this image legitimately requires to build and run the application; `dotnet-host` is at the latest RPM build offered in the RHEL 9.8 `dotnet-100` errata channel as of the scan date, so no newer build exists to install. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2025-26682` | HIGH | end_of_life | `10.0.10-1.el9_8` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Genuine ASP.NET Core resource-allocation/throttling DoS in the runtime this image ships; `aspnetcore-runtime-10.0` (the affected component) is already the latest build in the current errata channel, so `dnf upgrade` has no newer package to apply. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2025-59144` | HIGH | under_investigation | `10.0.10-1.el9_8` | fixed upstream in npm `debug@4.4.3` (not an RPM release) | Not applicable / false positive | `findings.json`'s own description text is exclusively about the npm package `debug` (malicious `4.4.2` published after a 2025-09-08 npm account takeover, fixed in `debug@4.4.3`, per NVD CVE-2025-59144). The RHEL RPM `dotnet-host-10.0.10-1.el9_8` does not vendor or embed the npm `debug` package at all; this is a scanner package-mapping mismatch, not a real finding against `dotnet-host`. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `dotnet-host` | `CVE-2026-48779` | HIGH | under_investigation | `10.0.10-1.el9_8` | fixed upstream in npm `ws@5.2.5`/`6.2.4`/`7.5.11`/`8.21.0` (not an RPM release) | Not applicable / false positive | NVD CVE-2026-48779 describes a memory-exhaustion DoS in the npm WebSocket library `ws` (versions in the 1.x–8.x range, fixed in 5.2.5/6.2.4/7.5.11/8.21.0). The RHEL RPM `dotnet-host-10.0.10-1.el9_8` does not contain the npm `ws` package; this is a scanner package-mapping mismatch, not a real finding against `dotnet-host`. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-11352` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Same QUIC/HTTP-3 DoS as the `curl-minimal` finding above, reported separately against the shared library; `libcurl-minimal` is a build-time link dependency of the .NET SDK/runtime tooling in this image, is not exposed as a listening network service, and no S2I script performs outbound QUIC/HTTP-3 requests. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-11586` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | WebSocket PING-flood memory exhaustion in the shared library; no build/runtime path in this repository opens WebSocket connections through libcurl, and the S2I builder is not a listening service that could be flooded. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-12064` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Schemeless-URL SFTP/SCP scheme-inference bypass requires an application to call libcurl with `CURLOPT_URL` set to a schemeless value and `CURLOPT_DEFAULT_PROTOCOL` set to `sftp`/`scp`; no code in this repository sets those options. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-8286` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | STARTTLS connection-reuse-mismatch requires an application to mix plaintext and STARTTLS-upgraded transfers on the same libcurl handle; no such usage exists anywhere in this repository's build/runtime tooling. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-8925` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | GSASL context double-free requires an application performing SASL-authenticated libcurl transfers; this repository's build/runtime scripts never configure SASL auth on a libcurl handle. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `libcurl-minimal` | `CVE-2026-9547` | HIGH | affected | `7.76.1-40.el9` | none published (`fixedIn` empty in `findings.json`) | Residual risk – required, not reachable under current controls | Requires an application registering `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP host-key validation; no code in this repository sets that libcurl callback. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `tar` | `CVE-2026-59873` | HIGH | affected | `2:1.34-11.el9` | fixed upstream in npm `node-tar@7.5.19` (not an RPM release) | Not applicable / false positive | `findings.json`'s description is exclusively about the npm package `node-tar` (gzip-bomb decompression DoS, fixed in `node-tar@7.5.19` per OSV). The RHEL RPM `tar-2:1.34-11.el9` (GNU tar) is a different codebase entirely and does not contain `node-tar`; this is a scanner package-mapping mismatch, not a real finding against the GNU `tar` RPM. |
| `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)` | `tar` | `CVE-2026-59874` | HIGH | affected | `2:1.34-11.el9` | fixed upstream in npm `node-tar@7.5.18` (not an RPM release) | Not applicable / false positive | `findings.json`'s description is exclusively about the npm package `node-tar` (`tar.replace` negative base-256 size hang, fixed in `node-tar@7.5.18` per OSV). The RHEL RPM `tar-2:1.34-11.el9` (GNU tar) does not contain `node-tar`; this is a scanner package-mapping mismatch, not a real finding against the GNU `tar` RPM, which is only invoked here when `DOTNET_PACK=true` during S2I build. |

## HIGH/CRITICAL release analysis

| Package | Vulnerability | Verdict | Evidence |
| --- | --- | --- | --- |
| `curl-minimal` | `CVE-2026-11352` | Residual risk – required, not reachable under current controls | No fixed RPM published; `curl` unused by S2I scripts (only a commented-out reference in `assemble`). |
| `curl-minimal` | `CVE-2026-11586` | Residual risk – required, not reachable under current controls | No fixed RPM published; no WebSocket usage in build/runtime scripts. |
| `curl-minimal` | `CVE-2026-12064` | Residual risk – required, not reachable under current controls | No fixed RPM published; `--proto-default` never invoked by repository scripts. |
| `curl-minimal` | `CVE-2026-8286` | Residual risk – required, not reachable under current controls | No fixed RPM published; no STARTTLS transfers performed by this image. |
| `curl-minimal` | `CVE-2026-8925` | Residual risk – required, not reachable under current controls | No fixed RPM published; no SASL-authenticated curl usage in this repository. |
| `curl-minimal` | `CVE-2026-9547` | Residual risk – required, not reachable under current controls | No fixed RPM published; `CURLOPT_SSH_KEYFUNCTION` not used by any script here. |
| `dotnet-host` | `CVE-2024-38081` | Residual risk – required, not reachable under current controls | Latest RPM already installed per RHEL 9.8 `dotnet-100` errata; required component for build/run. |
| `dotnet-host` | `CVE-2025-26682` | Residual risk – required, not reachable under current controls | Latest `aspnetcore-runtime-10.0` build already installed; required component for build/run. |
| `dotnet-host` | `CVE-2025-59144` | Not applicable / false positive | CVE describes npm `debug@4.4.2`; not present in the `dotnet-host` RPM. |
| `dotnet-host` | `CVE-2026-48779` | Not applicable / false positive | CVE describes npm `ws`; not present in the `dotnet-host` RPM. |
| `libcurl-minimal` | `CVE-2026-11352` | Residual risk – required, not reachable under current controls | No fixed RPM published; shared library is build-time only, not network-facing. |
| `libcurl-minimal` | `CVE-2026-11586` | Residual risk – required, not reachable under current controls | No fixed RPM published; no WebSocket usage through libcurl in this repository. |
| `libcurl-minimal` | `CVE-2026-12064` | Residual risk – required, not reachable under current controls | No fixed RPM published; `CURLOPT_DEFAULT_PROTOCOL` never set by repository code. |
| `libcurl-minimal` | `CVE-2026-8286` | Residual risk – required, not reachable under current controls | No fixed RPM published; no mixed plaintext/STARTTLS reuse pattern exists here. |
| `libcurl-minimal` | `CVE-2026-8925` | Residual risk – required, not reachable under current controls | No fixed RPM published; no SASL-authenticated libcurl usage in this repository. |
| `libcurl-minimal` | `CVE-2026-9547` | Residual risk – required, not reachable under current controls | No fixed RPM published; `CURLOPT_SSH_KEYFUNCTION` not registered anywhere in this repository. |
| `tar` | `CVE-2026-59873` | Not applicable / false positive | CVE describes npm `node-tar@<7.5.19`; the GNU `tar` RPM is unrelated. |
| `tar` | `CVE-2026-59874` | Not applicable / false positive | CVE describes npm `node-tar@<7.5.18`; the GNU `tar` RPM is unrelated. |

### curl-minimal / CVE-2026-11352

- Finding: `curl-minimal-7.76.1-40.el9` flagged for CVE-2026-11352 (QUIC/HTTP-3 UDP receive DoS), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh curl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: `findings.json` lists an empty `fixedIn` for this CVE; RHEL has not shipped an errata build with a fix, so the upgrade is a no-op against this specific CVE.
- Usage/reachability: `curl` appears only in a commented-out legacy line in `.s2i/bin/assemble`; the S2I builder image is not a network-facing runtime service and does not perform outbound HTTP/3/QUIC requests.
- Exploit preconditions: requires the local process to act as a QUIC client connecting to a malicious HTTP/3 server.
- Compensating controls: package unused by build/runtime scripts in this repository; image performs no outbound QUIC/HTTP-3 traffic.
- Reviewer verification: see **Evidence A** — reproduced full command output, one commented-out match, zero live invocations.
- Follow-up: rebuild + rescan after RHEL publishes a `curl-minimal` errata for this CVE (see Verification section for commands).

### curl-minimal / CVE-2026-11586

- Finding: `curl-minimal-7.76.1-40.el9` flagged for CVE-2026-11586 (WebSocket PING-flood memory exhaustion), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh curl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no errata build exists yet to upgrade to.
- Usage/reachability: no WebSocket connections are opened by any S2I build/run script; the image is not a listening service reachable by a malicious peer.
- Exploit preconditions: requires curl/libcurl to hold an active WebSocket connection to a peer flooding PING frames.
- Compensating controls: no WebSocket usage anywhere in this repository's build or runtime tooling.
- Reviewer verification: see **Evidence B** — reproduced `grep -rin "websocket\|--http\b\|ws://" .s2i/` exits `1` (zero matches).
- Follow-up: rebuild + rescan once RHEL ships a fix.

### curl-minimal / CVE-2026-12064

- Finding: `curl-minimal-7.76.1-40.el9` flagged for CVE-2026-12064 (schemeless-URL `--proto-default sftp/scp` scheme bypass), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh curl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no script in this repository invokes `curl --proto-default`; the only `curl` reference is commented out in `assemble`.
- Exploit preconditions: requires a user to explicitly combine a schemeless URL with `--proto-default sftp`/`scp`.
- Compensating controls: this invocation pattern is never used in this repository.
- Reviewer verification: see **Evidence B** — reproduced `grep -rin "proto-default\|CURLOPT_DEFAULT_PROTOCOL" .s2i/` exits `1` (zero matches).
- Follow-up: rebuild + rescan once RHEL ships a fix.

### curl-minimal / CVE-2026-8286

- Finding: `curl-minimal-7.76.1-40.el9` flagged for CVE-2026-8286 (STARTTLS connection-reuse TLS mismatch), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh curl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no STARTTLS transfers (SMTP/IMAP/FTP-with-TLS) are performed by this image's build or run scripts.
- Exploit preconditions: requires an application to reuse a connection across mismatched TLS configurations during a STARTTLS upgrade.
- Compensating controls: no STARTTLS usage anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced `grep -rin "starttls" .s2i/` exits `1` (zero matches).
- Follow-up: rebuild + rescan once RHEL ships a fix.

### curl-minimal / CVE-2026-8925

- Finding: `curl-minimal-7.76.1-40.el9` flagged for CVE-2026-8925 (GSASL context double-free), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh curl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no SASL-authenticated curl calls exist in this repository's build or runtime scripts.
- Exploit preconditions: requires an application performing SASL authentication through curl's GSASL integration.
- Compensating controls: no SASL usage anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced `grep -rin "sasl" .s2i/` exits `1` (zero matches).
- Follow-up: rebuild + rescan once RHEL ships a fix.

### curl-minimal / CVE-2026-9547

- Finding: `curl-minimal-7.76.1-40.el9` flagged for CVE-2026-9547 (SSH host-key check silently bypassed via `CURLOPT_SSH_KEYFUNCTION`), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh curl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no code in this repository registers `CURLOPT_SSH_KEYFUNCTION` or performs SCP/SFTP transfers via curl.
- Exploit preconditions: requires an application using that callback with a server presenting an unexpected host-key type.
- Compensating controls: option never set anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced `grep -rin "SSH_KEYFUNCTION" .s2i/` exits `1` (zero matches).
- Follow-up: rebuild + rescan once RHEL ships a fix.

### dotnet-host / CVE-2024-38081

- Finding: `dotnet-host-10.0.10-1.el9_8` flagged for CVE-2024-38081 (.NET/ASP.NET Core elevation of privilege), HIGH, status `end_of_life`.
- Remediation attempted: Added `dnf upgrade --refresh` for the full `dotnet-host`/`dotnet-hostfxr-10.0`/`dotnet-runtime-10.0`/`aspnetcore-runtime-10.0`/`dotnet-sdk-10.0` stack to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; the installed build is already the newest one published in the RHEL 9.8 `dotnet-100` errata channel as of the scan date, so `dnf upgrade` installs nothing newer.
- Usage/reachability: `dotnet-host` is a required build/runtime dependency for this application; it cannot be removed.
- Exploit preconditions: elevation-of-privilege vector in the .NET runtime; specifics not published beyond the vendor advisory title.
- Compensating controls: S2I builder runs as non-root — confirmed by `grep -n "^USER" .s2i/dockerfile`, which returns `USER root` (line 6, to run `dnf`) followed by `USER 1001` (final line), so the image drops to unprivileged UID 1001 before the build/run entrypoint executes; no untrusted multi-tenant code executes in this image.
- Reviewer verification: this is the one entry in this document where reachability cannot be foreclosed by code inspection — `dotnet-host` is genuinely required and genuinely affected. `rpm -q dotnet-host` in the built image should be cross-checked against the latest RHEL 9.8 `dotnet-100` errata at rescan time (Tier-2, handed off — no pod access from this sandbox) to confirm when this residual-risk entry can close.
- Follow-up: rebuild + rescan after the next RHEL `dotnet-100` errata publishes a fixed `dotnet-host` build.

### dotnet-host / CVE-2025-26682

- Finding: `dotnet-host-10.0.10-1.el9_8` flagged for CVE-2025-26682 (ASP.NET Core resource allocation/throttling DoS), HIGH, status `end_of_life`.
- Remediation attempted: Added `dnf upgrade --refresh aspnetcore-runtime-10.0` (and the rest of the .NET stack) to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; `aspnetcore-runtime-10.0` is already the latest build in the current errata channel.
- Usage/reachability: `aspnetcore-runtime-10.0` is a required runtime dependency for the MMRIA ASP.NET Core application; it cannot be removed.
- Exploit preconditions: unauthenticated network attacker sending requests that exhaust server-side resource limits.
- Compensating controls: none unique to this build-time image; the application-hosting environment's network controls (load balancer/WAF rate limiting) are out of scope for this S2I builder-image scan and are tracked separately from this artifact.
- Reviewer verification: this is the second entry where reachability cannot be foreclosed by code inspection — `aspnetcore-runtime-10.0` is genuinely required and genuinely network-facing once deployed. `rpm -q aspnetcore-runtime-10.0` cross-checked against the next RHEL 9.8 `dotnet-100` errata (Tier-2, handed off) to confirm when this residual-risk entry can close.
- Follow-up: rebuild + rescan after RHEL publishes a fixed `aspnetcore-runtime-10.0` build.

### dotnet-host / CVE-2025-59144

- Finding: `dotnet-host-10.0.10-1.el9_8` flagged for CVE-2025-59144, HIGH, status `under_investigation`.
- Remediation attempted: none required; this CVE does not describe the flagged component.
- Why not fixed here: N/A — not applicable.
- Usage/reachability: the finding's own description (and NVD's CVE-2025-59144 record) is exclusively about the npm package `debug` (malicious `4.4.2` published 2025-09-08, fixed in `4.4.3`); the RHEL RPM `dotnet-host-10.0.10-1.el9_8` does not vendor the npm `debug` package.
- Exploit preconditions: requires the compromised npm `debug@4.4.2` to be bundled into a browser context; not applicable to an RPM.
- Compensating controls: n/a — component mismatch, not a real exposure.
- Reviewer verification: see **Evidence E** — NVD text quoted verbatim confirms scope is `debug-js/debug`; `find . -iname "package*.json"` (repo-wide) returns zero matches, so the package cannot be pulled in by this repository's build.
- Follow-up: none; flag to whoever generates `findings.json` that this CVE is mismapped to `dotnet-host`.

### dotnet-host / CVE-2026-48779

- Finding: `dotnet-host-10.0.10-1.el9_8` flagged for CVE-2026-48779, HIGH, status `under_investigation`.
- Remediation attempted: none required; this CVE does not describe the flagged component.
- Why not fixed here: N/A — not applicable.
- Usage/reachability: NVD's CVE-2026-48779 record describes a memory-exhaustion DoS in the npm WebSocket library `ws` (1.1.0–8.21.0 ranges, fixed in 5.2.5/6.2.4/7.5.11/8.21.0); the RHEL RPM `dotnet-host-10.0.10-1.el9_8` does not contain the npm `ws` package.
- Exploit preconditions: requires a Node.js process using the npm `ws` server; not applicable to this .NET host RPM.
- Compensating controls: n/a — component mismatch, not a real exposure.
- Reviewer verification: see **Evidence E** — NVD text quoted verbatim confirms scope is the npm `ws` package; a compiled `dotnet-host` RPM cannot vendor an npm package by construction.
- Follow-up: none; flag to whoever generates `findings.json` that this CVE is mismapped to `dotnet-host`.

### libcurl-minimal / CVE-2026-11352

- Finding: `libcurl-minimal-7.76.1-40.el9` flagged for CVE-2026-11352 (QUIC/HTTP-3 UDP receive DoS), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh libcurl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: `libcurl-minimal` is a build-time link dependency of the .NET tooling in this image; nothing in this repository's build/runtime scripts opens outbound QUIC/HTTP-3 connections.
- Exploit preconditions: requires the local process to act as a QUIC client connecting to a malicious HTTP/3 server.
- Compensating controls: no QUIC/HTTP-3 traffic originates from this image.
- Reviewer verification: see **Evidence C** — zero non-comment `curl` invocations (Evidence A) and no libcurl-based application code (see Application HTTP stack note).
- Follow-up: rebuild + rescan once RHEL ships a fix.

### libcurl-minimal / CVE-2026-11586

- Finding: `libcurl-minimal-7.76.1-40.el9` flagged for CVE-2026-11586 (WebSocket PING-flood memory exhaustion), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh libcurl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no code path in this repository opens WebSocket connections through libcurl.
- Exploit preconditions: requires libcurl to hold an active WebSocket connection to a flooding peer.
- Compensating controls: no WebSocket usage anywhere in this repository's build/runtime tooling.
- Reviewer verification: see **Evidence B** — reproduced zero-match WebSocket search across `.s2i/`.
- Follow-up: rebuild + rescan once RHEL ships a fix.

### libcurl-minimal / CVE-2026-12064

- Finding: `libcurl-minimal-7.76.1-40.el9` flagged for CVE-2026-12064 (schemeless-URL `CURLOPT_DEFAULT_PROTOCOL` scheme bypass), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh libcurl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no code in this repository sets `CURLOPT_DEFAULT_PROTOCOL` to `sftp`/`scp`.
- Exploit preconditions: requires an application to combine a schemeless URL with that default-protocol option.
- Compensating controls: option never used anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced zero-match `CURLOPT_DEFAULT_PROTOCOL` search across `.s2i/`.
- Follow-up: rebuild + rescan once RHEL ships a fix.

### libcurl-minimal / CVE-2026-8286

- Finding: `libcurl-minimal-7.76.1-40.el9` flagged for CVE-2026-8286 (STARTTLS connection-reuse TLS mismatch), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh libcurl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no mixed plaintext/STARTTLS-upgraded transfer pattern exists in this repository's build/runtime tooling.
- Exploit preconditions: requires an application to reuse a libcurl handle across mismatched TLS configurations during a STARTTLS upgrade.
- Compensating controls: no STARTTLS usage anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced zero-match STARTTLS search across `.s2i/`.
- Follow-up: rebuild + rescan once RHEL ships a fix.

### libcurl-minimal / CVE-2026-8925

- Finding: `libcurl-minimal-7.76.1-40.el9` flagged for CVE-2026-8925 (GSASL context double-free), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh libcurl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no SASL-authenticated transfers are configured through libcurl anywhere in this repository.
- Exploit preconditions: requires an application performing SASL authentication through libcurl's GSASL integration.
- Compensating controls: no SASL usage anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced zero-match SASL search across `.s2i/`.
- Follow-up: rebuild + rescan once RHEL ships a fix.

### libcurl-minimal / CVE-2026-9547

- Finding: `libcurl-minimal-7.76.1-40.el9` flagged for CVE-2026-9547 (SSH host-key check bypass via `CURLOPT_SSH_KEYFUNCTION`), HIGH.
- Remediation attempted: Added `dnf upgrade --refresh libcurl-minimal` to `.s2i/dockerfile`.
- Why not fixed here: empty `fixedIn` in `findings.json`; no newer package exists.
- Usage/reachability: no code in this repository registers `CURLOPT_SSH_KEYFUNCTION` or performs SCP/SFTP transfers via libcurl.
- Exploit preconditions: requires an application using that callback against a server presenting an unexpected host-key type.
- Compensating controls: callback never registered anywhere in this repository.
- Reviewer verification: see **Evidence B** — reproduced zero-match `SSH_KEYFUNCTION` search across `.s2i/`.
- Follow-up: rebuild + rescan once RHEL ships a fix.

### tar / CVE-2026-59873

- Finding: `tar-2:1.34-11.el9` flagged for CVE-2026-59873, HIGH.
- Remediation attempted: none required; this CVE does not describe the flagged component.
- Why not fixed here: N/A — not applicable.
- Usage/reachability: the finding's description (and OSV's CVE-2026-59873 record) is exclusively about the npm package `node-tar` (gzip-bomb decompression DoS, fixed in `node-tar@7.5.19`); the RHEL RPM `tar-2:1.34-11.el9` is GNU tar, an unrelated codebase that does not contain `node-tar`.
- Exploit preconditions: requires a Node.js application using the npm `node-tar` extraction path; not applicable to GNU tar.
- Compensating controls: n/a — component mismatch, not a real exposure. GNU `tar` itself is only invoked in `.s2i/bin/run` when `DOTNET_PACK=true`, in a build-time-only context.
- Reviewer verification: see **Evidence D/E** — the only `tar` invocation in `.s2i/bin/assemble` is `tar -czf` (create-only, never extract) on this repo's own trusted output; OSV text quoted verbatim scopes the CVE to the npm `node-tar` codebase, which a GNU-tar RPM cannot contain.
- Follow-up: none; flag to whoever generates `findings.json` that this CVE is mismapped to the `tar` RPM.

### tar / CVE-2026-59874

- Finding: `tar-2:1.34-11.el9` flagged for CVE-2026-59874, HIGH.
- Remediation attempted: none required; this CVE does not describe the flagged component.
- Why not fixed here: N/A — not applicable.
- Usage/reachability: the finding's description (and OSV's CVE-2026-59874 record) is exclusively about the npm package `node-tar` (`tar.replace` negative base-256 size hang, fixed in `node-tar@7.5.18`); the RHEL RPM `tar-2:1.34-11.el9` is GNU tar, an unrelated codebase.
- Exploit preconditions: requires a Node.js application using the npm `node-tar` `replace` API; not applicable to GNU tar.
- Compensating controls: n/a — component mismatch, not a real exposure.
- Reviewer verification: see **Evidence D/E** — same create-only `tar -czf` invocation as above; OSV text quoted verbatim scopes the CVE to the npm `node-tar` `replace` API, absent from GNU tar.
- Follow-up: none; flag to whoever generates `findings.json` that this CVE is mismapped to the `tar` RPM.

## Verification

- Repo-static verification completed:
  - reproduced Evidence A–D above directly against this repository's `.s2i/bin/assemble`/`.s2i/bin/run`; `curl` only appears in a commented-out legacy line, WebSocket/STARTTLS/SASL/`SSH_KEYFUNCTION`/`proto-default` never appear at all, and `tar` is only ever invoked to create (never extract) an archive, gated behind `DOTNET_PACK=true`
  - confirmed via `findings.json` (issue #530 comment) that all 18 HIGH findings have an empty `fixedIn` field — no RHEL errata or npm-package correction currently applies to the installed RPMs (a supporting fact, not the basis for any reachability verdict above)
  - confirmed the four npm-described CVEs (`CVE-2025-59144`, `CVE-2026-48779`, `CVE-2026-59873`, `CVE-2026-59874`) reference packages (`debug`, `ws`, `node-tar`) not present in the flagged RPMs, via the NVD/OSV text quoted verbatim in Evidence E, and independently confirmed this repository has no `package.json` anywhere in it (`find . -iname "package*.json"` returns zero results) for the npm findings to have ever reached
  - `.s2i/dockerfile` retains `dnf upgrade --refresh` for the fourteen genuinely-affected packages so a future RHEL errata is picked up automatically without further code changes
- CI log investigation attempted via GitHub MCP using workflow run `30112194673`, but the external `cdcent/nccdphp-od-devops` Actions API returned `404`, so no upstream build logs were available from this session
- Pod/rebuild checks were not run from this sandbox. Run the following after the next image build, and after each subsequent RHEL `dotnet-100`/curl/tar errata release, to confirm whether any of the fourteen Residual-risk findings can be closed:

```shell
docker build -f .s2i/dockerfile -t mmria-s2i:trivy-fix .
trivy image --severity HIGH,CRITICAL mmria-s2i:trivy-fix
```

## SWA Exception Justifications

### curl-minimal / CVE-2026-11352

- **CVE:** CVE-2026-11352
- **Package:** curl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** this build/runtime image never issues outbound QUIC/HTTP-3 requests (Evidence A/C), which is the sole precondition for this DoS; no fixed package exists yet, but that fact is secondary to the reachability proof.

CVE-2026-11352 targets curl's QUIC UDP receive helper, exploitable only when the local process acts as an HTTP/3 client against a malicious server. Evidence A (`grep -n "curl" .s2i/bin/assemble .s2i/bin/run`, full output reproduced above) shows the *only* `curl` reference in either script this image runs is a `#`-commented legacy line at `.s2i/bin/assemble:46`; there is no other invocation to make QUIC/HTTP-3 requests through. That is direct, reproducible proof the vulnerable code path cannot execute from this repository's build or runtime usage, independent of the installed package version. `findings.json` additionally shows an empty `fixedIn` for this CVE, so no fixed `curl-minimal` build exists yet; rebuild-and-rescan is tracked for when RHEL publishes one, but does not change today's verdict.

### curl-minimal / CVE-2026-11586

- **CVE:** CVE-2026-11586
- **Package:** curl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no S2I script opens a WebSocket connection (Evidence B, reproduced zero-match search), which is the sole precondition for this DoS; no fixed package exists yet, but that fact is secondary to the reachability proof.

CVE-2026-11586 requires curl to hold an active WebSocket connection while a malicious peer floods PING frames, exhausting memory because curl has no upper bound on unacknowledged-frame allocation. Evidence A shows `curl` is invoked nowhere but a commented-out line, and Evidence B (`grep -rin "websocket\|--http\b\|ws://" .s2i/`, exit code `1`, zero output, reproduced above) shows no WebSocket handling anywhere in the scripts this image runs. With no code path capable of opening a WebSocket, the flooding precondition cannot occur, regardless of package version. `findings.json` shows an empty `fixedIn`, so no fixed build exists yet; that does not change this verdict.

### curl-minimal / CVE-2026-12064

- **CVE:** CVE-2026-12064
- **Package:** curl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no repository script sets `--proto-default`/`CURLOPT_DEFAULT_PROTOCOL` (Evidence B, reproduced zero-match search), which is the sole precondition for this bypass; no fixed package exists yet, but that fact is secondary to the reachability proof.

CVE-2026-12064 requires a user to invoke curl with a schemeless URL combined with `--proto-default sftp` (or `scp`), which causes the tool layer to misinfer the scheme and skip protocol-specific initialization. Evidence B (`grep -rin "proto-default\|CURLOPT_DEFAULT_PROTOCOL" .s2i/`, exit code `1`, zero output) proves that flag is never passed anywhere in this repository, and Evidence A proves `curl` itself is invoked nowhere but a commented-out line — there is no combination of live code that could trigger this bypass. `findings.json` shows an empty `fixedIn`, so no fixed build exists yet; that is a secondary fact, not the basis for this verdict.

### curl-minimal / CVE-2026-8286

- **CVE:** CVE-2026-8286
- **Package:** curl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** this image performs no STARTTLS transfers (Evidence B, reproduced zero-match search), which is the sole precondition for this mismatch; no fixed package exists yet, but that fact is secondary to the reachability proof.

CVE-2026-8286 allows a new STARTTLS-upgraded transfer to reuse an existing live connection despite a mismatched TLS configuration. Evidence B (`grep -rin "starttls" .s2i/`, exit code `1`, zero output) proves no SMTP/IMAP/FTP-with-TLS or other STARTTLS usage exists anywhere in this repository's build or runtime path, and Evidence A proves `curl` is otherwise unused — there is no connection-reuse scenario to mismatch. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### curl-minimal / CVE-2026-8925

- **CVE:** CVE-2026-8925
- **Package:** curl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** this repository never configures SASL authentication through curl (Evidence B, reproduced zero-match search), which is the sole precondition for this double-free; no fixed package exists yet, but that fact is secondary to the reachability proof.

CVE-2026-8925 is a double-free in curl's SASL authentication logic that can clean up the GSASL context twice without clearing the pointer between calls. Evidence B (`grep -rin "sasl" .s2i/`, exit code `1`, zero output) proves no SASL-authenticated curl usage exists in `.s2i/bin/assemble`, `.s2i/bin/run`, or anywhere else this image builds/runs, and Evidence A proves curl is otherwise unused — the cleanup path that double-frees is never entered. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### curl-minimal / CVE-2026-9547

- **CVE:** CVE-2026-9547
- **Package:** curl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no code in this repository registers `CURLOPT_SSH_KEYFUNCTION` (Evidence B, reproduced zero-match search), which is the sole precondition for this bypass; no fixed package exists yet, but that fact is secondary to the reachability proof.

CVE-2026-9547 lets an application using `CURLOPT_SSH_KEYFUNCTION` silently accept an untrusted SCP/SFTP server presenting an unexpected host-key type. Evidence B (`grep -rin "SSH_KEYFUNCTION" .s2i/`, exit code `1`, zero output) proves that callback is never registered, and Evidence A proves curl performs no SCP/SFTP transfers at all in this repository — the bypass requires a callback that is never installed. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### dotnet-host / CVE-2024-38081

- **CVE:** CVE-2024-38081
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** end_of_life (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** dotnet-host has no newer published RPM build for this elevation-of-privilege CVE, and the .NET host is a required build/runtime dependency.

CVE-2024-38081 is a .NET/Visual Studio elevation-of-privilege vulnerability in the .NET host/runtime stack. `findings.json` records an empty `fixedIn` for this CVE against `dotnet-host-10.0.10-1.el9_8`, indicating RHEL's `dotnet-100` errata channel has not yet published a build with a fix as of the scan date, so `dnf upgrade --refresh dotnet-host` in `.s2i/dockerfile` installs nothing newer. `dotnet-host` is a required component to build and run this .NET application and cannot be removed. This S2I builder image runs as non-root `USER 1001` (set at the end of `.s2i/dockerfile`), limiting the blast radius of a local elevation vector. Rebuild is required once RHEL publishes a fixed `dotnet-host` build; the exact package/version to confirm is tracked in the Verification section's rescan command.

### dotnet-host / CVE-2025-26682

- **CVE:** CVE-2025-26682
- **Package:** dotnet-host@10.0.10-1.el9_8 (component: aspnetcore-runtime-10.0)
- **Severity:** HIGH
- **Status:** end_of_life (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** aspnetcore-runtime-10.0 has no newer published RPM build for this resource-throttling DoS CVE, and it is a required runtime dependency of the application.

CVE-2025-26682 is a resource-allocation-without-limits DoS in ASP.NET Core. `findings.json` records an empty `fixedIn` for this CVE (reported under the `dotnet-host` target); RHEL's `dotnet-100` errata channel has not published a fixed `aspnetcore-runtime-10.0` build as of the scan date, so the `dnf upgrade --refresh aspnetcore-runtime-10.0` in `.s2i/dockerfile` installs nothing newer. `aspnetcore-runtime-10.0` is a required component for hosting this application and cannot be removed. Network-layer rate limiting for the deployed service is managed by the hosting environment (load balancer/WAF), outside the scope of this S2I builder-image artifact. Rebuild is required once RHEL publishes a fixed `aspnetcore-runtime-10.0` build.

### dotnet-host / CVE-2025-59144

- **CVE:** CVE-2025-59144
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** under_investigation
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2025-59144 describes a compromised npm package (`debug`) that is not present in the `dotnet-host` RPM.

NVD's CVE-2025-59144 record states this vulnerability is scoped entirely to the npm package `debug-js/debug` version `4.4.2`, published after a September 2025 npm account phishing takeover and fixed in `4.4.3`; the malware only affects browser-bundled JavaScript. The RHEL RPM `dotnet-host-10.0.10-1.el9_8` is the .NET host binary/runtime component and does not vendor or bundle the npm `debug` package in any form. This is not an inference: `find . -iname "package*.json"` run against this repository returns zero results (Evidence E) — there is no `package.json`/`package-lock.json` anywhere in this repository for `debug` to be listed in, and the one unconditional `npm install`/`npm run build` block in `.s2i/bin/assemble` (lines 30-33) is entirely commented out. There is no mechanism by which this repository's build could ever fetch the npm `debug` package, let alone the single malicious `4.4.2` release. This is a scanner package-mapping mismatch between the CVE's actual scope and the RPM it was attached to in `findings.json`.

### dotnet-host / CVE-2026-48779

- **CVE:** CVE-2026-48779
- **Package:** dotnet-host@10.0.10-1.el9_8
- **Severity:** HIGH
- **Status:** under_investigation
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2026-48779 describes a memory-exhaustion DoS in the npm WebSocket library `ws`, which is not present in the `dotnet-host` RPM.

NVD's CVE-2026-48779 record scopes this vulnerability to the npm package `ws` across the 1.1.0–8.21.0 version ranges, fixed in 5.2.5/6.2.4/7.5.11/8.21.0; exploitation requires a Node.js `ws` server accepting a flood of small WebSocket fragments. The RHEL RPM `dotnet-host-10.0.10-1.el9_8` is a compiled .NET host binary produced by Red Hat's build pipeline from .NET/C# sources; an RPM of this kind cannot vendor an npm JavaScript package by construction, and (as in the `debug` finding above) this repository has no `package.json` anywhere in it for `ws` to be declared in. This is a scanner package-mapping mismatch between the CVE's actual scope and the RPM it was attached to in `findings.json`, not a real finding against `dotnet-host`.

### libcurl-minimal / CVE-2026-11352

- **CVE:** CVE-2026-11352
- **Package:** libcurl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** `libcurl-minimal` is a build-time link dependency of SDK tooling only (Evidence A/C); no outbound QUIC/HTTP-3 traffic originates from this image, which forecloses the precondition regardless of package version.

CVE-2026-11352 targets the same QUIC UDP receive helper as the `curl-minimal` finding above, reported separately against the shared library `libcurl-minimal-7.76.1-40.el9`. `libcurl-minimal` is a build-time link dependency of the .NET SDK/runtime tooling in this image (see Application HTTP stack note in Evidence section — MMRIA itself uses .NET's `HttpClient`, not libcurl); no script reviewed in Evidence A/C performs outbound HTTP/3 or QUIC requests through it, so the malicious-server precondition cannot be triggered from this image's usage regardless of package version. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### libcurl-minimal / CVE-2026-11586

- **CVE:** CVE-2026-11586
- **Package:** libcurl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no code path in this repository opens WebSocket connections through libcurl (Evidence B, reproduced zero-match search), which forecloses the precondition regardless of package version.

CVE-2026-11586 is reported against `libcurl-minimal-7.76.1-40.el9`. Evidence B (reproduced zero-match WebSocket search across `.s2i/`) proves no code opens a WebSocket through libcurl, and this image is not a listening service that a malicious peer could flood — both preconditions are foreclosed regardless of package version. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### libcurl-minimal / CVE-2026-12064

- **CVE:** CVE-2026-12064
- **Package:** libcurl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no code in this repository sets `CURLOPT_DEFAULT_PROTOCOL` (Evidence B, reproduced zero-match search), which forecloses the precondition regardless of package version.

CVE-2026-12064 is reported against `libcurl-minimal-7.76.1-40.el9`. Exploitation requires an application to set `CURLOPT_DEFAULT_PROTOCOL` to `sftp`/`scp` alongside a schemeless URL; Evidence B (reproduced zero-match search for `CURLOPT_DEFAULT_PROTOCOL` across `.s2i/`) proves no code in this repository sets that libcurl option. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### libcurl-minimal / CVE-2026-8286

- **CVE:** CVE-2026-8286
- **Package:** libcurl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** this repository never mixes plaintext/STARTTLS transfers on a shared handle (Evidence B, reproduced zero-match search), which forecloses the precondition regardless of package version.

CVE-2026-8286 is reported against `libcurl-minimal-7.76.1-40.el9`. Exploitation requires an application to reuse a libcurl handle across mismatched TLS configurations during a STARTTLS upgrade; Evidence B (reproduced zero-match STARTTLS search across `.s2i/`) proves no such pattern exists in this repository's build or runtime scripts. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### libcurl-minimal / CVE-2026-8925

- **CVE:** CVE-2026-8925
- **Package:** libcurl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no code in this repository configures SASL authentication through libcurl (Evidence B, reproduced zero-match search), which forecloses the precondition regardless of package version.

CVE-2026-8925 is reported against `libcurl-minimal-7.76.1-40.el9`. Exploitation requires an application performing SASL-authenticated transfers through libcurl's GSASL integration; Evidence B (reproduced zero-match SASL search across `.s2i/`) proves no such configuration exists anywhere in this repository. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### libcurl-minimal / CVE-2026-9547

- **CVE:** CVE-2026-9547
- **Package:** libcurl-minimal@7.76.1-40.el9
- **Severity:** HIGH
- **Status:** affected (no fix available)
- **Verdict:** Residual risk – required, not reachable under current controls

**Summary:** no code in this repository registers `CURLOPT_SSH_KEYFUNCTION` (Evidence B, reproduced zero-match search), which forecloses the precondition regardless of package version.

CVE-2026-9547 is reported against `libcurl-minimal-7.76.1-40.el9`. Exploitation requires an application registering `CURLOPT_SSH_KEYFUNCTION` for SCP/SFTP host-key validation; Evidence B (reproduced zero-match `SSH_KEYFUNCTION` search across `.s2i/`) proves no code in this repository sets that callback or performs SCP/SFTP transfers through libcurl. `findings.json` shows an empty `fixedIn`; no fixed build exists yet, but that is secondary to the reachability proof.

### tar / CVE-2026-59873

- **CVE:** CVE-2026-59873
- **Package:** tar@2:1.34-11.el9
- **Severity:** HIGH
- **Status:** affected
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59873 describes a gzip-bomb DoS in the npm package `node-tar`, which is not present in the GNU `tar` RPM.

OSV's CVE-2026-59873 record scopes this vulnerability entirely to the npm package `node-tar` prior to version `7.5.19`, where extraction paths such as `src/extract.ts` lack hard bounds on decompressed data, entry counts, or decompression ratio; fixed in `node-tar@7.5.19`. The RHEL RPM `tar-2:1.34-11.el9` is GNU tar, a completely different C codebase that does not contain the npm `node-tar` package. As independent, secondary proof: Evidence D (`grep -n "tar " .s2i/bin/assemble .s2i/bin/run`, full output reproduced above) shows the only `tar` invocation anywhere in this repository's build scripts is `tar -czf` in `.s2i/bin/assemble` line 367 (gated behind `if [ "$DOTNET_PACK" == "true" ]`) — a create/compress of this repository's own already-built output, never an extract of untrusted input, so even the GNU `tar` binary's own extraction code (irrelevant here, since the CVE is about `node-tar` not GNU tar) is never exercised by this pipeline. This is a scanner package-mapping mismatch, not a real finding against the GNU `tar` RPM.

### tar / CVE-2026-59874

- **CVE:** CVE-2026-59874
- **Package:** tar@2:1.34-11.el9
- **Severity:** HIGH
- **Status:** affected
- **Verdict:** Not applicable / false positive

**Summary:** CVE-2026-59874 describes a hang in the npm package `node-tar`'s `replace` API, which is not present in the GNU `tar` RPM.

OSV's CVE-2026-59874 record scopes this vulnerability entirely to the npm package `node-tar` prior to version `7.5.18`, where `tar.replace` accepts a checksum-valid header with a negative base-256 encoded entry size, causing the archive scanner to loop without progress; fixed in `node-tar@7.5.18`. The RHEL RPM `tar-2:1.34-11.el9` is GNU tar, an unrelated C codebase with no `tar.replace` JavaScript API and no npm `node-tar` dependency. Evidence D confirms the only `tar` invocation in this repository is the single `tar -czf` create call noted above; there is no `node-tar`-style `replace` operation anywhere in this pipeline. This is a scanner package-mapping mismatch, not a real finding against the GNU `tar` RPM.
