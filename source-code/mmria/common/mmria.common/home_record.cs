using System;

namespace mmria.common.model;

public sealed class home_record
{
    public home_record ()
    {
    }

    public string id { get; set; }
    public string record_id { get; set; }
    public string first_name { get; set; }
    public string middle_name { get; set; }
    public string last_name { get; set; }
    public string date_of_death { get; set; }
    public string state_of_death { get; set; }
    public string agency_case_id { get; set; }
    public bool is_valid_maternal_mortality_record { get; set; }

    public DateTime date_created { get; set; }
    public string created_by { get; set; }
    public DateTime date_last_updated { get; set; }
    public string last_updated_by { get; set; }

    public string state_of_last_known_residence { get; set; }

    public string death_certificate { get; set; }
    public string autopsy_report { get; set; }
    public string birth_certificate_parent_section { get; set; }
    public string birth_certificate_infant_or_fetal_death_section { get; set; }
    public string prenatal_care_record { get; set; }
    public string other_medical_visits { get; set; }
    public string er_visits_and_hospitalizations { get; set; }
    public string social_and_psychological_profile { get; set; }
    public string informant_interviews { get; set; }
    public string committe_review_worksheet { get; set; }

}


