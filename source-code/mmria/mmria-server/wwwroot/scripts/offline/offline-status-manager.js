document.addEventListener('DOMContentLoaded', function() {
    const offlineModeIndicator = document.getElementById('offline-mode-indicator');
    
    function updateOfflineIndicator() {
      const isOffline = localStorage.getItem('is_offline') === 'true';
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
