// Global function for offline status toggle
async function toggle_offline_status(caseId, caseIndex) {
    try {
        // Show loading state
        var button = document.getElementById('offline_toggle_' + caseIndex);
        var originalContent = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';

        // Make API call to toggle offline status
        var response = await fetch('/api/case/toggle-offline/' + caseId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        var result = await response.json();
        
        if (response.ok && result.success) {
            // Update the case data in the UI
            if (g_ui.case_view_list[caseIndex]) {
                g_ui.case_view_list[caseIndex].value.is_offline = result.is_offline;
                g_ui.case_view_list[caseIndex].value.offline_date = new Date().toISOString();
                g_ui.case_view_list[caseIndex].value.offline_by = g_user_name; // Assuming g_user_name is available
            }

            // Hide the button after adding to offline list (since Remove functionality is only in offline table)
            if (result.is_offline) {
                button.style.display = 'none';
            }

            // Refresh offline documents list
            //refresh_offline_documents_list();
            
            // Refresh the main case listing to remove the case from view
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
        } else {
            throw new Error(result.message || 'Failed to toggle offline status');
        }
    } catch (error) {
        console.log('Error toggling offline status:', error);
        show_message('Error updating offline status: ' + error.message, 'error');
    } finally {
        // Restore button state
        button.disabled = false;
    }
}

// Function to remove a case from offline list (called from offline documents table)
async function remove_from_offline_list(caseId) {
    try {
        // Show loading state
        const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
        });

        // Make API call to toggle offline status
        const response = await fetch('/api/case/toggle-offline/' + caseId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        const result = await response.json();
        
        if (response.ok && result.success) {
            // Refresh offline documents list only - this will update the content without causing flicker
            //refresh_offline_documents_list();

            // Refresh the main case listing to show the case back in the list
            // Refresh the main case listing to remove the case from view
            if (typeof get_case_set === 'function') {
                get_case_set();
            }

            // Update any "Add to Offline List" buttons in the main case list to be visible again
            // Instead of refreshing the entire case list, just update the relevant button states
           // const mainCaseButtons = document.querySelectorAll(`button[id*="offline_toggle_"][onclick*="${caseId}"]`);
           // mainCaseButtons.forEach(button => {
           //     button.style.display = 'block'; // Show the "Add to Offline List" button again
           //     button.disabled = false;
           //     // Reset button text in case it was in loading state
           //     button.innerHTML = 'Add to Offline List';
           // });
        } else {
            throw new Error(result.message || 'Failed to remove case from offline list');
        }
    } catch (error) {
        console.error('Error removing case from offline list:', error);
        show_message('Error removing case from offline list: ' + error.message, 'error');
    } finally {
        // Restore button states for remove buttons in offline table
        const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
        buttons.forEach(button => {
            button.disabled = false;
            button.innerHTML = 'Remove From List';
        });
    }
}

// Global variable to store current offline documents
let g_current_offline_documents = [];

// Global array to map offline case indices to case IDs (for routing)
let g_offline_case_index_map = [];

// Global variable to track offline document changes
let g_offline_changes = new Map();

// Global variable to track original documents for comparison
let g_original_offline_documents = new Map();

// Function to refresh the offline documents list
async function refresh_offline_documents_list() {
    try {
        const offlineDocuments = await get_offline_documents();
        g_current_offline_documents = offlineDocuments; // Store globally
        
        // Build index map for offline case routing
        g_offline_case_index_map = offlineDocuments.map(doc => doc.id);
        
        // Make the index map globally accessible for navigation
        window.g_offline_case_index_map = g_offline_case_index_map;
        
        // Initialize offline change tracking when documents are loaded
        initialize_offline_change_tracking(offlineDocuments);
        
        // Check if we're in offline mode
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        
        // Update the offline-only section (only shown when in offline mode)
        const offlineOnlySection = document.getElementById('offline-only-documents-section');
        if (offlineOnlySection) {
            if (isOfflineMode) {
                offlineOnlySection.innerHTML = render_offline_only_documents_table(offlineDocuments);
            } else {
                offlineOnlySection.innerHTML = ''; // Hide when not in offline mode
            }
        }
        
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

// Function to initialize offline change tracking
function initialize_offline_change_tracking(offlineDocuments) {
    console.log('🔧 Initializing offline change tracking...');
    console.log('🔧 Current offline changes before init:', g_offline_changes.size);
    console.log('🔧 Current tracked documents before init:', Array.from(g_offline_changes.keys()));
    
    // Only reload from localStorage if g_offline_changes is empty or uninitialized
    if (!g_offline_changes || g_offline_changes.size === 0) {
        // Load existing changes from localStorage
        const storedChanges = localStorage.getItem('mmria_offline_changes');
        if (storedChanges) {
            try {
                const changesArray = JSON.parse(storedChanges);
                g_offline_changes = new Map(changesArray);
                console.log('✅ Loaded existing offline changes:', g_offline_changes.size, 'documents with changes');
                console.log('✅ Loaded change document IDs:', Array.from(g_offline_changes.keys()));
            } catch (error) {
                console.error('Error loading offline changes:', error);
                g_offline_changes = new Map();
            }
        } else {
            g_offline_changes = new Map();
            console.log('🔧 No existing offline changes found in localStorage');
        }
    } else {
        console.log('🔧 Preserving existing offline changes in memory:', g_offline_changes.size, 'documents');
    }
    
    // Note: We don't store the case listing metadata as original documents
    // because they don't have the same structure as full case documents.
    // Original documents will be fetched from cache when first needed via 
    // fetchAndStoreOriginalDocument() function to ensure structure consistency.
    
    console.log('✅ Offline change tracking initialized for', offlineDocuments.length, 'documents');
    console.log('✅ Case IDs available for tracking:', Array.from(g_original_offline_documents.keys()));
    console.log('✅ Final offline changes count after init:', g_offline_changes.size);
    console.log('✅ Final tracked documents after init:', Array.from(g_offline_changes.keys()));
}

// Function to track changes to an offline document
function track_offline_document_change(documentId, updatedDocument, changeDescription = '') {
    console.log('📝 Tracking change for document:', documentId);
    console.log('📝 Current offline changes count:', g_offline_changes.size);
    console.log('📝 Current tracked documents:', Array.from(g_offline_changes.keys()));
    
    // Check if we're in offline mode
    const isOffline = localStorage.getItem('is_offline') === 'true';
    if (!isOffline) {
        console.log('Not in offline mode - skipping change tracking');
        return;
    }
    
    // Get the original document for comparison - try to find it in our stored originals
    let originalDoc = g_original_offline_documents.get(documentId);
    console.log('📝 Original document found in g_original_offline_documents:', !!originalDoc);
    
    // If not found, check if we already have a change record with the original document
    if (!originalDoc) {
        const existingChange = g_offline_changes.get(documentId);
        if (existingChange && existingChange.originalDocument) {
            originalDoc = existingChange.originalDocument;
            console.log('Using original document from existing change record');
        }
    }
    
    // If still not found, fetch from cache and store it
    if (!originalDoc) {
        console.log('Original document not found in memory, fetching from cache for:', documentId);
        // We'll fetch it asynchronously and store it for future use
        fetchAndStoreOriginalDocument(documentId, updatedDocument, changeDescription);
        return;
    }
    
    // Get session ID from localStorage
    let sessionId = null;
    try {
        const offlineSession = localStorage.getItem('mmria_offline_session');
        if (offlineSession) {
            try {
                const sessionData = JSON.parse(offlineSession);
                // Try both possible field names for session ID
                sessionId = sessionData.sessionId || sessionData.offlineSessionId;
            } catch (parseError) {
                // Try using the session data directly as a string if JSON parsing fails
                sessionId = offlineSession;
            }
        }
    } catch (error) {
        console.warn('Error getting session ID from localStorage:', error);
    }
    
    // Create change record
    const changeRecord = {
        documentId: documentId,
        originalDocument: JSON.parse(JSON.stringify(originalDoc)), // Deep clone
        modifiedDocument: JSON.parse(JSON.stringify(updatedDocument)), // Deep clone
        timestamp: new Date().toISOString(),
        changeDescription: changeDescription,
        userId: g_user_name || 'unknown_user',
        sessionId: sessionId || 'unknown_session'
    };
    
    // Store the change
    g_offline_changes.set(documentId, changeRecord);
    
    // Persist changes to localStorage
    save_offline_changes_to_storage();
    
    console.log('📝 Change tracked for document:', documentId, 'at', changeRecord.timestamp, 'session:', sessionId);
    console.log('📝 Total offline changes now:', g_offline_changes.size);
    console.log('📝 All tracked documents:', Array.from(g_offline_changes.keys()));
}

// Helper function to fetch and store original document from cache
async function fetchAndStoreOriginalDocument(documentId, updatedDocument, changeDescription) {
    try {
        console.log('Fetching original document from cache:', documentId);
        
        // Get case data from service worker cache
        const cache_url = `/api/case?case_id=${documentId}`;
        
        // Try multiple cache names to find the case data
        const cacheNames = await caches.keys();
        let cached_response = null;
        
        // Look for the case in any mmria cache
        for (const cacheName of cacheNames) {
            if (cacheName.startsWith('mmria-')) {
                const cache = await caches.open(cacheName);
                cached_response = await cache.match(cache_url);
                if (cached_response) {
                    console.log('Found cached original case in:', cacheName);
                    break;
                }
            }
        }
        
        if (cached_response) {
            const originalCaseData = await cached_response.json();
            console.log('Retrieved original case data for change tracking:', documentId);
            
            // Store the original document
            g_original_offline_documents.set(documentId, JSON.parse(JSON.stringify(originalCaseData)));
            
            // Now track the change with the original document
            track_offline_document_change(documentId, updatedDocument, changeDescription);
        } else {
            console.warn('Could not find original document in cache for:', documentId);
            // Still track the change but without original document for comparison
            const changeRecord = {
                documentId: documentId,
                originalDocument: null, // No original for comparison
                modifiedDocument: JSON.parse(JSON.stringify(updatedDocument)),
                timestamp: new Date().toISOString(),
                changeDescription: changeDescription + ' (original document not available)',
                userId: g_user_name || 'unknown_user',
                sessionId: getSessionId()
            };
            
            g_offline_changes.set(documentId, changeRecord);
            save_offline_changes_to_storage();
            console.log('Change tracked without original document:', documentId);
        }
    } catch (error) {
        console.error('Error fetching original document:', error);
        // Fallback: track change without original
        const changeRecord = {
            documentId: documentId,
            originalDocument: null,
            modifiedDocument: JSON.parse(JSON.stringify(updatedDocument)),
            timestamp: new Date().toISOString(),
            changeDescription: changeDescription + ' (fetch error)',
            userId: g_user_name || 'unknown_user',
            sessionId: getSessionId()
        };
        
        g_offline_changes.set(documentId, changeRecord);
        save_offline_changes_to_storage();
        console.log('Change tracked with error fallback:', documentId);
    }
}

// Helper function to get session ID
function getSessionId() {
    try {
        const offlineSession = localStorage.getItem('mmria_offline_session');
        if (offlineSession) {
            try {
                const sessionData = JSON.parse(offlineSession);
                return sessionData.sessionId || sessionData.offlineSessionId;
            } catch (parseError) {
                return offlineSession;
            }
        }
    } catch (error) {
        console.warn('Error getting session ID:', error);
    }
    return 'unknown_session';
}

// Function to save offline changes to localStorage
function save_offline_changes_to_storage() {
    try {
        // Convert Map to Array for storage
        const changesArray = Array.from(g_offline_changes.entries());
        localStorage.setItem('mmria_offline_changes', JSON.stringify(changesArray));
        console.log('Offline changes saved to localStorage:', changesArray.length, 'documents with changes');
    } catch (error) {
        console.error('Error saving offline changes to localStorage:', error);
    }
}

// Function to get all tracked changes
function get_all_offline_changes() {
    return Array.from(g_offline_changes.values());
}

// Function to clear offline changes (called after successful sync)
function clear_offline_changes() {
    g_offline_changes.clear();
    g_original_offline_documents.clear();
    localStorage.removeItem('mmria_offline_changes');
    console.log('Offline changes cleared');
}

// Function to sync offline changes to server
async function sync_offline_changes(caseID) {
    try {
        console.log('🔄 Starting sync for case:', caseID);
        
        // Show loading state on button
        const buttons = document.querySelectorAll(`button[onclick*="sync_offline_changes('${caseID}')"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Syncing...';
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

        console.log('📤 Syncing document:', caseID, 'from offline session:', offlineSessionId);

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

        // Create simplified Change_Stack with single offline change entry
        const save_case_request = {
            Change_Stack: {
                _id: generateGuid(),
                case_id: modifiedDocument._id,
                case_rev: modifiedDocument._rev,
                date_created: new Date().toISOString(),
                user_name: g_user_name || 'unknown_user',
                items: [
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
                ],
                metadata_version: g_release_version || '2.5.8.14', // Use global version with fallback
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
                try {
                    const offlineCases = await get_offline_cases_by_session(offlineSessionId);
                    const processingSection = document.getElementById('offline-processing-section');
                    if (processingSection) {
                        processingSection.innerHTML = render_offline_processing_table(offlineCases);
                    }
                } catch (error) {
                    console.error('Error refreshing processing table:', error);
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
        const buttons = document.querySelectorAll(`button[onclick*="sync_offline_changes('${caseID}')"]`);
        buttons.forEach(button => {
            button.disabled = false;
            button.innerHTML = 'Upload';
        });
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
                            metadata_version: g_release_version || '2.5.8.14',
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
            
            try {
                const offlineSessionId = localStorage.getItem('offline_session_id');
                console.log('Using offline session ID:', offlineSessionId);
                
                if (offlineSessionId) {
                    // Fetch fresh data from server
                    const offlineCases = await get_offline_cases_by_session(offlineSessionId);
                    console.log('Fresh offline cases data:', offlineCases);
                    
                    // Find and update the processing section
                    const processingSection = document.getElementById('offline-processing-section');
                    console.log('Processing section element:', processingSection);
                    
                    if (processingSection) {
                        // Force clear and rebuild the HTML with DOM manipulation
                        processingSection.innerHTML = '';
                        
                        // Force a reflow
                        processingSection.offsetHeight;
                        
                        const newHTML = render_offline_processing_table(offlineCases);
                        console.log('Generated new HTML length:', newHTML.length);
                        
                        // Use a temporary container to ensure proper parsing
                        const tempDiv = document.createElement('div');
                        tempDiv.innerHTML = newHTML;
                        
                        // Move all child nodes from temp container to the actual section
                        while (tempDiv.firstChild) {
                            processingSection.appendChild(tempDiv.firstChild);
                        }
                        
                        console.log('✅ Processing table HTML updated successfully');
                    } else {
                        console.error('❌ Processing section element not found');
                    }
                } else {
                    console.error('❌ No offline session ID available');
                }
            } catch (error) {
                console.error('❌ Error during forced refresh:', error);
            }
            
        } else {
            throw new Error(result.error || 'Failed to abandon changes');
        }

    } catch (error) {
        console.error('❌ Error abandoning changes:', error);
        show_message('Error abandoning changes: ' + error.message, 'error');
    } finally {
        // Restore button state
        const buttons = document.querySelectorAll(`button[onclick*="abandon_offline_changes('${caseID}')"]`);
        buttons.forEach(button => {
            button.disabled = false;
            button.innerHTML = 'Abandon<br/> Changes';
        });
    }
}

// Function to clear offline processing mode and return to normal operation
function clear_offline_processing_mode() {
    try {
        console.log('Clearing offline processing mode...');
        
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
        }, 1000);
        
    } catch (error) {
        console.error('Error clearing offline processing mode:', error);
        if (typeof show_message === 'function') {
            show_message('Error clearing offline processing mode: ' + error.message, 'error');
        }
    }
}

// Make offline change tracking functions globally available
window.track_offline_document_change = track_offline_document_change;
window.initialize_offline_change_tracking = initialize_offline_change_tracking;
window.get_all_offline_changes = get_all_offline_changes;
window.clear_offline_changes = clear_offline_changes;
window.fetchAndStoreOriginalDocument = fetchAndStoreOriginalDocument;
window.sync_offline_changes = sync_offline_changes;
window.abandon_offline_changes = abandon_offline_changes;
window.clear_offline_processing_mode = clear_offline_processing_mode;

// Make network monitoring functions globally available
window.check_network_connectivity = check_network_connectivity;
window.update_go_online_button_state = update_go_online_button_state;
window.handle_network_status_change = handle_network_status_change;
window.initialize_network_monitoring = initialize_network_monitoring;

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
        const response = await fetch(`/api/OfflineCase/by-session/${sessionId}`, {
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

// Function to render offline documents table
// Function to render offline-only documents table (only shown when in offline mode)
function render_offline_only_documents_table(offlineDocuments) {
    let rows;
    const hasOfflineCases = offlineDocuments && offlineDocuments.length > 0;
    
    // Get offline status for debugging
    const isOfflineStatus = localStorage.getItem('is_offline') || 'false';
    
    // Count documents with changes
    let documentsWithChanges = 0;
    try {
        if (g_offline_changes) {
            documentsWithChanges = g_offline_changes.size;
        }
    } catch (error) {
        console.warn('Error counting offline changes:', error);
    }
    
    if (!hasOfflineCases) {
        rows = `
            <tr class="tr">
                <td class="td" colspan="6" style="text-align: center; padding: 20px; color: #6c757d; font-style: italic;">
                    No cases currently available for offline work.
                </td>
            </tr>
        `;
    } else {
        rows = offlineDocuments.map((item, i) => render_offline_only_document_item(item, i)).join('');
    }

    return `       
        <table class="table mb-0">
            <thead class='thead'>
                <tr class='tr bg-tertiary'>
                    <th class='th h4' colspan='6' scope='colgroup'>Offline Case List</th>
                </tr>
                <tr class='tr'>
                    <th class='th' scope='col'>Case Information</th>
                    <th class='th' scope='col'>Case Status</th>
                    <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                    <th class='th' scope='col'>Created</th>
                    <th class='th' scope='col'>Last Updated</th>
                    <th class='th' scope='col' style="width: 115px;">Actions</th>
                </tr>
            </thead>
            <tbody class="tbody">
                ${rows}
            </tbody>
            <tfoot class='tfoot'>
                <tr class='tr'>
                    <td class='td' colspan='5' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #f8f9fa;'>
                        <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #0c5460; line-height: 1.4; font-style: italic;'>
                            <li style='margin-bottom: 4px;'>You are currently working in offline mode.</li>
                            <li style='margin-bottom: 4px;'>These cases are available for offline editing and review.</li>
                            <li style='margin-bottom: 4px;'>Changes made offline will be tracked and synced when you go back online.</li>
                            <li style='margin-bottom: 4px;'>Ensure you sync your changes regularly to prevent data loss.</li>
                            ${documentsWithChanges > 0 ? `<li style='margin-bottom: 0; color: #856404; font-weight: bold;'><i class="fa fa-edit"></i> ${documentsWithChanges} document(s) have been modified offline and will be synced when you go online.</li>` : '<li style="margin-bottom: 0;">No offline changes detected.</li>'}
                        </ul>
                    </td>
                    <td class='td' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #f8f9fa; text-align: right; vertical-align: middle;'>
                        <button type="button" id="go-online-btn" class="btn btn-primary" onclick="go_online_clicked(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                            <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Online">Go Online
                        </button>
                    </td>
                </tr>
            </tfoot>
        </table>
    `;
}

function render_offline_documents_table(offlineDocuments) {
    let rows;
    const hasOfflineCases = offlineDocuments && offlineDocuments.length > 0;
    
    // Get offline status for debugging
    const isOfflineStatus = localStorage.getItem('is_offline') || 'false';
    
    // Count documents with changes
    let documentsWithChanges = 0;
    try {
        if (g_offline_changes) {
            documentsWithChanges = g_offline_changes.size;
        }
    } catch (error) {
        console.warn('Error counting offline changes:', error);
    }
    
    if (!hasOfflineCases) {
        rows = `
            <tr class="tr">
                <td class="td" colspan="6" style="text-align: center; padding: 20px; color: #6c757d; font-style: italic;">
                    No cases currently selected for offline work.
                </td>
            </tr>
        `;
    } else {
        rows = offlineDocuments.map((item, i) => render_offline_document_item(item, i)).join('');
    }

    return `
        <div style="margin-bottom: 10px; padding: 8px 12px; background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; font-size: 12px; color: #495057;">
            <strong>DEBUG:</strong> is_offline = ${isOfflineStatus} | Documents with changes: ${documentsWithChanges}
        </div>
        <table class="table mb-0">
            <thead class='thead'>
                <tr class='tr bg-tertiary'>
                    <th class='th h4' colspan='6' scope='colgroup'>Cases Selected for Offline Work</th>
                </tr>
                <tr class='tr'>
                    <th class='th' scope='col'>Case Information</th>
                    <th class='th' scope='col'>Case Status</th>
                    <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                    <th class='th' scope='col'>Created</th>
                    <th class='th' scope='col'>Last Updated</th>
                    <th class='th' scope='col' style="width: 115px;">Actions</th>
                </tr>
            </thead>
            <tbody class="tbody">
                ${rows}
            </tbody>
            <tfoot class='tfoot'>
                <tr class='tr'>
                    <td class='td' colspan='5' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                        <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic;'>
                            <li style='margin-bottom: 4px;'>Up to 3 existing cases can be brought offline at once.</li>
                            <li style='margin-bottom: 4px;'>Up to 3 new cases can be created offline.</li>
                            <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data. Please bring all cases back online regularly to ensure your data is saved to the system.</li>
                            <li style='margin-bottom: 4px;'>Navigating to another page will reset the list of cases selected for offline work.</li>
                            
                        </ul>
                    </td>
                    <td class='td' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>
                        ${isOfflineStatus === 'true' ? `
                            <button type="button" id="go-online-btn" class="btn btn-primary" onclick="go_online_clicked(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                                <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Online
                            </button>
                        ` : `
                            <button type="button" class="btn btn-primary" onclick="go_offline_clicked()" style="line-height: 1.15; ${!hasOfflineCases ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${!hasOfflineCases ? 'disabled' : ''}>
                                <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Offline
                            </button>
                        `}
                    </td>
                </tr>
            </tfoot>
        </table>
    `;
}

// Function to render offline cases processing table
function render_offline_processing_table(offlineCaseData) {
    if (!offlineCaseData || !offlineCaseData.case_documents || offlineCaseData.case_documents.length === 0) {
        return `
            <table class="table mb-0">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='6' scope='colgroup'>Offline Case List</th>
                    </tr>
                </thead>
                <tbody class="tbody">
                    <tr class="tr">
                        <td class="td" colspan="6" style="text-align: center; padding: 20px; color: #6c757d; font-style: italic;">
                            No offline cases found for processing.
                        </td>
                    </tr>
                </tbody>
            </table>
        `;
    }

    //loop through offlineCaseData.case_documents and determine if sync status for all documents is not equal to zero
    const allDocumentsSynced = offlineCaseData.case_documents.every(doc => doc.syncState !== 0);

    const rows = offlineCaseData.case_documents.map((caseDoc, i) => render_offline_processing_item(caseDoc, i)).join('');

    return `
    <div class="alert alert-success" style="border: 0px;" role="alert">
    <img src="../img/go-online-alert.svg" style="width: 35px; height: 35px; margin-right: 8px; vertical-align: middle;" alt="Online">
    Return to online mode successful. Please upload all offline cases to save changes and access other online cases.</div>
        
        <table class="table mb-0">
            <thead class='thead'>
                <tr class='tr bg-tertiary'>
                    <th class='th h4' colspan='4' scope='colgroup'>Offline Cases Requiring Processing</th>
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
                    <th class='th' scope='col' style="width: 115px;">Actions</th>
                </tr>
            </thead>
            <tbody class="tbody">
                ${rows}
            </tbody>
            <tfoot class='tfoot'>
                <tr class='tr'>
                    <td class='td' colspan='6' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                        <p style='margin: 0; font-size: 13px; color: #6c757d; font-style: italic;'>
                            These cases contain offline modifications that need to be processed and synced to the main database.
                        </p>
                    </td>
                </tr>
            </tfoot>
        </table>
    `;
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

    const canSync = syncState === 0; // Only allow sync if pending
    const canAbandon = syncState === 0; // Only allow abandon if pending
    const canDelete = syncState === 0; // Only allow delete if pending

    // Access nested properties from the proper mmria_case structure
    const caseID = modifiedDocument._id;    
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
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="sync_offline_changes('${caseID}')" style="line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canSync ? 'disabled' : ''}>
                    Upload
                </button>            
                <button type="button" class="btn btn-primary" onclick="delete_new_offline_case('${caseID}')" style="margin-top:2px;line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canDelete ? 'disabled' : ''}>
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
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
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
                <a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="remove_from_offline_list('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;">
                    Remove From List
                </button>
            </td>
        </tr>
    `;
}
// Function to render individual offline document item
function render_offline_document_item(item, i) {
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
    const currentCaseStatus = item.value.case_status == null || item.value.case_status.overall_case_status == null ? '(blank)' : caseStatuses[item.value.case_status.overall_case_status.toString()];
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
                <a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="remove_from_offline_list('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;">
                    Remove From List
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

     if (isProcessingOfflineCases !== 'true') {
        p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);
     }
    p_result.push("<span class='spinner-container spinner-inline ml-2'><span class='spinner-body text-primary'><span class='spinner'></span></span>");
    p_result.push("</div>");
    p_result.push("</div> <!-- end .content-intro -->");
    



    // Load offline documents after page render
    p_post_html_render.push("(async function() {");
    p_post_html_render.push("    try {");
    p_post_html_render.push("        console.log('Starting offline documents load...');");
    p_post_html_render.push("        const offlineDocuments = await get_offline_documents();");
    p_post_html_render.push("        console.log('Offline documents loaded:', offlineDocuments);");
    p_post_html_render.push("        g_current_offline_documents = offlineDocuments;"); // Store globally
    p_post_html_render.push("        // Build index map for offline case routing");
    p_post_html_render.push("        g_offline_case_index_map = offlineDocuments.map(doc => doc.id);");
    p_post_html_render.push("        // Make the index map globally accessible for navigation");
    p_post_html_render.push("        window.g_offline_case_index_map = g_offline_case_index_map;");
    p_post_html_render.push("        console.log('Offline case index map:', window.g_offline_case_index_map);");
    p_post_html_render.push("        // Initialize offline change tracking only if not already initialized");
    p_post_html_render.push("        if (!window.g_offline_tracking_initialized) {");
    p_post_html_render.push("            initialize_offline_change_tracking(offlineDocuments);");
    p_post_html_render.push("            window.g_offline_tracking_initialized = true;");
    p_post_html_render.push("        } else {");
    p_post_html_render.push("            console.log('Offline tracking already initialized, skipping');");
    p_post_html_render.push("        }");
    p_post_html_render.push("        // Initialize network monitoring for Go Online button");
    p_post_html_render.push("        if (typeof initialize_network_monitoring === 'function') {");
    p_post_html_render.push("            initialize_network_monitoring();");
    p_post_html_render.push("        }");
    p_post_html_render.push("        ");
    p_post_html_render.push("        // Check if we need to load and display offline processing cases");
    p_post_html_render.push("        const processOfflineCases = localStorage.getItem('process_offline_cases') || 'false';");
    p_post_html_render.push("        const offlineSessionId = localStorage.getItem('offline_session_id');");
    p_post_html_render.push("        ");
    p_post_html_render.push("        if (processOfflineCases === 'true' && offlineSessionId) {");
    p_post_html_render.push("            console.log('Processing offline cases mode - hiding offline documents sections');");
    p_post_html_render.push("            // Hide the offline-only documents section when processing offline cases");
    p_post_html_render.push("            const offlineOnlySection = document.getElementById('offline-only-documents-section');");
    p_post_html_render.push("            if (offlineOnlySection) {");
    p_post_html_render.push("                offlineOnlySection.style.display = 'none';");
    p_post_html_render.push("            }");
  // p_post_html_render.push("            // Hide the offline documents section when processing offline cases");
  // p_post_html_render.push("            const offlineSection = document.getElementById('offline-documents-section');");
  // p_post_html_render.push("            if (offlineSection) {");
  // p_post_html_render.push("                offlineSection.style.display = 'none';");
  // p_post_html_render.push("            }");
  // p_post_html_render.push("            ");
    p_post_html_render.push("            console.log('Loading offline cases for processing, session ID:', offlineSessionId);");
    p_post_html_render.push("            try {");
    p_post_html_render.push("                const offlineCases = await get_offline_cases_by_session(offlineSessionId);");
    p_post_html_render.push("                console.log('Offline cases loaded for processing:', offlineCases);");
    p_post_html_render.push("                ");
    p_post_html_render.push("                const processingSection = document.getElementById('offline-processing-section');");
    p_post_html_render.push("                if (processingSection) {");
    p_post_html_render.push("                    processingSection.innerHTML = render_offline_processing_table(offlineCases);");
    p_post_html_render.push("                    console.log('Offline processing table rendered');");
    p_post_html_render.push("                } else {");
    p_post_html_render.push("                    console.log('Offline processing section element not found');");
    p_post_html_render.push("                }");
    p_post_html_render.push("            } catch (error) {");
    p_post_html_render.push("                console.error('Error loading offline cases for processing:', error);");
    p_post_html_render.push("                const processingSection = document.getElementById('offline-processing-section');");
    p_post_html_render.push("                if (processingSection) {");
    p_post_html_render.push("                    processingSection.innerHTML = '<div class=\"alert alert-warning\">Unable to load offline cases for processing.</div>';");
    p_post_html_render.push("                }");
    p_post_html_render.push("            }");
    p_post_html_render.push("        } else {");
    p_post_html_render.push("            console.log('Process offline cases not enabled or no session ID found');");
    p_post_html_render.push("            // Check if we're in offline mode");
    p_post_html_render.push("            const isOfflineMode = localStorage.getItem('is_offline') === 'true';");
    p_post_html_render.push("            ");
    p_post_html_render.push("            // Show the offline-only documents section when in offline mode");
    p_post_html_render.push("            const offlineOnlySection = document.getElementById('offline-only-documents-section');");
    p_post_html_render.push("            if (offlineOnlySection) {");
    p_post_html_render.push("                if (isOfflineMode) {");
    p_post_html_render.push("                    offlineOnlySection.innerHTML = render_offline_only_documents_table(offlineDocuments);");
    p_post_html_render.push("                    console.log('Offline-only documents table rendered');");
    p_post_html_render.push("                } else {");
    p_post_html_render.push("                    offlineOnlySection.innerHTML = '';");
    p_post_html_render.push("                    console.log('Offline-only section hidden (not in offline mode)');");
    p_post_html_render.push("                }");
    p_post_html_render.push("            } else {");
    p_post_html_render.push("                console.log('Offline-only section element not found');");
    p_post_html_render.push("            }");
    p_post_html_render.push("            ");

    p_post_html_render.push("        }");
    p_post_html_render.push("    } catch (error) {");
    p_post_html_render.push("        console.error('Error in offline documents load:', error);");
    p_post_html_render.push("    }");
    p_post_html_render.push("})();");

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
    p_result.push("<div id='offline-only-documents-section' class='mb-4'>");
    p_result.push("</div>");

    // Add offline documents section
    //p_result.push("<div id='offline-documents-section' class='mb-4'>");
    //p_result.push("</div>");

    // Add offline processing section
    p_result.push("<div id='offline-processing-section' class='mb-4'>");
    p_result.push("</div>");


    if(is_offline_mode_enabled && isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true'){
        const hasOfflineCases = g_ui.offline_case_view_list_by_user && g_ui.offline_case_view_list_by_user.length > 0;

        p_result.push(`
            <table class="table mb-0">
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
                    ${g_ui.offline_case_view_list_by_user.map((item, i) => render_offline_document_item(item, i)).join('')}
                </tbody>
                <tfoot class='tfoot'>
                    <tr class='tr'>
                        <td class='td' colspan='6' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                            <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic;'>
                                <li style='margin-bottom: 4px;'>Up to 3 existing cases can be brought offline at once.</li>
                                <li style='margin-bottom: 4px;'>Up to 3 new cases can be created offline.</li>
                                <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data. Please bring all cases back online regularly to ensure your data is saved to the system.</li>
                                <li style='margin-bottom: 4px;'>Navigating to another page will reset the list of cases selected for offline work.</li>
                                
                            </ul>
                        </td>                    
                        <td class='td' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>
                            ${isOfflineStatus === 'true' ? `
                                <button type="button" id="go-online-btn" class="btn btn-primary" onclick="go_online_clicked(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                                    <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Online
                                </button>
                            ` : `
                                <button type="button" class="btn btn-primary" onclick="go_offline_clicked()" style="line-height: 1.15; ${!hasOfflineCases ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${!hasOfflineCases ? 'disabled' : ''}>
                                    <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Offline
                                </button>
                            `}
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

    if(case_is_locked || g_is_data_analyst_mode)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(is_checked_out)
    {
        // checked_out_html = ' [checked out by you] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else  if(!is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }

    
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

                ${(item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-outline-secondary" 
                        onclick="toggle_offline_status('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" 
                        ${delete_enabled_html}
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

    if(case_is_locked || g_is_data_analyst_mode)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(is_checked_out)
    {
        // checked_out_html = ' [checked out by you] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else  if(!is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }

    
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

                ${(item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-outline-secondary" 
                        onclick="toggle_offline_status('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" 
                        ${delete_enabled_html}
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

// Function for Go Offline button
function go_offline_clicked() {
    // Check if button is disabled (no cases selected)
    const button = event.target.closest('button');
    if (button && button.disabled) {
        console.log('Go Offline button clicked but disabled - no cases selected');
        return;
    }
    
    console.log('Go Offline button clicked - showing modal');
    show_go_offline_modal();
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
        try {
            sessionData = JSON.parse(offlineSession);
            // Try both possible field names for session ID
            offlineSessionId = sessionData.sessionId || sessionData.offlineSessionId;
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
        const offlineChanges = get_all_offline_changes();
        
        if (offlineChanges.length === 0) {
            console.log('No offline changes found - nothing to save');
            return;
        }
        
        console.log(`Preparing to save ${offlineChanges.length} document changes with session ID: ${offlineSessionId}`);
        
        // Prepare the request payload with document changes
        const payload = {
            offlineSessionId: offlineSessionId,            
            caseDocuments: offlineChanges.map(change => ({
                documentId: change.documentId,
                originalDocument: change.originalDocument,
                modifiedDocument: change.modifiedDocument,
                timestamp: change.timestamp,
                changeDescription: change.changeDescription,
                syncState: 0, // 0 = not synced, 1 = synced, 2 = abandeoned, 3 = error
                userId: change.userId,
                sessionId: change.sessionId
            }))
        };
        
        console.log('Payload prepared:', payload);
        
        // Make the API call to save offline document changes
        const response = await fetch(`/api/OfflineCase/update-cases/${offlineSessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
                // Note: Authorization header can be added if needed
                // 'Authorization': `Bearer ${g_auth_token}`
            },
            body: JSON.stringify(payload)
        });
        
        
        if (!response.ok) {
            console.error(`HTTP error! status: ${response.status}, statusText: ${response.statusText}`);
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('Successfully saved offline document changes to database:', result);

        //set local storage item to indicate we just went online
        localStorage.setItem('process_offline_cases', true);
        //set local storage item include the offline session id
        localStorage.setItem('offline_session_id', offlineSessionId);
        
        // Clear offline changes after successful save
        clear_offline_changes();
        
        return result;
        
    } catch (error) {
        console.error('Error saving offline document changes to database:', error);
        throw error; // Re-throw to be handled by calling function
    }
}

// Function for Go Online button
async function go_online_clicked(event) {
    // Prevent any default behavior and stop event propagation
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    
    console.log('Go Online button clicked - checking network connectivity...');
    
    // First check if we have network connectivity
    const isConnected = await check_network_connectivity();
    if (!isConnected) {
        console.log('Go Online blocked - no network connectivity');
        show_message('Cannot go online - no network connection detected. Please check your internet connection and try again.', 'error');
        return;
    }
    
    console.log('Network connectivity confirmed - transitioning back to online mode');
    
    // Disable the button to prevent multiple clicks
    const goOnlineButton = document.getElementById('go-online-btn');
    if (goOnlineButton) {
        goOnlineButton.disabled = true;
        goOnlineButton.style.opacity = '0.6';
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Going Online...';
        }
    }
    
    // Add a delay to ensure we can see the console logs
    await new Promise(resolve => setTimeout(resolve, 100));
    
    try {
        //add modal while going online
        show_moving_to_online_modal();

        console.log('About to call save_cached_cases_to_database...');
        // First, save cached case documents to the database
        await save_cached_cases_to_database();
        console.log('save_cached_cases_to_database completed successfully');
        
        console.log('About to unregister service worker...');
        
        // Unregister service worker first
        console.log('Unregistering service worker...');
        await unregister_service_worker();
        
        // Clear service worker caches
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHES' });
        }
        
        // Clear all cached data
        console.log('Clearing cached data...');
        await clear_all_cached_data();
        
        // Clear offline session data
        localStorage.removeItem('mmria_offline_session');
        localStorage.removeItem('is_offline');
        localStorage.removeItem('mmria_cached_cases');
        localStorage.removeItem('mmria_offline_changes');
        
        // Remove offline mode indicator from body
        document.body.classList.remove('mmria-offline-mode');
        
        // Add a longer delay before page reload to ensure API call completes
        console.log('Waiting before page reload to ensure API call completes...');
        await new Promise(resolve => setTimeout(resolve, 2000));
        
        // Refresh the page to fully return to online mode
        console.log('Returning to online mode - refreshing page');
        window.location.reload();
        
    } catch (error) {
        console.error('Error transitioning to online mode:', error);
        alert(`Error transitioning to online mode: ${error.message}\nSome cached data may remain. Check console for details.`);
        
        // Re-enable the button if there was an error
        const goOnlineButton = document.getElementById('go-online-btn');
        if (goOnlineButton) {
            goOnlineButton.disabled = false;
            goOnlineButton.style.opacity = '1';
            const buttonText = goOnlineButton.querySelector('.button-text');
            if (buttonText) {
                buttonText.textContent = 'Go Online';
            }
        }
        
        // Don't reload the page if there was an error - this allows debugging
        return false;
    }
}

// Function to show the Go Offline modal
function show_go_offline_modal() {
    // Create modal HTML
    const modalHtml = `
        <div id="go-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Go Offline</h4>
                        <button type="button" class="close" onclick="close_go_offline_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 25px; color: #333;">Please review the following before going offline:</p>
                        
                        <ul style="list-style: disc; padding-left: 20px; margin-bottom: 30px;">
                            <li style="margin-bottom: 15px; font-size: 14px; line-height: 1.5;">
                                To prevent data loss, it is highly recommended to <strong>avoid Incognito mode</strong> when using MMRIA Offline.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 14px; line-height: 1.5;">
                                Once offline, you assume the <strong>risk of losing your data</strong>. All cases created or edited in offline mode will need to be saved and brought back online regularly to be permanently saved in MMRIA.
                            </li>
                            <li style="margin-bottom: 0; font-size: 14px; line-height: 1.5;">
                                Remember the offline login key for use while in offline mode.
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-secondary" onclick="close_go_offline_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="continue_to_set_key()" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Continue to set key
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="go-offline-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('go-offline-modal');
        const backdrop = document.getElementById('go-offline-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close the Go Offline modal
function close_go_offline_modal() {
    const modal = document.getElementById('go-offline-modal');
    const backdrop = document.getElementById('go-offline-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

// Stub function for Continue to set key button
function continue_to_set_key() {
    console.log('Continue to set key button clicked - opening set key modal');
    // Close the current modal first
    close_go_offline_modal();
    // Then show the set key modal
    setTimeout(() => {
        show_set_offline_key_modal();
    }, 200);
}

// Function to show the Set Offline Key modal
function show_set_offline_key_modal() {
    // Create modal HTML
    const modalHtml = `
        <div id="set-offline-key-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Set Offline Key</h4>
                        <button type="button" class="close" onclick="close_set_offline_key_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 20px; color: #333;">Set a key to log in while in offline mode:</p>
                        
                        <input type="text" id="offline-key-input" class="form-control" style="margin-bottom: 10px; padding: 12px; font-size: 14px; border: 1px solid #ccc; border-radius: 4px;" placeholder="Enter your offline key" oninput="handle_key_input()" autocomplete="off" tabindex="1" value="sssDDDkkk@@@2">
                        
                        <div id="key-validation-error" style="display: none; color: #dc3545; font-size: 14px; margin-bottom: 20px; line-height: 1.4;">
                            The provided key does not fulfill one or more of the requirements below. Please update the key and try again.
                        </div>
                        
                        <p style="font-size: 14px; margin-bottom: 20px; color: #666; font-weight: bold;">NOTE: This key will be visible and accessible to the jurisdiction administrator.</p>
                        
                        <p style="font-size: 14px; margin-bottom: 15px; color: #333;">Please follow the following guidance when setting your offline key. The key must contain 10 characters including:</p>
                        
                        <ul style="list-style: disc; padding-left: 20px; margin-bottom: 0;">
                            <li style="margin-bottom: 8px; font-size: 14px; line-height: 1.4;">
                                one uppercase character (A-Z)
                            </li>
                            <li style="margin-bottom: 8px; font-size: 14px; line-height: 1.4;">
                                one lowercase character (a-z)
                            </li>
                            <li style="margin-bottom: 8px; font-size: 14px; line-height: 1.4;">
                                one number (0-9)
                            </li>
                            <li style="margin-bottom: 0; font-size: 14px; line-height: 1.4;">
                                one special character (!@#$%^&*_?><~)
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-secondary" onclick="close_set_offline_key_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" id="go-offline-btn" class="btn btn-primary" onclick="go_offline_final(); " style="background-color: #7b2d8e; border-color: #7b2d8e; color: white; padding: 8px 20px; opacity: 0.6;" disabled>
                            <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 5px; vertical-align: middle;" alt="Go Offline">Go Offline
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="set-offline-key-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('set-offline-key-modal');
        const backdrop = document.getElementById('set-offline-key-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
        // Focus on the input field
        const input = document.getElementById('offline-key-input');
        if (input) {
            input.disabled = false; // Ensure it's enabled
            input.focus();
            input.select(); // Select any existing text
        }
    }, 10);
}

// Function to close the Set Offline Key modal
function close_set_offline_key_modal() {
    const modal = document.getElementById('set-offline-key-modal');
    const backdrop = document.getElementById('set-offline-key-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

// Variable to store the validation timer
let validation_timer = null;

// Function to handle key input with delayed validation
function handle_key_input() {
    // Clear existing timer
    if (validation_timer) {
        clearTimeout(validation_timer);
    }
    
    // Set new timer for 1 second delay
    validation_timer = setTimeout(() => {
        validate_key_realtime();
    }, 300);
}

// Function to validate key in real-time
function validate_key_realtime() {
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    const errorDiv = document.getElementById('key-validation-error');
    const goOfflineBtn = document.getElementById('go-offline-btn');
    
    const isValid = validate_offline_key(key);
    
    if (key.length === 0) {
        // Empty key - hide error, disable button, default border
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        if (keyInput) {
            keyInput.disabled = false; // Ensure input stays enabled
            keyInput.style.borderColor = '#ccc';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = true;
            goOfflineBtn.style.opacity = '0.6';
            goOfflineBtn.style.color = 'white';
            goOfflineBtn.style.backgroundColor = '#7b2d8e';
            goOfflineBtn.style.borderColor = '#7b2d8e';
        }
    } else if (!isValid) {
        // Invalid key - show error, disable button, red border
        if (errorDiv) {
            errorDiv.style.display = 'block';
        }
        if (keyInput) {
            keyInput.disabled = false; // Ensure input stays enabled
            keyInput.style.borderColor = '#dc3545';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = true;
            goOfflineBtn.style.opacity = '0.6';
            goOfflineBtn.style.color = 'white';
            goOfflineBtn.style.backgroundColor = '#7b2d8e';
            goOfflineBtn.style.borderColor = '#7b2d8e';
        }
    } else {
        // Valid key - hide error, enable button, default border
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        if (keyInput) {
            keyInput.disabled = false; // Ensure input stays enabled
            keyInput.style.borderColor = '#ccc';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = false;
            goOfflineBtn.style.opacity = '1';
            goOfflineBtn.style.color = 'white';
            goOfflineBtn.style.backgroundColor = '#7b2d8e';
            goOfflineBtn.style.borderColor = '#7b2d8e';
        }
    }
}

// Function to show the Moving to Offline Mode modal
function show_moving_to_offline_modal() {
    // Create modal HTML
    const modalHtml = `
        <div id="moving-to-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: bold; font-size:17px;">Moving to Offline Mode</h4>
                    </div>
                    <div class="modal-body" style="padding-top: 10px;padding-bottom: 10px; text-align: center;">                        
                        <p style="font-size:17px; color: #333;">Now switching to offline mode - this process may take several minutes.</p>                  
                        <p style="font-size:17px; color: #666;">This screen will refresh when the system is in offline mode.</p>
                    </div>
                </div>
            </div>
        </div>
        <div id="moving-to-offline-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('moving-to-offline-modal');
        const backdrop = document.getElementById('moving-to-offline-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close the Moving to Offline Mode modal
function close_moving_to_offline_modal() {
    const modal = document.getElementById('moving-to-offline-modal');
    const backdrop = document.getElementById('moving-to-offline-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

// Function to show the Moving to Online Mode modal
function show_moving_to_online_modal() {
    // Create modal HTML
    const modalHtml = `
        <div id="moving-to-online-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: bold; font-size:17px;">Moving to Online Mode</h4>
                    </div>
                    <div class="modal-body" style="padding-top: 10px;padding-bottom: 10px; text-align: center;">                        
                        <p style="font-size:17px; color: #333;">Now switching to online mode - this process may take several minutes.</p>                  
                        <p style="font-size:17px; color: #666;">This screen will refresh when the system is back online.</p>
                    </div>
                </div>
            </div>
        </div>
        <div id="moving-to-online-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('moving-to-online-modal');
        const backdrop = document.getElementById('moving-to-online-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close the Moving to Online Mode modal
function close_moving_to_online_modal() {
    const modal = document.getElementById('moving-to-online-modal');
    const backdrop = document.getElementById('moving-to-online-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

// Function for final Go Offline button
async function go_offline_final() {
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    
    // Double-check validation before proceeding
    if (!validate_offline_key(key)) {
        console.log('Key validation failed on final check');
        return;
    }
    
    // Collect offline case IDs from the current offline documents
    const offlineIds = g_current_offline_documents.map(doc => doc.id);
    
    if (offlineIds.length === 0) {
        console.log('No offline cases found to save');
        alert('No cases selected for offline work.');
        return;
    }
    
    console.log('Starting offline mode transition...');
    console.log('Offline key:', key);
    console.log('Offline case IDs:', offlineIds);
    
    // Close the set key modal and show the moving to offline modal
    close_set_offline_key_modal();
    
    // Small delay to ensure the first modal closes before showing the second
    setTimeout(() => {
        show_moving_to_offline_modal();
    }, 200);
    
    try {
        // First, register and enable the service worker
        if (!('serviceWorker' in navigator)) {
            throw new Error('Service Worker not supported in this browser');
        }
        
        console.log('Registering service worker...');
        
        // Check if there's already a service worker registration
        const existingRegistration = await navigator.serviceWorker.getRegistration();
        if (existingRegistration) {
            console.log('Found existing service worker registration, unregistering first...');
            await existingRegistration.unregister();
            // Wait a bit for the unregistration to complete
            await new Promise(resolve => setTimeout(resolve, 500));
        }
        
        const registration = await navigator.serviceWorker.register('/service-worker.js');
        console.log('Service worker registered successfully:', registration);
        
        // Wait for service worker to be ready
        await navigator.serviceWorker.ready;
        console.log('Service worker is ready');
        
        // Use skipWaiting and claim to immediately take control
        if (registration.installing) {
            console.log('Service worker installing, sending skipWaiting message...');
            registration.installing.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.waiting) {
            console.log('Service worker waiting, sending skipWaiting message...');
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.active) {
            console.log('Service worker active, sending claim message...');
            registration.active.postMessage({ type: 'CLAIM_CLIENTS' });
        }
        
        // Wait for the service worker to take control of the page with proper event handling
        if (!navigator.serviceWorker.controller) {
            console.log('Service worker not controlling yet, waiting for controllerchange...');
            
            await new Promise((resolve) => {
                const handleControllerChange = () => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    console.log('Service worker now controlling the page');
                    resolve();
                };
                
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                
                // Set a reasonable timeout
                setTimeout(() => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    console.log('Timeout waiting for controller change, but proceeding');
                    resolve();
                }, 3000);
            });
        } else {
            console.log('Service worker already controlling the page');
        }
        
        // Prepare the request data
        const requestData = {
            OfflineIds: offlineIds,
            OfflineKey: key
        };
        
        // Send POST request to OfflineCaseController
        const response = await fetch('/api/OfflineCase', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        });
        
        if (response.ok) {
            // Check if the response is actually JSON before trying to parse it
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const result = await response.json();
                console.log('Offline data saved successfully:', result);
                
                if (result.ok) {
                    // Success - start offline mode transition
                    console.log('Starting offline resource caching...');
                
                    // Generate secure salt and derive key hash for offline session
                    const keySalt = await generateSecureOfflineKeySalt(result.id, new Date().toISOString());
                    const derivedKeyHash = await deriveOfflineKeyHash(key, keySalt);
                    
                    // Store offline session data with derived key hash (never store plaintext key)
                    const offlineSessionData = {
                        offlineSessionId: result.id,
                        keySalt: keySalt,
                        derivedKeyHash: derivedKeyHash,
                        offlineIds: offlineIds,
                        dateCreated: new Date().toISOString(),
                        isOffline: true
                        // Note: offlineKey is intentionally NOT stored for security
                    };
                    
                    localStorage.setItem('mmria_offline_session', JSON.stringify(offlineSessionData));
                    
                    // Make offline session data globally available for offline login
                    window.mmria_offline_session_data = offlineSessionData;
                    
                    // Cache offline session data with service worker for disconnected access
                    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                        navigator.serviceWorker.controller.postMessage({
                            type: 'CACHE_OFFLINE_SESSION_DATA',
                            data: offlineSessionData
                        });
                        console.log('Secure offline session data (with derived key hash) sent to service worker for caching');
                    }
                    
                    // Set simple offline flag for debugging
                    localStorage.setItem('is_offline', 'true');
                    
                    // Pre-fetch and cache the selected offline cases using service worker
                    await prefetch_offline_cases(offlineIds);
                    
                    // Pre-cache essential pages for navigation
                    await precache_essential_pages();
                    
                    // Cache metadata using service worker
                    await cache_metadata_with_service_worker();
                    
                    // Set up service worker message listener for offline status checks
                    setupServiceWorkerMessageListener();
                    
                    // Close the moving to offline modal
                    close_moving_to_offline_modal();
                    
                    // Refresh the offline documents table to update debug display
                    await refresh_offline_documents_list();
                    
                    // Hide case listing and filters when going offline
                    hideOnlineCaseListingElements();
                    
                    // Set offline mode indicator
                    document.body.classList.add('mmria-offline-mode');
                    
                    // Initialize network monitoring for Go Online button
                    initialize_network_monitoring();
                    
                    // Trigger update of offline mode indicator in breadcrumbs
                    if (window.updateOfflineModeIndicator) {
                        window.updateOfflineModeIndicator();
                    }
                    

                    if (typeof get_case_set === 'function') {
                        get_case_set();
                    }                    
                } else {
                    close_moving_to_offline_modal();
                    console.error('Server returned error:', result.error_description);
                    alert('Error saving offline data: ' + (result.error_description || 'Unknown error'));
                }
            } else {
                close_moving_to_offline_modal();
                console.error('Response is not JSON. Content-Type:', contentType);
                const responseText = await response.text();
                console.error('Response text preview:', responseText.substring(0, 500));
                alert('Error: Server returned an unexpected response format. Please check the console for details.');
            }
        } else {
            close_moving_to_offline_modal();
            console.error('HTTP error:', response.status, response.statusText);
            const responseText = await response.text();
            console.error('Error response:', responseText.substring(0, 500));
            alert('Error saving offline data. Please try again.');
        }
        
    } catch (error) {
        close_moving_to_offline_modal();
        console.error('Error setting up offline mode:', error);
        alert('Error setting up offline mode: ' + error.message);
    }
}

// Function to validate offline key
function validate_offline_key(key) {
    // Check if key is at least 10 characters
    if (key.length < 10) {
        return false;
    }
    
    // Check for at least one uppercase character (A-Z)
    if (!/[A-Z]/.test(key)) {
        return false;
    }
    
    // Check for at least one lowercase character (a-z)
    if (!/[a-z]/.test(key)) {
        return false;
    }
    
    // Check for at least one number (0-9)
    if (!/[0-9]/.test(key)) {
        return false;
    }
    
    // Check for at least one special character (!@#$%^&*_?><~)
    if (!/[!@#$%^&*_?><~]/.test(key)) {
        return false;
    }
    
    return true;
}

// Secure key derivation functions for offline mode
const OFFLINE_KEY_DERIVATION_ITERATIONS = 100000; // PBKDF2 iterations for offline keys
const OFFLINE_HASH_ALGORITHM = 'SHA-256';
const OFFLINE_KEY_LENGTH = 256; // bits

// Function to generate a secure salt for offline key derivation
async function generateSecureOfflineKeySalt(sessionId, timestamp) {
    try {
        // Combine session ID, timestamp, and cryptographic random data
        const randomArray = new Uint8Array(32); // 256 bits of randomness
        crypto.getRandomValues(randomArray);
        const randomHex = Array.from(randomArray, byte => byte.toString(16).padStart(2, '0')).join('');
        
        // Create a composite salt from multiple entropy sources
        const compositeSalt = `${sessionId}-${timestamp}-${randomHex}-${navigator.userAgent.length}`;
        
        // Hash the composite salt to ensure consistent length and format
        const encoder = new TextEncoder();
        const saltBuffer = await crypto.subtle.digest(OFFLINE_HASH_ALGORITHM, encoder.encode(compositeSalt));
        const saltArray = Array.from(new Uint8Array(saltBuffer));
        return saltArray.map(b => b.toString(16).padStart(2, '0')).join('');
    } catch (error) {
        console.error('Error generating secure offline key salt:', error);
        // Fallback to simpler salt generation
        return `${sessionId}-${timestamp}-${Math.random().toString(36).substring(2)}`;
    }
}

// Function to derive offline key hash using PBKDF2
async function deriveOfflineKeyHash(password, salt, iterations = OFFLINE_KEY_DERIVATION_ITERATIONS) {
    try {
        const encoder = new TextEncoder();
        const keyMaterial = await crypto.subtle.importKey(
            'raw',
            encoder.encode(password),
            { name: 'PBKDF2' },
            false,
            ['deriveBits']
        );
        
        const derivedBits = await crypto.subtle.deriveBits(
            {
                name: 'PBKDF2',
                salt: encoder.encode(salt),
                iterations: iterations,
                hash: OFFLINE_HASH_ALGORITHM
            },
            keyMaterial,
            OFFLINE_KEY_LENGTH
        );
        
        // Convert to hex string for storage and comparison
        const hashArray = Array.from(new Uint8Array(derivedBits));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    } catch (error) {
        console.error('Error deriving offline key hash:', error);
        throw new Error('Failed to derive offline key hash');
    }
}

// Function to pre-fetch offline cases using the service worker
async function prefetch_offline_cases(offlineIds) {
    console.log('Pre-fetching offline cases...');
    
    try {
        // Wait for service worker to be ready and controlling
        await navigator.serviceWorker.ready;
        
        // Wait a bit for the service worker to take control
        if (!navigator.serviceWorker.controller) {
            console.log('Service worker not controlling yet, waiting for controllerchange event...');
            
            await new Promise((resolve) => {
                const handleControllerChange = () => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    console.log('Service worker now controlling the page via controllerchange event');
                    resolve();
                };
                
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                
                // Set a reasonable timeout
                setTimeout(() => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    console.log('Timeout waiting for controllerchange in prefetch, but proceeding');
                    resolve();
                }, 2000);
            });
        }
        
        const serviceWorker = navigator.serviceWorker.controller;
        if (!serviceWorker) {
            console.warn('Service worker not controlling the page, but proceeding with fetch requests');
            console.warn('This may still work as the service worker should intercept the requests');
        } else {
            console.log('Service worker is controlling, starting pre-fetch...');
        }
        
        // Pre-fetch each case using the /api/case?case_id= endpoint
        for (const caseId of offlineIds) {
            try {
                console.log(`Pre-fetching case: ${caseId}`);
                const response = await fetch(`/api/case?case_id=${caseId}`);
                
                if (response.ok) {
                    // Check if the response is actually JSON before trying to parse it
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        const caseData = await response.json();
                        console.log(`Successfully fetched case ${caseId}, now sending to service worker`);
                        
                        // Send case data to service worker for caching if we have a controller
                        if (serviceWorker) {
                            serviceWorker.postMessage({
                                type: 'CACHE_CASE_DATA',
                                data: {
                                    caseId: caseId,
                                    caseData: caseData
                                }
                            });
                            
                            console.log(`Successfully sent case ${caseId} to service worker for caching`);
                        } else {
                            console.log(`Case ${caseId} fetched but no service worker controller to send message to (will be cached via fetch interception)`);
                        }
                    } else {
                        console.error(`Case ${caseId} response is not JSON. Content-Type:`, contentType);
                        const responseText = await response.text();
                        console.error(`Case ${caseId} response preview:`, responseText.substring(0, 200));
                    }
                } else {
                    console.error(`Failed to pre-fetch case ${caseId}: ${response.status} ${response.statusText}`);
                }
            } catch (error) {
                console.error(`Error pre-fetching case ${caseId}:`, error);
            }
        }
        
        console.log(`Completed pre-fetching ${offlineIds.length} cases`);
        
    } catch (error) {
        console.error('Error in prefetch_offline_cases:', error);
        throw error;
    }
}

// Function to pre-cache essential pages for offline mode
async function precache_essential_pages() {
    console.log('Pre-caching essential pages...');
    
    const essentialPages = [
        '/Case'
        // Note: /Case/summary doesn't exist as a server route
        // Client-side routes like /Case#/summary are handled by the main /Case page
    ];
    
    try {
        for (const pagePath of essentialPages) {
            try {
                console.log(`Pre-caching page: ${pagePath}`);
                const response = await fetch(pagePath);
                
                if (response.ok) {
                    // The service worker should automatically cache this response
                    console.log(`Successfully pre-cached page: ${pagePath}`);
                } else {
                    console.warn(`Failed to pre-cache page ${pagePath}: ${response.status} ${response.statusText}`);
                }
            } catch (error) {
                console.warn(`Error pre-caching page ${pagePath}:`, error);
            }

        }
        
        console.log('Essential pages pre-caching completed');
        
    } catch (error) {
        console.error('Error in precache_essential_pages:', error);
        throw error;
    }
}

// Function to cache metadata using service worker
async function cache_metadata_with_service_worker() {
    console.log('🚀 Caching metadata with service worker for offline mode...');
    
    try {
        // First determine the current version
        let currentVersion = g_release_version;
        if (!currentVersion) {
            try {
                const metadataResponse = await fetch('/api/metadata');
                if (metadataResponse.ok) {
                    // Check if the response is actually JSON before trying to parse it
                    const contentType = metadataResponse.headers.get('content-type');
                    if (contentType && contentType.includes('application/json')) {
                        const metadata = await metadataResponse.json();
                        currentVersion = metadata.version || metadata.data_dictionary?.version;
                    } else {
                        console.warn('Metadata response is not JSON, got content-type:', contentType);
                        const responseText = await metadataResponse.text();
                        console.warn('Response text preview:', responseText.substring(0, 200));
                        currentVersion = 'latest'; // fallback
                    }
                } else {
                    console.warn('Metadata response not OK:', metadataResponse.status, metadataResponse.statusText);
                    currentVersion = 'latest'; // fallback
                }
            } catch (error) {
                console.warn('Could not determine metadata version, using fallback. Error:', error.message);
                currentVersion = 'latest'; // fallback
            }
        }
        
        console.log(`📋 Caching metadata for version: ${currentVersion}`);
        
        // Check if service worker is available and active
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            console.log('📡 Service worker is available and active');
            
            // Send message to service worker to cache metadata
            navigator.serviceWorker.controller.postMessage({
                type: 'CACHE_METADATA',
                version: currentVersion
            });
            
            // Wait for service worker to process the caching request
            await new Promise(resolve => setTimeout(resolve, 3000));
            console.log('✅ Metadata caching request sent to service worker');
            
        } else {
            console.warn('⚠️ Service worker not available, falling back to basic fetch caching');
        }
        
        // Always perform basic fetch to ensure resources are cached (fallback or supplement)
        const criticalEndpoints = [
            `/api/version/${currentVersion}/metadata`,
            `/api/version/${currentVersion}/ui_specification`,
            `/api/version/${currentVersion}/validation`,
            '/_users/GetFormAccess',
            '/api/user/my-user',
            '/api/user_role_jurisdiction_view/my-roles'
        ];
        
        console.log(`📥 Fetching ${criticalEndpoints.length} critical metadata endpoints...`);
        
        for (const endpoint of criticalEndpoints) {
            try {
                const response = await fetch(endpoint);
                if (response.ok) {
                    console.log(`✓ Fetched: ${endpoint}`);
                } else {
                    console.warn(`⚠️ Failed to fetch ${endpoint}: ${response.status}`);
                }
            } catch (error) {
                console.warn(`❌ Error fetching ${endpoint}:`, error);
            }
        }
        
        console.log('📦 Metadata caching process completed');
        
    } catch (error) {
        console.error('❌ Error in metadata caching process:', error);
        throw error;
    }
}

// Function to set up service worker message listener
function setupServiceWorkerMessageListener() {
    if (!navigator.serviceWorker) return;
    
    navigator.serviceWorker.addEventListener('message', event => {
        const { type, data } = event.data;
        
        switch (type) {
            case 'CHECK_OFFLINE_STATUS':
                // Respond with current offline status
                const isOffline = localStorage.getItem('is_offline') === 'true';
                event.source.postMessage({
                    type: 'OFFLINE_STATUS_RESPONSE',
                    isOffline: isOffline
                });
                break;
                
            default:
                console.log('Service Worker message:', event.data);
        }
    });
    
    console.log('Service worker message listener set up');
}

// Function to unregister service worker (for going back online)
async function unregister_service_worker() {
    if ('serviceWorker' in navigator) {
        try {
            console.log('Starting service worker unregistration...');
            const registrations = await navigator.serviceWorker.getRegistrations();
            console.log(`Found ${registrations.length} service worker registrations to unregister`);
            
            for (const registration of registrations) {
                console.log('Unregistering service worker:', registration.scope);
                const result = await registration.unregister();
                console.log('Service worker unregistered successfully:', result);
            }
            
            // Wait a bit for the unregistration to fully complete
            await new Promise(resolve => setTimeout(resolve, 1000));
            
            // Clear the controller reference
            if (navigator.serviceWorker.controller) {
                console.log('Service worker controller still present, waiting for it to clear...');
                await new Promise(resolve => setTimeout(resolve, 1000));
            }
            
            console.log('Service worker unregistration completed');
        } catch (error) {
            console.error('Error unregistering service worker:', error);
            throw error;
        }
    }
}

// Function to clear all cached data when going back online
async function clear_all_cached_data() {
    console.log('Clearing all cached data...');
    
    try {
        // Clear Cache API storage
        if ('caches' in window) {
            const cacheNames = await caches.keys();
            console.log(`Found ${cacheNames.length} caches to clear:`, cacheNames);
            
            for (const cacheName of cacheNames) {
                if (cacheName.startsWith('mmria-')) {
                    const deleted = await caches.delete(cacheName);
                    console.log(`Cache '${cacheName}' deleted:`, deleted);
                }
            }
        }
        
        // Clear relevant localStorage items
        const localStorageKeys = [
            'mmria_offline_session',
            'is_offline',
            'mmria_cached_cases',
            'mmria_offline_changes',
            'mmria_offline_case_documents'
        ];
        
        for (const key of localStorageKeys) {
            if (localStorage.getItem(key)) {
                localStorage.removeItem(key);
                console.log(`Cleared localStorage key: ${key}`);
            }
        }
        
        // Clear any other MMRIA-related cached data
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && (key.startsWith('mmria_static_') || key.startsWith('mmria_meta_'))) {
                localStorage.removeItem(key);
                console.log(`Cleared cached resource: ${key}`);
                i--; // Adjust index since we removed an item
            }
        }
        
        console.log('All cached data cleared successfully');
        
    } catch (error) {
        console.error('Error clearing cached data:', error);
        throw error;
    }
}

/* OLD CACHE FUNCTIONS - Replaced by Service Worker implementation
// Function to cache offline resources and case documents
async function cache_offline_resources(offlineIds, offlineKey, sessionId) {
    console.log('Starting resource caching for offline mode...');
    
    try {
        // Initialize caches
        await initialize_offline_caches();
        
        // Cache static resources (CSS, JS, HTML)
        console.log('Caching static resources...');
        await cache_static_resources();
        
        // Cache case documents
        console.log('Caching case documents...');
        await cache_case_documents(offlineIds);
        
        // Cache metadata and form definitions
        console.log('Caching metadata...');
        await cache_metadata();
        
        console.log('All resources cached successfully');
        
    } catch (error) {
        console.error('Error caching offline resources:', error);
        throw error;
    }
}

// Function to initialize cache storage
async function initialize_offline_caches() {
    if ('caches' in window) {
        // Create cache for static resources
        await caches.open('mmria-static-v1');
        
        // Create cache for case documents
        await caches.open('mmria-cases-v1');
        
        // Create cache for metadata
        await caches.open('mmria-metadata-v1');
        
        console.log('Cache storage initialized');
    } else {
        console.warn('Cache API not supported, using localStorage fallback');
    }
}

// Function to cache static resources
async function cache_static_resources() {
    const staticResources = [
        // CSS files
        '/css/index.css',
        '/css/bootstrap.min.css',
        '/css/mmria.css',
        
        // JavaScript files
        '/scripts/editor/page_renderer/app.mmria.js',
        '/scripts/editor/page_renderer/string.js',
        '/scripts/jquery.min.js',
        '/scripts/bootstrap.min.js',
        
        // Essential HTML pages (if any)
        '/',
        '/Home/Index',
        
        // Icons and images
        '/img/icon_pin.png',
        '/img/icon_unpin.png',
        '/img/icon_unpinMultiple.png',
    ];
    
    if ('caches' in window) {
        const cache = await caches.open('mmria-static-v1');
        
        for (const resource of staticResources) {
            try {
                const response = await fetch(resource);
                if (response.ok) {
                    await cache.put(resource, response);
                    console.log(`Cached static resource: ${resource}`);
                }
            } catch (error) {
                console.warn(`Failed to cache resource ${resource}:`, error);
            }
        }
    } else {
        // Fallback to localStorage for static resources
        for (const resource of staticResources) {
            try {
                const response = await fetch(resource);
                if (response.ok) {
                    const content = await response.text();
                    localStorage.setItem(`mmria_static_${resource.replace(/[^a-zA-Z0-9]/g, '_')}`, content);
                }
            } catch (error) {
                console.warn(`Failed to cache resource ${resource}:`, error);
            }
        }
    }
}

// Function to cache case documents
async function cache_case_documents(offlineIds) {
    const cacheStorage = 'caches' in window ? await caches.open('mmria-cases-v1') : null;
    const caseDocuments = [];
    
    console.log(`Fetching ${offlineIds.length} case documents for offline caching...`);
    
    for (const caseId of offlineIds) {
        try {
            // Fetch full case document using the correct API endpoint
            const response = await fetch(`/api/case?case_id=${caseId}`);
            if (response.ok) {
                const caseDocument = await response.json();
                caseDocuments.push(caseDocument);
                console.log(`Fetched case document: ${caseId}`);
            } else {
                console.error(`Failed to fetch case ${caseId}: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            console.error(`Failed to fetch case ${caseId}:`, error);
        }
    }
    
    // Cache all documents as a single array
    if (caseDocuments.length > 0) {
        const cacheKey = 'mmria_offline_case_documents';
        
        if (cacheStorage) {
            // Store in Cache API as a single entry
            const response = new Response(JSON.stringify(caseDocuments));
            await cacheStorage.put(cacheKey, response);
            console.log(`Cached ${caseDocuments.length} case documents in Cache API`);
        } else {
            // Store in localStorage as a single entry
            localStorage.setItem(cacheKey, JSON.stringify(caseDocuments));
            console.log(`Cached ${caseDocuments.length} case documents in localStorage`);
        }
    }
    
    // Store the full case documents array in mmria_cached_cases
    localStorage.setItem('mmria_cached_cases', JSON.stringify(caseDocuments));
    
    return caseDocuments;
}

// Function to cache metadata and form definitions
async function cache_metadata() {
    const metadataResources = [
        '/api/metadata',
        '/api/metadata/version_specification',
        '/api/user_role_jurisdiction_view/my-roles'
    ];
    
    const cacheStorage = 'caches' in window ? await caches.open('mmria-metadata-v1') : null;
    
    for (const resource of metadataResources) {
        try {
            const response = await fetch(resource);
            if (response.ok) {
                if (cacheStorage) {
                    await cacheStorage.put(resource, response.clone());
                } else {
                    const content = await response.text();
                    localStorage.setItem(`mmria_meta_${resource.replace(/[^a-zA-Z0-9]/g, '_')}`, content);
                }
                console.log(`Cached metadata: ${resource}`);
            }
        } catch (error) {
            console.warn(`Failed to cache metadata ${resource}:`, error);
        }
    }
}
END OLD CACHE FUNCTIONS */

// Network connectivity management for Go Online button
let g_network_connected = navigator.onLine;

// Function to check network connectivity
async function check_network_connectivity() {
    console.log('Checking network connectivity...');
    
    // First check the navigator.onLine property
    if (!navigator.onLine) {
        console.log('Navigator indicates offline');
        return false;
    }

    try {
        // Try to make a lightweight request to check actual connectivity
        // Use our dedicated connectivity check endpoint that doesn't require database access
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 second timeout
        
        // Use the lightweight connectivity check endpoint
        const timestamp = Date.now();
        const response = await fetch(`/api/OfflineCase/connectivity-check?t=${timestamp}`, {
            method: 'GET',
            signal: controller.signal,
            cache: 'no-cache',
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache'
            }
        });
        
        clearTimeout(timeoutId);
        console.log('Network connectivity check response:', response.status);
        
        // The connectivity endpoint should always return 200 when the server is reachable
        const isConnected = response.ok && response.status === 200;
        
        if (isConnected) {
            console.log('Network connectivity confirmed - server is reachable');
        } else {
            console.log('Network connectivity check failed - server not reachable');
        }
        
        return isConnected;
        
    } catch (error) {
        console.log('Network connectivity check failed:', error.message);
        // If it's an AbortError, the request timed out
        if (error.name === 'AbortError') {
            console.log('Network connectivity check timed out');
        }
        return false;
    }
}// Function to update Go Online button state based on connectivity
function update_go_online_button_state(isConnected) {
    const goOnlineButton = document.getElementById('go-online-btn');
    if (!goOnlineButton) {
        return; // Button not found, might not be in offline mode
    }
    
    if (isConnected) {
        // Enable the button
        goOnlineButton.disabled = false;
        goOnlineButton.style.opacity = '1';
        goOnlineButton.style.cursor = 'pointer';
        goOnlineButton.title = 'Go back online and sync your changes';
        
        // Update button text to show connection is available
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Go Online';
        }
        
    } else {
        // Disable the button
        goOnlineButton.disabled = true;
        goOnlineButton.style.opacity = '0.6';
        goOnlineButton.style.cursor = 'not-allowed';
        goOnlineButton.title = 'Cannot go online - no network connection detected';
        
        // Update button text to show no connection
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Go Online';
        }
    }
    
    console.log(`Go Online button state updated: ${isConnected ? 'enabled' : 'disabled'}`);
}

// Function to handle network status changes
async function handle_network_status_change() {
    console.log('Network status change detected');
    const isConnected = await check_network_connectivity();
    g_network_connected = isConnected;
    update_go_online_button_state(isConnected);
    
    // Show a notification about the network status change
    if (isConnected) {
        show_message('Network connection restored. You can now go online.', 'success');
    } else {
        show_message('Network connection lost. Go Online button disabled.', 'warning');
    }
}

// Function to initialize network connectivity monitoring
function initialize_network_monitoring() {
    console.log('Initializing network connectivity monitoring...');
    
    // Set up event listeners for online/offline events
    window.addEventListener('online', handle_network_status_change);
    window.addEventListener('offline', handle_network_status_change);
    
    // Periodically check connectivity (every 30 seconds when offline)
    setInterval(async () => {
        if (!g_network_connected) {
            const isConnected = await check_network_connectivity();
            if (isConnected !== g_network_connected) {
                g_network_connected = isConnected;
                update_go_online_button_state(isConnected);
                if (isConnected) {
                    show_message('Network connection restored. You can now go online.', 'success');
                }
            }
        }
    }, 30000); // Check every 30 seconds
    
    // Initial connectivity check
    check_network_connectivity().then(isConnected => {
        g_network_connected = isConnected;
        update_go_online_button_state(isConnected);
    });
}

// Call network monitoring initialization on page load
document.addEventListener('DOMContentLoaded', () => {
    initialize_network_monitoring();
    check_network_connectivity();
});

// Function to get offline session data for offline login form
function get_offline_session_data() {
    // First try to get from global variable (if available)
    if (window.mmria_offline_session_data) {
        console.log('Retrieved offline session data from global variable');
        return window.mmria_offline_session_data;
    }
    
    // Fallback to localStorage
    try {
        const storedData = localStorage.getItem('mmria_offline_session');
        if (storedData) {
            const sessionData = JSON.parse(storedData);
            console.log('Retrieved offline session data from localStorage');
            
            // Cache in global variable for faster access
            window.mmria_offline_session_data = sessionData;
            
            return sessionData;
        }
    } catch (error) {
        console.error('Error parsing offline session data from localStorage:', error);
    }
    
    console.warn('No offline session data found');
    return null;
}

// Function to validate offline key against stored session data
function validate_offline_key_against_session(inputKey) {
    const sessionData = get_offline_session_data();
    
    if (!sessionData || !sessionData.offlineKey) {
        console.warn('No offline session data or key found for validation');
        return false;
    }
    
    const isValid = sessionData.offlineKey === inputKey;
    console.log('Offline key validation result:', isValid);
    
    return isValid;
}

// Function to check if user is in offline mode
function is_offline_mode() {
    return localStorage.getItem('is_offline') === 'true';
}

// Initialize offline session data on page load if in offline mode
document.addEventListener('DOMContentLoaded', () => {
    if (is_offline_mode()) {
        // Ensure offline session data is available globally
        const sessionData = get_offline_session_data();
        if (sessionData) {
            console.log('Offline mode detected, session data initialized');
            
            // Send to service worker if available
            if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                navigator.serviceWorker.controller.postMessage({
                    type: 'CACHE_OFFLINE_SESSION_DATA',
                    data: sessionData
                });
            }
        }
    }
});