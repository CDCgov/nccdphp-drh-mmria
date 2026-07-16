#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.ManageUsers.Model;

namespace mmria.common.SharedLibraries.Jurisdiction;

/// <summary>
/// Repository interface for all jurisdiction CouchDB operations.
/// JurisdictionDAL is the sole implementation. A SQL migration requires only
/// a new implementation of this interface — no caller changes needed.
/// </summary>
public interface IJurisdictionRepository
{
    // ── User-Role-Jurisdiction document CRUD ─────────────────────────────────

    /// <summary>GET a single user_role_jurisdiction document by ID.</summary>
    Task<user_role_jurisdiction> GetUserRoleJurisdictionAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>PUT (create or update) a user_role_jurisdiction document.</summary>
    Task<document_put_response> PutUserRoleJurisdictionAsync(user_role_jurisdiction item, DBConfigurationDetail dbConfig);

    /// <summary>DELETE a user_role_jurisdiction document by ID and revision.</summary>
    Task<document_put_response> DeleteUserRoleJurisdictionAsync(string id, string rev, DBConfigurationDetail dbConfig);

    /// <summary>GET all user_role_jurisdiction documents via _all_docs.</summary>
    Task<get_response_header<user_role_jurisdiction>> GetAllUserRoleJurisdictionsAsync(DBConfigurationDetail dbConfig);

    /// <summary>POST a batch of user_role_jurisdiction documents via _bulk_docs.</summary>
    Task<List<document_put_response>> BulkUpsertUserRoleJurisdictionsAsync(List<user_role_jurisdiction> items, DBConfigurationDetail dbConfig);

    // ── Sortable view queries ─────────────────────────────────────────────────

    /// <summary>
    /// GET user_role_jurisdiction documents via a pre-built sortable view URL.
    /// Prefer <see cref="GetUserRoleJurisdictionSortableViewByParamsAsync"/> for new call sites.
    /// </summary>
    Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewAsync(string requestUrl, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET user_role_jurisdiction documents from a sortable design-doc view, building the URL internally.
    /// Use this for all new call sites — URL construction belongs in JurisdictionDAL, not in callers.
    /// </summary>
    Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewByParamsAsync(
        int skip,
        int take,
        string sortView,
        bool hasSearchKey,
        bool descending,
        DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET session documents from the jurisdiction database via a sortable design-doc view.
    /// session and user_role_jurisdiction documents coexist in the jurisdiction database.
    /// </summary>
    Task<get_sortable_view_reponse_header<session>> GetSessionSortableViewAsync(
        int skip,
        int take,
        string sortView,
        bool hasSearchKey,
        bool descending,
        DBConfigurationDetail dbConfig);

    // ── Jurisdiction tree document ────────────────────────────────────────────

    /// <summary>GET the jurisdiction_tree well-known document.</summary>
    Task<jurisdiction_tree> GetJurisdictionTreeAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT (create or update) the jurisdiction_tree well-known document.</summary>
    Task<document_put_response> PutJurisdictionTreeAsync(jurisdiction_tree item, DBConfigurationDetail dbConfig);

    // ── Form access list document ─────────────────────────────────────────────

    /// <summary>GET the form-access-list well-known document.</summary>
    Task<FormAccessSpecification> GetFormAccessAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT (create or update) the form-access-list well-known document.</summary>
    Task<document_put_response> SaveFormAccessAsync(FormAccessSpecification request, DBConfigurationDetail dbConfig);

    // ── Pinned case set document ──────────────────────────────────────────────

    /// <summary>GET the pinned-case-set well-known document.</summary>
    Task<pinned_case_set?> GetPinnedCaseSetAsync(DBConfigurationDetail dbConfig);

    /// <summary>PUT (create or update) the pinned-case-set well-known document.</summary>
    Task<document_put_response> SavePinnedCaseSetAsync(pinned_case_set item, DBConfigurationDetail dbConfig);
}
