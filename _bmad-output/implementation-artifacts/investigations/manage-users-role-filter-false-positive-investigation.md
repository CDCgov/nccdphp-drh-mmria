# Investigation: Manage Users role filter shows users who don't hold the filtered role

## Hand-off Brief

1. **What happened.** Filtering the Manage Users grid by role `STEVE PRAMS` (`steve_prams`) incorrectly includes `user7`, because the filter matches jurisdiction records with `.endsWith(username)` instead of an exact match, and `user7` is a substring suffix of an unrelated jurisdiction record's `user_id` (`offline-test-user7`) that does hold `steve_prams`.
2. **Where the case stands.** Root cause Confirmed via direct data trace (`temp.json`) and code trace (`summary_renderer.js`). No further evidence needed.
3. **What's needed next.** Replace the `.endsWith()` substring match with an exact `user_id === user.name` comparison in `filter_by_role()`. Tracked as [Story 9.3](../9-3-fix-manage-users-role-filter-endswith.md).

## Case Info

| Field            | Value                                                                                                                |
| ---------------- | -------------------------------------------------------------------------------------------------------------------- |
| Ticket           | N/A                                                                                                                  |
| Date opened      | 2026-07-10                                                                                                           |
| Status           | Concluded                                                                                                            |
| System           | Manage Users screen, mmria-server (ASP.NET Core + client-side JS), CouchDB-backed jurisdiction data                  |
| Evidence sources | `temp.json` (raw `GetInitialData` payload attached to conversation), source code (`summary_renderer.js`, `index.js`) |

## Problem Statement

Tester filtered Manage Users by role `STEVE PRAMS` and observed `user7` appear in the results, even though `user7` does not hold that role (in either active or inactive state).

## Evidence Inventory

| Source                                | Status    | Notes                                                                                   |
| ------------------------------------- | --------- | --------------------------------------------------------------------------------------- |
| `temp.json` diagnostic payload        | Available | Full `my_roles.rows` (`user_role_jurisdiction` records) matching `GetInitialData` shape |
| `summary_renderer.js` (filter logic)  | Available | Client-side role filter implementation                                                  |
| `index.js` (data load / other usages) | Available | Shows how `g_jurisdiction_list` is populated and how role matching is done elsewhere    |

## Confirmed Findings

### Finding 1: `user7` has no `steve_prams` role record

**Evidence:** [temp.json](../../../temp.json#L6221-L6234) — the only two `user_role_jurisdiction` rows keyed to `user7` are `role_name: "vital_importer"` (`user_id: "user7"`) and `role_name: "abstractor"` (`user_id: "user7@cdc.gov"`). Neither is `steve_prams`.

### Finding 2: `offline-test-user7` does hold an active `steve_prams` role

**Evidence:** [temp.json](../../../temp.json#L2589-L2608) — record `_id: "02ce774c-7146-4133-9492-d725856eb18f"`, `user_id: "offline-test-user7"`, `role_name: "steve_prams"`, `is_active: true`.

### Finding 3: The role filter matches with `.endsWith()`, not exact equality

**Evidence:** [source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js](../../../source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js#L119-L121):

```js
g_filtered_user_list = g_ui.user_summary_list.filter((user) => {
  return filter_jurisdiction.some(
    (jurisdiction) =>
      jurisdiction.user_id.endsWith(user.name) ||
      jurisdiction.user_id === user.name,
  );
});
```

Since `"offline-test-user7".endsWith("user7")` is `true`, the user `user7` is pulled into the filtered list via a jurisdiction record that actually belongs to a completely different user (`offline-test-user7`).

### Finding 4: `is_active` is never checked by the filter

**Evidence:** same block as Finding 3 — `filter_jurisdiction` only checks `role_name === selectedValue` and that `user_id` is non-empty; `is_active` is not part of the predicate. This explains the tester's observation that the false positive occurs "inactive or active" — status has no bearing on the bug.

### Finding 5: Exact-match comparison is the established pattern elsewhere in the same file

**Evidence:** [index.js:830](../../../source-code/mmria/mmria-server/wwwroot/scripts/manage-users/index.js#L830) and [index.js:1258](../../../source-code/mmria/mmria-server/wwwroot/scripts/manage-users/index.js#L1258) both use `g_ui.user_summary_list[i].name == user_role.user_id` (strict equality) to associate a jurisdiction record with a user. `filter_by_role`'s `.endsWith()` branch is the outlier.

## Deduced Conclusions

### Deduction 1: Root cause is a substring/suffix match bug, not a data problem

**Based on:** Findings 1–3, 5.

**Reasoning:** The underlying data is correct (`user7` genuinely has no `steve_prams` role). The comparison logic in `filter_by_role()` treats any jurisdiction `user_id` ending in the target username as a match, which is a superset of exact-match and produces false positives whenever one username is a suffix of another (e.g. `user7` / `test-user7`, `offline-user7`, `cdc-user7`, etc.).

**Conclusion:** Any username that is a suffix of another user's `user_id` will falsely appear in every role filter that the longer-named user holds. This is a general-purpose bug, not specific to `steve_prams` or `user7` — it would reproduce for any similarly-suffixed username pair in the tenant's user list.

## Source Code Trace

| Element       | Detail                                                                                                                                                 |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Error origin  | `filter_by_role()` — [summary_renderer.js:119-121](../../../source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js#L119-L121) |
| Trigger       | Tester selects a role in the `#role_filter` dropdown on the Manage Users screen                                                                        |
| Condition     | Any `user_role_jurisdiction` record whose `user_id` ends with the substring of another user's bare username, and holds the filtered `role_name`        |
| Related files | [index.js](../../../source-code/mmria/mmria-server/wwwroot/scripts/manage-users/index.js) (builds `g_jurisdiction_list` and `g_ui.user_summary_list`)  |

## Conclusion

**Confidence:** High (Confirmed root cause, deterministic repro from data already in hand).

The Manage Users role filter uses `jurisdiction.user_id.endsWith(user.name)` to associate role records with users. `user7`'s username is a trailing substring of the unrelated user `offline-test-user7`, who genuinely holds an active `steve_prams` role — so `user7` is pulled into the filtered results by accident. `is_active` is not checked at all, so this reproduces identically whether the matching role record is active or inactive, matching the tester's report.

## Recommended Next Steps

### Fix direction

In `filter_by_role()` ([summary_renderer.js:119-121](../../../source-code/mmria/mmria-server/wwwroot/scripts/manage-users/summary_renderer.js#L119-L121)), drop the `.endsWith()` clause and match on exact equality only, consistent with the pattern already used in `index.js:830` / `index.js:1258`:

```js
g_filtered_user_list = g_ui.user_summary_list.filter((user) => {
  return filter_jurisdiction.some(
    (jurisdiction) => jurisdiction.user_id === user.name,
  );
});
```

This is a one-line, low-risk change confined to client-side filtering; it doesn't touch stored data or the API contract.

### Diagnostic

Not needed — root cause is Confirmed from data already provided.

## Reproduction Plan

1. Ensure two users exist where one username is a suffix of the other (e.g. `user7` and `offline-test-user7`), and only the longer-named user holds role `steve_prams`.
2. On Manage Users, select "Steve PRAMS" from the role filter dropdown.
3. Observe: both users appear, even though only `offline-test-user7` holds the role.
4. Expected after fix: only `offline-test-user7` appears.

## Side Findings

- The role filter also ignores `is_active` entirely (Finding 4). Even after the exact-match fix, filtering by role will surface users whose role assignment is inactive/expired-by-status (though not past `effective_end_date`, which is already excluded upstream in `index.js`). Worth confirming with the user/PM whether inactive-but-not-expired roles should be excluded from filter results — flagging as a separate, lower-severity issue from the reported bug.
