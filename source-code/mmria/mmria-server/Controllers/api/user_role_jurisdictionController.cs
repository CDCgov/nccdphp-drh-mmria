using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.ManageUsers.Manager;

using  mmria.server.extension; 
namespace mmria.server;

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


    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.model.couchdb.user_role_jurisdiction user_role_jurisdiction
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {
            #if !IS_PMSS_ENHANCED
            if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteUser, user_role_jurisdiction, _couchDbHttpClient))
            {
                return null;
            }
            #endif
            #if IS_PMSS_ENHANCED
            if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction, _couchDbHttpClient))
            {
                return null;
            }
            #endif

            result = await _manageUsersManager.SaveUserRoleJurisdictionAsync(user_role_jurisdiction, db_config);
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    }

    [HttpPost("bulk")]
    public async System.Threading.Tasks.Task<List<mmria.common.model.couchdb.document_put_response>> PostBulk
    (
        [FromBody] List<mmria.common.model.couchdb.user_role_jurisdiction> user_role_jurisdictions
    ) 
    { 
        try
        {
            #if !IS_PMSS_ENHANCED
            foreach (var user_role_jurisdiction in user_role_jurisdictions)
            {
                if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteUser, user_role_jurisdiction, _couchDbHttpClient))
                {
                    return null;
                }
            }
            #endif
            #if IS_PMSS_ENHANCED
            foreach (var user_role_jurisdiction in user_role_jurisdictions)
            {
                if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, mmria.pmss.server.utils.ResourceRightEnum.WriteUser, user_role_jurisdiction, _couchDbHttpClient))
                {
                    return null;
                }
            }
            #endif

            return await _manageUsersManager.SaveUserRoleJurisdictionsAsync(user_role_jurisdictions, db_config);
        }
        catch(Exception ex) 
        {
            Log.Information($"{ex}");
        }
            
        return new List<mmria.common.model.couchdb.document_put_response>();
    }


    [HttpDelete]
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

