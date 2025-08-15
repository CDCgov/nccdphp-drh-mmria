using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Akka.Actor;

namespace migrate;

public sealed class SaveRecord
{
    string host_db_url;
    string db_name;

    string user_name;
    string user_value;

    System.Text.StringBuilder output_builder;
    public SaveRecord
    (
        string p_host_db_url,
        string p_db_name,
        string p_user_name,
        string p_user_value, 
        System.Text.StringBuilder p_output_builder
    )
    {
        host_db_url = p_host_db_url;
        db_name = p_db_name;

        user_name = p_user_name;
        user_value = p_user_value;

        output_builder = p_output_builder;
    }

    public async Task<bool> save_case
    (
        IDictionary<string, object> item, 
        string p_migration_name, 
        bool force_write = false
    )
    {
        bool result = false;
        var gsv = new C_Get_Set_Value(this.output_builder);

        if(!p_migration_name.Equals("SubstanceMigration", StringComparison.OrdinalIgnoreCase))
        {
            gsv.set_value("version", p_migration_name, item);
        }

        var item_dictionary = item as IDictionary<string,object>;
        List<Migration_History_Item> migration_history_list = new List<Migration_History_Item>();
        
        try
        {

            if(item_dictionary.ContainsKey("data_migration_history"))
            {
                var list_object = item_dictionary["data_migration_history"] as List<object>;
                foreach(var o in list_object)
                {
                    var expando = o as System.Dynamic.ExpandoObject;
                    if(expando == null) continue;
                    var expando_dictionary = o as IDictionary<string,object>;
                    if(expando_dictionary == null) continue;

                    migration_history_list.Add
                    (
                        new Migration_History_Item()
                        {
                            version = expando_dictionary["version"]?.ToString(),
                            datetime = expando_dictionary["datetime"]?.ToString(),
                            is_forced_write = expando_dictionary["is_forced_write"]?.ToString() ?? "false"
                        }
                    );


                }
            }

            bool is_existing_migration = false;

            foreach(var migration_item in migration_history_list)
            {
                if(migration_item.version.Equals(p_migration_name, StringComparison.OrdinalIgnoreCase))
                {
                    is_existing_migration = true;
                }
            }

            if(is_existing_migration && !force_write)
            {
                return result;
            }


            migration_history_list.Add
            (
                new Migration_History_Item()
                {
                    version = p_migration_name,
                    datetime = DateTime.UtcNow.ToString("o"),
                    is_forced_write = force_write.ToString()
                }
            );

            item_dictionary["data_migration_history"] = migration_history_list;
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(item, settings);

            string put_url = $"{host_db_url}/{db_name}/{item["_id"]}";
            cURL document_curl = new cURL ("PUT", null, put_url, object_string, user_name, user_value);


            var responseFromServer = await document_curl.executeAsync();
            
            var	put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

            if(put_result.ok)
            {
                result = true;
            }
            else

            {
                var output_text = $"item record_id: {item["_id"]} error saving {p_migration_name}";
                this.output_builder.AppendLine(output_text);
                Console.WriteLine(output_text);
            }
            
        }
        catch(Exception ex)
        {
            var output_text = $"item record_id: {item["_id"]} error saving {p_migration_name}";
            this.output_builder.AppendLine(output_text);
            Console.WriteLine(output_text);
            Console.WriteLine(ex);


        }

        return result;
    }


    public sealed class Migration_History_Item
    {
        public Migration_History_Item(){}
        public string version { get;set; }
        public string datetime { get;set; }

        public string is_forced_write {get;set;}
    }
    public bool save_case(System.Dynamic.ExpandoObject item, string p_migration_name, bool force_write = false)
    {
        bool result = false;

        var gsv = new C_Get_Set_Value(this.output_builder);
        
        if(!p_migration_name.Equals("SubstanceMigration", StringComparison.OrdinalIgnoreCase))
        {
            gsv.set_value("version", p_migration_name, item);
        }

        var item_dictionary = item as IDictionary<string,object>;
        List<Migration_History_Item> migration_history_list = new List<Migration_History_Item>();
        
        try
        { 

            if(item_dictionary.ContainsKey("data_migration_history"))
            {
                var list_object = item_dictionary["data_migration_history"] as List<object>;
                foreach(var o in list_object)
                {
                    var expando = o as System.Dynamic.ExpandoObject;
                    if(expando == null) continue;
                    var expando_dictionary = o as IDictionary<string,object>;
                    if(expando_dictionary == null) continue;

                    migration_history_list.Add
                    (
                        new Migration_History_Item()
                        {
                            version = expando_dictionary["version"]?.ToString(),
                            datetime = expando_dictionary["datetime"]?.ToString(),
                            is_forced_write = expando_dictionary["is_forced_write"]?.ToString() ?? "false"
                        }
                    );


                }
            }

            bool is_existing_migration = false;

            foreach(var migration_item in migration_history_list)
            {
                if(migration_item.version.Equals(p_migration_name, StringComparison.OrdinalIgnoreCase))
                {
                    is_existing_migration = true;
                }
            }

            if(is_existing_migration && !force_write)
            {
                return result;
            }


            migration_history_list.Add
            (
                new Migration_History_Item()
                {
                    version = p_migration_name,
                    datetime = DateTime.UtcNow.ToString("o"),
                    is_forced_write = force_write.ToString()
                }
            );

            item_dictionary["data_migration_history"] = migration_history_list;

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            var changed_object_string = Newtonsoft.Json.JsonConvert.SerializeObject(item, settings);

            IDictionary<string, object> doc = item as IDictionary<string, object>;

            string put_url = $"{host_db_url}/{db_name}/{doc["_id"]}";
            cURL document_curl = new cURL ("PUT", null, put_url, changed_object_string, user_name, user_value);


            string responseFromServer = document_curl.execute();
            var	put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
        }
        catch(Exception ex)
        {
            var output_text = $"item record_id: {item_dictionary["_id"]} error saving {p_migration_name}";
            this.output_builder.AppendLine(output_text);
            Console.WriteLine(output_text);
            Console.WriteLine(ex);
        }
    

        return result;
    }
}
