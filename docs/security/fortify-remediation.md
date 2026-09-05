# Fortify Remediation Records

Records are prepended — newest scan block at the top.

---

## Scan: mmria s2i @ da593970 — 2026-08-20

- **Commit:** `da59397066aae420f419cbb7cc281ed7b0c9f498`
- **Service:** `mmria s2i`
- **SSC application version:** `10291`
- **Workflow run:** `https://github.com/cdcent/nccdphp-od-devops/actions/runs/32411523914`
- **Severity totals:** C:0 H:0 M:7

### Triage summary

| Category | File:Line | Severity | SSC Issue IDs | Verdict | Evidence |
|---|---|---|---|---|---|
| Log Forging | `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/Manager/CVSManager.cs:87` | Medium | `2245161` | Fixed | Removed untrusted structured log properties from warning message. |
| Log Forging | `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:97` | Medium | `2245158` | Fixed | Removed case ID logging from parse-failure warning path. |
| Log Forging | `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:119` | Medium | `2245159` | Fixed | Removed case ID logging from load-failure error path. |
| Log Forging | `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:135` | Medium | `2245156` | Fixed | Removed case ID logging from parse-failure error path. |
| Log Forging | `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:166` | Medium | `2245157` | Fixed | Removed case ID logging from geocode-failure error path. |
| Log Forging | `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:189` | Medium | `2245160` | Fixed | Removed case ID logging from CVS warning path. |
| Log Forging | `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:205` | Medium | `2245162` | Fixed | Removed case ID logging from save-failure error path. |

### Fixes made

- `source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs`
  - Replaced six logger calls that interpolated request-derived case identifiers with static messages.
  - This removes user-controlled data from all reported Fortify sink locations.
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/Manager/CVSManager.cs`
  - Replaced two logger calls that included request/config-derived values (`url`, `year`, geoid fields) with static messages.
  - This removes user-controlled data from the Fortify sink locations in CVS data request error paths.

## Finding 1 — Log Forging at nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/Manager/CVSManager.cs:87

**SSC Issue ID:** `2245161`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogWarning(... "CVS year-list query failed for url={Url}; proceeding with original year={Year}", cvs.cvs_api_url, get_all_data_body.payload.year);`
- Source path: `get_all_data_body.payload.year` is request-derived through `post_payload.year` and `cvs.cvs_api_url` is externalized configuration.
- Sink after fix: `_logger.LogWarning(ex, "CVS year-list query failed; proceeding with original year value.");`

### Verdict rationale
The warning log call no longer writes request- or configuration-derived string values into the log stream.

## Finding 2 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:97

**SSC Issue ID:** `2245158`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogWarning(ex, "case-geocode: failed to parse request body for case {CaseId}", safeCaseId);`
- Source path: `safeCaseId` is derived from route parameter `caseId`.
- Sink after fix: `_logger.LogWarning(ex, "case-geocode: failed to parse request body.");`

### Verdict rationale
The warning log call no longer logs request-derived case identifiers.

## Finding 3 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:119

**SSC Issue ID:** `2245159`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogError(ex, "case-geocode: failed to load case {CaseId}", safeCaseId);`
- Source path: `safeCaseId` is derived from route parameter `caseId`.
- Sink after fix: `_logger.LogError(ex, "case-geocode: failed to load case document.");`

### Verdict rationale
The error log call no longer logs request-derived case identifiers.

## Finding 4 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:135

**SSC Issue ID:** `2245156`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogError(ex, "case-geocode: failed to parse case {CaseId}", safeCaseId);`
- Source path: `safeCaseId` is derived from route parameter `caseId`.
- Sink after fix: `_logger.LogError(ex, "case-geocode: failed to parse case document.");`

### Verdict rationale
The error log call no longer logs request-derived case identifiers.

## Finding 5 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:166

**SSC Issue ID:** `2245157`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogError(ex, "case-geocode: geocode call failed for case {CaseId}", safeCaseId);`
- Source path: `safeCaseId` is derived from route parameter `caseId`.
- Sink after fix: `_logger.LogError(ex, "case-geocode: geocode call failed.");`

### Verdict rationale
The error log call no longer logs request-derived case identifiers.

## Finding 6 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:189

**SSC Issue ID:** `2245160`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogWarning(ex, "case-geocode: CVS lookup failed for case {CaseId} — continuing", safeCaseId);`
- Source path: `safeCaseId` is derived from route parameter `caseId`.
- Sink after fix: `_logger.LogWarning(ex, "case-geocode: CVS lookup failed — continuing.");`

### Verdict rationale
The warning log call no longer logs request-derived case identifiers.

## Finding 7 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:205

**SSC Issue ID:** `2245162`
**Rule GUID:** `20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0`
**Verdict:** Fixed

### Evidence
- Sink before fix: `_logger.LogError(ex, "case-geocode: failed to save case {CaseId}", safeCaseId);`
- Source path: `safeCaseId` is derived from route parameter `caseId`.
- Sink after fix: `_logger.LogError(ex, "case-geocode: failed to save case document.");`

### Verdict rationale
The error log call no longer logs request-derived case identifiers.
