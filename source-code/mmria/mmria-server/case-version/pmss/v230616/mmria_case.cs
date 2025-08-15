
using System;
using System.Collections.Generic;

namespace mmria.case_version.pmss.v230616;

public sealed class _A94E11FE6E84D88779FA7395D9C34DF5 : IConvertDictionary
{
	public _A94E11FE6E84D88779FA7395D9C34DF5()
	{
	}
	public string amss_folder { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		amss_folder = mmria_case.GetStringField(p_value, "amss_folder", "amss_tracking/folder_grp/amss_folder");
	}
}

public sealed class _4C1515801B98326EC6AEBA7A057CE829 : IConvertDictionary
{
	public _4C1515801B98326EC6AEBA7A057CE829()
	{
	}
	public string classification_diagnosis { get; set; }
	public string remarks { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		classification_diagnosis = mmria_case.GetStringListField(p_value, "classification_diagnosis", "amss_tracking/assessment_grp/classification_diagnosis");
		remarks = mmria_case.GetTextAreaField(p_value, "remarks", "amss_tracking/assessment_grp/remarks");
	}
}

public sealed class _783B66841FE95D0D97C64EE944131B93 : IConvertDictionary
{
	public _783B66841FE95D0D97C64EE944131B93()
	{
	}
	public string amss_status { get; set; }
	public DateOnly? case_rcvd_on { get; set; }
	public DateOnly? mr_rcvd_on { get; set; }
	public DateOnly? autopsy_rcvd_on { get; set; }
	public DateOnly? file_closed_on { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		amss_status = mmria_case.GetStringListField(p_value, "amss_status", "amss_tracking/admin_grp/amss_status");
		case_rcvd_on = mmria_case.GetDateField(p_value, "case_rcvd_on", "amss_tracking/admin_grp/case_rcvd_on");
		mr_rcvd_on = mmria_case.GetDateField(p_value, "mr_rcvd_on", "amss_tracking/admin_grp/mr_rcvd_on");
		autopsy_rcvd_on = mmria_case.GetDateField(p_value, "autopsy_rcvd_on", "amss_tracking/admin_grp/autopsy_rcvd_on");
		file_closed_on = mmria_case.GetDateField(p_value, "file_closed_on", "amss_tracking/admin_grp/file_closed_on");
	}
}

public sealed class _8355BA227D01C14C5E1E0C172277D46E : IConvertDictionary
{
	public _8355BA227D01C14C5E1E0C172277D46E()
	{
		admin_grp = new ();
		assessment_grp = new ();
		folder_grp = new ();
	}
	public _783B66841FE95D0D97C64EE944131B93 admin_grp{ get;set;}
	public _4C1515801B98326EC6AEBA7A057CE829 assessment_grp{ get;set;}
	public _A94E11FE6E84D88779FA7395D9C34DF5 folder_grp{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		admin_grp = mmria_case.GetGroupField<_783B66841FE95D0D97C64EE944131B93>(p_value, "admin_grp", "amss_tracking/admin_grp");
		assessment_grp = mmria_case.GetGroupField<_4C1515801B98326EC6AEBA7A057CE829>(p_value, "assessment_grp", "amss_tracking/assessment_grp");
		folder_grp = mmria_case.GetGroupField<_A94E11FE6E84D88779FA7395D9C34DF5>(p_value, "folder_grp", "amss_tracking/folder_grp");
	}
}

public sealed class _D4A1ABB625E70C7238A66CD237089F09 : IConvertDictionary
{
	public _D4A1ABB625E70C7238A66CD237089F09()
	{
	}
	public string mbplace_st_ter_txt_fdc { get; set; }
	public string mbplace_cntry_txt_fdc { get; set; }
	public string mager_fdc { get; set; }
	public string mdob_mo_fdc { get; set; }
	public string mdob_dy_fdc { get; set; }
	public string mdob_yr_fdc { get; set; }
	public string marn_fdc { get; set; }
	public double? meduc_fdc { get; set; }
	public string mom_in_t_fdc { get; set; }
	public string mom_oc_t_fdc { get; set; }
	public string methnic1_fdc { get; set; }
	public string methnic2_fdc { get; set; }
	public string methnic3_fdc { get; set; }
	public string methnic4_fdc { get; set; }
	public string methnic5_fdc { get; set; }
	public string mrace1_fdc { get; set; }
	public string mrace2_fdc { get; set; }
	public string mrace3_fdc { get; set; }
	public string mrace4_fdc { get; set; }
	public string mrace5_fdc { get; set; }
	public string mrace6_fdc { get; set; }
	public string mrace7_fdc { get; set; }
	public string mrace8_fdc { get; set; }
	public string mrace9_fdc { get; set; }
	public string mrace10_fdc { get; set; }
	public string mrace11_fdc { get; set; }
	public string mrace12_fdc { get; set; }
	public string mrace13_fdc { get; set; }
	public string mrace14_fdc { get; set; }
	public string mrace15_fdc { get; set; }
	public string mrace16_fdc { get; set; }
	public string mrace17_fdc { get; set; }
	public string mrace18_fdc { get; set; }
	public string mrace19_fdc { get; set; }
	public string mrace20_fdc { get; set; }
	public string mrace21_fdc { get; set; }
	public string mrace22_fdc { get; set; }
	public string mrace23_fdc { get; set; }
	public string fager_fdc { get; set; }
	public string dad_in_t_fdc { get; set; }
	public string dad_oc_t_fdc { get; set; }
	public string fbplacd_st_ter_c_fdc { get; set; }
	public string fbplace_cnt_c_fdc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		mbplace_st_ter_txt_fdc = mmria_case.GetStringField(p_value, "mbplace_st_ter_txt_fdc", "ije_fetaldc/demog_details/mbplace_st_ter_txt_fdc");
		mbplace_cntry_txt_fdc = mmria_case.GetStringField(p_value, "mbplace_cntry_txt_fdc", "ije_fetaldc/demog_details/mbplace_cntry_txt_fdc");
		mager_fdc = mmria_case.GetStringField(p_value, "mager_fdc", "ije_fetaldc/demog_details/mager_fdc");
		mdob_mo_fdc = mmria_case.GetStringField(p_value, "mdob_mo_fdc", "ije_fetaldc/demog_details/mdob_mo_fdc");
		mdob_dy_fdc = mmria_case.GetStringField(p_value, "mdob_dy_fdc", "ije_fetaldc/demog_details/mdob_dy_fdc");
		mdob_yr_fdc = mmria_case.GetStringField(p_value, "mdob_yr_fdc", "ije_fetaldc/demog_details/mdob_yr_fdc");
		marn_fdc = mmria_case.GetStringField(p_value, "marn_fdc", "ije_fetaldc/demog_details/marn_fdc");
		meduc_fdc = mmria_case.GetNumberListField(p_value, "meduc_fdc", "ije_fetaldc/demog_details/meduc_fdc");
		mom_in_t_fdc = mmria_case.GetStringField(p_value, "mom_in_t_fdc", "ije_fetaldc/demog_details/mom_in_t_fdc");
		mom_oc_t_fdc = mmria_case.GetStringField(p_value, "mom_oc_t_fdc", "ije_fetaldc/demog_details/mom_oc_t_fdc");
		methnic1_fdc = mmria_case.GetStringField(p_value, "methnic1_fdc", "ije_fetaldc/demog_details/methnic1_fdc");
		methnic2_fdc = mmria_case.GetStringField(p_value, "methnic2_fdc", "ije_fetaldc/demog_details/methnic2_fdc");
		methnic3_fdc = mmria_case.GetStringField(p_value, "methnic3_fdc", "ije_fetaldc/demog_details/methnic3_fdc");
		methnic4_fdc = mmria_case.GetStringField(p_value, "methnic4_fdc", "ije_fetaldc/demog_details/methnic4_fdc");
		methnic5_fdc = mmria_case.GetStringField(p_value, "methnic5_fdc", "ije_fetaldc/demog_details/methnic5_fdc");
		mrace1_fdc = mmria_case.GetStringField(p_value, "mrace1_fdc", "ije_fetaldc/demog_details/mrace1_fdc");
		mrace2_fdc = mmria_case.GetStringField(p_value, "mrace2_fdc", "ije_fetaldc/demog_details/mrace2_fdc");
		mrace3_fdc = mmria_case.GetStringField(p_value, "mrace3_fdc", "ije_fetaldc/demog_details/mrace3_fdc");
		mrace4_fdc = mmria_case.GetStringField(p_value, "mrace4_fdc", "ije_fetaldc/demog_details/mrace4_fdc");
		mrace5_fdc = mmria_case.GetStringField(p_value, "mrace5_fdc", "ije_fetaldc/demog_details/mrace5_fdc");
		mrace6_fdc = mmria_case.GetStringField(p_value, "mrace6_fdc", "ije_fetaldc/demog_details/mrace6_fdc");
		mrace7_fdc = mmria_case.GetStringField(p_value, "mrace7_fdc", "ije_fetaldc/demog_details/mrace7_fdc");
		mrace8_fdc = mmria_case.GetStringField(p_value, "mrace8_fdc", "ije_fetaldc/demog_details/mrace8_fdc");
		mrace9_fdc = mmria_case.GetStringField(p_value, "mrace9_fdc", "ije_fetaldc/demog_details/mrace9_fdc");
		mrace10_fdc = mmria_case.GetStringField(p_value, "mrace10_fdc", "ije_fetaldc/demog_details/mrace10_fdc");
		mrace11_fdc = mmria_case.GetStringField(p_value, "mrace11_fdc", "ije_fetaldc/demog_details/mrace11_fdc");
		mrace12_fdc = mmria_case.GetStringField(p_value, "mrace12_fdc", "ije_fetaldc/demog_details/mrace12_fdc");
		mrace13_fdc = mmria_case.GetStringField(p_value, "mrace13_fdc", "ije_fetaldc/demog_details/mrace13_fdc");
		mrace14_fdc = mmria_case.GetStringField(p_value, "mrace14_fdc", "ije_fetaldc/demog_details/mrace14_fdc");
		mrace15_fdc = mmria_case.GetStringField(p_value, "mrace15_fdc", "ije_fetaldc/demog_details/mrace15_fdc");
		mrace16_fdc = mmria_case.GetStringField(p_value, "mrace16_fdc", "ije_fetaldc/demog_details/mrace16_fdc");
		mrace17_fdc = mmria_case.GetStringField(p_value, "mrace17_fdc", "ije_fetaldc/demog_details/mrace17_fdc");
		mrace18_fdc = mmria_case.GetStringField(p_value, "mrace18_fdc", "ije_fetaldc/demog_details/mrace18_fdc");
		mrace19_fdc = mmria_case.GetStringField(p_value, "mrace19_fdc", "ije_fetaldc/demog_details/mrace19_fdc");
		mrace20_fdc = mmria_case.GetStringField(p_value, "mrace20_fdc", "ije_fetaldc/demog_details/mrace20_fdc");
		mrace21_fdc = mmria_case.GetStringField(p_value, "mrace21_fdc", "ije_fetaldc/demog_details/mrace21_fdc");
		mrace22_fdc = mmria_case.GetStringField(p_value, "mrace22_fdc", "ije_fetaldc/demog_details/mrace22_fdc");
		mrace23_fdc = mmria_case.GetStringField(p_value, "mrace23_fdc", "ije_fetaldc/demog_details/mrace23_fdc");
		fager_fdc = mmria_case.GetStringField(p_value, "fager_fdc", "ije_fetaldc/demog_details/fager_fdc");
		dad_in_t_fdc = mmria_case.GetStringField(p_value, "dad_in_t_fdc", "ije_fetaldc/demog_details/dad_in_t_fdc");
		dad_oc_t_fdc = mmria_case.GetStringField(p_value, "dad_oc_t_fdc", "ije_fetaldc/demog_details/dad_oc_t_fdc");
		fbplacd_st_ter_c_fdc = mmria_case.GetStringField(p_value, "fbplacd_st_ter_c_fdc", "ije_fetaldc/demog_details/fbplacd_st_ter_c_fdc");
		fbplace_cnt_c_fdc = mmria_case.GetStringField(p_value, "fbplace_cnt_c_fdc", "ije_fetaldc/demog_details/fbplace_cnt_c_fdc");
	}
}

public sealed class _E4780B5FB842310C2083C813389FE3F9 : IConvertDictionary
{
	public _E4780B5FB842310C2083C813389FE3F9()
	{
	}
	public string citytxt_fdc { get; set; }
	public string countytxt_fdc { get; set; }
	public string statetxt_fdc { get; set; }
	public string zipcode_fdc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		citytxt_fdc = mmria_case.GetStringField(p_value, "citytxt_fdc", "ije_fetaldc/residence_mother/citytxt_fdc");
		countytxt_fdc = mmria_case.GetStringField(p_value, "countytxt_fdc", "ije_fetaldc/residence_mother/countytxt_fdc");
		statetxt_fdc = mmria_case.GetStringField(p_value, "statetxt_fdc", "ije_fetaldc/residence_mother/statetxt_fdc");
		zipcode_fdc = mmria_case.GetStringField(p_value, "zipcode_fdc", "ije_fetaldc/residence_mother/zipcode_fdc");
	}
}

public sealed class _AD97F5FC6BAFD46C4023C3E12154E822 : IConvertDictionary
{
	public _AD97F5FC6BAFD46C4023C3E12154E822()
	{
	}
	public string dlmp_mo_fdc { get; set; }
	public string dlmp_dy_fdc { get; set; }
	public string dlmp_yr_fdc { get; set; }
	public string dofp_mo_fdc { get; set; }
	public string dofp_dy_fdc { get; set; }
	public string dofp_yr_fdc { get; set; }
	public string dolp_mo_fdc { get; set; }
	public string dolp_dy_fdc { get; set; }
	public string dolp_yr_fdc { get; set; }
	public string nprev_fdc { get; set; }
	public string plbl_fdc { get; set; }
	public string plbd_fdc { get; set; }
	public string popo_fdc { get; set; }
	public string mllb_fdc { get; set; }
	public string yllb_fdc { get; set; }
	public string mopo_fdc { get; set; }
	public string yopo_fdc { get; set; }
	public string cigpn_fdc { get; set; }
	public string cigfn_fdc { get; set; }
	public string cigsn_fdc { get; set; }
	public string cigln_fdc { get; set; }
	public string pdiab_fdc { get; set; }
	public string gdiab_fdc { get; set; }
	public string phype_fdc { get; set; }
	public string ghype_fdc { get; set; }
	public string ppb_fdc { get; set; }
	public string ppo_fdc { get; set; }
	public string inft_fdc { get; set; }
	public string pces_fdc { get; set; }
	public string npces_fdc { get; set; }
	public string ehype_fdc { get; set; }
	public string inft_drg_fdc { get; set; }
	public string inft_art_fdc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dlmp_mo_fdc = mmria_case.GetStringField(p_value, "dlmp_mo_fdc", "ije_fetaldc/previous_info/dlmp_mo_fdc");
		dlmp_dy_fdc = mmria_case.GetStringField(p_value, "dlmp_dy_fdc", "ije_fetaldc/previous_info/dlmp_dy_fdc");
		dlmp_yr_fdc = mmria_case.GetStringField(p_value, "dlmp_yr_fdc", "ije_fetaldc/previous_info/dlmp_yr_fdc");
		dofp_mo_fdc = mmria_case.GetStringField(p_value, "dofp_mo_fdc", "ije_fetaldc/previous_info/dofp_mo_fdc");
		dofp_dy_fdc = mmria_case.GetStringField(p_value, "dofp_dy_fdc", "ije_fetaldc/previous_info/dofp_dy_fdc");
		dofp_yr_fdc = mmria_case.GetStringField(p_value, "dofp_yr_fdc", "ije_fetaldc/previous_info/dofp_yr_fdc");
		dolp_mo_fdc = mmria_case.GetStringField(p_value, "dolp_mo_fdc", "ije_fetaldc/previous_info/dolp_mo_fdc");
		dolp_dy_fdc = mmria_case.GetStringField(p_value, "dolp_dy_fdc", "ije_fetaldc/previous_info/dolp_dy_fdc");
		dolp_yr_fdc = mmria_case.GetStringField(p_value, "dolp_yr_fdc", "ije_fetaldc/previous_info/dolp_yr_fdc");
		nprev_fdc = mmria_case.GetStringField(p_value, "nprev_fdc", "ije_fetaldc/previous_info/nprev_fdc");
		plbl_fdc = mmria_case.GetStringField(p_value, "plbl_fdc", "ije_fetaldc/previous_info/plbl_fdc");
		plbd_fdc = mmria_case.GetStringField(p_value, "plbd_fdc", "ije_fetaldc/previous_info/plbd_fdc");
		popo_fdc = mmria_case.GetStringField(p_value, "popo_fdc", "ije_fetaldc/previous_info/popo_fdc");
		mllb_fdc = mmria_case.GetStringField(p_value, "mllb_fdc", "ije_fetaldc/previous_info/mllb_fdc");
		yllb_fdc = mmria_case.GetStringField(p_value, "yllb_fdc", "ije_fetaldc/previous_info/yllb_fdc");
		mopo_fdc = mmria_case.GetStringField(p_value, "mopo_fdc", "ije_fetaldc/previous_info/mopo_fdc");
		yopo_fdc = mmria_case.GetStringField(p_value, "yopo_fdc", "ije_fetaldc/previous_info/yopo_fdc");
		cigpn_fdc = mmria_case.GetStringField(p_value, "cigpn_fdc", "ije_fetaldc/previous_info/cigpn_fdc");
		cigfn_fdc = mmria_case.GetStringField(p_value, "cigfn_fdc", "ije_fetaldc/previous_info/cigfn_fdc");
		cigsn_fdc = mmria_case.GetStringField(p_value, "cigsn_fdc", "ije_fetaldc/previous_info/cigsn_fdc");
		cigln_fdc = mmria_case.GetStringField(p_value, "cigln_fdc", "ije_fetaldc/previous_info/cigln_fdc");
		pdiab_fdc = mmria_case.GetStringField(p_value, "pdiab_fdc", "ije_fetaldc/previous_info/pdiab_fdc");
		gdiab_fdc = mmria_case.GetStringField(p_value, "gdiab_fdc", "ije_fetaldc/previous_info/gdiab_fdc");
		phype_fdc = mmria_case.GetStringField(p_value, "phype_fdc", "ije_fetaldc/previous_info/phype_fdc");
		ghype_fdc = mmria_case.GetStringField(p_value, "ghype_fdc", "ije_fetaldc/previous_info/ghype_fdc");
		ppb_fdc = mmria_case.GetStringField(p_value, "ppb_fdc", "ije_fetaldc/previous_info/ppb_fdc");
		ppo_fdc = mmria_case.GetStringField(p_value, "ppo_fdc", "ije_fetaldc/previous_info/ppo_fdc");
		inft_fdc = mmria_case.GetStringField(p_value, "inft_fdc", "ije_fetaldc/previous_info/inft_fdc");
		pces_fdc = mmria_case.GetStringField(p_value, "pces_fdc", "ije_fetaldc/previous_info/pces_fdc");
		npces_fdc = mmria_case.GetStringField(p_value, "npces_fdc", "ije_fetaldc/previous_info/npces_fdc");
		ehype_fdc = mmria_case.GetStringField(p_value, "ehype_fdc", "ije_fetaldc/previous_info/ehype_fdc");
		inft_drg_fdc = mmria_case.GetStringField(p_value, "inft_drg_fdc", "ije_fetaldc/previous_info/inft_drg_fdc");
		inft_art_fdc = mmria_case.GetStringField(p_value, "inft_art_fdc", "ije_fetaldc/previous_info/inft_art_fdc");
	}
}

public sealed class _9667FACA2FB9D76F3195048E6B7282AB : IConvertDictionary
{
	public _9667FACA2FB9D76F3195048E6B7282AB()
	{
	}
	public string cod18a1_fdc { get; set; }
	public string cod18a2_fdc { get; set; }
	public string cod18a3_fdc { get; set; }
	public string cod18a4_fdc { get; set; }
	public string cod18a5_fdc { get; set; }
	public string cod18a6_fdc { get; set; }
	public string cod18a7_fdc { get; set; }
	public string cod18a8_fdc { get; set; }
	public string cod18a9_fdc { get; set; }
	public string cod18a10_fdc { get; set; }
	public string cod18a11_fdc { get; set; }
	public string cod18a12_fdc { get; set; }
	public string cod18a13_fdc { get; set; }
	public string cod18a14_fdc { get; set; }
	public string cod18b1_fdc { get; set; }
	public string cod18b2_fdc { get; set; }
	public string cod18b3_fdc { get; set; }
	public string cod18b4_fdc { get; set; }
	public string cod18b5_fdc { get; set; }
	public string cod18b6_fdc { get; set; }
	public string cod18b7_fdc { get; set; }
	public string cod18b8_fdc { get; set; }
	public string cod18b9_fdc { get; set; }
	public string cod18b10_fdc { get; set; }
	public string cod18b11_fdc { get; set; }
	public string cod18b12_fdc { get; set; }
	public string cod18b13_fdc { get; set; }
	public string cod18b14_fdc { get; set; }
	public string icod_fdc { get; set; }
	public string ocod1_fdc { get; set; }
	public string ocod2_fdc { get; set; }
	public string ocod3_fdc { get; set; }
	public string ocod4_fdc { get; set; }
	public string ocod5_fdc { get; set; }
	public string ocod6_fdc { get; set; }
	public string ocod7_fdc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		cod18a1_fdc = mmria_case.GetStringField(p_value, "cod18a1_fdc", "ije_fetaldc/condition_cause/cod18a1_fdc");
		cod18a2_fdc = mmria_case.GetStringField(p_value, "cod18a2_fdc", "ije_fetaldc/condition_cause/cod18a2_fdc");
		cod18a3_fdc = mmria_case.GetStringField(p_value, "cod18a3_fdc", "ije_fetaldc/condition_cause/cod18a3_fdc");
		cod18a4_fdc = mmria_case.GetStringField(p_value, "cod18a4_fdc", "ije_fetaldc/condition_cause/cod18a4_fdc");
		cod18a5_fdc = mmria_case.GetStringField(p_value, "cod18a5_fdc", "ije_fetaldc/condition_cause/cod18a5_fdc");
		cod18a6_fdc = mmria_case.GetStringField(p_value, "cod18a6_fdc", "ije_fetaldc/condition_cause/cod18a6_fdc");
		cod18a7_fdc = mmria_case.GetStringField(p_value, "cod18a7_fdc", "ije_fetaldc/condition_cause/cod18a7_fdc");
		cod18a8_fdc = mmria_case.GetStringField(p_value, "cod18a8_fdc", "ije_fetaldc/condition_cause/cod18a8_fdc");
		cod18a9_fdc = mmria_case.GetStringField(p_value, "cod18a9_fdc", "ije_fetaldc/condition_cause/cod18a9_fdc");
		cod18a10_fdc = mmria_case.GetStringField(p_value, "cod18a10_fdc", "ije_fetaldc/condition_cause/cod18a10_fdc");
		cod18a11_fdc = mmria_case.GetStringField(p_value, "cod18a11_fdc", "ije_fetaldc/condition_cause/cod18a11_fdc");
		cod18a12_fdc = mmria_case.GetStringField(p_value, "cod18a12_fdc", "ije_fetaldc/condition_cause/cod18a12_fdc");
		cod18a13_fdc = mmria_case.GetStringField(p_value, "cod18a13_fdc", "ije_fetaldc/condition_cause/cod18a13_fdc");
		cod18a14_fdc = mmria_case.GetStringField(p_value, "cod18a14_fdc", "ije_fetaldc/condition_cause/cod18a14_fdc");
		cod18b1_fdc = mmria_case.GetStringField(p_value, "cod18b1_fdc", "ije_fetaldc/condition_cause/cod18b1_fdc");
		cod18b2_fdc = mmria_case.GetStringField(p_value, "cod18b2_fdc", "ije_fetaldc/condition_cause/cod18b2_fdc");
		cod18b3_fdc = mmria_case.GetStringField(p_value, "cod18b3_fdc", "ije_fetaldc/condition_cause/cod18b3_fdc");
		cod18b4_fdc = mmria_case.GetStringField(p_value, "cod18b4_fdc", "ije_fetaldc/condition_cause/cod18b4_fdc");
		cod18b5_fdc = mmria_case.GetStringField(p_value, "cod18b5_fdc", "ije_fetaldc/condition_cause/cod18b5_fdc");
		cod18b6_fdc = mmria_case.GetStringField(p_value, "cod18b6_fdc", "ije_fetaldc/condition_cause/cod18b6_fdc");
		cod18b7_fdc = mmria_case.GetStringField(p_value, "cod18b7_fdc", "ije_fetaldc/condition_cause/cod18b7_fdc");
		cod18b8_fdc = mmria_case.GetStringField(p_value, "cod18b8_fdc", "ije_fetaldc/condition_cause/cod18b8_fdc");
		cod18b9_fdc = mmria_case.GetStringField(p_value, "cod18b9_fdc", "ije_fetaldc/condition_cause/cod18b9_fdc");
		cod18b10_fdc = mmria_case.GetStringField(p_value, "cod18b10_fdc", "ije_fetaldc/condition_cause/cod18b10_fdc");
		cod18b11_fdc = mmria_case.GetStringField(p_value, "cod18b11_fdc", "ije_fetaldc/condition_cause/cod18b11_fdc");
		cod18b12_fdc = mmria_case.GetStringField(p_value, "cod18b12_fdc", "ije_fetaldc/condition_cause/cod18b12_fdc");
		cod18b13_fdc = mmria_case.GetStringField(p_value, "cod18b13_fdc", "ije_fetaldc/condition_cause/cod18b13_fdc");
		cod18b14_fdc = mmria_case.GetStringField(p_value, "cod18b14_fdc", "ije_fetaldc/condition_cause/cod18b14_fdc");
		icod_fdc = mmria_case.GetStringField(p_value, "icod_fdc", "ije_fetaldc/condition_cause/icod_fdc");
		ocod1_fdc = mmria_case.GetStringField(p_value, "ocod1_fdc", "ije_fetaldc/condition_cause/ocod1_fdc");
		ocod2_fdc = mmria_case.GetStringField(p_value, "ocod2_fdc", "ije_fetaldc/condition_cause/ocod2_fdc");
		ocod3_fdc = mmria_case.GetStringField(p_value, "ocod3_fdc", "ije_fetaldc/condition_cause/ocod3_fdc");
		ocod4_fdc = mmria_case.GetStringField(p_value, "ocod4_fdc", "ije_fetaldc/condition_cause/ocod4_fdc");
		ocod5_fdc = mmria_case.GetStringField(p_value, "ocod5_fdc", "ije_fetaldc/condition_cause/ocod5_fdc");
		ocod6_fdc = mmria_case.GetStringField(p_value, "ocod6_fdc", "ije_fetaldc/condition_cause/ocod6_fdc");
		ocod7_fdc = mmria_case.GetStringField(p_value, "ocod7_fdc", "ije_fetaldc/condition_cause/ocod7_fdc");
	}
}

public sealed class _389EAF30BFB0B5E91DE06287A4164076 : IConvertDictionary
{
	public _389EAF30BFB0B5E91DE06287A4164076()
	{
	}
	public string fdod_mo_fdc { get; set; }
	public string fdod_dy_fdc { get; set; }
	public string fdod_yr_fdc { get; set; }
	public string td_fdc { get; set; }
	public string dwgt_fdc { get; set; }
	public string pwgt_fdc { get; set; }
	public string hft_fdc { get; set; }
	public string hin_fdc { get; set; }
	public string fsex_fdc { get; set; }
	public string fwg_fdc { get; set; }
	public string owgest_fdc { get; set; }
	public string plur_fdc { get; set; }
	public string sord_fdc { get; set; }
	public string hosp_d_fdc { get; set; }
	public string cnty_d_fdc { get; set; }
	public string city_d_fdc { get; set; }
	public double? attend_fdc { get; set; }
	public string tran_fdc { get; set; }
	public string wic_fdc { get; set; }
	public double? pres_fdc { get; set; }
	public double? rout_fdc { get; set; }
	public string gon_fdc { get; set; }
	public string syph_fdc { get; set; }
	public string hsv_fdc { get; set; }
	public string cham_fdc { get; set; }
	public string lm_fdc { get; set; }
	public string gbs_fdc { get; set; }
	public string cmv_fdc { get; set; }
	public string b19_fdc { get; set; }
	public string toxo_fdc { get; set; }
	public string hsv1_fdc { get; set; }
	public string hiv_fdc { get; set; }
	public string tlab_fdc { get; set; }
	public string otheri_fdc { get; set; }
	public string mtr_fdc { get; set; }
	public string plac_fdc { get; set; }
	public string rut_fdc { get; set; }
	public string uhys_fdc { get; set; }
	public string aint_fdc { get; set; }
	public string uopr_fdc { get; set; }
	public string anen_fdc { get; set; }
	public string mnsb_fdc { get; set; }
	public string cchd_fdc { get; set; }
	public string cdh_fdc { get; set; }
	public string omph_fdc { get; set; }
	public string gast_fdc { get; set; }
	public string limb_fdc { get; set; }
	public string cl_fdc { get; set; }
	public string cp_fdc { get; set; }
	public string dowt_fdc { get; set; }
	public string cdit_fdc { get; set; }
	public string hypo_fdc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		fdod_mo_fdc = mmria_case.GetStringField(p_value, "fdod_mo_fdc", "ije_fetaldc/delivery_info/fdod_mo_fdc");
		fdod_dy_fdc = mmria_case.GetStringField(p_value, "fdod_dy_fdc", "ije_fetaldc/delivery_info/fdod_dy_fdc");
		fdod_yr_fdc = mmria_case.GetStringField(p_value, "fdod_yr_fdc", "ije_fetaldc/delivery_info/fdod_yr_fdc");
		td_fdc = mmria_case.GetStringField(p_value, "td_fdc", "ije_fetaldc/delivery_info/td_fdc");
		dwgt_fdc = mmria_case.GetStringField(p_value, "dwgt_fdc", "ije_fetaldc/delivery_info/dwgt_fdc");
		pwgt_fdc = mmria_case.GetStringField(p_value, "pwgt_fdc", "ije_fetaldc/delivery_info/pwgt_fdc");
		hft_fdc = mmria_case.GetStringField(p_value, "hft_fdc", "ije_fetaldc/delivery_info/hft_fdc");
		hin_fdc = mmria_case.GetStringField(p_value, "hin_fdc", "ije_fetaldc/delivery_info/hin_fdc");
		fsex_fdc = mmria_case.GetStringField(p_value, "fsex_fdc", "ije_fetaldc/delivery_info/fsex_fdc");
		fwg_fdc = mmria_case.GetStringField(p_value, "fwg_fdc", "ije_fetaldc/delivery_info/fwg_fdc");
		owgest_fdc = mmria_case.GetStringField(p_value, "owgest_fdc", "ije_fetaldc/delivery_info/owgest_fdc");
		plur_fdc = mmria_case.GetStringField(p_value, "plur_fdc", "ije_fetaldc/delivery_info/plur_fdc");
		sord_fdc = mmria_case.GetStringField(p_value, "sord_fdc", "ije_fetaldc/delivery_info/sord_fdc");
		hosp_d_fdc = mmria_case.GetStringField(p_value, "hosp_d_fdc", "ije_fetaldc/delivery_info/hosp_d_fdc");
		cnty_d_fdc = mmria_case.GetStringField(p_value, "cnty_d_fdc", "ije_fetaldc/delivery_info/cnty_d_fdc");
		city_d_fdc = mmria_case.GetStringField(p_value, "city_d_fdc", "ije_fetaldc/delivery_info/city_d_fdc");
		attend_fdc = mmria_case.GetNumberListField(p_value, "attend_fdc", "ije_fetaldc/delivery_info/attend_fdc");
		tran_fdc = mmria_case.GetStringField(p_value, "tran_fdc", "ije_fetaldc/delivery_info/tran_fdc");
		wic_fdc = mmria_case.GetStringField(p_value, "wic_fdc", "ije_fetaldc/delivery_info/wic_fdc");
		pres_fdc = mmria_case.GetNumberListField(p_value, "pres_fdc", "ije_fetaldc/delivery_info/pres_fdc");
		rout_fdc = mmria_case.GetNumberListField(p_value, "rout_fdc", "ije_fetaldc/delivery_info/rout_fdc");
		gon_fdc = mmria_case.GetStringField(p_value, "gon_fdc", "ije_fetaldc/delivery_info/gon_fdc");
		syph_fdc = mmria_case.GetStringField(p_value, "syph_fdc", "ije_fetaldc/delivery_info/syph_fdc");
		hsv_fdc = mmria_case.GetStringField(p_value, "hsv_fdc", "ije_fetaldc/delivery_info/hsv_fdc");
		cham_fdc = mmria_case.GetStringField(p_value, "cham_fdc", "ije_fetaldc/delivery_info/cham_fdc");
		lm_fdc = mmria_case.GetStringField(p_value, "lm_fdc", "ije_fetaldc/delivery_info/lm_fdc");
		gbs_fdc = mmria_case.GetStringField(p_value, "gbs_fdc", "ije_fetaldc/delivery_info/gbs_fdc");
		cmv_fdc = mmria_case.GetStringField(p_value, "cmv_fdc", "ije_fetaldc/delivery_info/cmv_fdc");
		b19_fdc = mmria_case.GetStringField(p_value, "b19_fdc", "ije_fetaldc/delivery_info/b19_fdc");
		toxo_fdc = mmria_case.GetStringField(p_value, "toxo_fdc", "ije_fetaldc/delivery_info/toxo_fdc");
		hsv1_fdc = mmria_case.GetStringField(p_value, "hsv1_fdc", "ije_fetaldc/delivery_info/hsv1_fdc");
		hiv_fdc = mmria_case.GetStringField(p_value, "hiv_fdc", "ije_fetaldc/delivery_info/hiv_fdc");
		tlab_fdc = mmria_case.GetStringField(p_value, "tlab_fdc", "ije_fetaldc/delivery_info/tlab_fdc");
		otheri_fdc = mmria_case.GetStringField(p_value, "otheri_fdc", "ije_fetaldc/delivery_info/otheri_fdc");
		mtr_fdc = mmria_case.GetStringField(p_value, "mtr_fdc", "ije_fetaldc/delivery_info/mtr_fdc");
		plac_fdc = mmria_case.GetStringField(p_value, "plac_fdc", "ije_fetaldc/delivery_info/plac_fdc");
		rut_fdc = mmria_case.GetStringField(p_value, "rut_fdc", "ije_fetaldc/delivery_info/rut_fdc");
		uhys_fdc = mmria_case.GetStringField(p_value, "uhys_fdc", "ije_fetaldc/delivery_info/uhys_fdc");
		aint_fdc = mmria_case.GetStringField(p_value, "aint_fdc", "ije_fetaldc/delivery_info/aint_fdc");
		uopr_fdc = mmria_case.GetStringField(p_value, "uopr_fdc", "ije_fetaldc/delivery_info/uopr_fdc");
		anen_fdc = mmria_case.GetStringField(p_value, "anen_fdc", "ije_fetaldc/delivery_info/anen_fdc");
		mnsb_fdc = mmria_case.GetStringField(p_value, "mnsb_fdc", "ije_fetaldc/delivery_info/mnsb_fdc");
		cchd_fdc = mmria_case.GetStringField(p_value, "cchd_fdc", "ije_fetaldc/delivery_info/cchd_fdc");
		cdh_fdc = mmria_case.GetStringField(p_value, "cdh_fdc", "ije_fetaldc/delivery_info/cdh_fdc");
		omph_fdc = mmria_case.GetStringField(p_value, "omph_fdc", "ije_fetaldc/delivery_info/omph_fdc");
		gast_fdc = mmria_case.GetStringField(p_value, "gast_fdc", "ije_fetaldc/delivery_info/gast_fdc");
		limb_fdc = mmria_case.GetStringField(p_value, "limb_fdc", "ije_fetaldc/delivery_info/limb_fdc");
		cl_fdc = mmria_case.GetStringField(p_value, "cl_fdc", "ije_fetaldc/delivery_info/cl_fdc");
		cp_fdc = mmria_case.GetStringField(p_value, "cp_fdc", "ije_fetaldc/delivery_info/cp_fdc");
		dowt_fdc = mmria_case.GetStringField(p_value, "dowt_fdc", "ije_fetaldc/delivery_info/dowt_fdc");
		cdit_fdc = mmria_case.GetStringField(p_value, "cdit_fdc", "ije_fetaldc/delivery_info/cdit_fdc");
		hypo_fdc = mmria_case.GetStringField(p_value, "hypo_fdc", "ije_fetaldc/delivery_info/hypo_fdc");
	}
}

public sealed class _69100D16668ED65EDA2D79F3633069E1 : IConvertDictionary
{
	public _69100D16668ED65EDA2D79F3633069E1()
	{
	}
	public string dstate_fdc { get; set; }
	public string fileno_fdc { get; set; }
	public string auxno_fdc { get; set; }
	public double? void_fdc { get; set; }
	public double? replace_fdc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dstate_fdc = mmria_case.GetStringField(p_value, "dstate_fdc", "ije_fetaldc/file_info/dstate_fdc");
		fileno_fdc = mmria_case.GetStringField(p_value, "fileno_fdc", "ije_fetaldc/file_info/fileno_fdc");
		auxno_fdc = mmria_case.GetStringField(p_value, "auxno_fdc", "ije_fetaldc/file_info/auxno_fdc");
		void_fdc = mmria_case.GetNumberListField(p_value, "void_fdc", "ije_fetaldc/file_info/void_fdc");
		replace_fdc = mmria_case.GetNumberListField(p_value, "replace_fdc", "ije_fetaldc/file_info/replace_fdc");
	}
}

public sealed class _1CDCB33AD951AAC284F43FCC500F25C8 : IConvertDictionary
{
	public _1CDCB33AD951AAC284F43FCC500F25C8()
	{
		file_info = new ();
		delivery_info = new ();
		condition_cause = new ();
		previous_info = new ();
		residence_mother = new ();
		demog_details = new ();
	}
	public _69100D16668ED65EDA2D79F3633069E1 file_info{ get;set;}
	public _389EAF30BFB0B5E91DE06287A4164076 delivery_info{ get;set;}
	public _9667FACA2FB9D76F3195048E6B7282AB condition_cause{ get;set;}
	public _AD97F5FC6BAFD46C4023C3E12154E822 previous_info{ get;set;}
	public _E4780B5FB842310C2083C813389FE3F9 residence_mother{ get;set;}
	public _D4A1ABB625E70C7238A66CD237089F09 demog_details{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		file_info = mmria_case.GetGroupField<_69100D16668ED65EDA2D79F3633069E1>(p_value, "file_info", "ije_fetaldc/file_info");
		delivery_info = mmria_case.GetGroupField<_389EAF30BFB0B5E91DE06287A4164076>(p_value, "delivery_info", "ije_fetaldc/delivery_info");
		condition_cause = mmria_case.GetGroupField<_9667FACA2FB9D76F3195048E6B7282AB>(p_value, "condition_cause", "ije_fetaldc/condition_cause");
		previous_info = mmria_case.GetGroupField<_AD97F5FC6BAFD46C4023C3E12154E822>(p_value, "previous_info", "ije_fetaldc/previous_info");
		residence_mother = mmria_case.GetGroupField<_E4780B5FB842310C2083C813389FE3F9>(p_value, "residence_mother", "ije_fetaldc/residence_mother");
		demog_details = mmria_case.GetGroupField<_D4A1ABB625E70C7238A66CD237089F09>(p_value, "demog_details", "ije_fetaldc/demog_details");
	}
}

public sealed class _8FD72D7AFDA852DEFE3E53E3CEFC2B28 : IConvertDictionary
{
	public _8FD72D7AFDA852DEFE3E53E3CEFC2B28()
	{
	}
	public string mbplace_st_ter_txt_bc { get; set; }
	public string mbplace_cntry_txt_bc { get; set; }
	public string mager_bc { get; set; }
	public string mdob_mo_bc { get; set; }
	public string mdob_dy_bc { get; set; }
	public string mdob_yr_bc { get; set; }
	public string marn_bc { get; set; }
	public string ackn_bc { get; set; }
	public double? meduc_bc { get; set; }
	public string mom_in_t_bc { get; set; }
	public string mom_oc_t_bc { get; set; }
	public string methnic1_bc { get; set; }
	public string methnic2_bc { get; set; }
	public string methnic3_bc { get; set; }
	public string methnic4_bc { get; set; }
	public string methnic5_bc { get; set; }
	public string mrace1_bc { get; set; }
	public string mrace2_bc { get; set; }
	public string mrace3_bc { get; set; }
	public string mrace4_bc { get; set; }
	public string mrace5_bc { get; set; }
	public string mrace6_bc { get; set; }
	public string mrace7_bc { get; set; }
	public string mrace8_bc { get; set; }
	public string mrace9_bc { get; set; }
	public string mrace10_bc { get; set; }
	public string mrace11_bc { get; set; }
	public string mrace12_bc { get; set; }
	public string mrace13_bc { get; set; }
	public string mrace14_bc { get; set; }
	public string mrace15_bc { get; set; }
	public string mrace16_bc { get; set; }
	public string mrace17_bc { get; set; }
	public string mrace18_bc { get; set; }
	public string mrace19_bc { get; set; }
	public string mrace20_bc { get; set; }
	public string mrace21_bc { get; set; }
	public string mrace22_bc { get; set; }
	public string mrace23_bc { get; set; }
	public string fager_bc { get; set; }
	public string dad_in_t_bc { get; set; }
	public string dad_oc_t_bc { get; set; }
	public string fbplacd_st_ter_c_bc { get; set; }
	public string fbplace_cnt_c_bc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		mbplace_st_ter_txt_bc = mmria_case.GetStringField(p_value, "mbplace_st_ter_txt_bc", "ije_bc/demog_details/mbplace_st_ter_txt_bc");
		mbplace_cntry_txt_bc = mmria_case.GetStringField(p_value, "mbplace_cntry_txt_bc", "ije_bc/demog_details/mbplace_cntry_txt_bc");
		mager_bc = mmria_case.GetStringField(p_value, "mager_bc", "ije_bc/demog_details/mager_bc");
		mdob_mo_bc = mmria_case.GetStringField(p_value, "mdob_mo_bc", "ije_bc/demog_details/mdob_mo_bc");
		mdob_dy_bc = mmria_case.GetStringField(p_value, "mdob_dy_bc", "ije_bc/demog_details/mdob_dy_bc");
		mdob_yr_bc = mmria_case.GetStringField(p_value, "mdob_yr_bc", "ije_bc/demog_details/mdob_yr_bc");
		marn_bc = mmria_case.GetStringField(p_value, "marn_bc", "ije_bc/demog_details/marn_bc");
		ackn_bc = mmria_case.GetStringField(p_value, "ackn_bc", "ije_bc/demog_details/ackn_bc");
		meduc_bc = mmria_case.GetNumberListField(p_value, "meduc_bc", "ije_bc/demog_details/meduc_bc");
		mom_in_t_bc = mmria_case.GetStringField(p_value, "mom_in_t_bc", "ije_bc/demog_details/mom_in_t_bc");
		mom_oc_t_bc = mmria_case.GetStringField(p_value, "mom_oc_t_bc", "ije_bc/demog_details/mom_oc_t_bc");
		methnic1_bc = mmria_case.GetStringField(p_value, "methnic1_bc", "ije_bc/demog_details/methnic1_bc");
		methnic2_bc = mmria_case.GetStringField(p_value, "methnic2_bc", "ije_bc/demog_details/methnic2_bc");
		methnic3_bc = mmria_case.GetStringField(p_value, "methnic3_bc", "ije_bc/demog_details/methnic3_bc");
		methnic4_bc = mmria_case.GetStringField(p_value, "methnic4_bc", "ije_bc/demog_details/methnic4_bc");
		methnic5_bc = mmria_case.GetStringField(p_value, "methnic5_bc", "ije_bc/demog_details/methnic5_bc");
		mrace1_bc = mmria_case.GetStringField(p_value, "mrace1_bc", "ije_bc/demog_details/mrace1_bc");
		mrace2_bc = mmria_case.GetStringField(p_value, "mrace2_bc", "ije_bc/demog_details/mrace2_bc");
		mrace3_bc = mmria_case.GetStringField(p_value, "mrace3_bc", "ije_bc/demog_details/mrace3_bc");
		mrace4_bc = mmria_case.GetStringField(p_value, "mrace4_bc", "ije_bc/demog_details/mrace4_bc");
		mrace5_bc = mmria_case.GetStringField(p_value, "mrace5_bc", "ije_bc/demog_details/mrace5_bc");
		mrace6_bc = mmria_case.GetStringField(p_value, "mrace6_bc", "ije_bc/demog_details/mrace6_bc");
		mrace7_bc = mmria_case.GetStringField(p_value, "mrace7_bc", "ije_bc/demog_details/mrace7_bc");
		mrace8_bc = mmria_case.GetStringField(p_value, "mrace8_bc", "ije_bc/demog_details/mrace8_bc");
		mrace9_bc = mmria_case.GetStringField(p_value, "mrace9_bc", "ije_bc/demog_details/mrace9_bc");
		mrace10_bc = mmria_case.GetStringField(p_value, "mrace10_bc", "ije_bc/demog_details/mrace10_bc");
		mrace11_bc = mmria_case.GetStringField(p_value, "mrace11_bc", "ije_bc/demog_details/mrace11_bc");
		mrace12_bc = mmria_case.GetStringField(p_value, "mrace12_bc", "ije_bc/demog_details/mrace12_bc");
		mrace13_bc = mmria_case.GetStringField(p_value, "mrace13_bc", "ije_bc/demog_details/mrace13_bc");
		mrace14_bc = mmria_case.GetStringField(p_value, "mrace14_bc", "ije_bc/demog_details/mrace14_bc");
		mrace15_bc = mmria_case.GetStringField(p_value, "mrace15_bc", "ije_bc/demog_details/mrace15_bc");
		mrace16_bc = mmria_case.GetStringField(p_value, "mrace16_bc", "ije_bc/demog_details/mrace16_bc");
		mrace17_bc = mmria_case.GetStringField(p_value, "mrace17_bc", "ije_bc/demog_details/mrace17_bc");
		mrace18_bc = mmria_case.GetStringField(p_value, "mrace18_bc", "ije_bc/demog_details/mrace18_bc");
		mrace19_bc = mmria_case.GetStringField(p_value, "mrace19_bc", "ije_bc/demog_details/mrace19_bc");
		mrace20_bc = mmria_case.GetStringField(p_value, "mrace20_bc", "ije_bc/demog_details/mrace20_bc");
		mrace21_bc = mmria_case.GetStringField(p_value, "mrace21_bc", "ije_bc/demog_details/mrace21_bc");
		mrace22_bc = mmria_case.GetStringField(p_value, "mrace22_bc", "ije_bc/demog_details/mrace22_bc");
		mrace23_bc = mmria_case.GetStringField(p_value, "mrace23_bc", "ije_bc/demog_details/mrace23_bc");
		fager_bc = mmria_case.GetStringField(p_value, "fager_bc", "ije_bc/demog_details/fager_bc");
		dad_in_t_bc = mmria_case.GetStringField(p_value, "dad_in_t_bc", "ije_bc/demog_details/dad_in_t_bc");
		dad_oc_t_bc = mmria_case.GetStringField(p_value, "dad_oc_t_bc", "ije_bc/demog_details/dad_oc_t_bc");
		fbplacd_st_ter_c_bc = mmria_case.GetStringField(p_value, "fbplacd_st_ter_c_bc", "ije_bc/demog_details/fbplacd_st_ter_c_bc");
		fbplace_cnt_c_bc = mmria_case.GetStringField(p_value, "fbplace_cnt_c_bc", "ije_bc/demog_details/fbplace_cnt_c_bc");
	}
}

public sealed class _818FFD2771E439076E6802569C82B49A : IConvertDictionary
{
	public _818FFD2771E439076E6802569C82B49A()
	{
	}
	public string citytext_bc { get; set; }
	public string countytxt_bc { get; set; }
	public string statetxt_bc { get; set; }
	public string zipcode_bc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		citytext_bc = mmria_case.GetStringField(p_value, "citytext_bc", "ije_bc/residence_mother/citytext_bc");
		countytxt_bc = mmria_case.GetStringField(p_value, "countytxt_bc", "ije_bc/residence_mother/countytxt_bc");
		statetxt_bc = mmria_case.GetStringField(p_value, "statetxt_bc", "ije_bc/residence_mother/statetxt_bc");
		zipcode_bc = mmria_case.GetStringField(p_value, "zipcode_bc", "ije_bc/residence_mother/zipcode_bc");
	}
}

public sealed class _D5B9BB3F1B25D94A01BA0C635102B22A : IConvertDictionary
{
	public _D5B9BB3F1B25D94A01BA0C635102B22A()
	{
	}
	public string dlmp_mo_bc { get; set; }
	public string dlmp_dy_bc { get; set; }
	public string dlmp_yr_bc { get; set; }
	public string dofp_mo_bc { get; set; }
	public string dofp_dy_bc { get; set; }
	public string dofp_yr_bc { get; set; }
	public string dolp_mo_bc { get; set; }
	public string dolp_dy_bc { get; set; }
	public string dolp_yr_bc { get; set; }
	public string nprev_bc { get; set; }
	public string plbl_bc { get; set; }
	public string plbd_bc { get; set; }
	public string popo_bc { get; set; }
	public string mllb_bc { get; set; }
	public string yllb_bc { get; set; }
	public string mopo_bc { get; set; }
	public string yopo_bc { get; set; }
	public string cigpn_bc { get; set; }
	public string cigfn_bc { get; set; }
	public string cigsn_bc { get; set; }
	public string cigln_bc { get; set; }
	public string pdiab_bc { get; set; }
	public string gdiab_bc { get; set; }
	public string phype_bc { get; set; }
	public string ghype_bc { get; set; }
	public string ppb_bc { get; set; }
	public string ppo_bc { get; set; }
	public string inft_bc { get; set; }
	public string pces_bc { get; set; }
	public string npces_bc { get; set; }
	public string ehype_bc { get; set; }
	public string inft_drg_bc { get; set; }
	public string inft_art_bc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dlmp_mo_bc = mmria_case.GetStringField(p_value, "dlmp_mo_bc", "ije_bc/previous_info/dlmp_mo_bc");
		dlmp_dy_bc = mmria_case.GetStringField(p_value, "dlmp_dy_bc", "ije_bc/previous_info/dlmp_dy_bc");
		dlmp_yr_bc = mmria_case.GetStringField(p_value, "dlmp_yr_bc", "ije_bc/previous_info/dlmp_yr_bc");
		dofp_mo_bc = mmria_case.GetStringField(p_value, "dofp_mo_bc", "ije_bc/previous_info/dofp_mo_bc");
		dofp_dy_bc = mmria_case.GetStringField(p_value, "dofp_dy_bc", "ije_bc/previous_info/dofp_dy_bc");
		dofp_yr_bc = mmria_case.GetStringField(p_value, "dofp_yr_bc", "ije_bc/previous_info/dofp_yr_bc");
		dolp_mo_bc = mmria_case.GetStringField(p_value, "dolp_mo_bc", "ije_bc/previous_info/dolp_mo_bc");
		dolp_dy_bc = mmria_case.GetStringField(p_value, "dolp_dy_bc", "ije_bc/previous_info/dolp_dy_bc");
		dolp_yr_bc = mmria_case.GetStringField(p_value, "dolp_yr_bc", "ije_bc/previous_info/dolp_yr_bc");
		nprev_bc = mmria_case.GetStringField(p_value, "nprev_bc", "ije_bc/previous_info/nprev_bc");
		plbl_bc = mmria_case.GetStringField(p_value, "plbl_bc", "ije_bc/previous_info/plbl_bc");
		plbd_bc = mmria_case.GetStringField(p_value, "plbd_bc", "ije_bc/previous_info/plbd_bc");
		popo_bc = mmria_case.GetStringField(p_value, "popo_bc", "ije_bc/previous_info/popo_bc");
		mllb_bc = mmria_case.GetStringField(p_value, "mllb_bc", "ije_bc/previous_info/mllb_bc");
		yllb_bc = mmria_case.GetStringField(p_value, "yllb_bc", "ije_bc/previous_info/yllb_bc");
		mopo_bc = mmria_case.GetStringField(p_value, "mopo_bc", "ije_bc/previous_info/mopo_bc");
		yopo_bc = mmria_case.GetStringField(p_value, "yopo_bc", "ije_bc/previous_info/yopo_bc");
		cigpn_bc = mmria_case.GetStringField(p_value, "cigpn_bc", "ije_bc/previous_info/cigpn_bc");
		cigfn_bc = mmria_case.GetStringField(p_value, "cigfn_bc", "ije_bc/previous_info/cigfn_bc");
		cigsn_bc = mmria_case.GetStringField(p_value, "cigsn_bc", "ije_bc/previous_info/cigsn_bc");
		cigln_bc = mmria_case.GetStringField(p_value, "cigln_bc", "ije_bc/previous_info/cigln_bc");
		pdiab_bc = mmria_case.GetStringField(p_value, "pdiab_bc", "ije_bc/previous_info/pdiab_bc");
		gdiab_bc = mmria_case.GetStringField(p_value, "gdiab_bc", "ije_bc/previous_info/gdiab_bc");
		phype_bc = mmria_case.GetStringField(p_value, "phype_bc", "ije_bc/previous_info/phype_bc");
		ghype_bc = mmria_case.GetStringField(p_value, "ghype_bc", "ije_bc/previous_info/ghype_bc");
		ppb_bc = mmria_case.GetStringField(p_value, "ppb_bc", "ije_bc/previous_info/ppb_bc");
		ppo_bc = mmria_case.GetStringField(p_value, "ppo_bc", "ije_bc/previous_info/ppo_bc");
		inft_bc = mmria_case.GetStringField(p_value, "inft_bc", "ije_bc/previous_info/inft_bc");
		pces_bc = mmria_case.GetStringField(p_value, "pces_bc", "ije_bc/previous_info/pces_bc");
		npces_bc = mmria_case.GetStringField(p_value, "npces_bc", "ije_bc/previous_info/npces_bc");
		ehype_bc = mmria_case.GetStringField(p_value, "ehype_bc", "ije_bc/previous_info/ehype_bc");
		inft_drg_bc = mmria_case.GetStringField(p_value, "inft_drg_bc", "ije_bc/previous_info/inft_drg_bc");
		inft_art_bc = mmria_case.GetStringField(p_value, "inft_art_bc", "ije_bc/previous_info/inft_art_bc");
	}
}

public sealed class _47628FA77E08A185109A556F92BC79C6 : IConvertDictionary
{
	public _47628FA77E08A185109A556F92BC79C6()
	{
	}
	public string dwgt_bc { get; set; }
	public string pwgt_bc { get; set; }
	public string hft_bc { get; set; }
	public string hin_bc { get; set; }
	public string idob_mo_bc { get; set; }
	public string idob_dy_bc { get; set; }
	public string idob_yr_bc { get; set; }
	public string tb_bc { get; set; }
	public string isex_bc { get; set; }
	public string bwg_bc { get; set; }
	public string owgest_bc { get; set; }
	public string apgar5_bc { get; set; }
	public string apgar10_bc { get; set; }
	public string plur_bc { get; set; }
	public string sord_bc { get; set; }
	public string hosp_bc { get; set; }
	public string birth_co_bc { get; set; }
	public double? bplace_bc { get; set; }
	public double? attend_bc { get; set; }
	public string tran_bc { get; set; }
	public string itran_bc { get; set; }
	public string bfed_bc { get; set; }
	public string wic_bc { get; set; }
	public double? pay_bc { get; set; }
	public double? pres_bc { get; set; }
	public double? rout_bc { get; set; }
	public string iliv_bc { get; set; }
	public string gon_bc { get; set; }
	public string syph_bc { get; set; }
	public string hsv_bc { get; set; }
	public string cham_bc { get; set; }
	public string hepb_bc { get; set; }
	public string hepc_bc { get; set; }
	public string cerv_bc { get; set; }
	public string toc_bc { get; set; }
	public string ecvs_bc { get; set; }
	public string ecvf_bc { get; set; }
	public string prom_bc { get; set; }
	public string pric_bc { get; set; }
	public string prol_bc { get; set; }
	public string indl_bc { get; set; }
	public string augl_bc { get; set; }
	public string nvpr_bc { get; set; }
	public string ster_bc { get; set; }
	public string antb_bc { get; set; }
	public string chor_bc { get; set; }
	public string mecs_bc { get; set; }
	public string fint_bc { get; set; }
	public string esan_bc { get; set; }
	public string tlab_bc { get; set; }
	public string mtr_bc { get; set; }
	public string plac_bc { get; set; }
	public string rut_bc { get; set; }
	public string uhys_bc { get; set; }
	public string aint_bc { get; set; }
	public string uopr_bc { get; set; }
	public string aven1_bc { get; set; }
	public string aven6_bc { get; set; }
	public string nicu_bc { get; set; }
	public string surf_bc { get; set; }
	public string anti_bc { get; set; }
	public string seiz_bc { get; set; }
	public string binj_bc { get; set; }
	public string anen_bc { get; set; }
	public string mnsb_bc { get; set; }
	public string cchd_bc { get; set; }
	public string cdh_bc { get; set; }
	public string omph_bc { get; set; }
	public string gast_bc { get; set; }
	public string limb_bc { get; set; }
	public string cl_bc { get; set; }
	public string cp_bc { get; set; }
	public string dowt_bc { get; set; }
	public string cdit_bc { get; set; }
	public string hypo_bc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dwgt_bc = mmria_case.GetStringField(p_value, "dwgt_bc", "ije_bc/delivery_info/dwgt_bc");
		pwgt_bc = mmria_case.GetStringField(p_value, "pwgt_bc", "ije_bc/delivery_info/pwgt_bc");
		hft_bc = mmria_case.GetStringField(p_value, "hft_bc", "ije_bc/delivery_info/hft_bc");
		hin_bc = mmria_case.GetStringField(p_value, "hin_bc", "ije_bc/delivery_info/hin_bc");
		idob_mo_bc = mmria_case.GetStringField(p_value, "idob_mo_bc", "ije_bc/delivery_info/idob_mo_bc");
		idob_dy_bc = mmria_case.GetStringField(p_value, "idob_dy_bc", "ije_bc/delivery_info/idob_dy_bc");
		idob_yr_bc = mmria_case.GetStringField(p_value, "idob_yr_bc", "ije_bc/delivery_info/idob_yr_bc");
		tb_bc = mmria_case.GetStringField(p_value, "tb_bc", "ije_bc/delivery_info/tb_bc");
		isex_bc = mmria_case.GetStringField(p_value, "isex_bc", "ije_bc/delivery_info/isex_bc");
		bwg_bc = mmria_case.GetStringField(p_value, "bwg_bc", "ije_bc/delivery_info/bwg_bc");
		owgest_bc = mmria_case.GetStringField(p_value, "owgest_bc", "ije_bc/delivery_info/owgest_bc");
		apgar5_bc = mmria_case.GetStringField(p_value, "apgar5_bc", "ije_bc/delivery_info/apgar5_bc");
		apgar10_bc = mmria_case.GetStringField(p_value, "apgar10_bc", "ije_bc/delivery_info/apgar10_bc");
		plur_bc = mmria_case.GetStringField(p_value, "plur_bc", "ije_bc/delivery_info/plur_bc");
		sord_bc = mmria_case.GetStringField(p_value, "sord_bc", "ije_bc/delivery_info/sord_bc");
		hosp_bc = mmria_case.GetStringField(p_value, "hosp_bc", "ije_bc/delivery_info/hosp_bc");
		birth_co_bc = mmria_case.GetStringField(p_value, "birth_co_bc", "ije_bc/delivery_info/birth_co_bc");
		bplace_bc = mmria_case.GetNumberListField(p_value, "bplace_bc", "ije_bc/delivery_info/bplace_bc");
		attend_bc = mmria_case.GetNumberListField(p_value, "attend_bc", "ije_bc/delivery_info/attend_bc");
		tran_bc = mmria_case.GetStringField(p_value, "tran_bc", "ije_bc/delivery_info/tran_bc");
		itran_bc = mmria_case.GetStringField(p_value, "itran_bc", "ije_bc/delivery_info/itran_bc");
		bfed_bc = mmria_case.GetStringField(p_value, "bfed_bc", "ije_bc/delivery_info/bfed_bc");
		wic_bc = mmria_case.GetStringField(p_value, "wic_bc", "ije_bc/delivery_info/wic_bc");
		pay_bc = mmria_case.GetNumberListField(p_value, "pay_bc", "ije_bc/delivery_info/pay_bc");
		pres_bc = mmria_case.GetNumberListField(p_value, "pres_bc", "ije_bc/delivery_info/pres_bc");
		rout_bc = mmria_case.GetNumberListField(p_value, "rout_bc", "ije_bc/delivery_info/rout_bc");
		iliv_bc = mmria_case.GetStringField(p_value, "iliv_bc", "ije_bc/delivery_info/iliv_bc");
		gon_bc = mmria_case.GetStringField(p_value, "gon_bc", "ije_bc/delivery_info/gon_bc");
		syph_bc = mmria_case.GetStringField(p_value, "syph_bc", "ije_bc/delivery_info/syph_bc");
		hsv_bc = mmria_case.GetStringField(p_value, "hsv_bc", "ije_bc/delivery_info/hsv_bc");
		cham_bc = mmria_case.GetStringField(p_value, "cham_bc", "ije_bc/delivery_info/cham_bc");
		hepb_bc = mmria_case.GetStringField(p_value, "hepb_bc", "ije_bc/delivery_info/hepb_bc");
		hepc_bc = mmria_case.GetStringField(p_value, "hepc_bc", "ije_bc/delivery_info/hepc_bc");
		cerv_bc = mmria_case.GetStringField(p_value, "cerv_bc", "ije_bc/delivery_info/cerv_bc");
		toc_bc = mmria_case.GetStringField(p_value, "toc_bc", "ije_bc/delivery_info/toc_bc");
		ecvs_bc = mmria_case.GetStringField(p_value, "ecvs_bc", "ije_bc/delivery_info/ecvs_bc");
		ecvf_bc = mmria_case.GetStringField(p_value, "ecvf_bc", "ije_bc/delivery_info/ecvf_bc");
		prom_bc = mmria_case.GetStringField(p_value, "prom_bc", "ije_bc/delivery_info/prom_bc");
		pric_bc = mmria_case.GetStringField(p_value, "pric_bc", "ije_bc/delivery_info/pric_bc");
		prol_bc = mmria_case.GetStringField(p_value, "prol_bc", "ije_bc/delivery_info/prol_bc");
		indl_bc = mmria_case.GetStringField(p_value, "indl_bc", "ije_bc/delivery_info/indl_bc");
		augl_bc = mmria_case.GetStringField(p_value, "augl_bc", "ije_bc/delivery_info/augl_bc");
		nvpr_bc = mmria_case.GetStringField(p_value, "nvpr_bc", "ije_bc/delivery_info/nvpr_bc");
		ster_bc = mmria_case.GetStringField(p_value, "ster_bc", "ije_bc/delivery_info/ster_bc");
		antb_bc = mmria_case.GetStringField(p_value, "antb_bc", "ije_bc/delivery_info/antb_bc");
		chor_bc = mmria_case.GetStringField(p_value, "chor_bc", "ije_bc/delivery_info/chor_bc");
		mecs_bc = mmria_case.GetStringField(p_value, "mecs_bc", "ije_bc/delivery_info/mecs_bc");
		fint_bc = mmria_case.GetStringField(p_value, "fint_bc", "ije_bc/delivery_info/fint_bc");
		esan_bc = mmria_case.GetStringField(p_value, "esan_bc", "ije_bc/delivery_info/esan_bc");
		tlab_bc = mmria_case.GetStringField(p_value, "tlab_bc", "ije_bc/delivery_info/tlab_bc");
		mtr_bc = mmria_case.GetStringField(p_value, "mtr_bc", "ije_bc/delivery_info/mtr_bc");
		plac_bc = mmria_case.GetStringField(p_value, "plac_bc", "ije_bc/delivery_info/plac_bc");
		rut_bc = mmria_case.GetStringField(p_value, "rut_bc", "ije_bc/delivery_info/rut_bc");
		uhys_bc = mmria_case.GetStringField(p_value, "uhys_bc", "ije_bc/delivery_info/uhys_bc");
		aint_bc = mmria_case.GetStringField(p_value, "aint_bc", "ije_bc/delivery_info/aint_bc");
		uopr_bc = mmria_case.GetStringField(p_value, "uopr_bc", "ije_bc/delivery_info/uopr_bc");
		aven1_bc = mmria_case.GetStringField(p_value, "aven1_bc", "ije_bc/delivery_info/aven1_bc");
		aven6_bc = mmria_case.GetStringField(p_value, "aven6_bc", "ije_bc/delivery_info/aven6_bc");
		nicu_bc = mmria_case.GetStringField(p_value, "nicu_bc", "ije_bc/delivery_info/nicu_bc");
		surf_bc = mmria_case.GetStringField(p_value, "surf_bc", "ije_bc/delivery_info/surf_bc");
		anti_bc = mmria_case.GetStringField(p_value, "anti_bc", "ije_bc/delivery_info/anti_bc");
		seiz_bc = mmria_case.GetStringField(p_value, "seiz_bc", "ije_bc/delivery_info/seiz_bc");
		binj_bc = mmria_case.GetStringField(p_value, "binj_bc", "ije_bc/delivery_info/binj_bc");
		anen_bc = mmria_case.GetStringField(p_value, "anen_bc", "ije_bc/delivery_info/anen_bc");
		mnsb_bc = mmria_case.GetStringField(p_value, "mnsb_bc", "ije_bc/delivery_info/mnsb_bc");
		cchd_bc = mmria_case.GetStringField(p_value, "cchd_bc", "ije_bc/delivery_info/cchd_bc");
		cdh_bc = mmria_case.GetStringField(p_value, "cdh_bc", "ije_bc/delivery_info/cdh_bc");
		omph_bc = mmria_case.GetStringField(p_value, "omph_bc", "ije_bc/delivery_info/omph_bc");
		gast_bc = mmria_case.GetStringField(p_value, "gast_bc", "ije_bc/delivery_info/gast_bc");
		limb_bc = mmria_case.GetStringField(p_value, "limb_bc", "ije_bc/delivery_info/limb_bc");
		cl_bc = mmria_case.GetStringField(p_value, "cl_bc", "ije_bc/delivery_info/cl_bc");
		cp_bc = mmria_case.GetStringField(p_value, "cp_bc", "ije_bc/delivery_info/cp_bc");
		dowt_bc = mmria_case.GetStringField(p_value, "dowt_bc", "ije_bc/delivery_info/dowt_bc");
		cdit_bc = mmria_case.GetStringField(p_value, "cdit_bc", "ije_bc/delivery_info/cdit_bc");
		hypo_bc = mmria_case.GetStringField(p_value, "hypo_bc", "ije_bc/delivery_info/hypo_bc");
	}
}

public sealed class _D48BB1C9495CCF57A18D865394E024C5 : IConvertDictionary
{
	public _D48BB1C9495CCF57A18D865394E024C5()
	{
	}
	public string bstate_bc { get; set; }
	public string fileno_bc { get; set; }
	public string auxno_bc { get; set; }
	public double? void_bc { get; set; }
	public double? replace_bc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		bstate_bc = mmria_case.GetStringField(p_value, "bstate_bc", "ije_bc/file_info/bstate_bc");
		fileno_bc = mmria_case.GetStringField(p_value, "fileno_bc", "ije_bc/file_info/fileno_bc");
		auxno_bc = mmria_case.GetStringField(p_value, "auxno_bc", "ije_bc/file_info/auxno_bc");
		void_bc = mmria_case.GetNumberListField(p_value, "void_bc", "ije_bc/file_info/void_bc");
		replace_bc = mmria_case.GetNumberListField(p_value, "replace_bc", "ije_bc/file_info/replace_bc");
	}
}

public sealed class _380867B2F39E2C3A1183FE46EB907A2F : IConvertDictionary
{
	public _380867B2F39E2C3A1183FE46EB907A2F()
	{
		file_info = new ();
		delivery_info = new ();
		previous_info = new ();
		residence_mother = new ();
		demog_details = new ();
	}
	public _D48BB1C9495CCF57A18D865394E024C5 file_info{ get;set;}
	public _47628FA77E08A185109A556F92BC79C6 delivery_info{ get;set;}
	public _D5B9BB3F1B25D94A01BA0C635102B22A previous_info{ get;set;}
	public _818FFD2771E439076E6802569C82B49A residence_mother{ get;set;}
	public _8FD72D7AFDA852DEFE3E53E3CEFC2B28 demog_details{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		file_info = mmria_case.GetGroupField<_D48BB1C9495CCF57A18D865394E024C5>(p_value, "file_info", "ije_bc/file_info");
		delivery_info = mmria_case.GetGroupField<_47628FA77E08A185109A556F92BC79C6>(p_value, "delivery_info", "ije_bc/delivery_info");
		previous_info = mmria_case.GetGroupField<_D5B9BB3F1B25D94A01BA0C635102B22A>(p_value, "previous_info", "ije_bc/previous_info");
		residence_mother = mmria_case.GetGroupField<_818FFD2771E439076E6802569C82B49A>(p_value, "residence_mother", "ije_bc/residence_mother");
		demog_details = mmria_case.GetGroupField<_8FD72D7AFDA852DEFE3E53E3CEFC2B28>(p_value, "demog_details", "ije_bc/demog_details");
	}
}

public sealed class _0C60477171E4EA635574CFB81D5F2FB1 : IConvertDictionary
{
	public _0C60477171E4EA635574CFB81D5F2FB1()
	{
	}
	public string dob_mo_dc { get; set; }
	public string dob_dy_dc { get; set; }
	public string dob_yr_dc { get; set; }
	public double? agetype_dc { get; set; }
	public string age_dc { get; set; }
	public string sex_dc { get; set; }
	public string marital_dc { get; set; }
	public double? deduc_dc { get; set; }
	public string indust_dc { get; set; }
	public string occup_dc { get; set; }
	public string armedf_dc { get; set; }
	public string dethnic1_dc { get; set; }
	public string dethnic2_dc { get; set; }
	public string dethnic3_dc { get; set; }
	public string dethnic4_dc { get; set; }
	public string dethnic5_dc { get; set; }
	public string race1_dc { get; set; }
	public string race2_dc { get; set; }
	public string race3_dc { get; set; }
	public string race4_dc { get; set; }
	public string race5_dc { get; set; }
	public string race6_dc { get; set; }
	public string race7_dc { get; set; }
	public string race8_dc { get; set; }
	public string race9_dc { get; set; }
	public string race10_dc { get; set; }
	public string race11_dc { get; set; }
	public string race12_dc { get; set; }
	public string race13_dc { get; set; }
	public string race14_dc { get; set; }
	public string race15_dc { get; set; }
	public string race16_dc { get; set; }
	public string race17_dc { get; set; }
	public string race18_dc { get; set; }
	public string race19_dc { get; set; }
	public string race20_dc { get; set; }
	public string race21_dc { get; set; }
	public string race22_dc { get; set; }
	public string race23_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dob_mo_dc = mmria_case.GetStringField(p_value, "dob_mo_dc", "ije_dc/demog_details/dob_mo_dc");
		dob_dy_dc = mmria_case.GetStringField(p_value, "dob_dy_dc", "ije_dc/demog_details/dob_dy_dc");
		dob_yr_dc = mmria_case.GetStringField(p_value, "dob_yr_dc", "ije_dc/demog_details/dob_yr_dc");
		agetype_dc = mmria_case.GetNumberListField(p_value, "agetype_dc", "ije_dc/demog_details/agetype_dc");
		age_dc = mmria_case.GetStringField(p_value, "age_dc", "ije_dc/demog_details/age_dc");
		sex_dc = mmria_case.GetStringField(p_value, "sex_dc", "ije_dc/demog_details/sex_dc");
		marital_dc = mmria_case.GetStringListField(p_value, "marital_dc", "ije_dc/demog_details/marital_dc");
		deduc_dc = mmria_case.GetNumberListField(p_value, "deduc_dc", "ije_dc/demog_details/deduc_dc");
		indust_dc = mmria_case.GetStringField(p_value, "indust_dc", "ije_dc/demog_details/indust_dc");
		occup_dc = mmria_case.GetStringField(p_value, "occup_dc", "ije_dc/demog_details/occup_dc");
		armedf_dc = mmria_case.GetStringField(p_value, "armedf_dc", "ije_dc/demog_details/armedf_dc");
		dethnic1_dc = mmria_case.GetStringField(p_value, "dethnic1_dc", "ije_dc/demog_details/dethnic1_dc");
		dethnic2_dc = mmria_case.GetStringField(p_value, "dethnic2_dc", "ije_dc/demog_details/dethnic2_dc");
		dethnic3_dc = mmria_case.GetStringField(p_value, "dethnic3_dc", "ije_dc/demog_details/dethnic3_dc");
		dethnic4_dc = mmria_case.GetStringField(p_value, "dethnic4_dc", "ije_dc/demog_details/dethnic4_dc");
		dethnic5_dc = mmria_case.GetStringField(p_value, "dethnic5_dc", "ije_dc/demog_details/dethnic5_dc");
		race1_dc = mmria_case.GetStringField(p_value, "race1_dc", "ije_dc/demog_details/race1_dc");
		race2_dc = mmria_case.GetStringField(p_value, "race2_dc", "ije_dc/demog_details/race2_dc");
		race3_dc = mmria_case.GetStringField(p_value, "race3_dc", "ije_dc/demog_details/race3_dc");
		race4_dc = mmria_case.GetStringField(p_value, "race4_dc", "ije_dc/demog_details/race4_dc");
		race5_dc = mmria_case.GetStringField(p_value, "race5_dc", "ije_dc/demog_details/race5_dc");
		race6_dc = mmria_case.GetStringField(p_value, "race6_dc", "ije_dc/demog_details/race6_dc");
		race7_dc = mmria_case.GetStringField(p_value, "race7_dc", "ije_dc/demog_details/race7_dc");
		race8_dc = mmria_case.GetStringField(p_value, "race8_dc", "ije_dc/demog_details/race8_dc");
		race9_dc = mmria_case.GetStringField(p_value, "race9_dc", "ije_dc/demog_details/race9_dc");
		race10_dc = mmria_case.GetStringField(p_value, "race10_dc", "ije_dc/demog_details/race10_dc");
		race11_dc = mmria_case.GetStringField(p_value, "race11_dc", "ije_dc/demog_details/race11_dc");
		race12_dc = mmria_case.GetStringField(p_value, "race12_dc", "ije_dc/demog_details/race12_dc");
		race13_dc = mmria_case.GetStringField(p_value, "race13_dc", "ije_dc/demog_details/race13_dc");
		race14_dc = mmria_case.GetStringField(p_value, "race14_dc", "ije_dc/demog_details/race14_dc");
		race15_dc = mmria_case.GetStringField(p_value, "race15_dc", "ije_dc/demog_details/race15_dc");
		race16_dc = mmria_case.GetStringField(p_value, "race16_dc", "ije_dc/demog_details/race16_dc");
		race17_dc = mmria_case.GetStringField(p_value, "race17_dc", "ije_dc/demog_details/race17_dc");
		race18_dc = mmria_case.GetStringField(p_value, "race18_dc", "ije_dc/demog_details/race18_dc");
		race19_dc = mmria_case.GetStringField(p_value, "race19_dc", "ije_dc/demog_details/race19_dc");
		race20_dc = mmria_case.GetStringField(p_value, "race20_dc", "ije_dc/demog_details/race20_dc");
		race21_dc = mmria_case.GetStringField(p_value, "race21_dc", "ije_dc/demog_details/race21_dc");
		race22_dc = mmria_case.GetStringField(p_value, "race22_dc", "ije_dc/demog_details/race22_dc");
		race23_dc = mmria_case.GetStringField(p_value, "race23_dc", "ije_dc/demog_details/race23_dc");
	}
}

public sealed class _72AE0B3D12974363B2EB78B928ED1A1B : IConvertDictionary
{
	public _72AE0B3D12974363B2EB78B928ED1A1B()
	{
	}
	public string citytext_r_dc { get; set; }
	public string countyc_dc { get; set; }
	public string countrytext_r_dc { get; set; }
	public string statec_dc { get; set; }
	public string statetext_r_dc { get; set; }
	public string zip9_r_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		citytext_r_dc = mmria_case.GetStringField(p_value, "citytext_r_dc", "ije_dc/residence_mother/citytext_r_dc");
		countyc_dc = mmria_case.GetStringField(p_value, "countyc_dc", "ije_dc/residence_mother/countyc_dc");
		countrytext_r_dc = mmria_case.GetStringField(p_value, "countrytext_r_dc", "ije_dc/residence_mother/countrytext_r_dc");
		statec_dc = mmria_case.GetStringField(p_value, "statec_dc", "ije_dc/residence_mother/statec_dc");
		statetext_r_dc = mmria_case.GetStringField(p_value, "statetext_r_dc", "ije_dc/residence_mother/statetext_r_dc");
		zip9_r_dc = mmria_case.GetStringField(p_value, "zip9_r_dc", "ije_dc/residence_mother/zip9_r_dc");
	}
}

public sealed class _3DBD3CA832B86599B55F856C95673096 : IConvertDictionary
{
	public _3DBD3CA832B86599B55F856C95673096()
	{
	}
	public string bplace_cnt_dc { get; set; }
	public string bplace_st_dc { get; set; }
	public string cityc_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		bplace_cnt_dc = mmria_case.GetStringField(p_value, "bplace_cnt_dc", "ije_dc/birthplace_mother/bplace_cnt_dc");
		bplace_st_dc = mmria_case.GetStringField(p_value, "bplace_st_dc", "ije_dc/birthplace_mother/bplace_st_dc");
		cityc_dc = mmria_case.GetStringField(p_value, "cityc_dc", "ije_dc/birthplace_mother/cityc_dc");
	}
}

public sealed class _28113EC10F204BFFB381650AD2DBBFEB : IConvertDictionary
{
	public _28113EC10F204BFFB381650AD2DBBFEB()
	{
	}
	public string doi_mo_dc { get; set; }
	public string doi_dy_dc { get; set; }
	public string doi_yr_dc { get; set; }
	public string toi_hr_dc { get; set; }
	public string howinj_dc { get; set; }
	public string workinj_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		doi_mo_dc = mmria_case.GetStringField(p_value, "doi_mo_dc", "ije_dc/injury_details/doi_mo_dc");
		doi_dy_dc = mmria_case.GetStringField(p_value, "doi_dy_dc", "ije_dc/injury_details/doi_dy_dc");
		doi_yr_dc = mmria_case.GetStringField(p_value, "doi_yr_dc", "ije_dc/injury_details/doi_yr_dc");
		toi_hr_dc = mmria_case.GetStringField(p_value, "toi_hr_dc", "ije_dc/injury_details/toi_hr_dc");
		howinj_dc = mmria_case.GetStringField(p_value, "howinj_dc", "ije_dc/injury_details/howinj_dc");
		workinj_dc = mmria_case.GetStringField(p_value, "workinj_dc", "ije_dc/injury_details/workinj_dc");
	}
}

public sealed class _71782EDC626BAE6EF282A4BFA94683F0 : IConvertDictionary
{
	public _71782EDC626BAE6EF282A4BFA94683F0()
	{
	}
	public string manner_dc { get; set; }
	public string cod1a_dc { get; set; }
	public string interval1a_dc { get; set; }
	public string cod1b_dc { get; set; }
	public string interval1b_dc { get; set; }
	public string cod1c_dc { get; set; }
	public string interval1c_dc { get; set; }
	public string cod1d_dc { get; set; }
	public string interval1d_dc { get; set; }
	public string othercondition_dc { get; set; }
	public string man_uc_dc { get; set; }
	public string acme_uc_dc { get; set; }
	public string eac_dc { get; set; }
	public string rac_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		manner_dc = mmria_case.GetStringListField(p_value, "manner_dc", "ije_dc/cause_details/manner_dc");
		cod1a_dc = mmria_case.GetStringField(p_value, "cod1a_dc", "ije_dc/cause_details/cod1a_dc");
		interval1a_dc = mmria_case.GetStringField(p_value, "interval1a_dc", "ije_dc/cause_details/interval1a_dc");
		cod1b_dc = mmria_case.GetStringField(p_value, "cod1b_dc", "ije_dc/cause_details/cod1b_dc");
		interval1b_dc = mmria_case.GetStringField(p_value, "interval1b_dc", "ije_dc/cause_details/interval1b_dc");
		cod1c_dc = mmria_case.GetStringField(p_value, "cod1c_dc", "ije_dc/cause_details/cod1c_dc");
		interval1c_dc = mmria_case.GetStringField(p_value, "interval1c_dc", "ije_dc/cause_details/interval1c_dc");
		cod1d_dc = mmria_case.GetStringField(p_value, "cod1d_dc", "ije_dc/cause_details/cod1d_dc");
		interval1d_dc = mmria_case.GetStringField(p_value, "interval1d_dc", "ije_dc/cause_details/interval1d_dc");
		othercondition_dc = mmria_case.GetStringField(p_value, "othercondition_dc", "ije_dc/cause_details/othercondition_dc");
		man_uc_dc = mmria_case.GetStringField(p_value, "man_uc_dc", "ije_dc/cause_details/man_uc_dc");
		acme_uc_dc = mmria_case.GetStringField(p_value, "acme_uc_dc", "ije_dc/cause_details/acme_uc_dc");
		eac_dc = mmria_case.GetStringField(p_value, "eac_dc", "ije_dc/cause_details/eac_dc");
		rac_dc = mmria_case.GetStringField(p_value, "rac_dc", "ije_dc/cause_details/rac_dc");
	}
}

public sealed class _61B99B384D3000FB6457F422C3039D85 : IConvertDictionary
{
	public _61B99B384D3000FB6457F422C3039D85()
	{
	}
	public string dod_mo_dc { get; set; }
	public string dod_dy_dc { get; set; }
	public string dod_yr_dc { get; set; }
	public string tod_dc { get; set; }
	public double? dplace_dc { get; set; }
	public string citytext_d_dc { get; set; }
	public string countytext_d_dc { get; set; }
	public string statetext_d_dc { get; set; }
	public string zip9_d_dc { get; set; }
	public string preg_dc { get; set; }
	public string inact_dc { get; set; }
	public string autop_dc { get; set; }
	public string autopf_dc { get; set; }
	public string transprt_dc { get; set; }
	public string tobac_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dod_mo_dc = mmria_case.GetStringField(p_value, "dod_mo_dc", "ije_dc/death_info/dod_mo_dc");
		dod_dy_dc = mmria_case.GetStringField(p_value, "dod_dy_dc", "ije_dc/death_info/dod_dy_dc");
		dod_yr_dc = mmria_case.GetStringField(p_value, "dod_yr_dc", "ije_dc/death_info/dod_yr_dc");
		tod_dc = mmria_case.GetStringField(p_value, "tod_dc", "ije_dc/death_info/tod_dc");
		dplace_dc = mmria_case.GetNumberListField(p_value, "dplace_dc", "ije_dc/death_info/dplace_dc");
		citytext_d_dc = mmria_case.GetStringField(p_value, "citytext_d_dc", "ije_dc/death_info/citytext_d_dc");
		countytext_d_dc = mmria_case.GetStringField(p_value, "countytext_d_dc", "ije_dc/death_info/countytext_d_dc");
		statetext_d_dc = mmria_case.GetStringField(p_value, "statetext_d_dc", "ije_dc/death_info/statetext_d_dc");
		zip9_d_dc = mmria_case.GetStringField(p_value, "zip9_d_dc", "ije_dc/death_info/zip9_d_dc");
		preg_dc = mmria_case.GetStringField(p_value, "preg_dc", "ije_dc/death_info/preg_dc");
		inact_dc = mmria_case.GetStringField(p_value, "inact_dc", "ije_dc/death_info/inact_dc");
		autop_dc = mmria_case.GetStringField(p_value, "autop_dc", "ije_dc/death_info/autop_dc");
		autopf_dc = mmria_case.GetStringField(p_value, "autopf_dc", "ije_dc/death_info/autopf_dc");
		transprt_dc = mmria_case.GetStringListField(p_value, "transprt_dc", "ije_dc/death_info/transprt_dc");
		tobac_dc = mmria_case.GetStringField(p_value, "tobac_dc", "ije_dc/death_info/tobac_dc");
	}
}

public sealed class _D7A964C0F749BC3F6923EDAB88E6C29E : IConvertDictionary
{
	public _D7A964C0F749BC3F6923EDAB88E6C29E()
	{
	}
	public string dstate_dc { get; set; }
	public string fileno_dc { get; set; }
	public string auxno_dc { get; set; }
	public double? void_dc { get; set; }
	public double? replace_dc { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dstate_dc = mmria_case.GetStringField(p_value, "dstate_dc", "ije_dc/file_info/dstate_dc");
		fileno_dc = mmria_case.GetStringField(p_value, "fileno_dc", "ije_dc/file_info/fileno_dc");
		auxno_dc = mmria_case.GetStringField(p_value, "auxno_dc", "ije_dc/file_info/auxno_dc");
		void_dc = mmria_case.GetNumberListField(p_value, "void_dc", "ije_dc/file_info/void_dc");
		replace_dc = mmria_case.GetNumberListField(p_value, "replace_dc", "ije_dc/file_info/replace_dc");
	}
}

public sealed class _0D69863CC31E49D042EEE11D6403ED7B : IConvertDictionary
{
	public _0D69863CC31E49D042EEE11D6403ED7B()
	{
		file_info = new ();
		death_info = new ();
		cause_details = new ();
		injury_details = new ();
		birthplace_mother = new ();
		residence_mother = new ();
		demog_details = new ();
	}
	public _D7A964C0F749BC3F6923EDAB88E6C29E file_info{ get;set;}
	public _61B99B384D3000FB6457F422C3039D85 death_info{ get;set;}
	public _71782EDC626BAE6EF282A4BFA94683F0 cause_details{ get;set;}
	public _28113EC10F204BFFB381650AD2DBBFEB injury_details{ get;set;}
	public _3DBD3CA832B86599B55F856C95673096 birthplace_mother{ get;set;}
	public _72AE0B3D12974363B2EB78B928ED1A1B residence_mother{ get;set;}
	public _0C60477171E4EA635574CFB81D5F2FB1 demog_details{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		file_info = mmria_case.GetGroupField<_D7A964C0F749BC3F6923EDAB88E6C29E>(p_value, "file_info", "ije_dc/file_info");
		death_info = mmria_case.GetGroupField<_61B99B384D3000FB6457F422C3039D85>(p_value, "death_info", "ije_dc/death_info");
		cause_details = mmria_case.GetGroupField<_71782EDC626BAE6EF282A4BFA94683F0>(p_value, "cause_details", "ije_dc/cause_details");
		injury_details = mmria_case.GetGroupField<_28113EC10F204BFFB381650AD2DBBFEB>(p_value, "injury_details", "ije_dc/injury_details");
		birthplace_mother = mmria_case.GetGroupField<_3DBD3CA832B86599B55F856C95673096>(p_value, "birthplace_mother", "ije_dc/birthplace_mother");
		residence_mother = mmria_case.GetGroupField<_72AE0B3D12974363B2EB78B928ED1A1B>(p_value, "residence_mother", "ije_dc/residence_mother");
		demog_details = mmria_case.GetGroupField<_0C60477171E4EA635574CFB81D5F2FB1>(p_value, "demog_details", "ije_dc/demog_details");
	}
}

public sealed class _18CCB400C97876203A224BBC13B8453C : IConvertDictionary
{
	public _18CCB400C97876203A224BBC13B8453C()
	{
	}
	public string manner_dc_mirror { get; set; }
	public string cod1a_dc { get; set; }
	public string interval1a_dc_mirror { get; set; }
	public string cod1b_dc_mirror { get; set; }
	public string interval1b_dc_mirror { get; set; }
	public string cod1c_dc_mirror { get; set; }
	public string interval1c_dc_mirror { get; set; }
	public string cod1d_dc_mirror { get; set; }
	public string interval1d_dc_mirror { get; set; }
	public string othercondition_dc_mirror { get; set; }
	public string man_uc_dc_mirror { get; set; }
	public string acme_uc_dc_mirror { get; set; }
	public string eac_dc_mirror { get; set; }
	public string rac_dc_mirror { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		manner_dc_mirror = mmria_case.GetStringListField(p_value, "manner_dc_mirror", "vro_case_determination/cause_details_mirror/manner_dc_mirror");
		cod1a_dc = mmria_case.GetStringField(p_value, "cod1a_dc", "vro_case_determination/cause_details_mirror/cod1a_dc");
		interval1a_dc_mirror = mmria_case.GetStringField(p_value, "interval1a_dc_mirror", "vro_case_determination/cause_details_mirror/interval1a_dc_mirror");
		cod1b_dc_mirror = mmria_case.GetStringField(p_value, "cod1b_dc_mirror", "vro_case_determination/cause_details_mirror/cod1b_dc_mirror");
		interval1b_dc_mirror = mmria_case.GetStringField(p_value, "interval1b_dc_mirror", "vro_case_determination/cause_details_mirror/interval1b_dc_mirror");
		cod1c_dc_mirror = mmria_case.GetStringField(p_value, "cod1c_dc_mirror", "vro_case_determination/cause_details_mirror/cod1c_dc_mirror");
		interval1c_dc_mirror = mmria_case.GetStringField(p_value, "interval1c_dc_mirror", "vro_case_determination/cause_details_mirror/interval1c_dc_mirror");
		cod1d_dc_mirror = mmria_case.GetStringField(p_value, "cod1d_dc_mirror", "vro_case_determination/cause_details_mirror/cod1d_dc_mirror");
		interval1d_dc_mirror = mmria_case.GetStringField(p_value, "interval1d_dc_mirror", "vro_case_determination/cause_details_mirror/interval1d_dc_mirror");
		othercondition_dc_mirror = mmria_case.GetStringField(p_value, "othercondition_dc_mirror", "vro_case_determination/cause_details_mirror/othercondition_dc_mirror");
		man_uc_dc_mirror = mmria_case.GetStringField(p_value, "man_uc_dc_mirror", "vro_case_determination/cause_details_mirror/man_uc_dc_mirror");
		acme_uc_dc_mirror = mmria_case.GetStringField(p_value, "acme_uc_dc_mirror", "vro_case_determination/cause_details_mirror/acme_uc_dc_mirror");
		eac_dc_mirror = mmria_case.GetStringField(p_value, "eac_dc_mirror", "vro_case_determination/cause_details_mirror/eac_dc_mirror");
		rac_dc_mirror = mmria_case.GetStringField(p_value, "rac_dc_mirror", "vro_case_determination/cause_details_mirror/rac_dc_mirror");
	}
}

public sealed class _2A437E681C1808FE4ACB2536CF0FAAE5 : IConvertDictionary
{
	public _2A437E681C1808FE4ACB2536CF0FAAE5()
	{
	}
	public double? preg_cb_match_mirror { get; set; }
	public double? literal_cod_match_mirror { get; set; }
	public double? icd10_match_mirror { get; set; }
	public double? bc_match { get; set; }
	public double? fdc_match { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		preg_cb_match_mirror = mmria_case.GetNumberListField(p_value, "preg_cb_match_mirror", "vro_case_determination/cdc_case_matching_results/preg_cb_match_mirror");
		literal_cod_match_mirror = mmria_case.GetNumberListField(p_value, "literal_cod_match_mirror", "vro_case_determination/cdc_case_matching_results/literal_cod_match_mirror");
		icd10_match_mirror = mmria_case.GetNumberListField(p_value, "icd10_match_mirror", "vro_case_determination/cdc_case_matching_results/icd10_match_mirror");
		bc_match = mmria_case.GetNumberListField(p_value, "bc_match", "vro_case_determination/cdc_case_matching_results/bc_match");
		fdc_match = mmria_case.GetNumberListField(p_value, "fdc_match", "vro_case_determination/cdc_case_matching_results/fdc_match");
	}
}

public sealed class _35FD478CD58D3D1159A0C338470EAA77 : IConvertDictionary
{
	public _35FD478CD58D3D1159A0C338470EAA77()
	{
	}
	public string vro_resolution_status { get; set; }
	public string vro_resolution_remarks { get; set; }
	public string vro_is_checkbox_correct { get; set; }
	public string vro_duration_endpreg_death { get; set; }
	public string vro_file_no_of_linked_lbfd { get; set; }
	public string note_to_vro_mirror { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		vro_resolution_status = mmria_case.GetStringListField(p_value, "vro_resolution_status", "vro_case_determination/vro_update/vro_resolution_status");
		vro_resolution_remarks = mmria_case.GetTextAreaField(p_value, "vro_resolution_remarks", "vro_case_determination/vro_update/vro_resolution_remarks");
		vro_is_checkbox_correct = mmria_case.GetStringField(p_value, "vro_is_checkbox_correct", "vro_case_determination/vro_update/vro_is_checkbox_correct");
		vro_duration_endpreg_death = mmria_case.GetStringField(p_value, "vro_duration_endpreg_death", "vro_case_determination/vro_update/vro_duration_endpreg_death");
		vro_file_no_of_linked_lbfd = mmria_case.GetStringField(p_value, "vro_file_no_of_linked_lbfd", "vro_case_determination/vro_update/vro_file_no_of_linked_lbfd");
		note_to_vro_mirror = mmria_case.GetTextAreaField(p_value, "note_to_vro_mirror", "vro_case_determination/vro_update/note_to_vro_mirror");
	}
}

public sealed class _1E75BD636D974364B908C7C0C0510342 : IConvertDictionary
{
	public _1E75BD636D974364B908C7C0C0510342()
	{
	}
	public string dob_mo_dc_mirror { get; set; }
	public string dob_dy_dc_mirror { get; set; }
	public string dob_yr_dc_mirror { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dob_mo_dc_mirror = mmria_case.GetStringField(p_value, "dob_mo_dc_mirror", "vro_case_determination/case_identifiers/dobirth_mirror/dob_mo_dc_mirror");
		dob_dy_dc_mirror = mmria_case.GetStringField(p_value, "dob_dy_dc_mirror", "vro_case_determination/case_identifiers/dobirth_mirror/dob_dy_dc_mirror");
		dob_yr_dc_mirror = mmria_case.GetStringField(p_value, "dob_yr_dc_mirror", "vro_case_determination/case_identifiers/dobirth_mirror/dob_yr_dc_mirror");
	}
}

public sealed class _DE9C417039AD3B0AC4694BEFCD271450 : IConvertDictionary
{
	public _DE9C417039AD3B0AC4694BEFCD271450()
	{
	}
	public string dod_mo_dc_mirror { get; set; }
	public string dod_dy_dc_mirror { get; set; }
	public string dod_yr_dc_mirror { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dod_mo_dc_mirror = mmria_case.GetStringField(p_value, "dod_mo_dc_mirror", "vro_case_determination/case_identifiers/dodeath_mirror/dod_mo_dc_mirror");
		dod_dy_dc_mirror = mmria_case.GetStringField(p_value, "dod_dy_dc_mirror", "vro_case_determination/case_identifiers/dodeath_mirror/dod_dy_dc_mirror");
		dod_yr_dc_mirror = mmria_case.GetStringField(p_value, "dod_yr_dc_mirror", "vro_case_determination/case_identifiers/dodeath_mirror/dod_yr_dc_mirror");
	}
}

public sealed class _016FCDB951F95B19033708F378D04AED : IConvertDictionary
{
	public _016FCDB951F95B19033708F378D04AED()
	{
		dodeath_mirror = new ();
		dobirth_mirror = new ();
	}
	public string dstate_dc_mirror { get; set; }
	public string fileno_dc_mirror { get; set; }
	public string auxno_dc_mirror { get; set; }
	public _DE9C417039AD3B0AC4694BEFCD271450 dodeath_mirror{ get;set;}
	public _1E75BD636D974364B908C7C0C0510342 dobirth_mirror{ get;set;}
	public string fileno_bc_mirror { get; set; }
	public string fileno_fdc_mirror { get; set; }
	public string year_birthorfetaldeath_mirror { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dstate_dc_mirror = mmria_case.GetStringField(p_value, "dstate_dc_mirror", "vro_case_determination/case_identifiers/dstate_dc_mirror");
		fileno_dc_mirror = mmria_case.GetStringField(p_value, "fileno_dc_mirror", "vro_case_determination/case_identifiers/fileno_dc_mirror");
		auxno_dc_mirror = mmria_case.GetStringField(p_value, "auxno_dc_mirror", "vro_case_determination/case_identifiers/auxno_dc_mirror");
		dodeath_mirror = mmria_case.GetGroupField<_DE9C417039AD3B0AC4694BEFCD271450>(p_value, "dodeath_mirror", "vro_case_determination/case_identifiers/dodeath_mirror");
		dobirth_mirror = mmria_case.GetGroupField<_1E75BD636D974364B908C7C0C0510342>(p_value, "dobirth_mirror", "vro_case_determination/case_identifiers/dobirth_mirror");
		fileno_bc_mirror = mmria_case.GetStringField(p_value, "fileno_bc_mirror", "vro_case_determination/case_identifiers/fileno_bc_mirror");
		fileno_fdc_mirror = mmria_case.GetStringField(p_value, "fileno_fdc_mirror", "vro_case_determination/case_identifiers/fileno_fdc_mirror");
		year_birthorfetaldeath_mirror = mmria_case.GetStringField(p_value, "year_birthorfetaldeath_mirror", "vro_case_determination/case_identifiers/year_birthorfetaldeath_mirror");
	}
}

public sealed class _4A4043A4503BB4DF04BA1D8D121871FD : IConvertDictionary
{
	public _4A4043A4503BB4DF04BA1D8D121871FD()
	{
		case_identifiers = new ();
		vro_update = new ();
		cdc_case_matching_results = new ();
		cause_details_mirror = new ();
	}
	public _016FCDB951F95B19033708F378D04AED case_identifiers{ get;set;}
	public _35FD478CD58D3D1159A0C338470EAA77 vro_update{ get;set;}
	public _2A437E681C1808FE4ACB2536CF0FAAE5 cdc_case_matching_results{ get;set;}
	public _18CCB400C97876203A224BBC13B8453C cause_details_mirror{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		case_identifiers = mmria_case.GetGroupField<_016FCDB951F95B19033708F378D04AED>(p_value, "case_identifiers", "vro_case_determination/case_identifiers");
		vro_update = mmria_case.GetGroupField<_35FD478CD58D3D1159A0C338470EAA77>(p_value, "vro_update", "vro_case_determination/vro_update");
		cdc_case_matching_results = mmria_case.GetGroupField<_2A437E681C1808FE4ACB2536CF0FAAE5>(p_value, "cdc_case_matching_results", "vro_case_determination/cdc_case_matching_results");
		cause_details_mirror = mmria_case.GetGroupField<_18CCB400C97876203A224BBC13B8453C>(p_value, "cause_details_mirror", "vro_case_determination/cause_details_mirror");
	}
}

public sealed class _541063F265FE8A4801FDE0056F0578F5 : IConvertDictionary
{
	public _541063F265FE8A4801FDE0056F0578F5()
	{
	}
	public string agreement_status { get; set; }
	public string agreement_remarks { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		agreement_status = mmria_case.GetStringListField(p_value, "agreement_status", "committee_review/agreement_grp/agreement_status");
		agreement_remarks = mmria_case.GetTextAreaField(p_value, "agreement_remarks", "committee_review/agreement_grp/agreement_remarks");
	}
}

public sealed class _AAF3874F75AF4E0C8B4F1A8C80211F77 : IConvertDictionary
{
	public _AAF3874F75AF4E0C8B4F1A8C80211F77()
	{
	}
	public double? dc_info_complete { get; set; }
	public string dc_info_remarks { get; set; }
	public double? mmria_used { get; set; }
	public string mmria_used_remarks { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dc_info_complete = mmria_case.GetNumberListField(p_value, "dc_info_complete", "committee_review/rev_assessment_grp/dc_info_complete");
		dc_info_remarks = mmria_case.GetTextAreaField(p_value, "dc_info_remarks", "committee_review/rev_assessment_grp/dc_info_remarks");
		mmria_used = mmria_case.GetNumberListField(p_value, "mmria_used", "committee_review/rev_assessment_grp/mmria_used");
		mmria_used_remarks = mmria_case.GetTextAreaField(p_value, "mmria_used_remarks", "committee_review/rev_assessment_grp/mmria_used_remarks");
	}
}

public sealed class _5D25FC75453F8EE5C815093BA91CB4E8 : IConvertDictionary
{
	public _5D25FC75453F8EE5C815093BA91CB4E8()
	{
		review_2_by = new ();
	}
	public List<double> review_2_by { get; set; }
	public DateOnly? review_2_on { get; set; }
	public string review_2_remarks { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		review_2_by = mmria_case.GetMultiSelectNumberListField(p_value, "review_2_by", "committee_review/reviewer_grp/review_2_by");
		review_2_on = mmria_case.GetDateField(p_value, "review_2_on", "committee_review/reviewer_grp/review_2_on");
		review_2_remarks = mmria_case.GetTextAreaField(p_value, "review_2_remarks", "committee_review/reviewer_grp/review_2_remarks");
	}
}

public sealed class _62AEF5C4D8129ED98ECA69F7779FCBFC : IConvertDictionary
{
	public _62AEF5C4D8129ED98ECA69F7779FCBFC()
	{
		reviewer_grp = new ();
		rev_assessment_grp = new ();
		agreement_grp = new ();
	}
	public _5D25FC75453F8EE5C815093BA91CB4E8 reviewer_grp{ get;set;}
	public _AAF3874F75AF4E0C8B4F1A8C80211F77 rev_assessment_grp{ get;set;}
	public _541063F265FE8A4801FDE0056F0578F5 agreement_grp{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		reviewer_grp = mmria_case.GetGroupField<_5D25FC75453F8EE5C815093BA91CB4E8>(p_value, "reviewer_grp", "committee_review/reviewer_grp");
		rev_assessment_grp = mmria_case.GetGroupField<_AAF3874F75AF4E0C8B4F1A8C80211F77>(p_value, "rev_assessment_grp", "committee_review/rev_assessment_grp");
		agreement_grp = mmria_case.GetGroupField<_541063F265FE8A4801FDE0056F0578F5>(p_value, "agreement_grp", "committee_review/agreement_grp");
	}
}

public sealed class _FC18FC205B32648E04182836031EAD4E : IConvertDictionary
{
	public _FC18FC205B32648E04182836031EAD4E()
	{
	}
	public string pdf_link { get; set; }
	public string pdf_steve_link { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		pdf_link = mmria_case.GetStringField(p_value, "pdf_link", "preparer_remarks/pdf_grp/pdf_link");
		pdf_steve_link = mmria_case.GetStringField(p_value, "pdf_steve_link", "preparer_remarks/pdf_grp/pdf_steve_link");
	}
}

public sealed class _9046BAEC9DC8EE8023CBD639FB324319 : IConvertDictionary
{
	public _9046BAEC9DC8EE8023CBD639FB324319()
	{
	}
	public string note_to_vro { get; set; }
	public string remarks { get; set; }
	public string update_remarks { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		note_to_vro = mmria_case.GetTextAreaField(p_value, "note_to_vro", "preparer_remarks/remarks_grp/note_to_vro");
		remarks = mmria_case.GetTextAreaField(p_value, "remarks", "preparer_remarks/remarks_grp/remarks");
		update_remarks = mmria_case.GetTextAreaField(p_value, "update_remarks", "preparer_remarks/remarks_grp/update_remarks");
	}
}

public sealed class _812D5DBE0EF4B69F9C6A32C631BB4A37 : IConvertDictionary
{
	public _812D5DBE0EF4B69F9C6A32C631BB4A37()
	{
		review_1_by = new ();
	}
	public List<double> review_1_by { get; set; }
	public DateOnly? review_1_on { get; set; }
	public string review_1_remarks { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		review_1_by = mmria_case.GetMultiSelectNumberListField(p_value, "review_1_by", "preparer_remarks/preparer_grp/review_1_by");
		review_1_on = mmria_case.GetDateField(p_value, "review_1_on", "preparer_remarks/preparer_grp/review_1_on");
		review_1_remarks = mmria_case.GetTextAreaField(p_value, "review_1_remarks", "preparer_remarks/preparer_grp/review_1_remarks");
	}
}

public sealed class _3401C823D7E669B796E77290FE6A6D5F : IConvertDictionary
{
	public _3401C823D7E669B796E77290FE6A6D5F()
	{
		preparer_grp = new ();
		remarks_grp = new ();
		pdf_grp = new ();
	}
	public _812D5DBE0EF4B69F9C6A32C631BB4A37 preparer_grp{ get;set;}
	public _9046BAEC9DC8EE8023CBD639FB324319 remarks_grp{ get;set;}
	public _FC18FC205B32648E04182836031EAD4E pdf_grp{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		preparer_grp = mmria_case.GetGroupField<_812D5DBE0EF4B69F9C6A32C631BB4A37>(p_value, "preparer_grp", "preparer_remarks/preparer_grp");
		remarks_grp = mmria_case.GetGroupField<_9046BAEC9DC8EE8023CBD639FB324319>(p_value, "remarks_grp", "preparer_remarks/remarks_grp");
		pdf_grp = mmria_case.GetGroupField<_FC18FC205B32648E04182836031EAD4E>(p_value, "pdf_grp", "preparer_remarks/pdf_grp");
	}
}

public sealed class _DBF0E29DA9412C5642B671AC17C4319F : IConvertDictionary
{
	public _DBF0E29DA9412C5642B671AC17C4319F()
	{
	}
	public double? drug_1 { get; set; }
	public double? drug_2 { get; set; }
	public double? drug_3 { get; set; }
	public double? drug_iv { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		drug_1 = mmria_case.GetNumberListField(p_value, "drug_1", "cause_of_death/q33/drug_1");
		drug_2 = mmria_case.GetNumberListField(p_value, "drug_2", "cause_of_death/q33/drug_2");
		drug_3 = mmria_case.GetNumberListField(p_value, "drug_3", "cause_of_death/q33/drug_3");
		drug_iv = mmria_case.GetNumberListField(p_value, "drug_iv", "cause_of_death/q33/drug_iv");
	}
}

public sealed class _87BC56534EFA145265161385D92A37BA : IConvertDictionary
{
	public _87BC56534EFA145265161385D92A37BA()
	{
	}
	public double? assoc3 { get; set; }
	public double? acon3 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		assoc3 = mmria_case.GetNumberListField(p_value, "assoc3", "cause_of_death/q31/assoc3");
		acon3 = mmria_case.GetNumberListField(p_value, "acon3", "cause_of_death/q31/acon3");
	}
}

public sealed class _F838126A49C426AB2E19412BD02941CF : IConvertDictionary
{
	public _F838126A49C426AB2E19412BD02941CF()
	{
	}
	public double? assoc2 { get; set; }
	public double? acon2 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		assoc2 = mmria_case.GetNumberListField(p_value, "assoc2", "cause_of_death/q30/assoc2");
		acon2 = mmria_case.GetNumberListField(p_value, "acon2", "cause_of_death/q30/acon2");
	}
}

public sealed class _840626E88AD4F5181D122006422746D1 : IConvertDictionary
{
	public _840626E88AD4F5181D122006422746D1()
	{
	}
	public double? assoc1 { get; set; }
	public double? acon1 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		assoc1 = mmria_case.GetNumberListField(p_value, "assoc1", "cause_of_death/q29/assoc1");
		acon1 = mmria_case.GetNumberListField(p_value, "acon1", "cause_of_death/q29/acon1");
	}
}

public sealed class _0144F5B1942DDC29EE5AE5B9DE40E934 : IConvertDictionary
{
	public _0144F5B1942DDC29EE5AE5B9DE40E934()
	{
	}
	public double? cdccod { get; set; }
	public double? cod { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		cdccod = mmria_case.GetNumberListField(p_value, "cdccod", "cause_of_death/q28/cdccod");
		cod = mmria_case.GetNumberListField(p_value, "cod", "cause_of_death/q28/cod");
	}
}

public sealed class _8C2D3559CAC525D0B20C896AB71DA6DE : IConvertDictionary
{
	public _8C2D3559CAC525D0B20C896AB71DA6DE()
	{
		q28 = new ();
		q29 = new ();
		q30 = new ();
		q31 = new ();
		q33 = new ();
	}
	public _0144F5B1942DDC29EE5AE5B9DE40E934 q28{ get;set;}
	public _840626E88AD4F5181D122006422746D1 q29{ get;set;}
	public _F838126A49C426AB2E19412BD02941CF q30{ get;set;}
	public _87BC56534EFA145265161385D92A37BA q31{ get;set;}
	public double? injury { get; set; }
	public _DBF0E29DA9412C5642B671AC17C4319F q33{ get;set;}
	public double? @class { get; set; }
	public string coder { get; set; }
	public double? clsmo { get; set; }
	public double? clsyr { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		q28 = mmria_case.GetGroupField<_0144F5B1942DDC29EE5AE5B9DE40E934>(p_value, "q28", "cause_of_death/q28");
		q29 = mmria_case.GetGroupField<_840626E88AD4F5181D122006422746D1>(p_value, "q29", "cause_of_death/q29");
		q30 = mmria_case.GetGroupField<_F838126A49C426AB2E19412BD02941CF>(p_value, "q30", "cause_of_death/q30");
		q31 = mmria_case.GetGroupField<_87BC56534EFA145265161385D92A37BA>(p_value, "q31", "cause_of_death/q31");
		injury = mmria_case.GetNumberListField(p_value, "injury", "cause_of_death/injury");
		q33 = mmria_case.GetGroupField<_DBF0E29DA9412C5642B671AC17C4319F>(p_value, "q33", "cause_of_death/q33");
		@class = mmria_case.GetNumberListField(p_value, "class", "cause_of_death/class");
		coder = mmria_case.GetStringField(p_value, "coder", "cause_of_death/coder");
		clsmo = mmria_case.GetNumberListField(p_value, "clsmo", "cause_of_death/clsmo");
		clsyr = mmria_case.GetNumberListField(p_value, "clsyr", "cause_of_death/clsyr");
	}
}

public sealed class _CCB1613E511CB7D633D8C1204A54BE73 : IConvertDictionary
{
	public _CCB1613E511CB7D633D8C1204A54BE73()
	{
	}
	public double? dterm_mo { get; set; }
	public double? dterm_dy { get; set; }
	public double? dterm_yr { get; set; }
	public TimeOnly? dterm_tm { get; set; }
	public DateOnly? dterm { get; set; }
	public double? daydif { get; set; }
	public DateOnly? dterm_est { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		dterm_mo = mmria_case.GetNumberListField(p_value, "dterm_mo", "outcome/dterm_grp/dterm_mo");
		dterm_dy = mmria_case.GetNumberListField(p_value, "dterm_dy", "outcome/dterm_grp/dterm_dy");
		dterm_yr = mmria_case.GetNumberListField(p_value, "dterm_yr", "outcome/dterm_grp/dterm_yr");
		dterm_tm = mmria_case.GetTimeField(p_value, "dterm_tm", "outcome/dterm_grp/dterm_tm");
		dterm = mmria_case.GetDateField(p_value, "dterm", "outcome/dterm_grp/dterm");
		daydif = mmria_case.GetNumberField(p_value, "daydif", "outcome/dterm_grp/daydif");
		dterm_est = mmria_case.GetDateField(p_value, "dterm_est", "outcome/dterm_grp/dterm_est");
	}
}

public sealed class _DA467D7ACB946E3D54CFCE16AC05413B : IConvertDictionary
{
	public _DA467D7ACB946E3D54CFCE16AC05413B()
	{
	}
	public double? termproc { get; set; }
	public double? termpro1 { get; set; }
	public double? termpro2 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		termproc = mmria_case.GetNumberListField(p_value, "termproc", "outcome/q25/termproc");
		termpro1 = mmria_case.GetNumberListField(p_value, "termpro1", "outcome/q25/termpro1");
		termpro2 = mmria_case.GetNumberListField(p_value, "termpro2", "outcome/q25/termpro2");
	}
}

public sealed class _3032AD6AED6C5C3CDA992D241F4D28BF : IConvertDictionary
{
	public _3032AD6AED6C5C3CDA992D241F4D28BF()
	{
		q25 = new ();
		dterm_grp = new ();
	}
	public double? outindx { get; set; }
	public double? multgest { get; set; }
	public _DA467D7ACB946E3D54CFCE16AC05413B q25{ get;set;}
	public double? gestwk { get; set; }
	public _CCB1613E511CB7D633D8C1204A54BE73 dterm_grp{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		outindx = mmria_case.GetNumberListField(p_value, "outindx", "outcome/outindx");
		multgest = mmria_case.GetNumberListField(p_value, "multgest", "outcome/multgest");
		q25 = mmria_case.GetGroupField<_DA467D7ACB946E3D54CFCE16AC05413B>(p_value, "q25", "outcome/q25");
		gestwk = mmria_case.GetNumberField(p_value, "gestwk", "outcome/gestwk");
		dterm_grp = mmria_case.GetGroupField<_CCB1613E511CB7D633D8C1204A54BE73>(p_value, "dterm_grp", "outcome/dterm_grp");
	}
}

public sealed class _5D2A16AFD7694B8E349B66F0C191B9F6 : IConvertDictionary
{
	public _5D2A16AFD7694B8E349B66F0C191B9F6()
	{
	}
	public double? educatn { get; set; }
	public string occup { get; set; }
	public string indust { get; set; }
	public string industry_code_1 { get; set; }
	public string industry_code_2 { get; set; }
	public string industry_code_3 { get; set; }
	public string occupation_code_1 { get; set; }
	public string occupation_code_2 { get; set; }
	public string occupation_code_3 { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		educatn = mmria_case.GetNumberListField(p_value, "educatn", "demographic/q14/educatn");
		occup = mmria_case.GetStringField(p_value, "occup", "demographic/q14/occup");
		indust = mmria_case.GetStringField(p_value, "indust", "demographic/q14/indust");
		industry_code_1 = mmria_case.GetStringField(p_value, "industry_code_1", "demographic/q14/industry_code_1");
		industry_code_2 = mmria_case.GetStringField(p_value, "industry_code_2", "demographic/q14/industry_code_2");
		industry_code_3 = mmria_case.GetStringField(p_value, "industry_code_3", "demographic/q14/industry_code_3");
		occupation_code_1 = mmria_case.GetStringField(p_value, "occupation_code_1", "demographic/q14/occupation_code_1");
		occupation_code_2 = mmria_case.GetStringField(p_value, "occupation_code_2", "demographic/q14/occupation_code_2");
		occupation_code_3 = mmria_case.GetStringField(p_value, "occupation_code_3", "demographic/q14/occupation_code_3");
	}
}

public sealed class _F6D53CB0472CB02EDB3EFC1344A1A79C : IConvertDictionary
{
	public _F6D53CB0472CB02EDB3EFC1344A1A79C()
	{
	}
	public double? hisporg { get; set; }
	public string ethnic1_mex { get; set; }
	public string ethnic2_pr { get; set; }
	public string ethnic3_cub { get; set; }
	public string ethnic4_other { get; set; }
	public string hisp_oth { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		hisporg = mmria_case.GetNumberListField(p_value, "hisporg", "demographic/q12/ethnicity/hisporg");
		ethnic1_mex = mmria_case.GetStringListField(p_value, "ethnic1_mex", "demographic/q12/ethnicity/ethnic1_mex");
		ethnic2_pr = mmria_case.GetStringListField(p_value, "ethnic2_pr", "demographic/q12/ethnicity/ethnic2_pr");
		ethnic3_cub = mmria_case.GetStringListField(p_value, "ethnic3_cub", "demographic/q12/ethnicity/ethnic3_cub");
		ethnic4_other = mmria_case.GetStringListField(p_value, "ethnic4_other", "demographic/q12/ethnicity/ethnic4_other");
		hisp_oth = mmria_case.GetStringField(p_value, "hisp_oth", "demographic/q12/ethnicity/hisp_oth");
	}
}

public sealed class _A50E2D82A8E3BC8A4288196E429C2446 : IConvertDictionary
{
	public _A50E2D82A8E3BC8A4288196E429C2446()
	{
	}
	public string race_source { get; set; }
	public string race_white { get; set; }
	public string race_black { get; set; }
	public string race_amindalknat { get; set; }
	public string tribe { get; set; }
	public string race_asianindian { get; set; }
	public string race_chinese { get; set; }
	public string race_filipino { get; set; }
	public string race_japanese { get; set; }
	public string race_korean { get; set; }
	public string race_vietnamese { get; set; }
	public string race_otherasian { get; set; }
	public string race_otherasian_literal { get; set; }
	public string race_nativehawaiian { get; set; }
	public string race_guamcham { get; set; }
	public string race_samoan { get; set; }
	public string race_otherpacific { get; set; }
	public string race_otherpacific_literal { get; set; }
	public string race_other { get; set; }
	public string race_oth { get; set; }
	public string race_notspecified { get; set; }
	public double? race_omb { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		race_source = mmria_case.GetStringListField(p_value, "race_source", "demographic/q12/group/race_source");
		race_white = mmria_case.GetStringListField(p_value, "race_white", "demographic/q12/group/race_white");
		race_black = mmria_case.GetStringListField(p_value, "race_black", "demographic/q12/group/race_black");
		race_amindalknat = mmria_case.GetStringListField(p_value, "race_amindalknat", "demographic/q12/group/race_amindalknat");
		tribe = mmria_case.GetStringField(p_value, "tribe", "demographic/q12/group/tribe");
		race_asianindian = mmria_case.GetStringListField(p_value, "race_asianindian", "demographic/q12/group/race_asianindian");
		race_chinese = mmria_case.GetStringListField(p_value, "race_chinese", "demographic/q12/group/race_chinese");
		race_filipino = mmria_case.GetStringListField(p_value, "race_filipino", "demographic/q12/group/race_filipino");
		race_japanese = mmria_case.GetStringListField(p_value, "race_japanese", "demographic/q12/group/race_japanese");
		race_korean = mmria_case.GetStringListField(p_value, "race_korean", "demographic/q12/group/race_korean");
		race_vietnamese = mmria_case.GetStringListField(p_value, "race_vietnamese", "demographic/q12/group/race_vietnamese");
		race_otherasian = mmria_case.GetStringListField(p_value, "race_otherasian", "demographic/q12/group/race_otherasian");
		race_otherasian_literal = mmria_case.GetStringField(p_value, "race_otherasian_literal", "demographic/q12/group/race_otherasian_literal");
		race_nativehawaiian = mmria_case.GetStringListField(p_value, "race_nativehawaiian", "demographic/q12/group/race_nativehawaiian");
		race_guamcham = mmria_case.GetStringListField(p_value, "race_guamcham", "demographic/q12/group/race_guamcham");
		race_samoan = mmria_case.GetStringListField(p_value, "race_samoan", "demographic/q12/group/race_samoan");
		race_otherpacific = mmria_case.GetStringListField(p_value, "race_otherpacific", "demographic/q12/group/race_otherpacific");
		race_otherpacific_literal = mmria_case.GetStringField(p_value, "race_otherpacific_literal", "demographic/q12/group/race_otherpacific_literal");
		race_other = mmria_case.GetStringListField(p_value, "race_other", "demographic/q12/group/race_other");
		race_oth = mmria_case.GetStringField(p_value, "race_oth", "demographic/q12/group/race_oth");
		race_notspecified = mmria_case.GetStringListField(p_value, "race_notspecified", "demographic/q12/group/race_notspecified");
		race_omb = mmria_case.GetNumberListField(p_value, "race_omb", "demographic/q12/group/race_omb");
	}
}

public sealed class _6135235F2FC2D9AD60100129ABB94EBC : IConvertDictionary
{
	public _6135235F2FC2D9AD60100129ABB94EBC()
	{
		group = new ();
		ethnicity = new ();
	}
	public double? race { get; set; }
	public _A50E2D82A8E3BC8A4288196E429C2446 group{ get;set;}
	public _F6D53CB0472CB02EDB3EFC1344A1A79C ethnicity{ get;set;}
	public double? matbplc { get; set; }
	public string matbplc_us { get; set; }
	public string matbplc_else { get; set; }
	public string matbplc_else_literal { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		race = mmria_case.GetNumberListField(p_value, "race", "demographic/q12/race");
		group = mmria_case.GetGroupField<_A50E2D82A8E3BC8A4288196E429C2446>(p_value, "group", "demographic/q12/group");
		ethnicity = mmria_case.GetGroupField<_F6D53CB0472CB02EDB3EFC1344A1A79C>(p_value, "ethnicity", "demographic/q12/ethnicity");
		matbplc = mmria_case.GetNumberListField(p_value, "matbplc", "demographic/q12/matbplc");
		matbplc_us = mmria_case.GetStringListField(p_value, "matbplc_us", "demographic/q12/matbplc_us");
		matbplc_else = mmria_case.GetStringListField(p_value, "matbplc_else", "demographic/q12/matbplc_else");
		matbplc_else_literal = mmria_case.GetStringField(p_value, "matbplc_else_literal", "demographic/q12/matbplc_else_literal");
	}
}

public sealed class _F7C389389B7004924221B820D81117CE : IConvertDictionary
{
	public _F7C389389B7004924221B820D81117CE()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public DateOnly? dob { get; set; }
	public double? agedif { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "demographic/date_of_birth/month");
		day = mmria_case.GetNumberListField(p_value, "day", "demographic/date_of_birth/day");
		year = mmria_case.GetNumberListField(p_value, "year", "demographic/date_of_birth/year");
		dob = mmria_case.GetDateField(p_value, "dob", "demographic/date_of_birth/dob");
		agedif = mmria_case.GetNumberField(p_value, "agedif", "demographic/date_of_birth/agedif");
	}
}

public sealed class _125E8E9BBE92C992689F82460866FEE3 : IConvertDictionary
{
	public _125E8E9BBE92C992689F82460866FEE3()
	{
		date_of_birth = new ();
		q12 = new ();
		q14 = new ();
	}
	public double? mage { get; set; }
	public _F7C389389B7004924221B820D81117CE date_of_birth{ get;set;}
	public _6135235F2FC2D9AD60100129ABB94EBC q12{ get;set;}
	public double? marstat { get; set; }
	public _5D2A16AFD7694B8E349B66F0C191B9F6 q14{ get;set;}
	public double? placedth { get; set; }
	public double? pnc { get; set; }
	public double? autopsy3 { get; set; }
	public double? height { get; set; }
	public double? wtpreprg { get; set; }
	public double? bmi { get; set; }
	public double? prevlb { get; set; }
	public double? prvothpg { get; set; }
	public double? pymtsrc { get; set; }
	public double? wic { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		mage = mmria_case.GetNumberField(p_value, "mage", "demographic/mage");
		date_of_birth = mmria_case.GetGroupField<_F7C389389B7004924221B820D81117CE>(p_value, "date_of_birth", "demographic/date_of_birth");
		q12 = mmria_case.GetGroupField<_6135235F2FC2D9AD60100129ABB94EBC>(p_value, "q12", "demographic/q12");
		marstat = mmria_case.GetNumberListField(p_value, "marstat", "demographic/marstat");
		q14 = mmria_case.GetGroupField<_5D2A16AFD7694B8E349B66F0C191B9F6>(p_value, "q14", "demographic/q14");
		placedth = mmria_case.GetNumberListField(p_value, "placedth", "demographic/placedth");
		pnc = mmria_case.GetNumberListField(p_value, "pnc", "demographic/pnc");
		autopsy3 = mmria_case.GetNumberListField(p_value, "autopsy3", "demographic/autopsy3");
		height = mmria_case.GetNumberField(p_value, "height", "demographic/height");
		wtpreprg = mmria_case.GetNumberField(p_value, "wtpreprg", "demographic/wtpreprg");
		bmi = mmria_case.GetNumberField(p_value, "bmi", "demographic/bmi");
		prevlb = mmria_case.GetNumberField(p_value, "prevlb", "demographic/prevlb");
		prvothpg = mmria_case.GetNumberField(p_value, "prvothpg", "demographic/prvothpg");
		pymtsrc = mmria_case.GetNumberListField(p_value, "pymtsrc", "demographic/pymtsrc");
		wic = mmria_case.GetNumberListField(p_value, "wic", "demographic/wic");
	}
}

public sealed class _7333B2136067574C87E59C086A46C60E : IConvertDictionary
{
	public _7333B2136067574C87E59C086A46C60E()
	{
	}
	public double? preg_cb_match { get; set; }
	public double? literal_cod_match { get; set; }
	public double? icd10_match { get; set; }
	public double? bc_match { get; set; }
	public double? fdc_match { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		preg_cb_match = mmria_case.GetNumberListField(p_value, "preg_cb_match", "tracking/cdc_case_matching_results/preg_cb_match");
		literal_cod_match = mmria_case.GetNumberListField(p_value, "literal_cod_match", "tracking/cdc_case_matching_results/literal_cod_match");
		icd10_match = mmria_case.GetNumberListField(p_value, "icd10_match", "tracking/cdc_case_matching_results/icd10_match");
		bc_match = mmria_case.GetNumberListField(p_value, "bc_match", "tracking/cdc_case_matching_results/bc_match");
		fdc_match = mmria_case.GetNumberListField(p_value, "fdc_match", "tracking/cdc_case_matching_results/fdc_match");
	}
}

public sealed class _8B0E34BF041F56AC0E459B9B50A63B22 : IConvertDictionary
{
	public _8B0E34BF041F56AC0E459B9B50A63B22()
	{
	}
	public string statres { get; set; }
	public string reszip { get; set; }
	public double? zipsrce { get; set; }
	public string county { get; set; }
	public double? cntysrce { get; set; }
	public string residence_feature_matching_geography_type { get; set; }
	public string residence_latitude { get; set; }
	public string residence_longitude { get; set; }
	public string residence_naaccr_gis_coordinate_quality_code { get; set; }
	public string residence_naaccr_gis_coordinate_quality_type { get; set; }
	public string residence_naaccr_census_tract_certainty_code { get; set; }
	public string residence_naaccr_census_tract_certainty_type { get; set; }
	public string residence_state_county_fips { get; set; }
	public string residence_census_state_fips { get; set; }
	public string residence_census_county_fips { get; set; }
	public string residence_census_tract_fips { get; set; }
	public string residence_urban_status { get; set; }
	public string residence_census_met_div_fips { get; set; }
	public string residence_census_cbsa_fips { get; set; }
	public string residence_census_cbsa_micro { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		statres = mmria_case.GetStringListField(p_value, "statres", "tracking/q9/statres");
		reszip = mmria_case.GetStringField(p_value, "reszip", "tracking/q9/reszip");
		zipsrce = mmria_case.GetNumberListField(p_value, "zipsrce", "tracking/q9/zipsrce");
		county = mmria_case.GetStringField(p_value, "county", "tracking/q9/county");
		cntysrce = mmria_case.GetNumberListField(p_value, "cntysrce", "tracking/q9/cntysrce");
		residence_feature_matching_geography_type = mmria_case.GetStringField(p_value, "residence_feature_matching_geography_type", "tracking/q9/residence_feature_matching_geography_type");
		residence_latitude = mmria_case.GetStringField(p_value, "residence_latitude", "tracking/q9/residence_latitude");
		residence_longitude = mmria_case.GetStringField(p_value, "residence_longitude", "tracking/q9/residence_longitude");
		residence_naaccr_gis_coordinate_quality_code = mmria_case.GetStringField(p_value, "residence_naaccr_gis_coordinate_quality_code", "tracking/q9/residence_naaccr_gis_coordinate_quality_code");
		residence_naaccr_gis_coordinate_quality_type = mmria_case.GetStringField(p_value, "residence_naaccr_gis_coordinate_quality_type", "tracking/q9/residence_naaccr_gis_coordinate_quality_type");
		residence_naaccr_census_tract_certainty_code = mmria_case.GetStringField(p_value, "residence_naaccr_census_tract_certainty_code", "tracking/q9/residence_naaccr_census_tract_certainty_code");
		residence_naaccr_census_tract_certainty_type = mmria_case.GetStringField(p_value, "residence_naaccr_census_tract_certainty_type", "tracking/q9/residence_naaccr_census_tract_certainty_type");
		residence_state_county_fips = mmria_case.GetStringField(p_value, "residence_state_county_fips", "tracking/q9/residence_state_county_fips");
		residence_census_state_fips = mmria_case.GetStringField(p_value, "residence_census_state_fips", "tracking/q9/residence_census_state_fips");
		residence_census_county_fips = mmria_case.GetStringField(p_value, "residence_census_county_fips", "tracking/q9/residence_census_county_fips");
		residence_census_tract_fips = mmria_case.GetStringField(p_value, "residence_census_tract_fips", "tracking/q9/residence_census_tract_fips");
		residence_urban_status = mmria_case.GetStringField(p_value, "residence_urban_status", "tracking/q9/residence_urban_status");
		residence_census_met_div_fips = mmria_case.GetStringField(p_value, "residence_census_met_div_fips", "tracking/q9/residence_census_met_div_fips");
		residence_census_cbsa_fips = mmria_case.GetStringField(p_value, "residence_census_cbsa_fips", "tracking/q9/residence_census_cbsa_fips");
		residence_census_cbsa_micro = mmria_case.GetStringField(p_value, "residence_census_cbsa_micro", "tracking/q9/residence_census_cbsa_micro");
	}
}

public sealed class _33C3CE3D9BD28A364510360F29828553 : IConvertDictionary
{
	public _33C3CE3D9BD28A364510360F29828553()
	{
	}
	public double? pregstat { get; set; }
	public double? pcbtime { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		pregstat = mmria_case.GetNumberListField(p_value, "pregstat", "tracking/q7/pregstat");
		pcbtime = mmria_case.GetNumberListField(p_value, "pcbtime", "tracking/q7/pcbtime");
	}
}

public sealed class _9E164C92EAA5177002A010E1A934B1D0 : IConvertDictionary
{
	public _9E164C92EAA5177002A010E1A934B1D0()
	{
	}
	public double? month { get; set; }
	public double? day { get; set; }
	public double? year { get; set; }
	public TimeOnly? time_of_death { get; set; }
	public DateOnly? dod { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		month = mmria_case.GetNumberListField(p_value, "month", "tracking/date_of_death/month");
		day = mmria_case.GetNumberListField(p_value, "day", "tracking/date_of_death/day");
		year = mmria_case.GetNumberListField(p_value, "year", "tracking/date_of_death/year");
		time_of_death = mmria_case.GetTimeField(p_value, "time_of_death", "tracking/date_of_death/time_of_death");
		dod = mmria_case.GetDateField(p_value, "dod", "tracking/date_of_death/dod");
	}
}

public sealed class _419E7777D6D3FC43E91EB066203CA08D : IConvertDictionary
{
	public _419E7777D6D3FC43E91EB066203CA08D()
	{
	}
	public string amssno { get; set; }
	public double? amssrel { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		amssno = mmria_case.GetStringField(p_value, "amssno", "tracking/q1/amssno");
		amssrel = mmria_case.GetNumberListField(p_value, "amssrel", "tracking/q1/amssrel");
	}
}

public sealed class _1DCADE918C1D24A0E4B29F438833775E : IConvertDictionary
{
	public _1DCADE918C1D24A0E4B29F438833775E()
	{
	}
	public string pmssno { get; set; }
	public string jurisdiction { get; set; }
	public double? track_year { get; set; }
	public double? med_coder_check { get; set; }
	public double? med_dir_check { get; set; }
	public string status { get; set; }
	public string vro_resolution_status_mirror { get; set; }
	public double? steve_transfer { get; set; }
	public string case_folder { get; set; }
	public string batch_name { get; set; }
	public string fileno_dc { get; set; }
	public string fileno_bc { get; set; }
	public string fileno_fdc { get; set; }
	public string year_birthorfetaldeath { get; set; }
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		pmssno = mmria_case.GetStringField(p_value, "pmssno", "tracking/admin_info/pmssno");
		jurisdiction = mmria_case.GetStringListField(p_value, "jurisdiction", "tracking/admin_info/jurisdiction");
		track_year = mmria_case.GetNumberListField(p_value, "track_year", "tracking/admin_info/track_year");
		med_coder_check = mmria_case.GetNumberListField(p_value, "med_coder_check", "tracking/admin_info/med_coder_check");
		med_dir_check = mmria_case.GetNumberListField(p_value, "med_dir_check", "tracking/admin_info/med_dir_check");
		status = mmria_case.GetStringListField(p_value, "status", "tracking/admin_info/status");
		vro_resolution_status_mirror = mmria_case.GetStringField(p_value, "vro_resolution_status_mirror", "tracking/admin_info/vro_resolution_status_mirror");
		steve_transfer = mmria_case.GetNumberListField(p_value, "steve_transfer", "tracking/admin_info/steve_transfer");
		case_folder = mmria_case.GetJurisdictionField(p_value, "case_folder", "tracking/admin_info/case_folder");
		batch_name = mmria_case.GetStringField(p_value, "batch_name", "tracking/admin_info/batch_name");
		fileno_dc = mmria_case.GetStringField(p_value, "fileno_dc", "tracking/admin_info/fileno_dc");
		fileno_bc = mmria_case.GetStringField(p_value, "fileno_bc", "tracking/admin_info/fileno_bc");
		fileno_fdc = mmria_case.GetStringField(p_value, "fileno_fdc", "tracking/admin_info/fileno_fdc");
		year_birthorfetaldeath = mmria_case.GetStringField(p_value, "year_birthorfetaldeath", "tracking/admin_info/year_birthorfetaldeath");
	}
}

public sealed class _1879966223C3E382E14C6524C84942F1 : IConvertDictionary
{
	public _1879966223C3E382E14C6524C84942F1()
	{
		admin_info = new ();
		q1 = new ();
		date_of_death = new ();
		q7 = new ();
		q9 = new ();
		cdc_case_matching_results = new ();
	}
	public _1DCADE918C1D24A0E4B29F438833775E admin_info{ get;set;}
	public _419E7777D6D3FC43E91EB066203CA08D q1{ get;set;}
	public string death_certificate_number { get; set; }
	public _9E164C92EAA5177002A010E1A934B1D0 date_of_death{ get;set;}
	public double? sourcnot { get; set; }
	public double? dcfile { get; set; }
	public double? lbfile { get; set; }
	public _33C3CE3D9BD28A364510360F29828553 q7{ get;set;}
	public string statdth { get; set; }
	public _8B0E34BF041F56AC0E459B9B50A63B22 q9{ get;set;}
	public _7333B2136067574C87E59C086A46C60E cdc_case_matching_results{ get;set;}
	public void Convert(System.Text.Json.JsonElement p_value)
	{
		admin_info = mmria_case.GetGroupField<_1DCADE918C1D24A0E4B29F438833775E>(p_value, "admin_info", "tracking/admin_info");
		q1 = mmria_case.GetGroupField<_419E7777D6D3FC43E91EB066203CA08D>(p_value, "q1", "tracking/q1");
		death_certificate_number = mmria_case.GetStringField(p_value, "death_certificate_number", "tracking/death_certificate_number");
		date_of_death = mmria_case.GetGroupField<_9E164C92EAA5177002A010E1A934B1D0>(p_value, "date_of_death", "tracking/date_of_death");
		sourcnot = mmria_case.GetNumberListField(p_value, "sourcnot", "tracking/sourcnot");
		dcfile = mmria_case.GetNumberListField(p_value, "dcfile", "tracking/dcfile");
		lbfile = mmria_case.GetNumberListField(p_value, "lbfile", "tracking/lbfile");
		q7 = mmria_case.GetGroupField<_33C3CE3D9BD28A364510360F29828553>(p_value, "q7", "tracking/q7");
		statdth = mmria_case.GetStringListField(p_value, "statdth", "tracking/statdth");
		q9 = mmria_case.GetGroupField<_8B0E34BF041F56AC0E459B9B50A63B22>(p_value, "q9", "tracking/q9");
		cdc_case_matching_results = mmria_case.GetGroupField<_7333B2136067574C87E59C086A46C60E>(p_value, "cdc_case_matching_results", "tracking/cdc_case_matching_results");
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
		tracking = new ();
		demographic = new ();
		outcome = new ();
		cause_of_death = new ();
		preparer_remarks = new ();
		committee_review = new ();
		vro_case_determination = new ();
		ije_dc = new ();
		ije_bc = new ();
		ije_fetaldc = new ();
		amss_tracking = new ();
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
	public _1879966223C3E382E14C6524C84942F1 tracking{ get;set;}
	public _125E8E9BBE92C992689F82460866FEE3 demographic{ get;set;}
	public _3032AD6AED6C5C3CDA992D241F4D28BF outcome{ get;set;}
	public _8C2D3559CAC525D0B20C896AB71DA6DE cause_of_death{ get;set;}
	public _3401C823D7E669B796E77290FE6A6D5F preparer_remarks{ get;set;}
	public _62AEF5C4D8129ED98ECA69F7779FCBFC committee_review{ get;set;}
	public _4A4043A4503BB4DF04BA1D8D121871FD vro_case_determination{ get;set;}
	public _0D69863CC31E49D042EEE11D6403ED7B ije_dc{ get;set;}
	public _380867B2F39E2C3A1183FE46EB907A2F ije_bc{ get;set;}
	public _1CDCB33AD951AAC284F43FCC500F25C8 ije_fetaldc{ get;set;}
	public _8355BA227D01C14C5E1E0C172277D46E amss_tracking{ get;set;}
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
		tracking = mmria_case.GetFormField<_1879966223C3E382E14C6524C84942F1>(p_value, "tracking", "tracking");
		demographic = mmria_case.GetFormField<_125E8E9BBE92C992689F82460866FEE3>(p_value, "demographic", "demographic");
		outcome = mmria_case.GetFormField<_3032AD6AED6C5C3CDA992D241F4D28BF>(p_value, "outcome", "outcome");
		cause_of_death = mmria_case.GetFormField<_8C2D3559CAC525D0B20C896AB71DA6DE>(p_value, "cause_of_death", "cause_of_death");
		preparer_remarks = mmria_case.GetFormField<_3401C823D7E669B796E77290FE6A6D5F>(p_value, "preparer_remarks", "preparer_remarks");
		committee_review = mmria_case.GetFormField<_62AEF5C4D8129ED98ECA69F7779FCBFC>(p_value, "committee_review", "committee_review");
		vro_case_determination = mmria_case.GetFormField<_4A4043A4503BB4DF04BA1D8D121871FD>(p_value, "vro_case_determination", "vro_case_determination");
		ije_dc = mmria_case.GetFormField<_0D69863CC31E49D042EEE11D6403ED7B>(p_value, "ije_dc", "ije_dc");
		ije_bc = mmria_case.GetFormField<_380867B2F39E2C3A1183FE46EB907A2F>(p_value, "ije_bc", "ije_bc");
		ije_fetaldc = mmria_case.GetFormField<_1CDCB33AD951AAC284F43FCC500F25C8>(p_value, "ije_fetaldc", "ije_fetaldc");
		amss_tracking = mmria_case.GetFormField<_8355BA227D01C14C5E1E0C172277D46E>(p_value, "amss_tracking", "amss_tracking");
	}
}

