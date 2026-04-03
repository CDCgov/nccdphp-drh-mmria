using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Serilog;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using mmria.common.utils;

using  mmria.server.extension;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class jurisdiction_treeController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    public class case_folder_metadata
    {
      public string Name { get; set; }
      public string ParentName { get; set; }
      public int NestedLevel { get; set; }
    }
    public jurisdiction_treeController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _couchDbHttpClient = couchDbHttpClient;
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> Get()
    {
        Log.Information  ("Recieved message.");
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");

            string response_from_server = await _couchDbHttpClient.ExecuteAsync("GET", jurisdiction_tree_url, null, db_config.user_name, db_config.user_value);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Route("new_case_folder")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> GetJurisdictionTree()
    {
        Log.Information  ("Recieved message.");
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");

            string response_from_server = await _couchDbHttpClient.ExecuteAsync("GET", jurisdiction_tree_url, null, db_config.user_name, db_config.user_value);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.model.couchdb.jurisdiction_tree jurisdiction_tree
    ) 
    { 
        string jurisdiction_json;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
            }

            var existingTree = await GetCurrentJurisdictionTreeAsync();
            var resolvedRevision = CouchDbRevisionHelper.ResolveServerOwnedRevision(
                jurisdiction_tree?._rev,
                existingTree?._rev);
            var revisionHandling = DescribeRevisionHandling(
                jurisdiction_tree?._rev,
                existingTree?._rev,
                resolvedRevision);

            var sanitizedJurisdictionTree = CreateSanitizedJurisdictionTree(
                jurisdiction_tree,
                userName,
                resolvedRevision);
            if (sanitizedJurisdictionTree == null)
            {
                return result;
            }

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            jurisdiction_json = Newtonsoft.Json.JsonConvert.SerializeObject(sanitizedJurisdictionTree, settings);

            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");

            try
            {
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync("PUT", jurisdiction_tree_url, jurisdiction_json, db_config.user_name, db_config.user_value);
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                if (result == null || !result.ok)
                {
                    Log.Warning(
                        "jurisdiction_tree save failed for {DocumentId}. rev={RevisionHandling}; response={Response}",
                        "jurisdiction/jurisdiction_tree",
                        revisionHandling,
                        responseFromServer);
                }
            }
            catch(Exception ex)
            {
                Log.Information ($"jurisdiction_treeController:{ex}");
            }

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }
            
        return result;
    } 

    private static mmria.common.model.couchdb.jurisdiction_tree CreateSanitizedJurisdictionTree(
        mmria.common.model.couchdb.jurisdiction_tree request,
        string currentUserName,
        string resolvedRevision)
    {
        if (request == null)
        {
            return null;
        }

        return new mmria.common.model.couchdb.jurisdiction_tree
        {
            _rev = resolvedRevision,
            date_created = request.date_created == default ? DateTime.UtcNow : request.date_created,
            created_by = string.IsNullOrWhiteSpace(request.created_by) ? currentUserName : SanitizeSingleLineText(request.created_by, 256),
            date_last_updated = DateTime.UtcNow,
            last_updated_by = SanitizeSingleLineText(currentUserName, 256),
            children = request.children?
                .Where(child => child != null)
                .Select(child => CreateSanitizedJurisdiction(child, currentUserName))
                .Where(child => child != null)
                .ToArray() ?? Array.Empty<mmria.common.model.couchdb.jurisdiction>()
        };
    }

    private static mmria.common.model.couchdb.jurisdiction CreateSanitizedJurisdiction(
        mmria.common.model.couchdb.jurisdiction request,
        string currentUserName)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.id))
        {
            return null;
        }

        return new mmria.common.model.couchdb.jurisdiction
        {
            id = SanitizeSingleLineText(request.id, 256),
            name = SanitizeSingleLineText(request.name, 256),
            date_created = request.date_created == default ? DateTime.UtcNow : request.date_created,
            created_by = string.IsNullOrWhiteSpace(request.created_by) ? currentUserName : SanitizeSingleLineText(request.created_by, 256),
            date_last_updated = DateTime.UtcNow,
            last_updated_by = SanitizeSingleLineText(currentUserName, 256),
            is_active = request.is_active,
            is_enabled = request.is_enabled,
            parent_id = SanitizeSingleLineText(request.parent_id, 256),
            children = request.children?
                .Where(child => child != null)
                .Select(child => CreateSanitizedJurisdiction(child, currentUserName))
                .Where(child => child != null)
                .ToList() ?? new List<mmria.common.model.couchdb.jurisdiction>()
        };
    }

    private static string SanitizeSingleLineText(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }

    private async System.Threading.Tasks.Task<mmria.common.model.couchdb.jurisdiction_tree> GetCurrentJurisdictionTreeAsync()
    {
        try
        {
            string jurisdiction_tree_url = db_config.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");
            string response_from_server = await _couchDbHttpClient.ExecuteAsync("GET", jurisdiction_tree_url, null, db_config.user_name, db_config.user_value);

            if (!string.IsNullOrWhiteSpace(response_from_server) &&
                response_from_server.Contains("\"error\":\"not_found\"", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);
        }
        catch
        {
            return null;
        }
    }

    private static string DescribeRevisionHandling(string incoming, string existing, string resolved)
    {
        var normalizedIncoming = CouchDbRevisionHelper.NormalizeOptionalRevision(incoming);
        var normalizedExisting = CouchDbRevisionHelper.NormalizeOptionalRevision(existing);

        if (string.IsNullOrWhiteSpace(resolved))
        {
            if (!string.IsNullOrWhiteSpace(normalizedIncoming) &&
                !CouchDbRevisionHelper.IsValidRevision(normalizedIncoming))
            {
                return "rejected_invalid";
            }

            return "omitted";
        }

        if (!string.IsNullOrWhiteSpace(normalizedExisting) &&
            string.Equals(resolved, normalizedExisting, StringComparison.Ordinal))
        {
            return "resolved_existing";
        }

        return "preserved_incoming";
    }

} 


