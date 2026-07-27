## Scan: 31000 — NCCD_Maternal Mortality Review Information App @ 1a75c40a

- **Scan ID:** 31000
- **Commit:** 1a75c40a3c8e85a9b2ada53141dca2cd3409687c
- **Service:** NCCD_Maternal Mortality Review Information App
- **Platform:** ARO (OpenShift)
- **SSC Application Version ID:** 10291
- **Scan date:** 2026-07-27
- **Findings in scope:** 7 (C:0 H:0 M:7)

---

## Finding 1 — JSON Hijacking Possible

**SSC Issue ID:** 2237682
**Instance ID:** 1d574ed4-7e3c-38b2-0828-badb6c515730
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/version/26.06.15/metadata`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
grep -rEn 'ssl_(certificate|protocols|ciphers)|tls:|certificate' . --include="*.conf" --include="*.yaml" --include="*.yml"
→ (no matches)

find . -name 'route.yaml' -o -name 'route.yml' -o -name '*ingress*.yaml'
→ (no matches)

find . -name 'nginx.conf'
→ (no matches)

find . -name "*.asp"
→ (no matches)
```

**Response headers observed at scan time (from finding object):**

```json
{
  "server": "Kestrel",
  "cache-control": "no-cache, no-store",
  "x-frame-options": "DENY",
  "content-security-policy": "frame-ancestors  'none'",
  "x-content-type-options": "nosniff",
  "x-xss-protection": "1; mode=block"
}
```

**Code location:** Security headers are set in `source-code/mmria/mmria-server/Program.cs` lines 920–924 via the global request middleware:

```csharp
context.Response.Headers.Append("X-Frame-Options", "DENY");
context.Response.Headers.Append("Content-Security-Policy","frame-ancestors  'none'");
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
```

**Endpoint controller:** `source-code/mmria/mmria-server/Controllers/api/versionController.cs` lines 157–191 — `Get_Version_Document(string version_specification_id, string document_name)` serves this path as a `FileResult` with `Content-Type: application/json`.

**WebInspect execution field:** empty — WebInspect recorded no actual exploitation steps.

### Verdict rationale

The JSON hijacking attack (CWE-346) relies on two mechanisms that are both blocked here:

1. **`<script src>` cross-origin vector** — requires the browser to execute the JSON response as JavaScript. The `X-Content-Type-Options: nosniff` header (confirmed in scan-time response headers above and set at `Program.cs:922`) instructs browsers to reject MIME-type sniffing. A response served as `application/json` cannot be loaded via `<script src>` in any browser that honours this header (all modern browsers since WHATWG Fetch specification adoption). This directly removes the exploit path.

2. **ES3-era constructor-override vector** — relied on overriding `Array.prototype.__defineSetter__` or the `Object` constructor via a cross-origin `<script>` include. ECMAScript 5 (2009) made `Array` and `Object` non-configurable, eliminating this attack in all currently supported browsers.

Additionally, `X-Frame-Options: DENY` and `Content-Security-Policy: frame-ancestors 'none'` prevent the JSON endpoint from being framed, removing any iframe-based cross-origin access vector. WebInspect's `execution` field is empty, confirming no actual exploitation was demonstrated. The data returned (application form-version document metadata — schema definitions and field structures, not user data) is low sensitivity even if accessed.

### SWA Summary

False positive. The WebInspect scanner flagged `/api/version/26.06.15/metadata` because it responds to an authenticated GET with a JSON body. The `X-Content-Type-Options: nosniff` header (confirmed present in scan-time response headers and set at `source-code/mmria/mmria-server/Program.cs:922`) blocks the `<script src>` cross-origin execution vector that underlies JSON hijacking. ES5+ browsers neutralise the constructor-override vector. WebInspect recorded no exploit steps (`execution` field empty). No code changes required.

---

## Finding 2 — JSON Hijacking Possible

**SSC Issue ID:** 2237684
**Instance ID:** 469e88f8-5ff8-0d33-026d-45a3679f3776
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/metadata`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
grep -rEn 'ssl_(certificate|protocols|ciphers)|tls:|certificate' . --include="*.conf" --include="*.yaml" --include="*.yml"
→ (no matches)

find . -name 'route.yaml' -o -name 'route.yml' -o -name '*ingress*.yaml'
→ (no matches)

find . -name 'nginx.conf'
→ (no matches)

find . -name "*.asp"
→ (no matches)
```

**Response headers observed at scan time (from finding object):**

```json
{
  "server": "Kestrel",
  "cache-control": "no-cache, no-store",
  "x-frame-options": "DENY",
  "content-security-policy": "frame-ancestors  'none'",
  "x-content-type-options": "nosniff",
  "x-xss-protection": "1; mode=block"
}
```

**Code location:** Security headers are set in `source-code/mmria/mmria-server/Program.cs` lines 920–924 via the global request middleware:

```csharp
context.Response.Headers.Append("X-Frame-Options", "DENY");
context.Response.Headers.Append("Content-Security-Policy","frame-ancestors  'none'");
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
```

**Endpoint controller:** `source-code/mmria/mmria-server/Controllers/api/metadataController.cs` lines 37–57 — `[AllowAnonymous] [HttpGet]` `Get()` returns the application metadata document serialised as `application/json`.

**WebInspect execution field:** empty — WebInspect recorded no actual exploitation steps.

### Verdict rationale

The JSON hijacking attack (CWE-346) relies on two mechanisms that are both blocked here:

1. **`<script src>` cross-origin vector** — requires the browser to execute the JSON response as JavaScript. The `X-Content-Type-Options: nosniff` header (confirmed in scan-time response headers above and set at `Program.cs:922`) instructs browsers to reject MIME-type sniffing. A response served as `application/json` cannot be loaded via `<script src>` in any browser that honours this header (all modern browsers since WHATWG Fetch specification adoption). This directly removes the exploit path.

2. **ES3-era constructor-override vector** — relied on overriding `Array.prototype.__defineSetter__` or the `Object` constructor via a cross-origin `<script>` include. ECMAScript 5 (2009) made `Array` and `Object` non-configurable, eliminating this attack in all currently supported browsers.

Additionally, `X-Frame-Options: DENY` and `Content-Security-Policy: frame-ancestors 'none'` prevent the JSON endpoint from being framed, removing any iframe-based cross-origin access vector. WebInspect's `execution` field is empty, confirming no actual exploitation was demonstrated. The data returned (application form metadata — field definitions, OMB expiry date, form structure — not user data) is low sensitivity.

### SWA Summary

False positive. The WebInspect scanner flagged `/api/metadata` because it responds to an anonymous GET with a JSON body. The `X-Content-Type-Options: nosniff` header (confirmed present in scan-time response headers and set at `source-code/mmria/mmria-server/Program.cs:922`) blocks the `<script src>` cross-origin execution vector that underlies JSON hijacking. ES5+ browsers neutralise the constructor-override vector. WebInspect recorded no exploit steps (`execution` field empty). No code changes required.

---

## Finding 3 — JSON Hijacking Possible

**SSC Issue ID:** 2237686
**Instance ID:** 5d8b1fd5-ac90-0f72-f87b-e0aed56ab6da
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/version/26.06.15/ui_specification`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
grep -rEn 'ssl_(certificate|protocols|ciphers)|tls:|certificate' . --include="*.conf" --include="*.yaml" --include="*.yml"
→ (no matches)

find . -name 'route.yaml' -o -name 'route.yml' -o -name '*ingress*.yaml'
→ (no matches)

find . -name 'nginx.conf'
→ (no matches)

find . -name "*.asp"
→ (no matches)
```

**Response headers observed at scan time (from finding object):**

```json
{
  "server": "Kestrel",
  "cache-control": "no-cache, no-store",
  "x-frame-options": "DENY",
  "content-security-policy": "frame-ancestors  'none'",
  "x-content-type-options": "nosniff",
  "x-xss-protection": "1; mode=block"
}
```

**Code location:** Security headers are set in `source-code/mmria/mmria-server/Program.cs` lines 920–924 via the global request middleware:

```csharp
context.Response.Headers.Append("X-Frame-Options", "DENY");
context.Response.Headers.Append("Content-Security-Policy","frame-ancestors  'none'");
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
```

**Endpoint controller:** `source-code/mmria/mmria-server/Controllers/api/versionController.cs` lines 157–191 — `Get_Version_Document(version_specification_id="26.06.15", document_name="ui_specification")` serves a `Content-Type: application/json` `FileResult`.

**WebInspect execution field:** empty — WebInspect recorded no actual exploitation steps.

### Verdict rationale

The JSON hijacking attack (CWE-346) relies on two mechanisms that are both blocked here:

1. **`<script src>` cross-origin vector** — requires the browser to execute the JSON response as JavaScript. The `X-Content-Type-Options: nosniff` header (confirmed in scan-time response headers above and set at `Program.cs:922`) instructs browsers to reject MIME-type sniffing. A response served as `application/json` cannot be loaded via `<script src>` in any browser that honours this header (all modern browsers since WHATWG Fetch specification adoption). This directly removes the exploit path.

2. **ES3-era constructor-override vector** — relied on overriding `Array.prototype.__defineSetter__` or the `Object` constructor via a cross-origin `<script>` include. ECMAScript 5 (2009) made `Array` and `Object` non-configurable, eliminating this attack in all currently supported browsers.

Additionally, `X-Frame-Options: DENY` and `Content-Security-Policy: frame-ancestors 'none'` prevent the JSON endpoint from being framed, removing any iframe-based cross-origin access vector. WebInspect's `execution` field is empty, confirming no actual exploitation was demonstrated. The data returned (UI specification document — display layout definitions for form version 26.06.15, not user data) is low sensitivity.

### SWA Summary

False positive. The WebInspect scanner flagged `/api/version/26.06.15/ui_specification` because it responds to a GET with a JSON body. The `X-Content-Type-Options: nosniff` header (confirmed present in scan-time response headers and set at `source-code/mmria/mmria-server/Program.cs:922`) blocks the `<script src>` cross-origin execution vector that underlies JSON hijacking. ES5+ browsers neutralise the constructor-override vector. WebInspect recorded no exploit steps (`execution` field empty). No code changes required.

---

## Finding 4 — JSON Hijacking Possible

**SSC Issue ID:** 2237693
**Instance ID:** 7cb0d1e1-8fec-46b0-b170-bb6221eedab0
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/metadata/version_specification-26.06.15`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
grep -rEn 'ssl_(certificate|protocols|ciphers)|tls:|certificate' . --include="*.conf" --include="*.yaml" --include="*.yml"
→ (no matches)

find . -name 'route.yaml' -o -name 'route.yml' -o -name '*ingress*.yaml'
→ (no matches)

find . -name 'nginx.conf'
→ (no matches)

find . -name "*.asp"
→ (no matches)
```

**Response headers observed at scan time (from finding object):**

```json
{
  "server": "Kestrel",
  "cache-control": "no-cache, no-store",
  "x-frame-options": "DENY",
  "content-security-policy": "frame-ancestors  'none'",
  "x-content-type-options": "nosniff",
  "x-xss-protection": "1; mode=block"
}
```

**Code location:** Security headers are set in `source-code/mmria/mmria-server/Program.cs` lines 920–924 via the global request middleware:

```csharp
context.Response.Headers.Append("X-Frame-Options", "DENY");
context.Response.Headers.Append("Content-Security-Policy","frame-ancestors  'none'");
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
```

**Endpoint controller:** `source-code/mmria/mmria-server/Controllers/api/metadataController.cs` lines 60–81 — `[AllowAnonymous] [HttpGet] [Route("{id}")]` `Get(string id)` with `id="version_specification-26.06.15"` returns the version specification serialised as `application/json`.

**WebInspect execution field:** empty — WebInspect recorded no actual exploitation steps.

### Verdict rationale

The JSON hijacking attack (CWE-346) relies on two mechanisms that are both blocked here:

1. **`<script src>` cross-origin vector** — requires the browser to execute the JSON response as JavaScript. The `X-Content-Type-Options: nosniff` header (confirmed in scan-time response headers above and set at `Program.cs:922`) instructs browsers to reject MIME-type sniffing. A response served as `application/json` cannot be loaded via `<script src>` in any browser that honours this header (all modern browsers since WHATWG Fetch specification adoption). This directly removes the exploit path.

2. **ES3-era constructor-override vector** — relied on overriding `Array.prototype.__defineSetter__` or the `Object` constructor via a cross-origin `<script>` include. ECMAScript 5 (2009) made `Array` and `Object` non-configurable, eliminating this attack in all currently supported browsers.

Additionally, `X-Frame-Options: DENY` and `Content-Security-Policy: frame-ancestors 'none'` prevent the JSON endpoint from being framed, removing any iframe-based cross-origin access vector. WebInspect's `execution` field is empty, confirming no actual exploitation was demonstrated. The data returned (version specification document — metadata schema and form structure for version 26.06.15, not user data) is low sensitivity.

### SWA Summary

False positive. The WebInspect scanner flagged `/api/metadata/version_specification-26.06.15` because it responds to an anonymous GET with a JSON body. The `X-Content-Type-Options: nosniff` header (confirmed present in scan-time response headers and set at `source-code/mmria/mmria-server/Program.cs:922`) blocks the `<script src>` cross-origin execution vector that underlies JSON hijacking. ES5+ browsers neutralise the constructor-override vector. WebInspect recorded no exploit steps (`execution` field empty). No code changes required.

---

## Finding 5 — Source Code Viewing Example Application

**SSC Issue ID:** 1966967
**Instance ID:** 1d87d592-51ba-75d6-c505-2f5eab2bdd87
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/scripts/viewcode.asp?file=index.asp`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
find . -name "*.asp"
→ (no matches)

grep -rn "viewcode" . --include="*.cs" --include="*.js" --include="*.html" --include="*.json"
→ (no matches)

cat source-code/mmria/mmria-server/Dockerfile | grep -i 'iis\|asp\|classic'
→ (no matches)
```

**HTTP response status at scan time:** `503` (Service Unavailable) — the server returned a non-200 error code. The URL did not return content.

**Server technology:** The `server` response header at other scanned paths shows `Kestrel` — the ASP.NET Core web server. Classic ASP (`.asp` files) requires IIS with the classic ASP ISAPI extension and is incompatible with Kestrel. No IIS configuration, web.config, or classic ASP handler mapping exists anywhere in this repository.

**Dockerfile review:** `source-code/mmria/mmria-server/Dockerfile` builds a multi-stage .NET 10 container using a Red Hat UBI dotnet image and publishes to a Kestrel runtime image. No IIS or classic ASP components are installed or referenced.

**Path structure:** The probed path `/scripts/viewcode.asp` is a standard WebInspect fingerprint probe for legacy IIS 5.0/6.0 example application files (`viewsource.asp`, `viewcode.asp`). No such file exists in `source-code/mmria/mmria-server/wwwroot/scripts/` or anywhere in the repository; the directory contains only JavaScript (`.js`) files.

### Verdict rationale

WebInspect probed well-known legacy IIS/classic ASP example file paths (`/scripts/viewcode.asp`, `/scripts/asp/samples/viewcode.asp`, `/scripts/samples/asp/viewcode.asp`) as a routine scanner fingerprint. This application uses ASP.NET Core on Kestrel — classic ASP is not supported and no `.asp` files are present in the repository (confirmed by `find . -name "*.asp"` returning no matches). The HTTP response code was 503 (not 200), confirming the server did not serve the probed resource. This finding does not correspond to an exploitable condition.

### SWA Summary

False positive. WebInspect probed `/scripts/viewcode.asp?file=index.asp` — a legacy IIS 5.0/6.0 classic ASP example file path — against this ASP.NET Core/Kestrel application. No `.asp` files exist in the repository (`find . -name "*.asp"` returns no matches). The response code was 503 (not 200), confirming the path does not serve any content. Classic ASP is not supported on Kestrel. No code changes required.

---

## Finding 6 — Source Code Viewing Example Application

**SSC Issue ID:** 1966968
**Instance ID:** 21ce9b1b-a664-bc0a-1f46-47fe48785f6f
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/scripts/asp/samples/viewcode.asp?file=index.asp`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
find . -name "*.asp"
→ (no matches)

grep -rn "viewcode" . --include="*.cs" --include="*.js" --include="*.html" --include="*.json"
→ (no matches)

cat source-code/mmria/mmria-server/Dockerfile | grep -i 'iis\|asp\|classic'
→ (no matches)
```

**HTTP response status at scan time:** `503` (Service Unavailable) — the server returned a non-200 error code. The URL did not return content.

**Server technology:** The `server` response header at other scanned paths shows `Kestrel` — the ASP.NET Core web server. Classic ASP (`.asp` files) requires IIS with the classic ASP ISAPI extension and is incompatible with Kestrel. No IIS configuration, web.config, or classic ASP handler mapping exists anywhere in this repository.

**Dockerfile review:** `source-code/mmria/mmria-server/Dockerfile` builds a multi-stage .NET 10 container using a Red Hat UBI dotnet image and publishes to a Kestrel runtime image. No IIS or classic ASP components are installed or referenced.

**Path structure:** The probed path `/scripts/asp/samples/viewcode.asp` is a standard WebInspect fingerprint probe for legacy IIS example application files. No such file exists in `source-code/mmria/mmria-server/wwwroot/scripts/` or anywhere in the repository; the directory contains only JavaScript (`.js`) files.

### Verdict rationale

WebInspect probed the well-known legacy IIS/classic ASP path `/scripts/asp/samples/viewcode.asp` as a routine scanner fingerprint. This application uses ASP.NET Core on Kestrel — classic ASP is not supported and no `.asp` files are present in the repository (confirmed by `find . -name "*.asp"` returning no matches). The HTTP response code was 503 (not 200), confirming the server did not serve the probed resource. The path `/scripts/asp/samples/` does not exist in `wwwroot`. This finding does not correspond to an exploitable condition.

### SWA Summary

False positive. WebInspect probed `/scripts/asp/samples/viewcode.asp?file=index.asp` — a legacy IIS classic ASP example path — against this ASP.NET Core/Kestrel application. No `.asp` files exist in the repository (`find . -name "*.asp"` returns no matches). The response code was 503 (not 200), confirming the path does not serve any content. Classic ASP is not supported on Kestrel. No code changes required.

---

## Finding 7 — Source Code Viewing Example Application

**SSC Issue ID:** 1966969
**Instance ID:** 118c7feb-5f4a-22a3-687b-d6c0d22e58cd
**Severity:** Medium
**URL:** `https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/scripts/samples/asp/viewcode.asp?file=index.asp`
**Verdict:** Not applicable / false positive

### Evidence

**Search commands run:**

```
find . -name "*.asp"
→ (no matches)

grep -rn "viewcode" . --include="*.cs" --include="*.js" --include="*.html" --include="*.json"
→ (no matches)

cat source-code/mmria/mmria-server/Dockerfile | grep -i 'iis\|asp\|classic'
→ (no matches)
```

**HTTP response status at scan time:** `503` (Service Unavailable) — the server returned a non-200 error code. The URL did not return content.

**Server technology:** The `server` response header at other scanned paths shows `Kestrel` — the ASP.NET Core web server. Classic ASP (`.asp` files) requires IIS with the classic ASP ISAPI extension and is incompatible with Kestrel. No IIS configuration, web.config, or classic ASP handler mapping exists anywhere in this repository.

**Dockerfile review:** `source-code/mmria/mmria-server/Dockerfile` builds a multi-stage .NET 10 container using a Red Hat UBI dotnet image and publishes to a Kestrel runtime image. No IIS or classic ASP components are installed or referenced.

**Path structure:** The probed path `/scripts/samples/asp/viewcode.asp` is a standard WebInspect fingerprint probe for legacy IIS example application files. No such file exists in `source-code/mmria/mmria-server/wwwroot/scripts/` or anywhere in the repository; the directory contains only JavaScript (`.js`) files.

### Verdict rationale

WebInspect probed the well-known legacy IIS/classic ASP path `/scripts/samples/asp/viewcode.asp` as a routine scanner fingerprint. This application uses ASP.NET Core on Kestrel — classic ASP is not supported and no `.asp` files are present in the repository (confirmed by `find . -name "*.asp"` returning no matches). The HTTP response code was 503 (not 200), confirming the server did not serve the probed resource. The path `/scripts/samples/asp/` does not exist in `wwwroot`. This finding does not correspond to an exploitable condition.

### SWA Summary

False positive. WebInspect probed `/scripts/samples/asp/viewcode.asp?file=index.asp` — a legacy IIS classic ASP example path — against this ASP.NET Core/Kestrel application. No `.asp` files exist in the repository (`find . -name "*.asp"` returns no matches). The response code was 503 (not 200), confirming the path does not serve any content. Classic ASP is not supported on Kestrel. No code changes required.

---

## Triage table — Scan 31000

| # | SSC Issue ID | Name | URL | Verdict |
|---|---|---|---|---|
| 1 | 2237682 | JSON Hijacking Possible | `/api/version/26.06.15/metadata` | Not applicable / false positive |
| 2 | 2237684 | JSON Hijacking Possible | `/api/metadata` | Not applicable / false positive |
| 3 | 2237686 | JSON Hijacking Possible | `/api/version/26.06.15/ui_specification` | Not applicable / false positive |
| 4 | 2237693 | JSON Hijacking Possible | `/api/metadata/version_specification-26.06.15` | Not applicable / false positive |
| 5 | 1966967 | Source Code Viewing Example Application | `/scripts/viewcode.asp?file=index.asp` | Not applicable / false positive |
| 6 | 1966968 | Source Code Viewing Example Application | `/scripts/asp/samples/viewcode.asp?file=index.asp` | Not applicable / false positive |
| 7 | 1966969 | Source Code Viewing Example Application | `/scripts/samples/asp/viewcode.asp?file=index.asp` | Not applicable / false positive |
