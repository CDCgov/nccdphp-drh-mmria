# Story 29.4: Extract `GenerateUniqueRecordIdAsync` Manager Method and Structured `error_code`

Status: ready-for-dev

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

- [ ] Add `SaveErrorCodes` constants class in `mmria.common.model.couchdb` (AC: #4)
- [ ] Add nullable `error_code` field to `document_put_response` (AC: #3)
- [ ] Extract `GenerateUniqueRecordIdAsync` on `CaseManager` (AC: #1)
  - [ ] Signature: `public async Task<string> GenerateUniqueRecordIdAsync(string statePrefix, string year, DBConfigurationDetail dbInfo, int maxAttempts = 20)`
  - [ ] Random 4-digit suffix in `[1000, 9999]`; check via `RecordIdExistsAsync`; retry until unique or `maxAttempts` exhausted
  - [ ] Throw `RecordIdGenerationExhaustedException` with `statePrefix`, `year`, `attempts` fields on exhaustion
- [ ] Refactor `GetRecordIdReplacementForYearOfDeathAsync` to delegate (AC: #2)
- [ ] Update Story 29.1 guard in `SaveCaseAsync` to set `error_code` on the two rejection paths (AC: #5)
- [ ] Unit tests (AC: #6)
- [ ] Build all projects (AC: #7)

## Dev Notes

**Primary files:**
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`
- `nccdphp-drh-mmria-common/mmria.common/model/couchdb/document_put_response.cs`
- `nccdphp-drh-mmria-common/mmria.common/model/couchdb/SaveErrorCodes.cs` (new)

**Exception type:** `RecordIdGenerationExhaustedException : Exception` — carry `statePrefix`, `year`, and `attempts` as public properties. Constructor sets `Message` to a diagnostic-friendly string. Live in the same namespace as `CaseManager` or a nested `Exceptions` folder.

**Serialization:** `document_put_response` is currently POCO with public properties. Add `public string error_code { get; set; }` — nullable string; JSON serializer emits it only when populated. Any existing consumers that ignore unknown fields (Newtonsoft default) are unaffected.

**Do NOT deprecate `record_idController` in this story** — Story 29.5 removes its last shipped caller and marks it for cleanup.

**Do NOT change client behavior in this story** — Stories 29.5, 29.6, 29.7 do that.
