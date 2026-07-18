/**
 * System offline status check module.
 * Provides functions to evaluate offline config dates and show appropriate modals.
 * All public functions are exposed on window for use by the initial page load
 * check (wired in _LayoutBase.cshtml) and by the polling module (Story 8.4).
 */

// Auto-logout countdown state.
var _autoLogoutCountdownInterval = null;

// Precision timeout handles for exact warn_date / offline_date triggers.
var _warnDateTimeout = null;
var _offlineDateTimeout = null;

// Last successfully fetched config — used as fallback when a status fetch fails.
// Avoids sign-out (or false-offline assumption) due to transient mmria-services downtime.
var _lastKnownConfig = null;

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
            // Re-check before signing out — admin may have delayed the release during the countdown.
            fetch('/api/system-offline/status', { credentials: 'same-origin' })
                .then(function (resp) { return resp.ok ? resp.json() : null; })
                .then(function (latestConfig) {
                    var result = latestConfig ? checkOfflineStatus(latestConfig) : { state: 'offline' };
                    if (result.state === 'offline') {
                        _proceedWithSignOut();
                    } else {
                        console.log('[OfflineCheck] Countdown expired but offline state no longer active; cancelling logout.');
                        _cancelOfflineLogout(result, latestConfig, null);
                    }
                })
                .catch(function () {
                    _handleFetchFailure(null);
                });
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
 * Re-checks offline status before acting — if the admin delayed or cancelled
 * the release the logout is cancelled and the updated state applied instead.
 * Falls back to _lastKnownConfig (or assumes online) if mmria-services is down.
 */
function mmria_offline_modal_ok_handler() {
    clearAutoLogoutTimer();
    var okBtn = document.getElementById('mmria-offline-modal-ok');
    if (okBtn) okBtn.disabled = true;

    fetch('/api/system-offline/status', { credentials: 'same-origin' })
        .then(function (resp) { return resp.ok ? resp.json() : null; })
        .then(function (latestConfig) {
            var result = latestConfig ? checkOfflineStatus(latestConfig) : { state: 'offline' };
            if (result.state !== 'offline') {
                console.log('[OfflineCheck] OK clicked but offline state no longer active; cancelling logout.');
                _cancelOfflineLogout(result, latestConfig, okBtn);
                return;
            }
            _proceedWithSignOut();
        })
        .catch(function () {
            _handleFetchFailure(okBtn);
        });
}

/**
 * Submits a logout POST form directly. No further re-check — used as the
 * terminal sign-out step so catch paths can't trigger recursive re-checks.
 */
function _doSignOut() {
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

/**
 * Optionally saves unsaved changes then calls _doSignOut.
 */
function _proceedWithSignOut() {
    if (window.hasUnsavedChanges && typeof window.mmria_save_before_signout === 'function') {
        try {
            var saveResult = window.mmria_save_before_signout();
            if (saveResult && typeof saveResult.then === 'function') {
                saveResult.then(_doSignOut).catch(_doSignOut);
                return;
            }
        } catch (e) { /* fall through */ }
    }
    _doSignOut();
}

/**
 * Cancels an in-progress offline logout: hides the modal, clears session gates,
 * and re-applies the latest config state (re-shows warn modal if date was pushed).
 * @param {Object} result     - Result from checkOfflineStatus.
 * @param {Object|null} config - Config to apply; no-op on state if null.
 * @param {Element|null} okButtonEl - OK button to re-enable, or null.
 */
function _cancelOfflineLogout(result, config, okButtonEl) {
    if (okButtonEl) okButtonEl.disabled = false;
    var modal = document.getElementById('mmria-offline-modal');
    var backdrop = document.getElementById('mmria-offline-modal-backdrop');
    if (modal) modal.style.display = 'none';
    if (backdrop) backdrop.style.display = 'none';
    localStorage.removeItem('offline_modal_shown');
    sessionStorage.removeItem('warn_modal_shown');
    if (config) {
        // Re-show warn modal if date was pushed to future; no-op if dates were cleared.
        handleOfflineState(result, config);
        scheduleOfflineCheckAtDates(config);
    }
}

/**
 * Handles a failed status fetch by falling back to _lastKnownConfig.
 * If last known state was offline: proceeds with sign-out.
 * If last known state was online (or no prior data): cancels the logout.
 * This prevents a mmria-services outage from incorrectly signing users out.
 * @param {Element|null} okButtonEl - OK button to re-enable if cancelling.
 */
function _handleFetchFailure(okButtonEl) {
    console.warn('[OfflineCheck] Status fetch failed; using last known config or assuming online.');
    var fb = _lastKnownConfig;
    var result = fb ? checkOfflineStatus(fb) : { state: 'normal' };
    if (result.state === 'offline') {
        _proceedWithSignOut();
    } else {
        _cancelOfflineLogout(result, fb, okButtonEl);
    }
}

/**
 * Stores the latest successfully fetched config and applies offline state.
 * Single entry point for all successful config-fetch paths so _lastKnownConfig
 * is always current.
 * @param {Object} config - Response from /api/system-offline/status.
 */
function runOfflineCheck(config) {
    _lastKnownConfig = config;
    var result = checkOfflineStatus(config);
    handleOfflineState(result, config);
    scheduleOfflineCheckAtDates(config);
}

/**
 * Schedules exact-time setTimeout callbacks for warn_date and offline_date so
 * the modal fires at the precise moment the threshold is crossed rather than
 * waiting for the next poll cycle.  Called after every config fetch (initial +
 * poll) so timeouts are refreshed whenever dates change server-side.
 *
 * @param {Object} config - Response from /api/system-offline/status.
 */
function scheduleOfflineCheckAtDates(config) {
    // Clear any existing precision timeouts before rescheduling.
    if (_warnDateTimeout !== null) { clearTimeout(_warnDateTimeout); _warnDateTimeout = null; }
    if (_offlineDateTimeout !== null) { clearTimeout(_offlineDateTimeout); _offlineDateTimeout = null; }

    if (!config) return;

    var now = Date.now();

    if (config.warn_date) {
        var warnMs = new Date(config.warn_date).getTime() - now;
        if (!isNaN(warnMs) && warnMs > 0) {
            console.log('[OfflineCheck] Scheduling precision warn trigger in', Math.round(warnMs / 1000), 's');
            _warnDateTimeout = setTimeout(function () {
                _warnDateTimeout = null;
                var result = checkOfflineStatus(config);
                handleOfflineState(result, config);
            }, warnMs);
        }
    }

    if (config.offline_date) {
        var offlineMs = new Date(config.offline_date).getTime() - now;
        if (!isNaN(offlineMs) && offlineMs > 0) {
            console.log('[OfflineCheck] Scheduling precision offline trigger in', Math.round(offlineMs / 1000), 's');
            _offlineDateTimeout = setTimeout(function () {
                _offlineDateTimeout = null;
                var result = checkOfflineStatus(config);
                handleOfflineState(result, config);
            }, offlineMs);
        }
    }
}

/**
 * Starts a periodic poll of /api/system-offline/status.
 * On each response, calls checkOfflineStatus → handleOfflineState using the same
 * gates as the initial page-load check, so modals are never shown twice.
 * Also reschedules precision timeouts so exact-time triggers stay current if
 * the admin changes the offline_date while users are logged in.
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
                runOfflineCheck(config);
            })
            .catch(function (err) {
                console.warn('Offline status poll failed — retaining last known state:', err);
            });
    }, ms);
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
window.scheduleOfflineCheckAtDates = scheduleOfflineCheckAtDates;
window.runOfflineCheck = runOfflineCheck;
