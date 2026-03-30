// MMRIA Offline Service Worker
// This service worker handles caching for offline mode functionality

// Import offline logger first to ensure logging is available throughout
importScripts('/scripts/offline/offline-logger.js');
importScripts('/scripts/offline/offline-cache-manifest.js');

// Initialize offline logger for service worker context
if (typeof self.offlineLog !== 'undefined') {
    self.offlineLog.initialize(true); // Always enable logging in service worker
}

// Set up service worker error handlers
self.addEventListener('error', function(event) {
    if (typeof self.offlineLog !== 'undefined') {
        self.offlineLog.error('ServiceWorker_UncaughtError',
            `${event.message} at ${event.filename}:${event.lineno}:${event.colno}`,
            event.error
        );
    }
});

self.addEventListener('unhandledrejection', function(event) {
    if (typeof self.offlineLog !== 'undefined') {
        const reason = event.reason;
        let errorMsg = 'Unhandled Promise Rejection in Service Worker';
        
        if (reason instanceof Error) {
            errorMsg = `${reason.name}: ${reason.message}`;
        } else if (typeof reason === 'string') {
            errorMsg = reason;
        } else {
            try {
                errorMsg = JSON.stringify(reason);
            } catch (e) {
                errorMsg = String(reason);
            }
        }
        
        self.offlineLog.error('ServiceWorker_UnhandledRejection', errorMsg, reason);
    }
});

// Cache version - will be fetched from server endpoint (single source of truth)
let CACHE_VERSION_BASE = null; // Must be set from server API - no fallback
let CACHE_VERSION_FETCHED = false;
let CACHE_VERSION_FETCH_PROMISE = null;

// Fetch cache version from server endpoint (single source of truth)
// Checks cache FIRST before network to handle service worker restarts
async function fetchCacheVersionFromServer() {
    try {
        // Return if already fetched
        if (CACHE_VERSION_FETCHED) {
            return CACHE_VERSION_BASE;
        }

        // Return existing promise if already fetching
        if (CACHE_VERSION_FETCH_PROMISE) {
            await CACHE_VERSION_FETCH_PROMISE;
            return CACHE_VERSION_BASE;
        }

        // FIRST: Try to get cache version from existing caches (handles offline + restarts)
        self.offlineLog.log('ServiceWorker', 'Checking existing caches for cache-version endpoint');
        const allCacheNames = await caches.keys();
        for (const cacheName of allCacheNames) {
            if (cacheName.startsWith('mmria-')) {
                const cache = await caches.open(cacheName);
                const cachedVersionResponse = await cache.match('/api/OfflineCase/cache-version');
                if (cachedVersionResponse) {
                    const data = await cachedVersionResponse.json();
                    if (data && data.baseVersion) {
                        CACHE_VERSION_BASE = data.baseVersion;
                        CACHE_VERSION_FETCHED = true;
                        self.offlineLog.log('ServiceWorker', 'Cache version found in cache:', CACHE_VERSION_BASE);
                        
                        // Automatically detect and set session info from existing caches
                        await detectAndSetSessionInfo();
                        return CACHE_VERSION_BASE;
                    }
                }
            }
        }

        // SECOND: Try network if cache lookup failed
        self.offlineLog.log('ServiceWorker', 'Cache version not in cache, trying network');
        CACHE_VERSION_FETCH_PROMISE = fetch('/api/OfflineCase/cache-version')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Failed to fetch cache version: ${response.status}`);
                }
                return response.json();
            })
            .then(async data => {
                // Use baseVersion directly from API response (e.g., 'v38-stable')
                if (data && data.baseVersion) {
                    CACHE_VERSION_BASE = data.baseVersion;
                    CACHE_VERSION_FETCHED = true;
                    self.offlineLog.log('ServiceWorker', 'Cache version fetched from network:', CACHE_VERSION_BASE);
                    
                    // Automatically detect and set session info from existing caches
                    await detectAndSetSessionInfo();
                } else {
                    throw new Error('Invalid cache version response from server');
                }
            });

        await CACHE_VERSION_FETCH_PROMISE;
        
        if (!CACHE_VERSION_BASE) {
            throw new Error('Cache version not available - API must be accessible');
        }
        
        return CACHE_VERSION_BASE;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Failed to fetch cache version from server:', error);
        throw error; // No fallback - fail fast if API unavailable
    }
}

// Helper function to detect and set session info from existing caches
// Populates OFFLINE_CACHE_ID, STATIC_CACHE_NAME, and API_CACHE_NAME
// Called after CACHE_VERSION_BASE is set to complete initialization
async function detectAndSetSessionInfo() {
    try {
        // If already set, no need to detect again
        if (self.OFFLINE_CACHE_ID && STATIC_CACHE_NAME && API_CACHE_NAME) {           
            return true;
        }
        
        const cacheNames = await caches.keys();
        const sessionCachePattern = /^mmria-(?:api|static)-(v\d+-\w+)-session-(.+)$/;
        
        for (const cacheName of cacheNames) {
            const match = cacheName.match(sessionCachePattern);
            if (match) {
                const detectedVersion = match[1];
                const detectedSessionId = match[2];
                
                // Only use this session if the version matches
                if (detectedVersion === CACHE_VERSION_BASE) {
                    self.OFFLINE_CACHE_ID = detectedSessionId;
                    STATIC_CACHE_NAME = `mmria-static-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
                    API_CACHE_NAME = `mmria-api-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
                    
                    self.offlineLog.log('ServiceWorker', 'Detected and set session info:', {
                        sessionId: self.OFFLINE_CACHE_ID,
                        static: STATIC_CACHE_NAME,
                        api: API_CACHE_NAME
                    });
                    return true;
                }
            }
        }
        
        self.offlineLog.log('ServiceWorker', 'No matching session caches found for version:', CACHE_VERSION_BASE);
        return false;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error detecting session info:', error);
        return false;
    }
}

// Detect existing cache version when API is unavailable (offline fallback)
async function detectExistingCacheVersion() {
    try {
        const cacheNames = await caches.keys();

        // Look for mmria-*-session-* caches to extract version and session ID
        const sessionCachePattern = /^mmria-(?:api|static)-(v\d+-\w+)-session-(.+)$/;
        
        for (const cacheName of cacheNames) {
            const match = cacheName.match(sessionCachePattern);
            
            if (match) {
                const detectedVersion = match[1];
                const detectedSessionId = match[2];
                self.offlineLog.log('ServiceWorker', 'Detected existing cache version:', detectedVersion, 'session:', detectedSessionId);
                
                CACHE_VERSION_BASE = detectedVersion;
                CACHE_VERSION_FETCHED = true;
                self.OFFLINE_CACHE_ID = detectedSessionId;
                STATIC_CACHE_NAME = `mmria-static-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
                API_CACHE_NAME = `mmria-api-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
                
                self.offlineLog.log('ServiceWorker', 'Using detected cache names:', { 
                    static: STATIC_CACHE_NAME, 
                    api: API_CACHE_NAME 
                });
                
                return CACHE_VERSION_BASE;
            }
        }
        
        throw new Error('No existing MMRIA session caches found');
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Failed to detect existing cache version:', error);
        throw error;
    }
}

// Fetch cache version immediately when service worker loads
// This ensures CACHE_VERSION_BASE and cache names are set before install event
// Falls back to detecting existing caches if API is unavailable (offline mode)
fetchCacheVersionFromServer().catch(async (error) => {
    self.offlineLog.warn('ServiceWorker', 'API unavailable, attempting to detect existing caches');
    try {
        await detectExistingCacheVersion();
        self.offlineLog.log('ServiceWorker', 'Successfully initialized from existing caches (offline mode)');
    } catch (fallbackError) {
        self.offlineLog.error('ServiceWorker', 'Failed to initialize cache version - no API and no existing caches:', fallbackError);
    }
});

// ===== Offline encryption key (in-memory only) =====
let offlineCryptoKey = null; // CryptoKey | null
const OFFLINE_ENCRYPTION_HEADER = 'X-Offline-Encrypted';
const OFFLINE_ENCRYPTION_VERSION = '1';

// Crypto constants for key derivation
const KEY_DERIVATION_ITERATIONS = 100000; // PBKDF2 iterations
const HASH_ALGORITHM = 'SHA-256';
const KEY_LENGTH = 256; // bits

// Cache names - will be set after fetching version from server
self.OFFLINE_CACHE_ID = null;
self.OFFLINE_SESSION_ID = null;
let STATIC_CACHE_NAME = null; // Will be set in fetchCacheVersionFromServer()
let API_CACHE_NAME = null; // Will be set in fetchCacheVersionFromServer()

// Cache offline status to avoid repeated expensive checks during page lifecycle
self.cachedOfflineStatus = null;
let cachedActiveOfflineSession = null;
let lastStatusCheckTime = 0;
const STATUS_CACHE_DURATION = 300000; // Cache for 5 minutes (300 seconds)

// ===== Helper: load case JSON from cache (handles encrypted/plain) =====
async function loadCaseJsonFromCache(cache, request) {
    const res = await cache.match(request);
    if (!res) return null;

    // If this is an encrypted payload, decrypt using offlineCryptoKey
    if (res.headers.get(OFFLINE_ENCRYPTION_HEADER) === '1') {
        if (!offlineCryptoKey) {
            throw new Error('Encrypted case in cache but no offlineCryptoKey in memory');
        }
        const decryptedRes = await decryptResponseBody(res);
        return await decryptedRes.json();
    }

    // Plain JSON case
    return await res.json();
}

// Function to initialize new offline session cache
async function initializeOfflineSessionCache(sessionId) {
    self.offlineLog.log('ServiceWorker', 'Initializing offline session cache for session:', sessionId);
    
    // Ensure CACHE_VERSION_BASE is set before creating session cache
    if (!CACHE_VERSION_BASE) {
        self.offlineLog.warn('ServiceWorker', 'CACHE_VERSION_BASE not yet set, attempting to detect from existing caches');
        try {
            await detectExistingCacheVersion();
        } catch (error) {
            self.offlineLog.error('ServiceWorker', 'Cannot initialize session cache without cache version:', error);
            return;
        }
    }
    
    self.OFFLINE_CACHE_ID = sessionId;
    // Both static and API caches use session-specific naming for complete isolation
    // Only one offline session is supported per browser
    STATIC_CACHE_NAME = `mmria-static-${CACHE_VERSION_BASE}-session-${sessionId}`;
    API_CACHE_NAME = `mmria-api-${CACHE_VERSION_BASE}-session-${sessionId}`;
    
    self.offlineLog.log('ServiceWorker', 'Updated cache names for session:', {
        static: STATIC_CACHE_NAME,
        api: API_CACHE_NAME
    });
    
    // Cache static files to session-specific static cache
    await cacheStaticFilesForSession();
    
    // Pre-cache API routes to the session-specific API cache
    await cacheApiRoutesForSession();
}

// Function to cache static files to session-specific cache
async function cacheStaticFilesForSession() {
    try {
        if (!STATIC_CACHE_NAME) {
            throw new Error('Cannot cache static files: STATIC_CACHE_NAME is not initialized');
        }
        self.offlineLog.log('ServiceWorker', 'Caching static files for session to:', STATIC_CACHE_NAME);
        const staticCache = await caches.open(STATIC_CACHE_NAME);
        
        const cachedFiles = [];
        const requiredFailures = [];

        for (const expectation of REQUIRED_STATIC_EXPECTATIONS) {
            const response = await fetch(expectation.path);
            const validation = await validateManifestResponse(response, expectation, expectation.path);
            if (!validation.valid) {
                const reason = `Required static asset validation failed for ${expectation.path}: ${validation.reason}`;
                self.offlineLog.error('ServiceWorker', reason);
                requiredFailures.push(reason);
                continue;
            }

            await staticCache.put(expectation.path, response.clone());
            cachedFiles.push(expectation.path);
        }

        for (const url of OPTIONAL_STATIC_FILES) {
            try {
                await staticCache.add(url);
                cachedFiles.push(url);
            } catch (error) {
                self.offlineLog.warn('ServiceWorker', `Optional static asset failed to cache: ${url}`, error);
            }
        }

        if (requiredFailures.length > 0) {
            throw new Error(requiredFailures.join(' | '));
        }
        
        // Log all successfully cached files in one consolidated message
        if (cachedFiles.length > 0) {
            self.offlineLog.log('ServiceWorker', `✅ Successfully cached ${cachedFiles.length} static files: ${cachedFiles.join(', ')}`);
        }
        
        self.offlineLog.log('ServiceWorker', 'Static file caching complete');
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Failed to cache static files for session:', error.message);
        throw error; // Re-throw to prevent offline mode activation
    }
}

// Function to cache API routes to session-specific cache
async function cacheApiRoutesForSession() {
    try {
        if (!API_CACHE_NAME) {
            throw new Error('Cannot cache API routes: API_CACHE_NAME is not initialized');
        }
        self.offlineLog.log('ServiceWorker', 'Caching API routes for session to:', API_CACHE_NAME);
        const apiCache = await caches.open(API_CACHE_NAME);
        
        const cachedRoutes = [];
        const requiredFailures = [];

        const routeCachePlan = [
            { fetchPath: '/Case', cachePaths: ['/Case', '/case'] },
            { fetchPath: '/Home/Index', cachePaths: ['/Home/Index', '/'] },
            { fetchPath: '/Account/OfflineLogin', cachePaths: ['/Account/OfflineLogin', '/Account/OfflineLogin/'] },
            { fetchPath: '/pdf-version/', cachePaths: ['/pdf-version', '/pdf-version/'], fetchOptions: { redirect: 'follow' } },
            { fetchPath: '/pdf-version/index.html', cachePaths: ['/pdf-version/index.html'] }
        ];

        for (const route of routeCachePlan) {
            const result = await cacheValidatedResponse(
                apiCache,
                route.fetchPath,
                route.cachePaths,
                REQUIRED_ROUTE_EXPECTATIONS,
                { fetchOptions: route.fetchOptions || {} }
            );

            if (!result.cached) {
                const reason = `Required route validation failed for ${route.fetchPath}: ${result.reason}`;
                if (result.blocked) {
                    self.offlineLog.error('ServiceWorker', reason);
                    requiredFailures.push(reason);
                } else {
                    self.offlineLog.warn('ServiceWorker', reason);
                }
                continue;
            }

            cachedRoutes.push(...route.cachePaths);
        }

        const cacheVersionPath = '/api/OfflineCase/cache-version';
        const cacheVersionResult = await cacheValidatedResponse(
            apiCache,
            cacheVersionPath,
            [cacheVersionPath],
            REQUIRED_API_EXPECTATIONS
        );
        if (!cacheVersionResult.cached) {
            const reason = `Required API route validation failed for ${cacheVersionPath}: ${cacheVersionResult.reason}`;
            self.offlineLog.error('ServiceWorker', reason);
            requiredFailures.push(reason);
        } else {
            cachedRoutes.push(cacheVersionPath);
        }

        if (requiredFailures.length > 0) {
            throw new Error(requiredFailures.join(' | '));
        }
        
        // Log all successfully cached routes in one consolidated message
        if (cachedRoutes.length > 0) {
            self.offlineLog.log('ServiceWorker', `✅ Cached ${cachedRoutes.length} API routes: ${cachedRoutes.join(', ')}`);
        }
        
        self.offlineLog.log('ServiceWorker', 'API routes caching complete for session:', self.OFFLINE_CACHE_ID);

    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Failed to cache API routes for session:', error.message);
        throw error;
    }
}

async function caseInsensitiveCacheMatch(request, cache) {
    const reqUrl = new URL(request.url);
    const cacheKeys = await cache.keys();
    for (const cachedRequest of cacheKeys) {
        const cachedUrl = new URL(cachedRequest.url);
        if (
            cachedUrl.pathname.toLowerCase() === reqUrl.pathname.toLowerCase() &&
            cachedUrl.search.toLowerCase() === reqUrl.search.toLowerCase()
        ) {
            return cache.match(cachedRequest);
        }
    }
    return undefined;
}

async function getCachedCaseResponseFromActiveSession(request) {
    const activeCacheName = await getActiveApiCacheName();
    const cache = await caches.open(activeCacheName);
    return await caseInsensitiveCacheMatch(request, cache);
}

// Function to clear all non-current session caches
// Since only one offline session is supported per browser, clear everything except current session
async function clearPreviousSessionCaches() {
    try {
        self.offlineLog.log('ServiceWorker', 'Clearing all non-current session caches...');
        const allCacheNames = await caches.keys();
        const cachesToClear = allCacheNames.filter(name => {
            // Clear all mmria caches that don't belong to current session
            if (name.startsWith('mmria-')) {
                // If we have a current session, keep only caches for that session
                if (self.OFFLINE_CACHE_ID) {
                    return !name.includes(`-session-${self.OFFLINE_CACHE_ID}`);
                }
                // If no current session, clear all mmria caches
                return true;
            }
            return false;
        });
        
        self.offlineLog.log('ServiceWorker', 'Found caches to clear:', cachesToClear);
        
        for (const cacheName of cachesToClear) {
            await caches.delete(cacheName);
            self.offlineLog.log('ServiceWorker', 'Cleared cache:', cacheName);
        }
        
        self.offlineLog.log('ServiceWorker', 'Cache cleanup complete');
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error clearing previous session caches:', error);
    }
}

// Helper function to lazily initialize cache names on-demand
// Called when cache names are null but needed for a fetch request
async function ensureCacheNamesInitialized() {
    try {
        // If already initialized, return immediately
        if (STATIC_CACHE_NAME && API_CACHE_NAME) {
            return true;
        }
        
        self.offlineLog.log('ServiceWorker', 'Cache names not initialized, attempting lazy initialization');
        
        // First ensure CACHE_VERSION_BASE is set
        if (!CACHE_VERSION_BASE) {
            self.offlineLog.log('ServiceWorker', 'CACHE_VERSION_BASE not set, fetching...');
            await fetchCacheVersionFromServer().catch(async (error) => {
                self.offlineLog.warn('ServiceWorker', 'API unavailable, detecting from caches');
                await detectExistingCacheVersion();
            });
        }
        
        // Then try to detect session info
        if (CACHE_VERSION_BASE && (!STATIC_CACHE_NAME || !API_CACHE_NAME)) {
            self.offlineLog.log('ServiceWorker', 'Version set but cache names missing, detecting session info');
            await detectAndSetSessionInfo();
        }
        
        // Verify initialization succeeded
        if (STATIC_CACHE_NAME && API_CACHE_NAME) {
            self.offlineLog.log('ServiceWorker', 'Lazy initialization successful');
            return true;
        }
        
        self.offlineLog.warn('ServiceWorker', 'Lazy initialization incomplete');
        return false;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error during lazy initialization:', error);
        return false;
    }
}

// Helper function to get the active API cache name
// Dynamically resolves the correct cache to use - handles browser reopen scenarios
// Now also populates global cache name variables as a side effect
async function getActiveApiCacheName() {
    try {
        // If CURRENT_SESSION_ID is set, use the session-specific cache name
        if (self.OFFLINE_CACHE_ID && CACHE_VERSION_BASE) {
            const sessionCacheName = `mmria-api-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
            return sessionCacheName;
        }
        
        // Try to initialize cache names if not set
        await ensureCacheNamesInitialized();
        
        // Check again after initialization attempt
        if (self.OFFLINE_CACHE_ID && CACHE_VERSION_BASE) {
            const sessionCacheName = `mmria-api-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
            return sessionCacheName;
        }
        
        // Otherwise, search for existing session-specific caches
        const allCacheNames = await caches.keys();
        const sessionCaches = allCacheNames.filter(name => 
            name.startsWith('mmria-api-') && name.includes('-session-')
        );
        
        if (sessionCaches.length > 0) {
            // Use the most recent session cache (last in the list)
            const cacheName = sessionCaches[sessionCaches.length - 1];
            self.offlineLog.log('ServiceWorker', 'Found existing session cache:', cacheName);
            
            // Extract and populate global variables from the found cache name
            const match = cacheName.match(/^mmria-api-(v\d+-\w+)-session-(.+)$/);
            if (match && !self.OFFLINE_CACHE_ID) {
                CACHE_VERSION_BASE = match[1];
                self.OFFLINE_CACHE_ID = match[2];
                STATIC_CACHE_NAME = `mmria-static-${CACHE_VERSION_BASE}-session-${self.OFFLINE_CACHE_ID}`;
                API_CACHE_NAME = cacheName;
                CACHE_VERSION_FETCHED = true;
                self.offlineLog.log('ServiceWorker', 'Populated globals from found cache:', {
                    version: CACHE_VERSION_BASE,
                    sessionId: self.OFFLINE_CACHE_ID
                });
            }
            
            return cacheName;
        }
        
        // Don't return null - throw error instead to prevent creating "null" cache
        self.offlineLog.error('ServiceWorker', 'No valid cache name available - cannot proceed');
        throw new Error('No valid cache name available - offline session not initialized');
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error resolving active API cache name:', error);
        throw error; // Re-throw to prevent cache creation with null name
    }
}



const offlineCacheManifest = self.OfflineCacheManifest || {};
const REQUIRED_STATIC_EXPECTATIONS = offlineCacheManifest.requiredStaticExpectations || [];
const OPTIONAL_STATIC_FILES = offlineCacheManifest.optionalStaticFiles || [];
const REQUIRED_ROUTE_EXPECTATIONS = offlineCacheManifest.requiredRouteExpectations || [];
const REQUIRED_API_EXPECTATIONS = offlineCacheManifest.requiredApiExpectations || [];
const STATIC_FILES = [
    ...REQUIRED_STATIC_EXPECTATIONS.map(item => item.path),
    ...OPTIONAL_STATIC_FILES
];
const CACHED_ROUTES = offlineCacheManifest.cachedRoutes || [];
const CACHED_API_ROUTES = offlineCacheManifest.cachedApiRoutes || [];

function getNormalizedContentType(response) {
    return ((response && response.headers && response.headers.get('content-type')) || '').toLowerCase();
}

function contentTypeMatches(contentType, expectedContentType) {
    if (!expectedContentType) {
        return true;
    }

    if (!contentType) {
        return false;
    }

    return contentType.indexOf(expectedContentType.toLowerCase()) >= 0;
}

function findManifestExpectation(pathname, expectations) {
    let normalizedPath = pathname;

    try {
        if (normalizedPath.indexOf('http://') === 0 || normalizedPath.indexOf('https://') === 0) {
            const url = new URL(normalizedPath);
            normalizedPath = url.pathname + url.search;
        }
    } catch (_error) {
        normalizedPath = pathname;
    }

    return (expectations || []).find(expectation => expectation.pattern.test(normalizedPath)) || null;
}

async function validateManifestResponse(response, expectation, requestPath) {
    if (!expectation) {
        return { valid: true };
    }

    if (!response) {
        return { valid: false, reason: `${requestPath} returned no response` };
    }

    if (typeof expectation.expectedStatus === 'number' && response.status !== expectation.expectedStatus) {
        return {
            valid: false,
            reason: `${requestPath} returned status ${response.status}; expected ${expectation.expectedStatus}`
        };
    }

    const contentType = getNormalizedContentType(response);
    if (!contentTypeMatches(contentType, expectation.expectedContentType)) {
        return {
            valid: false,
            reason: `${requestPath} returned content-type "${contentType || 'unknown'}"; expected ${expectation.expectedContentType}`
        };
    }

    const validation = expectation.validation || 'exists';
    try {
        switch (validation) {
            case 'asset_non_empty':
            case 'javascript_non_empty': {
                const bodyText = await response.clone().text();
                return bodyText.trim().length > 0
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} returned an empty response body` };
            }
            case 'html_shell': {
                const bodyText = (await response.clone().text()).toLowerCase();
                const hasHtmlShell = bodyText.indexOf('<html') >= 0 || bodyText.indexOf('<!doctype') >= 0;
                return hasHtmlShell
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not contain an HTML shell` };
            }
            case 'json_has_base_version': {
                const payload = await response.clone().json();
                return payload && typeof payload === 'object' && typeof payload.baseVersion === 'string' && payload.baseVersion.length > 0
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not include a baseVersion value` };
            }
            case 'json_has_form_design': {
                const payload = await response.clone().json();
                return payload && typeof payload === 'object' && Object.prototype.hasOwnProperty.call(payload, 'form_design')
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not include form_design` };
            }
            case 'json_has_children': {
                const payload = await response.clone().json();
                return payload && typeof payload === 'object' && Array.isArray(payload.children)
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not include a children array` };
            }
            case 'json_array_or_object': {
                const payload = await response.clone().json();
                const isValid = Array.isArray(payload) || (!!payload && typeof payload === 'object');
                return isValid
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not return a JSON object or array` };
            }
            case 'json_object': {
                const payload = await response.clone().json();
                return payload && typeof payload === 'object' && !Array.isArray(payload)
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not return a JSON object` };
            }
            case 'json_version_value': {
                const bodyText = (await response.clone().text()).trim();
                let payload = bodyText;

                try {
                    payload = JSON.parse(bodyText);
                } catch (_error) {
                    payload = bodyText;
                }

                const isValid =
                    (typeof payload === 'string' && payload.length > 0) ||
                    typeof payload === 'number' ||
                    (payload && typeof payload === 'object' && (
                        typeof payload.version === 'string' ||
                        typeof payload.release_version === 'string' ||
                        typeof payload.baseVersion === 'string'
                    ));
                return isValid
                    ? { valid: true }
                    : { valid: false, reason: `${requestPath} did not return a usable release version value` };
            }
            default:
                return { valid: true };
        }
    } catch (error) {
        return {
            valid: false,
            reason: `${requestPath} failed ${validation} validation: ${error.message}`
        };
    }
}

async function cacheValidatedResponse(cache, fetchPath, cachePaths, expectations, options = {}) {
    const response = await fetch(fetchPath, options.fetchOptions || {});
    const expectation = findManifestExpectation(fetchPath, expectations);
    const validation = await validateManifestResponse(response, expectation, fetchPath);

    if (!validation.valid) {
        return {
            cached: false,
            blocked: !expectation || expectation.blockOnFailure !== false,
            reason: validation.reason
        };
    }

    for (const cachePath of cachePaths) {
        await cache.put(cachePath, response.clone());
    }

    return {
        cached: true,
        blocked: false,
        response: response,
        expectation: expectation
    };
}

// Routes to exclude from caching
const EXCLUDED_ROUTES = [
    /\/api\/.*view.*pdf/i,
    /\/api\/.*save.*pdf/i,
    /\/print-version/i,
    /validate.*address/i,
    /geography.*context/i
];

self.addEventListener('install', event => {
    self.offlineLog.log('ServiceWorker', 'Installing...');
    
    // CRITICAL: Skip waiting IMMEDIATELY to take control as fast as possible during rapid refreshes
    self.skipWaiting();
    self.offlineLog.log('ServiceWorker', '⚡ skipWaiting() called immediately');
    
    event.waitUntil(
        // Fetch cache version from server (single source of truth)
        fetchCacheVersionFromServer()
            .catch(async (error) => {
                self.offlineLog.warn('ServiceWorker', 'API unavailable during install, attempting to detect existing caches');
                await detectExistingCacheVersion();
            })
            .then(() => {
                self.offlineLog.log('ServiceWorker', '✅ Cache version initialized:', CACHE_VERSION_BASE);
                self.offlineLog.log('ServiceWorker', 'Static files and API routes will be cached when offline session is initialized');
            })
            .catch(error => {
                self.offlineLog.error('ServiceWorker', 'Error during install:', error);
            })
    );
});

// Consolidated message handler for all service worker messages
let lastNetworkStatus = navigator.onLine;
self.addEventListener('message', event => {
    if (!event.data || !event.data.type) {
        self.offlineLog.warn('ServiceWorker', 'Received message without type');
        return;
    }

    const { type, data } = event.data;
    
    switch (type) {
        case 'NETWORK_STATUS_CHANGE':
            const newStatus = event.data.isOnline;
            self.offlineLog.log('ServiceWorker', 'Network status change detected:', lastNetworkStatus, '->', newStatus);
            
            if (lastNetworkStatus !== newStatus) {
                lastNetworkStatus = newStatus;
                
                // If going offline, ensure caches are preserved
                if (!newStatus) {
                    self.offlineLog.log('ServiceWorker', 'Going offline - preserving all caches');
                    // Don't delete any caches when going offline
                }
            }
            break;
            
        case 'OFFLINE_STATUS_UPDATE':
            self.offlineLog.log('ServiceWorker', 'Received offline status update, invalidating cache');
            self.cachedOfflineStatus = null;
            cachedActiveOfflineSession = null;
            lastStatusCheckTime = 0;
            break;
            
        case 'ACTIVE_OFFLINE_SESSION_UPDATE':
            self.offlineLog.log('ServiceWorker', 'Received active offline session update, invalidating cache');
            self.cachedOfflineStatus = null;
            cachedActiveOfflineSession = null;
            lastStatusCheckTime = 0;
            break;
            
        case 'GO_ONLINE_IMMEDIATE':
            self.offlineLog.log('ServiceWorker', 'Received GO_ONLINE_IMMEDIATE - setting cached status to online');
            self.cachedOfflineStatus = false; // Online
            cachedActiveOfflineSession = false; // No active offline session
            lastStatusCheckTime = Date.now();
            self.offlineLog.log('ServiceWorker', 'Immediate online status set');
            break;
            
        case 'INITIAL_STATUS_SETUP':
            self.offlineLog.log('ServiceWorker', 'Received initial status setup from main thread');
            if (event.data.offlineStatus !== undefined) {
                self.cachedOfflineStatus = event.data.offlineStatus;
            }
            if (event.data.activeOfflineSession !== undefined) {
                cachedActiveOfflineSession = event.data.activeOfflineSession;
            }
            lastStatusCheckTime = Date.now();
            break;

        case 'CACHE_CASE_DATA':
            (async () => {
                const success = await cacheCaseData(data.caseId, data.caseData);
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({
                        success: success,
                        caseId: data.caseId
                    });
                }
            })();
            break;
            
        case 'CACHE_METADATA':
        case 'CACHE_METADATA_RESOURCES':
            const version = data?.version || event.data.version;
            self.offlineLog.log('ServiceWorker', 'Caching metadata resources for version:', version);
            (async () => {
                try {
                    const result = await cacheMetadataResources(version);
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({
                            success: true,
                            version: version,
                            result: result
                        });
                    }
                } catch (error) {
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({
                            success: false,
                            version: version,
                            error: error.message
                        });
                    }
                }
            })();
            break;
            
        case 'CHECK_CRITICAL_RESOURCES':
            self.offlineLog.log('ServiceWorker', 'Checking critical resources cache for version:', data.version);
            checkCriticalResourcesCache(data.version).then(status => {
                event.ports[0].postMessage(status);
            });
            break;
            
        case 'SET_OFFLINE_SESSION_ID':
            self.OFFLINE_SESSION_ID = event.data.sessionId;
            self.offlineLog.log('ServiceWorker', `OFFLINE_SESSION_ID updated to: ${self.OFFLINE_SESSION_ID}`);
            break;
            
        case 'CLEAR_CACHES':
            clearAllCaches();
            break;
            
        case 'GET_CACHE_STATUS':
            getCacheStatus().then(status => {
                event.ports[0].postMessage(status);
            });
            break;
            
        case 'SKIP_WAITING':
            self.offlineLog.log('ServiceWorker', 'Received SKIP_WAITING message');
            self.skipWaiting();
            break;
            
        case 'CLAIM_CLIENTS':
            self.offlineLog.log('ServiceWorker', 'Received CLAIM_CLIENTS message');
            self.clients.claim();
            break;
            
        case 'INIT_OFFLINE_SESSION':
            self.offlineLog.log('ServiceWorker', 'Received INIT_OFFLINE_SESSION message');
            (async () => {
                const sessionId = data.sessionId || Date.now().toString();
                await initializeOfflineSessionCache(sessionId);
                // Clear previous session caches
                await clearPreviousSessionCaches();
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ 
                        success: true, 
                        sessionId: sessionId,
                        cacheNames: {
                            static: STATIC_CACHE_NAME,                        
                            api: API_CACHE_NAME
                        }
                    });
                }
            })();
            break;
            
        case 'GET_CURRENT_SESSION_INFO':
            self.offlineLog.log('ServiceWorker', 'Received GET_CURRENT_SESSION_INFO message');
            if (event.ports && event.ports[0]) {
                event.ports[0].postMessage({ 
                    sessionId: self.OFFLINE_CACHE_ID,
                    cacheVersion: CACHE_VERSION_BASE,
                    cacheNames: {
                        static: STATIC_CACHE_NAME,                        
                        api: API_CACHE_NAME
                    }
                });
            }
            break;
            
        case 'VALIDATE_OFFLINE_KEY':
            self.offlineLog.log('ServiceWorker', 'Received VALIDATE_OFFLINE_KEY message');
            validateOfflineKeyInServiceWorker(event.data.derivedKeyHash, event.data.sessionId, event);
            break;
            
        case 'CACHE_OFFLINE_SESSION_DATA':
            self.offlineLog.log('ServiceWorker', 'Received CACHE_OFFLINE_SESSION_DATA message');            
            handleCacheOfflineSessionData(event.data.data, event);
            break;
            
        case 'GET_OFFLINE_SESSION_DATA':
            self.offlineLog.log('ServiceWorker', 'Received GET_OFFLINE_SESSION_DATA message');
            getOfflineSessionDataFromServiceWorker(event);
            break;
            
        case 'SET_OFFLINE_ENCRYPTION_KEY':
            // Main thread sends a pre-derived key (raw secret never transmitted)
            self.offlineLog.log('ServiceWorker', 'Received SET_OFFLINE_ENCRYPTION_KEY');
            (async () => {
                try {
                    offlineCryptoKey = await crypto.subtle.importKey(
                        'raw',
                        event.data.keyBytes,
                        { name: 'AES-GCM' },
                        false,
                        ['encrypt', 'decrypt']
                    );
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({ success: true });
                    }
                } catch (err) {
                    self.offlineLog.error('ServiceWorker', 'Failed to import offline key', err);
                    offlineCryptoKey = null;
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({ success: false, error: err.message });
                    }
                }
            })();
            break;

        case 'OFFLINE_LOGOUT_ENCRYPT_CASES':
            self.offlineLog.log('ServiceWorker', 'Received OFFLINE_LOGOUT_ENCRYPT_CASES');
            (async () => {
                const success = await encryptAllOfflineCasesInCache();
                // Clear cached session status to force fresh check on next request
                self.cachedOfflineStatus = null;
                cachedActiveOfflineSession = null;
                lastStatusCheckTime = 0;
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ success });
                }
            })();
            break;

        case 'OFFLINE_LOGIN_DECRYPT_CASES':
            self.offlineLog.log('ServiceWorker', 'Received OFFLINE_LOGIN_DECRYPT_CASES');
            (async () => {
                const success = await decryptAllOfflineCasesInCache();
                // Clear cached session status to force fresh check on next request
                self.cachedOfflineStatus = null;
                cachedActiveOfflineSession = null;
                lastStatusCheckTime = 0;
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ success });
                }
            })();
            break;
            
        case 'KEEP_ALIVE':
            // Keep-alive ping to prevent service worker termination during heavy caching operations
            // No response needed - just receiving the message keeps the SW active
            break;
            
        default:
            self.offlineLog.log('ServiceWorker', 'Unknown message type:', type);
    }
});

self.addEventListener('activate', event => {
    self.offlineLog.log('ServiceWorker', 'Activating...');
    event.waitUntil(
        (async () => {
            // CRITICAL: Claim clients IMMEDIATELY before anything else to prevent request bypass during rapid refreshes
            await self.clients.claim();
            self.offlineLog.log('ServiceWorker', '✅ Clients claimed - now controlling all pages');
            
            // Ensure cache version is initialized before cleaning up caches
            if (!CACHE_VERSION_BASE) {
                self.offlineLog.warn('ServiceWorker', 'CACHE_VERSION_BASE not set during activate, attempting to detect');
                try {
                    await detectExistingCacheVersion();
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Cannot detect cache version during activate:', error);
                    return;
                }
            }
            
            // Clean up all caches except current session
            // Only one offline session is supported per browser
            const cacheNames = await caches.keys();
            self.offlineLog.log('ServiceWorker', 'Found existing caches:', cacheNames);
            await Promise.all(
                cacheNames.map(async cacheName => {
                    // Delete all mmria caches that don't belong to current session
                    if (cacheName.startsWith('mmria-')) {
                        const isCurrentSession = self.OFFLINE_CACHE_ID && cacheName.includes(`-session-${self.OFFLINE_CACHE_ID}`);
                        
                        if (!isCurrentSession) {
                            self.offlineLog.log('ServiceWorker', 'Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    }
                })
            );
            
            // Debug: Check what's in the cache after activation
            await debugCacheStatus();
            
            self.offlineLog.log('ServiceWorker', '✅ Activation complete - ready to intercept all requests');
        })()
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    const pathname = url.pathname;
    const fullUrl = event.request.url;
    const isOffline = !navigator.onLine;

    // Debug logging for ALL requests when offline
    if (isOffline) {
        self.offlineLog.log('ServiceWorker', '🔌 OFFLINE MODE - Intercepting ALL requests:', {
            url: fullUrl,
            method: event.request.method,
            pathname: pathname,
            origin: url.origin,
            host: url.host
        });
    }

    // Debug logging for print.css specifically
    if (pathname.includes('print.css')) {
        self.offlineLog.log('ServiceWorker', 'Fetch event for print.css detected:', {
            pathname: pathname,
            method: event.request.method,
            url: fullUrl,
            isOffline: isOffline
        });
    }

    // Log all fetch events when offline for debugging
    if (isOffline) {
        self.offlineLog.log('ServiceWorker', '🔌 OFFLINE - Intercepting request:', {
            url: fullUrl,
            method: event.request.method,
            pathname: pathname
        });
    }

    // Skip non-GET/POST requests for API endpoints
    if (event.request.method !== 'GET' && 
        !(event.request.method === 'POST' && 
          (pathname.startsWith('/api/') || pathname.startsWith('/Case/')))) {
        return;
    }

    // Check if route should be excluded
    if (EXCLUDED_ROUTES.some(pattern => pattern.test(fullUrl))) {
        self.offlineLog.log('ServiceWorker', 'Skipping excluded route:', fullUrl);
        return;
    }

    // Skip caching for network connectivity checks - always go to network
    if (url.searchParams.has('connectivity_check') || 
        url.pathname.includes('/OfflineCase/connectivity-check')) {
        self.offlineLog.log('ServiceWorker', 'Skipping cache for connectivity check:', fullUrl);
        return; // Let the request go directly to the network
    }
    // Skip caching for network connectivity checks - always go to network
    if (url.searchParams.has('save-offline-log-data') || 
        url.pathname.includes('/api/logger/save-offline-log-data')) {
        self.offlineLog.log('ServiceWorker', 'Skipping cache for /api/logger/save-offline-log-data:', fullUrl);
        return; // Let the request go directly to the network
    }
    // Skip caching for network connectivity checks - always go to network
    if (url.searchParams.has('OfflineCase/update-cases') || 
        url.pathname.includes('/api/OfflineCase/update-cases/')) {
        self.offlineLog.log('ServiceWorker', 'Skipping cache for /api/OfflineCase/update-cases/:', fullUrl);
        return; // Let the request go directly to the network
    }

    // Skip caching for offline case setup POST requests - these need to go to server
    // BUT allow them through the service worker for proper credential handling
    if (event.request.method === 'POST' && 
        (url.pathname === '/api/OfflineCase' || url.pathname.startsWith('/api/OfflineCase/'))) {
        self.offlineLog.log('ServiceWorker', 'Passing through offline case API request to server:', fullUrl);
        event.respondWith(
            fetch(event.request, { credentials: 'same-origin' })
        );
        return;
    }

    // Intercept login route and redirect to offline login when in offline mode
    if (url.pathname.toLowerCase() === '/account/login') {
        event.respondWith(
            (async () => {
                try {
                    const isOfflineMode = await isUserInOfflineMode();
                    if (isOfflineMode) {
                        self.offlineLog.log('ServiceWorker', 'User in offline mode, redirecting to offline login');
                        return Response.redirect('/Account/OfflineLogin', 302);
                    } else {
                        self.offlineLog.log('ServiceWorker', 'User not in offline mode, allowing normal login');
                        // Let the request go through normally
                        return fetch(event.request);
                    }
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Error checking offline status for login redirect:', error);
                    // If we can't determine offline status, let it go through normally
                    return fetch(event.request);
                }
            })()
        );
        return;
    }

    // Handle static files
    if (STATIC_FILES.includes(pathname)) {
        // Special debugging for print.css
        if (pathname.includes('print.css')) {
            self.offlineLog.log('ServiceWorker', 'Processing print.css request:', pathname);
            self.offlineLog.log('ServiceWorker', 'STATIC_FILES includes print.css:', STATIC_FILES.includes(pathname));
        }
        
        event.respondWith(
            // Cache-first strategy for static files - try current cache first, then backup
            (async () => {
                try {
                    // Try current session's static cache
                    if (STATIC_CACHE_NAME) {
                        const currentCache = await caches.open(STATIC_CACHE_NAME);
                        let cachedResponse = await currentCache.match(event.request);
                        if (cachedResponse) {
                            self.offlineLog.log('ServiceWorker', '✅ Serving static file from cache:', pathname);
                            return cachedResponse;
                        }
                        self.offlineLog.log('ServiceWorker', 'Static file not in session cache:', pathname);
                    } else {
                        // LAZY INITIALIZATION: Try to initialize cache names on-demand
                        self.offlineLog.log('ServiceWorker', 'STATIC_CACHE_NAME is null, attempting lazy initialization');
                        const initialized = await ensureCacheNamesInitialized();
                        
                        if (initialized && STATIC_CACHE_NAME) {
                            self.offlineLog.log('ServiceWorker', 'Lazy initialization successful, retrying cache lookup');
                            const currentCache = await caches.open(STATIC_CACHE_NAME);
                            let cachedResponse = await currentCache.match(event.request);
                            if (cachedResponse) {
                                self.offlineLog.log('ServiceWorker', '✅ Serving static file from cache after lazy init:', pathname);
                                return cachedResponse;
                            }
                        } else {
                            self.offlineLog.log('ServiceWorker', 'Lazy initialization failed, static file not available:', pathname);
                        }
                    }
                    
                    // Only try network if online
                    if (!isOffline) {
                        self.offlineLog.log('ServiceWorker', 'Online - trying network for static file:', pathname);
                        return fetch(event.request)
                            .then(networkResponse => {
                                // If successful, cache the response for future use
                                if (networkResponse.ok) {
                                    currentCache.put(event.request, networkResponse.clone());
                                    self.offlineLog.log('ServiceWorker', '✅ Cached static file from network:', pathname);
                                    return networkResponse;
                                } else {
                                    self.offlineLog.warn('ServiceWorker', `Network returned ${networkResponse.status} for:`, pathname);
                                    return networkResponse;
                                }
                            })
                            .catch(error => {
                                self.offlineLog.error('ServiceWorker', '❌ Network failed for static file:', pathname, error.message);
                                // Fall through to offline fallback
                                return createOfflineFallback(pathname, currentCache);
                            });
                    } else {
                        self.offlineLog.log('ServiceWorker', '🔌 OFFLINE - providing fallback for static file:', pathname);
                        return createOfflineFallback(pathname, currentCache);
                    }
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Error in static file handler:', error);
                    return createOfflineFallback(pathname, currentCache);
                }
            })()
        );
        return;
    }

    // Helper function to create offline fallbacks
    async function createOfflineFallback(pathname, cache) {
        try {
            // Provide appropriate fallbacks based on file type
            if (pathname.endsWith('.css')) {
                const fallbackResponse = new Response('/* CSS file not available offline */', { 
                    status: 200, 
                    headers: { 'Content-Type': 'text/css' }
                });
                // Cache the fallback for future requests
                if (cache) {
                    await cache.put(event.request, fallbackResponse.clone());
                }
                return fallbackResponse;
            }
            
            if (pathname.endsWith('.js')) {
                const fallbackResponse = new Response('// JavaScript file not available offline\nconsole.warn("Script not available offline: ' + pathname + '");', { 
                    status: 200, 
                    headers: { 'Content-Type': 'application/javascript' }
                });
                // Cache the fallback for future requests
                if (cache) {
                    await cache.put(event.request, fallbackResponse.clone());
                }
                return fallbackResponse;
            }
            
            if (pathname.includes('favicon') || pathname.endsWith('.ico') || pathname.endsWith('.png') || pathname.endsWith('.svg')) {
                // Return empty response for images/icons to avoid broken image displays
                return new Response('', { 
                    status: 204, 
                    statusText: 'No Content' 
                });
            }
            
            // Return a basic 404 response for other static files
            return new Response('File not found offline', { 
                status: 404, 
                statusText: 'Not Found' 
            });
        } catch (error) {
            self.offlineLog.error('ServiceWorker', 'Error creating offline fallback:', error);
            return new Response('File not available offline', { status: 404 });
        }
    }

    // Handle font files (may have query parameters)
    if (pathname.includes('/assets/fonts/')) {
        // Extract the base filename without query parameters
        const fontFileName = pathname.split('/').pop().split('?')[0];
        const matchingFontFile = STATIC_FILES.find(file => 
            file.includes('/assets/fonts/') && file.endsWith(fontFileName)
        );
        
        if (matchingFontFile) {
            event.respondWith(
                // Try all available caches for font files
                (async () => {
                    try {
                        // Try current session's static cache
                        if (STATIC_CACHE_NAME) {
                            let currentCache = await caches.open(STATIC_CACHE_NAME);
                            let cachedResponse = await currentCache.match(matchingFontFile);
                            if (cachedResponse) {
                                self.offlineLog.log('ServiceWorker', 'Serving font from cache:', matchingFontFile);
                                return cachedResponse;
                            }
                            self.offlineLog.log('ServiceWorker', 'Font not in session cache:', matchingFontFile);
                        } else {
                            // LAZY INITIALIZATION: Try to initialize cache names on-demand
                            self.offlineLog.log('ServiceWorker', 'STATIC_CACHE_NAME is null for font, attempting lazy initialization');
                            const initialized = await ensureCacheNamesInitialized();
                            
                            if (initialized && STATIC_CACHE_NAME) {
                                self.offlineLog.log('ServiceWorker', 'Lazy initialization successful for font, retrying');
                                let currentCache = await caches.open(STATIC_CACHE_NAME);
                                let cachedResponse = await currentCache.match(matchingFontFile);
                                if (cachedResponse) {
                                    self.offlineLog.log('ServiceWorker', 'Serving font from cache after lazy init:', matchingFontFile);
                                    return cachedResponse;
                                }
                            } else {
                                self.offlineLog.log('ServiceWorker', 'Lazy initialization failed, font not available:', matchingFontFile);
                            }
                        }
                        
                        // Only try network if online
                        if (!isOffline) {
                            self.offlineLog.log('ServiceWorker', 'Online - trying network for font:', matchingFontFile);
                            return fetch(event.request).then(fetchResponse => {
                                if (fetchResponse.ok) {
                                    currentCache.put(event.request, fetchResponse.clone());
                                    self.offlineLog.log('ServiceWorker', 'Cached font from network:', matchingFontFile);
                                }
                                return fetchResponse;
                            }).catch(error => {
                                self.offlineLog.log('ServiceWorker', 'Failed to fetch font file:', pathname, error);
                                // Return a 404 for missing fonts
                                return new Response('Font not found', { 
                                    status: 404, 
                                    statusText: 'Not Found' 
                                });
                            });
                        } else {
                            self.offlineLog.log('ServiceWorker', '🔌 OFFLINE - font not available:', pathname);
                            return new Response('Font not available offline', { 
                                status: 404, 
                                statusText: 'Not Found' 
                            });
                        }
                    } catch (error) {
                        self.offlineLog.error('ServiceWorker', 'Error in font file handler:', error);
                        return new Response('Font not available', { status: 404 });
                    }
                })()
            );
            return;
        }
    }

    // Handle API requests
    if (pathname.startsWith('/api/') || pathname.startsWith('/_users/') || pathname.startsWith('/Case/')) {
        self.offlineLog.log('ServiceWorker', `🎯 Routing API request to handleApiRequest: ${pathname}`);
        event.respondWith(
            handleApiRequest(event.request)
        );
        return;
    }

    // Handle page routes
    if (CACHED_ROUTES.some(pattern => pattern.test(pathname))) {
        event.respondWith(
            handlePageRequest(event.request)
        );
        return;
    }



    // Catch-all handler for any remaining requests when offline
    if (isOffline) {
        self.offlineLog.log('ServiceWorker', '🔌 OFFLINE - Handling unmatched request:', fullUrl);
        event.respondWith(
            (async () => {
                try {
                    // Try to find in any cache
                    const cachedResponse = await caches.match(event.request);
                    if (cachedResponse) {
                        self.offlineLog.log('ServiceWorker', '✅ Serving unmatched request from cache:', fullUrl);
                        return cachedResponse;
                    }

                    // Try backup caches
                    const allCacheNames = await caches.keys();
                    for (const cacheName of allCacheNames) {
                        if (cacheName.startsWith('mmria-')) {
                            const cache = await caches.open(cacheName);
                            const fallbackResponse = await caseInsensitiveCacheMatch(event.request, cache);
                            if (fallbackResponse) {
                                self.offlineLog.log('ServiceWorker', '✅ Serving unmatched request from fallback cache:', cacheName, fullUrl);
                                return fallbackResponse;
                            }
                        }
                    }

                    // Provide fallback based on content type
                    if (pathname.endsWith('.js')) {
                        return new Response('// Script not available offline', {
                            status: 200,
                            headers: { 'Content-Type': 'application/javascript' }
                        });
                    }
                    if (pathname.endsWith('.css')) {
                        return new Response('/* Stylesheet not available offline */', {
                            status: 200,
                            headers: { 'Content-Type': 'text/css' }
                        });
                    }
                    if (pathname.endsWith('.json') || pathname.startsWith('/api/')) {
                        return new Response(JSON.stringify({ error: 'Not available offline' }), {
                            status: 503,
                            headers: { 'Content-Type': 'application/json' }
                        });
                    }

                    // Default fallback
                    self.offlineLog.log('ServiceWorker', '❌ No cache available for offline request:', fullUrl);
                    return new Response('Not available offline', {
                        status: 503,
                        statusText: 'Service Unavailable'
                    });
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Error in catch-all handler:', error);
                    return new Response('Service worker error', { status: 500 });
                }
            })()
        );
        return;
    }

    // When online, let unmatched requests go to network
    self.offlineLog.log('ServiceWorker', 'Online - letting unmatched request go to network:', fullUrl);
});

// Handle API requests with cache-first strategy
async function handleApiRequest(request) {
    const url = new URL(request.url);
    const fullUrl = request.url;
    const pathWithQuery = url.pathname + url.search;
    const isCaseRequest = url.pathname === '/api/case' && url.searchParams.has('case_id');
    const isOffline = await isUserInOfflineMode();
    const hasActiveSession = await hasActiveOfflineSession();
    const isSteadyStateOffline = isOffline && hasActiveSession;

    // During Go Offline setup, case requests must come from network only.
    if (isCaseRequest && !isSteadyStateOffline) {
        const response = await fetch(request);

        // While preparing to go offline, only write case data into the active session cache.
        if (response.ok && request.method === 'GET' && offlineCryptoKey) {
            try {
                const activeCacheName = await getActiveApiCacheName();
                const cache = await caches.open(activeCacheName);
                let responseToCache = response.clone();

                try {
                    responseToCache = await encryptResponseBody(responseToCache);
                } catch (err) {
                    self.offlineLog.error('ServiceWorker', 'Failed to encrypt case response from network during setup, caching plaintext:', err);
                }

                await cache.put(request, responseToCache.clone());
            } catch (cacheError) {
                self.offlineLog.error('ServiceWorker', 'Failed to persist case response during offline setup:', cacheError);
            }
        }

        return response;
    }
    
    // FAST PATH: Immediately check if this request should use cache-first strategy
    const shouldUseCache = CACHED_API_ROUTES.some(pattern => {
        if (typeof pattern === 'string') {
            return pathWithQuery.includes(pattern) || fullUrl.includes(pattern);
        } else {
            return pattern.test(pathWithQuery);
        }
    });
    
    // If should use cache, try cache FIRST before any expensive async operations
    if (shouldUseCache) {
        // Special handling for offline-documents endpoint - always return cached case list
        if (url.pathname === '/api/case_view/offline-documents') {
            try {
                // Get cached cases from storage
                const offlineDocuments = await getCachedOfflineCaseList();
                return offlineDocuments;
            } catch (error) {
                self.offlineLog.error('ServiceWorker', 'Error getting cached offline documents:', error);
                self.offlineLog.error('ServiceWorker', 'Error stack:', error.stack);
                
                // Return empty list as fallback with proper structure
                return new Response(
                    JSON.stringify({
                        total_rows: 0,
                        offset: 0,
                        rows: []
                    }),
                    {
                        status: 200,
                        headers: { 'Content-Type': 'application/json' }
                    }
                );
            }
        }
        
        // Try cache first for other endpoints (FAST PATH - no async operations yet!)
        if (STATIC_CACHE_NAME === null || API_CACHE_NAME === null) {
            self.offlineLog.error('ServiceWorker', '🚨 SW RESTART: Cache names NULL - encryption key lost!');
        }
        
        const cachedResponse = isCaseRequest
            ? await getCachedCaseResponseFromActiveSession(request)
            : await caches.match(request);
        
        if (cachedResponse) {

            const urlPath = new URL(request.url).pathname;

            // 🔐 If it's a cached case and encrypted, decrypt on read
            if (urlPath === '/api/case' && cachedResponse.headers.get(OFFLINE_ENCRYPTION_HEADER) === '1') {
                if (!offlineCryptoKey) {
                    self.offlineLog.error('ServiceWorker', '🚨 ENCRYPTION KEY MISSING: offlineCryptoKey is NULL - cannot decrypt cached case');
                    self.offlineLog.error('ServiceWorker', '🔐 This typically means the service worker restarted and lost the in-memory encryption key');
                    self.offlineLog.warn('ServiceWorker', 'Returning 401 to trigger re-login and key re-establishment');
                    return new Response(
                        JSON.stringify({
                            error: 'offline_key_required',
                            message: 'Encrypted offline case data is locked. Please re-enter your offline key.'
                        }),
                        { status: 401, headers: { 'Content-Type': 'application/json' } }
                    );
                }
                try {
                    return await decryptResponseBody(cachedResponse);
                } catch (err) {
                    self.offlineLog.error('ServiceWorker', 'Failed to decrypt cached case response', err);
                    if (!isSteadyStateOffline) {
                        return await fetch(request);
                    }
                    return new Response(
                        JSON.stringify({
                            error: 'offline_decrypt_failed',
                            message: 'Unable to decrypt offline case data. You may need to reset offline cache and re-download.'
                        }),
                        { status: 500, headers: { 'Content-Type': 'application/json' } }
                    );
                }
            }

            // Non-case or non-encrypted response: return as-is
            return cachedResponse;
        }
        
        // SLOW PATH: Cache miss - now do expensive async operations
        // Cache miss, try network only if online
        if (!isOffline) {
            try {
                const response = await fetch(request);
                
                // Cache successful responses for future use (only GET requests can be cached)
                if (response.ok && request.method === 'GET') {
                    const activeCacheName = await getActiveApiCacheName();
                    const cache = await caches.open(activeCacheName);

                    let responseToCache = response.clone();

                    // 🔐 If this is a case endpoint and we have a key, store encrypted
                    const urlPath = new URL(request.url).pathname;
                    if (urlPath === '/api/case' && offlineCryptoKey) {
                        try {
                            responseToCache = await encryptResponseBody(responseToCache);
                            self.offlineLog.log('ServiceWorker', 'Cached case response ENCRYPTED from network');
                        } catch (err) {
                            self.offlineLog.error('ServiceWorker', 'Failed to encrypt case response from network, caching plaintext:', err);
                        }
                    }

                    cache.put(request, responseToCache.clone());
                }
                
                return response;
                
            } catch (error) {
                self.offlineLog.log(`ServiceWorker`, `Network failed for cached route: ${request.url}`, error);
                // Fall through to fallback handling below
            }
        }
    } else {
        // For non-cached routes, we need to check online status
        // Use network-first strategy only if online
        if (!isOffline) {
            try {
                const response = await fetch(request);
                return response;
                
            } catch (error) {
                self.offlineLog.log('ServiceWorker', 'Network failed, trying cache for:', request.url);
                
                // For case API requests that aren't in offline mode, let the network error propagate
                // This allows prefetch operations to handle failures gracefully without service worker interference
                if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
                    const isOfflineMode = await isInOfflineMode();
                    if (!isOfflineMode) {
                        self.offlineLog.log('ServiceWorker', 'Not in offline mode, letting case API network error propagate naturally');
                        throw error; // Let the fetch failure propagate to the caller
                    }
                }
                
                // Network failed, try cache
                const cachedResponse = isCaseRequest
                    ? await getCachedCaseResponseFromActiveSession(request)
                    : await caches.match(request);
                if (cachedResponse) {
                    return cachedResponse;
                }
                
                // Fall through to fallback handling below
            }
        } else {
            // When offline, try cache first for all routes
            const cachedResponse = isCaseRequest
                ? await getCachedCaseResponseFromActiveSession(request)
                : await caches.match(request);
            if (cachedResponse) {
                return cachedResponse;
            }
            
            // Fall through to fallback handling below
        }
    }
    
    // Handle cache-version endpoint (required for cache version management)
    if (url.pathname === '/api/OfflineCase/cache-version') {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedResponse = await cache.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, return error - no hardcoded fallback to avoid version mismatch
        self.offlineLog.log('ServiceWorker', 'Cache-version endpoint not available, returning error');
        return new Response(
            JSON.stringify({
                error: 'Cache version not available offline',
                message: 'Cache version endpoint must be cached before going offline'
            }),
            {
                status: 503,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }


    // Fallback handling for when both cache and network fail
    // Handle jurisdiction_tree endpoint specially (required for user info)
    if (url.pathname === '/api/jurisdiction_tree') {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedResponse = await cache.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        self.offlineLog.log('ServiceWorker', 'No cached jurisdiction_tree, providing fallback');
        return new Response(
            JSON.stringify({ 
                _id: "jurisdiction_tree",
                _rev: "offline-rev",
                name: "/",
                date_created: new Date().toISOString(),
                created_by: "offline-mode",
                date_last_updated: new Date().toISOString(),
                last_updated_by: "offline-mode",
                children: [{
                    _id: "offline-jurisdiction",
                    name: "offline",
                    title: "Offline Jurisdiction",
                    is_enabled: true,
                    parent_id: "/",
                    children: []
                }],
                data_type: "jursidiction_tree"
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    if (url.pathname === '/api/cvsAPI') {
        return new Response(
            JSON.stringify({ 
                success: false,
                message: 'CVS API not available offline'
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle release-version endpoint - try cache first, then fallback
    if (url.pathname === '/api/version/release-version') {
        // First try to get the cached version from when we were online
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedResponse = await cache.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide a reasonable fallback
        // This will only be used if the user goes offline before ever fetching the version
        self.offlineLog.log('ServiceWorker', 'No cached release-version, providing default fallback');
        return new Response(
            '"25.08.14"', // Default fallback - real version should be cached when online
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle ui_specification endpoint (returns minimal UI specification for offline)
    if (url.pathname.includes('/api/version/') && url.pathname.endsWith('/ui_specification')) {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide a minimal fallback
        self.offlineLog.log('ServiceWorker', 'No cached ui_specification, providing minimal fallback');
        return new Response(
            JSON.stringify({
                _id: "offline_ui_specification",
                data_type: "ui-specification",
                date_created: new Date().toISOString(),
                created_by: "offline-mode",
                date_last_updated: new Date().toISOString(),
                last_updated_by: "offline-mode",
                name: "offline_ui_specification",
                dimension: {
                    width: 1100
                },
                form_design: {}
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle metadata endpoint (returns minimal metadata structure for offline)
    if (url.pathname.includes('/api/version/') && url.pathname.endsWith('/metadata')) {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            cachedResponse = await cache.match(url.pathname);
        }
        if (!cachedResponse) {
            cachedResponse = await cache.match(request.url);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // Debug: List all cached requests to see what we have
        const allRequests = await cache.keys();
        self.offlineLog.log('ServiceWorker', 'Available cached requests:');
        allRequests.forEach((req, index) => {
            if (req.url.includes('metadata')) {
                self.offlineLog.log('ServiceWorker', `  ${index + 1}. ${req.url} (METADATA)`);
            }
        });
        
        // If not cached, provide a minimal fallback
        self.offlineLog.log('ServiceWorker', 'No cached metadata, providing minimal fallback');
        return new Response(
            JSON.stringify({
                _id: "offline_metadata",
                data_type: "form",
                date_created: new Date().toISOString(),
                created_by: "offline-mode",
                date_last_updated: new Date().toISOString(),
                last_updated_by: "offline-mode",
                name: "offline",
                prompt: "Offline Mode Case Form",
                type: "app",
                children: []
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle validation endpoint specially (returns JavaScript, not JSON)
    if (url.pathname.includes('/api/version/') && url.pathname.endsWith('/validation')) {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide a minimal fallback
        self.offlineLog.log('ServiceWorker', 'No cached validation script, providing minimal fallback');
        return new Response(
            `// Validation script not available offline
            console.log('Validation script not available in offline mode');
            // Minimal validation functions to prevent errors
            var g_validator = function() { return true; };
            var validation = { validate: function() { return []; } };`,
            {
                status: 200,
                headers: { 'Content-Type': 'application/javascript' }
            }
        );
    }
    
    // Handle version_specification endpoint specially (returns JavaScript, not JSON)
    if (url.pathname === '/api/metadata/version_specification') {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide a minimal fallback
        self.offlineLog.log('ServiceWorker', 'No cached version specification script, providing minimal fallback');
        return new Response(
            `// Version specification script not available offline
            console.log('Version specification script not available in offline mode');
            // Minimal version specification fallback
            var g_version_specification = { version: 'offline' };`,
            {
                status: 200,
                headers: { 'Content-Type': 'application/javascript' }
            }
        );
    }
    
    // Handle GetFormAccess endpoint specially (required for case access)
    if (url.pathname === '/_users/GetFormAccess') {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        self.offlineLog.log('ServiceWorker', 'No cached GetFormAccess, providing fallback');
        return new Response(
            JSON.stringify({ 
                _id: "form-access-list",
                created_by: "offline-mode",
                date_created: new Date().toISOString(),
                last_updated_by: "offline-mode", 
                date_last_updated: new Date().toISOString(),
                access_list: [
                    { form_path: "/tracking", abstractor: "view, edit", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/demographic", abstractor: "view, edit", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/outcome", abstractor: "view, edit", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/cause_of_death", abstractor: "view, edit", data_analyst: "view", committee_member: "view, edit", vro: "no_access" },
                    { form_path: "/preparer_remarks", abstractor: "view, edit", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/committee_review", abstractor: "view", data_analyst: "view", committee_member: "view, edit", vro: "no_access" },
                    { form_path: "/vro_case_determination", abstractor: "view", data_analyst: "view", committee_member: "view", vro: "view, edit" },
                    { form_path: "/ije_dc", abstractor: "view", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/ije_bc", abstractor: "view", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/ije_fetaldc", abstractor: "view", data_analyst: "view", committee_member: "view", vro: "no_access" },
                    { form_path: "/amss_tracking", abstractor: "view, edit", data_analyst: "view", committee_member: "view, edit", vro: "no_access" }
                ]
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle my-user endpoint specially (required for user info)
    if (url.pathname === '/api/user/my-user') {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        self.offlineLog.log('ServiceWorker', 'No cached my-user, providing fallback');
        return new Response(
            JSON.stringify({ 
                id: "offline-user",
                user_name: "offline-user",
                first_name: "Offline",
                last_name: "User", 
                email: "offline@localhost",
                roles: ["abstractor"],
                jurisdiction_id: "offline",
                is_active: true
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle my-roles endpoint specially (required for user role/jurisdiction info)
    if (url.pathname === '/api/user_role_jurisdiction_view/my-roles') {
        // First try to get from cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        self.offlineLog.log('ServiceWorker', 'No cached my-roles, providing fallback');
        return new Response(
            JSON.stringify({
                total_rows: 1,
                offset: 0,
                rows: [
                    {
                        id: "offline-user-role",
                        key: "offline-user",
                        value: {
                            _id: "offline-user-role",
                            user_id: "offline-user",
                            role_name: "abstractor",
                            jurisdiction_id: "offline",
                            is_active: true,
                            effective_start_date: new Date().toISOString(),
                            effective_end_date: null,
                            created_by: "offline-mode",
                            date_created: new Date().toISOString(),
                            last_updated_by: "offline-mode",
                            date_last_updated: new Date().toISOString()
                        }
                    }
                ]
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle GetDuplicateMultiFormList endpoint (returns empty list offline)
    if (url.pathname === '/Case/GetDuplicateMultiFormList') {
        return new Response(
            JSON.stringify({
                _id: "duplicate-multiform-list-offline",
                field_list: [] // Return empty array - no duplicate fields in offline mode
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Handle isDuplicateCase endpoint (returns true for offline mode)
    if (url.pathname === '/api/isDuplicateCase') {
        self.offlineLog.log('ServiceWorker', 'isDuplicateCase endpoint intercepted - returning true for offline mode');
        return new Response(
            'false',
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // Default offline response
    return new Response(
        JSON.stringify({ 
            error: 'Resource not available offline',
            message: 'This resource is not cached for offline use'
        }),
        {
            status: 503,
            headers: { 'Content-Type': 'application/json' }
        }
    );
}

// Helper function to check if offline session data exists in cache (for fallback)
async function hasOfflineSessionInCache() {
    try {
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedRequests = await cache.keys();
        
        // Look for offline session data entries
        for (const request of cachedRequests) {
            const url = request.url;
            if (url.includes('/offline-session-data/')) {
                self.offlineLog.log('ServiceWorker', 'Found offline session data in cache:', url);
                return true;
            }
        }
        
        self.offlineLog.log('ServiceWorker', 'No offline session data found in cache');
        return false;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error checking cache for offline session data:', error);
        return false;
    }
}

// Helper function to check if user is in offline mode via message passing
async function isUserInOfflineMode() {
    try {
        const currentTime = Date.now();
        
        // Return cached status if it's still fresh (within cache duration)
        if (self.cachedOfflineStatus !== null && 
            (currentTime - lastStatusCheckTime) < STATUS_CACHE_DURATION) {
            return self.cachedOfflineStatus;
        }
        
        // We can't access localStorage directly in service worker
        // Instead, we'll use message passing to get the status from the main thread
        const clients = await self.clients.matchAll();
        
        if (clients.length === 0) {
            // If we have a previous cached value, use it as fallback
            if (self.cachedOfflineStatus !== null) {
                return self.cachedOfflineStatus;
            }
            // Check if offline session data exists in cache (user previously went offline)
            const hasOfflineSession = await hasOfflineSessionInCache();
            self.cachedOfflineStatus = hasOfflineSession;
            lastStatusCheckTime = currentTime;
            return hasOfflineSession;
        }
        
        // Ask the first available client for offline status
        return new Promise((resolve) => {
            const messageChannel = new MessageChannel();
            
            messageChannel.port1.onmessage = (event) => {
                if (event.data && event.data.type === 'OFFLINE_STATUS_RESPONSE') {
                    const isOfflineMode = event.data.isOffline === true;
                    // Cache the result
                    self.cachedOfflineStatus = isOfflineMode;
                    lastStatusCheckTime = currentTime;
                    
                    resolve(isOfflineMode);
                } else {
                    // Use cached value if available, otherwise default to false
                    const fallbackStatus = self.cachedOfflineStatus !== null ? self.cachedOfflineStatus : false;
                    resolve(fallbackStatus);
                }
            };
            
            // Send request to client
            clients[0].postMessage({
                type: 'GET_OFFLINE_STATUS'
            }, [messageChannel.port2]);
            
            // Timeout after 1 second, with intelligent fallback
            setTimeout(async () => {
                // Use cached value if available
                if (self.cachedOfflineStatus !== null) {
                    resolve(self.cachedOfflineStatus);
                    return;
                }
                
                // Otherwise, check if offline session data exists in cache
                const hasOfflineSession = await hasOfflineSessionInCache();
                // Cache the detected status
                self.cachedOfflineStatus = hasOfflineSession;
                lastStatusCheckTime = currentTime;
                
                resolve(hasOfflineSession);
            }, 1000);
        });
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error checking offline session status:', error);
        // Try to use cache check as fallback on error
        try {
            return await hasOfflineSessionInCache();
        } catch (cacheError) {
            self.offlineLog.error('ServiceWorker', 'Error fallback also failed:', cacheError);
            // Last resort: use cached value if available, otherwise default to false
            return self.cachedOfflineStatus !== null ? self.cachedOfflineStatus : false;
        }
    }
}

// Helper function to check if user has an active offline session (is logged in to offline mode)
async function hasActiveOfflineSession() {
    try {
        const currentTime = Date.now();
        
        // Return cached status if it's still fresh (within cache duration)
        if (cachedActiveOfflineSession !== null && 
            (currentTime - lastStatusCheckTime) < STATUS_CACHE_DURATION) {
            return cachedActiveOfflineSession;
        }
        
        // We can't access localStorage directly in service worker
        // Instead, we'll use message passing to get the status from the main thread
        const clients = await self.clients.matchAll();
        
        if (clients.length === 0) {
            // If we have a previous cached value, use it as fallback
            if (cachedActiveOfflineSession !== null) {
                return cachedActiveOfflineSession;
            }
            // Otherwise default to false
            return false;
        }
        
        // Ask the first available client for active offline session status
        return new Promise((resolve) => {
            const messageChannel = new MessageChannel();
            
            messageChannel.port1.onmessage = (event) => {
                if (event.data && event.data.type === 'ACTIVE_OFFLINE_SESSION_RESPONSE') {
                    const hasActiveSession = event.data.hasActiveSession === true;
                    // Cache the result
                    cachedActiveOfflineSession = hasActiveSession;
                    lastStatusCheckTime = currentTime;
                    
                    resolve(hasActiveSession);
                } else {
                    // Use cached value if available, otherwise default to false
                    const fallbackStatus = cachedActiveOfflineSession !== null ? cachedActiveOfflineSession : false;
                    resolve(fallbackStatus);
                }
            };
            
            // Send request to client
            clients[0].postMessage({
                type: 'GET_ACTIVE_OFFLINE_SESSION'
            }, [messageChannel.port2]);
            
            // Timeout after 1 second, with intelligent fallback
            setTimeout(async () => {
                // Use cached value if available
                if (cachedActiveOfflineSession !== null) {
                    resolve(cachedActiveOfflineSession);
                    return;
                }
                
                // Otherwise, check if offline session data exists in cache
                const hasOfflineSession = await hasOfflineSessionInCache();
                // Cache the detected status
                cachedActiveOfflineSession = hasOfflineSession;
                lastStatusCheckTime = currentTime;
                
                resolve(hasOfflineSession);
            }, 1000);
        });
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error checking active offline session:', error);
        // Try to use cache check as fallback on error
        try {
            return await hasOfflineSessionInCache();
        } catch (cacheError) {
            self.offlineLog.error('ServiceWorker', 'Error fallback also failed:', cacheError);
            // Last resort: use cached value if available, otherwise default to false
            const fallbackStatus = cachedActiveOfflineSession !== null ? cachedActiveOfflineSession : false;
            self.offlineLog.log('ServiceWorker', 'Final fallback active offline session status:', fallbackStatus);
            return fallbackStatus;
        }
    }
}

// Handle page requests with cache-first strategy when offline
async function handlePageRequest(request) {
    const url = new URL(request.url);
    // Check if we're completely offline first
    const isOffline = await isUserInOfflineMode();//!navigator.onLine;
    
    //check if we have an active offline session localStorage item has_active_offline_session
    const hasActiveSession = await hasActiveOfflineSession();
    
    // Define protected routes that require active offline session and crypto key
    const PROTECTED_ROUTES = [
        /^\/Case/i,
        /^\/Home\/Index/i,
        /^\/pdf-version/i,
        /^\/$/ // Root route when offline
    ];
    
    // Check if this is a protected route
    const isProtectedRoute = PROTECTED_ROUTES.some(pattern => pattern.test(url.pathname));
    
    // Validate session for protected routes when in offline mode
    if (isOffline && isProtectedRoute) {
        // Check if user has active offline session
        if (!hasActiveSession) {
            self.offlineLog.log('ServiceWorker', 'Protected route access denied - no active session, redirecting to offline login');
            return Response.redirect('/Account/OfflineLogin', 302);
        }
        
        // Check if crypto key exists (required for accessing encrypted case data)
        if (!offlineCryptoKey) {
            self.offlineLog.log('ServiceWorker', 'Protected route access denied - no crypto key, redirecting to offline login');
            // Invalidate the session since key is lost
            cachedActiveOfflineSession = false;
            self.cachedOfflineStatus = false;
            return Response.redirect('/Account/OfflineLogin', 302);
        }
        
        self.offlineLog.log('ServiceWorker', 'Protected route access granted - valid session and crypto key');
    }
    
    // For offline mode, try cache first
    if (isOffline) {
        // Try current cache first
        try {
            const activeCacheName = await getActiveApiCacheName();
            const currentCache = await caches.open(activeCacheName);
            let cachedResponse = await caseInsensitiveCacheMatch(request, currentCache);
            if (cachedResponse) {
                return cachedResponse;
            }
        } catch (error) {
            self.offlineLog.warn('ServiceWorker', 'Error accessing current cache:', error);
        }
        
        // Try any available versioned cache
        try {
            const allCacheNames = await caches.keys();
            
            for (const cacheName of allCacheNames) {
                if (cacheName.startsWith('mmria-api-') || cacheName.startsWith('mmria-static-')) {
                    const cache = await caches.open(cacheName);
                    const cachedResponse = await caseInsensitiveCacheMatch(request, cache);
                    if (cachedResponse) {
                        return cachedResponse;
                    }
                }
            }
        } catch (error) {
            self.offlineLog.warn('ServiceWorker', 'Error searching caches:', error);
        }
        
        // No cache available - return error
        self.offlineLog.error('ServiceWorker', 'No cache available for offline page:', url.pathname);
        return new Response(
            JSON.stringify({
                error: 'Page not cached',
                message: 'This page is not available in the offline cache'
            }),
            {
                status: 503,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
    
    // When online, try network first
    try {
        // Use redirect: 'follow' for routes that may redirect (like /pdf-version)
        const response = await fetch(request, { redirect: 'follow' });
        
        // Cache successful responses to primary cache
        if (response.ok) {
            try {
                const activeCacheName = await getActiveApiCacheName();
                const primaryCache = await caches.open(activeCacheName);
                await primaryCache.put(request, response.clone());
                self.offlineLog.log('ServiceWorker', '✅ Cached page from network:', url.pathname);
            } catch (cacheError) {
                self.offlineLog.warn('ServiceWorker', 'Failed to cache response:', cacheError);
            }
        }
        
        return response;
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Network failed for page:', request.url, error);
        
        // Network failed, try to serve from cache as fallback
        try {
            // Try current cache first
            const activeCacheName = await getActiveApiCacheName();
            const currentCache = await caches.open(activeCacheName);
            let cachedResponse = await caseInsensitiveCacheMatch(request, currentCache);
            if (cachedResponse) {
                self.offlineLog.log('ServiceWorker', '✅ Serving cached page after network error:', url.pathname);
                return cachedResponse;
            }
            
            // Try any available versioned cache
            const allCacheNames = await caches.keys();
            for (const cacheName of allCacheNames) {
                if (cacheName.startsWith('mmria-api-') || cacheName.startsWith('mmria-static-')) {
                    const cache = await caches.open(cacheName);
                    const fallbackResponse = await caseInsensitiveCacheMatch(request, cache);
                    if (fallbackResponse) {
                        self.offlineLog.log('ServiceWorker', '✅ Serving from fallback cache after network error:', cacheName, url.pathname);
                        return fallbackResponse;
                    }
                }
            }
        } catch (cacheError) {
            self.offlineLog.warn('ServiceWorker', 'Cache fallback also failed:', cacheError);
        }
        
        // If cache fallback also fails, return error
        return new Response(
            JSON.stringify({
                error: 'Network error',
                message: 'Unable to load page. Please check your connection.'
            }),
            {
                status: 503,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
}

// Check if the application is in offline mode
async function isInOfflineMode() {
    try {
        // Send message to main thread to check offline status
        const clients = await self.clients.matchAll();
        if (clients.length > 0) {
            return new Promise((resolve) => {
                clients[0].postMessage({ type: 'CHECK_OFFLINE_STATUS' });
                
                // Set up message listener for response
                self.addEventListener('message', function handleOfflineCheck(event) {
                    if (event.data.type === 'OFFLINE_STATUS_RESPONSE') {
                        self.removeEventListener('message', handleOfflineCheck);
                        resolve(event.data.isOffline);
                    }
                });
                
                // Timeout after 1 second
                setTimeout(() => resolve(false), 1000);
            });
        }
    } catch (error) {
        self.offlineLog.log('ServiceWorker', 'Error checking offline status:', error);
    }
    return false;
}

// Get cached case data
async function getCachedCaseData(caseId) {
    try {
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedResponse = await cache.match(`/api/case?case_id=${caseId}`);
        return cachedResponse;
    } catch (error) {
        self.offlineLog.log('ServiceWorker', 'Error getting cached case data:', error);
        return null;
    }
}

// ===== Key derivation function (service worker only) =====
async function deriveAesKeyFromPassword(password, saltHex) {
    const encoder = new TextEncoder();
    const passwordBytes = encoder.encode(password);
    
    // Convert hex salt string to Uint8Array
    const saltBytes = new Uint8Array(
        saltHex.match(/.{1,2}/g).map(byte => parseInt(byte, 16))
    );
    
    // Import the user-provided secret as base key material
    const baseKey = await crypto.subtle.importKey(
        'raw',
        passwordBytes,
        'PBKDF2',
        false,
        ['deriveKey']
    );
    
    // Derive the AES-GCM key from the user-provided secret using PBKDF2
    return await crypto.subtle.deriveKey(
        {
            name: 'PBKDF2',
            salt: saltBytes,
            iterations: KEY_DERIVATION_ITERATIONS,
            hash: HASH_ALGORITHM
        },
        baseKey,
        { name: 'AES-GCM', length: KEY_LENGTH },
        false, // not extractable
        ['encrypt', 'decrypt']
    );
}

// ===== Base64 helpers =====
function bufferToBase64(buf) {
    const bytes = new Uint8Array(buf);
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
}

function base64ToBuffer(b64) {
    const binary = atob(b64);
    const len = binary.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
}

// ===== Encrypt/decrypt a Response body with AES-GCM =====
async function encryptResponseBody(res) {
    if (!offlineCryptoKey) throw new Error('No offline crypto key in memory');

    const contentType = res.headers.get('Content-Type') || 'application/json';
    const bodyBuffer = await res.arrayBuffer();

    const iv = crypto.getRandomValues(new Uint8Array(12));
    const ciphertext = await crypto.subtle.encrypt(
        { name: 'AES-GCM', iv },
        offlineCryptoKey,
        bodyBuffer
    );

    const encryptedBlob = {
        v: OFFLINE_ENCRYPTION_VERSION,
        iv: bufferToBase64(iv.buffer),
        data: bufferToBase64(ciphertext),
        contentType
    };

    return new Response(JSON.stringify(encryptedBlob), {
        status: res.status,
        statusText: res.statusText,
        headers: {
            'Content-Type': 'application/json',
            [OFFLINE_ENCRYPTION_HEADER]: '1'
        }
    });
}

async function decryptResponseBody(res) {
    if (!offlineCryptoKey) throw new Error('No offline crypto key in memory');

    const encryptedBlob = await res.json();
    if (encryptedBlob.v !== OFFLINE_ENCRYPTION_VERSION) {
        throw new Error('Unsupported encryption version');
    }

    const ivBuf = base64ToBuffer(encryptedBlob.iv);
    const dataBuf = base64ToBuffer(encryptedBlob.data);

    const plaintext = await crypto.subtle.decrypt(
        { name: 'AES-GCM', iv: new Uint8Array(ivBuf) },
        offlineCryptoKey,
        dataBuf
    );

    return new Response(plaintext, {
        status: res.status,
        statusText: res.statusText,
        headers: {
            'Content-Type': encryptedBlob.contentType || 'application/json'
        }
    });
}

// Cache case data
async function cacheCaseData(caseId, caseData) {
    try {
        //self.offlineLog.log('ServiceWorker', 'Case data:', caseData);
        
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cacheUrl = `/api/case?case_id=${caseId}`;

        // Build base response
        let response = new Response(JSON.stringify(caseData), {
            headers: { 'Content-Type': 'application/json' }
        });

        // 🔐 If we have an in-memory AES key, store encrypted-at-rest
        if (offlineCryptoKey) {
            try {
                response = await encryptResponseBody(response);
                self.offlineLog.log(`ServiceWorker`, `Stored case ${caseId} ENCRYPTED in cache`);
            } catch (err) {
                self.offlineLog.error('ServiceWorker', 'Failed to encrypt case before caching, falling back to plaintext:', err);
            }
        } else {
            self.offlineLog.log(`ServiceWorker`, `No offlineCryptoKey – storing case ${caseId} in plaintext cache`);
        }
        
        await cache.put(cacheUrl, response);
        
        // Verify the cache was successful
        const verification = await cache.match(cacheUrl);
        if (!verification) {
            self.offlineLog.error(`ServiceWorker`, `Verification failed - case ${caseId} not found in cache after put`);
            return false;
        }

        return true;
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error caching case data:', error);
        return false;
    }
}

// Cache metadata resources proactively
async function cacheMetadataResources(version) {
    try {
        self.offlineLog.log(`ServiceWorker`, `Starting to cache metadata resources for version: ${version}`);
        
        if (!version || version === 'undefined' || version === 'null') {
            self.offlineLog.error('ServiceWorker', 'Cannot cache metadata - no valid version provided');
            throw new Error('Invalid metadata version provided');
        }
        
        // Ensure API_CACHE_NAME is initialized before attempting to cache
        if (!API_CACHE_NAME) {
            self.offlineLog.error('ServiceWorker', 'Cannot cache metadata - API_CACHE_NAME not yet initialized');
            return;
        }
        
        const cache = await caches.open(API_CACHE_NAME);
        const baseUrl = `${self.location.protocol}//${self.location.host}`;
        
        // List of critical metadata endpoints to cache
        const endpoints = [
            `/api/version/${version}/metadata`,
            `/api/version/${version}/ui_specification`,
            `/api/version/${version}/validation`,
            `/api/version/release-version`,
            `/api/metadata`,
            `/api/metadata/version_specification`,
            `/api/jurisdiction_tree`
        ];
        
        let cachedCount = 0;
        let failedCount = 0;
        const blockingFailures = [];
        
        self.offlineLog.log(`ServiceWorker`, `Will attempt to cache ${endpoints.length} metadata endpoints`);
        
        for (const endpoint of endpoints) {
            try {
                const fullUrl = `${baseUrl}${endpoint}`;
                self.offlineLog.log(`ServiceWorker`, `Fetching and caching: ${fullUrl}`);
                
                const result = await cacheValidatedResponse(
                    cache,
                    fullUrl,
                    [fullUrl, endpoint],
                    REQUIRED_API_EXPECTATIONS
                );

                if (!result.cached) {
                    const reason = `Failed validation for ${endpoint}: ${result.reason}`;
                    if (result.blocked) {
                        self.offlineLog.error('ServiceWorker', reason);
                        blockingFailures.push(reason);
                    } else {
                        self.offlineLog.warn('ServiceWorker', reason);
                    }
                    failedCount++;
                    continue;
                }

                self.offlineLog.log(`ServiceWorker`, `✅ Successfully cached: ${endpoint}`);
                
                if (endpoint.includes('metadata') && !endpoint.includes('version_specification')) {
                    try {
                        const testData = await result.response.clone().json();
                        self.offlineLog.log(`ServiceWorker`, `Cached metadata has ${testData.children ? testData.children.length : 'N/A'} children`);
                        self.offlineLog.log(`ServiceWorker`, `Cached metadata _id: ${testData._id}`);
                        self.offlineLog.log(`ServiceWorker`, `Cached metadata name: ${testData.name}`);
                    } catch (e) {
                        self.offlineLog.warn(`ServiceWorker`, `Could not parse cached metadata:`, e);
                    }
                } else if (endpoint.includes('version_specification')) {
                    self.offlineLog.log(`ServiceWorker`, `Cached version specification (JavaScript content)`);
                }
                
                cachedCount++;
                
            } catch (error) {
                self.offlineLog.error(`ServiceWorker`, `❌ Error caching ${endpoint}:`, error);
                failedCount++;
            }
        }
        
        self.offlineLog.log(`ServiceWorker`, `Metadata caching complete. ✅ Cached: ${cachedCount}, ❌ Failed: ${failedCount}`);
        
        // Verify the metadata was actually cached
        const metadataEndpoint = `/api/version/${version}/metadata`;
        const verifyResponse = await cache.match(metadataEndpoint);
        if (verifyResponse) {
            try {
                const verifyData = await verifyResponse.clone().json();
                self.offlineLog.log(`ServiceWorker`, `✅ Verification successful - metadata has ${verifyData.children ? verifyData.children.length : 'N/A'} children`);
            } catch (e) {
                self.offlineLog.warn('ServiceWorker', 'Could not parse verification metadata:', e);
            }
        } else {
            self.offlineLog.warn(`ServiceWorker`, `⚠️ VERIFICATION FAILED - metadata not found in cache for ${metadataEndpoint}`);
            
            // Try to find any metadata entries
            const allRequests = await cache.keys();
            const metadataRequests = allRequests.filter(req => req.url.includes('metadata'));
            self.offlineLog.log('ServiceWorker', 'Found these metadata entries in cache:', metadataRequests.map(req => req.url));
        }
        
        // Cache additional common endpoints
        const additionalEndpoints = [
            '/_users/GetFormAccess',
            '/api/user/my-user',
            '/api/user_role_jurisdiction_view/my-roles',
            '/Case/GetDuplicateMultiFormList'
        ];
        
        self.offlineLog.log(`ServiceWorker`, `Caching ${additionalEndpoints.length} additional endpoints...`);
        
        let additionalCachedCount = 0;
        let additionalFailedCount = 0;
        
        for (const endpoint of additionalEndpoints) {
            try {
                const fullUrl = `${baseUrl}${endpoint}`;
                self.offlineLog.log(`ServiceWorker`, `Fetching and caching additional endpoint: ${fullUrl}`);
                const result = await cacheValidatedResponse(
                    cache,
                    fullUrl,
                    [fullUrl, endpoint],
                    REQUIRED_API_EXPECTATIONS
                );

                if (!result.cached) {
                    const reason = `Failed validation for additional endpoint ${endpoint}: ${result.reason}`;
                    if (result.blocked) {
                        self.offlineLog.error('ServiceWorker', reason);
                        blockingFailures.push(reason);
                    } else {
                        self.offlineLog.warn('ServiceWorker', reason);
                    }
                    additionalFailedCount++;
                    continue;
                }

                self.offlineLog.log(`ServiceWorker`, `✅ Successfully cached additional endpoint: ${endpoint}`);
                additionalCachedCount++;
            } catch (error) {
                self.offlineLog.error(`ServiceWorker`, `❌ Error caching additional endpoint ${endpoint}:`, error);
                additionalFailedCount++;
            }
        }
        
        self.offlineLog.log(`ServiceWorker`, `Additional endpoints caching complete. ✅ Cached: ${additionalCachedCount}, ❌ Failed: ${additionalFailedCount}`);
        self.offlineLog.log(`ServiceWorker`, `🎉 Total metadata caching process completed - Core: ${cachedCount}/${endpoints.length}, Additional: ${additionalCachedCount}/${additionalEndpoints.length}`);

        if (blockingFailures.length > 0) {
            throw new Error(blockingFailures.join(' | '));
        }

        return {
            success: true,
            cachedCount: cachedCount,
            failedCount: failedCount,
            additionalCachedCount: additionalCachedCount,
            additionalFailedCount: additionalFailedCount,
            blockingFailures: blockingFailures
        };

    } catch (error) {
        self.offlineLog.error('ServiceWorker', '❌ Error in cacheMetadataResources:', error);
        throw error;
    }
}

// Clear all caches
async function clearAllCaches() {
    try {
        const cacheNames = await caches.keys();
        await Promise.all(
            cacheNames
                .filter(name => name.startsWith('mmria-'))
                .map(name => caches.delete(name))
        );
        self.offlineLog.log('ServiceWorker', 'All caches cleared');
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error clearing caches:', error);
    }
}

// Get cache status
async function getCacheStatus() {
    try {
        const cacheNames = await caches.keys();
        const status = {};
        
        for (const cacheName of cacheNames) {
            if (cacheName.startsWith('mmria-')) {
                const cache = await caches.open(cacheName);
                const keys = await cache.keys();
                status[cacheName] = keys.length;
            }
        }
        
        return status;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error getting cache status:', error);
        return {};
    }
}

// Check if critical metadata resources are cached
async function checkCriticalResourcesCache(version) {
    try {
        if (!version) {
            self.offlineLog.warn('ServiceWorker', 'No version provided for critical resources check');
            return { allCached: false, missingResources: ['version not specified'] };
        }
        
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const criticalEndpoints = [
            `/api/version/${version}/metadata`,
            `/api/version/${version}/ui_specification`, 
            `/api/version/${version}/validation`,
            '/api/version/release-version',
            '/api/jurisdiction_tree'
        ];
        
        const results = {};
        const missingResources = [];
        
        for (const endpoint of criticalEndpoints) {
            const cachedResponse = await cache.match(endpoint);
            const isCached = !!cachedResponse;
            results[endpoint] = isCached;
            
            if (!isCached) {
                missingResources.push(endpoint);
            }
        }
        
        const allCached = missingResources.length === 0;
        
        self.offlineLog.log('ServiceWorker', 'Critical resources cache check:', {
            version,
            allCached,
            results,
            missingResources
        });
        
        return { 
            allCached, 
            results, 
            missingResources,
            version 
        };
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error checking critical resources cache:', error);
        return { 
            allCached: false, 
            missingResources: ['error checking cache'],
            error: error.message 
        };
    }
}

// Debug function to check cache status during activation
async function debugCacheStatus() {
    try {
        self.offlineLog.log('ServiceWorker', '🔍 Service Worker: Checking cache status after activation...');
        if (!STATIC_CACHE_NAME) {
            self.offlineLog.log('ServiceWorker', '📦 Static cache has not been initialized yet');
            return;
        }
        const cache = await caches.open(STATIC_CACHE_NAME);
        const requests = await cache.keys();
        self.offlineLog.log('ServiceWorker', `📦 Static cache has ${requests.length} entries`);
        
        // Check for some critical files
        const criticalFiles = ['/css/index.css', '/js/jquery.min.js', '/scripts/mmria.js'];
        for (const file of criticalFiles) {
            const response = await cache.match(file);
            self.offlineLog.log('ServiceWorker', `${response ? '✅' : '❌'} Critical file ${file}: ${response ? 'cached' : 'missing'}`);
        }
        
        return true;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error checking cache status:', error);
        return false;
    }
}

// Debug function to log all cached entries
async function debugCacheContents() {
    try {
        self.offlineLog.log('ServiceWorker', '🔍 Service Worker: DEBUG - Listing all cached entries');
        
        const cacheNames = await caches.keys();
        self.offlineLog.log('ServiceWorker', `Found ${cacheNames.length} cache(s):`, cacheNames);
        
        for (const cacheName of cacheNames) {
            if (cacheName.startsWith('mmria-')) {
                const cache = await caches.open(cacheName);
                const requests = await cache.keys();
                
                self.offlineLog.log('ServiceWorker', `\n📦 Cache: ${cacheName} (${requests.length} entries)`);
                
                requests.forEach((request, index) => {
                    const url = new URL(request.url);
                    self.offlineLog.log('ServiceWorker', `  ${index + 1}. ${url.pathname}${url.search}`);
                });
            }
        }
        
        return {
            cacheNames,
            totalEntries: cacheNames.reduce(async (total, name) => {
                const cache = await caches.open(name);
                const requests = await cache.keys();
                return (await total) + requests.length;
            }, Promise.resolve(0))
        };
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error debugging cache contents:', error);
        return { error: error.message };
    }
}

// Rebuild critical cache files if integrity check fails
async function rebuildCriticalCache() {
    try {
        self.offlineLog.log('ServiceWorker', 'Rebuilding critical cache...');
        
        if (!STATIC_CACHE_NAME) {
            self.offlineLog.error('ServiceWorker', 'Cannot rebuild cache - STATIC_CACHE_NAME not initialized');
            return { error: 'Cache not initialized' };
        }
        
        const cache = await caches.open(STATIC_CACHE_NAME);
        let successCount = 0;
        let failureCount = 0;
        
        // Try to cache key static files (first 15 files from STATIC_FILES)
        const keyFiles = STATIC_FILES.slice(0, 15);
        for (const file of keyFiles) {
            try {
                // Check if file is already cached
                const existing = await cache.match(file);
                if (existing) {
                    self.offlineLog.log(`ServiceWorker`, `Key file already cached: ${file}`);
                    successCount++;
                    continue;
                }
                
                // Try to fetch and cache the file
                const response = await fetch(file);
                if (response.ok) {
                    await cache.put(file, response);
                    self.offlineLog.log(`ServiceWorker`, `Successfully cached key file: ${file}`);
                    successCount++;
                } else {
                    self.offlineLog.error(`ServiceWorker`, `Failed to fetch key file ${file}: ${response.status}`);
                    failureCount++;
                }
            } catch (error) {
                self.offlineLog.error(`ServiceWorker`, `Error caching key file ${file}:`, error);
                failureCount++;
            }
        }
        
        const success = failureCount === 0;
        self.offlineLog.log(`ServiceWorker`, `Key cache rebuild complete - Success: ${successCount}, Failed: ${failureCount}`);
        
        // If rebuild was successful, try to claim clients
        if (success) {
            self.offlineLog.log('ServiceWorker', 'Critical cache rebuild successful, claiming clients');
            await self.clients.claim();
        }
        
        return success;
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error rebuilding critical cache:', error);
        return false;
    }
}

// Get list of cached offline cases
async function getCachedOfflineCaseList() {
    try {
        self.offlineLog.log('ServiceWorker', 'getCachedOfflineCaseList - Starting to retrieve cached cases');
        
        // Get the active API cache name (handles session-specific caches)
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const requests = await cache.keys();
        
        self.offlineLog.log(`ServiceWorker`, `Searching API cache (${activeCacheName}) for case data. Found ${requests.length} cached requests`);
        
        const caseList = [];
        
        // Process all cached requests in the API cache looking for case data
        for (const request of requests) {
            const url = new URL(request.url);
    
            // Check if this is a case data request
            if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
                self.offlineLog.log(`ServiceWorker`, `Processing cached case request: ${url.pathname}${url.search}`);
                        
                const caseId = url.searchParams.get('case_id');
                
                // Skip if we already have this case from another cache
                if (caseList.find(item => item.id === caseId)) {
                    self.offlineLog.log(`ServiceWorker`, `Skipping duplicate case ${caseId}`);
                    continue;
                }
                
                try {
                    const response = await cache.match(request);
                    if (!response) continue;

                    let caseData;
                    try {
                        // 🔐 Use helper that understands encrypted/plain
                        caseData = await loadCaseJsonFromCache(cache, request);
                    } catch (decryptErr) {
                        self.offlineLog.error('ServiceWorker', 'Failed to load/decrypt case for offline-documents list', decryptErr);
                        // If we can’t decrypt, skip this case in the list
                        continue;
                    }                   
                    
                    
                    // Debug: Log the actual structure of cached data
                    self.offlineLog.log('ServiceWorker', 'Cached case data structure for', caseId);
                    
                    // Create a case view item from the cached data (matching expected structure)
                    // Try multiple possible data locations
                    const caseViewItem = {
                        _id: caseData._id || caseId,
                        id: caseData._id || caseId,
                        _rev: caseData._rev || null,
                        rev: caseData._rev || null,
                        value: {
                            case_id: caseId,
                            record_id: caseData.record_id || caseData.home_record?.record_id || null,
                            first_name: caseData.home_record?.first_name || caseData.first_name || 'Unknown',
                            last_name: caseData.home_record?.last_name || caseData.last_name || 'Unknown', 
                            middle_name: caseData.home_record?.middle_name || caseData.middle_name || '',
                            date_of_death: caseData.home_record?.date_of_death || caseData.date_of_death || null,
                            agency_case_id: caseData.home_record?.agency_case_id || caseData.agency_case_id || null,
                            created_by: caseData.created_by || 'offline-user',
                            date_created: caseData.date_created || new Date().toISOString(),
                            last_updated_by: caseData.last_updated_by || 'offline-user',
                            date_last_updated: caseData.date_last_updated || new Date().toISOString(),
                            case_status: caseData.home_record?.case_status || 
                                        caseData.case_status || 
                                        caseData.home_record?.overall_case_status ||
                                        caseData.overall_case_status ||
                                        1, // Default to "Abstracting (Incomplete)" if not found
                            host_state: caseData.host_state || caseData.home_record?.host_state || 'Unknown',
                            jurisdiction_id: caseData.jurisdiction_id || caseData.home_record?.jurisdiction_id || 'Unknown',
                            review_date_projected: caseData.home_record?.review_date_projected || caseData.review_date_projected || null,
                            review_date_actual: caseData.home_record?.review_date_actual || caseData.review_date_actual || null,
                            is_offline: true
                        }
                    };
                    
                    self.offlineLog.log('ServiceWorker', 'Created case view item:', caseId);
                    
                    caseList.push(caseViewItem);
                    
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Error processing cached case:', caseId, error);
                }
            }
        }
        
        self.offlineLog.log(`ServiceWorker`, `Found ${caseList.length} cached offline cases`);
        
        return new Response(
            JSON.stringify({
                total_rows: caseList.length,
                offset: 0,
                rows: caseList
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error getting cached offline case list:', error);
        
        // Return empty list on error
        return new Response(
            JSON.stringify({
                total_rows: 0,
                offset: 0,
                rows: []
            }),
            {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
}

// ===== Encrypt/decrypt all cached cases in the active API cache =====
async function encryptAllOfflineCasesInCache() {
    if (!offlineCryptoKey) {
        self.offlineLog.warn('ServiceWorker', 'No offline crypto key set, cannot encrypt cases');
        return false;
    }

    const activeCacheName = await getActiveApiCacheName();
    const cache = await caches.open(activeCacheName);
    const requests = await cache.keys();

    let encryptedCount = 0;

    for (const request of requests) {
        const url = new URL(request.url);

        // Only touch case endpoints: /api/case?case_id=...
        if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
            const res = await cache.match(request);
            if (!res) continue;

            // Already encrypted?
            if (res.headers.get(OFFLINE_ENCRYPTION_HEADER) === '1') {
                continue;
            }

            try {
                const encryptedRes = await encryptResponseBody(res);
                await cache.put(request, encryptedRes);
                encryptedCount++;
            } catch (err) {
                self.offlineLog.error('ServiceWorker', 'Failed to encrypt case', url.search, err);
            }
        }
    }

    self.offlineLog.log(`ServiceWorker`, `Encrypted ${encryptedCount} cached case(s)`);
    // After encrypting, forget the key (user is now "logged out")
    offlineCryptoKey = null;
    return true;
}

async function decryptAllOfflineCasesInCache() {
    if (!offlineCryptoKey) {
        self.offlineLog.warn('ServiceWorker', 'No offline crypto key set, cannot decrypt cases');
        return false;
    }

    const activeCacheName = await getActiveApiCacheName();
    const cache = await caches.open(activeCacheName);
    const requests = await cache.keys();

    let decryptedCount = 0;

    for (const request of requests) {
        const url = new URL(request.url);

        if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
            const res = await cache.match(request);
            if (!res) continue;

            if (res.headers.get(OFFLINE_ENCRYPTION_HEADER) !== '1') {
                continue; // not encrypted
            }

            try {
                const decryptedRes = await decryptResponseBody(res);
                await cache.put(request, decryptedRes);
                decryptedCount++;
            } catch (err) {
                self.offlineLog.error('ServiceWorker', 'Failed to decrypt case', url.search, err);
            }
        }
    }

    self.offlineLog.log(`ServiceWorker`, `Decrypted ${decryptedCount} cached case(s)`);
    return true;
}

// Failed login attempt counter constants
const MAX_LOGIN_ATTEMPTS = 3;
const LOCKOUT_DURATION_MS = 2 * 60 * 60 * 1000; // 2 hours in milliseconds
const ATTEMPT_COUNTER_CACHE_KEY_PREFIX = '/offline-login-attempts/';

// Helper function to get attempt counter from cache
async function getLoginAttemptCounter(sessionId) {
    try {
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cacheKey = `${ATTEMPT_COUNTER_CACHE_KEY_PREFIX}${sessionId}`;
        const response = await cache.match(cacheKey);
        
        if (response) {
            const counterData = await response.json();
            return counterData;
        }
        
        // Return new counter if none exists
        return {
            attempts: 0,
            firstAttemptTime: null,
            lockoutUntil: null,
            sessionId: sessionId
        };
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error getting attempt counter:', error);
        return {
            attempts: 0,
            firstAttemptTime: null,
            lockoutUntil: null,
            sessionId: sessionId
        };
    }
}

// Helper function to save attempt counter to cache
async function saveLoginAttemptCounter(counterData) {
    try {
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cacheKey = `${ATTEMPT_COUNTER_CACHE_KEY_PREFIX}${counterData.sessionId}`;
        
        const response = new Response(JSON.stringify(counterData), {
            status: 200,
            headers: { 'Content-Type': 'application/json' }
        });
        
        await cache.put(cacheKey, response);
        self.offlineLog.log('ServiceWorker', 'Attempt counter saved:', counterData);
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error saving attempt counter:', error);
    }
}

// Helper function to reset attempt counter
async function resetLoginAttemptCounter(sessionId) {
    try {
        const counterData = {
            attempts: 0,
            firstAttemptTime: null,
            lockoutUntil: null,
            sessionId: sessionId
        };
        await saveLoginAttemptCounter(counterData);
        self.offlineLog.log('ServiceWorker', 'Attempt counter reset for session:', sessionId);
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error resetting attempt counter:', error);
    }
}

// Helper function to check if account is locked out
function isLockedOut(counterData) {
    if (!counterData.lockoutUntil) {
        return false;
    }
    
    const now = Date.now();
    return now < counterData.lockoutUntil;
}

// Helper function to increment failed attempt counter
async function incrementFailedAttempts(sessionId) {
    try {
        const counterData = await getLoginAttemptCounter(sessionId);
        const now = Date.now();
        
        // If this is the first attempt, record the time
        if (counterData.attempts === 0) {
            counterData.firstAttemptTime = now;
        }
        
        counterData.attempts += 1;
        
        // If we've reached max attempts, set lockout
        if (counterData.attempts >= MAX_LOGIN_ATTEMPTS) {
            counterData.lockoutUntil = now + LOCKOUT_DURATION_MS;
            self.offlineLog.log(`ServiceWorker`, `Max attempts reached. Locked out until ${new Date(counterData.lockoutUntil).toISOString()}`);
        }
        
        await saveLoginAttemptCounter(counterData);
        return counterData;
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error incrementing failed attempts:', error);
        return null;
    }
}

// Function to validate derived key hash against cached session data
async function validateOfflineKeyInServiceWorker(derivedKeyHash, sessionId, messageEvent) {
    try {
        self.offlineLog.log('ServiceWorker', 'Validating derived key hash...');
        
        // First, check if account is locked out
        const counterData = await getLoginAttemptCounter(sessionId);
        
        if (isLockedOut(counterData)) {
            const now = Date.now();
            const remainingMs = counterData.lockoutUntil - now;
            const remainingMinutes = Math.ceil(remainingMs / (60 * 1000));
            
            self.offlineLog.log(`ServiceWorker`, `Account locked out. ${remainingMinutes} minutes remaining.`);
            messageEvent.ports[0].postMessage({
                type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
                isValid: false,
                isLockedOut: true,
                lockoutUntil: counterData.lockoutUntil,
                remainingMinutes: remainingMinutes,
                attemptsRemaining: 0
            });
            return;
        }
        
        // Get the active API cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedRequests = await cache.keys();
        
        // Search for cached offline session data
        for (const request of cachedRequests) {
            const url = new URL(request.url);
            
            // Check if this is offline session data
            if (url.pathname.includes('offline-session') || 
                url.searchParams.has('type') && url.searchParams.get('type') === 'CACHE_OFFLINE_SESSION_DATA') {
                
                try {
                    const response = await cache.match(request);
                    if (response) {
                        const sessionData = await response.json();
                        self.offlineLog.log('ServiceWorker', 'Found cached offline session data', {
                            hasKeySalt: !!sessionData.keySalt,
                            hasDerivedKeyHash: !!sessionData.derivedKeyHash,
                            hasOfflineKey: !!sessionData.offlineKey,
                            sessionId: sessionData.offlineSessionId
                        });
                        
                        // Validate session ID matches (if provided)
                        if (sessionId && sessionData.offlineSessionId && sessionData.offlineSessionId !== sessionId) {
                            self.offlineLog.log('ServiceWorker', 'Session ID mismatch, skipping this session data');
                            continue;
                        }
                        
                        // Check if derived key hash matches stored hash
                        if (sessionData.derivedKeyHash && sessionData.derivedKeyHash === derivedKeyHash) {
                            self.offlineLog.log('ServiceWorker', 'Derived key validation successful');
                            
                            // Reset attempt counter on successful login
                            await resetLoginAttemptCounter(sessionId);
                            
                            messageEvent.ports[0].postMessage({
                                type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
                                isValid: true,
                                isLockedOut: false
                            });
                            return;
                        }
                        
                        self.offlineLog.log('ServiceWorker', 'Hash mismatch - entered hash does not match stored hash');
                        
                        // Legacy support for old plaintext keys (will be phased out)
                        if (!sessionData.derivedKeyHash && sessionData.offlineKey) {
                            self.offlineLog.warn('ServiceWorker', 'Found legacy plaintext key - this should be upgraded');
                            // For legacy support, we can't validate since we only have the derived hash
                            // This case should not happen in normal operation
                        }
                    }
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Error reading cached session data:', error);
                }
            }
        }
        
        // If we get here, no matching key hash was found - increment failed attempts
        self.offlineLog.log('ServiceWorker', 'Key validation failed - no matching derived key hash found');
        const updatedCounter = await incrementFailedAttempts(sessionId);
        
        const attemptsRemaining = MAX_LOGIN_ATTEMPTS - (updatedCounter?.attempts || 0);
        const isNowLockedOut = updatedCounter && isLockedOut(updatedCounter);
        
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
            isValid: false,
            isLockedOut: isNowLockedOut,
            lockoutUntil: updatedCounter?.lockoutUntil || null,
            attemptsRemaining: Math.max(0, attemptsRemaining),
            remainingMinutes: isNowLockedOut ? Math.ceil((updatedCounter.lockoutUntil - Date.now()) / (60 * 1000)) : 0
        });
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error validating derived key:', error);
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
            isValid: false,
            isLockedOut: false
        });
    }
}

// Function to handle caching offline session data
async function handleCacheOfflineSessionData(data, messageEvent) {
    try {
        self.offlineLog.log('ServiceWorker', 'Caching offline session data...');
        self.offlineLog.log('ServiceWorker', 'Incoming session ID:', data.offlineSessionId);
        
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        
        // Check if we already have this session cached (to prevent unnecessary deletion)
        const cachedRequests = await cache.keys();
        let existingSessionId = null;
        
        // Find existing session ID to compare
        for (const request of cachedRequests) {
            const url = new URL(request.url);
            if (url.pathname.includes('offline-session-data') || 
                (url.searchParams.has('type') && url.searchParams.get('type') === 'CACHE_OFFLINE_SESSION_DATA')) {
                try {
                    const existingResponse = await cache.match(request);
                    if (existingResponse) {
                        const existingData = await existingResponse.json();
                        existingSessionId = existingData.offlineSessionId;
                        self.offlineLog.log('ServiceWorker', 'Found existing session ID in cache:', existingSessionId);
                        break;
                    }
                } catch (err) {
                    self.offlineLog.warn('ServiceWorker', 'Error reading existing session data:', err);
                }
            }
        }
        
        // Only perform cleanup if this is a DIFFERENT session (session IDs don't match)
        // This preserves the current active session and only removes old/different sessions
        if (existingSessionId && existingSessionId === data.offlineSessionId) {
            self.offlineLog.log('ServiceWorker', 'Session ID matches existing session - updating in place, no cleanup needed');
        } else {
            self.offlineLog.log('ServiceWorker', 'New or different session detected - cleaning up old session data...');
            let deletedSessions = 0;
            let deletedCounters = 0;
            
            for (const request of cachedRequests) {
                const url = new URL(request.url);
                
                // Delete old session data (only if it's a different session)
                if (url.pathname.includes('offline-session-data') || 
                    (url.searchParams.has('type') && url.searchParams.get('type') === 'CACHE_OFFLINE_SESSION_DATA')) {
                    await cache.delete(request);
                    deletedSessions++;
                    self.offlineLog.log('ServiceWorker', 'Deleted old session:', url.pathname);
                }
                
                // Delete old attempt counters (only if it's a different session)
                if (url.pathname.includes('/offline-login-attempts/')) {
                    await cache.delete(request);
                    deletedCounters++;
                    self.offlineLog.log('ServiceWorker', 'Deleted old attempt counter:', url.pathname);
                }
            }
            
            self.offlineLog.log(`ServiceWorker`, `Cleanup complete - deleted ${deletedSessions} old session(s) and ${deletedCounters} attempt counter(s)`);
        }
        
        // Create a unique cache key for the offline session data
        const cacheKey = new Request(`/offline-session-data/${Date.now()}?type=CACHE_OFFLINE_SESSION_DATA`, {
            method: 'GET'
        });
        
        // Cache the session data (new or updated)
        const response = new Response(JSON.stringify(data), {
            status: 200,
            headers: { 'Content-Type': 'application/json' }
        });
        
        await cache.put(cacheKey, response);
        self.offlineLog.log('ServiceWorker', 'Offline session data cached successfully for session:', data.offlineSessionId);
        
        // Notify the client of successful caching
        if (messageEvent.ports && messageEvent.ports[0]) {
            messageEvent.ports[0].postMessage({
                type: 'CACHE_OFFLINE_SESSION_DATA_RESPONSE',
                success: true
            });
        }
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error caching offline session data:', error);
        
        if (messageEvent.ports && messageEvent.ports[0]) {
            messageEvent.ports[0].postMessage({
                type: 'CACHE_OFFLINE_SESSION_DATA_RESPONSE',
                success: false,
                error: error.message
            });
        }
    }
}

// Function to retrieve offline session data from cache
async function getOfflineSessionDataFromServiceWorker(messageEvent) {
    try {
        self.offlineLog.log('ServiceWorker', 'Retrieving offline session data...');
        
        // Get the active API cache
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const cachedRequests = await cache.keys();
        
        // Search for cached offline session data
        for (const request of cachedRequests) {
            const url = new URL(request.url);
            
            // Check if this is offline session data
            if (url.pathname.includes('offline-session') || 
                url.searchParams.has('type') && url.searchParams.get('type') === 'CACHE_OFFLINE_SESSION_DATA') {
                
                try {
                    const response = await cache.match(request);
                    if (response) {
                        const sessionData = await response.json();
                        self.offlineLog.log('ServiceWorker', 'Found cached offline session data');
                        
                        messageEvent.ports[0].postMessage({
                            type: 'OFFLINE_SESSION_DATA_RESPONSE',
                            success: true,
                            sessionData: sessionData
                        });
                        return;
                    }
                } catch (error) {
                    self.offlineLog.error('ServiceWorker', 'Error reading cached session data:', error);
                }
            }
        }
        
        // If we get here, no session data was found
        self.offlineLog.log('ServiceWorker', 'No cached offline session data found');
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_SESSION_DATA_RESPONSE',
            success: false,
            error: 'No offline session data found in cache'
        });
        
    } catch (error) {
        self.offlineLog.error('ServiceWorker', 'Error retrieving offline session data:', error);
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_SESSION_DATA_RESPONSE',
            success: false,
            error: error.message
        });
    }
}
