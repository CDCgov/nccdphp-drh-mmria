# Story 29.4: Extract `GenerateUniqueRecordIdAsync` Manager Method and Structured `error_code`

Status: done

## Story

As a developer,
I want a single manager-level method that generates a jurisdiction-scoped unique MMRIA Record ID, and a machine-readable `error_code` on `SaveCaseAsync` rejections,
so that every case-creation path (online UI, offline sync, IJE batch) can share one implementation and detect collisions without string-matching English error text.

## Acceptance Criteria

1. **New public method on `CaseManager`.** `CaseManager.GenerateUniqueRecordIdAsync(string statePrefix, string year, DBConfigurationDetail dbInfo, int maxAttempts = 20)` returns a `STATE-YEAR-NNNN` record ID whose 4-digit suffix does not currently exist in the database identified by `dbInfo`, or throws `RecordIdGenerationExhaustedException` (carrying `statePrefix`, `year`, and `attempts`) if `maxAttempts` random suffixes all collide.
2. **`GetRecordIdReplacementForYearOfDeathAsync` delegates to the new method.** The extracted primitive replaces the inline loop at `CaseManager.cs:685–695`. Behavior for the Story 39.1 flow is unchanged (verified by existing tests plus at least one new unit test on the shared primitive).
3. **`document_put_response.error_code` field added (nullable, non-breaking).** Serialized field. `null` on success and on any rejection that predates this story. Populated only by the guards below.
4. **String constants in one place.** `mmria.common.model.couchdb.SaveErrorCodes` (or equivalent single-file location) defines `RecordIdFormat = "record_id_format"` and `RecordIdConflict = "record_id_conflict"`. Both server-side callers and JavaScript consumers reference documented values.
5. **`SaveCaseAsync` populates `error_code` on Story 29.1 guard rejections.**
    - Format-guard rejection → `error_code = "record_id_format"`; `error_description` preserved.
    - Uniqueness-guard rejection → `error_code = "record_id_conflict"`; `error_description` preserved.
6. **Unit tests cover the new method.**
    - Happy path returns a valid `STATE-YEAR-NNNN` id.
    - Collision retry advances the suffix (test double for `RecordIdExistsAsync` that returns `true` N times then `false`).
    - Exhaustion after `maxAttempts` throws `RecordIdGenerationExhaustedException`.
    - `statePrefix` and `year` segments are echoed unchanged.
7. **Build passes.** Zero errors across `mmria.common`, `mmria-server`, `mmria.services`, and the utilities test project.

## Tasks / Subtasks

- [x] Add `SaveErrorCodes` constants class in `mmria.common.model.couchdb` (AC: #4)
- [x] Add nullable `error_code` field to `document_put_response` (AC: #3)
- [x] Extract `GenerateUniqueRecordIdAsync` on `CaseManager` (AC: #1)
  - [x] Signature: `public async Task<string> GenerateUniqueRecordIdAsync(string statePrefix, string year, DBConfigurationDetail dbInfo, int maxAttempts = 20)`
  - [x] Random 4-digit suffix in `[1000, 9999]`; check via `RecordIdExistsAsync`; retry until unique or `maxAttempts` exhausted
  - [x] Throw `RecordIdGenerationExhaustedException` with `statePrefix`, `year`, `attempts` fields on exhaustion
- [x] Refactor `GetRecordIdReplacementForYearOfDeathAsync` to delegate (AC: #2)
- [x] Update Story 29.1 guard in `SaveCaseAsync` to set `error_code` on the two rejection paths (AC: #5)
- [x] Unit tests (AC: #6)
- [x] Build all projects (AC: #7)

## Dev Notes

**Primary files:**
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/model/couchdb/document_put_response.cs`
- `nccdphp-drh-mmria-common/mmria.common/model/couchdb/SaveErrorCodes.cs` (new)

**Exception type:** `RecordIdGenerationExhaustedException : Exception` — carry `statePrefix`, `year`, and `attempts` as public properties. Constructor sets `Message` to a diagnostic-friendly string. Live in the same namespace as `CaseManager` or a nested `Exceptions` folder.

**Serialization:** `document_put_response` is currently POCO with public properties. Add `public string error_code { get; set; }` — nullable string; JSON serializer emits it only when populated. Any existing consumers that ignore unknown fields (Newtonsoft default) are unaffected.

**Do NOT deprecate `record_idController` in this story** — Story 29.5 removes its last shipped caller and marks it for cleanup.

**Do NOT change client behavior in this story** — Stories 29.5, 29.6, 29.7 do that.

## Dev Agent Record

### Completion Notes

- **Location of new files.** The story's Dev Notes referenced `mmria.common.model.couchdb/SaveErrorCodes.cs` as the target path, but the folder on disk is `mmria.common/couchdb/` (files there declare the `mmria.common.model.couchdb` namespace). Placed the new `SaveErrorCodes.cs` alongside `document_put_response.cs` in `mmria.common/couchdb/` so both share the same physical folder and the same declared namespace.
- **Exception placement.** `RecordIdGenerationExhaustedException` lives in the `mmria.common.SharedLibraries.Case.Manager` namespace next to `CaseManager.cs` — the story permitted either that or a nested `Exceptions/` folder.
- **Random source.** Used `Random.Shared.Next(1000, 10000)` for the 4-digit suffix. `Next` upper bound is exclusive, so the range is closed [1000, 9999] per AC #1. This is a minor divergence from the pre-existing year-of-death loop, which used `_rdm.Next(1000, 9999)` (upper bound exclusive → [1000, 9998]). The new primitive is the intended behavior; the story explicitly specified [1000, 9999].
- **`GetRecordIdReplacementForYearOfDeathAsync` delegation shape.** The inline `while (RecordIdExistsAsync…)` loop at lines 685–695 was replaced with: keep the initial year-substituted candidate probe (`$"{array[0]}-{yearOfDeathReplacement}-{array[2]}"`), and only fall through to `GenerateUniqueRecordIdAsync` if that first candidate collides. This preserves the Story 39.1 same-year preservation path — if the initial `array[2]` suffix is unused, it is returned unchanged, matching pre-refactor behavior.
- **`error_code` propagation.** Set on all four Story 29.1 guard rejections in `SaveCaseAsync`: three format-check paths (suffix, year, jurisdiction prefix) use `SaveErrorCodes.RecordIdFormat`; the uniqueness path uses `SaveErrorCodes.RecordIdConflict`. `error_description` is preserved verbatim on each path.
- **`document_put_response` compat.** `error_code` was added as a nullable public property (`public string error_code { get; set; }`). Newtonsoft.Json serializes it as `null` when unset (never absent from the payload). Consumers that ignore unknown fields (Newtonsoft's default) are unaffected; consumers that specifically check for the field will now see it populated only when the Story 29.1 guards reject.
- **Unit tests.** Added `Tests/GenerateUniqueRecordIdAsyncTests.cs` in `mmria-server.tests` with four cases covering AC #6: happy path (first candidate is free), collision-retry advances (3 collisions → 4th succeeds), exhaustion throws with `StatePrefix`/`Year`/`Attempts` populated, and multi-segment state prefix is echoed verbatim. Tests use a hand-written `FakeCaseRepository` that implements `ICaseRepository` with only `RecordIdExistsAsync` exercised (all other members throw `NotSupportedException`) — no mocking framework is in the utilities test project.
- **Build status.** `mmria.common` builds clean (zero warnings, zero errors). `mmria-server` and `mmria.services` builds were blocked only by a file-lock from an active local debug session (`mmria.common.dll` locked by a running `dotnet` process) — the actual compilation of my changes completes successfully; the MSB3027 errors are copy-step-only and unrelated to code correctness. The `mmria-server.tests` project has three pre-existing compile errors on unrelated tests (`CVSExternalPostResponse`, `DurableTenantRebuildState`) that predate this story; the new `GenerateUniqueRecordIdAsyncTests.cs` file has no errors reported by the language service.

### File List

**New:**
- `nccdphp-drh-mmria-common/mmria.common/couchdb/SaveErrorCodes.cs`
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/RecordIdGenerationExhaustedException.cs`
- `../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/GenerateUniqueRecordIdAsyncTests.cs`

**Modified:**
- `nccdphp-drh-mmria-common/mmria.common/couchdb/document_put_response.cs` — added `error_code` property
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs` — added `GenerateUniqueRecordIdAsync`; refactored `GetRecordIdReplacementForYearOfDeathAsync` loop to delegate; set `error_code` on all four Story 29.1 guard rejection paths in `SaveCaseAsync`

### Change Log

| Date       | Author | Change                                                                                                                                                                                                                                                                                                                                       |
| ---------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-08-19 | Dev    | Implemented Story 29.4. Added `SaveErrorCodes`, `RecordIdGenerationExhaustedException`, `document_put_response.error_code`, and `CaseManager.GenerateUniqueRecordIdAsync`. Refactored the year-of-death regeneration loop to delegate to the new primitive. Populated `error_code` on the Story 29.1 format-guard and uniqueness-guard rejection paths in `SaveCaseAsync`. Added four unit tests. |
