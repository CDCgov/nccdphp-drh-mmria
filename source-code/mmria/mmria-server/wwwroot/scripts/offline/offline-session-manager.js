/**
 * Offline Session Manager
 * Handles offline session data retrieval, active session checking, and session validation
 */

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
        !offlineSessionData.case_documents.some(change => change.documentId === id)
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
      offlineLog.log('OfflineSessionManager', 'Fetching offline cases by session ID:', offlineSessionId);
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
                !sessionData.case_documents.some(change => change.documentId === id)
              );
              offlineLog.log('OfflineSessionManager', 'Populated offline_ids_not_changed on first load:', 
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

    offlineLog.log('OfflineSessionManager', 'Offline mode: Loaded', recordIdSet.size, 'record IDs from cached data');
    return recordIdSet;
  },

  /**
   * Load offline cases from the API and populate case view list
   * @param {Function} ensureInitCallback - Callback to ensure offline initialization
   * @param {Function} updateIndexMapCallback - Callback to update offline case index map
   * @returns {Promise<Object>} Object with case_view_list and total_rows
   */
  loadOfflineCases: async function(ensureInitCallback, updateIndexMapCallback) {
    offlineLog.log('OfflineSessionManager', 'In offline mode - loading cached metadata and cases');

    // Ensure initialization is complete
    if (ensureInitCallback) {
      await ensureInitCallback();
    }

    const result = {
      offline_mode_case_view_list: [],
      case_view_list: [],
      total_rows: 0
    };

    try {
      // Get offline cases and populate case_view_list
      offlineLog.log('OfflineSessionManager', '📡 Making request to /api/case_view/offline-documents...');
      const response = await fetch('/api/case_view/offline-documents');
      offlineLog.log('OfflineSessionManager', '📡 Response received:', {
        status: response.status,
        statusText: response.statusText,
        headers: Object.fromEntries(response.headers.entries()),
        url: response.url
      });

      const offlineData = await response.json();

      offlineLog.log('OfflineSessionManager', '📊 Offline case data loaded:', {
        total_rows: offlineData.total_rows,
        rows_count: offlineData.rows?.length || 0,   
      
      });

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

        offlineLog.log('OfflineSessionManager', '✅ Populated case_view_list with offline cases:', 
          result.case_view_list.length, 'cases');
        offlineLog.log('OfflineSessionManager', 'Case IDs available:', result.case_view_list.map(c => c.id));

        // Update the offline case index map with the loaded cases
        if (updateIndexMapCallback) {
          updateIndexMapCallback();
        }
      } else {
        offlineLog.warn('OfflineSessionManager', 'No offline cases found, initializing empty case list');
      }
    } catch (error) {
      offlineLog.error('OfflineSessionManager', '❌ Error loading offline cases:', error);
    }

    return result;
  }
};
