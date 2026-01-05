using System;
using System.Collections.Generic;

namespace mmria.common.data.api;

public sealed class Set_Queue_Request
{
    public string security_token { get; set; }
    public string action { get; set; }
    public System.Dynamic.ExpandoObject[] case_list { get; set;}
}

public sealed class Set_Queue_Response
{
    public bool Ok { get; set;}
    public string message { get; set;}
    public string Queue_Id { get; set;}
}


public sealed class Check_Queue_Request
{
    public string security_token { get; set; }
    public string action { get; set; }
    public string[] Queue_Id { get; set;}
}


public sealed class Check_Queue_Response
{
    public bool Ok { get; set;}
    public string message { get; set;}
    public Queue_Status_Item[] queue_id { get; set;}
}

public sealed class Queue_Status_Item
{
    public string queue_id { get; set;}
    public string processing_status { get; set;}
    public Result_Item[] result_list { get; set;}
}



public sealed class Result_Item
{
    public string summary { get; set;}
    public string case_id { get; set;}
    public string path { get; set;}
    public string detail { get; set;}
}


public sealed class Queue_Item
{
    public string queue_id { get; set;}
    public string processing_status { get; set;}
    public string action { get; set; }
    public System.Dynamic.ExpandoObject[] case_list { get; set;}
    public Result_Item[] result_list { get; set;}
}

//test
//test2

