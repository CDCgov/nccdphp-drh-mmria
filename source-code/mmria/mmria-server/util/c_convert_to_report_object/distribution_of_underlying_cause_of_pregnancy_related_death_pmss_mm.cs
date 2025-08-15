using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.server.utils;

public sealed partial class c_convert_to_report_object
{

/*
    committee_review/pregnancy_relatedness = Pregnancy Related; 
    AND 
    committee_review/pmss_mm 
        - display each value with count,
        - include zero values
*/
    private void popluate_distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm(ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
        {
            var list = List_Look_Up["committee_review/pmss_mm"];

            p_report_object.distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm = new System.Collections.Generic.Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);
            var result = p_report_object.distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm;

            foreach(var kvp in list)
            {
                result.Add(kvp.Key, 0);
            }

            try
            {	
                string val = null;
                var get_value_result = get_value(p_source_object, "committee_review/pmss_mm");
                if(! get_value_result.is_erorr)
                {
                    val = get_value_result.result?.ToString();
                }

                if(val != null && result.ContainsKey(val))
                {
                    result[val] = 1;
                }
                else
                {
                    result["9999"] = 1;
                }

                val = null;
                get_value_result = get_value(p_source_object, "committee_review/pmss_mm_secondary");
                if(! get_value_result.is_erorr)
                {
                    val = get_value_result.result?.ToString();
                }
                
                if(val != null && result.ContainsKey(val))
                {
                    result[val] += 1;
                }
                else
                {
                    result["9999"] += 1;
                }
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
        }
    }
}


