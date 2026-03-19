# Case Save and Lock History 3/19/2026

This document summarizes the main case-related behavior changes from mid-February 2026 through March 19, 2026, with emphasis on:

- case save behavior
- edit lock behavior
- single-tab enforcement
- case-list delete behavior
- case-list offline soft-lock behavior
- edit-mode inactivity monitoring

## Main files
- `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-case-manager.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/edit-inactivity-manager.js`
- `source-code/mmria/mmria-server/Controllers/api/caseController.cs`

## Timeline

### 2026-02-20
- `offline_lock_type` was introduced so the system can distinguish soft offline locks from hard offline locks.
- This became important later because unload cleanup only removes soft locks.

### 2026-03-01
- Active case lock enforcement was added on save.
- Server save now blocks updates when another user still holds the active edit lock.
- Client edit entry also started reloading the case before checkout so it does not enter edit mode on stale data.

### 2026-03-01 to 2026-03-02
- Single-tab edit enforcement was added.
- `checked_out_by_tab_id` became part of case lock ownership.
- A case can no longer be actively edited in two tabs by the same user.
- The client now waits for the checkout save to succeed before switching the page into edit mode.

### 2026-03-02
- The edit lock became a sliding lock.
- Every successful save refreshes `date_last_checked_out`, so active editing keeps extending the lock window.
- `finalize-unload` was added to release the current edit lock and remove same-tab offline soft locks during refresh/navigation.
- `finalize-unload` does not remove hard offline locks.
- The client save queue was reworked:
  - FIFO processing
  - priority handling for awaited/user-driven saves
  - pruning redundant autosaves
  - retry/backoff for non-blocking saves
  - fail-fast handling for awaited saves like entering edit mode

### 2026-03-03
- Lock-related user modals were added and refined.
- `GET /api/case` was marked `no-store/no-cache` to reduce stale case payloads and stale lock state.

### 2026-03-04
- Delete behavior was tightened.
- Delete now sends `tab_id`.
- The server blocks delete when the case:
  - is offline
  - is locked by another user
  - is locked by the same user in another tab
- Case-list delete also got better confirmation/status handling.
- Offline soft-lock add/remove started carrying `tab_id`.
- Offline toggle now respects both active edit locks and offline-tab ownership.

### 2026-03-16
- Offline soft locks became single-tab for the user.
- Adding a case to offline mode now requires `tab_id`.
- If the user already has soft-locked cases in tab A, tab B cannot add another case to offline mode.
- Save also blocks if the case is offline in another tab for the same user.
- Offline conflict handling improved:
  - same-user/different-tab conflicts get a tab-conflict modal
  - cross-user lock conflicts get a locked-by-user modal

### 2026-03-17
- Case-list offline soft-lock add got cleaner conflict routing.
- Same-user/other-tab conflicts and different-user lock conflicts now use separate messages/modals.

### 2026-03-19
- Edit-mode inactivity monitoring was added.
- It only runs while the case is in edit mode.
- It warns before timeout.
- The user can continue editing by triggering an immediate save.
- If inactivity reaches the configured limit, the client auto-saves, ends edit mode, and shows a confirmation modal.
- New config keys:
  - `case_edit_inactivity_lock_minutes`
  - `case_edit_inactivity_warning_minutes_before_lock`
- Controller defaults:
  - lock = `120`
  - warning = `110`

## Current behavior by scenario

### Trying to edit a case while another user has it locked
- The client reloads the case before entering edit mode.
- If another user still owns the active lock, edit mode is blocked.
- The UI shows the locked-by-another-user modal.
- The server also rejects any save that slips through with a lock message naming the other user.

### Trying to edit the same case in a second tab
- Edit mode is single-tab for the same user.
- The second tab is blocked before entering edit mode.
- The server rejects save attempts if the stored `checked_out_by_tab_id` does not match the current tab.
- The page only treats the case as checked out in the tab that owns the lock.

### Trying to soft-lock a case from the case list when it is already locked
- The server distinguishes:
  - same user, different tab
  - different user owns the lock
- The case-list UI now shows different modals for those two scenarios.

### Trying to remove a soft lock from the wrong tab
- Removal is blocked unless the same user and same `offline_by_tab_id` own the soft lock.

### Trying to delete from the case list while the case is locked
- The server blocks delete if the case:
  - is offline
  - is actively locked by another user
  - is locked by the same user in another tab
- The case-list UI shows a delete status modal, but its 409 messaging is still more generic than the offline add flow.

## Save/lock behavior changes that mattered most
- Real server-side active lock enforcement on case save
- Single-tab ownership for edit mode
- Checkout save must succeed before the page enters edit mode
- Sliding edit lock on every successful save
- Save queue prioritization and retry behavior
- `finalize-unload` cleanup for normal refresh/navigation
- Inactivity monitoring to stop autosave from keeping edit sessions alive indefinitely

## Current caveats
- `show_locked_case_modal()` is still used for same-user/different-tab edit conflicts, but the current modal text is offline-specific. That is a message mismatch.
- The delete-status modal does not currently distinguish all 409 causes as clearly as the offline add flow.
- `finalize-unload` is best-effort only. It helps on normal navigation and refresh, but crashes or hard tab failures can still leave locks behind because unload cleanup may never run.
