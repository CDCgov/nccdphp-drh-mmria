using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.getset;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.model.couchdb.audit;
using mmria.common.SharedLibraries.ManageUsers.Model;
using mmria.common.SharedLibraries.Jurisdiction;
using System.Text.Json.Serialization;

namespace mmria.common.SharedLibraries.ManageUsers.DAL;

/// <summary>
/// Data Access Layer for Manage Users operations.
/// Contains ALL CouchDB calls for user CRUD and user_role_jurisdiction bulk operations.
/// No business logic - only data operations.
/// </summary>
public class ManageUsersDAL
{
    private static readonly System.Text.Json.JsonSerializerOptions SensitiveJsonPayloadOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CouchDbHttpClient _httpClient;
    private readonly mmria.common.SharedLibraries.Account.IUserRepository _userRepository;
    private readonly IJurisdictionRepository _jurisdictionRepository;

    public ManageUsersDAL(
        CouchDbHttpClient httpClient,
        mmria.common.SharedLibraries.Account.IUserRepository userRepository,
        IJurisdictionRepository jurisdictionRepository)
    {
        _httpClient = httpClient;
        _userRepository = userRepository;
        _jurisdictionRepository = jurisdictionRepository;
    }

    /// <summary>
    /// Get a CouchDB user document by user_id (e.g. "org.couchdb.user:someone").
    /// Delegates to IUserRepository.
    /// </summary>
    public Task<user> GetUserAsync(
        string user_id,
        DBConfigurationDetail db_config)
    {
        return _userRepository.GetUserAsync(user_id, db_config);
    }

    /// <summary>
    /// Check if a CouchDB user document exists by user_id.
    /// Returns the user if found, or an empty user object if not found or on error.
    /// Never returns null. Delegates to IUserRepository.
    /// </summary>
    public Task<user> CheckUserAsync(
        string user_id,
        DBConfigurationDetail db_config)
    {
        return _userRepository.CheckUserAsync(user_id, db_config);
    }

    /// <summary>
    /// Create or update a CouchDB user document via PUT.
    /// Caller is responsible for setting app_prefix_list before calling.
    /// Delegates to IUserRepository.
    /// </summary>
    public Task<document_put_response> PutUserAsync(
        user user,
        DBConfigurationDetail db_config)
    {
        return _userRepository.PutUserAsync(user, db_config);
    }

    /// <summary>
    /// Delete a CouchDB user document via DELETE.
    /// Delegates to IUserRepository.
    /// </summary>
    public Task<System.Dynamic.ExpandoObject> DeleteUserAsync(
        string user_id,
        string rev,
        DBConfigurationDetail db_config)
    {
        return _userRepository.DeleteUserAsync(user_id, rev, db_config);
    }

    /// <summary>
    /// Bulk create/update user_role_jurisdiction documents via CouchDB _bulk_docs.
    /// </summary>
    public Task<List<document_put_response>> BulkUpsertUserRoleJurisdictionsAsync(
        List<user_role_jurisdiction> user_role_jurisdictions,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.BulkUpsertUserRoleJurisdictionsAsync(user_role_jurisdictions, db_config);
    }

    public Task<get_response_header<user>> GetAllUsersAsync(
        int skip,
        int take,
        DBConfigurationDetail db_config)
    {
        return _userRepository.GetAllUsersAsync(skip, take, db_config);
    }

    public Task<get_response_header<user_role_jurisdiction>> GetAllUserRoleJurisdictionsAsync(DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.GetAllUserRoleJurisdictionsAsync(db_config);
    }

    public Task<user_role_jurisdiction> GetUserRoleJurisdictionAsync(
        string id,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.GetUserRoleJurisdictionAsync(id, db_config);
    }

    public Task<document_put_response> PutUserRoleJurisdictionAsync(
        user_role_jurisdiction item,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.PutUserRoleJurisdictionAsync(item, db_config);
    }

    public Task<document_put_response> DeleteUserRoleJurisdictionAsync(
        string id,
        string rev,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.DeleteUserRoleJurisdictionAsync(id, rev, db_config);
    }

    public Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewAsync(
        string request_string,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.GetUserRoleJurisdictionSortableViewAsync(request_string, db_config);
    }

    public Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewByParamsAsync(
        int skip,
        int take,
        string sortView,
        bool hasSearchKey,
        bool descending,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.GetUserRoleJurisdictionSortableViewByParamsAsync(skip, take, sortView, hasSearchKey, descending, db_config);
    }

    public Task<jurisdiction_tree> GetJurisdictionTreeAsync(DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.GetJurisdictionTreeAsync(db_config);
    }

    public async Task<Audit_Manage_User> GetAuditManageUserAsync(DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/{db_config.prefix}audit/audit-manage-user";
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);

        if (!string.IsNullOrWhiteSpace(responseFromServer) && responseFromServer.Contains("\"error\":\"not_found\""))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<Audit_Manage_User>(responseFromServer);
    }

    public Task<FormAccessSpecification> GetFormAccessAsync(DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.GetFormAccessAsync(db_config);
    }

    public Task<document_put_response> SaveFormAccessAsync(
        FormAccessSpecification request,
        DBConfigurationDetail db_config)
    {
        return _jurisdictionRepository.SaveFormAccessAsync(request, db_config);
    }
}
