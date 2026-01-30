using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Microsoft.Extensions.Configuration;
using mmria.getset;

namespace mmria.services.Utilities.CoreElementExport;

public sealed class core_element_exporter
{
    private string auth_token = null;
    private string user_name = null;
    private string value_string = null;
    private string database_path = null;
    private string database_url = null;
    private string item_file_name = null;
    private string item_directory_name = null;
    private string item_id = null;
    private bool is_offline_mode;

    private bool is_excel_file_type = false;


    private System.IO.StreamWriter[] qualitativeStreamWriter = new System.IO.StreamWriter[3];
    private int[] qualitativeStreamCount = new int[] { 0, 0, 0 };
    private const int max_qualitative_length = 31000;

    private const string over_limit_message = "Over the qualitative limit. Check the over-the-limit.txt folder for details.";

    private string juris_user_name = null;

    private mmria.server.model.actor.ScheduleInfoMessage Configuration;

    private List<string> Core_Element_Paths;


    private HashSet<string> de_identified_set;

    System.Collections.Generic.Dictionary<string, string> path_to_field_name_map = new Dictionary<string, string>();

    common.metadata.app current_metadata;

    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> List_Look_Up;

    mmria.common.couchdb.DBConfigurationDetail db_config;
    public core_element_exporter(mmria.server.model.actor.ScheduleInfoMessage configuration)
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
public void Execute(export_queue_item queue_item)
{

    this.database_path = this.Configuration.couch_db_url;
    this.juris_user_name = this.Configuration.jurisdiction_user_name;
    this.user_name = this.Configuration.user_name;
    this.value_string = this.Configuration.user_value;

    this.item_file_name = queue_item.file_name;
    this.item_directory_name = queue_item.file_name.Substring(0, queue_item.file_name.IndexOf("."));
    this.item_id = queue_item._id;

    this.is_excel_file_type = queue_item.case_file_type == "xlsx" ? true : false;

    bool is_header_written = false;


    string core_file_name = "core_mmria_export.csv";

    if (string.IsNullOrWhiteSpace(db_config.url))
    {
        if (string.IsNullOrWhiteSpace(db_config.url))
        {
            System.Console.WriteLine("missing database_url");
            System.Console.WriteLine(" form database:[file path]");
            System.Console.WriteLine(" example database:http://localhost:5984");
            System.Console.WriteLine(" mmria.exe export user_name:user1 password:secret url:http://localhost:12345 database_url:http://localhost:5984");
            return;
        }
    }

    if (string.IsNullOrWhiteSpace(this.user_name))
    {
        System.Console.WriteLine("missing user_name");
        System.Console.WriteLine(" form user_name:[user_name]");
        System.Console.WriteLine(" example user_name:user1");
        System.Console.WriteLine(" mmria.exe export user_name:user1 password:secret url:http://localhost:12345");
        return;
    }

    if (string.IsNullOrWhiteSpace(this.value_string))
    {
        System.Console.WriteLine("missing password");
        System.Console.WriteLine(" form password:[password]");
        System.Console.WriteLine(" example password:secret");
        System.Console.WriteLine(" mmria.exe export user_name:user1 password:secret url:http://localhost:12345");
        return;
    }

    // Save the home directory so we can put the case-narrative-plaintext-all.txt in the main directory
    string export_directory = System.IO.Path.Combine(Configuration.export_directory, this.item_directory_name, "over-the-limit");

    if (!System.IO.Directory.Exists(export_directory))
    {
        System.IO.Directory.CreateDirectory(export_directory);
    }

    this.qualitativeStreamWriter[0] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "over-the-qualitative-limit.txt"), true);
    this.qualitativeStreamWriter[1] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "case-narrative.txt"), true);
    this.qualitativeStreamWriter[2] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "informant-interview.txt"), true);

    string metadata_url = db_config.url + $"/metadata/version_specification-{this.Configuration.version_number}/metadata";
    cURL metadata_curl = new cURL("GET", null, metadata_url, null, this.user_name, this.value_string);
    mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());
    current_metadata = metadata;


    System.Collections.Generic.Dictionary<string, int> path_to_int_map = new Dictionary<string, int>();
    System.Collections.Generic.Dictionary<string, string> path_to_file_name_map = new Dictionary<string, string>();
    System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> path_to_node_map = new Dictionary<string, mmria.common.metadata.node>();
    System.Collections.Generic.Dictionary<string, string> path_to_grid_map = new Dictionary<string, string>();
    System.Collections.Generic.Dictionary<string, string> path_to_multi_form_map = new Dictionary<string, string>();
    System.Collections.Generic.Dictionary<string, string> grid_to_multi_form_map = new Dictionary<string, string>();

    System.Collections.Generic.HashSet<string> path_to_flat_map = new System.Collections.Generic.HashSet<string>();

    System.Collections.Generic.Dictionary<string, WriteCSV> path_to_csv_writer = new Dictionary<string, WriteCSV>();

    get_core_element_list(metadata);

    generate_path_map
    (
        metadata, "", core_file_name, "",
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
        if 
        (
            !mutiform_grid_set.Contains(kvp.Value) &&
            !flat_grid_set.Contains(kvp.Value)
        )
        {
            flat_grid_set.Add(kvp.Value);
        }
    }

    path_to_csv_writer.Add(core_file_name, new WriteCSV(core_file_name, this.item_directory_name, Configuration.export_directory, is_excel_file_type));

    create_header_row
    (
        path_to_int_map,
        path_to_flat_map,
        path_to_node_map,
        path_to_csv_writer[core_file_name].Table,
        true,
        false,
        false
    );

    var grantee_column = new System.Data.DataColumn("export_jurisdiction_name", typeof(string));

    grantee_column.DefaultValue = queue_item.grantee_name;
    path_to_csv_writer[core_file_name].Table.Columns.Add(grantee_column);

    cURL de_identified_list_curl = new cURL("GET", null, db_config.url + "/metadata/de-identified-list", null, this.user_name, this.value_string);
    System.Dynamic.ExpandoObject de_identified_ExpandoObject = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_list_curl.execute());
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


    if(Custom_Case_Id_List.Count == 0)
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

    foreach(string case_id in Custom_Case_Id_List)
    {

        string URL = $"{db_config.url}/{db_config.prefix}mmrds/{case_id}";
        cURL document_curl = new cURL("GET", null, URL, null, this.user_name, this.value_string);
        System.Dynamic.ExpandoObject case_row = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_curl.execute());

        IDictionary<string, object> case_doc;

        case_doc = case_row as IDictionary<string, object>;
        
        if
        (
            case_doc == null ||
            !case_doc.ContainsKey("_id") ||
            case_doc["_id"].ToString().StartsWith("_design", StringComparison.InvariantCultureIgnoreCase)

        )
        {
            continue;
        }

        var is_jurisdiction_ok = false;

        var home_record = case_doc["home_record"] as IDictionary<string, object>;

        if (home_record != null)
        {
            if (!home_record.ContainsKey("jurisdiction_id"))
            {
                home_record.Add("jurisdiction_id", "/");
            }

            foreach (var jurisdiction_item in jurisdiction_hashset)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction_id);

                #if !IS_PMSS_ENHANCED
                if (regex.IsMatch(home_record["jurisdiction_id"].ToString()) && jurisdiction_item.ResourceRight == mmria.services.ResourceRightEnum.ReadCase)
                {
                    is_jurisdiction_ok = true;
                    break;
                }
                #endif
                #if IS_PMSS_ENHANCED
                if (regex.IsMatch(home_record["jurisdiction_id"].ToString()) && jurisdiction_item.ResourceRight == mmria.pmss.server.utils.ResourceRightEnum.ReadCase)
                {
                    is_jurisdiction_ok = true;
                    break;
                }
                #endif
            }
        }

        if (!is_jurisdiction_ok)
        {
            continue;
        }

        

        System.Data.DataRow row = path_to_csv_writer[core_file_name].Table.NewRow();
        string mmria_case_id = case_doc["_id"].ToString();
        row["_id"] = mmria_case_id;

        List<string> ordered_column_list = this.Core_Element_Paths;

        foreach (string path in ordered_column_list)
        {
            if 
            (
                !path_to_node_map.ContainsKey(path) ||
                !path_to_field_name_map.ContainsKey(path) ||
                !row.Table.Columns.Contains(path_to_field_name_map[path]) ||
                path_to_node_map[path].type.ToLower() == "app" ||
                path_to_node_map[path].type.ToLower() == "form" ||
                path_to_node_map[path].type.ToLower() == "group" ||
                path_to_node_map[path].mirror_reference != null
            )
            {
                continue;
            }

            object val = get_value(case_doc as IDictionary<string, object>, path);

            try
            {

                var field_name = path_to_field_name_map[path];
                switch (path_to_node_map[path].type.ToLower())
                {

                    case "number":
                    if (val != null && (!string.IsNullOrWhiteSpace(val.ToString())))
                    {
                        double test_double = 0.0;
                        if (double.TryParse(val.ToString(), out test_double))
                        {
                            row[field_name] = test_double;
                        }
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

                            //Check if list can be sorted and list lookup has key
                            if (temp2?.Count > 1 && List_Look_Up.ContainsKey("/" + path))
                            {
                                //Get list lookup
                                var look_up_list = List_Look_Up["/" + path];

                                //Set sorted list back to the value to contiune regular flow
                                temp2 = SortListAgainstDictionary(temp2, look_up_list);
                            }

                            row[field_name] = string.Join("|", temp2);
                        }
                        else
                        {
                            row[field_name] = "(blank)";
                        }
                    }
                    else
                    {
                        if (val != null)
                        {
                        row[field_name] = val;
                        }
                    }

                    break;
                    //case "date":
                    case "datetime":
                    case "time":
                        if (val != null)
                        {
                            row[field_name] = val;
                        }
                    break;
                    default:
                    if (val != null)
                    {
                        if(path=="host_state")
                        {
                            val = val.ToString().ToUpper();
                        }

                        if (val is List<object>)
                        {
                            List<object> temp = val as List<object>;
                            if (temp != null && temp.Count > 0)
                            {

                                List<string> temp2 = new List<string>();
                                foreach (var item in temp)
                                {
                                    temp2.Add(List_Look_Up["/" + path][item.ToString()]);
                                }
                                row[field_name] = string.Join("|", temp);
                            }
                            else
                            {
                                row[field_name] = "(blank)";
                            }
                        }
                        else
                        {
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
                                    path,
                                    val?.ToString(),
                                    -1,
                                    -1
                                );

                                val = over_limit_message;
                            }

                            row[field_name] = val;
                        }

                    }
                    break;
                }
            }
            catch (Exception)
            {

            }
        }


        if(is_excel_file_type)
        {        
            path_to_csv_writer[core_file_name].Table.Rows.Add(row);
        }
        else
        {
            if(! is_header_written)
            {
                path_to_csv_writer[core_file_name].WriteHeadersToStream();
                is_header_written = true;
            }

            path_to_csv_writer[core_file_name].WriteToStream(row);
        }


    }


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
                mapping_document.WriteHeadersToStream();
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

    for 
    (
        int i_index = 0; 
        i_index < this.qualitativeStreamWriter.Length; 
        i_index++
    )
    {
        this.qualitativeStreamWriter[i_index].Flush();
        this.qualitativeStreamWriter[i_index].Close();
        this.qualitativeStreamWriter[i_index] = null;
    }

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



    var is_mapping_lookup_header_written = false;
    foreach 
    (
        var kvp in 
        path_to_node_map
    )
    {
        var node = kvp.Value;


        if (node.type.ToLower() == "list")
        {

            try
            {
                //List_Look_Up
                var value_list = node.values;

                if (!string.IsNullOrWhiteSpace(node.path_reference))
                {
                    //var key = node.path_reference.Replace("lookup/", "");
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
                                if(! is_mapping_lookup_header_written)
                                {
                                    mapping_look_up_document.WriteHeadersToStream();
                                    is_mapping_lookup_header_written = true;

                                }
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


    Console.WriteLine("{0} Export Finished.", System.DateTime.Now);
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

            //p_result[file_name].Add(p_path, field_name);

            break;
        default:
            break;
        }
    }

    private void process_multiform_grid
    (
        IDictionary<string, object> case_doc,
        string mmria_case_id,
        int parent_record_index,
        Dictionary<string, int> path_to_int_map,
        Dictionary<string, mmria.common.metadata.node> path_to_node_map,
        Dictionary<string, string> path_to_grid_map,
        Dictionary<string, WriteCSV> path_to_csv_writer,
        HashSet<string> flat_grid_set
    )
    {



        // flat grid - start
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

                string[] temp_path = path.Split('/');
                List<string> form_path_list = new List<string>();
                for (int temp_path_index = 0; temp_path_index < temp_path.Length; temp_path_index++)
                {
                    form_path_list.Add(temp_path[temp_path_index]);
                    if (temp_path_index == 0)
                    {
                        form_path_list.Add(parent_record_index.ToString());
                    }
                }

                object raw_data = get_value(case_doc as IDictionary<string, object>, string.Join("/", path));
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
                    grid_row["_id"] = mmria_case_id;
                    grid_row["_record_index"] = i;
                    grid_row["_parent_record_index"] = parent_record_index;
                    foreach (KeyValuePair<string, string> kvp in path_to_grid_map.Where(k => k.Value == grid_name))
                    {
                        foreach (string node in grid_field_set)
                        {
                            try
                            {
                                object val = grid_item_row[path_to_node_map[node].name];

                                if (de_identified_set.Contains(node))
                                {
                                    val = null;
                                }

                                if (val != null)
                                {
                                    string file_field_name = path_to_field_name_map[node];

                                    if (path_to_node_map[node].type.ToLower() == "number" && !string.IsNullOrWhiteSpace(val.ToString()))
                                    {
                                        if (path_to_csv_writer[grid_name].Table.Columns.Contains(file_field_name))
                                        {
                                            grid_row[file_field_name] = val;
                                        }
                                        else
                                        {
                                            grid_row[$"{file_field_name}_{path_to_int_map[node].ToString()}"] = val;
                                        }
                                    }
                                    else
                                    {
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
                                                path,
                                                val?.ToString(),
                                                i,
                                                parent_record_index
                                            );

                                            val = over_limit_message;
                                        }

                                        if (path_to_csv_writer[grid_name].Table.Columns.Contains(file_field_name))
                                        {
                                            grid_row[file_field_name] = val;
                                        }
                                        else
                                        {
                                            grid_row[$"{file_field_name}_{path_to_int_map[node].ToString()}"] = val;
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
                for (var i = 0; i < p_metadata.children.Length; i++)
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

        List<string> ordered_column_list = this.Core_Element_Paths;

        for (int i = 0; i < ordered_column_list.Count; i++)
        {
            string path = ordered_column_list[i];

            if (!p_path_to_csv_set.Contains(path))
            {
                continue;
            }

            if (!p_path_to_int_map.ContainsKey(path))
            {
                continue;
            }

            string file_field_name = convert_path_to_field_name(path, p_path_to_int_map);
            switch (p_path_to_node_map[path].type.ToLower())
            {
                case "app":
                case "form":
                case "group":
                case "grid":

                continue;
                case "number":
                column = new System.Data.DataColumn(file_field_name, typeof(double));

                break;
                default:

                column = new System.Data.DataColumn(file_field_name, typeof(string));
                break;

            }
            p_Table.Columns.Add(column);
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

        /*
            if (p_path == "death_certificate/causes_of_death")
            {
                System.Console.Write("break");
            }*/

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
        /*
                if (p_is_multiform_context)
                {
                }
                else
                {*/

        file_name = this.convert_path_to_file_name(p_path);

        p_path_to_grid_map.Add(p_path, file_name);

        if (p_is_multiform_context)
        {
            p_multi_form_to_grid_map.Add(p_path, form_path);
        }
        //}

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
        //		/birth_certificate_infant_fetal_section / causes_of_death
        // /birth_certificate_infant_fetal_section
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
        //result.Append(".csv");

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
                //string val = this.get_value(
            }
        }
    }

    public string get_csv_connection_string(string p_file_name)
    {
        // @"mapping-file-set/MMRDS-Mapping-NO-GRIDS-test.csv"
        string result = string.Format(
            @"Provider=Microsoft.Jet.OleDb.4.0; Data Source={0};Extended Properties=""Text;HDR=YES;FMT=Delimited""",
            p_file_name
        );

        return result;
    }

    public object get_value(IDictionary<string, object> p_object, string p_path, bool p_is_grid = false)
    {
        object result = null;

        var de_identified_path = System.Text.RegularExpressions.Regex.Replace(p_path, "/[0-9]/", "/");

        if (de_identified_set.Contains(de_identified_path))
        {
            return result;
        }

        /*
            foreach (KeyValuePair<string, object> kvp in p_object)
            {
                System.Console.WriteLine(kvp.Key);
            }*/

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = p_object;

            if (index != null)
            for (int i = 0; i < path.Length; i++)
            {
                switch (index)
                {

                    case IDictionary<string, object> val:
                    if (i == path.Length - 1)
                    {
                        if (val != null && val.ContainsKey(path[i]))
                        {

                            result = ((IDictionary<string, object>)val)[path[i]];
                        }
                        else if (p_is_grid)
                        {
                            result = index;
                        }
                        else
                        {
                            //result = index;
                        }

                    }
                    else if (val != null && val.ContainsKey(path[i]))
                    {
                        if (val[path[i]] is List<object>)
                        {
                            index = val[path[i]] as List<object>;
                        }
                        else if (val[path[i]] is IList<object>)
                        {
                            index = val[path[i]] as IList<object>;
                        }
                        else if (val[path[i]] is IDictionary<string, object>)
                        {
                            index = val[path[i]] as IDictionary<string, object>;
                        }
                        else
                        {
                            //System.Console.WriteLine("This should not happen. {0}", p_path);
                        }
                    }

                    break;

                    case IList<object> val:
                    if (number_regex.IsMatch(path[i]))
                    {
                        int item_index = int.Parse(path[i]);
                        if (val != null && val.Count > item_index)
                        {
                            index = val[item_index] as IDictionary<string, object>;
                        }
                    }
                    else
                    {
                        //System.Console.WriteLine("This should not happen. {0}", p_path);
                    }
                    break;
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
        string p_record_id, 
        string p_mmria_path, 
        string p_data, int p_index, 
        int p_parent_index)
    {
        const string record_split = "************************************************************";
        const string header_split = "\n\n";

        int index = 0;

        switch (p_mmria_path.Trim().ToLower())
        {
            case "case_narrative/case_opening_overview":
                index = 1;
                break;
            case "informant_interviews/interview_narrative":
                index = 2;
                break;
            default:
                index = 0;
                break;
        }


        if (this.qualitativeStreamCount[index] == 0)
        {
            this.qualitativeStreamWriter[index].WriteLine($"{record_split}\nid={p_record_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n{p_data}");
        }
        else
        {
            this.qualitativeStreamWriter[index].WriteLine($"\n{record_split}\nid={p_record_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n{p_data}");
        }
        this.qualitativeStreamCount[index] += 1;
    }


    private void get_core_element_list(ref List<string> p_list, mmria.common.metadata.node p_node, string p_path)
    {
        switch (p_node.type.ToLowerInvariant())
        {
            case "group":
            case "grid":
                foreach (var child in p_node.children)
                {
                    get_core_element_list(ref p_list, child, p_path + "/" + p_node.name);
                }
                break;
            case "form":
                foreach (var child in p_node.children)
                {
                    get_core_element_list(ref p_list, child, p_node.name);
                }
                break;
            case "app":
                break;
            default:
                if
                (
                    p_node.is_core_summary != null &&
                    p_node.is_core_summary.HasValue &&
                    p_node.is_core_summary.Value
                )
                {
                    p_list.Add(p_path + "/" + p_node.name);
                }

                break;
        }
    }

    private void get_core_element_list(mmria.common.metadata.app p_metadata)
    {
        this.Core_Element_Paths = new List<string> {
            "date_created",
            "created_by",
            "date_last_updated",
            "last_updated_by"
        };

        foreach (var child in p_metadata.children)
        {
            get_core_element_list(ref this.Core_Element_Paths, child, "");
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

}
