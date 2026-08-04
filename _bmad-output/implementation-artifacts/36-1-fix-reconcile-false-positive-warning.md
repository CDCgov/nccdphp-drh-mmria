# Story 36.1 — Fix False-Positive Reconcile Warning

**Epic:** 36 — Case Save Queue Reconcile — Idle Network Recovery Fix
**Story ID:** 36.1
**Status:** done
**Date added:** 2026-08-03
**Depends on:** None
**Source requirements:** FR-36.1

---

## User Story

As a case reviewer,
I want the absence of console warnings to reliably mean my edits may not be saved,
So that developers and reviewers are not misled by false-positive "unmatched edits" alerts during normal network-recovery save sequences.

---

## Acceptance Criteria

**AC-1 — No warning when the prior save already committed the items**

Given autosave A enqueued with `change_stack_items: [item1, item2, item3]`
And A succeeded and `mmria_reconcile_live_save_state_after_success` spliced those items from `g_change_stack`, leaving it empty
And inactivity save B was also enqueued with `change_stack_items: [item1, item2, item3]` (same items, captured before A completed)
When B's HTTP request succeeds and `mmria_reconcile_live_save_state_after_success` runs for B
Then **no `console.warn`** is emitted
And `g_change_stack` is not modified (nothing to remove — it is already empty)

**AC-2 — No warning when items are a strict already-committed subset**

Given `g_change_stack` is `[item4, item5]` (new edits added after A's reconcile)
And B's snapshot is `[item1, item2, item3]` (items that A already removed)
When B reconciles
Then **no `console.warn`** is emitted
And `g_change_stack` remains `[item4, item5]` unchanged (item4/item5 are new work not yet saved)

**AC-3 — Warning IS emitted for a genuine mismatch**

Given `g_change_stack` is `[item4, item5]`
And a save's snapshot is `[item4, item5]` — the items match exactly
When that save reconciles
Then the warning must NOT fire — `g_change_stack` is spliced to `[]` (normal happy path)

Given `g_change_stack` is `[item4, item5, item6]`
And a save's snapshot is `[itemX, item5]` — position 0 does not match
When that save reconciles
Then `console.warn('Save completed, but live change stack no longer matched the queued snapshot. Leaving unmatched edits intact.')` IS emitted
And `g_change_stack` is left unchanged (genuine mismatch — warn and leave intact)

**AC-4 — Empty snapshot is always a clean reconcile**

Given a save's `change_stack_items` snapshot is `[]` (no change stack items at enqueue time)
When that save reconciles
Then no warn is emitted and no splice is attempted regardless of `g_change_stack` contents

**AC-5 — Behaviour is unchanged at both inactivity thresholds**

Given the site configuration has `case_edit_inactivity_warning_minutes_before_lock: 110` and `case_edit_inactivity_lock_minutes: 120`
When the race occurs via the "Continue Editing" path (10-minute warning threshold)
Or when the race occurs via the lock-release path (120-minute threshold)
Then AC-1 and AC-2 hold in both cases — the fix is configuration-agnostic

---

## Background — The Race Condition

When a case is left idle during a transient network disruption (VPN change, WiFi handoff — evidenced by ERR_NETWORK_CHANGED / ERR_CONNECTION_RESET in browser console):

1. **Autosave A** is sent — `save_queue.active_item = A`. The HTTP request stalls.
2. While A's request is **in-flight** (not during backoff — `active_item` is set), the inactivity timer fires. An awaited save **B** is enqueued via `save_case_and_wait(...)`. Because A is the active item, the prune guard protects it. Queue = `[A (active, in-flight), B (awaited)]`.
3. A and B both snapshot `g_change_stack` at their respective enqueue times. Since no new edits were made during the idle period, **both snapshots are identical**.
4. Network recovers. A's request succeeds. A reconciles: prefix match passes → `g_change_stack` spliced to `[]`.
5. `mmria_rebase_queued_items_to_new_rev` updates B's `data._rev` to the new revision.
6. B processes and succeeds. B reconciles: `g_change_stack` is now `[]` (shorter than B's snapshot of 3 items) → `mmria_is_change_stack_prefix_match` returns `false` → **false-positive warning fires**.

**No data is lost.** The warning is the entire problem.

---

## Dev Notes — Implementation

### File to modify

```
source-code/mmria/mmria-server/wwwroot/scripts/case/index.js
```

### Current code (find by searching for the warning string)

```javascript
function mmria_reconcile_live_save_state_after_success(p_item)
{
  if(!p_item || !p_item.data || !g_data || g_data._id !== p_item.data._id)
  {
    return;
  }

  const snapshot_items = Array.isArray(p_item.change_stack_items)
    ? p_item.change_stack_items
    : [];

  if(snapshot_items.length > 0)
  {
    if(mmria_is_change_stack_prefix_match(g_change_stack, snapshot_items))
    {
      g_change_stack.splice(0, snapshot_items.length);
    }
    else
    {
      console.warn(
        'Save completed, but live change stack no longer matched the queued snapshot. Leaving unmatched edits intact.'
      );
    }
  }

  mmria_reconcile_live_narrative_state_after_success(p_item);
}
```

### Required change

The `else` branch (the warning) must distinguish two cases:

- **Already-committed**: `g_change_stack.length < snapshot_items.length` — the snapshot items are gone because a prior save already removed them. Treat as a clean reconcile — no warn.
- **Genuine mismatch**: `g_change_stack.length >= snapshot_items.length` but `mmria_is_change_stack_prefix_match` returned false — items are present but at unexpected positions or with unexpected content. The warning IS appropriate here.

**Replacement logic:**

```javascript
  if(snapshot_items.length > 0)
  {
    if(mmria_is_change_stack_prefix_match(g_change_stack, snapshot_items))
    {
      // Normal path: snapshot items are still at the head of the live stack — remove them.
      g_change_stack.splice(0, snapshot_items.length);
    }
    else if(g_change_stack.length >= snapshot_items.length)
    {
      // Genuine mismatch: the live stack has enough items but they don't match
      // the snapshot prefix. Items were modified or reordered between enqueue
      // and completion. Leave intact and warn.
      console.warn(
        'Save completed, but live change stack no longer matched the queued snapshot. Leaving unmatched edits intact.'
      );
    }
    // else: live stack is shorter than the snapshot — a prior save already committed
    // those items. Treat as clean; no warn, no splice needed.
  }
```

### Why this is safe

- The `mmria_is_change_stack_prefix_match` check is unchanged — it still detects genuine mismatches correctly.
- The new `else if` condition (`g_change_stack.length >= snapshot_items.length`) is the only addition.
- The implicit `else` (do nothing, no warn) covers the already-committed case.
- Empty snapshots (`snapshot_items.length === 0`) continue to be no-ops via the outer `if`.

### Verification

After applying the fix, reproduce the race manually:

1. Open a case in edit mode. Make a field change (creates a change stack entry).
2. Open DevTools → Network. Throttle to "Offline" to block saves.
3. Wait for autosave to fire and fail.
4. Switch network back to "Online" (A retries and will eventually succeed).
5. Before A succeeds, wait for the inactivity warning modal to appear (10 minutes idle — or lower `case_edit_inactivity_warning_minutes_before_lock` in the config temporarily for testing).
6. Dismiss the inactivity warning ("Continue Editing") — this enqueues save B.
7. Observe: both saves complete. **No `console.warn`** for "unmatched edits" should appear.
8. Confirm the case saved correctly (check CouchDB `_rev` advanced twice).

---

## Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Add `else if (g_change_stack.length >= snapshot_items.length)` guard around the existing `console.warn` in `mmria_reconcile_live_save_state_after_success` |

---

## Story Sequencing

| Dependency | Risk |
|---|---|
| None — isolated single-function change | Low |

---

## Dev Agent Record

**Completion Date:** 2026-08-03
**Completed by:** Amelia (bmad-agent-dev)

### Completion Notes

Implemented the single-line logic change in `mmria_reconcile_live_save_state_after_success`. The `else` branch that unconditionally emitted `console.warn` was replaced with an `else if (g_change_stack.length >= snapshot_items.length)` guard. This means:
- When `g_change_stack` is shorter than the snapshot (prior save already committed those items), the implicit `else` now silently does nothing — no warn, no splice. AC-1 and AC-2 satisfied.
- When `g_change_stack` has enough items but they don't prefix-match the snapshot, the warn still fires. AC-3 satisfied.
- Empty snapshots remain no-ops via the outer `if`. AC-4 satisfied.
- The fix is purely conditional logic; `mmria_is_change_stack_prefix_match` is unchanged.

### File List

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Replaced unconditional `else { console.warn(...) }` with `else if (g_change_stack.length >= snapshot_items.length) { console.warn(...) }` in `mmria_reconcile_live_save_state_after_success` |

### Change Log

- **index.js line ~818** — Added `else if(g_change_stack.length >= snapshot_items.length)` condition before the `console.warn`, turning the unconditional warn into a guarded warn. Added explanatory comments for all three branches.
