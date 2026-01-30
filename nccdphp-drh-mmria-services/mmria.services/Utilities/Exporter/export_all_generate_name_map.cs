using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Microsoft.Extensions.Configuration;
using mmria.getset;


namespace mmria.services.Utilities.Exporter;

public sealed class export_all_generate_name_map
{
    private string auth_token = null;
    private string user_name = null;

    private string juris_user_name = null;
    private string value_string = null;
    private string database_path = null;
    private string database_url = null;
    private string item_file_name = null;
    private string item_directory_name = null;
    private string item_id = null;
    private bool is_cdc_de_identified = false;

    private bool is_offline_mode;

    private HashSet<string> de_identified_set;

System.Collections.Generic.Dictionary<string, string> path_to_field_name_map = new Dictionary<string, string>();


    private System.IO.StreamWriter[] qualitativeStreamWriter = new System.IO.StreamWriter[3];
    private int[] qualitativeStreamCount = new int[]{ 0,0,0 };
    private const int max_qualitative_length = 31000;

    private const string over_limit_message = "Over the qualitative limit. check the over-the-qualitative-limit.txt file for details.";

    private common.couchdb.DBConfigurationDetail db_config;

    public export_all_generate_name_map(common.couchdb.DBConfigurationDetail _db_config)
    {
        this.db_config = _db_config;

    }
    public Dictionary<string, Dictionary<string, string>> Execute(string p_version, string p_export_type = "all")
    {
    
        string metadata_url = $"{db_config.url}/metadata/{p_version}/metadata";
        cURL metadata_curl = new cURL("GET", null, metadata_url, null, db_config.user_name, db_config.user_value);

        //System.Console.WriteLine("metadata_url: " + metadata_url);
        var curl_result = metadata_curl.execute();

        
        //System.Console.WriteLine("Execute(string p_version, string p_export_type = all)");
        //System.Console.WriteLine(curl_result);
        
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(curl_result);




        /*
        foreach (KeyValuePair<string, object> kvp in all_cases)
        {
            System.Console.WriteLine(kvp.Key);
        }*/

        System.Collections.Generic.Dictionary<string, int> path_to_int_map = new Dictionary<string, int>();
        System.Collections.Generic.Dictionary<string, string> path_to_file_name_map = new Dictionary<string, string>();
        System.Collections.Generic.Dictionary<string, mmria.common.metadata.node> path_to_node_map = new Dictionary<string, mmria.common.metadata.node>();
        System.Collections.Generic.Dictionary<string, string> path_to_grid_map = new Dictionary<string, string>();
        System.Collections.Generic.Dictionary<string, string> path_to_multi_form_map = new Dictionary<string, string>();
        System.Collections.Generic.Dictionary<string, string> grid_to_multi_form_map = new Dictionary<string, string>();

        System.Collections.Generic.HashSet<string> path_to_flat_map = new System.Collections.Generic.HashSet<string>();

        System.Collections.Generic.Dictionary<string, WriteCSV> path_to_csv_writer = new Dictionary<string, WriteCSV>();

        generate_path_map
        (	metadata, "", "mmria_case_export.csv", "",
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


        var name_map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        bool is_core_export = false;

        if(p_export_type.ToLower() == "core")
        {
            is_core_export = true;
        }

        generate_file_names(name_map, metadata, path_to_int_map, path_to_file_name_map, is_core_export);


        return name_map;
    }


    public void generate_file_names(Dictionary<string, Dictionary<string, string>> p_result, mmria.common.metadata.app p_metadata, Dictionary<string, int> path_to_int_map, Dictionary<string,string> path_to_file_name_map, bool p_is_core)
    {
        string main_file_name = null;
        if(p_is_core)
        {
            main_file_name = "core_mmria_export.csv";
        }
        else
        {
            main_file_name = "mmria_case_export.csv";
        }


        p_result.Add(main_file_name, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));



        p_result[main_file_name].Add("_id","_id");
        //p_result[main_file_name].Add("version","");
        /*
        p_result[main_file_name].Add("date_created","version");
        p_result[main_file_name].Add("created_by","created_by");
        p_result[main_file_name].Add("date_last_updated","date_last_updated");
        p_result[main_file_name].Add("last_updated_by","last_updated_by");
*/
        foreach(mmria.common.metadata.node node in p_metadata.children)
        {
            generate_file_names(p_result, node, path_to_int_map, path_to_file_name_map, "/" + node.name.ToLower(), main_file_name, p_is_core, false, false);
        }

    }

    public void generate_file_names
    (
        Dictionary<string, Dictionary<string, string>> p_result, 
        mmria.common.metadata.node p_metadata, 
        Dictionary<string, int> p_path_to_int_map, 
        Dictionary<string,string> p_path_to_file_name_map, 
        string p_path, 
        string file_name, 
        bool p_is_core, 
        bool p_is_multi_form, 
        bool p_is_grid
    )
    {

            //p_result.Add(field_name)
        string field_name = null;

        try
        {
            switch(p_metadata.type.ToLower())
            {
                case "form":
                    if
                    (
                        p_metadata.cardinality!= null &&
                        (
                            p_metadata.cardinality == "*" ||
                            p_metadata.cardinality == "+"
                        )
                        
                    )
                    {


                        file_name = this.convert_path_to_file_name(p_path);
                        if(p_result.ContainsKey(file_name))
                        {
                                file_name = $"{file_name}_{p_path_to_int_map[p_path].ToString()}";
                        }
                        p_result.Add(file_name, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

                        foreach(mmria.common.metadata.node node in p_metadata.children)
                        {
                            generate_file_names(p_result, node, p_path_to_int_map, p_path_to_file_name_map, p_path + "/" + node.name.ToLower(), file_name, p_is_core, true, false);
                        }
                    }
                    else
                    {
                        foreach(mmria.common.metadata.node node in p_metadata.children)
                        {
                            generate_file_names(p_result, node, p_path_to_int_map, p_path_to_file_name_map, p_path + "/" + node.name.ToLower(), file_name, p_is_core, false, false);
                        }
                    }
                    
                    break;
                case "group":

                    if
                    (
                        !(p_metadata.tags is null) &&
                        p_metadata.tags.Length > 0 &&
                        p_metadata.tags[0] == "CALC_DATE"
                    )
                    {
                        field_name = convert_path_to_field_name(p_path, p_path_to_int_map);
                    
                        p_result[file_name].Add(p_path, field_name);
                    }


                    foreach(mmria.common.metadata.node node in p_metadata.children)
                    {
                        generate_file_names(p_result, node, p_path_to_int_map, p_path_to_file_name_map, p_path + "/" + node.name.ToLower(), file_name, p_is_core, p_is_multi_form, p_is_grid);
                    }
                    
                    break;
                case "grid":
                    file_name = convert_path_to_file_name(p_path);
                    
                    if(p_result.ContainsKey(file_name))
                    {
                            file_name = $"{file_name}_{p_path_to_int_map[p_path].ToString()}";
                    }
                    p_result.Add(file_name, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

                    foreach(mmria.common.metadata.node node in p_metadata.children)
                    {
                        generate_file_names(p_result, node, p_path_to_int_map, p_path_to_file_name_map, p_path + "/" + node.name.ToLower(), file_name, p_is_core, p_is_multi_form, true);
                    }
                    break;
                case "button":
                case "chart":
                case "label":				
                    break;
                default:
                    if
                    (
                        p_is_core &&
                        (
                            p_metadata.is_core_summary == null ||
                            (
                                p_metadata.is_core_summary.HasValue &&
                                p_metadata.is_core_summary.Value != true
                            )
                        )
                    )
                    {
                        break;
                    }

                    field_name = convert_path_to_field_name(p_path, p_path_to_int_map);
                    /*
                    if(p_result.ContainsKey(field_name))
                    {
                            field_name = $"{file_name}_{p_path_to_int_map[p_path].ToString()}";
                    }*/
                    p_result[file_name].Add(p_path, field_name);
                    break;
            }

        }
        catch(Exception ex)
        {
            System.Console.Write(ex);
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
            var field_name = convert_path_to_field_name(path, p_path_to_int_map);

            switch (p_path_to_node_map[path].type.ToLower())
            {
                case "app":
                case "form":
                case "group":
                case "grid":

                                        continue;
                case "number":
                    //column = new System.Data.DataColumn(p_path_to_int_map[path].ToString(), typeof(double));
                    column = new System.Data.DataColumn(field_name, typeof(double));
                    break;
                default:
                    
                    //column = new System.Data.DataColumn(p_path_to_int_map[path].ToString(), typeof(string));
                    column = new System.Data.DataColumn(field_name, typeof(string));
                    break;

            }

            try
            {
                p_Table.Columns.Add(column);
            }
            catch (Exception)
            {
                column.ColumnName = "_" + p_path_to_int_map[path].ToString();
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

            if(children != null)
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];

                generate_path_map(child, child.name, p_file_name, p_form_path, p_path_to_int_map,  p_path_to_file_name_map, p_path_to_node_map, p_path_to_grid_map, p_path_to_multi_form_map, false, p_multi_form_to_grid_map, false, p_path_to_flat_map);
            }
        }
    }




    private string convert_path_to_field_name(string p_path, Dictionary<string,int> p_path_to_int_map)
    {
        //		/birth_certificate_infant_fetal_section / causes_of_death
        // /birth_certificate_infant_fetal_section
        bool is_added_item = false;
        int form_prefix = 0;
        //int field_prefix = 0;

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

                        //return result.ToString();
                    }



                }
                else if (!string.IsNullOrWhiteSpace(temp2[j]))
                {
                    result.Append(temp2[j][0]);
                    is_added_item = true;
                }
            }
        }

        var result_value = result.ToString();
        if(path_to_field_name_map.ContainsValue(result_value))
        {
            //path_to_field_name_map[p_path] = result_value;
            result_value = result_value + "_" + p_path_to_int_map[p_path.Substring(1, p_path.Length -1)];
            //path_to_field_name_map.Add(p_path, result_value + "_" + p_path_to_int_map[p_path]);
        }

        path_to_field_name_map.Add(p_path, result_value);


        return result_value;
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
            if(children != null)
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
        if(value.Length > 32)
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

    public object get_value(IDictionary<string, object> p_object, string p_path)
    {
        object result = null;
        /*
        foreach (KeyValuePair<string, object> kvp in p_object)
        {
            System.Console.WriteLine(kvp.Key);
        }*/

        if(de_identified_set.Contains(p_path))
        {
            return result;
        }

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = p_object;

            
            if (p_path.Contains("birth_order"))
            {
                System.Console.WriteLine("break");
            }
            

            for (int i = 0; i < path.Length; i++)
            {
                if (i == path.Length - 1)
                {
                    if (index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        result = ((IDictionary<string, object>)index)[path[i]];
                    }
                    else
                    {
                        result = index;
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
                else if(index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                {
                    
                    switch(((IDictionary<string, object>)index)[path[i]])
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
/*
                    if (((IDictionary<string, object>)index)[path[i]] is IList<object>)
                    {
                        index = ((IDictionary<string, object>)index)[path[i]] as IList<object>;
                    }
                    else if (((IDictionary<string, object>)index)[path[i]]is IDictionary<string, object>)
                    {
                        index = ((IDictionary<string, object>)index)[path[i]] as IDictionary<string, object>;
                    }
*/						
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



    private void WriteQualitativeData(string p_record_id, string p_mmria_path, string p_data, int p_index, int p_parent_index)
    {
        const string record_split = "************************************************************";
        const string header_split = "\n\n";

        int index = 0;

        switch(p_mmria_path.Trim().ToLower())
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


        if(this.qualitativeStreamCount[index] == 0)
        {
            this.qualitativeStreamWriter[index].WriteLine($"{record_split}\nid={p_record_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n\n{p_data}");
        }
        else
        {
            this.qualitativeStreamWriter[index].WriteLine($"\n{record_split}id={p_record_id}\npath={p_mmria_path}\nrecord_index={p_index}\nparent_index={p_parent_index}{header_split}\n\n{p_data}");
        }
        this.qualitativeStreamCount[index]+=1;
    }




}

