/**
 * Offline Case Manager Module
 * Manages offline case lists and document operations
 */

// Function to toggle offline status for a case
async function toggle_offline_status(caseId, makeOffline) {
    console.log(`Toggling offline status for case ${caseId} to ${makeOffline ? 'offline' : 'online'}`);
    
    try {
        const response = await fetch(`/api/OfflineCase/toggle/${caseId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ makeOffline: makeOffline })
        });
        
        if (!response.ok) {
            throw new Error(`Failed to toggle offline status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('Offline status toggled successfully:', result);
        
        show_message(`Case ${makeOffline ? 'added to' : 'removed from'} offline list`, 'success');
        
        return result;
        
    } catch (error) {
        console.error('Error toggling offline status:', error);
        show_message(`Error toggling offline status: ${error.message}`, 'error');
        throw error;
    }
}

// Function to remove case from offline list
async function remove_from_offline_list(caseId) {
    return await toggle_offline_status(caseId, false);
}

// Function to get offline documents for the current session
async function get_offline_documents() {
    console.log('Fetching offline documents...');
    
    try {
        const offlineSession = localStorage.getItem('mmria_offline_session');
        
        if (!offlineSession) {
            console.log('No offline session found');
            return [];
        }
        
        let sessionId;
        try {
            const sessionData = JSON.parse(offlineSession);
            sessionId = sessionData.offlineSessionId || sessionData.sessionId;
        } catch (error) {
            sessionId = offlineSession;
        }
        
        const response = await fetch(`/api/OfflineCase/session/${sessionId}`);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch offline documents: ${response.status}`);
        }
        
        const documents = await response.json();
        console.log(`Fetched ${documents.length} offline documents`);
        
        return documents;
        
    } catch (error) {
        console.error('Error fetching offline documents:', error);
        return [];
    }
}

// Function to get offline cases by session
async function get_offline_cases_by_session(sessionId) {
    console.log('Fetching offline cases for session:', sessionId);
    
    // Validate sessionId
    if (!sessionId || sessionId === '') {
        console.warn('No valid session ID provided to get_offline_cases_by_session');
        return { offline_ids: [], case_documents: [] };
    }
    
    try {
        const response = await fetch(`/api/OfflineCase/by-session/${sessionId}`);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch offline cases: ${response.status}`);
        }
        
        const sessionData = await response.json();
        console.log(`Fetched ${sessionData?.case_documents?.length || 0} offline cases`);
        
        return sessionData || { offline_ids: [], case_documents: [] };
        
    } catch (error) {
        console.error('Error fetching offline cases:', error);
        return { offline_ids: [], case_documents: [] };
    }
}

// Expose the offline case manager API to the global scope
window.OfflineCaseManager = {
    toggleStatus: toggle_offline_status,
    removeFromList: remove_from_offline_list,
    getDocuments: get_offline_documents,
    getCasesBySession: get_offline_cases_by_session
};

console.log('Offline Case Manager module loaded');
