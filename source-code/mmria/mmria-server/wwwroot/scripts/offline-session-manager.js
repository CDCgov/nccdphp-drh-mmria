// Offline Session Manager
// Manages offline session initialization and cache management

class OfflineSessionManager {
    constructor() {
        this.currentSessionId = null;
        this.isInitialized = false;
    }

    // Initialize a new offline session
    async initializeOfflineSession() {
        try {
            console.log('OfflineSessionManager: Initializing new offline session...');
            
            // Generate unique session ID
            const sessionId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
            
            // Check if service worker is available
            if (!('serviceWorker' in navigator)) {
                throw new Error('Service Worker not supported');
            }

            const registration = await navigator.serviceWorker.ready;
            if (!registration.active) {
                throw new Error('No active service worker found');
            }

            // Send initialization message to service worker
            const messageChannel = new MessageChannel();
            
            return new Promise((resolve, reject) => {
                messageChannel.port1.onmessage = (event) => {
                    const response = event.data;
                    if (response.success) {
                        this.currentSessionId = response.sessionId;
                        this.isInitialized = true;
                        console.log('OfflineSessionManager: Session initialized successfully:', {
                            sessionId: this.currentSessionId,
                            cacheNames: response.cacheNames
                        });
                        resolve(response);
                    } else {
                        console.error('OfflineSessionManager: Session initialization failed:', response.error);
                        reject(new Error(response.error || 'Session initialization failed'));
                    }
                };

                // Send initialization message
                registration.active.postMessage({
                    type: 'INIT_OFFLINE_SESSION',
                    data: { sessionId }
                }, [messageChannel.port2]);

                // Timeout after 10 seconds
                setTimeout(() => {
                    reject(new Error('Session initialization timed out'));
                }, 10000);
            });

        } catch (error) {
            console.error('OfflineSessionManager: Error initializing offline session:', error);
            throw error;
        }
    }

    // Get current session information
    async getCurrentSessionInfo() {
        try {
            if (!('serviceWorker' in navigator)) {
                return null;
            }

            const registration = await navigator.serviceWorker.ready;
            if (!registration.active) {
                return null;
            }

            const messageChannel = new MessageChannel();
            
            return new Promise((resolve, reject) => {
                messageChannel.port1.onmessage = (event) => {
                    resolve(event.data);
                };

                registration.active.postMessage({
                    type: 'GET_CURRENT_SESSION_INFO'
                }, [messageChannel.port2]);

                // Timeout after 5 seconds
                setTimeout(() => {
                    resolve(null);
                }, 5000);
            });

        } catch (error) {
            console.error('OfflineSessionManager: Error getting session info:', error);
            return null;
        }
    }

    // Check if current session is initialized
    isSessionInitialized() {
        return this.isInitialized && this.currentSessionId !== null;
    }

    // Get current session ID
    getSessionId() {
        return this.currentSessionId;
    }

    // Clear session data (called when going online)
    clearSession() {
        console.log('OfflineSessionManager: Clearing session data');
        this.currentSessionId = null;
        this.isInitialized = false;
    }
}

// Create global instance
window.offlineSessionManager = new OfflineSessionManager();

// Auto-initialize when going offline (if not already initialized)
//window.addEventListener('offline', async () => {
//    try {
//        console.log('OfflineSessionManager: Browser went offline, checking session...');
//        if (!window.offlineSessionManager.isSessionInitialized()) {
//            console.log('OfflineSessionManager: No active session, initializing new one...');
//            await window.offlineSessionManager.initializeOfflineSession();
//        } else {
//            console.log('OfflineSessionManager: Session already initialized:', window.offlineSessionManager.getSessionId());
//        }
//    } catch (error) {
//        console.error('OfflineSessionManager: Auto-initialization failed:', error);
//    }
//});
//
//// Clear session when going online
//window.addEventListener('online', () => {
//    console.log('OfflineSessionManager: Browser went online, clearing session');
//    window.offlineSessionManager.clearSession();
//});

console.log('OfflineSessionManager: Module loaded and ready');