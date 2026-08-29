---
baseline_commit: b09f5595031a00045ddb3413d9f779af60ff746a
---

# Story 9.3 — Fix Manage Users Role Filter False-Positive Match

**Epic:** Standalone Bug Fixes
**Story ID:** 9.3
**Status:** done
**Date added:** 2026-07-10

---

## User Story

As an installation admin managing users on the Manage Users page,
When I filter the user list by a specific role (e.g., "Steve PRAMS"),
So that only users who actually hold that role appear — not unrelated users whose username happens to be a trailing substring of another user's `user_id`.

---

## Background — Investigation

Root-caused via forensic investigation; full case file at
[`_bmad-output/implementation-artifacts/investigations/manage-users-role-filter-false-positive-investigation.md`](investigations/manage-users-role-filter-false-positive-investigation.md).

Confirmed repro: filtering by role `steve_prams` on a tenant containing both `user7` (holds only `vital_importer` /
`abstractor` roles) and `offline-test-user7` (holds an active `steve_prams` role) surfaces **both** users, because
`"offline-test-user7".endsWith("user7")` evaluates `true`.

---

## Acceptance Criteria

**AC-1 — Role filter matches only users who actually hold the selected role**
Given a role filter is selected (e.g., "Steve PRAMS")
And a `user_role_jurisdiction` record exists for a different user whose `user_id` ends with another user's username (e.g., `offline-test-user7` holds `steve_prams`, and a distinct user `user7` does not)
When the admin filters by that role
Then only users whose own `user_id` **exactly** matches a `user_role_jurisdiction.user_id` with that `role_name` appear in the filtered table
And `user7` (or any similarly-suffixed unrelated username) does not appear unless they hold the role themselves

**AC-2 — Role filter continues to ignore active/inactive status (existing, expected behavior — do not change)**
Given a user holds the selected role via a `user_role_jurisdiction` record with `is_active: false`
And that record has not passed its `effective_end_date` (i.e., it is still present in `g_jurisdiction_list`)
When the admin filters by that role
Then the user still appears in the filtered results, exactly as today
And this story introduces no new `is_active` check — the fix is scoped strictly to the `user_id` comparison

**AC-3 — Role filter still matches legitimate exact-match users (no regression)**
Given a role filter is selected
And one or more users genuinely hold that role (`user_role_jurisdiction.user_id === user.name`)
When the admin filters by that role
Then all such users continue to appear in the filtered results, identical to current behavior for true positives

**AC-4 — Username filter is untouched and unaffected**
Given the admin types a substring into the "Filter by username" input (`filter_by_username()`)
When the filtered table renders
Then matching is still performed via `.toLowerCase().includes()` substring matching, exactly as before
And this story makes no changes to `filter_by_username()`, `clear_filter()`, or any other function besides `filter_by_role()`

**AC-5 — Clear filter and re-filter still behave as before**
Given a role filter has been applied and then "Clear filter" is clicked
When the admin subsequently applies a new role filter
Then the filtered list is rebuilt correctly using the corrected exact-match logic, with no stale state from the previous filter

---

## Dev Notes — Root Cause and Fix

### Root Cause

`filter_by_role()` in `summary_renderer.js` (~line 107) matches jurisdiction records to users using `.endsWith()`
instead of exact equality:

```javascript
// current (buggy):
const filter_jurisdiction = g_jurisdiction_list.filter((jurisdiction) => {
  return (
    jurisdiction.role_name === selectedValue &&
    jurisdiction.user_id !== "" &&
    jurisdiction.user_id !== null
  );
});
g_filtered_user_list = g_ui.user_summary_list.filter((user) => {
  return filter_jurisdiction.some(
    (jurisdiction) =>
      jurisdiction.user_id.endsWith(user.name) ||
      jurisdiction.user_id === user.name,
  );
});
```

Because `.endsWith()` is a superset of exact match, any `user.name` that is a trailing substring of an unrelated
`jurisdiction.user_id` (e.g. `user7` vs. `offline-test-user7`) causes a false-positive match, regardless of whether
the matching role record is active or inactive — `is_active` is not part of the predicate at all today.

Elsewhere in the same feature, the equivalent user↔role association is done via exact equality only — see
`index.js:830` and `index.js:1258` (`g_ui.user_summary_list[i].name == user_role.user_id`). The `.endsWith()` branch
in `filter_by_role()` is the outlier and has no known reason to exist; it is not required by any documented
requirement.

### Fix — drop the `.endsWith()` clause

**File:** `source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js`
**Function:** `filter_by_role()` (~line 119)

```javascript
// BEFORE (buggy):
g_filtered_user_list = g_ui.user_summary_list.filter((user) => {
  return filter_jurisdiction.some(
    (jurisdiction) =>
      jurisdiction.user_id.endsWith(user.name) ||
      jurisdiction.user_id === user.name,
  );
});

// AFTER (fix):
g_filtered_user_list = g_ui.user_summary_list.filter((user) => {
  return filter_jurisdiction.some(
    (jurisdiction) => jurisdiction.user_id === user.name,
  );
});
```

Do **not** add an `is_active` check as part of this fix — AC-2 requires the existing active/inactive-agnostic
behavior to be preserved exactly. The scope of this fix is the equality operator only.

### Why this is safe

- `filter_jurisdiction` (the `role_name` + non-empty `user_id` predicate) is unchanged — only the final `user_id`
  comparison against `user.name` changes from `.endsWith() || ===` to `===` alone.
- `filter_by_username()`, `clear_filter()`, `role_filter_options_renderer()`, and all pagination/sort functions are
  untouched — they do not reference the `.endsWith()` logic and share no state that this change affects.
- No server-side changes. No changes to `GetInitialData`, the `user_role_jurisdiction` data shape, or any API
  contract.
- Purely a narrowing of a client-side filter predicate — it can only remove false positives, never remove a true
  positive, since `user_id === user.name` was already one of the two `||` branches.

---

## Tasks

- [x] Apply the fix in `summary_renderer.js`
  - [x] File: `source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js`
  - [x] Function: `filter_by_role()` (~line 119)
  - [x] Change: `jurisdiction.user_id.endsWith(user.name) || jurisdiction.user_id === user.name` → `jurisdiction.user_id === user.name`
- [x] Manual smoke test (AC-1 through AC-3)
  - [x] Confirm a username that is a suffix of another user's `user_id` no longer appears under the other user's role filter
  - [x] Confirm a user with an inactive-but-not-expired role assignment still appears under that role filter (AC-2)
  - [x] Confirm users who genuinely hold the filtered role still appear (AC-3)
- [x] Regression check — username filter untouched (AC-4)
  - [x] Confirm `filter_by_username()` substring matching behaves identically before/after the change
- [x] Regression check — clear filter / re-filter sequencing (AC-5)
- [x] Add/extend Playwright regression coverage under `nccdphp-drh-mmria-utilities/e2e/tests/manage-users/` for AC-1 through AC-5, following the pattern established in `manage-users-export-filter.spec.ts`

### Review Findings

- [x] [Review][Decision] No deterministic fixture reproduces the exact reported suffix-collision scenario — AC-1/AC-3 only assert "every visible row genuinely holds the filtered role" against whatever data the current tenant happens to have. If no username pair with a suffix relationship (e.g. `user7` / `offline-test-user7`) exists in the test tenant, a future reintroduction of `.endsWith()` would not be caught by CI. Options: (a) provision a dedicated fixture pair via the existing `manage-users-role-creation.spec.ts` harness so the exact bug scenario is deterministically pinned, or (b) accept the generic per-row assertion as sufficient regression coverage (it does catch the bug in _this_ tenant's current data, which already contains a real collision — `user7`/`offline-test-user7`). **Resolved 2026-07-10 by Nick: option (b) — current coverage accepted as sufficient; no dedicated fixture provisioning.**
- [x] [Review][Patch] `roleResultsSelector()` escapes quotes but not backslashes, and in the wrong order — a username containing `\` can still produce a malformed CSS attribute selector [manage-users-role-filter.spec.ts:roleResultsSelector()] — fixed, backslash now escaped before quote
- [x] [Review][Patch] `rolesToCheck = Math.min(optionCount - 1, 5)` arbitrarily caps AC-1/AC-3 to the first 5 role options — any suffix-collision bug affecting role #6+ would go untested [manage-users-role-filter.spec.ts:AC-1/AC-3 test] — fixed, cap removed, all role options now checked
- [x] [Review][Patch] AC-5 asserts filtered-count equality only, not row identity — a defect that swaps one matching user for a different (wrong) one at the same count would pass undetected [manage-users-role-filter.spec.ts:AC-5 test] — fixed, now also asserts the sorted username set matches before/after
- [x] [Review][Patch] AC-5's `firstRoleValue` from `getAttribute("value")` is force-unwrapped (`firstRoleValue!`) with no null guard before `selectOption()` [manage-users-role-filter.spec.ts:AC-5 test] — fixed, added `test.skip` guard
- [x] [Review][Defer] Regression coverage lives in a different repo (`nccdphp-drh-mmria-utilities`) than the fix (`nccdphp-drh-mmria`) [manage-users-role-filter.spec.ts] — deferred, pre-existing repo split, matches Story 9.2 precedent
- [x] [Review][Defer] Liberal `test.skip()` usage risks a false-green CI run if fixture data doesn't exercise a given scenario [manage-users-role-filter.spec.ts] — deferred, matches the skip pattern already established and accepted in Story 9.2's spec
- [x] [Review][Defer] Fixed `page.waitForTimeout(200/300)` sleeps instead of state-based waits [manage-users-role-filter.spec.ts] — deferred, copied from the accepted Story 9.2 pattern
- [x] [Review][Defer] `formatRoleLabel()` / `getFilteredUserCount()` reimplement production display logic instead of importing it, risking silent drift [manage-users-role-filter.spec.ts] — deferred, architectural constraint (plain `<script>` tags, no module system to share code between app and Playwright tests); same pattern as Story 9.2
- [x] [Review][Defer] `getFilteredUserCount()` regex-scans every `<div>` on the page for pagination text rather than a stable anchored element [manage-users-role-filter.spec.ts] — deferred, copied verbatim from the accepted Story 9.2 helper
- [x] [Review][Defer] `beforeEach` throws a raw `Error` on non-200 auth check and doesn't catch a network-level throw from `page.request.get` [manage-users-role-filter.spec.ts] — deferred, copied verbatim from Story 9.2's spec
- [x] [Review][Defer] Leftover `console.log("Filtered Users:", g_filtered_user_list)` logs user data to the browser console inside `filter_by_role()` [summary_renderer.js:122] — deferred, pre-existing line untouched by this diff, opportunistic cleanup candidate

---

## Dev Agent Record

**Implemented by:** Amelia (💻 bmad-agent-dev)
**Date:** 2026-07-10

### Implementation Notes

- Applied the one-token fix in `filter_by_role()`: removed the `jurisdiction.user_id.endsWith(user.name) ||` clause, keeping `jurisdiction.user_id === user.name` as the sole match predicate. Identical character-of-change discipline to Story 9.2.
- **AC-1/AC-2/AC-3 verification:** No JS unit-test framework exists in this repo for `wwwroot` client scripts (Playwright E2E is the only test layer for this code). Since `filter_by_role()`'s matching logic is a pure array filter with no DOM dependency, I extracted the before/after predicate into a standalone Node script and ran it against data shapes drawn directly from the reported diagnostic payload (`temp.json`): `user7` (no `steve_prams` role), `offline-test-user7` (active `steve_prams`), `508reviewer` (active `steve_prams`), and an added `user9` fixture with an **inactive** `steve_prams` role to explicitly exercise AC-2. All 5 assertions passed: `user7` excluded (AC-1), both active true positives retained (AC-3), the inactive-role user retained (AC-2), and the buggy logic was confirmed to reproduce the original report as a sanity check. This is a logic-level verification, not a rendered-DOM verification.
- **AC-4/AC-5 verification:** Confirmed by code inspection that `filter_by_username()`, `clear_filter()`, `role_filter_options_renderer()`, and all pagination/sort functions are untouched — the diff is scoped to a single line inside `filter_by_role()`. `clear_filter()` and `filter_by_role()` both unconditionally rebuild `g_filtered_user_list` from `g_ui.user_summary_list`/`g_jurisdiction_list` on each call, so no stale state carries across filter → clear → filter sequences.
- **Environment note:** Initially checked `https://tenant1-mmria.local:12345` (the Playwright config default for multi-tenant mode), which was unreachable. The user confirmed the actual running instance is single-tenant at `http://localhost:12345` — this matches `e2e/.env`'s `MMRIA_BASE_URL`, which I had not checked before concluding no server was available. Once pointed at the correct URL, the new spec ran live end-to-end.
- **Live Playwright run (`npx playwright test manage-users-role-filter`):** first run caught a genuine bug in the _test_, not the fix — AC-1's assertion compared the rendered Role(s) cell text against the raw `role_name` value (e.g. `abstractor`) instead of the app's own display-formatted label (e.g. `Abstractor`, `Steve PRAMS`), causing a false failure on a true-positive row (`auto10` / Abstractor). Added a `formatRoleLabel()` helper mirroring the exact transform used by `role_filter_options_renderer()`/`user_entry_render()` (split on `_`, uppercase `steve`/`mmria`/`prams`/`cdc`, title-case the rest, join with spaces) and compared against that instead. Re-ran: all 5 tests (setup + AC-1/AC-3, AC-2, AC-4, AC-5) passed against live data.
- **Regression check:** also ran the existing Story 9.2 spec (`manage-users-export-filter.spec.ts`) against the same server — all 4 ACs still pass, confirming the `filter_by_role()` change has no effect on the export-filter behavior that shares `g_filtered_user_list`.
- New spec follows the Story 9.2 pattern (`openManageUsers`, `getFilteredUserCount` helpers) but asserts generically against the rendered `#role_results_*` cell for every visible row, rather than hardcoding specific usernames — this makes the regression guard (AC-1) effective for any tenant's real user data, not just the specific `user7`/`offline-test-user7` pair from the original report.

### File List

| File                                                                                  | Change                                                                           |
| ------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js`     | Bug fix — removed `.endsWith()` clause from `filter_by_role()`, exact match only |
| `nccdphp-drh-mmria-utilities/e2e/tests/manage-users/manage-users-role-filter.spec.ts` | New — Playwright regression spec covering AC-1 through AC-5                      |

### Change Log

| Date       | Author | Change                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ---------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-07-10 | Amelia | Initial implementation — one-line fix, Node-script logic verification of AC-1/2/3, new Playwright regression spec for AC-1 through AC-5                                                                                                                                                                                                                                                                                                            |
| 2026-07-10 | Amelia | Ran new spec live against `http://localhost:12345` (single-tenant); fixed a test-assertion bug (raw vs. formatted role label); all 5 tests pass. Re-ran Story 9.2 export-filter spec — no regression                                                                                                                                                                                                                                               |
| 2026-07-10 | Amelia | Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor): 1 decision-needed resolved (accepted current coverage, no dedicated fixture), 4 test-file patches applied (selector escaping, removed arbitrary role cap, AC-5 row-identity check, null guard), 7 deferred (pre-existing/precedent), 4 dismissed as noise (incl. git-history-refuted claim re: `.endsWith()` origin). Re-ran spec live — all 5 tests still pass. Status → done |
