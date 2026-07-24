# Story 32.3 — Investigate Hospital Paternity Field Code Discrepancy

**Epic:** 32 — Export Consistency — Date Format, De-identification Parity, and Hospital Code Normalization
**Story ID:** 32.3
**Status:** done

---

## User Story

As a data quality officer,
I want to know why `bfdcpdom_imnmhpabsit_hospi` shows specific coded values in FL exports but `9999` in T1 exports for the same 213 cases,
So that the correct behavior is documented and I know whether a code fix or a data correction is required.

---

## Context

After updating the global de-identification list (Story 32.2), a re-export comparison shows `bfdcpdom_imnmhpabsit_hospi` ("If mother not married, has paternity acknowledgement been signed in the hospital") still differs in 213 cases:

| FL value | T1 value | Cases |
|---|---|---|
| `1` (Yes) | `9999` (blank) | ~50 |
| `2` (No) | `9999` (blank) | ~100 |
| `7777` (Unknown) | `9999` (blank) | ~63 |

The `9999` sentinel in MMRIA means "left blank / not answered." Two hypotheses:

**H1 — Data discrepancy (most likely):** T1's CouchDB documents have `9999` (or null) stored for this field on those 213 cases because the test data predates the NAT import integer-type fix (Story 11.1). Story 11.1 specifically addressed this exact field: after a NAT import, `ACKN` values were stored as JSON strings rather than integers, and subsequent data migration corrected them. If T1's data was never run through that migration, or was seeded from a pre-migration snapshot, the values would be absent or `9999`.

**H2 — Exporter transform (unlikely):** FL or T1 applies a post-read transform that converts specific values to `9999`, or vice versa, for this field. No such transform was found during code review of `mmrds_exporter.cs`, but must be confirmed.

**Representative differing case IDs for investigation:**
- `21735923-60c8-d814-f363-7fdeb539c919` — FL=`1`, T1=`9999`
- `54f85b4f-d979-49f5-94ba-2b6126a3d8dc` — FL=`7777`, T1=`9999`
- `77cd7fd7-137f-4fdf-bee5-20b00983a28e` — FL=`2`, T1=`9999`

---

## Acceptance Criteria

**AC-1 — Root cause documented**
Given the investigation is complete
When the findings are recorded
Then the determination is clearly one of:
- "Data discrepancy: T1's CouchDB documents have `9999` stored; FL's have specific values. No code change required."
- "Exporter transform: both CouchDB documents have the same raw value but export differently. A code fix story is needed."

**AC-2 — If H1 confirmed (data discrepancy)**
Given T1's CouchDB document for case `21735923-60c8-d814-f363-7fdeb539c919` is fetched directly
When the path `birth_fetal_death_certificate_parent/demographic_of_mother/if_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` is inspected
Then the stored value is `9999`, `"9999"`, or absent (confirming blank)

**AC-3 — If H1 confirmed: data correction plan documented**
Given root cause is data discrepancy
When the 213 cases are confirmed to have blank values in T1's CouchDB
Then a note records whether these cases need the vitals type correction migration run against T1, or whether the test data is expected to differ and the discrepancy is acceptable for T1

**AC-4 — If H2 confirmed (exporter transform)**
Given root cause is a code path difference
When the differing code path is located
Then a follow-up fix story (32.4) is created before this epic is closed

---

## Investigation Steps

1. **Fetch the raw CouchDB document from T1** for case `21735923-60c8-d814-f363-7fdeb539c919`:
   ```
   GET http://localhost:<t1-couchdb-port>/mmrds/21735923-60c8-d814-f363-7fdeb539c919
   ```
   Inspect `birth_fetal_death_certificate_parent` → `demographic_of_mother` → `if_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital`.

2. **Compare to the same case in FL.** If access to the FL CouchDB instance is available, fetch the same document and compare the raw field value. If FL access is unavailable, the FL export CSV value of `1` is sufficient evidence — the export path for list fields renders the raw value, so if FL exports `1`, FL's CouchDB has `1`.

3. **Check whether the value is stored as integer or string in T1.** This field is known from Story 11.1 to be susceptible to the NAT import string-vs-integer type issue. If T1 stores `"9999"` (string) while FL stores `1` (integer), the root cause is confirmed as the pre-Story-11.1 data state.

4. **Search for any special-casing of this path in the exporter.** Run:
   ```
   grep -rn "if_mother_not_married" nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/
   ```
   If no results, H2 is eliminated.

5. **Check the metadata type for this field:**
   ```
   grep -A5 "imnmhpabsit_hospi" source-code/mmria/mmria-server/database-scripts/metadata.json
   ```
   Confirm whether the field type is `"list"` and `data_type` is absent or `"number"` — which would explain why `9999` is NOT converted to empty string (the `data_type == "string"` sentinel conversion check in the exporter would not fire for numeric list fields).

6. **Record findings** in a brief note appended to this story or in the epic's findings log.

---

## Dev Notes

**Story 11.1 connection:** Story 11.1 specifically fixed `if_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` being stored as a JSON string after NAT import. Story 12.2 (`12-2-vitals-type-correction-migration.md`) included a data migration to correct existing affected records. If T1's case data was seeded from a snapshot taken before Story 12.2 was run against it, those 213 cases would still have the old (blank/`9999`) value.

**Why 9999 appears as `9999` in the export (not empty):** The exporter converts `9999` to empty string only for list fields where `path_to_node_map[path].data_type?.ToLower() == "string"`. If the metadata declares this field without `data_type` or with `data_type: "number"`, the `9999 → ""` conversion is skipped and `9999` is written as-is. This is not a bug — it is the intended behavior for numeric-coded list fields where `9999` is a valid display sentinel.

**Scope of this story:** Investigation and documentation only. If H1 is confirmed, no code change is needed. If H2 is confirmed, scope the fix in a follow-up Story 32.4.

---

## Findings (2026-07-24)

### Root Cause — Metadata Field Name Casing Discrepancy

**Neither H1 nor H2 as originally framed. The actual cause is H3: a metadata `name` casing defect.**

**Investigation results:**

1. **No exporter special-casing found (H2 eliminated).** `grep` for `if_mother_not_married` in `mmria.services/Utilities/Exporter/` returned zero results. The exporter treats this field identically to all other `data_type: "number"` list fields: it writes the raw stored value, or `9999` when the path is not found.

2. **Metadata confirms `data_type: "number"`.** The `imnmhpabsit_hospi` field in `metadata.json` is `type: "list"`, `data_type: "number"`. The exporter's `9999 → ""` sentinel conversion is guarded by `data_type == "string"`, so it does **not** fire for this field — `9999` is written as-is. This is correct behavior, not a bug.

3. **T1 CouchDB document inspection.** All three representative cases were fetched directly from T1 (`tenant1-couchdb.local:6984`):

   | Case ID | T1 stored key | T1 stored value | FL export |
   |---------|--------------|-----------------|-----------|
   | `21735923-60c8-d814-f363-7fdeb539c919` | `If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` | `1.0` | `1` |
   | `54f85b4f-d979-49f5-94ba-2b6126a3d8dc` | `If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` | `7777.0` | `7777` |
   | `77cd7fd7-137f-4fdf-bee5-20b00983a28e` | `If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` | `2.0` | `2` |

   The values ARE present in T1's CouchDB — but stored under the key `If_mother_not_married...` **(capital I)**. The exporter path lookup is case-sensitive and uses the metadata `name` value `if_mother_not_married...` (lowercase). The lookup misses, the field resolves to `null` / not-found, and the exporter outputs `9999`.

4. **Root cause confirmed.** The metadata `name` property for this field was previously `If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital` (capital I). Data entered or imported while that incorrect casing was in effect was written to CouchDB under the capitalized key. FL's data was either entered or imported after the field name was corrected to all-lowercase, so FL's documents store the value under the correct key and export correctly.

5. **Fix status.** The metadata `name` has been corrected to all-lowercase in the dev environment and will ship in the v4.1 release. No exporter code change is required.

### AC Determination

- **AC-1:** Root cause is **metadata field name casing discrepancy**. Data values ARE present in T1's documents but stored under the old capitalized key. The exporter's case-sensitive path lookup fails for those documents. The metadata fix (all-lowercase) is already applied in dev and ships in v4.1. **No code change required for the exporter.**

- **AC-2 / AC-3:** T1's documents do not have `9999` stored for this field — they have the correct values (`1`, `2`, `7777`) stored under the old capitalized key. The export discrepancy will persist for historical T1 test data that was written under the old key. This is acceptable: test data seeded before the metadata correction is expected to differ. A key-rename migration would be required to fix the export output for those pre-correction documents; that work is out of scope for the v4.1 release.

- **AC-4:** H2 is eliminated. No follow-up Story 32.4 is needed.
