// Debug utility to check service worker cache status
// Run this in the browser console to diagnose offline cache issues

async function debugServiceWorkerCache() {
    console.log('=== SERVICE WORKER CACHE DEBUG ===');
    
    // Check if service worker is available
    if (!('serviceWorker' in navigator)) {
        console.error('Service Worker not supported');
        return;
    }
    
    // Check if service worker is registered
    const registration = await navigator.serviceWorker.getRegistration();
    if (!registration) {
        console.error('No service worker registration found');
        return;
    }
    
    console.log('Service Worker registration found:', registration);
    console.log('Service Worker state:', registration.active?.state);
    
    // Check cache names
    const cacheNames = await caches.keys();
    console.log('Available cache names:', cacheNames);
    
    // Check API cache specifically
    const apiCacheNames = cacheNames.filter(name => name.includes('api'));
    console.log('API cache names:', apiCacheNames);
    
    for (const apiCacheName of apiCacheNames) {
        console.log(`\n--- Checking cache: ${apiCacheName} ---`);
        
        const cache = await caches.open(apiCacheName);
        const requests = await cache.keys();
        
        console.log(`Cached requests in ${apiCacheName}:`, requests.length);
        
        // Check for specific metadata endpoints
        const metadataEndpoints = [
            `/api/version/${g_release_version}/metadata`,
            `/api/version/${g_release_version}/ui_specification`,
            `/api/version/${g_release_version}/validation`,
            '/_users/GetFormAccess',
            '/api/user/my-user',
            '/api/user_role_jurisdiction_view/my-roles'
        ];
        
        for (const endpoint of metadataEndpoints) {
            console.log(`\n  Checking endpoint: ${endpoint}`);
            
            // Try multiple matching approaches
            const cachedResponse1 = await cache.match(endpoint);
            const cachedResponse2 = await cache.match(`${window.location.origin}${endpoint}`);
            const cachedResponse3 = await cache.match(new Request(endpoint));
            const cachedResponse4 = await cache.match(new Request(`${window.location.origin}${endpoint}`));
            
            console.log(`    Direct path match: ${!!cachedResponse1}`);
            console.log(`    Full URL match: ${!!cachedResponse2}`);
            console.log(`    Request object match: ${!!cachedResponse3}`);
            console.log(`    Full Request object match: ${!!cachedResponse4}`);
            
            const foundResponse = cachedResponse1 || cachedResponse2 || cachedResponse3 || cachedResponse4;
            
            if (foundResponse) {
                console.log(`    ✓ Found cached: ${endpoint}`);
                // Try to read the response to see if it's valid
                try {
                    const clonedResponse = foundResponse.clone();
                    const data = await clonedResponse.json();
                    if (endpoint.includes('metadata')) {
                        console.log(`      Metadata children count: ${data.children ? data.children.length : 'N/A'}`);
                        console.log(`      Metadata data_type: ${data.data_type}`);
                        console.log(`      Metadata name: ${data.name}`);
                        console.log(`      Metadata _id: ${data._id}`);
                        if (data.children && data.children.length > 0) {
                            console.log(`      First child: ${data.children[0].name || data.children[0].prompt}`);
                        }
                    }
                } catch (error) {
                    console.warn(`      Error reading cached data for ${endpoint}:`, error);
                }
            } else {
                console.log(`    ✗ Missing: ${endpoint}`);
            }
        }
        
        // List all cached URLs
        console.log(`\n  All cached URLs in ${apiCacheName}:`);
        requests.forEach((request, index) => {
            console.log(`    ${index + 1}. ${request.url}`);
        });
    }
    
    // Test metadata fetch directly
    console.log('\n=== TESTING METADATA FETCH ===');
    try {
        const metadataUrl = `/api/version/${g_release_version}/metadata`;
        console.log(`Fetching: ${metadataUrl}`);
        
        const response = await fetch(metadataUrl);
        if (response.ok) {
            const metadata = await response.json();
            console.log('✓ Metadata fetch successful');
            console.log(`  Children count: ${metadata.children ? metadata.children.length : 'N/A'}`);
            console.log(`  Data type: ${metadata.data_type}`);
            console.log(`  Name: ${metadata.name}`);
            console.log(`  _id: ${metadata._id}`);
            if (metadata.children && metadata.children.length > 0) {
                console.log(`  First child: ${metadata.children[0].name || metadata.children[0].prompt}`);
            }
        } else {
            console.error('✗ Metadata fetch failed:', response.status, response.statusText);
        }
    } catch (error) {
        console.error('✗ Metadata fetch error:', error);
    }
    
    console.log('\n=== DEBUG COMPLETE ===');
}

// Also add a function to manually trigger caching
async function manuallyTriggerCaching() {
    console.log('=== MANUALLY TRIGGERING CACHE ===');
    
    if (window.ServiceWorkerManager && window.ServiceWorkerManager.isSupported()) {
        console.log('Triggering metadata caching...');
        window.ServiceWorkerManager.cacheMetadataResources(g_release_version);
        
        // Wait a bit and check status
        setTimeout(async () => {
            console.log('Checking cache status after manual trigger...');
            const cacheStatus = await window.ServiceWorkerManager.checkCriticalResources(g_release_version);
            console.log('Cache status after manual trigger:', cacheStatus);
            
            // Also run our debug function
            setTimeout(() => {
                debugServiceWorkerCache();
            }, 1000);
        }, 5000);
    } else {
        console.error('ServiceWorkerManager not available');
    }
}

// Function to manually cache metadata with direct fetch
async function manuallyFetchAndCache() {
    console.log('=== MANUALLY FETCHING AND CACHING ===');
    
    try {
        const cache = await caches.open('mmria-api-v13'); // Use the current cache name
        const version = g_release_version || '25.08.14'; // fallback to known version
        
        console.log(`📋 Manually caching critical endpoints for version: ${version}`);
        
        // Critical endpoints to cache
        const endpoints = [
            `/api/version/${version}/metadata`,
            `/api/version/${version}/ui_specification`, 
            `/api/version/${version}/validation`,
            '/_users/GetFormAccess',
            '/api/user/my-user',
            '/api/user_role_jurisdiction_view/my-roles',
            '/api/jurisdiction_tree',
            '/api/metadata'
        ];
        
        let successCount = 0;
        let failCount = 0;
        
        for (const endpoint of endpoints) {
            try {
                console.log(`🔄 Fetching: ${endpoint}`);
                const response = await fetch(endpoint);
                
                if (response.ok) {
                    // Store it multiple ways for better compatibility
                    const fullUrl = `${window.location.origin}${endpoint}`;
                    
                    await cache.put(endpoint, response.clone());
                    await cache.put(fullUrl, response.clone());
                    await cache.put(new Request(endpoint), response.clone());
                    
                    // Special handling for metadata - verify it has children
                    if (endpoint.includes('metadata')) {
                        try {
                            const data = await response.clone().json();
                            console.log(`✅ Cached ${endpoint} - ${data.children ? data.children.length : 'N/A'} children`);
                            if (data.children && data.children.length > 0) {
                                console.log(`   First child: ${data.children[0].name || data.children[0].prompt}`);
                            }
                        } catch (e) {
                            console.log(`✅ Cached ${endpoint} (could not parse JSON)`);
                        }
                    } else {
                        console.log(`✅ Cached: ${endpoint}`);
                    }
                    successCount++;
                } else {
                    console.error(`❌ Failed to fetch ${endpoint}: ${response.status} ${response.statusText}`);
                    failCount++;
                }
            } catch (error) {
                console.error(`❌ Error fetching ${endpoint}:`, error);
                failCount++;
            }
        }
        
        console.log(`📊 Manual caching complete: ${successCount} successful, ${failCount} failed`);
        
        // Verify what we cached
        const requests = await cache.keys();
        console.log(`🔍 Cache now contains ${requests.length} requests`);
        
        // List metadata-related cached items
        const metadataRequests = requests.filter(req => req.url.includes('metadata'));
        if (metadataRequests.length > 0) {
            console.log('📋 Metadata-related cached items:');
            metadataRequests.forEach((req, index) => {
                console.log(`  ${index + 1}. ${req.url}`);
            });
        }
        
        console.log('🎯 Now try refreshing the page and testing offline mode');
        
    } catch (error) {
        console.error('❌ Error in manual caching:', error);
    }
}

// Function to check what's in global variables
function checkGlobalVariables() {
    console.log('=== GLOBAL VARIABLES ===');
    console.log('g_release_version:', typeof g_release_version !== 'undefined' ? g_release_version : 'UNDEFINED');
    console.log('g_metadata:', typeof g_metadata !== 'undefined' ? (g_metadata ? 'LOADED' : 'NULL') : 'UNDEFINED');
    console.log('g_ui:', typeof g_ui !== 'undefined' ? (g_ui ? 'LOADED' : 'NULL') : 'UNDEFINED');
    console.log('g_user_role_jurisdiction:', typeof g_user_role_jurisdiction !== 'undefined' ? (g_user_role_jurisdiction ? g_user_role_jurisdiction.length + ' roles' : 'NULL') : 'UNDEFINED');
    console.log('g_user_roles:', typeof g_user_roles !== 'undefined' ? (g_user_roles ? Object.keys(g_user_roles).length + ' roles' : 'NULL') : 'UNDEFINED');
    
    // Check offline mode status
    const isOfflineMode = localStorage.getItem('is_offline') === 'true';
    const offlineSession = localStorage.getItem('mmria_offline_session');
    console.log('🔍 Offline Mode Status:');
    console.log('  is_offline flag:', isOfflineMode);
    console.log('  offline session data:', offlineSession ? 'Present' : 'Not found');
    console.log('  navigator.onLine:', navigator.onLine);
    
    if (typeof g_metadata !== 'undefined' && g_metadata) {
        console.log('g_metadata details:');
        console.log('  data_type:', g_metadata.data_type);
        console.log('  name:', g_metadata.name);
        console.log('  children count:', g_metadata.children ? g_metadata.children.length : 'N/A');
        console.log('  _id:', g_metadata._id);
        if (g_metadata.children && g_metadata.children.length > 0) {
            console.log('  first child name:', g_metadata.children[0].name || g_metadata.children[0].prompt);
        }
    }
}

console.log('Debug functions loaded. Run:');
console.log('- debugServiceWorkerCache() to check cache status');
console.log('- manuallyTriggerCaching() to manually trigger caching');
console.log('- manuallyFetchAndCache() to manually fetch and cache metadata');
console.log('- checkGlobalVariables() to check global variables');
console.log('- testServiceWorkerCommunication() to test SW message passing');

// Test service worker communication
async function testServiceWorkerCommunication() {
    console.log('=== TESTING SERVICE WORKER COMMUNICATION ===');
    
    if (!navigator.serviceWorker.controller) {
        console.error('❌ No service worker controller available');
        return;
    }
    
    console.log('✅ Service worker controller found');
    
    // Test direct message to service worker
    navigator.serviceWorker.controller.postMessage({
        type: 'CACHE_METADATA_RESOURCES',
        data: { version: g_release_version }
    });
    
    console.log(`📤 Sent CACHE_METADATA_RESOURCES message for version: ${g_release_version}`);
    
    // Wait a bit and check cache
    setTimeout(async () => {
        console.log('🔍 Checking cache after direct message...');
        const cache = await caches.open('mmria-api-v13');
        const requests = await cache.keys();
        console.log(`📦 Cache now has ${requests.length} requests`);
        
        if (requests.length > 0) {
            console.log('📋 Cached URLs:');
            requests.forEach((req, index) => {
                console.log(`  ${index + 1}. ${req.url}`);
            });
        }
    }, 3000);
}
