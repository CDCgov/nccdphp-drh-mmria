using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

using mmria.common;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class aggregate_reportController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;

    public aggregate_reportController  
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IList<mmria.server.model.c_report_object>> Get()
    {

        List<mmria.server.model.c_report_object> result =  new List<mmria.server.model.c_report_object>();

        System.Console.WriteLine ("Recieved message.");


        try
        {
            string request_string = db_config.Get_Prefix_DB_Url("report/_all_docs?include_docs=true");


            var request_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await request_curl.executeAsync();

            System.Dynamic.ExpandoObject expando_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer, new  Newtonsoft.Json.Converters.ExpandoObjectConverter());

            IDictionary<string,object> all_docs_dictionary = expando_result as IDictionary<string,object>;
            List<object> row_list = null;

            if(all_docs_dictionary != null && all_docs_dictionary.ContainsKey("rows"))
            {
                row_list = all_docs_dictionary ["rows"] as List<object> ;
            }

            if(row_list != null)
            foreach (object row_item in row_list)
            {
                IDictionary<string, object> row_dictionary = row_item as IDictionary<string, object>; 

                if(row_dictionary != null && row_dictionary.ContainsKey("doc"))
                {

                    KeyValuePair<bool,mmria.server.model.c_report_object> convert_result = convert(row_dictionary["doc"]  as IDictionary<string,object>);


                    if(convert_result.Key)
                    {
                        var item = convert_result.Value;
                        
            //home_record/date_of_death/year ne 9999 
            // and committee_review/date_of_review is not missing
                        if
                        (
                            item.year_of_death.HasValue && 
                            item.year_of_death.Value != 9999 &&
                            item.year_of_case_review.HasValue
                        )
                        {
                            result.Add(item);
                        }
                    }
                }

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }


        //return result;
        return result;
    } 


    private KeyValuePair<bool,mmria.server.model.c_report_object> convert (IDictionary<string, object> p_item)
    {
        
        mmria.server.model.c_report_object  temp = new mmria.server.model.c_report_object ();
        bool is_complete_conversion = true;

        temp._id = p_item ["_id"].ToString ();
    
        int val = 0;


        try
        {

            if (p_item.ContainsKey("year_of_death") &&  p_item ["year_of_death"] != null && int.TryParse (p_item ["year_of_death"].ToString (), out val)) 
            {
                temp.year_of_death = val;
            }
            if (p_item.ContainsKey("year_of_case_review") &&  p_item ["year_of_case_review"] != null && int.TryParse (p_item ["year_of_case_review"].ToString (), out val))
            {
                temp.year_of_case_review = val; 
            }
            if (p_item.ContainsKey("month_of_case_review") &&  p_item ["month_of_case_review"] != null && int.TryParse (p_item ["month_of_case_review"].ToString (), out val)) 
            {
                temp.month_of_case_review = val; 
            }


            IDictionary<string, object> current_dictionary = p_item ["total_number_of_cases_by_pregnancy_relatedness"] as IDictionary<string, object>;

            if(current_dictionary != null)
            {
                temp.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related = int.Parse (current_dictionary ["pregnancy_related"].ToString ());
                temp.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related = int.Parse (current_dictionary ["pregnancy_associated_but_not_related"].ToString ());
                temp.total_number_of_cases_by_pregnancy_relatedness.not_pregnancy_related_or_associated = int.Parse (current_dictionary ["not_pregnancy_related_or_associated"].ToString ());
                temp.total_number_of_cases_by_pregnancy_relatedness.unable_to_determine = int.Parse (current_dictionary ["unable_to_determine"].ToString ());
                temp.total_number_of_cases_by_pregnancy_relatedness.blank = int.Parse (current_dictionary ["blank"].ToString ());
            }
            current_dictionary = p_item ["total_number_of_pregnancy_related_deaths_by_ethnicity"] as IDictionary<string, object>;
        
            if(current_dictionary != null)
            {
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.blank = int.Parse (current_dictionary ["blank"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.hispanic = int.Parse (current_dictionary ["hispanic"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.non_hispanic_black = int.Parse (current_dictionary ["non_hispanic_black"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.non_hispanic_white = int.Parse (current_dictionary ["non_hispanic_white"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.american_indian_alaska_native = int.Parse (current_dictionary ["american_indian_alaska_native"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.native_hawaiian = int.Parse (current_dictionary ["native_hawaiian"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.guamanian_or_chamorro = int.Parse (current_dictionary ["guamanian_or_chamorro"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.samoan = int.Parse (current_dictionary ["samoan"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.other_pacific_islander = int.Parse (current_dictionary ["other_pacific_islander"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.asian_indian = int.Parse (current_dictionary ["asian_indian"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.filipino = int.Parse (current_dictionary ["filipino"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.korean = int.Parse (current_dictionary ["korean"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.other_asian = int.Parse (current_dictionary ["other_asian"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.chinese = int.Parse (current_dictionary ["chinese"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.japanese = int.Parse (current_dictionary ["japanese"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.vietnamese = int.Parse (current_dictionary ["vietnamese"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_ethnicity.other = int.Parse (current_dictionary ["other"].ToString ());
            }

            current_dictionary = p_item ["total_number_of_pregnancy_associated_by_ethnicity"] as IDictionary<string, object>;

            if(current_dictionary != null)
            {		
                temp.total_number_of_pregnancy_associated_by_ethnicity.blank = int.Parse (current_dictionary ["blank"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.hispanic = int.Parse (current_dictionary ["hispanic"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.non_hispanic_black = int.Parse (current_dictionary ["non_hispanic_black"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.non_hispanic_white = int.Parse (current_dictionary ["non_hispanic_white"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.american_indian_alaska_native = int.Parse (current_dictionary ["american_indian_alaska_native"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.native_hawaiian = int.Parse (current_dictionary ["native_hawaiian"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.guamanian_or_chamorro = int.Parse (current_dictionary ["guamanian_or_chamorro"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.samoan = int.Parse (current_dictionary ["samoan"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.other_pacific_islander = int.Parse (current_dictionary ["other_pacific_islander"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.asian_indian = int.Parse (current_dictionary ["asian_indian"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.filipino = int.Parse (current_dictionary ["filipino"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.korean = int.Parse (current_dictionary ["korean"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.other_asian = int.Parse (current_dictionary ["other_asian"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.chinese = int.Parse (current_dictionary ["chinese"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.japanese = int.Parse (current_dictionary ["japanese"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.vietnamese = int.Parse (current_dictionary ["vietnamese"].ToString ());
                temp.total_number_of_pregnancy_associated_by_ethnicity.other = int.Parse (current_dictionary ["other"].ToString ());
            }

            current_dictionary = p_item ["total_number_of_pregnancy_related_deaths_by_age"] as IDictionary<string, object>;

            if(current_dictionary != null)
            {
                temp.total_number_of_pregnancy_related_deaths_by_age.age_less_than_20 = int.Parse (current_dictionary ["age_less_than_20"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_age.age_20_to_24 = int.Parse (current_dictionary ["age_20_to_24"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_age.age_25_to_29 = int.Parse (current_dictionary ["age_25_to_29"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_age.age_30_to_34 = int.Parse (current_dictionary ["age_30_to_34"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_age.age_35_to_44 = int.Parse (current_dictionary ["age_35_to_44"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_age.age_45_and_above = int.Parse (current_dictionary ["age_45_and_above"].ToString ());
                temp.total_number_of_pregnancy_related_deaths_by_age.blank = int.Parse (current_dictionary ["blank"].ToString ());
            }

            current_dictionary = p_item ["total_number_of_pregnancy_associated_deaths_by_age"] as IDictionary<string, object>;

            if(current_dictionary != null)
            {
                temp.total_number_of_pregnancy_associated_deaths_by_age.age_less_than_20 = int.Parse (current_dictionary ["age_less_than_20"].ToString ());
                temp.total_number_of_pregnancy_associated_deaths_by_age.age_20_to_24 = int.Parse (current_dictionary ["age_20_to_24"].ToString ());
                temp.total_number_of_pregnancy_associated_deaths_by_age.age_25_to_29 = int.Parse (current_dictionary ["age_25_to_29"].ToString ());
                temp.total_number_of_pregnancy_associated_deaths_by_age.age_30_to_34 = int.Parse (current_dictionary ["age_30_to_34"].ToString ());
                temp.total_number_of_pregnancy_associated_deaths_by_age.age_35_to_44 = int.Parse (current_dictionary ["age_35_to_44"].ToString ());
                temp.total_number_of_pregnancy_associated_deaths_by_age.age_45_and_above = int.Parse (current_dictionary ["age_45_and_above"].ToString ());
                temp.total_number_of_pregnancy_associated_deaths_by_age.blank = int.Parse (current_dictionary ["blank"].ToString ());
            }


            current_dictionary = p_item ["total_number_pregnancy_related_at_time_of_death"] as IDictionary<string, object>;

            if(current_dictionary != null)
            {
                temp.total_number_pregnancy_related_at_time_of_death.pregnant_at_the_time_of_death = int.Parse (current_dictionary ["pregnant_at_the_time_of_death"].ToString ());
                temp.total_number_pregnancy_related_at_time_of_death.pregnant_within_42_days_of_death = int.Parse (current_dictionary ["pregnant_within_42_days_of_death"].ToString ());
                temp.total_number_pregnancy_related_at_time_of_death.pregnant_within_43_to_365_days_of_death = int.Parse (current_dictionary ["pregnant_within_43_to_365_days_of_death"].ToString ());
                temp.total_number_pregnancy_related_at_time_of_death.blank = int.Parse (current_dictionary ["blank"].ToString ());
            }

            current_dictionary = p_item ["total_number_pregnancy_associated_at_time_of_death"] as IDictionary<string, object>;

            if(current_dictionary != null)
            {
                temp.total_number_pregnancy_associated_at_time_of_death.pregnant_at_the_time_of_death = int.Parse (current_dictionary ["pregnant_at_the_time_of_death"].ToString ());
                temp.total_number_pregnancy_associated_at_time_of_death.pregnant_within_42_days_of_death = int.Parse (current_dictionary ["pregnant_within_42_days_of_death"].ToString ());
                temp.total_number_pregnancy_associated_at_time_of_death.pregnant_within_43_to_365_days_of_death = int.Parse (current_dictionary ["pregnant_within_43_to_365_days_of_death"].ToString ());
                temp.total_number_pregnancy_associated_at_time_of_death.blank = int.Parse (current_dictionary ["blank"].ToString ());
            }



        this.popluate_list(ref temp.distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm, p_item ["distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm"] as IDictionary<string, object>);



        this.popluate_list(ref temp.total_pregnancy_related_determined_to_be_preventable, p_item ["total_pregnancy_related_determined_to_be_preventable"] as IDictionary<string, object>);
        this.popluate_list(ref temp.total_pregnancy_associated_determined_to_be_preventable, p_item ["total_pregnancy_associated_determined_to_be_preventable"] as IDictionary<string, object>);


        this.popluate_list(ref temp.total_pregnancy_related_obesity_contributed_to_the_death, p_item ["total_pregnancy_related_obesity_contributed_to_the_death"] as IDictionary<string, object>);
        this.popluate_list(ref temp.total_pregnancy_associated_obesity_contributed_to_the_death, p_item ["total_pregnancy_associated_obesity_contributed_to_the_death"] as IDictionary<string, object>);


        this.popluate_list(ref temp.total_pregnancy_related_mental_health_conditions_contributed_to_death, p_item ["total_pregnancy_related_mental_health_conditions_contributed_to_death"] as IDictionary<string, object>);
        this.popluate_list(ref temp.total_pregnancy_associated_mental_health_conditions_contributed_to_death, p_item ["total_pregnancy_associated_mental_health_conditions_contributed_to_death"] as IDictionary<string, object>);



        this.popluate_list(ref temp.total_pregnancy_related_substance_use_disorder_contributed_to_death, p_item ["total_pregnancy_related_substance_use_disorder_contributed_to_death"] as IDictionary<string, object>);
        this.popluate_list(ref temp.total_pregnancy_associated_substance_use_disorder_contributed_to_death, p_item ["total_pregnancy_associated_substance_use_disorder_contributed_to_death"] as IDictionary<string, object>);


        this.popluate_list(ref temp.total_pregnancy_related_is_suicide, p_item ["total_pregnancy_related_is_suicide"] as IDictionary<string, object>);
        this.popluate_list(ref temp.total_pregnancy_associated_is_suicide, p_item ["total_pregnancy_associated_is_suicide"] as IDictionary<string, object>);

        this.popluate_list(ref temp.total_pregnancy_related_is_homocide, p_item ["total_pregnancy_related_is_homocide"] as IDictionary<string, object>);
        this.popluate_list(ref temp.total_pregnancy_associated_is_homocide, p_item ["total_pregnancy_associated_is_homocide"] as IDictionary<string, object>);



        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
            is_complete_conversion = false;
        }

        
        return new KeyValuePair<bool,mmria.server.model.c_report_object>(is_complete_conversion, temp);
    }


    private void popluate_list (ref System.Collections.Generic.Dictionary<string, int> p_result, IDictionary<string, object> p_source_object)
    {

        //p_result = new System.Collections.Generic.Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);

        foreach(var kvp in p_source_object)
        {
            p_result.Add(kvp.Key, int.Parse(kvp.Value.ToString()));
        }
        
    }
} 


