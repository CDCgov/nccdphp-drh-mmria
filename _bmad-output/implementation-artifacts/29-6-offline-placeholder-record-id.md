# Story 29.6: Offline Placeholder Record IDs (Path B)

Status: backlog

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

- [ ] Add `getNextOfflineCaseSequence()` to `OfflineSessionManager` (AC: #2)
  - [ ] Persist counter alongside existing offline-session state
  - [ ] Return zero-padded 2-digit string
- [ ] Update offline branch of `add_new_case()` in `index.mmria.js` to write placeholder (AC: #1, #3)
- [ ] Remove `generateOfflineRecordId` from `offline-case-manager.js` and all call sites (AC: #3)
- [ ] Update `OfflineCaseManager.ApplyOfflineDocumentAsync` sync-side pattern detection and record-id generation (AC: #4)
  - [ ] Add regex match; on match, call `GenerateUniqueRecordIdAsync` (Story 29.4) before `SaveCaseAsync`
  - [ ] Legacy suffix-strip path retained (AC: #5)
  - [ ] Emit structured log distinguishing which format was seen
- [ ] Update `offline-sync-manager.js` L169–208 detection to accept both patterns (AC: #5)
- [ ] Update `offline-ui-renderer.js` L40 offline-case detection to accept both patterns (AC: #5, #6)
- [ ] Build + smoke test (AC: #7)

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
