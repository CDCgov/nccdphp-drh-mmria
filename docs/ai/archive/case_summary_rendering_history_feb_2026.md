# Case Summary Rendering History (Feb 2026)

- Status: Historical
- Scope: Earlier investigation notes about deferred flag clearing and manual hashchange timing on the case summary page.
- When to use: Use only when comparing older debugging notes with the current implementation.
- Last verified: 2026-03-24
- Related docs: [Case Summary Rendering Context](../case_summary_rendering_context.md)

> Historical note: This file preserves an earlier explanation of the case summary render cycle. Current code should be treated as authoritative when it differs from this note.

## Summary

An earlier investigation concluded that the case summary page should defer certain state mutations through `p_post_html_render` so the message-rendering pass and follow-up state-clearing pass stayed separate.

That note also focused on a now-removed manual `window.onhashchange(...)` trigger after initial page load.

## What changed since then

As of 2026-03-24:

- the case page still uses `page_render(...)` plus evaluated post-render callback arrays
- the current `app.mmria.js` implementation clears `is_go_offline_error` inline rather than through a deferred callback
- the active guidance now lives in `docs/ai/case_summary_rendering_context.md`

## How to use this note safely

- Use this file to understand the historical debugging context.
- Do not assume every implementation detail here still matches the live code.
- Re-check the current files before making behavior-sensitive changes.


