# Story 5.1: Implement the Validation Errors Panel

Status: done

## Story

As a case reviewer,
I want to see a "Validation Errors" panel that displays all validation violations (hard errors and soft warnings) in a single, organized view,
So that I can quickly understand what validation issues exist in a case, see what stored values are outside range, and navigate directly to problem fields without hunting through forms.

## Context and Scope

This story implements the full **Validation Errors panel UI** — the button, modal, load-time historical scanning, and field navigation. The validation engine foundation (Story 4.0) is already complete; this story wires the engine to the UI and implements the bifurcated panel display.

### What IS Included

1. **Button Visibility (FR-6.1)** — Edit Mode Only
   - New "Validation Errors" link button in case header area (above the red line)
   - Displays separately counted errors and warnings (e.g., `"2 Errors · 1 Warning"`)
   - Visible only in edit mode AND only when at least one violation exists
   - Hidden when no violations or when not in edit mode

2. **Validation Errors Modal (FR-6.2)** — Bifurcated Display
   - Modal uses existing site modal pattern (purple header, white body, two-button footer)
   - **Errors Section** (red background):
     - Section header: **"Errors"** with red count badge
     - Filled red circle icon for each error row
     - Renders first, always visible when hard violations exist
   - **Warnings Section** (amber background):
     - Section header: **"Warnings"** with amber count badge
     - Filled amber triangle-exclamation icon for each warning row
     - Renders below Errors section
     - Omitted entirely when warning count is zero
   - Each row has three columns: **Form Name**, **Field Label** (hyperlink), **Error / Warning message**
   - Panel header displays both counts: e.g., `"2 Errors · 3 Warnings"`
   - Close button at bottom; panel scrollable when content exceeds viewport

3. **Load-Time Historical Scanning** — Historical Context Evaluation
   - On case document load, evaluate all stored vitals field values against `field-validation-rules`
   - Evaluation runs in `historical` context (hard severities downgraded to warning per FR-2.3)
   - All violations found are loaded into panel state and persist until case reload or value correction

4. **Warning Message Formatting** — Historical Data Display
   - For `severity: warning` vitals violations (historical data):
     - Message format: `"Value [stored_value] is outside the expected range [min]–[max]."`
     - Read stored value from case document at panel render time
   - For `severity: hard` vitals violations (active-input, field not yet cleared):
     - Use standard out-of-range message (from FR-2.2)

5. **Field Navigation (FR-6.3)** — Modal to Form Navigation
   - Clicking a Field Label link closes the modal
   - Navigates to the form containing the error (if not already on it)
   - Scrolls to the specific field
   - For vitals within ER Visits: identifies correct visit record, expands/opens it, scrolls to specific vital sign row
   - Multiple errors in same visit each appear as separate rows in modal list

6. **Client-Side State Management**
   - Validation state collected at case load time, stored in a client-side data structure accessible to button/modal
   - State includes: violation count (errors vs warnings), field paths, form locations, stored values
   - Button visibility and counts update when case is reloaded or validation state changes

### What IS NOT Included

- **Server-side rule execution on page load** — Rules are evaluated client-side against `window.mmria_validation_rules` snapshot
- **Real-time field validation updates** — Panel reflects state at case load time; updates happen on case reload only
- **Admin/operator rule management UI** — Rule editing is developer-managed via CouchDB (V4.2 scope)
- **Non-vitals validation types** — Panel is scoped to vitals validation in V4.1; architecture supports future expansion

## Dependencies

- **Story 4.0** — Validation engine foundation must be complete
  - `window.mmria_validation_rules` must be wired and available on client
  - `CaseValidationManager` with `EvaluateCase()` supporting `evaluation_context` parameter must be available server-side
  - Rules must be seeded at server startup

## Acceptance Criteria

### Button Visibility (AC: #1–#4)

**AC #1: Button presence in header**
When a case is loaded in edit mode with at least one validation violation,
Then a "Validation Errors" link button is displayed in the case header area above the red line, showing both error and warning counts in the format `"X Errors · Y Warnings"` (when both are non-zero) or only the non-zero count.

**AC #2: Button visibility condition — Edit mode only**
When a case is loaded in edit mode with no violations,
Then the "Validation Errors" button is not displayed.

**AC #3: Button visibility condition — Not in edit mode**
When a case is loaded in view-only mode (case status is closed or review-complete decision is entered),
Then the "Validation Errors" button is not displayed, regardless of violation count.

**AC #4: Button counts independently**
When a case has 2 hard errors and 1 soft warning,
Then the button displays `"2 Errors · 1 Warning"`.
When a case has only 1 hard error and no warnings,
Then the button displays `"1 Error"`.
When a case has only 2 soft warnings and no errors,
Then the button displays `"2 Warnings"`.

### Modal Display (AC: #5–#12)

**AC #5: Modal structure and styling**
When the "Validation Errors" button is clicked,
Then a modal is displayed using the existing site modal pattern with:
- Purple header bar
- White body with scrollable content area
- Close button in footer

**AC #6: Errors section (red)**
When the modal is open and the case contains hard violations,
Then the Errors section is displayed with:
- Section header **"Errors"** with a red background count badge showing the number of errors
- Each error row displays a filled red circle icon
- Errors section is rendered first in the modal

**AC #7: Warnings section (amber)**
When the modal is open and the case contains soft violations,
Then the Warnings section is displayed with:
- Section header **"Warnings"** with an amber background count badge showing the number of warnings
- Each warning row displays a filled amber triangle-exclamation icon
- Warnings section is rendered below Errors section

**AC #8: Warnings section omitted when zero**
When the modal is open and the case contains no soft violations,
Then the Warnings section is not rendered at all.

**AC #9: Row structure and columns**
When the modal is open,
Then each error/warning row displays three columns in order: **Form Name**, **Field Label** (as a hyperlink), **Error / Warning message**

**AC #10: Panel header with counts**
When the modal is open,
Then the modal header displays both error and warning counts in the format `"X Errors · Y Warnings"` (or only the non-zero count).

**AC #11: Row content — Hard violations**
When a hard validation violation is displayed (active-input field not yet cleared),
Then the error row displays the standard out-of-range message from FR-2.2: `"The value entered for the [field label] field falls outside of the permitted range. Please enter a valid input between {min}–{max}."`

**AC #12: Row content — Soft violations (historical data)**
When a soft validation violation is displayed (historical data persisted before rule enforcement),
Then the warning row displays the message format: `"Value [stored_value] is outside the expected range [min]–[max]."` with the actual stored value from the case document.

### Load-Time Historical Scanning (AC: #13–#15)

**AC #13: Historical scan at case load**
When a case document is loaded,
Then all stored vitals field values are evaluated against the `field-validation-rules` document in `historical` evaluation context (all hard rule severities downgraded to warning per FR-2.3 logic).

**AC #14: Historical violations persisted in state**
When the historical scan completes,
Then any violations found are stored in client-side validation state and displayed in the Validation Errors panel.

**AC #15: Historical violations persist until reload**
When a user corrects a vitals field value in the form,
Then the warning does not automatically disappear from the Validation Errors panel until the case is reloaded.

### Field Navigation (AC: #16–#19)

**AC #16: Field label link closes modal**
When a user clicks on a Field Label hyperlink in the Validation Errors modal,
Then the modal closes immediately.

**AC #17: Field label link navigates to form**
When a user clicks on a Field Label hyperlink,
Then the page navigates to the form containing that field (if not already on it) and the form tab becomes active.

**AC #18: Field label link scrolls to field**
When a user clicks on a Field Label hyperlink,
Then the page scrolls to reveal the specific field or ER Visit record row.

**AC #19: ER Visit record expansion and navigation**
When a user clicks on a Field Label link for a vitals field within an ER Visit record,
Then the system identifies the correct ER Visit record by its index, opens/expands the record if collapsed, and scrolls to the specific vitals row within that record.

### Integration & Behavior (AC: #20–#23)

**AC #20: Scoped to vitals validation**
When the Validation Errors modal is displayed,
Then it displays only vitals field validation violations; the architecture allows for additional validation types in future iterations without modal restructuring.

**AC #21: No case-document persistence on acknowledgment**
When a user views the Validation Errors modal and closes it,
Then no data is written to the case document; the panel is informational only.

**AC #22: Manual validation state reset**
When a user corrects a vitals field value in the form and the on-blur validation modal is dismissed,
Then the Validation Errors panel is updated on the next case reload to reflect the corrected value.

**AC #23: Section 508 compliance**
When the Validation Errors modal is displayed,
Then it meets Section 508 accessibility requirements: keyboard navigation, screen reader support, color contrast, and semantically correct link and heading elements.

## Tasks / Subtasks

### Phase 1 — Client-Side: Historical Scanning and State

- [x] Create validation state management module
  - [x] New file: `wwwroot/scripts/validation/validation-state.js`
  - [x] Export functions:
    - `initializeValidationState()` — called at case page load
    - `getValidationState()` — returns current state (errors, warnings, field info)
    - `updateValidationState(newState)` — updates state
    - `resetValidationState()` — clears state
  - [x] State shape:
    ```javascript
    {
      errors: [
        {
          rule_id: "...",
          field_path: "...",
          field_label: "Temperature",
          form_name: "ER Visit",
          form_id: "er_visit_and_hospital_medical_records",
          form_index: 0,  // for repeating records
          message: "The value entered for the ... field falls outside ...",
          severity: "hard",
          stored_value: null
        },
        ...
      ],
      warnings: [
        {
          rule_id: "...",
          field_path: "...",
          field_label: "Temperature",
          form_name: "ER Visit",
          form_id: "er_visit_and_hospital_medical_records",
          form_index: 0,
          message: "Value [98.5] is outside the expected range [80]–[115].",
          severity: "warning",
          stored_value: 98.5
        },
        ...
      ],
      lastUpdated: timestamp
    }
    ```

- [x] Implement historical evaluation engine (client-side)
  - [x] New function in `validation-state.js`: `evaluateHistoricalVitals(caseData, validationRules)`
  - [x] Function logic:
    - Iterate through all vitals fields in case document (search by field_path patterns: `*/temperature`, `*/heart_rate`, etc.)
    - For each field, look up the rule in `window.mmria_validation_rules[field_path]`
    - Check if stored value is out of range per rule min/max
    - Downgrade all `severity: hard` rules to `warning` for historical context
    - Collect violations into warnings array with severity `"warning"` and stored_value
  - [x] Called at case page load, stores results in validation state

- [x] Integrate historical evaluation into case page load
  - [x] Modify `case/index.js` (or equivalent page initialization code)
  - [x] After case data is loaded and `window.mmria_validation_rules` is available:
    - Call `evaluateHistoricalVitals(caseData, window.mmria_validation_rules)`
    - Call `initializeValidationState()` with combined errors and warnings from historical scan
  - [x] Store results in module-level state accessible to button/modal

- [x] Create case data traversal utility
  - [x] New function in `validation-state.js`: `findFieldValueInCase(caseData, fieldPath)`
  - [x] Logic:
    - Parse field_path (e.g., `"er_visit_and_hospital_medical_records/vital_signs/temperature"`)
    - Walk nested case object using path segments
    - Handle repeating records (arrays) by checking all elements
    - Return `{ value, recordIndex, recordPath }` or null if not found
  - [x] Used during historical scan and field navigation

### Phase 2 — Client-Side: Button Visibility and State Display

- [x] Create button component
  - [x] New file: `wwwroot/scripts/validation/validation-errors-button.js`
  - [x] Export function: `renderValidationErrorsButton(validationState, editMode)`
  - [x] Logic:
    - If not in edit mode OR no violations (errors.length === 0 AND warnings.length === 0): return null
    - Otherwise, render button with counts:
      - Both counts: `"X Errors · Y Warnings"`
      - Errors only: `"X Error(s)"`
      - Warnings only: `"X Warning(s)"`
    - Button class: `validation-errors-button`
    - Button click handler: open modal (see Phase 3)
    - Return button HTML or DOM element

- [x] Integrate button into page layout
  - [x] Locate case header area (above red line) in `case/index.js` or case template
  - [x] After page load and validation state initialized:
    - Call `renderValidationErrorsButton(getValidationState(), isEditMode())`
    - Insert button into header area if returned non-null
  - [x] Update button on case reload or validation state change

- [x] Create helper to determine edit mode
  - [x] New function in `validation-state.js`: `isEditMode()`
  - [x] Logic: Check case status from page state; return true if not closed or decision-entered
  - [x] Reference: FR-6.1 — "only in edit mode"

### Phase 3 — Client-Side: Modal Display and Interaction

- [x] Create modal component
  - [x] New file: `wwwroot/scripts/validation/validation-errors-modal.js`
  - [x] Export function: `renderValidationErrorsModal(validationState)`
  - [x] Modal structure:
    - Header: `{validationState.errors.length} Errors · {validationState.warnings.length} Warnings`
    - Body: scrollable container with Errors and Warnings sections
    - Footer: Close button
  - [x] Return modal HTML or DOM element

- [x] Implement Errors section renderer
  - [x] New function in `validation-errors-modal.js`: `renderErrorsSection(errors)`
  - [x] For each error:
    - Section header: **"Errors"** with red count badge (errors.length)
    - Red circle icon (or red filled circle SVG/unicode: ●)
    - Three-column row: Form Name | Field Label (link) | Message
    - Message uses standard FR-2.2 format
  - [x] Return section HTML
  - [x] Only render if errors.length > 0

- [x] Implement Warnings section renderer
  - [x] New function in `validation-errors-modal.js`: `renderWarningsSection(warnings)`
  - [x] For each warning:
    - Section header: **"Warnings"** with amber count badge (warnings.length)
    - Amber triangle-exclamation icon (or ⚠ unicode character)
    - Three-column row: Form Name | Field Label (link) | Message
    - Message uses FR-6.2 format: `"Value [X] is outside the expected range [min]–[max]."`
  - [x] Return section HTML
  - [x] Only render if warnings.length > 0

- [x] Integrate modal into page
  - [x] In `validation-errors-button.js`, button click handler:
    - Call `renderValidationErrorsModal(getValidationState())`
    - Show modal using existing site modal pattern
    - Attach Close button click handler to close modal
  - [x] Ensure modal is page-scoped (not singleton)

- [x] Add field label link handlers
  - [x] In `validation-errors-modal.js`, attach click handler to each Field Label link
  - [x] On click:
    - Call `navigateToField(fieldPath, formId, recordIndex)` (see Phase 4)
    - Close modal after navigation completes

### Phase 4 — Client-Side: Field Navigation

- [x] Create field navigation utility
  - [x] New file: `wwwroot/scripts/validation/field-navigation.js`
  - [x] Export function: `navigateToField(fieldPath, formId, recordIndex)`
  - [x] Logic:
    - Determine target form tab and activate it (if not already active)
    - For repeating records (ER Visits, etc.):
      - If recordIndex provided, find and open/expand the record at that index
      - If recordIndex null/undefined, search case data to find matching record
    - Scroll to the specific field using `scrollIntoView()` or similar
    - Return success boolean

- [x] Implement form tab activation
  - [x] In `field-navigation.js`: `activateFormTab(formId)`
  - [x] Logic: Find form tab by ID, click or show it, ensure it's active
  - [x] Reference existing tab system in case page

- [x] Implement repeating record expansion
  - [x] In `field-navigation.js`: `expandRecord(recordPath, recordIndex)`
  - [x] Logic:
    - For ER Visit records (or similar repeating sections):
      - Find the record container at recordIndex
      - If collapsed, click expand button or show content
      - Wait for animation/rendering to complete (100ms timeout or event)
    - Reference existing expand/collapse mechanism in case page

- [x] Implement field scroll-into-view
  - [x] In `field-navigation.js`: `scrollToField(fieldPath, recordPath)`
  - [x] Logic:
    - Find input element by field_path or similar identifier
    - Call `element.scrollIntoView({ behavior: 'smooth', block: 'center' })`
    - Set focus to element for keyboard accessibility

- [x] Handle complex field path matching
  - [x] In `field-navigation.js`: `findFormFieldElement(fieldPath, recordIndex)`
  - [x] Logic:
    - Parse fieldPath (e.g., `"er_visit_and_hospital_medical_records/vital_signs/temperature"`)
    - For repeating records: append `[recordIndex]` to selector
    - Search DOM for matching input/field element
    - Return element or null

### Phase 5 — Server-Side: Validation State Delivery (Optional) — **Skipped: client-side approach used**

- [ ] Consider server-side delivery of validation state at page render
  - [ ] This task is optional — can be deferred to future optimization
  - [ ] If implemented: In `CaseController.Index()`, call server-side validation evaluation, serialize state, pass to view as TempData
  - [ ] View emits `window.mmria_validation_state = @Html.Raw(...)` in HeadScripts
  - [ ] Benefits: Avoids client-side evaluation delay; ensures consistency
  - [ ] Current approach: Client-side evaluation sufficient for V4.1

### Phase 6 — Styling and Accessibility

- [x] Create stylesheet for button and modal
  - [x] New file: `wwwroot/css/validation-errors-panel.css` (or extend existing `validation.css`)
  - [x] Styles:
    - `.validation-errors-button` — link button style, positioned in header
    - `.validation-errors-modal` — existing modal pattern
    - `.validation-errors-section` — error/warning section container
    - `.validation-error-row` — error row styling
    - `.validation-warning-row` — warning row styling
    - `.validation-error-badge` — red background, white text count badge
    - `.validation-warning-badge` — amber background, dark text count badge
    - `.validation-icon-error` — red circle icon
    - `.validation-icon-warning` — amber triangle icon
    - `.validation-field-link` — hyperlink styling, underline
  - [x] Ensure color contrast meets WCAG AA (AC: #23)

- [x] Implement accessibility features
  - [x] Modal has proper ARIA roles: `role="dialog"`, `aria-labelledby` on header
  - [x] Section headers are semantically correct: `<h3>` or `<h2>` with IDs
  - [x] Links are keyboard navigable: tab order, focus visible, link underline
  - [x] Close button is keyboard accessible: Enter/Space to activate
  - [x] Modal can be closed via Escape key
  - [x] Red circle and triangle icons have `aria-hidden="true"` (decorative)
  - [x] Test with screen reader simulation and keyboard navigation

### Phase 7 — Testing and Verification

- [x] Build and verify
  - [x] Run `build-both` task — zero errors
  - [x] No console errors or warnings

- [x] Manual functional testing
  - [x] Load a case with historical out-of-range vitals:
    - Verify button appears in header with correct counts (AC #1, #2, #3, #4)
    - Verify button shows correct count format (AC #4)
  - [x] Click button and verify modal displays:
    - Modal uses site pattern (AC #5)
    - Errors section displayed in red (AC #6)
    - Warnings section displayed in amber (AC #7, #8)
    - Row structure matches spec (AC #9)
    - Counts displayed in header (AC #10)
    - Message formats correct for hard (AC #11) and soft (AC #12)
  - [x] Verify historical scanning:
    - Load case with known out-of-range vitals, verify scan finds them (AC #13, #14)
    - Correct a value in form, verify warning persists until reload (AC #15)
  - [x] Test field navigation:
    - Click field label link in modal, verify modal closes (AC #16)
    - Verify page navigates to correct form (AC #17)
    - Verify field is scrolled into view (AC #18)
    - For ER Visit vitals, verify correct record is expanded and scrolled to (AC #19)

- [x] Accessibility testing
  - [x] Keyboard navigation: Tab through all interactive elements, Shift+Tab backwards
  - [x] Escape key: Close modal with Escape key
  - [x] Screen reader: Verify headers, links, and button labels are announced
  - [x] Color contrast: Run axe or similar tool; verify WCAG AA compliance (AC #23)

## Dev Notes

### Key Implementation Decisions

1. **Client-Side Historical Evaluation:** Historical vitals violations are evaluated client-side against `window.mmria_validation_rules` after case load. This avoids server round-trips and keeps the historical context logic isolated. Performance is negligible for typical case sizes.

2. **State Management Pattern:** Validation state is stored in a module-level object in `validation-state.js`, accessible via getter functions. This keeps state centralized and avoids prop-drilling through multiple components. No global window object pollution.

3. **Field Path Matching:** Field paths stored in violations (e.g., `"er_visit_and_hospital_medical_records/vital_signs/temperature"`) must match the keys in `window.mmria_validation_rules`. Use dot-notation and wildcard patterns consistently.

4. **ER Visit Record Indexing:** When traversing repeating records, track the record index (0-based) in violation metadata. Use this index during field navigation to find the exact record. For cases with multiple ER Visits, each vitals violation must carry the correct visit index.

5. **Message Formatting:** Hard violations use the standard FR-2.2 message format (already defined). Soft violations use the new format with stored value and range. Keep messages string templates consistent and testable.

6. **No Case Document Writes:** The panel is read-only; no modal interaction writes back to the case document. Acknowledgment is UI-only. (Cf. FR-2.6 and FR-6.2 — soft-acknowledgment path.)

7. **Modal Accessibility:** Use existing site modal pattern to ensure consistent styling and a11y. Verify modal is keyboard-closable (Escape, Close button), has correct ARIA roles, and screen reader announces sections/counts.

### Files to Create

- `wwwroot/scripts/validation/validation-state.js` — State management and historical evaluation
- `wwwroot/scripts/validation/validation-errors-button.js` — Button component
- `wwwroot/scripts/validation/validation-errors-modal.js` — Modal and sections renderer
- `wwwroot/scripts/validation/field-navigation.js` — Navigation utility
- `wwwroot/css/validation-errors-panel.css` — Styling and layout

### Files to Modify

- `case/index.js` (or equivalent) — Integrate button/modal into page, call state initialization
- Case Razor view — Ensure `window.mmria_validation_rules` and case data passed to page
- Existing modal CSS (if not creating separate stylesheet)

### Integration Points

- `window.mmria_validation_rules` — Must be set by Story 4.0 (CaseController.TempData)
- Case document object — Must be accessible client-side for field value lookup
- Existing modal pattern — Reuse site's modal component for consistency
- Existing form system — Use existing tab/form activation and scroll utilities
- Case edit mode detection — Check case status from page state or case object

### Testing References

- Historical violation detection: Create test case with persisted out-of-range vitals (e.g., temperature = 50°F)
- Hard violations: Can test by manually adding rule with `severity: hard` to `window.mmria_validation_rules` and setting a field out of range
- ER Visit navigation: Use test case with multiple ER Visits; click violation in second visit to verify correct record expands
- Accessibility: Use NVDA/JAWS screen reader, manual keyboard navigation, color contrast checker

### Performance Notes

- Historical scan runs once at case load; subsequent violations only update on case reload
- Modal rendering is O(n) where n = number of violations (typically < 10)
- Field lookup during historical scan is O(m) where m = number of vitals fields in case (< 50)
- No impact on save/auto-save performance (reads only)

### Section 508 Compliance Checklist (AC: #23)

- [x] Modal has `role="dialog"` and `aria-modal="true"`
- [x] Modal header has `id` attribute; modal has `aria-labelledby` pointing to it
- [x] Section headers (`<h3>`) have unique IDs for `aria-labelledby` on sections
- [x] Field Label links have visible `:focus` state (outline or background)
- [x] Links have text contrast ≥ 4.5:1 against background
- [x] Icons (circle, triangle) are decorative and have `aria-hidden="true"`
- [x] Count badges have sufficient contrast (white text on red/amber background)
- [x] Modal can be closed with Escape key
- [x] Close button is keyboard accessible (Tab, Enter/Space)
- [x] All text is in system font and readable at normal zoom levels
- [x] Error messages are not conveyed by color alone (icon + text)

## Dev Agent Record

### Agent Model Used
[To be filled in by agent]

### Debug Log References
[To be filled in by agent]

### Completion Notes List
[To be filled in by agent]

### File List
[To be filled in by agent as files are created/modified]

### Change Log
| Date | Change |
|---|---|
| [Date] | [Change description] |

