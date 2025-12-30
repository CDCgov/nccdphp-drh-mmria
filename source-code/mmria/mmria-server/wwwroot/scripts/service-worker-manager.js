// Service Worker Manager for MMRIA
// This file provides helper functions for managing the service worker lifecycle
// and communicating between the main thread and service worker.

console.log('Service Worker Manager loaded');

// =============================================================================
// === Zombie State Detection ==================================================
// === Detect and recover from interrupted offline transitions =================
// =============================================================================

// Detect zombie state: service worker active but no offline flags in localStorage
(async function detectZombieState() {
    try {
        if (!('serviceWorker' in navigator)) return;
        
        const registration = await navigator.serviceWorker.getRegistration();
        
        if (registration && registration.active) {
            const isOffline = localStorage.getItem('is_offline') === 'true';
            const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
            
            // ZOMBIE STATE: Service worker active but no offline mode flags
            if (!isOffline && !hasActiveSession) {
                console.error('🚨 ZOMBIE STATE DETECTED: Service worker active but app is not in offline mode!');
                console.error('This typically happens when offline transition was interrupted by page refresh.');
                
                // Show modal instead of confirm dialog
                if (window.OfflineModals && window.OfflineModals.showZombieStateRecovery) {
                 
                    window.OfflineModals.showZombieStateRecovery();
                } else {
                    console.warn('OfflineModals not loaded yet, waiting...');
                    // Wait a moment for modals to load, then try again
                    setTimeout(() => {
                        if (window.OfflineModals && window.OfflineModals.showZombieStateRecovery) {
                          
                            window.OfflineModals.showZombieStateRecovery();
                        }
                    }, 500);
                }
            }
        }
    } catch (error) {
        console.error('Error detecting zombie state:', error);
    }
})();

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
            console.error('Error getting service worker registration:', error);
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
            console.warn('No service worker controller available');
            return;
        }
        
        navigator.serviceWorker.controller.postMessage(message);
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
                console.log('ServiceWorkerManager.session: Initializing new offline session...');
                
                // Generate unique session ID
                const sessionId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
                
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
                            console.log('ServiceWorkerManager.session: Session initialized successfully:', {
                                sessionId: this.currentSessionId,
                                cacheNames: response.cacheNames
                            });
                            resolve(response);
                        } else {
                            console.error('ServiceWorkerManager.session: Session initialization failed:', response.error);
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
                console.error('ServiceWorkerManager.session: Error initializing offline session:', error);
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
                console.error('ServiceWorkerManager.session: Error getting session info:', error);
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
            console.log('ServiceWorkerManager.session: Clearing session data');
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
    
    // Cache metadata resources for offline use
    cacheMetadataResources: function(version) {
        if (!version) {
            console.warn('Service Worker Manager: No version provided for metadata caching');
            return;
        }
        
        console.log(`Service Worker Manager: Requesting cache of metadata resources for version: ${version}`);
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
        console.log('ServiceWorkerManager: Pre-fetching offline cases...');
        console.log(`ServiceWorkerManager: Requesting prefetch of ${offlineIds.length} cases`);
        
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
                        
                        // Send to service worker for caching (with encryption if key is set)
                        registration.active.postMessage({
                            type: 'CACHE_CASE_DATA',
                            data: {
                                caseId: caseId,
                                caseData: caseData
                            }
                        });
                        
                        cachedCount++;
                        console.log(`ServiceWorkerManager: ✅ Prefetched case ${cachedCount}/${offlineIds.length}: ${caseId}`);
                    } else {
                        console.error(`ServiceWorkerManager: ❌ Failed to fetch case ${caseId}: ${response.status}`);
                        failedCount++;
                        failedCases.push({ caseId, error: `HTTP ${response.status}` });
                    }
                } catch (error) {
                    console.error(`ServiceWorkerManager: ❌ Error prefetching case ${caseId}:`, error);
                    failedCount++;
                    failedCases.push({ caseId, error: error.message });
                }
            }
            
            console.log(`ServiceWorkerManager: Prefetch complete - ✅ ${cachedCount} cached, ❌ ${failedCount} failed`);
            
            return {
                success: true,
                cachedCount,
                failedCount,
                failedCases: failedCount > 0 ? failedCases : undefined
            };

        } catch (error) {
            console.error('ServiceWorkerManager: Error prefetching cases:', error);
            throw error;
        }
    },
    
    // Pre-cache essential pages for offline mode
    precachePages: async function() {
        console.log('ServiceWorkerManager: Pre-caching essential pages...');
        
        const essentialPages = [
            '/Case'
        ];
        
        try {
            for (const pagePath of essentialPages) {
                try {
                    console.log(`ServiceWorkerManager: Pre-caching page: ${pagePath}`);
                    const response = await fetch(pagePath);
                    
                    if (response.ok) {
                        console.log(`ServiceWorkerManager: Successfully pre-cached page: ${pagePath}`);
                    } else {
                        console.warn(`ServiceWorkerManager: Failed to pre-cache page ${pagePath}: ${response.status} ${response.statusText}`);
                    }
                } catch (error) {
                    console.warn(`ServiceWorkerManager: Error pre-caching page ${pagePath}:`, error);
                }
            }
            
            console.log('ServiceWorkerManager: Essential pages pre-caching completed');
            
        } catch (error) {
            console.error('ServiceWorkerManager: Error in precachePages:', error);
            throw error;
        }
    },
    
    // Cache metadata using service worker
    cacheMetadata: async function(currentVersion) {
        console.log('ServiceWorkerManager: Caching metadata with service worker for offline mode...');
        
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
                        console.log(`ServiceWorkerManager: Fetched release version: ${version}`);
                    } else {
                        console.error('ServiceWorkerManager: Failed to fetch release version:', versionResponse.status, versionResponse.statusText);
                        throw new Error(`Failed to fetch release version: ${versionResponse.status}`);
                    }
                } catch (error) {
                    console.error('ServiceWorkerManager: Could not determine metadata version. Error:', error.message);
                    throw new Error('Cannot proceed without valid metadata version');
                }
            }
            
            // Verify we have a valid version before proceeding
            if (!version || version === 'undefined' || version === 'null') {
                throw new Error('Invalid metadata version - cannot cache metadata without valid version');
            }
            
            console.log(`ServiceWorkerManager: Caching metadata for version: ${version}`);
            
            // Check if service worker is available and active
            if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                console.log('ServiceWorkerManager: Service worker is available and active');
                
                // Send message to service worker to cache metadata
                navigator.serviceWorker.controller.postMessage({
                    type: 'CACHE_METADATA',
                    version: version
                });
                
                // Wait for service worker to process the caching request
                await new Promise(resolve => setTimeout(resolve, 3000));
                console.log('ServiceWorkerManager: Metadata caching request sent to service worker');
                
            } else {
                console.warn('ServiceWorkerManager: Service worker not available, falling back to basic fetch caching');
            }
            
            // Always perform basic fetch to ensure resources are cached (fallback or supplement)
            const criticalEndpoints = [
                `/api/version/${version}/metadata`,
                `/api/version/${version}/ui_specification`,
                `/api/version/${version}/validation`,
                '/_users/GetFormAccess',
                '/api/user/my-user',
                '/api/user_role_jurisdiction_view/my-roles'
            ];
            
            console.log(`ServiceWorkerManager: Fetching ${criticalEndpoints.length} critical metadata endpoints...`);
            
            for (const endpoint of criticalEndpoints) {
                try {
                    const response = await fetch(endpoint);
                    if (response.ok) {
                        console.log(`ServiceWorkerManager: Fetched: ${endpoint}`);
                    } else {
                        console.warn(`ServiceWorkerManager: Failed to fetch ${endpoint}: ${response.status}`);
                    }
                } catch (error) {
                    console.warn(`ServiceWorkerManager: Error fetching ${endpoint}:`, error);
                }
            }
            
            console.log('ServiceWorkerManager: Metadata caching process completed');
            
        } catch (error) {
            console.error('ServiceWorkerManager: Error in metadata caching process:', error);
            throw error;
        }
    },
    
    // Send password to service worker to derive and set encryption key
    setOfflineKey: async function(password, saltHex) {
        if (!('serviceWorker' in navigator)) return false;

        const registration = await navigator.serviceWorker.ready;
        if (!registration.active) return false;

        return new Promise(resolve => {
            const messageChannel = new MessageChannel();

            messageChannel.port1.onmessage = (event) => {
                resolve(event.data && event.data.success === true);
            };

            registration.active.postMessage(
                {
                    type: 'DERIVE_AND_SET_OFFLINE_KEY',
                    password: password,
                    saltHex: saltHex
                },
                [messageChannel.port2]
            );
        });
    },
    
    // =============================================================================
    // === Status & Communication ==================================================
    // === Offline status checks and service worker notifications ==================
    // =============================================================================

    // Check offline status
    checkOfflineStatus: function() {
        const isOffline = localStorage.getItem('is_offline') === 'true';
        console.log('Service Worker Manager: Offline status =', isOffline);
        return isOffline;
    },
    
    // Notify service worker of offline status change
    notifyOfflineStatusChange: function() {
        if (navigator.serviceWorker.controller) {
            console.log('Service Worker Manager: Notifying service worker of offline status change');
            navigator.serviceWorker.controller.postMessage({
                type: 'OFFLINE_STATUS_UPDATE'
            });
        }
    },
    
    // Notify service worker of active offline session change
    notifyActiveOfflineSessionChange: function() {
        if (navigator.serviceWorker.controller) {
            console.log('Service Worker Manager: Notifying service worker of active offline session change');
            navigator.serviceWorker.controller.postMessage({
                type: 'ACTIVE_OFFLINE_SESSION_UPDATE'
            });
        }
    },
    
    // Immediately set service worker to online mode (for go online process)
    setOnlineImmediately: function() {
        if (navigator.serviceWorker.controller) {
            console.log('Service Worker Manager: Setting service worker to online mode immediately');
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
                const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
                event.ports[0].postMessage({
                    type: 'ACTIVE_OFFLINE_SESSION_RESPONSE',
                    hasActiveSession: hasActiveSession
                });
                break;
                
            default:
                console.log('Service Worker Manager received message:', event.data);
        }
    });
}

// Make sure this doesn't interfere with existing offline functionality
console.log('Service Worker Manager initialized successfully');

// Send initial status to service worker to avoid first-request lifecycle issues
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.ready.then(function(registration) {
        if (registration.active) {
            console.log('Service Worker Manager: Sending initial status setup to service worker');
            
            const offlineStatus = ServiceWorkerManager.checkOfflineStatus();
            const activeOfflineSession = localStorage.getItem('has_active_offline_session') === 'true';
            
            registration.active.postMessage({
                type: 'INITIAL_STATUS_SETUP',
                offlineStatus: offlineStatus,
                activeOfflineSession: activeOfflineSession
            });
            
            console.log('Service Worker Manager: Initial status sent:', {
                offlineStatus: offlineStatus,
                activeOfflineSession: activeOfflineSession
            });
        }
    }).catch(function(error) {
        console.warn('Service Worker Manager: Could not send initial status:', error);
    });
}

// Backward compatibility: Expose session manager as window.offlineSessionManager
window.offlineSessionManager = ServiceWorkerManager.session;
