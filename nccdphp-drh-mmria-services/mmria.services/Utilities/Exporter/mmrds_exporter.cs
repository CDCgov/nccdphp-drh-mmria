﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Microsoft.Extensions.Configuration;
using mmria.getset;


namespace mmria.services.Utilities.Exporter;

public sealed class mmrds_exporter
{


    #if !IS_PMSS_ENHANCED
    const string main_export_file_name = "mmria_case_export.csv";
    #endif
    #if IS_PMSS_ENHANCED
    const string main_export_file_name = "pmss_case_export.csv";
    #endif
    private string auth_token = null;
    private string user_name = null;

    private string juris_user_name = null;
    private string value_string = null;

    private string database_url = null;
    private string item_file_name = null;
    private string item_directory_name = null;
    private string item_id = null;
    private bool is_cdc_de_identified = false;

    private bool is_offline_mode;

    private bool is_excel_file_type = false;

    private HashSet<string> de_identified_set;

    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> List_Look_Up;

    private System.Collections.Generic.Dictionary<string, bool> is_header_written = new();
    System.Collections.Generic.Dictionary<string, string> path_to_field_name_map = new Dictionary<string, string>();

    mmria.common.metadata.app current_metadata;

    mmria.common.couchdb.DBConfigurationDetail db_config;

    private System.IO.StreamWriter[] qualitativeStreamWriter = new System.IO.StreamWriter[5];
    private int[] qualitativeStreamCount = new int[] { 0, 0, 0, 0, 0 };
    private const int max_qualitative_length = 31000;

    private const string over_limit_message = "Over the qualitative limit. Check the over-the-limit folder for details.";

    private mmria.server.model.actor.ScheduleInfoMessage Configuration;

    public mmrds_exporter
    (
        mmria.server.model.actor.ScheduleInfoMessage configuration
    )
    {
        this.Configuration = configuration;

        db_config = new()
        {
            url = configuration.couch_db_url,
            prefix = configuration.db_prefix,
            user_name = configuration.user_name,
            user_value = configuration.user_value
        };
    }
    public bool Execute(export_queue_item queue_item)
    {

        try
        {
        db_config.url = this.Configuration.couch_db_url;
        this.juris_user_name = this.Configuration.jurisdiction_user_name;
        this.user_name = this.Configuration.user_name;
        this.value_string = this.Configuration.user_value;

        this.item_file_name = queue_item.file_name;
        this.item_directory_name = queue_item.file_name.Substring(0, queue_item.file_name.IndexOf("."));
        this.item_id = queue_item._id;

        this.is_excel_file_type = queue_item.case_file_type == "xlsx" ? true : false;


        if (string.IsNullOrWhiteSpace(db_config.url))
        {
            db_config.url = Configuration.couch_db_url;

            if (string.IsNullOrWhiteSpace(db_config.url))
            {
                System.Console.WriteLine("missing database_url");
                System.Console.WriteLine(" form database:[file path]");
                System.Console.WriteLine(" example database:http://localhost:5984");
                System.Console.WriteLine(" mmria.exe export user_name:user1 password:secret url:http://localhost:12345 database_url:http://localhost:5984");

                return false;
            }
        }


        if (string.IsNullOrWhiteSpace(this.user_name))
        {
            System.Console.WriteLine("missing user_name");
            System.Console.WriteLine(" form user_name:[user_name]");
            System.Console.WriteLine(" example user_name:user1");
            System.Console.WriteLine(" mmria.exe export user_name:user1 password:secret url:http://localhost:12345");
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.value_string))
        {
            System.Console.WriteLine("missing password");
            System.Console.WriteLine(" form password:[password]");
            System.Console.WriteLine(" example password:secret");
            System.Console.WriteLine(" mmria.exe export user_name:user1 password:secret url:http://localhost:12345");
            return false;
        }

        string export_directory = System.IO.Path.Combine(Configuration.export_directory, this.item_directory_name);

        if (!System.IO.Directory.Exists(export_directory))
        {
            System.IO.Directory.CreateDirectory(export_directory);
        }

        string export_root_directory = export_directory;

        export_directory = System.IO.Path.Combine(Configuration.export_directory, this.item_directory_name, "over-the-limit");

        if (!System.IO.Directory.Exists(export_directory))
        {
            System.IO.Directory.CreateDirectory(export_directory);
        }

        this.qualitativeStreamWriter[0] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "over-the-qualitative-limit.txt"), true);
        #if !IS_PMSS_ENHANCED
            this.qualitativeStreamWriter[1] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "case-narrative.txt"), true);
            this.qualitativeStreamWriter[2] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "informant-interview.txt"), true);
            this.qualitativeStreamWriter[3] = new System.IO.StreamWriter(System.IO.Path.Combine(export_root_directory, "case-narrative-plaintext.txt"), true);
            this.qualitativeStreamWriter[4] = new System.IO.StreamWriter(System.IO.Path.Combine(export_root_directory, "informant-interview-plaintext.txt"), true);
        #endif


        string metadata_url = db_config.url + $"/metadata/version_specification-{this.Configuration.version_number}/metadata";
        cURL metadata_curl = new cURL("GET", null, metadata_url, null, this.user_name, this.value_string);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());
        this.current_metadata = metadata;

        System.Collections.Generic.Dictionary<string, int> path_to_int_map = new Dictionary<string, int>();
        System.Collections.Generic.Dictionary<string, string> path_to_file_name_map = new Dictionary<string, string>();

        System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> path_to_node_map = new Dictionary<string, mmria.common.metadata.node>();
        System.Collections.Generic.Dictionary<string, string> path_to_grid_map = new Dictionary<string, string>();
        System.Collections.Generic.Dictionary<string, string> path_to_multi_form_map = new Dictionary<string, string>();
        System.Collections.Generic.Dictionary<string, string> grid_to_multi_form_map = new Dictionary<string, string>();

        System.Collections.Generic.HashSet<string> path_to_flat_map = new System.Collections.Generic.HashSet<string>();

        System.Collections.Generic.Dictionary<string, WriteCSV> path_to_csv_writer = new Dictionary<string, WriteCSV>();

        generate_path_map
        (metadata, "", main_export_file_name, "",
            path_to_int_map,
            path_to_file_name_map,
            path_to_node_map,
            path_to_grid_map,
            path_to_multi_form_map,
            false,
            grid_to_multi_form_map,
            false,
            path_to_flat_map
        );


        List_Look_Up = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in metadata.children)
        {
            Get_List_Look_Up(List_Look_Up, metadata.lookup, child, path_to_int_map, "/" + child.name);
        }

        System.Collections.Generic.HashSet<string> flat_grid_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        System.Collections.Generic.HashSet<string> mutiform_grid_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> kvp in path_to_grid_map)
        {
            if (grid_to_multi_form_map.ContainsKey(kvp.Key))
            {
            mutiform_grid_set.Add(kvp.Value);
            }
        }

        foreach (KeyValuePair<string, string> kvp in path_to_grid_map)
        {
            if (
            !mutiform_grid_set.Contains(kvp.Value) &&
            !flat_grid_set.Contains(kvp.Value)
            )
            {
            flat_grid_set.Add(kvp.Value);
            }
        }

        int stream_file_count = 0;
        foreach (string file_name in path_to_file_name_map.Select(kvp => kvp.Value).Distinct())
        {
            path_to_csv_writer.Add(file_name, new WriteCSV(file_name, this.item_directory_name, Configuration.export_directory, is_excel_file_type));
            stream_file_count++;
        }

        create_header_row
        (
            path_to_int_map,
            path_to_flat_map,
            path_to_node_map,
            path_to_csv_writer[main_export_file_name].Table,
            true,
            false,
            false
        );

        var grantee_column = new System.Data.DataColumn("export_jurisdiction_name", typeof(string));

        grantee_column.DefaultValue = queue_item.grantee_name;
        path_to_csv_writer[main_export_file_name].Table.Columns.Add(grantee_column);

        if(! is_header_written.ContainsKey(main_export_file_name))
        {
            is_header_written.Add(main_export_file_name, true);
            path_to_csv_writer[main_export_file_name].WriteHeadersToStream();
        }


        de_identified_set = new HashSet<string>();
        if (queue_item.de_identified_field_set != null)
        {
            foreach (string path in queue_item.de_identified_field_set)
            {
            de_identified_set.Add(path.TrimStart('/'));
            }
        }


        HashSet<string> Custom_Case_Id_List = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in queue_item.case_set)
        {
            Custom_Case_Id_List.Add(id);
        }

        List<System.Dynamic.ExpandoObject> all_cases_rows = new List<System.Dynamic.ExpandoObject>();

        #if !IS_PMSS_ENHANCED
        var jurisdiction_hashset = mmria.services.authorization.get_current_jurisdiction_id_set_for(db_config, this.juris_user_name);
        #endif
        #if IS_PMSS_ENHANCED
        var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, this.juris_user_name);
        #endif

        if (queue_item.case_filter_type == "custom")
        {
            /*
            foreach (System.Dynamic.ExpandoObject case_row in all_cases.rows)
            {
            var check_item = ((IDictionary<string, object>)case_row)["doc"] as System.Dynamic.ExpandoObject;
            if (check_item != null)
            {
                var temp = check_item as IDictionary<string, object>;

                if
                (
                temp != null &&
                temp.ContainsKey("_id") &&

                temp["_id"] != null &&
                Custom_Case_Id_List.Contains(temp["_id"].ToString())
                )
                {
                all_cases_rows.Add(check_item);
                }

            }

            }*/
        }

        else
        {

            try
            {
                string request_string = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000";

                var case_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                string case_view_responseFromServer = case_view_curl.execute();

                mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(case_view_responseFromServer);

                foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
                {
                    Custom_Case_Id_List.Add(cvi.id);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

    /*
            foreach (System.Dynamic.ExpandoObject case_row in all_cases.rows)
            {
            all_cases_rows.Add(((IDictionary<string, object>)case_row)["doc"] as System.Dynamic.ExpandoObject);
            }
            */
        }



        //foreach (System.Dynamic.ExpandoObject case_row in all_cases_rows)
        foreach(string case_id in Custom_Case_Id_List)
        {
            string URL = $"{db_config.url}/{db_config.prefix}mmrds/{case_id}";
            cURL document_curl = new cURL("GET", null, URL, null, this.user_name, this.value_string);
            System.Dynamic.ExpandoObject case_row = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_curl.execute());

            IDictionary<string, object> case_doc = case_row as IDictionary<string, object>;

            if
            (
                case_doc == null ||
                !case_doc.ContainsKey("_id") ||
                case_doc["_id"] == null ||
                case_doc["_id"].ToString().StartsWith("_design", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                continue;
            }


            var is_jurisdiction_ok = false;
            string HR_R_ID = null;

            #if !IS_PMSS_ENHANCED
            var home_record = case_doc["home_record"] as IDictionary<string, object>;

            if (home_record != null)
            {
                if (!home_record.ContainsKey("jurisdiction_id"))
                {
                    home_record.Add("jurisdiction_id", "/");
                }

                if(home_record.ContainsKey("record_id"))
                {
                    HR_R_ID = home_record["record_id"].ToString();
                }

                foreach (var jurisdiction_item in jurisdiction_hashset)
                {
                    var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction_id);
                    
                    if (regex.IsMatch(home_record["jurisdiction_id"].ToString()) && jurisdiction_item.ResourceRight == mmria.services.ResourceRightEnum.ReadCase)
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }
                    
                }
            }
            #endif
            #if IS_PMSS_ENHANCED
            var tracking_record = case_doc["tracking"] as IDictionary<string, object>;
            if (tracking_record == null) continue;

            var home_record = tracking_record["admin_info"] as IDictionary<string, object>;

            if (home_record != null)
            {
                if (!home_record.ContainsKey("jurisdiction"))
                {
                    home_record.Add("jurisdiction", "/");
                }

                if(home_record.ContainsKey("pmssno"))
                {
                    HR_R_ID = home_record["pmssno"].ToString();
                }

                foreach (var jurisdiction_item in jurisdiction_hashset)
                {
                    var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction_id);
                    /*
                    if(jurisdiction_item.jurisdiction_id == "/")
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }*/

                    if (regex.IsMatch("/" + home_record["jurisdiction"].ToString()) && jurisdiction_item.ResourceRight == mmria.pmss.server.utils.ResourceRightEnum.ReadCase)
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }
                    
                    

                }
            }
            #endif

            if (!is_jurisdiction_ok)
            {
                continue;
            }


            System.Data.DataRow row = path_to_csv_writer[main_export_file_name].Table.NewRow();
            string mmria_case_id = case_doc["_id"].ToString();
            row["hr_r_id"] = HR_R_ID;
            row["_id"] = mmria_case_id;
            
            foreach (string path in path_to_flat_map)
            {
            if 
            (
                !path_to_node_map.ContainsKey(path) ||
                path_to_node_map[path].type.ToLower() == "app" ||
                path_to_node_map[path].type.ToLower() == "form" ||
                ( 
                    path_to_node_map[path].type.ToLower() == "group" &&
                    (
                        path_to_node_map[path].tags == null ||
                        path_to_node_map[path].tags.Length < 1 ||
                        !path_to_node_map[path].tags[0].Equals("CALC_DATE", StringComparison.OrdinalIgnoreCase)
                    )
                    
                ) ||
                path_to_node_map[path].type.ToLower() == "always_enabled_button" ||
                path_to_node_map[path].type.ToLower() == "button" ||
                path_to_node_map[path].type.ToLower() == "chart" ||
                path_to_node_map[path].type.ToLower() == "label" ||
                path_to_node_map[path].mirror_reference != null

            )
            {
                continue;
            }

            if 
            (
                path_to_node_map[path].type.ToLower() == "list" &&
                path_to_node_map[path].is_multiselect != null &&
                path_to_node_map[path].is_multiselect == true
            )
            {
                //System.Console.WriteLine("break");
            }

            if(path == "case_narrative/case_opening_overview")
            {
                //System.Console.WriteLine("break");
            }

            if(path == "home_record/record_id")
            {
                continue;
            }

            
            object val = get_value(case_doc as IDictionary<string, object>, path);
            try
            {

                switch (path_to_node_map[path].type.ToLower())
                {

                    case "group":

                        int? month = null;
                        int? day = null;
                        int? year = null;

                        var month_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/month");
                        var day_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/day");
                        var year_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/year");
                        
                        

                        val = "";

                        month = ConvertToInt(month_result);
                        day = ConvertToInt(day_result);
                        year = ConvertToInt(year_result);

                        if
                        (
                            month.HasValue && 
                            day.HasValue &&
                            year.HasValue
                        )
                        {
                            string file_field_name = path_to_field_name_map[path];
                            val = $"{month}/{day}/{year}";  
                            row[file_field_name] = val;

                        }
                        
                    break;

                case "number":
                    double try_double = 0;

                    if (val != null && (!string.IsNullOrWhiteSpace(val.ToString())) && double.TryParse(val.ToString(), out try_double))
                    {
                    string file_field_name = path_to_field_name_map[path];

                    row[file_field_name] = val;

                    }
                    break;
                case "list":

                    if
                    (
                    (
                        path_to_node_map[path].is_multiselect != null &&
                        path_to_node_map[path].is_multiselect == true
                    ) ||
                    val is List<object>
                    )
                    {

                    List<object> temp = val as List<object>;
                    if (temp != null && temp.Count > 0)
                    {
                        List<string> temp2 = new List<string>();
                        foreach (var item in temp)
                        {
                        var key = "/" + path;
                        var item_key = item.ToString();
                        if (List_Look_Up.ContainsKey(key) && List_Look_Up[key].ContainsKey(item_key))
                        {
                            temp2.Add(List_Look_Up["/" + path][item.ToString()]);
                        }
                        else
                        {
                            temp2.Add(item.ToString());
                        }
                        }


                        if(temp2?.Count > 1 && List_Look_Up.ContainsKey("/" + path))
                        {
                        var look_up_list = List_Look_Up["/" + path];
                        temp2 = SortListAgainstDictionary(temp2, look_up_list);
                        }

                        string file_field_name = path_to_field_name_map[path];
                        row[file_field_name] = string.Join("|", temp2);

                    }
                    else
                    {
                        string file_field_name = path_to_field_name_map[path];
                        row[file_field_name] = "(blank)";
                    }
                    }
                    else
                    {
                    string file_field_name = path_to_field_name_map[path];
                    if (val != null)
                    {
                        if (val is List<object>)
                        {
                        List<object> temp = val as List<object>;
                        if (temp != null && temp.Count > 0)
                        {
                            List<string> temp2 = new List<string>();
                            foreach (var item in temp)
                            {
                            var key = "/" + path;
                            var item_key = item.ToString();
                            if (List_Look_Up.ContainsKey(key) && List_Look_Up[key].ContainsKey(item_key))
                            {
                                temp2.Add(List_Look_Up["/" + path][item.ToString()]);
                            }
                            else
                            {
                                temp2.Add(item.ToString());
                            }
                            }


                            row[file_field_name] = string.Join("|", temp2);
                        }
                        else
                        {
                            row[file_field_name] = "(blank)";
                        }

                        }
                        else
                        {
                        if
                        (
                            path_to_node_map[path].data_type != null &&
                            path_to_node_map[path].data_type.ToLower() == "string" &&
                            (
                            val.ToString() == "9999" ||
                            val.ToString() == "8888" ||
                            val.ToString() == "7777"
                            )
                        )
                        {

                            if (val.ToString() == "9999")
                            {
                            row[file_field_name] = "";
                            }

                            if (val.ToString() == "8888")
                            {
                            row[file_field_name] = "Not specified";
                            }

                            if (val.ToString() == "7777")
                            {
                            row[file_field_name] = "Unknown";
                            }

                        }
                        else
                        {
                            if (val.ToString() == "")
                            {
                            if
                            (
                                path_to_node_map[path].data_type != null &&
                                path_to_node_map[path].data_type.ToLower() == "string"
                            )
                            {
                                row[file_field_name] = "";
                            }
                            else
                            {
                                row[file_field_name] = "9999";
                            }

                            }
                            else
                            {
                            row[file_field_name] = val;
                            }

                        }

                        }
                    }
                    else
                    {
                        if
                        (
                        path_to_node_map[path].data_type != null &&
                        path_to_node_map[path].data_type.ToLower() == "string"

                        )
                        {
                        row[file_field_name] = "";
                        }
                        else
                        {
                        row[file_field_name] = "9999";
                        }
                    }
                    }

                    break;
                default:
                    if (val != null)
                    {
                    string clearText = "";

                    if(path=="host_state")
                    {
                        val = val.ToString().ToUpper();
                    }

                    if(path == "case_narrative/case_opening_overview")
                    {
                        clearText = mmria.common.util.CaseNarrative.StripHTML(val?.ToString());
                        if (clearText.Length > 0) 
                        {
                            
                            WriteQualitativeData
                            (
                            mmria_case_id,
                            HR_R_ID,
                            path,
                            clearText,
                            -1,
                            -1,
                            true
                            );
                        }
                    }

                    if
                    (
                        (
                        path_to_node_map[path].type.ToLower() == "textarea" ||
                        path_to_node_map[path].type.ToLower() == "string"
                        ) &&
                        val.ToString().Length > max_qualitative_length
                    )
                    {
                        WriteQualitativeData
                        (
                        mmria_case_id,
                        HR_R_ID,
                        path,
                        val?.ToString(),
                        -1,
                        -1
                        );

                        val = over_limit_message;
                    }

                    string file_field_name = path_to_field_name_map[path];
                    row[file_field_name] = val;

                    }
                    break;

                }
            }
            catch (Exception)
            {
                System.Console.Write("bad export value: {0} - {1} - {2}", mmria_case_id, val, path);
            }

            }
            if(is_excel_file_type)
            {
                path_to_csv_writer[main_export_file_name].Table.Rows.Add(row);
            }
            else
            {
                path_to_csv_writer[main_export_file_name].WriteToStream(row);
            }

        //System.Console.WriteLine("flat grid-star 621");
            // flat grid - start
            foreach (KeyValuePair<string, mmria.common.metadata.node> ptn in path_to_node_map.Where(x => x.Value.type.ToLower() == "grid"))
            {
            string path = ptn.Key;
            if (flat_grid_set.Contains(path_to_grid_map[path]))
            {
                string grid_name = path_to_grid_map[path];

                HashSet<string> grid_field_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, mmria.common.metadata.node> ptgm in path_to_node_map.Where(x => x.Key.StartsWith(path) && x.Key != path))
                {
                grid_field_set.Add(ptgm.Key);
                }

                create_header_row
                (
                    path_to_int_map,
                    grid_field_set,
                    path_to_node_map,
                    path_to_csv_writer[grid_name].Table,
                    true,
                    true,
                    false
                );

                if(! is_header_written.ContainsKey(grid_name))
                {
                    is_header_written.Add(grid_name, true);
                    path_to_csv_writer[grid_name].WriteHeadersToStream();
                }

                object raw_data = get_value(case_doc as IDictionary<string, object>, path);
                List<object> object_data = raw_data as List<object>;

                if (object_data != null)
                for (int i = 0; i < object_data.Count; i++)
                {
                    IDictionary<string, object> grid_item_row = object_data[i] as IDictionary<string, object>;

                    if (grid_item_row == null)
                    {
                        continue;
                    }

                    System.Data.DataRow grid_row = path_to_csv_writer[grid_name].Table.NewRow();
                    grid_row["hr_r_id"] = HR_R_ID;
                    grid_row["_id"] = mmria_case_id;
                    grid_row["_record_index"] = i;
                    foreach (KeyValuePair<string, string> kvp in path_to_grid_map.Where(k => k.Value == grid_name))
                    {
                        foreach (string node in grid_field_set)
                        {
                            try
                            {
                                var test_key = path_to_node_map[node].name;
                                if (!grid_item_row.ContainsKey(test_key))
                                {
                                    continue;
                                }
                                object val = grid_item_row[test_key];

                                if (de_identified_set.Contains(node))
                                {
                                    val = null;
                                }

                                string file_field_name = path_to_field_name_map[node];
                                if (val != null)
                                {
                                    switch (path_to_node_map[node].type.ToLower())
                                    {
                                        case "group":

                                        int? month = null;
                                        int? day = null;
                                        int? year = null;

                                        var month_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/month");
                                        var day_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/day");
                                        var year_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/year");
                                        
                                        

                                        val = "";

                                        month = ConvertToInt(month_result);
                                        day = ConvertToInt(day_result);
                                        year = ConvertToInt(year_result);

                                        if
                                        (
                                            month.HasValue && 
                                            day.HasValue &&
                                            year.HasValue
                                        )
                                        {
                                            //string file_field_name = path_to_field_name_map[path];
                                            val = $"{month}/{day}/{year}";  
                                            grid_row[file_field_name] = val;

                                        }
                                        
                                    break;








                                    case "number":
                                        if (!string.IsNullOrWhiteSpace(val.ToString()))
                                        {
                                        double test_double = 0.0;
                                        if (double.TryParse(val.ToString(), out test_double))
                                        {

                                            grid_row[file_field_name] = val;
                                        }
                                        else
                                        {

                                        }

                                        }
                                        break;
                                    case "list":
                                        if
                                        (
                                        (
                                            path_to_node_map[node].is_multiselect != null &&
                                            path_to_node_map[node].is_multiselect == true
                                        ) ||
                                        val is List<object>
                                        )
                                        {

                                        List<object> temp = val as List<object>;
                                        if (temp != null && temp.Count > 0)
                                        {
                                            List<string> temp2 = new List<string>();
                                            foreach (var item in temp)
                                            {
                                            var key = "/" + node;
                                            var item_key = item.ToString();
                                            if (List_Look_Up.ContainsKey(key) && List_Look_Up[key].ContainsKey(item_key))
                                            {
                                                temp2.Add(List_Look_Up["/" + node][item.ToString()]);
                                            }
                                            else
                                            {
                                                temp2.Add(item.ToString());
                                            }
                                            }

                                            grid_row[file_field_name] = string.Join("|", temp2);

                                        }
                                        else
                                        {
                                            grid_row[file_field_name] = "(blank)";
                                        }
                                        }
                                        else
                                        {
                                        if (val != null)
                                        {


                                            if (val is List<object>)
                                            {
                                            List<object> temp = val as List<object>;
                                            if (temp != null && temp.Count > 0)
                                            {
                                                List<string> temp2 = new List<string>();
                                                foreach (var item in temp)
                                                {
                                                var key = "/" + node;
                                                var item_key = item.ToString();
                                                if (List_Look_Up.ContainsKey(key) && List_Look_Up[key].ContainsKey(item_key))
                                                {
                                                    temp2.Add(List_Look_Up["/" + node][item.ToString()]);
                                                }
                                                else
                                                {
                                                    temp2.Add(item.ToString());
                                                }
                                                }


                                                grid_row[file_field_name] = string.Join("|", temp2);

                                            }
                                            else
                                            {
                                                grid_row[file_field_name] = "(blank)";
                                            }
                                            }
                                            else
                                            {
                                            if
                                            (
                                                path_to_node_map[node].data_type != null &&
                                                path_to_node_map[node].data_type.ToLower() == "string" &&
                                                (
                                                val.ToString().Equals("9999") ||
                                                val.ToString().Equals("8888") ||
                                                val.ToString().Equals("7777")
                                                )
                                            )
                                            {

                                                if (val.ToString().Equals("9999"))
                                                {
                                                grid_row[file_field_name] = "";
                                                }

                                                if (val.ToString().Equals("8888"))
                                                {
                                                grid_row[file_field_name] = "Not specified";
                                                }

                                                if (val.ToString().Equals("7777"))
                                                {
                                                grid_row[file_field_name] = "Unknown";
                                                }

                                            }
                                            else
                                            {
                                                if (val.ToString().Equals("") || val.ToString().Equals("9999"))
                                                {
                                                if
                                                (
                                                    path_to_node_map[node].data_type != null &&
                                                    path_to_node_map[node].data_type.ToLower() == "string"
                                                )
                                                {
                                                    grid_row[file_field_name] = "";
                                                }
                                                else
                                                {
                                                    grid_row[file_field_name] = "9999";
                                                }

                                                //grid_row[file_field_name] = "9999";
                                                }
                                                else
                                                {
                                                grid_row[file_field_name] = val;
                                                }

                                            }
                                            }
                                        }
                                        else
                                        {
                                            if
                                            (
                                            path_to_node_map[node].data_type != null &&
                                            path_to_node_map[node].data_type.ToLower() == "string"

                                            )
                                            {
                                            grid_row[file_field_name] = "";
                                            }
                                            else
                                            {
                                            grid_row[file_field_name] = "9999";
                                            }

                                        }
                                        }

                                        break;
                                    default:
                                        if
                                        (
                                        (
                                            path_to_node_map[node].type.ToLower() == "textarea" ||
                                            path_to_node_map[node].type.ToLower() == "string"
                                        )
                                        &&
                                        val.ToString().Length > max_qualitative_length
                                        )
                                        {
                                        WriteQualitativeData
                                        (
                                            mmria_case_id,
                                            HR_R_ID,
                                            node,
                                            val?.ToString(),
                                            i,
                                            -1
                                        );
                                        val = over_limit_message;
                                        }




                                        grid_row[file_field_name] = val;
                                        break;
                                    }
                                }
                                else
                                {

                                    if (path_to_node_map[node].type.ToLower() == "list")
                                    {
                                    if
                                    (
                                        path_to_node_map[node].data_type != null &&
                                        path_to_node_map[node].data_type.ToLower() == "string"

                                    )
                                    {
                                        grid_row[file_field_name] = "";
                                    }
                                    else
                                    {
                                        grid_row[file_field_name] = "9999";
                                    }
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    if(is_excel_file_type)
                    {
                        path_to_csv_writer[grid_name].Table.Rows.Add(grid_row);
                    }
                    else
                    {
                        path_to_csv_writer[grid_name].WriteToStream(grid_row);
                    }
                }

            }
            }
            // flat grid - end

            //System.Console.WriteLine("multiform-start 918");

            // multiform - start
            foreach (KeyValuePair<string, string> kvp in path_to_multi_form_map)
            {
            HashSet<string> form_field_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, mmria.common.metadata.node> ptgm in path_to_node_map.Where(x => x.Key.StartsWith(kvp.Key) && x.Key != kvp.Key))
            {
                form_field_set.Add(ptgm.Key);
            }


            foreach (KeyValuePair<string, string> ptgm in path_to_grid_map)
            {
                form_field_set.RemoveWhere(x => x.StartsWith(ptgm.Key, StringComparison.InvariantCultureIgnoreCase));

            }



            create_header_row
            (
                path_to_int_map,
                form_field_set,
                path_to_node_map,
                path_to_csv_writer[kvp.Value].Table,
                true,
                true,
                false
            );

            if(! is_header_written.ContainsKey(kvp.Value))
            {
                is_header_written.Add(kvp.Value, true);
                path_to_csv_writer[kvp.Value].WriteHeadersToStream();
            }

            object form_raw_data = get_value(case_doc as IDictionary<string, object>, kvp.Key);
            List<object> form_object_data = form_raw_data as List<object>;

            if (form_object_data != null)
                for (int i = 0; i < form_object_data.Count; i++)
                {
                
                System.Data.DataRow form_row = path_to_csv_writer[kvp.Value].Table.NewRow();
                form_row["hr_r_id"] = HR_R_ID;
                form_row["_id"] = mmria_case_id;
                form_row["_record_index"] = i;

                foreach (string path in form_field_set)
                {
                    if (
                    path_to_node_map[path].type.ToLower() == "app" ||
                    path_to_node_map[path].type.ToLower() == "form" ||
                    ( 
                        path_to_node_map[path].type.ToLower() == "group" &&
                        (
                            path_to_node_map[path].tags == null ||
                            path_to_node_map[path].tags.Length < 1 ||
                            !path_to_node_map[path].tags[0].Equals("CALC_DATE", StringComparison.OrdinalIgnoreCase)
                        )
                    
                    ) ||
                    path_to_node_map[path].type.ToLower() == "grid" ||
                    path_to_node_map[path].type.ToLower() == "always_enabled_button" ||
                    path_to_node_map[path].type.ToLower() == "button" ||
                    path_to_node_map[path].type.ToLower() == "chart" ||
                    path_to_node_map[path].type.ToLower() == "label" ||
                    path_to_node_map[path].mirror_reference != null

                    )
                    {
                    continue;
                    }

                    string[] temp_path = path.Split('/');
                    List<string> form_path_list = new List<string>();
                    for (int temp_path_index = 0; temp_path_index < temp_path.Length; temp_path_index++)
                    {
                    form_path_list.Add(temp_path[temp_path_index]);
                    if (temp_path_index == 0)
                    {
                        form_path_list.Add(i.ToString());
                    }

                    }

                    object val = get_value(case_doc as IDictionary<string, object>, string.Join("/", form_path_list));

                    switch (path_to_node_map[path].type.ToLower())
                    {
                    case "group":


                        

                        int? month = null;
                        int? day = null;
                        int? year = null;

                        object month_result =  null;
                        object day_result =  null;
                        object year_result =  null;

                        var dictionary = val as IDictionary<string,object>;

                        if(dictionary == null) break;
                        if(dictionary.ContainsKey("month"))
                        {
                            month_result = dictionary["month"];
                        }

                        if(dictionary.ContainsKey("day"))
                        {
                            day_result = dictionary["day"];
                        }

                        if(dictionary.ContainsKey("year"))
                        {
                            year_result = dictionary["year"];
                        }

                        //var month_result =  dictionary["month"]get_value(case_doc as IDictionary<string, object>, $"{path}/month");
                        //var day_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/day");
                        //var year_result =  get_value(case_doc as IDictionary<string, object>, $"{path}/year");
                        
                        var new_val = "";

                        month = ConvertToInt(month_result);
                        day = ConvertToInt(day_result);
                        year = ConvertToInt(year_result);

                        if
                        (
                            month.HasValue && 
                            day.HasValue &&
                            year.HasValue
                        )
                        {
                            string file_field_name = path_to_field_name_map[path];
                            new_val = $"{month}/{day}/{year}";  
                            form_row[file_field_name] = new_val;

                        }
                        
                    break;
                    case "number":
                        if (val != null)
                        {
                        var test_value_string = val.ToString();
                        if (!string.IsNullOrWhiteSpace(test_value_string))
                        {
                            double test_double = 0.0;
                            if (double.TryParse(test_value_string, out test_double))
                            {
                            string file_field_name = path_to_field_name_map[path];
                            form_row[file_field_name] = test_double;
                            }
                            else
                            {

                            }
                        }
                        else
                        {

                        }
                        }
                        break;
                    case "list":

                        if
                        (path_to_node_map[path].is_multiselect != null &&
                        path_to_node_map[path].is_multiselect == true

                        )
                        {

                        IList<object> temp = val as IList<object>;
                        if (temp != null && temp.Count > 0)
                        {
                            List<string> temp2 = new List<string>();
                            foreach (var item in temp)
                            {
                            var key = "/" + path;
                            var item_key = item.ToString();
                            if (List_Look_Up.ContainsKey(key) && List_Look_Up[key].ContainsKey(item_key))
                            {
                                temp2.Add(List_Look_Up["/" + path][item.ToString()]);
                            }
                            else
                            {
                                temp2.Add(item.ToString());
                            }
                            }

                            
                            if (temp2?.Count > 1 && List_Look_Up.ContainsKey("/" + path))
                            {
                                var look_up_list = List_Look_Up["/" + path];
                                temp2 = SortListAgainstDictionary(temp2, look_up_list);
                            }
                            
                            string file_field_name = path_to_field_name_map[path];
                            form_row[file_field_name] = string.Join("|", temp2);

                        }
                        else
                        {
                            string file_field_name = path_to_field_name_map[path];
                            form_row[file_field_name] = "(blank)";
                        }
                        }
                        else
                        {
                        string file_field_name = path_to_field_name_map[path];
                        if (val != null)
                        {
                            if
                            (
                            (
                                path_to_node_map[path].data_type != null &&
                                path_to_node_map[path].data_type.ToLower() == "number"

                            )
                            )
                            {
                            if (val.ToString() == "")
                            {
                                form_row[file_field_name] = "9999";
                            }
                            else
                            {
                                form_row[file_field_name] = val;
                            }

                            }
                            else if
                            (
                            path_to_node_map[path].data_type != null &&
                            path_to_node_map[path].data_type.ToLower() == "string" &&
                            (
                                val.ToString().Equals("9999") ||
                                val.ToString().Equals("8888") ||
                                val.ToString().Equals("7777")
                            )
                            )
                            {

                            if (val.Equals("9999"))
                            {
                                form_row[file_field_name] = "";
                            }

                            if (val.Equals("8888"))
                            {
                                form_row[file_field_name] = "Not specified";
                            }

                            if (val.Equals("7777"))
                            {
                                form_row[file_field_name] = "Unknown";
                            }

                            }
                            else
                            {
                            if (val.ToString().Equals("") || val.ToString().Equals("9999"))
                            {
                                if
                                (
                                path_to_node_map[path].data_type != null &&
                                path_to_node_map[path].data_type.ToLower() == "string"
                                )
                                {
                                form_row[file_field_name] = "";
                                }
                                else
                                {
                                form_row[file_field_name] = "9999";
                                }

                                
                            }
                            else
                            {
                                form_row[file_field_name] = val;
                            }

                            }

                        }
                        else
                        {
                            if
                            (
                            path_to_node_map[path].data_type != null &&
                            path_to_node_map[path].data_type.ToLower() == "string"

                            )
                            {
                            form_row[file_field_name] = "";
                            }
                            else
                            {
                            form_row[file_field_name] = "9999";
                            }
                        }
                        }

                        break;
                    default:
                        if (val != null)
                        {
                        string file_field_name = path_to_field_name_map[path];

                        if (path == "informant_interviews/interview_narrative")
                        {
                            // Informant Interview Narrative - Clean up, if necessary
                            string clearText = mmria.common.util.TextAreaField.CleanUp(val?.ToString());
                            if (clearText.Length > 0)
                            {
                                WriteQualitativeData
                                (
                                    mmria_case_id,
                                    HR_R_ID,
                                    path,
                                    clearText,
                                    i,
                                    -1,
                                    true
                                );
                            }
                        if
                        (
                            (
                            path_to_node_map[path].type.ToLower() == "textarea" ||
                            path_to_node_map[path].type.ToLower() == "string"
                            ) &&
                            val.ToString().Length > max_qualitative_length
                        )
                        {
                            WriteQualitativeData
                            (
                            mmria_case_id,
                            HR_R_ID,
                            path,
                            val?.ToString(),
                            i,
                            -1
                            );

                            val = over_limit_message;
                        }

                            
                            // Check the over the limit size
                            if(val?.ToString().Length > max_qualitative_length)
                            {
                                // Replace the field with over the limit notification
                                form_row[file_field_name] = "Over the qualitative limit. Check the over-the-limit folder for details.";

                                // Write to the over the limit file
                                WriteQualitativeData
                                (
                                    mmria_case_id,
                                    HR_R_ID,
                                    path,
                                    val?.ToString(),
                                    i,
                                    -1,
                                    false
                                );
                            }
                            else
                            {
                                form_row[file_field_name] = val;
                            }							
                        }
                        else
                        {
                            // If regular row, then copy field
                            form_row[file_field_name] = val;
                        }
                        }
                        break;

                    }

                }

                HashSet<string> multifom_grid_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, string> gtmfm in grid_to_multi_form_map)
                {
                    if (gtmfm.Value == kvp.Key)
                    {
                    multifom_grid_set.Add(gtmfm.Key);
                    }
                }

                process_multiform_grid
                (
                    case_doc,
                    mmria_case_id,
                    HR_R_ID,
                    i,
                    path_to_int_map,
                    path_to_node_map,
                    path_to_grid_map,
                    path_to_csv_writer,
                    multifom_grid_set
                );

                if(is_excel_file_type)
                {
                    path_to_csv_writer[kvp.Value].Table.Rows.Add(form_row);
                }
                else
                {
                    path_to_csv_writer[kvp.Value].WriteToStream(form_row);
                }
                }
            }

            // multiform - end

        }

        System.Console.WriteLine("preparing file-output 1231");
        Dictionary<string, string> int_to_path_map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


        foreach (KeyValuePair<string, int> ptn in path_to_int_map)
        {
            if (path_to_field_name_map.ContainsKey(ptn.Key))
            {
            string key = path_to_field_name_map[ptn.Key];
            if (int_to_path_map.ContainsKey(key))
            {
                int_to_path_map.Add("_" + ptn.Value.ToString("X"), ptn.Key);
            }
            else
            {
                int_to_path_map.Add(key, ptn.Key);
            }
            }
        }


        WriteCSV mapping_document = new WriteCSV("data-dictionary.csv", this.item_directory_name, Configuration.export_directory, is_excel_file_type);
        System.Data.DataColumn column = null;


        column = new System.Data.DataColumn("metadata_version", typeof(string));
        column.DefaultValue = metadata.version;
        mapping_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("file_name", typeof(string));
        mapping_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("deidentified", typeof(string));
        mapping_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("column_name", typeof(string));
        mapping_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("mmria_path", typeof(string));
        mapping_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("mmria_prompt", typeof(string));
        mapping_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("field_description", typeof(string));
        mapping_document.Table.Columns.Add(column);

        if(! is_excel_file_type)
        {
            mapping_document.WriteHeadersToStream();
        }


        foreach (KeyValuePair<string, WriteCSV> kvp in path_to_csv_writer)
        {

            foreach (System.Data.DataColumn table_column in kvp.Value.Table.Columns)
            {
                System.Data.DataRow mapping_row = mapping_document.Table.NewRow();
                mapping_row["file_name"] = kvp.Key;

                if (int_to_path_map.ContainsKey(table_column.ColumnName))
                {
                    string deidentified = "no";
                    string path = int_to_path_map[table_column.ColumnName];
                    string selection = queue_item.de_identified_selection_type;
                    if (selection == "custom" || selection == "standard")
                    {
                    if (de_identified_set.Contains(path))
                    {
                        deidentified = "yes";
                    }
                    }
                    mapping_row["deidentified"] = deidentified;
                    mapping_row["mmria_path"] = path;
                    mapping_row["mmria_prompt"] = path_to_node_map[path].prompt;
                    mapping_row["field_description"] = path_to_node_map[path].description;
                }
                else
                {
                    mapping_row["deidentified"] = "no";
                    switch (table_column.ColumnName)
                    {
                    case "_record_index":
                    case "_parent_index":
                    default:
                        mapping_row["mmria_path"] = table_column.ColumnName;
                        break;
                    }
                }

                mapping_row["column_name"] = table_column.ColumnName;

                if(is_excel_file_type)
                {
                    mapping_document.Table.Rows.Add(mapping_row);
                }
                else
                {
                    mapping_document.WriteToStream(mapping_row);
                }
            }

            if(is_excel_file_type)
            {
                kvp.Value.WriteToStream();
            }
            else
            {
                kvp.Value.FinishStream();
            }
        }

        if(is_excel_file_type)
        {
            mapping_document.WriteToStream();
        }
        else
        {
            mapping_document.FinishStream();
        }

        for (int i_index = 0; i_index < this.qualitativeStreamWriter.Length; i_index++)
        {
            if(qualitativeStreamWriter[i_index] == null) continue;

            this.qualitativeStreamWriter[i_index].Flush();
            this.qualitativeStreamWriter[i_index].Close();
            this.qualitativeStreamWriter[i_index] = null;
        }

        System.Console.WriteLine("write-csv 1338");
        WriteCSV mapping_look_up_document = new WriteCSV("data-dictionary-lookup.csv", this.item_directory_name, Configuration.export_directory, is_excel_file_type);
        column = null;


        column = new System.Data.DataColumn("metadata_version", typeof(string));
        column.DefaultValue = metadata.version;
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("file_name", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("column_name", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("mmria_path", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("mmria_prompt", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("field_description", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("item_value", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);


        column = new System.Data.DataColumn("item_display", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        column = new System.Data.DataColumn("item_description", typeof(string));
        mapping_look_up_document.Table.Columns.Add(column);

        if(! is_excel_file_type)
        {
            mapping_look_up_document.WriteHeadersToStream();
        }


        foreach (KeyValuePair<string, mmria.common.metadata.node> kvp in path_to_node_map)
        {
            var node = kvp.Value;


            if (node.type.ToLower() == "list")
            {

            try
            {
                var value_list = node.values;

                if (!string.IsNullOrWhiteSpace(node.path_reference))
                {
                var key = "/" + kvp.Key;

                if (List_Look_Up.ContainsKey(key))
                {
                    foreach (var item in List_Look_Up[key])
                    {
                        System.Data.DataRow row = mapping_look_up_document.Table.NewRow();

                        if (path_to_file_name_map.ContainsKey(kvp.Key))
                        {
                            row["file_name"] = path_to_file_name_map[kvp.Key];
                        }

                        if (path_to_field_name_map.ContainsKey(kvp.Key))
                        {
                            row["column_name"] = path_to_field_name_map[kvp.Key];
                        }

                        row["mmria_path"] = kvp.Key;
                        row["mmria_prompt"] = node.prompt;
                        row["field_description"] = node.description;
                        row["item_value"] = item.Key;
                        row["item_display"] = item.Value;
                        //row["item_description"] = item.description;
                        if(is_excel_file_type)
                        {
                            mapping_look_up_document.Table.Rows.Add(row);
                        }
                        else
                        {
                            mapping_look_up_document.WriteToStream(row);
                        }
                    }
                }

                }
                else
                {
                    foreach (var item in value_list)
                    {
                        System.Data.DataRow row = mapping_look_up_document.Table.NewRow();

                        if (path_to_file_name_map.ContainsKey(kvp.Key))
                        {
                            row["file_name"] = path_to_file_name_map[kvp.Key];
                        }

                        if (path_to_field_name_map.ContainsKey(kvp.Key))
                        {
                            row["column_name"] = path_to_field_name_map[kvp.Key];
                        }
                        row["mmria_path"] = kvp.Key;
                        row["mmria_prompt"] = node.prompt;
                        row["field_description"] = node.description;
                        row["item_value"] = item.value;
                        row["item_display"] = item.display;
                        row["item_description"] = item.description;


                        if(is_excel_file_type)
                        {
                            mapping_look_up_document.Table.Rows.Add(row);
                        }
                        else
                        {
                            mapping_look_up_document.WriteToStream(row);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            }
        }

        if(is_excel_file_type)
        {
            mapping_look_up_document.WriteToStream();
        }
        else
        {
            mapping_look_up_document.FinishStream();
        }


        mmria.server.utils.cFolderCompressor folder_compressor = new mmria.server.utils.cFolderCompressor();


        string encryption_key = null;

        if (!string.IsNullOrWhiteSpace(queue_item.zip_key))
        {
            encryption_key = queue_item.zip_key;
        }

        folder_compressor.Compress
        (
            System.IO.Path.Combine(Configuration.export_directory, this.item_file_name),
            encryption_key,// string password 
            System.IO.Path.Combine(Configuration.export_directory, this.item_directory_name)
        );


        var get_item_curl = new cURL("GET", null, db_config.url + $"/{db_config.prefix}export_queue/" + this.item_id, null, this.user_name, this.value_string);
        string responseFromServer = get_item_curl.execute();
        export_queue_item export_queue_item = Newtonsoft.Json.JsonConvert.DeserializeObject<export_queue_item>(responseFromServer);

        export_queue_item.status = "Download";


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(export_queue_item, settings);
        var set_item_curl = new cURL("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + export_queue_item._id, object_string, this.user_name, this.value_string);
        responseFromServer = set_item_curl.execute();


        Console.WriteLine("{0} Export Finished", System.DateTime.Now);



        return true;

        }
        catch (Exception ex)
        {

        var get_item_curl = new cURL("GET", null, db_config.url + $"/{db_config.prefix}export_queue/" + this.item_id, null, this.user_name, this.value_string);
        string responseFromServer = get_item_curl.execute();
        export_queue_item export_queue_item = Newtonsoft.Json.JsonConvert.DeserializeObject<export_queue_item>(responseFromServer);

        export_queue_item.status = "Queue Failed:" + ex.ToString();

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(export_queue_item, settings);
        var set_item_curl = new cURL("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + export_queue_item._id, object_string, this.user_name, this.value_string);
        responseFromServer = set_item_curl.execute();


        return false;
        }
    }

        private List<string> SortListAgainstDictionary(List<string> temp2, Dictionary<string, string> look_up_list)
        {
            var result = new List<string>();
            var itemsNotInDictionary = new List<string>();

            if (temp2 != null)
            {
                foreach (var item in look_up_list)
                {
                    if (temp2.Contains(item.Value))
                    {
                        if(item.Key == "9999")
                        {
                            if (look_up_list.Count == 1) result.Add(item.Value);
                        }
                        else
                        {
                            result.Add(item.Value);
                        }

                        
                    }
                }

                itemsNotInDictionary = temp2?.Except(result)?.ToList();

                if (itemsNotInDictionary != null && itemsNotInDictionary.Count > 0)
                    result.AddRange(itemsNotInDictionary);

            }
            return result;
        }

    private void Get_List_Look_Up
    (
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> p_result,
        mmria.common.metadata.node[] p_lookup,
        mmria.common.metadata.node p_metadata,
        Dictionary<string, int> p_path_to_int_map,
        string p_path
    )
    {
        switch (p_metadata.type.ToLower())
        {
        case "form":
        case "group":
        case "grid":
            foreach (mmria.common.metadata.node node in p_metadata.children)
            {
            Get_List_Look_Up(p_result, p_lookup, node, p_path_to_int_map, p_path + "/" + node.name.ToLower());
            }
            break;
        case "list":
            if
            (
            p_metadata.control_style != null &&
            p_metadata.control_style.ToLower() == "editable"
            )
            {
            break;
            }

            p_result.Add(p_path, new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            var value_node_list = p_metadata.values;
            if
            (
            !string.IsNullOrWhiteSpace(p_metadata.path_reference)
            )
            {
            var name = p_metadata.path_reference.Replace("lookup/", "");
            foreach (var item in p_lookup)
            {
                if (item.name.ToLower() == name.ToLower())
                {
                value_node_list = item.values;
                break;
                }
            }
            }

            foreach (var value in value_node_list)
            {
            p_result[p_path].Add(value.value, value.display);
            }

            break;
        default:
            break;
        }
    }


    private void process_multiform_grid
    (
        IDictionary<string, object> case_doc,
        string mmria_case_id,
        string HR_R_ID,
        int parent_record_index,
        Dictionary<string, int> path_to_int_map,
        Dictionary<string, mmria.common.metadata.node> path_to_node_map,
        Dictionary<string, string> path_to_grid_map,
        Dictionary<string, WriteCSV> path_to_csv_writer,
        HashSet<string> flat_grid_set
    )
    {

        foreach (KeyValuePair<string, string> ptn in path_to_grid_map)
        {
        string path = ptn.Key;
        if (flat_grid_set.Contains(path))
        {
            string grid_name = path_to_grid_map[path];

            HashSet<string> grid_field_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, mmria.common.metadata.node> ptgm in path_to_node_map.Where(x => x.Key.StartsWith(path) && x.Key != path))
            {
            grid_field_set.Add(ptgm.Key);
            }

            create_header_row
            (
            path_to_int_map,
            grid_field_set,
            path_to_node_map,
            path_to_csv_writer[grid_name].Table,
            true,
            true,
            true
            );

            if(! is_header_written.ContainsKey(grid_name))
            {
                is_header_written.Add(grid_name, true);
                path_to_csv_writer[grid_name].WriteHeadersToStream();
            }

            object raw_data = get_multiform_grid(case_doc as IDictionary<string, object>, string.Join("/", path), true);
            List<object> raw_data_list = raw_data as List<object>;
            if(raw_data_list== null) return;
            if(parent_record_index > raw_data_list.Count) return;

            List<object> grid_raw_data = raw_data_list[parent_record_index] as List<object>;

            if (grid_raw_data == null) return;

            for (int i = 0; i < grid_raw_data.Count; i++)
            {
                IDictionary<string, object> grid_item_row = grid_raw_data[i] as IDictionary<string, object>;

                if (grid_item_row == null)
                {
                continue;
                }

                System.Data.DataRow grid_row = path_to_csv_writer[grid_name].Table.NewRow();
                grid_row["hr_r_id"] = HR_R_ID;
                grid_row["_id"] = mmria_case_id;
                grid_row["_record_index"] = i;
                grid_row["_parent_record_index"] = parent_record_index;
                foreach (KeyValuePair<string, string> kvp in path_to_grid_map.Where(k => k.Value == grid_name))
                {
                    foreach (string field_node in grid_field_set)
                    {
                        try
                        {

                            var key_name = field_node.Substring(field_node.LastIndexOf("/") + 1, field_node.Length - field_node.LastIndexOf("/") - 1);

                            object grid_item_value = null;
                            
                            if (grid_item_row.ContainsKey(key_name))
                                grid_item_value = grid_item_row[key_name];



                            if (de_identified_set.Contains(field_node))
                            {
                                grid_item_value = null;
                            }

                            string file_field_name = path_to_field_name_map[field_node];
                            if (grid_item_value != null)
                            {


                                if 
                                (
                                    path_to_node_map[field_node].type.ToLower() == "number" && 
                                    !string.IsNullOrWhiteSpace(grid_item_value.ToString())
                                )
                                {
                                    grid_row[file_field_name] = grid_item_value;
                                }
                                else if (path_to_node_map[field_node].type.ToLower() == "list")
                                {
                                if
                                (
                                    (
                                    path_to_node_map[field_node].is_multiselect != null &&
                                    path_to_node_map[field_node].is_multiselect == true
                                    ) ||
                                    grid_item_value is List<object>
                                )
                                {
                                    List<object> temp = grid_item_value as List<object>;
                                    if (temp != null && temp.Count > i)
                                    {
                                    var temp_grid_row = temp[i];
                                    var item_dictionary_key = path_to_field_name_map[field_node];
                                    IDictionary<string, object> item_dictionary = temp[i] as IDictionary<string, object>;
                                    if (item_dictionary != null && item_dictionary.ContainsKey(item_dictionary_key))
                                    {
                                        var temp2 = item_dictionary[item_dictionary_key] as List<object>;
                                        grid_row[file_field_name] = string.Join("|", temp2);
                                    }
                                    }
                                    if
                                    (
                                        path_to_node_map[field_node].data_type != null &&
                                        path_to_node_map[field_node].data_type.ToLower() == "string" &&
                                        (
                                            grid_item_value.Equals("9999") ||
                                            grid_item_value.Equals("8888") ||
                                            grid_item_value.Equals("7777")
                                        )
                                    )
                                    {

                                    if (grid_item_value.Equals("9999"))
                                    {
                                        grid_row[file_field_name] = "";
                                    }

                                    if (grid_item_value.Equals("8888"))
                                    {
                                        grid_row[file_field_name] = "Not specified";
                                    }

                                    if (grid_item_value.Equals("7777"))
                                    {
                                        grid_row[file_field_name] = "Unknown";
                                    }

                                    }
                                    else
                                    {
                                    if (grid_item_value.Equals(""))
                                    {

                                        if
                                        (
                                        path_to_node_map[field_node].data_type != null &&
                                        path_to_node_map[field_node].data_type.ToLower() == "string"
                                        )
                                        {
                                        grid_row[file_field_name] = "";
                                        }
                                        else
                                        {
                                        grid_row[file_field_name] = "9999";
                                        }
                                        grid_row[file_field_name] = "9999";
                                    }
                                    else
                                    {
                                        grid_row[file_field_name] = grid_item_value;
                                    }

                                    }
                                }
                                else
                                {

                                    if (grid_item_value != null)
                                    {
                                        if (grid_item_value is List<object>)
                                        {
                                            List<object> temp = grid_item_value as List<object>;
                                            if (temp != null && temp.Count > 0)
                                            {
                                            List<string> temp2 = new List<string>();
                                            foreach (var item in temp)
                                            {
                                                var key = "/" + field_node;
                                                var item_key = item.ToString();
                                                if (List_Look_Up.ContainsKey(key) && List_Look_Up[key].ContainsKey(item_key))
                                                {
                                                temp2.Add(List_Look_Up["/" + field_node][item.ToString()]);
                                                }
                                                else
                                                {
                                                temp2.Add(item.ToString());
                                                }
                                            }


                                            grid_row[file_field_name] = string.Join("|", temp2);

                                            }
                                            else
                                            {
                                                grid_row[file_field_name] = "(blank)";
                                            }
                                        }
                                        else
                                        {
                                            if (grid_item_value == "")
                                            {

                                            if
                                            (
                                                path_to_node_map[field_node].data_type != null &&
                                                path_to_node_map[field_node].data_type.ToLower() == "string"
                                            )
                                            {
                                                grid_row[file_field_name] = "";
                                            }
                                            else
                                            {
                                                grid_row[file_field_name] = "9999";
                                            }
                                            grid_row[file_field_name] = "9999";
                                            }
                                            else
                                            {
                                            grid_row[file_field_name] = grid_item_value;
                                            }

                                        }
                                    }
                                    else
                                    {
                                    if
                                    (
                                        path_to_node_map[field_node].data_type != null &&
                                        path_to_node_map[field_node].data_type.ToLower() == "string"

                                    )
                                    {
                                        grid_row[file_field_name] = "";
                                    }
                                    else
                                    {
                                        grid_row[file_field_name] = "9999";
                                    }
                                    }
                                }

                                }
                                else
                                {
                                if
                                (
                                    (
                                    path_to_node_map[field_node].type.ToLower() == "textarea" ||
                                    path_to_node_map[field_node].type.ToLower() == "string"
                                    ) &&
                                    grid_item_value.ToString().Length > max_qualitative_length
                                )
                                {
                                    WriteQualitativeData
                                    (
                                    mmria_case_id,
                                    HR_R_ID,
                                    field_node,
                                    grid_item_value?.ToString(),
                                    i,
                                    parent_record_index
                                    );
                                    grid_item_value = over_limit_message;
                                }


                                switch (grid_item_value)
                                {

                                    case System.DateTime val:
                                    grid_row[file_field_name] = val.ToString("o");
                                    break;
                                    case System.String val:
                                    if (!string.IsNullOrWhiteSpace(val))
                                    {
                                        grid_row[file_field_name] = val;
                                    }
                                    break;
                                    case System.Double val:
                                    default:
                                    if (!string.IsNullOrWhiteSpace(grid_item_value?.ToString()))
                                    {
                                        grid_row[file_field_name] = grid_item_value;
                                    }
                                    break;
                                }
                                }
                            }
                            else
                            {
                                if (path_to_node_map[field_node].type.ToLower() == "list")
                                {
                                if
                                (
                                    path_to_node_map[field_node].data_type != null &&
                                    path_to_node_map[field_node].data_type.ToLower() == "string"

                                )
                                {
                                    grid_row[file_field_name] = "";
                                }
                                else
                                {
                                    grid_row[file_field_name] = "9999";
                                }
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                if(is_excel_file_type)
                {
                    path_to_csv_writer[grid_name].Table.Rows.Add(grid_row);
                }
                else
                {
                    path_to_csv_writer[grid_name].WriteToStream(grid_row);
                }
            }
        }
        }

    }


    private void create_header_row
    (
        System.Collections.Generic.Dictionary<string, int> p_path_to_int_map,
        System.Collections.Generic.HashSet<string> p_path_to_csv_set,
        System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> p_path_to_node_map,
        System.Data.DataTable p_Table,
        bool p_add_id,
        bool p_add_record_index,
        bool p_add_parent_record_index
    )
    {
        if (p_Table.Columns.Count > 0)
        {
        return;
        }

        System.Data.DataColumn column = null;
        // create header row
        if (p_add_id)
        {
            column = new System.Data.DataColumn("hr_r_id", typeof(string));
            p_Table.Columns.Add(column);

            column = new System.Data.DataColumn("_id", typeof(string));
            p_Table.Columns.Add(column);
        }

        if (p_add_record_index)
        {
        column = new System.Data.DataColumn("_record_index", typeof(long));
        p_Table.Columns.Add(column);
        }

        if (p_add_parent_record_index)
        {
        column = new System.Data.DataColumn("_parent_record_index", typeof(long));
        p_Table.Columns.Add(column);
        }


        foreach (string path in p_path_to_csv_set)
        {

            if(path == "home_record/record_id") continue;
        string file_field_name = convert_path_to_field_name(path, p_path_to_int_map);

        switch (p_path_to_node_map[path].type.ToLower())
        {
            case "app":
            case "form":
            case "grid":
            case "always_enabled_button":
            case "button":
            case "chart":
            case "label":
            continue;
            case "group":
                if
                (
                    p_path_to_node_map[path].tags ==null ||
                    p_path_to_node_map[path].tags.Length == 0 ||
                    !p_path_to_node_map[path].tags[0].Equals("CALC_DATE", StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }
                else
                {
                    column = new System.Data.DataColumn(file_field_name, typeof(string));
                }
                break;
            case "number":

            if (p_path_to_node_map[path].mirror_reference == null)
            {
                column = new System.Data.DataColumn(file_field_name, typeof(double));
            }
            else
            {
                continue;
            }
            break;
            default:
            if (p_path_to_node_map[path].mirror_reference == null)
            {
                column = new System.Data.DataColumn(file_field_name, typeof(string));
            }
            else
            {
                continue;
            }
            break;

        }

        try
        {
            p_Table.Columns.Add(column);
        }
        catch (Exception)
        {
            column.ColumnName = $"{file_field_name}_{p_path_to_int_map[path].ToString()}";
            p_Table.Columns.Add(column);
        }

        }
    }


    private void generate_path_map
    (

        mmria.common.metadata.app p_metadata, string p_path,
        string p_file_name,
        string p_form_path,
        System.Collections.Generic.Dictionary<string, int> p_path_to_int_map,
        System.Collections.Generic.Dictionary<string, string> p_path_to_file_name_map,
        System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> p_path_to_node_map,
        System.Collections.Generic.Dictionary<string, string> p_path_to_grid_map,
        System.Collections.Generic.Dictionary<string, string> p_path_to_multi_form_map,
        bool p_is_multiform_context,
        System.Collections.Generic.Dictionary<string, string> p_multi_form_to_grid_map,
        bool p_is_grid_context,
        System.Collections.Generic.HashSet<string> p_path_to_flat_map

    )
    {
        p_path_to_int_map.Add(p_path, p_path_to_int_map.Count);



        if (p_metadata.children != null)
        {
        IList<mmria.common.metadata.node> children = p_metadata.children as IList<mmria.common.metadata.node>;

        if (children != null)
            for (var i = 0; i < children.Count; i++)
            {
            var child = children[i];

            generate_path_map(child, child.name, p_file_name, p_form_path, p_path_to_int_map, p_path_to_file_name_map, p_path_to_node_map, p_path_to_grid_map, p_path_to_multi_form_map, false, p_multi_form_to_grid_map, false, p_path_to_flat_map);
            }
        }
    }

    private string convert_path_to_field_name(string p_path, Dictionary<string, int> p_path_to_int_map)
    {
        string result_value = null;
        //		/birth_certificate_infant_fetal_section / causes_of_death
        // /birth_certificate_infant_fetal_section
        bool is_added_item = false;
        int form_prefix = 0;
        int field_prefix = 0;

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        string[] temp = p_path.Split('/');

        for (int i = 0; i < temp.Length; i++)
        {
        string[] temp2 = temp[i].Split('_');
        for (int j = 0; j < temp2.Length; j++)
        {
            if (i == 0 && form_prefix < 3 && i != temp.Length - 1)
            {
            if (!string.IsNullOrWhiteSpace(temp2[j]))
            {
                result.Append(temp2[j][0]);
                is_added_item = true;
            }
            form_prefix++;
            }
            else if (i == temp.Length - 1)
            {
            if (j == 0 && result.Length != 0 && j < temp2.Length - 1)
            {
                result.Append("_");
                if (!string.IsNullOrWhiteSpace(temp2[j]))
                {
                result.Append(temp2[j][0]);
                is_added_item = true;
                }
            }
            else if (j < temp2.Length - 1)
            {
                if (!string.IsNullOrWhiteSpace(temp2[j]))
                {
                result.Append(temp2[j][0]);
                is_added_item = true;
                }
            }
            else
            {
                result.Append("_");
                if (temp2[j].Length > 5)
                {
                result.Append(temp2[j].Substring(0, 5));
                }
                else
                {
                result.Append(temp2[j]);
                }

            }



            }
            else if (!string.IsNullOrWhiteSpace(temp2[j]))
            {
            result.Append(temp2[j][0]);
            is_added_item = true;
            }
        }
        }

        result_value = result.ToString();
        var test_result = get_sass_name(this.current_metadata, "/" + p_path, "");
        if (test_result != null)
        {
        result_value = test_result;
        }

        if (path_to_field_name_map.ContainsValue(result_value))
        {
        result_value = result_value + "_" + p_path_to_int_map[p_path];
        }

        path_to_field_name_map.Add(p_path, result_value);
        return result_value;
    }


    private string get_sass_name(mmria.common.metadata.app p_metadata, string p_search_path, string p_path)
    {
        string result = null;
        for (var i = 0; i < p_metadata.children.Length; i++)
        {
        var child = p_metadata.children[i];
        result = get_sass_name(child, p_search_path, p_path + "/" + child.name);
        if (result != null)
        {
            break;
        }
        }

        return result;
    }
    private string get_sass_name(mmria.common.metadata.node p_metadata, string p_search_path, string p_path)
    {
        string result = null;
        switch (p_metadata.type.ToLower())
        {
        case "app":
        case "form":
        case "group":
        case "grid":
            if
            (
                p_metadata.sass_export_name != null &&
                p_metadata.sass_export_name != "" &&
                p_search_path == p_path
            )
            {
                result = p_metadata.sass_export_name;
            }
            else for (var i = 0; i < p_metadata.children.Length; i++)
            {
            var child = p_metadata.children[i];
            result = get_sass_name(child, p_search_path, p_path + "/" + child.name);
            if (result != null)
            {
                break;
            }
            }
            break;
        default:
            if
            (
            p_metadata.sass_export_name != null &&
            p_metadata.sass_export_name != "" &&
            p_search_path == p_path
            )
            {
            result = p_metadata.sass_export_name;
            }
            break;
        }
        return result;

    }

    private void generate_path_map
    (
        mmria.common.metadata.node p_metadata,
        string p_path,
        string p_file_name,
        string p_form_path,
        System.Collections.Generic.Dictionary<string, int> p_path_to_int_map,
        System.Collections.Generic.Dictionary<string, string> p_path_to_file_name_map,
        System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> p_path_to_node_map,
        System.Collections.Generic.Dictionary<string, string> p_path_to_grid_map,
        System.Collections.Generic.Dictionary<string, string> p_path_to_multi_form_map,
        bool p_is_multiform_context,
        System.Collections.Generic.Dictionary<string, string> p_multi_form_to_grid_map,
        bool p_is_grid_context,
        System.Collections.Generic.HashSet<string> p_path_to_flat_map
    )
    {

        bool is_flat_map = true;
        bool is_grid = false;
        bool is_multiform = false;

        string file_name = p_file_name;
        string form_path = p_form_path;

        p_path_to_int_map.Add(p_path, p_path_to_int_map.Count);
        p_path_to_node_map.Add(p_path, p_metadata);



        if (p_metadata.type.ToLower() == "grid")
        {
        is_flat_map = false;
        is_grid = true;

        file_name = this.convert_path_to_file_name(p_path);

        p_path_to_grid_map.Add(p_path, file_name);

        if (p_is_multiform_context)
        {
            p_multi_form_to_grid_map.Add(p_path, form_path);
        }

        }
        else
        {
        is_grid = p_is_grid_context;
        }

        if (p_metadata.type.ToLower() == "form" && (p_metadata.cardinality == "*" || p_metadata.cardinality == "+"))
        {

        is_flat_map = false;
        file_name = this.convert_path_to_file_name(p_path);
        form_path = p_path;
        is_multiform = true;
        p_path_to_multi_form_map.Add(p_path, file_name);
        }
        else
        {
        is_multiform = p_is_multiform_context;
        }

        if (is_flat_map && !(is_multiform || is_grid || p_is_grid_context || p_is_multiform_context))
        {
        p_path_to_flat_map.Add(p_path);
        }

        p_path_to_file_name_map.Add(p_path, file_name);

        if (p_metadata.children != null)
        {
        IList<mmria.common.metadata.node> children = p_metadata.children as IList<mmria.common.metadata.node>;
        if (children != null)
            for (var i = 0; i < children.Count; i++)
            {
            var child = children[i];

            generate_path_map(child, p_path + "/" + child.name, file_name, form_path, p_path_to_int_map, p_path_to_file_name_map, p_path_to_node_map, p_path_to_grid_map, p_path_to_multi_form_map, is_multiform, p_multi_form_to_grid_map, is_grid, p_path_to_flat_map);
            }
        }
    }

    private string convert_path_to_file_name(string p_path)
    {
        bool is_added_item = false;

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        string[] temp = p_path.Split('/');
        for (int i = 0; i < temp.Length - 1; i++)
        {
        string[] temp2 = temp[i].Split('_');
        for (int j = 0; j < temp2.Length; j++)
        {
            if (!string.IsNullOrWhiteSpace(temp2[j]))
            {
            result.Append(temp2[j][0]);
            is_added_item = true;
            }
        }
        }

        if (is_added_item)
        {
        result.Append("_");
        }
        result.Append(temp[temp.Length - 1]);

        string value = result.ToString();
        if (value.Length > 32)
        {
        value = value.Substring(value.Length - 32, 32);
        }

        return value + ".csv";
    }


    private void process_case_row
    (
        System.Collections.Generic.Dictionary<string, string> p_path_to_file_name_map,
        System.Collections.Generic.Dictionary<string, WriteCSV> p_path_to_csv_writer,
        System.Dynamic.ExpandoObject case_row, string p_path)
    {
        foreach (KeyValuePair<string, object> kvp in case_row)
        {
        if (kvp.Value is IList<object>)
        {
        }
        else if (kvp.Value is IDictionary<string, object>)
        {

        }
        else
        {

        }
        }
    }

    public object get_value(IDictionary<string, object> p_object, string p_path, bool p_is_grid = false)
    {
        object result = null;
        
        var de_identified_path = System.Text.RegularExpressions.Regex.Replace(p_path, "/[0-9]/", "/");

        if (de_identified_set.Contains(de_identified_path))
        {
        return result;
        }

        try
        {
        string[] path = p_path.Split('/');

        System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

        object index = p_object;


        for (int i = 0; i < path.Length; i++)
        {
            if (i == path.Length - 1)
            {
            if (index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
            {
                result = ((IDictionary<string, object>)index)[path[i]];
            }
            else if (p_is_grid)
            {
                result = index;
            }
            else
            {

            }

            }
            else if (number_regex.IsMatch(path[i]))
            {
            int temp_index = int.Parse(path[i]);
            IList<object> temp_list = index as IList<object>;

            if
            (
                temp_list != null &&
                (temp_list.Count > temp_index)
            )
            {
                index = temp_list[temp_index] as IDictionary<string, object>;
            }

            }
            else if (index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
            {

            switch (((IDictionary<string, object>)index)[path[i]])
            {
                case IList<object> val:
                index = val;
                break;
                case IDictionary<string, object> val:
                index = val;
                break;
                default:
                System.Console.WriteLine("check this");
                //index = value_string;
                break;
            }

            }
            else
            {
            //System.Console.WriteLine("This should not happen. {0}", p_path);
            }
        }
        }
        catch (Exception)
        {
        //System.Console.WriteLine("case_maker.set_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }


    public List<object> get_multiform_grid(IDictionary<string, object> p_object, string p_path, bool p_is_grid = false)
    {
        List<object> result = new List<object>();

        if (de_identified_set.Contains(p_path))
        {
        return result;
        }

        try
        {
        string[] path = p_path.Split('/');

        System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

        List<object> multiform = p_object[path[0]] as List<object>;

        if (multiform != null)
        {
            for (int form_index = 0; form_index < multiform.Count; form_index++)
            {
            object index = multiform[form_index];

            for (int i = 1; i < path.Length; i++)
            {
                if (i == path.Length - 1)
                {
                if (index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                {
                    result.Add(((IDictionary<string, object>)index)[path[i]]);
                }
                else if (p_is_grid)
                {
                    result.Add(index);
                }
                else
                {

                }

                }
                else if (number_regex.IsMatch(path[i]))
                {
                int temp_index = int.Parse(path[i]);
                IList<object> temp_list = index as IList<object>;

                if
                (
                    temp_list != null &&
                    (temp_list.Count > temp_index)
                )
                {
                    index = temp_list[temp_index] as IDictionary<string, object>;
                }

                }
                else if (index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                {

                switch (((IDictionary<string, object>)index)[path[i]])
                {
                    case IList<object> val:
                    index = val;
                    break;
                    case IDictionary<string, object> val:
                    index = val;
                    break;
                    default:
                    index = value_string;
                    break;
                }

                }
                else
                {
                //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
            }
        }
        }
        catch (Exception)
        {
        //System.Console.WriteLine("case_maker.set_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }


    private void WriteQualitativeData
    (
        string p_id, 
        string p_record_id, 
        string p_mmria_path, 
        string p_data, 
        int p_index, 
        int p_parent_index,
        bool isClearText=false
    )
    {
        const string record_split = "************************************************************";
        const string header_split = "\n\n";

        int index = 0;

        switch (p_mmria_path.Trim().ToLower())
        {
            case "case_narrative/case_opening_overview":
                index = (isClearText) ? 3 : 1;
                break;
            case "informant_interviews/interview_narrative":
                index = (isClearText) ? 4 : 2;
                break;
            default:
                index = 0;
                break;
        }


        if (this.qualitativeStreamCount[index] == 0)
        {
            this.qualitativeStreamWriter[index].WriteLine($"{record_split}\nhr_r_id={p_record_id}\nid={p_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n{p_data}");
        }
        else
        {
            this.qualitativeStreamWriter[index].WriteLine($"\n{record_split}\nhr_r_id={p_record_id}\nid={p_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n{p_data}");
        }
        this.qualitativeStreamCount[index] += 1;
    }
    int? ConvertToInt(object value)
    {
        int? result = null;
        int try_int = -1;
        if
        (
            value != null
        )
        {
            if(int.TryParse(value.ToString(), out try_int))
            {
                if(try_int != 9999)
                {
                    result = try_int;
                }
            }
        }

        return result;
    }
}
