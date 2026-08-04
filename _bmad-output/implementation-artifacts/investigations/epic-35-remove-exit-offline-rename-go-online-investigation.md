# Investigation: Epic 35 — Remove 'Exit Offline Mode' button, rename 'Go Online' to 'Go Online & Sync Changes'

## Hand-off Brief

1. **What happened.** Rel 4.1 late-breaking change request (Katrina's feedback, ADO #119294 / Marta Puskarz): (a) remove the "Exit Offline Mode" button while a user is in offline mode, (b) rename the "Go Online" button label to "Go Online & Sync Changes". This is a UI-scope change request, not a bug.
2. **Where the case stands.** Stronghold mapped: all render sites and text sources for both buttons are Confirmed via grep + read. No functional ambiguity remains on the rename; one open design question remains on the removal (widget-only hide vs. full dead-code removal of the exit flow).
3. **What's needed next.** Confirm removal scope with the user (hide vs. remove), then proceed to `bmad-quick-dev` — this is a small, well-bounded UI change across a known set of files.

## Case Info

| Field            | Value                                                                                               |
| ---------------- | --------------------------------------------------------------------------------------------------- |
| Ticket           | ADO #119294 (Marta Puskarz) — Epic 35, Rel 4.1 P-Immediate                                          |
| Date opened      | 2026-08-03                                                                                          |
| Status           | Active                                                                                              |
| System           | mmria-server, wwwroot/scripts/offline (vanilla JS, no build step)                                   |
| Evidence sources | Source code (grep + read), git log, docs/ai/offline_mode.md, \_bmad-output/implementation-artifacts |

## Problem Statement

Rel 4.1 must ship with two additional offline-mode UI changes before deployment:

1. Remove the `'-> Exit Offline Mode'` button shown while the user is in Offline mode.
2. Rename the `'Go Online'` button to `'Go Online & Sync Changes'`.

## Evidence Inventory

| Source                                           | Status    | Notes                                                                                                                                                          |
| ------------------------------------------------ | --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Source code (offline widget/button render sites) | Available | All 2 button families fully traced via grep + read (below).                                                                                                    |
| git log on `offline-exit-manager.js`             | Available | 3 commits total — feature is very recent (`e7dbe212b exit offline button`, `3bbdacf37 fixing an edge case`, `2ca9b9b8c *508 fixes for exit offline widge...`). |
| docs/ai/offline_mode.md                          | Missing   | No mention of "Exit Offline Mode" at all — doc predates or was never updated for this feature.                                                                 |
| \_bmad-output/implementation-artifacts           | Missing   | No prior story/artifact found documenting the Exit Offline Mode feature's intended removal semantics or product rationale.                                     |
| ADO work item #119294                            | Missing   | Not fetched (no MCP/ADO tool available in this session) — only the user-supplied summary text is available.                                                    |

## Investigation Backlog

| #   | Path to Explore                                                                                                                                                                                                                                                                   | Priority | Status | Notes                                        |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ------ | -------------------------------------------- |
| 1   | Confirm with user: does "remove" mean hide the button only (leave `OfflineExitManager` dead-code in place) or fully remove the exit-offline-mode flow (modal, `confirmExitOfflineMode`, `showExitOfflineModeModal`, widget host divs)?                                            | High     | Open   | Affects blast radius of the change.          |
| 2   | Confirm scope of "Go Online" rename: does it include the modal title/button in `offline-modals.js` (`show_go_online_modal`) and the dynamic state text in `offline-transition-manager.js` / `offline-network-monitor.js`, or only the two static button labels in `app.mmria.js`? | High     | Open   | 4+ distinct text sources found (below).      |
| 3   | Side finding: the two "Go Online" buttons in `app.mmria.js` have inconsistent click handlers — one opens a confirmation modal (`show_go_online_modal`), the other calls `go_online_clicked` directly, skipping confirmation.                                                      | Low      | Open   | Not in scope of this request; flagging only. |

## Timeline of Events

| Time                              | Event                                                                     | Source        | Confidence           |
| --------------------------------- | ------------------------------------------------------------------------- | ------------- | -------------------- |
| (recent, exact date not in scope) | `e7dbe212b exit offline button` — feature added                           | git log       | Confirmed            |
| (recent)                          | `3bbdacf37 fixing an edge case`                                           | git log       | Confirmed            |
| (recent)                          | `2ca9b9b8c *508 fixes for exit offline widge... case narrative heading`   | git log       | Confirmed            |
| 2026-08-03                        | Katrina's feedback received; Rel 4.1 change request opened as ADO #119294 | User-supplied | Confirmed (per user) |

## Confirmed Findings

### Finding 1: "Exit Offline Mode" button — single reusable widget, 4 host locations

**Evidence:** `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-exit-manager.js:404-421` (`renderWidgetMarkup`), `:426-441` (`initializeWidgetHosts`), `:576-580` (`shouldShowExitWidget`).

**Detail:** The button markup (`<span>Exit Offline Mode</span>`, line 420) is injected by `OfflineExitManager` into every DOM element matching `[data-offline-exit-host]`. Host elements are declared in 4 views:

- [source-code/mmria/mmria-server/Views/Home/Index.cshtml](source-code/mmria/mmria-server/Views/Home/Index.cshtml#L167) — `<div id="offline-home-exit-widget" data-offline-exit-host="home">`
- [source-code/mmria/mmria-server/Views/Account/OfflineLogin.cshtml](source-code/mmria/mmria-server/Views/Account/OfflineLogin.cshtml#L43) — `data-offline-exit-host="offline-login"`
- [source-code/mmria/mmria-server/Views/Shared/\_BreadCrumbs.cshtml](source-code/mmria/mmria-server/Views/Shared/_BreadCrumbs.cshtml#L25) — `id="offline-mode-indicator" data-offline-exit-host="breadcrumbs"` (this is the one visible on essentially every page, since breadcrumbs are shared layout).

Visibility is controlled by `shouldShowExitWidget()` = `isOfflineModeActive() || isProcessingOfflineCases() || isOfflineModeServerSession()` — i.e., it shows precisely "while the user is in Offline mode", matching the request's condition.

### Finding 2: "Exit Offline Mode" click flow

**Evidence:** `offline-exit-manager.js:449-457` (`showExitOfflineModeModal`), `offline-modals.js:535-608` (`show_exit_offline_mode_modal`), `offline-exit-manager.js:479-511` (`confirmExitOfflineMode`).

**Detail:** Clicking the widget button calls `window.OfflineModals.showExitOfflineMode()`, which renders a confirmation modal warning that edited cases will "lose all changes" and new offline cases will be "permanently deleted". Confirming calls `OfflineExitManager.confirmExitOfflineMode()`, which does best-effort server cleanup (release case locks, mark offline session complete, sync logs) and redirects to `/Account/AutoLogin` — i.e., this is a genuine "abandon offline session without syncing" escape hatch, distinct from "Go Online" (which syncs first).

### Finding 3: "Go Online" text appears in (at least) 6 distinct places

**Evidence (button labels users see):**

- [source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js](source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js#L728) — `id="go-online-btn"` in the "Offline Case List" table footer, `onclick="show_go_online_modal(event)"`, label text `Go Online`.
- [source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js](source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js#L790) — second `id="go-online-btn"` in the "Cases Selected for Offline Work" table footer, `onclick="go_online_clicked(event)"` (bypasses the modal — see Backlog #3), label text `Go Online`.

**Evidence (dynamic state text on the same buttons, via `.button-text` span or `textContent`):**

- [source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-transition-manager.js](source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-transition-manager.js#L196-209) — `set_go_online_button_state(isBusy)`: sets text to `'Going Online...'` when busy, else `'Go Online'`.
- [source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-network-monitor.js](source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-network-monitor.js#L54-86) — connectivity-driven state updates, also set text to `'Go Online'` (both enabled/disabled branches).

**Evidence (confirmation modal, separate from the buttons above):**

- [source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js](source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js#L416) — modal title `Go Online` (`show_go_online_modal`).
- [source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js](source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js#L434) — modal confirm button, label `Go Online`.

## Deduced Conclusions

### Deduction 1: Removing the button is a UI-visibility change, not a full feature removal, unless the user says otherwise

**Based on:** Finding 1, Finding 2.

**Reasoning:** The request text says "Remove the button" (singular UI element), not "remove the feature". `OfflineExitManager`'s underlying cleanup logic (`confirmExitOfflineMode`, server calls to release locks/mark offline state complete) has no other caller in the codebase found so far, so leaving it in place as unreachable code is low-risk but arguably code debt Katrina's request doesn't ask to also clean up.

**Conclusion:** Default fix direction is to stop rendering/injecting the button (hide the host or skip widget initialization) while leaving the modal/manager code intact and dormant, unless the user confirms full removal is wanted. Backlog #1 is the open question to resolve before implementing.

### Deduction 2: The rename request most plausibly targets the primary action buttons, not every occurrence

**Based on:** Finding 3.

**Reasoning:** The two `id="go-online-btn"` elements in `app.mmria.js` are the actual clickable entry points a user acts on directly from the offline case list — these are almost certainly what Katrina means by "the 'Go Online' button". The dynamic state-text setters (`offline-transition-manager.js`, `offline-network-monitor.js`) write back to the _same_ DOM elements and must be updated in lockstep or the button will revert to the old label mid-flow (e.g., after a failed connectivity check). The confirmation modal (`offline-modals.js`) is a separate, secondary surface — whether it should also read "Go Online & Sync Changes" is a genuine open question, not a code fact.

**Conclusion:** All 4 button-label + state-text sites in `app.mmria.js`, `offline-transition-manager.js`, and `offline-network-monitor.js` must change together to keep the button state machine consistent regardless of the answer to Backlog #2. The modal (2 sites in `offline-modals.js`) is optional/pending user confirmation.

## Hypothesized Paths

_(none — this is a scoping/exploration case, not a defect; no unresolved causal hypotheses)_

## Missing Evidence

| Gap                                                                   | Impact                                                                                                      | How to Obtain                                                                            |
| --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| ADO work item #119294 full text                                       | Might contain exact wording/screenshots clarifying rename/removal scope                                     | No ADO MCP tool available this session; ask user to paste details or grant DevOps access |
| Whether "Exit Offline Mode" full removal (vs. hide) is wanted         | Determines whether `offline-modals.js` exit-modal code and `OfflineExitManager` public API also get deleted | Ask user (Backlog #1)                                                                    |
| Whether the "Go Online" confirmation modal text also needs the rename | Determines 2 additional edit sites in `offline-modals.js`                                                   | Ask user (Backlog #2)                                                                    |

## Source Code Trace

| Element                                    | Detail                                                                                                                                                                        |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Button 1 (Exit Offline Mode)               | `offline-exit-manager.js:404-421` renders into `[data-offline-exit-host]` in 4 views (Home/Index, Account/OfflineLogin, Shared/\_BreadCrumbs — the last is global via layout) |
| Button 2/3 (Go Online, static labels)      | `app.mmria.js:728, 790` (`id="go-online-btn"`, two independent render call sites feeding two different tables)                                                                |
| Button 2/3 (Go Online, dynamic state text) | `offline-transition-manager.js:196-209`, `offline-network-monitor.js:54-86`                                                                                                   |
| Modal (Go Online confirm)                  | `offline-modals.js:409-459` (`show_go_online_modal`)                                                                                                                          |
| Modal (Exit Offline confirm)               | `offline-modals.js:535-608` (`show_exit_offline_mode_modal`)                                                                                                                  |
| Related files                              | `offline-integrity-validator.js` (references "go online validation" in error copy, not a button), `docs/ai/offline_mode.md` (not updated for either feature)                  |

## Conclusion

**Confidence:** High (all render/text sites Confirmed via source read; no defect reasoning required — this is a scoping case).

Both changes are small, mechanical, well-bounded UI edits. The rename requires touching 4 JS files in lockstep to avoid the label reverting under busy/connectivity state changes. The removal is a one-line-per-host visibility change with a design choice (hide vs. delete) that should be confirmed with the user before implementation to avoid over- or under-scoping the diff.

## Recommended Next Steps

### Fix direction

1. **Remove 'Exit Offline Mode' button:** Stop showing the widget while offline — simplest, most reversible option is to make `shouldShowExitWidget()` always return `false` (or skip calling `initializeWidgetHosts()`/injecting markup entirely), keeping the manager and modal code dormant in place. Full deletion of `offline-exit-manager.js`'s public surface and the 4 host `<div>`s is a larger, separate cleanup the user can request explicitly.
2. **Rename 'Go Online' → 'Go Online & Sync Changes':** Update the literal string in all 4 sites that render/reset the primary button label — `app.mmria.js:728,790` (static labels) and `offline-transition-manager.js:203`, `offline-network-monitor.js:69,80` (dynamic reset text) — leaving the `'Going Online...'` busy-state text as-is unless the user wants that changed too. Confirm separately whether the `offline-modals.js` confirmation modal (title + button, lines 416/434) should also change.

### Diagnostic

None required — no defect, just confirm scope per Backlog #1 and #2 before implementing.

## Reproduction Plan

N/A (feature-scope change, not a repro case). Verification plan once implemented: go offline, confirm the exit widget no longer renders in breadcrumbs/Home/OfflineLogin views while offline; confirm both "Go Online" buttons (offline case list and cases-selected-for-offline table) read "Go Online & Sync Changes" in idle, busy, and connectivity-disabled states.

## Side Findings

- The two "Go Online" buttons in `app.mmria.js` (lines 728 and 790) have inconsistent click handlers: one opens a confirmation modal first (`show_go_online_modal`), the other calls `go_online_clicked` directly with no confirmation step. Not in scope of this request but worth flagging to the user/product as a UX inconsistency.
- `docs/ai/offline_mode.md` has never been updated to document the "Exit Offline Mode" feature at all, despite it being a real, shipped user flow with server-side cleanup calls. Worth a documentation follow-up regardless of this change's outcome.

## Follow-up: 2026-08-03

### New Evidence

User decisions on the two open Backlog items, plus scope expansion:

1. **Backlog #1 (removal scope) — resolved:** Hide only. Do not delete any code. `OfflineExitManager`, the exit-offline confirmation modal, and all server cleanup calls stay fully intact and dormant; functionality will be revisited "in another release."
2. **Backlog #2 (rename scope) — resolved:** Rename everywhere, including the confirmation modal in `offline-modals.js` — not just the two primary buttons. User's reasoning: consistent labeling is better UX.
3. **Scope addition:** The Side Finding about the two "Go Online" buttons having inconsistent click handlers (one confirms via modal, one doesn't) is promoted from a side finding into in-scope work for this epic — the two entry points should be made consistent (both routing through the confirmation modal).
4. User wants this formally tracked through the full BMAD process (epics, stories, sprint status) rather than jumping straight to ad hoc implementation.

### Additional Findings

None beyond what was already Confirmed in the initial pass — the user's answers resolved ambiguity without requiring new code exploration.

### Updated Hypotheses

N/A — no open hypotheses in this case (scoping case, not a defect).

### Backlog Changes

| #   | Path to Explore                                                    | Priority       | Status                   | Notes                                                                                                                                                                               |
| --- | ------------------------------------------------------------------ | -------------- | ------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Confirm hide vs. delete for Exit Offline Mode                      | High           | **Done**                 | Resolved: hide only, no deletion.                                                                                                                                                   |
| 2   | Confirm rename scope (buttons only vs. + modal)                    | High           | **Done**                 | Resolved: rename everywhere including the modal.                                                                                                                                    |
| 3   | Two "Go Online" buttons have inconsistent confirm behavior         | Low → **High** | **Promoted to in-scope** | Now tracked as Story 35.3 — align both entry points to confirm via modal.                                                                                                           |
| 4   | Track this work through full BMAD epic/story/sprint-status process | High           | **Done**                 | Added as Epic 35 to `_bmad-output/planning-artifacts/epics.md` (Stories 35.1–35.3) and registered in `_bmad-output/implementation-artifacts/sprint-status.yaml` (epic-35: backlog). |

### Updated Conclusion

**Confidence:** High. All scope questions are now resolved by explicit user decision; no further investigation is required before implementation. Work is now formally tracked as **Epic 35: Offline Exit/Go Online UX Cleanup (Rel 4.1 P-Immediate)** with three stories:

- **Story 35.1** — Hide Exit Offline Mode widget (code preserved).
- **Story 35.2** — Rename "Go Online" to "Go Online & Sync Changes" everywhere, including the confirmation modal.
- **Story 35.3** — Align the two "Go Online" entry points so both confirm via modal before syncing.

See `_bmad-output/planning-artifacts/epics.md` (Epic 35 section) for full acceptance criteria, implementation notes, and evidence per story, and `_bmad-output/implementation-artifacts/sprint-status.yaml` for tracking status.

**Status:** Concluded (investigation phase). Next recommended action: `bmad-create-story` for Story 35.1 (or all three, since each is small and independent) to produce dev-ready story files, then `bmad-dev-story` / `bmad-quick-dev` to implement.

## Follow-up: 2026-08-03 #2

### New Evidence

User added a requirement to Story 35.1 after story creation: the "You're Offline" text/icon indicator must remain visible (only the button is removed) and must be right-aligned within its host container now that the button no longer occupies that space.

### Additional Findings

This surfaced a scope correction needed in the initial Story 35.1 draft: the originally proposed fix point (`shouldShowExitWidget()` → always `false`) would have hidden the _entire_ widget, including the "You're Offline" text — contradicting the requirement that the indicator stay visible. The corrected fix point is `renderWidgetMarkup()` (remove only the button element from the template string); `shouldShowExitWidget()` is left unchanged since it correctly continues to gate the remaining indicator's visibility to "while offline."

Host layout context confirmed via read of all 3 views: `Shared/_BreadCrumbs.cshtml` and `Views/Account/OfflineLogin.cshtml` already have right-justified flex parents; `Views/Home/Index.cshtml`'s host `<div>` has no existing alignment styling, so it's the location most dependent on the widget's own new right-alignment rule.

### Updated Hypotheses

N/A — no open hypotheses in this case.

### Backlog Changes

Story 35.1 file and Epic 35 / FR-35.1 in `epics.md` updated to reflect: button removed from markup (not whole-widget hide), indicator stays visible, indicator right-aligned via wrapper CSS.

### Updated Conclusion

**Confidence:** High. Story 35.1 ([\_bmad-output/implementation-artifacts/35-1-hide-exit-offline-mode-widget.md](../35-1-hide-exit-offline-mode-widget.md)) and `epics.md` Epic 35 Story 35.1 are both updated and in sync. No further investigation required; ready for implementation.
