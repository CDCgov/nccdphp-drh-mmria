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

namespace mmria.common.SharedLibraries.ManageUsers.DAL;

/// <summary>
/// Data Access Layer for Manage Users operations.
/// Contains ALL CouchDB calls for user CRUD and user_role_jurisdiction bulk operations.
/// No business logic - only data operations.
/// </summary>
public class ManageUsersDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public ManageUsersDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get a CouchDB user document by user_id (e.g. "org.couchdb.user:someone").
    /// </summary>
    public async Task<user> GetUserAsync(
        string user_id,
        DBConfigurationDetail db_config)
    {
        string request_string = db_config.url + "/_users/" + user_id;
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        var result = JsonConvert.DeserializeObject<user>(responseFromServer);
        return result;
    }

    /// <summary>
    /// Check if a CouchDB user document exists by user_id.
    /// Returns the user if found, or an empty user object if not found or on error.
    /// Never returns null.
    /// </summary>
    public async Task<user> CheckUserAsync(
        string user_id,
        DBConfigurationDetail db_config)
    {
        try
        {
            string request_string = db_config.url + "/_users/" + user_id;
            string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);

            if(string.IsNullOrWhiteSpace(responseFromServer))
            {
                // Empty response (treat as not found)
                return new user();
            }
            else if(responseFromServer.Contains("\"error\"") && responseFromServer.Contains("not_found"))
            {
                // CouchDB not_found JSON – return empty object so caller can treat as "available"
                return new user();
            }
            else
            {
                return JsonConvert.DeserializeObject<user>(responseFromServer) 
                       ?? new user();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            // Fall back to empty object rather than null
            return new user();
        }
    }

    /// <summary>
    /// Create or update a CouchDB user document via PUT.
    /// Caller is responsible for setting app_prefix_list before calling.
    /// </summary>
    public async Task<document_put_response> PutUserAsync(
        user user,
        DBConfigurationDetail db_config)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.NullValueHandling = NullValueHandling.Ignore;
        string object_string = JsonConvert.SerializeObject(user, settings);

        string user_db_url = db_config.url + "/_users/" + user._id;

        string responseFromServer = await _httpClient.ExecuteAsync("PUT", user_db_url, object_string, db_config.user_name, db_config.user_value);
        var result = JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
        return result;
    }

    /// <summary>
    /// Delete a CouchDB user document via DELETE.
    /// </summary>
    public async Task<System.Dynamic.ExpandoObject> DeleteUserAsync(
        string user_id,
        string rev,
        DBConfigurationDetail db_config)
    {
        string request_string = db_config.url + "/_users/" + user_id + "?rev=" + rev;
        string responseFromServer = await _httpClient.ExecuteAsync("DELETE", request_string, null, db_config.user_name, db_config.user_value);
        var result = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);
        return result;
    }

    /// <summary>
    /// Bulk create/update user_role_jurisdiction documents via CouchDB _bulk_docs.
    /// </summary>
    public async Task<List<document_put_response>> BulkUpsertUserRoleJurisdictionsAsync(
        List<user_role_jurisdiction> user_role_jurisdictions,
        DBConfigurationDetail db_config)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.NullValueHandling = NullValueHandling.Ignore;
        string user_role_jurisdictions_json = JsonConvert.SerializeObject(new { docs = user_role_jurisdictions }, settings);

        string bulk_docs_url = db_config.url + $"/{db_config.prefix}jurisdiction/_bulk_docs";

        string responseFromServer = await _httpClient.ExecuteAsync("POST", bulk_docs_url, user_role_jurisdictions_json, db_config.user_name, db_config.user_value);
        var results = JsonConvert.DeserializeObject<List<document_put_response>>(responseFromServer);
        return results;
    }

    public async Task<get_response_header<user>> GetAllUsersAsync(
        int skip,
        int take,
        DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/_users/_all_docs?include_docs=true&skip={skip}&limit={take}";
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<get_response_header<user>>(responseFromServer);
    }

    public async Task<get_response_header<user_role_jurisdiction>> GetAllUserRoleJurisdictionsAsync(DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/{db_config.prefix}jurisdiction/_all_docs?include_docs=true";
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<get_response_header<user_role_jurisdiction>>(responseFromServer);
    }

    public async Task<user_role_jurisdiction> GetUserRoleJurisdictionAsync(
        string id,
        DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/{db_config.prefix}jurisdiction/{id}";
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<user_role_jurisdiction>(responseFromServer);
    }

    public async Task<document_put_response> PutUserRoleJurisdictionAsync(
        user_role_jurisdiction item,
        DBConfigurationDetail db_config)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.NullValueHandling = NullValueHandling.Ignore;
        string object_string = JsonConvert.SerializeObject(item, settings);

        string request_string = $"{db_config.url}/{db_config.prefix}jurisdiction/{item._id}";
        string responseFromServer = await _httpClient.ExecuteAsync("PUT", request_string, object_string, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
    }

    public async Task<document_put_response> DeleteUserRoleJurisdictionAsync(
        string id,
        string rev,
        DBConfigurationDetail db_config)
    {
        string request_string = $"{db_config.url}/{db_config.prefix}jurisdiction/{id}?rev={rev}";
        string responseFromServer = await _httpClient.ExecuteAsync("DELETE", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
    }

    public async Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewAsync(
        string request_string,
        DBConfigurationDetail db_config)
    {
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<user_role_jurisdiction>>(responseFromServer);
    }

    public async Task<jurisdiction_tree> GetJurisdictionTreeAsync(DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<jurisdiction_tree>(responseFromServer);
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

    public async Task<FormAccessSpecification> GetFormAccessAsync(DBConfigurationDetail db_config)
    {
        string request_string = db_config.Get_Prefix_DB_Url("jurisdiction/form-access-list");
        string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, null, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<FormAccessSpecification>(responseFromServer);
    }

    public async Task<document_put_response> SaveFormAccessAsync(
        FormAccessSpecification request,
        DBConfigurationDetail db_config)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.NullValueHandling = NullValueHandling.Ignore;
        string object_string = JsonConvert.SerializeObject(request, settings);

        string request_string = db_config.Get_Prefix_DB_Url("jurisdiction/form-access-list");
        string responseFromServer = await _httpClient.ExecuteAsync("PUT", request_string, object_string, db_config.user_name, db_config.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(responseFromServer);
    }
}
