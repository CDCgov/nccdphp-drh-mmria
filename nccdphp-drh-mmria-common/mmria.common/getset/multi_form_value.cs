using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate;

public sealed partial class C_Get_Set_Value
{
    public sealed class get_multiform_value_result
    {
        public get_multiform_value_result(bool p_is_error, List<(int, object)> p_result)
        {
            is_error = p_is_error;
            result = p_result;
        }

        public bool is_error { get; set; }
        public List<(int, object)> result { get; set; }

    }
    public get_multiform_value_result get_multiform_value(System.Dynamic.ExpandoObject p_object, string p_path)
    {

        
        var is_error = false;

        var result = new List<(int, object)>();

        object current = null;

        try
        {
            string[] path = p_path.Split('/');
            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            

            IDictionary<string,object> case_dictionary = p_object as IDictionary<string,object>;


            if(!case_dictionary.ContainsKey(path[0]))
            {
                return new get_multiform_value_result(is_error, result);
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

                            if(dictionary_object.Count == 0)
                            {
                                return new get_multiform_value_result( is_error, result);
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
                        else if(index == null)
                        {
                            return new get_multiform_value_result( is_error, result);
                        }
                        else
                        {
                            return new get_multiform_value_result( is_error, result);
                        }
                    
                    }
                    else if(index != null)
                    {
                        Console.WriteLine(index.GetType());
                        /*
                        else if (index != null && index is ICollection collection[path[i]].GetType() == typeof(IList<object>))
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
        }
        catch (Exception ex)
        {
            is_error = true;
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }
        return new get_multiform_value_result( is_error, result);
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
                            }*/
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


}

