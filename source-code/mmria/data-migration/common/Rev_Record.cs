using System;
using System.Collections.Generic;

namespace migrate;

public sealed class Rev_Record
{
    public Rev_Record(){}

    public string _id { get;set; }
    public string _rev { get;set; }

    public DateTime? date_created { get;set; }
    public string created_by { get;set; }
    public DateTime? date_last_updated { get;set; }
    public string last_updated_by { get;set; }
    public string avilability_status { get;set; }
}

public sealed class Rev_Record_Value
{
    public Rev_Record_Value(){}

    public string _id { get;set; }
    public string _rev { get;set; }

    public DateTime? date_created { get;set; }
    public string created_by { get;set; }
    public DateTime? date_last_updated { get;set; }
    public string last_updated_by { get;set; }
    public string avilability_status { get;set; }

    public string mmria_path { get; set;}

    public object value { get; set; }
}
