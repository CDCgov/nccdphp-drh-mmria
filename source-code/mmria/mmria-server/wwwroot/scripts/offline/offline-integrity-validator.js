(function() {
    'use strict';

    const CONTEXT = 'OfflineIntegrityValidator';
    const SESSION_CACHE_PATH_FRAGMENT = '/offline-session-data/';
    const CACHE_NAME_PATTERN = /^mmria-(static|api)-(.+)-session-(.+)$/;
    let monitoringIntervalId = null;

    function getConfiguredBlockOnError(sessionData) {
        if (sessionData && typeof sessionData.blockAndAlertOnError === 'boolean') {
            return sessionData.blockAndAlertOnError;
        }

        if (typeof window.is_offline_mode_block_and_alert_on_error === 'boolean') {
            return window.is_offline_mode_block_and_alert_on_error;
        }

        if (typeof is_offline_mode_block_and_alert_on_error === 'boolean') {
            return is_offline_mode_block_and_alert_on_error;
        }

        return false;
    }

    function getLocalStorageValue(key) {
        try {
            return localStorage.getItem(key);
        } catch (_error) {
            return null;
        }
    }

    function safeJsonParse(value) {
        if (!value) {
            return { ok: false, value: null, error: null };
        }

        try {
            return { ok: true, value: JSON.parse(value), error: null };
        } catch (error) {
            return { ok: false, value: null, error: error };
        }
    }

    function getExpectedCaseIds(sessionData, expectedOfflineIds) {
        const ids = [];

        if (Array.isArray(expectedOfflineIds) && expectedOfflineIds.length > 0) {
            ids.push(...expectedOfflineIds);
        }

        if (!sessionData || typeof sessionData !== 'object') {
            return [...new Set(ids.filter(Boolean))];
        }

        if (Array.isArray(sessionData.offlineIds)) {
            ids.push(...sessionData.offlineIds);
        }

        if (Array.isArray(sessionData.offline_ids)) {
            ids.push(...sessionData.offline_ids);
        }

        if (Array.isArray(sessionData.caseDocuments)) {
            ids.push(...sessionData.caseDocuments.map(doc => doc && (doc.documentId || doc._id)).filter(Boolean));
        }

        if (Array.isArray(sessionData.case_documents)) {
            ids.push(...sessionData.case_documents.map(doc => doc && (doc.documentId || doc._id)).filter(Boolean));
        }

        return [...new Set(ids.filter(Boolean))];
    }

    function getSessionId(sessionData) {
        if (sessionData && typeof sessionData === 'object') {
            return sessionData.offlineSessionId || sessionData.sessionId || sessionData._id || null;
        }

        return getLocalStorageValue('offline_session_id');
    }

    function getServiceWorkerSessionId() {
        try {
            if (window.ServiceWorkerManager && window.ServiceWorkerManager.session) {
                if (typeof window.ServiceWorkerManager.session.getSessionId === 'function') {
                    return window.ServiceWorkerManager.session.getSessionId();
                }

                return window.ServiceWorkerManager.session.currentSessionId || null;
            }
        } catch (_error) {
            return null;
        }

        return null;
    }

    function summarizeSession(sessionData) {
        if (!sessionData || typeof sessionData !== 'object') {
            return {
                hasSessionData: false,
                sessionId: null,
                expectedCaseIds: [],
                userId: null,
                blockAndAlertOnError: false
            };
        }

        return {
            hasSessionData: true,
            sessionId: getSessionId(sessionData),
            expectedCaseIds: getExpectedCaseIds(sessionData),
            userId: sessionData.user_id || sessionData.user_name || null,
            blockAndAlertOnError: getConfiguredBlockOnError(sessionData)
        };
    }

    async function getServiceWorkerDetails() {
        const details = {
            supported: typeof navigator !== 'undefined' && 'serviceWorker' in navigator,
            hasController: false,
            registrationActive: false
        };

        if (!details.supported) {
            return details;
        }

        try {
            details.hasController = !!navigator.serviceWorker.controller;
            const registration = await navigator.serviceWorker.getRegistration();
            details.registrationActive = !!(registration && registration.active);
        } catch (error) {
            offlineLog.warn(CONTEXT, 'Unable to inspect service worker registration:', error);
        }

        return details;
    }

    async function inspectApiCache(cacheName) {
        const details = {
            hasOfflineSessionCacheEntry: false,
            cachedOfflineSessionId: null,
            cachedOfflineSessionData: null,
            foundCaseIds: [],
            foundCaseCount: 0
        };

        if (!cacheName) {
            return details;
        }

        const apiCache = await caches.open(cacheName);
        const requests = await apiCache.keys();
        const foundCaseIds = new Set();

        for (const request of requests) {
            const requestUrl = new URL(request.url);

            if (requestUrl.href.includes(SESSION_CACHE_PATH_FRAGMENT)) {
                details.hasOfflineSessionCacheEntry = true;

                if (!details.cachedOfflineSessionId) {
                    try {
                        const response = await apiCache.match(request);
                        const payload = response ? await response.json() : null;
                        details.cachedOfflineSessionData = payload;
                        details.cachedOfflineSessionId = payload && (payload.offlineSessionId || payload.sessionId || payload._id || null);
                    } catch (_error) {
                        details.cachedOfflineSessionData = null;
                        details.cachedOfflineSessionId = null;
                    }
                }
            }

            if (requestUrl.pathname === '/api/case' && requestUrl.searchParams.has('case_id')) {
                foundCaseIds.add(requestUrl.searchParams.get('case_id'));
            }
        }

        details.foundCaseIds = Array.from(foundCaseIds);
        details.foundCaseCount = details.foundCaseIds.length;
        return details;
    }

    async function inspectCaches(sessionId, expectedCaseIds) {
        const result = {
            supported: typeof caches !== 'undefined',
            allCacheNames: [],
            expectedCacheNames: [],
            matchingCacheNames: [],
            cacheSessionId: null,
            cacheResolution: 'none',
            staticCacheName: null,
            apiCacheName: null,
            hasOfflineSessionCacheEntry: false,
            cachedOfflineSessionId: null,
            cachedOfflineSessionData: null,
            foundCaseIds: [],
            foundCaseCount: 0
        };

        if (!result.supported) {
            return result;
        }

        result.allCacheNames = await caches.keys();

        const sessionGroups = new Map();
        for (const cacheName of result.allCacheNames) {
            const match = cacheName.match(CACHE_NAME_PATTERN);
            if (!match) {
                continue;
            }

            const cacheType = match[1];
            const groupSessionId = match[3];
            const existingGroup = sessionGroups.get(groupSessionId) || {
                cacheSessionId: groupSessionId,
                staticCacheName: null,
                apiCacheName: null
            };

            if (cacheType === 'static') {
                existingGroup.staticCacheName = cacheName;
            } else if (cacheType === 'api') {
                existingGroup.apiCacheName = cacheName;
            }

            sessionGroups.set(groupSessionId, existingGroup);
        }

        if (sessionGroups.size === 0) {
            return result;
        }

        const candidateSessionIds = [];
        if (sessionId) {
            candidateSessionIds.push(sessionId);
        }

        const serviceWorkerSessionId = getServiceWorkerSessionId();
        if (serviceWorkerSessionId && !candidateSessionIds.includes(serviceWorkerSessionId)) {
            candidateSessionIds.push(serviceWorkerSessionId);
        }

        let selectedGroup = null;
        for (const candidateSessionId of candidateSessionIds) {
            if (sessionGroups.has(candidateSessionId)) {
                selectedGroup = sessionGroups.get(candidateSessionId);
                result.cacheResolution = candidateSessionId === sessionId
                    ? 'offline_session_id_match'
                    : 'service_worker_session_id_match';
                break;
            }
        }

        if (!selectedGroup) {
            const scoredGroups = [];
            for (const group of sessionGroups.values()) {
                const apiDetails = await inspectApiCache(group.apiCacheName);
                const overlapCount = Array.isArray(expectedCaseIds)
                    ? expectedCaseIds.filter(caseId => apiDetails.foundCaseIds.includes(caseId)).length
                    : 0;
                const sessionIdMatch = !!(sessionId && apiDetails.cachedOfflineSessionId && apiDetails.cachedOfflineSessionId === sessionId);
                const score =
                    (sessionIdMatch ? 1000 : 0) +
                    (apiDetails.hasOfflineSessionCacheEntry ? 100 : 0) +
                    (overlapCount * 10) +
                    (group.staticCacheName ? 1 : 0) +
                    (group.apiCacheName ? 1 : 0);

                scoredGroups.push({
                    group,
                    apiDetails,
                    overlapCount,
                    score
                });
            }

            scoredGroups.sort((left, right) => right.score - left.score);
            if (scoredGroups.length > 0 && scoredGroups[0].score > 0) {
                selectedGroup = scoredGroups[0].group;
                result.cacheResolution = scoredGroups[0].apiDetails.cachedOfflineSessionId === sessionId
                    ? 'cached_offline_session_payload_match'
                    : 'best_available_session_cache_match';
            }
        }

        if (!selectedGroup) {
            return result;
        }

        result.cacheSessionId = selectedGroup.cacheSessionId;
        result.staticCacheName = selectedGroup.staticCacheName;
        result.apiCacheName = selectedGroup.apiCacheName;
        result.matchingCacheNames = [selectedGroup.staticCacheName, selectedGroup.apiCacheName].filter(Boolean);
        result.expectedCacheNames = [...result.matchingCacheNames];

        if (result.apiCacheName) {
            const apiDetails = await inspectApiCache(result.apiCacheName);
            result.hasOfflineSessionCacheEntry = apiDetails.hasOfflineSessionCacheEntry;
            result.cachedOfflineSessionId = apiDetails.cachedOfflineSessionId;
            result.cachedOfflineSessionData = apiDetails.cachedOfflineSessionData;
            result.foundCaseIds = apiDetails.foundCaseIds;
            result.foundCaseCount = apiDetails.foundCaseCount;
        }

        return result;
    }

    function detectCurrentState(options = {}) {
        const checkPoint = options.checkPoint || 'manual';
        const sessionValue = getLocalStorageValue('mmria_offline_session');
        const sessionParse = safeJsonParse(sessionValue);
        const sessionData = sessionParse.ok ? sessionParse.value : null;
        const isOffline = getLocalStorageValue('is_offline') === 'true';
        const hasActiveSession = getLocalStorageValue('has_active_offline_session') === 'true';
        const isProcessingOfflineCases = getLocalStorageValue('process_offline_cases') === 'true';
        const offlineSessionId = getLocalStorageValue('offline_session_id');
        const hasSessionArtifacts = !!(sessionValue || offlineSessionId);
        const lowerCheckPoint = checkPoint.toLowerCase();

        let state = 'unknown';
        const conflicts = [];

        if (isOffline && isProcessingOfflineCases) {
            conflicts.push('is_offline and process_offline_cases are both true');
        }

        if (lowerCheckPoint.indexOf('go_online') >= 0) {
            state = 'going_online';
        } else if (lowerCheckPoint.indexOf('go_offline') >= 0) {
            state = 'going_offline';
        } else if (isProcessingOfflineCases) {
            state = 'going_online';
        } else if (isOffline || hasActiveSession) {
            state = 'offline';
        } else if (hasSessionArtifacts) {
            state = 'going_offline';
        }

        if (conflicts.length > 0) {
            state = 'unknown';
        }

        return {
            state,
            checkPoint,
            conflicts,
            flags: {
                isOffline,
                hasActiveSession,
                isProcessingOfflineCases,
                offlineSessionId,
                hasSessionArtifacts,
                hasSessionValue: !!sessionValue,
                sessionParseOk: sessionParse.ok
            },
            sessionData
        };
    }

    function buildFailureResult(baseResult, validationCode, failureCategory, issues, missingArtifacts, warnings) {
        return {
            ...baseResult,
            valid: false,
            validationCode,
            failureCategory,
            issues,
            missingArtifacts,
            warnings,
            shouldBlockOnError: baseResult.blockAndAlertOnError
        };
    }

    async function validateCurrentState(options = {}) {
        const detected = detectCurrentState(options);
        const initialSessionSummary = summarizeSession(detected.sessionData);
        const initialSessionId = initialSessionSummary.sessionId || detected.flags.offlineSessionId;
        const initialExpectedCaseIds = getExpectedCaseIds(detected.sessionData, options.expectedOfflineIds);
        const serviceWorker = await getServiceWorkerDetails();
        const cacheDetails = await inspectCaches(initialSessionId, initialExpectedCaseIds);
        const recoveredSessionData = detected.sessionData || cacheDetails.cachedOfflineSessionData;
        const sessionSummary = summarizeSession(recoveredSessionData);
        const sessionId = sessionSummary.sessionId || detected.flags.offlineSessionId || cacheDetails.cachedOfflineSessionId;
        const expectedCaseIds = getExpectedCaseIds(recoveredSessionData, options.expectedOfflineIds);
        const issues = [];
        const missingArtifacts = [];
        const warnings = [];

        offlineLog.log(CONTEXT, 'Starting integrity validation', {
            checkPoint: detected.checkPoint,
            detectedState: detected.state,
            sessionId: sessionId,
            expectedCaseCount: expectedCaseIds.length
        });

        if (detected.conflicts.length > 0) {
            issues.push(...detected.conflicts);
            missingArtifacts.push('conflicting_state_flags');
        }

        if (!detected.flags.hasSessionValue) {
            issues.push('mmria_offline_session is missing');
            missingArtifacts.push('mmria_offline_session');
        } else if (!detected.flags.sessionParseOk) {
            issues.push('mmria_offline_session could not be parsed');
            missingArtifacts.push('mmria_offline_session_parse');
        }

        if (!sessionId) {
            issues.push('offline session id is missing');
            missingArtifacts.push('offline_session_id');
        } else if (detected.flags.offlineSessionId && detected.flags.offlineSessionId !== sessionId) {
            issues.push('offline_session_id does not match mmria_offline_session');
            missingArtifacts.push('offline_session_id_mismatch');
        }

        if (detected.state === 'offline') {
            if (!detected.flags.isOffline) {
                issues.push('offline state detected without is_offline flag');
                missingArtifacts.push('is_offline');
            }

            if (!detected.flags.hasActiveSession) {
                issues.push('offline state detected without has_active_offline_session flag');
                missingArtifacts.push('has_active_offline_session');
            }
        }

        if (detected.state === 'going_online' && !detected.flags.hasSessionArtifacts) {
            issues.push('go online validation requires offline session artifacts');
            missingArtifacts.push('offline_session_artifacts');
        }

        if (!serviceWorker.supported) {
            issues.push('service worker is not supported');
            missingArtifacts.push('service_worker_support');
        } else {
            if (!serviceWorker.registrationActive) {
                issues.push('service worker registration is not active');
                missingArtifacts.push('service_worker_registration');
            }

            if ((detected.state === 'offline' || detected.state === 'going_offline') && !serviceWorker.hasController) {
                issues.push('service worker controller is missing');
                missingArtifacts.push('service_worker_controller');
            }
        }

        if (!cacheDetails.supported) {
            issues.push('cache storage API is not supported');
            missingArtifacts.push('cache_storage_support');
        } else {
            if (!cacheDetails.staticCacheName) {
                issues.push('session-specific static cache is missing');
                missingArtifacts.push('session_static_cache');
            }

            if (!cacheDetails.apiCacheName) {
                issues.push('session-specific api cache is missing');
                missingArtifacts.push('session_api_cache');
            }

            if (!cacheDetails.hasOfflineSessionCacheEntry) {
                issues.push('cached offline session payload is missing');
                missingArtifacts.push('cached_offline_session_payload');
            } else if (sessionId && cacheDetails.cachedOfflineSessionId && cacheDetails.cachedOfflineSessionId !== sessionId) {
                issues.push('cached offline session payload does not match mmria_offline_session');
                missingArtifacts.push('cached_offline_session_payload_mismatch');
            }
        }

        if (expectedCaseIds.length === 0 && (detected.state === 'offline' || detected.state === 'going_offline' || detected.state === 'going_online')) {
            warnings.push('no expected offline case ids were found in mmria_offline_session');
        }

        const foundCaseIdSet = new Set(cacheDetails.foundCaseIds);
        const missingCaseIds = expectedCaseIds.filter(caseId => !foundCaseIdSet.has(caseId));

        if (missingCaseIds.length > 0) {
            issues.push(`missing cached case data for ${missingCaseIds.length} expected case(s)`);
            missingArtifacts.push(...missingCaseIds.map(caseId => `cached_case:${caseId}`));
        }

        if (cacheDetails.foundCaseIds.length > expectedCaseIds.length && expectedCaseIds.length > 0) {
            warnings.push('more cached case entries were found than expected from mmria_offline_session');
        }

        const result = {
            checkPoint: detected.checkPoint,
            detectedState: detected.state,
            sessionId,
            userId: sessionSummary.userId,
            expectedCacheNames: cacheDetails.expectedCacheNames,
            foundCacheNames: cacheDetails.matchingCacheNames,
            cacheSessionId: cacheDetails.cacheSessionId,
            cacheResolution: cacheDetails.cacheResolution,
            expectedCaseIds,
            foundCaseIds: cacheDetails.foundCaseIds,
            blockAndAlertOnError: sessionSummary.blockAndAlertOnError,
            serviceWorker,
            flags: detected.flags,
            cacheSummary: {
                staticCacheName: cacheDetails.staticCacheName,
                apiCacheName: cacheDetails.apiCacheName,
                hasOfflineSessionCacheEntry: cacheDetails.hasOfflineSessionCacheEntry,
                cachedOfflineSessionId: cacheDetails.cachedOfflineSessionId,
                recoveredSessionData: !!cacheDetails.cachedOfflineSessionData
            }
        };

        if (issues.length > 0) {
            const failureResult = buildFailureResult(
                result,
                options.validationCode || 'offline_integrity_validation_failed',
                options.failureCategory || detected.state,
                issues,
                missingArtifacts,
                warnings
            );

            offlineLog.error(CONTEXT, 'Integrity validation failed', failureResult);
            return failureResult;
        }

        const successResult = {
            ...result,
            valid: true,
            validationCode: options.validationCode || 'offline_integrity_validation_passed',
            failureCategory: null,
            issues: [],
            missingArtifacts: [],
            warnings,
            shouldBlockOnError: false
        };

        if (warnings.length > 0) {
            offlineLog.warn(CONTEXT, 'Integrity validation passed with warnings', successResult);
        } else {
            offlineLog.info(CONTEXT, 'Integrity validation passed', successResult);
        }

        return successResult;
    }

    async function validateOrThrow(options = {}) {
        const result = await validateCurrentState(options);

        if (!result.valid && options.throwOnFailure === true) {
            const error = new Error(`Offline integrity validation failed during ${result.checkPoint}`);
            error.name = 'OfflineIntegrityValidationError';
            error.validationResult = result;
            throw error;
        }

        return result;
    }

    function stopMonitoring() {
        if (monitoringIntervalId) {
            clearInterval(monitoringIntervalId);
            monitoringIntervalId = null;
            offlineLog.log(CONTEXT, 'Stopped offline integrity monitoring');
        }
    }

    function startMonitoring(intervalMs = 30000) {
        stopMonitoring();

        monitoringIntervalId = setInterval(async () => {
            const detected = detectCurrentState({ checkPoint: 'offline_monitor' });
            if (detected.state !== 'offline') {
                return;
            }

            try {
                await validateCurrentState({ checkPoint: 'offline_monitor' });
            } catch (error) {
                offlineLog.error(CONTEXT, 'Unexpected monitoring error:', error);
            }
        }, intervalMs);

        offlineLog.log(CONTEXT, 'Started offline integrity monitoring', { intervalMs: intervalMs });
    }

    window.OfflineIntegrityValidator = {
        detectCurrentState: detectCurrentState,
        validateCurrentState: validateCurrentState,
        validateOrThrow: validateOrThrow,
        startMonitoring: startMonitoring,
        stopMonitoring: stopMonitoring
    };
})();
