// MMRIA Offline Service Worker
// This service worker handles caching for offline mode functionality

// Cache version - will be fetched from server endpoint (single source of truth)
let CACHE_VERSION_BASE = null; // Must be set from server API - no fallback
let CACHE_VERSION_FETCHED = false;
let CACHE_VERSION_FETCH_PROMISE = null;

// Fetch cache version from server endpoint (single source of truth)
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

        // Create fetch promise - fail fast if API unavailable
        CACHE_VERSION_FETCH_PROMISE = fetch('/api/OfflineCase/cache-version')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Failed to fetch cache version: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                // Use baseVersion directly from API response (e.g., 'v38-stable')
                if (data && data.baseVersion) {
                    CACHE_VERSION_BASE = data.baseVersion;
                    CACHE_VERSION_FETCHED = true;
                    
                    // Update cache names now that we have the version
                    STATIC_CACHE_NAME = `mmria-static-${CACHE_VERSION_BASE}`;
                    API_CACHE_NAME = `mmria-api-${CACHE_VERSION_BASE}`;
                    
                    console.log('Service Worker: Cache version fetched from server:', CACHE_VERSION_BASE);
                    console.log('Service Worker: Cache names initialized:', { static: STATIC_CACHE_NAME, api: API_CACHE_NAME });
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
        console.error('Service Worker: Failed to fetch cache version from server:', error);
        throw error; // No fallback - fail fast if API unavailable
    }
}

// Fetch cache version immediately when service worker loads
// This ensures CACHE_VERSION_BASE and cache names are set before install event
fetchCacheVersionFromServer().catch(error => {
    console.error('Service Worker: Failed to fetch cache version on load:', error);
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
let CURRENT_SESSION_ID = null;
let STATIC_CACHE_NAME = null; // Will be set in fetchCacheVersionFromServer()
let API_CACHE_NAME = null; // Will be set in fetchCacheVersionFromServer()

// Cache offline status to avoid repeated expensive checks during page lifecycle
let cachedOfflineStatus = null;
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
function initializeOfflineSessionCache(sessionId) {
    console.log('Service Worker: Initializing offline session cache for session:', sessionId);
    CURRENT_SESSION_ID = sessionId;
    // Only API cache needs session-specific naming for offline data isolation
    // Static files remain shared across sessions using base version
    API_CACHE_NAME = `mmria-api-${CACHE_VERSION_BASE}-session-${sessionId}`;
    
    console.log('Service Worker: Updated cache names for session:', {
        static: STATIC_CACHE_NAME,
        api: API_CACHE_NAME
    });
    
    // Pre-cache API routes to the session-specific API cache
    cacheApiRoutesForSession();
}

// Function to cache API routes to session-specific cache
async function cacheApiRoutesForSession() {
    try {
        console.log('Service Worker: Caching API routes for session to:', API_CACHE_NAME);
        const apiCache = await caches.open(API_CACHE_NAME);
        
        // Cache the Case route
        const caseResponse = await fetch('/Case');
        if (caseResponse.ok) {
            await Promise.all([
                apiCache.put('/Case', caseResponse.clone()),
                apiCache.put('/case', caseResponse.clone())
            ]);
            console.log('Service Worker: ✅ Cached main Case route to session cache');
        } else {
            console.warn('Service Worker: Case route returned non-OK status:', caseResponse.status);
        }
        
        // Cache the Home/Index route and root route
        const homeResponse = await fetch('/Home/Index');
        if (homeResponse.ok) {
            await Promise.all([
                apiCache.put('/Home/Index', homeResponse.clone()),
                apiCache.put('/', homeResponse.clone())
            ]);
            console.log('Service Worker: ✅ Cached Home/Index and root routes to session cache');
        } else {
            console.warn('Service Worker: Home/Index route returned non-OK status:', homeResponse.status);
        }
        
        // Cache the Offline Login route
        const offlineLoginResponse = await fetch('/Account/Offlinelogin');
        if (offlineLoginResponse.ok) {
            await apiCache.put('/Account/Offlinelogin', offlineLoginResponse.clone());
            console.log('Service Worker: ✅ Cached /Account/Offlinelogin route to session cache');
        } else {
            console.warn('Service Worker: /Account/Offlinelogin route returned non-OK status:', offlineLoginResponse.status);
        }

        // Cache the PDF version route (with and without trailing slash)
        const pdfVersionResponse = await fetch('/pdf-version/', { redirect: 'follow' });
        if (pdfVersionResponse.ok) {
            await Promise.all([
                apiCache.put('/pdf-version', pdfVersionResponse.clone()),
                apiCache.put('/pdf-version/', pdfVersionResponse.clone())
            ]);
            console.log('Service Worker: ✅ Cached /pdf-version routes to session cache');
        } else {
            console.warn('Service Worker: /pdf-version/ route returned non-OK status:', pdfVersionResponse.status);
        }

        // Cache the cache-version endpoint (required for offline mode)
        const cacheVersionResponse = await fetch('/api/OfflineCase/cache-version');
        if (cacheVersionResponse.ok) {
            await apiCache.put('/api/OfflineCase/cache-version', cacheVersionResponse.clone());
            console.log('Service Worker: ✅ Cached /api/OfflineCase/cache-version endpoint to session cache');
        } else {
            console.warn('Service Worker: cache-version endpoint returned non-OK status:', cacheVersionResponse.status);
        }
        
        console.log('Service Worker: API routes caching complete for session:', CURRENT_SESSION_ID);
    } catch (error) {
        console.error('Service Worker: Failed to cache API routes for session:', error.message);
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

// Function to clear previous session caches
async function clearPreviousSessionCaches() {
    try {
        console.log('Service Worker: Clearing previous session caches...');
        const allCacheNames = await caches.keys();
        const sessionCaches = allCacheNames.filter(name => 
            name.includes('-session-') && !name.includes(CURRENT_SESSION_ID)
        );
        
        console.log('Service Worker: Found previous session caches to clear:', sessionCaches);
        
        for (const cacheName of sessionCaches) {
            await caches.delete(cacheName);
            console.log('Service Worker: Cleared previous session cache:', cacheName);
        }
        
        console.log('Service Worker: Previous session cache cleanup complete');
    } catch (error) {
        console.error('Service Worker: Error clearing previous session caches:', error);
    }
}

// Helper function to get the active API cache name
// Dynamically resolves the correct cache to use - handles browser reopen scenarios
// where CURRENT_SESSION_ID is null but session-specific caches still exist
async function getActiveApiCacheName() {
    try {
        // If CURRENT_SESSION_ID is set, use the session-specific cache name
        if (CURRENT_SESSION_ID) {
            const sessionCacheName = `mmria-api-${CACHE_VERSION_BASE}-session-${CURRENT_SESSION_ID}`;
            console.log('Service Worker: Using active session cache:', sessionCacheName);
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
            console.log('Service Worker: Found existing session cache:', cacheName);
            return cacheName;
        }
        
        // Fallback to base API cache name
        console.log('Service Worker: No session cache found, using base API cache:', API_CACHE_NAME);
        return API_CACHE_NAME;
        
    } catch (error) {
        console.error('Service Worker: Error resolving active API cache name:', error);
        return API_CACHE_NAME; // Fallback to base cache
    }
}



// Static files to cache
const STATIC_FILES = [
    // Core CSS files
    '/css/index.css',
    '/css/bootstrap.min.css',
    '/css/animate.css',
    '/TemplatePackage/4.0/assets/css/app.min.css',
    '/TemplatePackage/4.0/assets/css/print.css',
    '/TemplatePackage/4.0/assets/vendor/css/bootstrap.css',
    '/styles/mmria-custom.css',
    '/styles/template-package-override.css',
    '/styles/jquery/jquery.timepicker.css',
    '/styles/jquery/jquery.datetimepicker.css',
    '/styles/bootstrap/bootstrap-datetimepicker.min.css',
    '/styles/bootstrap/jquery.bootstrap-touchspin.min.css',
    '/styles/bootstrap/bootstrap-timepicker.css',
    '/styles/flatpickr/flatpickr.min.css',
    '/styles/d3/c3/0.7.20/c3.min.css',
    '/styles/trumbowyg/trumbowyg.min.css',
    
    // Fonts (only include files that actually exist)
    '/TemplatePackage/4.0/assets/fonts/open-sans-v15-latin-regular.woff2',
    '/TemplatePackage/4.0/assets/fonts/merriweather-v19-latin-regular.woff2',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff2',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.ttf',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.eot',
    '/TemplatePackage/4.0/assets/fonts/fontawesome-webfont.woff',
    '/TemplatePackage/4.0/assets/fonts/fontawesome-webfont.ttf',
    '/TemplatePackage/4.0/assets/fonts/fontawesome-webfont.eot',
    '/TemplatePackage/4.0/assets/fonts/glyphicons-halflings-regular.woff',
    '/TemplatePackage/4.0/assets/fonts/glyphicons-halflings-regular.ttf',
    '/TemplatePackage/4.0/assets/fonts/glyphicons-halflings-regular.eot',
    '/TemplatePackage/4.0/assets/fonts/lato-regular-webfont.woff',
    '/TemplatePackage/4.0/assets/fonts/lato-regular-webfont.ttf',
    '/TemplatePackage/4.0/assets/fonts/lato-regular-webfont.eot',
    // Common cache-busting variants for CDC icon font
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff2?2747808d2c4ae8c1059745ae5eddb65e',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff?2747808d2c4ae8c1059745ae5eddb65e',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.ttf?2747808d2c4ae8c1059745ae5eddb65e',
    
    // Core JavaScript libraries
    '/js/jquery.min.js',
    '/js/bootstrap.min.js',
    '/js/jquery.easing.min.js',
    '/js/wow.js',
    '/js/jquery.bxslider.min.js',
    '/TemplatePackage/4.0/assets/vendor/js/jquery.min.js',
    '/TemplatePackage/4.0/assets/vendor/js/bootstrap.min.js',
    
    // jQuery UI and extensions
    '/scripts/jquery-ui.min.js',
    '/scripts/jquery/moment.js',
    '/scripts/jquery/jquery.timepicker.js',
    '/scripts/jquery/jquery.numeric.min.js',
    '/scripts/jquery/jquery.datetimepicker.js',
    
    // Bootstrap extensions
    '/scripts/bootstrap/bootstrap-datetimepicker.min.js',
    '/scripts/bootstrap/jquery.bootstrap-touchspin.min.js',
    '/scripts/bootstrap/bootstrap-timepicker.js',
    
    // Utility libraries
    '/scripts/esprima.js',
    '/scripts/escodegen.browser.js',
    '/scripts/peg.js/0.10.0/peg.js',
    '/scripts/rxjs/7.5.5/rxjs.umd.min.js',
    
    // D3 and charting
    '/scripts/d3/d3/v5/d3.v5.min.js',
    '/scripts/d3/c3/0.7.20/c3.min.js',
    
    // Rich text editor
    '/scripts/trumbowyg/trumbowyg.min.js',
    '/scripts/trumbowyg/trumbowyg.colors.min.js',
    '/scripts/trumbowyg/trumbowyg.fontsize.min.js',
    
    // MMRIA core scripts
    '/scripts/mmria.js',
    '/scripts/mmria-custom.js',
    '/scripts/metadata_summary.js',
    '/scripts/service-worker-manager.js',
    '/scripts/offline-session-manager.js',
    
    // Editor and page renderer
    '/scripts/editor/page_renderer/app.mmria.js',
    '/scripts/editor/page_renderer/string.js',
    '/scripts/editor/page_renderer.js',
    '/scripts/editor/page_renderer/number.js',
    '/scripts/editor/page_renderer/textarea.js',
    '/scripts/editor/page_renderer/html_area.js',
    '/scripts/editor/page_renderer/time.js',
    '/scripts/editor/page_renderer/boolean.js',
    '/scripts/editor/page_renderer/chart.js',
    '/scripts/editor/page_renderer/date.mmria.js',
    '/scripts/editor/page_renderer/datetime.js',
    '/scripts/editor/page_renderer/form.mmria.js',
    '/scripts/editor/page_renderer/form.pmss.attachment.js',
    '/scripts/editor/page_renderer/grid.js',
    '/scripts/editor/page_renderer/group.js',
    '/scripts/editor/page_renderer/hidden.js',
    '/scripts/editor/page_renderer/jurisdiction.js',
    '/scripts/editor/page_renderer/label.js',
    '/scripts/editor/page_renderer/list.js',
    '/scripts/editor/navigation_renderer.js',
    '/scripts/editor/apply_sort.js',
    
    // Case-specific scripts
    '/scripts/case/index.js',
    '/scripts/case/index.mmria.js',
    '/scripts/case/search_view.js',
    '/scripts/case/conversion-calculator.js',
    
    // PDF version scripts
    '/scripts/pdf-version/pdfmake.min.js',
    '/scripts/pdf-version/vfs_fonts.js',
    '/scripts/pdf-version/chart.min.js',
    '/scripts/pdf-version/index.js',
    
    // Utility scripts
    '/scripts/create_default_object.js',
    '/scripts/url_monitor.js',    
    
    // Icons and images
    '/favicon.ico',
    '/img/icon_pin.png',
    '/img/icon_unpin.png',
    '/img/online-go.svg',
    '/img/offline-info.svg',
    '/img/offline-index.svg',
    '/img/icon_error.svg',
    '/images/mmria-secondary.svg',    
    // Offline login view and required scripts
    //'/Account/OfflineLogin',
    '/scripts/Account/offline_key_login.js',
    '/scripts/shared/logout-handler.js'
];

// Routes that should be cached for offline access
const CACHED_ROUTES = [
    // Home page routes (root and explicit)
    /^\/$/,
    /^\/Home\/Index\/?$/,
    // Case index route
    /^\/Case\/?$/,
    // Case summary routes (for specific case IDs)
    /^\/Account\/OfflineLogin\/?$/i,
    /^\/Account\/Login\/?$/i,
    /^\/Case\/([^\/]+)\/summary$/,
    // Case form routes 
    /^\/Case\/([^\/]+)\/0\/home_record$/,
    /^\/Case\/([^\/]+)\/0\/death_certificate$/,
    /^\/Case\/([^\/]+)\/0\/birth_fetal_death_certificate_parent$/,
    /^\/Case\/([^\/]+)\/0\/birth_certificate_infant_fetal_section$/,
    /^\/Case\/([^\/]+)\/0\/cvs$/,
    /^\/Case\/([^\/]+)\/0\/social_and_environmental_profile$/,
    /^\/Case\/([^\/]+)\/0\/autopsy_report$/,
    /^\/Case\/([^\/]+)\/0\/prenatal$/,
    /^\/Case\/([^\/]+)\/0\/er_visit_and_hospital_medical_records$/,
    /^\/Case\/([^\/]+)\/0\/other_medical_office_visits$/,
    /^\/Case\/([^\/]+)\/0\/medical_transport$/,
    /^\/Case\/([^\/]+)\/0\/mental_health_profile$/,
    /^\/Case\/([^\/]+)\/0\/informant_interviews$/,
    /^\/Case\/([^\/]+)\/0\/case_narrative$/,
    /^\/Case\/([^\/]+)\/0\/committee_review$/,
    // PDF version route
    /^\/pdf-version\/?$/
];

// API routes that should be cached
const CACHED_API_ROUTES = [
    /^\/api\/case\?case_id=/,
    /^\/api\/case_view\/record-id-list/,
    /^\/api\/case_view\/offline-documents/,
    /^\/api\/case_view$/,
    /^\/api\/OfflineCase\/cache-version/,
    /^\/api\/version\/.*\/validation$/,
    /^\/api\/version\/.*\/ui_specification$/,
    /^\/api\/version\/.*\/metadata$/,
    /^\/api\/version\/release-version$/,
    /^\/api\/metadata$/,
    /^\/api\/metadata\/version_specification$/,
    /^\/api\/user_role_jurisdiction_view\/my-roles/,
    /^\/api\/user\/my-user$/,
    /^\/api\/jurisdiction_tree$/,
    /^\/api\/cvsAPI$/,
    /^\/_users\/GetFormAccess/,
    /^\/Case\/GetDuplicateMultiFormList/
];

// Routes to exclude from caching
const EXCLUDED_ROUTES = [
    /\/api\/.*view.*pdf/i,
    /\/api\/.*save.*pdf/i,
    /\/print-version/i,
    /validate.*address/i,
    /geography.*context/i
];

// Verify cache integrity before activation
async function verifyCacheIntegrity() {
    try {
        console.log('Service Worker: Verifying cache integrity...');
        
        const staticCache = await caches.open(STATIC_CACHE_NAME);
        
        // Check if cache has critical static files (sample a subset)
        let missingFiles = [];
        // Check key static files (first 10 files from STATIC_FILES as representative sample)
        const keyFiles = STATIC_FILES.slice(0, 10);
        for (const file of keyFiles) {
            const response = await staticCache.match(file);
            if (!response) {
                missingFiles.push(file);
            }
        }
        
        if (missingFiles.length > 0) {
            console.warn('Service Worker: Missing critical files from cache:', missingFiles);
            return false;
        }
        
        console.log('Service Worker: Cache integrity verification passed');
        return true;
        
    } catch (error) {
        console.error('Service Worker: Cache integrity verification failed:', error);
        return false;
    }
}

self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');
    event.waitUntil(
        // Fetch cache version from server first (single source of truth)
        fetchCacheVersionFromServer()
            .then(() => caches.open(STATIC_CACHE_NAME))
            .then(cache => {
                console.log('Service Worker: Caching static files...');
                let successCount = 0;
                let failureCount = 0;
                
                // Cache files individually to identify any failures
                const cachePromises = STATIC_FILES.map(async (url) => {
                    try {
                        await cache.add(url);
                        successCount++;
                        console.log(`Service Worker: ✅ Cached: ${url}`);
                        return Promise.resolve();
                    } catch (error) {
                        failureCount++;
                        console.error(`Service Worker: ❌ Failed to cache ${url}:`, error.message);
                        // Try to add a fallback entry for critical files
                        if (url.endsWith('.js')) {
                            const fallbackResponse = new Response('// File not available offline', {
                                status: 200,
                                headers: { 'Content-Type': 'application/javascript' }
                            });
                            await cache.put(url, fallbackResponse);
                            console.log(`Service Worker: Added fallback for JS: ${url}`);
                        } else if (url.endsWith('.css')) {
                            const fallbackResponse = new Response('/* File not available offline */', {
                                status: 200,
                                headers: { 'Content-Type': 'text/css' }
                            });
                            await cache.put(url, fallbackResponse);
                            console.log(`Service Worker: Added fallback for CSS: ${url}`);
                        }
                        return Promise.resolve();
                    }
                });
                
                return Promise.all(cachePromises).then(() => {
                    console.log(`Service Worker: Caching complete - ✅ Success: ${successCount}, ❌ Failed: ${failureCount}`);
                });
            })
            .then(async () => {
                console.log('Service Worker: Static files cached successfully');
                console.log('Service Worker: API routes will be cached when offline session is initialized');
                
                // Verify cache integrity before skipping waiting
                const isValid = await verifyCacheIntegrity();
                if (isValid) {
                    console.log('Service Worker: Cache integrity verified, skipping waiting');
                    return self.skipWaiting();
                } else {
                    console.warn('Service Worker: Cache integrity check failed, not skipping waiting');
                    // Let the natural activation process handle this
                    return Promise.resolve();
                }
            })
            .catch(error => {
                console.error('Service Worker: Error caching static files:', error);
            })
    );
});

// Listen for network status changes to avoid cache clearing during transitions
let lastNetworkStatus = navigator.onLine;
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'NETWORK_STATUS_CHANGE') {
        const newStatus = event.data.isOnline;
        console.log('Service Worker: Network status change detected:', lastNetworkStatus, '->', newStatus);
        
        if (lastNetworkStatus !== newStatus) {
            lastNetworkStatus = newStatus;
            
            // If going offline, ensure caches are preserved
            if (!newStatus) {
                console.log('Service Worker: Going offline - preserving all caches');
                // Don't delete any caches when going offline
            }
        }
    }
    
    // Handle offline session cache initialization
    if (event.data && event.data.type === 'INITIALIZE_OFFLINE_SESSION') {
        const sessionId = event.data.sessionId;
        if (sessionId) {
            console.log('Service Worker: Received offline session initialization request for:', sessionId);
            initializeOfflineSessionCache(sessionId);
            clearPreviousSessionCaches();
        }
    }
    
    // Handle offline status updates to invalidate cache
    if (event.data && event.data.type === 'OFFLINE_STATUS_UPDATE') {
        console.log('Service Worker: Received offline status update, invalidating cache');
        cachedOfflineStatus = null;
        cachedActiveOfflineSession = null;
        lastStatusCheckTime = 0;
    }
    
    // Handle active offline session updates to invalidate cache
    if (event.data && event.data.type === 'ACTIVE_OFFLINE_SESSION_UPDATE') {
        console.log('Service Worker: Received active offline session update, invalidating cache');
        cachedOfflineStatus = null;
        cachedActiveOfflineSession = null;
        lastStatusCheckTime = 0;
    }
    
    // Handle immediate transition to online mode (for going online process)
    if (event.data && event.data.type === 'GO_ONLINE_IMMEDIATE') {
        console.log('Service Worker: Received GO_ONLINE_IMMEDIATE - setting cached status to online');
        cachedOfflineStatus = false; // Online
        cachedActiveOfflineSession = false; // No active offline session
        lastStatusCheckTime = Date.now();
        console.log('Service Worker: Immediate online status set');
    }
    
    // Handle initial status setup from main thread (to avoid first-request issues)
    if (event.data && event.data.type === 'INITIAL_STATUS_SETUP') {
        console.log('Service Worker: Received initial status setup from main thread');
        if (event.data.offlineStatus !== undefined) {
            cachedOfflineStatus = event.data.offlineStatus;
        }
        if (event.data.activeOfflineSession !== undefined) {
            cachedActiveOfflineSession = event.data.activeOfflineSession;
        }
        lastStatusCheckTime = Date.now();
        console.log('Service Worker: Initial status cached:', {
            offlineStatus: cachedOfflineStatus,
            activeOfflineSession: cachedActiveOfflineSession
        });
    }
});

self.addEventListener('activate', event => {
    console.log('Service Worker: Activating...');
    event.waitUntil(
        Promise.all([
            // Clean up old caches more carefully - but preserve current session
            caches.keys().then(cacheNames => {
                console.log('Service Worker: Found existing caches:', cacheNames);
                return Promise.all(
                    cacheNames.map(async cacheName => {
                        // Only delete caches from older versions or different sessions
                        if (cacheName.startsWith('mmria-')) {
                            // Keep current base version caches and current session cache
                            const isCurrentBaseVersion = cacheName.includes(CACHE_VERSION_BASE) && !cacheName.includes('-session-');
                            const isCurrentSession = CURRENT_SESSION_ID && cacheName.includes(`-session-${CURRENT_SESSION_ID}`);
                            
                            if (!isCurrentBaseVersion && !isCurrentSession) {
                                console.log('Service Worker: Deleting old cache:', cacheName);
                                return caches.delete(cacheName);
                            }
                        }
                    })
                );
            }),
            // Verify cache integrity before claiming clients
            verifyCacheIntegrity().then(isValid => {
                if (isValid) {
                    console.log('Service Worker: Cache verification passed, claiming clients');
                    return self.clients.claim();
                } else {
                    console.warn('Service Worker: Cache verification failed, will not claim clients yet');
                    // Don't claim clients if cache is not ready
                    // The service worker will still be active but won't intercept requests
                    return Promise.resolve();
                }
            }),
            // Debug: Check what's in the cache after activation
            debugCacheStatus()
        ])
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    const pathname = url.pathname;
    const fullUrl = event.request.url;
    const isOffline = !navigator.onLine;

    // Debug logging for ALL requests when offline
    if (isOffline) {
        console.log('Service Worker: 🔌 OFFLINE MODE - Intercepting ALL requests:', {
            url: fullUrl,
            method: event.request.method,
            pathname: pathname,
            origin: url.origin,
            host: url.host
        });
    }

    // Debug logging for print.css specifically
    if (pathname.includes('print.css')) {
        console.log('Service Worker: Fetch event for print.css detected:', {
            pathname: pathname,
            method: event.request.method,
            url: fullUrl,
            isOffline: isOffline
        });
    }

    // Log all fetch events when offline for debugging
    if (isOffline) {
        console.log('Service Worker: 🔌 OFFLINE - Intercepting request:', {
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
        console.log('Service Worker: Skipping excluded route:', fullUrl);
        return;
    }

    // Skip caching for network connectivity checks - always go to network
    if (url.searchParams.has('connectivity_check') || 
        url.pathname.includes('/OfflineCase/connectivity-check')) {
        console.log('Service Worker: Skipping cache for connectivity check:', fullUrl);
        return; // Let the request go directly to the network
    }

    // Skip caching for offline case setup POST requests - these need to go to server
    // BUT allow them through the service worker for proper credential handling
    if (event.request.method === 'POST' && 
        (url.pathname === '/api/OfflineCase' || url.pathname.startsWith('/api/OfflineCase/'))) {
        console.log('Service Worker: Passing through offline case API request to server:', fullUrl);
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
                    const isOfflineMode = await checkOfflineSessionStatus();
                    if (isOfflineMode) {
                        console.log('Service Worker: User in offline mode, redirecting to offline login');
                        return Response.redirect('/Account/OfflineLogin', 302);
                    } else {
                        console.log('Service Worker: User not in offline mode, allowing normal login');
                        // Let the request go through normally
                        return fetch(event.request);
                    }
                } catch (error) {
                    console.error('Service Worker: Error checking offline status for login redirect:', error);
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
            console.log('Service Worker: Processing print.css request:', pathname);
            console.log('Service Worker: STATIC_FILES includes print.css:', STATIC_FILES.includes(pathname));
        }
        
        event.respondWith(
            // Cache-first strategy for static files - try current cache first, then backup
            (async () => {
                try {
                    // Try current cache first
                    const currentCache = await caches.open(STATIC_CACHE_NAME);
                    let cachedResponse = await currentCache.match(event.request);
                    if (cachedResponse) {
                        console.log('Service Worker: ✅ Serving static file from cache:', pathname);
                        return cachedResponse;
                    }
                    
                    // Try any available static cache (for version transitions)
                    const allCacheNames = await caches.keys();
                    for (const cacheName of allCacheNames) {
                        if (cacheName.startsWith('mmria-static-')) {
                            const cache = await caches.open(cacheName);
                            cachedResponse = await cache.match(event.request);
                            if (cachedResponse) {
                                console.log('Service Worker: ✅ Serving static file from cache:', cacheName, pathname);
                                return cachedResponse;
                            }
                        }
                    }
                    
                    console.log('Service Worker: Static file not in any cache:', pathname);
                    
                    // Only try network if online
                    if (!isOffline) {
                        console.log('Service Worker: Online - trying network for static file:', pathname);
                        return fetch(event.request)
                            .then(networkResponse => {
                                // If successful, cache the response for future use
                                if (networkResponse.ok) {
                                    currentCache.put(event.request, networkResponse.clone());
                                    console.log('Service Worker: ✅ Cached static file from network:', pathname);
                                    return networkResponse;
                                } else {
                                    console.warn(`Service Worker: Network returned ${networkResponse.status} for:`, pathname);
                                    return networkResponse;
                                }
                            })
                            .catch(error => {
                                console.error('Service Worker: ❌ Network failed for static file:', pathname, error.message);
                                // Fall through to offline fallback
                                return createOfflineFallback(pathname, currentCache);
                            });
                    } else {
                        console.log('Service Worker: 🔌 OFFLINE - providing fallback for static file:', pathname);
                        return createOfflineFallback(pathname, currentCache);
                    }
                } catch (error) {
                    console.error('Service Worker: Error in static file handler:', error);
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
            console.error('Service Worker: Error creating offline fallback:', error);
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
                        // Try current cache first
                        let currentCache = await caches.open(STATIC_CACHE_NAME);
                        let cachedResponse = await currentCache.match(matchingFontFile);
                        if (cachedResponse) {
                            console.log('Service Worker: Serving font from cache:', matchingFontFile);
                            return cachedResponse;
                        }
                        
                        // Try any available static cache
                        const allCacheNames = await caches.keys();
                        for (const cacheName of allCacheNames) {
                            if (cacheName.startsWith('mmria-static-')) {
                                const cache = await caches.open(cacheName);
                                cachedResponse = await cache.match(matchingFontFile);
                                if (cachedResponse) {
                                    console.log('Service Worker: Serving font from fallback cache:', cacheName, matchingFontFile);
                                    return cachedResponse;
                                }
                            }
                        }
                        
                        console.log('Service Worker: Font not in any cache:', matchingFontFile);
                        
                        // Only try network if online
                        if (!isOffline) {
                            console.log('Service Worker: Online - trying network for font:', matchingFontFile);
                            return fetch(event.request).then(fetchResponse => {
                                if (fetchResponse.ok) {
                                    currentCache.put(event.request, fetchResponse.clone());
                                    console.log('Service Worker: Cached font from network:', matchingFontFile);
                                }
                                return fetchResponse;
                            }).catch(error => {
                                console.log('Service Worker: Failed to fetch font file:', pathname, error);
                                // Return a 404 for missing fonts
                                return new Response('Font not found', { 
                                    status: 404, 
                                    statusText: 'Not Found' 
                                });
                            });
                        } else {
                            console.log('Service Worker: 🔌 OFFLINE - font not available:', pathname);
                            return new Response('Font not available offline', { 
                                status: 404, 
                                statusText: 'Not Found' 
                            });
                        }
                    } catch (error) {
                        console.error('Service Worker: Error in font file handler:', error);
                        return new Response('Font not available', { status: 404 });
                    }
                })()
            );
            return;
        }
    }

    // Handle API requests
    if (pathname.startsWith('/api/') || pathname.startsWith('/_users/') || pathname.startsWith('/Case/')) {
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
        console.log('Service Worker: 🔌 OFFLINE - Handling unmatched request:', fullUrl);
        event.respondWith(
            (async () => {
                try {
                    // Try to find in any cache
                    const cachedResponse = await caches.match(event.request);
                    if (cachedResponse) {
                        console.log('Service Worker: ✅ Serving unmatched request from cache:', fullUrl);
                        return cachedResponse;
                    }

                    // Try backup caches
                    const allCacheNames = await caches.keys();
                    for (const cacheName of allCacheNames) {
                        if (cacheName.startsWith('mmria-')) {
                            const cache = await caches.open(cacheName);
                            const fallbackResponse = await caseInsensitiveCacheMatch(event.request, cache);
                            if (fallbackResponse) {
                                console.log('Service Worker: ✅ Serving unmatched request from fallback cache:', cacheName, fullUrl);
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
                    console.log('Service Worker: ❌ No cache available for offline request:', fullUrl);
                    return new Response('Not available offline', {
                        status: 503,
                        statusText: 'Service Unavailable'
                    });
                } catch (error) {
                    console.error('Service Worker: Error in catch-all handler:', error);
                    return new Response('Service worker error', { status: 500 });
                }
            })()
        );
        return;
    }

    // When online, let unmatched requests go to network
    console.log('Service Worker: Online - letting unmatched request go to network:', fullUrl);
});

// Handle API requests with cache-first strategy
async function handleApiRequest(request) {
    const url = new URL(request.url);
    const fullUrl = request.url;
    
    const isOffline = await checkOfflineSessionStatus();//!navigator.onLine;
    
    //check if we have an active offline session localStorage item has_active_offline_session
    const hasActiveOfflineSession = await checkActiveOfflineSession();
    
    
    //const isOffline = !navigator.onLine;
    
    // Check if this request should use cache-first strategy
    console.log(`Service Worker: Checking cache strategy for: ${request.url}`, { isOffline });
    console.log(`Service Worker: URL pathname: ${url.pathname}`);
    console.log(`Service Worker: URL pathname + search: ${url.pathname}${url.search}`);
    console.log(`Service Worker: Full URL: ${fullUrl}`);
    
    const pathWithQuery = url.pathname + url.search;
    
    const shouldUseCache = CACHED_API_ROUTES.some(pattern => {
        let matches = false;
        if (typeof pattern === 'string') {
            matches = pathWithQuery.includes(pattern) || fullUrl.includes(pattern);
            console.log(`Service Worker: String pattern "${pattern}" matches path "${pathWithQuery}": ${matches}`);
        } else {
            matches = pattern.test(pathWithQuery);
            console.log(`Service Worker: Regex pattern ${pattern} matches path "${pathWithQuery}": ${matches}`);
        }
        return matches;
    });
    
    console.log(`Service Worker: shouldUseCache = ${shouldUseCache}`);
    
    if (shouldUseCache) {
        console.log(`Service Worker: Using cache-first strategy for: ${request.url}`);
        
        // Special handling for offline-documents endpoint - always return cached case list
        if (url.pathname === '/api/case_view/offline-documents') {
            console.log('Service Worker: Handling offline-documents request with cache-first strategy');
            console.log('Service Worker: Current cache names available:', await caches.keys());
            
            try {
                // First check if we have any cached cases
                const apiCache = await caches.open(API_CACHE_NAME);
                const cachedRequests = await apiCache.keys();
                console.log('Service Worker: Found cached requests:', cachedRequests.length);
                
                // Log the URLs of cached requests for debugging
                cachedRequests.forEach((request, index) => {
                    console.log(`Service Worker: Cached request ${index + 1}:`, request.url);
                });
                
                // Get cached cases from storage
                const offlineDocuments = await getCachedOfflineCaseList();
                console.log('Service Worker: Successfully retrieved offline documents response');
                
                // Parse the response to check content (for debugging only)
                const responseClone = offlineDocuments.clone();
                const responseText = await responseClone.text();
                const responseData = JSON.parse(responseText);
                console.log('Service Worker: Offline documents response data:', {
                    total_rows: responseData.total_rows,
                    rows_count: responseData.rows?.length || 0,
                    first_row_sample: responseData.rows?.[0] || 'No rows'
                });
                
                // Return the original response object (not the parsed data)
                return offlineDocuments;
            } catch (error) {
                console.error('Service Worker: Error getting cached offline documents:', error);
                console.error('Service Worker: Error stack:', error.stack);
                
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
        
        // Try cache first for other endpoints
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            console.log(`Service Worker: ✅ Serving from cache: ${request.url}`);

            const urlPath = new URL(request.url).pathname;

            // 🔐 If it's a cached case and encrypted, decrypt on read
            if (urlPath === '/api/case' && cachedResponse.headers.get(OFFLINE_ENCRYPTION_HEADER) === '1') {
                if (!offlineCryptoKey) {
                    console.warn('Service Worker: Encrypted case in cache but no offlineCryptoKey – returning 401 to trigger re-login');
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
                    console.error('Service Worker: Failed to decrypt cached case response', err);
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
        
        console.log(`Service Worker: Cache miss, checking network availability: ${request.url}`);
        
        // Cache miss, try network only if online
        if (!isOffline) {
            try {
                console.log(`Service Worker: Online - trying network: ${request.url}`);
                const response = await fetch(request);
                
                // Cache successful responses for future use (only GET requests can be cached)
                if (response.ok && request.method === 'GET') {
                    const cache = await caches.open(API_CACHE_NAME);

                    let responseToCache = response.clone();

                    // 🔐 If this is a case endpoint and we have a key, store encrypted
                    const urlPath = new URL(request.url).pathname;
                    if (urlPath === '/api/case' && offlineCryptoKey) {
                        try {
                            responseToCache = await encryptResponseBody(responseToCache);
                            console.log('Service Worker: Cached case response ENCRYPTED from network');
                        } catch (err) {
                            console.error('Service Worker: Failed to encrypt case response from network, caching plaintext:', err);
                        }
                    }

                    cache.put(request, responseToCache.clone());
                    console.log(`Service Worker: ✅ Cached response from network: ${request.url}`);
                }
                
                return response;
                
            } catch (error) {
                console.log(`Service Worker: Network failed for cached route: ${request.url}`, error);
                // Fall through to fallback handling below
            }
        } else {
            console.log(`Service Worker: 🔌 OFFLINE - skipping network request for: ${request.url}`);
            // Fall through to fallback handling below
        }
    } else {
        // For non-cached routes, use network-first strategy only if online
        if (!isOffline) {
            try {
                console.log(`Service Worker: Online - using network-first strategy for: ${request.url}`);
                const response = await fetch(request);
                return response;
                
            } catch (error) {
                console.log('Service Worker: Network failed, trying cache for:', request.url);
                
                // For case API requests that aren't in offline mode, let the network error propagate
                // This allows prefetch operations to handle failures gracefully without service worker interference
                if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
                    const isOfflineMode = await isInOfflineMode();
                    if (!isOfflineMode) {
                        console.log('Service Worker: Not in offline mode, letting case API network error propagate naturally');
                        throw error; // Let the fetch failure propagate to the caller
                    }
                }
                
                // Network failed, try cache
                const cachedResponse = await caches.match(request);
                if (cachedResponse) {
                    return cachedResponse;
                }
                
                // Fall through to fallback handling below
            }
        } else {
            console.log(`Service Worker: 🔌 OFFLINE - trying cache for non-cached route: ${request.url}`);
            
            // When offline, try cache first for all routes
            const cachedResponse = await caches.match(request);
            if (cachedResponse) {
                console.log(`Service Worker: ✅ Serving from cache (offline): ${request.url}`);
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
            console.log('Service Worker: Serving cached cache-version from cache');
            return cachedResponse;
        }
        
        // If not cached, return error - no hardcoded fallback to avoid version mismatch
        console.log('Service Worker: Cache-version endpoint not available, returning error');
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
            console.log('Service Worker: Serving cached jurisdiction_tree from cache');
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        console.log('Service Worker: No cached jurisdiction_tree, providing fallback');
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
            console.log('Service Worker: Serving cached release-version from cache');
            return cachedResponse;
        }
        
        // If not cached, provide a reasonable fallback
        // This will only be used if the user goes offline before ever fetching the version
        console.log('Service Worker: No cached release-version, providing default fallback');
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
            console.log('Service Worker: Serving cached ui_specification from cache');
            return cachedResponse;
        }
        
        // If not cached, provide a minimal fallback
        console.log('Service Worker: No cached ui_specification, providing minimal fallback');
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
        
        console.log(`Service Worker: Looking for metadata in cache for URL: ${url.pathname}`);
        console.log(`Service Worker: Full request URL: ${request.url}`);
        
        // Try to match using both the full request and the pathname
        let cachedResponse = await cache.match(request);
        if (!cachedResponse) {
            console.log(`Service Worker: No match with full request, trying pathname: ${url.pathname}`);
            // Try matching with just the pathname
            cachedResponse = await cache.match(url.pathname);
        }
        if (!cachedResponse) {
            console.log(`Service Worker: No match with pathname, trying full URL: ${request.url}`);
            // Try matching with the full URL
            cachedResponse = await cache.match(request.url);
        }
        
        if (cachedResponse) {
            console.log('Service Worker: Serving cached metadata from cache');
            // Verify the cached data
            try {
                const testData = await cachedResponse.clone().json();
                console.log(`Service Worker: Cached metadata has ${testData.children ? testData.children.length : 'N/A'} children`);
            } catch (e) {
                console.warn('Service Worker: Could not parse cached metadata:', e);
            }
            return cachedResponse;
        }
        
        // Debug: List all cached requests to see what we have
        const allRequests = await cache.keys();
        console.log('Service Worker: Available cached requests:');
        allRequests.forEach((req, index) => {
            if (req.url.includes('metadata')) {
                console.log(`  ${index + 1}. ${req.url} (METADATA)`);
            }
        });
        
        // If not cached, provide a minimal fallback
        console.log('Service Worker: No cached metadata, providing minimal fallback');
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
            console.log('Service Worker: Serving cached validation script from cache');
            return cachedResponse;
        }
        
        // If not cached, provide a minimal fallback
        console.log('Service Worker: No cached validation script, providing minimal fallback');
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
            console.log('Service Worker: Serving cached version specification script from cache');
            return cachedResponse;
        }
        
        // If not cached, provide a minimal fallback
        console.log('Service Worker: No cached version specification script, providing minimal fallback');
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
            console.log('Service Worker: Serving cached GetFormAccess from cache');
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        console.log('Service Worker: No cached GetFormAccess, providing fallback');
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
            console.log('Service Worker: Serving cached my-user from cache');
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        console.log('Service Worker: No cached my-user, providing fallback');
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
            console.log('Service Worker: Serving cached my-roles from cache');
            return cachedResponse;
        }
        
        // If not cached, provide fallback
        console.log('Service Worker: No cached my-roles, providing fallback');
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
        console.log('Service Worker: isDuplicateCase endpoint intercepted - returning true for offline mode');
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
                console.log('Service Worker: Found offline session data in cache:', url);
                return true;
            }
        }
        
        console.log('Service Worker: No offline session data found in cache');
        return false;
    } catch (error) {
        console.error('Service Worker: Error checking cache for offline session data:', error);
        return false;
    }
}

// Helper function to check offline session status via message passing
async function checkOfflineSessionStatus() {
    try {
        const currentTime = Date.now();
        
        // Return cached status if it's still fresh (within cache duration)
        if (cachedOfflineStatus !== null && 
            (currentTime - lastStatusCheckTime) < STATUS_CACHE_DURATION) {
            console.log('Service Worker: Using cached offline status:', cachedOfflineStatus);
            return cachedOfflineStatus;
        }
        
        // We can't access localStorage directly in service worker
        // Instead, we'll use message passing to get the status from the main thread
        const clients = await self.clients.matchAll();
        
        if (clients.length === 0) {
            console.log('Service Worker: No clients available to check offline status');
            // If we have a previous cached value, use it as fallback
            if (cachedOfflineStatus !== null) {
                console.log('Service Worker: Using previous cached offline status as fallback:', cachedOfflineStatus);
                return cachedOfflineStatus;
            }
            // Otherwise default to false (online)
            return false;
        }
        
        // Ask the first available client for offline status
        return new Promise((resolve) => {
            const messageChannel = new MessageChannel();
            
            messageChannel.port1.onmessage = (event) => {
                if (event.data && event.data.type === 'OFFLINE_STATUS_RESPONSE') {
                    const isOfflineMode = event.data.isOffline === true;
                    console.log('Service Worker: Received offline status from client:', isOfflineMode);
                    
                    // Cache the result
                    cachedOfflineStatus = isOfflineMode;
                    lastStatusCheckTime = currentTime;
                    
                    resolve(isOfflineMode);
                } else {
                    console.log('Service Worker: Invalid response from client, using cached or default value');
                    // Use cached value if available, otherwise default to false
                    const fallbackStatus = cachedOfflineStatus !== null ? cachedOfflineStatus : false;
                    resolve(fallbackStatus);
                }
            };
            
            // Send request to client
            clients[0].postMessage({
                type: 'GET_OFFLINE_STATUS'
            }, [messageChannel.port2]);
            
            // Timeout after 1 second, with intelligent fallback
            setTimeout(async () => {
                console.log('Service Worker: Timeout checking offline status from client');
                
                // Use cached value if available
                if (cachedOfflineStatus !== null) {
                    console.log('Service Worker: Using cached offline status:', cachedOfflineStatus);
                    resolve(cachedOfflineStatus);
                    return;
                }
                
                // Otherwise, check if offline session data exists in cache
                const hasOfflineSession = await hasOfflineSessionInCache();
                console.log('Service Worker: Offline session in cache:', hasOfflineSession);
                
                // Cache the detected status
                cachedOfflineStatus = hasOfflineSession;
                lastStatusCheckTime = currentTime;
                
                resolve(hasOfflineSession);
            }, 1000);
        });
    } catch (error) {
        console.error('Service Worker: Error checking offline session status:', error);
        // Try to use cache check as fallback on error
        try {
            const hasOfflineSession = await hasOfflineSessionInCache();
            console.log('Service Worker: Error fallback - offline session in cache:', hasOfflineSession);
            return hasOfflineSession;
        } catch (cacheError) {
            console.error('Service Worker: Error fallback also failed:', cacheError);
            // Last resort: use cached value if available, otherwise default to false
            const fallbackStatus = cachedOfflineStatus !== null ? cachedOfflineStatus : false;
            console.log('Service Worker: Final fallback offline status:', fallbackStatus);
            return fallbackStatus;
        }
    }
}

async function checkActiveOfflineSession() {
    try {
        const currentTime = Date.now();
        
        // Return cached status if it's still fresh (within cache duration)
        if (cachedActiveOfflineSession !== null && 
            (currentTime - lastStatusCheckTime) < STATUS_CACHE_DURATION) {
            console.log('Service Worker: Using cached active offline session status:', cachedActiveOfflineSession);
            return cachedActiveOfflineSession;
        }
        
        // We can't access localStorage directly in service worker
        // Instead, we'll use message passing to get the status from the main thread
        const clients = await self.clients.matchAll();
        
        if (clients.length === 0) {
            console.log('Service Worker: No clients available to check active offline session');
            // If we have a previous cached value, use it as fallback
            if (cachedActiveOfflineSession !== null) {
                console.log('Service Worker: Using previous cached active offline session as fallback:', cachedActiveOfflineSession);
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
                    console.log('Service Worker: Received active offline session status from client:', hasActiveSession);
                    
                    // Cache the result
                    cachedActiveOfflineSession = hasActiveSession;
                    lastStatusCheckTime = currentTime;
                    
                    resolve(hasActiveSession);
                } else {
                    console.log('Service Worker: Invalid response from client, using cached or default value');
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
                console.log('Service Worker: Timeout checking active offline session from client');
                
                // Use cached value if available
                if (cachedActiveOfflineSession !== null) {
                    console.log('Service Worker: Using cached active offline session status:', cachedActiveOfflineSession);
                    resolve(cachedActiveOfflineSession);
                    return;
                }
                
                // Otherwise, check if offline session data exists in cache
                const hasOfflineSession = await hasOfflineSessionInCache();
                console.log('Service Worker: Active offline session in cache:', hasOfflineSession);
                
                // Cache the detected status
                cachedActiveOfflineSession = hasOfflineSession;
                lastStatusCheckTime = currentTime;
                
                resolve(hasOfflineSession);
            }, 1000);
        });
    } catch (error) {
        console.error('Service Worker: Error checking active offline session:', error);
        // Try to use cache check as fallback on error
        try {
            const hasOfflineSession = await hasOfflineSessionInCache();
            console.log('Service Worker: Error fallback - offline session in cache:', hasOfflineSession);
            return hasOfflineSession;
        } catch (cacheError) {
            console.error('Service Worker: Error fallback also failed:', cacheError);
            // Last resort: use cached value if available, otherwise default to false
            const fallbackStatus = cachedActiveOfflineSession !== null ? cachedActiveOfflineSession : false;
            console.log('Service Worker: Final fallback active offline session status:', fallbackStatus);
            return fallbackStatus;
        }
    }
}

// Handle page requests with cache-first strategy when offline
async function handlePageRequest(request) {
    const url = new URL(request.url);
    console.log('Service Worker: Handling page request for:', url.pathname);
    
    // Check if we're completely offline first
    const isOffline = await checkOfflineSessionStatus();//!navigator.onLine;
    
    //check if we have an active offline session localStorage item has_active_offline_session
    const hasActiveOfflineSession = await checkActiveOfflineSession();
    
    // Note: We don't redirect to offline login for page requests here.
    // The offline login page should only be accessed when the user explicitly
    // goes offline or when they're network-offline without a session.
    // Normal online browsing should not trigger redirects.
    
    // For offline mode, try cache first
    if (isOffline) {
        console.log('Service Worker: Offline detected, trying cache first for:', url.pathname);
        
        // Try current cache first
        try {
            const currentCache = await caches.open(API_CACHE_NAME);
            let cachedResponse = await caseInsensitiveCacheMatch(request, currentCache);
            if (cachedResponse) {
                console.log('Service Worker: ✅ Serving cached page from current cache:', url.pathname);
                return cachedResponse;
            }
        } catch (error) {
            console.warn('Service Worker: Error accessing current cache:', error);
        }
        
        // Try any available versioned cache
        try {
            const allCacheNames = await caches.keys();
            console.log('Service Worker: Searching all available caches:', allCacheNames);
            
            for (const cacheName of allCacheNames) {
                if (cacheName.startsWith('mmria-api-') || cacheName.startsWith('mmria-static-')) {
                    const cache = await caches.open(cacheName);
                    const cachedResponse = await caseInsensitiveCacheMatch(request, cache);
                    if (cachedResponse) {
                        console.log('Service Worker: ✅ Serving cached page from cache:', cacheName, url.pathname);
                        return cachedResponse;
                    }
                }
            }
        } catch (error) {
            console.warn('Service Worker: Error searching caches:', error);
        }
        
        // No cache available - return error
        console.error('Service Worker: No cache available for offline page:', url.pathname);
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
        console.log('Service Worker: Online detected, trying network first for:', url.pathname);
        
        // Use redirect: 'follow' for routes that may redirect (like /pdf-version)
        const response = await fetch(request, { redirect: 'follow' });
        
        // Cache successful responses to primary cache
        if (response.ok) {
            try {
                const primaryCache = await caches.open(API_CACHE_NAME);
                await primaryCache.put(request, response.clone());
                console.log('Service Worker: ✅ Cached page from network:', url.pathname);
            } catch (cacheError) {
                console.warn('Service Worker: Failed to cache response:', cacheError);
            }
        }
        
        return response;
        
    } catch (error) {
        console.error('Service Worker: Network failed for page:', request.url, error);
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
        console.log('Service Worker: Error checking offline status:', error);
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
        console.log('Service Worker: Error getting cached case data:', error);
        return null;
    }
}

// Listen for messages from main thread
self.addEventListener('message', event => {
    console.log('Service Worker: Received message:', event.data);
    const { type, data } = event.data;
    
    switch (type) {
        case 'CACHE_CASE_DATA':
            console.log('Service Worker: Caching case data for:', data.caseId);
            cacheCaseData(data.caseId, data.caseData);
            break;
        case 'CACHE_METADATA':
        case 'CACHE_METADATA_RESOURCES':
            const version = data?.version || event.data.version;
            console.log('Service Worker: Caching metadata resources for version:', version);
            cacheMetadataResources(version);
            break;
        case 'CHECK_CRITICAL_RESOURCES':
            console.log('Service Worker: Checking critical resources cache for version:', data.version);
            checkCriticalResourcesCache(data.version).then(status => {
                event.ports[0].postMessage(status);
            });
            break;
        case 'CLEAR_CACHES':
            clearAllCaches();
            break;
        case 'GET_CACHE_STATUS':
            getCacheStatus().then(status => {
                event.ports[0].postMessage(status);
            });
            break;
        case 'DEBUG_CACHE_CONTENTS':
            console.log('Service Worker: Debug cache contents requested');
            debugCacheContents();
            break;
        case 'SKIP_WAITING':
            console.log('Service Worker: Received SKIP_WAITING message');
            self.skipWaiting();
            break;
        case 'CLAIM_CLIENTS':
            console.log('Service Worker: Received CLAIM_CLIENTS message');
            self.clients.claim();
            break;
        case 'REBUILD_CACHE':
            console.log('Service Worker: Received REBUILD_CACHE message');
            rebuildCriticalCache().then(success => {
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ success });
                }
            });
            break;
        case 'VERIFY_CACHE':
            console.log('Service Worker: Received VERIFY_CACHE message');
            verifyCacheIntegrity().then(isValid => {
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ isValid });
                }
            });
            break;
        case 'INIT_OFFLINE_SESSION':
            console.log('Service Worker: Received INIT_OFFLINE_SESSION message');
            const sessionId = data.sessionId || Date.now().toString();
            initializeOfflineSessionCache(sessionId);
            // Clear previous session caches
            clearPreviousSessionCaches();
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
            break;
        case 'GET_CURRENT_SESSION_INFO':
            console.log('Service Worker: Received GET_CURRENT_SESSION_INFO message');
            if (event.ports && event.ports[0]) {
                event.ports[0].postMessage({ 
                    sessionId: CURRENT_SESSION_ID,
                    cacheVersion: CACHE_VERSION_BASE,
                    cacheNames: {
                        static: STATIC_CACHE_NAME,                        
                        api: API_CACHE_NAME
                    }
                });
            }
            break;
        case 'VALIDATE_OFFLINE_KEY':
            console.log('Service Worker: Received VALIDATE_OFFLINE_KEY message');
            validateOfflineKeyInServiceWorker(event.data.derivedKeyHash, event.data.sessionId, event);
            break;
        case 'CACHE_OFFLINE_SESSION_DATA':
            console.log('Service Worker: Received CACHE_OFFLINE_SESSION_DATA message');
            handleCacheOfflineSessionData(event.data.data, event);
            break;
        case 'GET_OFFLINE_SESSION_DATA':
            console.log('Service Worker: Received GET_OFFLINE_SESSION_DATA message');
            getOfflineSessionDataFromServiceWorker(event);
            break;
        case 'DERIVE_AND_SET_OFFLINE_KEY':
            // Main thread sends password and salt, service worker derives and stores key
            console.log('Service Worker: Received DERIVE_AND_SET_OFFLINE_KEY');
            (async () => {
                try {
                    const aesKey = await deriveAesKeyFromPassword(
                        event.data.password,
                        event.data.saltHex
                    );
                    offlineCryptoKey = aesKey;
                    
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({ success: true });
                    }
                } catch (err) {
                    console.error('Service Worker: Failed to derive and set offline key', err);
                    offlineCryptoKey = null;
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({ success: false, error: err.message });
                    }
                }
            })();
            break;

        case 'SET_OFFLINE_ENCRYPTION_KEY':
            // Legacy support: event.data.keyBytes is an ArrayBuffer with the raw AES key
            console.log('Service Worker: Received SET_OFFLINE_ENCRYPTION_KEY (legacy)');
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
                    console.error('Service Worker: Failed to import offline key', err);
                    offlineCryptoKey = null;
                    if (event.ports && event.ports[0]) {
                        event.ports[0].postMessage({ success: false, error: err.message });
                    }
                }
            })();
            break;

        case 'OFFLINE_LOGOUT_ENCRYPT_CASES':
            console.log('Service Worker: Received OFFLINE_LOGOUT_ENCRYPT_CASES');
            (async () => {
                const success = await encryptAllOfflineCasesInCache();
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ success });
                }
            })();
            break;

        case 'OFFLINE_LOGIN_DECRYPT_CASES':
            console.log('Service Worker: Received OFFLINE_LOGIN_DECRYPT_CASES');
            (async () => {
                const success = await decryptAllOfflineCasesInCache();
                if (event.ports && event.ports[0]) {
                    event.ports[0].postMessage({ success });
                }
            })();
            break;
            
        default:
            console.log('Service Worker: Unknown message type:', type);
    }
});
// ===== Key derivation function (service worker only) =====
async function deriveAesKeyFromPassword(password, saltHex) {
    const encoder = new TextEncoder();
    const passwordBytes = encoder.encode(password);
    
    // Convert hex salt string to Uint8Array
    const saltBytes = new Uint8Array(
        saltHex.match(/.{1,2}/g).map(byte => parseInt(byte, 16))
    );
    
    // Import password as base key material
    const baseKey = await crypto.subtle.importKey(
        'raw',
        passwordBytes,
        'PBKDF2',
        false,
        ['deriveKey']
    );
    
    // Derive AES-GCM key from password using PBKDF2
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
        console.log(`Service Worker: Starting to cache case ${caseId}`);
        console.log('Service Worker: Case data:', caseData);
        
        const cache = await caches.open(API_CACHE_NAME);
        const cacheUrl = `/api/case?case_id=${caseId}`;

        // Build base response
        let response = new Response(JSON.stringify(caseData), {
            headers: { 'Content-Type': 'application/json' }
        });

        // 🔐 If we have an in-memory AES key, store encrypted-at-rest
        if (offlineCryptoKey) {
            try {
                response = await encryptResponseBody(response);
                console.log(`Service Worker: Stored case ${caseId} ENCRYPTED in cache`);
            } catch (err) {
                console.error('Service Worker: Failed to encrypt case before caching, falling back to plaintext:', err);
            }
        } else {
            console.log(`Service Worker: No offlineCryptoKey – storing case ${caseId} in plaintext cache`);
        }
        
        await cache.put(cacheUrl, response);
        console.log(`Service Worker: Successfully cached case data for: ${caseId} at URL: ${cacheUrl}`);
        
        // Verify the cache was successful
        const verification = await cache.match(cacheUrl);
        if (verification) {
            console.log(`Service Worker: Verification successful - case ${caseId} is in cache (encrypted=${verification.headers.get(OFFLINE_ENCRYPTION_HEADER) === '1'})`);
        } else {
            console.error(`Service Worker: Verification failed - case ${caseId} not found in cache after put`);
        }
        
    } catch (error) {
        console.error('Service Worker: Error caching case data:', error);
    }
}

// Cache metadata resources proactively
async function cacheMetadataResources(version) {
    try {
        console.log(`Service Worker: Starting to cache metadata resources for version: ${version}`);
        
        if (!version) {
            console.warn('Service Worker: No version provided to cacheMetadataResources, using default');
            version = 'latest';
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
        
        console.log(`Service Worker: Will attempt to cache ${endpoints.length} metadata endpoints`);
        
        for (const endpoint of endpoints) {
            try {
                const fullUrl = `${baseUrl}${endpoint}`;
                console.log(`Service Worker: Fetching and caching: ${fullUrl}`);
                
                const response = await fetch(fullUrl);
                
                if (response.ok) {
                    // Clone the response to cache it
                    const responseToCache = response.clone();
                    
                    // Store using both the full URL and the relative path for better matching
                    await cache.put(fullUrl, responseToCache.clone());
                    await cache.put(endpoint, responseToCache.clone());
                    
                    console.log(`Service Worker: ✅ Successfully cached: ${endpoint}`);
                    
                    // Extra debugging for metadata specifically (but not version_specification which is JavaScript)
                    if (endpoint.includes('metadata') && !endpoint.includes('version_specification')) {
                        try {
                            const testData = await responseToCache.clone().json();
                            console.log(`Service Worker: Cached metadata has ${testData.children ? testData.children.length : 'N/A'} children`);
                            console.log(`Service Worker: Cached metadata _id: ${testData._id}`);
                            console.log(`Service Worker: Cached metadata name: ${testData.name}`);
                        } catch (e) {
                            console.warn(`Service Worker: Could not parse cached metadata:`, e);
                        }
                    } else if (endpoint.includes('version_specification')) {
                        console.log(`Service Worker: Cached version specification (JavaScript content)`);
                    }
                    
                    cachedCount++;
                } else {
                    console.warn(`Service Worker: ❌ Failed to fetch ${endpoint}: ${response.status} ${response.statusText}`);
                    failedCount++;
                }
                
            } catch (error) {
                console.error(`Service Worker: ❌ Error caching ${endpoint}:`, error);
                failedCount++;
            }
        }
        
        console.log(`Service Worker: Metadata caching complete. ✅ Cached: ${cachedCount}, ❌ Failed: ${failedCount}`);
        
        // Verify the metadata was actually cached
        const metadataEndpoint = `/api/version/${version}/metadata`;
        const verifyResponse = await cache.match(metadataEndpoint);
        if (verifyResponse) {
            try {
                const verifyData = await verifyResponse.clone().json();
                console.log(`Service Worker: ✅ Verification successful - metadata has ${verifyData.children ? verifyData.children.length : 'N/A'} children`);
            } catch (e) {
                console.warn('Service Worker: Could not parse verification metadata:', e);
            }
        } else {
            console.warn(`Service Worker: ⚠️ VERIFICATION FAILED - metadata not found in cache for ${metadataEndpoint}`);
            
            // Try to find any metadata entries
            const allRequests = await cache.keys();
            const metadataRequests = allRequests.filter(req => req.url.includes('metadata'));
            console.log('Service Worker: Found these metadata entries in cache:', metadataRequests.map(req => req.url));
        }
        
        // Cache additional common endpoints
        const additionalEndpoints = [
            '/_users/GetFormAccess',
            '/api/user/my-user',
            '/api/user_role_jurisdiction_view/my-roles',
            '/Case/GetDuplicateMultiFormList'
        ];
        
        console.log(`Service Worker: Caching ${additionalEndpoints.length} additional endpoints...`);
        
        let additionalCachedCount = 0;
        let additionalFailedCount = 0;
        
        for (const endpoint of additionalEndpoints) {
            try {
                const fullUrl = `${baseUrl}${endpoint}`;
                console.log(`Service Worker: Fetching and caching additional endpoint: ${fullUrl}`);
                const response = await fetch(fullUrl);
                
                if (response.ok) {
                    const responseToCache = response.clone();
                    
                    // Store using both the full URL and the relative path for better matching
                    await cache.put(fullUrl, responseToCache.clone());
                    await cache.put(endpoint, responseToCache.clone());
                    
                    console.log(`Service Worker: ✅ Successfully cached additional endpoint: ${endpoint}`);
                    additionalCachedCount++;
                } else {
                    console.warn(`Service Worker: ❌ Failed to fetch additional endpoint ${endpoint}: ${response.status}`);
                    additionalFailedCount++;
                }
            } catch (error) {
                console.error(`Service Worker: ❌ Error caching additional endpoint ${endpoint}:`, error);
                additionalFailedCount++;
            }
        }
        
        console.log(`Service Worker: Additional endpoints caching complete. ✅ Cached: ${additionalCachedCount}, ❌ Failed: ${additionalFailedCount}`);
        console.log(`Service Worker: 🎉 Total metadata caching process completed - Core: ${cachedCount}/${endpoints.length}, Additional: ${additionalCachedCount}/${additionalEndpoints.length}`);
        
    } catch (error) {
        console.error('Service Worker: ❌ Error in cacheMetadataResources:', error);
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
        console.log('Service Worker: All caches cleared');
    } catch (error) {
        console.error('Service Worker: Error clearing caches:', error);
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
        console.error('Service Worker: Error getting cache status:', error);
        return {};
    }
}

// Check if critical metadata resources are cached
async function checkCriticalResourcesCache(version) {
    try {
        if (!version) {
            console.warn('Service Worker: No version provided for critical resources check');
            return { allCached: false, missingResources: ['version not specified'] };
        }
        
        const cache = await caches.open(API_CACHE_NAME);
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
        
        console.log('Service Worker: Critical resources cache check:', {
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
        console.error('Service Worker: Error checking critical resources cache:', error);
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
        console.log('🔍 Service Worker: Checking cache status after activation...');
        const cache = await caches.open(STATIC_CACHE_NAME);
        const requests = await cache.keys();
        console.log(`📦 Static cache has ${requests.length} entries`);
        
        // Check for some critical files
        const criticalFiles = ['/css/index.css', '/js/jquery.min.js', '/scripts/mmria.js'];
        for (const file of criticalFiles) {
            const response = await cache.match(file);
            console.log(`${response ? '✅' : '❌'} Critical file ${file}: ${response ? 'cached' : 'missing'}`);
        }
        
        return true;
    } catch (error) {
        console.error('Service Worker: Error checking cache status:', error);
        return false;
    }
}

// Debug function to log all cached entries
async function debugCacheContents() {
    try {
        console.log('🔍 Service Worker: DEBUG - Listing all cached entries');
        
        const cacheNames = await caches.keys();
        console.log(`Found ${cacheNames.length} cache(s):`, cacheNames);
        
        for (const cacheName of cacheNames) {
            if (cacheName.startsWith('mmria-')) {
                const cache = await caches.open(cacheName);
                const requests = await cache.keys();
                
                console.log(`\n📦 Cache: ${cacheName} (${requests.length} entries)`);
                
                requests.forEach((request, index) => {
                    const url = new URL(request.url);
                    console.log(`  ${index + 1}. ${url.pathname}${url.search}`);
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
        console.error('Service Worker: Error debugging cache contents:', error);
        return { error: error.message };
    }
}

// Rebuild critical cache files if integrity check fails
async function rebuildCriticalCache() {
    try {
        console.log('Service Worker: Rebuilding critical cache...');
        
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
                    console.log(`Service Worker: Key file already cached: ${file}`);
                    successCount++;
                    continue;
                }
                
                // Try to fetch and cache the file
                const response = await fetch(file);
                if (response.ok) {
                    await cache.put(file, response);
                    console.log(`Service Worker: Successfully cached key file: ${file}`);
                    successCount++;
                } else {
                    console.error(`Service Worker: Failed to fetch key file ${file}: ${response.status}`);
                    failureCount++;
                }
            } catch (error) {
                console.error(`Service Worker: Error caching key file ${file}:`, error);
                failureCount++;
            }
        }
        
        const success = failureCount === 0;
        console.log(`Service Worker: Key cache rebuild complete - Success: ${successCount}, Failed: ${failureCount}`);
        
        // If rebuild was successful, try to claim clients
        if (success) {
            console.log('Service Worker: Critical cache rebuild successful, claiming clients');
            await self.clients.claim();
        }
        
        return success;
        
    } catch (error) {
        console.error('Service Worker: Error rebuilding critical cache:', error);
        return false;
    }
}

// Get list of cached offline cases
async function getCachedOfflineCaseList() {
    try {
        console.log('Service Worker: getCachedOfflineCaseList - Starting to retrieve cached cases');
        
        // Get the active API cache name (handles session-specific caches)
        const activeCacheName = await getActiveApiCacheName();
        const cache = await caches.open(activeCacheName);
        const requests = await cache.keys();
        
        console.log(`Service Worker: Searching API cache (${activeCacheName}) for case data. Found ${requests.length} cached requests`);
        
        const caseList = [];
        
        // Process all cached requests in the API cache looking for case data
        for (const request of requests) {
            const url = new URL(request.url);
            console.log(`Service Worker: Processing cached request: ${url.pathname}${url.search}`);
            
            // Check if this is a case data request
            if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
                const caseId = url.searchParams.get('case_id');
                
                // Skip if we already have this case from another cache
                if (caseList.find(item => item.id === caseId)) {
                    console.log(`Service Worker: Skipping duplicate case ${caseId}`);
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
                        console.error('Service Worker: Failed to load/decrypt case for offline-documents list', decryptErr);
                        // If we can’t decrypt, skip this case in the list
                        continue;
                    }                   
                    
                    
                    // Debug: Log the actual structure of cached data
                    console.log('Service Worker: Cached case data structure for', caseId, ':', {
                        hasHomeRecord: !!caseData.home_record,
                        rootKeys: Object.keys(caseData),
                        homeRecordKeys: caseData.home_record ? Object.keys(caseData.home_record) : null,
                        sampleData: {
                            first_name_root: caseData.first_name,
                            first_name_home: caseData.home_record?.first_name,
                            last_name_root: caseData.last_name,
                            last_name_home: caseData.home_record?.last_name
                        }
                    });
                    
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
                    
                    console.log('Service Worker: Created case view item:', caseViewItem);
                    
                    caseList.push(caseViewItem);
                    
                } catch (error) {
                    console.error('Service Worker: Error processing cached case:', caseId, error);
                }
            }
        }
        
        console.log(`Service Worker: Found ${caseList.length} cached offline cases`);
        
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
        console.error('Service Worker: Error getting cached offline case list:', error);
        
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
        console.warn('Service Worker: No offline crypto key set, cannot encrypt cases');
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
                console.error('Service Worker: Failed to encrypt case', url.search, err);
            }
        }
    }

    console.log(`Service Worker: Encrypted ${encryptedCount} cached case(s)`);
    // After encrypting, forget the key (user is now "logged out")
    offlineCryptoKey = null;
    return true;
}

async function decryptAllOfflineCasesInCache() {
    if (!offlineCryptoKey) {
        console.warn('Service Worker: No offline crypto key set, cannot decrypt cases');
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
                console.error('Service Worker: Failed to decrypt case', url.search, err);
            }
        }
    }

    console.log(`Service Worker: Decrypted ${decryptedCount} cached case(s)`);
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
        console.error('Service Worker: Error getting attempt counter:', error);
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
        console.log('Service Worker: Attempt counter saved:', counterData);
    } catch (error) {
        console.error('Service Worker: Error saving attempt counter:', error);
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
        console.log('Service Worker: Attempt counter reset for session:', sessionId);
    } catch (error) {
        console.error('Service Worker: Error resetting attempt counter:', error);
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
            console.log(`Service Worker: Max attempts reached. Locked out until ${new Date(counterData.lockoutUntil).toISOString()}`);
        }
        
        await saveLoginAttemptCounter(counterData);
        return counterData;
    } catch (error) {
        console.error('Service Worker: Error incrementing failed attempts:', error);
        return null;
    }
}

// Function to validate derived key hash against cached session data
async function validateOfflineKeyInServiceWorker(derivedKeyHash, sessionId, messageEvent) {
    try {
        console.log('Service Worker: Validating derived key hash...');
        
        // First, check if account is locked out
        const counterData = await getLoginAttemptCounter(sessionId);
        
        if (isLockedOut(counterData)) {
            const now = Date.now();
            const remainingMs = counterData.lockoutUntil - now;
            const remainingMinutes = Math.ceil(remainingMs / (60 * 1000));
            
            console.log(`Service Worker: Account locked out. ${remainingMinutes} minutes remaining.`);
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
                        console.log('Service Worker: Found cached offline session data', {
                            hasKeySalt: !!sessionData.keySalt,
                            hasDerivedKeyHash: !!sessionData.derivedKeyHash,
                            hasOfflineKey: !!sessionData.offlineKey,
                            sessionId: sessionData.offlineSessionId
                        });
                        
                        // Validate session ID matches (if provided)
                        if (sessionId && sessionData.offlineSessionId && sessionData.offlineSessionId !== sessionId) {
                            console.log('Service Worker: Session ID mismatch, skipping this session data');
                            continue;
                        }
                        
                        // Check if derived key hash matches stored hash
                        if (sessionData.derivedKeyHash && sessionData.derivedKeyHash === derivedKeyHash) {
                            console.log('Service Worker: Derived key validation successful');
                            
                            // Reset attempt counter on successful login
                            await resetLoginAttemptCounter(sessionId);
                            
                            messageEvent.ports[0].postMessage({
                                type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
                                isValid: true,
                                isLockedOut: false
                            });
                            return;
                        }
                        
                        console.log('Service Worker: Hash mismatch - entered hash does not match stored hash');
                        
                        // Legacy support for old plaintext keys (will be phased out)
                        if (!sessionData.derivedKeyHash && sessionData.offlineKey) {
                            console.warn('Service Worker: Found legacy plaintext key - this should be upgraded');
                            // For legacy support, we can't validate since we only have the derived hash
                            // This case should not happen in normal operation
                        }
                    }
                } catch (error) {
                    console.error('Service Worker: Error reading cached session data:', error);
                }
            }
        }
        
        // If we get here, no matching key hash was found - increment failed attempts
        console.log('Service Worker: Key validation failed - no matching derived key hash found');
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
        console.error('Service Worker: Error validating derived key:', error);
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
        console.log('Service Worker: Caching offline session data...');
        console.log('Service Worker: Incoming session ID:', data.offlineSessionId);
        
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
                        console.log('Service Worker: Found existing session ID in cache:', existingSessionId);
                        break;
                    }
                } catch (err) {
                    console.warn('Service Worker: Error reading existing session data:', err);
                }
            }
        }
        
        // Only perform cleanup if this is a DIFFERENT session (session IDs don't match)
        // This preserves the current active session and only removes old/different sessions
        if (existingSessionId && existingSessionId === data.offlineSessionId) {
            console.log('Service Worker: Session ID matches existing session - updating in place, no cleanup needed');
        } else {
            console.log('Service Worker: New or different session detected - cleaning up old session data...');
            let deletedSessions = 0;
            let deletedCounters = 0;
            
            for (const request of cachedRequests) {
                const url = new URL(request.url);
                
                // Delete old session data (only if it's a different session)
                if (url.pathname.includes('offline-session-data') || 
                    (url.searchParams.has('type') && url.searchParams.get('type') === 'CACHE_OFFLINE_SESSION_DATA')) {
                    await cache.delete(request);
                    deletedSessions++;
                    console.log('Service Worker: Deleted old session:', url.pathname);
                }
                
                // Delete old attempt counters (only if it's a different session)
                if (url.pathname.includes('/offline-login-attempts/')) {
                    await cache.delete(request);
                    deletedCounters++;
                    console.log('Service Worker: Deleted old attempt counter:', url.pathname);
                }
            }
            
            console.log(`Service Worker: Cleanup complete - deleted ${deletedSessions} old session(s) and ${deletedCounters} attempt counter(s)`);
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
        console.log('Service Worker: Offline session data cached successfully for session:', data.offlineSessionId);
        
        // Notify the client of successful caching
        if (messageEvent.ports && messageEvent.ports[0]) {
            messageEvent.ports[0].postMessage({
                type: 'CACHE_OFFLINE_SESSION_DATA_RESPONSE',
                success: true
            });
        }
        
    } catch (error) {
        console.error('Service Worker: Error caching offline session data:', error);
        
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
        console.log('Service Worker: Retrieving offline session data...');
        
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
                        console.log('Service Worker: Found cached offline session data');
                        
                        messageEvent.ports[0].postMessage({
                            type: 'OFFLINE_SESSION_DATA_RESPONSE',
                            success: true,
                            sessionData: sessionData
                        });
                        return;
                    }
                } catch (error) {
                    console.error('Service Worker: Error reading cached session data:', error);
                }
            }
        }
        
        // If we get here, no session data was found
        console.log('Service Worker: No cached offline session data found');
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_SESSION_DATA_RESPONSE',
            success: false,
            error: 'No offline session data found in cache'
        });
        
    } catch (error) {
        console.error('Service Worker: Error retrieving offline session data:', error);
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_SESSION_DATA_RESPONSE',
            success: false,
            error: error.message
        });
    }
}
