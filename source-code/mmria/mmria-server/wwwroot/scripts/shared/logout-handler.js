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

        const sessionData = await window.OfflineSessionValidator.getSessionDataForValidation();
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

async function handleLogout(event) {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline) {
        // Prevent the form from submitting to server
        event.preventDefault();
        
        // Validate offline session before logout
        if (window.OfflineSessionValidator.validateOfflineSession()) {
            // Log the logout event for audit purposes
            window.OfflineSessionValidator.logOfflineEvent('logout', 'User logged out in offline mode');
            
            // Show a brief message before redirecting
            //showLogoutMessage('Logging out of offline mode...');
        }
        
        // Encrypt cached cases before clearing data
        //await encryptCasesOnOfflineLogout("sssDDDkkk@@@2d");    

        // Clear all offline data securely
        await window.OfflineSessionValidator.clearOfflineSessionData();
        
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
        window.OfflineSessionValidator.checkOfflineSessionAndRedirect();
    }
});

// Make functions globally available
window.handleLogout = handleLogout;
window.clearOfflineSessionData = window.OfflineSessionValidator.clearOfflineSessionData;
window.validateOfflineSession = window.OfflineSessionValidator.validateOfflineSession;
window.checkOfflineSessionAndRedirect = window.OfflineSessionValidator.checkOfflineSessionAndRedirect;