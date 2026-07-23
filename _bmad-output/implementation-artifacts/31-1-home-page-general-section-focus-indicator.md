# Story 31.1: Add `:focus-visible` Outline to Home Page General Section Buttons

Status: ready

## Story

As a keyboard-only user navigating the MMRIA Home page,
I want to see a clear visual indicator when either General section download button has keyboard focus,
so that I can tell which element is active and navigate the page with confidence.

## Context

A Section 508 accessibility review identified two buttons in the Home page General section that lack any visible keyboard focus indicator. Both elements are rendered as `<button class="btn btn-link p-0 offline-disable">` in `Views/Home/Index.cshtml`.

**Root cause:** Bootstrap's `.btn-link:focus` rule in `index.css` sets `box-shadow: none`, which cancels the `.btn:focus` box-shadow indicator. The only remaining focus style is `text-decoration: underline` — but both buttons already carry `text-decoration: underline` unconditionally via their inline `style` attribute. The result is **zero visible change** when either button receives keyboard focus.

**Affected elements:**

| Element ID | Visible text |
|---|---|
| `#view-informant-interview-summary-template-button` | View/Download Informant Interview Summary Template |
| `#view-cdf-template-button` | View/Download MMRIA Committee Decisions Form (CDF) Template PDF |

**Fix:** Add a `:focus-visible` CSS rule in `index.scss` targeting both element IDs. No server-side, controller, or JavaScript changes are required.

## Acceptance Criteria

1. When the user presses Tab to move keyboard focus to `#view-informant-interview-summary-template-button`, a clearly visible, high-contrast outline (≥ 3 px solid, sufficient contrast against the white card background) appears around the button.
2. When the user presses Tab to move keyboard focus to `#view-cdf-template-button`, the same outline appears.
3. When either button is reached via mouse click (not keyboard Tab), no new outline appears — the `:focus-visible` pseudo-class must not fire for pointer-initiated focus.
4. The fix is implemented exclusively by adding a `:focus-visible` rule to `index.scss`. No changes to `Views/Home/Index.cshtml`, any controller, or any JavaScript file.
5. Tabbing through the General section in both Microsoft Edge and Google Chrome shows a visible outline on each button when focused.

## Tasks / Subtasks

- [ ] Add `:focus-visible` rule to `index.scss` (AC: #1, #2, #3, #4)
  - [ ] File: `source-code/mmria/mmria-server/wwwroot/css/index.scss`
  - [ ] Locate the existing focus rules section (near the `.info-icon:focus` rule, ~line 1477)
  - [ ] Add the following rule block after the `.info-icon:focus` rule:
    ```scss
    #view-informant-interview-summary-template-button:focus-visible,
    #view-cdf-template-button:focus-visible {
      outline: 3px solid #0056b3;
      outline-offset: 3px;
    }
    ```
  - [ ] The compiled `index.css` must reflect the new rule (if a build step compiles SCSS to CSS, run it; if `index.css` is edited directly, apply the same rule there)
- [ ] Verify no regression to mouse/pointer users (AC: #3)
  - [ ] Click each button with a mouse — confirm no outline appears
  - [ ] `:focus-visible` is supported in all in-scope browsers (Edge and Chrome — NFR-1)
- [ ] Manual keyboard verification in Edge and Chrome (AC: #1, #2, #5)
  - [ ] Tab to `#view-informant-interview-summary-template-button` — visible outline appears ✓
  - [ ] Tab to `#view-cdf-template-button` — visible outline appears ✓
  - [ ] Shift+Tab back — outline follows focus correctly ✓

## Dev Notes

**Why `:focus-visible` and not `:focus`?**
Using `:focus` would show the outline on mouse clicks as well, which is not the accessibility intent (and was intentionally suppressed by Bootstrap for pointer users). `:focus-visible` fires only for keyboard-initiated focus in all in-scope browsers (Edge and Chrome both support it).

**Color choice:** `#0056b3` is Bootstrap's `$link-hover-color` (dark blue). It provides > 3:1 contrast against the white `#ffffff` card background, satisfying WCAG 2.1 SC 1.4.11 (Non-text Contrast) for enhanced focus indicators.

**SCSS vs CSS:** `index.scss` is the source file. Check whether the project has a SCSS compile step. If `index.css` is served directly from the compiled output, both files need the rule. If `index.css` is checked in as the compiled artifact, update both. If only `index.scss` is the source-of-truth and CSS is generated at build time, update `index.scss` only.

**Scope confirmation:** No changes to `Views/Home/Index.cshtml`, `HomeController.cs`, or any `.js` file are needed or permitted for this story.
