using System;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.OfflineCase;
using mmria.common.SharedLibraries.OfflineCase.Model;
using mmria.common.utils;

namespace mmria.server.util
{
    /// <summary>
    /// Helper class for checking active offline sessions across the application.
    /// Provides centralized logic for determining if a user has an active offline session
    /// that requires attention (offline_state 0 or 1).
    /// </summary>
    public static class OfflineSessionHelper
    {
        /// <summary>
        /// Checks if the specified user has an active offline session.
        /// Returns detailed session status information.
        /// </summary>
        /// <param name="db_config">Database configuration for CouchDB access</param>
        /// <param name="userName">Username to check for active sessions</param>
        /// <returns>OfflineSessionStatus containing session details</returns>
        public static async Task<OfflineSessionStatus> CheckActiveOfflineSession(
            mmria.common.couchdb.DBConfigurationDetail db_config,
            string userName,
            IOfflineCaseRepository offlineCaseRepository)
        {
            try
            {
                if (string.IsNullOrEmpty(userName))
                {
                    return new OfflineSessionStatus
                    {
                        HasActiveSession = false,
                        OfflineState = null,
                        SessionData = null
                    };
                }

                // Query the offline_cases view for all documents
                var offline_case_documents = await offlineCaseRepository.GetAllActiveSessionsAsync(db_config);

                // Filter for current user and active states (0 or 1)
                var active_sessions = offline_case_documents.rows.Where(row =>
                    row?.value.created_by != null &&
                    string.Equals(row.value.created_by, userName, StringComparison.OrdinalIgnoreCase) &&
                    (row.value.offline_state == 0 || row.value.offline_state == 1)
                ).ToList();

                if (active_sessions.Count == 0)
                {
                    return new OfflineSessionStatus
                    {
                        HasActiveSession = false,
                        OfflineState = null,
                        SessionData = null
                    };
                }

                // Return the first active session found
                var firstSession = active_sessions.First().value;
                return new OfflineSessionStatus
                {
                    HasActiveSession = true,
                    OfflineState = firstSession.offline_state,
                    SessionData = firstSession
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking active offline session for user {userName}: {ex}");
                // Return false on error to avoid blocking user access
                return new OfflineSessionStatus
                {
                    HasActiveSession = false,
                    OfflineState = null,
                    SessionData = null
                };
            }
        }
        /// <summary>
        /// Checks if the specified user has an active offline session.
        /// Returns detailed session status information.
        /// </summary>
        /// <param name="db_config">Database configuration for CouchDB access</param>
        /// <param name="userName">Username to check for active sessions</param>
        /// <returns>OfflineSessionStatus containing session details</returns>
        public static async Task<OfflineSessionStatusLight> CheckActiveOfflineSessionLight(
            mmria.common.couchdb.DBConfigurationDetail db_config,
            string userName,
            IOfflineCaseRepository offlineCaseRepository)
        {
            try
            {
                if (string.IsNullOrEmpty(userName))
                {
                    return new OfflineSessionStatusLight
                    {
                        HasActiveSession = false,
                        OfflineState = null,
                        SessionData = null
                    };
                }

                // Query the offline_cases view for all documents
                var offline_case_documents = await offlineCaseRepository.GetAllLightweightOfflineCasesAsync(db_config);

                // Filter for current user and active states (0 or 1)
                var active_sessions = offline_case_documents.rows.Where(row =>
                    row?.value.created_by != null &&
                    string.Equals(row.value.created_by, userName, StringComparison.OrdinalIgnoreCase) &&
                    (row.value.offline_state == 0 || row.value.offline_state == 1)
                ).ToList();

                if (active_sessions.Count == 0)
                {
                    return new OfflineSessionStatusLight
                    {
                        HasActiveSession = false,
                        OfflineState = null,
                        SessionData = null
                    };
                }

                // Return the first active session found
                var firstSession = active_sessions.First().value;
                return new OfflineSessionStatusLight
                {
                    HasActiveSession = true,
                    OfflineState = firstSession.offline_state,
                    SessionData = firstSession
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking active offline session for user {userName}: {ex}");
                // Return false on error to avoid blocking user access
                return new OfflineSessionStatusLight
                {
                    HasActiveSession = false,
                    OfflineState = null,
                    SessionData = null
                };
            }
        }
        
    }
}
