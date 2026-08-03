# Story 36.4 — Change Stack Snapshot Deduplication on Enqueue (Optional)

**Epic:** 36 — Case Save Queue Reconcile — Idle Network Recovery Fix
**Story ID:** 36.4
**Status:** todo
**Date added:** 2026-08-03
**Depends on:** Story 36.1 (must be complete before scheduling this story)
**Source requirements:** FR-36.2

> ⚠️ **This story is optional.** Story 36.1 fully resolves the observable symptom (false-positive warning). This story adds a defence-in-depth layer that prevents the snapshot overlap from occurring in the first place. Schedule only if the team decides the additional complexity is warranted.

---

## User Story

As a developer,
I want the save queue to avoid capturing overlapping `g_change_stack` snapshots when an active save is already committed to those items,
So that the reconcile false-positive cannot occur even if Story 36.1's reconcile fix is ever removed or regressed.

---

## Acceptance Criteria

**AC-1 — New save's snapshot excludes items already captured in the active save**

Given autosave A is the active item (`save_queue.active_item = A`) with `A.change_stack_items: [item1, item2, item3]`
And A is actively making its HTTP request (in-flight)
When inactivity save B is enqueued via `get_new_save_queue_item`
Then `B.change_stack_items` is `[]` (no items — all existing stack items are already in A's snapshot)

Given autosave A is active with `A.change_stack_items: [item1, item2, item3]`
And the user made a new edit after A was enqueued → `g_change_stack: [item1, item2, item3, item4]`
When B is enqueued
Then `B.change_stack_items` is `[item4]` — only the item not already captured in A

**AC-2 — When no active save exists, full snapshot is taken as normal**

Given `save_queue.active_item` is null (no save in-flight)
And `g_change_stack: [item1, item2, item3]`
When any save is enqueued
Then `B.change_stack_items` is `[item1, item2, item3]` (existing full-snapshot behaviour preserved)

**AC-3 — Active save in backoff (active_item = null) is treated as "no active save"**

Given autosave A has failed and is in its retry backoff window
And `save_queue.active_item` is null (cleared by `finalize_queue_state` after the failure)
When B is enqueued
Then B takes the full `g_change_stack` snapshot (A's stale snapshot is irrelevant — A will be pruned anyway when B is enqueued as a non-awaited save, or B's enqueue prunes A)

**AC-4 — Empty active snapshot produces full snapshot for B**

Given autosave A is active with `A.change_stack_items: []`
When B is enqueued
Then B takes the full `g_change_stack` snapshot (offset of 0 items)

**AC-5 — Reconcile of both saves still produces correct g_change_stack state**

Given A is active with snapshot `[item1, item2, item3]`
And B is enqueued with snapshot `[item4]` (offset per AC-1)
When A succeeds and reconciles → `g_change_stack` goes from `[item1, item2, item3, item4]` to `[item4]`
And B then succeeds and reconciles → B's snapshot `[item4]` matches `g_change_stack[0]` → `g_change_stack` cleared to `[]`
Then no warning fires and `g_change_stack` ends up empty ✓

---

## Dev Notes — Implementation

### File to modify

```
source-code/mmria/mmria-server/wwwroot/scripts/case/index.js
```

### Approach

Modify `get_new_save_queue_item` to compute the snapshot offset before cloning `g_change_stack`.

**Step 1 — Determine offset from active save**

```javascript
function get_new_save_queue_item(p_data, p_call_back, p_note, p_options)
{
  const policy = mmria_get_save_queue_item_policy(p_options);
  const cloned_data = mmria_safe_clone(p_data);

  // ...existing host_state logic...

  // Compute change stack offset: exclude items already captured in the active save
  // so that two saves in sequence don't double-submit the same change stack entries.
  const active_snapshot_length = mmria_get_active_save_snapshot_length(
    p_data && p_data._id
  );
  const change_stack_to_snapshot = Array.isArray(g_change_stack)
    ? g_change_stack.slice(active_snapshot_length)
    : [];

  return {
    id: $mmria.get_new_guid(),
    // ...
    change_stack_items: mmria_safe_clone(change_stack_to_snapshot),
    // ...
  };
}
```

**Step 2 — Add helper to get the active save's snapshot length**

```javascript
function mmria_get_active_save_snapshot_length(p_case_id)
{
  if (!p_case_id) return 0;
  const active = save_queue.active_item;
  if (!active || !active.data || active.data._id !== p_case_id) return 0;
  if (!Array.isArray(active.change_stack_items)) return 0;
  return active.change_stack_items.length;
}
```

### Edge cases to verify

| Scenario | Expected B.change_stack_items |
|---|---|
| A active with 3 items, g_change_stack has same 3 items | `[]` |
| A active with 3 items, g_change_stack has 4 items (new edit) | `[item4]` |
| No active save | full `g_change_stack` |
| A active with 0 items | full `g_change_stack` |
| A active for a different case ID | full `g_change_stack` (offset applies per-case only) |
| A in backoff (active_item = null) | full `g_change_stack` |

### Risk: offset may exceed g_change_stack length

If for any reason `active_snapshot_length > g_change_stack.length` (e.g. items were externally cleared), `slice(active_snapshot_length)` returns `[]`. This is safe — the empty slice produces no snapshot and reconcile will find nothing to remove, which is correct.

### Risk: interaction with mmria_reconcile_live_save_state_after_success

With this story in place, when A (snapshot: `[item1, item2, item3]`) and B (snapshot: `[item4]`) complete in sequence:
- A reconciles: `g_change_stack` goes from `[item1, item2, item3, item4]` → `[item4]`
- B reconciles: prefix match `[item4]` against `[item4]` → TRUE → `g_change_stack` cleared to `[]`
- No warning. Story 36.1's fix is still beneficial as a safety net for any path this story doesn't cover.

---

## Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js` | Add `mmria_get_active_save_snapshot_length` helper; modify `get_new_save_queue_item` to offset the `change_stack_items` snapshot by the active save's captured length |

---

## Story Sequencing

| Dependency | Risk |
|---|---|
| Story 36.1 must be complete (same file; reconcile fix acts as safety net for any edge cases this story misses) | Medium — offset logic must be tested across all enqueue paths including fire-and-forget autosave, navigation saves, and offline saves |
