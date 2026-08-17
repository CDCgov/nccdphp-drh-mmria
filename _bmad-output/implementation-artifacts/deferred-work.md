# Deferred Work

## Deferred from: code review of view-data-summary-multi-field-filter fix (2026-07-22)

- No automated regression coverage added for the field-filter results-table fix (`renderer.js` `render_search_result_item`) — requires a running app + CouchDB instance to author/verify a Playwright test (`../nccdphp-drh-mmria-utilities/e2e/tests/`), which is unavailable in this session. Follow-up: add a test selecting 2+ specific fields and asserting only matching rows appear in `#search_result_list`, alongside the existing `view-data-summary.field-form-filter.spec.ts` coverage from Story 9.1.
- The `"all"` sentinel string used in `g_filter.field_selection` collides with the user-controlled field-name namespace — nothing prevents a real MMRIA field from being named `"all"`. Pre-existing debt, not introduced by this fix.
- `g_filter` is mutable global state read directly inside `render_search_result_item`, making the filtering logic hard to unit-test in isolation without reaching into global setup. Pre-existing architectural pattern across this file, not introduced by this fix.

## Deferred from: code review of 9-3-fix-manage-users-role-filter-endswith (2026-07-10)

- Regression coverage lives in a different repo (`nccdphp-drh-mmria-utilities`) than the fix (`nccdphp-drh-mmria`) — pre-existing repo split, matches Story 9.2 precedent.
- Liberal `test.skip()` usage in `manage-users-role-filter.spec.ts` risks a false-green CI run if fixture data doesn't exercise a given scenario — matches the skip pattern already established and accepted in Story 9.2's spec.
- Fixed `page.waitForTimeout(200/300)` sleeps instead of state-based waits in `manage-users-role-filter.spec.ts` — copied from the accepted Story 9.2 pattern.
- `formatRoleLabel()` / `getFilteredUserCount()` in `manage-users-role-filter.spec.ts` reimplement production display logic instead of importing it, risking silent drift — architectural constraint (plain `<script>` tags, no module system to share code between app and Playwright tests); same pattern as Story 9.2.
- `getFilteredUserCount()` regex-scans every `<div>` on the page for pagination text rather than a stable anchored element — copied verbatim from the accepted Story 9.2 helper.
- `beforeEach` in `manage-users-role-filter.spec.ts` throws a raw `Error` on non-200 auth check and doesn't catch a network-level throw from `page.request.get` — copied verbatim from Story 9.2's spec.
- Leftover `console.log("Filtered Users:", g_filtered_user_list)` logs user data to the browser console inside `filter_by_role()` (`summary_renderer.js:122`) — pre-existing line untouched by the Story 9.3 diff, opportunistic cleanup candidate.
