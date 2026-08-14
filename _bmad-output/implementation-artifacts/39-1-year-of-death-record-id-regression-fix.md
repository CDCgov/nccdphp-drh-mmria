# Story 39.1: Update Year of Death — Record ID Assignment Regression Fix

Status: ready-for-dev

## Story

As a CDC admin or jurisdiction admin,
when I use the Update Year of Death page without changing the year,
I want the system to correctly preserve or generate a valid record ID,
so that cases are not accidentally assigned a different record ID or left with no record ID.

## Acceptance Criteria

1. **No record ID, year unchanged:** When the case has no existing record ID (null/empty) and the year is not changed, `GetRecordIdReplacementForYearOfDeathAsync` generates a new valid record ID using the current year (e.g., `GA-2020-XXXX`). It does not return an empty string.
2. **Existing record ID, year unchanged:** When the case has a valid record ID (`{state}-{year}-{4-digit}`) and the user submits with the same year, the existing record ID is returned unchanged. The uniqueness loop does not fire against the case's own record ID.
3. Both fixes apply identically for `cdc_admin` and `jurisdiction_admin` role variants (same code path).
4. When the year IS changed (existing behavior), the method still generates a new record ID with the replacement year. This behavior is not changed.
5. `dotnet build mmria-server.csproj` and `dotnet build mmria.common.csproj` — zero errors.

## Tasks / Subtasks

- [ ] Fix Bug 1 — generate record ID when case has none and year is unchanged (AC: #1)
  - [ ] In `GetRecordIdReplacementForYearOfDeathAsync` in `CaseManager.cs`, find the early return: `if (array.Length < 3) { return recordId ?? string.Empty; }`
  - [ ] Change to: if `array.Length < 3` AND `yearOfDeathReplacement` is not null, generate a brand-new record ID using `yearOfDeathReplacement`: `new_record_id = $"{statePrefix}-{yearOfDeathReplacement}-{_rdm.Next(1000, 9999)}"` then run the uniqueness loop
  - [ ] The state prefix when no record ID exists: derive from the `stateDatabase` parameter (it IS the state prefix for this tenant)
- [ ] Fix Bug 2 — preserve existing record ID when year is unchanged (AC: #2)
  - [ ] After computing `new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{array[2]}"`, check if `new_record_id == recordId` (same year, same suffix → same ID as current)
  - [ ] If equal: return `recordId` immediately without calling `RecordIdExistsAsync` — the current case owns this ID
  - [ ] If not equal (year changed): continue to the existing uniqueness loop as before
- [ ] Build and smoke test (AC: #5)
  - [ ] Run `build-server` task — zero errors
  - [ ] Open Update Year of Death for a case WITH a valid record ID, submit without changing year → confirm record ID is unchanged on the confirmation screen
  - [ ] Open Update Year of Death for a case WITHOUT a record ID, submit without changing year → confirm a new valid record ID is shown on the confirmation screen

## Dev Notes

**Primary file:** `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`

**Method to fix:** `GetRecordIdReplacementForYearOfDeathAsync` (~line 661)

**Current code (buggy):**
```csharp
var array = recordId?.Split('-') ?? Array.Empty<string>();
if (array.Length < 3)
{
    // Bug 1: returns "" when no record ID exists and year is unchanged
    return recordId ?? string.Empty;
}

string new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{array[2]}";

// Bug 2: when year is unchanged, new_record_id == recordId, so RecordIdExistsAsync
// finds the current case's own ID as a conflict and generates a DIFFERENT random ID
while (await RecordIdExistsAsync(new_record_id, db_info))
{
    new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{_rdm.Next(_min, _max)}";
}
```

**Fixed code pattern:**
```csharp
var array = recordId?.Split('-') ?? Array.Empty<string>();
if (array.Length < 3)
{
    // Bug 1 fix: generate new ID with current year when no valid existing ID
    if (yearOfDeathReplacement.HasValue && db_info != null)
    {
        string prefix = stateDatabase; // use stateDatabase as state prefix
        string new_id = $"{prefix}-{yearOfDeathReplacement}-{_rdm.Next(_min, _max)}";
        while (await RecordIdExistsAsync(new_id, db_info))
            new_id = $"{prefix}-{yearOfDeathReplacement}-{_rdm.Next(_min, _max)}";
        return new_id;
    }
    return recordId ?? string.Empty;
}

string new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{array[2]}";

// Bug 2 fix: if unchanged year produces the same record ID, return it directly
if (string.Equals(new_record_id, recordId, StringComparison.OrdinalIgnoreCase))
    return recordId;

while (await RecordIdExistsAsync(new_record_id, db_info))
    new_record_id = $"{array[0]}-{yearOfDeathReplacement}-{_rdm.Next(_min, _max)}";
```

**`stateDatabase` parameter** — this is the `stateDatabase` string passed in from the controller (the tenant state prefix). Use it as the state prefix when generating a record ID for a case that has none.

**Both role variants** (`cdc_admin` and `jurisdiction_admin`) call the same `GetRecordIdReplacementForYearOfDeathAsync` method via `ConfirmUpdateYearOfDeathRequest`. No view changes needed.

**Pre-v4.1 reference:** Git log the pre-v4.1 version of this method if the above analysis needs verification.
