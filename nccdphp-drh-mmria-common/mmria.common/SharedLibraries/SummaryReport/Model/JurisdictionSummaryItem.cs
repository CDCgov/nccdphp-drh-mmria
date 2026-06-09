using System.Collections.Generic;

namespace mmria.common.SharedLibraries.SummaryReport.Model;

public sealed class JurisdictionSummaryItem
{
    public string host_name { get; set; }
    public string folder_name { get; set; } = "/";
    public string rpt_date { get; set; }
    public int num_recs { get; set; }
    public int num_users_unq { get; set; }
    public int num_users_ja { get; set; }
    public int num_users_abs { get; set; }
    public int num_user_anl { get; set; }
    public int num_user_cm { get; set; }
}

public sealed class SessionSummaryItem
{
    public SessionSummaryItem()
    {
        rpt_date = new List<int>();
    }

    public string host_name { get; set; }
    public List<int> rpt_date { get; set; }
    public int total { get; set; }
}

public sealed class SessionSummaryDocument
{
    public string _id { get; set; }
    public string _rev { get; set; }
    public string data_type { get; set; }
    public System.DateTime date_created { get; set; }
    public System.DateTime date_last_updated { get; set; }
    public System.DateTime date_expired { get; set; }
    public string user_id { get; set; }
    public string ip { get; set; }
    public int? action_result { get; set; }
    public string session_event_id { get; set; }
    public string[] role_list { get; set; }
    public Dictionary<string, object> data { get; set; }
}
