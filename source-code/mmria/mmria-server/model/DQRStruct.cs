using System;
using System.Collections.Generic;

namespace mmria.server.model.dqr;

public sealed class DQRSL3
{
    public DQRSL3()
    {

    }
    public int? n { get; set; }

    public int? p { get; set; }
}

public sealed class DQRSL2
{
    public DQRSL2 () {}
    public DQRSL3 m { get; set; }

    public DQRSL3 u { get; set; }
}
public sealed class DQRCLine
{
    public DQRCLine () {}
    public DQRSL2 c { get; set; }

    public DQRSL2 p { get; set; }
}
public sealed class DQRSummary
{
    public DQRSummary()
    {
        n03 = new int[8];
    }

    public string add_quarter_name { get; set; }

    public double? add_quarter_number { get; set; }

    public string cmp_quarter_name { get; set; }

    public double? cmp_quarter_number { get; set; }
    
    
    public int? n01 { get; set; }
	public int? n02  { get; set; }
	public int[] n03 { get; set; }
	public int? n04  { get; set; }
	public int? n05  { get; set; }
	public int? n06  { get; set; }
	public int? n07  { get; set; }
	public int? n08  { get; set; }
	public int? n09 { get; set; }
	public DQRCLine n10 { get; set; }

    public DQRCLine n11 { get; set; }

    public DQRCLine n12 { get; set; }

    public DQRCLine n { get; set; }

    public DQRCLine n13 { get; set; }

    public DQRCLine n14 { get; set; }

    public DQRCLine n15 { get; set; }

    public DQRCLine n16 { get; set; }

    public DQRCLine n17 { get; set; }

    public DQRCLine n18 { get; set; }

    public DQRCLine n19 { get; set; }

    public DQRCLine n20 { get; set; }

    public DQRCLine n21 { get; set; }

    public DQRCLine n22 { get; set; }

    public DQRCLine n23 { get; set; }

    public DQRCLine n24 { get; set; }
    public DQRCLine n25 { get; set; }
    public DQRCLine n26 { get; set; }
    public DQRCLine n27 { get; set; }
    public DQRCLine n28 { get; set; }
    public DQRCLine n29 { get; set; }
    public DQRCLine n30 { get; set; }
    public DQRCLine n31 { get; set; }
    public DQRCLine n32 { get; set; }
    public DQRCLine n33 { get; set; }
    public DQRCLine n34 { get; set; }
    public DQRCLine n35 { get; set; }
    public DQRCLine n36 { get; set; }
    public DQRCLine n37 { get; set; }
    public DQRCLine n38 { get; set; }
    public DQRCLine n39 { get; set; }
    public DQRCLine n40 { get; set; }
    public DQRCLine n41 { get; set; }
    public DQRCLine n42 { get; set; }
    public DQRCLine n43 { get; set; }
    public DQRCLine n44 { get; set; }
    public DQRCLine n45 { get; set; }
    public DQRCLine n46 { get; set; }
    public DQRCLine n47 { get; set; }
    public DQRCLine n48 { get; set; }
    public DQRCLine n49 { get; set; }

}

