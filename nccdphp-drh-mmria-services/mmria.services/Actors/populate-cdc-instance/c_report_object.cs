using System;
namespace mmria.server.model;

public struct timing_of_death_in_relation_to_pregnancy_struct
{
    public int pregnant_at_the_time_of_death;
    public int pregnant_within_42_days_of_death;
    public int pregnant_within_43_to_365_days_of_death;
    public int blank;
}

public struct age_at_death_struct
{
    public int age_less_than_20;
    public int age_20_to_24;
    public int age_25_to_29;
    public int age_30_to_34;
    public int age_35_to_44;
    public int age_45_and_above;
    public int blank;
}

public struct  total_number_of_cases_by_pregnancy_relatedness_struct 
{
    public int pregnancy_related;
    public int pregnancy_associated_but_not_related;
    public int not_pregnancy_related_or_associated;
    public int unable_to_determine;
    public int blank;
}


public struct ethnicity_struct
{
    public int blank;
    public int hispanic;
    public int non_hispanic_black;
    public int non_hispanic_white;
    public int american_indian_alaska_native;
    public int native_hawaiian;
    public int guamanian_or_chamorro;
    public int samoan;
    public int other_pacific_islander;
    public int asian_indian;
    public int filipino;
    public int korean;
    public int other_asian;
    public int chinese;
    public int japanese;
    public int vietnamese;
    public int other;
}


//  new - start

public struct total_value_struct
{
    public int value;
}

// singular
// distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm


// double
//determined_to_be_preventable - value struct
// obesity_contributed_to_the_death
// mental_health_conditions_contributed_to_death
// substance_use_disorder_contributed_to_death
// is_suicide
// is_homocide
//

//  new - end



public sealed class c_report_object
{

    public c_report_object()
    {
        this.distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_related_determined_to_be_preventable = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_associated_determined_to_be_preventable = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_related_obesity_contributed_to_the_death = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_associated_obesity_contributed_to_the_death = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        this.total_pregnancy_related_mental_health_conditions_contributed_to_death = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_associated_mental_health_conditions_contributed_to_death = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);


        this.total_pregnancy_related_substance_use_disorder_contributed_to_death = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_associated_substance_use_disorder_contributed_to_death = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        this.total_pregnancy_related_is_suicide = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_associated_is_suicide = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);


        this.total_pregnancy_related_is_homocide = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        this.total_pregnancy_associated_is_homocide = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    }
    public string _id ;
    public int? year_of_death;
    public int? month_of_case_review;
    public int? year_of_case_review;
    public total_number_of_cases_by_pregnancy_relatedness_struct total_number_of_cases_by_pregnancy_relatedness;
    public ethnicity_struct total_number_of_pregnancy_related_deaths_by_ethnicity;
    public ethnicity_struct total_number_of_pregnancy_associated_by_ethnicity;
    public age_at_death_struct total_number_of_pregnancy_related_deaths_by_age;
    public age_at_death_struct total_number_of_pregnancy_associated_deaths_by_age;
    public timing_of_death_in_relation_to_pregnancy_struct total_number_pregnancy_related_at_time_of_death;
    public timing_of_death_in_relation_to_pregnancy_struct total_number_pregnancy_associated_at_time_of_death;

    // singular
    public System.Collections.Generic.Dictionary<string, int> distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm;

    // double
    //determined_to_be_preventable - value struct
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_related_determined_to_be_preventable;
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_associated_determined_to_be_preventable;


    // obesity_contributed_to_the_death
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_related_obesity_contributed_to_the_death;
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_associated_obesity_contributed_to_the_death;

    // mental_health_conditions_contributed_to_death
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_related_mental_health_conditions_contributed_to_death;
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_associated_mental_health_conditions_contributed_to_death;

    // substance_use_disorder_contributed_to_death
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_related_substance_use_disorder_contributed_to_death;
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_associated_substance_use_disorder_contributed_to_death;

    // is_suicide
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_related_is_suicide;
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_associated_is_suicide;

    // is_homocide
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_related_is_homocide;
    public System.Collections.Generic.Dictionary<string, int> total_pregnancy_associated_is_homocide;


}

