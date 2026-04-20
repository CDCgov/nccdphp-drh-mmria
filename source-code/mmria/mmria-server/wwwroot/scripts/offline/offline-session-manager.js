/**
 * Offline Session Manager
 * Handles offline session data retrieval, active session checking, and session validation
 */

function redirectToOfflineLoginForReauth() {
  const offlineLoginUrl = window.OfflineStatus && typeof window.OfflineStatus.getOfflineLoginUrl === 'function'
    ? window.OfflineStatus.getOfflineLoginUrl()
    : '/Account/OfflineLogin';

  window.location.href = offlineLoginUrl;
}

window.OfflineSessionManager = {
  /**
   * Load offline session data for the current user
   * @param {boolean} isOfflineModeEnabled - Whether offline mode is enabled
   * @returns {Promise<Object>} Object with offline_session_data and offline_ids_not_changed
   */
  loadOfflineSessionData: async function(isOfflineModeEnabled) {
    const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
    
    if (isOfflineModeEnabled && isProcessingOfflineCases) {
      const offlineSessionId = window.OfflineStatus.getOfflineSessionId() || '';
      const offlineSessionData = await window.OfflineCaseManager.getCasesBySession(offlineSessionId);
      const offline_ids_not_changed = offlineSessionData.offline_ids.filter(id => 
        !offlineSessionData.case_documents.some(change => change.documentId === id && change.syncState !== 5) // 5 = no changes
      );
      
      return {
        offline_session_data: offlineSessionData,
        offline_ids_not_changed: offline_ids_not_changed
      };
    } else {
      return {
        offline_session_data: null,
        offline_ids_not_changed: []
      };
    }
  },

  /**
   * Check for active offline session and update localStorage accordingly
   * @param {string} userName - Current user's username
   * @param {Array} caseViewList - List of case view items for filtering
   * @returns {Promise<Object>} Object with session data and user's offline cases
   */
  checkActiveSession: async function(userName, caseViewList) {
    const isOfflineMode = window.OfflineStatus.isOffline();
    const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
    let offlineSessionId = window.OfflineStatus.getOfflineSessionId();
    
    const result = {
      offline_case_view_list_by_user: [],
      process_offline_case_view_list_by_user: null,
      offline_ids_not_changed: []
    };

    // Filter user's offline cases if not in processing mode
    if (isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true') {
      result.offline_case_view_list_by_user = caseViewList.filter(
        x => x.value.offline_by === userName && x.value.is_offline === true
      );
    }

    try {
      const response = await fetch(`/api/OfflineCase/active-user-session`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const sessionData = await response.json();
        if (sessionData && sessionData.error !== "no active sessions") {
          result.process_offline_case_view_list_by_user = sessionData;

          // Check if offline_session_id is not set and set it from the response
          if (!offlineSessionId || offlineSessionId === 'null' || offlineSessionId === '') {
            offlineLog.log('OfflineSessionManager', 'Setting offline_session_id from response:', sessionData._id);
            localStorage.setItem('offline_session_id', sessionData._id);
          }

          // Handle offline session state
          if (sessionData.offline_state === 0) {
            localStorage.setItem('abandon_offline_session', 'true');
          } else if (sessionData.offline_state === 1) {
            localStorage.setItem('process_offline_cases', 'true');

            // Fix race condition: Populate offline_ids_not_changed here as well
            if (sessionData.offline_ids && sessionData.case_documents) {
              result.offline_ids_not_changed = sessionData.offline_ids.filter(id =>
                !sessionData.case_documents.some(change => change.documentId === id && change.syncState !== 5) // 5 = no changes
              );
              offlineLog.log('OfflineSessionManager', 'Active session found:', 
                result.offline_ids_not_changed.length, 'cases without changes');
            }
          }
        }
      } else {
        offlineLog.warn('OfflineSessionManager', 'Failed to fetch active user session:', response.statusText);
      }
    } catch (error) {
      offlineLog.error('OfflineSessionManager', 'Error checking active offline session:', error);
    }

    return result;
  },

  /**
   * Load offline record IDs from cached case data
   * @param {Object} g_ui - Global UI object with offline case lists
   * @returns {Set<string>} Set of record IDs
   */
  loadOfflineRecordIds: function(g_ui) {
    const recordIdSet = new Set();

    // Use offline case list if available
    if (g_ui && g_ui.offline_mode_case_view_list && g_ui.offline_mode_case_view_list.length > 0) {
      for (let i = 0; i < g_ui.offline_mode_case_view_list.length; i++) {
        const item = g_ui.offline_mode_case_view_list[i];
        if (item.value && item.value.record_id) {
          recordIdSet.add(item.value.record_id.toUpperCase());
        }
      }
    }

    // Also check regular case view list if available
    if (g_ui && g_ui.case_view_list && g_ui.case_view_list.length > 0) {
      for (let i = 0; i < g_ui.case_view_list.length; i++) {
        const item = g_ui.case_view_list[i];
        if (item.value && item.value.record_id) {
          recordIdSet.add(item.value.record_id.toUpperCase());
        }
      }
    }

    return recordIdSet;
  },

  /**
   * Load offline cases from the API and populate case view list
   * @param {Function} ensureInitCallback - Callback to ensure offline initialization
   * @param {Function} updateIndexMapCallback - Callback to update offline case index map
   * @returns {Promise<Object>} Object with case_view_list and total_rows
   */
  loadOfflineCases: async function(ensureInitCallback, updateIndexMapCallback) {
    // Ensure initialization is complete
    if (ensureInitCallback) {
      await ensureInitCallback();
    }

    const validationContext = {
      checkPoint: 'case_list_load'
    };

    if (window.OfflineIntegrityValidator) {
      await window.OfflineIntegrityValidator.validateCurrentState(validationContext);
    }

    const result = {
      offline_mode_case_view_list: [],
      case_view_list: [],
      total_rows: 0
    };

    try {
      const response = await fetch('/api/case_view/offline-documents');

      if (!response.ok) {
        if (response.status === 401) {
          try {
            const errorData = await response.json();
            if (errorData && errorData.error === 'offline_key_required') {
              offlineLog.warn('OfflineSessionManager', 'Offline key re-entry required before loading cached case list');
              redirectToOfflineLoginForReauth();
              return result;
            }
          } catch (parseError) {
            offlineLog.warn('OfflineSessionManager', 'Could not parse offline case-list auth failure response', parseError);
          }
        }

        offlineLog.error('OfflineSessionManager', 'Failed to load offline cases:', response.status);
        return result;
      }

      const offlineData = await response.json();

      // Convert offline document format to case_view_list format
      if (offlineData.rows && Array.isArray(offlineData.rows)) {
        const mappedRows = offlineData.rows.map(row => ({
          id: row.id,
          rev: row.rev,
          key: row.key,
          value: row.value,
          doc: row.doc
        }));

        result.offline_mode_case_view_list = mappedRows;
        result.case_view_list = mappedRows;
        result.total_rows = offlineData.total_rows || offlineData.rows.length;

        offlineLog.info('OfflineSessionManager', 'Loaded offline cases successfully', {
          loadedCaseCount: result.case_view_list.length,
          loadedCaseIds: result.case_view_list.map(item => item.id)
        });

        if (
          window.OfflineCaseManager &&
          typeof window.OfflineCaseManager.reconcileOfflineRemovedCaseState === 'function'
        ) {
          await window.OfflineCaseManager.reconcileOfflineRemovedCaseState(
            result.case_view_list.map(item => item.id)
          );
        }

        // Update the offline case index map with the loaded cases
        if (updateIndexMapCallback) {
          updateIndexMapCallback();
        }

        if (window.OfflineIntegrityValidator) {
          await window.OfflineIntegrityValidator.validateCurrentState({
            checkPoint: 'case_list_load',
            expectedOfflineIds: result.case_view_list.map(item => item.id)
          });
        }
      } else {
        offlineLog.warn('OfflineSessionManager', 'No offline cases found');
      }
    } catch (error) {
      offlineLog.error('OfflineSessionManager', 'Error loading offline cases:', error);
    }

    return result;
  }
};
