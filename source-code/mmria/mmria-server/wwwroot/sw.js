// MMRIA Service Worker for Offline Mode
const CACHE_VERSION = 'v7'; // Increment this when updating service worker
const CACHE_NAME = `mmria-offline-cache-${CACHE_VERSION}`;
const OFFLINE_CACHE_NAME = `mmria-offline-files-${CACHE_VERSION}`;

console.log(`Service Worker: Loading version ${CACHE_VERSION}`);

// Files to cache when entering offline mode
const CACHE_URLS = [
    // HTML pages
    '/Case',
    
    // CSS files
    '/styles/bootstrap/bootstrap.min.css',
    '/styles/jquery/jquery.timepicker.css',
    '/styles/jquery/jquery.datetimepicker.css',
    '/styles/bootstrap/bootstrap-datetimepicker.min.css',
    '/styles/bootstrap/jquery.bootstrap-touchspin.min.css',
    '/styles/flatpickr/flatpickr.min.css',
    '/styles/d3/c3.min.css',
    '/styles/d3/c3/0.7.20/c3.min.css',
    '/styles/bootstrap/bootstrap-timepicker.css',
    '/styles/trumbowyg/trumbowyg.min.css',
    '/css/animate.css',
    '/css/style.css',
    '/css/index.css',
    
    // Template Package CSS files
    '/TemplatePackage/4.0/assets/vendor/css/bootstrap.css',
    '/TemplatePackage/4.0/assets/css/app.min.css',
    '/TemplatePackage/4.0/assets/css/print.css',
    
    // jQuery core
    '/js/jquery.min.js',
    '/scripts/jquery-3.1.1.min.js',
    '/scripts/jquery-ui.min.js', // Fixed: was jquery-ui.js (doesn't exist)
    
    // Bootstrap and third-party JS
    '/TemplatePackage/4.0/assets/vendor/js/bootstrap.min.js',
    '/scripts/bootstrap/bootstrap.min.js',
    '/js/jquery.easing.min.js',
    '/js/wow.js',
    '/js/jquery.bxslider.min.js',
    '/js/custom.js',
    
    // Core JavaScript files
    '/scripts/mmria.js',
    '/scripts/offline-manager.js',
    '/scripts/case/index.js',
    '/scripts/case/search_view.js',
    '/scripts/case/conversion-calculator.js',
    '/scripts/editor/navigation_renderer.js',
    '/scripts/editor/page_renderer.js',
    '/scripts/create_default_object.js',
    '/scripts/url_monitor.js',
    '/scripts/metadata_summary.js',
    
    // jQuery plugins and utilities  
    '/scripts/jquery/moment.js',
    '/scripts/jquery/jquery.timepicker.js',
    '/scripts/jquery/jquery.numeric.min.js',
    '/scripts/jquery/jquery.datetimepicker.js',
    '/scripts/bootstrap/bootstrap-datetimepicker.min.js',
    '/scripts/bootstrap/jquery.bootstrap-touchspin.min.js',
    '/scripts/flatpickr/flatpickr.js',
    
    // Editor components (using actual existing files)
    '/scripts/editor/page_renderer/app.mmria.js', // Fixed: was app.js (doesn't exist)
    '/scripts/editor/page_renderer/boolean.js',
    '/scripts/editor/page_renderer/chart.js',
    '/scripts/editor/page_renderer/datetime.js', // Fixed: was date.js (doesn't exist)
    '/scripts/editor/page_renderer/form.mmria.js', // Fixed: was form.js (doesn't exist)
    '/scripts/editor/page_renderer/form.pmss.attachment.js',
    '/scripts/editor/page_renderer/grid.js',
    '/scripts/editor/page_renderer/group.js',
    '/scripts/editor/page_renderer/hidden.js',
    '/scripts/editor/page_renderer/jurisdiction.js',
    '/scripts/editor/page_renderer/label.js',
    '/scripts/editor/page_renderer/list.js',
    '/scripts/editor/page_renderer/number.js',
    '/scripts/editor/page_renderer/string.js',
    '/scripts/editor/page_renderer/textarea.js',
    '/scripts/editor/page_renderer/html_area.js',
    '/scripts/editor/page_renderer/time.js',
    '/scripts/editor/apply_sort.js',
    
    // D3 and visualization (multiple versions)
    '/scripts/d3/d3.min.js',
    '/scripts/d3/c3.min.js',
    '/scripts/d3/d3/v5/d3.v5.min.js',
    '/scripts/d3/c3/0.7.20/c3.min.js',
    '/scripts/rxjs/7.5.5/rxjs.umd.min.js',
    '/scripts/peg.js/0.10.0/peg.js',
    
    // Additional utilities
    '/scripts/esprima.js',
    '/scripts/escodegen.browser.js',
    
    // Rich text editor
    '/scripts/trumbowyg/trumbowyg.min.js',
    '/scripts/trumbowyg/trumbowyg.colors.min.js',
    '/scripts/trumbowyg/trumbowyg.fontsize.min.js',
    
    // Font files
    '/TemplatePackage/4.0/assets/fonts/open-sans-v15-latin-regular.woff2',
    '/TemplatePackage/4.0/assets/fonts/merriweather-v19-latin-regular.woff2',
    '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff2',
    '/fonts/open-sans-v15-latin-regular.woff2',
    '/fonts/merriweather-v19-latin-regular.woff2',
    '/fonts/cdciconfont.woff2',
    
    // API endpoints (will be dynamically cached)
    '/api/validator'
];

// Install event
self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');
    // Skip waiting to activate immediately
    self.skipWaiting();
});

// Activate event
self.addEventListener('activate', event => {
    console.log('Service Worker: Activating...');
    
    event.waitUntil(
        Promise.all([
            // Clean up old caches
            caches.keys().then(cacheNames => {
                return Promise.all(
                    cacheNames
                        .filter(cacheName => {
                            // Delete old versions of our caches
                            return (cacheName.startsWith('mmria-offline-cache-') && cacheName !== CACHE_NAME) ||
                                   (cacheName.startsWith('mmria-offline-files-') && cacheName !== OFFLINE_CACHE_NAME);
                        })
                        .map(cacheName => {
                            console.log('Service Worker: Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        })
                );
            }),
            // Claim all clients immediately
            self.clients.claim()
        ])
    );
});

// Listen for messages from the main thread
self.addEventListener('message', event => {
    const { type, data } = event.data;
    
    if (type === 'CACHE_FILES') {
        console.log('Service Worker: Starting to cache files for offline mode...');
        const releaseVersion = event.data.releaseVersion || '25.06.16';
        event.waitUntil(cacheFilesForOffline(releaseVersion));
    } else if (type === 'CLEAR_CACHE') {
        console.log('Service Worker: Clearing offline cache...');
        event.waitUntil(clearOfflineCache());
    } else if (type === 'GET_CACHE_STATUS') {
        event.waitUntil(getCacheStatus().then(status => {
            event.ports[0].postMessage({ type: 'CACHE_STATUS', data: status });
        }));
    } else if (type === 'DEBUG_CACHE') {
        event.waitUntil(debugCache().then(debug => {
            event.ports[0].postMessage({ type: 'DEBUG_CACHE', data: debug });
        }));
    }
});

// Function to cache files for offline mode
async function cacheFilesForOffline(releaseVersion = '25.06.16') {
    try {
        const cache = await caches.open(OFFLINE_CACHE_NAME);
        
        // Add dynamic API endpoints to the cache URLs
        const dynamicUrls = [
            `/api/version/${releaseVersion}/validation`,
            '/api/jurisdiction_tree',
            '/api/validator'
            // Removed '/api/cvsAPI' - requires ID parameter, would return 405
        ];
        
        // Combine static and dynamic URLs
        const allUrls = [...CACHE_URLS, ...dynamicUrls];
        
        // Cache files in batches to avoid overwhelming the browser
        const batchSize = 10;
        let cachedCount = 0;
        let failedCount = 0;
        const failedUrls = []; // Track which URLs failed
        
        for (let i = 0; i < allUrls.length; i += batchSize) {
            const batch = allUrls.slice(i, i + batchSize);
            
            const promises = batch.map(async (url) => {
                try {
                    const response = await fetch(url);
                    if (response.ok) {
                        await cache.put(url, response);
                        cachedCount++;
                        console.log(`Service Worker: Cached ${url}`);
                    } else {
                        console.warn(`Service Worker: Failed to fetch ${url} (${response.status} ${response.statusText})`);
                        failedUrls.push({ url, error: `${response.status} ${response.statusText}` });
                        failedCount++;
                    }
                } catch (error) {
                    console.error(`Service Worker: Error caching ${url}:`, error);
                    failedUrls.push({ url, error: error.message });
                    failedCount++;
                }
            });
            
            await Promise.all(promises);
            
            // Send progress update to main thread
            self.clients.matchAll().then(clients => {
                clients.forEach(client => {
                    client.postMessage({
                        type: 'CACHE_PROGRESS',
                        data: {
                            cached: cachedCount,
                            failed: failedCount,
                            failedUrls: failedUrls,
                            total: allUrls.length,
                            completed: i + batchSize >= allUrls.length
                        }
                    });
                });
            });
        }
        
        console.log(`Service Worker: Caching complete. ${cachedCount} files cached, ${failedCount} failed.`);
        console.log('Service Worker: Final failedUrls array:', failedUrls);
        if (failedUrls.length > 0) {
            console.log('Service Worker: Failed URLs:', failedUrls);
        }
        
        // Debug: List what's actually in the cache
        const cacheKeys = await cache.keys();
        console.log('Service Worker: Cached URLs:', cacheKeys.map(req => req.url));
        
        // Specifically check for validation API
        const validationCached = cacheKeys.find(req => 
            req.url.includes('/api/version/') && req.url.endsWith('/validation')
        );
        console.log('Service Worker: Validation API cached:', validationCached ? validationCached.url : 'NOT FOUND');
        
        // Notify main thread that caching is complete
        self.clients.matchAll().then(clients => {
            clients.forEach(client => {
                client.postMessage({
                    type: 'CACHE_COMPLETE',
                    data: { 
                        cached: cachedCount, 
                        failed: failedCount,
                        failedUrls: failedUrls // Include the failed URLs array
                    }
                });
            });
        });
        
    } catch (error) {
        console.error('Service Worker: Error during caching:', error);
        
        self.clients.matchAll().then(clients => {
            clients.forEach(client => {
                client.postMessage({
                    type: 'CACHE_ERROR',
                    data: { error: error.message }
                });
            });
        });
    }
}

// Function to clear offline cache
async function clearOfflineCache() {
    try {
        const deleted = await caches.delete(OFFLINE_CACHE_NAME);
        console.log(`Service Worker: Offline cache cleared (${deleted ? 'success' : 'not found'})`);
        
        // Notify main thread
        self.clients.matchAll().then(clients => {
            clients.forEach(client => {
                client.postMessage({
                    type: 'CACHE_CLEARED',
                    data: { success: deleted }
                });
            });
        });
        
    } catch (error) {
        console.error('Service Worker: Error clearing cache:', error);
        
        self.clients.matchAll().then(clients => {
            clients.forEach(client => {
                client.postMessage({
                    type: 'CACHE_CLEAR_ERROR',
                    data: { error: error.message }
                });
            });
        });
    }
}

// Function to get cache status
async function getCacheStatus() {
    try {
        const cache = await caches.open(OFFLINE_CACHE_NAME);
        const keys = await cache.keys();
        
        // Base count is static URLs, but we may have additional dynamic URLs cached
        const baseCount = CACHE_URLS.length;
        const dynamicUrlsCount = Math.max(0, keys.length - baseCount);
        const totalExpectedFiles = baseCount + Math.max(1, dynamicUrlsCount); // At least 1 for validation API
        
        return {
            hasCachedFiles: keys.length > 0,
            cachedFilesCount: keys.length,
            totalFilesToCache: totalExpectedFiles
        };
    } catch (error) {
        console.error('Service Worker: Error getting cache status:', error);
        return {
            hasCachedFiles: false,
            cachedFilesCount: 0,
            totalFilesToCache: CACHE_URLS.length + 1, // +1 for validation API
            error: error.message
        };
    }
}

// Function to debug cache contents
async function debugCache() {
    try {
        const cache = await caches.open(OFFLINE_CACHE_NAME);
        const keys = await cache.keys();
        
        const cachedUrls = keys.map(req => req.url);
        const validationApis = cachedUrls.filter(url => 
            url.includes('/api/version/') && url.endsWith('/validation')
        );
        
        return {
            totalCached: cachedUrls.length,
            cachedUrls: cachedUrls,
            validationApis: validationApis,
            hasValidationApi: validationApis.length > 0
        };
    } catch (error) {
        return {
            error: error.message,
            totalCached: 0,
            cachedUrls: [],
            validationApis: [],
            hasValidationApi: false
        };
    }
}

// Fetch event - serve cached files when offline
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);
    
    // Only handle GET requests for same-origin resources
    if (request.method !== 'GET' || url.origin !== self.location.origin) {
        return;
    }
    
    // Check if this is a file we should cache for offline mode
    const isStaticFile = CACHE_URLS.some(cachedUrl => {
        return url.pathname === cachedUrl || url.pathname.startsWith(cachedUrl);
    });
    
    const isValidationAPI = url.pathname.includes('/api/version/') && url.pathname.endsWith('/validation');
    const isJurisdictionAPI = url.pathname === '/api/jurisdiction_tree';
    const isValidatorAPI = url.pathname === '/api/validator';
    
    // Also cache common file types that are likely needed for offline
    const isCommonAsset = /\.(js|css|html|json|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf)$/i.test(url.pathname);
    
    // Scripts, CSS, HTML files and critical API endpoints should be cached
    const isCacheable = isStaticFile || isValidationAPI || isJurisdictionAPI || isValidatorAPI || isCommonAsset || 
                       url.pathname.startsWith('/scripts/') || 
                       url.pathname.startsWith('/css/') ||
                       url.pathname.startsWith('/styles/') ||
                       url.pathname.startsWith('/js/') ||
                       url.pathname.startsWith('/TemplatePackage/') ||
                       url.pathname.startsWith('/fonts/') ||
                       url.pathname === '/Case';
    
    if (isCacheable) {
        // Add detailed logging
        console.log(`Service Worker: Processing request for ${url.pathname}`);
        console.log(`Service Worker: isStaticFile=${isStaticFile}, isValidationAPI=${isValidationAPI}, isJurisdictionAPI=${isJurisdictionAPI}, isValidatorAPI=${isValidatorAPI}, isCommonAsset=${isCommonAsset}`);
        
        event.respondWith(
            // Network-first strategy: try network first, then cache
            fetch(request)
                .then(networkResponse => {
                    // If network fetch succeeds, cache it and return the fresh response
                    if (networkResponse.ok) {
                        console.log(`Service Worker: Serving fresh content and caching: ${url.pathname}`);
                        
                        // Clone the response immediately for caching
                        const responseToCache = networkResponse.clone();
                        
                        // Cache the cloned response (don't await this)
                        caches.open(OFFLINE_CACHE_NAME).then(cache => {
                            cache.put(request, responseToCache).catch(error => {
                                console.warn(`Service Worker: Failed to cache ${url.pathname}:`, error);
                            });
                        });
                        
                        return networkResponse;
                    } else {
                        console.warn(`Service Worker: Network response not ok for ${url.pathname}: ${networkResponse.status}`);
                        return networkResponse;
                    }
                })
                .catch(error => {
                    // Network failed, try to serve from cache
                    console.log(`Service Worker: Network failed, trying cache for: ${url.pathname}`);
                    console.log(`Service Worker: Error details:`, error.message);
                    
                    return caches.match(request).then(cachedResponse => {
                        if (cachedResponse) {
                            console.log(`Service Worker: Found cached response for: ${url.pathname}`);
                            return cachedResponse;
                        }
                        
                        // Try alternative matching strategies for API endpoints
                        if (isValidationAPI) {
                            console.log(`Service Worker: Trying alternative cache matching for validation API`);
                            return caches.open(OFFLINE_CACHE_NAME).then(cache => {
                                return cache.keys().then(keys => {
                                    // Look for any cached validation API endpoint
                                    const validationKey = keys.find(key => 
                                        key.url.includes('/api/version/') && key.url.endsWith('/validation')
                                    );
                                    
                                    if (validationKey) {
                                        console.log(`Service Worker: Found alternative validation cache: ${validationKey.url}`);
                                        return cache.match(validationKey);
                                    }
                                    
                                    console.log(`Service Worker: No validation API found in cache`);
                                    return null;
                                });
                            });
                        }
                        
                        if (isJurisdictionAPI) {
                            console.log(`Service Worker: Trying alternative cache matching for jurisdiction_tree API`);
                            return caches.open(OFFLINE_CACHE_NAME).then(cache => {
                                return cache.match('/api/jurisdiction_tree').then(response => {
                                    if (response) {
                                        console.log(`Service Worker: Found cached jurisdiction_tree API`);
                                        return response;
                                    }
                                    console.log(`Service Worker: No jurisdiction_tree API found in cache`);
                                    return null;
                                });
                            });
                        }
                        
                        return null;
                    }).then(response => {
                        if (response) {
                            return response;
                        }
                        
                        // No cached version available
                        console.error(`Service Worker: No cached version available for ${url.pathname}`);
                        
                        // For API endpoints, return a JSON error response
                        if (url.pathname.startsWith('/api/')) {
                            return new Response(
                                JSON.stringify({ error: 'API not available offline', offline: true }),
                                { 
                                    status: 503,
                                    headers: { 'Content-Type': 'application/json' } 
                                }
                            );
                        }
                        
                        // For HTML pages, return a generic offline page
                        if (request.headers.get('accept') && request.headers.get('accept').includes('text/html')) {
                            return new Response(
                                '<html><body><h1>Offline</h1><p>This page is not available offline.</p></body></html>',
                                { headers: { 'Content-Type': 'text/html' } }
                            );
                        }
                        
                        throw error;
                    });
                })
        );
    }
});
