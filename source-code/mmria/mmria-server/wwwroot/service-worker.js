// MMRIA Offline Service Worker
// This service worker handles caching for offline mode functionality

const CACHE_VERSION = 'v15';
const STATIC_CACHE_NAME = `mmria-static-${CACHE_VERSION}`;
const CASES_CACHE_NAME = `mmria-cases-${CACHE_VERSION}`;
const API_CACHE_NAME = `mmria-api-${CACHE_VERSION}`;

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
    
    // Utility scripts
    '/scripts/create_default_object.js',
    '/scripts/url_monitor.js',
    '/scripts/debug-cache-status.js',
    
    // Icons and images
    '/img/icon_pin.png',
    '/img/icon_unpin.png',
    '/img/online-go.svg',
    '/img/offline-info.svg',
    
    // Offline login view and required scripts
    '/Account/OfflineLogin',
    '/scripts/Account/offline_key_login.js'
];

// Routes that should be cached for offline access
const CACHED_ROUTES = [
    // Case index route
    /^\/Case\/?$/,
    // Case summary routes (for specific case IDs)
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
    /^\/Case\/([^\/]+)\/0\/committee_review$/
];

// API routes that should be cached
const CACHED_API_ROUTES = [
    /^\/api\/case\?case_id=/,
    /^\/api\/case_view\/record-id-list$/,
    /^\/api\/case_view\/offline-documents$/,
    /^\/api\/case_view$/,
    /^\/api\/version\/.*\/validation$/,
    /^\/api\/version\/.*\/ui_specification$/,
    /^\/api\/version\/.*\/metadata$/,
    /^\/api\/version\/release-version$/,
    /^\/api\/metadata$/,
    /^\/api\/metadata\/version_specification$/,
    /^\/api\/user_role_jurisdiction_view\/my-roles$/,
    /^\/api\/user\/my-user$/,
    /^\/api\/jurisdiction_tree$/,
    /^\/api\/cvsAPI$/,
    /^\/_users\/GetFormAccess$/,
    /^\/Case\/GetDuplicateMultiFormList$/
];

// Routes to exclude from caching
const EXCLUDED_ROUTES = [
    /view.*pdf/i,
    /save.*pdf/i,
    /print/i,
    /validate.*address/i,
    /geography.*context/i
];

self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');
    event.waitUntil(
        caches.open(STATIC_CACHE_NAME)
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
            .then(() => {
                console.log('Service Worker: Static files cached successfully');
                // Skip waiting to activate immediately
                return self.skipWaiting();
            })
            .catch(error => {
                console.error('Service Worker: Error caching static files:', error);
            })
    );
});

self.addEventListener('activate', event => {
    console.log('Service Worker: Activating...');
    event.waitUntil(
        Promise.all([
            // Take control of all clients immediately
            self.clients.claim(),
            // Debug: Check what's in the cache after activation
            debugCacheStatus(),
            // Clean up old caches
            caches.keys().then(cacheNames => {
                return Promise.all(
                    cacheNames.map(cacheName => {
                        if (cacheName.startsWith('mmria-') && 
                            !cacheName.includes(CACHE_VERSION)) {
                            console.log('Service Worker: Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
        ])
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    const pathname = url.pathname;
    const fullUrl = event.request.url;

    // Debug logging for print.css specifically
    if (pathname.includes('print.css')) {
        console.log('Service Worker: Fetch event for print.css detected:', {
            pathname: pathname,
            method: event.request.method,
            url: fullUrl
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
    if (event.request.method === 'POST' && url.pathname === '/api/OfflineCase') {
        console.log('Service Worker: Skipping cache for offline case setup POST request:', fullUrl);
        return; // Let the request go directly to the server for processing
    }

    // Handle static files
    if (STATIC_FILES.includes(pathname)) {
        // Special debugging for print.css
        if (pathname.includes('print.css')) {
            console.log('Service Worker: Processing print.css request:', pathname);
            console.log('Service Worker: STATIC_FILES includes print.css:', STATIC_FILES.includes(pathname));
        }
        
        event.respondWith(
            // Cache-first strategy for static files
            caches.open(STATIC_CACHE_NAME).then(cache => {
                return cache.match(event.request).then(cachedResponse => {
                    if (cachedResponse) {
                        console.log('Service Worker: ✅ Serving cached static file:', pathname);
                        return cachedResponse;
                    }
                    
                    console.log('Service Worker: Static file not in cache, trying network:', pathname);
                    
                    // Try network
                    return fetch(event.request)
                        .then(networkResponse => {
                            // If successful, cache the response for future use
                            if (networkResponse.ok) {
                                cache.put(event.request, networkResponse.clone());
                                console.log('Service Worker: ✅ Cached static file from network:', pathname);
                                return networkResponse;
                            } else {
                                console.warn(`Service Worker: Network returned ${networkResponse.status} for:`, pathname);
                                return networkResponse;
                            }
                        })
                        .catch(error => {
                            console.error('Service Worker: ❌ Network failed for static file:', pathname, error.message);
                            
                            // Provide appropriate fallbacks based on file type
                            if (pathname.endsWith('.css')) {
                                const fallbackResponse = new Response('/* CSS file not available offline */', { 
                                    status: 200, 
                                    headers: { 'Content-Type': 'text/css' }
                                });
                                // Cache the fallback for future requests
                                cache.put(event.request, fallbackResponse.clone());
                                return fallbackResponse;
                            }
                            
                            if (pathname.endsWith('.js')) {
                                const fallbackResponse = new Response('// JavaScript file not available offline\nconsole.warn("Script not available offline: ' + pathname + '");', { 
                                    status: 200, 
                                    headers: { 'Content-Type': 'application/javascript' }
                                });
                                // Cache the fallback for future requests
                                cache.put(event.request, fallbackResponse.clone());
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
                        });
                });
            })
        );
        return;
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
                caches.match(matchingFontFile)
                    .then(response => {
                        if (response) {
                            console.log('Service Worker: Serving cached font file:', matchingFontFile);
                            return response;
                        }
                        // Try to fetch and cache the font with its query parameters
                        return fetch(event.request).then(fetchResponse => {
                            if (fetchResponse.ok) {
                                const cache = caches.open(STATIC_CACHE_NAME);
                                cache.then(c => c.put(event.request, fetchResponse.clone()));
                            }
                            return fetchResponse;
                        }).catch(error => {
                            console.log('Service Worker: Failed to fetch font file (offline):', pathname);
                            // Return a 404 for missing fonts
                            return new Response('Font not found', { 
                                status: 404, 
                                statusText: 'Not Found' 
                            });
                        });
                    })
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
});

// Handle API requests with cache-first strategy for offline cases
async function handleApiRequest(request) {
    const url = new URL(request.url);
    const fullUrl = request.url;
    
    try {
        // For case API requests, serve from cache when in offline mode
        if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
            const caseId = url.searchParams.get('case_id');
            console.log(`Service Worker: Intercepted case request for: ${caseId}`);
            
            // Check if we're in offline mode
            const isOffline = await isInOfflineMode();
            console.log(`Service Worker: Offline mode status: ${isOffline}`);
            
            if (isOffline) {
                // When in offline mode, always serve from cache regardless of network
                const cachedResponse = await getCachedCaseData(caseId);
                if (cachedResponse) {
                    console.log(`Service Worker: Serving cached case data for: ${caseId}`);
                    return cachedResponse;
                } else {
                    console.log(`Service Worker: No cached data found for case: ${caseId}`);
                    return new Response(
                        JSON.stringify({ error: 'Case not available offline' }),
                        { status: 404, headers: { 'Content-Type': 'application/json' } }
                    );
                }
            }
            // When not in offline mode, continue to network (for prefetching, etc.)
        }

        // Try network first, then cache
        const response = await fetch(request);
        
        // Only cache responses for routes that are in our cached routes list
        if (response.ok && CACHED_API_ROUTES.some(pattern => {
            if (typeof pattern === 'string') {
                return fullUrl.includes(pattern);
            } else {
                return pattern.test(fullUrl);
            }
        })) {
            const cache = await caches.open(API_CACHE_NAME);
            cache.put(request, response.clone());
            console.log('Service Worker: Cached API response for:', request.url);
        }
        
        return response;
        
    } catch (error) {
        console.log('Service Worker: Network failed, trying cache for:', request.url);
        
        // For case API requests that aren't in offline mode, let the network error propagate
        // This allows prefetch operations to handle failures gracefully without service worker interference
        if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
            const isOffline = await isInOfflineMode();
            if (!isOffline) {
                console.log('Service Worker: Not in offline mode, letting case API network error propagate naturally');
                throw error; // Let the fetch failure propagate to the caller
            }
        }
        
        // Network failed, try cache
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If no cache, return a meaningful error response
        // url is already declared at function scope
        
        // Handle jurisdiction_tree endpoint specially (required for user info)
        if (url.pathname === '/api/jurisdiction_tree') {
            // First try to get from cache
            const cache = await caches.open(API_CACHE_NAME);
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
            const cache = await caches.open(API_CACHE_NAME);
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
            const cache = await caches.open(API_CACHE_NAME);
            
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
        
        // Handle offline-documents endpoint (returns cached case list)
        if (url.pathname === '/api/case_view/offline-documents') {
            console.log('Service Worker: Handling offline-documents request');
            console.log('Service Worker: Current cache names available:', await caches.keys());
            
            try {
                // First check if we have any cached cases
                const casesCache = await caches.open(CASES_CACHE_NAME);
                const cachedRequests = await casesCache.keys();
                console.log('Service Worker: Found cached requests:', cachedRequests.length);
                
                // Log the URLs of cached requests for debugging
                cachedRequests.forEach((request, index) => {
                    console.log(`Service Worker: Cached request ${index + 1}:`, request.url);
                });
                
                // Get cached cases from storage
                const offlineDocuments = await getCachedOfflineCaseList();
                console.log('Service Worker: Successfully retrieved offline documents response');
                
                // Parse the response to check content
                const responseClone = offlineDocuments.clone();
                const responseText = await responseClone.text();
                const responseData = JSON.parse(responseText);
                console.log('Service Worker: Offline documents response data:', {
                    total_rows: responseData.total_rows,
                    rows_count: responseData.rows?.length || 0,
                    first_row_sample: responseData.rows?.[0] || 'No rows'
                });
                
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
                'true',
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
}

// Handle page requests with network-first strategy
async function handlePageRequest(request) {
    try {
        // Try network first
        const response = await fetch(request);
        
        // Cache successful responses
        if (response.ok) {
            const cache = await caches.open(API_CACHE_NAME);
            cache.put(request, response.clone());
        }
        
        return response;
        
    } catch (error) {
        console.log('Service Worker: Network failed for page, trying cache:', request.url);
        
        // Network failed, try cache
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // If it's the Case route and we're offline, try to provide a basic response
        const url = new URL(request.url);
        if (url.pathname === '/Case' || url.pathname === '/Case/') {
            console.log('Service Worker: Case route not cached, but allowing fallback');
            // Return a basic redirect to try again
            return new Response('', {
                status: 200,
                headers: { 'Content-Type': 'text/html' }
            });
        }
        
        // Return a basic offline page if no cache
        return new Response(
            `<!DOCTYPE html>
            <html>
            <head>
                <title>Offline - MMRIA</title>
                <style>
                    body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
                    .offline-message { color: #666; }
                </style>
            </head>
            <body>
                <h1>You're Offline</h1>
                <p class="offline-message">This page is not available offline. Please check your connection.</p>
                <script>
                    // Try to redirect to a cached route
                    if (window.location.pathname !== '/Case') {
                        window.location.href = '/Case';
                    }
                </script>
            </body>
            </html>`,
            {
                status: 200,
                headers: { 'Content-Type': 'text/html' }
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
        const cache = await caches.open(CASES_CACHE_NAME);
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
        default:
            console.log('Service Worker: Unknown message type:', type);
    }
});

// Cache case data
async function cacheCaseData(caseId, caseData) {
    try {
        console.log(`Service Worker: Starting to cache case ${caseId}`);
        console.log('Service Worker: Case data:', caseData);
        
        const cache = await caches.open(CASES_CACHE_NAME);
        const cacheUrl = `/api/case?case_id=${caseId}`;
        
        const response = new Response(JSON.stringify(caseData), {
            headers: { 'Content-Type': 'application/json' }
        });
        
        await cache.put(cacheUrl, response);
        console.log(`Service Worker: Successfully cached case data for: ${caseId} at URL: ${cacheUrl}`);
        
        // Verify the cache was successful
        const verification = await cache.match(cacheUrl);
        if (verification) {
            console.log(`Service Worker: Verification successful - case ${caseId} is in cache`);
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

// Get list of cached offline cases
async function getCachedOfflineCaseList() {
    try {
        console.log('Service Worker: getCachedOfflineCaseList - Starting to retrieve cached cases');
        const cache = await caches.open(CASES_CACHE_NAME);
        const requests = await cache.keys();
        const caseList = [];
        
        console.log(`Service Worker: Found ${requests.length} cached requests in ${CASES_CACHE_NAME}`);
        
        for (const request of requests) {
            const url = new URL(request.url);
            console.log(`Service Worker: Processing cached request: ${url.pathname}${url.search}`);
            
            // Check if this is a case data request
            if (url.pathname === '/api/case' && url.searchParams.has('case_id')) {
                const caseId = url.searchParams.get('case_id');
                
                try {
                    const response = await cache.match(request);
                    if (response) {
                        const caseData = await response.json();
                        
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
                    }
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

// Handle messages from the main thread
self.addEventListener('message', event => {
    console.log('Service Worker: Received message:', event.data);
    
    if (event.data && event.data.type === 'VALIDATE_OFFLINE_KEY') {
        validateOfflineKeyInServiceWorker(event.data.derivedKeyHash, event.data.sessionId, event);
    } else if (event.data && event.data.type === 'CACHE_OFFLINE_SESSION_DATA') {
        // Handle caching offline session data - the data is in event.data.data
        handleCacheOfflineSessionData(event.data.data, event);
    } else if (event.data && event.data.type === 'GET_OFFLINE_SESSION_DATA') {
        // Handle retrieving cached offline session data
        getOfflineSessionDataFromServiceWorker(event);
    }
});

// Function to validate derived key hash against cached session data
async function validateOfflineKeyInServiceWorker(derivedKeyHash, sessionId, messageEvent) {
    try {
        console.log('Service Worker: Validating derived key hash...');
        
        // Look for cached offline session data
        const cache = await caches.open(API_CACHE_NAME);
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
                        
                        // Validate session ID matches (if provided)
                        if (sessionId && sessionData.offlineSessionId && sessionData.offlineSessionId !== sessionId) {
                            console.log('Service Worker: Session ID mismatch, skipping this session data');
                            continue;
                        }
                        
                        // Check if derived key hash matches stored hash
                        if (sessionData.derivedKeyHash && sessionData.derivedKeyHash === derivedKeyHash) {
                            console.log('Service Worker: Derived key validation successful');
                            messageEvent.ports[0].postMessage({
                                type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
                                isValid: true
                            });
                            return;
                        }
                        
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
        
        // If we get here, no matching key hash was found
        console.log('Service Worker: Key validation failed - no matching derived key hash found');
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
            isValid: false
        });
        
    } catch (error) {
        console.error('Service Worker: Error validating derived key:', error);
        messageEvent.ports[0].postMessage({
            type: 'OFFLINE_KEY_VALIDATION_RESPONSE',
            isValid: false
        });
    }
}

// Function to handle caching offline session data
async function handleCacheOfflineSessionData(data, messageEvent) {
    try {
        console.log('Service Worker: Caching offline session data...');
        
        const cache = await caches.open(API_CACHE_NAME);
        
        // Create a unique cache key for the offline session data
        const cacheKey = new Request(`/offline-session-data/${Date.now()}?type=CACHE_OFFLINE_SESSION_DATA`, {
            method: 'GET'
        });
        
        // Cache the session data
        const response = new Response(JSON.stringify(data), {
            status: 200,
            headers: { 'Content-Type': 'application/json' }
        });
        
        await cache.put(cacheKey, response);
        console.log('Service Worker: Offline session data cached successfully');
        
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
        
        const cache = await caches.open(API_CACHE_NAME);
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
