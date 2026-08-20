# Story 29.7: IJE Batch Collision-Retry via `SaveCaseAsync` (Path C)

Status: superseded

> **⚠️ Superseded by Story 29.8 (2026-08-20).** This story's implementation strategy — route batch writes through `CaseManager.SaveCaseAsync` — caused an authorization regression: `SaveCaseAsync` runs the user-request authorization check against the synthetic `vital-import` `ClaimsPrincipal`, which has no role/jurisdiction entries, so every batch save fails with `unauthorized PUT`. Story 29.8 fixes this by routing batch writes through a dedicated `VitalImportCaseWriter` instead. The Story 29.1 record-id format/uniqueness guards and Story 29.4 collision-retry loop are preserved via a shared private helper on `CaseManager`. See FR-2.8 in [`prd-mmria-2026-08-06/prd.md`](../planning-artifacts/prds/prd-mmria-2026-08-06/prd.md) and the `2026-08-20 — FR-2.8 and FR-2.9 added` decision-log entry for the design rationale. This story file is retained for audit purposes; no further work will be performed against it.

## Story

As an operator running IJE batch imports,
I want each new-case write in the batch to route through the same `SaveCaseAsync` guard that the online UI uses, and to auto-regenerate the record ID on collision,
so that the batch cannot silently write duplicates when the tenant DB has more than 25 000 rows or when another writer wins a race against the batch's stale HashSet.

## Acceptance Criteria

1. **Batch write path routes through `CaseManager.SaveCaseAsync`.** In `BatchItemProcessingService.Process_Message`, each successful batch item is persisted via `CaseManager.SaveCaseAsync(caseData, changeStack, dbConfig, user, configuration, hostPrefix)` — not via the raw `_caseRepository.PutCaseDocumentJsonAsync` call at line ~2635. The `ClaimsPrincipal` is built from the vitals-import service account (`mmria.services.vitalsimport.Program.timer_user_name`) with the minimum claims `SaveCaseAsync` requires.
2. **Collision response triggers regeneration and retry.** When the returned `SaveCaseResult.Response.error_code === "record_id_conflict"`:
    - Call `CaseManager.GenerateUniqueRecordIdAsync(state, year, dbConfig)`.
    - Update `home_record.record_id` in the case document.
    - Retry the save.
    - Cap at 5 attempts. On exhaustion, mark the batch item `ImportFailed` with `StatusDetail = "unable to generate unique record id after 5 attempts"`.
3. **Final batch item record_id reflects post-retry value.** `BatchItem.mmria_record_id` on the final status object carries the record ID that was actually persisted (not the pre-retry candidate), so users tracing a case in the UI see the correct value.
4. **Stale `GetExistingRecordIds` path is retired for uniqueness.** `MMRIAServicesHelper.ConvertLineToBatchItem` no longer consumes `ExistingRecordIds` as a cross-writer uniqueness guard. The parameter is either:
    - **Removed** (if a caller-scan confirms no other consumers), or
    - **Documented** as batch-local dedup only (see AC #5).
    
    `MMRIAServicesManager.GetExistingRecordIds` and `MMRIAServicesDAL.GetExistingRecordIds` are marked `[Obsolete]` if any other callers exist (verify with a grep across both repos), or deleted if no other callers remain.
5. **In-batch dedup preserved.** A batch-local `HashSet<string>` (fresh, initialized empty per batch) prevents intra-file suffix collisions when two rows in the same MOR happen to generate the same 4-digit random suffix. The AC #2 retry path only fires against cross-writer / cross-DB collisions.
6. **Build passes and IJE smoke test succeeds.** Zero build errors in `mmria.common` and `mmria.services`. A small IJE batch imported against the local multi-tenant environment: every `NewCaseAdded` batch item has a unique `home_record.record_id` in the DB. A forced-collision test (pre-seed a case with a known suffix, then import a batch designed to attempt that suffix) confirms the retry loop picks a fresh suffix and completes successfully.

## Tasks / Subtasks

- [x] Locate the raw DAL PUT in `BatchItemProcessingService.Process_Message` (~line 2635) (AC: #1)
- [x] Build a service-account `ClaimsPrincipal` for the vitals-import identity (AC: #1)
- [x] Build a synthetic `Change_Stack` for the new-case save (`object_path = "vital_import"`) (AC: #1)
- [x] Replace the raw PUT with `CaseManager.SaveCaseAsync` (AC: #1)
- [x] Wrap the save in a collision-retry loop keyed on `error_code === "record_id_conflict"` (AC: #2)
  - [x] Cap at 5 attempts
  - [x] On exhaustion, populate `BatchItem` with `ImportFailed` status and the failure `StatusDetail`
- [x] Ensure the final `BatchItem.mmria_record_id` reflects the persisted value (AC: #3)
- [x] Retire the stale HashSet uniqueness path in `MMRIAServicesHelper.ConvertLineToBatchItem` (AC: #4)
  - [x] Grep both repos for callers of `GetExistingRecordIds`
  - [x] Delete or mark `[Obsolete]` accordingly
- [x] Add batch-local dedup HashSet in the loop that assigns initial candidates (AC: #5)
- [x] Build both projects (AC: #6)
- [ ] IJE smoke test in local multi-tenant environment (AC: #6)

## Dev Notes

**Primary files:**
- `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Manager/MMRIAServicesManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs`

**Depends on Story 29.4** — this story consumes both `GenerateUniqueRecordIdAsync` and the `error_code` field on `document_put_response`.

**Service-account `ClaimsPrincipal`:** minimum shape required by `SaveCaseAsync` is a `ClaimsPrincipal` with an authenticated identity carrying a `Name` claim. Build one using `new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "vital-import") }, "vital-import-auth")`. Verify `authorization_case.is_authorized_to_handle_jurisdiction_id` accepts this identity for the target jurisdiction — the vitals-import identity already writes cases via other paths.

**`Change_Stack` shape:** reuse the same minimal shape used by `OfflineCaseManager.ApplyOfflineDocumentAsync` (single `Change_Stack_Item` with `object_path = "vital_import"`, `metadata_path = "/vital_import"`, `prompt = "Vital Import"`, etc.).

**Audit-log implication:** `SaveCaseAsync` writes `Change_Stack` audit entries via `IAuditRepository`. IJE-imported cases will now have an audit trail attributed to the vitals-import service account — this is desired.

**In-batch dedup (AC #5):** keep the loop shape similar to today's, just replace the DB-wide `ExistingRecordIds` HashSet with a fresh batch-local one:

```csharp
var batchLocalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var row in mor_set) {
    string candidate;
    do { candidate = $"{state.ToUpper()}-{x["DOD_YR"]}-{GenerateRandomFourDigits()}"; }
    while (!batchLocalIds.Add(candidate));
    // ... assign to batch item
}
```

**Do NOT delete `MMRIAServicesDAL.GetExistingRecordIds` if the utilities test project references it.** Add `[Obsolete("Retired for uniqueness by Story 29.7; delete after utility callers migrate.")]` and open a follow-up.

**Retry cap = 5** — matches Stories 29.5 and 29.6 for consistency across paths.
