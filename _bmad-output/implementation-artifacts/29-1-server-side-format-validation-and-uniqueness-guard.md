# Story 29.1: Server-Side Record ID Format Validation and Uniqueness Guard

Status: ready-for-dev

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

- [ ] Locate the 404-branch guard in `SaveCaseAsync` in `CaseManager.cs` (AC: #1)
  - [ ] Find the `if (checkStatusCode == 404)` block (~line 923) — insert guard inside this branch, before the CouchDB write
- [ ] Implement format validation (AC: #2)
  - [ ] Read `mmria_record_id` from `caseData.home_record.record_id`
  - [ ] Skip if null/whitespace (AC: #4)
  - [ ] `string[] array = recordId.Split('-')` — require `array.Length >= 3`
  - [ ] `array[^1]` matches `^\d{4}$`
  - [ ] `array[^2]` matches `^\d{4}$` and `int.Parse(array[^2])` is in range [1900, 2100]
  - [ ] `string.Join('-', array[..^2])` is non-empty
  - [ ] On failure: return existing error result pattern with `ok = false` and descriptive `error_description`
- [ ] Implement uniqueness guard (AC: #3)
  - [ ] Call `await RecordIdExistsAsync(mmria_record_id, dbConfig)` — already available via `_caseRepository`
  - [ ] On `true`: return error result with `ok = false` and `error_description = $"Record ID '{mmria_record_id}' is already in use. Please generate a new Record ID."`
- [ ] Build and smoke test (AC: #6)
  - [ ] Run `build-server` task — zero errors
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
