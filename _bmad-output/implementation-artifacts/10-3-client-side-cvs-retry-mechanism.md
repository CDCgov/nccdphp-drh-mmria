---
baseline_commit: cb40e16bdf2867eb10a552897456c446bbde041f
---

# Story 10.3 — Client-Side CVS Retry Mechanism with Countdown

**Epic:** 10 — CVS PDF Export Tool Reliability
**Story ID:** 10.3
**Status:** done
**Date added:** 2026-07-06

---

## User Story

As a case reviewer generating a CVS PDF,
When the service is still preparing the report,
I want the page to automatically retry and show me a countdown between attempts,
So that I don't have to refresh the browser and I can see the system is actively working.

---

## Acceptance Criteria

**AC-1 — Retry loop replaces busy-while pattern**
Given the CVS page starts polling
When `run_cvs_report_polling` executes
Then polling is implemented as a `for (let attempt = 1; attempt <= CVS_MAX_ATTEMPTS; attempt++)` loop — not as `while (!is_finished)`
And each iteration calls `get_cvs_api_dashboard_info` exactly once

**AC-2 — Attempt progress is shown to the user**
Given a polling attempt is in progress
When `show_active_request(header, el, attempt)` is called at the start of each iteration
Then `header.innerHTML` = `"Please wait."`
And `el.innerHTML` = `` `Generating PDF... Checking Community Vital Signs service, attempt ${attempt} of ${CVS_MAX_ATTEMPTS}.` ``

**AC-3 — Countdown timer is shown between retries**
Given the service returns `"generating"` or `"unavailable"` and more attempts remain
When `wait_for_next_attempt(header, el, next_attempt)` is called
Then a `setInterval` timer fires every 1000ms, decrementing `remaining_seconds` from `CVS_RETRY_DELAY_SECONDS` to 0
And `update_countdown_message` updates the UI each second: `` `The Community Vital Signs report is being prepared. Next check in ${remaining_seconds} seconds. Attempt ${next_attempt} of ${CVS_MAX_ATTEMPTS} will run automatically.` ``
And the promise resolves (timer cleared) when `remaining_seconds <= 0`
And the timer handle is stored in `g_countdown_timer` so it can be cleared in the `finally` block

**AC-4 — "Try again" button is shown when max retries are exhausted**
Given the loop reaches `CVS_MAX_ATTEMPTS` without a terminal result
When the loop exits without `"file ready"`, `"error"`, or a validation-error status
Then `spinner.innerHTML` is set to a combination of the close button and an enabled **Try again** button
And `post_cvs_status("max_retries")` is broadcast
And focus is moved to the `try_again_button` element

**AC-5 — "Try again" restarts the loop from attempt 1**
Given the user clicks **Try again**
When `try_again_button_click()` fires
Then `run_cvs_report_polling` is called with `reset_log = false` (log is appended, not cleared)
And the button is immediately rendered as disabled (via `render_disabled_try_again_button_html`) until the new run completes or fails

**AC-6 — Concurrent runs are prevented**
Given a polling run is in progress (`g_is_running === true`)
When `run_cvs_report_polling` is called again (e.g., rapid double-click)
Then the function returns immediately without starting a second run

**AC-7 — `g_is_running` is always reset in the `finally` block**
Given polling completes for any reason (success, error, or exception)
When the `try/finally` block executes
Then `g_is_running` is set to `false`
And if `g_countdown_timer` is non-null it is cleared with `window.clearInterval` and set to `null`

**AC-8 — File status is normalized before branching**
Given the server may return `file_status` in mixed case or with surrounding whitespace
When `normalize_file_status(response.file_status)` is called
Then it returns `String(file_status).trim().toLowerCase()` (or `""` for null)
And all `if (file_status == ...)` comparisons use the normalized value

**AC-9 — HTTP error responses from the fetch are handled**
Given `get_cvs_api_dashboard_info` receives a non-2xx HTTP response
When `!response.ok` is true
Then a synthetic result object is returned: `{ file_status: response.status >= 500 || response.status == 408 || response.status == 429 ? "unavailable" : "error", status: response.status, detail: ... }`
And this synthetic result flows into the same status-branching logic as a normal API response

**AC-10 — Fetch network failure returns unavailable**
Given `fetch` throws (network unreachable, CORS, etc.)
When the `catch(ex)` block in `get_cvs_api_dashboard_info` fires
Then `{ file_status: "unavailable", detail: ex.message || String(ex) }` is returned
And the retry loop treats this as `"unavailable"` and continues retrying if attempts remain

---

## Dev Notes — Root Cause and Fix

### Root Cause

The original polling loop was:

```javascript
var is_finished = false;
while (!is_finished) {
    const response = await get_cvs_api_dashboard_info(g_lat, g_lon, g_year, g_record_id);
    if (response.file_status != null) {
        if (...) { is_finished = true; }
        else if (...) { is_finished = true; }
        // ...
    } else {
        // generic error path
        is_finished = true;
    }
}
```

This had no retry count limit, no delay between retries, no countdown UI, and no "try again" path. The `is_finished` flag was fragile and easy to leave unset on unexpected status values.

### New constants

```javascript
const CVS_MAX_ATTEMPTS = 10;          // confirm value with team (OI-5)
const CVS_RETRY_DELAY_SECONDS = 30;   // confirm value with team (OI-5)
```

### Key new functions

```javascript
async function run_cvs_report_polling(header, el, spinner, report_output_element, reset_log) { ... }
function wait_for_next_attempt(header, el, next_attempt) { /* returns Promise, uses setInterval */ }
function update_countdown_message(header, el, remaining_seconds, next_attempt) { ... }
function show_active_request(header, el, attempt) { ... }
function normalize_file_status(file_status) { ... }
function render_report_log(report_output_element) { ... }
function render_try_again_button_html() { ... }
function render_disabled_try_again_button_html() { ... }
async function try_again_button_click() { ... }
function post_cvs_status(status) { /* BroadcastChannel — see Story 10.4 */ }
```

### Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/cvs/index.js` | Replace `while(!is_finished)` with bounded `for` loop; add `CVS_MAX_ATTEMPTS`, `CVS_RETRY_DELAY_SECONDS` constants; add countdown, try-again, normalization, and guard functions; improve `get_cvs_api_dashboard_info` to handle non-2xx and fetch errors |

### Sequencing

- Story 10.2 (server error hardening) should be merged first so the `message` field and improved `file_status` values are available for client testing, but Story 10.3 is not strictly blocked by 10.2 — the client handles unknown statuses gracefully.
- Story 10.4 depends on Story 10.3 (`post_cvs_status` is defined here and consumed there).

---

## Dev Agent Record

### Completion Notes

All 10 ACs implemented in `source-code/mmria/mmria-server/wwwroot/scripts/cvs/index.js`:

- AC-1: `while(!is_finished)` loop replaced with `for (let attempt = 1; attempt <= CVS_MAX_ATTEMPTS; attempt++)`.
- AC-2: `show_active_request(header, el, attempt)` called at the start of each iteration with required text.
- AC-3: `wait_for_next_attempt` uses `setInterval` / `g_countdown_timer`; `update_countdown_message` fires each second.
- AC-4: After loop exhaustion, spinner shows close + enabled Try again button; `post_cvs_status("max_retries")` broadcast; focus moved to try_again_button.
- AC-5: `try_again_button_click` renders disabled Try again button immediately, then calls `run_cvs_report_polling(..., false)`.
- AC-6: `g_is_running` guard returns early on concurrent call.
- AC-7: `finally` block resets `g_is_running` and clears `g_countdown_timer`.
- AC-8: `normalize_file_status` returns `String(file_status).trim().toLowerCase()` or `""` for null/undefined; all branching uses normalized value.
- AC-9: `get_cvs_api_dashboard_info` checks `!response.ok` and returns synthetic `{ file_status: "unavailable"|"error", status, detail }`.
- AC-10: `catch(ex)` returns `{ file_status: "unavailable", detail: ex.message || String(ex) }`.

`post_cvs_status` stub (`bc.postMessage({ type: "cvs_status", status })`) defined for Story 10.4 consumption.

### Change Log

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/cvs/index.js` | Full rewrite of polling logic: bounded `for` loop, countdown timer, try-again mechanism, concurrency guard, status normalization, HTTP error handling |
