## Scan: mmria s2i @ abe653b1 (2026-08-21)

- Commit: `abe653b17f6e2331d941b954f1e23b29c1f714df`
- SSC application version: `10291`
- Workflow run: `https://github.com/cdcent/nccdphp-od-devops/actions/runs/32494346607`

## Finding 1 — Log Forging at nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CVS/Manager/CVSManager.cs:87
**SSC Issue ID:** 2245161
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Previous logging included externally-derived values in the log template context at `CVSManager.cs:87`.
- Updated code now logs a static warning message without external payload fields.

### Verdict rationale
Removing externally influenced values from this warning log eliminates CR/LF injection opportunities in this sink.

## Finding 2 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:97
**SSC Issue ID:** 2245158
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Logging statement at `CaseGeocodeController.cs:97` previously included case identifier data in structured log arguments.
- Updated statement is a constant message with no request-derived value in log fields.

### Verdict rationale
The sink no longer writes user-influenced content, preventing log-forging via line-break injection.

## Finding 3 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:119
**SSC Issue ID:** 2245159
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Logging statement at `CaseGeocodeController.cs:119` previously included case identifier data in structured log arguments.
- Updated statement is a constant message with no request-derived value in log fields.

### Verdict rationale
The sink no longer writes user-influenced content, preventing log-forging via line-break injection.

## Finding 4 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:135
**SSC Issue ID:** 2245156
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Logging statement at `CaseGeocodeController.cs:135` previously included case identifier data in structured log arguments.
- Updated statement is a constant message with no request-derived value in log fields.

### Verdict rationale
The sink no longer writes user-influenced content, preventing log-forging via line-break injection.

## Finding 5 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:166
**SSC Issue ID:** 2245157
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Logging statement at `CaseGeocodeController.cs:166` previously included case identifier data in structured log arguments.
- Updated statement is a constant message with no request-derived value in log fields.

### Verdict rationale
The sink no longer writes user-influenced content, preventing log-forging via line-break injection.

## Finding 6 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:189
**SSC Issue ID:** 2245160
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Logging statement at `CaseGeocodeController.cs:189` previously included case identifier data in structured log arguments.
- Updated statement is a constant message with no request-derived value in log fields.

### Verdict rationale
The sink no longer writes user-influenced content, preventing log-forging via line-break injection.

## Finding 7 — Log Forging at source-code/mmria/mmria-server/Controllers/api/CaseGeocodeController.cs:205
**SSC Issue ID:** 2245162
**Rule GUID:** 20ACF15D-2C61-4E85-BFC3-6C1345C42F0D0
**Verdict:** Fixed

### Evidence
- Logging statement at `CaseGeocodeController.cs:205` previously included case identifier data in structured log arguments.
- Updated statement is a constant message with no request-derived value in log fields.

### Verdict rationale
The sink no longer writes user-influenced content, preventing log-forging via line-break injection.
