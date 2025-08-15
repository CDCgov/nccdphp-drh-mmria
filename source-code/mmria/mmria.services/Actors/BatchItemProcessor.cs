using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;
using System.Globalization;

namespace RecordsProcessor_Worker.Actors;

public sealed class BatchItemProcessor : ReceiveActor
{
    Dictionary<string, mmria.common.metadata.value_node[]> lookup;
    static Dictionary<string, string> IJE_to_MMRIA_Path = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        #region MOR Mappings
        { "DState","home_record/state" }, 
        //3 home_record/date_of_death - DOD_YR, DOD_MO, DOD_DY
        //{ "DOD_YR", "home_record/date_of_death/year"},
        //{ "DOD_MO", "home_record/date_of_death/month"},
        //{ "DOD_DY", "home_record/date_of_death/day"},
        {"DOD_YR","home_record/date_of_death/year"},
        {"DOD_MO","home_record/date_of_death/month"},
        {"DOD_DY","home_record/date_of_death/day"},


        //4 death_certificate/date_of_birth - DOB_YR, DOB_MO, DOD_DY
        { "DOB_YR", "death_certificate/demographics/date_of_birth/year"},
        { "DOB_MO", "death_certificate/demographics/date_of_birth/month"},
        { "DOB_DY", "death_certificate/demographics/date_of_birth/day"},
        //5 home_record/last_name - LNAME  
        { "LNAME", "home_record/last_name"}, 
        //6 home_record/first_name - GNAME*/}
        { "GNAME", "home_record/first_name" },

        {"HR_CDC_OTHER", "home_record/automated_vitals_group/hr_cdc_other"},

        //Rest of Mor mappings
        //{"DOD_YR","home_record/date_of_death/Year"},
        //{"DSTATE","home_record/state_of_death_record"},
        { "FILENO","death_certificate/certificate_identification/state_file_number"},
        { "AUXNO","death_certificate/certificate_identification/local_file_number"},
        //{"GNAME","home_record/first_name"},
        //{"LNAME","home_record/last_name"},
        { "AGE","death_certificate/demographics/age"},
        { "DMAIDEN","death_certificate/certificate_identification/dmaiden"},

        //{"DOB_YR","death_certificate/demographics/date_of_birth/year"},
        //{"DOB_MO","death_certificate/demographics/date_of_birth/month"},
        //{"DOB_DY","death_certificate/demographics/date_of_birth/day"},
        { "BPLACE_CNT","death_certificate/demographics/country_of_birth"},
        { "BPLACE_ST","death_certificate/demographics/state_of_birth"},
        { "STATEC","death_certificate/place_of_last_residence/state"},
        { "COUNTRYC","death_certificate/place_of_last_residence/country_of_last_residence"},
        { "MARITAL","death_certificate/demographics/marital_status"},

        { "DPLACE","death_certificate/death_information/death_occured_in_hospital"},
        { "DPLACE_Outside_of_hospital","death_certificate/death_information/death_outside_of_hospital"},

        { "TOD","death_certificate/certificate_identification/time_of_death"},
        { "DEDUC","death_certificate/demographics/education_level"},

        { "DETHNIC_is_of_hispanic_origin","death_certificate/demographics/is_of_hispanic_origin"},
        //{ "DETHNIC1","death_certificate/demographics/is_of_hispanic_origin"},
        //{ "DETHNIC2","death_certificate/demographics/is_of_hispanic_origin"},
        //{ "DETHNIC3","death_certificate/demographics/is_of_hispanic_origin"},
        //{ "DETHNIC4","death_certificate/demographics/is_of_hispanic_origin"},

        //TODO: James I need the new MMRIA fields for these
        { "DETHNIC5","death_certificate/demographics/is_of_hispanic_origin_other_specify"},

        { "RACE","death_certificate/race/race"},

        //{ "RACE1","death_certificate/race/race"},
        //{ "RACE2","death_certificate/race/race"},
        //{ "RACE3","death_certificate/race/race"},
        //{ "RACE4","death_certificate/race/race"},
        //{ "RACE5","death_certificate/race/race"},
        //{ "RACE6","death_certificate/race/race"},
        //{ "RACE7","death_certificate/race/race"},
        //{ "RACE8","death_certificate/race/race"},
        //{ "RACE9","death_certificate/race/race"},
        //{ "RACE10","death_certificate/race/race"},
        //{ "RACE11","death_certificate/race/race"},
        //{ "RACE12","death_certificate/race/race"},
        //{ "RACE13","death_certificate/race/race"},
        //{ "RACE14","death_certificate/race/race"},
        //{ "RACE15","death_certificate/race/race"},

        { "RACE_Principal_Tribe","death_certificate/race/principle_tribe"},

        //{ "RACE16","death_certificate/race/principle_tribe"},
        //{ "RACE17","death_certificate/race/principle_tribe"},

        { "RACE_other_asian","death_certificate/race/other_asian"},

        //{ "RACE18","death_certificate/race/other_asian"},
        //{ "RACE19","death_certificate/race/other_asian"},

        { "RACE_other_pacific_islander","death_certificate/race/other_pacific_islander"},

        //{ "RACE20","death_certificate/race/other_pacific_islander"},
        //{ "RACE21","death_certificate/race/other_pacific_islander"},

        { "RACE_other_race","death_certificate/race/other_race"},

        //{ "RACE22","death_certificate/race/other_race"},
        //{ "RACE23","death_certificate/race/other_race"},

        { "OCCUP","death_certificate/demographics/primary_occupation"},
        { "INDUST","death_certificate/demographics/occupation_business_industry"},
        { "MANNER","death_certificate/death_information/manner_of_death"},

        { "MAN_UC","death_certificate/vitals_import_group/man_uc"},
        { "ACME_UC","death_certificate/vitals_import_group/acme_uc"},
        { "EAC","death_certificate/vitals_import_group/eac"},
        { "RAC","death_certificate/vitals_import_group/rac"},

        { "AUTOP","death_certificate/death_information/was_autopsy_performed"},
        { "AUTOPF","death_certificate/death_information/was_autopsy_used_for_death_coding"},
        { "TOBAC","death_certificate/death_information/did_tobacco_contribute_to_death"},
        { "PREG","death_certificate/death_information/pregnancy_status"},
        { "DOI_MO","death_certificate/injury_associated_information/date_of_injury/month"},
        { "DOI_DY","death_certificate/injury_associated_information/date_of_injury/day"},
        { "DOI_YR","death_certificate/injury_associated_information/date_of_injury/year"},
        { "TOI_HR","death_certificate/injury_associated_information/time_of_injury"},
        { "WORKINJ","death_certificate/injury_associated_information/was_injury_at_work"},

        { "ARMEDF","death_certificate/demographics/ever_in_us_armed_forces"},
        { "DINSTI","death_certificate/address_of_death/place_of_death"},

        { "ADDRESS_OF_DEATH_street","death_certificate/address_of_death/street"},

        { "CITYTEXT_D","death_certificate/address_of_death/city"},
        { "STATETEXT_D","death_certificate/address_of_death/state"},
        { "ZIP9_D","death_certificate/address_of_death/zip_code"},
        { "COUNTYTEXT_D","death_certificate/address_of_death/county"},

        { "PLACE_OF_LAST_RESIDENCE_street","death_certificate/place_of_last_residence/street"},

        { "UNITNUM_R","death_certificate/place_of_last_residence/apartment"},
        { "CITYTEXT_R","death_certificate/place_of_last_residence/city"},
        { "ZIP9_R","death_certificate/place_of_last_residence/zip_code"},
        { "COUNTYTEXT_R","death_certificate/place_of_last_residence/county"},
        { "DMIDDLE","home_record/middle_name"},
        { "POILITRL","death_certificate/injury_associated_information/place_of_injury"},

        { "TRANSPRT","death_certificate/injury_associated_information/transportation_related_injury"},
        { "TRANSPRT_other_specify","death_certificate/injury_associated_information/transport_related_other_specify"},

        { "COUNTYTEXT_I","death_certificate/address_of_injury/county"},
        { "CITYTEXT_I","death_certificate/address_of_injury/city"},

        { "COD1A","death_certificate/vitals_import_group/cod1a"},
        { "INTERVAL1A","death_certificate/vitals_import_group/interval1a"},
        { "COD1B","death_certificate/vitals_import_group/cod1b"},
        { "INTERVAL1B","death_certificate/vitals_import_group/interval1b"},
        { "COD1C","death_certificate/vitals_import_group/cod1c"},
        { "INTERVAL1C","death_certificate/vitals_import_group/interval1c"},
        { "COD1D","death_certificate/vitals_import_group/cod1d"},
        { "INTERVAL1D","death_certificate/vitals_import_group/interfval1d"},
        { "OTHERCONDITION","death_certificate/vitals_import_group/othercondition"},

        { "DBPLACECITY","death_certificate/demographics/city_of_birth"},
        { "STINJURY","death_certificate/address_of_injury/state"},

        { "VRO_STATUS","home_record/automated_vitals_group/vro_status"},
        { "BC_DET_MATCH","home_record/automated_vitals_group/bc_det_match"},
        { "FDC_DET_MATCH","home_record/automated_vitals_group/fdc_det_match"},
        { "BC_PROB_MATCH","home_record/automated_vitals_group/bc_prob_match"},
        { "FDC_PROB_MATCH","home_record/automated_vitals_group/fdc_prob_match"},
        { "ICD10_MATCH","home_record/automated_vitals_group/icd10_match"},
        { "PREGCB_MATCH","home_record/automated_vitals_group/pregcb_match"},
        { "LITERALCOD_MATCH","home_record/automated_vitals_group/literalcod_match"},
    #endregion
    };

    //NAT and FET have different record fields
    static Dictionary<string, string> Parent_NAT_IJE_to_MMRIA_Path = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"STATEC","birth_fetal_death_certificate_parent/location_of_residence/state"},
        {"IDOB_YR","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year"},
        {"IDOB_MO","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month"},
        {"IDOB_DY","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day"},
        {"FNPI","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_npi_number"},
        {"MDOB_YR","birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year"},
        {"MDOB_MO","birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month"},
        {"MDOB_DY","birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day"},
        {"FDOB_YR","birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/year"},
        {"FDOB_MO","birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/month"},
        {"MARN","birth_fetal_death_certificate_parent/demographic_of_mother/mother_married"},
        {"ACKN","birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital"},
        {"MEDUC","birth_fetal_death_certificate_parent/demographic_of_mother/education_level"},
        {"FEDUC","birth_fetal_death_certificate_parent/demographic_of_father/education_level"},
        {"ATTEND","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_type"},
        {"TRAN","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_mother_transferred"},
        {"NPREV","birth_fetal_death_certificate_parent/prenatal_care/number_of_visits"},
        {"HFT","birth_fetal_death_certificate_parent/maternal_biometrics/height_feet"},
        {"HIN","birth_fetal_death_certificate_parent/maternal_biometrics/height_inches"},
        {"PWGT","birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight"},
        {"DWGT","birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery"},
        {"WIC","birth_fetal_death_certificate_parent/prenatal_care/was_wic_used"},
        {"PLBL","birth_fetal_death_certificate_parent/pregnancy_history/now_living"},
        {"PLBD","birth_fetal_death_certificate_parent/pregnancy_history/now_dead"},
        {"POPO","birth_fetal_death_certificate_parent/pregnancy_history/other_outcomes"},
        {"MLLB","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month"},
        {"YLLB","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year"},
        {"MOPO","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month"},
        {"YOPO","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year"},
        {"PAY","birth_fetal_death_certificate_parent/prenatal_care/principal_source_of_payment_for_this_delivery"},
        {"DLMP_YR","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year"},
        {"DLMP_MO","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month"},
        {"DLMP_DY","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day"},
        {"NPCES","birth_fetal_death_certificate_parent/risk_factors/number_of_c_sections"},
        {"OWGEST","birth_fetal_death_certificate_parent/prenatal_care/obsteric_estimate_of_gestation"},
            {"BIRTH_CO","birth_fetal_death_certificate_parent/facility_of_delivery_location/county"},
        {"BRTHCITY","birth_fetal_death_certificate_parent/facility_of_delivery_location/city"},
        {"HOSP","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name"},
        {"MOMFNAME","birth_fetal_death_certificate_parent/record_identification/first_name"},
        {"MOMMIDDL","birth_fetal_death_certificate_parent/record_identification/middle_name"},
        {"MOMLNAME","birth_fetal_death_certificate_parent/record_identification/last_name"},
        {"MOMMAIDN","birth_fetal_death_certificate_parent/record_identification/maiden_name"},
        {"LOCATION_OF_RESIDENCE_street","birth_fetal_death_certificate_parent/location_of_residence/street"},
        {"UNUM","birth_fetal_death_certificate_parent/location_of_residence/apartment"},
        {"ZIPCODE","birth_fetal_death_certificate_parent/location_of_residence/zip_code"},
        {"COUNTYTXT","birth_fetal_death_certificate_parent/location_of_residence/county"},
        {"CITYTEXT","birth_fetal_death_certificate_parent/location_of_residence/city"},
        {"MOM_OC_T","birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation"},
        {"DAD_OC_T","birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation"},
        {"MOM_IN_T","birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry"},
        {"DAD_IN_T","birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry"},
        {"HOSPFROM","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/transferred_from_where"},
        {"ATTEND_OTH_TXT","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_attendant_type"},
        {"ATTEND_NPI","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_npi"},
        {"MOM_MED_REC_NUM","birth_fetal_death_certificate_parent/record_identification/medical_record_number"},

        {"BSTATE","birth_fetal_death_certificate_parent/facility_of_delivery_location/state"},

        {"BPLACE","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/type_of_place"},
        {"BPLACE_was_home_delivery_planned","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_home_delivery_planned"},

        {"BPLACEC_ST_TER","birth_fetal_death_certificate_parent/demographic_of_mother/state_of_birth"},
        {"BPLACEC_CNT","birth_fetal_death_certificate_parent/demographic_of_mother/country_of_birth"},

        {"METHNIC","birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin"},

                    {"METHNIC5","birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin_other_specify"},
        {"FETHNIC5","birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin_other_specify"},

        {"MRACE","birth_fetal_death_certificate_parent/race/race_of_mother"},

        {"MRACE16_17","birth_fetal_death_certificate_parent/race/principle_tribe"},

        {"MRACE18_19","birth_fetal_death_certificate_parent/race/other_asian"},

        {"MRACE20_21","birth_fetal_death_certificate_parent/race/other_pacific_islander"},

        {"MRACE22_23","birth_fetal_death_certificate_parent/race/other_race"},

        {"FETHNIC","birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin"},

        {"FRACE","birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father"},

        {"FRACE16_17","birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe"},

        {"FRACE18_19","birth_fetal_death_certificate_parent/demographic_of_father/race/other_asian"},

        {"FRACE20_21","birth_fetal_death_certificate_parent/demographic_of_father/race/other_pacific_islander"},

        {"FRACE22_23","birth_fetal_death_certificate_parent/demographic_of_father/race/other_race"},


        {"DOFP_MO","birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/month"},
        {"DOFP_MO_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},


        {"DOFP_DY","birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/day"},
        {"DOFP_DY_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},

        {"DOFP_YR","birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/year"},
        {"DOFP_YR_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},

        {"DOLP_MO","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/month"},
        {"DOLP_MO_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},

        {"DOLP_DY","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/day"},
        {"DOLP_DY_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},

        {"DOLP_YR","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/year"},
        {"DOLP_YR_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},


        {"CIGPN","birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months"},
        {"CIGPN_prior_3_months_type","birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months_type"},
        //{"CIGPN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIGFN","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st"},
        {"CIGFN_trimester_1st_type","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st_type"},
        //{"CIGFN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIGSN","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd"},
        {"CIGSN_trimester_2nd_type","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd_type"},
        //{"CIGSN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIGLN","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd"},
        {"CIGLN_trimester_3rd_type","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd_type"},
        //{"CIGLN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIG_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},


        {"risk_factors_in_this_pregnancy","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},

        {"infections_present_or_treated_during_pregnancy","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},

        {"obstetric_procedures","birth_fetal_death_certificate_parent/obstetric_procedures"},

        {"onset_of_labor","birth_fetal_death_certificate_parent/onset_of_labor"},

        {"characteristics_of_labor_and_delivery","birth_fetal_death_certificate_parent/characteristics_of_labor_and_delivery"},

        {"maternal_morbidity","birth_fetal_death_certificate_parent/maternal_morbidity"},

        {"MAGER","birth_fetal_death_certificate_parent/demographic_of_mother/age"},
        {"FAGER","birth_fetal_death_certificate_parent/demographic_of_father/age"},
        {"EHYPE","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
        //{"INFT_DRG","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
        //{"INFT_ART","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
        {"FBPLACD_ST_TER_C","birth_fetal_death_certificate_parent/demographic_of_father/state_of_birth"},
        {"FBPLACE_CNT_C","birth_fetal_death_certificate_parent/demographic_of_father/father_country_of_birth"},

        {"PLUR","birth_fetal_death_certificate_parent/prenatal_care/plurality"},
        {"PLUR_specify_if_greater_than_3","birth_fetal_death_certificate_parent/prenatal_care/specify_if_greater_than_3"},
    };

    static Dictionary<string, string> Parent_FET_IJE_to_MMRIA_Path = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"STATEC","birth_fetal_death_certificate_parent/location_of_residence/state"},
        {"FDOD_YR","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year"},
        {"FDOD_MO","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month"},
        {"FDOD_DY","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day"},
        {"FNPI","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_npi_number"},
        {"MDOB_YR","birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year"},
        {"MDOB_MO","birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month"},
        {"MDOB_DY","birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day"},
        {"FDOB_YR","birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/year"},
        {"FDOB_MO","birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/month"},
        {"MARN","birth_fetal_death_certificate_parent/demographic_of_mother/mother_married"},
        {"MEDUC","birth_fetal_death_certificate_parent/demographic_of_mother/education_level"},
        {"ATTEND","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_type"},
        {"TRAN","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_mother_transferred"},
        {"NPREV","birth_fetal_death_certificate_parent/prenatal_care/number_of_visits"},
        {"HFT","birth_fetal_death_certificate_parent/maternal_biometrics/height_feet"},
        {"HIN","birth_fetal_death_certificate_parent/maternal_biometrics/height_inches"},
        {"PWGT","birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight"},
        {"DWGT","birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery"},
        {"WIC","birth_fetal_death_certificate_parent/prenatal_care/was_wic_used"},
        {"PLBL","birth_fetal_death_certificate_parent/pregnancy_history/now_living"},
        {"PLBD","birth_fetal_death_certificate_parent/pregnancy_history/now_dead"},
        {"POPO","birth_fetal_death_certificate_parent/pregnancy_history/other_outcomes"},
        {"MLLB","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month"},
        {"YLLB","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year"},
        {"MOPO","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month"},
        {"YOPO","birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year"},
        {"DLMP_YR","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year"},
        {"DLMP_MO","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month"},
        {"DLMP_DY","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day"},
        {"NPCES","birth_fetal_death_certificate_parent/risk_factors/number_of_c_sections"},
        {"OWGEST","birth_fetal_death_certificate_parent/prenatal_care/obsteric_estimate_of_gestation"},
        {"HOSP_D","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name"},
        {"ADDRESS_D","birth_fetal_death_certificate_parent/facility_of_delivery_location/street"},
        {"ZIPCODE_D","birth_fetal_death_certificate_parent/facility_of_delivery_location/zip_code"},
        {"CNTY_D","birth_fetal_death_certificate_parent/facility_of_delivery_location/county"},
        {"CITY_D","birth_fetal_death_certificate_parent/facility_of_delivery_location/city"},
        {"MOMFNAME","birth_fetal_death_certificate_parent/record_identification/first_name"},
        {"MOMMNAME","birth_fetal_death_certificate_parent/record_identification/middle_name"},
        {"MOMLNAME","birth_fetal_death_certificate_parent/record_identification/last_name"},
        {"MOMMAIDN","birth_fetal_death_certificate_parent/record_identification/maiden_name"},
        {"LOCATION_OF_RESIDENCE_street","birth_fetal_death_certificate_parent/location_of_residence/street"},
        {"APTNUMB","birth_fetal_death_certificate_parent/location_of_residence/apartment"},
        {"ZIPCODE","birth_fetal_death_certificate_parent/location_of_residence/zip_code"},
        {"COUNTYTXT","birth_fetal_death_certificate_parent/location_of_residence/county"},
        {"CITYTXT","birth_fetal_death_certificate_parent/location_of_residence/city"},
        {"MOM_OC_T","birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation"},
        {"DAD_OC_T","birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation"},
        {"MOM_IN_T","birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry"},
        {"DAD_IN_T","birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry"},
        {"FEDUC","birth_fetal_death_certificate_parent/demographic_of_father/education_level"},
        {"HOSPFROM","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/transferred_from_where"},
        {"ATTEND_NPI","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_npi"},
        {"ATTEND_OTH_TXT","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_attendant_type"},
        {"METHNIC5","birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin_other_specify"},
        {"FETHNIC5","birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin_other_specify"},




        {"DSTATE","birth_fetal_death_certificate_parent/facility_of_delivery_location/state"},
        {"DPLACE","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/type_of_place"},
        {"DPLACE_was_home_delivery_planned","birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_home_delivery_planned"},
        {"BPLACEC_ST_TER","birth_fetal_death_certificate_parent/demographic_of_mother/state_of_birth"},
        {"BPLACEC_CNT","birth_fetal_death_certificate_parent/demographic_of_mother/country_of_birth"},


        {"METHNIC","birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin"},


        {"MRACE","birth_fetal_death_certificate_parent/race/race_of_mother"},

        {"MRACE16_17","birth_fetal_death_certificate_parent/race/principle_tribe"},

        {"MRACE18_19","birth_fetal_death_certificate_parent/race/other_asian"},

        {"MRACE20_21","birth_fetal_death_certificate_parent/race/other_pacific_islander"},

        {"MRACE22_23","birth_fetal_death_certificate_parent/race/other_race"},



        {"DOFP_MO","birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/month" },
        {"DOFP_MO_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},
        {"DOFP_DY","birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/day"},
        {"DOFP_DY_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},
        {"DOFP_YR","birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/year"},
        {"DOFP_YR_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},
        {"DOLP_MO","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/month"},
        {"DOLP_MO_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},
        {"DOLP_DY","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/day"},
        {"DOLP_DY_trimester_of_1st_prenatal_care_visit","bbirth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},
        {"DOLP_YR","birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/year"},
        {"DOLP_YR_trimester_of_1st_prenatal_care_visit","birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit"},
        {"CIGPN","birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months"},
        {"CIGPN_prior_3_months_type","birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months_type"},
        {"CIGPN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIGFN","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st"},
        {"CIGFN_trimester_1st_type","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st_type"},
        //{"CIGFN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIGSN","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd"},
        {"CIGSN_trimester_2nd_type","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd_type"},
        //{"CIGSN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},

        {"CIGLN","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd"},
        {"CIGLN_trimester_3rd_type","birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd_type"},
        //{"CIGLN_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},


        {"CIG_none_or_not_specified","birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified"},



        {"risk_factors_in_this_pregnancy","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},

        {"infections_present_or_treated_during_pregnancy","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},

        //{"obstetric_procedures","birth_fetal_death_certificate_parent/obstetric_procedures"},

        //{"onset_of_labor","birth_fetal_death_certificate_parent/onset_of_labor"},

        //{"characteristics_of_labor_and_delivery","birth_fetal_death_certificate_parent/characteristics_of_labor_and_delivery"},

        {"maternal_morbidity","birth_fetal_death_certificate_parent/maternal_morbidity"},

//{"PDIAB","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"GDIAB","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"PHYPE","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"GHYPE","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"PPB","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"PPO","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"INFT","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
//{"PCES","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},

//{"GON","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"SYPH","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"HSV","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"CHAM","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"LM","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"GBS","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"CMV","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"B19","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"TOXO","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
//{"OTHERI","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},

//{"MTR","birth_fetal_death_certificate_parent/maternal_morbidity"},
//{"PLAC","birth_fetal_death_certificate_parent/maternal_morbidity"},
//{"RUT","birth_fetal_death_certificate_parent/maternal_morbidity"},
//{"UHYS","birth_fetal_death_certificate_parent/maternal_morbidity"},
//{"AINT","birth_fetal_death_certificate_parent/maternal_morbidity"},


        {"PLUR","birth_fetal_death_certificate_parent/prenatal_care/plurality"},
        {"PLUR_specify_if_greater_than_3","birth_fetal_death_certificate_parent/prenatal_care/specify_if_greater_than_3"},
        //{"UOPR","birth_fetal_death_certificate_parent/maternal_morbidity"},

        {"MAGER","birth_fetal_death_certificate_parent/demographic_of_mother/age"},
        {"FAGER","birth_fetal_death_certificate_parent/demographic_of_father/age"},
        //{"EHYPE","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
        //{"INFT_DRG","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
        //{"INFT_ART","birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy"},
        {"HSV1","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
        {"HIV","birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy"},
        {"FBPLACD_ST_TER_C","birth_fetal_death_certificate_parent/demographic_of_father/state_of_birth"},
        {"FBPLACE_CNT_C","birth_fetal_death_certificate_parent/demographic_of_father/father_country_of_birth"},


        {"FETHNIC","birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin"},

        {"FRACE","birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father"},

        {"FRACE16_17","birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe"},

        {"FRACE18_19","birth_fetal_death_certificate_parent/demographic_of_father/race/other_asian"},

        {"FRACE20_21","birth_fetal_death_certificate_parent/demographic_of_father/race/other_pacific_islander"},

        {"FRACE22_23","birth_fetal_death_certificate_parent/demographic_of_father/race/other_race"},


    };

    static Dictionary<string, string> NAT_IJE_to_MMRIA_Path = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        #region NAT Mappings

        {"DATE_OF_DELIVERY","birth_certificate_infant_fetal_section/record_identification/date_of_delivery"},
        {"FILENO","birth_certificate_infant_fetal_section/record_identification/state_file_number"},
        {"AUXNO","birth_certificate_infant_fetal_section/record_identification/local_file_number"},
        {"TB","birth_certificate_infant_fetal_section/record_identification/time_of_delivery"},

        {"ATTF","birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_forceps_attempted_but_unsuccessful"},
        {"ATTV","birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_vacuum_extration_attempted_but_unsuccessful"},
        {"PRES","birth_certificate_infant_fetal_section/method_of_delivery/fetal_delivery"},
        {"ROUT","birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery"},
        {"APGAR5","birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_5"},
        {"APGAR10","birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_10"},
        {"SORD","birth_certificate_infant_fetal_section/birth_order"},
        {"ITRAN","birth_certificate_infant_fetal_section/biometrics_and_demographics/was_infant_transferred_within_24_hours"},
        {"ILIV","birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_living_at_time_of_report"},
        {"BFED","birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_being_breastfed_at_discharge"},
        {"HOSPTO","birth_certificate_infant_fetal_section/biometrics_and_demographics/facility_city_state"},
        {"INF_MED_REC_NUM","birth_certificate_infant_fetal_section/record_identification/newborn_medical_record_number"},

        {"PLUR_is_multiple_gestation","birth_certificate_infant_fetal_section/is_multiple_gestation"},
            {"BWG_unit_of_measurement","birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/unit_of_measurement"},
            {"BWG","birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/grams_or_pounds"},
            

            {"abnormal_conditions_of_newborn","birth_certificate_infant_fetal_section/abnormal_conditions_of_newborn"},



            {"congenital_anomalies","birth_certificate_infant_fetal_section/congenital_anomalies"},



            {"TLAB","birth_certificate_infant_fetal_section/method_of_delivery/if_cesarean_was_trial_of_labor_attempted"},
            {"RECORD_TYPE","birth_certificate_infant_fetal_section/record_type"},
            {"ISEX","birth_certificate_infant_fetal_section/biometrics_and_demographics/gender"},
/*
{"COD18a1", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a1"},
{"COD18a2", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a2"},
{"COD18a3", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a3"},
{"COD18a4", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a4"},
{"COD18a5", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a5"},
{"COD18a6", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a6"},
{"COD18a7", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a7"},
{"COD18a8", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a8"},
{"COD18a9", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a9"},
{"COD18a10", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a10"},
{"COD18a11", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a11"},
{"COD18a12", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a12"},
{"COD18a13", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a13"},
{"COD18a14", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a14"},
{"COD18b1", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b1"},
{"COD18b2", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b2"},
{"COD18b3", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b3"},
{"COD18b4", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b4"},
{"COD18b5", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b5"},
{"COD18b6", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b6"},
{"COD18b7", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b7"},
{"COD18b8", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b8"},
{"COD18b9", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b9"},
{"COD18b10", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b10"},
{"COD18b11", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b11"},
{"COD18b12", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b12"},
{"COD18b13", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b13"},
{"COD18b14", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b14"},
{"ICOD", "birth_certificate_infant_fetal_section/vitals_import_group/icod"},
{"OCOD1", "birth_certificate_infant_fetal_section/vitals_import_group/ocod1"},
{"OCOD2", "birth_certificate_infant_fetal_section/vitals_import_group/ocod2"},
{"OCOD3", "birth_certificate_infant_fetal_section/vitals_import_group/ocod3"},
{"OCOD4", "birth_certificate_infant_fetal_section/vitals_import_group/ocod4"},
{"OCOD5", "birth_certificate_infant_fetal_section/vitals_import_group/ocod5"},
{"OCOD6", "birth_certificate_infant_fetal_section/vitals_import_group/ocod6"},
{"OCOD7", "birth_certificate_infant_fetal_section/vitals_import_group/ocod7"}
*/

        #endregion
    };

    static Dictionary<string, string> FET_IJE_to_MMRIA_Path = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        #region FET Mappings

        {"DATE_OF_DELIVERY","birth_certificate_infant_fetal_section/record_identification/date_of_delivery"},
        {"FILENO","birth_certificate_infant_fetal_section/record_identification/state_file_number"},
        {"AUXNO","birth_certificate_infant_fetal_section/record_identification/local_file_number"},
        {"TD","birth_certificate_infant_fetal_section/record_identification/time_of_delivery"},
        {"ATTF","birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_forceps_attempted_but_unsuccessful"},
        {"ATTV","birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_vacuum_extration_attempted_but_unsuccessful"},
        {"PRES","birth_certificate_infant_fetal_section/method_of_delivery/fetal_delivery"},
        {"ROUT","birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery"},
        {"SORD","birth_certificate_infant_fetal_section/birth_order"},


{"COD18a1", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a1"},
{"COD18a2", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a2"},
{"COD18a3", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a3"},
{"COD18a4", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a4"},
{"COD18a5", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a5"},
{"COD18a6", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a6"},
{"COD18a7", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a7"},
{"COD18a8", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a8"},
{"COD18a9", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a9"},
{"COD18a10", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a10"},
{"COD18a11", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a11"},
{"COD18a12", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a12"},
{"COD18a13", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a13"},
{"COD18a14", "birth_certificate_infant_fetal_section/vitals_import_group/cod18a14"},
{"COD18b1", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b1"},
{"COD18b2", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b2"},
{"COD18b3", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b3"},
{"COD18b4", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b4"},
{"COD18b5", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b5"},
{"COD18b6", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b6"},
{"COD18b7", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b7"},
{"COD18b8", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b8"},
{"COD18b9", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b9"},
{"COD18b10", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b10"},
{"COD18b11", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b11"},
{"COD18b12", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b12"},
{"COD18b13", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b13"},
{"COD18b14", "birth_certificate_infant_fetal_section/vitals_import_group/cod18b14"},
{"ICOD", "birth_certificate_infant_fetal_section/vitals_import_group/icod"},
{"OCOD1", "birth_certificate_infant_fetal_section/vitals_import_group/ocod1"},
{"OCOD2", "birth_certificate_infant_fetal_section/vitals_import_group/ocod2"},
{"OCOD3", "birth_certificate_infant_fetal_section/vitals_import_group/ocod3"},
{"OCOD4", "birth_certificate_infant_fetal_section/vitals_import_group/ocod4"},
{"OCOD5", "birth_certificate_infant_fetal_section/vitals_import_group/ocod5"},
{"OCOD6", "birth_certificate_infant_fetal_section/vitals_import_group/ocod6"},
{"OCOD7", "birth_certificate_infant_fetal_section/vitals_import_group/ocod7"},

            {"FSEX","birth_certificate_infant_fetal_section/biometrics_and_demographics/gender"},
            {"TLAB","birth_certificate_infant_fetal_section/method_of_delivery/if_cesarean_was_trial_of_labor_attempted"},
            {"FWG","birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/grams_or_pounds"},
            {"FWG_unit_of_measurement","birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/unit_of_measurement"},
            {"PLUR_is_multiple_gestation","birth_certificate_infant_fetal_section/is_multiple_gestation"},

            {"congenital_anomalies","birth_certificate_infant_fetal_section/congenital_anomalies"},

            //{"ANEN","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"MNSB","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"CCHD","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"CDH","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"OMPH","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"GAST","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"LIMB","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"CL","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"CP","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"DOWT","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"CDIT","birth_certificate_infant_fetal_section/congenital_anomalies"},
            //{"HYPO","birth_certificate_infant_fetal_section/congenital_anomalies"},
            {"RECORD_TYPE","birth_certificate_infant_fetal_section/record_type"},


        #endregion
    };

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    private string config_timer_user_name = null;
    private string config_timer_value = null;

    private string config_couchdb_url = null;
    private string db_prefix = "";

    
    static HashSet<string> ExistingRecordIds = null;

    mmria.common.couchdb.DBConfigurationDetail item_db_info;

    string geocode_api_key =  "";

    private System.Dynamic.ExpandoObject case_expando_object = null;

    private Dictionary<string, string> StateDisplayToValue;

    private string location_of_residence_latitude = null;
    private string location_of_residence_longitude = null;
    private string facility_of_delivery_location_latitude = null;
    private string facility_of_delivery_location_longitude = null;

    private string death_certificate_place_of_last_residence_latitude = null;
    private string death_certificate_place_of_last_residence_longitude = null;
    private string death_certificate_address_of_death_latitude = null;
    private string death_certificate_address_of_death_longitude = null;

    public BatchItemProcessor()
    {
        Receive<mmria.common.ije.StartBatchItemMessage>(message =>
        {    
            Console.WriteLine("Message Recieved");
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            Sender.Tell("Message Recieved");
            Process_Message(message);
        });
    }

    private void Process_Message(mmria.common.ije.StartBatchItemMessage message)
    {

        config_timer_user_name = mmria.services.vitalsimport.Program.timer_user_name;
        config_timer_value = mmria.services.vitalsimport.Program.timer_value;

        config_couchdb_url = mmria.services.vitalsimport.Program.couchdb_url;
        db_prefix = "";

        mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
        item_db_info = db_config_set.detail_list[message.host_state];
        geocode_api_key = db_config_set.name_value["geocode_api_key"];

        var mor_field_set = mor_get_header(message.mor);

        //get parent header set fet/nat

        var nat_field_set = nat_get_header(message.nat);

        var fet_field_set = fet_get_header(message.fet);


        string metadata_url = $"{mmria.services.vitalsimport.Program.couchdb_url}/metadata/version_specification-{db_config_set.name_value["metadata_version"]}/metadata";
        var metadata_curl = new mmria.getset.cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());

        lookup = get_look_up(metadata);

        StateDisplayToValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in lookup["lookup/state"])
        {
            StateDisplayToValue.Add(kvp.display, kvp.value);
        }

        var is_case_already_present = false;




        var case_view_response = GetCaseView(item_db_info, mor_field_set["LNAME"].Trim());
        string mmria_id = null;

        var gs = new migrate.C_Get_Set_Value(new System.Text.StringBuilder());

        string record_id = null;

        /*if (case_view_response == null)
        {

        }
        else      */
        if (case_view_response.total_rows > 0)
        {
            int dod_yr = -1;
            int dod_mo = -1;
            int dod_dy = -1;

            int dob_yr = -1;
            int dob_mo = -1;
            int dob_dy = -1;

            int.TryParse(mor_field_set["DOD_YR"], out dod_yr);
            int.TryParse(mor_field_set["DOD_MO"], out dod_mo);
            int.TryParse(mor_field_set["DOD_DY"], out dod_dy);

            int.TryParse(mor_field_set["DOB_YR"], out dob_yr);
            int.TryParse(mor_field_set["DOB_MO"], out dob_mo);
            int.TryParse(mor_field_set["DOB_DY"], out dob_dy);



            foreach (var kvp in case_view_response.rows)
            {


                if
                (
                    kvp.value.host_state.Trim().Equals(message.host_state.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    kvp.value.last_name.Trim().Equals(mor_field_set["LNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                    kvp.value.first_name.Trim().Equals(mor_field_set["GNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                    kvp.value.date_of_death_year == dod_yr &&
                    kvp.value.date_of_death_month == dod_mo

                )
                {
                    var case_expando_object = GetCaseById(item_db_info, kvp.id);
                    if (case_expando_object != null)
                    {

                        migrate.C_Get_Set_Value.get_value_result value_result = gs.get_value(case_expando_object, "_id");
                        mmria_id = value_result.result?.ToString();


                        var DSTATE_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DState"]);
                        var host_state_result = gs.get_value(case_expando_object, "host_state");
                        var DOD_YR_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DOD_YR"]);
                        var DOD_MO_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DOD_MO"]);
                        var DOD_DY_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DOD_DY"]);
                        var DOB_YR_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DOB_YR"]);
                        var DOB_MO_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DOB_MO"]);
                        var DOB_DY_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["DOB_DY"]);
                        var LNAME_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["LNAME"]);
                        var GNAME_result = gs.get_value(case_expando_object, IJE_to_MMRIA_Path["GNAME"]);

                        if
                        (
                            DOD_YR_result.is_error == false &&
                            host_state_result.is_error == false &&
                            DOD_MO_result.is_error == false &&
                            DOD_DY_result.is_error == false &&
                            DOB_YR_result.is_error == false &&
                            DOB_MO_result.is_error == false &&
                            DOB_DY_result.is_error == false &&
                            LNAME_result.is_error == false &&
                            GNAME_result.is_error == false
                        )
                        {
                            var host_state_string = host_state_result.result?.ToString().Trim() ?? "";
                            var LNAME_string = LNAME_result.result?.ToString().Trim() ?? "";
                            var GNAME_string = GNAME_result.result?.ToString().Trim() ?? "";

                            if
                            (
                                host_state_string.Equals(message.host_state, StringComparison.OrdinalIgnoreCase) &&
                                LNAME_string.Equals(mor_field_set["LNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                                GNAME_string.Equals(mor_field_set["GNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                                DOD_YR_result.result!= null &&
                                DOD_MO_result.result!= null &&
                                DOD_DY_result.result!= null &&
                                DOB_YR_result.result!= null &&
                                DOB_MO_result.result!= null &&
                                DOB_DY_result.result!= null
                                

                            )
                            {

                                int DOD_YR_result_Check = -1;
                                int DOD_MO_result_Check = -1;
                                int DOD_DY_result_Check = -1;
                                int DOB_YR_result_Check = -1;
                                int DOB_MO_result_Check = -1;
                                int DOB_DY_result_Check = -1;



                                if(
                                    int.TryParse(DOD_YR_result.result.ToString(), out DOD_YR_result_Check) &&
                                    int.TryParse(DOD_MO_result.result.ToString(), out DOD_MO_result_Check) &&
                                    int.TryParse(DOD_DY_result.result.ToString(), out DOD_DY_result_Check) &&
                                    int.TryParse(DOB_YR_result.result.ToString(), out DOB_YR_result_Check) &&
                                    int.TryParse(DOB_MO_result.result.ToString(), out DOB_MO_result_Check) &&
                                    int.TryParse(DOB_DY_result.result.ToString(), out DOB_DY_result_Check) &&
                                    DOD_YR_result_Check == dod_yr &&
                                    DOD_MO_result_Check == dod_mo &&
                                    DOD_DY_result_Check == dod_dy &&
                                    DOB_YR_result_Check == dob_yr &&
                                    DOB_MO_result_Check == dob_mo &&
                                    DOB_DY_result_Check == dob_dy 
                                )
                                {
                                    var record_id_result = gs.get_value(case_expando_object, "home_record/record_id");
                                    if(!record_id_result.is_error && record_id_result.result!= null)
                                    {
                                        record_id = record_id_result.result.ToString();
                                    }
                                    is_case_already_present = true;
                                    break;
                                }
                                else
                                {
                                    System.Console.WriteLine("inner check 5");
                                }
                            }
                            else
                            {
                                System.Console.WriteLine("inner check 4");
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("inner check 3");
                        }

                    }
                    else
                    {
                        System.Console.WriteLine("inner check 2");
                    }
                }
                else
                {
                    System.Console.WriteLine("inner check 1");
                }
            }

        }
        else
        {
            System.Console.WriteLine("No CaseView Rows found");
        }


        if (is_case_already_present)
        {
            var result = new mmria.common.ije.BatchItem()
            {
                Status = mmria.common.ije.BatchItem.StatusEnum.ExistingCaseSkipped,
                CDCUniqueID = mor_field_set["SSN"].Trim(),
                ImportDate = message.ImportDate,
                ImportFileName = message.ImportFileName,
                ReportingState = message.host_state,

                case_folder = message.case_folder,

                StateOfDeathRecord = mor_field_set["DSTATE"],
                DateOfDeath = $"{mor_field_set["DOD_YR"]}-{mor_field_set["DOD_MO"]}-{mor_field_set["DOD_DY"]}",
                DateOfBirth = $"{mor_field_set["DOB_YR"]}-{mor_field_set["DOB_MO"]}-{mor_field_set["DOB_DY"]}",
                LastName = mor_field_set["LNAME"],
                FirstName = mor_field_set["GNAME"],
                mmria_record_id = record_id,
                mmria_id = mmria_id,
                StatusDetail = "matching case found in database"
            };

            Sender.Tell(result);
        }
        else
        {
            mmria_id = System.Guid.NewGuid().ToString();

            var current_status = new mmria.common.ije.BatchItem()
            {
                Status = mmria.common.ije.BatchItem.StatusEnum.InProcess,
                CDCUniqueID = mor_field_set["SSN"].Trim(),
                mmria_record_id = message.record_id,
                ImportDate = message.ImportDate,
                ImportFileName = message.ImportFileName,
                ReportingState = message.host_state,

                case_folder = message.case_folder,

                StateOfDeathRecord = mor_field_set["DSTATE"],
                DateOfDeath = $"{mor_field_set["DOD_YR"]}-{mor_field_set["DOD_MO"]}-{mor_field_set["DOD_DY"]}",
                DateOfBirth = $"{mor_field_set["DOB_YR"]}-{mor_field_set["DOB_MO"]}-{mor_field_set["DOB_DY"]}",
                LastName = mor_field_set["LNAME"],
                FirstName = mor_field_set["GNAME"],

                mmria_id = mmria_id,
                StatusDetail = "Inprocess of creating new case"
            };

            Sender.Tell(current_status);


            var new_case = new System.Dynamic.ExpandoObject();

            mmria.services.vitalsimport.default_case.create(metadata, new_case);

            var current_date_iso_string = System.DateTime.UtcNow.ToString("o");

            #region MOR Assignments
            gs.set_value("_id", mmria_id, new_case);


            var case_folder = message.case_folder;

            if(string.IsNullOrWhiteSpace(case_folder))
            {
                case_folder = "/";
            }

            gs.set_value("home_record/jurisdiction_id", case_folder, new_case);

            gs.set_value("date_created", current_date_iso_string, new_case);
            gs.set_value("created_by", "vitals-import", new_case);
            gs.set_value("date_last_updated", current_date_iso_string, new_case);
            gs.set_value("last_updated_by", "vitals-import", new_case);
            gs.set_value("version", metadata.version, new_case);
            gs.set_value("host_state", message.host_state, new_case);
            gs.set_value("home_record/state_of_death_record", message.host_state, new_case);
            

            var VitalsImportStatusValue = "0";
            gs.set_value("home_record/case_status/overall_case_status", VitalsImportStatusValue, new_case);

            var test_vro_status = mor_field_set["VRO_STATUS"];
            string vro_staus_value = "9999";
            if
            (
                test_vro_status != null
            )
            {
                var trimmed_value = test_vro_status.Trim();
                if( int.TryParse(trimmed_value, out var test_vro_status_int))
                {
                    var list_values = new HashSet<int>()
                    {
                        0, 1,2,3,4,5,6
                    };

                    if(list_values.Contains(test_vro_status_int))
                    {
                        vro_staus_value = test_vro_status_int.ToString();
                    }
                    else if(test_vro_status_int == 9)
                    {
                        vro_staus_value = "5";
                    }
                }
            }
            //gs.set_value("home_record/automated_vitals_group/vro_status", mor_field_set["VRO_STATUS"], new_case);


            gs.set_value("home_record/automated_vitals_group/vro_status", vro_staus_value, new_case);

            gs.set_value("home_record/record_id", message.record_id, new_case);

            gs.set_value("home_record/record_id", message.record_id, new_case);

            gs.set_value("home_record/automated_vitals_group/import_date", current_date_iso_string, new_case);

            
            //  Vital Report Start
            var hr_cdc_match_det_bc_values = get_metadata_value_node("home_record/automated_vitals_group/bc_det_match", metadata);
            var hr_cdc_match_det_fdc_values = get_metadata_value_node("home_record/automated_vitals_group/fdc_det_match", metadata);
            var hr_cdc_match_prob_bc_values = get_metadata_value_node("home_record/automated_vitals_group/bc_prob_match", metadata);
            var hr_cdc_match_prob_fdc_values = get_metadata_value_node("home_record/automated_vitals_group/fdc_prob_match", metadata);
            var hr_cdc_icd_values = get_metadata_value_node("home_record/automated_vitals_group/icd10_match", metadata);
            var hr_cdc_checkbox_values = get_metadata_value_node("home_record/automated_vitals_group/pregcb_match", metadata);
            var hr_cdc_literalcod_values = get_metadata_value_node("home_record/automated_vitals_group/literalcod_match", metadata);
            var hr_cdc_other_values =  get_metadata_value_node("home_record/automated_vitals_group/hr_cdc_other", metadata);

            var hr_cdc_match_det_bc = hr_cdc_match_det_bc_values.Where(x=> x.value == mor_field_set["BC_DET_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_match_det_fdc = hr_cdc_match_det_fdc_values.Where(x=> x.value == mor_field_set["FDC_DET_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_match_prob_bc = hr_cdc_match_prob_bc_values.Where(x=> x.value == mor_field_set["BC_PROB_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_match_prob_fdc = hr_cdc_match_prob_fdc_values.Where(x=> x.value == mor_field_set["FDC_PROB_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_icd = hr_cdc_icd_values.Where(x=> x.value == mor_field_set["ICD10_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_checkbox = hr_cdc_checkbox_values.Where(x=> x.value == mor_field_set["PREGCB_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_literalcod = hr_cdc_literalcod_values.Where(x=> x.value == mor_field_set["LITERALCOD_MATCH"]).Select(x=> x.display).FirstOrDefault();
            var hr_cdc_other =  mor_field_set["HR_CDC_OTHER"];

            var hr_cdc_other_display  = hr_cdc_other;

            if(int.TryParse(hr_cdc_other, out var hr_cdc_other_int))
            {
                hr_cdc_other = hr_cdc_other_int.ToString();
                foreach(var item in hr_cdc_other_values)
                {
                    if(item.value.Trim() == hr_cdc_other.Trim())
                    {
                        hr_cdc_other_display = item?.display;
                    }
                }
            }
            else
            {
                hr_cdc_other = "9999";
                hr_cdc_other_display = "(blank)";
            }
            
            gs.set_value(IJE_to_MMRIA_Path["HR_CDC_OTHER"], hr_cdc_other, new_case);

            

            var string_builder = new System.Text.StringBuilder();
            
            
            string_builder.AppendLine($"Vitals Import Date:  {DateTime.Now.ToString("MM/dd/yyyy")}\n");

            string_builder.AppendLine($"1) CDC Deterministic Linkage with Infant Birth Certificate: {hr_cdc_match_det_bc}");
            string_builder.AppendLine($"2) CDC Deterministic Linkage with Fetal Death Certificate: {hr_cdc_match_det_fdc}");
            string_builder.AppendLine($"3) CDC Probabilistic Linkage with Infant Birth Certificate: {hr_cdc_match_prob_bc}");
            string_builder.AppendLine($"4) CDC Probabilistic Linkage with Fetal Death Certificate: {hr_cdc_match_prob_fdc}");
            string_builder.AppendLine($"5) CDC Identified ICD-10 Code Indicating Pregnancy on Death Certificate: {hr_cdc_icd}");
            string_builder.AppendLine($"6) CDC Identified Pregnancy Checkbox Indicating Pregnancy on Death Certificate: {hr_cdc_checkbox}");
            string_builder.AppendLine($"7) CDC Identified Literal Cause of Death that Included Pregnancy Related Term on Death Certificate: {hr_cdc_literalcod}");
            string_builder.AppendLine($"8) CDC Other Identification Method: {hr_cdc_other_display}");
            
            gs.set_value("home_record/automated_vitals_group/vital_report", string_builder.ToString(), new_case);
            //  Vital Report End



            var DSTATE_result = gs.set_value(IJE_to_MMRIA_Path["DState"], mor_field_set["DState"], new_case);
            var DOD_YR_result = gs.set_value(IJE_to_MMRIA_Path["DOD_YR"], mor_field_set["DOD_YR"], new_case);
            var DOD_MO_result = gs.set_value(IJE_to_MMRIA_Path["DOD_MO"], TryPaseToIntOr_DefaultBlank(mor_field_set["DOD_MO"]), new_case);
            var DOD_DY_result = gs.set_value(IJE_to_MMRIA_Path["DOD_DY"], TryPaseToIntOr_DefaultBlank(mor_field_set["DOD_DY"]), new_case);
            var DOB_YR_result = gs.set_value(IJE_to_MMRIA_Path["DOB_YR"], mor_field_set["DOB_YR"], new_case);
            var DOB_MO_result = gs.set_value(IJE_to_MMRIA_Path["DOB_MO"], TryPaseToIntOr_DefaultBlank(mor_field_set["DOB_MO"]), new_case);
            var DOB_DY_result = gs.set_value(IJE_to_MMRIA_Path["DOB_DY"], TryPaseToIntOr_DefaultBlank(mor_field_set["DOB_DY"]), new_case);
            var LNAME_result = gs.set_value(IJE_to_MMRIA_Path["LNAME"], mor_field_set["LNAME"], new_case);           
            var GNAME_result = gs.set_value(IJE_to_MMRIA_Path["GNAME"], mor_field_set["GNAME"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["FILENO"], mor_field_set["FILENO"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["AUXNO"], mor_field_set["AUXNO"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["AGE"], mor_field_set["AGE"]?.TrimStart('0') ?? "", new_case);
            gs.set_value("death_certificate/demographics/age_on_death_certificate", mor_field_set["AGE"]?.TrimStart('0') ?? "", new_case);
            
            gs.set_value(IJE_to_MMRIA_Path["DMAIDEN"], mor_field_set["DMAIDEN"]?.Trim() ?? "", new_case);
            
            gs.set_value(IJE_to_MMRIA_Path["BPLACE_CNT"], mor_field_set["BPLACE_CNT"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["BPLACE_ST"], mor_field_set["BPLACE_ST"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["STATEC"], mor_field_set["STATEC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COUNTRYC"], mor_field_set["COUNTRYC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["MARITAL"], mor_field_set["MARITAL"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["DPLACE"], DPLACE_Rule(mor_field_set["DPLACE"]), new_case);
            gs.set_value(IJE_to_MMRIA_Path["DPLACE_Outside_of_hospital"], DPLACE_Outside_of_hospital_Rule(mor_field_set["DPLACE"]), new_case);

            gs.set_value(IJE_to_MMRIA_Path["TOD"], mor_field_set["TOD"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["DEDUC"], mor_field_set["DEDUC"], new_case);



            gs.set_value(IJE_to_MMRIA_Path["DETHNIC_is_of_hispanic_origin"], DETHNIC_Rule(mor_field_set["DETHNIC1"], mor_field_set["DETHNIC2"], mor_field_set["DETHNIC3"], mor_field_set["DETHNIC4"]), new_case);
            //gs.set_value(IJE_to_MMRIA_Path["DETHNIC1"], mor_field_set["DETHNIC1"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["DETHNIC1"], mor_field_set["DETHNIC1"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["DETHNIC2"], mor_field_set["DETHNIC2"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["DETHNIC3"], mor_field_set["DETHNIC3"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["DETHNIC4"], mor_field_set["DETHNIC4"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["DETHNIC5"], mor_field_set["DETHNIC5"], new_case);

            gs.set_multi_value(IJE_to_MMRIA_Path["RACE"],
                RACE_Rule(mor_field_set["RACE1"], mor_field_set["RACE2"], mor_field_set["RACE3"],
                            mor_field_set["RACE4"], mor_field_set["RACE5"],
                            mor_field_set["RACE6"], mor_field_set["RACE7"], mor_field_set["RACE8"],
                            mor_field_set["RACE9"], mor_field_set["RACE10"], mor_field_set["RACE11"],
                            mor_field_set["RACE12"], mor_field_set["RACE13"], mor_field_set["RACE14"], mor_field_set["RACE15"]), new_case);

            omb_race_recode_dc(gs, new_case, RACE_Rule(mor_field_set["RACE1"], mor_field_set["RACE2"], mor_field_set["RACE3"],
                            mor_field_set["RACE4"], mor_field_set["RACE5"],
                            mor_field_set["RACE6"], mor_field_set["RACE7"], mor_field_set["RACE8"],
                            mor_field_set["RACE9"], mor_field_set["RACE10"], mor_field_set["RACE11"],
                            mor_field_set["RACE12"], mor_field_set["RACE13"], mor_field_set["RACE14"], mor_field_set["RACE15"]));

            //gs.set_value(IJE_to_MMRIA_Path["RACE1"], mor_field_set["RACE1"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE2"], mor_field_set["RACE2"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE3"], mor_field_set["RACE3"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE4"], mor_field_set["RACE4"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE5"], mor_field_set["RACE5"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE6"], mor_field_set["RACE6"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE7"], mor_field_set["RACE7"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE8"], mor_field_set["RACE8"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE9"], mor_field_set["RACE9"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE10"], mor_field_set["RACE10"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE11"], mor_field_set["RACE11"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE12"], mor_field_set["RACE12"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE13"], mor_field_set["RACE13"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE14"], mor_field_set["RACE14"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE15"], mor_field_set["RACE15"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["RACE_Principal_Tribe"], RACE_Principal_Tribe_Rule(mor_field_set["RACE16"], mor_field_set["RACE17"]), new_case);

            //gs.set_value(IJE_to_MMRIA_Path["RACE16"], mor_field_set["RACE16"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE17"], mor_field_set["RACE17"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["RACE_other_asian"], RACE_other_asian_Rule(mor_field_set["RACE18"], mor_field_set["RACE19"]), new_case);

            //gs.set_value(IJE_to_MMRIA_Path["RACE18"], mor_field_set["RACE18"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE19"], mor_field_set["RACE19"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["RACE_other_pacific_islander"], RACE_other_pacific_islander_Rule(mor_field_set["RACE20"], mor_field_set["RACE21"]), new_case);

            //gs.set_value(IJE_to_MMRIA_Path["RACE20"], mor_field_set["RACE20"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE21"], mor_field_set["RACE21"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["RACE_other_race"], RACE_other_race_Rule(mor_field_set["RACE22"], mor_field_set["RACE23"]), new_case);

            //gs.set_value(IJE_to_MMRIA_Path["RACE22"], mor_field_set["RACE22"], new_case);
            //gs.set_value(IJE_to_MMRIA_Path["RACE23"], mor_field_set["RACE23"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["OCCUP"], mor_field_set["OCCUP"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["INDUST"], mor_field_set["INDUST"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["MANNER"], mor_field_set["MANNER"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["MAN_UC"], mor_field_set["MAN_UC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["ACME_UC"], mor_field_set["ACME_UC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["EAC"], mor_field_set["EAC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["RAC"], mor_field_set["RAC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["AUTOP"], mor_field_set["AUTOP"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["AUTOPF"], mor_field_set["AUTOPF"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["TOBAC"], mor_field_set["TOBAC"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["PREG"], mor_field_set["PREG"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["DOI_MO"], TryPaseToIntOr_DefaultBlank(mor_field_set["DOI_MO"]), new_case);
            gs.set_value(IJE_to_MMRIA_Path["DOI_DY"], TryPaseToIntOr_DefaultBlank(mor_field_set["DOI_DY"]), new_case);
            gs.set_value(IJE_to_MMRIA_Path["DOI_YR"], mor_field_set["DOI_YR"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["TOI_HR"], mor_field_set["TOI_HR"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["WORKINJ"], mor_field_set["WORKINJ"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["ARMEDF"], mor_field_set["ARMEDF"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["DINSTI"], mor_field_set["DINSTI"], new_case);


            gs.set_value(IJE_to_MMRIA_Path["ADDRESS_OF_DEATH_street"], ADDRESS_OF_DEATH_street_Rule(mor_field_set["STNUM_D"]
                                                                                                , mor_field_set["PREDIR_D"]
                                                                                                , mor_field_set["STNAME_D"]
                                                                                                , mor_field_set["STDESIG_D"]
                                                                                                , mor_field_set["POSTDIR_D"]), new_case);


            gs.set_value(IJE_to_MMRIA_Path["CITYTEXT_D"], mor_field_set["CITYTEXT_D"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["STATETEXT_D"], STATETEXT_D_Rule(mor_field_set["STATETEXT_D"]), new_case);
            gs.set_value(IJE_to_MMRIA_Path["ZIP9_D"], mor_field_set["ZIP9_D"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COUNTYTEXT_D"], mor_field_set["COUNTYTEXT_D"], new_case);

            Set_address_of_death_Gecocode
            (
                gs, 
                get_geocode_info
                (
                ADDRESS_OF_DEATH_street_Rule
                (
                    mor_field_set["STNUM_D"],
                    mor_field_set["PREDIR_D"],
                    mor_field_set["STNAME_D"],
                    mor_field_set["STDESIG_D"],
                    mor_field_set["POSTDIR_D"]
                ), 
                mor_field_set["CITYTEXT_D"],
                STATETEXT_D_Rule(mor_field_set["STATETEXT_D"]),
                mor_field_set["ZIP9_D"]), 
                new_case
            );

            gs.set_value
            (
                IJE_to_MMRIA_Path["PLACE_OF_LAST_RESIDENCE_street"], 
                PLACE_OF_LAST_RESIDENCE_street_Rule
                (
                    mor_field_set["STNUM_R"],
                    mor_field_set["PREDIR_R"],
                    mor_field_set["STNAME_R"],
                    mor_field_set["STDESIG_R"],
                    mor_field_set["POSTDIR_R"]
                ), 
                new_case
            );

            gs.set_value(IJE_to_MMRIA_Path["UNITNUM_R"], mor_field_set["UNITNUM_R"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["CITYTEXT_R"], mor_field_set["CITYTEXT_R"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["ZIP9_R"], mor_field_set["ZIP9_R"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COUNTYTEXT_R"], mor_field_set["COUNTYTEXT_R"], new_case);

            Set_place_of_last_residence_Gecocode
            (
                gs,
                get_geocode_info
                (
                    PLACE_OF_LAST_RESIDENCE_street_Rule
                    (
                        mor_field_set["STNUM_R"], 
                        mor_field_set["PREDIR_R"],
                        mor_field_set["STNAME_R"],
                        mor_field_set["STDESIG_R"],
                        mor_field_set["POSTDIR_R"]
                    ), 
                    mor_field_set["CITYTEXT_R"],
                    mor_field_set["STATEC"],
                    mor_field_set["ZIP9_R"]
                ), 
                new_case
            );

            var new_case_dictionary = new_case as IDictionary<string, object>;

            {
                string get_value(System.Dynamic.ExpandoObject p_doc, string p_path)
                {
                    var result = String.Empty;


                    migrate.C_Get_Set_Value.get_value_result value_result = gs.get_value(p_doc, p_path);
                    if
                    (
                        ! value_result.is_error &&
                        value_result.result != null
                    )
                    {
                        result = value_result.result.ToString();
                    }

                    return result;
                }


                //bool set_grid_value(string p_path, List<(int, object)> p_value_list)
                bool set_grid_value(string p_path, object p_value_list)

                {
                    var result = true;

                    result = result &&  gs.set_grid_value(new_case, p_path, new List<(int, object)>() { ( 0, p_value_list) });

                    return result;
                }

                var state_county_fips = get_value(new_case, "death_certificate/place_of_last_residence/state_county_fips");
                var  census_tract_fips = get_value(new_case, "death_certificate/place_of_last_residence/census_tract_fips");
                var  year = get_value(new_case, "home_record/date_of_death/year");




                var cvs_form_metadata = new mmria.common.metadata.node();

                foreach(var child in metadata.children)
                {
                    if(child.name.Equals("cvs", StringComparison.OrdinalIgnoreCase))
                    {
                        cvs_form_metadata = child;
                    }
                }

                var new_cvs_form = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                mmria.services.vitalsimport.default_case.create(cvs_form_metadata, new_cvs_form, true);
                var list = new_cvs_form["cvs"] as  IDictionary<string,object>;

                if(new_case_dictionary != null)
                {
                    new_case_dictionary["cvs"] = list;               
                }


                var Valid_CVS_Years = CVS_Get_Valid_Years(db_config_set);

                var int_year_of_death = -1;
                int test_int_year = -1;

                const int year_difference_limit = 9;

                if(int.TryParse(year, out test_int_year))
                {
                    int_year_of_death = test_int_year;
                }

                var calculated_year_of_death = int_year_of_death;

                if
                (
                    Valid_CVS_Years != null &&
                    Valid_CVS_Years.Count > 0 &&
                    ! Valid_CVS_Years.Contains(int_year_of_death)
                )
                {

                    var lower_diff = System.Math.Abs(Valid_CVS_Years[0] - int_year_of_death);
                    var upper_diff = System.Math.Abs(Valid_CVS_Years[Valid_CVS_Years.Count -1] - int_year_of_death);

                    if(lower_diff < upper_diff)
                    {
                        if(lower_diff <= year_difference_limit)
                        {
                            calculated_year_of_death = Valid_CVS_Years[0];
                        }
                    }
                    else
                    {
                        if(upper_diff <= year_difference_limit)
                        {
                            calculated_year_of_death = Valid_CVS_Years[Valid_CVS_Years.Count -1];
                        }
                    }
                }


                if
                (
                    !string.IsNullOrEmpty(state_county_fips) &&
                    !string.IsNullOrEmpty(census_tract_fips) &&
                    !string.IsNullOrEmpty(year)
                )
                {
                    var t_geoid = $"{state_county_fips}{census_tract_fips.Replace(".","").PadRight(6, '0')}";


                    var (cvs_response_status, tract_county_result) = GetCVSData
                    (
                        state_county_fips,
                        t_geoid,
                        calculated_year_of_death.ToString(),
                       db_config_set
                    );


                    set_grid_value("cvs/cvs_grid/cvs_api_request_url", db_config_set.name_value["cvs_api_url"]);
                    set_grid_value("cvs/cvs_grid/cvs_api_request_date_time", DateTime.Now.ToString("o"));
                    set_grid_value("cvs/cvs_grid/cvs_api_request_c_geoid", state_county_fips);
                    set_grid_value("cvs/cvs_grid/cvs_api_request_t_geoid", t_geoid);
                    set_grid_value("cvs/cvs_grid/cvs_api_request_year", calculated_year_of_death.ToString());


                    if(cvs_response_status == "success")
                    {

                        if(calculated_year_of_death != int_year_of_death)
                        {
                            cvs_response_status += " year_of_death adjusted";
                        }

                        if
                        (
                          is_result_quality_in_need_of_checking(tract_county_result)
                        )
                        {
                            cvs_response_status += " check quality";
                        }

                        set_grid_value("cvs/cvs_grid/cvs_mdrate_county", tract_county_result.county.MDrate);
                        set_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_county", tract_county_result.county.pctNOIns_Fem);
                        set_grid_value("cvs/cvs_grid/cvs_pctnoins_fem_tract", tract_county_result.tract.pctNOIns_Fem);
                        set_grid_value("cvs/cvs_grid/cvs_pctnovehicle_county", tract_county_result.county.pctNoVehicle);
                        set_grid_value("cvs/cvs_grid/cvs_pctnovehicle_tract", tract_county_result.tract.pctNoVehicle);
                        set_grid_value("cvs/cvs_grid/cvs_pctmove_county", tract_county_result.county.pctMOVE);
                        set_grid_value("cvs/cvs_grid/cvs_pctmove_tract", tract_county_result.tract.pctMOVE);
                        set_grid_value("cvs/cvs_grid/cvs_pctsphh_county", tract_county_result.county.pctSPHH);
                        set_grid_value("cvs/cvs_grid/cvs_pctsphh_tract", tract_county_result.tract.pctSPHH);
                        set_grid_value("cvs/cvs_grid/cvs_pctovercrowdhh_county", tract_county_result.county.pctOVERCROWDHH);
                        set_grid_value("cvs/cvs_grid/cvs_pctovercrowdhh_tract", tract_county_result.tract.pctOVERCROWDHH);
                        set_grid_value("cvs/cvs_grid/cvs_pctowner_occ_county", tract_county_result.county.pctOWNER_OCC);
                        set_grid_value("cvs/cvs_grid/cvs_pctowner_occ_tract", tract_county_result.tract.pctOWNER_OCC);
                        set_grid_value("cvs/cvs_grid/cvs_pct_less_well_county", tract_county_result.county.pct_less_well);
                        set_grid_value("cvs/cvs_grid/cvs_pct_less_well_tract", tract_county_result.tract.pct_less_well);
                        set_grid_value("cvs/cvs_grid/cvs_ndi_raw_county", tract_county_result.county.NDI_raw);
                        set_grid_value("cvs/cvs_grid/cvs_ndi_raw_tract", tract_county_result.tract.NDI_raw);
                        set_grid_value("cvs/cvs_grid/cvs_pctpov_county", tract_county_result.county.pctPOV);
                        set_grid_value("cvs/cvs_grid/cvs_pctpov_tract", tract_county_result.tract.pctPOV);
                        set_grid_value("cvs/cvs_grid/cvs_ice_income_all_county", tract_county_result.county.ICE_INCOME_all);
                        set_grid_value("cvs/cvs_grid/cvs_ice_income_all_tract", tract_county_result.tract.ICE_INCOME_all);
                        set_grid_value("cvs/cvs_grid/cvs_medhhinc_county", tract_county_result.county.MEDHHINC);
                        set_grid_value("cvs/cvs_grid/cvs_medhhinc_tract", tract_county_result.tract.MEDHHINC);
                        set_grid_value("cvs/cvs_grid/cvs_pctobese_county", tract_county_result.county.pctOBESE);
                        set_grid_value("cvs/cvs_grid/cvs_fi_county", tract_county_result.county.FI);
                        set_grid_value("cvs/cvs_grid/cvs_cnmrate_county", tract_county_result.county.CNMrate);
                        set_grid_value("cvs/cvs_grid/cvs_obgynrate_county", tract_county_result.county.OBGYNrate);
                        set_grid_value("cvs/cvs_grid/cvs_rtteenbirth_county", tract_county_result.county.rtTEENBIRTH);
                        set_grid_value("cvs/cvs_grid/cvs_rtstd_county", tract_county_result.county.rtSTD);
                        set_grid_value("cvs/cvs_grid/cvs_rtmhpract_county", tract_county_result.county.MHCENTERrate);
                        set_grid_value("cvs/cvs_grid/cvs_rtdrugodmortality_county", tract_county_result.county.rtDRUGODMORTALITY);
                        set_grid_value("cvs/cvs_grid/cvs_rtopioidprescript_county", tract_county_result.county.rtOPIOIDPRESCRIPT);
                        set_grid_value("cvs/cvs_grid/cvs_soccap_county", tract_county_result.county.SocCap);
                        set_grid_value("cvs/cvs_grid/cvs_rtsocassoc_county", tract_county_result.county.rtSocASSOC);
                        set_grid_value("cvs/cvs_grid/cvs_pcthouse_distress_county", tract_county_result.county.pctHOUSE_DISTRESS);
                        set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR_ICPSR);
                        set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.isolation);

                        set_grid_value("cvs/cvs_grid/cvs_cnmrate_county", tract_county_result.county.MIDWIVESrate);
                        set_grid_value("cvs/cvs_grid/cvs_isolation_county", tract_county_result.county.segregation);
                        set_grid_value("cvs/cvs_grid/cvs_mdrate_county", tract_county_result.county.PCPrate);
                        set_grid_value("cvs/cvs_grid/cvs_rtviolentcr_icpsr_county", tract_county_result.county.rtVIOLENTCR);

                        set_grid_value("cvs/cvs_grid/cvs_pctrural", tract_county_result.county.pctRural);
                        set_grid_value("cvs/cvs_grid/cvs_racialized_pov",  tract_county_result.county.Racialized_pov);
                        set_grid_value("cvs/cvs_grid/cvs_mhproviderrate",  tract_county_result.county.MHPROVIDERrate);
                        
                    }

                    set_grid_value("cvs/cvs_grid/cvs_api_request_result_message", cvs_response_status);
                }
                else
                {
                    
                }
                    

            }

            gs.set_value(IJE_to_MMRIA_Path["DMIDDLE"], mor_field_set["DMIDDLE"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["POILITRL"], mor_field_set["POILITRL"], new_case);

            gs.set_value(IJE_to_MMRIA_Path["TRANSPRT"], TRANSPRT_Rule(mor_field_set["TRANSPRT"]), new_case);
            gs.set_value(IJE_to_MMRIA_Path["TRANSPRT_other_specify"], TRANSPRT_other_specify_Rule(mor_field_set["TRANSPRT"]), new_case);


            gs.set_value(IJE_to_MMRIA_Path["COUNTYTEXT_I"], mor_field_set["COUNTYTEXT_I"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["CITYTEXT_I"], mor_field_set["CITYTEXT_I"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COD1A"], mor_field_set["COD1A"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["INTERVAL1A"], mor_field_set["INTERVAL1A"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COD1B"], mor_field_set["COD1B"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["INTERVAL1B"], mor_field_set["INTERVAL1B"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COD1C"], mor_field_set["COD1C"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["INTERVAL1C"], mor_field_set["INTERVAL1C"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["COD1D"], mor_field_set["COD1D"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["INTERVAL1D"], mor_field_set["INTERVAL1D"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["OTHERCONDITION"], mor_field_set["OTHERCONDITION"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["DBPLACECITY"], mor_field_set["DBPLACECITY"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["STINJURY"], STINJURY_Rule(mor_field_set["STINJURY"]), new_case);
            //gs.set_value(IJE_to_MMRIA_Path["VRO_STATUS"], mor_field_set["VRO_STATUS"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["BC_DET_MATCH"], mor_field_set["BC_DET_MATCH"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["FDC_DET_MATCH"], mor_field_set["FDC_DET_MATCH"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["BC_PROB_MATCH"], mor_field_set["BC_PROB_MATCH"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["FDC_PROB_MATCH"], mor_field_set["FDC_PROB_MATCH"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["ICD10_MATCH"], mor_field_set["ICD10_MATCH"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["PREGCB_MATCH"], mor_field_set["PREGCB_MATCH"], new_case);
            gs.set_value(IJE_to_MMRIA_Path["LITERALCOD_MATCH"], mor_field_set["LITERALCOD_MATCH"], new_case);


            // death_certificate/vitals_import_group/vital_summary_text - begin
            string_builder.Clear();

            string_builder.AppendLine($"Cause of Death:");
            string_builder.AppendLine($"01) Part I Line A: {mor_field_set["COD1A"]}");
            string_builder.AppendLine($"02) Part I Interval, Line A: {mor_field_set["INTERVAL1A"]}");
            string_builder.AppendLine($"03) Part I Line B: {mor_field_set["COD1B"]}");
            string_builder.AppendLine($"04) Part I Interval, Line B: {mor_field_set["INTERVAL1B"]}");
            string_builder.AppendLine($"05) Part I Line C: {mor_field_set["COD1C"]}");
            string_builder.AppendLine($"06) Part I Interval, Line C: {mor_field_set["INTERVAL1C"]}");
            string_builder.AppendLine($"07) Part I Line D: {mor_field_set["COD1D"]}");
            string_builder.AppendLine($"08) Part I Interval, Line D: {mor_field_set["INTERVAL1D"]}");
            string_builder.AppendLine($"09) Part II: {mor_field_set["OTHERCONDITION"]}");
            string_builder.AppendLine($"");
            string_builder.AppendLine($"Codes:");
            string_builder.AppendLine($"10) Manual Underlying Cause: {mor_field_set["MAN_UC"]}");
            string_builder.AppendLine($"11) ACME Underlying Cause: {mor_field_set["ACME_UC"]}");
            string_builder.AppendLine($"12) Entity-axis Codes: {mor_field_set["EAC"]}");
            string_builder.AppendLine($"13) Record-axis codes: {mor_field_set["RAC"]}");

            gs.set_value("death_certificate/vitals_import_group/vital_summary_text", string_builder.ToString(), new_case);


            // death_certificate/vitals_import_group/vital_summary_text - end

            #endregion

            #region ParentForm Section

            var natal_fetal_metadata = new mmria.common.metadata.node();

            foreach(var child in metadata.children)
            {
                if(child.name.Equals("birth_certificate_infant_fetal_section", StringComparison.OrdinalIgnoreCase))
                {
                    natal_fetal_metadata = child;
                }
            }

        
            if(new_case_dictionary != null)
            {
                var natal_fetal_list = new List<object>();

                if (nat_field_set != null && nat_field_set.Count > 0)
                {
                    var field_set = nat_field_set.First();



                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["STATEC"], field_set["STATEC"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["IDOB_YR"], field_set["IDOB_YR"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["IDOB_MO"], TryPaseToIntOr_DefaultBlank(field_set["IDOB_MO"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["IDOB_DY"], TryPaseToIntOr_DefaultBlank(field_set["IDOB_DY"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MDOB_YR"], field_set["MDOB_YR"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MDOB_MO"], TryPaseToIntOr_DefaultBlank(field_set["MDOB_MO"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MDOB_DY"], TryPaseToIntOr_DefaultBlank(field_set["MDOB_DY"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FDOB_YR"], field_set["FDOB_YR"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FDOB_MO"], TryPaseToIntOr_DefaultBlank(field_set["FDOB_MO"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ACKN"], field_set["ACKN"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MEDUC"], field_set["MEDUC"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FEDUC"], FEDUC_Rule(field_set["FEDUC"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ATTEND"], field_set["ATTEND"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["TRAN"], field_set["TRAN"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["NPREV"], TryPaseToIntOr_DefaultBlank(field_set["NPREV"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["HFT"], field_set["HFT"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["HIN"], TryPaseToIntOr_DefaultBlank(field_set["HIN"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["PWGT"], field_set["PWGT"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DWGT"], field_set["DWGT"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["WIC"], field_set["WIC"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["PLBL"], TryPaseToIntOr_DefaultBlank(field_set["PLBL"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["PLBD"], TryPaseToIntOr_DefaultBlank(field_set["PLBD"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["POPO"], TryPaseToIntOr_DefaultBlank(field_set["POPO"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MLLB"], TryPaseToIntOr_DefaultBlank(field_set["MLLB"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["YLLB"], field_set["YLLB"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOPO"], TryPaseToIntOr_DefaultBlank(field_set["MOPO"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["YOPO"], TryPaseToIntOr_DefaultBlank(field_set["YOPO"], "9999"), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["PAY"], field_set["PAY"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DLMP_YR"], field_set["DLMP_YR"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DLMP_MO"], TryPaseToIntOr_DefaultBlank(field_set["DLMP_MO"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DLMP_DY"], TryPaseToIntOr_DefaultBlank(field_set["DLMP_DY"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["NPCES"], TryPaseToInt_00_To30(field_set["NPCES"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["OWGEST"], TryPaseToIntOr_DefaultBlank(field_set["OWGEST"], ""), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BIRTH_CO"], field_set["BIRTH_CO"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BRTHCITY"], field_set["BRTHCITY"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["HOSP"], field_set["HOSP"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOMFNAME"], field_set["MOMFNAME"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOMMIDDL"], field_set["MOMMIDDL"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOMLNAME"], field_set["MOMLNAME"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOMMAIDN"], field_set["MOMMAIDN"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["UNUM"], field_set["UNUM"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ZIPCODE"], field_set["ZIPCODE"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["COUNTYTXT"], field_set["COUNTYTXT"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CITYTEXT"], field_set["CITYTEXT"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOM_OC_T"], field_set["MOM_OC_T"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DAD_OC_T"], field_set["DAD_OC_T"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOM_IN_T"], field_set["MOM_IN_T"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DAD_IN_T"], field_set["DAD_IN_T"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["HOSPFROM"], field_set["HOSPFROM"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ATTEND_OTH_TXT"], field_set["ATTEND_OTH_TXT"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["ATTEND_NPI"], field_set["ATTEND_NPI"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MOM_MED_REC_NUM"], field_set["MOM_MED_REC_NUM"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FNPI"], field_set["FNPI"], new_case);

                    gs.set_value
                    (
                        Parent_NAT_IJE_to_MMRIA_Path["LOCATION_OF_RESIDENCE_street"], 
                        LOCATION_OF_RESIDENCE_street_Rule
                        (
                            field_set["STNUM"],
                            field_set["PREDIR"],
                            field_set["STNAME"],
                            field_set["STDESIG"],
                            field_set["POSTDIR"]
                        ), 
                        new_case
                    );

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["METHNIC5"], field_set["METHNIC5"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FETHNIC5"], field_set["FETHNIC5"], new_case);


                    Set_location_of_residence_Gecocode
                    (
                        gs, 
                        get_geocode_info
                        (
                            LOCATION_OF_RESIDENCE_street_Rule
                            (
                                field_set["STNUM"],
                                field_set["PREDIR"], 
                                field_set["STNAME"],
                                field_set["STDESIG"],
                                field_set["POSTDIR"]
                            ),
                            field_set["CITYTEXT"],
                            field_set["STATEC"],
                            field_set["ZIPCODE"]
                        ), 
                        new_case
                    );


                    birth_2_death(gs, new_case, field_set["IDOB_YR"], field_set["IDOB_MO"], field_set["IDOB_DY"]
                        , mor_field_set["DOD_YR"], mor_field_set["DOD_MO"], mor_field_set["DOD_DY"]);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BSTATE"], field_set["BSTATE"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BPLACE"], BPLACE_place_NAT_Rule(field_set["BPLACE"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BPLACE_was_home_delivery_planned"], BPLACE_plann_NAT_Rule(field_set["BPLACE"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BPLACEC_ST_TER"], field_set["BPLACEC_ST_TER"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["BPLACEC_CNT"], field_set["BPLACEC_CNT"], new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["METHNIC"], NAT_METHNIC_Rule(field_set["METHNIC1"], field_set["METHNIC2"], field_set["METHNIC3"], field_set["METHNIC4"]), new_case);


                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["MRACE"], MRACE_NAT_Rule(field_set["MRACE1"],
                        field_set["MRACE2"],
                        field_set["MRACE3"],
                        field_set["MRACE4"],
                        field_set["MRACE5"],
                        field_set["MRACE6"],
                        field_set["MRACE7"],
                        field_set["MRACE8"],
                        field_set["MRACE9"],
                        field_set["MRACE10"],
                        field_set["MRACE11"],
                        field_set["MRACE12"],
                        field_set["MRACE13"],
                        field_set["MRACE14"],
                        field_set["MRACE15"])
                        , new_case);

                    omb_mrace_recode(gs, new_case, MRACE_NAT_Rule(field_set["MRACE1"],
                        field_set["MRACE2"],
                        field_set["MRACE3"],
                        field_set["MRACE4"],
                        field_set["MRACE5"],
                        field_set["MRACE6"],
                        field_set["MRACE7"],
                        field_set["MRACE8"],
                        field_set["MRACE9"],
                        field_set["MRACE10"],
                        field_set["MRACE11"],
                        field_set["MRACE12"],
                        field_set["MRACE13"],
                        field_set["MRACE14"],
                        field_set["MRACE15"]));

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MRACE16_17"], MRACE16_17_NAT_Rule(field_set["MRACE16"], field_set["MRACE17"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MRACE18_19"], MRACE18_19_NAT_Rule(field_set["MRACE18"], field_set["MRACE19"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MRACE20_21"], MRACE20_21_NAT_Rule(field_set["MRACE20"], field_set["MRACE21"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MRACE22_23"], MRACE22_23_NAT_Rule(field_set["MRACE22"], field_set["MRACE23"]), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FETHNIC"],
                        FETHNIC_NAT_Rule(field_set["FETHNIC1"]
                        , field_set["FETHNIC2"]
                        , field_set["FETHNIC3"]
                        , field_set["FETHNIC4"])
                        , new_case);

                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE"], FRACE_NAT_Rule(field_set["FRACE1"],
                        field_set["FRACE2"],
                        field_set["FRACE3"],
                        field_set["FRACE4"],
                        field_set["FRACE5"],
                        field_set["FRACE6"],
                        field_set["FRACE7"],
                        field_set["FRACE8"],
                        field_set["FRACE9"],
                        field_set["FRACE10"],
                        field_set["FRACE11"],
                        field_set["FRACE12"],
                        field_set["FRACE13"],
                        field_set["FRACE14"],
                        field_set["FRACE15"])
                        , new_case);

                    omb_frace_recode(gs, new_case, FRACE_NAT_Rule(field_set["FRACE1"],
                        field_set["FRACE2"],
                        field_set["FRACE3"],
                        field_set["FRACE4"],
                        field_set["FRACE5"],
                        field_set["FRACE6"],
                        field_set["FRACE7"],
                        field_set["FRACE8"],
                        field_set["FRACE9"],
                        field_set["FRACE10"],
                        field_set["FRACE11"],
                        field_set["FRACE12"],
                        field_set["FRACE13"],
                        field_set["FRACE14"],
                        field_set["FRACE15"]));

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE16_17"], FRACE16_17_NAT_Rule(field_set["FRACE16"], field_set["FRACE16"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE18_19"], FRACE18_19_NAT_Rule(field_set["FRACE18"], field_set["FRACE19"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE20_21"], FRACE20_21_NAT_Rule(field_set["FRACE20"], field_set["FRACE21"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FRACE22_23"], FRACE22_23_NAT_Rule(field_set["FRACE22"], field_set["FRACE23"]), new_case);


                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DOFP_MO"], TryPaseToIntOr_DefaultBlank(field_set["DOFP_MO"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DOFP_DY"], TryPaseToIntOr_DefaultBlank(field_set["DOFP_DY"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DOFP_YR"], TryPaseToIntOr_DefaultBlank(field_set["DOFP_YR"], "9999"), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DOLP_MO"], TryPaseToIntOr_DefaultBlank(field_set["DOLP_MO"], "9999"), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DOLP_DY"], TryPaseToIntOr_DefaultBlank(field_set["DOLP_DY"], "9999"), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["DOLP_YR"], TryPaseToIntOr_DefaultBlank(field_set["DOLP_YR"], "9999"), new_case);


                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGPN"], CIGPN_NAT_Rule(field_set["CIGPN"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGPN_prior_3_months_type"], CIGPN_Type_NAT_Rule(field_set["CIGPN"]), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGFN"], CIGFN_NAT_Rule(field_set["CIGFN"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGFN_trimester_1st_type"], CIGFN_Type_NAT_Rule(field_set["CIGFN"]), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGSN"], CIGSN_NAT_Rule(field_set["CIGSN"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGSN_trimester_2nd_type"], CIGSN_Type_NAT_Rule(field_set["CIGSN"]), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGLN"], CIGLN_NAT_Rule(field_set["CIGLN"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIGLN_trimester_3rd_type"], CIGLN_Type_NAT_Rule(field_set["CIGLN"]), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["CIG_none_or_not_specified"], 
                        CIG_none_or_not_specified_NAT_Rule(
                            field_set["CIGPN"],
                            field_set["CIGFN"],
                            field_set["CIGSN"],
                            field_set["CIGLN"]
                            ), new_case);


                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["risk_factors_in_this_pregnancy"],
                            NAT_risk_factors_in_this_pregnancy_Rule(
                                field_set["PDIAB"],
                                field_set["GDIAB"],
                                field_set["PHYPE"],
                                field_set["GHYPE"],
                                field_set["PPB"],
                                field_set["PPO"],
                                field_set["INFT"],
                                field_set["PCES"],
                                field_set["EHYPE"],
                                field_set["INFT_DRG"],
                                field_set["INFT_ART"]

                            ), new_case);

                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["infections_present_or_treated_during_pregnancy"],
                            NAT_infections_present_or_treated_during_pregnancy_Rule(
                                field_set["GON"],
                                field_set["SYPH"],
                                field_set["HSV"],
                                field_set["CHAM"],
                                field_set["HEPB"],
                                field_set["HEPC"]
                            ), new_case);


                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["obstetric_procedures"],
                                NAT_obstetric_procedures_Rule(
                                    field_set["CERV"],
                                    field_set["TOC"],
                                    field_set["ECVS"],
                                    field_set["ECVF"]
                                ), new_case);

                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["onset_of_labor"],
                                                    NAT_onset_of_labor_Rule(
                                                        field_set["PROM"],
                                                        field_set["PRIC"],
                                                        field_set["PROL"]
                                                    ), new_case);

                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["characteristics_of_labor_and_delivery"],
                                                    NAT_characteristics_of_labor_and_delivery_Rule(
                                                        field_set["INDL"],
                                                        field_set["AUGL"],
                                                        field_set["NVPR"],
                                                        field_set["STER"],
                                                        field_set["ANTB"],
                                                        field_set["CHOR"],
                                                        field_set["MECS"],
                                                        field_set["FINT"],
                                                        field_set["ESAN"]
                                                    ), new_case);


                    gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["maternal_morbidity"],
                                                    NAT_maternal_morbidity_Rule(
                                                        field_set["MTR"],
                                                        field_set["PLAC"],
                                                        field_set["RUT"],
                                                        field_set["UHYS"],
                                                        field_set["AINT"],
                                                        field_set["UOPR"]
                                                    ), new_case);


                    //gs.set_multi_value(Parent_NAT_IJE_to_MMRIA_Path["risk_factors_in_this_pregnancy"],
                    //        NAT_risk_factors_in_this_pregnancyy_Rule(

                    //            field_set["INFT_DRG"],
                    //            field_set["INFT_ART"]
                    //        ), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MAGER"], MAGER_NAT_Rule(field_set["MAGER"],
                                field_set["MDOB_YR"], field_set["MDOB_MO"], field_set["MDOB_DY"],
                                field_set["IDOB_YR"], field_set["IDOB_MO"], field_set["IDOB_DY"]), new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FAGER"], FAGER_NAT_Rule(field_set["FAGER"],
                        field_set["FDOB_YR"], field_set["FDOB_MO"],
                        field_set["IDOB_YR"], field_set["IDOB_MO"], field_set["IDOB_DY"]), new_case);


                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FBPLACD_ST_TER_C"], field_set["FBPLACD_ST_TER_C"], new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["FBPLACE_CNT_C"], field_set["FBPLACE_CNT_C"], new_case);

                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["PLUR"], PLUR_Custom_NAT_Rule(field_set["PLUR"]), new_case);
                    gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["PLUR_specify_if_greater_than_3"], PLUR_sigt_NAT_Rule( field_set["PLUR"]), new_case);

                }
                else if (fet_field_set != null && fet_field_set.Count > 0)
                {
                    var field_set = fet_field_set.First();



                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["STATEC"], mor_field_set["STATEC"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FDOD_YR"], field_set["FDOD_YR"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FDOD_MO"], TryPaseToIntOr_DefaultBlank(field_set["FDOD_MO"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FDOD_DY"], TryPaseToIntOr_DefaultBlank(field_set["FDOD_DY"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FNPI"], field_set["FNPI"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MDOB_YR"], field_set["MDOB_YR"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MDOB_MO"], TryPaseToIntOr_DefaultBlank(field_set["MDOB_MO"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MDOB_DY"], TryPaseToIntOr_DefaultBlank(field_set["MDOB_DY"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FDOB_YR"], field_set["FDOB_YR"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FDOB_MO"], TryPaseToIntOr_DefaultBlank(field_set["FDOB_MO"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MEDUC"], field_set["MEDUC"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["ATTEND"], field_set["ATTEND"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["TRAN"], field_set["TRAN"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["NPREV"], TryPaseToIntOr_DefaultBlank(field_set["NPREV"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HFT"], field_set["HFT"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HIN"], TryPaseToIntOr_DefaultBlank(field_set["HIN"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["PWGT"], field_set["PWGT"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DWGT"], field_set["DWGT"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["WIC"], field_set["WIC"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["PLBL"], TryPaseToIntOr_DefaultBlank(field_set["PLBL"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["PLBD"], TryPaseToIntOr_DefaultBlank(field_set["PLBD"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["POPO"], TryPaseToIntOr_DefaultBlank(field_set["POPO"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MLLB"], TryPaseToIntOr_DefaultBlank(field_set["MLLB"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["YLLB"], TryPaseToIntOr_DefaultBlank(field_set["YLLB"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOPO"], TryPaseToIntOr_DefaultBlank(field_set["MOPO"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["YOPO"], TryPaseToIntOr_DefaultBlank(field_set["YOPO"], "9999"), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DLMP_YR"], field_set["DLMP_YR"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DLMP_MO"], TryPaseToIntOr_DefaultBlank(field_set["DLMP_MO"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DLMP_DY"], TryPaseToIntOr_DefaultBlank(field_set["DLMP_DY"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["NPCES"], TryPaseToInt_00_To30(field_set["NPCES"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["OWGEST"], TryPaseToIntOr_DefaultBlank(field_set["OWGEST"], ""), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HOSP_D"], field_set["HOSP_D"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["ADDRESS_D"], field_set["ADDRESS_D"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["ZIPCODE_D"], field_set["ZIPCODE_D"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CNTY_D"], field_set["CNTY_D"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CITY_D"], field_set["CITY_D"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOMFNAME"], field_set["MOMFNAME"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOMMNAME"], field_set["MOMMNAME"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOMLNAME"], field_set["MOMLNAME"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOMMAIDN"], field_set["MOMMAIDN"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["APTNUMB"], field_set["APTNUMB"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["ZIPCODE"], field_set["ZIPCODE"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["COUNTYTXT"], field_set["COUNTYTXT"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CITYTXT"], field_set["CITYTXT"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOM_OC_T"], field_set["MOM_OC_T"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DAD_OC_T"], field_set["DAD_OC_T"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MOM_IN_T"], field_set["MOM_IN_T"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DAD_IN_T"], field_set["DAD_IN_T"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FEDUC"], field_set["FEDUC"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HOSPFROM"], field_set["HOSPFROM"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["ATTEND_NPI"], field_set["ATTEND_NPI"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["ATTEND_OTH_TXT"], field_set["ATTEND_OTH_TXT"], new_case);


                    gs.set_value
                    (
                        Parent_FET_IJE_to_MMRIA_Path["LOCATION_OF_RESIDENCE_street"], 
                        FET_LOCATION_OF_RESIDENCE_street_Rule
                        (
                            field_set["STNUM"],
                            field_set["PREDIR"],
                            field_set["STNAME"],
                            field_set["STDESIG"],
                            field_set["POSTDIR"]
                        ), 
                        new_case
                    );

                    Set_location_of_residence_Gecocode
                    (
                        gs,
                        get_geocode_info
                        (
                            FET_LOCATION_OF_RESIDENCE_street_Rule
                            (
                                field_set["STNUM"],
                                field_set["PREDIR"],
                                field_set["STNAME"],
                                field_set["STDESIG"],
                                field_set["POSTDIR"]
                            ),
                            field_set["CITYTXT"],
                            field_set["STATEC"],
                            field_set["ZIPCODE"]
                        ), 
                        new_case
                    );

                    Set_facility_of_delivery_location_Gecocode
                    (
                        gs, 
                        get_geocode_info
                        (
                            field_set["ADDRESS_D"],
                            field_set["CITY_D"],
                            "", //field_set["STATEC"],
                            field_set["ZIPCODE_D"]
                        ), 
                        new_case
                    );

                    birth_2_death
                    (
                        gs, 
                        new_case, 
                        field_set["FDOD_YR"], 
                        field_set["FDOD_MO"], 
                        field_set["FDOD_DY"], 
                        mor_field_set["DOD_YR"], 
                        mor_field_set["DOD_MO"], 
                        mor_field_set["DOD_DY"]
                    );

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DSTATE"], field_set["DSTATE"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DPLACE"], DPLACE_Custom_FET_Rule(field_set["DPLACE"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["BPLACEC_ST_TER"], field_set["BPLACEC_ST_TER"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["BPLACEC_CNT"], field_set["BPLACEC_CNT"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["STATEC"], field_set["STATEC"], new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["METHNIC"], FET_METHNIC_Rule(field_set["METHNIC1"], field_set["METHNIC2"], field_set["METHNIC3"], field_set["METHNIC4"]), new_case);


                    gs.set_multi_value(Parent_FET_IJE_to_MMRIA_Path["MRACE"], MRACE_NAT_Rule(field_set["MRACE1"],
                        field_set["MRACE2"],
                        field_set["MRACE3"],
                        field_set["MRACE4"],
                        field_set["MRACE5"],
                        field_set["MRACE6"],
                        field_set["MRACE7"],
                        field_set["MRACE8"],
                        field_set["MRACE9"],
                        field_set["MRACE10"],
                        field_set["MRACE11"],
                        field_set["MRACE12"],
                        field_set["MRACE13"],
                        field_set["MRACE14"],
                        field_set["MRACE15"])
                        , new_case);

                    omb_mrace_recode(gs, new_case, MRACE_NAT_Rule(field_set["MRACE1"],
                        field_set["MRACE2"],
                        field_set["MRACE3"],
                        field_set["MRACE4"],
                        field_set["MRACE5"],
                        field_set["MRACE6"],
                        field_set["MRACE7"],
                        field_set["MRACE8"],
                        field_set["MRACE9"],
                        field_set["MRACE10"],
                        field_set["MRACE11"],
                        field_set["MRACE12"],
                        field_set["MRACE13"],
                        field_set["MRACE14"],
                        field_set["MRACE15"]));

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MRACE16_17"], MRACE16_17_FET_Rule(field_set["MRACE16"], field_set["MRACE17"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MRACE18_19"], MRACE18_19_FET_Rule(field_set["MRACE18"], field_set["MRACE19"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MRACE20_21"], MRACE20_21_FET_Rule(field_set["MRACE20"], field_set["MRACE21"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MRACE22_23"], MRACE22_23_FET_Rule(field_set["MRACE22"], field_set["MRACE23"]), new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FETHNIC"],
                        FETHNIC_FET_Rule(field_set["FETHNIC1"]
                        , field_set["FETHNIC2"]
                        , field_set["FETHNIC3"]
                        , field_set["FETHNIC4"])
                        , new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["METHNIC5"], field_set["METHNIC5"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FETHNIC5"], field_set["FETHNIC5"], new_case);

                    gs.set_multi_value(Parent_FET_IJE_to_MMRIA_Path["FRACE"], FRACE_FET_Rule(field_set["FRACE1"],
                        field_set["FRACE2"],
                        field_set["FRACE3"],
                        field_set["FRACE4"],
                        field_set["FRACE5"],
                        field_set["FRACE6"],
                        field_set["FRACE7"],
                        field_set["FRACE8"],
                        field_set["FRACE9"],
                        field_set["FRACE10"],
                        field_set["FRACE11"],
                        field_set["FRACE12"],
                        field_set["FRACE13"],
                        field_set["FRACE14"],
                        field_set["FRACE15"])
                        , new_case);

                    omb_frace_recode(gs, new_case, FRACE_FET_Rule(field_set["FRACE1"],
                        field_set["FRACE2"],
                        field_set["FRACE3"],
                        field_set["FRACE4"],
                        field_set["FRACE5"],
                        field_set["FRACE6"],
                        field_set["FRACE7"],
                        field_set["FRACE8"],
                        field_set["FRACE9"],
                        field_set["FRACE10"],
                        field_set["FRACE11"],
                        field_set["FRACE12"],
                        field_set["FRACE13"],
                        field_set["FRACE14"],
                        field_set["FRACE15"]));

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FRACE16_17"], FRACE16_17_FET_Rule(field_set["FRACE16"], field_set["FRACE16"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FRACE18_19"], FRACE18_19_FET_Rule(field_set["FRACE18"], field_set["FRACE19"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FRACE20_21"], FRACE20_21_FET_Rule(field_set["FRACE20"], field_set["FRACE21"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FRACE22_23"], FRACE22_23_FET_Rule(field_set["FRACE22"], field_set["FRACE23"]), new_case);


                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DOFP_MO"], TryPaseToIntOr_DefaultBlank(field_set["DOFP_MO"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DOFP_DY"], TryPaseToIntOr_DefaultBlank(field_set["DOFP_DY"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DOFP_YR"], TryPaseToIntOr_DefaultBlank(field_set["DOFP_YR"], "9999"), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DOLP_MO"], TryPaseToIntOr_DefaultBlank(field_set["DOLP_MO"], "9999"), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DOLP_DY"], TryPaseToIntOr_DefaultBlank(field_set["DOLP_DY"], "9999"), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["DOLP_YR"], TryPaseToIntOr_DefaultBlank(field_set["DOLP_YR"], "9999"), new_case);

                    
                    

                

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["PLUR"], PLUR_Custom_FET_Rule(field_set["PLUR"]), new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["MAGER"], MAGER_FET_Rule(field_set["MAGER"], 
                        field_set["MDOB_YR"], field_set["MDOB_MO"], field_set["MDOB_DY"], 
                        field_set["FDOD_YR"], field_set["FDOD_MO"], field_set["FDOD_DY"]), new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FAGER"], FAGER_FET_Rule(field_set["FAGER"],
                        field_set["FDOB_YR"], field_set["FDOB_MO"], 
                        field_set["FDOD_YR"], field_set["FDOD_MO"], field_set["FDOD_DY"]), new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["INFT_DRG"], field_set["INFT_DRG"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["INFT_ART"], field_set["INFT_ART"], new_case);
                    
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FBPLACD_ST_TER_C"], field_set["FBPLACD_ST_TER_C"], new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["FBPLACE_CNT_C"], field_set["FBPLACE_CNT_C"], new_case);


                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGPN"], CIGPN_Custom_FET_Rule(field_set["CIGPN"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGPN_prior_3_months_type"], CIGPN_Type_FET_Rule(field_set["CIGPN"]), new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGFN"], CIGFN_Custom_FET_Rule(field_set["CIGFN"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGFN_trimester_1st_type"], CIGFN_Type_FET_Rule(field_set["CIGFN"]), new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGSN"], CIGSN_Custom_FET_Rule(field_set["CIGSN"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGSN_trimester_2nd_type"], CIGSN_Type_FET_Rule(field_set["CIGSN"]), new_case);

                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGLN"], CIGLN_Custom_FET_Rule(field_set["CIGLN"]), new_case);
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIGLN_trimester_3rd_type"], CIGLN_Type_FET_Rule(field_set["CIGLN"]), new_case);
                    
                    gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CIG_none_or_not_specified"], 
                        CIG_none_or_not_specified_NAT_Rule(
                            field_set["CIGPN"],
                            field_set["CIGFN"],
                            field_set["CIGSN"],
                            field_set["CIGLN"]
                            ), new_case);



                    gs.set_multi_value(Parent_FET_IJE_to_MMRIA_Path["risk_factors_in_this_pregnancy"],
                            FET_risk_factors_in_this_pregnancy_Rule(
                                field_set["PDIAB"],
                                field_set["GDIAB"],
                                field_set["PHYPE"],
                                field_set["GHYPE"],
                                field_set["PPB"],
                                field_set["PPO"],
                                field_set["INFT"],
                                field_set["PCES"],
                                field_set["EHYPE"]
                            ), new_case);

                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["GON"],    field_set["GON"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["SYPH"],   field_set["SYPH"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CHAM"],   field_set["CHAM"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["LM"],     field_set["LM"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["GBS"],    field_set["GBS"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["CMV"],    field_set["CMV"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["B19"],    field_set["B19"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["TOXO"],   field_set["TOXO"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HSV"],    field_set["HSV"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HSV1"],   field_set["HSV1"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["HIV"],    field_set["HIV"], new_case);
                    //gs.set_value(Parent_FET_IJE_to_MMRIA_Path["OTHERI"], field_set["OTHERI"], new_case);

                    gs.set_multi_value(Parent_FET_IJE_to_MMRIA_Path["infections_present_or_treated_during_pregnancy"],
                            FET_infections_present_or_treated_during_pregnancy_Rule(
                                field_set["GON"], 
                                field_set["SYPH"],
                                field_set["CHAM"],
                                field_set["LM"], 
                                field_set["GBS"], 
                                field_set["CMV"], 
                                field_set["B19"], 
                                field_set["TOXO"],
                                field_set["HSV"], 
                                field_set["HSV1"],
                                field_set["HIV"], 
                                field_set["OTHERI"]
                            ), new_case);

                    gs.set_multi_value(Parent_FET_IJE_to_MMRIA_Path["maternal_morbidity"],
                            FET_maternal_morbidity_Rule(
                                field_set["MTR"],
                                field_set["PLAC"],
                                field_set["RUT"],
                                field_set["UHYS"],
                                field_set["AINT"],
                                field_set["UOPR"]
                            ), new_case);

                }



                #endregion

                var (gestation_weeks, gestation_days) =  CALCULATE_GESTATIONAL_AGE_AT_BIRTH_ON_BC
                (
                    gs.get_value(new_case,"birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year"),
                    gs.get_value(new_case,"birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month"),
                    gs.get_value(new_case,"birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day"),
                    gs.get_value(new_case,"birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year"),
                    gs.get_value(new_case,"birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month"),
                    gs.get_value(new_case,"birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day")
                );

                gs.set_value("birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation",gestation_weeks, new_case);
                gs.set_value("birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days", gestation_days, new_case);


                #region NAT Assignments
                for (int nat_index = 0; nat_index < nat_field_set?.Count; nat_index++)
                {
                    var new_natal_fetal_form = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                    mmria.services.vitalsimport.default_case.create(natal_fetal_metadata, new_natal_fetal_form, true);
                    
                    var list = new_natal_fetal_form["birth_certificate_infant_fetal_section"] as IList<object>;
                    
                    natal_fetal_list.Add(list[0]);
                    new_case_dictionary["birth_certificate_infant_fetal_section"] = natal_fetal_list;
                    
                    var live_birth = "0";
                    gs.set_multiform_value(new_case, "birth_certificate_infant_fetal_section/record_type", new List<(int, object)>() { (nat_index,  live_birth) });

                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["DATE_OF_DELIVERY"], new List<(int, object)>() { (nat_index, DATE_OF_DELIVERY_Rule(nat_field_set[nat_index]["IDOB_YR"], nat_field_set[nat_index]["IDOB_MO"], nat_field_set[nat_index]["IDOB_DY"])) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["HOSPTO"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["HOSPTO"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["FILENO"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["FILENO"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["AUXNO"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["AUXNO"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["TB"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["TB"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["ATTF"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["ATTF"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["ATTV"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["ATTV"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["PRES"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["PRES"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["ROUT"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["ROUT"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["APGAR5"], new List<(int, object)>() { (nat_index, TryPaseToIntOr_DefaultBlank(nat_field_set[nat_index]["APGAR5"], "")) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["APGAR10"], new List<(int, object)>() { (nat_index, TryPaseToIntOr_DefaultBlank(nat_field_set[nat_index]["APGAR10"], "")) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["SORD"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["SORD"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["ITRAN"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["ITRAN"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["ILIV"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["ILIV"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["BFED"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["BFED"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["INF_MED_REC_NUM"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["INF_MED_REC_NUM"]) });

                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["BWG"], new List<(int, object)>() { (nat_index, BWG_NAT_Rule(nat_field_set[nat_index]["BWG"])) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["BWG_unit_of_measurement"], new List<(int, object)>() { (nat_index, BWG_measu_NAT_Rule(nat_field_set[nat_index]["BWG"])) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["PLUR_is_multiple_gestation"], new List<(int, object)>() { (nat_index, PLUR_gesta_NAT_Rule(nat_field_set[nat_index]["PLUR"])) });
                    
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["abnormal_conditions_of_newborn"]
                        , new List<(int, object)>() { (nat_index, NAT_abnormal_Rule(nat_field_set[nat_index]["AVEN1"]
                        , nat_field_set[nat_index]["AVEN6"]
                        , nat_field_set[nat_index]["NICU"]
                        , nat_field_set[nat_index]["SURF"]
                        , nat_field_set[nat_index]["ANTI"]
                        , nat_field_set[nat_index]["SEIZ"]
                        , nat_field_set[nat_index]["BINJ"]
                        )) });

                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["congenital_anomalies"],
                        new List<(int, object)>() { (nat_index,
                        NAT_congenital_Rule(nat_field_set[nat_index]["ANEN"]
                            , nat_field_set[nat_index]["MNSB"]
                            , nat_field_set[nat_index]["CCHD"]
                            , nat_field_set[nat_index]["CDH"]
                            , nat_field_set[nat_index]["OMPH"]
                            , nat_field_set[nat_index]["GAST"]
                            , nat_field_set[nat_index]["LIMB"]
                            , nat_field_set[nat_index]["CL"]
                            , nat_field_set[nat_index]["CP"]
                            , nat_field_set[nat_index]["DOWT"]
                            , nat_field_set[nat_index]["CDIT"]
                            , nat_field_set[nat_index]["HYPO"]
                        )
                        ) });

                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["TLAB"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["TLAB"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["RECORD_TYPE"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["RECORD_TYPE"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["ISEX"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["ISEX"]) });

                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["SORD"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["SORD"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["INF_MED_REC_NUM"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["INF_MED_REC_NUM"]) });
                    gs.set_multiform_value(new_case, NAT_IJE_to_MMRIA_Path["APGAR10"], new List<(int, object)>() { (nat_index, nat_field_set[nat_index]["APGAR10"]) });



                }

                #endregion

                #region FET Assignments

                for (int fet_index = 0; fet_index < fet_field_set?.Count; fet_index++)
                {
                    var new_natal_fetal_form = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                    mmria.services.vitalsimport.default_case.create(natal_fetal_metadata, new_natal_fetal_form, true);
                    var list = new_natal_fetal_form["birth_certificate_infant_fetal_section"] as IList<object>;
                    
                    natal_fetal_list.Add(list[0]);
                    new_case_dictionary["birth_certificate_infant_fetal_section"] = natal_fetal_list;

//gs.set_multiform_value(p_object,p_path, list_change_set);
                    var fetal_death = "1";
                    gs.set_multiform_value(new_case, "birth_certificate_infant_fetal_section/record_type", new List<(int, object)>() { (fet_index,  fetal_death) });

                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["DATE_OF_DELIVERY"], new List<(int, object)>() { (fet_index, FET_DATE_OF_DELIVERY_Rule(fet_field_set[fet_index]["FDOD_YR"], fet_field_set[fet_index]["FDOD_MO"], fet_field_set[fet_index]["FDOD_DY"])) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["FILENO"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["FILENO"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["AUXNO"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["AUXNO"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["TD"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["TD"]) });

                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["ATTF"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["ATTF"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["ATTV"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["ATTV"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["PRES"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["PRES"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["ROUT"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["ROUT"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["SORD"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["SORD"]) });





                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a1"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a1"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a2"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a2"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a3"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a3"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a4"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a4"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a5"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a5"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a6"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a6"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a7"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a7"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a8"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a8"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a9"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a9"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a10"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a10"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a11"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a11"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a12"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a12"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a13"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a13"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18a14"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18a14"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b1"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b1"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b2"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b2"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b3"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b3"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b4"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b4"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b5"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b5"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b6"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b6"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b7"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b7"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b8"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b8"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b9"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b9"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b10"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b10"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b11"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b11"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b12"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b12"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b13"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b13"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["COD18b14"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["COD18b14"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["ICOD"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["ICOD"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD1"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD1"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD2"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD2"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD3"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD3"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD4"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD4"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD5"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD5"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD6"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD6"]) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["OCOD7"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["OCOD7"]) });

                gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["FSEX"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["FSEX"]) });
                gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["TLAB"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["TLAB"]) });
                gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["FWG"],  new List<(int, object)>() { (fet_index, FWG_pound_FET_Rule(fet_field_set[fet_index]["FWG"])) });
                gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["FWG_unit_of_measurement"],  new List<(int, object)>() { (fet_index, FWG_measure_FET_Rule(fet_field_set[fet_index]["FWG"])) });
                    gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["PLUR_is_multiple_gestation"],  new List<(int, object)>() { (fet_index, PLUR_gesta_FET_Rule(fet_field_set[fet_index]["PLUR"])) });

                gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["RECORD_TYPE"], new List<(int, object)>() { (fet_index, fet_field_set[fet_index]["RECORD_TYPE"]) });

                gs.set_multiform_value(new_case, FET_IJE_to_MMRIA_Path["congenital_anomalies"]
                    , new List<(int, object)>() { (fet_index,  FET_congenital_Rule(fet_field_set[fet_index]["ANEN"]
                        , fet_field_set[fet_index]["MNSB"]
                        , fet_field_set[fet_index]["CCHD"]
                        , fet_field_set[fet_index]["CDH"]
                        , fet_field_set[fet_index]["OMPH"]
                        , fet_field_set[fet_index]["GAST"]
                        , fet_field_set[fet_index]["LIMB"]
                        , fet_field_set[fet_index]["CL"]
                        , fet_field_set[fet_index]["CP"]
                        , fet_field_set[fet_index]["DOWT"]
                        , fet_field_set[fet_index]["CDIT"]
                        , fet_field_set[fet_index]["HYPO"]
                        )) });


                    string_builder.Clear();
                    string_builder.AppendLine($"Initiating cause/condition:");
                    string_builder.AppendLine($"01) Rupture of membranes prior to onset of labor: {fet_field_set[fet_index]["COD18a1"]}");
                    string_builder.AppendLine($"02) Abruptio placenta: {fet_field_set[fet_index]["COD18a2"]}");
                    string_builder.AppendLine($"03) Placental insufficiency: {fet_field_set[fet_index]["COD18a3"]}");
                    string_builder.AppendLine($"04) Prolapsed cord: {fet_field_set[fet_index]["COD18a4"]}");
                    string_builder.AppendLine($"05) Chorioamnionitis: {fet_field_set[fet_index]["COD18a5"]}");
                    string_builder.AppendLine($"06) Other complications of placenta, cord, or membranes: {fet_field_set[fet_index]["COD18a6"]}");
                    string_builder.AppendLine($"07) Unknown: {fet_field_set[fet_index]["COD18a7"]}");
                    string_builder.AppendLine($"08) Maternal conditions/diseases literal: {fet_field_set[fet_index]["COD18a8"]}");
                    string_builder.AppendLine($"09) Other complications of placenta, cord, or membranes literal: {fet_field_set[fet_index]["COD18a9"]}");
                    string_builder.AppendLine($"10) Other obstetrical or pregnancy complications literal: {fet_field_set[fet_index]["COD18a10"]}");
                    string_builder.AppendLine($"11) Fetal anomaly literal: {fet_field_set[fet_index]["COD18a11"]}");
                    string_builder.AppendLine($"12) Fetal injury literal: {fet_field_set[fet_index]["COD18a12"]}");
                    string_builder.AppendLine($"13) Fetal infection literal: {fet_field_set[fet_index]["COD18a13"]}");
                    string_builder.AppendLine($"14) Other fetal conditions/disorders literal: {fet_field_set[fet_index]["COD18a14"]}");
                    string_builder.AppendLine($"");
                    string_builder.AppendLine($"");
                    string_builder.AppendLine($"Other significant causes or conditions:");
                    string_builder.AppendLine($"01) Rupture of membranes prior to onset of labor: {fet_field_set[fet_index]["COD18b1"]} ");
                    string_builder.AppendLine($"02) Abruptio placenta: {fet_field_set[fet_index]["COD18b2"]}");
                    string_builder.AppendLine($"03) Placental insufficiency: {fet_field_set[fet_index]["COD18b3"]}");
                    string_builder.AppendLine($"04) Prolapsed cord: {fet_field_set[fet_index]["COD18b4"]}");
                    string_builder.AppendLine($"05) Chorioamnionitis: {fet_field_set[fet_index]["COD18b5"]}");
                    string_builder.AppendLine($"06) Other complications of placenta, cord, or membranes: {fet_field_set[fet_index]["COD18b6"]}");
                    string_builder.AppendLine($"07) Unknown: {fet_field_set[fet_index]["COD18b7"]}");
                    string_builder.AppendLine($"08) Maternal conditions/diseases literal: {fet_field_set[fet_index]["COD18b8"]}");
                    string_builder.AppendLine($"09) Other complications of placenta, cord, or membranes literal: {fet_field_set[fet_index]["COD18b9"]}");
                    string_builder.AppendLine($"10) Other obstetrical or pregnancy complications literal: {fet_field_set[fet_index]["COD18b10"]}");
                    string_builder.AppendLine($"11) Fetal anomaly literal: {fet_field_set[fet_index]["COD18b11"]}");
                    string_builder.AppendLine($"12) Fetal injury literal: {fet_field_set[fet_index]["COD18b12"]}");
                    string_builder.AppendLine($"13) Fetal infection literal: {fet_field_set[fet_index]["COD18b13"]}");
                    string_builder.AppendLine($"14) Other fetal conditions/disorders literal: {fet_field_set[fet_index]["COD18b14"]}");
                    string_builder.AppendLine($"");
                    string_builder.AppendLine($"Coded initiating cause/condition: {fet_field_set[fet_index]["ICOD"]}");
                    string_builder.AppendLine($"Coded other significant causes or conditions:");
                    string_builder.AppendLine($"01) First mentioned: {fet_field_set[fet_index]["OCOD1"]} ");
                    string_builder.AppendLine($"02) Second mentioned: {fet_field_set[fet_index]["OCOD2"]}");
                    string_builder.AppendLine($"03) Third mentioned: {fet_field_set[fet_index]["OCOD3"]}");
                    string_builder.AppendLine($"04) Fourth mentioned: {fet_field_set[fet_index]["OCOD4"]}");
                    string_builder.AppendLine($"05) Fifth mentioned: {fet_field_set[fet_index]["OCOD5"]}");
                    string_builder.AppendLine($"06) Sixth mentioned: {fet_field_set[fet_index]["OCOD6"]}");
                    string_builder.AppendLine($"07) Seventh mentioned: {fet_field_set[fet_index]["OCOD7"]}");

                    var res = gs.set_multiform_value(new_case, "birth_certificate_infant_fetal_section/vitals_import_group/summary_text",new List<(int, object)>(){ (fet_index, string_builder.ToString())});

                    if(!res)
                    {
                        Console.WriteLine("error");
                    }

                }
            }

            birth_distance(gs, new_case);
            death_distance(gs, new_case);

            var item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery");

            var weight_at_delivery = item_result.is_error || item_result.result == null ? null : item_result.result.ToString();
            item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight");
            var pre_pregnancy_weight = item_result.is_error || item_result.result == null ? null : item_result.result.ToString();
            item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/maternal_biometrics/height_feet");
            var height_feet_string = item_result.is_error || item_result.result == null ? null : item_result.result.ToString();
            item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/maternal_biometrics/height_inches");
            var height_inches_string = item_result.is_error || item_result.result == null  ? null : item_result.result.ToString();

//Weight Gain during Pregnancy (lbs) (bfdcpmb_w_gain)
//birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain
//birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery
//birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight

double weight_del = double.NaN;
double.TryParse(weight_at_delivery, out weight_del);

double weight_pp = double.NaN;
double.TryParse(pre_pregnancy_weight, out weight_pp);


if (weight_del > 50 && weight_del < 800 && weight_pp > 50 && weight_pp < 800) 
{
    var gain = weight_del - weight_pp;
    gs.set_value("birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain", $"{gain:0.00}", new_case);
}



//Pre-Pregnancy BMI* (bfdcpmb_bmi)
//birth_fetal_death_certificate_parent/maternal_biometrics/bmi
//birth_fetal_death_certificate_parent/maternal_biometrics/height_feet
//birth_fetal_death_certificate_parent/maternal_biometrics/height_inches
//birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight
double height_feet = double.NaN;
double.TryParse(height_feet_string, out height_feet);

double height_inches  = double.NaN;
double.TryParse(height_inches_string, out height_inches);

double weight = double.NaN;

double.TryParse(pre_pregnancy_weight, out weight);
double height = height_feet * 12 + height_inches;
if (height > 24 && height < 108 && weight > 50 && weight < 800) 
{
    var bmi = calc_bmi(height, weight);
    gs.set_value("birth_fetal_death_certificate_parent/maternal_biometrics/bmi", $"{bmi:0.00}", new_case);
}

double calc_bmi(double height, double weight) 
{
    double bmi = double.NaN;
    height /= 39.3700787;
    weight /= 2.20462;
    bmi = Math.Round(weight / Math.Pow(height, 2D) * 10D) / 10D;
    return bmi;
}



//addquarter
gs.set_value("addquarter", get_year_and_quarter(DateTime.Now), new_case);



string primary_occupation = null;
string business_industry = null;

//OCCUP
//mor_field_set["OCCUP"]
item_result = gs.get_value(new_case, "death_certificate/demographics/primary_occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

//INDUST
item_result = gs.get_value(new_case, "death_certificate/demographics/occupation_business_industry");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    business_industry = item_result.result.ToString();
}
var niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
    if(niosh_result.Industry.Count > 0)                      
    gs.set_value("death_certificate/demographics/dc_m_industry_code_1", niosh_result.Industry[0].Code, new_case);
    if(niosh_result.Industry.Count > 1)
    gs.set_value("death_certificate/demographics/dc_m_industry_code_2",  niosh_result.Industry[1].Code, new_case);
    if(niosh_result.Industry.Count > 2)
    gs.set_value("death_certificate/demographics/dc_m_industry_code_3",  niosh_result.Industry[2].Code, new_case);
    if(niosh_result.Occupation.Count > 0)
    gs.set_value("death_certificate/demographics/dc_m_occupation_code_1",  niosh_result.Occupation[0].Code, new_case);
    if(niosh_result.Occupation.Count > 1)
    gs.set_value("death_certificate/demographics/dc_m_occupation_code_2", niosh_result.Occupation[1].Code, new_case);
    if(niosh_result.Occupation.Count > 2)
    gs.set_value("death_certificate/demographics/dc_m_occupation_code_3", niosh_result.Occupation[2].Code, new_case);
}



primary_occupation = null;
business_industry = null;

//DAD_OC_T
item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

//DAD_IN_T
item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    business_industry = item_result.result.ToString();
}
niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
    if(niosh_result.Industry.Count > 0)                      
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_1", niosh_result.Industry[0].Code, new_case);
    if(niosh_result.Industry.Count > 1)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_2",  niosh_result.Industry[1].Code, new_case);
    if(niosh_result.Industry.Count > 2)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_3",  niosh_result.Industry[2].Code, new_case);
    if(niosh_result.Occupation.Count > 0)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_1",  niosh_result.Occupation[0].Code, new_case);
    if(niosh_result.Occupation.Count > 1)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_2", niosh_result.Occupation[1].Code, new_case);
    if(niosh_result.Occupation.Count > 2)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_3", niosh_result.Occupation[2].Code, new_case);
}


primary_occupation = null;
business_industry = null;
//MOM_OC_T
item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

//MOM_IN_T
item_result = gs.get_value(new_case, "birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    business_industry = item_result.result.ToString();
}
niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
    if(niosh_result.Industry.Count > 0)                      
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_1", niosh_result.Industry[0].Code, new_case);
    if(niosh_result.Industry.Count > 1)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_2",  niosh_result.Industry[1].Code, new_case);
    if(niosh_result.Industry.Count > 2)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_3",  niosh_result.Industry[2].Code, new_case);
    if(niosh_result.Occupation.Count > 0)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_1",  niosh_result.Occupation[0].Code, new_case);
    if(niosh_result.Occupation.Count > 1)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_2", niosh_result.Occupation[1].Code, new_case);
    if(niosh_result.Occupation.Count > 2)
    gs.set_value("birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_3", niosh_result.Occupation[2].Code, new_case);
}

/*

primary_occupation = null;
business_industry = null;

item_result = gs.get_value(new_case, "/social_and_environmental_profile/socio_economic_characteristics/occupation");
if
(
    !item_result.is_error && 
    item_result.result != null &&
    !string.IsNullOrWhiteSpace(item_result.result.ToString())
)
{
    primary_occupation = item_result.result.ToString();
}

niosh_result = get_niosh_codes
(
    primary_occupation,
    business_industry
);

if
(
    !niosh_result.is_error && 
    (
        niosh_result.Industry.Count > 0 ||
        niosh_result.Occupation.Count > 0 
    )
)
{   
    if(niosh_result.Industry.Count > 0)                      
    gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1", niosh_result.Industry[0].Code, new_case);
    if(niosh_result.Industry.Count > 1)
    gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2",  niosh_result.Industry[1].Code, new_case);
    if(niosh_result.Industry.Count > 2)
    gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3",  niosh_result.Industry[2].Code, new_case);
    if(niosh_result.Occupation.Count > 0)
    gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1",  niosh_result.Occupation[0].Code, new_case);
    if(niosh_result.Occupation.Count > 1)
    gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2", niosh_result.Occupation[1].Code, new_case);
    if(niosh_result.Occupation.Count > 2)
    gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3", niosh_result.Occupation[2].Code, new_case);
}

*/
        







            





            #endregion

            var case_dictionary = new_case as IDictionary<string, object>;

            var finished = new mmria.common.ije.BatchItem()
            {
                Status = mmria.common.ije.BatchItem.StatusEnum.NewCaseAdded,
                CDCUniqueID = mor_field_set["SSN"],
                ImportDate = message.ImportDate,
                ImportFileName = message.ImportFileName,
                ReportingState = message.host_state,

                StateOfDeathRecord = mor_field_set["DSTATE"],
                DateOfDeath = $"{mor_field_set["DOD_YR"]}-{mor_field_set["DOD_MO"]}-{mor_field_set["DOD_DY"]}",
                DateOfBirth = $"{mor_field_set["DOB_YR"]}-{mor_field_set["DOB_MO"]}-{mor_field_set["DOB_DY"]}",
                LastName = mor_field_set["LNAME"],
                FirstName = mor_field_set["GNAME"],
                
                mmria_record_id = message.record_id,
                mmria_id = mmria_id,
                StatusDetail = "Added new case"
            };


            var _dbConfigSet = mmria.services.vitalsimport.Program.DbConfigSet;
            var db_info = _dbConfigSet.detail_list[message.host_state];
            string request_string = $"{db_info.url}/{db_info.prefix}mmrds/{mmria_id}";

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(new_case, settings);

            var document_curl = new mmria.getset.cURL("PUT", null, request_string, object_string, db_info.user_name, db_info.user_value);

            var document_put_response = new mmria.common.model.couchdb.document_put_response();
            try
            {
                var responseFromServer = document_curl.execute();
                document_put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch (Exception ex)
            {
                finished = new mmria.common.ije.BatchItem()
                {
                    Status = mmria.common.ije.BatchItem.StatusEnum.ImportFailed,
                    CDCUniqueID = mor_field_set["SSN"],
                    ImportDate = message.ImportDate,
                    ImportFileName = message.ImportFileName,
                    ReportingState = message.host_state,

                    StateOfDeathRecord = mor_field_set["DSTATE"],
                    DateOfDeath = $"{mor_field_set["DOD_YR"]}-{mor_field_set["DOD_MO"]}-{mor_field_set["DOD_DY"]}",
                    DateOfBirth = $"{mor_field_set["DOB_YR"]}-{mor_field_set["DOB_MO"]}-{mor_field_set["DOB_DY"]}",
                    LastName = mor_field_set["LNAME"],
                    FirstName = mor_field_set["GNAME"],
                    mmria_record_id = message.record_id,
                    mmria_id = mmria_id,
                    StatusDetail = "Error\n" + ex.ToString()
                };
            }

            Sender.Tell(finished);

            Context.Stop(this.Self);

        }


        /*
        fail if three records do NOT match file names record in report

        exclude any records that do NOT have - content validation failed

        1.	Date of Death
        2.	SODR
        3.	CDC generated Unique ID

        key fields - only exact matches are excluded

        1 mmria site/host/ reporting state - file-name ? or 2.	SODR
        2 home_record/state of death - DState
        3 home_record/date_of_death - DOD_YR, DOD_MO, DOD_DY
        4 death_certificate/date_of_birth - DOB_YR, DOB_MO, DOD_BY
        5 home_record/last_name - LNAME  
        6 home_record/first_name - GNAME

        */


    }

    

    private void omb_mrace_recode(migrate.C_Get_Set_Value gs, System.Dynamic.ExpandoObject new_case, string[] race)
    {
        string race_recode = null;
        race_recode = calculate_omb_recode(race);
        gs.set_value("birth_fetal_death_certificate_parent/race/omb_race_recode", race_recode, new_case);
    }

    private void omb_frace_recode(migrate.C_Get_Set_Value gs, System.Dynamic.ExpandoObject new_case, string[] race)
    {
        string race_recode = null;
        race_recode = calculate_omb_recode(race);
        gs.set_value("birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode", race_recode, new_case);
    }

    private string TryPaseToIntOr_DefaultBlank(string value, string defaultString = "99")
    {
        string result = defaultString;

        if(int.TryParse(value, out int value_result))
        {
            result = value_result.ToString();
        }

        return result;
    }

    private string TryPaseToInt_00_To30(string value)
    {
        string result = "";

        if
        (
            int.TryParse(value, out int value_result) &&
            value_result >= 00 && 
            value_result <= 30
        )
        {
            result = value_result.ToString();
        }

        return result;
    }

    private void death_distance(migrate.C_Get_Set_Value gs, System.Dynamic.ExpandoObject new_case)
    {
        if (!string.IsNullOrWhiteSpace(death_certificate_place_of_last_residence_latitude)
            && !string.IsNullOrWhiteSpace(death_certificate_place_of_last_residence_longitude)
            && !string.IsNullOrWhiteSpace(death_certificate_address_of_death_latitude)
            && !string.IsNullOrWhiteSpace(death_certificate_address_of_death_longitude))
        {
            double? dist = null;
            float.TryParse(death_certificate_place_of_last_residence_latitude, out float res_lat);
            float.TryParse(death_certificate_place_of_last_residence_longitude, out float res_lon);
            float.TryParse(death_certificate_address_of_death_latitude, out float hos_lat);
            float.TryParse(death_certificate_address_of_death_longitude, out float hos_lon);
            if (res_lat >= -90 && res_lat <= 90 && res_lon >= -180 && res_lon <= 180 && hos_lat >= -90 && hos_lat <= 90 && hos_lon >= -180 && hos_lon <= 180)
            {
                dist = calc_distance(res_lat, res_lon, hos_lat, hos_lon);
                gs.set_value("death_certificate/address_of_death/estimated_death_distance_from_residence", dist?.ToString(), new_case);
            }
        }
    }

    private void birth_distance(migrate.C_Get_Set_Value gs, System.Dynamic.ExpandoObject new_case)
    {
        if (!string.IsNullOrWhiteSpace(location_of_residence_latitude)
            && !string.IsNullOrWhiteSpace(location_of_residence_longitude)
            && !string.IsNullOrWhiteSpace(facility_of_delivery_location_latitude)
            && !string.IsNullOrWhiteSpace(facility_of_delivery_location_longitude))
        {

            double? dist = null;
            float.TryParse(location_of_residence_latitude, out float res_lat);
            float.TryParse(location_of_residence_longitude, out float res_lon);
            float.TryParse(facility_of_delivery_location_latitude, out float hos_lat);
            float.TryParse(facility_of_delivery_location_longitude, out float hos_lon);
            if (res_lat >= -90 && res_lat <= 90 && res_lon >= -180 && res_lon <= 180 && hos_lat >= -90 && hos_lat <= 90 && hos_lon >= -180 && hos_lon <= 180)
            {
                dist = calc_distance(res_lat, res_lon, hos_lat, hos_lon);
                gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/estimated_distance_from_residence", dist?.ToString(), new_case);
            }
        }
    }

    private double calc_distance(float lat1, float lon1, float lat2, float lon2)
    {
        var radlat1 = Math.PI * lat1 / 180;
        var radlat2 = Math.PI * lat2 / 180;
        var theta = lon1 - lon2;
        var radtheta = Math.PI * theta / 180;
        var dist = Math.Sin(radlat1) * Math.Sin(radlat2) + Math.Cos(radlat1) * Math.Cos(radlat2) * Math.Cos(radtheta);
        dist = Math.Acos(dist);
        dist = dist * 180 / Math.PI;
        dist = Math.Round(dist * 60 * 1.1515 * 100) / 100;
        return dist;
    }

    private void omb_race_recode_dc(migrate.C_Get_Set_Value gs, System.Dynamic.ExpandoObject new_case, string[] race)
    {
        string race_recode = null;
        race_recode = calculate_omb_recode(race);
        gs.set_value("death_certificate/race/omb_race_recode", race_recode, new_case);
    }

    private string calculate_omb_recode(string[] p_value_list)
    {
        string result = "9999";
        var asian_list = new Dictionary<int, string>(){ 
                {7,"Asian Indian"},
                {8,"Chinese"},
                {9,"Filipino"},
                {10,"Japanese"},
                {11,"Korean"},
                {12,"Vietnamese"},
                {13,"Other Asian"}
            };
        var islander_list = new Dictionary<int, string>(){
                {3,"Native Hawaiian"},
                {4,"Guamanian or Chamorro"},
                {5,"Samoan"},
                {6,"Other Pacific Islander"}
            };
        if (p_value_list.Length == 0)
        {
            System.Console.WriteLine("here");
        }
        else if (p_value_list.Length == 1)
        {
            if (get_intersection(p_value_list, asian_list)?.Length > 0) 
            {
                result = "4"; //"Asian";
            } 
            else if (get_intersection(p_value_list, islander_list)?.Length > 0) 
            {
                result = "3"; //"Pacific Islander";
            } 
            else
            {
                result = p_value_list[0];
            }
        }
        else
        {
            if (p_value_list.Contains("8888"))
            {
                result = "8888"; //Race Not Specified";
            }
            else
            {
                var asian_intersection_count = get_intersection(p_value_list, asian_list)?.Length;
                var is_asian = 0;
                var islander_intersection_count = get_intersection(p_value_list, islander_list)?.Length;
                var is_islander = 0;
                if (asian_intersection_count > 0)
                    is_asian = 1;
                if (islander_intersection_count > 0)
                    is_islander = 1;
                var number_not_in_asian_or_islander_categories = p_value_list.Length - asian_intersection_count - islander_intersection_count;
                var total_unique_items = number_not_in_asian_or_islander_categories + is_asian + is_islander;
                switch (total_unique_items)
                {
                    case 1:
                        if (is_asian == 1)
                        {
                            result = "4"; //"Asian";
                        }
                        else if (is_islander == 1)
                        {
                            result = "3"; //"Pacific Islander";
                        }
                        else
                        {
                            Console.WriteLine("This should never happen bug");
                        }
                        break;
                    case 2:
                        result = "5";//"Bi-Racial";
                        break;
                    default:
                        result = "6"; //"Multi-Racial";
                        break;
                }
            }
        }
        return result;
    }

    public string[] get_intersection(string[] p_list_1, Dictionary<int,string> p_list_2)
    {
        List<string> result = new();

        foreach(var item_string in p_list_1)
        {
            if(int.TryParse(item_string, out var item))
            {
                if(p_list_2.ContainsKey(item))
                {
                    result.Add(item_string);
                }
            }
        }

        //var a = p_list_1;
        //var b = p_list_2;
        //a.sort();
        //b.sort();
        //var ai = 0, bi = 0;
        //var result = [];
        //while (ai < a.length && bi < b.length)
        //{
        //    if (a[ai] < b[bi])
        //    {
        //        ai++;
        //    }
        //    else if (a[ai] > b[bi])
        //    {
        //        bi++;
        //    }
        //    else
        //    {
        //        result.push(a[ai]);
        //        ai++;
        //        bi++;
        //    }
        //}
        return result.ToArray();
    }

    private void birth_2_death(migrate.C_Get_Set_Value gs, System.Dynamic.ExpandoObject new_case
        , string date_of_delivery_year, string date_of_delivery_month, string date_of_delivery_day
        , string date_of_death_year, string date_of_death_month, string date_of_death_day)
    {
            double? length_between_child_birth_and_death_of_mother = null;
            int.TryParse(date_of_delivery_year, out int start_year);
            int.TryParse(date_of_delivery_month, out int start_month);
            int.TryParse(date_of_delivery_day, out int start_day);
            int.TryParse(date_of_death_year, out int end_year);
            int.TryParse(date_of_death_month, out int end_month);
            int.TryParse(date_of_death_day, out int end_day);

            if (DateTime.TryParse($"{start_year}/{start_month}/{start_day}", out DateTime startDateTest) == true 
                && DateTime.TryParse($"{end_year}/{end_month}/{end_day}", out DateTime endDateTest) == true) 
            {
                var time_span = endDateTest - startDateTest;

                //var days = $global.calc_days(start_date, end_date);
                var days = time_span.Days;
                length_between_child_birth_and_death_of_mother = (double) days;
            }

            gs.set_value("birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother", length_between_child_birth_and_death_of_mother?.ToString(), new_case);
    }

    private void Set_facility_of_delivery_location_Gecocode(migrate.C_Get_Set_Value gs, GeocodeTuple geocode_data, System.Dynamic.ExpandoObject new_case)
    {
        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";
        urban_status = "";
        state_county_fips = "";

        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;
        
        if
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {
            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;
            // calculate urban_status
            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }

            facility_of_delivery_location_latitude = latitude;
            facility_of_delivery_location_longitude = longitude;
        }

        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/feature_matching_geography_type", feature_matching_geography_type, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/latitude", latitude, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/longitude", longitude, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_code", naaccr_gis_coordinate_quality_code, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_type", naaccr_gis_coordinate_quality_type, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_code", naaccr_census_tract_certainty_code, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_type", naaccr_census_tract_certainty_type, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/census_state_fips", census_state_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/census_county_fips", census_county_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/census_tract_fips", census_tract_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_fips", census_cbsa_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_micro", census_cbsa_micro, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/census_met_div_fips", census_met_div_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status", urban_status, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/facility_of_delivery_location/state_county_fips", state_county_fips, new_case);
        
    }

    private void Set_location_of_residence_Gecocode(migrate.C_Get_Set_Value gs, GeocodeTuple geocode_data, System.Dynamic.ExpandoObject new_case)
    {
        
        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";


        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;

        if 
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {
            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;

            // calculate urban_status
            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }

            location_of_residence_latitude = latitude;
            location_of_residence_longitude = longitude;
        }
        else
        {

            urban_status = "";
            state_county_fips = "";


        }


        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/feature_matching_geography_type", feature_matching_geography_type, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/latitude", latitude, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/longitude", longitude, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_code", naaccr_gis_coordinate_quality_code, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_type", naaccr_gis_coordinate_quality_type, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_code", naaccr_census_tract_certainty_code, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_type", naaccr_census_tract_certainty_type, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/census_state_fips", census_state_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/census_county_fips", census_county_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/census_tract_fips", census_tract_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_fips", census_cbsa_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_micro", census_cbsa_micro, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/census_met_div_fips", census_met_div_fips, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/urban_status", urban_status, new_case);
        gs.set_value("birth_fetal_death_certificate_parent/location_of_residence/state_county_fips", state_county_fips, new_case);

    }

    private void Set_place_of_last_residence_Gecocode(migrate.C_Get_Set_Value gs, GeocodeTuple geocode_data, System.Dynamic.ExpandoObject new_case)
    {

        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";
        urban_status = "";
        state_county_fips = "";

        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;
        
        if
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {

            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;

            // calculate urban_status

            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }


            death_certificate_place_of_last_residence_latitude = latitude;
            death_certificate_place_of_last_residence_longitude = longitude;
        }

        gs.set_value("death_certificate/place_of_last_residence/feature_matching_geography_type", feature_matching_geography_type, new_case);
        gs.set_value("death_certificate/place_of_last_residence/latitude", latitude, new_case);
        gs.set_value("death_certificate/place_of_last_residence/longitude", longitude, new_case);
        gs.set_value("death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_code", naaccr_gis_coordinate_quality_code, new_case);
        gs.set_value("death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_type", naaccr_gis_coordinate_quality_type, new_case);
        gs.set_value("death_certificate/place_of_last_residence/naaccr_census_tract_certainty_code", naaccr_census_tract_certainty_code, new_case);
        gs.set_value("death_certificate/place_of_last_residence/naaccr_census_tract_certainty_type", naaccr_census_tract_certainty_type, new_case);
        gs.set_value("death_certificate/place_of_last_residence/census_state_fips", census_state_fips, new_case);
        gs.set_value("death_certificate/place_of_last_residence/census_county_fips", census_county_fips, new_case);
        gs.set_value("death_certificate/place_of_last_residence/census_tract_fips", census_tract_fips, new_case);
        gs.set_value("death_certificate/place_of_last_residence/census_cbsa_fips", census_cbsa_fips, new_case);
        gs.set_value("death_certificate/place_of_last_residence/census_cbsa_micro", census_cbsa_micro, new_case);
        gs.set_value("death_certificate/place_of_last_residence/census_met_div_fips", census_met_div_fips, new_case);
        gs.set_value("death_certificate/place_of_last_residence/urban_status", urban_status, new_case);
        gs.set_value("death_certificate/place_of_last_residence/state_county_fips", state_county_fips, new_case);

        
    }

    private void Set_address_of_death_Gecocode(migrate.C_Get_Set_Value gs, GeocodeTuple geocode_data, System.Dynamic.ExpandoObject new_case)
    {
        
        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";

        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;
        

        if 
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {
            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;

            // calculate urban_status
            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }

            death_certificate_address_of_death_latitude = latitude;
            death_certificate_address_of_death_longitude = longitude;
        }
        else
        {

            urban_status = "";
            state_county_fips = "";

        }

        gs.set_value("death_certificate/address_of_death/feature_matching_geography_type", feature_matching_geography_type, new_case);
        gs.set_value("death_certificate/address_of_death/latitude", latitude, new_case);
        gs.set_value("death_certificate/address_of_death/longitude", longitude, new_case);
        gs.set_value("death_certificate/address_of_death/naaccr_gis_coordinate_quality_code", naaccr_gis_coordinate_quality_code, new_case);
        gs.set_value("death_certificate/address_of_death/naaccr_gis_coordinate_quality_type", naaccr_gis_coordinate_quality_type, new_case);
        gs.set_value("death_certificate/address_of_death/naaccr_census_tract_certainty_code", naaccr_census_tract_certainty_code, new_case);
        gs.set_value("death_certificate/address_of_death/naaccr_census_tract_certainty_type", naaccr_census_tract_certainty_type, new_case);
        gs.set_value("death_certificate/address_of_death/census_state_fips", census_state_fips, new_case);
        gs.set_value("death_certificate/address_of_death/census_county_fips", census_county_fips, new_case);
        gs.set_value("death_certificate/address_of_death/census_tract_fips", census_tract_fips, new_case);
        gs.set_value("death_certificate/address_of_death/census_cbsa_fips", census_cbsa_fips, new_case);
        gs.set_value("death_certificate/address_of_death/census_cbsa_micro", census_cbsa_micro, new_case);
        gs.set_value("death_certificate/address_of_death/census_met_div_fips", census_met_div_fips, new_case);
        gs.set_value("death_certificate/address_of_death/urban_status", urban_status, new_case);
        gs.set_value("death_certificate/address_of_death/state_county_fips", state_county_fips, new_case);

    }

    public sealed class GeocodeTuple
    {
        public GeocodeTuple(){}

        public mmria.common.texas_am.OutputGeocode OutputGeocode {get;set;}
        public mmria.common.texas_am.CensusValue Census_Value {get;set;}

    }

    private GeocodeTuple get_geocode_info(string street, string city, string state, string zip)
    {

        var result = new GeocodeTuple();

        if (!string.IsNullOrEmpty(state))
        {
            var check_state = state.Split("-");
            state = check_state[0];
        }

        var TAMUGeocoder = new mmria.services.vitalsimport.Utilities.TAMUGeoCode();

        var response = TAMUGeocoder.execute(geocode_api_key, street, city, state, zip);
        
        if(response!= null && response.OutputGeocodes?.Length > 0)
        {
            result.OutputGeocode = response.OutputGeocodes[0].OutputGeocode;

            if(response.OutputGeocodes[0].CensusValues.Count > 0)
            {
                if(response.OutputGeocodes[0].CensusValues[0].ContainsKey("CensusValue1"))
                {
                    result.Census_Value = response.OutputGeocodes[0].CensusValues[0]["CensusValue1"];
                }
                
            }
        }

        return result;
    }

    struct Result_Struct
    {
        public System.Dynamic.ExpandoObject[] docs;
    }

    struct Selector_Struc
    {
        //public System.Dynamic.ExpandoObject selector;
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> selector;
        public string[] fields;

        public int limit;
    }

    private async Task<Result_Struct> get_matching_cases_for(string p_selector, string p_find_value)
    {

        Result_Struct result = new Result_Struct();

        try
        {

            var selector_struc = new Selector_Struc();
            selector_struc.selector = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            selector_struc.limit = 10000;
            selector_struc.selector.Add(p_selector, new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            selector_struc.selector[p_selector].Add("$eq", p_find_value);

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string selector_struc_string = Newtonsoft.Json.JsonConvert.SerializeObject(selector_struc, settings);

            System.Console.WriteLine(selector_struc_string);

            /*
                            string find_url = $"{db_server_url}/{db_name}/_find";

                            var case_curl = new mmria.getset.cURL("POST", null, find_url, selector_struc_string, config_timer_user_name, config_timer_value);
                            string responseFromServer = await case_curl.executeAsync();

                            result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result_Struct>(responseFromServer);
            */
            System.Console.WriteLine($"case_response.docs.length {result.docs.Length}");
        }
        catch (Exception ex)
        {

        }

        return result;
    }

    private Dictionary<string, mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string, mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }


    private  mmria.common.metadata.value_node[] get_metadata_value_node(string search_path, mmria.common.metadata.app p_metadata, string path = "")
    {
        mmria.common.metadata.value_node[] result = null;

        foreach (var node in p_metadata.children)
        {
            result = get_metadata_value_node(search_path, node, node.name);
            if(result != null) break;
        }
        return result;
    }

    private mmria.common.metadata.value_node[] get_metadata_value_node(string search_path, mmria.common.metadata.node p_metadata, string path = "")
    {
        mmria.common.metadata.value_node[] result = null;
        string key = $"{path}/{p_metadata.name}";
        if(search_path.Equals(path, StringComparison.OrdinalIgnoreCase))
        {
            if(! string.IsNullOrWhiteSpace(p_metadata.path_reference))
            {
                result = lookup[p_metadata.path_reference];
            }
            else
            {
                result = p_metadata.values;
            }
        }
        else if(p_metadata.children!= null)
        {
            foreach (var node in p_metadata.children)
            {
                result = get_metadata_value_node(search_path, node, $"{path}/{node.name}");
                if(result != null) break;
            }
        }
        return result;
    }

    private mmria.common.ije.BatchItem Convert
    (
            string LineItem,
            DateTime ImportDate,
            string ImportFileName,
            string ReportingState
    )
    {

        var x = mor_get_header(LineItem);
        var result = new mmria.common.ije.BatchItem()
        {
            Status = mmria.common.ije.BatchItem.StatusEnum.InProcess,
            CDCUniqueID = x["SSN"],
            ImportDate = ImportDate,
            ImportFileName = ImportFileName,
            ReportingState = ReportingState,

            StateOfDeathRecord = x["DSTATE"],
            DateOfDeath = $"{x["DOD_YR"]}-{x["DOD_MO"]}-{x["DOD_DY"]}",
            DateOfBirth = $"{x["DOB_YR"]}-{x["DOB_MO"]}-{x["DOB_DY"]}",
            LastName = x["LNAME"],
            FirstName = x["GNAME"]//,
            //MMRIARecordID = x[""],
            //StatusDetail = x[""]
        };

        return result;
    }

    private Dictionary<string, string> mor_get_header(string row)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /*
DState 5 2
DOD_YR 1 4, 
DOD_MO 237 2, 
DOD_DY 239 2
DOB_YR 205 4, 
DOB_MO 209 2, 
DOD_DY 239 2
LNAME 78 50
GNAME 27 50
*/
        //result.Add("DState",row.Substring(5-1, 2).Trim());
        //result.Add("DOD_YR",row.Substring(1-1, 4).Trim());
        //result.Add("DOD_MO",row.Substring(237-1, 2).Trim());
        //result.Add("DOD_DY",row.Substring(239-1, 2).Trim());
        //result.Add("DOB_YR",row.Substring(205-1, 4).Trim());
        //result.Add("DOB_MO",row.Substring(209-1, 2).Trim());
        //result.Add("DOB_DY",row.Substring(211-1, 2).Trim());
        //result.Add("LNAME",row.Substring(78-1, 50).Trim());
        //result.Add("GNAME",row.Substring(27-1, 50).Trim());
        //result.Add("SSN",row.Substring(191-1, 9).Trim());

        result.Add("DOD_YR", DOD_YR_Rule(row.Substring(0, 4).Trim()));
        result.Add("DSTATE", row.Substring(4, 2).Trim());
        result.Add("FILENO", row.Substring(6, 6).Trim());
        result.Add("AUXNO", row.Substring(13, 12).Trim());
        result.Add("GNAME", row.Substring(26, 50).Trim());
        result.Add("LNAME", row.Substring(77, 50).Trim());
        result.Add("SSN", row.Substring(190, 9).Trim());
        result.Add("AGETYPE", row.Substring(199, 1).Trim());
        result.Add("AGE", AGE_Rule(row.Substring(200, 3).Trim()));
        result.Add("DMAIDEN", AGE_Rule(row.Substring(3341, 50).Trim()));
        result.Add("DOB_YR", row.Substring(204, 4).Trim());
        result.Add("DOB_MO", DOB_MO_Rule(row.Substring(208, 2).Trim()));
        result.Add("DOB_DY", DOB_DY_Rule(row.Substring(210, 2).Trim()));
        result.Add("BPLACE_CNT", row.Substring(212, 2).Trim());
        result.Add("BPLACE_ST", BPLACE_ST_Rule(row.Substring(214, 2).Trim()));
        result.Add("CITYC", row.Substring(216, 5).Trim());
        result.Add("COUNTYC", row.Substring(221, 3).Trim());
        result.Add("STATEC", STATEC_Rule(row.Substring(224, 2).Trim()));
        result.Add("COUNTRYC", COUNTRYC_Rule(row.Substring(226, 2).Trim()));
        result.Add("MARITAL", MARITAL_Rule(row.Substring(229, 1).Trim()));
        result.Add("DPLACE", row.Substring(231, 1).Trim());
        result.Add("COD", row.Substring(232, 3).Trim());
        result.Add("DOD_MO", DOD_MO_Rule(row.Substring(236, 2).Trim()));
        result.Add("DOD_DY", DOD_DY_Rule(row.Substring(238, 2).Trim()));
        result.Add("TOD", TOD_Rule(row.Substring(240, 4).Trim()));
        result.Add("DEDUC", DEDUC_Rule(row.Substring(244, 1).Trim()));

        result.Add("DETHNIC1", row.Substring(246, 1).Trim());
        result.Add("DETHNIC2", row.Substring(247, 1).Trim());
        result.Add("DETHNIC3", row.Substring(248, 1).Trim());
        result.Add("DETHNIC4", row.Substring(249, 1).Trim());
        result.Add("DETHNIC5", row.Substring(250, 20).Trim());

        result.Add("RACE1", row.Substring(270, 1).Trim());
        result.Add("RACE2", row.Substring(271, 1).Trim());
        result.Add("RACE3", row.Substring(272, 1).Trim());
        result.Add("RACE4", row.Substring(273, 1).Trim());
        result.Add("RACE5", row.Substring(274, 1).Trim());
        result.Add("RACE6", row.Substring(275, 1).Trim());
        result.Add("RACE7", row.Substring(276, 1).Trim());
        result.Add("RACE8", row.Substring(277, 1).Trim());
        result.Add("RACE9", row.Substring(278, 1).Trim());
        result.Add("RACE10", row.Substring(279, 1).Trim());
        result.Add("RACE11", row.Substring(280, 1).Trim());
        result.Add("RACE12", row.Substring(281, 1).Trim());
        result.Add("RACE13", row.Substring(282, 1).Trim());
        result.Add("RACE14", row.Substring(283, 1).Trim());
        result.Add("RACE15", row.Substring(284, 1).Trim());
        result.Add("RACE16", row.Substring(285, 30).Trim());
        result.Add("RACE17", row.Substring(315, 30).Trim());
        result.Add("RACE18", row.Substring(345, 30).Trim());
        result.Add("RACE19", row.Substring(375, 30).Trim());
        result.Add("RACE20", row.Substring(405, 30).Trim());
        result.Add("RACE21", row.Substring(435, 30).Trim());
        result.Add("RACE22", row.Substring(465, 30).Trim());
        result.Add("RACE23", row.Substring(495, 30).Trim());

        result.Add("OCCUP", row.Substring(574, 40).Trim());
        result.Add("INDUST", row.Substring(617, 40).Trim());
        result.Add("MANNER", MANNER_Rule(row.Substring(700, 1).Trim()));
        result.Add("MAN_UC", row.Substring(704, 5).Trim());
        result.Add("ACME_UC", row.Substring(709, 5).Trim());
        result.Add("EAC", row.Substring(714, 160).Trim());
        result.Add("TRX_FLG", row.Substring(874, 1).Trim());
        result.Add("RAC", row.Substring(875, 100).Trim());
        result.Add("AUTOP", AUTOP_Rule(row.Substring(975, 1).Trim()));
        result.Add("AUTOPF", AUTOPF_Rule(row.Substring(976, 1).Trim()));
        result.Add("TOBAC", TOBAC_Rule(row.Substring(977, 1).Trim()));
        result.Add("PREG", PREG_Rule(row.Substring(978, 1).Trim()));
        result.Add("DOI_MO", DOI_MO_Rule(row.Substring(980, 2).Trim()));
        result.Add("DOI_DY", DOI_DY_Rule(row.Substring(982, 2).Trim()));
        result.Add("DOI_YR", DOI_YR_Rule(row.Substring(984, 4).Trim()));
        result.Add("TOI_HR", TOI_HR_Rule(row.Substring(988, 4).Trim()));
        result.Add("WORKINJ", WORKINJ_Rule(row.Substring(992, 1).Trim()));
        result.Add("BLANK", row.Substring(1024, 56).Trim());
        result.Add("ARMEDF", ARMEDF_Rule(row.Substring(1080, 1).Trim()));
        result.Add("DINSTI", row.Substring(1081, 30).Trim());
        result.Add("STNUM_D", row.Substring(1161, 10).Trim());
        result.Add("PREDIR_D", row.Substring(1171, 10).Trim());
        result.Add("STNAME_D", row.Substring(1181, 50).Trim());
        result.Add("STDESIG_D", row.Substring(1231, 10).Trim());
        result.Add("POSTDIR_D", row.Substring(1241, 10).Trim());
        result.Add("CITYTEXT_D", row.Substring(1251, 28).Trim());
        result.Add("STATETEXT_D", row.Substring(1279, 28).Trim());
        result.Add("ZIP9_D", ZIP9_D_Rule(row.Substring(1307, 9).Trim()));
        result.Add("COUNTYTEXT_D", row.Substring(1316, 28).Trim());
        result.Add("CITYCODE_D", row.Substring(1344, 5).Trim());
        result.Add("STNUM_R", row.Substring(1484, 10).Trim());
        result.Add("PREDIR_R", row.Substring(1494, 10).Trim());
        result.Add("STNAME_R", row.Substring(1504, 28).Trim());
        result.Add("STDESIG_R", row.Substring(1532, 10).Trim());
        result.Add("POSTDIR_R", row.Substring(1542, 10).Trim());
        result.Add("UNITNUM_R", row.Substring(1552, 7).Trim());
        result.Add("CITYTEXT_R", row.Substring(1559, 28).Trim());
        result.Add("ZIP9_R", row.Substring(1587, 9).Trim());
        result.Add("COUNTYTEXT_R", row.Substring(1596, 28).Trim());
        result.Add("COUNTRYTEXT_R", row.Substring(1652, 28).Trim());
        result.Add("DMIDDLE", row.Substring(1807, 50).Trim());
        result.Add("POILITRL", row.Substring(2108, 50).Trim());
        result.Add("TRANSPRT", row.Substring(2408, 30).Trim());
        result.Add("COUNTYTEXT_I", row.Substring(2438, 28).Trim());
        result.Add("CITYTEXT_I", row.Substring(2469, 28).Trim());
        result.Add("COD1A", row.Substring(2541, 120).Trim());
        result.Add("INTERVAL1A", row.Substring(2661, 20).Trim());
        result.Add("COD1B", row.Substring(2681, 120).Trim());
        result.Add("INTERVAL1B", row.Substring(2801, 20).Trim());
        result.Add("COD1C", row.Substring(2821, 120).Trim());
        result.Add("INTERVAL1C", row.Substring(2941, 20).Trim());
        result.Add("COD1D", row.Substring(2961, 120).Trim());
        result.Add("INTERVAL1D", row.Substring(3081, 20).Trim());
        result.Add("OTHERCONDITION", row.Substring(3101, 240).Trim());
        result.Add("DBPLACECITY", row.Substring(3396, 28).Trim());
        result.Add("STINJURY", row.Substring(4269, 28).Trim());
        result.Add("VRO_STATUS", VRO_STATUS_Rule(row.Substring(4992, 1).Trim()));
        result.Add("BC_DET_MATCH", row.Substring(4993, 1).Trim());
        result.Add("FDC_DET_MATCH", row.Substring(4994, 1).Trim());
        result.Add("BC_PROB_MATCH", row.Substring(4995, 1).Trim());
        result.Add("FDC_PROB_MATCH", row.Substring(4996, 1).Trim());
        result.Add("ICD10_MATCH", row.Substring(4997, 1).Trim());
        result.Add("PREGCB_MATCH", row.Substring(4998, 1).Trim());
        result.Add("LITERALCOD_MATCH", row.Substring(4999, 1).Trim());

        result.Add("HR_CDC_OTHER", row.Substring(4991, 1).Trim());


        return result;

        /*
        2 home_record/state of death - DState
3 home_record/date_of_death - DOD_YR, DOD_MO, DOD_DY
4 death_certificate/date_of_birth - DOB_YR, DOB_MO, DOD_DY
5 home_record/last_name - LNAME  
6 home_record/first_name - GNAME*/
    }

    private List<Dictionary<string, string>> nat_get_header(List<string> rows)
    {
        var listResults = new List<Dictionary<string, string>>();

        foreach (var row in rows)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);



            result.Add("IDOB_YR", row.Substring(0, 4).Trim());

            result.Add("BSTATE", row.Substring(4, 2).Trim());

            result.Add("FILENO", row.Substring(6, 6).Trim());
            result.Add("AUXNO", row.Substring(13, 12).Trim());
            result.Add("TB", TB_NAT_Rule(row.Substring(25, 4).Trim()));


            result.Add("IDOB_MO", row.Substring(30, 2).Trim());
            result.Add("IDOB_DY", row.Substring(32, 2).Trim());

            result.Add("BPLACE", (row.Substring(37, 1).Trim()));

            result.Add("FNPI", row.Substring(38, 12).Trim());
            result.Add("MDOB_YR", MDOB_YR_Rule(row.Substring(54, 4).Trim()));
            result.Add("MDOB_MO", MDOB_MO_Rule(row.Substring(58, 2).Trim()));
            result.Add("MDOB_DY", MDOB_DY_Rule(row.Substring(60, 2).Trim()));

            result.Add("BPLACEC_ST_TER", BPLACEC_ST_TER_NAT_Rule(row.Substring(63, 2).Trim()));
            result.Add("BPLACEC_CNT", (row.Substring(65, 2).Trim()));

            result.Add("STATEC", NAT_STATEC_Rule(row.Substring(75, 2).Trim()));
            result.Add("FDOB_YR", FDOB_YR_Rule(row.Substring(80, 4).Trim()));
            result.Add("FDOB_MO", FDOB_MO_Rule(row.Substring(84, 2).Trim()));
            result.Add("MARN", MARN_Rule(row.Substring(90, 1).Trim()));
            result.Add("ACKN", ACKN_Rule(row.Substring(91, 1).Trim()));
            result.Add("MEDUC", MEDUC_Rule(row.Substring(92, 1).Trim()));

            result.Add("METHNIC1", row.Substring(94, 1).Trim());
            result.Add("METHNIC2", row.Substring(95, 1).Trim());
            result.Add("METHNIC3", row.Substring(96, 1).Trim());
            result.Add("METHNIC4", row.Substring(97, 1).Trim());

            result.Add("METHNIC5", row.Substring(98, 20).Trim());

            result.Add("MRACE1", (row.Substring(118, 1).Trim()));
            result.Add("MRACE2", (row.Substring(119, 1).Trim()));
            result.Add("MRACE3", (row.Substring(120, 1).Trim()));
            result.Add("MRACE4", (row.Substring(121, 1).Trim()));
            result.Add("MRACE5", (row.Substring(122, 1).Trim()));
            result.Add("MRACE6", (row.Substring(123, 1).Trim()));
            result.Add("MRACE7", (row.Substring(124, 1).Trim()));
            result.Add("MRACE8", (row.Substring(125, 1).Trim()));
            result.Add("MRACE9", (row.Substring(126, 1).Trim()));
            result.Add("MRACE10", (row.Substring(127, 1).Trim()));
            result.Add("MRACE11", (row.Substring(128, 1).Trim()));
            result.Add("MRACE12", (row.Substring(129, 1).Trim()));
            result.Add("MRACE13", (row.Substring(130, 1).Trim()));
            result.Add("MRACE14", (row.Substring(131, 1).Trim()));
            result.Add("MRACE15", (row.Substring(132, 1).Trim()));
            result.Add("MRACE16", (row.Substring(133, 30).Trim()));
            result.Add("MRACE17", (row.Substring(163, 30).Trim()));
            result.Add("MRACE18", (row.Substring(193, 30).Trim()));
            result.Add("MRACE19", (row.Substring(223, 30).Trim()));
            result.Add("MRACE20", (row.Substring(253, 30).Trim()));
            result.Add("MRACE21", (row.Substring(283, 30).Trim()));
            result.Add("MRACE22", (row.Substring(313, 30).Trim()));
            result.Add("MRACE23", (row.Substring(343, 30).Trim()));

            result.Add("FEDUC", row.Substring(421, 1).Trim());

            result.Add("FETHNIC1", (row.Substring(423, 1).Trim()));
            result.Add("FETHNIC2", (row.Substring(424, 1).Trim()));
            result.Add("FETHNIC3", (row.Substring(425, 1).Trim()));
            result.Add("FETHNIC4", (row.Substring(426, 1).Trim()));
            result.Add("FETHNIC5", row.Substring(427, 20).Trim());

            result.Add("FRACE1", (row.Substring(447, 1).Trim()));
            result.Add("FRACE2", (row.Substring(448, 1).Trim()));
            result.Add("FRACE3", (row.Substring(449, 1).Trim()));
            result.Add("FRACE4", (row.Substring(450, 1).Trim()));
            result.Add("FRACE5", (row.Substring(451, 1).Trim()));
            result.Add("FRACE6", (row.Substring(452, 1).Trim()));
            result.Add("FRACE7", (row.Substring(453, 1).Trim()));
            result.Add("FRACE8", (row.Substring(454, 1).Trim()));
            result.Add("FRACE9", (row.Substring(455, 1).Trim()));
            result.Add("FRACE10", (row.Substring(456, 1).Trim()));
            result.Add("FRACE11", (row.Substring(457, 1).Trim()));
            result.Add("FRACE12", (row.Substring(458, 1).Trim()));
            result.Add("FRACE13", (row.Substring(459, 1).Trim()));
            result.Add("FRACE14", (row.Substring(460, 1).Trim()));
            result.Add("FRACE15", (row.Substring(461, 1).Trim()));
            result.Add("FRACE16", (row.Substring(462, 30).Trim()));
            result.Add("FRACE17", (row.Substring(492, 30).Trim()));
            result.Add("FRACE18", (row.Substring(522, 30).Trim()));
            result.Add("FRACE19", (row.Substring(552, 30).Trim()));
            result.Add("FRACE20", (row.Substring(582, 30).Trim()));
            result.Add("FRACE21", (row.Substring(612, 30).Trim()));
            result.Add("FRACE22", (row.Substring(642, 30).Trim()));
            result.Add("FRACE23", (row.Substring(672, 30).Trim()));

            result.Add("ATTEND", ATTEND_Rule(row.Substring(750, 1).Trim()));
            result.Add("TRAN", TRAN_Rule(row.Substring(751, 1).Trim()));

            result.Add("DOFP_MO", DOFP_MO_NAT_Rule(row.Substring(752, 2).Trim()));
            result.Add("DOFP_DY", DOFP_DY_NAT_Rule(row.Substring(754, 2).Trim()));
            result.Add("DOFP_YR", DOFP_YR_NAT_Rule(row.Substring(756, 4).Trim()));
            result.Add("DOLP_MO", DOLP_MO_NAT_Rule(row.Substring(760, 2).Trim()));
            result.Add("DOLP_DY", DOLP_DY_NAT_Rule(row.Substring(762, 2).Trim()));
            result.Add("DOLP_YR", DOLP_YR_NAT_Rule(row.Substring(764, 4).Trim()));

            result.Add("NPREV", NPREV_Rule(row.Substring(768, 2).Trim()));
            result.Add("HFT", HFT_Rule(row.Substring(771, 1).Trim()));
            result.Add("HIN", HIN_Rule(row.Substring(772, 2).Trim()));
            result.Add("PWGT", PWGT_Rule(row.Substring(775, 3).Trim()));
            result.Add("DWGT", DWGT_Rule(row.Substring(779, 3).Trim()));
            result.Add("WIC", WIC_Rule(row.Substring(783, 1).Trim()));
            result.Add("PLBL", PLBL_Rule(row.Substring(784, 2).Trim()));
            result.Add("PLBD", PLBD_Rule(row.Substring(786, 2).Trim()));
            result.Add("POPO", POPO_Rule(row.Substring(788, 2).Trim()));
            result.Add("MLLB", MLLB_Rule(row.Substring(790, 2).Trim()));
            result.Add("YLLB", YLLB_Rule(row.Substring(792, 4).Trim()));
            result.Add("MOPO", MOPO_Rule(row.Substring(796, 2).Trim()));
            result.Add("YOPO", YOPO_Rule(row.Substring(798, 4).Trim()));

            result.Add("CIGPN", (row.Substring(802, 2).Trim()));
            result.Add("CIGFN", (row.Substring(804, 2).Trim()));
            result.Add("CIGSN", (row.Substring(806, 2).Trim()));
            result.Add("CIGLN", (row.Substring(808, 2).Trim()));

            result.Add("PAY", PAY_Rule(row.Substring(810, 1).Trim()));
            result.Add("DLMP_YR", DLMP_YR_Rule(row.Substring(811, 4).Trim()));
            result.Add("DLMP_MO", DLMP_MO_Rule(row.Substring(815, 2).Trim()));
            result.Add("DLMP_DY", DLMP_DY_Rule(row.Substring(817, 2).Trim()));

            result.Add("PDIAB", PDIAB_NAT_Rule(row.Substring(819, 1).Trim()));
            result.Add("GDIAB", GDIAB_NAT_Rule(row.Substring(820, 1).Trim()));
            result.Add("PHYPE", PHYPE_NAT_Rule(row.Substring(821, 1).Trim()));
            result.Add("GHYPE", GHYPE_NAT_Rule(row.Substring(822, 1).Trim()));
            result.Add("PPB", PPB_NAT_Rule(row.Substring(823, 1).Trim()));
            result.Add("PPO", PPO_NAT_Rule(row.Substring(824, 1).Trim()));
            result.Add("INFT", INFT_NAT_Rule(row.Substring(826, 1).Trim()));
            result.Add("PCES", PCES_NAT_Rule(row.Substring(827, 1).Trim()));

            result.Add("NPCES", NPCES_Rule(row.Substring(828, 2).Trim()));

            result.Add("GON", GON_NAT_Rule(row.Substring(831, 1).Trim()));
            result.Add("SYPH", SYPH_NAT_Rule(row.Substring(832, 1).Trim()));
            result.Add("HSV", HSV_NAT_Rule(row.Substring(833, 1).Trim()));
            result.Add("CHAM", CHAM_NAT_Rule(row.Substring(834, 1).Trim()));
            result.Add("HEPB", HEPB_NAT_Rule(row.Substring(835, 1).Trim()));
            result.Add("HEPC", HEPC_NAT_Rule(row.Substring(836, 1).Trim()));
            result.Add("CERV", CERV_NAT_Rule(row.Substring(837, 1).Trim()));
            result.Add("TOC", TOC_NAT_Rule(row.Substring(838, 1).Trim()));
            result.Add("ECVS", ECVS_NAT_Rule(row.Substring(839, 1).Trim()));
            result.Add("ECVF", ECVF_NAT_Rule(row.Substring(840, 1).Trim()));
            result.Add("PROM", PROM_NAT_Rule(row.Substring(841, 1).Trim()));
            result.Add("PRIC", PRIC_NAT_Rule(row.Substring(842, 1).Trim()));
            result.Add("PROL", PROL_NAT_Rule(row.Substring(843, 1).Trim()));
            result.Add("INDL", INDL_NAT_Rule(row.Substring(844, 1).Trim()));
            result.Add("AUGL", AUGL_NAT_Rule(row.Substring(845, 1).Trim()));
            result.Add("NVPR", NVPR_NAT_Rule(row.Substring(846, 1).Trim()));
            result.Add("STER", STER_NAT_Rule(row.Substring(847, 1).Trim()));
            result.Add("ANTB", ANTB_NAT_Rule(row.Substring(848, 1).Trim()));
            result.Add("CHOR", CHOR_NAT_Rule(row.Substring(849, 1).Trim()));
            result.Add("MECS", MECS_NAT_Rule(row.Substring(850, 1).Trim()));
            result.Add("FINT", FINT_NAT_Rule(row.Substring(851, 1).Trim()));
            result.Add("ESAN", ESAN_NAT_Rule(row.Substring(852, 1).Trim()));

            result.Add("ATTF", ATTF_Rule(row.Substring(853, 1).Trim()));
            result.Add("ATTV", ATTV_Rule(row.Substring(854, 1).Trim()));
            result.Add("PRES", PRES_Rule(row.Substring(855, 1).Trim()));
            result.Add("ROUT", ROUT_Rule(row.Substring(856, 1).Trim()));

            result.Add("MTR", MTR_NAT_Rule(row.Substring(858, 1).Trim()));
            result.Add("PLAC", PLAC_NAT_Rule(row.Substring(859, 1).Trim()));
            result.Add("RUT", RUT_NAT_Rule(row.Substring(860, 1).Trim()));
            result.Add("UHYS", UHYS_NAT_Rule(row.Substring(861, 1).Trim()));
            result.Add("AINT", AINT_NAT_Rule(row.Substring(862, 1).Trim()));
            result.Add("UOPR", UOPR_NAT_Rule(row.Substring(863, 1).Trim()));
            result.Add("BWG", (row.Substring(864, 4).Trim()));

            result.Add("OWGEST", OWGEST_Rule(row.Substring(869, 2).Trim()));
            result.Add("APGAR5", APGAR5_Rule(row.Substring(872, 2).Trim()));
            result.Add("APGAR10", APGAR10_Rule(row.Substring(874, 2).Trim()));

            result.Add("PLUR", (row.Substring(876, 2).Trim()));

            result.Add("SORD", SORD_Rule(row.Substring(878, 2).Trim()));



            result.Add("ITRAN", ITRAN_Rule(row.Substring(908, 1).Trim()));
            result.Add("ILIV", ILIV_Rule(row.Substring(909, 1).Trim()));
            result.Add("BFED", BFED_Rule(row.Substring(910, 1).Trim()));

            result.Add("MAGER", (row.Substring(919, 2).Trim()));
            result.Add("FAGER", (row.Substring(921, 2).Trim()));
            result.Add("EHYPE", EHYPE_NAT_Rule(row.Substring(923, 1).Trim()));
            result.Add("INFT_DRG", INFT_DRG_NAT_Rule(row.Substring(924, 1).Trim()));
            result.Add("INFT_ART", INFT_ART_NAT_Rule(row.Substring(925, 1).Trim()));

            result.Add("BIRTH_CO", row.Substring(1157, 25).Trim());
            result.Add("BRTHCITY", row.Substring(1182, 50).Trim());
            result.Add("HOSP", row.Substring(1232, 50).Trim());
            result.Add("MOMFNAME", row.Substring(1282, 50).Trim());
            result.Add("MOMMIDDL", row.Substring(1332, 50).Trim());
            result.Add("MOMLNAME", row.Substring(1382, 50).Trim());
            result.Add("MOMMAIDN", row.Substring(1539, 50).Trim());
            result.Add("STNUM", row.Substring(1596, 10).Trim());
            result.Add("PREDIR", row.Substring(1606, 10).Trim());
            result.Add("STNAME", row.Substring(1616, 28).Trim());
            result.Add("STDESIG", row.Substring(1644, 10).Trim());
            result.Add("POSTDIR", row.Substring(1654, 10).Trim());
            result.Add("UNUM", row.Substring(1664, 7).Trim());
            result.Add("ZIPCODE", row.Substring(1721, 9).Trim());
            result.Add("COUNTYTXT", row.Substring(1730, 28).Trim());
            result.Add("CITYTEXT", row.Substring(1758, 28).Trim());
            result.Add("MOM_OC_T", row.Substring(2021, 25).Trim());
            result.Add("DAD_OC_T", row.Substring(2049, 25).Trim());
            result.Add("MOM_IN_T", row.Substring(2077, 25).Trim());
            result.Add("DAD_IN_T", row.Substring(2105, 25).Trim());

            result.Add("FBPLACD_ST_TER_C", FBPLACD_ST_TER_C_NAT_Rule(row.Substring(2133, 2).Trim()));
            result.Add("FBPLACE_CNT_C", FBPLACE_CNT_C_NAT_Rule(row.Substring(2135, 2).Trim()));

            result.Add("HOSPFROM", row.Substring(2283, 50).Trim());
            result.Add("HOSPTO", row.Substring(2333, 50).Trim());
            result.Add("ATTEND_OTH_TXT", row.Substring(2383, 20).Trim());
            result.Add("ATTEND_NPI", row.Substring(2826, 12).Trim());
            result.Add("INF_MED_REC_NUM", row.Substring(2921, 15).Trim());
            result.Add("MOM_MED_REC_NUM", row.Substring(2936, 15).Trim());



            result.Add("COD18a1", row.Substring(587-1, 1).Trim());
            result.Add("COD18a2", row.Substring(588-1, 1).Trim());
            result.Add("COD18a3", row.Substring(589-1, 1).Trim());
            result.Add("COD18a4", row.Substring(590-1, 1).Trim());
            result.Add("COD18a5", row.Substring(591-1, 1).Trim());
            result.Add("COD18a6", row.Substring(592-1, 1).Trim());
            result.Add("COD18a7", row.Substring(593-1, 1).Trim());
            result.Add("COD18a8", row.Substring(594-1, 60).Trim());
            result.Add("COD18a9", row.Substring(654-1, 60).Trim());
            result.Add("COD18a10", row.Substring(714-1, 60).Trim());
            result.Add("COD18a11", row.Substring(774-1, 60).Trim());
            result.Add("COD18a12", row.Substring(834-1, 60).Trim());
            result.Add("COD18a13", row.Substring(894-1, 60).Trim());
            result.Add("COD18a14", row.Substring(954-1, 60).Trim());
            result.Add("COD18b1", row.Substring(1014-1, 1).Trim());
            result.Add("COD18b2", row.Substring(1015-1, 1).Trim());
            result.Add("COD18b3", row.Substring(1016-1, 1).Trim());
            result.Add("COD18b4", row.Substring(1017-1, 1).Trim());
            result.Add("COD18b5", row.Substring(1018-1, 1).Trim());
            result.Add("COD18b6", row.Substring(1019-1, 1).Trim());
            result.Add("COD18b7", row.Substring(1020-1, 1).Trim());
            result.Add("COD18b8", row.Substring(1021-1, 240).Trim());
            result.Add("COD18b9", row.Substring(1261-1, 240).Trim());
            result.Add("COD18b10", row.Substring(1501-1, 240).Trim());
            result.Add("COD18b11", row.Substring(1741-1, 240).Trim());
            result.Add("COD18b12", row.Substring(1981-1, 240).Trim());
            result.Add("COD18b13", row.Substring(2221-1, 240).Trim());
            result.Add("COD18b14", row.Substring(2461-1, 240).Trim());
            result.Add("ICOD", row.Substring(2701-1, 5).Trim());
            result.Add("OCOD1", row.Substring(2706-1, 5).Trim());
            result.Add("OCOD2", row.Substring(2711-1, 5).Trim());
            result.Add("OCOD3", row.Substring(2716-1, 5).Trim());
            result.Add("OCOD4", row.Substring(2721-1, 5).Trim());
            result.Add("OCOD5", row.Substring(2726-1, 5).Trim());
            result.Add("OCOD6", row.Substring(2731-1, 5).Trim());
            result.Add("OCOD7", row.Substring(2736-1, 5).Trim());

            result.Add("AVEN1", AVEN1_NAT_Rule(row.Substring(889, 1).Trim()));
            result.Add("AVEN6", AVEN6_NAT_Rule(row.Substring(890, 1).Trim()));
            result.Add("NICU", NICU_NAT_Rule(row.Substring(891, 1).Trim()));
            result.Add("SURF", SURF_NAT_Rule(row.Substring(892, 1).Trim()));
            result.Add("ANTI", ANTI_NAT_Rule(row.Substring(893, 1).Trim()));
            result.Add("SEIZ", SEIZ_NAT_Rule(row.Substring(894, 1).Trim()));
            result.Add("BINJ", BINJ_NAT_Rule(row.Substring(895, 1).Trim()));
            result.Add("ANEN", ANEN_NAT_Rule(row.Substring(896, 1).Trim()));
            result.Add("MNSB", MNSB_NAT_Rule(row.Substring(897, 1).Trim()));
            result.Add("CCHD", CCHD_NAT_Rule(row.Substring(898, 1).Trim()));
            result.Add("CDH", CDH_NAT_Rule(row.Substring(899, 1).Trim()));
            result.Add("OMPH", OMPH_NAT_Rule(row.Substring(900, 1).Trim()));
            result.Add("GAST", GAST_NAT_Rule(row.Substring(901, 1).Trim()));
            result.Add("LIMB", LIMB_NAT_Rule(row.Substring(902, 1).Trim()));
            result.Add("CL", CL_NAT_Rule(row.Substring(903, 1).Trim()));
            result.Add("CP", CP_NAT_Rule(row.Substring(904, 1).Trim()));
            result.Add("DOWT", DOWT_NAT_Rule(row.Substring(905, 1).Trim()));
            result.Add("CDIT", CDIT_NAT_Rule(row.Substring(906, 1).Trim()));
            result.Add("HYPO", HYPO_NAT_Rule(row.Substring(907, 1).Trim()));
            result.Add("TLAB", TLAB_NAT_Rule(row.Substring(857, 1).Trim()));
            result.Add("RECORD_TYPE", (row.Substring(3999, 1).Trim()));
            result.Add("ISEX", ISEX_NAT_Rule(row.Substring(29, 1).Trim()));
            listResults.Add(result);
        }

        return listResults;
    }

    

    private List<Dictionary<string, string>> fet_get_header(List<string> rows)
    {
        var listResults = new List<Dictionary<string, string>>();

        foreach (var row in rows)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            result.Add("FDOD_YR", row.Substring(0, 4).Trim());
            result.Add("FILENO", row.Substring(6, 6).Trim());
            result.Add("AUXNO", row.Substring(13, 12).Trim());
            result.Add("TD", TD_FET_Rule(row.Substring(25, 4).Trim()));
            result.Add("FDOD_MO", row.Substring(30, 2).Trim());
            result.Add("FDOD_DY", row.Substring(32, 2).Trim());
            result.Add("FNPI", row.Substring(38, 12).Trim());
            result.Add("MDOB_YR", MDOB_YR_FET_Rule(row.Substring(54, 4).Trim()));
            result.Add("MDOB_MO", MDOB_MO_FET_Rule(row.Substring(58, 2).Trim()));
            result.Add("MDOB_DY", MDOB_DY_FET_Rule(row.Substring(60, 2).Trim()));
            result.Add("STATEC", (row.Substring(75, 2).Trim()));
            result.Add("FDOB_YR", FDOB_YR_FET_Rule(row.Substring(80, 4).Trim()));
            result.Add("FDOB_MO", FDOB_MO_FET_Rule(row.Substring(84, 2).Trim()));
            result.Add("MARN", MARN_FET_Rule(row.Substring(90, 1).Trim()));
            result.Add("MEDUC", MEDUC_FET_Rule(row.Substring(92, 1).Trim()));
            result.Add("METHNIC5", row.Substring(98, 20).Trim());
            result.Add("ATTEND", ATTEND_FET_Rule(row.Substring(421, 1).Trim()));
            result.Add("TRAN", TRAN_FET_Rule(row.Substring(422, 1).Trim()));
            result.Add("NPREV", NPREV_FET_Rule(row.Substring(439, 2).Trim()));
            result.Add("HFT", HFT_FET_Rule(row.Substring(442, 1).Trim()));
            result.Add("HIN", HIN_FET_Rule(row.Substring(443, 2).Trim()));
            result.Add("PWGT", PWGT_FET_Rule(row.Substring(446, 3).Trim()));
            result.Add("DWGT", DWGT_FET_Rule(row.Substring(450, 3).Trim()));
            result.Add("WIC", WIC_FET_Rule(row.Substring(454, 1).Trim()));
            result.Add("PLBL", PLBL_FET_Rule(row.Substring(455, 2).Trim()));
            result.Add("PLBD", PLBD_FET_Rule(row.Substring(457, 2).Trim()));
            result.Add("POPO", POPO_FET_Rule(row.Substring(459, 2).Trim()));
            result.Add("MLLB", MLLB_FET_Rule(row.Substring(461, 2).Trim()));
            result.Add("YLLB", YLLB_FET_Rule(row.Substring(463, 4).Trim()));
            result.Add("MOPO", MOPO_FET_Rule(row.Substring(467, 2).Trim()));
            result.Add("YOPO", YOPO_FET_Rule(row.Substring(469, 4).Trim()));
            result.Add("DLMP_YR", DLMP_YR_FET_Rule(row.Substring(481, 4).Trim()));
            result.Add("DLMP_MO", DLMP_MO_FET_Rule(row.Substring(485, 2).Trim()));
            result.Add("DLMP_DY", DLMP_DY_FET_Rule(row.Substring(487, 2).Trim()));
            result.Add("NPCES", NPCES_FET_Rule(row.Substring(498, 2).Trim()));
            result.Add("ATTF", ATTF_FET_Rule(row.Substring(511, 1).Trim()));
            result.Add("ATTV", ATTV_FET_Rule(row.Substring(512, 1).Trim()));
            result.Add("PRES", PRES_FET_Rule(row.Substring(513, 1).Trim()));
            result.Add("ROUT", ROUT_FET_Rule(row.Substring(514, 1).Trim()));
            result.Add("OWGEST", OWGEST_FET_Rule(row.Substring(528, 2).Trim()));
            result.Add("SORD", SORD_FET_Rule(row.Substring(537, 2).Trim()));
            result.Add("HOSP_D", row.Substring(2904, 50).Trim());
            result.Add("ADDRESS_D", row.Substring(3051, 50).Trim());
            result.Add("ZIPCODE_D", row.Substring(3101, 9).Trim());
            result.Add("CNTY_D", row.Substring(3110, 28).Trim());
            result.Add("CITY_D", row.Substring(3138, 28).Trim());
            result.Add("MOMFNAME", row.Substring(3256, 50).Trim());
            result.Add("MOMMNAME", row.Substring(3306, 50).Trim());
            result.Add("MOMLNAME", row.Substring(3356, 50).Trim());
            result.Add("MOMMAIDN", row.Substring(3516, 50).Trim());
            result.Add("STNUM", row.Substring(3576, 10).Trim());
            result.Add("PREDIR", row.Substring(3586, 10).Trim());
            result.Add("STNAME", row.Substring(3596, 50).Trim());
            result.Add("STDESIG", row.Substring(3646, 10).Trim());
            result.Add("POSTDIR", row.Substring(3656, 10).Trim());
            result.Add("APTNUMB", row.Substring(3666, 7).Trim());
            result.Add("ZIPCODE", row.Substring(3723, 9).Trim());
            result.Add("COUNTYTXT", row.Substring(3732, 28).Trim());
            result.Add("CITYTXT", row.Substring(3760, 28).Trim());
            result.Add("MOM_OC_T", row.Substring(4060, 25).Trim());
            result.Add("DAD_OC_T", row.Substring(4088, 25).Trim());
            result.Add("MOM_IN_T", row.Substring(4116, 25).Trim());
            result.Add("DAD_IN_T", row.Substring(4144, 25).Trim());
            result.Add("FEDUC", FEDUC_FET_Rule(row.Substring(4288, 1).Trim()));
            result.Add("FETHNIC5", row.Substring(4294, 20).Trim());
            result.Add("HOSPFROM", row.Substring(4763, 50).Trim());
            result.Add("ATTEND_NPI", row.Substring(4863, 12).Trim());
            result.Add("ATTEND_OTH_TXT", row.Substring(4875, 20).Trim());
            
            result.Add("COD18a1", row.Substring(587-1, 1).Trim());
            result.Add("COD18a2", row.Substring(588-1, 1).Trim());
            result.Add("COD18a3", row.Substring(589-1, 1).Trim());
            result.Add("COD18a4", row.Substring(590-1, 1).Trim());
            result.Add("COD18a5", row.Substring(591-1, 1).Trim());
            result.Add("COD18a6", row.Substring(592-1, 1).Trim());
            result.Add("COD18a7", row.Substring(593-1, 1).Trim());
            result.Add("COD18a8", row.Substring(594-1, 60).Trim());
            result.Add("COD18a9", row.Substring(654-1, 60).Trim());
            result.Add("COD18a10", row.Substring(714-1, 60).Trim());
            result.Add("COD18a11", row.Substring(774-1, 60).Trim());
            result.Add("COD18a12", row.Substring(834-1, 60).Trim());
            result.Add("COD18a13", row.Substring(894-1, 60).Trim());
            result.Add("COD18a14", row.Substring(954-1, 60).Trim());
            result.Add("COD18b1", row.Substring(1014-1, 1).Trim());
            result.Add("COD18b2", row.Substring(1015-1, 1).Trim());
            result.Add("COD18b3", row.Substring(1016-1, 1).Trim());
            result.Add("COD18b4", row.Substring(1017-1, 1).Trim());
            result.Add("COD18b5", row.Substring(1018-1, 1).Trim());
            result.Add("COD18b6", row.Substring(1019-1, 1).Trim());
            result.Add("COD18b7", row.Substring(1020-1, 1).Trim());
            result.Add("COD18b8", row.Substring(1021-1, 240).Trim());
            result.Add("COD18b9", row.Substring(1261-1, 240).Trim());
            result.Add("COD18b10", row.Substring(1501-1, 240).Trim());
            result.Add("COD18b11", row.Substring(1741-1, 240).Trim());
            result.Add("COD18b12", row.Substring(1981-1, 240).Trim());
            result.Add("COD18b13", row.Substring(2221-1, 240).Trim());
            result.Add("COD18b14", row.Substring(2461-1, 240).Trim());
            result.Add("ICOD", row.Substring(2701-1, 5).Trim());
            result.Add("OCOD1", row.Substring(2706-1, 5).Trim());
            result.Add("OCOD2", row.Substring(2711-1, 5).Trim());
            result.Add("OCOD3", row.Substring(2716-1, 5).Trim());
            result.Add("OCOD4", row.Substring(2721-1, 5).Trim());
            result.Add("OCOD5", row.Substring(2726-1, 5).Trim());
            result.Add("OCOD6", row.Substring(2731-1, 5).Trim());
            result.Add("OCOD7", row.Substring(2736-1, 5).Trim());

            result.Add("DSTATE", (row.Substring(4, 2).Trim()));
            result.Add("FSEX", FSEX_FET_Rule(row.Substring(29, 1).Trim()));
            result.Add("DPLACE", (row.Substring(37, 1).Trim()));
            result.Add("BPLACEC_ST_TER", BPLACEC_ST_TER_FET_Rule(row.Substring(63, 2).Trim()));
            result.Add("BPLACEC_CNT", BPLACEC_CNT_FET_Rule(row.Substring(65, 2).Trim()));

            result.Add("METHNIC1", (row.Substring(94, 1).Trim()));
            result.Add("METHNIC2", (row.Substring(95, 1).Trim()));
            result.Add("METHNIC3", (row.Substring(96, 1).Trim()));
            result.Add("METHNIC4", (row.Substring(97, 1).Trim()));

            result.Add("MRACE1", (row.Substring(118, 1).Trim()));
            result.Add("MRACE2", (row.Substring(119, 1).Trim()));
            result.Add("MRACE3", (row.Substring(120, 1).Trim()));
            result.Add("MRACE4", (row.Substring(121, 1).Trim()));
            result.Add("MRACE5", (row.Substring(122, 1).Trim()));
            result.Add("MRACE6", (row.Substring(123, 1).Trim()));
            result.Add("MRACE7", (row.Substring(124, 1).Trim()));
            result.Add("MRACE8", (row.Substring(125, 1).Trim()));
            result.Add("MRACE9", (row.Substring(126, 1).Trim()));
            result.Add("MRACE10", (row.Substring(127, 1).Trim()));
            result.Add("MRACE11", (row.Substring(128, 1).Trim()));
            result.Add("MRACE12", (row.Substring(129, 1).Trim()));
            result.Add("MRACE13", (row.Substring(130, 1).Trim()));
            result.Add("MRACE14", (row.Substring(131, 1).Trim()));
            result.Add("MRACE15", (row.Substring(132, 1).Trim()));
            result.Add("MRACE16", (row.Substring(133, 30).Trim()));
            result.Add("MRACE17", (row.Substring(163, 30).Trim()));
            result.Add("MRACE18", (row.Substring(193, 30).Trim()));
            result.Add("MRACE19", (row.Substring(223, 30).Trim()));
            result.Add("MRACE20", (row.Substring(253, 30).Trim()));
            result.Add("MRACE21", (row.Substring(283, 30).Trim()));
            result.Add("MRACE22", (row.Substring(313, 30).Trim()));
            result.Add("MRACE23", (row.Substring(343, 30).Trim()));

            result.Add("DOFP_MO", DOFP_MO_FET_Rule(row.Substring(423, 2).Trim()));
            result.Add("DOFP_DY", DOFP_DY_FET_Rule(row.Substring(425, 2).Trim()));
            result.Add("DOFP_YR", DOFP_YR_FET_Rule(row.Substring(427, 4).Trim()));
            result.Add("DOLP_MO", DOLP_MO_FET_Rule(row.Substring(431, 2).Trim()));
            result.Add("DOLP_DY", DOLP_DY_FET_Rule(row.Substring(433, 2).Trim()));
            result.Add("DOLP_YR", DOLP_YR_FET_Rule(row.Substring(435, 4).Trim()));

            result.Add("CIGPN", (row.Substring(473, 2).Trim()));
            result.Add("CIGFN", (row.Substring(475, 2).Trim()));
            result.Add("CIGSN", (row.Substring(477, 2).Trim()));
            result.Add("CIGLN", (row.Substring(479, 2).Trim()));
            result.Add("PDIAB", PDIAB_FET_Rule(row.Substring(489, 1).Trim()));
            result.Add("GDIAB", GDIAB_FET_Rule(row.Substring(490, 1).Trim()));
            result.Add("PHYPE", PHYPE_FET_Rule(row.Substring(491, 1).Trim()));
            result.Add("GHYPE", GHYPE_FET_Rule(row.Substring(492, 1).Trim()));
            result.Add("PPB", PPB_FET_Rule(row.Substring(493, 1).Trim()));
            result.Add("PPO", PPO_FET_Rule(row.Substring(494, 1).Trim()));
            result.Add("INFT", INFT_FET_Rule(row.Substring(496, 1).Trim()));
            result.Add("PCES", PCES_FET_Rule(row.Substring(497, 1).Trim()));
            result.Add("GON", GON_FET_Rule(row.Substring(501, 1).Trim()));
            result.Add("SYPH", SYPH_FET_Rule(row.Substring(502, 1).Trim()));
            result.Add("HSV", HSV_FET_Rule(row.Substring(503, 1).Trim()));
            result.Add("CHAM", CHAM_FET_Rule(row.Substring(504, 1).Trim()));
            result.Add("LM", LM_FET_Rule(row.Substring(505, 1).Trim()));
            result.Add("GBS", GBS_FET_Rule(row.Substring(506, 1).Trim()));
            result.Add("CMV", CMV_FET_Rule(row.Substring(507, 1).Trim()));
            result.Add("B19", B19_FET_Rule(row.Substring(508, 1).Trim()));
            result.Add("TOXO", TOXO_FET_Rule(row.Substring(509, 1).Trim()));
            result.Add("OTHERI", OTHERI_FET_Rule(row.Substring(510, 1).Trim()));
            result.Add("TLAB", TLAB_FET_Rule(row.Substring(515, 1).Trim()));
            result.Add("MTR", MTR_FET_Rule(row.Substring(517, 1).Trim()));
            result.Add("PLAC", PLAC_FET_Rule(row.Substring(518, 1).Trim()));
            result.Add("RUT", RUT_FET_Rule(row.Substring(519, 1).Trim()));
            result.Add("UHYS", UHYS_FET_Rule(row.Substring(520, 1).Trim()));
            result.Add("AINT", AINT_FET_Rule(row.Substring(521, 1).Trim()));
            result.Add("UOPR", UOPR_FET_Rule(row.Substring(522, 1).Trim()));
            result.Add("FWG", (row.Substring(523, 4).Trim()));
            result.Add("PLUR", (row.Substring(535, 2).Trim()));
            result.Add("ANEN", ANEN_FET_Rule(row.Substring(548, 1).Trim()));
            result.Add("MNSB", MNSB_FET_Rule(row.Substring(549, 1).Trim()));
            result.Add("CCHD", CCHD_FET_Rule(row.Substring(550, 1).Trim()));
            result.Add("CDH", CDH_FET_Rule(row.Substring(551, 1).Trim()));
            result.Add("OMPH", OMPH_FET_Rule(row.Substring(552, 1).Trim()));
            result.Add("GAST", GAST_FET_Rule(row.Substring(553, 1).Trim()));
            result.Add("LIMB", LIMB_FET_Rule(row.Substring(554, 1).Trim()));
            result.Add("CL", CL_FET_Rule(row.Substring(555, 1).Trim()));
            result.Add("CP", CP_FET_Rule(row.Substring(556, 1).Trim()));
            result.Add("DOWT", DOWT_FET_Rule(row.Substring(557, 1).Trim()));
            result.Add("CDIT", CDIT_FET_Rule(row.Substring(558, 1).Trim()));
            result.Add("HYPO", HYPO_FET_Rule(row.Substring(559, 1).Trim()));
            result.Add("MAGER", (row.Substring(568, 2).Trim()));
            result.Add("FAGER", (row.Substring(570, 2).Trim()));
            result.Add("EHYPE", EHYPE_FET_Rule(row.Substring(572, 1).Trim()));
            result.Add("INFT_DRG", INFT_DRG_FET_Rule(row.Substring(573, 1).Trim()));
            result.Add("INFT_ART", INFT_ART_FET_Rule(row.Substring(574, 1).Trim()));
            result.Add("HSV1", HSV1_FET_Rule(row.Substring(2740, 1).Trim()));
            result.Add("HIV", HIV_FET_Rule(row.Substring(2741, 1).Trim()));
            result.Add("FBPLACD_ST_TER_C", FBPLACD_ST_TER_C_FET_Rule(row.Substring(4172, 2).Trim()));
            result.Add("FBPLACE_CNT_C", FBPLACE_CNT_C_FET_Rule(row.Substring(4174, 2).Trim()));

            result.Add("FETHNIC1", (row.Substring(4290, 1).Trim()));
            result.Add("FETHNIC2", (row.Substring(4291, 1).Trim()));
            result.Add("FETHNIC3", (row.Substring(4292, 1).Trim()));
            result.Add("FETHNIC4", (row.Substring(4293, 1).Trim()));

            result.Add("FRACE1", (row.Substring(4314, 1).Trim()));
            result.Add("FRACE2", (row.Substring(4315, 1).Trim()));
            result.Add("FRACE3", (row.Substring(4316, 1).Trim()));
            result.Add("FRACE4", (row.Substring(4317, 1).Trim()));
            result.Add("FRACE5", (row.Substring(4318, 1).Trim()));
            result.Add("FRACE6", (row.Substring(4319, 1).Trim()));
            result.Add("FRACE7", (row.Substring(4320, 1).Trim()));
            result.Add("FRACE8", (row.Substring(4321, 1).Trim()));
            result.Add("FRACE9", (row.Substring(4322, 1).Trim()));
            result.Add("FRACE10",(row.Substring(4323, 1).Trim()));
            result.Add("FRACE11",(row.Substring(4324, 1).Trim()));
            result.Add("FRACE12",(row.Substring(4325, 1).Trim()));
            result.Add("FRACE13",(row.Substring(4326, 1).Trim()));
            result.Add("FRACE14",(row.Substring(4327, 1).Trim()));
            result.Add("FRACE15",(row.Substring(4328, 1).Trim()));
            result.Add("FRACE16",(row.Substring(4329, 30).Trim()));
            result.Add("FRACE17",(row.Substring(4359, 30).Trim()));
            result.Add("FRACE18",(row.Substring(4389, 30).Trim()));
            result.Add("FRACE19",(row.Substring(4419, 30).Trim()));
            result.Add("FRACE20",(row.Substring(4449, 30).Trim()));
            result.Add("FRACE21",(row.Substring(4479, 30).Trim()));
            result.Add("FRACE22",(row.Substring(4509, 30).Trim()));
            result.Add("FRACE23",(row.Substring(4539, 30).Trim()));

            result.Add("RECORD_TYPE", (row.Substring(5999, 1).Trim()));



            listResults.Add(result);
        }

        return listResults;
    }

    private string TB_NAT_Rule(string value)
    {
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);
            }
        }

        return parsedValue;
    }

    private string TD_FET_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field, format as MMRIA time.; if TOD = 9999 then this field should be left blank
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);

            }
        }

        return parsedValue;
    }

    private static string ConvertHHmm_To_MMRIATime(string value)
    {
        string result = value;
        try
        {
            /*
                42 => 00:42:00
                1945 => 19:45:00
                1530 => 15:30:00
                815 => 08:15:00


                42 => 00:42
                1945 => 19:45
                1530 => 15:30
                815 => 08:15
            */
            //Ensure three digit times parse with 4 digits, e.g. 744 becomes 0744 and will be parsed to 7:44 AM
            switch (value.Length)
            {
                case 0:
                    break;
                case 1:
                    result = $"00:0{value}:00";
                    break;
                case 2:
                    result = $"00:{value}:00";
                    break;
                case 3:
                    result = $"0{value[0]}:{value[1..^0]}:00";
                    break;
                case 4:
                    result = $"{value[0..2]}:{value[2..^0]}:00";
                    break;


                default:
                    //result = $"{value.Substring(0,2)}:{value.Substring(2,2)}";
                    System.Console.Write($"ConvertHHmm_To_MMRIATime unable to convert {value}");
                    break;
            }
        }
        catch (Exception ex)
        {
            //Error parsing, eat it and put exact text in as to not lose data on import
            //result = value;
        }

        return result;
    }
    

    private string STATEC_FET_Rule(string value)
    {
        //"Map XX --> 9999 (blank)
        //Map ZZ --> 9999(blank)
        //Map all other values to MMRIA field state listing"

        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    #region Rules Section

    //CALCULATE MOTHERS AGE AT DELIVERY ON BC
    /*
    path=birth_fetal_death_certificate_parent/demographic_of_mother/age
    event=onfocus
    */
    private string age_delivery(string dob_YR, string dob_MO, string dob_day, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        string years = "";
        int.TryParse(dob_YR, out int start_year);
        int.TryParse(dob_MO, out int start_month);
        int.TryParse(dob_day, out int start_day);
        int.TryParse(dodeliv_YR, out int end_year);
        int.TryParse(dodeliv_MO, out int end_month);
        int.TryParse(dodeliv_day, out int end_day);
        //int.TryParse(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year, out int end_year);
        //int.TryParse(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month, out int end_month);
        //int.TryParse(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day, out int end_day);

        if
        (
            DateTime.TryParse($"{start_year}/{start_month}/{start_day}", out DateTime birthDateCheck) == true &&
            DateTime.TryParse($"{end_year}/{end_month}/{end_day}", out DateTime endDateCheck) == true
        )
        {
            var start_date = new DateTime(start_year, start_month, start_day).AddMonths(-1);
            var end_date = new DateTime(end_year, end_month, end_day).AddMonths(-1);
            years = calc_years(start_date, end_date);
        }

        return years;
    }
    //CALCULATE FATHERS AGE AT DELIVERY ON BC
    /*
    path=birth_fetal_death_certificate_parent/demographic_of_father/age
    event=onfocus
    */
//    function fathers_age_delivery(p_control)
//    {
//        var years = null;
//        var start_year = parseInt(this.date_of_birth.year);
//        var start_month = parseInt(this.date_of_birth.month);
//        var start_day = 1;
//        var end_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
//        var end_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
//        var end_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
//        if
//        (
//            $global.isValidDate(start_year, start_month, start_day) == true &&
//            $global.isValidDate(end_year, end_month, end_day) == true
//        )
//{
//            var start_date = new Date(start_year, start_month - 1, start_day);
//            var end_date = new Date(end_year, end_month - 1, end_day);
//            var years = $global.calc_years(start_date, end_date);
//            this.age = years;
//            p_control.value = this.age;
//        }
//    }

    private string calc_years(DateTime p_start_date, DateTime p_end_date)
    {
        var years = "";

        var age = p_end_date.Year - p_start_date.Year;
        if (p_end_date.DayOfYear < p_start_date.DayOfYear)
            age = age - 1;

        years = age.ToString();

        return years;
    }

    #region MOR Rules

    private string DPLACE_Rule(string value)
    {
        /*"1 --> dcdi_doi_hospi = 0 and dcdi_doo_hospi = 9999 (blank)
            2 --> dcdi_doi_hospi = 1 and dcdi_doo_hospi = 9999 (blank)
            3 --> dcdi_doi_hospi = 2 and dcdi_doo_hospi = 9999 (blank)
            4 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 2
            5 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 0
            6 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 1 
            7 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 3
            9 --> dcdi_doi_hosp = 7777 (unknown) and dcdi_doo_hospi = 7777 (unknown) "
                */
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "9999";
                break;
            case "5":
                value = "9999";
                break;
            case "6":
                value = "9999";
                break;
            case "7":
                value = "9999";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string DPLACE_Outside_of_hospital_Rule(string value)
    {
        /*"1 --> dcdi_doi_hospi = 0 and dcdi_doo_hospi = 9999 (blank)
            2 --> dcdi_doi_hospi = 1 and dcdi_doo_hospi = 9999 (blank)
            3 --> dcdi_doi_hospi = 2 and dcdi_doo_hospi = 9999 (blank)
            4 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 2
            5 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 0
            6 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 1 
            7 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 3
            9 --> dcdi_doi_hosp = 7777 (unknown) and dcdi_doo_hospi = 7777 (unknown) "
                */
        switch (value?.ToUpper())
        {
            case "1":
                value = "9999";
                break;
            case "2":
                value = "9999";
                break;
            case "3":
                value = "9999";
                break;
            case "4":
                value = "2";
                break;
            case "5":
                value = "0";
                break;
            case "6":
                value = "1";
                break;
            case "7":
                value = "3";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string STINJURY_Rule(string value)
    {
        var result = value;

        if (StateDisplayToValue.ContainsKey(value))
        {
            result = StateDisplayToValue[value];
        }

        return result;
    }

    private string STATETEXT_D_Rule(string value)
    {
        var result = value;

        if (StateDisplayToValue.ContainsKey(value))
        {
            result = StateDisplayToValue[value];
        }

        return result;
    }

    private string PLACE_OF_LAST_RESIDENCE_street_Rule(string stnum_r, string predir_r, string stname_r, string stdesig_r, string postdir_r)
    {
        //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
        string determinedValue = $"{stnum_r} {predir_r} {stname_r} {stdesig_r} {postdir_r}";

        return determinedValue;
    }

    private string ADDRESS_OF_DEATH_street_Rule(string stnum_d, string predir_d, string stname_d, string stdesig_d, string postdir_d)
    {
        //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
        string determinedValue = $"{stnum_d} {predir_d} {stname_d} {stdesig_d} {postdir_d}";

        return determinedValue;
    }

    private string RACE_other_race_Rule(string race22, string race23)
    {
        //"Combine RACE22 and RACE23 into one field (dcr_o_race), separated by pipe delimiter. 
        //1.Transfer string verbatim from RACE22 to MMRIA field.
        //2.Transfer string verbatim from RACE23 and add to same MMRIA field.
        //3.If both RACE22 and RACE23 are empty, leave MMRIA field empty(blank)."
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race22) && !string.IsNullOrWhiteSpace(race23))
            determinedValue = $"{race22}|{race23}";
        else if (!string.IsNullOrWhiteSpace(race22))
            determinedValue = race22;
        else if (!string.IsNullOrWhiteSpace(race23))
            determinedValue = race23;

        return determinedValue;
    }

    private string RACE_other_pacific_islander_Rule(string race20, string race21)
    {
        //"Combine RACE20 and RACE21 into one field (dcr_op_islan), separated by pipe delimiter. 
        //1.Transfer string verbatim from RACE20 to MMRIA field.
        //2.Transfer string verbatim from RACE21 and add to same MMRIA field.
        //3.If both RACE20 and RACE21 are empty, leave MMRIA field empty(blank)."
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race20) && !string.IsNullOrWhiteSpace(race21))
            determinedValue = $"{race20}|{race21}";
        else if (!string.IsNullOrWhiteSpace(race20))
            determinedValue = race20;
        else if (!string.IsNullOrWhiteSpace(race21))
            determinedValue = race21;

        return determinedValue;
    }

    private string RACE_other_asian_Rule(string race18, string race19)
    {
        //"Combine RACE18 and RACE19 into one field (dcr_o_asian), separated by pipe delimiter.
        //1.Transfer string verbatim from RACE18 to MMRIA field.
        //2.Transfer string verbatim from RACE19 and add to same MMRIA field.
        //3.If both RACE18 and RACE19 are empty, leave MMRIA field empty(blank)."
        //Defaulting to blank
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race18) && !string.IsNullOrWhiteSpace(race19))
            determinedValue = $"{race18}|{race19}";
        else if (!string.IsNullOrWhiteSpace(race18))
            determinedValue = race18;
        else if (!string.IsNullOrWhiteSpace(race19))
            determinedValue = race19;

        return determinedValue;

    }

    private string RACE_Principal_Tribe_Rule(string race16, string race17)
    {
        //"Combine RACE16 and RACE17 into one field (dcr_p_tribe), separated by pipe delimiter. 
        //1.Transfer string verbatim from RACE16 to MMRIA field.
        //2.Transfer string verbatim from RACE17 and add to same MMRIA field.
        //3.If both RACE16 and RACE17 are empty, leave MMRIA field empty(blank)."
        //Defaulting to blank
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race16) && !string.IsNullOrWhiteSpace(race17))
            determinedValue = $"{race16}|{race17}";
        else if (!string.IsNullOrWhiteSpace(race16))
            determinedValue = race16;
        else if (!string.IsNullOrWhiteSpace(race17))
            determinedValue = race17;

        return determinedValue;
    }

    private string[] RACE_Rule(string race1, string race2, string race3,
        string race4, string race5, string race6,
        string race7, string race8, string race9,
        string race10, string race11, string race12,
        string race13, string race14, string race15)
    {
        //"Use values from RACE1 through RACE15 to populate MMRIA multi-select field (dcr_race).
        //If every one of RACE1 through RACE15 is equal to ""N"", then dcr_race = 8888(Race Not Specified)"
        //"Use values from RACE1 through RACE15 to populate MMRIA multi-select field (dcr_race).
        //RACE1 = Y-- > dcr_race = 0
        //RACE2 = Y-- > dcr_race = 1
        //RACE3 = Y-- > dcr_race = 2
        //RACE4 = Y-- > dcr_race = 7
        //RACE5 = Y-- > dcr_race = 8
        //RACE6 = Y-- > dcr_race = 9
        //RACE7 = Y-- > dcr_race = 10
        //RACE8 = Y-- > dcr_race = 11
        //RACE9 = Y-- > dcr_race = 12
        //RACE10 = Y-- > dcr_race = 13
        //RACE11 = Y-- > dcr_race = 3
        //RACE12 = Y-- > dcr_race = 4
        //RACE13 = Y-- > dcr_race = 5
        //RACE14 = Y-- > dcr_race = 6
        //RACE15 = Y-- > dcr_race = 14

        //Defaulting to blank
        List<string> determinedValues = new List<string>();

        if (race1 == "N" && race2 == "N" && race3 == "N" && race4 == "N"
            && race5 == "N" && race6 == "N" && race7 == "N" && race8 == "N"
            && race9 == "N" && race10 == "N" && race11 == "N" && race12 == "N"
            && race13 == "N" && race14 == "N" && race15 == "N")
            determinedValues.Add("8888");
        else
        {
            if (race1 == "Y")
                determinedValues.Add("0");

            if (race2 == "Y")
                determinedValues.Add("1");

            if (race3 == "Y")
                determinedValues.Add("2");

            if (race4 == "Y")
                determinedValues.Add("7");

            if (race5 == "Y")
                determinedValues.Add("8");

            if (race6 == "Y")
                determinedValues.Add("9");

            if (race7 == "Y")
                determinedValues.Add("10");

            if (race8 == "Y")
                determinedValues.Add("11");

            if (race9 == "Y")
                determinedValues.Add("12");

            if (race10 == "Y")
                determinedValues.Add("13");

            if (race11 == "Y")
                determinedValues.Add("3");

            if (race12 == "Y")
                determinedValues.Add("4");

            if (race13 == "Y")
                determinedValues.Add("5");

            if (race14 == "Y")
                determinedValues.Add("6");

            if (race15 == "Y")
                determinedValues.Add("14");
        }

        return determinedValues.ToArray();
    }

    private string DETHNIC_Rule(string value1, string value2, string value3, string value4)
    {
        //"Use values of DETHNIC1, DETHNIC2, DETHNIC3, DETHNIC4 to fill out MMRIA field dcd_ioh_origi.
        //If DETHNIC1 = N and DETHNIC2 = N and DETHNIC3 = N and DETHNIC 4 = N-- > dcd_ioh_origi = 0 No, Not Spanish/ Hispanic / Latino
        //If DETHNIC1 = U and DETHNIC2 = U and DETHNIC3 = U and DETHNIC4 = U-- > dcd_ioh_origi = 7777 Unknown
        //If DETHNIC1 = (empty)and DETHNIC2 = (empty)and DETHNIC3 = (empty)and DETHNIC4 = (empty)-- > dcd_ioh_origi = 9999(blank)"
        //H-- > dcd_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        //H-- > dcd_ioh_origi = 2 Yes, Puerto Rican
        //H-- > dcd_ioh_origi = 3 Yes, Cuban
        //H-- > dcd_ioh_origi = 4 Yes, Other Spanish/ Hispanic / Latino

        //Defaulting to blank
        string determinedValue = "9999";

        if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N")
            determinedValue = "0";
        else if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U")
            determinedValue = "7777";
        else if (value1 == "H")
            determinedValue = "1";
        else if (value2 == "H")
            determinedValue = "2";
        else if (value3 == "H")
            determinedValue = "3";
        else if (value4 == "H")
            determinedValue = "4";

        return determinedValue;
    }

    private string MANNER_Rule(string value)
    {
        //"Map character to MMRIA code values as follows:
        //Blank fields -> 9999(blank)
        //N-> 0 Natural
        //A-> 2 Accident
        //S-> 3 Suicide
        //H-> 1 Homicide
        //P-> 5 Pending Investigation
        //C-> 6 Could Not Be Determined

        //Map empty rows-- > 9999(blank)"

        switch (value?.ToUpper())
        {
            case "N":
                value = "0";
                break;
            case "A":
                value = "2";
                break;
            case "S":
                value = "3";
                break;
            case "H":
                value = "1";
                break;
            case "P":
                value = "5";
                break;
            case "C":
                value = "6";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string AUTOP_Rule(string value)
    {
        //"Map character to MMRIA code values as follows:
        //Blank fields -> 9999(blank)
        //Y-> 1 = Yes
        //N-> 0 = No
        //U->  7777 = Unknown
        //"

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string AUTOPF_Rule(string value)
    {
        //"Map character to MMRIA code values as follows:
        //Blank fields -> 9999(blank)
        //Y-> 1 = Yes
        //N-> 0 = No
        //U->  7777 = Unknown
        //"

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string TOBAC_Rule(string value)
    {
        //"Map character to MMRIA code values as follows: 
        //Blank fields -> 9999(blank)
        //Y-> 1 = Yes
        //N-> 0 = No
        //P-> 2 = Probably
        //U-> 7777 = Unknown
        //C-> 7777 = Unknown"

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "P":
                value = "2";
                break;
            case "U":
            case "C":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string PREG_Rule(string value)
    {
        //"Map number to MMRIA number codes as follows:
        //Empty columns -> 9999 = (blank)
        //1-- > 0 Not pregnant within last year
        //2-- > 1 Pregnant at the time of death
        //3-- > 2 Pregnant within 42 days of death
        //4-- > 3 Pregnant within 43 to 365 days of death
        //8-- > 5 Not Applicable
        //9-- > 88 Unknown if pregnant in last year "

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "8":
                value = "5";
                break;
            case "9":
                value = "88";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string DOI_MO_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; Map 99 and blank -> 9999(blank)
        if (value == "99" || string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    private string DOI_DY_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; Map 99 and blank -> 9999(blank)
        if (value == "99" || string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    private string DOI_YR_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; Map 9999 and blank ->9999(blank)
        if (string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    private string TOI_HR_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; Values of 9999 and blank should be mapped as blank; need to map these values to MMRIA time format
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);
            }
        }

        return parsedValue;
    }

    private string WORKINJ_Rule(string value)
    {
        //"Map character to MMRIA code values as follows:
        //Blank fields -> 9999(blank)
        //Y-> 1 = Yes
        //N-> 0 = No
        //U->  7777 = Unknown
        //"

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ARMEDF_Rule(string value)
    {
        //"Map character to MMRIA code values as follows:
        //Blank fields -> 9999(blank)
        //Y-> 1 = Yes
        //N-> 0 = No
        //U->  7777 = Unknown
        //"

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ZIP9_D_Rule(string value)
    {
        //Transfer string verbatim to MMRIA field; map values of 99999 to blank
        if (value == "99999")
            value = string.Empty;

        return value;
    }

    private string TRANSPRT_Rule(string value)
    {
        //"1. Map character to MMRIA code values as follows: 
        //Blank fields -> 9999(blank)
        //DR-> 0 = Driver / Operator
        //PA-> 1 = Passenger
        //PE-> 2 = Pedestrian
        //Map any other text -> 3 = Other
        //2.Map full text to MMRIA Specify Other field"

        switch (value?.ToUpper())
        {
            case "DR":
                value = "0";
                break;
            case "PA":
                value = "1";
                break;
            case "PE":
                value = "2";
                break;
            case "":
                value = "9999";
                break;
            default:
                value = "3";
                break;
        }

        return value;
    }

    private string TRANSPRT_other_specify_Rule(string value)
    {
        //"1. Map character to MMRIA code values as follows: 
        //Blank fields -> 9999(blank)
        //DR-> 0 = Driver / Operator
        //PA-> 1 = Passenger
        //PE-> 2 = Pedestrian
        //Map any other text -> 3 = Other
        //2.Map full text to MMRIA Specify Other field"

        switch (value?.ToUpper())
        {
            case "DR":
                value = "";
                break;
            case "PA":
                value = "";
                break;
            case "PE":
                value = "";
                break;
            case "":
                value = "";
                break;
            default:
                //I know this looks weird, just coded it to show that the above values resolve this field to empty
                // but other text is passed through
                value = value;
                break;
        }

        return value;
    }


    private string VRO_STATUS_Rule(string value)
    {
        //3-> 3 = N / A(identified via linkage or literal cause of death field)        9999-> 9999(blank)
        if (value == "9999")
            value = string.Empty;

        return value;
    }

    private string DEDUC_Rule(string value)
    {
        //Map number to MMRIA number codes as follows:
        //Empty columns -> 9999 = (blank)
        //1-> 0 = 8th Grade or Less
        //2-> 1 = 9th - 12th grade; No Diploma
        //3-> 2 = High School Graduate or GED Completed
        //4-> 3 = Some college credit, but no degree
        //5-> 4 = Associate Degree
        //6-> 5 = Bachelor's Degree
        //7-> 6 = Master's Degree
        //8-> 7 = Doctorate Degree or Professional Degree
        //9-> 7777 = Unknown

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;

    }

    private string TOD_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field, format as MMRIA time.; if TOD = 9999 then this field should be left blank
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);

            }
        }

        return parsedValue;
    }

    private string DOD_DY_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; if DOD_DY = 99 then this field should be mapped to 9999(blank)
        if (value == "99")
            value = "9999";

        return value;
    }

    private string DOD_MO_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; if DOD_MO = 99 then this field should be mapped to 9999(blank)
        if (value == "99")
            value = "9999";

        return value;
    }

    private string MARITAL_Rule(string value)
    {
        //Map character to MMRIA number codes as follows:
        //Blank-> 9999 = (blank)
        //M-> 0 = Married
        //A-> 1 = Married, but Separated
        //W-> 2 = Widowed
        //D-> 3 = Divorced
        //S-> 4 = Never Married
        //U->  7777 = Unknown

        switch (value?.ToUpper())
        {
            case "M":
                value = "0";
                break;
            case "A":
                value = "1";
                break;
            case "W":
                value = "2";
                break;
            case "D":
                value = "3";
                break;
            case "S":
                value = "4";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string COUNTRYC_Rule(string value)
    {
        //Map to MMRIA field Country listing 
        //Map XX to 9999(blank)
        //Map ZZ to 9999(blank)
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;

    }

    private string STATEC_Rule(string value)
    {
        // Map to MMRIA field state listing.
        //Map XX to 9999(blank)
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    private string BPLACE_ST_Rule(string value)
    {
        // Map to MMRIA field state listing.
        //Map XX to 9999(blank)
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    private string DOB_DY_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; IF value='99', this field should be mapped to 9999 (blank)
        if (value == "99")
            value = "9999";

        return value;
    }

    private string DOB_MO_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; IF value='99', this field should be mapped to 9999 (blank)
        if (value == "99")
            value = "9999";

        return value;
    }

    private string AGE_Rule(string value)
    {
        //Transfer number verbatim to MMRIA field; IF AGE = 999 this field should be left blank
        if (value == "999")
            value = string.Empty;

        return value;
    }

    private string DOD_YR_Rule(string value)
    {
        //Transfer string verbatim to MMRIA field; empty fields should map to 9999(blank)
        if (string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    #endregion

    #region NAT Rules
    private object NAT_maternal_morbidity_Rule(string value1, string value2, string value3, string value4, string value5, string value6)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N")
        //    determinedValues.Add("6");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

        }

        return determinedValues.ToArray();
    }

    private object NAT_characteristics_of_labor_and_delivery_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

INDL = Y --> bfdcp_cola_deliv = 0 Induction of labor

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N")
        //    determinedValues.Add("9");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);
        }

        return determinedValues.ToArray();
    }

    private object NAT_onset_of_labor_Rule(string value1, string value2, string value3)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

PROM = Y --> bfdcp_oo_labor = 0 Premature Rupture of Membranes (Prolonged)

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N")
        //    determinedValues.Add("3");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

        }

        return determinedValues.ToArray();
    }

    private object NAT_obstetric_procedures_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

CERV = Y --> bfdcp_o_proce = 0 Cervical Cerclage

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N")
        //    determinedValues.Add("4");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

        }

        return determinedValues.ToArray();
    }

    private object NAT_infections_present_or_treated_during_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N")
        //    determinedValues.Add("10");
        //else
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

        }

        return determinedValues.ToArray();
    }

    private object NAT_risk_factors_in_this_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9, string value10, string value11)
    {
        //    /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        //   EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

/*
                                field_set["PDIAB"],
                                field_set["GDIAB"],
                                field_set["PHYPE"],
                                field_set["GHYPE"],
                                field_set["PPB"],
                                field_set["PPO"],
                                field_set["INFT"],
                                field_set["PCES"],
                                field_set["EHYPE"],
                                field_set["INFT_DRG"],
                                field_set["INFT_ART"]
*/


        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        //   *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //    && value9 == "N")
        //    determinedValues.Add("11");
        //else 
        if 
        (
            value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
            && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);


            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);
        }

        return determinedValues.ToArray();
    }

    private object NAT_congenital_Rule(string value1, string value2, string value3, string value4, string value5
        , string value6, string value7, string value8, string value9
        , string value10, string value11, string value12)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N" && value10 == "N" && value11 == "N" && value12 == "N")
        //    determinedValues.Add("17");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U" && value10 == "U" && value11 == "U" && value12 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);

            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);

            if (int.TryParse(value12, out result))
                determinedValues.Add(value12);
        }

        return determinedValues.ToArray();
    }

    private object NAT_abnormal_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N")
        //    determinedValues.Add("8");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);
        }

        return determinedValues.ToArray();
    }

    private string LOCATION_OF_RESIDENCE_street_Rule(string stnum_r, string predir_r, string stname_r, string stdesig_r, string postdir_r)
    {
        //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
        string determinedValue = $"{stnum_r} {predir_r} {stname_r} {stdesig_r} {postdir_r}";

        return determinedValue;
    }

    private string DATE_OF_DELIVERY_Rule(string year, string month, string day)
    {
        //2.Merge 3 fields(IDOB_MO, IDOB_DY, IDOB_YR) map resulting date to MMRIA field -date_of _delivery(bcifsri_do_deliv)."
        string determinedValue = $"{year}-{month}-{day}";

        return determinedValue;
    }

    private string IDOB_YR_Merge_Rule(string value)
    {
        /*1. Transfer number verbatim to MMRIA field - date_of_delivery/year (bfdcpfodddod_year)
        2. Merge 3 fields (IDOB_MO, IDOB_DY, IDOB_YR) map resulting date to MMRIA field - date_of _delivery (bcifsri_do_deliv).*/
        return value;
    }

    private string MDOB_YR_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    private string MDOB_MO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string MDOB_DY_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string FDOB_YR_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    private string FDOB_MO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string NAT_STATEC_Rule(string value)
    {
        //"Map XX --> 9999 (blank)
        //Map ZZ --> 9999(blank)
        //Map all other values to MMRIA field state listing"

        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    private string MARN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ACKN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        X -> 2=Not Applicable
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            case "X":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string MEDUC_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string FEDUC_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ATTEND_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = MD
        2 -> 1 = DO
        3 -> 2 = CNM/CM
        4 -> 3 = Other Midwife
        5 -> 4 = Other 
        9 -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string TRAN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string NPREV_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field. 

        If value = 99, map to 9999 (blank)*/

        if (value == "99")
            value = "";

        return value;
    }

    private string HFT_Rule(string value)
    {
        /*If value is in 1-8, transfer number verbatim to MMRIA field. 

        If value = 9, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "9")
            value = "";

        return value;
    }

    private string HIN_Rule(string value)
    {
        /*If value is in 00-11, transfer number verbatim to MMRIA field. 

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    private string PWGT_Rule(string value)
    {
        /*If value is in 050-400, transfer number verbatim to MMRIA field.

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    private string DWGT_Rule(string value)
    {
        /*If value is in 050-450, transfer number verbatim to MMRIA field.  

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    private string WIC_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string PLBL_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    private string PLBD_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    private string POPO_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99" || value == "9999")
            value = "";

        return value;
    }

    private string MLLB_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string YLLB_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string MOPO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string YOPO_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.  

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string PAY_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 1 = Medicaid
        2 -> 0 = Private Insurance
        3 -> 2 = Self-pay                                       
        4 -> 4=Indian Health Service                     
        5 -> 5=CHAMPUS/TRICARE                               
        6 -> 6 = Other Government (Fed, State, Local)
        8 -> 3 = Other                                          
        9 -> 7777=Unknown*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "1";
                break;
            case "2":
                value = "0";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "4";
                break;
            case "5":
                value = "5";
                break;
            case "6":
                value = "6";
                break;
            case "8":
                value = "3";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string DLMP_YR_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    private string DLMP_MO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string DLMP_DY_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string NPCES_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, leave the value empty/blank.*/

        if (value == "99")
            value = "";

        return value;
    }

    private string ATTF_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ATTV_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  -> 7777 = Unknown
        */

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string PRES_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Cephalic
        2 -> 1 = Breech
        3 -> 4 = Other
        9 -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ROUT_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Vaginal/Spontaneous
        2 -> 1 = Vaginal/Forceps
        3  -> 2 = Vaginal/Vacuum
        4  -> 3 = Cesarean
        9  -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "9":
                value = "7";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string OWGEST_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the value empty/blank. */

        if (value == "99")
            value = "";

        return value;
    }

    private string APGAR5_Rule(string value)
    {
        /*If value is in 00-10, transfer number verbatim to MMRIA field.  

        If value = 99, leave the value empty/blank. */

        if (value == "99")
            value = "";

        return value;
    }

    private string APGAR10_Rule(string value)
    {
        /*If value is in 00-10, transfer number verbatim to MMRIA field.  

        If value = 88 or 99, leave the value empty/blank. */

        if (value == "88" || value == "99")
            value = "";

        return value;
    }

    private string SORD_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.  

        If value = 99, leave the MMRIA value empty/blank.*/

        if (value == "99")
            value = "";

        return value;
    }

    private string ITRAN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 Yes
        N  -> 0 No
        U  -> 7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ILIV_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 = Yes
        N  -> 0 = No
        U  -> 2 = Infant transferred, status unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string BFED_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 Yes
        N  -> 0 No
        U  -> 7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ISEX_NAT_Rule(string value)
    {
        /*M = Male -> 0:Male
        F = Female -> 1:Female
        N = 2:Not Yet Determined

        Map empty rows to 9999 (blank)
        */

        switch (value?.ToUpper())
        {
            case "M":
                value = "0";
                break;
            case "F":
                value = "1";
                break;
            case "N":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    private string BPLACE_place_NAT_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

        2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

        3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

        4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

        5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

        6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

        7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

        9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "2";
                break;
            case "5":
                value = "2";
                break;
            case "6":
                value = "3";
                break;
            case "7":
                value = "4";
                break;
            default:
                value = "7777";
                break;
        }
        return value;
    }
    private string BPLACE_plann_NAT_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

        2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

        3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

        4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

        5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

        6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

        7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

        9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "9999";
                break;
            case "2":
                value = "9999";
                break;
            case "3":
                value = "1";
                break;
            case "4":
                value = "0";
                break;
            case "5":
                value = "7777";
                break;
            case "6":
                value = "9999";
                break;
            case "7":
                value = "9999";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }
    private string BPLACEC_ST_TER_NAT_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    private string NAT_METHNIC_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of METHNIC1, METHNIC2, METHNIC3, METHNIC4 to populate MMRIA field bfdcpdom_ioh_origi.

        H --> bfdcpdom_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdom_ioh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdom_ioh_origi = 3 Yes, Cuban
        H --> bfdcpdom_ioh_origi = 4 Yes, Other Spanish/Hispanic/Latino

        If METHNIC1 = N and METHNIC2 = N and METHNIC3 = N and METHNIC 4 = N --> bfdcpdom_ioh_origi = 0 No, Not Spanish/Hispanic/Latino

        If METHNIC1 = U and METHNIC2 = U and METHNIC3 = U and METHNIC4 = U --> bfdcpdom_ioh_origi = 7777 Unknown

        If METHNIC1 = (empty) and METHNIC2 = (empty) and METHNIC3 = (empty) and METHNIC4 = (empty) --> bfdcpdom_ioh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }

    private string[] MRACE_NAT_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from MRACE1 through MRACE15 to populate MMRIA multi-select field (bfdcpr_ro_mothe).

        MRACE1 = Y --> bfdcpr_ro_mothe = 0 White
        MRACE2 = Y --> bfdcpr_ro_mothe = 1 Black or African American
        MRACE3 = Y --> bfdcpr_ro_mothe = 2 American Indian or Alaska Native
        MRACE4 = Y --> bfdcpr_ro_mothe = 7 Asian Indian
        MRACE5 = Y --> bfdcpr_ro_mothe = 8 Chinese
        MRACE6 = Y --> bfdcpr_ro_mothe = 9 Filipino
        MRACE7 = Y --> bfdcpr_ro_mothe = 10 Japanese
        MRACE8 = Y --> bfdcpr_ro_mothe = 11 Korean
        MRACE9 = Y --> bfdcpr_ro_mothe = 12 Vietnamese
        MRACE10 = Y --> bfdcpr_ro_mothe = 13 Other Asian
        MRACE11 = Y --> bfdcpr_ro_mothe = 3 Native Hawaiian
        MRACE12 = Y --> bfdcpr_ro_mothe = 4 Guamanian or Chamorro
        MRACE13 = Y --> bfdcpr_ro_mothe = 5 Samoan
        MRACE14 = Y --> bfdcpr_ro_mothe = 6 Other Pacific Islander
        MRACE15 = Y --> bfdcpr_ro_mothe = 14 Other Race

        If every one of MRACE1 through MRACE15 is equal to "N", then bfdcpr_ro_mothe = 8888 (Race Not Specified)*/
        //Defaulting to blank
        List<string> determinedValues = new List<string>();

        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }
        if(determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }

        return determinedValues.ToArray();
    }

    private string MRACE16_17_NAT_Rule(string value16, string value17)
    {
        /*Combine MRACE16 and MRACE17 into one field (bfdcpr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE16 to MMRIA field.
        2. Transfer string verbatim from MRACE17 and add to same MMRIA field.
        3. If both MRACE16 and MRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value16) && 
            !string.IsNullOrWhiteSpace(value17)
        )
        {
            value = $"{value16}, {value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else if (!string.IsNullOrWhiteSpace(value17))
        {
            value = $"{value17}";
        }

        return value;
    }

    private string MRACE18_19_NAT_Rule(string value18, string value19)
    {
        /*Combine MRACE18 and MRACE19 into one field (bfdcpr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE18 to MMRIA field.
        2. Transfer string verbatim from MRACE19 and add to same MMRIA field.
        3. If both MRACE18 and MRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value18) && 
            !string.IsNullOrWhiteSpace(value19)
        )
        {
            value = $"{value18}, {value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else if (!string.IsNullOrWhiteSpace(value19))
        {
            value = $"{value19}";
        }

        return value;
    }

    private string MRACE20_21_NAT_Rule(string value20, string value21)
    {
        /*Combine MRACE20 and MRACE21 into one field (bfdcpr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE20 to MMRIA field.
        2. Transfer string verbatim from MRACE21 and add to same MMRIA field.
        3. If both MRACE20 and MRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value20) && 
            !string.IsNullOrWhiteSpace(value21)
        )
        {
            value = $"{value20}, {value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else if (!string.IsNullOrWhiteSpace(value21))
        {
            value = $"{value21}";
        }

        return value;
    }

    private string MRACE22_23_NAT_Rule(string value22, string value23)
    {
        /*Combine MRACE22 and MRACE23 into one field (bfdcpr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE22 to MMRIA field.
        2. Transfer string verbatim from MRACE23 and add to same MMRIA field.
        3. If both MRACE22 and MRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value22) && 
            !string.IsNullOrWhiteSpace(value23)
        )
        {
            value = $"{value22}, {value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else if (!string.IsNullOrWhiteSpace(value23))
        {
            value = $"{value23}";
        }

        return value;
    }

    private string FETHNIC_NAT_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of FETHNIC1, FETHNIC2, FETHNIC3, FETHNIC4 to populate MMRIA field bfdcpdof_ifoh_origi.

            H --> bfdcpdof_ifoh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdof_ifoh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdof_ifoh_origi = 3 Yes, Cuban
        H --> bfdcpdof_ifoh_origi = 4, Yes, Other Spanish/Hispanic/Latino

            If FETHNIC1 = N and FETHNIC2 = N and FETHNIC3 = N and FETHNIC 4 = N --> bfdcpdof_ifoh_origi = 0 No, Not Spanish/Hispanic/Latino

            If FETHNIC1 = U and FETHNIC2 = U and FETHNIC3 = U and FETHNIC4 = U --> bfdcpdof_ifoh_origi = 7777 Unknown

            If FETHNIC1 = (empty) and FETHNIC2 = (empty) and FETHNIC3 = (empty) and FETHNIC4 = (empty) --> bfdcpdof_ifoh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }


    private string[] FRACE_NAT_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from FRACE1 through FRACE15 to populate MMRIA multi-select field (bfdcpdofr_ro_fathe).

        FRACE1 = Y --> bfdcpdofr_ro_fathe = 0 White
        FRACE2 = Y --> bfdcpdofr_ro_fathe = 1 Black or African American
        FRACE3 = Y --> bfdcpdofr_ro_fathe = 2 American Indian or Alaska Native
        FRACE4 = Y --> bfdcpdofr_ro_fathe = 7 Asian Indian
        FRACE5 = Y --> bfdcpdofr_ro_fathe = 8 Chinese
        FRACE6 = Y --> bfdcpdofr_ro_fathe = 9 Filipino
        FRACE7 = Y --> bfdcpdofr_ro_fathe = 10 Japanese
        FRACE8 = Y --> bfdcpdofr_ro_fathe = 11 Korean
        FRACE9 = Y --> bfdcpdofr_ro_fathe = 12 Vietnamese
        FRACE10 = Y --> bfdcpdofr_ro_fathe = 13 Other Asian
        FRACE11 = Y --> bfdcpdofr_ro_fathe = 3 Native Hawaiian
        FRACE12 = Y --> bfdcpdofr_ro_fathe = 4 Guamanian or Chamorro
        FRACE13 = Y --> bfdcpdofr_ro_fathe = 5 Samoan
        FRACE14 = Y --> bfdcpdofr_ro_fathe = 6 Other Pacific Islander
        FRACE15 = Y --> bfdcpdofr_ro_fathe = 14 Other Race

        If every one of FRACE1 through FRACE15 is equal to "N", then bfdcpdofr_ro_fathe = 8888 (Race Not Specified)*/
        List<string> determinedValues = new List<string>();


        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }

        if(determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }

        return determinedValues.ToArray();
    }

    private string FRACE16_17_NAT_Rule(string value16, string value17)
    {
        /*Combine FRACE16 and FRACE17 into one field (bfdcpdofr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE16 to MMRIA field.
        2. Transfer string verbatim from FRACE17 and add to same MMRIA field.
        3. If both FRACE16 and FRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value16) || string.IsNullOrWhiteSpace(value17)))
        {
            value = $"{value16}|{value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else
        {
            value = $"{value17}";
        }

        return value;
    }

    private string FRACE18_19_NAT_Rule(string value18, string value19)
    {
        /*Combine FRACE18 and FRACE19 into one field (bfdcpdofr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE18 to MMRIA field.
        2. Transfer string verbatim from FRACE19 and add to same MMRIA field.
        3. If both FRACE18 and FRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value18) || string.IsNullOrWhiteSpace(value19)))
        {
            value = $"{value18}|{value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else
        {
            value = $"{value19}";
        }

        return value;
    }

    private string FRACE20_21_NAT_Rule(string value20, string value21)
    {
        /*Combine FRACE20 and FRACE21 into one field (bfdcpdofr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE20 to MMRIA field.
        2. Transfer string verbatim from FRACE21 and add to same MMRIA field.
        3. If both FRACE20 and FRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value20) || string.IsNullOrWhiteSpace(value21)))
        {
            value = $"{value20}|{value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else
        {
            value = $"{value21}";
        }

        return value;
    }

    private string FRACE22_23_NAT_Rule(string value22, string value23)
    {
        /*Combine FRACE22 and FRACE23 into one field (bfdcpdofr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE22 to MMRIA field.
        2. Transfer string verbatim from FRACE23 and add to same MMRIA field.
        3. If both FRACE22 and FRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value22) || string.IsNullOrWhiteSpace(value23)))
        {
            value = $"{value22}|{value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else
        {
            value = $"{value23}";
        }

        return value;
    }

    private string DOFP_MO_NAT_Rule(string value)
    {
        /*
        If DOFP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdo1pv_month).

        If DOFP_MO = 99 --> bfdcppcdo1pv_month = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string DOFP_DY_NAT_Rule(string value)
    {
        /*If DOFP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdo1pv_day).

        If DOFP_DY = 99 --> bfdcppcdo1pv_day = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string DOFP_YR_NAT_Rule(string value)
    {
        /*If DOFP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdo1pv_year).

        If DOFP_YR = 9999 --> bfdcppcdo1pv_year = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string DOLP_MO_NAT_Rule(string value)
    {
        /*If DOLP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdolpv_month).

        If DOLP_MO = 99 --> bfdcppcdolpv_month = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string DOLP_DY_NAT_Rule(string value)
    {
        /*If DOLP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdolpv_day).

        If DOLP_DY = 99 --> bfdcppcdolpv_day = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.*/
        if (value == "88" || value == "99")
            value = "9999";
        return value;
    }

    private string DOLP_YR_NAT_Rule(string value)
    {
        /*If DOLP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdolpv_year).

        If DOLP_YR = 9999 --> bfdcppcdolpv_year = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string CIGPN_NAT_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month = (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    private string CIGPN_Type_NAT_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month = 9999 (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string CIGFN_NAT_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    private string CIGFN_Type_NAT_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string CIGSN_NAT_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    private string CIGSN_Type_NAT_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string CIGLN_NAT_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    private string CIGLN_Type_NAT_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string CIG_none_or_not_specified_NAT_Rule(string value1, string value2, string value3, string value4)
    {
        /*
        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        string determinedValue = "9999";

        if 
        (
            value1 == "99" && 
            value2 == "99" && 
            value3 == "99" && 
            value4 == "99"
        )
        {
            determinedValue = "7777";
        }
        else if 
        (
            (
                value1 == "00" && 
                value2 == "00" && 
                value3 == "00" && 
                value4 == "00"
            ) || 
            (
                value1 == "0" && 
                value2 == "0" && 
                value3 == "0" && 
                value4 == "0"
            )
        )
        {
            determinedValue = "0";
        }
        return determinedValue;
    }

    private string PDIAB_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PDIAB = Y --> bfdcprf_rfit_pregn = 0 Prepregnancy Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        if (value == "Y")
            value = "0";

        return value;
    }
    private string GDIAB_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GDIAB = Y --> bfdcprf_rfit_pregn = 1 Gestational Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        if (value == "Y")
            value = "1";

        return value;
    }
    private string PHYPE_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PHYPE = Y --> bfdcprf_rfit_pregn = 2 Prepregnacy Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "2";

        return value;
    }
    private string GHYPE_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GHYPE = Y --> bfdcprf_rfit_pregn = 3 Gestational Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "3";

        return value;
    }
    private string PPB_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPB = Y --> bfdcprf_rfit_pregn = 5 Previous Preterm Birth

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "5";

        return value;
    }
    private string PPO_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPO = Y --> bfdcprf_rfit_pregn = 6 Other Previous Poor Outcome

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "6";

        return value;
    }
    private string INFT_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT = Y --> bfdcprf_rfit_pregn = 7 Pregnancy Resulted from Infertility Treatment

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "7";

        return value;
    }
    private string PCES_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PCES = Y --> bfdcprf_rfit_pregn = 10 Mother had a Previous Cesarean Delivery

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "10";

        return value;
    }
    private string GON_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }
    private string SYPH_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        SYPH = Y --> bfdcp_ipotd_pregn = 3 Syphilis

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    private string HSV_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        HSV = Y --> bfdcp_ipotd_pregn = 11 Herpes Simplex [HSV]

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }
    private string CHAM_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        CHAM = Y --> bfdcp_ipotd_pregn = 6 Chlamydia

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }
    private string HEPB_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        HEPB = Y --> bfdcp_ipotd_pregn = 0 Hepatitis B (live birth only)

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }
    private string HEPC_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        HEPC = Y --> bfdcp_ipotd_pregn = 1 Hepatitis C (live birth only)

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }
    private string CERV_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        CERV = Y --> bfdcp_o_proce = 0 Cervical Cerclage

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }
    private string TOC_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        TOC = Y --> bfdcp_o_proce = 1 Tocolysis

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    private string ECVS_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        ECVS = Y --> bfdcp_o_proce = 2 External Cephalic Version: Successful

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    private string ECVF_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        ECVS = Y --> bfdcp_o_proce = 3 External Cephalic Version: Failed

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    private string PROM_NAT_Rule(string value)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

        PROM = Y --> bfdcp_oo_labor = 0 Premature Rupture of Membranes (Prolonged)

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    private string PRIC_NAT_Rule(string value)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

        PRIC = Y --> bfdcp_oo_labor = 2 Precipitous labor (< 3 hours)

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    private string PROL_NAT_Rule(string value)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

        PROL = Y --> bfdcp_oo_labor = 1 Prolonged labor (> 20 hours)

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    private string INDL_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        INDL = Y --> bfdcp_cola_deliv = 0 Induction of labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    private string AUGL_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        AUGL = Y --> bfdcp_cola_deliv = 4 Augmentation of labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    private string NVPR_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        NVPR = Y --> bfdcp_cola_deliv = 8 Non-vertex presentation

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }

    private string STER_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        STER = Y --> bfdcp_cola_deliv = 1 Steroids (glucocorticoids) for fetal lung maturation received by mother prior to delivery

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    private string ANTB_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        ANTB = Y --> bfdcp_cola_deliv = 5 Antibiotics received by the mother during labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }

    private string CHOR_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        CHOR = Y --> bfdcp_cola_deliv = 2 Clinical chorioamnionitis diagnosed during labor or maternal temperature >= 38 degrees C (100.4 degrees F)

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    private string MECS_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        MECS = Y --> bfdcp_cola_deliv = 6 Moderate to heavy meconium staining of the amniotic fluid

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }

    private string FINT_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        FINT = Y --> bfdcp_cola_deliv = 7 Fetal intolerance of labor such that one or more of the following actions was taken: in-utero resuscitative measures, further fetal assessment, or operative delivery 

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "7";

        return value;
    }

    private string ESAN_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        ESAN = Y --> bfdcp_cola_deliv = 3 Epidural or spinal anesthesia during labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    private string TLAB_NAT_Rule(string value)
    {
        /*Y = Yes -> 1 Yes
        N = No -> 0 No
        U = Unknown -> 7777 Unknown
        X = Not Applicable -> 2 Not Applicable

        Map empty rows to 9999 (blank)
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            case "X":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string MTR_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    private string PLAC_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        PLAC = Y --> bfdcp_m_morbi = 3 Third or fourth degree perineal laceration

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    private string RUT_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        RUT = Y --> bfdcp_m_morbi = 5 Ruptured uterus

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }

    private string UHYS_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UHYS = Y --> bfdcp_m_morbi = 1 Unplanned hysterectomy

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    private string AINT_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        AINT = Y --> bfdcp_m_morbi = 4 Admission to intensive care unit

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    private string UOPR_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UOPR = Y --> bfdcp_m_morbi = 2 Unplanned operating room procedure following delivery

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    private string BWG_NAT_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        if (value == "9999")
            value = "";

        return value;
    }

    private string BWG_measu_NAT_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        if (value == "9999")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string PLUR_Custom_NAT_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "1";
                break;
            case "02":
            case "2":
                value = "2";
                break;
            case "03":
            case "3":
                value = "3";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "4";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    private string PLUR_sigt_NAT_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "";
                break;
            case "02":
            case "2":
                value = "";
                break;
            case "03":
            case "3":
                value = "";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = value;
                break;
            default:
                value = "";
                break;
        }

        return value;
    }
    private string PLUR_gesta_NAT_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "0";
                break;
            case "02":
            case "2":
                value = "1";
                break;
            case "03":
            case "3":
                value = "1";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "1";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string AVEN1_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        AVEN1 = Y --> bcifs_aco_newbo = 0 Assisted ventilation required immediately following delivery

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    private string AVEN6_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        AVEN6 = Y --> bcifs_aco_newbo = 3 Assisted ventilation required for more than 6 hours

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    private string NICU_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        NICU = Y --> bcifs_aco_newbo = 4 NICU admission

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    private string SURF_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        SURF = Y --> bcifs_aco_newbo = 1 Newborn given surfactant replacement therapy

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    private string ANTI_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        ANTI = Y --> bcifs_aco_newbo = 5 Antibiotics received by the newborn for suspected neonatal sepsis

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }

    private string SEIZ_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        SEIZ = Y --> bcifs_aco_newbo = 2 Seizure or serious neurologic dysfunction

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    private string BINJ_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        BINJ = Y --> bcifs_aco_newbo = 6 Significant birth injury (skeletal fracture(s), peripheral nerve injury and or soft tissue or solid organ hemorrhage which requires intervention)

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }

    private string ANEN_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        ANEN = Y --> bcifs_c_anoma = 0 Anencephaly

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    private string MNSB_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        MNSB = Y --> bcifs_c_anoma = 9 Meningomyelocele or Spina bifida

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "9";

        return value;
    }

    private string CCHD_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CCHD = Y --> bcifs_c_anoma = 1 Cyanotic congenital heart disease

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    private string CDH_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CDH = Y --> bcifs_c_anoma = 10 Congenital diaphragmatic hernia

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "10";

        return value;
    }

    private string OMPH_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        OMPH = Y --> bcifs_c_anoma = 2 Omphalocele

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    private string GAST_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        GAST = Y --> bcifs_c_anoma = 11 Gastroschisis

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }

    private string LIMB_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        LIMB = Y --> bcifs_c_anoma = 3 Limb reduction defect (excluding congenital amputation and dwarfing syndromes)

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    private string CL_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CL = Y --> bcifs_c_anoma = 4 Cleft Lip with or without Cleft Palate

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    private string CP_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CP = Y --> bcifs_c_anoma = 12 Cleft palate alone

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "12";

        return value;
    }

    private string DOWT_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If DOWT = C --> bcifs_c_anoma = 6 Karyotype confirmed - Downs Syndrome
        If DOWT = P --> bcifs_c_anoma = 7 Karyotype pending - Downs Syndrome

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "C")
            value = "6";
        else if (value == "P")
            value = "7";

        return value;
    }

    private string CDIT_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If CDIT = C --> bcifs_c_anoma = 14 Karyotype confirmed - Suspected chromosomal disorder
        If CDIT = P --> bcifs_c_anoma = 15 Karyotype pending - Suspected chromosomal disorder

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "C")
            value = "14";
        else if (value == "P")
            value = "15";

        return value;
    }

    private string HYPO_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        HYPO = Y --> bcifs_c_anoma = 8 Hypospadias

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }

    private string MAGER_NAT_Rule(string value, string dob_YR, string dob_MO, string dob_day, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, dob_day, dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }

    private string FAGER_NAT_Rule(string value, string dob_YR, string dob_MO, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, "1", dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }

    private string EHYPE_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "4";

        return value;
    }

    private string INFT_DRG_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_DRG = Y --> bfdcprf_rfit_pregn = 8 Fertility Enhancing Drugs, Artificial Insemination or Intrauterine Insemination

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "8";

        return value;
    }

    private string INFT_ART_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_ART = Y --> bfdcprf_rfit_pregn = 9 Assisted Reproductive Technology (e.g. in vitro fertilization (IVF), gamete intrafallopian transfer (GIFT))

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "9";

        return value;
    }


    private string FBPLACD_ST_TER_C_NAT_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    private string FBPLACE_CNT_C_NAT_Rule(string value)
    {
        /*Map to MMRIA field country listing

        Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    #endregion

    #region FET Rules

    private object FET_maternal_morbidity_Rule(string value1, string value2, string value3, string value4, string value5, string value6)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N")
        //    determinedValues.Add("6");
        //else
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

        }

        return determinedValues.ToArray();
    }

    private object FET_characteristics_of_labor_and_delivery_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

INDL = Y --> bfdcp_cola_deliv = 0 Induction of labor

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N")
        //    determinedValues.Add("9");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);
        }

        return determinedValues.ToArray();
    }

    private object FET_onset_of_labor_Rule(string value1, string value2, string value3)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

PROM = Y --> bfdcp_oo_labor = 0 Premature Rupture of Membranes (Prolonged)

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N")
        //    determinedValues.Add("3");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

        }

        return determinedValues.ToArray();
    }

    private object FET_obstetric_procedures_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

CERV = Y --> bfdcp_o_proce = 0 Cervical Cerclage

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N")
        //    determinedValues.Add("4");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

        }

        return determinedValues.ToArray();
    }


    private object FET_infections_present_or_treated_during_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9, string value10, string value11, string value12)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N" && value10 == "N" && value11 == "N" && value12 == "N")
        //    determinedValues.Add("17");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U" && value10 == "U" && value11 == "U" && value12 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);

            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);

            if (int.TryParse(value12, out result))
                determinedValues.Add(value12);
        }

        return determinedValues.ToArray();
    }

    private object FET_risk_factors_in_this_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9)
    {
        //    /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        //   EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        //   *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //    && value9 == "N")
        //    determinedValues.Add("11");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
            && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

        }

        return determinedValues.ToArray();
    }

    private object FET_congenital_Rule(string value1, string value2, string value3, string value4, string value5
        , string value6, string value7, string value8, string value9
        , string value10, string value11, string value12)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N" && value10 == "N" && value11 == "N" && value12 == "N")
        //    determinedValues.Add("17");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U" && value10 == "U" && value11 == "U" && value12 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);

            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);

            if (int.TryParse(value12, out result))
                determinedValues.Add(value12);
        }

        return determinedValues.ToArray();
    }

    private object FET_abnormal_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N")
        //    determinedValues.Add("8");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);
        }

        return determinedValues.ToArray();
    }

    private string FET_LOCATION_OF_RESIDENCE_street_Rule(string stnum_r, string predir_r, string stname_r, string stdesig_r, string postdir_r)
    {
        //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
        string determinedValue = $"{stnum_r} {predir_r} {stname_r} {stdesig_r} {postdir_r}";

        return determinedValue;
    }

    private string FET_DATE_OF_DELIVERY_Rule(string year, string month, string day)
    {
        //2.Merge 3 fields(IDOB_MO, IDOB_DY, IDOB_YR) map resulting date to MMRIA field -date_of _delivery(bcifsri_do_deliv)."
        string determinedValue = $"{year}-{month}-{day}";

        return determinedValue;
    }


    private string MDOB_YR_FET_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/
        if (value == "9999")
            value = "9999";

        return value;
    }

    private string MDOB_MO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.  

        If value = 99, map to 9999 (blank). */

        if (value == "99")
            value = "9999";

        return value;
    }

    private string MDOB_DY_FET_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.  
            * If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string FDOB_YR_FET_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    private string FDOB_MO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string MARN_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string MEDUC_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string ATTEND_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = MD
        2 -> 1 = DO
        3 -> 2 = CNM/CM
        4 -> 3 = Other Midwife
        5 -> 4 = Other 
        9 -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string TRAN_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string NPREV_FET_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field. 

        If value = 99, map to 9999 (blank)*/

        if (value == "99")
            value = "";

        return value;
    }

    private string HFT_FET_Rule(string value)
    {
        /*If value is in 1-8, transfer number verbatim to MMRIA field. 

        If value = 9, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "9")
            value = "";

        return value;
    }

    private string HIN_FET_Rule(string value)
    {
        /*If value is in 00-11, transfer number verbatim to MMRIA field. 

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    private string PWGT_FET_Rule(string value)
    {
        /*If value is in 050-400, transfer number verbatim to MMRIA field.

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    private string DWGT_FET_Rule(string value)
    {
        /*If value is in 050-450, transfer number verbatim to MMRIA field.  

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    private string WIC_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string PLBL_FET_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    private string PLBD_FET_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    private string POPO_FET_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99" || value == "9999")
            value = "";

        return value;
    }

    private string MLLB_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string YLLB_FET_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string MOPO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string YOPO_FET_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.  

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string DLMP_YR_FET_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    private string DLMP_MO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/
        if (value == "99")
            value = "9999";

        return value;
    }

    private string DLMP_DY_FET_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string NPCES_FET_Rule(string value)
    {
        /*Transfer number verbatim to MMRIA field.  Map 99 to 9999 (blank)*/

        if (value == "99")
            value = "9999";

        return value;
    }

    private string ATTF_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string ATTV_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  -> 7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string PRES_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Cephalic
        2 -> 1 = Breech
        3 -> 4 = Other
        9 -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ROUT_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Vaginal/Spontaneous
        2 -> 1 = Vaginal/Forceps
        3  -> 2 = Vaginal/Vacuum
        4  -> 3 = Cesarean
        9  -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string OWGEST_FET_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the value empty/blank. */

        if (value == "99")
            value = "";

        return value;
    }

    private string SORD_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.  

        If value = 99, leave the MMRIA value empty/blank.*/

        if (value == "99")
            value = "";

        return value;
    }


    private string FEDUC_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string FSEX_FET_Rule(string value)
    {
        /*M = Male -> 0 Male
        F = Female -> 1 Female
        U = Unknown -> 7777 Unknown

        Map empty rows to 9999 (blank)
        */
        switch (value?.ToUpper())
        {
            case "M":
                value = "0";
                break;
            case "F":
                value = "1";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }
    private string DPLACE_Custom_FET_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

        2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

        3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

        4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

        5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

        6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

        7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

        9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "2";
                break;
            case "5":
                value = "2";
                break;
            case "6":
                value = "3";
                break;
            case "7":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    private string DPLACE_plann_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

            2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

            3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

            4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

            5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

            6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

            7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

            9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "9999";
                break;
            case "2":
                value = "9999";
                break;
            case "3":
                value = "1";
                break;
            case "4":
                value = "0";
                break;
            case "5":
                value = "7777";
                break;
            case "6":
                value = "9999";
                break;
            case "7":
                value = "9999";
                break;
            case "9":
                value = "9999";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    private string BPLACEC_ST_TER_FET_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    private string BPLACEC_CNT_FET_Rule(string value)
    {
        /*Map to MMRIA field country listing 

        Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    
    private string METHNIC_FET_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of METHNIC1, METHNIC2, METHNIC3, METHNIC4 to populate MMRIA field bfdcpdom_ioh_origi.

        H --> bfdcpdom_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdom_ioh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdom_ioh_origi = 3 Yes, Cuban
        H --> bfdcpdom_ioh_origi = 4 Yes, Other Spanish/Hispanic/Latino


        If METHNIC1 = N and METHNIC2 = N and METHNIC3 = N and METHNIC 4 = N --> bfdcpdom_ioh_origi = 0 No, Not Spanish/Hispanic/Latino

        If METHNIC1 = U and METHNIC2 = U and METHNIC3 = U and METHNIC4 = U --> bfdcpdom_ioh_origi = 7777 Unknown

        If METHNIC1 = (empty) and METHNIC2 = (empty) and METHNIC3 = (empty) and METHNIC4 = (empty) --> bfdcpdom_ioh_origi = 9999 (blank)*/
        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }

    private string[] MRACE_FET_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from MRACE1 through MRACE15 to populate MMRIA multi-select field (bfdcpr_ro_mothe).

        MRACE1 = Y --> bfdcpr_ro_mothe = 0 White
        MRACE2 = Y --> bfdcpr_ro_mothe = 1 Black or African American
        MRACE3 = Y --> bfdcpr_ro_mothe = 2 American Indian or Alaska Native
        MRACE4 = Y --> bfdcpr_ro_mothe = 7 Asian Indian
        MRACE5 = Y --> bfdcpr_ro_mothe = 8 Chinese
        MRACE6 = Y --> bfdcpr_ro_mothe = 9 Filipino
        MRACE7 = Y --> bfdcpr_ro_mothe = 10 Japanese
        MRACE8 = Y --> bfdcpr_ro_mothe = 11 Korean
        MRACE9 = Y --> bfdcpr_ro_mothe = 12 Vietnamese
        MRACE10 = Y --> bfdcpr_ro_mothe = 13 Other Asian
        MRACE11 = Y --> bfdcpr_ro_mothe = 3 Native Hawaiian
        MRACE12 = Y --> bfdcpr_ro_mothe = 4 Guamanian or Chamorro
        MRACE13 = Y --> bfdcpr_ro_mothe = 5 Samoan
        MRACE14 = Y --> bfdcpr_ro_mothe = 6 Other Pacific Islander
        MRACE15 = Y --> bfdcpr_ro_mothe = 14 Other Race

        If every one of MRACE1 through MRACE15 is equal to "N", then bfdcpr_ro_mothe = 8888 (Race Not Specified)*/

        List<string> determinedValues = new List<string>();

        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }
        if (determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }
        return determinedValues.ToArray();
    }

    private string MRACE16_17_FET_Rule(string value16, string value17)
    {
        /*Combine MRACE16 and MRACE17 into one field (bfdcpr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE16 to MMRIA field.
        2. Transfer string verbatim from MRACE17 and add to same MMRIA field.
        3. If both MRACE16 and MRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value16) && 
            !string.IsNullOrWhiteSpace(value17)
        )
        {
            value = $"{value16}, {value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else if (!string.IsNullOrWhiteSpace(value17))
        {
            value = $"{value17}";
        }

        return value;
    }
    
    private string MRACE18_19_FET_Rule(string value18, string value19)
    {
        /*Combine MRACE18 and MRACE19 into one field (bfdcpr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE18 to MMRIA field.
        2. Transfer string verbatim from MRACE19 and add to same MMRIA field.
        3. If both MRACE18 and MRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value18) && 
            !string.IsNullOrWhiteSpace(value19)
        )
        {
            value = $"{value18}, {value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else if (!string.IsNullOrWhiteSpace(value19))
        {
            value = $"{value19}";
        }

        return value;
       
    }
    
    private string MRACE20_21_FET_Rule(string value20, string value21)
    {
        /*Combine MRACE20 and MRACE21 into one field (bfdcpr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE20 to MMRIA field.
        2. Transfer string verbatim from MRACE21 and add to same MMRIA field.
        3. If both MRACE20 and MRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value20) && 
            !string.IsNullOrWhiteSpace(value21)
        )
        {
            value = $"{value20}, {value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else if(!string.IsNullOrWhiteSpace(value21))
        {
            value = $"{value21}";
        }

        return value;
    }
    
    private string MRACE22_23_FET_Rule(string value22, string value23)
    {
        /*Combine MRACE22 and MRACE23 into one field (bfdcpr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE22 to MMRIA field.
        2. Transfer string verbatim from MRACE23 and add to same MMRIA field.
        3. If both MRACE22 and MRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value22) &&
            !string.IsNullOrWhiteSpace(value23)
        )
        {
            value = $"{value22}, {value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else if (!string.IsNullOrWhiteSpace(value23))
        {
            value = $"{value23}";
        }

        return value;
    }
    
    private string DOFP_MO_FET_Rule(string value)
    {
        /*
        If DOFP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdo1pv_month).

        If DOFP_MO = 99 --> bfdcppcdo1pv_month = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value; 
    }

    private string DOFP_DY_FET_Rule(string value)
    {
        /*If DOFP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdo1pv_day).

        If DOFP_DY = 99 --> bfdcppcdo1pv_day = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string DOFP_YR_FET_Rule(string value)
    {
        /*If DOFP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdo1pv_year).

        If DOFP_YR = 9999 --> bfdcppcdo1pv_year = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string DOLP_MO_FET_Rule(string value)
    {
        /*If DOLP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdolpv_month).

        If DOLP_MO = 99 --> bfdcppcdolpv_month = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value; 
    }

    private string DOLP_DY_FET_Rule(string value)
    {
        /*If DOLP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdolpv_day).

        If DOLP_DY = 99 --> bfdcppcdolpv_day = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    private string DOLP_YR_FET_Rule(string value)
    {
        /*If DOLP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdolpv_year).

        If DOLP_YR = 9999 --> bfdcppcdolpv_year = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    private string CIGPN_Custom_FET_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month =  (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }
    
    private string CIGPN_Type_FET_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month = 9999 (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string CIGFN_Custom_FET_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }
    private string CIGFN_Type_FET_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string CIGSN_Type_FET_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }
    private string CIGSN_Custom_FET_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }

    private string CIGLN_Type_FET_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }
    private string CIGLN_Custom_FET_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }

    private string PDIAB_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PDIAB = Y --> bfdcprf_rfit_pregn = 0 Prepregnancy Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "0";

        return value;
    }
    private string GDIAB_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GDIAB = Y --> bfdcprf_rfit_pregn = 1 Gestational Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "1";

        return value;
    }
    private string PHYPE_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PHYPE = Y --> bfdcprf_rfit_pregn = 2 Prepregnacy Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "2";

        return value;
    }
    private string GHYPE_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GHYPE = Y --> bfdcprf_rfit_pregn = 3 Gestational Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "3";

        return value;
    }
    private string PPB_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPB = Y --> bfdcprf_rfit_pregn = 5 Previous Preterm Birth

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "5";

        return value;
    }
    private string PPO_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPO = Y --> bfdcprf_rfit_pregn = 6 Other Previous Poor Outcome

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "6";

        return value;
    }
    private string INFT_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT = Y --> bfdcprf_rfit_pregn = 7 Pregnancy Resulted from Infertility Treatment

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "7";

        return value;
    }
    private string PCES_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PCES = Y --> bfdcprf_rfit_pregn = 10 Mother had a Previous Cesarean Delivery

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "10";

        return value;
    }
    private string GON_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }
    private string SYPH_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        SYPH = Y --> bfdcp_ipotd_pregn = 3 Syphilis

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    private string HSV_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        HSV = Y --> bfdcp_ipotd_pregn = 11 Herpes Simplex [HSV]

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }
    private string CHAM_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        CHAM = Y --> bfdcp_ipotd_pregn = 6 Chlamydia

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }
    private string LM_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        LM = Y --> bfdcp_ipotd_pregn = 4 Listeria

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }
    private string GBS_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        GBS = Y --> bfdcp_ipotd_pregn = 8 Group B Streptococcus (fetal death(s) only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }
    private string CMV_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        CMV = Y --> bfdcp_ipotd_pregn = 5 Cytomegalovirus

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }
    private string B19_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        B19 = Y --> bfdcp_ipotd_pregn = 7 Parvovirus

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "7";

        return value;
    }
    private string TOXO_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        TOXO = Y --> bfdcp_ipotd_pregn = 9 Toxoplasmosis (fetal death(s) only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "9";

        return value;
    }
    private string OTHERI_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        OTHERI = Y --> bfdcp_ipotd_pregn = 14 Other

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "14";

        return value;
    }
    private string TLAB_FET_Rule(string value)
    {
        /*Y = Yes -> 1 Yes
        N = No -> 0 No
        U = Unknown -> 7777 Unknown
        X = Not Applicable -> 2 Not Applicable

        Map empty rows to 9999 (blank)
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            case "X":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }
    private string MTR_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "0";

        return value;
    }
    private string PLAC_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        PLAC = Y --> bfdcp_m_morbi = 3 Third or fourth degree perineal laceration

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "3";

        return value;
    }
    private string RUT_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        RUT = Y --> bfdcp_m_morbi = 5 Ruptured uterus

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "5";

        return value;
    }
    private string UHYS_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UHYS = Y --> bfdcp_m_morbi = 1 Unplanned hysterectomy

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "1";

        return value;
    }
    private string AINT_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        AINT = Y --> bfdcp_m_morbi = 4 Admission to intensive care unit

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "4";

        return value;
    }
    private string UOPR_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UOPR = Y --> bfdcp_m_morbi = 2 Unplanned operating room procedure following delivery

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "2";

        return value;
    }
    private string FWG_pound_FET_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        If BWG > 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        int.TryParse(value, out int numberParsed);

        if (numberParsed >= 9999)
            value = "";

        return value;
    }
    private string FWG_measure_FET_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        If BWG > 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        int.TryParse(value, out int numberParsed);

        if (numberParsed >= 9999)
            value = "9999";
        else
            value = "0";

        return value;
    }

    private string PLUR_Custom_FET_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "1";
                break;
            case "02":
            case "2":
                value = "2";
                break;
            case "03":
            case "3":
                value = "3";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "4";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    private string PLUR_sigt_FET_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "";
                break;
            case "02":
            case "2":
                value = "";
                break;
            case "03":
            case "3":
                value = "";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = value;
                break;
            default:
                value = "";
                break;
        }

        return value;
    }
    private string PLUR_gesta_FET_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "0";
                break;
            case "02":
            case "2":
                value = "1";
                break;
            case "03":
            case "3":
                value = "1";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "1";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    private string ANEN_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        ANEN = Y --> bcifs_c_anoma = 0 Anencephaly

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }
    private string MNSB_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        MNSB = Y --> bcifs_c_anoma = 9 Meningomyelocele or Spina bifida

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "9";

        return value;
    }
    private string CCHD_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CCHD = Y --> bcifs_c_anoma = 1 Cyanotic congenital heart disease

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }
    private string CDH_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CDH = Y --> bcifs_c_anoma = 10 Congenital diaphragmatic hernia

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "10";

        return value;
    }
    private string OMPH_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        OMPH = Y --> bcifs_c_anoma = 2 Omphalocele

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }
    private string GAST_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        GAST = Y --> bcifs_c_anoma = 11 Gastroschisis

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }
    private string LIMB_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        LIMB = Y --> bcifs_c_anoma = 3 Limb reduction defect (excluding congenital amputation and dwarfing syndromes)

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    private string CL_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CL = Y --> bcifs_c_anoma = 4 Cleft Lip with or without Cleft Palate

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }
    private string CP_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CP = Y --> bcifs_c_anoma = 12 Cleft palate alone

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "12";

        return value;
    }
    private string DOWT_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If DOWT = C --> bcifs_c_anoma = 6 Karyotype confirmed - Downs Syndrome
        If DOWT = P --> bcifs_c_anoma = 7 Karyotype pending - Downs Syndrome

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if(value == "C")
            value = "6";
        else if (value == "P")
            value = "7";

        return value;
    }
    private string CDIT_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If CDIT = C --> bcifs_c_anoma = 14 Karyotype confirmed - Suspected chromosomal disorder
        If CDIT = P --> bcifs_c_anoma = 15 Karyotype pending - Suspected chromosomal disorder

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "C")
            value = "14";
        else if (value == "P")
            value = "15";

        return value;
    }
    private string HYPO_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        HYPO = Y --> bcifs_c_anoma = 8 Hypospadias

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }
    private string MAGER_FET_Rule(string value, string dob_YR, string dob_MO, string dob_day, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, dob_day, dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }
    private string FAGER_FET_Rule(string value, string dob_YR, string dob_MO, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, "1", dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }
    private string EHYPE_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "4";

        return value;
    }
    private string INFT_DRG_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_DRG = Y --> bfdcprf_rfit_pregn = 8 Fertility Enhancing Drugs, Artificial Insemination or Intrauterine Insemination

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "8";

        return value;
    }
    private string INFT_ART_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_ART = Y --> bfdcprf_rfit_pregn = 9 Assisted Reproductive Technology (e.g. in vitro fertilization (IVF), gamete intrafallopian transfer (GIFT))

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "9";

        return value;
    }
    private string HSV1_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        HSV1 = Y --> bfdcp_ipotd_pregn = 12 Genital Herpes (fetal death only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "12";

        return value;
    }
    private string HIV_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        HIV = Y --> bfdcp_ipotd_pregn = 13 HIV (fetal death only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "13";

        return value;
    }
    private string FBPLACD_ST_TER_C_FET_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    private string FBPLACE_CNT_C_FET_Rule(string value)
    {
        /*Map to MMRIA field country listing 

        XX --> 9999 (blank)
        ZZ --> 9999 (blank)*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    private string FETHNIC_FET_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of FETHNIC1, FETHNIC2, FETHNIC3, FETHNIC4 to populate MMRIA field bfdcpdof_ifoh_origi.

            H --> bfdcpdof_ifoh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdof_ifoh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdof_ifoh_origi = 3 Yes, Cuban
        H --> bfdcpdof_ifoh_origi = 4, Yes, Other Spanish/Hispanic/Latino

            If FETHNIC1 = N and FETHNIC2 = N and FETHNIC3 = N and FETHNIC 4 = N --> bfdcpdof_ifoh_origi = 0 No, Not Spanish/Hispanic/Latino

            If FETHNIC1 = U and FETHNIC2 = U and FETHNIC3 = U and FETHNIC4 = U --> bfdcpdof_ifoh_origi = 7777 Unknown

            If FETHNIC1 = (empty) and FETHNIC2 = (empty) and FETHNIC3 = (empty) and FETHNIC4 = (empty) --> bfdcpdof_ifoh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }


    private string[] FRACE_FET_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from FRACE1 through FRACE15 to populate MMRIA multi-select field (bfdcpdofr_ro_fathe).

        FRACE1 = Y --> bfdcpdofr_ro_fathe = 0 White
        FRACE2 = Y --> bfdcpdofr_ro_fathe = 1 Black or African American
        FRACE3 = Y --> bfdcpdofr_ro_fathe = 2 American Indian or Alaska Native
        FRACE4 = Y --> bfdcpdofr_ro_fathe = 7 Asian Indian
        FRACE5 = Y --> bfdcpdofr_ro_fathe = 8 Chinese
        FRACE6 = Y --> bfdcpdofr_ro_fathe = 9 Filipino
        FRACE7 = Y --> bfdcpdofr_ro_fathe = 10 Japanese
        FRACE8 = Y --> bfdcpdofr_ro_fathe = 11 Korean
        FRACE9 = Y --> bfdcpdofr_ro_fathe = 12 Vietnamese
        FRACE10 = Y --> bfdcpdofr_ro_fathe = 13 Other Asian
        FRACE11 = Y --> bfdcpdofr_ro_fathe = 3 Native Hawaiian
        FRACE12 = Y --> bfdcpdofr_ro_fathe = 4 Guamanian or Chamorro
        FRACE13 = Y --> bfdcpdofr_ro_fathe = 5 Samoan
        FRACE14 = Y --> bfdcpdofr_ro_fathe = 6 Other Pacific Islander
        FRACE15 = Y --> bfdcpdofr_ro_fathe = 14 Other Race

        If every one of FRACE1 through FRACE15 is equal to "N", then bfdcpdofr_ro_fathe = 8888 (Race Not Specified)*/
        List<string> determinedValues = new List<string>();


        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }
        if(determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }
        return determinedValues.ToArray();
    }

    private string FRACE16_17_FET_Rule(string value16, string value17)
    {
        /*Combine FRACE16 and FRACE17 into one field (bfdcpdofr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE16 to MMRIA field.
        2. Transfer string verbatim from FRACE17 and add to same MMRIA field.
        3. If both FRACE16 and FRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value16) || string.IsNullOrWhiteSpace(value17)))
        {
            value = $"{value16}|{value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else
        {
            value = $"{value17}";
        }

        return value;
    }

    private string FRACE18_19_FET_Rule(string value18, string value19)
    {
        /*Combine FRACE18 and FRACE19 into one field (bfdcpdofr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE18 to MMRIA field.
        2. Transfer string verbatim from FRACE19 and add to same MMRIA field.
        3. If both FRACE18 and FRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value18) || string.IsNullOrWhiteSpace(value19)))
        {
            value = $"{value18}|{value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else
        {
            value = $"{value19}";
        }

        return value;
    }

    private string FRACE20_21_FET_Rule(string value20, string value21)
    {
        /*Combine FRACE20 and FRACE21 into one field (bfdcpdofr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE20 to MMRIA field.
        2. Transfer string verbatim from FRACE21 and add to same MMRIA field.
        3. If both FRACE20 and FRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value20) || string.IsNullOrWhiteSpace(value21)))
        {
            value = $"{value20}|{value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else
        {
            value = $"{value21}";
        }

        return value;
    }

    private string FRACE22_23_FET_Rule(string value22, string value23)
    {
        /*Combine FRACE22 and FRACE23 into one field (bfdcpdofr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE22 to MMRIA field.
        2. Transfer string verbatim from FRACE23 and add to same MMRIA field.
        3. If both FRACE22 and FRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value22) || string.IsNullOrWhiteSpace(value23)))
        {
            value = $"{value22}|{value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else
        {
            value = $"{value23}";
        }

        return value;
    }

    private string FET_METHNIC_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of METHNIC1, METHNIC2, METHNIC3, METHNIC4 to populate MMRIA field bfdcpdom_ioh_origi.

        H --> bfdcpdom_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdom_ioh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdom_ioh_origi = 3 Yes, Cuban
        H --> bfdcpdom_ioh_origi = 4 Yes, Other Spanish/Hispanic/Latino

        If METHNIC1 = N and METHNIC2 = N and METHNIC3 = N and METHNIC 4 = N --> bfdcpdom_ioh_origi = 0 No, Not Spanish/Hispanic/Latino

        If METHNIC1 = U and METHNIC2 = U and METHNIC3 = U and METHNIC4 = U --> bfdcpdom_ioh_origi = 7777 Unknown

        If METHNIC1 = (empty) and METHNIC2 = (empty) and METHNIC3 = (empty) and METHNIC4 = (empty) --> bfdcpdom_ioh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }

    #endregion

    #endregion

    private mmria.common.model.couchdb.case_view_response GetCaseView
    (

        mmria.common.couchdb.DBConfigurationDetail db_info,
        string search_key
    )
    {
        string request_string  = $"{db_info.url}/{db_info.prefix}mmrds/_design/sortable/_view/by_last_name?skip=0&limit=100000&startkey=\"{search_key.ToLower()}\"&endkey=\"{search_key.ToUpper()}\"";

        try
        {

            var case_view_curl = new mmria.getset.cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
            case_view_curl.SetTimeout(300 * 1000);
            string responseFromServer = case_view_curl.execute();

            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);


            string key_compare = search_key.ToLower().Trim(new char[] { '"' });

            mmria.common.model.couchdb.case_view_response result = new mmria.common.model.couchdb.case_view_response();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {
                bool add_item = false;

                if (is_matching_search_text(cvi.value.last_name, key_compare))
                {
                    add_item = true;
                }

                if (add_item)
                {
                    result.rows.Add(cvi);
                }

            }


            result.total_rows = result.rows.Count;
            result.rows = result.rows.Skip(0).Take(100000).ToList();

            return result;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"BatchItemProcessor GetCaseView\nurl: {request_string}\n\nerror:\n{ex}");

        }


        return null;
    }

    public System.Dynamic.ExpandoObject GetCaseById(mmria.common.couchdb.DBConfigurationDetail db_info, string case_id)
    {
        try
        {
            string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_all_docs?include_docs=true";

            if (!string.IsNullOrWhiteSpace(case_id))
            {
                request_string = $"{db_info.url}/{db_info.prefix}mmrds/{case_id}";
                var case_curl = new mmria.getset.cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
                string responseFromServer = case_curl.execute();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseFromServer);

                return result;

            }

        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"BatchItemProcessor.GetCaseById 11078\n{ex}");
        }

        return null;
    }

    private bool is_matching_search_text(string p_val1, string p_val2)
    {
        var result = false;

        if
        (
            !string.IsNullOrWhiteSpace(p_val1) &&
            //p_val1.Length > 3 &&
            (
                p_val2.IndexOf(p_val1, StringComparison.OrdinalIgnoreCase) > -1 ||
                p_val1.IndexOf(p_val2, StringComparison.OrdinalIgnoreCase) > -1
            )
        )
        {
            result = true;
        }

        return result;
    }

    //CALCULATE GESTATIONAL AGE AT BIRTH ON BC (LMP)
    /*
    path=birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation
CALCULATE_GESTATIONAL_AGE_AT_BIRTH_ON_BC
        int event_year = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year);
        int event_month = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month);
        int event_day = parseInt(g_data.birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day);
        int lmp_year = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.year);
        int lmp_month = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.month);
        int lmp_day = parseInt(g_data.birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.day);
    event=onfocus
    */
    (string weeks, string days) CALCULATE_GESTATIONAL_AGE_AT_BIRTH_ON_BC
    (
        migrate.C_Get_Set_Value.get_value_result p_event_year_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_event_month_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_event_day_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_lmp_year_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_lmp_month_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_lmp_day_get_result
    ) 
    {
        var result = ("","");


        bool is_valid_date(int year, int month, int day)
        {

            if
            (
                year < 1920 ||
                month == -1 ||
                day == -1 ||
                year > 3000 ||
                month > 12 ||
                day > 31
            )
            {
                return false;
            }




            var months31 = new HashSet<int>()
            {
                    1,
                    3,
                    5,
                    7,
                    8,
                    10,
                    12
            };
            // months with 31 days
            var months30 = new HashSet<int>()
            {
                    4,
                    6,
                    9,
                    11
            };
            // months with 30 days
            var months28 = new HashSet<int>(){2};
            // the only month with 28 days (29 if year isLeap)
            var isLeap = year % 4 == 0 && year % 100 != 0 || year % 400 == 0;
            var valid = 
                months31.Contains(month) && day <= 31 || 
                months30.Contains(month)  && day <= 30 || 
                months28.Contains(month) && day <= 28 || 
                months28.Contains(month) && day <= 29 && isLeap;
            return valid;
        }
        int convert_from_dynamic_to_int(dynamic p_value)
        {
            int result = -1;
            if(p_value != null)
            {
                int.TryParse(p_value.ToString(), out result);
            }
            return result;
        }
        int calc_days(DateTime p_start_date, DateTime p_end_date) 
        {
            int days = (int) (p_end_date - p_start_date).TotalDays;
            return days;
        }

        (int weeks, int days) calc_ga_lmp(DateTime p_start_date, DateTime p_end_date) 
        {
            var weeks = calc_days(p_start_date, p_end_date) / 7;
            var days = calc_days(p_start_date, p_end_date) % 7;
            return (weeks, days);
        }

        object p_event_year_dynamic;
        object p_event_month_dynamic;
        object p_event_day_dynamic;
        object p_lmp_year_dynamic;
        object p_lmp_month_dynamic;
        object p_lmp_day_dynamic;


        if
        (
            p_event_year_get_result.is_error ||
            p_event_month_get_result.is_error ||
            p_event_day_get_result.is_error ||
            p_lmp_year_get_result.is_error ||
            p_lmp_month_get_result.is_error ||
            p_lmp_day_get_result.is_error
        )
        {
            return result;
        }
        else
        {
            p_event_year_dynamic = p_event_year_get_result.result;
            p_event_month_dynamic = p_event_month_get_result.result;
            p_event_day_dynamic = p_event_day_get_result.result;
            p_lmp_year_dynamic = p_lmp_year_get_result.result;
            p_lmp_month_dynamic = p_lmp_month_get_result.result;
            p_lmp_day_dynamic = p_lmp_day_get_result.result;
        }


        int event_year = convert_from_dynamic_to_int(p_event_year_dynamic);
        int event_month = convert_from_dynamic_to_int(p_event_month_dynamic);
        int event_day = convert_from_dynamic_to_int(p_event_day_dynamic);
        int lmp_year = convert_from_dynamic_to_int(p_lmp_year_dynamic);
        int lmp_month = convert_from_dynamic_to_int(p_lmp_month_dynamic);
        int lmp_day = convert_from_dynamic_to_int(p_lmp_day_dynamic);
        
        if 
        (
            is_valid_date(event_year, event_month, event_day) && 
            is_valid_date(lmp_year, lmp_month, lmp_day)
        ) 
        {
            try
            {
                var lmp_date = new DateTime(lmp_year, lmp_month, lmp_day);
                var event_date = new DateTime(event_year, event_month, event_day);

                var int_result = calc_ga_lmp(lmp_date, event_date);
                if(int_result.weeks > -1 && int_result.days > -1)
                {
                    result = (int_result.weeks.ToString(), int_result.days.ToString());
                }
            }
            catch(Exception)
            {

            }

        }

        return result;
    }

    mmria.common.niosh.NioshResult get_niosh_codes(string p_occupation, string p_industry)
    {
        var result = new mmria.common.niosh.NioshResult();
        var builder = new StringBuilder();
        builder.Append("https://wwwn.cdc.gov/nioccs/IOCode?n=3");
        var has_occupation = false;
        var has_industry = false;

        if(!string.IsNullOrWhiteSpace(p_occupation))
        {
            has_occupation = true;
            builder.Append($"&o={p_occupation}");
        }

        if(!string.IsNullOrWhiteSpace(p_industry))
        {
            has_industry = true;
            builder.Append($"&i={p_industry}");
        }

        


        if(has_occupation || has_industry)
        {
            var niosh_url = builder.ToString();

            var niosh_curl = new mmria.getset.cURL("GET", null, niosh_url, null);

            try
            {
                string responseFromServer = niosh_curl.execute();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.niosh.NioshResult>(responseFromServer);
            }
            catch
            {
                result.is_error = true;
            }
            
        }
        //{"Industry": [{"Code": "611110","Title": "Elementary and Secondary Schools","Probability": "9.999934E-001"},{"Code": "611310","Title": "Colleges, Universities, and Professional Schools","Probability": "2.598214E-006"},{"Code": "009990","Title": "Insufficient information","Probability": "2.312557E-006"}],"Occupation": [{"Code": "00-9900","Title": "Insufficient Information","Probability": "9.999897E-001"},{"Code": "11-9032","Title": "Education Administrators, Elementary and Secondary School","Probability": "6.550550E-006"},{"Code": "53-3022","Title": "Bus Drivers, School or Special Client","Probability": "4.932875E-007"}],"Scheme": "NAICS 2012 and SOC 2010"}
        return result;

    }


    string get_year_and_quarter(object p_value)
    {
        var result = string.Empty;
        
        if(p_value != null && !string.IsNullOrWhiteSpace(p_value.ToString()))
        try
        {
    
            if(p_value is DateTime)
            {
                var date_time = (DateTime) p_value;
                result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
            }
            else
            {
                var date_string = p_value.ToString();
                if(date_string.IndexOf("-") > -1)
                {
                    var int_array = date_string.Split("-");
                    if(int_array.Length == 3)
                    {
                        DateTime date_time = new DateTime(int.Parse(int_array[0]), int.Parse(int_array[1]), int.Parse(int_array[2]));
                        result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                    }
                    else
                    {
                        DateTime date_time = DateTime.ParseExact
                        (
                            date_string,
                            "yyyy-MM-dd", //"MM/dd/yyyy", 
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                    }
                }
                else if(date_string.IndexOf("/") > -1)
                {
                    DateTime date_time = DateTime.ParseExact
                    (
                        date_string,
                        "MM/dd/yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                }
                else
                {
                    DateTime date_time = DateTime.ParseExact
                    (
                        date_string,
                        "yyyy-MM-dd", //"MM/dd/yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                }
            }
        
        }
        catch
        {
            // do nothing
        }

        return result;
    }





	public (string, mmria.common.cvs.tract_county_result) GetCVSData
    (
        string c_geoid,
		string t_geoid,
		string year,
        mmria.common.couchdb.ConfigurationSet ConfigDB
    ) 
    { 

        mmria.common.cvs.tract_county_result result = null;
        var response_string = string.Empty;

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {
            
            var get_all_data_body = new mmria.common.cvs.get_all_data_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],
                payload = new()
                {
                    
                    c_geoid = c_geoid,
                    t_geoid = t_geoid,
                    year = year
                }
            };

            var body_text =  System.Text.Json.JsonSerializer.Serialize(get_all_data_body);
            var get_all_data_curl = new mmria.getset.cURL("POST", null, base_url, body_text);

            response_string = get_all_data_curl.execute();
            System.Console.WriteLine(response_string);

            result = System.Text.Json.JsonSerializer.Deserialize<mmria.common.cvs.tract_county_result>(response_string);
        
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/

            return ($"Status: {ex.Status} {ex.Message} {response_string}", null);
        }

        return ("success", result);
    }


    public List<int> CVS_Get_Valid_Years(mmria.common.couchdb.ConfigurationSet ConfigDB) 
    { 
        var result = new List<int>()
		{
			2010,
			2011,
			2012,
			2013,
			2014,
			2015,
			2016,
			2017,
			2018,
			2019,
			2020
		};

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {


			var get_year_body = new mmria.common.cvs.get_year_post_body()
			{
				id = ConfigDB.name_value["cvs_api_id"],
				secret = ConfigDB.name_value["cvs_api_key"],
				payload = new()
			};

			var body_text =  System.Text.Json.JsonSerializer.Serialize(get_year_body);
			var get_year_curl = new mmria.getset.cURL("POST", null, base_url, body_text);
			string get_year_response = get_year_curl.execute();
			result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>> (get_year_response);

			System.Console.WriteLine(get_year_response);

    
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController Get Year POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/
        }


        return result;
    }	

    bool is_result_quality_in_need_of_checking(mmria.common.cvs.tract_county_result val)
	{

		var over_all_result = false;
		var tract_result = false;
		var county_result = false;

		const float tract_total = 11F;
		const float county_total = 26F;

		float tract_zero_count = 0F;
		float county_zero_count = 0F;

		const float _30_percent_correct = .3F;

		if
		(
			val.tract.pctMOVE == 0  && //cvs_pctmove_tract
			val.tract.pctNOIns_Fem == 0 && //cvs_pctnoins_fem_tract		
			val.county.pctNoVehicle == 0 && //cvs_pctnovehicle_county
			val.tract.pctNoVehicle == 0 && //cvs_pctnovehicle_tract
			val.tract.pctOWNER_OCC == 0 //cvs_pctowner_occ_tract
		)
		{
			over_all_result = true;
		}


		if(val.tract.pctNOIns_Fem == 0) tract_zero_count += 1;
		if(val.tract.MEDHHINC == 0) tract_zero_count += 1;
		if(val.tract.pctNoVehicle == 0) tract_zero_count += 1;
		if(val.tract.pctMOVE == 0) tract_zero_count += 1;
		if(val.tract.pctSPHH == 0) tract_zero_count += 1;
		if(val.tract.pctOVERCROWDHH == 0) tract_zero_count += 1;
		if(val.tract.pctOWNER_OCC == 0) tract_zero_count += 1;
		if(val.tract.pct_less_well == 0) tract_zero_count += 1;
		if(val.tract.NDI_raw == 0) tract_zero_count += 1;
		if(val.tract.pctPOV == 0) tract_zero_count += 1;
		if(val.tract.ICE_INCOME_all == 0) tract_zero_count += 1;



		if(val.county.MDrate == 0) county_zero_count += 1;
		if(val.county.pctNOIns_Fem == 0) county_zero_count += 1;
		if(val.county.pctNoVehicle == 0) county_zero_count += 1;
		if(val.county.pctMOVE == 0) county_zero_count += 1;
		if(val.county.pctSPHH == 0) county_zero_count += 1;
		if(val.county.pctOVERCROWDHH == 0) county_zero_count += 1;
		if(val.county.pctOWNER_OCC == 0) county_zero_count += 1;
		if(val.county.pct_less_well == 0) county_zero_count += 1;
		if(val.county.NDI_raw == 0) county_zero_count += 1;
		if(val.county.pctPOV == 0) county_zero_count += 1;
		if(val.county.ICE_INCOME_all == 0) county_zero_count += 1;
		if(val.county.MEDHHINC == 0) county_zero_count += 1;
		if(val.county.pctOBESE == 0) county_zero_count += 1;
		if(val.county.FI == 0) county_zero_count += 1;
		if(val.county.CNMrate == 0) county_zero_count += 1;
		if(val.county.OBGYNrate == 0) county_zero_count += 1;
		if(val.county.rtTEENBIRTH == 0) county_zero_count += 1;
		if(val.county.rtSTD == 0) county_zero_count += 1;
		if(val.county.rtMHPRACT == 0) county_zero_count += 1;
		if(val.county.rtDRUGODMORTALITY == 0) county_zero_count += 1;
		if(val.county.rtOPIOIDPRESCRIPT == 0) county_zero_count += 1;
		if(val.county.SocCap == 0) county_zero_count += 1;
		if(val.county.rtSocASSOC == 0) county_zero_count += 1;
		if(val.county.pctHOUSE_DISTRESS == 0) county_zero_count += 1;
		if(val.county.rtVIOLENTCR_ICPSR == 0) county_zero_count += 1;
		if(val.county.isolation == 0) county_zero_count += 1;


		if(tract_zero_count / tract_total < _30_percent_correct) tract_result = true;

		if(county_zero_count / county_total < _30_percent_correct) county_result = true;


		return over_all_result || tract_result || county_result;
	}

}

