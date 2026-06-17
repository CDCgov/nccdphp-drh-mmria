# Story 4.3: OMB Block Right-Alignment — Home Page

Status: not-started

## Story

As a MMRIA user viewing the Home page,
I want the OMB block to appear right-aligned on the page,
so that the layout matches the intended design with the heading on the left and the OMB block on the right.

## Acceptance Criteria

1. On the Home page, the OMB block (`_BurdenStatement`) is visually right-aligned to the page — it appears on the right side of the header row, opposite the "MMRIA Home" heading.
2. Text content within the OMB block remains left-aligned (default browser / Bootstrap alignment — no `text-right` class is applied to the block or its container).
3. The PMSS variant of the Home page is not affected — the OMB block is not rendered in the PMSS path and that path is unchanged.
4. The Committee Decisions form is not modified in any way.
5. No content, behavior, or configuration changes are made — this is a layout-only change.
6. The change is limited to a single file: `Views/Home/Index.cshtml`.

## Tasks / Subtasks

- [ ] Move `_BurdenStatement` partial out of the heading `<div>` into a sibling `<div>` in `Views/Home/Index.cshtml` (AC: #1–#6)
  - [ ] Remove `@await Html.PartialAsync("_BurdenStatement")` from inside the `else` block of the `is_pmss_enhanced` conditional (currently appears after the `<h1>` tag)
  - [ ] Add a new sibling `<div>` inside the outer `justify-content-between` row, after the heading `<div>` and before the `#offline-home-exit-widget` div, that renders `_BurdenStatement` only when `!is_pmss_enhanced`
  - [ ] Verify the outer row already has `justify-content-between` — no class changes to the row div are needed

## Dev Notes

### Why this works — no CSS change required

The outer row already uses Bootstrap's `justify-content-between` flex utility:

```html
<div class="row no-gutters justify-content-between align-items-start mb-3">
```

`justify-content-between` distributes direct child flex items so that the first child aligns to the start (left) and the last child aligns to the end (right). Currently the `_BurdenStatement` partial is nested *inside* the heading `<div>` (the first child), so it follows the heading. Moving it to its own sibling `<div>` makes it a second direct child of the flex row — `justify-content-between` then automatically positions it to the right.

No CSS, no inline `style`, no `text-right` or `ml-auto` class is needed.

### Primary file
- `source-code/mmria/mmria-server/Views/Home/Index.cshtml`

### No other files touched
- `Views/Shared/_BurdenStatement.cshtml` — not modified
- Committee Decisions form files — not modified
- No C# controller changes
- No CSS changes
- No database document changes
- No JS changes
- No build step required (Razor views are compiled at runtime)

### Current code (lines ~151–168)

```razor
<div class="row no-gutters justify-content-between align-items-start mb-3">
    <div>
        @if(is_pmss_enhanced)
        {
            <h1 class="h2 d-block" tabindex="-1">PMSS Home</h1>
        }
        else
        {
            <h1 class="h2 d-block" tabindex="-1">MMRIA Home</h1>

             @await Html.PartialAsync("_BurdenStatement")
        }
    </div>
    <div id="offline-home-exit-widget" data-offline-exit-host="home" style="display: none;"></div>
</div>
```

### Target code

```razor
<div class="row no-gutters justify-content-between align-items-start mb-3">
    <div>
        @if(is_pmss_enhanced)
        {
            <h1 class="h2 d-block" tabindex="-1">PMSS Home</h1>
        }
        else
        {
            <h1 class="h2 d-block" tabindex="-1">MMRIA Home</h1>
        }
    </div>
    @if(!is_pmss_enhanced)
    {
        <div>
            @await Html.PartialAsync("_BurdenStatement")
        </div>
    }
    <div id="offline-home-exit-widget" data-offline-exit-host="home" style="display: none;"></div>
</div>
```

### Visual result
- Left side of row: "MMRIA Home" heading
- Right side of row: OMB block (Form Approved / OMB No. / Exp. Date)
- Text inside the OMB block: left-aligned (unchanged — `_BurdenStatement.cshtml` is not modified)
- PMSS path: unchanged (heading only, no OMB block)
