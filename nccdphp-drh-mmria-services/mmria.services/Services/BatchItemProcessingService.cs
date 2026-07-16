using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Helper;
using static mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper;
using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.Case.DAL;
using mmria.common.SharedLibraries.MetadataVersion.DAL;

namespace RecordsProcessor_Worker.Services;

public sealed class BatchItemProcessingService
{
    Dictionary<string, mmria.common.metadata.value_node[]> lookup;
    private static readonly Dictionary<string, string> StateFipsToPostalCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["01"] = "AL",
        ["02"] = "AK",
        ["04"] = "AZ",
        ["05"] = "AR",
        ["06"] = "CA",
        ["08"] = "CO",
        ["09"] = "CT",
        ["10"] = "DE",
        ["11"] = "DC",
        ["12"] = "FL",
        ["13"] = "GA",
        ["15"] = "HI",
        ["16"] = "ID",
        ["17"] = "IL",
        ["18"] = "IN",
        ["19"] = "IA",
        ["20"] = "KS",
        ["21"] = "KY",
        ["22"] = "LA",
        ["23"] = "ME",
        ["24"] = "MD",
        ["25"] = "MA",
        ["26"] = "MI",
        ["27"] = "MN",
        ["28"] = "MS",
        ["29"] = "MO",
        ["30"] = "MT",
        ["31"] = "NE",
        ["32"] = "NV",
        ["33"] = "NH",
        ["34"] = "NJ",
        ["35"] = "NM",
        ["36"] = "NY",
        ["37"] = "NC",
        ["38"] = "ND",
        ["39"] = "OH",
        ["40"] = "OK",
        ["41"] = "OR",
        ["42"] = "PA",
        ["44"] = "RI",
        ["45"] = "SC",
        ["46"] = "SD",
        ["47"] = "TN",
        ["48"] = "TX",
        ["49"] = "UT",
        ["50"] = "VT",
        ["51"] = "VA",
        ["53"] = "WA",
        ["54"] = "WV",
        ["55"] = "WI",
        ["56"] = "WY",
        ["72"] = "PR"
    };
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
        {"ACKN","birth_fetal_death_certificate_parent/demographic_of_mother/if_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital"},
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
    private string config_timer_user_name = null;
    private string config_timer_value = null;

    mmria.common.couchdb.DBConfigurationDetail item_db_info;

    string geocode_api_key =  "";

    private Dictionary<string, string> StateDisplayToValue;

    private string location_of_residence_latitude = null;
    private string location_of_residence_longitude = null;
    private string facility_of_delivery_location_latitude = null;
    private string facility_of_delivery_location_longitude = null;

    private string death_certificate_place_of_last_residence_latitude = null;
    private string death_certificate_place_of_last_residence_longitude = null;
    private string death_certificate_address_of_death_latitude = null;
    private string death_certificate_address_of_death_longitude = null;

    private mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private System.Net.Http.HttpClient _externalHttpClient;
    private MMRIAServicesManager _mmriaServicesManager;
    private ICaseRepository _caseRepository;
    public BatchItemProcessingService(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = new MMRIAServicesManager(new MMRIAServicesDAL(_couchDbHttpClient, new mmria.common.SharedLibraries.SystemConfig.DAL.SystemConfigDAL(_couchDbHttpClient), new MetadataVersionDAL(_couchDbHttpClient)), _couchDbHttpClient);
        _caseRepository = new CaseDAL(_couchDbHttpClient);
        var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
        _externalHttpClient = httpClientFactory.CreateClient("external");
    }

    public async System.Threading.Tasks.Task<(mmria.common.ije.BatchItemComplete completion, mmria.common.ije.BatchItem batchItem)> Process_Message(mmria.common.ije.StartBatchItemMessage message)
    {

        config_timer_user_name = mmria.services.vitalsimport.Program.timer_user_name;
        config_timer_value = mmria.services.vitalsimport.Program.timer_value;

        mmria.common.couchdb.ConfigurationSet db_config_set = mmria.services.vitalsimport.Program.DbConfigSet;
        item_db_info = db_config_set.detail_list[message.host_state];
        geocode_api_key = db_config_set.name_value["geocode_api_key"];

        var mor_field_set = mor_get_header(message.mor);

        //get parent header set fet/nat

        var nat_field_set = nat_get_header(message.nat);

        var fet_field_set = fet_get_header(message.fet);


        string metadata_url = $"{mmria.services.vitalsimport.Program.couchdb_url}/metadata/version_specification-{db_config_set.name_value["metadata_version"]}/metadata";
        string metadata_response = await _couchDbHttpClient.ExecuteAsync("GET", metadata_url, null, config_timer_user_name, config_timer_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_response);

        lookup = get_look_up(metadata);

        StateDisplayToValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in lookup["lookup/state"])
        {
            StateDisplayToValue.Add(kvp.display, kvp.value);
        }

        var case_present_result = await _mmriaServicesManager.IsCaseAlreadyPresent
        (
            item_db_info,
            message.host_state,
            mor_field_set,
            IJE_to_MMRIA_Path
        );

        var is_case_already_present = case_present_result.is_case_already_present;
        string mmria_id = case_present_result.mmria_id;

        var gs = new migrate.C_Get_Set_Value(new System.Text.StringBuilder());

        string record_id = case_present_result.record_id;


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
            // Notify BatchProcessor of completion
            var completion = new mmria.common.ije.BatchItemComplete()
            {
                cdc_unique_id = message.cdc_unique_id,
                success = true,
                error_message = null
            };

            return (completion, result);
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

            // Note: Intermediate status not sent - final status sent at completion


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
                mor_field_set["ZIP9_D"],
                mor_field_set["DOD_YR"]), 
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
                    mor_field_set["ZIP9_R"],
                    mor_field_set["DOD_YR"]
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


                var Valid_CVS_Years = await MMRIAServicesHelper.CVS_Get_Valid_Years(db_config_set, _externalHttpClient);

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


                          var (cvs_response_status, tract_county_result) = await MMRIAServicesHelper.GetCVSData
                    (
                        state_county_fips,
                        t_geoid,
                        calculated_year_of_death.ToString(),
                              db_config_set,
                              _externalHttpClient
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
                                                    MMRIAServicesHelper.is_result_quality_in_need_of_checking(tract_county_result)
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
                    if (int.TryParse(field_set["MARN"], out int nat_marn_val) && field_set["MARN"] != "9999")
                        gs.set_objectvalue(Parent_NAT_IJE_to_MMRIA_Path["MARN"], nat_marn_val, new_case);
                    else
                        gs.set_value(Parent_NAT_IJE_to_MMRIA_Path["MARN"], field_set["MARN"], new_case);
                    if (int.TryParse(field_set["ACKN"], out int nat_ackn_val) && field_set["ACKN"] != "9999")
                        gs.set_objectvalue(Parent_NAT_IJE_to_MMRIA_Path["ACKN"], nat_ackn_val, new_case);
                    else
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
                            field_set["ZIPCODE"],
                            mor_field_set["DOD_YR"]
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
                    if (int.TryParse(field_set["MARN"], out int fet_marn_val) && field_set["MARN"] != "9999")
                        gs.set_objectvalue(Parent_FET_IJE_to_MMRIA_Path["MARN"], fet_marn_val, new_case);
                    else
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
                            field_set["ZIPCODE"],
                            mor_field_set["DOD_YR"]
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
                            field_set["ZIPCODE_D"],
                            mor_field_set["DOD_YR"]
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

                var (gestation_weeks, gestation_days) =  MMRIAServicesHelper.CALCULATE_GESTATIONAL_AGE_AT_BIRTH_ON_BC
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
gs.set_value("addquarter", MMRIAServicesHelper.get_year_and_quarter(DateTime.Now), new_case);



string primary_occupation = null;
string business_industry = null;
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

var niosh_result = await MMRIAServicesHelper.get_niosh_codes
(
    primary_occupation,
    business_industry,
    _couchDbHttpClient
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
niosh_result = await MMRIAServicesHelper.get_niosh_codes
(
    primary_occupation,
    business_industry,
    _couchDbHttpClient
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

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(new_case, settings);

            var document_put_response = new mmria.common.model.couchdb.document_put_response();
            try
            {
                var responseFromServer = await _caseRepository.PutCaseDocumentJsonAsync(mmria_id, object_string, db_info);
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
            // Notify BatchProcessor of completion
            var completion = new mmria.common.ije.BatchItemComplete()
            {
                cdc_unique_id = message.cdc_unique_id,
                success = finished.Status != mmria.common.ije.BatchItem.StatusEnum.ImportFailed,
                error_message = finished.Status == mmria.common.ije.BatchItem.StatusEnum.ImportFailed ? finished.StatusDetail : null
            };

            return (completion, finished);

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

    private GeocodeTuple get_geocode_info(string street, string city, string state, string zip, string year)
    {

        var result = new GeocodeTuple();

        if (!string.IsNullOrEmpty(state))
        {
            var check_state = state.Split("-");
            state = check_state[0];
        }

        var TAMUGeocoder = new mmria.services.vitalsimport.Utilities.TAMUGeoCode();

        var response = TAMUGeocoder.execute(geocode_api_key, street, city, state, zip, year);
        
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

    #region Rules Section

    #region MOR Rules

    private string STINJURY_Rule(string value)
    {
        return NormalizeStateLookupValue(value);
    }

    private string STATETEXT_D_Rule(string value)
    {
        return NormalizeStateLookupValue(value);
    }

    private string STATEC_Rule(string value)
    {
        return NormalizeStateLookupValue(value);
    }

    private string NormalizeStateLookupValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "9999";
        }

        var normalized = value.Trim();

        if (normalized.Equals("XX", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("ZZ", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("9999", StringComparison.OrdinalIgnoreCase))
        {
            return "9999";
        }

        if (StateDisplayToValue != null && StateDisplayToValue.TryGetValue(normalized, out var displayValue))
        {
            return displayValue;
        }

        if (normalized.Length == 1 && char.IsDigit(normalized[0]))
        {
            normalized = normalized.PadLeft(2, '0');
        }

        if (StateFipsToPostalCode.TryGetValue(normalized, out var postalValue))
        {
            return postalValue;
        }

        return normalized.ToUpperInvariant();
    }

    #endregion

    #region NAT Rules
    #endregion

    #region FET Rules

    #endregion

    #endregion

}


