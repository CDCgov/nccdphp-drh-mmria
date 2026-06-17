# Story 4.1: Print/View/PDF Validation Gate (Supersedes Prior FR-2.6)

Status: done

## Story

As a case reviewer,
I want the View, View PDF, and Save PDF actions to warn me when a case contains out-of-range vitals — but only for open cases — so that I can decide whether to proceed before rendering.

## Context and Scope

This story does two things in a single atomic change:

1. **Removes the prior FR-2.6 behavior** implemented in Story 2.5:
   - Modal shown on edit-mode entry
   - Modal shown on form-navigation (`window_on_hash_change`)
   - Red text indicator per vitals record in graph and table views

2. **Implements the new FR-2.6 behavior** — the print/view/PDF validation gate.

The PRD states explicitly: *"Story 2.5 covers the implementation that was built under the prior requirement; the prior behavior is removed as part of the story implementing this requirement. This requirement fully supersedes the prior FR-2.6."*

**Story 4.0 Dependency:** Story 4.0 ports the vitals validation engine to client-side and wires `window.mmria_validation_rules` with seeded rule entries. Story 4.1 **does not** re-port the engine or reseed rules — it only implements the gate logic at the print/PDF call sites, using the engine and rules already provided by Story 4.0.

Both removal and implementation must land together in the same commit. Do not split them.

## Acceptance Criteria

**Closed-state bypass (AC: #1)**
When the user initiates a View, View PDF, or Save PDF action and `g_data.home_record.case_status.overall_case_status` is `4`, `5`, or `6`, the action is performed directly — no validation runs, no modal appears.

**Open-state — no out-of-range values (AC: #2)**
When the case is not closed and `mmria_vitals_revalidate_all()` returns `false`, the action is performed directly — no modal.

**Open-state — out-of-range values exist (AC: #3)**
When the case is not closed and violations are detected, the behavior depends on violation severity:

- **Hard violations only:** A modal is displayed with the hard-block message and a Close button only — no proceed path. The action is **not performed**. The user must resolve the hard violations before continuing.
- **Soft/warning violations (no hard violations):** A modal is displayed with the soft-acknowledgment message and two buttons: Close and contextual action. The user may acknowledge and proceed, or close without proceeding. Proceeding is UI-only — no change is persisted to the case document.

**Historical out-of-range vitals** (values persisted before rule enforcement) are always evaluated as `severity: warning`, triggering the soft-acknowledgment path, never the hard-block path.

**Modal specification (AC: #4)**
Two modal variants are displayed based on violation severity:

- **Hard-block modal:** Purple header, white body, single Close button.
  - Message: `"This case contains vital sign records with values outside the permitted range. These values must be corrected before printing or viewing."`
  - Button: **Close** only — dismisses the modal. The action is NOT performed.
  
- **Soft-acknowledgment modal:** Purple header, white body, two-button footer.
  - Message: `"This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views."`
  - Buttons: **Close** (action not performed) and **Contextual action** (label matches triggering action: `"View"`, `"View PDF"`, or `"Save PDF"` — clicking proceeds with the action without persisting changes).

Both modals use the existing site modal pattern (`background-color: #7b2d8e; color: white` header, white body).


**Section 508 (AC: #5)**
The modal meets Section 508 requirements: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` pointing to the header, focus goes to the Close button on open, Escape key dismisses.

**`window.mmria_vital_sign_range` null guard (AC: #6)**
If `window.mmria_vital_sign_range` is `null`, `mmria_vitals_revalidate_all()` already returns `false` — the action is performed directly with no modal.

**Prior FR-2.6 behavior removed (AC: #7)**
- No modal appears on edit-mode entry.
- No modal appears on form-selector navigation.
- No red text "Contains excluded values" row appears in any vitals chart or table.

## Tasks / Subtasks

### Phase 1 — Remove prior FR-2.6 behavior from `chart.js`

- [x] **Remove** `mmria_chart_has_excluded_values()` function (AC: #7)
  - Deleted from chart.js

- [x] **Remove** `mmria_vitals_show_historical_modal()` function (AC: #7)
  - Located at ~line 437, starts with `function mmria_vitals_show_historical_modal()`, ends with its closing `}` at ~line 498, just before `function chart_render(`
  - This is the "Historical Vitals Data" modal shown on edit-mode entry and form navigation

- [x] **Remove** `chart_has_excluded` indicator block from `chart_render()` (AC: #7)
  - In `chart_render()`, locate the two lines at ~line 520:
    ```javascript
    const chart_has_excluded = mmria_chart_has_excluded_values(p_metadata, p_object_path);
    const chart_excluded_row = chart_has_excluded
        ? `<tr><td colspan="100" style="padding:4px 8px;color:#c00;font-size:small;text-align:left;">Contains excluded values &#8212; one or more readings fall outside the permitted range.</td></tr>`
        : '';
    ```
  - Remove both `const` declarations entirely
  - Also remove the `${chart_excluded_row}` interpolation from the template literal (~line 545):
    - Current: `</tr>\n            ${chart_excluded_row}\n            <tr align=center><td>`
    - After: `</tr>\n            <tr align=center><td>`

- [x] **Remove** `chart_has_excluded_tbl` indicator block from `chart_switch_to_table()` (AC: #7)
  - In `chart_switch_to_table()`, locate the two lines at ~line 1301:
    ```javascript
    const chart_has_excluded_tbl = mmria_chart_has_excluded_values(metadata, params.p_object_path);
    const chart_excluded_row_tbl = chart_has_excluded_tbl
        ? `<tr><td colspan="100" style="padding:4px 8px;color:#c00;font-size:small;text-align:left;">Contains excluded values &#8212; one or more readings fall outside the permitted range.</td></tr>`
        : '';
    ```
  - Remove both `const` declarations entirely
  - Also remove `${chart_excluded_row_tbl}` from the template literal (~line 1336):
    - Current: `${chart_excluded_row_tbl}${data_table_body_html.join("")}`
    - After: `${data_table_body_html.join("")}`

### Phase 2 — Remove prior FR-2.6 behavior from `case/index.js`

- [x] **Remove** edit-mode entry call at ~line 4556 (AC: #7)
  - Locate the block that immediately follows `$global.case_document_begin_edit()`:
    ```javascript
    if (mmria_vitals_revalidate_all())
    {
        mmria_vitals_show_historical_modal();
    }
    ```
  - Remove the entire `if` block (3 lines). Leave the surrounding code intact.

- [x] **Remove** form-navigation call at ~line 3080 (AC: #7)
  - Locate the block inside `window_on_hash_change` after `g_render()`:
    ```javascript
    if (g_data_is_checked_out && mmria_vitals_revalidate_all())
    {
        mmria_vitals_show_historical_modal();
    }
    ```
  - Remove the entire `if` block (3 lines). Leave `g_render()` and the surrounding code intact.

### Phase 3 — Add new helpers and gate modal to `chart.js`

- [x] **Add** `mmria_vitals_case_is_closed()` in `chart.js` (AC: #1)
  - Insert immediately after the closing `}` of `mmria_vitals_revalidate_all()` (which ends at ~line 434, just before where `mmria_vitals_show_historical_modal` was)
  - Function:
    ```javascript
    function mmria_vitals_case_is_closed()
    {
        if (!window.g_data) { return false; }
        var hr = g_data.home_record;
        if (!hr || !hr.case_status || !hr.case_status.overall_case_status) { return false; }
        var status = Number(hr.case_status.overall_case_status);
        return status === 4 || status === 5 || status === 6;
    }
    ```
  - Note: uses `Number()` not `new Number()` (avoids object-vs-primitive comparison pitfall); no dependency on `g_is_confirm_for_case_lock` — the bypass is unconditional when the case is closed.

- [x] **Add** `mmria_vitals_has_hard_violations()` in `chart.js` (AC: #3)
  - Insert immediately after `mmria_vitals_case_is_closed()`
  - Function must evaluate all vitals in the current case against the rules document (provided by Story 4.0 in `window.mmria_validation_rules`) and return `true` if any `severity: "hard"` violations are detected, `false` otherwise.
  - Use the engine and rules from Story 4.0 — do not duplicate validation logic.
  - Implementation guidance: iterate through case vitals, check each against `window.mmria_validation_rules`, apply rule severity as-is (do not downgrade), and return `true` if any hard violation found.
  - If `window.mmria_validation_rules` is `null` or unavailable, return `false` (no hard violations detected).
  - Example stub:
    ```javascript
    function mmria_vitals_has_hard_violations()
    {
        if (!window.g_data || !window.mmria_validation_rules) { return false; }
        // TODO: Iterate case vitals, check against window.mmria_validation_rules rules,
        // return true if any rule with severity === "hard" is violated, false otherwise.
        // Use the engine and rules from Story 4.0 — do not duplicate validation logic.
        return false;  // placeholder
    }
    ```

- [x] **Add** `mmria_vitals_show_print_gate_modal(actionLabel, isHardBlock, onConfirm)` in `chart.js` (AC: #3, #4, #5)
  - Insert immediately after `mmria_vitals_has_hard_violations()`, in the slot formerly occupied by `mmria_vitals_show_historical_modal()`
  - Parameters:
    - `actionLabel` — string: `"View"`, `"View PDF"`, or `"Save PDF"`
    - `isHardBlock` — boolean: `true` for hard-violation modal (close only), `false` for soft-acknowledgment modal (two buttons)
    - `onConfirm` — function: callback to execute when contextual action button is clicked (soft mode only; ignored in hard mode)
  - Hard-block modal HTML structure:
    - Purple header with title "Vital Signs Out of Range"
    - Body with hard-block message: "This case contains vital sign records with values outside the permitted range. These values must be corrected before printing or viewing."
    - Single Close button (same styling as soft mode)
    - On Close: dismiss modal, do NOT call `onConfirm`, do NOT perform action
    - Escape key closes (AC: #5)
  - Soft-acknowledgment modal HTML structure:
    - Purple header with title "Vital Signs Out of Range"
    - Body with soft-acknowledgment message: "This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views."
    - Two buttons: Close (no action) and contextual action button (label: `actionLabel`)
    - Close button: dismisses, no action
    - Contextual action button: dismisses and calls `onConfirm()`
    - Escape key closes (AC: #5)
  - Both modals: Section 508 compliance (AC: #5) — `role="dialog"`, `aria-modal="true"`, `aria-labelledby` pointing to header, focus goes to Close button on open.
  - Clean up any existing modals before inserting new one (prevent duplication).
  - Implementation (supports both hard-block and soft-acknowledgment modes):
    ```javascript
    function mmria_vitals_show_print_gate_modal(actionLabel, isHardBlock, onConfirm)
    {
        var existingModal = document.getElementById('vitals-print-gate-modal');
        if (existingModal && existingModal.parentNode) { existingModal.parentNode.removeChild(existingModal); }
        var existingBackdrop = document.getElementById('vitals-print-gate-backdrop');
        if (existingBackdrop && existingBackdrop.parentNode) { existingBackdrop.parentNode.removeChild(existingBackdrop); }

        var message = isHardBlock
            ? 'This case contains vital sign records with values outside the permitted range. These values must be corrected before printing or viewing.'
            : 'This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views.';

        var proceedButtonHtml = isHardBlock
            ? ''
            : '<button type="button" id="vitals-print-gate-modal-proceed" class="btn btn-primary" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">' + actionLabel + '</button>';

        var modalHtml =
            '<div id="vitals-print-gate-modal" class="modal fade" tabindex="-1" role="dialog" aria-modal="true" aria-labelledby="vitals-print-gate-modal-title" style="z-index: 1050;">'
            + '<div class="modal-dialog" role="document">'
            + '<div class="modal-content">'
            + '<div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">'
            + '<h4 id="vitals-print-gate-modal-title" class="modal-title" style="margin: 0; font-weight: 600; font-size: 17px;">Vital Signs Out of Range</h4>'
            + '</div>'
            + '<div class="modal-body" style="padding: 20px;">'
            + '<p style="font-size: 16px; color: #333; margin: 0;">' + message + '</p>'
            + '</div>'
            + '<div class="modal-footer" style="padding: 15px 20px; text-align: right;">'
            + '<button type="button" id="vitals-print-gate-modal-close" class="btn btn-default" style="padding: 8px 20px; margin-right: 8px;">Close</button>'
            + proceedButtonHtml
            + '</div>'
            + '</div>'
            + '</div>'
            + '</div>'
            + '<div id="vitals-print-gate-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>';

        document.body.insertAdjacentHTML('beforeend', modalHtml);

        var modal = document.getElementById('vitals-print-gate-modal');
        var backdrop = document.getElementById('vitals-print-gate-backdrop');

        setTimeout(function()
        {
            if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
            if (backdrop) { backdrop.classList.add('show'); }
            var closeBtn = document.getElementById('vitals-print-gate-modal-close');
            if (closeBtn) { closeBtn.focus(); }
        }, 10);

        function closeGateModal()
        {
            if (modal) { modal.classList.remove('show'); }
            if (backdrop) { backdrop.classList.remove('show'); }
            setTimeout(function()
            {
                if (modal && modal.parentNode) { modal.parentNode.removeChild(modal); }
                if (backdrop && backdrop.parentNode) { backdrop.parentNode.removeChild(backdrop); }
            }, 150);
        }

        var closeBtn = document.getElementById('vitals-print-gate-modal-close');
        if (closeBtn) { closeBtn.onclick = closeGateModal; }

        var proceedBtn = document.getElementById('vitals-print-gate-modal-proceed');
        if (proceedBtn)
        {
            proceedBtn.onclick = function()
            {
                closeGateModal();
                if (typeof onConfirm === 'function') { onConfirm(); }
            };
        }

        if (modal)
        {
            modal.addEventListener('keydown', function(e)
            {
                if (e.key === 'Escape') { e.preventDefault(); closeGateModal(); }
            });
        }
    }
    ```

            + '</div>'
            + '<div class="modal-footer" style="padding: 15px 20px; text-align: right;">'
            + '<button type="button" id="vitals-print-gate-modal-close" class="btn btn-default" style="padding: 8px 20px; margin-right: 8px;">Close</button>'
            + '<button type="button" id="vitals-print-gate-modal-proceed" class="btn btn-primary" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">' + actionLabel + '</button>'
            + '</div>'
            + '</div>'
            + '</div>'
            + '</div>'
            + '<div id="vitals-print-gate-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>';

        document.body.insertAdjacentHTML('beforeend', modalHtml);

        var modal = document.getElementById('vitals-print-gate-modal');
        var backdrop = document.getElementById('vitals-print-gate-backdrop');

        setTimeout(function()
        {
            if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
            if (backdrop) { backdrop.classList.add('show'); }
            var closeBtn = document.getElementById('vitals-print-gate-modal-close');
            if (closeBtn) { closeBtn.focus(); }
        }, 10);

        function closeGateModal()
        {
            if (modal) { modal.classList.remove('show'); }
            if (backdrop) { backdrop.classList.remove('show'); }
            setTimeout(function()
            {
                if (modal && modal.parentNode) { modal.parentNode.removeChild(modal); }
                if (backdrop && backdrop.parentNode) { backdrop.parentNode.removeChild(backdrop); }
            }, 150);
        }

        var closeBtn = document.getElementById('vitals-print-gate-modal-close');
        if (closeBtn) { closeBtn.onclick = closeGateModal; }

        var proceedBtn = document.getElementById('vitals-print-gate-modal-proceed');
        if (proceedBtn)
        {
            proceedBtn.onclick = function()
            {
                closeGateModal();
                if (typeof onConfirm === 'function') { onConfirm(); }
            };
        }

        if (modal)
        {
            modal.addEventListener('keydown', function(e)
            {
                if (e.key === 'Escape') { e.preventDefault(); closeGateModal(); }
            });
        }
    }
    ```

### Phase 4 — Modify print/view/PDF action handlers in `case/index.js`

The strategy for both functions is identical: extract the `openTab` call into a local `performAction` closure, then gate it through three checks:
1. **Case is closed?** → bypass gate, perform action directly
2. **Hard violations exist?** → show hard-block modal (close only, action blocked)
3. **Soft/warning violations exist?** → show soft-acknowledgment modal (two buttons, action gated on acknowledgment)
4. **No violations?** → perform action directly

- [x] **Modify** `pdf_case_onclick(event, type_output)` (AC: #1–#6)
  - Current function structure (~line 4234):
    ```javascript
    function pdf_case_onclick(event, type_output) {
        const btn = event.target;
        const dropdown = ( type_output == 'view' )
            ? btn.previousSibling.previousSibling
            : btn.previousSibling.previousSibling.previousSibling;
        let section_name = dropdown.value;
        unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);
        if (section_name) {
            const selectedOption = dropdown.options[dropdown.options.selectedIndex];
            const record_number = selectedOption.dataset.record;
            if(section_name == "all_hidden") {
                section_name = 'all';
                window.setTimeout(function() {
                    openTab('./pdf-version', unique_tab_name, section_name, type_output, record_number, true);
                }, 1000);
            } else {
                window.setTimeout(function() {
                    openTab('./pdf-version', unique_tab_name, section_name, type_output, record_number);
                }, 1000);
            }
        }
    }
    ```
  - Replace the body of the `if (section_name)` block with:
    ```javascript
        if (section_name) {
            const selectedOption = dropdown.options[dropdown.options.selectedIndex];
            const record_number = selectedOption.dataset.record;
            const is_all_hidden = section_name == "all_hidden";
            if (is_all_hidden) { section_name = 'all'; }

            function performPdfAction() {
                window.setTimeout(function() {
                    if (is_all_hidden) {
                        openTab('./pdf-version', unique_tab_name, section_name, type_output, record_number, true);
                    } else {
                        openTab('./pdf-version', unique_tab_name, section_name, type_output, record_number);
                    }
                }, 1000);
            }

            // Bypass gate if case is closed
            if (mmria_vitals_case_is_closed()) {
                performPdfAction();
                return;
            }

            // Check for hard violations — block with hard-block modal
            if (mmria_vitals_has_hard_violations()) {
                var actionLabel = type_output === 'view' ? 'View PDF' : 'Save PDF';
                mmria_vitals_show_print_gate_modal(actionLabel, true, null);  // isHardBlock=true
                return;
            }

            // Check for soft/warning violations — gate with soft-acknowledgment modal
            if (mmria_vitals_revalidate_all()) {
                var actionLabel = type_output === 'view' ? 'View PDF' : 'Save PDF';
                mmria_vitals_show_print_gate_modal(actionLabel, false, performPdfAction);  // isHardBlock=false
                return;
            }

            // No violations — perform action directly
            performPdfAction();
        }
    ```

- [x] **Modify** `print_case_onclick(event)` (AC: #1–#6)
  - Current function structure (~line 4275):
    ```javascript
    function print_case_onclick(event) {
        const btn = event.target;
        const dropdown = btn.previousSibling;
        let section_name = dropdown.value;
        unique_tab_name = '_print_tab_' + Math.random().toString(36).substring(2, 9);
        if (section_name) {
            const selectedOption = dropdown.options[dropdown.options.selectedIndex];
            const record_number = selectedOption.dataset.record;
            if(section_name == "all_hidden") {
                section_name = 'all';
                window.setTimeout(function() {
                    openTab('./print-version', unique_tab_name, section_name, 'print', record_number, true);
                }, 1000);
            } else {
                window.setTimeout(function() {
                    openTab('./print-version', unique_tab_name, section_name, 'print', record_number);
                }, 1000);
            }
        }
    }
    ```
  - Replace the body of the `if (section_name)` block with:
    ```javascript
        if (section_name) {
            const selectedOption = dropdown.options[dropdown.options.selectedIndex];
            const record_number = selectedOption.dataset.record;
            const is_all_hidden = section_name == "all_hidden";
            if (is_all_hidden) { section_name = 'all'; }

            function performPrintAction() {
                window.setTimeout(function() {
                    if (is_all_hidden) {
                        openTab('./print-version', unique_tab_name, section_name, 'print', record_number, true);
                    } else {
                        openTab('./print-version', unique_tab_name, section_name, 'print', record_number);
                    }
                }, 1000);
            }

            // Bypass gate if case is closed
            if (mmria_vitals_case_is_closed()) {
                performPrintAction();
                return;
            }

            // Check for hard violations — block with hard-block modal
            if (mmria_vitals_has_hard_violations()) {
                mmria_vitals_show_print_gate_modal('View', true, null);  // isHardBlock=true
                return;
            }

            // Check for soft/warning violations — gate with soft-acknowledgment modal
            if (mmria_vitals_revalidate_all()) {
                mmria_vitals_show_print_gate_modal('View', false, performPrintAction);  // isHardBlock=false
                return;
            }

            // No violations — perform action directly
            performPrintAction();
        }
    ```

## Dev Notes

### Primary files
- `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`
- `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`

### No other files touched
- No server-side C# changes
- No changes to any print renderer, PDF renderer, or other JS file
- No database document changes
- No build step required for JS changes

### Story 2.5 baseline reference
Story 2.5 is `done` at commit `3d907ee8d92473811ad909f4e6924e06106d7934`. The following symbols introduced in Story 2.5 are removed entirely by this story:
- `mmria_chart_has_excluded_values()` — function in `chart.js`
- `mmria_vitals_show_historical_modal()` — function in `chart.js`
- `chart_has_excluded` / `chart_excluded_row` — local vars in `chart_render()`
- `chart_has_excluded_tbl` / `chart_excluded_row_tbl` — local vars in `chart_switch_to_table()`
- Two call sites in `case/index.js` (edit-mode entry + form nav)

The function `mmria_vitals_revalidate_all()` is **kept** — it is reused by the new print gate logic.

### Case closed-state numeric codes
The `overall_case_status` values 4, 5, 6 correspond to the three closed statuses per the PRD:
- `4` — Review complete and decision entered
- `5` — Out of Scope and death certificate entered
- `6` — False Positive and death certificate entered

This mapping is confirmed by the existing `is_case_locked()` function in `case/index.js` which uses the same three values. The new `mmria_vitals_case_is_closed()` does NOT reuse `is_case_locked()` because that function applies a `g_is_confirm_for_case_lock` gate that must not affect the print gate bypass.

### Modal IDs — no conflict with existing modals
- `vitals-print-gate-modal` (new — this story)
- `vitals-print-gate-backdrop` (new — this story)
- `vitals-range-modal` (Story 2.2 — field-level blur modal, unaffected)
- `vitals-historical-modal` (Story 2.5 — removed by this story)

### Button label mapping
| Trigger | `type_output` arg | Contextual button label |
|---|---|---|
| View button (`print_case_onclick`) | n/a | `"View"` |
| View PDF button (`pdf_case_onclick`) | `'view'` | `"View PDF"` |
| Save PDF button (`pdf_case_onclick`) | `'save'` | `"Save PDF"` |

### Prerequisites
- Story 2.1 must be complete (provides `window.mmria_vital_sign_range` and `mmria_vitals_is_out_of_range`)
- Story 2.5 must be complete (this story removes its implementation)
- **Story 4.0 must be complete** — ports the vitals validation engine to client-side, seeds `window.mmria_validation_rules` with rule entries, and provides the engine functions that evaluate violations by severity. Story 4.1 uses the engine and rules from Story 4.0 and does **not** re-port the engine or reseed rules.
- All four are confirmed done.

### Verification checklist
After implementation, manually verify:

**Hard violations (should block action):**
1. Create/find a case with a hard violation (e.g., temperature value 150 °F when max is 115). Click any print/view/PDF action → hard-block modal appears with message "These values must be corrected..." and Close button only. Click Close → modal dismisses, print does NOT open. No Proceed button exists.

**Soft/warning violations (should gate action):**
2. Open a case with only soft/warning violations (or manually downgrade a rule severity to "soft"). Click "View" → soft-acknowledgment modal appears with "Close" and "View" buttons. Click Close → modal dismisses, print does NOT open. Click "View" button → print view opens.
3. Same case: click "View PDF" → modal appears with "View PDF" button. Proceed → PDF view opens.
4. Same case: click "Save PDF" → modal appears with "Save PDF" button. Proceed → PDF save opens.

**Closed state bypass (should bypass gate entirely):**
5. Open a case with status 4, 5, or 6 containing any violations (hard or soft). Click any print/view/PDF action → opens directly, no modal appears.

**No violations (should proceed directly):**
6. Open a case with no out-of-range vitals. Click any action → opens directly.

**Prior FR-2.6 behavior (should be removed):**
7. Enter edit mode on any case with out-of-range vitals → NO modal appears (prior behavior removed).
8. While in edit mode, navigate to a different form → NO modal appears (prior behavior removed).
9. Open any vitals chart or table → NO red "Contains excluded values" row appears (prior behavior removed).

**Edge cases:**
10. `window.mmria_validation_rules = null` in console → check for hard violations returns false, all three actions open directly (soft-acknowledgment flow still works if mmria_vitals_revalidate_all() returns true).
11. `window.g_data = null` → all gate functions return false, actions proceed directly without error.

## Dev Agent Record

### Agent Model Used
_To be filled in by dev agent_

### Completion Notes List
_To be filled in by dev agent_

### Change Log
_To be filled in by dev agent_
