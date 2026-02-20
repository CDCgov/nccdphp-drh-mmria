// Check for active offline sessions on page load
document.addEventListener('DOMContentLoaded', async function() {
        
 
//   try {
//       const offlineSessionId = localStorage.getItem('offline_session_id');
//       if (offlineSessionId) {
//           offlineLog.log('OfflineHomePage', 'Checking offline session user match for session:', offlineSessionId);
          
        
//               if (offlineSessionId.indexOf(userName) === -1) {
//                   offlineLog.warn('OfflineHomePage', 'Offline session user mismatch detected. Clearing offline session data.');

//                   // Use the existing clear_all_cached_data function from offline-transition-manager
//                   await window.OfflineTransitionManager.clear_all_cached_data();
                  
//                   // Also clear has_active_offline_session and offline_session_id flags
//                   localStorage.removeItem('has_active_offline_session');
//                   localStorage.removeItem('offline_session_id');
//                   localStorage.removeItem('process_offline_cases');
//                   offlineLog.log('OfflineHomePage', 'Offline session cleared due to user mismatch');
//               } else {
//                   offlineLog.log('OfflineHomePage', 'Offline session user matches current user');
//               }
        
//       }
//   } catch (error) {
//   offlineLog.error('OfflineHomePage', 'Error checking offline session user:', error);
//   }     
  
  
  // First check if user is already in offline mode or processing offline cases
  const isOffline = localStorage.getItem('is_offline') === 'true';
  const isProcessingOfflineCases = localStorage.getItem('process_offline_cases') === 'true';
  


  // If not in offline mode, check for active sessions on server
  if (!isOffline && !isProcessingOfflineCases) {
    fetch('/api/OfflineCase/lightweight-status-only')
      .then(response => response.json())
      .then(data => {
        // offline_state: 0 = user has active offline session (needs to go to summary)
        // offline_state: 1 = user has partially synced (needs to finish syncing)
        // offline_state: 2 = fully synced/no active session
        if (data && (data.offline_state === 0 || data.offline_state === 1)) {
          // Redirect to case summary to handle the offline session
          window.location.href = '/Case#/summary';
        }
      })
      .catch(error => {
        offlineLog.warn('OfflineHomePage', 'Unable to check for active offline sessions:', error);
      });
  }
  
  // Continue with existing offline mode UI logic
  handleOfflineModeUI(isOffline, isProcessingOfflineCases);
});

function handleOfflineModeUI(isOffline, isProcessingOfflineCases) {
  try {
    // Show banner and disable links if either offline mode or processing offline cases
    if (isOffline || isProcessingOfflineCases) {
      const banner = document.getElementById('offline_mode_banner');
      if (banner) {
        banner.style.display = 'block';
      }
      
      // Hide broadcast messages when offline
      const broadcastMsg1 = document.getElementById('broadcast_published_message_one');
      const broadcastMsg2 = document.getElementById('broadcast_published_message_two');
      if (broadcastMsg1) {
        broadcastMsg1.style.display = 'none';
      }
      if (broadcastMsg2) {
        broadcastMsg2.style.display = 'none';
      }
      
      // Disable all links except "View or Modify Case Data" when offline
      const allLinks = document.querySelectorAll('a[href]');
      allLinks.forEach(function(link) {
        // Skip the "View or Modify Case Data" link (href="/Case")
        if (link.getAttribute('href') === '/Case') {
          return; // Keep this link enabled
        }
        
        // Disable other links
        link.style.color = '#999';
        link.style.textDecoration = 'none';
        link.style.cursor = 'not-allowed';
        link.style.opacity = '0.6';
        
        // Prevent click events
        link.addEventListener('click', function(e) {
          e.preventDefault();
          e.stopPropagation();
          return false;
        });
        
        // Add disabled attribute for accessibility
        link.setAttribute('aria-disabled', 'true');
        link.setAttribute('title', 'This feature is not available in offline mode');
      });
      
      // Disable all buttons with offline-disable class when offline
      const offlineDisableButtons = document.querySelectorAll('button.offline-disable');
      offlineDisableButtons.forEach(function(button) {
        // Disable buttons
        button.style.color = '#999';
        button.style.cursor = 'not-allowed';
        button.style.opacity = '0.6';
        button.disabled = true;

        // Prevent click events
        button.addEventListener('click', function(e) {
          e.preventDefault();
          e.stopPropagation();
          return false;
        });

        // Add disabled attribute for accessibility
        button.setAttribute('aria-disabled', 'true');
        button.setAttribute('title', 'This feature is not available in offline mode');
      });
    }
  } catch (error) {
    offlineLog.warn('OfflineHomePage', 'Unable to check offline mode status:', error);
  }
}
