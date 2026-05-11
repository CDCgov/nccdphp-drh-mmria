using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using mmria.common.model;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.ManageUsers.Manager;
using mmria.common.utils;

using  mmria.server.extension;
using mmria.server.util;
namespace mmria.server;


[Route("api/[controller]")]
public sealed class userController: ControllerBase 
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;

    IHttpContextAccessor httpContextAccessor;
    string host_prefix = null;
    private readonly ManageUsersManager _manageUsersManager;

    public userController
	(
        IHttpContextAccessor p_httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        ManageUsersManager manageUsersManager
    )
    {
        _manageUsersManager = manageUsersManager;
        httpContextAccessor = p_httpContextAccessor; 
        
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        
        configuration = tenantRuntime.RequireConfiguration();
        
        db_config = tenantRuntime.RequireDbConfig();
    }
    
    [Authorize(Roles  = "abstractor,data_analyst")]
    [Route("my-user")]
    [HttpGet]
    public async System.Threading.Tasks.Task<UserResponse> GetMyUser() 
    { 
        try
        {
            var user = await _manageUsersManager.GetMyUserAsync(httpContextAccessor.HttpContext.User, db_config);
            return ToResponse(user);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
        
    } 

    //public IEnumerable<mmria.common.model.couchdb.user_alldocs_response> Get() 
    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.get_response_header<UserResponse>> Get(int skip = 1, int take = 9000) 
    { 

        try
        {
            var users = await _manageUsersManager.GetUsersAsync(skip, take, httpContextAccessor.HttpContext.User, db_config);
            return ToResponseHeader(users);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
        //return new mmria.common.model.couchdb.user[] { default(mmria.common.model.couchdb.user), default(mmria.common.model.couchdb.user) }; 
    } 

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    public async System.Threading.Tasks.Task<UserResponse> Get(string id) 
    { 
        mmria.common.model.couchdb.user result = null;
        try
        {
            result = await _manageUsersManager.GetUserAsync(id, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return ToResponse(result); 
    }


    [Authorize(Roles = "jurisdiction_admin,installation_admin")]
    [Route("check-user/{id}")]
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> CheckUser(string id)
    {
        bool exists = await _manageUsersManager.CheckUserAsync(id, db_config);
        if (exists)
        {
            return Ok();
        }
        return NotFound();
    }

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post() 
    { 
        var user = await JsonRequestBodyReader.ReadAsync<UserSaveRequest>(Request);
        var sanitizedUser = await CreateSanitizedUserAsync(user);
        if (sanitizedUser == null)
        {
            return new mmria.common.model.couchdb.document_put_response
            {
                ok = false,
                error_description = "Invalid user payload."
            };
        }

        var result = await _manageUsersManager.SaveUserAsync(sanitizedUser, db_config);
        if (result == null || !result.ok)
        {
            var revisionHandling = string.IsNullOrWhiteSpace(sanitizedUser._rev)
                ? "omitted"
                : "resolved_existing";
            Console.WriteLine(
                $"User save failed for {sanitizedUser._id}: rev={revisionHandling}; response={result?.error_description}");
        }

        return result;
    } 

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpDelete]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Delete(string user_id = null, string rev = null) 
    { 
        try
        {
            if (string.IsNullOrWhiteSpace(user_id) || string.IsNullOrWhiteSpace(rev)) 
            {
                return null;
            }

            // Authorization check must remain in controller (server-only dependency)
            try 
            {
                var user = await _manageUsersManager.GetUserAsync(user_id, db_config);
                if(!await _manageUsersManager.IsAuthorizedToDeleteUserAsync(User, user, db_config))
                {
                    return null;
                }
            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Delete\n{ex}");
            }

            return await _manageUsersManager.DeleteUserAsync(user_id, rev, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

    public sealed class UserSaveRequest
    {
        public string _id { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public bool is_active { get; set; }
        public bool is_enabled { get; set; }
        public string open_id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string alternate_email { get; set; }
    }

    /// <summary>
    /// Response DTO for user GET endpoints. Intentionally omits CouchDB credential
    /// material (password, password_scheme, iterations, derived_key, salt) so PBKDF2
    /// hash material is not exposed over the wire. Crypto fields remain in the
    /// underlying CouchDB user document and are managed server-side only.
    /// </summary>
    public sealed class UserResponse
    {
        public string _id { get; set; }
        public string _rev { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string[] roles { get; set; }
        public bool is_active { get; set; }
        public bool is_enabled { get; set; }
        public string open_id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string alternate_email { get; set; }
        public System.Collections.Generic.Dictionary<string, bool> app_prefix_list { get; set; }
    }

    private static UserResponse ToResponse(mmria.common.model.couchdb.user source)
    {
        if (source == null)
        {
            return null;
        }

        return new UserResponse
        {
            _id = source._id,
            _rev = source._rev,
            name = source.name,
            type = source.type,
            roles = source.roles,
            is_active = source.is_active,
            is_enabled = source.is_enabled,
            open_id = source.open_id,
            email = source.email,
            first_name = source.first_name,
            last_name = source.last_name,
            alternate_email = source.alternate_email,
            app_prefix_list = source.app_prefix_list,
        };
    }

    private static mmria.common.model.couchdb.get_response_header<UserResponse> ToResponseHeader(
        mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user> source)
    {
        if (source == null)
        {
            return null;
        }

        var result = new mmria.common.model.couchdb.get_response_header<UserResponse>
        {
            offset = source.offset,
            total_rows = source.total_rows,
            rows = new System.Collections.Generic.List<mmria.common.model.couchdb.get_response_item<UserResponse>>()
        };

        if (source.rows != null)
        {
            foreach (var row in source.rows)
            {
                result.rows.Add(new mmria.common.model.couchdb.get_response_item<UserResponse>
                {
                    id = row?.id,
                    key = row?.key,
                    value = row?.value,
                    doc = ToResponse(row?.doc)
                });
            }
        }

        return result;
    }

    private async Task<mmria.common.model.couchdb.user> CreateSanitizedUserAsync(UserSaveRequest request)
    {
        if (!TryNormalizeUserIdentity(request, out var userName, out var userId))
        {
            return null;
        }

        mmria.common.model.couchdb.user existingUser = null;
        try
        {
            existingUser = await _manageUsersManager.GetUserAsync(userId, db_config);
        }
        catch
        {
            // Treat missing users as creates. Other failures will surface on save.
        }

        var sanitizedUser = existingUser ?? new mmria.common.model.couchdb.user();
        sanitizedUser._id = userId;
        sanitizedUser._rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(null, existingUser?._rev);
        sanitizedUser.name = userName;
        sanitizedUser.type = "user";
        sanitizedUser.roles = existingUser?.roles ?? Array.Empty<string>();
        sanitizedUser.is_active = request.is_active;
        sanitizedUser.is_enabled = request.is_enabled;
        sanitizedUser.open_id = request.open_id;
        sanitizedUser.email = request.email;
        sanitizedUser.first_name = request.first_name;
        sanitizedUser.last_name = request.last_name;
        sanitizedUser.alternate_email = request.alternate_email;
        sanitizedUser.app_prefix_list = existingUser?.app_prefix_list ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        sanitizedUser.password_scheme = existingUser?.password_scheme ?? "pbkdf2";
        sanitizedUser.iterations = existingUser?.iterations ?? 10;
        sanitizedUser.derived_key = existingUser?.derived_key;
        sanitizedUser.salt = existingUser?.salt;
        sanitizedUser.password = string.IsNullOrWhiteSpace(request.password) ? null : request.password;

        return sanitizedUser;
    }

    private static bool TryNormalizeUserIdentity(
        UserSaveRequest request,
        out string userName,
        out string userId)
    {
        const string userIdPrefix = "org.couchdb.user:";

        userName = null;
        userId = null;

        if (request == null)
        {
            return false;
        }

        var requestName = request.name?.Trim();
        var requestId = request._id?.Trim();
        string idName = null;

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            if (!requestId.StartsWith(userIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            idName = requestId.Substring(userIdPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(idName))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(requestName) &&
            !string.IsNullOrWhiteSpace(idName) &&
            !requestName.Equals(idName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        userName = !string.IsNullOrWhiteSpace(idName) ? idName : requestName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        userId = $"{userIdPrefix}{userName}";
        return true;
    }

} 


