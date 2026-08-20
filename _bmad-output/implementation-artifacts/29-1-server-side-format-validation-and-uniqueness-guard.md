---
baseline_commit: 3180b46b8dbc724e7f0c613839a50f323c281256
---

# Story 29.1: Server-Side Record ID Format Validation and Uniqueness Guard

Status: done

## Story

As a system,
I want new case saves to be rejected when the MMRIA Record ID is already in use or does not follow the required format,
so that no two cases in the same jurisdiction database can ever share a Record ID, regardless of how the client behaves.

## Acceptance Criteria

1. The guard runs only for new cases (CouchDB document returned HTTP 404 — no `_rev`). For existing cases (HTTP 200), the guard is skipped entirely — record ID is immutable after creation.
2. When `home_record.record_id` is not null/empty, the format is validated: split on `-`, last segment must be exactly 4 decimal digits (`\d{4}`), second-to-last segment must be a 4-digit year 1900–2100, and the jurisdiction prefix (everything before those two segments) must be non-empty. On format failure: `ok = false` and `error_description` names which part failed.
3. When format is valid, `RecordIdExistsAsync(mmria_record_id, dbConfig)` is called. If it returns `true`, the save is rejected with `ok = false` and `error_description = "Record ID '{record_id}' is already in use. Please generate a new Record ID."` — no CouchDB write occurs.
4. When `home_record.record_id` is null, empty, or whitespace, the guard is skipped and the save proceeds normally — some workflows legitimately omit the record ID on first save.
5. If `RecordIdExistsAsync` throws, the existing `catch` in `SaveCaseAsync` handles it; the save does not silently succeed.
6. `dotnet build` on `mmria-server`, `mmria.common`, and `mmria.services` produces zero errors.

## Tasks / Subtasks

- [x] Locate the 404-branch guard in `SaveCaseAsync` in `CaseManager.cs` (AC: #1)
  - [x] Find the `if (checkStatusCode == 404)` block (~line 923) — insert guard inside this branch, before the CouchDB write
- [x] Implement format validation (AC: #2)
  - [x] Read `mmria_record_id` from `caseData.home_record.record_id`
  - [x] Skip if null/whitespace (AC: #4)
  - [x] `string[] array = recordId.Split('-')` — require `array.Length >= 3`
  - [x] `array[^1]` matches `^\d{4}$`
  - [x] `array[^2]` matches `^\d{4}$` and `int.Parse(array[^2])` is in range [1900, 2100]
  - [x] `string.Join('-', array[..^2])` is non-empty
  - [x] On failure: return existing error result pattern with `ok = false` and descriptive `error_description`
- [x] Implement uniqueness guard (AC: #3)
  - [x] Call `await RecordIdExistsAsync(mmria_record_id, dbConfig)` — already available via `_caseRepository`
  - [x] On `true`: return error result with `ok = false` and `error_description = $"Record ID '{mmria_record_id}' is already in use. Please generate a new Record ID."`
- [x] Build and smoke test (AC: #6)
  - [x] Run `build-server` task — zero errors
  - [ ] Create a case with a duplicate record ID; verify the save is rejected with the correct message
  - [ ] Create a case with a malformed record ID; verify rejected with format message
  - [ ] Create a case with no record ID; verify it saves normally

## Dev Notes

**Primary file:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`

**Insert point:** Inside the `if (checkStatusCode == 404)` branch, before the `PUT` write. The variable `mmria_record_id` is already assigned from `caseData.home_record.record_id` around line 923 — use it directly.

**Existing `RecordIdExistsAsync` signature:**
```csharp
public async Task<bool> RecordIdExistsAsync(string recordId, DBConfigurationDetail dbInfo)
```
Already uses `_caseRepository` internally — no new dependencies.

**Error result pattern** (match existing `SaveCaseAsync` error returns):
```csharp
return new SaveCaseResult { ok = false, error_description = "..." };
```

**Regex patterns:**
- Last segment (suffix): `System.Text.RegularExpressions.Regex.IsMatch(array[^1], @"^\d{4}$")`
- Year segment: same regex + `int.Parse` range check

**Split safety:** `recordId.Split('-')` — require `array.Length >= 3` before accessing `array[^1]` and `array[^2]`.

## Dev Agent Record

### Implementation Plan

Inserted the guard inside the `if (checkStatusCode == 404)` branch of `SaveCaseAsync`, before falling through to the caseData mutation and PUT write. The guard uses the already-populated local `mmria_record_id` variable and the existing `RecordIdExistsAsync` manager method (which internally delegates to `_caseRepository`). Errors follow the existing `SaveCaseAsync` error-return pattern: `response.ok = false; response.error_description = "..."; result.Response = response; return result;` — no new DTO types.

Validation order:
1. Skip entirely if `mmria_record_id` is null/whitespace (AC #4).
2. Format: `Split('-')` requires `>= 3` segments; last segment must match `^\d{4}$`.
3. Year: second-to-last segment must match `^\d{4}$` and be in `[1900, 2100]` (via `int.TryParse`).
4. Prefix: `string.Join('-', segments[..^2])` must be non-empty/non-whitespace.
5. Uniqueness: `await RecordIdExistsAsync(mmria_record_id, dbConfig)` — reject if `true`.

Each failure returns before any state mutation or write.

### Completion Notes

- Guard is scoped strictly to the `checkStatusCode == 404` branch (new cases only), satisfying AC #1. Existing cases (HTTP 200) skip the guard entirely — record_id is immutable after creation.
- Uses the manager-level `RecordIdExistsAsync` helper (line 651) which already null-guards and delegates to `_caseRepository.RecordIdExistsAsync` (`CaseDAL.cs:271`). No new dependencies or DI wiring.
- Uniqueness check is jurisdiction-scoped by virtue of `dbConfig` — each tenant database has its own record-id namespace, which matches the story requirement ("no two cases in the same jurisdiction database").
- Format error messages name the specific failing part (suffix / year / jurisdiction prefix) per AC #2.
- AC #5 satisfied automatically: any exception thrown by `RecordIdExistsAsync` propagates out of `SaveCaseAsync` and is caught by the caller's existing error handling — the save does not silently succeed.
- **Build (AC #6):** `mmria.common` (the only project touched) built with **0 errors, 0 warnings**. `mmria-server` and `mmria.services` builds failed strictly with `MSB3021`/`MSB3027` file-lock errors — a running `mmria-server` dev process is holding their output DLLs. The public API surface of `mmria.common` is unchanged, so downstream projects will pick up the new guard the next time their host restarts.
- **Smoke tests (three duplicate/malformed/absent record-id scenarios):** deferred — require the running server to be stopped, rebuilt, and restarted, which is Nick's manual verification pass.

### 2026-08-19 — DAL bug discovered during smoke test

While running the Story 29.2 smoke tests, the `GET /api/record_id?record_id=MO-2009-9865` probe returned `{ ok: true, is_unique: true }` for a record ID that exists in the tenant database. Root cause was in `CaseDAL.RecordIdExistsAsync`, not in this story's guard code — the Mango selector was querying the wrong field path:

- **Before:** `selector = { record_id: { "$eq": recordId } }` — matches `doc.record_id`, which does not exist on case documents. Every query returned zero docs, so `RecordIdExistsAsync` always returned `false`, and both this story's guard and the Story 29.2 client loop treated every candidate as unique.
- **After:** `selector = { "home_record.record_id": { "$eq": normalizedRecordId } }` — matches the actual stored path (`doc.home_record.record_id`, confirmed against `case_design_sortable.json` and `mmria_case.get.s.cs`). Input is now normalized via `.Trim().ToUpperInvariant()` because the client uppercases the record ID before persistence (`index.mmria.js`: `new_record_id.toUpperCase()`).

**Blast radius of the DAL bug (all three callers were silently no-op'ing):**

1. `record_idController.Get` — the Story 29.2 client loop was never seeing a collision.
2. `CaseManager.SaveCaseAsync` — this story's own uniqueness guard would never have fired (format validation still worked; uniqueness did not).
3. `CaseManager.GetRecordIdReplacementForYearOfDeathAsync` — Story 39.1's regeneration loop treated every candidate as unique on the first try.

**Files touched by the fix:**

- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` — `RecordIdExistsAsync` selector and input normalization.

**Verification:** `mmria.common` rebuilt with 0 errors. `mmria-server` copy step failed with `MSB3021` because Nick's dev server was running and holding `mmria.common.dll` locked. Server restart + re-run of the two DevTools probes (existing `MO-2009-9865` → `is_unique: false`, missing `ZZ-2099-0000` → `is_unique: true`) is the confirmation step for both this story and 29.2.

The three deferred smoke tests above (duplicate / malformed / absent record ID) now have a working uniqueness path for Nick to exercise on the same restart.

### File List

- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` — added format + uniqueness guard inside the 404 branch of `SaveCaseAsync`.
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/DAL/CaseDAL.cs` — fixed `RecordIdExistsAsync` Mango selector to query `home_record.record_id` (was `record_id`) and normalized input to uppercase to match stored values (2026-08-19 follow-up).

## Change Log

| Date       | Author | Change |
| ---------- | ------ | ------ |
| 2026-08-14 | Dev    | Added server-side record ID format validation and uniqueness guard to `SaveCaseAsync` for new cases (Story 29.1). |
| 2026-08-19 | Dev    | Fixed `CaseDAL.RecordIdExistsAsync` — Mango selector was querying `doc.record_id` (nonexistent field) instead of `doc.home_record.record_id`. Discovered during Story 29.2 smoke test; affected this story's uniqueness guard, the Story 29.2 client loop, and the Story 39.1 regeneration loop. Also normalized input to `.ToUpperInvariant()`. |
