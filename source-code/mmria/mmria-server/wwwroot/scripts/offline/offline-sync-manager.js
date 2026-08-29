/**
 * Offline Sync Manager Module
 * Manages syncing offline changes with the server
 */

function append_case_to_update_geo(caseId) {
    try {
        if (!caseId || typeof caseId !== 'string') return;

        const storageKey = 'cases_to_update_geo';
        const normalizedCaseId = caseId.trim();
        if (!normalizedCaseId) return;

        const raw = localStorage.getItem(storageKey) || '';
        const existingIds = raw
            .split(',')
            .map(x => (x || '').trim())
            .filter(x => x && x.toLowerCase() !== 'false');
        const existingIdSet = new Set(existingIds.map(x => x.toLowerCase()));

        if (!existingIdSet.has(normalizedCaseId.toLowerCase())) {
            existingIds.push(normalizedCaseId);
        }

        localStorage.setItem(storageKey, existingIds.join(','));
    } catch (e) {
        offlineLog.warn('OfflineSyncManager', 'Could not append case to cases_to_update_geo:', e);
    }
}

function is_offline_tab_conflict_error(errorText) {
    return typeof errorText === 'string' &&
        errorText.indexOf('Case is offline in another tab for this user.') > -1;
}

function ensure_online_auth_for_processing(operationName) {
    if (
        !window.OfflineStatus ||
        !window.OfflineStatus.isOfflineModeServerSession ||
        !window.OfflineStatus.isOfflineModeServerSession()
    ) {
        return true;
    }

    offlineLog.warn(
        'OfflineSyncManager',
        `Current browser session is limited to SaveOfflineCases. Redirecting to AutoLogin before ${operationName || 'continuing offline processing'}.`
    );

    if (window.OfflineModals && typeof window.OfflineModals.closeLoadingSpinner === 'function') {
        window.OfflineModals.closeLoadingSpinner();
    }

    window.OfflineStatus.redirectToAutoLogin('/Case#/summary');
    return false;
}

function get_server_backed_offline_case_ids(offlineSessionData) {
    if (!offlineSessionData || !Array.isArray(offlineSessionData.offline_ids)) {
        return [];
    }

    return offlineSessionData.offline_ids.filter(caseId =>
        typeof caseId === 'string' && caseId.trim().length > 0
    );
}

function case_requires_server_lock_release(offlineSessionData, caseID) {
    return get_server_backed_offline_case_ids(offlineSessionData).includes(caseID);
}

async function update_offline_case_sync_status(offlineSessionId, caseID, syncState) {
    const response = await fetch('/api/OfflineCase/update-sync-status', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json; charset=utf-8'
        },
        body: JSON.stringify({
            OfflineSessionId: offlineSessionId,
            _id: caseID,
            SyncState: syncState
        })
    });

    let result = null;
    try {
        result = await response.json();
    } catch (_error) {
        result = null;
    }

    if (!response.ok) {
        throw new Error(
            (result && (result.error_description || result.error || result.message || result.details)) ||
            `Failed to update sync status for case ${caseID}.`
        );
    }

    return result;
}

async function release_processing_case_lock_if_needed(offlineSessionData, caseID, actionName) {
    if (!case_requires_server_lock_release(offlineSessionData, caseID)) {
        return false;
    }

    await ReleaseOfflineCaseLocks([caseID]);
    offlineLog.log(
        'OfflineSyncManager',
        `Released server-backed offline lock before ${actionName} for case: ${caseID}`
    );

    return true;
}

// Function to sync offline changes to server
async function sync_offline_changes(caseID) {
    if (!ensure_online_auth_for_processing('syncing offline case changes')) {
        return;
    }
   
    // Prevent multiple operations from running simultaneously
    if (g_processing_operation_in_progress) {
        offlineLog.warn('OfflineSyncManager', 'Another processing operation is already in progress. ');
        return;
    }
    
    try {
        // Set global flag and disable all processing buttons
        g_processing_operation_in_progress = true;
        // if (typeof disable_all_processing_buttons === 'function') {
        //     disable_all_processing_buttons();
        // }

        window.OfflineModals.showLoadingSpinner();  

        // Get the offline session ID
        const offlineSessionId = localStorage.getItem('offline_session_id');
        if (!offlineSessionId) {
            throw new Error('No offline session ID found');
        }

        // Fetch offline session data to get the modified case document
        const offlineSessionData = await get_offline_cases_by_session(offlineSessionId);
        if (!offlineSessionData || !offlineSessionData.case_documents) {
            throw new Error('No offline session data found for session: ' + offlineSessionId);
        }

        // Find the specific case document in the offline session data
        const caseDocument = offlineSessionData.case_documents.find(doc => 
            (doc.modifiedDocument && doc.modifiedDocument._id === caseID) || 
            (doc.ModifiedDocument && doc.ModifiedDocument._id === caseID)
        );
        
        if (!caseDocument) {
            throw new Error('Case not found in offline session data: ' + caseID);
        }

        // Extract the modified document
        const modifiedDocument = caseDocument.modifiedDocument || caseDocument.ModifiedDocument;
        if (!modifiedDocument) {
            throw new Error('No modified document found for case: ' + caseID);
        }
        modifiedDocument.is_offline = false; // Ensure the document is marked as online before syncing
        modifiedDocument.offline_date = null; // Clear offline date
        modifiedDocument.offline_by = null;
        modifiedDocument.offline_lock_type = null;
        // Story 29.6: detect offline-created cases via either the new placeholder
        // pattern ({STATE}-OFFLINE-CASE-XX) or the legacy "-offline" suffix.
        const recordIdForDetection = modifiedDocument.home_record && modifiedDocument.home_record.record_id;
        const isNewOfflineCase = !!recordIdForDetection && (
            /-OFFLINE-CASE-\d+$/i.test(recordIdForDetection) ||
            /-offline$/i.test(recordIdForDetection)
        );

        // Only validate revision for existing cases (not new cases created offline)
        if (!isNewOfflineCase) {
            // Fetch current case document from server to validate revision number
            const currentDocResponse = await fetch(`/api/case?case_id=${caseID}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json; charset=utf-8'
                }
            });

            if (!currentDocResponse.ok) {
                throw new Error(`Failed to fetch current case document: ${currentDocResponse.status} ${currentDocResponse.statusText}`);
            }

            const currentDocument = await currentDocResponse.json();

            // Compare revision numbers to detect if case was modified externally (e.g., unlocked by administrator)
            if (currentDocument._rev !== modifiedDocument._rev) {
                offlineLog.warn('OfflineSyncManager', 'Revision mismatch detected - Unlocked by admin ver:', currentDocument._rev, 'Offline:', modifiedDocument._rev);
        
                show_revision_mismatch_modal(caseID);                
                       
                // Abandon the offline changes to clear the lock
                await abandon_offline_changes(caseID, 4); // 4 = released by admin
                
                // Exit early - do not proceed with sync
                return;
            }
        }

        // Story 29.6: legacy transitional path — strip the "-offline" suffix
        // for pre-29.6 offline caches. New offline cases use the placeholder
        // pattern ({STATE}-OFFLINE-CASE-XX) and the real record_id is assigned
        // by the server in OfflineCaseManager.SyncOfflineCaseAsync before save.
        if (modifiedDocument.home_record && modifiedDocument.home_record.record_id && /-offline$/i.test(modifiedDocument.home_record.record_id)) {
            modifiedDocument.home_record.record_id = modifiedDocument.home_record.record_id.replace(/-offline$/i, '');
            offlineLog.log('OfflineSyncManager', 'record_id_format=legacy_offline_suffix new record_id after removing -offline suffix:', modifiedDocument.home_record.record_id);
        }

        // Make API call using the offline-session workflow.
        const response = await fetch('/api/OfflineCase/sync-case', {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json; charset=utf-8'
            },
            body: JSON.stringify({
                offlineSessionId: offlineSessionId,
                caseId: caseID
            })
        });

        const result = await response.json();

        if (response.ok && result.ok) {
            // Success - update sync status in offline case document
            if (offlineSessionId) {
                try {
                  
                    // Call the update-sync-status API to mark this document as synced
                    const syncStatusResponse = await fetch('/api/OfflineCase/update-sync-status', {
                        method: 'POST',
                        headers: {
                            'Accept': 'application/json',
                            'Content-Type': 'application/json; charset=utf-8'
                        },
                        body: JSON.stringify({
                            OfflineSessionId: offlineSessionId,
                            _id: caseID,
                            SyncState: 1 // 1 = synced
                        })
                    });
                    
                } catch (syncStatusError) {
                    offlineLog.warn('OfflineSyncManager', 'Error updating sync status:', syncStatusError);
                    // Don't fail the entire operation if sync status update fails
                }
            }

            // After sync status update attempt, track case for post-sync geo reminder banner
            append_case_to_update_geo(caseID);

            // Success - remove from offline changes if present
            if (g_offline_changes.has(caseID)) {
                g_offline_changes.delete(caseID);
                save_offline_changes_to_storage();
            }
             offlineLog.log('OfflineSyncManager', 'Case unlocked. Offline changes synced successfully for case:', caseID);
            // Reset flag before refresh
            g_processing_operation_in_progress = false;
            
            // Refresh the processing table to remove the synced case
            const processOfflineCases = localStorage.getItem('process_offline_cases') || 'false';
            
            if (processOfflineCases === 'true' && offlineSessionId) {
                if (typeof get_case_set === 'function') {
                    await get_case_set();
                }         
                //window.OfflineModals.closeLoadingSpinner();   
            }
            
        } else {
            if (is_offline_tab_conflict_error(result && result.error_description)) {
                g_processing_operation_in_progress = false;
                window.OfflineModals.closeLoadingSpinner();

                if (typeof show_edit_offline_case_tab_conflict_modal === 'function') {
                    window.setTimeout(() => {
                        show_edit_offline_case_tab_conflict_modal(caseID);
                    }, 175);
                }

                return;
            }

            throw new Error(result.error_description || 'Failed to sync case');
        }

    } catch (error) {
        offlineLog.error('OfflineSyncManager', '❌ Error syncing case:', error);
        // Reset flag on error
        g_processing_operation_in_progress = false;
        window.OfflineModals.closeLoadingSpinner();

        if (is_offline_tab_conflict_error(error && error.message)) {
            if (typeof show_edit_offline_case_tab_conflict_modal === 'function') {
                window.setTimeout(() => {
                    show_edit_offline_case_tab_conflict_modal(caseID);
                }, 175);
            }
        }
    }
}
// Function to abandon offline changes for a case
async function abandon_offline_changes(caseID, SyncState=2) {
    if (!ensure_online_auth_for_processing('abandoning offline changes')) {
        return;
    }
   
    try {
        // Get the offline session ID
        const offlineSessionId = localStorage.getItem('offline_session_id');
        if (!offlineSessionId) {
            throw new Error('No offline session ID found');
        }

        const offlineSessionData = await get_offline_cases_by_session(offlineSessionId);
        if (!offlineSessionData) {
            throw new Error('No offline session data found for session: ' + offlineSessionId);
        }

        const releasedServerLock = await release_processing_case_lock_if_needed(
            offlineSessionData,
            caseID,
            'abandoning offline changes'
        );

        await update_offline_case_sync_status(offlineSessionId, caseID, SyncState);

        offlineLog.log(
            'OfflineSyncManager',
            releasedServerLock
                ? `Changes abandoned and offline lock cleared for case: ${caseID}`
                : `Changes abandoned for local offline case: ${caseID}`
        );

        g_processing_operation_in_progress = false;

        if (typeof get_case_set === 'function') {
            await get_case_set();
        }

        return;

    } catch (error) {
        offlineLog.error('OfflineSyncManager', '❌ Error abandoning changes:', error);
        // Reset flag on error
        g_processing_operation_in_progress = false;
        throw error;
    }
}

// Function to delete offline changes for a case
async function delete_offline_changes(caseID) {
    if (!ensure_online_auth_for_processing('deleting offline changes')) {
        return;
    }

    try {
        // Get the offline session ID
        const offlineSessionId = localStorage.getItem('offline_session_id');
        
        if (!offlineSessionId) {
            throw new Error('No offline session ID found');
        }

        const offlineSessionData = await get_offline_cases_by_session(offlineSessionId);
        if (!offlineSessionData) {
            throw new Error('No offline session data found for session: ' + offlineSessionId);
        }

        const releasedServerLock = await release_processing_case_lock_if_needed(
            offlineSessionData,
            caseID,
            'deleting offline changes'
        );

        await update_offline_case_sync_status(offlineSessionId, caseID, 3);

        offlineLog.log(
            'OfflineSyncManager',
            releasedServerLock
                ? `Changes deleted and offline lock cleared for case: ${caseID}`
                : `Changes deleted for local offline case: ${caseID}`
        );

        g_processing_operation_in_progress = false;

        if (typeof get_case_set === 'function') {
            await get_case_set();
        }

        return;

    } catch (error) {
        offlineLog.error('OfflineSyncManager', '❌ Error abandoning changes:', error);
        // Reset flag on error
        g_processing_operation_in_progress = false;
        throw error;
    }
}

async function release_case_locks() {
    if (!ensure_online_auth_for_processing('releasing offline case locks')) {
        return;
    }

    try {
        // Validate all required objects exist before attempting to iterate
        if (!g_ui || !g_ui.offline_case_view_list_by_user || !Array.isArray(g_ui.offline_case_view_list_by_user)) {
            offlineLog.log('OfflineSyncManager', 'release_case_locks: Invalid or missing offline case list - skipping');
            return;
        }
        
        const offline_ids = g_ui.offline_case_view_list_by_user;
        offlineLog.log('OfflineSyncManager', `Releasing locks for ${offline_ids.length} cases`);
        
        for (const caseID of offline_ids) {
            if (caseID && caseID.id) {
                await SaveCaseAndReleaseOfflineLock(caseID.id);
            } else {
                offlineLog.warn('OfflineSyncManager', 'release_case_locks: Skipping invalid case ID:', caseID);
            }
        }
    } catch (error) {
        offlineLog.error('OfflineSyncManager', 'Error releasing case locks:', error);
    }
}


// Function to release case locks 
async function abandon_session_release_case_locks() {
    if (!ensure_online_auth_for_processing('loading the active offline session for cleanup')) {
        return;
    }

    try {
        const response = await fetch(`/api/OfflineCase/active-user-session`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        if (!response.ok) {
            offlineLog.error('OfflineSyncManager', 'Failed to fetch active user session:', response.status, response.statusText);
            return;
        }

        const sessionData = await response.json();
        
        // Check if we have a valid session with offline_ids
        if (!sessionData || sessionData.error === "no active sessions") {
            offlineLog.log('OfflineSyncManager', 'No active sessions found - nothing to release');
            return;
        }

        if (!sessionData.offline_ids || !Array.isArray(sessionData.offline_ids)) {
            offlineLog.warn('OfflineSyncManager', 'Session data does not contain offline_ids array');
            return;
        }

        const offline_ids = sessionData.offline_ids;
        offlineLog.log('OfflineSyncManager', `Releasing locks for ${offline_ids.length} cases`);
        
        for (const caseID of offline_ids) {
            if (caseID) {
                await SaveCaseAndReleaseOfflineLock(caseID);
            } else {
                offlineLog.warn('OfflineSyncManager', 'release_case_locks: Skipping null/undefined case ID');
            }
        }
    } catch (error) {
        offlineLog.error('OfflineSyncManager', 'Error releasing case locks:', error);
    }
}
// Function to abandon offline session
async  function abandon_offline_session(reloadAfter=true) {
    if (!ensure_online_auth_for_processing('abandoning the offline session')) {
        return;
    }

    try {
        if(document.getElementById('abandon-offline-session')){
            document.getElementById('abandon-offline-session').disabled = true;
        }
        // Release case locks
        await abandon_session_release_case_locks();

        await new Promise(resolve => setTimeout(resolve, 500));

        //update the offline_state. Call api/offlinecase/update-offline-state to set all cases to offline_state = false
        await fetch('/api/OfflineCase/update-offline-state', {
            method: 'POST',         
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                offlineSessionId: localStorage.getItem('offline_session_id'),
                offlineState: 3
            })
        });
        await new Promise(resolve => setTimeout(resolve, 500));

        window.OfflineTransitionManager.clear_all_cached_data();
        // Clear the specified localStorage items
        //localStorage.removeItem('process_offline_cases');
        //localStorage.removeItem('offline_session_id');
        localStorage.removeItem('abandon_offline_session');
        localStorage.removeItem('offline_bypass_unlock_case_beacon');
        
        // Refresh the page after a short delay to allow the message to be seen
        if (reloadAfter){
            setTimeout(() => {
                window.location.reload();
            }, 500);
        }
    } catch (error) {
        offlineLog.error('OfflineSyncManager', 'Error abandoned offline processing mode:', error);
    }
}

// Function to save case and release offline lock
async function SaveCaseAndReleaseOfflineLock(caseID) {
     if (!ensure_online_auth_for_processing('releasing an offline case lock')) {
        return;
     }

     const response = await fetch('/api/case?case_id=' + caseID, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });
        
        if (response.ok) {
            const result = await response.json();
        
            g_data = result; //set to local var
        

            if(g_data.last_updated_by !== g_user_name || g_data.offline_by !== g_user_name || g_data.is_offline !== "true"){
                offlineLog.error('OfflineSyncManager', 'Failed to release case. This case was not checked out for offline editing by the current user.');
                return
            }

            g_data.date_last_updated = new Date(); 
            g_data.date_last_checked_out = null; 
            g_data.last_checked_out_by = null; 
            g_data.is_offline = false; 
            g_data.offline_date = null;
            g_data.offline_lock_type = null;
            let save_case_request = { 
                Change_Stack:{
                    _id: $mmria.get_new_guid(),
                    case_id: g_data._id,
                    case_rev: g_data._rev,
                    date_created: new Date().toISOString(),
                    user_name: g_user_name, 
                    items: [],
                    metadata_version: "",
                    note: "Manage Case Release"

                },
                Case_Data:g_data
            };

            const saveResponse = await fetch('/api/case', {
                method: 'POST',         
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(save_case_request)
            });
            if (saveResponse.ok) {
                const result = await saveResponse.json();
            } else {
                offlineLog.error('OfflineSyncManager', 'Failed to save case:', saveResponse.status, saveResponse.statusText);
            }

        } else {
            offlineLog.error('OfflineSyncManager', 'Failed to fetch case document:', response.status, response.statusText);
            return [];
        }

}

async function ReleaseOfflineCaseLocks(caseIds) {
    if (!ensure_online_auth_for_processing('releasing offline case locks')) {
        return;
    }

    const validCaseIds = Array.isArray(caseIds)
        ? caseIds.filter(caseId => caseId != null && caseId !== '')
        : [];

    if (validCaseIds.length === 0) {
        return;
    }

    const offlineSessionId = localStorage.getItem('offline_session_id');
    if (!offlineSessionId) {
        throw new Error('offline_session_id not found for offline case lock release.');
    }

    const response = await fetch('/api/OfflineCase/release-case-locks', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            offlineSessionId: offlineSessionId,
            caseIds: validCaseIds
        })
    });

    if (!response.ok) {
        let errorText = `Failed to release offline case locks: ${response.status} ${response.statusText}`;
        try {
            const errorResult = await response.json();
            if (errorResult && errorResult.error_description) {
                errorText = errorResult.error_description;
            } else if (errorResult && errorResult.error) {
                errorText = errorResult.error;
            }
        } catch (_ex) {
        }

        throw new Error(errorText);
    }
}

async function sync_log_data() {
    // Sync logs to server before transitioning back online
    offlineLog.log('OfflineTransitionManager', 'Syncing logs to server...');
    try {
        const syncResult = await offlineLog.syncToServer();
        if (syncResult.success) {
            offlineLog.log('OfflineTransitionManager', `Successfully synced ${syncResult.synced} logs to server`);           
        } else {
            offlineLog.warn('OfflineTransitionManager', 'Log sync failed:', syncResult.message);
        }
    } catch (syncError) {
        offlineLog.error('OfflineTransitionManager', 'Error syncing logs:', syncError);
    }
}

// Function to clear offline processing mode
async function finish_online_processing_mode() {
    if (!ensure_online_auth_for_processing('finishing offline processing mode')) {
        return;
    }

    // Note: Loading spinner should already be shown by caller (case/index.js)
    // We keep it visible throughout and let page reload naturally clean it up
    try {
        const processingSessionData =
            (g_ui && g_ui.offline_session_data) ||
            (g_ui && g_ui.process_offline_case_view_list_by_user) ||
            null;

        // Clear offline locks for all server-backed cases in the session as a final safety net.
        await ReleaseOfflineCaseLocks(get_server_backed_offline_case_ids(processingSessionData));

        //update the offline_state. Call api/offlinecase/update-offline-state to set all cases to offline_state = false
        fetch('/api/OfflineCase/update-offline-state', {
            method: 'POST',         
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                offlineSessionId: localStorage.getItem('offline_session_id'),
                offlineState: 2
            })
        });

        // Clear the specified localStorage items
        localStorage.removeItem('process_offline_cases');
        localStorage.removeItem('offline_session_id');
        localStorage.removeItem('offline_bypass_unlock_case_beacon');

        //sync log data before exiting offline processing mode (non-blocking, keepalive ensures completion)
        await sync_log_data();
        await offlineLog.clearLogs();
        
        // Refresh the page after a short delay to allow the message to be seen
        // Spinner remains visible until reload completes (natural cleanup)
        setTimeout(() => {
            window.location.reload();
        }, 500);
        
    } catch (error) {
        offlineLog.error('OfflineSyncManager', 'Error clearing offline processing mode:', error);
        
        // Only close spinner on error (prevents stuck spinner without reload)
        window.OfflineModals.closeLoadingSpinner();
        
        // Show error to user
        alert('Error completing offline processing mode. Please try refreshing the page manually.');
    }
}
// Function to update cached case document when changes are saved in offline mode
// Sends case data to service worker to ensure encryption is applied before caching
async function update_cached_case_document(caseId, updatedDocument) {
    try {
        if (!('serviceWorker' in navigator)) {
            offlineLog.warn('OfflineSyncManager', 'Service worker not available, skipping cache update');
            return false;
        }

        offlineLog.log('OfflineSyncManager', 'Updating cached case document via service worker:', caseId);

        const registration = await navigator.serviceWorker.ready;
        if (!registration.active) {
            offlineLog.warn('OfflineSyncManager', 'Service worker not active, skipping cache update');
            return false;
        }

        let cacheResult = null;

        if (window.ServiceWorkerManager && typeof window.ServiceWorkerManager.requestResponse === 'function') {
            cacheResult = await window.ServiceWorkerManager.requestResponse({
                type: 'CACHE_CASE_DATA',
                data: {
                    caseId: caseId,
                    caseData: updatedDocument
                }
            }, {
                timeoutMs: 15000,
                useController: false
            });
        } else {
            cacheResult = await new Promise((resolve, reject) => {
                const messageChannel = new MessageChannel();
                let isSettled = false;

                const timeoutId = setTimeout(() => {
                    if (isSettled) {
                        return;
                    }

                    isSettled = true;
                    reject(new Error(`Timed out updating cached case document for ${caseId}`));
                }, 15000);

                messageChannel.port1.onmessage = function(event) {
                    if (isSettled) {
                        return;
                    }

                    isSettled = true;
                    clearTimeout(timeoutId);
                    resolve(event.data);
                };

                try {
                    registration.active.postMessage({
                        type: 'CACHE_CASE_DATA',
                        data: {
                            caseId: caseId,
                            caseData: updatedDocument
                        }
                    }, [messageChannel.port2]);
                } catch (error) {
                    if (isSettled) {
                        return;
                    }

                    isSettled = true;
                    clearTimeout(timeoutId);
                    reject(error);
                }
            });
        }

        if (!cacheResult || cacheResult.success !== true) {
            offlineLog.error('OfflineSyncManager', 'Service worker did not confirm cache update:', {
                caseId: caseId,
                cacheResult: cacheResult
            });
            return false;
        }

        offlineLog.log('OfflineSyncManager', 'Service worker confirmed encrypted cache update:', caseId);
        return true;

    } catch (error) {
        offlineLog.error('OfflineSyncManager', 'Error updating cached case via service worker:', error);
        return false;
    }
}

// Function to save cached case documents to the database
async function save_cached_cases_to_database() {
    offlineLog.log('OfflineSyncManager', 'Saving cached case documents and changes to database...');
    
    try {
        // Get the offline session ID
        const offlineSession = localStorage.getItem('mmria_offline_session');
        if (!offlineSession) {
            offlineLog.error('OfflineSyncManager', 'No mmria_offline_session in localStorage - cannot save cases');
            return;
        }
        
        offlineLog.log('OfflineSyncManager', `mmria_offline_session found (length: ${offlineSession.length})`);
        
        let sessionData;
        let offlineSessionId;
        let offlineIds;
        try {
            sessionData = JSON.parse(offlineSession);
            // Try both possible field names for session ID
            offlineSessionId = sessionData.sessionId || sessionData.offlineSessionId;
            offlineIds = sessionData.offlineIds || sessionData.offline_ids;
            
            offlineLog.log('OfflineSyncManager', 'Parsed session data:', {
                sessionId: offlineSessionId || 'Not found',
                offlineIds_count: offlineIds?.length || 0,
                offlineIds: offlineIds || [],
                hasSessionId: !!sessionData.sessionId,
                hasOfflineSessionId: !!sessionData.offlineSessionId,
                hasOfflineIds: !!sessionData.offlineIds,
                hasOffline_ids: !!sessionData.offline_ids
            });
        } catch (error) {
            offlineLog.error('OfflineSyncManager', 'Error parsing offline session data:', error);
            // Try using the session data directly as a string if JSON parsing fails
            offlineSessionId = offlineSession;
            offlineLog.log('OfflineSyncManager', 'Using offlineSession string as ID after parse failure');
        }
        
        if (!offlineSessionId) {
            offlineLog.error('OfflineSyncManager', 'No offline session ID found in localStorage after parsing');
            return;
        }
        
        offlineLog.log('OfflineSyncManager', `Using offline session ID: ${offlineSessionId}`);
       
        // Get all tracked changes
        const offlineChanges = window.OfflineChangeTracker.getAll();
        
        // Find cases that were taken offline but have no changes
        const changedCaseIds = new Set(offlineChanges.map(change => change.documentId));
        const unchangedCaseIds = offlineIds ? offlineIds.filter(id => !changedCaseIds.has(id)) : [];
        
        offlineLog.log('OfflineSyncManager', `Found ${unchangedCaseIds.length} cases without changes out of ${offlineIds ? offlineIds.length : 0} total offline cases`);
        
        // Fetch unchanged cases and add them to the payload
        const unchangedCases = [];
        offlineLog.log('OfflineSyncManager', `Fetching ${unchangedCaseIds.length} unchanged cases from cache...`);
        
        for (const caseId of unchangedCaseIds) {
            try {
                offlineLog.log('OfflineSyncManager', `Attempting to fetch unchanged case from cache: ${caseId}`);
                const caseDocument = await get_case_for_processing(caseId);
                
                if (caseDocument) {
                    offlineLog.log('OfflineSyncManager', `Successfully fetched unchanged case: ${caseId}`);
                    unchangedCases.push({
                        documentId: caseId,
                        originalDocument: caseDocument,
                        modifiedDocument: caseDocument,
                        timestamp: new Date().toISOString(),
                        changeDescription: 'No changes',
                        syncState: 5, // 5 = no changes
                        userId: g_user_name || 'unknown_user',
                        sessionId: offlineSessionId,
                        changeStackItems: []
                    });
                } else {
                    offlineLog.warn('OfflineSyncManager', `Could not fetch case document for unchanged case: ${caseId} - get_case_for_processing returned null/undefined`);
                }
            } catch (error) {
                offlineLog.error('OfflineSyncManager', `Error fetching unchanged case ${caseId}:`, error);
                offlineLog.error('OfflineSyncManager', `Error details - name: ${error.name}, message: ${error.message}, status: ${error.status}`);
            }
        }
        
        offlineLog.log('OfflineSyncManager', `Successfully fetched ${unchangedCases.length} out of ${unchangedCaseIds.length} unchanged cases`);
        
        let payload = null;
        
        if (offlineChanges.length === 0 && unchangedCases.length === 0) {
            payload = {
                offlineSessionId: offlineSessionId,            
                caseDocuments: []        
            };
        } else {
            const totalCases = offlineChanges.length + unchangedCases.length;
            offlineLog.log('OfflineSyncManager', `Saving ${totalCases} cases (${offlineChanges.length} changed, ${unchangedCases.length} unchanged)`);
            
            // Combine changed and unchanged cases
            const allCaseDocuments = [
                ...offlineChanges.map(change => ({
                    documentId: change.documentId,
                    originalDocument: change.originalDocument,
                    modifiedDocument: change.modifiedDocument,
                    timestamp: change.timestamp,
                    changeDescription: change.changeDescription,
                    syncState: 0, // 0 = not synced, 1 = synced, 2 = abandoned, 3 = error
                    userId: change.userId,
                    sessionId: change.sessionId,
                    changeStackItems: change.changeStackItems || []
                })),
                ...unchangedCases
            ];
            
            payload = {
                offlineSessionId: offlineSessionId,            
                caseDocuments: allCaseDocuments
            };
        }
        
        // Make the API call to save offline document changes
        const response = await fetch(`/api/OfflineCase/update-cases/${offlineSessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });
        
        if (!response.ok) {
            offlineLog.error('OfflineSyncManager', `HTTP error! status: ${response.status}, statusText: ${response.statusText}`);
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        offlineLog.log('OfflineSyncManager', 'Successfully saved offline document changes to database:', result);


        
        // Clear offline changes after successful save
        window.OfflineChangeTracker.clear();
        
        return result;
        
    } catch (error) {
        offlineLog.error('OfflineSyncManager', 'Error saving offline document changes to database:', error);
        throw error; // Re-throw to be handled by calling function
    }
}

// Expose the offline sync manager API to the global scope
window.OfflineSyncManager = {
    sync: sync_offline_changes,
    abandon: abandon_offline_changes,
    deleteChanges: delete_offline_changes,
    abandonOfflineSession: abandon_offline_session,
    saveCaseAndReleaseLock: SaveCaseAndReleaseOfflineLock,
    finishOnlineProcessingMode: finish_online_processing_mode,
    updateCachedDocument: update_cached_case_document,
    saveCasesToDatabase: save_cached_cases_to_database,
    releaseCaseLocks: release_case_locks
};

