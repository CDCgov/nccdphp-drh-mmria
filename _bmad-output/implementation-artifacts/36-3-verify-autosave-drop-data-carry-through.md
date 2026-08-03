# Story 36.3 — Verify Autosave-Drop Data Carry-Through

**Epic:** 36 — Case Save Queue Reconcile — Idle Network Recovery Fix
**Story ID:** 36.3
**Status:** done
**Date added:** 2026-08-03
**Depends on:** Story 36.1 (same file — `index.js`)
**Source requirements:** FR-36.4

---

## User Story

As a case reviewer,
I want assurance that when a retrying autosave is dropped in favour of an awaited inactivity save, none of my edits are silently lost,
So that I can trust the inactivity save captures my full work even if I didn't manually click Save.

---

## Acceptance Criteria

**AC-1 — Awaited save data is always cloned from the live g_data at enqueue time**

Given autosave A is retrying (network failure, in backoff — `save_queue.active_item` is null)
And an inactivity save B is enqueued via `save_case_and_wait(g_data, null, 'edit_inactivity_continue')` or `save_case_and_wait(g_data, null, 'edit_inactivity_lock_release', ...)`
When `get_new_save_queue_item` runs for B
Then `B.data` is a deep clone of `g_data` at B's enqueue time — the live, up-to-date case document
And `B.data` is NOT a reference to or derived from A's stale `data` clone

**AC-2 — When A is dropped, B carries all edits made up to B's enqueue time**

Given the `awaited_save_is_queued` guard in `schedule_retry_or_fail` drops A
And B then processes
When the server returns `ok: true` for B
Then `g_data._rev` is updated to the new revision returned by B's save
And the case document in CouchDB reflects all field values present in `g_data` at B's enqueue time

**AC-3 — Both inactivity paths carry through correctly**

Given the "Continue Editing" path (`edit_inactivity_continue`) in `edit-inactivity-manager.js`
When `save_case_and_wait(g_data, null, 'edit_inactivity_continue')` is called
Then `g_data` is passed directly — not a pre-modified intermediate copy

Given the lock-release path (`edit_inactivity_lock_release`) in `edit-inactivity-manager.js`
When `save_case_and_wait(g_data, null, 'edit_inactivity_lock_release', { authRefreshPolicy: 'suppress' })` is called
Then `g_data` has been updated with cleared checkout fields (`date_last_checked_out = null`, `last_checked_out_by = null`) before the call — this is correct and intentional
And `get_new_save_queue_item` clones this updated `g_data` as B's payload

**AC-4 — Data carry-through holds regardless of inactivity configuration values**

Given any combination of `case_edit_inactivity_warning_minutes_before_lock` and `case_edit_inactivity_lock_minutes` in the site config
Then AC-1 through AC-3 hold — the behaviour is not threshold-dependent

---

## Background

When autosave A is in its retry backoff period (`save_queue.active_item = null`), the prune guard in `mmria_prune_nonblocking_save_queue_items_for_case` does NOT protect A. Enqueuing the awaited inactivity save B causes A to be pruned from the queue. B then becomes the sole pending save and carries the full case data.

This story verifies that `get_new_save_queue_item` always clones the **live** `g_data` reference passed to it — not some older snapshot — so the pruned A's earlier data snapshot is irrelevant. If `g_data` is the live reference at both inactivity call sites in `edit-inactivity-manager.js`, no code change is needed.

---

## Dev Notes — Implementation

### Files to read

```
source-code/mmria/mmria-server/wwwroot/scripts/case/index.js
source-code/mmria/mmria-server/wwwroot/scripts/case/edit-inactivity-manager.js
```

### Step 1 — Confirm get_new_save_queue_item clones its input

In `index.js`, locate `get_new_save_queue_item`. Confirm:

```javascript
const cloned_data = mmria_safe_clone(p_data);
```

`mmria_safe_clone` performs a deep clone. This means whatever `g_data` reference is passed as `p_data`, the queue item receives an independent snapshot of it at enqueue time. No reference aliasing to A's stale data is possible.

### Step 2 — Confirm the "Continue Editing" path passes g_data

In `edit-inactivity-manager.js`, locate `continue_editing_inactivity()`. Confirm the call is:

```javascript
await save_case_and_wait(g_data, null, 'edit_inactivity_continue');
```

And NOT a pre-modified copy (e.g., not `structuredClone(g_data)` with intermediate mutations). If the call passes `g_data` directly, the clone happens inside `get_new_save_queue_item` with the live state at call time. ✓

### Step 3 — Confirm the lock-release path mutates g_data then passes it

In `edit-inactivity-manager.js`, locate `release_edit_lock_due_to_inactivity()`. Confirm the sequence:

```javascript
g_data.date_last_checked_out = null;      // mutate live g_data
g_data.last_checked_out_by = null;        // mutate live g_data
g_data.checked_out_by_tab_id = release_tab_id;
g_data_is_checked_out = false;
// ...
await save_case_and_wait(g_data, null, 'edit_inactivity_lock_release', { authRefreshPolicy: 'suppress' });
```

The mutation happens before `save_case_and_wait` — so `get_new_save_queue_item` clones the already-mutated `g_data`. The checkout fields are cleared in the saved document. This is the correct and intended behaviour. ✓

### Step 4 — Add confirming code comments

If Steps 1–3 all confirm correct behaviour, add brief comments at the relevant lines to document the intent for future maintainers.

**In `get_new_save_queue_item` (index.js):**

```javascript
// Deep-clone the live case data at enqueue time. If a prior queued save is later
// dropped (e.g. schedule_retry_or_fail's awaited_save_is_queued guard), this item's
// data still reflects the complete case state at the moment this save was requested.
const cloned_data = mmria_safe_clone(p_data);
```

**In `continue_editing_inactivity` (edit-inactivity-manager.js):**

```javascript
// Pass live g_data — get_new_save_queue_item will deep-clone it at enqueue time,
// capturing all edits made up to this moment.
await save_case_and_wait(g_data, null, 'edit_inactivity_continue');
```

**In `release_edit_lock_due_to_inactivity` (edit-inactivity-manager.js), just before the save call:**

```javascript
// g_data has already been mutated above to clear checkout fields.
// get_new_save_queue_item will deep-clone this updated state — the saved document
// correctly reflects the lock release.
await save_case_and_wait(g_data, null, 'edit_inactivity_lock_release', { authRefreshPolicy: 'suppress' });
```

### Step 5 — If a gap is found

If either inactivity path passes a stale or intermediate copy of `g_data` (not the live reference with all edits), fix the call site to pass `g_data` directly. Do not change `get_new_save_queue_item` — the clone must remain inside it for all callers.

---

## Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Add confirming comment in `get_new_save_queue_item`; fix any gap found |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/edit-inactivity-manager.js` | Add confirming comments in `continue_editing_inactivity` and `release_edit_lock_due_to_inactivity`; fix any gap found |

---

## Story Sequencing

| Dependency | Risk |
|---|---|
| Story 36.1 must be complete (both touch `index.js`) | Low |

---

## Dev Agent Record

**Completion Date:** 2026-08-03

**Completion Notes:**
All three verification checks passed — no code logic changes were required. Data carry-through was already correct:
- AC-1/AC-2: `get_new_save_queue_item` in `index.js` uses `mmria_safe_clone(p_data)` — a deep clone of whatever live reference is passed. Any dropped autosave's stale data cannot contaminate a subsequently enqueued awaited save.
- AC-3 (Continue Editing path): `continue_editing_inactivity` passes `g_data` directly — not a pre-modified copy.
- AC-3 (Lock Release path): `release_edit_lock_due_to_inactivity` mutates `g_data` in-place (clears checkout fields) before calling `save_case_and_wait`, which is correct — the clone captures the fully updated state.
- AC-4: The behaviour is independent of inactivity timer configuration values.

Deliverable: confirming code comments added at all three relevant sites.

**File List:**
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/edit-inactivity-manager.js`

**Change Log:**

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Added confirming comment before `mmria_safe_clone` in `get_new_save_queue_item` |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/edit-inactivity-manager.js` | Added confirming comment before `save_case_and_wait` in `continue_editing_inactivity`; added confirming comment before `save_case_and_wait` in `release_edit_lock_due_to_inactivity` |
