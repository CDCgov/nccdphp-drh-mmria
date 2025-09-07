// Service Worker Manager for MMRIA
// This file provides helper functions for managing the service worker lifecycle
// and communicating between the main thread and service worker.

console.log('Service Worker Manager loaded');

// Helper object for service worker management
window.ServiceWorkerManager = {
    
    // Check if service worker is supported
    isSupported: function() {
        return 'serviceWorker' in navigator;
    },
    
    // Get the current service worker registration
    getRegistration: async function() {
        if (!this.isSupported()) return null;
        
        try {
            return await navigator.serviceWorker.getRegistration();
        } catch (error) {
            console.error('Error getting service worker registration:', error);
            return null;
        }
    },
    
    // Check if service worker is active
    isActive: async function() {
        const registration = await this.getRegistration();
        return registration && registration.active;
    },
    
    // Send message to service worker
    sendMessage: function(message) {
        if (!navigator.serviceWorker.controller) {
            console.warn('No service worker controller available');
            return;
        }
        
        navigator.serviceWorker.controller.postMessage(message);
    },
    
    // Get cache status from service worker
    getCacheStatus: async function() {
        return new Promise((resolve) => {
            if (!navigator.serviceWorker.controller) {
                resolve({});
                return;
            }
            
            const messageChannel = new MessageChannel();
            
            messageChannel.port1.onmessage = function(event) {
                resolve(event.data);
            };
            
            navigator.serviceWorker.controller.postMessage(
                { type: 'GET_CACHE_STATUS' },
                [messageChannel.port2]
            );
            
            // Timeout after 5 seconds
            setTimeout(() => resolve({}), 5000);
        });
    },
    
    // Clear all caches through service worker
    clearCaches: function() {
        this.sendMessage({ type: 'CLEAR_CACHES' });
    },
    
    // Check offline status
    checkOfflineStatus: function() {
        const isOffline = localStorage.getItem('is_offline') === 'true';
        console.log('Service Worker Manager: Offline status =', isOffline);
        return isOffline;
    }
};

// Set up service worker message listener
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.addEventListener('message', function(event) {
        const { type, data } = event.data;
        
        switch (type) {
            case 'CHECK_OFFLINE_STATUS':
                // Service worker is asking for offline status
                const isOffline = ServiceWorkerManager.checkOfflineStatus();
                event.source.postMessage({
                    type: 'OFFLINE_STATUS_RESPONSE',
                    isOffline: isOffline
                });
                break;
                
            default:
                console.log('Service Worker Manager received message:', event.data);
        }
    });
}

// Make sure this doesn't interfere with existing offline functionality
console.log('Service Worker Manager initialized successfully');
