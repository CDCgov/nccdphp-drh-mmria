using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.ManageUsers.Manager;
using mmria.common.utils;

using  mmria.server.extension; 
using mmria.server.util;
namespace mmria.server;

[Authorize]
[Route("api/[controller]")]
public sealed class user_role_jurisdictionController: ControllerBase 
{ 
     IHttpContextAccessor httpContextAccessor;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly ManageUsersManager _manageUsersManager;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public user_role_jurisdictionController
    (
        IHttpContextAccessor p_httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        ManageUsersManager manageUsersManager,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _manageUsersManager = manageUsersManager;
        _couchDbHttpClient = couchDbHttpClient;
        httpContextAccessor = p_httpContextAccessor;

        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IList<mmria.common.model.couchdb.user_role_jurisdiction>> Get(string p_urj_id)
    {
        //Log.Information  ("Recieved message.");
        var result = new List<mmria.common.model.couchdb.user_role_jurisdiction>();

        try
        {
            return await _manageUsersManager.GetUserRoleJurisdictionsAsync(p_urj_id, httpContextAccessor.HttpContext.User, db_config);
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }


    [Authorize(Roles = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post() 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
        var user_role_jurisdiction = await JsonRequestBodyReader.ReadAsync<UserRoleJurisdictionSaveRequest>(Request);
        var safeUserRoleJurisdiction = await CreateSanitizedUserRoleJurisdictionAsync(user_role_jurisdiction);

        if (safeUserRoleJurisdiction == null)
        {
            return result;
        }

        try
        {
            #if !IS_PMSS_ENHANCED
            if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteUser, safeUserRoleJurisdiction, _couchDbHttpClient))
            {
                return null;
            }
            #endif
            #if IS_PMSS_ENHANCED
            if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, safeUserRoleJurisdiction, _couchDbHttpClient))
            {
                return null;
            }
            #endif

            result = await _manageUsersManager.SaveUserRoleJurisdictionAsync(safeUserRoleJurisdiction, db_config);
            if (result == null || !result.ok)
            {
                var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(
                    user_role_jurisdiction?._rev,
                    safeUserRoleJurisdiction._rev);
                Log.Information(
                    "user_role_jurisdiction save failed for {DocumentId}. rev={RevisionHandling}; response={Response}",
                    safeUserRoleJurisdiction._id,
                    revisionHandling,
                    result?.error_description);
            }
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    }

    [Authorize(Roles = "jurisdiction_admin,installation_admin")]
    [HttpPost("bulk")]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<List<mmria.common.model.couchdb.document_put_response>> PostBulk() 
    { 
        var user_role_jurisdictions = await JsonRequestBodyReader.ReadAsync<List<UserRoleJurisdictionSaveRequest>>(Request);
        var safeUserRoleJurisdictions = await CreateSanitizedUserRoleJurisdictionsAsync(user_role_jurisdictions);
        if (safeUserRoleJurisdictions == null)
        {
            return new List<mmria.common.model.couchdb.document_put_response>();
        }

        try
        {
            #if !IS_PMSS_ENHANCED
            foreach (var user_role_jurisdiction in safeUserRoleJurisdictions)
            {
                if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteUser, user_role_jurisdiction, _couchDbHttpClient))
                {
                    return null;
                }
            }
            #endif
            #if IS_PMSS_ENHANCED
            foreach (var user_role_jurisdiction in safeUserRoleJurisdictions)
            {
                if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction, _couchDbHttpClient))
                {
                    return null;
                }
            }
            #endif

            var results = await _manageUsersManager.SaveUserRoleJurisdictionsAsync(safeUserRoleJurisdictions, db_config);
            for (var index = 0; index < safeUserRoleJurisdictions.Count && index < results.Count; index++)
            {
                if (results[index] == null || !results[index].ok)
                {
                    var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(
                        user_role_jurisdictions?[index]?._rev,
                        safeUserRoleJurisdictions[index]._rev);
                    Log.Information(
                        "user_role_jurisdiction bulk save failed for {DocumentId}. rev={RevisionHandling}; response={Response}",
                        safeUserRoleJurisdictions[index]._id,
                        revisionHandling,
                        results[index]?.error_description);
                }
            }

            return results;
        }
        catch(Exception ex) 
        {
            Log.Information($"{ex}");
        }

        return new List<mmria.common.model.couchdb.document_put_response>();
    }

    public sealed class UserRoleJurisdictionSaveRequest
    {
        public string _id { get; set; }
        public string _rev { get; set; }
        public bool? _deleted { get; set; }
        public string parent_id { get; set; }
        public string role_name { get; set; }
        public string user_id { get; set; }
        public string jurisdiction_id { get; set; }
        public string application_namespace { get; set; }
        public DateTime? effective_start_date { get; set; }
        public DateTime? effective_end_date { get; set; }
        public bool? is_active { get; set; }
    }

    private async System.Threading.Tasks.Task<List<mmria.common.model.couchdb.user_role_jurisdiction>> CreateSanitizedUserRoleJurisdictionsAsync(
        List<UserRoleJurisdictionSaveRequest> requests)
    {
        if (requests == null)
        {
            return null;
        }

        var results = new List<mmria.common.model.couchdb.user_role_jurisdiction>();
        foreach (var request in requests)
        {
            var sanitized = await CreateSanitizedUserRoleJurisdictionAsync(request);
            if (sanitized == null)
            {
                return null;
            }

            results.Add(sanitized);
        }

        return results;
    }

    private async System.Threading.Tasks.Task<mmria.common.model.couchdb.user_role_jurisdiction> CreateSanitizedUserRoleJurisdictionAsync(
        UserRoleJurisdictionSaveRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        mmria.common.model.couchdb.user_role_jurisdiction existingItem = null;
        try
        {
            existingItem = await _manageUsersManager.GetUserRoleJurisdictionAsync(request._id.Trim(), db_config);
        }
        catch
        {
            // Missing documents are treated as creates.
        }

        var currentUserName = GetCurrentUserName();
        return new mmria.common.model.couchdb.user_role_jurisdiction
        {
            _id = request._id.Trim(),
            _rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(request._rev, existingItem?._rev),
            _deleted = request._deleted,
            parent_id = NormalizeOptionalString(request.parent_id),
            role_name = NormalizeOptionalString(request.role_name),
            user_id = NormalizeOptionalString(request.user_id),
            jurisdiction_id = NormalizeOptionalString(request.jurisdiction_id),
            application_namespace = NormalizeOptionalString(request.application_namespace),
            effective_start_date = request.effective_start_date,
            effective_end_date = request.effective_end_date,
            is_active = request.is_active,
            date_created = existingItem?.date_created ?? DateTime.UtcNow,
            created_by = !string.IsNullOrWhiteSpace(existingItem?.created_by) ? existingItem.created_by : currentUserName,
            date_last_updated = DateTime.UtcNow,
            last_updated_by = currentUserName,
            data_type = existingItem?.data_type ?? mmria.common.model.couchdb.user_role_jurisdiction.user_role_jursidiction_const
        };
    }

    private string GetCurrentUserName()
    {
        if (User?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            return User.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name)
                .Value;
        }

        return null;
    }

    private static string NormalizeOptionalString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }


    [Authorize(Roles = "jurisdiction_admin,installation_admin")]
    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Delete(string _id = null, string rev = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return BadRequest(new { error = "missing_id" });
            }

            // Prefer authoritative rev from the DB; fall back to client-provided rev when necessary.
            string delete_rev = rev;

            try
            {
                var check_document_curl_result = await _manageUsersManager.GetUserRoleJurisdictionAsync(_id, db_config);

                #if !IS_PMSS_ENHANCED
                if (!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteUser, check_document_curl_result, _couchDbHttpClient))
                {
                    return Forbid();
                }
                #endif
                #if IS_PMSS_ENHANCED
                if (!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, check_document_curl_result, _couchDbHttpClient))
                {
                    return Forbid();
                }
                #endif

                if (!string.IsNullOrWhiteSpace(check_document_curl_result._rev))
                {
                    delete_rev = check_document_curl_result._rev; // prefer DB rev
                }
            }
            catch (Exception ex)
            {
                // If GET failed, surface a 502 so caller knows the backing DB couldn't be read.
                Log.Information($"user_role_jurisdictionController.Delete: error fetching doc {_id}: {ex}");
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "failed_to_fetch_document" });
            }

            if (string.IsNullOrWhiteSpace(delete_rev))
            {
                return BadRequest(new { error = "missing_rev" });
            }

            try
            {
                var result = await _manageUsersManager.DeleteUserRoleJurisdictionAsync(_id, delete_rev, db_config);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Information($"user_role_jurisdictionController.Delete: error deleting doc {_id}: {ex}");
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "failed_to_delete_document" });
            }
        }
        catch (Exception ex)
        {
            Log.Information($"user_role_jurisdictionController.Delete: unexpected error: {ex}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "internal_error" });
        }
    }


} 

