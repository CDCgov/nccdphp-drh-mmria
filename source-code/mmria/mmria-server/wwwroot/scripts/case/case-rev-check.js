/**
 * Case stale-tab detection module.
 * Polls the open case revision and shows stale-case recovery UI.
 */

var _caseRevPollInterval = null;
var _caseRevPollIntervalMs = 45000; // normal: 45 s
var _caseRevPollGeneration = 0; // incremented on stop/restart; guards stale in-flight responses

function isCaseRevPollingAllowed() {
    if (typeof window.mmria_is_case_rev_polling_allowed !== 'function') return true;

    try {
        return window.mmria_is_case_rev_polling_allowed() === true;
    } catch (err) {
        console.warn('[CaseRevPoll] eligibility check failed:', err);
        return false;
    }
}

/**
 * Reloads the open case's data in-place via get_specific_case.
 * Falls back to a full page reload if the case page hook is not available.
 */
function mmria_do_case_reload() {
    if (typeof window.mmria_reload_case_data === 'function') {
        window.mmria_reload_case_data();
    } else {
        window.location.reload();
    }
}

/**
 * Shows the non-dismissable stale-case modal.
 * Called when a case save returns a 409 conflict.
 */
function showStaleCaseModal() {
    var existingModal = document.getElementById('mmria-stale-case-modal');
    if (existingModal && existingModal.parentNode) existingModal.parentNode.removeChild(existingModal);
    var existingBackdrop = document.getElementById('mmria-stale-case-modal-backdrop');
    if (existingBackdrop && existingBackdrop.parentNode) existingBackdrop.parentNode.removeChild(existingBackdrop);

    var html =
        '<div id="mmria-stale-case-modal" class="modal fade" tabindex="-1" role="alertdialog" aria-modal="true"' +
        '     aria-labelledby="mmria-stale-case-modal-title" aria-describedby="mmria-stale-case-modal-msg"' +
        '     style="z-index:1050;">' +
        '  <div class="modal-dialog" role="document">' +
        '    <div class="modal-content">' +
        '      <div class="modal-header" style="background-color:#7b2d8e; color:white; padding:7px;">' +
        '        <h4 id="mmria-stale-case-modal-title" class="modal-title"' +
        '            style="margin:0; font-weight:600; font-size:17px;">This case was updated</h4>' +
        '      </div>' +
        '      <div class="modal-body" style="padding:20px;">' +
        '        <p id="mmria-stale-case-modal-msg" style="font-size:16px; color:#333; margin:0;">' +
        '          This case was updated elsewhere. Reload to get the latest version before saving.' +
        '        </p>' +
        '      </div>' +
        '      <div class="modal-footer" style="padding:15px 20px; text-align:right;">' +
        '        <button type="button" id="mmria-stale-case-reload-btn" class="btn btn-primary"' +
        '                style="background-color:#7b2d8e; border-color:#7b2d8e; padding:8px 20px;">Reload Case</button>' +
        '      </div>' +
        '    </div>' +
        '  </div>' +
        '</div>' +
        '<div id="mmria-stale-case-modal-backdrop" class="modal-backdrop fade" style="z-index:1040;"></div>';

    document.body.insertAdjacentHTML('beforeend', html);

    var modal = document.getElementById('mmria-stale-case-modal');
    var backdrop = document.getElementById('mmria-stale-case-modal-backdrop');

    setTimeout(function () {
        if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
        if (backdrop) { backdrop.classList.add('show'); }
        var btn = document.getElementById('mmria-stale-case-reload-btn');
        if (btn) btn.focus();
    }, 10);

    var reloadBtn = document.getElementById('mmria-stale-case-reload-btn');
    if (reloadBtn) {
        reloadBtn.addEventListener('click', function () {
            if (modal && modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop && backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
            mmria_do_case_reload();
        });
    }
}

/**
 * Shows a stale-case modal when _rev polling detects that the open case
 * has been updated server-side. Has only a Reload button; autosave is paused
 * until reload.
 */
function showStaleCaseBanner() {
    if (document.getElementById('mmria-stale-case-banner')) return;

    if (typeof window.mmria_mark_case_stale === 'function') window.mmria_mark_case_stale();

    var html =
        '<div id="mmria-stale-case-banner" class="modal fade" tabindex="-1" role="alertdialog" aria-modal="true"' +
        '     aria-labelledby="mmria-stale-case-banner-title" aria-describedby="mmria-stale-case-banner-msg"' +
        '     style="z-index:1050;">' +
        '  <div class="modal-dialog" role="document">' +
        '    <div class="modal-content">' +
        '      <div class="modal-header" style="background-color:#7b2d8e; color:white; padding:7px;">' +
        '        <h4 id="mmria-stale-case-banner-title" class="modal-title"' +
        '            style="margin:0; font-weight:600; font-size:17px;">This case was updated</h4>' +
        '      </div>' +
        '      <div class="modal-body" style="padding:20px;">' +
        '        <p id="mmria-stale-case-banner-msg" style="font-size:16px; color:#333; margin:0;">' +
        '          This case has been updated. Reload to see the latest version.' +
        '        </p>' +
        '      </div>' +
        '      <div class="modal-footer" style="padding:15px 20px; text-align:right;">' +
        '        <button type="button" id="mmria-stale-case-banner-reload" class="btn btn-primary"' +
        '                style="background-color:#7b2d8e; border-color:#7b2d8e; padding:8px 20px;">Reload</button>' +
        '      </div>' +
        '    </div>' +
        '  </div>' +
        '</div>' +
        '<div id="mmria-stale-case-banner-backdrop" class="modal-backdrop fade" style="z-index:1040;"></div>';

    document.body.insertAdjacentHTML('beforeend', html);

    var modal = document.getElementById('mmria-stale-case-banner');
    var backdrop = document.getElementById('mmria-stale-case-banner-backdrop');

    function closeModal() {
        if (modal && modal.parentNode) modal.parentNode.removeChild(modal);
        if (backdrop && backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
    }

    setTimeout(function () {
        if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
        if (backdrop) { backdrop.classList.add('show'); }
        var reloadBtn = document.getElementById('mmria-stale-case-banner-reload');
        if (reloadBtn) reloadBtn.focus();
    }, 10);

    var reloadBtn = document.getElementById('mmria-stale-case-banner-reload');
    if (reloadBtn) {
        reloadBtn.addEventListener('click', function () {
            closeModal();
            mmria_do_case_reload();
        });
    }
}

/**
 * Stops the case _rev polling interval.
 * Also increments _caseRevPollGeneration so any in-flight fetch responses
 * from the old polling session are discarded when they resolve.
 */
function stopCaseRevPolling() {
    _caseRevPollGeneration++;
    if (_caseRevPollInterval !== null) {
        clearInterval(_caseRevPollInterval);
        _caseRevPollInterval = null;
    }
}

/**
 * Starts polling /api/case/{caseId}/rev every _caseRevPollIntervalMs milliseconds.
 * If the returned _rev differs from loadedRev, shows a stale-case modal.
 *
 * @param {string} caseId    - The CouchDB document ID of the open case.
 * @param {string} loadedRev - The _rev captured at case load time or last successful save.
 */
function startCaseRevPolling(caseId, loadedRev) {
    if (!caseId || !loadedRev) return;
    stopCaseRevPolling(); // increments _caseRevPollGeneration
    if (!isCaseRevPollingAllowed()) return;

    // Capture the generation at the time this polling session starts.
    // Any fetch response that resolves after stopCaseRevPolling() is called
    // (e.g. an in-flight response from a previous session that was overtaken
    // by a successful autosave updating _rev) will see a different generation
    // and bail out instead of showing a false-positive stale-case banner.
    var myGeneration = _caseRevPollGeneration;

    function poll() {
        if (!isCaseRevPollingAllowed()) {
            stopCaseRevPolling();
            return;
        }

        fetch('/api/case/' + encodeURIComponent(caseId) + '/rev', { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) return null;
                return response.json();
            })
            .then(function (data) {
                // Discard if this response belongs to a superseded polling session.
                // Guards against a late-arriving response from before mmria_sync_case_rev_polling()
                // restarted polling with a new _rev after a successful autosave. Without this check,
                // the stale response could incorrectly trigger the stale-case banner.
                if (_caseRevPollGeneration !== myGeneration) return;
                if (!data) return;
                if (data._rev && data._rev !== loadedRev) {
                    if (!isCaseRevPollingAllowed()) {
                        stopCaseRevPolling();
                        return;
                    }
                    showStaleCaseBanner();
                }
            })
            .catch(function (err) {
                console.warn('[CaseRevPoll] poll failed:', err);
            });
    }

    _caseRevPollInterval = setInterval(poll, _caseRevPollIntervalMs);
}

window.mmria_do_case_reload = mmria_do_case_reload;
window.showStaleCaseModal = showStaleCaseModal;
window.showStaleCaseBanner = showStaleCaseBanner;
window.stopCaseRevPolling = stopCaseRevPolling;
window.startCaseRevPolling = startCaseRevPolling;
