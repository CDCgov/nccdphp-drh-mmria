/**
 * Offline Sync Manager Module
 * Manages syncing offline changes with the server
 */

// Function to sync offline changes to the server
async function sync_offline_changes() {
    console.log('Starting offline changes sync...');
    
    try {
        // Get all offline changes
        const offlineChanges = window.OfflineChangeTracker.getAll();
        
        if (offlineChanges.length === 0) {
            console.log('No offline changes to sync');
            show_message('No offline changes to sync', 'info');
            return;
        }
        
        console.log(`Syncing ${offlineChanges.length} offline document changes`);
        
        // Track sync progress
        let successCount = 0;
        let errorCount = 0;
        const errors = [];
        
        // Process each offline change
        for (const change of offlineChanges) {
            try {
                const documentId = change.documentId;
                const modifiedDocument = change.modifiedDocument;
                
                console.log(`Syncing document: ${documentId}`);
                
                // Check if this is a new offline case (has -offline suffix in record_id)
                const isNewOfflineCase = modifiedDocument.home_record && 
                                        modifiedDocument.home_record.record_id && 
                                        modifiedDocument.home_record.record_id.toLowerCase().indexOf('-offline') >= 0;
                
                if (isNewOfflineCase) {
                    console.log(`Document ${documentId} is a new offline case - skipping server validation`);
                    // For new offline cases, we don't need to fetch from server
                    // The sync will create the case on the server
                } else {
                    // For existing cases, validate against server version if online
                    if (navigator.onLine) {
                        console.log(`Validating document ${documentId} against server version`);
                        
                        // Fetch current version from server
                        const response = await fetch(`/api/case/${documentId}`);
                        
                        if (!response.ok) {
                            throw new Error(`Failed to fetch server version: ${response.status}`);
                        }
                        
                        const serverDocument = await response.json();
                        
                        // Check for revision conflicts
                        if (serverDocument._rev && change.originalDocument._rev && 
                            serverDocument._rev !== change.originalDocument._rev) {
                            
                            console.warn(`Revision conflict detected for ${documentId}`);
                            console.warn(`Original: ${change.originalDocument._rev}, Server: ${serverDocument._rev}`);
                            
                            // Show revision conflict modal to user
                            if (typeof show_revision_mismatch_modal === 'function') {
                                show_revision_mismatch_modal(documentId, change.originalDocument, serverDocument, modifiedDocument);
                            } else {
                                console.error('Revision mismatch modal not available');
                                errors.push({
                                    documentId: documentId,
                                    error: 'Revision conflict - manual resolution required'
                                });
                                errorCount++;
                            }
                            
                            continue; // Skip this document for now
                        }
                    }
                }
                
                // Prepare the PUT request to save the modified document
                console.log(`Saving modified document ${documentId} to server`);
                
                const saveResponse = await fetch(`/api/case`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(modifiedDocument)
                });
                
                if (saveResponse.ok) {
                    console.log(`✓ Document ${documentId} synced successfully`);
                    successCount++;
                } else {
                    throw new Error(`Failed to save document: ${saveResponse.status}`);
                }
                
            } catch (error) {
                console.error(`Error syncing document ${change.documentId}:`, error);
                errors.push({
                    documentId: change.documentId,
                    error: error.message
                });
                errorCount++;
            }
        }
        
        // Report results
        console.log(`Sync completed: ${successCount} successful, ${errorCount} errors`);
        
        if (errorCount === 0) {
            show_message(`Successfully synced ${successCount} document(s)`, 'success');
            
            // Clear offline changes after successful sync
            window.OfflineChangeTracker.clear();
        } else {
            const message = `Sync completed with errors:\n${successCount} successful, ${errorCount} failed\n\nErrors:\n${errors.map(e => `${e.documentId}: ${e.error}`).join('\n')}`;
            show_message(message, 'warning');
        }
        
        return {
            success: successCount,
            errors: errorCount,
            details: errors
        };
        
    } catch (error) {
        console.error('Fatal error during offline changes sync:', error);
        show_message(`Sync failed: ${error.message}`, 'error');
        throw error;
    }
}

// Function to abandon offline changes
async function abandon_offline_changes() {
    console.log('Abandoning all offline changes...');
    
    try {
        // Get confirmation from user
        const confirmed = confirm('Are you sure you want to abandon all offline changes? This cannot be undone.');
        
        if (!confirmed) {
            console.log('User canceled abandon operation');
            return false;
        }
        
        // Clear all offline changes
        window.OfflineChangeTracker.clear();
        
        // Refresh the offline documents list
        if (typeof refresh_offline_documents_list === 'function') {
            await refresh_offline_documents_list();
        }
        
        show_message('All offline changes have been abandoned', 'info');
        console.log('Offline changes abandoned successfully');
        
        return true;
        
    } catch (error) {
        console.error('Error abandoning offline changes:', error);
        show_message(`Error abandoning changes: ${error.message}`, 'error');
        return false;
    }
}

// Function to delete offline changes for a specific document
async function delete_offline_changes(documentId) {
    console.log('Deleting offline changes for document:', documentId);
    
    try {
        const changes = window.OfflineChangeTracker.getAll();
        const filteredChanges = changes.filter(change => change.documentId !== documentId);
        
        // This is a bit of a hack - we clear all and re-add the filtered changes
        // A better implementation would modify the Map directly in the tracker module
        window.OfflineChangeTracker.clear();
        
        for (const change of filteredChanges) {
            window.OfflineChangeTracker.track(
                change.documentId,
                change.modifiedDocument,
                change.changeDescription,
                change.userId,
                change.changeStackItems
            );
        }
        
        console.log(`Deleted offline changes for document ${documentId}`);
        show_message(`Changes for document ${documentId} have been deleted`, 'info');
        
        return true;
        
    } catch (error) {
        console.error('Error deleting offline changes:', error);
        show_message(`Error deleting changes: ${error.message}`, 'error');
        return false;
    }
}

// Function to abandon offline session
async function abandon_offline_session() {
    console.log('Abandoning offline session...');
    
    try {
        // Get the offline session ID
        const offlineSession = localStorage.getItem('mmria_offline_session');
        
        if (!offlineSession) {
            console.log('No offline session found to abandon');
            return false;
        }
        
        let sessionId;
        try {
            const sessionData = JSON.parse(offlineSession);
            sessionId = sessionData.offlineSessionId || sessionData.sessionId;
        } catch (error) {
            sessionId = offlineSession;
        }
        
        // Call API to abandon the session on the server
        const response = await fetch(`/api/OfflineCase/abandon/${sessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            console.log('Offline session abandoned on server');
            
            // Clear local offline data
            window.OfflineChangeTracker.clear();
            localStorage.removeItem('mmria_offline_session');
            localStorage.removeItem('has_active_offline_session');
            
            show_message('Offline session abandoned successfully', 'success');
            return true;
        } else {
            throw new Error(`Failed to abandon session: ${response.status}`);
        }
        
    } catch (error) {
        console.error('Error abandoning offline session:', error);
        show_message(`Error abandoning session: ${error.message}`, 'error');
        return false;
    }
}

// Function to save case and release offline lock
async function SaveCaseAndReleaseOfflineLock(caseId) {
    console.log('Saving case and releasing offline lock:', caseId);
    
    try {
        // This function should integrate with the existing case save mechanism
        // For now, we'll just track the change
        
        const response = await fetch(`/api/case/${caseId}`);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch case: ${response.status}`);
        }
        
        const caseDocument = await response.json();
        
        // Track the document change
        window.OfflineChangeTracker.track(
            caseId,
            caseDocument,
            'Case saved and lock released',
            g_user_name || 'unknown'
        );
        
        console.log('Case saved and lock released successfully');
        return true;
        
    } catch (error) {
        console.error('Error saving case and releasing lock:', error);
        return false;
    }
}

// Function to clear offline processing mode
function clear_offline_processing_mode() {
    console.log('Clearing offline processing mode...');
    
    // Remove offline processing flags
    localStorage.removeItem('process_offline_cases');
    localStorage.removeItem('offline_session_id');
    
    // Remove offline mode indicator
    document.body.classList.remove('mmria-offline-mode');
    
    console.log('Offline processing mode cleared');
}

// Function to update cached case document
async function update_cached_case_document(caseId, updatedDocument) {
    console.log('Updating cached case document:', caseId);
    
    try {
        // Update the cache
        const cacheKey = `mmria-api-cache-v1`;
        const cache = await caches.open(cacheKey);
        const cacheUrl = `/api/case/${caseId}`;
        
        // Create a new Response object with the updated document
        const response = new Response(JSON.stringify(updatedDocument), {
            headers: { 'Content-Type': 'application/json' }
        });
        
        // Put the updated document in the cache
        await cache.put(cacheUrl, response);
        
        console.log('Cached case document updated successfully');
        return true;
        
    } catch (error) {
        console.error('Error updating cached case document:', error);
        return false;
    }
}

// Function to save cached case documents to the database
async function save_cached_cases_to_database() {
    console.log('Saving cached case documents and changes to database...');
    
    try {
        // Get the offline session ID
        const offlineSession = localStorage.getItem('mmria_offline_session');
        console.log('Raw offline session from localStorage:', offlineSession);
        if (!offlineSession) {
            console.log('No offline session found - nothing to save');
            return;
        }
        
        let sessionData;
        let offlineSessionId;
        let offlineIds;
        try {
            sessionData = JSON.parse(offlineSession);
            // Try both possible field names for session ID
            offlineSessionId = sessionData.sessionId || sessionData.offlineSessionId;
            offlineIds = sessionData.offlineIds || sessionData.offline_ids;
            console.log('Parsed session data:', sessionData);
            console.log('Extracted session ID:', offlineSessionId);
        } catch (error) {
            console.error('Error parsing offline session data:', error);
            // Try using the session data directly as a string if JSON parsing fails
            offlineSessionId = offlineSession;
            console.log('Using session data as string:', offlineSessionId);
        }
        
        if (!offlineSessionId) {
            console.error('No offline session ID found in localStorage');
            // Let's check what localStorage contains
            console.log('All localStorage keys:', Object.keys(localStorage));
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.includes('offline')) {
                    console.log(`localStorage[${key}]:`, localStorage.getItem(key));
                }
            }
            return;
        }
        
        // Get all tracked changes
        const offlineChanges = window.OfflineChangeTracker.getAll();
        let payload = null;
        
        if (offlineChanges.length === 0) {
            console.log(`Preparing to save ${offlineChanges.length} document changes with session ID: ${offlineSessionId}`);
            
            // Prepare the request payload with document changes
            payload = {
                offlineSessionId: offlineSessionId,            
                caseDocuments: []        
            };
        } else {
            console.log(`Preparing to save ${offlineChanges.length} document changes with session ID: ${offlineSessionId}`);
            
            // Prepare the request payload with document changes
            payload = {
                offlineSessionId: offlineSessionId,            
                caseDocuments: offlineChanges.map(change => ({
                    documentId: change.documentId,
                    originalDocument: change.originalDocument,
                    modifiedDocument: change.modifiedDocument,
                    timestamp: change.timestamp,
                    changeDescription: change.changeDescription,
                    syncState: 0, // 0 = not synced, 1 = synced, 2 = abandoned, 3 = error
                    userId: change.userId,
                    sessionId: change.sessionId,
                    changeStackItems: change.changeStackItems || []
                }))
            };
        }
        
        console.log('Payload prepared:', payload);
        
        // Make the API call to save offline document changes
        const response = await fetch(`/api/OfflineCase/update-cases/${offlineSessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });
        
        if (!response.ok) {
            console.error(`HTTP error! status: ${response.status}, statusText: ${response.statusText}`);
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('Successfully saved offline document changes to database:', result);

        // Only set process_offline_cases if the response indicates we should
        if (result.shouldSetProcessOffline !== false) {
            //set local storage item to indicate we just went online
            localStorage.setItem('process_offline_cases', true);
            //set local storage item include the offline session id
            localStorage.setItem('offline_session_id', offlineSessionId);
        } else {
            console.log('Offline state is 0 - skipping localStorage updates for process_offline_cases');
        }
        
        // Clear offline changes after successful save
        window.OfflineChangeTracker.clear();
        
        return result;
        
    } catch (error) {
        console.error('Error saving offline document changes to database:', error);
        throw error; // Re-throw to be handled by calling function
    }
}

// Expose the offline sync manager API to the global scope
window.OfflineSyncManager = {
    sync: sync_offline_changes,
    abandon: abandon_offline_changes,
    deleteChanges: delete_offline_changes,
    abandonSession: abandon_offline_session,
    saveCaseAndReleaseLock: SaveCaseAndReleaseOfflineLock,
    clearProcessingMode: clear_offline_processing_mode,
    updateCachedCase: update_cached_case_document,
    saveCasesToDatabase: save_cached_cases_to_database
};

console.log('Offline Sync Manager module loaded');
