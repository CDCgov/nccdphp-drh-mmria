#if IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;

using mmria.server.utils;
namespace mmria.pmss.server.utils;

/*

app/tracking/admin_info/pmssno           PMSS#
app/tracking/death_certificate_number       Death certificate number
app/tracking/date_of_death/dod           Date of Death (calculated)
app/demographic/date_of_birth/dob       Date of Birth (calculated)
app/tracking/q9/reszip               Zip code of residence
app/demographic/mage               Maternal age at Death
app/ije_dc/cause_details/manner_dc       Manner
app/ije_dc/cause_details/cod1a_dc       Cause of Death Part I Line A
app/ije_dc/cause_details/cod1b_dc       Cause of Death Part I Line B
app/ije_dc/cause_details/cod1c_dc       Cause of Death Part I Line C
app/ije_dc/cause_details/cod1d_dc       Cause of Death Part I Line D
app/ije_dc/cause_details/othercondition_dc Cause of Death Part II    

 
9
CURRENT Picklists on Line-Listing Summary Page
-----------------------------------------------------------------------------------
app/tracking/admin_info/jurisdiction          Jurisdiction    
app/tracking/admin_info/track_year          Year of Death
app/tracking/admin_info/status         Status    
app/cause_of_death/class           Classification

*/
public sealed class CaseViewSearch
{
    common.couchdb.DBConfigurationDetail db_config;

    System.Security.Claims.ClaimsPrincipal User;

    bool is_case_identified_data = false;
    bool is_include_pinned_cases = false;
    mmria.pmss.server.utils.ResourceRightEnum ResourceRight;

    public CaseViewSearch
    (
        common.couchdb.DBConfigurationDetail p_configuration, 
        System.Security.Claims.ClaimsPrincipal p_user, 
        bool p_is_case_identified_data = false,
        bool p_include_pinned_cases = false
    )
    {
        db_config = p_configuration;
        User = p_user;

        is_case_identified_data = p_is_case_identified_data;
        is_include_pinned_cases = p_include_pinned_cases;

        if(is_case_identified_data)
        {
            ResourceRight = mmria.pmss.server.utils.ResourceRightEnum.ReadCase;
        }
        else
        {
            ResourceRight = mmria.pmss.server.utils.ResourceRightEnum.ReadDeidentifiedCase;
        }
        
    }

    delegate bool is_valid_predicate(mmria.common.model.couchdb.pmss_case_view_item item);

    List<is_valid_predicate> all_predicate_list = new List<is_valid_predicate>();
    List<is_valid_predicate> any_predicate_list = new List<is_valid_predicate>();
    delegate is_valid_predicate create_predicate_delegate
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    );

/*
host_state
pmssno
death_certificate_number
dod
dob
residence_zip
mage
manner
cod1a
cod1b
cod1c
cod1d
cod_other_condition
classification
jurisdiction
case_folder
track_year
med_coder_check
med_dir_check
status
month_of_death
day_of_death
year_of_death
month_of_birth
day_of_birth
year_of_birth
agreement_status
version
date_created
created_by
date_last_updated
last_updated_by
date_last_checked_out
last_checked_out_by
*/
    
    is_valid_predicate create_predicate_by_date_created
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.date_created.HasValue ? item.value.date_created.Value.ToString() : "", search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_date_created")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_date_last_updated
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.date_last_updated.HasValue ? item.value.date_last_updated.Value.ToString() : "", search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_date_last_updated")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    /*
    is_valid_predicate create_predicate_by_last_name
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            

            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.last_name, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_last_name")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;

    }
    is_valid_predicate create_predicate_by_first_name
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(search_key != null )
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
                {
                    bool result = false;
                    if(is_matching_search_text(item.value.first_name, search_key))
                    {
                        result = true;
                    }

                    return result;
                };
            if (field_selection == "all")
            {
                any_predicate_list.Add(f);
            }

            if(field_selection == "by_first_name")
            {
                all_predicate_list.Add(f);
            }

            return f;
        }

        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_middle_name
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(! string.IsNullOrWhiteSpace(item.value.middle_name))
                if(is_matching_search_text(item.value.middle_name, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_middle_name")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    */
    is_valid_predicate create_predicate_by_year_of_death
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                //if(is_matching_search_text(item.value.date_of_death_year.HasValue ? item.value.date_of_death_year.Value.ToString() : "", search_key))
                if
                (
                    item.value.date_of_death_year.HasValue &&
                    item.value.date_of_death_year.Value.ToString() == search_key
                )
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_year_of_death")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_month_of_death
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.date_of_death_month.HasValue ? item.value.date_of_death_month.Value.ToString() : "", search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_month_of_death")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    /*
    is_valid_predicate create_predicate_by_committee_review_date
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.review_date_actual.HasValue ? item.value.review_date_actual.Value.ToString() : "", search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_committee_review_date")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }*/

    is_valid_predicate create_predicate_by_created_by
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(! string.IsNullOrWhiteSpace(item.value.created_by))
                if(is_matching_search_text(item.value.created_by, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_created_by")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_last_updated_by
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(! string.IsNullOrWhiteSpace(item.value.last_updated_by))
                if(is_matching_search_text(item.value.last_updated_by, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_last_updated_by")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_state_of_death
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(! string.IsNullOrWhiteSpace(item.value.jurisdiction))
                if(is_matching_search_text(item.value.jurisdiction, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_state_of_death")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_date_last_checked_out
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.date_last_checked_out.HasValue ? item.value.date_last_checked_out.Value.ToString() : "", search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_date_last_checked_out")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_last_checked_out_by
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(! string.IsNullOrWhiteSpace(item.value.last_checked_out_by))
                if(is_matching_search_text(item.value.last_checked_out_by, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_last_checked_out_by")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_status
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_pmssno
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(search_key != null)
        {

            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(is_matching_search_text(item.value.pmssno, search_key))
                {
                    result = true;
                }

                return result;
            };

            if (field_selection == "all")
                any_predicate_list.Add(f);

            if (field_selection == "by_pmssno")
                all_predicate_list.Add(f);

        }

        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
    /*
    is_valid_predicate create_predicate_by_pregnancy_relatedness
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(pregnancy_relatedness != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(item.value.pregnancy_relatedness.HasValue ? item.value.pregnancy_relatedness.Value.ToString() == pregnancy_relatedness : string.IsNullOrWhiteSpace(pregnancy_relatedness))
                {
                    result = true;
                }

                return result;
            };

            all_predicate_list.Add(f);

            return f;
        }

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    */
    is_valid_predicate create_predicate_by_host_state
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                //if(! string.IsNullOrWhiteSpace(item.value.host_state))
                //if(is_matching_search_text(item.value.host_state, search_key))
                if
                (
                    ! string.IsNullOrWhiteSpace(item.value.host_state) &&
                    item.value.host_state == search_key
                )
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_host_state")
                all_predicate_list.Add(f);
        }

        
        return (mmria.common.model.couchdb.pmss_case_view_item item) => true;
    }
/*
    is_valid_predicate create_predicate_by_record_id
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(is_matching_search_text(item.value.record_id, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                    any_predicate_list.Add(f);

            if(field_selection == "by_record_id")
                all_predicate_list.Add(f);

            
            return f;
        }

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    */
/*

    is_valid_predicate create_predicate_by_date_of_review
    (
        string field_selection,
        string date_of_review_range
    )
    {
        bool result(mmria.common.model.couchdb.pmss_case_view_item item) => false;
        if
        (
            !string.IsNullOrWhiteSpace(date_of_review_range) &&
            date_of_review_range.ToLower() != "all" 
        )
        {
            var dates = date_of_review_range.Split("T");
            DateTime start_date;
            DateTime end_date;

            if
            (
                dates.Length < 2 ||
                string.IsNullOrWhiteSpace(dates[0]) ||
                string.IsNullOrWhiteSpace(dates[1]) ||
                ! DateTime.TryParse(dates[0], out start_date) ||
                ! DateTime.TryParse(dates[1], out end_date)
            )
                return result;

        

            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;

                if
                (

                    item.value.date_of_committee_review.HasValue &&
                    item.value.date_of_committee_review.Value  >= start_date &&
                    item.value.date_of_committee_review.Value  <= end_date

                )
                {
                    result = true;
                }

                return result;
            };

            all_predicate_list.Add(f);

            return f;
        }

        return result;
    }
    */


    is_valid_predicate create_predicate_by_date_of_death
    (
        string field_selection,
        string date_of_death_range
    )
    {
        bool result(mmria.common.model.couchdb.pmss_case_view_item item) => false;
        if
        (
            !string.IsNullOrWhiteSpace(date_of_death_range) &&
            date_of_death_range.ToLower() != "all" 
        )
        {
            var dates = date_of_death_range.Split("T");
            DateTime start_date;
            DateTime end_date;

            if
            (
                dates.Length < 2 ||
                string.IsNullOrWhiteSpace(dates[0]) ||
                string.IsNullOrWhiteSpace(dates[1]) ||
                ! DateTime.TryParse(dates[0], out start_date) ||
                ! DateTime.TryParse(dates[1], out end_date)
            )
                return result;

            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;

                if
                (
                    item.value.date_of_death_year.HasValue &&
                    item.value.date_of_death_month.HasValue &&
                    item.value.date_of_death_year.Value <=2100 &&
                    item.value.date_of_death_year.Value >= 1900 &&
                    item.value.date_of_death_month.Value <= 12 &&
                    item.value.date_of_death_month.Value >= 1
                )
                {
                    /* try
                    {*/
                        DateTime compare_date = new DateTime
                        (
                            item.value.date_of_death_year.Value,
                            item.value.date_of_death_month.Value,
                            01
                        );

                        if
                        (
                            compare_date >= start_date &&
                            compare_date  <= end_date
                        )
                            result = true;
                    /*}
                    catch(Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }*/
                }

                return result;
            };

            all_predicate_list.Add(f);
/*
            if(field_selection == "all")
                any_predicate_list.Add(f);

            if
            (
                field_selection == "by_year_of_death" ||
                field_selection == "by_month_of_death"
            )
                all_predicate_list.Add(f);
                */
            
            return f;
        }

        return result;
    }

    is_valid_predicate create_predicate_by_death_certificate_number
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) => 
            {
                bool result = false;
                if(is_matching_search_text(item.value.death_certificate_number, search_key))
                {
                    result = true;
                }

                return result;
            };

            if(field_selection == "all")
                any_predicate_list.Add(f);



            if(field_selection == "by_death_certificate_number")
                all_predicate_list.Add(f);
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_dod
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_dob
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_residence_zip
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_mage
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_manner
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_cod1a
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_cod1b
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_cod1c
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_cod1d
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_cod_other_condition
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_classification
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(classification != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.classification))
                {
                    if
                    (
                        item.value.classification.Equals(classification, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_jurisdiction
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(jurisdiction != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.jurisdiction))
                {
                    if
                    (
                        item.value.jurisdiction.Equals(jurisdiction, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }

    /*
    is_valid_predicate create_predicate_by_case_folder
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }*/
    is_valid_predicate create_predicate_by_track_year
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(year_of_death != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(item.value.track_year.HasValue)
                {
                    if
                    (
                        item.value.track_year.Value.ToString().Equals(year_of_death, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_med_coder_check
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_med_dir_check
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }

    is_valid_predicate create_predicate_by_month_of_birth
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_day_of_birth
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_year_of_birth
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_agreement_status
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_version
    (
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification
    )
    {

        if(status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item item) =>
            {
                bool result = false;
                if(!string.IsNullOrWhiteSpace(item.value.status))
                {
                    if
                    (
                        item.value.status.Equals(status, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        result = true;
                    }
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.pmss_case_view_item item) => false;
    }
    

    is_valid_predicate create_predicate_by_case_folder(HashSet<(string jurisdiction, mmria.pmss.server.utils.ResourceRightEnum ResourceRight)> ctx)
    {
        is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item cvi) => {
            bool result = false;
            if(cvi.value.case_folder == null)
            {
                cvi.value.case_folder = "/";
            }

            foreach(var jurisdiction_item in ctx)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction);


                if(regex.IsMatch(cvi.value.case_folder) && jurisdiction_item.ResourceRight == ResourceRight)
                {
                    result = true;
                    break;
                }
            }
            return result;
        };

        all_predicate_list.Add(f);

        return f;
    }


    is_valid_predicate create_predicate_by_vro_role(HashSet<(string jurisdiction, mmria.pmss.server.utils.ResourceRightEnum ResourceRight)> ctx)
    {
        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_user_role_jurisdiction_set_for(db_config, User.Identity.Name);

        var status_set = new HashSet<string>()
        {
            "STEVE: Pending VRO Investigation",
            "STEVE: Pending VRO Investigation, Re-review Requested by CDC",
            "STEVE: Pending VRO Investigation, Linkage Review Requested by CDC"
        };

        /*
app/tracking/admin_info/status
STEVE: Pending VRO Investigation
STEVE: Pending VRO Investigation, Re-review Requested by CDC
STEVE: Pending VRO Investigation, Linkage Review Requested by CDC
        */
        is_valid_predicate f = (mmria.common.model.couchdb.pmss_case_view_item cvi) => 
        {
            if(jurisdiction_hashset.Count > 1) return true;
            else if(jurisdiction_hashset.Single().role_name != "vro") return true;

            bool result = status_set.Contains(cvi.value.status);

            return result;
        };

        all_predicate_list.Add(f);

        return f;
    }

    is_valid_predicate is_valid_date_created;
    
    is_valid_predicate is_valid_date_last_updated;

    is_valid_predicate is_valid_created_by;
    is_valid_predicate is_valid_last_updated_by;
    is_valid_predicate is_valid_state_of_death;
    is_valid_predicate is_valid_date_last_checked_out;
    is_valid_predicate is_valid_last_checked_out_by;
    is_valid_predicate is_valid_host_state;
    is_valid_predicate is_valid_year_of_death;
    is_valid_predicate is_valid_month_of_death;
    is_valid_predicate is_valid_case_folder;

    is_valid_predicate is_valid_vro_search;
    is_valid_predicate is_valid_date_of_death;
    is_valid_predicate is_valid_pmssno;
    is_valid_predicate is_valid_death_certificate_number;
    is_valid_predicate is_valid_dod;
    is_valid_predicate is_valid_dob;
    is_valid_predicate is_valid_residence_zip;
    is_valid_predicate is_valid_mage;
    is_valid_predicate is_valid_manner;
    is_valid_predicate is_valid_cod1a;
    is_valid_predicate is_valid_cod1b;
    is_valid_predicate is_valid_cod1c;
    is_valid_predicate is_valid_cod1d;
    is_valid_predicate is_valid_cod_other_condition;
    is_valid_predicate is_valid_classification;
    is_valid_predicate is_valid_jurisdiction;

    is_valid_predicate is_valid_track_year;
    is_valid_predicate is_valid_med_coder_check;
    is_valid_predicate is_valid_med_dir_check;
    is_valid_predicate is_valid_status;

    is_valid_predicate is_valid_month_of_birth;
    is_valid_predicate is_valid_day_of_birth;
    is_valid_predicate is_valid_year_of_birth;
    is_valid_predicate is_valid_agreement_status;
    is_valid_predicate is_valid_version;


    HashSet<string> sort_list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "by_date_created",
        "by_date_last_updated",
        "by_created_by",
        "by_last_updated_by",
        "by_date_last_checked_out",
        "by_last_checked_out_by",
        "conflicts",
        "by_pmss_number",
        "by_jurisdiction",
        "by_status",
        "by_case_folder",
        "by_death_classification",
        "by_cause_of_death",
        "by_med_coder_check",
        "by_med_dir_check",
        "by_track_year"

    };
    public async Task<mmria.common.model.couchdb.pmss_case_view_response> execute
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_last_updated",
        string search_key = null,
        bool descending = false,
        string field_selection = "all",
        string jurisdiction = "all",
        string year_of_death = "all",
        string status = "all",
        string classification = "all",
        string date_of_death_range = "all",
        string date_of_review_range = "all"
    ) 
    {

        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, User);

        string sort_view = sort.ToLower ();

        if (! sort_list.Contains(sort_view))
        {
            sort_view = "by_date_last_updated";
        }

        try
        {
            System.Text.StringBuilder request_builder = new System.Text.StringBuilder ();

            if(is_case_identified_data)
            {
                request_builder.Append ($"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/{sort_view}?");
            }
            else
            {
                request_builder.Append ($"{db_config.url}/{db_config.prefix}de_id/_design/sortable/_view/{sort_view}?");
            }


            if (string.IsNullOrWhiteSpace (search_key))
            {
                if (skip > -1) 
                {
                    request_builder.Append ($"skip={0}");
                } 
                else 
                {

                    request_builder.Append ("skip=0");
                }

                if (take > -1) 
                {
                    request_builder.Append ($"&limit={30000}");
                }

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            } 
            else 
            {
                request_builder.Append ("skip=0");
                request_builder.Append ($"&limit={30000}");

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            }

            string request_string = request_builder.ToString();
            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            create_predicates
            (
                jurisdiction_hashset,
                search_key?.ToLower ().Trim (new char [] { '"' }),
                field_selection,
                jurisdiction,
                year_of_death,
                status,
                classification,
                date_of_review_range,
                date_of_death_range
            );

            mmria.common.model.couchdb.pmss_case_view_response pmss_case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pmss_case_view_response>(responseFromServer);
            mmria.common.model.couchdb.pmss_case_view_response result = new mmria.common.model.couchdb.pmss_case_view_response();
            result.offset = pmss_case_view_response.offset;
            result.total_rows = pmss_case_view_response.total_rows;
            


            if (is_case_identified_data && is_include_pinned_cases)
            {
                var pinned_cases = await GetPinnedCaseSet();
                
                var pinned_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                
                foreach(var kvp in pinned_cases.list)
                {
                    foreach(var case_id in kvp.Value)
                    {
                        if(kvp.Key == "everyone")
                        {
                            pinned_id_set.Add(case_id);
                        }
                        else if(kvp.Key.Equals(User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            pinned_id_set.Add(case_id);
                        }

                    }
                }
                

                var pinned_data = pmss_case_view_response.rows
                    .Where
                    (
                        cvi => pinned_id_set.Contains(cvi.id) && 
                        is_valid_case_folder(cvi) &&
                        is_valid_vro_search(cvi)
                        
                    );

                result.total_rows = pinned_data.Count();

                result.rows.AddRange(pinned_data);

                var data = pmss_case_view_response.rows
                    .Where
                    (
                        cvi => 
                            all_predicate_list.All( f => f(cvi)) &&
                            (
                                any_predicate_list.Count == 0 ||
                                any_predicate_list.Any( f => f(cvi)) 
                            ) && 
                            ! pinned_id_set.Contains(cvi.id) &&
                            is_valid_vro_search(cvi)
                        
                    );

                var unpinned_rows = data.Skip (skip).Take (take).ToList ();
                var next = unpinned_rows.Take(take - result.total_rows);
                result.total_rows = result.total_rows + data.Count();
                result.rows.AddRange(next);


            }
            else
            {
                var data = pmss_case_view_response.rows
                    .Where
                    (
                        cvi => 
                            all_predicate_list.All( f => f(cvi)) &&
                            (
                                any_predicate_list.Count == 0 ||
                                any_predicate_list.Any( f => f(cvi)) 
                            ) &&
                            is_valid_vro_search(cvi)
                        
                    );

                result.total_rows = data.Count();
                result.rows =  data.Skip (skip).Take (take).ToList ();
            }

            return result;
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }


        return null;
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

    void create_predicates
    (
        HashSet<(string jurisdiction_id, mmria.pmss.server.utils.ResourceRightEnum ResourceRight)> ctx,
        string search_key,
        string field_selection,
        string jurisdiction,
        string year_of_death,
        string status,
        string classification,
        string date_of_review_range,
        string date_of_death_range
    )
    {
        is_valid_case_folder = create_predicate_by_case_folder(ctx);

        is_valid_vro_search = create_predicate_by_vro_role(ctx);
        is_valid_date_created = create_predicate_by_date_created(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_date_last_updated = create_predicate_by_date_last_updated(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        /*
        is_valid_last_name = create_predicate_by_last_name(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_first_name = create_predicate_by_first_name(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_middle_name = create_predicate_by_middle_name(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        */
        is_valid_year_of_death = create_predicate_by_year_of_death(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_month_of_death = create_predicate_by_month_of_death(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        //is_valid_committee_review_date = create_predicate_by_committee_review_date(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_created_by = create_predicate_by_created_by(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_last_updated_by = create_predicate_by_last_updated_by(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_state_of_death = create_predicate_by_state_of_death(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_date_last_checked_out = create_predicate_by_date_last_checked_out(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_last_checked_out_by = create_predicate_by_last_checked_out_by(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_status = create_predicate_by_status(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_pmssno = create_predicate_by_pmssno(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        //is_valid_pregnancy_relatedness = create_predicate_by_pregnancy_relatedness(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        is_valid_host_state = create_predicate_by_host_state(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        //is_valid_record_id = create_predicate_by_record_id(search_key, field_selection, jurisdiction, year_of_death, status, classification);
        //is_valid_date_of_review = create_predicate_by_date_of_review(field_selection, date_of_review_range);
        is_valid_date_of_death = create_predicate_by_date_of_death(field_selection, date_of_death_range);
    is_valid_pmssno = create_predicate_by_pmssno
    (
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_death_certificate_number = create_predicate_by_death_certificate_number(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_dod = create_predicate_by_dod(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_dob = create_predicate_by_dob(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );

    is_valid_residence_zip = create_predicate_by_residence_zip(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_mage = create_predicate_by_mage(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_manner = create_predicate_by_manner(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_cod1a = create_predicate_by_cod1a(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_cod1b = create_predicate_by_cod1b(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_cod1c = create_predicate_by_cod1c(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_cod1d = create_predicate_by_cod1d(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_cod_other_condition = create_predicate_by_cod_other_condition(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_classification = create_predicate_by_classification(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_jurisdiction = create_predicate_by_jurisdiction(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );

    is_valid_track_year = create_predicate_by_track_year(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_med_coder_check = create_predicate_by_med_coder_check(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_med_dir_check = create_predicate_by_med_dir_check(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    /*
    is_valid_status = create_predicate_by_status(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );*/
    is_valid_month_of_birth = create_predicate_by_month_of_birth(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_day_of_birth = create_predicate_by_day_of_birth(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_year_of_birth = create_predicate_by_year_of_birth(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_agreement_status = create_predicate_by_agreement_status(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    is_valid_version = create_predicate_by_version(
        search_key,
        field_selection,
        jurisdiction, year_of_death, status, classification
    );
    }


    async Task<mmria.common.model.couchdb.pinned_case_set> GetPinnedCaseSet()
    {

        mmria.common.model.couchdb.pinned_case_set result = null;

        try
        {
            string request_string = $"{db_config.url}/jurisdiction/pinned-case-set";
            var case_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pinned_case_set>(responseFromServer);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
        
    }
}

#endif