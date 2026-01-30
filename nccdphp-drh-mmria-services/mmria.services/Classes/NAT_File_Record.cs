using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace mmria.services.vitalsimport;

public sealed class NAT_File_Record
{
    public static int Record_Length = 4000;

    [Layout(0, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", "bfdcpfodddod_year")]
    [MMRIA_Path("/birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "bcifsri_do_deliv")]
    [IJE_Name("IDOB_YR")]
    public string IDOB_YR;

    [Layout(6, 6)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/record_identification/state_file_number", "bcifsri_sf_numbe")]
    [IJE_Name("FILENO")]
    public string FILENO;

    [Layout(13, 12)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/record_identification/local_file_number", "bcifsri_lf_numbe")]
    [IJE_Name("AUXNO")]
    public string AUXNO;

    [Layout(25, 4)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/record_identification/time_of_delivery", "bcifsri_to_deliv")]
    [IJE_Name("TB")]
    public string TB;

    [Layout(30, 2)]
    [MMRIA_Path("/birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "bcifsri_do_deliv")]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", "bfdcpfodddod_month")]
    [IJE_Name("IDOB_MO")]
    public string IDOB_MO;

    [Layout(32, 2)]
    [MMRIA_Path("/birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "bcifsri_do_deliv ")]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", "bfdcpfodddod_day")]
    [IJE_Name("IDOB_DY")]
    public string IDOB_DY;

    [Layout(38, 12)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_npi_number", "bfdcpfodd_fn_numbe")]
    [IJE_Name("FNPI")]
    public string FNPI;

    [Layout(54, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year", "bfdcpdomdob_year")]
    [IJE_Name("MDOB_YR")]
    public string MDOB_YR;

    [Layout(58, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month", "bfdcpdomdob_month")]
    [IJE_Name("MDOB_MO")]
    public string MDOB_MO;

    [Layout(60, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day", "bfdcpdomdob_day")]
    [IJE_Name("MDOB_DY")]
    public string MDOB_DY;

    [Layout(80, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/year", "bfdcpdofdob_year")]
    [IJE_Name("FDOB_YR")]
    public string FDOB_YR;

    [Layout(84, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/month", "bfdcpdofdob_month")]
    [IJE_Name("FDOB_MO")]
    public string FDOB_MO;

    [Layout(90, 1)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/demographic_of_mother/mother_married", "bfdcpdom_m_marri")]
    [IJE_Name("MARN")]
    public string MARN;

    [Layout(91, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital", "bfdcpdom_Imnmhpabsit_hospi")]
    [IJE_Name("ACKN")]
    public string ACKN;

    [Layout(92, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/education_level", "bfdcpdom_e_level")]
    [IJE_Name("MEDUC")]
    public string MEDUC;

    [Layout(98, 20)]
    [MMRIA_Path("New MMRIA Field: (for mother) Other Hispanic, specify (add MMRIA path when available)", "bfdcpdom_ioh_origi_othsp")]
    [IJE_Name("METHNIC5")]
    public string METHNIC5;

    [Layout(421, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/education_level", "bfdcpdof_e_level")]
    [IJE_Name("FEDUC")]
    public string FEDUC;

    [Layout(427, 20)]
    [MMRIA_Path("New MMRIA Field: (for father) Other Hispanic, specify (add MMRIA path when available)", "bfdcpdof_ifoh_origi _othsp")]
    [IJE_Name("FETHNIC5")]
    public string FETHNIC5;

    [Layout(750, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_type", "bfdcpfodd_a_type")]
    [IJE_Name("ATTEND")]
    public string ATTEND;

    [Layout(751, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_mother_transferred", "bfdcpfodd_wm_trans")]
    [IJE_Name("TRAN")]
    public string TRAN;

    [Layout(768, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/number_of_visits", "bfdcppc_no_visit")]
    [IJE_Name("NPREV")]
    public string NPREV;

    [Layout(771, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/height_feet", "bfdcpmb_h_feet")]
    [IJE_Name("HFT")]
    public string HFT;

    [Layout(772, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/height_inches", "bfdcpmb_h_inche")]
    [IJE_Name("HIN")]
    public string HIN;

    [Layout(775, 3)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight", "bfdcpmb_pp_weigh")]
    [IJE_Name("PWGT")]
    public string PWGT;

    [Layout(779, 3)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery", "bfdcpmb_wa_deliv")]
    [IJE_Name("DWGT")]
    public string DWGT;

    [Layout(783, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/was_wic_used", "bfdcppc_ww_used")]
    [IJE_Name("WIC")]
    public string WIC;

    [Layout(784, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/now_living", "bfdcpph_n_livin")]
    [IJE_Name("PLBL")]
    public string PLBL;

    [Layout(786, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/now_dead", "bfdcpph_n_dead")]
    [IJE_Name("PLBD")]
    public string PLBD;

    [Layout(788, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/pregnancy_history/other_outcomes", "bfdcpph_o_outco")]
    [IJE_Name("POPO")]
    public string POPO;

    [Layout(790, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month", "bfdcpphdollb_month")]
    [IJE_Name("MLLB")]
    public string MLLB;

    [Layout(792, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year", "bfdcpphdollb_year")]
    [IJE_Name("YLLB")]
    public string YLLB;

    [Layout(796, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month", "bfdcpphdoloo_month")]
    [IJE_Name("MOPO")]
    public string MOPO;

    [Layout(798, 4)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year", "bfdcpphdoloo_year")]
    [IJE_Name("YOPO")]
    public string YOPO;

    [Layout(810, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/principal_source_of_payment_for_this_delivery ", "bfdcppc_psopft_deliv")]
    [IJE_Name("PAY")]
    public string PAY;

    [Layout(811, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year", "bfdcppcdolnm_year")]
    [IJE_Name("DLMP_YR")]
    public string DLMP_YR;

    [Layout(815, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month", "bfdcppcdolnm_month")]
    [IJE_Name("DLMP_MO")]
    public string DLMP_MO;

    [Layout(817, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day", "bfdcppcdolnm_day")]
    [IJE_Name("DLMP_DY")]
    public string DLMP_DY;

    [Layout(828, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/risk_factors/number_of_c_sections", "bfdcprf_noc_secti")]
    [IJE_Name("NPCES")]
    public string NPCES;

    [Layout(853, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_forceps_attempted_but_unsuccessful", "bcifsmod_wdwfab_unsuc")]
    [IJE_Name("ATTF")]
    public string ATTF;

    [Layout(854, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_vacuum_extration_attempted_but_unsuccessful", "bcifsmod_wdwveab_unsuc")]
    [IJE_Name("ATTV")]
    public string ATTV;

    [Layout(855, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/fetal_delivery", "bcifsmod_f_deliv")]
    [IJE_Name("PRES")]
    public string PRES;

    [Layout(856, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery", "bcifsmod_framo_deliv")]
    [IJE_Name("ROUT")]
    public string ROUT;

    [Layout(869, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/prenatal_care/obsteric_estimate_of_gestation", "bfdcppc_oeo_gesta")]
    [IJE_Name("OWGEST")]
    public string OWGEST;

    [Layout(872, 2)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_5", "bcifsbadas_m_5")]
    [IJE_Name("APGAR5")]
    public string APGAR5;

    [Layout(874, 2)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_10", "bcifsbadas_m_10")]
    [IJE_Name("APGAR10")]
    public string APGAR10;

    [Layout(878, 2)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/birth_order", "bcifs_b_order")]
    [IJE_Name("SORD")]
    public string SORD;

    [Layout(908, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/biometrics_and_demographics/was_infant_transferred_within_24_hours", "bcifsbad_witw2_hours")]
    [IJE_Name("ITRAN")]
    public string ITRAN;

    [Layout(909, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_living_at_time_of_report", "bcifsbad_iilato_repor")]
    [IJE_Name("ILIV")]
    public string ILIV;

    [Layout(910, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_being_breastfed_at_discharge", "bcifsbad_iibba_disch")]
    [IJE_Name("BFED")]
    public string BFED;

    [Layout(1157, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_location/county", "bfdcpfodl_count")]
    [IJE_Name("BIRTH_CO")]
    public string BIRTH_CO;

    [Layout(1182, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_location/city", "bfdcpfodl_city")]
    [IJE_Name("BRTHCITY")]
    public string BRTHCITY;

    [Layout(1232, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name", "bfdcpfodd_f_name")]
    [IJE_Name("HOSP")]
    public string HOSP;

    [Layout(1282, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/first_name", "bfdcpri_f_name")]
    [IJE_Name("MOMFNAME")]
    public string MOMFNAME;

    [Layout(1332, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/middle_name", "bfdcpri_m_name")]
    [IJE_Name("MOMMIDDL")]
    public string MOMMIDDL;

    [Layout(1382, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/last_name", "bfdcpri_l_name")]
    [IJE_Name("MOMLNAME")]
    public string MOMLNAME;

    [Layout(1539, 50)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/record_identification/maiden_name", "bfdcpri_m_name_244")]
    [IJE_Name("MOMMAIDN")]
    public string MOMMAIDN;

    [Layout(1596, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("STNUM")]
    public string STNUM;

    [Layout(1606, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("PREDIR")]
    public string PREDIR;

    [Layout(1616, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("STNAME")]
    public string STNAME;

    [Layout(1644, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("STDESIG")]
    public string STDESIG;

    [Layout(1654, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("POSTDIR")]
    public string POSTDIR;

    [Layout(1664, 7)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/apartment", "bfdcplor_apart")]
    [IJE_Name("UNUM")]
    public string UNUM;

    [Layout(1721, 9)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/zip_code", "bfdcplor_z_code")]
    [IJE_Name("ZIPCODE")]
    public string ZIPCODE;

    [Layout(1730, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/county", "bfdcplor_count")]
    [IJE_Name("COUNTYTXT")]
    public string COUNTYTXT;

    [Layout(1758, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/city", "bfdcplor_city")]
    [IJE_Name("CITYTEXT")]
    public string CITYTEXT;

    [Layout(2021, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation", "bfdcpdom_p_occup")]
    [IJE_Name("MOM_OC_T")]
    public string MOM_OC_T;

    [Layout(2049, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation", "bfdcpdof_p_occup")]
    [IJE_Name("DAD_OC_T")]
    public string DAD_OC_T;

    [Layout(2077, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry", "bfdcpdom_ob_indus")]
    [IJE_Name("MOM_IN_T")]
    public string MOM_IN_T;

    [Layout(2105, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry", "bfdcpdof_ob_indus")]
    [IJE_Name("DAD_IN_T")]
    public string DAD_IN_T;

    [Layout(2283, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/transferred_from_where", "bfdcpfodd_tf_where")]
    [IJE_Name("HOSPFROM")]
    public string HOSPFROM;

    [Layout(2333, 50)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/biometrics_and_demographics/facility_city_state", "bcifsbad_fc_state")]
    [IJE_Name("HOSPTO")]
    public string HOSPTO;

    [Layout(2383, 20)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_attendant_type", "bfdcpfodd_oa_type")]
    [IJE_Name("ATTEND_OTH_TXT")]
    public string ATTEND_OTH_TXT;

    [Layout(2826, 12)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_npi", "bfdcpfodd_a_npi")]
    [IJE_Name("ATTEND_NPI")]
    public string ATTEND_NPI;

    [Layout(2921, 15)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/record_identification/newborn_medical_record_number", "bcifsri_nmr_numbe")]
    [IJE_Name("INF_MED_REC_NUM")]
    public string INF_MED_REC_NUM;

    [Layout(2936, 15)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/medical_record_number", "bfdcpri_mr_numbe")]
    [IJE_Name("MOM_MED_REC_NUM")]
    public string MOM_MED_REC_NUM;






    public static string ToCSV(NAT_File_Record record)
    {
        var kvpList = new List<KeyValuePair<string, string>>();

        //Itterate through each property
        foreach (var prop in record.GetType().GetFields())
        {
            foreach (object attr in prop.GetCustomAttributes())
            {
                if (attr is MMRIA_PathAttribute)
                {
                    var mmria_pathAttribute = (MMRIA_PathAttribute)attr;

                    if (mmria_pathAttribute == null)
                    {
                    }
                    else
                    {
                        string pathSanitized = mmria_pathAttribute.MMRIA_Path_Name?.Replace(',', ' ');
                        string fieldSanitized = mmria_pathAttribute.MMRIA_Field_Name?.Replace(',', ' ');
                        //Extract path from property and its associated value
                        kvpList.Add(new KeyValuePair<string, string>($"{pathSanitized}/{fieldSanitized}", prop.GetValue(record)?.ToString()));
                    }
                }
            }
        }

        //Create a comma seperated list from the kvp
        var strings = kvpList.Select(kvp => $"{kvp.Key?.Replace(',', ' ')},{kvp.Value}");

        //Return the new line seperated csv
        return string.Join(Environment.NewLine, strings);
    }

    private static string GetMMRIAAttribute(FieldInfo fi)
    {
        string result = string.Empty;


        foreach (object attr in fi.GetCustomAttributes())
        {
            if (attr is MMRIA_PathAttribute)
            {
                var mmria_pathAttribute = (MMRIA_PathAttribute)attr;

                if (mmria_pathAttribute == null)
                {
                }
                else
                {
                    result = $"{mmria_pathAttribute.MMRIA_Path_Name?.Replace(',', ' ')}/{mmria_pathAttribute.MMRIA_Field_Name?.Replace(',', ' ')}";
                }
            }
        }



        return result;
    }

    public Dictionary<string, string> GetComparisonValues()
    {
        var dictionary = new Dictionary<string, string>();

        //1 mmria site/host/ reporting state - file-name ? or 2.    SODR
        //2 home_record/state of death - DState

        ////3 home_recode/date_of_death - DOD_YR, DOD_MO, DOD_DY
        //dictionary.Add("DOD_YR", this.hrdod_year);
        //dictionary.Add("DOD_MO", this.hrdod_month);
        //dictionary.Add("DOD_DY", this.hrdod_day);

        ////4 death_certificate/date_of_birth - DOB_YR, DOB_MO, DOD_DY
        //dictionary.Add("DOB_YR", this.dcddob_year);
        //dictionary.Add("DOB_MO", this.dcddob_month);
        //dictionary.Add("DOD_DY", this.dcddob_day);

        ////5 home_record/last_name - LNAME 
        //dictionary.Add("LNAME", this.hr_l_name);

        ////6 home_record/first_name - GNAME
        //dictionary.Add("GNAME", this.hr_f_name);

        return dictionary;
    }

    public string GetIJEAttributeValue(FieldInfo fi)
    {
        string result = string.Empty;


        foreach (object attr in fi.GetCustomAttributes())
        {
            if (attr is IJE_Name)
            {
                var ije_Name = (IJE_Name)attr;

                if (ije_Name == null)
                {
                }
                else
                {
                    result = $"{ije_Name.Name}";
                }
            }
        }



        return result;
    }
}

