# Story 32.1 — Normalize Datetime Serialization in CSV Export

**Epic:** 32 — Export Consistency — Date Format, De-identification Parity, and Hospital Code Normalization
**Story ID:** 32.1
**Status:** ready

---

## User Story

As a data consumer,
I want all timestamp columns in every MMRIA CSV export to use a fixed, unambiguous format,
So that date parsing never depends on which server locale or culture produced the file.

---

## Context

Four columns in `mmria_case_export.csv` and `data_migration_history.csv` render timestamps differently between the FL production server and a T1 local server:

| Column | FL value | T1 value | Rows affected |
|---|---|---|---|
| `d_creat` | `01/30/2018 21:46:38` | `1/30/2018 4:46:38 PM` | 1,695 |
| `dl_updat` | `10/27/2020 18:00:50` | `10/27/2020 2:00:50 PM` | 1,695 |
| `hr_vitals_imp_date` | `09/25/2023 16:33:37` | `9/25/2023 4:33:37 PM` | 459 |
| `dlc_out` | `07/22/2021 18:20:31` | `7/22/2021 2:20:31 PM` | 53 |

**Root cause:** These fields have metadata type `"datetime"`. Newtonsoft.Json deserializes their ISO 8601 values from CouchDB JSON (e.g., `"2018-01-30T21:46:38.000Z"`) into `System.DateTime` objects. The flat-field `switch` in both exporters has no explicit `case "datetime":` — datetime paths fall through to `default:`, which ends with `row[file_field_name] = val`. When the `DataRow` (typed as `string`) coerces the `DateTime` to string, it calls the system default `DateTime.ToString()`, which is culture-dependent. FL's server renders zero-padded 24-hour; T1's server renders unpadded 12-hour AM/PM. The agreed canonical format is `MM/dd/yyyy HH:mm:ss` (matching current FL production output).

---

## Acceptance Criteria

**AC-1 — Consistent format regardless of server locale**
Given any MMRIA CSV export is generated
When a field has metadata type `"datetime"` and the CouchDB document contains a parseable ISO 8601 timestamp
Then the exported CSV cell contains the value formatted as `MM/dd/yyyy HH:mm:ss`

**AC-2 — Four affected columns match FL production format**
Given `d_creat`, `dl_updat`, `hr_vitals_imp_date`, `dlc_out` in exports from T1 or any local environment
When compared against the same case in an FL export
Then the timestamp strings are identical in format: zero-padded month/day, 24-hour time, no AM/PM

**AC-3 — Null/absent datetime fields are still empty**
Given a case document where a `datetime` field is absent or null
When exported
Then the CSV cell is empty (no change from current behavior)

**AC-4 — Non-datetime default-path fields are unaffected**
Given any field that falls through to the `default:` case with a non-DateTime value (string, textarea, etc.)
When exported
Then it renders exactly as before — no regression

**AC-5 — Both exporters apply the fix**
Given an export triggered via either `mmrds_exporter.cs` or `exporter.cs`
When datetime fields are written
Then both apply the explicit `MM/dd/yyyy HH:mm:ss` format string

---

## Tasks / Subtasks

- [ ] Fix UTC timezone handling in `mmrds_exporter.cs` (AC-1, AC-2)
  - [ ] File: `nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/mmrds_exporter.cs`
  - [ ] Just before the `await foreach(string case_id ...)` loop, add a `JsonSerializerSettings` with `DateTimeZoneHandling.Utc` and pass it to `DeserializeObject`:
    ```csharp
    var _utcSettings = new Newtonsoft.Json.JsonSerializerSettings { DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc };
    await foreach(string case_id in get_case_ids_to_process())
    {
        System.Dynamic.ExpandoObject case_row = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(await _caseRepository.GetCaseDocumentJsonAsync(case_id, db_config), _utcSettings);
    ```
  - [ ] This prevents UTC-marked timestamps (stored with `Z` in CouchDB) from being converted to the server's local timezone during deserialization

- [ ] Fix UTC timezone handling in `exporter.cs` (AC-1, AC-2)
  - [ ] File: `nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/exporter.cs`
  - [ ] Same change before the `await foreach(string case_id ...)` loop:
    ```csharp
    var _utcSettings = new Newtonsoft.Json.JsonSerializerSettings { DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc };
    await foreach(string case_id in get_case_ids_to_process())
    {
        System.Dynamic.ExpandoObject case_row = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(await _caseRepository.GetCaseDocumentJsonAsync(case_id, db_config), _utcSettings);
    ```

- [ ] Fix datetime serialization in `mmrds_exporter.cs` (AC-1, AC-2, AC-3, AC-4, AC-5)
  - [ ] File: `nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/mmrds_exporter.cs`
  - [ ] Locate the flat-field `switch` statement (around line 441) inside the main case-processing loop
  - [ ] In the `default:` case (line ~646), find the final row assignment just before `break`:
    ```csharp
    string file_field_name = path_to_field_name_map[path];
    row[file_field_name] = val;
    ```
  - [ ] Replace with:
    ```csharp
    string file_field_name = path_to_field_name_map[path];
    row[file_field_name] = val is System.DateTime dt ? dt.ToString("MM/dd/yyyy HH:mm:ss") : val;
    ```

- [ ] Fix datetime serialization in `exporter.cs` (AC-1, AC-2, AC-3, AC-4, AC-5)
  - [ ] File: `nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/exporter.cs`
  - [ ] Locate the flat-field `switch` statement (around line 688) inside the main case-processing loop
  - [ ] In the `default:` case (line ~857), find the final row assignment just before `break`:
    ```csharp
    string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
    row[file_field_name] = val;
    ```
  - [ ] Replace with:
    ```csharp
    string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
    row[file_field_name] = val is System.DateTime dt ? dt.ToString("MM/dd/yyyy HH:mm:ss") : val;
    ```

- [ ] Verify no regression to grid-row DateTime handling (AC-4)
  - [ ] The grid-row switch (in `mmrds_exporter.cs` around line 2226) already uses `val.ToString("o")` for DateTime — confirm this line is **not** changed by this story

- [ ] Build and verify (AC-1 through AC-5)
  - [ ] Run a de-identified export from T1/local
  - [ ] Confirm `d_creat`, `dl_updat`, `hr_vitals_imp_date`, `dlc_out` columns match format `MM/dd/yyyy HH:mm:ss` in the output CSV
  - [ ] Build succeeds: `dotnet build nccdphp-drh-mmria-services/mmria.services/mmria.services.csproj`

---

## Dev Notes

**Why `val is System.DateTime dt`?**
Newtonsoft.Json's `ExpandoObject` deserialization converts ISO 8601 strings to `System.DateTime` automatically when the value is recognized as a date. The `"datetime"` metadata type falls through `default:` because there is no explicit `case "datetime":` — the switch currently dispatches only `"group"`, `"number"`, and `"list"` explicitly. Adding the inline type check at the point of row assignment is the minimal, safe change: it only affects values that are already DateTime objects, leaving all string/number/etc. values untouched.

**Two-part fix (both required):**

**Part 1 — `DateTimeZoneHandling.Utc` on deserialization:** Newtonsoft's default `DateTimeZoneHandling` is `Local`, which converts UTC-marked timestamps (`"2018-01-30T21:46:38Z"`) to the server's local timezone when building the `ExpandoObject`. On FL's UTC server, no conversion occurs. On any non-UTC server (Eastern, etc.), UTC dates are shifted to local time, producing a different value. Setting `DateTimeZoneHandling.Utc` keeps all UTC-marked dates as UTC regardless of server timezone. Dates stored without a timezone marker are unaffected (they deserialize as `DateTimeKind.Unspecified`).

**Part 2 — Explicit `ToString("MM/dd/yyyy HH:mm:ss")` on row assignment:** Even with UTC-correct `DateTime` objects, without an explicit format string the `DataRow` calls `DateTime.ToString()` using the server's current culture — producing `M/d/yyyy h:mm:ss AM/PM` on some locales. The explicit format string locks in canonical zero-padded 24-hour output on any server.

**Why only some rows matched after Part 2 alone:** Rows stored with `Z` in CouchDB still had different VALUES (UTC vs. Eastern offset) even though the format was now consistent. Rows stored without a timezone marker already had matching values and were fixed by Part 2 alone.
