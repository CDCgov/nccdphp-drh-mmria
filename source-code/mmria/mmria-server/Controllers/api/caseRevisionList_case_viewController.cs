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

[Authorize(Roles  = "installation_admin")]
[Route("api/[controller]")]
public sealed class caseRevisionList_case_viewController: ControllerBase 
{  
    mmria.common.couchdb.OverridableConfiguration configuration;

    delegate bool is_valid_predicate(mmria.common.model.couchdb.case_view_item item);
 
    public caseRevisionList_case_viewController
    (

        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
    }


    [HttpGet]
    public async Task<mmria.common.model.couchdb.case_view_response> Get
    (
        System.Threading.CancellationToken cancellationToken,
        string jurisdiction_id,
        string search_key
    ) 
    {
        var config = configuration.GetDBConfig(jurisdiction_id);

        System.Text.StringBuilder request_builder = new System.Text.StringBuilder ();

        string request_string = $"{config.url}/{config.prefix}mmrds/_design/sortable/_view/by_date_created?&skip=0&take=100&field_selection=by_record_id&search_key={System.Net.WebUtility.UrlEncode(search_key)}";
        
        var case_view_curl = new cURL("GET", null, request_string, null, config.user_name, config.user_value);
        string responseFromServer = await case_view_curl.executeAsync();


        mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

        mmria.common.model.couchdb.case_view_response result = new mmria.common.model.couchdb.case_view_response();
        result.offset = case_view_response.offset;
        result.total_rows = case_view_response.total_rows;

        is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
            {
                bool result = false;
                if(is_matching_search_text(item.value.record_id, search_key))
                {
                    result = true;
                }

                return result;
            };

        
        var data = case_view_response.rows
            .Where
            (
                cvi => f(cvi)
            );

        


        result.total_rows = data.Count();
        result.rows =  data.ToList ();
    

        return result;

      
    }

    bool is_matching_search_text(string p_val1, string p_val2)
    {
        var result = false;

        if 
        (
            !string.IsNullOrWhiteSpace(p_val1) && 
            p_val1.Length > 1 &&
            (
                //p_val2.IndexOf (p_val1, StringComparison.OrdinalIgnoreCase) > -1 //||
                p_val1.IndexOf (p_val2, StringComparison.OrdinalIgnoreCase) > -1
            )
        )
        {
            result = true;
        }

        return result;
    }
} 

