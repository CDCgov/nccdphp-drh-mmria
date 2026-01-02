/**
 * Offline Navigation Manager
 * Handles case navigation and index mapping for offline mode
 */

window.OfflineNavigationManager = {
  /**
   * Get target case ID for navigation in offline mode
   * @param {number} caseIndex - The index of the case to navigate to
   * @param {string} currentCaseId - The current case ID (for logging)
   * @param {Object} g_ui - Global UI object with case_view_list
   * @returns {Object} Object with targetCaseId and error (if any)
   */
  getTargetCaseId: function(caseIndex, currentCaseId, g_ui) {
    const result = {
      targetCaseId: null,
      error: null
    };

    // Update offline case index map first
    if (typeof window.OfflineCaseManager !== 'undefined' && 
        window.OfflineCaseManager.updateOfflineCaseIndexMap) {
      window.OfflineCaseManager.updateOfflineCaseIndexMap();
    } else if (typeof update_offline_case_index_map === 'function') {
      update_offline_case_index_map();
    }

    offlineLog.log('OfflineNavigationManager', 'Offline case index map:', window.g_offline_case_index_map);
    offlineLog.log('OfflineNavigationManager', 'g_ui.case_view_list length:', g_ui.case_view_list ? g_ui.case_view_list.length : 'undefined');

    // Check if case exists in offline index map
    if (window.g_offline_case_index_map && 
        caseIndex < window.g_offline_case_index_map.length && 
        caseIndex >= 0) {
      result.targetCaseId = window.g_offline_case_index_map[caseIndex];
      offlineLog.log('OfflineNavigationManager', 'Target offline case ID from index map:', result.targetCaseId, 
                  'Current case ID:', currentCaseId);
    }
    // Invalid case index (but not 100 which is a special case for "all cases")
    else if (caseIndex !== 100) {
      const availableInIndexMap = window.g_offline_case_index_map ? 
        window.g_offline_case_index_map.length : 0;
      const availableInCaseList = g_ui.case_view_list ? 
        g_ui.case_view_list.length : 0;
      
      offlineLog.error('OfflineNavigationManager', 'Invalid offline case index:', caseIndex, 
                   'Available in index map:', availableInIndexMap,
                   'Available in case list:', availableInCaseList);
      
      result.error = 'Case not found in offline list.';
    }

    return result;
  },

  /**
   * Navigate to a case by index in offline mode
   * @param {number} caseIndex - The index of the case to navigate to
   * @param {string} currentCaseId - The current case ID
   * @param {Object} g_ui - Global UI object with case_view_list
   * @returns {boolean} True if navigation was successful, false otherwise
   */
  navigateToCaseByIndex: function(caseIndex, currentCaseId, g_ui) {
    const result = this.getTargetCaseId(caseIndex, currentCaseId, g_ui);
    
    if (result.error) {
      alert(result.error);
      window.location.hash = '#/summary';
      return false;
    }
    
    if (result.targetCaseId) {
      // Navigate to the target case
      window.location.hash = `#/${result.targetCaseId}`;
      return true;
    }
    
    return false;
  },

  /**
   * Get the current case index from the offline index map
   * @param {string} caseId - The case ID to find
   * @returns {number} The index of the case, or -1 if not found
   */
  getCurrentCaseIndex: function(caseId) {
    if (!window.g_offline_case_index_map || !Array.isArray(window.g_offline_case_index_map)) {
      return -1;
    }
    
    return window.g_offline_case_index_map.indexOf(caseId);
  },

  /**
   * Check if navigation to next/previous case is possible
   * @param {string} caseId - Current case ID
   * @param {string} direction - 'next' or 'previous'
   * @returns {boolean} True if navigation is possible
   */
  canNavigate: function(caseId, direction) {
    if (!window.g_offline_case_index_map || !Array.isArray(window.g_offline_case_index_map)) {
      return false;
    }
    
    const currentIndex = this.getCurrentCaseIndex(caseId);
    
    if (currentIndex === -1) {
      return false;
    }
    
    if (direction === 'next') {
      return currentIndex < window.g_offline_case_index_map.length - 1;
    } else if (direction === 'previous') {
      return currentIndex > 0;
    }
    
    return false;
  },

  /**
   * Get the next or previous case ID
   * @param {string} caseId - Current case ID
   * @param {string} direction - 'next' or 'previous'
   * @returns {string|null} The next/previous case ID, or null if not available
   */
  getAdjacentCaseId: function(caseId, direction) {
    if (!this.canNavigate(caseId, direction)) {
      return null;
    }
    
    const currentIndex = this.getCurrentCaseIndex(caseId);
    const newIndex = direction === 'next' ? currentIndex + 1 : currentIndex - 1;
    
    return window.g_offline_case_index_map[newIndex];
  },

  /**
   * Get target case ID for hash change navigation
   * Handles both offline mode and processing offline cases mode
   * @param {number} caseIndex - The case index from the URL
   * @param {string} currentCaseId - The current case ID
   * @param {Object} g_ui - Global UI object
   * @returns {Object} Object with targetCaseId and error (if any)
   */
  getTargetCaseIdForHashChange: function(caseIndex, currentCaseId, g_ui) {
    const result = {
      targetCaseId: null,
      error: null
    };

    const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
    const isOffline = window.OfflineStatus.isOffline();
    
    offlineLog.log('OfflineNavigationManager', 'Hash change: navigating to case index:', caseIndex);
    offlineLog.log('OfflineNavigationManager', 'Processing offline cases mode:', isProcessingOfflineCases);
    offlineLog.log('OfflineNavigationManager', 'Offline mode:', isOffline);
    
    if (isProcessingOfflineCases) {
      // In processing offline cases mode, get case from offline session
      offlineLog.log('OfflineNavigationManager', 'Processing offline cases - getting case ID from session at index:', caseIndex);
      
      if (g_ui.process_offline_case_view_list_by_user?.case_documents &&
          caseIndex >= 0 && 
          caseIndex < g_ui.process_offline_case_view_list_by_user.case_documents.length) {
        
        result.targetCaseId = g_ui.process_offline_case_view_list_by_user.case_documents[caseIndex].documentId;
        offlineLog.log('OfflineNavigationManager', 'Target case ID from offline session:', result.targetCaseId, 'Current case ID:', currentCaseId);
      } else {
        const availableCount = g_ui.process_offline_case_view_list_by_user?.case_documents?.length || 0;
        offlineLog.error('OfflineNavigationManager', 'Invalid case index for offline session:', caseIndex, 'Available:', availableCount);
        result.error = 'This case is not available in the current offline session. Please return to the case list.';
      }
    } else if (isOffline) {
      // In offline mode, ensure index map is synchronized first
      if (typeof update_offline_case_index_map === 'function') {
        update_offline_case_index_map();
      } else if (typeof window.OfflineCaseManager !== 'undefined' && window.OfflineCaseManager.updateOfflineCaseIndexMap) {
        window.OfflineCaseManager.updateOfflineCaseIndexMap();
      }
      
      offlineLog.log('OfflineNavigationManager', 'Offline case index map:', window.g_offline_case_index_map);
      offlineLog.log('OfflineNavigationManager', 'g_ui.case_view_list length:', g_ui.case_view_list ? g_ui.case_view_list.length : 'undefined');
      
      // Check if case exists in offline index map
      if (window.g_offline_case_index_map && caseIndex < window.g_offline_case_index_map.length && caseIndex >= 0) {
        result.targetCaseId = window.g_offline_case_index_map[caseIndex];
        offlineLog.log('OfflineNavigationManager', 'Target offline case ID from index map:', result.targetCaseId, 'Current case ID:', currentCaseId);
      }
      // Invalid case index (but not 100 which is a special case)
      else if (caseIndex !== 100) {
        const availableInIndexMap = window.g_offline_case_index_map ? window.g_offline_case_index_map.length : 0;
        const availableInCaseList = g_ui.case_view_list ? g_ui.case_view_list.length : 0;
        offlineLog.error('OfflineNavigationManager', 'Invalid offline case index:', caseIndex, 
                     'Available in index map:', availableInIndexMap,
                     'Available in case list:', availableInCaseList);
        result.error = 'Case not found in offline list.';
      }
    }

    return result;
  }
};
