using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor.quartz;

public sealed class Process_Migrate_Charactor_to_Numeric : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Process_Migrate_Charactor_to_Numeric started");
    //protected override void PostStop() => Console.WriteLine("Process_Migrate_Charactor_to_Numeric stopped");
	    mmria.common.couchdb.DBConfigurationDetail db_config = null;

    public Process_Migrate_Charactor_to_Numeric
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        db_config = _db_config;
    }
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case Process_Initial_Migrations_Message process_initial_migrations_message:

                process_charactor_to_numeric_migration();

                var Process_Initial_Migrations_Message = new mmria.server.model.actor.quartz.Process_Initial_Migrations_Message
                (
                    DateTime.Now
                );

                Context.ActorOf(Props.Create<mmria.server.model.actor.quartz.Process_Migrate_Data>(db_config)).Tell(Process_Initial_Migrations_Message);
                //Context.ActorSelection("akka://mmria-actor-system/user/Process_Migrate_Data").Tell(Process_Initial_Migrations_Message);

                break;
        }

    }

    Dictionary<string,mmria.common.metadata.value_node[]> lookup = null;

    private void process_charactor_to_numeric_migration()
    {

        DateTime begin_time = System.DateTime.Now;
        try
        {
            //string migration_plan_id = message.ToString();

            Console.WriteLine($"Process_Migrate_Charactor_to_Numeric Begin {begin_time}");


            string metadata_url = db_config.url + "/metadata/2016-06-12T13:49:24.759Z";
            cURL metadata_curl = new cURL("GET", null, metadata_url, null, db_config.user_name, db_config.user_value);
            mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());

            this.lookup = get_look_up(metadata);

            string url = db_config.url + $"/{db_config.prefix}mmrds/_all_docs?include_docs=true";
            var case_curl = new cURL("GET", null, url, null, db_config.user_name, db_config.user_value);
            string responseFromServer = case_curl.execute();
            
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

            foreach(var case_item in case_response.rows)
            {
                var case_has_changed = false;

                IDictionary<string, object> doc = case_item.doc as IDictionary<string, object>;

                if(doc != null)
                {
                    foreach(var child in metadata.children)
                    {
                        //var object_dictionary = case_item[child.name];
                        if(doc.ContainsKey(child.name))
                        {
                            process_node(ref case_has_changed, child, doc[child.name]);
                        }
                    }

                    if(case_has_changed)
                    {
                        set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item.doc);
                        set_value("last_updated_by", "migration_plan", case_item.doc);

                        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_item.doc, settings);

                        string put_url = db_config.url + $"/{db_config.prefix}mmrds/"  + case_item.id;
                        cURL document_curl = new cURL ("PUT", null, put_url, object_string, db_config.user_name, db_config.user_value);

                        try
                        {
                            responseFromServer = document_curl.execute();
                            var	result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                        }
                        catch(Exception ex)
                        {
                            //Console.Write("auth_session_token: {0}", auth_session_token);
                            Console.WriteLine(ex);
                        }
                    }
                }
            }

            Console.WriteLine($"Process_Migrate_Charactor_to_Numeric End {System.DateTime.Now}");
        
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Process_Migrate_Charactor_to_Numeric exception {System.DateTime.Now}\n{ex}");
        }

        TimeSpan time_span = System.DateTime.Now - begin_time;
        Console.WriteLine($"Process_Migrate_Charactor_to_Numeric duration total seconds: {time_span.TotalSeconds} total_minutes{time_span.TotalMinutes}\n");
        
    }

    private void process_list(ref bool is_changed, Dictionary<string,mmria.common.metadata.value_node[]> p_lookup,  mmria.common.metadata.node p_metadata, object p_data)
    {
        var data_migration_list = p_metadata.values;

        if(!string.IsNullOrWhiteSpace(p_metadata.path_reference))
        {
            data_migration_list = p_lookup[p_metadata.path_reference];

            if(data_migration_list == null)	
            {
                data_migration_list = p_metadata.values;
            }
        }

        if(p_data is IList<object>)
        {
            var data_list = p_data as IList<object>;
            for(var item_index = 0; item_index < data_list.Count; item_index++)
            {
                var array_item = data_list[item_index] as string;

                var is_found = false;
                for(var i = 0; i < data_migration_list.Length; i++)
                {
                    var item = data_migration_list[i];

                    if(item.value == array_item)
                    {
                        is_found = true;
                        break;
                    }
                }

                if(!is_found)
                {
                    for(var i = 0; i < data_migration_list.Length; i++)
                    {
                        var item = data_migration_list[i];
            
                        if(item.value == "9999" && array_item == null || array_item == "")
                        {
                            data_list[item_index] = item.value;
                            is_changed = true;
                            break;
                        }
                        if(item.display != null && item.display == array_item)
                        {
                            data_list[item_index] = item.value;
                            is_changed = true;
                            break;
                        }
                    }   
                }
            }
        }
        else
        {

            var is_found = false;
            for(var i = 0; i < data_migration_list.Length; i++)
            {
                var item = data_migration_list[i];

                if(item.value == p_data as string)
                {
                    is_found = true;
                    break;
                }
            }

            if(!is_found)
            {

                if(p_metadata.name.IndexOf("pmss") > -1)
                {
                    for(var i = 0; i < data_migration_list.Length; i++)
                    {
                        var item = data_migration_list[i];

                        if(item.value == "9999" && string.IsNullOrWhiteSpace(p_data as string))
                        {
                            p_data = item.value;
                            is_changed = true;
                            break;
                        }
                        else if(item.display != null && item.display == p_data as string)
                        {
                            p_data = item.value;
                            is_changed = true;
                            break;
                        }
                    }   

                }
                else if(p_metadata.data_type == "string")
                {
                    for(var i = 0; i < data_migration_list.Length; i++)
                    {
                        var item = data_migration_list[i];

                        string[] name_value =  null;
                        
                        if(p_data != null)
                            {
                            name_value = p_data.ToString().Split("-");
                            }
                            else
                            {
                            name_value = new string[]{};
                            }

                        if(name_value.Length > 1)
                        {
                            var value = name_value[0].Trim();
                            var display = name_value[1].Trim();                    
                
                            if(item.value == "9999" && string.IsNullOrWhiteSpace(p_data as string))
                            {
                                p_data = item.value;
                                is_changed = true;
                                break;
                            }
                            else if(display != null && display == item.display)
                            {
                                p_data = item.value;
                                is_changed = true;
                                break;
                            }
                        }
                        else if(item.value == "9999" && string.IsNullOrWhiteSpace(p_data as string))
                        {
                            p_data = item.value;
                            is_changed = true;
                            break;
                        }
                    }   

                }
                else
                {
                    for(var i = 0; i < data_migration_list.Length; i++)
                    {
                        var item = data_migration_list[i];
            
                        if(item.value == "9999" && string.IsNullOrWhiteSpace(p_data as string))
                        {
                            p_data = item.value;
                            is_changed = true;
                            break;
                        }
                        else if(item.display != null && item.display == p_data as string)
                        {
                            p_data = item.value;
                            is_changed = true;
                            break;
                        }
                    }   
                }
                
            }
            
        }

    }

    private void process_node(ref bool is_changed, mmria.common.metadata.node p_metadata, object p_value)
    {
        IDictionary<string,object> object_dictionary = null;
        switch(p_metadata.type.ToLowerInvariant())
        {
            case "group":
                object_dictionary = p_value as IDictionary<string,object>;
                
                if(object_dictionary != null)
                foreach(var child in p_metadata.children)
                {
                    if(object_dictionary.ContainsKey(child.name))
                    {
                        try
                        {
                            process_node(ref is_changed, child, object_dictionary[child.name]);
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("unable to process" + child.name + " : " + child.type);
                        }
                    }

                }
            break;				
            case "grid":
                var object_array = p_value as IList<object>;

                if(object_array != null)
                foreach(var object_item in object_array)
                {
                    object_dictionary = object_item as IDictionary<string,object>;
                    if(object_dictionary != null)
                    foreach(var child in p_metadata.children)
                    {
                        if(object_dictionary.ContainsKey(child.name))
                        {
                            try
                            {
                                process_node(ref is_changed, child, object_dictionary[child.name]);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("unable to process" + child.name + " : " + child.type);
                            }
                        }

                    }
                }
            break;
            case "form":
                if
                (
                    p_metadata.cardinality == "+" ||
                    p_metadata.cardinality == "*"
                )
                {
                    var form_array = p_value as IList<object>;
                    if(form_array != null)
                    foreach(var form in form_array)
                    {
                        object_dictionary = form as IDictionary<string,object>;
                        
                        if(object_dictionary != null)
                        foreach(var child in p_metadata.children)
                        {
                            if(object_dictionary.ContainsKey(child.name))
                            {
                                try
                                {
                                    process_node(ref is_changed, child, object_dictionary[child.name]);
                                }
                                catch(Exception)
                                {
                                    Console.WriteLine("unable to process" + child.name + " : " + child.type);
                                }
                            }
                            

                        }		
                    }
                }
                else
                {
                    object_dictionary = p_value as IDictionary<string,object>;

                    if(object_dictionary != null)
                    foreach(var child in p_metadata.children)
                    {
                        if(object_dictionary.ContainsKey(child.name))
                        {
                            try
                            {
                                process_node(ref is_changed, child, object_dictionary[child.name]);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("unable to process" + child.name + " : " + child.type);
                            }
                        }
                    }	
                }

            break;
            case "list":
                try
                {
                    process_list(ref is_changed, this.lookup,  p_metadata, p_value);
                }
                catch(Exception)
                {
                    Console.WriteLine("unable to process" + p_metadata.name + " : " + p_metadata.type);
                }
            break;
            default:
                break;
        }
    }


    private Dictionary<string,mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach(var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }		
    private bool set_value(string p_metadata_path, string p_value, object p_case, int p_index = -1)
    {
        bool result = false;

        var metadata_path_array = p_metadata_path.Split("/");
        var item_key = metadata_path_array[0];

        if(metadata_path_array.Length == 1)
        {
            if(p_index < 0)
            {
                switch(p_case)
                {
                    case IDictionary<string,object> val:
                        if(val.ContainsKey(item_key))
                        {
                            val[item_key] = p_value;
                        }
                        else
                        {
                            val.Add(item_key, p_value);
                        }
                        result = true;
                        break;

                }
            }
            else
            {
                switch(p_case)
                {
                    case IList<object> val:
                        switch(val[p_index])
                        {
                            case  IDictionary<string,object> list_val:
                            if(list_val.ContainsKey(item_key))
                            {
                                list_val[item_key] = p_value;
                            }
                            else
                            {
                                list_val.Add(item_key, p_value);
                            }
                            result = true;
                            break;
                        }
                        break;

                }
            }
        }
        else
        {
            var  builder = new System.Text.StringBuilder();
            for(int i = 1; i < metadata_path_array.Length; i++)
            {
                builder.Append(metadata_path_array[i]);
                builder.Append("/");
            }
            builder.Length = builder.Length - 1;

            var metadata_path = builder.ToString();
            //object new_item = null;

            switch(p_case)
            {
                case IDictionary<string,object> val:
                    if(val.ContainsKey(item_key))
                    {
                        result = set_value(metadata_path, p_value, val[item_key]);
                    }
                    else
                    {
                        // error
                    }

                    break;
                case IList<object> val:
                    foreach(var item in val)
                    {
                        switch(item)
                        {
                            case IDictionary<string,object> item_val:
                                if(item_val.ContainsKey(item_key))
                                {
                                    result &= set_value(metadata_path, p_value, item_val[item_key]);
                                }
                                else
                                {
                                    // error
                                }

                                break;
                        }
                    }

                    break;

            }
            //result = set_value(metadata_path, p_value, new_item);
        }
        



        return result;
    }
    


}


