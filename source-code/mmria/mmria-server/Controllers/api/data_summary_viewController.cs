
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
[Route("/api/data-summary/{skip}")]
public sealed class data_summary_viewControllerController: ControllerBase 
{  

    struct Selector_Struc
    {
        //public System.Dynamic.ExpandoObject selector;
        public System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>> selector;

        public string use_index;

        public int limit;

        public int skip;
    } 
    
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public data_summary_viewControllerController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
    public async Task<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.server.model.SummaryReport.FrequencySummaryDocument>> Get(string skip)
    {
        var result = new mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.server.model.SummaryReport.FrequencySummaryDocument>();
        
        const int take = 100;
        int skip_number = 0;

        int.TryParse(skip, out skip_number);

        var config_couchdb_url = db_config.url;
        var config_timer_user_name = db_config.user_name;
        var config_timer_value = db_config.user_value;
        var config_db_prefix = db_config.prefix;
        
        try
        {

            string find_url = $"{config_couchdb_url}/{config_db_prefix}report/_design/data_summary_view_report/_view/year_of_death?skip={skip_number}&limit={take}";
            var case_curl = new cURL("GET", null, find_url, null, config_timer_user_name, config_timer_value);
            string responseFromServer = await case_curl.executeAsync();
            
            #if !IS_PMSS_ENHANCED
                var jurisdiction_hashset = mmria.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, User);
            #endif
            #if IS_PMSS_ENHANCED
                var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, User);
            #endif

            List<mmria.server.model.SummaryReport.FrequencySummaryDocument> new_list = new();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.server.model.SummaryReport.FrequencySummaryDocument>>(responseFromServer);

            /*if(!string.IsNullOrWhiteSpace(skip))
            {
                foreach(var doc in response_result.rows)
                {
                    if(doc.key.ToLower() == skip.ToLower())
                    {
                        foreach(var jurisdiction_item in  jurisdiction_hashset)
                        {
                            var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.jurisdiction_id);
                            if
                            (
                                regex.IsMatch(doc.value.case_folder) && 
                                utils.ResourceRightEnum.ReadCase ==  jurisdiction_item.ResourceRight
                            )
                            {
                                if
                                (
                                    doc.value.year_of_death.HasValue && doc.value.year_of_death.Value != 9999 &&
                                    doc.value.month_of_death.HasValue && doc.value.month_of_death.Value != 9999 &&
                                    doc.value.day_of_death.HasValue && doc.value.day_of_death.Value != 9999 &&
                                    doc.value.day_of_case_review.HasValue && doc.value.day_of_case_review.Value != 9999 && 
                                    doc.value.month_of_case_review.HasValue && doc.value.month_of_case_review.Value != 9999 &&
                                    doc.value.year_of_case_review.HasValue && doc.value.year_of_case_review.Value != 9999
                                )
                                {
                                    result.Add(doc.value);
                                }
                                break;
                            }
                        }
                    }
                    
                }
            }
            else
            {
                foreach(var doc in result.rows)
                {
                    foreach(var jurisdiction_item in  jurisdiction_hashset)
                    {
                        var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.jurisdiction_id);
                        if
                        (
                            regex.IsMatch(doc.value.case_folder) //&& 
                            //utils.ResourceRightEnum.ReadCase ==  jurisdiction_item.ResourceRight
                        )
                        {
                            
                            if
                            (
                                doc.value.year_of_death.HasValue && doc.value.year_of_death.Value != 9999 &&
                                doc.value.month_of_death.HasValue && doc.value.month_of_death.Value != 9999 &&
                                doc.value.day_of_death.HasValue && doc.value.day_of_death.Value != 9999 &&
                                doc.value.day_of_case_review.HasValue && doc.value.day_of_case_review.Value != 9999 && 
                                doc.value.month_of_case_review.HasValue && doc.value.month_of_case_review.Value != 9999 &&
                                doc.value.year_of_case_review.HasValue && doc.value.year_of_case_review.Value != 9999
                            )
                            {
                                result.Add(doc.value);
                            //}
                            break;
                        }
                    }
                }
            //}


            System.Console.WriteLine($"case_response.docs.length {result.Count}");*/
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }




} 


