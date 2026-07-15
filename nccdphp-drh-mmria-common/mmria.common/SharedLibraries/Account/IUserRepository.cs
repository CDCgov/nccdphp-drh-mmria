#nullable enable

using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Account;

/// <summary>
/// Repository interface for all _users CouchDB operations.
/// AccountDAL is the sole implementation. A SQL or ASP.NET Identity migration
/// requires only a new implementation of this interface — no caller changes needed.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get CouchDB user document by plain username (builds org.couchdb.user: prefix internally).
    /// </summary>
    Task<user?> GetCouchDbUserAsync(string userName, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get a CouchDB user document by full user_id (e.g. "org.couchdb.user:someone").
    /// </summary>
    Task<user> GetUserAsync(string userId, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Check if a CouchDB user document exists by full user_id.
    /// Returns an empty user object if not found or on error — never returns null.
    /// </summary>
    Task<user> CheckUserAsync(string userId, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Create or update a CouchDB user document via PUT.
    /// </summary>
    Task<document_put_response> PutUserAsync(user user, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Delete a CouchDB user document via DELETE.
    /// </summary>
    Task<ExpandoObject> DeleteUserAsync(string userId, string rev, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Get all users from _all_docs with pagination.
    /// </summary>
    Task<get_response_header<user>> GetAllUsersAsync(int skip, int take, DBConfigurationDetail dbConfig);
}
