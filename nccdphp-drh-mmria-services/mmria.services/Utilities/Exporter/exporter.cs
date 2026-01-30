using System;
using System.Collections.Generic;
using System.Linq;
using mmria.getset;

namespace mmria.services.Utilities.Exporter;

public sealed partial class exporter
{


sealed class StandardReportList
{
    public StandardReportList() 
    {
        name_path_list = new(StringComparer.OrdinalIgnoreCase);
    }
    public string _id { get; set; }
    public string _rev { get; set; }
    public string data_type { get; set; }
    public Dictionary<string,List<string>> name_path_list { get; set; }
}

StandardReportList standard_export_report_set;

List<string> export_report;

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
System.Collections.Generic.Dictionary<string, string> path_to_field_name_map = new Dictionary<string, string>();

mmria.common.metadata.app current_metadata;

private System.IO.StreamWriter[] qualitativeStreamWriter = new System.IO.StreamWriter[3];
private int[] qualitativeStreamCount = new int[] { 0, 0, 0 };

private System.IO.StreamWriter[] clearTextStreamWriter = new System.IO.StreamWriter[2];
private int[] clearTextStreamCount = new int[] { 0, 0 };
private const int max_qualitative_length = 31000;

private const string over_limit_message = "Over the qualitative limit. Check the over-the-limit folder for details.";

private mmria.server.model.actor.ScheduleInfoMessage Configuration;

mmria.common.couchdb.DBConfigurationDetail db_config;

public exporter(mmria.server.model.actor.ScheduleInfoMessage configuration)
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
    this.database_url = this.Configuration.couch_db_url;
    this.juris_user_name = this.Configuration.jurisdiction_user_name;
    this.user_name = this.Configuration.user_name;
    this.value_string = this.Configuration.user_value;

    this.item_file_name = queue_item.file_name;
    this.item_directory_name = queue_item.file_name.Substring(0, queue_item.file_name.IndexOf("."));
    this.item_id = queue_item._id;

    this.is_excel_file_type = queue_item.case_file_type == "xlsx" ? true : false;


    if (string.IsNullOrWhiteSpace(this.database_url))
    {
        this.database_url = Configuration.couch_db_url;

        if (string.IsNullOrWhiteSpace(this.database_url))
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
    this.qualitativeStreamWriter[1] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "case-narrative.txt"), true);
    this.qualitativeStreamWriter[2] = new System.IO.StreamWriter(System.IO.Path.Combine(export_directory, "informant-interview.txt"), true);


/*
    string URL = this.database_url + $"/{db_config.prefix}mmrds/_all_docs";
    string urlParameters = "?include_docs=true";
    cURL document_curl = new cURL("GET", null, URL + urlParameters, null, this.user_name, this.value_string);
    object all_cases = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_curl.execute());
*/
    string metadata_url = this.database_url + $"/metadata/version_specification-{this.Configuration.version_number}/metadata";
    cURL metadata_curl = new cURL("GET", null, metadata_url, null, this.user_name, this.value_string);
    mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());
    this.current_metadata = metadata;


    this.lookup = get_look_up(metadata);


    MetaDataNode_Dictionary = get_metadata_node(metadata);


    string standardreportlist_url = $"{this.database_url}/metadata/export-standard-list";
    cURL standardreportlist_curl = new cURL("GET", null, standardreportlist_url, null, this.user_name, this.value_string);
    var standardreportlist_curl_result = standardreportlist_curl.execute();
    standard_export_report_set = Newtonsoft.Json.JsonConvert.DeserializeObject<StandardReportList>(standardreportlist_curl_result);

    var report_name = queue_item.export_type.Substring(0,queue_item.export_type.LastIndexOf(' '));

    export_report = standard_export_report_set.name_path_list[report_name];


    var mmria_custom_export_file_name = $"{report_name}.csv";

    System.Collections.Generic.Dictionary<string, int> path_to_int_map = new Dictionary<string, int>();
    System.Collections.Generic.Dictionary<string, string> path_to_file_name_map = new Dictionary<string, string>();

    System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> path_to_node_map = new Dictionary<string, mmria.common.metadata.node>();
    System.Collections.Generic.Dictionary<string, string> path_to_grid_map = new Dictionary<string, string>();
    System.Collections.Generic.Dictionary<string, string> path_to_multi_form_map = new Dictionary<string, string>();
    System.Collections.Generic.Dictionary<string, string> grid_to_multi_form_map = new Dictionary<string, string>();

    System.Collections.Generic.HashSet<string> path_to_flat_map = new System.Collections.Generic.HashSet<string>();

    System.Collections.Generic.Dictionary<string, WriteCSV> path_to_csv_writer = new Dictionary<string, WriteCSV>();

    generate_path_map
    (
        metadata, 
        "", 
        mmria_custom_export_file_name, 
        "",
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

    path_to_csv_writer.Add(mmria_custom_export_file_name, new WriteCSV(mmria_custom_export_file_name, this.item_directory_name, Configuration.export_directory, is_excel_file_type));
    stream_file_count++;

var flat_field_list = new List<string>();
var grid_field_list = new List<string>();
var multiform_field_list = new List<string>();

var is_using_record_index = false;
var is_using_parent_record_index = false;
var is_using_grid = false;
var is_using_multiform = false;

var flat_table = new System.Data.DataTable();
var grid_table = new System.Data.DataTable();
var multiform_table = new System.Data.DataTable();

foreach(var mmria_path in export_report)
{
    switch(GetTableType(mmria_path))
    {
        case TableTypeEnum.flat:
            flat_field_list.Add(mmria_path);
            break;
        case TableTypeEnum.grid:
            grid_field_list.Add(mmria_path);
            is_using_record_index = true;
            is_using_grid = true;
            break;
        case TableTypeEnum.multiform:
            multiform_field_list.Add(mmria_path);
            is_using_record_index = true;
            is_using_multiform = true;
            break;
        case TableTypeEnum.multiform_grid:
            grid_field_list.Add(mmria_path);
            is_using_record_index = true;
            is_using_parent_record_index = true;
            is_using_grid = true;
            is_using_multiform = true;
            break;
        case TableTypeEnum.none:
        default:
        break;
    }
}


if(flat_field_list.Count > 0)
    create_header_row
    (
        path_to_int_map,
        flat_field_list.ToHashSet(),
        path_to_node_map,
        flat_table,
        false,
        false
    );


if(grid_field_list.Count > 0)
{
    if(is_using_record_index && is_using_parent_record_index)
    {
        create_header_row
        (
        path_to_int_map,
        grid_field_list.ToHashSet(),
        path_to_node_map,
        grid_table,
        true,
        true
        );
    }
    else
    {
        create_header_row
        (
        path_to_int_map,
        grid_field_list.ToHashSet(),
        path_to_node_map,
        grid_table,
        true,
        false
        );
    }



}

if(multiform_field_list.Count > 0)
{
    create_header_row
    (
        path_to_int_map,
        multiform_field_list.ToHashSet(),
        path_to_node_map,
        multiform_table,
        true,
        false
        
    );


}

    create_header_row
    (
        path_to_int_map,
        export_report.ToHashSet(),
        path_to_node_map,
        path_to_csv_writer[mmria_custom_export_file_name].Table,
        is_using_record_index,
        is_using_parent_record_index
    );

    var grantee_column = new System.Data.DataColumn("export_jurisdiction_name", typeof(string));

    grantee_column.DefaultValue = queue_item.grantee_name;
    path_to_csv_writer[mmria_custom_export_file_name].Table.Columns.Add(grantee_column);

    de_identified_set = new HashSet<string>();

    if (queue_item.de_identified_field_set != null)
    {
        foreach (string path in queue_item.de_identified_field_set)
        {
        de_identified_set.Add(path.TrimStart('/'));
        }
    }

    if(flat_field_list.Contains("case_narrative/case_opening_overview"))
    {
        this.clearTextStreamWriter[0] = new System.IO.StreamWriter(System.IO.Path.Combine(export_root_directory, "case-narrative-plaintext.txt"), true);
    }

    if(multiform_field_list.Contains("informant_interviews/interview_narrative"))
    {
        this.clearTextStreamWriter[1] = new System.IO.StreamWriter(System.IO.Path.Combine(export_root_directory, "informant-interview-plaintext.txt"), true);
    }


    HashSet<string> Custom_Case_Id_List = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var id in queue_item.case_set)
    {
        Custom_Case_Id_List.Add(id);
    }

    List<System.Dynamic.ExpandoObject> cases_to_process = new List<System.Dynamic.ExpandoObject>();

    #if !IS_PMSS_ENHANCED
    var jurisdiction_hashset = mmria.services.authorization.get_current_jurisdiction_id_set_for(db_config, this.juris_user_name);
    #endif
    #if IS_PMSS_ENHANCED
    var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, this.juris_user_name);
    #endif

    if (queue_item.case_filter_type != "custom")
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

    }


    foreach(string case_id in Custom_Case_Id_List)
    {
        string URL = $"{this.database_url}/{db_config.prefix}mmrds/{case_id}";

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

        #if !IS_PMSS_ENHANCED
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
        #endif
        #if IS_PMSS_ENHANCED
        var tracking_record = case_doc["tracking"] as IDictionary<string, object>;
        if (tracking_record == null) continue;
        
        var home_record = tracking_record["admin_info"] as IDictionary<string, object>;

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
        #endif

        if (!is_jurisdiction_ok)
        {
            continue;
        }


        
        string mmria_case_id = case_doc["_id"].ToString();
    
    System.Data.DataRow row = flat_table.NewRow();
    flat_table.Rows.Add(row);

        foreach (string path in flat_field_list)
        {
        if 
        (
            !path_to_node_map.ContainsKey(path) ||
            path_to_node_map[path].type.ToLower() == "app" ||
            path_to_node_map[path].type.ToLower() == "form" ||
            path_to_node_map[path].type.ToLower() == "group" ||
            path_to_node_map[path].type.ToLower() == "button" ||
            path_to_node_map[path].type.ToLower() == "always_enabled_button" ||
            path_to_node_map[path].type.ToLower() == "chart" ||
            path_to_node_map[path].type.ToLower() == "label" ||
            path_to_node_map[path].mirror_reference != null

        )
        {
            continue;
        }



        row["_id"] = mmria_case_id;
        
        //System.Console.WriteLine("path {0}", path);

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

        object val = get_value(case_doc as IDictionary<string, object>, path);
        try
        {
            switch (path_to_node_map[path].type.ToLower())
            {

            case "number":
                double try_double = 0;

                if (val != null && (!string.IsNullOrWhiteSpace(val.ToString())) && double.TryParse(val.ToString(), out try_double))
                {
                string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
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

                    string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
                    row[file_field_name] = string.Join("|", temp2);

                }
                else
                {
                    string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
                    row[file_field_name] = "(blank)";
                }
                }
                else
                {
                string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
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

                if(path=="host_state")
                {
                    val = val.ToString().ToUpper();
                }

                string clearText = "";
                if
                (
                    path == "case_narrative/case_opening_overview" ||
                    path == "informant_interviews/interview_narrative" 
                )
                {
                    clearText = (path == "case_narrative/case_opening_overview")
                        ? mmria.common.util.CaseNarrative.StripHTML(val?.ToString())
                        : mmria.common.util.TextAreaField.CleanUp(val?.ToString());
                    if (clearText.Length > 0) 
                    {
                        
                        WriteClearTextData
                        (
                            mmria_case_id,
                            path,
                            clearText,
                            -1,
                            -1
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
                    path,
                    val?.ToString(),
                    -1,
                    -1
                    );

                    
                    val = over_limit_message;
                }

                string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
                row[file_field_name] = val;

                }
                break;

            }
        }
        catch (Exception)// ex)
        {
            System.Console.Write("bad export value: {0} - {1} - {2}", mmria_case_id, val, path);
        }

        } // end of flat_table

        var gs = new migrate.C_Get_Set_Value(new ());

        if(is_using_grid && is_using_multiform)
        {
            
            migrate.C_Get_Set_Value.get_multiform_grid_value_result multiform_grid_value_result = null;
            var grid_row_list = new List<System.Data.DataRow>();

            foreach (string path in grid_field_list)
            {
                multiform_grid_value_result = gs.get_multiform_grid_value(case_row, path);
                if( !multiform_grid_value_result.is_error)
                {
                    if
                    (
                        multiform_grid_value_result.result != null &&
                        multiform_grid_value_result.result.Count > 0
                    )
                    
                    if(grid_row_list.Count == 0)
                    {
                        if(grid_row_list.Count == 0)
                        {
                            for(var i = 0; i < multiform_grid_value_result.result.Count; i++)
                            {
                                grid_row_list.Add(grid_table.NewRow());
                                grid_table.Rows.Add(grid_row_list[i]);
                                grid_row_list[i]["_id"] = mmria_case_id;
                            }
                        }

            
                        string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
                        foreach(var (form_index, grid_index, value) in multiform_grid_value_result.result)
                        {
                            GetDataRowValue
                            (
                                grid_row_list[grid_index],
                                grid_table.Columns[file_field_name], 
                                file_field_name,
                                MetaDataNode_Dictionary[path],
                                mmria_case_id,
                                value,
                                form_index,
                                grid_index

                            );    
                            
                            grid_row_list[grid_index]["_record_index"] = grid_index;
                            grid_row_list[grid_index]["_parent_record_index"] = form_index;
                        }
                    }
                }

            }//end of grid
        }
        else
        {
            migrate.C_Get_Set_Value.get_grid_value_result grid_value_result = null;
            var grid_row_list = new List<System.Data.DataRow>();

            foreach (string path in grid_field_list)
            {
                grid_value_result = gs.get_grid_value(case_row, path);
                if( !grid_value_result.is_error)
                {
                    if
                    (
                        grid_value_result.result != null &&
                        grid_value_result.result.Count > 0
                    )
                    {
                        if(grid_row_list.Count == 0)
                        {
                            for(var i = 0; i < grid_value_result.result.Count; i++)
                            {
                                grid_row_list.Add(grid_table.NewRow());
                                grid_table.Rows.Add(grid_row_list[i]);
                                grid_row_list[i]["_id"] = mmria_case_id;
                            }
                        }

            
                        string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
                        foreach(var (index, value) in grid_value_result.result)
                        {
                            GetDataRowValue
                            (
                                grid_row_list[index],
                                grid_table.Columns[file_field_name], 
                                file_field_name,
                                MetaDataNode_Dictionary[path],
                                mmria_case_id,
                                value,
                                -1,
                                index
                            );  
                            
                            grid_row_list[index]["_record_index"] = index;
                        }
                    }
                }

            }
        }

        migrate.C_Get_Set_Value.get_multiform_value_result multiform_value_result = null;
        var multiform_row  = multiform_table.NewRow();
        var multiform_row_list = new List<System.Data.DataRow>();
        
        foreach (string path in multiform_field_list)
        {
            multiform_value_result = gs.get_multiform_value(case_row, path);
            if( !multiform_value_result.is_error)
            {
                if
                (
                    multiform_value_result.result != null &&
                    multiform_value_result.result.Count > 0
                )
                {

                    if(multiform_row_list.Count == 0)
                    {
                        for(var i = 0; i < multiform_value_result.result.Count; i++)
                        {
                            multiform_row_list.Add(multiform_table.NewRow());
                            multiform_table.Rows.Add(multiform_row_list[i]);
                            multiform_row_list[i]["_id"] = mmria_case_id;
                        }
                    }

                    if(MetaDataNode_Dictionary.ContainsKey(path))
                    {

                        string file_field_name = MetaDataNode_Dictionary[path].sass_export_name;
                        foreach(var (index, value) in multiform_value_result.result)
                        {

                            try
                            {
                                GetDataRowValue
                                (
                                    multiform_row_list[index],
                                    multiform_table.Columns[file_field_name], 
                                    file_field_name,
                                    MetaDataNode_Dictionary[path],
                                    mmria_case_id,
                                    value,
                                    index
                                ); 

                                multiform_row_list[index]["_record_index"] = index;
                            }
                            catch(Exception ex)
                            {

                            }
                        }
                    }
                    else
                    {

                    }
                }
            }
        }// end of multiform
    } // end of foreach


    if(is_using_grid && is_using_multiform)
    {
        foreach(System.Data.DataRow gr in grid_table.Rows)
        {
            System.Data.DataRow output_row = path_to_csv_writer[mmria_custom_export_file_name].Table.NewRow();
            
            var fr = flat_table.Select($"_id='{gr["_id"]}'");
            if(fr.Length > 0)
            {
                foreach(System.Data.DataColumn c in flat_table.Columns)
                {
                    
                    output_row[c.ColumnName] = fr[0][c.ColumnName];
                }
            }
            else
            {

            }

            if(gr["_parent_record_index"] == System.DBNull.Value)
            {
                continue;
            }

            var mr = multiform_table.Select($"_id='{gr["_id"]}' and _record_index = {gr["_parent_record_index"]}");
            if(fr.Length > 0)
            {
                foreach(System.Data.DataColumn c in multiform_table.Columns)
                {
                    output_row[c.ColumnName] = mr[0][c.ColumnName];
                }
            }
            else
            {

            }

            foreach(System.Data.DataColumn c in grid_table.Columns)
            {
                output_row[c.ColumnName] = gr[c.ColumnName];
            }
        
            path_to_csv_writer[mmria_custom_export_file_name].Table.Rows.Add(output_row);
        }
    }
    else if(is_using_grid)
    {
        foreach(System.Data.DataRow gr in grid_table.Rows)
        {
            System.Data.DataRow output_row = path_to_csv_writer[mmria_custom_export_file_name].Table.NewRow();
            
            var fr = flat_table.Select($"_id='{gr["_id"]}'");
            if(flat_table.Columns.Count > 0)
            {
                foreach(System.Data.DataColumn c in flat_table.Columns)
                {
                    output_row[c.ColumnName] = fr[0][c.ColumnName];
                }
            }

            foreach(System.Data.DataColumn c in grid_table.Columns)
            {
                output_row[c.ColumnName] = gr[c.ColumnName];
            }

                path_to_csv_writer[mmria_custom_export_file_name].Table.Rows.Add(output_row);
        }
    }
    else if(is_using_multiform)
    {
        foreach(System.Data.DataRow mr in multiform_table.Rows)
        {
            System.Data.DataRow output_row = path_to_csv_writer[mmria_custom_export_file_name].Table.NewRow();
            
            if(flat_table.Columns.Count > 0)
            {
                var fr = flat_table.Select($"_id='{mr["_id"]}'");
                foreach(System.Data.DataColumn c in flat_table.Columns)
                {
                    output_row[c.ColumnName] = fr[0][c.ColumnName];
                }
            }

            foreach(System.Data.DataColumn c in multiform_table.Columns)
            {
                output_row[c.ColumnName] = mr[c.ColumnName];
            }

            path_to_csv_writer[mmria_custom_export_file_name].Table.Rows.Add(output_row);
        }
    }
    else
    {
        foreach(System.Data.DataRow fr in flat_table.Rows)
        {
            var output_row = path_to_csv_writer[mmria_custom_export_file_name].Table.NewRow();
            
            foreach(System.Data.DataColumn c in flat_table.Columns)
            {
                output_row[c.ColumnName] = fr[c.ColumnName];
            }

            path_to_csv_writer[mmria_custom_export_file_name].Table.Rows.Add(output_row);
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
        
        var metadata_node_kvp = MetaDataNode_Dictionary.Where(x => x.Value.sass_export_name == table_column.ColumnName).FirstOrDefault();

        System.Data.DataRow mapping_row = mapping_document.Table.NewRow();
        mapping_row["file_name"] = kvp.Key;


        if (! metadata_node_kvp.Equals(default(KeyValuePair<string, Metadata_Node>)))
        {
            var metadata_node = metadata_node_kvp.Value;

            string deidentified = "no";
            string path = metadata_node.path;
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
            mapping_row["mmria_prompt"] = metadata_node.Node.prompt;
            mapping_row["field_description"] = metadata_node.Node.description;
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


        mapping_document.Table.Rows.Add(mapping_row);
        }


        kvp.Value.WriteHeadersToStream();
        kvp.Value.WriteToStream();
    }

    mapping_document.WriteHeadersToStream();
    mapping_document.WriteToStream();

    for (int i_index = 0; i_index < this.qualitativeStreamWriter.Length; i_index++)
    {
        this.qualitativeStreamWriter[i_index].Flush();
        this.qualitativeStreamWriter[i_index].Close();
        this.qualitativeStreamWriter[i_index] = null;
    }

    for (int i_index = 0; i_index < this.clearTextStreamWriter.Length; i_index++)
    {
        if(this.clearTextStreamWriter[i_index]!= null)
        {
            this.clearTextStreamWriter[i_index].Flush();
            this.clearTextStreamWriter[i_index].Close();
            this.clearTextStreamWriter[i_index] = null;
        }
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




    foreach (KeyValuePair<string, mmria.common.metadata.node> kvp in path_to_node_map)
    {
        

        if(export_report.IndexOf(kvp.Key) < 0)
        {
            continue;
        }

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

                if (MetaDataNode_Dictionary.ContainsKey(kvp.Key))
                {
                    row["column_name"] = MetaDataNode_Dictionary[kvp.Key].sass_export_name;
                }

                row["mmria_path"] = kvp.Key;
                row["mmria_prompt"] = node.prompt;
                row["field_description"] = node.description;
                row["item_value"] = item.Key;
                row["item_display"] = item.Value;
                //row["item_description"] = item.description;
                mapping_look_up_document.Table.Rows.Add(row);
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

                if (MetaDataNode_Dictionary.ContainsKey(kvp.Key))
                {
                row["column_name"] = MetaDataNode_Dictionary[kvp.Key].sass_export_name;
                }
                row["mmria_path"] = kvp.Key;
                row["mmria_prompt"] = node.prompt;
                row["field_description"] = node.description;
                row["item_value"] = item.value;
                row["item_display"] = item.display;
                row["item_description"] = item.description;


                mapping_look_up_document.Table.Rows.Add(row);
            }
            }


        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
        }
        }
    }

    mapping_look_up_document.WriteHeadersToStream();
    mapping_look_up_document.WriteToStream();


    mmria.server.utils.cFolderCompressor folder_compressor = new mmria.server.utils.cFolderCompressor();


    string encryption_key = null;

    if (!string.IsNullOrWhiteSpace(queue_item.zip_key))
    {
        encryption_key = queue_item.zip_key;
    }

    folder_compressor.Compress
    (
        System.IO.Path.Combine(Configuration.export_directory, this.item_file_name),
        encryption_key,
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


    private void GetDataRowValue
    (
        System.Data.DataRow p_row, 
        System.Data.DataColumn column, 
        string p_column_name,
        Metadata_Node p_node,
        string p_mmria_case_id,
        object p_value,
        int p_form_index = -1,
        int p_grid_index = -1
        
        
    )
    {
        if(p_value == null  || string.IsNullOrWhiteSpace(p_value.ToString()))
        {
            if(p_node.Node.type.ToLower() == "list")
            {
                if
                (
                    p_node.Node.is_multiselect != null &&
                    p_node.Node.is_multiselect == true
                )
                {
                    p_row[p_column_name] = "(blank)";
                }
                else
                {
                    p_row[p_column_name] = "9999";
                }
            }
        }
        else
        {

            switch(column.DataType)
            {
                default:
                    if
                    (
                        (
                            p_node.Node.type.ToLower() == "list" &&
                            p_node.Node.is_multiselect != null &&
                            p_node.Node.is_multiselect == true
                        ) 
                            ||
                        (
                            p_value != null &&
                            p_value is List<object>
                        )

                    )
                    {

                        var path = p_node.path;
                        List<object> temp = p_value as List<object>;
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

                            p_row[p_column_name] = string.Join("|", temp2);
                        }
                        else
                        {
                            p_row[p_column_name] = "(blank)";
                        }
                    }
                    else if
                    (
                        p_node.Node.type.ToLower() == "textarea" ||
                        p_node.Node.type.ToLower() == "string"
                    )
                    {
                        if (p_value != null)
                        {
                            string clearText = "";
                            
                            if
                            (
                                p_node.path == "case_narrative/case_opening_overview" ||
                                p_node.path == "informant_interviews/interview_narrative"
                            )
                            {
                                clearText = (p_node.path == "case_narrative/case_opening_overview")
                                    ? mmria.common.util.CaseNarrative.StripHTML(p_value.ToString())
                                    : mmria.common.util.TextAreaField.CleanUp(p_value.ToString());
                                if (clearText.Length > 0) 
                                {
                                    
                                    WriteClearTextData
                                    (
                                        p_mmria_case_id,
                                        p_node.path,
                                        clearText,
                                        p_grid_index,
                                        p_form_index
                                    );
                                }
                            }

                            if
                            (
                                p_value.ToString().Length > max_qualitative_length
                            )
                            {
                                WriteQualitativeData
                                (
                                    p_mmria_case_id,
                                    p_node.path,
                                    p_value?.ToString(),
                                    p_grid_index,
                                    p_form_index
                                );

                                
                                p_value = over_limit_message;
                            }

                            string file_field_name = MetaDataNode_Dictionary[p_node.path].sass_export_name;
                            p_row[p_column_name] = p_value;

                        }
                    }
                    else
                    {
                        p_row[p_column_name] = p_value;
                    }
                    break;
            }
                            
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

private void create_header_row
(
    System.Collections.Generic.Dictionary<string, int> p_path_to_int_map,
    System.Collections.Generic.HashSet<string> p_path_to_csv_set,
    System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> p_path_to_node_map,
    System.Data.DataTable p_Table,
    bool p_add_record_index,
    bool p_add_parent_record_index
)
{
    if (p_Table.Columns.Count > 0)
    {
        return;
    }

    System.Data.DataColumn column = null;

    column = new System.Data.DataColumn("_id", typeof(string));
    p_Table.Columns.Add(column);


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
    var file_field_name = MetaDataNode_Dictionary[path].sass_export_name;

    switch (p_path_to_node_map[path].type.ToLower())
    {
        case "app":
        case "form":
        case "group":
        case "grid":
        case "button":
        case "chart":
        case "label":
        continue;
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
                //result = index;
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
    string p_record_id, 
    string p_mmria_path, 
    string p_data, 
    int p_index, 
    int p_parent_index
    
)
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


private void WriteClearTextData
(
    string p_record_id, 
    string p_mmria_path, 
    string p_data, 
    int p_index, 
    int p_parent_index
    
)
{
    const string record_split = "************************************************************";
    const string header_split = "\n\n";

    int index = 0;

    switch (p_mmria_path.Trim().ToLower())
    {
        case "case_narrative/case_opening_overview":
            index = 0;
            break;
        case "informant_interviews/interview_narrative":
            index = 1;
            break;
        default:
            return;
    }


    if (this.clearTextStreamCount[index] == 0)
    {
        this.clearTextStreamWriter[index].WriteLine($"{record_split}\nid={p_record_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n{p_data}");
    }
    else
    {
        this.clearTextStreamWriter[index].WriteLine($"\n{record_split}\nid={p_record_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n{p_data}");
    }
    this.clearTextStreamCount[index] += 1;
}



}
