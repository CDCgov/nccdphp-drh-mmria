# Story 35.2: Rename Go Online to Go Online & Sync Changes

Status: review

<!-- baseline_commit: 56d69b7cb7157149c693c999620ac88ce10da778 -->

## Story

As a case reviewer working in offline mode,
I want the action to return online to be labeled "Go Online & Sync Changes",
so that it's clear the action both reconnects me and syncs my offline work.

## Acceptance Criteria

1. Given the "Offline Case List" table footer or the "Cases Selected for Offline Work" table footer is rendered while offline, when the primary action button is displayed, then its label reads "Go Online & Sync Changes" instead of "Go Online".
2. Given the button is clicked and connectivity is being checked or the transition is in progress, when `set_go_online_button_state(isBusy)` or the connectivity-driven handler in `offline-network-monitor.js` updates the button text, then the idle-state text reads "Go Online & Sync Changes" (the busy-state text `"Going Online..."` is unchanged).
3. Given the confirmation modal (`show_go_online_modal`) is opened, when the modal title and confirm button are rendered, then both read "Go Online & Sync Changes" (or a natural title-cased variant), consistent with the button label.
4. Given all four rename sites are updated, when the button cycles through idle → busy → idle again (e.g., a failed connectivity check resets state), then the label never reverts to the old "Go Online" text at any point in that cycle.

## Tasks / Subtasks

- [x] Rename the two static "Go Online" button labels in `app.mmria.js`. (AC: 1)
  - [x] Line ~728 — the button in the "Offline Case List" table footer (`onclick="show_go_online_modal(event)"`).
  - [x] Line ~790 — the button in the "Cases Selected for Offline Work" table footer (`onclick="go_online_clicked(event)"` today; see Story 35.3 for the handler alignment — rename the label text regardless of which handler it ends up calling).
- [x] Update the dynamic idle-state text setters so they match the new label. (AC: 2, 4)
  - [x] `offline-transition-manager.js` — `set_go_online_button_state(isBusy)` (~line 196-209): change the else-branch string from `'Go Online'` to `'Go Online & Sync Changes'`; leave `'Going Online...'` (busy branch) unchanged.
  - [x] `offline-network-monitor.js` — connectivity state handler (~lines 54-86): both branches that set `buttonText.textContent = 'Go Online'` (enabled and disabled connectivity states) need to become `'Go Online & Sync Changes'`.
- [x] Update the confirmation modal in `offline-modals.js`. (AC: 3)
  - [x] `show_go_online_modal()` (~line 416) — modal title `<h2 id="go-online-modal-title" ...>Go Online</h2>` → `Go Online & Sync Changes`.
  - [x] Same function (~line 434) — confirm button text `Go Online` → `Go Online & Sync Changes`.
- [x] Grep the full `wwwroot/scripts/offline/` and `wwwroot/scripts/editor/page_renderer/` trees for any remaining bare `'Go Online'` string literals (excluding `'Going Online...'` and non-button copy like log messages/img `alt` text) to confirm no site was missed.
- [x] Manually verify: go offline, observe both buttons read "Go Online & Sync Changes" in idle state; click one, confirm the modal also reads "Go Online & Sync Changes"; simulate a failed connectivity check (or busy state) and confirm the label returns to "Go Online & Sync Changes" (not the old text) once the busy/disabled state clears.

## Dev Notes

### Current Evidence

- This story originates from an investigation case file with full evidence citations: `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md` (Finding 3, Deduction 2).
- User decision (2026-08-03, recorded in the investigation Follow-up): **rename everywhere, including the confirmation modal** — not just the two primary buttons. Reasoning given: consistent labeling is better UX.
- 6 distinct text sites were found for "Go Online"; 4 are user-visible button/modal labels in scope for this story (2 static buttons + connectivity/busy-state resets + modal). Log-message copy (e.g., `offlineLog.log('OfflineTransitionManager', '=== Go Online Diagnostic Info ===')`) and `alt="Go Online Alert"` image attributes are **not** user-facing button text and are out of scope.

### Relevant Code

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js`:
  - Line ~728: `<button type="button" id="go-online-btn" ... onclick="show_go_online_modal(event)" ...>...Go Online</button>` (Offline Case List table).
  - Line ~790: `<button type="button" id="go-online-btn" ... onclick="go_online_clicked(event)" ...>...Go Online</button>` (Cases Selected for Offline Work table).
  - Note: both buttons share the DOM id `go-online-btn` but only one exists in the DOM at a time (different tables render conditionally) — this is pre-existing behavior, not something to fix in this story.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-transition-manager.js`:
  - `set_go_online_button_state(isBusy)` (~line 196): `goOnlineButton.querySelector('.button-text').textContent = isBusy ? 'Going Online...' : 'Go Online';` — change the false-branch string only.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-network-monitor.js`:
  - Connectivity handler (~lines 54-86): sets `buttonText.textContent = 'Go Online';` in both the "enabled" and "disabled" branches — update both occurrences.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js`:
  - `show_go_online_modal()` (~lines 409-459): modal title (`go-online-modal-title`, ~line 416) and confirm button (~line 434) both currently read `Go Online`.

### Scope Boundaries

- String/label changes only — no changes to click handlers, connectivity logic, or sync behavior in this story (handler alignment is Story 35.3).
- Do not change the busy-state string `'Going Online...'` unless the user separately requests it — the investigation and user decision only covered the idle-state "Go Online" label.
- Do not change non-button copy: diagnostic log messages (`offlineLog.log(...)`), the `offline-integrity-validator.js` error string `'go online validation requires offline session artifacts'`, or `alt="Go Online Alert"` image attributes in `index.js`/`app.mmria.js` — these are not the button being renamed.
- No server-side, CouchDB, or metadata changes. Client-side JS only, consistent with project-context.md's no-build-step vanilla JS rule for `wwwroot/`.
- Coordinate with Story 35.1 (Exit Offline Mode is hidden separately) and Story 35.3 (handler alignment) — all three stories touch adjacent code in the same files but change different things; land in any order, though 35.3 should follow this story so the aligned button already shows the new label (see Epic 35 Story Sequencing in epics.md).

### Project Structure Notes

- All 4 files are plain vanilla JS loaded via `<script src="...">` in Razor views — no bundler, no module system. Edits are simple string literal replacements at named call sites.
- The two `id="go-online-btn"` elements in `app.mmria.js` are generated from JS template strings (backtick literals), not `.cshtml` markup — the rename happens in the `.js` file, not in a Razor view.

### References

- Epic: `_bmad-output/planning-artifacts/epics.md` — Epic 35, Story 35.2.
- Investigation case file: `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md` — Finding 3 (all 6 text sites), Deduction 2 (rename scope reasoning).
- Related story: `_bmad-output/implementation-artifacts/35-3-align-go-online-confirmation-behavior.md` (handler alignment on the same buttons).
- Project rule: `_bmad-output/project-context.md` §5 (client-side JS, no build step).
- ADO: #119294 (Marta Puskarz) — Rel 4.1 P-Immediate change request from Katrina's feedback.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6 (bmad-agent-dev / Amelia)

### Debug Log References

- `node docs/ai/local/epic-35-offline-ux/verify-35-2-go-online-rename.js` — RED before implementation (7 assertions failed, all expected), GREEN after implementation (11/11 assertions passed). Re-verified GREEN (11/11) after the follow-up two-line button styling change. Re-verified GREEN (16/16) after the PR-review `.button-text` selector-mismatch fix.

### Completion Notes List

- Renamed all 4 in-scope "Go Online" sites to "Go Online & Sync Changes": 2 static button labels in `app.mmria.js`, the idle-state branch in `offline-transition-manager.js`'s `set_go_online_button_state`, both connectivity branches in `offline-network-monitor.js`, and the modal title + confirm button in `offline-modals.js`.
- Busy-state text `'Going Online...'` and all out-of-scope copy (diagnostic logs, the `offline-integrity-validator.js` error string, `alt="Go Online Alert"` image attributes, code comments) were left unchanged per Scope Boundaries — confirmed via grep sweep after implementation.
- **Side finding — RESOLVED (2026-08-03, PR #583 review by Copilot, Medium severity, both comments valid):** `set_go_online_button_state` and the connectivity handler in `offline-network-monitor.js` both look up `goOnlineButton.querySelector('.button-text')`, but neither button's markup in `app.mmria.js` contained an element with class `button-text` — only a bare text node after the `<img>` tag — making the busy-state and connectivity-disabled text updates dead code. Fix: wrapped each button's label in `<span class="button-text" data-idle-label="...">` in `app.mmria.js`. Because button #1 needs its two-line `<br>` markup preserved (per the button-styling follow-up above) while button #2 is single-line, the idle-state restore in both `set_go_online_button_state` and `update_go_online_button_state` now does `buttonText.innerHTML = buttonText.dataset.idleLabel || 'Go Online & Sync Changes'` instead of a hardcoded `textContent` assignment — this restores whichever button's own idle markup (two-line or one-line) is currently in the DOM, rather than collapsing button #1 back to one line after a busy/connectivity-disabled cycle. The busy-state branch (`'Going Online...'`) is unchanged and still uses plain `textContent` since it's never multi-line.
- Verification approach: created a Node-based static-content verification harness (`docs/ai/local/epic-35-offline-ux/verify-35-2-go-online-rename.js`) since this codebase has no JS test runner for `wwwroot/` (no build step, no package.json). The harness asserts exact string content at each named site — this is a full substitute for the AC's literal text requirements, but does not exercise a live browser. Recommend a final manual smoke test (go offline, observe both buttons/modal, and specifically cycle busy/connectivity-lost state on button #1 to confirm the two-line label is restored correctly) before merging.
- **Follow-up (2026-08-03):** per requirements-mockup review, the "Offline Case List" table's Go Online button (line ~728, `show_go_online_modal(event)`) now renders as two centered lines ("Go Online &" / "Sync Changes") instead of one long line — added `display: inline-flex; align-items: center; justify-content: center; text-align: center;` and a `<br>` between "Go Online &" and "Sync Changes"; bumped `line-height` from 1.15 to 1.3 for two-line readability. Scoped to this one button only, per explicit user direction ("this is for the offline case list button") — the "Cases Selected for Offline Work" button (line ~790) is unchanged and remains single-line. Updated the verification harness's button-#1 assertion to normalize `<br>` tags before checking for the label text, since the two-line markup no longer contains the label as one contiguous string.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js` — renamed both `go-online-btn` labels; restyled the "Offline Case List" button to two centered lines; wrapped both labels in `.button-text` spans with `data-idle-label`
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-transition-manager.js` — renamed idle-state text in `set_go_online_button_state`; idle restore now uses `data-idle-label` via `innerHTML`
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-network-monitor.js` — renamed both connectivity-branch button text assignments; idle restore now uses `data-idle-label` via `innerHTML`
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js` — renamed modal title and confirm button in `show_go_online_modal`
- `docs/ai/local/epic-35-offline-ux/verify-35-2-go-online-rename.js` — new static-content verification harness (11 assertions); updated for two-line markup; updated for `.button-text`/`data-idle-label` fix (16 assertions)
