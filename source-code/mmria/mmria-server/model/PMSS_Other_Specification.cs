using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace mmria_pmss_client.Models.IJE;

public interface I_PMSS_File_Specification
{

    public string this[string i] {get; }
    public bool Contains(string Value);
}

public sealed class PMSS_Other_Specification : I_PMSS_File_Specification
{
    public int Size => data.Count; 
    public string this[string i]
    {
        get { return data[i]; }
    }

    public bool Contains(string value)
    {
        return data.ContainsKey(value);
    }
    ImmutableDictionary<string, string> data = ImmutableDictionary.CreateRange
    (
        StringComparer.OrdinalIgnoreCase,
        new KeyValuePair<string,string>[] 
        {
            KeyValuePair.Create("batch_name","tracking/admin_info/batch_name"),
            KeyValuePair.Create("fileno_dc","tracking/admin_info/fileno_dc"),
            KeyValuePair.Create("fileno_bc","tracking/admin_info/fileno_bc"),
            KeyValuePair.Create("fileno_fdc", "tracking/admin_info/fileno_fdc"),
            KeyValuePair.Create("year_birthorfetaldeath", "tracking/admin_info/year_birthorfetaldeath"),
            KeyValuePair.Create("pregcb_match", "vro_case_determination/cdc_case_matching_results/pregcb_match"),
            KeyValuePair.Create("literalcod_match", "vro_case_determination/cdc_case_matching_results/literalcod_match"),
            KeyValuePair.Create("icd10_match", "vro_case_determination/cdc_case_matching_results/icd10_match"),
            KeyValuePair.Create("bc_det_match", "vro_case_determination/cdc_case_matching_results/bc_det_match"),
            KeyValuePair.Create("fdc_det_match", "vro_case_determination/cdc_case_matching_results/fdc_det_match"),
            KeyValuePair.Create("bc_prob_match", "vro_case_determination/cdc_case_matching_results/bc_prob_match"),
            KeyValuePair.Create("fdc_prob_match", "vro_case_determination/cdc_case_matching_results/fdc_prob_match"),
            KeyValuePair.Create("vro_resolution_status", "vro_case_determination/vro_update/vro_resolution_status"),
            KeyValuePair.Create("vro_resolution_remarks", "vro_case_determination/vro_update/vro_resolution_remarks"),
            KeyValuePair.Create("Year", "tracking/admin_info/track_year"),
            KeyValuePair.Create("CaseNo", "tracking/admin_info/pmssno"),
            KeyValuePair.Create("PMSS_State_Code", "tracking/admin_info/jurisdiction"),
            KeyValuePair.Create("Jurisdiction", "NOT MAPPED"),
            KeyValuePair.Create("Jurisdiction_Name","NOT MAPPED"),
            KeyValuePair.Create("Status", "tracking/admin_info/status"),
            KeyValuePair.Create("AmssNo", "tracking/q1/amssno"),
            KeyValuePair.Create("AmssRel", "tracking/q1/amssrel"),
            KeyValuePair.Create("Dc", "tracking/death_certificate_number"),
            KeyValuePair.Create("Dod", "tracking/date_of_death/dod"),
            KeyValuePair.Create("SourcNot", "tracking/sourcnot"),
            KeyValuePair.Create("DcFile", "tracking/dcfile"),
            KeyValuePair.Create("LbFile", "tracking/lbfile"),
            KeyValuePair.Create("PregStat", "tracking/q7/pregstat"),
            KeyValuePair.Create("PcbTime", "tracking/q7/pcbtime"),
            KeyValuePair.Create("StatDth", "tracking/statdth"),
            KeyValuePair.Create("StatRes", "tracking/q9/statres"),
            KeyValuePair.Create("ResZip", "tracking/q9/reszip"),
            KeyValuePair.Create("ZipSrce", "tracking/q9/zipsrce"),
            KeyValuePair.Create("County", "tracking/q9/county"),
            KeyValuePair.Create("CntySrce", "tracking/q9/cntysrce"),
            KeyValuePair.Create("MAge", "demographic/mage"),
            KeyValuePair.Create("Dob", "demographic/date_of_birth/dob"),
            KeyValuePair.Create("AgeDif", "demographic/date_of_birth/agedif"),
            KeyValuePair.Create("Race", "demographic/q12/race"),
            KeyValuePair.Create("Race_Oth", "demographic/q12/group/race_oth"),
            KeyValuePair.Create("Race_Source", "demographic/q12/group/race_source"),
            KeyValuePair.Create("Race_OMB", "demographic/q12/group/race_omb"),
            KeyValuePair.Create("Race_White", "demographic/q12/group/race_white"),
            KeyValuePair.Create("Race_Black", "demographic/q12/group/race_black"),
            KeyValuePair.Create("Race_AmIndAlkNat", "demographic/q12/group/race_amindalknat"),
            KeyValuePair.Create("Race_AsianIndian", "demographic/q12/group/race_asianindian"),
            KeyValuePair.Create("Race_Chinese", "demographic/q12/group/race_chinese"),
            KeyValuePair.Create("Race_Filipino", "demographic/q12/group/race_filipino"),
            KeyValuePair.Create("Race_Japanese", "demographic/q12/group/race_japanese"),
            KeyValuePair.Create("Race_Korean", "demographic/q12/group/race_korean"),
            KeyValuePair.Create("Race_Vietnamese", "demographic/q12/group/race_vietnamese"),
            KeyValuePair.Create("Race_OtherAsian", "demographic/q12/group/race_otherasian"),
            KeyValuePair.Create("Race_NativeHawaiian", "demographic/q12/group/race_nativehawaiian"),
            KeyValuePair.Create("Race_GuamCham", "demographic/q12/group/race_guamcham"),
            KeyValuePair.Create("Race_Samoan", "demographic/q12/group/race_samoan"),
            KeyValuePair.Create("Race_OtherPacific", "demographic/q12/group/race_otherpacific"),
            KeyValuePair.Create("Race_Other", "demographic/q12/group/race_other"),
            KeyValuePair.Create("Race_NotSpecified", "demographic/q12/group/race_notspecified"),
            KeyValuePair.Create("HispOrg", "demographic/q12/ethnicity/hisporg"),
            KeyValuePair.Create("Hisp_Oth", "demographic/q12/ethnicity/hisp_oth"),
            KeyValuePair.Create("MatBplc", "demographic/q12/matbplc"),
            KeyValuePair.Create("MatBplc_US", "demographic/q12/matbplc_us"),
            KeyValuePair.Create("MatBplc_Else", "demographic/q12/matbplc_else"),
            KeyValuePair.Create("MarStat", "demographic/marstat"),
            KeyValuePair.Create("EducaTn", "demographic/q14/educatn"),
            KeyValuePair.Create("PlaceDth", "demographic/placedth"),
            KeyValuePair.Create("Pnc", "demographic/pnc"),
            KeyValuePair.Create("Autopsy3", "demographic/autopsy3"),
            KeyValuePair.Create("Height", "demographic/height"),
            KeyValuePair.Create("WtPrePrg", "demographic/wtpreprg"),
            KeyValuePair.Create("PrevLb", "demographic/prevlb"),
            KeyValuePair.Create("PrvOthPg", "demographic/prvothpg"),
            KeyValuePair.Create("PymtSrc", "demographic/pymtsrc"),
            KeyValuePair.Create("Wic", "demographic/wic"),
            KeyValuePair.Create("OutIndx", "outcome/outindx"),
            KeyValuePair.Create("MultGest", "outcome/multgest"),
            KeyValuePair.Create("TermProc", "outcome/q25/termproc"),
            KeyValuePair.Create("TermPro1", "outcome/q25/termpro1"),
            KeyValuePair.Create("TermPro2", "outcome/q25/termpro2"),
            KeyValuePair.Create("GestWk", "outcome/gestwk"),
            KeyValuePair.Create("DTerm", "outcome/dterm_grp/dterm"),
            KeyValuePair.Create("DayDif", "outcome/dterm_grp/daydif"),
            KeyValuePair.Create("CdcCod", "cause_of_death/q28/cdccod"),
            KeyValuePair.Create("Cod", "cause_of_death/q28/cod"),
            KeyValuePair.Create("AssoC1", "cause_of_death/q29/assoc1"),
            KeyValuePair.Create("Acon1", "cause_of_death/q29/acon1"),
            KeyValuePair.Create("AssoC2", "cause_of_death/q30/assoc2"),
            KeyValuePair.Create("Acon2", "cause_of_death/q30/acon2"),
            KeyValuePair.Create("AssoC3", "cause_of_death/q31/assoc3"),
            KeyValuePair.Create("Acon3", "cause_of_death/q31/acon3"),
            KeyValuePair.Create("Injury", "cause_of_death/injury"),
            KeyValuePair.Create("Drug_1", "cause_of_death/q33/drug_1"),
            KeyValuePair.Create("Drug_2", "cause_of_death/q33/drug_2"),
            KeyValuePair.Create("Drug_3", "cause_of_death/q33/drug_3"),
            KeyValuePair.Create("Class", "cause_of_death/class"),
            KeyValuePair.Create("Coder", "cause_of_death/coder"),
            KeyValuePair.Create("ClsMo", "cause_of_death/clsmo"),
            KeyValuePair.Create("ClsYr", "cause_of_death/clsyr"),
            KeyValuePair.Create("Tribe", "demographic/q12/group/tribe"),
            KeyValuePair.Create("Occup", "demographic/q14/occup"),
            KeyValuePair.Create("Indust", "demographic/q14/indust"),
            KeyValuePair.Create("Bmi", "demographic/bmi"),
            KeyValuePair.Create("Ethnic_Mexican", "demographic/q12/ethnicity/ethnic1_mex"),
            KeyValuePair.Create("Ethnic_PR", "demographic/q12/ethnicity/ethnic2_pr"),
            KeyValuePair.Create("Ethnic_Cuban", "demographic/q12/ethnicity/ethnic3_cub"),
            KeyValuePair.Create("Ethnic_Other", "demographic/q12/ethnicity/ethnic4_other"),
            KeyValuePair.Create("Drug_IV", "cause_of_death/q33/drug_iv"),
            KeyValuePair.Create("Dod_MO", "tracking/date_of_death/month"),
            KeyValuePair.Create("Dod_DY", "tracking/date_of_death/day"),
            KeyValuePair.Create("Dod_YR", "tracking/date_of_death/year"),
            KeyValuePair.Create("Dod_TM", "tracking/date_of_death/time_of_death"),
            KeyValuePair.Create("Dob_MO", "demographic/date_of_birth/month"),
            KeyValuePair.Create("Dob_DY", "demographic/date_of_birth/day"),
            KeyValuePair.Create("Dob_YR", "demographic/date_of_birth/year"),
            KeyValuePair.Create("DTerm_MO", "outcome/dterm_grp/dterm_mo"),
            KeyValuePair.Create("DTerm_DY", "outcome/dterm_grp/dterm_dy"),
            KeyValuePair.Create("DTerm_YR", "outcome/dterm_grp/dterm_yr"),
            KeyValuePair.Create("DTerm_TM", "outcome/dterm_grp/dterm_tm"),
            KeyValuePair.Create("Review_1_By", "preparer_remarks/preparer_grp/review_1_by"),
            KeyValuePair.Create("Review_1_On", "preparer_remarks/preparer_grp/review_1_on"),
            KeyValuePair.Create("Review_1_Remarks", "preparer_remarks/preparer_grp/review_1_remarks"),
            KeyValuePair.Create("Remarks", "preparer_remarks/remarks_grp/remarks"),
            KeyValuePair.Create("Update_Remarks", "preparer_remarks/remarks_grp/update_remarks"),
            KeyValuePair.Create("Pdf_Link", "preparer_remarks/pdf_grp/pdf_link"),
            KeyValuePair.Create("Pdf_Steve_Link", "preparer_remarks/pdf_grp/pdf_steve_link"),
            KeyValuePair.Create("Review_2_By", "committee_review/reviewer_grp/review_2_by"),
            KeyValuePair.Create("Review_2_On", "committee_review/reviewer_grp/review_2_on"),
            KeyValuePair.Create("Review_2_Remarks", "committee_review/reviewer_grp/review_2_remarks"),
            KeyValuePair.Create("dc_info_complete", "committee_review/rev_assessment_grp/dc_info_complete"),
            KeyValuePair.Create("dc_info_remarks", "committee_review/rev_assessment_grp/dc_info_remarks"),
            KeyValuePair.Create("mmria_used", "committee_review/rev_assessment_grp/mmria_used"),
            KeyValuePair.Create("mmria_used_remarks", "committee_review/rev_assessment_grp/mmria_used_remarks"),
            KeyValuePair.Create("agreement_status", "committee_review/agreement_grp/agreement_status"),
            KeyValuePair.Create("agreement_remarks", "committee_review/agreement_grp/agreement_remarks"),
            KeyValuePair.Create("transprt_dc", "ije_dc/death_info/transprt_dc"),
            KeyValuePair.Create("manner_dc", "ije_dc/cause_details/manner_dc"),
            KeyValuePair.Create("cod1a_dc", "ije_dc/cause_details/cod1a_dc"),
            KeyValuePair.Create("interval1a_dc", "ije_dc/cause_details/interval1a_dc"),
            KeyValuePair.Create("cod1b_dc", "ije_dc/cause_details/cod1b_dc"),
            KeyValuePair.Create("interval1b_dc", "ije_dc/cause_details/interval1b_dc"),
            KeyValuePair.Create("cod1c_dc", "ije_dc/cause_details/cod1c_dc"),
            KeyValuePair.Create("interval1c_dc", "ije_dc/cause_details/interval1c_dc"),
            KeyValuePair.Create("cod1d_dc", "ije_dc/cause_details/cod1d_dc"),
            KeyValuePair.Create("interval1d_dc", "ije_dc/cause_details/interval1d_dc"),
            KeyValuePair.Create("othercondition_dc", "ije_dc/cause_details/othercondition_dc"),
            KeyValuePair.Create("man_uc_dc", "ije_dc/cause_details/man_uc_dc"),
            KeyValuePair.Create("acme_uc_dc", "ije_dc/cause_details/acme_uc_dc"),
            KeyValuePair.Create("eac_dc", "ije_dc/cause_details/eac_dc"),
            KeyValuePair.Create("rac_dc", "ije_dc/cause_details/rac_dc"),
            KeyValuePair.Create("howinj_dc", "ije_dc/injury_details/howinj_dc"),

            KeyValuePair.Create("vro_is_checkbox_correct", "vro_case_determination/vro_update/vro_is_checkbox_correct"),
            KeyValuePair.Create("vro_duration_endpreg_death", "vro_case_determination/vro_update/vro_duration_endpreg_death"),
            KeyValuePair.Create("vro_file_no_of_linked_lbfd", "vro_case_determination/vro_update/vro_file_no_of_linked_lbfd"),
            KeyValuePair.Create("Race_OtherAsian_Literal", "demographic/q12/group/race_otherasian_literal"),
            KeyValuePair.Create("Race_OtherPacific_Literal", "demographic/q12/group/race_otherpacific_literal"),
            KeyValuePair.Create("MatBplc_Else_Literal", "demographic/q12/matbplc_else_literal")




    });




}