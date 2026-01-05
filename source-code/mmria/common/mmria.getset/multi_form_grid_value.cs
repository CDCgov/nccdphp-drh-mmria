using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate;

public sealed partial class C_Get_Set_Value
{

    public sealed class get_multiform_grid_value_result
    {
        
        public get_multiform_grid_value_result(bool p_is_error, List<(int, int, object)> p_result)
        {
            is_error = p_is_error;
            result = p_result;
        }

        public bool is_error { get; set; }
        public List<(int, int, object)> result { get; set; }
    }
    public get_multiform_grid_value_result get_multiform_grid_value(IDictionary<string, object> p_object, string p_path)
    {
        bool is_error = false;
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
                            if (index != null && index is IList<object>)
                            {
                                var index_list = index as  IList<object>;
                                if(index_list != null)
                                for(var list_index = 0; list_index < index_list.Count; list_index++)
                                {
                                    var grid_row = index_list[list_index] as IDictionary<string,object>;
                                    if(grid_row != null && grid_row.ContainsKey(path[i]))
                                    {
                                        result.Add((form_index, list_index, grid_row[path[i]]));
                                    }
                                    else
                                    {
                                        result.Add((form_index, list_index, null));
                                    }
                                }
                                //result.Add(form_index, ((IDictionary<string, object>)index)[path[i]]);
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
            is_error = true;
            //System.Console.WriteLine("case_maker.set_value bad mapping {0}\n {1}", p_path, ex);
        }

        return new get_multiform_grid_value_result(is_error, result);

    }


    public bool set_multiform_grid_value(IDictionary<string, object> p_object, string p_path, List<(int, int, object)> p_value_list)
    {
        bool result = false;

        int change_count = 0;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            
            foreach(var set_tuple in p_value_list)
            {

                var set_form_index = set_tuple.Item1;
                var set_grid_index = set_tuple.Item2;
                var set_value = set_tuple.Item3;

                List<object> multiform = p_object[path[0]] as List<object>;
                
                if(multiform!= null)
                {
                    for(int form_index = 0; form_index < multiform.Count; form_index++)
                    {
                        if(form_index != set_form_index)
                        {
                            continue;
                        }

                        object index = multiform[form_index];

                        for (int i = 1; i < path.Length; i++)
                        {
                            if (i == path.Length - 1)
                            {
                                if (index != null && index is IList<object>)
                                {


                                    var index_list = index as  IList<object>;
                                    if(index_list != null)
                                    for(var list_index = 0; list_index < index_list.Count; list_index++)
                                    {

                                        if(list_index != set_grid_index)
                                        {
                                            continue;
                                        }
                
                                        var grid_row = index_list[list_index] as IDictionary<string,object>;
                                        if(grid_row != null)
                                        {
                                            try
                                            {

                                                grid_row[path[i]] = set_value;
                                                

                                                if(change_count == 0)
                                                {
                                                    result = true;
                                                }
                                                else
                                                {
                                                    result = result && true;
                                                }
                                                change_count+= 1;
                                                
                                            }
                                            catch(Exception ex)
                                            {
                                                System.Console.WriteLine(ex);
                                                result = false;
                                            }
                                        }
                                    }
                                    //result.Add(form_index, ((IDictionary<string, object>)index)[path[i]]);
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



}

