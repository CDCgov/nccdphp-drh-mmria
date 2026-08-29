using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Session.Model;

namespace mmria.common.SharedLibraries.Session;

/// <summary>
/// Repository interface for all session database operations.
/// SessionDAL is the sole implementation. A SQL session-store migration
/// requires only a new implementation of this interface — no caller changes needed.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Get a paginated, sortable view of session documents.
    /// </summary>
    Task<get_sortable_view_reponse_header<session>> GetSessionSortableViewAsync(int skip, int take, string sortView, bool hasSearchKey, bool descending, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Create or replace a session document using a Session_Message (preserves role_list).
    /// </summary>
    Task<document_put_response> CreateSessionAsync(Session_Message session, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Save a session-event document (login/logout audit trail).
    /// </summary>
    Task SaveSessionEventAsync(session_event sessionEvent, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get the session database metadata.
    /// </summary>
    Task<session_response> GetSessionDatabaseAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get a session document by ID.
    /// </summary>
    Task<session> GetSessionDocumentAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Save (PUT) a typed session document.
    /// </summary>
    Task<document_put_response> SaveSessionAsync(session session, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Save a session document from pre-serialized JSON. Used when the caller holds fields
    /// (e.g. role_list) not captured by the typed <see cref="session"/> model.
    /// </summary>
    Task<document_put_response?> SaveSessionRawAsync(string sessionId, string sessionJson, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get the current CouchDB /_session response (auth validation).
    /// </summary>
    Task<session_response> GetCouchDbSessionAsync(string authSessionValue, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Log in to CouchDB /_session to obtain an AuthSession cookie.
    /// </summary>
    Task<login_response> LoginToCouchDbSessionAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get all session-event documents for a specific user.
    /// </summary>
    Task<get_sortable_view_reponse_header<session_event>> GetSessionEventsByUserIdAsync(string userName, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get recent session documents ordered by date_created (for dashboard / summary views).
    /// </summary>
    Task<view_response<session>> GetSessionByDateCreatedViewAsync(bool descending, int limit, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get a session document as raw JSON. Use when the caller needs fields not present in the
    /// typed <see cref="session"/> model (e.g. role_list for logout re-save).
    /// </summary>
    Task<string?> GetSessionDocumentRawAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Delete a session document by ID and revision.
    /// </summary>
    Task DeleteSessionAsync(string id, string rev, DBConfigurationDetail dbConfig);
}
