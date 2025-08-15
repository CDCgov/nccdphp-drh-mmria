using System;
namespace mmria.common.texas_am;

public sealed class Census_Variable
{
    public string B27010_001E { get; set; }
    public string B15001_001E { get; set; }
    public string B17020_001E { get; set; }
    public string state { get; set; }
    public string county { get; set; }
    public string tract { get; set; }

    public Census_Variable()
    {
    }
    /*
    [["B27010_001E","B15001_001E","B17020_001E","state","county","tract"],
["6254","6143","2655","06","037","224700"]]*/
}

