// validation-errors-modal.js
// Renders and manages the Validation Errors panel modal.
// Story 5.1: Validation Errors Panel

(function (window) {
    'use strict';

    // Show the validation errors panel modal.
    function showValidationPanel() {
        var state = window.mmria_validation_state.get();
        var errorCount = state.errors.length;
        var warningCount = state.warnings.length;
        var infoCount = state.infos ? state.infos.length : 0;

        // Remove any existing modal
        var existing = document.getElementById('validation-errors-modal');
        if (existing && existing.parentNode) { existing.parentNode.removeChild(existing); }
        var existingBd = document.getElementById('validation-errors-backdrop');
        if (existingBd && existingBd.parentNode) { existingBd.parentNode.removeChild(existingBd); }

        if (errorCount === 0 && warningCount === 0 && infoCount === 0) { return; }

        var headerLabel = window.mmria_validation_state.buildButtonLabel(errorCount, warningCount, infoCount);

        var errorsHtml = renderErrorsSection(state.errors);
        var warningsHtml = renderWarningsSection(state.warnings, state.infos || []);

        var modalHtml =
            '<div id="validation-errors-modal" class="modal fade" tabindex="-1" role="dialog"' +
            ' aria-modal="true" aria-labelledby="validation-errors-modal-title" style="z-index:1050; overflow-y:auto;">' +
            '<div class="modal-dialog modal-lg" role="document" style="max-width:800px;">' +
            '<div class="modal-content">' +
            '<div class="modal-header" style="background-color:#7b2d8e;color:white;padding:10px 15px;">' +
            '<h2 id="validation-errors-modal-title" class="modal-title" style="margin:0;font-weight:600;font-size:18px;">' +
            headerLabel +
            '</h2>' +
            '</div>' +
            '<div class="modal-body" style="padding:0;max-height:65vh;overflow-y:auto;">' +
            errorsHtml +
            warningsHtml +
            '</div>' +
            '<div class="modal-footer" style="padding:10px 15px;text-align:right;">' +
            '<button type="button" id="validation-errors-modal-close" class="btn btn-default" style="padding:6px 18px;">Close</button>' +
            '</div>' +
            '</div>' +
            '</div>' +
            '</div>' +
            '<div id="validation-errors-backdrop" class="modal-backdrop fade" style="z-index:1040;"></div>';

        document.body.insertAdjacentHTML('beforeend', modalHtml);

        var modal = document.getElementById('validation-errors-modal');
        var backdrop = document.getElementById('validation-errors-backdrop');

        setTimeout(function () {
            if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
            if (backdrop) { backdrop.classList.add('show'); }
            var closeBtn = document.getElementById('validation-errors-modal-close');
            if (closeBtn) { closeBtn.focus(); }
        }, 10);

        function closeModal() {
            if (modal) { modal.classList.remove('show'); modal.style.display = 'none'; }
            if (backdrop) { backdrop.classList.remove('show'); }
            setTimeout(function () {
                if (modal && modal.parentNode) { modal.parentNode.removeChild(modal); }
                if (backdrop && backdrop.parentNode) { backdrop.parentNode.removeChild(backdrop); }
            }, 150);
        }

        var closeBtn = document.getElementById('validation-errors-modal-close');
        if (closeBtn) { closeBtn.onclick = closeModal; }

        if (modal) {
            modal.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') { e.preventDefault(); closeModal(); }
            });
        }

        // Attach field navigation links
        attachFieldNavLinks(modal, closeModal);
    }

    function renderErrorsSection(errors) {
        if (!errors || errors.length === 0) { return ''; }

        var rows = errors.map(function (e) {
            return renderViolationRow(e, 'error');
        }).join('');

        return '<div class="validation-errors-section validation-errors-section--errors" style="background:#fff5f5;padding:12px 15px 4px;">' +
            '<h3 style="margin:0 0 8px;font-size:15px;font-weight:600;color:#c00;">' +
            '<span class="validation-error-badge" style="background:#c00;color:white;border-radius:4px;padding:1px 7px;margin-right:6px;" aria-label="' + errors.length + ' errors">' + errors.length + '</span>' +
            'Errors' +
            '</h3>' +
            '<table style="width:100%;border-collapse:collapse;" role="table" aria-label="Validation Errors">' +
            '<thead><tr>' +
            '<th style="width:28%;text-align:left;padding:4px 6px;font-size:12px;color:#666;">Form</th>' +
            '<th style="width:28%;text-align:left;padding:4px 6px;font-size:12px;color:#666;">Field</th>' +
            '<th style="text-align:left;padding:4px 6px;font-size:12px;color:#666;">Message</th>' +
            '</tr></thead>' +
            '<tbody>' + rows + '</tbody>' +
            '</table>' +
            '</div>';
    }

    function renderWarningsSection(warnings, infos) {
        var warningItems = (warnings || []).map(function(w) { return { v: w, type: 'warning' }; });
        var infoItems = (infos || []).map(function(w) { return { v: w, type: 'info' }; });
        var combined = warningItems.concat(infoItems);
        if (combined.length === 0) { return ''; }

        var rows = combined.map(function (item) {
            return renderViolationRow(item.v, item.type);
        }).join('');

        var sectionTitle = (warningItems.length > 0 && infoItems.length > 0) ? 'Warnings & Info'
            : warningItems.length > 0 ? 'Warnings' : 'Info';

        return '<div class="validation-errors-section validation-errors-section--warnings" style="background:#fffdf0;padding:12px 15px 4px;border-top:1px solid #eee;">' +
            '<h3 style="margin:0 0 8px;font-size:15px;font-weight:600;color:#8a6000;">' +
            '<span class="validation-warning-badge" style="background:#f0ad00;color:#333;border-radius:4px;padding:1px 7px;margin-right:6px;" aria-label="' + combined.length + ' ' + sectionTitle + '">' + combined.length + '</span>' +
            sectionTitle +
            '</h3>' +
            '<table style="width:100%;border-collapse:collapse;" role="table" aria-label="Validation Warnings and Info">' +
            '<thead><tr>' +
            '<th style="width:28%;text-align:left;padding:4px 6px;font-size:12px;color:#666;">Form</th>' +
            '<th style="width:28%;text-align:left;padding:4px 6px;font-size:12px;color:#666;">Field</th>' +
            '<th style="text-align:left;padding:4px 6px;font-size:12px;color:#666;">Message</th>' +
            '</tr></thead>' +
            '<tbody>' + rows + '</tbody>' +
            '</table>' +
            '</div>';
    }

    function renderViolationRow(v, type) {
        var iconHtml = type === 'error'
            ? '<span aria-hidden="true" style="color:#c00;font-size:16px;margin-right:4px;">&#9679;</span>'
            : type === 'info'
            ? '<span aria-hidden="true" style="color:#0077cc;font-size:16px;margin-right:4px;">&#9432;</span>'
            : '<span aria-hidden="true" style="color:#f0ad00;font-size:16px;margin-right:4px;">&#9888;</span>';

        var fieldLinkHtml =
            '<a href="javascript:void(0)"' +
            ' class="validation-field-link"' +
            ' style="text-decoration:underline;color:#0056b3;"' +
            ' data-field-path="' + escapeAttr(v.field_path) + '"' +
            ' data-form-id="' + escapeAttr(v.form_id) + '"' +
            ' data-form-index="' + (v.form_index || 0) + '"' +
            ' data-vital-set="' + escapeAttr(v.vital_set || '') + '"' +
            ' data-vital-index="' + (v.vital_index || 0) + '"' +
            '>' + escapeHtml(v.field_label) + '</a>';

        var rowStyle = type === 'error'
            ? 'background:#fff0f0;border-bottom:1px solid #f5c6c6;'
            : type === 'info'
            ? 'background:#f0f7ff;border-bottom:1px solid #c6d9f5;'
            : 'background:#fffdf0;border-bottom:1px solid #f0e5a0;';

        return '<tr style="' + rowStyle + '">' +
            '<td style="padding:6px;font-size:13px;vertical-align:top;">' + iconHtml + escapeHtml(v.form_name) + '</td>' +
            '<td style="padding:6px;font-size:13px;vertical-align:top;">' + fieldLinkHtml + '</td>' +
            '<td style="padding:6px;font-size:13px;vertical-align:top;color:#444;">' + escapeHtml(v.message) + '</td>' +
            '</tr>';
    }

    function attachFieldNavLinks(modal, closeModal) {
        if (!modal) { return; }
        var links = modal.querySelectorAll('.validation-field-link');
        for (var i = 0; i < links.length; i++) {
            (function (link) {
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    var fieldPath = link.getAttribute('data-field-path');
                    var formId = link.getAttribute('data-form-id');
                    var formIndex = parseInt(link.getAttribute('data-form-index') || '0', 10);
                    var vitalSet = link.getAttribute('data-vital-set');
                    var vitalIndex = parseInt(link.getAttribute('data-vital-index') || '0', 10);

                    closeModal();
                    setTimeout(function () {
                        if (window.mmria_validation_navigate) {
                            window.mmria_validation_navigate(fieldPath, formId, formIndex, vitalSet, vitalIndex);
                        }
                    }, 200);
                });
            }(links[i]));
        }
    }

    function escapeHtml(str) {
        if (!str) { return ''; }
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function escapeAttr(str) {
        if (!str) { return ''; }
        return String(str).replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    window.mmria_validation_show_panel = showValidationPanel;

}(window));
