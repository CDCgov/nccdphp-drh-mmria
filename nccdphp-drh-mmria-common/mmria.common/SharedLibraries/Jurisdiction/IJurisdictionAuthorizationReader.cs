using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.Jurisdiction.Model;

namespace mmria.common.SharedLibraries.Jurisdiction;

/// <summary>
/// Read-only interface for the per-request authorization view query against
/// <c>jurisdiction/_design/sortable/_view/by_user_id</c>.
/// <see cref="DAL.JurisdictionAuthorizationDAL"/> is the sole CouchDB implementation.
/// A SQL migration requires only a new implementation of this interface — no auth handler
/// code changes are needed.
/// </summary>
/// <remarks>
/// When <paramref name="userId"/> is non-null and non-empty the query is filtered to that
/// user via <c>?startkey="{userId}"&amp;endkey="{userId}"</c>.
/// When <paramref name="userId"/> is null or empty all role documents are returned
/// (whole-tenant scan, used by <c>authorization_case</c>).
/// Active-role filtering is NOT performed by this interface — it is the responsibility
/// of callers (primarily <see cref="mmria.common.utils.AuthorizationRoleCache"/>).
/// </remarks>
public interface IJurisdictionAuthorizationReader
{
    Task<IReadOnlyList<JurisdictionRoleEntry>> GetRolesByUserIdAsync(
        string? userId,
        DBConfigurationDetail dbConfig);
}
