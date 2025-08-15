
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.pmss.v230616;

public sealed partial class mmria_case
{


    public string? GetS_String(string path)
    {
        string? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "version" => version,
         "created_by" => created_by,
         "last_updated_by" => last_updated_by,
         "last_checked_out_by" => last_checked_out_by,
         "host_state" => host_state,
         "addquarter" => addquarter,
         "cmpquarter" => cmpquarter,
         "tracking/admin_info/pmssno" => tracking.admin_info.pmssno,
         "tracking/admin_info/jurisdiction" => tracking.admin_info.jurisdiction,
         "tracking/admin_info/status" => tracking.admin_info.status,
         "tracking/admin_info/vro_resolution_status_mirror" => tracking.admin_info.vro_resolution_status_mirror,
         "tracking/admin_info/case_folder" => tracking.admin_info.case_folder,
         "tracking/admin_info/batch_name" => tracking.admin_info.batch_name,
         "tracking/admin_info/fileno_dc" => tracking.admin_info.fileno_dc,
         "tracking/admin_info/fileno_bc" => tracking.admin_info.fileno_bc,
         "tracking/admin_info/fileno_fdc" => tracking.admin_info.fileno_fdc,
         "tracking/admin_info/year_birthorfetaldeath" => tracking.admin_info.year_birthorfetaldeath,
         "tracking/q1/amssno" => tracking.q1.amssno,
         "tracking/death_certificate_number" => tracking.death_certificate_number,
         "tracking/statdth" => tracking.statdth,
         "tracking/q9/statres" => tracking.q9.statres,
         "tracking/q9/reszip" => tracking.q9.reszip,
         "tracking/q9/county" => tracking.q9.county,
         "tracking/q9/residence_feature_matching_geography_type" => tracking.q9.residence_feature_matching_geography_type,
         "tracking/q9/residence_latitude" => tracking.q9.residence_latitude,
         "tracking/q9/residence_longitude" => tracking.q9.residence_longitude,
         "tracking/q9/residence_naaccr_gis_coordinate_quality_code" => tracking.q9.residence_naaccr_gis_coordinate_quality_code,
         "tracking/q9/residence_naaccr_gis_coordinate_quality_type" => tracking.q9.residence_naaccr_gis_coordinate_quality_type,
         "tracking/q9/residence_naaccr_census_tract_certainty_code" => tracking.q9.residence_naaccr_census_tract_certainty_code,
         "tracking/q9/residence_naaccr_census_tract_certainty_type" => tracking.q9.residence_naaccr_census_tract_certainty_type,
         "tracking/q9/residence_state_county_fips" => tracking.q9.residence_state_county_fips,
         "tracking/q9/residence_census_state_fips" => tracking.q9.residence_census_state_fips,
         "tracking/q9/residence_census_county_fips" => tracking.q9.residence_census_county_fips,
         "tracking/q9/residence_census_tract_fips" => tracking.q9.residence_census_tract_fips,
         "tracking/q9/residence_urban_status" => tracking.q9.residence_urban_status,
         "tracking/q9/residence_census_met_div_fips" => tracking.q9.residence_census_met_div_fips,
         "tracking/q9/residence_census_cbsa_fips" => tracking.q9.residence_census_cbsa_fips,
         "tracking/q9/residence_census_cbsa_micro" => tracking.q9.residence_census_cbsa_micro,
         "demographic/q12/group/race_source" => demographic.q12.group.race_source,
         "demographic/q12/group/race_white" => demographic.q12.group.race_white,
         "demographic/q12/group/race_black" => demographic.q12.group.race_black,
         "demographic/q12/group/race_amindalknat" => demographic.q12.group.race_amindalknat,
         "demographic/q12/group/tribe" => demographic.q12.group.tribe,
         "demographic/q12/group/race_asianindian" => demographic.q12.group.race_asianindian,
         "demographic/q12/group/race_chinese" => demographic.q12.group.race_chinese,
         "demographic/q12/group/race_filipino" => demographic.q12.group.race_filipino,
         "demographic/q12/group/race_japanese" => demographic.q12.group.race_japanese,
         "demographic/q12/group/race_korean" => demographic.q12.group.race_korean,
         "demographic/q12/group/race_vietnamese" => demographic.q12.group.race_vietnamese,
         "demographic/q12/group/race_otherasian" => demographic.q12.group.race_otherasian,
         "demographic/q12/group/race_otherasian_literal" => demographic.q12.group.race_otherasian_literal,
         "demographic/q12/group/race_nativehawaiian" => demographic.q12.group.race_nativehawaiian,
         "demographic/q12/group/race_guamcham" => demographic.q12.group.race_guamcham,
         "demographic/q12/group/race_samoan" => demographic.q12.group.race_samoan,
         "demographic/q12/group/race_otherpacific" => demographic.q12.group.race_otherpacific,
         "demographic/q12/group/race_otherpacific_literal" => demographic.q12.group.race_otherpacific_literal,
         "demographic/q12/group/race_other" => demographic.q12.group.race_other,
         "demographic/q12/group/race_oth" => demographic.q12.group.race_oth,
         "demographic/q12/group/race_notspecified" => demographic.q12.group.race_notspecified,
         "demographic/q12/ethnicity/ethnic1_mex" => demographic.q12.ethnicity.ethnic1_mex,
         "demographic/q12/ethnicity/ethnic2_pr" => demographic.q12.ethnicity.ethnic2_pr,
         "demographic/q12/ethnicity/ethnic3_cub" => demographic.q12.ethnicity.ethnic3_cub,
         "demographic/q12/ethnicity/ethnic4_other" => demographic.q12.ethnicity.ethnic4_other,
         "demographic/q12/ethnicity/hisp_oth" => demographic.q12.ethnicity.hisp_oth,
         "demographic/q12/matbplc_us" => demographic.q12.matbplc_us,
         "demographic/q12/matbplc_else" => demographic.q12.matbplc_else,
         "demographic/q12/matbplc_else_literal" => demographic.q12.matbplc_else_literal,
         "demographic/q14/occup" => demographic.q14.occup,
         "demographic/q14/indust" => demographic.q14.indust,
         "demographic/q14/industry_code_1" => demographic.q14.industry_code_1,
         "demographic/q14/industry_code_2" => demographic.q14.industry_code_2,
         "demographic/q14/industry_code_3" => demographic.q14.industry_code_3,
         "demographic/q14/occupation_code_1" => demographic.q14.occupation_code_1,
         "demographic/q14/occupation_code_2" => demographic.q14.occupation_code_2,
         "demographic/q14/occupation_code_3" => demographic.q14.occupation_code_3,
         "cause_of_death/coder" => cause_of_death.coder,
         "preparer_remarks/preparer_grp/review_1_remarks" => preparer_remarks.preparer_grp.review_1_remarks,
         "preparer_remarks/remarks_grp/note_to_vro" => preparer_remarks.remarks_grp.note_to_vro,
         "preparer_remarks/remarks_grp/remarks" => preparer_remarks.remarks_grp.remarks,
         "preparer_remarks/remarks_grp/update_remarks" => preparer_remarks.remarks_grp.update_remarks,
         "preparer_remarks/pdf_grp/pdf_link" => preparer_remarks.pdf_grp.pdf_link,
         "preparer_remarks/pdf_grp/pdf_steve_link" => preparer_remarks.pdf_grp.pdf_steve_link,
         "committee_review/reviewer_grp/review_2_remarks" => committee_review.reviewer_grp.review_2_remarks,
         "committee_review/rev_assessment_grp/dc_info_remarks" => committee_review.rev_assessment_grp.dc_info_remarks,
         "committee_review/rev_assessment_grp/mmria_used_remarks" => committee_review.rev_assessment_grp.mmria_used_remarks,
         "committee_review/agreement_grp/agreement_status" => committee_review.agreement_grp.agreement_status,
         "committee_review/agreement_grp/agreement_remarks" => committee_review.agreement_grp.agreement_remarks,
         "vro_case_determination/case_identifiers/dstate_dc_mirror" => vro_case_determination.case_identifiers.dstate_dc_mirror,
         "vro_case_determination/case_identifiers/fileno_dc_mirror" => vro_case_determination.case_identifiers.fileno_dc_mirror,
         "vro_case_determination/case_identifiers/auxno_dc_mirror" => vro_case_determination.case_identifiers.auxno_dc_mirror,
         "vro_case_determination/case_identifiers/dodeath_mirror/dod_mo_dc_mirror" => vro_case_determination.case_identifiers.dodeath_mirror.dod_mo_dc_mirror,
         "vro_case_determination/case_identifiers/dodeath_mirror/dod_dy_dc_mirror" => vro_case_determination.case_identifiers.dodeath_mirror.dod_dy_dc_mirror,
         "vro_case_determination/case_identifiers/dodeath_mirror/dod_yr_dc_mirror" => vro_case_determination.case_identifiers.dodeath_mirror.dod_yr_dc_mirror,
         "vro_case_determination/case_identifiers/dobirth_mirror/dob_mo_dc_mirror" => vro_case_determination.case_identifiers.dobirth_mirror.dob_mo_dc_mirror,
         "vro_case_determination/case_identifiers/dobirth_mirror/dob_dy_dc_mirror" => vro_case_determination.case_identifiers.dobirth_mirror.dob_dy_dc_mirror,
         "vro_case_determination/case_identifiers/dobirth_mirror/dob_yr_dc_mirror" => vro_case_determination.case_identifiers.dobirth_mirror.dob_yr_dc_mirror,
         "vro_case_determination/case_identifiers/fileno_bc_mirror" => vro_case_determination.case_identifiers.fileno_bc_mirror,
         "vro_case_determination/case_identifiers/fileno_fdc_mirror" => vro_case_determination.case_identifiers.fileno_fdc_mirror,
         "vro_case_determination/case_identifiers/year_birthorfetaldeath_mirror" => vro_case_determination.case_identifiers.year_birthorfetaldeath_mirror,
         "vro_case_determination/vro_update/vro_resolution_status" => vro_case_determination.vro_update.vro_resolution_status,
         "vro_case_determination/vro_update/vro_resolution_remarks" => vro_case_determination.vro_update.vro_resolution_remarks,
         "vro_case_determination/vro_update/vro_is_checkbox_correct" => vro_case_determination.vro_update.vro_is_checkbox_correct,
         "vro_case_determination/vro_update/vro_duration_endpreg_death" => vro_case_determination.vro_update.vro_duration_endpreg_death,
         "vro_case_determination/vro_update/vro_file_no_of_linked_lbfd" => vro_case_determination.vro_update.vro_file_no_of_linked_lbfd,
         "vro_case_determination/vro_update/note_to_vro_mirror" => vro_case_determination.vro_update.note_to_vro_mirror,
         "vro_case_determination/cause_details_mirror/manner_dc_mirror" => vro_case_determination.cause_details_mirror.manner_dc_mirror,
         "vro_case_determination/cause_details_mirror/cod1a_dc" => vro_case_determination.cause_details_mirror.cod1a_dc,
         "vro_case_determination/cause_details_mirror/interval1a_dc_mirror" => vro_case_determination.cause_details_mirror.interval1a_dc_mirror,
         "vro_case_determination/cause_details_mirror/cod1b_dc_mirror" => vro_case_determination.cause_details_mirror.cod1b_dc_mirror,
         "vro_case_determination/cause_details_mirror/interval1b_dc_mirror" => vro_case_determination.cause_details_mirror.interval1b_dc_mirror,
         "vro_case_determination/cause_details_mirror/cod1c_dc_mirror" => vro_case_determination.cause_details_mirror.cod1c_dc_mirror,
         "vro_case_determination/cause_details_mirror/interval1c_dc_mirror" => vro_case_determination.cause_details_mirror.interval1c_dc_mirror,
         "vro_case_determination/cause_details_mirror/cod1d_dc_mirror" => vro_case_determination.cause_details_mirror.cod1d_dc_mirror,
         "vro_case_determination/cause_details_mirror/interval1d_dc_mirror" => vro_case_determination.cause_details_mirror.interval1d_dc_mirror,
         "vro_case_determination/cause_details_mirror/othercondition_dc_mirror" => vro_case_determination.cause_details_mirror.othercondition_dc_mirror,
         "vro_case_determination/cause_details_mirror/man_uc_dc_mirror" => vro_case_determination.cause_details_mirror.man_uc_dc_mirror,
         "vro_case_determination/cause_details_mirror/acme_uc_dc_mirror" => vro_case_determination.cause_details_mirror.acme_uc_dc_mirror,
         "vro_case_determination/cause_details_mirror/eac_dc_mirror" => vro_case_determination.cause_details_mirror.eac_dc_mirror,
         "vro_case_determination/cause_details_mirror/rac_dc_mirror" => vro_case_determination.cause_details_mirror.rac_dc_mirror,
         "ije_dc/file_info/dstate_dc" => ije_dc.file_info.dstate_dc,
         "ije_dc/file_info/fileno_dc" => ije_dc.file_info.fileno_dc,
         "ije_dc/file_info/auxno_dc" => ije_dc.file_info.auxno_dc,
         "ije_dc/death_info/dod_mo_dc" => ije_dc.death_info.dod_mo_dc,
         "ije_dc/death_info/dod_dy_dc" => ije_dc.death_info.dod_dy_dc,
         "ije_dc/death_info/dod_yr_dc" => ije_dc.death_info.dod_yr_dc,
         "ije_dc/death_info/tod_dc" => ije_dc.death_info.tod_dc,
         "ije_dc/death_info/citytext_d_dc" => ije_dc.death_info.citytext_d_dc,
         "ije_dc/death_info/countytext_d_dc" => ije_dc.death_info.countytext_d_dc,
         "ije_dc/death_info/statetext_d_dc" => ije_dc.death_info.statetext_d_dc,
         "ije_dc/death_info/zip9_d_dc" => ije_dc.death_info.zip9_d_dc,
         "ije_dc/death_info/preg_dc" => ije_dc.death_info.preg_dc,
         "ije_dc/death_info/inact_dc" => ije_dc.death_info.inact_dc,
         "ije_dc/death_info/autop_dc" => ije_dc.death_info.autop_dc,
         "ije_dc/death_info/autopf_dc" => ije_dc.death_info.autopf_dc,
         "ije_dc/death_info/transprt_dc" => ije_dc.death_info.transprt_dc,
         "ije_dc/death_info/tobac_dc" => ije_dc.death_info.tobac_dc,
         "ije_dc/cause_details/manner_dc" => ije_dc.cause_details.manner_dc,
         "ije_dc/cause_details/cod1a_dc" => ije_dc.cause_details.cod1a_dc,
         "ije_dc/cause_details/interval1a_dc" => ije_dc.cause_details.interval1a_dc,
         "ije_dc/cause_details/cod1b_dc" => ije_dc.cause_details.cod1b_dc,
         "ije_dc/cause_details/interval1b_dc" => ije_dc.cause_details.interval1b_dc,
         "ije_dc/cause_details/cod1c_dc" => ije_dc.cause_details.cod1c_dc,
         "ije_dc/cause_details/interval1c_dc" => ije_dc.cause_details.interval1c_dc,
         "ije_dc/cause_details/cod1d_dc" => ije_dc.cause_details.cod1d_dc,
         "ije_dc/cause_details/interval1d_dc" => ije_dc.cause_details.interval1d_dc,
         "ije_dc/cause_details/othercondition_dc" => ije_dc.cause_details.othercondition_dc,
         "ije_dc/cause_details/man_uc_dc" => ije_dc.cause_details.man_uc_dc,
         "ije_dc/cause_details/acme_uc_dc" => ije_dc.cause_details.acme_uc_dc,
         "ije_dc/cause_details/eac_dc" => ije_dc.cause_details.eac_dc,
         "ije_dc/cause_details/rac_dc" => ije_dc.cause_details.rac_dc,
         "ije_dc/injury_details/doi_mo_dc" => ije_dc.injury_details.doi_mo_dc,
         "ije_dc/injury_details/doi_dy_dc" => ije_dc.injury_details.doi_dy_dc,
         "ije_dc/injury_details/doi_yr_dc" => ije_dc.injury_details.doi_yr_dc,
         "ije_dc/injury_details/toi_hr_dc" => ije_dc.injury_details.toi_hr_dc,
         "ije_dc/injury_details/howinj_dc" => ije_dc.injury_details.howinj_dc,
         "ije_dc/injury_details/workinj_dc" => ije_dc.injury_details.workinj_dc,
         "ije_dc/birthplace_mother/bplace_cnt_dc" => ije_dc.birthplace_mother.bplace_cnt_dc,
         "ije_dc/birthplace_mother/bplace_st_dc" => ije_dc.birthplace_mother.bplace_st_dc,
         "ije_dc/birthplace_mother/cityc_dc" => ije_dc.birthplace_mother.cityc_dc,
         "ije_dc/residence_mother/citytext_r_dc" => ije_dc.residence_mother.citytext_r_dc,
         "ije_dc/residence_mother/countyc_dc" => ije_dc.residence_mother.countyc_dc,
         "ije_dc/residence_mother/countrytext_r_dc" => ije_dc.residence_mother.countrytext_r_dc,
         "ije_dc/residence_mother/statec_dc" => ije_dc.residence_mother.statec_dc,
         "ije_dc/residence_mother/statetext_r_dc" => ije_dc.residence_mother.statetext_r_dc,
         "ije_dc/residence_mother/zip9_r_dc" => ije_dc.residence_mother.zip9_r_dc,
         "ije_dc/demog_details/dob_mo_dc" => ije_dc.demog_details.dob_mo_dc,
         "ije_dc/demog_details/dob_dy_dc" => ije_dc.demog_details.dob_dy_dc,
         "ije_dc/demog_details/dob_yr_dc" => ije_dc.demog_details.dob_yr_dc,
         "ije_dc/demog_details/age_dc" => ije_dc.demog_details.age_dc,
         "ije_dc/demog_details/sex_dc" => ije_dc.demog_details.sex_dc,
         "ije_dc/demog_details/marital_dc" => ije_dc.demog_details.marital_dc,
         "ije_dc/demog_details/indust_dc" => ije_dc.demog_details.indust_dc,
         "ije_dc/demog_details/occup_dc" => ije_dc.demog_details.occup_dc,
         "ije_dc/demog_details/armedf_dc" => ije_dc.demog_details.armedf_dc,
         "ije_dc/demog_details/dethnic1_dc" => ije_dc.demog_details.dethnic1_dc,
         "ije_dc/demog_details/dethnic2_dc" => ije_dc.demog_details.dethnic2_dc,
         "ije_dc/demog_details/dethnic3_dc" => ije_dc.demog_details.dethnic3_dc,
         "ije_dc/demog_details/dethnic4_dc" => ije_dc.demog_details.dethnic4_dc,
         "ije_dc/demog_details/dethnic5_dc" => ije_dc.demog_details.dethnic5_dc,
         "ije_dc/demog_details/race1_dc" => ije_dc.demog_details.race1_dc,
         "ije_dc/demog_details/race2_dc" => ije_dc.demog_details.race2_dc,
         "ije_dc/demog_details/race3_dc" => ije_dc.demog_details.race3_dc,
         "ije_dc/demog_details/race4_dc" => ije_dc.demog_details.race4_dc,
         "ije_dc/demog_details/race5_dc" => ije_dc.demog_details.race5_dc,
         "ije_dc/demog_details/race6_dc" => ije_dc.demog_details.race6_dc,
         "ije_dc/demog_details/race7_dc" => ije_dc.demog_details.race7_dc,
         "ije_dc/demog_details/race8_dc" => ije_dc.demog_details.race8_dc,
         "ije_dc/demog_details/race9_dc" => ije_dc.demog_details.race9_dc,
         "ije_dc/demog_details/race10_dc" => ije_dc.demog_details.race10_dc,
         "ije_dc/demog_details/race11_dc" => ije_dc.demog_details.race11_dc,
         "ije_dc/demog_details/race12_dc" => ije_dc.demog_details.race12_dc,
         "ije_dc/demog_details/race13_dc" => ije_dc.demog_details.race13_dc,
         "ije_dc/demog_details/race14_dc" => ije_dc.demog_details.race14_dc,
         "ije_dc/demog_details/race15_dc" => ije_dc.demog_details.race15_dc,
         "ije_dc/demog_details/race16_dc" => ije_dc.demog_details.race16_dc,
         "ije_dc/demog_details/race17_dc" => ije_dc.demog_details.race17_dc,
         "ije_dc/demog_details/race18_dc" => ije_dc.demog_details.race18_dc,
         "ije_dc/demog_details/race19_dc" => ije_dc.demog_details.race19_dc,
         "ije_dc/demog_details/race20_dc" => ije_dc.demog_details.race20_dc,
         "ije_dc/demog_details/race21_dc" => ije_dc.demog_details.race21_dc,
         "ije_dc/demog_details/race22_dc" => ije_dc.demog_details.race22_dc,
         "ije_dc/demog_details/race23_dc" => ije_dc.demog_details.race23_dc,
         "ije_bc/file_info/bstate_bc" => ije_bc.file_info.bstate_bc,
         "ije_bc/file_info/fileno_bc" => ije_bc.file_info.fileno_bc,
         "ije_bc/file_info/auxno_bc" => ije_bc.file_info.auxno_bc,
         "ije_bc/delivery_info/dwgt_bc" => ije_bc.delivery_info.dwgt_bc,
         "ije_bc/delivery_info/pwgt_bc" => ije_bc.delivery_info.pwgt_bc,
         "ije_bc/delivery_info/hft_bc" => ije_bc.delivery_info.hft_bc,
         "ije_bc/delivery_info/hin_bc" => ije_bc.delivery_info.hin_bc,
         "ije_bc/delivery_info/idob_mo_bc" => ije_bc.delivery_info.idob_mo_bc,
         "ije_bc/delivery_info/idob_dy_bc" => ije_bc.delivery_info.idob_dy_bc,
         "ije_bc/delivery_info/idob_yr_bc" => ije_bc.delivery_info.idob_yr_bc,
         "ije_bc/delivery_info/tb_bc" => ije_bc.delivery_info.tb_bc,
         "ije_bc/delivery_info/isex_bc" => ije_bc.delivery_info.isex_bc,
         "ije_bc/delivery_info/bwg_bc" => ije_bc.delivery_info.bwg_bc,
         "ije_bc/delivery_info/owgest_bc" => ije_bc.delivery_info.owgest_bc,
         "ije_bc/delivery_info/apgar5_bc" => ije_bc.delivery_info.apgar5_bc,
         "ije_bc/delivery_info/apgar10_bc" => ije_bc.delivery_info.apgar10_bc,
         "ije_bc/delivery_info/plur_bc" => ije_bc.delivery_info.plur_bc,
         "ije_bc/delivery_info/sord_bc" => ije_bc.delivery_info.sord_bc,
         "ije_bc/delivery_info/hosp_bc" => ije_bc.delivery_info.hosp_bc,
         "ije_bc/delivery_info/birth_co_bc" => ije_bc.delivery_info.birth_co_bc,
         "ije_bc/delivery_info/tran_bc" => ije_bc.delivery_info.tran_bc,
         "ije_bc/delivery_info/itran_bc" => ije_bc.delivery_info.itran_bc,
         "ije_bc/delivery_info/bfed_bc" => ije_bc.delivery_info.bfed_bc,
         "ije_bc/delivery_info/wic_bc" => ije_bc.delivery_info.wic_bc,
         "ije_bc/delivery_info/iliv_bc" => ije_bc.delivery_info.iliv_bc,
         "ije_bc/delivery_info/gon_bc" => ije_bc.delivery_info.gon_bc,
         "ije_bc/delivery_info/syph_bc" => ije_bc.delivery_info.syph_bc,
         "ije_bc/delivery_info/hsv_bc" => ije_bc.delivery_info.hsv_bc,
         "ije_bc/delivery_info/cham_bc" => ije_bc.delivery_info.cham_bc,
         "ije_bc/delivery_info/hepb_bc" => ije_bc.delivery_info.hepb_bc,
         "ije_bc/delivery_info/hepc_bc" => ije_bc.delivery_info.hepc_bc,
         "ije_bc/delivery_info/cerv_bc" => ije_bc.delivery_info.cerv_bc,
         "ije_bc/delivery_info/toc_bc" => ije_bc.delivery_info.toc_bc,
         "ije_bc/delivery_info/ecvs_bc" => ije_bc.delivery_info.ecvs_bc,
         "ije_bc/delivery_info/ecvf_bc" => ije_bc.delivery_info.ecvf_bc,
         "ije_bc/delivery_info/prom_bc" => ije_bc.delivery_info.prom_bc,
         "ije_bc/delivery_info/pric_bc" => ije_bc.delivery_info.pric_bc,
         "ije_bc/delivery_info/prol_bc" => ije_bc.delivery_info.prol_bc,
         "ije_bc/delivery_info/indl_bc" => ije_bc.delivery_info.indl_bc,
         "ije_bc/delivery_info/augl_bc" => ije_bc.delivery_info.augl_bc,
         "ije_bc/delivery_info/nvpr_bc" => ije_bc.delivery_info.nvpr_bc,
         "ije_bc/delivery_info/ster_bc" => ije_bc.delivery_info.ster_bc,
         "ije_bc/delivery_info/antb_bc" => ije_bc.delivery_info.antb_bc,
         "ije_bc/delivery_info/chor_bc" => ije_bc.delivery_info.chor_bc,
         "ije_bc/delivery_info/mecs_bc" => ije_bc.delivery_info.mecs_bc,
         "ije_bc/delivery_info/fint_bc" => ije_bc.delivery_info.fint_bc,
         "ije_bc/delivery_info/esan_bc" => ije_bc.delivery_info.esan_bc,
         "ije_bc/delivery_info/tlab_bc" => ije_bc.delivery_info.tlab_bc,
         "ije_bc/delivery_info/mtr_bc" => ije_bc.delivery_info.mtr_bc,
         "ije_bc/delivery_info/plac_bc" => ije_bc.delivery_info.plac_bc,
         "ije_bc/delivery_info/rut_bc" => ije_bc.delivery_info.rut_bc,
         "ije_bc/delivery_info/uhys_bc" => ije_bc.delivery_info.uhys_bc,
         "ije_bc/delivery_info/aint_bc" => ije_bc.delivery_info.aint_bc,
         "ije_bc/delivery_info/uopr_bc" => ije_bc.delivery_info.uopr_bc,
         "ije_bc/delivery_info/aven1_bc" => ije_bc.delivery_info.aven1_bc,
         "ije_bc/delivery_info/aven6_bc" => ije_bc.delivery_info.aven6_bc,
         "ije_bc/delivery_info/nicu_bc" => ije_bc.delivery_info.nicu_bc,
         "ije_bc/delivery_info/surf_bc" => ije_bc.delivery_info.surf_bc,
         "ije_bc/delivery_info/anti_bc" => ije_bc.delivery_info.anti_bc,
         "ije_bc/delivery_info/seiz_bc" => ije_bc.delivery_info.seiz_bc,
         "ije_bc/delivery_info/binj_bc" => ije_bc.delivery_info.binj_bc,
         "ije_bc/delivery_info/anen_bc" => ije_bc.delivery_info.anen_bc,
         "ije_bc/delivery_info/mnsb_bc" => ije_bc.delivery_info.mnsb_bc,
         "ije_bc/delivery_info/cchd_bc" => ije_bc.delivery_info.cchd_bc,
         "ije_bc/delivery_info/cdh_bc" => ije_bc.delivery_info.cdh_bc,
         "ije_bc/delivery_info/omph_bc" => ije_bc.delivery_info.omph_bc,
         "ije_bc/delivery_info/gast_bc" => ije_bc.delivery_info.gast_bc,
         "ije_bc/delivery_info/limb_bc" => ije_bc.delivery_info.limb_bc,
         "ije_bc/delivery_info/cl_bc" => ije_bc.delivery_info.cl_bc,
         "ije_bc/delivery_info/cp_bc" => ije_bc.delivery_info.cp_bc,
         "ije_bc/delivery_info/dowt_bc" => ije_bc.delivery_info.dowt_bc,
         "ije_bc/delivery_info/cdit_bc" => ije_bc.delivery_info.cdit_bc,
         "ije_bc/delivery_info/hypo_bc" => ije_bc.delivery_info.hypo_bc,
         "ije_bc/previous_info/dlmp_mo_bc" => ije_bc.previous_info.dlmp_mo_bc,
         "ije_bc/previous_info/dlmp_dy_bc" => ije_bc.previous_info.dlmp_dy_bc,
         "ije_bc/previous_info/dlmp_yr_bc" => ije_bc.previous_info.dlmp_yr_bc,
         "ije_bc/previous_info/dofp_mo_bc" => ije_bc.previous_info.dofp_mo_bc,
         "ije_bc/previous_info/dofp_dy_bc" => ije_bc.previous_info.dofp_dy_bc,
         "ije_bc/previous_info/dofp_yr_bc" => ije_bc.previous_info.dofp_yr_bc,
         "ije_bc/previous_info/dolp_mo_bc" => ije_bc.previous_info.dolp_mo_bc,
         "ije_bc/previous_info/dolp_dy_bc" => ije_bc.previous_info.dolp_dy_bc,
         "ije_bc/previous_info/dolp_yr_bc" => ije_bc.previous_info.dolp_yr_bc,
         "ije_bc/previous_info/nprev_bc" => ije_bc.previous_info.nprev_bc,
         "ije_bc/previous_info/plbl_bc" => ije_bc.previous_info.plbl_bc,
         "ije_bc/previous_info/plbd_bc" => ije_bc.previous_info.plbd_bc,
         "ije_bc/previous_info/popo_bc" => ije_bc.previous_info.popo_bc,
         "ije_bc/previous_info/mllb_bc" => ije_bc.previous_info.mllb_bc,
         "ije_bc/previous_info/yllb_bc" => ije_bc.previous_info.yllb_bc,
         "ije_bc/previous_info/mopo_bc" => ije_bc.previous_info.mopo_bc,
         "ije_bc/previous_info/yopo_bc" => ije_bc.previous_info.yopo_bc,
         "ije_bc/previous_info/cigpn_bc" => ije_bc.previous_info.cigpn_bc,
         "ije_bc/previous_info/cigfn_bc" => ije_bc.previous_info.cigfn_bc,
         "ije_bc/previous_info/cigsn_bc" => ije_bc.previous_info.cigsn_bc,
         "ije_bc/previous_info/cigln_bc" => ije_bc.previous_info.cigln_bc,
         "ije_bc/previous_info/pdiab_bc" => ije_bc.previous_info.pdiab_bc,
         "ije_bc/previous_info/gdiab_bc" => ije_bc.previous_info.gdiab_bc,
         "ije_bc/previous_info/phype_bc" => ije_bc.previous_info.phype_bc,
         "ije_bc/previous_info/ghype_bc" => ije_bc.previous_info.ghype_bc,
         "ije_bc/previous_info/ppb_bc" => ije_bc.previous_info.ppb_bc,
         "ije_bc/previous_info/ppo_bc" => ije_bc.previous_info.ppo_bc,
         "ije_bc/previous_info/inft_bc" => ije_bc.previous_info.inft_bc,
         "ije_bc/previous_info/pces_bc" => ije_bc.previous_info.pces_bc,
         "ije_bc/previous_info/npces_bc" => ije_bc.previous_info.npces_bc,
         "ije_bc/previous_info/ehype_bc" => ije_bc.previous_info.ehype_bc,
         "ije_bc/previous_info/inft_drg_bc" => ije_bc.previous_info.inft_drg_bc,
         "ije_bc/previous_info/inft_art_bc" => ije_bc.previous_info.inft_art_bc,
         "ije_bc/residence_mother/citytext_bc" => ije_bc.residence_mother.citytext_bc,
         "ije_bc/residence_mother/countytxt_bc" => ije_bc.residence_mother.countytxt_bc,
         "ije_bc/residence_mother/statetxt_bc" => ije_bc.residence_mother.statetxt_bc,
         "ije_bc/residence_mother/zipcode_bc" => ije_bc.residence_mother.zipcode_bc,
         "ije_bc/demog_details/mbplace_st_ter_txt_bc" => ije_bc.demog_details.mbplace_st_ter_txt_bc,
         "ije_bc/demog_details/mbplace_cntry_txt_bc" => ije_bc.demog_details.mbplace_cntry_txt_bc,
         "ije_bc/demog_details/mager_bc" => ije_bc.demog_details.mager_bc,
         "ije_bc/demog_details/mdob_mo_bc" => ije_bc.demog_details.mdob_mo_bc,
         "ije_bc/demog_details/mdob_dy_bc" => ije_bc.demog_details.mdob_dy_bc,
         "ije_bc/demog_details/mdob_yr_bc" => ije_bc.demog_details.mdob_yr_bc,
         "ije_bc/demog_details/marn_bc" => ije_bc.demog_details.marn_bc,
         "ije_bc/demog_details/ackn_bc" => ije_bc.demog_details.ackn_bc,
         "ije_bc/demog_details/mom_in_t_bc" => ije_bc.demog_details.mom_in_t_bc,
         "ije_bc/demog_details/mom_oc_t_bc" => ije_bc.demog_details.mom_oc_t_bc,
         "ije_bc/demog_details/methnic1_bc" => ije_bc.demog_details.methnic1_bc,
         "ije_bc/demog_details/methnic2_bc" => ije_bc.demog_details.methnic2_bc,
         "ije_bc/demog_details/methnic3_bc" => ije_bc.demog_details.methnic3_bc,
         "ije_bc/demog_details/methnic4_bc" => ije_bc.demog_details.methnic4_bc,
         "ije_bc/demog_details/methnic5_bc" => ije_bc.demog_details.methnic5_bc,
         "ije_bc/demog_details/mrace1_bc" => ije_bc.demog_details.mrace1_bc,
         "ije_bc/demog_details/mrace2_bc" => ije_bc.demog_details.mrace2_bc,
         "ije_bc/demog_details/mrace3_bc" => ije_bc.demog_details.mrace3_bc,
         "ije_bc/demog_details/mrace4_bc" => ije_bc.demog_details.mrace4_bc,
         "ije_bc/demog_details/mrace5_bc" => ije_bc.demog_details.mrace5_bc,
         "ije_bc/demog_details/mrace6_bc" => ije_bc.demog_details.mrace6_bc,
         "ije_bc/demog_details/mrace7_bc" => ije_bc.demog_details.mrace7_bc,
         "ije_bc/demog_details/mrace8_bc" => ije_bc.demog_details.mrace8_bc,
         "ije_bc/demog_details/mrace9_bc" => ije_bc.demog_details.mrace9_bc,
         "ije_bc/demog_details/mrace10_bc" => ije_bc.demog_details.mrace10_bc,
         "ije_bc/demog_details/mrace11_bc" => ije_bc.demog_details.mrace11_bc,
         "ije_bc/demog_details/mrace12_bc" => ije_bc.demog_details.mrace12_bc,
         "ije_bc/demog_details/mrace13_bc" => ije_bc.demog_details.mrace13_bc,
         "ije_bc/demog_details/mrace14_bc" => ije_bc.demog_details.mrace14_bc,
         "ije_bc/demog_details/mrace15_bc" => ije_bc.demog_details.mrace15_bc,
         "ije_bc/demog_details/mrace16_bc" => ije_bc.demog_details.mrace16_bc,
         "ije_bc/demog_details/mrace17_bc" => ije_bc.demog_details.mrace17_bc,
         "ije_bc/demog_details/mrace18_bc" => ije_bc.demog_details.mrace18_bc,
         "ije_bc/demog_details/mrace19_bc" => ije_bc.demog_details.mrace19_bc,
         "ije_bc/demog_details/mrace20_bc" => ije_bc.demog_details.mrace20_bc,
         "ije_bc/demog_details/mrace21_bc" => ije_bc.demog_details.mrace21_bc,
         "ije_bc/demog_details/mrace22_bc" => ije_bc.demog_details.mrace22_bc,
         "ije_bc/demog_details/mrace23_bc" => ije_bc.demog_details.mrace23_bc,
         "ije_bc/demog_details/fager_bc" => ije_bc.demog_details.fager_bc,
         "ije_bc/demog_details/dad_in_t_bc" => ije_bc.demog_details.dad_in_t_bc,
         "ije_bc/demog_details/dad_oc_t_bc" => ije_bc.demog_details.dad_oc_t_bc,
         "ije_bc/demog_details/fbplacd_st_ter_c_bc" => ije_bc.demog_details.fbplacd_st_ter_c_bc,
         "ije_bc/demog_details/fbplace_cnt_c_bc" => ije_bc.demog_details.fbplace_cnt_c_bc,
         "ije_fetaldc/file_info/dstate_fdc" => ije_fetaldc.file_info.dstate_fdc,
         "ije_fetaldc/file_info/fileno_fdc" => ije_fetaldc.file_info.fileno_fdc,
         "ije_fetaldc/file_info/auxno_fdc" => ije_fetaldc.file_info.auxno_fdc,
         "ije_fetaldc/delivery_info/fdod_mo_fdc" => ije_fetaldc.delivery_info.fdod_mo_fdc,
         "ije_fetaldc/delivery_info/fdod_dy_fdc" => ije_fetaldc.delivery_info.fdod_dy_fdc,
         "ije_fetaldc/delivery_info/fdod_yr_fdc" => ije_fetaldc.delivery_info.fdod_yr_fdc,
         "ije_fetaldc/delivery_info/td_fdc" => ije_fetaldc.delivery_info.td_fdc,
         "ije_fetaldc/delivery_info/dwgt_fdc" => ije_fetaldc.delivery_info.dwgt_fdc,
         "ije_fetaldc/delivery_info/pwgt_fdc" => ije_fetaldc.delivery_info.pwgt_fdc,
         "ije_fetaldc/delivery_info/hft_fdc" => ije_fetaldc.delivery_info.hft_fdc,
         "ije_fetaldc/delivery_info/hin_fdc" => ije_fetaldc.delivery_info.hin_fdc,
         "ije_fetaldc/delivery_info/fsex_fdc" => ije_fetaldc.delivery_info.fsex_fdc,
         "ije_fetaldc/delivery_info/fwg_fdc" => ije_fetaldc.delivery_info.fwg_fdc,
         "ije_fetaldc/delivery_info/owgest_fdc" => ije_fetaldc.delivery_info.owgest_fdc,
         "ije_fetaldc/delivery_info/plur_fdc" => ije_fetaldc.delivery_info.plur_fdc,
         "ije_fetaldc/delivery_info/sord_fdc" => ije_fetaldc.delivery_info.sord_fdc,
         "ije_fetaldc/delivery_info/hosp_d_fdc" => ije_fetaldc.delivery_info.hosp_d_fdc,
         "ije_fetaldc/delivery_info/cnty_d_fdc" => ije_fetaldc.delivery_info.cnty_d_fdc,
         "ije_fetaldc/delivery_info/city_d_fdc" => ije_fetaldc.delivery_info.city_d_fdc,
         "ije_fetaldc/delivery_info/tran_fdc" => ije_fetaldc.delivery_info.tran_fdc,
         "ije_fetaldc/delivery_info/wic_fdc" => ije_fetaldc.delivery_info.wic_fdc,
         "ije_fetaldc/delivery_info/gon_fdc" => ije_fetaldc.delivery_info.gon_fdc,
         "ije_fetaldc/delivery_info/syph_fdc" => ije_fetaldc.delivery_info.syph_fdc,
         "ije_fetaldc/delivery_info/hsv_fdc" => ije_fetaldc.delivery_info.hsv_fdc,
         "ije_fetaldc/delivery_info/cham_fdc" => ije_fetaldc.delivery_info.cham_fdc,
         "ije_fetaldc/delivery_info/lm_fdc" => ije_fetaldc.delivery_info.lm_fdc,
         "ije_fetaldc/delivery_info/gbs_fdc" => ije_fetaldc.delivery_info.gbs_fdc,
         "ije_fetaldc/delivery_info/cmv_fdc" => ije_fetaldc.delivery_info.cmv_fdc,
         "ije_fetaldc/delivery_info/b19_fdc" => ije_fetaldc.delivery_info.b19_fdc,
         "ije_fetaldc/delivery_info/toxo_fdc" => ije_fetaldc.delivery_info.toxo_fdc,
         "ije_fetaldc/delivery_info/hsv1_fdc" => ije_fetaldc.delivery_info.hsv1_fdc,
         "ije_fetaldc/delivery_info/hiv_fdc" => ije_fetaldc.delivery_info.hiv_fdc,
         "ije_fetaldc/delivery_info/tlab_fdc" => ije_fetaldc.delivery_info.tlab_fdc,
         "ije_fetaldc/delivery_info/otheri_fdc" => ije_fetaldc.delivery_info.otheri_fdc,
         "ije_fetaldc/delivery_info/mtr_fdc" => ije_fetaldc.delivery_info.mtr_fdc,
         "ije_fetaldc/delivery_info/plac_fdc" => ije_fetaldc.delivery_info.plac_fdc,
         "ije_fetaldc/delivery_info/rut_fdc" => ije_fetaldc.delivery_info.rut_fdc,
         "ije_fetaldc/delivery_info/uhys_fdc" => ije_fetaldc.delivery_info.uhys_fdc,
         "ije_fetaldc/delivery_info/aint_fdc" => ije_fetaldc.delivery_info.aint_fdc,
         "ije_fetaldc/delivery_info/uopr_fdc" => ije_fetaldc.delivery_info.uopr_fdc,
         "ije_fetaldc/delivery_info/anen_fdc" => ije_fetaldc.delivery_info.anen_fdc,
         "ije_fetaldc/delivery_info/mnsb_fdc" => ije_fetaldc.delivery_info.mnsb_fdc,
         "ije_fetaldc/delivery_info/cchd_fdc" => ije_fetaldc.delivery_info.cchd_fdc,
         "ije_fetaldc/delivery_info/cdh_fdc" => ije_fetaldc.delivery_info.cdh_fdc,
         "ije_fetaldc/delivery_info/omph_fdc" => ije_fetaldc.delivery_info.omph_fdc,
         "ije_fetaldc/delivery_info/gast_fdc" => ije_fetaldc.delivery_info.gast_fdc,
         "ije_fetaldc/delivery_info/limb_fdc" => ije_fetaldc.delivery_info.limb_fdc,
         "ije_fetaldc/delivery_info/cl_fdc" => ije_fetaldc.delivery_info.cl_fdc,
         "ije_fetaldc/delivery_info/cp_fdc" => ije_fetaldc.delivery_info.cp_fdc,
         "ije_fetaldc/delivery_info/dowt_fdc" => ije_fetaldc.delivery_info.dowt_fdc,
         "ije_fetaldc/delivery_info/cdit_fdc" => ije_fetaldc.delivery_info.cdit_fdc,
         "ije_fetaldc/delivery_info/hypo_fdc" => ije_fetaldc.delivery_info.hypo_fdc,
         "ije_fetaldc/condition_cause/cod18a1_fdc" => ije_fetaldc.condition_cause.cod18a1_fdc,
         "ije_fetaldc/condition_cause/cod18a2_fdc" => ije_fetaldc.condition_cause.cod18a2_fdc,
         "ije_fetaldc/condition_cause/cod18a3_fdc" => ije_fetaldc.condition_cause.cod18a3_fdc,
         "ije_fetaldc/condition_cause/cod18a4_fdc" => ije_fetaldc.condition_cause.cod18a4_fdc,
         "ije_fetaldc/condition_cause/cod18a5_fdc" => ije_fetaldc.condition_cause.cod18a5_fdc,
         "ije_fetaldc/condition_cause/cod18a6_fdc" => ije_fetaldc.condition_cause.cod18a6_fdc,
         "ije_fetaldc/condition_cause/cod18a7_fdc" => ije_fetaldc.condition_cause.cod18a7_fdc,
         "ije_fetaldc/condition_cause/cod18a8_fdc" => ije_fetaldc.condition_cause.cod18a8_fdc,
         "ije_fetaldc/condition_cause/cod18a9_fdc" => ije_fetaldc.condition_cause.cod18a9_fdc,
         "ije_fetaldc/condition_cause/cod18a10_fdc" => ije_fetaldc.condition_cause.cod18a10_fdc,
         "ije_fetaldc/condition_cause/cod18a11_fdc" => ije_fetaldc.condition_cause.cod18a11_fdc,
         "ije_fetaldc/condition_cause/cod18a12_fdc" => ije_fetaldc.condition_cause.cod18a12_fdc,
         "ije_fetaldc/condition_cause/cod18a13_fdc" => ije_fetaldc.condition_cause.cod18a13_fdc,
         "ije_fetaldc/condition_cause/cod18a14_fdc" => ije_fetaldc.condition_cause.cod18a14_fdc,
         "ije_fetaldc/condition_cause/cod18b1_fdc" => ije_fetaldc.condition_cause.cod18b1_fdc,
         "ije_fetaldc/condition_cause/cod18b2_fdc" => ije_fetaldc.condition_cause.cod18b2_fdc,
         "ije_fetaldc/condition_cause/cod18b3_fdc" => ije_fetaldc.condition_cause.cod18b3_fdc,
         "ije_fetaldc/condition_cause/cod18b4_fdc" => ije_fetaldc.condition_cause.cod18b4_fdc,
         "ije_fetaldc/condition_cause/cod18b5_fdc" => ije_fetaldc.condition_cause.cod18b5_fdc,
         "ije_fetaldc/condition_cause/cod18b6_fdc" => ije_fetaldc.condition_cause.cod18b6_fdc,
         "ije_fetaldc/condition_cause/cod18b7_fdc" => ije_fetaldc.condition_cause.cod18b7_fdc,
         "ije_fetaldc/condition_cause/cod18b8_fdc" => ije_fetaldc.condition_cause.cod18b8_fdc,
         "ije_fetaldc/condition_cause/cod18b9_fdc" => ije_fetaldc.condition_cause.cod18b9_fdc,
         "ije_fetaldc/condition_cause/cod18b10_fdc" => ije_fetaldc.condition_cause.cod18b10_fdc,
         "ije_fetaldc/condition_cause/cod18b11_fdc" => ije_fetaldc.condition_cause.cod18b11_fdc,
         "ije_fetaldc/condition_cause/cod18b12_fdc" => ije_fetaldc.condition_cause.cod18b12_fdc,
         "ije_fetaldc/condition_cause/cod18b13_fdc" => ije_fetaldc.condition_cause.cod18b13_fdc,
         "ije_fetaldc/condition_cause/cod18b14_fdc" => ije_fetaldc.condition_cause.cod18b14_fdc,
         "ije_fetaldc/condition_cause/icod_fdc" => ije_fetaldc.condition_cause.icod_fdc,
         "ije_fetaldc/condition_cause/ocod1_fdc" => ije_fetaldc.condition_cause.ocod1_fdc,
         "ije_fetaldc/condition_cause/ocod2_fdc" => ije_fetaldc.condition_cause.ocod2_fdc,
         "ije_fetaldc/condition_cause/ocod3_fdc" => ije_fetaldc.condition_cause.ocod3_fdc,
         "ije_fetaldc/condition_cause/ocod4_fdc" => ije_fetaldc.condition_cause.ocod4_fdc,
         "ije_fetaldc/condition_cause/ocod5_fdc" => ije_fetaldc.condition_cause.ocod5_fdc,
         "ije_fetaldc/condition_cause/ocod6_fdc" => ije_fetaldc.condition_cause.ocod6_fdc,
         "ije_fetaldc/condition_cause/ocod7_fdc" => ije_fetaldc.condition_cause.ocod7_fdc,
         "ije_fetaldc/previous_info/dlmp_mo_fdc" => ije_fetaldc.previous_info.dlmp_mo_fdc,
         "ije_fetaldc/previous_info/dlmp_dy_fdc" => ije_fetaldc.previous_info.dlmp_dy_fdc,
         "ije_fetaldc/previous_info/dlmp_yr_fdc" => ije_fetaldc.previous_info.dlmp_yr_fdc,
         "ije_fetaldc/previous_info/dofp_mo_fdc" => ije_fetaldc.previous_info.dofp_mo_fdc,
         "ije_fetaldc/previous_info/dofp_dy_fdc" => ije_fetaldc.previous_info.dofp_dy_fdc,
         "ije_fetaldc/previous_info/dofp_yr_fdc" => ije_fetaldc.previous_info.dofp_yr_fdc,
         "ije_fetaldc/previous_info/dolp_mo_fdc" => ije_fetaldc.previous_info.dolp_mo_fdc,
         "ije_fetaldc/previous_info/dolp_dy_fdc" => ije_fetaldc.previous_info.dolp_dy_fdc,
         "ije_fetaldc/previous_info/dolp_yr_fdc" => ije_fetaldc.previous_info.dolp_yr_fdc,
         "ije_fetaldc/previous_info/nprev_fdc" => ije_fetaldc.previous_info.nprev_fdc,
         "ije_fetaldc/previous_info/plbl_fdc" => ije_fetaldc.previous_info.plbl_fdc,
         "ije_fetaldc/previous_info/plbd_fdc" => ije_fetaldc.previous_info.plbd_fdc,
         "ije_fetaldc/previous_info/popo_fdc" => ije_fetaldc.previous_info.popo_fdc,
         "ije_fetaldc/previous_info/mllb_fdc" => ije_fetaldc.previous_info.mllb_fdc,
         "ije_fetaldc/previous_info/yllb_fdc" => ije_fetaldc.previous_info.yllb_fdc,
         "ije_fetaldc/previous_info/mopo_fdc" => ije_fetaldc.previous_info.mopo_fdc,
         "ije_fetaldc/previous_info/yopo_fdc" => ije_fetaldc.previous_info.yopo_fdc,
         "ije_fetaldc/previous_info/cigpn_fdc" => ije_fetaldc.previous_info.cigpn_fdc,
         "ije_fetaldc/previous_info/cigfn_fdc" => ije_fetaldc.previous_info.cigfn_fdc,
         "ije_fetaldc/previous_info/cigsn_fdc" => ije_fetaldc.previous_info.cigsn_fdc,
         "ije_fetaldc/previous_info/cigln_fdc" => ije_fetaldc.previous_info.cigln_fdc,
         "ije_fetaldc/previous_info/pdiab_fdc" => ije_fetaldc.previous_info.pdiab_fdc,
         "ije_fetaldc/previous_info/gdiab_fdc" => ije_fetaldc.previous_info.gdiab_fdc,
         "ije_fetaldc/previous_info/phype_fdc" => ije_fetaldc.previous_info.phype_fdc,
         "ije_fetaldc/previous_info/ghype_fdc" => ije_fetaldc.previous_info.ghype_fdc,
         "ije_fetaldc/previous_info/ppb_fdc" => ije_fetaldc.previous_info.ppb_fdc,
         "ije_fetaldc/previous_info/ppo_fdc" => ije_fetaldc.previous_info.ppo_fdc,
         "ije_fetaldc/previous_info/inft_fdc" => ije_fetaldc.previous_info.inft_fdc,
         "ije_fetaldc/previous_info/pces_fdc" => ije_fetaldc.previous_info.pces_fdc,
         "ije_fetaldc/previous_info/npces_fdc" => ije_fetaldc.previous_info.npces_fdc,
         "ije_fetaldc/previous_info/ehype_fdc" => ije_fetaldc.previous_info.ehype_fdc,
         "ije_fetaldc/previous_info/inft_drg_fdc" => ije_fetaldc.previous_info.inft_drg_fdc,
         "ije_fetaldc/previous_info/inft_art_fdc" => ije_fetaldc.previous_info.inft_art_fdc,
         "ije_fetaldc/residence_mother/citytxt_fdc" => ije_fetaldc.residence_mother.citytxt_fdc,
         "ije_fetaldc/residence_mother/countytxt_fdc" => ije_fetaldc.residence_mother.countytxt_fdc,
         "ije_fetaldc/residence_mother/statetxt_fdc" => ije_fetaldc.residence_mother.statetxt_fdc,
         "ije_fetaldc/residence_mother/zipcode_fdc" => ije_fetaldc.residence_mother.zipcode_fdc,
         "ije_fetaldc/demog_details/mbplace_st_ter_txt_fdc" => ije_fetaldc.demog_details.mbplace_st_ter_txt_fdc,
         "ije_fetaldc/demog_details/mbplace_cntry_txt_fdc" => ije_fetaldc.demog_details.mbplace_cntry_txt_fdc,
         "ije_fetaldc/demog_details/mager_fdc" => ije_fetaldc.demog_details.mager_fdc,
         "ije_fetaldc/demog_details/mdob_mo_fdc" => ije_fetaldc.demog_details.mdob_mo_fdc,
         "ije_fetaldc/demog_details/mdob_dy_fdc" => ije_fetaldc.demog_details.mdob_dy_fdc,
         "ije_fetaldc/demog_details/mdob_yr_fdc" => ije_fetaldc.demog_details.mdob_yr_fdc,
         "ije_fetaldc/demog_details/marn_fdc" => ije_fetaldc.demog_details.marn_fdc,
         "ije_fetaldc/demog_details/mom_in_t_fdc" => ije_fetaldc.demog_details.mom_in_t_fdc,
         "ije_fetaldc/demog_details/mom_oc_t_fdc" => ije_fetaldc.demog_details.mom_oc_t_fdc,
         "ije_fetaldc/demog_details/methnic1_fdc" => ije_fetaldc.demog_details.methnic1_fdc,
         "ije_fetaldc/demog_details/methnic2_fdc" => ije_fetaldc.demog_details.methnic2_fdc,
         "ije_fetaldc/demog_details/methnic3_fdc" => ije_fetaldc.demog_details.methnic3_fdc,
         "ije_fetaldc/demog_details/methnic4_fdc" => ije_fetaldc.demog_details.methnic4_fdc,
         "ije_fetaldc/demog_details/methnic5_fdc" => ije_fetaldc.demog_details.methnic5_fdc,
         "ije_fetaldc/demog_details/mrace1_fdc" => ije_fetaldc.demog_details.mrace1_fdc,
         "ije_fetaldc/demog_details/mrace2_fdc" => ije_fetaldc.demog_details.mrace2_fdc,
         "ije_fetaldc/demog_details/mrace3_fdc" => ije_fetaldc.demog_details.mrace3_fdc,
         "ije_fetaldc/demog_details/mrace4_fdc" => ije_fetaldc.demog_details.mrace4_fdc,
         "ije_fetaldc/demog_details/mrace5_fdc" => ije_fetaldc.demog_details.mrace5_fdc,
         "ije_fetaldc/demog_details/mrace6_fdc" => ije_fetaldc.demog_details.mrace6_fdc,
         "ije_fetaldc/demog_details/mrace7_fdc" => ije_fetaldc.demog_details.mrace7_fdc,
         "ije_fetaldc/demog_details/mrace8_fdc" => ije_fetaldc.demog_details.mrace8_fdc,
         "ije_fetaldc/demog_details/mrace9_fdc" => ije_fetaldc.demog_details.mrace9_fdc,
         "ije_fetaldc/demog_details/mrace10_fdc" => ije_fetaldc.demog_details.mrace10_fdc,
         "ije_fetaldc/demog_details/mrace11_fdc" => ije_fetaldc.demog_details.mrace11_fdc,
         "ije_fetaldc/demog_details/mrace12_fdc" => ije_fetaldc.demog_details.mrace12_fdc,
         "ije_fetaldc/demog_details/mrace13_fdc" => ije_fetaldc.demog_details.mrace13_fdc,
         "ije_fetaldc/demog_details/mrace14_fdc" => ije_fetaldc.demog_details.mrace14_fdc,
         "ije_fetaldc/demog_details/mrace15_fdc" => ije_fetaldc.demog_details.mrace15_fdc,
         "ije_fetaldc/demog_details/mrace16_fdc" => ije_fetaldc.demog_details.mrace16_fdc,
         "ije_fetaldc/demog_details/mrace17_fdc" => ije_fetaldc.demog_details.mrace17_fdc,
         "ije_fetaldc/demog_details/mrace18_fdc" => ije_fetaldc.demog_details.mrace18_fdc,
         "ije_fetaldc/demog_details/mrace19_fdc" => ije_fetaldc.demog_details.mrace19_fdc,
         "ije_fetaldc/demog_details/mrace20_fdc" => ije_fetaldc.demog_details.mrace20_fdc,
         "ije_fetaldc/demog_details/mrace21_fdc" => ije_fetaldc.demog_details.mrace21_fdc,
         "ije_fetaldc/demog_details/mrace22_fdc" => ije_fetaldc.demog_details.mrace22_fdc,
         "ije_fetaldc/demog_details/mrace23_fdc" => ije_fetaldc.demog_details.mrace23_fdc,
         "ije_fetaldc/demog_details/fager_fdc" => ije_fetaldc.demog_details.fager_fdc,
         "ije_fetaldc/demog_details/dad_in_t_fdc" => ije_fetaldc.demog_details.dad_in_t_fdc,
         "ije_fetaldc/demog_details/dad_oc_t_fdc" => ije_fetaldc.demog_details.dad_oc_t_fdc,
         "ije_fetaldc/demog_details/fbplacd_st_ter_c_fdc" => ije_fetaldc.demog_details.fbplacd_st_ter_c_fdc,
         "ije_fetaldc/demog_details/fbplace_cnt_c_fdc" => ije_fetaldc.demog_details.fbplace_cnt_c_fdc,
         "amss_tracking/admin_grp/amss_status" => amss_tracking.admin_grp.amss_status,
         "amss_tracking/assessment_grp/classification_diagnosis" => amss_tracking.assessment_grp.classification_diagnosis,
         "amss_tracking/assessment_grp/remarks" => amss_tracking.assessment_grp.remarks,
         "amss_tracking/folder_grp/amss_folder" => amss_tracking.folder_grp.amss_folder,

                _ => null
            };
        }
        catch(Exception)
        {
            
        }
        return result;
    }

    public double? GetS_Double(string path)
    {
        double? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "tracking/admin_info/track_year" => tracking.admin_info.track_year,
         "tracking/admin_info/med_coder_check" => tracking.admin_info.med_coder_check,
         "tracking/admin_info/med_dir_check" => tracking.admin_info.med_dir_check,
         "tracking/admin_info/steve_transfer" => tracking.admin_info.steve_transfer,
         "tracking/q1/amssrel" => tracking.q1.amssrel,
         "tracking/date_of_death/month" => tracking.date_of_death.month,
         "tracking/date_of_death/day" => tracking.date_of_death.day,
         "tracking/date_of_death/year" => tracking.date_of_death.year,
         "tracking/sourcnot" => tracking.sourcnot,
         "tracking/dcfile" => tracking.dcfile,
         "tracking/lbfile" => tracking.lbfile,
         "tracking/q7/pregstat" => tracking.q7.pregstat,
         "tracking/q7/pcbtime" => tracking.q7.pcbtime,
         "tracking/q9/zipsrce" => tracking.q9.zipsrce,
         "tracking/q9/cntysrce" => tracking.q9.cntysrce,
         "tracking/cdc_case_matching_results/preg_cb_match" => tracking.cdc_case_matching_results.preg_cb_match,
         "tracking/cdc_case_matching_results/literal_cod_match" => tracking.cdc_case_matching_results.literal_cod_match,
         "tracking/cdc_case_matching_results/icd10_match" => tracking.cdc_case_matching_results.icd10_match,
         "tracking/cdc_case_matching_results/bc_match" => tracking.cdc_case_matching_results.bc_match,
         "tracking/cdc_case_matching_results/fdc_match" => tracking.cdc_case_matching_results.fdc_match,
         "demographic/mage" => demographic.mage,
         "demographic/date_of_birth/month" => demographic.date_of_birth.month,
         "demographic/date_of_birth/day" => demographic.date_of_birth.day,
         "demographic/date_of_birth/year" => demographic.date_of_birth.year,
         "demographic/date_of_birth/agedif" => demographic.date_of_birth.agedif,
         "demographic/q12/race" => demographic.q12.race,
         "demographic/q12/group/race_omb" => demographic.q12.group.race_omb,
         "demographic/q12/ethnicity/hisporg" => demographic.q12.ethnicity.hisporg,
         "demographic/q12/matbplc" => demographic.q12.matbplc,
         "demographic/marstat" => demographic.marstat,
         "demographic/q14/educatn" => demographic.q14.educatn,
         "demographic/placedth" => demographic.placedth,
         "demographic/pnc" => demographic.pnc,
         "demographic/autopsy3" => demographic.autopsy3,
         "demographic/height" => demographic.height,
         "demographic/wtpreprg" => demographic.wtpreprg,
         "demographic/bmi" => demographic.bmi,
         "demographic/prevlb" => demographic.prevlb,
         "demographic/prvothpg" => demographic.prvothpg,
         "demographic/pymtsrc" => demographic.pymtsrc,
         "demographic/wic" => demographic.wic,
         "outcome/outindx" => outcome.outindx,
         "outcome/multgest" => outcome.multgest,
         "outcome/q25/termproc" => outcome.q25.termproc,
         "outcome/q25/termpro1" => outcome.q25.termpro1,
         "outcome/q25/termpro2" => outcome.q25.termpro2,
         "outcome/gestwk" => outcome.gestwk,
         "outcome/dterm_grp/dterm_mo" => outcome.dterm_grp.dterm_mo,
         "outcome/dterm_grp/dterm_dy" => outcome.dterm_grp.dterm_dy,
         "outcome/dterm_grp/dterm_yr" => outcome.dterm_grp.dterm_yr,
         "outcome/dterm_grp/daydif" => outcome.dterm_grp.daydif,
         "cause_of_death/q28/cdccod" => cause_of_death.q28.cdccod,
         "cause_of_death/q28/cod" => cause_of_death.q28.cod,
         "cause_of_death/q29/assoc1" => cause_of_death.q29.assoc1,
         "cause_of_death/q29/acon1" => cause_of_death.q29.acon1,
         "cause_of_death/q30/assoc2" => cause_of_death.q30.assoc2,
         "cause_of_death/q30/acon2" => cause_of_death.q30.acon2,
         "cause_of_death/q31/assoc3" => cause_of_death.q31.assoc3,
         "cause_of_death/q31/acon3" => cause_of_death.q31.acon3,
         "cause_of_death/injury" => cause_of_death.injury,
         "cause_of_death/q33/drug_1" => cause_of_death.q33.drug_1,
         "cause_of_death/q33/drug_2" => cause_of_death.q33.drug_2,
         "cause_of_death/q33/drug_3" => cause_of_death.q33.drug_3,
         "cause_of_death/q33/drug_iv" => cause_of_death.q33.drug_iv,
         "cause_of_death/class" => cause_of_death.@class,
         "cause_of_death/clsmo" => cause_of_death.clsmo,
         "cause_of_death/clsyr" => cause_of_death.clsyr,
         "committee_review/rev_assessment_grp/dc_info_complete" => committee_review.rev_assessment_grp.dc_info_complete,
         "committee_review/rev_assessment_grp/mmria_used" => committee_review.rev_assessment_grp.mmria_used,
         "vro_case_determination/cdc_case_matching_results/preg_cb_match_mirror" => vro_case_determination.cdc_case_matching_results.preg_cb_match_mirror,
         "vro_case_determination/cdc_case_matching_results/literal_cod_match_mirror" => vro_case_determination.cdc_case_matching_results.literal_cod_match_mirror,
         "vro_case_determination/cdc_case_matching_results/icd10_match_mirror" => vro_case_determination.cdc_case_matching_results.icd10_match_mirror,
         "vro_case_determination/cdc_case_matching_results/bc_match" => vro_case_determination.cdc_case_matching_results.bc_match,
         "vro_case_determination/cdc_case_matching_results/fdc_match" => vro_case_determination.cdc_case_matching_results.fdc_match,
         "ije_dc/file_info/void_dc" => ije_dc.file_info.void_dc,
         "ije_dc/file_info/replace_dc" => ije_dc.file_info.replace_dc,
         "ije_dc/death_info/dplace_dc" => ije_dc.death_info.dplace_dc,
         "ije_dc/demog_details/agetype_dc" => ije_dc.demog_details.agetype_dc,
         "ije_dc/demog_details/deduc_dc" => ije_dc.demog_details.deduc_dc,
         "ije_bc/file_info/void_bc" => ije_bc.file_info.void_bc,
         "ije_bc/file_info/replace_bc" => ije_bc.file_info.replace_bc,
         "ije_bc/delivery_info/bplace_bc" => ije_bc.delivery_info.bplace_bc,
         "ije_bc/delivery_info/attend_bc" => ije_bc.delivery_info.attend_bc,
         "ije_bc/delivery_info/pay_bc" => ije_bc.delivery_info.pay_bc,
         "ije_bc/delivery_info/pres_bc" => ije_bc.delivery_info.pres_bc,
         "ije_bc/delivery_info/rout_bc" => ije_bc.delivery_info.rout_bc,
         "ije_bc/demog_details/meduc_bc" => ije_bc.demog_details.meduc_bc,
         "ije_fetaldc/file_info/void_fdc" => ije_fetaldc.file_info.void_fdc,
         "ije_fetaldc/file_info/replace_fdc" => ije_fetaldc.file_info.replace_fdc,
         "ije_fetaldc/delivery_info/attend_fdc" => ije_fetaldc.delivery_info.attend_fdc,
         "ije_fetaldc/delivery_info/pres_fdc" => ije_fetaldc.delivery_info.pres_fdc,
         "ije_fetaldc/delivery_info/rout_fdc" => ije_fetaldc.delivery_info.rout_fdc,
         "ije_fetaldc/demog_details/meduc_fdc" => ije_fetaldc.demog_details.meduc_fdc,

                _ => null
            };        
        }
        catch(Exception)
        {
            
        }


        
        return result;
    }

    public bool? GetS_Boolean(string path)
    {
        bool? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }

    public List<double>? GetS_List_Of_Double(string path)
    {
        List<double>? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "preparer_remarks/preparer_grp/review_1_by" => preparer_remarks.preparer_grp.review_1_by,
         "committee_review/reviewer_grp/review_2_by" => committee_review.reviewer_grp.review_2_by,

                _ => null
            };        
        }
        catch(Exception)
        {
            
        }


        
        return result;
    }

    
    public List<string>? GetS_List_Of_String(string path)
    {
        List<string>? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };        
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }

    public DateTime? GetS_Datetime(string path)
    {
        DateTime? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "date_created" => date_created,
         "date_last_updated" => date_last_updated,
         "date_last_checked_out" => date_last_checked_out,

                _ => null
            };        
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }


    public DateOnly? GetS_Date_Only(string path)
    {
        DateOnly? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "tracking/date_of_death/dod" => tracking.date_of_death.dod,
         "demographic/date_of_birth/dob" => demographic.date_of_birth.dob,
         "outcome/dterm_grp/dterm" => outcome.dterm_grp.dterm,
         "outcome/dterm_grp/dterm_est" => outcome.dterm_grp.dterm_est,
         "preparer_remarks/preparer_grp/review_1_on" => preparer_remarks.preparer_grp.review_1_on,
         "committee_review/reviewer_grp/review_2_on" => committee_review.reviewer_grp.review_2_on,
         "amss_tracking/admin_grp/case_rcvd_on" => amss_tracking.admin_grp.case_rcvd_on,
         "amss_tracking/admin_grp/mr_rcvd_on" => amss_tracking.admin_grp.mr_rcvd_on,
         "amss_tracking/admin_grp/autopsy_rcvd_on" => amss_tracking.admin_grp.autopsy_rcvd_on,
         "amss_tracking/admin_grp/file_closed_on" => amss_tracking.admin_grp.file_closed_on,

                _ => null
            };
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }


    public TimeOnly? GetS_Time_Only(string path)
    {
        TimeOnly? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "tracking/date_of_death/time_of_death" => tracking.date_of_death.time_of_death,
         "outcome/dterm_grp/dterm_tm" => outcome.dterm_grp.dterm_tm,

                _ => null
            };
                    
        }
        catch(Exception)
        {
            
        }

        return result;
    }


}