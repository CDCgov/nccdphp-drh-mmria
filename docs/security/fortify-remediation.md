# Fortify Remediation

## Scan: 2026-07-20 — mmria services @ 06fcb937

- **Commit scanned:** `06fcb9375d0bdbf03d0a30808234c05dfc158218`
- **Issue:** `CDCgov/nccdphp-drh-mmria#495`
- **SSC application version:** `12317`
- **Workflow run:** `https://github.com/cdcent/nccdphp-od-devops/actions/runs/29750124299`
- **Findings in scope:** `1`
- **Fixed in this repo:** `1`

## Finding 1 — Mass Assignment: Request Parameters Bound via Input Formatter at nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46

**SSC Issue ID:** `2236287`
**Rule GUID:** `7AA165DE-F8D9-471F-A1E4-562BB552146D`
**Verdict:** Fixed

### Taint path

- **Source (original):** ASP.NET Core input formatter bound the full request body into `SaveSystemOfflineConfigRequest` via `[FromBody]` in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46-47`.
- **Propagation:** The bound request object flowed into the persisted payload when `warn_date`, `warn_message`, `offline_date`, `offline_modal_message`, `offline_page_message`, `apply_to_all_jurisdictions`, `selected_jurisdictions`, `restoration_hours`, and `auto_logout_minutes` were copied into `payload` in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:74-85`.
- **Sink:** The resulting JSON was written through `_metadataRepository.SaveSystemOfflineConfigAsync(json, cdcConfig)` in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:94`.

### Fix

- Removed the `[FromBody] SaveSystemOfflineConfigRequest request` action parameter so the endpoint no longer uses ASP.NET Core input-formatter model binding for a complex object.
- Added `ReadSaveSystemOfflineConfigRequestAsync()` in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:104-196` to parse the raw JSON body and explicitly whitelist only the permitted fields.
- The handler now ignores any over-posted properties such as `_id`, `_rev`, `data_type`, or any other unexpected JSON members before building the saved `SystemOfflineConfig`.

### Evidence

- The action signature is now `SaveSystemOfflineConfig()` with no bound body parameter in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:52`.
- `ReadSaveSystemOfflineConfigRequestAsync()` only reads the allowed field names (`warn_date`, `warn_message`, `offline_date`, `offline_modal_message`, `offline_page_message`, `apply_to_all_jurisdictions`, `selected_jurisdictions`, `restoration_hours`, `auto_logout_minutes`) from a `JObject` in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:137-148`.
- The persisted payload still uses the server-owned revision from `existing?._rev` and never accepts client-supplied document identity or revision fields in `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:72-85`.

### Validation

- `dotnet build /home/runner/work/nccdphp-drh-mmria/nccdphp-drh-mmria/nccdphp-drh-mmria-services/mmria.services/mmria.services.sln`
