/**
 * Offline Change Tracker Module
 * Manages tracking of document changes made in offline mode
 */

// Note: g_offline_changes and g_original_offline_documents are defined in app.mmria.js
// This module operates on those global variables

// Function to initialize the offline change tracking system
function initialize_offline_change_tracking() {
    console.log('Initializing offline change tracking system...');
    
    // Try to load existing offline changes from localStorage
    try {
        const storedChanges = localStorage.getItem('mmria_offline_changes');
        const storedOriginals = localStorage.getItem('mmria_original_offline_documents');
        
        if (storedChanges) {
            const changesArray = JSON.parse(storedChanges);
            g_offline_changes = new Map(changesArray.map(item => [item.documentId, item]));
            console.log(`Loaded ${g_offline_changes.size} tracked document changes from localStorage`);
        }
        
        if (storedOriginals) {
            const originalsArray = JSON.parse(storedOriginals);
            g_original_offline_documents = new Map(originalsArray.map(item => [item.id, item]));
            console.log(`Loaded ${g_original_offline_documents.size} original document snapshots from localStorage`);
        }
        
        // Debug output
        if (g_offline_changes.size > 0) {
            console.log('Tracked documents:', Array.from(g_offline_changes.keys()));
        }
        
    } catch (error) {
        console.error('Error loading offline changes from localStorage:', error);
        g_offline_changes = new Map();
        g_original_offline_documents = new Map();
    }
    
    console.log('Offline change tracking initialized successfully');
}

// Function to track a document change
function track_offline_document_change(documentId, modifiedDocument, changeDescription = '', userId = '', changeStackItems = []) {
    console.log('Tracking offline document change for:', documentId);
    console.log('Modified document state:', modifiedDocument);
    console.log('Change description:', changeDescription);
    console.log('Change stack items count:', changeStackItems.length);
    
    try {
        // Get the offline session ID
        const offlineSession = localStorage.getItem('mmria_offline_session');
        let sessionId = null;
        
        if (offlineSession) {
            try {
                const sessionData = JSON.parse(offlineSession);
                sessionId = sessionData.offlineSessionId || sessionData.sessionId;
            } catch (parseError) {
                console.warn('Error parsing offline session data:', parseError);
                sessionId = offlineSession; // Use as string if parsing fails
            }
        }
        
        if (!sessionId) {
            console.warn('No offline session ID found - changes may not sync properly');
        }
        
        // Get existing change record or create new one
        let changeRecord = g_offline_changes.get(documentId);
        
        if (!changeRecord) {
            // First change for this document - initialize change record
            console.log('First change for document, creating new change record');
            
            // Get original document snapshot if not already stored
            let originalDocument = g_original_offline_documents.get(documentId);
            
            if (!originalDocument) {
                console.log('No original document snapshot found - fetching from cache or API');
                // This is handled by the calling code which should call fetchAndStoreOriginalDocument
                // For now, we'll use the modified document as the "original" baseline
                // This can happen if the document was newly created in offline mode
                originalDocument = JSON.parse(JSON.stringify(modifiedDocument));
                g_original_offline_documents.set(documentId, originalDocument);
            }
            
            changeRecord = {
                documentId: documentId,
                originalDocument: originalDocument,
                modifiedDocument: modifiedDocument,
                timestamp: new Date().toISOString(),
                changeDescription: changeDescription,
                syncState: 0, // 0 = not synced, 1 = synced, 2 = abandoned, 3 = error
                userId: userId || g_user_name || 'unknown',
                sessionId: sessionId,
                changeStackItems: changeStackItems || [],
                firstChangedAt: new Date().toISOString(),
                lastChangedAt: new Date().toISOString(),
                changeCount: 1
            };
        } else {
            // Update existing change record
            console.log('Updating existing change record');
            changeRecord.modifiedDocument = modifiedDocument;
            changeRecord.lastChangedAt = new Date().toISOString();
            changeRecord.changeCount = (changeRecord.changeCount || 1) + 1;
            
            // Append change description if provided
            if (changeDescription) {
                if (changeRecord.changeDescription) {
                    changeRecord.changeDescription += '\n' + changeDescription;
                } else {
                    changeRecord.changeDescription = changeDescription;
                }
            }
            
            // Merge change stack items
            if (changeStackItems && changeStackItems.length > 0) {
                if (!changeRecord.changeStackItems) {
                    changeRecord.changeStackItems = [];
                }
                changeRecord.changeStackItems = changeRecord.changeStackItems.concat(changeStackItems);
            }
        }
        
        // Store the updated change record
        g_offline_changes.set(documentId, changeRecord);
        console.log(`Change tracked successfully. Total tracked changes: ${g_offline_changes.size}`);
        
        // Persist to localStorage for durability
        save_offline_changes_to_storage();
        
        return changeRecord;
        
    } catch (error) {
        console.error('Error tracking offline document change:', error);
        throw error;
    }
}

// Function to fetch and store original document for change tracking
async function fetchAndStoreOriginalDocument(documentId) {
    console.log('Fetching original document for tracking:', documentId);
    
    try {
        // Check if we already have the original
        if (g_original_offline_documents.has(documentId)) {
            console.log('Original document already stored');
            return g_original_offline_documents.get(documentId);
        }
        
        // Try to get from cache first
        const cacheKey = `mmria-api-cache-v1`;
        if ('caches' in window) {
            const cache = await caches.open(cacheKey);
            const cacheUrl = `/api/case/${documentId}`;
            const cachedResponse = await cache.match(cacheUrl);
            
            if (cachedResponse) {
                const originalDocument = await cachedResponse.json();
                console.log('Original document retrieved from cache');
                g_original_offline_documents.set(documentId, originalDocument);
                save_offline_changes_to_storage(); // Persist originals
                return originalDocument;
            }
        }
        
        // If not in cache and we're online, fetch from API
        if (navigator.onLine) {
            console.log('Fetching original document from API');
            const response = await fetch(`/api/case/${documentId}`);
            
            if (response.ok) {
                const originalDocument = await response.json();
                console.log('Original document fetched successfully from API');
                g_original_offline_documents.set(documentId, originalDocument);
                save_offline_changes_to_storage(); // Persist originals
                return originalDocument;
            } else {
                console.warn('Failed to fetch original document from API:', response.status);
            }
        }
        
        console.warn('Could not retrieve original document - not in cache and offline/API unavailable');
        return null;
        
    } catch (error) {
        console.error('Error fetching original document:', error);
        return null;
    }
}

// Helper function to get session ID from localStorage
function getSessionId() {
    try {
        const offlineSession = localStorage.getItem('mmria_offline_session');
        if (!offlineSession) return null;
        
        const sessionData = JSON.parse(offlineSession);
        return sessionData.offlineSessionId || sessionData.sessionId || null;
    } catch (error) {
        console.warn('Error parsing offline session:', error);
        return null;
    }
}

// Function to save offline changes to localStorage for persistence
function save_offline_changes_to_storage() {
    try {
        // Convert Map to Array for JSON serialization
        const changesArray = Array.from(g_offline_changes.values());
        const originalsArray = Array.from(g_original_offline_documents.values());
        
        // Save to localStorage
        localStorage.setItem('mmria_offline_changes', JSON.stringify(changesArray));
        localStorage.setItem('mmria_original_offline_documents', JSON.stringify(originalsArray));
        
        console.log(`Saved ${changesArray.length} changes and ${originalsArray.length} originals to localStorage`);
        
    } catch (error) {
        console.error('Error saving offline changes to localStorage:', error);
        
        // Check if quota exceeded
        if (error.name === 'QuotaExceededError') {
            console.error('localStorage quota exceeded - cannot save offline changes');
            alert('Warning: Storage limit reached. Some offline changes may not be saved. Please go online and sync soon.');
        }
    }
}

// Function to get all offline changes for syncing
function get_all_offline_changes() {
    const changes = Array.from(g_offline_changes.values());
    console.log(`Retrieved ${changes.length} offline changes for processing`);
    return changes;
}

// Function to clear all offline changes after successful sync
function clear_offline_changes() {
    console.log('Clearing all offline changes...');
    
    g_offline_changes.clear();
    g_original_offline_documents.clear();
    
    // Remove from localStorage
    localStorage.removeItem('mmria_offline_changes');
    localStorage.removeItem('mmria_original_offline_documents');
    
    console.log('All offline changes cleared successfully');
}

// Expose the offline change tracker API to the global scope
window.OfflineChangeTracker = {
    initialize: initialize_offline_change_tracking,
    track: track_offline_document_change,
    fetchOriginal: fetchAndStoreOriginalDocument,
    getSessionId: getSessionId,
    save: save_offline_changes_to_storage,
    getAll: get_all_offline_changes,
    clear: clear_offline_changes
};

console.log('Offline Change Tracker module loaded');
