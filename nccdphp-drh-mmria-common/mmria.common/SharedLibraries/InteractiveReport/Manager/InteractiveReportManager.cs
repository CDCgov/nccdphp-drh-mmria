using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.Model.InteractiveReport;

namespace mmria.common.Manager.InteractiveReport;

public sealed class InteractiveReportManager
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public InteractiveReportManager(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<IList<report_measure_value_struct>> Get(string indicator_id, DBConfigurationDetail db_config, IList<JurisdictionAccessInfo> jurisdictionAccessList)
    {
        var result = new List<report_measure_value_struct>();
        
        var config_couchdb_url = db_config.url;
        var config_timer_user_name = db_config.user_name;
        var config_timer_value = db_config.user_value;
        var config_db_prefix = db_config.prefix;
        
        try
        {

            string find_url = $"{config_couchdb_url}/{config_db_prefix}report/_design/interactive_aggregate_report/_view/indicator_id?skip=0&limit={30000}&key=\"{indicator_id}\"";
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", find_url, null, config_timer_user_name, config_timer_value);
            
            var response_result = JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<report_measure_value_struct>>(responseFromServer);
            
            if (response_result?.rows != null)
            {
                foreach (var row in response_result.rows)
                {
                    try
                    {
                        var value = row.value;

                        if(!string.IsNullOrWhiteSpace(indicator_id))
                        {
                            if(row.key.ToLower() == indicator_id.ToLower())
                            {
                                foreach(var jurisdiction_item in  jurisdictionAccessList)
                                {
                                    var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.JurisdictionId);
                                    if
                                    (
                                        regex.IsMatch(value.jurisdiction_id) && 
                                        1 == jurisdiction_item.ResourceRight
                                    )
                                    {
                                        if
                                        (
                                            value.year_of_death.HasValue && value.year_of_death.Value != 9999 &&
                                            value.month_of_death.HasValue && value.month_of_death.Value != 9999 &&
                                            value.day_of_death.HasValue && value.day_of_death.Value != 9999 &&
                                            value.case_review_day.HasValue && value.case_review_day.Value != 9999 && 
                                            value.case_review_month.HasValue && value.case_review_month.Value != 9999 &&
                                            value.case_review_year.HasValue && value.case_review_year.Value != 9999
                                        )
                                        {
                                            result.Add(value);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach(var jurisdiction_item in  jurisdictionAccessList)
                            {
                                var regex = new System.Text.RegularExpressions.Regex("^" + jurisdiction_item.JurisdictionId);
                                if
                                (
                                    regex.IsMatch(value.jurisdiction_id) && 
                                    1 == jurisdiction_item.ResourceRight
                                )
                                {
                                    if
                                    (
                                        value.year_of_death.HasValue && value.year_of_death.Value != 9999 &&
                                        value.month_of_death.HasValue && value.month_of_death.Value != 9999 &&
                                        value.day_of_death.HasValue && value.day_of_death.Value != 9999 &&
                                        value.case_review_day.HasValue && value.case_review_day.Value != 9999 && 
                                        value.case_review_month.HasValue && value.case_review_month.Value != 9999 &&
                                        value.case_review_year.HasValue && value.case_review_year.Value != 9999
                                    )
                                    {
                                        result.Add(value);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip rows that fail deserialization
                    }
                }
            }

            System.Console.WriteLine($"case_response.docs.length {result.Count}");
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }
}

