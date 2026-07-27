## Scan: 2026-07-27 | Scan ID 31010 | Commit 35f51d1c4c5050d9c9c16a0fd05eaa3aa4c0189b

## Finding 1 — JSON Hijacking Possible
**SSC Issue ID:** 2237682  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/version/26.06.15/metadata  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `grep -nE '\[AllowAnonymous\]|\[Route\("\{version_specification_id\}/\{document_name\}"\)' source-code/mmria/mmria-server/Controllers/api/versionController.cs`
- Output:
  - `67:    [AllowAnonymous]`
  - `87:    [AllowAnonymous]`
  - `107:    [AllowAnonymous]`
  - `115:    [AllowAnonymous]`
  - `135:    [AllowAnonymous]`
  - `157:    [AllowAnonymous]`
  - `159:    [Route("{version_specification_id}/{document_name}")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/versionController.cs` lines 154-191.

### Verdict rationale
The flagged endpoint is an intentionally anonymous metadata/version document endpoint (`[AllowAnonymous]`) serving public app configuration data (metadata/UI specification), not authenticated session or PHI payloads. JSON hijacking requires sensitive authenticated data in a script-readable GET response; this endpoint is designed for public bootstrap/version retrieval and does not expose user-bound secrets.

### SWA Summary
False positive for sensitive-data exposure: endpoint is intentionally anonymous and serves public metadata artifacts used by the client bootstrap flow.

## Finding 2 — JSON Hijacking Possible
**SSC Issue ID:** 2237684  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/metadata  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `grep -nE '\[AllowAnonymous\]|\[Route\("\{id\}"\)|return Content\(json_string, "application/json"\);' source-code/mmria/mmria-server/Controllers/api/metadataController.cs`
- Output:
  - `37:    [AllowAnonymous]`
  - `56:        return Content(json_string, "application/json");`
  - `60:    [AllowAnonymous]`
  - `61:    [Route("{id}")]`
  - `80:        return Content(json_string, "application/json");`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/metadataController.cs` lines 34-82.

### Verdict rationale
`/api/metadata` is explicitly anonymous and returns application metadata JSON content, not user/session-bound records. The route and return path show a public metadata payload for app operation. Because no authenticated sensitive object is exposed, the JSON hijacking exploit precondition is not met for this endpoint.

### SWA Summary
False positive for sensitive-data interception: anonymous metadata endpoint returns public application metadata only.

## Finding 3 — JSON Hijacking Possible
**SSC Issue ID:** 2237686  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/version/26.06.15/ui_specification  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `grep -nE '\[AllowAnonymous\]|\[Route\("\{version_specification_id\}/\{document_name\}"\)|case "ui_specification"' source-code/mmria/mmria-server/Controllers/api/versionController.cs`
- Output:
  - `157:    [AllowAnonymous]`
  - `159:    [Route("{version_specification_id}/{document_name}")]`
  - `173:                case "ui_specification":`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/versionController.cs` lines 154-191.

### Verdict rationale
The finding URL maps to the anonymous version-document route that serves metadata artifacts (`ui_specification`) and marks them as JSON. The endpoint is not a user/session data endpoint; it is configuration content required for client rendering. Without sensitive authenticated payload disclosure, JSON hijacking impact does not apply.

### SWA Summary
False positive for data theft risk: endpoint serves anonymous UI specification artifacts rather than user-confidential data.

## Finding 4 — JSON Hijacking Possible
**SSC Issue ID:** 2237693  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/metadata/version_specification-26.06.15  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `grep -nE '\[AllowAnonymous\]|\[Route\("\{id\}"\)|GetMetadataAsync\(id, db_config\)' source-code/mmria/mmria-server/Controllers/api/metadataController.cs`
- Output:
  - `60:    [AllowAnonymous]`
  - `61:    [Route("{id}")]`
  - `70:            json_result = await _metadataVersionManager.GetMetadataAsync(id, db_config);`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/metadataController.cs` lines 60-80.

### Verdict rationale
This finding resolves to the anonymous metadata-by-id route and fetches metadata version documents. The handler is not tied to user/session context and does not process or expose authenticated case data. The scanner condition is present (JSON GET), but the exploit condition of sensitive authenticated disclosure is absent.

### SWA Summary
False positive for confidentiality impact: anonymous metadata-by-id endpoint provides versioned metadata artifacts, not sensitive user data.

## Finding 5 — Backup file (_old)
**SSC Issue ID:** 2238517  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/OfflineCase/active-user-session._old  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `find source-code/mmria/mmria-server/wwwroot -type f \( -name '*._old' -o -name '*._bak' -o -name '*._backup' -o -name '*.zip' -o -name '*.tar' -o -name '*.~' \)`
- Output: `(no matches)`
- Command: `grep -nE '\[Route\("api/\[controller\]"\)|\[HttpGet\("\{userId\}"\)\]|\[HttpGet\("active-user-session"\)\]' source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
- Output:
  - `19:[Route("api/[controller]")]`
  - `106:    [HttpGet("{userId}")]`
  - `147:    [HttpGet("active-user-session")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs` lines 96-170.

### Verdict rationale
No backup artifact exists in webroot. The URL is an API path under controller routing, and WebInspect reached a `200` through API route matching (`{userId}` pattern), not by retrieving a filesystem backup file. This finding does not represent exposed backup content in webroot.

### SWA Summary
False positive for backup-file exposure: request hit routed API logic, not a static backup artifact.

## Finding 6 — Archived File (appended .zip)
**SSC Issue ID:** 2238518  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/OfflineCase/active-user-session.zip  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `find source-code/mmria/mmria-server/wwwroot -type f \( -name '*._old' -o -name '*._bak' -o -name '*._backup' -o -name '*.zip' -o -name '*.tar' -o -name '*.~' \)`
- Output: `(no matches)`
- Command: `find . -type f -name 'active-user-session*'`
- Output: `(no matches)`
- Command: `grep -nE '\[HttpGet\("\{userId\}"\)\]|\[HttpGet\("active-user-session"\)\]' source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
- Output:
  - `106:    [HttpGet("{userId}")]`
  - `147:    [HttpGet("active-user-session")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs` lines 96-170.

### Verdict rationale
No `.zip` artifact exists in repository webroot or as a server file named `active-user-session*`. The scan URL is handled through API route matching and does not indicate static archive exposure.

### SWA Summary
False positive for archive-file exposure: no `.zip` file exists; `200` response originated from controller route handling.

## Finding 7 — Backup file (_bak)
**SSC Issue ID:** 2238519  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/OfflineCase/active-user-session._bak  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `find source-code/mmria/mmria-server/wwwroot -type f \( -name '*._old' -o -name '*._bak' -o -name '*._backup' -o -name '*.zip' -o -name '*.tar' -o -name '*.~' \)`
- Output: `(no matches)`
- Command: `grep -nE '\[Route\("api/\[controller\]"\)|\[HttpGet\("\{userId\}"\)' source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
- Output:
  - `19:[Route("api/[controller]")]`
  - `106:    [HttpGet("{userId}")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs` lines 96-170.

### Verdict rationale
Repository inspection shows no `_bak` backup files in the served webroot. The scanned path is API-controller routed under `/api/OfflineCase` and was not a static backup file retrieval.

### SWA Summary
False positive for backup-file disclosure: no `_bak` artifacts are present in webroot; scanner matched an API route.

## Finding 8 — Backup file (_backup)
**SSC Issue ID:** 2238520  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/OfflineCase/active-user-session._backup  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `find source-code/mmria/mmria-server/wwwroot -type f \( -name '*._old' -o -name '*._bak' -o -name '*._backup' -o -name '*.zip' -o -name '*.tar' -o -name '*.~' \)`
- Output: `(no matches)`
- Command: `grep -nE '\[HttpGet\("\{userId\}"\)\]|\[HttpGet\("active-user-session"\)\]' source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
- Output:
  - `106:    [HttpGet("{userId}")]`
  - `147:    [HttpGet("active-user-session")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs` lines 96-170.

### Verdict rationale
No `_backup` files are present in served static content paths. The URL is within a routed API namespace and reaches controller logic via route matching, not a backup file in webroot.

### SWA Summary
False positive for backup-file exposure: no `_backup` file exists; API routing generated the observed response.

## Finding 9 — Archived File (appended .tar)
**SSC Issue ID:** 2238521  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/OfflineCase/active-user-session.tar  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `find source-code/mmria/mmria-server/wwwroot -type f \( -name '*._old' -o -name '*._bak' -o -name '*._backup' -o -name '*.zip' -o -name '*.tar' -o -name '*.~' \)`
- Output: `(no matches)`
- Command: `find . -type f -name 'active-user-session*'`
- Output: `(no matches)`
- Command: `grep -nE '\[Route\("api/\[controller\]"\)|\[HttpGet\("\{userId\}"\)' source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
- Output:
  - `19:[Route("api/[controller]")]`
  - `106:    [HttpGet("{userId}")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs` lines 96-170.

### Verdict rationale
No `.tar` archive is present in static content or repository files named `active-user-session*`. The observed 200 was produced by API route handling under `/api/OfflineCase`, not by exposing an archived file.

### SWA Summary
False positive for archive leakage: no `.tar` resource exists in webroot; scanner request was routed to API logic.

## Finding 10 — Backup file (~)
**SSC Issue ID:** 2238522  
**URL:** https://fl-mmria.apps.ecpaas-dev.cdc.gov:443/api/OfflineCase/active-user-session.~  
**Verdict:** Not applicable / false positive

### Evidence
- Command: `find source-code/mmria/mmria-server/wwwroot -type f \( -name '*._old' -o -name '*._bak' -o -name '*._backup' -o -name '*.zip' -o -name '*.tar' -o -name '*.~' \)`
- Output: `(no matches)`
- Command: `grep -nE '\[HttpGet\("\{userId\}"\)\]|\[HttpGet\("active-user-session"\)\]' source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs`
- Output:
  - `106:    [HttpGet("{userId}")]`
  - `147:    [HttpGet("active-user-session")]`
- Files opened and line ranges reviewed:
  - `source-code/mmria/mmria-server/Controllers/api/OfflineCaseController.cs` lines 96-170.

### Verdict rationale
No tilde-suffixed backup artifacts are present in webroot. The scanner URL remained inside API route space and reached a controller route pattern instead of retrieving a backup file.

### SWA Summary
False positive for temp/backup artifact exposure: there is no `.~` file in webroot; response came from route processing.
