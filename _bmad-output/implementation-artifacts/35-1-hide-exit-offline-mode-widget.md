# Story 35.1: Hide Exit Offline Mode Widget

Status: review

<!-- baseline_commit: 56d69b7cb7157149c693c999620ac88ce10da778 -->

## Story

As a case reviewer working in offline mode,
I want the "Exit Offline Mode" button to no longer appear while still seeing a clear "You're Offline" indicator,
so that I am not tempted into an escape hatch that discards my offline changes, while the underlying code stays available for a future release.

## Acceptance Criteria

1. Given a user is in offline mode (`isOfflineModeActive()`, `isProcessingOfflineCases()`, or `isOfflineModeServerSession()` is true), when any page renders a `[data-offline-exit-host]` widget host (Home/Index, Account/OfflineLogin, Shared/\_BreadCrumbs), then no "Exit Offline Mode" button is visible anywhere on the page.
2. Given a user is in offline mode, when the widget renders, then the "You're Offline" text and icon indicator remain visible (only the button is removed from view) — the widget is not hidden in its entirety.
3. Given the button is no longer shown, when the "You're Offline" indicator renders in any of the 3 host locations, then it is right-aligned within its host container (matching the layout's existing right-side placement conventions, e.g. the breadcrumbs row's `justify-content: space-between` and the offline-login row's `justify-content-end`).
4. Given the "Exit Offline Mode" button is hidden, when the codebase is reviewed, then `OfflineExitManager`'s underlying logic (`confirmExitOfflineMode`, cleanup/audit calls) and the `show_exit_offline_mode_modal` confirmation modal in `offline-modals.js` remain fully present and unmodified in behavior — only the button's markup/visibility and the indicator's alignment change.
5. Given a user was mid-flow with a pending deferred cleanup (`mmria_exit_offline_cleanup_pending`) before this change ships, when they return online, then `finishPendingCleanup()` still runs unaffected, since only the button's rendering is suppressed, not the cleanup pipeline.

## Tasks / Subtasks

- [x] Remove the "Exit Offline Mode" button from the widget's rendered markup while keeping the "You're Offline" indicator. (AC: 1, 2)
  - [x] In `offline-exit-manager.js`, edit `renderWidgetMarkup()`: delete (or comment out) the `<button data-action="show-exit-offline-mode">...Exit Offline Mode</button>` element from the returned template string. Keep the `<div>` containing the offline-info icon and `<span>You're Offline</span>` text.
  - [x] Do **not** change `shouldShowExitWidget()` — it correctly controls whether the whole indicator (text + icon) shows while offline, and that behavior must be preserved.
  - [x] In `initializeWidgetHosts()`, the existing `if (actionButton && !actionButton.dataset.exitWidgetBound)` guard already tolerates `actionButton` being `null` once the button is removed from markup — verify no error is thrown and no click binding attempt occurs.
- [x] Right-align the remaining "You're Offline" indicator within its host container. (AC: 3)
  - [x] Update the outer wrapper style in `renderWidgetMarkup()` (`.mmria-offline-exit-widget`) so its content sits flush right within the host `<div>` now that the button no longer fills that space — e.g. add `width: 100%; justify-content: flex-end;` to the existing `display: inline-flex; align-items: center; gap: 16px; padding: 6px 0;` style, or an equivalent host-level right-alignment rule.
  - [x] Verify this works correctly in all 3 host contexts: `Shared/_BreadCrumbs.cshtml` (already `display: flex; justify-content: space-between`, so the host div is a flex item — confirm the widget's own internal alignment doesn't fight the parent's layout), `Views/Account/OfflineLogin.cshtml` (host row already uses `d-flex justify-content-end` — confirm no double-alignment conflict), and `Views/Home/Index.cshtml` (host `<div>` has no existing flex/alignment styling — this is the location most likely to need the new right-alignment rule to take effect).
- [x] Confirm no deletions to dormant code. (AC: 4, 5)
  - [x] Do not remove `confirmExitOfflineMode`, `showExitOfflineModeModal`, `closeExitOfflineModeModal`, `finishPendingCleanup`, `hasPendingCleanup`, or any of the `window.OfflineExitManager` public exports — only the button's markup is removed from `renderWidgetMarkup()`.
  - [x] Do not remove `show_exit_offline_mode_modal` / `close_exit_offline_mode_modal` from `offline-modals.js` (they become unreachable via UI but must remain in code).
  - [x] Do not remove the `[data-offline-exit-host]` host `<div>` elements from `Views/Home/Index.cshtml`, `Views/Account/OfflineLogin.cshtml`, or `Views/Shared/_BreadCrumbs.cshtml`.
- [x] Manually verify across all three host pages while offline that: the button is not visible, the "You're Offline" text/icon is visible and right-aligned within its host, and going online afterward still works normally via the (separately renamed/aligned) Go Online button.

## Dev Notes

### Current Evidence

- The "Exit Offline Mode" feature is very recent — only 3 commits exist against `offline-exit-manager.js` (`e7dbe212b exit offline button`, `3bbdacf37 fixing an edge case`, `2ca9b9b8c *508 fixes for exit offline widge...`). Treat it as a young, still-settling feature; be conservative with changes.
- This story originates from an investigation case file with full evidence citations: `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md` (Finding 1, Finding 2, Deduction 1).
- User decision (2026-08-03, recorded in the investigation Follow-up): **hide only, do not delete any code** — functionality will be revisited in a future release.
- User addition (2026-08-03): the "You're Offline" text indicator must remain visible (this story only removes the button) and must be right-aligned within its host now that the button no longer occupies that space.
- **Correction to initial approach:** the fix point is `renderWidgetMarkup()` (remove the button element only), **not** `shouldShowExitWidget()`. Setting `shouldShowExitWidget()` to always return `false` would incorrectly hide the entire indicator (text + icon), which the user wants to keep visible. Leave `shouldShowExitWidget()` exactly as-is — it correctly gates the whole widget's visibility to "while offline," which still applies to the remaining text/icon.

### Relevant Code

- Primary file: `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-exit-manager.js`.
  - `renderWidgetMarkup()` (~line 404) — **this is the fix point.** Currently returns a template string with an outer `.mmria-offline-exit-widget` div (`display: inline-flex; align-items: center; gap: 16px; padding: 6px 0;`) containing (a) an inner div with the offline-info icon + `<span>You're Offline</span>`, and (b) a `<button data-action="show-exit-offline-mode">` with an arrow span and `<span>Exit Offline Mode</span>`. Remove (b); keep (a); add right-alignment styling to the outer wrapper.
  - `initializeWidgetHosts()` (~line 426) injects markup into every `[data-offline-exit-host]` element; its `actionButton` lookup (`host.querySelector('[data-action="show-exit-offline-mode"]')`) already null-checks before binding, so it degrades gracefully once the button is removed from markup.
  - `updateWidgetVisibility()` (~line 466) toggles `host.style.display` based on `shouldShowExitWidget()` — **do not change this function or `shouldShowExitWidget()`** (~line 576); both continue to correctly gate the remaining text/icon indicator's visibility to "while offline."
  - `document.addEventListener('DOMContentLoaded', ...)` (near end of file) calls `initializeWidgetHosts()` and `finishPendingCleanup()` — leave both calls intact.
- Host `<div>` locations and their existing layout context (relevant to the new right-alignment requirement):
  - `source-code/mmria/mmria-server/Views/Shared/_BreadCrumbs.cshtml` (line ~25) — `id="offline-mode-indicator" data-offline-exit-host="breadcrumbs"`, inside a parent `<div style="display: flex; justify-content: space-between; align-items: center;">` alongside the breadcrumb nav — already positioned on the right side of that flex row; visible on essentially every page via shared layout.
  - `source-code/mmria/mmria-server/Views/Account/OfflineLogin.cshtml` (line ~43) — `id="offline-login-exit-widget" data-offline-exit-host="offline-login"`, inside `<div class="container d-flex justify-content-end mb-3">` — parent row is already right-justified.
  - `source-code/mmria/mmria-server/Views/Home/Index.cshtml` (line ~167) — `id="offline-home-exit-widget" data-offline-exit-host="home"`, a plain block-level `<div>` with **no** existing flex or right-alignment styling on its parent — this host needs the widget's own internal right-alignment rule to actually take visual effect.
- Confirmation modal (leave untouched, just becomes unreachable): `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-modals.js`, `show_exit_offline_mode_modal()` (~line 535).

### Scope Boundaries

- This is a **hide-only** change per explicit user decision. Do not delete `OfflineExitManager`'s public API, the exit confirmation modal, or any server cleanup call (`/api/OfflineCase/update-offline-state`, `/api/OfflineCase/release-case-locks`).
- The "You're Offline" text/icon indicator is **not** in scope for removal — only the button. Right-alignment is a CSS-only change to the existing wrapper/markup, not a redesign of the indicator.
- No server-side, CouchDB, or metadata changes. Client-side JS only.
- Do not touch the "Go Online" button/text — that is Story 35.2. Do not touch the two Go Online buttons' click handlers — that is Story 35.3.
- Per project-context.md §5: `wwwroot/` is vanilla JS with no build step — do not introduce bundlers, npm dependencies, or TypeScript.
- Per project-context.md §6 (Offline Mode): do not alter the seven fields cleared by `ForceRemoveOfflineLockAsync`, and do not touch `offline-integrity-validator.js` health checks — this story does not touch server-side offline lock logic at all.

### Project Structure Notes

- `wwwroot/scripts/offline/*.js` are all loaded directly by Razor views via `<script src="...">` — no module bundling, no import/export syntax expected in this codebase's client JS.
- `[data-offline-exit-host]` is a shared selector pattern across 3 distinct Razor views plus the shared breadcrumbs partial — a single JS-level change in `shouldShowExitWidget()` covers all of them, since they all funnel through the same `OfflineExitManager` module. No per-view changes are needed.

### References

- Epic: `_bmad-output/planning-artifacts/epics.md` — Epic 35, Story 35.1.
- Investigation case file: `_bmad-output/implementation-artifacts/investigations/epic-35-remove-exit-offline-rename-go-online-investigation.md` — Finding 1 (widget/host mapping), Finding 2 (click flow), Deduction 1 (hide vs. delete reasoning).
- Project rule: `_bmad-output/project-context.md` §5 (client-side JS, no build step), §6 (offline mode constraints).
- ADO: #119294 (Marta Puskarz) — Rel 4.1 P-Immediate change request from Katrina's feedback.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6 (bmad-agent-dev / Amelia)

### Debug Log References

- `node docs/ai/local/epic-35-offline-ux/verify-35-1-hide-exit-offline-button.js` — RED before implementation (4 assertions failed, all expected), GREEN after implementation (19/19 assertions passed).

### Completion Notes List

- Removed the `<button data-action="show-exit-offline-mode">` element entirely from `renderWidgetMarkup()`'s template string; kept the icon + `<span>You're Offline</span>` div untouched.
- Added `width: 100%; justify-content: flex-end;` to the outer `.mmria-offline-exit-widget` wrapper's inline style so the remaining indicator sits flush right within its host now that the button no longer occupies that space. Verified via the harness that this doesn't conflict with the pre-existing parent-level right-alignment in `_BreadCrumbs.cshtml` (`justify-content: space-between`) or `OfflineLogin.cshtml` (`d-flex justify-content-end`) — the widget's own `width: 100%` makes it fill its flex-item slot in both cases, and `justify-content: flex-end` then pushes its own content (icon + text) to the right edge of that slot. `Views/Home/Index.cshtml`'s host `<div>` has no parent flex context, so the widget's own `width: 100%` + `flex-end` is what actually produces the right alignment there.
- `shouldShowExitWidget()` was deliberately left unchanged (per the investigation's documented correction) — it still correctly gates the whole indicator's visibility to "while offline," and that's exactly the behavior needed for the remaining text/icon.
- Confirmed no code was deleted: `OfflineExitManager`'s full public API (`confirmExitOfflineMode`, `showExitOfflineModeModal`, `closeExitOfflineModeModal`, `finishPendingCleanup`, `hasPendingCleanup`) and `offline-modals.js`'s `show_exit_offline_mode_modal`/`close_exit_offline_mode_modal` are all still present and unmodified — only unreachable via UI now, as intended (hide-only, not delete).
- `initializeWidgetHosts()` required no code change: its existing `if (actionButton && !actionButton.dataset.exitWidgetBound)` guard already handles `actionButton` being `null` now that the button is absent from markup — confirmed by inspection, no click-binding attempt occurs.
- Verification approach: static-content Node harness (`docs/ai/local/epic-35-offline-ux/verify-35-1-hide-exit-offline-button.js`), same rationale as Story 35.2 (no JS test runner exists for `wwwroot/`). Recommend a final manual smoke test across all 3 host pages (Home, OfflineLogin, breadcrumbs-bearing pages) while offline, per the story's last task, to visually confirm the right-alignment renders as expected in a real browser.

### File List

- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-exit-manager.js` — removed exit button from `renderWidgetMarkup()`, added right-alignment styling to the wrapper
- `docs/ai/local/epic-35-offline-ux/verify-35-1-hide-exit-offline-button.js` — new static-content verification harness (19 assertions)
