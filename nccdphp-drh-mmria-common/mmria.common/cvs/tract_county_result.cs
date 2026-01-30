using System;

namespace mmria.common.cvs;

public sealed class tract
{
    public tract(){}

    public string GEOID { get;set;}
    public int YEAR { get;set;}
    public string state { get;set;}
    public string stfips { get;set;}
    public string NAME { get;set;}
    public double pctNOIns_Fem { get;set;}
    public double pctNoVehicle { get;set;}
    public double pctMOVE { get;set;}
    public double pctSPHH { get;set;}
    public double pctOVERCROWDHH { get;set;}
    public double pctOWNER_OCC { get;set;}
    public double pct_less_well { get;set;}
    public double NDI_raw { get;set;}
    public double pctPOV { get;set;}
    public double ICE_INCOME_all { get;set;}
    public double MEDHHINC { get;set;}

    public double MIDWIVESrate { get;set;}
    public double segregation { get;set;}
    public double PCPrate { get;set;}
    public double rtVIOLENTCR { get;set;}

    public double pctRural  { get;set;}

    public double MHPROVIDERrate  { get;set;}

    public double Racialized_pov { get; set; }

}
public sealed class county
{
    public county(){}

    public string GEOID { get;set;}
    public int YEAR { get;set;}
    public string state { get;set;}
    public string NAME { get;set;}
    public double pctNOIns_Fem { get;set;}
    public double pctNoVehicle { get;set;}
    public double pctMOVE { get;set;}
    public double pctSPHH { get;set;}
    public double pctOVERCROWDHH { get;set;}
    public double pctOWNER_OCC { get;set;}
    public double pct_less_well { get;set;}
    public double NDI_raw { get;set;}
    public double pctPOV { get;set;}
    public double ICE_INCOME_all { get;set;}
    public double MEDHHINC { get;set;}
    public double MDrate { get;set;}
    public double pctOBESE { get;set;}
    public double FI { get;set;}
    public double CNMrate { get;set;}
    public double OBGYNrate { get;set;}
    public double rtTEENBIRTH { get;set;}
    public double rtSTD { get;set;}
    public double rtMHPRACT { get;set;}
    public double rtDRUGODMORTALITY { get;set;}
    public double rtOPIOIDPRESCRIPT { get;set;}
    public double SocCap { get;set;}
    public double rtSocASSOC { get;set;}
    public double pctHOUSE_DISTRESS { get;set;}
    public double rtVIOLENTCR_ICPSR { get;set;}
    public double isolation { get;set;}

    public double MHCENTERrate { get; set; }
    
    public double MIDWIVESrate { get;set;}
    public double segregation { get;set;}
    public double PCPrate { get;set;}
    public double rtVIOLENTCR { get;set;}

    public double pctRural  { get;set;}
    public double MHPROVIDERrate  { get;set;}

    public double Racialized_pov { get; set; }
}

public sealed class tract_county_result
{
    public tract_county_result()
    {

    }

    public tract tract { get;set;}

    public county county { get;set;}
}