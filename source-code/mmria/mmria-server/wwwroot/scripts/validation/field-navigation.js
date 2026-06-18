// field-navigation.js
// Navigates to a specific field in the case editor after clicking a validation error link.
// Story 5.1: Validation Errors Panel

(function (window) {
    'use strict';

    // Navigate to a field from the validation errors panel.
    // fieldPath: full path e.g. "er_visit_and_hospital_medical_records/vital_signs/temperature"
    // formId: top-level form name e.g. "er_visit_and_hospital_medical_records"
    // formIndex: 0-based index for repeating records
    // vitalSet: "vital_signs" or "transport_vital_signs"
    // vitalIndex: 0-based index within the vital set
    function navigateToField(fieldPath, formId, formIndex, vitalSet, vitalIndex) {
        if (!formId || !fieldPath) { return; }

        var fieldName = fieldPath.split('/').pop();

        // Build the hash URL to navigate to the form and record
        var currentHash = window.location.hash || '';
        var pathParts = currentHash.replace('#/', '').split('/');
        var caseId = pathParts[0];

        if (!caseId || isNaN(parseInt(caseId))) {
            // Cannot navigate without a case ID in the URL
            scrollToFieldById(fieldName, formId, formIndex, vitalSet, vitalIndex);
            return;
        }

        var targetHash;
        if (typeof formIndex === 'number' && !isNaN(formIndex)) {
            // Repeating record — include record index
            targetHash = '#/' + caseId + '/' + formId + '/' + formIndex;
        } else {
            targetHash = '#/' + caseId + '/' + formId;
        }

        if (window.location.hash !== targetHash) {
            window.location.hash = targetHash;
            // Wait for form to render, then scroll
            setTimeout(function () {
                scrollToFieldById(fieldName, formId, formIndex, vitalSet, vitalIndex);
            }, 600);
        } else {
            scrollToFieldById(fieldName, formId, formIndex, vitalSet, vitalIndex);
        }
    }

    // Scroll to and focus the field element matching fieldName within the given record context.
    function scrollToFieldById(fieldName, formId, formIndex, vitalSet, vitalIndex) {
        var el = null;

        // Strategy 1: construct the exact element id using the same formula as convert_object_path_to_jquery_id.
        // g_data.{formId}[{formIndex}].{vitalSet}[{vitalIndex}].{fieldName} → replace . [ ] with _
        // e.g. g_data_other_medical_office_visits_0__vital_signs_1__pulse_control
        if (formId) {
            var idBase;
            if (vitalSet) {
                idBase = 'g_data_' + formId + '_' + formIndex + '__' + vitalSet + '_' + vitalIndex + '__' + fieldName;
            } else {
                idBase = 'g_data_' + formId + '_' + formIndex + '__' + fieldName;
            }
            el = document.getElementById(idBase + '_control') ||
                 document.getElementById(idBase);
        }

        // Strategy 2: fall back to name-based search within the same vital set index
        if (!el && vitalSet) {
            var allByName = document.querySelectorAll('input[name="' + fieldName + '"]');
            if (allByName.length > vitalIndex) {
                el = allByName[vitalIndex];
            } else if (allByName.length > 0) {
                el = allByName[allByName.length - 1];
            }
        }

        // Strategy 3: first input with matching name
        if (!el) {
            el = document.querySelector('input[name="' + fieldName + '"]') ||
                 document.querySelector('input[id*="' + fieldName + '_control"]');
        }

        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            setTimeout(function () { el.focus(); }, 300);
        } else {
            var content = document.getElementById('form_content_id');
            if (content) { content.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
        }
    }

    window.mmria_validation_navigate = navigateToField;

}(window));
