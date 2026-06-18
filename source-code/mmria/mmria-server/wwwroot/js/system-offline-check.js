/**
 * System offline status check module.
 * Provides functions to evaluate offline config dates and show appropriate modals.
 * All public functions are exposed on window for use by the initial page load
 * check (wired in _LayoutBase.cshtml) and by the polling module (Story 8.4).
 */

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
        return { state: 'normal' };
    }

    var now = Date.now();

    if (config.offline_date) {
        var offlineTime = new Date(config.offline_date).getTime();
        if (!isNaN(offlineTime) && now >= offlineTime) {
            return { state: 'offline' };
        }
    }

    if (config.warn_date) {
        var warnTime = new Date(config.warn_date).getTime();
        if (!isNaN(warnTime) && now >= warnTime) {
            return { state: 'warn' };
        }
    }

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
        if (sessionStorage.getItem('warn_modal_shown') !== '1') {
            showWarnModal(config.warn_message || 'The system will be going offline soon. Please save your work.');
            sessionStorage.setItem('warn_modal_shown', '1');
        }
    } else if (statusResult.state === 'offline') {
        if (localStorage.getItem('offline_modal_shown') !== '1') {
            showOfflineModal(config.offline_modal_message || 'The system is now offline. You will be signed out.');
            localStorage.setItem('offline_modal_shown', '1');
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
    modal.style.display = 'block';
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
 * Shows the non-dismissable going-offline modal with the given message.
 * @param {string} message
 */
function showOfflineModal(message) {
    var modal = document.getElementById('mmria-offline-modal');
    var backdrop = document.getElementById('mmria-offline-modal-backdrop');
    if (!modal) return;
    var msgEl = document.getElementById('mmria-offline-modal-message');
    if (msgEl) msgEl.textContent = message;
    if (backdrop) backdrop.style.display = 'block';
    modal.style.display = 'block';
    var okBtn = document.getElementById('mmria-offline-modal-ok');
    if (okBtn) setTimeout(function () { okBtn.focus(); }, 0);
}

/**
 * OK button handler for the going-offline modal.
 * Optionally calls window.mmria_save_before_signout() if unsaved changes exist,
 * then navigates to sign-out by submitting a POST form to /Account/Logout.
 */
function mmria_offline_modal_ok_handler() {
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

// Expose all public functions on window for external callers.
window.checkOfflineStatus = checkOfflineStatus;
window.handleOfflineState = handleOfflineState;
window.showWarnModal = showWarnModal;
window.closeWarnModal = closeWarnModal;
window.showOfflineModal = showOfflineModal;
window.mmria_offline_modal_ok_handler = mmria_offline_modal_ok_handler;
