# Populate CDC Instance and De-identification Context

- Status: Active
- Scope: Populate CDC Instance behavior, CDC database rebuild flow, and de-identification rule application.
- When to use: Read this before changing Populate CDC, CDC-target database setup, or de-identification behavior.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [MMRIA Services and Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md)
This document captures the important implementation details for the Populate CDC Instance feature, with emphasis on how case documents are de-identified and normalized before being written into the CDC database.

## Scope
- Populate CDC Instance flow
- Source case retrieval
- De-identification metadata source
- Field transformation rules
- CDC import lock cleanup
- Fallback behavior when de-identification is incomplete

## High-level Flow

Primary server flow:
- [`MMRIAServicesManager.PopulateCDCInstanceManger(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs)

For each source case, the flow is:
1. Read the source case document from the jurisdiction `mmrds`
2. Serialize it
3. De-identify it through the CDC de-identifier helper
4. Normalize/remove operational lock fields
5. Save the resulting document into the CDC `mmrds`

## Target Database Setup Behavior

Populate CDC does not update the existing CDC databases in place. It rebuilds them each run.

At the start of the workflow, [`SetupPopulateCdcDatabases(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs#L632) does the following:

- delete CDC `mmrds` if it exists
- recreate CDC `mmrds`
- reapply security and design documents
- delete CDC `de_id` if it exists
- recreate CDC `de_id`
- delete CDC `report` if it exists
- recreate CDC `report`

Important conclusion:
- populate-CDC rebuilds the target CDC databases rather than replacing selected documents in place

## Report Database Behavior

Populate CDC recreates the CDC `report` database, but it does not appear to run report-generation jobs as part of this workflow.

Current behavior in `SetupPopulateCdcDatabases(...)`:
- recreate `report`
- create the `opioid-report-index`

Relevant code:
- [`MMRIAServicesManager.cs:669`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs#L669)
- [`MMRIAServicesManager.cs:680`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs#L680)
- [`MMRIAServicesManager.cs:684`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs#L684)

What is not present in `PopulateCDCInstanceManger(...)`:
- no aggregate report rebuild trigger
- no report actor/job dispatch
- no code that populates CDC report documents as part of populate-CDC

Important conclusion:
- populate-CDC prepares the CDC `report` database structure, but does not itself generate report data

## Source Data vs Metadata Source

### Source case data
Source case documents come from the jurisdiction database configuration passed into populate-CDC.

Relevant data-access code:
- [`MMRIAServicesDAL.GetCaseDocumentForPopulateCDC(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs)

### De-identification metadata source
The metadata document `metadata/de-identified-export-list` is **not** fetched from the source jurisdiction database.

It is fetched from the CDC/CDCQA connection used by the populate-CDC workflow.

Relevant code:
- [`MMRIAServicesManager.PopulateCDCInstanceManger(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs)
- [`c_cdc_de_identifier.executeAsync(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs#L25)

Important conclusion:
- case data comes from the jurisdiction database
- de-identification rules come from CDC metadata

## De-identification Rule Selection

The de-identifier selects a field-path list from `metadata/de-identified-export-list` based on the source instance prefix.

Behavior:
- If `name_path_list` contains the prefix, use that list
- Otherwise use `global`

Important detail:
- it does **not** merge `global` with the state-specific list
- the state-specific list replaces `global`

Relevant code:
- [`c_cdc_de_identifier.executeAsync(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs#L38)

## Field Transformation Rules

Transformation logic is in:
- [`c_cdc_de_identifier.set_de_identified_value(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs#L126)

Current behavior:
- `_rev` is removed
- leaf fields named exactly `first_name` or `last_name` are set to `"de-identified"`
- other targeted string fields are set to `null`
- targeted `DateTime` fields are set to `null`
- other targeted scalar values are set to `null`
- arrays/objects are traversed recursively so the configured path can be reached

## Current Metadata Lists

The field list is runtime metadata, not hard-coded in C#.

Observed live list sizes during analysis:
- `global`: 79 paths
- `mn`: 233 paths
- `wa`: 126 paths
- `testlist1`: 85 paths

These values depend on the metadata document stored in CouchDB and can change over time.

## CDC-specific Lock Cleanup

Populate CDC now performs explicit lock cleanup before saving the de-identified case into the CDC database.

Relevant code:
- [`MMRIAServicesManager.ClearPopulateCdcLockFields(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs)

The following 7 fields are removed from the outgoing CDC document:
- `date_last_checked_out`
- `last_checked_out_by`
- `checked_out_by_tab_id`
- `is_offline`
- `offline_by`
- `offline_lock_type`
- `offline_by_tab_id`

Important conclusion:
- CDC imported cases should not carry operational edit-lock or offline-lock state from the source jurisdiction

## Fallback Behavior

If the helper determines the case was not fully de-identified, it falls back to the versioned case template.

Relevant code:
- [`c_cdc_de_identifier.executeAsync(...)`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs#L86)

Fallback behavior:
- load `case-version-{metadata_release_version_name}.json`
- preserve `_id`
- set `created_by` to `system` if blank
- set `last_updated_by` to `system`
- use template/default structure for the rest of the document

Important implication:
- this fallback is not just “clear a few fields”
- it can materially replace the case body with the versioned template structure

## Historical Finding

Analysis of older code paths showed no strong evidence that refactoring removed a prior explicit lock-cleanup feature.

Most likely conclusion:
- explicit CDC lock cleanup did not previously exist as intentional code
- if some lock-like fields were absent in older behavior, that was likely incidental through metadata-driven de-identification or template fallback, not a dedicated normalization step

## Practical Guidance for Future Changes

1. Do not assume the de-identification path list is local or jurisdiction-owned
   - it is currently CDC metadata driven

2. Do not assume `global` and state-specific lists combine
   - they do not today

3. Keep CDC normalization explicit
   - operational fields such as locks should be removed in code, not implicitly via metadata

4. Be careful with fallback changes
   - fallback behavior can significantly alter output shape/content

5. When investigating a de-identified output issue, check all three layers:
   - source case document
   - `metadata/de-identified-export-list`
   - helper/fallback logic in `c_cdc_de_identifier`

## Related Files
- [`MMRIAServicesManager.cs`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs)
- [`MMRIAServicesDAL.cs`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs)
- [`c_cdc_de_identifier.cs`](../../nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/c_cdc_de_identifier.cs)
- [`PopulateCDCInstanceTests.cs`](../../source-code/mmria/mmria-server.tests/Tests/PopulateCDCInstanceTests.cs)




