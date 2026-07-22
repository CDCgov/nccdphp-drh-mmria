---
title: 'Fix View Data Summary multi-field filter not excluding unselected fields'
type: 'bugfix'
created: '2026-07-22'
status: 'done'
route: 'one-shot'
context: []
---

# Fix View Data Summary multi-field filter not excluding unselected fields

## Intent

**Problem:** On `/view-data-summary`, unchecking "All Fields" and selecting 2 or more specific fields had no filtering effect — the results table still rendered every field, because the exclusion guard in `render_search_result_item` only fired when exactly one field was selected.

**Approach:** Generalize the exclusion guard in `renderer.js` to exclude any field not present in `g_filter.field_selection` whenever `"all"` isn't selected, regardless of how many specific fields are checked, while preserving the existing single-field header behavior.

## Suggested Review Order

**Filter logic fix**

- Entry point — the exclusion guard now checks membership at any selection size, not just size 1.
  [`renderer.js:662`](../../source-code/mmria/mmria-server/wwwroot/scripts/view-data-summary/renderer.js#L662)

**Investigation record**

- Root-cause trace and evidence backing this fix.
  [`view-data-summary-multi-field-filter-investigation.md`](investigations/view-data-summary-multi-field-filter-investigation.md)

**Deferred follow-ups**

- No automated regression test could be authored/verified in this session (requires a running app + CouchDB); tracked for follow-up.
  [`deferred-work.md`](deferred-work.md)
