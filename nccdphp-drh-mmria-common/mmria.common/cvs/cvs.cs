using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mmria.common.cvs;

public sealed class post_payload
{
    public post_payload(){}


    public string action { get;set;}
    public string c_geoid { get;set;}
    public string t_geoid { get;set;}
    public string year { get;set;}

    public string lat { get;set;}
    public string lon { get;set;}

    public string id { get;set;}

}


public sealed class all_data_payload
{
    public all_data_payload(){}

    public string c_geoid { get;set;}
    public string t_geoid { get;set;}
    public string year { get;set;}
}


public sealed class get_dashboard_payload
{
    public get_dashboard_payload(){}

    public string year { get;set;}
    public string lat { get;set;}
    public string lon { get;set;}

    public string id { get;set;}
}


public sealed class get_year_payload
{
    public get_year_payload(){}

    public string table { get;set;} = "acs-county";
}

public sealed class server_status_post_body
{
    public server_status_post_body(){}

    public string id {get;set;}
    public string secret {get;set;}
    public string operation {get;set;} = "server-status";
    public System.Collections.Generic.Dictionary<string,string> payload {get;set;}
}

public sealed class get_all_data_post_body
{
    public get_all_data_post_body(){}

    public string id {get;set;}
    public string secret {get;set;}
    public string operation {get;set;} = "get-all-data";

    public all_data_payload payload {get;set;}
}

public sealed class get_dashboard_post_body
{
    public get_dashboard_post_body(){}

    public string id {get;set;}
    public string secret {get;set;}
    public string operation {get;set;}  = "get-dashboard";

    public get_dashboard_payload payload {get;set;}
}


public sealed class get_year_post_body
{
    public get_year_post_body(){}

    public string id {get;set;}
    public string secret {get;set;}
    public string operation {get;set;}  = "get-years";

    public get_year_payload payload {get;set;}
}