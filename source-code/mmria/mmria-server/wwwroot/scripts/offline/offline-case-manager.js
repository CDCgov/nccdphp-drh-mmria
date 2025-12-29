/**
 * Offline Case Manager Module
 * Manages offline case operations and status
 */

// Global function for offline status toggle
async function toggle_offline_status(caseId, caseIndex) {
    // Prevent multiple operations from running simultaneously
    if (g_offline_operation_in_progress) {
        return;
    }
    
    try {
        // Set global flag to disable all offline buttons
        g_offline_operation_in_progress = true;
        
        // Disable all offline-related buttons immediately
        disable_all_offline_buttons();
        
        // Show loading state on clicked button
        var button = document.getElementById('offline_toggle_' + caseIndex);
        var originalContent = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Adding...';

        // Make API call to add to offline status
        var response = await fetch('/api/case/toggle-offline/' + caseId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ direction: 'add' })
        });

        var result = await response.json();
        
        if (response.ok && result.success) {
            // Success - case added to offline mode
            console.log('Case successfully added to offline mode:', caseId);
            // Clear flag before refresh so buttons render correctly
            g_offline_operation_in_progress = false;
            // Refresh case list on success
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
        } else if (result.already_in_state) {
            // Case is already offline - show modal to inform user
            console.log('Case is already in offline mode:', caseId);
            show_case_already_offline_modal();
            g_offline_operation_in_progress = false;
        } else {
            throw new Error(result.message || 'Failed to toggle offline status');
        }
    } catch (error) {
        console.log('Error toggling offline status:', error);
        g_offline_operation_in_progress = false;
    }
}

// Function to remove a case from offline list (called from offline documents table)
async function remove_from_offline_list(caseId) {
    // Prevent multiple operations from running simultaneously
    if (g_offline_operation_in_progress) {
        return;
    }
    
    try {
        // Set global flag to disable all offline buttons
        g_offline_operation_in_progress = true;
        
        // Disable all offline-related buttons immediately
        disable_all_offline_buttons();
        
        // Show loading state on clicked button
        const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Removing...';
        });

        // Make API call to remove from offline status
        const response = await fetch('/api/case/toggle-offline/' + caseId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ direction: 'remove' })
        });

        const result = await response.json();
        
        if (response.ok && result.success) {
            // Success - case removed from offline mode
            console.log('Case successfully removed from offline mode:', caseId);
            // Clear flag before refresh so buttons render correctly
            g_offline_operation_in_progress = false;
            // Refresh case list on success
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
        } else if (result.already_in_state) {
            // Case is already online - show modal to inform user
            console.log('Case is already in online mode:', caseId);
            show_case_already_online_modal();
            g_offline_operation_in_progress = false;
        } else {
            throw new Error(result.message || 'Failed to remove case from offline list');
        }
    } catch (error) {
        console.error('Error removing case from offline list:', error);
        g_offline_operation_in_progress = false;
    }
}
// Function to get offline documents
async function get_offline_documents() {
    try {
        console.log('Fetching offline documents...');
        const response = await fetch('/api/case_view/offline-documents', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        console.log('Offline documents response:', response.status, response.statusText);
        
        if (response.ok) {
            const result = await response.json();
            console.log('Offline documents result:', result);
            return result.rows || [];
        } else {
            console.error('Failed to fetch offline documents:', response.status, response.statusText);
            return [];
        }
    } catch (error) {
        console.error('Error fetching offline documents:', error);
        return [];
    }
}

// Function to fetch offline cases by session ID for processing
async function get_offline_cases_by_session(sessionId) {
    try {
        console.log('Fetching offline cases by session ID:', sessionId);
            const response = await fetch(`/api/OfflineCase/active-user-session`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        console.log('Offline cases by session response:', response.status, response.statusText);
        
        if (response.ok) {
            const result = await response.json();
            console.log('Offline cases by session result:', result);
            return result;
        } else {
            console.error('Failed to fetch offline cases by session:', response.status, response.statusText);
            return null;
        }
    } catch (error) {
        console.error('Error fetching offline cases by session:', error);
        return null;
    }
}

// Helper function to ensure offline case index map stays synchronized
function update_offline_case_index_map() {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline && typeof g_ui !== 'undefined' && g_ui.case_view_list && Array.isArray(g_ui.case_view_list)) {
        // Update the offline index map to match current case view list
        window.g_offline_case_index_map = g_ui.case_view_list.map(c => c.id);
        console.log('Updated offline case index map:', window.g_offline_case_index_map.length, 'cases');
    }
}

// Helper function to get case from offline session
function get_case_from_offline_session(p_id) {
  console.log('Looking for case in offline session:', p_id);
  
  // Verify offline session data exists
  if (!g_ui.process_offline_case_view_list_by_user || 
      !g_ui.process_offline_case_view_list_by_user.case_documents ||
      !Array.isArray(g_ui.process_offline_case_view_list_by_user.case_documents)) {
    console.error('No offline session data available');
    return null;
  }
  
  // Search for the case in the offline session documents
  for (const caseDoc of g_ui.process_offline_case_view_list_by_user.case_documents) {
    if (caseDoc.documentId === p_id) {
      // Check both lowercase and uppercase 'M' for compatibility
      const modifiedDoc = caseDoc.modifiedDocument || caseDoc.ModifiedDocument;
      
      if (modifiedDoc) {
        console.log('Found case in offline session:', p_id);
        return modifiedDoc;
      } else {
        console.warn('Case found but modifiedDocument is missing:', p_id);
        return null;
      }
    }
  }
  
  console.warn('Case not found in offline session:', p_id);
  return null;
}

// Ensure that metadata, UI specification, and g_ui are initialized for offline mode
async function ensure_offline_initialization() {
    console.log('🔧 Ensuring offline initialization...');
    
    try {
        // Check if metadata is already loaded and has children
        if (!g_metadata || !g_metadata.children || g_metadata.children.length === 0) {
            console.log('Loading metadata from cache...');
            
            // Try to load from cache first
            const metadata_url = `${location.protocol}//${location.host}/api/version/${g_release_version}/metadata`;
            let metadata_response = null;
            
            try {
                // Try cache first
                const cacheNames = await caches.keys();
                for (const cacheName of cacheNames) {
                    if (cacheName.startsWith('mmria-')) {
                        const cache = await caches.open(cacheName);
                        const cached_response = await cache.match(metadata_url);
                        if (cached_response) {
                            metadata_response = await cached_response.json();
                            console.log('✅ Metadata loaded from cache');
                            break;
                        }
                    }
                }
                
                // If not in cache, try network (will fail if truly offline)
                if (!metadata_response) {
                    const ajax_response = await $.ajax({
                        url: metadata_url,
                    });
                    metadata_response = ajax_response;
                    console.log('✅ Metadata loaded from network');
                }
            } catch (error) {
                console.error('Failed to load metadata:', error);
                throw new Error('Metadata not available offline');
            }
            
            g_metadata = metadata_response;
            console.log('✅ Metadata loaded:', g_metadata?.children?.length || 0, 'children');
            
            // Process metadata
            metadata_summary(g_metadata_summary, g_metadata, 'g_metadata', 0, 0);
            default_object = create_default_object(g_metadata, {});
            build_other_specify_lookup(g_other_specify_lookup, g_metadata);
        }
        
        // Check if UI specification is loaded
        if (!g_default_ui_specification) {
            console.log('Loading UI specification from cache...');
            
            // Try to load from cache first
            const ui_spec_url = `${location.protocol}//${location.host}/api/version/${g_release_version}/ui_specification`;
            let ui_specification_response = null;
            
            try {
                // Try cache first
                const cacheNames = await caches.keys();
                for (const cacheName of cacheNames) {
                    if (cacheName.startsWith('mmria-')) {
                        const cache = await caches.open(cacheName);
                        const cached_response = await cache.match(ui_spec_url);
                        if (cached_response) {
                            ui_specification_response = await cached_response.json();
                            console.log('✅ UI specification loaded from cache');
                            break;
                        }
                    }
                }
                
                // If not in cache, try network (will fail if truly offline)
                if (!ui_specification_response) {
                    const ajax_response = await $.ajax({
                        url: ui_spec_url,
                    });
                    ui_specification_response = ajax_response;
                    console.log('✅ UI specification loaded from network');
                }
            } catch (error) {
                console.error('Failed to load UI specification:', error);
                throw new Error('UI specification not available offline');
            }
            
            g_default_ui_specification = ui_specification_response;
            console.log('✅ UI specification loaded');
        }
        
        // Ensure g_ui is initialized
        if (typeof g_ui === 'undefined') {
            console.log('Initializing g_ui object...');
            window.g_ui = {
                case_view_list: [],
                case_view_request: {
                    total_rows: 0,
                    page: 1,
                    skip: 0,
                    take: 100
                },
                url_state: {
                    selected_form_name: null,
                    selected_id: null,
                    selected_child_id: null,
                    path_array: []
                },
                broken_rules: []
            };
            console.log('✅ g_ui object initialized');
        }
        
        console.log('✅ Offline initialization complete');
        
    } catch (error) {
        console.error('❌ Error during offline initialization:', error);
        // Fallback - create minimal structures
        if (!g_metadata) {
            g_metadata = { children: [] };
        }
        if (!g_default_ui_specification) {
            g_default_ui_specification = {};
        }
        if (typeof g_ui === 'undefined') {
            window.g_ui = { case_view_list: [], case_view_request: { total_rows: 0 } };
        }
    }
}

async function get_offline_case(p_id) 
{
  console.log('Loading offline case from cache:', p_id);

  try
  {
    // Use fetch to get case data - service worker will intercept and handle decryption
    const cache_url = `/api/case?case_id=${p_id}`;
    
    console.log('Fetching case from cache via service worker:', cache_url);
    
    // Service worker will:
    // 1. Intercept this fetch request
    // 2. Find the cached response
    // 3. Detect if it's encrypted (X-Offline-Encrypted header)
    // 4. Decrypt it using offlineCryptoKey if needed
    // 5. Return the decrypted response
    const response = await fetch(cache_url);
    
    if (response.ok) 
    {
      const case_response = await response.json();
      console.log('Retrieved offline case data (decrypted by service worker):', case_response);
      
      if (case_response) 
      {
        // Ensure metadata and UI are loaded before rendering
        await ensure_offline_initialization();
        
        if(g_is_pmss_enhanced)
        {
            // Note: Attachment list not available in offline mode
        }
    
        if(!g_is_pmss_enhanced)
        {
            g_case_narrative_original_value = case_response.case_narrative?.case_opening_overview || '';
        }

        // For offline mode, we use the cached data directly
        g_data = case_response;
        
        // Check if there are any offline changes for this document
        try {
            const offlineChanges = localStorage.getItem('mmria_offline_changes');
            if (offlineChanges) {
                const changesMap = new Map(JSON.parse(offlineChanges));
                const documentChange = changesMap.get(p_id);
                
                if (documentChange && documentChange.modifiedDocument) {
                    console.log('Found offline changes for document:', p_id);
                    // Use the modified document instead of the original cached version
                    g_data = documentChange.modifiedDocument;
                    console.log('Applied offline changes to document');
                } else {
                    console.log('No offline changes found for document:', p_id);
                }
            }
        } catch (error) {
            console.warn('Error loading offline changes for document:', p_id, error);
        }
        
        g_data_is_checked_out = false; // Cases are editable in offline mode but not "checked out" in the traditional sense
        
        // Clear autosave interval since we can't save in offline mode
        if (g_autosave_interval != null) 
        {
          clearInterval(g_autosave_interval);
          g_autosave_interval = null;
        }
        
        g_render();
      }
    } 
    else 
    {
      console.error('Case not found in offline cache or request failed:', p_id);
      console.log('Response status:', response.status);
      console.log('Response statusText:', response.statusText);
      
      // Check if this is an encryption key error (401 from service worker)
      if (response.status === 401) {
        try {
          const errorData = await response.json();
          if (errorData.error === 'offline_key_required') {
            console.error('Offline encryption key required - redirecting to offline login');
            alert('Your offline session has expired. Please log in again with your offline password.');
            window.location.href = '/Account/Offlinelogin';
            return;
          }
        } catch (err) {
          console.warn('Could not parse 401 error response:', err);
        }
      }
      
      throw new Error(`Case ${p_id} not found in offline cache or could not be decrypted (status: ${response.status})`);
    }
  }
  catch(e)
  {
    console.error('Error loading offline case:', e);
    throw e; // Re-throw the error so the caller can handle it
  }
}

// Expose the offline case manager API to the global scope
window.OfflineCaseManager = {
    toggleStatus: toggle_offline_status,
    removeFromList: remove_from_offline_list,
    getDocuments: get_offline_documents,
    getCasesBySession: get_offline_cases_by_session,
    updateOfflineCaseIndexMap: update_offline_case_index_map,
    getCaseFromOfflineSession: get_case_from_offline_session,
    ensureOfflineInitialization: ensure_offline_initialization,
    getOfflineCase: get_offline_case
};

// Make functions globally accessible for backward compatibility
window.update_offline_case_index_map = update_offline_case_index_map;
window.get_case_from_offline_session = get_case_from_offline_session;
window.ensure_offline_initialization = ensure_offline_initialization;
window.get_offline_case = get_offline_case;

console.log('Offline Case Manager module loaded');
