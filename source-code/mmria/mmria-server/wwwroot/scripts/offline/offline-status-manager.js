// Helper functions for checking offline status
window.OfflineStatus = {
  /**
   * Check if the application is currently in offline mode
   * @returns {boolean} True if offline mode is enabled
   */
  isOffline: function() {
    return localStorage.getItem('is_offline') === 'true';
  },

  /**
   * Check if the application is currently processing offline cases
   * @returns {boolean} True if processing offline cases
   */
  isProcessingOfflineCases: function() {
    return localStorage.getItem('process_offline_cases') === 'true';
  },

  /**
   * Get the current offline session ID
   * @returns {string|null} The offline session ID or null if not set
   */
  getOfflineSessionId: function() {
    return localStorage.getItem('offline_session_id');
  },

  /**
   * Check if there is an active offline session
   * @returns {boolean} True if an offline session is active
   */
  hasActiveSession: function() {
    const hasActiveOfflineSession = localStorage.getItem('has_active_offline_session');
    if (hasActiveOfflineSession != null) {
      return hasActiveOfflineSession === 'true';
    }

    return this.getOfflineSessionId() != null && this.getOfflineSessionId() !== '';
  }
};

document.addEventListener('DOMContentLoaded', function() {
    const offlineModeIndicator = document.getElementById('offline-mode-indicator');
    
    function updateOfflineIndicator() {
      const isOffline = window.OfflineStatus.isOffline();
      if (offlineModeIndicator) {
        offlineModeIndicator.style.display = isOffline ? 'block' : 'none';
      }
    }
    
    // Initial check
    updateOfflineIndicator();
    
    // RESTART SERVICE WORKER KEEP-ALIVE IF IN OFFLINE MODE
    // If user refreshes page during offline session, keep-alive timer is lost from memory
    // This restarts it to prevent service worker termination and loss of encryption key
    if (window.OfflineStatus.isOffline()) {
        // Check if keep-alive is not already running
        const existingInterval = window.OfflineTransitionManager?.g_service_worker_keep_alive_interval;
        
        if (!existingInterval && 'serviceWorker' in navigator && navigator.serviceWorker.controller) {
            // Use the same interval as during initial offline transition (15 seconds)
            const keepAliveInterval = setInterval(() => {
                if (navigator.serviceWorker.controller) {
                    navigator.serviceWorker.controller.postMessage({ type: 'KEEP_ALIVE' });
                } else {
                    // Service worker controller lost - clear interval
                    if (window.offlineLog) {
                        offlineLog.warn('OfflineStatusManager', 'Service worker controller lost, clearing keep-alive interval');
                    }
                    clearInterval(keepAliveInterval);
                }
            }, 15000); // Every 15 seconds
            
            // Store in OfflineTransitionManager for cleanup when going online
            if (window.OfflineTransitionManager) {
                window.OfflineTransitionManager.g_service_worker_keep_alive_interval = keepAliveInterval;
            }
            
            if (window.offlineLog) {
                offlineLog.log('OfflineStatusManager', 'Restarted service worker keep-alive after page load');
            }
        } 

        if (window.OfflineIntegrityValidator) {
            window.OfflineIntegrityValidator.startMonitoring();
        }
    }
    
    // Listen for storage changes from other tabs/windows
    window.addEventListener('storage', function(e) {
      if (e.key === 'is_offline') {
        updateOfflineIndicator();
        if (window.OfflineIntegrityValidator) {
          if (window.OfflineStatus.isOffline()) {
            window.OfflineIntegrityValidator.startMonitoring();
          } else {
            window.OfflineIntegrityValidator.stopMonitoring();
          }
        }
      }
    });
    
    // Listen for custom offline status change events
    window.addEventListener('offlineStatusChanged', function() {
      updateOfflineIndicator();
    });
    
    // Periodic check every 2 seconds as a fallback
    setInterval(function() {
      updateOfflineIndicator();
    }, 2000);
    
    // Make the update function globally available so other parts of the app can call it
    window.updateOfflineModeIndicator = updateOfflineIndicator;
  });
