# Validator.js Management and Deployment Context

- Status: Active
- Scope: How `validator.js` is structured, where it lives, how it flows from base metadata to the case editor, and how to update or repair it.
- Last verified: 2026-07-10

---

## What validator.js does

`validator.js` is a generated JavaScript file that wires up the client-side validation engine for the case editor. It is served as a CouchDB attachment and injected into the case page as a `<script>` tag.

On load it sets four global maps:
- `path_to_int_map` — maps full metadata path strings to integer IDs
- `dictionary_path_to_path_map` — maps slug/field-name paths to `g_metadata` dotted paths
- `path_to_onclick_map` / `path_to_onfocus_map` / `path_to_onblur_map` — event handler name maps

Any path present in `g_metadata` that is missing from `path_to_int_map` will crash `page_renderer.js` at the line:

```js
// source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.js ~line 606
let f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_of";
// TypeError: Cannot read properties of undefined (reading 'toString')
```

---

## How it is loaded

1. `Views/Case/Index.cshtml` (line ~152) emits:
   ```html
   <script src="./api/version/@metadata_version/validation" type="text/javascript"></script>
   ```
2. `@metadata_version` is resolved by `versionController.release_version()`, which reads `metadata_version` from the CouchDB `configuration/dev_cluster` document at runtime (not from `appsettings`).
3. The `api/version/{id}/validation` route delegates to `MetadataVersionManager.GetVersionDocumentAsync`, which fetches:
   ```
   {couchdb_url}/metadata/version_specification-{id}/validation
   ```

---

## CouchDB document layout

| Document ID | Attachment name | Role |
|---|---|---|
| `2016-06-12T13:49:24.759Z` | `validator.js` | Base/seed validator. Used by version-manager to seed new version specifications. |
| `version_specification-26.06.15` | `validation` | Active validator served to the case editor for version 26.06.15. |

`DefaultMetadataId = "2016-06-12T13:49:24.759Z"` and `ValidatorAttachmentName = "validator.js"` are constants in:
```
nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MetadataVersion/Manager/MetadataVersionManager.cs
```

---

## Repo source file

```
source-code/mmria/mmria-server/wwwroot/scripts/validator.js
```

This is the **canonical repo copy** and must be kept in sync with the base CouchDB attachment. As of 2026-07-10 it is 504,810 bytes / 4,756 lines.

**Do not use** `source-code/mmria/mmria-server/database-scripts/validator.js` as the source — that file is a shorter, older seed artifact used only for initial DB bootstrapping and is missing many `path_to_int_map` entries.

---

## Known recurring bug: field-name casing

The `dictionary_path_to_path_map` entry for the paternity acknowledgement field has been observed with an uppercase-initial `I` in some generated versions:

```js
// BAD — uppercase I — will not match C# model field name
dictionary_path_to_path_map['birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital']

// CORRECT — lowercase i
dictionary_path_to_path_map['birth_fetal_death_certificate_parent/demographic_of_mother/if_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital']
```

After any download or regeneration, always verify:
```powershell
Select-String -Path "...\wwwroot\scripts\validator.js" -Pattern "If_mother_not_married" -CaseSensitive
# Should return 0 matches
```

---

## How to update validator.js

### Step 1 — Download from the reference environment

Use `curl.exe --compressed`. **Do not use `Invoke-WebRequest`** — PowerShell 5 `$response.Content` returns the raw gzip-compressed bytes and writes a corrupted file at ~304 KB instead of the real ~505 KB file.

```powershell
$destFile = "c:\repos\nccdphp-drh-mmria\source-code\mmria\mmria-server\wwwroot\scripts\validator.js"
curl.exe --compressed -s -o $destFile https://test-mmria.apps.ecpaas-dev.cdc.gov/api/version/26.06.15/validation
(Get-Item $destFile).Length   # expect ~504810
(Get-Content $destFile).Count # expect ~4756
```

### Step 2 — Fix the casing bug if present

```powershell
Select-String -Path $destFile -Pattern "If_mother_not_married" -CaseSensitive
# If any results: open the file and replace the uppercase I with lowercase i (one line in dictionary_path_to_path_map)
```

### Step 3 — Push to the CouchDB base document

Use `curl.exe --data-binary` to push the full file without body truncation. `Invoke-RestMethod -Body` truncates large payloads.

```powershell
$creds = "mmrds:<password>"   # substitute actual password

# Get current _rev
$baseRev = (curl.exe -s -u $creds "http://localhost:5984/metadata/2016-06-12T13%3A49%3A24.759Z" | ConvertFrom-Json)._rev

# Push
curl.exe -s -u $creds -X PUT `
    "http://localhost:5984/metadata/2016-06-12T13%3A49%3A24.759Z/validator.js" `
    -H "If-Match: $baseRev" `
    -H "Content-Type: application/javascript" `
    --data-binary "@$destFile"
# Expect: {"ok":true,...}

# Verify size
$att = (curl.exe -s -u $creds "http://localhost:5984/metadata/2016-06-12T13%3A49%3A24.759Z" | ConvertFrom-Json)._attachments."validator.js"
"CouchDB: $($att.length)  Local: $((Get-Item $destFile).Length)  Match: $($att.length -eq (Get-Item $destFile).Length)"
```

### Step 4 — Use version-manager to propagate

Once the base document is correct, use the `/version-manager` UI to regenerate `version_specification-26.06.15/validation` from the base rather than pushing directly to the version-specific document.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `TypeError: Cannot read properties of undefined (reading 'toString')` in `page_renderer.js` | A metadata path is missing from `path_to_int_map` in the active validator | Update the active `version_specification-{id}/validation` attachment via version-manager or direct push |
| Active validator is shorter than expected (~304 KB instead of ~505 KB) | `Invoke-WebRequest` wrote gzip bytes; or the old `database-scripts/validator.js` was pushed instead of `wwwroot/scripts/validator.js` | Re-download with `curl.exe --compressed` and re-push with `curl.exe --data-binary` |
| Casing mismatch on `if_mother_not_married...` | Regenerated validator retained uppercase `I` | Apply Step 2 fix and re-push |
