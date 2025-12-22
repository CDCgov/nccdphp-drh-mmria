/**
 * Offline Case Manager Module
 * Manages offline case operations and status
 */

// Function to toggle offline status of a case
async function toggle_offline_status(caseId, caseIndex) {
    try {
        // Show loading state
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
            // Refresh case list on success
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
        } else if (result.already_in_state) {
            // Case is already offline - show modal to inform user
            console.log('Case is already in offline mode:', caseId);
            show_case_already_offline_modal();
        } else {
            throw new Error(result.message || 'Failed to toggle offline status');
        }
    } catch (error) {
        console.log('Error toggling offline status:', error);
        show_message('Error updating offline status: ' + error.message, 'error');
    } finally {
        // Restore button state
        if (button) {
            button.disabled = false;
        }
    }
}

// Function to remove a case from offline list (called from offline documents table)
async function remove_from_offline_list(caseId) {
    try {
        // Show loading state
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
            // Refresh case list on success
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
        } else if (result.already_in_state) {
            // Case is already online - show modal to inform user
            console.log('Case is already in online mode:', caseId);
            show_case_already_online_modal();      
        } else {
            throw new Error(result.message || 'Failed to remove case from offline list');
        }
    } catch (error) {
        console.error('Error removing case from offline list:', error);
        show_message('Error removing case from offline list: ' + error.message, 'error');
    }
}

// Function to get offline documents
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
            const response = await fetch(`/api/OfflineCase/active-user-session`, {
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

// Expose the offline case manager API to the global scope
window.OfflineCaseManager = {
    toggleStatus: toggle_offline_status,
    removeFromList: remove_from_offline_list,
    getDocuments: get_offline_documents,
    getCasesBySession: get_offline_cases_by_session
};

console.log('Offline Case Manager module loaded');
