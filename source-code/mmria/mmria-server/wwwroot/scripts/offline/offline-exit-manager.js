/**
 * Offline Exit Manager
 * Reusable "Exit Offline Mode" widget and best-effort cleanup flow.
 */
(function () {
    'use strict';

    const PENDING_STORAGE_KEY = 'mmria_exit_offline_cleanup_pending';
    const PENDING_COOKIE_NAME = 'mmria_offline_exit_pending';
    const EXIT_WIDGET_HOST_SELECTOR = '[data-offline-exit-host]';
    let g_exit_offline_in_progress = false;
    let g_pending_cleanup_in_progress = false;

    function getOfflineLoggerMethod(methodName) {
        if (typeof window !== 'undefined' && window.offlineLog && typeof window.offlineLog[methodName] === 'function') {
            return window.offlineLog[methodName].bind(window.offlineLog);
        }

        const consoleMethod = typeof console !== 'undefined' && typeof console[methodName] === 'function'
            ? console[methodName].bind(console)
            : console.log.bind(console);

        return function (_componentName, message, ...args) {
            consoleMethod(`[OfflineExitManager] ${message}`, ...args);
        };
    }

    const logInfo = getOfflineLoggerMethod('log');
    const logWarn = getOfflineLoggerMethod('warn');
    const logError = getOfflineLoggerMethod('error');

    function setPendingCleanupCookie(enabled) {
        let cookieValue = `${PENDING_COOKIE_NAME}=${enabled ? 'true' : ''}; path=/; SameSite=Lax`;
        if (window.location && window.location.protocol === 'https:') {
            cookieValue += '; Secure';
        }

        if (!enabled) {
            cookieValue += '; expires=Thu, 01 Jan 1970 00:00:00 GMT';
        }

        document.cookie = cookieValue;
    }

    function readStoredOfflineSession() {
        const rawValue = localStorage.getItem('mmria_offline_session');
        if (!rawValue) {
            return null;
        }

        try {
            return JSON.parse(rawValue);
        } catch (error) {
            logWarn('OfflineExitManager', 'Unable to parse mmria_offline_session. Proceeding with minimal cleanup context.', error);
            return null;
        }
    }

    function normalizeCaseIds(caseIds) {
        if (!Array.isArray(caseIds)) {
            return [];
        }

        return caseIds
            .map(caseId => (caseId == null ? '' : String(caseId).trim()))
            .filter(caseId => caseId.length > 0);
    }

    function buildPendingCleanupPayload() {
        const sessionData = readStoredOfflineSession() || {};
        const offlineSessionId =
            localStorage.getItem('offline_session_id') ||
            sessionData.sessionId ||
            sessionData.offlineSessionId ||
            '';

        const caseIds = normalizeCaseIds(sessionData.offlineIds || sessionData.offline_ids || []);

        if (!offlineSessionId && caseIds.length === 0) {
            return {
                offlineSessionId: '',
                caseIds: [],
                createdAt: new Date().toISOString()
            };
        }

        return {
            offlineSessionId: offlineSessionId,
            caseIds: caseIds,
            createdAt: new Date().toISOString()
        };
    }

    function getPendingCleanupPayload() {
        const rawValue = localStorage.getItem(PENDING_STORAGE_KEY);
        if (!rawValue) {
            return null;
        }

        try {
            const payload = JSON.parse(rawValue);
            return {
                offlineSessionId: payload && payload.offlineSessionId ? String(payload.offlineSessionId) : '',
                caseIds: normalizeCaseIds(payload && payload.caseIds),
                createdAt: payload && payload.createdAt ? payload.createdAt : null
            };
        } catch (error) {
            logWarn('OfflineExitManager', 'Unable to parse pending offline cleanup payload. Clearing invalid data.', error);
            localStorage.removeItem(PENDING_STORAGE_KEY);
            return null;
        }
    }

    function hasPendingCleanup() {
        return !!getPendingCleanupPayload();
    }

    function clearPendingCleanup() {
        localStorage.removeItem(PENDING_STORAGE_KEY);
        setPendingCleanupCookie(false);
    }

    function persistPendingCleanup(payload) {
        if (!payload) {
            return;
        }

        localStorage.setItem(PENDING_STORAGE_KEY, JSON.stringify(payload));
        setPendingCleanupCookie(true);
    }

    function isOfflineModeActive() {
        return !!(window.OfflineStatus && typeof window.OfflineStatus.isOffline === 'function' && window.OfflineStatus.isOffline());
    }

    function isOfflineModeServerSession() {
        return !!(
            window.OfflineStatus &&
            typeof window.OfflineStatus.isOfflineModeServerSession === 'function' &&
            window.OfflineStatus.isOfflineModeServerSession()
        );
    }

    function isProcessingOfflineCases() {
        return !!(
            window.OfflineStatus &&
            typeof window.OfflineStatus.isProcessingOfflineCases === 'function' &&
            window.OfflineStatus.isProcessingOfflineCases()
        );
    }

    function shouldShowExitWidget() {
        return isOfflineModeActive() || isProcessingOfflineCases() || isOfflineModeServerSession();
    }

    function canUseFullOfflineCasePersistence() {
        return !!(
            window.OfflineSyncManager &&
            typeof window.OfflineSyncManager.saveCasesToDatabase === 'function' &&
            typeof get_case_for_processing === 'function' &&
            window.OfflineChangeTracker &&
            typeof window.OfflineChangeTracker.getAll === 'function'
        );
    }

    async function bestEffortPersistOfflineCases() {
        if (!canUseFullOfflineCasePersistence()) {
            logInfo('OfflineExitManager', 'Skipping cached-case persistence because the full offline case stack is not available on this page.');
            return false;
        }

        try {
            await window.OfflineSyncManager.saveCasesToDatabase();
            logInfo('OfflineExitManager', 'Best-effort offline case persistence completed.');
            return true;
        } catch (error) {
            logWarn('OfflineExitManager', 'Best-effort offline case persistence failed. Continuing exit flow.', error);
            return false;
        }
    }

    async function bestEffortSyncLogs() {
        if (!window.offlineLog || typeof window.offlineLog.syncToServer !== 'function') {
            return false;
        }

        try {
            const syncResult = await window.offlineLog.syncToServer({ keepalive: true });
            if (syncResult && syncResult.success) {
                logInfo('OfflineExitManager', `Synced ${syncResult.synced} offline log entries.`);
                return true;
            }

            logWarn('OfflineExitManager', 'Offline log sync did not complete successfully.', syncResult);
            return false;
        } catch (error) {
            logWarn('OfflineExitManager', 'Offline log sync failed. Continuing exit flow.', error);
            return false;
        }
    }

    async function postJson(url, payload) {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            let errorText = `Request failed: ${response.status} ${response.statusText}`;

            try {
                const errorPayload = await response.json();
                errorText =
                    (errorPayload && (errorPayload.details || errorPayload.error_description || errorPayload.error || errorPayload.message)) ||
                    errorText;
            } catch (_error) {
                // ignore parse failure and keep fallback text
            }

            throw new Error(errorText);
        }

        try {
            return await response.json();
        } catch (_error) {
            return {};
        }
    }

    async function bestEffortUpdateOfflineState(payload) {
        if (!payload || !payload.offlineSessionId) {
            return false;
        }

        try {
            await postJson('/api/OfflineCase/update-offline-state', {
                offlineSessionId: payload.offlineSessionId,
                offlineState: 2
            });
            logInfo('OfflineExitManager', 'Marked offline session as completed.', payload.offlineSessionId);
            return true;
        } catch (error) {
            logWarn('OfflineExitManager', 'Unable to mark offline session as completed. Will retry later if possible.', error);
            return false;
        }
    }

    async function bestEffortReleaseCaseLocks(payload) {
        if (!payload || !payload.offlineSessionId || !Array.isArray(payload.caseIds) || payload.caseIds.length === 0) {
            return false;
        }

        try {
            await postJson('/api/OfflineCase/release-case-locks', {
                offlineSessionId: payload.offlineSessionId,
                caseIds: payload.caseIds
            });
            logInfo('OfflineExitManager', `Released ${payload.caseIds.length} offline case lock(s).`);
            return true;
        } catch (error) {
            logWarn('OfflineExitManager', 'Unable to release offline case locks. Will retry later if possible.', error);
            return false;
        }
    }

    async function bestEffortLocalCleanup() {
        try {
            if (
                window.OfflineTransitionManager &&
                typeof window.OfflineTransitionManager.unregisterServiceWorker === 'function'
            ) {
                await window.OfflineTransitionManager.unregisterServiceWorker();
            } else if (
                window.OfflineTransitionManager &&
                typeof window.OfflineTransitionManager.unregister_service_worker === 'function'
            ) {
                await window.OfflineTransitionManager.unregister_service_worker();
            }
        } catch (error) {
            logWarn('OfflineExitManager', 'Service worker cleanup failed during exit. Continuing anyway.', error);
        }

        try {
            if (
                window.OfflineTransitionManager &&
                typeof window.OfflineTransitionManager.clearAllCachedData === 'function'
            ) {
                await window.OfflineTransitionManager.clearAllCachedData();
            } else if (
                window.OfflineTransitionManager &&
                typeof window.OfflineTransitionManager.clear_all_cached_data === 'function'
            ) {
                await window.OfflineTransitionManager.clear_all_cached_data();
            } else {
                localStorage.removeItem('is_offline');
                localStorage.removeItem('process_offline_cases');
                localStorage.removeItem('offline_session_id');
                localStorage.removeItem('mmria_offline_session');
            }
        } catch (error) {
            logWarn('OfflineExitManager', 'Offline cache cleanup failed during exit. Continuing anyway.', error);
        }

        try {
            document.body.classList.remove('mmria-offline-mode');
            window.dispatchEvent(new Event('offlineStatusChanged'));
        } catch (_error) {
            // best-effort only
        }
    }

    function renderWidgetMarkup() {
        return `
            <div class="mmria-offline-exit-widget" style="display: inline-flex; align-items: center; gap: 16px; padding: 6px 0;">
                <div style="display: inline-flex; align-items: center; color: #712177; font-weight: 600; font-size: 18px;">
                    <img src="/img/offline-info.svg" alt="" aria-hidden="true" style="width: 18px; height: 18px; margin-right: 8px;">
                    <span>You're Offline</span>
                </div>
                <button
                    type="button"
                    data-action="show-exit-offline-mode"
                    class="btn"
                    style="background-color: #e3d3e4; color: #712177; font-weight: 600; border: 1px solid #e3d3e4; padding: 8px 18px; display: inline-flex; align-items: center; border-radius: 4px;"
                >
                    <span aria-hidden="true" style="font-size: 16px; line-height: 1; margin-right: 8px;">&rarr;</span>
                    <span>Exit Offline Mode</span>
                </button>
            </div>
        `;
    }

    function updateWidgetVisibility() {
        const shouldShow = shouldShowExitWidget();
        const hosts = document.querySelectorAll(EXIT_WIDGET_HOST_SELECTOR);

        hosts.forEach(host => {
            host.style.display = shouldShow ? 'block' : 'none';
        });
    }

    function initializeWidgetHosts() {
        const hosts = document.querySelectorAll(EXIT_WIDGET_HOST_SELECTOR);

        hosts.forEach(host => {
            if (!host.dataset.exitWidgetInitialized) {
                host.innerHTML = renderWidgetMarkup();
                host.dataset.exitWidgetInitialized = 'true';
            }

            const actionButton = host.querySelector('[data-action="show-exit-offline-mode"]');
            if (actionButton && !actionButton.dataset.exitWidgetBound) {
                actionButton.addEventListener('click', showExitOfflineModeModal);
                actionButton.dataset.exitWidgetBound = 'true';
            }
        });

        updateWidgetVisibility();
    }

    function showExitOfflineModeModal() {
        if (!window.OfflineModals || typeof window.OfflineModals.showExitOfflineMode !== 'function') {
            logWarn('OfflineExitManager', 'Exit Offline Mode modal is not available.');
            return;
        }

        window.OfflineModals.showExitOfflineMode();
    }

    function closeExitOfflineModeModal() {
        if (g_exit_offline_in_progress) {
            return;
        }

        if (window.OfflineModals && typeof window.OfflineModals.closeExitOfflineMode === 'function') {
            window.OfflineModals.closeExitOfflineMode();
        }
    }

    async function redirectToNormalLogin() {
        window.location.href = '/Account/AutoLogin';
    }

    async function confirmExitOfflineMode() {
        if (g_exit_offline_in_progress) {
            return;
        }

        const pendingCleanupPayload = buildPendingCleanupPayload();

        try {
            closeExitOfflineModeModal();
            g_exit_offline_in_progress = true;
            if (window.OfflineModals && typeof window.OfflineModals.showMovingToOnline === 'function') {
                window.OfflineModals.showMovingToOnline();
            }

            setPendingCleanupCookie(true);

            await bestEffortPersistOfflineCases();

            if (!isOfflineModeServerSession()) {
                await bestEffortUpdateOfflineState(pendingCleanupPayload);
                await bestEffortReleaseCaseLocks(pendingCleanupPayload);
            } else {
                logInfo('OfflineExitManager', 'Current browser session is scoped to offline_mode. Deferring server cleanup until after normal sign-in.');
            }

            await bestEffortSyncLogs();
        } catch (error) {
            logError('OfflineExitManager', 'Unexpected error during exit offline flow. Continuing with local cleanup and redirect.', error);
        } finally {
            try {
                await bestEffortLocalCleanup();
            } catch (cleanupError) {
                logWarn('OfflineExitManager', 'Unexpected local cleanup failure during exit offline mode.', cleanupError);
            }

            persistPendingCleanup(pendingCleanupPayload);
            await redirectToNormalLogin();
        }
    }

    async function finishPendingCleanup() {
        if (g_pending_cleanup_in_progress) {
            return false;
        }

        const payload = getPendingCleanupPayload();
        if (!payload) {
            clearPendingCleanup();
            return false;
        }

        if (isOfflineModeActive() || isOfflineModeServerSession()) {
            return false;
        }

        g_pending_cleanup_in_progress = true;

        try {
            logInfo('OfflineExitManager', 'Finishing deferred offline exit cleanup.', payload);

            await bestEffortUpdateOfflineState(payload);
            await bestEffortReleaseCaseLocks(payload);
            await bestEffortSyncLogs();

            clearPendingCleanup();
            logInfo('OfflineExitManager', 'Deferred offline exit cleanup completed.');
            return true;
        } catch (error) {
            logWarn('OfflineExitManager', 'Deferred offline exit cleanup did not fully complete. Leaving retry markers in place.', error);
            return false;
        } finally {
            g_pending_cleanup_in_progress = false;
        }
    }

    function onStorageChanged(event) {
        if (
            event.key === 'is_offline' ||
            event.key === 'process_offline_cases' ||
            event.key === PENDING_STORAGE_KEY
        ) {
            updateWidgetVisibility();
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        initializeWidgetHosts();
        void finishPendingCleanup();
    });

    window.addEventListener('storage', onStorageChanged);
    window.addEventListener('offlineStatusChanged', updateWidgetVisibility);

    window.OfflineExitManager = {
        showExitOfflineModeModal: showExitOfflineModeModal,
        closeExitOfflineModeModal: closeExitOfflineModeModal,
        confirmExitOfflineMode: confirmExitOfflineMode,
        finishPendingCleanup: finishPendingCleanup,
        hasPendingCleanup: hasPendingCleanup
    };
})();
