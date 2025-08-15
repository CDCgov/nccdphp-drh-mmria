using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace mmria_pmss_client.Models.IJE;
public sealed class PMSS_All
{
    public PMSS_All() {}

    public string batch_name{ get; set; }
    public string fileno_dc{ get; set; }
    public string fileno_bc{ get; set; }
    public string fileno_fdc{ get; set; }
    public string year_birthorfetaldeath{ get; set; }
    public string pregcb_match{ get; set; }
    public string literalcod_match{ get; set; }
    public string icd10_match{ get; set; }
    public string bc_det_match{ get; set; }
    public string fdc_det_match{ get; set; }
    public string bc_prob_match{ get; set; }
    public string fdc_prob_match{ get; set; }
    public string vro_resolution_status{ get; set; }
    public string vro_resolution_remarks{ get; set; }
    public string Year{ get; set; }
    public string CaseNo{ get; set; }
    public string PMSS_State_Code{ get; set; }
    
    public string Jurisdiction{ get; set; }
    public string Jurisdiction_Name	{ get; set; }
    public string Status{ get; set; }
    public string AmssNo{ get; set; }
    public string AmssRel{ get; set; }
    public string Dc{ get; set; }
    public string Dod{ get; set; }
    public string SourcNot{ get; set; }
    public string DcFile{ get; set; }
    public string LbFile{ get; set; }
    public string PregStat{ get; set; }
    public string PcbTime{ get; set; }
    public string StatDth{ get; set; }
    public string StatRes{ get; set; }
    public string ResZip{ get; set; }
    public string ZipSrce{ get; set; }
    public string County{ get; set; }
    public string CntySrce{ get; set; }
    public string MAge{ get; set; }
    public string Dob{ get; set; }
    public string AgeDif{ get; set; }
    public string Race{ get; set; }
    public string Race_Oth{ get; set; }
    public string Race_Source{ get; set; }
    public string Race_OMB{ get; set; }
    public string Race_White{ get; set; }
    public string Race_Black{ get; set; }
    public string Race_AmIndAlkNat{ get; set; }
    public string Race_AsianIndian{ get; set; }
    public string Race_Chinese{ get; set; }
    public string Race_Filipino{ get; set; }
    public string Race_Japanese{ get; set; }
    public string Race_Korean{ get; set; }
    public string Race_Vietnamese{ get; set; }
    public string Race_OtherAsian{ get; set; }
    public string Race_NativeHawaiian{ get; set; }
    public string Race_GuamCham{ get; set; }
    public string Race_Samoan{ get; set; }
    public string Race_OtherPacific{ get; set; }
    public string Race_Other{ get; set; }
    public string Race_NotSpecified{ get; set; }
    public string HispOrg{ get; set; }
    public string Hisp_Oth{ get; set; }
    public string MatBplc{ get; set; }
    public string MatBplc_US{ get; set; }
    public string MatBplc_Else{ get; set; }
    public string MarStat{ get; set; }
    public string EducaTn{ get; set; }
    public string PlaceDth{ get; set; }
    public string Pnc{ get; set; }
    public string Autopsy3{ get; set; }
    public string Height{ get; set; }
    public string WtPrePrg{ get; set; }
    public string PrevLb{ get; set; }
    public string PrvOthPg{ get; set; }
    public string PymtSrc{ get; set; }
    public string Wic{ get; set; }
    public string OutIndx{ get; set; }
    public string MultGest{ get; set; }
    public string TermProc{ get; set; }
    public string TermPro1{ get; set; }
    public string TermPro2{ get; set; }
    public string GestWk{ get; set; }
    public string DTerm{ get; set; }
    public string DayDif{ get; set; }
    public string CdcCod{ get; set; }
    public string Cod{ get; set; }
    public string AssoC1{ get; set; }
    public string Acon1{ get; set; }
    public string AssoC2{ get; set; }
    public string Acon2{ get; set; }
    public string AssoC3{ get; set; }
    public string Acon3{ get; set; }
    public string Injury{ get; set; }
    public string Drug_1{ get; set; }
    public string Drug_2{ get; set; }
    public string Drug_3{ get; set; }
    public string Class{ get; set; }
    public string Coder{ get; set; }
    public string ClsMo{ get; set; }
    public string ClsYr{ get; set; }
    public string Tribe{ get; set; }
    public string Occup{ get; set; }
    public string Indust{ get; set; }
    public string Bmi{ get; set; }
    public string Ethnic_Mexican{ get; set; }
    public string Ethnic_PR{ get; set; }
    public string Ethnic_Cuban{ get; set; }
    public string Ethnic_Other{ get; set; }
    public string Drug_IV{ get; set; }
    public string Dod_MO{ get; set; }
    public string Dod_DY{ get; set; }
    public string Dod_YR{ get; set; }
    public string Dod_TM{ get; set; }
    public string Dob_MO{ get; set; }
    public string Dob_DY{ get; set; }
    public string Dob_YR{ get; set; }
    public string DTerm_MO{ get; set; }
    public string DTerm_DY{ get; set; }
    public string DTerm_YR{ get; set; }
    public string DTerm_TM{ get; set; }
    public string Review_1_By{ get; set; }
    public string Review_1_On{ get; set; }
    public string Review_1_Remarks{ get; set; }
    public string Remarks{ get; set; }
    public string Update_Remarks{ get; set; }
    public string Pdf_Link{ get; set; }
    public string Pdf_Steve_Link{ get; set; }
    public string Review_2_By{ get; set; }
    public string Review_2_On{ get; set; }
    public string Review_2_Remarks{ get; set; }
    public string dc_info_complete{ get; set; }
    public string dc_info_remarks{ get; set; }
    public string mmria_used{ get; set; }
    public string mmria_used_remarks{ get; set; }
    public string agreement_status{ get; set; }
    public string agreement_remarks{ get; set; }
    //public string fileno_dc{ get; set; }
    public string auxno_dc{ get; set; }
    public string replace_dc{ get; set; }
    public string void_dc{ get; set; }
    public string dod_mo_dc{ get; set; }
    public string dod_dy_dc{ get; set; }
    public string dod_yr_dc{ get; set; }
    public string tod_dc{ get; set; }
    public string dplace_dc{ get; set; }
    public string citytext_d_dc{ get; set; }
    public string countytext_d_dc{ get; set; }
    public string statetext_d_dc{ get; set; }
    public string zip9_d_dc{ get; set; }
    public string preg_dc{ get; set; }
    public string inact_dc{ get; set; }
    public string autop_dc{ get; set; }
    public string autopf_dc{ get; set; }
    public string transprt_dc{ get; set; }
    public string tobac_dc{ get; set; }
    public string manner_dc{ get; set; }
    public string cod1a_dc{ get; set; }
    public string interval1a_dc{ get; set; }
    public string cod1b_dc{ get; set; }
    public string interval1b_dc{ get; set; }
    public string cod1c_dc{ get; set; }
    public string interval1c_dc{ get; set; }
    public string cod1d_dc{ get; set; }
    public string interval1d_dc{ get; set; }
    public string othercondition_dc{ get; set; }
    public string man_uc_dc{ get; set; }
    public string acme_uc_dc{ get; set; }
    public string eac_dc{ get; set; }
    public string rac_dc{ get; set; }
    public string doi_mo_dc{ get; set; }
    public string doi_dy_dc{ get; set; }
    public string doi_yr_dc{ get; set; }
    public string toi_hr_dc{ get; set; }
    public string howinj_dc{ get; set; }
    public string workinj_dc{ get; set; }
    public string bplace_cnt_dc{ get; set; }
    public string bplace_st_dc{ get; set; }
    public string cityc_dc{ get; set; }
    public string citytext_r_dc{ get; set; }
    public string countyc_dc{ get; set; }
    public string countrytext_r_dc{ get; set; }
    public string statec_dc{ get; set; }
    public string statetext_r_dc{ get; set; }
    public string zip9_r_dc{ get; set; }
    public string dob_mo_dc{ get; set; }
    public string dob_dy_dc{ get; set; }
    public string dob_yr_dc{ get; set; }
    public string agetype_dc{ get; set; }
    public string age_dc{ get; set; }
    public string sex_dc{ get; set; }
    public string marital_dc{ get; set; }
    public string deduc_dc{ get; set; }
    public string indust_dc{ get; set; }
    public string occup_dc{ get; set; }
    public string armedf_dc{ get; set; }
    public string dethnic1_dc{ get; set; }
    public string dethnic2_dc{ get; set; }
    public string dethnic3_dc{ get; set; }
    public string dethnic4_dc{ get; set; }
    public string dethnic5_dc{ get; set; }
    public string race1_dc{ get; set; }
    public string race2_dc{ get; set; }
    public string race3_dc{ get; set; }
    public string race4_dc{ get; set; }
    public string race5_dc{ get; set; }
    public string race6_dc{ get; set; }
    public string race7_dc{ get; set; }
    public string race8_dc{ get; set; }
    public string race9_dc{ get; set; }
    public string race10_dc{ get; set; }
    public string race11_dc{ get; set; }
    public string race12_dc{ get; set; }
    public string race13_dc{ get; set; }
    public string race14_dc{ get; set; }
    public string race15_dc{ get; set; }
    public string race16_dc{ get; set; }
    public string race17_dc{ get; set; }
    public string race18_dc{ get; set; }
    public string race19_dc{ get; set; }
    public string race20_dc{ get; set; }
    public string race21_dc{ get; set; }
    public string race22_dc{ get; set; }
    public string race23_dc{ get; set; }
    public string bstate_bc{ get; set; }
    //public string fileno_bc{ get; set; }
    public string auxno_bc{ get; set; }
    public string void_bc{ get; set; }
    public string replace_bc{ get; set; }
    public string dwgt_bc{ get; set; }
    public string pwgt_bc{ get; set; }
    public string hft_bc{ get; set; }
    public string hin_bc{ get; set; }
    public string idob_mo_bc{ get; set; }
    public string idob_dy_bc{ get; set; }
    public string idob_yr_bc{ get; set; }
    public string tb_bc{ get; set; }
    public string isex_bc{ get; set; }
    public string bwg_bc{ get; set; }
    public string owgest_bc{ get; set; }
    public string apgar5_bc{ get; set; }
    public string apgar10_bc{ get; set; }
    public string plur_bc{ get; set; }
    public string sord_bc{ get; set; }
    public string hosp_bc{ get; set; }
    public string birth_co_bc{ get; set; }
    public string bplace_bc{ get; set; }
    public string attend_bc{ get; set; }
    public string tran_bc{ get; set; }
    public string itran_bc{ get; set; }
    public string bfed_bc{ get; set; }
    public string wic_bc{ get; set; }
    public string pay_bc{ get; set; }
    public string pres_bc{ get; set; }
    public string rout_bc{ get; set; }
    public string iliv_bc{ get; set; }
    public string gon_bc{ get; set; }
    public string syph_bc{ get; set; }
    public string hsv_bc{ get; set; }
    public string cham_bc{ get; set; }
    public string hepb_bc{ get; set; }
    public string hepc_bc{ get; set; }
    public string cerv_bc{ get; set; }
    public string toc_bc{ get; set; }
    public string ecvs_bc{ get; set; }
    public string ecvf_bc{ get; set; }
    public string prom_bc{ get; set; }
    public string pric_bc{ get; set; }
    public string prol_bc{ get; set; }
    public string indl_bc{ get; set; }
    public string augl_bc{ get; set; }
    public string nvpr_bc{ get; set; }
    public string ster_bc{ get; set; }
    public string antb_bc{ get; set; }
    public string chor_bc{ get; set; }
    public string mecs_bc{ get; set; }
    public string fint_bc{ get; set; }
    public string esan_bc{ get; set; }
    public string tlab_bc{ get; set; }
    public string mtr_bc{ get; set; }
    public string plac_bc{ get; set; }
    public string rut_bc{ get; set; }
    public string uhys_bc{ get; set; }
    public string aint_bc{ get; set; }
    public string uopr_bc{ get; set; }
    public string aven1_bc{ get; set; }
    public string aven6_bc{ get; set; }
    public string nicu_bc{ get; set; }
    public string surf_bc{ get; set; }
    public string anti_bc{ get; set; }
    public string seiz_bc{ get; set; }
    public string binj_bc{ get; set; }
    public string anen_bc{ get; set; }
    public string mnsb_bc{ get; set; }
    public string cchd_bc{ get; set; }
    public string cdh_bc{ get; set; }
    public string omph_bc{ get; set; }
    public string gast_bc{ get; set; }
    public string limb_bc{ get; set; }
    public string cl_bc{ get; set; }
    public string cp_bc{ get; set; }
    public string dowt_bc{ get; set; }
    public string cdit_bc{ get; set; }
    public string hypo_bc{ get; set; }
    public string dlmp_mo_bc{ get; set; }
    public string dlmp_dy_bc{ get; set; }
    public string dlmp_yr_bc{ get; set; }
    public string dofp_mo_bc{ get; set; }
    public string dofp_dy_bc{ get; set; }
    public string dofp_yr_bc{ get; set; }
    public string dolp_mo_bc{ get; set; }
    public string dolp_dy_bc{ get; set; }
    public string dolp_yr_bc{ get; set; }
    public string nprev_bc{ get; set; }
    public string plbl_bc{ get; set; }
    public string plbd_bc{ get; set; }
    public string popo_bc{ get; set; }
    public string mllb_bc{ get; set; }
    public string yllb_bc{ get; set; }
    public string mopo_bc{ get; set; }
    public string yopo_bc{ get; set; }
    public string cigpn_bc{ get; set; }
    public string cigfn_bc{ get; set; }
    public string cigsn_bc{ get; set; }
    public string cigln_bc{ get; set; }
    public string pdiab_bc{ get; set; }
    public string gdiab_bc{ get; set; }
    public string phype_bc{ get; set; }
    public string ghype_bc{ get; set; }
    public string ppb_bc{ get; set; }
    public string ppo_bc{ get; set; }
    public string inft_bc{ get; set; }
    public string pces_bc{ get; set; }
    public string npces_bc{ get; set; }
    public string ehype_bc{ get; set; }
    public string inft_drg_bc{ get; set; }
    public string inft_art_bc{ get; set; }
    public string citytext_bc{ get; set; }
    public string countytxt_bc{ get; set; }
    public string statetxt_bc{ get; set; }
    public string zipcode_bc{ get; set; }
    public string mbplace_st_ter_tx_bc{ get; set; }
    public string mbplace_cntry_tx_bc{ get; set; }
    public string mager_bc{ get; set; }
    public string mdob_mo_bc{ get; set; }
    public string mdob_dy_bc{ get; set; }
    public string mdob_yr_bc{ get; set; }
    public string marn_bc{ get; set; }
    public string ackn_bc{ get; set; }
    public string meduc_bc{ get; set; }
    public string mom_in_t_bc{ get; set; }
    public string mom_oc_t_bc{ get; set; }
    public string methnic1_bc{ get; set; }
    public string methnic2_bc{ get; set; }
    public string methnic3_bc{ get; set; }
    public string methnic4_bc{ get; set; }
    public string methnic5_bc{ get; set; }
    public string mrace1_bc{ get; set; }
    public string mrace2_bc{ get; set; }
    public string mrace3_bc{ get; set; }
    public string mrace4_bc{ get; set; }
    public string mrace5_bc{ get; set; }
    public string mrace6_bc{ get; set; }
    public string mrace7_bc{ get; set; }
    public string mrace8_bc{ get; set; }
    public string mrace9_bc{ get; set; }
    public string mrace10_bc{ get; set; }
    public string mrace11_bc{ get; set; }
    public string mrace12_bc{ get; set; }
    public string mrace13_bc{ get; set; }
    public string mrace14_bc{ get; set; }
    public string mrace15_bc{ get; set; }
    public string mrace16_bc{ get; set; }
    public string mrace17_bc{ get; set; }
    public string mrace18_bc{ get; set; }
    public string mrace19_bc{ get; set; }
    public string mrace20_bc{ get; set; }
    public string mrace21_bc{ get; set; }
    public string mrace22_bc{ get; set; }
    public string mrace23_bc{ get; set; }
    public string fager_bc{ get; set; }
    public string dad_in_t_bc{ get; set; }
    public string dad_oc_t_bc{ get; set; }
    public string fbplacd_st_ter_c_bc{ get; set; }
    public string fbplace_cnt_c_bc{ get; set; }
    public string dstate_fdc{ get; set; }
    //public string fileno_fdc{ get; set; }
    public string auxno_fdc{ get; set; }
    public string void_fdc{ get; set; }
    public string replace_fdc{ get; set; }
    public string fdod_mo_fdc{ get; set; }
    public string fdod_dy_fdc{ get; set; }
    public string fdod_yr_fdc{ get; set; }
    public string td_fdc{ get; set; }
    public string dwgt_fdc{ get; set; }
    public string pwgt_fdc{ get; set; }
    public string hft_fdc{ get; set; }
    public string hin_fdc{ get; set; }
    public string fsex_fdc{ get; set; }
    public string fwg_fdc{ get; set; }
    public string owgest_fdc{ get; set; }
    public string plur_fdc{ get; set; }
    public string sord_fdc{ get; set; }
    public string hosp_d_fdc{ get; set; }
    public string cnty_d_fdc{ get; set; }
    public string city_d_fdc{ get; set; }
    public string attend_fdc{ get; set; }
    public string tran_fdc{ get; set; }
    public string wic_fdc{ get; set; }
    public string pres_fdc{ get; set; }
    public string rout_fdc{ get; set; }
    public string gon_fdc{ get; set; }
    public string syph_fdc{ get; set; }
    public string hsv_fdc{ get; set; }
    public string cham_fdc{ get; set; }
    public string lm_fdc{ get; set; }
    public string gbs_fdc{ get; set; }
    public string cmv_fdc{ get; set; }
    public string b19_fdc{ get; set; }
    public string toxo_fdc{ get; set; }
    public string hsv1_fdc{ get; set; }
    public string hiv_fdc{ get; set; }
    public string tlab_fdc{ get; set; }
    public string otheri_fdc{ get; set; }
    public string mtr_fdc{ get; set; }
    public string plac_fdc{ get; set; }
    public string rut_fdc{ get; set; }
    public string uhys_fdc{ get; set; }
    public string aint_fdc{ get; set; }
    public string uopr_fdc{ get; set; }
    public string anen_fdc{ get; set; }
    public string mnsb_fdc{ get; set; }
    public string cchd_fdc{ get; set; }
    public string cdh_fdc{ get; set; }
    public string omph_fdc{ get; set; }
    public string gast_fdc{ get; set; }
    public string limb_fdc{ get; set; }
    public string cl_fdc{ get; set; }
    public string cp_fdc{ get; set; }
    public string dowt_fdc{ get; set; }
    public string cdit_fdc{ get; set; }
    public string hypo_fdc{ get; set; }
    public string cod18a1_fdc{ get; set; }
    public string cod18a2_fdc{ get; set; }
    public string cod18a3_fdc{ get; set; }
    public string cod18a4_fdc{ get; set; }
    public string cod18a5_fdc{ get; set; }
    public string cod18a6_fdc{ get; set; }
    public string cod18a7_fdc{ get; set; }
    public string cod18a8_fdc{ get; set; }
    public string cod18a9_fdc{ get; set; }
    public string cod18a10_fdc{ get; set; }
    public string cod18a11_fdc{ get; set; }
    public string cod18a12_fdc{ get; set; }
    public string cod18a13_fdc{ get; set; }
    public string cod18a14_fdc{ get; set; }
    public string cod18b1_fdc{ get; set; }
    public string cod18b2_fdc{ get; set; }
    public string cod18b3_fdc{ get; set; }
    public string cod18b4_fdc{ get; set; }
    public string cod18b5_fdc{ get; set; }
    public string cod18b6_fdc{ get; set; }
    public string cod18b7_fdc{ get; set; }
    public string cod18b8_fdc{ get; set; }
    public string cod18b9_fdc{ get; set; }
    public string cod18b10_fdc{ get; set; }
    public string cod18b11_fdc{ get; set; }
    public string cod18b12_fdc{ get; set; }
    public string cod18b13_fdc{ get; set; }
    public string cod18b14_fdc{ get; set; }
    public string icod_fdc{ get; set; }
    public string ocod1_fdc{ get; set; }
    public string ocod2_fdc{ get; set; }
    public string ocod3_fdc{ get; set; }
    public string ocod4_fdc{ get; set; }
    public string ocod5_fdc{ get; set; }
    public string ocod6_fdc{ get; set; }
    public string ocod7_fdc{ get; set; }
    public string dlmp_mo_fdc{ get; set; }
    public string dlmp_dy_fdc{ get; set; }
    public string dlmp_yr_fdc{ get; set; }
    public string dofp_mo_fdc{ get; set; }
    public string dofp_dy_fdc{ get; set; }
    public string dofp_yr_fdc{ get; set; }
    public string dolp_mo_fdc{ get; set; }
    public string dolp_dy_fdc{ get; set; }
    public string dolp_yr_fdc{ get; set; }
    public string nprev_fdc{ get; set; }
    public string plbl_fdc{ get; set; }
    public string plbd_fdc{ get; set; }
    public string popo_fdc{ get; set; }
    public string mllb_fdc{ get; set; }
    public string yllb_fdc{ get; set; }
    public string mopo_fdc{ get; set; }
    public string yopo_fdc{ get; set; }
    public string cigpn_fdc{ get; set; }
    public string cigfn_fdc{ get; set; }
    public string cigsn_fdc{ get; set; }
    public string cigln_fdc{ get; set; }
    public string pdiab_fdc{ get; set; }
    public string gdiab_fdc{ get; set; }
    public string phype_fdc{ get; set; }
    public string ghype_fdc{ get; set; }
    public string ppb_fdc{ get; set; }
    public string ppo_fdc{ get; set; }
    public string inft_fdc{ get; set; }
    public string pces_fdc{ get; set; }
    public string npces_fdc{ get; set; }
    public string ehype_fdc{ get; set; }
    public string inft_drg_fdc{ get; set; }
    public string inft_art_fdc{ get; set; }
    public string citytxt_fdc{ get; set; }
    public string countytxt_fdc{ get; set; }
    public string statetxt_fdc{ get; set; }
    public string zipcode_fdc{ get; set; }
    public string mbplace_st_ter_txt_fdc{ get; set; }
    public string mbplace_cntry_txt_fdc{ get; set; }
    public string mager_fdc{ get; set; }
    public string mdob_mo_fdc{ get; set; }
    public string mdob_dy_fdc{ get; set; }
    public string mdob_yr_fdc{ get; set; }
    public string marn_fdc{ get; set; }
    public string meduc_fdc{ get; set; }
    public string mom_in_t_fdc{ get; set; }
    public string mom_oc_t_fdc{ get; set; }
    public string methnic1_fdc{ get; set; }
    public string methnic2_fdc{ get; set; }
    public string methnic3_fdc{ get; set; }
    public string methnic4_fdc{ get; set; }
    public string methnic5_fdc{ get; set; }
    public string mrace1_fdc{ get; set; }
    public string mrace2_fdc{ get; set; }
    public string mrace3_fdc{ get; set; }
    public string mrace4_fdc{ get; set; }
    public string mrace5_fdc{ get; set; }
    public string mrace6_fdc{ get; set; }
    public string mrace7_fdc{ get; set; }
    public string mrace8_fdc{ get; set; }
    public string mrace9_fdc{ get; set; }
    public string mrace10_fdc{ get; set; }
    public string mrace11_fdc{ get; set; }
    public string mrace12_fdc{ get; set; }
    public string mrace13_fdc{ get; set; }
    public string mrace14_fdc{ get; set; }
    public string mrace15_fdc{ get; set; }
    public string mrace16_fdc{ get; set; }
    public string mrace17_fdc{ get; set; }
    public string mrace18_fdc{ get; set; }
    public string mrace19_fdc{ get; set; }
    public string mrace20_fdc{ get; set; }
    public string mrace21_fdc{ get; set; }
    public string mrace22_fdc{ get; set; }
    public string mrace23_fdc{ get; set; }
    public string fager_fdc{ get; set; }
    public string dad_in_t_fdc{ get; set; }
    public string dad_oc_t_fdc{ get; set; }
    public string fbplacd_st_ter_c_fdc{ get; set; }
    public string fbplace_cnt_c_fdc{ get; set; }


    public static PMSS_All FromList(List<string> row)
    {
        var x = new PMSS_All();
        
        x.batch_name = row[  0];
        x.fileno_dc = row[  1];
        x.fileno_bc = row[  2];
        x.fileno_fdc = row[  3];
        x.year_birthorfetaldeath = row[  4];
        x.pregcb_match = row[  5];
        x.literalcod_match = row[  6];
        x.icd10_match = row[  7];
        x.bc_det_match = row[  8];
        x.fdc_det_match = row[  9];
        x.bc_prob_match = row[ 10];
        x.fdc_prob_match = row[ 11];
        x.vro_resolution_status = row[ 12];
        x.vro_resolution_remarks = row[ 13];
        x.Year = row[ 14];
        x.CaseNo = row[ 15];
        x.PMSS_State_Code = row[ 16];
        x.Status = row[ 17];
        x.AmssNo = row[ 18];
        x.AmssRel = row[ 19];
        x.Dc = row[ 20];
        x.Dod = row[ 21];
        x.SourcNot = row[ 22];
        x.DcFile = row[ 23];
        x.LbFile = row[ 24];
        x.PregStat = row[ 25];
        x.PcbTime = row[ 26];
        x.StatDth = row[ 27];
        x.StatRes = row[ 28];
        x.ResZip = row[ 29];
        x.ZipSrce = row[ 30];
        x.County = row[ 31];
        x.CntySrce = row[ 32];
        x.MAge = row[ 33];
        x.Dob = row[ 34];
        x.AgeDif = row[ 35];
        x.Race = row[ 36];
        x.Race_Oth = row[ 37];
        x.Race_Source = row[ 38];
        x.Race_OMB = row[ 39];
        x.Race_White = row[ 40];
        x.Race_Black = row[ 41];
        x.Race_AmIndAlkNat = row[ 42];
        x.Race_AsianIndian = row[ 43];
        x.Race_Chinese = row[ 44];
        x.Race_Filipino = row[ 45];
        x.Race_Japanese = row[ 46];
        x.Race_Korean = row[ 47];
        x.Race_Vietnamese = row[ 48];
        x.Race_OtherAsian = row[ 49];
        x.Race_NativeHawaiian = row[ 50];
        x.Race_GuamCham = row[ 51];
        x.Race_Samoan = row[ 52];
        x.Race_OtherPacific = row[ 53];
        x.Race_Other = row[ 54];
        x.Race_NotSpecified = row[ 55];
        x.HispOrg = row[ 56];
        x.Hisp_Oth = row[ 57];
        x.MatBplc = row[ 58];
        x.MatBplc_US = row[ 59];
        x.MatBplc_Else = row[ 60];
        x.MarStat = row[ 61];
        x.EducaTn = row[ 62];
        x.PlaceDth = row[ 63];
        x.Pnc = row[ 64];
        x.Autopsy3 = row[ 65];
        x.Height = row[ 66];
        x.WtPrePrg = row[ 67];
        x.PrevLb = row[ 68];
        x.PrvOthPg = row[ 69];
        x.PymtSrc = row[ 70];
        x.Wic = row[ 71];
        x.OutIndx = row[ 72];
        x.MultGest = row[ 73];
        x.TermProc = row[ 74];
        x.TermPro1 = row[ 75];
        x.TermPro2 = row[ 76];
        x.GestWk = row[ 77];
        x.DTerm = row[ 78];
        x.DayDif = row[ 79];
        x.CdcCod = row[ 80];
        x.Cod = row[ 81];
        x.AssoC1 = row[ 82];
        x.Acon1 = row[ 83];
        x.AssoC2 = row[ 84];
        x.Acon2 = row[ 85];
        x.AssoC3 = row[ 86];
        x.Acon3 = row[ 87];
        x.Injury = row[ 88];
        x.Drug_1 = row[ 89];
        x.Drug_2 = row[ 90];
        x.Drug_3 = row[ 91];
        x.Class = row[ 92];
        x.Coder = row[ 93];
        x.ClsMo = row[ 94];
        x.ClsYr = row[ 95];
        x.Tribe = row[ 96];
        x.Occup = row[ 97];
        x.Indust = row[ 98];
        x.Bmi = row[ 99];
        x.Ethnic_Mexican = row[100];
        x.Ethnic_PR = row[101];
        x.Ethnic_Cuban = row[102];
        x.Ethnic_Other = row[103];
        x.Drug_IV = row[104];
        x.Dod_MO = row[105];
        x.Dod_DY = row[106];
        x.Dod_YR = row[107];
        x.Dod_TM = row[108];
        x.Dob_MO = row[109];
        x.Dob_DY = row[110];
        x.Dob_YR = row[111];
        x.DTerm_MO = row[112];
        x.DTerm_DY = row[113];
        x.DTerm_YR = row[114];
        x.DTerm_TM = row[115];
        x.Review_1_By = row[116];
        x.Review_1_On = row[117];
        x.Review_1_Remarks = row[118];
        x.Remarks = row[119];
        x.Update_Remarks = row[120];
        x.Pdf_Link = row[121];
        x.Pdf_Steve_Link = row[122];
        x.Review_2_By = row[123];
        x.Review_2_On = row[124];
        x.Review_2_Remarks = row[125];
        x.dc_info_complete = row[126];
        x.dc_info_remarks = row[127];
        x.mmria_used = row[128];
        x.mmria_used_remarks = row[129];
        x.agreement_status = row[130];
        x.agreement_remarks = row[131];
        //x.fileno_dc = row[132];
        x.auxno_dc = row[133];
        x.replace_dc = row[134];
        x.void_dc = row[135];
        x.dod_mo_dc = row[136];
        x.dod_dy_dc = row[137];
        x.dod_yr_dc = row[138];
        x.tod_dc = row[139];
        x.dplace_dc = row[140];
        x.citytext_d_dc = row[141];
        x.countytext_d_dc = row[142];
        x.statetext_d_dc = row[143];
        x.zip9_d_dc = row[144];
        x.preg_dc = row[145];
        x.inact_dc = row[146];
        x.autop_dc = row[147];
        x.autopf_dc = row[148];
        x.transprt_dc = row[149];
        x.tobac_dc = row[150];
        x.manner_dc = row[151];
        x.cod1a_dc = row[152];
        x.interval1a_dc = row[153];
        x.cod1b_dc = row[154];
        x.interval1b_dc = row[155];
        x.cod1c_dc = row[156];
        x.interval1c_dc = row[157];
        x.cod1d_dc = row[158];
        x.interval1d_dc = row[159];
        x.othercondition_dc = row[160];
        x.man_uc_dc = row[161];
        x.acme_uc_dc = row[162];
        x.eac_dc = row[163];
        x.rac_dc = row[164];
        x.doi_mo_dc = row[165];
        x.doi_dy_dc = row[166];
        x.doi_yr_dc = row[167];
        x.toi_hr_dc = row[168];
        x.howinj_dc = row[169];
        x.workinj_dc = row[170];
        x.bplace_cnt_dc = row[171];
        x.bplace_st_dc = row[172];
        x.cityc_dc = row[173];
        x.citytext_r_dc = row[174];
        x.countyc_dc = row[175];
        x.countrytext_r_dc = row[176];
        x.statec_dc = row[177];
        x.statetext_r_dc = row[178];
        x.zip9_r_dc = row[179];
        x.dob_mo_dc = row[180];
        x.dob_dy_dc = row[181];
        x.dob_yr_dc = row[182];
        x.agetype_dc = row[183];
        x.age_dc = row[184];
        x.sex_dc = row[185];
        x.marital_dc = row[186];
        x.deduc_dc = row[187];
        x.indust_dc = row[188];
        x.occup_dc = row[189];
        x.armedf_dc = row[190];
        x.dethnic1_dc = row[191];
        x.dethnic2_dc = row[192];
        x.dethnic3_dc = row[193];
        x.dethnic4_dc = row[194];
        x.dethnic5_dc = row[195];
        x.race1_dc = row[196];
        x.race2_dc = row[197];
        x.race3_dc = row[198];
        x.race4_dc = row[199];
        x.race5_dc = row[200];
        x.race6_dc = row[201];
        x.race7_dc = row[202];
        x.race8_dc = row[203];
        x.race9_dc = row[204];
        x.race10_dc = row[205];
        x.race11_dc = row[206];
        x.race12_dc = row[207];
        x.race13_dc = row[208];
        x.race14_dc = row[209];
        x.race15_dc = row[210];
        x.race16_dc = row[211];
        x.race17_dc = row[212];
        x.race18_dc = row[213];
        x.race19_dc = row[214];
        x.race20_dc = row[215];
        x.race21_dc = row[216];
        x.race22_dc = row[217];
        x.race23_dc = row[218];
        x.bstate_bc = row[219];
        x.fileno_bc = row[220];
        x.auxno_bc = row[221];
        x.void_bc = row[222];
        x.replace_bc = row[223];
        x.dwgt_bc = row[224];
        x.pwgt_bc = row[225];
        x.hft_bc = row[226];
        x.hin_bc = row[227];
        x.idob_mo_bc = row[228];
        x.idob_dy_bc = row[229];
        x.idob_yr_bc = row[230];
        x.tb_bc = row[231];
        x.isex_bc = row[232];
        x.bwg_bc = row[233];
        x.owgest_bc = row[234];
        x.apgar5_bc = row[235];
        x.apgar10_bc = row[236];
        x.plur_bc = row[237];
        x.sord_bc = row[238];
        x.hosp_bc = row[239];
        x.birth_co_bc = row[240];
        x.bplace_bc = row[241];
        x.attend_bc = row[242];
        x.tran_bc = row[243];
        x.itran_bc = row[244];
        x.bfed_bc = row[245];
        x.wic_bc = row[246];
        x.pay_bc = row[247];
        x.pres_bc = row[248];
        x.rout_bc = row[249];
        x.iliv_bc = row[250];
        x.gon_bc = row[251];
        x.syph_bc = row[252];
        x.hsv_bc = row[253];
        x.cham_bc = row[254];
        x.hepb_bc = row[255];
        x.hepc_bc = row[256];
        x.cerv_bc = row[257];
        x.toc_bc = row[258];
        x.ecvs_bc = row[259];
        x.ecvf_bc = row[260];
        x.prom_bc = row[261];
        x.pric_bc = row[262];
        x.prol_bc = row[263];
        x.indl_bc = row[264];
        x.augl_bc = row[265];
        x.nvpr_bc = row[266];
        x.ster_bc = row[267];
        x.antb_bc = row[268];
        x.chor_bc = row[269];
        x.mecs_bc = row[270];
        x.fint_bc = row[271];
        x.esan_bc = row[272];
        x.tlab_bc = row[273];
        x.mtr_bc = row[274];
        x.plac_bc = row[275];
        x.rut_bc = row[276];
        x.uhys_bc = row[277];
        x.aint_bc = row[278];
        x.uopr_bc = row[279];
        x.aven1_bc = row[280];
        x.aven6_bc = row[281];
        x.nicu_bc = row[282];
        x.surf_bc = row[283];
        x.anti_bc = row[284];
        x.seiz_bc = row[285];
        x.binj_bc = row[286];
        x.anen_bc = row[287];
        x.mnsb_bc = row[288];
        x.cchd_bc = row[289];
        x.cdh_bc = row[290];
        x.omph_bc = row[291];
        x.gast_bc = row[292];
        x.limb_bc = row[293];
        x.cl_bc = row[294];
        x.cp_bc = row[295];
        x.dowt_bc = row[296];
        x.cdit_bc = row[297];
        x.hypo_bc = row[298];
        x.dlmp_mo_bc = row[299];
        x.dlmp_dy_bc = row[300];
        x.dlmp_yr_bc = row[301];
        x.dofp_mo_bc = row[302];
        x.dofp_dy_bc = row[303];
        x.dofp_yr_bc = row[304];
        x.dolp_mo_bc = row[305];
        x.dolp_dy_bc = row[306];
        x.dolp_yr_bc = row[307];
        x.nprev_bc = row[308];
        x.plbl_bc = row[309];
        x.plbd_bc = row[310];
        x.popo_bc = row[311];
        x.mllb_bc = row[312];
        x.yllb_bc = row[313];
        x.mopo_bc = row[314];
        x.yopo_bc = row[315];
        x.cigpn_bc = row[316];
        x.cigfn_bc = row[317];
        x.cigsn_bc = row[318];
        x.cigln_bc = row[319];
        x.pdiab_bc = row[320];
        x.gdiab_bc = row[321];
        x.phype_bc = row[322];
        x.ghype_bc = row[323];
        x.ppb_bc = row[324];
        x.ppo_bc = row[325];
        x.inft_bc = row[326];
        x.pces_bc = row[327];
        x.npces_bc = row[328];
        x.ehype_bc = row[329];
        x.inft_drg_bc = row[330];
        x.inft_art_bc = row[331];
        x.citytext_bc = row[332];
        x.countytxt_bc = row[333];
        x.statetxt_bc = row[334];
        x.zipcode_bc = row[335];
        x.mbplace_st_ter_tx_bc = row[336];
        x.mbplace_cntry_tx_bc = row[337];
        x.mager_bc = row[338];
        x.mdob_mo_bc = row[339];
        x.mdob_dy_bc = row[340];
        x.mdob_yr_bc = row[341];
        x.marn_bc = row[342];
        x.ackn_bc = row[343];
        x.meduc_bc = row[344];
        x.mom_in_t_bc = row[345];
        x.mom_oc_t_bc = row[346];
        x.methnic1_bc = row[347];
        x.methnic2_bc = row[348];
        x.methnic3_bc = row[349];
        x.methnic4_bc = row[350];
        x.methnic5_bc = row[351];
        x.mrace1_bc = row[352];
        x.mrace2_bc = row[353];
        x.mrace3_bc = row[354];
        x.mrace4_bc = row[355];
        x.mrace5_bc = row[356];
        x.mrace6_bc = row[357];
        x.mrace7_bc = row[358];
        x.mrace8_bc = row[359];
        x.mrace9_bc = row[360];
        x.mrace10_bc = row[361];
        x.mrace11_bc = row[362];
        x.mrace12_bc = row[363];
        x.mrace13_bc = row[364];
        x.mrace14_bc = row[365];
        x.mrace15_bc = row[366];
        x.mrace16_bc = row[367];
        x.mrace17_bc = row[368];
        x.mrace18_bc = row[369];
        x.mrace19_bc = row[370];
        x.mrace20_bc = row[371];
        x.mrace21_bc = row[372];
        x.mrace22_bc = row[373];
        x.mrace23_bc = row[374];
        x.fager_bc = row[375];
        x.dad_in_t_bc = row[376];
        x.dad_oc_t_bc = row[377];
        x.fbplacd_st_ter_c_bc = row[378];
        x.fbplace_cnt_c_bc = row[379];
        x.dstate_fdc = row[380];
        //x.fileno_fdc = row[381];
        x.auxno_fdc = row[382];
        x.void_fdc = row[383];
        x.replace_fdc = row[384];
        x.fdod_mo_fdc = row[385];
        x.fdod_dy_fdc = row[386];
        x.fdod_yr_fdc = row[387];
        x.td_fdc = row[388];
        x.dwgt_fdc = row[389];
        x.pwgt_fdc = row[390];
        x.hft_fdc = row[391];
        x.hin_fdc = row[392];
        x.fsex_fdc = row[393];
        x.fwg_fdc = row[394];
        x.owgest_fdc = row[395];
        x.plur_fdc = row[396];
        x.sord_fdc = row[397];
        x.hosp_d_fdc = row[398];
        x.cnty_d_fdc = row[399];
        x.city_d_fdc = row[400];
        x.attend_fdc = row[401];
        x.tran_fdc = row[402];
        x.wic_fdc = row[403];
        x.pres_fdc = row[404];
        x.rout_fdc = row[405];
        x.gon_fdc = row[406];
        x.syph_fdc = row[407];
        x.hsv_fdc = row[408];
        x.cham_fdc = row[409];
        x.lm_fdc = row[410];
        x.gbs_fdc = row[411];
        x.cmv_fdc = row[412];
        x.b19_fdc = row[413];
        x.toxo_fdc = row[414];
        x.hsv1_fdc = row[415];
        x.hiv_fdc = row[416];
        x.tlab_fdc = row[417];
        x.otheri_fdc = row[418];
        x.mtr_fdc = row[419];
        x.plac_fdc = row[420];
        x.rut_fdc = row[421];
        x.uhys_fdc = row[422];
        x.aint_fdc = row[423];
        x.uopr_fdc = row[424];
        x.anen_fdc = row[425];
        x.mnsb_fdc = row[426];
        x.cchd_fdc = row[427];
        x.cdh_fdc = row[428];
        x.omph_fdc = row[429];
        x.gast_fdc = row[430];
        x.limb_fdc = row[431];
        x.cl_fdc = row[432];
        x.cp_fdc = row[433];
        x.dowt_fdc = row[434];
        x.cdit_fdc = row[435];
        x.hypo_fdc = row[436];
        x.cod18a1_fdc = row[437];
        x.cod18a2_fdc = row[438];
        x.cod18a3_fdc = row[439];
        x.cod18a4_fdc = row[440];
        x.cod18a5_fdc = row[441];
        x.cod18a6_fdc = row[442];
        x.cod18a7_fdc = row[443];
        x.cod18a8_fdc = row[444];
        x.cod18a9_fdc = row[445];
        x.cod18a10_fdc = row[446];
        x.cod18a11_fdc = row[447];
        x.cod18a12_fdc = row[448];
        x.cod18a13_fdc = row[449];
        x.cod18a14_fdc = row[450];
        x.cod18b1_fdc = row[451];
        x.cod18b2_fdc = row[452];
        x.cod18b3_fdc = row[453];
        x.cod18b4_fdc = row[454];
        x.cod18b5_fdc = row[455];
        x.cod18b6_fdc = row[456];
        x.cod18b7_fdc = row[457];
        x.cod18b8_fdc = row[458];
        x.cod18b9_fdc = row[459];
        x.cod18b10_fdc = row[460];
        x.cod18b11_fdc = row[461];
        x.cod18b12_fdc = row[462];
        x.cod18b13_fdc = row[463];
        x.cod18b14_fdc = row[464];
        x.icod_fdc = row[465];
        x.ocod1_fdc = row[466];
        x.ocod1_fdc = row[467];
        x.ocod3_fdc = row[468];
        x.ocod4_fdc = row[469];
        x.ocod5_fdc = row[470];
        x.ocod6_fdc = row[471];
        x.ocod7_fdc = row[472];
        x.dlmp_mo_fdc = row[473];
        x.dlmp_dy_fdc = row[474];
        x.dlmp_yr_fdc = row[475];
        x.dofp_mo_fdc = row[476];
        x.dofp_dy_fdc = row[477];
        x.dofp_yr_fdc = row[478];
        x.dolp_mo_fdc = row[479];
        x.dolp_dy_fdc = row[480];
        x.dolp_yr_fdc = row[481];
        x.nprev_fdc = row[482];
        x.plbl_fdc = row[483];
        x.plbd_fdc = row[484];
        x.popo_fdc = row[485];
        x.mllb_fdc = row[486];
        x.yllb_fdc = row[487];
        x.mopo_fdc = row[488];
        x.yopo_fdc = row[489];
        x.cigpn_fdc = row[490];
        x.cigfn_fdc = row[491];
        x.cigsn_fdc = row[492];
        x.cigln_fdc = row[493];
        x.pdiab_fdc = row[494];
        x.gdiab_fdc = row[495];
        x.phype_fdc = row[496];
        x.ghype_fdc = row[497];
        x.ppb_fdc = row[498];
        x.ppo_fdc = row[499];
        x.inft_fdc = row[500];
        x.pces_fdc = row[501];
        x.npces_fdc = row[502];
        x.ehype_fdc = row[503];
        x.inft_drg_fdc = row[504];
        x.inft_art_fdc = row[505];
        x.citytxt_fdc = row[506];
        x.countytxt_fdc = row[507];
        x.statetxt_fdc = row[508];
        x.zipcode_fdc = row[509];
        x.mbplace_st_ter_txt_fdc = row[510];
        x.mbplace_cntry_txt_fdc = row[511];
        x.mager_fdc = row[512];
        x.mdob_mo_fdc = row[513];
        x.mdob_dy_fdc = row[514];
        x.mdob_yr_fdc = row[515];
        x.marn_fdc = row[516];
        x.meduc_fdc = row[517];
        x.mom_in_t_fdc = row[518];
        x.mom_oc_t_fdc = row[519];
        x.methnic1_fdc = row[520];
        x.methnic2_fdc = row[521];
        x.methnic3_fdc = row[522];
        x.methnic4_fdc = row[523];
        x.methnic5_fdc = row[524];
        x.mrace1_fdc = row[525];
        x.mrace2_fdc = row[526];
        x.mrace3_fdc = row[527];
        x.mrace4_fdc = row[528];
        x.mrace5_fdc = row[529];
        x.mrace6_fdc = row[530];
        x.mrace7_fdc = row[531];
        x.mrace8_fdc = row[532];
        x.mrace9_fdc = row[533];
        x.mrace10_fdc = row[534];
        x.mrace11_fdc = row[535];
        x.mrace12_fdc = row[536];
        x.mrace13_fdc = row[537];
        x.mrace14_fdc = row[538];
        x.mrace15_fdc = row[539];
        x.mrace16_fdc = row[540];
        x.mrace17_fdc = row[541];
        x.mrace18_fdc = row[542];
        x.mrace19_fdc = row[543];
        x.mrace20_fdc = row[544];
        x.mrace21_fdc = row[545];
        x.mrace22_fdc = row[546];
        x.mrace23_fdc = row[547];
        x.fager_fdc = row[548];
        x.dad_in_t_fdc = row[549];
        x.dad_oc_t_fdc = row[550];
        x.fbplacd_st_ter_c_fdc = row[551];
        x.fbplace_cnt_c_fdc = row[552];


        return x;
    }





}