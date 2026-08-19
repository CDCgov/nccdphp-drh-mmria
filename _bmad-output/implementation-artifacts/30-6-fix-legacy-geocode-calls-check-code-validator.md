# Story 30.6: Fix Legacy Geocode Calls in mmria-check-code.js and validator.js

Status: in-progress

## Story

As a developer,
I want the legacy geocode call sites in the four on-disk copies of `validator.js` / `mmria-check-code.js` / `MMRIA_calculations.js` to use the new server endpoint with `census_year`, and the CouchDB `metadata` document's `validator.js` and `mmria-check-code.js` attachments updated via the `/version-manager` UI,
so that census tract results are not stale and every runtime path — DB seed, edit-mode case page, editor preview — invokes the same server geocode endpoint.

## Background — file layout you MUST understand before starting

There are **four** on-disk JS files in scope, not two. The story index and the earlier draft of this story listed the paths incorrectly. The actual layout at HEAD (verified 2026-08-19):

| # | On-disk path | Size @ HEAD | Legacy `get_geocode_info` calls | Runtime role |
|---|---|---|---|---|
| A | [`source-code/mmria/mmria-server/wwwroot/scripts/validator.js`](../../source-code/mmria/mmria-server/wwwroot/scripts/validator.js) | ~505 KB | **10** | Canonical repo copy of the validator (per [`docs/ai/validator_js_management.md`](../../docs/ai/validator_js_management.md)). The version-manager UI reads this to seed the CouchDB `metadata/{DefaultId}/validator.js` attachment, which is what the case page loads via `<script src="./api/validator">`. |
| B | [`source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js`](../../source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js) | ~206 KB | **10** | Canonical repo copy of the check-code. [`util/c_db_setup.cs`](../../source-code/mmria/mmria-server/util/c_db_setup.cs) line ~501 reads this file and PUTs it as the CouchDB `metadata/{DefaultId}/mmria-check-code.js` attachment on fresh DB init. |
| C | [`source-code/mmria/mmria-server/database-scripts/validator.js`](../../source-code/mmria/mmria-server/database-scripts/validator.js) | ~430 KB | **8** | Seed source read by `c_db_setup.cs` line ~519 for the initial `metadata/{DefaultId}/validator.js` attachment on fresh DB init. |
| D | [`source-code/mmria/mmria-server/database-scripts/mmria-check-code.js`](../../source-code/mmria/mmria-server/database-scripts/mmria-check-code.js) | ~430 KB | **8** | Pre-existing byte-identical duplicate of file C. Nothing in the codebase reads it (verified by grep). Kept in sync with file C in this story for defence-in-depth; a separate future story should delete it. |

**Total call sites to convert: 36** (10 + 10 + 8 + 8).

Runtime consequence: even after all four files are edited, the browser will still execute the **old** code until the CouchDB `metadata` document's `validator.js` and `mmria-check-code.js` attachments are refreshed. Doing that is part of this story via the `/version-manager` UI (see Task 5).

## Acceptance Criteria

1. **Zero legacy calls remain.** `Select-String` for `get_geocode_info` across all four files (A–D above) returns exactly zero matches.
2. **All 36 call sites use the server endpoint.** Each replaced site invokes `POST /api/case-geocode/{g_data._id}/{locationKey}` with a JSON body containing the address fields and `censusYear: g_data.home_record.date_of_death.year`.
3. **Same busy-modal + reload pattern as Story 30.4.** No new modal implementation; reuse the existing helper. On success the case reloads in edit mode.
4. **Handler names are unchanged.** `x2f_ocl`, `x6b_ocl`, and any other `xNN_ocl` names are event handler names bound by metadata — do NOT rename them. Only the *body* of each handler function is rewritten.
5. **No unrelated code deletion.** After edits, `git diff --stat HEAD` shows each file grew or held steady in line count (allow +/- for the geocode helper additions). Any file with a **net loss > 20 lines** must be reviewed by the architect before proceeding — that is the trip-wire signal that unrelated code was deleted (a real defect from the previous attempt on this story).
6. **`get_geocode_info` in `mmria.js` is preserved.** That helper is still called by `mmria.committee_member.js` until Story 30.7 lands. Do not delete or modify it.
7. **CouchDB attachments refreshed via `/version-manager`.** After the on-disk edits build and pass smoke test, the dev logs into `/version-manager` on the local dev tenant, uses the "MMRIA_Calculations" and "validation" attach buttons to push files A and B to the `metadata` document, and the case page's next reload executes the new geocode path (verified by opening DevTools Network tab and confirming the geocode click issues a `POST /api/case-geocode/...` request).

## Tasks / Subtasks

- [ ] **Task 1 — Confirm baseline.** (Prerequisite; do this first.)
  - [ ] Run in `c:\repos\nccdphp-drh-mmria`:
    ```powershell
    git status --short source-code/mmria/mmria-server/database-scripts source-code/mmria/mmria-server/wwwroot/scripts/validator.js
    ```
    Expect: no output (clean working tree). If any of the four files show `M`, STOP and notify the architect — a previous attempt at this story may still be uncommitted.
  - [ ] Run the legacy-call inventory:
    ```powershell
    foreach ($f in @(
      'source-code\mmria\mmria-server\wwwroot\scripts\validator.js',
      'source-code\mmria\mmria-server\database-scripts\MMRIA_calculations.js',
      'source-code\mmria\mmria-server\database-scripts\validator.js',
      'source-code\mmria\mmria-server\database-scripts\mmria-check-code.js'
    )) {
      $c = (Select-String -Path $f -Pattern 'get_geocode_info' -SimpleMatch).Count
      Write-Host ("{0,-70} {1}" -f $f, $c)
    }
    ```
    Expect exactly 10 / 10 / 8 / 8 respectively. If any count differs, STOP and notify the architect.

- [ ] **Task 2 — Convert File A: `wwwroot/scripts/validator.js`.** (AC: #1, #2, #3, #4)
  - [ ] For each of the 10 legacy call sites: read the enclosing `xNN_ocl` handler body, identify which address section it geocodes, and replace only the body — leave the function signature and handler name intact.
  - [ ] Use the exact busy-modal + reload pattern from Story 30.4 (`MMRIA_calculations.js` in that story is the reference — do NOT invent a new helper).
  - [ ] Include `censusYear: g_data.home_record.date_of_death.year` in every POST body.
  - [ ] After all 10 sites are done, run:
    ```powershell
    git diff --stat HEAD -- source-code/mmria/mmria-server/wwwroot/scripts/validator.js
    ```
    Report the insertions/deletions numbers. Deletions should be roughly proportional to the number of legacy call bodies removed (~1 line each). Insertions should be substantially larger. If **deletions > 100 lines**, STOP.

- [ ] **Task 3 — Convert File B: `database-scripts/MMRIA_calculations.js`.** (AC: #1, #2, #3, #4)
  - [ ] Same procedure as Task 2 for the 10 legacy call sites in this file.
  - [ ] Run the same `git diff --stat` guard after completion.

- [ ] **Task 4 — Convert Files C and D: `database-scripts/validator.js` and `database-scripts/mmria-check-code.js`.** (AC: #1, #2, #3, #4)
  - [ ] Files C and D are byte-identical at HEAD (pre-existing duplication — flagged for future cleanup, out of scope here). Convert both to keep them in sync.
  - [ ] Same procedure as Tasks 2 and 3 for the 8 legacy call sites in each file.
  - [ ] After completion, confirm C and D remain byte-identical:
    ```powershell
    $ha = (Get-FileHash source-code\mmria\mmria-server\database-scripts\validator.js -Algorithm SHA256).Hash
    $hb = (Get-FileHash source-code\mmria\mmria-server\database-scripts\mmria-check-code.js -Algorithm SHA256).Hash
    "identical: $($ha -eq $hb)"
    ```
    Expect `identical: True`. If False, the two files diverged during editing — resolve by copying C over D (or vice versa) so they remain in sync.

- [ ] **Task 5 — Final zero-legacy verification.** (AC: #1)
  - [ ] Run the same inventory command from Task 1. Every file must return 0.

- [ ] **Task 6 — Build.** (AC: no build regression)
  - [ ] Run the `build-server` task (dotnet build of `mmria-server.csproj`). Must succeed.

- [ ] **Task 7 — Push updated attachments to CouchDB via version-manager.** (AC: #7)
  - [ ] Ensure the local multi-tenant CouchDB pods are up (`Launch Multi-Tenant DBs Only` task).
  - [ ] Ensure the local mmria-server is running.
  - [ ] Navigate to `https://localhost:44320/version-manager` (or the current dev URL for the version-manager page).
  - [ ] For each of the two attachments, click the corresponding "attach" button in the version-manager UI:
    - `MMRIA_Calculations` button → refreshes the `mmria-check-code.js` attachment on the base `metadata/2016-06-12T13:49:24.759Z` document.
    - `validation` button → refreshes the `validator.js` attachment on the same document.
  - [ ] Then use the version-manager to regenerate the active `version_specification-{version}/validation` attachment from the base (per the propagation step documented in [`docs/ai/validator_js_management.md`](../../docs/ai/validator_js_management.md) Step 4).
  - [ ] Reload a case in edit mode. Open DevTools Network tab. Click any geocode button. Confirm the browser issues `POST /api/case-geocode/{id}/{locationKey}` (not a request through the old `get_geocode_info` path). Confirm the case reloads on success.

## Dev Notes

**Location-key mapping.** When replacing each call site, read the surrounding function context to identify which address section is being geocoded (e.g., if the handler reads `g_data.death_certificate.address_of_injury.*`, the `locationKey` is `"dc_address_of_injury"`). The 10 valid location keys are the same set defined in Story 30.3.

**Census year.** The old 4-argument signature was `get_geocode_info(street, city, state, zip, callback)` — no `censusYear`. The new POST body MUST include `censusYear: g_data.home_record.date_of_death.year`.

**Handler names — do NOT rename.** `x2f_ocl`, `x6b_ocl`, etc. are event handler names registered in `home_record.json` metadata. Only the *body* of each function changes.

**Busy modal + reload pattern.** Reuse the exact pattern established in Story 30.4 (see [`30-4-refactor-mmria-calculations-geocode-functions.md`](30-4-refactor-mmria-calculations-geocode-functions.md) and the resulting code in `wwwroot/scripts/MMRIA_calculations.js`). Do not invent a new helper.

**`get_geocode_info` in `mmria.js`.** Do NOT touch — still referenced by `mmria.committee_member.js` (which Story 30.7 removes).

**Pre-existing duplicate — Files C and D.** [`database-scripts/mmria-check-code.js`](../../source-code/mmria/mmria-server/database-scripts/mmria-check-code.js) is a byte-identical duplicate of [`database-scripts/validator.js`](../../source-code/mmria/mmria-server/database-scripts/validator.js). Nothing in the codebase reads it — `c_db_setup.cs` seeds the check-code attachment from `MMRIA_calculations.js`, not from this file. Deleting D is deferred to a separate cleanup story; keep it in sync with C for now.

**Prior attempt was reverted.** An earlier run of this story deleted ~1,300 lines from `MMRIA_calculations.js` and ~60–83 KB from each of the other three files. All four files were reset to HEAD on 2026-08-19 before this revision. The AC #5 net-loss guard and the per-task `git diff --stat` checks exist specifically to catch that class of defect early.

**Depends on:** Story 30.3 (endpoint + location keys) and Story 30.4 (busy-modal + reload helper).
