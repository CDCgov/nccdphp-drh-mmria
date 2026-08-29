# Story 30.7: Remove Dead TAMU Code from mmria.committee_member.js

Status: done

## Story

As a security engineer,
I want the embedded TAMU API key and dead geocode code removed from `mmria.committee_member.js`,
so that the API key is not present in any file served to the browser.

## Acceptance Criteria

1. `mmria.committee_member.js` no longer contains a `get_geocode_info` function that references `geoservices.tamu.edu` or the API key.
2. No `geoservices.tamu.edu` URL appears in any client-served JS file. PowerShell grep confirms zero.
3. No `geocode_api_key` or `apikey=` value appears in any client-served JS file. PowerShell grep confirms zero.
4. The committee member view continues to function correctly — the validate-address button is always disabled in this view (read-only), so no geocode functionality is lost.
5. `node --check` on `mmria.committee_member.js` — zero syntax errors. (The `.csproj` build is not a valid verification for JS-only changes; it does not lint or bundle files under `wwwroot/scripts/`.)

## Tasks / Subtasks

- [x] Locate and remove dead geocode code (AC: #1)
  - [x] Open `source-code/mmria/mmria-server/wwwroot/scripts/mmria.committee_member.js`
  - [x] Find the `get_geocode_info` function that constructs a `geoservices.tamu.edu` URL with an embedded API key
  - [x] Remove the entire function body and declaration — this is dead code (the button is always disabled in committee member view)
- [x] Verify no TAMU URL remains in client JS (AC: #2)
  - [x] `Get-ChildItem -Path "source-code\mmria\mmria-server\wwwroot\scripts" -Recurse -File | Select-String -Pattern "geoservices.tamu.edu"` → zero results
- [x] Verify no API key in client JS (AC: #3)
  - [x] `Get-ChildItem -Path "source-code\mmria\mmria-server\wwwroot\scripts" -Recurse -File | Select-String -Pattern "geocode_api_key|apikey="` → zero results
- [ ] Verify committee member view still works (AC: #4) — Human-TODO: smoke test in dev env
  - [ ] Open a case in committee member view in the local environment — confirm the form loads correctly and the validate-address button is disabled as expected
- [x] Node syntax check (AC: #5)
  - [x] `node --check source-code/mmria/mmria-server/wwwroot/scripts/mmria.committee_member.js` — zero output (parse OK).

## Dev Notes

**File to modify:** `source-code/mmria/mmria-server/wwwroot/scripts/mmria.committee_member.js`

**Why this is removal not replacement:** The committee member view is read-only. The validate-address button (`cmd_get_coordinates`) is always rendered in a disabled state in this view. The `get_geocode_info` implementation in `mmria.committee_member.js` was never reachable in production and is dead code that happens to contain an API key.

**`get_geocode_info` in `mmria.js`** — do NOT touch. It is a different implementation (routes through the server proxy correctly) and is still referenced by Layers B (mmria-check-code.js, validator.js) until Stories 30.6 completes.

**No functional replacement needed** — Story 30.7 is a deletion-only story.

**Independent story** — no dependency on Stories 30.1–30.6. Can be done at any time.
