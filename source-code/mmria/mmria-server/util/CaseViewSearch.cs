#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace mmria.server.utils;

public sealed class CaseViewSearch
{
    common.couchdb.DBConfigurationDetail db_config;

    System.Security.Claims.ClaimsPrincipal User;

    bool is_case_identified_data = false;
    bool is_include_pinned_cases = false;
    mmria.server.utils.ResourceRightEnum ResourceRight;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public CaseViewSearch
    (
        common.couchdb.DBConfigurationDetail p_configuration, 
        System.Security.Claims.ClaimsPrincipal p_user, 
        bool p_is_case_identified_data = false,
        bool p_include_pinned_cases = false,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient = null
    )
    {
        db_config = p_configuration;
        User = p_user;

        is_case_identified_data = p_is_case_identified_data;
        is_include_pinned_cases = p_include_pinned_cases;
        _couchDbHttpClient = couchDbHttpClient;

        if(is_case_identified_data)
        {
            ResourceRight = mmria.server.utils.ResourceRightEnum.ReadCase;
        }
        else
        {
            ResourceRight = mmria.server.utils.ResourceRightEnum.ReadDeidentifiedCase;
        }
        
    }

    delegate bool is_valid_predicate(mmria.common.model.couchdb.case_view_item item);

    List<is_valid_predicate> all_predicate_list = new List<is_valid_predicate>();
    List<is_valid_predicate> any_predicate_list = new List<is_valid_predicate>();
    delegate is_valid_predicate create_predicate_delegate
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    );
    
    is_valid_predicate create_predicate_by_date_created
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_date_last_updated
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_last_name
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            

            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;

    }
    is_valid_predicate create_predicate_by_first_name
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {

        if(search_key != null )
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_middle_name
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_year_of_death
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_month_of_death
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_committee_review_date
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_created_by
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_last_updated_by
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_state_of_death
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
            {
                bool result = false;
                if(! string.IsNullOrWhiteSpace(item.value.state_of_death))
                if(is_matching_search_text(item.value.state_of_death, search_key))
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_date_last_checked_out
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_last_checked_out_by
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_case_status
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {

        if(case_status != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
            {
                bool result = false;
                if(item.value.case_status.HasValue ? item.value.case_status.Value.ToString() == case_status : string.IsNullOrWhiteSpace(case_status))
                {
                    result = true;
                }

                return result;
            };

            all_predicate_list.Add(f);

            
            return f;
        }
            

        return (mmria.common.model.couchdb.case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_agency_case_id
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {

        if(search_key != null)
        {

            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
            {
                bool result = false;
                if(is_matching_search_text(item.value.agency_case_id, search_key))
                {
                    result = true;
                }

                return result;
            };

            if (field_selection == "all")
                any_predicate_list.Add(f);

            if (field_selection == "by_agency_case_id")
                all_predicate_list.Add(f);

        }

        return (mmria.common.model.couchdb.case_view_item item) => true;
    }
    is_valid_predicate create_predicate_by_pregnancy_relatedness
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {

        if(pregnancy_relatedness != "all")
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
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

        return (mmria.common.model.couchdb.case_view_item item) => false;
    }
    is_valid_predicate create_predicate_by_host_state
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) => 
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

        
        return (mmria.common.model.couchdb.case_view_item item) => true;
    }

    is_valid_predicate create_predicate_by_record_id
    (
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness
    )
    {
        if(search_key != null)
        {
            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
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

        return (mmria.common.model.couchdb.case_view_item item) => false;
    }


    is_valid_predicate create_predicate_by_date_of_review
    (
        string field_selection,
        string date_of_review_range
    )
    {
        bool result(mmria.common.model.couchdb.case_view_item item) => false;
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

        

            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
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

/*
            if(field_selection == "all")
                any_predicate_list.Add(f);

            if(field_selection == "by_committee_review_date")
                all_predicate_list.Add(f);
                */

            return f;
        }

        return result;
    }


    is_valid_predicate create_predicate_by_date_of_death
    (
        string field_selection,
        string date_of_death_range
    )
    {
        bool result(mmria.common.model.couchdb.case_view_item item) => false;
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

            is_valid_predicate f = (mmria.common.model.couchdb.case_view_item item) =>
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
    

    is_valid_predicate create_predicate_by_jurisdiction(HashSet<(string jurisdiction_id, mmria.server.utils.ResourceRightEnum ResourceRight)> ctx)
    {
        is_valid_predicate f = (mmria.common.model.couchdb.case_view_item cvi) => {
            bool result = false;
            if(cvi.value.jurisdiction_id == null)
            {
                cvi.value.jurisdiction_id = "/";
            }

            foreach(var jurisdiction_item in ctx)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction_id);


                if(regex.IsMatch(cvi.value.jurisdiction_id) && jurisdiction_item.ResourceRight == ResourceRight)
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

    is_valid_predicate is_valid_date_created;
    
    is_valid_predicate is_valid_date_last_updated;
    is_valid_predicate is_valid_last_name;
    is_valid_predicate is_valid_first_name;
    is_valid_predicate is_valid_middle_name;
    is_valid_predicate is_valid_year_of_death;
    is_valid_predicate is_valid_month_of_death;
    is_valid_predicate is_valid_committee_review_date;
    is_valid_predicate is_valid_created_by;
    is_valid_predicate is_valid_last_updated_by;
    is_valid_predicate is_valid_state_of_death;
    is_valid_predicate is_valid_date_last_checked_out;
    is_valid_predicate is_valid_last_checked_out_by;
    is_valid_predicate is_valid_case_status;
    is_valid_predicate is_valid_agency_case_id;
    is_valid_predicate is_valid_pregnancy_relatedness;
    is_valid_predicate is_valid_host_state;

    is_valid_predicate is_valid_jurisdition;

    is_valid_predicate is_valid_record_id;

    is_valid_predicate is_valid_date_of_review;

    is_valid_predicate is_valid_date_of_death;

    HashSet<string> sort_list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "by_date_created",
        "by_date_last_updated",
        "by_last_name",
        "by_first_name",
        "by_middle_name",
        "by_year_of_death",
        "by_month_of_death",
        "by_committee_review_date",
        "by_created_by",
        "by_last_updated_by",
        "by_state_of_death",
        "by_date_last_checked_out",
        "by_last_checked_out_by",
        "by_case_status",
        "by_agency_case_id",
        "by_pregnancy_relatedness",
        "by_host_state"
    };
    public async Task<mmria.common.model.couchdb.case_view_response> execute
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
        string date_of_review_range = "all"
    ) 
    {

        var jurisdiction_hashset = mmria.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, User);

        string sort_view = sort.ToLower ();

        if (! sort_list.Contains(sort_view))
        {
            sort_view = "by_date_created";
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
            
            // Fetch main query and pinned cases in parallel to reduce round-trip latency
            Task<string> mainQueryTask = _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                db_config.user_name,
                db_config.user_value
            );
            
            Task<mmria.common.model.couchdb.pinned_case_set> pinnedCasesTask = null;
            if (is_case_identified_data && is_include_pinned_cases)
            {
                pinnedCasesTask = GetPinnedCaseSet();
            }
            
            // Wait for both queries to complete
            string responseFromServer = await mainQueryTask;
            
            create_predicates
            (
                jurisdiction_hashset,
                search_key?.ToLower ().Trim (new char [] { '"' }),
                case_status,
                field_selection,
                pregnancy_relatedness,
                date_of_review_range,
                date_of_death_range
            );

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);
            mmria.common.model.couchdb.case_view_response result = new mmria.common.model.couchdb.case_view_response();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;
            


            if (is_case_identified_data && is_include_pinned_cases)
            {
                var pinned_cases = await pinnedCasesTask;
                
                // Include pinned_case_set in response to eliminate separate client call
                result.pinned_case_set = pinned_cases;
                
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
                

                var pinned_data = case_view_response.rows
                    .Where
                    (
                        cvi => pinned_id_set.Contains(cvi.id) && 
                        is_valid_jurisdition(cvi)
                        
                    );

                result.total_rows = pinned_data.Count();

                result.rows.AddRange(pinned_data);

                // Single-pass filtering: apply predicates and separate offline/online in one iteration
                var offline_rows = new List<mmria.common.model.couchdb.case_view_item>();
                var online_rows = new List<mmria.common.model.couchdb.case_view_item>();
                
                foreach (var cvi in case_view_response.rows)
                {
                    if (pinned_id_set.Contains(cvi.id))
                        continue;
                        
                    if (!all_predicate_list.All(f => f(cvi)))
                        continue;
                        
                    if (any_predicate_list.Count > 0 && !any_predicate_list.Any(f => f(cvi)))
                        continue;
                    
                    if (cvi.value.is_offline.HasValue && cvi.value.is_offline.Value)
                        offline_rows.Add(cvi);
                    else
                        online_rows.Add(cvi);
                }

                // Calculate pagination with offline documents always included
                int remaining_capacity = take - result.total_rows; // Account for pinned documents already added
                remaining_capacity = Math.Max(0, remaining_capacity);

                List<mmria.common.model.couchdb.case_view_item> paginated_online = new List<mmria.common.model.couchdb.case_view_item>();

                if (skip < offline_rows.Count)
                {
                    // Skip falls within offline documents
                    var offline_to_show = offline_rows.Skip(skip).Take(remaining_capacity).ToList();
                    result.rows.AddRange(offline_to_show);
                    
                    // Fill remaining with online documents
                    if (offline_to_show.Count < remaining_capacity)
                    {
                        paginated_online = online_rows.Take(remaining_capacity - offline_to_show.Count).ToList();
                        result.rows.AddRange(paginated_online);
                    }
                }
                else
                {
                    // Skip goes past offline documents
                    int skip_into_online = skip - offline_rows.Count;
                    
                    // Add all offline documents
                    result.rows.AddRange(offline_rows);
                    
                    // Add online documents with adjusted skip
                    paginated_online = online_rows.Skip(skip_into_online).Take(remaining_capacity).ToList();
                    result.rows.AddRange(paginated_online);
                }

                result.total_rows = result.total_rows + offline_rows.Count + online_rows.Count;


            }
            else
            {
                // Single-pass filtering: apply predicates and separate offline/online in one iteration
                var offline_rows = new List<mmria.common.model.couchdb.case_view_item>();
                var online_rows = new List<mmria.common.model.couchdb.case_view_item>();
                
                foreach (var cvi in case_view_response.rows)
                {
                    if (!all_predicate_list.All(f => f(cvi)))
                        continue;
                        
                    if (any_predicate_list.Count > 0 && !any_predicate_list.Any(f => f(cvi)))
                        continue;
                    
                    if (cvi.value.is_offline.HasValue && cvi.value.is_offline.Value)
                        offline_rows.Add(cvi);
                    else
                        online_rows.Add(cvi);
                }

                // Calculate pagination with offline documents always included
                int remaining_capacity = take - offline_rows.Count;
                remaining_capacity = Math.Max(0, remaining_capacity); // Don't go negative

                List<mmria.common.model.couchdb.case_view_item> paginated_online = new List<mmria.common.model.couchdb.case_view_item>();
                
                if (skip < offline_rows.Count)
                {
                    // Skip falls within offline documents
                    var offline_to_show = offline_rows.Skip(skip).Take(take).ToList();
                    result.rows.AddRange(offline_to_show);
                    
                    // Fill remaining with online documents
                    if (offline_to_show.Count < take)
                    {
                        paginated_online = online_rows.Take(take - offline_to_show.Count).ToList();
                        result.rows.AddRange(paginated_online);
                    }
                }
                else
                {
                    // Skip goes past offline documents
                    int skip_into_online = skip - offline_rows.Count;
                    
                    // Add all offline documents
                    result.rows.AddRange(offline_rows);
                    
                    // Add online documents with adjusted skip
                    paginated_online = online_rows.Skip(skip_into_online).Take(take).ToList();
                    result.rows.AddRange(paginated_online);
                }

                result.total_rows = offline_rows.Count + online_rows.Count;
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
        HashSet<(string jurisdiction_id, mmria.server.utils.ResourceRightEnum ResourceRight)> ctx,
        string search_key,
        string case_status,
        string field_selection,
        string pregnancy_relatedness,
        string date_of_review_range,
        string date_of_death_range
    )
    {
        // Always create jurisdiction predicate
        is_valid_jurisdition = create_predicate_by_jurisdiction(ctx);
        
        // Skip creating search/filter predicates when all filters are 'all' and no search key
        // This optimization reduces predicate evaluation overhead for common unfiltered queries
        bool has_filters = !string.IsNullOrWhiteSpace(search_key) ||
                          !case_status.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                          !field_selection.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                          !pregnancy_relatedness.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                          !date_of_review_range.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                          !date_of_death_range.Equals("all", StringComparison.OrdinalIgnoreCase);
        
        if (!has_filters)
        {
            // No additional filtering needed beyond jurisdiction
            return;
        }
        
        // Create predicates only when filtering is needed
        is_valid_date_created = create_predicate_by_date_created(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_date_last_updated = create_predicate_by_date_last_updated(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_last_name = create_predicate_by_last_name(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_first_name = create_predicate_by_first_name(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_middle_name = create_predicate_by_middle_name(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_year_of_death = create_predicate_by_year_of_death(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_month_of_death = create_predicate_by_month_of_death(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_committee_review_date = create_predicate_by_committee_review_date(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_created_by = create_predicate_by_created_by(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_last_updated_by = create_predicate_by_last_updated_by(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_state_of_death = create_predicate_by_state_of_death(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_date_last_checked_out = create_predicate_by_date_last_checked_out(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_last_checked_out_by = create_predicate_by_last_checked_out_by(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_case_status = create_predicate_by_case_status(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_agency_case_id = create_predicate_by_agency_case_id(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_pregnancy_relatedness = create_predicate_by_pregnancy_relatedness(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_host_state = create_predicate_by_host_state(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_record_id = create_predicate_by_record_id(search_key, case_status, field_selection, pregnancy_relatedness);
        is_valid_date_of_review = create_predicate_by_date_of_review(field_selection, date_of_review_range);
        is_valid_date_of_death = create_predicate_by_date_of_death(field_selection, date_of_death_range);
    }


    async Task<mmria.common.model.couchdb.pinned_case_set> GetPinnedCaseSet()
    {

        mmria.common.model.couchdb.pinned_case_set result = null;

        try
        {
            string request_string = $"{db_config.url}/jurisdiction/pinned-case-set";            
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                db_config.user_name,
                db_config.user_value
            );
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