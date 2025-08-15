using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace mmria_pmss_client.Models.IJE;
public sealed class PMSS_Other
{

    public PMSS_Other() {}
    

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
    public string Jurisdiction_Name{ get; set; }
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
    public string transprt_dc{ get; set; }
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
    public string howinj_dc{ get; set; }



    public static PMSS_Other FromList(List<string> row)
    {
        var x = new PMSS_Other();
        
        x.batch_name = row[0];
        x.fileno_dc = row[1];
        x.fileno_bc = row[2];
        x.fileno_fdc = row[3];
        x.year_birthorfetaldeath = row[4];
        x.pregcb_match = row[5];
        x.literalcod_match = row[6];
        x.icd10_match = row[7];
        x.bc_det_match = row[8];
        x.fdc_det_match = row[9];
        x.bc_prob_match = row[10];
        x.fdc_prob_match = row[11];
        x.vro_resolution_status = row[12];
        x.vro_resolution_remarks = row[13];
        x.Year = row[14];
        x.CaseNo = row[15];
        x.PMSS_State_Code = row[16];
        x.Jurisdiction = row[17];
        x.Jurisdiction_Name = row[18];
        x.Status = row[19];
        x.AmssNo = row[20];
        x.AmssRel = row[21];
        x.Dc = row[22];
        x.Dod = row[23];
        x.SourcNot = row[24];
        x.DcFile = row[25];
        x.LbFile = row[26];
        x.PregStat = row[27];
        x.PcbTime = row[28];
        x.StatDth = row[29];
        x.StatRes = row[30];
        x.ResZip = row[31];
        x.ZipSrce = row[32];
        x.County = row[33];
        x.CntySrce = row[34];
        x.MAge = row[35];
        x.Dob = row[36];
        x.AgeDif = row[37];
        x.Race = row[38];
        x.Race_Oth = row[39];
        x.Race_Source = row[40];
        x.Race_OMB = row[41];
        x.Race_White = row[42];
        x.Race_Black = row[43];
        x.Race_AmIndAlkNat = row[44];
        x.Race_AsianIndian = row[45];
        x.Race_Chinese = row[46];
        x.Race_Filipino = row[47];
        x.Race_Japanese = row[48];
        x.Race_Korean = row[49];
        x.Race_Vietnamese = row[50];
        x.Race_OtherAsian = row[51];
        x.Race_NativeHawaiian = row[52];
        x.Race_GuamCham = row[53];
        x.Race_Samoan = row[54];
        x.Race_OtherPacific = row[55];
        x.Race_Other = row[56];
        x.Race_NotSpecified = row[57];
        x.HispOrg = row[58];
        x.Hisp_Oth = row[59];
        x.MatBplc = row[60];
        x.MatBplc_US = row[61];
        x.MatBplc_Else = row[62];
        x.MarStat = row[63];
        x.EducaTn = row[64];
        x.PlaceDth = row[65];
        x.Pnc = row[66];
        x.Autopsy3 = row[67];
        x.Height = row[68];
        x.WtPrePrg = row[69];
        x.PrevLb = row[70];
        x.PrvOthPg = row[71];
        x.PymtSrc = row[72];
        x.Wic = row[73];
        x.OutIndx = row[74];
        x.MultGest = row[75];
        x.TermProc = row[76];
        x.TermPro1 = row[77];
        x.TermPro2 = row[78];
        x.GestWk = row[79];
        x.DTerm = row[80];
        x.DayDif = row[81];
        x.CdcCod = row[82];
        x.Cod = row[83];
        x.AssoC1 = row[84];
        x.Acon1 = row[85];
        x.AssoC2 = row[86];
        x.Acon2 = row[87];
        x.AssoC3 = row[88];
        x.Acon3 = row[89];
        x.Injury = row[90];
        x.Drug_1 = row[91];
        x.Drug_2 = row[92];
        x.Drug_3 = row[93];
        x.Class = row[94];
        x.Coder = row[95];
        x.ClsMo = row[96];
        x.ClsYr = row[97];
        x.Tribe = row[98];
        x.Occup = row[99];
        x.Indust = row[100];
        x.Bmi = row[101];
        x.Ethnic_Mexican = row[102];
        x.Ethnic_PR = row[103];
        x.Ethnic_Cuban = row[104];
        x.Ethnic_Other = row[105];
        x.Drug_IV = row[106];
        x.Dod_MO = row[107];
        x.Dod_DY = row[108];
        x.Dod_YR = row[109];
        x.Dod_TM = row[110];
        x.Dob_MO = row[111];
        x.Dob_DY = row[112];
        x.Dob_YR = row[113];
        x.DTerm_MO = row[114];
        x.DTerm_DY = row[115];
        x.DTerm_YR = row[116];
        x.DTerm_TM = row[117];
        x.Review_1_By = row[118];
        x.Review_1_On = row[119];
        x.Review_1_Remarks = row[120];
        x.Remarks = row[121];
        x.Update_Remarks = row[122];
        x.Pdf_Link = row[123];
        x.Pdf_Steve_Link = row[124];
        x.Review_2_By = row[125];
        x.Review_2_On = row[126];
        x.Review_2_Remarks = row[127];
        x.dc_info_complete = row[128];
        x.dc_info_remarks = row[129];
        x.mmria_used = row[130];
        x.mmria_used_remarks = row[131];
        x.agreement_status = row[132];
        x.agreement_remarks = row[133];
        x.transprt_dc = row[134];
        x.manner_dc = row[135];
        x.cod1a_dc = row[136];
        x.interval1a_dc = row[137];
        x.cod1b_dc = row[138];
        x.interval1b_dc = row[139];
        x.cod1c_dc = row[140];
        x.interval1c_dc = row[141];
        x.cod1d_dc = row[142];
        x.interval1d_dc = row[143];
        x.othercondition_dc = row[144];
        x.man_uc_dc = row[145];
        x.acme_uc_dc = row[146];
        x.eac_dc = row[147];
        x.rac_dc = row[148];
        x.howinj_dc = row[149];
        

        return x;
    }





}