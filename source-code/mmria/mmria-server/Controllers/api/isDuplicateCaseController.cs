using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Dynamic;
using mmria.common;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server;

public sealed class IsDuplicateCaseRequest
{
    public IsDuplicateCaseRequest(){}

    public string FirstName {get;set;}
    public string LastName {get;set;}

    public int MonthOfDeath {get;set;}
    public int DayOfDeath {get;set;}
    public int YearOfDeath {get;set;}
    public string StateOfDeath {get;set;}

}

[Authorize(Roles  = "abstractor")]
[Route("api/[controller]")]
public sealed class isDuplicateCaseController: ControllerBase 
{ 
    ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public isDuplicateCaseController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _actorSystem = actorSystem;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
    
    
    [HttpPost]
    public async Task<bool> Post([FromBody] IsDuplicateCaseRequest DuplicateCaseRequest) 
    { 

        var result = false;
        try
        {
            var case_view_response = await GetCaseView(DuplicateCaseRequest.LastName);
            string mmria_id = null;

            var gs = new migrate.C_Get_Set_Value(new System.Text.StringBuilder());

            string record_id = null;

            if (case_view_response.total_rows > 0)
            {
                foreach (var kvp in case_view_response.rows)
                {
                    if
                    (
                        //kvp.value.host_state.Equals(DuplicateCaseRequest.StateOfDeath, StringComparison.OrdinalIgnoreCase) &&
                        kvp.value.last_name.Equals(DuplicateCaseRequest.LastName, StringComparison.OrdinalIgnoreCase) &&
                        kvp.value.first_name.Equals(DuplicateCaseRequest.FirstName, StringComparison.OrdinalIgnoreCase) &&
                        kvp.value.date_of_death_year == DuplicateCaseRequest.YearOfDeath &&
                        kvp.value.date_of_death_month == DuplicateCaseRequest.MonthOfDeath
                    )
                    {
                        var case_expando_object = await GetCaseById(kvp.id);
                        if (case_expando_object != null)
                        {

                            migrate.C_Get_Set_Value.get_value_result value_result = gs.get_value(case_expando_object, "_id");
                            mmria_id = value_result.result.ToString();


                            var DSTATE_result = gs.get_value(case_expando_object, "home_record/state_of_death_record");
                            var host_state_result = gs.get_value(case_expando_object, "host_state");
                            var DOD_YR_result = gs.get_value(case_expando_object, "home_record/date_of_death/year");
                            var DOD_MO_result = gs.get_value(case_expando_object, "home_record/date_of_death/month");
                            var DOD_DY_result = gs.get_value(case_expando_object, "home_record/date_of_death/day");
                            var LNAME_result = gs.get_value(case_expando_object, "home_record/last_name");
                            var GNAME_result = gs.get_value(case_expando_object, "home_record/first_name");

                            if
                            (
                                DOD_YR_result.is_error == false &&
                                host_state_result.is_error == false &&
                                DOD_MO_result.is_error == false &&
                                DOD_DY_result.is_error == false &&
                                LNAME_result.is_error == false &&
                                GNAME_result.is_error == false
                            )
                            {
                                if
                                (
                                    DSTATE_result.result.ToString().Equals(DuplicateCaseRequest.StateOfDeath, StringComparison.OrdinalIgnoreCase) &&
                                    LNAME_result.result.ToString().Equals(DuplicateCaseRequest.LastName, StringComparison.OrdinalIgnoreCase) &&
                                    GNAME_result.result.ToString().Equals(DuplicateCaseRequest.FirstName, StringComparison.OrdinalIgnoreCase) &&
                                    DOD_YR_result.result!= null &&
                                    DOD_MO_result.result!= null &&
                                    DOD_DY_result.result!= null
                                )
                                {

                                    int DOD_YR_result_Check = -1;
                                    int DOD_MO_result_Check = -1;
                                    int DOD_DY_result_Check = -1;

                                    if(
                                        int.TryParse(DOD_YR_result.result.ToString(), out DOD_YR_result_Check) &&
                                        int.TryParse(DOD_MO_result.result.ToString(), out DOD_MO_result_Check) &&
                                        int.TryParse(DOD_DY_result.result.ToString(), out DOD_DY_result_Check) &&
                                        DOD_YR_result_Check == DuplicateCaseRequest.YearOfDeath &&
                                        DOD_MO_result_Check == DuplicateCaseRequest.MonthOfDeath &&
                                        DOD_DY_result_Check == DuplicateCaseRequest.DayOfDeath

                                    )
                                    {
                                        var record_id_result = gs.get_value(case_expando_object, "home_record/record_id");
                                        if(!record_id_result.is_error && record_id_result.result!= null)
                                        {
                                            record_id = record_id_result.result.ToString();
                                        }
                                        result = true;
                                        break;
                                    }
                                    else
                                    {
                                        System.Console.WriteLine("inner check 5");
                                    }
                                }
                                else
                                {
                                    System.Console.WriteLine("inner check 4");
                                }
                            }
                            else
                            {
                                System.Console.WriteLine("inner check 3");
                            }

                        }
                        else
                        {
                            System.Console.WriteLine("inner check 2");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("inner check 1");
                    }
                }

            }
            else
            {
                System.Console.WriteLine("No CaseView Rows found");
            }


        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return result;
    } 

    private async Task<mmria.common.model.couchdb.case_view_response> GetCaseView
    (
        string search_key,
        int skip = 0,
        int take = 268_435_456, // max couchdb take value
        string sort = "by_last_name",
        bool descending = false,
        string case_status = "all"
    )
    {
        string sort_view = sort.ToLower();
        switch (sort_view)
        {
            case "by_date_created":
            case "by_date_last_updated":
            case "by_last_name":
            case "by_first_name":
            case "by_middle_name":
            case "by_year_of_death":
            case "by_month_of_death":
            case "by_committee_review_date":
            case "by_created_by":
            case "by_last_updated_by":
            case "by_state_of_death":
            case "by_date_last_checked_out":
            case "by_last_checked_out_by":

            case "by_case_status":
                break;

            default:
                sort_view = "by_date_created";
                break;
        }



        try
        {
            System.Text.StringBuilder request_builder = new System.Text.StringBuilder();
            request_builder.Append(db_config.Get_Prefix_DB_Url($"mmrds/_design/sortable/_view/{sort_view}?"));

            if (skip > -1)
            {
                request_builder.Append($"skip={skip}");
            }
            else
            {

                request_builder.Append("skip=0");
            }

            if (take > -1)
            {
                request_builder.Append($"&limit={take}");
            }

            if (descending)
            {
                request_builder.Append("&descending=true");
            }


            string request_string = request_builder.ToString();
            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);


            string key_compare = search_key.ToLower().Trim(new char[] { '"' });

            mmria.common.model.couchdb.case_view_response result = new mmria.common.model.couchdb.case_view_response();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                bool add_item = false;

                if (is_matching_search_text(cvi.value.last_name, key_compare))
                {
                    add_item = true;
                }

                if (add_item)
                {
                    result.rows.Add(cvi);
                }

            }


            result.total_rows = result.rows.Count;
            result.rows = result.rows.Skip(skip).Take(take).ToList();

            return result;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

        }


        return null;
    }

    private bool is_matching_search_text(string p_val1, string p_val2)
    {
        var result = false;

        if
        (
            !string.IsNullOrWhiteSpace(p_val1) &&
            //p_val1.Length > 3 &&
            (
                p_val2.IndexOf(p_val1, StringComparison.OrdinalIgnoreCase) > -1 ||
                p_val1.IndexOf(p_val2, StringComparison.OrdinalIgnoreCase) > -1
            )
        )
        {
            result = true;
        }

        return result;
    }

    public async Task<System.Dynamic.ExpandoObject> GetCaseById(string case_id)
    {
        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("mmrds/_all_docs?include_docs=true");

            if (!string.IsNullOrWhiteSpace(case_id))
            {
                request_string = db_config.Get_Prefix_DB_Url($"mmrds/{case_id}");
                var case_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                string responseFromServer = await case_curl.executeAsync();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);

                return result;

            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

} 


