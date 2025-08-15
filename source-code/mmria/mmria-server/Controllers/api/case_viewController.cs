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
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;

    public case_viewController  (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);

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
    public async Task<HashSet<string>> GetExistingRecordIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000");

            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
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