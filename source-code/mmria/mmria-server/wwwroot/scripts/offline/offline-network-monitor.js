/**
 * Offline Network Monitor Module
 * Monitors network connectivity for offline mode
 */

// Note: g_network_connected is defined in app.mmria.js
// This module operates on that global variable

// Function to check network connectivity
async function check_network_connectivity() {   
    
    if (!navigator.onLine) {
        offlineLog.log('OfflineNetworkMonitor', 'Navigator indicates offline');
        return false;
    }

    try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000);
        
        const timestamp = Date.now();
        const response = await fetch(`/api/OfflineCase/connectivity-check?t=${timestamp}`, {
            method: 'GET',
            signal: controller.signal,
            cache: 'no-cache',
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache'
            }
        });
        
        clearTimeout(timeoutId);
        
        const isConnected = response.ok && response.status === 200;       
       
        if (!isConnected) {
            offlineLog.log('OfflineNetworkMonitor', 'Navigator indicates offline');     
        }

        return isConnected;
        
    } catch (error) {
        offlineLog.log('OfflineNetworkMonitor', 'Network connectivity check failed:', error.message);
        if (error.name === 'AbortError') {
            offlineLog.log('OfflineNetworkMonitor', 'Network connectivity check timed out');
        }
        return false;
    }
}

// Track last known button state to avoid redundant logging
let lastButtonState = null;

// Function to update Go Online button state based on connectivity
function update_go_online_button_state(isConnected) {
    const goOnlineButton = document.getElementById('go-online-btn');
    if (!goOnlineButton) {
        return;
    }
    
    if (isConnected) {
        goOnlineButton.disabled = false;
        goOnlineButton.style.opacity = '1';
        goOnlineButton.style.cursor = 'pointer';
        goOnlineButton.title = 'Go back online and sync your changes';
        
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Go Online & Sync Changes';
        }
        
    } else {
        goOnlineButton.disabled = true;
        goOnlineButton.style.opacity = '0.6';
        goOnlineButton.style.cursor = 'not-allowed';
        goOnlineButton.title = 'Cannot go online - no network connection detected';
        
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Go Online & Sync Changes';
        }
    }
    
    // Only log when state actually changes
    if (lastButtonState !== isConnected) {
        offlineLog.log('OfflineNetworkMonitor', `Go Online button state changed: ${isConnected ? 'enabled' : 'disabled'}`);
        lastButtonState = isConnected;
    }
}

// Function to handle network status changes
async function handle_network_status_change() {
    const isConnected = await check_network_connectivity();
    g_network_connected = isConnected;
    
    update_go_online_button_state(isConnected);
    
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        try {
            navigator.serviceWorker.controller.postMessage({
                type: 'NETWORK_STATUS_CHANGE',
                isOnline: isConnected
            });     
        } catch (error) {
            offlineLog.warn('OfflineNetworkMonitor', 'Failed to notify service worker of network status change:', error);
        }
    }  
}

// Function to initialize network connectivity monitoring
function initialize_network_monitoring() {    
    window.addEventListener('online', handle_network_status_change);
    window.addEventListener('offline', handle_network_status_change);
    
    setInterval(async () => {
        if (!g_network_connected) {
            const isConnected = await check_network_connectivity();
            if (isConnected !== g_network_connected) {
                g_network_connected = isConnected;
                update_go_online_button_state(isConnected);
            }
        }
    }, 5000);
    
    check_network_connectivity().then(isConnected => {
        g_network_connected = isConnected;
        update_go_online_button_state(isConnected);
    });
}

// Initialize on DOM load
document.addEventListener('DOMContentLoaded', () => {
    if(window.OfflineStatus.isOffline()){
        initialize_network_monitoring();
        check_network_connectivity();
    }
});

// Add network status monitoring for service worker coordination (case page specific)
function handle_network_status_change_case() {
    const isOnline = navigator.onLine;
    
    // Notify service worker about network status change
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        try {
            navigator.serviceWorker.controller.postMessage({
                type: 'NETWORK_STATUS_CHANGE',
                isOnline: isOnline
            });          
        } catch (error) {
            offlineLog.warn('OfflineNetworkMonitor', 'Case page: Failed to notify service worker of network status change:', error);
        }
    }
}

// Set up network monitoring for case pages
function setup_case_page_network_monitoring() {
    if (typeof window !== 'undefined') {
        window.addEventListener('online', handle_network_status_change_case);
        window.addEventListener('offline', handle_network_status_change_case);
    }
}

// Expose the offline network monitor API to the global scope
window.OfflineNetworkMonitor = {
    check: check_network_connectivity,
    updateGoOnlineButtonState: update_go_online_button_state,
    handleStatusChange: handle_network_status_change,
    initialize: initialize_network_monitoring,
    handleStatusChangeCase: handle_network_status_change_case,
    setupCasePageMonitoring: setup_case_page_network_monitoring
};

// Make functions globally accessible for backward compatibility
window.handle_network_status_change_case = handle_network_status_change_case;

