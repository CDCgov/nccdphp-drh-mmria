# Case Summary Rendering Context

- Status: Active
- Scope: Current case-summary rendering flow, `p_post_html_render` usage, hashchange behavior, and related data-loading notes for the case editor.
- When to use: Read this before changing `wwwroot/scripts/case/index.js`, `page_renderer.js`, or `editor/page_renderer/app.mmria.js`.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Offline Mode Documentation](./offline_mode.md), [Historical Case Summary Rendering Note](./local/archive/case_summary_rendering_history_feb_2026.md)

## What this doc covers

This document records the current behavior of the case summary and case-detail rendering flow in the case editor. Older investigation notes described a more specific render-cycle rule around deferred flag clearing; the current code does not fully match that older narrative, so this file favors the code as it exists today.

## Current render pipeline

### Core entry points

- [case/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js) is the main orchestrator.
- [page_renderer.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer.js) routes rendering by metadata type.
- [app.mmria.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js) renders the `app` view used for the case summary screen.

### High-level flow

1. `load_and_set_data()` in `case/index.js` sets `window.onhashchange = window_on_hash_change` and then calls `await get_case_set()`.
2. `get_case_set()` loads the case-view payload and related supporting data.
3. `page_render(...)` builds HTML into an array and collects post-render callbacks in `post_html_call_back`.
4. `case/index.js` writes the joined HTML into `form_content_id`.
5. `case/index.js` then executes `eval(post_html_call_back.join('\n'))` when callbacks are present.

## `p_post_html_render` semantics

`p_post_html_render` is still an important pattern in the editor renderer:

- It is used for event binding after fresh DOM is inserted.
- It is used for follow-up work that depends on rendered elements already existing.
- It is not the only place where state mutations happen today.

Examples in the current code:

- Search textbox bindings are added through `p_post_html_render.push(...)` in [app.mmria.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js).
- `case/index.js` evaluates the accumulated callback strings after writing the new HTML to the page.

## Current go-offline error flag behavior

The current code does **not** defer every flag mutation through `p_post_html_render`.

In particular, `app.mmria.js` currently renders the "Cases could not be brought offline" message and then immediately clears:

```javascript
localStorage.setItem('is_go_offline_error', 'false');
```

That means:

- the historical claim that this flag is always cleared in a post-render callback is no longer accurate for the code as of 2026-03-24
- any future changes in this area should be validated against the actual runtime behavior, not assumed from older notes

If you are changing the offline error-message lifecycle, compare current code in:

- [app.mmria.js](../../source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js)
- [case/index.js](../../source-code/mmria/mmria-server/wwwroot/scripts/case/index.js)
- [offline-transition-manager.js](../../source-code/mmria/mmria-server/wwwroot/scripts/offline/offline-transition-manager.js)

## Pinned cases and offline session fetch behavior

The current `get_case_set()` flow includes two important data-loading details:

- The case view response now carries `pinned_case_set`, and `case/index.js` reads it directly instead of issuing a second pinned-cases request.
- When offline mode is enabled and the client is not already in an invalid offline state, `get_case_set()` also starts `/api/OfflineCase/active-user-session` in parallel with `/api/case_view`.

This means changes to case-summary load behavior need to consider:

- the combined case-view response contract
- the parallel `Promise.all(...)` wait in `get_case_set()`
- the possibility that offline mode is enabled but active-session fetch is intentionally skipped

## Hashchange behavior

Current behavior:

- `load_and_set_data()` assigns `window.onhashchange = window_on_hash_change`.
- The initial page load then calls `await get_case_set()` directly.
- The case page no longer performs the same explicit manual hashchange trigger that older investigation notes focused on.

Practical guidance:

- Do not assume the older "manual hashchange trigger after initial load" bug narrative still matches the current code.
- If you need to change navigation or rerender behavior, trace both `get_case_set()` and `window_on_hash_change(...)` in the current file before making assumptions.

## Safe editing checklist

- Verify whether the change belongs in `case/index.js`, `page_renderer.js`, or `app.mmria.js`.
- If you are adding DOM-dependent follow-up work, prefer `p_post_html_render`.
- If you are changing localStorage or other state flags, confirm whether the flag is currently mutated inline or deferred.
- Re-test summary load, case navigation, offline-mode entry points, and any message lifecycle tied to hash navigation.

## Historical note

An earlier investigation write-up about deferred flag clearing and manual hashchange timing is preserved in [local/archive/case_summary_rendering_history_feb_2026.md](./local/archive/case_summary_rendering_history_feb_2026.md). Use it as historical context only.


