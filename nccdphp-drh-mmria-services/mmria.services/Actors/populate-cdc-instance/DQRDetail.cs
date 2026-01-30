using System;
using System.Collections.Generic;

namespace mmria.server.model.dqr;


public sealed class DQRDetail_2
{
    public DQRDetail_2()
    {
        
    }
    public int? m { get; set; }
    public int? u { get; set; }


}

public sealed class DQRDetail_3
{
    public DQRDetail_3()
    {
        
    }
    public int? t { get; set; }
    public int? p { get; set; }


}
public sealed class DQRDetail
{
    public DQRDetail()
    {
         n03 = new int[8];
    }
    public string _id { get; set; }
    public string _rev { get; set; }

    public string case_folder { get; set; }

    public string record_id { get; set; }

    public string dt_death { get; set; }
	public string dt_com_rev { get; set; }

    public string data_type { get; set; } = "dqr-detail";

    public string add_quarter_name { get; set; }

    public double? add_quarter_number { get; set; }

    public string cmp_quarter_name { get; set; }

    public double? cmp_quarter_number { get; set; }

    public int? hrcpr_bcp_secti_is_2 {get; set; }
    public int? is_preventable_death {get; set; }
    public int? n01 { get; set; }
	public int? n02  { get; set; }
	public int[] n03 { get; set; }
	public int? n04  { get; set; }
	public int? n05  { get; set; }
	public int? n06  { get; set; }
	public int? n07  { get; set; }
	public int? n08  { get; set; }
	public int? n09 { get; set; }
	public DQRDetail_2 n10 { get; set; } = new();

    public DQRDetail_2 n11 { get; set; } = new();

    public DQRDetail_2 n12 { get; set; } = new();

    public DQRDetail_2 n13 { get; set; } = new();

    public DQRDetail_2 n14 { get; set; } = new();

    public DQRDetail_2 n15 { get; set; } = new();

    public DQRDetail_2 n16 { get; set; } = new();

    public DQRDetail_2 n17 { get; set; } = new();

    public DQRDetail_2 n18 { get; set; } = new();

    public DQRDetail_2 n19 { get; set; } = new();

    public DQRDetail_2 n20 { get; set; } = new();

    public DQRDetail_2 n21 { get; set; } = new();

    public DQRDetail_2 n22 { get; set; } = new();

    public DQRDetail_2 n23 { get; set; } = new();

    public DQRDetail_2 n24 { get; set; } = new();
    public DQRDetail_2 n25 { get; set; } = new();
    public DQRDetail_2 n26 { get; set; } = new();
    public DQRDetail_2 n27 { get; set; } = new();
    public DQRDetail_2 n28 { get; set; } = new();
    public DQRDetail_2 n29 { get; set; } = new();
    public DQRDetail_2 n30 { get; set; } = new();
    public DQRDetail_2 n31 { get; set; } = new();
    public DQRDetail_2 n32 { get; set; } = new();
    public DQRDetail_2 n33 { get; set; } = new();
    public DQRDetail_2 n34 { get; set; } = new();
    public DQRDetail_2 n35 { get; set; } = new();
    public DQRDetail_2 n36 { get; set; } = new();
    public DQRDetail_2 n37 { get; set; } = new();
    public DQRDetail_2 n38 { get; set; } = new();
    public DQRDetail_2 n39 { get; set; } = new();
    public DQRDetail_2 n40 { get; set; } = new();
    public DQRDetail_2 n41 { get; set; } = new();
    public DQRDetail_2 n42 { get; set; } = new();
    public DQRDetail_2 n43 { get; set; } = new();
    public DQRDetail_3 n44 { get; set; } = new();
    public DQRDetail_3 n45 { get; set; } = new();
    public DQRDetail_3 n46 { get; set; } = new();
    public DQRDetail_3 n47 { get; set; } = new();
    public DQRDetail_3 n48 { get; set; } = new();
    public DQRDetail_3 n49 { get; set; } = new();

}