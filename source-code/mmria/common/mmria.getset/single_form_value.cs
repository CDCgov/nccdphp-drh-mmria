using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate;

public sealed partial class C_Get_Set_Value
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

    public bool set_objectvalue(string p_metadata_path, object p_value, object p_case, int p_index = -1)
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
                        result = set_objectvalue(metadata_path, p_value, val[item_key]);
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
                                    result &= set_objectvalue(metadata_path, p_value, item_val[item_key]);
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

    public bool set_multi_value(string p_metadata_path, object p_value, object p_case)
    {
        bool result = false;

        var metadata_path_array = p_metadata_path.Split("/");
        var item_key = metadata_path_array[0];

        if(metadata_path_array.Length == 1)
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
                default:
                    System.Console.WriteLine("gs.set_muilti_value error: " + p_metadata_path);
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
            //object new_item = null;

            switch(p_case)
            {
                case IDictionary<string,object> val:
                    if(val.ContainsKey(item_key))
                    {
                        result = set_multi_value(metadata_path, p_value, val[item_key]);
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
                                    result &= set_multi_value(metadata_path, p_value, item_val[item_key]);
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

    public sealed class get_value_result
    {
        public get_value_result(bool p_is_error, object p_result)
        {
            is_error = p_is_error;
            result = p_result;
        }

        public bool is_error { get; set; }
        public object result { get; set; }

    }

    public get_value_result get_value(System.Dynamic.ExpandoObject p_object, string p_path, string p_data_type = "string")
    {
        var is_error = false;
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
                            is_error = true;
                            result = null;
                            return new get_value_result(is_error, result);
                        }

                        if(dictionary_object.Count == 0)
                        {
                            is_error = true;
                            result = null;
                            return new get_value_result(is_error, result);
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
                            is_error = true;
                            result = null;
                            return new get_value_result(is_error, result);
                        }

                        if(dictionary_object.Count == 0)
                        {
                            is_error = true;
                            result = null;
                            return new get_value_result(is_error, result);;
                        }/**/

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
                    else
                    {
                        is_error = true;
                        result = null;
                        return new get_value_result(is_error, result);
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
            is_error = true;
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return new get_value_result(is_error, result);

    }

}

