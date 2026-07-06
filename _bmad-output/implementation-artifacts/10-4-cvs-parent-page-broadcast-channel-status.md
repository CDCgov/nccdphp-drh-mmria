---
baseline_commit: cb40e16bdf2867eb10a552897456c446bbde041f
---

# Story 10.4 — CVS Parent-Page Button State via BroadcastChannel

**Epic:** 10 — CVS PDF Export Tool Reliability
**Story ID:** 10.4
**Status:** review
**Date added:** 2026-07-06

---

## User Story

As a case reviewer on the case form,
When I click the CVS report button and the report is generating in a separate tab,
I want the button to show it is busy and re-enable automatically when the report finishes,
So that I cannot accidentally open duplicate CVS windows and I know when the report is ready.

---

## Acceptance Criteria

**AC-1 — CVS button is disabled when a report is in progress**
Given the user clicks a CVS report button (`p_control`) on the case form
When `beginCvsReportRequest(record_id, p_control)` is called
Then `p_control.disabled = true`
And `p_control.setAttribute("aria-busy", "true")` is set
And the button's label text is changed to `"Generating…"` (or equivalent in-progress label) via `setControlText`

**AC-2 — CVS button re-enables on terminal BroadcastChannel status**
Given a CVS report window broadcasts a terminal status (`"ready"`, `"failed"`, `"max_retries"`, `"validation_error"`)
When the `cvsReportChannel` message handler in `$mmria` receives it
Then `endCvsReportRequest(record_id)` is called for the matching `record_id`
And the button's `disabled` is set to `false`, `aria-busy` is removed, and the label text is restored

**AC-3 — Multiple CVS records are tracked independently**
Given two different cases have CVS buttons on the same page (or the same case has multiple CVS invocations)
When `beginCvsReportRequest` is called with different `record_id` values
Then each is tracked separately in `cvsReportControls` (keyed by normalized `record_id`)
And `endCvsReportRequest` for one record does not affect the button state of another

**AC-4 — Fallback timer re-enables the button after 20 minutes**
Given `beginCvsReportRequest` is called and no terminal BroadcastChannel message arrives
When 20 minutes (`cvsReportButtonFallbackMs = 20 * 60 * 1000`) elapse
Then `endCvsReportRequest` is called automatically via `setTimeout`
And the button is re-enabled regardless of the report window state

**AC-5 — Fallback timer is cancelled when a BroadcastChannel message arrives first**
Given a terminal BroadcastChannel message arrives before the 20-minute timer fires
When `endCvsReportRequest` is called from the message handler
Then the fallback `setTimeout` is cleared (`clearTimeout(state.fallbackTimerId)`)
And the timer does not fire afterward

**AC-6 — Window-open failure is handled**
Given `window.open(base_url, id)` returns `null` (popup blocked or opener denied)
When the return value is checked
Then `endCvsReportRequest(id)` is called immediately to re-enable the button
And no orphaned "in-progress" state is left

**AC-7 — CVS window URL is built with URLSearchParams**
Given `lat`, `lon`, `year`, or `id` values contain special characters
When the CVS window URL is constructed
Then `new URLSearchParams({ lat: lat ?? "", lon: lon ?? "", year: year ?? "", id: id ?? "" })` is used
And the resulting URL is `${base_url}?${query.toString()}`
And raw string concatenation is not used for the query string

**AC-8 — BroadcastChannel is initialized once and shared**
Given `$mmria` initializes
When the module loads
Then `cvsReportChannel = new BroadcastChannel('cvs_channel')` is created once
And all CVS report status messages are received on this single channel instance

---

## Dev Notes — Root Cause and Fix

### Root Cause

The case form launched the CVS window with `window.open()` and had no mechanism to:
- Disable the button while the report was open
- Know when the report window closed or finished
- Re-enable the button

This led to users opening multiple CVS windows for the same record and no feedback that the system was working.

### BroadcastChannel integration

The CVS page (`cvs/index.js`, Story 10.3) calls `post_cvs_status(status)` which posts:
```javascript
bc.postMessage({
    type: "cvs-report-status",
    status: status,          // "started" | "ready" | "failed" | "max_retries" | "validation_error"
    record_id: g_record_id,
    lat: g_lat,
    lon: g_lon,
    year: g_year
});
```

The parent page (`mmria.js`) listens on the same channel.

### Key additions to `mmria.js`

```javascript
const cvsReportControls = new Map();
const cvsReportTerminalStatuses = new Set(["ready", "failed", "max_retries", "validation_error"]);
const cvsReportButtonFallbackMs = 20 * 60 * 1000;
let cvsReportChannel = null;

const getCvsReportKey = (recordId) => String(recordId ?? '').trim().toLowerCase();

const setControlText = (control, text) => {
    if (!control) return;
    if ("value" in control) { control.value = text; }
    else { control.textContent = text; }
};

function beginCvsReportRequest(recordId, control) {
    const key = getCvsReportKey(recordId);
    let state = cvsReportControls.get(key);
    if (!state) {
        state = {
            control,
            originalText: control?.value ?? control?.textContent,
            fallbackTimerId: null
        };
        cvsReportControls.set(key, state);
    } else {
        state.control = control;
    }
    control.disabled = true;
    if (typeof control.setAttribute === "function") {
        control.setAttribute("aria-busy", "true");
    }
    setControlText(control, "Generating…");
    state.fallbackTimerId = setTimeout(() => endCvsReportRequest(recordId), cvsReportButtonFallbackMs);
}

function endCvsReportRequest(recordId) {
    const key = getCvsReportKey(recordId);
    const state = cvsReportControls.get(key);
    if (!state) return;
    if (state.fallbackTimerId != null) {
        clearTimeout(state.fallbackTimerId);
        state.fallbackTimerId = null;
    }
    if (state.control) {
        state.control.disabled = false;
        if (typeof state.control.removeAttribute === "function") {
            state.control.removeAttribute("aria-busy");
        }
        setControlText(state.control, state.originalText);
    }
    cvsReportControls.delete(key);
}
```

Channel initialization (done once inside the `$mmria` module init):
```javascript
cvsReportChannel = new BroadcastChannel('cvs_channel');
cvsReportChannel.onmessage = (event) => {
    const message = event.data;
    if (!message || message.type !== "cvs-report-status") return;
    if (message.status === "started") {
        const state = cvsReportControls.get(getCvsReportKey(message.record_id));
        if (state?.control) { beginCvsReportRequest(message.record_id, state.control); }
        return;
    }
    if (cvsReportTerminalStatuses.has(message.status)) {
        endCvsReportRequest(message.record_id);
    }
};
```

CVS window open (updated to use `URLSearchParams` and handle null return):
```javascript
const query = new URLSearchParams({
    lat: lat ?? "",
    lon: lon ?? "",
    year: year ?? "",
    id: id ?? ""
});
const base_url = `${location.protocol}//${location.host}/community-vital-signs?${query.toString()}`;
beginCvsReportRequest(record_id, p_control);
const reportWindow = window.open(base_url, id);
if (!reportWindow) {
    endCvsReportRequest(id);
}
```

### Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/mmria.js` | Add `cvsReportControls` map, `beginCvsReportRequest`, `endCvsReportRequest`, `setControlText`, `getCvsReportKey`, fallback timer logic, `BroadcastChannel` listener; update CVS window-open calls to use `URLSearchParams` and call `beginCvsReportRequest`; handle null `window.open` return |

### Sequencing

- Depends on Story 10.3 — `post_cvs_status` and the BroadcastChannel message schema are defined there.
- Independent of Stories 10.1 and 10.2.
