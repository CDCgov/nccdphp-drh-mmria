using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace mmria.services.vitalsimport;

public sealed class FET_File_Record
{
    public static int Record_Length = 6000;

    [Layout(0, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year", "bfdcpfodddod_year")]
    [MMRIA_Path(" birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "bcifsri_do_deliv")]
    [IJE_Name("FDOD_YR")]
    public string FDOD_YR;

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
    [IJE_Name("TD")]
    public string TD;

    [Layout(30, 2)]
    [MMRIA_Path("/birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "bcifsri_do_deliv")]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month", "bfdcpfodddod_month")]
    [IJE_Name("FDOD_MO")]
    public string FDOD_MO;

    [Layout(32, 2)]
    [MMRIA_Path("/birth_certificate_infant_fetal_section/record_identification/date_of_delivery", "bcifsri_do_deliv")]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day", "bfdcpfodddod_day")]
    [IJE_Name("FDOD_DY")]
    public string FDOD_DY;

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
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/mother_married", "bfdcpdom_m_marri")]
    [IJE_Name("MARN")]
    public string MARN;

    [Layout(92, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/education_level", "bfdcpdom_e_level")]
    [IJE_Name("MEDUC")]
    public string MEDUC;

    [Layout(98, 20)]
    [MMRIA_Path("New MMRIA Field: (for mother) Other Hispanic, specify (add MMRIA path when available)", "bfdcpdom_ioh_origi_othsp")]
    [IJE_Name("METHNIC5")]
    public string METHNIC5;

    [Layout(421, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_type", "bfdcpfodd_a_type")]
    [IJE_Name("ATTEND")]
    public string ATTEND;

    [Layout(422, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_mother_transferred", "bfdcpfodd_wm_trans")]
    [IJE_Name("TRAN")]
    public string TRAN;

    [Layout(439, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/number_of_visits", "bfdcppc_no_visit")]
    [IJE_Name("NPREV")]
    public string NPREV;

    [Layout(442, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/height_feet", "bfdcpmb_h_feet")]
    [IJE_Name("HFT")]
    public string HFT;

    [Layout(443, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/height_inches", "bfdcpmb_h_inche")]
    [IJE_Name("HIN")]
    public string HIN;

    [Layout(446, 3)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight", "bfdcpmb_pp_weigh")]
    [IJE_Name("PWGT")]
    public string PWGT;

    [Layout(450, 3)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery", "bfdcpmb_wa_deliv")]
    [IJE_Name("DWGT")]
    public string DWGT;

    [Layout(454, 1)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/was_wic_used", "bfdcppc_ww_used")]
    [IJE_Name("WIC")]
    public string WIC;

    [Layout(455, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/now_living", "bfdcpph_n_livin")]
    [IJE_Name("PLBL")]
    public string PLBL;

    [Layout(457, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/now_dead", "bfdcpph_n_dead")]
    [IJE_Name("PLBD")]
    public string PLBD;

    [Layout(459, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/pregnancy_history/other_outcomes", "bfdcpph_o_outco")]
    [IJE_Name("POPO")]
    public string POPO;

    [Layout(461, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month", "bfdcpphdollb_month")]
    [IJE_Name("MLLB")]
    public string MLLB;

    [Layout(463, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year", "bfdcpphdollb_year")]
    [IJE_Name("YLLB")]
    public string YLLB;

    [Layout(467, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month", "bfdcpphdoloo_month")]
    [IJE_Name("MOPO")]
    public string MOPO;

    [Layout(469, 4)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year", "bfdcpphdoloo_year")]
    [IJE_Name("YOPO")]
    public string YOPO;

    [Layout(481, 4)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year", "bfdcppcdolnm_year")]
    [IJE_Name("DLMP_YR")]
    public string DLMP_YR;

    [Layout(485, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month", "bfdcppcdolnm_month")]
    [IJE_Name("DLMP_MO")]
    public string DLMP_MO;

    [Layout(487, 2)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day", "bfdcppcdolnm_day")]
    [IJE_Name("DLMP_DY")]
    public string DLMP_DY;

    [Layout(498, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/risk_factors/number_of_c_sections", "bfdcprf_noc_secti")]
    [IJE_Name("NPCES")]
    public string NPCES;

    [Layout(511, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_forceps_attempted_but_unsuccessful", "bcifsmod_wdwfab_unsuc")]
    [IJE_Name("ATTF")]
    public string ATTF;

    [Layout(512, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_vacuum_extration_attempted_but_unsuccessful", "bcifsmod_wdwveab_unsuc")]
    [IJE_Name("ATTV")]
    public string ATTV;

    [Layout(513, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/fetal_delivery", "bcifsmod_f_deliv")]
    [IJE_Name("PRES")]
    public string PRES;

    [Layout(514, 1)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery", "bcifsmod_framo_deliv")]
    [IJE_Name("ROUT")]
    public string ROUT;

    [Layout(528, 2)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/prenatal_care/obsteric_estimate_of_gestation", "bfdcppc_oeo_gesta")]
    [IJE_Name("OWGEST")]
    public string OWGEST;

    [Layout(537, 2)]
    [MMRIA_Path("birth_certificate_infant_fetal_section/birth_order", "bcifs_b_order")]
    [IJE_Name("SORD")]
    public string SORD;

    [Layout(2904, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name", "bfdcpfodd_f_name")]
    [IJE_Name("HOSP_D")]
    public string HOSP_D;

    [Layout(3051, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_location/street", "bfdcpfodl_stree")]
    [IJE_Name("ADDRESS_D")]
    public string ADDRESS_D;

    [Layout(3101, 9)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_location/zip_code", "bfdcpfodl_z_code")]
    [IJE_Name("ZIPCODE_D")]
    public string ZIPCODE_D;

    [Layout(3110, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_location/County", "bfdcpfodl_count")]
    [IJE_Name("CNTY_D")]
    public string CNTY_D;

    [Layout(3138, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_location/city", "bfdcpfodl_city")]
    [IJE_Name("CITY_D")]
    public string CITY_D;

    [Layout(3256, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/first_name", "bfdcpri_f_name")]
    [IJE_Name("MOMFNAME")]
    public string MOMFNAME;

    [Layout(3306, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/middle_name", "bfdcpri_m_name")]
    [IJE_Name("MOMMNAME")]
    public string MOMMNAME;

    [Layout(3356, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/record_identification/last_name", "bfdcpri_l_name")]
    [IJE_Name("MOMLNAME")]
    public string MOMLNAME;

    [Layout(3516, 50)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/record_identification/maiden_name", "bfdcpri_m_name_244")]
    [IJE_Name("MOMMAIDN")]
    public string MOMMAIDN;

    [Layout(3576, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("STNUM")]
    public string STNUM;

    [Layout(3586, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("PREDIR")]
    public string PREDIR;

    [Layout(3596, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("STNAME")]
    public string STNAME;

    [Layout(3646, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("STDESIG")]
    public string STDESIG;

    [Layout(3656, 10)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/street", "bfdcplor_stree")]
    [IJE_Name("POSTDIR")]
    public string POSTDIR;

    [Layout(3666, 7)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/apartment", "bfdcplor_apart")]
    [IJE_Name("APTNUMB")]
    public string APTNUMB;

    [Layout(3723, 9)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/zip_code", "bfdcplor_z_code")]
    [IJE_Name("ZIPCODE")]
    public string ZIPCODE;

    [Layout(3732, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/county", "bfdcplor_count")]
    [IJE_Name("COUNTYTXT")]
    public string COUNTYTXT;

    [Layout(3760, 28)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/location_of_residence/city", "bfdcplor_city")]
    [IJE_Name("CITYTXT")]
    public string CITYTXT;

    [Layout(4060, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation", "bfdcpdom_p_occup")]
    [IJE_Name("MOM_OC_T")]
    public string MOM_OC_T;

    [Layout(4088, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation", "bfdcpdof_p_occup")]
    [IJE_Name("DAD_OC_T")]
    public string DAD_OC_T;

    [Layout(4116, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry", "bfdcpdom_ob_indus")]
    [IJE_Name("MOM_IN_T")]
    public string MOM_IN_T;

    [Layout(4144, 25)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry", "bfdcpdof_ob_indus")]
    [IJE_Name("DAD_IN_T")]
    public string DAD_IN_T;

    [Layout(4288, 1)]
    [MMRIA_Path("/birth_fetal_death_certificate_parent/demographic_of_father/education_level", "bfdcpdof_e_level")]
    [IJE_Name("FEDUC")]
    public string FEDUC;

    [Layout(4294, 20)]
    [MMRIA_Path("New MMRIA Field: (for father) Other Hispanic, specify (add MMRIA path when available)", "bfdcpdof_ifoh_origi _othsp")]
    [IJE_Name("FETHNIC5")]
    public string FETHNIC5;

    [Layout(4763, 50)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/transferred_from_where", "bfdcpfodd_tf_where")]
    [IJE_Name("HOSPFROM")]
    public string HOSPFROM;

    [Layout(4863, 12)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_npi", "bfdcpfodd_a_npi")]
    [IJE_Name("ATTEND_NPI")]
    public string ATTEND_NPI;

    [Layout(4875, 20)]
    [MMRIA_Path("birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_attendant_type", "bfdcpfodd_oa_type")]
    [IJE_Name("ATTEND_OTH_TXT")]
    public string ATTEND_OTH_TXT;






    public static string ToCSV(FET_File_Record record)
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

