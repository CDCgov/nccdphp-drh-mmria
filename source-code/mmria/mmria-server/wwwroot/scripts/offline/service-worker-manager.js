// Service Worker Manager for MMRIA
// This file provides helper functions for managing the service worker lifecycle
// and communicating between the main thread and service worker.

// =============================================================================
// === Invalid Offline State Detection =========================================
// === Detect and recover from interrupted offline transitions =================
// =============================================================================

// Detect invalid offline state: service worker active but no offline flags in localStorage
(async function detectInvalidOfflineState() {
    try {
        if (!('serviceWorker' in navigator)) return;
        
        const registration = await navigator.serviceWorker.getRegistration();
        
        if (registration && registration.active) {
            const isOffline = localStorage.getItem('is_offline') === 'true';
            const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
            
            // INVALID OFFLINE STATE: Service worker active but no offline mode flags
            if (!isOffline && !hasActiveSession) {
                offlineLog.error('ServiceWorkerManager', 'INVALID OFFLINE STATE DETECTED: Service worker active but app is not in offline mode!');
                offlineLog.error('ServiceWorkerManager', 'This typically happens when offline transition was interrupted by page refresh.');
                
                // Show modal instead of confirm dialog
                if (window.OfflineModals && window.OfflineModals.showInvalidOfflineStateRecovery) {
                 
                    window.OfflineModals.showInvalidOfflineStateRecovery();
                } else {
                    offlineLog.warn('ServiceWorkerManager', 'OfflineModals not loaded yet, waiting...');
                    // Wait a moment for modals to load, then try again
                    setTimeout(() => {
                        if (window.OfflineModals && window.OfflineModals.showInvalidOfflineStateRecovery) {
                          
                            window.OfflineModals.showInvalidOfflineStateRecovery();
                        }
                    }, 500);
                }
            }
        }
    } catch (error) {
        offlineLog.error('ServiceWorkerManager', 'Error detecting invalid offline state:', error);
    }
})();

function getEffectiveActiveOfflineSessionStatus() {
    try {
        if (window.OfflineStatus && typeof window.OfflineStatus.hasEffectiveActiveSession === 'function') {
            return window.OfflineStatus.hasEffectiveActiveSession();
        }

        return localStorage.getItem('has_active_offline_session') === 'true';
    } catch (_error) {
        return false;
    }
}

// Helper object for service worker management
window.ServiceWorkerManager = {
    
    // =============================================================================
    // === Core Service Worker Management ==========================================
    // === Basic service worker lifecycle and communication methods ================
    // =============================================================================
    
    // Check if service worker is supported
    isSupported: function() {
        return 'serviceWorker' in navigator;
    },
    
    // Get the current service worker registration
    getRegistration: async function() {
        if (!this.isSupported()) return null;
        
        try {
            return await navigator.serviceWorker.getRegistration();
        } catch (error) {
            offlineLog.error('ServiceWorkerManager', 'Error getting service worker registration:', error);
            return null;
        }
    },
    
    // Check if service worker is active
    isActive: async function() {
        const registration = await this.getRegistration();
        return registration && registration.active;
    },
    
    // Send message to service worker (fire-and-forget)
    sendMessage: function(message) {
        if (!navigator.serviceWorker.controller) {
            offlineLog.warn('ServiceWorkerManager', 'No service worker controller available');
            return;
        }
        
        navigator.serviceWorker.controller.postMessage(message);
    },

    requestResponse: async function(message, options = {}) {
        const timeoutMs = options.timeoutMs || 15000;
        const useController = options.useController !== false;

        if (!('serviceWorker' in navigator)) {
            throw new Error('Service Worker not supported');
        }

        const registration = await navigator.serviceWorker.ready;
        const target = useController ? navigator.serviceWorker.controller : registration.active;

        if (!target) {
            throw new Error('No active service worker target available');
        }

        return new Promise((resolve, reject) => {
            const messageChannel = new MessageChannel();
            let isSettled = false;

            const timeoutId = setTimeout(() => {
                if (isSettled) {
                    return;
                }

                isSettled = true;
                reject(new Error(`Service worker request timed out for ${message.type}`));
            }, timeoutMs);

            messageChannel.port1.onmessage = (event) => {
                if (isSettled) {
                    return;
                }

                isSettled = true;
                clearTimeout(timeoutId);
                resolve(event.data);
            };

            try {
                target.postMessage(message, [messageChannel.port2]);
            } catch (error) {
                if (isSettled) {
                    return;
                }

                isSettled = true;
                clearTimeout(timeoutId);
                reject(error);
            }
        });
    },

    hasAllCacheEntries: async function(cache, paths) {
        for (const path of paths) {
            const match = await cache.match(path);
            if (!match) {
                return false;
            }
        }

        return true;
    },

    waitForCacheReadiness: async function(expectedOfflineIds, options = {}) {
        const timeoutMs = options.timeoutMs || 20000;
        const pollMs = options.pollMs || 500;
        const startTime = Date.now();
        const requiredStaticFiles = window.OfflineCacheManifest ? (window.OfflineCacheManifest.requiredStaticFiles || []) : [];
        const requiredRouteEntries = ['/Case', '/Home/Index', '/', '/Account/OfflineLogin', '/Account/OfflineLogin/'];
        const requiredApiEntries = ['/api/OfflineCase/cache-version'];

        while ((Date.now() - startTime) < timeoutMs) {
            const sessionInfo = await this.session.getCurrent();
            if (sessionInfo && sessionInfo.cacheNames && sessionInfo.cacheNames.static && sessionInfo.cacheNames.api) {
                const staticCache = await caches.open(sessionInfo.cacheNames.static);
                const apiCache = await caches.open(sessionInfo.cacheNames.api);

                const hasAllStaticFiles = await this.hasAllCacheEntries(staticCache, requiredStaticFiles);
                const hasAllRoutes = await this.hasAllCacheEntries(apiCache, requiredRouteEntries);
                const hasAllApiEntries = await this.hasAllCacheEntries(apiCache, requiredApiEntries);
                const hasAllCases = await this.hasAllCacheEntries(
                    apiCache,
                    (expectedOfflineIds || []).map(caseId => `/api/case?case_id=${caseId}`)
                );

                const apiKeys = await apiCache.keys();
                const hasOfflineSessionData = apiKeys.some(request => request.url.indexOf('CACHE_OFFLINE_SESSION_DATA') >= 0);

                if (hasAllStaticFiles && hasAllRoutes && hasAllApiEntries && hasAllCases && hasOfflineSessionData) {
                    return { success: true };
                }
            }

            await new Promise(resolve => setTimeout(resolve, pollMs));
        }

        throw new Error('Timed out waiting for offline cache readiness');
    },
    
    // =============================================================================
    // === Session Management ======================================================
    // === Offline session initialization and lifecycle ============================
    // =============================================================================
    
    session: {
        currentSessionId: null,
        isInitialized: false,
        
        // Initialize a new offline session
        initialize: async function() {
            try {
                offlineLog.log('ServiceWorkerManager', 'ServiceWorkerManager.session: Initializing new offline session...');
                
                // Generate unique session ID
                const sessionId = `${Date.now()}-${crypto.randomUUID()}`;
                
                // Check if service worker is available
                if (!('serviceWorker' in navigator)) {
                    throw new Error('Service Worker not supported');
                }

                const registration = await navigator.serviceWorker.ready;
                if (!registration.active) {
                    throw new Error('No active service worker found');
                }

                // Send initialization message to service worker
                const messageChannel = new MessageChannel();
                
                return new Promise((resolve, reject) => {
                    messageChannel.port1.onmessage = (event) => {
                        const response = event.data;
                        if (response.success) {
                            this.currentSessionId = response.sessionId;
                            this.isInitialized = true;
                            offlineLog.log('ServiceWorkerManager', 'Session initialized successfully:', response.sessionId);
                            resolve(response);
                        } else {
                            offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager.session: Session initialization failed:', response.error);
                            reject(new Error(response.error || 'Session initialization failed'));
                        }
                    };

                    // Send initialization message
                    registration.active.postMessage({
                        type: 'INIT_OFFLINE_SESSION',
                        data: { sessionId }
                    }, [messageChannel.port2]);

                    // Timeout after 10 seconds
                    setTimeout(() => {
                        reject(new Error('Session initialization timed out'));
                    }, 10000);
                });

            } catch (error) {
                offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager.session: Error initializing offline session:', error);
                throw error;
            }
        },
        
        // Get current session information
        getCurrent: async function() {
            try {
                if (!('serviceWorker' in navigator)) {
                    return null;
                }

                const registration = await navigator.serviceWorker.ready;
                if (!registration.active) {
                    return null;
                }

                const messageChannel = new MessageChannel();
                
                return new Promise((resolve) => {
                    messageChannel.port1.onmessage = (event) => {
                        resolve(event.data);
                    };

                    registration.active.postMessage({
                        type: 'GET_CURRENT_SESSION_INFO'
                    }, [messageChannel.port2]);

                    // Timeout after 5 seconds
                    setTimeout(() => {
                        resolve(null);
                    }, 5000);
                });

            } catch (error) {
                offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager.session: Error getting session info:', error);
                return null;
            }
        },
        
        // Check if current session is initialized
        isInitialized: function() {
            return this.isInitialized && this.currentSessionId !== null;
        },
        
        // Get current session ID
        getSessionId: function() {
            return this.currentSessionId;
        },
        
        // Clear session data (called when going online)
        clear: function() {
            this.currentSessionId = null;
            this.isInitialized = false;
        }
    },
    
    // =============================================================================
    // === Cache Operations ========================================================
    // === Pre-caching and cache management for offline mode =======================
    // =============================================================================
    
    // Get cache status from service worker
    getCacheStatus: async function() {
        return new Promise((resolve) => {
            if (!navigator.serviceWorker.controller) {
                resolve({});
                return;
            }
            
            const messageChannel = new MessageChannel();
            
            messageChannel.port1.onmessage = function(event) {
                resolve(event.data);
            };
            
            navigator.serviceWorker.controller.postMessage(
                { type: 'GET_CACHE_STATUS' },
                [messageChannel.port2]
            );
            
            // Timeout after 5 seconds
            setTimeout(() => resolve({}), 5000);
        });
    },
    
    // Clear all caches through service worker
    clearCaches: function() {
        this.sendMessage({ type: 'CLEAR_CACHES' });
    },

    getActiveApiCacheName: async function() {
        const sessionInfo = await this.session.getCurrent();
        if (sessionInfo && sessionInfo.cacheNames && sessionInfo.cacheNames.api) {
            return sessionInfo.cacheNames.api;
        }

        return null;
    },

    cacheOfflineSessionData: async function(sessionData) {
        if (!sessionData) {
            throw new Error('Offline session data is required');
        }

        const result = await this.requestResponse({
            type: 'CACHE_OFFLINE_SESSION_DATA',
            data: sessionData
        }, {
            timeoutMs: 10000
        });

        if (!result || result.success !== true) {
            throw new Error(result && result.error ? result.error : 'Failed to cache offline session data');
        }

        return true;
    },

    getOfflineRemovedCasesState: async function(sessionId) {
        const result = await this.requestResponse({
            type: 'GET_OFFLINE_REMOVED_CASES_STATE',
            data: { sessionId: sessionId || null }
        }, {
            timeoutMs: 10000
        });

        if (!result || result.success !== true) {
            throw new Error(result && result.error ? result.error : 'Failed to load offline removed cases state');
        }

        return result.state || null;
    },

    setOfflineRemovedCasesState: async function(state) {
        const result = await this.requestResponse({
            type: 'SET_OFFLINE_REMOVED_CASES_STATE',
            data: { state: state || null }
        }, {
            timeoutMs: 10000
        });

        if (!result || result.success !== true) {
            throw new Error(result && result.error ? result.error : 'Failed to save offline removed cases state');
        }

        return result.state || null;
    },
    
    // Cache metadata resources for offline use
    cacheMetadataResources: function(version) {
        if (!version) {
            offlineLog.warn('ServiceWorkerManager', 'Service Worker Manager: No version provided for metadata caching');
            return;
        }
        
        offlineLog.log('ServiceWorkerManager', `Service Worker Manager: Requesting cache of metadata resources for version: ${version}`);
        this.sendMessage({ 
            type: 'CACHE_METADATA_RESOURCES',
            data: { version: version }
        });
    },
    
    // Check if critical metadata resources are cached
    checkCriticalResources: async function(version) {
        return new Promise((resolve) => {
            if (!navigator.serviceWorker.controller) {
                resolve({ allCached: false, missingResources: ['no service worker'] });
                return;
            }
            
            if (!version) {
                resolve({ allCached: false, missingResources: ['no version provided'] });
                return;
            }
            
            const messageChannel = new MessageChannel();
            
            messageChannel.port1.onmessage = function(event) {
                resolve(event.data);
            };
            
            navigator.serviceWorker.controller.postMessage(
                { 
                    type: 'CHECK_CRITICAL_RESOURCES',
                    data: { version: version }
                },
                [messageChannel.port2]
            );
            
            // Timeout after 5 seconds
            setTimeout(() => resolve({ 
                allCached: false, 
                missingResources: ['timeout'],
                error: 'Check operation timed out' 
            }), 5000);
        });
    },
    
    // Pre-fetch offline cases and cache them via service worker
    prefetchCases: async function(offlineIds) {
        offlineLog.log('ServiceWorkerManager', `Pre-fetching ${offlineIds.length} offline cases...`);
        
        try {
            if (!('serviceWorker' in navigator)) {
                throw new Error('Service Worker not supported');
            }

            const registration = await navigator.serviceWorker.ready;
            if (!registration.active) {
                throw new Error('No active service worker found');
            }

            let cachedCount = 0;
            let failedCount = 0;
            const failedCases = [];
            
            // Fetch each case and send to service worker for caching
            for (const caseId of offlineIds) {
                try {
                    // Fetch case from server
                    const response = await fetch(`/api/case?case_id=${caseId}`);
                    
                    if (response.ok) {
                        const caseData = await response.json();
                        
                        const cacheResult = await this.requestResponse({
                            type: 'CACHE_CASE_DATA',
                            data: {
                                caseId: caseId,
                                caseData: caseData
                            }
                        }, {
                            timeoutMs: 15000,
                            useController: false
                        });

                        if (!cacheResult || cacheResult.success !== true) {
                            throw new Error(cacheResult && cacheResult.error ? cacheResult.error : 'Service worker did not confirm case cache');
                        }

                        cachedCount++;
                    } else {
                        offlineLog.error('ServiceWorkerManager', `Failed to fetch case ${caseId}: ${response.status}`);
                        failedCount++;
                        failedCases.push({ caseId, error: `HTTP ${response.status}` });
                    }
                } catch (error) {
                    offlineLog.error('ServiceWorkerManager', `Error prefetching case ${caseId}:`, error);
                    failedCount++;
                    failedCases.push({ caseId, error: error.message });
                }
            }
            
            offlineLog.log('ServiceWorkerManager', `Prefetch complete - ${cachedCount} cached, ${failedCount} failed`);
            
            return {
                success: true,
                cachedCount,
                failedCount,
                failedCases: failedCount > 0 ? failedCases : undefined
            };

        } catch (error) {
            offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager: Error prefetching cases:', error);
            throw error;
        }
    },
    
    // Pre-cache essential pages for offline mode
    precachePages: async function() {
        const essentialPages = [
            '/Case'
        ];
        
        try {
            for (const pagePath of essentialPages) {
                try {
                    const response = await fetch(pagePath);
                    
                    if (!response.ok) {
                        offlineLog.warn('ServiceWorkerManager', `Failed to pre-cache page ${pagePath}: ${response.status} ${response.statusText}`);
                    }
                } catch (error) {
                    offlineLog.warn('ServiceWorkerManager', `Error pre-caching page ${pagePath}:`, error);
                }
            }
            
            offlineLog.log('ServiceWorkerManager', 'Essential pages pre-cached');
            
        } catch (error) {
            offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager: Error in precachePages:', error);
            throw error;
        }
    },
    
    // Cache metadata using service worker
    cacheMetadata: async function(currentVersion) {
        try {
            // Validate or determine the current version
            let version = currentVersion;
            if (!version) {
                // Get the release version from the API
                try {
                    const versionResponse = await fetch('/api/version/release-version');
                    if (versionResponse.ok) {
                        version = await versionResponse.text();
                        // Remove quotes if present
                        version = version.replace(/"/g, '').trim();
                    } else {
                        offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager: Failed to fetch release version:', versionResponse.status, versionResponse.statusText);
                        throw new Error(`Failed to fetch release version: ${versionResponse.status}`);
                    }
                } catch (error) {
                    offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager: Could not determine metadata version. Error:', error.message);
                    throw new Error('Cannot proceed without valid metadata version');
                }
            }
            
            // Verify we have a valid version before proceeding
            if (!version || version === 'undefined' || version === 'null') {
                throw new Error('Invalid metadata version - cannot cache metadata without valid version');
            }
            
            if (!('serviceWorker' in navigator) || !navigator.serviceWorker.controller) {
                throw new Error('Service worker not available for metadata caching');
            }

            const metadataResult = await this.requestResponse({
                type: 'CACHE_METADATA',
                data: { version: version }
            }, {
                timeoutMs: 30000
            });

            if (!metadataResult || metadataResult.success !== true) {
                throw new Error(metadataResult && metadataResult.error ? metadataResult.error : 'Metadata caching was not confirmed by the service worker');
            }
            
            offlineLog.log('ServiceWorkerManager', 'Metadata caching completed');
            
        } catch (error) {
            offlineLog.error('ServiceWorkerManager', 'ServiceWorkerManager: Error in metadata caching process:', error);
            throw error;
        }
    },
    
    // Derive the offline encryption key in-page and send only the result to the service worker
    setOfflineKey: async function(password, saltHex) {
        if (!('serviceWorker' in navigator)) return false;

        const registration = await navigator.serviceWorker.ready;
        if (!registration.active) return false;

        try {
            // Convert hex salt to Uint8Array
            const saltBytes = new Uint8Array(saltHex.match(/.{1,2}/g).map(byte => parseInt(byte, 16)));
            
            // Encode the user-provided secret as UTF-8
            const encoder = new TextEncoder();
            const passwordBytes = encoder.encode(password);
            
            // Import the user-provided secret as key material
            const keyMaterial = await crypto.subtle.importKey(
                'raw',
                passwordBytes,
                { name: 'PBKDF2' },
                false,
                ['deriveBits', 'deriveKey']
            );
            
            // Derive encryption key using PBKDF2
            const derivedKey = await crypto.subtle.deriveKey(
                {
                    name: 'PBKDF2',
                    salt: saltBytes,
                    iterations: 100000,
                    hash: 'SHA-256'
                },
                keyMaterial,
                { name: 'AES-GCM', length: 256 },
                true,
                ['encrypt', 'decrypt']
            );
            
            // Export the derived key as raw bytes
            const derivedKeyBytes = await crypto.subtle.exportKey('raw', derivedKey);
            const derivedKeyArray = new Uint8Array(derivedKeyBytes);
            
            // Clear the raw secret from memory (best effort)
            passwordBytes.fill(0);
            
            // Send only the derived key to the service worker
            return new Promise(resolve => {
                const messageChannel = new MessageChannel();

                messageChannel.port1.onmessage = (event) => {
                    resolve(event.data && event.data.success === true);
                };

                registration.active.postMessage(
                    {
                        type: 'SET_OFFLINE_ENCRYPTION_KEY',
                        keyBytes: derivedKeyArray.buffer,
                        saltHex: saltHex
                    },
                    [messageChannel.port2]
                );
            });
            
        } catch (error) {
            offlineLog.error('ServiceWorkerManager', 'Error deriving offline encryption key:', error);
            return false;
        }
    },
    
    // =============================================================================
    // === Status & Communication ==================================================
    // === Offline status checks and service worker notifications ==================
    // =============================================================================

    // Check offline status
    checkOfflineStatus: function() {
        const isOffline = localStorage.getItem('is_offline') === 'true';
        return isOffline;
    },
    
    // Notify service worker of offline status change
    notifyOfflineStatusChange: function() {
        if (navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({
                type: 'OFFLINE_STATUS_UPDATE'
            });
        }
    },
    
    // Notify service worker of active offline session change
    notifyActiveOfflineSessionChange: function() {
        if (navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({
                type: 'ACTIVE_OFFLINE_SESSION_UPDATE'
            });
        }
    },
    
    // Immediately set service worker to online mode (for go online process)
    setOnlineImmediately: function() {
        if (navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({
                type: 'GO_ONLINE_IMMEDIATE'
            });
        }
    }
};

// =============================================================================
// === Initialization ==========================================================
// === Set up message listeners and initial service worker communication =======
// =============================================================================

// Set up service worker message listener
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.addEventListener('message', function(event) {
        const { type, data } = event.data;
        
        switch (type) {
            case 'CHECK_OFFLINE_STATUS':
                // Service worker is asking for offline status
                const isOffline = ServiceWorkerManager.checkOfflineStatus();
                event.source.postMessage({
                    type: 'OFFLINE_STATUS_RESPONSE',
                    isOffline: isOffline
                });
                break;
                
            case 'GET_OFFLINE_STATUS':
                // Service worker is asking for offline status (via port)
                const isOfflineMode = ServiceWorkerManager.checkOfflineStatus();
                event.ports[0].postMessage({
                    type: 'OFFLINE_STATUS_RESPONSE',
                    isOffline: isOfflineMode
                });
                break;
                
            case 'GET_ACTIVE_OFFLINE_SESSION':
                // Service worker is asking for active offline session status (via port)
                const hasActiveSession = getEffectiveActiveOfflineSessionStatus();
                event.ports[0].postMessage({
                    type: 'ACTIVE_OFFLINE_SESSION_RESPONSE',
                    hasActiveSession: hasActiveSession
                });
                break;
                
            default:
                // Silently ignore unknown message types
                break;
        }
    });
}

// Send initial status to service worker to avoid first-request lifecycle issues
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.ready.then(function(registration) {
        if (registration.active) {
            const offlineStatus = ServiceWorkerManager.checkOfflineStatus();
            const activeOfflineSession = getEffectiveActiveOfflineSessionStatus();
            
            registration.active.postMessage({
                type: 'INITIAL_STATUS_SETUP',
                offlineStatus: offlineStatus,
                activeOfflineSession: activeOfflineSession
            });
        }
    }).catch(function(error) {
        offlineLog.warn('ServiceWorkerManager', 'Service Worker Manager: Could not send initial status:', error);
    });
}

// Backward compatibility: Expose session manager as window.offlineSessionManager
window.offlineSessionManager = ServiceWorkerManager.session;
