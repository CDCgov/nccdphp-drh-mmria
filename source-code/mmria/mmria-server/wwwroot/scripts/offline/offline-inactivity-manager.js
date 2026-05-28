(function(root) {
    'use strict';

    const activityStorageKey = 'mmria_offline_last_activity_at';
    const activityDocumentEvents = ['keydown', 'pointerdown', 'input'];
    const activityWindowEvents = ['scroll'];
    const checkIntervalMs = 10000;
    const rawIdleTimeoutMinutes = Number(root.offline_session_timeout_config?.idle_timeout_minutes);
    const idleTimeoutMinutes = Number.isFinite(rawIdleTimeoutMinutes) && rawIdleTimeoutMinutes > 0
        ? rawIdleTimeoutMinutes
        : 30;
    const idleTimeoutMs = idleTimeoutMinutes * 60 * 1000;

    let monitoringInterval = null;
    let listenersAttached = false;
    let timeoutRedirectInProgress = false;

    function log(level, message, detail) {
        if (root.offlineLog && typeof root.offlineLog[level] === 'function') {
            if (typeof detail === 'undefined') {
                root.offlineLog[level]('OfflineInactivityManager', message);
            } else {
                root.offlineLog[level]('OfflineInactivityManager', message, detail);
            }
            return;
        }

        const consoleMethod = console[level] || console.log;
        if (typeof detail === 'undefined') {
            consoleMethod.call(console, `[OfflineInactivityManager] ${message}`);
        } else {
            consoleMethod.call(console, `[OfflineInactivityManager] ${message}`, detail);
        }
    }

    function isOfflineRoute() {
        return root.location.pathname.toLowerCase() === '/account/offlinelogin';
    }

    function isOfflineSessionActive() {
        try {
            return localStorage.getItem('is_offline') === 'true' &&
                localStorage.getItem('has_active_offline_session') === 'true' &&
                localStorage.getItem('process_offline_cases') !== 'true';
        } catch (_error) {
            return false;
        }
    }

    function getLastActivityTimestamp() {
        try {
            const raw = localStorage.getItem(activityStorageKey);
            const parsed = Number(raw);
            return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
        } catch (_error) {
            return null;
        }
    }

    function setLastActivityTimestamp(timestamp = Date.now()) {
        try {
            localStorage.setItem(activityStorageKey, String(timestamp));
        } catch (error) {
            log('warn', 'Unable to persist offline inactivity timestamp.', error);
        }
    }

    function attachActivityListeners() {
        if (listenersAttached) {
            return;
        }

        for (const eventName of activityDocumentEvents) {
            document.addEventListener(eventName, onActivityDetected, true);
        }

        for (const eventName of activityWindowEvents) {
            root.addEventListener(eventName, onActivityDetected, true);
        }

        listenersAttached = true;
    }

    function detachActivityListeners() {
        if (!listenersAttached) {
            return;
        }

        for (const eventName of activityDocumentEvents) {
            document.removeEventListener(eventName, onActivityDetected, true);
        }

        for (const eventName of activityWindowEvents) {
            root.removeEventListener(eventName, onActivityDetected, true);
        }

        listenersAttached = false;
    }

    function stopMonitoring() {
        if (monitoringInterval != null) {
            root.clearInterval(monitoringInterval);
            monitoringInterval = null;
        }

        detachActivityListeners();
    }

    function redirectToOfflineLogin() {
        const offlineLoginUrl = root.OfflineStatus &&
            typeof root.OfflineStatus.getOfflineLoginUrl === 'function'
            ? root.OfflineStatus.getOfflineLoginUrl()
            : '/Account/OfflineLogin';
        root.location.href = offlineLoginUrl;
    }

    async function encryptOfflineCasesAndDropKey() {
        try {
            if (!('serviceWorker' in navigator)) {
                return false;
            }

            const registration = await navigator.serviceWorker.ready;
            if (!registration || !registration.active) {
                return false;
            }

            return await new Promise(resolve => {
                const messageChannel = new MessageChannel();
                let settled = false;
                const timeoutId = root.setTimeout(() => {
                    if (settled) {
                        return;
                    }

                    settled = true;
                    resolve(false);
                }, 4000);

                messageChannel.port1.onmessage = function(event) {
                    if (settled) {
                        return;
                    }

                    settled = true;
                    root.clearTimeout(timeoutId);
                    resolve(event.data?.success === true);
                };

                registration.active.postMessage(
                    { type: 'OFFLINE_LOGOUT_ENCRYPT_CASES' },
                    [messageChannel.port2]
                );
            });
        } catch (error) {
            log('warn', 'Unable to encrypt offline cases before inactivity redirect.', error);
            return false;
        }
    }

    async function expireOfflineSessionAndRedirect(reason) {
        if (timeoutRedirectInProgress) {
            return;
        }

        timeoutRedirectInProgress = true;
        stopMonitoring();
        log('log', `Offline inactivity timeout triggered: ${reason}`);

        try {
            await encryptOfflineCasesAndDropKey();
        } catch (_error) {
        }

        try {
            localStorage.setItem('has_active_offline_session', 'false');
        } catch (_error) {
        }

        try {
            if (root.ServiceWorkerManager &&
                typeof root.ServiceWorkerManager.notifyActiveOfflineSessionChange === 'function') {
                root.ServiceWorkerManager.notifyActiveOfflineSessionChange();
            }
        } catch (error) {
            log('warn', 'Unable to notify service worker about offline inactivity timeout.', error);
        }

        redirectToOfflineLogin();
    }

    function onActivityDetected(event) {
        if (!isOfflineSessionActive() || timeoutRedirectInProgress) {
            return;
        }

        const target = event && event.target ? event.target : null;
        if (target && typeof target.closest === 'function' && target.closest('#offline_login_button')) {
            return;
        }

        setLastActivityTimestamp();
    }

    function checkInactivity() {
        if (!isOfflineSessionActive()) {
            stopMonitoring();
            return false;
        }

        const lastActivityTimestamp = getLastActivityTimestamp();
        if (lastActivityTimestamp == null) {
            setLastActivityTimestamp();
            return false;
        }

        if ((Date.now() - lastActivityTimestamp) >= idleTimeoutMs) {
            expireOfflineSessionAndRedirect('idle_timeout');
            return true;
        }

        return false;
    }

    function initialize() {
        if (isOfflineRoute()) {
            stopMonitoring();
            return;
        }

        if (!isOfflineSessionActive()) {
            stopMonitoring();
            return;
        }

        if (checkInactivity()) {
            return;
        }

        attachActivityListeners();

        if (getLastActivityTimestamp() == null) {
            setLastActivityTimestamp();
        }

        if (monitoringInterval == null) {
            monitoringInterval = root.setInterval(checkInactivity, checkIntervalMs);
        }
    }

    function handleStorageChange(event) {
        if (!event || !event.key) {
            return;
        }

        if (event.key === 'has_active_offline_session') {
            if (event.newValue === 'false' && localStorage.getItem('is_offline') === 'true' && !isOfflineRoute()) {
                stopMonitoring();
                redirectToOfflineLogin();
                return;
            }

            if (event.newValue === 'true') {
                timeoutRedirectInProgress = false;
                initialize();
            }
        }

        if (event.key === 'is_offline' || event.key === 'process_offline_cases') {
            if (isOfflineSessionActive()) {
                timeoutRedirectInProgress = false;
                initialize();
            } else {
                stopMonitoring();
            }
        }
    }

    root.OfflineInactivityManager = {
        initialize: initialize,
        stop: stopMonitoring,
        refreshActivity: setLastActivityTimestamp,
        expireSession: expireOfflineSessionAndRedirect,
        getLastActivityTimestamp: getLastActivityTimestamp
    };

    root.addEventListener('storage', handleStorageChange);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
})(window);
