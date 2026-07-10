# Addendum — prd-mmria-2026-06-12

Technical context and implementation notes captured during discovery. This material belongs in architecture and solution design, not the PRD.

---

## Editor Fidelity (FR-1)

**Bug characterization — FR-1.1 (line breaks):**
The defect is on the save/serialization path, not the display path. Content renders correctly in the editor while the user is typing. Line breaks are lost after save and reopen. Print and PDF views display breaks correctly, suggesting the serialization step strips `<br>` or paragraph tags that the print/PDF renderer handles differently. A prior fix has been applied to the save path; status is unconfirmed pending test coverage.

**Bug characterization — FR-1.3 (cut/paste):**
Ctrl+X / Ctrl+V behavior is erratic — content pastes at random positions (observed at lines 1, 4, 5, 6, 8 in a single session) or between words in a different paragraph rather than at the cursor. This is a cursor/selection state management issue in the rich text editor component.

---

## OMB Expiration Date (FR-3.1)

**Current implementation:**
The OMB expiration date is hardcoded as a `label` type field in `metadata.json` (loaded into CouchDB):
```json
{
  "prompt": "Exp. Date 05/31/2026",
  "name": "omb_expiration_label",
  "type": "label",
  "tags": []
}
```
Located in `source-code/mmria/mmria-server/database-scripts/metadata.json`.

The OMB block renders on the Home page and inline in case forms (confirmed: Committee Decisions form). Architect needs to determine the correct mechanism for making this label's value dynamic at render time without breaking the form definition structure.

**Render surfaces confirmed:**
- Home page — OMB block (Form Approved / OMB No. / Exp. Date)
- Committee Decisions form — same OMB block appears inline

---

## MMRIA Version (FR-3.2)

**Render surface confirmed:**
- Application footer only — "MMRIA V4.0.1" (current value as of 2026-06-12)

---

## Core Elements Removal (FR-4)

**Current implementation:**
`core-summary` section key maps to "Core Elements Only" in `getReportTabName()` in `wwwroot/scripts/pdf-version/index.js` (line ~775). Also present in `TitleMap` as `"core-summary": "Core"`. The `formatContent()` function handles `case 'core-summary':` which dispatches to `core_summary()`. Three client-side print dropdown render locations need to be identified before implementation.

PMSS-specific dropdowns are excluded from scope.

---

## Vitals Validation (FR-2)

**Forms and grid names (from index.js):**
- `transport_vital_signs` — Medical Transport form
- `vital_signs` — appears on multiple forms
- `routine_monitoring` — Prenatal form

Architect should confirm the exact grid names on all four targeted forms before implementation.

**Config loading:** Ranges loaded once at server startup into memory. No per-request CouchDB lookups for validation.

---

## Vitals Import Integer Type Fix (FR-12)

**Root cause confirmed (2026-07-07):**
The Rule methods (`MARN_Rule`, `ACKN_Rule`, `MEDUC_Rule`, etc.) ARE defined as `public static string` methods in `mmria.common/SharedLibraries/MMRIAServices/Helper/MMRIAServicesHelper.cs`. `BatchItemProcessingService.cs` accesses them via `using static mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper;` — no stubs, no missing methods.

The actual defect is in `C_Get_Set_Value.set_value()`, which is declared as `set_value(string p_metadata_path, string p_value, object p_case, int p_index = -1)`. It always assigns `val[item_key] = p_value` where `p_value` is a .NET `string`. Newtonsoft.Json serializes this as a JSON string (`"0"`) rather than a JSON number (`0`). mmria-server creates cases via a different code path that preserves integer types.

**Affected call sites in BatchItemProcessingService.cs:**
NAT path (lines ~1477–1479):
```csharp
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);  // ← stores "0" not 0
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ACKN"], field_set["ACKN"], new_case);  // ← stores "1" not 1
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MEDUC"], field_set["MEDUC"], new_case); // ← also not wrapped
```
FET path has a similar issue for MARN (line ~1794).

**Note on `TryPaseToIntOr_DefaultBlank`:** This method (defined in `BatchItemProcessingService` at line ~2691) also returns `string` — it only validates the format and returns the numeric string or a default. Wrapping alone is insufficient; the `set_value` call must ultimately store an integer type.

**Implementation options:**
1. Add `set_value(string path, object value, ...)` overload to `C_Get_Set_Value` that stores `object` as-is — cleanest, enables both string and integer storage
2. Add a `set_value_int(string path, string value, ...)` helper in `BatchItemProcessingService` that parses and stores as int directly in the document dictionary — scoped fix, no mmria.common change
3. At the call sites, parse the string to int and directly set `case_dict[path] = intValue` — most surgical, no new methods

**Field MMRIA paths confirmed (from `Parent_NAT_IJE_to_MMRIA_Path` dictionary):**
- `MARN` → `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married`
- `ACKN` → `birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital`

**Audit scope for FR-12.2:** Other NAT/FET fields not wrapped with `TryPaseToIntOr_DefaultBlank` at their `set_value` call sites that likely need integer storage: `MEDUC`, `FEDUC`, `ATTEND`, `TRAN`, `PAY`, `WIC`. Developer confirms type from mmria-server behavior or metadata.

---

## Data Migration Environment Config (FR-13)

**Current appsettings.json structure in data-migration:**
Uses a flat `data_migration` config section with `couchdb_url`, `timer_user_name`, `timer_value`, `config_id`, `metadata_version`. Jurisdiction lists are hardcoded in `Program.cs` as `run_list`, `test_list`, `prefix_list` static fields. The `ConfigurationSet` is fetched from CouchDB using `config_id`, which is another layer of complexity.

**Target structure (mirrors Replication project):**
- `appsettings.json`: schema with blank defaults (committed)
- `appsettings.local.json`: actual credentials and environment (gitignored)
- `Configuration.cs`: typed `DataMigrationAppConfiguration` with `EnvironmentSettings`, `CouchDBSettings`, `Credentials`, `JurisdictionLists`, `MigrationSettings`
- `Program.cs`: reads `JurisdictionLists[ConfigEnvironment]` for active prefix list

**Key files:**
- `c:\repos\nccdphp-drh-mmria-utilities\Replication\Configuration.cs` — source model to mirror
- `c:\repos\nccdphp-drh-mmria-utilities\Replication\appsettings.json` — source schema to adapt
- `c:\repos\nccdphp-drh-mmria-utilities\data-migration\Program.cs` — main file to refactor
- `c:\repos\nccdphp-drh-mmria-utilities\data-migration\appsettings.json` — to be replaced

**CouchDB config_id / ConfigurationSet:** The current code fetches a `ConfigurationSet` from CouchDB using `config_id` and then selects the `DBConfigurationDetail` for each prefix (URL, username, password). This complexity is replaced by the environment-keyed `Credentials` dictionary in the new config model.

---

## Vitals Retrospective Data Correction (FR-14)

**Migration infrastructure reference:** `VitalsMigration01` in `data-migration/migration-set/vVitalsMigration01.cs` — this is the pattern for new migrations (constructor-injected config, `execute()` async method, `is_report_only_mode` flag, `output_builder` for logging).

**New migration:** `VitalsTypeCorrection` — iterates all case documents in the target database, checks each target field path, replaces `string` values that parse as integers with their integer equivalents. Depends on FR-13 for environment targeting.

---

## Vitals Import Type Normalization (FR-12)

**Defect location:** `nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs`

**Build defect (secondary):** `#region NAT Rules` (line ~4282) and `#region FET Rules` are empty stubs. The following methods are called in the NAT parse path but have no definitions, meaning a clean build of `mmria.services` fails:
- `MARN_Rule`, `ACKN_Rule`, `MEDUC_Rule`, `NPREV_Rule`, `ANTB_NAT_Rule`
- `TB_NAT_Rule`, `MDOB_YR_Rule`, `MDOB_MO_Rule`, `MDOB_DY_Rule`
- `FDOB_YR_Rule`, `FDOB_MO_Rule`, `BPLACEC_ST_TER_NAT_Rule`, `NAT_STATEC_Rule`

The project currently links against a previously-compiled binary; the missing definitions are masked by incremental build caching.

**Primary type defect:** At the `gs.set_value()` call sites (lines ~1477–1478), `MARN` and `ACKN` are passed as raw strings from the parse dictionary:
```csharp
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ACKN"], field_set["ACKN"], new_case);
```
Other numeric fields (e.g., `DOD_MO`, `DOB_MO`, `IDOB_MO`, `NPREV`, `HIN`) use the helper:
```csharp
gs.set_value(path, TryPaseToIntOr_DefaultBlank(field_set["DOD_MO"]), new_case);
```
The fix is to apply `TryPaseToIntOr_DefaultBlank()` at the `MARN` and `ACKN` call sites and to any other NAT/FET numeric fields the developer confirms are affected.

**MMRIA field paths confirmed from ticket:**
- `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married` ← IJE field `MARN`, NAT record offset 90 length 1
- `birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` ← IJE field `ACKN`, NAT record offset 91 length 1

**FET parallel:** `MARN` also appears at line ~1794 in the FET processing path — same fix applies there.

**Resolution approach (from ticket comments, Jun 12):** Fix code in vitals import first; then run data migration (FR-14) as a second pass to repair existing bad data.

---

## Data Migration Project Configuration (FR-13)

**Current state:** `data-migration/Program.cs` hardcodes `run_list`, `test_list`, and `prefix_list` in source. `appsettings.json` uses a flat `mmria_settings` / `data_migration` key structure. Environment targeting requires a code edit and rebuild.

**Reference implementation:** `nccdphp-drh-mmria-utilities/Replication/` — `appsettings.json`, `appsettings.local.json`, `Configuration.cs`. The `appsettings.local.json` in Replication carries active credentials (gitignored); `appsettings.json` carries the schema template.

Key Replication configuration classes to mirror verbatim: `AppConfiguration`, `EnvironmentSettings`, `CouchDBSettings`, `DatabaseUrlTemplates`, `CredentialConfig`, `JurisdictionLists`. Valid `ConfigEnvironment` values: `LOCALHOST`, `DEV`, `QA`, `INT`, `PROD`.

**Migration Settings addition:** The existing `RunTypeEnum` and `is_report_only_mode` bool in `Program.cs` should be lifted into a `MigrationSettings` config section so they can be controlled via `appsettings.local.json` without a code change between runs.

---

## Vitals Import Retrospective Migration (FR-14)

**Scope:** Only case documents in the `{prefix}mmrds` databases are in scope — not metadata or configuration documents.

**CouchDB traversal pattern:** The existing migrations use `_all_docs?include_docs=true` with pagination or `_find` with a selector. The same approach applies. Reference `VitalsMigration01.cs` for the document traversal and update pattern already in the project.

**Field path traversal:** The affected field paths are nested (e.g., `birth_fetal_death_certificate_parent/demographic_of_mother/mother_married`). The existing migration infrastructure (`mmria.common.couchdb` helpers or the dynamic ExpandoObject traversal pattern in `Program.cs`) handles nested path navigation — use the same mechanism.

**Idempotency:** The migration is safe to re-run. Documents already corrected (field is already an integer) are skipped by the `int.TryParse` guard in FR-14.3.

**Order of operations:** Run FR-14 after FR-12 is deployed to production. Running before FR-12 is deployed would be corrected immediately by the next import of the same records — acceptable but wasteful.
