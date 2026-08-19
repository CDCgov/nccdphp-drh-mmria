---
baseline_commit: ae1651c151f955484cf6da0183cc7495d07a0a9c
---

# Story 29.5: Online Save-Then-Retry-on-Collision (Path A)

Status: done

## Story

As an abstractor,
I want the "Generate Record ID" flow to POST the case with a single locally-generated candidate and automatically try a fresh suffix if the server reports a collision,
so that the create-case flow does not do 20 pre-flight round trips in the common case and does not surface a hard failure when a rare race condition occurs.

## Acceptance Criteria

1. **Online path no longer pre-flights `/api/record_id`.** In online mode, `add_new_case()` in `index.mmria.js` generates one candidate `STATE-YEAR-NNNN` locally and issues a single POST to `/api/case`. No calls to `/api/record_id` on the online branch.
2. **Collision response triggers regeneration and retry.** When the response body contains `error_code === "record_id_conflict"`, `add_new_case()` regenerates the 4-digit suffix (same `generateCandidate` closure as Story 29.2), reassigns `home_record.record_id`, and re-POSTs to `/api/case`.
3. **Retry cap prevents infinite loop.** After 5 consecutive `record_id_conflict` responses, the loop exits, `alert("Unable to generate a unique Record ID after multiple attempts. Please try again.")` fires, and no case is created. Cap is a named constant `MAX_UNIQUE_RETRIES = 5` at the top of the loop.
4. **Non-collision errors surface immediately.** Any `error_code` other than `record_id_conflict` (or an absent `error_code` on a failed response) bypasses the retry loop; the response is surfaced via the existing `save_case_and_wait` failure path.
5. **PMSS variant kept consistent.** `index.pmss.js` uses the server-authoritative `/api/case_view/next-pmss-number` endpoint (already the source of truth). Diff review confirms no pre-flight loop remains and no changes are required beyond keeping the removal of the Story 29.2 `Get_Record_Id_List` wrapper intact.
6. **Offline branch untouched.** The offline branch of `add_new_case()` still works as-is in this story. Story 29.6 handles the offline behavior change; 29.5 and 29.6 do not conflict because they modify different branches of the same conditional.
7. **Build passes and smoke test succeeds.** Zero build errors. A case creates successfully with one round trip in the local multi-tenant environment. A DevTools instrumentation forcing `record_id_conflict` twice confirms the retry loop advances and eventually succeeds.

## Tasks / Subtasks

- [x] Remove Story 29.2's pre-flight loop from the online branch of `add_new_case()` in `index.mmria.js` (AC: #1)
- [x] Wrap the POST in a retry loop keyed on `error_code === "record_id_conflict"` (AC: #2, #3)
- [x] Preserve the existing user-facing exhaustion message and `__handled` error marker (AC: #3)
- [x] Ensure other error codes / missing codes short-circuit to normal failure (AC: #4)
- [x] Diff-review `index.pmss.js` and confirm no pre-flight remains (AC: #5)
- [x] Keep the offline branch of `add_new_case()` untouched (AC: #6)
- [x] Add a `[Obsolete("no shipped callers after Story 29.5; retain for Story 29.3 cleanup pass")]` note (or code comment) on `record_idController.Get` (AC: none — housekeeping)
- [x] Build + smoke test (AC: #7)

## Dev Notes

**Primary file:** `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — the online branch of `add_new_case()`.

**Depends on Story 29.4** — this story reads `response.error_code`, which is added by 29.4.

**Detection pattern (client):**

```javascript
const resp = await save_case_and_wait_return_response(...); // whatever the accessor is
if (resp && resp.error_code === "record_id_conflict") { /* regenerate + retry */ }
```

Reuse the existing `save_case_and_wait` return path — do not introduce a parallel save function. If `save_case_and_wait` currently discards the response body, extend it to surface `error_code` (backward compatible — existing callers ignore it).

**Loop shape:**

```javascript
const MAX_UNIQUE_RETRIES = 5;
let attempts = 0;
let candidate = generateCandidate();
while (true) {
    result.home_record.record_id = candidate.toUpperCase();
    const resp = await try_save_case(result); // wraps save_case_and_wait
    if (!resp || resp.error_code !== "record_id_conflict") break; // success or non-collision error
    attempts++;
    if (attempts >= MAX_UNIQUE_RETRIES) {
        alert("Unable to generate a unique Record ID after multiple attempts. Please try again.");
        const err = new Error("Unable to generate a unique Record ID after multiple attempts.");
        err.__handled = true;
        throw err;
    }
    candidate = generateCandidate();
}
```

**PMSS:** the `/api/case_view/next-pmss-number` endpoint is already server-authoritative, so no analogous retry loop is needed. Just confirm Story 29.2's `Get_Record_Id_List` wrapper removal stayed in place.

**`record_idController` deprecation:** do not delete in this story. Add an `[Obsolete]` marker and a `// no shipped callers after Story 29.5` comment. Story 29.3 (or a follow-up) removes it once the sprint is confident nothing else calls it.

## Dev Agent Record

### Implementation Plan

- **Server-side rejection contract (already in place from Story 29.4).** `CaseManager.SaveCaseAsync` populates `document_put_response.error_code = SaveErrorCodes.RecordIdConflict` when a new case's `record_id` collides with an existing one (`CaseManager.cs` ~line 1014). The client just needs to see that code on the rejection object.
- **Client rejection propagation.** `save_case_and_wait` (in `case/index.js`) currently constructs `{ status, responseText }` on the failure path and drops `error_code`. Extended the failure `err_object` to include `error_code: case_response.error_code`, and added a short-circuit that skips the generic `save_error_500_dialog_show` when `error_code === "record_id_conflict"` — otherwise every retry would flash a scary "server error" dialog before the collision is resolved. Backward compatible: existing callers ignore the extra field.
- **`add_new_case()` rewrite (online branch only).**
  - Removed the 20-attempt `GET /api/record_id?record_id=…` pre-flight loop.
  - Hoisted the `generateRecordIdCandidate` closure and a `hasGeneratedRecordId` flag out of the record-id `if` block so the save-time retry loop can call it. `reporting_state` and `yearPart` remain captured by the closure.
  - Kept the offline branch (`isOfflineForUniqueness === true`) exactly as-is — Story 29.6 will replace that with the placeholder pattern.
- **Save-with-retry loop.** Inside the existing `set_local_case → save_case_and_wait` block, wrapped the save call in a `while (true)` loop. Success or non-collision failure breaks out immediately; `error_code === "record_id_conflict"` regenerates a candidate, updates `g_data.home_record.record_id`, syncs `result.home_record.record_id` and the last `g_ui.case_view_list[].value.record_id`, and re-posts. `MAX_UNIQUE_RETRIES = 5`; on exhaustion, `alert(…)` fires and a `new Error(...)` with `__handled = true` is thrown so upstream handlers do not double-report.
- **Retry-gate.** The retry logic is guarded by `canRetryOnCollision = hasGeneratedRecordId && generateRecordIdCandidate != null && isOfflineMode !== 'true' && !window.OfflineStatus.isOffline()`. If the caller supplied a `record_id`, or the case is offline, we do not retry — collision would either be programmer error (caller-supplied `record_id`) or impossible (offline path never contacts the server).
- **PMSS diff-review.** `index.pmss.js` already relies on the server-authoritative `/api/case_view/next-pmss-number` endpoint (line ~113). Lines 96–103 are the old dead-code candidate loop, already commented out. Line 424 documents the Story 29.2 `Get_Record_Id_List` removal. No changes required.
- **`record_idController.Get` deprecation.** Added `[Obsolete("no shipped callers after Story 29.5; retain for Story 29.3 cleanup pass")]` plus a matching one-line comment. Body untouched.

### Completion Notes

- Zero CS compile errors (`dotnet build -t:CoreCompile` succeeds; the observable MSB3021/MSB3027 errors on a full build are file-copy locks caused by the user's running server holding `mmria.common.dll`, not source errors — VS Code language service also reports no errors).
- Only client-side JS behavior and one C# attribute change; no new dependencies, no server API surface change.
- No new automated tests added — the story explicitly scopes verification to "build passes and smoke test succeeds" and the codebase does not have a browser-JS test harness for these scripts. Manual DevTools verification is called out in AC #7 for the collision-retry path.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — removed online `/api/record_id` pre-flight loop; hoisted `generateRecordIdCandidate`; wrapped `save_case_and_wait` in a `record_id_conflict` retry loop (cap 5)
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` — extended `save_case_and_wait` failure path to surface `error_code`; skipped 500 dialog when `error_code === "record_id_conflict"`
- `source-code/mmria/mmria-server/Controllers/api/record_idController.cs` — added `[Obsolete]` marker on `Get(...)` per Story 29.3 cleanup pass

### Change Log

| Date       | Author | Description                                                                                                                                                                                                                                            |
| ---------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 2026-08-19 | Dev    | Implemented Story 29.5. Replaced Story 29.2's online per-candidate pre-flight loop with a server-authoritative single POST + retry on `record_id_conflict` (cap 5). Extended `save_case_and_wait` rejection object with `error_code`. Marked `record_idController.Get` obsolete pending Story 29.3 cleanup. |
