using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.ManageUsers.DAL;

namespace mmria.common.SharedLibraries.ManageUsers.Manager;

/// <summary>
/// Manager for Manage Users operations.
/// Contains business logic and orchestrates DAL calls for user CRUD and role assignment.
/// NO CouchDB calls in this class - all are delegated to ManageUsersDAL.
/// </summary>
public class ManageUsersManager
{
    private readonly ManageUsersDAL _dal;

    public ManageUsersManager(ManageUsersDAL dal)
    {
        _dal = dal;
    }

    /// <summary>
    /// Create or update a user. Applies app_prefix_list logic before saving.
    /// Preserves existing controller logic from userController.Post.
    /// </summary>
    public async Task<document_put_response> SaveUserAsync(
        user user,
        DBConfigurationDetail db_config)
    {
        document_put_response result = new document_put_response();

        try
        {
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
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex);
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
        try
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
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
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

        try
        {
            try
            {
                results = await _dal.BulkUpsertUserRoleJurisdictionsAsync(user_role_jurisdictions, db_config);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ManageUsersManager.SaveUserRoleJurisdictionsAsync:{ex}");
            }
        }
        catch(Exception ex) 
        {
            Console.WriteLine($"{ex}");
        }
            
        return results;
    }
}
