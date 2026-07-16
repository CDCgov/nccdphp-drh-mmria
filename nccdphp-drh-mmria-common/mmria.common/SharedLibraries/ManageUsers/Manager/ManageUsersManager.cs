using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.ManageUsers.DAL;
using mmria.common.SharedLibraries.ManageUsers.Model;
using mmria.common.SharedLibraries.Other;
using mmria.common.model.couchdb.audit;

namespace mmria.common.SharedLibraries.ManageUsers.Manager;

/// <summary>
/// Manager for Manage Users operations.
/// Contains business logic and orchestrates DAL calls for user CRUD and role assignment.
/// NO CouchDB calls in this class - all are delegated to ManageUsersDAL.
/// </summary>
public class ManageUsersManager
{
    private readonly ManageUsersDAL _dal;
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public ManageUsersManager(ManageUsersDAL dal, CouchDbHttpClient couchDbHttpClient)
    {
        _dal = dal;
        _couchDbHttpClient = couchDbHttpClient;
    }

    /// <summary>
    /// Check if a user exists by user_id.
    /// Returns true if the user exists, false if not found.
    /// </summary>
    public async Task<bool> CheckUserAsync(
        string user_id,
        DBConfigurationDetail db_config)
    {
        var existing = await _dal.CheckUserAsync(user_id, db_config);
        return !string.IsNullOrWhiteSpace(existing.name);
    }

    /// <summary>
    /// Create or update a user. Applies app_prefix_list logic before saving.
    /// For new users (_rev is null/empty), performs a server-side duplicate check
    /// before creation. Returns a response with ok=false if the user already exists.
    /// Preserves existing controller logic from userController.Post.
    /// </summary>
    public async Task<document_put_response> SaveUserAsync(
        user user,
        DBConfigurationDetail db_config)
    {
        document_put_response result = new document_put_response();

        // Server-side duplicate check for new user creation only.
            // When _rev is null/empty this is a create, not an update.
            if (string.IsNullOrWhiteSpace(user._rev))
            {
                var existing = await _dal.CheckUserAsync(user._id, db_config);
                if (!string.IsNullOrWhiteSpace(existing.name))
                {
                    Console.WriteLine($"SaveUserAsync: duplicate user rejected - {user._id}");
                    result.ok = false;
                    result.id = user._id;
                    return result;
                }
            }

            if(string.IsNullOrWhiteSpace(db_config.prefix))
            {
                if(user.app_prefix_list == null)
                {
                    user.app_prefix_list = new Dictionary<string, bool>();
                }

                if(user.app_prefix_list.Count == 0 || !user.app_prefix_list.ContainsKey("__no_prefix__"))
                {
                    user.app_prefix_list.Add("__no_prefix__", true);
                }
            }
            else if(!user.app_prefix_list.ContainsKey(db_config.prefix))
            {
                user.app_prefix_list.Add(db_config.prefix, true);
            }

            result = await _dal.PutUserAsync(user, db_config);

            if (!result.ok) 
            {

            }

        return result;
    }

    /// <summary>
    /// Delete a user. Fetches existing user, applies prefix logic to determine 
    /// hard delete vs prefix removal, then executes.
    /// Preserves existing controller logic from userController.Delete.
    /// Authorization must be performed by the caller before invoking this method.
    /// </summary>
    public async Task<System.Dynamic.ExpandoObject> DeleteUserAsync(
        string user_id,
        string rev,
        DBConfigurationDetail db_config)
    {
        bool is_only_remove_prefix = true;

            if (string.IsNullOrWhiteSpace(user_id) || string.IsNullOrWhiteSpace(rev)) 
            {
                return null;
            }

            // check if doc exists
            user user = null;

            try 
            {
                user = await _dal.GetUserAsync(user_id, db_config);
                
                if(string.IsNullOrWhiteSpace(db_config.prefix))
                {
                    if
                    (
                        user.app_prefix_list.Count == 0 ||
                        (
                            user.app_prefix_list.Count == 1 && 
                            user.app_prefix_list.ContainsKey("__no_prefix__")
                        )
                    )
                    {
                        is_only_remove_prefix = false;
                    }
                }
                else if(user.app_prefix_list.Count == 1 && user.app_prefix_list.ContainsKey(db_config.prefix))
                {
                    is_only_remove_prefix = false;
                }
            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine($"err ManageUsersManager.DeleteUserAsync\n{ex}");
            }

            if(is_only_remove_prefix == false)
            {
                var result = await _dal.DeleteUserAsync(user_id, rev, db_config);
                return result;
            }
            else if(user != null)
            {
                user.app_prefix_list.Remove(db_config.prefix);
                
                var put_response = await _dal.PutUserAsync(user, db_config);

                var result = new System.Dynamic.ExpandoObject();
                result.Append(new KeyValuePair<string, object>("ok", put_response.ok));
                result.Append(new KeyValuePair<string, object>("id", put_response.id));
                result.Append(new KeyValuePair<string, object>("rev", put_response.rev));

                return result;
            }

        return null;
    }

    /// <summary>
    /// Bulk create/update user_role_jurisdiction records.
    /// Preserves existing controller logic from user_role_jurisdictionController.PostBulk.
    /// Authorization must be performed by the caller before invoking this method.
    /// </summary>
    public async Task<List<document_put_response>> SaveUserRoleJurisdictionsAsync(
        List<user_role_jurisdiction> user_role_jurisdictions,
        DBConfigurationDetail db_config)
    {
        List<document_put_response> results = new List<document_put_response>();

        results = await _dal.BulkUpsertUserRoleJurisdictionsAsync(user_role_jurisdictions, db_config);
            
        return results;
    }

    public async Task<user> GetMyUserAsync(
        ClaimsPrincipal user,
        DBConfigurationDetail db_config)
    {
        var userName = GetCurrentUserName(user);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var userResult = await _dal.GetUserAsync($"org.couchdb.user:{userName}", db_config);
        ScrubUserSecrets(userResult);
        return userResult;
    }

    private static void ScrubUserSecrets(user u)
    {
        if (u == null) return;
        u.password = null;
        u.password_scheme = null;
        u.iterations = null;
        u.derived_key = null;
        u.salt = null;
    }

    public async Task<get_response_header<user>> GetUsersAsync(
        int skip,
        int take,
        ClaimsPrincipal user,
        DBConfigurationDetail db_config)
    {
        var jurisdiction_hashset = authorization.get_current_jurisdiction_id_set_for(db_config, user, _couchDbHttpClient);
        var jurisdiction_username_hashset = mmria.common.utils.authorization_case.get_user_jurisdiction_set(db_config, _couchDbHttpClient);
        var user_alldocs_response = await _dal.GetAllUsersAsync(skip, take, db_config);

        get_response_header<user> result = new get_response_header<user>();
        result.offset = user_alldocs_response.offset;
        result.total_rows = user_alldocs_response.total_rows;

        List<get_response_item<user>> temp_list = new List<get_response_item<user>>();
        foreach (get_response_item<user> uai in user_alldocs_response.rows)
        {
            bool is_jurisdiction_ok = false;
            bool is_app_prefix_ok = false;
            foreach (var jurisdiction_item in jurisdiction_hashset)
            {
                if (string.IsNullOrWhiteSpace(db_config.prefix))
                {
                    if (uai.doc.app_prefix_list == null || uai.doc.app_prefix_list.Count == 0)
                    {
                        is_app_prefix_ok = true;
                    }
                    else if (uai.doc.app_prefix_list.ContainsKey("__no_prefix__"))
                    {
                        is_app_prefix_ok = true;
                    }
                }
                else if (uai.doc.app_prefix_list.ContainsKey(db_config.prefix))
                {
                    is_app_prefix_ok = uai.doc.app_prefix_list[db_config.prefix];
                }

                if (jurisdiction_item.jurisdiction_id == "/")
                {
                    is_jurisdiction_ok = true;
                    break;
                }

                var regex = new Regex("^" + jurisdiction_item.jurisdiction_id);
                foreach (var jurisdiction_username in jurisdiction_username_hashset)
                {
                    if (regex.IsMatch(jurisdiction_username.jurisdiction_id) && uai.doc.name == jurisdiction_username.user_id)
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }
                }

                if (is_jurisdiction_ok)
                {
                    break;
                }
            }

            if (is_jurisdiction_ok && is_app_prefix_ok)
            {
                ScrubUserSecrets(uai.doc);
                temp_list.Add(uai);
            }
        }

        result.rows = temp_list;
        return result;
    }

    public async Task<user> GetUserAsync(string id, DBConfigurationDetail db_config)
    {
        var u = await _dal.GetUserAsync(id, db_config);
        ScrubUserSecrets(u);
        return u;
    }

    /// <summary>
    /// Returns the raw user document including credential fields
    /// (password_scheme, iterations, derived_key, salt). Use ONLY for server-side
    /// flows that must preserve credential material when round-tripping the
    /// document back to CouchDB (e.g. the user save path). Do NOT return the
    /// result of this method to clients.
    /// </summary>
    public async Task<user> GetUserRawAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetUserAsync(id, db_config);
    }

    public async Task<bool> IsAuthorizedToDeleteUserAsync(
        ClaimsPrincipal claimsPrincipal,
        user user,
        DBConfigurationDetail db_config)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.name))
        {
            return false;
        }

        var jurisdiction_hashset = authorization.get_current_jurisdiction_id_set_for(db_config, claimsPrincipal, _couchDbHttpClient);
        var user_role_response = await _dal.GetUserRoleJurisdictionSortableViewByParamsAsync(
            skip: 0, take: -1, sortView: "by_user_id", hasSearchKey: false, descending: false, db_config);

        foreach (get_sortable_view_response_item<user_role_jurisdiction> cvi in user_role_response.rows)
        {
            foreach ((string, ResourceRightEnum) jurisdiction_item in jurisdiction_hashset)
            {
                var regex = new Regex("^" + jurisdiction_item.Item1);
                if (cvi.value.jurisdiction_id == null)
                {
                    cvi.value.jurisdiction_id = "/";
                }

                if (regex.IsMatch(cvi.value.jurisdiction_id))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<IList<user_role_jurisdiction>> GetUserRoleJurisdictionsAsync(
        string p_urj_id,
        ClaimsPrincipal user,
        DBConfigurationDetail db_config)
    {
        var result = new List<user_role_jurisdiction>();
        var jurisdiction_hashset = authorization.get_current_jurisdiction_id_set_for(db_config, user, _couchDbHttpClient);

        if (string.IsNullOrWhiteSpace(p_urj_id))
        {
            var user_role_list = await _dal.GetAllUserRoleJurisdictionsAsync(db_config);
            foreach (var row in user_role_list.rows)
            {
                var item = row.doc;
                if
                (
                    item.data_type != null &&
                    item.data_type == user_role_jurisdiction.user_role_jursidiction_const &&
                    authorization.is_authorized_to_handle_jurisdiction_id(jurisdiction_hashset, ResourceRightEnum.ReadUser, item)
                )
                {
                    result.Add(item);
                }
            }
        }
        else
        {
            var item = await _dal.GetUserRoleJurisdictionAsync(p_urj_id, db_config);
            if
            (
                item.data_type != null &&
                item.data_type == user_role_jurisdiction.user_role_jursidiction_const &&
                authorization.is_authorized_to_handle_jurisdiction_id(jurisdiction_hashset, ResourceRightEnum.ReadUser, item)
            )
            {
                result.Add(item);
            }
        }

        return result;
    }

    public async Task<document_put_response> SaveUserRoleJurisdictionAsync(
        user_role_jurisdiction item,
        DBConfigurationDetail db_config)
    {
        return await _dal.PutUserRoleJurisdictionAsync(item, db_config);
    }

    public async Task<user_role_jurisdiction> GetUserRoleJurisdictionAsync(
        string id,
        DBConfigurationDetail db_config)
    {
        return await _dal.GetUserRoleJurisdictionAsync(id, db_config);
    }

    public async Task<document_put_response> DeleteUserRoleJurisdictionAsync(
        string id,
        string rev,
        DBConfigurationDetail db_config)
    {
        return await _dal.DeleteUserRoleJurisdictionAsync(id, rev, db_config);
    }

    public async Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetMyRolesAsync(
        ClaimsPrincipal user,
        DBConfigurationDetail db_config)
    {
        string search_key = GetCurrentUserName(user);
        var case_view_response = await _dal.GetUserRoleJurisdictionSortableViewByParamsAsync(
            skip: 0, take: -1, sortView: "by_date_created", hasSearchKey: false, descending: false, db_config);

        var result = new get_sortable_view_reponse_header<user_role_jurisdiction>();
        result.offset = case_view_response.offset;
        result.total_rows = case_view_response.total_rows;

        foreach (get_sortable_view_response_item<user_role_jurisdiction> cvi in case_view_response.rows)
        {
            if
            (
                cvi.value != null &&
                !string.IsNullOrWhiteSpace(cvi.value.user_id) &&
                cvi.value.user_id.Equals(search_key, StringComparison.OrdinalIgnoreCase)
            )
            {
                result.rows.Add(cvi);
            }
        }
        result.total_rows = result.rows.Count;

        if (result.total_rows == 0 && user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "installation_admin"))
        {
            var svri = new get_sortable_view_response_item<user_role_jurisdiction>();
            var urj = new user_role_jurisdiction
            {
                effective_start_date = DateTime.Now,
                role_name = "installation_admin",
                user_id = search_key,
                last_updated_by = "system",
                created_by = "system",
                date_created = DateTime.Now,
                date_last_updated = DateTime.Now
            };

            result.total_rows = 1;
            svri.id = "id";
            svri.key = "key";
            svri.value = urj;
            result.rows.Add(svri);
        }

        return result;
    }

    public async Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionViewAsync(
        int skip,
        int take,
        string sort,
        string search_key,
        bool descending,
        ClaimsPrincipal user,
        DBConfigurationDetail db_config)
    {
        var jurisdiction_hashset = await GetCurrentJurisdictionIdSetForViewAsync(user, db_config);
        string sort_view = sort.ToLower();
        switch (sort_view)
        {
            case "by_date_created":
            case "by_created_by":
            case "by_date_last_updated":
            case "by_last_updated_by":
            case "by_role_name":
            case "by_user_id":
            case "by_parent_id":
            case "by_jurisdiction_id":
            case "by_is_active":
            case "by_effective_start_date":
            case "by_effective_end_date":
                break;
            default:
                sort_view = "by_date_created";
                break;
        }

        var case_view_response = await _dal.GetUserRoleJurisdictionSortableViewByParamsAsync(
            skip: skip,
            take: take,
            sortView: sort_view,
            hasSearchKey: !string.IsNullOrWhiteSpace(search_key),
            descending: descending,
            db_config);

        if (string.IsNullOrWhiteSpace(search_key))
        {
            var result = new get_sortable_view_reponse_header<user_role_jurisdiction>();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach (get_sortable_view_response_item<user_role_jurisdiction> cvi in case_view_response.rows)
            {
                bool is_jurisdiction_ok = false;
                foreach (string jurisdiction_item in jurisdiction_hashset)
                {
                    var regex = new Regex("^" + jurisdiction_item);
                    if (cvi.value.jurisdiction_id == null)
                    {
                        cvi.value.jurisdiction_id = "/";
                    }

                    if (regex.IsMatch(cvi.value.jurisdiction_id))
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }
                }

                if (is_jurisdiction_ok)
                {
                    result.rows.Add(cvi);
                }
            }
            result.total_rows = result.rows.Count;
            return result;
        }

        string key_compare = search_key.ToLower().Trim(new char[] { '"' });
        {
            var result = new get_sortable_view_reponse_header<user_role_jurisdiction>();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach (get_sortable_view_response_item<user_role_jurisdiction> cvi in case_view_response.rows)
            {
                bool add_item = false;
                if (cvi.value.jurisdiction_id != null && cvi.value.jurisdiction_id.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
                {
                    add_item = true;
                }

                if (cvi.value.is_active != null && cvi.value.is_active.HasValue && bool.TryParse(key_compare, out bool is_active) && cvi.value.is_active.Value == is_active)
                {
                    add_item = true;
                }

                if (cvi.value.role_name != null && cvi.value.role_name.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
                {
                    add_item = true;
                }

                if (cvi.value.user_id != null && cvi.value.user_id.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
                {
                    add_item = true;
                }

                if (cvi.value.effective_start_date != null && cvi.value.effective_start_date.HasValue && DateTime.TryParse(key_compare, out DateTime is_date1) && cvi.value.effective_start_date.Value == is_date1)
                {
                    add_item = true;
                }

                if (cvi.value.effective_end_date != null && cvi.value.effective_end_date.HasValue && DateTime.TryParse(key_compare, out DateTime is_date2) && cvi.value.effective_end_date.Value == is_date2)
                {
                    add_item = true;
                }

                if (cvi.value.date_created != null && cvi.value.date_created.HasValue && DateTime.TryParse(key_compare, out DateTime is_date3) && cvi.value.date_created.Value == is_date3)
                {
                    add_item = true;
                }

                if (cvi.value.date_last_updated != null && cvi.value.date_last_updated.HasValue && DateTime.TryParse(key_compare, out DateTime is_date4) && cvi.value.date_last_updated.Value == is_date4)
                {
                    add_item = true;
                }

                if (cvi.value.created_by != null && cvi.value.created_by.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
                {
                    add_item = true;
                }

                if (cvi.value.last_updated_by != null && cvi.value.last_updated_by.Equals(key_compare, StringComparison.OrdinalIgnoreCase))
                {
                    add_item = true;
                }

                bool is_jurisdiction_ok = false;
                foreach (string jurisdiction_item in jurisdiction_hashset)
                {
                    var regex = new Regex("^" + jurisdiction_item);
                    if (cvi.value.jurisdiction_id == null)
                    {
                        cvi.value.jurisdiction_id = "/";
                    }

                    if (regex.IsMatch(cvi.value.jurisdiction_id))
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }
                }

                if (add_item && is_jurisdiction_ok)
                {
                    result.rows.Add(cvi);
                }
            }

            result.total_rows = result.rows.Count;
            result.rows = take > -1 ? result.rows.Skip(skip).Take(take).ToList() : result.rows.Skip(skip).ToList();
            return result;
        }
    }

    public async Task<Dictionary<string, object>> GetInitialDataAsync(
        ClaimsPrincipal user,
        OverridableConfiguration configuration,
        string host_prefix,
        DBConfigurationDetail db_config)
    {
        var result = new Dictionary<string, object>();
        result["policy_values"] = GetPolicyValues(configuration, host_prefix);
        result["my_roles"] = await GetUserRoleJurisdictionViewAsync(0, -1, "by_user_id", null, false, user, db_config);
        result["jurisdiction_tree"] = await _dal.GetJurisdictionTreeAsync(db_config);
        result["user_role_jurisdiction"] = await GetUserRoleJurisdictionViewAsync(0, -1, "by_user_id", null, false, user, db_config);
        result["user_list"] = await GetUsersAsync(1, 9000, user, db_config);
        result["manage_user_audit"] = await GetAuditDocumentSafeAsync(db_config);
        return result;
    }

    public async Task<FormAccessSpecification> GetFormAccessAsync(DBConfigurationDetail db_config)
    {
        try
        {
            return await _dal.GetFormAccessAsync(db_config);
        }
        catch (System.Net.WebException ex)
        {
            if (ex.Message.IndexOf("404") > -1)
            {
                return BuildDefaultFormAccessSpecification();
            }

            throw;
        }
    }

    public async Task<document_put_response> SaveFormAccessAsync(
        FormAccessSpecification request,
        string userName,
        DBConfigurationDetail db_config)
    {
        request.last_updated_by = userName;
        request.date_last_updated = DateTime.UtcNow;
        return await _dal.SaveFormAccessAsync(request, db_config);
    }

    private static string GetCurrentUserName(ClaimsPrincipal user)
    {
        if (user != null && user.Identities.Any(u => u.IsAuthenticated))
        {
            return user.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
        }

        return null;
    }

    private async Task<HashSet<string>> GetCurrentJurisdictionIdSetForViewAsync(
        ClaimsPrincipal claimsPrincipal,
        DBConfigurationDetail db_config)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!claimsPrincipal.HasClaim(c => c.Type == ClaimTypes.Name && c.Issuer == "https://contoso.com"))
        {
            return result;
        }

        if (claimsPrincipal.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "installation_admin"))
        {
            result.Add("/");
        }

        var user_name = claimsPrincipal.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value;
        var jurisdiction_view_response = await _dal.GetUserRoleJurisdictionSortableViewByParamsAsync(
            skip: 0, take: -1, sortView: "by_user_id", hasSearchKey: false, descending: false, db_config);

        var now = DateTime.Now;
        foreach (get_sortable_view_response_item<user_role_jurisdiction> jvi in jurisdiction_view_response.rows)
        {
            if (jvi.key != null && jvi.key == user_name)
            {
                if (jvi.value.is_active != null && jvi.value.is_active.HasValue && jvi.value.is_active.Value)
                {
                    bool add_item = true;

                    if (jvi.value.effective_start_date != null && jvi.value.effective_start_date.HasValue && jvi.value.effective_start_date > now)
                    {
                        add_item = false;
                    }

                    if (jvi.value.effective_end_date != null && jvi.value.effective_end_date.HasValue && jvi.value.effective_end_date.Value < now)
                    {
                        add_item = false;
                    }

                    if (add_item)
                    {
                        result.Add(jvi.value.jurisdiction_id);
                    }
                }
            }
        }

        return result;
    }

    private static Dictionary<string, string> GetPolicyValues(
        OverridableConfiguration configuration,
        string host_prefix)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var minimum_length = configuration.GetInteger("pass_word_minimum_length", host_prefix);
        var days_before_expires = configuration.GetInteger("pass_word_days_before_expires", host_prefix);
        var days_before_user_is_notified_of_expiration = configuration.GetInteger("pass_word_days_before_user_is_notified_of_expiration", host_prefix);
        var default_days_in_effective_date_interval = configuration.GetInteger("default_days_in_effective_date_interval", host_prefix);
        var unsuccessful_login_attempts_number_before_lockout = configuration.GetInteger("unsuccessful_login_attempts_number_before_lockout", host_prefix);
        var unsuccessful_login_attempts_within_number_of_minutes = configuration.GetInteger("unsuccessful_login_attempts_within_number_of_minutes", host_prefix);
        var unsuccessful_login_attempts_lockout_number_of_minutes = configuration.GetInteger("unsuccessful_login_attempts_lockout_number_of_minutes", host_prefix);
        var sams_is_enabled = configuration.GetBoolean("sams:is_enabled", host_prefix);

        result.Add("minimum_length", minimum_length.HasValue ? minimum_length.Value.ToString() : "");
        result.Add("days_before_expires", days_before_expires.HasValue ? days_before_expires.Value.ToString() : "");
        result.Add("days_before_user_is_notified_of_expiration", days_before_user_is_notified_of_expiration.HasValue ? days_before_user_is_notified_of_expiration.Value.ToString() : "");
        result.Add("default_days_in_effective_date_interval", default_days_in_effective_date_interval.HasValue ? default_days_in_effective_date_interval.Value.ToString() : "");
        result.Add("unsuccessful_login_attempts_number_before_lockout", unsuccessful_login_attempts_number_before_lockout.HasValue ? unsuccessful_login_attempts_number_before_lockout.Value.ToString() : "");
        result.Add("unsuccessful_login_attempts_within_number_of_minutes", unsuccessful_login_attempts_within_number_of_minutes.HasValue ? unsuccessful_login_attempts_within_number_of_minutes.Value.ToString() : "");
        result.Add("unsuccessful_login_attempts_lockout_number_of_minutes", unsuccessful_login_attempts_lockout_number_of_minutes.HasValue ? unsuccessful_login_attempts_lockout_number_of_minutes.Value.ToString() : "");
        result.Add("sams_is_enabled", sams_is_enabled.HasValue ? sams_is_enabled.Value.ToString() : "");

        return result;
    }

    private async Task<Audit_Manage_User> GetAuditDocumentSafeAsync(DBConfigurationDetail db_config)
    {
        try
        {
            return await _dal.GetAuditManageUserAsync(db_config);
        }
        catch
        {
            return null;
        }
    }

    private static FormAccessSpecification BuildDefaultFormAccessSpecification()
    {
        var result = new FormAccessSpecification
        {
            _id = "form-access-list",
            created_by = "system",
            date_created = DateTime.UtcNow,
            last_updated_by = "system",
            date_last_updated = DateTime.UtcNow
        };

        result.access_list.Add(new FormAccess { form_path = "/tracking", abstractor = "view, edit", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/demographic", abstractor = "view, edit", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/outcome", abstractor = "view, edit", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/cause_of_death", abstractor = "view, edit", data_analyst = "view", committee_member = "view, edit", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/preparer_remarks", abstractor = "view, edit", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/committee_review", abstractor = "view", data_analyst = "view", committee_member = "view, edit", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/vro_case_determination", abstractor = "view", data_analyst = "view", committee_member = "view", vro = "view, edit" });
        result.access_list.Add(new FormAccess { form_path = "/ije_dc", abstractor = "view", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/ije_bc", abstractor = "view", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/ije_fetaldc", abstractor = "view", data_analyst = "view", committee_member = "view", vro = "no_access" });
        result.access_list.Add(new FormAccess { form_path = "/amss_tracking", abstractor = "view, edit", data_analyst = "view", committee_member = "view, edit", vro = "no_access" });

        return result;
    }
}
