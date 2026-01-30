using System;
namespace mmria.server.model;

public struct report_measure_value_struct
{
    /*
    public string _id;
    public string _rev;

    public string type;*/

    public string case_id;
    public string host_state;

    public int? means_of_fatal_injury;

    public int? year_of_death;
    public int? month_of_death;
    public int? day_of_death;

    public int? case_review_year;
    public int? case_review_month;

    public int? case_review_day;

    public int? pregnancy_related;

    public string indicator_id;

    public string field_id;
    public int? value;
    public string jurisdiction_id;
}
public struct opioid_report_value_struct
{
    public string host_state;

    public int? year_of_death;
    public int? month_of_death;
    public int? day_of_death;

    public int? case_review_year;
    public int? case_review_month;

    public int? case_review_day;

    public int? pregnancy_related;

    public string indicator_id;

    public string field_id;
    public int? value;
    public string jurisdiction_id;
}

public sealed class c_opioid_report_object
{

    public c_opioid_report_object(string p_type = "overdose")
    {
        this.type = p_type;
        this.data = new System.Collections.Generic.List<opioid_report_value_struct>();
    }
    public string _id ;

    public string type;
    public int? means_of_fatal_injury;

    public int? year_of_death;
    public int? month_of_case_review;
    public int? year_of_case_review;
    public int? day_of_case_review;

    public string jurisdiction_id;

    public System.Collections.Generic.List<opioid_report_value_struct> data;

/*
    public System.Collections.Generic.List<report_measure_value_struct> ToReportMeasureValueList()
    {
        var result = new System.Collections.Generic.List<report_measure_value_struct>();

        foreach(var item in data)
        {
            if(item.value > 0)
            {
                report_measure_value_struct new_data = new report_measure_value_struct();
                
                new_data. _id = $"{this._id.Replace("powerbi-","")}-{item.indicator_id}-{item.field_id}";
                new_data._rev = null;

                new_data.type = "report-measure";

                new_data.case_id = this._id.Replace("powerbi-","");
                new_data.host_state = item.host_state;
                new_data.year_of_death = item.year_of_death;
                new_data.month_of_death = item.month_of_death;
                new_data.day_of_death = item.day_of_death;

                new_data.case_review_year = item.case_review_year;
                new_data.case_review_month = item.case_review_month;
                new_data.case_review_day = item.case_review_day;

                new_data.pregnancy_related = item.pregnancy_related;
                new_data.indicator_id = item.indicator_id;
                new_data.field_id = item.field_id;
                new_data.value = item.value;
                new_data.jurisdiction_id = item.jurisdiction_id;
                new_data.means_of_fatal_injury = this.means_of_fatal_injury;


                result.Add(new_data);
            }
        }

        return result;

    }
    */
}

