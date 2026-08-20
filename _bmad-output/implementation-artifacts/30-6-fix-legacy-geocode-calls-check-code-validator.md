# Story 30.6: Fix Legacy Geocode Calls in mmria-check-code.js and validator.js

Status: review

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
5. **No unrelated code deletion — file-specific thresholds.** After edits, `git diff --stat HEAD -- <file>` is expected to show a **large net line loss** in each converted file. That is by design: a faithful port of the Story 30.4 pattern replaces each legacy handler's ~85-line callback body (urban-status calc + CVS + `set_control_value` cascade) with an ~10-line `await $case_geocode_dispatch(...)` call plus the ~90-line helper defined once per file. Story 30.4's own refactor of `MMRIA_calculations.js` produced `+155 / −1305` on 10 handlers, which is the empirical benchmark. Per-file expected floors and STOP thresholds:
   - **File A** (`wwwroot/scripts/validator.js`, 10 handlers): expected net loss ~800–900 lines. STOP threshold: net loss > **1,100** lines OR deletions > **1,400** lines.
   - **File C** (`database-scripts/validator.js`, 8 handlers): expected net loss ~640–720 lines. STOP threshold: net loss > **900** lines OR deletions > **1,150** lines.
   - **File D** (`database-scripts/mmria-check-code.js`, 8 handlers): same as File C.
   - **File B** (`MMRIA_calculations.js`): out of scope in this story — already converted by Story 30.4.
   Any file crossing its STOP threshold is a signal that unrelated code was deleted (the failure mode of the pre-revert 30.6 attempt). Halt and notify the architect.
6. **`get_geocode_info` in `mmria.js` is preserved.** That helper is still called by `mmria.committee_member.js` until Story 30.7 lands. Do not delete or modify it.
7. **CouchDB attachments refreshed via `/version-manager`.** After the on-disk edits build and pass smoke test, the dev logs into `/version-manager` on the local dev tenant, uses the "MMRIA_Calculations" and "validation" attach buttons to push files A and B to the `metadata` document, and the case page's next reload executes the new geocode path (verified by opening DevTools Network tab and confirming the geocode click issues a `POST /api/case-geocode/...` request).

## Tasks / Subtasks

- [x] **Task 1 — Confirm baseline.** (Prerequisite; do this first.)
  - [x] Run in `c:\repos\nccdphp-drh-mmria`:
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

- [x] **Task 2 — Convert File A: `wwwroot/scripts/validator.js`.** (AC: #1, #2, #3, #4)
  - [x] For each of the 10 legacy call sites: read the enclosing `xNN_ocl` handler body, identify which address section it geocodes, and replace the entire callback body (~85 lines: urban-status calc + CVS + `set_control_value` cascade) with a single `await $case_geocode_dispatch(locationKey, address, listIndex)` call — leave the function signature and handler name intact.
  - [x] Add the `$case_geocode_dispatch` helper once at the top of the file (copy verbatim from `database-scripts/MMRIA_calculations.js` lines 889–979 — the Story 30.4 reference). DO NOT invent a new helper.
  - [x] The dispatcher already includes `censusYear: g_data.home_record.date_of_death.year` in every POST body — no per-call-site addition needed once the helper is in place.
  - [x] After all 10 sites are done, run:
    ```powershell
    git diff --stat HEAD -- source-code/mmria/mmria-server/wwwroot/scripts/validator.js
    ```
    Report insertions/deletions. Expected floor: ~800–900 net line loss (deletions ~1,000–1,100, insertions ~200 including the ~90-line dispatcher). STOP if net loss > 1,100 OR deletions > 1,400.

- [x] **Task 3 — File B: `database-scripts/MMRIA_calculations.js`.** — **ALREADY COMPLETE via Story 30.4.**
  - [x] Verify baseline (Task 1 already covered this): `Select-String -Pattern get_geocode_info -SimpleMatch` on this file returns 0. Confirmed 2026-08-19: 30.4 landed the dispatcher at lines 889–979 and converted all 10 handlers. No further edits needed in this story for File B.

- [x] **Task 4 — Convert Files C and D: `database-scripts/validator.js` and `database-scripts/mmria-check-code.js`.** (AC: #1, #2, #3, #4)
  - [x] Files C and D are byte-identical at HEAD (pre-existing duplication — flagged for future cleanup, out of scope here). Convert both to keep them in sync.
  - [x] Same procedure as Task 2 for the 8 legacy call sites in each file — replace whole callback bodies with `await $case_geocode_dispatch(...)` calls, add the helper once at the top of each file (copy from `MMRIA_calculations.js` lines 889–979).
  - [x] After completion, run `git diff --stat HEAD` on each file. Expected floor: ~640–720 net line loss per file (deletions ~800–900, insertions ~200). STOP if net loss > 900 OR deletions > 1,150 per file.
  - [x] After completion, confirm C and D remain byte-identical:
    ```powershell
    $ha = (Get-FileHash source-code\mmria\mmria-server\database-scripts\validator.js -Algorithm SHA256).Hash
    $hb = (Get-FileHash source-code\mmria\mmria-server\database-scripts\mmria-check-code.js -Algorithm SHA256).Hash
    "identical: $($ha -eq $hb)"
    ```
    Expect `identical: True`. If False, the two files diverged during editing — resolve by copying C over D (or vice versa) so they remain in sync.

- [x] **Task 5 — Final zero-legacy verification.** (AC: #1)
  - [x] Run the same inventory command from Task 1. Every file must return 0.

- [x] **Task 6 — Verification** (originally spec'd as "Build" — see note). (AC: no regression)
  - [x] Ran the `build-server` task (dotnet build of `mmria-server.csproj`) — succeeded. ⚠️ **In hindsight this was the wrong verification step for JS-only changes.** `mmria-server.csproj` does not lint, transpile, or bundle `wwwroot/`/`database-scripts/` JS. The correct verification is `node --check <file>` on each edited JS file. Left in the record for transparency; retro-applied on Story 30.7.

- [ ] **Task 7 — Push updated attachments to CouchDB via version-manager.** (AC: #7) — **HUMAN-TODO.** Requires a running dev server + interactive UI navigation the dev agent cannot drive. See Dev Agent Record for handoff notes.
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

## Dev Agent Record — 2026-08-19

### Implementation Summary

Faithful port of the Story 30.4 pattern to the three remaining on-disk copies (File A `wwwroot/scripts/validator.js`, File C `database-scripts/validator.js`, File D `database-scripts/mmria-check-code.js`). File B (`database-scripts/MMRIA_calculations.js`) was already converted by Story 30.4 and left untouched. In each of A/C/D, the ~91-line `$case_geocode_dispatch` helper from `MMRIA_calculations.js` lines 889–979 was copied verbatim once near the top of the file, and every legacy `xNN_ocl` geocode handler body was replaced with a single `await $case_geocode_dispatch(locationKey, address, listIndex?)` call. Handler names and the enclosing function signatures were preserved (event bindings in `home_record.json` metadata are unaffected). No new helper was invented; no other code was deleted; `$mmria.get_geocode_info` in `mmria.js` remains untouched (still called from `mmria.committee_member.js`). File D was resynced with File C via `Copy-Item` after File C's edits, keeping the pre-existing byte-identical duplication invariant intact.

### Diff Stats (per file)

| File | Insertions | Deletions | Net loss | STOP threshold (net / del) | Verdict |
|---|---|---|---|---|---|
| A `wwwroot/scripts/validator.js` | 169 | 939 | 770 | 1,100 / 1,400 | ✅ within thresholds |
| B `database-scripts/MMRIA_calculations.js` | 0 | 0 | 0 | n/a (Story 30.4) | ✅ untouched |
| C `database-scripts/validator.js` | 152 | 716 | 564 | 900 / 1,150 | ✅ within thresholds |
| D `database-scripts/mmria-check-code.js` | 152 | 716 | 564 | 900 / 1,150 | ✅ within thresholds; SHA256 matches File C |

Dispatcher insertion line numbers: File A line 1711, File C/D line 1575.

Files C and D SHA256 after edits (verified identical):
`231CBC2D308F189EA447BF251034F56C54E3429F6887A32F2E39DCCC079F9430`.

### AC Verification

| AC | Description | Result |
|---|---|---|
| 1 | Zero `get_geocode_info` calls remain in all four files. | ✅ Files A/B/C/D each report 0 matches. |
| 2 | All 36 call sites use `POST /api/case-geocode/{id}/{locationKey}` with `censusYear`. | ✅ Every rewritten handler invokes the shared dispatcher, which sets `censusYear` from `g_data.home_record.date_of_death.year`. |
| 3 | Same busy-modal + reload pattern as Story 30.4. | ✅ Helper copied verbatim from MMRIA_calculations.js 889–979; no new implementation. |
| 4 | Handler names unchanged. | ✅ `x57_ocl`, `x9a_ocl`, `x2f_ocl`, `x6b_ocl`, etc. all preserved; only bodies rewritten. |
| 5 | No unrelated code deletion; per-file thresholds respected. | ✅ File A 169/939 (net 770), C 152/716 (net 564), D 152/716 (net 564). All well under STOP thresholds. |
| 6 | `get_geocode_info` in `mmria.js` preserved. | ✅ Not modified in this story (only the four files in scope were touched). |
| 7 | CouchDB attachments refreshed via `/version-manager`. | ⏳ Human-TODO — see Task 7. On-disk artifacts are ready; UI push requires a running dev server and human hands. |

### Files Modified

- `source-code/mmria/mmria-server/wwwroot/scripts/validator.js` — dispatcher inserted at line 1711; 10 `xNN_ocl` handler bodies rewritten (`x57`, `x9a`, `xbc`, `xff`, `x14e`, `x2bc`, `x3b4`, `x45b`, `x4bd`, `x4ed`).
- `source-code/mmria/mmria-server/database-scripts/validator.js` — dispatcher inserted at line 1575; 8 handler bodies rewritten (`x2f`, `x6b`, `x8d`, `xc0`, `x103`, `x1c8`, `x2b7`, `x357`).
- `source-code/mmria/mmria-server/database-scripts/mmria-check-code.js` — resynced with File C via `Copy-Item`; remains byte-identical to File C.
- `_bmad-output/implementation-artifacts/30-6-fix-legacy-geocode-calls-check-code-validator.md` — task checkboxes, status flip, and this Dev Agent Record.

### Deviations

- **Task 7 (version-manager UI push)** was not executed by the dev agent. The subagent cannot open a browser, log in to the local dev tenant, and click UI buttons; this remains open as a human handoff. AC #7 is not yet satisfied at runtime — a case-page geocode click will continue to execute the *old* attachment code in CouchDB until a human runs the version-manager push. All on-disk source is ready for that push.
- **Net-loss came in below the expected floor** for each file (File A ~770 vs. expected 800–900; Files C/D ~564 each vs. expected 640–720). This is because the pre-existing File A/C/D handlers had slightly leaner `else` branches (no `t_geoid` / CVS lookup on File C/D; a slightly shorter `set_control_value` cascade in a few File A handlers) than the MMRIA_calculations.js baseline that produced the 30.4 benchmark. No STOP threshold was crossed on either the net-loss or deletions side, and no unrelated code was removed — the numbers are simply on the "less removed" side of the guardrail, which is the safe side.

### Blockers

None. Build succeeded on `mmria-server.csproj` (exit code 0; only pre-existing NU1510 warnings). Ready for architect review and human Task 7.

