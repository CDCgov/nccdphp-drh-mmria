using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using mmria.common.util;

namespace mmria.common.couchdb;

public sealed class DBConfigurationDetail
{
    public DBConfigurationDetail() { }
    public string prefix { get; set;}
    public string url { get; set; }

    public string user_name { get; set; }

    public string user_value { get; set; }

    public string Get_Prefix_DB_Url(string p_database_name)
    {
        return $"{url}/{prefix}{p_database_name}";
    }

}

public sealed class SAMSConfigurationDetail
{
    public string client_id { get; set;}
    public string client_secret { get; set; }

    public string callback_url { get; set; }

    public string activity_name { get; set; }

}


public sealed class CVSConfigurationDetail
{
    public string cvs_api_id  { get; set;}
    public string cvs_api_key  { get; set; }

    public string cvs_api_url  { get; set; }
}

public sealed class SteveAPIConfigurationDetail
{
    public SteveAPIConfigurationDetail() {}
    public string sea_bucket_kms_key  { get; set; }
    public string client_name  { get; set; }
    public string client_secret_key  { get; set; }
    public string base_url { get; set; }
   
}

public sealed class ConfigurationSet
{
    public ConfigurationSet()
    {
        detail_list = new Dictionary<string, DBConfigurationDetail>(StringComparer.OrdinalIgnoreCase);
        name_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
    public string _id { get; set;}
    public string _rev { get; set; }
    public string service_key {get; set; }

    public string data_type { get; } = "configuration-set";

    public Dictionary<string, string> name_value { get;set; }

    public Dictionary<string, DBConfigurationDetail> detail_list { get;set; }

    public DateTime date_created { get; set; } 
    public string created_by { get; set; } 
    public DateTime date_last_updated { get; set; } 
    public string last_updated_by { get; set; } 

}

public sealed class Configuration
{
    public Configuration()
    {
        detail_list = new Dictionary<string, DBConfigurationDetail>(StringComparer.OrdinalIgnoreCase);
        name_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static string g_geocode_api_key;
    public static string geocode_api_url;
    public static string couchdb_url = "http://localhost:5984";

    public static string db_prefix = "";
    public static string web_site_url;

    public static string timer_user_name;
    public static string timer_value;
    public static string cron_schedule;
    public static string export_directory;

    public static bool is_schedule_enabled = true;
    public static int session_idle_timeout_minutes;

    public static bool is_db_check_enabled = false;

    public static string vitals_url;

    public Dictionary<string, string> name_value { get;set; }

    public Dictionary<string, DBConfigurationDetail> detail_list { get;set; }
}

public sealed class OverridableConfiguration
{
    public OverridableConfiguration()
    {
        boolean_keys = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        string_keys = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        integer_keys = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
    }
    public string _id { get; set;}
    public string _rev { get; set; }

    public DateTime? date_created { get; set; } = DateTime.UtcNow;
    public string created_by { get; set; } = "system";
    public DateTime? date_last_updated { get; set; }  = DateTime.UtcNow;
    public string last_updated_by { get; set; } = "system";

    public string data_type { get; } = "configuration-master";

    public Dictionary<string, Dictionary<string, bool>> boolean_keys { get;set; }
    public Dictionary<string, Dictionary<string, string>> string_keys { get;set; }
    public Dictionary<string, Dictionary<string, int>> integer_keys { get;set; }
    public bool? GetBoolean(string key, string prefix)
    {
        if(prefix.Equals("shared", StringComparison.OrdinalIgnoreCase)) return GetSharedBoolean(key);

        if(boolean_keys.ContainsKey(prefix))
        {
            if(boolean_keys[prefix].ContainsKey(key))
            {
                return boolean_keys[prefix][key];
            }
        }
        
        return GetSharedBoolean(key);
    }

    public void SetBoolean(string prefix, string key, bool value)
    {
        if(!boolean_keys.ContainsKey(prefix))
        {
            boolean_keys.Add(prefix, new(StringComparer.OrdinalIgnoreCase));
        }

        if(!boolean_keys[prefix].ContainsKey(key))
        {
            boolean_keys[prefix].Add(key, value);
        }
        else
        {
            boolean_keys[prefix][key] = value;
        }
    }



    public string GetString(string key, string prefix)
    {
        if(prefix.Equals("shared", StringComparison.OrdinalIgnoreCase)) return GetSharedString(key);

        if(string_keys.ContainsKey(prefix))
        {
            if(string_keys[prefix].ContainsKey(key))
            {
                return string_keys[prefix][key];
            }
        }
        
        return GetSharedString(key);
    }
    public void SetString(string prefix, string key, string value)
    {
        if(!string_keys.ContainsKey(prefix))
        {
            string_keys.Add(prefix, new(StringComparer.OrdinalIgnoreCase));
        }

        if(!string_keys[prefix].ContainsKey(key))
        {
            string_keys[prefix].Add(key, value);
        }
        else
        {
            string_keys[prefix][key] = value;
        }
        
    }
    
    public int? GetInteger(string key, string prefix)
    {
        if(prefix.Equals("shared", StringComparison.OrdinalIgnoreCase)) return GetSharedInteger(key);

        if(integer_keys.ContainsKey(prefix))
        {
            if(integer_keys[prefix].ContainsKey(key))
            {
                return integer_keys[prefix][key];
            }
        }
    
        return GetSharedInteger(key);

    }

    public void SetInteger(string prefix, string key, int value)
    {
        if(!integer_keys.ContainsKey(prefix))
        {
            integer_keys.Add(prefix, new(StringComparer.OrdinalIgnoreCase));
        }

        if(!integer_keys[prefix].ContainsKey(key))
        {
            integer_keys[prefix].Add(key, value);
        }
        else
        {
            integer_keys[prefix][key] = value;
        }
        
        
    }


    public bool? GetSharedBoolean(string key)
    {
        if(boolean_keys["shared"].ContainsKey(key))
        {
            return boolean_keys["shared"][key];
        }

        return null;
    }

    public string GetSharedString(string key)
    {
        if(string_keys["shared"].ContainsKey(key))
        {
            return string_keys["shared"][key];
        }

        return null;
    }
    
    public int? GetSharedInteger(string key)
    {
        if(integer_keys["shared"].ContainsKey(key))
        {
            return integer_keys["shared"][key];
        }
    
        return null;
    }


    public bool? GetOverridedBoolean(string key, string prefix)
    {
        if
        (
            boolean_keys.ContainsKey(prefix) &&
            boolean_keys[prefix].ContainsKey(key)
        )
        {
            return boolean_keys[prefix][key];
        }

        return null;
    }

    public string GetOverridedString(string key, string prefix)
    {
        if
        (
            string_keys.ContainsKey(prefix) && 
            string_keys[prefix].ContainsKey(key)
        )
        {
            return string_keys[prefix][key];
        }

        return null;
    }
    
    public int? GetOverridedInteger(string key, string prefix)
    {
        if
        (
            integer_keys.ContainsKey(prefix) &&
            integer_keys[prefix].ContainsKey(key)
        )
        {
            return integer_keys[prefix][key];
        }
    
        return null;
    }

    public DBConfigurationDetail GetDBConfig(string prefix)
    {
        DBConfigurationDetail result = null;

        Console.WriteLine(string_keys.ToString());

        if (string_keys.ContainsKey(prefix))
        {
            result = new();

            result.url = string_keys[prefix]["couchdb_url"];
            result.prefix = string_keys[prefix]["db_prefix"];
            result.user_name = string_keys[prefix]["timer_user_name"];
            result.user_value = string_keys[prefix]["timer_value"];
        }
        return result;
    
    }

    public SAMSConfigurationDetail GetSAMSConfigurationDetail(string prefix)
    {
        SAMSConfigurationDetail result = new();

        result.client_id = string_keys[prefix]["sams:client_id"];
        result.client_secret = string_keys[prefix]["sams:client_secret"];
        result.callback_url = string_keys[prefix]["sams:callback_url"];
        result.activity_name = string_keys[prefix]["sams:activity_name"];

        return result;
    }

    public CVSConfigurationDetail  GetCVSConfigurationDetail()
    {
        CVSConfigurationDetail result = new();

        result.cvs_api_id = string_keys["shared"]["cvs_api_id"];
        result.cvs_api_key = string_keys["shared"]["cvs_api_key"];
        result.cvs_api_url = string_keys["shared"]["cvs_api_url"];
        

        return result;
    }

    public SteveAPIConfigurationDetail  GetSteveAPIConfigurationDetail()
    {
        SteveAPIConfigurationDetail result = new();

        result.sea_bucket_kms_key = string_keys["shared"]["steve_api:sea_bucket_kms_key"];
        result.client_name = string_keys["shared"]["steve_api:client_name"];
        result.client_secret_key = string_keys["shared"]["steve_api:client_secret_key"];
        result.base_url = string_keys["shared"]["steve_api:base_url"];
        

        return result;
    }

    public  List<Html_Link> GetExternalHomePageLinks()
    {
        var result = new List<Html_Link>();
        const int Max_List_Size = 10;

        for(var i = 0; i < Max_List_Size; i++)
        {
            var display_key = $"link:display:{i}";
            var value_key = $"link:value:{i}";
            
            if
            (
                string_keys["shared"].ContainsKey(display_key) &&
                string_keys["shared"].ContainsKey(value_key) 
            )
            {
                var item = new Html_Link()
                {
                    Text = string_keys["shared"][display_key],
                    Url = string_keys["shared"][value_key]
                };

                result.Add(item);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    public class Html_Link
    {
        public Html_Link() {}

        public string Text { get; set; }
        public string Url { get; set; }
    }

}
