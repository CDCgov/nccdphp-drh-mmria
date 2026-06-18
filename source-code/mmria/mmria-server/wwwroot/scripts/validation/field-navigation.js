// field-navigation.js
// Navigates to a specific field in the case editor after clicking a validation error link.
// Story 5.1: Validation Errors Panel

(function (window) {
    'use strict';

    // Maximum number of 200ms polling attempts before giving up (20 × 200ms = 4s).
    var MAX_POLL_ATTEMPTS = 20;

    // Navigate to a field from the validation errors panel.
    // fieldPath: full path e.g. "er_visit_and_hospital_medical_records/vital_signs/temperature"
    // formId: top-level form name e.g. "er_visit_and_hospital_medical_records"
    // formIndex: 0-based index for repeating records
    // vitalSet: "vital_signs" or "transport_vital_signs"
    // vitalIndex: 0-based index within the vital set
    function navigateToField(fieldPath, formId, formIndex, vitalSet, vitalIndex) {
        if (!formId || !fieldPath) { return; }

        var fieldName = fieldPath.split('/').pop();

        // Use url_monitor to parse the current hash robustly.
        // Direct hash string parsing breaks when the hash lacks a leading slash
        // (e.g. "#0/er_visit.../0" produced by "View Record N" table links),
        // causing parseInt to fail and silently skipping navigation to the wrong record.
        var urlState = (typeof url_monitor !== 'undefined')
            ? url_monitor.get_url_state(window.location.href)
            : null;
        var caseId = urlState && urlState.path_array && urlState.path_array.length > 0
            ? urlState.path_array[0]
            : null;

        if (!caseId || isNaN(parseInt(caseId, 10))) {
            // Cannot navigate without a case ID — poll in the current DOM context.
            pollForFieldElement(fieldName, formId, formIndex, vitalSet, vitalIndex, 0);
            return;
        }

        var targetPathArray = [caseId, formId];
        if (typeof formIndex === 'number' && !isNaN(formIndex)) {
            targetPathArray.push(String(formIndex));
        }

        // Compare path arrays (normalised, slash-insensitive) to decide if navigation is needed.
        var currentPath = urlState.path_array;
        var needsNav = currentPath.length < targetPathArray.length ||
            currentPath.slice(0, targetPathArray.length).join('/') !== targetPathArray.join('/');

        if (needsNav) {
            // Set the hash with a leading slash — consistent with the form-selector navigation path.
            window.location.hash = '/' + targetPathArray.join('/');
            // Allow the hash-change handler (and any async save it triggers) to start,
            // then begin polling for the target element by its exact ID.
            setTimeout(function () {
                pollForFieldElement(fieldName, formId, formIndex, vitalSet, vitalIndex, 0);
            }, 300);
        } else {
            // Already on the correct form — poll immediately.
            pollForFieldElement(fieldName, formId, formIndex, vitalSet, vitalIndex, 0);
        }
    }

    // Build the exact element ID using the same algorithm as convert_object_path_to_jquery_id.
    // g_data.{formId}[{formIndex}].{vitalSet}[{vitalIndex}].{fieldName}
    //   => g_data_{formId}_{formIndex}__{vitalSet}_{vitalIndex}__{fieldName}
    function buildElementId(fieldName, formId, formIndex, vitalSet, vitalIndex) {
        if (!formId) { return null; }
        var idBase = vitalSet
            ? 'g_data_' + formId + '_' + formIndex + '__' + vitalSet + '_' + vitalIndex + '__' + fieldName
            : 'g_data_' + formId + '_' + formIndex + '__' + fieldName;
        return idBase;
    }

    // Poll every 200ms until the exact target element appears in the DOM (up to MAX_POLL_ATTEMPTS).
    // Using the exact element ID prevents false matches with identically-named inputs from other
    // ER visit records that may still be in the DOM while a hash-change render is in flight.
    function pollForFieldElement(fieldName, formId, formIndex, vitalSet, vitalIndex, attempt) {
        var idBase = buildElementId(fieldName, formId, formIndex, vitalSet, vitalIndex);
        var el = idBase
            ? (document.getElementById(idBase + '_control') || document.getElementById(idBase))
            : null;

        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            setTimeout(function () { el.focus(); }, 300);
            return;
        }

        if (attempt < MAX_POLL_ATTEMPTS) {
            setTimeout(function () {
                pollForFieldElement(fieldName, formId, formIndex, vitalSet, vitalIndex, attempt + 1);
            }, 200);
        } else {
            // Final fallback after polling expires: name-based search scoped to vitalIndex.
            // By this point the page has had up to 4s to render, so name-based matching
            // is unlikely to hit a stale/wrong-record element.
            var allByName = vitalSet
                ? document.querySelectorAll('input[name="' + fieldName + '"]')
                : null;
            var elFallback = null;
            if (allByName && allByName.length > 0) {
                elFallback = allByName.length > vitalIndex ? allByName[vitalIndex] : allByName[0];
            }
            if (!elFallback) {
                elFallback = document.querySelector('input[name="' + fieldName + '"]') ||
                             document.querySelector('input[id*="' + fieldName + '_control"]');
            }
            if (elFallback) {
                elFallback.scrollIntoView({ behavior: 'smooth', block: 'center' });
                setTimeout(function () { elFallback.focus(); }, 300);
            }
        }
    }

    window.mmria_validation_navigate = navigateToField;

}(window));
