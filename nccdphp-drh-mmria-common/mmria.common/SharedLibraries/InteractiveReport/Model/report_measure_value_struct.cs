using System;
namespace mmria.common.Model.InteractiveReport;

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
