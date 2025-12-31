/**
 * Offline Case Manager Module
 * Manages offline case operations and status
 */

// Helper function to disable all offline-related buttons
function disable_all_offline_buttons() {
    // Disable all "Add to Offline List" buttons
    const addButtons = document.querySelectorAll('button[id^="offline_toggle_"]');
    addButtons.forEach(button => {
        button.disabled = true;
        button.classList.add('offline-processing-disabled');
    });
    
    // Disable all "Remove from List" buttons
    const removeButtons = document.querySelectorAll('button[onclick*="remove_from_offline_list"]');
    removeButtons.forEach(button => {
        button.disabled = true;
        button.classList.add('offline-processing-disabled');
    });
    
    // Disable all "Go Offline" buttons
    const goOfflineButtons = document.querySelectorAll('button[onclick*="go_offline_clicked"]');
    goOfflineButtons.forEach(button => {
        button.disabled = true;
        button.classList.add('offline-processing-disabled');
    });
}

// Helper function to disable all processing-related buttons (abandon/delete)
function disable_all_processing_buttons() {
    // Disable all "Upload" buttons
    const uploadButtons = document.querySelectorAll('button[onclick*="sync_offline_changes"]');
    uploadButtons.forEach(button => {
        button.disabled = true;
        button.classList.add('offline-processing-disabled');
    });
    
    // Disable all "Abandon Changes" buttons
    const abandonButtons = document.querySelectorAll('button[onclick*="handle_abandon_changes_click"]');
    abandonButtons.forEach(button => {
        button.disabled = true;
        button.classList.add('offline-processing-disabled');
    });
    
    // Disable all "Delete/Abandon Changes" buttons (for offline-created cases)
    const deleteButtons = document.querySelectorAll('button[onclick*="handle_delete_changes_click"]');
    deleteButtons.forEach(button => {
        button.disabled = true;
        button.classList.add('offline-processing-disabled');
    });
}

// Helper function to enable all processing-related buttons
function enable_all_processing_buttons() {
    // Enable all "Abandon Changes" buttons
    const abandonButtons = document.querySelectorAll('button[onclick*="handle_abandon_changes_click"]');
    abandonButtons.forEach(button => {
        button.disabled = false;
        button.classList.remove('offline-processing-disabled');
    });
    
    // Enable all "Delete/Abandon Changes" buttons
    const deleteButtons = document.querySelectorAll('button[onclick*="handle_delete_changes_click"]');
    deleteButtons.forEach(button => {
        button.disabled = false;
        button.classList.remove('offline-processing-disabled');
    });
    
    // Clear the global flag
    g_processing_operation_in_progress = false;
}

// Wrapper function to handle abandon changes button click
function handle_abandon_changes_click(caseID, syncState) {
    // Prevent multiple operations from running simultaneously
    if (g_processing_operation_in_progress) {
        return;
    }
 
    
    // Call the actual modal function
    show_abandon_changes_processing_modal(caseID, syncState);
}

// Wrapper function to handle delete changes button click
function handle_delete_changes_click(caseID, syncState) {
    // Prevent multiple operations from running simultaneously
    if (g_processing_operation_in_progress) {
        return;
    }
    

    
    // Call the actual modal function
    show_delete_changes_processing_modal(caseID, syncState);
}

// Function to refresh the offline documents list
async function refresh_offline_documents_list() {
    try {
        g_ui.offline_case_view_list_by_user = await get_offline_documents();
        
        // Build index map for offline case routing
        g_offline_case_index_map = g_ui.offline_case_view_list_by_user.map(doc => doc.id);
        
        // Make the index map globally accessible for navigation
        window.g_offline_case_index_map = g_offline_case_index_map;
        
        // Initialize offline change tracking when documents are loaded
        initialize_offline_change_tracking(g_ui.offline_case_view_list_by_user);
        
        // Check if we're in offline mode
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        
    } catch (error) {
        console.error('Error refreshing offline documents list:', error);
    }
}

// Make functions globally available
window.disable_all_offline_buttons = disable_all_offline_buttons;
window.disable_all_processing_buttons = disable_all_processing_buttons;
window.enable_all_processing_buttons = enable_all_processing_buttons;
window.handle_abandon_changes_click = handle_abandon_changes_click;
window.handle_delete_changes_click = handle_delete_changes_click;
window.refresh_offline_documents_list = refresh_offline_documents_list;

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

/**
 * Process and save case data in offline mode
 * @param {Object} p_data - The case data to save
 * @param {Object} save_case_request - The save request object with Change_Stack
 * @param {string} p_note - Note/reason for the change
 * @param {Function} p_call_back - Callback function after save
 * @returns {Object} Response object with save status
 */
async function process_offline_save(p_data, save_case_request, p_note, p_call_back) {
    console.log('Offline mode detected - tracking document changes instead of saving to server');
    
    let case_response;
    
    try {
        // Create a copy of the complete change stack including all items
        // This must be done AFTER all change stack items are added (including case narrative)
        const changeStackCopy = JSON.parse(JSON.stringify(save_case_request.Change_Stack.items));
        console.log('📝 Copying change stack with', changeStackCopy.length, 'items for offline tracking');
        
        // Track the document change for offline sync with field-level changes
        if (typeof track_offline_document_change === 'function') {
            track_offline_document_change(
                p_data._id, 
                p_data, 
                p_note || 'Document modified while offline',
                changeStackCopy  // Pass the complete change stack
            );
        } else {
            console.warn('track_offline_document_change function not available');
        }
        
        // Update local storage with the modified document
        if (typeof set_local_case === 'function') {
            set_local_case(p_data, p_call_back);
        } else {
            console.warn('set_local_case function not available');
        }
        
        // Simulate successful save response for offline mode
        case_response = {
            ok: true,
            rev: p_data._rev, // Keep the same revision for offline
            id: p_data._id,
            offline_save: true
        };
        
        console.log('✅ Offline save completed for document:', p_data._id);
        console.log('✅ Simulated response:', case_response);
        
    } catch (error) {
        console.error('Error tracking offline document change:', error);
        case_response = {
            ok: false,
            error_description: 'Failed to track offline changes: ' + error.message
        };
    }
    
    return case_response;
}

/**
 * Generate offline record ID by appending "-offline" suffix if in offline mode
 * @param {string} baseRecordId - The base record ID
 * @returns {string} Record ID with "-offline" suffix if in offline mode
 */
function generateOfflineRecordId(baseRecordId) {
    const isOffline = window.OfflineStatus.isOffline();
    if (isOffline) {
        return baseRecordId + '-offline';
    }
    return baseRecordId;
}

/**
 * Handle offline setup for newly created case
 * @param {Object} result - The newly created case data
 * @param {Object} g_ui - Global UI object
 * @returns {Promise<void>}
 */
async function handleNewCaseOfflineSetup(result, g_ui) {
    const isOffline = window.OfflineStatus.isOffline();
    if (isOffline && window.g_offline_case_index_map) {
        window.g_offline_case_index_map = g_ui.case_view_list.map(c => c.id);
        console.log('Updated offline case index map after adding new case:', window.g_offline_case_index_map.length, 'cases');
        
        // Cache the new case in service worker for offline access
        try {
            const cacheUrl = `/api/case?case_id=${result._id}`;
            const cacheResponse = new Response(JSON.stringify(result), {
                headers: { 'Content-Type': 'application/json' }
            });
            
            // Use the global cache name function (gets version from server endpoint)
            // This ensures consistency with service worker cache naming
            const apiCacheName = await window.getActualApiCacheName();
            
            console.log('🎯 Using cache name for new case:', apiCacheName);
            
            // Cache the case data
            const cache = await caches.open(apiCacheName);
            await cache.put(cacheUrl, cacheResponse);
            console.log('✅ Cached new case for offline access:', result._id);
            
            // Track as new offline document
            if (typeof track_offline_document_change === 'function') {
                track_offline_document_change(
                    result._id, 
                    result, 
                    'New case created while offline'
                );
                console.log('✅ Tracked new case as offline change:', result._id);
            }
            
            // Add new case to offline_mode_case_view_list so it displays in offline mode
            if (g_ui && g_ui.offline_mode_case_view_list && Array.isArray(g_ui.offline_mode_case_view_list)) {
                const newCaseItem = {
                    id: result._id,
                    rev: result._rev,  // New cases might not have rev yet
                    key: result._id,
                    value: {
                        host_state: result.host_state,
                        jurisdiction_id: result.home_record?.jurisdiction_id,
                        first_name: result.home_record?.first_name,
                        last_name: result.home_record?.last_name,
                        record_id: result.home_record?.record_id,
                        agency_case_id: result.home_record?.agency_case_id,
                        case_status: result.home_record?.case_status?.overall_case_status,
                        review_date_projected: result.home_record?.case_status?.projected_review_date,
                        review_date_actual: result.home_record?.case_status?.committee_review_date,
                        created_by: result.created_by,
                        last_updated_by: result.last_updated_by,
                        date_created: result.date_created,
                        date_last_updated: result.date_last_updated
                    },
                    doc: result
                };
                
                // Check if case already exists in the list to avoid duplicates
                const caseExists = g_ui.offline_mode_case_view_list.some(c => c.id === result._id);
                if (!caseExists) {
                    g_ui.offline_mode_case_view_list.push(newCaseItem);
                    console.log('✅ Added new case to offline_mode_case_view_list:', result._id);
                } else {
                    console.log('ℹ️ Case already exists in offline_mode_case_view_list:', result._id);
                }
            }
            
            // Refresh the offline documents list to include the new case
            if (typeof refresh_offline_documents_list === 'function') {
                await refresh_offline_documents_list();
                console.log('✅ Refreshed offline documents list to include new case');
            }
            
        } catch (error) {
            console.error('❌ Error caching new case for offline:', error);
        }
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
    getOfflineCase: get_offline_case,
    processOfflineSave: process_offline_save,
    generateOfflineRecordId: generateOfflineRecordId,
    handleNewCaseOfflineSetup: handleNewCaseOfflineSetup
};

// Function to load offline record IDs for case/index.js
async function load_offline_record_ids_for_case_index() {
    try {
        // Use offline case list if available
        if (typeof g_ui !== 'undefined' && g_ui.offline_mode_case_view_list && g_ui.offline_mode_case_view_list.length > 0) {
            for (var i = 0; i < g_ui.offline_mode_case_view_list.length; i++) {
                let item = g_ui.offline_mode_case_view_list[i];
                if (item.value && item.value.record_id) {
                    g_record_id_list.add(item.value.record_id.toUpperCase());
                }
            }
        }
        
        // Also check regular case view list if available
        if (typeof g_ui !== 'undefined' && g_ui.case_view_list && g_ui.case_view_list.length > 0) {
            for (var i = 0; i < g_ui.case_view_list.length; i++) {
                let item = g_ui.case_view_list[i];
                if (item.value && item.value.record_id) {
                    g_record_id_list.add(item.value.record_id.toUpperCase());
                }
            }
        }
        
        console.log('Offline mode: Loaded', g_record_id_list.size, 'record IDs from cached data');
    } catch (error) {
        console.error('Error loading offline record IDs:', error);
    }
}

// Function to load offline session data for case/index.js
async function load_offline_session_data_for_case_index() {
    const offlineSessionId = localStorage.getItem('offline_session_id') || '';
    const offlineSessionData = await window.OfflineCaseManager.getCasesBySession(offlineSessionId);
    g_ui.offline_session_data = offlineSessionData;
    g_ui.offline_ids_not_changed = g_ui.offline_session_data.offline_ids.filter(id => !g_ui.offline_session_data.case_documents.some(change => change.documentId === id));
}

// Function to load processing offline session for case/index.js
async function load_processing_offline_session_for_case_index() {
    const isOfflineMode = localStorage.getItem('is_offline') || 'false';
    const processOfflineCases = localStorage.getItem('process_offline_cases') || 'false';
    const offlineSessionId = localStorage.getItem('offline_session_id');

    if(isOfflineMode !== 'true' && processOfflineCases !== 'true'){
        g_ui.offline_case_view_list_by_user = g_ui.case_view_list.filter(x=> x.value.offline_by == g_user_name && x.value.is_offline == true);
    }   
    //if(processOfflineCases ==='true' && offlineSessionId != null && offlineSessionId !=''){
         console.log('Fetching offline cases by session ID:', offlineSessionId);
        const response = await fetch(`/api/OfflineCase/active-user-session`, {///${offlineSessionId}
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });
        
        if (response.ok) {
            const result = await response.json();
            if(result && result.error !=="no active sessions"){
                g_ui.process_offline_case_view_list_by_user = result;

                // Check if offline_session_id is not set and set it from the response
                if (!offlineSessionId || offlineSessionId === 'null' || offlineSessionId === '') {
                    console.log('Setting offline_session_id from response:', result._id);
                    localStorage.setItem('offline_session_id', result._id);
                }

                if(g_ui.process_offline_case_view_list_by_user.offline_state === 0){
                    localStorage.setItem('abandon_offline_session', 'true');
                    //localStorage.setItem('offline_session_id', g_ui.process_offline_case_view_list_by_user._id)
                }else if(g_ui.process_offline_case_view_list_by_user.offline_state === 1){
                    localStorage.setItem('process_offline_cases', 'true');
                    
                    // Fix race condition: Populate offline_ids_not_changed here as well
                    // This ensures it's set even on first load when process_offline_cases wasn't true yet
                    if (result.offline_ids && result.case_documents) {
                        g_ui.offline_ids_not_changed = result.offline_ids.filter(id => 
                            !result.case_documents.some(change => change.documentId === id)
                        );
                        console.log('Populated offline_ids_not_changed on first load:', g_ui.offline_ids_not_changed.length, 'cases without changes');
                    }
                }
            }
        } 
    //}
}

// Function to load offline mode cases for case/index.js
async function load_offline_mode_cases_for_case_index(p_call_back, default_object) {
    console.log('In offline mode - loading cached metadata and cases');
    
    // Ensure initialization is complete
    await ensure_offline_initialization();
    
    try {
        // Get offline cases and populate g_ui.case_view_list
        console.log('📡 Making request to /api/case_view/offline-documents...');
        const response = await fetch('/api/case_view/offline-documents');
        console.log('📡 Response received:', {
            status: response.status,
            statusText: response.statusText,
            headers: Object.fromEntries(response.headers.entries()),
            url: response.url
        });
        
        const offlineData = await response.json();
        
        console.log('📊 Offline case data loaded:', {
            total_rows: offlineData.total_rows,
            rows_count: offlineData.rows?.length || 0,
            first_row_sample: offlineData.rows?.[0] || 'No rows',
            full_data: offlineData
        });
        
        // Convert offline document format to case_view_list format
        if (offlineData.rows && Array.isArray(offlineData.rows)) {
            g_ui.offline_mode_case_view_list = offlineData.rows.map(row => ({
                id: row.id,
                rev: row.rev,
                key: row.key,
                value: row.value,
                doc: row.doc
            }));
            
            
            g_ui.case_view_list = offlineData.rows.map(row => ({
                id: row.id,
                rev: row.rev,
                key: row.key,
                value: row.value,
                doc: row.doc
            }));
            g_ui.case_view_request.total_rows = offlineData.total_rows || offlineData.rows.length;


            console.log('✅ Populated g_ui.case_view_list with offline cases:', g_ui.case_view_list.length, 'cases');
            console.log('Case IDs available:', g_ui.case_view_list.map(c => c.id));
            
            // Update the offline case index map with the loaded cases
            if (typeof window.OfflineCaseManager !== 'undefined' && window.OfflineCaseManager.updateOfflineCaseIndexMap) {
                window.OfflineCaseManager.updateOfflineCaseIndexMap();
            } else if (typeof update_offline_case_index_map === 'function') {
                update_offline_case_index_map();
            } else {
                console.warn('update_offline_case_index_map not available');
            }
        } else {
            console.warn('No offline cases found, initializing empty case list');
            g_ui.case_view_list = [];
            g_ui.offline_mode_case_view_list = [];
            g_ui.case_view_request.total_rows = 0;
        }
    } catch (error) {
        console.error('❌ Error loading offline cases:', error);
        g_ui.case_view_list = [];
        g_ui.case_view_request.total_rows = 0;
    }
    
    // In offline mode, we need to render the navigation too
    if (p_call_back) {
        p_call_back();
    } else {
        // Verify all required data is loaded before rendering navigation
        console.log('🎯 OFFLINE: Verifying required data before navigation render:');
        console.log('  - g_metadata exists:', typeof g_metadata !== 'undefined');
        console.log('  - g_metadata.children length:', g_metadata?.children?.length || 0);
        console.log('  - g_form_access_list size:', g_form_access_list?.size || 0);
        console.log('  - role_set size:', role_set?.size || 0);
        console.log('  - role_set contents:', role_set ? Array.from(role_set) : 'undefined');
        
        if (!g_metadata || !g_metadata.children || g_form_access_list.size === 0 || role_set.size === 0) {
            console.error('❌ Missing required data for navigation rendering!');
            console.error('  - Missing metadata:', !g_metadata || !g_metadata.children);
            console.error('  - Missing form access:', g_form_access_list.size === 0);
            console.error('  - Missing roles:', role_set.size === 0);
        } else {
            console.log('✅ All required data is available for navigation rendering');
        }
        
        // Ensure default_object exists
        if (!default_object) {
            console.log('⚠️ default_object not found, creating minimal default');
            default_object = {};
        }

        // Render navigation for offline mode
        var post_html_call_back = [];

        document.getElementById('navbar').innerHTML = navigation_render
        (
            g_metadata,
            0,
            g_ui
        ).join('');
        document.getElementById('form_content_id').innerHTML =
        '<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>';
        document.getElementById('form_content_id').innerHTML = page_render(
            g_metadata,
            default_object,
            g_ui,
            'g_metadata',
            'default_object',
            '',
            false,
            post_html_call_back,
            null,
            null
        ).join('');
        
        if (post_html_call_back.length > 0) 
        {
            const codeToEval = post_html_call_back.join('\n');
            console.log('OFFLINE: About to evaluate post_html_call_back code:');
            console.log(codeToEval);
            console.log('Code length:', codeToEval.length);
            
            try {
                eval(codeToEval);
            } catch (error) {
                console.error('OFFLINE: Error evaluating post_html_call_back:', error);
                console.error('Code that failed:', codeToEval);
            }
        }
    }
    
    // Trigger hash change handler for offline mode since we're returning early
    console.log('🔄 OFFLINE: About to trigger hash change after offline case set loaded:', window.location.href);
    console.log('🔄 OFFLINE: Hash part:', window.location.hash);
    console.log('🔄 OFFLINE: Cases available:', g_ui.case_view_list ? g_ui.case_view_list.length : 'undefined');
    console.log('🔄 OFFLINE: window.onhashchange type:', typeof window.onhashchange);
    console.log('🔄 OFFLINE: Current URL state:', g_ui.url_state);
    
    // Use setTimeout to ensure the rendering is complete before triggering hash change
    setTimeout(() => {
        console.log('🔄 OFFLINE: Inside setTimeout, about to trigger hash change');
        if (typeof window.onhashchange === 'function') {
            console.log('🔄 OFFLINE: Calling window.onhashchange with:', window.location.href);
            window.onhashchange({ isTrusted: true, newURL: window.location.href });
            console.log('🔄 OFFLINE: Hash change call completed');
        } else {
            console.error('🔄 OFFLINE: window.onhashchange is not a function:', window.onhashchange);
        }
    }, 10);
    
    console.log('🔄 OFFLINE: Set setTimeout and about to return');
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
    getOfflineCase: get_offline_case,
    processOfflineSave: process_offline_save,
    generateOfflineRecordId: generateOfflineRecordId,
    handleNewCaseOfflineSetup: handleNewCaseOfflineSetup,
    loadOfflineRecordIdsForCaseIndex: load_offline_record_ids_for_case_index,
    loadOfflineSessionDataForCaseIndex: load_offline_session_data_for_case_index,
    loadProcessingOfflineSessionForCaseIndex: load_processing_offline_session_for_case_index,
    loadOfflineModeCasesForCaseIndex: load_offline_mode_cases_for_case_index
};

// Make functions globally accessible for backward compatibility
window.update_offline_case_index_map = update_offline_case_index_map;
window.get_case_from_offline_session = get_case_from_offline_session;
window.ensure_offline_initialization = ensure_offline_initialization;
window.get_offline_case = get_offline_case;
window.load_offline_record_ids_for_case_index = load_offline_record_ids_for_case_index;
window.load_offline_session_data_for_case_index = load_offline_session_data_for_case_index;
window.load_processing_offline_session_for_case_index = load_processing_offline_session_for_case_index;
window.load_offline_mode_cases_for_case_index = load_offline_mode_cases_for_case_index;

console.log('Offline Case Manager module loaded');
