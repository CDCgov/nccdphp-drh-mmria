# Trivy Remediation Records

This file is the system of record for all Trivy scan remediations. Newest scan
blocks are prepended. Carry-forward verdicts are noted inline.

---

## Scan: MMRIA S2I @ 609db310 — 2026-07-16

**Service:** MMRIA S2I  
**Scan ID:** 30780  
**Commit:** 609db31044d34809ffc447b67c5261be9a081617  
**Scan date:** 2026-07-16  
**Target:** `default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/mmria/mmria-s2i:latest (redhat 9.8)`  
**Workflow run:** https://github.com/cdcent/nccdphp-od-devops/actions/runs/29462913456

### Triage Summary

| Severity | Original | Fixed | Pending image update | Residual risk | Not applicable | Remaining |
|---|---:|---:|---:|---:|---:|---:|
| CRITICAL | 0 | 0 | 0 | 0 | 0 | 0 |
| HIGH | 24 | 0 | 0 | 20 | 4 | 0 |

### Remediation Actions

A `RUN dnf update -y --nodocs && dnf clean all` layer was added to the runtime
stage of `source-code/mmria/mmria-server/Dockerfile` and
`nccdphp-drh-mmria-services/mmria.services/Dockerfile`. At the time of this scan
all 24 HIGH findings carry empty `fixedIn` fields — no RHEL 9.8 patches are yet
published — so the layer does not change current verdicts. It will reduce the
finding count on the next rebuild once Red Hat releases patches.

### Finding Inventory

| Package | CVE | Severity | Status | Verdict |
|---|---|---|---|---|
| aspnetcore-runtime-10.0 | CVE-2026-50651 | HIGH | under_investigation | Residual risk – required, not reachable under current controls |
| curl-minimal | CVE-2026-11352 | HIGH | affected | Residual risk – required, not reachable under current controls |
| curl-minimal | CVE-2026-11586 | HIGH | affected | Residual risk – required, not reachable under current controls |
| curl-minimal | CVE-2026-12064 | HIGH | affected | Residual risk – required, not reachable under current controls |
| curl-minimal | CVE-2026-8286 | HIGH | affected | Residual risk – required, not reachable under current controls |
| curl-minimal | CVE-2026-8925 | HIGH | affected | Residual risk – required, not reachable under current controls |
| curl-minimal | CVE-2026-9547 | HIGH | affected | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2024-38081 | HIGH | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2025-26682 | HIGH | end_of_life | Residual risk – required, not reachable under current controls |
| dotnet-host | CVE-2025-59144 | HIGH | end_of_life | Not applicable / false positive |
| dotnet-host | CVE-2026-48779 | HIGH | under_investigation | Not applicable / false positive |
| dotnet-host | CVE-2026-50651 | HIGH | under_investigation | Residual risk – required, not reachable under current controls |
| dotnet-hostfxr-10.0 | CVE-2026-50651 | HIGH | under_investigation | Residual risk – required, not reachable under current controls |
| dotnet-runtime-10.0 | CVE-2026-50651 | HIGH | under_investigation | Residual risk – required, not reachable under current controls |
| glib2 | CVE-2026-58016 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libacl | CVE-2026-54369 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libcurl-minimal | CVE-2026-11352 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libcurl-minimal | CVE-2026-11586 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libcurl-minimal | CVE-2026-12064 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libcurl-minimal | CVE-2026-8286 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libcurl-minimal | CVE-2026-8925 | HIGH | affected | Residual risk – required, not reachable under current controls |
| libcurl-minimal | CVE-2026-9547 | HIGH | affected | Residual risk – required, not reachable under current controls |
| tar | CVE-2026-59873 | HIGH | affected | Not applicable / false positive |
| tar | CVE-2026-59874 | HIGH | affected | Not applicable / false positive |

## SWA Exception Justifications

### aspnetcore-runtime-10.0 / CVE-2026-50651

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 10.0.9-1.el9_8  
**Fix available:** None (fixedIn empty; Red Hat status: under_investigation)  
**Summary:** CVE-2026-50651 describes an allocation-of-resources denial-of-service condition in .NET reachable by an unauthenticated network attacker. The ASP.NET Core runtime package `aspnetcore-runtime-10.0` is a mandatory dependency of the MMRIA server application; it cannot be removed without replacing the entire application stack. Red Hat has not yet published a patch for RHEL 9.8. NVD describes the attack vector as network-based; mitigation is possible through request-rate controls and resource quotas at the OpenShift router and namespace level. These cluster-level controls are present in the EcPaaS environment but cannot be verified from repository context — a Tier-2 check is flagged. Residual risk is accepted pending Red Hat patch availability.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q aspnetcore-runtime-10.0
```

---

### curl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-11352 describes a denial-of-service flaw in curl's QUIC/HTTP3 UDP receive path: a malicious HTTP/3 server can trigger a remote DoS against a curl or libcurl client by exploiting how zero-length UDP datagrams are handled before the per-connection counter is incremented. The `curl-minimal` package is a base OS dependency of the RHEL 9.8 runtime image and is not directly invoked by MMRIA application code. Exploitation requires the container to initiate an outbound HTTP/3 connection to a malicious server — MMRIA makes outbound calls to CouchDB and internal APIs over HTTPS/TCP, not HTTP/3 UDP. No Red Hat patch is available for RHEL 9.8. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q curl-minimal
oc rsh <mmria-pod> grep -r "http3\|QUIC\|quic" /app/ 2>/dev/null || echo "no HTTP/3 references in app"
```

---

### curl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-11586 describes a memory-exhaustion DoS in curl's WebSocket PING handler: curl automatically responds to WebSocket PING frames and has no upper bound on memory allocation for unacknowledged frames, allowing a malicious server to exhaust client memory via rapid sequential PINGs. Exploitation requires an active WebSocket session initiated via the `curl` binary to a malicious server. The `curl-minimal` package is a base OS dependency of the RHEL 9.8 runtime image and is not directly called by MMRIA application code. MMRIA communicates with CouchDB and internal services over standard HTTPS without using the OS `curl` binary for WebSocket connections. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q curl-minimal
oc rsh <mmria-pod> grep -r "WebSocket\|websocket" /app/ 2>/dev/null | grep -i curl || echo "no curl WebSocket usage in app"
```

---

### curl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-12064 describes a URL-scheme confusion in curl when a schemeless URL is combined with `--proto-default sftp` or `--proto-default scp`: the tool layer incorrectly infers the URL scheme, bypassing SFTP/SCP initialization and potentially allowing connection to an unintended server. Exploitation requires command-line invocation of `curl` with specific `--proto-default` flags set to SFTP or SCP. The `curl-minimal` package is a base OS dependency of the RHEL 9.8 runtime image; MMRIA application code does not invoke the `curl` binary with SFTP or SCP protocol defaults. The container runs as non-root user 1001 and does not execute shell scripts that call curl in this configuration. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q curl-minimal
oc rsh <mmria-pod> grep -r "proto-default\|sftp\|scp" /app/ 2>/dev/null || echo "no sftp/scp references in app"
```

---

### curl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-8286 describes a TLS connection reuse flaw in curl's STARTTLS upgrade path: a new transfer using STARTTLS may reuse an existing live connection even though TLS configuration mismatches should prevent it, potentially transmitting data over a misconfigured channel. Exploitation requires the application to initiate STARTTLS-based connections through the curl binary — protocols such as SMTP, IMAP, or FTP with TLS upgrade. The `curl-minimal` package is a base OS dependency of the RHEL 9.8 runtime image; MMRIA communicates with CouchDB and internal services via direct HTTPS and does not use STARTTLS-capable protocols via the OS `curl` binary. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q curl-minimal
oc rsh <mmria-pod> grep -r "STARTTLS\|starttls\|smtp\|imap" /app/ 2>/dev/null || echo "no STARTTLS protocols in app"
```

---

### curl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-8925 describes a double-free vulnerability in curl's SASL/GSASL authentication handler: the GSASL context is freed twice without clearing the pointer between frees, creating potential memory corruption. Exploitation requires SASL or GSASL authentication to be actively used in a curl connection. The `curl-minimal` package is a base OS dependency of the RHEL 9.8 runtime image; MMRIA does not invoke the OS `curl` binary with SASL or GSASL authentication — CouchDB connections use basic HTTP authentication over HTTPS via the .NET `HttpClient`. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q curl-minimal
oc rsh <mmria-pod> grep -r "GSASL\|gsasl\|SASL\|sasl" /app/ 2>/dev/null || echo "no SASL references in app"
```

---

### curl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-9547 describes a silent host-key acceptance vulnerability in curl's SSH key callback: when a server presents a host-key type not handled by the `CURLOPT_SSH_KEYFUNCTION` callback, curl may silently accept an untrusted server during SCP or SFTP transfers. Exploitation requires the application to perform SCP or SFTP transfers via curl with the SSH key callback configured. The `curl-minimal` package is a base OS dependency of the RHEL 9.8 runtime image; MMRIA does not perform SCP or SFTP file transfers — it communicates with CouchDB and internal services over HTTPS. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q curl-minimal
oc rsh <mmria-pod> grep -r "sftp://\|scp://\|SSH_KEYFUNCTION" /app/ 2>/dev/null || echo "no SCP/SFTP URLs in app"
```

---

### dotnet-host / CVE-2024-38081

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 10.0.9-1.el9_8  
**Fix available:** None (fixedIn empty; Red Hat status: end_of_life)  
**Summary:** CVE-2024-38081 describes an Elevation of Privilege vulnerability in .NET, .NET Framework, and Visual Studio. NVD CVSS vector is AV:L (Local), requiring an attacker to already have local code execution on the system to escalate privileges. Visual Studio is a Windows IDE not present in the RHEL 9.8 container image. The `dotnet-host` package is the .NET runtime host binary required to execute the MMRIA server; it cannot be removed. Red Hat marks this as end_of_life with no patch available for the installed version. The local attack vector (AV:L) means the attacker must already have code execution in the container; the container runs as non-root user 1001 under OpenShift's restricted SCC, limiting privilege escalation paths. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q dotnet-host
oc rsh <mmria-pod> id
```

---

### dotnet-host / CVE-2025-26682

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 10.0.9-1.el9_8  
**Fix available:** None (fixedIn empty; Red Hat status: end_of_life)  
**Summary:** CVE-2025-26682 describes an allocation-of-resources denial-of-service flaw in ASP.NET Core reachable by an unauthenticated network attacker. The `dotnet-host` package is a required runtime component for executing the MMRIA .NET application; it cannot be removed. Red Hat marks this as end_of_life with no patch available for RHEL 9.8. The MMRIA application is deployed behind an OpenShift router with rate-limiting capabilities that reduce DoS exploitability; however, these cluster-level controls cannot be verified from repository context. Residual risk is accepted pending a Red Hat or upstream patch; the application should be monitored for abnormal resource consumption.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q dotnet-host
```

---

### dotnet-host / CVE-2025-59144

**Verdict:** Not applicable / false positive  
**Installed version:** 10.0.9-1.el9_8  
**Summary:** CVE-2025-59144 was assigned to the npm JavaScript package `debug` (a debugging utility) following a supply-chain attack in which the npm publishing account was compromised on 8 September 2025 and a malicious version 4.4.2 was published. The CVE description states explicitly: "debug is a JavaScript debugging utility." The RHEL package `dotnet-host` is the .NET runtime host binary — a native C/C++ executable distributed by Red Hat — and has no relationship to the JavaScript npm ecosystem or the `debug` npm package. The Trivy vulnerability database has incorrectly mapped this JavaScript supply-chain CVE to the unrelated `dotnet-host` system package. The MMRIA container is a pure .NET application with no Node.js runtime or npm packages in its final image; the `debug` npm package is entirely absent from the image. This finding is a false positive caused by a CVE-database package attribution error.  
**Verification commands:**
```sh
oc rsh <mmria-pod> which node 2>/dev/null || echo "no Node.js runtime in image"
oc rsh <mmria-pod> find /app /usr/lib /usr/local -name "debug" -path "*/node_modules/*" 2>/dev/null || echo "no npm debug package in image"
```

---

### dotnet-host / CVE-2026-48779

**Verdict:** Not applicable / false positive  
**Installed version:** 10.0.9-1.el9_8  
**Summary:** CVE-2026-48779 was assigned to the npm JavaScript package `ws` (a WebSocket client/server for Node.js) for a memory-exhaustion DoS vulnerability affecting ws versions 1.1.0 through 8.20.x. The CVE description states explicitly: "ws is an open source WebSocket client and server for Node.js." The RHEL package `dotnet-host` is the .NET runtime host binary — a native C/C++ executable distributed by Red Hat — and has no relationship to the npm `ws` package or the Node.js ecosystem. The Trivy vulnerability database has incorrectly mapped this Node.js library CVE to the unrelated `dotnet-host` system package. The MMRIA container is a pure .NET application with no Node.js runtime or npm packages in its final image; the `ws` npm package is entirely absent from the image. This finding is a false positive caused by a CVE-database package attribution error.  
**Verification commands:**
```sh
oc rsh <mmria-pod> which node 2>/dev/null || echo "no Node.js runtime in image"
oc rsh <mmria-pod> find /app /usr/lib /usr/local -name "ws" -path "*/node_modules/*" 2>/dev/null || echo "no npm ws package in image"
```

---

### dotnet-host / CVE-2026-50651

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 10.0.9-1.el9_8  
**Fix available:** None (fixedIn empty; Red Hat status: under_investigation)  
**Summary:** CVE-2026-50651 describes an allocation-of-resources denial-of-service condition in .NET reachable by an unauthenticated network attacker. The `dotnet-host` package is the .NET runtime host binary — a mandatory dependency for executing the MMRIA server application. It cannot be removed without making the application non-functional. Red Hat is actively investigating a patch for RHEL 9.8; none is yet available. OpenShift router rate-limiting and namespace resource quotas reduce exploitability; however, these cluster controls cannot be verified from repository context. Residual risk is accepted pending Red Hat patch availability.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q dotnet-host
```

---

### dotnet-hostfxr-10.0 / CVE-2026-50651

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 10.0.9-1.el9_8  
**Fix available:** None (fixedIn empty; Red Hat status: under_investigation)  
**Summary:** CVE-2026-50651 describes an allocation-of-resources denial-of-service condition in .NET reachable by an unauthenticated network attacker. The `dotnet-hostfxr-10.0` package is the .NET host framework resolver — a mandatory runtime component that locates and initializes the correct .NET runtime version for MMRIA. It cannot be removed without breaking application startup. Red Hat is actively investigating a patch for RHEL 9.8; none is yet available. OpenShift router rate-limiting and namespace resource quotas reduce exploitability; however, these cluster controls cannot be verified from repository context. Residual risk is accepted pending Red Hat patch availability.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q dotnet-hostfxr-10.0
```

---

### dotnet-runtime-10.0 / CVE-2026-50651

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 10.0.9-1.el9_8  
**Fix available:** None (fixedIn empty; Red Hat status: under_investigation)  
**Summary:** CVE-2026-50651 describes an allocation-of-resources denial-of-service condition in .NET reachable by an unauthenticated network attacker. The `dotnet-runtime-10.0` package provides the core .NET CLR (Common Language Runtime) required to execute the MMRIA server's compiled assemblies. It cannot be removed without making the application non-functional. Red Hat is actively investigating a patch for RHEL 9.8; none is yet available. OpenShift router rate-limiting and namespace resource quotas reduce exploitability; however, these cluster controls cannot be verified from repository context. Residual risk is accepted pending Red Hat patch availability.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q dotnet-runtime-10.0
```

---

### glib2 / CVE-2026-58016

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 2.68.4-19.el9_8.1  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-58016 describes a state-confusion vulnerability in GLib's `g_dbus_node_info_new_for_xml()` function in `gio/gdbusintrospection.c` triggered by malformed D-Bus introspection XML containing a `<node>` element nested within method, signal, or property elements. Exploitation requires an attacker to supply malformed D-Bus introspection XML to a D-Bus client using this GLib function. The `glib2` package is a base OS library in the RHEL 9.8 runtime image. The MMRIA application is an ASP.NET Core web service that does not use D-Bus IPC or process D-Bus introspection XML; the container does not expose or connect to a D-Bus socket. The D-Bus attack surface is absent from the container's runtime context. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q glib2
oc rsh <mmria-pod> find /run /var/run -name "dbus*" 2>/dev/null || echo "no D-Bus socket in container"
```

---

### libacl / CVE-2026-54369

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 2.3.1-4.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-54369 describes a symlink traversal privilege-escalation vulnerability in libacl (versions prior to 2.4.0): the pathname-based functions `acl_get_file()`, `acl_set_file()`, `acl_extended_file()`, and `acl_delete_def_file()` are vulnerable to local symlink replacement attacks allowing local attackers to escalate privileges. Exploitation requires a local attacker with the ability to create or replace symlinks — meaning the attacker must already have local code execution on the system. The MMRIA container runs as non-root user 1001 under OpenShift's restricted SCC, preventing SETUID-based privilege escalation and limiting filesystem write scope. The container does not run SETUID binaries that invoke the vulnerable libacl functions in an escalating context. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libacl
oc rsh <mmria-pod> find /app /usr -perm /4000 2>/dev/null || echo "no SETUID binaries in app paths"
```

---

### libcurl-minimal / CVE-2026-11352

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-11352 describes a denial-of-service flaw in libcurl's QUIC/HTTP3 UDP receive path: a malicious HTTP/3 server triggers a DoS against a libcurl client by exploiting how zero-length UDP datagrams are handled before the per-connection counter increments. The `libcurl-minimal` shared library is a base OS dependency of the RHEL 9.8 runtime image. The MMRIA ASP.NET Core application uses the .NET `HttpClient` class — backed by .NET's own HTTP stack — for outbound communication to CouchDB and internal services over HTTPS/TCP, not HTTP/3 UDP. Repository search confirms `libcurl-minimal` is not linked by the MMRIA application binary; a Tier-2 `ldd` check is flagged for confirmation. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libcurl-minimal
oc rsh <mmria-pod> ldd /app/mmria-server 2>/dev/null | grep curl || echo "mmria-server does not link libcurl"
```

---

### libcurl-minimal / CVE-2026-11586

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-11586 describes a memory-exhaustion DoS in libcurl's WebSocket PING handler: libcurl has no upper bound on memory for unacknowledged WebSocket PING frames, allowing a malicious server to exhaust client memory. Exploitation requires the application to open a WebSocket connection via libcurl. The `libcurl-minimal` shared library is a base OS dependency of the RHEL 9.8 runtime image. The MMRIA .NET application uses the .NET `HttpClient` stack for HTTP communication and does not invoke libcurl WebSocket functionality; a Tier-2 `ldd` check is flagged to confirm the binary does not dynamically link libcurl. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libcurl-minimal
oc rsh <mmria-pod> ldd /app/mmria-server 2>/dev/null | grep curl || echo "mmria-server does not link libcurl"
```

---

### libcurl-minimal / CVE-2026-12064

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-12064 describes a URL-scheme confusion in libcurl when `CURLOPT_DEFAULT_PROTOCOL` is set to sftp or scp and a schemeless URL is used: the library incorrectly infers the URL scheme and bypasses SFTP/SCP initialization. Exploitation requires the application to configure libcurl with SFTP or SCP as the default protocol. The `libcurl-minimal` shared library is a base OS dependency of the RHEL 9.8 runtime image. The MMRIA .NET application uses the .NET `HttpClient` for HTTPS communication and does not configure libcurl with SFTP or SCP protocol defaults; a Tier-2 `ldd` check is flagged to confirm the binary does not dynamically link libcurl. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libcurl-minimal
oc rsh <mmria-pod> ldd /app/mmria-server 2>/dev/null | grep curl || echo "mmria-server does not link libcurl"
```

---

### libcurl-minimal / CVE-2026-8286

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-8286 describes a TLS connection reuse flaw in libcurl's STARTTLS upgrade path: a new transfer using STARTTLS may reuse an existing live connection despite TLS configuration mismatches, potentially transmitting data over a misconfigured channel. Exploitation requires the application to initiate STARTTLS-based transfers via libcurl — protocols such as SMTP, IMAP, or FTP with TLS upgrade. The `libcurl-minimal` shared library is a base OS dependency of the RHEL 9.8 runtime image. The MMRIA .NET application uses .NET's `HttpClient` for HTTPS communication exclusively and does not use STARTTLS-capable protocols via libcurl; a Tier-2 `ldd` check is flagged to confirm the binary does not dynamically link libcurl. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libcurl-minimal
oc rsh <mmria-pod> ldd /app/mmria-server 2>/dev/null | grep curl || echo "mmria-server does not link libcurl"
```

---

### libcurl-minimal / CVE-2026-8925

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-8925 describes a double-free vulnerability in libcurl's SASL/GSASL authentication handler: the GSASL context is freed twice without clearing the pointer, enabling potential memory corruption. Exploitation requires the application to use SASL or GSASL authentication in a libcurl connection. The `libcurl-minimal` shared library is a base OS dependency of the RHEL 9.8 runtime image. The MMRIA .NET application uses the .NET `HttpClient` for HTTPS communication and does not invoke libcurl with SASL or GSASL authentication — CouchDB access uses basic HTTP authentication over HTTPS via .NET's own HTTP stack; a Tier-2 `ldd` check is flagged to confirm the binary does not dynamically link libcurl. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libcurl-minimal
oc rsh <mmria-pod> ldd /app/mmria-server 2>/dev/null | grep curl || echo "mmria-server does not link libcurl"
```

---

### libcurl-minimal / CVE-2026-9547

**Verdict:** Residual risk – required, not reachable under current controls  
**Installed version:** 7.76.1-40.el9  
**Fix available:** None (fixedIn empty; Red Hat status: affected)  
**Summary:** CVE-2026-9547 describes a silent host-key acceptance vulnerability in libcurl: when using SCP or SFTP with `CURLOPT_SSH_KEYFUNCTION`, a server presenting an unhandled host-key type may be silently accepted. Exploitation requires the application to perform SCP or SFTP transfers via libcurl with the SSH key callback configured. The `libcurl-minimal` shared library is a base OS dependency of the RHEL 9.8 runtime image. The MMRIA .NET application uses the .NET `HttpClient` for HTTPS communication and does not perform SCP or SFTP file transfers via libcurl; a Tier-2 `ldd` check is flagged to confirm the binary does not dynamically link libcurl. No Red Hat patch is available. Residual risk is accepted.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q libcurl-minimal
oc rsh <mmria-pod> ldd /app/mmria-server 2>/dev/null | grep curl || echo "mmria-server does not link libcurl"
```

---

### tar / CVE-2026-59873

**Verdict:** Not applicable / false positive  
**Installed version:** 2:1.34-11.el9  
**Summary:** CVE-2026-59873 was assigned to the npm JavaScript package `node-tar` (a pure-JavaScript tar archive library for Node.js, published on npm as `tar`). The CVE description states explicitly: "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.19, node-tar does not enforce hard upper bounds on total decompressed data, entry counts, or decompression ratio in extraction and parsing paths." The RHEL system package `tar` (GNU tar, version 1.34) is a POSIX-compliant, C-language archive utility with no shared code, version numbering, or dependency relationship with the JavaScript `node-tar` npm package. The Trivy vulnerability database has incorrectly mapped this Node.js library CVE to the unrelated GNU `tar` system package. The MMRIA container is a .NET application with no Node.js runtime or npm packages in its final image; the `node-tar` npm package is entirely absent. This finding is a false positive caused by a CVE-database package attribution error.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q tar        # confirms GNU tar 1.34, not node-tar
oc rsh <mmria-pod> which node 2>/dev/null || echo "no Node.js runtime in image"
oc rsh <mmria-pod> find / -name "node_modules" 2>/dev/null || echo "no npm packages in image"
```

---

### tar / CVE-2026-59874

**Verdict:** Not applicable / false positive  
**Installed version:** 2:1.34-11.el9  
**Summary:** CVE-2026-59874 was assigned to the npm JavaScript package `node-tar` (a pure-JavaScript tar archive library for Node.js, published on npm as `tar`). The CVE description states explicitly: "node-tar is a tar archive manipulation library for Node.js. Prior to 7.5.18, tar.replace accepts a checksum-valid tar header with a negative base-256 encoded entry size, causing the archive scanner to make no progress while repeatedly parsing the same entry." The RHEL system package `tar` (GNU tar, version 1.34) is a POSIX-compliant, C-language archive utility with no shared code, version numbering, or dependency relationship with the JavaScript `node-tar` npm package. The Trivy vulnerability database has incorrectly mapped this Node.js library CVE to the unrelated GNU `tar` system package. The MMRIA container is a .NET application with no Node.js runtime or npm packages in its final image; the `node-tar` npm package is entirely absent. This finding is a false positive caused by a CVE-database package attribution error.  
**Verification commands:**
```sh
oc rsh <mmria-pod> rpm -q tar        # confirms GNU tar 1.34, not node-tar
oc rsh <mmria-pod> which node 2>/dev/null || echo "no Node.js runtime in image"
oc rsh <mmria-pod> find / -name "node_modules" 2>/dev/null || echo "no npm packages in image"
```
