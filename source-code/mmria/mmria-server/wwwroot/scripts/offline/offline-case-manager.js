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
    const removeButtons = document.querySelectorAll('button[onclick*="remove_offline_mode_softlock"]');
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
        offlineLog.error('OfflineCaseManager', 'Error refreshing offline documents list:', error);
    }
}

async function restore_pending_go_offline_softlocks() {
    const storageKey = 'pending_go_offline_softlock_restore';

    try {
        const rawValue = localStorage.getItem(storageKey);
        if (!rawValue) {
            return { attempted: 0, restored: 0, failed: 0, didRestore: false };
        }

        let pendingIds = [];
        try {
            const parsed = JSON.parse(rawValue);
            if (Array.isArray(parsed)) {
                pendingIds = parsed;
            }
        } catch (_parseError) {
            pendingIds = [];
        }

        const normalizedIds = Array.from(new Set(
            pendingIds
                .map(id => (id || '').toString().trim())
                .filter(id => id.length > 0)
        ));

        if (normalizedIds.length === 0) {
            localStorage.removeItem(storageKey);
            return { attempted: 0, restored: 0, failed: 0, didRestore: false };
        }

        let tab_id = null;
        try {
            if (typeof window.mmria_get_unique_tab_id === 'function') {
                await window.mmria_get_unique_tab_id();
            }
            if (typeof get_mmria_tab_id === 'function') {
                tab_id = get_mmria_tab_id();
            }
        } catch (_ex) {
            tab_id = null;
        }

        const alreadyOfflineIds = new Set(
            (g_ui && Array.isArray(g_ui.offline_case_view_list_by_user) ? g_ui.offline_case_view_list_by_user : [])
                .map(doc => doc && doc.id ? doc.id : null)
                .filter(id => id)
        );

        let attempted = 0;
        let restored = 0;
        let failed = 0;

        for (const caseId of normalizedIds) {
            if (alreadyOfflineIds.has(caseId)) {
                restored++;
                continue;
            }

            attempted++;

            let url = '/api/case/toggle-offline/' + caseId;
            if (tab_id) {
                url += '?tab_id=' + encodeURIComponent(tab_id);
            }

            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ direction: 'add' })
                });

                let result = {};
                try {
                    result = await response.json();
                } catch (_jsonError) {
                    result = {};
                }

                if ((response.ok && result.success) || result.already_in_state) {
                    restored++;
                } else {
                    failed++;
                    offlineLog.warn('OfflineCaseManager', `Failed to restore pending go offline soft lock for case ${caseId}`, result);
                }
            } catch (error) {
                failed++;
                offlineLog.error('OfflineCaseManager', `Error restoring pending go offline soft lock for case ${caseId}:`, error);
            }
        }

        localStorage.removeItem(storageKey);
        offlineLog.log('OfflineCaseManager', 'Completed pending go offline soft lock restore', {
            attempted,
            restored,
            failed
        });

        return {
            attempted,
            restored,
            failed,
            didRestore: attempted > 0 || restored > 0
        };
    } catch (error) {
        offlineLog.error('OfflineCaseManager', 'Error restoring pending go offline soft locks:', error);
        localStorage.removeItem(storageKey);
        return { attempted: 0, restored: 0, failed: 0, didRestore: false };
    }
}

// Make functions globally available
window.disable_all_offline_buttons = disable_all_offline_buttons;
window.disable_all_processing_buttons = disable_all_processing_buttons;
window.enable_all_processing_buttons = enable_all_processing_buttons;
window.handle_abandon_changes_click = handle_abandon_changes_click;
window.handle_delete_changes_click = handle_delete_changes_click;
window.refresh_offline_documents_list = refresh_offline_documents_list;
window.restore_pending_go_offline_softlocks = restore_pending_go_offline_softlocks;

// Global function for offline status toggle
async function add_offline_mode_softlock(caseId, caseIndex) {
    // Prevent multiple operations from running simultaneously
    if (g_offline_operation_in_progress) {
        return;
    }
    
    try {
        // Set global flag to disable all offline buttons
        g_offline_operation_in_progress = true;
        

        window.OfflineModals.showLoadingSpinner();

        // Disable all offline-related buttons immediately
        //disable_all_offline_buttons();
        

        // Make API call to add to offline status (include tab id for same-user/different-tab enforcement)
        let tab_id = null;
        try {
            if (typeof window.mmria_get_unique_tab_id === 'function') {
                await window.mmria_get_unique_tab_id();
            }
            if (typeof get_mmria_tab_id === 'function') {
                tab_id = get_mmria_tab_id();
            }
        } catch (_ex) {
            tab_id = null;
        }

        let url = '/api/case/toggle-offline/' + caseId;
        if (tab_id) {
            url += '?tab_id=' + encodeURIComponent(tab_id);
        }

        var response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ direction: 'add' })
        });

        let result = {};
        try {
            result = await response.json();
        } catch (_ex) {
            result = {};
        }
        
        if (response.ok && result.success) {
            // Success - case added to offline mode
            offlineLog.log('OfflineCaseManager', 'Case successfully added to offline mode(soft lock):', caseId);

            // Show loading state on clicked button
            var button = document.getElementById('offline_toggle_' + caseIndex);
            var originalContent = button.innerHTML;
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Adding...';            
            
            // Clear flag before refresh so buttons render correctly
            g_offline_operation_in_progress = false;
            // Refresh case list on success
            if (typeof get_case_set === 'function') {
                await get_case_set();

                //window.OfflineModals.closeLoadingSpinner();
            }
        } else if (result.already_in_state) {
            // Case is already offline - show modal to inform user
            offlineLog.log('OfflineCaseManager', 'Case is already in offline mode(soft lock):', caseId);
            show_case_already_offline_modal();
            g_offline_operation_in_progress = false;
        } else {
            // If the server reports a lock-style conflict, show the same modal UX as the case editor.
            const message = (result && result.message) ? String(result.message) : '';
            const normalizedMessage = message.toLowerCase();
            const extractLockedUserName = (text, prefix) => {
                if (!text || !prefix) {
                    return '';
                }

                if (text.indexOf(prefix) !== 0) {
                    return '';
                }

                return text.substring(prefix.length).replace(/\.$/, '').trim();
            };
            if (
                normalizedMessage.indexOf('case is offline locked by another tab for this user.') > -1 ||
                normalizedMessage.indexOf('case is locked by another tab for this user.') > -1 ||
                normalizedMessage.indexOf('please close the other tab, or wait for the lock to expire.') > -1
            )
            {
                g_offline_operation_in_progress = false;
                window.OfflineModals.closeLoadingSpinner();
                try {
                    if (typeof show_add_offline_softlock_tab_conflict_modal === 'function') {
                        window.setTimeout(() => {
                            show_add_offline_softlock_tab_conflict_modal(caseId);
                        }, 175);
                    }
                } catch (_ex) {
                    // best-effort
                }
                return;
            }

            if (
                normalizedMessage.indexOf('case is locked by ') === 0 ||
                normalizedMessage.indexOf('case is offline locked by ') === 0
            )
            {
                const lockedBy =
                    extractLockedUserName(message, 'Case is locked by ') ||
                    extractLockedUserName(message, 'Case is offline locked by ');

                g_offline_operation_in_progress = false;
                window.OfflineModals.closeLoadingSpinner();
                try {
                    if (typeof show_case_locked_by_another_user_modal === 'function') {
                        window.setTimeout(() => {
                            show_case_locked_by_another_user_modal(caseId, lockedBy);
                        }, 175);
                    }
                } catch (_ex) {
                    // best-effort
                }
                return;
            }

            throw new Error(result.message || 'Failed to toggle offline status');
        }
    } catch (error) {
        offlineLog.log('OfflineCaseManager', 'Error toggling offline status:', error);
        g_offline_operation_in_progress = false;
        window.OfflineModals.closeLoadingSpinner();
    }
}

// Function to remove a case from offline list (called from offline documents table)
async function remove_offline_mode_softlock(caseId) {
    // Prevent multiple operations from running simultaneously
    if (g_offline_operation_in_progress) {
        return;
    }
    
    try {
        // Set global flag to disable all offline buttons
        g_offline_operation_in_progress = true;
        

        window.OfflineModals.showLoadingSpinner();        

        // Disable all offline-related buttons immediately
        //disable_all_offline_buttons();
        


        // Make API call to remove from offline status (include tab id for same-user/different-tab enforcement)
        let tab_id = null;
        try {
            if (typeof window.mmria_get_unique_tab_id === 'function') {
                await window.mmria_get_unique_tab_id();
            }
            if (typeof get_mmria_tab_id === 'function') {
                tab_id = get_mmria_tab_id();
            }
        } catch (_ex) {
            tab_id = null;
        }

        let url = '/api/case/toggle-offline/' + caseId;
        if (tab_id) {
            url += '?tab_id=' + encodeURIComponent(tab_id);
        }

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ direction: 'remove' })
        });

        const result = await response.json();
        
        if (response.ok && result.success) {
            // Success - case removed from offline mode
            offlineLog.log('OfflineCaseManager', 'Soft lock - Case successfully removed from offline mode:', caseId);
            
            // Show loading state on clicked button
            const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
            buttons.forEach(button => {
                button.disabled = true;
                button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Removing...';
            });            
            // Clear flag before refresh so buttons render correctly
            g_offline_operation_in_progress = false;
            // Refresh case list on success
            if (typeof get_case_set === 'function') {
                await get_case_set();
            }

            
        } else if (result.already_in_state) {
            // Case is already online - show modal to inform user
            offlineLog.log('OfflineCaseManager', 'Soft lock - Case is already in online mode:', caseId);
            show_case_already_online_modal();
            g_offline_operation_in_progress = false;
        } else {
            // If the server reports a lock-style conflict, show the same modal UX as the case editor.
            const message = (result && result.message) ? String(result.message) : '';
            const normalizedMessage = message.toLowerCase();
            const extractLockedUserName = (text, prefix) => {
                if (!text || !prefix) {
                    return '';
                }

                if (text.indexOf(prefix) !== 0) {
                    return '';
                }

                return text.substring(prefix.length).replace(/\.$/, '').trim();
            };
            if (
                normalizedMessage.indexOf('case is offline locked by another tab for this user.') > -1 ||
                normalizedMessage.indexOf('case is locked by another tab for this user.') > -1 ||
                normalizedMessage.indexOf('please close the other tab, or wait for the lock to expire.') > -1
            )
            {
                try {
                    if (typeof show_remove_offline_softlock_tab_conflict_modal === 'function') {
                        show_remove_offline_softlock_tab_conflict_modal(caseId);
                    }
                } catch (_ex) {
                    // best-effort
                }
                g_offline_operation_in_progress = false;
                window.OfflineModals.closeLoadingSpinner();
                return;
            }

            if (
                normalizedMessage.indexOf('case is offline locked by ') === 0 ||
                normalizedMessage.indexOf('case is locked by ') === 0
            )
            {
                const lockedBy =
                    extractLockedUserName(message, 'Case is offline locked by ') ||
                    extractLockedUserName(message, 'Case is locked by ');

                try {
                    if (typeof show_case_locked_by_another_user_modal === 'function') {
                        show_case_locked_by_another_user_modal(caseId, lockedBy);
                    }
                } catch (_ex) {
                    // best-effort
                }
                g_offline_operation_in_progress = false;
                window.OfflineModals.closeLoadingSpinner();
                return;
            }

            throw new Error(result.message || 'Failed to remove case from offline list');
        }
    } catch (error) {
        offlineLog.error('OfflineCaseManager', 'Error removing case from offline list:', error);
        g_offline_operation_in_progress = false;
        window.OfflineModals.closeLoadingSpinner();
    }
}
// Function to get offline documents
async function get_offline_documents() {
    try {
        offlineLog.log('OfflineCaseManager', 'Fetching offline documents...');
        if (window.OfflineIntegrityValidator) {
            await window.OfflineIntegrityValidator.validateCurrentState({
                checkPoint: 'case_list_load'
            });
        }

        const response = await fetch('/api/case_view/offline-documents', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        offlineLog.log('OfflineCaseManager', 'Offline documents response:', response.status, response.statusText);
        
        if (response.ok) {
            const result = await response.json();
            offlineLog.info('OfflineCaseManager', 'Offline documents loaded successfully', {
                loadedCaseCount: Array.isArray(result.rows) ? result.rows.length : 0
            });
            return result.rows || [];
        } else {
            offlineLog.error('OfflineCaseManager', 'Failed to fetch offline documents:', response.status, response.statusText);
            return [];
        }
    } catch (error) {
        offlineLog.error('OfflineCaseManager', 'Error fetching offline documents:', error);
        return [];
    }
}

// Function to fetch offline cases by session ID for processing
async function get_offline_cases_by_session(sessionId) {
    try {
        
            const response = await fetch(`/api/OfflineCase/active-user-session`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });     
        
        if (response.ok) {
            const result = await response.json();           
            return result;
        } else {
            offlineLog.error('OfflineCaseManager', 'Failed to fetch offline cases by session:', response.status, response.statusText);
            return null;
        }
    } catch (error) {
        offlineLog.error('OfflineCaseManager', 'Error fetching offline cases by session:', error);
        return null;
    }
}

// Helper function to ensure offline case index map stays synchronized
function update_offline_case_index_map() {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline && typeof g_ui !== 'undefined' && g_ui.case_view_list && Array.isArray(g_ui.case_view_list)) {
        // Update the offline index map to match current case view list
        window.g_offline_case_index_map = g_ui.case_view_list.map(c => c.id);
        offlineLog.log('OfflineCaseManager', 'Updated offline case index map:', window.g_offline_case_index_map.length, 'cases');
    }
}

// Helper function to get case from offline session
function get_case_from_offline_session(p_id) {
  // Verify offline session data exists
  if (!g_ui.process_offline_case_view_list_by_user || 
      !g_ui.process_offline_case_view_list_by_user.case_documents ||
      !Array.isArray(g_ui.process_offline_case_view_list_by_user.case_documents)) {
    offlineLog.error('OfflineCaseManager', 'No offline session data available');
    return null;
  }
  
  // Search for the case in the offline session documents
  for (const caseDoc of g_ui.process_offline_case_view_list_by_user.case_documents) {
    if (caseDoc.documentId === p_id) {
      // Check both lowercase and uppercase 'M' for compatibility
      const modifiedDoc = caseDoc.modifiedDocument || caseDoc.ModifiedDocument;
      
      if (modifiedDoc) {
        offlineLog.log('OfflineCaseManager', 'Found case in offline session:', p_id);
        return modifiedDoc;
      } else {
        offlineLog.warn('OfflineCaseManager', 'Case found but modifiedDocument is missing:', p_id);
        return null;
      }
    }
  }
  
  offlineLog.warn('OfflineCaseManager', 'Case not found in offline session:', p_id);
  return null;
}

// Ensure that metadata, UI specification, and g_ui are initialized for offline mode
async function ensure_offline_initialization() {
    try {
        // Check if metadata is already loaded and has children
        if (!g_metadata || !g_metadata.children || g_metadata.children.length === 0) {
            
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
                }
            } catch (error) {
                offlineLog.error('OfflineCaseManager', 'Failed to load metadata:', error);
                throw new Error('Metadata not available offline');
            }
            
            g_metadata = metadata_response;
            
            // Process metadata
            metadata_summary(g_metadata_summary, g_metadata, 'g_metadata', 0, 0);
            default_object = create_default_object(g_metadata, {});
            build_other_specify_lookup(g_other_specify_lookup, g_metadata);
        }
        
        // Check if UI specification is loaded
        if (!g_default_ui_specification) {
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
                }
            } catch (error) {
                offlineLog.error('OfflineCaseManager', 'Failed to load UI specification:', error);
                throw new Error('UI specification not available offline');
            }
            
            g_default_ui_specification = ui_specification_response;
        }
        
        // Ensure g_ui is initialized
        if (typeof g_ui === 'undefined') {
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
        }
        
        offlineLog.log('OfflineCaseManager', 'Offline initialization complete');
        
    } catch (error) {
        offlineLog.error('OfflineCaseManager', '❌ Error during offline initialization:', error);
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
  offlineLog.log('OfflineCaseManager', 'Loading offline case:', p_id);

  if (window.OfflineIntegrityValidator) {
    await window.OfflineIntegrityValidator.validateCurrentState({
      checkPoint: 'case_detail_load',
      expectedOfflineIds: [p_id]
    });
  }

  try
  {
    // Use fetch to get case data - service worker will intercept and handle decryption
    const cache_url = `/api/case?case_id=${p_id}`;
    
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
      offlineLog.log('OfflineCaseManager', 'Retrieved offline case data (decrypted by service worker):', p_id);
      
      if (case_response) 
      {
        // Ensure metadata and UI are loaded before rendering
        await ensure_offline_initialization();
        
      
        g_case_narrative_original_value = case_response.case_narrative?.case_opening_overview || '';
        

        // For offline mode, we use the cached data directly
        g_data = case_response;
        
        // Check if there are any offline changes for this document
        try {
            const offlineChanges = localStorage.getItem('mmria_offline_changes');
            if (offlineChanges) {
                const changesMap = new Map(JSON.parse(offlineChanges));
                const documentChange = changesMap.get(p_id);
                
                if (documentChange && documentChange.modifiedDocument) {
                    // Use the modified document instead of the original cached version
                    g_data = documentChange.modifiedDocument;
                    offlineLog.log('OfflineCaseManager', 'Applied offline changes to document:', p_id);
                }
            }
        } catch (error) {
            offlineLog.warn('OfflineCaseManager', 'Error loading offline changes for document:', p_id, error);
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
      offlineLog.error('OfflineCaseManager', 'Case not found in offline cache:', p_id, 'Status:', response.status);
      
      // Check if this is an encryption key error (401 from service worker)
      if (response.status === 401) {
        try {
          const errorData = await response.json();
          if (errorData.error === 'offline_key_required') {
            offlineLog.error('OfflineCaseManager', 'Offline encryption key required - redirecting to offline login');
            //alert('Your offline session has expired. Please log in again with your offline access key.');
            window.location.href = '/Account/Offlinelogin';
            return;
          }
        } catch (err) {
          offlineLog.warn('OfflineCaseManager', 'Could not parse 401 error response:', err);
        }
      }
      
      throw new Error(`Case ${p_id} not found in offline cache or could not be decrypted (status: ${response.status})`);
    }
  }
  catch(e)
  {
    offlineLog.error('OfflineCaseManager', 'Error loading offline case:', e);
    throw e; // Re-throw the error so the caller can handle it
  }
}

async function get_case_for_processing(p_id) 
{
  if (window.OfflineIntegrityValidator) {
    await window.OfflineIntegrityValidator.validateCurrentState({
      checkPoint: 'case_detail_load',
      expectedOfflineIds: [p_id]
    });
  }

  try
  {
    // Use fetch to get case data - service worker will intercept and handle decryption
    const cache_url = `/api/case?case_id=${p_id}`;
    
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
      offlineLog.log('OfflineCaseManager', 'Retrieved offline case data (decrypted by service worker):', p_id);
      return case_response;     
    } 
    else 
    {
      offlineLog.error('OfflineCaseManager', 'Case not found in offline cache:', p_id, 'Status:', response.status);
      throw new Error(`Case ${p_id} not found in offline cache or could not be decrypted (status: ${response.status})`);
    }
  }
  catch(e)
  {
    offlineLog.error('OfflineCaseManager', 'Error loading offline case:', e);
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
    offlineLog.log('OfflineCaseManager', 'Offline mode detected - tracking document changes instead of saving to server');
    offlineLog.info('OfflineCaseManager', 'Processing offline save request', {
        caseId: p_data && p_data._id,
        note: p_note || '',
        changeCount: save_case_request && save_case_request.Change_Stack && Array.isArray(save_case_request.Change_Stack.items)
            ? save_case_request.Change_Stack.items.length
            : 0
    });

    if (window.OfflineIntegrityValidator) {
        await window.OfflineIntegrityValidator.validateCurrentState({
            checkPoint: 'case_save',
            expectedOfflineIds: p_data && p_data._id ? [p_data._id] : []
        });
    }
    
    let case_response;
    
    try {
        // Create a copy of the complete change stack including all items
        // This must be done AFTER all change stack items are added (including case narrative)
        const changeStackCopy = JSON.parse(JSON.stringify(save_case_request.Change_Stack.items));
        
        // Track the document change for offline sync with field-level changes
        if (typeof track_offline_document_change === 'function') {
            track_offline_document_change(
                p_data._id, 
                p_data, 
                p_note || 'Document modified while offline',
                changeStackCopy  // Pass the complete change stack
            );
        } else {
            offlineLog.warn('OfflineCaseManager', 'track_offline_document_change function not available');
        }
        
        // Update local storage with the modified document
        if (window.OfflineCaseStorage && typeof window.OfflineCaseStorage.setCase === 'function') {
            window.OfflineCaseStorage.setCase(p_data, p_call_back);
        } else if (typeof set_local_case === 'function') {
            set_local_case(p_data, p_call_back);
        } else {
            offlineLog.warn('OfflineCaseManager', 'set_local_case function not available');
        }
        
        // Simulate successful save response for offline mode
        case_response = {
            ok: true,
            rev: p_data._rev, // Keep the same revision for offline
            id: p_data._id,
            offline_save: true
        };
        
        offlineLog.log('OfflineCaseManager', 'Offline save completed:', p_data._id);
        
    } catch (error) {
        offlineLog.error('OfflineCaseManager', 'Error tracking offline document change:', error);
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
        
        // Cache the new case in service worker for offline access
        try {
            const cacheUrl = `/api/case?case_id=${result._id}`;
            const cacheResponse = new Response(JSON.stringify(result), {
                headers: { 'Content-Type': 'application/json' }
            });
            
            // Use the global cache name function (gets version from server endpoint)
            // This ensures consistency with service worker cache naming
            const apiCacheName = await window.getActualApiCacheName();
            
            // Cache the case data
            const cache = await caches.open(apiCacheName);
            await cache.put(cacheUrl, cacheResponse);
            
            // Track as new offline document
            if (typeof track_offline_document_change === 'function') {
                track_offline_document_change(
                    result._id, 
                    result, 
                    'New case created while offline'
                );
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
                }
            }
            
            // Refresh the offline documents list to include the new case
            if (typeof refresh_offline_documents_list === 'function') {
                await refresh_offline_documents_list();
            }
            
            offlineLog.log('OfflineCaseManager', 'New case setup complete for offline:', result._id);
            
        } catch (error) {
            offlineLog.error('OfflineCaseManager', 'Error caching new case for offline:', error);
        }
    }
}

// Expose the offline case manager API to the global scope
window.OfflineCaseManager = {
    addOfflineModeSoftlock: add_offline_mode_softlock,
    removeOfflineModeSoftlock: remove_offline_mode_softlock,
    getDocuments: get_offline_documents,
    getCasesBySession: get_offline_cases_by_session,
    updateOfflineCaseIndexMap: update_offline_case_index_map,
    getCaseFromOfflineSession: get_case_from_offline_session,
    ensureOfflineInitialization: ensure_offline_initialization,
    getOfflineCase: get_offline_case,
    processOfflineSave: process_offline_save,
    generateOfflineRecordId: generateOfflineRecordId,
    handleNewCaseOfflineSetup: handleNewCaseOfflineSetup,
    get_case_for_processing: get_case_for_processing
};

// Make functions globally accessible for backward compatibility
window.update_offline_case_index_map = update_offline_case_index_map;
window.get_case_from_offline_session = get_case_from_offline_session;
window.ensure_offline_initialization = ensure_offline_initialization;
window.get_offline_case = get_offline_case;
window.get_case_for_processing = get_case_for_processing;

