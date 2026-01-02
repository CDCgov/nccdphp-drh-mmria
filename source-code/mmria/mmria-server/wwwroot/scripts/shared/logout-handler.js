/**
 * Logout Handler for MMRIA Application
 * Handles conditional logout behavior for online vs offline modes
 */

/**
 * Main logout handler function that intercepts form submission
 * @param {Event} event - The form submit event
 * @returns {boolean} - Whether to proceed with form submission
 */

async function encryptCasesOnOfflineLogout(enteredKey) {
    try {
        if (!('serviceWorker' in navigator) || !navigator.serviceWorker.controller) {
            return;
        }

        const sessionData = await getSessionDataForValidation();
        if (!sessionData || !sessionData.keySalt) return;

        // Send password to service worker to derive and set key
        const keySet = await ServiceWorkerManager.setOfflineKey(enteredKey, sessionData.keySalt);
        if (!keySet) return;

        const registration = await navigator.serviceWorker.ready;

        await new Promise(resolve => {
            const messageChannel = new MessageChannel();
            messageChannel.port1.onmessage = () => resolve();
            registration.active.postMessage(
                { type: 'OFFLINE_LOGOUT_ENCRYPT_CASES' },
                [messageChannel.port2]
            );
        });

        offlineLog.log('LogoutHandler', 'Offline cached cases encrypted and key dropped in service worker');
    } catch (err) {
        offlineLog.error('LogoutHandler', 'Error encrypting cases on offline logout:', err);
    }
}

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
            offlineLog.warn('LogoutHandler', 'Failed to get session data from service worker:', error);
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
        offlineLog.warn('LogoutHandler', 'localStorage not available for session data:', error);
    }
    
    return null;
}

async function handleLogout(event) {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline) {
        // Prevent the form from submitting to server
        event.preventDefault();
        
        // Validate offline session before logout
        if (validateOfflineSession()) {
            // Log the logout event for audit purposes
            offlineLog.log('LogoutHandler', 'logout: User logged out in offline mode');
            
            // Show a brief message before redirecting
            //showLogoutMessage('Logging out of offline mode...');
        }
        
        // Encrypt cached cases before clearing data
        //await encryptCasesOnOfflineLogout("sssDDDkkk@@@2d");    

        // Clear all offline data securely
        await clearOfflineSessionData();
        
        // Small delay to show message, then redirect
        setTimeout(() => {
            window.location.href = '/Account/OfflineLogin';
        }, 800);
        
        return false;
    }
    
    // Online mode - proceed with normal server logout
    return true;
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
        offlineLog.error('LogoutHandler', 'Error validating offline session:', error);
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
        
        offlineLog.log('LogoutHandler', `Cleared ${keysToRemove.length} case data items from localStorage on logout`);
    } catch (error) {
        offlineLog.error('LogoutHandler', 'Error clearing case data on logout:', error);
    }
    
    // Notify service worker of status change
    if (window.ServiceWorkerManager) {
        window.ServiceWorkerManager.notifyActiveOfflineSessionChange();
    }
}

/**
 * Shows a logout message to the user
 * @param {string} message - Message to display
 */
function showLogoutMessage(message) {
    // Create a simple toast notification
    const toast = document.createElement('div');
    toast.className = 'alert alert-info alert-dismissible fade show';
    toast.style.position = 'fixed';
    toast.style.top = '20px';
    toast.style.right = '20px';
    toast.style.zIndex = '9999';
    toast.style.minWidth = '300px';
    
    toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    document.body.appendChild(toast);
    
    // Auto-remove after 3 seconds
    setTimeout(() => {
        if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
        }
    }, 3000);
}

/**
 * Checks if user has a valid offline session and redirects to login if not
 * Should be called on page load for protected routes
 */
function checkOfflineSessionAndRedirect() {
    const isOfflineMode = localStorage.getItem('is_offline') === 'true';
    const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
    
    if (isOfflineMode && !hasActiveSession) {
        offlineLog.log('LogoutHandler', 'Session validation failed: No active offline session, redirecting to offline login');
        
        // Log the event for audit purposes
        offlineLog.log('LogoutHandler', 'session_invalid: User attempted to access protected route without valid session');
        
        // Clear any potentially stale data
        clearOfflineSessionData();
        
        // Redirect to offline login
        window.location.href = '/Account/OfflineLogin';
        return false; // Session invalid
    }
    
    return true; // Session valid or not in offline mode
}

/**
 * Initialize logout handlers and session validation when DOM is ready
 * This provides an alternative to inline onsubmit handlers
 */
document.addEventListener('DOMContentLoaded', function() {
    // Find all logout forms and add event listeners
    const logoutForms = document.querySelectorAll('form[action="/Account/Logout"]');
    
    logoutForms.forEach(form => {
        // Check if form already has an onsubmit handler to avoid double-binding
        if (!form.onsubmit) {
            form.addEventListener('submit', handleLogout);
        }
    });
    
    // Perform session validation on page load for case-related pages
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath.includes('/case') || currentPath.includes('/home')) {
        offlineLog.log('LogoutHandler', 'Protected route detected, validating offline session...');
        checkOfflineSessionAndRedirect();
    }
});

// Make functions globally available
window.handleLogout = handleLogout;
window.clearOfflineSessionData = clearOfflineSessionData;
window.validateOfflineSession = validateOfflineSession;
window.checkOfflineSessionAndRedirect = checkOfflineSessionAndRedirect;