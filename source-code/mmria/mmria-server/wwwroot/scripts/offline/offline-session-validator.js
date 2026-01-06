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
        offlineLog.log('OfflineSessionValidator', 'Retrieved offline session data from global variable');
        return window.mmria_offline_session_data;
    }
    
    // Fallback to localStorage
    try {
        const storedData = localStorage.getItem('mmria_offline_session');
        if (storedData) {
            const sessionData = JSON.parse(storedData);
            offlineLog.log('OfflineSessionValidator', 'Retrieved offline session data from localStorage');
            
            // Cache in global variable for faster access
            window.mmria_offline_session_data = sessionData;
            
            return sessionData;
        }
    } catch (error) {
        offlineLog.error('OfflineSessionValidator', 'Error parsing offline session data from localStorage:', error);
    }
    
    offlineLog.warn('OfflineSessionValidator', 'No offline session data found');
    return null;
}

// Function to validate offline key against stored session data
function validate_offline_key_against_session(inputKey) {
    const sessionData = get_offline_session_data();
    
    if (!sessionData || !sessionData.offlineKey) {
        offlineLog.warn('OfflineSessionValidator', 'No offline session data or key found for validation');
        return false;
    }
    
    const isValid = sessionData.offlineKey === inputKey;
    offlineLog.log('OfflineSessionValidator', 'Offline key validation result:', isValid);
    
    return isValid;
}

// Function to check if user is in offline mode
function is_offline_mode() {
    return localStorage.getItem('is_offline') === 'true';
}

// Expose the offline session validator API to the global scope
window.OfflineSessionValidator = {
    validateKey: validate_offline_key,
    getSessionData: get_offline_session_data,
    validateKeyAgainstSession: validate_offline_key_against_session,
    isOfflineMode: is_offline_mode
};


