
using System;
using System.Collections.Generic;

namespace mmria.case_version.mmria.v240616;

public sealed class _03787BE62E53DBA5CE6F69EA96515700 : IConvertDictionary
{
	public _03787BE62E53DBA5CE6F69EA96515700()
	{
	}
	public string committee_recommendations { get; set; }
	public double? prevention { get; set; }
	public double? impact_level { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		committee_recommendations = mmria_case.GetTextAreaField(p_value, "committee_recommendations", "committee_review/recommendations_of_committee/committee_recommendations");
		prevention = mmria_case.GetNumberListField(p_value, "prevention", "committee_review/recommendations_of_committee/prevention");
		impact_level = mmria_case.GetNumberListField(p_value, "impact_level", "committee_review/recommendations_of_committee/impact_level");
	}
}

public sealed class _65D125FC8B7F77F97A3B80D4C65928B0 : IConvertDictionary
{
	public _65D125FC8B7F77F97A3B80D4C65928B0()
	{
	}
	public string description { get; set; }
	public double? @class { get; set; }
	public double? category { get; set; }
	public string committee_recommendations { get; set; }
	public double? recommendation_level { get; set; }
	public double? prevention { get; set; }
	public double? impact_level { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		description = mmria_case.GetStringField(p_value, "description", "committee_review/critical_factors_worksheet/description");
		@class = mmria_case.GetNumberListField(p_value, "class", "committee_review/critical_factors_worksheet/class");
		category = mmria_case.GetNumberListField(p_value, "category", "committee_review/critical_factors_worksheet/category");
		committee_recommendations = mmria_case.GetTextAreaField(p_value, "committee_recommendations", "committee_review/critical_factors_worksheet/committee_recommendations");
		recommendation_level = mmria_case.GetNumberListField(p_value, "recommendation_level", "committee_review/critical_factors_worksheet/recommendation_level");
		prevention = mmria_case.GetNumberListField(p_value, "prevention", "committee_review/critical_factors_worksheet/prevention");
		impact_level = mmria_case.GetNumberListField(p_value, "impact_level", "committee_review/critical_factors_worksheet/impact_level");
	}
}

public sealed class _9454814D91C4C5F2C88BCCE5E93B074F : IConvertDictionary
{
	public _9454814D91C4C5F2C88BCCE5E93B074F()
	{
	}
	public double? type { get; set; }
	public string cause_descriptive { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		type = mmria_case.GetNumberListField(p_value, "type", "committee_review/committee_determination_of_causes_of_death/type");
		cause_descriptive = mmria_case.GetStringField(p_value, "cause_descriptive", "committee_review/committee_determination_of_causes_of_death/cause_descriptive");
		comments = mmria_case.GetStringField(p_value, "comments", "committee_review/committee_determination_of_causes_of_death/comments");
	}
}

public sealed class _62AEF5C4D8129ED98ECA69F7779FCBFC : IConvertDictionary
{
	public _62AEF5C4D8129ED98ECA69F7779FCBFC()
	{
		committee_determination_of_causes_of_death = new ();
		critical_factors_worksheet = new ();
		recommendations_of_committee = new ();
	}
	public DateOnly? date_of_review { get; set; }
	public double? pregnancy_relatedness { get; set; }
	public double? estimate_degree_relevant_information_available { get; set; }
	public double? does_committee_agree_with_cod_on_death_certificate { get; set; }
	public string pmss_mm { get; set; }
	public string pmss_mm_secondary { get; set; }
	public List<_9454814D91C4C5F2C88BCCE5E93B074F> committee_determination_of_causes_of_death{ get;set;}
	public double? did_obesity_contribute_to_the_death { get; set; }
	public double? did_discrimination_contribute_to_the_death { get; set; }
	public double? did_mental_health_conditions_contribute_to_the_death { get; set; }
	public double? did_substance_use_disorder_contribute_to_the_death { get; set; }
	public double? was_this_death_a_sucide { get; set; }
	public double? was_this_death_a_homicide { get; set; }
	public double? means_of_fatal_injury { get; set; }
	public string specify_other_means_fatal_injury { get; set; }
	public double? if_homicide_relationship_of_perpetrator { get; set; }
	public string specify_other_relationship { get; set; }
	public double? was_this_death_preventable { get; set; }
	public double? chance_to_alter_outcome { get; set; }
	public List<_65D125FC8B7F77F97A3B80D4C65928B0> critical_factors_worksheet{ get;set;}
	public string cr_add_recs { get; set; }
	public string notes_about_key_circumstances_surrounding_death { get; set; }
	public List<_03787BE62E53DBA5CE6F69EA96515700> recommendations_of_committee{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_review = mmria_case.GetDateField(p_value, "date_of_review", "committee_review/date_of_review");
		pregnancy_relatedness = mmria_case.GetNumberListField(p_value, "pregnancy_relatedness", "committee_review/pregnancy_relatedness");
		estimate_degree_relevant_information_available = mmria_case.GetNumberListField(p_value, "estimate_degree_relevant_information_available", "committee_review/estimate_degree_relevant_information_available");
		does_committee_agree_with_cod_on_death_certificate = mmria_case.GetNumberListField(p_value, "does_committee_agree_with_cod_on_death_certificate", "committee_review/does_committee_agree_with_cod_on_death_certificate");
		pmss_mm = mmria_case.GetStringListField(p_value, "pmss_mm", "committee_review/pmss_mm");
		pmss_mm_secondary = mmria_case.GetStringListField(p_value, "pmss_mm_secondary", "committee_review/pmss_mm_secondary");
		committee_determination_of_causes_of_death = mmria_case.GetGridField<_9454814D91C4C5F2C88BCCE5E93B074F>(p_value, "committee_determination_of_causes_of_death", "committee_review/committee_determination_of_causes_of_death");
		did_obesity_contribute_to_the_death = mmria_case.GetNumberListField(p_value, "did_obesity_contribute_to_the_death", "committee_review/did_obesity_contribute_to_the_death");
		did_discrimination_contribute_to_the_death = mmria_case.GetNumberListField(p_value, "did_discrimination_contribute_to_the_death", "committee_review/did_discrimination_contribute_to_the_death");
		did_mental_health_conditions_contribute_to_the_death = mmria_case.GetNumberListField(p_value, "did_mental_health_conditions_contribute_to_the_death", "committee_review/did_mental_health_conditions_contribute_to_the_death");
		did_substance_use_disorder_contribute_to_the_death = mmria_case.GetNumberListField(p_value, "did_substance_use_disorder_contribute_to_the_death", "committee_review/did_substance_use_disorder_contribute_to_the_death");
		was_this_death_a_sucide = mmria_case.GetNumberListField(p_value, "was_this_death_a_sucide", "committee_review/was_this_death_a_sucide");
		was_this_death_a_homicide = mmria_case.GetNumberListField(p_value, "was_this_death_a_homicide", "committee_review/was_this_death_a_homicide");
		means_of_fatal_injury = mmria_case.GetNumberListField(p_value, "means_of_fatal_injury", "committee_review/means_of_fatal_injury");
		specify_other_means_fatal_injury = mmria_case.GetStringField(p_value, "specify_other_means_fatal_injury", "committee_review/specify_other_means_fatal_injury");
		if_homicide_relationship_of_perpetrator = mmria_case.GetNumberListField(p_value, "if_homicide_relationship_of_perpetrator", "committee_review/if_homicide_relationship_of_perpetrator");
		specify_other_relationship = mmria_case.GetStringField(p_value, "specify_other_relationship", "committee_review/specify_other_relationship");
		was_this_death_preventable = mmria_case.GetNumberListField(p_value, "was_this_death_preventable", "committee_review/was_this_death_preventable");
		chance_to_alter_outcome = mmria_case.GetNumberListField(p_value, "chance_to_alter_outcome", "committee_review/chance_to_alter_outcome");
		critical_factors_worksheet = mmria_case.GetGridField<_65D125FC8B7F77F97A3B80D4C65928B0>(p_value, "critical_factors_worksheet", "committee_review/critical_factors_worksheet");
		cr_add_recs = mmria_case.GetTextAreaField(p_value, "cr_add_recs", "committee_review/cr_add_recs");
		notes_about_key_circumstances_surrounding_death = mmria_case.GetTextAreaField(p_value, "notes_about_key_circumstances_surrounding_death", "committee_review/notes_about_key_circumstances_surrounding_death");
		recommendations_of_committee = mmria_case.GetGridField<_03787BE62E53DBA5CE6F69EA96515700>(p_value, "recommendations_of_committee", "committee_review/recommendations_of_committee");
	}
}

public sealed class _A35F564798944667E91C53B3A3DA359D : IConvertDictionary
{
	public _A35F564798944667E91C53B3A3DA359D()
	{
	}
	public string case_opening_overview { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		case_opening_overview = mmria_case.GetTextAreaField(p_value, "case_opening_overview", "case_narrative/case_opening_overview");
	}
}

public sealed class _3324B59308063F372636E2BE07764C8B : IConvertDictionary
{
	public _3324B59308063F372636E2BE07764C8B()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "informant_interviews/date_of_interview/month");
		day = mmria_case.GetNumberListField(p_value, "day", "informant_interviews/date_of_interview/day");
		year = mmria_case.GetNumberListField(p_value, "year", "informant_interviews/date_of_interview/year");
	}
}

		public sealed class _18CD53D47CBDE2540A9EF3EC5B51E0BA : IConvertDictionary
		{
	public _18CD53D47CBDE2540A9EF3EC5B51E0BA()
{
		date_of_interview = new ();
	}
	public _3324B59308063F372636E2BE07764C8B date_of_interview{ get;set;}
	public double? interview_type { get; set; }
	public string other_interview_type { get; set; }
	public double? age_group { get; set; }
	public double? relationship_to_deceased { get; set; }
	public string other_relationship { get; set; }
	public string interview_narrative { get; set; }
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_interview = mmria_case.GetGroupField<_3324B59308063F372636E2BE07764C8B>(p_value, "date_of_interview", "informant_interviews/date_of_interview");
		interview_type = mmria_case.GetNumberListField(p_value, "interview_type", "informant_interviews/interview_type");
		other_interview_type = mmria_case.GetStringField(p_value, "other_interview_type", "informant_interviews/other_interview_type");
		age_group = mmria_case.GetNumberListField(p_value, "age_group", "informant_interviews/age_group");
		relationship_to_deceased = mmria_case.GetNumberListField(p_value, "relationship_to_deceased", "informant_interviews/relationship_to_deceased");
		other_relationship = mmria_case.GetStringField(p_value, "other_relationship", "informant_interviews/other_relationship");
		interview_narrative = mmria_case.GetTextAreaField(p_value, "interview_narrative", "informant_interviews/interview_narrative");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "informant_interviews/reviewer_note");
	}
}

public sealed class _C03B8001285602379CED67187C028FDD : IConvertDictionary
{
	public _C03B8001285602379CED67187C028FDD()
	{
	}
	public DateOnly? date_of_screening { get; set; }
	public double? gestational_weeks { get; set; }
	public double? gestational_days { get; set; }
	public double? days_postpartum { get; set; }
	public double? screening_tool { get; set; }
	public string other_screening_tool { get; set; }
	public double? referral_for_treatment { get; set; }
	public string findings { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_screening = mmria_case.GetDateField(p_value, "date_of_screening", "mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening");
		gestational_weeks = mmria_case.GetNumberField(p_value, "gestational_weeks", "mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks");
		gestational_days = mmria_case.GetNumberField(p_value, "gestational_days", "mental_health_profile/were_there_documented_mental_health_conditions/gestational_days");
		days_postpartum = mmria_case.GetNumberField(p_value, "days_postpartum", "mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum");
		screening_tool = mmria_case.GetNumberListField(p_value, "screening_tool", "mental_health_profile/were_there_documented_mental_health_conditions/screening_tool");
		other_screening_tool = mmria_case.GetStringField(p_value, "other_screening_tool", "mental_health_profile/were_there_documented_mental_health_conditions/other_screening_tool");
		referral_for_treatment = mmria_case.GetNumberListField(p_value, "referral_for_treatment", "mental_health_profile/were_there_documented_mental_health_conditions/referral_for_treatment");
		findings = mmria_case.GetStringField(p_value, "findings", "mental_health_profile/were_there_documented_mental_health_conditions/findings");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "mental_health_profile/were_there_documented_mental_health_conditions/comments");
	}
}

public sealed class _39DB9B13455B28F2E49AD74B4CF2D85A : IConvertDictionary
{
	public _39DB9B13455B28F2E49AD74B4CF2D85A()
	{
	}
	public double? condition { get; set; }
	public string mhpdpmhc_condi_othsp { get; set; }
	public string duration_of_condition { get; set; }
	public string treatments { get; set; }
	public string duration_of_tx { get; set; }
	public double? treatment_changed_during_pregnancy { get; set; }
	public double? dosage_changed_during_pregnancy { get; set; }
	public double? if_yes_mental_health_provider_consultation_during_this_pregnancy { get; set; }
	public double? did_patient_adhere_to_treatment { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		condition = mmria_case.GetNumberListField(p_value, "condition", "mental_health_profile/documented_preexisting_mental_health_conditions/condition");
		mhpdpmhc_condi_othsp = mmria_case.GetStringField(p_value, "mhpdpmhc_condi_othsp", "mental_health_profile/documented_preexisting_mental_health_conditions/mhpdpmhc_condi_othsp");
		duration_of_condition = mmria_case.GetStringField(p_value, "duration_of_condition", "mental_health_profile/documented_preexisting_mental_health_conditions/duration_of_condition");
		treatments = mmria_case.GetStringField(p_value, "treatments", "mental_health_profile/documented_preexisting_mental_health_conditions/treatments");
		duration_of_tx = mmria_case.GetStringField(p_value, "duration_of_tx", "mental_health_profile/documented_preexisting_mental_health_conditions/duration_of_tx");
		treatment_changed_during_pregnancy = mmria_case.GetNumberListField(p_value, "treatment_changed_during_pregnancy", "mental_health_profile/documented_preexisting_mental_health_conditions/treatment_changed_during_pregnancy");
		dosage_changed_during_pregnancy = mmria_case.GetNumberListField(p_value, "dosage_changed_during_pregnancy", "mental_health_profile/documented_preexisting_mental_health_conditions/dosage_changed_during_pregnancy");
		if_yes_mental_health_provider_consultation_during_this_pregnancy = mmria_case.GetNumberListField(p_value, "if_yes_mental_health_provider_consultation_during_this_pregnancy", "mental_health_profile/documented_preexisting_mental_health_conditions/if_yes_mental_health_provider_consultation_during_this_pregnancy");
		did_patient_adhere_to_treatment = mmria_case.GetNumberListField(p_value, "did_patient_adhere_to_treatment", "mental_health_profile/documented_preexisting_mental_health_conditions/did_patient_adhere_to_treatment");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "mental_health_profile/documented_preexisting_mental_health_conditions/comments");
	}
}

public sealed class _06AA314F235917500C48AB5E3CD1C034 : IConvertDictionary
{
	public _06AA314F235917500C48AB5E3CD1C034()
	{
		documented_preexisting_mental_health_conditions = new ();
		were_there_documented_mental_health_conditions = new ();
		mental_health_conditions_prior_to_the_most_recent_pregnancy = new ();
		mental_health_conditions_during_the_most_recent_pregnancy = new ();
		mental_health_conditions_after_the_most_recent_pregnancy = new ();
	}
	public double? were_there_documented_preexisting_mental_health_conditions { get; set; }
	public List<_39DB9B13455B28F2E49AD74B4CF2D85A> documented_preexisting_mental_health_conditions{ get;set;}
	public List<_C03B8001285602379CED67187C028FDD> were_there_documented_mental_health_conditions{ get;set;}
	public List<double> mental_health_conditions_prior_to_the_most_recent_pregnancy { get; set; }
	public string other_prior_to_pregnancy { get; set; }
	public List<double> mental_health_conditions_during_the_most_recent_pregnancy { get; set; }
	public string other_during_pregnancy { get; set; }
	public List<double> mental_health_conditions_after_the_most_recent_pregnancy { get; set; }
	public string other_after_pregnancy { get; set; }
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		were_there_documented_preexisting_mental_health_conditions = mmria_case.GetNumberListField(p_value, "were_there_documented_preexisting_mental_health_conditions", "mental_health_profile/were_there_documented_preexisting_mental_health_conditions");
		documented_preexisting_mental_health_conditions = mmria_case.GetGridField<_39DB9B13455B28F2E49AD74B4CF2D85A>(p_value, "documented_preexisting_mental_health_conditions", "mental_health_profile/documented_preexisting_mental_health_conditions");
		were_there_documented_mental_health_conditions = mmria_case.GetGridField<_C03B8001285602379CED67187C028FDD>(p_value, "were_there_documented_mental_health_conditions", "mental_health_profile/were_there_documented_mental_health_conditions");
		mental_health_conditions_prior_to_the_most_recent_pregnancy = mmria_case.GetMultiSelectNumberListField(p_value, "mental_health_conditions_prior_to_the_most_recent_pregnancy", "mental_health_profile/mental_health_conditions_prior_to_the_most_recent_pregnancy");
		other_prior_to_pregnancy = mmria_case.GetStringField(p_value, "other_prior_to_pregnancy", "mental_health_profile/other_prior_to_pregnancy");
		mental_health_conditions_during_the_most_recent_pregnancy = mmria_case.GetMultiSelectNumberListField(p_value, "mental_health_conditions_during_the_most_recent_pregnancy", "mental_health_profile/mental_health_conditions_during_the_most_recent_pregnancy");
		other_during_pregnancy = mmria_case.GetStringField(p_value, "other_during_pregnancy", "mental_health_profile/other_during_pregnancy");
		mental_health_conditions_after_the_most_recent_pregnancy = mmria_case.GetMultiSelectNumberListField(p_value, "mental_health_conditions_after_the_most_recent_pregnancy", "mental_health_profile/mental_health_conditions_after_the_most_recent_pregnancy");
		other_after_pregnancy = mmria_case.GetStringField(p_value, "other_after_pregnancy", "mental_health_profile/other_after_pregnancy");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "mental_health_profile/reviewer_note");
	}
}

public sealed class _7304BC460354BFEEA44959EE266F14DC : IConvertDictionary
{
	public _7304BC460354BFEEA44959EE266F14DC()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string country_of_last_residence { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public double? estimated_distance { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "medical_transport/destination_information/address/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "medical_transport/destination_information/address/apartment");
		city = mmria_case.GetStringField(p_value, "city", "medical_transport/destination_information/address/city");
		state = mmria_case.GetStringListField(p_value, "state", "medical_transport/destination_information/address/state");
		country_of_last_residence = mmria_case.GetStringListField(p_value, "country_of_last_residence", "medical_transport/destination_information/address/country_of_last_residence");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "medical_transport/destination_information/address/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "medical_transport/destination_information/address/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "medical_transport/destination_information/address/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "medical_transport/destination_information/address/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "medical_transport/destination_information/address/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "medical_transport/destination_information/address/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "medical_transport/destination_information/address/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "medical_transport/destination_information/address/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "medical_transport/destination_information/address/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "medical_transport/destination_information/address/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "medical_transport/destination_information/address/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "medical_transport/destination_information/address/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "medical_transport/destination_information/address/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "medical_transport/destination_information/address/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "medical_transport/destination_information/address/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "medical_transport/destination_information/address/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "medical_transport/destination_information/address/census_cbsa_micro");
		estimated_distance = mmria_case.GetNumberField(p_value, "estimated_distance", "medical_transport/destination_information/address/estimated_distance");
	}
}

public sealed class _016DDBD8BE4E0CA726E60B2A0B7C6F20 : IConvertDictionary
{
	public _016DDBD8BE4E0CA726E60B2A0B7C6F20()
	{
		address = new ();
	}
	public string place_of_destination { get; set; }
	public string destination_type { get; set; }
	public string place_of_origin_other { get; set; }
	public _7304BC460354BFEEA44959EE266F14DC address{ get;set;}
	public double? trauma_level_of_care { get; set; }
	public string other_trauma_level_of_care { get; set; }
	public double? maternal_level_of_care { get; set; }
	public string other_maternal_level_of_care { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		place_of_destination = mmria_case.GetStringField(p_value, "place_of_destination", "medical_transport/destination_information/place_of_destination");
		destination_type = mmria_case.GetStringListField(p_value, "destination_type", "medical_transport/destination_information/destination_type");
		place_of_origin_other = mmria_case.GetStringField(p_value, "place_of_origin_other", "medical_transport/destination_information/place_of_origin_other");
		address = mmria_case.GetGroupField<_7304BC460354BFEEA44959EE266F14DC>(p_value, "address", "medical_transport/destination_information/address");
		trauma_level_of_care = mmria_case.GetNumberListField(p_value, "trauma_level_of_care", "medical_transport/destination_information/trauma_level_of_care");
		other_trauma_level_of_care = mmria_case.GetStringField(p_value, "other_trauma_level_of_care", "medical_transport/destination_information/other_trauma_level_of_care");
		maternal_level_of_care = mmria_case.GetNumberListField(p_value, "maternal_level_of_care", "medical_transport/destination_information/maternal_level_of_care");
		other_maternal_level_of_care = mmria_case.GetStringField(p_value, "other_maternal_level_of_care", "medical_transport/destination_information/other_maternal_level_of_care");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "medical_transport/destination_information/comments");
	}
}

public sealed class _9E38EAE350ADD1FBBA07FB7CC906C6B1 : IConvertDictionary
{
	public _9E38EAE350ADD1FBBA07FB7CC906C6B1()
	{
	}
	public DateTime? date_and_time { get; set; }
	public double? gestational_weeks { get; set; }
	public double? gestational_days { get; set; }
	public double? systolic_bp { get; set; }
	public double? diastolic_bp { get; set; }
	public double? heart_rate { get; set; }
	public double? oxygen_saturation { get; set; }
	public double? blood_sugar { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "medical_transport/transport_vital_signs/date_and_time");
		gestational_weeks = mmria_case.GetNumberField(p_value, "gestational_weeks", "medical_transport/transport_vital_signs/gestational_weeks");
		gestational_days = mmria_case.GetNumberField(p_value, "gestational_days", "medical_transport/transport_vital_signs/gestational_days");
		systolic_bp = mmria_case.GetNumberField(p_value, "systolic_bp", "medical_transport/transport_vital_signs/systolic_bp");
		diastolic_bp = mmria_case.GetNumberField(p_value, "diastolic_bp", "medical_transport/transport_vital_signs/diastolic_bp");
		heart_rate = mmria_case.GetNumberField(p_value, "heart_rate", "medical_transport/transport_vital_signs/heart_rate");
		oxygen_saturation = mmria_case.GetNumberField(p_value, "oxygen_saturation", "medical_transport/transport_vital_signs/oxygen_saturation");
		blood_sugar = mmria_case.GetNumberField(p_value, "blood_sugar", "medical_transport/transport_vital_signs/blood_sugar");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "medical_transport/transport_vital_signs/comments");
	}
}

public sealed class _C253C7CC2A0397535E3968F4A3E8EBBC : IConvertDictionary
{
	public _C253C7CC2A0397535E3968F4A3E8EBBC()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string country { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "medical_transport/origin_information/address/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "medical_transport/origin_information/address/apartment");
		city = mmria_case.GetStringField(p_value, "city", "medical_transport/origin_information/address/city");
		state = mmria_case.GetStringListField(p_value, "state", "medical_transport/origin_information/address/state");
		country = mmria_case.GetStringListField(p_value, "country", "medical_transport/origin_information/address/country");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "medical_transport/origin_information/address/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "medical_transport/origin_information/address/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "medical_transport/origin_information/address/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "medical_transport/origin_information/address/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "medical_transport/origin_information/address/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "medical_transport/origin_information/address/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "medical_transport/origin_information/address/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "medical_transport/origin_information/address/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "medical_transport/origin_information/address/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "medical_transport/origin_information/address/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "medical_transport/origin_information/address/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "medical_transport/origin_information/address/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "medical_transport/origin_information/address/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "medical_transport/origin_information/address/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "medical_transport/origin_information/address/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "medical_transport/origin_information/address/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "medical_transport/origin_information/address/census_cbsa_micro");
	}
}

public sealed class _296A49A114016CF9781A8EAD842554D7 : IConvertDictionary
{
	public _296A49A114016CF9781A8EAD842554D7()
	{
		address = new ();
	}
	public string place_of_origin { get; set; }
	public string place_of_origin_other { get; set; }
	public _C253C7CC2A0397535E3968F4A3E8EBBC address{ get;set;}
	public double? trauma_level_of_care { get; set; }
	public string other_trauma_level_of_care { get; set; }
	public double? maternal_level_of_care { get; set; }
	public string other_maternal_level_of_care { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		place_of_origin = mmria_case.GetStringListField(p_value, "place_of_origin", "medical_transport/origin_information/place_of_origin");
		place_of_origin_other = mmria_case.GetStringField(p_value, "place_of_origin_other", "medical_transport/origin_information/place_of_origin_other");
		address = mmria_case.GetGroupField<_C253C7CC2A0397535E3968F4A3E8EBBC>(p_value, "address", "medical_transport/origin_information/address");
		trauma_level_of_care = mmria_case.GetNumberListField(p_value, "trauma_level_of_care", "medical_transport/origin_information/trauma_level_of_care");
		other_trauma_level_of_care = mmria_case.GetStringField(p_value, "other_trauma_level_of_care", "medical_transport/origin_information/other_trauma_level_of_care");
		maternal_level_of_care = mmria_case.GetNumberListField(p_value, "maternal_level_of_care", "medical_transport/origin_information/maternal_level_of_care");
		other_maternal_level_of_care = mmria_case.GetStringField(p_value, "other_maternal_level_of_care", "medical_transport/origin_information/other_maternal_level_of_care");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "medical_transport/origin_information/comments");
	}
}

public sealed class _D9426331BDEDF9BD2CEAE93CFEC64630 : IConvertDictionary
{
	public _D9426331BDEDF9BD2CEAE93CFEC64630()
	{
	}
	public DateTime? call_received { get; set; }
	public DateTime? depart_for_patient_origin { get; set; }
	public DateTime? arrive_at_patient_origin { get; set; }
	public DateTime? patient_contact { get; set; }
	public DateTime? depart_for_referring_facility { get; set; }
	public DateTime? arrive_at_referring_facility { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		call_received = mmria_case.GetDateTimeField(p_value, "call_received", "medical_transport/timing_of_transport/call_received");
		depart_for_patient_origin = mmria_case.GetDateTimeField(p_value, "depart_for_patient_origin", "medical_transport/timing_of_transport/depart_for_patient_origin");
		arrive_at_patient_origin = mmria_case.GetDateTimeField(p_value, "arrive_at_patient_origin", "medical_transport/timing_of_transport/arrive_at_patient_origin");
		patient_contact = mmria_case.GetDateTimeField(p_value, "patient_contact", "medical_transport/timing_of_transport/patient_contact");
		depart_for_referring_facility = mmria_case.GetDateTimeField(p_value, "depart_for_referring_facility", "medical_transport/timing_of_transport/depart_for_referring_facility");
		arrive_at_referring_facility = mmria_case.GetDateTimeField(p_value, "arrive_at_referring_facility", "medical_transport/timing_of_transport/arrive_at_referring_facility");
	}
}

public sealed class _14A430A7CEAC8A1A9E5DDC9741D8AA8B : IConvertDictionary
{
	public _14A430A7CEAC8A1A9E5DDC9741D8AA8B()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public double? days_postpartum { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "medical_transport/date_of_transport/month");
		day = mmria_case.GetNumberListField(p_value, "day", "medical_transport/date_of_transport/day");
		year = mmria_case.GetNumberListField(p_value, "year", "medical_transport/date_of_transport/year");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "medical_transport/date_of_transport/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "medical_transport/date_of_transport/gestational_age_days");
		days_postpartum = mmria_case.GetNumberField(p_value, "days_postpartum", "medical_transport/date_of_transport/days_postpartum");
	}
}

		public sealed class _9206DAB82DFEDA2BC11D83175919BA02 : IConvertDictionary
		{
	public _9206DAB82DFEDA2BC11D83175919BA02()
{
		date_of_transport = new ();
		timing_of_transport = new ();
		origin_information = new ();
		transport_vital_signs = new ();
		destination_information = new ();
	}
	public _14A430A7CEAC8A1A9E5DDC9741D8AA8B date_of_transport{ get;set;}
	public string reason_for_transport { get; set; }
	public string maternal_conditions { get; set; }
	public double? who_managed_the_transport { get; set; }
	public string other_transport_manager { get; set; }
	public double? transport_vehicle { get; set; }
	public string other_transport_vehicle { get; set; }
	public _D9426331BDEDF9BD2CEAE93CFEC64630 timing_of_transport{ get;set;}
	public _296A49A114016CF9781A8EAD842554D7 origin_information{ get;set;}
	public string procedures_before_transport { get; set; }
	public string procedures_during_transport { get; set; }
	public List<_9E38EAE350ADD1FBBA07FB7CC906C6B1> transport_vital_signs{ get;set;}
	public string mental_status_of_patient_during_transport { get; set; }
	public string documented_pertinent_oral_statements_made_by_patient_and_other_on_scene { get; set; }
	public _016DDBD8BE4E0CA726E60B2A0B7C6F20 destination_information{ get;set;}
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_transport = mmria_case.GetGroupField<_14A430A7CEAC8A1A9E5DDC9741D8AA8B>(p_value, "date_of_transport", "medical_transport/date_of_transport");
		reason_for_transport = mmria_case.GetTextAreaField(p_value, "reason_for_transport", "medical_transport/reason_for_transport");
		maternal_conditions = mmria_case.GetTextAreaField(p_value, "maternal_conditions", "medical_transport/maternal_conditions");
		who_managed_the_transport = mmria_case.GetNumberListField(p_value, "who_managed_the_transport", "medical_transport/who_managed_the_transport");
		other_transport_manager = mmria_case.GetStringField(p_value, "other_transport_manager", "medical_transport/other_transport_manager");
		transport_vehicle = mmria_case.GetNumberListField(p_value, "transport_vehicle", "medical_transport/transport_vehicle");
		other_transport_vehicle = mmria_case.GetStringField(p_value, "other_transport_vehicle", "medical_transport/other_transport_vehicle");
		timing_of_transport = mmria_case.GetGroupField<_D9426331BDEDF9BD2CEAE93CFEC64630>(p_value, "timing_of_transport", "medical_transport/timing_of_transport");
		origin_information = mmria_case.GetGroupField<_296A49A114016CF9781A8EAD842554D7>(p_value, "origin_information", "medical_transport/origin_information");
		procedures_before_transport = mmria_case.GetTextAreaField(p_value, "procedures_before_transport", "medical_transport/procedures_before_transport");
		procedures_during_transport = mmria_case.GetTextAreaField(p_value, "procedures_during_transport", "medical_transport/procedures_during_transport");
		transport_vital_signs = mmria_case.GetGridField<_9E38EAE350ADD1FBBA07FB7CC906C6B1>(p_value, "transport_vital_signs", "medical_transport/transport_vital_signs");
		mental_status_of_patient_during_transport = mmria_case.GetTextAreaField(p_value, "mental_status_of_patient_during_transport", "medical_transport/mental_status_of_patient_during_transport");
		documented_pertinent_oral_statements_made_by_patient_and_other_on_scene = mmria_case.GetTextAreaField(p_value, "documented_pertinent_oral_statements_made_by_patient_and_other_on_scene", "medical_transport/documented_pertinent_oral_statements_made_by_patient_and_other_on_scene");
		destination_information = mmria_case.GetGroupField<_016DDBD8BE4E0CA726E60B2A0B7C6F20>(p_value, "destination_information", "medical_transport/destination_information");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "medical_transport/reviewer_note");
	}
}

public sealed class _68170408B3167DC59913CD9A2CA1357B : IConvertDictionary
{
	public _68170408B3167DC59913CD9A2CA1357B()
	{
	}
	public string abnormal_findings { get; set; }
	public string recommendations_and_action_plans { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		abnormal_findings = mmria_case.GetStringField(p_value, "abnormal_findings", "other_medical_office_visits/new_grid/abnormal_findings");
		recommendations_and_action_plans = mmria_case.GetStringField(p_value, "recommendations_and_action_plans", "other_medical_office_visits/new_grid/recommendations_and_action_plans");
	}
}

public sealed class _E4CA2E16DDEE982A85D4D22DCC31E70F : IConvertDictionary
{
	public _E4CA2E16DDEE982A85D4D22DCC31E70F()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string medication_name { get; set; }
	public string dose_frequeny_duration { get; set; }
	public string adverse_reaction { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "other_medical_office_visits/medications/date_and_time");
		medication_name = mmria_case.GetStringField(p_value, "medication_name", "other_medical_office_visits/medications/medication_name");
		dose_frequeny_duration = mmria_case.GetStringField(p_value, "dose_frequeny_duration", "other_medical_office_visits/medications/dose_frequeny_duration");
		adverse_reaction = mmria_case.GetStringField(p_value, "adverse_reaction", "other_medical_office_visits/medications/adverse_reaction");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "other_medical_office_visits/medications/comments");
	}
}

public sealed class _C1EFC5F4AC8542D8630ED4C880B9EDD8 : IConvertDictionary
{
	public _C1EFC5F4AC8542D8630ED4C880B9EDD8()
	{
	}
	public DateOnly? date { get; set; }
	public string speciality { get; set; }
	public string reason { get; set; }
	public string recommendations { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "other_medical_office_visits/referrals_and_consultations/date");
		speciality = mmria_case.GetStringField(p_value, "speciality", "other_medical_office_visits/referrals_and_consultations/speciality");
		reason = mmria_case.GetStringField(p_value, "reason", "other_medical_office_visits/referrals_and_consultations/reason");
		recommendations = mmria_case.GetStringField(p_value, "recommendations", "other_medical_office_visits/referrals_and_consultations/recommendations");
	}
}

public sealed class _0BC6600AA8457AB07561633DC8DBF6C3 : IConvertDictionary
{
	public _0BC6600AA8457AB07561633DC8DBF6C3()
	{
	}
	public double? body_system { get; set; }
	public string finding { get; set; }
	public string comment { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		body_system = mmria_case.GetNumberListField(p_value, "body_system", "other_medical_office_visits/physical_exam/body_system");
		finding = mmria_case.GetStringField(p_value, "finding", "other_medical_office_visits/physical_exam/finding");
		comment = mmria_case.GetTextAreaField(p_value, "comment", "other_medical_office_visits/physical_exam/comment");
	}
}

public sealed class _8558B3DB502E84E098AC581E499D032A : IConvertDictionary
{
	public _8558B3DB502E84E098AC581E499D032A()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string procedure { get; set; }
	public string target_procedure { get; set; }
	public string finding { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "other_medical_office_visits/diagnostic_imaging_and_other_technology/date_and_time");
		procedure = mmria_case.GetStringField(p_value, "procedure", "other_medical_office_visits/diagnostic_imaging_and_other_technology/procedure");
		target_procedure = mmria_case.GetStringField(p_value, "target_procedure", "other_medical_office_visits/diagnostic_imaging_and_other_technology/target_procedure");
		finding = mmria_case.GetStringField(p_value, "finding", "other_medical_office_visits/diagnostic_imaging_and_other_technology/finding");
	}
}

public sealed class _06921F604DB46ECCAB769043FEC60E87 : IConvertDictionary
{
	public _06921F604DB46ECCAB769043FEC60E87()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string specimen { get; set; }
	public string test_name { get; set; }
	public string result { get; set; }
	public double? diagnostic_level { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "other_medical_office_visits/laboratory_tests/date_and_time");
		specimen = mmria_case.GetStringField(p_value, "specimen", "other_medical_office_visits/laboratory_tests/specimen");
		test_name = mmria_case.GetStringField(p_value, "test_name", "other_medical_office_visits/laboratory_tests/test_name");
		result = mmria_case.GetStringField(p_value, "result", "other_medical_office_visits/laboratory_tests/result");
		diagnostic_level = mmria_case.GetNumberListField(p_value, "diagnostic_level", "other_medical_office_visits/laboratory_tests/diagnostic_level");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "other_medical_office_visits/laboratory_tests/comments");
	}
}

public sealed class _8E6189F67C4CB139080D5867B1DA8617 : IConvertDictionary
{
	public _8E6189F67C4CB139080D5867B1DA8617()
	{
	}
	public DateTime? date_and_time { get; set; }
	public double? temperature { get; set; }
	public double? pulse { get; set; }
	public double? respiration { get; set; }
	public double? bp_systolic { get; set; }
	public double? bp_diastolic { get; set; }
	public double? oxygen_saturation { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "other_medical_office_visits/vital_signs/date_and_time");
		temperature = mmria_case.GetNumberField(p_value, "temperature", "other_medical_office_visits/vital_signs/temperature");
		pulse = mmria_case.GetNumberField(p_value, "pulse", "other_medical_office_visits/vital_signs/pulse");
		respiration = mmria_case.GetNumberField(p_value, "respiration", "other_medical_office_visits/vital_signs/respiration");
		bp_systolic = mmria_case.GetNumberField(p_value, "bp_systolic", "other_medical_office_visits/vital_signs/bp_systolic");
		bp_diastolic = mmria_case.GetNumberField(p_value, "bp_diastolic", "other_medical_office_visits/vital_signs/bp_diastolic");
		oxygen_saturation = mmria_case.GetNumberField(p_value, "oxygen_saturation", "other_medical_office_visits/vital_signs/oxygen_saturation");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "other_medical_office_visits/vital_signs/comments");
	}
}

public sealed class _B5CBB37577260A8F84CFD2C24F0461AE : IConvertDictionary
{
	public _B5CBB37577260A8F84CFD2C24F0461AE()
	{
	}
	public string finding { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		finding = mmria_case.GetStringField(p_value, "finding", "other_medical_office_visits/relevant_social_history/finding");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "other_medical_office_visits/relevant_social_history/comments");
	}
}

public sealed class _C269EDFA69E501F5A86850D3AD13ADE2 : IConvertDictionary
{
	public _C269EDFA69E501F5A86850D3AD13ADE2()
	{
	}
	public string finding { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		finding = mmria_case.GetStringField(p_value, "finding", "other_medical_office_visits/relevant_family_history/finding");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "other_medical_office_visits/relevant_family_history/comments");
	}
}

public sealed class _1345442838A1AC070037B7F85CA7E067 : IConvertDictionary
{
	public _1345442838A1AC070037B7F85CA7E067()
	{
	}
	public string finding { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		finding = mmria_case.GetStringField(p_value, "finding", "other_medical_office_visits/relevant_medical_history/finding");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "other_medical_office_visits/relevant_medical_history/comments");
	}
}

public sealed class _98BB954A3934D72B967DE0372E7B381C : IConvertDictionary
{
	public _98BB954A3934D72B967DE0372E7B381C()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "other_medical_office_visits/location_of_medical_care_facility/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "other_medical_office_visits/location_of_medical_care_facility/apartment");
		city = mmria_case.GetStringField(p_value, "city", "other_medical_office_visits/location_of_medical_care_facility/city");
		state = mmria_case.GetStringListField(p_value, "state", "other_medical_office_visits/location_of_medical_care_facility/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "other_medical_office_visits/location_of_medical_care_facility/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "other_medical_office_visits/location_of_medical_care_facility/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "other_medical_office_visits/location_of_medical_care_facility/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "other_medical_office_visits/location_of_medical_care_facility/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "other_medical_office_visits/location_of_medical_care_facility/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "other_medical_office_visits/location_of_medical_care_facility/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "other_medical_office_visits/location_of_medical_care_facility/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "other_medical_office_visits/location_of_medical_care_facility/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "other_medical_office_visits/location_of_medical_care_facility/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "other_medical_office_visits/location_of_medical_care_facility/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "other_medical_office_visits/location_of_medical_care_facility/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "other_medical_office_visits/location_of_medical_care_facility/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "other_medical_office_visits/location_of_medical_care_facility/census_cbsa_micro");
	}
}

public sealed class _32D77F04277335567F6CCC5A74D707E3 : IConvertDictionary
{
	public _32D77F04277335567F6CCC5A74D707E3()
	{
	}
	public double? place_type { get; set; }
	public string specify_other_place_type { get; set; }
	public double? provider_type { get; set; }
	public string specify_other_provider_type { get; set; }
	public double? payment_source { get; set; }
	public string other_payment_source { get; set; }
	public double? pregnancy_status { get; set; }
	public double? was_this_provider_her_primary_prenatal_care_provider { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		place_type = mmria_case.GetNumberListField(p_value, "place_type", "other_medical_office_visits/medical_care_facility/place_type");
		specify_other_place_type = mmria_case.GetStringField(p_value, "specify_other_place_type", "other_medical_office_visits/medical_care_facility/specify_other_place_type");
		provider_type = mmria_case.GetNumberListField(p_value, "provider_type", "other_medical_office_visits/medical_care_facility/provider_type");
		specify_other_provider_type = mmria_case.GetStringField(p_value, "specify_other_provider_type", "other_medical_office_visits/medical_care_facility/specify_other_provider_type");
		payment_source = mmria_case.GetNumberListField(p_value, "payment_source", "other_medical_office_visits/medical_care_facility/payment_source");
		other_payment_source = mmria_case.GetStringField(p_value, "other_payment_source", "other_medical_office_visits/medical_care_facility/other_payment_source");
		pregnancy_status = mmria_case.GetNumberListField(p_value, "pregnancy_status", "other_medical_office_visits/medical_care_facility/pregnancy_status");
		was_this_provider_her_primary_prenatal_care_provider = mmria_case.GetNumberListField(p_value, "was_this_provider_her_primary_prenatal_care_provider", "other_medical_office_visits/medical_care_facility/was_this_provider_her_primary_prenatal_care_provider");
	}
}

public sealed class _432D0EB265A4EA9F9676E09A2A3DCBC3 : IConvertDictionary
{
	public _432D0EB265A4EA9F9676E09A2A3DCBC3()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? arrival_time { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public double? days_postpartum { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "other_medical_office_visits/visit/date_of_medical_office_visit/month");
		day = mmria_case.GetNumberListField(p_value, "day", "other_medical_office_visits/visit/date_of_medical_office_visit/day");
		year = mmria_case.GetNumberListField(p_value, "year", "other_medical_office_visits/visit/date_of_medical_office_visit/year");
		arrival_time = mmria_case.GetTimeField(p_value, "arrival_time", "other_medical_office_visits/visit/date_of_medical_office_visit/arrival_time");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days");
		days_postpartum = mmria_case.GetNumberField(p_value, "days_postpartum", "other_medical_office_visits/visit/date_of_medical_office_visit/days_postpartum");
	}
}

public sealed class _A5A3B6D489A0736D012BE52DE281A8BD : IConvertDictionary
{
	public _A5A3B6D489A0736D012BE52DE281A8BD()
	{
		date_of_medical_office_visit = new ();
	}
	public _432D0EB265A4EA9F9676E09A2A3DCBC3 date_of_medical_office_visit{ get;set;}
	public double? visit_type { get; set; }
	public string visit_type_other_specify { get; set; }
	public string medical_record_no { get; set; }
	public string reason_for_visit_or_chief_complaint { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_medical_office_visit = mmria_case.GetGroupField<_432D0EB265A4EA9F9676E09A2A3DCBC3>(p_value, "date_of_medical_office_visit", "other_medical_office_visits/visit/date_of_medical_office_visit");
		visit_type = mmria_case.GetNumberListField(p_value, "visit_type", "other_medical_office_visits/visit/visit_type");
		visit_type_other_specify = mmria_case.GetStringField(p_value, "visit_type_other_specify", "other_medical_office_visits/visit/visit_type_other_specify");
		medical_record_no = mmria_case.GetStringField(p_value, "medical_record_no", "other_medical_office_visits/visit/medical_record_no");
		reason_for_visit_or_chief_complaint = mmria_case.GetStringField(p_value, "reason_for_visit_or_chief_complaint", "other_medical_office_visits/visit/reason_for_visit_or_chief_complaint");
	}
}

		public sealed class _CAE881A4974F08BB4F9D46B90FEF51D4 : IConvertDictionary
		{
	public _CAE881A4974F08BB4F9D46B90FEF51D4()
{
		visit = new ();
		medical_care_facility = new ();
		location_of_medical_care_facility = new ();
		relevant_medical_history = new ();
		relevant_family_history = new ();
		relevant_social_history = new ();
		vital_signs = new ();
		laboratory_tests = new ();
		diagnostic_imaging_and_other_technology = new ();
		physical_exam = new ();
		referrals_and_consultations = new ();
		medications = new ();
		new_grid = new ();
	}
	public _A5A3B6D489A0736D012BE52DE281A8BD visit{ get;set;}
	public _32D77F04277335567F6CCC5A74D707E3 medical_care_facility{ get;set;}
	public _98BB954A3934D72B967DE0372E7B381C location_of_medical_care_facility{ get;set;}
	public List<_1345442838A1AC070037B7F85CA7E067> relevant_medical_history{ get;set;}
	public List<_C269EDFA69E501F5A86850D3AD13ADE2> relevant_family_history{ get;set;}
	public List<_B5CBB37577260A8F84CFD2C24F0461AE> relevant_social_history{ get;set;}
	public List<_8E6189F67C4CB139080D5867B1DA8617> vital_signs{ get;set;}
	public List<_06921F604DB46ECCAB769043FEC60E87> laboratory_tests{ get;set;}
	public List<_8558B3DB502E84E098AC581E499D032A> diagnostic_imaging_and_other_technology{ get;set;}
	public List<_0BC6600AA8457AB07561633DC8DBF6C3> physical_exam{ get;set;}
	public List<_C1EFC5F4AC8542D8630ED4C880B9EDD8> referrals_and_consultations{ get;set;}
	public List<_E4CA2E16DDEE982A85D4D22DCC31E70F> medications{ get;set;}
	public List<_68170408B3167DC59913CD9A2CA1357B> new_grid{ get;set;}
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		visit = mmria_case.GetGroupField<_A5A3B6D489A0736D012BE52DE281A8BD>(p_value, "visit", "other_medical_office_visits/visit");
		medical_care_facility = mmria_case.GetGroupField<_32D77F04277335567F6CCC5A74D707E3>(p_value, "medical_care_facility", "other_medical_office_visits/medical_care_facility");
		location_of_medical_care_facility = mmria_case.GetGroupField<_98BB954A3934D72B967DE0372E7B381C>(p_value, "location_of_medical_care_facility", "other_medical_office_visits/location_of_medical_care_facility");
		relevant_medical_history = mmria_case.GetGridField<_1345442838A1AC070037B7F85CA7E067>(p_value, "relevant_medical_history", "other_medical_office_visits/relevant_medical_history");
		relevant_family_history = mmria_case.GetGridField<_C269EDFA69E501F5A86850D3AD13ADE2>(p_value, "relevant_family_history", "other_medical_office_visits/relevant_family_history");
		relevant_social_history = mmria_case.GetGridField<_B5CBB37577260A8F84CFD2C24F0461AE>(p_value, "relevant_social_history", "other_medical_office_visits/relevant_social_history");
		vital_signs = mmria_case.GetGridField<_8E6189F67C4CB139080D5867B1DA8617>(p_value, "vital_signs", "other_medical_office_visits/vital_signs");
		laboratory_tests = mmria_case.GetGridField<_06921F604DB46ECCAB769043FEC60E87>(p_value, "laboratory_tests", "other_medical_office_visits/laboratory_tests");
		diagnostic_imaging_and_other_technology = mmria_case.GetGridField<_8558B3DB502E84E098AC581E499D032A>(p_value, "diagnostic_imaging_and_other_technology", "other_medical_office_visits/diagnostic_imaging_and_other_technology");
		physical_exam = mmria_case.GetGridField<_0BC6600AA8457AB07561633DC8DBF6C3>(p_value, "physical_exam", "other_medical_office_visits/physical_exam");
		referrals_and_consultations = mmria_case.GetGridField<_C1EFC5F4AC8542D8630ED4C880B9EDD8>(p_value, "referrals_and_consultations", "other_medical_office_visits/referrals_and_consultations");
		medications = mmria_case.GetGridField<_E4CA2E16DDEE982A85D4D22DCC31E70F>(p_value, "medications", "other_medical_office_visits/medications");
		new_grid = mmria_case.GetGridField<_68170408B3167DC59913CD9A2CA1357B>(p_value, "new_grid", "other_medical_office_visits/new_grid");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "other_medical_office_visits/reviewer_note");
	}
}

public sealed class _ABB6583CF8BFB479D6A720B3F297116E : IConvertDictionary
{
	public _ABB6583CF8BFB479D6A720B3F297116E()
	{
	}
	public DateOnly? date { get; set; }
	public string specialist_type { get; set; }
	public string reason { get; set; }
	public string recommendations { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "er_visit_and_hospital_medical_records/referrals_and_consultations/date");
		specialist_type = mmria_case.GetStringField(p_value, "specialist_type", "er_visit_and_hospital_medical_records/referrals_and_consultations/specialist_type");
		reason = mmria_case.GetStringField(p_value, "reason", "er_visit_and_hospital_medical_records/referrals_and_consultations/reason");
		recommendations = mmria_case.GetStringField(p_value, "recommendations", "er_visit_and_hospital_medical_records/referrals_and_consultations/recommendations");
	}
}

public sealed class _E6866482E638B68311DFC377B9C1683A : IConvertDictionary
{
	public _E6866482E638B68311DFC377B9C1683A()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string procedure { get; set; }
	public string target { get; set; }
	public string finding { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/date_and_time");
		procedure = mmria_case.GetStringField(p_value, "procedure", "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/procedure");
		target = mmria_case.GetStringField(p_value, "target", "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/target");
		finding = mmria_case.GetStringField(p_value, "finding", "er_visit_and_hospital_medical_records/diagnostic_imaging_grid/finding");
	}
}

public sealed class _A5A4696A044B1F5146F3E8EDF1D941F9 : IConvertDictionary
{
	public _A5A4696A044B1F5146F3E8EDF1D941F9()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string product { get; set; }
	public string number_of_units { get; set; }
	public string reaction_complications { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/blood_product_grid/date_and_time");
		product = mmria_case.GetStringField(p_value, "product", "er_visit_and_hospital_medical_records/blood_product_grid/product");
		number_of_units = mmria_case.GetStringField(p_value, "number_of_units", "er_visit_and_hospital_medical_records/blood_product_grid/number_of_units");
		reaction_complications = mmria_case.GetStringField(p_value, "reaction_complications", "er_visit_and_hospital_medical_records/blood_product_grid/reaction_complications");
	}
}

public sealed class _2019412E9EF12669F55BF8489F89661B : IConvertDictionary
{
	public _2019412E9EF12669F55BF8489F89661B()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string hospital_unit { get; set; }
	public string procedure { get; set; }
	public string performed_by { get; set; }
	public string outcome { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/surgical_procedures/date_and_time");
		hospital_unit = mmria_case.GetStringField(p_value, "hospital_unit", "er_visit_and_hospital_medical_records/surgical_procedures/hospital_unit");
		procedure = mmria_case.GetStringField(p_value, "procedure", "er_visit_and_hospital_medical_records/surgical_procedures/procedure");
		performed_by = mmria_case.GetStringField(p_value, "performed_by", "er_visit_and_hospital_medical_records/surgical_procedures/performed_by");
		outcome = mmria_case.GetStringField(p_value, "outcome", "er_visit_and_hospital_medical_records/surgical_procedures/outcome");
	}
}

public sealed class _8E65FBD98EC828A01C337F1DE556F4E6 : IConvertDictionary
{
	public _8E65FBD98EC828A01C337F1DE556F4E6()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string medication { get; set; }
	public string dose_frequency_duration { get; set; }
	public string adverse_reaction { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/list_of_medications/date_and_time");
		medication = mmria_case.GetStringField(p_value, "medication", "er_visit_and_hospital_medical_records/list_of_medications/medication");
		dose_frequency_duration = mmria_case.GetStringField(p_value, "dose_frequency_duration", "er_visit_and_hospital_medical_records/list_of_medications/dose_frequency_duration");
		adverse_reaction = mmria_case.GetStringField(p_value, "adverse_reaction", "er_visit_and_hospital_medical_records/list_of_medications/adverse_reaction");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "er_visit_and_hospital_medical_records/list_of_medications/comments");
	}
}

public sealed class _FEBD3534BC1A1F4B92B9CA881C05005E : IConvertDictionary
{
	public _FEBD3534BC1A1F4B92B9CA881C05005E()
	{
	}
	public DateTime? date_time { get; set; }
	public string method { get; set; }
	public string complications { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_time = mmria_case.GetDateTimeField(p_value, "date_time", "er_visit_and_hospital_medical_records/anesthesia/date_time");
		method = mmria_case.GetStringField(p_value, "method", "er_visit_and_hospital_medical_records/anesthesia/method");
		complications = mmria_case.GetStringField(p_value, "complications", "er_visit_and_hospital_medical_records/anesthesia/complications");
	}
}

public sealed class _9E3EACC3763707183F4DC8A0F2BC27D7 : IConvertDictionary
{
	public _9E3EACC3763707183F4DC8A0F2BC27D7()
	{
	}
	public double? title { get; set; }
	public string specify_other { get; set; }
	public string npi { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		title = mmria_case.GetNumberListField(p_value, "title", "er_visit_and_hospital_medical_records/birth_attendant/title");
		specify_other = mmria_case.GetStringField(p_value, "specify_other", "er_visit_and_hospital_medical_records/birth_attendant/specify_other");
		npi = mmria_case.GetStringField(p_value, "npi", "er_visit_and_hospital_medical_records/birth_attendant/npi");
	}
}

public sealed class _503A2DD428104660DFA832A84C662EEB : IConvertDictionary
{
	public _503A2DD428104660DFA832A84C662EEB()
	{
	}
	public double? systolic_bp { get; set; }
	public double? diastolic_bp { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		systolic_bp = mmria_case.GetNumberField(p_value, "systolic_bp", "er_visit_and_hospital_medical_records/highest_bp/systolic_bp");
		diastolic_bp = mmria_case.GetNumberField(p_value, "diastolic_bp", "er_visit_and_hospital_medical_records/highest_bp/diastolic_bp");
	}
}

public sealed class _8D7B5CF7224A7CD146C1B4CB60C3007D : IConvertDictionary
{
	public _8D7B5CF7224A7CD146C1B4CB60C3007D()
	{
	}
	public DateTime? date_and_time { get; set; }
	public double? temperature { get; set; }
	public double? pulse { get; set; }
	public double? respiration { get; set; }
	public double? bp_systolic { get; set; }
	public double? bp_diastolic { get; set; }
	public double? oxygen_saturation { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/vital_signs/date_and_time");
		temperature = mmria_case.GetNumberField(p_value, "temperature", "er_visit_and_hospital_medical_records/vital_signs/temperature");
		pulse = mmria_case.GetNumberField(p_value, "pulse", "er_visit_and_hospital_medical_records/vital_signs/pulse");
		respiration = mmria_case.GetNumberField(p_value, "respiration", "er_visit_and_hospital_medical_records/vital_signs/respiration");
		bp_systolic = mmria_case.GetNumberField(p_value, "bp_systolic", "er_visit_and_hospital_medical_records/vital_signs/bp_systolic");
		bp_diastolic = mmria_case.GetNumberField(p_value, "bp_diastolic", "er_visit_and_hospital_medical_records/vital_signs/bp_diastolic");
		oxygen_saturation = mmria_case.GetNumberField(p_value, "oxygen_saturation", "er_visit_and_hospital_medical_records/vital_signs/oxygen_saturation");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "er_visit_and_hospital_medical_records/vital_signs/comments");
	}
}

public sealed class _6381ED26A687768AE7B6BBD94A0BA396 : IConvertDictionary
{
	public _6381ED26A687768AE7B6BBD94A0BA396()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? time_of_rupture { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/month");
		day = mmria_case.GetNumberListField(p_value, "day", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/day");
		year = mmria_case.GetNumberListField(p_value, "year", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/year");
		time_of_rupture = mmria_case.GetTimeField(p_value, "time_of_rupture", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/time_of_rupture");
	}
}

public sealed class _EA59C8E228397D9BF03803728F7ED2A3 : IConvertDictionary
{
	public _EA59C8E228397D9BF03803728F7ED2A3()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? time_of_onset_of_labor { get; set; }
	public string duration_of_labor_prior_to_arrival { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/month");
		day = mmria_case.GetNumberListField(p_value, "day", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/day");
		year = mmria_case.GetNumberListField(p_value, "year", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/year");
		time_of_onset_of_labor = mmria_case.GetTimeField(p_value, "time_of_onset_of_labor", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/time_of_onset_of_labor");
		duration_of_labor_prior_to_arrival = mmria_case.GetStringField(p_value, "duration_of_labor_prior_to_arrival", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/duration_of_labor_prior_to_arrival");
	}
}

public sealed class _B74C0D0C7B1923DEEFA36A850701F77E : IConvertDictionary
{
	public _B74C0D0C7B1923DEEFA36A850701F77E()
	{
		date_of_onset_of_labor = new ();
		date_of_rupture = new ();
	}
	public _EA59C8E228397D9BF03803728F7ED2A3 date_of_onset_of_labor{ get;set;}
	public _6381ED26A687768AE7B6BBD94A0BA396 date_of_rupture{ get;set;}
	public double? final_delivery_route { get; set; }
	public double? onset_of_labor_was { get; set; }
	public string is_artificial { get; set; }
	public string is_spontaneous { get; set; }
	public double? multiple_gestation { get; set; }
	public double? pregnancy_outcome { get; set; }
	public string pregnancy_outcome_other_specify { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_onset_of_labor = mmria_case.GetGroupField<_EA59C8E228397D9BF03803728F7ED2A3>(p_value, "date_of_onset_of_labor", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor");
		date_of_rupture = mmria_case.GetGroupField<_6381ED26A687768AE7B6BBD94A0BA396>(p_value, "date_of_rupture", "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture");
		final_delivery_route = mmria_case.GetNumberListField(p_value, "final_delivery_route", "er_visit_and_hospital_medical_records/onset_of_labor/final_delivery_route");
		onset_of_labor_was = mmria_case.GetNumberListField(p_value, "onset_of_labor_was", "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was");
		is_artificial = mmria_case.GetHiddenField(p_value, "is_artificial", "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial");
		is_spontaneous = mmria_case.GetHiddenField(p_value, "is_spontaneous", "er_visit_and_hospital_medical_records/onset_of_labor/is_spontaneous");
		multiple_gestation = mmria_case.GetNumberListField(p_value, "multiple_gestation", "er_visit_and_hospital_medical_records/onset_of_labor/multiple_gestation");
		pregnancy_outcome = mmria_case.GetNumberListField(p_value, "pregnancy_outcome", "er_visit_and_hospital_medical_records/onset_of_labor/pregnancy_outcome");
		pregnancy_outcome_other_specify = mmria_case.GetStringField(p_value, "pregnancy_outcome_other_specify", "er_visit_and_hospital_medical_records/onset_of_labor/pregnancy_outcome_other_specify");
	}
}

public sealed class _86F90F08838CF04EE360A249116DAFAA : IConvertDictionary
{
	public _86F90F08838CF04EE360A249116DAFAA()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string specimen { get; set; }
	public string exam_type { get; set; }
	public string findings { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/pathology/date_and_time");
		specimen = mmria_case.GetStringField(p_value, "specimen", "er_visit_and_hospital_medical_records/pathology/specimen");
		exam_type = mmria_case.GetStringField(p_value, "exam_type", "er_visit_and_hospital_medical_records/pathology/exam_type");
		findings = mmria_case.GetStringField(p_value, "findings", "er_visit_and_hospital_medical_records/pathology/findings");
	}
}

public sealed class _C1688CD204DA4B73362401B17E90B9F0 : IConvertDictionary
{
	public _C1688CD204DA4B73362401B17E90B9F0()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string specimen { get; set; }
	public string test_name { get; set; }
	public string result { get; set; }
	public double? diagnostic_level { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/labratory_tests/date_and_time");
		specimen = mmria_case.GetStringField(p_value, "specimen", "er_visit_and_hospital_medical_records/labratory_tests/specimen");
		test_name = mmria_case.GetStringField(p_value, "test_name", "er_visit_and_hospital_medical_records/labratory_tests/test_name");
		result = mmria_case.GetStringField(p_value, "result", "er_visit_and_hospital_medical_records/labratory_tests/result");
		diagnostic_level = mmria_case.GetNumberListField(p_value, "diagnostic_level", "er_visit_and_hospital_medical_records/labratory_tests/diagnostic_level");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "er_visit_and_hospital_medical_records/labratory_tests/comments");
	}
}

public sealed class _0882CD00F617A7535223091DA16842BB : IConvertDictionary
{
	public _0882CD00F617A7535223091DA16842BB()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string exam_assessments { get; set; }
	public string findings { get; set; }
	public string performed_by { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/date_and_time");
		exam_assessments = mmria_case.GetStringField(p_value, "exam_assessments", "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/exam_assessments");
		findings = mmria_case.GetStringField(p_value, "findings", "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/findings");
		performed_by = mmria_case.GetStringField(p_value, "performed_by", "er_visit_and_hospital_medical_records/psychological_exam_and_assesments/performed_by");
	}
}

public sealed class _6728FFF0F95C284D75C57D93ECD96EBB : IConvertDictionary
{
	public _6728FFF0F95C284D75C57D93ECD96EBB()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string exam_evaluation { get; set; }
	public double? body_system { get; set; }
	public string findings { get; set; }
	public string performed_by { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/date_and_time");
		exam_evaluation = mmria_case.GetStringField(p_value, "exam_evaluation", "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/exam_evaluation");
		body_system = mmria_case.GetNumberListField(p_value, "body_system", "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/body_system");
		findings = mmria_case.GetStringField(p_value, "findings", "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/findings");
		performed_by = mmria_case.GetStringField(p_value, "performed_by", "er_visit_and_hospital_medical_records/physical_exam_and_evaluations/performed_by");
	}
}

public sealed class _F4EEC96036D1425284F51CC9BD1BDD14 : IConvertDictionary
{
	public _F4EEC96036D1425284F51CC9BD1BDD14()
	{
	}
	public double? feet { get; set; }
	public double? inches { get; set; }
	public double? bmi { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		feet = mmria_case.GetNumberField(p_value, "feet", "er_visit_and_hospital_medical_records/maternal_biometrics/height/feet");
		inches = mmria_case.GetNumberField(p_value, "inches", "er_visit_and_hospital_medical_records/maternal_biometrics/height/inches");
		bmi = mmria_case.GetNumberField(p_value, "bmi", "er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi");
	}
}

public sealed class _4E6929B36D562AFE2732E8D38E813499 : IConvertDictionary
{
	public _4E6929B36D562AFE2732E8D38E813499()
	{
		height = new ();
	}
	public _F4EEC96036D1425284F51CC9BD1BDD14 height{ get;set;}
	public double? admission_weight { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		height = mmria_case.GetGroupField<_F4EEC96036D1425284F51CC9BD1BDD14>(p_value, "height", "er_visit_and_hospital_medical_records/maternal_biometrics/height");
		admission_weight = mmria_case.GetNumberField(p_value, "admission_weight", "er_visit_and_hospital_medical_records/maternal_biometrics/admission_weight");
	}
}

public sealed class _9132A2F6CA994FEE04B15402054C7474 : IConvertDictionary
{
	public _9132A2F6CA994FEE04B15402054C7474()
	{
	}
	public DateTime? date_and_time { get; set; }
	public string from_unit { get; set; }
	public string to_unit { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateTimeField(p_value, "date_and_time", "er_visit_and_hospital_medical_records/internal_transfers/date_and_time");
		from_unit = mmria_case.GetStringField(p_value, "from_unit", "er_visit_and_hospital_medical_records/internal_transfers/from_unit");
		to_unit = mmria_case.GetStringField(p_value, "to_unit", "er_visit_and_hospital_medical_records/internal_transfers/to_unit");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "er_visit_and_hospital_medical_records/internal_transfers/comments");
	}
}

public sealed class _556EB19EBE4E888F2496647D61C50B5D : IConvertDictionary
{
	public _556EB19EBE4E888F2496647D61C50B5D()
	{
	}
	public double? value { get; set; }
	public double? unit { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		value = mmria_case.GetNumberField(p_value, "value", "er_visit_and_hospital_medical_records/name_and_location_facility/travel_time_to_hospital/value");
		unit = mmria_case.GetNumberListField(p_value, "unit", "er_visit_and_hospital_medical_records/name_and_location_facility/travel_time_to_hospital/unit");
	}
}

public sealed class _694C70F9FE970FCF099E9F6F1097BD3A : IConvertDictionary
{
	public _694C70F9FE970FCF099E9F6F1097BD3A()
	{
		travel_time_to_hospital = new ();
	}
	public string facility_name { get; set; }
	public double? type_of_facility { get; set; }
	public string type_of_facility_other_specify { get; set; }
	public string facility_npi_no { get; set; }
	public double? maternal_level_of_care { get; set; }
	public string other_maternal_level_of_care { get; set; }
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public double? mode_of_transportation_to_facility { get; set; }
	public string mode_of_transportation_to_facility_other { get; set; }
	public double? origin_of_travel { get; set; }
	public string origin_of_travel_other { get; set; }
	public _556EB19EBE4E888F2496647D61C50B5D travel_time_to_hospital{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		facility_name = mmria_case.GetStringField(p_value, "facility_name", "er_visit_and_hospital_medical_records/name_and_location_facility/facility_name");
		type_of_facility = mmria_case.GetNumberListField(p_value, "type_of_facility", "er_visit_and_hospital_medical_records/name_and_location_facility/type_of_facility");
		type_of_facility_other_specify = mmria_case.GetStringField(p_value, "type_of_facility_other_specify", "er_visit_and_hospital_medical_records/name_and_location_facility/type_of_facility_other_specify");
		facility_npi_no = mmria_case.GetStringField(p_value, "facility_npi_no", "er_visit_and_hospital_medical_records/name_and_location_facility/facility_npi_no");
		maternal_level_of_care = mmria_case.GetNumberListField(p_value, "maternal_level_of_care", "er_visit_and_hospital_medical_records/name_and_location_facility/maternal_level_of_care");
		other_maternal_level_of_care = mmria_case.GetStringField(p_value, "other_maternal_level_of_care", "er_visit_and_hospital_medical_records/name_and_location_facility/other_maternal_level_of_care");
		street = mmria_case.GetStringField(p_value, "street", "er_visit_and_hospital_medical_records/name_and_location_facility/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "er_visit_and_hospital_medical_records/name_and_location_facility/apartment");
		city = mmria_case.GetStringField(p_value, "city", "er_visit_and_hospital_medical_records/name_and_location_facility/city");
		state = mmria_case.GetStringListField(p_value, "state", "er_visit_and_hospital_medical_records/name_and_location_facility/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "er_visit_and_hospital_medical_records/name_and_location_facility/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "er_visit_and_hospital_medical_records/name_and_location_facility/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "er_visit_and_hospital_medical_records/name_and_location_facility/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "er_visit_and_hospital_medical_records/name_and_location_facility/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "er_visit_and_hospital_medical_records/name_and_location_facility/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "er_visit_and_hospital_medical_records/name_and_location_facility/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "er_visit_and_hospital_medical_records/name_and_location_facility/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "er_visit_and_hospital_medical_records/name_and_location_facility/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "er_visit_and_hospital_medical_records/name_and_location_facility/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "er_visit_and_hospital_medical_records/name_and_location_facility/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "er_visit_and_hospital_medical_records/name_and_location_facility/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_micro");
		mode_of_transportation_to_facility = mmria_case.GetNumberListField(p_value, "mode_of_transportation_to_facility", "er_visit_and_hospital_medical_records/name_and_location_facility/mode_of_transportation_to_facility");
		mode_of_transportation_to_facility_other = mmria_case.GetStringField(p_value, "mode_of_transportation_to_facility_other", "er_visit_and_hospital_medical_records/name_and_location_facility/mode_of_transportation_to_facility_other");
		origin_of_travel = mmria_case.GetNumberListField(p_value, "origin_of_travel", "er_visit_and_hospital_medical_records/name_and_location_facility/origin_of_travel");
		origin_of_travel_other = mmria_case.GetStringField(p_value, "origin_of_travel_other", "er_visit_and_hospital_medical_records/name_and_location_facility/origin_of_travel_other");
		travel_time_to_hospital = mmria_case.GetGroupField<_556EB19EBE4E888F2496647D61C50B5D>(p_value, "travel_time_to_hospital", "er_visit_and_hospital_medical_records/name_and_location_facility/travel_time_to_hospital");
	}
}

public sealed class _B40821148E2FC7A3B24644E46499217A : IConvertDictionary
{
	public _B40821148E2FC7A3B24644E46499217A()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? time_of_discharge { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public double? days_postpartum { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/month");
		day = mmria_case.GetNumberListField(p_value, "day", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/day");
		year = mmria_case.GetNumberListField(p_value, "year", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/year");
		time_of_discharge = mmria_case.GetTimeField(p_value, "time_of_discharge", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/time_of_discharge");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_days");
		days_postpartum = mmria_case.GetNumberField(p_value, "days_postpartum", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/days_postpartum");
	}
}

public sealed class _AC9ED4D4E4037C26C9051F03B2BAD54E : IConvertDictionary
{
	public _AC9ED4D4E4037C26C9051F03B2BAD54E()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? time_of_admission { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public double? days_postpartum { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/month");
		day = mmria_case.GetNumberListField(p_value, "day", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/day");
		year = mmria_case.GetNumberListField(p_value, "year", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/year");
		time_of_admission = mmria_case.GetTimeField(p_value, "time_of_admission", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/time_of_admission");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_days");
		days_postpartum = mmria_case.GetNumberField(p_value, "days_postpartum", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/days_postpartum");
	}
}

public sealed class _C8C8E31D7D69C21EF76FA585C4D158E3 : IConvertDictionary
{
	public _C8C8E31D7D69C21EF76FA585C4D158E3()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? time_of_arrival { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public double? days_postpartum { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/month");
		day = mmria_case.GetNumberListField(p_value, "day", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/day");
		year = mmria_case.GetNumberListField(p_value, "year", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year");
		time_of_arrival = mmria_case.GetTimeField(p_value, "time_of_arrival", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/time_of_arrival");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days");
		days_postpartum = mmria_case.GetNumberField(p_value, "days_postpartum", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/days_postpartum");
	}
}

public sealed class _52E32FFA383E16324279014838607851 : IConvertDictionary
{
	public _52E32FFA383E16324279014838607851()
	{
		date_of_arrival = new ();
		date_of_hospital_admission = new ();
		date_of_hospital_discharge = new ();
	}
	public _C8C8E31D7D69C21EF76FA585C4D158E3 date_of_arrival{ get;set;}
	public _AC9ED4D4E4037C26C9051F03B2BAD54E date_of_hospital_admission{ get;set;}
	public double? admission_condition { get; set; }
	public double? admission_status { get; set; }
	public string admission_status_other { get; set; }
	public double? admission_reason { get; set; }
	public string admission_reason_other { get; set; }
	public double? principle_source_of_payment { get; set; }
	public string principle_source_of_payment_other_specify { get; set; }
	public double? was_recieved_from_another_hospital { get; set; }
	public string from_where { get; set; }
	public double? was_transferred_to_another_hospital { get; set; }
	public string to_where { get; set; }
	public _B40821148E2FC7A3B24644E46499217A date_of_hospital_discharge{ get;set;}
	public double? discharge_pregnancy_status { get; set; }
	public double? deceased_at_discharge { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_arrival = mmria_case.GetGroupField<_C8C8E31D7D69C21EF76FA585C4D158E3>(p_value, "date_of_arrival", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival");
		date_of_hospital_admission = mmria_case.GetGroupField<_AC9ED4D4E4037C26C9051F03B2BAD54E>(p_value, "date_of_hospital_admission", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission");
		admission_condition = mmria_case.GetNumberListField(p_value, "admission_condition", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_condition");
		admission_status = mmria_case.GetNumberListField(p_value, "admission_status", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_status");
		admission_status_other = mmria_case.GetStringField(p_value, "admission_status_other", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_status_other");
		admission_reason = mmria_case.GetNumberListField(p_value, "admission_reason", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_reason");
		admission_reason_other = mmria_case.GetStringField(p_value, "admission_reason_other", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_reason_other");
		principle_source_of_payment = mmria_case.GetNumberListField(p_value, "principle_source_of_payment", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/principle_source_of_payment");
		principle_source_of_payment_other_specify = mmria_case.GetStringField(p_value, "principle_source_of_payment_other_specify", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/principle_source_of_payment_other_specify");
		was_recieved_from_another_hospital = mmria_case.GetNumberListField(p_value, "was_recieved_from_another_hospital", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/was_recieved_from_another_hospital");
		from_where = mmria_case.GetStringField(p_value, "from_where", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/from_where");
		was_transferred_to_another_hospital = mmria_case.GetNumberListField(p_value, "was_transferred_to_another_hospital", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/was_transferred_to_another_hospital");
		to_where = mmria_case.GetStringField(p_value, "to_where", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/to_where");
		date_of_hospital_discharge = mmria_case.GetGroupField<_B40821148E2FC7A3B24644E46499217A>(p_value, "date_of_hospital_discharge", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge");
		discharge_pregnancy_status = mmria_case.GetNumberListField(p_value, "discharge_pregnancy_status", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/discharge_pregnancy_status");
		deceased_at_discharge = mmria_case.GetNumberListField(p_value, "deceased_at_discharge", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/deceased_at_discharge");
	}
}

public sealed class _022BC412269BEF5AB3D35F5F824B83E5 : IConvertDictionary
{
	public _022BC412269BEF5AB3D35F5F824B83E5()
	{
	}
	public string medical_record_no { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		medical_record_no = mmria_case.GetStringField(p_value, "medical_record_no", "er_visit_and_hospital_medical_records/maternal_record_identification/medical_record_no");
	}
}

		public sealed class _0CE40C4018C47CA22AC1A0003DC34FB7 : IConvertDictionary
		{
	public _0CE40C4018C47CA22AC1A0003DC34FB7()
{
		maternal_record_identification = new ();
		basic_admission_and_discharge_information = new ();
		name_and_location_facility = new ();
		internal_transfers = new ();
		maternal_biometrics = new ();
		physical_exam_and_evaluations = new ();
		psychological_exam_and_assesments = new ();
		labratory_tests = new ();
		pathology = new ();
		onset_of_labor = new ();
		vital_signs = new ();
		highest_bp = new ();
		birth_attendant = new ();
		anesthesia = new ();
		list_of_medications = new ();
		surgical_procedures = new ();
		blood_product_grid = new ();
		diagnostic_imaging_grid = new ();
		referrals_and_consultations = new ();
	}
	public _022BC412269BEF5AB3D35F5F824B83E5 maternal_record_identification{ get;set;}
	public _52E32FFA383E16324279014838607851 basic_admission_and_discharge_information{ get;set;}
	public _694C70F9FE970FCF099E9F6F1097BD3A name_and_location_facility{ get;set;}
	public List<_9132A2F6CA994FEE04B15402054C7474> internal_transfers{ get;set;}
	public _4E6929B36D562AFE2732E8D38E813499 maternal_biometrics{ get;set;}
	public List<_6728FFF0F95C284D75C57D93ECD96EBB> physical_exam_and_evaluations{ get;set;}
	public List<_0882CD00F617A7535223091DA16842BB> psychological_exam_and_assesments{ get;set;}
	public List<_C1688CD204DA4B73362401B17E90B9F0> labratory_tests{ get;set;}
	public List<_86F90F08838CF04EE360A249116DAFAA> pathology{ get;set;}
	public _B74C0D0C7B1923DEEFA36A850701F77E onset_of_labor{ get;set;}
	public List<_8D7B5CF7224A7CD146C1B4CB60C3007D> vital_signs{ get;set;}
	public _503A2DD428104660DFA832A84C662EEB highest_bp{ get;set;}
	public List<_9E3EACC3763707183F4DC8A0F2BC27D7> birth_attendant{ get;set;}
	public double? were_there_complications_of_anesthesia { get; set; }
	public List<_FEBD3534BC1A1F4B92B9CA881C05005E> anesthesia{ get;set;}
	public double? any_adverse_reactions { get; set; }
	public List<_8E65FBD98EC828A01C337F1DE556F4E6> list_of_medications{ get;set;}
	public double? any_surgical_procedures { get; set; }
	public List<_2019412E9EF12669F55BF8489F89661B> surgical_procedures{ get;set;}
	public double? any_blood_transfusions { get; set; }
	public string patient_blood_type { get; set; }
	public List<_A5A4696A044B1F5146F3E8EDF1D941F9> blood_product_grid{ get;set;}
	public List<_E6866482E638B68311DFC377B9C1683A> diagnostic_imaging_grid{ get;set;}
	public List<_ABB6583CF8BFB479D6A720B3F297116E> referrals_and_consultations{ get;set;}
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		maternal_record_identification = mmria_case.GetGroupField<_022BC412269BEF5AB3D35F5F824B83E5>(p_value, "maternal_record_identification", "er_visit_and_hospital_medical_records/maternal_record_identification");
		basic_admission_and_discharge_information = mmria_case.GetGroupField<_52E32FFA383E16324279014838607851>(p_value, "basic_admission_and_discharge_information", "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information");
		name_and_location_facility = mmria_case.GetGroupField<_694C70F9FE970FCF099E9F6F1097BD3A>(p_value, "name_and_location_facility", "er_visit_and_hospital_medical_records/name_and_location_facility");
		internal_transfers = mmria_case.GetGridField<_9132A2F6CA994FEE04B15402054C7474>(p_value, "internal_transfers", "er_visit_and_hospital_medical_records/internal_transfers");
		maternal_biometrics = mmria_case.GetGroupField<_4E6929B36D562AFE2732E8D38E813499>(p_value, "maternal_biometrics", "er_visit_and_hospital_medical_records/maternal_biometrics");
		physical_exam_and_evaluations = mmria_case.GetGridField<_6728FFF0F95C284D75C57D93ECD96EBB>(p_value, "physical_exam_and_evaluations", "er_visit_and_hospital_medical_records/physical_exam_and_evaluations");
		psychological_exam_and_assesments = mmria_case.GetGridField<_0882CD00F617A7535223091DA16842BB>(p_value, "psychological_exam_and_assesments", "er_visit_and_hospital_medical_records/psychological_exam_and_assesments");
		labratory_tests = mmria_case.GetGridField<_C1688CD204DA4B73362401B17E90B9F0>(p_value, "labratory_tests", "er_visit_and_hospital_medical_records/labratory_tests");
		pathology = mmria_case.GetGridField<_86F90F08838CF04EE360A249116DAFAA>(p_value, "pathology", "er_visit_and_hospital_medical_records/pathology");
		onset_of_labor = mmria_case.GetGroupField<_B74C0D0C7B1923DEEFA36A850701F77E>(p_value, "onset_of_labor", "er_visit_and_hospital_medical_records/onset_of_labor");
		vital_signs = mmria_case.GetGridField<_8D7B5CF7224A7CD146C1B4CB60C3007D>(p_value, "vital_signs", "er_visit_and_hospital_medical_records/vital_signs");
		highest_bp = mmria_case.GetGroupField<_503A2DD428104660DFA832A84C662EEB>(p_value, "highest_bp", "er_visit_and_hospital_medical_records/highest_bp");
		birth_attendant = mmria_case.GetGridField<_9E3EACC3763707183F4DC8A0F2BC27D7>(p_value, "birth_attendant", "er_visit_and_hospital_medical_records/birth_attendant");
		were_there_complications_of_anesthesia = mmria_case.GetNumberListField(p_value, "were_there_complications_of_anesthesia", "er_visit_and_hospital_medical_records/were_there_complications_of_anesthesia");
		anesthesia = mmria_case.GetGridField<_FEBD3534BC1A1F4B92B9CA881C05005E>(p_value, "anesthesia", "er_visit_and_hospital_medical_records/anesthesia");
		any_adverse_reactions = mmria_case.GetNumberListField(p_value, "any_adverse_reactions", "er_visit_and_hospital_medical_records/any_adverse_reactions");
		list_of_medications = mmria_case.GetGridField<_8E65FBD98EC828A01C337F1DE556F4E6>(p_value, "list_of_medications", "er_visit_and_hospital_medical_records/list_of_medications");
		any_surgical_procedures = mmria_case.GetNumberListField(p_value, "any_surgical_procedures", "er_visit_and_hospital_medical_records/any_surgical_procedures");
		surgical_procedures = mmria_case.GetGridField<_2019412E9EF12669F55BF8489F89661B>(p_value, "surgical_procedures", "er_visit_and_hospital_medical_records/surgical_procedures");
		any_blood_transfusions = mmria_case.GetNumberListField(p_value, "any_blood_transfusions", "er_visit_and_hospital_medical_records/any_blood_transfusions");
		patient_blood_type = mmria_case.GetStringField(p_value, "patient_blood_type", "er_visit_and_hospital_medical_records/patient_blood_type");
		blood_product_grid = mmria_case.GetGridField<_A5A4696A044B1F5146F3E8EDF1D941F9>(p_value, "blood_product_grid", "er_visit_and_hospital_medical_records/blood_product_grid");
		diagnostic_imaging_grid = mmria_case.GetGridField<_E6866482E638B68311DFC377B9C1683A>(p_value, "diagnostic_imaging_grid", "er_visit_and_hospital_medical_records/diagnostic_imaging_grid");
		referrals_and_consultations = mmria_case.GetGridField<_ABB6583CF8BFB479D6A720B3F297116E>(p_value, "referrals_and_consultations", "er_visit_and_hospital_medical_records/referrals_and_consultations");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "er_visit_and_hospital_medical_records/reviewer_note");
	}
}

public sealed class _E564335670B1446923BCA2B6D0E5147F : IConvertDictionary
{
	public _E564335670B1446923BCA2B6D0E5147F()
	{
	}
	public double? place { get; set; }
	public double? provider_type { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public DateOnly? begin_date { get; set; }
	public DateOnly? end_date { get; set; }
	public string pregrid_comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		place = mmria_case.GetNumberListField(p_value, "place", "prenatal/other_sources_of_prenatal_care/place");
		provider_type = mmria_case.GetNumberListField(p_value, "provider_type", "prenatal/other_sources_of_prenatal_care/provider_type");
		city = mmria_case.GetStringField(p_value, "city", "prenatal/other_sources_of_prenatal_care/city");
		state = mmria_case.GetStringListField(p_value, "state", "prenatal/other_sources_of_prenatal_care/state");
		begin_date = mmria_case.GetDateField(p_value, "begin_date", "prenatal/other_sources_of_prenatal_care/begin_date");
		end_date = mmria_case.GetDateField(p_value, "end_date", "prenatal/other_sources_of_prenatal_care/end_date");
		pregrid_comments = mmria_case.GetTextAreaField(p_value, "pregrid_comments", "prenatal/other_sources_of_prenatal_care/pregrid_comments");
	}
}

public sealed class _09BC93A66151FE9672FFEB3841E02A0D : IConvertDictionary
{
	public _09BC93A66151FE9672FFEB3841E02A0D()
	{
	}
	public DateOnly? date { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public string type_of_specialist { get; set; }
	public string reason { get; set; }
	public double? was_appointment_kept { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "prenatal/medical_referrals/date");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/medical_referrals/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/medical_referrals/gestational_age_days");
		type_of_specialist = mmria_case.GetStringField(p_value, "type_of_specialist", "prenatal/medical_referrals/type_of_specialist");
		reason = mmria_case.GetStringField(p_value, "reason", "prenatal/medical_referrals/reason");
		was_appointment_kept = mmria_case.GetNumberListField(p_value, "was_appointment_kept", "prenatal/medical_referrals/was_appointment_kept");
	}
}

public sealed class _D2DAC659661D88CEB6D93A2091F71CAD : IConvertDictionary
{
	public _D2DAC659661D88CEB6D93A2091F71CAD()
	{
	}
	public DateOnly? date { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public string facility { get; set; }
	public string duration { get; set; }
	public string reason { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "prenatal/pre_delivery_hospitalizations_details/date");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/pre_delivery_hospitalizations_details/gestational_age_days");
		facility = mmria_case.GetStringField(p_value, "facility", "prenatal/pre_delivery_hospitalizations_details/facility");
		duration = mmria_case.GetStringField(p_value, "duration", "prenatal/pre_delivery_hospitalizations_details/duration");
		reason = mmria_case.GetStringField(p_value, "reason", "prenatal/pre_delivery_hospitalizations_details/reason");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/pre_delivery_hospitalizations_details/comments");
	}
}

public sealed class _011C3E72A57DE3AB66351169D6F6D5EA : IConvertDictionary
{
	public _011C3E72A57DE3AB66351169D6F6D5EA()
	{
	}
	public DateOnly? date { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public string medication { get; set; }
	public string dose_frequency_duration { get; set; }
	public string reason { get; set; }
	public double? is_adverse_reaction { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "prenatal/medications_and_drugs_during_pregnancy/date");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/medications_and_drugs_during_pregnancy/gestational_age_days");
		medication = mmria_case.GetStringField(p_value, "medication", "prenatal/medications_and_drugs_during_pregnancy/medication");
		dose_frequency_duration = mmria_case.GetStringField(p_value, "dose_frequency_duration", "prenatal/medications_and_drugs_during_pregnancy/dose_frequency_duration");
		reason = mmria_case.GetStringField(p_value, "reason", "prenatal/medications_and_drugs_during_pregnancy/reason");
		is_adverse_reaction = mmria_case.GetNumberListField(p_value, "is_adverse_reaction", "prenatal/medications_and_drugs_during_pregnancy/is_adverse_reaction");
	}
}

public sealed class _5180AC492C8C104F8787060407249334 : IConvertDictionary
{
	public _5180AC492C8C104F8787060407249334()
	{
	}
	public DateOnly? date_1st_noted { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public string problem { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_1st_noted = mmria_case.GetDateField(p_value, "date_1st_noted", "prenatal/problems_identified_grid/date_1st_noted");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/problems_identified_grid/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/problems_identified_grid/gestational_age_days");
		problem = mmria_case.GetStringField(p_value, "problem", "prenatal/problems_identified_grid/problem");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/problems_identified_grid/comments");
	}
}

public sealed class _84561DBF6AEC5892A65DF8E2D85470D7 : IConvertDictionary
{
	public _84561DBF6AEC5892A65DF8E2D85470D7()
	{
	}
	public DateOnly? date { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public string procedure { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "prenatal/diagnostic_procedures/date");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/diagnostic_procedures/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/diagnostic_procedures/gestational_age_days");
		procedure = mmria_case.GetStringField(p_value, "procedure", "prenatal/diagnostic_procedures/procedure");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/diagnostic_procedures/comments");
	}
}

public sealed class _524E042339910523A01145C2724B9350 : IConvertDictionary
{
	public _524E042339910523A01145C2724B9350()
	{
	}
	public DateOnly? date_and_time { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public string test_or_procedure { get; set; }
	public string results { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateField(p_value, "date_and_time", "prenatal/other_lab_tests/date_and_time");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/other_lab_tests/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/other_lab_tests/gestational_age_days");
		test_or_procedure = mmria_case.GetStringField(p_value, "test_or_procedure", "prenatal/other_lab_tests/test_or_procedure");
		results = mmria_case.GetStringField(p_value, "results", "prenatal/other_lab_tests/results");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/other_lab_tests/comments");
	}
}

public sealed class _7D05294A3D621B59A07C395DD05AC6ED : IConvertDictionary
{
	public _7D05294A3D621B59A07C395DD05AC6ED()
	{
	}
	public double? systolic { get; set; }
	public double? diastolic { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		systolic = mmria_case.GetNumberField(p_value, "systolic", "prenatal/highest_blood_pressure/systolic");
		diastolic = mmria_case.GetNumberField(p_value, "diastolic", "prenatal/highest_blood_pressure/diastolic");
	}
}

public sealed class _B2FC9E3D5D8BAE58DD3E9BB9CF1B46D5 : IConvertDictionary
{
	public _B2FC9E3D5D8BAE58DD3E9BB9CF1B46D5()
	{
	}
	public DateOnly? date_and_time { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public double? systolic_bp { get; set; }
	public double? diastolic { get; set; }
	public double? heart_rate { get; set; }
	public double? oxygen_saturation { get; set; }
	public double? urine_protein { get; set; }
	public double? urine_ketones { get; set; }
	public double? urine_glucose { get; set; }
	public double? blood_hematocrit { get; set; }
	public double? weight { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_and_time = mmria_case.GetDateField(p_value, "date_and_time", "prenatal/routine_monitoring/date_and_time");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/routine_monitoring/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/routine_monitoring/gestational_age_days");
		systolic_bp = mmria_case.GetNumberField(p_value, "systolic_bp", "prenatal/routine_monitoring/systolic_bp");
		diastolic = mmria_case.GetNumberField(p_value, "diastolic", "prenatal/routine_monitoring/diastolic");
		heart_rate = mmria_case.GetNumberField(p_value, "heart_rate", "prenatal/routine_monitoring/heart_rate");
		oxygen_saturation = mmria_case.GetNumberField(p_value, "oxygen_saturation", "prenatal/routine_monitoring/oxygen_saturation");
		urine_protein = mmria_case.GetNumberListField(p_value, "urine_protein", "prenatal/routine_monitoring/urine_protein");
		urine_ketones = mmria_case.GetNumberListField(p_value, "urine_ketones", "prenatal/routine_monitoring/urine_ketones");
		urine_glucose = mmria_case.GetNumberListField(p_value, "urine_glucose", "prenatal/routine_monitoring/urine_glucose");
		blood_hematocrit = mmria_case.GetNumberField(p_value, "blood_hematocrit", "prenatal/routine_monitoring/blood_hematocrit");
		weight = mmria_case.GetNumberField(p_value, "weight", "prenatal/routine_monitoring/weight");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "prenatal/routine_monitoring/comments");
	}
}

public sealed class _DBD80EB2135F84E1BD3FE6A7640B4C65 : IConvertDictionary
{
	public _DBD80EB2135F84E1BD3FE6A7640B4C65()
	{
	}
	public double? feet { get; set; }
	public double? inches { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		feet = mmria_case.GetNumberField(p_value, "feet", "prenatal/current_pregnancy/height/feet");
		inches = mmria_case.GetNumberField(p_value, "inches", "prenatal/current_pregnancy/height/inches");
	}
}

public sealed class _863F482DC827D58387CF8EA02878C4A9 : IConvertDictionary
{
	public _863F482DC827D58387CF8EA02878C4A9()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public double? gestational_age_at_last_prenatal_visit { get; set; }
	public double? gestational_age_at_last_prenatal_visit_days { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "prenatal/current_pregnancy/date_of_last_prenatal_visit/month");
		day = mmria_case.GetNumberListField(p_value, "day", "prenatal/current_pregnancy/date_of_last_prenatal_visit/day");
		year = mmria_case.GetNumberListField(p_value, "year", "prenatal/current_pregnancy/date_of_last_prenatal_visit/year");
		gestational_age_at_last_prenatal_visit = mmria_case.GetNumberField(p_value, "gestational_age_at_last_prenatal_visit", "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit");
		gestational_age_at_last_prenatal_visit_days = mmria_case.GetNumberField(p_value, "gestational_age_at_last_prenatal_visit_days", "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days");
	}
}

public sealed class _E4A6FED46D116B4778FC2131B5A5D739 : IConvertDictionary
{
	public _E4A6FED46D116B4778FC2131B5A5D739()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public double? gestational_age_at_first_ultrasound { get; set; }
	public double? gestational_age_at_first_ultrasound_days { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "prenatal/current_pregnancy/date_of_1st_ultrasound/month");
		day = mmria_case.GetNumberListField(p_value, "day", "prenatal/current_pregnancy/date_of_1st_ultrasound/day");
		year = mmria_case.GetNumberListField(p_value, "year", "prenatal/current_pregnancy/date_of_1st_ultrasound/year");
		gestational_age_at_first_ultrasound = mmria_case.GetNumberField(p_value, "gestational_age_at_first_ultrasound", "prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound");
		gestational_age_at_first_ultrasound_days = mmria_case.GetNumberField(p_value, "gestational_age_at_first_ultrasound_days", "prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound_days");
	}
}

public sealed class _DB29CBF2CD768EBBCD0FB7C970B2E585 : IConvertDictionary
{
	public _DB29CBF2CD768EBBCD0FB7C970B2E585()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public double? gestational_age_weeks { get; set; }
	public double? gestational_age_days { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/month");
		day = mmria_case.GetNumberListField(p_value, "day", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/day");
		year = mmria_case.GetNumberListField(p_value, "year", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/year");
		gestational_age_weeks = mmria_case.GetNumberField(p_value, "gestational_age_weeks", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks");
		gestational_age_days = mmria_case.GetNumberField(p_value, "gestational_age_days", "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days");
	}
}

public sealed class _BC6376F2085FB073004A29BE96A7676E : IConvertDictionary
{
	public _BC6376F2085FB073004A29BE96A7676E()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public double? estimate_based_on { get; set; }
	public string estimate_based_on_ultrasound { get; set; }
	public string estimate_based_on_lmp { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "prenatal/current_pregnancy/estimated_date_of_confinement/month");
		day = mmria_case.GetNumberListField(p_value, "day", "prenatal/current_pregnancy/estimated_date_of_confinement/day");
		year = mmria_case.GetNumberListField(p_value, "year", "prenatal/current_pregnancy/estimated_date_of_confinement/year");
		estimate_based_on = mmria_case.GetNumberListField(p_value, "estimate_based_on", "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on");
		estimate_based_on_ultrasound = mmria_case.GetHiddenField(p_value, "estimate_based_on_ultrasound", "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on_ultrasound");
		estimate_based_on_lmp = mmria_case.GetHiddenField(p_value, "estimate_based_on_lmp", "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on_lmp");
	}
}

public sealed class _EF34E70882D335F28D703380EF23BA00 : IConvertDictionary
{
	public _EF34E70882D335F28D703380EF23BA00()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "prenatal/current_pregnancy/date_of_last_normal_menses/month");
		day = mmria_case.GetNumberListField(p_value, "day", "prenatal/current_pregnancy/date_of_last_normal_menses/day");
		year = mmria_case.GetNumberListField(p_value, "year", "prenatal/current_pregnancy/date_of_last_normal_menses/year");
	}
}

public sealed class _17F17143D948AABBCF8F029D68346E4B : IConvertDictionary
{
	public _17F17143D948AABBCF8F029D68346E4B()
	{
		date_of_last_normal_menses = new ();
		estimated_date_of_confinement = new ();
		date_of_1st_prenatal_visit = new ();
		date_of_1st_ultrasound = new ();
		date_of_last_prenatal_visit = new ();
		height = new ();
	}
	public _EF34E70882D335F28D703380EF23BA00 date_of_last_normal_menses{ get;set;}
	public _BC6376F2085FB073004A29BE96A7676E estimated_date_of_confinement{ get;set;}
	public _DB29CBF2CD768EBBCD0FB7C970B2E585 date_of_1st_prenatal_visit{ get;set;}
	public _E4A6FED46D116B4778FC2131B5A5D739 date_of_1st_ultrasound{ get;set;}
	public _863F482DC827D58387CF8EA02878C4A9 date_of_last_prenatal_visit{ get;set;}
	public _DBD80EB2135F84E1BD3FE6A7640B4C65 height{ get;set;}
	public double? pre_pregnancy_weight { get; set; }
	public double? bmi { get; set; }
	public double? weight_at_1st_visit { get; set; }
	public double? weight_at_last_visit { get; set; }
	public double? weight_gain { get; set; }
	public double? total_number_of_visits { get; set; }
	public double? trimester_of_first_pnc_visit { get; set; }
	public double? number_of_fetuses { get; set; }
	public double? was_home_delivery_planned { get; set; }
	public double? attended_prenatal_visits_alone { get; set; }
	public string intended_birthing_facility { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_last_normal_menses = mmria_case.GetGroupField<_EF34E70882D335F28D703380EF23BA00>(p_value, "date_of_last_normal_menses", "prenatal/current_pregnancy/date_of_last_normal_menses");
		estimated_date_of_confinement = mmria_case.GetGroupField<_BC6376F2085FB073004A29BE96A7676E>(p_value, "estimated_date_of_confinement", "prenatal/current_pregnancy/estimated_date_of_confinement");
		date_of_1st_prenatal_visit = mmria_case.GetGroupField<_DB29CBF2CD768EBBCD0FB7C970B2E585>(p_value, "date_of_1st_prenatal_visit", "prenatal/current_pregnancy/date_of_1st_prenatal_visit");
		date_of_1st_ultrasound = mmria_case.GetGroupField<_E4A6FED46D116B4778FC2131B5A5D739>(p_value, "date_of_1st_ultrasound", "prenatal/current_pregnancy/date_of_1st_ultrasound");
		date_of_last_prenatal_visit = mmria_case.GetGroupField<_863F482DC827D58387CF8EA02878C4A9>(p_value, "date_of_last_prenatal_visit", "prenatal/current_pregnancy/date_of_last_prenatal_visit");
		height = mmria_case.GetGroupField<_DBD80EB2135F84E1BD3FE6A7640B4C65>(p_value, "height", "prenatal/current_pregnancy/height");
		pre_pregnancy_weight = mmria_case.GetNumberField(p_value, "pre_pregnancy_weight", "prenatal/current_pregnancy/pre_pregnancy_weight");
		bmi = mmria_case.GetNumberField(p_value, "bmi", "prenatal/current_pregnancy/bmi");
		weight_at_1st_visit = mmria_case.GetNumberField(p_value, "weight_at_1st_visit", "prenatal/current_pregnancy/weight_at_1st_visit");
		weight_at_last_visit = mmria_case.GetNumberField(p_value, "weight_at_last_visit", "prenatal/current_pregnancy/weight_at_last_visit");
		weight_gain = mmria_case.GetNumberField(p_value, "weight_gain", "prenatal/current_pregnancy/weight_gain");
		total_number_of_visits = mmria_case.GetNumberField(p_value, "total_number_of_visits", "prenatal/current_pregnancy/total_number_of_visits");
		trimester_of_first_pnc_visit = mmria_case.GetNumberListField(p_value, "trimester_of_first_pnc_visit", "prenatal/current_pregnancy/trimester_of_first_pnc_visit");
		number_of_fetuses = mmria_case.GetNumberField(p_value, "number_of_fetuses", "prenatal/current_pregnancy/number_of_fetuses");
		was_home_delivery_planned = mmria_case.GetNumberListField(p_value, "was_home_delivery_planned", "prenatal/current_pregnancy/was_home_delivery_planned");
		attended_prenatal_visits_alone = mmria_case.GetNumberListField(p_value, "attended_prenatal_visits_alone", "prenatal/current_pregnancy/attended_prenatal_visits_alone");
		intended_birthing_facility = mmria_case.GetStringField(p_value, "intended_birthing_facility", "prenatal/current_pregnancy/intended_birthing_facility");
	}
}

public sealed class _E08949C3EA32F7E6C01990BF3094115F : IConvertDictionary
{
	public _E08949C3EA32F7E6C01990BF3094115F()
	{
	}
	public double? was_pregnancy_result_of_infertility_treatment { get; set; }
	public double? fertility_enhanding_drugs { get; set; }
	public double? assisted_reproductive_technology { get; set; }
	public double? art_type { get; set; }
	public string specify_other_art_type { get; set; }
	public double? cycle_number { get; set; }
	public double? embryos_transferred { get; set; }
	public double? embryos_growing { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		was_pregnancy_result_of_infertility_treatment = mmria_case.GetNumberListField(p_value, "was_pregnancy_result_of_infertility_treatment", "prenatal/infertility_treatment/was_pregnancy_result_of_infertility_treatment");
		fertility_enhanding_drugs = mmria_case.GetNumberListField(p_value, "fertility_enhanding_drugs", "prenatal/infertility_treatment/fertility_enhanding_drugs");
		assisted_reproductive_technology = mmria_case.GetNumberListField(p_value, "assisted_reproductive_technology", "prenatal/infertility_treatment/assisted_reproductive_technology");
		art_type = mmria_case.GetNumberListField(p_value, "art_type", "prenatal/infertility_treatment/art_type");
		specify_other_art_type = mmria_case.GetStringField(p_value, "specify_other_art_type", "prenatal/infertility_treatment/specify_other_art_type");
		cycle_number = mmria_case.GetNumberField(p_value, "cycle_number", "prenatal/infertility_treatment/cycle_number");
		embryos_transferred = mmria_case.GetNumberField(p_value, "embryos_transferred", "prenatal/infertility_treatment/embryos_transferred");
		embryos_growing = mmria_case.GetNumberField(p_value, "embryos_growing", "prenatal/infertility_treatment/embryos_growing");
	}
}

public sealed class _5AE0C5476B5EBCB6169B575B3C9A7054 : IConvertDictionary
{
	public _5AE0C5476B5EBCB6169B575B3C9A7054()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "prenatal/intendedenes/date_birth_control_was_discontinued/month");
		day = mmria_case.GetNumberListField(p_value, "day", "prenatal/intendedenes/date_birth_control_was_discontinued/day");
		year = mmria_case.GetNumberListField(p_value, "year", "prenatal/intendedenes/date_birth_control_was_discontinued/year");
	}
}

public sealed class _3B50369398258E8F818A54026A4083D0 : IConvertDictionary
{
	public _3B50369398258E8F818A54026A4083D0()
	{
		date_birth_control_was_discontinued = new ();
	}
	public _5AE0C5476B5EBCB6169B575B3C9A7054 date_birth_control_was_discontinued{ get;set;}
	public double? was_pregnancy_planned { get; set; }
	public string pi_wp_plann_sp { get; set; }
	public double? was_patient_using_birth_control { get; set; }
	public string was_patient_using_birth_control_other_specify { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_birth_control_was_discontinued = mmria_case.GetGroupField<_5AE0C5476B5EBCB6169B575B3C9A7054>(p_value, "date_birth_control_was_discontinued", "prenatal/intendedenes/date_birth_control_was_discontinued");
		was_pregnancy_planned = mmria_case.GetNumberListField(p_value, "was_pregnancy_planned", "prenatal/intendedenes/was_pregnancy_planned");
		pi_wp_plann_sp = mmria_case.GetStringField(p_value, "pi_wp_plann_sp", "prenatal/intendedenes/pi_wp_plann_sp");
		was_patient_using_birth_control = mmria_case.GetNumberListField(p_value, "was_patient_using_birth_control", "prenatal/intendedenes/was_patient_using_birth_control");
		was_patient_using_birth_control_other_specify = mmria_case.GetStringField(p_value, "was_patient_using_birth_control_other_specify", "prenatal/intendedenes/was_patient_using_birth_control_other_specify");
	}
}

public sealed class _806E4DA069E6081A570C043C83F92FB9 : IConvertDictionary
{
	public _806E4DA069E6081A570C043C83F92FB9()
	{
	}
	public DateOnly? date_ended { get; set; }
	public double? outcome { get; set; }
	public double? gestational_age { get; set; }
	public double? birth_weight_uom { get; set; }
	public double? birth_weight { get; set; }
	public double? birth_weight_oz { get; set; }
	public string method_of_delivery { get; set; }
	public string complications { get; set; }
	public double? is_now_living { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_ended = mmria_case.GetDateField(p_value, "date_ended", "prenatal/pregnancy_history/details_grid/date_ended");
		outcome = mmria_case.GetNumberListField(p_value, "outcome", "prenatal/pregnancy_history/details_grid/outcome");
		gestational_age = mmria_case.GetNumberField(p_value, "gestational_age", "prenatal/pregnancy_history/details_grid/gestational_age");
		birth_weight_uom = mmria_case.GetNumberListField(p_value, "birth_weight_uom", "prenatal/pregnancy_history/details_grid/birth_weight_uom");
		birth_weight = mmria_case.GetNumberField(p_value, "birth_weight", "prenatal/pregnancy_history/details_grid/birth_weight");
		birth_weight_oz = mmria_case.GetNumberField(p_value, "birth_weight_oz", "prenatal/pregnancy_history/details_grid/birth_weight_oz");
		method_of_delivery = mmria_case.GetStringField(p_value, "method_of_delivery", "prenatal/pregnancy_history/details_grid/method_of_delivery");
		complications = mmria_case.GetStringField(p_value, "complications", "prenatal/pregnancy_history/details_grid/complications");
		is_now_living = mmria_case.GetNumberListField(p_value, "is_now_living", "prenatal/pregnancy_history/details_grid/is_now_living");
	}
}

public sealed class _CB4A0311CE874081BDBCB946D4CE3D0E : IConvertDictionary
{
	public _CB4A0311CE874081BDBCB946D4CE3D0E()
	{
		details_grid = new ();
	}
	public double? gravida { get; set; }
	public double? para { get; set; }
	public double? abortions { get; set; }
	public List<_806E4DA069E6081A570C043C83F92FB9> details_grid{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		gravida = mmria_case.GetNumberField(p_value, "gravida", "prenatal/pregnancy_history/gravida");
		para = mmria_case.GetNumberField(p_value, "para", "prenatal/pregnancy_history/para");
		abortions = mmria_case.GetNumberField(p_value, "abortions", "prenatal/pregnancy_history/abortions");
		details_grid = mmria_case.GetGridField<_806E4DA069E6081A570C043C83F92FB9>(p_value, "details_grid", "prenatal/pregnancy_history/details_grid");
	}
}

public sealed class _DB12CCDCFE9F36C81A924DA8DAA1F817 : IConvertDictionary
{
	public _DB12CCDCFE9F36C81A924DA8DAA1F817()
	{
	}
	public string substance { get; set; }
	public string substance_other { get; set; }
	public double? screening { get; set; }
	public double? couseling_education { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		substance = mmria_case.GetStringListField(p_value, "substance", "prenatal/substance_use_grid/substance");
		substance_other = mmria_case.GetStringField(p_value, "substance_other", "prenatal/substance_use_grid/substance_other");
		screening = mmria_case.GetNumberListField(p_value, "screening", "prenatal/substance_use_grid/screening");
		couseling_education = mmria_case.GetNumberListField(p_value, "couseling_education", "prenatal/substance_use_grid/couseling_education");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/substance_use_grid/comments");
	}
}

public sealed class _A42F5E1D0CC492390B78D665C2162B38 : IConvertDictionary
{
	public _A42F5E1D0CC492390B78D665C2162B38()
	{
	}
	public double? relation { get; set; }
	public string condition { get; set; }
	public double? is_living { get; set; }
	public double? age_at_death { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		relation = mmria_case.GetNumberListField(p_value, "relation", "prenatal/family_medical_history/relation");
		condition = mmria_case.GetStringField(p_value, "condition", "prenatal/family_medical_history/condition");
		is_living = mmria_case.GetNumberListField(p_value, "is_living", "prenatal/family_medical_history/is_living");
		age_at_death = mmria_case.GetNumberField(p_value, "age_at_death", "prenatal/family_medical_history/age_at_death");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/family_medical_history/comments");
	}
}

public sealed class _99F6F090455B774931675928588796B4 : IConvertDictionary
{
	public _99F6F090455B774931675928588796B4()
	{
	}
	public double? condition { get; set; }
	public string other { get; set; }
	public string duration { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		condition = mmria_case.GetNumberListField(p_value, "condition", "prenatal/pre_existing_conditons_grid/condition");
		other = mmria_case.GetStringField(p_value, "other", "prenatal/pre_existing_conditons_grid/other");
		duration = mmria_case.GetStringField(p_value, "duration", "prenatal/pre_existing_conditons_grid/duration");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/pre_existing_conditons_grid/comments");
	}
}

public sealed class _0BEE61C35D43336B78C52D609B939CFF : IConvertDictionary
{
	public _0BEE61C35D43336B78C52D609B939CFF()
	{
	}
	public DateOnly? date { get; set; }
	public string procedure { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "prenatal/prior_surgical_procedures_before_pregnancy/date");
		procedure = mmria_case.GetStringField(p_value, "procedure", "prenatal/prior_surgical_procedures_before_pregnancy/procedure");
		comments = mmria_case.GetStringField(p_value, "comments", "prenatal/prior_surgical_procedures_before_pregnancy/comments");
	}
}

public sealed class _3707ED9DADC50A2734DB4FBA5596CC6F : IConvertDictionary
{
	public _3707ED9DADC50A2734DB4FBA5596CC6F()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "prenatal/location_of_primary_prenatal_care_facility/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "prenatal/location_of_primary_prenatal_care_facility/apartment");
		city = mmria_case.GetStringField(p_value, "city", "prenatal/location_of_primary_prenatal_care_facility/city");
		state = mmria_case.GetStringListField(p_value, "state", "prenatal/location_of_primary_prenatal_care_facility/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "prenatal/location_of_primary_prenatal_care_facility/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "prenatal/location_of_primary_prenatal_care_facility/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "prenatal/location_of_primary_prenatal_care_facility/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "prenatal/location_of_primary_prenatal_care_facility/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "prenatal/location_of_primary_prenatal_care_facility/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "prenatal/location_of_primary_prenatal_care_facility/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "prenatal/location_of_primary_prenatal_care_facility/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "prenatal/location_of_primary_prenatal_care_facility/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "prenatal/location_of_primary_prenatal_care_facility/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "prenatal/location_of_primary_prenatal_care_facility/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "prenatal/location_of_primary_prenatal_care_facility/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "prenatal/location_of_primary_prenatal_care_facility/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "prenatal/location_of_primary_prenatal_care_facility/census_cbsa_micro");
	}
}

public sealed class _F345FC0E715533656FD350796788C303 : IConvertDictionary
{
	public _F345FC0E715533656FD350796788C303()
	{
	}
	public double? place_type { get; set; }
	public string other_place_type { get; set; }
	public double? primary_provider_type { get; set; }
	public string specify_other_provider_type { get; set; }
	public double? principal_source_of_payment { get; set; }
	public string other_payment_source { get; set; }
	public double? is_use_wic { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		place_type = mmria_case.GetNumberListField(p_value, "place_type", "prenatal/primary_prenatal_care_facility/place_type");
		other_place_type = mmria_case.GetStringField(p_value, "other_place_type", "prenatal/primary_prenatal_care_facility/other_place_type");
		primary_provider_type = mmria_case.GetNumberListField(p_value, "primary_provider_type", "prenatal/primary_prenatal_care_facility/primary_provider_type");
		specify_other_provider_type = mmria_case.GetStringField(p_value, "specify_other_provider_type", "prenatal/primary_prenatal_care_facility/specify_other_provider_type");
		principal_source_of_payment = mmria_case.GetNumberListField(p_value, "principal_source_of_payment", "prenatal/primary_prenatal_care_facility/principal_source_of_payment");
		other_payment_source = mmria_case.GetStringField(p_value, "other_payment_source", "prenatal/primary_prenatal_care_facility/other_payment_source");
		is_use_wic = mmria_case.GetNumberListField(p_value, "is_use_wic", "prenatal/primary_prenatal_care_facility/is_use_wic");
	}
}

public sealed class _02DBD2E611DEC822A826C2F0B1D1DE0F : IConvertDictionary
{
	public _02DBD2E611DEC822A826C2F0B1D1DE0F()
	{
		primary_prenatal_care_facility = new ();
		location_of_primary_prenatal_care_facility = new ();
		prior_surgical_procedures_before_pregnancy = new ();
		pre_existing_conditons_grid = new ();
		family_medical_history = new ();
		substance_use_grid = new ();
		pregnancy_history = new ();
		intendedenes = new ();
		infertility_treatment = new ();
		current_pregnancy = new ();
		routine_monitoring = new ();
		highest_blood_pressure = new ();
		other_lab_tests = new ();
		diagnostic_procedures = new ();
		problems_identified_grid = new ();
		medications_and_drugs_during_pregnancy = new ();
		pre_delivery_hospitalizations_details = new ();
		medical_referrals = new ();
		other_sources_of_prenatal_care = new ();
	}
	public string prenatal_care_record_no { get; set; }
	public double? number_of_pnc_sources { get; set; }
	public _F345FC0E715533656FD350796788C303 primary_prenatal_care_facility{ get;set;}
	public _3707ED9DADC50A2734DB4FBA5596CC6F location_of_primary_prenatal_care_facility{ get;set;}
	public List<_0BEE61C35D43336B78C52D609B939CFF> prior_surgical_procedures_before_pregnancy{ get;set;}
	public double? had_pre_existing_conditions { get; set; }
	public List<_99F6F090455B774931675928588796B4> pre_existing_conditons_grid{ get;set;}
	public double? were_there_documented_mental_health_conditions { get; set; }
	public List<_A42F5E1D0CC492390B78D665C2162B38> family_medical_history{ get;set;}
	public double? evidence_of_substance_use { get; set; }
	public List<_DB12CCDCFE9F36C81A924DA8DAA1F817> substance_use_grid{ get;set;}
	public _CB4A0311CE874081BDBCB946D4CE3D0E pregnancy_history{ get;set;}
	public _3B50369398258E8F818A54026A4083D0 intendedenes{ get;set;}
	public _E08949C3EA32F7E6C01990BF3094115F infertility_treatment{ get;set;}
	public _17F17143D948AABBCF8F029D68346E4B current_pregnancy{ get;set;}
	public List<_B2FC9E3D5D8BAE58DD3E9BB9CF1B46D5> routine_monitoring{ get;set;}
	public _7D05294A3D621B59A07C395DD05AC6ED highest_blood_pressure{ get;set;}
	public double? lowest_hematocrit { get; set; }
	public List<_524E042339910523A01145C2724B9350> other_lab_tests{ get;set;}
	public List<_84561DBF6AEC5892A65DF8E2D85470D7> diagnostic_procedures{ get;set;}
	public double? were_there_problems_identified { get; set; }
	public List<_5180AC492C8C104F8787060407249334> problems_identified_grid{ get;set;}
	public double? were_there_adverse_reactions { get; set; }
	public List<_011C3E72A57DE3AB66351169D6F6D5EA> medications_and_drugs_during_pregnancy{ get;set;}
	public double? were_there_pre_delivery_hospitalizations { get; set; }
	public List<_D2DAC659661D88CEB6D93A2091F71CAD> pre_delivery_hospitalizations_details{ get;set;}
	public double? were_medical_referrals_to_others { get; set; }
	public List<_09BC93A66151FE9672FFEB3841E02A0D> medical_referrals{ get;set;}
	public List<_E564335670B1446923BCA2B6D0E5147F> other_sources_of_prenatal_care{ get;set;}
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		prenatal_care_record_no = mmria_case.GetStringField(p_value, "prenatal_care_record_no", "prenatal/prenatal_care_record_no");
		number_of_pnc_sources = mmria_case.GetNumberListField(p_value, "number_of_pnc_sources", "prenatal/number_of_pnc_sources");
		primary_prenatal_care_facility = mmria_case.GetGroupField<_F345FC0E715533656FD350796788C303>(p_value, "primary_prenatal_care_facility", "prenatal/primary_prenatal_care_facility");
		location_of_primary_prenatal_care_facility = mmria_case.GetGroupField<_3707ED9DADC50A2734DB4FBA5596CC6F>(p_value, "location_of_primary_prenatal_care_facility", "prenatal/location_of_primary_prenatal_care_facility");
		prior_surgical_procedures_before_pregnancy = mmria_case.GetGridField<_0BEE61C35D43336B78C52D609B939CFF>(p_value, "prior_surgical_procedures_before_pregnancy", "prenatal/prior_surgical_procedures_before_pregnancy");
		had_pre_existing_conditions = mmria_case.GetNumberListField(p_value, "had_pre_existing_conditions", "prenatal/had_pre_existing_conditions");
		pre_existing_conditons_grid = mmria_case.GetGridField<_99F6F090455B774931675928588796B4>(p_value, "pre_existing_conditons_grid", "prenatal/pre_existing_conditons_grid");
		were_there_documented_mental_health_conditions = mmria_case.GetNumberListField(p_value, "were_there_documented_mental_health_conditions", "prenatal/were_there_documented_mental_health_conditions");
		family_medical_history = mmria_case.GetGridField<_A42F5E1D0CC492390B78D665C2162B38>(p_value, "family_medical_history", "prenatal/family_medical_history");
		evidence_of_substance_use = mmria_case.GetNumberListField(p_value, "evidence_of_substance_use", "prenatal/evidence_of_substance_use");
		substance_use_grid = mmria_case.GetGridField<_DB12CCDCFE9F36C81A924DA8DAA1F817>(p_value, "substance_use_grid", "prenatal/substance_use_grid");
		pregnancy_history = mmria_case.GetGroupField<_CB4A0311CE874081BDBCB946D4CE3D0E>(p_value, "pregnancy_history", "prenatal/pregnancy_history");
		intendedenes = mmria_case.GetGroupField<_3B50369398258E8F818A54026A4083D0>(p_value, "intendedenes", "prenatal/intendedenes");
		infertility_treatment = mmria_case.GetGroupField<_E08949C3EA32F7E6C01990BF3094115F>(p_value, "infertility_treatment", "prenatal/infertility_treatment");
		current_pregnancy = mmria_case.GetGroupField<_17F17143D948AABBCF8F029D68346E4B>(p_value, "current_pregnancy", "prenatal/current_pregnancy");
		routine_monitoring = mmria_case.GetGridField<_B2FC9E3D5D8BAE58DD3E9BB9CF1B46D5>(p_value, "routine_monitoring", "prenatal/routine_monitoring");
		highest_blood_pressure = mmria_case.GetGroupField<_7D05294A3D621B59A07C395DD05AC6ED>(p_value, "highest_blood_pressure", "prenatal/highest_blood_pressure");
		lowest_hematocrit = mmria_case.GetNumberField(p_value, "lowest_hematocrit", "prenatal/lowest_hematocrit");
		other_lab_tests = mmria_case.GetGridField<_524E042339910523A01145C2724B9350>(p_value, "other_lab_tests", "prenatal/other_lab_tests");
		diagnostic_procedures = mmria_case.GetGridField<_84561DBF6AEC5892A65DF8E2D85470D7>(p_value, "diagnostic_procedures", "prenatal/diagnostic_procedures");
		were_there_problems_identified = mmria_case.GetNumberListField(p_value, "were_there_problems_identified", "prenatal/were_there_problems_identified");
		problems_identified_grid = mmria_case.GetGridField<_5180AC492C8C104F8787060407249334>(p_value, "problems_identified_grid", "prenatal/problems_identified_grid");
		were_there_adverse_reactions = mmria_case.GetNumberListField(p_value, "were_there_adverse_reactions", "prenatal/were_there_adverse_reactions");
		medications_and_drugs_during_pregnancy = mmria_case.GetGridField<_011C3E72A57DE3AB66351169D6F6D5EA>(p_value, "medications_and_drugs_during_pregnancy", "prenatal/medications_and_drugs_during_pregnancy");
		were_there_pre_delivery_hospitalizations = mmria_case.GetNumberListField(p_value, "were_there_pre_delivery_hospitalizations", "prenatal/were_there_pre_delivery_hospitalizations");
		pre_delivery_hospitalizations_details = mmria_case.GetGridField<_D2DAC659661D88CEB6D93A2091F71CAD>(p_value, "pre_delivery_hospitalizations_details", "prenatal/pre_delivery_hospitalizations_details");
		were_medical_referrals_to_others = mmria_case.GetNumberListField(p_value, "were_medical_referrals_to_others", "prenatal/were_medical_referrals_to_others");
		medical_referrals = mmria_case.GetGridField<_09BC93A66151FE9672FFEB3841E02A0D>(p_value, "medical_referrals", "prenatal/medical_referrals");
		other_sources_of_prenatal_care = mmria_case.GetGridField<_E564335670B1446923BCA2B6D0E5147F>(p_value, "other_sources_of_prenatal_care", "prenatal/other_sources_of_prenatal_care");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "prenatal/reviewer_note");
	}
}

public sealed class _2D2652A51B7645E75AABEEA44B2748C6 : IConvertDictionary
{
	public _2D2652A51B7645E75AABEEA44B2748C6()
	{
	}
	public double? type { get; set; }
	public string cause { get; set; }
	public string icd_code { get; set; }
	public string comment { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		type = mmria_case.GetNumberListField(p_value, "type", "autopsy_report/causes_of_death/type");
		cause = mmria_case.GetStringField(p_value, "cause", "autopsy_report/causes_of_death/cause");
		icd_code = mmria_case.GetStringField(p_value, "icd_code", "autopsy_report/causes_of_death/icd_code");
		comment = mmria_case.GetStringField(p_value, "comment", "autopsy_report/causes_of_death/comment");
	}
}

public sealed class _5964DC7BA40CF36668B104232BD8921E : IConvertDictionary
{
	public _5964DC7BA40CF36668B104232BD8921E()
	{
	}
	public string substance { get; set; }
	public string substance_other { get; set; }
	public string concentration { get; set; }
	public string unit_of_measure { get; set; }
	public double? level { get; set; }
	public string comment { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		substance = mmria_case.GetStringListField(p_value, "substance", "autopsy_report/toxicology/substance");
		substance_other = mmria_case.GetStringField(p_value, "substance_other", "autopsy_report/toxicology/substance_other");
		concentration = mmria_case.GetStringField(p_value, "concentration", "autopsy_report/toxicology/concentration");
		unit_of_measure = mmria_case.GetStringField(p_value, "unit_of_measure", "autopsy_report/toxicology/unit_of_measure");
		level = mmria_case.GetNumberListField(p_value, "level", "autopsy_report/toxicology/level");
		comment = mmria_case.GetStringField(p_value, "comment", "autopsy_report/toxicology/comment");
	}
}

public sealed class _CF4B7B66A1DC8C0EBA59772A9B821F57 : IConvertDictionary
{
	public _CF4B7B66A1DC8C0EBA59772A9B821F57()
	{
	}
	public string finding { get; set; }
	public string comment { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		finding = mmria_case.GetTextAreaField(p_value, "finding", "autopsy_report/relevant_maternal_death_findings/microscopic_findings/finding");
		comment = mmria_case.GetTextAreaField(p_value, "comment", "autopsy_report/relevant_maternal_death_findings/microscopic_findings/comment");
	}
}

public sealed class _D816D73B417F4C6B7AC740C90F0B9CE4 : IConvertDictionary
{
	public _D816D73B417F4C6B7AC740C90F0B9CE4()
	{
	}
	public string finding { get; set; }
	public string comment { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		finding = mmria_case.GetTextAreaField(p_value, "finding", "autopsy_report/relevant_maternal_death_findings/gross_findings/finding");
		comment = mmria_case.GetTextAreaField(p_value, "comment", "autopsy_report/relevant_maternal_death_findings/gross_findings/comment");
	}
}

public sealed class _83E78247F38C6BD2EE189D9AB6041AF2 : IConvertDictionary
{
	public _83E78247F38C6BD2EE189D9AB6041AF2()
	{
		gross_findings = new ();
		microscopic_findings = new ();
	}
	public List<_D816D73B417F4C6B7AC740C90F0B9CE4> gross_findings{ get;set;}
	public List<_CF4B7B66A1DC8C0EBA59772A9B821F57> microscopic_findings{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		gross_findings = mmria_case.GetGridField<_D816D73B417F4C6B7AC740C90F0B9CE4>(p_value, "gross_findings", "autopsy_report/relevant_maternal_death_findings/gross_findings");
		microscopic_findings = mmria_case.GetGridField<_CF4B7B66A1DC8C0EBA59772A9B821F57>(p_value, "microscopic_findings", "autopsy_report/relevant_maternal_death_findings/microscopic_findings");
	}
}

public sealed class _27CF95EB08CABBF46F4FF4422CE2FC55 : IConvertDictionary
{
	public _27CF95EB08CABBF46F4FF4422CE2FC55()
	{
	}
	public double? fetal_weight_uom { get; set; }
	public double? fetal_weight { get; set; }
	public double? fetal_weight_ounce_value { get; set; }
	public double? fetal_length_uom { get; set; }
	public double? fetal_length { get; set; }
	public string gestational_age_estimate { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		fetal_weight_uom = mmria_case.GetNumberListField(p_value, "fetal_weight_uom", "autopsy_report/biometrics/fetus/fetal_weight_uom");
		fetal_weight = mmria_case.GetNumberField(p_value, "fetal_weight", "autopsy_report/biometrics/fetus/fetal_weight");
		fetal_weight_ounce_value = mmria_case.GetNumberField(p_value, "fetal_weight_ounce_value", "autopsy_report/biometrics/fetus/fetal_weight_ounce_value");
		fetal_length_uom = mmria_case.GetNumberListField(p_value, "fetal_length_uom", "autopsy_report/biometrics/fetus/fetal_length_uom");
		fetal_length = mmria_case.GetNumberField(p_value, "fetal_length", "autopsy_report/biometrics/fetus/fetal_length");
		gestational_age_estimate = mmria_case.GetStringField(p_value, "gestational_age_estimate", "autopsy_report/biometrics/fetus/gestational_age_estimate");
	}
}

public sealed class _25D210E54E314A74A67BCE675C71E0E8 : IConvertDictionary
{
	public _25D210E54E314A74A67BCE675C71E0E8()
	{
	}
	public double? feet { get; set; }
	public double? inches { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		feet = mmria_case.GetNumberField(p_value, "feet", "autopsy_report/biometrics/mother/height/feet");
		inches = mmria_case.GetNumberField(p_value, "inches", "autopsy_report/biometrics/mother/height/inches");
	}
}

public sealed class _4170BDAD1CE2C52F88B99BD6A5470A93 : IConvertDictionary
{
	public _4170BDAD1CE2C52F88B99BD6A5470A93()
	{
		height = new ();
	}
	public _25D210E54E314A74A67BCE675C71E0E8 height{ get;set;}
	public double? weight { get; set; }
	public double? bmi { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		height = mmria_case.GetGroupField<_25D210E54E314A74A67BCE675C71E0E8>(p_value, "height", "autopsy_report/biometrics/mother/height");
		weight = mmria_case.GetNumberField(p_value, "weight", "autopsy_report/biometrics/mother/weight");
		bmi = mmria_case.GetNumberField(p_value, "bmi", "autopsy_report/biometrics/mother/bmi");
	}
}

public sealed class _25978EF7952E5F9AC1D8095C00D28D01 : IConvertDictionary
{
	public _25978EF7952E5F9AC1D8095C00D28D01()
	{
		mother = new ();
		fetus = new ();
	}
	public _4170BDAD1CE2C52F88B99BD6A5470A93 mother{ get;set;}
	public _27CF95EB08CABBF46F4FF4422CE2FC55 fetus{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		mother = mmria_case.GetGroupField<_4170BDAD1CE2C52F88B99BD6A5470A93>(p_value, "mother", "autopsy_report/biometrics/mother");
		fetus = mmria_case.GetGroupField<_27CF95EB08CABBF46F4FF4422CE2FC55>(p_value, "fetus", "autopsy_report/biometrics/fetus");
	}
}

public sealed class _2D7D94EA0A596F15ED7BFB8F26D8FE51 : IConvertDictionary
{
	public _2D7D94EA0A596F15ED7BFB8F26D8FE51()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "autopsy_report/reporter_characteristics/date_of_autopsy/month");
		day = mmria_case.GetNumberListField(p_value, "day", "autopsy_report/reporter_characteristics/date_of_autopsy/day");
		year = mmria_case.GetNumberListField(p_value, "year", "autopsy_report/reporter_characteristics/date_of_autopsy/year");
	}
}

public sealed class _772E74492326508617EAE8C9A1363875 : IConvertDictionary
{
	public _772E74492326508617EAE8C9A1363875()
	{
		date_of_autopsy = new ();
	}
	public double? reporter_type { get; set; }
	public string other_specify { get; set; }
	public _2D7D94EA0A596F15ED7BFB8F26D8FE51 date_of_autopsy{ get;set;}
	public string jurisdiction { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		reporter_type = mmria_case.GetNumberListField(p_value, "reporter_type", "autopsy_report/reporter_characteristics/reporter_type");
		other_specify = mmria_case.GetStringField(p_value, "other_specify", "autopsy_report/reporter_characteristics/other_specify");
		date_of_autopsy = mmria_case.GetGroupField<_2D7D94EA0A596F15ED7BFB8F26D8FE51>(p_value, "date_of_autopsy", "autopsy_report/reporter_characteristics/date_of_autopsy");
		jurisdiction = mmria_case.GetStringField(p_value, "jurisdiction", "autopsy_report/reporter_characteristics/jurisdiction");
	}
}

public sealed class _B01FDEA65CCD8F2AE7E63858F58F93D2 : IConvertDictionary
{
	public _B01FDEA65CCD8F2AE7E63858F58F93D2()
	{
		reporter_characteristics = new ();
		biometrics = new ();
		relevant_maternal_death_findings = new ();
		toxicology = new ();
		causes_of_death = new ();
	}
	public double? was_there_an_autopsy_referral { get; set; }
	public double? type_of_autopsy_or_examination { get; set; }
	public double? is_autopsy_or_exam_report_available { get; set; }
	public double? was_toxicology_performed { get; set; }
	public double? is_toxicology_report_available { get; set; }
	public double? completeness_of_autopsy_information { get; set; }
	public _772E74492326508617EAE8C9A1363875 reporter_characteristics{ get;set;}
	public _25978EF7952E5F9AC1D8095C00D28D01 biometrics{ get;set;}
	public _83E78247F38C6BD2EE189D9AB6041AF2 relevant_maternal_death_findings{ get;set;}
	public double? was_drug_toxicology_positive { get; set; }
	public List<_5964DC7BA40CF36668B104232BD8921E> toxicology{ get;set;}
	public string icd_code_version { get; set; }
	public List<_2D2652A51B7645E75AABEEA44B2748C6> causes_of_death{ get;set;}
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		was_there_an_autopsy_referral = mmria_case.GetNumberListField(p_value, "was_there_an_autopsy_referral", "autopsy_report/was_there_an_autopsy_referral");
		type_of_autopsy_or_examination = mmria_case.GetNumberListField(p_value, "type_of_autopsy_or_examination", "autopsy_report/type_of_autopsy_or_examination");
		is_autopsy_or_exam_report_available = mmria_case.GetNumberListField(p_value, "is_autopsy_or_exam_report_available", "autopsy_report/is_autopsy_or_exam_report_available");
		was_toxicology_performed = mmria_case.GetNumberListField(p_value, "was_toxicology_performed", "autopsy_report/was_toxicology_performed");
		is_toxicology_report_available = mmria_case.GetNumberListField(p_value, "is_toxicology_report_available", "autopsy_report/is_toxicology_report_available");
		completeness_of_autopsy_information = mmria_case.GetNumberListField(p_value, "completeness_of_autopsy_information", "autopsy_report/completeness_of_autopsy_information");
		reporter_characteristics = mmria_case.GetGroupField<_772E74492326508617EAE8C9A1363875>(p_value, "reporter_characteristics", "autopsy_report/reporter_characteristics");
		biometrics = mmria_case.GetGroupField<_25978EF7952E5F9AC1D8095C00D28D01>(p_value, "biometrics", "autopsy_report/biometrics");
		relevant_maternal_death_findings = mmria_case.GetGroupField<_83E78247F38C6BD2EE189D9AB6041AF2>(p_value, "relevant_maternal_death_findings", "autopsy_report/relevant_maternal_death_findings");
		was_drug_toxicology_positive = mmria_case.GetNumberListField(p_value, "was_drug_toxicology_positive", "autopsy_report/was_drug_toxicology_positive");
		toxicology = mmria_case.GetGridField<_5964DC7BA40CF36668B104232BD8921E>(p_value, "toxicology", "autopsy_report/toxicology");
		icd_code_version = mmria_case.GetStringField(p_value, "icd_code_version", "autopsy_report/icd_code_version");
		causes_of_death = mmria_case.GetGridField<_2D2652A51B7645E75AABEEA44B2748C6>(p_value, "causes_of_death", "autopsy_report/causes_of_death");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "autopsy_report/reviewer_note");
	}
}

public sealed class _E756520306FD338C0B0860593DC8DA3A : IConvertDictionary
{
	public _E756520306FD338C0B0860593DC8DA3A()
	{
	}
	public string substance { get; set; }
	public string substance_other { get; set; }
	public double? timing_of_substance_use { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		substance = mmria_case.GetStringListField(p_value, "substance", "social_and_environmental_profile/if_yes_specify_substances/substance");
		substance_other = mmria_case.GetStringField(p_value, "substance_other", "social_and_environmental_profile/if_yes_specify_substances/substance_other");
		timing_of_substance_use = mmria_case.GetNumberListField(p_value, "timing_of_substance_use", "social_and_environmental_profile/if_yes_specify_substances/timing_of_substance_use");
	}
}

public sealed class _6E27E4FBB87969E701428AE88CA84C81 : IConvertDictionary
{
	public _6E27E4FBB87969E701428AE88CA84C81()
	{
	}
	public DateOnly? date { get; set; }
	public double? element { get; set; }
	public string element_other { get; set; }
	public string source_name { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "social_and_environmental_profile/sources_of_social_services_information_for_this_record/date");
		element = mmria_case.GetNumberListField(p_value, "element", "social_and_environmental_profile/sources_of_social_services_information_for_this_record/element");
		element_other = mmria_case.GetStringField(p_value, "element_other", "social_and_environmental_profile/sources_of_social_services_information_for_this_record/element_other");
		source_name = mmria_case.GetStringField(p_value, "source_name", "social_and_environmental_profile/sources_of_social_services_information_for_this_record/source_name");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "social_and_environmental_profile/sources_of_social_services_information_for_this_record/comments");
	}
}

public sealed class _432CE1B3BC36EF9DE32E8468D8534AE8 : IConvertDictionary
{
	public _432CE1B3BC36EF9DE32E8468D8534AE8()
	{
	}
	public DateOnly? date { get; set; }
	public string referred_to { get; set; }
	public string specialty { get; set; }
	public string reason { get; set; }
	public double? compiled { get; set; }
	public string reason_for_non_compliance { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "social_and_environmental_profile/social_and_medical_referrals/date");
		referred_to = mmria_case.GetStringField(p_value, "referred_to", "social_and_environmental_profile/social_and_medical_referrals/referred_to");
		specialty = mmria_case.GetStringField(p_value, "specialty", "social_and_environmental_profile/social_and_medical_referrals/specialty");
		reason = mmria_case.GetStringField(p_value, "reason", "social_and_environmental_profile/social_and_medical_referrals/reason");
		compiled = mmria_case.GetNumberListField(p_value, "compiled", "social_and_environmental_profile/social_and_medical_referrals/compiled");
		reason_for_non_compliance = mmria_case.GetStringField(p_value, "reason_for_non_compliance", "social_and_environmental_profile/social_and_medical_referrals/reason_for_non_compliance");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "social_and_environmental_profile/social_and_medical_referrals/comments");
	}
}

public sealed class _50B879B8B59D6A57B6EB05567DDBCA33 : IConvertDictionary
{
	public _50B879B8B59D6A57B6EB05567DDBCA33()
	{
		reasons_for_missed_appointments = new ();
	}
	public double? no_prenatal_care { get; set; }
	public List<double> reasons_for_missed_appointments { get; set; }
	public string specify_other_reason { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		no_prenatal_care = mmria_case.GetNumberListField(p_value, "no_prenatal_care", "social_and_environmental_profile/health_care_system/no_prenatal_care");
		reasons_for_missed_appointments = mmria_case.GetMultiSelectNumberListField(p_value, "reasons_for_missed_appointments", "social_and_environmental_profile/health_care_system/reasons_for_missed_appointments");
		specify_other_reason = mmria_case.GetStringField(p_value, "specify_other_reason", "social_and_environmental_profile/health_care_system/specify_other_reason");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "social_and_environmental_profile/health_care_system/comments");
	}
}

public sealed class _96AA3E194F6BC537FB5F434DFD615757 : IConvertDictionary
{
	public _96AA3E194F6BC537FB5F434DFD615757()
	{
		evidence_of_social_or_emotional_stress = new ();
	}
	public List<double> evidence_of_social_or_emotional_stress { get; set; }
	public string specify_other_evidence_stress { get; set; }
	public string explain_further { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		evidence_of_social_or_emotional_stress = mmria_case.GetMultiSelectNumberListField(p_value, "evidence_of_social_or_emotional_stress", "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress");
		specify_other_evidence_stress = mmria_case.GetStringField(p_value, "specify_other_evidence_stress", "social_and_environmental_profile/social_or_emotional_stress/specify_other_evidence_stress");
		explain_further = mmria_case.GetTextAreaField(p_value, "explain_further", "social_and_environmental_profile/social_or_emotional_stress/explain_further");
	}
}

public sealed class _E8C0CF44AF3545233757E6289E24D81E : IConvertDictionary
{
	public _E8C0CF44AF3545233757E6289E24D81E()
	{
		barriers_to_communications = new ();
	}
	public List<double> barriers_to_communications { get; set; }
	public string barriers_to_communications_other_specify { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		barriers_to_communications = mmria_case.GetMultiSelectNumberListField(p_value, "barriers_to_communications", "social_and_environmental_profile/communications/barriers_to_communications");
		barriers_to_communications_other_specify = mmria_case.GetStringField(p_value, "barriers_to_communications_other_specify", "social_and_environmental_profile/communications/barriers_to_communications_other_specify");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "social_and_environmental_profile/communications/comments");
	}
}

public sealed class _BF3869379E680EFF0AFD228F6721E321 : IConvertDictionary
{
	public _BF3869379E680EFF0AFD228F6721E321()
	{
		barriers_to_health_care_access = new ();
	}
	public List<double> barriers_to_health_care_access { get; set; }
	public string barriers_to_health_care_access_other_specify { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		barriers_to_health_care_access = mmria_case.GetMultiSelectNumberListField(p_value, "barriers_to_health_care_access", "social_and_environmental_profile/health_care_access/barriers_to_health_care_access");
		barriers_to_health_care_access_other_specify = mmria_case.GetStringField(p_value, "barriers_to_health_care_access_other_specify", "social_and_environmental_profile/health_care_access/barriers_to_health_care_access_other_specify");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "social_and_environmental_profile/health_care_access/comments");
	}
}

public sealed class _89F006213C97FCDC5E5ADF202F4F432A : IConvertDictionary
{
	public _89F006213C97FCDC5E5ADF202F4F432A()
	{
	}
	public DateOnly? date_of_arrest { get; set; }
	public string arest_reason { get; set; }
	public double? occurrence { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_arrest = mmria_case.GetDateField(p_value, "date_of_arrest", "social_and_environmental_profile/details_of_arrests/date_of_arrest");
		arest_reason = mmria_case.GetStringField(p_value, "arest_reason", "social_and_environmental_profile/details_of_arrests/arest_reason");
		occurrence = mmria_case.GetNumberListField(p_value, "occurrence", "social_and_environmental_profile/details_of_arrests/occurrence");
		comments = mmria_case.GetTextAreaField(p_value, "comments", "social_and_environmental_profile/details_of_arrests/comments");
	}
}

public sealed class _A8642BB74354C5376FC865B44333C728 : IConvertDictionary
{
	public _A8642BB74354C5376FC865B44333C728()
	{
	}
	public DateOnly? date { get; set; }
	public string duration { get; set; }
	public string reason { get; set; }
	public double? occurrence { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date = mmria_case.GetDateField(p_value, "date", "social_and_environmental_profile/details_of_incarcerations/date");
		duration = mmria_case.GetStringField(p_value, "duration", "social_and_environmental_profile/details_of_incarcerations/duration");
		reason = mmria_case.GetStringField(p_value, "reason", "social_and_environmental_profile/details_of_incarcerations/reason");
		occurrence = mmria_case.GetNumberListField(p_value, "occurrence", "social_and_environmental_profile/details_of_incarcerations/occurrence");
		comments = mmria_case.GetStringField(p_value, "comments", "social_and_environmental_profile/details_of_incarcerations/comments");
	}
}

public sealed class _3B2F8F0C651E92A4C85C4EE2E36AAFCD : IConvertDictionary
{
	public _3B2F8F0C651E92A4C85C4EE2E36AAFCD()
	{
	}
	public double? relationship { get; set; }
	public double? gender { get; set; }
	public string age { get; set; }
	public string comments { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		relationship = mmria_case.GetNumberListField(p_value, "relationship", "social_and_environmental_profile/members_of_household/relationship");
		gender = mmria_case.GetNumberListField(p_value, "gender", "social_and_environmental_profile/members_of_household/gender");
		age = mmria_case.GetStringField(p_value, "age", "social_and_environmental_profile/members_of_household/age");
		comments = mmria_case.GetStringField(p_value, "comments", "social_and_environmental_profile/members_of_household/comments");
	}
}

public sealed class _EB9A042F77D74AD816926B7E549ED2B0 : IConvertDictionary
{
	public _EB9A042F77D74AD816926B7E549ED2B0()
	{
		sep_genid_source = new ();
	}
	public double? sep_genid_is_nonfemale { get; set; }
	public List<double> sep_genid_source { get; set; }
	public string sep_genid_source_othersp { get; set; }
	public string sep_genid_source_terms { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		sep_genid_is_nonfemale = mmria_case.GetNumberListField(p_value, "sep_genid_is_nonfemale", "social_and_environmental_profile/gender_identity/sep_genid_is_nonfemale");
		sep_genid_source = mmria_case.GetMultiSelectNumberListField(p_value, "sep_genid_source", "social_and_environmental_profile/gender_identity/sep_genid_source");
		sep_genid_source_othersp = mmria_case.GetStringField(p_value, "sep_genid_source_othersp", "social_and_environmental_profile/gender_identity/sep_genid_source_othersp");
		sep_genid_source_terms = mmria_case.GetTextAreaField(p_value, "sep_genid_source_terms", "social_and_environmental_profile/gender_identity/sep_genid_source_terms");
	}
}

public sealed class _C86F3E109F5ED04A5C51E2F5FBD01ACD : IConvertDictionary
{
	public _C86F3E109F5ED04A5C51E2F5FBD01ACD()
	{
		homelessness = new ();
		unstable_housing = new ();
	}
	public double? source_of_income { get; set; }
	public string source_of_income_other_specify { get; set; }
	public double? employment_status { get; set; }
	public string employment_status_other_specify { get; set; }
	public string occupation { get; set; }
	public string religious_preference { get; set; }
	public string country_of_birth { get; set; }
	public double? immigration_status { get; set; }
	public double? time_in_the_us { get; set; }
	public double? time_in_the_us_units { get; set; }
	public double? current_living_arrangements { get; set; }
	public List<double> homelessness { get; set; }
	public List<double> unstable_housing { get; set; }
	public string sep_m_occupation_code_1 { get; set; }
	public string sep_m_occupation_code_2 { get; set; }
	public string sep_m_occupation_code_3 { get; set; }
	public string sep_m_industry_code_1 { get; set; }
	public string sep_m_industry_code_2 { get; set; }
	public string sep_m_industry_code_3 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		source_of_income = mmria_case.GetNumberListField(p_value, "source_of_income", "social_and_environmental_profile/socio_economic_characteristics/source_of_income");
		source_of_income_other_specify = mmria_case.GetStringField(p_value, "source_of_income_other_specify", "social_and_environmental_profile/socio_economic_characteristics/source_of_income_other_specify");
		employment_status = mmria_case.GetNumberListField(p_value, "employment_status", "social_and_environmental_profile/socio_economic_characteristics/employment_status");
		employment_status_other_specify = mmria_case.GetStringField(p_value, "employment_status_other_specify", "social_and_environmental_profile/socio_economic_characteristics/employment_status_other_specify");
		occupation = mmria_case.GetStringField(p_value, "occupation", "social_and_environmental_profile/socio_economic_characteristics/occupation");
		religious_preference = mmria_case.GetStringField(p_value, "religious_preference", "social_and_environmental_profile/socio_economic_characteristics/religious_preference");
		country_of_birth = mmria_case.GetStringListField(p_value, "country_of_birth", "social_and_environmental_profile/socio_economic_characteristics/country_of_birth");
		immigration_status = mmria_case.GetNumberListField(p_value, "immigration_status", "social_and_environmental_profile/socio_economic_characteristics/immigration_status");
		time_in_the_us = mmria_case.GetNumberField(p_value, "time_in_the_us", "social_and_environmental_profile/socio_economic_characteristics/time_in_the_us");
		time_in_the_us_units = mmria_case.GetNumberListField(p_value, "time_in_the_us_units", "social_and_environmental_profile/socio_economic_characteristics/time_in_the_us_units");
		current_living_arrangements = mmria_case.GetNumberListField(p_value, "current_living_arrangements", "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements");
		homelessness = mmria_case.GetMultiSelectNumberListField(p_value, "homelessness", "social_and_environmental_profile/socio_economic_characteristics/homelessness");
		unstable_housing = mmria_case.GetMultiSelectNumberListField(p_value, "unstable_housing", "social_and_environmental_profile/socio_economic_characteristics/unstable_housing");
		sep_m_occupation_code_1 = mmria_case.GetHiddenField(p_value, "sep_m_occupation_code_1", "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1");
		sep_m_occupation_code_2 = mmria_case.GetHiddenField(p_value, "sep_m_occupation_code_2", "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2");
		sep_m_occupation_code_3 = mmria_case.GetHiddenField(p_value, "sep_m_occupation_code_3", "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3");
		sep_m_industry_code_1 = mmria_case.GetHiddenField(p_value, "sep_m_industry_code_1", "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1");
		sep_m_industry_code_2 = mmria_case.GetHiddenField(p_value, "sep_m_industry_code_2", "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2");
		sep_m_industry_code_3 = mmria_case.GetHiddenField(p_value, "sep_m_industry_code_3", "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3");
	}
}

public sealed class _F495787DD96BB2B871443F9596F9C77F : IConvertDictionary
{
	public _F495787DD96BB2B871443F9596F9C77F()
	{
		socio_economic_characteristics = new ();
		gender_identity = new ();
		members_of_household = new ();
		previous_or_current_incarcerations = new ();
		details_of_incarcerations = new ();
		was_decedent_ever_arrested = new ();
		details_of_arrests = new ();
		health_care_access = new ();
		communications = new ();
		social_or_emotional_stress = new ();
		health_care_system = new ();
		social_and_medical_referrals = new ();
		sources_of_social_services_information_for_this_record = new ();
		if_yes_specify_substances = new ();
	}
	public _C86F3E109F5ED04A5C51E2F5FBD01ACD socio_economic_characteristics{ get;set;}
	public _EB9A042F77D74AD816926B7E549ED2B0 gender_identity{ get;set;}
	public List<_3B2F8F0C651E92A4C85C4EE2E36AAFCD> members_of_household{ get;set;}
	public List<double> previous_or_current_incarcerations { get; set; }
	public List<_A8642BB74354C5376FC865B44333C728> details_of_incarcerations{ get;set;}
	public List<double> was_decedent_ever_arrested { get; set; }
	public List<_89F006213C97FCDC5E5ADF202F4F432A> details_of_arrests{ get;set;}
	public _BF3869379E680EFF0AFD228F6721E321 health_care_access{ get;set;}
	public _E8C0CF44AF3545233757E6289E24D81E communications{ get;set;}
	public _96AA3E194F6BC537FB5F434DFD615757 social_or_emotional_stress{ get;set;}
	public _50B879B8B59D6A57B6EB05567DDBCA33 health_care_system{ get;set;}
	public double? had_military_service { get; set; }
	public double? was_there_bereavement_support { get; set; }
	public List<_432CE1B3BC36EF9DE32E8468D8534AE8> social_and_medical_referrals{ get;set;}
	public List<_6E27E4FBB87969E701428AE88CA84C81> sources_of_social_services_information_for_this_record{ get;set;}
	public double? documented_substance_use { get; set; }
	public List<_E756520306FD338C0B0860593DC8DA3A> if_yes_specify_substances{ get;set;}
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		socio_economic_characteristics = mmria_case.GetGroupField<_C86F3E109F5ED04A5C51E2F5FBD01ACD>(p_value, "socio_economic_characteristics", "social_and_environmental_profile/socio_economic_characteristics");
		gender_identity = mmria_case.GetGroupField<_EB9A042F77D74AD816926B7E549ED2B0>(p_value, "gender_identity", "social_and_environmental_profile/gender_identity");
		members_of_household = mmria_case.GetGridField<_3B2F8F0C651E92A4C85C4EE2E36AAFCD>(p_value, "members_of_household", "social_and_environmental_profile/members_of_household");
		previous_or_current_incarcerations = mmria_case.GetMultiSelectNumberListField(p_value, "previous_or_current_incarcerations", "social_and_environmental_profile/previous_or_current_incarcerations");
		details_of_incarcerations = mmria_case.GetGridField<_A8642BB74354C5376FC865B44333C728>(p_value, "details_of_incarcerations", "social_and_environmental_profile/details_of_incarcerations");
		was_decedent_ever_arrested = mmria_case.GetMultiSelectNumberListField(p_value, "was_decedent_ever_arrested", "social_and_environmental_profile/was_decedent_ever_arrested");
		details_of_arrests = mmria_case.GetGridField<_89F006213C97FCDC5E5ADF202F4F432A>(p_value, "details_of_arrests", "social_and_environmental_profile/details_of_arrests");
		health_care_access = mmria_case.GetGroupField<_BF3869379E680EFF0AFD228F6721E321>(p_value, "health_care_access", "social_and_environmental_profile/health_care_access");
		communications = mmria_case.GetGroupField<_E8C0CF44AF3545233757E6289E24D81E>(p_value, "communications", "social_and_environmental_profile/communications");
		social_or_emotional_stress = mmria_case.GetGroupField<_96AA3E194F6BC537FB5F434DFD615757>(p_value, "social_or_emotional_stress", "social_and_environmental_profile/social_or_emotional_stress");
		health_care_system = mmria_case.GetGroupField<_50B879B8B59D6A57B6EB05567DDBCA33>(p_value, "health_care_system", "social_and_environmental_profile/health_care_system");
		had_military_service = mmria_case.GetNumberListField(p_value, "had_military_service", "social_and_environmental_profile/had_military_service");
		was_there_bereavement_support = mmria_case.GetNumberListField(p_value, "was_there_bereavement_support", "social_and_environmental_profile/was_there_bereavement_support");
		social_and_medical_referrals = mmria_case.GetGridField<_432CE1B3BC36EF9DE32E8468D8534AE8>(p_value, "social_and_medical_referrals", "social_and_environmental_profile/social_and_medical_referrals");
		sources_of_social_services_information_for_this_record = mmria_case.GetGridField<_6E27E4FBB87969E701428AE88CA84C81>(p_value, "sources_of_social_services_information_for_this_record", "social_and_environmental_profile/sources_of_social_services_information_for_this_record");
		documented_substance_use = mmria_case.GetNumberListField(p_value, "documented_substance_use", "social_and_environmental_profile/documented_substance_use");
		if_yes_specify_substances = mmria_case.GetGridField<_E756520306FD338C0B0860593DC8DA3A>(p_value, "if_yes_specify_substances", "social_and_environmental_profile/if_yes_specify_substances");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "social_and_environmental_profile/reviewer_note");
	}
}

public sealed class _5FC61AE6AD6DF115AA29045A66A14983 : IConvertDictionary
{
	public _5FC61AE6AD6DF115AA29045A66A14983()
	{
	}
	public string cvs_api_request_url { get; set; }
	public string cvs_api_request_date_time { get; set; }
	public string cvs_api_request_c_geoid { get; set; }
	public string cvs_api_request_t_geoid { get; set; }
	public string cvs_api_request_year { get; set; }
	public string cvs_api_request_result_message { get; set; }
	public string cvs_mdrate_county { get; set; }
	public string cvs_pctnoins_fem_tract { get; set; }
	public string cvs_pctnovehicle_county { get; set; }
	public string cvs_pctnovehicle_tract { get; set; }
	public string cvs_pctmove_tract { get; set; }
	public string cvs_pctsphh_tract { get; set; }
	public string cvs_pctovercrowdhh_tract { get; set; }
	public string cvs_pctowner_occ_tract { get; set; }
	public string cvs_pct_less_well_tract { get; set; }
	public string cvs_ndi_raw_tract { get; set; }
	public string cvs_pctpov_tract { get; set; }
	public string cvs_ice_income_all_tract { get; set; }
	public string cvs_pctobese_county { get; set; }
	public string cvs_fi_county { get; set; }
	public string cvs_cnmrate_county { get; set; }
	public string cvs_obgynrate_county { get; set; }
	public string cvs_rtteenbirth_county { get; set; }
	public string cvs_rtstd_county { get; set; }
	public string cvs_rtdrugodmortality_county { get; set; }
	public string cvs_rtsocassoc_county { get; set; }
	public string cvs_pcthouse_distress_county { get; set; }
	public string cvs_rtviolentcr_icpsr_county { get; set; }
	public string cvs_isolation_county { get; set; }
	public string cvs_pctrural { get; set; }
	public string cvs_racialized_pov { get; set; }
	public string cvs_mhproviderrate { get; set; }
	public string cvs_rtmhpract_county { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		cvs_api_request_url = mmria_case.GetHiddenField(p_value, "cvs_api_request_url", "cvs/cvs_grid/cvs_api_request_url");
		cvs_api_request_date_time = mmria_case.GetHiddenField(p_value, "cvs_api_request_date_time", "cvs/cvs_grid/cvs_api_request_date_time");
		cvs_api_request_c_geoid = mmria_case.GetHiddenField(p_value, "cvs_api_request_c_geoid", "cvs/cvs_grid/cvs_api_request_c_geoid");
		cvs_api_request_t_geoid = mmria_case.GetHiddenField(p_value, "cvs_api_request_t_geoid", "cvs/cvs_grid/cvs_api_request_t_geoid");
		cvs_api_request_year = mmria_case.GetHiddenField(p_value, "cvs_api_request_year", "cvs/cvs_grid/cvs_api_request_year");
		cvs_api_request_result_message = mmria_case.GetHiddenField(p_value, "cvs_api_request_result_message", "cvs/cvs_grid/cvs_api_request_result_message");
		cvs_mdrate_county = mmria_case.GetHiddenField(p_value, "cvs_mdrate_county", "cvs/cvs_grid/cvs_mdrate_county");
		cvs_pctnoins_fem_tract = mmria_case.GetHiddenField(p_value, "cvs_pctnoins_fem_tract", "cvs/cvs_grid/cvs_pctnoins_fem_tract");
		cvs_pctnovehicle_county = mmria_case.GetHiddenField(p_value, "cvs_pctnovehicle_county", "cvs/cvs_grid/cvs_pctnovehicle_county");
		cvs_pctnovehicle_tract = mmria_case.GetHiddenField(p_value, "cvs_pctnovehicle_tract", "cvs/cvs_grid/cvs_pctnovehicle_tract");
		cvs_pctmove_tract = mmria_case.GetHiddenField(p_value, "cvs_pctmove_tract", "cvs/cvs_grid/cvs_pctmove_tract");
		cvs_pctsphh_tract = mmria_case.GetHiddenField(p_value, "cvs_pctsphh_tract", "cvs/cvs_grid/cvs_pctsphh_tract");
		cvs_pctovercrowdhh_tract = mmria_case.GetHiddenField(p_value, "cvs_pctovercrowdhh_tract", "cvs/cvs_grid/cvs_pctovercrowdhh_tract");
		cvs_pctowner_occ_tract = mmria_case.GetHiddenField(p_value, "cvs_pctowner_occ_tract", "cvs/cvs_grid/cvs_pctowner_occ_tract");
		cvs_pct_less_well_tract = mmria_case.GetHiddenField(p_value, "cvs_pct_less_well_tract", "cvs/cvs_grid/cvs_pct_less_well_tract");
		cvs_ndi_raw_tract = mmria_case.GetHiddenField(p_value, "cvs_ndi_raw_tract", "cvs/cvs_grid/cvs_ndi_raw_tract");
		cvs_pctpov_tract = mmria_case.GetHiddenField(p_value, "cvs_pctpov_tract", "cvs/cvs_grid/cvs_pctpov_tract");
		cvs_ice_income_all_tract = mmria_case.GetHiddenField(p_value, "cvs_ice_income_all_tract", "cvs/cvs_grid/cvs_ice_income_all_tract");
		cvs_pctobese_county = mmria_case.GetHiddenField(p_value, "cvs_pctobese_county", "cvs/cvs_grid/cvs_pctobese_county");
		cvs_fi_county = mmria_case.GetHiddenField(p_value, "cvs_fi_county", "cvs/cvs_grid/cvs_fi_county");
		cvs_cnmrate_county = mmria_case.GetHiddenField(p_value, "cvs_cnmrate_county", "cvs/cvs_grid/cvs_cnmrate_county");
		cvs_obgynrate_county = mmria_case.GetHiddenField(p_value, "cvs_obgynrate_county", "cvs/cvs_grid/cvs_obgynrate_county");
		cvs_rtteenbirth_county = mmria_case.GetHiddenField(p_value, "cvs_rtteenbirth_county", "cvs/cvs_grid/cvs_rtteenbirth_county");
		cvs_rtstd_county = mmria_case.GetHiddenField(p_value, "cvs_rtstd_county", "cvs/cvs_grid/cvs_rtstd_county");
		cvs_rtdrugodmortality_county = mmria_case.GetHiddenField(p_value, "cvs_rtdrugodmortality_county", "cvs/cvs_grid/cvs_rtdrugodmortality_county");
		cvs_rtsocassoc_county = mmria_case.GetHiddenField(p_value, "cvs_rtsocassoc_county", "cvs/cvs_grid/cvs_rtsocassoc_county");
		cvs_pcthouse_distress_county = mmria_case.GetHiddenField(p_value, "cvs_pcthouse_distress_county", "cvs/cvs_grid/cvs_pcthouse_distress_county");
		cvs_rtviolentcr_icpsr_county = mmria_case.GetHiddenField(p_value, "cvs_rtviolentcr_icpsr_county", "cvs/cvs_grid/cvs_rtviolentcr_icpsr_county");
		cvs_isolation_county = mmria_case.GetHiddenField(p_value, "cvs_isolation_county", "cvs/cvs_grid/cvs_isolation_county");
		cvs_pctrural = mmria_case.GetHiddenField(p_value, "cvs_pctrural", "cvs/cvs_grid/cvs_pctrural");
		cvs_racialized_pov = mmria_case.GetHiddenField(p_value, "cvs_racialized_pov", "cvs/cvs_grid/cvs_racialized_pov");
		cvs_mhproviderrate = mmria_case.GetHiddenField(p_value, "cvs_mhproviderrate", "cvs/cvs_grid/cvs_mhproviderrate");
		cvs_rtmhpract_county = mmria_case.GetHiddenField(p_value, "cvs_rtmhpract_county", "cvs/cvs_grid/cvs_rtmhpract_county");
	}
}

public sealed class _72F1A850D966375FA159121C7C8B09A1 : IConvertDictionary
{
	public _72F1A850D966375FA159121C7C8B09A1()
	{
		cvs_grid = new ();
	}
	public double? cvs_used { get; set; }
	public double? cvs_used_how { get; set; }
	public string cvs_used_other_sp { get; set; }
	public string reviewer_note { get; set; }
	public List<_5FC61AE6AD6DF115AA29045A66A14983> cvs_grid{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		cvs_used = mmria_case.GetNumberListField(p_value, "cvs_used", "cvs/cvs_used");
		cvs_used_how = mmria_case.GetNumberListField(p_value, "cvs_used_how", "cvs/cvs_used_how");
		cvs_used_other_sp = mmria_case.GetStringField(p_value, "cvs_used_other_sp", "cvs/cvs_used_other_sp");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "cvs/reviewer_note");
		cvs_grid = mmria_case.GetGridField<_5FC61AE6AD6DF115AA29045A66A14983>(p_value, "cvs_grid", "cvs/cvs_grid");
	}
}

public sealed class _180D7E8BAC8D3315D498840F354A7694 : IConvertDictionary
{
	public _180D7E8BAC8D3315D498840F354A7694()
	{
	}
	public string summary_text { get; set; }
	public string cod18a1 { get; set; }
	public string cod18a2 { get; set; }
	public string cod18a3 { get; set; }
	public string cod18a4 { get; set; }
	public string cod18a5 { get; set; }
	public string cod18a6 { get; set; }
	public string cod18a7 { get; set; }
	public string cod18a8 { get; set; }
	public string cod18a9 { get; set; }
	public string cod18a10 { get; set; }
	public string cod18a11 { get; set; }
	public string cod18a12 { get; set; }
	public string cod18a13 { get; set; }
	public string cod18a14 { get; set; }
	public string cod18b1 { get; set; }
	public string cod18b2 { get; set; }
	public string cod18b3 { get; set; }
	public string cod18b4 { get; set; }
	public string cod18b5 { get; set; }
	public string cod18b6 { get; set; }
	public string cod18b7 { get; set; }
	public string cod18b8 { get; set; }
	public string cod18b9 { get; set; }
	public string cod18b10 { get; set; }
	public string cod18b11 { get; set; }
	public string cod18b12 { get; set; }
	public string cod18b13 { get; set; }
	public string cod18b14 { get; set; }
	public string icod { get; set; }
	public string ocod1 { get; set; }
	public string ocod2 { get; set; }
	public string ocod3 { get; set; }
	public string ocod4 { get; set; }
	public string ocod5 { get; set; }
	public string ocod6 { get; set; }
	public string ocod7 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		summary_text = mmria_case.GetTextAreaField(p_value, "summary_text", "birth_certificate_infant_fetal_section/vitals_import_group/summary_text");
		cod18a1 = mmria_case.GetStringField(p_value, "cod18a1", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a1");
		cod18a2 = mmria_case.GetStringField(p_value, "cod18a2", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a2");
		cod18a3 = mmria_case.GetStringField(p_value, "cod18a3", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a3");
		cod18a4 = mmria_case.GetStringField(p_value, "cod18a4", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a4");
		cod18a5 = mmria_case.GetStringField(p_value, "cod18a5", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a5");
		cod18a6 = mmria_case.GetStringField(p_value, "cod18a6", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a6");
		cod18a7 = mmria_case.GetStringField(p_value, "cod18a7", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a7");
		cod18a8 = mmria_case.GetStringField(p_value, "cod18a8", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a8");
		cod18a9 = mmria_case.GetStringField(p_value, "cod18a9", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a9");
		cod18a10 = mmria_case.GetStringField(p_value, "cod18a10", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a10");
		cod18a11 = mmria_case.GetStringField(p_value, "cod18a11", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a11");
		cod18a12 = mmria_case.GetStringField(p_value, "cod18a12", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a12");
		cod18a13 = mmria_case.GetStringField(p_value, "cod18a13", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a13");
		cod18a14 = mmria_case.GetStringField(p_value, "cod18a14", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a14");
		cod18b1 = mmria_case.GetStringField(p_value, "cod18b1", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b1");
		cod18b2 = mmria_case.GetStringField(p_value, "cod18b2", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b2");
		cod18b3 = mmria_case.GetStringField(p_value, "cod18b3", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b3");
		cod18b4 = mmria_case.GetStringField(p_value, "cod18b4", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b4");
		cod18b5 = mmria_case.GetStringField(p_value, "cod18b5", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b5");
		cod18b6 = mmria_case.GetStringField(p_value, "cod18b6", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b6");
		cod18b7 = mmria_case.GetStringField(p_value, "cod18b7", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b7");
		cod18b8 = mmria_case.GetStringField(p_value, "cod18b8", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b8");
		cod18b9 = mmria_case.GetStringField(p_value, "cod18b9", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b9");
		cod18b10 = mmria_case.GetStringField(p_value, "cod18b10", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b10");
		cod18b11 = mmria_case.GetStringField(p_value, "cod18b11", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b11");
		cod18b12 = mmria_case.GetStringField(p_value, "cod18b12", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b12");
		cod18b13 = mmria_case.GetStringField(p_value, "cod18b13", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b13");
		cod18b14 = mmria_case.GetStringField(p_value, "cod18b14", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b14");
		icod = mmria_case.GetStringField(p_value, "icod", "birth_certificate_infant_fetal_section/vitals_import_group/icod");
		ocod1 = mmria_case.GetStringField(p_value, "ocod1", "birth_certificate_infant_fetal_section/vitals_import_group/ocod1");
		ocod2 = mmria_case.GetStringField(p_value, "ocod2", "birth_certificate_infant_fetal_section/vitals_import_group/ocod2");
		ocod3 = mmria_case.GetStringField(p_value, "ocod3", "birth_certificate_infant_fetal_section/vitals_import_group/ocod3");
		ocod4 = mmria_case.GetStringField(p_value, "ocod4", "birth_certificate_infant_fetal_section/vitals_import_group/ocod4");
		ocod5 = mmria_case.GetStringField(p_value, "ocod5", "birth_certificate_infant_fetal_section/vitals_import_group/ocod5");
		ocod6 = mmria_case.GetStringField(p_value, "ocod6", "birth_certificate_infant_fetal_section/vitals_import_group/ocod6");
		ocod7 = mmria_case.GetStringField(p_value, "ocod7", "birth_certificate_infant_fetal_section/vitals_import_group/ocod7");
	}
}

public sealed class _D254CBE21CC45929794503CA76F3D904 : IConvertDictionary
{
	public _D254CBE21CC45929794503CA76F3D904()
	{
	}
	public string type { get; set; }
	public string @class { get; set; }
	public string complication_subclass { get; set; }
	public string other_specify { get; set; }
	public string icd_code { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		type = mmria_case.GetStringField(p_value, "type", "birth_certificate_infant_fetal_section/causes_of_death/type");
		@class = mmria_case.GetStringField(p_value, "class", "birth_certificate_infant_fetal_section/causes_of_death/class");
		complication_subclass = mmria_case.GetStringField(p_value, "complication_subclass", "birth_certificate_infant_fetal_section/causes_of_death/complication_subclass");
		other_specify = mmria_case.GetStringField(p_value, "other_specify", "birth_certificate_infant_fetal_section/causes_of_death/other_specify");
		icd_code = mmria_case.GetStringField(p_value, "icd_code", "birth_certificate_infant_fetal_section/causes_of_death/icd_code");
	}
}

public sealed class _EE7E3E53FEF286D9C356E5E2EFC17F2C : IConvertDictionary
{
	public _EE7E3E53FEF286D9C356E5E2EFC17F2C()
	{
	}
	public double? was_delivery_with_forceps_attempted_but_unsuccessful { get; set; }
	public double? was_delivery_with_vacuum_extration_attempted_but_unsuccessful { get; set; }
	public double? fetal_delivery { get; set; }
	public string other_presentation { get; set; }
	public double? final_route_and_method_of_delivery { get; set; }
	public double? if_cesarean_was_trial_of_labor_attempted { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		was_delivery_with_forceps_attempted_but_unsuccessful = mmria_case.GetNumberListField(p_value, "was_delivery_with_forceps_attempted_but_unsuccessful", "birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_forceps_attempted_but_unsuccessful");
		was_delivery_with_vacuum_extration_attempted_but_unsuccessful = mmria_case.GetNumberListField(p_value, "was_delivery_with_vacuum_extration_attempted_but_unsuccessful", "birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_vacuum_extration_attempted_but_unsuccessful");
		fetal_delivery = mmria_case.GetNumberListField(p_value, "fetal_delivery", "birth_certificate_infant_fetal_section/method_of_delivery/fetal_delivery");
		other_presentation = mmria_case.GetStringField(p_value, "other_presentation", "birth_certificate_infant_fetal_section/method_of_delivery/other_presentation");
		final_route_and_method_of_delivery = mmria_case.GetNumberListField(p_value, "final_route_and_method_of_delivery", "birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery");
		if_cesarean_was_trial_of_labor_attempted = mmria_case.GetNumberListField(p_value, "if_cesarean_was_trial_of_labor_attempted", "birth_certificate_infant_fetal_section/method_of_delivery/if_cesarean_was_trial_of_labor_attempted");
	}
}

public sealed class _65228517D188454210BA20A6234601CB : IConvertDictionary
{
	public _65228517D188454210BA20A6234601CB()
	{
	}
	public double? minute_5 { get; set; }
	public double? minute_10 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		minute_5 = mmria_case.GetNumberField(p_value, "minute_5", "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_5");
		minute_10 = mmria_case.GetNumberField(p_value, "minute_10", "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_10");
	}
}

public sealed class _564D05D8DE1347EFFC8CACE64166DFB3 : IConvertDictionary
{
	public _564D05D8DE1347EFFC8CACE64166DFB3()
	{
	}
	public double? unit_of_measurement { get; set; }
	public double? grams_or_pounds { get; set; }
	public double? ounces { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		unit_of_measurement = mmria_case.GetNumberListField(p_value, "unit_of_measurement", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/unit_of_measurement");
		grams_or_pounds = mmria_case.GetNumberField(p_value, "grams_or_pounds", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/grams_or_pounds");
		ounces = mmria_case.GetNumberField(p_value, "ounces", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/ounces");
	}
}

public sealed class _B2BBB7831DD41F0BF6176ACD23ADB38C : IConvertDictionary
{
	public _B2BBB7831DD41F0BF6176ACD23ADB38C()
	{
		birth_weight = new ();
		apgar_scores = new ();
	}
	public _564D05D8DE1347EFFC8CACE64166DFB3 birth_weight{ get;set;}
	public double? gender { get; set; }
	public _65228517D188454210BA20A6234601CB apgar_scores{ get;set;}
	public double? is_infant_living_at_time_of_report { get; set; }
	public double? is_infant_being_breastfed_at_discharge { get; set; }
	public double? was_infant_transferred_within_24_hours { get; set; }
	public string facility_city_state { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		birth_weight = mmria_case.GetGroupField<_564D05D8DE1347EFFC8CACE64166DFB3>(p_value, "birth_weight", "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight");
		gender = mmria_case.GetNumberListField(p_value, "gender", "birth_certificate_infant_fetal_section/biometrics_and_demographics/gender");
		apgar_scores = mmria_case.GetGroupField<_65228517D188454210BA20A6234601CB>(p_value, "apgar_scores", "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores");
		is_infant_living_at_time_of_report = mmria_case.GetNumberListField(p_value, "is_infant_living_at_time_of_report", "birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_living_at_time_of_report");
		is_infant_being_breastfed_at_discharge = mmria_case.GetNumberListField(p_value, "is_infant_being_breastfed_at_discharge", "birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_being_breastfed_at_discharge");
		was_infant_transferred_within_24_hours = mmria_case.GetNumberListField(p_value, "was_infant_transferred_within_24_hours", "birth_certificate_infant_fetal_section/biometrics_and_demographics/was_infant_transferred_within_24_hours");
		facility_city_state = mmria_case.GetStringField(p_value, "facility_city_state", "birth_certificate_infant_fetal_section/biometrics_and_demographics/facility_city_state");
	}
}

public sealed class _036EC801621DF61F061A328B5607A96A : IConvertDictionary
{
	public _036EC801621DF61F061A328B5607A96A()
	{
	}
	public string state_file_number { get; set; }
	public string local_file_number { get; set; }
	public string newborn_medical_record_number { get; set; }
	public DateOnly? date_of_delivery { get; set; }
	public TimeOnly? time_of_delivery { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		state_file_number = mmria_case.GetStringField(p_value, "state_file_number", "birth_certificate_infant_fetal_section/record_identification/state_file_number");
		local_file_number = mmria_case.GetStringField(p_value, "local_file_number", "birth_certificate_infant_fetal_section/record_identification/local_file_number");
		newborn_medical_record_number = mmria_case.GetStringField(p_value, "newborn_medical_record_number", "birth_certificate_infant_fetal_section/record_identification/newborn_medical_record_number");
		date_of_delivery = mmria_case.GetDateField(p_value, "date_of_delivery", "birth_certificate_infant_fetal_section/record_identification/date_of_delivery");
		time_of_delivery = mmria_case.GetTimeField(p_value, "time_of_delivery", "birth_certificate_infant_fetal_section/record_identification/time_of_delivery");
	}
}

		public sealed class _580C23C24054AB0BE30540A0BDCD16A0 : IConvertDictionary
		{
	public _580C23C24054AB0BE30540A0BDCD16A0()
{
		record_identification = new ();
		biometrics_and_demographics = new ();
		method_of_delivery = new ();
		abnormal_conditions_of_newborn = new ();
		congenital_anomalies = new ();
		causes_of_death = new ();
		vitals_import_group = new ();
	}
	public double? record_type { get; set; }
	public double? is_multiple_gestation { get; set; }
	public double? birth_order { get; set; }
	public _036EC801621DF61F061A328B5607A96A record_identification{ get;set;}
	public _B2BBB7831DD41F0BF6176ACD23ADB38C biometrics_and_demographics{ get;set;}
	public _EE7E3E53FEF286D9C356E5E2EFC17F2C method_of_delivery{ get;set;}
	public List<double> abnormal_conditions_of_newborn { get; set; }
	public List<double> congenital_anomalies { get; set; }
	public string icd_version { get; set; }
	public List<_D254CBE21CC45929794503CA76F3D904> causes_of_death{ get;set;}
	public string reviewer_note { get; set; }
	public _180D7E8BAC8D3315D498840F354A7694 vitals_import_group{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		record_type = mmria_case.GetNumberListField(p_value, "record_type", "birth_certificate_infant_fetal_section/record_type");
		is_multiple_gestation = mmria_case.GetNumberListField(p_value, "is_multiple_gestation", "birth_certificate_infant_fetal_section/is_multiple_gestation");
		birth_order = mmria_case.GetNumberField(p_value, "birth_order", "birth_certificate_infant_fetal_section/birth_order");
		record_identification = mmria_case.GetGroupField<_036EC801621DF61F061A328B5607A96A>(p_value, "record_identification", "birth_certificate_infant_fetal_section/record_identification");
		biometrics_and_demographics = mmria_case.GetGroupField<_B2BBB7831DD41F0BF6176ACD23ADB38C>(p_value, "biometrics_and_demographics", "birth_certificate_infant_fetal_section/biometrics_and_demographics");
		method_of_delivery = mmria_case.GetGroupField<_EE7E3E53FEF286D9C356E5E2EFC17F2C>(p_value, "method_of_delivery", "birth_certificate_infant_fetal_section/method_of_delivery");
		abnormal_conditions_of_newborn = mmria_case.GetMultiSelectNumberListField(p_value, "abnormal_conditions_of_newborn", "birth_certificate_infant_fetal_section/abnormal_conditions_of_newborn");
		congenital_anomalies = mmria_case.GetMultiSelectNumberListField(p_value, "congenital_anomalies", "birth_certificate_infant_fetal_section/congenital_anomalies");
		icd_version = mmria_case.GetStringField(p_value, "icd_version", "birth_certificate_infant_fetal_section/icd_version");
		causes_of_death = mmria_case.GetGridField<_D254CBE21CC45929794503CA76F3D904>(p_value, "causes_of_death", "birth_certificate_infant_fetal_section/causes_of_death");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "birth_certificate_infant_fetal_section/reviewer_note");
		vitals_import_group = mmria_case.GetGroupField<_180D7E8BAC8D3315D498840F354A7694>(p_value, "vitals_import_group", "birth_certificate_infant_fetal_section/vitals_import_group");
	}
}

public sealed class _729048DC80F0E6748B09405ACE96B47A : IConvertDictionary
{
	public _729048DC80F0E6748B09405ACE96B47A()
	{
		risk_factors_in_this_pregnancy = new ();
	}
	public List<double> risk_factors_in_this_pregnancy { get; set; }
	public double? number_of_c_sections { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		risk_factors_in_this_pregnancy = mmria_case.GetMultiSelectNumberListField(p_value, "risk_factors_in_this_pregnancy", "birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy");
		number_of_c_sections = mmria_case.GetNumberField(p_value, "number_of_c_sections", "birth_fetal_death_certificate_parent/risk_factors/number_of_c_sections");
	}
}

public sealed class _138CDA950E0762377AF4F350FAC6446E : IConvertDictionary
{
	public _138CDA950E0762377AF4F350FAC6446E()
	{
	}
	public double? prior_3_months { get; set; }
	public double? prior_3_months_type { get; set; }
	public double? trimester_1st { get; set; }
	public double? trimester_1st_type { get; set; }
	public double? trimester_2nd { get; set; }
	public double? trimester_2nd_type { get; set; }
	public double? trimester_3rd { get; set; }
	public double? trimester_3rd_type { get; set; }
	public double? none_or_not_specified { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		prior_3_months = mmria_case.GetNumberField(p_value, "prior_3_months", "birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months");
		prior_3_months_type = mmria_case.GetNumberListField(p_value, "prior_3_months_type", "birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months_type");
		trimester_1st = mmria_case.GetNumberField(p_value, "trimester_1st", "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st");
		trimester_1st_type = mmria_case.GetNumberListField(p_value, "trimester_1st_type", "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st_type");
		trimester_2nd = mmria_case.GetNumberField(p_value, "trimester_2nd", "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd");
		trimester_2nd_type = mmria_case.GetNumberListField(p_value, "trimester_2nd_type", "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd_type");
		trimester_3rd = mmria_case.GetNumberField(p_value, "trimester_3rd", "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd");
		trimester_3rd_type = mmria_case.GetNumberListField(p_value, "trimester_3rd_type", "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd_type");
		none_or_not_specified = mmria_case.GetNumberListField(p_value, "none_or_not_specified", "birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified");
	}
}

public sealed class _FF5F1AAE5D8E4D6B59BD2F0DE60CDC4F : IConvertDictionary
{
	public _FF5F1AAE5D8E4D6B59BD2F0DE60CDC4F()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/year");
	}
}

public sealed class _7047641B2EDBD576995FFE43AB716526 : IConvertDictionary
{
	public _7047641B2EDBD576995FFE43AB716526()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/year");
	}
}

public sealed class _BF73700031E60AB353EA4F6CE845BDB4 : IConvertDictionary
{
	public _BF73700031E60AB353EA4F6CE845BDB4()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year");
	}
}

public sealed class _F99ABFF90FBA0ED894B160D3B34256AD : IConvertDictionary
{
	public _F99ABFF90FBA0ED894B160D3B34256AD()
	{
		date_of_last_normal_menses = new ();
		date_of_1st_prenatal_visit = new ();
		date_of_last_prenatal_visit = new ();
	}
	public _BF73700031E60AB353EA4F6CE845BDB4 date_of_last_normal_menses{ get;set;}
	public _7047641B2EDBD576995FFE43AB716526 date_of_1st_prenatal_visit{ get;set;}
	public _FF5F1AAE5D8E4D6B59BD2F0DE60CDC4F date_of_last_prenatal_visit{ get;set;}
	public double? calculated_gestation { get; set; }
	public double? calculated_gestation_days { get; set; }
	public double? obsteric_estimate_of_gestation { get; set; }
	public double? plurality { get; set; }
	public string specify_if_greater_than_3 { get; set; }
	public double? was_wic_used { get; set; }
	public double? principal_source_of_payment_for_this_delivery { get; set; }
	public string specify_other_payor { get; set; }
	public double? trimester_of_1st_prenatal_care_visit { get; set; }
	public double? number_of_visits { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_last_normal_menses = mmria_case.GetGroupField<_BF73700031E60AB353EA4F6CE845BDB4>(p_value, "date_of_last_normal_menses", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses");
		date_of_1st_prenatal_visit = mmria_case.GetGroupField<_7047641B2EDBD576995FFE43AB716526>(p_value, "date_of_1st_prenatal_visit", "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit");
		date_of_last_prenatal_visit = mmria_case.GetGroupField<_FF5F1AAE5D8E4D6B59BD2F0DE60CDC4F>(p_value, "date_of_last_prenatal_visit", "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit");
		calculated_gestation = mmria_case.GetNumberField(p_value, "calculated_gestation", "birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation");
		calculated_gestation_days = mmria_case.GetNumberField(p_value, "calculated_gestation_days", "birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days");
		obsteric_estimate_of_gestation = mmria_case.GetNumberField(p_value, "obsteric_estimate_of_gestation", "birth_fetal_death_certificate_parent/prenatal_care/obsteric_estimate_of_gestation");
		plurality = mmria_case.GetNumberListField(p_value, "plurality", "birth_fetal_death_certificate_parent/prenatal_care/plurality");
		specify_if_greater_than_3 = mmria_case.GetStringField(p_value, "specify_if_greater_than_3", "birth_fetal_death_certificate_parent/prenatal_care/specify_if_greater_than_3");
		was_wic_used = mmria_case.GetNumberListField(p_value, "was_wic_used", "birth_fetal_death_certificate_parent/prenatal_care/was_wic_used");
		principal_source_of_payment_for_this_delivery = mmria_case.GetNumberListField(p_value, "principal_source_of_payment_for_this_delivery", "birth_fetal_death_certificate_parent/prenatal_care/principal_source_of_payment_for_this_delivery");
		specify_other_payor = mmria_case.GetStringField(p_value, "specify_other_payor", "birth_fetal_death_certificate_parent/prenatal_care/specify_other_payor");
		trimester_of_1st_prenatal_care_visit = mmria_case.GetNumberListField(p_value, "trimester_of_1st_prenatal_care_visit", "birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit");
		number_of_visits = mmria_case.GetNumberField(p_value, "number_of_visits", "birth_fetal_death_certificate_parent/prenatal_care/number_of_visits");
	}
}

public sealed class _9695E0E82516F4F139F21CEF9F275B1B : IConvertDictionary
{
	public _9695E0E82516F4F139F21CEF9F275B1B()
	{
	}
	public double? height_feet { get; set; }
	public double? height_inches { get; set; }
	public double? pre_pregnancy_weight { get; set; }
	public double? weight_at_delivery { get; set; }
	public double? weight_gain { get; set; }
	public double? bmi { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		height_feet = mmria_case.GetNumberField(p_value, "height_feet", "birth_fetal_death_certificate_parent/maternal_biometrics/height_feet");
		height_inches = mmria_case.GetNumberField(p_value, "height_inches", "birth_fetal_death_certificate_parent/maternal_biometrics/height_inches");
		pre_pregnancy_weight = mmria_case.GetNumberField(p_value, "pre_pregnancy_weight", "birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight");
		weight_at_delivery = mmria_case.GetNumberField(p_value, "weight_at_delivery", "birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery");
		weight_gain = mmria_case.GetNumberField(p_value, "weight_gain", "birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain");
		bmi = mmria_case.GetNumberField(p_value, "bmi", "birth_fetal_death_certificate_parent/maternal_biometrics/bmi");
	}
}

public sealed class _99B9ACB24E94944545611B1E9E9C6415 : IConvertDictionary
{
	public _99B9ACB24E94944545611B1E9E9C6415()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year");
	}
}

public sealed class _EE580ACBE00EC5F8FC589EDA2D706CE3 : IConvertDictionary
{
	public _EE580ACBE00EC5F8FC589EDA2D706CE3()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year");
	}
}

public sealed class _5572D24F3E250810200ACE846413E414 : IConvertDictionary
{
	public _5572D24F3E250810200ACE846413E414()
	{
		date_of_last_live_birth = new ();
		date_of_last_other_outcome = new ();
	}
	public _EE580ACBE00EC5F8FC589EDA2D706CE3 date_of_last_live_birth{ get;set;}
	public double? live_birth_interval { get; set; }
	public double? number_of_previous_live_births { get; set; }
	public double? now_living { get; set; }
	public double? now_dead { get; set; }
	public double? other_outcomes { get; set; }
	public _99B9ACB24E94944545611B1E9E9C6415 date_of_last_other_outcome{ get;set;}
	public string pregnancy_interval { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_last_live_birth = mmria_case.GetGroupField<_EE580ACBE00EC5F8FC589EDA2D706CE3>(p_value, "date_of_last_live_birth", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth");
		live_birth_interval = mmria_case.GetNumberField(p_value, "live_birth_interval", "birth_fetal_death_certificate_parent/pregnancy_history/live_birth_interval");
		number_of_previous_live_births = mmria_case.GetNumberField(p_value, "number_of_previous_live_births", "birth_fetal_death_certificate_parent/pregnancy_history/number_of_previous_live_births");
		now_living = mmria_case.GetNumberField(p_value, "now_living", "birth_fetal_death_certificate_parent/pregnancy_history/now_living");
		now_dead = mmria_case.GetNumberField(p_value, "now_dead", "birth_fetal_death_certificate_parent/pregnancy_history/now_dead");
		other_outcomes = mmria_case.GetNumberField(p_value, "other_outcomes", "birth_fetal_death_certificate_parent/pregnancy_history/other_outcomes");
		date_of_last_other_outcome = mmria_case.GetGroupField<_99B9ACB24E94944545611B1E9E9C6415>(p_value, "date_of_last_other_outcome", "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome");
		pregnancy_interval = mmria_case.GetStringField(p_value, "pregnancy_interval", "birth_fetal_death_certificate_parent/pregnancy_history/pregnancy_interval");
	}
}

public sealed class _A6F09668892BE9993511ADC113345A8B : IConvertDictionary
{
	public _A6F09668892BE9993511ADC113345A8B()
	{
		race_of_mother = new ();
	}
	public List<double> race_of_mother { get; set; }
	public string other_race { get; set; }
	public string other_asian { get; set; }
	public string other_pacific_islander { get; set; }
	public string principle_tribe { get; set; }
	public string omb_race_recode { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		race_of_mother = mmria_case.GetMultiSelectNumberListField(p_value, "race_of_mother", "birth_fetal_death_certificate_parent/race/race_of_mother");
		other_race = mmria_case.GetStringField(p_value, "other_race", "birth_fetal_death_certificate_parent/race/other_race");
		other_asian = mmria_case.GetStringField(p_value, "other_asian", "birth_fetal_death_certificate_parent/race/other_asian");
		other_pacific_islander = mmria_case.GetStringField(p_value, "other_pacific_islander", "birth_fetal_death_certificate_parent/race/other_pacific_islander");
		principle_tribe = mmria_case.GetStringField(p_value, "principle_tribe", "birth_fetal_death_certificate_parent/race/principle_tribe");
		omb_race_recode = mmria_case.GetStringListField(p_value, "omb_race_recode", "birth_fetal_death_certificate_parent/race/omb_race_recode");
	}
}

public sealed class _6F8603F4EEF2CB125B9224B073EBAF80 : IConvertDictionary
{
	public _6F8603F4EEF2CB125B9224B073EBAF80()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public double? estimated_distance_from_residence { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "birth_fetal_death_certificate_parent/location_of_residence/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "birth_fetal_death_certificate_parent/location_of_residence/apartment");
		city = mmria_case.GetStringField(p_value, "city", "birth_fetal_death_certificate_parent/location_of_residence/city");
		state = mmria_case.GetStringListField(p_value, "state", "birth_fetal_death_certificate_parent/location_of_residence/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "birth_fetal_death_certificate_parent/location_of_residence/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "birth_fetal_death_certificate_parent/location_of_residence/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "birth_fetal_death_certificate_parent/location_of_residence/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "birth_fetal_death_certificate_parent/location_of_residence/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "birth_fetal_death_certificate_parent/location_of_residence/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "birth_fetal_death_certificate_parent/location_of_residence/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "birth_fetal_death_certificate_parent/location_of_residence/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "birth_fetal_death_certificate_parent/location_of_residence/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "birth_fetal_death_certificate_parent/location_of_residence/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "birth_fetal_death_certificate_parent/location_of_residence/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "birth_fetal_death_certificate_parent/location_of_residence/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_micro");
		estimated_distance_from_residence = mmria_case.GetNumberField(p_value, "estimated_distance_from_residence", "birth_fetal_death_certificate_parent/location_of_residence/estimated_distance_from_residence");
	}
}

public sealed class _7F7317DE738C9AD7FCEF25EFC45B48EF : IConvertDictionary
{
	public _7F7317DE738C9AD7FCEF25EFC45B48EF()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year");
	}
}

public sealed class _2F6924AE0E06FCE968631ECF2E8FD06A : IConvertDictionary
{
	public _2F6924AE0E06FCE968631ECF2E8FD06A()
	{
		date_of_birth = new ();
	}
	public _7F7317DE738C9AD7FCEF25EFC45B48EF date_of_birth{ get;set;}
	public double? age { get; set; }
	public double? mother_married { get; set; }
	public double? If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital { get; set; }
	public string city_of_birth { get; set; }
	public string state_of_birth { get; set; }
	public string country_of_birth { get; set; }
	public string primary_occupation { get; set; }
	public string occupation_business_industry { get; set; }
	public double? ever_in_us_armed_forces { get; set; }
	public double? is_of_hispanic_origin { get; set; }
	public string is_of_hispanic_origin_other_specify { get; set; }
	public double? education_level { get; set; }
	public string bcdcp_m_industry_code_1 { get; set; }
	public string bcdcp_m_industry_code_2 { get; set; }
	public string bcdcp_m_industry_code_3 { get; set; }
	public string bcdcp_m_occupation_code_1 { get; set; }
	public string bcdcp_m_occupation_code_2 { get; set; }
	public string bcdcp_m_occupation_code_3 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_birth = mmria_case.GetGroupField<_7F7317DE738C9AD7FCEF25EFC45B48EF>(p_value, "date_of_birth", "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth");
		age = mmria_case.GetNumberField(p_value, "age", "birth_fetal_death_certificate_parent/demographic_of_mother/age");
		mother_married = mmria_case.GetNumberListField(p_value, "mother_married", "birth_fetal_death_certificate_parent/demographic_of_mother/mother_married");
		If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital = mmria_case.GetNumberListField(p_value, "If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital", "birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital");
		city_of_birth = mmria_case.GetStringField(p_value, "city_of_birth", "birth_fetal_death_certificate_parent/demographic_of_mother/city_of_birth");
		state_of_birth = mmria_case.GetStringListField(p_value, "state_of_birth", "birth_fetal_death_certificate_parent/demographic_of_mother/state_of_birth");
		country_of_birth = mmria_case.GetStringListField(p_value, "country_of_birth", "birth_fetal_death_certificate_parent/demographic_of_mother/country_of_birth");
		primary_occupation = mmria_case.GetStringField(p_value, "primary_occupation", "birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation");
		occupation_business_industry = mmria_case.GetStringField(p_value, "occupation_business_industry", "birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry");
		ever_in_us_armed_forces = mmria_case.GetNumberListField(p_value, "ever_in_us_armed_forces", "birth_fetal_death_certificate_parent/demographic_of_mother/ever_in_us_armed_forces");
		is_of_hispanic_origin = mmria_case.GetNumberListField(p_value, "is_of_hispanic_origin", "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin");
		is_of_hispanic_origin_other_specify = mmria_case.GetStringField(p_value, "is_of_hispanic_origin_other_specify", "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin_other_specify");
		education_level = mmria_case.GetNumberListField(p_value, "education_level", "birth_fetal_death_certificate_parent/demographic_of_mother/education_level");
		bcdcp_m_industry_code_1 = mmria_case.GetHiddenField(p_value, "bcdcp_m_industry_code_1", "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_1");
		bcdcp_m_industry_code_2 = mmria_case.GetHiddenField(p_value, "bcdcp_m_industry_code_2", "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_2");
		bcdcp_m_industry_code_3 = mmria_case.GetHiddenField(p_value, "bcdcp_m_industry_code_3", "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_3");
		bcdcp_m_occupation_code_1 = mmria_case.GetHiddenField(p_value, "bcdcp_m_occupation_code_1", "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_1");
		bcdcp_m_occupation_code_2 = mmria_case.GetHiddenField(p_value, "bcdcp_m_occupation_code_2", "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_2");
		bcdcp_m_occupation_code_3 = mmria_case.GetHiddenField(p_value, "bcdcp_m_occupation_code_3", "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_3");
	}
}

public sealed class _92AF48F0C3E500B292CF57F4A5A0FFC7 : IConvertDictionary
{
	public _92AF48F0C3E500B292CF57F4A5A0FFC7()
	{
	}
	public string first_name { get; set; }
	public string middle_name { get; set; }
	public string last_name { get; set; }
	public string maiden_name { get; set; }
	public string medical_record_number { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		first_name = mmria_case.GetStringField(p_value, "first_name", "birth_fetal_death_certificate_parent/record_identification/first_name");
		middle_name = mmria_case.GetStringField(p_value, "middle_name", "birth_fetal_death_certificate_parent/record_identification/middle_name");
		last_name = mmria_case.GetStringField(p_value, "last_name", "birth_fetal_death_certificate_parent/record_identification/last_name");
		maiden_name = mmria_case.GetStringField(p_value, "maiden_name", "birth_fetal_death_certificate_parent/record_identification/maiden_name");
		medical_record_number = mmria_case.GetStringField(p_value, "medical_record_number", "birth_fetal_death_certificate_parent/record_identification/medical_record_number");
	}
}

public sealed class _D06302063800C30E582D8D21EB802481 : IConvertDictionary
{
	public _D06302063800C30E582D8D21EB802481()
	{
		race_of_father = new ();
	}
	public List<double> race_of_father { get; set; }
	public string other_race { get; set; }
	public string other_asian { get; set; }
	public string other_pacific_islander { get; set; }
	public string principle_tribe { get; set; }
	public string omb_race_recode { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		race_of_father = mmria_case.GetMultiSelectNumberListField(p_value, "race_of_father", "birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father");
		other_race = mmria_case.GetStringField(p_value, "other_race", "birth_fetal_death_certificate_parent/demographic_of_father/race/other_race");
		other_asian = mmria_case.GetStringField(p_value, "other_asian", "birth_fetal_death_certificate_parent/demographic_of_father/race/other_asian");
		other_pacific_islander = mmria_case.GetStringField(p_value, "other_pacific_islander", "birth_fetal_death_certificate_parent/demographic_of_father/race/other_pacific_islander");
		principle_tribe = mmria_case.GetStringField(p_value, "principle_tribe", "birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe");
		omb_race_recode = mmria_case.GetStringListField(p_value, "omb_race_recode", "birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode");
	}
}

public sealed class _69209A9AA276ABCD41A7FAD0F3E7F1F4 : IConvertDictionary
{
	public _69209A9AA276ABCD41A7FAD0F3E7F1F4()
	{
	}
	public double? month { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/month");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/year");
	}
}

public sealed class _C8EB8776C7C93B639B2CC9A7E8F29017 : IConvertDictionary
{
	public _C8EB8776C7C93B639B2CC9A7E8F29017()
	{
		date_of_birth = new ();
		race = new ();
	}
	public _69209A9AA276ABCD41A7FAD0F3E7F1F4 date_of_birth{ get;set;}
	public double? age { get; set; }
	public double? education_level { get; set; }
	public string city_of_birth { get; set; }
	public string state_of_birth { get; set; }
	public string father_country_of_birth { get; set; }
	public string primary_occupation { get; set; }
	public string occupation_business_industry { get; set; }
	public double? is_father_of_hispanic_origin { get; set; }
	public string is_father_of_hispanic_origin_other_specify { get; set; }
	public _D06302063800C30E582D8D21EB802481 race{ get;set;}
	public string bcdcp_f_industry_code_1 { get; set; }
	public string bcdcp_f_industry_code_2 { get; set; }
	public string bcdcp_f_industry_code_3 { get; set; }
	public string bcdcp_f_occupation_code_1 { get; set; }
	public string bcdcp_f_occupation_code_2 { get; set; }
	public string bcdcp_f_occupation_code_3 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_birth = mmria_case.GetGroupField<_69209A9AA276ABCD41A7FAD0F3E7F1F4>(p_value, "date_of_birth", "birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth");
		age = mmria_case.GetNumberField(p_value, "age", "birth_fetal_death_certificate_parent/demographic_of_father/age");
		education_level = mmria_case.GetNumberListField(p_value, "education_level", "birth_fetal_death_certificate_parent/demographic_of_father/education_level");
		city_of_birth = mmria_case.GetStringField(p_value, "city_of_birth", "birth_fetal_death_certificate_parent/demographic_of_father/city_of_birth");
		state_of_birth = mmria_case.GetStringListField(p_value, "state_of_birth", "birth_fetal_death_certificate_parent/demographic_of_father/state_of_birth");
		father_country_of_birth = mmria_case.GetStringListField(p_value, "father_country_of_birth", "birth_fetal_death_certificate_parent/demographic_of_father/father_country_of_birth");
		primary_occupation = mmria_case.GetStringField(p_value, "primary_occupation", "birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation");
		occupation_business_industry = mmria_case.GetStringField(p_value, "occupation_business_industry", "birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry");
		is_father_of_hispanic_origin = mmria_case.GetNumberListField(p_value, "is_father_of_hispanic_origin", "birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin");
		is_father_of_hispanic_origin_other_specify = mmria_case.GetStringField(p_value, "is_father_of_hispanic_origin_other_specify", "birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin_other_specify");
		race = mmria_case.GetGroupField<_D06302063800C30E582D8D21EB802481>(p_value, "race", "birth_fetal_death_certificate_parent/demographic_of_father/race");
		bcdcp_f_industry_code_1 = mmria_case.GetHiddenField(p_value, "bcdcp_f_industry_code_1", "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_1");
		bcdcp_f_industry_code_2 = mmria_case.GetHiddenField(p_value, "bcdcp_f_industry_code_2", "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_2");
		bcdcp_f_industry_code_3 = mmria_case.GetHiddenField(p_value, "bcdcp_f_industry_code_3", "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_3");
		bcdcp_f_occupation_code_1 = mmria_case.GetHiddenField(p_value, "bcdcp_f_occupation_code_1", "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_1");
		bcdcp_f_occupation_code_2 = mmria_case.GetHiddenField(p_value, "bcdcp_f_occupation_code_2", "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_2");
		bcdcp_f_occupation_code_3 = mmria_case.GetHiddenField(p_value, "bcdcp_f_occupation_code_3", "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_3");
	}
}

public sealed class _19AA41C28005BECF3173B505FC11D868 : IConvertDictionary
{
	public _19AA41C28005BECF3173B505FC11D868()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string urban_status { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "birth_fetal_death_certificate_parent/facility_of_delivery_location/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "birth_fetal_death_certificate_parent/facility_of_delivery_location/apartment");
		city = mmria_case.GetStringField(p_value, "city", "birth_fetal_death_certificate_parent/facility_of_delivery_location/city");
		state = mmria_case.GetStringListField(p_value, "state", "birth_fetal_death_certificate_parent/facility_of_delivery_location/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "birth_fetal_death_certificate_parent/facility_of_delivery_location/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "birth_fetal_death_certificate_parent/facility_of_delivery_location/county");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "birth_fetal_death_certificate_parent/facility_of_delivery_location/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "birth_fetal_death_certificate_parent/facility_of_delivery_location/longitude");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "birth_fetal_death_certificate_parent/facility_of_delivery_location/feature_matching_geography_type");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_type");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "birth_fetal_death_certificate_parent/facility_of_delivery_location/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_tract_fips");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_micro");
	}
}

public sealed class _074CF5662C2CC629E97364D0748249E3 : IConvertDictionary
{
	public _074CF5662C2CC629E97364D0748249E3()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month");
		day = mmria_case.GetNumberListField(p_value, "day", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day");
		year = mmria_case.GetNumberListField(p_value, "year", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year");
	}
}

public sealed class _A307CA5839D318B971B8B5A4CD130A43 : IConvertDictionary
{
	public _A307CA5839D318B971B8B5A4CD130A43()
	{
		date_of_delivery = new ();
	}
	public _074CF5662C2CC629E97364D0748249E3 date_of_delivery{ get;set;}
	public double? type_of_place { get; set; }
	public double? was_home_delivery_planned { get; set; }
	public double? maternal_level_of_care { get; set; }
	public string other_maternal_level_of_care { get; set; }
	public string facility_npi_number { get; set; }
	public string facility_name { get; set; }
	public double? attendant_type { get; set; }
	public string other_attendant_type { get; set; }
	public string attendant_npi { get; set; }
	public double? was_mother_transferred { get; set; }
	public string transferred_from_where { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_delivery = mmria_case.GetGroupField<_074CF5662C2CC629E97364D0748249E3>(p_value, "date_of_delivery", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery");
		type_of_place = mmria_case.GetNumberListField(p_value, "type_of_place", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/type_of_place");
		was_home_delivery_planned = mmria_case.GetNumberListField(p_value, "was_home_delivery_planned", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_home_delivery_planned");
		maternal_level_of_care = mmria_case.GetNumberListField(p_value, "maternal_level_of_care", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/maternal_level_of_care");
		other_maternal_level_of_care = mmria_case.GetStringField(p_value, "other_maternal_level_of_care", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_maternal_level_of_care");
		facility_npi_number = mmria_case.GetStringField(p_value, "facility_npi_number", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_npi_number");
		facility_name = mmria_case.GetStringField(p_value, "facility_name", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name");
		attendant_type = mmria_case.GetNumberListField(p_value, "attendant_type", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_type");
		other_attendant_type = mmria_case.GetStringField(p_value, "other_attendant_type", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_attendant_type");
		attendant_npi = mmria_case.GetStringField(p_value, "attendant_npi", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_npi");
		was_mother_transferred = mmria_case.GetNumberListField(p_value, "was_mother_transferred", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_mother_transferred");
		transferred_from_where = mmria_case.GetStringField(p_value, "transferred_from_where", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/transferred_from_where");
	}
}

public sealed class _1757340A93C7CC802C810229D906E417 : IConvertDictionary
{
	public _1757340A93C7CC802C810229D906E417()
	{
		facility_of_delivery_demographics = new ();
		facility_of_delivery_location = new ();
		demographic_of_father = new ();
		record_identification = new ();
		demographic_of_mother = new ();
		location_of_residence = new ();
		race = new ();
		pregnancy_history = new ();
		maternal_biometrics = new ();
		prenatal_care = new ();
		cigarette_smoking = new ();
		risk_factors = new ();
		infections_present_or_treated_during_pregnancy = new ();
		onset_of_labor = new ();
		obstetric_procedures = new ();
		characteristics_of_labor_and_delivery = new ();
		maternal_morbidity = new ();
	}
	public _A307CA5839D318B971B8B5A4CD130A43 facility_of_delivery_demographics{ get;set;}
	public _19AA41C28005BECF3173B505FC11D868 facility_of_delivery_location{ get;set;}
	public _C8EB8776C7C93B639B2CC9A7E8F29017 demographic_of_father{ get;set;}
	public _92AF48F0C3E500B292CF57F4A5A0FFC7 record_identification{ get;set;}
	public _2F6924AE0E06FCE968631ECF2E8FD06A demographic_of_mother{ get;set;}
	public _6F8603F4EEF2CB125B9224B073EBAF80 location_of_residence{ get;set;}
	public _A6F09668892BE9993511ADC113345A8B race{ get;set;}
	public _5572D24F3E250810200ACE846413E414 pregnancy_history{ get;set;}
	public _9695E0E82516F4F139F21CEF9F275B1B maternal_biometrics{ get;set;}
	public _F99ABFF90FBA0ED894B160D3B34256AD prenatal_care{ get;set;}
	public _138CDA950E0762377AF4F350FAC6446E cigarette_smoking{ get;set;}
	public _729048DC80F0E6748B09405ACE96B47A risk_factors{ get;set;}
	public List<double> infections_present_or_treated_during_pregnancy { get; set; }
	public string specify_other_infection { get; set; }
	public List<double> onset_of_labor { get; set; }
	public List<double> obstetric_procedures { get; set; }
	public List<double> characteristics_of_labor_and_delivery { get; set; }
	public List<double> maternal_morbidity { get; set; }
	public double? length_between_child_birth_and_death_of_mother { get; set; }
	public string reviewer_note { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		facility_of_delivery_demographics = mmria_case.GetGroupField<_A307CA5839D318B971B8B5A4CD130A43>(p_value, "facility_of_delivery_demographics", "birth_fetal_death_certificate_parent/facility_of_delivery_demographics");
		facility_of_delivery_location = mmria_case.GetGroupField<_19AA41C28005BECF3173B505FC11D868>(p_value, "facility_of_delivery_location", "birth_fetal_death_certificate_parent/facility_of_delivery_location");
		demographic_of_father = mmria_case.GetGroupField<_C8EB8776C7C93B639B2CC9A7E8F29017>(p_value, "demographic_of_father", "birth_fetal_death_certificate_parent/demographic_of_father");
		record_identification = mmria_case.GetGroupField<_92AF48F0C3E500B292CF57F4A5A0FFC7>(p_value, "record_identification", "birth_fetal_death_certificate_parent/record_identification");
		demographic_of_mother = mmria_case.GetGroupField<_2F6924AE0E06FCE968631ECF2E8FD06A>(p_value, "demographic_of_mother", "birth_fetal_death_certificate_parent/demographic_of_mother");
		location_of_residence = mmria_case.GetGroupField<_6F8603F4EEF2CB125B9224B073EBAF80>(p_value, "location_of_residence", "birth_fetal_death_certificate_parent/location_of_residence");
		race = mmria_case.GetGroupField<_A6F09668892BE9993511ADC113345A8B>(p_value, "race", "birth_fetal_death_certificate_parent/race");
		pregnancy_history = mmria_case.GetGroupField<_5572D24F3E250810200ACE846413E414>(p_value, "pregnancy_history", "birth_fetal_death_certificate_parent/pregnancy_history");
		maternal_biometrics = mmria_case.GetGroupField<_9695E0E82516F4F139F21CEF9F275B1B>(p_value, "maternal_biometrics", "birth_fetal_death_certificate_parent/maternal_biometrics");
		prenatal_care = mmria_case.GetGroupField<_F99ABFF90FBA0ED894B160D3B34256AD>(p_value, "prenatal_care", "birth_fetal_death_certificate_parent/prenatal_care");
		cigarette_smoking = mmria_case.GetGroupField<_138CDA950E0762377AF4F350FAC6446E>(p_value, "cigarette_smoking", "birth_fetal_death_certificate_parent/cigarette_smoking");
		risk_factors = mmria_case.GetGroupField<_729048DC80F0E6748B09405ACE96B47A>(p_value, "risk_factors", "birth_fetal_death_certificate_parent/risk_factors");
		infections_present_or_treated_during_pregnancy = mmria_case.GetMultiSelectNumberListField(p_value, "infections_present_or_treated_during_pregnancy", "birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy");
		specify_other_infection = mmria_case.GetStringField(p_value, "specify_other_infection", "birth_fetal_death_certificate_parent/specify_other_infection");
		onset_of_labor = mmria_case.GetMultiSelectNumberListField(p_value, "onset_of_labor", "birth_fetal_death_certificate_parent/onset_of_labor");
		obstetric_procedures = mmria_case.GetMultiSelectNumberListField(p_value, "obstetric_procedures", "birth_fetal_death_certificate_parent/obstetric_procedures");
		characteristics_of_labor_and_delivery = mmria_case.GetMultiSelectNumberListField(p_value, "characteristics_of_labor_and_delivery", "birth_fetal_death_certificate_parent/characteristics_of_labor_and_delivery");
		maternal_morbidity = mmria_case.GetMultiSelectNumberListField(p_value, "maternal_morbidity", "birth_fetal_death_certificate_parent/maternal_morbidity");
		length_between_child_birth_and_death_of_mother = mmria_case.GetNumberField(p_value, "length_between_child_birth_and_death_of_mother", "birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "birth_fetal_death_certificate_parent/reviewer_note");
	}
}

public sealed class _F01C82803721A4C8A0DB077B9E1138E4 : IConvertDictionary
{
	public _F01C82803721A4C8A0DB077B9E1138E4()
	{
	}
	public string vital_summary_text { get; set; }
	public string cod1a { get; set; }
	public string interval1a { get; set; }
	public string cod1b { get; set; }
	public string interval1b { get; set; }
	public string cod1c { get; set; }
	public string interval1c { get; set; }
	public string cod1d { get; set; }
	public string interfval1d { get; set; }
	public string othercondition { get; set; }
	public string man_uc { get; set; }
	public string acme_uc { get; set; }
	public string eac { get; set; }
	public string rac { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		vital_summary_text = mmria_case.GetTextAreaField(p_value, "vital_summary_text", "death_certificate/vitals_import_group/vital_summary_text");
		cod1a = mmria_case.GetStringField(p_value, "cod1a", "death_certificate/vitals_import_group/cod1a");
		interval1a = mmria_case.GetStringField(p_value, "interval1a", "death_certificate/vitals_import_group/interval1a");
		cod1b = mmria_case.GetStringField(p_value, "cod1b", "death_certificate/vitals_import_group/cod1b");
		interval1b = mmria_case.GetStringField(p_value, "interval1b", "death_certificate/vitals_import_group/interval1b");
		cod1c = mmria_case.GetStringField(p_value, "cod1c", "death_certificate/vitals_import_group/cod1c");
		interval1c = mmria_case.GetStringField(p_value, "interval1c", "death_certificate/vitals_import_group/interval1c");
		cod1d = mmria_case.GetStringField(p_value, "cod1d", "death_certificate/vitals_import_group/cod1d");
		interfval1d = mmria_case.GetStringField(p_value, "interfval1d", "death_certificate/vitals_import_group/interfval1d");
		othercondition = mmria_case.GetStringField(p_value, "othercondition", "death_certificate/vitals_import_group/othercondition");
		man_uc = mmria_case.GetStringField(p_value, "man_uc", "death_certificate/vitals_import_group/man_uc");
		acme_uc = mmria_case.GetStringField(p_value, "acme_uc", "death_certificate/vitals_import_group/acme_uc");
		eac = mmria_case.GetStringField(p_value, "eac", "death_certificate/vitals_import_group/eac");
		rac = mmria_case.GetStringField(p_value, "rac", "death_certificate/vitals_import_group/rac");
	}
}

public sealed class _194FE6F9039297ED012FE605A81755D2 : IConvertDictionary
{
	public _194FE6F9039297ED012FE605A81755D2()
	{
	}
	public double? cause_type { get; set; }
	public string cause_descriptive { get; set; }
	public string icd_code { get; set; }
	public string interval { get; set; }
	public double? interval_unit { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		cause_type = mmria_case.GetNumberListField(p_value, "cause_type", "death_certificate/causes_of_death/cause_type");
		cause_descriptive = mmria_case.GetStringField(p_value, "cause_descriptive", "death_certificate/causes_of_death/cause_descriptive");
		icd_code = mmria_case.GetStringField(p_value, "icd_code", "death_certificate/causes_of_death/icd_code");
		interval = mmria_case.GetStringField(p_value, "interval", "death_certificate/causes_of_death/interval");
		interval_unit = mmria_case.GetNumberListField(p_value, "interval_unit", "death_certificate/causes_of_death/interval_unit");
	}
}

public sealed class _F8873E911BE747DC2E249ACC7FC0ECC9 : IConvertDictionary
{
	public _F8873E911BE747DC2E249ACC7FC0ECC9()
	{
	}
	public string place_of_death { get; set; }
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public double? estimated_death_distance_from_residence { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		place_of_death = mmria_case.GetStringField(p_value, "place_of_death", "death_certificate/address_of_death/place_of_death");
		street = mmria_case.GetStringField(p_value, "street", "death_certificate/address_of_death/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "death_certificate/address_of_death/apartment");
		city = mmria_case.GetStringField(p_value, "city", "death_certificate/address_of_death/city");
		state = mmria_case.GetStringListField(p_value, "state", "death_certificate/address_of_death/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "death_certificate/address_of_death/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "death_certificate/address_of_death/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "death_certificate/address_of_death/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "death_certificate/address_of_death/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "death_certificate/address_of_death/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "death_certificate/address_of_death/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "death_certificate/address_of_death/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "death_certificate/address_of_death/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "death_certificate/address_of_death/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "death_certificate/address_of_death/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "death_certificate/address_of_death/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "death_certificate/address_of_death/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "death_certificate/address_of_death/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "death_certificate/address_of_death/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "death_certificate/address_of_death/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "death_certificate/address_of_death/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "death_certificate/address_of_death/census_cbsa_micro");
		estimated_death_distance_from_residence = mmria_case.GetNumberField(p_value, "estimated_death_distance_from_residence", "death_certificate/address_of_death/estimated_death_distance_from_residence");
	}
}

public sealed class _079B7F4BEB356992B2DD1A9449392DE8 : IConvertDictionary
{
	public _079B7F4BEB356992B2DD1A9449392DE8()
	{
	}
	public double? death_occured_in_hospital { get; set; }
	public double? death_outside_of_hospital { get; set; }
	public string other_death_outside_of_hospital { get; set; }
	public double? manner_of_death { get; set; }
	public double? was_autopsy_performed { get; set; }
	public double? was_autopsy_used_for_death_coding { get; set; }
	public double? pregnancy_status { get; set; }
	public double? did_tobacco_contribute_to_death { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		death_occured_in_hospital = mmria_case.GetNumberListField(p_value, "death_occured_in_hospital", "death_certificate/death_information/death_occured_in_hospital");
		death_outside_of_hospital = mmria_case.GetNumberListField(p_value, "death_outside_of_hospital", "death_certificate/death_information/death_outside_of_hospital");
		other_death_outside_of_hospital = mmria_case.GetStringField(p_value, "other_death_outside_of_hospital", "death_certificate/death_information/other_death_outside_of_hospital");
		manner_of_death = mmria_case.GetNumberListField(p_value, "manner_of_death", "death_certificate/death_information/manner_of_death");
		was_autopsy_performed = mmria_case.GetNumberListField(p_value, "was_autopsy_performed", "death_certificate/death_information/was_autopsy_performed");
		was_autopsy_used_for_death_coding = mmria_case.GetNumberListField(p_value, "was_autopsy_used_for_death_coding", "death_certificate/death_information/was_autopsy_used_for_death_coding");
		pregnancy_status = mmria_case.GetNumberListField(p_value, "pregnancy_status", "death_certificate/death_information/pregnancy_status");
		did_tobacco_contribute_to_death = mmria_case.GetNumberListField(p_value, "did_tobacco_contribute_to_death", "death_certificate/death_information/did_tobacco_contribute_to_death");
	}
}

public sealed class _C4B6CBD761F83E9017FA96D0C02BB6BE : IConvertDictionary
{
	public _C4B6CBD761F83E9017FA96D0C02BB6BE()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "death_certificate/address_of_injury/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "death_certificate/address_of_injury/apartment");
		city = mmria_case.GetStringField(p_value, "city", "death_certificate/address_of_injury/city");
		state = mmria_case.GetStringListField(p_value, "state", "death_certificate/address_of_injury/state");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "death_certificate/address_of_injury/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "death_certificate/address_of_injury/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "death_certificate/address_of_injury/feature_matching_geography_type");
		latitude = mmria_case.GetHiddenField(p_value, "latitude", "death_certificate/address_of_injury/latitude");
		longitude = mmria_case.GetHiddenField(p_value, "longitude", "death_certificate/address_of_injury/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "death_certificate/address_of_injury/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "death_certificate/address_of_injury/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "death_certificate/address_of_injury/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "death_certificate/address_of_injury/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "death_certificate/address_of_injury/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "death_certificate/address_of_injury/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "death_certificate/address_of_injury/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "death_certificate/address_of_injury/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "death_certificate/address_of_injury/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "death_certificate/address_of_injury/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "death_certificate/address_of_injury/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "death_certificate/address_of_injury/census_cbsa_micro");
	}
}

public sealed class _B29F65CCE15C783D65EDB21A820303C6 : IConvertDictionary
{
	public _B29F65CCE15C783D65EDB21A820303C6()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "death_certificate/injury_associated_information/date_of_injury/month");
		day = mmria_case.GetNumberListField(p_value, "day", "death_certificate/injury_associated_information/date_of_injury/day");
		year = mmria_case.GetNumberListField(p_value, "year", "death_certificate/injury_associated_information/date_of_injury/year");
	}
}

public sealed class _E238EE0EC013BC544EA03FABE426DF45 : IConvertDictionary
{
	public _E238EE0EC013BC544EA03FABE426DF45()
	{
		date_of_injury = new ();
	}
	public _B29F65CCE15C783D65EDB21A820303C6 date_of_injury{ get;set;}
	public TimeOnly? time_of_injury { get; set; }
	public string place_of_injury { get; set; }
	public double? was_injury_at_work { get; set; }
	public double? transportation_related_injury { get; set; }
	public string transport_related_other_specify { get; set; }
	public double? were_seat_belts_in_use { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_injury = mmria_case.GetGroupField<_B29F65CCE15C783D65EDB21A820303C6>(p_value, "date_of_injury", "death_certificate/injury_associated_information/date_of_injury");
		time_of_injury = mmria_case.GetTimeField(p_value, "time_of_injury", "death_certificate/injury_associated_information/time_of_injury");
		place_of_injury = mmria_case.GetStringField(p_value, "place_of_injury", "death_certificate/injury_associated_information/place_of_injury");
		was_injury_at_work = mmria_case.GetNumberListField(p_value, "was_injury_at_work", "death_certificate/injury_associated_information/was_injury_at_work");
		transportation_related_injury = mmria_case.GetNumberListField(p_value, "transportation_related_injury", "death_certificate/injury_associated_information/transportation_related_injury");
		transport_related_other_specify = mmria_case.GetStringField(p_value, "transport_related_other_specify", "death_certificate/injury_associated_information/transport_related_other_specify");
		were_seat_belts_in_use = mmria_case.GetNumberListField(p_value, "were_seat_belts_in_use", "death_certificate/injury_associated_information/were_seat_belts_in_use");
	}
}

public sealed class _4DFB05F38E9F2A6773B8FFF545D24AA2 : IConvertDictionary
{
	public _4DFB05F38E9F2A6773B8FFF545D24AA2()
	{
		race = new ();
	}
	public List<double> race { get; set; }
	public string other_race { get; set; }
	public string other_asian { get; set; }
	public string other_pacific_islander { get; set; }
	public string principle_tribe { get; set; }
	public double? omb_race_recode { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		race = mmria_case.GetMultiSelectNumberListField(p_value, "race", "death_certificate/race/race");
		other_race = mmria_case.GetStringField(p_value, "other_race", "death_certificate/race/other_race");
		other_asian = mmria_case.GetStringField(p_value, "other_asian", "death_certificate/race/other_asian");
		other_pacific_islander = mmria_case.GetStringField(p_value, "other_pacific_islander", "death_certificate/race/other_pacific_islander");
		principle_tribe = mmria_case.GetStringField(p_value, "principle_tribe", "death_certificate/race/principle_tribe");
		omb_race_recode = mmria_case.GetNumberListField(p_value, "omb_race_recode", "death_certificate/race/omb_race_recode");
	}
}

public sealed class _F9220E33675619A7A5453C904567BE59 : IConvertDictionary
{
	public _F9220E33675619A7A5453C904567BE59()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "death_certificate/demographics/date_of_birth/month");
		day = mmria_case.GetNumberListField(p_value, "day", "death_certificate/demographics/date_of_birth/day");
		year = mmria_case.GetNumberListField(p_value, "year", "death_certificate/demographics/date_of_birth/year");
	}
}

public sealed class _72A993E1B915072396CD06F798020CEF : IConvertDictionary
{
	public _72A993E1B915072396CD06F798020CEF()
	{
		date_of_birth = new ();
	}
	public _F9220E33675619A7A5453C904567BE59 date_of_birth{ get;set;}
	public double? age { get; set; }
	public double? age_on_death_certificate { get; set; }
	public double? marital_status { get; set; }
	public string city_of_birth { get; set; }
	public string state_of_birth { get; set; }
	public string country_of_birth { get; set; }
	public string primary_occupation { get; set; }
	public string occupation_business_industry { get; set; }
	public double? ever_in_us_armed_forces { get; set; }
	public double? is_of_hispanic_origin { get; set; }
	public string is_of_hispanic_origin_other_specify { get; set; }
	public double? education_level { get; set; }
	public string dc_m_industry_code_1 { get; set; }
	public string dc_m_industry_code_2 { get; set; }
	public string dc_m_industry_code_3 { get; set; }
	public string dc_m_occupation_code_1 { get; set; }
	public string dc_m_occupation_code_2 { get; set; }
	public string dc_m_occupation_code_3 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		date_of_birth = mmria_case.GetGroupField<_F9220E33675619A7A5453C904567BE59>(p_value, "date_of_birth", "death_certificate/demographics/date_of_birth");
		age = mmria_case.GetNumberField(p_value, "age", "death_certificate/demographics/age");
		age_on_death_certificate = mmria_case.GetNumberField(p_value, "age_on_death_certificate", "death_certificate/demographics/age_on_death_certificate");
		marital_status = mmria_case.GetNumberListField(p_value, "marital_status", "death_certificate/demographics/marital_status");
		city_of_birth = mmria_case.GetStringField(p_value, "city_of_birth", "death_certificate/demographics/city_of_birth");
		state_of_birth = mmria_case.GetStringListField(p_value, "state_of_birth", "death_certificate/demographics/state_of_birth");
		country_of_birth = mmria_case.GetStringListField(p_value, "country_of_birth", "death_certificate/demographics/country_of_birth");
		primary_occupation = mmria_case.GetStringField(p_value, "primary_occupation", "death_certificate/demographics/primary_occupation");
		occupation_business_industry = mmria_case.GetStringField(p_value, "occupation_business_industry", "death_certificate/demographics/occupation_business_industry");
		ever_in_us_armed_forces = mmria_case.GetNumberListField(p_value, "ever_in_us_armed_forces", "death_certificate/demographics/ever_in_us_armed_forces");
		is_of_hispanic_origin = mmria_case.GetNumberListField(p_value, "is_of_hispanic_origin", "death_certificate/demographics/is_of_hispanic_origin");
		is_of_hispanic_origin_other_specify = mmria_case.GetStringField(p_value, "is_of_hispanic_origin_other_specify", "death_certificate/demographics/is_of_hispanic_origin_other_specify");
		education_level = mmria_case.GetNumberListField(p_value, "education_level", "death_certificate/demographics/education_level");
		dc_m_industry_code_1 = mmria_case.GetHiddenField(p_value, "dc_m_industry_code_1", "death_certificate/demographics/dc_m_industry_code_1");
		dc_m_industry_code_2 = mmria_case.GetHiddenField(p_value, "dc_m_industry_code_2", "death_certificate/demographics/dc_m_industry_code_2");
		dc_m_industry_code_3 = mmria_case.GetHiddenField(p_value, "dc_m_industry_code_3", "death_certificate/demographics/dc_m_industry_code_3");
		dc_m_occupation_code_1 = mmria_case.GetHiddenField(p_value, "dc_m_occupation_code_1", "death_certificate/demographics/dc_m_occupation_code_1");
		dc_m_occupation_code_2 = mmria_case.GetHiddenField(p_value, "dc_m_occupation_code_2", "death_certificate/demographics/dc_m_occupation_code_2");
		dc_m_occupation_code_3 = mmria_case.GetHiddenField(p_value, "dc_m_occupation_code_3", "death_certificate/demographics/dc_m_occupation_code_3");
	}
}

public sealed class _4D33674516ACCD60111807435908AEDC : IConvertDictionary
{
	public _4D33674516ACCD60111807435908AEDC()
	{
	}
	public string street { get; set; }
	public string apartment { get; set; }
	public string city { get; set; }
	public string state { get; set; }
	public string country_of_last_residence { get; set; }
	public string zip_code { get; set; }
	public string county { get; set; }
	public string feature_matching_geography_type { get; set; }
	public string latitude { get; set; }
	public string longitude { get; set; }
	public string naaccr_gis_coordinate_quality_code { get; set; }
	public string naaccr_gis_coordinate_quality_type { get; set; }
	public string naaccr_census_tract_certainty_code { get; set; }
	public string naaccr_census_tract_certainty_type { get; set; }
	public string state_county_fips { get; set; }
	public string census_state_fips { get; set; }
	public string census_county_fips { get; set; }
	public string census_tract_fips { get; set; }
	public string urban_status { get; set; }
	public string census_met_div_fips { get; set; }
	public string census_cbsa_fips { get; set; }
	public string census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		street = mmria_case.GetStringField(p_value, "street", "death_certificate/place_of_last_residence/street");
		apartment = mmria_case.GetStringField(p_value, "apartment", "death_certificate/place_of_last_residence/apartment");
		city = mmria_case.GetStringField(p_value, "city", "death_certificate/place_of_last_residence/city");
		state = mmria_case.GetStringListField(p_value, "state", "death_certificate/place_of_last_residence/state");
		country_of_last_residence = mmria_case.GetStringListField(p_value, "country_of_last_residence", "death_certificate/place_of_last_residence/country_of_last_residence");
		zip_code = mmria_case.GetStringField(p_value, "zip_code", "death_certificate/place_of_last_residence/zip_code");
		county = mmria_case.GetStringField(p_value, "county", "death_certificate/place_of_last_residence/county");
		feature_matching_geography_type = mmria_case.GetStringField(p_value, "feature_matching_geography_type", "death_certificate/place_of_last_residence/feature_matching_geography_type");
		latitude = mmria_case.GetStringField(p_value, "latitude", "death_certificate/place_of_last_residence/latitude");
		longitude = mmria_case.GetStringField(p_value, "longitude", "death_certificate/place_of_last_residence/longitude");
		naaccr_gis_coordinate_quality_code = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_code", "death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_code");
		naaccr_gis_coordinate_quality_type = mmria_case.GetHiddenField(p_value, "naaccr_gis_coordinate_quality_type", "death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_type");
		naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_code", "death_certificate/place_of_last_residence/naaccr_census_tract_certainty_code");
		naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "naaccr_census_tract_certainty_type", "death_certificate/place_of_last_residence/naaccr_census_tract_certainty_type");
		state_county_fips = mmria_case.GetHiddenField(p_value, "state_county_fips", "death_certificate/place_of_last_residence/state_county_fips");
		census_state_fips = mmria_case.GetHiddenField(p_value, "census_state_fips", "death_certificate/place_of_last_residence/census_state_fips");
		census_county_fips = mmria_case.GetHiddenField(p_value, "census_county_fips", "death_certificate/place_of_last_residence/census_county_fips");
		census_tract_fips = mmria_case.GetHiddenField(p_value, "census_tract_fips", "death_certificate/place_of_last_residence/census_tract_fips");
		urban_status = mmria_case.GetStringField(p_value, "urban_status", "death_certificate/place_of_last_residence/urban_status");
		census_met_div_fips = mmria_case.GetHiddenField(p_value, "census_met_div_fips", "death_certificate/place_of_last_residence/census_met_div_fips");
		census_cbsa_fips = mmria_case.GetHiddenField(p_value, "census_cbsa_fips", "death_certificate/place_of_last_residence/census_cbsa_fips");
		census_cbsa_micro = mmria_case.GetHiddenField(p_value, "census_cbsa_micro", "death_certificate/place_of_last_residence/census_cbsa_micro");
	}
}

public sealed class _E2EB1399F547FAAA8FA9D40A84DF9DDC : IConvertDictionary
{
	public _E2EB1399F547FAAA8FA9D40A84DF9DDC()
	{
	}
	public TimeOnly? time_of_death { get; set; }
	public string local_file_number { get; set; }
	public string state_file_number { get; set; }
	public string dmaiden { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		time_of_death = mmria_case.GetTimeField(p_value, "time_of_death", "death_certificate/certificate_identification/time_of_death");
		local_file_number = mmria_case.GetStringField(p_value, "local_file_number", "death_certificate/certificate_identification/local_file_number");
		state_file_number = mmria_case.GetStringField(p_value, "state_file_number", "death_certificate/certificate_identification/state_file_number");
		dmaiden = mmria_case.GetStringField(p_value, "dmaiden", "death_certificate/certificate_identification/dmaiden");
	}
}

public sealed class _172DA69DB9FF602A0978A04E9D3E470F : IConvertDictionary
{
	public _172DA69DB9FF602A0978A04E9D3E470F()
	{
		certificate_identification = new ();
		place_of_last_residence = new ();
		demographics = new ();
		race = new ();
		injury_associated_information = new ();
		address_of_injury = new ();
		death_information = new ();
		address_of_death = new ();
		causes_of_death = new ();
		vitals_import_group = new ();
	}
	public _E2EB1399F547FAAA8FA9D40A84DF9DDC certificate_identification{ get;set;}
	public _4D33674516ACCD60111807435908AEDC place_of_last_residence{ get;set;}
	public _72A993E1B915072396CD06F798020CEF demographics{ get;set;}
	public string citizen_of_what_country { get; set; }
	public _4DFB05F38E9F2A6773B8FFF545D24AA2 race{ get;set;}
	public _E238EE0EC013BC544EA03FABE426DF45 injury_associated_information{ get;set;}
	public _C4B6CBD761F83E9017FA96D0C02BB6BE address_of_injury{ get;set;}
	public _079B7F4BEB356992B2DD1A9449392DE8 death_information{ get;set;}
	public _F8873E911BE747DC2E249ACC7FC0ECC9 address_of_death{ get;set;}
	public List<_194FE6F9039297ED012FE605A81755D2> causes_of_death{ get;set;}
	public string reviewer_note { get; set; }
	public _F01C82803721A4C8A0DB077B9E1138E4 vitals_import_group{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		certificate_identification = mmria_case.GetGroupField<_E2EB1399F547FAAA8FA9D40A84DF9DDC>(p_value, "certificate_identification", "death_certificate/certificate_identification");
		place_of_last_residence = mmria_case.GetGroupField<_4D33674516ACCD60111807435908AEDC>(p_value, "place_of_last_residence", "death_certificate/place_of_last_residence");
		demographics = mmria_case.GetGroupField<_72A993E1B915072396CD06F798020CEF>(p_value, "demographics", "death_certificate/demographics");
		citizen_of_what_country = mmria_case.GetStringListField(p_value, "citizen_of_what_country", "death_certificate/citizen_of_what_country");
		race = mmria_case.GetGroupField<_4DFB05F38E9F2A6773B8FFF545D24AA2>(p_value, "race", "death_certificate/race");
		injury_associated_information = mmria_case.GetGroupField<_E238EE0EC013BC544EA03FABE426DF45>(p_value, "injury_associated_information", "death_certificate/injury_associated_information");
		address_of_injury = mmria_case.GetGroupField<_C4B6CBD761F83E9017FA96D0C02BB6BE>(p_value, "address_of_injury", "death_certificate/address_of_injury");
		death_information = mmria_case.GetGroupField<_079B7F4BEB356992B2DD1A9449392DE8>(p_value, "death_information", "death_certificate/death_information");
		address_of_death = mmria_case.GetGroupField<_F8873E911BE747DC2E249ACC7FC0ECC9>(p_value, "address_of_death", "death_certificate/address_of_death");
		causes_of_death = mmria_case.GetGridField<_194FE6F9039297ED012FE605A81755D2>(p_value, "causes_of_death", "death_certificate/causes_of_death");
		reviewer_note = mmria_case.GetTextAreaField(p_value, "reviewer_note", "death_certificate/reviewer_note");
		vitals_import_group = mmria_case.GetGroupField<_F01C82803721A4C8A0DB077B9E1138E4>(p_value, "vitals_import_group", "death_certificate/vitals_import_group");
	}
}

public sealed class _0E4A47EE2AB2DBA5C0DF22ECFE205A61 : IConvertDictionary
{
	public _0E4A47EE2AB2DBA5C0DF22ECFE205A61()
	{
	}
	public string vital_report { get; set; }
	public string vro_status { get; set; }
	public string import_date { get; set; }
	public double? bc_det_match { get; set; }
	public double? fdc_det_match { get; set; }
	public double? bc_prob_match { get; set; }
	public double? fdc_prob_match { get; set; }
	public double? icd10_match { get; set; }
	public double? pregcb_match { get; set; }
	public double? literalcod_match { get; set; }
	public double? hr_cdc_other { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		vital_report = mmria_case.GetTextAreaField(p_value, "vital_report", "home_record/automated_vitals_group/vital_report");
		vro_status = mmria_case.GetStringListField(p_value, "vro_status", "home_record/automated_vitals_group/vro_status");
		import_date = mmria_case.GetHiddenField(p_value, "import_date", "home_record/automated_vitals_group/import_date");
		bc_det_match = mmria_case.GetNumberListField(p_value, "bc_det_match", "home_record/automated_vitals_group/bc_det_match");
		fdc_det_match = mmria_case.GetNumberListField(p_value, "fdc_det_match", "home_record/automated_vitals_group/fdc_det_match");
		bc_prob_match = mmria_case.GetNumberListField(p_value, "bc_prob_match", "home_record/automated_vitals_group/bc_prob_match");
		fdc_prob_match = mmria_case.GetNumberListField(p_value, "fdc_prob_match", "home_record/automated_vitals_group/fdc_prob_match");
		icd10_match = mmria_case.GetNumberListField(p_value, "icd10_match", "home_record/automated_vitals_group/icd10_match");
		pregcb_match = mmria_case.GetNumberListField(p_value, "pregcb_match", "home_record/automated_vitals_group/pregcb_match");
		literalcod_match = mmria_case.GetNumberListField(p_value, "literalcod_match", "home_record/automated_vitals_group/literalcod_match");
		hr_cdc_other = mmria_case.GetNumberListField(p_value, "hr_cdc_other", "home_record/automated_vitals_group/hr_cdc_other");
	}
}

public sealed class _346F75CE75CAB1576391D6018DCD93F4 : IConvertDictionary
{
	public _346F75CE75CAB1576391D6018DCD93F4()
	{
	}
	public double? death_certificate { get; set; }
	public double? autopsy_report { get; set; }
	public double? birth_certificate_parent_section { get; set; }
	public double? birth_certificate_infant_or_fetal_death_section { get; set; }
	public double? community_vital_signs { get; set; }
	public double? social_and_psychological_profile { get; set; }
	public double? prenatal_care_record { get; set; }
	public double? er_visits_and_hospitalizations { get; set; }
	public double? other_medical_visits { get; set; }
	public double? medical_transport { get; set; }
	public double? mental_health_profile { get; set; }
	public double? informant_interviews { get; set; }
	public double? case_narrative { get; set; }
	public double? committe_review_worksheet { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		death_certificate = mmria_case.GetNumberListField(p_value, "death_certificate", "home_record/case_progress_report/death_certificate");
		autopsy_report = mmria_case.GetNumberListField(p_value, "autopsy_report", "home_record/case_progress_report/autopsy_report");
		birth_certificate_parent_section = mmria_case.GetNumberListField(p_value, "birth_certificate_parent_section", "home_record/case_progress_report/birth_certificate_parent_section");
		birth_certificate_infant_or_fetal_death_section = mmria_case.GetNumberListField(p_value, "birth_certificate_infant_or_fetal_death_section", "home_record/case_progress_report/birth_certificate_infant_or_fetal_death_section");
		community_vital_signs = mmria_case.GetNumberListField(p_value, "community_vital_signs", "home_record/case_progress_report/community_vital_signs");
		social_and_psychological_profile = mmria_case.GetNumberListField(p_value, "social_and_psychological_profile", "home_record/case_progress_report/social_and_psychological_profile");
		prenatal_care_record = mmria_case.GetNumberListField(p_value, "prenatal_care_record", "home_record/case_progress_report/prenatal_care_record");
		er_visits_and_hospitalizations = mmria_case.GetNumberListField(p_value, "er_visits_and_hospitalizations", "home_record/case_progress_report/er_visits_and_hospitalizations");
		other_medical_visits = mmria_case.GetNumberListField(p_value, "other_medical_visits", "home_record/case_progress_report/other_medical_visits");
		medical_transport = mmria_case.GetNumberListField(p_value, "medical_transport", "home_record/case_progress_report/medical_transport");
		mental_health_profile = mmria_case.GetNumberListField(p_value, "mental_health_profile", "home_record/case_progress_report/mental_health_profile");
		informant_interviews = mmria_case.GetNumberListField(p_value, "informant_interviews", "home_record/case_progress_report/informant_interviews");
		case_narrative = mmria_case.GetNumberListField(p_value, "case_narrative", "home_record/case_progress_report/case_narrative");
		committe_review_worksheet = mmria_case.GetNumberListField(p_value, "committe_review_worksheet", "home_record/case_progress_report/committe_review_worksheet");
	}
}

public sealed class _AA4A06C727F9AE4E0C243FC39E60526B : IConvertDictionary
{
	public _AA4A06C727F9AE4E0C243FC39E60526B()
	{
	}
	public double? abstrator_assigned_status { get; set; }
	public double? number_of_days_after_end_of_pregnancey { get; set; }
	public double? hr_prg_outcome { get; set; }
	public string hr_prg_outcome_othsp { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		abstrator_assigned_status = mmria_case.GetNumberListField(p_value, "abstrator_assigned_status", "home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status");
		number_of_days_after_end_of_pregnancey = mmria_case.GetNumberField(p_value, "number_of_days_after_end_of_pregnancey", "home_record/overall_assessment_of_timing_of_death/number_of_days_after_end_of_pregnancey");
		hr_prg_outcome = mmria_case.GetNumberListField(p_value, "hr_prg_outcome", "home_record/overall_assessment_of_timing_of_death/hr_prg_outcome");
		hr_prg_outcome_othsp = mmria_case.GetStringField(p_value, "hr_prg_outcome_othsp", "home_record/overall_assessment_of_timing_of_death/hr_prg_outcome_othsp");
	}
}

public sealed class _37634834EDCA925A2BFFD92AB1270BA1 : IConvertDictionary
{
	public _37634834EDCA925A2BFFD92AB1270BA1()
	{
	}
	public double? overall_case_status { get; set; }
	public DateOnly? abstraction_begin_date { get; set; }
	public DateOnly? abstraction_complete_date { get; set; }
	public DateOnly? projected_review_date { get; set; }
	public DateOnly? committee_review_date { get; set; }
	public DateOnly? case_locked_date { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		overall_case_status = mmria_case.GetNumberListField(p_value, "overall_case_status", "home_record/case_status/overall_case_status");
		abstraction_begin_date = mmria_case.GetDateField(p_value, "abstraction_begin_date", "home_record/case_status/abstraction_begin_date");
		abstraction_complete_date = mmria_case.GetDateField(p_value, "abstraction_complete_date", "home_record/case_status/abstraction_complete_date");
		projected_review_date = mmria_case.GetDateField(p_value, "projected_review_date", "home_record/case_status/projected_review_date");
		committee_review_date = mmria_case.GetDateField(p_value, "committee_review_date", "home_record/case_status/committee_review_date");
		case_locked_date = mmria_case.GetDateField(p_value, "case_locked_date", "home_record/case_status/case_locked_date");
	}
}

public sealed class _DAFBAA00B2FF0706366F6EF7604B7CC5 : IConvertDictionary
{
	public _DAFBAA00B2FF0706366F6EF7604B7CC5()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "home_record/date_of_death/month");
		day = mmria_case.GetNumberListField(p_value, "day", "home_record/date_of_death/day");
		year = mmria_case.GetNumberListField(p_value, "year", "home_record/date_of_death/year");
	}
}

public sealed class _2F66E2C85C3BE07445A8007E07961BF7 : IConvertDictionary
{
	public _2F66E2C85C3BE07445A8007E07961BF7()
	{
		date_of_death = new ();
		how_was_this_death_identified = new ();
		case_status = new ();
		overall_assessment_of_timing_of_death = new ();
		case_progress_report = new ();
		automated_vitals_group = new ();
	}
	public string first_name { get; set; }
	public string middle_name { get; set; }
	public string last_name { get; set; }
	public _DAFBAA00B2FF0706366F6EF7604B7CC5 date_of_death{ get;set;}
	public string state_of_death_record { get; set; }
	public string record_id { get; set; }
	public string agency_case_id { get; set; }
	public List<double> how_was_this_death_identified { get; set; }
	public string specify_other_multiple_sources { get; set; }
	public string primary_abstractor { get; set; }
	public string jurisdiction_id { get; set; }
	public _37634834EDCA925A2BFFD92AB1270BA1 case_status{ get;set;}
	public _AA4A06C727F9AE4E0C243FC39E60526B overall_assessment_of_timing_of_death{ get;set;}
	public _346F75CE75CAB1576391D6018DCD93F4 case_progress_report{ get;set;}
	public _0E4A47EE2AB2DBA5C0DF22ECFE205A61 automated_vitals_group{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		first_name = mmria_case.GetStringField(p_value, "first_name", "home_record/first_name");
		middle_name = mmria_case.GetStringField(p_value, "middle_name", "home_record/middle_name");
		last_name = mmria_case.GetStringField(p_value, "last_name", "home_record/last_name");
		date_of_death = mmria_case.GetGroupField<_DAFBAA00B2FF0706366F6EF7604B7CC5>(p_value, "date_of_death", "home_record/date_of_death");
		state_of_death_record = mmria_case.GetStringListField(p_value, "state_of_death_record", "home_record/state_of_death_record");
		record_id = mmria_case.GetStringField(p_value, "record_id", "home_record/record_id");
		agency_case_id = mmria_case.GetStringField(p_value, "agency_case_id", "home_record/agency_case_id");
		how_was_this_death_identified = mmria_case.GetMultiSelectNumberListField(p_value, "how_was_this_death_identified", "home_record/how_was_this_death_identified");
		specify_other_multiple_sources = mmria_case.GetStringField(p_value, "specify_other_multiple_sources", "home_record/specify_other_multiple_sources");
		primary_abstractor = mmria_case.GetStringField(p_value, "primary_abstractor", "home_record/primary_abstractor");
		jurisdiction_id = mmria_case.GetJurisdictionField(p_value, "jurisdiction_id", "home_record/jurisdiction_id");
		case_status = mmria_case.GetGroupField<_37634834EDCA925A2BFFD92AB1270BA1>(p_value, "case_status", "home_record/case_status");
		overall_assessment_of_timing_of_death = mmria_case.GetGroupField<_AA4A06C727F9AE4E0C243FC39E60526B>(p_value, "overall_assessment_of_timing_of_death", "home_record/overall_assessment_of_timing_of_death");
		case_progress_report = mmria_case.GetGroupField<_346F75CE75CAB1576391D6018DCD93F4>(p_value, "case_progress_report", "home_record/case_progress_report");
		automated_vitals_group = mmria_case.GetGroupField<_0E4A47EE2AB2DBA5C0DF22ECFE205A61>(p_value, "automated_vitals_group", "home_record/automated_vitals_group");
	}
}

public sealed class _31525A784A20079888C887AC49E5D1B9 : IConvertDictionary
{
	public _31525A784A20079888C887AC49E5D1B9()
	{
	}
	public string version { get; set; }
	public string datetime { get; set; }
	public string is_forced_write { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		version = mmria_case.GetStringField(p_value, "version", "data_migration_history/version");
		datetime = mmria_case.GetStringField(p_value, "datetime", "data_migration_history/datetime");
		is_forced_write = mmria_case.GetStringField(p_value, "is_forced_write", "data_migration_history/is_forced_write");
	}
}

public sealed partial class mmria_case
{
	public mmria_case()
	{
		data_migration_history = new ();
		home_record = new ();
		death_certificate = new ();
		birth_fetal_death_certificate_parent = new ();
		birth_certificate_infant_fetal_section = new ();
		cvs = new ();
		social_and_environmental_profile = new ();
		autopsy_report = new ();
		prenatal = new ();
		er_visit_and_hospital_medical_records = new ();
		other_medical_office_visits = new ();
		medical_transport = new ();
		mental_health_profile = new ();
		informant_interviews = new ();
		case_narrative = new ();
		committee_review = new ();
	}
	public string _id { get; set; }
	public string _rev { get; set; }

	public string version { get; set; }
	public List<_31525A784A20079888C887AC49E5D1B9> data_migration_history{ get;set;}
	public DateTime? date_created { get; set; }
	public string created_by { get; set; }
	public DateTime? date_last_updated { get; set; }
	public string last_updated_by { get; set; }
	public DateTime? date_last_checked_out { get; set; }
	public string last_checked_out_by { get; set; }
	public string host_state { get; set; }
	public string addquarter { get; set; }
	public string cmpquarter { get; set; }
	public _2F66E2C85C3BE07445A8007E07961BF7 home_record{ get;set;}
	public _172DA69DB9FF602A0978A04E9D3E470F death_certificate{ get;set;}
	public _1757340A93C7CC802C810229D906E417 birth_fetal_death_certificate_parent{ get;set;}
	public List<_580C23C24054AB0BE30540A0BDCD16A0> birth_certificate_infant_fetal_section{ get;set;}
	public _72F1A850D966375FA159121C7C8B09A1 cvs{ get;set;}
	public _F495787DD96BB2B871443F9596F9C77F social_and_environmental_profile{ get;set;}
	public _B01FDEA65CCD8F2AE7E63858F58F93D2 autopsy_report{ get;set;}
	public _02DBD2E611DEC822A826C2F0B1D1DE0F prenatal{ get;set;}
	public List<_0CE40C4018C47CA22AC1A0003DC34FB7> er_visit_and_hospital_medical_records{ get;set;}
	public List<_CAE881A4974F08BB4F9D46B90FEF51D4> other_medical_office_visits{ get;set;}
	public List<_9206DAB82DFEDA2BC11D83175919BA02> medical_transport{ get;set;}
	public _06AA314F235917500C48AB5E3CD1C034 mental_health_profile{ get;set;}
	public List<_18CD53D47CBDE2540A9EF3EC5B51E0BA> informant_interviews{ get;set;}
	public _A35F564798944667E91C53B3A3DA359D case_narrative{ get;set;}
	public _62AEF5C4D8129ED98ECA69F7779FCBFC committee_review{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		_id = mmria_case.GetStringField(p_value, "_id", "_id");
		_rev = mmria_case.GetStringField(p_value, "_rev", "_rev");
		version = mmria_case.GetStringField(p_value, "version", "version");
		data_migration_history = mmria_case.GetGridField<_31525A784A20079888C887AC49E5D1B9>(p_value, "data_migration_history", "data_migration_history");
		date_created = mmria_case.GetDateTimeField(p_value, "date_created", "date_created");
		created_by = mmria_case.GetStringField(p_value, "created_by", "created_by");
		date_last_updated = mmria_case.GetDateTimeField(p_value, "date_last_updated", "date_last_updated");
		last_updated_by = mmria_case.GetStringField(p_value, "last_updated_by", "last_updated_by");
		date_last_checked_out = mmria_case.GetDateTimeField(p_value, "date_last_checked_out", "date_last_checked_out");
		last_checked_out_by = mmria_case.GetStringField(p_value, "last_checked_out_by", "last_checked_out_by");
		host_state = mmria_case.GetStringField(p_value, "host_state", "host_state");
		addquarter = mmria_case.GetStringField(p_value, "addquarter", "addquarter");
		cmpquarter = mmria_case.GetStringField(p_value, "cmpquarter", "cmpquarter");
		home_record = mmria_case.GetFormField<_2F66E2C85C3BE07445A8007E07961BF7>(p_value, "home_record", "home_record");
		death_certificate = mmria_case.GetFormField<_172DA69DB9FF602A0978A04E9D3E470F>(p_value, "death_certificate", "death_certificate");
		birth_fetal_death_certificate_parent = mmria_case.GetFormField<_1757340A93C7CC802C810229D906E417>(p_value, "birth_fetal_death_certificate_parent", "birth_fetal_death_certificate_parent");
		birth_certificate_infant_fetal_section = mmria_case.GetMultiFormField<_580C23C24054AB0BE30540A0BDCD16A0>(p_value, "birth_certificate_infant_fetal_section", "birth_certificate_infant_fetal_section");
		cvs = mmria_case.GetFormField<_72F1A850D966375FA159121C7C8B09A1>(p_value, "cvs", "cvs");
		social_and_environmental_profile = mmria_case.GetFormField<_F495787DD96BB2B871443F9596F9C77F>(p_value, "social_and_environmental_profile", "social_and_environmental_profile");
		autopsy_report = mmria_case.GetFormField<_B01FDEA65CCD8F2AE7E63858F58F93D2>(p_value, "autopsy_report", "autopsy_report");
		prenatal = mmria_case.GetFormField<_02DBD2E611DEC822A826C2F0B1D1DE0F>(p_value, "prenatal", "prenatal");
		er_visit_and_hospital_medical_records = mmria_case.GetMultiFormField<_0CE40C4018C47CA22AC1A0003DC34FB7>(p_value, "er_visit_and_hospital_medical_records", "er_visit_and_hospital_medical_records");
		other_medical_office_visits = mmria_case.GetMultiFormField<_CAE881A4974F08BB4F9D46B90FEF51D4>(p_value, "other_medical_office_visits", "other_medical_office_visits");
		medical_transport = mmria_case.GetMultiFormField<_9206DAB82DFEDA2BC11D83175919BA02>(p_value, "medical_transport", "medical_transport");
		mental_health_profile = mmria_case.GetFormField<_06AA314F235917500C48AB5E3CD1C034>(p_value, "mental_health_profile", "mental_health_profile");
		informant_interviews = mmria_case.GetMultiFormField<_18CD53D47CBDE2540A9EF3EC5B51E0BA>(p_value, "informant_interviews", "informant_interviews");
		case_narrative = mmria_case.GetFormField<_A35F564798944667E91C53B3A3DA359D>(p_value, "case_narrative", "case_narrative");
		committee_review = mmria_case.GetFormField<_62AEF5C4D8129ED98ECA69F7779FCBFC>(p_value, "committee_review", "committee_review");
	}
}

