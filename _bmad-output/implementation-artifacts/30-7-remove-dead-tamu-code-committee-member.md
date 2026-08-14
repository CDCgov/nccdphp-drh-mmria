# Story 30.7: Remove Dead TAMU Code from mmria.committee_member.js

Status: ready-for-dev

## Story

As a security engineer,
I want the embedded TAMU API key and dead geocode code removed from `mmria.committee_member.js`,
so that the API key is not present in any file served to the browser.

## Acceptance Criteria

1. `mmria.committee_member.js` no longer contains a `get_geocode_info` function that references `geoservices.tamu.edu` or the API key.
2. No `geoservices.tamu.edu` URL appears in any client-served JS file. PowerShell grep confirms zero.
3. No `geocode_api_key` or `apikey=` value appears in any client-served JS file. PowerShell grep confirms zero.
4. The committee member view continues to function correctly — the validate-address button is always disabled in this view (read-only), so no geocode functionality is lost.
5. `dotnet build mmria-server.csproj` — zero errors.

## Tasks / Subtasks

- [ ] Locate and remove dead geocode code (AC: #1)
  - [ ] Open `source-code/mmria/mmria-server/wwwroot/scripts/mmria.committee_member.js`
  - [ ] Find the `get_geocode_info` function that constructs a `geoservices.tamu.edu` URL with an embedded API key
  - [ ] Remove the entire function body and declaration — this is dead code (the button is always disabled in committee member view)
- [ ] Verify no TAMU URL remains in client JS (AC: #2)
  - [ ] `Select-String -Recurse -Path "source-code\mmria\mmria-server\wwwroot\scripts" -Pattern "geoservices.tamu.edu"` → zero results
- [ ] Verify no API key in client JS (AC: #3)
  - [ ] `Select-String -Recurse -Path "source-code\mmria\mmria-server\wwwroot\scripts" -Pattern "geocode_api_key|apikey="` → zero results in browser-served files
- [ ] Verify committee member view still works (AC: #4)
  - [ ] Open a case in committee member view in the local environment — confirm the form loads correctly and the validate-address button is disabled as expected
- [ ] Build (AC: #5)
  - [ ] `dotnet build` on mmria-server — zero errors

## Dev Notes

**File to modify:** `source-code/mmria/mmria-server/wwwroot/scripts/mmria.committee_member.js`

**Why this is removal not replacement:** The committee member view is read-only. The validate-address button (`cmd_get_coordinates`) is always rendered in a disabled state in this view. The `get_geocode_info` implementation in `mmria.committee_member.js` was never reachable in production and is dead code that happens to contain an API key.

**`get_geocode_info` in `mmria.js`** — do NOT touch. It is a different implementation (routes through the server proxy correctly) and is still referenced by Layers B (mmria-check-code.js, validator.js) until Stories 30.6 completes.

**No functional replacement needed** — Story 30.7 is a deletion-only story.

**Independent story** — no dependency on Stories 30.1–30.6. Can be done at any time.
