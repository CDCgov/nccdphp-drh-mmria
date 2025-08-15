
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.v241001;

public sealed partial class mmria_case
{


    public bool SetSG_String(string path, int index, string value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "data_migration_history/version":
                    data_migration_history[index].version = value;
                    result = true;
            break;
            case "data_migration_history/datetime":
                    data_migration_history[index].datetime = value;
                    result = true;
            break;
            case "data_migration_history/is_forced_write":
                    data_migration_history[index].is_forced_write = value;
                    result = true;
            break;
            case "death_certificate/causes_of_death/cause_descriptive":
                    death_certificate.causes_of_death[index].cause_descriptive = value;
                    result = true;
            break;
            case "death_certificate/causes_of_death/icd_code":
                    death_certificate.causes_of_death[index].icd_code = value;
                    result = true;
            break;
            case "death_certificate/causes_of_death/interval":
                    death_certificate.causes_of_death[index].interval = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_api_request_url":
                    cvs.cvs_grid[index].cvs_api_request_url = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_api_request_date_time":
                    cvs.cvs_grid[index].cvs_api_request_date_time = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_api_request_c_geoid":
                    cvs.cvs_grid[index].cvs_api_request_c_geoid = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_api_request_t_geoid":
                    cvs.cvs_grid[index].cvs_api_request_t_geoid = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_api_request_year":
                    cvs.cvs_grid[index].cvs_api_request_year = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_api_request_result_message":
                    cvs.cvs_grid[index].cvs_api_request_result_message = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_mdrate_county":
                    cvs.cvs_grid[index].cvs_mdrate_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctnoins_fem_tract":
                    cvs.cvs_grid[index].cvs_pctnoins_fem_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctnovehicle_county":
                    cvs.cvs_grid[index].cvs_pctnovehicle_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctnovehicle_tract":
                    cvs.cvs_grid[index].cvs_pctnovehicle_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctmove_tract":
                    cvs.cvs_grid[index].cvs_pctmove_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctsphh_tract":
                    cvs.cvs_grid[index].cvs_pctsphh_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctovercrowdhh_tract":
                    cvs.cvs_grid[index].cvs_pctovercrowdhh_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctowner_occ_tract":
                    cvs.cvs_grid[index].cvs_pctowner_occ_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pct_less_well_tract":
                    cvs.cvs_grid[index].cvs_pct_less_well_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_ndi_raw_tract":
                    cvs.cvs_grid[index].cvs_ndi_raw_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctpov_tract":
                    cvs.cvs_grid[index].cvs_pctpov_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_ice_income_all_tract":
                    cvs.cvs_grid[index].cvs_ice_income_all_tract = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctobese_county":
                    cvs.cvs_grid[index].cvs_pctobese_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_fi_county":
                    cvs.cvs_grid[index].cvs_fi_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_cnmrate_county":
                    cvs.cvs_grid[index].cvs_cnmrate_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_obgynrate_county":
                    cvs.cvs_grid[index].cvs_obgynrate_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_rtteenbirth_county":
                    cvs.cvs_grid[index].cvs_rtteenbirth_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_rtstd_county":
                    cvs.cvs_grid[index].cvs_rtstd_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_rtdrugodmortality_county":
                    cvs.cvs_grid[index].cvs_rtdrugodmortality_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_rtsocassoc_county":
                    cvs.cvs_grid[index].cvs_rtsocassoc_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pcthouse_distress_county":
                    cvs.cvs_grid[index].cvs_pcthouse_distress_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_rtviolentcr_icpsr_county":
                    cvs.cvs_grid[index].cvs_rtviolentcr_icpsr_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_isolation_county":
                    cvs.cvs_grid[index].cvs_isolation_county = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_pctrural":
                    cvs.cvs_grid[index].cvs_pctrural = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_racialized_pov":
                    cvs.cvs_grid[index].cvs_racialized_pov = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_mhproviderrate":
                    cvs.cvs_grid[index].cvs_mhproviderrate = value;
                    result = true;
            break;
            case "cvs/cvs_grid/cvs_rtmhpract_county":
                    cvs.cvs_grid[index].cvs_rtmhpract_county = value;
                    result = true;
            break;
            case "social_and_environmental_profile/members_of_household/age":
                    social_and_environmental_profile.members_of_household[index].age = value;
                    result = true;
            break;
            case "social_and_environmental_profile/members_of_household/comments":
                    social_and_environmental_profile.members_of_household[index].comments = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_incarcerations/duration":
                    social_and_environmental_profile.details_of_incarcerations[index].duration = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_incarcerations/reason":
                    social_and_environmental_profile.details_of_incarcerations[index].reason = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_incarcerations/comments":
                    social_and_environmental_profile.details_of_incarcerations[index].comments = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_arrests/arest_reason":
                    social_and_environmental_profile.details_of_arrests[index].arest_reason = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_arrests/comments":
                    social_and_environmental_profile.details_of_arrests[index].comments = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/referred_to":
                    social_and_environmental_profile.social_and_medical_referrals[index].referred_to = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/specialty":
                    social_and_environmental_profile.social_and_medical_referrals[index].specialty = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/reason":
                    social_and_environmental_profile.social_and_medical_referrals[index].reason = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/reason_for_non_compliance":
                    social_and_environmental_profile.social_and_medical_referrals[index].reason_for_non_compliance = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/comments":
                    social_and_environmental_profile.social_and_medical_referrals[index].comments = value;
                    result = true;
            break;
            case "social_and_environmental_profile/sources_of_social_services_information_for_this_record/element_other":
                    social_and_environmental_profile.sources_of_social_services_information_for_this_record[index].element_other = value;
                    result = true;
            break;
            case "social_and_environmental_profile/sources_of_social_services_information_for_this_record/source_name":
                    social_and_environmental_profile.sources_of_social_services_information_for_this_record[index].source_name = value;
                    result = true;
            break;
            case "social_and_environmental_profile/sources_of_social_services_information_for_this_record/comments":
                    social_and_environmental_profile.sources_of_social_services_information_for_this_record[index].comments = value;
                    result = true;
            break;
            case "social_and_environmental_profile/if_yes_specify_substances/substance":
                    social_and_environmental_profile.if_yes_specify_substances[index].substance = value;
                    result = true;
            break;
            case "social_and_environmental_profile/if_yes_specify_substances/substance_other":
                    social_and_environmental_profile.if_yes_specify_substances[index].substance_other = value;
                    result = true;
            break;
            case "autopsy_report/relevant_maternal_death_findings/gross_findings/finding":
                    autopsy_report.relevant_maternal_death_findings.gross_findings[index].finding = value;
                    result = true;
            break;
            case "autopsy_report/relevant_maternal_death_findings/gross_findings/comment":
                    autopsy_report.relevant_maternal_death_findings.gross_findings[index].comment = value;
                    result = true;
            break;
            case "autopsy_report/relevant_maternal_death_findings/microscopic_findings/finding":
                    autopsy_report.relevant_maternal_death_findings.microscopic_findings[index].finding = value;
                    result = true;
            break;
            case "autopsy_report/relevant_maternal_death_findings/microscopic_findings/comment":
                    autopsy_report.relevant_maternal_death_findings.microscopic_findings[index].comment = value;
                    result = true;
            break;
            case "autopsy_report/toxicology/substance":
                    autopsy_report.toxicology[index].substance = value;
                    result = true;
            break;
            case "autopsy_report/toxicology/substance_other":
                    autopsy_report.toxicology[index].substance_other = value;
                    result = true;
            break;
            case "autopsy_report/toxicology/concentration":
                    autopsy_report.toxicology[index].concentration = value;
                    result = true;
            break;
            case "autopsy_report/toxicology/unit_of_measure":
                    autopsy_report.toxicology[index].unit_of_measure = value;
                    result = true;
            break;
            case "autopsy_report/toxicology/comment":
                    autopsy_report.toxicology[index].comment = value;
                    result = true;
            break;
            case "autopsy_report/causes_of_death/cause":
                    autopsy_report.causes_of_death[index].cause = value;
                    result = true;
            break;
            case "autopsy_report/causes_of_death/icd_code":
                    autopsy_report.causes_of_death[index].icd_code = value;
                    result = true;
            break;
            case "autopsy_report/causes_of_death/comment":
                    autopsy_report.causes_of_death[index].comment = value;
                    result = true;
            break;
            case "prenatal/prior_surgical_procedures_before_pregnancy/procedure":
                    prenatal.prior_surgical_procedures_before_pregnancy[index].procedure = value;
                    result = true;
            break;
            case "prenatal/prior_surgical_procedures_before_pregnancy/comments":
                    prenatal.prior_surgical_procedures_before_pregnancy[index].comments = value;
                    result = true;
            break;
            case "prenatal/pre_existing_conditons_grid/other":
                    prenatal.pre_existing_conditons_grid[index].other = value;
                    result = true;
            break;
            case "prenatal/pre_existing_conditons_grid/duration":
                    prenatal.pre_existing_conditons_grid[index].duration = value;
                    result = true;
            break;
            case "prenatal/pre_existing_conditons_grid/comments":
                    prenatal.pre_existing_conditons_grid[index].comments = value;
                    result = true;
            break;
            case "prenatal/family_medical_history/condition":
                    prenatal.family_medical_history[index].condition = value;
                    result = true;
            break;
            case "prenatal/family_medical_history/comments":
                    prenatal.family_medical_history[index].comments = value;
                    result = true;
            break;
            case "prenatal/substance_use_grid/substance":
                    prenatal.substance_use_grid[index].substance = value;
                    result = true;
            break;
            case "prenatal/substance_use_grid/substance_other":
                    prenatal.substance_use_grid[index].substance_other = value;
                    result = true;
            break;
            case "prenatal/substance_use_grid/comments":
                    prenatal.substance_use_grid[index].comments = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/method_of_delivery":
                    prenatal.pregnancy_history.details_grid[index].method_of_delivery = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/complications":
                    prenatal.pregnancy_history.details_grid[index].complications = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/comments":
                    prenatal.routine_monitoring[index].comments = value;
                    result = true;
            break;
            case "prenatal/other_lab_tests/test_or_procedure":
                    prenatal.other_lab_tests[index].test_or_procedure = value;
                    result = true;
            break;
            case "prenatal/other_lab_tests/results":
                    prenatal.other_lab_tests[index].results = value;
                    result = true;
            break;
            case "prenatal/other_lab_tests/comments":
                    prenatal.other_lab_tests[index].comments = value;
                    result = true;
            break;
            case "prenatal/diagnostic_procedures/procedure":
                    prenatal.diagnostic_procedures[index].procedure = value;
                    result = true;
            break;
            case "prenatal/diagnostic_procedures/comments":
                    prenatal.diagnostic_procedures[index].comments = value;
                    result = true;
            break;
            case "prenatal/problems_identified_grid/problem":
                    prenatal.problems_identified_grid[index].problem = value;
                    result = true;
            break;
            case "prenatal/problems_identified_grid/comments":
                    prenatal.problems_identified_grid[index].comments = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/medication":
                    prenatal.medications_and_drugs_during_pregnancy[index].medication = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/dose_frequency_duration":
                    prenatal.medications_and_drugs_during_pregnancy[index].dose_frequency_duration = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/reason":
                    prenatal.medications_and_drugs_during_pregnancy[index].reason = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/facility":
                    prenatal.pre_delivery_hospitalizations_details[index].facility = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/duration":
                    prenatal.pre_delivery_hospitalizations_details[index].duration = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/reason":
                    prenatal.pre_delivery_hospitalizations_details[index].reason = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/comments":
                    prenatal.pre_delivery_hospitalizations_details[index].comments = value;
                    result = true;
            break;
            case "prenatal/medical_referrals/type_of_specialist":
                    prenatal.medical_referrals[index].type_of_specialist = value;
                    result = true;
            break;
            case "prenatal/medical_referrals/reason":
                    prenatal.medical_referrals[index].reason = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/city":
                    prenatal.other_sources_of_prenatal_care[index].city = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/state":
                    prenatal.other_sources_of_prenatal_care[index].state = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/pregrid_comments":
                    prenatal.other_sources_of_prenatal_care[index].pregrid_comments = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/mhpdpmhc_condi_othsp":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].mhpdpmhc_condi_othsp = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/duration_of_condition":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].duration_of_condition = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/treatments":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].treatments = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/duration_of_tx":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].duration_of_tx = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/comments":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].comments = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/other_screening_tool":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].other_screening_tool = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/findings":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].findings = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/comments":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].comments = value;
                    result = true;
            break;
            case "committee_review/committee_determination_of_causes_of_death/cause_descriptive":
                    committee_review.committee_determination_of_causes_of_death[index].cause_descriptive = value;
                    result = true;
            break;
            case "committee_review/committee_determination_of_causes_of_death/comments":
                    committee_review.committee_determination_of_causes_of_death[index].comments = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/description":
                    committee_review.critical_factors_worksheet[index].description = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/committee_recommendations":
                    committee_review.critical_factors_worksheet[index].committee_recommendations = value;
                    result = true;
            break;
            case "committee_review/recommendations_of_committee/committee_recommendations":
                    committee_review.recommendations_of_committee[index].committee_recommendations = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }


        
        return result;
    }

    public bool SetSG_Double(string path, int index, double? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "death_certificate/causes_of_death/cause_type":
                    death_certificate.causes_of_death[index].cause_type = value;
                    result = true;
            break;
            case "death_certificate/causes_of_death/interval_unit":
                    death_certificate.causes_of_death[index].interval_unit = value;
                    result = true;
            break;
            case "social_and_environmental_profile/members_of_household/relationship":
                    social_and_environmental_profile.members_of_household[index].relationship = value;
                    result = true;
            break;
            case "social_and_environmental_profile/members_of_household/gender":
                    social_and_environmental_profile.members_of_household[index].gender = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_incarcerations/occurrence":
                    social_and_environmental_profile.details_of_incarcerations[index].occurrence = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_arrests/occurrence":
                    social_and_environmental_profile.details_of_arrests[index].occurrence = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/compiled":
                    social_and_environmental_profile.social_and_medical_referrals[index].compiled = value;
                    result = true;
            break;
            case "social_and_environmental_profile/sources_of_social_services_information_for_this_record/element":
                    social_and_environmental_profile.sources_of_social_services_information_for_this_record[index].element = value;
                    result = true;
            break;
            case "social_and_environmental_profile/if_yes_specify_substances/timing_of_substance_use":
                    social_and_environmental_profile.if_yes_specify_substances[index].timing_of_substance_use = value;
                    result = true;
            break;
            case "autopsy_report/toxicology/level":
                    autopsy_report.toxicology[index].level = value;
                    result = true;
            break;
            case "autopsy_report/causes_of_death/type":
                    autopsy_report.causes_of_death[index].type = value;
                    result = true;
            break;
            case "prenatal/pre_existing_conditons_grid/condition":
                    prenatal.pre_existing_conditons_grid[index].condition = value;
                    result = true;
            break;
            case "prenatal/family_medical_history/relation":
                    prenatal.family_medical_history[index].relation = value;
                    result = true;
            break;
            case "prenatal/family_medical_history/is_living":
                    prenatal.family_medical_history[index].is_living = value;
                    result = true;
            break;
            case "prenatal/family_medical_history/age_at_death":
                    prenatal.family_medical_history[index].age_at_death = value;
                    result = true;
            break;
            case "prenatal/substance_use_grid/screening":
                    prenatal.substance_use_grid[index].screening = value;
                    result = true;
            break;
            case "prenatal/substance_use_grid/couseling_education":
                    prenatal.substance_use_grid[index].couseling_education = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/outcome":
                    prenatal.pregnancy_history.details_grid[index].outcome = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/gestational_age":
                    prenatal.pregnancy_history.details_grid[index].gestational_age = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/birth_weight_uom":
                    prenatal.pregnancy_history.details_grid[index].birth_weight_uom = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/birth_weight":
                    prenatal.pregnancy_history.details_grid[index].birth_weight = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/birth_weight_oz":
                    prenatal.pregnancy_history.details_grid[index].birth_weight_oz = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/is_now_living":
                    prenatal.pregnancy_history.details_grid[index].is_now_living = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/gestational_age_weeks":
                    prenatal.routine_monitoring[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/gestational_age_days":
                    prenatal.routine_monitoring[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/systolic_bp":
                    prenatal.routine_monitoring[index].systolic_bp = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/diastolic":
                    prenatal.routine_monitoring[index].diastolic = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/heart_rate":
                    prenatal.routine_monitoring[index].heart_rate = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/oxygen_saturation":
                    prenatal.routine_monitoring[index].oxygen_saturation = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/urine_protein":
                    prenatal.routine_monitoring[index].urine_protein = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/urine_ketones":
                    prenatal.routine_monitoring[index].urine_ketones = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/urine_glucose":
                    prenatal.routine_monitoring[index].urine_glucose = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/blood_hematocrit":
                    prenatal.routine_monitoring[index].blood_hematocrit = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/weight":
                    prenatal.routine_monitoring[index].weight = value;
                    result = true;
            break;
            case "prenatal/other_lab_tests/gestational_age_weeks":
                    prenatal.other_lab_tests[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/other_lab_tests/gestational_age_days":
                    prenatal.other_lab_tests[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/diagnostic_procedures/gestational_age_weeks":
                    prenatal.diagnostic_procedures[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/diagnostic_procedures/gestational_age_days":
                    prenatal.diagnostic_procedures[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/problems_identified_grid/gestational_age_weeks":
                    prenatal.problems_identified_grid[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/problems_identified_grid/gestational_age_days":
                    prenatal.problems_identified_grid[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/gestational_age_weeks":
                    prenatal.medications_and_drugs_during_pregnancy[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/gestational_age_days":
                    prenatal.medications_and_drugs_during_pregnancy[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/is_adverse_reaction":
                    prenatal.medications_and_drugs_during_pregnancy[index].is_adverse_reaction = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/gestational_age_weeks":
                    prenatal.pre_delivery_hospitalizations_details[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/gestational_age_days":
                    prenatal.pre_delivery_hospitalizations_details[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/medical_referrals/gestational_age_weeks":
                    prenatal.medical_referrals[index].gestational_age_weeks = value;
                    result = true;
            break;
            case "prenatal/medical_referrals/gestational_age_days":
                    prenatal.medical_referrals[index].gestational_age_days = value;
                    result = true;
            break;
            case "prenatal/medical_referrals/was_appointment_kept":
                    prenatal.medical_referrals[index].was_appointment_kept = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/place":
                    prenatal.other_sources_of_prenatal_care[index].place = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/provider_type":
                    prenatal.other_sources_of_prenatal_care[index].provider_type = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/condition":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].condition = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/treatment_changed_during_pregnancy":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].treatment_changed_during_pregnancy = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/dosage_changed_during_pregnancy":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].dosage_changed_during_pregnancy = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/if_yes_mental_health_provider_consultation_during_this_pregnancy":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].if_yes_mental_health_provider_consultation_during_this_pregnancy = value;
                    result = true;
            break;
            case "mental_health_profile/documented_preexisting_mental_health_conditions/did_patient_adhere_to_treatment":
                    mental_health_profile.documented_preexisting_mental_health_conditions[index].did_patient_adhere_to_treatment = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/gestational_weeks":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].gestational_weeks = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/gestational_days":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].gestational_days = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/days_postpartum":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].days_postpartum = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/screening_tool":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].screening_tool = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/referral_for_treatment":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].referral_for_treatment = value;
                    result = true;
            break;
            case "committee_review/committee_determination_of_causes_of_death/type":
                    committee_review.committee_determination_of_causes_of_death[index].type = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/class":
                    committee_review.critical_factors_worksheet[index].@class = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/category":
                    committee_review.critical_factors_worksheet[index].category = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/recommendation_level":
                    committee_review.critical_factors_worksheet[index].recommendation_level = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/prevention":
                    committee_review.critical_factors_worksheet[index].prevention = value;
                    result = true;
            break;
            case "committee_review/critical_factors_worksheet/impact_level":
                    committee_review.critical_factors_worksheet[index].impact_level = value;
                    result = true;
            break;
            case "committee_review/recommendations_of_committee/prevention":
                    committee_review.recommendations_of_committee[index].prevention = value;
                    result = true;
            break;
            case "committee_review/recommendations_of_committee/impact_level":
                    committee_review.recommendations_of_committee[index].impact_level = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetSG_Boolean(string path, int index, bool? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetSG_List_Of_Double(string path, int index, List<double> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    
    public bool SetSG_List_Of_String(string path, int index, List<string> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetSG_Datetime(string path, int index, DateTime? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


    public bool SetSG_Date_Only(string path, int index, DateOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "social_and_environmental_profile/details_of_incarcerations/date":
                    social_and_environmental_profile.details_of_incarcerations[index].date = value;
                    result = true;
            break;
            case "social_and_environmental_profile/details_of_arrests/date_of_arrest":
                    social_and_environmental_profile.details_of_arrests[index].date_of_arrest = value;
                    result = true;
            break;
            case "social_and_environmental_profile/social_and_medical_referrals/date":
                    social_and_environmental_profile.social_and_medical_referrals[index].date = value;
                    result = true;
            break;
            case "social_and_environmental_profile/sources_of_social_services_information_for_this_record/date":
                    social_and_environmental_profile.sources_of_social_services_information_for_this_record[index].date = value;
                    result = true;
            break;
            case "prenatal/prior_surgical_procedures_before_pregnancy/date":
                    prenatal.prior_surgical_procedures_before_pregnancy[index].date = value;
                    result = true;
            break;
            case "prenatal/pregnancy_history/details_grid/date_ended":
                    prenatal.pregnancy_history.details_grid[index].date_ended = value;
                    result = true;
            break;
            case "prenatal/routine_monitoring/date_and_time":
                    prenatal.routine_monitoring[index].date_and_time = value;
                    result = true;
            break;
            case "prenatal/other_lab_tests/date_and_time":
                    prenatal.other_lab_tests[index].date_and_time = value;
                    result = true;
            break;
            case "prenatal/diagnostic_procedures/date":
                    prenatal.diagnostic_procedures[index].date = value;
                    result = true;
            break;
            case "prenatal/problems_identified_grid/date_1st_noted":
                    prenatal.problems_identified_grid[index].date_1st_noted = value;
                    result = true;
            break;
            case "prenatal/medications_and_drugs_during_pregnancy/date":
                    prenatal.medications_and_drugs_during_pregnancy[index].date = value;
                    result = true;
            break;
            case "prenatal/pre_delivery_hospitalizations_details/date":
                    prenatal.pre_delivery_hospitalizations_details[index].date = value;
                    result = true;
            break;
            case "prenatal/medical_referrals/date":
                    prenatal.medical_referrals[index].date = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/begin_date":
                    prenatal.other_sources_of_prenatal_care[index].begin_date = value;
                    result = true;
            break;
            case "prenatal/other_sources_of_prenatal_care/end_date":
                    prenatal.other_sources_of_prenatal_care[index].end_date = value;
                    result = true;
            break;
            case "mental_health_profile/were_there_documented_mental_health_conditions/date_of_screening":
                    mental_health_profile.were_there_documented_mental_health_conditions[index].date_of_screening = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


    public bool SetSG_Time_Only(string path, int index, TimeOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


}