using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace mmria.server.model.actor.quartz;

public sealed class Process_Initial_Migrations_Message
{
    public Process_Initial_Migrations_Message (DateTime p_time_sent)
    {
        time_sent = p_time_sent;
    }
    public DateTime time_sent { get; private set; }
}
public sealed class Process_Migrate_Data : UntypedActor
{
    //protected override void PreStart() => Console.WriteLine("Process_Migrate_Data started");
    //protected override void PostStop() => Console.WriteLine("Process_Migrate_Data stopped");

	mmria.common.couchdb.DBConfigurationDetail db_config = null;

    public Process_Migrate_Data
    (
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        db_config = _db_config;
    }

    protected override void OnReceive(object message)
    {
        try
        {
            switch (message)
            {
                case string migration_plan_id:
                    //process_migration_plan_by_id(migration_plan_id);
                    break;
                case Process_Initial_Migrations_Message process_initial_migrations_message:
                    /*
                    string current_directory = AppContext.BaseDirectory;
                    if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
                    {
                        current_directory = System.IO.Directory.GetCurrentDirectory();
                    }

                    var migration_plan_directory_files =  System.IO.Directory.GetFiles(System.IO.Path.Combine (current_directory, "database-scripts/migration-plan-set"));
                    foreach(var file_path in migration_plan_directory_files)
                    {
                        var file_info = new System.IO.FileInfo(file_path);
                        var id = file_info.Name.Replace(".json","");
                        process_migration_plan_by_id(id);
                    }

                    var Sync_All_Documents_Message = new mmria.server.model.actor.Sync_All_Documents_Message
                    (
                        DateTime.Now
                    );
                    
                    Context.ActorOf(Props.Create<mmria.server.model.actor.Synchronize_Case>()).Tell(Sync_All_Documents_Message);
                    */
                    /*
                    var case_sync_actor = Context.ActorSelection("akka://mmria-actor-system/user/case_sync_actor");
                    case_sync_actor.Tell(Sync_All_Documents_Message);
                    */
                    break;
        }
        }
        finally
        {
            Context.Stop(this.Self);
        }

        
    }

    private void process_migration_plan_by_id(string migration_plan_id)
    {
        try
        {
            //string migration_plan_id = message.ToString();

            Console.WriteLine($"Process_Migrate_Data Begin {System.DateTime.Now}");


            string url = db_config.url + $"/{db_config.prefix}mmrds/_all_docs?include_docs=true";

            var case_curl = new cURL("GET", null, url, null, db_config.user_name, db_config.user_value);
            string responseFromServer = case_curl.execute();
            
            var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

            var migration_plan = get_migration_plan(migration_plan_id);

            var lookup = get_look_up(migration_plan);

            foreach(var case_item in case_response.rows)
            {
                var case_has_changed = false;
                foreach(var plan_item in migration_plan.plan_items)
                {
                    if
                    (
                        string.IsNullOrWhiteSpace(plan_item.old_mmria_path) || 
                        string.IsNullOrWhiteSpace(plan_item.new_mmria_path)
                    )
                    {
                        continue;
                    }
/*
                    if
                    (
                        plan_item.old_mmria_path == "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial" &&
                        case_item.id == "ad52d04f-dced-60ed-ba09-3cdfbba390b0"
                    )
                    {
                        Console.Write("break");
                    }
*/

                    var old_value_list = get_value(plan_item.old_mmria_path.TrimEnd('/'), case_item.doc);

                    for(var i = 0; i < old_value_list.Count; i++)
                    {
                        var old_value = old_value_list[i];

                        string new_value = old_value.value;
                        if
                        (
                            lookup[plan_item.old_mmria_path][plan_item.new_mmria_path].ContainsKey(old_value.value + "")
                            
                        )
                        {
                            new_value = lookup[plan_item.old_mmria_path][plan_item.new_mmria_path][old_value.value];
                            if(old_value.value != new_value)
                            {
                                var set_result = set_value(plan_item.new_mmria_path.TrimEnd('/'), new_value, case_item.doc, old_value.index);

                                case_has_changed = true;
                            }
                            
                        }
                        /*
                        else if
                        (
                            plan_item.old_mmria_path == plan_item.new_mmria_path
                        )
                        {
                            var set_result = set_value(plan_item.new_mmria_path.TrimEnd('/'), new_value, case_item.doc, old_value.index);
                        }
                        */

                        
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

            Console.WriteLine($"Process_Migrate_Data End {System.DateTime.Now}");
        
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Process_Migrate_Data exception {System.DateTime.Now}\n{ex}");
        }
    }

    private mmria.common.model.migration_plan get_migration_plan(string p_id)
    {
        string url = db_config.url + $"/metadata/{p_id}";

        var curl = new cURL("GET", null, url, null, db_config.user_name, db_config.user_value);
        string responseFromServer = curl.execute();
            
        var migration_plan = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.migration_plan>(responseFromServer);

        return migration_plan;

    }


    private Dictionary<string,Dictionary<string,Dictionary<string,string>>> get_look_up(mmria.common.model.migration_plan p_migration_plan)
    {
        var result = new Dictionary<string,Dictionary<string,Dictionary<string,string>>>(StringComparer.OrdinalIgnoreCase);

        foreach(var plan_item in p_migration_plan.plan_items)
        {

            if
            (
                string.IsNullOrEmpty(plan_item.old_mmria_path) ||
                string.IsNullOrEmpty(plan_item.new_mmria_path) 
            )
            {
                continue;
            }

            if(!result.ContainsKey(plan_item.old_mmria_path))
            {
                result.Add(plan_item.old_mmria_path, new Dictionary<string,Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase));
            }

            var Left_dictionary = result[plan_item.old_mmria_path];

            if(!Left_dictionary.ContainsKey(plan_item.new_mmria_path))
            {
                Left_dictionary.Add(plan_item.new_mmria_path, new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase));
            }

            var current_dictionary = Left_dictionary[plan_item.new_mmria_path];
            if(current_dictionary.ContainsKey(plan_item.old_value))
            {
                current_dictionary[plan_item.old_value] = plan_item.new_value;
            }
            else
            {
                current_dictionary.Add(plan_item.old_value, plan_item.new_value);
            }
        }


        return result;
    }

    private List<(string value,int index)> get_value(string p_metadata_path, object p_case, int p_index = -1)
    {
        var result = new List<(string value,int index)>();

        var metadata_path_array = p_metadata_path.Split("/");
        var item_key = metadata_path_array[0];

        if(metadata_path_array.Length == 1)
        {
            switch(p_case)
            {
                case IDictionary<string,object> val:
                    if(val.ContainsKey(item_key))
                    {
                        object new_item = val[item_key];

                        switch(new_item)
                        {
                            default:
                                if(new_item != null)
                                {
                                    result.Add((new_item.ToString(), p_index));
                                }
                                
                            break;
                        }
                    }

                    break;

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
            object new_item = null;

            switch(p_case)
            {
                case IDictionary<string,object> val:
                    if(val.ContainsKey(item_key))
                    {
                        new_item = val[item_key];
                    }
                    result.AddRange(get_value(metadata_path, new_item));
                    break;

                case IList<object> val:

                    for(var i = 0; i < val.Count; i++)
                    {
                        var item = val[i];
                        switch(item)
                        {
                            case IDictionary<string,object> item_val:
                            if(item_val.ContainsKey(item_key))
                            {
                                new_item = item_val[item_key];
                            }
                            result.AddRange(get_value(metadata_path, new_item, i));
                            break;
                        }
                    }
                    
                    break;
            }
            
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


