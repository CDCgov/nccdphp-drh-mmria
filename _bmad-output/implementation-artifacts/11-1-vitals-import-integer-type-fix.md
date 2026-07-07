# Story 11.1 — Vitals Import: Store Integer-Coded Fields as JSON Numbers

**Epic:** 11 — Vitals Import Integer Type Fix
**Story ID:** 11.1
**Status:** not-started
**Date added:** 2026-07-07

---

## User Story

As a case reviewer,
When a case is created via vitals import (NAT or FET file),
I want coded dropdown fields (such as "Mother Married" and "Paternity Acknowledgement") to display their correct label values,
So that the case form does not show "Select Value" for fields that were successfully imported.

---

## Acceptance Criteria

**AC-1 — `mother_married` stored as JSON number after NAT import**
Given a NAT file with `MARN = "Y"` (or "N" or "U") at byte offset 90
When the vitals import processes the record
Then the CouchDB case document has `"mother_married": 1` (or `0` or `7777`) as a JSON number — not the string `"1"`, `"0"`, or `"7777"`
And the front-end dropdown for `mother_married` displays the correct label ("Yes", "No", "Unknown") instead of "Select Value"

**AC-2 — `If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` stored as JSON number after NAT import**
Given a NAT file with `ACKN = "Y"` (or "N", "U", "X") at byte offset 91
When the vitals import processes the record
Then the CouchDB case document has the paternity acknowledgement field stored as a JSON number (e.g., `1` for Y, `0` for N, `7777` for U, `2` for X)
And the front-end dropdown for this field displays the correct label instead of "Select Value"

**AC-3 — FET path: same fields stored as JSON numbers**
Given a FET file whose field set includes `MARN`
When the vitals import processes the FET record via `Parent_FET_IJE_to_MMRIA_Path["MARN"]`
Then the same integer storage fix is applied — the MMRIA field value is a JSON number, not a string

**AC-4 — String-typed fields are not affected**
Given text fields such as `MOMFNAME`, `MOMLNAME`, `MOMMAIDN`, `BRTHCITY`, `UNUM`, `ZIPCODE`, and similar free-text string fields
When the import processes those fields
Then they continue to be stored as JSON strings — no regression

**AC-5 — Existing null / blank handling preserved**
Given a NAT field value that is blank or maps to "9999" by its Rule method
When the import processes it
Then the blank/9999 handling behavior is unchanged — neither stored nor stored as 9999 integer per existing behavior for that field

---

## Dev Notes — Root Cause and Fix

### Root Cause

The `_Rule` methods (`MARN_Rule`, `ACKN_Rule`, etc.) are defined as `public static string` methods in:
```
nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs
```
They are accessed in `BatchItemProcessingService` via:
```csharp
using static mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper;
```
These methods correctly map IJE characters to numeric string codes:
```csharp
public static string MARN_Rule(string value)
{
    // Y → "1", N → "0", U → "7777", default → "9999"
}
public static string ACKN_Rule(string value)
{
    // Y → "1", N → "0", U → "7777", X → "2", default → "9999"
}
```

The defect is that `C_Get_Set_Value.set_value` is declared with `string p_value`:
```csharp
// mmria.common/getset/single_form_value.cs
public bool set_value(string p_metadata_path, string p_value, object p_case, int p_index = -1)
{
    // ...
    val[item_key] = p_value;  // stores .NET string → serializes as JSON string
}
```

When `"mother_married"` is set to .NET string `"0"`, Newtonsoft.Json serializes it as `"0"` (JSON string). The mmria-server writes the same field as `.NET int 0` → `0` (JSON number). The front-end dropdown resolver expects an integer.

### Specific Call Sites to Fix

**File:** `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs`

NAT processing path (around line 1477):
```csharp
// BEFORE — stores JSON string
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ACKN"], field_set["ACKN"], new_case);

// AFTER — must store JSON integer
// (implementation approach is developer's choice — see options below)
```

FET processing path (around line 1794):
```csharp
// BEFORE
gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
// AFTER — same fix applies
```

### Implementation Approach Options

**Option A — `set_value_int` helper method in `BatchItemProcessingService`**
Add a private helper that parses the string to int and sets the value directly on the case dictionary, bypassing `set_value`:
```csharp
private void set_value_int(string mmria_path, string string_value, object case_doc)
{
    if (int.TryParse(string_value, out int parsed))
    {
        // Navigate the path manually and assign as int
        // ...
    }
}
```
Then at the call site:
```csharp
set_value_int(Parent_NAT_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
```

**Option B — Object overload on `C_Get_Set_Value`** (in `mmria.common/getset/single_form_value.cs`):
```csharp
public bool set_value(string p_metadata_path, object p_value, object p_case, int p_index = -1)
{
    // same logic, but assigns object instead of string
    val[item_key] = p_value;
}
```
Then call with the parsed integer:
```csharp
if (int.TryParse(field_set["MARN"], out int marn_val))
    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MARN"], marn_val, new_case);
```

**Option C — Inline dictionary navigation** (most surgical, only touches the exact keys):
```csharp
// Navigate the case document path and assign int directly without using set_value
```

Choose whichever approach is most consistent with existing patterns. Option B is preferred if other integers need fixing across multiple call sites.

### MMRIA Path Reference

| IJE Field | MMRIA Path |
|-----------|-----------|
| `MARN` | `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married` |
| `ACKN` | `birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` |

Both are in `Parent_NAT_IJE_to_MMRIA_Path` (line ~249–250) and `Parent_FET_IJE_to_MMRIA_Path` (line ~406).

### Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs` | Fix `set_value` call sites for MARN, ACKN (NAT + FET paths only) |
| `nccdphp-drh-mmria-common/mmria.common/getset/single_form_value.cs` (conditional) | Add `object` overload to `set_value` only if Option B chosen |
| `nccdphp-drh-mmria-common/mmria.common/getset/multi_form_value.cs` (conditional) | Same object overload if Option B |

### Sequencing

- Independent of all other epics. Can be worked immediately.
- Story 12.2 (data correction migration) fixes existing data already in CouchDB that was corrupted before this fix.

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

### Change Log

| File | Change |
|------|--------|
| | |
