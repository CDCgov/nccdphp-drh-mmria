# Story 35.3: Align Go Online Confirmation Behavior

Status: invalid — premise disproved, no code changed

> **INVALID (2026-08-04):** This story's premise does not hold. The "Cases Selected for Offline Work" table's `isOfflineStatus === 'true'` branch (the "Go Online & Sync Changes" / `go_online_clicked` variant this story targeted) is **unreachable dead code** — see Dev Agent Record below for the proof. That button variant never renders in practice; the table always shows "Go Offline" instead. There is no live inconsistency between the two buttons to fix. No implementation was performed. See the investigation case file's Follow-up #3 for full detail and recommended next steps.

## Story

As a case reviewer working in offline mode,
I want the same confirmation experience regardless of which table's button I click,
so that going online and syncing changes is predictable no matter where I trigger it from.

## Acceptance Criteria

1. Given the "Go Online & Sync Changes" button in the "Offline Case List" table (`show_go_online_modal(event)`, line ~728), when it is clicked, then the confirmation modal appears before `go_online_clicked` runs (existing, unchanged behavior).
2. Given the "Go Online & Sync Changes" button in the "Cases Selected for Offline Work" table (currently `go_online_clicked(event)` directly, line ~790), when it is clicked, then the same confirmation modal (`show_go_online_modal`) now appears first, and `go_online_clicked` only runs after the user confirms — matching the first entry point.
3. Given either entry point is confirmed, when `go_online_clicked` executes, then existing connectivity checks, diagnostic logging, and sync behavior are unchanged — only the missing confirmation step is added to the second entry point.

## Tasks / Subtasks

- [ ] Change the second "Go Online" button's click handler to match the first. (AC: 2)
  - [ ] In `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js`, at the button in the "Cases Selected for Offline Work" table footer (~line 790), change `onclick="go_online_clicked(event)"` to `onclick="show_go_online_modal(event)"`.
- [ ] Confirm the modal's own confirm button still calls `go_online_clicked(event)` unmodified. (AC: 1, 3)
  - [ ] `offline-modals.js` `show_go_online_modal()` confirm button (~line 434) already does `onclick="go_online_clicked(event)"` — do not change this.
- [ ] Confirm no double-invocation or race condition is introduced. (AC: 3)
  - [ ] Both buttons now render the same modal via the same function (`show_go_online_modal`); verify `show_go_online_modal()` doesn't assume a specific caller context (it doesn't take arguments from the triggering button today — confirm this remains true after the change).
- [ ] Manually verify both entry points: click the button in each of the two tables (may require test data with both an "Offline Case List" and a "Cases Selected for Offline Work" table populated in offline mode) and confirm both now show the confirmation modal before syncing, with identical modal copy (per Story 35.2's rename).

## Dev Notes

### Current Evidence

- This story originates from a Side Finding in the investigation, later promoted to in-scope work at the user's explicit request (2026-08-03): "let's add to our discussion about the two 'Go Online' buttons behaving differently... we'll want to add it." See `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md`, Follow-up section, Backlog item #3.
- The two buttons render in two different, mutually-exclusive tables in `app.mmria.js`:
  - "Offline Case List" table (~line 706-750): shown when `isOfflineStatus === 'true'` — button already routes through `show_go_online_modal(event)`.
  - "Cases Selected for Offline Work" table (~line 768-810): shown under a different condition (`is_offline_mode_enabled && isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true'`) — button currently bypasses the modal and calls `go_online_clicked(event)` directly.
- This is a minimal, one-line-per-button change: point the second button's `onclick` at the same function the first button already uses.

### Relevant Code

- Primary file: `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js`.
  - Line ~728 (reference/no-change): `onclick="show_go_online_modal(event)"`.
  - Line ~790 (change target): `onclick="go_online_clicked(event)"` → `onclick="show_go_online_modal(event)"`.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js`:
  - `show_go_online_modal()` (~lines 409-459) — renders the modal; its own confirm button already calls `go_online_clicked(event)` (~line 434) — this is the single, unchanged path both entry points will now share.
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-transition-manager.js`:
  - `go_online_clicked(event)` (function definition) — connectivity check, diagnostic logging, and sync logic. Not modified by this story; only its callers change.

### Scope Boundaries

- Do not modify `go_online_clicked`'s internal logic (connectivity check, diagnostic logging via `offlineLog`, `set_go_online_button_state`, sync behavior) — only the triggering `onclick` wiring on the second button changes.
- Do not modify `show_go_online_modal()` or `close_go_online_modal()` beyond what Story 35.2 already does for label text — this story only changes which button calls `show_go_online_modal`.
- This story should land **after** Story 35.2 (per Epic 35 Story Sequencing in epics.md) so that when the second button starts routing through the modal, the modal already shows the correct "Go Online & Sync Changes" copy rather than the old "Go Online" text.
- No server-side, CouchDB, or metadata changes. Client-side JS only.

### Project Structure Notes

- Both buttons are generated from JS template-string literals inside `app.mmria.js`, not separate `.cshtml` partials — the fix is a single attribute-value change in that one file.
- `show_go_online_modal` is globally available (attached via plain `<script>` tags, no module scoping), so referencing it from either table's render function requires no additional wiring or imports.

### References

- Epic: `_bmad-output/planning-artifacts/epics.md` — Epic 35, Story 35.3.
- Investigation case file: `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md` — Side Findings (original observation) and Follow-up section (scope promotion decision).
- Related story: `_bmad-output/implementation-artifacts/35-2-rename-go-online-to-go-online-and-sync-changes.md` (label rename on the same buttons/modal — should land first).
- Project rule: `_bmad-output/project-context.md` §5 (client-side JS, no build step).
- ADO: #119294 (Marta Puskarz) — Rel 4.1 P-Immediate change request from Katrina's feedback.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6 (bmad-agent-dev / Amelia)

### Debug Log References

- Verified via `grep` that `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js` contains no `localStorage.setItem('is_offline', ...)` call anywhere in the file.

### Completion Notes List

- **Story invalidated (2026-08-04) — no implementation performed.** Before implementing the one-line `onclick` change, traced both flags this story's AC depends on:
  - `isOfflineMode` (line ~300) and `isOfflineStatus` (line ~461) are both declared as `const ... = localStorage.getItem('is_offline') || 'false';` inside the **same synchronous function**, `app_render` (no `await` anywhere in that function, confirmed by inspection).
  - Since `app_render` runs to completion synchronously and nothing in the file ever calls `localStorage.setItem('is_offline', ...)`, `isOfflineMode` and `isOfflineStatus` are **guaranteed identical** for the entire duration of any single `app_render` invocation.
  - The "Cases Selected for Offline Work" table only renders when `is_offline_mode_enabled && isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true'` (outer gate). Its inner button ternary checks `isOfflineStatus === 'true' ? <Go Online button, onclick="go_online_clicked(event)"> : <Go Offline button>`.
  - Because `isOfflineStatus` always equals `isOfflineMode`, and the outer gate already requires `isOfflineMode !== 'true'` to enter the block, the inner `isOfflineStatus === 'true'` condition is **always false** whenever this code executes. The "Go Online & Sync Changes" branch this story targeted (AC 2, line ~790) can never render — the table always shows "Go Offline" instead.
  - This means there is no reachable "Go Online" button in that table missing a confirmation step, so AC 2 and AC 3 (which depend on that button existing and being clickable) cannot be satisfied or verified, because the code path they describe is dead.
- No files were modified. No `onclick` change was made to avoid "fixing" a branch that can never execute, which would be a no-op change presented as if it resolved a real inconsistency.
- **Recommendation for the team:** decide whether this dead conditional is a leftover bug (e.g., `isOfflineStatus` should check something case-specific rather than the global `is_offline` flag) or intentional-but-then-abandoned code, and whether it's worth a small cleanup story on its own. That decision is out of scope for Epic 35 as currently defined.

### File List

_(none — no code changes made)_
