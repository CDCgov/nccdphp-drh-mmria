# Story 12.2 — Vitals Retrospective Data Correction Migration

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.2
**Status:** done
**Date added:** 2026-07-07
**Depends on:** Story 12.1 (data-migration environment config)

---

## User Story

As a database administrator,
After the vitals import integer type fix has been deployed (Story 11.1),
I want to run a targeted migration that converts previously imported string values to integers for the affected dropdown fields,
So that cases imported before the fix display the correct dropdown labels instead of "Select Value".

---

## Acceptance Criteria

**AC-1 — Migration is invocable via `RunType = "VitalsTypeCorrection"` in appsettings.local.json**
Given `appsettings.local.json` has `MigrationSettings.RunType = "VitalsTypeCorrection"`
When the migration runs
Then `Program.cs` dispatches to the `VitalsTypeCorrectionMigration` class
And the `RunTypeEnum` contains a `VitalsTypeCorrection` member

**AC-2 — String-valued integer fields are converted to integers**
Given a CouchDB case document where `mother_married` is stored as `"0"` (JSON string)
When the migration processes that document
Then `mother_married` is updated to `0` (JSON number) in the saved document
And the change is logged in `output_builder` as `"record_id: {id} [path] converted: \"0\" => 0"`

**AC-3 — Both MARN and ACKN target paths are corrected**
Given the migration executes against a database
When it processes each case document
Then it checks and corrects `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married`
And it checks and corrects `birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital`
And no other paths are modified

**AC-4 — Fields already stored as integers are skipped**
Given a case document where `mother_married` is already `0` (JSON number)
When the migration processes that document
Then no write is performed for that field
And the log does not mention that field for that document

**AC-5 — Fields with null or absent values are skipped**
Given a case document where `mother_married` is `null` or the path does not exist
When the migration processes that document
Then no write is performed for that field
And no error is thrown

**AC-6 — Report-only mode prevents writes**
Given `MigrationSettings.IsReportOnlyMode = true` in config
When the migration runs
Then no `PUT` or `PATCH` is issued to CouchDB
And the console and `output_builder` log show what would have been changed

**AC-7 — Non-integer strings are logged and skipped**
Given a case document where `mother_married` is stored as `"Y"` (a non-numeric string)
When the migration processes it
Then no write is performed
And the log records `"WARNING: record_id: {id} [path] value \"Y\" is not a parseable integer — skipping"`
And the migration continues to the next document without error

**AC-8 — Uses environment and credentials from Story 12.1 config**
Given the configuration loaded by the refactored `Program.cs` (Story 12.1)
When the migration iterates databases
Then the target DB URL and credentials come from `config.CouchDBSettings.DatabaseUrlTemplates[ConfigEnvironment]` and `config.Credentials[ConfigEnvironment]`
And the prefix list comes from `config.JurisdictionLists[ConfigEnvironment]`

---

## Dev Notes — Implementation

### New File: `migration-set/VitalsTypeCorrectionMigration.cs`

**Pattern:** Mirror `VitalsMigration01` constructor and `execute()` shape.

The key difference from `VitalsMigration01`: this migration does NOT need metadata lookup or race-recode logic. It only needs to:
1. Fetch `_all_docs` from the target database
2. For each case document, for each target path:
   - Get current value
   - If it is a .NET `string` and parses as `int` → store as `int`
   - If already `int` or null → skip
   - If string but not parseable as int → warn and skip
3. If any field changed and not report-only → save the document

**Constructor signature (mirrors VitalsMigration01 but without `ConfigurationSet`):**
```csharp
public VitalsTypeCorrectionMigration(
    string p_host_db_url,
    string p_db_name,
    string p_config_timer_user_name,
    string p_config_timer_value,
    System.Text.StringBuilder p_output_builder,
    bool p_is_report_only_mode,
    string p_state_prefix
)
```

**`execute()` implementation sketch:**
```csharp
public async Task execute()
{
    var migration_name = "VitalsTypeCorrectionMigration";
    output_builder.AppendLine($"{migration_name} started at: {DateTime.Now:o}");

    string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";
    var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
    string responseFromServer = await case_curl.executeAsync();

    var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<
        mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

    var gs = new C_Get_Set_Value(output_builder);

    var target_paths = new List<string>
    {
        "birth_fetal_death_certificate_parent/demographic_of_mother/mother_married",
        "birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital"
    };

    foreach (var case_item in case_response.rows)
    {
        var doc = case_item.doc;
        if (doc == null) continue;

        var id_result = gs.get_value(doc, "_id");
        var doc_id = id_result.result?.ToString();
        if (doc_id?.Contains("_design") == true) continue;

        bool case_changed = false;

        foreach (var path in target_paths)
        {
            var value_result = gs.get_value(doc, path);
            if (value_result.is_error || value_result.result == null) continue;

            if (value_result.result is string str_value)
            {
                if (int.TryParse(str_value, out int int_value))
                {
                    gs.set_objectvalue(path, int_value, doc);
                    output_builder.AppendLine($"record_id: {doc_id} [{path}] converted: \"{str_value}\" => {int_value}");
                    case_changed = true;
                }
                else
                {
                    output_builder.AppendLine($"WARNING: record_id: {doc_id} [{path}] value \"{str_value}\" is not a parseable integer — skipping");
                }
            }
            // else: already int, bool, null etc. — skip silently
        }

        if (!is_report_only_mode && case_changed)
        {
            await new SaveRecord(host_db_url, db_name, config_timer_user_name, config_timer_value, output_builder)
                .save_case(doc as IDictionary<string, object>, migration_name);
        }
    }

    output_builder.AppendLine($"{migration_name} Finished {DateTime.Now:o}");
    Console.WriteLine($"{migration_name} Finished {DateTime.Now}");
}
```

**Note on `set_objectvalue`:** `VitalsMigration01` uses `gs.set_objectvalue(path, new_value, doc)` to store non-string values (e.g., `List<object>`). Use the same method to store the `int`. Confirm the signature in `mmria.common/getset/single_form_value.cs` — it should be `set_objectvalue(string path, object value, object case_doc)`. If this method doesn't handle `int` as a stored type, it may need to be tested; fallback is direct dictionary navigation.

### Adding `VitalsTypeCorrection` to `RunTypeEnum` in `Program.cs`

In the refactored `Program.cs` (Story 12.1), the `RunTypeEnum` must include:
```csharp
enum RunTypeEnum
{
    OnBoarding,
    DataMigration,
    OneTime,
    MMRDSImport,
    VitalsTypeCorrection   // ← add this
}
```

And the dispatch block must include a case:
```csharp
case RunTypeEnum.VitalsTypeCorrection:
    var vitals_type_correction = new VitalsTypeCorrectionMigration(
        db_url,
        db_name,
        username,
        password,
        output_builder,
        is_report_only_mode,
        prefix
    );
    await vitals_type_correction.execute();
    break;
```

### Target Field MMRIA Paths

| IJE Origin | MMRIA Path |
|------------|-----------|
| `MARN` | `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married` |
| `ACKN` | `birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` |

These are the only two paths with confirmed defects from Bug 117351. The migration scope is intentionally narrow.

### Important Notes

- Run with `IsReportOnlyMode = true` first in a non-production environment to audit the scope of affected records
- After confirming report output is correct, re-run with `IsReportOnlyMode = false`
- The migration is idempotent — running it twice on corrected data is safe (already-int values are skipped)
- `_design` documents are always skipped (same as `VitalsMigration01`)

### Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-utilities/data-migration/migration-set/VitalsTypeCorrectionMigration.cs` | **New file** — migration class |
| `nccdphp-drh-mmria-utilities/data-migration/Program.cs` | Add `VitalsTypeCorrection` to `RunTypeEnum`; add dispatch case in migration loop |

### Sequencing

- **Must follow Story 12.1** (config infrastructure must exist first)
- Can be worked concurrently with Story 11.1 (different project)
- This story fixes historical data; Story 11.1 fixes the import going forward — both are needed

---

## Dev Agent Record

### Completion Notes

- All 8 ACs verified against the current codebase.
- `VitalsTypeCorrectionMigration.cs` was implemented using `CouchDbHttpClient` (FR-15 pattern), not the `cURL` pattern shown in the story's dev notes — this is correct and more current.
- `RunTypeEnum.VitalsTypeCorrection` is present in `Program.cs` (line 34). Dispatch and instantiation are wired correctly (lines 146–157), passing `couchDbHttpClient` from DI.
- Program.cs uses the FR-13 typed config for `db_url`, `username`/`password`, and the prefix list — AC-8 confirmed.
- The dev notes constructor signature omits `CouchDbHttpClient` but the actual implementation includes it; no discrepancy in behavior.
- Note: FR-17 hardening (retry-on-409, `SaveResult` enum, pre-flight offline check, run summary) is tracked separately and does **not** block this story's completion — those are new requirements added after this story was written.

### Change Log

| File | Change |
|------|--------|
| `data-migration/migration-set/VitalsTypeCorrectionMigration.cs` | New file — migration class (uses `CouchDbHttpClient`) |
| `data-migration/Program.cs` | `VitalsTypeCorrection` added to `RunTypeEnum`; dispatch case and instantiation added |
