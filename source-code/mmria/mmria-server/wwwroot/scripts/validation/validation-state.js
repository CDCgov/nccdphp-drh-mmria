// validation-state.js
// Manages client-side validation state for case vitals violations.
// Story 5.1: Validation Errors Panel

(function (window) {
    'use strict';

    var _state = {
        errors: [],
        warnings: [],
        infos: [],
        lastUpdated: null
    };

    function initializeValidationState() {
        _state = { errors: [], warnings: [], infos: [], lastUpdated: null };
    }

    function getValidationState() {
        return _state;
    }

    function updateValidationState(newState) {
        _state = newState;
        _state.lastUpdated = Date.now();
    }

    function resetValidationState() {
        initializeValidationState();
    }

    // Returns true if case is in edit mode (checked out).
    function isEditMode() {
        return typeof g_data_is_checked_out !== 'undefined' && g_data_is_checked_out === true;
    }

    // Recursively walks caseData following path segments, expanding every array encountered.
    // Returns an array of { value, formId, formIndex, parentPath, indices, arraySegments }
    // arraySegments: [ { key: 'form_name', index: 0 }, ... ] for each array level crossed.
    function collectFieldInstances(obj, segments, segIdx, arraySegments) {
        if (obj === null || obj === undefined) { return []; }
        if (segIdx === segments.length) {
            return [{ value: obj, arraySegments: arraySegments }];
        }

        var key = segments[segIdx];
        var child = obj[key];

        if (child === null || child === undefined) { return []; }

        if (Array.isArray(child)) {
            var result = [];
            for (var i = 0; i < child.length; i++) {
                var newSegments = arraySegments.concat([{ key: key, index: i }]);
                var sub = collectFieldInstances(child[i], segments, segIdx + 1, newSegments);
                result = result.concat(sub);
            }
            return result;
        }

        return collectFieldInstances(child, segments, segIdx + 1, arraySegments);
    }

    // Builds a human-readable form label from array segments.
    // e.g. [ {key:'er_visit_and_hospital_medical_records', index:0}, {key:'vital_signs', index:0} ]
    //   -> "Er Visit And Hospital Medical Records 1 — Vital Signs 1"
    function buildFormLabel(arraySegments, fieldSegments) {
        if (!arraySegments || arraySegments.length === 0) {
            // No array levels — use the top-level form segment
            return toTitleCase(fieldSegments[0] || '');
        }
        var parts = [];
        for (var i = 0; i < arraySegments.length; i++) {
            var seg = arraySegments[i];
            parts.push(toTitleCase(seg.key) + ' ' + (seg.index + 1));
        }
        return parts.join(' \u2014 ');
    }

    function toTitleCase(str) {
        return str.replace(/_/g, ' ').replace(/\b\w/g, function (c) { return c.toUpperCase(); });
    }

    // Evaluates ALL fields in caseData against validationRules using each rule's field_path.
    // Fully data-driven: adding a new rule is sufficient to add new validation — no code changes needed.
    // Historical context: hard severity is downgraded to warning (stored data, not active input).
    function evaluateHistoricalVitals(caseData, validationRules) {
        var errors = [];
        var warnings = [];
        var infos = [];

        if (!caseData || !validationRules) {
            return { errors: errors, warnings: warnings, infos: infos };
        }

        var ruleKeys = Object.keys(validationRules);

        for (var ki = 0; ki < ruleKeys.length; ki++) {
            var ruleKey = ruleKeys[ki];
            var rule = validationRules[ruleKey];
            if (!rule || !rule.enabled) { continue; }
            if (rule.min_value === undefined && rule.max_value === undefined) { continue; }

            var fieldPath = rule.field_path || ruleKey;
            var segments = fieldPath.split('/').filter(Boolean);
            if (segments.length === 0) { continue; }

            var fieldName = segments[segments.length - 1];
            var instances = collectFieldInstances(caseData, segments, 0, []);

            for (var ii = 0; ii < instances.length; ii++) {
                var instance = instances[ii];
                var storedValue = instance.value;

                if (storedValue === null || storedValue === undefined || storedValue === '') { continue; }
                var numVal = parseFloat(storedValue);
                if (isNaN(numVal)) { continue; }

                var isOutOfRange = false;
                if (rule.min_value !== undefined && numVal < parseFloat(rule.min_value)) { isOutOfRange = true; }
                if (rule.max_value !== undefined && numVal > parseFloat(rule.max_value)) { isOutOfRange = true; }
                if (!isOutOfRange) { continue; }

                // Historical context: hard severities are downgraded to warning
                var effectiveSeverity = rule.severity === 'hard' ? 'warning' : (rule.severity || 'warning');
                var min = rule.min_value !== undefined ? rule.min_value : '?';
                var max = rule.max_value !== undefined ? rule.max_value : '?';

                var arrSegs = instance.arraySegments;
                var formId = segments[0];
                var formIndex = (arrSegs.length > 0 && arrSegs[0].key === segments[0]) ? arrSegs[0].index : 0;
                var vitalSet = segments.length > 1 ? segments[segments.length - 2] : null;
                var vitalIndex = (arrSegs.length > 1) ? arrSegs[arrSegs.length - 1].index : 0;

                var violation = {
                    rule_id: rule.id || ruleKey,
                    field_path: ruleKey,
                    field_name: fieldName,
                    field_label: rule.subject || toTitleCase(fieldName),
                    form_name: buildFormLabel(arrSegs, segments),
                    form_id: formId,
                    form_index: formIndex,
                    vital_set: vitalSet,
                    vital_index: vitalIndex,
                    message: 'Value ' + numVal + ' is outside the expected range ' + min + '\u2013' + max + '.',
                    severity: effectiveSeverity,
                    stored_value: numVal
                };

                if (effectiveSeverity === 'hard') {
                    errors.push(violation);
                } else if (effectiveSeverity === 'info') {
                    infos.push(violation);
                } else {
                    warnings.push(violation);
                }
            }
        }

        return { errors: errors, warnings: warnings, infos: infos };
    }

    // Run historical scan and update state. Called after case data is loaded.
    function runHistoricalScan() {
        if (!window.g_data || !window.mmria_validation_rules) {
            return;
        }
        var result = evaluateHistoricalVitals(window.g_data, window.mmria_validation_rules);
        updateValidationState({ errors: result.errors, warnings: result.warnings, infos: result.infos });
        refreshValidationErrorsButton();
    }

    // Refresh the button UI — called after state changes.
    function refreshValidationErrorsButton() {
        var areas = document.querySelectorAll('.validation-errors-button-area');
        for (var i = 0; i < areas.length; i++) {
            renderValidationButtonInto(areas[i]);
        }
    }

    function renderValidationButtonInto(container) {
        if (!container) { return; }

        var state = getValidationState();
        var editMode = isEditMode();
        var totalViolations = state.errors.length + state.warnings.length + (state.infos ? state.infos.length : 0);

        if (!editMode || totalViolations === 0) {
            container.innerHTML = '';
            container.style.display = 'none';
            return;
        }

        var label = buildButtonLabel(state.errors.length, state.warnings.length, state.infos ? state.infos.length : 0);
        container.innerHTML =
            '<button type="button" class="btn validation-errors-button" onclick="mmria_validation_show_panel()" title="View validation errors and warnings">' +
            label +
            '</button>';
        container.style.display = 'inline-block';
    }

    function buildButtonLabel(errorCount, warningCount, infoCount) {
        var parts = [];
        if (errorCount > 0) {
            parts.push(errorCount + (errorCount === 1 ? ' Error' : ' Errors'));
        }
        if (warningCount > 0) {
            parts.push(warningCount + (warningCount === 1 ? ' Warning' : ' Warnings'));
        }
        if (infoCount > 0) {
            parts.push(infoCount + ' Info');
        }
        return parts.join(' \u00b7 ');
    }

    // One-time event delegation for blur validation on all number inputs.
    // Uses focusout (bubbles) so it works even after partial DOM re-renders (e.g. g_add_grid_item).
    document.addEventListener('focusout', function(e) {
        var target = e.target;
        if (!target || !target.classList || !target.classList.contains('number')) { return; }
        if (typeof mmria_vitals_validate_field === 'function') {
            mmria_vitals_validate_field(target);
        }
    });

    // Expose public API on window
    window.mmria_validation_state = {
        initialize: initializeValidationState,
        get: getValidationState,
        update: updateValidationState,
        reset: resetValidationState,
        isEditMode: isEditMode,
        runHistoricalScan: runHistoricalScan,
        refreshButton: refreshValidationErrorsButton,
        buildButtonLabel: buildButtonLabel
    };

}(window));
