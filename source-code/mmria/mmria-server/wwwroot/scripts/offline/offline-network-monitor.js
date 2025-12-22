/**
 * Offline Network Monitor Module
 * Monitors network connectivity for offline mode
 */

// Note: g_network_connected is defined in app.mmria.js
// This module operates on that global variable

// Function to check network connectivity
async function check_network_connectivity() {
    console.log('Checking network connectivity...');
    
    if (!navigator.onLine) {
        console.log('Navigator indicates offline');
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
        console.log('Network connectivity check response:', response.status);
        
        const isConnected = response.ok && response.status === 200;
        
        if (isConnected) {
            console.log('Network connectivity confirmed - server is reachable');
        } else {
            console.log('Network connectivity check failed - server not reachable');
        }
        
        return isConnected;
        
    } catch (error) {
        console.log('Network connectivity check failed:', error.message);
        if (error.name === 'AbortError') {
            console.log('Network connectivity check timed out');
        }
        return false;
    }
}

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
            buttonText.textContent = 'Go Online';
        }
        
    } else {
        goOnlineButton.disabled = true;
        goOnlineButton.style.opacity = '0.6';
        goOnlineButton.style.cursor = 'not-allowed';
        goOnlineButton.title = 'Cannot go online - no network connection detected';
        
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Go Online';
        }
    }
    
    console.log(`Go Online button state updated: ${isConnected ? 'enabled' : 'disabled'}`);
}

// Function to handle network status changes
async function handle_network_status_change() {
    console.log('Network status change detected');
    const isConnected = await check_network_connectivity();
    g_network_connected = isConnected;
    update_go_online_button_state(isConnected);
    
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        try {
            navigator.serviceWorker.controller.postMessage({
                type: 'NETWORK_STATUS_CHANGE',
                isOnline: isConnected
            });
            console.log('Notified service worker of network status change:', isConnected);
        } catch (error) {
            console.warn('Failed to notify service worker of network status change:', error);
        }
    }
    
    if (isConnected) {
        show_message('Network connection restored. You can now go online.', 'success');
    } else {
        show_message('Network connection lost. Go Online button disabled.', 'warning');
    }
}

// Function to initialize network connectivity monitoring
function initialize_network_monitoring() {
    console.log('Initializing network connectivity monitoring...');
    
    window.addEventListener('online', handle_network_status_change);
    window.addEventListener('offline', handle_network_status_change);
    
    setInterval(async () => {
        if (!g_network_connected) {
            const isConnected = await check_network_connectivity();
            if (isConnected !== g_network_connected) {
                g_network_connected = isConnected;
                update_go_online_button_state(isConnected);
                if (isConnected) {
                    show_message('Network connection restored. You can now go online.', 'success');
                }
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
    initialize_network_monitoring();
    check_network_connectivity();
});

// Expose the offline network monitor API to the global scope
window.OfflineNetworkMonitor = {
    check: check_network_connectivity,
    updateGoOnlineButtonState: update_go_online_button_state,
    handleStatusChange: handle_network_status_change,
    initialize: initialize_network_monitoring
};

console.log('Offline Network Monitor module loaded');
