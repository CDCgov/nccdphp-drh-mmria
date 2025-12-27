// Cache version fetching - lazy-loaded from server endpoint
let cachedApiVersionInfo = null;
let apiVersionPromise = null;

// Global flag to track if an offline toggle operation is in progress
let g_offline_operation_in_progress = false;

// Helper function to disable all offline-related buttons
function disable_all_offline_buttons() {
    // Disable all "Add to Offline List" buttons
    const addButtons = document.querySelectorAll('button[id^="offline_toggle_"]');
    addButtons.forEach(button => {
        button.disabled = true;
    });
    
    // Disable all "Remove from List" buttons
    const removeButtons = document.querySelectorAll('button[onclick*="remove_from_offline_list"]');
    removeButtons.forEach(button => {
        button.disabled = true;
    });
    
    // Disable all "Go Offline" buttons
    const goOfflineButtons = document.querySelectorAll('button[onclick*="go_offline_clicked"]');
    goOfflineButtons.forEach(button => {
        button.disabled = true;
    });
}

// Fetch cache version from server endpoint (single source of truth)
async function fetchCacheVersionFromServer() {
    try {
        // Return cached version if available
        if (cachedApiVersionInfo) {
            return cachedApiVersionInfo.cacheVersion;
        }

        // Return existing promise if already fetching
        if (apiVersionPromise) {
            const versionInfo = await apiVersionPromise;
            return versionInfo.cacheVersion;
        }

        // Create fetch promise
        apiVersionPromise = fetch('/api/OfflineCase/cache-version')
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    throw new Error(`Failed to fetch cache version: ${response.status}`);
                }
            })
            .catch(error => {
                console.error('Could not fetch cache version from server:', error);
                // Throw error instead of using hardcoded fallback
                throw error;
            });

        const versionInfo = await apiVersionPromise;
        cachedApiVersionInfo = versionInfo;
        return versionInfo.cacheVersion;
    } catch (error) {
        console.error('Error in fetchCacheVersionFromServer:', error);
        // Re-throw error instead of returning hardcoded fallback
        throw error;
    }
}

// Function to get the actual API cache name (handles session-specific caches)
async function getActualApiCacheName() {
    try {
        if (!('caches' in window)) {
            return await fetchCacheVersionFromServer();
        }
        
        // Get the current cache version from server
        const baseVersion = await fetchCacheVersionFromServer();
        const cacheNames = await caches.keys();
        
        // First, check for session-specific cache (offline mode with active session)
        // Session caches follow pattern: baseVersion-session-{sessionId}
        const sessionCacheName = cacheNames.find(name => 
            name.startsWith(baseVersion + '-session-')
        );
        if (sessionCacheName) {
            console.log('Service Worker: Cache version fetched from server:', sessionCacheName);
            return sessionCacheName;
        }
        
        // Otherwise, look for the base API cache (online mode)
        const baseCacheName = cacheNames.find(name => 
            name === baseVersion
        );
        if (baseCacheName) {
            console.log('Found base API cache:', baseCacheName);
            return baseCacheName;
        }
        
        // Fallback to base name from server
        console.warn('No API cache found, using fallback:', baseVersion);
        return baseVersion;
    } catch (error) {
        console.error('Error getting actual cache name:', error);
        // Re-throw error instead of returning hardcoded fallback
        throw error;
    }
}

// Expose function globally for access from other scripts
window.getActualApiCacheName = getActualApiCacheName;
window.fetchCacheVersionFromServer = fetchCacheVersionFromServer;

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
        show_message('Error updating offline status: ' + error.message, 'error');
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
        show_message('Error removing case from offline list: ' + error.message, 'error');
        g_offline_operation_in_progress = false;
    }
}

// Global array to map offline case indices to case IDs (for routing)
let g_offline_case_index_map = [];

// Global variable to track offline document changes
// Initialize from localStorage if available, otherwise create empty Map
let g_offline_changes = (() => {
    try {
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        if (isOfflineMode) {
            const storedChanges = localStorage.getItem('mmria_offline_changes');
            if (storedChanges) {
                console.log('Initializing g_offline_changes from localStorage');
                return new Map(JSON.parse(storedChanges));
            }
        }
    } catch (error) {
        console.error('Error initializing g_offline_changes from localStorage:', error);
    }
    return new Map();
})();

// Global variable to track original documents for comparison
let g_original_offline_documents = new Map();

// Function to refresh the offline documents list
async function refresh_offline_documents_list() {
    try {
        g_ui.offline_case_view_list_by_user = await get_offline_documents();
        //g_current_offline_documents = offlineDocuments; // Store globally
        
        // Build index map for offline case routing
        g_offline_case_index_map = g_ui.offline_case_view_list_by_user.map(doc => doc.id);
        
        // Make the index map globally accessible for navigation
        window.g_offline_case_index_map = g_offline_case_index_map;
        
        // Initialize offline change tracking when documents are loaded
        initialize_offline_change_tracking(g_ui.offline_case_view_list_by_user);
        
        // Check if we're in offline mode
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        
        // Update the offline-only section (only shown when in offline mode)
       //const offlineOnlySection = document.getElementById('offline-only-documents-section');
       //if (offlineOnlySection) {
       //    if (isOfflineMode) {
       //        offlineOnlySection.innerHTML = render_offline_only_documents_table(offlineDocuments);
       //    } else {
       //        offlineOnlySection.innerHTML = ''; // Hide when not in offline mode
       //    }
       //}
        
        // Update the regular offline documents section (only show when not in offline mode)
       // const offlineSection = document.getElementById('offline-documents-section');
       // if (offlineSection) {
       //     if (!isOfflineMode) {
       //         offlineSection.innerHTML = render_offline_documents_table(offlineDocuments);
       //     } else {
       //         offlineSection.innerHTML = ''; // Hide when in offline mode
       //     }
       // }
    } catch (error) {
        console.error('Error refreshing offline documents list:', error);
    }
}

// Make offline change tracking functions globally available
// Use wrapper functions to ensure modules are available at call time
window.track_offline_document_change = function(...args) {
    return window.OfflineChangeTracker?.track?.(...args);
};
window.initialize_offline_change_tracking = function(...args) {
    return window.OfflineChangeTracker?.initialize?.(...args);
};
window.get_all_offline_changes = function(...args) {
    return window.OfflineChangeTracker?.getAll?.(...args);
};
window.clear_offline_changes = function(...args) {
    return window.OfflineChangeTracker?.clear?.(...args);
};
window.fetchAndStoreOriginalDocument = function(...args) {
    return window.OfflineChangeTracker?.fetchAndStoreOriginal?.(...args);
};
window.sync_offline_changes = function(...args) {
    return window.OfflineSyncManager?.sync?.(...args);
};
window.abandon_offline_changes = function(...args) {
    return window.OfflineSyncManager?.abandon?.(...args);
};
window.clear_offline_processing_mode = function(...args) {
    return window.OfflineSyncManager?.clearOfflineMode?.(...args);
};
window.update_cached_case_document = function(...args) {
    return window.OfflineSyncManager?.updateCachedDocument?.(...args);
};
window.offline_mode_abandon_offline_changes = function(...args) {
    return window.OfflineModals?.abandonOfflineChanges?.(...args);
};
window.show_abandon_case_modal = function(...args) {
    return window.OfflineModals?.showAbandonCase?.(...args);
};
window.close_abandon_case_modal = function(...args) {
    return window.OfflineModals?.closeAbandonCase?.(...args);
};
window.confirm_abandon_case = function(...args) {
    return window.OfflineModals?.confirmAbandonCase?.(...args);
};
window.show_revision_mismatch_modal = function(...args) {
    return window.OfflineModals?.showRevisionMismatch?.(...args);
};
window.close_revision_mismatch_modal = function(...args) {
    return window.OfflineModals?.closeRevisionMismatch?.(...args);
};
window.show_case_already_offline_modal = function(...args) {
    return window.OfflineModals?.showCaseAlreadyOffline?.(...args);
};
window.close_case_already_offline_modal = function(...args) {
    return window.OfflineModals?.closeCaseAlreadyOffline?.(...args);
};
window.show_case_already_online_modal = function(...args) {
    return window.OfflineModals?.showCaseAlreadyOnline?.(...args);
};
window.close_case_already_online_modal = function(...args) {
    return window.OfflineModals?.closeCaseAlreadyOnline?.(...args);
};
window.show_go_online_modal = function(...args) {
    return window.OfflineModals?.showGoOnline?.(...args);
};
window.close_go_online_modal = function(...args) {
    return window.OfflineModals?.closeGoOnline?.(...args);
};

// Make network monitoring functions globally available
window.check_network_connectivity = function(...args) {
    return window.OfflineNetworkMonitor?.check?.(...args);
};
window.update_go_online_button_state = function(...args) {
    return window.OfflineNetworkMonitor?.updateGoOnlineButtonState?.(...args);
};
window.handle_network_status_change = function(...args) {
    return window.OfflineNetworkMonitor?.handleStatusChange?.(...args);
};
window.initialize_network_monitoring = function(...args) {
    return window.OfflineNetworkMonitor?.initialize?.(...args);
};

// Make offline transition functions globally available
window.go_offline_clicked = function(...args) {
    return window.OfflineTransitionManager?.goOfflineClicked?.(...args);
};
window.go_online_clicked = function(...args) {
    return window.OfflineTransitionManager?.goOnlineClicked?.(...args);
};

// Make offline utility functions globally available
window.generateSecureOfflineKeySalt = function(...args) {
    return window.OfflineUtils?.generateKeySalt?.(...args);
};
window.deriveOfflineKeyHash = function(...args) {
    return window.OfflineUtils?.deriveKeyHash?.(...args);
};

// Function to fetch offline documents
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
            const response = await fetch(`/api/OfflineCase/active-user-session`, {///${offlineSessionId}
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




function render_offline_processing_item(caseDoc, i) {
    const modifiedDocument = caseDoc.modifiedDocument || caseDoc.ModifiedDocument || {};
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 

    // Try multiple possible property names for sync state
    const syncState = caseDoc.syncState;


    // Access nested properties from the proper mmria_case structure
    const caseID = modifiedDocument._id;
    
    // Find the actual index in the processing list for proper routing
    // Find the actual index in the main case list for proper routing
    //const actualIndex = g_ui.case_view_list ? g_ui.case_view_list.findIndex(c => c.id === caseID) : -1;
    //const caseIndex = actualIndex >= 0 ? actualIndex : i;
    
    const rev = modifiedDocument._rev;    
    const hostState = modifiedDocument.host_state;
    const jurisdictionID = modifiedDocument.home_record?.jurisdiction_id;
    const firstName = modifiedDocument.home_record?.first_name;
    const lastName = modifiedDocument.home_record?.last_name;
    const recordID = modifiedDocument.home_record?.record_id ? `- (${modifiedDocument.home_record.record_id})` : '';
    const agencyCaseID = modifiedDocument.home_record?.agency_case_id;
    const createdBy = modifiedDocument.created_by;
    const lastUpdatedBy = modifiedDocument.last_updated_by;
    const caseStatus = modifiedDocument.home_record?.case_status?.overall_case_status;
    const currentCaseStatus = caseStatus == null ? '(blank)' : caseStatuses[caseStatus.toString()];
    const dateCreated = modifiedDocument.date_created ? new Date(modifiedDocument.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = modifiedDocument.date_last_updated ? new Date(modifiedDocument.date_last_updated).toLocaleDateString('en-US') : '';
    
    let projectedReviewDate = modifiedDocument.home_record?.case_status?.projected_review_date ? new Date(modifiedDocument.home_record.case_status.projected_review_date).toLocaleDateString('en-US') : '';
    let actualReviewDate = modifiedDocument.home_record?.case_status?.committee_review_date ? new Date(modifiedDocument.home_record.case_status.committee_review_date).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;


    const canSync = syncState === 0; // Only allow sync if pending
    const canAbandon = syncState === 0 && rev!=null; // Only allow abandon if pending
    const canDelete = syncState === 0 && rev==null; // Only allow delete if pending


    // Check if this document has offline changes
    let hasChanges = false;
    let changeIndicator = '';
    try {
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            hasChanges = true;
            const changeRecord = g_offline_changes.get(caseID);
            changeIndicator = `
                <div style="margin-top: 4px;">
                    <span class="badge badge-warning" title="Document has offline changes made at ${new Date(changeRecord.timestamp).toLocaleString()}">
                        <i class="fa fa-edit"></i> Modified Offline
                    </span>
                </div>
            `;
        }
    } catch (error) {
        console.warn('Error checking for offline changes:', error);
    }

    return `
        <tr class="tr" path="${caseID}" ${hasChanges ? 'style="background-color: #fff3cd;"' : ''}>
            <td class="td">
                <a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="sync_offline_changes('${caseID}')" style="line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canSync ? 'disabled' : ''}>
                    Upload
                </button>            
                <button type="button" class="btn btn-primary" onclick="delete_offline_changes('${caseID}')" style="margin-top:2px;line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canDelete ? 'disabled' : ''}>
                    Delete
                </button>                
                <button type="button" class="btn btn-primary" onclick="abandon_offline_changes('${caseID}')" style="margin-top:2px; line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canAbandon ? 'disabled' : ''}>
                    Abandon</br> Changes
                </button>            
                
            </td>
        </tr>
    `;
}
// Function to render individual offline document item
function render_offline_only_document_item(item, i) {
    const caseStatuses = {
        9999:"(blank)",	
        1:"Abstracting (Incomplete)",
        2:"Abstraction Complete",
        3:"Ready for Review",
        4:"Review Complete and Decision Entered",
        5:"Out of Scope and Death Certificate Entered",
        6:"False Positive and Death Certificate Entered",
        0:"Vitals Import"
    }; 

    const caseID = item.id;
    const rev = item.rev;
    
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[parseInt((item.value.case_status.overall_case_status != null ? item.value.case_status.overall_case_status : item.value.case_status).toString())];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : '';
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : '';
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    // Check if this document has offline changes
    let hasChanges = false;
    let changeIndicator = '';
    const isNew = rev == null;
    let isNewIndicator = '';
    if (isNew) {
        isNewIndicator = `
            <div style="margin-top: 4px;">
                <span class="badge badge-success" title="This is a new offline document that has not been uploaded yet">
                    <i class="fa fa-plus"></i> New Offline Document
                </span>
            </div>
        `;
    }
    try {
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            hasChanges = true;
            const changeRecord = g_offline_changes.get(caseID);
            changeIndicator = `
                <div style="margin-top: 4px;">
                    <span class="badge badge-warning" title="Document has offline changes made at ${new Date(changeRecord.timestamp).toLocaleString()}">
                        <i class="fa fa-edit"></i> Modified Offline
                    </span>
                </div>
            `;
        }
    } catch (error) {
        console.warn('Error checking for offline changes:', error);
    }

    return `
        <tr class="tr" path="${caseID}" ${hasChanges ? 'style="background-color: #fff3cd;"' : ''}>
            <td class="td">
                <a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator} ${isNewIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="offline_mode_abandon_offline_changes('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;">
                    Abandon Changes
                </button>
            </td>
        </tr>
    `;
}
// Function to render individual offline document item
function render_offline_document_item(item, i) {
    const caseStatuses = {
        9999:"(blank)",	
        1:"Abstracting (Incomplete)",
        2:"Abstraction Complete",
        3:"Ready for Review",
        4:"Review Complete and Decision Entered",
        5:"Out of Scope and Death Certificate Entered",
        6:"False Positive and Death Certificate Entered",
        0:"Vitals Import"
    }; 

    const caseID = item.id;
    
    // Find the actual index in the main case list for proper routing
    const actualIndex = g_ui.case_view_list ? g_ui.case_view_list.findIndex(c => c.id === caseID) : -1;
    const caseIndex = actualIndex >= 0 ? actualIndex : i;
    
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[parseInt(item.value.case_status)];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : '';
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : '';
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    // Check if this document has offline changes
    let hasChanges = false;
    let changeIndicator = '';
    try {
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            hasChanges = true;
            const changeRecord = g_offline_changes.get(caseID);
            changeIndicator = `
                <div style="margin-top: 4px;">
                    <span class="badge badge-warning" title="Document has offline changes made at ${new Date(changeRecord.timestamp).toLocaleString()}">
                        <i class="fa fa-edit"></i> Modified Offline
                    </span>
                </div>
            `;
        }
    } catch (error) {
        console.warn('Error checking for offline changes:', error);
    }

    return `
        <tr class="tr" path="${caseID}" ${hasChanges ? 'style="background-color: #fff3cd;"' : ''}>
            <td class="td">
                <a href="#/${caseIndex}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="remove_from_offline_list('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px; ${g_offline_operation_in_progress ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${g_offline_operation_in_progress ? 'disabled' : ''}>
                    Remove</br> From List
                </button>
            </td>
        </tr>
    `;
}

// Function to hide case listing elements when going offline
function hideOnlineCaseListingElements() {
    console.log('Hiding case listing elements for offline mode');
    
    // Hide the case listing table specifically (by looking for "Case Listing" header)
    const allTables = document.querySelectorAll('table.table.mb-0');
    allTables.forEach(table => {
        const headers = table.querySelectorAll('th');
        let isCaseListingTable = false;
        headers.forEach(header => {
            if (header.textContent.includes('Case Listing')) {
                isCaseListingTable = true;
            }
        });
        
        if (isCaseListingTable) {
            table.style.display = 'none';
            console.log('Case listing table hidden');
        }
    });
    
    // Hide pagination elements
    const paginationElements = document.querySelectorAll('.table-pagination');
    paginationElements.forEach(element => {
        element.style.display = 'none';
        console.log('Pagination element hidden');
    });
    
    // Hide the search/filter form elements
    console.log('Looking for search/filter elements to hide...');
    
    // Hide individual search/filter elements by their IDs
    const searchElements = [
        'search_text_box',
        'search_field_selection', 
        'search_case_status',
        'search_pregnancy_relatedness',
        'search_sort_by',
        'search_records_per_page',
        'sort_descending'
    ];
    
    searchElements.forEach(elementId => {
        const element = document.getElementById(elementId);
        if (element) {
            // Hide the parent container (form-inline div)
            const parentDiv = element.closest('.form-inline');
            if (parentDiv) {
                parentDiv.style.display = 'none';
                console.log(`Search element container hidden: ${elementId}`);
            } else {
                element.style.display = 'none';
                console.log(`Search element hidden: ${elementId}`);
            }
        }
    });
    
    // Hide the Apply Filters and Reset buttons
    const applyFilterButton = document.querySelector('button[onclick*="apply_filter_click"]');
    if (applyFilterButton) {
        const buttonContainer = applyFilterButton.closest('.form-inline');
        if (buttonContainer) {
            buttonContainer.style.display = 'none';
            console.log('Apply Filters button container hidden');
        }
    }
    
    // Hide any remaining form elements that might be missed
    const searchForm = document.querySelector('form[onsubmit*="get_case_set"]');
    if (searchForm) {
        searchForm.style.display = 'none';
        console.log('Search form hidden');
    }
    
    // Alternative approach - hide by class or parent elements if the direct selectors don't work
    const searchContainer = document.querySelector('.search-container, .case-search-form, [id*="search"], [class*="search"]');
    if (searchContainer) {
        searchContainer.style.display = 'none';
        console.log('Search container hidden');
    }
}

// Function to show case listing elements when going online
function showOnlineCaseListingElements() {
    console.log('Showing case listing elements for online mode');
    
    // Show the case listing table specifically (by looking for "Case Listing" header)
    const allTables = document.querySelectorAll('table.table.mb-0');
    allTables.forEach(table => {
        const headers = table.querySelectorAll('th');
        let isCaseListingTable = false;
        headers.forEach(header => {
            if (header.textContent.includes('Case Listing')) {
                isCaseListingTable = true;
            }
        });
        
        if (isCaseListingTable) {
            table.style.display = '';
            console.log('Case listing table shown');
        }
    });
    
    // Show pagination elements
    const paginationElements = document.querySelectorAll('.table-pagination');
    paginationElements.forEach(element => {
        element.style.display = '';
        console.log('Pagination element shown');
    });
    
    // Show the search/filter form elements
    console.log('Looking for search/filter elements to show...');
    
    // Show individual search/filter elements by their IDs
    const searchElements = [
        'search_text_box',
        'search_field_selection', 
        'search_case_status',
        'search_pregnancy_relatedness',
        'search_sort_by',
        'search_records_per_page',
        'sort_descending'
    ];
    
    searchElements.forEach(elementId => {
        const element = document.getElementById(elementId);
        if (element) {
            // Show the parent container (form-inline div)
            const parentDiv = element.closest('.form-inline');
            if (parentDiv) {
                parentDiv.style.display = '';
                console.log(`Search element container shown: ${elementId}`);
            } else {
                element.style.display = '';
                console.log(`Search element shown: ${elementId}`);
            }
        }
    });
    
    // Show the Apply Filters and Reset buttons
    const applyFilterButton = document.querySelector('button[onclick*="apply_filter_click"]');
    if (applyFilterButton) {
        const buttonContainer = applyFilterButton.closest('.form-inline');
        if (buttonContainer) {
            buttonContainer.style.display = '';
            console.log('Apply Filters button container shown');
        }
    }
    
    // Show any remaining form elements that might be missed
    const searchForm = document.querySelector('form[onsubmit*="get_case_set"]');
    if (searchForm) {
        searchForm.style.display = '';
        console.log('Search form shown');
    }
    
    // Show search container
    const searchContainer = document.querySelector('.search-container, .case-search-form, [id*="search"], [class*="search"]');
    if (searchContainer) {
        searchContainer.style.display = '';
        console.log('Search container shown');
    }
}

function app_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx) 
{
    const isProcessingOfflineCases = localStorage.getItem('process_offline_cases') || 'false';
    const isOfflineMode = localStorage.getItem('is_offline') || 'false';
    const isAbandonOfflineChangesInProgress = localStorage.getItem('abandon_offline_session') || 'false';


    if(isAbandonOfflineChangesInProgress ==='true'){
            p_result.push(`
            <div class="alert alert-warning" style="border-top: 1px;" role="alert">
               <img src="./img/offline-warning.svg" alt="Go Online Alert"> You have an active offline session. Proceeding will abandon this session and prevent any changes from being synced. Are you sure you want to continue?
                     <button type="button" class="btn btn-primary btn-sm" onclick="abandon_offline_session()" title="Clear offline processing mode and return to normal case listing">
                                Abandon Offline Session
                            </button>
            </div>`)
            
            return;
    }

    const offlineSession = localStorage.getItem('mmria_offline_session');
    let sessionData;
    let offlineSessionId;
    try {
        sessionData = JSON.parse(offlineSession);
        // Try both possible field names for session ID
        offlineSessionId = sessionData.sessionId || sessionData.offlineSessionId;        
    } catch (error) {        
    }

    if (window.location.hash == '')
      window.location.hash = "#/summary";
    g_pinned_case_count = 0;
    
    p_result.push("<section id='app_summary'>");

    /* The Intro */
    p_result.push("<div>");
    p_result.push("<h1 class='content-intro-title h2' tabindex='-1'>");
    g_is_data_analyst_mode ? p_result.push("Analyst ") : p_result.push("Abstractor ");
    p_result.push("Line Listing Summary</h1>");
    p_result.push("<div class='row no-gutters align-items-center'>");
    
    let is_read_only_html = '';
    
    if(g_is_data_analyst_mode)
    {
        is_read_only_html = "disabled='disabled'";
        // is_read_only_html = "disabled='disabled'";
    }

    if(isOfflineMode === 'true' || isProcessingOfflineCases === 'true'){ 
        const newCaseCount =  g_ui.offline_mode_case_view_list ? g_ui.offline_mode_case_view_list.filter(doc => doc.rev == null).length : 0;
        const newCaseButtonDisabled = (newCaseCount >= offline_mode_max_new_cases) ? true : false;
        if(newCaseButtonDisabled){
            p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' disabled='disabled' ${is_read_only_html}>Add New Case</button>`);
        }
        else if (isProcessingOfflineCases !== 'true') {
            p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);
        }

    }   
    else{
        p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);
    }
     
    p_result.push("<span class='spinner-container spinner-inline ml-2'><span class='spinner-body text-primary'><span class='spinner'></span></span>");
    p_result.push("</div>");
    p_result.push("</div> <!-- end .content-intro -->");
    

    // Check if we're in offline mode - if so, skip case listing and filters
    const isOfflineStatus = localStorage.getItem('is_offline') || 'false';
    
    if (isOfflineStatus !== 'true' && isProcessingOfflineCases !== 'true') {
        p_result.push(`<hr class="border-top mt-4 mb-4" />`);

        p_result.push("<div class='mb-4'>");
        /* Custom Search */
        p_result.push("<div class='form-inline mb-2'>");
        p_result.push("<label for='search_text_box' class='mr-2'> Search for:</label>");
        p_result.push("<input type='text' class='form-control mr-2' id='search_text_box' onchange='g_ui.case_view_request.search_key=this.value;' value='");
        if (g_ui.case_view_request.search_key != null) 
        {
            p_result.push(p_ui.case_view_request.search_key.replace(/'/g, "&quot;"));
        }
        p_result.push("' />");

        p_post_html_render.push("$('#search_text_box').bind(\"enterKey\",function(e){");
        p_post_html_render.push("	get_case_set();");
        p_post_html_render.push(" });");
        p_post_html_render.push("$('#search_text_box').keyup(function(e){");
        p_post_html_render.push("	if(e.keyCode == 13)");
        p_post_html_render.push("	{");
        p_post_html_render.push("	$(this).trigger(\"enterKey\");");
        p_post_html_render.push("	}");
        p_post_html_render.push("});");

        p_result.push(
            `<div class="form-inline mb-2">
                <label for="search_field_selection" class="mr-2">Search in:</label>
                <select id="search_field_selection" name="search_field_selection" class="custom-select" onchange="search_field_selection_onchange(this.value)">
                    ${render_field_selection(p_ui.case_view_request)}
                </select>
            </div>`
        );

        
        p_result.push("</div>");

        /* Case Status */
        p_result.push(
            `<div class="form-inline mb-2">
                <label for="search_case_status" class="mr-2">Case Status:</label>
                <select id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                    ${renderSortCaseStatus(p_ui.case_view_request)}
                </select>
            </div>`
        );
        p_result.push(
            `<div class="form-inline mb-2">
                <label for="search_pregnancy_relatedness" class="mr-2">Pregnancy Relatedness:</label>
                <select id="search_pregnancy_relatedness" class="custom-select" onchange="search_pregnancy_relatedness_onchange(this.value)">
                    ${renderPregnancyRelatedness(p_ui.case_view_request)}
                </select>
            </div>`
        );
        /* Sort By: */
        p_result.push(
            `<div class="form-inline mb-2">
                <label for="search_sort_by" class="mr-2">Sort:</label>
                <select id="search_sort_by" class="custom-select" onchange="g_ui.case_view_request.sort = this.options[this.selectedIndex].value;">
                    ${render_sort_by_include_in_export(p_ui.case_view_request)}
                </select>
            </div>`
        );

        /* Records per page */
        p_result.push(
            `<div class="form-inline mb-2">
                <label for="search_records_per_page" class="mr-2">Records per page:</label>
                <select id="search_records_per_page" class="custom-select" onchange="records_per_page_change(this.value);">
                    ${render_filter_records_per_page(p_ui.case_view_request)}
                </select>
            </div>`
        );

        /* Descending Order */
        p_result.push(
            `<div class="form-inline mb-3">
                <label for="sort_descending" class="mr-2">Descending order:</label>
                <input id="sort_descending" name="sort_descending" type="checkbox" onchange="g_ui.case_view_request.descending = this.checked" ${p_ui.case_view_request.descending && 'checked' || ''} />
            </div>`
        );

        /* Apply Filters Btn */
        p_result.push(
            `<div class="form-inline">
                <button type="button" class="btn btn-secondary mr-2" alt="Apply filters" onclick="init_inline_loader(async function(){ await apply_filter_click() })">Apply Filters</button>
                <button type="button" class="btn btn-secondary" alt="Reset filters" id="search_command_button" onclick="init_inline_loader(function(){ clear_case_search() })">Reset</button>
                <span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
            </div>`
        );

        p_result.push("</div> <!-- end .content-intro -->");
    }

    // Add offline-only documents section (only shown when in offline mode)
    //p_result.push("<div id='offline-only-documents-section' class='mb-4'>");
    //p_result.push("</div>");

    // Add offline documents section
    //p_result.push("<div id='offline-documents-section' class='mb-4'>");
    //p_result.push("</div>");

    // Add offline processing section
    p_result.push("<div id='offline-processing-section' class='mb-4'>");
    p_result.push("</div>");



    if (g_ui.process_offline_case_view_list_by_user && g_ui.process_offline_case_view_list_by_user.length > 0) {
        p_result.push("<table class='table mb-0'>");
        p_result.push("<thead class='thead'>");
        p_result.push("<tr class='tr bg-tertiary'>");
        p_result.push("<th class='th h4' colspan='7' scope='colgroup'>Offline Processing Cases</th>");
        p_result.push("</tr>");
        p_result.push("<tr class='tr'>");
        p_result.push("<th class='th' scope='col'>Case Information</th>");
        p_result.push("<th class='th' scope='col'>Case Status</th>");
        p_result.push("<th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>");
        p_result.push("<th class='th' scope='col'>Created</th>");
        p_result.push("<th class='th' scope='col'>Last Updated</th>");
        p_result.push("<th class='th' scope='col'>Currently Edited By</th>");
        p_result.push("<th class='th' scope='col' style=\"width: 115px;\">Actions</th>");
        p_result.push("</tr>");
        p_result.push("</thead>");
        p_result.push("<tbody class='tbody'>");
        g_ui.process_offline_case_view_list_by_user.forEach((item, i) => {
            p_result.push(render_offline_processing_item(item, i));
        });
        p_result.push("</tbody>");
        p_result.push("</table>");
    }

    //code to render offline processing table g_ui.process_offline_case_view_list_by_user 
    //offline case processing
    if (isProcessingOfflineCases === 'true') {

        if(!g_ui.process_offline_case_view_list_by_user || !g_ui.process_offline_case_view_list_by_user.case_documents)return "";

        const allDocumentsSynced = g_ui.process_offline_case_view_list_by_user.case_documents.every(doc => doc.syncState !== 0);
        p_result.push(`
            <div class="alert alert-success" style="border-top: 1px;" role="alert">
               <img src="./img/go-online-alert.svg" alt="Go Online Alert"> Return to online mode successful. Please upload all offline cases to save changes and access other online cases.
            </div>
            <table class="table mb-0">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='5' scope='colgroup'>Offline Case List</th>
                        <th class='th h4' colspan='2' scope='colgroup'>
                            <button type="button" class="btn btn-primary btn-sm" onclick="clear_offline_processing_mode()" title="Clear offline processing mode and return to normal case listing" ${!allDocumentsSynced ? 'disabled' : ''}>
                                Exit Processing Mode
                            </button>
                        </th>
                    </tr>
                    <tr class='tr'>
                        <th class='th' scope='col'>Case Information</th>
                        <th class='th' scope='col'>Case Status</th>
                        <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                        <th class='th' scope='col'>Created</th>
                        <th class='th' scope='col'>Last Updated</th>
                        <th class='th' scope='col'>Currently Edited By</th>
                        <th class='th' scope='col' style="width: 115px;">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">
                    ${g_ui.process_offline_case_view_list_by_user.case_documents.map((item, i) => render_offline_processing_item(item, i)).join('')}
                </tbody>
            <tfoot class='tfoot'>
                ${g_ui.offline_ids_not_changed.length > 0 ? `<tr class='tr'>
                    <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                        <p style='margin: 0; font-weight:bold;font-size: 13px; color: #6c757d; font-style: italic;'>
                             One or more cases where taken offline do not contain any changes. These will be automatically unlocked.
                        </p>                        
                    </td>
                </tr>` : ''}
                </tr>
                <tr class='tr'>
                    <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                        <p style='margin: 0; font-size: 13px; color: #6c757d; font-style: italic;'>${localStorage.getItem("offline_session_id")}</p>
                    </td>
                </tr>                
            </tfoot>        
            </table>
            </br>
            Online case listing will not be available until outstanding offline cases are brought back online.
            </br>Please resolve any cases from the Offline Cases list.
        `);
    }

    // code to render offline-only documents table (only shown when in offline mode)
    if(is_offline_mode_enabled && isOfflineMode === 'true'){
        if(!g_ui.offline_mode_case_view_list || g_ui.offline_mode_case_view_list == undefined)return"error";

        const newCaseCount =  g_ui.offline_mode_case_view_list ? g_ui.offline_mode_case_view_list.filter(doc => doc.rev == null).length : 0;
        const newCaseButtonDisabled = newCaseCount >= offline_mode_max_new_cases ? true : false;

        g_current_offline_documents = g_ui.offline_mode_case_view_list;
        // Build index map for offline case routing");
        g_offline_case_index_map = g_ui.offline_mode_case_view_list.map(doc => doc.id);
        // Make the index map globally accessible for navigation");
        window.g_offline_case_index_map = g_offline_case_index_map;
        console.log('Offline case index map:', window.g_offline_case_index_map);

        if (!window.g_offline_tracking_initialized) {
            // g_offline_changes is already loaded from localStorage during initialization
            // Just initialize the original documents tracking
            if (typeof window.OfflineChangeTracker !== 'undefined' && window.OfflineChangeTracker.initialize) {
                window.OfflineChangeTracker.initialize(g_ui.offline_mode_case_view_list);
                window.g_offline_tracking_initialized = true;
            } else if (typeof initialize_offline_change_tracking === 'function') {
                initialize_offline_change_tracking(g_ui.offline_mode_case_view_list);
                window.g_offline_tracking_initialized = true;
            } else {
                console.warn('OfflineChangeTracker not available, skipping initialization');
            }
            console.log('Offline change tracking initialized. g_offline_changes size:', g_offline_changes.size);
        } 

        // Initialize network monitoring for Go Online button");
        if (typeof initialize_network_monitoring === 'function') {
            initialize_network_monitoring();
        }      
        p_result.push(`
            <table class="table mb-0">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='7' scope='colgroup'>Offline Case List</th>
                    </tr>
                    <tr class='tr'>
                        <th class='th' scope='col'>Case Information</th>
                        <th class='th' scope='col'>Case Status</th>
                        <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                        <th class='th' scope='col'>Created</th>
                        <th class='th' scope='col'>Last Updated</th>
                        <th class='th' scope='col'>Currently Edited By</th>
                        <th class='th' scope='col' style="width: 115px;">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">
                    ${g_ui.offline_mode_case_view_list.length == 0 ?"<tr class='tr'><td class='td' colspan='7'><i>No cases to display</i></td></tr>":g_ui.offline_mode_case_view_list.map((item, i) => render_offline_only_document_item(item, i)).join('')}
                </tbody>
                <tfoot class='tfoot'> 
                    <tr class='tr'>
                        <td class='td' colspan='6' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                            <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic;'>
                                <li style='margin-bottom: 4px;font-weight: ${newCaseButtonDisabled ? 'bold' :'normal'};'>Up to 3 new cases can be created offline.</li>
                                <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data. Please bring all cases back online regularly to ensure your data is saved to the system.</li>                                
                            </ul>
                        </td>                    
                        <td class='td' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>
                            ${isOfflineStatus === 'true' ? `
                                <button type="button" id="go-online-btn" class="btn btn-primary" onclick="show_go_online_modal(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                                    <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Online
                                </button>
                            ` : `
                                <button type="button" class="btn btn-primary" onclick="go_offline_clicked(event)" style="line-height: 1.15; ${g_offline_operation_in_progress ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${g_offline_operation_in_progress ? 'disabled' : ''}>
                                    <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Offline
                                </button>
                            `}
                        </td>
                    </tr>
                    <tr class='tr'>
                        <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                            <p style='margin: 0; font-size: 13px; color: #6c757d; font-style: italic;'>${offlineSessionId}</p>
                        </td>
                    </tr>
                </tfoot>
            </table>
        `);
    }

    if(is_offline_mode_enabled && isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true'){
        const currentOfflineCount = g_ui.offline_case_view_list_by_user ? g_ui.offline_case_view_list_by_user.length : 0;
        const offline_button_disabled = currentOfflineCount >= offline_mode_max_existing_cases ? true: false;
        const hasOfflineCases = g_ui.offline_case_view_list_by_user && g_ui.offline_case_view_list_by_user.length > 0;

        if(!g_ui.offline_case_view_list_by_user)return "";
        p_result.push(`
            <table class="table mb-3">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='7' scope='colgroup'>Cases Selected for Offline Work</th>
                    </tr>
                    <tr class='tr'>
                        <th class='th' scope='col'>Case Information</th>
                        <th class='th' scope='col'>Case Status</th>
                        <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                        <th class='th' scope='col'>Created</th>
                        <th class='th' scope='col'>Last Updated</th>
                        <th class='th' scope='col'>Currently Edited By</th>
                        <th class='th' scope='col' style="width: 115px;">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">
                ${g_ui.offline_case_view_list_by_user.length == 0 ?"<tr class='tr'><td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>Select cases for offline work from the Case Listing table below</td></tr>":g_ui.offline_case_view_list_by_user.map((item, i) => render_offline_document_item(item, i)).join('')}
                    
                </tbody>
                <tfoot class='tfoot'>
                    <tr class='tr'>
                        <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                            <div style='display: flex; justify-content: space-between; align-items: flex-start; gap: 20px;'>                        
                                <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic; flex: 1;'>
                                    <li style='margin-bottom: 4px;font-weight:${offline_button_disabled ? "bold" : "normal"}'>Up to 3 existing cases can be brought offline at once.</li>
                                    <li style='margin-bottom: 4px;'>Up to 3 new cases can be created offline.</li>
                                    <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data.</li>
                                    <li style='margin-bottom: 4px;'>Please bring all cases back online regularly to ensure your data is saved to the system - for security reasons, cases that are offline for more than 30 days will be automatically deleted.</li>
                                    <li style='margin-bottom: 4px;'>Navigating to another page will reset the list of cases selected for offline work.</li>
                                    
                                </ul>
                                <div style='flex-shrink: 0; display: flex; align-items: flex-start;'>
                                ${isOfflineStatus === 'true' ? `
                                    <button type="button" id="go-online-btn" class="btn btn-primary" onclick="go_online_clicked(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                                        <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Online
                                    </button>
                                ` : `
                                    <button type="button" class="btn btn-primary" onclick="go_offline_clicked(event)" style="line-height: 1.15; ${g_offline_operation_in_progress ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${g_offline_operation_in_progress ? 'disabled' : ''}>
                                        <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Offline
                                    </button>
                                `}     
                                </div>                      
                            </div>                      
                        </td>                    
                    </tr>
                </tfoot>            
            </table>
        `);
     }

    // Only show case listing table and pagination if not in offline mode and not processing offline cases
    
    
    
    if (isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true') {
        let pagination_current_page = p_ui.case_view_request.page;
        const pagination_number_of_pages = Math.ceil(p_ui.case_view_request.total_rows / p_ui.case_view_request.take);
        if(pagination_number_of_pages == 0)
        {
            pagination_current_page = 0;
        }

        p_result.push("<div class='table-pagination row align-items-center no-gutters'>");
            p_result.push("<div class='col'>");
                p_result.push("<div class='row no-gutters'>");
                    p_result.push("<p class='mb-0'>Total Records: ");
                        p_result.push("<strong>" + p_ui.case_view_request.total_rows + "</strong>");
                    p_result.push("</p>");
                    p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
                    p_result.push("<p class='mb-0'>Viewing Page(s): ");
                        p_result.push("<strong>" + pagination_current_page + "</strong> ");
                        p_result.push("of ");
                        p_result.push("<strong>" + pagination_number_of_pages + "</strong>");
                    p_result.push("</p>");
                p_result.push("</div>");
            p_result.push("</div>");
            p_result.push("<div class='col row no-gutters align-items-center justify-content-end'>");
                p_result.push("<p class='mb-0'>Select by page:</p>");
                for(var current_page = 1; (current_page - 1) * p_ui.case_view_request.take < p_ui.case_view_request.total_rows; current_page++)
                {
                    p_result.push("<button type='button' class='table-btn-link btn btn-link' alt='select page " + current_page + "' onclick='g_ui.case_view_request.page=");
                        p_result.push(current_page);
                        p_result.push(";get_case_set();'>");
                        p_result.push(current_page);
                    p_result.push("</button>");
                }
            p_result.push("</div>");
        p_result.push("</div>");
        
        // Ensure case_view_list is defined and is an array
        if (!p_ui.case_view_list || !Array.isArray(p_ui.case_view_list)) {
            p_ui.case_view_list = [];
        }
        
        p_result.push(`
            <table class="table mb-0">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='7' scope='colgroup'>Case Listing</th>
                    </tr>
                    <tr class='tr'>
                        <th class='th' scope='col'>Case Information</th>
                        <th class='th' scope='col'>Case Status</th>
                        <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                        <th class='th' scope='col'>Created</th>
                        <th class='th' scope='col'>Last Updated</th>
                        <th class='th' scope='col'>Currently Edited By</th>
                        ${!g_is_data_analyst_mode ? `<th class='th' scope='col' style="width: 115px;">Actions</th>` : ''}
                    </tr>
                </thead>
                <tbody class="tbody">
                    
                    ${ !g_is_data_analyst_mode ? p_ui.case_view_list.map((item, i) => render_app_pinned_summary_result(item, i)).join('') : ""}

                    ${p_ui.case_view_list.map((item, i) => render_app_summary_result_item(item, i)).join('')}
                </tbody>
            </table>
        `);

        p_result.push("<div class='table-pagination row align-items-center no-gutters'>");
            p_result.push("<div class='col'>");
                p_result.push("<div class='row no-gutters'>");
                    p_result.push("<p class='mb-0'>Total Records: ");
                        p_result.push("<strong>" + p_ui.case_view_request.total_rows + "</strong>");
                    p_result.push("</p>");
                    p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
                    p_result.push("<p class='mb-0'>Viewing Page(s): ");
                        p_result.push("<strong>" + pagination_current_page + "</strong> ");
                        p_result.push("of ");
                        p_result.push("<strong>" + pagination_number_of_pages + "</strong>");
                    p_result.push("</p>");
                p_result.push("</div>");
            p_result.push("</div>");
            p_result.push("<div class='col row no-gutters align-items-center justify-content-end'>");
                p_result.push("<p class='mb-0'>Select by page:</p>");
                for(var current_page = 1; (current_page - 1) * p_ui.case_view_request.take < p_ui.case_view_request.total_rows; current_page++) 
                {
                    p_result.push("<button type='button' class='table-btn-link btn btn-link' alt='select page " + current_page + "' onclick='g_ui.case_view_request.page=");
                        p_result.push(current_page);
                        p_result.push(";get_case_set();'>");
                        p_result.push(current_page);
                    p_result.push("</button>");
                }
            p_result.push("</div>");
        p_result.push("</div>");
    }    p_result.push("</section>");

    if (p_ui.url_state.path_array.length > 1) 
    {
        if(p_ui.url_state.path_array[1] == "field_search")
        {
            var search_text = p_ui.url_state.path_array[2].replace(/%20/g, " ");
            p_result.push("<section id='field_search_id'>");
            let is_case_read_only = false;
            let is_checked_out = is_case_checked_out(g_data);
            let case_is_locked = is_case_locked(g_data);


            if(case_is_locked || g_is_data_analyst_mode)
            {
                is_case_read_only = true;
            }
            else if(!is_checked_out)
            {
                is_case_read_only = true;
            }

            quick_edit_header_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, { search_text: search_text, is_read_only: is_case_read_only });
            
            var search_text_context = get_seach_text_context(p_result, [], p_metadata, p_data, p_dictionary_path, p_metadata_path, p_object_path, search_text, is_case_read_only);

            render_search_text(search_text_context);

            Array.prototype.push.apply(p_post_html_render, search_text_context.post_html_render);
            
            p_result.push("</section>");
        }
        else
        {
            for (var i = 0; i < p_metadata.children.length; i++) 
            {
                var child = p_metadata.children[i];

                if (child.type.toLowerCase() == 'form' && p_ui.url_state.path_array[1] == child.name) 
                {
                    if (p_data[child.name] || p_data[child.name] == 0) 
                    {
                        // do nothing 
                    }
                    else 
                    {
                        p_data[child.name] = create_default_object(child, {})[child.name];
                    }

                    const page_render_array = page_render(child, p_data[child.name], p_ui, p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name, false, p_post_html_render);
                    for(let j = 0; j < page_render_array.length; j++)
                    {
                        p_result.push(page_render_array[j]);
                    }
                }
            }

        }
    }
}

async function unpin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_show(p_id, false);
    }
    else
    {
        await mmria_pin_case_click(p_id, true)
    }
}

// Helper function to show messages (if not already available)
function show_message(message, type) {
    if (!type) type = 'info';
    
    // Create a simple toast notification
    var toast = document.createElement('div');
    var alertClass = 'alert-info';
    if (type === 'error') alertClass = 'alert-danger';
    else if (type === 'success') alertClass = 'alert-success';
    else if (type === 'warning') alertClass = 'alert-warning';
    
    toast.className = 'alert ' + alertClass + ' alert-dismissible fade show';
    toast.style.position = 'fixed';
    toast.style.top = '20px';
    toast.style.right = '20px';
    toast.style.zIndex = '9999';
    toast.style.minWidth = '300px';
    toast.innerHTML = message + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';
    
    document.body.appendChild(toast);
    
    // Auto-remove after 5 seconds
    setTimeout(function() {
        if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
        }
    }, 5000);
}

function render_sort_by_include_in_export(p_sort)
{
	const sort_list = [
        {
            value : 'by_date_created',
            display : 'By date created'
        },
        {
            value : 'by_date_last_updated',
            display : 'By date last updated'
        },
        {
            value : 'by_last_name',
            display : 'By last name'
        },
        {
            value : 'by_first_name',
            display : 'By first name'
        },
        {
            value : 'by_middle_name',
            display : 'By middle name'
        },
        {
            value : 'by_year_of_death',
            display : 'By year of death'
        },
        {
            value : 'by_month_of_death',
            display : 'By month of death'
        },
        {
            value : 'by_committee_review_date',
            display : 'By committee review date'
        },
        {
            value : 'by_created_by',
            display : 'By created by'
        },
        {
            value : 'by_last_updated_by',
            display : 'By last updated by'
        },
        {
            value : 'by_state_of_death',
            display : 'By state of death'
        },
        {
            value : 'by_agency_case_id',
            display : 'By agency-based case identifier'
        },
        {
            value : 'by_record_id',
            display : 'By Record id'
        },
        {
            value : 'by_pregnancy_relatedness',
            display : 'By pregnancy relatedness'
        }
	];

    const f_result = [];

	sort_list.map((item) => {
        f_result.push(`<option value="${item.value}" ${item.value === p_sort.sort ? 'selected' : ''}>${item.display}</option>`);
    });

	return f_result.join(''); 
}

function render_field_selection(p_sort)
{
	const sort_list = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : 'by_agency_case_id',
            display : 'Agency-Based Case Identifier'
        },
        {
            value : 'by_record_id',
            display : 'Record Id'
        },
        {
            value : 'by_last_name',
            display : 'Last Name'
        },
        {
            value : 'by_first_name',
            display : 'First Name'
        },
        {
            value : 'by_middle_name',
            display : 'Middle Name'
        },
        {
            value : 'by_state_of_death',
            display : 'State of Death'
        },
        {
            value : 'by_year_of_death',
            display : 'Year of Death'
        },
        {
            value : 'by_month_of_death',
            display : 'Month of Death'
        },
        {
            value : 'by_committee_review_date',
            display : 'Committee Review Date'
        },
        {
            value : 'by_date_created',
            display : 'Date Created'
        },
        {
            value : 'by_date_last_updated',
            display : 'Date Last Updated'
        },
        {
            value : 'by_created_by',
            display : 'Created By'
        },
        {
            value : 'by_last_updated_by',
            display : 'Last Updated By'
        }
	];

    const f_result = [];

	sort_list.map((item) => {
       f_result.push(`<option value="${item.value}" ${item.value === p_sort.field_selection ? 'selected' : ''}>${item.display}</option>`);
    });

	return f_result.join('');
}

function renderSortCaseStatus(p_case_view)
{
	const sortCaseStatuses = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : '9999',
            display : '(blank)'
        },
        ,
        {
            value : '1',
            display : 'Abstracting (incomplete)'
        },
        {
            value : '2',
            display : 'Abstraction Complete'
        },
        {
            value : '3',
            display : 'Ready For Review'
        },
        {
            value : '4',
            display : 'Review complete and decision entered'
        },
        {
            value : '5',
            display : 'Out of Scope and death certificate entered'
        },
        {
            value : '6',
            display : 'False Positive and death certificate entered'
        },
        {
            value : '0',
            display : 'Vitals Import'
        },
    ];
    const sortCaseStatusList = [];

	sortCaseStatuses.map((status, i) => {

        return sortCaseStatusList.push(`<option value="${status.value}" ${status.value == p_case_view.case_status ? ' selected ' : ''}>${status.display}</option>`);
    });

	return sortCaseStatusList.join('');
}


function renderPregnancyRelatedness(p_case_view)
{
	const sortCaseStatuses = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : '9999',
            display : '(blank)'
        },
        ,
        {
            value : '1',
            display : 'Pregnancy-related'
        },
        {
            value : '0',
            display : 'Pregnancy-Associated, but NOT-Related'
        },
        {
            value : '2',
            display : 'Pregnancy-Associated, but unable to Determine Pregnancy-Relatedness'
        },
        {
            value : '99',
            display : 'Not Pregnancy-Related or -Associated (i.e. False Positive)'
        }
    ];
    const sortCaseStatusList = [];

	sortCaseStatuses.map((status, i) => {

        return sortCaseStatusList.push(`<option value="${status.value}" ${status.value == p_case_view.pregnancy_relatedness ? ' selected ' : ''}>${status.display}</option>`);
    });

	return sortCaseStatusList.join(''); 
}


function render_filter_records_per_page(p_sort)
{
    const sort_list = [25, 50, 100, 250, 500, 1000];
    const f_result = [];

    sort_list.map((item) => {
        f_result.push(`<option value="${item}" ${item == p_sort.take ? 'selected' : ''}>${item}</option>`)
    });

    return f_result.join('');
}

function clear_case_search() 
{
    // Check if we're in offline mode - if so, skip API calls
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline) {
        console.log('In offline mode - skipping clear_case_search API call');
        return;
    }

    g_ui.case_view_request.search_key = '';
    g_ui.case_view_request.sort = 'by_date_created';
    g_ui.case_view_request.case_status = 'all'
    g_ui.case_view_request.pregnancy_relatedness = 'all';
    g_ui.case_view_request.field_selection = 'all';
    g_ui.case_view_request.descending = true;
    g_ui.case_view_request.take = 100;
    g_ui.case_view_request.page = 1;
    g_ui.case_view_request.skip = 0;
    g_ui.case_view_list = [];

    get_case_set();
}

function search_case_status_onchange(p_value)
{
    if(g_ui.case_view_request.case_status != p_value)
    {
        g_ui.case_view_request.case_status = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}

function search_pregnancy_relatedness_onchange(p_value)
{
    if(g_ui.case_view_request.pregnancy_relatedness != p_value)
    {
        g_ui.case_view_request.pregnancy_relatedness = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}

function search_field_selection_onchange(p_value)
{
    if(g_ui.case_view_request.field_selection != p_value)
    {
        g_ui.case_view_request.field_selection = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}

function records_per_page_change(p_value)
{
    if(p_value != g_ui.case_view_request.take)
    {
        g_ui.case_view_request.take = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}


function app_is_item_pinned(p_id)
{
    var is_pin = 0;
    
    if
    (
        g_pinned_case_set!= null && 
        Object.hasOwn(g_pinned_case_set, 'list')
    )
    {
        if(Object.hasOwn(g_pinned_case_set.list, 'everyone'))
        {
            if(g_pinned_case_set.list.everyone.indexOf(p_id) != -1)
            {
                is_pin = 2;
            }
        }

        if(is_pin == 0)
        {
            if(Object.hasOwn(g_pinned_case_set.list, g_user_name))
            {
                if(g_pinned_case_set.list[g_user_name].indexOf(p_id) != -1)
                {
                    is_pin = 1;
                }
            }
        }
    }

    return is_pin;
}

// Expose app_is_item_pinned globally for use by index.js
window.app_is_item_pinned = app_is_item_pinned;

function render_pin_un_pin_button
(
    p_case_view_item,
    p_is_checked_out,
    p_is_checked_out_expired,
    p_delete_enabled_html
)
{
    const is_pinned = app_is_item_pinned(p_case_view_item.id);

    if(is_pinned == 0)
    {
        return `<input type="image" src="../img/icon_pin.png" title="Pin this case." alt="Pin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="pin_case_clicked('${p_case_view_item.id}')"/>`;
    }
    else if(is_pinned == 1)
    {
        return `<input type="image" src="../img/icon_unpin.png"  title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="unpin_case_clicked('${p_case_view_item.id}')"/>`;
    }
    else
    {
        let click_event = ` onclick="unpin_case_clicked('${p_case_view_item.id}')" `;
        let cursor_pointer = "";
        if(is_pinned == 2 && g_is_jurisdiction_admin == false)
        {
            cursor_pointer = "disabled=disabled";
            click_event = "";
        }

        return `<input type="image" src="../img/icon_unpinMultiple.png" title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" ${cursor_pointer} ${click_event}/>`;
    }
}



function render_app_summary_result_item(item, i)
{

    if(app_is_item_pinned(item.id) != 0)
    {
        return "";
    }

    // Ensure offline state properties have default values
    if (item.value.is_offline === undefined || item.value.is_offline === null) {
        item.value.is_offline = false;
    }

    let is_checked_out = is_case_checked_out(item.value);
    let case_is_locked = is_case_view_locked(item.value);
    // let checked_out_html = ' [not checked out] ';
    let checked_out_html = '';
    let delete_enabled_html = ''; 

    // Check if case is offline by another user
    let is_offline_by_other_user = item.value.is_offline === true && item.value.offline_by && 
        item.value.offline_by !== g_user_name;

    if(case_is_locked || g_is_data_analyst_mode || is_offline_by_other_user)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(!is_checked_out && !is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    // If is_checked_out is true (current user has it checked out) or case is available,
    // then buttons should be enabled (delete_enabled_html stays empty)

    // Check if offline case limit is reached or if an operation is in progress
    const currentOfflineCount = g_ui.offline_case_view_list_by_user ? g_ui.offline_case_view_list_by_user.length : 0;
    const offline_button_disabled = (currentOfflineCount >= offline_mode_max_existing_cases) || g_offline_operation_in_progress;
    const offline_button_disabled_attr = offline_button_disabled ? 'disabled="disabled"' : '';
    const offline_button_style = offline_button_disabled ? 'color: white; background-color: rgba(113, 33, 119, 0.7450980392); border-color: #cfcfcf;' : '';

    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    

    return (
    `<tr class="tr" path="${caseID}">
        <td class="td"><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
            ${checked_out_html}</td>
        <td class="td">${currentCaseStatus}</td>
        <td class="td">${reviewDates}</td>
        <td class="td">${createdBy} - ${dateCreated}</td>
        <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
        <td class="td">
            ${is_checked_out ? (`
            <span class="icn-info">${lockedBy}</span>
            `) : ''}
            ${!is_checked_out && !is_checked_out_expired(item.value) ? (`
            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="Case is locked"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${lockedBy}</span>
            </span>
            `) : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td">       
                <div>
                    <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15; margin-right: 8px;" ${delete_enabled_html}>Delete</button>${render_pin_un_pin_button(item, is_checked_out, is_checked_out_expired(item.value), delete_enabled_html)}
                </div>

                ${(is_offline_mode_enabled && item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-outline-secondary" 
                        onclick="toggle_offline_status('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px; ${offline_button_style}" 
                        ${delete_enabled_html}
                        ${offline_button_disabled_attr}
                        title="Mark for offline use">
                        <span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List
                    </button>
                </div>` : ''}
                </td>`
            ) : ''}
        </tr>`
    );


}


function render_app_pinned_summary_result(item, i)
{
    if(app_is_item_pinned(item.id) == 0)
    {
        return "";
    }

    // Ensure offline state properties have default values
    if (item.value.is_offline === undefined || item.value.is_offline === null) {
        item.value.is_offline = false;
    }

    let is_checked_out = is_case_checked_out(item.value);
    let case_is_locked = is_case_view_locked(item.value);
    // let checked_out_html = ' [not checked out] ';
    let checked_out_html = '';
    let delete_enabled_html = ''; 

    // Check if case is offline by another user
    let is_offline_by_other_user = item.value.is_offline === true && item.value.offline_by && 
        item.value.offline_by !== g_user_name;

    if(case_is_locked || g_is_data_analyst_mode || is_offline_by_other_user)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(!is_checked_out && !is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    // If is_checked_out is true (current user has it checked out) or case is available,
    // then buttons should be enabled (delete_enabled_html stays empty)

    // Check if offline case limit is reached or if an operation is in progress
    const currentOfflineCount = g_ui.offline_case_view_list_by_user ? g_ui.offline_case_view_list_by_user.length : 0;
    const offline_button_disabled = (currentOfflineCount >= offline_mode_max_existing_cases) || g_offline_operation_in_progress;
    const offline_button_disabled_attr = offline_button_disabled ? 'disabled="disabled"' : '';
    const offline_button_style = offline_button_disabled ? 'color: white; background-color: rgba(113, 33, 119, 0.7450980392); border-color: #cfcfcf;' : '';
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    g_pinned_case_count += 1;


    let border_bottom_color = ""
    if(g_pinned_case_count == mmria_count_number_pinned())
    {
        border_bottom_color = 'style="border-bottom-color: #712177;border-bottom-width:2px"';
    }

    return (
    `<tr class="tr" path="${caseID}" style="background-color: #f7f2f7;">
        <td class="td" ${border_bottom_color}><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
            ${checked_out_html}</td>
        <td class="td" ${border_bottom_color}>${currentCaseStatus}</td>
        <td class="td" ${border_bottom_color}>${reviewDates}</td>
        <td class="td" ${border_bottom_color}>${createdBy} - ${dateCreated}</td>
        <td class="td" ${border_bottom_color}>${lastUpdatedBy} - ${lastUpdatedDate}</td>
        <td class="td" ${border_bottom_color}>
            ${is_checked_out ? (`
            <span class="icn-info">${lockedBy}</span>
            `) : ''}
            ${!is_checked_out && !is_checked_out_expired(item.value) ? (`
            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="Case is locked"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${lockedBy}</span>
            </span>
            `) : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td" ${border_bottom_color}>
                <div>
                    <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15; margin-right: 8px;" ${delete_enabled_html}>Delete</button>${render_pin_un_pin_button(item, is_checked_out, is_checked_out_expired(item.value), delete_enabled_html)}
                </div>

                ${(is_offline_mode_enabled && item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-outline-secondary" 
                        onclick="toggle_offline_status('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px; ${offline_button_style}" 
                        ${delete_enabled_html}
                        ${offline_button_disabled_attr}
                        title="Mark for offline use">
                        <span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List
                    </button>
                </div>` : ''}
                </td>`
            ) : ''}
        </tr>`
    );
}

async function pin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_show(p_id, true);
    }
    else
    {
        await mmria_pin_case_click(p_id, false)
    }
}

async function unpin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin && app_is_item_pinned(p_id) != 1)
    {
        $mmria.pin_un_pin_dialog_show(p_id, false);
    }
    else
    {
        await mmria_un_pin_case_click(p_id, false)
    }
}

