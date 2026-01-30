using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace mmria.common.cvs;

public sealed class CVS_Grid_Item
{
    public CVS_Grid_Item(){}

    public string cvs_api_request_url { get; set; }
    public string cvs_api_request_date_time { get; set; }
    public string cvs_api_request_c_geoid { get; set; }
    public string cvs_api_request_t_geoid { get; set; }
    public string cvs_api_request_year { get; set; }
    public string cvs_api_request_result_message { get; set; }
    public string cvs_mdrate_county { get; set; }
    public string cvs_pctnoins_fem_county { get; set; }
    public string cvs_pctnoins_fem_tract { get; set; }
    public string cvs_pctnovehicle_county { get; set; }
    public string cvs_pctnovehicle_tract { get; set; }
    public string cvs_pctmove_county { get; set; }
    public string cvs_pctmove_tract { get; set; }
    public string cvs_pctsphh_county { get; set; }
    public string cvs_pctsphh_tract { get; set; }
    public string cvs_pctovercrowdhh_county { get; set; }
    public string cvs_pctovercrowdhh_tract { get; set; }
    public string cvs_pctowner_occ_county { get; set; }
    public string cvs_pctowner_occ_tract { get; set; }
    public string cvs_pct_less_well_county { get; set; }
    public string cvs_pct_less_well_tract { get; set; }
    public string cvs_ndi_raw_county { get; set; }
    public string cvs_ndi_raw_tract { get; set; }
    public string cvs_pctpov_county { get; set; }
    public string cvs_pctpov_tract { get; set; }
    public string cvs_ice_income_all_county { get; set; }
    public string cvs_ice_income_all_tract { get; set; }
    public string cvs_medhhinc_county { get; set; }
    public string cvs_medhhinc_tract { get; set; }
    public string cvs_pctobese_county { get; set; }
    public string cvs_fi_county { get; set; }
    public string cvs_cnmrate_county { get; set; }
    public string cvs_obgynrate_county { get; set; }
    public string cvs_rtteenbirth_county { get; set; }
    public string cvs_rtstd_county { get; set; }
    public string cvs_rtmhpract_county { get; set; }
    public string cvs_rtdrugodmortality_county { get; set; }
    public string cvs_rtopioidprescript_county { get; set; }
    public string cvs_soccap_county { get; set; }
    public string cvs_rtsocassoc_county { get; set; }
    public string cvs_pcthouse_distress_county { get; set; }
    public string cvs_rtviolentcr_icpsr_county { get; set; }
    public string cvs_isolation_count { get; set; }

}