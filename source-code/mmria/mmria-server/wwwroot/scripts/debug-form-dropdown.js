// Debug utility for form dropdown issues
// Run this in the browser console when offline to debug the form dropdown

window.debugFormDropdown = function() {
    console.log('=== FORM DROPDOWN DEBUG ===');
    
    // Check global variables
    console.log('1. Global Variables:');
    console.log('   g_metadata exists:', typeof g_metadata !== 'undefined');
    console.log('   g_metadata.children length:', g_metadata?.children?.length || 0);
    console.log('   g_form_access_list exists:', typeof g_form_access_list !== 'undefined');
    console.log('   g_form_access_list size:', g_form_access_list?.size || 0);
    console.log('   role_set exists:', typeof role_set !== 'undefined'); 
    console.log('   role_set contents:', role_set ? Array.from(role_set) : 'undefined');
    
    // Check if we're in offline mode
    console.log('   is_offline:', localStorage.getItem('is_offline'));
    
    // Check metadata children
    console.log('\n2. Metadata Children:');
    if (g_metadata && g_metadata.children) {
        g_metadata.children.forEach((child, index) => {
            console.log(`   [${index}] ${child.name}: "${child.prompt}"`);
        });
    } else {
        console.log('   No metadata children found');
    }
    
    // Check form access list
    console.log('\n3. Form Access List:');
    if (g_form_access_list && g_form_access_list.size > 0) {
        for (const [key, value] of g_form_access_list.entries()) {
            console.log(`   ${key}:`, value);
        }
    } else {
        console.log('   No form access entries found');
    }
    
    // Check cache status
    console.log('\n4. Cache Status Check:');
    if (window.ServiceWorkerManager && window.ServiceWorkerManager.getCacheStatus) {
        window.ServiceWorkerManager.getCacheStatus().then(status => {
            console.log('   Cache status:', status);
        }).catch(error => {
            console.log('   Cache status error:', error);
        });
    }
    
    // Simulate dropdown generation
    console.log('\n5. Dropdown Simulation:');
    if (g_metadata && g_metadata.children && g_form_access_list && role_set) {
        let validOptions = 0;
        g_metadata.children.forEach(child => {
            const hasAccess = g_form_access_list.has(child.name);
            console.log(`   ${child.name}: hasAccess=${hasAccess}`);
            
            if (hasAccess) {
                const formAccess = g_form_access_list.get(child.name);
                let showForm = false;
                
                for (const key of Object.keys(formAccess)) {
                    const hasRole = role_set.has(key);
                    const accessLevel = formAccess[key];
                    console.log(`     Role ${key}: hasRole=${hasRole}, access=${accessLevel}`);
                    
                    if (hasRole && accessLevel !== "no_access") {
                        showForm = true;
                        break;
                    }
                }
                
                if (showForm) {
                    validOptions++;
                    console.log(`     ✅ WILL SHOW: ${child.name} - ${child.prompt}`);
                } else {
                    console.log(`     ❌ NO ACCESS: ${child.name} - ${child.prompt}`);
                }
            }
        });
        
        console.log(`\n   Total valid dropdown options: ${validOptions}`);
    } else {
        console.log('   Cannot simulate - missing required variables');
        console.log('   Missing metadata:', !g_metadata || !g_metadata.children);
        console.log('   Missing form_access_list:', !g_form_access_list);
        console.log('   Missing role_set:', !role_set);
    }
    
    // Check actual dropdown content
    console.log('\n6. Current Dropdown Content:');
    const dropdown = document.querySelector('#form_case_list_display_id');
    if (dropdown) {
        const options = dropdown.querySelectorAll('option');
        console.log(`   Found dropdown with ${options.length} options:`);
        options.forEach((option, index) => {
            console.log(`   [${index}] value="${option.value}" text="${option.textContent}"`);
        });
    } else {
        console.log('   Dropdown element not found (#form_case_list_display_id)');
    }
    
    console.log('\n=== END DEBUG ===');
};

// Also provide a function to force re-render the navigation
window.forceRerenderNavigation = function() {
    console.log('🔄 Force re-rendering navigation...');
    
    if (typeof g_render === 'function') {
        console.log('Using g_render function');
        g_render();
    } else if (typeof navigation_render === 'function' && g_metadata && g_ui) {
        console.log('Using navigation_render directly');
        document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join('');
    } else {
        console.error('Cannot re-render navigation - missing required functions or data');
    }
    
    console.log('Navigation re-render complete');
};

// Test page_render with proper parameters to ensure it works in offline mode
window.testPageRender = function() {
    console.log('🧪 Testing page_render function with full parameters...');
    
    if (typeof page_render !== 'function') {
        console.error('❌ page_render function not available');
        return;
    }
    
    if (!g_metadata || !g_ui) {
        console.error('❌ Required global variables not available');
        console.log('   - g_metadata:', typeof g_metadata !== 'undefined');
        console.log('   - g_ui:', typeof g_ui !== 'undefined');
        return;
    }
    
    try {
        const testPostHtml = [];
        const testPageRender = page_render(
            g_metadata,
            {},
            g_ui,
            'g_metadata',
            'test_object',
            '',
            false,
            testPostHtml,
            null,
            null
        );
        console.log('✅ Page render test successful, returned:', testPageRender?.length || 0, 'elements');
        console.log('📝 Post HTML callbacks generated:', testPostHtml?.length || 0);
        if (testPostHtml.length > 0) {
            console.log('📄 Sample callback:', testPostHtml[0]?.substring(0, 100) + '...');
        }
    } catch (pageError) {
        console.error('❌ Page render test failed:', pageError);
    }
};

console.log('Debug utilities loaded:');
console.log('- Run debugFormDropdown() to debug form dropdown issues');
console.log('- Run forceRerenderNavigation() to force re-render the navigation');
console.log('- Run testPageRender() to test page rendering with full parameters');
console.log('- Run testOfflineInitialization() to test offline initialization');

// Test offline initialization
window.testOfflineInitialization = function() {
    console.log('🧪 Testing offline initialization...');
    
    if (typeof ensure_offline_initialization !== 'function') {
        console.error('❌ ensure_offline_initialization function not available');
        return;
    }
    
    console.log('Current state before initialization:');
    console.log('  - g_metadata children:', g_metadata?.children?.length || 0);
    console.log('  - g_default_ui_specification exists:', typeof g_default_ui_specification !== 'undefined');
    console.log('  - g_ui exists:', typeof g_ui !== 'undefined');
    
    ensure_offline_initialization().then(() => {
        console.log('✅ Initialization complete, new state:');
        console.log('  - g_metadata children:', g_metadata?.children?.length || 0);
        console.log('  - g_default_ui_specification exists:', typeof g_default_ui_specification !== 'undefined');
        console.log('  - g_ui exists:', typeof g_ui !== 'undefined');
        
        // Test navigation render after initialization
        if (g_metadata?.children?.length > 0) {
            console.log('🔧 Testing navigation render after initialization...');
            try {
                const testNav = navigation_render(g_metadata, 0, g_ui);
                console.log('✅ Navigation render successful after init, returned:', testNav?.length || 0, 'elements');
            } catch (navError) {
                console.error('❌ Navigation render failed after init:', navError);
            }
        }
    }).catch(error => {
        console.error('❌ Initialization failed:', error);
    });
};
