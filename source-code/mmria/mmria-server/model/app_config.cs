using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Web;
using Quartz;
using Quartz.Impl;
using Microsoft.Extensions.Configuration;

namespace mmria.server.model;

public sealed class app_config
{
        public string web_site_url {get; set;} = "http://*:12345";
        public string log_directory {get; set;} = "c:/temp/mmria-log";
        public string export_directory {get; set;} = "c:/temp/mmria-export";
        public string couchdb_url {get; set;} = "http://localhost:5984";
        public string timer_user_name {get; set;} = null;
        public string timer_value {get; set;} = null;
        public string cron_schedule {get; set;} = "0 */1 * * * ?";
        public int? pass_word_minimum_length {get; set;} =  8;
        public int? pass_word_days_before_expires {get; set;} = 0;
        public int? pass_word_days_before_user_is_notified_of_expiration {get; set;} = 0;
        public bool? EMAIL_USE_AUTHENTICATION {get; set;} = false;
        public bool? EMAIL_USE_SSL {get; set;} = false;

        public bool? is_development {get; set;} = false;

        public bool? sams_is_enabled {get; set;} = false;
        public string SMTP_HOST {get; set;} = null;
        public int? SMTP_PORT {get; set;} = 25;
        public string EMAIL_FROM {get; set;} = null;
        public string EMAIL_PASSWORD {get; set;} = null;

        public bool? is_environment_based {get; set;} = false;

        public int? default_days_in_effective_date_interval {get; set;} = 90;
        public int? unsuccessful_login_attempts_number_before_lockout {get; set;} = 5;
        public int? unsuccessful_login_attempts_within_number_of_minutes {get; set;} = 120;
        public int? unsuccessful_login_attempts_lockout_number_of_minutes {get; set;} = 15;

        public string shared_config_id { get; set; }

        public string host_site {get; set; }
        
}
