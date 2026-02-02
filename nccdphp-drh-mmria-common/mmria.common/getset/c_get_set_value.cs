using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate;

public sealed partial class C_Get_Set_Value2
{

    public bool set_value(string p_metadata_path, string p_value, object p_case, int p_index = -1)
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
/*
    public object get_value(mmria.common.metadata.app p_metadata, System.Dynamic.ExpandoObject p_object, string p_path)
    {
        object result = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");
            if(path.Length == 1)
            {

            }
            


            //IDictionary<string, object> index = p_object;
            object index = p_object;
        }
        catch(System.Exception ex)
        {

        }
    }

    public object get_value(mmria.common.metadata.node p_metadata, System.Dynamic.ExpandoObject p_object, string p_path)
    {
        object result = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = p_object;
        }
        catch(System.Exception ex)
        {
            
        }
    }
*/
    public object get_value(System.Dynamic.ExpandoObject p_object, string p_path, string p_data_type = "string")
    {
        object result = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = p_object;

            /*
            if (path[1] == "abnormal_conditions_of_newborn")
            {
                System.Console.WriteLine("break");
            }*/


            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {

                    if (i == path.Length - 1 && index is IDictionary<string, object>)
                    {
                        
                        IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                        if(dictionary_object == null)
                        {
                            result = null;
                            return result;
                        }

                        object val = null;

                        if(dictionary_object.ContainsKey(path[i]))
                        {
                            val = dictionary_object[path[i]]; 
                        }

                        if(val != null)
                        {
                            if(val.GetType() == typeof(System.DateTime))
                            {
                                System.DateTime? temp_date_time = val as System.DateTime?;
                                result = temp_date_time.Value;	
                            }
                            else if(val.GetType() == typeof(string))
                            {
                                result = val.ToString();	
                            }

                            else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                            {
                                
                                result = val as System.Collections.Generic.List<string>;	
                            }
                            else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                            {

                                result = val as System.Collections.Generic.List<object>;	
                            }
                            else
                            {
                                result = val;	
                            }
                        }
                        else
                        {
                            result = null;	
                        }
                    }
                    else
                    {
                        index = ((IDictionary<string, object>)p_object)[path[i]];
                    }
                }
                else if (i == path.Length - 1)
                {
                    if (index is IDictionary<string, object>)
                    {

                        IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                        if(dictionary_object == null)
                        {
                            result = null;
                            return result;
                        }

                        object val = null;

                        if(dictionary_object.ContainsKey(path[i]))
                        {
                            val = dictionary_object[path[i]]; 
                        }


                        if(val != null)
                        {
                            if(val.GetType() == typeof(System.DateTime))
                            {
                                System.DateTime? temp_date_time = val as System.DateTime?;
                                result = temp_date_time.Value;	
                            }
                            else if(val.GetType() == typeof(string))
                            {
                                result = val.ToString();	
                            }

                            else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                            {

                                result = val as System.Collections.Generic.List<string>;	
                            }
                            else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                            {

                                result = val as System.Collections.Generic.List<object>;	
                            }
                            else
                            {
                                result = val;	
                            }

                        }
                        else
                        {
                            result = null;	
                        }
                    }
                    else
                    {
                        //System.Console.WriteLine("break");
                    }
                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                        /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }*/
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }


    public List<(int, object)> get_grid_value(System.Dynamic.ExpandoObject p_object, string p_path)
    {
        List<(int, object)> result = new List<(int, object)>();

        //dynamic current = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = null;

            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {
                    IDictionary<string, object> index_dictionary = p_object as IDictionary<string, object>;
                    if
                    (
                        index_dictionary != null &&
                        index_dictionary.ContainsKey(path[0])
                    )
                    {
                        index = index_dictionary[path[0]];
                    }
                    else
                    {
                        return result;
                    }

                }
                else if (i == path.Length - 1)
                {
                    if (index is IList<object>)
                    {
                        var grid_list = index as IList<object>;

                        for(int grid_index = 0; grid_index < grid_list.Count; grid_index++)
                        {
                            var grid_row = grid_list[grid_index];

                            if(grid_row is IDictionary<string, object>)
                            {
                                var grid_row_dictionary = grid_row as IDictionary<string, object>;
                                if(grid_row_dictionary.ContainsKey(path[i]))
                                {
                                    result.Add((grid_index, grid_row_dictionary[path[i]]));
                                }
                            }
                        }		
                    }
                    else
                    {
                        System.Console.WriteLine("get_grid_value: This should not happen. {0}", p_path);
                    }

                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }*/
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }


    public bool set_grid_value(System.Dynamic.ExpandoObject p_object, string p_path, List<(int, object)> p_value_list)
    {
        var result = false;

        //List<(int, object)> result = new List<(int, object)>();

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = null;

            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {
                    IDictionary<string, object> index_dictionary = p_object as IDictionary<string, object>;
                    if
                    (
                        index_dictionary != null &&
                        index_dictionary.ContainsKey(path[0])
                    )
                    {
                        index = index_dictionary[path[0]];
                    }
                    else
                    {
                        return result;
                    }

                }
                else if (i == path.Length - 1)
                {
                    if (index is IList<object>)
                    {
                        var grid_list = index as IList<object>;
                        result = true;
                        foreach(var tuple in p_value_list)
                        //for(int grid_index = 0; grid_index < grid_list.Count; grid_index++)
                        {

                            int grid_index = tuple.Item1;
                            if(grid_list.Count > grid_index)
                            {
                                var grid_row = grid_list[grid_index];

                                if(grid_row is IDictionary<string, object>)
                                {
                                    var grid_row_dictionary = grid_row as IDictionary<string, object>;
                                    if(grid_row_dictionary.ContainsKey(path[i]))
                                    {
                                        grid_row_dictionary[path[i]] = tuple.Item2;
                                    }
                                    else
                                    {
                                        grid_row_dictionary.Add(path[i].ToLower(), tuple.Item2);
                                    }
                                    
                                    result = result && true;
                                    
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                            else
                            {
                                result = false;
                            }
                        }		
                    }
                    else
                    {
                        System.Console.WriteLine("set_grid_value: This should not happen. {0}", p_path);
                    }

                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }
                    */
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }


    public List<(int, object)> get_multiform_value(System.Dynamic.ExpandoObject p_object, string p_path)
    {

        var result = new List<(int, object)>();

        object current = null;

        try
        {
            string[] path = p_path.Split('/');
            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            

            IDictionary<string,object> case_dictionary = p_object as IDictionary<string,object>;


            if(!case_dictionary.ContainsKey(path[0]))
            {
                return result;
            }

            object index = case_dictionary[path[0]];

            var form_list = case_dictionary[path[0]] as IList<object>;
            if(form_list != null)
            for(int form_index = 0; form_index < form_list.Count; form_index++)
            {
                index = form_list[form_index];

                for (int i = 1; i < path.Length; i++)
                {
                    
                    if (i == path.Length - 1)
                    {
                        if (index is IDictionary<string, object>)
                        {

                            IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                            if(dictionary_object == null)
                            {
                                current = null;
                                continue;
                            }

                            object val = null;

                            if(dictionary_object.ContainsKey(path[i]))
                            {
                                val = dictionary_object[path[i]]; 
                            }


                            if(val != null)
                            {
                                if(val.GetType() == typeof(System.DateTime))
                                {
                                    System.DateTime? temp_date_time = val as System.DateTime?;
                                    current = temp_date_time.Value;	
                                }
                                else if(val.GetType() == typeof(string))
                                {
                                    current = val.ToString();	
                                }

                                else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                                {

                                    current = val as System.Collections.Generic.List<string>;	
                                }
                                else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                                {

                                    current = val as System.Collections.Generic.List<object>;	
                                }
                                else
                                {
                                    current = val;	
                                }

                            }
                            else
                            {
                                current = null;	
                            }

                            result.Add((form_index, current));
                        }
                        else
                        {
                            System.Console.WriteLine("break");
                        }
                    }
                    else if(index is IDictionary<string, object>)
                    {
                        if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                        {
                            index = ((IDictionary<string, object>)index)[path[i]];
                        }
                    
                    }
                    else if (index != null)
                    {
                        System.Console.WriteLine(index.GetType());
                        /*
                        else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                        {
                            index = index[path[i]] as IList<object>;
                        }
                        else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                        {
                            //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                        }
                        else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                        {
                            index = index[path[i]] as IDictionary<string, object>;
                        }
                        */
                    }
                    else
                    {
                        //System.Console.WriteLine("This should not happen. {0}", p_path);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }
        return result;
    }


    public bool set_multiform_value(System.Dynamic.ExpandoObject p_object, string p_path, List<(int, object)> p_value_list)
    {

        var result = false;

        try
        {
            string[] path = p_path.Split('/');
            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            

            IDictionary<string,object> case_dictionary = p_object as IDictionary<string,object>;

            object index = case_dictionary[path[0]];

            var form_list = case_dictionary[path[0]] as IList<object>;
            if(form_list != null)
            {
                result = true;

                for(int form_index = 0; form_index < p_value_list.Count; form_index++)
                {
                    index = form_list[p_value_list[form_index].Item1];
                    
                    for (int i = 1; i < path.Length; i++)
                    {
                        
                        if (i == path.Length - 1)
                        {
                            if (index is IDictionary<string, object>)
                            {

                                IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;
                                var key = path[i];
                                if(dictionary_object.ContainsKey(key))
                                {
                                    dictionary_object[key] = p_value_list[form_index].Item2; 
                                }
                                else
                                {
                                    dictionary_object.Add(key, p_value_list[form_index].Item2);
                                }

                                result = result && true;
                            }
                            else
                            {
                                result = false;
                                System.Console.WriteLine("break");
                            }
                        }
                        else if(index is IDictionary<string, object>)
                        {
                            if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                            {
                                index = ((IDictionary<string, object>)index)[path[i]];
                            }
                        
                        }
                        else if (index != null)
                        {
                            System.Console.WriteLine(index.GetType());
                            /*
                            else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                            {
                                index = index[path[i]] as IList<object>;
                            }
                            else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                            {
                                System.Console.WriteLine("set_multiform_value: Index not found. This should not happen. {0}", p_path);
                            }
                            else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                            {
                                index = index[path[i]] as IDictionary<string, object>;
                            }
                            */
                        }
                        else
                        {
                            System.Console.WriteLine("set_multiform_value: This should not happen. {0}", p_path);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }
        return result;
    }


    public List<object> get_multiform_grid(IDictionary<string, object> p_object, string p_path, bool p_is_grid = false)
    {
        List<object> result = new List<object>();

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            

            List<object> multiform = p_object[path[0]] as List<object>;
            
            if(multiform!= null)
            {
                for(int form_index = 0; form_index < multiform.Count; form_index++)
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
                            else if(p_is_grid)
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
            }
        }
        catch (Exception)
        {
            //System.Console.WriteLine("case_maker.set_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }

    public List<(int, int, object)> get_multiform_grid_value(IDictionary<string, object> p_object, string p_path, bool p_is_grid = true)
    {
        List<(int, int, object)> result = new List<(int, int, object)>();

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            

            List<object> multiform = p_object[path[0]] as List<object>;
            
            if(multiform!= null)
            {
                for(int form_index = 0; form_index < multiform.Count; form_index++)
                {
                    object index = multiform[form_index];

                    for (int i = 1; i < path.Length; i++)
                    {
                        if (i == path.Length - 1)
                        {
                            if (index != null && index is IDictionary<string, object> && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                            {
                                //result.Add(form_index, ((IDictionary<string, object>)index)[path[i]]);
                            }
                            else if(p_is_grid)
                            {
                                //result.Add(index);
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
            }
        }
        catch (Exception)
        {
            //System.Console.WriteLine("case_maker.set_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }



}

