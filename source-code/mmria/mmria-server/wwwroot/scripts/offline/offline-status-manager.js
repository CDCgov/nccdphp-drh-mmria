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
    
    // Listen for storage changes from other tabs/windows
    window.addEventListener('storage', function(e) {
      if (e.key === 'is_offline') {
        updateOfflineIndicator();
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
