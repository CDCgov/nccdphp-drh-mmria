using System;
namespace mmria.server;

public sealed class c_config
{
    public c_config ()
    {
    }

    public string web_site_url { get; set; }
    public string export_directory { get; set; }
    public string file_root_folder { get; set; }
    public string couchdb_url { get; set; }
    public string timer_user_name { get; set; }
    public string timer_password { get; set; }
    public string cron_schedule { get; set; }
}

