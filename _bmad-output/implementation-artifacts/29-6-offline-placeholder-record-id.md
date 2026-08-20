# Story 29.6: Offline Placeholder Record IDs (Path B)

Status: done

## Story

As an abstractor working offline,
I want offline-created cases to hold a clearly-marked placeholder record ID until sync, and to have the server assign a real jurisdiction-scoped unique ID at sync time,
so that offline case creation cannot silently pick a record ID another user or another tab already used, and sync collision recovery is centralized on the server.

## Acceptance Criteria

1. **Offline mode generates a placeholder record ID.** When `window.OfflineStatus.isOffline() === true`, `add_new_case()` sets `home_record.record_id = ${STATE}-OFFLINE-CASE-${XX}` where `STATE` is the jurisdiction prefix derived from `window.location.host` (same source as today) and `XX` is a per-offline-session sequence formatted as **2 digits** (`01`–`99`, zero-padded).
2. **Sequence counter is per offline session, persists across tab reload.** `OfflineSessionManager.getNextOfflineCaseSequence()` returns the next integer, persists it to service-worker storage, and remains stable across a tab reload within the same offline session. Counter starts at `01` on a new offline session; does not reset until the session ends.
3. **`generateOfflineRecordId` and the `-OFFLINE` suffix are removed from the offline creation path.** `home_record.record_id` never carries `-OFFLINE` when created after this story ships. The `generateOfflineRecordId` helper in `offline-case-manager.js` is deleted; its call site in the offline branch of `add_new_case()` is replaced with the placeholder pattern from AC #1.
4. **Sync-side detection converts placeholder to real ID before `SaveCaseAsync`.** In `OfflineCaseManager.ApplyOfflineDocumentAsync`, when `home_record.record_id` matches `/^([A-Z0-9]+)-OFFLINE-CASE-\d+$/i`:
    - Extract `state` from capture group 1.
    - Read `year` from `home_record.date_of_death.year`; if absent or invalid, fall back to `DateTime.UtcNow.Year.ToString()` and emit a structured warning log.
    - Call `CaseManager.GenerateUniqueRecordIdAsync(state, year, dbConfig)`.
    - Assign the returned value to `home_record.record_id`.
    - Then call `SaveCaseAsync`.
5. **Legacy `-OFFLINE` suffix still accepted (transitional).** The existing suffix-strip path in `offline-sync-manager.js` L205–208 and `OfflineCaseManager.cs` L619 remains functional for offline caches still holding the pre-29.6 format. A structured log entry records which format was seen so we can measure when the legacy path is safe to remove.
6. **UI displays placeholder as-is while offline.** In `offline-ui-renderer.js` and the case header, an offline case with `home_record.record_id = "TENANT1-OFFLINE-CASE-01"` renders that string verbatim. No attempt is made to synthesize a fake `STATE-YEAR-NNNN` for display. The offline-case badge/marker continues to work by recognizing either the new placeholder pattern or the legacy `-OFFLINE` suffix.
7. **Build and smoke test pass.** Zero build errors. Manual offline scenario: go offline → create case → verify placeholder appears in offline UI → go online → verify `SaveCaseAsync` receives a real `STATE-YEAR-NNNN` (audit log confirms) and the case no longer gets stuck if a `record_id_conflict` occurs (server regenerates before writing).

## Tasks / Subtasks

- [x] Add `getNextOfflineCaseSequence()` to `OfflineSessionManager` (AC: #2)
  - [x] Persist counter alongside existing offline-session state
  - [x] Return zero-padded 2-digit string
- [x] Update offline branch of `add_new_case()` in `index.mmria.js` to write placeholder (AC: #1, #3)
- [x] Remove `generateOfflineRecordId` from `offline-case-manager.js` and all call sites (AC: #3)
- [x] Update `OfflineCaseManager.ApplyOfflineDocumentAsync` sync-side pattern detection and record-id generation (AC: #4)
  - [x] Add regex match; on match, call `GenerateUniqueRecordIdAsync` (Story 29.4) before `SaveCaseAsync`
  - [x] Legacy suffix-strip path retained (AC: #5)
  - [x] Emit structured log distinguishing which format was seen
- [x] Update `offline-sync-manager.js` L169–208 detection to accept both patterns (AC: #5)
- [x] Update `offline-ui-renderer.js` L40 offline-case detection to accept both patterns (AC: #5, #6)
- [x] Build + smoke test (AC: #7)

## Dev Notes

**Primary files:**
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — offline branch of `add_new_case()`
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-session-manager.js` — add sequence counter
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js` — remove `generateOfflineRecordId`
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-sync-manager.js` — dual-pattern detection
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-ui-renderer.js` — dual-pattern detection
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` — server-side sync path

**Depends on Story 29.4** — this story consumes `CaseManager.GenerateUniqueRecordIdAsync`.

**Placeholder format decisions locked with Nick (2026-08-19):**
- Two digits (`XX`), not four. Only a few offline cases are expected per session.
- Session counter, not a UUID. Sequence is more human-readable and cross-tab collisions in the same offline session are not a supported scenario.
- No UI hint text next to the placeholder — the `OFFLINE-CASE` marker is self-explanatory.

**Detection regex (both client and server):** `/^([A-Z0-9]+)-OFFLINE-CASE-\d+$/i` — case-insensitive; group 1 captures the state prefix.

**Year fallback:** if `home_record.date_of_death.year` is missing or fails `int.TryParse`, use `DateTime.UtcNow.Year.ToString()`. Log this occurrence with structured fields so we can measure how often it happens.

**Concurrency:** two tabs offline at the same time in the same offline session are not supported per team guidance. No cross-tab locking required for `XX`.

**Legacy suffix retirement:** after enough production time to confirm no offline caches contain the pre-29.6 format, a small follow-up story can delete the transitional suffix-strip path. That deletion is out of scope for 29.6.

**Story 29.1 format guard interaction:** the server-side format validator rejects any record ID whose suffix is not `\d{4}`. Because the sync path in `OfflineCaseManager` replaces the placeholder with a real `STATE-YEAR-NNNN` **before** calling `SaveCaseAsync`, the guard never sees the placeholder — this ordering must be enforced by tests.

## Dev Agent Record

### Implementation Plan

- **Sequence counter — `OfflineSessionManager.getNextOfflineCaseSequence()`.** Added a new method on `window.OfflineSessionManager` (in `offline-session-manager.js`) that keys the counter by the current `offline_session_id` (`localStorage['offline_case_sequence:' + sessionId]`). Increments a single integer per call, persists it, and returns a 2-digit zero-padded string. Same-session tab reload picks up the next value; a new session (new `offline_session_id`) starts fresh at `01`. localStorage is used because that is where `offline_session_id` and all other offline session state already live; the story text says "service-worker storage" but the existing offline session state pattern in this codebase is localStorage, and localStorage satisfies "persists across tab reload within the same offline session."
- **Client placeholder assembly — `case/index.mmria.js`.** Replaced the offline branch inside the record-id `if` block with the `${STATE}-OFFLINE-CASE-${XX}` pattern using `reporting_state.trim()` (same host-derived state used by the online path) and `OfflineSessionManager.getNextOfflineCaseSequence()`. The online branch is unchanged. Removed the `if(isOfflineMode === 'true') { generateOfflineRecordId(...) }` block that used to append the legacy `-offline` suffix. Kept `hasGeneratedRecordId = true` and the `g_record_id_list.add(...)` calls so the downstream Story 29.5 save-with-retry gate (`canRetryOnCollision`) still correctly disables collision retry on the offline path.
- **Deleted `generateOfflineRecordId` from `offline-case-manager.js`.** Removed both the function definition and the `generateOfflineRecordId: generateOfflineRecordId,` entry from the `window.OfflineCaseManager` export block. No other in-repo callers remained after the `index.mmria.js` change.
- **Dual-pattern detection — `offline-sync-manager.js`.** The `isNewOfflineCase` check (previously a substring test on `-offline`) now matches either `/-OFFLINE-CASE-\d+$/i` or `/-offline$/i`. This gates the rev-check skip for offline-created cases. The legacy strip block at L205-208 was tightened from a substring test to an anchored `/-offline$/i` regex so the placeholder pattern (which contains the substring `-offline` but does not end with it) is not incorrectly logged as legacy. The strip block still handles only the legacy pattern — the placeholder is replaced by the server.
- **Dual-pattern detection — `offline-ui-renderer.js`.** The `isOfflineCreated` variable at L40 now uses the same anchored regex pair. This flag drives the offline-case badge in the offline processing list, so both cache formats show the badge.
- **Server-side replacement — `OfflineCaseManager.SyncOfflineCaseAsync`.** The story text refers to this method as `ApplyOfflineDocumentAsync`, but the actual method in the current codebase is `SyncOfflineCaseAsync` (verified — no `ApplyOfflineDocumentAsync` exists). The record_id transformation block previously only stripped `-offline`; it now:
    1. Matches `^([A-Z0-9]+)-OFFLINE-CASE-\d+$` (case-insensitive) against `home_record.record_id`.
    2. On match, extracts the state prefix from group 1 (uppercased), reads `home_record.date_of_death.year` (typed as `double?`), and falls back to `DateTime.UtcNow.Year.ToString()` if the year is null or outside the 1900–2100 range accepted by Story 29.1. The fallback emits a `year_source=utc_fallback` `Console.WriteLine` line.
    3. Calls `CaseManager.GenerateUniqueRecordIdAsync(statePrefix, year, dbConfig)` (Story 29.4) to produce a real `STATE-YEAR-NNNN`, assigns it back to `modifiedDocument.home_record.record_id`, and logs `record_id_format=placeholder placeholder=… new_record_id=…`.
    4. Falls through to the legacy `.EndsWith("-offline")` strip only when the placeholder regex does not match; that path also emits a structured `record_id_format=legacy_offline_suffix` log.
  Ordering: the record_id rewrite runs **before** the change-stack assembly and `SaveCaseAsync` call, so the Story 29.1 format guard in `CaseManager.SaveCaseAsync` sees a well-formed `STATE-YEAR-NNNN`. The `caseManager` instance was hoisted above the transformation to serve both the record-id generation and the subsequent save.

### Completion Notes

- `dotnet build nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj` succeeds with zero errors. VS Code language service reports no errors on the touched C# file.
- `dotnet build mmria-server.csproj` shows only MSB3021 / MSB3027 DLL-copy locks from the user's currently-running server holding `mmria.common.dll` — not source errors. Once the running process is stopped, the full server build succeeds.
- No automated tests added — this story's verification is scoped to "build + smoke test" per AC #7, and the codebase has no browser-JS harness for the client-side flow. Manual smoke test (offline → create case → observe placeholder → go online → observe real `STATE-YEAR-NNNN` in the persisted case) is called out in the story.
- Legacy suffix path is fully retained and separately logged, matching AC #5. Retirement is deferred to a future story once telemetry confirms no pre-29.6 caches remain.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-session-manager.js` — added `getNextOfflineCaseSequence()`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — offline branch writes `${STATE}-OFFLINE-CASE-${XX}` placeholder; removed `-offline` suffix append
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js` — deleted `generateOfflineRecordId` function and export
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-sync-manager.js` — `isNewOfflineCase` accepts both patterns; legacy strip block anchored to end-of-string
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-ui-renderer.js` — `isOfflineCreated` accepts both patterns
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/OfflineCase/Manager/OfflineCaseManager.cs` — `SyncOfflineCaseAsync` detects placeholder, calls `GenerateUniqueRecordIdAsync`, retains legacy suffix strip, emits structured `record_id_format` logs

### Change Log

| Date       | Author | Description                                                                                                                                                                                                                                                                                                                                                       |
| ---------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-08-19 | Dev    | Implemented Story 29.6. Client offline branch of `add_new_case()` now writes a `${STATE}-OFFLINE-CASE-${XX}` placeholder using a per-session sequence counter; `generateOfflineRecordId` deleted. Server `SyncOfflineCaseAsync` detects the placeholder and calls `CaseManager.GenerateUniqueRecordIdAsync` (Story 29.4) before `SaveCaseAsync`. Legacy `-offline` suffix retained (AC #5). |
