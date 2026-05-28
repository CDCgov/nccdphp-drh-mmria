# Release 4 Outstanding 508 Plan Split

## Summary

This breaks the current Release 4 internal 508 remediation work into two execution parts so we can keep scope tight and reduce regression risk.

The timeout warning item is intentionally deferred and is **not** included in either part.

## Workbook Status Correlation

Status snapshot as of `2026-04-17`.

| Workbook Row | Issue/Error Location | Issue/Error Description | Planned Part | Current Status | Notes |
| --- | --- | --- | --- | --- | --- |
| `A3` | Case Listing Summary | Elements must meet minimum color contrast ratio thresholds | Part 1 | Implemented | Offline guidance bullet text updated to `#555555` in the offline table renderer. |
| `A4` | Go Offline Modal, Remove from Case Listing modal when Offline, editing conflict/error message | Buttons must have discernible text | Part 1 | Implemented | Close buttons on the in-scope offline and conflict modals now have accessible names. |
| `A5` | Go Offline Modal, Remove from Case Listing modal when Offline, editing conflict/error message | ARIA dialog and alertdialog nodes should have an accessible name | Part 1 | Implemented | In-scope modals now use stable title ids with `aria-labelledby` and `aria-modal="true"`. |
| `A6` | Go Offline Modal | Focus is not maintained within the modal | Part 1 | Implemented | Focus trap behavior was added to the Go Offline flow. |
| `A7` | Go Offline Modal | Screen readers can read parent page content outside the modal | Part 1 | Implemented | Background content is hidden from assistive tech while the modal is open. |
| `A8` | Go Offline Modal | When the modal is activated, focus is not placed on the modal | Part 1 | Implemented | Focus now moves into the Go Offline modal on open. |
| `A9` | App-wide | Timeout warning missing | Deferred | Deferred | Intentionally left out of this remediation batch pending separate planning/discussion. |
| `A10` | Go Offline Modal - Set Key | Elements should not have tabindex greater than zero | Part 1 | Implemented | Positive `tabindex` values were removed from the Set Offline Key inputs. |
| `A11` | Case Listing - Home Record (edit not yet enabled) | Scrollable region must have keyboard access | Part 2 | Implemented | Active `.construct__body` form regions are now keyboard reachable and named with `aria-labelledby`. |
| `A12` | Case Narrative form | Form elements must have labels | Part 2 | Implemented | `#case_narrative_editor` now points to a stable heading via `aria-labelledby`, and the Trumbowyg editor surface inherits the same accessible name. |
| `A13` | Case Listing - Home Record | Page should contain a level-one heading | Part 2 | Needs validation | We preserved and strengthened heading semantics in the form shell, but the fallback route-level `h1` host was intentionally not added pending a real re-test of the Home Record route. |
| `A14` | Go Offline Modal and Remove from Case Listing modal when Offline | Heading levels should only increase by one | Part 1 | Implemented | In-scope modal titles were changed from `h4` to `h2`. |
| `A15` | Go Offline Modal | ESC key does not close modal | Part 1 | Implemented | `Escape` close behavior was added to the dismissible in-scope modals. |
| `A16` | Case Listing Summary, Go Offline Modal, and in Offline mode in the upper right | Alternative text of images should not be repeated as text | Part 1 | Implemented | In-scope offline/go-online/offline-status icons were marked decorative with `alt=""` and `aria-hidden="true"` where visible text already provides the meaning. |

### Status Legend

- `Implemented`: code changes are in place and build-validated
- `Needs validation`: implementation is intentionally partial until a fresh browser/axe check confirms whether more work is still needed
- `Deferred`: intentionally excluded from this batch

## Part 1: Offline Modals And Offline Surface Fixes

### Goal

Resolve the workbook findings tied to:

- Go Offline modal
- Set Offline Key modal
- Remove From List modal while already offline
- offline conflict/error modals on the case page
- offline footer guidance contrast
- redundant alt text on offline/go-online icons

### Scope

#### `wwwroot/scripts/offline/offline-transition-manager.js`

Update:

- `go-offline-modal`
- `set-offline-key-modal`

For both modals:

- add a stable title id
- keep `role="dialog"` and add `aria-modal="true"`
- add `aria-labelledby`
- give the close button `aria-label="Close"`
- change title headings from `h4` to `h2`
- move focus into the modal on open
- trap `Tab` / `Shift+Tab`
- support `Escape`
- restore focus to the invoking control on close
- hide background content from assistive tech while open, then restore it on close

Set Offline Key specifics:

- remove `tabindex="1"` and `tabindex="2"` from the key inputs
- keep natural DOM tab order
- treat the Go Offline button icon as decorative with `alt=""` and `aria-hidden="true"`

#### `wwwroot/scripts/offline/offline-modals.js`

Update only:

- `abandon-case-modal`

For that modal:

- add title id, `aria-modal="true"`, and `aria-labelledby`
- add `aria-label="Close"` to the close button
- change the title to `h2`
- move focus into the modal on open
- trap focus while open
- support `Escape`
- restore focus to the invoking Remove From List button
- hide background content from assistive tech while open, then restore it on close

Do not refactor unrelated offline modals in this pass.

#### `wwwroot/scripts/case/index.js`

Update these conflict/error modals in place:

- `remove-offline-softlock-tab-conflict-modal`
- `edit-lock-tab-conflict-modal`
- `add-offline-softlock-tab-conflict-modal`
- `go-offline-tab-conflict-modal`
- `edit-offline-case-tab-conflict-modal`

For each:

- add title id, `aria-modal="true"`, and `aria-labelledby`
- add `aria-label="Close"` to the close button
- change title heading to `h2`
- move focus into the modal on open
- trap focus while open
- support `Escape`
- restore focus to the invoking control on close
- hide background content from assistive tech while open, then restore it on close

Keep all existing recovery and conflict logic unchanged.

#### `wwwroot/scripts/editor/page_renderer/app.mmria.js`

Update both offline table footers:

- `Offline Case List`
- `Cases Selected for Offline Work`

Changes:

- darken the informational bullet text to `#555555`
- keep existing copy unchanged
- keep current emphasis/bold behavior unchanged

Also update the Go Online / Go Offline button icons in those tables:

- `alt=""`
- `aria-hidden="true"`

#### `Views/Home/Index.cshtml`

For the offline banner icon next to the visible `Offline Mode:` text:

- set `alt=""`
- add `aria-hidden="true"`

### Acceptance Checks

- axe no longer flags the offline guidance bullets for color contrast
- Go Offline / Go Online icons no longer trigger redundant-alt findings where visible text already exists
- Go Offline, Set Offline Key, Remove From List, and the listed conflict/error modals all:
  - have accessible names
  - have discernible close buttons
  - move focus into the modal
  - keep focus trapped
  - support `Escape`
  - restore focus on close
  - prevent screen readers from browsing background content while open

## Part 2: Case Page Accessibility Fixes

### Goal

Resolve the workbook findings tied to:

- scrollable case form region keyboard access
- Case Narrative missing label
- possible missing `h1` on the Home Record route

### Scope

#### Scrollable region

Update the generated `.construct__body` container in the active case form renderers:

- `wwwroot/scripts/editor/page_renderer/form.mmria.js`
- `wwwroot/scripts/editor/page_renderer/form.pmss.js`
- `wwwroot/scripts/editor/page_renderer/form.abstractor.committee.js`
- `wwwroot/scripts/editor/page_renderer/form.committee_member.mmria.js`
- `wwwroot/scripts/editor/page_renderer/form.committee_member.pmss.js`
- `wwwroot/scripts/editor/page_renderer/form.pmss.attachment.js`

Changes:

- replace the current programmatic-only focus pattern with keyboard-reachable focus for the scrollable form region
- use `tabindex="0"` on `.construct__body` where it serves as the active scrollable region
- add an accessible name using `aria-labelledby` tied to the current form title
- keep the existing skip-link and scripted focus behavior working

#### Case Narrative labeling

Update:

- `wwwroot/scripts/editor/page_renderer.js`
- `wwwroot/scripts/editor/page_renderer/textarea.js`
- the Case Narrative label injection points in the active form renderers

Changes:

- give the visible Case Narrative heading/label a stable id
- associate `#case_narrative_editor` with that heading using `aria-labelledby`
- when Trumbowyg initializes, apply the same accessible name to the generated editor surface so the rich-text editor is labeled in practice, not just the hidden textarea

#### Page `h1` validation

Treat the Home Record `h1` finding as validation-first.

Current state to preserve unless needed:

- the summary renderer already emits an `h1`
- the construct header renderer already emits an `h1`

Plan:

1. re-run axe/manual review on the exact Home Record route after the `.construct__body` and Case Narrative fixes
2. if the `h1` finding still reproduces, add one persistent route-level `h1` host in the case shell so the active case page always exposes a level-one heading during the dynamic render lifecycle
3. keep visible design unchanged as much as possible

### Acceptance Checks

- `.construct__body` is keyboard reachable on the affected case pages
- the scrollable region has an accessible name
- Case Narrative is labeled for:
  - the textarea
  - the Trumbowyg editor surface
- the Home Record route no longer fails the `h1` check, or the fallback `h1` host is added only if validation proves it is still needed

## Deferred Item

The workbook item below is intentionally deferred and should be planned separately:

- `App-wide / Timeout warning missing`

## Defaults And Constraints

- no route changes
- no API changes
- no auth/session behavior changes in this split plan
- no shared modal refactor for this pass
- keep existing user flows and business logic unchanged unless an accessibility behavior requires the modal/focus updates listed above
