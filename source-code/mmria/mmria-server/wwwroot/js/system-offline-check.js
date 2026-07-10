/**
 * System offline status check module.
 * Provides functions to evaluate offline config dates and show appropriate modals.
 * All public functions are exposed on window for use by the initial page load
 * check (wired in _LayoutBase.cshtml) and by the polling module (Story 8.4).
 */

// Auto-logout countdown state.
var _autoLogoutCountdownInterval = null;

/**
 * Evaluates the offline config dates against the current time.
 *
 * @param {Object|null} config - Response body from /api/system-offline/status.
 * @param {string|null|undefined} config.warn_date    - ISO date string for warning threshold.
 * @param {string|null|undefined} config.offline_date - ISO date string for offline threshold.
 * @returns {{ state: "normal"|"warn"|"offline" }}
 */
function checkOfflineStatus(config) {
    if (!config || (!config.warn_date && !config.offline_date)) {
        console.log('[OfflineCheck] No dates configured — state: normal');
        return { state: 'normal' };
    }

    var now = Date.now();

    if (config.offline_date) {
        var offlineTime = new Date(config.offline_date).getTime();
        console.log('[OfflineCheck] offline_date raw:', config.offline_date, '→ parsed ms:', offlineTime, '| now:', now, '| past?', now >= offlineTime);
        if (!isNaN(offlineTime) && now >= offlineTime) {
            console.log('[OfflineCheck] state: offline');
            return { state: 'offline' };
        }
    }

    if (config.warn_date) {
        var warnTime = new Date(config.warn_date).getTime();
        console.log('[OfflineCheck] warn_date raw:', config.warn_date, '→ parsed ms:', warnTime, '| now:', now, '| past?', now >= warnTime);
        if (!isNaN(warnTime) && now >= warnTime) {
            console.log('[OfflineCheck] state: warn');
            return { state: 'warn' };
        }
    }

    console.log('[OfflineCheck] state: normal (dates in future)');
    return { state: 'normal' };
}

/**
 * Handles the offline state by showing the appropriate modal once per session/login.
 *
 * Gate for "warn"    : sessionStorage["warn_modal_shown"]   === "1"  (tab-lifetime)
 * Gate for "offline" : localStorage["offline_modal_shown"]  === "1"  (cleared on next login)
 *
 * @param {{ state: "normal"|"warn"|"offline" }} statusResult - Result from checkOfflineStatus.
 * @param {Object} config - The full config object (used for message text).
 */
function handleOfflineState(statusResult, config) {
    if (!statusResult) return;

    if (statusResult.state === 'warn') {
        var shownForDate = sessionStorage.getItem('warn_modal_shown');
        console.log('[OfflineCheck] handleOfflineState: state=warn | warn_modal_shown sessionStorage gate =', shownForDate, '| current warn_date =', config.warn_date);
        if (shownForDate !== config.warn_date) {
            showWarnModal(config.warn_message || 'The system will be going offline soon. Please save your work.');
            sessionStorage.setItem('warn_modal_shown', config.warn_date);
        }
    } else if (statusResult.state === 'offline') {
        var shownForOfflineDate = localStorage.getItem('offline_modal_shown');
        console.log('[OfflineCheck] handleOfflineState: state=offline | offline_modal_shown localStorage gate =', shownForOfflineDate, '| current offline_date =', config.offline_date);
        if (shownForOfflineDate !== config.offline_date) {
            showOfflineModal(
                config.offline_modal_message || 'The system is now offline. You will be signed out.',
                config.auto_logout_minutes
            );
            localStorage.setItem('offline_modal_shown', config.offline_date);
        }
    }
}

/**
 * Shows the dismissable warning modal with the given message.
 * @param {string} message
 */
function showWarnModal(message) {
    var modal = document.getElementById('mmria-warn-modal');
    var backdrop = document.getElementById('mmria-warn-modal-backdrop');
    if (!modal) return;
    var msgEl = document.getElementById('mmria-warn-modal-message');
    if (msgEl) msgEl.textContent = message;
    if (backdrop) backdrop.style.display = 'block';
    modal.style.display = 'flex';
    modal.style.alignItems = 'center';
    modal.style.justifyContent = 'center';
    var closeBtn = modal.querySelector('.mmria-warn-close-btn');
    if (closeBtn) setTimeout(function () { closeBtn.focus(); }, 0);
}

/**
 * Closes the warning modal.
 */
function closeWarnModal() {
    var modal = document.getElementById('mmria-warn-modal');
    var backdrop = document.getElementById('mmria-warn-modal-backdrop');
    if (modal) modal.style.display = 'none';
    if (backdrop) backdrop.style.display = 'none';
}

/**
 * Shows the non-dismissable going-offline modal with the given message and
 * starts an auto-logout countdown. When the countdown reaches zero, or when
 * the user clicks OK, any unsaved case changes are saved before signing out.
 *
 * @param {string} message
 * @param {number} [autoLogoutMinutes] - Minutes before automatic sign-out (default 5).
 */
function showOfflineModal(message, autoLogoutMinutes) {
    var modal = document.getElementById('mmria-offline-modal');
    var backdrop = document.getElementById('mmria-offline-modal-backdrop');
    if (!modal) return;
    var msgEl = document.getElementById('mmria-offline-modal-message');
    if (msgEl) msgEl.textContent = message;
    if (backdrop) backdrop.style.display = 'block';
    modal.style.display = 'flex';
    modal.style.alignItems = 'center';
    modal.style.justifyContent = 'center';
    var okBtn = document.getElementById('mmria-offline-modal-ok');
    if (okBtn) setTimeout(function () { okBtn.focus(); }, 0);

    // Start auto-logout countdown.
    clearAutoLogoutTimer();
    var minutes = (typeof autoLogoutMinutes === 'number' && autoLogoutMinutes > 0) ? autoLogoutMinutes : 5;
    var endTime = Date.now() + minutes * 60 * 1000;
    var countdownEl = document.getElementById('mmria-offline-modal-countdown');

    function tick() {
        var remaining = Math.max(0, endTime - Date.now());
        var totalSecs = Math.ceil(remaining / 1000);
        var m = Math.floor(totalSecs / 60);
        var s = totalSecs % 60;
        if (countdownEl) {
            countdownEl.textContent = 'Automatically signing out in ' + m + ':' + (s < 10 ? '0' : '') + s + '.';
        }
        if (remaining <= 0) {
            clearAutoLogoutTimer();
            mmria_offline_modal_ok_handler();
        }
    }

    tick();
    _autoLogoutCountdownInterval = setInterval(tick, 1000);
}

/**
 * Clears the auto-logout countdown interval and resets the countdown display.
 */
function clearAutoLogoutTimer() {
    if (_autoLogoutCountdownInterval !== null) {
        clearInterval(_autoLogoutCountdownInterval);
        _autoLogoutCountdownInterval = null;
    }
    var countdownEl = document.getElementById('mmria-offline-modal-countdown');
    if (countdownEl) countdownEl.textContent = '';
}

/**
 * OK button handler for the going-offline modal.
 * Optionally calls window.mmria_save_before_signout() if unsaved changes exist,
 * then navigates to sign-out by submitting a POST form to /Account/Logout.
 */
function mmria_offline_modal_ok_handler() {
    clearAutoLogoutTimer();
    var okBtn = document.getElementById('mmria-offline-modal-ok');
    if (okBtn) okBtn.disabled = true;

    function doSignOut() {
        var token = '';
        var tokenMeta = document.querySelector('meta[name="request-verification-token"]');
        if (tokenMeta) token = tokenMeta.getAttribute('content') || '';

        var form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Account/Logout';

        if (token) {
            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = '__RequestVerificationToken';
            input.value = token;
            form.appendChild(input);
        }

        document.body.appendChild(form);
        form.submit();
    }

    // Check for unsaved changes and an optional save hook registered by the page.
    if (window.hasUnsavedChanges && typeof window.mmria_save_before_signout === 'function') {
        try {
            var saveResult = window.mmria_save_before_signout();
            if (saveResult && typeof saveResult.then === 'function') {
                saveResult.then(doSignOut).catch(doSignOut);
                return;
            }
        } catch (e) {
            // Fall through to immediate sign-out on error.
        }
    }

    doSignOut();
}

/**
 * Starts a periodic poll of /api/system-offline/status.
 * On each response, calls checkOfflineStatus → handleOfflineState using the same
 * gates as the initial page-load check, so modals are never shown twice.
 *
 * @param {number} intervalMs - Poll interval in milliseconds. Default: 120000 (2 min).
 * @returns {number} The setInterval ID (can be passed to clearInterval if needed).
 */
function startOfflineStatusPolling(intervalMs) {
    var ms = (typeof intervalMs === 'number' && intervalMs > 0) ? intervalMs : 120000;
    return setInterval(function () {
        fetch('/api/system-offline/status', { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) return null;
                return response.json();
            })
            .then(function (config) {
                if (!config) return;
                var result = checkOfflineStatus(config);
                handleOfflineState(result, config);
            })
            .catch(function (err) {
                console.warn('Offline status poll failed:', err);
            });
    }, ms);
}

// ── Case Stale Detection (Story 12.4) ────────────────────────────────────────

var _caseRevPollInterval = null;
var _caseRevPollIntervalMs = 45000; // normal: 45 s
var _caseRevFastIntervalMs = 10000; // migration window: 10 s

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
 * Dynamically injected following the same Bootstrap fade/show pattern as
 * mmria_vitals_show_print_gate_modal in chart.js.
 * Called when a case save returns a 409 conflict (case updated elsewhere).
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
 * Shows a stale-case modal (Bootstrap fade pattern) when _rev polling detects the case
 * has been updated server-side. Has only a Reload button — autosave is paused until reload.
 */
function showStaleCaseBanner() {
    // Only one instance at a time
    if (document.getElementById('mmria-stale-case-banner')) return;

    // Notify case/index.js to pause autosave
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
 */
function stopCaseRevPolling() {
    if (_caseRevPollInterval !== null) {
        clearInterval(_caseRevPollInterval);
        _caseRevPollInterval = null;
    }
}

/**
 * Starts polling /api/case/{caseId}/rev every _caseRevPollIntervalMs milliseconds.
 * If the returned _rev differs from loadedRev, shows a dismissable stale banner.
 * Reduces poll interval to 10 s when the X-Offline-Date header signals a migration window.
 *
 * @param {string} caseId    - The CouchDB document ID of the open case.
 * @param {string} loadedRev - The _rev captured at case load time (or last successful save).
 */
function startCaseRevPolling(caseId, loadedRev) {
    if (!caseId || !loadedRev) return;
    stopCaseRevPolling();

    function poll() {
        fetch('/api/case/' + encodeURIComponent(caseId) + '/rev', { credentials: 'same-origin' })
            .then(function (response) {
                // Check X-Offline-Date to accelerate polling during migration window (AC-3)
                var offlineDateHeader = response.headers.get('X-Offline-Date');
                if (offlineDateHeader) {
                    var offlineMs = new Date(offlineDateHeader).getTime();
                    if (!isNaN(offlineMs) && Date.now() > offlineMs) {
                        if (_caseRevPollIntervalMs !== _caseRevFastIntervalMs) {
                            _caseRevPollIntervalMs = _caseRevFastIntervalMs;
                            stopCaseRevPolling();
                            _caseRevPollInterval = setInterval(poll, _caseRevPollIntervalMs);
                        }
                    }
                }
                if (!response.ok) return null;
                return response.json();
            })
            .then(function (data) {
                if (!data) return;
                if (data._rev && data._rev !== loadedRev) {
                    showStaleCaseBanner();
                }
            })
            .catch(function (err) {
                console.warn('[CaseRevPoll] poll failed:', err);
            });
    }

    _caseRevPollInterval = setInterval(poll, _caseRevPollIntervalMs);
}

// Expose all public functions on window for external callers.
window.checkOfflineStatus = checkOfflineStatus;
window.handleOfflineState = handleOfflineState;
window.showWarnModal = showWarnModal;
window.closeWarnModal = closeWarnModal;
window.showOfflineModal = showOfflineModal;
window.clearAutoLogoutTimer = clearAutoLogoutTimer;
window.mmria_offline_modal_ok_handler = mmria_offline_modal_ok_handler;
window.startOfflineStatusPolling = startOfflineStatusPolling;
window.showStaleCaseModal = showStaleCaseModal;
window.showStaleCaseBanner = showStaleCaseBanner;
window.stopCaseRevPolling = stopCaseRevPolling;
window.startCaseRevPolling = startCaseRevPolling;
