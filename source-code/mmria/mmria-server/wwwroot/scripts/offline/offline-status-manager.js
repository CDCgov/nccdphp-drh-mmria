const OFFLINE_ACTIVITY_STORAGE_KEY = 'mmria_offline_last_activity_at';
const SERVER_SESSION_SCOPE_COOKIE_NAME = 'mmria_session_scope';
const rawOfflineIdleTimeoutMinutes = Number(window.offline_session_timeout_config?.idle_timeout_minutes);
const offlineIdleTimeoutMinutes = Number.isFinite(rawOfflineIdleTimeoutMinutes) && rawOfflineIdleTimeoutMinutes > 0
  ? rawOfflineIdleTimeoutMinutes
  : 30;
const offlineIdleTimeoutMs = offlineIdleTimeoutMinutes * 60 * 1000;

function getCookieValue(cookieName) {
  if (typeof document === 'undefined' || typeof document.cookie !== 'string' || !cookieName) {
    return null;
  }

  const encodedName = `${encodeURIComponent(cookieName)}=`;
  const cookieParts = document.cookie.split(';');

  for (let i = 0; i < cookieParts.length; i++) {
    const cookiePart = cookieParts[i].trim();
    if (cookiePart.indexOf(encodedName) === 0) {
      return decodeURIComponent(cookiePart.substring(encodedName.length));
    }
  }

  return null;
}

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
   * Get the configured offline idle timeout in milliseconds
   * @returns {number} Idle timeout in milliseconds
   */
  getIdleTimeoutMs: function() {
    return offlineIdleTimeoutMs;
  },

  /**
   * Get the most recent offline activity timestamp
   * @returns {number|null} Unix timestamp in milliseconds or null
   */
  getLastActivityTimestamp: function() {
    try {
      const rawTimestamp = localStorage.getItem(OFFLINE_ACTIVITY_STORAGE_KEY);
      const parsedTimestamp = Number(rawTimestamp);
      return Number.isFinite(parsedTimestamp) && parsedTimestamp > 0 ? parsedTimestamp : null;
    } catch (_error) {
      return null;
    }
  },

  /**
   * Check whether offline activity is still within the idle timeout window
   * @returns {boolean} True when the activity timestamp is recent enough
   */
  hasRecentOfflineActivity: function() {
    const lastActivityTimestamp = this.getLastActivityTimestamp();
    if (lastActivityTimestamp == null) {
      return false;
    }

    return (Date.now() - lastActivityTimestamp) < offlineIdleTimeoutMs;
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
  },

  /**
   * Check whether the offline session is still effectively active for startup/bootstrap
   * @returns {boolean} True when offline mode is active, not processing, explicitly logged in, and not idle-expired
   */
  hasEffectiveActiveSession: function() {
    if (!this.isOffline()) {
      return false;
    }

    if (this.isProcessingOfflineCases()) {
      return false;
    }

    if (localStorage.getItem('has_active_offline_session') !== 'true') {
      return false;
    }

    return this.hasRecentOfflineActivity();
  },

  /**
   * Get the current server session scope as advertised by the browser cookie
   * @returns {string|null} Session scope string or null when unavailable
   */
  getServerSessionScope: function() {
    return getCookieValue(SERVER_SESSION_SCOPE_COOKIE_NAME);
  },

  /**
   * Check whether the current browser session is the narrowed offline token
   * @returns {boolean} True when the server session is scoped to offline_mode
   */
  isOfflineModeServerSession: function() {
    return this.getServerSessionScope() === 'offline_mode';
  },

  /**
   * Build the offline login URL for the current route or a supplied returnUrl
   * @param {string} returnUrl - Optional local returnUrl
   * @returns {string} Offline login URL
   */
  getOfflineLoginUrl: function(returnUrl) {
    const resolvedReturnUrl = typeof returnUrl === 'string' && returnUrl.length > 0
      ? returnUrl
      : `${window.location.pathname}${window.location.search}${window.location.hash}`;

    return `/Account/OfflineLogin?returnUrl=${encodeURIComponent(resolvedReturnUrl)}`;
  },

  /**
   * Build the auto-login URL for the current route or a supplied returnUrl
   * @param {string} returnUrl - Optional local returnUrl
   * @returns {string} Auto-login URL
   */
  getAutoLoginUrl: function(returnUrl) {
    const resolvedReturnUrl = typeof returnUrl === 'string' && returnUrl.length > 0
      ? returnUrl
      : `${window.location.pathname}${window.location.search}${window.location.hash}`;

    return `/Account/AutoLogin?returnUrl=${encodeURIComponent(resolvedReturnUrl)}`;
  },

  /**
   * Redirect the browser through the normal login flow
   * @param {string} returnUrl - Optional local returnUrl
   * @returns {string} Redirect target
   */
  redirectToAutoLogin: function(returnUrl) {
    const autoLoginUrl = this.getAutoLoginUrl(returnUrl);
    window.location.href = autoLoginUrl;
    return autoLoginUrl;
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
    if (window.OfflineStatus.isOffline() && window.OfflineStatus.hasEffectiveActiveSession()) {
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
    } else if (window.OfflineIntegrityValidator) {
        window.OfflineIntegrityValidator.stopMonitoring();
    }
    
    // Listen for storage changes from other tabs/windows
    window.addEventListener('storage', function(e) {
      if (e.key === 'is_offline') {
        updateOfflineIndicator();
        if (window.OfflineIntegrityValidator) {
          if (window.OfflineStatus.isOffline() && window.OfflineStatus.hasEffectiveActiveSession()) {
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
