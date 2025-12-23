/**
 * Offline Change Tracker Module
 * Manages tracking of document changes made in offline mode
 */

// Note: g_offline_changes and g_original_offline_documents are defined in app.mmria.js
// This module operates on those global variables

// Initialize offline change tracking with offline documents
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
async function track_offline_document_change(documentId, updatedDocument, changeDescription = '', changeStack = []) {
    console.log('📝 Tracking change for document:', documentId);
    console.log('📝 Current offline changes count:', g_offline_changes.size);
    console.log('📝 Current tracked documents:', Array.from(g_offline_changes.keys()));
    console.log('📝 Change stack items received:', changeStack.length);;
    
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
    
    // Get existing change record to accumulate change stack items
    const existingChange = g_offline_changes.get(documentId);
    let accumulatedChangeStack = [];
    
    if (existingChange && existingChange.changeStackItems && Array.isArray(existingChange.changeStackItems)) {
        // Start with existing changes
        accumulatedChangeStack = [...existingChange.changeStackItems];
        console.log('📝 Found existing change stack with', accumulatedChangeStack.length, 'items');
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
                console.log('📝 Updated existing change for:', newItem.metadata_path);
            } else {
                // Add new change
                accumulatedChangeStack.push(newItem);
                console.log('📝 Added new change for:', newItem.metadata_path);
            }
        }
    }
    
    console.log('📝 Total accumulated change stack items:', accumulatedChangeStack.length);
    
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
    
    // Persist changes to localStorage
    save_offline_changes_to_storage();
    
    // Update the cached case document with the changes
    try {
        await update_cached_case_document(documentId, updatedDocument);
    } catch (error) {
        console.error('Error updating cache:', error);
    }
    
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

// Function to get all offline changes for syncing
function get_all_offline_changes() {
    const changes = Array.from(g_offline_changes.values());
    console.log(`Retrieved ${changes.length} offline changes for processing`);
    return changes;
}

// Function to clear offline changes (called after successful sync)
function clear_offline_changes() {
    g_offline_changes.clear();
    g_original_offline_documents.clear();
    localStorage.removeItem('mmria_offline_changes');
    console.log('Offline changes cleared');
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

console.log('Offline Change Tracker module loaded');
