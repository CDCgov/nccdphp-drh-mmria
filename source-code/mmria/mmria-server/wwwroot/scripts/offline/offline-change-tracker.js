/**
 * Offline Change Tracker Module
 * Manages tracking of document changes made in offline mode
 */

// Note: g_offline_changes and g_original_offline_documents are defined in app.mmria.js
// This module operates on those global variables

// Initialize offline change tracking with offline documents
function initialize_offline_change_tracking(offlineDocuments) { 
    // Only reload from localStorage if g_offline_changes is empty or uninitialized
    if (!g_offline_changes || g_offline_changes.size === 0) {
        // Load existing changes from localStorage
        const storedChanges = localStorage.getItem('mmria_offline_changes');
        if (storedChanges) {
            try {
                const changesArray = JSON.parse(storedChanges);
                g_offline_changes = new Map(changesArray);
            } catch (error) {
                offlineLog.error('OfflineChangeTracker', 'Error loading offline changes:', error);
                g_offline_changes = new Map();
            }
        } else {
            g_offline_changes = new Map();
        }
    }    
}

// Function to track changes to an offline document
async function track_offline_document_change(documentId, updatedDocument, changeDescription = '', changeStack = []) {
    // Check if we're in offline mode
    const isOffline = localStorage.getItem('is_offline') === 'true';
    if (!isOffline) {     
        return;
    }
    
    // Get the original document for comparison - try to find it in our stored originals
    let originalDoc = g_original_offline_documents.get(documentId);  
    // If not found, check if we already have a change record with the original document
    if (!originalDoc) {
        const existingChange = g_offline_changes.get(documentId);
        if (existingChange && existingChange.originalDocument) {
            originalDoc = existingChange.originalDocument;
            offlineLog.log('OfflineChangeTracker', 'Using original document from existing change record');
        }
    }
    
    // If still not found, fetch from cache and store it
    if (!originalDoc) {
        offlineLog.log('OfflineChangeTracker', 'Original document not found in memory, fetching from cache for:', documentId);
        return await fetchAndStoreOriginalDocument(documentId, updatedDocument, changeDescription, changeStack);
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
        offlineLog.warn('OfflineChangeTracker', 'Error getting session ID from localStorage:', error);
    }
    
    // Get existing change record to accumulate change stack items
    const existingChange = g_offline_changes.get(documentId);
    let accumulatedChangeStack = [];
    
    if (existingChange && existingChange.changeStackItems && Array.isArray(existingChange.changeStackItems)) {
        // Start with existing changes
        accumulatedChangeStack = [...existingChange.changeStackItems];
    }
    
    // Add new changes, avoiding duplicates by metadata_path (keep most recent)
    if (changeStack && Array.isArray(changeStack) && changeStack.length > 0) {
        for (const newItem of changeStack) {
            // Find if this field was already changed
            const existingIndex = accumulatedChangeStack.findIndex(
                item => item.metadata_path === newItem.metadata_path
            );
            
            if (existingIndex >= 0) {
                // Update existing entry with most recent change
                accumulatedChangeStack[existingIndex] = newItem;
            } else {
                // Add new change
                accumulatedChangeStack.push(newItem);
            }
        }
    }    
    
    // Create change record with accumulated change stack
    const changeRecord = {
        documentId: documentId,
        originalDocument: JSON.parse(JSON.stringify(originalDoc)), // Deep clone
        modifiedDocument: JSON.parse(JSON.stringify(updatedDocument)), // Deep clone
        timestamp: new Date().toISOString(),
        changeDescription: changeDescription,
        userId: g_user_name || 'unknown_user',
        sessionId: sessionId || 'unknown_session',
        changeStackItems: accumulatedChangeStack // Store accumulated field-level changes
    };
    
    // Store the change
    g_offline_changes.set(documentId, changeRecord);

    offlineLog.info('OfflineChangeTracker', 'Tracked offline document change', {
        documentId: documentId,
        changeDescription: changeDescription,
        changeCount: accumulatedChangeStack.length,
        changedFields: accumulatedChangeStack.map(item => item.metadata_path).filter(Boolean)
    });
    
    // Persist changes to localStorage
    save_offline_changes_to_storage();
    
    // Update the cached case document with the changes
    const cacheUpdated = await update_cached_case_document(documentId, updatedDocument);
    if (cacheUpdated !== true) {
        throw new Error(`Failed to update cached case document for ${documentId}`);
    }   
    
    return true;
}

// Helper function to fetch and store original document from cache
async function fetchAndStoreOriginalDocument(documentId, updatedDocument, changeDescription, changeStack = []) {
    try {
        
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
                    offlineLog.log('OfflineChangeTracker', 'Found cached original case in:', cacheName);
                    break;
                }
            }
        }
        
        if (cached_response) {
            const originalCaseData = await cached_response.json();
            
            // Store the original document
            g_original_offline_documents.set(documentId, JSON.parse(JSON.stringify(originalCaseData)));
            
            // Now track the change with the original document
            return await track_offline_document_change(documentId, updatedDocument, changeDescription, changeStack);
        } else {
            offlineLog.warn('OfflineChangeTracker', 'Could not find original document in cache for:', documentId);
            // Still track the change but without original document for comparison
            const changeRecord = {
                documentId: documentId,
                originalDocument: null, // No original for comparison
                modifiedDocument: JSON.parse(JSON.stringify(updatedDocument)),
                timestamp: new Date().toISOString(),
                changeDescription: changeDescription + ' (original document not available)',
                userId: g_user_name || 'unknown_user',
                sessionId: getSessionId(),
                changeStackItems: Array.isArray(changeStack) ? changeStack : []
            };
            
            g_offline_changes.set(documentId, changeRecord);
            save_offline_changes_to_storage();
            const cacheUpdated = await update_cached_case_document(documentId, updatedDocument);
            if (cacheUpdated !== true) {
                throw new Error(`Failed to update cached case document for ${documentId}`);
            }
            offlineLog.log('OfflineChangeTracker', 'Change tracked without original document:', documentId);
            return true;
        }
    } catch (error) {
        offlineLog.error('OfflineChangeTracker', 'Error fetching original document:', error);
        // Fallback: track change without original
        const changeRecord = {
            documentId: documentId,
            originalDocument: null,
            modifiedDocument: JSON.parse(JSON.stringify(updatedDocument)),
            timestamp: new Date().toISOString(),
            changeDescription: changeDescription + ' (fetch error)',
            userId: g_user_name || 'unknown_user',
            sessionId: getSessionId(),
            changeStackItems: Array.isArray(changeStack) ? changeStack : []
        };
        
        g_offline_changes.set(documentId, changeRecord);
        save_offline_changes_to_storage();
        const cacheUpdated = await update_cached_case_document(documentId, updatedDocument);
        if (cacheUpdated !== true) {
            throw new Error(`Failed to update cached case document for ${documentId}`);
        }
        offlineLog.log('OfflineChangeTracker', 'Change tracked with error fallback:', documentId);
        return true;
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
        offlineLog.warn('OfflineChangeTracker', 'Error getting session ID:', error);
    }
    return 'unknown_session';
}

// Function to save offline changes to localStorage
function save_offline_changes_to_storage() {
    try {
        // Convert Map to Array for storage
        const changesArray = Array.from(g_offline_changes.entries());
        localStorage.setItem('mmria_offline_changes', JSON.stringify(changesArray));
    } catch (error) {
        offlineLog.error('OfflineChangeTracker', 'Error saving offline changes to localStorage:', error);
    }
}

// Function to get all offline changes for syncing
function get_all_offline_changes() {
    const changes = Array.from(g_offline_changes.values());
    return changes;
}

// Function to clear offline changes (called after successful sync)
function clear_offline_changes() {
    g_offline_changes.clear();
    g_original_offline_documents.clear();
    localStorage.removeItem('mmria_offline_changes');
    offlineLog.log('OfflineChangeTracker', 'Offline changes cleared');
}

// Expose the offline change tracker API to the global scope
window.OfflineChangeTracker = {
    initialize: initialize_offline_change_tracking,
    track: track_offline_document_change,
    fetchAndStoreOriginal: fetchAndStoreOriginalDocument,
    getSessionId: getSessionId,
    save: save_offline_changes_to_storage,
    getAll: get_all_offline_changes,
    clear: clear_offline_changes
};


