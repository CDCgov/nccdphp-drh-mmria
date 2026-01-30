using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.services.Models;

public sealed class ScheduleInfoMessage
{
    public ScheduleInfoMessage
    (
        string p_cron_schedule, 
        string p_couch_db_url,
        string p_db_prefix,
        string p_user_name,
        string p_user_value,
        string p_export_directory,
        string p_jurisdiction_user_name,
        string p_version_number,
        string p_cdc_instance_pull_list
        )
    {
        cron_schedule = p_cron_schedule;
        couch_db_url = p_couch_db_url;
        user_name = p_user_name;
        user_value = p_user_value;
        db_prefix = p_db_prefix;
        export_directory = p_export_directory;
        jurisdiction_user_name = p_jurisdiction_user_name;
        version_number = p_version_number;
        cdc_instance_pull_list  = p_cdc_instance_pull_list;
    }

    public string cron_schedule { get; private set; }
    public string couch_db_url { get; private set; }
    public string db_prefix { get; private set; }
    public string user_name { get; private set; }

    public string jurisdiction_user_name { get; private set; }

    public string version_number { get; private set; }

    public string user_value { get; private set; }
    public string export_directory { get; private set; }

    public string cdc_instance_pull_list { get; private set; }
}
