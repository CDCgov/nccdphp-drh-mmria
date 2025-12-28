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

        console.log('Offline cached cases encrypted and key dropped in service worker');
    } catch (err) {
        console.error('Error encrypting cases on offline logout:', err);
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

async function handleLogout(event) {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline) {
        // Prevent the form from submitting to server
        event.preventDefault();
        
        // Validate offline session before logout
        if (validateOfflineSession()) {
            // Log the logout event for audit purposes
            logOfflineEvent('logout', 'User logged out in offline mode');
            
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
        
        // Keep only last 100 events to prevent localStorage bloat
        if (events.length > 100) {
            events.splice(0, events.length - 100);
        }
        
        localStorage.setItem('offline_audit_log', JSON.stringify(events));
    } catch (error) {
        console.error('Error logging offline event:', error);
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
    
    console.log(`Logout handler initialized for ${logoutForms.length} form(s)`);
    
    // Perform session validation on page load for case-related pages
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath.includes('/case') || currentPath.includes('/home')) {
        console.log('Protected route detected, validating offline session...');
        checkOfflineSessionAndRedirect();
    }
});

// Make functions globally available
window.handleLogout = handleLogout;
window.clearOfflineSessionData = clearOfflineSessionData;
window.validateOfflineSession = validateOfflineSession;
window.checkOfflineSessionAndRedirect = checkOfflineSessionAndRedirect;