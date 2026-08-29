/**
 * Offline Logout Button Visibility Manager
 * Conditionally hides/shows the login partial based on offline mode status
 */

// Conditionally hide login partial based on offline mode status
document.addEventListener('DOMContentLoaded', function() {
  function updateLoginPartialVisibility() {
    const isOfflineLoginPage = /^\/Account\/OfflineLogin\/?$/i.test(window.location.pathname);
    const isOffline = localStorage.getItem('is_offline') === 'true';
    const hasActiveSession = localStorage.getItem('has_active_offline_session') === 'true';
    const loginContainer = document.getElementById('login-partial-container');
    
    if (loginContainer) {
      // Always hide the login partial on the offline login page.
      if (isOfflineLoginPage) {
        loginContainer.style.display = 'none';
      // Hide if: offline mode is true AND has no active offline session
      } else if (isOffline && !hasActiveSession) {
        loginContainer.style.display = 'none';   
      } else {
        loginContainer.style.display = '';     
      }
    }
  }
  
  // Initial check on page load
  updateLoginPartialVisibility();
  
  // Listen for custom offline status change events
  window.addEventListener('offlineStatusChanged', function() {
    updateLoginPartialVisibility();
  });
  
  // Listen for localStorage changes (from other tabs/windows)
  window.addEventListener('storage', function(e) {
    if (e.key === 'is_offline' || e.key === 'has_active_offline_session') {
      updateLoginPartialVisibility();
    }
  });
  
  // Make function globally available for programmatic control
  window.updateLoginPartialVisibility = updateLoginPartialVisibility;
});

