/**
 * Offline Session Validator Module
 * Validates offline keys and session data
 */

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

// Expose the offline session validator API to the global scope
// Helper function to get session data for validation
async function getSessionDataForValidation() {
    // Try service worker first
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        try {
            const sessionData = await requestSessionDataFromServiceWorker();
            if (sessionData) {
                return sessionData;
            }
        } catch (error) {
            console.warn('Failed to get session data from service worker:', error);
        }
    }
    
    // Fallback to global variable
    if (window.mmria_offline_session_data) {
        return window.mmria_offline_session_data;
    }
    
    // Last resort: localStorage
    try {
        const storedData = localStorage.getItem('mmria_offline_session');
        if (storedData) {
            return JSON.parse(storedData);
        }
    } catch (error) {
        console.warn('localStorage not available for session data:', error);
    }
    
    return null;
}

/**
 * Validates the current offline session
 * @returns {boolean} - Whether the offline session is valid
 */
function validateOfflineSession() {
    try {
        const sessionData = localStorage.getItem('mmria_offline_session');
        if (!sessionData) return false;
        
        const session = JSON.parse(sessionData);
        return session && session.user_id;
    } catch (error) {
        console.error('Error validating offline session:', error);
        return false;
    }
}

/**
 * Clears offline session state but preserves session data for re-login
 */
function clearOfflineSessionData() {
    localStorage.setItem('has_active_offline_session', 'false');
    
    // Clear all case data from localStorage for security
    try {
        const keysToRemove = [];
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && key.startsWith('case_')) {
                keysToRemove.push(key);
            }
        }
        
        // Remove all case-related keys
        keysToRemove.forEach(key => {
            localStorage.removeItem(key);
        });
        
        // Clear the case index as well
        localStorage.removeItem('case_index');
        
        console.log(`Cleared ${keysToRemove.length} case data items from localStorage on logout`);
    } catch (error) {
        console.error('Error clearing case data on logout:', error);
    }
    
    // Notify service worker of status change
    if (window.ServiceWorkerManager) {
        window.ServiceWorkerManager.notifyActiveOfflineSessionChange();
    }
}

/**
 * Logs offline events for audit purposes
 * @param {string} action - The action being performed
 * @param {string} message - Description of the action
 */
function logOfflineEvent(action, message) {
    try {
        const events = JSON.parse(localStorage.getItem('offline_audit_log') || '[]');
        const sessionData = JSON.parse(localStorage.getItem('mmria_offline_session') || '{}');
        
        events.push({
            action,
            message,
            timestamp: new Date().toISOString(),
            user: sessionData.user_id || 'unknown',
            sessionId: localStorage.getItem('offline_session_id') || 'unknown'
        });
        
        // Keep only last 100 events to prevent localStorage overflow
        if (events.length > 100) {
            events.splice(0, events.length - 100);
        }
        
        localStorage.setItem('offline_audit_log', JSON.stringify(events));
    } catch (error) {
        console.error('Error logging offline event:', error);
    }
}

/**
 * Checks if user has a valid offline session and redirects to login if not
 * Should be called on page load for protected routes
 */
function checkOfflineSessionAndRedirect() {
    const isOfflineMode = localStorage.getItem('is_offline') === 'true';
    const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
    
    if (isOfflineMode && !hasActiveSession) {
        console.log('Session validation failed: No active offline session, redirecting to offline login');
        
        // Log the event for audit purposes
        logOfflineEvent('session_invalid', 'User attempted to access protected route without valid session');
        
        // Clear any potentially stale data
        clearOfflineSessionData();
        
        // Redirect to offline login
        window.location.href = '/Account/OfflineLogin';
        return false; // Session invalid
    }
    
    return true; // Session valid or not in offline mode
}

window.OfflineSessionValidator = {
    validateKey: validate_offline_key,
    getSessionData: get_offline_session_data,
    validateKeyAgainstSession: validate_offline_key_against_session,
    isOfflineMode: is_offline_mode,
    getSessionDataForValidation: getSessionDataForValidation,
    validateOfflineSession: validateOfflineSession,
    clearOfflineSessionData: clearOfflineSessionData,
    logOfflineEvent: logOfflineEvent,
    checkOfflineSessionAndRedirect: checkOfflineSessionAndRedirect
};

console.log('Offline Session Validator module loaded');
