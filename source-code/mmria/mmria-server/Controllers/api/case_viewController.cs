#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.functional;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/[controller]")]
public sealed class case_viewController: ControllerBase 
{  

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;

    public case_viewController  (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);

    }

    [HttpGet]
    public async Task<mmria.common.model.couchdb.case_view_response> Get
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,
        bool descending = false,
        string case_status = "all",
        string field_selection = "all",
        string pregnancy_relatedness ="all",
        string date_of_death_range = "all",
        string date_of_review_range = "all",
        bool include_pinned_cases = false

    ) 
    {
        /*
        System.Console.WriteLine("case_viewController.Get");
        System.Console.WriteLine($"host_prefix = {host_prefix}");
        System.Console.WriteLine($"db_config.url = {db_config.url}");
        System.Console.WriteLine($"db_config.prefix = {db_config.prefix}");
        */
        
        var is_identefied_case = true;
        var cvs = new mmria.server.utils.CaseViewSearch
        (
            db_config, 
            User,
            is_identefied_case,
            include_pinned_cases
        );

        var result = await cvs.execute
        (
            cancellationToken,
            skip,
            take,
            sort,
            search_key,
            descending,
            case_status,
            field_selection,
            pregnancy_relatedness,
            date_of_death_range,
            date_of_review_range
        );


        return result;
    }



    [HttpGet("record-id-list")]
    public async Task<System.Collections.Generic.List<string>> GetRecordIdList(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var case_view_curl = new cURL(
                "GET",
                null,
                db_config.url + $"/{db_config.prefix}mmrds/_design/sortable/_view/record_id_list",
                null,
                db_config.user_name,
                db_config.user_value
            );

            var case_view_response = await case_view_curl.executeAsync();
            var case_view_result = System.Text.Json.JsonSerializer.Deserialize<mmria.common.model.couchdb.case_view_response>(case_view_response);

            var result = new System.Collections.Generic.List<string>();

            foreach (var item in case_view_result.rows)
            {
                result.Add(item.value.record_id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return new System.Collections.Generic.List<string>();
    }

   

    [HttpGet("offline-documents")]
    public async Task<mmria.common.model.couchdb.case_view_response> GetOfflineDocuments
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        bool descending = false
    )
    {
        try
        {
            Console.WriteLine($"GetOfflineDocuments called by user: {User.Identity?.Name}");
            
            var current_user = User.Identity?.Name;
            if (string.IsNullOrEmpty(current_user))
            {
                Console.WriteLine("User identity not found");
                return new mmria.common.model.couchdb.case_view_response();
            }

            // For debugging, let's get all documents and filter to see what we have
            // Using a larger limit to ensure we get all potential offline documents
            var large_limit = 10000; // Get many documents to ensure we don't miss any
            
            var sort_view = sort switch
            {
                "by_date_created" => "by_date_created",
                "by_date_last_updated" => "by_date_last_updated",
                "by_last_name" => "by_last_name",
                "by_first_name" => "by_first_name",
                "by_middle_name" => "by_middle_name",
                "by_year_of_death" => "by_year_of_death",
                "by_month_of_death" => "by_month_of_death",
                "by_committee_review_date" => "by_committee_review_date",
                "by_created_by" => "by_created_by",
                "by_last_updated_by" => "by_last_updated_by",
                "by_state_of_death" => "by_state_of_death",
                "by_record_id" => "by_record_id",
                _ => "by_date_created"
            };

            // Get all documents to see what's available (force view update)
            var descending_text = descending ? "&descending=true" : "";
            var request_string = db_config.Get_Prefix_DB_Url($"mmrds/_design/sortable/_view/{sort_view}?skip=0&limit={large_limit}{descending_text}&update=true");

            Console.WriteLine($"Executing CouchDB query: {request_string}");

            var case_view_curl = new cURL(
                "GET",
                null,
                request_string,
                null,
                db_config.user_name,
                db_config.user_value
            );

            var case_view_response = await case_view_curl.executeAsync();
            
            if (string.IsNullOrEmpty(case_view_response))
            {
                Console.WriteLine("Empty response from CouchDB");
                return new mmria.common.model.couchdb.case_view_response();
            }

            Console.WriteLine($"CouchDB response length: {case_view_response.Length}");
            Console.WriteLine($"First 500 chars of response: {case_view_response.Substring(0, Math.Min(500, case_view_response.Length))}");

            // Use Newtonsoft.Json for consistency with ToggleOfflineStatus method
            var case_view_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(case_view_response);
            
            if (case_view_result?.rows == null)
            {
                Console.WriteLine("No rows found in CouchDB response");
                return new mmria.common.model.couchdb.case_view_response();
            }

            // Filter to only include documents that are offline and created by the current user
            Console.WriteLine($"Total rows retrieved from CouchDB: {case_view_result.rows.Count}");
            
            var all_by_user = case_view_result.rows.Where(row => 
                row?.value != null && 
                string.Equals(row.value.offline_by, current_user, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            Console.WriteLine($"Documents created by current user ({current_user}): {all_by_user.Count}");
            
            var offline_by_user = all_by_user.Where(row => 
            {
                var is_offline = false;
                
                // Handle different possible types for is_offline field
                if (row.value.is_offline.HasValue)
                {
                    is_offline = row.value.is_offline.Value;
                }
                else
                {
                    // Check if the raw JSON might have this as a different type
                    Console.WriteLine($"Document {row.value.record_id}: is_offline is null, checking raw data...");
                }
                
                Console.WriteLine($"Document {row.value.record_id}: is_offline={is_offline}, created_by={row.value.created_by}, offline_by={row.value.offline_by}, offline_date={row.value.offline_date}");
                return is_offline;
            }).ToList();

            Console.WriteLine($"Offline documents created by current user: {offline_by_user.Count}");

            var filtered_rows = offline_by_user.Skip(skip).Take(take).ToList();

            Console.WriteLine($"Final filtered results (after skip/take): {filtered_rows.Count}");

            // Create a new response with filtered results
            var result = new mmria.common.model.couchdb.case_view_response
            {
                total_rows = offline_by_user.Count, // Total offline documents for this user
                offset = skip,
                rows = filtered_rows
            };

            Console.WriteLine($"Returning {filtered_rows.Count} documents out of {offline_by_user.Count} total offline documents");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in GetOfflineDocuments: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new mmria.common.model.couchdb.case_view_response();
        }
    }

    public async Task<HashSet<string>> GetExistingRecordIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000");

            var case_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                result.Add(cvi.value.record_id);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

} 

#endif