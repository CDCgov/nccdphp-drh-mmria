/**
 * Offline Logout Button Visibility Manager
 * Conditionally hides/shows the login partial based on offline mode status
 */

// Conditionally hide login partial based on offline mode status
document.addEventListener('DOMContentLoaded', function() {
  function updateLoginPartialVisibility() {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
    const loginContainer = document.getElementById('login-partial-container');
    
    if (loginContainer) {
      // Hide if: offline mode is true AND has no active offline session
      if (isOffline && !hasActiveSession) {
        loginContainer.style.display = 'none';
        if (window.offlineLog) {
          offlineLog.log('OfflineLogoutButton', 'Login partial hidden: offline mode without active session');
        }
      } else {
        loginContainer.style.display = '';
        if (window.offlineLog) {
          offlineLog.log('OfflineLogoutButton', 'Login partial visible: ' + 
            (isOffline ? 'offline mode with active session' : 'online mode'));
        }
      }
    }
  }
  
  // Initial check on page load
  updateLoginPartialVisibility();
  
  // Listen for custom offline status change events
  window.addEventListener('offlineStatusChanged', function() {
    if (window.offlineLog) {
      offlineLog.log('OfflineLogoutButton', 'Offline status changed event received, updating login partial visibility');
    }
    updateLoginPartialVisibility();
  });
  
  // Listen for localStorage changes (from other tabs/windows)
  window.addEventListener('storage', function(e) {
    if (e.key === 'is_offline' || e.key === 'has_active_offline_session') {
      if (window.offlineLog) {
        offlineLog.log('OfflineLogoutButton', 'Storage change detected for key:', e.key, 'updating login partial visibility');
      }
      updateLoginPartialVisibility();
    }
  });
  
  // Make function globally available for programmatic control
  window.updateLoginPartialVisibility = updateLoginPartialVisibility;
});

if (window.offlineLog) {
  offlineLog.log('OfflineLogoutButton', 'Offline Logout Button Visibility Manager loaded');
}
