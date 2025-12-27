/**
 * Offline Sync Manager Module
 * Manages syncing offline changes with the server
 */

// Function to sync offline changes to server
async function sync_offline_changes(caseID) {
    try {
        console.log('🔄 Starting sync for case:', caseID);
        
        // Show loading state on button
        const buttons = document.querySelectorAll(`button[onclick*="sync_offline_changes('${caseID}')"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Uploading...';
        });

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
        
        // Check if this is a new case created offline by looking for "-offline" suffix in record_id
        const isNewOfflineCase = modifiedDocument.home_record && 
                                 modifiedDocument.home_record.record_id && 
                                 modifiedDocument.home_record.record_id.toLowerCase().indexOf('-offline') >= 0;

        console.log('📤 Syncing document:', caseID, 'from offline session:', offlineSessionId);
        if (isNewOfflineCase) {
            console.log('🆕 Detected new case created offline - skipping server validation');
        }

        // Only validate revision for existing cases (not new cases created offline)
        if (!isNewOfflineCase) {
            // Fetch current case document from server to validate revision number
            console.log('🔍 Fetching current case document to validate revision...');
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
            console.log('📋 Current server revision:', currentDocument._rev);
            console.log('📋 Modified document revision:', modifiedDocument._rev);

            // Compare revision numbers to detect if case was modified externally (e.g., unlocked by administrator)
            if (currentDocument._rev !== modifiedDocument._rev) {
                console.warn('⚠️ Revision mismatch detected! Case was modified externally.');
                console.warn('   Server revision:', currentDocument._rev);
                console.warn('   Offline revision:', modifiedDocument._rev);
                
                // Show modal to inform user about the revision mismatch
                show_revision_mismatch_modal(caseID);
                
                // Abandon the offline changes to clear the lock
                console.log('🗑️ Abandoning offline changes due to revision mismatch...');
                await abandon_offline_changes(caseID);
                
                // Exit early - do not proceed with sync
                return;
            }

            console.log('✅ Revision validation passed - proceeding with sync');
        }

        // Remove "-offline" suffix from record_id if present (for both new and existing cases)
        if (modifiedDocument.home_record && modifiedDocument.home_record.record_id) {
            modifiedDocument.home_record.record_id = modifiedDocument.home_record.record_id.replace(/-offline$/i, '');
        }

        // Helper function to generate GUID (simplified version of $mmria.get_new_guid)
        function generateGuid() {
            let d = new Date().getTime();
            let d2 = (performance && performance.now && (performance.now()*1000)) || 0;
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                let r = Math.random() * 16;
                if(d > 0) {
                    r = (d + r)%16 | 0;
                    d = Math.floor(d/16);
                } else {
                    r = (d2 + r)%16 | 0;
                    d2 = Math.floor(d2/16);
                }
                return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
            });
        }

        // Extract field-level changes from the offline change record, or use generic placeholder for backwards compatibility
        let changeStackItems = [];
        
        if (caseDocument.changeStackItems && Array.isArray(caseDocument.changeStackItems) && caseDocument.changeStackItems.length > 0) {
            // Use the accumulated field-level changes
            changeStackItems = caseDocument.changeStackItems;
            console.log('📦 Using', changeStackItems.length, 'field-level change items from offline tracking');
        } else {
            // Backwards compatibility: use generic placeholder for older offline changes without changeStackItems
            console.log('⚠️ No changeStackItems found - using generic placeholder for backwards compatibility');
            changeStackItems = [
                {
                    _id: modifiedDocument._id,
                    _rev: modifiedDocument._rev,
                    object_path: 'offline_document_sync',
                    metadata_path: '/offline_sync',
                    old_value: 'offline_changes',
                    new_value: 'synced_to_server',
                    dictionary_path: '/offline_sync',
                    metadata_type: 'offline_sync',
                    prompt: 'Offline Document Sync',
                    date_created: new Date().toISOString(),
                    user_name: g_user_name || 'unknown_user'
                }
            ];
        }

        // Create Change_Stack with field-level changes or generic placeholder
        const save_case_request = {
            Change_Stack: {
                _id: generateGuid(),
                case_id: modifiedDocument._id,
                case_rev: modifiedDocument._rev,
                date_created: new Date().toISOString(),
                user_name: g_user_name || 'unknown_user',
                items: changeStackItems,  // Use field-level changes from offline tracking
                metadata_version: g_release_version, // Use global version with fallback
                note: `Offline sync: Document modified offline and synced from session ${offlineSessionId}`
            },
            Case_Data: modifiedDocument
        };

        console.log('📦 Prepared save request for:', caseID);

        // Make API call
        const response = await fetch('/api/case', {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json; charset=utf-8'
            },
            body: JSON.stringify(save_case_request)
        });

        const result = await response.json();
        console.log('📡 API response:', result);

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

                    if (syncStatusResponse.ok) {
                        const syncStatusResult = await syncStatusResponse.json();
                        console.log('📝 Sync status updated successfully:', syncStatusResult);
                    } else {
                        console.warn('Failed to update sync status, but case was saved successfully');
                    }
                } catch (syncStatusError) {
                    console.warn('Error updating sync status:', syncStatusError);
                    // Don't fail the entire operation if sync status update fails
                }
            }

            // Success - remove from offline changes if present
            if (g_offline_changes.has(caseID)) {
                g_offline_changes.delete(caseID);
                save_offline_changes_to_storage();
            }
            
            console.log('✅ Case synced successfully:', caseID);
            show_message('Case synced successfully', 'success');
            
            // Refresh the processing table to remove the synced case
            const processOfflineCases = localStorage.getItem('process_offline_cases') || 'false';
            
            if (processOfflineCases === 'true' && offlineSessionId) {
                if (typeof get_case_set === 'function') {
                    get_case_set();
                }          
            }
            
        } else {
            throw new Error(result.error_description || 'Failed to sync case');
        }

    } catch (error) {
        console.error('❌ Error syncing case:', error);
        show_message('Error syncing case: ' + error.message, 'error');
    } finally {
        // Restore button state
       // const buttons = document.querySelectorAll(`button[onclick*="sync_offline_changes('${caseID}')"]`);
       // buttons.forEach(button => {
       //     button.disabled = false;
       //     button.innerHTML = 'Upload';
       // });
    }
}
// Function to abandon offline changes for a case
async function abandon_offline_changes(caseID) {
    try {
        console.log('🗑️ Abandoning offline changes for case:', caseID);
        
        // Show loading state on button
        const buttons = document.querySelectorAll(`button[onclick*="abandon_offline_changes('${caseID}')"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Abandoning...';
        });

        // Get the offline session ID
        const offlineSessionId = localStorage.getItem('offline_session_id');
        if (!offlineSessionId) {
            throw new Error('No offline session ID found');
        }

          const offlineSessionData = await get_offline_cases_by_session(offlineSessionId);
        if (!offlineSessionData || !offlineSessionData.case_documents) {
            throw new Error('No offline session data found for session: ' + offlineSessionId);
        }

        // Find the specific case document in the offline session data
        const caseDocument = offlineSessionData.case_documents.find(doc => 
            (doc.modifiedDocument && doc.modifiedDocument._id === caseID) || 
            (doc.ModifiedDocument && doc.ModifiedDocument._id === caseID)
        );
        


        // Call the update-sync-status API to mark this document as abandoned
        const response = await fetch('/api/OfflineCase/update-sync-status', {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json; charset=utf-8'
            },
            body: JSON.stringify({
                OfflineSessionId: offlineSessionId,
                _id: caseID,
                SyncState: 2 // 2 = abandoned
            })
        });

        const result = await response.json();
        console.log('📝 Abandon response:', result);

        if (response.ok) {
            
            //add code to update the /api/case document
            // Fetch the original document from the database to clear offline fields
            try {
                
                const getDocResponse = await fetch(`/api/case?case_id=${caseID}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json; charset=utf-8'
                    }
                });

                if (getDocResponse.ok) {
                    const originalDocument = await getDocResponse.json();
                    
                    // Clear the offline fields from the original document
                    originalDocument.is_offline = false;
                    originalDocument.offline_date = null;
                    originalDocument.offline_by = null;
                    originalDocument.date_last_updated = new Date().toISOString();
                    originalDocument.last_updated_by = g_user_name || 'unknown_user';

                    // Helper function to generate GUID
                    function generateGuid() {
                        let d = new Date().getTime();
                        let d2 = (performance && performance.now && (performance.now()*1000)) || 0;
                        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                            let r = Math.random() * 16;
                            if(d > 0) {
                                r = (d + r)%16 | 0;
                                d = Math.floor(d/16);
                            } else {
                                r = (d2 + r)%16 | 0;
                                d2 = Math.floor(d2/16);
                            }
                            return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
                        });
                    }

                    // Create save request to clear offline fields
                    const clearOfflineFieldsRequest = {
                        Change_Stack: {
                            _id: generateGuid(),
                            case_id: originalDocument._id,
                            case_rev: originalDocument._rev,
                            date_created: new Date().toISOString(),
                            user_name: g_user_name || 'unknown_user',
                            items: [
                                {
                                    _id: originalDocument._id,
                                    _rev: originalDocument._rev,
                                    object_path: 'offline_changes_abandoned',
                                    metadata_path: '/offline_abandoned',
                                    old_value: 'true',
                                    new_value: 'false',
                                    dictionary_path: '/offline_abandoned',
                                    metadata_type: 'offline_abandoned',
                                    prompt: 'Abandon Offline Changes',
                                    date_created: new Date().toISOString(),
                                    user_name: g_user_name || 'unknown_user'
                                }
                            ],
                            metadata_version: g_release_version,
                            note: `Abandoned offline changes and cleared offline fields for session ${offlineSessionId}`
                        },
                        Case_Data: originalDocument
                    };

                    console.log('🧹 Clearing offline fields for abandoned case...');

                    // Save the updated document with cleared offline fields
                    const clearResponse = await fetch('/api/case', {
                        method: 'POST',
                        headers: {
                            'Accept': 'application/json',
                            'Content-Type': 'application/json; charset=utf-8'
                        },
                        body: JSON.stringify(clearOfflineFieldsRequest)
                    });

                    const clearResult = await clearResponse.json();
                    console.log('🧹 Clear offline fields response:', clearResult);

                    if (!clearResponse.ok || !clearResult.ok) {
                        console.warn('Failed to clear offline fields after abandon, but abandon was successful');
                    } else {
                        console.log('✅ Offline fields cleared successfully after abandon');
                    }
                } else {
                    console.warn('Failed to fetch original document for clearing offline fields');
                }
            } catch (error) {
                console.warn('Error fetching original document for clearing offline fields:', error);
            }
            
            console.log('✅ Changes abandoned successfully for case:', caseID);
            show_message('Changes abandoned successfully', 'success');
            
            // Force refresh the processing table
            console.log('Starting forced refresh of processing table...');
            
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
            
        } else {
            throw new Error(result.error || 'Failed to abandon changes');
        }

    } catch (error) {
        console.error('❌ Error abandoning changes:', error);
        show_message('Error abandoning changes: ' + error.message, 'error');
    } finally {
        // Restore button state
       //const buttons = document.querySelectorAll(`button[onclick*="abandon_offline_changes('${caseID}')"]`);
       //buttons.forEach(button => {
       //    button.disabled = false;
       //    button.innerHTML = 'Abandon<br/> Changes';
       //});
    }
}

// Function to delete offline changes for a case
async function delete_offline_changes(caseID) {
    try {
        console.log('🗑️ Deleting offline changes for case:', caseID);
        
        // Show loading state on button
        const buttons = document.querySelectorAll(`button[onclick*="abandon_offline_changes('${caseID}')"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Deleting...';
        });

        // Get the offline session ID
        const offlineSessionId = localStorage.getItem('offline_session_id');
        
        if (!offlineSessionId) {
            throw new Error('No offline session ID found');
        }

        // Call the update-sync-status API to mark this document as abandoned
        const response = await fetch('/api/OfflineCase/update-sync-status', {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json; charset=utf-8'
            },
            body: JSON.stringify({
                OfflineSessionId: offlineSessionId,
                _id: caseID,
                SyncState: 3 // 3 = deleted
            })
        });

        const result = await response.json();
        console.log('📝 Abandon response:', result);

        if (response.ok) {
            
            //add code to update the /api/case document
            // Fetch the original document from the database to clear offline fields
            try {
                
                const getDocResponse = await fetch(`/api/case?case_id=${caseID}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json; charset=utf-8'
                    }
                });

                if (getDocResponse.ok) {
                    const originalDocument = await getDocResponse.json();
                    
                    // Clear the offline fields from the original document
                    originalDocument.is_offline = false;
                    originalDocument.offline_date = null;
                    originalDocument.offline_by = null;
                    originalDocument.date_last_updated = new Date().toISOString();
                    originalDocument.last_updated_by = g_user_name || 'unknown_user';

                    // Helper function to generate GUID
                    function generateGuid() {
                        let d = new Date().getTime();
                        let d2 = (performance && performance.now && (performance.now()*1000)) || 0;
                        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                            let r = Math.random() * 16;
                            if(d > 0) {
                                r = (d + r)%16 | 0;
                                d = Math.floor(d/16);
                            } else {
                                r = (d2 + r)%16 | 0;
                                d2 = Math.floor(d2/16);
                            }
                            return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
                        });
                    }

                    // Create save request to clear offline fields
                    const clearOfflineFieldsRequest = {
                        Change_Stack: {
                            _id: generateGuid(),
                            case_id: originalDocument._id,
                            case_rev: originalDocument._rev,
                            date_created: new Date().toISOString(),
                            user_name: g_user_name || 'unknown_user',
                            items: [
                                {
                                    _id: originalDocument._id,
                                    _rev: originalDocument._rev,
                                    object_path: 'offline_changes_abandoned',
                                    metadata_path: '/offline_abandoned',
                                    old_value: 'true',
                                    new_value: 'false',
                                    dictionary_path: '/offline_abandoned',
                                    metadata_type: 'offline_abandoned',
                                    prompt: 'Abandon Offline Changes',
                                    date_created: new Date().toISOString(),
                                    user_name: g_user_name || 'unknown_user'
                                }
                            ],
                            metadata_version: g_release_version,
                            note: `Deleting offline changes and cleared offline fields for session ${offlineSessionId}`
                        },
                        Case_Data: originalDocument
                    };

                    console.log('🧹 Clearing offline fields for deleting case...');

                    // Save the updated document with cleared offline fields
                    const clearResponse = await fetch('/api/case', {
                        method: 'POST',
                        headers: {
                            'Accept': 'application/json',
                            'Content-Type': 'application/json; charset=utf-8'
                        },
                        body: JSON.stringify(clearOfflineFieldsRequest)
                    });

                    const clearResult = await clearResponse.json();
                    console.log('🧹 Clear offline fields response:', clearResult);

                    if (!clearResponse.ok || !clearResult.ok) {
                        console.warn('Failed to clear offline fields after abandon, but abandon was successful');
                    } else {
                        console.log('✅ Offline fields cleared successfully after abandon');
                    }
                } else {
                    console.warn('Failed to fetch original document for clearing offline fields');
                }
            } catch (error) {
                console.warn('Error fetching original document for clearing offline fields:', error);
            }
            
            console.log('✅ Changes abandoned successfully for case:', caseID);
            show_message('Changes abandoned successfully', 'success');
            
            // Force refresh the processing table
            console.log('Starting forced refresh of processing table...');
            
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
            
        } else {
            throw new Error(result.error || 'Failed to abandon changes');
        }

    } catch (error) {
        console.error('❌ Error abandoning changes:', error);
        show_message('Error abandoning changes: ' + error.message, 'error');
    } finally {
        // Restore button state
       //const buttons = document.querySelectorAll(`button[onclick*="abandon_offline_changes('${caseID}')"]`);
       //buttons.forEach(button => {
       //    button.disabled = false;
       //    button.innerHTML = 'Abandon<br/> Changes';
       //});
    }
}

// Function to abandon offline session
async  function abandon_offline_session() {
    try {
        console.log('Abandoning offline processing mode...');
        

        const offline_ids = g_ui.process_offline_case_view_list_by_user.offline_ids;

        for (const caseID of offline_ids) {
            await SaveCaseAndReleaseOfflineLock(caseID);
        }

        //update the offline_state. Call api/offlinecase/update-offline-state to set all cases to offline_state = false
        fetch('/api/OfflineCase/update-offline-state', {
            method: 'POST',         
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                offlineSessionId: localStorage.getItem('offline_session_id'),
                offlineState: 3
            })
        });

        // Clear the specified localStorage items
        localStorage.removeItem('process_offline_cases');
        localStorage.removeItem('offline_session_id');
        localStorage.removeItem('abandon_offline_session');
                
        console.log('Offline processing localStorage items cleared');
        
        // Show a message to the user
        if (typeof show_message === 'function') {
            show_message('Offline processing mode abandoned. Refreshing page...', 'success');
        }
        
        // Refresh the page after a short delay to allow the message to be seen
        setTimeout(() => {
            window.location.reload();
        }, 500);
        
    } catch (error) {
        console.error('Error abandoned offline processing mode:', error);
        if (typeof show_message === 'function') {
            show_message('Error abandoned offline processing mode: ' + error.message, 'error');
        }
    }
}

// Function to save case and release offline lock
async function SaveCaseAndReleaseOfflineLock(caseID) {
     const response = await fetch('/api/case?case_id=' + caseID, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        console.log('Case document response:', response.status, response.statusText);
        
        if (response.ok) {
            const result = await response.json();
        
            g_data = result; //set to local var
        

            if(g_data.last_updated_by !== g_user_name || g_data.offline_by !== g_user_name || g_data.is_offline !== "true"){
                console.error('Failed to release case. This case was not checked out for offline editing by the current user.');
                return
            }

            g_data.date_last_updated = new Date(); 
            g_data.date_last_checked_out = null; 
            g_data.last_checked_out_by = null; 
            g_data.is_offline = false; 
            g_data.offline_date = null;

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
                console.log('Case saved successfully:', result);
            } else {
                console.error('Failed to save case:', saveResponse.status, saveResponse.statusText);
            }

        } else {
            console.error('Failed to fetch case document:', response.status, response.statusText);
            return [];
        }

}

// Function to clear offline processing mode
async function clear_offline_processing_mode() {
    try {
        console.log('Clearing offline processing mode...');
        
        

        //clear locks for cases taken offline with no edits        
        for (const caseID of g_ui.offline_ids_not_changed) {
            await SaveCaseAndReleaseOfflineLock(caseID);
        }


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
                
        console.log('Offline processing localStorage items cleared');
        
        // Show a message to the user
        if (typeof show_message === 'function') {
            show_message('Offline processing mode cleared. Refreshing page...', 'success');
        }
        
        // Refresh the page after a short delay to allow the message to be seen
        setTimeout(() => {
            window.location.reload();
        }, 500);
        
    } catch (error) {
        console.error('Error clearing offline processing mode:', error);
        if (typeof show_message === 'function') {
            show_message('Error clearing offline processing mode: ' + error.message, 'error');
        }
    }
}
// Function to update cached case document when changes are saved in offline mode
// Sends case data to service worker to ensure encryption is applied before caching
async function update_cached_case_document(caseId, updatedDocument) {
    try {
        if (!('serviceWorker' in navigator)) {
            console.warn('Service worker not available, skipping cache update');
            return false;
        }

        console.log('🔄 Updating cached case document via service worker:', caseId);

        const registration = await navigator.serviceWorker.ready;
        if (!registration.active) {
            console.warn('Service worker not active, skipping cache update');
            return false;
        }

        // Send case data to service worker via CACHE_CASE_DATA message
        // The service worker will handle encryption before caching
        registration.active.postMessage({
            type: 'CACHE_CASE_DATA',
            data: {
                caseId: caseId,
                caseData: updatedDocument
            }
        });

        console.log('✅ Sent case data to service worker for encrypted cache update:', caseId);
        return true;

    } catch (error) {
        console.error('Error updating cached case via service worker:', error);
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
            //localStorage.setItem('process_offline_cases', true);
            //set local storage item include the offline session id
           // localStorage.setItem('offline_session_id', offlineSessionId);
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
    clearOfflineMode: clear_offline_processing_mode,
    updateCachedDocument: update_cached_case_document,
    saveCasesToDatabase: save_cached_cases_to_database
};

console.log('Offline Sync Manager module loaded');
